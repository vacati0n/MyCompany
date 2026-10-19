#!/usr/bin/env python3
"""Validation Engine: technical-recommendation.md conformance.

Mechanically checks a produced `technical-recommendation.md` against the contract set that
governs it:

  - `.claude/templates/technical-recommendation.md`  rendered form, section and field labels
  - `.claude/agents/tech-lead/identity.md`       behavioural contract for the decision lens
  - `.claude/agents/tech-lead/output.md`         structural contract this artifact renders to
  - `.claude/agents/tech-lead/quality.md`        the tech lead's own self-verification checks

One artifact type carries every technical leadership output the framework asks for: both
`investigate` decision phases, both `research` decision phases, the `review-pull-request` merge
decision, and the `release` readiness assessment. `templates/technical-recommendation.md` records
why they are one type: each is a criteria-based comparison of named options over a stated decision
context, closing with a recommendation and a readiness position. The `decisionBasis` field carries
the lens; the structure does not change with it.

Scope boundary: `T1` to `T8` are the tech lead's own decision rules and constraints, expressed as
checks over the artifact. Whether the criteria were fixed before the options were scored, and
whether an effort figure is honest rather than optimistic, are judgement and are recorded as
not-machine-checkable obligations. Whether a recommendation names an option the artifact never
evaluated, whether a summary agrees with the tables under it, whether a `proceed` sits on top of an
open blocker, and whether the producer awarded itself the gate are not judgement, and are enforced.
"""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import artifact_contract as ac  # noqa: E402
from artifact_lib import Check, strip_md  # noqa: E402

TEMPLATE = "templates/technical-recommendation.md"
# The tech lead's authoritative contract is a module set, not a single file. Each check below
# names the module that actually states the rule it enforces, so a failure points at the document
# that decides it rather than at the agent in general.
CONTRACT_REF = "agents/tech-lead/identity.md"
OUTPUT_REF = "agents/tech-lead/output.md"
QUALITY_REF = "agents/tech-lead/quality.md"

PRODUCER = "tech-lead"

BASES = ("option-analysis", "recommendation", "merge-decision", "release-readiness")
DECISIONS = ("proceed", "proceed-with-conditions", "do-not-proceed", "deferred")
PRIORITIES = ("must-have", "high", "medium", "low")
EFFORTS = ("trivial", "small", "medium", "large", "unknown")
REVERSIBILITY = ("reversible", "costly-to-reverse", "irreversible")
LIKELIHOODS = ("certain", "likely", "possible", "unlikely")
RISK_STATUSES = ("open", "mitigated", "accepted", "resolved")
# A blocker is an entry severe enough that delivery does not proceed around it.
BLOCKING_SEVERITIES = ("critical", "high")

CRITERIA_TABLE = ("ID", "Criterion", "Why it matters", "Priority", "Source")
OPTION_TABLE = ("ID", "Option", "Summary", "Effort", "Delivery risk", "Reversibility",
                "Evidence")
TRADEOFF_TABLE = ("Option", "Criteria met", "Criteria missed", "Strengths", "Weaknesses",
                  "Sequencing implication")
RISK_TABLE = ("ID", "Risk or blocker", "Severity", "Likelihood", "Delivery impact",
              "Mitigation", "Owner", "Status")

CONTRACT = ac.ArtifactContract(
    artifact="technical-recommendation.md",
    # One producer. Delivery feasibility and the tradeoff judgement that follows from it are
    # this role's alone; no other agent may emit a recommendation of this type.
    producers=(PRODUCER,),
    metadata_key="technicalRecommendation",
    metadata_fields=("recommendationId", "decisionReference", "decisionBasis", "sourceInputs",
                     "producedBy", "agentVersion", "schemaVersion", "status",
                     "recommendedOption", "readinessDecision", "inputDigest", "contextDigest"),
    statuses=ac.STATUSES_COMPLETE,
    template_ref=TEMPLATE,
    contract_refs=(TEMPLATE, CONTRACT_REF, OUTPUT_REF, QUALITY_REF),
    appendices=("Open Questions",),
    # `EC` precedes `O` in the alternation the identifier scan builds, so `EC-001` is read as
    # one identifier rather than as a stray `C-001`.
    id_prefixes={"EC": "Evaluation Criteria", "O": "Options",
                 "RK": "Risk and Blocker Register", "Q": "Open Questions"},
    # This artifact reasons about a change; it does not quote one. Two blocks above the
    # metadata block are allowed for quoted output of a permitted read-only command, which is
    # what an effort or sequencing claim may legitimately rest on.
    max_fenced_blocks=3,
    allow_diff_markers=False,
    sections=(
        ac.Section("Metadata", fields=(
            ac.Field("Recommendation ID"),
            ac.Field("Decision owner"),
            ac.Field("Requested by"),
            ac.Field("Decision date"),
        )),
        ac.Section("Decision Context", fields=(
            ac.Field("Decision to make", min_words=3),
            ac.Field("Delivery constraints"),
            ac.Field("Assumptions in force"),
        )),
        ac.Section("Evaluation Criteria",
                   table=ac.Table(CRITERIA_TABLE, min_rows=1, id_column="ID")),
        # Two rows, not one: a single option is a proposal, not an evaluation. The floor is
        # structural because the rule is absolute -- `output.md` section 4.
        ac.Section("Options",
                   table=ac.Table(OPTION_TABLE, min_rows=2, id_column="ID")),
        ac.Section("Tradeoff Analysis",
                   table=ac.Table(TRADEOFF_TABLE, min_rows=2, id_column="Option")),
        ac.Section("Risk and Blocker Register",
                   table=ac.Table(RISK_TABLE, min_rows=1, id_column="ID")),
        ac.Section("Assessment Summary", fields=(
            ac.Field("Criteria applied", pattern=r"\d+"),
            ac.Field("Options evaluated", pattern=r"\d+"),
            ac.Field("Risks and blockers recorded", pattern=r"\d+"),
            ac.Field("Blocking items open", pattern=r"\d+"),
        )),
        ac.Section("Recommendation", fields=(
            ac.Field("Recommended option", pattern=r"O-\d{3}|deferred"),
            ac.Field("Rationale", min_words=5),
            ac.Field("Preconditions"),
            ac.Field("Options rejected"),
        )),
        ac.Section("Delivery Impact", fields=(
            ac.Field("Effort and capacity"),
            ac.Field("Sequencing constraints"),
            ac.Field("Dependencies"),
            ac.Field("Reversal plan"),
        )),
        ac.Section("Readiness", fields=(
            ac.Field("Recommended decision", enum=DECISIONS),
            ac.Field("Conditions to satisfy"),
            ac.Field("Blocking items outstanding"),
            ac.Field("Deciding authority"),
        )),
    ),
    obligations=(
        ("N1", QUALITY_REF + "#not-machine-checkable-obligations",
         "The criteria were fixed before the options were scored, not adjusted to fit one"),
        ("N2", QUALITY_REF + "#not-machine-checkable-obligations",
         "Every option genuinely open under the recorded constraints was enumerated"),
        ("N3", QUALITY_REF + "#not-machine-checkable-obligations",
         "Each effort and severity position reflects the evidence, not the schedule"),
        ("N4", QUALITY_REF + "#not-machine-checkable-obligations",
         "No quality gate was traded, weakened, or routed around to reach the recommendation"),
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


def semantic_checks(rep, contract):
    """The tech lead's own decision rules and constraints, as checks."""
    meta = getattr(rep, "meta", {}) or {}
    body_of = getattr(rep, "body_of", {}) or {}

    def add(cid, ref, severity, description, result, detail=""):
        rep.checks.append(Check(cid, ref, severity, description, result, detail))

    criteria = _rows(body_of.get("Evaluation Criteria", ""), CRITERIA_TABLE)
    options = _rows(body_of.get("Options", ""), OPTION_TABLE)
    tradeoffs = _rows(body_of.get("Tradeoff Analysis", ""), TRADEOFF_TABLE)
    risks = _rows(body_of.get("Risk and Blocker Register", ""), RISK_TABLE)

    basis = str(meta.get("decisionBasis") or "").strip().lower()
    meta_option = strip_md(str(meta.get("recommendedOption") or "")).strip()
    meta_decision = str(meta.get("readinessDecision") or "").strip().lower()

    rec = ac.parse_field_bullets(body_of.get("Recommendation", ""))
    ready = ac.parse_field_bullets(body_of.get("Readiness", ""))
    section_option = strip_md(rec.get("recommended option", "")).strip()
    section_decision = strip_md(ready.get("recommended decision", "")).strip().lower()
    authority = strip_md(ready.get("deciding authority", "")).strip()

    # T1 -- a metadata block edited apart from the prose it summarises is how a consumer routing
    # on metadata and a reader reading prose reach different conclusions about one artifact.
    agree_option = meta_option and meta_option == section_option
    agree_decision = meta_decision in DECISIONS and meta_decision == section_decision
    add("T1", TEMPLATE, "Blocking",
        "The metadata recommendation and readiness decision match the sections that state them",
        "pass" if agree_option and agree_decision else "fail",
        f"metadata recommendedOption={meta_option!r} section={section_option!r}; "
        f"metadata readinessDecision={meta_decision!r} section={section_decision!r}")

    # T2 -- vocabularies. A comparison whose dimensions are spelled freehand cannot be compared
    # against another run of the same phase, which is the point of running it the same way twice.
    bad_priority = [c["id"] for c in criteria if c["priority"].lower() not in PRIORITIES]
    bad_source = [c["id"] for c in criteria
                  if not c["source"] or ac.is_none_marker(c["source"])]
    bad_effort = [o["id"] for o in options if o["effort"].lower() not in EFFORTS]
    bad_risk = [o["id"] for o in options if o["delivery risk"].lower() not in ac.SEVERITIES]
    bad_rev = [o["id"] for o in options if o["reversibility"].lower() not in REVERSIBILITY]
    bad_sev = [r["id"] for r in risks if r["severity"].lower() not in ac.SEVERITIES]
    bad_like = [r["id"] for r in risks if r["likelihood"].lower() not in LIKELIHOODS]
    bad_status = [r["id"] for r in risks if r["status"].lower() not in RISK_STATUSES]
    bad_owner = [r["id"] for r in risks if not r["owner"] or ac.is_none_marker(r["owner"])]
    bad_basis = basis not in BASES
    ok = criteria and options and risks and not (
        bad_priority or bad_source or bad_effort or bad_risk or bad_rev
        or bad_sev or bad_like or bad_status or bad_owner or bad_basis)
    add("T2", "rule-engine.md", "Blocking",
        "The decision basis, every scale value, every criterion source, and every risk owner "
        "come from the declared vocabularies",
        "pass" if ok else "fail",
        f"invalid basis={basis!r} priority={bad_priority} source={bad_source} "
        f"effort={bad_effort} deliveryRisk={bad_risk} reversibility={bad_rev} "
        f"severity={bad_sev} likelihood={bad_like} status={bad_status} owner={bad_owner}"
        if not ok else f"basis={basis!r}, {len(criteria)} criteria, {len(options)} option(s), "
                       f"{len(risks)} risk(s)")

    # T3 -- "No count without a recount": the assessment summary is recomputed, so it cannot
    # drift from the tables it summarises.
    blocking_open = {r["id"] for r in risks
                     if r["severity"].lower() in BLOCKING_SEVERITIES
                     and r["status"].lower() == "open"}
    summary = ac.parse_field_bullets(body_of.get("Assessment Summary", ""))
    counted = {
        "criteria applied": len(criteria),
        "options evaluated": len(options),
        "risks and blockers recorded": len(risks),
        "blocking items open": len(blocking_open),
    }
    drift = []
    for label, actual in counted.items():
        stated = strip_md(summary.get(label, ""))
        if not stated.isdigit() or int(stated) != actual:
            drift.append(f"{label}: stated={stated!r} counted={actual}")
    add("T3", QUALITY_REF + "#semantic-checks", "Blocking",
        "The assessment summary recomputes exactly from the criteria, option, and risk tables",
        "pass" if not drift else "fail",
        f"drift: {drift}" if drift else f"summary equals the counted tables: {counted}")

    # T4 -- the rule this artifact exists to make mechanical. Recommending an option the
    # artifact never evaluated means recommending something the reader cannot check.
    defined = {o["id"] for o in options if o["id"]}
    assessed = {t["option"] for t in tradeoffs if t["option"]}
    if section_option.lower() == "deferred":
        ok, detail = True, "recommendation is deferred; no option identifier is claimed"
    else:
        ok = section_option in defined and section_option in assessed
        detail = (f"recommended {section_option!r}; defined options {sorted(defined)}; "
                  f"assessed in tradeoff analysis {sorted(assessed)}")
    add("T4", CONTRACT_REF + "#decision-rules", "Blocking",
        "The recommended option is one the artifact evaluated, or the decision is deferred",
        "pass" if ok else "fail", detail)

    # T5 -- an option listed but never scored is a decoy: it makes the comparison look wider
    # than it was.
    unassessed = sorted(defined - assessed)
    unknown = sorted(assessed - defined)
    duplicated = sorted({t["option"] for t in tradeoffs
                         if [x["option"] for x in tradeoffs].count(t["option"]) > 1})
    ok = len(defined) >= 2 and not (unassessed or unknown or duplicated)
    add("T5", OUTPUT_REF + "#5-tradeoff-analysis", "Blocking",
        "At least two options are recorded, and every one is assessed exactly once in the "
        "tradeoff analysis",
        "pass" if ok else "fail",
        f"options={len(defined)} unassessed={unassessed} not-an-option={unknown} "
        f"assessed-twice={duplicated}" if not ok
        else f"{len(defined)} option(s), each assessed once")

    # T6 -- invariant I7. The check that stops delivery pressure from being resolved in the
    # verdict line while everything above it stays honest.
    ok = section_decision != "proceed" or not blocking_open
    add("T6", CONTRACT_REF + "#policy-constraints", "Blocking",
        "An unqualified proceed carries no open critical or high blocker",
        "pass" if ok else "fail",
        f"decision=proceed with open {sorted(blocking_open)}" if not ok
        else f"decision={section_decision!r}, {len(blocking_open)} open blocking item(s)")

    # T7 -- the closing statement agrees with the register it claims to summarise.
    outstanding = ready.get("blocking items outstanding", "")
    stated_ids = set(ac.referenced_ids(outstanding, "RK"))
    if ac.is_none_marker(outstanding):
        agrees = not blocking_open
    else:
        agrees = stated_ids == blocking_open
    add("T7", TEMPLATE, "Correctable",
        "The outstanding blockers named at the close are exactly the open critical and high "
        "entries",
        "pass" if agrees else "fail",
        f"readiness names {sorted(stated_ids) or 'none'}; register carries "
        f"{sorted(blocking_open) or 'none'}")

    # T8 -- the Producer Exclusion Rule, made mechanical. This role decides more gates than any
    # other, which is exactly why the exclusion is enforced rather than remembered.
    named = bool(authority) and not ac.is_none_marker(authority)
    ok = named and PRODUCER not in authority.lower()
    add("T8", QUALITY_REF + "#semantic-checks", "Blocking",
        "The deciding authority is named and is not the agent that produced this artifact",
        "pass" if ok else "fail",
        f"deciding authority={authority!r}; the producer may not decide a gate over its own "
        f"evidence" if not ok else f"deciding authority={authority!r}")

    rep.counts["criteria"] = len(criteria)
    rep.counts["options"] = len(options)
    rep.counts["risks"] = len(risks)
    rep.counts["blockingItemsOpen"] = len(blocking_open)
    rep.counts["decisionBasis"] = basis
    rep.counts["recommendedOption"] = section_option
    rep.counts["readinessDecision"] = section_decision
    rep.counts["decidingAuthority"] = authority
    rep.counts["severity"] = {
        level: sum(1 for r in risks if r["severity"].lower() == level)
        for level in ac.SEVERITIES}
    return rep


def validate(artifact_path, envelope: dict | None = None):
    rep = ac.run_contract(CONTRACT, Path(artifact_path), envelope)
    return semantic_checks(rep, CONTRACT)


def main():
    return ac.cli(CONTRACT, extra=semantic_checks)


if __name__ == "__main__":
    sys.exit(main())
