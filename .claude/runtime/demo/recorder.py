"""The Recorder -- observability and live telemetry hook for a demo-enabled run.

The runtime core never talks to a model, so the exchange between the framework and an
agent is visible only at the adapter boundary: the dispatch prompt the runtime writes
before `invocation_started`, and the result envelope the agent writes before
`invocation_completed`. The Recorder reads both from disk at the moment the canonical
event fires, which is the one moment they are guaranteed complete, and turns each into a
structured execution marker. Token utilisation is the runtime's own context estimate
(`context_budget` on the invocation envelope, bytes / 4) plus the size of what came back;
it is an estimate and every marker labels it as one.

Marker schema (`runs/<run>/demo/markers.jsonl`, append-only, one JSON object per line):

    marker_id             "M-000042"
    seq                   42
    timestamp             ISO-8601 UTC with millisecond precision
    t_ms                  milliseconds since the first marker of the run
    run_id
    agent_name            "planner" | "runtime:invocation-gateway" | "host:claude-code"
    action_type           see ACTION_TYPES
    phase                 the workflow phase the marker belongs to, or null
    title                 one-line presentation title
    payload               structured detail; prompt/response excerpts, token estimates,
                          validation counts, gate decisions, tool names
    visual_snapshot_text  the headless dashboard rendered as text at this instant
    source                "runtime" | "host-hook" | "backfill"
    source_event_id       the canonical event id the marker derives from, or null

Everything the Recorder writes lives under `runs/<run>/demo/`. It never mutates a run.
"""

from __future__ import annotations

import hashlib
import json
import re
from datetime import datetime, timezone
from pathlib import Path

from . import DEMO_DIR, MARKERS_FILE, log
from .context import DemoContext

ACTION_TYPES = (
    "run_initialized", "work_item_enqueued", "context_hydrated", "dispatch",
    "prompt_exchange", "agent_response", "validation", "retry", "rollback",
    "gate_awaiting", "gate_decision", "phase_blocked", "phase_unblocked",
    "tool_invocation", "aggregation", "run_completed", "run_aborted", "note",
)

PROMPT_EXCERPT_CHARS = 700
RESPONSE_EXCERPT_CHARS = 1200
LOG_LINES_KEPT = 10
BYTES_PER_TOKEN = 4


def now_ms() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="milliseconds").replace("+00:00", "Z")


def parse_ts(ts: str | None) -> datetime | None:
    if not ts:
        return None
    try:
        return datetime.fromisoformat(ts.replace("Z", "+00:00"))
    except ValueError:
        return None


def ms_between(a: str | None, b: str | None) -> int:
    ta, tb = parse_ts(a), parse_ts(b)
    if not ta or not tb:
        return 0
    return max(0, int((tb - ta).total_seconds() * 1000))


def _digest(text: str) -> str:
    return "sha256:" + hashlib.sha256(text.encode("utf-8")).hexdigest()[:16]


def _read_json(p: Path) -> dict | None:
    try:
        return json.loads(p.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return None


def _read_text(p: Path) -> str | None:
    try:
        return p.read_text(encoding="utf-8", errors="replace")
    except OSError:
        return None


def _size(p: Path) -> int:
    try:
        return p.stat().st_size
    except OSError:
        return 0


def _compact(value, limit: int) -> str:
    """A JSON rendering of `value` cut to `limit` characters, marked when truncated."""
    text = json.dumps(value, ensure_ascii=False, default=str) if not isinstance(value, str) \
        else value
    return text if len(text) <= limit else text[: limit - 1] + "…"


def _first_long_string(value, minimum: int = 40, _depth: int = 0) -> str | None:
    """The first string of at least `minimum` characters found in a structured output, in
    document order. Agents record their reasoning summaries as prose fields (an
    `order_check`, an `executionNote`, a `rationale`), and that prose is the closest thing
    the boundary exposes to the agent's own thinking."""
    if _depth > 6:
        return None
    if isinstance(value, str):
        return value if len(value) >= minimum else None
    if isinstance(value, dict):
        for v in value.values():
            found = _first_long_string(v, minimum, _depth + 1)
            if found:
                return found
    elif isinstance(value, list):
        for v in value:
            found = _first_long_string(v, minimum, _depth + 1)
            if found:
                return found
    return None


def _humanize(identifier: str | None) -> str:
    if not identifier:
        return ""
    return identifier.replace("-", " ")


# ------------------------------------------------------------------ projection


def new_projection(run_id: str) -> dict:
    return {
        "run_id": run_id,
        "command_id": None,
        "workflow_id": None,
        "phases": [],            # ordered: {state_id, index, owner, status, attempt, ...}
        "gates": {},             # name -> {closes, status, owner_roles, decision, ...}
        "current": {"agent": None, "phase": None, "action": "idle", "thinking": False,
                    "detail": ""},
        "tokens": {"prompt_est": 0, "legacy_est": 0, "response_est": 0},
        "counters": {"invocations": 0, "validations_passed": 0, "validations_failed": 0,
                     "retries": 0, "rollbacks": 0, "tool_calls": 0, "gates_decided": 0},
        "log": [],
        "started_at": None,
        "last_at": None,
        "finished": None,
    }


def _phase(proj: dict, state_id: str | None) -> dict | None:
    if not state_id:
        return None
    return next((p for p in proj["phases"] if p["state_id"] == state_id), None)


def _ensure_phase(proj: dict, state_id: str, owner: str | None = None,
                  index: int | None = None) -> dict:
    p = _phase(proj, state_id)
    if p is None:
        p = {"state_id": state_id, "index": index or len(proj["phases"]) + 1,
             "owner": owner, "status": "pending", "attempt": 0, "validation": None,
             "gates": [], "artifact": None}
        proj["phases"].append(p)
        proj["phases"].sort(key=lambda x: (x["index"], x["state_id"]))
    if owner and not p.get("owner"):
        p["owner"] = owner
    return p


def _fmt_clock(ms: int) -> str:
    s = max(0, ms) // 1000
    h, rem = divmod(s, 3600)
    m, sec = divmod(rem, 60)
    return f"{h:d}:{m:02d}:{sec:02d}"


def _estimate_budget(envelope: dict, claude_root: Path) -> dict:
    """The dispatch's context estimate. Envelopes written by runtime 0.7.0+ carry it as
    `context_budget`; older ones are measured now with the runtime's own estimator, so a
    historical run shows real numbers rather than zeros."""
    budget = envelope.get("context_budget")
    if budget:
        return budget
    try:
        import execution_metrics as em  # sibling runtime module, on sys.path with the runtime
        return em.estimate_dispatch_budget(envelope, claude_root) or {}
    except Exception:  # noqa: BLE001 -- an estimate is decoration, never a dependency
        return {}


def apply_marker(proj: dict, m: dict) -> dict:
    """Advance the projection by one marker. Pure: the same marker sequence always yields
    the same projection, which is what makes the snapshot text and the video deterministic."""
    a, p, phase_id = m["action_type"], m.get("payload") or {}, m.get("phase")
    if proj["started_at"] is None:
        proj["started_at"] = m["timestamp"]
    proj["last_at"] = m["timestamp"]
    cur = proj["current"]

    if a == "run_initialized":
        proj["command_id"] = p.get("command_id")
        proj["workflow_id"] = p.get("workflow_id")
        cur.update(agent="runtime", phase=None, action="run accepted", thinking=False,
                   detail=m["title"])
    elif a == "work_item_enqueued":
        if p.get("work_type") == "gate":
            proj["gates"][p["gate"]] = {
                "closes": phase_id, "status": "undecided",
                "owner_roles": p.get("owner_roles") or [], "decision": None,
                "owner_role": None, "decided_by": None}
            ph = _ensure_phase(proj, phase_id)
            if p["gate"] not in ph["gates"]:
                ph["gates"].append(p["gate"])
        else:
            _ensure_phase(proj, phase_id, owner=p.get("owner_agent_id"), index=p.get("index"))
        cur.update(agent="runtime", action="materializing workflow", thinking=False,
                   detail=m["title"])
    elif a == "context_hydrated":
        ph = _ensure_phase(proj, phase_id)
        cur.update(agent="runtime", phase=phase_id, action="freezing context slice",
                   thinking=False, detail=m["title"])
    elif a == "dispatch":
        ph = _ensure_phase(proj, phase_id, owner=p.get("agent_id"))
        ph["status"] = "leased"
        # Counted, not read: the envelope on disk is always the latest attempt's, so a
        # backfilled first attempt would otherwise report the last attempt's number.
        ph["attempt"] = ph["attempt"] + 1
        proj["tokens"]["prompt_est"] += p.get("prompt_tokens_est") or 0
        proj["tokens"]["legacy_est"] += p.get("legacy_tokens_est") or 0
        cur.update(agent=p.get("agent_id") or ph["owner"], phase=phase_id,
                   action="leased to adapter", thinking=False, detail=m["title"])
    elif a == "prompt_exchange":
        ph = _ensure_phase(proj, phase_id)
        ph["status"] = "running"
        proj["counters"]["invocations"] += 1
        cur.update(agent=m["agent_name"], phase=phase_id, action="thinking", thinking=True,
                   detail=p.get("prompt_excerpt") or m["title"])
    elif a == "agent_response":
        ph = _ensure_phase(proj, phase_id)
        ph["status"] = "reported"
        proj["tokens"]["response_est"] += p.get("response_tokens_est") or 0
        cur.update(agent=m["agent_name"], phase=phase_id, action="responded", thinking=False,
                   detail=p.get("agent_note") or p.get("structured_output_excerpt")
                   or m["title"])
    elif a == "validation":
        ph = _ensure_phase(proj, phase_id)
        ph["validation"] = {"result": p.get("result"), "passed": p.get("checks_passed"),
                            "run": p.get("checks_run"), "failures": p.get("failures") or []}
        if p.get("result") == "pass":
            ph["status"] = "completed"
            proj["counters"]["validations_passed"] += 1
        else:
            ph["status"] = "rejected"
            proj["counters"]["validations_failed"] += 1
        cur.update(agent="runtime:validation-engine", phase=phase_id,
                   action="validating artifact", thinking=False, detail=m["title"])
    elif a == "retry":
        ph = _ensure_phase(proj, phase_id)
        ph["status"] = "retrying"
        proj["counters"]["retries"] += 1
        cur.update(agent="runtime:recovery-controller", phase=phase_id,
                   action="scheduling retry", thinking=False, detail=m["title"])
    elif a == "rollback":
        proj["counters"]["rollbacks"] += 1
        for sid in p.get("superseded") or []:
            ph = _phase(proj, sid)
            if ph:
                ph["status"] = "superseded"
        g = proj["gates"].get(p.get("gate") or "")
        if g:
            g.update(status="undecided", decision=None)
        cur.update(agent=m["agent_name"], phase=phase_id, action="rolling back",
                   thinking=False, detail=m["title"])
    elif a == "gate_awaiting":
        g = proj["gates"].setdefault(p.get("gate") or m["title"], {
            "closes": phase_id, "status": "undecided", "owner_roles": [], "decision": None,
            "owner_role": None, "decided_by": None})
        g["status"] = "awaiting"
        cur.update(agent="runtime:state-engine", phase=g.get("closes") or phase_id,
                   action="awaiting human decision", thinking=False, detail=m["title"])
    elif a == "gate_decision":
        g = proj["gates"].setdefault(p.get("gate") or m["title"], {
            "closes": phase_id, "status": "undecided", "owner_roles": [], "decision": None,
            "owner_role": None, "decided_by": None})
        g.update(status=p.get("decision") or "approved", decision=p.get("decision"),
                 owner_role=p.get("owner_role"), decided_by=p.get("decided_by"))
        proj["counters"]["gates_decided"] += 1
        cur.update(agent=m["agent_name"], phase=g.get("closes") or phase_id,
                   action=f"gate {p.get('decision') or 'decided'}", thinking=False,
                   detail=p.get("rationale") or m["title"])
    elif a == "phase_blocked":
        ph = _ensure_phase(proj, phase_id)
        ph["status"] = "blocked"
        ph["blocked_reason"] = p.get("reason")
        cur.update(agent="runtime:state-engine", phase=phase_id, action="blocked",
                   thinking=False, detail=m["title"])
    elif a == "phase_unblocked":
        ph = _ensure_phase(proj, phase_id)
        if ph["status"] in ("blocked", "retrying", "rejected"):
            ph["status"] = "pending"
        cur.update(agent="runtime:state-engine", phase=phase_id, action="unblocked",
                   thinking=False, detail=m["title"])
    elif a == "tool_invocation":
        proj["counters"]["tool_calls"] += 1
        cur.update(action=f"tool: {p.get('tool_name')}", thinking=True,
                   detail=p.get("tool_input_excerpt") or m["title"])
        if m.get("agent_name"):
            cur["agent"] = m["agent_name"]
    elif a == "aggregation":
        cur.update(agent="runtime:output-aggregator", phase=None, action="aggregating outputs",
                   thinking=False, detail=m["title"])
    elif a in ("run_completed", "run_aborted"):
        proj["finished"] = a
        cur.update(agent="runtime", phase=None, action=a.replace("_", " "), thinking=False,
                   detail=m["title"])
    elif a == "note":
        cur.update(detail=m["title"])

    stamp = _fmt_clock(m.get("t_ms", 0))
    proj["log"].append(f"{stamp}  {m['agent_name'][:26]:<26} {m['title']}")
    proj["log"] = proj["log"][-LOG_LINES_KEPT:]
    return proj


def build_projection(markers: list) -> dict:
    proj = new_projection(markers[0]["run_id"] if markers else "run")
    for m in markers:
        apply_marker(proj, m)
    return proj


# ------------------------------------------------------------------ the recorder


class Recorder:
    """Writes one marker per observed fact into `runs/<run>/demo/markers.jsonl`."""

    def __init__(self, run_dir: Path):
        self.run_dir = Path(run_dir)
        self.claude_root = self.run_dir.parent.parent
        self.dir = self.run_dir / DEMO_DIR
        self.path = self.dir / MARKERS_FILE
        self.ctx = DemoContext.load(self.run_dir)

    # ---------------------------------------------------------------- storage

    def markers(self) -> list:
        if not self.path.exists():
            return []
        out = []
        for line in self.path.read_text(encoding="utf-8").splitlines():
            line = line.strip()
            if line:
                try:
                    out.append(json.loads(line))
                except ValueError:
                    continue
        return out

    def _append(self, marker: dict) -> dict:
        self.dir.mkdir(parents=True, exist_ok=True)
        with self.path.open("a", encoding="utf-8") as fh:
            fh.write(json.dumps(marker, ensure_ascii=False, default=str) + "\n")
        return marker

    def _stamp(self, markers: list, timestamp: str | None) -> tuple[str, int]:
        ts = timestamp or now_ms()
        first = markers[0]["timestamp"] if markers else ts
        return ts, ms_between(first, ts)

    def _emit(self, *, action_type: str, agent_name: str, title: str, phase: str | None,
              payload: dict, source: str, source_event_id: str | None,
              timestamp: str | None = None) -> dict:
        if action_type not in ACTION_TYPES:
            action_type = "note"
        markers = self.markers()
        ts, t_ms = self._stamp(markers, timestamp)
        marker = {
            "marker_id": f"M-{len(markers) + 1:06d}",
            "seq": len(markers) + 1,
            "timestamp": ts,
            "t_ms": t_ms,
            "run_id": self.run_dir.name,
            "agent_name": agent_name,
            "action_type": action_type,
            "phase": phase,
            "title": title,
            "payload": payload,
            "visual_snapshot_text": "",
            "source": source,
            "source_event_id": source_event_id,
        }
        proj = build_projection(markers)
        apply_marker(proj, marker)
        try:
            from . import renderer
            marker["visual_snapshot_text"] = renderer.render_text(proj, marker,
                                                                  title=self._title())
        except Exception as exc:  # noqa: BLE001 -- a snapshot is nice to have, never required
            log(self.run_dir, f"text snapshot failed at {marker['marker_id']}: {exc!r}")
        return self._append(marker)

    def _title(self) -> str:
        return (self.ctx.opt("title") if self.ctx else None) or self.run_dir.name

    # ---------------------------------------------------------------- runtime events

    def record_event(self, ev: dict, *, source: str = "runtime") -> dict | None:
        """Translate one canonical runtime event into a marker, enriched from disk."""
        markers = self.markers()
        eid = ev.get("event_id")
        if eid and any(m.get("source_event_id") == eid for m in markers):
            return None      # a replayed event is not a second fact
        et = ev.get("event_type")
        state_id = ev.get("state_id")
        details = ev.get("details_ref") or {}
        summary = ev.get("summary") or ""
        actor = ev.get("actor_id") or "runtime"
        ts = ev.get("timestamp")
        if ts and "." not in ts:
            ts = ts.replace("Z", ".000Z") if source == "backfill" else now_ms()
        state_dir = self.run_dir / "states" / (state_id or "")
        common = {"source": source, "source_event_id": eid, "timestamp": ts,
                  "payload": {"event_type": et, "reason_code": ev.get("reason_code"),
                              "summary": summary}}

        if et == "run_initialized":
            req = _read_json(self.run_dir / "execution-request.json") or {}
            common["payload"].update(command_id=req.get("command_id"),
                                     workflow_id=req.get("workflow_id"),
                                     phases=req.get("phases") or [],
                                     inputs=req.get("inputs") or [],
                                     requester=req.get("requester"))
            return self._emit(action_type="run_initialized", agent_name=f"runtime:{actor}",
                              phase=state_id,
                              title=f"Run accepted: /{req.get('command_id', '?')} -> "
                                    f"{req.get('workflow_id', '?')} "
                                    f"({len(req.get('phases') or [])} phases)", **common)

        if et == "work_item_enqueued":
            wt = details.get("work_type") or "state"
            if wt == "gate":
                closes = None
                m = re.search(r"close phase (\S+)", summary)
                if m:
                    closes = m.group(1)
                common["payload"].update(work_type="gate", gate=state_id,
                                         owner_roles=details.get("owner_roles") or [],
                                         producer=details.get("producer_agent"))
                return self._emit(action_type="work_item_enqueued",
                                  agent_name=f"runtime:{actor}", phase=closes,
                                  title=f"Gate armed: {state_id} "
                                        f"({', '.join(details.get('owner_roles') or [])})",
                                  **common)
            m = re.search(r"work item (\d+)/(\d+)", summary)
            idx = int(m.group(1)) if m else None
            owner = details.get("owner_agent_id")
            common["payload"].update(work_type="state", index=idx, owner_agent_id=owner,
                                     depends_on=details.get("depends_on") or [])
            return self._emit(action_type="work_item_enqueued", agent_name=f"runtime:{actor}",
                              phase=state_id,
                              title=f"Phase {idx or '?'}: {state_id} -> {owner}", **common)

        if et == "context_hydrated":
            m = re.search(r"(\d+) member\(s\), (\d+) input", summary)
            common["payload"].update(members=int(m.group(1)) if m else None,
                                     inputs=int(m.group(2)) if m else None,
                                     snapshot="states/%s/context-snapshot.json" % state_id)
            return self._emit(action_type="context_hydrated", agent_name=f"runtime:{actor}",
                              phase=state_id,
                              title=f"Context slice frozen for {state_id}"
                                    + (f": {m.group(1)} files" if m else ""), **common)

        if et == "work_item_leased":
            env = _read_json(state_dir / "invocation-envelope.json") or {}
            budget = _estimate_budget(env, self.claude_root) if env else {}
            prompt_tokens = budget.get("progressive_estimate_tokens")
            legacy_tokens = budget.get("legacy_estimate_tokens")
            cb = env.get("capability_bindings") or {}
            profile = cb.get("load_profile") or {}
            required_skills = [s.get("skillCode") for s in env.get("skill_dispatch") or []
                               if s.get("status") == "required"]
            common["payload"].update(
                agent_id=env.get("agent_id"), agent_version=env.get("agent_version"),
                invocation_id=env.get("invocation_id"),
                attempt=(env.get("timeout_profile") or {}).get("attempt"),
                idempotency_key=details.get("idempotency_key"),
                adapter=details.get("adapter"),
                prompt_tokens_est=prompt_tokens, legacy_tokens_est=legacy_tokens,
                savings_pct=budget.get("savings_pct"),
                core_modules=len(profile.get("core") or []) or None,
                modules_total=len(cb.get("load_order") or []) or None,
                skills_required=required_skills,
                upstream=[u.get("type") for u in env.get("upstream_artifacts") or []],
                token_basis="runtime context estimate, bytes/4"
                            + ("" if env.get("context_budget") else
                               " (measured now: the envelope predates runtime 0.7.0)"))
            agent = env.get("agent_id") or (_phase(build_projection(markers), state_id)
                                            or {}).get("owner") or "agent"
            title = f"Dispatching {agent} for {state_id}"
            if prompt_tokens:
                title += f" (~{prompt_tokens:,} tokens of context)"
            return self._emit(action_type="dispatch", agent_name=f"runtime:{actor}",
                              phase=state_id, title=title, **common)

        if et == "invocation_started":
            prompt = _read_text(state_dir / "dispatch-prompt.md") or ""
            m = re.search(r"dispatching agent (\S+) v(\S+)", summary)
            agent = m.group(1) if m else actor
            version = m.group(2) if m else None
            excerpt = _prompt_excerpt(prompt)
            common["payload"].update(
                agent_id=agent, agent_version=version,
                invocation_id=details.get("invocation_id"),
                prompt_path=f"states/{state_id}/dispatch-prompt.md",
                prompt_bytes=len(prompt.encode("utf-8")),
                prompt_digest=_digest(prompt) if prompt else None,
                prompt_tokens_est=len(prompt.encode("utf-8")) // BYTES_PER_TOKEN,
                prompt_excerpt=excerpt, thinking=True)
            return self._emit(action_type="prompt_exchange", agent_name=agent, phase=state_id,
                              title=f"{agent} is thinking about {state_id}", **common)

        if et == "invocation_completed":
            res = _read_json(state_dir / "result-envelope.json") or {}
            so = res.get("structured_output")
            refs = res.get("artifact_refs") or []
            art_bytes = sum(_size(self.claude_root / r) for r in refs if isinstance(r, str))
            res_bytes = _size(state_dir / "result-envelope.json")
            common["payload"].update(
                agent_id=actor, status=res.get("status"), artifact_refs=refs,
                confidence=res.get("confidence"),
                declared_side_effects=res.get("declared_side_effects") or [],
                error_class=res.get("error_class"),
                structured_output_excerpt=_compact(so, RESPONSE_EXCERPT_CHARS) if so else None,
                agent_note=_first_long_string(so),
                response_bytes=res_bytes, artifact_bytes=art_bytes,
                response_tokens_est=(res_bytes + art_bytes) // BYTES_PER_TOKEN,
                thinking=False)
            status = res.get("status") or "reported"
            return self._emit(action_type="agent_response", agent_name=actor, phase=state_id,
                              title=f"{actor} returned '{status}' with {len(refs)} "
                                    f"artifact{'s' if len(refs) != 1 else ''}", **common)

        if et in ("validation_passed", "validation_failed"):
            rep = _read_json(state_dir / "validation-report.json") or {}
            m = re.search(r"(\d+)/(\d+) checks", summary)
            passed = rep.get("checksPassed", int(m.group(1)) if m else None)
            run = rep.get("checksRun", int(m.group(2)) if m else None)
            failures = details.get("failures") or [c.get("id") for c in rep.get("checks") or []
                                                   if c.get("result") == "fail"]
            result = "pass" if et == "validation_passed" else "fail"
            common["payload"].update(result=result, checks_passed=passed, checks_run=run,
                                     failures=failures,
                                     blocking=rep.get("blockingFailures"),
                                     correctable=rep.get("correctableFailures"),
                                     undeclared_side_effects=details.get(
                                         "undeclared_side_effects") or [])
            if result == "pass":
                title = f"Validation passed for {state_id}"
                if passed is not None and run is not None:
                    title += f": {passed}/{run} checks"
            else:
                title = f"Validation rejected {state_id}: {len(failures)} failing check" \
                        f"{'s' if len(failures) != 1 else ''}"
            return self._emit(action_type="validation", agent_name=f"runtime:{actor}",
                              phase=state_id, title=title, **common)

        if et == "retry_scheduled":
            m = re.search(r"classified (\S+) -> (\S+)", summary)
            common["payload"].update(failure_class=m.group(1) if m else details.get(
                "failure_class"), action=m.group(2) if m else "retry",
                available_at=details.get("available_at"))
            return self._emit(action_type="retry", agent_name=f"runtime:{actor}",
                              phase=state_id,
                              title=f"Retry scheduled for {state_id} "
                                    f"({m.group(1) if m else 'classified failure'})", **common)

        if et == "rollback_scheduled":
            common["payload"].update(gate=details.get("gate"),
                                     superseded=details.get("superseded") or [],
                                     authorisation_id=details.get("authorisation_id"))
            return self._emit(action_type="rollback", agent_name=f"{ev.get('actor_type')}:"
                                                                 f"{actor}",
                              phase=state_id,
                              title=f"Rollback authorised through {details.get('gate') or state_id}",
                              **common)

        if et == "escalation_opened":
            proj = build_projection(markers)
            is_gate = details.get("work_type") == "gate" or state_id in proj["gates"] \
                or "gate work item" in summary
            reason = summary.split("blocked:")[-1].strip() if "blocked:" in summary else summary
            if is_gate:
                g = proj["gates"].get(state_id) or {}
                common["payload"].update(gate=state_id, closes=g.get("closes"),
                                         owner_roles=g.get("owner_roles") or [], reason=reason)
                return self._emit(action_type="gate_awaiting", agent_name=f"runtime:{actor}",
                                  phase=g.get("closes"),
                                  title=f"{state_id} awaits a human decision", **common)
            common["payload"].update(reason=reason)
            return self._emit(action_type="phase_blocked", agent_name=f"runtime:{actor}",
                              phase=state_id, title=f"{state_id} blocked: {reason}", **common)

        if et == "escalation_resolved":
            if details.get("gate"):
                decision = "approved" if "approved" in summary else \
                    "rejected" if "rejected" in summary else "decided"
                common["payload"].update(gate=details["gate"], decision=decision,
                                         owner_role=details.get("owner_role"),
                                         decided_by=details.get("decided_by"),
                                         rationale=details.get("rationale"),
                                         auto_policy=details.get("auto_policy"))
                proj = build_projection(markers)
                closes = (proj["gates"].get(details["gate"]) or {}).get("closes")
                who = details.get("owner_role") or actor
                return self._emit(action_type="gate_decision",
                                  agent_name=f"{ev.get('actor_type', 'human')}:{who}",
                                  phase=closes,
                                  title=f"{details['gate']} {decision} by {_humanize(who)}",
                                  **common)
            return self._emit(action_type="phase_unblocked", agent_name=f"runtime:{actor}",
                              phase=state_id, title=f"{state_id} unblocked", **common)

        if et == "aggregation_completed":
            m = re.search(r"(\d+)/(\d+) completed", summary)
            common["payload"].update(package=details.get("package"),
                                     completed=int(m.group(1)) if m else None,
                                     phases=int(m.group(2)) if m else None)
            return self._emit(action_type="aggregation", agent_name=f"runtime:{actor}",
                              phase=None,
                              title=f"Completion package aggregated"
                                    + (f" ({m.group(1)}/{m.group(2)} phases)" if m else ""),
                              **common)

        if et in ("run_completed", "run_aborted"):
            metrics = _read_json(self.run_dir / "execution-metrics.json") or {}
            est = metrics.get("tokens_or_context_estimate") or {}
            common["payload"].update(final_report=details.get("final_report"),
                                     package=details.get("package"),
                                     progressive_tokens=est.get("progressive_tokens"),
                                     legacy_tokens=est.get("legacy_tokens"),
                                     savings_pct=est.get("savings_pct"))
            title = "Run completed: every phase delivered and every gate decided" \
                if et == "run_completed" else f"Run aborted: {summary}"
            return self._emit(action_type=et, agent_name=f"runtime:{actor}", phase=None,
                              title=title, **common)

        return self._emit(action_type="note", agent_name=f"runtime:{actor}", phase=state_id,
                          title=summary[:120] or et, **common)

    # ---------------------------------------------------------------- host tool hooks

    def record_tool_invocation(self, payload: dict) -> dict:
        """One marker per host tool call. `payload` is the hook's JSON as the host sent it
        (Claude Code: session_id, hook_event_name, tool_name, tool_input, tool_response).
        Inputs are excerpted, never stored whole: a demo shows the shape of the work, and a
        tool input can carry file contents that do not belong in a presentation."""
        proj = build_projection(self.markers())
        tool = payload.get("tool_name") or payload.get("tool") or "tool"
        tool_input = payload.get("tool_input") or payload.get("input") or {}
        response = payload.get("tool_response") or payload.get("output")
        target = None
        if isinstance(tool_input, dict):
            target = tool_input.get("file_path") or tool_input.get("path") \
                or tool_input.get("command") or tool_input.get("pattern") \
                or tool_input.get("url") or tool_input.get("description")
        excerpt = _compact(target if target is not None else tool_input, 160)
        # Attributed to the agent whose phase is running: the host's tool calls are that
        # agent's hands. Between phases they belong to the session itself.
        agent = str(proj["current"].get("agent") or "")
        if agent.startswith("host:"):
            agent = agent[5:]
        if not agent or agent.startswith("runtime"):
            agent = (payload.get("session_id") or "session")[:8]
        agent_name = f"host:{agent}"
        marker_payload = {
            "hook_event": payload.get("hook_event_name") or "PostToolUse",
            "tool_name": tool,
            "tool_input_excerpt": excerpt,
            "tool_input_keys": sorted(tool_input.keys()) if isinstance(tool_input, dict) else [],
            "tool_response_bytes": len(json.dumps(response, default=str)) if response is not None
            else 0,
            "session_id": (payload.get("session_id") or "")[:12] or None,
            "cwd": payload.get("cwd"),
        }
        title = f"{tool}: {excerpt}" if excerpt and excerpt != "{}" else tool
        return self._emit(action_type="tool_invocation", agent_name=agent_name,
                          phase=proj["current"].get("phase"), title=title[:140],
                          payload=marker_payload, source="host-hook", source_event_id=None)

    def note(self, title: str, payload: dict | None = None, phase: str | None = None) -> dict:
        return self._emit(action_type="note", agent_name="runtime:demo", phase=phase,
                          title=title, payload=payload or {}, source="runtime",
                          source_event_id=None)

    # ---------------------------------------------------------------- backfill

    def backfill_from_events(self) -> dict:
        """Reconstruct markers from `events.jsonl` for every event not yet recorded.

        Used when `--demo` was enabled on a run already in flight, and by `demo --backfill`
        to build a presentation of any historical run. Event timestamps carry second
        precision, so backfilled markers are stamped `.000`; live ones carry milliseconds.
        """
        ev_path = self.run_dir / "events.jsonl"
        if not ev_path.exists():
            return {"recorded": 0, "skipped": 0}
        recorded = skipped = 0
        for line in ev_path.read_text(encoding="utf-8").splitlines():
            line = line.strip()
            if not line:
                continue
            try:
                ev = json.loads(line)
            except ValueError:
                skipped += 1
                continue
            if self.record_event(ev, source="backfill") is None:
                skipped += 1
            else:
                recorded += 1
        log(self.run_dir, f"backfill: {recorded} marker(s) recorded, {skipped} skipped")
        return {"recorded": recorded, "skipped": skipped}


def _prompt_excerpt(prompt: str) -> str:
    """The part of a dispatch prompt worth showing: the instruction block when present,
    otherwise the head of the document, whitespace-collapsed."""
    if not prompt:
        return ""
    body = prompt
    for pattern in (r"## Instruction to the agent\s*\n(.+)", r"## Read first[^\n]*\n(.+)"):
        m = re.search(pattern, prompt, re.S)
        if m:
            body = m.group(1)
            break
    # prose only: the metadata table and the headings are structure, not what the agent reads
    lines = [ln.strip() for ln in body.splitlines()
             if ln.strip() and not ln.lstrip().startswith(("|", "#"))]
    text = " ".join(lines)
    text = re.sub(r"`[^`]*`", lambda mm: mm.group(0).strip("`").split("/")[-1], text)
    text = re.sub(r"\s+", " ", text).strip()
    return text[:PROMPT_EXCERPT_CHARS] + ("…" if len(text) > PROMPT_EXCERPT_CHARS else "")
