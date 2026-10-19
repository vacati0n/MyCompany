#!/usr/bin/env python3
"""Validation Engine: investigation-report.md conformance.

Mechanically checks a produced `investigation-report.md` against the contract set that
governs it:

  - `.claude/templates/investigation-report.md`   rendered form, section and field labels
  - `.claude/agents/context-agent/`           behavioural contract: the module set's
                                                  scope, decision rules, output contract,
                                                  and self-verification checks

One artifact type serves two phases -- `investigate/technical-discovery` and
`research/technical-validation` -- because both ask the same agent the same question. The
validator does not branch on which phase produced the artifact: a report that satisfies this
contract satisfies both phases, and a report that does not satisfies neither.

Scope boundary: `I1` to `I5` are the rules in that contract set which are decidable by
inspecting the artifact. Whether an observation is *true* is not decidable here; what is
decidable is whether it is sourced, confidence-marked, dated for staleness, and actually used
by the options it is said to support.
"""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import artifact_contract as ac  # noqa: E402
from artifact_lib import Check, strip_md  # noqa: E402

TEMPLATE = "templates/investigation-report.md"
# The Context Agent's authoritative contract is a module set, not a single file. Each check below
# names the module that actually states the rule it enforces, so a failure points at the document
# that decides it rather than at the agent in general.
CONTRACT_REF = "agents/context-agent/identity.md"
OUTPUT_REF = "agents/context-agent/output.md"
QUALITY_REF = "agents/context-agent/quality.md"

EVIDENCE_TABLE = ("ID", "Source", "Observation", "Confidence", "Staleness")
OPTION_TABLE = ("ID", "Option", "Benefits", "Risks", "Effort", "Evidence")

CONTRACT = ac.ArtifactContract(
    artifact="investigation-report.md",
    producers=("context-agent",),
    metadata_key="investigation",
    metadata_fields=("investigationId", "decisionReference", "sourceInputs", "producedBy",
                     "agentVersion", "schemaVersion", "status", "confidence",
                     "inputDigest", "contextDigest"),
    statuses=ac.STATUSES_COMPLETE,
    template_ref=TEMPLATE,
    contract_refs=(TEMPLATE, CONTRACT_REF),
    appendices=("Contradictions and Gaps", "Open Questions"),
    id_prefixes={"E": "Evidence", "O": "Options Evaluated", "Q": "Open Questions"},
    sections=(
        ac.Section("Metadata", fields=(
            ac.Field("Investigation ID"),
            ac.Field("Owner"),
            ac.Field("Requested by"),
            ac.Field("Decision deadline"),
        )),
        ac.Section("Objective", fields=(
            ac.Field("Decision to support", min_words=4),
            ac.Field("Key question", min_words=4),
            ac.Field("Scope boundaries", min_words=3),
        )),
        ac.Section("Context", fields=(
            ac.Field("Relevant systems and components"),
            ac.Field("Related incidents or prior findings"),
            ac.Field("Constraints and assumptions"),
        )),
        # Two rows, because a decision supported by a single observation is a claim.
        ac.Section("Evidence", table=ac.Table(EVIDENCE_TABLE, min_rows=2), allow_none=False),
        # Two options, because a single option is a proposal, not an evaluation.
        ac.Section("Options Evaluated", table=ac.Table(OPTION_TABLE, min_rows=2),
                   allow_none=False),
        ac.Section("Recommendation", fields=(
            ac.Field("Recommended option"),
            ac.Field("Rationale", min_words=5),
            ac.Field("Preconditions"),
            ac.Field("Risks requiring monitoring"),
        )),
        ac.Section("Next Steps", fields=(
            ac.Field("Immediate actions", min_words=3),
            ac.Field("Decision owner and deadline"),
            ac.Field("Follow-up validation"),
        )),
    ),
    obligations=(
        ("N1", QUALITY_REF + "#not-machine-checkable-obligations",
         "The reconstructed current state matches the source it was read from"),
        ("N2", QUALITY_REF + "#not-machine-checkable-obligations",
         "Every stale assumption the sources contain is named as stale"),
        ("N3", QUALITY_REF + "#not-machine-checkable-obligations",
         "No inference is presented as an observation"),
    ),
)


def semantic_checks(rep, contract):
    """The investigation-specific rules the `agents/context-agent/` module set states."""
    meta = getattr(rep, "meta", {}) or {}
    body_of = getattr(rep, "body_of", {}) or {}

    def add(cid, ref, severity, description, result, detail=""):
        rep.checks.append(Check(cid, ref, severity, description, result, detail))

    e_headers, e_rows = ac.find_table(body_of.get("Evidence", ""), EVIDENCE_TABLE)
    o_headers, o_rows = ac.find_table(body_of.get("Options Evaluated", ""), OPTION_TABLE)
    defined_options = ac.defined_ids(body_of.get("Options Evaluated", ""), "O")

    # I1 -- the recommendation resolves to an evaluated option.
    rec = ac.parse_field_bullets(body_of.get("Recommendation", "")).get(
        "recommended option", "")
    named = ac.referenced_ids(rec, "O")
    resolves = bool(named) and all(o in defined_options for o in named)
    add("I1", TEMPLATE, "Blocking",
        "The recommended option names an option evaluated in this report",
        "pass" if resolves else "fail",
        f"recommendation names {named or 'no option identifier'}; "
        f"evaluated: {sorted(set(defined_options))}")

    # I2 -- "consolidates evidence": an option with no evidence is an opinion.
    unsupported = []
    if o_headers:
        ev_col = ac.column_index(o_headers, "evidence")
        id_col = ac.column_index(o_headers, "id")
        for r in o_rows:
            cell = r[ev_col] if 0 <= ev_col < len(r) else ""
            if not ac.referenced_ids(cell, "E"):
                unsupported.append(strip_md(r[id_col]) if 0 <= id_col < len(r) else "?")
    add("I2", OUTPUT_REF + "#5-options-evaluated", "Blocking",
        "Every evaluated option cites the evidence it rests on",
        "pass" if (o_headers and not unsupported) else "fail",
        f"options without an evidence citation: {unsupported}" if unsupported
        else (f"{len(o_rows)} option(s) evidence-backed" if o_headers
              else "no options table to check"))

    # I3 -- "marks confidence": the confidence column uses the declared vocabulary.
    bad_conf = []
    if e_headers:
        c_col = ac.column_index(e_headers, "confidence")
        id_col = ac.column_index(e_headers, "id")
        for r in e_rows:
            val = strip_md(r[c_col]).lower().rstrip(".") if 0 <= c_col < len(r) else ""
            if val not in ac.CONFIDENCE:
                bad_conf.append(
                    f"{strip_md(r[id_col]) if 0 <= id_col < len(r) else '?'}={val!r}")
    add("I3", OUTPUT_REF + "#4-evidence", "Blocking",
        "Every observation is confidence-marked with high, medium, or low",
        "pass" if (e_headers and not bad_conf) else "fail",
        f"outside vocabulary: {bad_conf}" if bad_conf
        else (f"{len(e_rows)} observation(s) marked" if e_headers
              else "no evidence table to check"))

    # I4 -- "names stale assumptions": staleness is stated, never left implicit.
    unstated = []
    if e_headers:
        s_col = ac.column_index(e_headers, "staleness")
        id_col = ac.column_index(e_headers, "id")
        for r in e_rows:
            val = strip_md(r[s_col]).strip() if 0 <= s_col < len(r) else ""
            if not val:
                unstated.append(strip_md(r[id_col]) if 0 <= id_col < len(r) else "?")
    add("I4", OUTPUT_REF + "#4-evidence", "Blocking",
        "Every observation states its staleness, current or otherwise",
        "pass" if (e_headers and not unstated) else "fail",
        f"staleness unstated: {unstated}" if unstated
        else (f"{len(e_rows)} observation(s) dated" if e_headers
              else "no evidence table to check"))

    # I5 -- "names contradictions": the appendix is required, and `None identified.` is an
    # answer. A missing appendix is not.
    contradictions = body_of.get("Contradictions and Gaps")
    add("I5", QUALITY_REF + "#reconciliation-checks", "Blocking",
        "Contradictions and gaps are recorded, explicitly as none where there are none",
        "pass" if contradictions is not None and contradictions.strip() else "fail",
        "section absent" if contradictions is None
        else ("section empty" if not contradictions.strip() else "recorded"))

    conf = str(meta.get("confidence") or "").strip().lower()
    add("I6", TEMPLATE, "Correctable",
        "The report-level confidence uses the declared vocabulary",
        "pass" if conf in ac.CONFIDENCE else "fail", f"confidence={conf!r}")
    return rep


def validate(artifact_path, envelope: dict | None = None):
    rep = ac.run_contract(CONTRACT, Path(artifact_path), envelope)
    return semantic_checks(rep, CONTRACT)


def main():
    return ac.cli(CONTRACT, extra=semantic_checks)


if __name__ == "__main__":
    sys.exit(main())
