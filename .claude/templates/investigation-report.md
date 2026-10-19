# Template: Investigation Report

## Usage

Canonical artifact template for `investigation-report.md`, produced by agent
`context-agent` in the `technical-discovery` phase of `workflows/investigate.md` and the
`technical-validation` phase of `workflows/research.md`. One artifact type serves both, because
both phases ask the same question of the same agent: what is actually true right now, and how
confident may a decision be in it.

Template version 1.1.0. Version 1.1.0 is the machine-checkable revision: it adds the leading
provenance metadata block, the Evidence table with confidence and staleness columns, and
identifiers on evaluated options, so the Validation Engine
(`runtime/investigation_report_validator.py`) can decide conformance rather than trusting it.
Section titles and section order are unchanged from 1.0.0.

Section titles, section order, and identifier schemes are fixed. Sections are never omitted.
A section with nothing to report reads `None identified.`

Identifier schemes: evidence records `E-nnn`, options `O-nnn`, open questions `Q-nnn`. All
zero-padded to three digits and ascending.

The binding behavioural contract is the `agents/context-agent/` module set, and
`agents/context-agent/output.md` governs this artifact's structure where the two differ. Its
rules on confidence marking, stale assumptions, and contradictions are enforced mechanically where
they are decidable by inspection. Earlier revisions named `agents/context-agent.md`, which was
never written; the module set replaced the reference.

---

```yaml
investigation:
  investigationId:
  decisionReference:
  sourceInputs:
    - type:          # requirement-framing | investigation-question | research-question | framed-objective | research-brief | context-sources | technical-data-access | business-evidence
      reference:     # supplied reference, or "inline"
  producedBy: context-agent
  agentVersion:
  schemaVersion: 1.0.0
  status:            # complete | provisional | blocked
  confidence:        # high | medium | low
  inputDigest:
  contextDigest:
```

## Metadata

- Investigation ID:
- Owner:
- Requested by:
- Decision deadline:

## Objective

- Decision to support:
- Key question:
- Scope boundaries:

## Context

- Relevant systems and components:
- Related incidents or prior findings:
- Constraints and assumptions:

## Evidence

<!-- One row per observation. Confidence is high | medium | low. Staleness states when the
source was last authoritative, or `current`; an observation whose staleness is unknown is
recorded as `unknown`, never left blank. -->

| ID | Source | Observation | Confidence | Staleness |
|---|---|---|---|---|
| `E-001` | | | | |

## Options Evaluated

<!-- At least two options: a single option is a proposal, not an evaluation. Every option
cites the evidence identifiers it rests on. -->

| ID | Option | Benefits | Risks | Effort | Evidence |
|---|---|---|---|---|---|
| `O-001` | | | | | `E-001` |
| `O-002` | | | | | |

## Recommendation

- Recommended option:            <!-- an option identifier from the table above -->
- Rationale:
- Preconditions:
- Risks requiring monitoring:

## Next Steps

- Immediate actions:
- Decision owner and deadline:
- Follow-up validation:

## Contradictions and Gaps

<!-- Appendix. Contradictions between sources, and questions the available evidence cannot
answer. `None identified.` is a valid entry; silence is not. -->

## Open Questions

<!-- Appendix. Required whenever status is provisional or blocked. -->

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
