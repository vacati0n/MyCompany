# Business Analyst: Output Contract

## Status

Binding structural and semantic contract for the sole deliverable of
`business-analyst`, version 1.0.0.

This module governs the artifact's shape. `quality.md` governs the checks that decide
whether an instance conforms. Where the two appear to disagree, this module defines the
requirement and `quality.md` defines how it is measured.

## Deliverable

| Property | Value |
|---|---|
| artifact | `requirement-framing.md` |
| template | `../../templates/requirement-framing.md` |
| validator | `../../runtime/requirement_framing_validator.py` |
| registry record | `requirement-framing` in `../../registry/templates.yaml` |
| produced in | `problem-framing` of `investigate`, `research-framing` of `research` |
| assessed at | Framing Gate, decided by `product-owner` |

One artifact per invocation. No supplementary file is written, and no content is emitted to
the reply that belongs in the artifact.

## Structural Contract

The artifact carries, in this exact order:

1. A single fenced `yaml` block holding the `requirementFraming` metadata, before any
   level-2 section.
2. Nine mandatory level-2 sections, in the order below, none omitted.

| # | Section | Content form |
|---|---|---|
| 1 | Metadata | field bullets |
| 2 | Business Context | field bullets |
| 3 | Target Outcomes | table, `O-nnn` |
| 4 | Requirements | table, `R-nnn` |
| 5 | Acceptance Intent | table, `AI-nnn` |
| 6 | Framing Boundaries | table, `B-nnn` |
| 7 | Assumptions | table, `AS-nnn` |
| 8 | Open Questions | table, `Q-nnn` |
| 9 | Handoff | field bullets |

Section titles are fixed. A section is never renamed, merged, reordered, or dropped. A
section with nothing to report reads `None identified.` — except Target Outcomes,
Requirements, and Acceptance Intent, which must carry at least one row, because a framing
with no outcome, no requirement, or no acceptance intent has framed nothing.

## Metadata Block

The leading block is the artifact's provenance record and is machine-read first.

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

Rules:

- Every key is present and populated. An empty key is a contract violation, not a blank.
- `producedBy` is always `business-analyst`. This agent never claims another producer.
- `subject` equals the Metadata section's `Subject` field. The two are one fact.
- `requirementCount` equals the Requirements row count. The two are one fact.
- `status` and `framingVerdict` agree: `complete` pairs with `framed`, `provisional` with
  `partially-framed`, `blocked` with `blocked`.
- `inputDigest` and `contextDigest` identify what was read, so a later run can be compared
  against this one.

## Section Contracts

### Metadata

Fields: `Subject`, `Requested by`, `Decision owner`, `Workflow phase`, `Framing date`.

`Workflow phase` is `problem-framing` or `research-framing`, matching the phase routed. It
is never left generic.

### Business Context

Fields: `Business intent`, `Problem statement`, `Affected stakeholders`,
`Current-state pain`.

`Problem statement` states the problem in business terms and names no technical approach.
`Business intent` restates the supplied intent stripped of any mechanism it arrived wrapped
in. A field the inputs do not support is recorded as unknown with a matching open question,
never filled from general knowledge.

### Target Outcomes

Columns: `ID`, `Outcome`, `Measure`, `Business Driver`. At least one row.

Each row states a business result, not a capability to build. `Measure` states how the
business would know the outcome was reached; where the inputs supply no measure, the cell
records its absence and a matching open question is raised. Outcomes keep the order the
supplied intent states them in.

### Requirements

Columns: `ID`, `Requirement`, `Type`, `Outcome Ref`, `Priority`. At least one row.

- `Requirement` states a condition that must be true, in the present tense, naming no
  mechanism.
- `Type` is `functional` or `non-functional`, and nothing else.
- `Outcome Ref` names an `O-nnn` defined in Target Outcomes. A requirement that serves no
  declared outcome does not belong in this table.
- Ordering: by the outcome served, then by priority descending; ties break toward the lower
  requirement text in lexical order.

### Acceptance Intent

Columns: `ID`, `Acceptance Intent`, `Requirement Ref`, `Demonstrated By`, `Priority`. At
least one row.

- `Acceptance Intent` states what acceptance would have to demonstrate — the observation,
  not the test design and not the numeric threshold.
- `Requirement Ref` names an `R-nnn` defined in Requirements.
- Every requirement is referenced by at least one acceptance intent. A requirement nothing
  would demonstrate is untestable and belongs in Open Questions instead.
- Thresholds belong to `product-owner`; verification design belongs to `qa`. Naming
  either here is a contract violation.

### Framing Boundaries

Columns: `ID`, `Excluded Concern`, `Reason`, `Revisit Trigger`.

A named exclusion prevents requirement drift that an unstated one does not. Each row states
why the concern sits outside and what would bring it back.

### Assumptions

Columns: `ID`, `Assumption`, `Basis`, `Confidence`, `Impact If False`.

- `Basis` states what the assumption rests on. An assumption with no basis is an invented
  requirement and is recorded as an open question instead.
- `Confidence` is `high`, `medium`, or `low`.
- `Impact If False` states what breaks, so a reader can weigh the risk of carrying it.

### Open Questions

Columns: `ID`, `Question`, `Blocking`, `Owner`, `Needed By`.

`Blocking` is `yes` or `no`. `Owner` names the agent that holds the decision. A
`partially-framed` or `blocked` verdict records at least one row here; a verdict that says
the framing is incomplete while this section is empty is a contradiction.

### Handoff

Fields: `Downstream owner`, `Gate`, `Evidence for the gate`, `Deferred to downstream`.

`Deferred to downstream` names what this artifact deliberately leaves to scope, design, and
planning. A framing that does not say what it deferred reads as one that covered everything.

## Traceability

The artifact is a chain, and each link is checkable by inspection:

```
supplied intent -> O-nnn (outcome) -> R-nnn (requirement) -> AI-nnn (acceptance intent)
```

- Every `R-nnn` names the `O-nnn` it serves.
- Every `AI-nnn` names the `R-nnn` it bounds.
- Every `R-nnn` is named by at least one `AI-nnn`.
- Every referenced identifier is defined in its declaring section.

A break anywhere in this chain means a downstream phase would have to guess what an item is
for, which is the failure this artifact exists to prevent.

## Identifier Schemes

| Prefix | Declared in | Ordering |
|---|---|---|
| `O-nnn` | Target Outcomes | order of the supplied intent |
| `R-nnn` | Requirements | outcome served, then priority descending |
| `AI-nnn` | Acceptance Intent | order of the requirement bounded |
| `B-nnn` | Framing Boundaries | order recorded |
| `AS-nnn` | Assumptions | order recorded |
| `Q-nnn` | Open Questions | order recorded |

Each is zero-padded to three digits and contiguous from `001`. An identifier is assigned once
and never renumbered; a correction opens a new row and records the superseded one as an open
question.

## Prohibited Content

The artifact never carries:

- A task, work-breakdown, or estimate identifier — `planner` owns those.
- A change-set identifier — `dev-1-implement` owns those.
- An architecture decision record identifier — `architect` owns those.
- A technical approach, component design, schema, interface, or technology selection.
- An acceptance threshold, test case, or verification procedure.
- A scope decision stating what is in or out of the change.
- A gate decision on this artifact.
- A readiness, merge, release, or deployment judgement.
- A model, vendor, or agent-runtime name.
- A credential, token, or secret.

## Rendering Rules

- Markdown only; one fenced block, the metadata.
- Tables carry the declared columns, in the declared order, with the header spelled as the
  template spells it.
- Identifiers are rendered in backticks, as the template renders them.
- No template comment survives into the rendered artifact.
- No cell is left empty; a cell with nothing to say carries an explicit marker.
