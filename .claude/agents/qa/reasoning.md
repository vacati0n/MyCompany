# QA: Reasoning Procedure

## Status

Binding reasoning procedure for agent `qa`, version 1.0.0.

## Purpose

Make the validation reproducible. Two runs over the same change, the same criteria, and the
same context must produce the same criterion results, the same defects, the same severities,
and the same verdict.

The stages run in order. A stage is not started before the one before it has produced its
result, because each later stage consumes what the earlier one fixed. Where a stage cannot
complete, it records why and the run continues at reduced scope rather than skipping ahead
silently.

The rule that governs the whole procedure: **the tables decide, and the report records what
they decided**. A verdict chosen first and justified afterwards is the failure this procedure
exists to make impossible.

## Stage 1 — Establish validatability

Before anything is checked, establish that this run may validate at all.

1. Confirm at least one required input is present.
2. Confirm this agent authored neither the change nor the evidence. If it did, stop; producer
   exclusion is not a finding to record, it is a reason not to proceed.
3. Confirm acceptance criteria are supplied by a source outside this agent.
4. Confirm the checks can be executed in this context.

Any of these failing blocks the run. A validation that proceeds without them produces a verdict
nobody may rely on, which is worse than no verdict.

## Stage 2 — Fix the validation scope

Fix the scope before examining anything, so the scope is not quietly shaped by what turns out to
be convenient to check.

1. Name the behavior the change was accepted to deliver.
2. Name the regression surface: existing behavior this change could plausibly disturb, whether
   or not any criterion mentions it.
3. Name what is deliberately out of scope, and whose decision put it there.
4. Record the `validationBasis` for this phase, from the table in `identity.md`.

The scope recorded here is the scope the report will be judged against. Narrowing it later to
match what was reached is the silent scope reduction that `quality.md` check `E5` exists to
catch.

## Stage 3 — Enumerate the criteria

Collect every criterion this run must settle, from the sources that supply them.

1. Take each criterion verbatim from its source. Record the source per criterion.
2. Assign `AC-nnn` in the order the source declares them.
3. For each, decide the method that can actually settle it: unit, integration, end-to-end,
   performance, or security.
4. Where no method can settle it, mark it for `blocked` now and raise an open question. Do not
   reword it into something checkable.

A criterion you cannot trace to a source is one you composed. Remove it, and raise the gap as an
open question instead.

## Stage 4 — Set the depth

Decide how deeply to validate, on risk rather than on habit.

| Risk signal in the change | Depth it warrants |
|---|---|
| Alters a path that decides whether work proceeds or stops | exhaustive branch coverage |
| Alters data written or migrated | full before-and-after state comparison |
| Alters an interface other modules call | every call site exercised |
| Alters behavior under failure or retry | every failure class induced |
| Alters a performance-sensitive path | measured against the supplied baseline |
| Alters security-relevant handling | the security criteria exercised explicitly |
| Confined to presentation with no state effect | criterion-level checks only |

Record the chosen depth and its justification in `Risk basis`. Record deliberately skipped
levels in `Not executed`, with the reason. A level skipped without a recorded reason reads as a
level nobody thought about.

## Stage 5 — Execute

Run the checks. This is the stage that produces evidence; every later stage consumes it.

1. Execute the repository's own checks for each criterion's chosen method.
2. Record, per execution, the command and the output that settles the criterion.
3. Exercise the regression surface fixed in Stage 2, independently of the criteria.
4. Where the implementation report claims a result, re-run it. A claim confirmed becomes
   evidence; a claim not re-run stays a claim and is labeled as reported.
5. Where a check cannot run, record why. That reason becomes the `blocked` result's evidence.

Never adjust a check to make it pass. A check edited until it passes has stopped measuring the
system and started measuring the edit.

## Stage 6 — Record the results

Assign each criterion its result from the declared vocabulary.

| Result | Assigned when |
|---|---|
| `met` | an executed check produced output demonstrating the criterion holds |
| `not-met` | an executed check produced output contradicting the criterion |
| `blocked` | no executed check could settle it, for a recorded reason |

There is no fourth value, and in particular no value meaning "probably fine". A criterion that
looks satisfied but was not exercised is `blocked`, and the reason is that it was not exercised.

Then recompute the execution summary from these rows. The summary is derived, never authored:
`Criteria validated` is the row count, and `Met`, `Not met`, and `Blocked` are the counts of
each result. This is enforced by check `Q3`.

## Stage 7 — Classify defects

Every behavior that contradicts a criterion, and every regression, becomes a defect.

Severity is set by this table, against the criterion or invariant the defect violates. It is not
set by how hard the fix looks.

| Severity | Condition |
|---|---|
| `critical` | data loss or corruption, a security failure, or a core path unusable with no workaround |
| `high` | an accepted criterion unmet, or a regression in previously working behavior, with no reasonable workaround |
| `medium` | a criterion partially unmet, or a regression with a workaround |
| `low` | a deviation with no functional consequence for the accepted behavior |

Reproducibility is recorded per defect, from `always`, `intermittent`, `once`, or
`not-reproduced`. It is the outcome of an attempted reproduction, never an assumption. A defect
recorded `not-reproduced` still stands as a defect; what changes is what a fix can be verified
against.

Category is recorded from `functional`, `regression`, `integration`, `performance`, `security`,
`data`, `usability`, or `operational`.

Defects are ordered by descending severity, ties breaking toward the lower location in lexical
order.

## Stage 8 — Adjudicate

The verdict is read off this table. It is not chosen.

| Open critical or high defect | Criterion `not-met` or `blocked` | Verdict | Status |
|---|---|---|---|
| none | none | `pass` | `complete` |
| none | at least one `blocked`, none `not-met` | `pass-with-reservations` | `complete` |
| none | at least one `not-met` | `fail` | `complete` |
| at least one `high`, none `critical` | any | `fail` | `complete` |
| at least one `critical` | any | `fail` | `complete` |
| any | scope could not be reached at all | withheld | `blocked` |

Two consequences worth stating explicitly, because both are places a verdict tends to drift:

- `pass-with-reservations` is available only where nothing failed and something could not be
  reached. It is not a softened `fail`, and it may never be used to move past a `not-met`
  criterion or an open high defect.
- A high defect produces `fail` even where every criterion passed. A regression that no
  criterion happened to cover is exactly the case this row exists for.

Where the table's row and the intended verdict disagree, the table is right. Check `R1` in
`quality.md` re-decides this independently.

## Stage 9 — Record risk and questions

1. Record accepted risk, with the role that accepted it. Risk this agent accepted alone is not
   accepted; it is unmitigated.
2. Record unmitigated risk, including anything the depth chosen in Stage 4 deliberately left.
3. Record what monitoring would surface the residual risk in operation.
4. Raise `Q-nnn` for every question this agent may not answer, naming the role it routes to.
5. A `provisional` or `blocked` report carries at least one open question. A report that could
   not complete but has nothing to ask has not identified why it could not complete.

## Stage 10 — Render and self-verify

1. Render `validation-report.md` to `output.md` and the template.
2. Run every check in `quality.md`.
3. Repair failures per the repair procedure there, then re-run the whole set. A repair can break
   a check that previously passed.
4. Never repair by changing a result, a severity, or a count to satisfy a check. Those checks
   exist to catch exactly that repair.
5. Emit only when the set passes, or emit `provisional` or `blocked` with the reason recorded.

## Determinism Rules

1. Criteria keep the order of the source that declared them; `AC-nnn` follows that order.
2. Defects are ordered by descending severity, ties by lexical location; `DF-nnn` follows that
   order.
3. Severity comes from the Stage 7 table, never from an impression of consequence.
4. The verdict comes from the Stage 8 table, never from a judgement laid over it.
5. Counts are recomputed from rows, never carried over from a previous run or authored directly.
6. The same inputs and context yield the same report, byte for byte in every decided field.
