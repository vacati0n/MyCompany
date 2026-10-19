# Context Agent: Output Contract

## Status

Binding structural and semantic contract for `investigation-report.md`, produced by agent
`context-agent`, version 1.0.0, schema version 1.0.0.

The rendered form is `templates/investigation-report.md`. This module is the contract; the template
is the shape it renders to. The Validation Engine decides conformance in
`runtime/investigation_report_validator.py`.

## Deliverable

One artifact per run: `investigation-report.md`, written at the artifact path the phase declares.

One artifact type serves both phases this agent owns:

| Workflow | Phase | Discovery basis |
|---|---|---|
| `investigate` | `technical-discovery` | `current-state-discovery` |
| `research` | `technical-validation` | `evidence-validation` |

Both are the same thing: a source-attributed record of what is true now, marked for confidence and
staleness, with the contradictions and gaps named, the courses the evidence admits set out, and a
declared confidence for the whole. The basis records which emphasis the phase asked for. The
structure does not change with it.

## Structural Contract

Seven mandatory level-2 sections, in this exact order, each present exactly once:

| # | Section | Content form |
|---|---|---|
| 1 | `Metadata` | four declared field bullets |
| 2 | `Objective` | three declared field bullets |
| 3 | `Context` | three declared field bullets |
| 4 | `Evidence` | table, at least two rows |
| 5 | `Options Evaluated` | table, at least two rows |
| 6 | `Recommendation` | four declared field bullets |
| 7 | `Next Steps` | three declared field bullets |

Two appendices follow, in this order, and no other level-2 section may appear:

| Appendix | Requirement |
|---|---|
| `Contradictions and Gaps` | always present and non-empty; `None identified.` is a valid entry |
| `Open Questions` | always present; a table of `Q-nnn` rows, or `None identified.` |

Sections are never omitted and never left empty. A section with nothing to report reads
`None identified.` — with two exceptions, `Evidence` and `Options Evaluated`, where that marker is
not permitted at all. A report with no evidence has established nothing, and a report with fewer
than two options has evaluated nothing; neither is a report that may be emitted.

`Contradictions and Gaps` is an appendix and is nonetheless mandatory. A discovery that found no
contradiction and no gap has made a strong claim, and it makes it explicitly here rather than by
omitting the section. Check `I5` decides this.

### Fenced blocks

Exactly one: the leading metadata block. No other fenced block is permitted.

This is stricter than the sibling artifacts allow, and deliberately. Pasting an extract of a source
into the report puts a second copy of the evidence into circulation, one that no longer moves when
the source does, and invites a reader to weigh the quotation instead of opening the source. Evidence
is cited so that it can be opened. The `Source` column carries the path and the element; that is the
whole mechanism.

## Metadata Block

A single leading fenced YAML block, rooted at `investigation`. Every field is required and must be
populated.

| Field | Constraint |
|---|---|
| `investigationId` | unique identifier for this discovery |
| `decisionReference` | the decision this evidence supports |
| `sourceInputs` | one entry per input actually read, each with `type` and `reference` |
| `producedBy` | `context-agent` |
| `agentVersion` | semantic version of this agent |
| `schemaVersion` | `1.0.0` |
| `status` | `complete` \| `provisional` \| `blocked` |
| `confidence` | `high` \| `medium` \| `low` |
| `inputDigest` | from the frozen snapshot in the invocation envelope |
| `contextDigest` | from the frozen snapshot in the invocation envelope |

`sourceInputs` uses the declared type vocabulary: `requirement-framing`, `investigation-question`,
`research-question`, `framed-objective`, `research-brief`, `context-sources`,
`technical-data-access`, `business-evidence`. It lists what was read, not what was available. An input listed but unread is a
false claim about the basis of this report.

`confidence` is the report-level confidence from the Stage 10 table in `reasoning.md`. It is not an
average of the evidence column and it is not a mood; it is what the observations behind the
recommendation permit.

## Section Contracts

### 1. Metadata

| Field | Content |
|---|---|
| `Investigation ID` | matches `investigationId` |
| `Owner` | `context-agent` |
| `Requested by` | the role that requested this discovery |
| `Decision deadline` | the date the decision this supports is needed by |

### 2. Objective

| Field | Content |
|---|---|
| `Decision to support` | the decision this evidence feeds. At least four words. |
| `Key question` | the question this report answers, in one sentence. At least four words. |
| `Scope boundaries` | what is inside and outside, and whose decision put it outside. At least three words. |

These three are the scope fixed at Stage 2 of `reasoning.md`, before any source was read. Narrowing
them afterwards to match what was reached is a silent scope reduction and is rejected by `quality.md`
check `X5`.

### 3. Context

| Field | Content |
|---|---|
| `Relevant systems and components` | the systems in play, from the observations that established them |
| `Related incidents or prior findings` | prior evidence bearing on this question, or `None identified.` |
| `Constraints and assumptions` | constraints in force, marking which are established and which supplied |

An assumption recorded here is labelled as an assumption. An assumption recorded as a constraint is
the failure mode this field exists to prevent.

### 4. Evidence

A table, at least two rows, with these columns in this order:

| Column | Content |
|---|---|
| `ID` | `E-nnn`, in the source order fixed at Stage 3 |
| `Source` | the source read, precisely enough to open: the path with the element named |
| `Observation` | what the source states, not what it implies |
| `Confidence` | `high` \| `medium` \| `low`, by the Stage 4 table |
| `Staleness` | `current`, `as of <date>`, or `unknown` |

Every column is required in every row. Two rules make this table the load-bearing part of the
report, and both are checked:

- **Every row is an observation, not an inference.** A conclusion drawn from other rows belongs in
  `Contradictions and Gaps` or in `Options Evaluated`. This is obligation `N3`, and it is the one a
  machine cannot decide.
- **No cell is blank.** A blank `Staleness` reads as `current` to every downstream reader, and a
  blank `Confidence` reads as certainty. Checks `I3` and `I4` reject both.

A minimum of two rows is not a formality. A decision supported by a single observation is supported
by a single point of failure.

### 5. Options Evaluated

A table, at least two rows, with these columns in this order:

| Column | Content |
|---|---|
| `ID` | `O-nnn`, ordered by descending evidential support per the Stage 8 rule |
| `Option` | the course of action, described — never designed |
| `Benefits` | what the cited evidence shows in its favour |
| `Risks` | what the cited evidence shows against it |
| `Effort` | as the evidence characterises it, or a statement that the evidence supports no figure |
| `Evidence` | the `E-nnn` identifiers this option rests on |

An empty `Evidence` cell makes the row an opinion, and check `I2` rejects it. At least two options
are required, because a single option is a proposal rather than an evaluation.

An option describes a course; it does not specify how to build it. A row that carries an
implementation approach has taken the architect's work, and it also leaves the Technical Gate
assessing a design rather than the evidence it was convened to assess.

### 6. Recommendation

| Field | Content |
|---|---|
| `Recommended option` | an `O-nnn` identifier from the table above |
| `Rationale` | why the cited observations favour it. At least five words. |
| `Preconditions` | what the evidence shows must hold for it |
| `Risks requiring monitoring` | risks the evidence raises that would need watching |

`Recommended option` must name an option this report evaluated; check `I1` rejects anything else,
including an option named in prose that appears in no row.

This is a recommendation on the evidence, and it is not a decision. The architect decides at the
gate; the tech lead decides the direction in the phase that follows. Where the evidence favours no
option clearly, `Rationale` says so and names what would distinguish them — and the report
confidence drops accordingly.

### 7. Next Steps

| Field | Content |
|---|---|
| `Immediate actions` | what should happen next, for the roles that own it. At least three words. |
| `Decision owner and deadline` | who decides, and by when |
| `Follow-up validation` | what would confirm the reading, or close a gap |

`Immediate actions` names actions for their owners. It does not assign work, sequence it, or
estimate it; that is `planner`'s output, in a phase this agent does not own.

### Appendix — Contradictions and Gaps

Two things, in one section:

- **Contradictions.** Each names the observation identifiers that disagree, and states whether a
  further source settled it or the contradiction stands. A contradiction that is really two accurate
  statements about different things — a target state and a current state, for instance — is recorded
  as exactly that, in those words.
- **Gaps.** Each names what could not be established from any reachable source, and what would
  close it.

`None identified.` is a valid entry and a real claim: it asserts that the sources agree and that the
question is fully answerable from them. Omitting the section is not an option, and neither is
leaving it blank.

### Appendix — Open Questions

A table of `Q-nnn` rows, with these columns in this order: `ID`, `Question`, `Blocking`, `Owner`,
`Affects`. `None identified.` where there are none.

A `provisional` or `blocked` report carries at least one open question. A report that could not
complete but has nothing to ask has not identified why it could not complete.

## Identifier Schemes

| Prefix | Declared in | Scheme |
|---|---|---|
| `E` | `Evidence` | `E-nnn`, zero-padded, ascending from 001 |
| `O` | `Options Evaluated` | `O-nnn`, zero-padded, ascending from 001 |
| `Q` | `Open Questions` | `Q-nnn`, zero-padded, ascending from 001 |

Every identifier referenced anywhere in the report is defined in its declaring section. No
identifier is defined twice, and none is renumbered after assignment, except to compact a
family after a mid-run retirement; the compaction records its old-to-new mapping, describing
superseded items by subject, never by their retired identifier token.

## Prohibited Content

1. Any model, vendor, or agent-runtime name that a cited source does not already use.
2. Any credential, token, or secret, including one encountered in a source.
3. Real personal or production data quoted as evidence.
4. An exploitable detail of an observed weakness beyond what the observation requires.
5. An observation with no source, or a source that cannot be resolved.
6. An inference recorded as a row of the evidence table.
7. A technical design, an architecture decision, code, a test, or a migration.
8. A task breakdown, a sequence, or an effort estimate this agent produced rather than read.
9. A scope change, a gate decision, a merge decision, or a release decision.
10. A fenced block other than the leading metadata block.

## Rendering Rules

1. Section titles are used verbatim as this module states them.
2. Field bullet labels are used verbatim as this module states them.
3. Table columns appear in the declared order, with no empty required cell.
4. No template authoring comment survives into the emitted artifact.
5. Confidence, staleness, status, and basis values are rendered in lower case, exactly as their
   vocabularies declare them.
6. `None identified.` is the only permitted empty-content marker, and it is not permitted in
   `Evidence` or `Options Evaluated`.
