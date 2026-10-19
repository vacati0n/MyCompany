#!/usr/bin/env python3
"""Self-hosting command profile: parser, classifier, and router.

`config/self-hosting-profile.md` declares one profile, `framework-internal-change`, whose
tables decide whether a change is framework-internal and which command carries it. This module
is the executable form of those tables, so the profile is a routing decision the runtime can
make rather than a document an operator is trusted to have read.

Nothing here invents routing. Every command, workflow, and phase the profile names is resolved
through the same registries and Phase Models the runtime gateway reads
(`framework_runtime.resolve_command`, `resolve_workflow`, `parse_phase_model`), so a profile row
that names something unroutable fails here rather than at execution time.

    python .claude/runtime/self_hosting.py classify --path .claude/workflows/release.md
    python .claude/runtime/self_hosting.py route    --intent capability-addition
    python .claude/runtime/self_hosting.py evidence --run-id run-abc123456789
    python .claude/runtime/self_hosting.py checklist
"""

from __future__ import annotations

import argparse
import fnmatch
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import framework_runtime as fr  # noqa: E402

CLAUDE = fr.CLAUDE
PROFILE_REL = "config/self-hosting-profile.md"
CHECKLIST_REL = "validation/framework-release-checklist.md"
PROPOSALS_REL = "proposals"

PROFILE_ID = "framework-internal-change"


class ProfileError(Exception):
    """The profile does not resolve. Raised rather than guessed around."""


# --------------------------------------------------------------------------- markdown tables


def tables_under(text: str, heading: str) -> list:
    """Every table declared under one `##` or `###` heading, as lists of row dicts.

    A heading's body ends at the next heading of the same or a higher level, so a `###`
    subsection does not leak its tables into the `##` section above it.
    """
    m = re.search(r"^(#{2,3})\s+" + re.escape(heading) + r"\s*$", text, re.M)
    if not m:
        raise ProfileError(f"{PROFILE_REL} declares no section {heading!r}")
    level = len(m.group(1))
    rest = text[m.end():]
    stop = re.search(r"^#{1," + str(level) + r"}\s+\S", rest, re.M)
    body = rest[: stop.start()] if stop else rest

    out, rows, headers = [], [], None
    for ln in body.split("\n"):
        s = ln.strip()
        if not s.startswith("|"):
            if rows:
                out.append(rows)
            rows, headers = [], None
            continue
        cells = [c.strip().strip("`") for c in s.strip("|").split("|")]
        if headers is None:
            headers = [c.lower() for c in cells]
            continue
        if set("".join(cells)) <= set("-: "):
            continue
        rows.append(dict(zip(headers, cells)))
    if rows:
        out.append(rows)
    return out


def one_table(text: str, heading: str) -> list:
    tabs = tables_under(text, heading)
    if not tabs:
        raise ProfileError(f"{PROFILE_REL} section {heading!r} carries no table")
    return tabs[0]


def split_patterns(cell: str) -> list:
    """A path-pattern cell holds one pattern, or several separated by commas."""
    return [p.strip().strip("`") for p in cell.split(",") if p.strip()]


# --------------------------------------------------------------------------- profile


def load_profile() -> dict:
    text = fr.read_text(PROFILE_REL)

    identity = {r["field"].lower(): r["value"] for r in one_table(text, "Profile Identity")}
    declared_id = identity.get("profile identifier", "")
    if declared_id != PROFILE_ID:
        raise ProfileError(
            f"{PROFILE_REL} declares profile identifier {declared_id!r}, expected {PROFILE_ID!r}")
    if identity.get("status") != "active":
        raise ProfileError(f"profile status is {identity.get('status')!r}, not active")

    scope = []
    for r in one_table(text, "Scope Rule"):
        decision = r["decision"].strip().lower()
        if decision not in ("in-scope", "out-of-scope"):
            raise ProfileError(f"scope rule {r['rule']} declares decision {decision!r}")
        scope.append({"rule": r["rule"], "patterns": split_patterns(r["path pattern"]),
                      "decision": decision, "reason": r["reason"]})

    routing = []
    for idx, r in enumerate(one_table(text, "Routing Table")):
        routing.append({
            "order": idx + 1,
            "change_class": r["change class"],
            "selector": r["selector"],
            "command": r["command"].lstrip("/"),
            "workflow": r["primary workflow"],
            "required_inputs": [i.strip().strip("`")
                                for i in r["required inputs"].split(",") if i.strip()],
            "entry_phase": r["entry phase"],
        })
    classes = [row["change_class"] for row in routing]
    if len(set(classes)) != len(classes):
        raise ProfileError(f"routing table declares a change class twice: {classes}")

    evidence = [{"id": r["evidence id"], "link": r["required link"], "why": r["why it is required"]}
                for r in one_table(text, "Evidence Rule")]
    completion = [{"id": r["id"], "condition": r["condition"]}
                  for r in one_table(text, "Completion Rule")]

    return {"identity": identity, "scope": scope, "routing": routing,
            "evidence": evidence, "completion": completion,
            "source": PROFILE_REL,
            "source_digest": fr.sha256_text(text)}


# --------------------------------------------------------------------------- scope


def normalize(path: str) -> str:
    """Repository-relative posix form, so a pattern written once matches either shell's form."""
    p = str(path).replace("\\", "/").lstrip("./")
    try:
        abs_p = Path(path).resolve()
        repo = CLAUDE.parent
        if repo in abs_p.parents:
            p = abs_p.relative_to(repo).as_posix()
    except (OSError, ValueError):
        pass
    return p


def matches(pattern: str, path: str) -> bool:
    pat = pattern.replace("\\", "/")
    if pat.endswith("/**"):
        prefix = pat[:-3]
        return path == prefix or path.startswith(prefix + "/")
    return fnmatch.fnmatch(path, pat)


def classify_path(path: str, profile: dict | None = None) -> dict:
    """Decide one path against the Scope Rule. Exclusions take precedence over inclusions."""
    profile = profile or load_profile()
    rel = normalize(path)
    hits = [(rule, pat) for rule in profile["scope"] for pat in rule["patterns"]
            if matches(pat, rel)]
    excluded = [(r, p) for r, p in hits if r["decision"] == "out-of-scope"]
    included = [(r, p) for r, p in hits if r["decision"] == "in-scope"]
    if excluded:
        rule, pat = excluded[0]
        return {"path": rel, "in_scope": False, "rule": rule["rule"], "pattern": pat,
                "reason": rule["reason"]}
    if included:
        rule, pat = included[0]
        return {"path": rel, "in_scope": True, "rule": rule["rule"], "pattern": pat,
                "reason": rule["reason"]}
    return {"path": rel, "in_scope": False, "rule": None, "pattern": None,
            "reason": "no scope rule matches; the path is outside the framework surface"}


def classify_change(paths, profile: dict | None = None) -> dict:
    """A change is framework-internal when at least one touched path is in scope."""
    profile = profile or load_profile()
    decisions = [classify_path(p, profile) for p in paths]
    in_scope = [d for d in decisions if d["in_scope"]]
    return {"framework_internal": bool(in_scope), "paths": decisions,
            "in_scope_count": len(in_scope), "out_of_scope_count": len(decisions) - len(in_scope)}


# --------------------------------------------------------------------------- routing


def routing_row(change_class: str, profile: dict | None = None) -> dict:
    profile = profile or load_profile()
    row = next((r for r in profile["routing"] if r["change_class"] == change_class), None)
    if row is None:
        raise ProfileError(
            f"change class {change_class!r} is not declared by the profile; declared: "
            + ", ".join(r["change_class"] for r in profile["routing"]))
    return row


def resolve_route(change_class: str, profile: dict | None = None) -> dict:
    """Resolve one routing row all the way to a phase the routed workflow declares.

    The three failure modes this closes are the ones a prose profile cannot: a command with no
    active record, a command whose primary workflow is not the one the profile claims, and an
    entry phase the workflow's Phase Model never declares.
    """
    profile = profile or load_profile()
    row = routing_row(change_class, profile)

    cmd = fr.resolve_command(row["command"])          # raises when absent or not active
    if cmd["primaryWorkflow"] != row["workflow"]:
        raise ProfileError(
            f"profile routes {change_class!r} to /{row['command']} -> {row['workflow']}, but "
            f"registry/commands.yaml records primaryWorkflow={cmd['primaryWorkflow']!r}")
    wf = fr.resolve_workflow(cmd["primaryWorkflow"])
    phases = [p["phase"] for p in fr.parse_phase_model(wf["specificationPath"])]
    if row["entry_phase"] not in phases:
        raise ProfileError(
            f"entry phase {row['entry_phase']!r} is not declared by workflow "
            f"{wf['identifier']!r}; declared: {phases}")

    return {"change_class": change_class, "command": row["command"],
            "workflow": wf["identifier"], "workflow_version": wf["version"],
            "entry_phase": row["entry_phase"], "phases": phases,
            "required_inputs": row["required_inputs"],
            "command_spec": cmd["specificationPath"],
            "workflow_spec": wf["specificationPath"],
            "selector": row["selector"]}


def dispatchable_phases(command_id: str) -> list:
    """Phases of the command's workflow that resolve the runtime's full capability chain.

    Asked of the runtime rather than declared anywhere, because it changes as agents register.
    A workflow with none can be routed and can record a run, but no run of it produces an
    artifact, which is what the profile's Evidence Rule turns on.
    """
    try:
        cmd = fr.resolve_command(command_id)
        wf = fr.resolve_workflow(cmd["primaryWorkflow"])
        phases = [p["phase"] for p in fr.parse_phase_model(wf["specificationPath"])]
    except fr.RuntimeError_:
        return []
    out = []
    for phase in phases:
        try:
            fr.resolve_chain(command_id, phase)
        except fr.RuntimeError_:
            continue
        out.append(phase)
    return out


# --------------------------------------------------------------------------- evidence


def expand_evidence(run_id: str, profile: dict | None = None) -> list:
    """The Evidence Rule for one run: each required link, resolved or not.

    `E-6` and `E-7` are per-phase rather than fixed paths, so they are expanded from what the
    run actually executed. A run that executed no phase resolves neither, which is the correct
    answer rather than an error: the rule is then unsatisfied and the proposal fails.
    """
    profile = profile or load_profile()
    run_dir = fr.RUNS / run_id
    out = []
    for row in profile["evidence"]:
        eid, link = row["id"], row["link"]
        if eid in ("E-6", "E-7"):
            glob = "states/*/artifacts/*.md" if eid == "E-6" else "states/*/validation-report.json"
            found = sorted(p.relative_to(CLAUDE).as_posix() for p in run_dir.glob(glob))
            out.append({"id": eid, "kind": "per-phase", "pattern": glob,
                        "resolved": bool(found), "paths": found})
            continue
        rel = link.replace("<run-id>", run_id)
        out.append({"id": eid, "kind": "fixed", "pattern": rel,
                    "resolved": (CLAUDE / rel).exists(), "paths": [rel]})
    return out


# --------------------------------------------------------------------------- release checklist


def load_release_checklist() -> list:
    """The machine-readable item table of `validation/framework-release-checklist.md`."""
    text = fr.read_text(CHECKLIST_REL)
    rows = one_table(text, "Checklist Items")
    items = []
    for r in rows:
        obligation = r["obligation"].strip().lower()
        if obligation not in ("mandatory", "advisory"):
            raise ProfileError(f"checklist item {r['id']} declares obligation {obligation!r}")
        cmd = r["verification"].strip()
        items.append({
            "id": r["id"],
            "requirement": r["requirement"],
            "obligation": obligation,
            "owner_role": r["owner role"],
            "verification": None if cmd.lower() in ("inspection", "none", "-") else cmd,
            "mode": "inspection" if cmd.lower() in ("inspection", "none", "-") else "command",
        })
    if not items:
        raise ProfileError(f"{CHECKLIST_REL} declares no checklist items")
    return items


# --------------------------------------------------------------------------- proposals


def proposals() -> list:
    d = CLAUDE / PROPOSALS_REL
    return sorted(d.glob("framework-change-proposal-*.md")) if d.exists() else []


# --------------------------------------------------------------------------- cli


def main() -> int:
    ap = argparse.ArgumentParser(description="Self-hosting command profile")
    sub = ap.add_subparsers(dest="cmd", required=True)

    c = sub.add_parser("classify", help="decide whether paths are framework-internal")
    c.add_argument("--path", action="append", required=True)

    r = sub.add_parser("route", help="resolve a change class to its command, workflow, and phase")
    r.add_argument("--intent", required=True, help="change class from the profile Routing Table")

    e = sub.add_parser("evidence", help="expand the Evidence Rule against one run")
    e.add_argument("--run-id", required=True)

    sub.add_parser("checklist", help="print the framework release checklist items")
    sub.add_parser("show", help="print the parsed profile")

    for p in (c, r, e):
        p.add_argument("--json", action="store_true")

    a = ap.parse_args()

    try:
        profile = load_profile()
    except (ProfileError, fr.RuntimeError_) as exc:
        print(f"PROFILE FAILURE {exc}")
        return 2

    if a.cmd == "classify":
        res = classify_change(a.path, profile)
        if getattr(a, "json", False):
            print(json.dumps(res, indent=2))
            return 0
        for d in res["paths"]:
            print(f"  [{'IN ' if d['in_scope'] else 'OUT'}] {d['path']}")
            print(f"        {d['rule'] or 'no rule'}: {d['reason']}")
        print()
        print("framework-internal change" if res["framework_internal"]
              else "not a framework-internal change")
        return 0

    if a.cmd == "route":
        try:
            route = resolve_route(a.intent, profile)
        except (ProfileError, fr.RuntimeError_) as exc:
            print(f"ROUTING FAILURE {exc}")
            return 2
        if getattr(a, "json", False):
            print(json.dumps(route, indent=2))
            return 0
        print(f"change class : {route['change_class']}")
        print(f"selector     : {route['selector']}")
        print(f"command      : /{route['command']}  ({route['command_spec']})")
        print(f"workflow     : {route['workflow']} v{route['workflow_version']} "
              f"({route['workflow_spec']})")
        print(f"entry phase  : {route['entry_phase']} of {len(route['phases'])}")
        print(f"inputs       : {', '.join(route['required_inputs'])}")
        print()
        print("submit with:")
        inputs = " ".join(f"\\\n    --input {t}=.claude/runs/inputs/<file>.md"
                          for t in route["required_inputs"])
        print(f"  python .claude/runtime/framework_runtime.py plan "
              f"--command {route['command']} {inputs}")
        return 0

    if a.cmd == "evidence":
        rows = expand_evidence(a.run_id, profile)
        if getattr(a, "json", False):
            print(json.dumps(rows, indent=2))
            return 0
        for row in rows:
            print(f"  [{'OK ' if row['resolved'] else 'MISS'}] {row['id']:<5} {row['pattern']}")
            for p in row["paths"]:
                print(f"        {p}")
        return 0 if all(row["resolved"] for row in rows) else 1

    if a.cmd == "checklist":
        try:
            items = load_release_checklist()
        except (ProfileError, fr.RuntimeError_) as exc:
            print(f"CHECKLIST FAILURE {exc}")
            return 2
        for it in items:
            print(f"  {it['id']:<6} {it['obligation']:<9} {it['owner_role']:<20} "
                  f"{it['requirement']}")
            print(f"         {it['verification'] or 'inspection'}")
        return 0

    print(json.dumps({k: v for k, v in profile.items() if k != "identity"}, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
