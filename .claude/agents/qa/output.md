# QA: Output Contract

## Status

Binding structural and semantic contract for `validation-report.md`, produced by agent
`qa`, version 1.0.0, schema version 1.0.0.

The rendered form is `templates/validation-report.md`. This module is the contract; the template
is the shape it renders to. The Validation Engine decides conformance in
`runtime/validation_report_validator.py`.

## Deliverable

One artifact per run: `validation-report.md`, written at the artifact path the phase declares.

One artifact type carries every validation output the framework asks for:

| Workflow | Phase | `validationBasis` |
|---|---|---|
| `fix-bug` | `regression-validation` | `regression` |
| `refactor` | `safety-net-establishment` | `safety-net` |
| `refactor` | `behavioral-validation` | `behavioral-parity` |
| `review-pull-request` | `test-risk-validation` | `test-risk` |
| `release` | `candidate-validation` | `release-candidate` |

Each is a criterion-by-criterion result set over a defined validation scope, carrying the
defects it found and closing with a readiness verdict. The basis records which question the
report answered. The structure does not change with it.

## Structural Contract

Nine mandatory level-2 sections, in this exact order, each present exactly once:

| # | Section | Content form |
|---|---|---|
| 1 | `Metadata` | four declared field bullets |
| 2 | `Validation Scope` | three declared field bullets |
| 3 | `Test Strategy` | four declared field bullets |
| 4 | `Acceptance Criteria Results` | table, at least one row |
| 5 | `Execution Summary` | four declared field bullets, each an integer |
| 6 | `Defects` | table, at least one row |
| 7 | `Regression Assessment` | four declared field bullets |
| 8 | `Residual Risk` | three declared field bullets |
| 9 | `Verdict` | four declared field bullets |

One optional appendix is permitted: `Open Questions`. No other level-2 section may appear.

Sections are never omitted and never left empty. A section with nothing to report reads
`None identified.` — including `Defects`, where a clean validation states plainly that it found
nothing rather than dropping the section and leaving a reader to infer why it is missing.

### Fenced blocks

At most twelve, the leading metadata block among them. A validation report quotes the output of
the checks it ran, so fenced blocks and diff markers are permitted here, unlike in a plan or a
design. The prohibition that does hold is the shared one: no model, vendor, or provider name.

## Metadata Block

A single leading fenced YAML block, rooted at `validationReport`. Every field is required and
must be populated.

| Field | Constraint |
|---|---|
| `reportId` | unique identifier for this report |
| `validationReference` | the change, defect, or release this validation is about |
| `validationBasis` | `regression` \| `safety-net` \| `behavioral-parity` \| `test-risk` \| `release-candidate` |
| `sourceInputs` | one entry per input actually read, each with `type` and `reference` |
| `producedBy` | `qa` |
| `agentVersion` | semantic version of this agent |
| `schemaVersion` | `1.0.0` |
| `status` | `complete` \| `provisional` \| `blocked` |
| `verdict` | `pass` \| `pass-with-reservations` \| `fail` |
| `inputDigest` | from the frozen snapshot in the invocation envelope |
| `contextDigest` | from the frozen snapshot in the invocation envelope |

`sourceInputs` lists what was read, not what was available. An input listed but unread is a
false claim about the basis of the verdict.

## Section Contracts

### 1. Metadata

| Field | Content |
|---|---|
| `Validation ID` | matches `reportId` |
| `Validator` | `qa` |
| `Change under validation` | what was validated, in one line |
| `Validation date` | the date of this run |

### 2. Validation Scope

| Field | Content |
|---|---|
| `In scope` | the behavior, criteria, and regression surface this run validated. At least three words. |
| `Out of scope` | what was deliberately not validated, and whose decision put it there |
| `Evidence examined` | the artifacts read and the checks run, each named. At least three words. |

The scope recorded here is the scope fixed at Stage 2 of `reasoning.md`, before examination
began. Narrowing it afterwards to match what was reached is a silent scope reduction and is
rejected by `quality.md` check `E5`.

### 3. Test Strategy

| Field | Content |
|---|---|
| `Risk basis` | what made this the right depth for this change, per the Stage 4 table. At least three words. |
| `Levels executed` | the levels actually run: unit, integration, end-to-end, performance, security |
| `Environment` | where the checks ran, and how it differs from target |
| `Not executed` | levels deliberately skipped, each with its reason |

`Not executed` reading `None identified.` asserts that every level the risk warranted was run.
It is a claim, and check `E4` decides it.

### 4. Acceptance Criteria Results

A table, at least one row, with these columns in this order:

| Column | Content |
|---|---|
| `ID` | `AC-nnn`, in the order the source declared the criteria |
| `Criterion` | the criterion verbatim from its source |
| `Source` | the artifact or input that supplied it |
| `Method` | how it was checked: unit, integration, end-to-end, performance, security |
| `Result` | `met` \| `not-met` \| `blocked` |
| `Evidence` | what demonstrates the result |

Every column is required in every row. A `met` result with an empty or `None identified.`
Evidence cell is rejected by check `Q5` — this is the single most important structural rule in
this contract, because it is what stops the report from asserting that the change works.

A `blocked` row's Evidence states why the criterion could not be settled and what would settle
it.

### 5. Execution Summary

| Field | Content |
|---|---|
| `Criteria validated` | integer; the row count of section 4 |
| `Met` | integer; rows with result `met` |
| `Not met` | integer; rows with result `not-met` |
| `Blocked` | integer; rows with result `blocked` |

These are derived, never authored. Check `Q3` recomputes all four from the table and rejects any
drift. A summary that cannot be recounted is a claim, not a summary.

### 6. Defects

A table, at least one row, with these columns in this order:

| Column | Content |
|---|---|
| `ID` | `DF-nnn`, ordered by descending severity, ties by lexical location |
| `Severity` | `critical` \| `high` \| `medium` \| `low`, per the Stage 7 table |
| `Category` | `functional` \| `regression` \| `integration` \| `performance` \| `security` \| `data` \| `usability` \| `operational` |
| `Location` | where the defect manifests, resolvable in the change or system |
| `Symptom` | the observed behavior, not the suspected cause |
| `Reproducibility` | `always` \| `intermittent` \| `once` \| `not-reproduced` |
| `Status` | `open` \| `resolved` \| `accepted-risk` |

`Symptom` records what was observed. Diagnosing why belongs to `dev-1-bug-analyst`, and a
symptom column written as a cause is this agent taking that role's work.

A defect marked `accepted-risk` requires a recorded acceptance by the role that owns it. This
agent may not accept risk on another role's behalf.

### 7. Regression Assessment

| Field | Content |
|---|---|
| `Regression scope` | the existing behavior this change could plausibly have broken |
| `Regressions detected` | what broke, by defect ID, or `None identified.` |
| `Coverage of changed behavior` | which altered behavior the executed checks actually reach |
| `Untested areas` | reachable behavior no executed check covers |

`Untested areas` is the field that keeps this report honest. Behavior that no check reached
appears here whether or not any criterion mentioned it.

### 8. Residual Risk

| Field | Content |
|---|---|
| `Accepted risk` | risk accepted, naming the role that accepted it |
| `Unmitigated risk` | risk carried forward with no acceptance |
| `Monitoring required` | what would surface this risk in operation |

Risk this agent accepted alone is not accepted risk; it is unmitigated risk, and belongs in the
second field.

### 9. Verdict

| Field | Content |
|---|---|
| `Decision` | `pass` \| `pass-with-reservations` \| `fail`; matches metadata `verdict` |
| `Rationale` | why the Stage 8 table yielded this row. At least five words. |
| `Blocking defects outstanding` | the open critical and high defects by ID, or `None identified.` |
| `Readiness recommendation` | a recommendation for the gate owner, never a decision |

`Readiness recommendation` is worded as a recommendation. Wording it as a decision takes a
judgement this agent does not hold, and is rejected by check `A3`.

### Appendix — Open Questions

`Q-nnn` items, each naming the question and the role it routes to, or `None identified.`

A `provisional` or `blocked` report carries at least one open question. A report that could not
complete but has nothing to ask has not identified why it could not complete.

## Identifier Schemes

| Prefix | Declared in | Scheme |
|---|---|---|
| `AC` | `Acceptance Criteria Results` | `AC-nnn`, zero-padded, ascending from 001 |
| `DF` | `Defects` | `DF-nnn`, zero-padded, ascending from 001 |
| `Q` | `Open Questions` | `Q-nnn`, zero-padded, ascending from 001 |

Every identifier referenced anywhere in the report is defined in its declaring section. No
identifier is defined twice, and none is renumbered after assignment, except to compact a
family after a mid-run retirement; the compaction records its old-to-new mapping, describing
superseded items by subject, never by their retired identifier token.

## Prohibited Content

1. Any model, vendor, or agent-runtime name the inputs and context did not already use.
2. Any credential, token, or secret, including one surfaced by an executed check.
3. A working exploitation path for a security defect.
4. Production source, or a patch for any defect this report records.
5. A merge, release, or deployment decision.
6. A criterion this agent composed rather than took from a source.
7. A result recorded for a check that was not executed.
8. Real personal or production data in evidence.

## Rendering Rules

1. Section titles are used verbatim as this module states them.
2. Field bullet labels are used verbatim as this module states them.
3. Table columns appear in the declared order, with no empty required cell.
4. No template authoring comment survives into the emitted artifact.
5. Counts are rendered as bare integers, with no surrounding prose.
6. `None identified.` is the only permitted empty-content marker.
