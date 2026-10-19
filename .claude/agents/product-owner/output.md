# Product Owner: Output Contract

## Status

Binding output contract for agent `product-owner`, version 1.0.0. Governs
`templates/scope-definition.md` where the two disagree.

## Deliverable

One artifact: `scope-definition.md`, written to the path the invocation envelope names in
`expected_output_schema.artifact_path`.

The artifact is the boundary, not a summary of the conversation that produced it. A reader
who has never seen the request must be able to tell from this file alone what is being
delivered, what is not, and how anyone would know it was delivered.

## Structural Contract

Nine mandatory sections, each present exactly once, in this order:

| # | Section | Shape | Empty form |
|---|---|---|---|
| 1 | Metadata | five field bullets | never empty |
| 2 | Business Context | four field bullets | never empty |
| 3 | In Scope | table, at least one row | never empty |
| 4 | Out of Scope | table | `None identified.` |
| 5 | Acceptance Criteria | table, at least one row | never empty |
| 6 | Constraints and Dependencies | four field bullets | never empty |
| 7 | Scope Decisions | table | `None identified.` |
| 8 | Open Questions | table | `None identified.` |
| 9 | Handoff | four field bullets | never empty |

No appendix section is permitted. No other level-2 section may be introduced.

A leading fenced YAML block under key `scopeDefinition` carries the provenance metadata. It
is the only fenced block the artifact may contain.

Sections 3 and 5 have no empty form. A scope definition that delivers nothing and accepts
nothing is not a bounded scope at a lower verdict; it is an artifact that should not have
been emitted, and the run reports `E-INPUT-MISSING` instead.

## Metadata Block

```yaml
scopeDefinition:
  scopeId:                   # SCOPE-<year>-<sequence>
  featureName:               # the change this scope bounds
  sourceInputs:              # one entry per supplied input, with its declared type and reference
  producedBy: product-owner
  agentVersion:              # semantic version of this agent
  schemaVersion: 1.0.0
  status:                    # complete | provisional | blocked
  scopeVerdict:              # bounded | partially-bounded | blocked
  acceptanceCriteriaCount:   # the number of rows in the Acceptance Criteria table
  inputDigest:               # from the invocation envelope's context slice
  contextDigest:             # from the invocation envelope's context slice
```

`featureName` and `status` are stated twice: once here and once in the body. They are one
fact recorded in two places, and they must agree. `acceptanceCriteriaCount` is the same
rule applied to a count: the metadata and the table must be counting the same criteria. A
disagreement is a blocking failure, not a formatting slip.

`status` and `scopeVerdict` move together: `complete` with `bounded`, `provisional` with
`partially-bounded`, `blocked` with `blocked`.

## Section Contracts

### Metadata

Five bullets: `Feature name`, `Requested by`, `Business goal`, `Target outcome`,
`Scope decision date`.

`Business goal` states the outcome the business is buying, in business terms. A goal that
names a mechanism has decided something this artifact does not decide.

### Business Context

Four bullets: `Problem statement`, `Value hypothesis`, `Affected users`, `Success measure`.

`Problem statement` states the problem without a solution inside it. `Success measure`
states how the business would know this worked; where the inputs supply no measure, the
field says so and a corresponding open question is recorded.

### In Scope

One row per bounded deliverable.

| Column | Content |
|---|---|
| ID | `S-nnn`, ascending from `S-001` |
| Scope Item | what becomes true for the affected users, as observable behaviour |
| Rationale | the supplied expectation this item delivers |
| Priority | `must-have`, `should-have`, or `could-have` |

A scope item states behaviour, never an implementation step. "Runs awaiting a decision are
listed with their decision owner" is a scope item; "add a listing endpoint" is a design
choice, and the architect owns it.

### Out of Scope

One row per deliberate exclusion.

| Column | Content |
|---|---|
| ID | `X-nnn`, ascending from `X-001` |
| Excluded Item | what this change does not deliver |
| Reason | why it is excluded from this change |
| Revisit Trigger | the condition under which it would be reconsidered |

An exclusion is recorded whenever a reader of the In Scope table could reasonably assume
the item was included. `None identified.` is permitted, but a verdict of `bounded` with no
exclusion recorded is reported as a correctable weakness: a boundary with no far side has
not been drawn.

### Acceptance Criteria

One row per criterion.

| Column | Content |
|---|---|
| ID | `A-nnn`, ascending from `A-001` |
| Criterion | the condition, with its threshold, that must hold |
| Scope Ref | the `S-nnn` this criterion bounds |
| Verification Method | the check, demonstration, measurement, or review that decides it |
| Priority | `must-have`, `should-have`, or `could-have` |

Every criterion states a condition on observable behaviour, with a threshold somebody could
disagree about the meeting of. "Fast" is not a criterion; "the listing renders within two
seconds for a run holding fifty work items" is.

`Verification Method` is never empty and never a none-marker. An expectation with no
verification method is recorded as a blocking open question instead, per Stage 5 of
`reasoning.md`.

### Constraints and Dependencies

Four bullets: `Business constraints`, `Regulatory or policy constraints`,
`Delivery constraints`, `External dependencies`.

Each field carries the constraints the supplied inputs and context state, or an explicit
none-marker. A constraint invented here is scope this agent added, and it belongs in the
In Scope table with a decision behind it.

### Scope Decisions

One row per boundary choice a reader could reasonably have expected to go the other way.

| Column | Content |
|---|---|
| ID | `D-nnn`, ascending from `D-001` |
| Decision | what was decided about the boundary |
| Rationale | why, in terms of the supplied intent and constraints |
| Impact | what the decision changes about the delivered outcome |
| Decided By | the role or person the decision rests with |

Every decision carries a rationale of substance. A rationale that restates the decision
justifies nothing, and the gate cannot assess it. `None identified.` is the correct content
when the boundary followed the request without a contested choice.

### Open Questions

One row per unresolved matter.

| Column | Content |
|---|---|
| ID | `Q-nnn`, ascending from `Q-001` |
| Question | what is unresolved, stated so an answer would settle it |
| Blocking | `yes` or `no` |
| Owner | the agent, role, or person that owns the answer |
| Needed By | the phase or point by which the answer is required |

A `blocking` question is incompatible with verdict `bounded`. Where one stands, the verdict
is `partially-bounded` or `blocked`, and `status` follows it.

### Handoff

Four bullets: `Downstream owner`, `Gate`, `Evidence for the gate`,
`Deferred to downstream`.

`Deferred to downstream` names what this phase deliberately left to a later one —
decomposition, technical approach, test strategy — so a downstream phase inherits the gap
rather than discovering it.

## Traceability

Two traces are mandatory and both are machine-checked.

1. Every acceptance criterion names an `S-nnn` defined in the In Scope section. A criterion
   that bounds nothing in scope is either scope this artifact failed to declare, or a
   criterion belonging to a different change.
2. Every in-scope item and every exclusion carries a rationale that ties it to a supplied
   expectation or to a recorded scope decision.

A criterion may bound only one scope item. Where an expectation spans two items, it is two
criteria, so that a later partial delivery can be assessed against each independently.

## Identifier Schemes

| Prefix | Declared in | Meaning |
|---|---|---|
| `S-nnn` | In Scope | one bounded deliverable |
| `X-nnn` | Out of Scope | one deliberate exclusion |
| `A-nnn` | Acceptance Criteria | one criterion |
| `D-nnn` | Scope Decisions | one boundary decision |
| `Q-nnn` | Open Questions | one unresolved matter |

All are zero-padded to three digits, ascend from 001 without gaps, and are defined exactly
once in their declaring section. An identifier referenced anywhere in the artifact is
defined in its declaring section.

## Prohibited Content

- task, wave, or estimate identifiers — decomposition belongs to `planner`
- change-set identifiers — the change set belongs to `dev-1-implement`
- architecture decision record identifiers — those belong to `architect`
- code blocks other than the leading metadata block
- a gate decision, a merge decision, or a release readiness claim
- a model, vendor, or agent-runtime name absent from the inputs or the loaded context
- credentials, tokens, secrets, or customer-identifying content copied from any source
- a technical approach presented as a scope item

## Rendering Rules

- Field bullets render as `- Label: value`, with the label exactly as this contract states it.
- Table headers render exactly as this contract states them, in the stated column order.
- Identifiers render in backticks in their defining column and wherever they are referenced.
- A section with nothing to report renders `None identified.` and nothing else.
- The artifact carries no template authoring comments.
