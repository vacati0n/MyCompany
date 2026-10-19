#!/usr/bin/env python3
"""Execution metrics -- the lightweight performance trace every run records.

`runs/<run-id>/execution-metrics.json` is derived from persisted evidence only: the state
store, the event stream, each phase's invocation envelope and state ledger, and the sizes of
the files those name. Nothing here is measured by instrumenting a model; the estimate is a
byte count of what the dispatch asked the agent to read, which is the quantity the runtime
controls and the one that makes a regression visible.

Two estimates are kept side by side for every dispatch:

- `legacy` -- what the dispatch cost under the pre-0.7.0 rules: every module in the load
  order, every context-slice member, every upstream artifact, all read in full;
- `progressive` -- what the same dispatch costs under `config/runtime.md` ("Progressive
  Module Loading" and "Task Context"): the core modules, the slice members marked
  `required`, the upstream sections marked `read_first`, and the task context.

Both are computed the same way from the same envelope, so a historical run can be measured
under the new rules and a new run under the old, which is what makes the before/after
comparison in `reports/` like-for-like. Tokens are estimated at four bytes per token; the
figure is an estimate and is labelled as one.
"""

from __future__ import annotations

import json
import re
from datetime import datetime, timezone
from pathlib import Path

import yaml

import artifact_lib as al

SCHEMA = "framework.runtime/execution-metrics.v1"
FILENAME = "execution-metrics.json"
BYTES_PER_TOKEN = 4

# Tier names in promotion order, and the bucket for records that predate the model tier field.
# The tier policy itself lives in `framework_runtime.py`; this module only groups by what the
# persisted envelopes and events already say, so it names no hint and imports nothing from it.
TIERS = ("light", "standard", "deep")
UNTIERED = "untiered"

CORE_MODULE_ROLES = ("operating-charter", "reasoning-procedure", "output-contract",
                     "quality-contract")


def _size(path: Path) -> int:
    try:
        return path.stat().st_size
    except OSError:
        return 0


def _parse_ts(ts: str | None):
    if not ts:
        return None
    try:
        return datetime.fromisoformat(ts.replace("Z", "+00:00"))
    except ValueError:
        return None


def _seconds(a: str | None, b: str | None) -> float | None:
    ta, tb = _parse_ts(a), _parse_ts(b)
    if not ta or not tb:
        return None
    return max(0.0, (tb - ta).total_seconds())


def _fmt(seconds: float | None) -> str:
    if seconds is None:
        return "-"
    s = int(seconds)
    h, rem = divmod(s, 3600)
    m, sec = divmod(rem, 60)
    return f"{h}h{m:02d}m{sec:02d}s" if h else f"{m}m{sec:02d}s"


def section_bytes(text: str, titles: list) -> int:
    """Bytes of the level-2 sections whose normalised title matches one in `titles`."""
    wanted = [t.lower() for t in titles]
    total = 0
    for title, body in al.split_sections(text):
        nt = re.sub(r"^[0-9]+(\.[0-9]+)*\s*", "", title).strip().lower()
        if any(nt.startswith(w) or nt.endswith(w) for w in wanted):
            total += len(body.encode("utf-8"))
    return total


def estimate_dispatch_budget(envelope: dict, claude_root: Path,
                             upstream_sections: dict | None = None,
                             task_context_bytes: int = 0, read_hint=None) -> dict:
    """Legacy and progressive byte estimates for one invocation envelope.

    Works for envelopes written before this module existed: the module roles come from the
    manifest the envelope names, a slice member without a read hint is classified by
    `read_hint(path, phase)` when one is supplied and counted as `required` otherwise, and the
    upstream section map defaults to reading the whole artifact.
    """
    cb = envelope.get("capability_bindings") or {}
    manifest_rel = cb.get("manifest")
    roles = {}
    if manifest_rel and (claude_root / manifest_rel).exists():
        man = yaml.safe_load((claude_root / manifest_rel).read_text(encoding="utf-8")) or {}
        base = Path(manifest_rel).parent.as_posix()
        for m in (man.get("runtime") or {}).get("modules") or []:
            roles[f"{base}/{m['path']}"] = m.get("role")
    profile = cb.get("load_profile") or {}
    core_paths = set(profile.get("core") or [])
    modules_full = modules_core = 0
    for rel in cb.get("load_order") or []:
        b = _size(claude_root / rel)
        modules_full += b
        is_core = rel in core_paths if core_paths else roles.get(rel) in CORE_MODULE_ROLES
        if is_core:
            modules_core += b

    slice_all = slice_required = 0
    seen_members = set()
    for m in (envelope.get("context_slice") or {}).get("members") or []:
        b = _size(claude_root / m["path"])
        slice_all += b
        if m["path"] in seen_members:
            continue      # a duplicate member was one file; the progressive rule reads it once
        seen_members.add(m["path"])
        read = m.get("read")
        if read is None:
            read = read_hint(m["path"], envelope.get("state_id")) if read_hint else "required"
        if read == "required":
            slice_required += b

    inputs = 0
    upstream_full = upstream_first = 0
    upstream_refs = {u["reference"] for u in envelope.get("upstream_artifacts") or []}
    for s in (envelope.get("input_contract") or {}).get("supplied") or []:
        p = Path(s.get("source_file") or "")
        if not p.is_absolute():
            p = claude_root / s.get("reference", "")
        b = _size(p)
        if s.get("reference") in upstream_refs:
            upstream_full += b
            spec = (upstream_sections or {}).get(Path(s["reference"]).name)
            if spec and p.exists():
                upstream_first += section_bytes(p.read_text(encoding="utf-8"),
                                                spec.get("read_first") or [])
            else:
                upstream_first += b
        else:
            inputs += b

    fixed = 0
    eo = envelope.get("expected_output_schema") or {}
    for rel in (eo.get("template_ref"), envelope.get("host_registration")):
        if rel:
            fixed += _size(claude_root / rel)
    run_id, state = envelope.get("run_id"), envelope.get("state_id")
    state_dir = claude_root / "runs" / str(run_id) / "states" / str(state)
    fixed += _size(state_dir / "invocation-envelope.json") + _size(state_dir / "dispatch-prompt.md")

    legacy = modules_full + slice_all + inputs + upstream_full + fixed
    progressive = modules_core + slice_required + inputs + upstream_first + fixed \
        + task_context_bytes
    return {
        "modules_full_bytes": modules_full,
        "modules_core_bytes": modules_core,
        "slice_all_bytes": slice_all,
        "slice_required_bytes": slice_required,
        "inputs_bytes": inputs,
        "upstream_full_bytes": upstream_full,
        "upstream_read_first_bytes": upstream_first,
        "task_context_bytes": task_context_bytes,
        "fixed_bytes": fixed,
        "legacy_estimate_bytes": legacy,
        "progressive_estimate_bytes": progressive,
        "legacy_estimate_tokens": legacy // BYTES_PER_TOKEN,
        "progressive_estimate_tokens": progressive // BYTES_PER_TOKEN,
        "savings_pct": round(100 * (1 - progressive / legacy), 1) if legacy else 0.0,
    }


def tier_summary(events: list, per_phase: list) -> dict:
    """Invocations and estimated context bytes grouped by model tier.

    Invocations are counted from `invocation_started` events, whose detail carries the tier the
    dispatch resolved; an event that predates the field lands in `untiered`. Bytes are each
    phase's progressive estimate, taken from the tier of that phase's latest envelope, and a
    phase whose envelope predates the field also lands in `untiered`, so the per-tier values
    sum to the run totals. `non_deep_share` is over tiered invocations only, and is None while
    there are none.
    """
    names = TIERS + (UNTIERED,)
    by_tier = {t: {"invocations": 0, "estimated_bytes": 0} for t in names}
    for e in events:
        if e.get("event_type") != "invocation_started":
            continue
        tier = (e.get("details_ref") or {}).get("model_tier")
        by_tier[tier if tier in TIERS else UNTIERED]["invocations"] += 1
    for p in per_phase:
        tier = p.get("model_tier")
        by_tier[tier if tier in TIERS else UNTIERED]["estimated_bytes"] += \
            (p.get("context_estimate") or {}).get("progressive_estimate_bytes", 0)
    tiered = sum(by_tier[t]["invocations"] for t in TIERS)
    non_deep = tiered - by_tier["deep"]["invocations"]
    return {
        "by_tier": by_tier,
        "invocations_total": sum(v["invocations"] for v in by_tier.values()),
        "estimated_bytes_total": sum(v["estimated_bytes"] for v in by_tier.values()),
        "non_deep_share": round(non_deep / tiered, 3) if tiered else None,
    }


def compute(run_dir: Path, claude_root: Path, *, store=None, events: list | None = None,
            upstream_sections: dict | None = None, now: str | None = None,
            read_hint=None) -> dict:
    """Build the metrics document for one run from its persisted evidence."""
    if store is None:
        import state_engine as se
        store = se.StateStore.load(run_dir)
    if events is None:
        ev_path = run_dir / "events.jsonl"
        events = [json.loads(ln) for ln in ev_path.read_text(encoding="utf-8").splitlines()
                  if ln.strip()] if ev_path.exists() else []
    data = store.data
    states = store.state_items()

    invocations = [e for e in events if e["event_type"] == "invocation_started"]
    validations = [e for e in events if e["event_type"] in ("validation_passed",
                                                             "validation_failed")]
    by_agent: dict = {}
    for i in states:
        n = sum(1 for e in invocations if e["state_id"] == i["state_id"])
        if n:
            by_agent[i["owner_agent_id"]] = by_agent.get(i["owner_agent_id"], 0) + n

    # Agent-active time: each dispatch from its `invocation_started` to the next validation
    # event on the same phase. Wall clock is the run's own created/updated span.
    active = 0.0
    per_phase = []
    for i in states:
        sid = i["state_id"]
        starts = [e["timestamp"] for e in invocations if e["state_id"] == sid]
        ends = [e["timestamp"] for e in validations if e["state_id"] == sid]
        spans = []
        for s in starts:
            end = next((t for t in ends if t > s), None)
            sec = _seconds(s, end)
            if sec is not None:
                spans.append(sec)
                active += sec
        env_path = run_dir / "states" / sid / "invocation-envelope.json"
        ledger_path = run_dir / "states" / sid / "state-ledger.json"
        budget = None
        skills = None
        members = []
        phase_tier = None
        if env_path.exists():
            env = json.loads(env_path.read_text(encoding="utf-8"))
            phase_tier = (env.get("model_tier") or {}).get("tier")
            members = [(m["path"], m.get("read") or (read_hint(m["path"], sid) if read_hint
                                                     else "required"))
                       for m in (env.get("context_slice") or {}).get("members") or []]
            tc_bytes = _size(run_dir / "task-context.yaml") if env.get("task_context") else 0
            budget = estimate_dispatch_budget(env, claude_root, upstream_sections, tc_bytes,
                                              read_hint)
            sd = env.get("skill_dispatch")
            if sd is not None:
                skills = {"required": sum(1 for s in sd if s["status"] == "required"),
                          "not_triggered": sum(1 for s in sd if s["status"] == "not-triggered")}
            else:
                skills = {"required": len((env.get("capability_bindings") or {})
                                          .get("phase_skills") or []),
                          "not_triggered": 0}
        if ledger_path.exists() and budget is None:
            ledger = json.loads(ledger_path.read_text(encoding="utf-8"))
            budget = ledger.get("context_budget")
        # Lessons from earlier runs shown to each dispatch of this phase, read from the
        # `lessons_shown` detail of its `invocation_started` events; an event without the key
        # (every dispatch of a run planned before runtime 0.10.0) counts zero.
        hydrated = [{"invocation_id": (e.get("details_ref") or {}).get("invocation_id"),
                     "lessons": len((e.get("details_ref") or {}).get("lessons_shown") or [])}
                    for e in invocations if e["state_id"] == sid]
        per_phase.append({
            "phase": sid, "agent": i["owner_agent_id"], "status": i["status"],
            "dispatches": len(starts), "attempts_charged": i.get("attempt", 0),
            "agent_active_seconds": round(sum(spans), 1) if spans else None,
            "context_members_declared": len(members),
            "context_members_required": sum(1 for _, r in members if r == "required"),
            "skills": skills,
            "model_tier": phase_tier,
            "context_estimate": budget,
            "lessons_hydrated": sum(h["lessons"] for h in hydrated),
            "lessons_hydrated_by_invocation": hydrated,
        })

    # Repeated context loads: a slice member declared for more than one dispatch is a file
    # the run asked more than one agent to read.
    seen: dict = {}
    for p in per_phase:
        env_path = run_dir / "states" / p["phase"] / "invocation-envelope.json"
        if not env_path.exists():
            continue
        env = json.loads(env_path.read_text(encoding="utf-8"))
        for m in (env.get("context_slice") or {}).get("members") or []:
            read = m.get("read") or (read_hint(m["path"], p["phase"]) if read_hint
                                     else "required")
            if read == "required":
                seen[m["path"]] = seen.get(m["path"], 0) + max(p["dispatches"], 1)
    repeated = {k: v for k, v in seen.items() if v > 1}

    legacy = sum((p["context_estimate"] or {}).get("legacy_estimate_bytes", 0)
                 for p in per_phase)
    progressive = sum((p["context_estimate"] or {}).get("progressive_estimate_bytes", 0)
                      for p in per_phase)
    gates = list((data.get("gates") or {}).values())
    tc = run_dir / "task-context.yaml"
    groups = []
    if tc.exists():
        groups = (yaml.safe_load(tc.read_text(encoding="utf-8")) or {}).get("parallel_groups") or []

    last = events[-1]["timestamp"] if events else data.get("updated_at")
    return {
        "schema": SCHEMA,
        "task_id": data["run_id"],
        "workflow": f"{data['workflow_id']} v{data['workflow_version']}",
        "runtime_version": data.get("runtime_version"),
        "computed_at": now or datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "total_duration": {
            "wall_clock_seconds": _seconds(data.get("created_at"), last),
            "wall_clock": _fmt(_seconds(data.get("created_at"), last)),
            "agent_active_seconds": round(active, 1),
            "agent_active": _fmt(active),
        },
        "phases_executed": {
            "declared": len(states),
            "completed": sum(1 for i in states if i["status"] == "completed"),
            "blocked": sum(1 for i in states if i["status"] == "blocked"),
        },
        "agents_executed": {
            "invocations": len(invocations),
            "distinct": len(by_agent),
            "by_agent": by_agent,
        },
        "skills_executed": {
            "required_reads": sum((p["skills"] or {}).get("required", 0) for p in per_phase),
            "not_triggered": sum((p["skills"] or {}).get("not_triggered", 0) for p in per_phase),
        },
        "parallel_groups": {"count": len(groups), "max_width": max((len(g) for g in groups),
                                                                   default=0),
                            "groups": groups},
        "context_loads": {
            "members_declared": sum(p["context_members_declared"] for p in per_phase),
            "members_required": sum(p["context_members_required"] for p in per_phase),
        },
        "repeated_context_loads": {"files": len(repeated), "by_path": repeated},
        "validation_runs": {
            "passed": sum(1 for e in validations if e["event_type"] == "validation_passed"),
            "failed": sum(1 for e in validations if e["event_type"] == "validation_failed"),
        },
        "gate_decisions": {
            "decided": sum(1 for g in gates if g.get("decision")),
            "auto_policy": sum(1 for g in gates if g.get("auto_policy")),
            "rejected": sum(1 for g in gates if g.get("decision") == "rejected")
            + sum(len(g.get("decision_history") or []) for g in gates),
        },
        "tokens_or_context_estimate": {
            "basis": f"{BYTES_PER_TOKEN} bytes per token; bytes the dispatch asked the agent "
                     f"to read, per envelope",
            "legacy_bytes": legacy,
            "progressive_bytes": progressive,
            "legacy_tokens": legacy // BYTES_PER_TOKEN,
            "progressive_tokens": progressive // BYTES_PER_TOKEN,
            "savings_pct": round(100 * (1 - progressive / legacy), 1) if legacy else 0.0,
        },
        "model_tiers": tier_summary(events, per_phase),
        "lessons_hydrated_total": sum(p["lessons_hydrated"] for p in per_phase),
        "per_phase": per_phase,
    }


def write(run_dir: Path, data: dict) -> Path:
    path = run_dir / FILENAME
    path.write_text(json.dumps(data, indent=2), encoding="utf-8")
    return path


def summary_lines(data: dict) -> list:
    """Compact rendering for the final report and the `metrics` subcommand."""
    est = data["tokens_or_context_estimate"]
    lines = [
        "| Metric | Value |", "|---|---|",
        f"| wall clock | {data['total_duration']['wall_clock']} |",
        f"| agent-active time | {data['total_duration']['agent_active']} |",
        f"| phases completed / declared | {data['phases_executed']['completed']} / "
        f"{data['phases_executed']['declared']} |",
        f"| agent invocations (distinct agents) | {data['agents_executed']['invocations']} "
        f"({data['agents_executed']['distinct']}) |",
        f"| skill reads required / not triggered | {data['skills_executed']['required_reads']} / "
        f"{data['skills_executed']['not_triggered']} |",
        f"| context members required / declared | {data['context_loads']['members_required']} / "
        f"{data['context_loads']['members_declared']} |",
        f"| files read by more than one dispatch | {data['repeated_context_loads']['files']} |",
        f"| validation runs passed / failed | {data['validation_runs']['passed']} / "
        f"{data['validation_runs']['failed']} |",
        f"| gate decisions (auto) | {data['gate_decisions']['decided']} "
        f"({data['gate_decisions']['auto_policy']}) |",
        f"| parallel groups (max width) | {data['parallel_groups']['count']} "
        f"({data['parallel_groups']['max_width']}) |",
        f"| context estimate, legacy rules | ~{est['legacy_tokens']:,} tokens |",
        f"| context estimate, progressive rules | ~{est['progressive_tokens']:,} tokens "
        f"({est['savings_pct']}% less) |",
    ]
    mt = data.get("model_tiers")
    if mt:
        share = mt["non_deep_share"]
        lines.append("| invocations by tier (light / standard / deep / untiered) | "
                     + " / ".join(str(mt["by_tier"][t]["invocations"])
                                  for t in TIERS + (UNTIERED,))
                     + (f"; non-deep share {share:.0%}" if share is not None else "") + " |")
    return lines
