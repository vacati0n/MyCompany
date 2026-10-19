#!/usr/bin/env python3
"""Executable validation for multi-phase orchestration.

`verify_vertical_slice.py` proves that one phase really executed. This script proves the
claim this slice makes instead: that a run is a **state machine over many phases**, that
its transitions are legal and ordered, that gates hold successors, and that re-running the
same request does not duplicate any side effect.

Nothing here is asserted from a specification document. Every check reads live run state:
`state.json`, `events.jsonl`, the per-phase ledgers, and the artifacts themselves. The
final check is not a read at all -- it re-executes the runtime against the same request and
proves that nothing changed.

That re-execution runs against a disposable copy, never against the run itself. The runtime's
replay path persists bookkeeping by design -- refreshed stamps, re-rendered guard details,
appended replay records, a rebuilt `task-context.yaml` -- so replaying in place would rewrite
committed run evidence while proving it unchanged. `M14` copies the working tree (git's view of
it, leaving out every other run directory, `__pycache__`, and nested worktrees), runs the copied
runtime with the copy as its root, fingerprints the copy before and after, and removes the copy in
a `finally` block. The verdict is the one it always was; what the copy absorbs is reported by file.

Usage:
    python .claude/runtime/verify_multi_phase.py [--run-id run-xxxxxxxxxxxx]
    python .claude/runtime/verify_multi_phase.py --no-replay      # skip the live re-run
    python .claude/runtime/verify_multi_phase.py --json-out F

Exit code 0 only when every check passes.
"""

from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
CLAUDE = HERE.parent
RUNS = CLAUDE / "runs"
sys.path.insert(0, str(HERE))

import framework_runtime as fr   # noqa: E402
import state_engine as se        # noqa: E402
import verify_self_hosting as vsh  # noqa: E402

# The exit criterion for this slice: a run must actually traverse the chain that ends in its
# delivery phase -- the delivery phase itself and the two hard predecessors that feed it. A
# delivery phase whose owner agent has no registered capability is expected to be `blocked`,
# not skipped and not silently absent.
#
# The chain is read from the routed workflow's Phase Model, never named here. Before runtime
# 0.7.1 this table pinned implement-feature's phase identifiers (`execution-planning`,
# `solution-design-and-risk-assessment`, `implementation`), so M6 failed on every other
# workflow whether or not the run traversed its own chain. `required_phases` derives the same
# three roles for implement-feature and the corresponding ones for every other workflow.
DELIVERY_CAPABILITY = "implementation-delivery"
ROLE_LABELS = ("planning", "design", "delivery")


def required_phases(rows: list) -> dict:
    """Role label -> phase identifier, derived from the routed Phase Model.

    `delivery` is the phase whose owner agent holds the `implementation-delivery` capability in
    `registry/agents.yaml`; a workflow with no such phase (review, investigate, research,
    release, quality scan) delivers its last phase. `design` is the delivery phase's nearest
    hard predecessor and `planning` is that phase's nearest hard predecessor, both read from
    the dependency graph the runtime derives from the Phase Model with no supplied inputs, so
    every edge is hard. A workflow shorter than the chain omits the roles it does not have.
    """
    if not rows:
        return {}
    order = [r["phase"] for r in rows]
    capabilities = {rec["identifier"]: set(rec.get("capabilities") or [])
                    for rec in fr.load_yaml("registry/agents.yaml").get("records") or []}
    delivery = next((r["phase"] for r in rows
                     if DELIVERY_CAPABILITY in capabilities.get(r.get("owner agent"), set())),
                    order[-1])
    deps = fr.derive_dependencies(rows, [])

    def nearest_hard_predecessor(phase: str):
        hard = [d["state_id"] for d in deps.get(phase, []) if d["kind"] == "hard"]
        return max(hard, key=order.index) if hard else None

    chain = [delivery]
    while len(chain) < len(ROLE_LABELS):
        up = nearest_hard_predecessor(chain[-1])
        if up is None:
            break
        chain.append(up)
    chain.reverse()
    return dict(zip(ROLE_LABELS[len(ROLE_LABELS) - len(chain):], chain))

class Result:
    def __init__(self):
        self.checks = []

    def add(self, cid, title, ok, detail):
        self.checks.append({"id": cid, "check": title,
                            "result": "PASS" if ok else "FAIL", "detail": detail})
        return ok

    @property
    def passed(self):
        return all(c["result"] == "PASS" for c in self.checks)


def latest_state_run() -> Path | None:
    cands = [p for p in RUNS.glob("run-*") if (p / se.StateStore.FILENAME).exists()]
    if not cands:
        return None
    return max(cands, key=lambda p: (p / se.StateStore.FILENAME).stat().st_mtime)


def events_of(run_dir: Path) -> list:
    p = run_dir / "events.jsonl"
    if not p.exists():
        return []
    return [json.loads(l) for l in p.read_text(encoding="utf-8").splitlines() if l.strip()]


def fingerprint(run_dir: Path, store, claude: Path = CLAUDE) -> dict:
    """Everything a second execution of the same request must leave untouched.

    Artifact paths are run-relative to a framework root; `claude` is the root they resolve
    against, which for `M14` is the disposable copy the replay ran in, never the original.
    """
    arts = {}
    for i in store.state_items():
        c = i.get("completion") or {}
        if c.get("artifact_path"):
            f = claude / c["artifact_path"]
            arts[i["state_id"]] = fr.sha256_text(f.read_text(encoding="utf-8")) \
                if f.exists() else None
    return {
        "run_id": store.data["run_id"],
        "events": len(events_of(run_dir)),
        "transitions": len(store.data["transitions"]),
        "statuses": {i["work_item_id"]: i["status"] for i in store.ordered_items()},
        "idempotency_keys": {i["work_item_id"]: i["idempotency_key"]
                             for i in store.state_items()},
        "artifact_digests": arts,
    }


def carried_at(run_dir: Path) -> str | None:
    """The instant this run was last carried forward by its own execution.

    `GD-001` (`config/self-hosting-profile.md#point-in-time-evidence-rule`) makes a completed
    run a point-in-time record: it is judged against the baseline it recorded, never against
    what the framework has since become. The run ledger is that baseline. It is written when
    the run progresses and is not rewritten by the scheduler afterwards, which is precisely
    what distinguishes it from `state.json` -- the scheduler clears a guard-raised block as
    soon as the guard passes, so a phase that was blocked when the run was carried can read
    `pending` today without the run having changed at all.

    Returns `None` when no ledger exists, which leaves the caller evaluating current state, the
    behaviour every in-flight run should keep.
    """
    for path, key in ((run_dir / "run-ledger.json", "updated_at"),):
        if path.exists():
            at = json.loads(path.read_text(encoding="utf-8")).get(key)
            if at:
                return str(at)
    agg = (json.loads((run_dir / "state.json").read_text(encoding="utf-8"))
           .get("last_aggregation") or {}).get("at")
    return str(agg) if agg else None


def run_checks(run_dir: Path, replay: bool) -> Result:
    r = Result()
    store = se.StateStore.load(run_dir)
    if store is None:
        r.add("M0", "the run carries persisted state-engine state", False,
              f"{run_dir}/state.json not found")
        return r
    r.add("M0", "the run carries persisted state-engine state", True,
          f"{run_dir.name}/state.json, schema {store.data['schema']}, "
          f"run_status={store.data['run_status']}")

    events = events_of(run_dir)
    items = store.ordered_items()
    state_items = store.state_items()
    transitions = store.data["transitions"]

    # ----------------------------------------------------------------- M1 coverage
    wf = fr.resolve_workflow(store.data["workflow_id"])
    rows = fr.parse_phase_model(wf["specificationPath"])
    declared_phases = [row["phase"] for row in rows]
    declared_gates = sorted({g for row in rows for g in fr.phase_gates(row)})
    have_phases = [i["state_id"] for i in state_items]
    have_gates = sorted(i["state_id"] for i in store.ordered_items("gate"))
    ok = have_phases == declared_phases and have_gates == declared_gates
    r.add("M1", "the run holds one work item per declared phase and per declared gate", ok,
          f"{len(have_phases)} phase item(s) matching the Phase Model row order, "
          f"{len(have_gates)} gate item(s); "
          + ("complete" if ok else f"phases={have_phases} gates={have_gates}"))

    # ----------------------------------------------------------------- M2 vocabulary
    bad_status = [i["work_item_id"] for i in items if i["status"] not in se.STATUSES]
    bad_queue = [i["work_item_id"] for i in items
                 if i["queue_status"] != se.queue_status(i)]
    ok = not bad_status and not bad_queue
    r.add("M2", "every work item holds one of the seven persisted statuses, projected onto "
                "the canonical queue vocabulary", ok,
          f"statuses in use: {sorted({i['status'] for i in items})}; queue projections: "
          f"{sorted({i['queue_status'] for i in items})}"
          + ("" if ok else f"; invalid status={bad_status} projection drift={bad_queue}"))

    # ----------------------------------------------------------------- M3 legality
    illegal = []
    for t in transitions:
        table = se.TRANSITIONS[t["work_type"]]
        if (t["from"], t["to"]) not in table:
            illegal.append(f"seq {t['seq']}: {t['from']}->{t['to']} ({t['work_type']})")
        elif table[(t["from"], t["to"])] != t["trigger"]:
            illegal.append(f"seq {t['seq']}: trigger {t['trigger']!r} does not match table")
    non_canonical = [t["seq"] for t in transitions if t["reason_code"] not in se.REASON_CODES]
    ok = not illegal and not non_canonical
    r.add("M3", "every recorded transition is legal for its work type and carries a "
                "canonical reason code", ok,
          f"{len(transitions)} transition(s) replayed against the transition table"
          + ("" if ok else f"; illegal={illegal} non-canonical reason codes={non_canonical}"))

    # ----------------------------------------------------------------- M4 ordering
    seqs = [t["seq"] for t in transitions]
    ordered = seqs == list(range(1, len(seqs) + 1))
    times = [t["at"] for t in transitions]
    monotonic = all(a <= b for a, b in zip(times, times[1:]))
    # A terminal status is left only by the two authorised re-entry pairs the state engine
    # names in `AUTHORISED_EXITS`, each recorded under its own trigger. A `superseded` exit
    # must also have left a supersession record on the item, or the engine was bypassed.
    escaped, authorised = [], []
    seen_terminal = {}
    by_wid = {i["work_item_id"]: i for i in items}
    for t in transitions:
        if t["work_item_id"] in seen_terminal:
            exits = se.AUTHORISED_EXITS.get(t["work_type"], {})
            if exits.get((t["from"], t["to"])) == t["trigger"]:
                authorised.append(f"seq {t['seq']}: {t['state_id']} {t['from']}->{t['to']} "
                                  f"({t['trigger']})")
                if t["trigger"] == "superseded" and not (
                        by_wid.get(t["work_item_id"], {}).get("supersessions")):
                    escaped.append(f"seq {t['seq']} superseded {t['work_item_id']} without a "
                                   f"supersession record on the item")
            else:
                escaped.append(f"seq {t['seq']} moved {t['work_item_id']} out of "
                               f"{seen_terminal[t['work_item_id']]}")
            seen_terminal.pop(t["work_item_id"], None)
        if t["to"] in se.TERMINAL_STATUSES:
            seen_terminal[t["work_item_id"]] = t["to"]
    ok = ordered and monotonic and not escaped
    r.add("M4", "the transition log is strictly ordered and no work item leaves a terminal "
                "status except under an authorised rollback", ok,
          f"seq 1..{len(seqs)} contiguous={ordered}, timestamps non-decreasing={monotonic}, "
          f"terminal escapes={escaped or 'none'}, authorised re-entries="
          f"{authorised or 'none'}")

    # ----------------------------------------------------------------- M5 lease integrity
    # A phase may block, lose a lease, and be reclaimed any number of times; M3 already
    # proves each individual step is legal. What must hold regardless of the route taken is
    # that a phase never reaches `completed` without having been leased and invoked -- that
    # is, no result can be accepted for work the gateway never dispatched.
    problems, routes = [], []
    for i in state_items:
        if i["status"] != se.COMPLETED:
            continue
        steps = [(t["from"], t["to"]) for t in transitions
                 if t["work_item_id"] == i["work_item_id"]]
        routes.append(f"{i['state_id']}: " + " -> ".join(
            [steps[0][0]] + [b for _, b in steps]) + f" ({len(steps)} transitions)")
        if steps[-1] != (se.RUNNING, se.COMPLETED):
            problems.append(f"{i['state_id']} completed from {steps[-1][0]}, not running")
        if (se.PENDING, se.LEASED) not in steps:
            problems.append(f"{i['state_id']} completed without ever being leased")
        if (se.LEASED, se.RUNNING) not in steps:
            problems.append(f"{i['state_id']} completed without an invocation")
    ok = not problems
    r.add("M5", "no phase reached completed without being leased and invoked", ok,
          "; ".join(routes) or "no completed phase" if ok else f"violations: {problems}")

    # ----------------------------------------------------------------- M6 multi-phase
    # What this check asks is what the run *did*, which is a fact about the past. Reading it
    # from current `state.json` made it a question about the present, and the two answers
    # parted company the moment capability grew: the scheduler returns a blocked phase to
    # `pending` as soon as its guard passes, so a run that correctly recorded a blocked
    # delivery phase began failing a check it had passed, without the run changing. `GD-001`
    # settles this -- governance evidence is evaluated against the baseline it recorded -- so
    # the run's append-only transition log is replayed to the instant the run was last carried
    # forward, reusing the same reconstruction `verify_self_hosting.completion_rule` applies.
    # Divergence since is reported as informational drift, never failed on.
    as_of = carried_at(run_dir)
    by_id = {i["state_id"]: i for i in state_items}
    recorded = vsh._phase_status_as_of(run_dir, by_id, as_of)

    def status_then(phase):
        v = recorded.get(phase)
        if v is None:
            return "absent"
        return str(v.get("status") or (by_id.get(phase) or {}).get("status") or "absent")

    completed = [p for p in by_id if status_then(p) == se.COMPLETED]
    roles = required_phases(rows)
    reached = {label: status_then(phase) for label, phase in roles.items()}
    ok = (len(completed) >= min(2, len(roles))
          and all(reached[label] == se.COMPLETED for label in roles if label != "delivery")
          and reached.get("delivery") in (se.COMPLETED, se.BLOCKED))
    drift = sorted(f"{p}: recorded {status_then(p)}, now {by_id[p]['status']}"
                   for p in by_id if status_then(p) != by_id[p]["status"])
    r.add("M6", "the run traversed the chain that ends in its delivery phase, as the routed "
                "Phase Model declares it", ok,
          ", ".join(f"{k}={roles[k]}:{v}" for k, v in reached.items())
          + f"; {len(completed)} phase(s) completed"
          + (f"; as of {as_of}" if as_of else "; as of current state, no ledger baseline")
          + ("; no drift since" if not drift
             else f"; drift since (informational, GD-001): {drift}"))

    # ----------------------------------------------------------------- M7 phase order
    order = {p: n for n, p in enumerate(declared_phases)}
    commit = {t["state_id"]: t["seq"] for t in transitions
              if t["work_type"] == "state" and t["to"] == se.COMPLETED}
    lease = {t["state_id"]: t["seq"] for t in transitions
             if t["work_type"] == "state" and t["to"] == se.LEASED}
    violations = []
    for i in state_items:
        for dep in i["depends_on"]:
            if dep["kind"] != "hard" or i["state_id"] not in lease:
                continue
            up = dep["state_id"]
            if up not in commit:
                violations.append(f"{i['state_id']} leased while {up} had not completed")
            elif commit[up] > lease[i["state_id"]]:
                violations.append(f"{i['state_id']} leased at seq {lease[i['state_id']]} "
                                  f"before {up} completed at seq {commit[up]}")
    ok = not violations
    r.add("M7", "no phase was leased before its hard predecessors had committed", ok,
          f"lease order {sorted(lease.items(), key=lambda kv: kv[1])} against commit order "
          f"{sorted(commit.items(), key=lambda kv: kv[1])}"
          if ok else f"ordering violations: {violations}")

    # ----------------------------------------------------------------- M8 gate guard
    gate_seq = {t["state_id"]: t["seq"] for t in transitions
                if t["work_type"] == "gate" and t["to"] == se.COMPLETED}
    gate_violations = []
    for i in state_items:
        if i["state_id"] not in lease:
            continue
        for dep in i["depends_on"]:
            if dep["kind"] != "hard":
                continue
            for g in store.gates_for(dep["state_id"]):
                if g["decision"] != "approved":
                    gate_violations.append(
                        f"{i['state_id']} was leased while {g['gate']} was "
                        f"{g['decision'] or 'undecided'}")
                elif gate_seq.get(g["gate"], 10 ** 9) > lease[i["state_id"]]:
                    gate_violations.append(
                        f"{i['state_id']} leased at seq {lease[i['state_id']]} before "
                        f"{g['gate']} was decided at seq {gate_seq.get(g['gate'])}")
                elif g["owner_role"] in fr.producer_aliases(g["producer_agent"]):
                    gate_violations.append(
                        f"{g['gate']} was approved by {g['owner_role']}, which names the "
                        f"producing agent {g['producer_agent']}")
    decided = {g["gate"]: g["decision"] for g in store.data["gates"].values() if g["decision"]}
    ok = not gate_violations
    r.add("M8", "every gate closing a hard predecessor was decided, by a non-producing "
                "owner, before its successor was leased", ok,
          f"decisions recorded: {decided or 'none'}"
          if ok else f"gate violations: {gate_violations}")

    # ----------------------------------------------------------------- M9 idempotency keys
    drift, dupes = [], []
    seen = {}
    for i in state_items:
        if not i["idempotency_key"]:
            continue
        expect = se.idempotency_key(i["run_id"], i["state_id"], i["owner_agent_id"],
                                    _agent_version(run_dir, i), i["payload_digest"])
        if expect != i["idempotency_key"]:
            drift.append(f"{i['state_id']}: stored {i['idempotency_key']} != derived {expect}")
        if i["idempotency_key"] in seen:
            dupes.append(f"{i['state_id']} shares a key with {seen[i['idempotency_key']]}")
        seen[i["idempotency_key"]] = i["state_id"]
    ok = not drift and not dupes
    r.add("M9", "each dispatched work item carries a derivable, unique idempotency key", ok,
          f"{len(seen)} key(s), each recomputed from the persisted run, phase, agent, and "
          f"payload digest" if ok else f"drift={drift} duplicates={dupes}")

    # ----------------------------------------------------------------- M10 event integrity
    ids = [e["event_id"] for e in events]
    non_canon = sorted({e["event_type"] for e in events} - fr.CANONICAL_EVENTS)
    ts = [e["timestamp"] for e in events]
    ok = (len(set(ids)) == len(ids) and not non_canon
          and all(a <= b for a, b in zip(ts, ts[1:])))
    r.add("M10", "the event stream is append-only, unique, ordered, and canonical", ok,
          f"{len(events)} event(s), {len(set(e['event_type'] for e in events))} distinct "
          f"canonical type(s)"
          + ("" if ok else f"; duplicates={len(ids) - len(set(ids))} "
                           f"non-canonical={non_canon}"))

    # ----------------------------------------------------------------- M11 blocked reasons
    blocked = [i for i in items if i["status"] == se.BLOCKED]
    escalated = {e["work_item_id"] for e in events if e["event_type"] == "escalation_opened"}
    silent = [i["work_item_id"] for i in blocked
              if not i["blocked_reason"] or i["work_item_id"] not in escalated]
    ok = not silent
    r.add("M11", "every blocked work item records a reason and opened an escalation", ok,
          "; ".join(f"{i['state_id']}={i['blocked_reason']}" for i in blocked) or "none blocked"
          if ok else f"blocked without reason or escalation: {silent}")

    # ----------------------------------------------------------------- M12 evidence
    # Every completion block the run ever committed: the current one on each item, and every
    # one an authorised rollback moved into `supersessions`. A superseded attempt's artifact is
    # immutable evidence exactly as a current one is; the next attempt writes elsewhere.
    missing, committed = [], []
    for i in state_items:
        blocks = [("current", i.get("completion"))] + [
            (f"attempt {sup.get('attempt')} superseded under {sup.get('authorisation_id')}",
             sup.get("completion")) for sup in (i.get("supersessions") or [])]
        for label, c in blocks:
            if not c:
                continue
            f = CLAUDE / c["artifact_path"]
            committed.append(f"{i['state_id']} [{label}] -> {c['artifact_digest']}")
            if not f.exists():
                missing.append(f"{i['state_id']} [{label}]: artifact absent")
            elif fr.sha256_text(f.read_text(encoding="utf-8")) != c["artifact_digest"]:
                missing.append(f"{i['state_id']} [{label}]: artifact digest changed since "
                               f"commit")
    ok = not missing
    r.add("M12", "every committed artifact, current or superseded, is present and unchanged "
                 "since it was committed", ok,
          "; ".join(committed) or "no committed artifact" if ok else f"{missing}")

    # ----------------------------------------------------------------- M13 negative paths
    # The checks above prove that what was recorded is legal. This one proves the state
    # machine actually refuses what is not, which no successful run can demonstrate.
    refused, allowed = [], []
    tmp = Path(tempfile.mkdtemp(prefix="state-engine-check-"))
    try:
        probe = se.StateStore.create(
            tmp, run_id="run-probe", command_id="implement",
            workflow_id="implement-feature", workflow_version="1.0.0",
            runtime_version=fr.RUNTIME_VERSION, input_digest="sha256:probe", inputs=[])
        st = probe.add_item(se.new_work_item(
            run_id="run-probe", workflow_id="implement-feature", state_id="p",
            work_type="state", owner_agent_id="planner", phase_index=1, gate=None,
            artifact="execution-plan.md", depends_on=[]))
        gt = probe.add_item(se.new_work_item(
            run_id="run-probe", workflow_id="implement-feature", state_id="G",
            work_type="gate", owner_agent_id=None, phase_index=1, gate="G",
            artifact=None, depends_on=[]))

        def refuses(label, fn):
            try:
                fn()
                allowed.append(label)
            except se.TransitionError:
                refused.append(label)

        common = dict(actor_type="runtime", actor_id="probe", detail="probe")
        refuses("state pending -> completed (skipping lease and invocation)",
                lambda: probe.transition(st, se.COMPLETED, reason_code="output_accepted",
                                         **common))
        refuses("gate pending -> leased (a gate is never leased)",
                lambda: probe.transition(gt, se.LEASED, reason_code="leased", **common))
        refuses("non-canonical reason code",
                lambda: probe.transition(st, se.LEASED, reason_code="looks_fine", **common))
        probe.transition(st, se.LEASED, reason_code="leased", **common)
        probe.transition(st, se.RUNNING, reason_code="execution_started", **common)
        probe.transition(st, se.COMPLETED, reason_code="output_accepted", **common)
        refuses("completed -> running (a terminal status is terminal)",
                lambda: probe.transition(st, se.RUNNING, reason_code="execution_started",
                                         **common))
        probe.bind_payload(st, owner_agent_id="planner", agent_version="1.0.0",
                           payload_digest="sha256:one")
        refuses("rebinding a completed work item to a different payload",
                lambda: probe.bind_payload(st, owner_agent_id="planner",
                                           agent_version="1.0.0",
                                           payload_digest="sha256:two"))

        # Terminality, exhaustively. Every (from, to) pair out of `completed` and `failed`
        # is probed for both work types; exactly the two authorised re-entry pairs may pass,
        # and the state pair only when a supersession record accompanies it. Four probe
        # items: a completed state (st, above), a failed state, a failed gate, and a
        # completed gate.
        st_failed = probe.add_item(se.new_work_item(
            run_id="run-probe", workflow_id="implement-feature", state_id="pf",
            work_type="state", owner_agent_id="planner", phase_index=2, gate=None,
            artifact="execution-plan.md", depends_on=[]))
        probe.transition(st_failed, se.LEASED, reason_code="leased", **common)
        probe.transition(st_failed, se.RETRYING, reason_code="tool_failure", **common)
        probe.transition(st_failed, se.FAILED, reason_code="terminal_failure", **common)
        probe.transition(gt, se.FAILED, reason_code="validation_failed", **common)
        gt_done = probe.add_item(se.new_work_item(
            run_id="run-probe", workflow_id="implement-feature", state_id="GD",
            work_type="gate", owner_agent_id=None, phase_index=2, gate="GD",
            artifact=None, depends_on=[]))
        probe.transition(gt_done, se.COMPLETED, reason_code="output_accepted", **common)

        expected_exits = {("state", se.COMPLETED, se.PENDING), ("gate", se.FAILED, se.PENDING)}
        passed_pairs, wrong = [], []
        for label, probe_item in (("state completed", st), ("state failed", st_failed),
                                  ("gate failed", gt), ("gate completed", gt_done)):
            frm = probe_item["status"]
            for to in se.STATUSES:
                key = (probe_item["work_type"], frm, to)
                fields = ({"supersession": {"authorisation_id": "RB-probe-01"}}
                          if key == ("state", se.COMPLETED, se.PENDING) else None)
                snapshot = json.dumps(probe_item, sort_keys=True)
                try:
                    probe.transition(probe_item, to, reason_code="enqueued", fields=fields,
                                     **common)
                    outcome = "allowed"
                except se.TransitionError:
                    outcome = "refused"
                if key in expected_exits:
                    if outcome == "allowed":
                        passed_pairs.append(f"{label} -> {to}")
                        # restore the terminal status for the remaining probes of this item
                        probe_item.update(json.loads(snapshot))
                        probe.save()
                    else:
                        wrong.append(f"{label} -> {to} refused, but it is the authorised pair")
                elif outcome == "allowed":
                    wrong.append(f"{label} -> {to} allowed")
                    probe_item.update(json.loads(snapshot))
                    probe.save()
        refuses("completed -> pending (superseded) without a supersession record",
                lambda: probe.transition(st, se.PENDING, reason_code="enqueued", **common))
        refuses("completed -> pending (superseded) with a record lacking an authorisation_id",
                lambda: probe.transition(st, se.PENDING, reason_code="enqueued",
                                         fields={"supersession": {"gate": "G"}}, **common))
        probed = 4 * len(se.STATUSES)
        if wrong:
            allowed.extend(wrong)
        else:
            refused.append(f"{probed - len(expected_exits)} of {probed} exits from "
                           f"completed/failed refused across both work types; only "
                           f"{sorted(passed_pairs)} pass")
    finally:
        shutil.rmtree(tmp, ignore_errors=True)
    ok = not allowed and len(passed_pairs) == len(expected_exits)
    r.add("M13", "the state machine refuses illegal transitions: completed and failed are "
                 "terminal except for the two authorised re-entry pairs, and superseded is "
                 "refused without a record", ok,
          f"{len(refused)} illegal operation(s) refused: " + "; ".join(refused)
          if ok else f"permitted what it should refuse: {allowed}; authorised pairs that "
                     f"passed: {passed_pairs}")

    # ----------------------------------------------------------------- M14 live replay
    if not replay:
        r.add("M14", "re-running the same request produces no new side effect", False,
              "skipped by --no-replay")
        return r

    cmds = [["plan", "--run-id", store.data["run_id"]]]
    for i in state_items:
        if i["status"] == se.COMPLETED:
            cmds.append(["dispatch", "--run-id", store.data["run_id"],
                         "--phase", i["state_id"]])
            cmds.append(["complete", "--run-id", store.data["run_id"],
                         "--phase", i["state_id"]])
    # The replay runs in a disposable copy: the copied runtime resolves its root from its own
    # location, so every write it makes lands in the copy, which is removed whatever happens.
    scratch = vsh.scratch_tree("m14-replay-", keep_run=run_dir.name)
    try:
        claude_copy = scratch / CLAUDE.name
        copy_run = claude_copy / "runs" / run_dir.name
        if run_dir.resolve() != (RUNS / run_dir.name).resolve() or not copy_run.exists():
            # A run outside this framework's runs/ directory, or one git does not list, is
            # copied from where it is.
            vsh.remove_tree(copy_run)
            shutil.copytree(run_dir, copy_run,
                            ignore=shutil.ignore_patterns("__pycache__"))
        copy_store = se.StateStore.load(copy_run)
        before = fingerprint(copy_run, copy_store, claude_copy)
        files_before = vsh.walk_snapshot(copy_run)
        outputs = []
        for c in cmds:
            p = subprocess.run(
                [sys.executable, str(claude_copy / "runtime" / "framework_runtime.py")] + c,
                capture_output=True, text=True, cwd=str(scratch))
            outputs.append((c[0], c[-1], p.returncode))
        after_store = se.StateStore.load(copy_run)
        after = fingerprint(copy_run, after_store, claude_copy)
        rewritten = vsh.snapshot_changes(files_before, vsh.walk_snapshot(copy_run))
    finally:
        vsh.remove_tree(scratch)
    diffs = [k for k in before if before[k] != after[k]]
    failed_cmds = [o for o in outputs if o[2] != 0]
    replays = len(after_store.data.get("replays") or [])
    ok = not diffs and not failed_cmds
    r.add("M14", "re-running the same request produces no new side effect", ok,
          f"re-executed {len(cmds)} runtime command(s) against a disposable copy of the "
          f"committed run; events {before['events']} -> {after['events']}, transitions "
          f"{before['transitions']} -> {after['transitions']}, artifact digests unchanged, "
          f"{replays} replay(s) recorded and suppressed; bookkeeping rewritten in the copy: "
          f"{', '.join(rewritten) or 'none'}"
          if ok else f"changed: {diffs}; non-zero exits: {failed_cmds}; replayed on a "
                     f"disposable copy, files rewritten there: {', '.join(rewritten) or 'none'}")
    return r


def _agent_version(run_dir: Path, item: dict) -> str:
    """Read the agent version this work item was bound to, from its per-phase ledger."""
    p = run_dir / "states" / item["state_id"] / "state-ledger.json"
    if not p.exists():
        return ""
    return json.loads(p.read_text(encoding="utf-8")).get("agent_version", "")


def main():
    ap = argparse.ArgumentParser(description="Verify multi-phase orchestration")
    ap.add_argument("--run-id")
    ap.add_argument("--no-replay", action="store_true",
                    help="skip the live re-execution check")
    ap.add_argument("--json-out")
    a = ap.parse_args()

    run_dir = (RUNS / a.run_id) if a.run_id else latest_state_run()
    if run_dir is None or not run_dir.exists():
        print("no run with persisted state-engine state found under .claude/runs/")
        return 1

    res = run_checks(run_dir, replay=not a.no_replay)

    print(f"Multi-Phase Orchestration Validation: {run_dir.name}")
    print("-" * 100)
    for c in res.checks:
        print(f"[{c['result']}] {c['id']:<4} {c['check']}")
        print(f"        {c['detail']}")
    print("-" * 100)
    n_pass = sum(1 for c in res.checks if c["result"] == "PASS")
    verdict = "PROVEN" if res.passed else "NOT PROVEN"
    print(f"{n_pass}/{len(res.checks)} checks passed -- {verdict}")

    if a.json_out:
        Path(a.json_out).write_text(json.dumps({
            "run": run_dir.name, "verdict": verdict, "passed": n_pass,
            "total": len(res.checks), "checks": res.checks,
        }, indent=2), encoding="utf-8")
    return 0 if res.passed else 1


if __name__ == "__main__":
    sys.exit(main())
