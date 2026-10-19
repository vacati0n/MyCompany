# Template: Technical Design

## Usage

Canonical technical design package, produced by agent `architect`.
The binding structural and semantic contract is `agents/architect/output.md`.
Where this template and that contract differ, the contract governs.

Template version 1.1.0. Version 1.1.0 adds four sections additively: Current-State
Assumptions and Constraints, Reusable Components and Reuse Rationale, Estimate and
Confidence, and Open Decisions and Escalations. Existing sections are unchanged.

Section titles and order are fixed. Sections are never omitted. A section with nothing to
report reads `None identified.`

Identifier schemes: statements `S-nnn`, facts `F-nnn`, assumptions `A-nnn`, constraints
`C-nnn`, modules `M-nnn`, options `O-nnn`, decisions `D-nnn`, risks `R-nnn`, plan steps
`P-nnn`, open questions `Q-nnn`. All zero-padded to three digits and ascending.

Architecture-significant decisions additionally produce
`templates/architecture-decision-record.md` at status `Proposed`.

---

```yaml
design:
  designId:
  changeReference:
  sourceInputs:
    - type:          # change-request | business-intent | architecture-context | execution-plan
      reference:     # supplied reference, or "inline"
  producedBy: architect
  agentVersion:
  schemaVersion: 1.0.0
  status:            # complete | provisional | blocked
  decisionRecords: []
  consumesPlan:      # execution plan reference, or "none"
  inputDigest:
  contextDigest:
```

## Metadata

- Feature or Change ID:
- Author:
- Reviewers:
- Last Updated:

## Objective

- Desired outcome:
- Architectural objectives:   <!-- structural properties, each tracing to a statement -->
- In scope:
- Out of scope:               <!-- bounds the impact surface before analysis begins -->

## Requirements Summary

- Functional requirements:
- Non-functional requirements:
- Acceptance criteria:

<!-- Each requirement traces to a statement identifier. -->

## Current-State Assumptions and Constraints

<!-- Facts and assumptions are disjoint. No claim about the current system may appear
anywhere in this package without an F-nnn or A-nnn reference. -->

### 4.1 Facts

| ID | Fact | Established by |
|---|---|---|
| F-001 | | |

### 4.2 Assumptions

| ID | Assumption | Why needed | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | | | | |

### 4.3 Constraints

| ID | Class | Constraint | Hard or negotiable | Source |
|---|---|---|---|---|
| C-001 | | | | |

<!-- Class: functional | quality-attribute | security | compliance | operability |
structural | migration. A hard constraint eliminates options; a negotiable one costs
them points. -->

## Architecture and Component Design

### 5.1 Impacted Modules

| ID | Module | Impact type | Basis | Interfaces affected | Confidence |
|---|---|---|---|---|---|
| M-001 | | | | | |

<!-- Impact type: contract-change | behavior-change | extension | dependency-change |
operational-impact | no-change-verified. Name modules explicitly; a layer is not a module.
Record no-change-verified for modules a reader would expect to be impacted. -->

### 5.2 Options Considered

| Option | Structural change | <constraint columns> | Impact surface | Reuse leverage | Migration burden | Outcome |
|---|---|---|---|---|---|---|
| O-001 | | | | | | |

<!-- Every option evaluated against every criterion. Eliminated options name the hard
constraint violated. The selection must be re-derivable from this table. -->

### 5.3 Selected Approach

- Selected:
- Structural change:
- Rationale:
- Highest-scoring rejected alternative and why it lost:
- Tradeoffs accepted:

### 5.4 Decisions

| ID | Decision | Architecture-significant | Record |
|---|---|---|---|
| D-001 | | | |

## API and Data Model Impact

- API changes:
- Contract compatibility notes:
- Schema or migration changes:

<!-- For each contract-change module: current shape, target shape, compatibility approach,
coexistence period, retirement condition, rollback position. Data migrations state
direction, reversibility, and reader/writer behavior during transition. -->

## Reusable Components and Reuse Rationale

| Capability | Candidate | Outcome | Rationale |
|---|---|---|---|
| | | | |

<!-- Outcome: reuse-as-is | reuse-extended | rejected | none-found. A none-found outcome
states where the search looked. New structure may be proposed only where the outcome is
rejected or none-found. -->

## Operational Considerations

- Logging and observability updates:
- Error handling strategy:
- Security considerations:
- Performance considerations:

<!-- Each item references the M-nnn or C-nnn it derives from. Security and compliance
impact is assessed for every change; None identified. requires a stated reason. -->

## Delivery Plan

### Sequencing Constraints

| ID | Constraint | Modules | Prerequisites | Reason | Binds |
|---|---|---|---|---|---|
| P-001 | | | | | |

<!-- These are structural constraints, not tasks. No T-nnn identifier is created here;
the planner owns task decomposition. Binds names planner tasks constrained by this step
when an execution plan was supplied. Milestones and schedules belong to tech-lead. -->

### Test Strategy Focus Areas

-

### Rollout and Rollback

- Rollout:
- Rollback:

## Risks and Mitigations

| ID | Class | Trigger | Impact | Likelihood | Affects | Mitigation | Owner |
|---|---|---|---|---|---|---|---|
| R-001 | | | | | | | |

<!-- Class: structural | contract | migration | security | performance | operability |
delivery. Affects: M-nnn, D-nnn, or P-nnn. A risk without a trigger or an attachment does
not appear. -->

## Estimate and Confidence

- Overall:              <!-- XS | S | M | L | XL (confidence: high | medium | low) -->
- Breakdown:
- Scope assumptions:
- Uncertainty drivers:

<!-- Effort is a complexity level, never a duration. An XL estimate requires a
corresponding open decision stating what must resolve first. -->

## Open Decisions and Escalations

| ID | Question | Blocking | Owner | Affects | Consequence |
|---|---|---|---|---|---|
| Q-001 | | | | | |

<!-- Every decision record still at Proposed that blocks execution appears here with its
Design Gate owners. A blocking entry sets package status to blocked. -->

## Sign-off

- Architect:
- Tech Lead:
- QA:

<!-- Owners per workflows/workflow-gate-matrix.md. Lines are left unsigned by the
producing agent. -->
