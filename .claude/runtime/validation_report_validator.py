#!/usr/bin/env python3
"""Validation Engine: validation-report.md conformance.

Mechanically checks a produced `validation-report.md` against the contract set that governs it:

  - `.claude/templates/validation-report.md`   rendered form, section and field labels
  - `.claude/agents/qa/identity.md`        behavioural contract for the validation lens
  - `.claude/agents/qa/output.md`          structural contract this artifact renders to
  - `.claude/agents/qa/quality.md`         QA's own self-verification checks

One artifact type carries every validation output the framework asks for: the `fix-bug`
regression evidence, both `refactor` validations, the `review-pull-request` test adequacy
verdict, and the `release` candidate verdict. `templates/validation-report.md` records why they
are one type: each is a criterion-by-criterion result set over a defined validation scope,
closing with a readiness verdict. The `validationBasis` field carries the lens; the structure
does not change with it.

Scope boundary: `Q1` to `Q7` are QA's own decision rules and constraints, expressed as checks
over the artifact. Whether the chosen test depth was the right depth for the risk is QA's
judgement and is recorded as a not-machine-checkable obligation. Whether a pass verdict
contradicts the results and defects it sits on top of is not judgement, and is enforced.
"""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import artifact_contract as ac  # noqa: E402
from artifact_lib import Check, strip_md  # noqa: E402

TEMPLATE = "templates/validation-report.md"
# QA's authoritative contract is a module set, not a single file. Each check below names the
# module that actually states the rule it enforces, so a failure points at the document that
# decides it rather than at the agent in general.
CONTRACT_REF = "agents/qa/identity.md"
OUTPUT_REF = "agents/qa/output.md"
QUALITY_REF = "agents/qa/quality.md"

VERDICTS = ("pass", "pass-with-reservations", "fail")
RESULTS = ("met", "not-met", "blocked")
DEFECT_STATUSES = ("open", "resolved", "accepted-risk")
REPRODUCIBILITY = ("always", "intermittent", "once", "not-reproduced")
CATEGORIES = ("functional", "regression", "integration", "performance", "security",
              "data", "usability", "operational")
BASES = ("regression", "safety-net", "behavioral-parity", "test-risk", "release-candidate")

CRITERIA_TABLE = ("ID", "Criterion", "Source", "Method", "Result", "Evidence")
DEFECT_TABLE = ("ID", "Severity", "Category", "Location", "Symptom", "Reproducibility",
                "Status")

CONTRACT = ac.ArtifactContract(
    artifact="validation-report.md",
    # One producer. Unlike a review package, no second role authors a validation report:
    # validating acceptance and release thresholds is QA's alone.
    producers=("qa",),
    metadata_key="validationReport",
    metadata_fields=("reportId", "validationReference", "validationBasis", "sourceInputs",
                     "producedBy", "agentVersion", "schemaVersion", "status", "verdict",
                     "inputDigest", "contextDigest"),
    statuses=ac.STATUSES_COMPLETE,
    template_ref=TEMPLATE,
    contract_refs=(TEMPLATE, CONTRACT_REF, OUTPUT_REF, QUALITY_REF),
    appendices=("Open Questions",),
    id_prefixes={"AC": "Acceptance Criteria Results", "DF": "Defects",
                 "Q": "Open Questions"},
    # A validation report quotes the output of the checks it ran. The vendor-token
    # prohibition still applies; the fenced-block and diff-marker prohibitions that govern a
    # plan or a design do not.
    max_fenced_blocks=12,
    allow_diff_markers=True,
    sections=(
        ac.Section("Metadata", fields=(
            ac.Field("Validation ID"),
            ac.Field("Validator"),
            ac.Field("Change under validation"),
            ac.Field("Validation date"),
        )),
        ac.Section("Validation Scope", fields=(
            ac.Field("In scope", min_words=3),
            ac.Field("Out of scope"),
            ac.Field("Evidence examined", min_words=3),
        )),
        ac.Section("Test Strategy", fields=(
            ac.Field("Risk basis", min_words=3),
            ac.Field("Levels executed"),
            ac.Field("Environment"),
            ac.Field("Not executed"),
        )),
        ac.Section("Acceptance Criteria Results",
                   table=ac.Table(CRITERIA_TABLE, min_rows=1, id_column="ID")),
        ac.Section("Execution Summary", fields=(
            ac.Field("Criteria validated", pattern=r"\d+"),
            ac.Field("Met", pattern=r"\d+"),
            ac.Field("Not met", pattern=r"\d+"),
            ac.Field("Blocked", pattern=r"\d+"),
        )),
        ac.Section("Defects", table=ac.Table(DEFECT_TABLE, min_rows=1, id_column="ID")),
        ac.Section("Regression Assessment", fields=(
            ac.Field("Regression scope"),
            ac.Field("Regressions detected"),
            ac.Field("Coverage of changed behavior"),
            ac.Field("Untested areas"),
        )),
        ac.Section("Residual Risk", fields=(
            ac.Field("Accepted risk"),
            ac.Field("Unmitigated risk"),
            ac.Field("Monitoring required"),
        )),
        ac.Section("Verdict", fields=(
            ac.Field("Decision", enum=VERDICTS),
            ac.Field("Rationale", min_words=5),
            ac.Field("Blocking defects outstanding"),
            ac.Field("Readiness recommendation"),
        )),
    ),
    obligations=(
        ("N1", QUALITY_REF + "#not-machine-checkable-obligations",
         "The executed test depth matches the risk the change actually carries"),
        ("N2", QUALITY_REF + "#not-machine-checkable-obligations",
         "No criterion was recorded as met on evidence that does not demonstrate it"),
        ("N3", QUALITY_REF + "#not-machine-checkable-obligations",
         "Every regression the change could plausibly cause was looked for"),
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
    """QA's own decision rules and constraints, as checks."""
    meta = getattr(rep, "meta", {}) or {}
    body_of = getattr(rep, "body_of", {}) or {}

    def add(cid, ref, severity, description, result, detail=""):
        rep.checks.append(Check(cid, ref, severity, description, result, detail))

    criteria = _rows(body_of.get("Acceptance Criteria Results", ""), CRITERIA_TABLE)
    defects = _rows(body_of.get("Defects", ""), DEFECT_TABLE)
    verdict = str(meta.get("verdict") or "").strip().lower()
    basis = str(meta.get("validationBasis") or "").strip().lower()
    decision = strip_md(
        ac.parse_field_bullets(body_of.get("Verdict", "")).get("decision", "")).lower()

    add("Q1", TEMPLATE, "Blocking",
        "The metadata verdict and the recorded Decision are the same",
        "pass" if verdict in VERDICTS and verdict == decision else "fail",
        f"metadata verdict={verdict!r} section decision={decision!r}")

    # Q2 -- vocabularies. A validation whose results and defects are spelled freehand cannot
    # be compared against another run of the same phase.
    bad_result = [c["id"] for c in criteria if c["result"].lower() not in RESULTS]
    bad_sev = [d["id"] for d in defects if d["severity"].lower() not in ac.SEVERITIES]
    bad_cat = [d["id"] for d in defects if d["category"].lower() not in CATEGORIES]
    bad_repro = [d["id"] for d in defects
                 if d["reproducibility"].lower() not in REPRODUCIBILITY]
    bad_status = [d["id"] for d in defects if d["status"].lower() not in DEFECT_STATUSES]
    bad_basis = basis not in BASES
    ok = criteria and not (bad_result or bad_sev or bad_cat or bad_repro or bad_status
                           or bad_basis)
    add("Q2", "rule-engine.md", "Blocking",
        "Every result, defect field, and the validation basis come from the declared "
        "vocabularies",
        "pass" if ok else "fail",
        f"invalid result={bad_result} severity={bad_sev} category={bad_cat} "
        f"reproducibility={bad_repro} status={bad_status} "
        f"basis={basis!r} valid={not bad_basis}"
        if not ok else f"basis={basis!r}, {len(criteria)} criteria, {len(defects)} defect(s)")

    # Q3 -- "No count without a recount": the execution summary is recomputed, so it cannot
    # drift from the results it summarises.
    summary = ac.parse_field_bullets(body_of.get("Execution Summary", ""))
    counted = {
        "criteria validated": len(criteria),
        "met": sum(1 for c in criteria if c["result"].lower() == "met"),
        "not met": sum(1 for c in criteria if c["result"].lower() == "not-met"),
        "blocked": sum(1 for c in criteria if c["result"].lower() == "blocked"),
    }
    drift = []
    for label, actual in counted.items():
        stated = strip_md(summary.get(label, ""))
        if not stated.isdigit() or int(stated) != actual:
            drift.append(f"{label}: stated={stated!r} counted={actual}")
    add("Q3", QUALITY_REF + "#arithmetic-checks", "Blocking",
        "The execution summary recomputes exactly from the acceptance criteria results",
        "pass" if not drift else "fail",
        f"drift: {drift}" if drift
        else f"summary equals the counted results: {counted}")

    # Q4 -- "Critical defects block progression" and "No pass with unresolved critical
    # defects."
    unresolved = [d["id"] for d in defects
                  if d["severity"].lower() in ("critical", "high")
                  and d["status"].lower() != "resolved"]
    ok = not (decision == "pass" and unresolved)
    add("Q4", CONTRACT_REF + "#policy-constraints", "Blocking",
        "An unqualified pass carries no unresolved critical or high defect",
        "pass" if ok else "fail",
        f"decision=pass with unresolved {unresolved}" if not ok
        else f"decision={decision!r}, {len(unresolved)} unresolved critical/high defect(s)")

    # Q5 -- "No criterion is met without evidence." This is the rule that stops a validation
    # from becoming an assertion that the change works.
    unevidenced = [c["id"] for c in criteria
                   if c["result"].lower() == "met"
                   and (not c["evidence"] or ac.is_none_marker(c["evidence"]))]
    add("Q5", CONTRACT_REF + "#decision-rules", "Blocking",
        "Every criterion recorded as met names the evidence that demonstrates it",
        "pass" if not unevidenced else "fail",
        f"met with no evidence: {unevidenced}" if unevidenced
        else f"{counted['met']} criteria met, each with recorded evidence")

    # Q6 -- a pass may not sit on top of a criterion that was never resolved, and a report
    # that could not reach part of its scope may not read as a clean one.
    unmet = [c["id"] for c in criteria if c["result"].lower() in ("not-met", "blocked")]
    ok = decision != "pass" or not unmet
    add("Q6", CONTRACT_REF + "#decision-rules", "Blocking",
        "An unqualified pass carries no criterion left not-met or blocked",
        "pass" if ok else "fail",
        f"decision=pass with unresolved criteria {unmet}" if not ok
        else f"decision={decision!r}, {len(unmet)} criteria not met or blocked")

    # Q7 -- the Verdict section's outstanding-defects statement agrees with the table.
    outstanding = ac.parse_field_bullets(body_of.get("Verdict", "")).get(
        "blocking defects outstanding", "")
    stated_ids = set(ac.referenced_ids(outstanding, "DF"))
    blocking_open = {d["id"] for d in defects
                     if d["severity"].lower() in ("critical", "high")
                     and d["status"].lower() == "open"}
    if ac.is_none_marker(outstanding):
        agrees = not blocking_open
    else:
        agrees = stated_ids == blocking_open
    add("Q7", TEMPLATE, "Correctable",
        "The outstanding blocking defects named in the verdict are exactly the open "
        "critical and high defects",
        "pass" if agrees else "fail",
        f"verdict names {sorted(stated_ids) or 'none'}; table carries "
        f"{sorted(blocking_open) or 'none'}")

    rep.counts["criteria"] = len(criteria)
    rep.counts["defects"] = len(defects)
    rep.counts["verdict"] = verdict
    rep.counts["validationBasis"] = basis
    rep.counts["results"] = {r: sum(1 for c in criteria if c["result"].lower() == r)
                             for r in RESULTS}
    rep.counts["severity"] = {
        level: sum(1 for d in defects if d["severity"].lower() == level)
        for level in ac.SEVERITIES}
    return rep


def validate(artifact_path, envelope: dict | None = None):
    rep = ac.run_contract(CONTRACT, Path(artifact_path), envelope)
    return semantic_checks(rep, CONTRACT)


def main():
    return ac.cli(CONTRACT, extra=semantic_checks)


if __name__ == "__main__":
    sys.exit(main())
