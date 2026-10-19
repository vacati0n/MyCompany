#!/usr/bin/env python3
"""Validation Engine: framework-change-proposal.md conformance.

Mechanically checks a produced `framework-change-proposal.md` against the contract set that
governs it:

  - `.claude/templates/framework-change-proposal.md`  rendered form, section and field labels
  - `.claude/config/self-hosting-profile.md`          routing, Evidence Rule, Completion Rule
  - `.claude/validation/framework-release-checklist.md`  the mandatory items a framework
                                                          update must record
  - the run the proposal names, as the runtime wrote it

Scope boundary. The structural rules are the engine's (`artifact_contract.py`). What this module
adds is the only thing that makes a change proposal worth requiring: it refuses to take the
author's word for the run. `F2` to `F5` re-read `execution-request.json` and `state.json` and
compare them to what the proposal claims, so a proposal describing a run that was reached by
another command, that never executed, or whose blocked phases were quietly dropped from the table
is rejected. Whether the change is a good idea is a Design Gate judgment and is not decidable
here; whether it was actually carried by the framework is.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import artifact_contract as ac  # noqa: E402
import framework_runtime as fr  # noqa: E402
import self_hosting as sh  # noqa: E402
from artifact_lib import Check, canonical_agent_id, strip_md  # noqa: E402

# Verification scripts a committed proposal names that were later removed on purpose. A
# proposal is an audit record and is never edited in place, so F10 reads a script listed here
# as accounted for rather than as a command the proposal could not have run.
REMOVED_SCRIPTS = {
    # removed with the CLI wrapper package and its bundled payload mirror
    "tests/test_bundled_payload.py",
}

TEMPLATE = "templates/framework-change-proposal.md"
GD001_EFFECTIVE = "2026-08-19"   # config/self-hosting-profile.md#point-in-time-evidence-rule

PROFILE = "config/self-hosting-profile.md"
CHECKLIST = "validation/framework-release-checklist.md"

SCOPE_TABLE = ("Path", "Scope Rule", "Decision", "Note")
EVIDENCE_TABLE = ("Evidence ID", "Link", "Status")
PHASE_TABLE = ("Phase", "Owner Agent", "Runtime Status", "Disposition", "Performed By", "Evidence")
GATE_TABLE = ("Gate", "Decision", "Owner Role", "Decided By", "Rationale")
VERIFY_TABLE = ("Check", "Command", "Result")
RELEASE_TABLE = ("Item", "Requirement", "Result", "Evidence")
OPEN_TABLE = ("ID", "Item", "Owner", "Disposition")

CONTRACT = ac.ArtifactContract(
    artifact="framework-change-proposal.md",
    # The orchestrator owns run closure and is the contracted producer. `operator` is permitted
    # because 33 of the framework's 36 phases still block at capability registration, so the
    # human carrying a blocked phase is the honest author of the record for it. The permission
    # narrows as agents register; it is not a permanent second producer.
    producers=("orchestrator", "operator"),
    metadata_key="frameworkChangeProposal",
    metadata_fields=("proposalId", "changeClass", "routedCommand", "routedWorkflow", "runId",
                     "profileRef", "profileVersion", "producedBy", "agentVersion",
                     "schemaVersion", "status"),
    statuses=ac.STATUSES_COMPLETE,
    template_ref=TEMPLATE,
    contract_refs=(TEMPLATE, PROFILE, CHECKLIST),
    # GD-001: `Authoring Baseline` is a permitted section rather than a mandatory one, so a
    # proposal authored before the decision stays valid unchanged. `F14` makes it mandatory
    # from the decision date onward, which is the obligation applying it to *future* records
    # without reaching backwards into accepted ones.
    appendices=("Authoring Baseline",),
    id_prefixes={"O": "Open Items"},
    sections=(
        ac.Section("Metadata", fields=(
            ac.Field("Proposal identifier"),
            ac.Field("Change title", min_words=3),
            ac.Field("Change class"),
            ac.Field("Routed command"),
            ac.Field("Run identifier"),
            ac.Field("Authored on"),
        )),
        ac.Section("Change Statement", fields=(
            ac.Field("Objective", min_words=6),
            ac.Field("In scope", min_words=4),
            ac.Field("Out of scope", min_words=3),
            ac.Field("Acceptance basis", min_words=4),
        )),
        ac.Section("Scope Classification", table=ac.Table(SCOPE_TABLE, min_rows=1),
                   allow_none=False),
        ac.Section("Routing Decision", fields=(
            ac.Field("Change class"),
            ac.Field("Selector satisfied by", min_words=4),
            ac.Field("Command"),
            ac.Field("Primary workflow"),
            ac.Field("Entry phase"),
            ac.Field("Required inputs supplied"),
        )),
        ac.Section("Run Evidence", table=ac.Table(EVIDENCE_TABLE, min_rows=7), allow_none=False),
        ac.Section("Phase Disposition", table=ac.Table(PHASE_TABLE, min_rows=1),
                   allow_none=False),
        ac.Section("Gate Decisions", table=ac.Table(GATE_TABLE, min_rows=1)),
        ac.Section("Verification", table=ac.Table(VERIFY_TABLE, min_rows=1), allow_none=False),
        ac.Section("Risk and Rollback", fields=(
            ac.Field("Blast radius", min_words=3),
            ac.Field("Risk assessment", min_words=5),
            ac.Field("Rollback procedure", min_words=5),
        )),
        ac.Section("Release Checklist Result", table=ac.Table(RELEASE_TABLE, min_rows=1),
                   allow_none=False),
        ac.Section("Open Items", table=ac.Table(OPEN_TABLE, min_rows=1)),
        ac.Section("Sign-off", fields=(
            ac.Field("Proposed by"),
            ac.Field("Accepted by"),
            ac.Field("Acceptance basis", min_words=4),
        )),
    ),
    obligations=(
        ("N1", PROFILE + "#completion-rule",
         "The work recorded against a blocked phase was performed to that phase's contract"),
        ("N2", PROFILE + "#evidence-rule",
         "The linked run is the run that carried this change, and no other run was consulted"),
    ),
)



# --------------------------------------------------------------------------- GD-001 support
# `config/self-hosting-profile.md#point-in-time-evidence-rule` makes a proposal a record of the
# state it was authored against. These two helpers supply that state: the declared baseline
# when the proposal carries one, and a reconstruction from the run's append-only transition
# log when it predates `GD-001`. Neither reads current state, which is the whole point.

def _baseline(rep) -> dict:
    """The Authoring Baseline the proposal declares.

    A proposal that predates `GD-001` carries no baseline section. It still carries an
    `Authored on` date in its Metadata, and that is enough to place it in time, so the
    reconstruction path uses it rather than falling back to current state. `dispatchable`
    stays absent in that case, which is what tells `F12` to derive dispatchability from the
    run's own record as of that date.
    """
    body_of = getattr(rep, "body_of", {}) or {}
    out = {}
    legacy = strip_md(ac.parse_field_bullets(body_of.get("Metadata", "")).get("authored on", ""))
    if legacy and not ac.is_none_marker(legacy):
        # End of the authoring day: a same-day transition is part of what was authored.
        out["authored_at"] = legacy.strip() + "T23:59:59Z"
    body = body_of.get("Authoring Baseline", "")
    if not body:
        return out
    f = ac.parse_field_bullets(body)
    at = strip_md(f.get("authored at", ""))
    if at and not ac.is_none_marker(at):
        out["authored_at"] = at
    phases = strip_md(f.get("routed workflow dispatchable phases", ""))
    if phases and not ac.is_none_marker(phases):
        out["dispatchable"] = [x.strip().strip("`") for x in phases.split(",") if x.strip()]
    elif phases:
        out["dispatchable"] = []
    return out


def _status_as_of(run_dir, work_items: dict, at: str | None) -> dict:
    """Per-phase {status, blocked_reason} as of `at`, from the transition log.

    With no instant, the run's current work items are returned unchanged. The log is
    append-only, so replaying it to a cut-off is deterministic and does not depend on what the
    framework can do now.
    """
    if not at:
        return {k: dict(v) for k, v in work_items.items()}
    state = json.loads((run_dir / "state.json").read_text(encoding="utf-8"))
    out = {k: {"status": None, "blocked_reason": None, "work_type": v.get("work_type")}
           for k, v in work_items.items()}
    for t in sorted(state.get("transitions") or [], key=lambda x: x.get("seq", 0)):
        if str(t.get("at") or "") > at:
            break
        sid = t.get("state_id")
        if sid not in out:
            continue
        out[sid]["status"] = t.get("to")
        if t.get("to") != "blocked":
            out[sid]["blocked_reason"] = None
    # The classified blocked reason lives in the recovery ledger, which is append-only and
    # marks entries resolved in place rather than removing them. That makes it the one source
    # that still holds what a phase was blocked *for* after the block itself has cleared.
    ledger_path = run_dir / "recovery-ledger.json"
    if ledger_path.exists():
        raw = json.loads(ledger_path.read_text(encoding="utf-8"))
        entries = raw if isinstance(raw, list) else (raw.get("entries") or [])
        for e in sorted(entries, key=lambda x: str(x.get("detected_at") or "")):
            sid = e.get("state_id")
            if sid in out and str(e.get("detected_at") or "") <= at                     and out[sid]["status"] == "blocked" and e.get("blocked_reason"):
                out[sid]["blocked_reason"] = e["blocked_reason"]
    for sid, v in out.items():
        if v["status"] is None:
            v["status"] = (work_items.get(sid) or {}).get("status")
            v["blocked_reason"] = (work_items.get(sid) or {}).get("blocked_reason")
    return out

def _rows(rep, section: str, columns: tuple):
    body = (getattr(rep, "body_of", {}) or {}).get(section, "")
    headers, rows = ac.find_table(body, columns)
    return headers or [], rows


def _cell(headers, row, column) -> str:
    i = ac.column_index(headers, column)
    return strip_md(row[i]).strip() if 0 <= i < len(row) else ""


def semantic_checks(rep, contract):  # noqa: C901 - one check per profile rule, kept flat
    """The rules `config/self-hosting-profile.md` declares, made decidable."""
    meta = getattr(rep, "meta", {}) or {}
    body_of = getattr(rep, "body_of", {}) or {}

    def add(cid, ref, severity, description, result, detail=""):
        rep.checks.append(Check(cid, ref, severity, description, result, detail))

    change_class = str(meta.get("changeClass") or "").strip()
    routed_command = str(meta.get("routedCommand") or "").strip().lstrip("/")
    routed_workflow = str(meta.get("routedWorkflow") or "").strip()
    run_id = str(meta.get("runId") or "").strip()

    # ------------------------------------------------------------------ F1 routing agreement
    route = None
    try:
        profile = sh.load_profile()
        route = sh.resolve_route(change_class, profile)
        ok = route["command"] == routed_command and route["workflow"] == routed_workflow
        add("F1", PROFILE + "#routing-table", "Blocking",
            "The declared change class routes to the declared command and workflow",
            "pass" if ok else "fail",
            f"profile routes {change_class!r} to /{route['command']} -> {route['workflow']}; "
            f"proposal declares /{routed_command} -> {routed_workflow}" if not ok
            else f"{change_class} -> /{routed_command} -> {routed_workflow}")
    except (sh.ProfileError, fr.RuntimeError_) as exc:
        profile = None
        add("F1", PROFILE + "#routing-table", "Blocking",
            "The declared change class routes to the declared command and workflow",
            "fail", str(exc))

    # ------------------------------------------------------------------ F2 the run is real
    run_dir = fr.RUNS / run_id if run_id else None
    request = {}
    if run_dir and (run_dir / "execution-request.json").exists():
        request = json.loads((run_dir / "execution-request.json").read_text(encoding="utf-8"))
    agrees = bool(request) and request.get("command_id") == routed_command \
        and request.get("workflow_id") == routed_workflow
    add("F2", PROFILE + "#evidence-rule", "Blocking",
        "The named run exists and was submitted under the routed command and workflow",
        "pass" if agrees else "fail",
        f"run {run_id!r}: execution-request records command_id="
        f"{request.get('command_id')!r} workflow_id={request.get('workflow_id')!r}"
        if request else f"run {run_id!r} has no execution-request.json under runs/")

    state = {}
    if run_dir and (run_dir / "state.json").exists():
        state = json.loads((run_dir / "state.json").read_text(encoding="utf-8"))
    work_items = state.get("work_items") or {}
    run_phases = [k for k, v in work_items.items() if v.get("work_type") == "state"]

    # ------------------------------------------------------------------ F3/F4 evidence links
    headers, ev_rows = _rows(rep, "Run Evidence", EVIDENCE_TABLE)
    declared_ids, unresolved, foreign = [], [], []
    for row in ev_rows:
        eid = _cell(headers, row, "Evidence ID")
        link = _cell(headers, row, "Link").strip("`")
        declared_ids.append(eid)
        if not link:
            unresolved.append(f"{eid}: no link")
            continue
        if not (fr.CLAUDE / link).exists():
            unresolved.append(f"{eid}: {link}")
        if run_id and not link.startswith(f"runs/{run_id}/"):
            foreign.append(f"{eid}: {link}")
    required_ids = [e["id"] for e in (profile or {}).get("evidence", [])] if profile else []
    absent = [e for e in required_ids if e not in declared_ids]
    add("F3", PROFILE + "#evidence-rule", "Blocking",
        "Every required evidence link is declared and resolves on disk",
        "pass" if not unresolved and not absent else "fail",
        f"unresolved: {unresolved}; missing rows: {absent}" if (unresolved or absent)
        else f"{len(ev_rows)} link(s) resolved, covering {', '.join(required_ids)}")
    add("F4", PROFILE + "#evidence-rule", "Blocking",
        "Every evidence link belongs to the run the proposal names",
        "pass" if not foreign else "fail",
        f"outside runs/{run_id}/: {foreign}" if foreign
        else f"all links under runs/{run_id}/")

    # ------------------------------------------------------------------ F12 evidence shape
    # `E-6` and `E-7` mean different things depending on whether the routed workflow could
    # execute anything at all, and that is the runtime's answer rather than the author's.
    link_of = {}
    for row in ev_rows:
        link_of[_cell(headers, row, "Evidence ID")] = _cell(headers, row, "Link").strip("`")
    # GD-001: dispatchability is the baseline's, not today's. A proposal that predates the
    # decision declares none and falls back to the live chain exactly as before, so no
    # historical proposal is judged by a rule that did not exist when it was written.
    baseline = _baseline(rep)
    if "dispatchable" in baseline:
        dispatchable = baseline["dispatchable"]
        dispatch_source = "authoring baseline"
    else:
        # Pre-GD-001: what the workflow could dispatch then is what the run actually reached
        # by then. The transition log is append-only, so this answer never moves again.
        _as_of = _status_as_of(run_dir, work_items, baseline.get("authored_at"))
        dispatchable = sorted(ph for ph, v in _as_of.items()
                              if (v.get("work_type") or "state") == "state"
                              and str(v.get("status") or "").lower() == "completed")
        dispatch_source = ("the run's own record as of "
                           f"{baseline.get('authored_at')} (proposal predates GD-001)")
    shape_failures = []
    e6, e7 = link_of.get("E-6", ""), link_of.get("E-7", "")
    if dispatchable:
        if "/states/" not in e6 or "/artifacts/" not in e6:
            shape_failures.append(
                f"E-6 must link a phase artifact under states/<phase>/artifacts/, got {e6!r}")
        if not e7.endswith("validation-report.json"):
            shape_failures.append(
                f"E-7 must link a phase validation report, got {e7!r}")
    else:
        executed = [p for p, i in work_items.items()
                    if i.get("work_type") == "state" and i.get("status") == "completed"]
        if executed:
            shape_failures.append(
                f"the routed workflow has no dispatchable phase, yet the run records "
                f"{executed} as completed")
    add("F12", PROFILE + "#evidence-rule", "Blocking",
        "The evidence linked matches what the routed workflow could produce",
        "pass" if not shape_failures else "fail",
        f"{shape_failures}" if shape_failures else
        (f"/{routed_command} has {len(dispatchable)} dispatchable phase(s) per "
         f"the {dispatch_source} ({', '.join(dispatchable)}); E-6 and E-7 link "
         f"executed-phase evidence"
         if dispatchable else
         f"/{routed_command} has no dispatchable phase, so E-6 and E-7 are satisfied by the "
         f"run's own record that every phase blocked"))

    # ------------------------------------------------------------------ F14 authoring baseline
    # `GD-001` took effect on 2026-08-19. A proposal authored on or after that date declares
    # the state it was authored against; one authored before it is judged by the contract that
    # existed when it was written, which is the decision applied to itself.
    authored_on = strip_md(ac.parse_field_bullets(
        (getattr(rep, "body_of", {}) or {}).get("Metadata", "")).get("authored on", ""))
    declares = bool((getattr(rep, "body_of", {}) or {}).get("Authoring Baseline", "").strip())
    in_force = bool(authored_on) and authored_on.strip() >= GD001_EFFECTIVE
    add("F14", PROFILE + "#point-in-time-evidence-rule", "Blocking",
        "A proposal authored on or after the point-in-time decision declares its authoring "
        "baseline",
        "pass" if (declares or not in_force) else "fail",
        f"authored {authored_on!r}, baseline declared={declares}"
        + ("" if in_force else f"; predates GD-001 ({GD001_EFFECTIVE}), so not required"))

    # ------------------------------------------------------------------ F5 phase disposition
    headers, ph_rows = _rows(rep, "Phase Disposition", PHASE_TABLE)
    listed = [_cell(headers, r, "Phase").strip("`") for r in ph_rows]
    missing_phases = [p for p in run_phases if p not in listed]
    invented = [p for p in listed if run_phases and p not in run_phases]
    add("F5", PROFILE + "#completion-rule", "Blocking",
        "Phase Disposition accounts for every phase the run enqueued, and invents none",
        "pass" if run_phases and not missing_phases and not invented else "fail",
        f"unaccounted: {missing_phases}; not in the run: {invented}"
        if (missing_phases or invented or not run_phases)
        else f"{len(listed)} of {len(run_phases)} phase(s) accounted for")

    # F6 -- a blocked phase carries the reason the runtime recorded, not a summary of it.
    # GD-001: judge each claim against the state the proposal was authored against. Where the
    # framework has since moved, that is drift to report, not a record to fail.
    as_of = _status_as_of(run_dir, work_items, baseline.get("authored_at"))
    basis = (f"authoring baseline {baseline['authored_at']}" if baseline.get("authored_at")
             else "run transition log (proposal predates GD-001)")
    mismatched, unreasoned, drift = [], [], []
    for row in ph_rows:
        phase = _cell(headers, row, "Phase").strip("`")
        claimed = _cell(headers, row, "Runtime Status").strip("`").lower()
        recorded = as_of.get(phase) or {}
        actual = str(recorded.get("status") or "").lower()
        if actual and claimed != actual:
            mismatched.append(f"{phase}: claims {claimed!r}, the run recorded {actual!r}")
        current = str((work_items.get(phase) or {}).get("status") or "").lower()
        if actual and current and current != actual:
            drift.append(f"{phase}: {actual} -> {current}")
        if actual == "blocked":
            reason = str(recorded.get("blocked_reason") or "")
            disposition = _cell(headers, row, "Disposition")
            if reason and reason.split(":")[0] not in disposition                     and reason not in disposition:
                unreasoned.append(f"{phase}: recorded reason {reason!r} not stated")
    add("F6", PROFILE + "#completion-rule", "Blocking",
        "Each phase reports the status the runtime recorded, and each blocked phase states "
        "its recorded reason",
        "pass" if not mismatched and not unreasoned else "fail",
        f"status mismatch: {mismatched}; reason omitted: {unreasoned}"
        if (mismatched or unreasoned) else
        f"{len(ph_rows)} phase row(s) agree with the {basis}"
        + (f"; informational drift since authoring: {drift}" if drift else ""))

    # ------------------------------------------------------------------ F7 scope rows recomputed
    headers, sc_rows = _rows(rep, "Scope Classification", SCOPE_TABLE)
    wrong = []
    for row in sc_rows:
        path = _cell(headers, row, "Path").strip("`")
        rule = _cell(headers, row, "Scope Rule").strip("`")
        decision = _cell(headers, row, "Decision").lower()
        if not path or profile is None:
            continue
        actual = sh.classify_path(path, profile)
        want = "in-scope" if actual["in_scope"] else "out-of-scope"
        if decision != want or (rule and actual["rule"] and rule != actual["rule"]):
            wrong.append(f"{path}: claims {decision}/{rule or '-'}, profile decides "
                         f"{want}/{actual['rule'] or '-'}")
    add("F7", PROFILE + "#scope-rule", "Blocking",
        "Every scope classification row recomputes to the same decision under the profile",
        "pass" if not wrong else "fail",
        f"disagreements: {wrong}" if wrong else f"{len(sc_rows)} row(s) recomputed")

    # ------------------------------------------------------------------ F8 gates
    headers, gate_rows = _rows(rep, "Gate Decisions", GATE_TABLE)
    gate_body = body_of.get("Gate Decisions", "")
    bad_gates = []
    if routed_workflow and not ac.is_none_marker(gate_body):
        owners = fr.parse_gate_matrix(routed_workflow)
        for row in gate_rows:
            gate = _cell(headers, row, "Gate").strip("`")
            role = canonical_agent_id(_cell(headers, row, "Owner Role").strip("`"))
            if gate not in owners:
                bad_gates.append(f"{gate!r} is not a gate of {routed_workflow}")
            elif role not in owners[gate]:
                bad_gates.append(f"{role!r} does not own {gate!r} "
                                 f"(owners: {', '.join(owners[gate])})")
    add("F8", "workflows/workflow-gate-matrix.md", "Blocking",
        "Every recorded gate decision names a gate of the routed workflow and an owner "
        "permitted to decide it",
        "pass" if not bad_gates else "fail",
        f"{bad_gates}" if bad_gates else f"{len(gate_rows)} gate decision(s) resolve")

    # ------------------------------------------------------------------ F9 release checklist
    headers, rel_rows = _rows(rep, "Release Checklist Result", RELEASE_TABLE)
    recorded = {_cell(headers, r, "Item").strip("`"): _cell(headers, r, "Result")
                for r in rel_rows}
    try:
        items = sh.load_release_checklist()
        mandatory = [i["id"] for i in items if i["obligation"] == "mandatory"]
        omitted = [i for i in mandatory if i not in recorded]
        blank = [i for i in mandatory if i in recorded and not recorded[i]]
        add("F9", CHECKLIST, "Blocking",
            "Every mandatory release checklist item is recorded with a result",
            "pass" if not omitted and not blank else "fail",
            f"omitted: {omitted}; blank: {blank}" if (omitted or blank)
            else f"{len(mandatory)} mandatory item(s) recorded")
    except (sh.ProfileError, fr.RuntimeError_) as exc:
        add("F9", CHECKLIST, "Blocking",
            "Every mandatory release checklist item is recorded with a result", "fail", str(exc))

    # ------------------------------------------------------------------ F10 verification rows
    headers, v_rows = _rows(rep, "Verification", VERIFY_TABLE)
    unrunnable = []
    for row in v_rows:
        cmd = _cell(headers, row, "Command").strip("`")
        if not cmd or cmd.lower() == "inspection":
            continue
        script = next((tok for tok in cmd.split() if tok.endswith(".py")), None)
        script = script and script.replace("\\", "/")
        if script and script not in REMOVED_SCRIPTS and not (fr.CLAUDE.parent / script).exists():
            unrunnable.append(cmd)
    add("F10", TEMPLATE, "Correctable",
        "Every verification row names a command that exists, or declares inspection",
        "pass" if not unrunnable else "fail",
        f"absent: {unrunnable}" if unrunnable else f"{len(v_rows)} verification row(s)")

    # ------------------------------------------------------------------ F11 self-consistency
    section_meta = ac.parse_field_bullets(body_of.get("Metadata", ""))
    routing = ac.parse_field_bullets(body_of.get("Routing Decision", ""))
    disagreements = []
    for label, value in (("proposal identifier", str(meta.get("proposalId") or "")),
                         ("change class", change_class),
                         ("run identifier", run_id)):
        stated = strip_md(section_meta.get(label, "")).strip().strip("`")
        if stated and stated != value:
            disagreements.append(f"Metadata {label}={stated!r} vs metadata block {value!r}")
    for label, value in (("change class", change_class),
                         ("primary workflow", routed_workflow)):
        stated = strip_md(routing.get(label, "")).strip().strip("`")
        if stated and stated != value:
            disagreements.append(f"Routing Decision {label}={stated!r} vs {value!r}")
    entry = strip_md(routing.get("entry phase", "")).strip().strip("`")
    if route and entry and entry != route["entry_phase"]:
        disagreements.append(f"entry phase {entry!r} vs profile {route['entry_phase']!r}")
    add("F11", TEMPLATE, "Blocking",
        "The metadata block, the Metadata section, and the Routing Decision agree",
        "pass" if not disagreements else "fail",
        f"{disagreements}" if disagreements else "all three agree")

    # ------------------------------------------------------------------ F13 declared inputs
    # The run records exactly which input types were supplied, so the claim is decidable and
    # does not need a substance floor standing in for a check.
    declared_inputs = {tok.strip().strip("`,.") for tok in
                       strip_md(routing.get("required inputs supplied", "")).split()
                       if tok.strip().strip("`,.")}
    actual_inputs = {i.get("type") for i in (request.get("inputs") or [])}
    missing_inputs = sorted(actual_inputs - declared_inputs)
    invented_inputs = sorted(declared_inputs - actual_inputs) if actual_inputs else []
    add("F13", PROFILE + "#routing-resolution", "Blocking",
        "The inputs the proposal says were supplied are the inputs the run records",
        "pass" if actual_inputs and not missing_inputs and not invented_inputs else "fail",
        f"unlisted: {missing_inputs}; not in the run: {invented_inputs}"
        if (missing_inputs or invented_inputs or not actual_inputs)
        else f"{len(actual_inputs)} input type(s): {', '.join(sorted(actual_inputs))}")

    rep.counts["changeClass"] = change_class or None
    rep.counts["runId"] = run_id or None
    rep.counts["evidenceLinks"] = len(ev_rows)
    rep.counts["phasesAccounted"] = len(ph_rows)
    rep.counts["gateDecisions"] = len(gate_rows)
    return rep


def validate(artifact_path, envelope: dict | None = None):
    rep = ac.run_contract(CONTRACT, Path(artifact_path), envelope)
    return semantic_checks(rep, CONTRACT)


def main():
    return ac.cli(CONTRACT, extra=semantic_checks)


if __name__ == "__main__":
    sys.exit(main())
