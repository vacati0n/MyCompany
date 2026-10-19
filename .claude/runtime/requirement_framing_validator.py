#!/usr/bin/env python3
"""Validation Engine: requirement-framing.md conformance.

Mechanically checks a produced `requirement-framing.md` against the contract set that governs
it:

  - `.claude/templates/requirement-framing.md`            rendered form, section and field labels
  - `.claude/agents/business-analyst/output.md`       structural and semantic output contract
  - `.claude/agents/business-analyst/quality.md`      the checks the agent runs on itself

Scope boundary: `RF1` to `RF11` are the rules in that contract decidable by inspecting the
artifact. Whether the recorded requirements are the *right* requirements for the business is a
judgement the Framing Gate makes, not a property of the file. What is decidable is that the
framing is stated rather than implied: every requirement traces to a declared outcome and
carries a type, every requirement is bounded by acceptance intent that could demonstrate it,
every assumption states the basis it rests on, an incomplete framing names what is missing,
and the artifact stays inside analysis authority by carrying no task, change, or design
identifier.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import artifact_contract as ac  # noqa: E402
from artifact_lib import Check, strip_md  # noqa: E402

TEMPLATE = "templates/requirement-framing.md"
OUTPUT_REF = "agents/business-analyst/output.md"
QUALITY_REF = "agents/business-analyst/quality.md"

VERDICTS = ("framed", "partially-framed", "blocked")
REQUIREMENT_TYPES = ("functional", "non-functional")

OUTCOME_TABLE = ("ID", "Outcome", "Measure", "Business Driver")
REQUIREMENT_TABLE = ("ID", "Requirement", "Type", "Outcome Ref", "Priority")
ACCEPTANCE_TABLE = ("ID", "Acceptance Intent", "Requirement Ref", "Demonstrated By", "Priority")
BOUNDARY_TABLE = ("ID", "Excluded Concern", "Reason", "Revisit Trigger")
ASSUMPTION_TABLE = ("ID", "Assumption", "Basis", "Confidence", "Impact If False")
QUESTION_TABLE = ("ID", "Question", "Blocking", "Owner", "Needed By")

# Identifier schemes owned by phases downstream of this one. A requirement framing that
# carries one has crossed out of analysis authority into planning, architecture, or
# implementation -- the three boundaries the role specification draws explicitly.
DOWNSTREAM_ID_PREFIXES = {
    "T": "planner task breakdown",
    "C": "implementation change set",
    "ADR": "architecture decision record",
}

CONTRACT = ac.ArtifactContract(
    artifact="requirement-framing.md",
    producers=("business-analyst",),
    metadata_key="requirementFraming",
    metadata_fields=("framingId", "subject", "sourceInputs", "producedBy", "agentVersion",
                     "schemaVersion", "status", "framingVerdict", "requirementCount",
                     "inputDigest", "contextDigest"),
    statuses=ac.STATUSES_COMPLETE,
    template_ref=TEMPLATE,
    contract_refs=(TEMPLATE, OUTPUT_REF, QUALITY_REF),
    id_prefixes={
        "O": "Target Outcomes",
        "R": "Requirements",
        "AI": "Acceptance Intent",
        "B": "Framing Boundaries",
        "AS": "Assumptions",
        "Q": "Open Questions",
    },
    sections=(
        ac.Section("Metadata", fields=(
            ac.Field("Subject"),
            ac.Field("Requested by"),
            ac.Field("Decision owner"),
            ac.Field("Workflow phase",
                     enum=("problem-framing", "research-framing")),
            ac.Field("Framing date"),
        )),
        ac.Section("Business Context", fields=(
            ac.Field("Business intent", min_words=4),
            ac.Field("Problem statement", min_words=6),
            ac.Field("Affected stakeholders"),
            ac.Field("Current-state pain", min_words=4),
        )),
        ac.Section("Target Outcomes",
                   table=ac.Table(OUTCOME_TABLE, min_rows=1, id_column="ID"),
                   allow_none=False),
        ac.Section("Requirements",
                   table=ac.Table(REQUIREMENT_TABLE, min_rows=1, id_column="ID"),
                   allow_none=False),
        ac.Section("Acceptance Intent",
                   table=ac.Table(ACCEPTANCE_TABLE, min_rows=1, id_column="ID"),
                   allow_none=False),
        ac.Section("Framing Boundaries",
                   table=ac.Table(BOUNDARY_TABLE, min_rows=1, id_column="ID")),
        ac.Section("Assumptions",
                   table=ac.Table(ASSUMPTION_TABLE, min_rows=1, id_column="ID")),
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
        ("RN1", OUTPUT_REF + "#requirements",
         "Each requirement is one the supplied inputs support, not one inferred for the business"),
        ("RN2", OUTPUT_REF + "#requirements",
         "The requirement set covers the known edge cases of the intent, with none silently dropped"),
        ("RN3", OUTPUT_REF + "#target-outcomes",
         "The declared outcomes are the outcomes the requester holds, not ones substituted for them"),
        ("RN4", OUTPUT_REF + "#acceptance-intent",
         "Each acceptance intent describes something acceptance could genuinely demonstrate"),
    ),
)


def semantic_checks(rep, contract):
    """The framing-specific rules in `agents/business-analyst/`."""
    meta = getattr(rep, "meta", {}) or {}
    body_of = getattr(rep, "body_of", {}) or {}

    def add(cid, ref, severity, description, result, detail=""):
        rep.checks.append(Check(cid, ref, severity, description, result, detail))

    def rows_of(section, columns):
        headers, rows = ac.find_table(body_of.get(section, ""), columns)
        return (headers or []), rows

    verdict = str(meta.get("framingVerdict") or "").strip().lower()
    add("RF1", TEMPLATE, "Blocking",
        f"framingVerdict is one of {', '.join(VERDICTS)}",
        "pass" if verdict in VERDICTS else "fail", f"framingVerdict={verdict!r}")

    # RF2 -- the requirement count is one fact. A downstream phase that scopes against the
    # metadata and a gate that reads the table must be counting the same requirements.
    req_headers, req_rows = rows_of("Requirements", REQUIREMENT_TABLE)
    declared = meta.get("requirementCount")
    try:
        declared_n = int(str(declared).strip())
    except (TypeError, ValueError):
        declared_n = None
    add("RF2", TEMPLATE, "Blocking",
        "requirementCount equals the number of requirements recorded",
        "pass" if declared_n is not None and declared_n == len(req_rows) else "fail",
        f"metadata={declared!r} table={len(req_rows)} row(s)")

    # RF3 -- outcome alignment. A requirement that serves no declared outcome is either an
    # outcome the framing failed to declare or a requirement belonging to another change.
    outcome_i = ac.column_index(req_headers, "outcome ref")
    unaligned = []
    for row in req_rows:
        rid = strip_md(row[0]) if row else "?"
        cell = strip_md(row[outcome_i]) if 0 <= outcome_i < len(row) else ""
        if not re.search(r"\bO-\d{3}\b", cell):
            unaligned.append(rid)
    add("RF3", OUTPUT_REF + "#traceability", "Blocking",
        "Every requirement names the target outcome it serves",
        "pass" if not unaligned else "fail",
        f"no outcome reference: {unaligned}" if unaligned
        else f"{len(req_rows)} requirement(s) trace to a declared outcome")

    # RF4 -- a requirement with no type has not been analysed, only transcribed. The
    # functional / non-functional split is what makes the set checkable for completeness.
    type_i = ac.column_index(req_headers, "type")
    untyped = []
    for row in req_rows:
        rid = strip_md(row[0]) if row else "?"
        cell = strip_md(row[type_i]).lower() if 0 <= type_i < len(row) else ""
        if not any(re.fullmatch(rf"{t}", cell) for t in REQUIREMENT_TYPES):
            untyped.append(rid)
    add("RF4", OUTPUT_REF + "#requirements", "Blocking",
        f"Every requirement declares its type as {' or '.join(REQUIREMENT_TYPES)}",
        "pass" if not untyped else "fail",
        f"type absent or not a declared value: {untyped}" if untyped
        else f"{len(req_rows)} requirement(s), each typed")

    # RF5 -- an acceptance intent that bounds nothing recorded is intent for another change.
    acc_headers, acc_rows = rows_of("Acceptance Intent", ACCEPTANCE_TABLE)
    req_ref_i = ac.column_index(acc_headers, "requirement ref")
    unbound, bounded_ids = [], set()
    for row in acc_rows:
        aid = strip_md(row[0]) if row else "?"
        cell = strip_md(row[req_ref_i]) if 0 <= req_ref_i < len(row) else ""
        found = re.findall(r"\bR-\d{3}\b", cell)
        if not found:
            unbound.append(aid)
        bounded_ids.update(found)
    add("RF5", OUTPUT_REF + "#acceptance-intent", "Blocking",
        "Every acceptance intent names the requirement it bounds",
        "pass" if not unbound else "fail",
        f"no requirement reference: {unbound}" if unbound
        else f"{len(acc_rows)} acceptance intent(s) trace to a declared requirement")

    # RF6 -- the testability floor. A requirement nothing would demonstrate cannot be
    # accepted, validated, or disputed; it is an open question wearing a requirement's
    # clothes.
    declared_reqs = [strip_md(row[0]) for row in req_rows if row]
    undemonstrated = [rid for rid in declared_reqs
                      if re.fullmatch(r"R-\d{3}", rid) and rid not in bounded_ids]
    add("RF6", QUALITY_REF + "#testability", "Blocking",
        "Every requirement is bounded by at least one acceptance intent",
        "pass" if not undemonstrated else "fail",
        f"no acceptance intent: {undemonstrated}" if undemonstrated
        else f"{len(declared_reqs)} requirement(s), each demonstrated by acceptance intent")

    # RF7 -- a framing that reports itself incomplete while recording nothing missing has
    # hidden the gap rather than named it, which is the failure this role exists to prevent.
    _qh, q_rows = rows_of("Open Questions", QUESTION_TABLE)
    needs_question = verdict in ("partially-framed", "blocked")
    add("RF7", QUALITY_REF + "#framing-integrity", "Blocking",
        "A partially-framed or blocked framing records at least one open question",
        "pass" if not needs_question or q_rows else "fail",
        f"framingVerdict={verdict!r} with no open question recorded"
        if needs_question and not q_rows
        else f"framingVerdict={verdict!r}, {len(q_rows)} open question(s)")

    # RF8 -- a framing whose boundary excludes nothing has described a wish rather than
    # drawn a line.
    _bh, b_rows = rows_of("Framing Boundaries", BOUNDARY_TABLE)
    add("RF8", QUALITY_REF + "#framing-integrity", "Correctable",
        "A completed framing names at least one framing boundary",
        "pass" if verdict != "framed" or b_rows else "fail",
        f"framingVerdict={verdict!r} with an empty Framing Boundaries section"
        if verdict == "framed" and not b_rows
        else f"{len(b_rows)} boundary(s) recorded")

    # RF9 -- an assumption with no basis is an invented requirement in disguise, which is
    # the one output this role must never produce.
    as_headers, as_rows = rows_of("Assumptions", ASSUMPTION_TABLE)
    basis_i = ac.column_index(as_headers, "basis")
    baseless = []
    for row in as_rows:
        aid = strip_md(row[0]) if row else "?"
        cell = row[basis_i].strip() if 0 <= basis_i < len(row) else ""
        if not cell or ac.is_none_marker(cell) or len(cell.split()) < 3:
            baseless.append(aid)
    add("RF9", QUALITY_REF + "#assumption-checks", "Blocking",
        "Every assumption states the basis it rests on",
        "pass" if not baseless else "fail",
        f"basis absent or too thin: {baseless}" if baseless
        else f"{len(as_rows)} assumption(s), each with a basis")

    # RF10 -- an assumption a reader cannot weigh is carried blind.
    conf_i = ac.column_index(as_headers, "confidence")
    unrated = []
    for row in as_rows:
        aid = strip_md(row[0]) if row else "?"
        cell = strip_md(row[conf_i]).lower() if 0 <= conf_i < len(row) else ""
        if cell not in ac.CONFIDENCE:
            unrated.append(aid)
    add("RF10", QUALITY_REF + "#assumption-checks", "Correctable",
        f"Every assumption declares a confidence of {', '.join(ac.CONFIDENCE)}",
        "pass" if not unrated else "fail",
        f"confidence absent or not a declared value: {unrated}" if unrated
        else f"{len(as_rows)} assumption(s), each rated")

    # RF11 -- authority boundary. Task breakdown, change sets, and decision records belong
    # to the phases that own them; a framing that issues one has decided their work.
    text = Path(rep.artifact).read_text(encoding="utf-8")
    crossed = {}
    for prefix, owner in DOWNSTREAM_ID_PREFIXES.items():
        found = sorted(set(re.findall(r"\b" + prefix + r"-\d{3}\b", text)))
        if found:
            crossed[owner] = found
    add("RF11", QUALITY_REF + "#authority-boundary", "Blocking",
        "The artifact issues no task, change, or design identifier owned downstream",
        "pass" if not crossed else "fail",
        f"downstream identifiers issued: {crossed}" if crossed
        else "no downstream identifier scheme used")

    _oh, o_rows = rows_of("Target Outcomes", OUTCOME_TABLE)
    blocking_i = ac.column_index(_qh, "blocking")
    blocking_questions = sum(
        1 for row in q_rows
        if 0 <= blocking_i < len(row) and strip_md(row[blocking_i]).lower().startswith("yes"))

    functional = sum(
        1 for row in req_rows
        if 0 <= type_i < len(row) and strip_md(row[type_i]).lower() == "functional")

    rep.counts["framingVerdict"] = verdict
    rep.counts["targetOutcomes"] = len(o_rows)
    rep.counts["requirements"] = len(req_rows)
    rep.counts["functionalRequirements"] = functional
    rep.counts["nonFunctionalRequirements"] = len(req_rows) - functional
    rep.counts["acceptanceIntent"] = len(acc_rows)
    rep.counts["framingBoundaries"] = len(b_rows)
    rep.counts["assumptions"] = len(as_rows)
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
