#!/usr/bin/env python3
"""Validation Engine: implementation-report.md conformance.

Mechanically checks a produced `implementation-report.md` against the contract set that
governs it:

  - `.claude/templates/implementation-report.md`     rendered form, section and field labels
  - `.claude/agents/dev-1-implement/output.md`   output contract
  - `.claude/agents/dev-1-implement/quality.md`  numbered self-verification set

The structural, field, vocabulary, and identifier rules are declared as data and executed by
`artifact_contract.py`. The checks below are the ones specific to an implementation record
and not expressible as structure: coverage of the change set by test evidence, agreement
between the metadata block and the Metadata section, the completion rule, deviation
attribution, the evidence vocabulary, the self-review prohibition, and the verification
claim.

Scope boundary: `M1` to `M7` are the rules in `quality.md` that are decidable by inspecting
the artifact. Whether the code behind a change-set row is correct, and whether a test
actually exercises what it claims, are not decidable here; both are recorded as
not-machine-checkable obligations rather than silently skipped.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import artifact_contract as ac  # noqa: E402
from artifact_lib import Check, strip_md  # noqa: E402

TEMPLATE = "templates/implementation-report.md"
OUTPUT_REF = "agents/dev-1-implement/output.md"
QUALITY_REF = "agents/dev-1-implement/quality.md"

PHASES = ("implementation", "fix-implementation", "refactor-implementation")
VERIFICATION = ("verified", "partially-verified", "unverified")
TEST_TYPES = ("unit", "integration", "contract", "regression", "end-to-end", "static")
TEST_RESULTS = ("pass", "fail", "not-run")

CHANGE_TABLE = ("ID", "Path", "Change Type", "Purpose", "Design Ref")
TEST_TABLE = ("ID", "Test", "Type", "Covers", "Command", "Result")
DEVIATION_TABLE = ("ID", "Deviation", "Design element", "Rationale", "Escalation")
RISK_TABLE = ("ID", "Risk", "Likelihood", "Impact", "Mitigation")

# Claims of merge or release readiness. The producing agent is assessed at a gate it does
# not own, so a report that awards itself that verdict has overstepped its authority scope
# whatever the Review status field says.
READINESS_CLAIMS = [
    r"ready to merge", r"merge[- ]ready", r"approved for merge", r"approved to merge",
    r"ready for release", r"release[- ]ready", r"approved for release",
    r"cleared for merge", r"no review (?:is )?(?:required|needed)",
]

CONTRACT = ac.ArtifactContract(
    artifact="implementation-report.md",
    producers=("dev-1-implement",),
    metadata_key="implementationReport",
    metadata_fields=("reportId", "changeReference", "sourceInputs", "producedBy",
                     "agentVersion", "schemaVersion", "status", "workflowPhase",
                     "verificationStatus", "inputDigest", "contextDigest"),
    statuses=ac.STATUSES_COMPLETE,
    template_ref=TEMPLATE,
    contract_refs=(TEMPLATE, OUTPUT_REF, QUALITY_REF),
    appendices=("Open Questions",),
    id_prefixes={"C": "Change Set", "T": "Test Evidence",
                 "V": "Deviations and Tradeoffs", "R": "Residual Risk",
                 "Q": "Open Questions"},
    sections=(
        ac.Section("Metadata", fields=(
            ac.Field("Report ID"),
            ac.Field("Change reference"),
            ac.Field("Workflow phase", enum=PHASES),
            ac.Field("Status", enum=ac.STATUSES_COMPLETE),
            ac.Field("Verification status", enum=VERIFICATION),
            ac.Field("Review status"),
        )),
        ac.Section("Implementation Summary", fields=(
            ac.Field("Change intent", min_words=6),
            ac.Field("Approach taken", min_words=6),
            ac.Field("Design reference", min_words=2),
            ac.Field("Out of scope", min_words=3),
        )),
        ac.Section("Change Set", table=ac.Table(CHANGE_TABLE, min_rows=1, id_column="ID"),
                   allow_none=False),
        ac.Section("Test Evidence", table=ac.Table(TEST_TABLE, min_rows=1, id_column="ID"),
                   allow_none=False),
        ac.Section("Verification Results", fields=(
            ac.Field("Verification method", min_words=4),
            ac.Field("Commands executed", min_words=2),
            ac.Field("Result summary", min_words=4),
            ac.Field("Unverified areas", min_words=1),
        )),
        ac.Section("Deviations and Tradeoffs",
                   table=ac.Table(DEVIATION_TABLE, min_rows=1, id_column="ID")),
        ac.Section("Boundary Compliance", fields=(
            ac.Field("Module boundaries preserved", min_words=3),
            ac.Field("Public interface changes", min_words=2),
            ac.Field("Data or migration impact", min_words=2),
            ac.Field("Declared side effects", min_words=2),
        )),
        ac.Section("Residual Risk",
                   table=ac.Table(RISK_TABLE, min_rows=1, id_column="ID")),
        ac.Section("Handoff Notes", fields=(
            ac.Field("Reviewer focus areas", min_words=3),
            ac.Field("Follow-up work", min_words=2),
            ac.Field("Documentation impact", min_words=2),
        )),
    ),
    obligations=(
        ("N1", OUTPUT_REF + "#change-set",
         "Each change-set row describes the change that was actually made at that path"),
        ("N2", QUALITY_REF + "#evidence-checks",
         "Each test-evidence row exercises the change-set entries its Covers cell names"),
        ("N3", QUALITY_REF + "#boundary-checks",
         "No hidden side effect was introduced into critical business logic"),
    ),
)


def _cells(headers, rows, name):
    """Every value of one column, stripped of markdown, in row order."""
    idx = ac.column_index(headers or [], name)
    if idx < 0:
        return []
    return [strip_md(r[idx]) if idx < len(r) else "" for r in rows]


def semantic_checks(rep, contract):
    """The implementation-specific rules in `agents/dev-1-implement/quality.md`."""
    meta = getattr(rep, "meta", {}) or {}
    body_of = getattr(rep, "body_of", {}) or {}
    text_all = "\n".join(body_of.values())

    def add(cid, ref, severity, description, result, detail=""):
        rep.checks.append(Check(cid, ref, severity, description, result, detail))

    def field(section, label):
        return strip_md(ac.parse_field_bullets(body_of.get(section, "")).get(
            label, "")).lower().rstrip(".")

    change_h, change_rows = ac.find_table(body_of.get("Change Set", ""), CHANGE_TABLE)
    test_h, test_rows = ac.find_table(body_of.get("Test Evidence", ""), TEST_TABLE)
    dev_body = body_of.get("Deviations and Tradeoffs", "")
    dev_h, dev_rows = (None, []) if ac.is_none_marker(dev_body) \
        else ac.find_table(dev_body, DEVIATION_TABLE)

    change_ids = ac.defined_ids(body_of.get("Change Set", ""), "C")
    status = str(meta.get("status") or "").strip().lower()
    verification = str(meta.get("verificationStatus") or "").strip().lower()

    # M1 -- "Every change carries test evidence." A change-set entry no test row covers is
    # an unverified change, whatever the summary claims.
    covered = set()
    for cell in _cells(test_h, test_rows, "covers"):
        covered.update(ac.referenced_ids(cell, "C"))
    uncovered = [c for c in change_ids if c not in covered]
    add("M1", QUALITY_REF + "#evidence-checks", "Blocking",
        "Every change-set entry is cited by at least one test-evidence row",
        "pass" if (change_ids and not uncovered) else "fail",
        f"change(s) with no covering test: {uncovered}" if uncovered
        else (f"{len(change_ids)} change(s) covered by {len(test_rows)} test row(s)"
              if change_ids else "no change-set entries to check"))

    # M2 -- the metadata block and the Metadata section are one fact, not two.
    drift = []
    for meta_key, label in (("workflowPhase", "workflow phase"),
                            ("status", "status"),
                            ("verificationStatus", "verification status")):
        a = str(meta.get(meta_key) or "").strip().lower()
        b = field("Metadata", label)
        if a != b:
            drift.append(f"{meta_key}: metadata={a!r} section={b!r}")
    add("M2", TEMPLATE, "Blocking",
        "Workflow phase, status, and verification status agree between the metadata block "
        "and the Metadata section",
        "pass" if not drift else "fail",
        f"disagreement: {drift}" if drift else "all three fields agree")

    # M3 -- "A report claiming completion carries no failing or unrun verification."
    results = [r.lower() for r in _cells(test_h, test_rows, "result")]
    failing = [r for r in results if r in ("fail", "not-run")]
    questions = ac.defined_ids(body_of.get("Open Questions", ""), "Q")
    complete = status == "complete"
    ok = not complete or (not failing and verification == "verified")
    add("M3", QUALITY_REF + "#completion-rule", "Blocking",
        "A report at status complete records no failing or unrun test and claims verified "
        "verification",
        "pass" if ok else "fail",
        f"status={status!r} verification={verification!r} with results {results}" if not ok
        else f"status={status!r}, {len(results)} result(s), "
             f"{len(failing)} failing or unrun, {len(questions)} open question(s)")

    # M4 -- "A deviation names the change that embodies it." A deviation that cites no
    # change-set identifier is a claim about the work rather than a record of it.
    unattributed = []
    if dev_h:
        id_col = ac.column_index(dev_h, "id")
        for r in dev_rows:
            if not ac.referenced_ids(" ".join(r), "C"):
                unattributed.append(strip_md(r[id_col]) if 0 <= id_col < len(r) else "?")
    add("M4", QUALITY_REF + "#deviation-checks", "Blocking",
        "Every recorded deviation cites the change-set entry that embodies it",
        "pass" if not unattributed else "fail",
        f"deviation(s) citing no change: {unattributed}" if unattributed
        else (f"{len(dev_rows)} deviation(s), each attributed" if dev_rows
              else "no deviation recorded"))

    # M5 -- the evidence vocabulary. A result column outside the declared set cannot be
    # counted, and an uncountable result is not evidence.
    bad_type = [t for t in _cells(test_h, test_rows, "type") if t.lower() not in TEST_TYPES]
    bad_result = [r for r in results if r not in TEST_RESULTS]
    add("M5", TEMPLATE, "Blocking",
        "Test evidence uses the declared type and result vocabularies",
        "pass" if not (bad_type or bad_result) else "fail",
        f"types outside {list(TEST_TYPES)}: {bad_type}; results outside "
        f"{list(TEST_RESULTS)}: {bad_result}" if (bad_type or bad_result)
        else f"{len(test_rows)} row(s) within both vocabularies")

    # M6 -- authority scope. This agent's output is assessed at a gate it does not own.
    review = field("Metadata", "review status")
    claims = sorted({m.group(0).lower() for p in READINESS_CLAIMS
                     for m in re.finditer(p, text_all, re.I)})
    ok = review == "pending-review" and not claims
    add("M6", QUALITY_REF + "#authority-checks", "Blocking",
        "The report records no review verdict and no merge or release readiness claim",
        "pass" if ok else "fail",
        f"review status={review!r}; readiness claim(s)={claims}" if not ok
        else "review status is pending-review and no readiness claim appears")

    # M7 -- the verification claim must match what the report says it did not reach.
    gap = field("Verification Results", "unverified areas")
    named_gap = bool(gap) and gap not in ac.NONE_MARKERS
    if verification == "verified":
        ok, why = not named_gap, f"declared verified, yet unverified areas are named: {gap!r}"
    elif verification in ("partially-verified", "unverified"):
        ok, why = named_gap, f"declared {verification}, yet no unverified area is named"
    else:
        ok, why = False, f"verification status {verification!r} is outside the vocabulary"
    add("M7", QUALITY_REF + "#evidence-checks", "Blocking",
        "The declared verification status agrees with the unverified areas the report names",
        "pass" if ok else "fail",
        why if not ok else f"verification={verification!r}, unverified areas "
                           f"{'named' if named_gap else 'none'}")
    return rep


def validate(artifact_path, envelope: dict | None = None):
    rep = ac.run_contract(CONTRACT, Path(artifact_path), envelope)
    return semantic_checks(rep, CONTRACT)


def main():
    return ac.cli(CONTRACT, extra=semantic_checks)


if __name__ == "__main__":
    sys.exit(main())
