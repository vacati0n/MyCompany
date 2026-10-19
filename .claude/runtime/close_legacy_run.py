#!/usr/bin/env python3
"""Close a run created by runtime 0.2.0, under the runtime that replaced it.

Runtime 0.2.0 executed one phase per run and closed it with `complete --run-id`. Runtime
0.3.0 replaced that with a persisted multi-phase state store, one work item per phase, and
`complete --run-id --phase`. A 0.2.0 run directory carries no `state.json`, so the 0.3.0
command cannot load it and the run is left without its terminal evidence.

This closer exists for exactly that case and nothing else. It is a migration utility, not
a second execution path:

  - it never invokes an agent and never writes an artifact;
  - it validates the artifact already on disk through the registered Validation Engine,
    with the frozen envelope, exactly as `complete` did;
  - it emits its events through the runtime's own `RunLedger`, so every event is canonical
    and the stream stays append-only. The failed first validation is preserved, not
    rewritten;
  - it refuses any run that already carries a 0.3.0 state store, and any run whose
    artifact does not pass validation.

Once no 0.2.0 run directory remains, delete this file.

Usage:
    python .claude/runtime/close_legacy_run.py --run-id run-xxxxxxxxxxxx
"""

from __future__ import annotations

import argparse
import fnmatch
import json
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
CLAUDE = HERE.parent
RUNS = CLAUDE / "runs"
sys.path.insert(0, str(HERE))

import framework_runtime as fr   # noqa: E402


def close(run_id: str, note: str) -> int:
    run_dir = RUNS / run_id
    if not (run_dir / "run-ledger.json").exists():
        raise SystemExit(f"no run ledger at {run_dir}")
    if (run_dir / "state.json").exists():
        raise SystemExit(
            f"{run_id} carries a 0.3.0 state store; close it with "
            f"`framework_runtime.py complete --run-id {run_id} --phase <phase>`")

    ledger = fr.RunLedger(run_dir)
    data = ledger.read_ledger()
    state_id = data["state_id"]
    envelope = json.loads((run_dir / "invocation-envelope.json").read_text(encoding="utf-8"))
    result_path = CLAUDE / data["result_envelope_path"]
    result = json.loads(result_path.read_text(encoding="utf-8")) if result_path.exists() else {}

    artifact = CLAUDE / data["artifact_path"]
    if not artifact.exists():
        raise SystemExit(f"no artifact at {data['artifact_path']}; nothing to close")

    validator_name = data.get("validator") or fr.VALIDATORS.get(artifact.name)
    if not validator_name:
        raise SystemExit(f"no validator registered for {artifact.name!r}")
    validator = __import__(validator_name)

    rep = validator.validate(artifact, envelope)
    rd = rep.to_dict()
    (run_dir / "validation-report.json").write_text(json.dumps(rd, indent=2), encoding="utf-8")

    declared = {x.replace(".claude/", "").lstrip("./")
                for x in (result.get("declared_side_effects") or [])}
    permitted = envelope["constraints"]["permitted_writes"]
    undeclared = sorted(d for d in declared
                        if not any(fnmatch.fnmatch(d, w) for w in permitted))

    if not rep.passed or undeclared:
        print(f"NOT CLOSED: validation {rd['result']}, undeclared side effects {undeclared}")
        for c in rd["checks"]:
            if c["result"] == "fail":
                print(f"  FAIL [{c['severity']}] {c['id']} ({c['quality_ref']}): {c['detail']}")
        return 1

    # Idempotent: an event type already recorded after the last rejection is not repeated,
    # so re-running the closer rebuilds the package without inflating the event stream.
    def emitted_since_rejection(*types) -> bool:
        stream = ledger.events()
        last_reject = max((i for i, e in enumerate(stream)
                           if e["event_type"] == "validation_failed"), default=-1)
        return any(e["event_type"] in types for e in stream[last_reject + 1:])

    prior = [e for e in ledger.events() if e["event_type"] == "validation_failed"]
    if prior and not emitted_since_rejection("retry_scheduled"):
        ledger.emit("retry_scheduled", state_id=state_id, actor_type="runtime",
                    actor_id="execution-coordinator",
                    summary=f"run returned to Execution for repair after {len(prior)} rejected "
                            "validation attempt(s)",
                    reason_code="validation_failed",
                    details={"rejected_attempts": [e["event_id"] for e in prior],
                             "recorded_by": "close_legacy_run.py", "note": note})
        ledger.emit("invocation_started", state_id=state_id, actor_type="runtime",
                    actor_id="invocation-gateway",
                    summary=f"repair invocation of agent {data['agent_id']} "
                            f"v{data['agent_version']} through host registration "
                            f"{envelope['host_registration']}",
                    reason_code="execution_started",
                    details={"invocation_id": data["invocation_id"],
                             "attempt": len(prior) + 1, "recorded_by": "close_legacy_run.py"})
        ledger.emit("invocation_completed", state_id=state_id, actor_type="agent",
                    actor_id=data["agent_id"],
                    summary=f"agent returned status {result.get('status', 'unreported')!r} "
                            f"with {len(result.get('artifact_refs') or [])} artifact ref(s) "
                            "after repair",
                    reason_code="output_accepted",
                    details={"invocation_id": result.get("invocation_id", data["invocation_id"]),
                             "declared_side_effects": result.get("declared_side_effects"),
                             "confidence": result.get("confidence"),
                             "error_class": result.get("error_class"),
                             "recorded_by": "close_legacy_run.py"})

    if not emitted_since_rejection("validation_passed"):
        ledger.emit("validation_passed", state_id=state_id, actor_type="runtime",
                    actor_id="validation-engine",
                    summary=f"artifact conforms: {rd['checksPassed']}/{rd['checksRun']} "
                            "checks passed",
                    reason_code="output_accepted",
                    details={"report": "validation-report.json", "validator": validator_name,
                             "counts": rd["counts"], "recorded_by": "close_legacy_run.py"})

    package = build_package(data, envelope, result, rd, ledger.events(), note, validator_name)
    (run_dir / "completion-package.md").write_text(package, encoding="utf-8")

    if not emitted_since_rejection("aggregation_completed"):
        ledger.emit("aggregation_completed", state_id=state_id, actor_type="runtime",
                    actor_id="output-aggregator",
                    summary="completion package and provenance manifest rebuilt at closure",
                    reason_code="output_accepted",
                    details={"package": "completion-package.md",
                             "recorded_by": "close_legacy_run.py"})

    data["execution_completed_at"] = fr.now()
    data["status"] = "Completed"
    data["closed_by"] = "close_legacy_run.py"
    data["closure_note"] = note
    data["validation"] = {
        "result": rd["result"],
        "checksRun": rd["checksRun"],
        "checksPassed": rd["checksPassed"],
        "blockingFailures": rd["blockingFailures"],
        "correctableFailures": rd["correctableFailures"],
        "notMachineCheckable": rd["notMachineCheckable"],
        "counts": rd["counts"],
        "undeclaredSideEffects": undeclared,
        "validator": validator_name,
    }
    data["agent_result_status"] = result.get("status")
    ledger.write_ledger(data)

    if not emitted_since_rejection("run_completed"):
        ledger.emit("run_completed", state_id=state_id, actor_type="runtime",
                    actor_id="execution-coordinator",
                    summary="run terminal status Completed",
                    reason_code="output_accepted",
                    details={"artifact": data["artifact_path"],
                             "recorded_by": "close_legacy_run.py"})

    print(f"run_id      : {run_id}")
    print(f"agent       : {data['agent_id']} v{data['agent_version']}")
    print(f"artifact    : .claude/{data['artifact_path']} ({artifact.stat().st_size} bytes)")
    print(f"validation  : {rd['result'].upper()}  "
          f"({rd['checksPassed']}/{rd['checksRun']} checks passed, "
          f"{rd['notMachineCheckable']} not machine-checkable)")
    print(f"run status  : {data['status']}")
    return 0


def build_package(data, envelope, result, rd, events, note, validator_name) -> str:
    lines = [
        f"# Completion Package: {data['run_id']}",
        "",
        "Rebuilt at closure by `runtime/close_legacy_run.py`. The run executed under runtime",
        f"{data['runtime_version']} and was closed under {fr.RUNTIME_VERSION}, whose",
        "multi-phase `complete` cannot load a run directory that carries no state store.",
        "",
        f"Closure note: {note}",
        "",
        "## Execution Summary",
        "",
        "| Field | Value |",
        "|---|---|",
        f"| run_id | `{data['run_id']}` |",
        f"| slice | `{data['slice']}` |",
        f"| command | `/{data['command_id']}` |",
        f"| workflow | `{data['workflow_id']}` v{data['workflow_version']} |",
        f"| state_id (phase) | `{data['state_id']}` |",
        f"| agent_id | `{data['agent_id']}` |",
        f"| agent_version | `{data['agent_version']}` |",
        f"| adapter | `{data['adapter']}` / `{data.get('dispatch_mode')}` |",
        f"| invocation_id | `{data['invocation_id']}` |",
        f"| execution started | `{data['execution_started_at']}` |",
        f"| execution completed | `{data['execution_completed_at'] or fr.now()}` |",
        f"| input digest | `{data['input_digest']}` |",
        f"| context digest | `{data['context_digest']}` |",
        f"| agent result status | `{result.get('status', 'unreported')}` |",
        f"| validation | `{rd['result']}` ({rd['checksPassed']}/{rd['checksRun']} checks passed) |",
        "",
        "## Supplied Inputs",
        "",
        "| Declared type | Reference | Digest |",
        "|---|---|---|",
    ]
    for s in envelope["context_slice"].get("inputs", []):
        lines.append(f"| `{s['type']}` | `{s['reference']}` | `{s['digest']}` |")

    lines += [
        "",
        "## Module Provenance",
        "",
        "Modules loaded by the agent, in the order declared by its manifest.",
        "",
        "| # | Module | Digest |",
        "|---|---|---|",
    ]
    digests = envelope["capability_bindings"]["module_digests"]
    for i, p in enumerate(envelope["capability_bindings"]["load_order"], 1):
        lines.append(f"| {i} | `{p}` | `{digests[p]}` |")

    lines += [
        "",
        "## Artifact Ledger",
        "",
        "| Artifact | Path | Declared by |",
        "|---|---|---|",
        f"| {Path(data['artifact_path']).stem} | `{data['artifact_path']}` "
        f"| agent output contract |",
    ]
    for ref in sorted(result.get("artifact_refs") or []):
        rel = ref.replace(".claude/", "").lstrip("./")
        if rel != data["artifact_path"]:
            lines.append(f"| {Path(rel).stem} | `{rel}` | agent conditional output contract |")
    lines += [
        f"| result envelope | `{data['result_envelope_path']}` "
        f"| execution-engine result contract |",
        f"| validation report | `runs/{data['run_id']}/validation-report.json` "
        f"| validation engine |",
        "",
        "## Validation Outcome",
        "",
        "| Metric | Value |",
        "|---|---|",
        f"| validator | `{validator_name}.py` |",
        f"| result | {rd['result']} |",
        f"| checks run | {rd['checksRun']} |",
        f"| checks passed | {rd['checksPassed']} |",
        f"| blocking failures | {rd['blockingFailures']} |",
        f"| correctable failures | {rd['correctableFailures']} |",
        f"| declared not machine-checkable | {rd['notMachineCheckable']} |",
        f"| design counts | `{rd['counts']}` |",
        "",
        "## Event Stream",
        "",
        "| # | Event | Actor | Reason | Summary |",
        "|---|---|---|---|---|",
    ]
    for e in events:
        lines.append(f"| {e['event_id']} | `{e['event_type']}` | {e['actor_type']}:{e['actor_id']} "
                     f"| `{e['reason_code']}` | {e['summary']} |")

    lines += [
        "",
        "## Gate Position",
        "",
        f"This package is the evidence assessed at the `{data['workflow_id']}` gate that closes",
        f"state `{data['state_id']}`. The runtime does not approve that gate; approval remains",
        "with the owners named in `workflows/workflow-gate-matrix.md`, including acceptance of",
        "the decision records this run emitted at status `Proposed`.",
        "",
        "## Residual Items",
        "",
        "- Decision records emitted by this run are at `Proposed` and are unaccepted.",
        "- This run predates the multi-phase state store, so it carries no work item per phase",
        "  and no gate work item. It is a single-state run and its evidence is scoped to that",
        "  state.",
        "",
    ]
    return "\n".join(lines)


def main():
    ap = argparse.ArgumentParser(description="Close a runtime 0.2.0 run under a later runtime")
    ap.add_argument("--run-id", required=True)
    ap.add_argument("--note", default="closed after an in-place repair pass by the owning agent")
    a = ap.parse_args()
    return close(a.run_id, a.note)


if __name__ == "__main__":
    sys.exit(main())
