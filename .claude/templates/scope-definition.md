# Template: Scope Definition

## Usage

Canonical artifact template for `scope-definition.md`, produced by agent
`product-owner` in the `scope-and-acceptance` phase of
`workflows/implement-feature.md`.

Template version 1.0.0. It is machine-checkable from its first revision: the leading
provenance metadata block, the four identifier tables, and the acceptance-to-scope
reference column exist so the Validation Engine
(`runtime/scope_definition_validator.py`) can decide conformance rather than trusting it.

Section titles, section order, and identifier schemes are fixed. Sections are never
omitted. A section with nothing to report reads `None identified.`

Identifier schemes, each zero-padded to three digits and ascending from `001`:

| Prefix | Declared in |
|---|---|
| `S-nnn` | In Scope |
| `X-nnn` | Out of Scope |
| `A-nnn` | Acceptance Criteria |
| `D-nnn` | Scope Decisions |
| `Q-nnn` | Open Questions |

The binding behavioural contract is `agents/product-owner/output.md`, and the checks
that decide conformance are `agents/product-owner/quality.md`. Two rules from that
contract are enforced mechanically because they are decidable by inspecting the artifact:
every acceptance criterion names the in-scope item it bounds and the method that verifies
it, and the artifact carries no task, design, or change identifier, because breakdown and
design belong downstream.

---

```yaml
scopeDefinition:
  scopeId:           # SCOPE-<yyyy>-<nnnn>
  featureName:
  sourceInputs:
    - type:          # feature-request | business-intent | change-request | acceptance-intent | business-constraints
      reference:     # supplied reference, or "inline"
  producedBy: product-owner
  agentVersion:
  schemaVersion: 1.0.0
  status:                    # complete | provisional | blocked
  scopeVerdict:              # bounded | partially-bounded | blocked
  acceptanceCriteriaCount:   # equals the row count of the Acceptance Criteria table
  inputDigest:
  contextDigest:
```

## Metadata

- Feature name:                  <!-- equals featureName in the metadata block -->
- Requested by:
- Business goal:                 <!-- the outcome the business is buying, not the solution -->
- Target outcome:
- Scope decision date:

## Business Context

- Problem statement:             <!-- the problem in business terms; no technical approach -->
- Value hypothesis:
- Affected users:
- Success measure:               <!-- how the business will know this worked -->

## In Scope

What this change delivers. One row per bounded deliverable, stated as observable
behaviour rather than as an implementation step.

| ID | Scope Item | Rationale | Priority |
|---|---|---|---|
| `S-001` |  |  |  |

## Out of Scope

The boundary. A named exclusion prevents scope drift that an unstated one does not.

| ID | Excluded Item | Reason | Revisit Trigger |
|---|---|---|---|
| `X-001` |  |  |  |

## Acceptance Criteria

Every criterion is measurable, names the in-scope item it bounds, and names the method
that verifies it. A criterion that cannot be verified is an open question, not a
criterion.

| ID | Criterion | Scope Ref | Verification Method | Priority |
|---|---|---|---|---|
| `A-001` |  | `S-001` |  |  |

## Constraints and Dependencies

- Business constraints:
- Regulatory or policy constraints:
- Delivery constraints:
- External dependencies:

## Scope Decisions

Every decision that moved the boundary, with the rationale that justifies it. A decision
without a rationale cannot be reviewed at the Scope Gate.

| ID | Decision | Rationale | Impact | Decided By |
|---|---|---|---|---|
| `D-001` |  |  |  |  |

## Open Questions

| ID | Question | Blocking | Owner | Needed By |
|---|---|---|---|---|
| `Q-001` |  | yes / no |  |  |

## Handoff

- Downstream owner:              <!-- the phase owner that consumes this artifact -->
- Gate:                          <!-- the gate this artifact is evidence for -->
- Evidence for the gate:
- Deferred to downstream:        <!-- what this artifact deliberately leaves to design and planning -->
