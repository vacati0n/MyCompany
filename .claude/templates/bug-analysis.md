# Template: Bug Analysis

## Usage

Canonical artifact template for `bug-analysis.md`, produced by agent `dev-1-bug-analyst`
in the `root-cause-analysis` phase of `workflows/fix-bug.md`.

Template version 1.1.0. Version 1.1.0 is the machine-checkable revision: it adds the leading
provenance metadata block, the Evidence Register, and the Causal Chain table, so the
Validation Engine (`runtime/bug_analysis_validator.py`) can decide conformance rather than
trusting it. Section titles and section order are unchanged from 1.0.0.

Section titles, section order, and identifier schemes are fixed. Sections are never omitted.
A section with nothing to report reads `None identified.`

Identifier schemes: evidence records `E-nnn`, causal steps `C-nnn`, open questions `Q-nnn`.
All zero-padded to three digits and ascending.

The binding behavioural contract is `agents/dev-1-bug-analyst/output.md`, within the module
set `agents/dev-1-bug-analyst/manifest.yaml` declares. Two of its rules are
enforced mechanically and are worth restating here: every causal claim cites evidence, and
reproducibility is established before a diagnosis is called final.

---

```yaml
bugAnalysis:
  analysisId:
  defectReference:
  sourceInputs:
    - type:          # defect-report | symptom-evidence | business-impact-statement | logs | traces | code-context
      reference:     # supplied reference, or "inline"
  producedBy: dev-1-bug-analyst
  agentVersion:
  schemaVersion: 1.0.0
  status:            # complete | provisional | blocked
  severity:          # critical | high | medium | low
  reproducibility:   # deterministic | intermittent | not-reproduced
  inputDigest:
  contextDigest:
```

## Metadata

- Bug ID:
- Reporter:
- Severity:                     <!-- critical | high | medium | low; equals the metadata block -->
- Status:                       <!-- complete | provisional | blocked; equals the metadata block -->

## Symptom Summary

- Observed behavior:
- Expected behavior:
- First observed date:
- Affected environments:

## Reproduction

- Preconditions:
- Steps to reproduce:
- Reproduction frequency:       <!-- deterministic | intermittent | not-reproduced -->
- Evidence:                     <!-- one line naming the register below -->

### Evidence Register

<!-- Every diagnostic artifact the analysis rests on, one row each. Confidence is
high | medium | low. A causal claim may cite only identifiers declared here. -->

| ID | Evidence | Source | Confidence |
|---|---|---|---|
| `E-001` | | | |

## Impact Assessment

- User impact:
- Business impact:
- Technical impact:
- Blast radius:

## Root Cause Analysis

- Root cause statement:
- Why detection failed earlier:

### Causal Chain

<!-- Ordered steps from trigger to observed symptom. Every step cites at least one evidence
identifier from the register; a step with no citation is an inference, not a finding. -->

| ID | Step | Claim | Evidence | Confidence |
|---|---|---|---|---|
| `C-001` | | | `E-001` | |

## Fix Strategy

- Proposed fix:
- Alternative options:
- Regression risk:              <!-- high | medium | low -->
- Regression scope:             <!-- the areas a fix here can break; never omitted -->

## Validation Plan

- Verification steps:
- Regression tests added:
- Monitoring signals after release:

## Closure

- Resolution summary:
- Linked PR and release:
- Prevention actions:

## Open Questions

<!-- Appendix. Required whenever status is provisional or blocked, or whenever a critical
defect is not fully diagnosed. -->

| ID | Question | Blocking | Owner | Affected steps |
|---|---|---|---|---|
