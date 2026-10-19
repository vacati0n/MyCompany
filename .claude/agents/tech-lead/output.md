# Tech Lead: Output Contract

## Status

Authoritative on artifact structure. This module governs `templates/technical-recommendation.md`
and `examples.md` where they differ. `runtime/technical_recommendation_validator.py` enforces
mechanically what is decidable by inspection; the rest is recorded as a not-machine-checkable
obligation in `quality.md`.

## Deliverable

One file per invocation: `technical-recommendation.md`, written to the artifact path the
invocation envelope declares.

One artifact type carries every technical leadership output the framework asks for: both
`investigate` decision phases, both `research` decision phases, the `review-pull-request` merge
decision, and the `release` readiness assessment. Each is a criteria-based comparison of named
options over a stated decision context, closing with a recommendation and a readiness position.
The `decisionBasis` metadata field carries the lens; the structure does not change with it.

| Decision basis | Phases | The options are |
|---|---|---|
| `option-analysis` | `investigate/option-analysis`, `research/option-synthesis` | directions the work could take |
| `recommendation` | `investigate/recommendation`, `research/recommendation-draft` | directions, with one recommended |
| `merge-decision` | `review-pull-request/merge-decision` | dispositions of the change |
| `release-readiness` | `release/readiness-assessment` | go positions for the release |

## Structural Contract

Ten level-2 sections, in this exact order, with these exact titles. None is omitted. A section
with nothing to report reads `None identified.`

| # | Section | Shape |
|---|---|---|
| 1 | `Metadata` | four field bullets |
| 2 | `Decision Context` | three field bullets |
| 3 | `Evaluation Criteria` | table, `EC-nnn`, at least one row |
| 4 | `Options` | table, `O-nnn`, at least two rows |
| 5 | `Tradeoff Analysis` | table, one row per option |
| 6 | `Risk and Blocker Register` | table, `RK-nnn`, at least one row |
| 7 | `Assessment Summary` | four numeric field bullets |
| 8 | `Recommendation` | four field bullets |
| 9 | `Delivery Impact` | four field bullets |
| 10 | `Readiness` | four field bullets |

One appendix is permitted and required whenever `status` is `provisional` or `blocked`:
`Open Questions`, carrying `Q-nnn` items or `None identified.`

The artifact opens with a fenced yaml metadata block. That is the only fenced block the contract
expects; up to two more are permitted where a command's output is quoted as evidence. Diff
markers are not permitted: this artifact reasons about a change, it does not quote one.

## Metadata Block

A fenced yaml block under root key `technicalRecommendation`, before the first section. Every
field is present and populated.

| Field | Rule |
|---|---|
| `recommendationId` | stable identifier for this recommendation |
| `decisionReference` | the run, ticket, change, or release this decision serves |
| `decisionBasis` | `option-analysis` \| `recommendation` \| `merge-decision` \| `release-readiness`, resolved from the routed phase |
| `sourceInputs` | one entry per supplied input actually used, each with `type` and `reference` |
| `producedBy` | `tech-lead`; no other agent may emit this artifact |
| `agentVersion` | semantic version of this agent |
| `schemaVersion` | `1.0.0` |
| `status` | `complete` \| `provisional` \| `blocked` |
| `recommendedOption` | an `O-nnn` defined in the Options table, or `deferred` |
| `readinessDecision` | `proceed` \| `proceed-with-conditions` \| `do-not-proceed` \| `deferred` |
| `inputDigest` | digest of the resolved input set |
| `contextDigest` | digest of the frozen context slice |

`recommendedOption` and `readinessDecision` are carried in the metadata as well as in their
sections so that a consumer can route on them without parsing prose — and so that the two
statements can be checked against each other.

## Section Contracts

### 1. Metadata

- `Recommendation ID:` matches `recommendationId`.
- `Decision owner:` the role or person the decision belongs to.
- `Requested by:` who asked for it.
- `Decision date:` when the recommendation was made.

### 2. Decision Context

- `Decision to make:` one question, stated as a decision. At least three words.
- `Delivery constraints:` the time, capacity, sequencing, and dependency limits in force.
- `Assumptions in force:` what is taken as true and would change the answer if it were not.

### 3. Evaluation Criteria

Columns: `ID`, `Criterion`, `Why it matters`, `Priority`, `Source`.

- `ID` is `EC-nnn`, contiguous from 001.
- `Priority` is `must-have`, `high`, `medium`, or `low`.
- `Source` names where the criterion came from. It is never empty and never this artifact.
- At least one row. Criteria are fixed before options are scored (reasoning Stage 3).

### 4. Options

Columns: `ID`, `Option`, `Summary`, `Effort`, `Delivery risk`, `Reversibility`, `Evidence`.

- `ID` is `O-nnn`, contiguous from 001.
- `Effort` is `trivial`, `small`, `medium`, `large`, or `unknown`.
- `Delivery risk` is `critical`, `high`, `medium`, or `low`.
- `Reversibility` is `reversible`, `costly-to-reverse`, or `irreversible`.
- `Evidence` names what the assessment rests on; it is never empty.
- **At least two rows.** A single option is a proposal, not an evaluation.

### 5. Tradeoff Analysis

Columns: `Option`, `Criteria met`, `Criteria missed`, `Strengths`, `Weaknesses`,
`Sequencing implication`.

- `Option` is an `O-nnn` defined in section 4.
- Every option defined in section 4 appears here exactly once.
- `Criteria met` and `Criteria missed` name `EC-nnn` identifiers, or read `None.`
- Between them they account for every criterion declared in section 3, for every option.

### 6. Risk and Blocker Register

Columns: `ID`, `Risk or blocker`, `Severity`, `Likelihood`, `Delivery impact`, `Mitigation`,
`Owner`, `Status`.

- `ID` is `RK-nnn`, contiguous from 001.
- `Severity` is `critical`, `high`, `medium`, or `low`, judged on delivery impact.
- `Likelihood` is `certain`, `likely`, `possible`, or `unlikely`. A blocker that has already
  occurred is `certain`.
- `Status` is `open`, `mitigated`, `accepted`, or `resolved`.
- `Owner` names the role that carries it; it is never empty.
- At least one row. A decision with no recorded risk has not been examined.

### 7. Assessment Summary

Four numeric bullets, each of which must recompute exactly from the tables above:

- `Criteria applied:` the row count of section 3.
- `Options evaluated:` the row count of section 4.
- `Risks and blockers recorded:` the row count of section 6.
- `Blocking items open:` the count of section 6 rows whose severity is `critical` or `high`
  **and** whose status is `open`.

These are a recount, not a recollection. That is the only thing that makes them worth checking.

### 8. Recommendation

- `Recommended option:` an `O-nnn` defined in section 4, or `deferred`. Matches
  `recommendedOption` in the metadata.
- `Rationale:` why this option beats the others against the declared criteria. At least five
  words, and it references the criteria rather than asserting a preference.
- `Preconditions:` what must be true before the option is taken, or `None identified.`
- `Options rejected:` the option identifiers not recommended, each with its reason.

### 9. Delivery Impact

- `Effort and capacity:` what taking the recommended option costs, in the declared effort scale.
- `Sequencing constraints:` what must precede, follow, or run alongside.
- `Dependencies:` work, teams, or systems this direction depends on.
- `Reversal plan:` how the direction is unwound if it proves wrong. `irreversible` is stated as
  such rather than left blank.

### 10. Readiness

- `Recommended decision:` `proceed`, `proceed-with-conditions`, `do-not-proceed`, or `deferred`.
  Matches `readinessDecision` in the metadata.
- `Conditions to satisfy:` what a conditional recommendation is conditional on, or
  `None identified.`
- `Blocking items outstanding:` exactly the `RK-nnn` entries whose severity is `critical` or
  `high` and whose status is `open`, or `None identified.`
- `Deciding authority:` the role that decides the gate this artifact is evidence for. Never
  `tech-lead`.

### Appendix — Open Questions

`Q-nnn` items, each with the owner who can answer it and what it affects, or `None identified.`
A `provisional` or `blocked` artifact carries at least one.

## Identifier Schemes

| Prefix | Declared in | Rule |
|---|---|---|
| `EC-nnn` | Evaluation Criteria | zero-padded, ascending, contiguous from 001 |
| `O-nnn` | Options | zero-padded, ascending, contiguous from 001 |
| `RK-nnn` | Risk and Blocker Register | zero-padded, ascending, contiguous from 001 |
| `Q-nnn` | Open Questions | zero-padded, ascending, contiguous from 001 |

An identifier referenced anywhere in the artifact is defined in its declaring section. A
reference to something undefined is a broken claim, not a typo.

## Prohibited Content

- Any wording that records a gate decision as taken: `approved`, `merged`, `released`,
  `signed off`. This artifact recommends.
- `tech-lead` as the `Deciding authority`.
- A recommended option not defined in the Options table.
- A model, vendor, or provider name. Technology names appear only where they trace to the
  supplied options, constraints, or repository context.
- Diff markers, patch hunks, or quoted source changes.
- Acceptance criteria, task breakdowns, decision records, or design sections — each belongs to
  another role's artifact.

## Rendering Rules

- Section titles are copied exactly, including capitalisation. A renamed section is a structural
  failure, not a style choice.
- Empty cells are not left blank. A cell with nothing to record reads `None.`
- Tables carry their declared columns in their declared order.
- The metadata block is the first content in the file.
