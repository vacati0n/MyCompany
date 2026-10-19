#!/usr/bin/env python3
"""Validation Engine: bug-analysis.md conformance.

Mechanically checks a produced `bug-analysis.md` against the contract set that governs it:

  - `.claude/templates/bug-analysis.md`               rendered form, section and field labels
  - `.claude/agents/dev-1-bug-analyst/output.md`  structural and semantic output contract
  - `.claude/agents/dev-1-bug-analyst/identity.md` decision rules, constraints, authority
  - `.claude/agents/dev-1-bug-analyst/quality.md`  the numbered check set this reproduces

The structural, field, vocabulary, and identifier rules are declared as data and executed by
`artifact_contract.py` as `C1` to `C7`; the checks below are the ones specific to a defect
analysis and not expressible as structure. `B1` to `B5` here are the same rules the owning
agent's `quality.md` states under those identifiers, re-decided independently of the agent
that produced the artifact.

Scope boundary: `B1` to `B5` are the agent's semantic rules that are decidable by inspecting
the artifact. Whether a stated root cause is *the* root cause is not decidable here and is
recorded as a not-machine-checkable obligation rather than silently skipped.
"""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import artifact_contract as ac  # noqa: E402
from artifact_lib import Check, strip_md  # noqa: E402

TEMPLATE = "templates/bug-analysis.md"
CONTRACT_REF = "agents/dev-1-bug-analyst/identity.md"
OUTPUT_REF = "agents/dev-1-bug-analyst/output.md"
QUALITY_REF = "agents/dev-1-bug-analyst/quality.md"

REPRODUCIBILITY = ("deterministic", "intermittent", "not-reproduced")
RISK_LEVELS = ("high", "medium", "low")

EVIDENCE_TABLE = ("ID", "Evidence", "Source", "Confidence")
CAUSAL_TABLE = ("ID", "Step", "Claim", "Evidence", "Confidence")

CONTRACT = ac.ArtifactContract(
    artifact="bug-analysis.md",
    producers=("dev-1-bug-analyst",),
    metadata_key="bugAnalysis",
    metadata_fields=("analysisId", "defectReference", "sourceInputs", "producedBy",
                     "agentVersion", "schemaVersion", "status", "severity",
                     "reproducibility", "inputDigest", "contextDigest"),
    statuses=ac.STATUSES_COMPLETE,
    template_ref=TEMPLATE,
    contract_refs=(TEMPLATE, CONTRACT_REF, OUTPUT_REF, QUALITY_REF),
    appendices=("Open Questions",),
    id_prefixes={"E": "Reproduction", "C": "Root Cause Analysis", "Q": "Open Questions"},
    sections=(
        ac.Section("Metadata", fields=(
            ac.Field("Bug ID"),
            ac.Field("Reporter"),
            ac.Field("Severity", enum=ac.SEVERITIES),
            ac.Field("Status", enum=ac.STATUSES_COMPLETE),
        )),
        ac.Section("Symptom Summary", fields=(
            ac.Field("Observed behavior", min_words=4),
            ac.Field("Expected behavior", min_words=4),
            ac.Field("First observed date"),
            ac.Field("Affected environments"),
        )),
        ac.Section("Reproduction", fields=(
            ac.Field("Preconditions"),
            ac.Field("Steps to reproduce", min_words=4),
            ac.Field("Reproduction frequency", enum=REPRODUCIBILITY),
            ac.Field("Evidence"),
        ), table=ac.Table(EVIDENCE_TABLE, min_rows=1)),
        ac.Section("Impact Assessment", fields=(
            ac.Field("User impact"),
            ac.Field("Business impact"),
            ac.Field("Technical impact"),
            ac.Field("Blast radius"),
        )),
        ac.Section("Root Cause Analysis", fields=(
            ac.Field("Root cause statement", min_words=5),
            ac.Field("Why detection failed earlier", min_words=4),
        ), table=ac.Table(CAUSAL_TABLE, min_rows=1)),
        ac.Section("Fix Strategy", fields=(
            ac.Field("Proposed fix", min_words=4),
            ac.Field("Alternative options"),
            ac.Field("Regression risk", enum=RISK_LEVELS),
            ac.Field("Regression scope", min_words=3),
        )),
        ac.Section("Validation Plan", fields=(
            ac.Field("Verification steps", min_words=4),
            ac.Field("Regression tests added"),
            ac.Field("Monitoring signals after release"),
        )),
        ac.Section("Closure", fields=(
            ac.Field("Resolution summary"),
            ac.Field("Linked PR and release"),
            ac.Field("Prevention actions"),
        )),
    ),
    obligations=(
        ("N1", QUALITY_REF + "#not-machine-checkable-obligations",
         "The stated root cause is the cause, not the symptom location"),
        ("N2", QUALITY_REF + "#not-machine-checkable-obligations",
         "No speculative conclusion appears without supporting data"),
        ("N3", QUALITY_REF + "#not-machine-checkable-obligations",
         "The declared blast radius covers every boundary the defect crosses"),
    ),
)


def semantic_checks(rep, contract):
    """The defect-specific rules in `agents/dev-1-bug-analyst/quality.md`."""
    meta = getattr(rep, "meta", {}) or {}
    body_of = getattr(rep, "body_of", {}) or {}

    def add(cid, ref, severity, description, result, detail=""):
        rep.checks.append(Check(cid, ref, severity, description, result, detail))

    # B1 -- "Require evidence for each causal claim."
    headers, rows = ac.find_table(body_of.get("Root Cause Analysis", ""), CAUSAL_TABLE)
    uncited = []
    if headers:
        ev_col = ac.column_index(headers, "evidence")
        id_col = ac.column_index(headers, "id")
        for r in rows:
            cell = r[ev_col] if 0 <= ev_col < len(r) else ""
            if not ac.referenced_ids(cell, "E"):
                uncited.append(strip_md(r[id_col]) if 0 <= id_col < len(r) else "?")
    add("B1", QUALITY_REF + "#evidence-checks", "Blocking",
        "Every causal step cites at least one evidence identifier",
        "pass" if (headers and not uncited) else "fail",
        f"steps without an evidence citation: {uncited}" if uncited
        else (f"{len(rows)} causal step(s) evidence-backed" if headers
              else "no causal chain table to check"))

    # B2 -- the metadata block and the Metadata section are one fact, not two.
    section_fields = ac.parse_field_bullets(body_of.get("Metadata", ""))
    pairs = [("severity", "severity"), ("status", "status")]
    drift = []
    for meta_key, label in pairs:
        a = str(meta.get(meta_key) or "").strip().lower()
        b = strip_md(section_fields.get(label, "")).lower().rstrip(".")
        if a != b:
            drift.append(f"{meta_key}: metadata={a!r} section={b!r}")
    add("B2", QUALITY_REF + "#agreement-checks", "Blocking",
        "Severity and status agree between the metadata block and the Metadata section",
        "pass" if not drift else "fail",
        f"disagreement: {drift}" if drift else "both fields agree")

    repro = str(meta.get("reproducibility") or "").strip().lower()
    repro_field = strip_md(
        ac.parse_field_bullets(body_of.get("Reproduction", "")).get(
            "reproduction frequency", "")).lower().rstrip(".")
    add("B3", QUALITY_REF + "#agreement-checks", "Blocking",
        "Declared reproducibility agrees with the recorded reproduction frequency",
        "pass" if repro and repro == repro_field else "fail",
        f"metadata={repro!r} section={repro_field!r}")

    # B4 -- "Validate reproducibility before final diagnosis."
    status = str(meta.get("status") or "").strip().lower()
    ok = not (repro == "not-reproduced" and status == "complete")
    add("B4", QUALITY_REF + "#reproduction-checks", "Blocking",
        "An unreproduced defect is not reported as a complete diagnosis",
        "pass" if ok else "fail",
        f"status={status!r} with reproducibility={repro!r}" if not ok
        else f"status={status!r}, reproducibility={repro!r}")

    # B5 -- "No closure of unresolved critical defects."
    severity = str(meta.get("severity") or "").strip().lower()
    questions = ac.defined_ids(body_of.get("Open Questions", ""), "Q")
    ok = not (severity == "critical" and status != "complete") or bool(questions)
    add("B5", QUALITY_REF + "#status-and-question-checks", "Blocking",
        "A critical defect left unresolved records what is unresolved as an open question",
        "pass" if ok else "fail",
        f"severity=critical status={status!r} with no open question recorded" if not ok
        else f"severity={severity!r} status={status!r}, {len(questions)} open question(s)")
    return rep


def validate(artifact_path, envelope: dict | None = None):
    rep = ac.run_contract(CONTRACT, Path(artifact_path), envelope)
    return semantic_checks(rep, CONTRACT)


def main():
    return ac.cli(CONTRACT, extra=semantic_checks)


if __name__ == "__main__":
    sys.exit(main())
