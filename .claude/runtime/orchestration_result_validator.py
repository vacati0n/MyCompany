#!/usr/bin/env python3
"""Validation Engine: orchestration-result.md conformance.

Mechanically checks a produced `orchestration-result.md` against the contract set that governs
it:

  - `.claude/templates/orchestration-result.md`          rendered form, section and field labels
  - `.claude/agents/orchestrator/identity.md`        behavioural contract for coordination
  - `.claude/agents/orchestrator/output.md`          structural contract this artifact renders to
  - `.claude/agents/orchestrator/quality.md`         the orchestrator's own self-verification checks

One artifact type carries every coordination output the framework asks for: the `fix-bug` closure
record, the `refactor` closure and debt record, and the `release` deployment record.
`templates/orchestration-result.md` records why they are one type: each is a phase-by-phase
progression account over a defined coordination scope, closing with a coordination position. The
`coordinationBasis` field carries the lens; the structure does not change with it.

Scope boundary: `O1` to `O9` are the orchestrator's own decision rules and constraints, expressed
as checks over the artifact. Whether the sequence chosen was the right sequence for the risk is
coordination judgement and is recorded as a not-machine-checkable obligation. Whether a record
claims a progression its own gate ledger contradicts is not judgement, and is enforced.

The check that matters most here is `O4`. Every other role's validator guards against overstating
its own evidence; a coordinator's characteristic failure is different, and worse: awarding itself
the gate decision that its own output is the evidence for. The Producer Exclusion Rule in
`workflows/workflow-gate-matrix.md` exists to stop exactly that, and `O4` is that rule expressed
as an artifact check rather than as an expectation.
"""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import artifact_contract as ac  # noqa: E402
from artifact_lib import Check, canonical_agent_id, strip_md  # noqa: E402

TEMPLATE = "templates/orchestration-result.md"
# The orchestrator's authoritative contract is a module set, not a single file. Each check below
# names the module that actually states the rule it enforces, so a failure points at the document
# that decides it rather than at the agent in general.
CONTRACT_REF = "agents/orchestrator/identity.md"
OUTPUT_REF = "agents/orchestrator/output.md"
QUALITY_REF = "agents/orchestrator/quality.md"

AGENT = "orchestrator"

DISPOSITIONS = ("closed", "closed-with-followups", "held", "rolled-back")
BASES = ("closure", "debt-closure", "deployment")
PROGRESSIONS = ("complete", "blocked", "not-started")
GATE_DECISIONS = ("approved", "rejected", "none", "not-applicable")
HANDOFF_ACCEPTED = ("accepted", "refused", "pending")
ESCALATION_STATUSES = ("open", "resolved", "routed", "accepted-risk")
ESCALATION_CATEGORIES = ("scope", "design", "quality", "validation", "delivery",
                         "capability", "operational", "coordination")
FOLLOWUP_CATEGORIES = ("technical-debt", "deferred-scope", "documentation", "monitoring",
                       "operational", "process", "defect")
FOLLOWUP_STATUSES = ("open", "scheduled", "resolved", "accepted-risk")
DEPLOYMENT_STATES = ("not-attempted", "deployed", "partially-deployed", "rolled-back",
                     "not-applicable")

# Both dispositions that finish a run. A follow-up records work the run was not obliged to do, so
# it qualifies what remains open -- it does not weaken the bar for closing at all. An open critical
# escalation is therefore disqualifying for either, which is why `O6` reads this set rather than
# `CLEAN_DISPOSITIONS`.
CLOSING_DISPOSITIONS = ("closed", "closed-with-followups")
# A disposition that claims the run finished with nothing left. `closed-with-followups` is excluded
# because carrying deferred work is exactly what it declares, so `O7` reads this narrower set.
CLEAN_DISPOSITIONS = ("closed",)
# The two that must state what stopped them, so they are named rather than derived by exclusion.
UNFINISHED_DISPOSITIONS = ("held", "rolled-back")

PHASE_TABLE = ("ID", "Phase", "Owner", "Declared Output", "Gate", "Gate Decision",
               "Decided By", "Evidence", "Progression")
HANDOFF_TABLE = ("ID", "From", "To", "Artifact", "Accepted", "Evidence")
ESCALATION_TABLE = ("ID", "Severity", "Category", "Raised By", "Routed To", "Status", "Detail")
FOLLOWUP_TABLE = ("ID", "Category", "Description", "Owner", "Severity", "Status")

CONTRACT = ac.ArtifactContract(
    artifact="orchestration-result.md",
    # One producer. Coordination authority is not shared: a second role authoring a progression
    # record would mean two accounts of what the run did, and no way to decide between them.
    producers=(AGENT,),
    metadata_key="orchestrationResult",
    metadata_fields=("resultId", "coordinationReference", "coordinationBasis", "sourceInputs",
                     "producedBy", "agentVersion", "schemaVersion", "status", "disposition",
                     "inputDigest", "contextDigest"),
    statuses=ac.STATUSES_COMPLETE,
    template_ref=TEMPLATE,
    contract_refs=(TEMPLATE, CONTRACT_REF, OUTPUT_REF, QUALITY_REF),
    appendices=("Open Questions",),
    id_prefixes={"PH": "Phase Progression", "HO": "Handoffs", "ES": "Escalations",
                 "FU": "Follow-Up Actions", "Q": "Open Questions"},
    # A coordination record quotes the gate evidence and the command output it read. The
    # vendor-token prohibition still applies; the fenced-block ceiling that governs a plan does
    # not.
    max_fenced_blocks=12,
    allow_diff_markers=False,
    sections=(
        ac.Section("Metadata", fields=(
            ac.Field("Orchestration ID"),
            ac.Field("Coordinator"),
            ac.Field("Run under coordination"),
            ac.Field("Coordination date"),
        )),
        ac.Section("Coordination Scope", fields=(
            ac.Field("In scope", min_words=3),
            ac.Field("Out of scope"),
            ac.Field("Evidence examined", min_words=3),
        )),
        ac.Section("Phase Progression",
                   table=ac.Table(PHASE_TABLE, min_rows=1, id_column="ID")),
        ac.Section("Progression Summary", fields=(
            ac.Field("Phases coordinated", pattern=r"\d+"),
            ac.Field("Complete", pattern=r"\d+"),
            ac.Field("Blocked", pattern=r"\d+"),
            ac.Field("Not started", pattern=r"\d+"),
        )),
        ac.Section("Handoffs",
                   table=ac.Table(HANDOFF_TABLE, min_rows=1, id_column="ID")),
        ac.Section("Escalations",
                   table=ac.Table(ESCALATION_TABLE, min_rows=1, id_column="ID")),
        ac.Section("Follow-Up Actions",
                   table=ac.Table(FOLLOWUP_TABLE, min_rows=1, id_column="ID")),
        ac.Section("Operational Status", fields=(
            ac.Field("Deployment state", enum=DEPLOYMENT_STATES),
            ac.Field("Environment"),
            ac.Field("Monitoring health"),
            ac.Field("Rollback position"),
        )),
        ac.Section("Coordination Position", fields=(
            ac.Field("Decision", enum=DISPOSITIONS),
            ac.Field("Rationale", min_words=5),
            ac.Field("Blocking escalations outstanding"),
            ac.Field("Closure recommendation"),
        )),
    ),
    obligations=(
        ("N1", QUALITY_REF + "#not-machine-checkable-obligations",
         "The sequence the run executed was the right sequence for the dependencies it carried"),
        ("N2", QUALITY_REF + "#not-machine-checkable-obligations",
         "Every escalation raised was one this role could not resolve within its own authority"),
        ("N3", QUALITY_REF + "#not-machine-checkable-obligations",
         "No follow-up action records as deferred work something the run was obliged to finish"),
    ),
)


def _rows(body: str, columns: tuple) -> list:
    """Table rows as dicts keyed by column name, or [] when the table is absent."""
    headers, rows = ac.find_table(body, columns)
    if not headers:
        return []
    idx = {c.lower(): ac.column_index(headers, c) for c in columns}
    out = []
    for r in rows:
        out.append({name: (strip_md(r[i]) if 0 <= i < len(r) else "")
                    for name, i in idx.items()})
    return out


def _gated(row: dict) -> bool:
    """Whether this phase row declares a gate that carries a decision at all.

    A Phase Model row may declare `none` in its Gate column, which is a real value rather than a
    missing one: the phase transitions without a gate. Rows like that are excluded from the
    gate-evidence and producer-exclusion checks, because there is no decision to record and no
    authority to misassign.
    """
    gate = row.get("gate", "").strip().lower()
    return bool(gate) and not ac.is_none_marker(gate) and gate not in ("none", "not-applicable")


def semantic_checks(rep, contract):
    """The orchestrator's own decision rules and constraints, as checks."""
    meta = getattr(rep, "meta", {}) or {}
    body_of = getattr(rep, "body_of", {}) or {}

    def add(cid, ref, severity, description, result, detail=""):
        rep.checks.append(Check(cid, ref, severity, description, result, detail))

    phases = _rows(body_of.get("Phase Progression", ""), PHASE_TABLE)
    handoffs = _rows(body_of.get("Handoffs", ""), HANDOFF_TABLE)
    escalations = _rows(body_of.get("Escalations", ""), ESCALATION_TABLE)
    followups = _rows(body_of.get("Follow-Up Actions", ""), FOLLOWUP_TABLE)

    disposition = str(meta.get("disposition") or "").strip().lower()
    basis = str(meta.get("coordinationBasis") or "").strip().lower()
    position = ac.parse_field_bullets(body_of.get("Coordination Position", ""))
    decision = strip_md(position.get("decision", "")).lower()

    # ---------------------------------------------------------------- O1 one disposition
    add("O1", TEMPLATE, "Blocking",
        "The metadata disposition and the recorded Decision are the same",
        "pass" if disposition in DISPOSITIONS and disposition == decision else "fail",
        f"metadata disposition={disposition!r} section decision={decision!r}")

    # ---------------------------------------------------------------- O2 vocabularies
    # A coordination record whose gate decisions and escalation states are spelled freehand
    # cannot be compared against another run of the same phase, which is the whole point of
    # keeping one.
    bad_prog = [p["id"] for p in phases
                if p["progression"].lower() not in PROGRESSIONS]
    bad_gate_dec = [p["id"] for p in phases
                    if p["gate decision"].lower() not in GATE_DECISIONS
                    and not ac.is_none_marker(p["gate decision"])]
    bad_accept = [h["id"] for h in handoffs
                  if h["accepted"].lower() not in HANDOFF_ACCEPTED]
    bad_esc_sev = [e["id"] for e in escalations
                   if e["severity"].lower() not in ac.SEVERITIES]
    bad_esc_cat = [e["id"] for e in escalations
                   if e["category"].lower() not in ESCALATION_CATEGORIES]
    bad_esc_status = [e["id"] for e in escalations
                      if e["status"].lower() not in ESCALATION_STATUSES]
    bad_fu_cat = [f["id"] for f in followups
                  if f["category"].lower() not in FOLLOWUP_CATEGORIES]
    bad_fu_sev = [f["id"] for f in followups
                  if f["severity"].lower() not in ac.SEVERITIES]
    bad_fu_status = [f["id"] for f in followups
                     if f["status"].lower() not in FOLLOWUP_STATUSES]
    bad_basis = basis not in BASES
    ok = phases and not (bad_prog or bad_gate_dec or bad_accept or bad_esc_sev or bad_esc_cat
                         or bad_esc_status or bad_fu_cat or bad_fu_sev or bad_fu_status
                         or bad_basis)
    add("O2", "rule-engine.md", "Blocking",
        "Every progression, gate decision, handoff state, escalation field, follow-up field, "
        "and the coordination basis come from the declared vocabularies",
        "pass" if ok else "fail",
        f"invalid progression={bad_prog} gateDecision={bad_gate_dec} accepted={bad_accept} "
        f"escalationSeverity={bad_esc_sev} escalationCategory={bad_esc_cat} "
        f"escalationStatus={bad_esc_status} followUpCategory={bad_fu_cat} "
        f"followUpSeverity={bad_fu_sev} followUpStatus={bad_fu_status} "
        f"basis={basis!r} valid={not bad_basis}"
        if not ok else f"basis={basis!r}, {len(phases)} phase(s), {len(handoffs)} handoff(s), "
        f"{len(escalations)} escalation(s), {len(followups)} follow-up(s)")

    # ---------------------------------------------------------------- O3 no count without a recount
    summary = ac.parse_field_bullets(body_of.get("Progression Summary", ""))
    counted = {
        "phases coordinated": len(phases),
        "complete": sum(1 for p in phases if p["progression"].lower() == "complete"),
        "blocked": sum(1 for p in phases if p["progression"].lower() == "blocked"),
        "not started": sum(1 for p in phases if p["progression"].lower() == "not-started"),
    }
    drift = []
    for label, actual in counted.items():
        stated = strip_md(summary.get(label, ""))
        if not stated.isdigit() or int(stated) != actual:
            drift.append(f"{label}: stated={stated!r} counted={actual}")
    add("O3", QUALITY_REF + "#arithmetic-checks", "Blocking",
        "The progression summary recomputes exactly from the phase progression table",
        "pass" if not drift else "fail",
        f"drift: {drift}" if drift
        else f"summary equals the counted progression: {counted}")

    # ---------------------------------------------------------------- O4 Producer Exclusion Rule
    # The characteristic coordinator failure: recording itself as the authority that approved the
    # gate its own artifact is the evidence for. `workflows/workflow-gate-matrix.md` assigns those
    # gates elsewhere precisely so that the producer cannot decide them, and a record that names
    # this role as the decider has either bypassed that assignment or misreported it. Either way
    # the progression it claims rests on a decision nobody with the authority to take it took.
    self_decided = [p["id"] for p in phases
                    if _gated(p)
                    and canonical_agent_id(p["owner"].lower()) == AGENT
                    and canonical_agent_id(p["decided by"].lower()) == AGENT]
    add("O4", "workflows/workflow-gate-matrix.md#producer-exclusion-rule", "Blocking",
        "No gate assessing this role's own output is recorded as decided by this role",
        "pass" if not self_decided else "fail",
        f"producer decided its own gate at {self_decided}" if self_decided
        else f"{sum(1 for p in phases if _gated(p) and canonical_agent_id(p['owner'].lower()) == AGENT)} gated "
             f"phase(s) owned by {AGENT}, none self-decided")

    # ---------------------------------------------------------------- O5 no transition without a gate
    # "No progression without a recorded gate decision." A phase this record calls complete, whose
    # Phase Model row declares a gate, must name the decision and the evidence behind it. Without
    # both, the record is asserting that the run advanced rather than showing that it was allowed
    # to.
    ungated = []
    for p in phases:
        if p["progression"].lower() != "complete" or not _gated(p):
            continue
        dec = p["gate decision"].lower()
        ev = p["evidence"]
        if dec not in ("approved", "rejected") or not ev or ac.is_none_marker(ev):
            ungated.append(p["id"])
    add("O5", CONTRACT_REF + "#decision-rules", "Blocking",
        "Every phase recorded complete at a gate names the recorded decision and its evidence",
        "pass" if not ungated else "fail",
        f"complete without a recorded gate decision and evidence: {ungated}" if ungated
        else f"{counted['complete']} complete phase(s), each gated one carrying a decision "
             f"and evidence")

    # ---------------------------------------------------------------- O6 clean closure is clean
    unresolved = [e["id"] for e in escalations
                  if e["severity"].lower() in ("critical", "high")
                  and e["status"].lower() not in ("resolved", "accepted-risk")]
    ok = not (decision in CLOSING_DISPOSITIONS and unresolved)
    add("O6", CONTRACT_REF + "#policy-constraints", "Blocking",
        "A closure of either kind carries no unresolved critical or high escalation",
        "pass" if ok else "fail",
        f"decision={decision!r} with unresolved {unresolved}" if not ok
        else f"decision={decision!r}, {len(unresolved)} unresolved critical/high escalation(s)")

    # ---------------------------------------------------------------- O7 closure covers the run
    # A closure may not sit on top of a phase that never finished. A run with work left blocked or
    # unstarted is `held`, or it is `closed-with-followups` carrying that work as a follow-up; it
    # is not `closed`.
    unfinished = [p["id"] for p in phases
                  if p["progression"].lower() in ("blocked", "not-started")]
    ok = decision not in CLEAN_DISPOSITIONS or not unfinished
    add("O7", CONTRACT_REF + "#decision-rules", "Blocking",
        "An unqualified closure carries no phase left blocked or not started",
        "pass" if ok else "fail",
        f"decision={decision!r} with unfinished phases {unfinished}" if not ok
        else f"decision={decision!r}, {len(unfinished)} phase(s) blocked or not started")

    # ---------------------------------------------------------------- O8 the position agrees
    outstanding = position.get("blocking escalations outstanding", "")
    stated_ids = set(ac.referenced_ids(outstanding, "ES"))
    blocking_open = {e["id"] for e in escalations
                     if e["severity"].lower() in ("critical", "high")
                     and e["status"].lower() == "open"}
    if ac.is_none_marker(outstanding):
        agrees = not blocking_open
    else:
        agrees = stated_ids == blocking_open
    add("O8", TEMPLATE, "Correctable",
        "The outstanding blocking escalations named in the position are exactly the open "
        "critical and high escalations",
        "pass" if agrees else "fail",
        f"position names {sorted(stated_ids) or 'none'}; table carries "
        f"{sorted(blocking_open) or 'none'}")

    # ---------------------------------------------------------------- O9 the basis is served
    # A deployment record whose operational fields say nothing is not a deployment record, and an
    # unfinished position that states no cause leaves the next run to guess why it stopped. Both
    # are the same failure: the artifact's own basis or disposition demands content it did not
    # carry.
    ops = ac.parse_field_bullets(body_of.get("Operational Status", ""))
    dep_state = strip_md(ops.get("deployment state", "")).lower()
    unserved = []
    if basis == "deployment":
        if dep_state in ("", "not-applicable"):
            unserved.append(f"deployment basis with deployment state {dep_state!r}")
        for label in ("monitoring health", "rollback position"):
            if ac.is_none_marker(strip_md(ops.get(label, ""))):
                unserved.append(f"deployment basis with no {label}")
    if decision == "rolled-back" and dep_state != "rolled-back":
        unserved.append(f"rolled-back decision with deployment state {dep_state!r}")
    if decision in UNFINISHED_DISPOSITIONS:
        rationale = strip_md(position.get("rationale", ""))
        if len(rationale.split()) < 5:
            unserved.append(f"{decision!r} decision with no stated cause")
    if decision == "closed-with-followups" and not followups:
        unserved.append("closed-with-followups with no follow-up action recorded")
    add("O9", OUTPUT_REF + "#basis-obligations", "Blocking",
        "The coordination basis and the recorded disposition each carry the content they "
        "oblige",
        "pass" if not unserved else "fail",
        f"unserved: {unserved}" if unserved
        else f"basis={basis!r}, decision={decision!r}, deployment state={dep_state!r}")

    rep.counts["phases"] = len(phases)
    rep.counts["handoffs"] = len(handoffs)
    rep.counts["escalations"] = len(escalations)
    rep.counts["followUps"] = len(followups)
    rep.counts["disposition"] = disposition
    rep.counts["coordinationBasis"] = basis
    rep.counts["progression"] = {
        p: sum(1 for r in phases if r["progression"].lower() == p) for p in PROGRESSIONS}
    rep.counts["escalationSeverity"] = {
        level: sum(1 for e in escalations if e["severity"].lower() == level)
        for level in ac.SEVERITIES}
    return rep


def validate(artifact_path, envelope: dict | None = None):
    rep = ac.run_contract(CONTRACT, Path(artifact_path), envelope)
    return semantic_checks(rep, CONTRACT)


def main():
    return ac.cli(CONTRACT, extra=semantic_checks)


if __name__ == "__main__":
    sys.exit(main())
