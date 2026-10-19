#!/usr/bin/env python3
"""Validation Engine: scope-definition.md conformance.

Mechanically checks a produced `scope-definition.md` against the contract set that governs
it:

  - `.claude/templates/scope-definition.md`             rendered form, section and field labels
  - `.claude/agents/product-owner/output.md`        structural and semantic output contract
  - `.claude/agents/product-owner/quality.md`       the checks the agent runs on itself

Scope boundary: `SD1` to `SD8` are the rules in that contract decidable by inspecting the
artifact. Whether the recorded scope is the *right* scope for the business is a judgement
the Scope Gate makes, not a property of the file. What is decidable is that the boundary is
stated rather than implied: every acceptance criterion names the in-scope item it bounds and
the method that verifies it, every scope decision carries its rationale, the criterion count
is one fact rather than two, and the artifact stays inside product authority by carrying no
task, design, or change identifier.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import artifact_contract as ac  # noqa: E402
from artifact_lib import Check, strip_md  # noqa: E402

TEMPLATE = "templates/scope-definition.md"
OUTPUT_REF = "agents/product-owner/output.md"
QUALITY_REF = "agents/product-owner/quality.md"

VERDICTS = ("bounded", "partially-bounded", "blocked")

SCOPE_TABLE = ("ID", "Scope Item", "Rationale", "Priority")
EXCLUSION_TABLE = ("ID", "Excluded Item", "Reason", "Revisit Trigger")
ACCEPTANCE_TABLE = ("ID", "Criterion", "Scope Ref", "Verification Method", "Priority")
DECISION_TABLE = ("ID", "Decision", "Rationale", "Impact", "Decided By")
QUESTION_TABLE = ("ID", "Question", "Blocking", "Owner", "Needed By")

# Identifier schemes owned by phases downstream of this one. A scope definition that carries
# one has crossed out of product authority into planning, design, or implementation.
DOWNSTREAM_ID_PREFIXES = {
    "T": "planner task breakdown",
    "C": "implementation change set",
    "ADR": "architecture decision record",
}

CONTRACT = ac.ArtifactContract(
    artifact="scope-definition.md",
    producers=("product-owner",),
    metadata_key="scopeDefinition",
    metadata_fields=("scopeId", "featureName", "sourceInputs", "producedBy", "agentVersion",
                     "schemaVersion", "status", "scopeVerdict", "acceptanceCriteriaCount",
                     "inputDigest", "contextDigest"),
    statuses=ac.STATUSES_COMPLETE,
    template_ref=TEMPLATE,
    contract_refs=(TEMPLATE, OUTPUT_REF, QUALITY_REF),
    id_prefixes={
        "S": "In Scope",
        "X": "Out of Scope",
        "A": "Acceptance Criteria",
        "D": "Scope Decisions",
        "Q": "Open Questions",
    },
    sections=(
        ac.Section("Metadata", fields=(
            ac.Field("Feature name"),
            ac.Field("Requested by"),
            ac.Field("Business goal", min_words=4),
            ac.Field("Target outcome", min_words=3),
            ac.Field("Scope decision date"),
        )),
        ac.Section("Business Context", fields=(
            ac.Field("Problem statement", min_words=6),
            ac.Field("Value hypothesis", min_words=4),
            ac.Field("Affected users"),
            ac.Field("Success measure", min_words=4),
        )),
        ac.Section("In Scope", table=ac.Table(SCOPE_TABLE, min_rows=1, id_column="ID"),
                   allow_none=False),
        ac.Section("Out of Scope",
                   table=ac.Table(EXCLUSION_TABLE, min_rows=1, id_column="ID")),
        ac.Section("Acceptance Criteria",
                   table=ac.Table(ACCEPTANCE_TABLE, min_rows=1, id_column="ID"),
                   allow_none=False),
        ac.Section("Constraints and Dependencies", fields=(
            ac.Field("Business constraints"),
            ac.Field("Regulatory or policy constraints"),
            ac.Field("Delivery constraints"),
            ac.Field("External dependencies"),
        )),
        ac.Section("Scope Decisions",
                   table=ac.Table(DECISION_TABLE, min_rows=1, id_column="ID")),
        ac.Section("Open Questions",
                   table=ac.Table(QUESTION_TABLE, min_rows=1, id_column="ID")),
        ac.Section("Handoff", fields=(
            ac.Field("Downstream owner"),
            ac.Field("Gate"),
            ac.Field("Evidence for the gate", min_words=4),
            ac.Field("Deferred to downstream", min_words=3),
        )),
    ),
    obligations=(
        ("SN1", OUTPUT_REF + "#acceptance-criteria",
         "Each acceptance criterion states a threshold the business would actually accept"),
        ("SN2", QUALITY_REF + "#scope-integrity",
         "The recorded scope matches what the requester asked for, with no silent widening"),
        ("SN3", QUALITY_REF + "#value-alignment",
         "The stated business goal is the goal the requester holds, not one inferred for them"),
    ),
)


def semantic_checks(rep, contract):
    """The scope-specific rules in `agents/product-owner/`."""
    meta = getattr(rep, "meta", {}) or {}
    body_of = getattr(rep, "body_of", {}) or {}

    def add(cid, ref, severity, description, result, detail=""):
        rep.checks.append(Check(cid, ref, severity, description, result, detail))

    def rows_of(section, columns):
        headers, rows = ac.find_table(body_of.get(section, ""), columns)
        return (headers or []), rows

    verdict = str(meta.get("scopeVerdict") or "").strip().lower()
    add("SD1", TEMPLATE, "Blocking",
        f"scopeVerdict is one of {', '.join(VERDICTS)}",
        "pass" if verdict in VERDICTS else "fail", f"scopeVerdict={verdict!r}")

    # SD2 -- the criterion count is one fact. A downstream phase that plans against the
    # metadata and a gate that reads the table must be counting the same criteria.
    acc_headers, acc_rows = rows_of("Acceptance Criteria", ACCEPTANCE_TABLE)
    declared = meta.get("acceptanceCriteriaCount")
    try:
        declared_n = int(str(declared).strip())
    except (TypeError, ValueError):
        declared_n = None
    add("SD2", TEMPLATE, "Blocking",
        "acceptanceCriteriaCount equals the number of acceptance criteria recorded",
        "pass" if declared_n is not None and declared_n == len(acc_rows) else "fail",
        f"metadata={declared!r} table={len(acc_rows)} row(s)")

    # SD3 -- an unverifiable criterion is an open question wearing a criterion's clothes.
    verify_i = ac.column_index(acc_headers, "verification method")
    unverifiable = []
    for row in acc_rows:
        rid = strip_md(row[0]) if row else "?"
        cell = row[verify_i].strip() if 0 <= verify_i < len(row) else ""
        if not cell or ac.is_none_marker(cell):
            unverifiable.append(rid)
    add("SD3", OUTPUT_REF + "#acceptance-criteria", "Blocking",
        "Every acceptance criterion names the method that verifies it",
        "pass" if not unverifiable else "fail",
        f"no verification method: {unverifiable}" if unverifiable
        else f"{len(acc_rows)} criterion(s), each with a verification method")

    # SD4 -- traceability. A criterion that bounds nothing in scope is either scope this
    # artifact failed to declare or a criterion that belongs to another change.
    scope_i = ac.column_index(acc_headers, "scope ref")
    untraced = []
    for row in acc_rows:
        rid = strip_md(row[0]) if row else "?"
        cell = strip_md(row[scope_i]) if 0 <= scope_i < len(row) else ""
        if not re.search(r"\bS-\d{3}\b", cell):
            untraced.append(rid)
    add("SD4", OUTPUT_REF + "#traceability", "Blocking",
        "Every acceptance criterion names the in-scope item it bounds",
        "pass" if not untraced else "fail",
        f"no in-scope reference: {untraced}" if untraced
        else f"{len(acc_rows)} criterion(s) trace to a declared scope item")

    # SD5 -- a scope that is not fully bounded says what is still open, or it is not
    # partially bounded, it is unrecorded.
    _qh, q_rows = rows_of("Open Questions", QUESTION_TABLE)
    needs_question = verdict in ("partially-bounded", "blocked")
    add("SD5", QUALITY_REF + "#scope-integrity", "Blocking",
        "A partially-bounded or blocked scope records at least one open question",
        "pass" if not needs_question or q_rows else "fail",
        f"scopeVerdict={verdict!r} with no open question recorded"
        if needs_question and not q_rows
        else f"scopeVerdict={verdict!r}, {len(q_rows)} open question(s)")

    # SD6 -- a decision without a rationale cannot be reviewed, only accepted on trust.
    dec_headers, dec_rows = rows_of("Scope Decisions", DECISION_TABLE)
    rationale_i = ac.column_index(dec_headers, "rationale")
    unjustified = []
    for row in dec_rows:
        rid = strip_md(row[0]) if row else "?"
        cell = row[rationale_i].strip() if 0 <= rationale_i < len(row) else ""
        if not cell or ac.is_none_marker(cell) or len(cell.split()) < 3:
            unjustified.append(rid)
    add("SD6", OUTPUT_REF + "#scope-decisions", "Blocking",
        "Every scope decision carries a rationale a reviewer can assess",
        "pass" if not unjustified else "fail",
        f"rationale absent or too thin: {unjustified}" if unjustified
        else f"{len(dec_rows)} decision(s), each with a rationale")

    # SD7 -- authority boundary. Breakdown, design, and change identifiers belong to the
    # phases that own them; a scope definition that issues them has decided their work.
    text = Path(rep.artifact).read_text(encoding="utf-8")
    crossed = {}
    for prefix, owner in DOWNSTREAM_ID_PREFIXES.items():
        found = sorted(set(re.findall(r"\b" + prefix + r"-\d{3}\b", text)))
        if found:
            crossed[owner] = found
    add("SD7", QUALITY_REF + "#authority-boundary", "Blocking",
        "The artifact issues no task, change, or design identifier owned downstream",
        "pass" if not crossed else "fail",
        f"downstream identifiers issued: {crossed}" if crossed
        else "no downstream identifier scheme used")

    # SD8 -- the boundary is the deliverable. A bounded scope with nothing excluded has
    # described a wish rather than drawn a line.
    _xh, x_rows = rows_of("Out of Scope", EXCLUSION_TABLE)
    add("SD8", OUTPUT_REF + "#out-of-scope", "Correctable",
        "A bounded scope names at least one explicit exclusion",
        "pass" if verdict != "bounded" or x_rows else "fail",
        f"scopeVerdict={verdict!r} with an empty Out of Scope section"
        if verdict == "bounded" and not x_rows
        else f"{len(x_rows)} exclusion(s) recorded")

    _sh, s_rows = rows_of("In Scope", SCOPE_TABLE)
    q_headers, _ = rows_of("Open Questions", QUESTION_TABLE)
    blocking_i = ac.column_index(q_headers, "blocking")
    blocking_questions = sum(
        1 for row in q_rows
        if 0 <= blocking_i < len(row) and strip_md(row[blocking_i]).lower().startswith("yes"))

    rep.counts["scopeVerdict"] = verdict
    rep.counts["scopeItems"] = len(s_rows)
    rep.counts["exclusions"] = len(x_rows)
    rep.counts["acceptanceCriteria"] = len(acc_rows)
    rep.counts["scopeDecisions"] = len(dec_rows)
    rep.counts["openQuestions"] = len(q_rows)
    rep.counts["blockingOpenQuestions"] = blocking_questions
    return rep


def validate(artifact_path, envelope: dict | None = None):
    rep = ac.run_contract(CONTRACT, Path(artifact_path), envelope)
    rep.artifact = str(artifact_path)
    return semantic_checks(rep, CONTRACT)


def main():
    return ac.cli(CONTRACT, extra=semantic_checks)


if __name__ == "__main__":
    sys.exit(main())
