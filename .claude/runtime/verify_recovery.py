#!/usr/bin/env python3
"""Executable proof that the runtime recovers from classified failures.

`verify_vertical_slice.py` proves a phase can execute. `verify_multi_phase.py` proves a run is
a state machine. Neither proves anything about the paths a run takes when something goes wrong,
because a successful run never takes them. This script induces the failures instead.

Four runs, each injecting one fault, driven through the real command line rather than by
calling functions -- what is proved is the runtime an operator uses:

  Run A  a rejected artifact, repaired
         attempt 1 returns a structurally invalid plan. The Validation Engine rejects it, the
         Recovery Controller classifies `output-schema-failure` as retryable, and the work item
         moves to `retrying` with a deadline. Once the backoff elapses the scheduler promotes
         it, the re-dispatch carries the rejection back to the agent as a repair pass, and
         attempt 2 returns a conforming plan that is accepted.

  Run B  a rejected artifact, never repaired
         every attempt returns the same invalid plan. Attempts are charged, the budget bounds
         them at three, and the fourth is refused rather than granted: the work item blocks
         with a recorded reason and an open envelope, and `release` refuses to grant an attempt
         the budget does not have.

  Run C  a policy violation
         the artifact is valid, but the agent declares a write outside its permitted set. That
         is not a transient fault, so it is classified `policy-failure` and never scheduled for
         a retry. This is the negative case: it proves the policy discriminates rather than
         retrying whatever fails.

  Run D  a gate rejection, rolled back and rebuilt
         attempt 1 is accepted and its gate is rejected by a listed non-producing owner. The
         rejection is classified `gate-rejection` with action rollback; its envelope names the
         `rollback` command, which this run executes verbatim. The phase is superseded --
         re-entered as attempt 2 under its existing idempotency key, attempt 1's artifact
         untouched at its committed path -- and the gate re-armed on the same work item with
         the rejection in its `decision_history`. The stale successor block clears, the
         re-dispatch carries the rejection as `prior_rejection`, attempt 2 commits at an
         attempt-scoped path, the gate blocks again for a fresh human decision, and the
         negatives -- producer role, unlisted role, approved sibling gate on the closed
         phase, invalid target, exhausted
         budget -- are each refused without touching the store.

The harness stands in for the agent adapter and nothing else. It writes the artifact and the
result envelope the adapter would have written, then hands control back to the runtime. Every
transition, classification, and envelope this script asserts on is produced by the runtime.

Each run is keyed by its own input file, so its `run_id` is derived, not assigned. A previous
execution's run directory is removed before it is re-planned, and only ever the directories
these four inputs derive.

    python .claude/runtime/verify_recovery.py            # run all four
    python .claude/runtime/verify_recovery.py --keep     # leave the run directories in place
"""

from __future__ import annotations

import argparse
import json
import re
import shlex
import shutil
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import framework_runtime as fr  # noqa: E402
import recovery_policy as rp    # noqa: E402
import state_engine as se       # noqa: E402
import verify_multi_phase as vmp  # noqa: E402

CLAUDE = fr.CLAUDE
RUNS = fr.RUNS
INPUTS = RUNS / "inputs"
PY = sys.executable
RUNTIME = str(Path(__file__).resolve().parent / "framework_runtime.py")

# The conforming artifact every scenario starts from: a plan the runtime itself already
# accepted, in a committed run. Reusing accepted evidence rather than authoring a fixture keeps
# the proof honest -- an artifact the Validation Engine has passed in production conditions.
GOOD_SOURCE = (RUNS / "run-c5a8d50d3238/states/execution-planning/artifacts/"
               "execution-plan.md")

PHASE = "execution-planning"
GATE = "Planning Gate"
INPUT_TYPE = "feature-request"


class Result:
    def __init__(self, title):
        self.title = title
        self.checks = []

    def add(self, cid, description, ok, detail=""):
        self.checks.append({"id": cid, "description": description,
                            "result": "pass" if ok else "fail", "detail": detail})
        return ok

    @property
    def passed(self):
        return all(c["result"] == "pass" for c in self.checks)

    def render(self):
        out = ["", self.title, "-" * 100]
        for c in self.checks:
            out.append(f"[{'PASS' if c['result'] == 'pass' else 'FAIL'}] {c['id']:<5} "
                       f"{c['description']}")
            if c["detail"]:
                out.append(f"        {c['detail']}")
        return out


# --------------------------------------------------------------------------- harness plumbing


def run(*argv, expect: int | None = 0) -> tuple[int, str]:
    """Invoke the runtime command line and return (exit code, combined output)."""
    proc = subprocess.run([PY, RUNTIME, *argv], capture_output=True, text=True,
                          cwd=str(CLAUDE.parent))
    out = proc.stdout + proc.stderr
    if expect is not None and proc.returncode != expect:
        raise AssertionError(f"`{' '.join(argv)}` exited {proc.returncode}, expected "
                             f"{expect}\n{out}")
    return proc.returncode, out


def write_input(slug: str, note: str) -> Path:
    """One input file per scenario, so each derives its own run identity."""
    INPUTS.mkdir(parents=True, exist_ok=True)
    path = INPUTS / f"recovery-injection-{slug}.md"
    path.write_text(
        f"# Feature Request: Validation and Recovery Hardening ({slug})\n\n"
        f"## Intent\n\n"
        f"Improve the reliability of autonomous execution: extend validator coverage to every\n"
        f"core emitted artifact type, bound retries by a classified failure policy, and emit a\n"
        f"structured failure envelope for every blocked transition.\n\n"
        f"## Acceptance Criteria\n\n"
        f"- Every core emitted artifact type has a registered validator.\n"
        f"- A retryable failure is retried within a bounded attempt budget.\n"
        f"- A non-retryable failure is never retried.\n"
        f"- Every blocked transition emits a structured, classified failure envelope.\n\n"
        f"## Injection Note\n\n"
        f"{note}\n",
        encoding="utf-8")
    return path


def run_id_for(input_path: Path) -> str:
    supplied = [fr.read_supplied(INPUT_TYPE, str(input_path))]
    cmd = fr.resolve_command("implement")
    wf = fr.resolve_workflow(cmd["primaryWorkflow"])
    return fr.make_run_id(cmd["identifier"], wf["identifier"],
                          fr.run_input_digest(supplied))


def reset_run(run_id: str, input_path: Path):
    """Remove a run directory this harness previously created, and only such a directory."""
    d = RUNS / run_id
    if not d.exists():
        return
    state = d / "state.json"
    if state.exists():
        data = json.loads(state.read_text(encoding="utf-8"))
        refs = [i.get("reference", "") for i in data.get("inputs") or []]
        if not any("recovery-injection" in r for r in refs):
            raise AssertionError(f"refusing to remove {d}: it was not created by this "
                                 f"harness (inputs {refs})")
    shutil.rmtree(d)


def plan(input_path: Path) -> str:
    run_id = run_id_for(input_path)
    reset_run(run_id, input_path)
    run("plan", "--command", "implement", "--input", f"{INPUT_TYPE}={input_path}")
    return run_id


def state_dir(run_id: str) -> Path:
    return RUNS / run_id / "states" / PHASE


def load_state(run_id: str) -> dict:
    return json.loads((RUNS / run_id / "state.json").read_text(encoding="utf-8"))


def item_of(run_id: str) -> dict:
    return load_state(run_id)["work_items"][PHASE]


def transitions_of(run_id: str) -> list:
    return [(t["state_id"], t["from"], t["to"], t["reason_code"])
            for t in load_state(run_id)["transitions"]]


def events_of(run_id: str) -> list:
    path = RUNS / run_id / "events.jsonl"
    return [json.loads(ln) for ln in path.read_text(encoding="utf-8").splitlines() if ln]


def phase_entries(run_id: str) -> list:
    """Recovery ledger entries for the injected phase only.

    A run of `implement-feature` blocks its other five phases at `G1-CAPABILITY`, because their
    owner agents hold no registry record, and each of those blocks emits its own envelope. Those
    are correct and are asserted on separately; here the subject is the one phase that executed.
    """
    return [e for e in rp.RecoveryLedger(RUNS / run_id).entries()
            if e["state_id"] == PHASE]


def envelope_of(run_id: str) -> dict:
    return json.loads((state_dir(run_id) / "failure-envelope.json").read_text(
        encoding="utf-8"))


# --------------------------------------------------------------------------- fake adapter


def good_artifact(run_id: str) -> str:
    """The accepted plan, re-stamped with this run's frozen digests.

    The plan carries the digests of the context slice it was produced against, and the
    validator cross-checks them against the invocation envelope. A different run freezes a
    different slice, so an agent executing here would stamp this run's digests; the harness does
    the same rather than leaving a mismatch the validator would correctly reject.
    """
    env = json.loads((state_dir(run_id) / "invocation-envelope.json").read_text(
        encoding="utf-8"))
    text = GOOD_SOURCE.read_text(encoding="utf-8")
    text = re.sub(r"(?m)^(\s*inputDigest:\s*).*$",
                  lambda m: m.group(1) + env["context_slice"]["input_digest"], text, count=1)
    text = re.sub(r"(?m)^(\s*contextDigest:\s*).*$",
                  lambda m: m.group(1) + env["context_slice"]["context_digest"], text,
                  count=1)
    return text


def broken_artifact(run_id: str) -> str:
    """The same plan with one mandatory section renamed.

    A single, surgical mutation, so the rejection is attributable: `Risks` is one of the twelve
    sections `agents/planner/output.md` fixes, so renaming it fails the mandatory-section check
    and introduces an unpermitted section, and nothing else changes.
    """
    return good_artifact(run_id).replace("\n## Risks\n", "\n## Risk Register\n", 1)


def act_as_adapter(run_id: str, *, artifact_text: str, side_effects: list | None = None):
    """Write what the adapter would have written, and nothing more."""
    d = state_dir(run_id)
    ledger = json.loads((d / "state-ledger.json").read_text(encoding="utf-8"))
    (CLAUDE / ledger["artifact_path"]).write_text(artifact_text, encoding="utf-8")
    (CLAUDE / ledger["result_envelope_path"]).write_text(json.dumps({
        "invocation_id": ledger["invocation_id"],
        "status": "succeeded",
        "artifact_refs": [ledger["artifact_path"]],
        "structured_output": {"produced_by": "verify_recovery.py acting as the adapter"},
        "evidence_refs": [ledger["artifact_path"]],
        "confidence": 0.5,
        "declared_side_effects": side_effects if side_effects is not None else [
            ledger["artifact_path"], ledger["result_envelope_path"]],
        "error_class": None,
        "error_detail": None,
    }, indent=2), encoding="utf-8")


def wait_for_backoff(run_id: str, r: Result, cid: str) -> float:
    """Sleep until the scheduled attempt is due, and report how long that was.

    The wait is real. A retry deadline the harness stepped over would prove the transition
    table and nothing about the policy that set the deadline.
    """
    item = item_of(run_id)
    due = datetime.strptime(item["available_at"], "%Y-%m-%dT%H:%M:%SZ").replace(
        tzinfo=timezone.utc)
    waited = 0.0
    while datetime.now(timezone.utc) < due:
        time.sleep(0.25)
        waited += 0.25
        if waited > rp.RETRY_PROFILE["max_delay_seconds"] + 5:
            break
    r.add(cid, "the scheduled attempt is not dispatchable before its deadline",
          waited > 0 or datetime.now(timezone.utc) >= due,
          f"waited {waited}s for the deadline {item['available_at']} set by the retry "
          f"profile ({rp.RETRY_PROFILE['backoff_strategy']}, base "
          f"{rp.RETRY_PROFILE['base_delay_seconds']}s)")
    return waited


# --------------------------------------------------------------------------- scenario A


def scenario_a() -> Result:
    r = Result("Run A -- a rejected artifact, classified retryable, repaired on the "
               "next attempt")
    run_id = plan(write_input(
        "repaired", "Attempt 1 returns a plan with one mandatory section renamed."))
    r.add("A0", "the run materialized and its planning phase is dispatchable",
          item_of(run_id)["status"] == se.PENDING and item_of(run_id)["eligible"],
          f"run {run_id}, {PHASE} is {item_of(run_id)['status']}")

    # ---- attempt 1: reject
    run("dispatch", "--run-id", run_id, "--phase", PHASE)
    act_as_adapter(run_id, artifact_text=broken_artifact(run_id))
    code, out = run("complete", "--run-id", run_id, "--phase", PHASE, expect=1)

    item = item_of(run_id)
    env = envelope_of(run_id)
    cls = env["classification"]
    r.add("A1", "the Validation Engine rejected the artifact",
          "validation     : FAIL" in out,
          next((ln.strip() for ln in out.splitlines() if "validation " in ln), ""))
    r.add("A2", "the failure was classified, not merely recorded",
          cls["failure_class"] == "output-schema-failure"
          and cls["detection_point"] == "validation" and cls["retryable"] is True,
          f"{cls['failure_class']} at {cls['detection_point']}, retryable="
          f"{cls['retryable']}, basis {cls['basis']}")
    r.add("A3", "the chosen action was retry, with the budget stated",
          cls["action"] == rp.RETRY and cls["attempts_charged"] == 1
          and cls["max_attempts"] == 3,
          f"action={cls['action']}, {cls['attempts_charged']} of {cls['max_attempts']} "
          f"charged, {cls['attempts_remaining']} remaining")
    r.add("A4", "the work item holds the canonical Retrying status with a deadline",
          item["status"] == se.RETRYING and item["queue_status"] == "Retrying"
          and bool(item["available_at"]),
          f"status={item['status']} queue={item['queue_status']} "
          f"available_at={item['available_at']} (+{cls['next_delay_seconds']}s)")
    r.add("A5", "the transition the state engine recorded is the canonical one",
          ("execution-planning", "running", "retrying", "validation_failed")
          in transitions_of(run_id),
          " -> ".join(t[2] for t in transitions_of(run_id) if t[0] == PHASE))
    r.add("A6", "a retry_scheduled event names the class, the action, and the deadline",
          any(e["event_type"] == "retry_scheduled"
              and e["details_ref"].get("recovery_action") == rp.RETRY
              for e in events_of(run_id)),
          next((f"{e['event_id']} {e['summary']}" for e in events_of(run_id)
                if e["event_type"] == "retry_scheduled"), "no retry_scheduled event"))
    r.add("A7", "the failure envelope states the decision needed and the action that "
                "clears it",
          env["schema"] == rp.ENVELOPE_SCHEMA and env["status"] == "open"
          and bool(env["required_decision_type"]) and bool(env["clearing_action"])
          and env["impacted_artifacts"],
          f"{env['envelope_id']}: decision {env['required_decision_type']}, impacted "
          f"{env['impacted_artifacts']}")
    r.add("A8", "the run projects the canonical Retrying lifecycle state",
          load_state(run_id)["run_status"] == "Retrying",
          f"run_status={load_state(run_id)['run_status']}")

    # ---- the deadline is enforced, then the scheduler promotes the work item.
    # The enforcement check is made against the persisted work item rather than by racing a
    # live dispatch against a two-second deadline: a proof whose outcome depends on how long a
    # subprocess took to start proves nothing repeatable.
    item = item_of(run_id)
    r.add("A9", "the scheduler holds the work item until its deadline and admits it after",
          se.retry_due({**item, "available_at": "2999-01-01T00:00:00Z"}) is False
          and se.retry_due({**item, "available_at": "2000-01-01T00:00:00Z"}) is True
          and item["eligible"] is False,
          f"the persisted item is {item['status']}, not eligible, and its deadline "
          f"{item['available_at']} is what the scheduler tests")

    wait_for_backoff(run_id, r, "A10")
    code, out = run("next", "--command", "implement", "--run-id", run_id)
    item = item_of(run_id)
    r.add("A11", "the scheduler promoted the work item once the backoff elapsed",
          item["status"] == se.PENDING and item["eligible"]
          and item["available_at"] is None
          and (PHASE, "retrying", "pending", "enqueued") in transitions_of(run_id),
          f"status={item['status']} eligible={item['eligible']}; "
          f"`next` says: "
          + next((ln.strip() for ln in out.splitlines() if ln.startswith("NEXT")), ""))
    r.add("A12", "the idempotency key survived the retry, because the unit of work did",
          item["idempotency_key"] == se.idempotency_key(
              run_id, PHASE, item["owner_agent_id"], "1.0.0", item["payload_digest"]),
          f"{item['idempotency_key']} recomputed from the persisted payload")

    # ---- attempt 2: the rejection is carried back, and the repair is accepted
    run("dispatch", "--run-id", run_id, "--phase", PHASE)
    prompt = (state_dir(run_id) / "dispatch-prompt.md").read_text(encoding="utf-8")
    envelope = json.loads((state_dir(run_id) / "invocation-envelope.json").read_text(
        encoding="utf-8"))
    prior = envelope.get("prior_validation") or {}
    r.add("A13", "the second attempt carries the rejection back to the agent as a repair pass",
          bool(prior.get("failures")) and "repair pass" in prompt.lower()
          and item_of(run_id)["attempt"] == 2,
          f"attempt {item_of(run_id)['attempt']}, {len(prior.get('failures') or [])} failed "
          f"check(s) named in the envelope and in the dispatch prompt")

    act_as_adapter(run_id, artifact_text=good_artifact(run_id))
    code, out = run("complete", "--run-id", run_id, "--phase", PHASE)
    item = item_of(run_id)
    entries = phase_entries(run_id)
    r.add("A14", "the repaired artifact was accepted and committed",
          item["status"] == se.COMPLETED and item["completion"]["validation"]["result"]
          == "pass",
          f"status={item['status']}, validation "
          f"{item['completion']['validation']['checksPassed']}/"
          f"{item['completion']['validation']['checksRun']} checks passed")
    r.add("A15", "the recovery ledger records the classification and its resolution, "
                 "rather than discarding it",
          len(entries) == 1 and entries[0]["status"] == "resolved"
          and bool(entries[0].get("resolution")),
          f"{len(entries)} entry: {entries[0]['envelope_id']} "
          f"{entries[0]['status']} -- {entries[0].get('resolution')}")
    r.add("A16", "one dispatched attempt was charged and one was not needed",
          item["attempt"] == 2 and se.attempts_charged(item) == 2
          and item["attempts_lost"] == 0,
          f"{item['attempt']} attempt(s) dispatched, {se.attempts_charged(item)} charged, "
          f"{item['attempts_lost']} lost to worker loss")

    # Every other phase of this workflow blocks at a guard rather than at a result, and those
    # blocks are the common case in this framework today. They must carry an envelope too.
    blocked = [i for i in load_state(run_id)["work_items"].values()
               if i["status"] == se.BLOCKED]
    all_entries = rp.RecoveryLedger(RUNS / run_id).entries()
    enveloped = {e["state_id"] for e in all_entries}
    guard_entries = [e for e in all_entries if e["guard"]]
    r.add("A17", "every guard-raised block emitted a classified envelope as well",
          blocked and all(i["state_id"] in enveloped for i in blocked),
          f"{len(blocked)} blocked work item(s), "
          f"{len(guard_entries)} guard-raised classification(s): "
          + ", ".join(sorted({f"{e['guard']}={e['failure_class']}"
                              for e in guard_entries})))
    r.add("A18", "each guard-raised envelope names the action that clears it",
          all(e["clearing_action"] and e["required_decision_type"]
              for e in guard_entries),
          f"decision types: "
          + ", ".join(sorted({e["required_decision_type"] for e in guard_entries})))
    return r


# --------------------------------------------------------------------------- scenario B


def scenario_b() -> Result:
    r = Result("Run B -- the same rejection every attempt: the budget bounds it and the "
               "fourth attempt is refused")
    run_id = plan(write_input(
        "exhausted", "Every attempt returns the same plan with a renamed section."))
    charged = []
    for attempt in (1, 2, 3):
        run("dispatch", "--run-id", run_id, "--phase", PHASE)
        act_as_adapter(run_id, artifact_text=broken_artifact(run_id))
        run("complete", "--run-id", run_id, "--phase", PHASE, expect=1)
        item = item_of(run_id)
        charged.append(se.attempts_charged(item))
        if item["status"] == se.RETRYING:
            if attempt == 2:
                # The second backoff is four seconds, which leaves room to observe the live
                # refusal without the observation itself deciding the outcome.
                code, out = run("dispatch", "--run-id", run_id, "--phase", PHASE,
                                expect=None)
                r.add("B0", "a live dispatch before the deadline is refused, and reports it",
                      code == 4 and item_of(run_id)["status"] == se.RETRYING,
                      next((ln.strip() for ln in out.splitlines()
                            if ln.startswith("RETRYING")), f"exit {code}"))
            wait_for_backoff(run_id, r, f"B{attempt}w")
            run("next", "--command", "implement", "--run-id", run_id)

    item = item_of(run_id)
    env = envelope_of(run_id)
    cls = env["classification"]
    r.add("B1", "every attempt that produced a judged result was charged to the budget",
          charged == [1, 2, 3],
          f"charged after each attempt: {charged}, ceiling {item['max_attempts']}")
    r.add("B2", "the retry budget was enforced rather than exceeded",
          cls["budget_exhausted"] is True and cls["action"] != rp.RETRY
          and item["attempt"] == 3,
          f"attempt {item['attempt']} of {item['max_attempts']}: "
          f"budget_exhausted={cls['budget_exhausted']}, action={cls['action']}")
    r.add("B3", "the work item blocked with a recorded reason rather than failing, because "
                "its artifact still exists",
          item["status"] == se.BLOCKED
          and item["blocked_reason"] == "awaiting_recovery_task"
          and item["blocked_by"] == "recovery-controller",
          f"status={item['status']} reason={item['blocked_reason']} "
          f"raised_by={item['blocked_by']}")
    r.add("B4", "the envelope explains the exhaustion and names a human decision",
          env["status"] == "open" and cls["attempts_remaining"] == 0
          and env["required_decision_type"] == "recovery-authorisation",
          f"{cls['reason']}")
    # B5 -- the exhausted phase must not be reported as progressing. The run-level status is
    # `WaitingForHuman` only while nothing else in the run is dispatchable; as more phases
    # gain a registered capability, an unrelated eligible phase legitimately reports
    # `Dispatching` at the run level, per the precedence in `state_engine.run_status`. So the
    # claim is checked where it belongs: on this work item, and on the run only when this
    # blocker is the run's whole story.
    state = load_state(run_id)
    others_eligible = sorted(
        i["state_id"] for i in state["work_items"].values()
        if i["work_type"] == "state" and i["state_id"] != PHASE
        and i["status"] == se.PENDING and i["eligible"])
    held = item["status"] == se.BLOCKED and item["blocked_reason"] == "awaiting_recovery_task"
    run_ok = state["run_status"] == ("Dispatching" if others_eligible else "WaitingForHuman")
    r.add("B5", "the exhausted phase is held for a human, and the run status reports that "
                "rather than progress on it",
          held and run_ok,
          f"work item {PHASE}={item['status']}/{item['blocked_reason']}, "
          f"run_status={state['run_status']}"
          + (f", independently dispatchable elsewhere: {others_eligible}"
             if others_eligible else ", nothing else dispatchable"))

    code, out = run("release", "--run-id", run_id, "--phase", PHASE, "--reason",
                    "tool_failure", "--detail", "operator asserts the defect is repaired",
                    expect=2)
    r.add("B6", "a fourth attempt is refused, because the budget does not have one",
          "RUNTIME FAILURE" in out and "retry budget" in out,
          next((ln.strip() for ln in out.splitlines() if "RUNTIME FAILURE" in ln), ""))
    r.add("B7", "the refusal changed nothing: the work item is still blocked on the same "
                "condition",
          item_of(run_id)["status"] == se.BLOCKED
          and item_of(run_id)["attempt"] == 3
          and envelope_of(run_id)["status"] == "open",
          f"status={item_of(run_id)['status']} attempt={item_of(run_id)['attempt']} "
          f"envelope={envelope_of(run_id)['status']}")

    entries = phase_entries(run_id)
    r.add("B8", "every attempt left an append-only ledger entry, none overwritten",
          len(entries) == 3
          and [e["occurrence"] for e in entries] == [1, 2, 3]
          and all(e["failure_class"] == "output-schema-failure" for e in entries),
          f"{len(entries)} classification(s), occurrences "
          f"{[e['occurrence'] for e in entries]}, actions "
          f"{[e['classification']['action'] for e in entries]}")
    return r


# --------------------------------------------------------------------------- scenario C


def scenario_c() -> Result:
    r = Result("Run C -- a policy violation is never retried, however many attempts remain")
    run_id = plan(write_input(
        "policy", "The artifact conforms, but the adapter declares a write outside its "
                  "permitted set."))
    run("dispatch", "--run-id", run_id, "--phase", PHASE)
    envelope = json.loads((state_dir(run_id) / "invocation-envelope.json").read_text(
        encoding="utf-8"))
    permitted = envelope["constraints"]["permitted_writes"]
    ledger_path = state_dir(run_id) / "state-ledger.json"
    ledger = json.loads(ledger_path.read_text(encoding="utf-8"))
    act_as_adapter(run_id, artifact_text=good_artifact(run_id),
                   side_effects=[ledger["artifact_path"], ledger["result_envelope_path"],
                                 "memory/architecture.md"])
    code, out = run("complete", "--run-id", run_id, "--phase", PHASE, expect=1)

    item = item_of(run_id)
    env = envelope_of(run_id)
    cls = env["classification"]
    r.add("C1", "the artifact itself conformed, so the rejection is attributable to the "
                "policy alone",
          "validation     : PASS" in out and "UNDECLARED" in out,
          next((ln.strip() for ln in out.splitlines() if "side effects" in ln), ""))
    r.add("C2", "the failure was classified as a policy failure, not a schema failure",
          cls["failure_class"] == "policy-failure" and cls["retryable"] is False,
          f"{cls['failure_class']} at {cls['detection_point']}, retryable="
          f"{cls['retryable']}, basis {cls['basis']}")
    r.add("C3", "no retry was scheduled, although the budget had attempts left",
          cls["action"] != rp.RETRY and cls["attempts_remaining"] >= 0
          and item["status"] == se.BLOCKED and item["available_at"] is None,
          f"action={cls['action']} with {cls['attempts_remaining']} attempt(s) unspent; "
          f"status={item['status']}")
    r.add("C4", "the block names the policy condition and the exception decision it needs",
          item["blocked_reason"] == "awaiting_policy_exception"
          and env["required_decision_type"] == "policy-exception",
          f"reason={item['blocked_reason']}, decision={env['required_decision_type']}")
    r.add("C5", "no retry_scheduled event was emitted for this run at all",
          not any(e["event_type"] == "retry_scheduled" for e in events_of(run_id)),
          f"event types: {sorted({e['event_type'] for e in events_of(run_id)})}")
    r.add("C6", "the undeclared write is named in the envelope evidence",
          "memory/architecture.md" in json.dumps(env["evidence_bundle_ref"]),
          f"permitted_writes were {permitted}; declared write outside that set is recorded")
    return r


# --------------------------------------------------------------------------- clearing actions


def runtime_command(action: str) -> list | None:
    """The runtime command a clearing action names, as argv, or None for prose."""
    if not action or "framework_runtime.py" not in action:
        return None
    text = action[action.index("python"):] if "python" in action else action
    # An action may carry prose after the command (`... To reclaim it sooner: <cmd>`); the
    # command is what runs. Placeholders in angle brackets are not executable.
    argv = shlex.split(text.split("\n")[0], posix=True)
    return argv


def placeholders(argv: list, exempt: set = frozenset()) -> list:
    """Every placeholder a clearing action's argv may not carry, each as its text.

    A placeholder is free text in angle brackets, written unquoted, so `shlex` splits one of
    several words (`<fill in owner name>`) across arguments and leaves one glued to its
    neighbours (`x<who>`, `<who>x`) inside a single argument; neither survives as one argument
    that starts with `<` and ends with `>`. An argument that opens `<` without closing it
    therefore extends the placeholder to the next argument that closes it, or to the end when
    none does, and any argument carrying a bracket at all is part of one. Only a placeholder
    that is exactly one whole argument directly after a flag in `exempt` is the human's to fill;
    every other one is not executable as written.
    """
    found, i = [], 0
    while i < len(argv):
        arg = argv[i]
        if "<" not in arg and ">" not in arg:
            i += 1
            continue
        end = i
        if "<" in arg and ">" not in arg[arg.index("<"):]:
            end = next((k for k in range(i + 1, len(argv)) if ">" in argv[k]), len(argv) - 1)
        whole = (end == i and arg.startswith("<") and arg.endswith(">")
                 and arg.count("<") == arg.count(">") == 1)
        if not (whole and i > 0 and argv[i - 1] in exempt):
            found.append(" ".join(argv[i:end + 1]))
        i = end + 1
    return found


def executable(argv: list, decided_by: str = "verify_recovery") -> list:
    """A clearing action's argv with its one human placeholder, `--decided-by <who>`, filled.

    The authoriser is the one argument the runtime cannot know, so the envelope leaves it a
    placeholder rather than defaulting it from who rejected; executing the action as written
    means supplying exactly that and nothing else. Any other placeholder, in any form
    `placeholders` recognises, is a harness fault.
    """
    out = list(argv)
    if "--decided-by" in out and out[out.index("--decided-by") + 1] == "<who>":
        out[out.index("--decided-by") + 1] = decided_by
    left = placeholders(out)
    assert not left, f"clearing action still carries placeholders {left}: {argv}"
    return out


def clearing_action_admissible(run_id: str, entry: dict) -> tuple[bool, str]:
    """Whether an envelope's clearing action can succeed for its work item's type and status.

    The admissibility table. A command is admissible only where the runtime would act on it:

      release  --phase P     P is a state item that holds a lease, or is blocked by the
                             Recovery Controller (not by a guard)
      gate     --gate G      G is a gate item that is blocked awaiting a decision and undecided;
                             the role, decider, and rationale are the human's judgement and
                             may stay placeholders -- pre-filling an approval would be a
                             prescription to approve, which no envelope may make
      rollback --gate G      G is a gate item that is failed with decision rejected, the target
                             is the closed phase or a completed hard predecessor, and every
                             phase in range has a charged attempt left; the decider is the
                             authoriser -- a second human decision the envelope cannot name --
                             and may stay a placeholder, while the role and rationale are
                             filled in and validated
      policy-exception --phase P
                             P is a state item that is blocked with reason
                             awaiting_policy_exception; the role, decider, and rationale are
                             the human's judgement and may stay placeholders on exactly that
                             item -- whether a declared write is a breach or a false positive
                             is the decision the envelope asks for, so it cannot pre-fill it.
                             A filled role must be one the envelope lists, where it lists
                             any, and may not be the producer's own. A completed item is
                             inadmissible although the command exits 0 there, by replay
      prose (no command)     admissible: it names no command that can fail

    A retry-class action states that nothing is required and appends `release` as an optional
    reclaim; the prescription is the wait, so it is judged as prose.

    A placeholder is whatever `placeholders` recognises, split, glued, or unterminated alike.
    One that "may stay" above does so only as one whole argument directly after its flag.
    """
    state = load_state(run_id)
    items = state["work_items"]
    action = entry.get("clearing_action") or ""
    if action.startswith("none required"):
        return True, "retry: nothing required"
    argv = runtime_command(action)
    if argv is None:
        return True, "prose; names no runtime command"
    sub = argv[2] if len(argv) > 2 else None

    def opt(flag):
        return argv[argv.index(flag) + 1] if flag in argv else None

    def awaiting_policy_exception(phase):
        it = items.get(phase)
        return (it is not None and it.get("work_type") == "state"
                and it["status"] == se.BLOCKED
                and it.get("blocked_reason") == "awaiting_policy_exception")

    judgement = {"--owner-role", "--decided-by", "--rationale"}
    human = (judgement if sub == "gate"
             else {"--decided-by"} if sub == "rollback"
             else judgement if (sub == "policy-exception"
                                and awaiting_policy_exception(opt("--phase")))
             else set())
    left = placeholders(argv, human)
    if left:
        return False, f"placeholder argument {left} in {argv}"

    if sub == "release":
        it = items.get(opt("--phase"))
        if it is None:
            return False, f"release names {opt('--phase')!r}, not a state work item"
        ok = it["status"] in se.ACTIVE_STATUSES or (
            it["status"] == se.BLOCKED and it.get("blocked_by") not in (None, "guard"))
        return ok, (f"release on {it['state_id']} status={it['status']} "
                    f"blocked_by={it.get('blocked_by')}")
    if sub == "gate":
        name = opt("--gate")
        it = items.get(f"gate:{name}")
        g = state["gates"].get(name)
        if it is None or g is None:
            return False, f"gate names {name!r}, not a gate work item"
        ok = it["status"] == se.BLOCKED and g.get("decision") is None
        return ok, f"gate on {name} status={it['status']} decision={g.get('decision')}"
    if sub == "rollback":
        name = opt("--gate")
        it = items.get(f"gate:{name}")
        g = state["gates"].get(name)
        if it is None or g is None:
            return False, f"rollback names {name!r}, not a gate work item"
        if it["status"] != se.FAILED or g.get("decision") != "rejected":
            return False, f"rollback on {name} status={it['status']} decision={g.get('decision')}"
        target = opt("--target") or g["closes_state"]
        store = se.StateStore.load(RUNS / run_id)
        try:
            phases = fr.rollback_range(store, target, g["closes_state"])
        except ValueError:
            return False, f"rollback target {target!r} is not in range"
        over = [p for p in phases if se.attempts_charged(items[p]) >= items[p]["max_attempts"]]
        if over:
            return False, f"rollback would exceed the budget of {over}"
        role = opt("--owner-role")
        if role in fr.producer_aliases(g.get("producer_agent")) or (
                g.get("owner_roles") and role not in g["owner_roles"]):
            return False, f"rollback names role {role!r}, which may not decide {name}"
        return True, f"rollback on {name} -> {target}, superseding {phases}"
    if sub == "policy-exception":
        phase = opt("--phase")
        it = items.get(phase)
        if it is None or it.get("work_type") != "state":
            return False, f"policy-exception names {phase!r}, not a state work item"
        if not awaiting_policy_exception(phase):
            return False, (f"policy-exception on {phase} status={it['status']} "
                           f"blocked_reason={it.get('blocked_reason')}")
        role = opt("--owner-role")
        if role != "<role>":
            producer = entry.get("owner_agent_id") or it.get("owner_agent_id")
            listed = entry.get("owner_roles") or []
            if (listed and role not in listed) or (
                    producer and role in fr.producer_aliases(producer)):
                return False, f"policy-exception names role {role!r}, which may not decide {phase}"
        return True, f"policy-exception on {phase} blocked_reason=awaiting_policy_exception"
    return False, f"unknown subcommand {sub!r}"


def store_content(run_id: str) -> dict:
    """The run's state and recovery ledger with every `updated_at` removed.

    Every command re-evaluates the guards and saves the store, which rewrites the timestamps
    and nothing else; a refusal that changed no fact must leave everything else identical.
    """
    def strip(node):
        if isinstance(node, dict):
            return {k: strip(v) for k, v in node.items() if k != "updated_at"}
        if isinstance(node, list):
            return [strip(v) for v in node]
        return node
    ledger = RUNS / run_id / "recovery-ledger.json"
    return strip({"state": load_state(run_id),
                  "ledger": json.loads(ledger.read_text(encoding="utf-8"))
                  if ledger.exists() else None})


def check_clearing_actions(r: Result, cid: str, run_ids: list):
    """Every open envelope of every run names a clearing action admissible for its item."""
    bad, seen = [], 0
    for run_id in run_ids:
        for e in rp.RecoveryLedger(RUNS / run_id).entries():
            if e.get("status") != "open":
                continue
            seen += 1
            ok, why = clearing_action_admissible(run_id, e)
            if not ok:
                bad.append(f"{e['envelope_id']}: {why}")
    r.add(cid, "every open envelope's clearing action is admissible for its work item's type "
               "and status, or names no command",
          not bad, f"{seen} open envelope(s) across {len(run_ids)} run(s) checked against "
                   f"the admissibility table" if not bad else f"inadmissible: {bad}")


# --------------------------------------------------------------------------- scenario D


def gate_item_of(run_id: str) -> dict:
    return load_state(run_id)["work_items"][f"gate:{GATE}"]


def gate_of(run_id: str) -> dict:
    return load_state(run_id)["gates"][GATE]


def gate_entries(run_id: str) -> list:
    return [e for e in rp.RecoveryLedger(RUNS / run_id).entries() if e["state_id"] == GATE]


def scenario_d() -> Result:
    r = Result("Run D -- a gate rejection is rolled back: the phase is superseded and rebuilt, "
               "the gate re-armed, the evidence immutable")
    run_id = plan(write_input(
        "rollback", "Attempt 1 is accepted; its gate is rejected and a rollback authorised."))
    gate_rec = gate_of(run_id)
    producer = gate_rec["producer_agent"]
    deciders = [o for o in gate_rec["owner_roles"]
                if o not in fr.producer_aliases(producer)]
    r.add("D0", "the gate closing the planning phase lists a non-producing owner",
          bool(deciders), f"{GATE} owners {gate_rec['owner_roles']}, producer {producer!r}, "
                          f"deciders {deciders}")
    decider = deciders[0]

    # ---- attempt 1: accepted, then rejected at the gate
    run("dispatch", "--run-id", run_id, "--phase", PHASE)
    act_as_adapter(run_id, artifact_text=good_artifact(run_id))
    run("complete", "--run-id", run_id, "--phase", PHASE)
    item1 = item_of(run_id)
    key1 = item1["idempotency_key"]
    art1 = CLAUDE / item1["completion"]["artifact_path"]
    digest1 = fr.sha256_text(art1.read_text(encoding="utf-8"))
    bytes1 = art1.read_bytes()
    seq_before = len(load_state(run_id)["transitions"])
    r.add("D1", "attempt 1 was accepted and committed, and its gate awaits a decision",
          item1["status"] == se.COMPLETED and gate_item_of(run_id)["status"] == se.BLOCKED
          and gate_item_of(run_id)["blocked_reason"] == "awaiting_human_decision",
          f"{PHASE} completed at {item1['completion']['artifact_path']} ({digest1}); "
          f"{GATE} {gate_item_of(run_id)['status']}/{gate_item_of(run_id)['blocked_reason']}")

    rationale = "plan omits the rollback verification steps; return to planning"
    run("gate", "--run-id", run_id, "--gate", GATE, "--decision", "reject",
        "--owner-role", decider, "--decided-by", "verify_recovery", "--rationale", rationale)
    gi = gate_item_of(run_id)
    g = gate_of(run_id)
    rejection_seq = next(t["seq"] for t in load_state(run_id)["transitions"]
                         if t["state_id"] == GATE and t["to"] == se.FAILED)
    env = json.loads((RUNS / run_id / "gates" / "planning-gate" /
                      "failure-envelope.json").read_text(encoding="utf-8"))
    r.add("D2", "the rejection left the gate failed, classified gate-rejection with action "
                "rollback, and labelled the work item with the same class",
          gi["status"] == se.FAILED and g["decision"] == "rejected"
          and gi["failure_class"] == "gate-rejection"
          and env["failure_class"] == "gate-rejection"
          and env["classification"]["action"] == rp.ROLLBACK and env["status"] == "open",
          f"gate {gi['status']} class={gi['failure_class']}; envelope {env['envelope_id']} "
          f"{env['failure_class']} -> {env['classification']['action']}, "
          f"decision needed {env['required_decision_type']}")
    successors = [i for i in load_state(run_id)["work_items"].values()
                  if i["work_type"] == "state" and i["status"] == se.BLOCKED
                  and i.get("failure_class") == "gate-rejection"]
    r.add("D3", "the successor phase blocked on the rejection with the rollback class",
          bool(successors) and all(i["blocked_reason"] == "awaiting_recovery_task"
                                   and i.get("blocked_by") == "guard" for i in successors),
          ", ".join(f"{i['state_id']}={i['blocked_reason']}" for i in successors)
          or "no successor blocked")
    check_clearing_actions(r, "D4", [run_id])
    successor_env = None
    for i in successors:
        fe = state_dir(run_id).parent / i["state_id"] / "failure-envelope.json"
        if fe.exists():
            successor_env = json.loads(fe.read_text(encoding="utf-8"))
    r.add("D5", "both the gate's and the successor's envelopes name the rollback command "
                "with the gate, and the option list offers no exception",
          "rollback" in env["clearing_action"] and GATE in env["clearing_action"]
          and successor_env is not None and "rollback" in successor_env["clearing_action"]
          and GATE in successor_env["clearing_action"]
          and not any("exception" in o for o in env["proposed_options"]),
          f"gate: {env['clearing_action']}")
    r.add("D5a", "the clearing action names the role that rejected but never defaults the "
                 "authoriser from the rejection: `--decided-by` stays a placeholder",
          f"--owner-role {decider}" in env["clearing_action"]
          and "--decided-by <who>" in env["clearing_action"]
          and "--decided-by verify_recovery" not in env["clearing_action"]
          and successor_env is not None
          and "--decided-by <who>" in successor_env["clearing_action"],
          env["clearing_action"])
    code, nxt0 = run("next", "--command", "implement", "--run-id", run_id, expect=None)
    first_next = next((ln for ln in nxt0.splitlines() if ln.startswith("NEXT:")), "")
    clear_by = [ln for ln in nxt0.splitlines() if "clear by" in ln]
    r.add("D5b", "while the rejection stands `next` names the rollback command first, derived "
                 "live, offers no gate decision, and every clearing action it prints is the "
                 "rollback",
          "authorise the rollback" in first_next and GATE in first_next
          and f'--gate "{GATE}"' in nxt0 and "--decided-by <who>" in nxt0
          and "record a decision" not in nxt0
          and all("rollback" in ln for ln in clear_by),
          f"exit {code}; {first_next[:110]}")
    ledger_path = RUNS / run_id / rp.RecoveryLedger.FILENAME
    ledger_bytes = ledger_path.read_bytes()
    code, rec = run("recovery", "--run-id", run_id, "--open-only")
    clear = [ln.strip() for ln in rec.splitlines() if ln.strip().startswith("clear by")]
    r.add("D5c", "`recovery --open-only` derives the clearing action of every open "
                 "gate-rejection envelope live -- `rollback` naming the gate, never "
                 "`release` -- and leaves the ledger as written",
          bool(clear) and any("rollback" in ln and GATE in ln for ln in clear)
          and not any("release --run-id" in ln for ln in clear)
          and ledger_path.read_bytes() == ledger_bytes,
          f"{len(clear)} open envelope(s); ledger unchanged="
          f"{ledger_path.read_bytes() == ledger_bytes}")

    # ---- negatives, before the authorisation: each refused, store unchanged
    before = store_content(run_id)
    refusals = []
    for label, argv in (
            ("producer role", ["--owner-role", producer]),
            ("unlisted role", ["--owner-role", "nobody-role"]),
            ("invalid target", ["--owner-role", decider, "--target", "implementation"]),
    ):
        code, out = run("rollback", "--run-id", run_id, "--gate", GATE, "--decided-by",
                        "verify_recovery", *argv, expect=None)
        refusals.append((label, code, "RUNTIME FAILURE" in out))
    unchanged = store_content(run_id) == before
    r.add("D6", "a producer role, an unlisted role, and an invalid target are each refused "
                "with no store change",
          all(code == 2 and failed for _, code, failed in refusals) and unchanged,
          f"{[(l, c) for l, c, _ in refusals]}; state.json and recovery-ledger.json "
          f"unchanged apart from timestamps={unchanged}")

    # ---- negative: an approved sibling gate closing the rejected phase itself. Injected into
    # the store as `record_gate_decision` would leave it, then removed byte-for-byte; the
    # runtime must refuse to leave an approval standing over evidence it would rebuild.
    state_path = RUNS / run_id / "state.json"
    original = state_path.read_bytes()
    state = json.loads(original.decode("utf-8"))
    sibling = json.loads(json.dumps(state["work_items"][f"gate:{GATE}"]))
    sibling.update({"state_id": "Sibling Gate", "gate": "Sibling Gate",
                    "work_item_id": f"{run_id}::gate::Sibling Gate", "status": se.COMPLETED,
                    "queue_status": "Completed", "blocked_reason": None,
                    "blocked_detail": None, "blocked_by": None, "failure_class": None,
                    "failure_detail": None})
    state["work_items"]["gate:Sibling Gate"] = sibling
    state["gates"]["Sibling Gate"] = {**state["gates"][GATE], "gate": "Sibling Gate",
                                     "decision": "approved", "owner_role": decider,
                                     "decided_by": "verify_recovery",
                                     "rationale": "sibling approval", "decision_history": []}
    state_path.write_text(json.dumps(state, indent=2), encoding="utf-8")
    before_sibling = store_content(run_id)
    code, out = run("rollback", "--run-id", run_id, "--gate", GATE, "--owner-role", decider,
                    "--decided-by", "verify_recovery", expect=None)
    unchanged = store_content(run_id) == before_sibling
    state_path.write_bytes(original)
    r.add("D6a", "an approved sibling gate closing the rejected phase itself refuses the "
                 "rollback -- the closed phase is checked like every phase in range -- with "
                 "no store change",
          code == 2 and "approved gate" in out and "Sibling Gate" in out and unchanged,
          next((ln.strip() for ln in out.splitlines() if "RUNTIME FAILURE" in ln), "")[:150]
          + f"; store unchanged={unchanged}")

    # ---- the clearing action, executed as written with the authoriser its only substitution
    argv = runtime_command(env["clearing_action"])
    proc = subprocess.run([PY, *executable(argv)[1:]], capture_output=True, text=True,
                          cwd=str(CLAUDE.parent))
    out = proc.stdout + proc.stderr
    r.add("D7", "the envelope's clearing action, executed as written with the authoriser as "
                "its only substitution, exits 0",
          argv[0] == "python" and proc.returncode == 0,
          f"`{env['clearing_action']}` -> exit {proc.returncode}; "
          + next((ln.strip() for ln in out.splitlines() if ln.startswith("rollback")), ""))

    item = item_of(run_id)
    gi = gate_item_of(run_id)
    g = gate_of(run_id)
    sups = item.get("supersessions") or []
    code, nxt = run("next", "--command", "implement", "--run-id", run_id)
    row = next((ln for ln in nxt.splitlines() if f" {PHASE} " in ln and " state " in ln), "")
    r.add("D8", "the phase is pending and eligible under its unchanged idempotency key, and "
                "`next` reports it Ready",
          item["status"] == se.PENDING and item["eligible"]
          and item["idempotency_key"] == key1 and item["completion"] is None
          and "pending" in row and "Ready" in row,
          f"status={item['status']} eligible={item['eligible']} key unchanged="
          f"{item['idempotency_key'] == key1}; `next` row: {' '.join(row.split())}")
    r.add("D9", "the superseded completion moved unmodified into supersessions, and attempt 1's "
                "artifact is byte-identical at its committed path",
          len(sups) == 1 and sups[0]["completion"]["artifact_digest"] == digest1
          and sups[0]["completion"]["artifact_path"] == item1["completion"]["artifact_path"]
          and sups[0]["attempt"] == 1 and bool(sups[0].get("authorisation_id"))
          and art1.read_bytes() == bytes1
          and fr.sha256_text(art1.read_text(encoding="utf-8")) == digest1,
          f"supersessions[0]: attempt {sups[0]['attempt'] if sups else '-'} under "
          f"{sups[0].get('authorisation_id') if sups else '-'}, digest "
          f"{sups[0]['completion']['artifact_digest'] if sups else '-'} == file digest "
          f"{fr.sha256_text(art1.read_text(encoding='utf-8'))}")
    snap = state_dir(run_id) / "attempts" / "1"
    r.add("D10", "attempt 1's invocation, result, ledger, and validation files were snapshotted",
          all((snap / n).exists() for n in ("invocation-envelope.json", "result-envelope.json",
                                            "state-ledger.json", "validation-report.json")),
          f"{sorted(p.name for p in snap.glob('*')) if snap.exists() else 'no snapshot dir'}")
    history = g.get("decision_history") or []
    r.add("D11", "the gate re-armed on the same work item: pending, decision null, the "
                 "rejection first in decision_history, failure class cleared",
          gi["status"] == se.PENDING and g["decision"] is None and gi["failure_class"] is None
          and len(history) == 1 and history[0]["decision"] == "rejected"
          and history[0]["owner_role"] == decider and history[0]["rationale"] == rationale
          and history[0].get("superseded_by") == sups[0].get("authorisation_id"),
          f"gate {gi['status']} decision={g['decision']} history="
          f"{[(h['decision'], h['owner_role']) for h in history]}")
    transitions = load_state(run_id)["transitions"]
    rejection_t = next(t for t in transitions if t["seq"] == rejection_seq)
    gate_env_now = json.loads((RUNS / run_id / "gates" / "planning-gate" /
                               "failure-envelope.json").read_text(encoding="utf-8"))
    r.add("D12", "the rejection's transition and envelope are untouched, and the envelope is "
                 "resolved naming the authorisation",
          rejection_t["from"] == se.BLOCKED and rejection_t["to"] == se.FAILED
          and rejection_t["trigger"] == "decision_rejected"
          and gate_env_now["envelope_id"] == env["envelope_id"]
          and gate_env_now["status"] == "resolved"
          and sups[0]["authorisation_id"] in (gate_env_now.get("resolution") or "")
          and all(e["status"] == "resolved" for e in gate_entries(run_id)),
          f"seq {rejection_seq} {rejection_t['from']}->{rejection_t['to']} "
          f"({rejection_t['trigger']}); {gate_env_now['envelope_id']} "
          f"{gate_env_now['status']}: {gate_env_now.get('resolution')}")
    r.add("D13", "the two authorised transitions were recorded under their triggers, and "
                 "nothing else left a terminal status",
          any(t["state_id"] == PHASE and t["from"] == se.COMPLETED and t["to"] == se.PENDING
              and t["trigger"] == "superseded" for t in transitions)
          and any(t["state_id"] == GATE and t["from"] == se.FAILED and t["to"] == se.PENDING
                  and t["trigger"] == "rollback_authorised" for t in transitions)
          and any(e["event_type"] == "rollback_scheduled" for e in events_of(run_id)),
          " ".join(f"{t['state_id']}:{t['from']}->{t['to']}({t['trigger']})"
                   for t in transitions if t["from"] in se.TERMINAL_STATUSES))
    successors_now = [load_state(run_id)["work_items"][i["state_id"]] for i in successors]
    r.add("D14", "the successor's stale rollback block cleared: it waits on its predecessor "
                 "instead, with its envelope resolved",
          all(i["status"] == se.PENDING and not i["eligible"] and i["blocked_reason"] is None
              for i in successors_now)
          and all(e["status"] == "resolved"
                  for e in rp.RecoveryLedger(RUNS / run_id).entries()
                  if e["state_id"] in {i["state_id"] for i in successors}
                  and e["failure_class"] == "gate-rejection"),
          ", ".join(f"{i['state_id']}={i['status']}/{i['queue_status']}"
                    for i in successors_now) or "no successor")
    code, out = run("gate", "--run-id", run_id, "--gate", GATE, "--decision", "approve",
                    "--owner-role", decider, "--decided-by", "verify_recovery",
                    "--rationale", "premature", expect=2)
    r.add("D15", "a decision on the re-armed gate is refused while its evidence is being "
                 "rebuilt, and `next` does not offer it",
          "RUNTIME FAILURE" in out and gate_of(run_id)["decision"] is None
          and "record a decision" not in nxt,
          next((ln.strip() for ln in out.splitlines() if "RUNTIME FAILURE" in ln), ""))
    code, out = run("status", "--run-id", run_id, "--json")
    view = json.loads(out[out.index("{"):])
    step = next(s for ph in view["phases"] for s in ph["steps"] if s["state_id"] == PHASE)
    gate_view = next(gv for ph in view["phases"] for gv in ph["gates"]
                     if gv["state_id"] == GATE)
    r.add("D16", "the status view carries the consumer contract: the target step pending, "
                 "eligible, attempt 1, with a rollback block; the gate pending and undecided",
          step["status"] == se.PENDING and step["eligible"] and step["attempt"] == 1
          and step.get("rollback", {}).get("gate") == GATE
          and step["rollback"]["authorisation_id"] == sups[0]["authorisation_id"]
          and step["rollback"]["superseded_attempt"] == 1
          and gate_view["status"] == se.PENDING and gate_view["decision"] is None
          and gate_view["decision_history"][0]["decision"] == "rejected",
          f"step {step['status']}/{step['eligible']}/attempt {step['attempt']} rollback="
          f"{step.get('rollback')}; gate {gate_view['status']} decision="
          f"{gate_view['decision']}")

    # ---- attempt 2: the rejection rides along, the artifact lands attempt-scoped
    run("dispatch", "--run-id", run_id, "--phase", PHASE)
    envelope = json.loads((state_dir(run_id) / "invocation-envelope.json").read_text(
        encoding="utf-8"))
    prompt = (state_dir(run_id) / "dispatch-prompt.md").read_text(encoding="utf-8")
    ledger2 = json.loads((state_dir(run_id) / "state-ledger.json").read_text(encoding="utf-8"))
    pr = envelope.get("prior_rejection") or {}
    declared = envelope["expected_output_schema"]["artifact_path"]
    r.add("D17", "the re-dispatch carries the rejection as prior_rejection and the same key",
          pr.get("rationale") == rationale and pr.get("gate") == GATE
          and pr.get("superseded_attempt") == 1 and "rework pass" in prompt.lower()
          and envelope["idempotency_key"] == key1 and item_of(run_id)["attempt"] == 2,
          f"attempt {item_of(run_id)['attempt']}, prior_rejection by {pr.get('rejected_by')}, "
          f"key unchanged={envelope['idempotency_key'] == key1}")
    r.add("D18", "the declared artifact path, permitted writes, and state ledger name the "
                 "attempt-scoped path, while attempt 1's path is untouched",
          declared.endswith(f"/artifacts/attempt-2/execution-plan.md")
          and declared != item1["completion"]["artifact_path"]
          and declared in envelope["constraints"]["permitted_writes"]
          and ledger2["artifact_path"] == declared
          and fr.permitted_write(declared, envelope),
          f"declared {declared}; attempt 1 at {item1['completion']['artifact_path']}")
    code, out = run("dispatch", "--run-id", run_id, "--phase", PHASE)
    r.add("D19", "a second dispatch of the re-entered attempt is replay-suppressed",
          "REPLAY" in out and item_of(run_id)["attempt"] == 2,
          next((ln.strip() for ln in out.splitlines() if ln.startswith("REPLAY")), ""))

    act_as_adapter(run_id, artifact_text=good_artifact(run_id))
    run("complete", "--run-id", run_id, "--phase", PHASE)
    item = item_of(run_id)
    gi = gate_item_of(run_id)
    r.add("D20", "attempt 2 completed at the attempt path, both attempts charged, attempt 1's "
                 "artifact still byte-identical",
          item["status"] == se.COMPLETED and item["completion"]["artifact_path"] == declared
          and se.attempts_charged(item) == 2 and item["attempt"] == 2
          and art1.read_bytes() == bytes1 and len(item["supersessions"]) == 1,
          f"completed at {item['completion']['artifact_path']}, "
          f"{se.attempts_charged(item)} of {item['max_attempts']} charged")
    r.add("D21", "the re-armed gate blocks again awaiting a fresh human decision",
          gi["status"] == se.BLOCKED and gi["blocked_reason"] == "awaiting_human_decision"
          and gate_of(run_id)["decision"] is None,
          f"{GATE} {gi['status']}/{gi['blocked_reason']}")
    code, out = run("next", "--command", "implement", "--run-id", run_id,
                    "--gate-policy", "auto", expect=None)
    r.add("D22", "under the auto-approval policy the re-armed gate is held for a human",
          gate_of(run_id)["decision"] is None and "held for a human decision" in out
          and "rollback" in out,
          next((ln.strip() for ln in out.splitlines() if "held for a human" in ln), ""))
    check_clearing_actions(r, "D23", [run_id])

    # ---- the multi-phase invariants hold over the superseded run
    checks = {c["id"]: c for c in vmp.run_checks(RUNS / run_id, replay=False).checks}
    r.add("D24", "verify_multi_phase M4, M12, and M13 hold over the superseded run",
          all(checks[c]["result"] == "PASS" for c in ("M3", "M4", "M12", "M13")),
          "; ".join(f"{c}={checks[c]['result']}" for c in ("M3", "M4", "M12", "M13")))

    # ---- a second rejection, then exhaustion
    run("gate", "--run-id", run_id, "--gate", GATE, "--decision", "reject",
        "--owner-role", decider, "--decided-by", "verify_recovery",
        "--rationale", "still missing the verification steps")
    env2 = json.loads((RUNS / run_id / "gates" / "planning-gate" /
                       "failure-envelope.json").read_text(encoding="utf-8"))
    argv = runtime_command(env2["clearing_action"])
    proc = subprocess.run([PY, *executable(argv)[1:]], capture_output=True, text=True,
                          cwd=str(CLAUDE.parent))
    run("dispatch", "--run-id", run_id, "--phase", PHASE)
    act_as_adapter(run_id, artifact_text=good_artifact(run_id))
    run("complete", "--run-id", run_id, "--phase", PHASE)
    run("gate", "--run-id", run_id, "--gate", GATE, "--decision", "reject",
        "--owner-role", decider, "--decided-by", "verify_recovery",
        "--rationale", "third rejection")
    item = item_of(run_id)
    r.add("D25", "a second rollback re-entered the phase as attempt 3, which was charged",
          proc.returncode == 0 and item["attempt"] == 3 and se.attempts_charged(item) == 3
          and len(item["supersessions"]) == 2
          and len(gate_of(run_id)["decision_history"]) == 2,
          f"attempt {item['attempt']}, {se.attempts_charged(item)} charged, "
          f"{len(item['supersessions'])} supersession(s)")
    env3 = json.loads((RUNS / run_id / "gates" / "planning-gate" /
                       "failure-envelope.json").read_text(encoding="utf-8"))
    before = store_content(run_id)
    code, out = run("rollback", "--run-id", run_id, "--gate", GATE, "--owner-role", decider,
                    "--decided-by", "verify_recovery", expect=2)
    r.add("D26", "with the budget spent a further rollback is refused, the store is "
                 "unchanged, and the envelope says so instead of naming the command",
          "RUNTIME FAILURE" in out and "further attempt" in out
          and store_content(run_id) == before
          and runtime_command(env3["clearing_action"]) is None
          and "no further attempt" in env3["clearing_action"],
          next((ln.strip() for ln in out.splitlines() if "RUNTIME FAILURE" in ln), "")[:120]
          + f"; store unchanged={store_content(run_id) == before}; envelope action: "
          + env3["clearing_action"][:80])
    check_clearing_actions(r, "D27", [run_id])
    return r


# --------------------------------------------------------------------------- policy closure


def scenario_policy() -> Result:
    """Every row of the classification matrix, checked against the policy it declares.

    The three runs above exercise three classes. This closes over the rest without inventing
    runs for failures no current path can raise: for every row, the action the Recovery
    Controller chooses must agree with what the row declares, and a non-retryable row must
    never produce a retry at any point in the budget.
    """
    r = Result("Policy closure -- no failure class is retried against its own declaration")
    wrong_action, retried_anyway = [], []
    for cls_name, pol in rp.FAILURE_POLICIES.items():
        for attempt in range(0, 5):
            c = rp.classify(cls_name, attempt=attempt, attempts_lost=0,
                            max_attempts=rp.RETRY_PROFILE["max_attempts"],
                            seed=f"seed-{cls_name}")
            if not pol.retryable:
                if c.action != pol.default_action:
                    wrong_action.append((cls_name, attempt, c.action))
                if c.action == rp.RETRY:
                    retried_anyway.append((cls_name, attempt))
            elif attempt < rp.RETRY_PROFILE["max_attempts"] and c.action != rp.RETRY:
                wrong_action.append((cls_name, attempt, c.action))
            elif attempt >= rp.RETRY_PROFILE["max_attempts"] and c.action == rp.RETRY:
                retried_anyway.append((cls_name, attempt))
    r.add("P1", "every classified action matches the matrix row that declares it",
          not wrong_action,
          f"{len(rp.FAILURE_POLICIES)} class(es) checked across 5 attempt positions each"
          if not wrong_action else f"disagreements: {wrong_action}")
    r.add("P2", "no non-retryable class is retried, and no budget is exceeded",
          not retried_anyway,
          "the retry action appears only for retryable classes inside the budget"
          if not retried_anyway else f"retried anyway: {retried_anyway}")

    unknown = rp.classify("something-nobody-rowed", attempt=1, attempts_lost=0,
                          max_attempts=3)
    r.add("P3", "an unclassified failure escalates rather than being treated as transient",
          unknown.action == rp.ESCALATE and unknown.retryable is False,
          f"action={unknown.action}, retryable={unknown.retryable}")

    delays = [rp.backoff_seconds(a, seed="k") for a in range(1, 9)]
    profile = rp.RETRY_PROFILE
    r.add("P4", "backoff grows exponentially and is capped by the profile",
          all(b >= a for a, b in zip(delays, delays[1:]))
          and max(delays) <= profile["max_delay_seconds"],
          f"delays {delays} under base={profile['base_delay_seconds']}s "
          f"x{profile['multiplier']}, cap {profile['max_delay_seconds']}s, jitter "
          f"+-{int(profile['jitter_ratio'] * 100)}%")
    same = {rp.backoff_seconds(2, seed="fixed-key") for _ in range(5)}
    spread = {rp.backoff_seconds(2, seed=f"key-{i}") for i in range(8)}
    r.add("P5", "jitter is stable per work item and spread across work items, so replay "
                "stays deterministic",
          len(same) == 1 and len(spread) > 1,
          f"one key over five evaluations -> {same}; eight keys -> "
          f"{len(spread)} distinct delay(s)")
    return r


# --------------------------------------------------------------------------- main


def main():
    ap = argparse.ArgumentParser(
        description="Prove classified retry, bounded attempts, and structured failure "
                    "envelopes by inducing the failures")
    ap.add_argument("--keep", action="store_true",
                    help="leave the injected run directories and inputs in place as evidence")
    ap.add_argument("--json-out")
    a = ap.parse_args()

    print("Recovery Verification -- induced failure injection")
    print(f"  retry profile: {rp.RETRY_PROFILE}")
    print(f"  conforming source artifact: .claude/"
          f"{GOOD_SOURCE.relative_to(CLAUDE).as_posix()}")
    # Cleanup runs in `finally`: a scenario that raises must still remove the inputs and runs
    # it injected, or they are left under runs/ beside committed evidence.
    try:
        results = [scenario_a(), scenario_b(), scenario_c(), scenario_d(), scenario_policy()]
        admissibility = Result("Clearing actions -- every open envelope of every induced run "
                               "names an action admissible for its work item")
        check_clearing_actions(admissibility, "X1", [
            run_id_for(INPUTS / f"recovery-injection-{slug}.md")
            for slug in ("repaired", "exhausted", "policy", "rollback")])
        results.append(admissibility)
        for res in results:
            print("\n".join(res.render()))

        total = sum(len(r.checks) for r in results)
        passed = sum(1 for r in results for c in r.checks if c["result"] == "pass")
        print()
        print("-" * 100)
        print(f"{passed}/{total} checks passed -- "
              f"{'RECOVERY PROVEN' if passed == total else 'NOT PROVEN'}")

        if a.json_out:
            Path(a.json_out).write_text(json.dumps({
                "schema": "framework.runtime/recovery-verification.v1",
                "verified_at": rp.iso(rp.now()),
                "runtime_version": fr.RUNTIME_VERSION,
                "retry_profile": rp.RETRY_PROFILE,
                "result": "pass" if passed == total else "fail",
                "checks_run": total,
                "checks_passed": passed,
                "scenarios": [{"title": r.title, "checks": r.checks} for r in results],
            }, indent=2), encoding="utf-8")
    finally:
        if not a.keep:
            for slug in ("repaired", "exhausted", "policy", "rollback"):
                path = INPUTS / f"recovery-injection-{slug}.md"
                if path.exists():
                    reset_run(run_id_for(path), path)
                    path.unlink()
            print("injected runs and inputs removed; pass --keep to retain them as evidence")
    return 0 if passed == total else 1


if __name__ == "__main__":
    sys.exit(main())
