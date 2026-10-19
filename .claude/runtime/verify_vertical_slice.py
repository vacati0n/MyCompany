#!/usr/bin/env python3
"""Executable validation for the framework's vertical slices.

Proves, or fails to prove, a chain of the shape:

    /<command> -> <workflow> -> <phase> -> <agent>
               -> REAL EXECUTION -> <artifact> -> VALIDATION PASS

Five slices are wired today:

    slice 1  /implement -> implement-feature -> execution-planning
                        -> planner   -> execution-plan.md
    slice 2  /implement -> implement-feature -> solution-design-and-risk-assessment
                        -> architect -> technical-design.md
    slice 3  /implement -> implement-feature -> implementation
                        -> dev-1-implement -> implementation-report.md
    slice 4  /implement -> implement-feature -> scope-and-acceptance
                        -> product-owner -> scope-definition.md
    slice 5  /implement -> implement-feature -> quality-review
                        -> dev-2-reviewer -> review-package.md

This is deliberately narrow. It does not turn `validation/framework-validation-checklist.md`
into executable code; it adds only the checks needed to decide whether a slice is real.
Every check either passes against live repository and run state, or fails. Nothing is
asserted from a specification document alone: checks C5 to C9 read the run ledger, the
event stream, and the produced artifact.

Usage:
    python .claude/runtime/verify_vertical_slice.py [--slice architect]
    python .claude/runtime/verify_vertical_slice.py [--run-id run-xxxxxxxxxxxx] [--json-out F]

With `--run-id`, the slice is taken from that run's ledger. With neither argument, the
most recently modified run under `.claude/runs/` is used. Exit code 0 only when every
check passes.
"""

from __future__ import annotations

import argparse
import fnmatch
import json
import re
import sys
from pathlib import Path

import yaml

HERE = Path(__file__).resolve().parent
CLAUDE = HERE.parent
RUNS = CLAUDE / "runs"
sys.path.insert(0, str(HERE))

import framework_runtime as fr   # noqa: E402

SLICE_CONFIG = {
    "planner": {
        "agent": "planner",
        "command": "implement",
        "phase": "execution-planning",
        "artifact": "execution-plan.md",
        "min_bytes": 2000,
    },
    "architect": {
        "agent": "architect",
        "command": "implement",
        "phase": "solution-design-and-risk-assessment",
        "artifact": "technical-design.md",
        "min_bytes": 2000,
    },
    "implement": {
        "agent": "dev-1-implement",
        "command": "implement",
        "phase": "implementation",
        "artifact": "implementation-report.md",
        "min_bytes": 2000,
    },
    "product-owner": {
        "agent": "product-owner",
        "command": "implement",
        "phase": "scope-and-acceptance",
        "artifact": "scope-definition.md",
        "min_bytes": 2000,
    },
    "reviewer": {
        "agent": "dev-2-reviewer",
        "command": "implement",
        "phase": "quality-review",
        "artifact": "review-package.md",
        "min_bytes": 2000,
    },
    # The one documentation phase whose Phase Model row names its output as a file. The other
    # four state it in prose, so they resolve as far as registration and hold at the output
    # contract; a slice pinned to one of them would be proving a phase the runtime cannot
    # dispatch.
    "documentation": {
        "agent": "documentation",
        "command": "release",
        "phase": "communication-and-post-release",
        "artifact": "release-note.md",
        "min_bytes": 2000,
    },
}

REQUIRED_EVIDENCE_FIELDS = [
    "command_id", "workflow_id", "agent_id", "agent_version",
    "execution_started_at", "execution_completed_at",
    "input_digest", "artifact_path", "validation",
]

# Canonical events that must appear for the phase under test, scoped to its work item.
REQUIRED_PHASE_EVENTS = [
    "context_hydrated", "work_item_enqueued", "work_item_leased",
    "invocation_started", "invocation_completed", "validation_passed",
]

# Canonical events that must appear once for the run that contains the phase. Note that
# `run_completed` is deliberately absent: a multi-phase run completes only when every one
# of its phases has, so requiring it here would make one proven phase depend on phases that
# have no registered capability yet.
REQUIRED_RUN_EVENTS = ["run_initialized", "aggregation_completed"]


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


def latest_run() -> Path | None:
    cands = [p for p in RUNS.glob("run-*") if (p / "run-ledger.json").exists()]
    if not cands:
        return None
    return max(cands, key=lambda p: (p / "run-ledger.json").stat().st_mtime)


def evidence_for(run_dir: Path | None, cfg: dict):
    """Locate the per-phase evidence for one slice inside a run.

    Two layouts exist. A multi-phase run keeps one ledger per phase under
    `states/<phase>/`; a single-phase run written before the state engine kept exactly one
    ledger at the run root. Both are read here, so proofs recorded against earlier runs
    stay verifiable.
    """
    if run_dir is None:
        return None, None
    state_dir = run_dir / "states" / cfg["phase"]
    if (state_dir / "state-ledger.json").exists():
        return json.loads((state_dir / "state-ledger.json").read_text(encoding="utf-8")), \
            state_dir
    root = run_dir / "run-ledger.json"
    if root.exists():
        data = json.loads(root.read_text(encoding="utf-8"))
        if data.get("state_id") == cfg["phase"]:
            return data, run_dir
    return None, None


def run_has_slice(run_dir: Path, cfg: dict) -> bool:
    led, _ = evidence_for(run_dir, cfg)
    return led is not None and led.get("agent_id") == cfg["agent"]


def slice_for(run_dir: Path | None, requested: str | None) -> dict:
    if requested:
        if requested not in SLICE_CONFIG:
            raise SystemExit(f"unknown slice {requested!r}; known: {sorted(SLICE_CONFIG)}")
        return SLICE_CONFIG[requested]
    if run_dir:
        for cfg in SLICE_CONFIG.values():
            if run_has_slice(run_dir, cfg):
                return cfg
    return SLICE_CONFIG["planner"]


def normalized_shingles(text: str, n: int = 12):
    words = re.findall(r"[a-z0-9]+", text.lower())
    return {" ".join(words[i:i + n]) for i in range(max(0, len(words) - n + 1))}


def run(run_dir: Path | None, cfg: dict) -> Result:
    r = Result()
    agent_id = cfg["agent"]
    command_id = cfg["command"]
    phase_id = cfg["phase"]
    artifact_name = cfg["artifact"]

    # --------------------------------------------------------------- C1 registration
    try:
        reg = yaml.safe_load((CLAUDE / "registry/agents.yaml").read_text(encoding="utf-8"))
        rec = next((x for x in (reg.get("records") or []) if x["identifier"] == agent_id), None)
        ok = bool(rec) and rec["status"] == "active" \
            and (CLAUDE / rec["specificationPath"]).exists()
        r.add("C1", f"{agent_id} is registered in registry/agents.yaml", ok,
              f"record found, status={rec['status']}, specificationPath="
              f"{rec['specificationPath']} resolves" if ok else "no active resolvable record")
    except Exception as exc:  # noqa: BLE001
        r.add("C1", f"{agent_id} is registered in registry/agents.yaml", False, repr(exc))
        rec = None

    # --------------------------------------------------------------- C2 host registration
    host_rel = f"agents/{agent_id}.agent.md"
    host_path = CLAUDE / host_rel
    host_body, fm = "", {}
    if not host_path.exists():
        r.add("C2", f"{agent_id} host registration exists and is host-compatible", False,
              f"{host_rel} not found")
    else:
        raw = host_path.read_text(encoding="utf-8")
        m = re.match(r"^---\s*\n(.*?)\n---\s*\n(.*)$", raw, re.S)
        if not m:
            r.add("C2", f"{agent_id} host registration exists and is host-compatible", False,
                  "no YAML frontmatter")
        else:
            fm = yaml.safe_load(m.group(1)) or {}
            host_body = m.group(2)
            ok = fm.get("name") == agent_id and bool(str(fm.get("description", "")).strip())
            r.add("C2", f"{agent_id} host registration exists and is host-compatible", ok,
                  f"{host_rel}: name={fm.get('name')!r}, tools={fm.get('tools')!r}, "
                  f"description {len(str(fm.get('description','')))} chars"
                  if ok else f"frontmatter invalid: {fm}")

    # --------------------------------------------------------------- C3 load order
    agent = None
    try:
        agent = fr.load_agent(agent_id)
        order = [m["path"] for m in agent["modules"]]
        declared = agent["load_order"]
        ok = len(order) == len(declared) and all((CLAUDE / p).exists() for p in order)
        r.add("C3", f"{agent_id} module load order resolves from the manifest", ok,
              f"{len(order)} module(s) in declared order: {declared}")
    except Exception as exc:  # noqa: BLE001
        r.add("C3", f"{agent_id} module load order resolves from the manifest", False, repr(exc))

    # --------------------------------------------------------------- C4 skills
    try:
        wf = fr.resolve_workflow(fr.resolve_command(command_id)["primaryWorkflow"])
        required = fr.resolve_phase(wf, phase_id)["required_skills"]
        skills = fr.resolve_skills(agent)
        phase_skills = fr.resolve_phase_skills(required)
        ok = all(s["resolved"] for s in skills) and all(s["resolved"] for s in phase_skills)
        r.add("C4", f"{agent_id} manifest skills and phase-mandatory skills resolve", ok,
              "manifest: " + ", ".join(f"{s['skillCode']}->{s['registryIdentifier']}" for s in skills)
              + " | phase: " + ", ".join(f"{s['skillCode']}={s['status']}" for s in phase_skills))
    except Exception as exc:  # noqa: BLE001
        r.add("C4", f"{agent_id} manifest skills and phase-mandatory skills resolve", False, repr(exc))

    # --------------------------------------------------------------- C5 invocable
    try:
        chain = fr.resolve_chain(command_id, phase_id)
        ok = (chain["command"]["identifier"] == command_id
              and chain["workflow"]["identifier"] == chain["command"]["primaryWorkflow"]
              and chain["phase"]["owner_agent"] == agent_id
              and chain["output"]["artifact"] == artifact_name
              and chain["agent"]["host_registration"]["registered"])
        r.add("C5", f"{agent_id} is invocable: the full routing chain resolves to a registered "
                    "host entry point", ok,
              f"/{command_id} -> {chain['workflow']['identifier']} -> {chain['phase']['phase']} "
              f"-> {chain['phase']['owner_agent']} -> {chain['output']['artifact']} "
              f"-> host {chain['agent']['host_registration']['path']}")
    except Exception as exc:  # noqa: BLE001
        r.add("C5", f"{agent_id} is invocable: the full routing chain resolves to a registered "
                    "host entry point", False, repr(exc))

    # --------------------------------------------------------------- run-scoped checks
    ledger, evidence_dir = evidence_for(run_dir, cfg)
    if ledger is None:
        for cid, title in [
            ("C6", f"{agent_id} executed: the run ledger records a completed real invocation"),
            ("C7", f"{artifact_name} was produced at the declared artifact path"),
            ("C8", f"the artifact conforms to the {agent_id} output and quality contract"),
            ("C9", "execution evidence exists and is complete"),
            ("C10", f"no {agent_id} contract duplication exists"),
        ]:
            r.add(cid, title, False,
                  f"no run under .claude/runs/ carries evidence for phase {phase_id}")
        return r

    events = [json.loads(l) for l in
              (run_dir / "events.jsonl").read_text(encoding="utf-8").splitlines() if l.strip()]
    envelope = json.loads(
        (evidence_dir / "invocation-envelope.json").read_text(encoding="utf-8"))
    work_item_id = ledger.get("work_item_id", f"{run_dir.name}::{phase_id}")
    phase_events = [e for e in events if e.get("work_item_id") == work_item_id]

    # --------------------------------------------------------------- C6 executed
    result_path = CLAUDE / ledger["result_envelope_path"]
    result = json.loads(result_path.read_text(encoding="utf-8")) if result_path.exists() else {}
    types = [e["event_type"] for e in phase_events]
    agent_events = [e for e in phase_events if e["actor_type"] == "agent"]
    drift = []
    if agent:
        current = {m["path"]: m["digest"] for m in agent["modules"]}
        recorded = envelope["capability_bindings"]["module_digests"]
        drift = [p for p, d in recorded.items() if current.get(p) != d]
    # GD-001 (`config/self-hosting-profile.md#point-in-time-evidence-rule`): this check proves
    # that an invocation happened, which is a fact about the past. Module digest drift means
    # the agent's contract has changed *since* — current-state information, not evidence that
    # the historical invocation was defective. It is reported, never failed on. The envelope
    # still records exactly which modules were loaded, so the proof stays auditable.
    ok = ("invocation_started" in types and "invocation_completed" in types
          and result.get("status") == "succeeded"
          and bool(agent_events)
          and ledger["agent_id"] == agent_id)
    drift_note = (f"; module digest drift since execution (informational, GD-001): {drift}"
                  if drift else "; module digests unchanged since execution")
    r.add("C6", f"{agent_id} executed: the run ledger records a completed real invocation", ok,
          f"invocation_id={ledger['invocation_id']}, adapter={ledger['adapter']}"
          f"/{ledger.get('dispatch_mode')}, agent result status={result.get('status')!r}, "
          f"agent-actor events={len(agent_events)}{drift_note}")

    # --------------------------------------------------------------- C7 artifact
    artifact = CLAUDE / ledger["artifact_path"]
    exists = artifact.exists()
    size = artifact.stat().st_size if exists else 0
    declared_effects = set(result.get("declared_side_effects") or [])
    permitted = envelope["constraints"]["permitted_writes"]
    undeclared = sorted(d for d in {x.replace(".claude/", "").lstrip("./") for x in declared_effects}
                        if not fr.permitted_write(d, envelope))
    ok = exists and size > cfg["min_bytes"] and not undeclared
    r.add("C7", f"{artifact_name} was produced at the declared artifact path", ok,
          f"{ledger['artifact_path']} ({size} bytes); declared side effects="
          f"{sorted(declared_effects)}; undeclared={undeclared or 'none'}")

    # --------------------------------------------------------------- C8 conformance
    if exists:
        validator_name = ledger.get("validator") or fr.VALIDATORS.get(artifact_name)
        validator = __import__(validator_name)
        rep = validator.validate(artifact, envelope).to_dict()
        ok = rep["result"] == "pass"
        r.add("C8", f"the artifact conforms to the {agent_id} output and quality contract", ok,
              f"{validator_name}: {rep['checksPassed']}/{rep['checksRun']} checks passed, "
              f"{rep['blockingFailures']} blocking, {rep['correctableFailures']} correctable, "
              f"{rep['notMachineCheckable']} declared not-machine-checkable; counts={rep['counts']}"
              + ("" if ok else "; failures="
                 + str([c["id"] for c in rep["checks"] if c["result"] == "fail"])))
    else:
        r.add("C8", f"the artifact conforms to the {agent_id} output and quality contract", False,
              "no artifact to validate")

    # --------------------------------------------------------------- C9 evidence
    missing = [f for f in REQUIRED_EVIDENCE_FIELDS if not ledger.get(f)]
    run_types = [e["event_type"] for e in events]
    missing_events = [e for e in REQUIRED_PHASE_EVENTS if e not in types] \
        + [e for e in REQUIRED_RUN_EVENTS if e not in run_types]
    package = run_dir / "completion-package.md"
    ok = (not missing and not missing_events and package.exists()
          and (evidence_dir / "validation-report.json").exists())
    r.add("C9", "execution evidence exists and is complete", ok,
          f"{len(phase_events)} event(s) for {work_item_id} of {len(events)} in the run; "
          f"missing evidence fields={missing or 'none'}; "
          f"missing canonical events={missing_events or 'none'}; "
          f"completion package={'present' if package.exists() else 'ABSENT'}")

    # --------------------------------------------------------------- C10 no duplication
    if agent and host_body:
        adapter_shingles = normalized_shingles(host_body)
        overlaps = {}
        for mod in agent["modules"]:
            mod_sh = normalized_shingles((CLAUDE / mod["path"]).read_text(encoding="utf-8"))
            shared = adapter_shingles & mod_sh
            if shared:
                overlaps[mod["path"]] = sorted(shared)[:3]
        ok = not overlaps
        r.add("C10", f"no {agent_id} contract duplication exists", ok,
              "host adapter shares no 12-word sequence with any authoritative module "
              f"({len(agent['modules'])} module(s) compared)" if ok
              else f"verbatim overlap detected: {overlaps}")
    else:
        r.add("C10", f"no {agent_id} contract duplication exists", False,
              "could not compare adapter against module set")

    return r


def main():
    ap = argparse.ArgumentParser(description="Verify a framework vertical slice")
    ap.add_argument("--slice", choices=sorted(SLICE_CONFIG),
                    help="which slice to verify; inferred from --run-id when omitted")
    ap.add_argument("--run-id")
    ap.add_argument("--json-out")
    a = ap.parse_args()

    run_dir = (RUNS / a.run_id) if a.run_id else latest_run()
    cfg = slice_for(run_dir, a.slice)
    if not a.run_id and not (run_dir and run_has_slice(run_dir, cfg)):
        # Prefer a run that actually carries evidence for the requested slice.
        for cand in sorted(RUNS.glob("run-*"), key=lambda p: p.stat().st_mtime, reverse=True):
            if run_has_slice(cand, cfg):
                run_dir = cand
                break
    res = run(run_dir, cfg)

    print(f"Vertical Slice Validation: /{cfg['command']} -> {cfg['phase']} -> "
          f"{cfg['agent']} -> {cfg['artifact']}")
    print(f"run: {run_dir.name if run_dir else '(none)'}")
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
            "slice": cfg,
            "run": run_dir.name if run_dir else None,
            "verdict": verdict,
            "passed": n_pass,
            "total": len(res.checks),
            "checks": res.checks,
        }, indent=2), encoding="utf-8")

    return 0 if res.passed else 1


if __name__ == "__main__":
    sys.exit(main())
