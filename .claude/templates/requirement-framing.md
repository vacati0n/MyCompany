# Template: Requirement Framing

## Usage

Canonical artifact template for `requirement-framing.md`, produced by agent
`business-analyst` in the `problem-framing` phase of `workflows/investigate.md` and the
`research-framing` phase of `workflows/research.md`.

Template version 1.0.0. It is machine-checkable from its first revision: the leading
provenance metadata block, the six identifier tables, and the outcome and requirement
reference columns exist so the Validation Engine
(`runtime/requirement_framing_validator.py`) can decide conformance rather than trusting it.

Section titles, section order, and identifier schemes are fixed. Sections are never
omitted. A section with nothing to report reads `None identified.`

Identifier schemes, each zero-padded to three digits and ascending from `001`:

| Prefix | Declared in |
|---|---|
| `O-nnn` | Target Outcomes |
| `R-nnn` | Requirements |
| `AI-nnn` | Acceptance Intent |
| `B-nnn` | Framing Boundaries |
| `AS-nnn` | Assumptions |
| `Q-nnn` | Open Questions |

The binding behavioural contract is `agents/business-analyst/output.md`, and the checks
that decide conformance are `agents/business-analyst/quality.md`. Four rules from that
contract are enforced mechanically because they are decidable by inspecting the artifact:
every requirement traces to a declared outcome, every requirement is bounded by at least one
acceptance intent, every assumption states the basis it rests on, and the artifact carries no
task, design, or change identifier, because decomposition, architecture decisions, and
implementation belong downstream.

---

```yaml
requirementFraming:
  framingId:         # FRAME-<yyyy>-<nnnn>
  subject:
  sourceInputs:
    - type:          # business-intent | product-context | problem-statement | requirement-input
      reference:     # supplied reference, or "inline"
  producedBy: business-analyst
  agentVersion:
  schemaVersion: 1.0.0
  status:                 # complete | provisional | blocked
  framingVerdict:         # framed | partially-framed | blocked
  requirementCount:       # equals the row count of the Requirements table
  inputDigest:
  contextDigest:
```

## Metadata

- Subject:                       <!-- equals subject in the metadata block -->
- Requested by:
- Decision owner:                <!-- who the framing is produced for -->
- Workflow phase:                <!-- problem-framing | research-framing -->
- Framing date:

## Business Context

- Business intent:               <!-- the intent as supplied, in business terms -->
- Problem statement:             <!-- the problem being solved; no technical approach -->
- Affected stakeholders:
- Current-state pain:            <!-- what the present situation costs, as supplied -->

## Target Outcomes

The outcomes the business is buying. One row per outcome, stated as a business result rather
than as a capability to build. Requirements are aligned to these rows and to nothing else.

| ID | Outcome | Measure | Business Driver |
|---|---|---|---|
| `O-001` |  |  |  |

## Requirements

One row per expectation. Every requirement names the outcome it serves, so a requirement
that traces to nothing is either an outcome this artifact failed to declare or a requirement
that belongs to another change.

| ID | Requirement | Type | Outcome Ref | Priority |
|---|---|---|---|---|
| `R-001` |  | functional / non-functional | `O-001` |  |

## Acceptance Intent

What acceptance would have to demonstrate. This is the intent a criterion is later written
against, not the criterion itself: it names what must be shown, and leaves the threshold and
verification design to the phases that own them.

| ID | Acceptance Intent | Requirement Ref | Demonstrated By | Priority |
|---|---|---|---|---|
| `AI-001` |  | `R-001` |  |  |

## Framing Boundaries

The edge of the framing. A named exclusion prevents requirement drift that an unstated one
does not.

| ID | Excluded Concern | Reason | Revisit Trigger |
|---|---|---|---|
| `B-001` |  |  |  |

## Assumptions

Every assumption states the basis it rests on. An assumption with no basis is an invented
requirement wearing an assumption's clothes.

| ID | Assumption | Basis | Confidence | Impact If False |
|---|---|---|---|---|
| `AS-001` |  |  | high / medium / low |  |

## Open Questions

| ID | Question | Blocking | Owner | Needed By |
|---|---|---|---|---|
| `Q-001` |  | yes / no |  |  |

## Handoff

- Downstream owner:              <!-- the phase owner that consumes this artifact -->
- Gate:                          <!-- the gate this artifact is evidence for -->
- Evidence for the gate:
- Deferred to downstream:        <!-- what this artifact deliberately leaves to design and planning -->
