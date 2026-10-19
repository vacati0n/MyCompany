#!/usr/bin/env python3
"""Validation Engine: review-package.md conformance.

Mechanically checks a produced `review-package.md` against the contract set that governs it:

  - `.claude/templates/review-package.md`             rendered form, section and field labels
  - `.claude/agents/dev-2-reviewer/identity.md`   behavioural contract for the quality lens
  - `.claude/agents/dev-2-reviewer/output.md`     structural contract this artifact renders to
  - `.claude/agents/dev-2-reviewer/quality.md`    the reviewer's own self-verification checks
  - `.claude/agents/architect/quality.md`             severity scale for the structural lens

One artifact type carries every review output the framework asks for: the
`implement-feature/quality-review` findings log, both `review-pull-request` assessments, and
the `release/artifact-packaging` packaging evidence. `templates/review-package.md` records why
they are one type: each is a severity-classified findings set over a defined scope, closing
with a readiness decision. The Category column carries the lens; the structure does not change
with it.

Scope boundary: `P1` to `P6` are the reviewer's own decision rules and constraints, expressed
as checks over the artifact. Whether a finding is worth raising is the reviewer's judgement and
is recorded as a not-machine-checkable obligation. Whether an approval contradicts the findings
it sits on top of is not judgement, and is enforced.
"""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import artifact_contract as ac  # noqa: E402
from artifact_lib import Check, strip_md  # noqa: E402

TEMPLATE = "templates/review-package.md"
# The reviewer's authoritative contract is a module set, not a single file. Each check below
# names the module that actually states the rule it enforces, so a failure points at the
# document that decides it rather than at the agent in general.
CONTRACT_REF = "agents/dev-2-reviewer/identity.md"
OUTPUT_REF = "agents/dev-2-reviewer/output.md"
QUALITY_REF = "agents/dev-2-reviewer/quality.md"

VERDICTS = ("approve", "approve-with-corrections", "reject")
FINDING_STATUSES = ("open", "resolved", "accepted-risk")
CATEGORIES = ("correctness", "maintainability", "standards", "architecture", "security",
              "test-adequacy", "packaging",
              # Junk-detection lenses reported by code-quality-scan/repository-quality-scan.
              "duplication", "dead-code", "over-abstraction", "generated-noise",
              "legacy-drift", "reviewability")

FINDING_TABLE = ("ID", "Severity", "Category", "Location", "Requirement", "Finding",
                 "Correction Request", "Status")
CR_TABLE = ("ID", "Addresses", "Required change", "Blocking", "Owner")

CONTRACT = ac.ArtifactContract(
    artifact="review-package.md",
    # Two producers, because `review-pull-request/structural-compliance` is owned by the
    # architecture role while every other review phase is owned by the reviewer.
    producers=("dev-2-reviewer", "architect"),
    metadata_key="reviewPackage",
    metadata_fields=("packageId", "reviewReference", "sourceInputs", "producedBy",
                     "agentVersion", "schemaVersion", "status", "verdict", "inputDigest",
                     "contextDigest"),
    statuses=ac.STATUSES_COMPLETE,
    template_ref=TEMPLATE,
    contract_refs=(TEMPLATE, CONTRACT_REF, OUTPUT_REF, QUALITY_REF),
    appendices=("Open Questions",),
    id_prefixes={"F": "Findings", "CR": "Correction Requests", "Q": "Open Questions"},
    # A review quotes the code it reviews. The vendor-token prohibition still applies; the
    # fenced-block and diff-marker prohibitions that govern a plan or a design do not.
    max_fenced_blocks=12,
    allow_diff_markers=True,
    sections=(
        ac.Section("Metadata", fields=(
            ac.Field("Review ID"),
            ac.Field("Reviewer"),
            ac.Field("Change under review"),
            ac.Field("Review date"),
        )),
        ac.Section("Review Scope", fields=(
            ac.Field("In scope", min_words=3),
            ac.Field("Out of scope"),
            ac.Field("Evidence reviewed", min_words=3),
        )),
        ac.Section("Findings", table=ac.Table(FINDING_TABLE, min_rows=1,
                                              optional_columns=("Correction Request",))),
        ac.Section("Severity Summary", fields=(
            ac.Field("Critical", pattern=r"\d+"),
            ac.Field("High", pattern=r"\d+"),
            ac.Field("Medium", pattern=r"\d+"),
            ac.Field("Low", pattern=r"\d+"),
        )),
        ac.Section("Standards and Architecture Conformance", fields=(
            ac.Field("Coding standards"),
            ac.Field("Architecture rules"),
            ac.Field("Security criteria"),
            ac.Field("Exceptions requested"),
        )),
        ac.Section("Test Adequacy Assessment", fields=(
            ac.Field("Test evidence reviewed"),
            ac.Field("Coverage of changed behavior"),
            ac.Field("Gaps requiring new tests"),
        )),
        ac.Section("Correction Requests", table=ac.Table(CR_TABLE, min_rows=1)),
        ac.Section("Residual Risk", fields=(
            ac.Field("Accepted risk"),
            ac.Field("Unmitigated risk"),
            ac.Field("Monitoring required"),
        )),
        ac.Section("Verdict", fields=(
            ac.Field("Decision", enum=VERDICTS),
            ac.Field("Rationale", min_words=5),
            ac.Field("Blocking findings outstanding"),
            ac.Field("Readiness recommendation"),
        )),
    ),
    obligations=(
        ("N1", QUALITY_REF + "#not-machine-checkable-obligations",
         "Every high-risk issue in the change under review was actually found"),
        ("N2", QUALITY_REF + "#not-machine-checkable-obligations",
         "No finding substitutes reviewer opinion for a policy-backed standard"),
        ("N3", QUALITY_REF + "#not-machine-checkable-obligations",
         "No severity was downscaled without evidence"),
    ),
)


def _findings(body_of: dict) -> list:
    """Findings as dicts keyed by column name, or [] when the table is absent."""
    headers, rows = ac.find_table(body_of.get("Findings", ""), FINDING_TABLE)
    if not headers:
        return []
    idx = {c.lower(): ac.column_index(headers, c) for c in FINDING_TABLE}
    out = []
    for r in rows:
        rec = {}
        for name, i in idx.items():
            rec[name] = strip_md(r[i]) if 0 <= i < len(r) else ""
        out.append(rec)
    return out


def semantic_checks(rep, contract):
    """The reviewer's own decision rules and constraints, as checks."""
    meta = getattr(rep, "meta", {}) or {}
    body_of = getattr(rep, "body_of", {}) or {}

    def add(cid, ref, severity, description, result, detail=""):
        rep.checks.append(Check(cid, ref, severity, description, result, detail))

    findings = _findings(body_of)
    verdict = str(meta.get("verdict") or "").strip().lower()
    decision = strip_md(
        ac.parse_field_bullets(body_of.get("Verdict", "")).get("decision", "")).lower()

    add("P1", TEMPLATE, "Blocking",
        "The metadata verdict and the recorded Decision are the same",
        "pass" if verdict in VERDICTS and verdict == decision else "fail",
        f"metadata verdict={verdict!r} section decision={decision!r}")

    # P2 -- severity and category vocabularies.
    bad_sev = [f["id"] for f in findings if f["severity"].lower() not in ac.SEVERITIES]
    bad_cat = [f["id"] for f in findings if f["category"].lower() not in CATEGORIES]
    bad_status = [f["id"] for f in findings if f["status"].lower() not in FINDING_STATUSES]
    ok = findings and not (bad_sev or bad_cat or bad_status)
    add("P2", "rule-engine.md", "Blocking",
        "Every finding carries a severity, a category, and a status from the declared "
        "vocabularies",
        "pass" if ok else "fail",
        f"invalid severity={bad_sev} category={bad_cat} status={bad_status}"
        if findings else "no findings table to check")

    # P3 -- "No severity downscaling without evidence": the summary is recomputed, so it
    # cannot drift from the findings it summarises.
    summary = ac.parse_field_bullets(body_of.get("Severity Summary", ""))
    drift = []
    for level in ac.SEVERITIES:
        stated = strip_md(summary.get(level, ""))
        actual = sum(1 for f in findings if f["severity"].lower() == level)
        if not stated.isdigit() or int(stated) != actual:
            drift.append(f"{level}: stated={stated!r} counted={actual}")
    add("P3", QUALITY_REF + "#arithmetic-checks", "Blocking",
        "The severity summary recomputes exactly from the findings table",
        "pass" if not drift else "fail",
        f"drift: {drift}" if drift
        else "summary equals the counted findings at every severity")

    # P4 -- "Critical defects block progression" and "No approval with unresolved critical
    # findings."
    unresolved = [f["id"] for f in findings
                  if f["severity"].lower() in ("critical", "high")
                  and f["status"].lower() != "resolved"]
    ok = not (decision == "approve" and unresolved)
    add("P4", CONTRACT_REF + "#policy-constraints", "Blocking",
        "An unqualified approval carries no unresolved critical or high finding",
        "pass" if ok else "fail",
        f"decision=approve with unresolved {unresolved}" if not ok
        else f"decision={decision!r}, {len(unresolved)} unresolved critical/high finding(s)")

    # P5 -- every critical or high finding has a correction request that addresses it.
    cr_headers, cr_rows = ac.find_table(body_of.get("Correction Requests", ""), CR_TABLE)
    addressed = set()
    if cr_headers:
        a_col = ac.column_index(cr_headers, "addresses")
        for r in cr_rows:
            cell = r[a_col] if 0 <= a_col < len(r) else ""
            addressed |= set(ac.referenced_ids(cell, "F"))
    missing = [f["id"] for f in findings
               if f["severity"].lower() in ("critical", "high")
               and f["status"].lower() == "open" and f["id"] not in addressed]
    add("P5", OUTPUT_REF + "#7-correction-requests", "Blocking",
        "Every open critical or high finding is addressed by a correction request",
        "pass" if not missing else "fail",
        f"unaddressed: {missing}" if missing
        else f"{len(addressed)} finding(s) addressed by "
             f"{len(cr_rows) if cr_headers else 0} request(s)")

    # P6 -- "Missing test evidence blocks approval."
    test_evidence = ac.parse_field_bullets(body_of.get("Test Adequacy Assessment", "")).get(
        "test evidence reviewed", "")
    has_evidence = bool(test_evidence) and not ac.is_none_marker(test_evidence)
    ok = has_evidence or decision == "reject"
    add("P6", CONTRACT_REF + "#decision-rules", "Blocking",
        "Approval is withheld when no test evidence was reviewed",
        "pass" if ok else "fail",
        f"test evidence reviewed={test_evidence!r} with decision={decision!r}" if not ok
        else f"test evidence recorded={has_evidence}, decision={decision!r}")

    # P7 -- the Verdict section's outstanding-findings statement agrees with the table.
    outstanding = ac.parse_field_bullets(body_of.get("Verdict", "")).get(
        "blocking findings outstanding", "")
    stated_ids = set(ac.referenced_ids(outstanding, "F"))
    blocking_open = {f["id"] for f in findings
                     if f["severity"].lower() in ("critical", "high")
                     and f["status"].lower() == "open"}
    if ac.is_none_marker(outstanding):
        agrees = not blocking_open
    else:
        agrees = stated_ids == blocking_open
    add("P7", TEMPLATE, "Correctable",
        "The outstanding blocking findings named in the verdict are exactly the open "
        "critical and high findings",
        "pass" if agrees else "fail",
        f"verdict names {sorted(stated_ids) or 'none'}; table carries "
        f"{sorted(blocking_open) or 'none'}")

    rep.counts["findings"] = len(findings)
    rep.counts["verdict"] = verdict
    rep.counts["correctionRequests"] = len(cr_rows) if cr_headers else 0
    rep.counts["severity"] = {
        level: sum(1 for f in findings if f["severity"].lower() == level)
        for level in ac.SEVERITIES}
    return rep


def validate(artifact_path, envelope: dict | None = None):
    rep = ac.run_contract(CONTRACT, Path(artifact_path), envelope)
    return semantic_checks(rep, CONTRACT)


def main():
    return ac.cli(CONTRACT, extra=semantic_checks)


if __name__ == "__main__":
    sys.exit(main())
