#!/usr/bin/env python3
"""Registry coverage verification for the operational surface.

Answers four questions about the registered surface, with counts and an explicit unresolved
count for each, using the same resolvers the runtime itself uses so that this report cannot
agree with a runtime that would disagree:

  C1  Does every active command record resolve to exactly one active workflow record?
  C2  Does every active workflow publish a machine-resolvable Phase Model table?
  C3  Is every phase owner named by those tables host-invocable?
  C4  Does every phase-mandatory skill resolve to an active skill registry record?

Two further checks cover what makes a phase dispatchable rather than merely registered, so a
run of this script never reports coverage as capability:

  C5  Every gate a Phase Model names resolves to owners, at least one of whom does not
      produce the evidence that gate assesses.
  C6  Per phase, the full G1-CAPABILITY chain verdict, counted rather than summarised.

Two more cover the model tier declaration (`config/model-tier-policy.json`):

  C7  Every phase of every active Phase Model has exactly one declared tier, no entry names a
      phase that does not exist, and at least 3 of the 6 implement-feature phases are non-deep.
  C8  Escalation is monotonic: over every declared tier and every recorded rejection count a
      phase never resolves lower, rises by one tier per rejection, stops at deep, and resolves
      identically when asked twice.

Usage:
    python .claude/runtime/verify_registry_coverage.py
    python .claude/runtime/verify_registry_coverage.py --json-out coverage.json
    python .claude/runtime/verify_registry_coverage.py --markdown-out report.md

Exit code 0 when every check passes, 1 otherwise.
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

import framework_runtime as fr  # noqa: E402

CLAUDE = fr.CLAUDE


class Result:
    def __init__(self):
        self.checks = []

    def add(self, cid: str, check: str, passed: bool, counts: dict, detail: str,
            rows: list | None = None):
        self.checks.append({
            "id": cid, "check": check, "result": "PASS" if passed else "FAIL",
            "counts": counts, "detail": detail, "rows": rows or [],
        })

    @property
    def passed(self) -> bool:
        return all(c["result"] == "PASS" for c in self.checks)


def active(registry: str) -> list:
    reg = fr.load_yaml(registry)
    return [r for r in (reg.get("records") or []) if r.get("status") == "active"]


def all_records(registry: str) -> list:
    return fr.load_yaml(registry).get("records") or []


def command_specs_on_disk() -> list:
    """Command specification files, excluding the index documents and grouped host commands."""
    out = []
    for p in sorted((CLAUDE / "commands").glob("*.md")):
        if p.stem in ("README", "command-catalog"):
            continue
        out.append(f"commands/{p.name}")
    return out


def c1_commands_resolve(res: Result):
    cmds = active("registry/commands.yaml")
    wf_by_id = {r["identifier"]: r for r in all_records("registry/workflows.yaml")}
    rows, unresolved = [], 0
    for c in cmds:
        wf = wf_by_id.get(c["primaryWorkflow"])
        spec_ok = (CLAUDE / c["specificationPath"]).exists()
        wf_active = bool(wf) and wf["status"] == "active"
        wf_spec_ok = bool(wf) and (CLAUDE / wf["specificationPath"]).exists()
        ok = spec_ok and wf_active and wf_spec_ok
        if not ok:
            unresolved += 1
        rows.append({
            "command": c["identifier"], "primary_workflow": c["primaryWorkflow"],
            "workflow_status": wf["status"] if wf else "missing",
            "command_spec_exists": spec_ok, "workflow_spec_exists": wf_spec_ok,
            "resolved": ok,
        })
    specs = command_specs_on_disk()
    registered_specs = {c["specificationPath"] for c in all_records("registry/commands.yaml")}
    unregistered_specs = [s for s in specs if s not in registered_specs]
    res.add("C1", "every active command record resolves to exactly one active workflow record",
            unresolved == 0 and not unregistered_specs,
            {"active_commands": len(cmds), "resolved": len(cmds) - unresolved,
             "unresolved": unresolved, "command_specs_on_disk": len(specs),
             "specs_without_a_record": len(unregistered_specs)},
            f"{len(cmds) - unresolved}/{len(cmds)} active command record(s) resolve; "
            + (f"specs without a record: {unregistered_specs}" if unregistered_specs
               else "every command specification on disk carries a record"),
            rows)


def c2_phase_models(res: Result):
    wfs = active("registry/workflows.yaml")
    section = (fr.load_yaml("registry/workflows.yaml").get("automation", {})
               .get("resolution", {}).get("phaseModelSection", "## Phase Model"))
    rows, unresolved = [], 0
    for w in wfs:
        try:
            phases = fr.parse_phase_model(w["specificationPath"])
            rows.append({"workflow": w["identifier"], "phase_count": len(phases),
                         "phases": [p["phase"] for p in phases], "resolved": True})
        except fr.RuntimeError_ as exc:
            unresolved += 1
            rows.append({"workflow": w["identifier"], "phase_count": 0, "phases": [],
                         "resolved": False, "detail": str(exc)})
    res.add("C2", f"every active workflow publishes a `{section}` table the Task Router can read",
            unresolved == 0,
            {"active_workflows": len(wfs), "with_phase_model": len(wfs) - unresolved,
             "unresolved": unresolved,
             "phases_total": sum(r["phase_count"] for r in rows)},
            f"{len(wfs) - unresolved}/{len(wfs)} active workflow(s) publish a Phase Model, "
            f"{sum(r['phase_count'] for r in rows)} phase(s) in total",
            rows)


def phase_rows() -> list:
    """Every (workflow, phase row) pair the active registry routes."""
    out = []
    for w in active("registry/workflows.yaml"):
        try:
            for r in fr.parse_phase_model(w["specificationPath"]):
                out.append((w, r))
        except fr.RuntimeError_:
            continue
    return out


def host_registration(agent_id: str) -> dict:
    """Host invocability, judged exactly as the runtime's host-subagent adapter judges it."""
    rel = f"agents/{agent_id}.agent.md"
    path = CLAUDE / rel
    if not path.exists():
        return {"registered": False, "path": None, "detail": "no entry point file"}
    import re

    import yaml
    raw = path.read_text(encoding="utf-8")
    m = re.match(r"^---\s*\n(.*?)\n---\s*\n", raw, flags=16)  # re.S
    if not m:
        return {"registered": False, "path": rel, "detail": "no frontmatter"}
    try:
        fm = yaml.safe_load(m.group(1)) or {}
    except yaml.YAMLError as exc:
        return {"registered": False, "path": rel,
                "detail": f"frontmatter does not parse: {exc.__class__.__name__}"}
    if fm.get("name") != agent_id:
        return {"registered": False, "path": rel,
                "detail": f"frontmatter name {fm.get('name')!r} != {agent_id!r}"}
    return {"registered": True, "path": rel, "detail": "frontmatter parses and name matches",
            "tools": fm.get("tools"), "model": fm.get("model")}


def c3_owners_invocable(res: Result):
    owners = {}
    for w, r in phase_rows():
        owners.setdefault(r["owner agent"], []).append(f"{w['identifier']}/{r['phase']}")
    rows, unresolved = [], 0
    for agent_id in sorted(owners):
        host = host_registration(agent_id)
        if not host["registered"]:
            unresolved += 1
        rows.append({"agent": agent_id, "phases_owned": len(owners[agent_id]),
                     "phase_refs": owners[agent_id], "entry_point": host["path"],
                     "host_invocable": host["registered"], "detail": host["detail"]})
    res.add("C3", "every phase owner named by an active Phase Model is host-invocable",
            unresolved == 0,
            {"phase_owners": len(owners), "host_invocable": len(owners) - unresolved,
             "unresolved": unresolved},
            f"{len(owners) - unresolved}/{len(owners)} phase owner(s) resolve to a valid "
            f"`agents/<agent-id>.agent.md` entry point",
            rows)


def c4_phase_skills(res: Result):
    rows, unresolved_codes = [], set()
    total = 0
    for w, r in phase_rows():
        codes = [s.strip() for s in (r.get("required skills") or "").split(",") if s.strip()]
        resolved = fr.resolve_phase_skills(codes)
        total += len(codes)
        bad = [s["skillCode"] for s in resolved if not s["resolved"]]
        unresolved_codes.update(bad)
        rows.append({"workflow": w["identifier"], "phase": r["phase"], "required": codes,
                     "unresolved": bad, "resolved": not bad})
    unresolved_refs = sum(len(r["unresolved"]) for r in rows)
    res.add("C4", "every phase-mandatory skill resolves to an active skill registry record",
            unresolved_refs == 0,
            {"skill_references": total, "resolved": total - unresolved_refs,
             "unresolved": unresolved_refs,
             "unresolved_codes": sorted(unresolved_codes)},
            f"{total - unresolved_refs}/{total} phase skill reference(s) resolve"
            + (f"; unresolved codes {sorted(unresolved_codes)}" if unresolved_codes else ""),
            rows)


def c5_gates_decidable(res: Result):
    rows, unresolved = [], 0
    total = 0
    for w, r in phase_rows():
        owners_by_gate = fr.parse_gate_matrix(w["identifier"])
        for gate in fr.phase_gates(r):
            total += 1
            gate_owners = owners_by_gate.get(gate, [])
            producer = fr.producer_aliases(r["owner agent"])
            deciders = [o for o in gate_owners if o not in producer]
            ok = bool(deciders)
            if not ok:
                unresolved += 1
            rows.append({"workflow": w["identifier"], "phase": r["phase"], "gate": gate,
                         "owners": gate_owners, "producer": r["owner agent"],
                         "eligible_deciders": deciders, "decidable": ok})
    res.add("C5", "every gate a Phase Model names resolves to a non-producing owner",
            unresolved == 0,
            {"gate_references": total, "decidable": total - unresolved,
             "unresolved": unresolved},
            f"{total - unresolved}/{total} gate reference(s) carry an owner permitted to "
            f"decide under the Producer Exclusion Rule",
            rows)


def c6_dispatchable(res: Result):
    """The G1-CAPABILITY chain per phase. Reported, never asserted: a blocked phase with a
    recorded reason is the framework's accurate state, not a coverage failure."""
    cmd_by_workflow = {}
    for c in active("registry/commands.yaml"):
        cmd_by_workflow.setdefault(c["primaryWorkflow"], c["identifier"])
    rows, dispatchable = [], 0
    for w, r in phase_rows():
        cmd = cmd_by_workflow.get(w["identifier"])
        entry = {"workflow": w["identifier"], "phase": r["phase"],
                 "owner": r["owner agent"], "command": cmd}
        if cmd is None:
            entry.update(dispatchable=False, blocked_reason="no active command routes here",
                         failure_class="policy-failure")
            rows.append(entry)
            continue
        try:
            fr.resolve_chain(cmd, r["phase"])
            if r["phase"] not in fr.CONTEXT_SLICE_PHASE:
                entry.update(dispatchable=False,
                             blocked_reason="awaiting_capability_registration",
                             failure_class="missing-capability-failure",
                             detail=f"no context slice is declared for {r['phase']!r}")
            else:
                entry.update(dispatchable=True, blocked_reason=None, failure_class=None,
                             detail="G1-CAPABILITY and G2-CONTEXT both resolve")
                dispatchable += 1
        except fr.RuntimeError_ as exc:
            entry.update(
                dispatchable=False,
                blocked_reason=fr.BLOCK_REASON_BY_FAILURE_CLASS.get(
                    exc.failure_class, "awaiting_policy_exception"),
                failure_class=exc.failure_class, detail=str(exc))
        rows.append(entry)
    res.add("C6", "phase dispatchability is reported per phase, with a reason for each blocker",
            True,
            {"phases": len(rows), "dispatchable": dispatchable,
             "blocked": len(rows) - dispatchable},
            f"{dispatchable}/{len(rows)} phase(s) resolve the full capability and context "
            f"chain today; every other phase carries a recorded blocked reason",
            rows)


# The request's named categories, resolved to phases, pinned so a quiet edit of the
# declaration cannot move a light phase to deep or a deep phase to light unnoticed.
PINNED_LIGHT = {("implement-feature", "scope-and-acceptance"),
                ("investigate", "technical-discovery"),
                ("implement-feature", "documentation-and-release-handoff"),
                ("release", "artifact-packaging"),
                ("code-quality-scan", "repository-quality-scan")}
PINNED_DEEP = {("implement-feature", "solution-design-and-risk-assessment"),
               ("fix-bug", "root-cause-analysis"),
               ("implement-feature", "implementation"),
               ("implement-feature", "quality-review")}
NON_DEEP_BAR = 3


def c7_tier_declaration(res: Result):
    rows, problems = [], []
    try:
        policy = fr.load_model_tier_policy()
    except fr.RuntimeError_ as exc:
        policy, problems = None, [f"declaration unreadable: {exc}"]
    else:
        if policy is None:
            problems.append(f"{fr.MODEL_TIER_POLICY_REL} is absent")
    routed = {}
    for w, r in phase_rows():
        routed.setdefault(w["identifier"], []).append(r["phase"])
    uncovered, stale, tier_of = [], [], {}
    if policy is not None:
        declared = policy["phases"]
        for wid, phases in routed.items():
            for ph in phases:
                tier = (declared.get(wid) or {}).get(ph)
                tier_of[(wid, ph)] = tier
                if tier is None:
                    uncovered.append(f"{wid}/{ph}")
                rows.append({"workflow": wid, "phase": ph, "tier": tier,
                             "host_hint": policy["hints"].get(tier) if tier else None})
        for wid, entries in declared.items():
            for ph in entries:
                if ph not in routed.get(wid, []):
                    stale.append(f"{wid}/{ph}")
        if policy["hints"].get(fr.DEFAULT_MODEL_TIER) != fr.INHERIT_HINT:
            problems.append(f"the {fr.DEFAULT_MODEL_TIER} hint must be the reserved value "
                            f"{fr.INHERIT_HINT!r}, is {policy['hints'].get(fr.DEFAULT_MODEL_TIER)!r}")
        impl = routed.get("implement-feature", [])
        non_deep = sum(1 for ph in impl if tier_of.get(("implement-feature", ph))
                       not in (None, "deep"))
        if non_deep < NON_DEEP_BAR:
            problems.append(f"implement-feature has {non_deep} of {len(impl)} non-deep "
                            f"phase(s); the bar is {NON_DEEP_BAR}")
        for key in sorted(PINNED_LIGHT):
            if tier_of.get(key) != "light":
                problems.append(f"{'/'.join(key)} must be light, is {tier_of.get(key)}")
        for key in sorted(PINNED_DEEP):
            if tier_of.get(key) != "deep":
                problems.append(f"{'/'.join(key)} must be deep, is {tier_of.get(key)}")
    if uncovered:
        problems.append(f"phases without a declared tier: {uncovered}")
    if stale:
        problems.append(f"entries naming a phase that does not exist: {stale}")
    total = sum(len(v) for v in routed.values())
    res.add("C7", "every phase of every active Phase Model has exactly one declared tier",
            not problems,
            {"phases": total, "declared": total - len(uncovered), "uncovered": len(uncovered),
             "stale_entries": len(stale)},
            "every phase is covered, no entry is stale, the implement-feature bar and the "
            "named categories hold" if not problems else "; ".join(problems),
            rows)


def c8_escalation_monotonic(res: Result):
    hints = {"light": "h-light", "standard": "h-standard", "deep": "h-deep"}
    policy = {"hints": hints, "phases": {"w": {"p-light": "light", "p-standard": "standard",
                                                 "p-deep": "deep"}}}
    tiers = fr.MODEL_TIERS
    problems, rows, cases = [], [], 0
    for phase, declared in (("p-light", "light"), ("p-standard", "standard"),
                            ("p-deep", "deep"), ("p-undeclared", "standard")):
        previous = -1
        for total in range(0, 5):
            for validator in range(0, total + 1):
                rej = {"validator": validator, "gate_rollback": total - validator}
                a = fr.resolve_model_tier(policy, "w", phase, rej)
                b = fr.resolve_model_tier(policy, "w", phase, dict(rej))
                cases += 1
                got, base = tiers.index(a["tier"]), tiers.index(declared)
                want = min(base + total, len(tiers) - 1)
                if a != b:
                    problems.append(f"{phase} {rej}: two resolutions differ")
                if got < base:
                    problems.append(f"{phase} {rej}: resolved {a['tier']}, below {declared}")
                if got != want:
                    problems.append(f"{phase} {rej}: resolved {a['tier']}, expected "
                                    f"{tiers[want]}")
                if total and base < len(tiers) - 1 and got <= base:
                    problems.append(f"{phase} {rej}: a rejection did not raise the tier")
                if total and not a["escalation"] and got != base:
                    problems.append(f"{phase} {rej}: a promotion states no reason")
                if a["host_hint"] != hints[a["tier"]]:
                    problems.append(f"{phase} {rej}: hint does not follow the resolved tier")
                if previous >= 0 and got < previous:
                    problems.append(f"{phase} {rej}: a later rejection lowered the tier")
            previous = got
        rows.append({"phase": phase, "declared": declared,
                     "after_1": fr.resolve_model_tier(policy, "w", phase,
                                                      {"validator": 1})["tier"],
                     "after_2": fr.resolve_model_tier(policy, "w", phase,
                                                      {"gate_rollback": 2})["tier"]})
    none = fr.resolve_model_tier(None, "w", "p-deep", {"validator": 3})
    if none["tier"] != fr.DEFAULT_MODEL_TIER or none["host_hint"] != fr.INHERIT_HINT \
            or none["escalation"]:
        problems.append("an absent declaration must resolve to standard, inherit, no promotion")
    res.add("C8", "model tier escalation is monotonic, capped at deep, and repeatable",
            not problems, {"cases": cases, "violations": len(problems)},
            "no case lowers a tier, every rejection raises it one step to deep, and every "
            "resolution repeats" if not problems else "; ".join(problems[:5]),
            rows)


def run_checks() -> Result:
    res = Result()
    c1_commands_resolve(res)
    c2_phase_models(res)
    c3_owners_invocable(res)
    c4_phase_skills(res)
    c5_gates_decidable(res)
    c6_dispatchable(res)
    c7_tier_declaration(res)
    c8_escalation_monotonic(res)
    return res


def print_report(res: Result):
    print("Registry Coverage Verification")
    print("-" * 100)
    for c in res.checks:
        print(f"[{c['result']}] {c['id']:<4} {c['check']}")
        print(f"        {c['detail']}")
        print(f"        counts: {json.dumps(c['counts'])}")
    print("-" * 100)
    n_pass = sum(1 for c in res.checks if c["result"] == "PASS")
    print(f"{n_pass}/{len(res.checks)} checks passed -- "
          f"{'COVERED' if res.passed else 'NOT COVERED'}")


def markdown(res: Result) -> str:
    out = ["# Registry Coverage Report", "",
           "Generated by `runtime/verify_registry_coverage.py`.", "",
           "| Check | Result | Counts |", "|---|---|---|"]
    for c in res.checks:
        counts = ", ".join(f"{k}={v}" for k, v in c["counts"].items())
        out.append(f"| {c['id']} {c['check']} | {c['result']} | {counts} |")
    out.append("")
    for c in res.checks:
        out.append(f"## {c['id']} — {c['check']}")
        out.append("")
        out.append(c["detail"])
        out.append("")
        if c["rows"]:
            keys = list(c["rows"][0].keys())
            out.append("| " + " | ".join(keys) + " |")
            out.append("|" + "---|" * len(keys))
            for r in c["rows"]:
                out.append("| " + " | ".join(
                    ("`" + ", ".join(map(str, r.get(k) or [])) + "`") if isinstance(r.get(k), list)
                    else str(r.get(k)) for k in keys) + " |")
            out.append("")
    return "\n".join(out)


def main() -> int:
    ap = argparse.ArgumentParser(description="Verify registered operational surface coverage")
    ap.add_argument("--json-out")
    ap.add_argument("--markdown-out")
    a = ap.parse_args()

    res = run_checks()
    print_report(res)

    if a.json_out:
        Path(a.json_out).write_text(json.dumps({
            "verdict": "COVERED" if res.passed else "NOT COVERED",
            "checks": res.checks,
        }, indent=2), encoding="utf-8")
        print(f"\nwrote {a.json_out}")
    if a.markdown_out:
        Path(a.markdown_out).write_text(markdown(res), encoding="utf-8")
        print(f"wrote {a.markdown_out}")
    return 0 if res.passed else 1


if __name__ == "__main__":
    sys.exit(main())
