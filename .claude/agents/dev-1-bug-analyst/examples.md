# Bug Analyst: Reference Examples

## Status

Reference examples for agent `dev-1-bug-analyst`, version 1.0.0. Lowest precedence in the
module set. Where an example appears to permit what `output.md` or `quality.md` forbids, those
modules govern and the example is wrong.

Examples are abridged: each shows the sections that carry the point, not a whole artifact. A real
artifact renders all eight sections plus the appendix where required. Identifiers, dates, and
component names are illustrative.

## Example 1 — Deterministic defect, chain closed on a condition

### Input

A defect report: a scheduled export writes an empty file on the first run after a service
restart, but not afterwards. Logs supplied. Business impact statement supplied.

### Conforming output, abridged

## Reproduction

- Preconditions: the export service restarted within the last scheduling interval, with no
  export having run since the restart.
- Steps to reproduce: restart the export service, then trigger the scheduled export before the
  first interval tick completes, and read the written file.
- Reproduction frequency: deterministic
- Evidence: see the register below.

### Evidence Register

| ID | Evidence | Source | Confidence |
|---|---|---|---|
| `E-001` | the written file is zero bytes on the first post-restart run | reproduction run in this analysis | high |
| `E-002` | the cursor field is unset at the first tick and populated at the second | export service log, reproduction run | high |
| `E-003` | the cursor is initialized by the interval tick, not by service start | source read of the scheduler initialization path | high |

### Causal Chain

| ID | Step | Claim | Evidence | Confidence |
|---|---|---|---|---|
| `C-001` | restart | the in-memory cursor is discarded and not restored from persisted state | `E-003` | high |
| `C-002` | first tick | the export reads an unset cursor as "no rows since last export" rather than as "unknown" | `E-002` | high |
| `C-003` | write | an empty result set is written as a valid empty export rather than rejected | `E-001`, `E-002` | high |

- Root cause statement: an unset cursor and a cursor meaning "nothing new" are represented by the
  same value, so the export cannot distinguish an uninitialized state from an empty one.
- Why detection failed earlier: the export's only assertion checks that a file was written, not
  that its row count is consistent with the cursor it advanced.

### Why it conforms

- Reproduction was attempted in this run, and the frequency recorded is this run's outcome
  rather than the reporter's claim (`V1`).
- Every causal step cites a registered identifier (`B1`), and each identifier is defined in the
  register (`V9`).
- The chain's final step names a representation problem — one value carrying two meanings —
  rather than the line that wrote the empty file (`V11`). The place the symptom appeared is
  `E-001`, registered as evidence, which is what it is.
- Detection failure names the specific assertion that exists and what it does not cover (`V16`).
- Confidence on `C-003` is `high` because both cited records are `high`; it is not raised by
  citing two rather than one (`V12`).

## Example 2 — Intermittent defect, varying precondition isolated

### Input

A production issue description: a payment reconciliation job occasionally reports a mismatch that
disappears when re-run. Traces supplied, no impact statement.

### Conforming output, abridged

## Reproduction

- Preconditions: the reconciliation job overlapping a settlement batch write. The overlap is the
  varying precondition; runs that do not overlap a batch write do not fail.
- Steps to reproduce: start a settlement batch write, then trigger reconciliation before the
  batch commits, and compare the reported totals against the ledger.
- Reproduction frequency: intermittent
- Evidence: see the register below.

## Impact Assessment

- User impact: an operator sees a mismatch alert that resolves without action, on roughly one run
  in twelve during batch windows.
- Business impact: not supplied. No business impact statement accompanied this issue, so the
  severity below rests on technical impact alone and `Q-001` routes the question.
- Technical impact: reconciliation reports a false mismatch; no ledger data is altered, and no
  downstream process consumes the mismatch automatically.
- Blast radius: the reconciliation reporting path only. The settlement write path is unaffected,
  and no persisted data is written incorrectly. The nightly close job was not observed and is
  recorded as not observed rather than unaffected.

## Open Questions

| ID | Question | Blocking | Owner | Affected steps |
|---|---|---|---|---|
| `Q-001` | What is the business cost of a false mismatch alert during batch windows? Needed to confirm severity. | no | `product-owner` | none |
| `Q-002` | Does the nightly close job read reconciliation output on the same uncommitted view? Would extend the blast radius. | no | `context-agent` | `C-003` |

### Why it conforms

- The varying precondition was isolated and recorded, which is the real work of an intermittent
  diagnosis rather than the label itself (`V3`).
- Missing business impact is stated as missing, severity is declared as resting on technical
  impact alone, and the question is routed rather than estimated (`V22`).
- An unobserved environment is recorded as unobserved, not folded into "unaffected" (`V21`).
- The blast radius states what is *not* reached as well as what is, and `Q-002` records the
  boundary that could not be checked (`V19`, `V31`).

## Example 3 — Not reproduced, and the analysis says so

### Situation

An incident report describes a timeout under load. The load profile cannot be recreated in any
available environment.

### Conforming output, abridged

## Reproduction

- Preconditions: sustained concurrent request volume above the level any available environment
  can generate. This precondition could not be met.
- Steps to reproduce: attempted at the highest load the shared environment sustains, which is
  below the reported threshold, across three runs at increasing concurrency; the timeout did not
  occur at any level reached.
- Reproduction frequency: not-reproduced
- Evidence: see the register below.

## Root Cause Analysis

- Root cause statement: not established. The evidence reaches a connection pool that saturates
  under the reported concurrency, but nothing in this run distinguishes saturation as the cause
  from saturation as a second symptom of the same upstream contention.
- Why detection failed earlier: no alert covers pool saturation, and the load tests that exist
  run below the reported concurrency.

### Why it conforms

- Status is `provisional`, not `complete`, because reproducibility is `not-reproduced` (`B4`).
- The root cause statement says the cause is not established, in the statement itself, rather
  than naming the pool and letting the reader infer certainty (`V11`, `V15`).
- What was attempted, at what levels, and in which environment is recorded — so a later run with
  a better environment knows where to start (`V4`).
- The two competing accounts are both recorded; neither is chosen on the strength of fitting
  (`V14`).

## Example 4 — Triage phase, chain deliberately left open

### Situation

A `triage-and-impact` invocation on a newly reported data corruption defect. Severity and radius
must be settled now; the cause is not yet traced.

### Conforming output, abridged

## Impact Assessment

- User impact: affected records display a stale value after an update, for any user whose update
  landed during a replica lag window.
- Business impact: customer-facing data is wrong in a way the customer can see and act on, per
  the supplied impact statement.
- Technical impact: incorrect values are persisted, not merely displayed; the corruption survives
  a cache flush.
- Blast radius: the record update path, the read replica set, and every record written during a
  lag window since the affected release. Persisted incorrect data is in the radius and is not
  removed by a code fix.

## Fix Strategy

- Proposed fix: not yet determined; the causal chain is provisional pending `root-cause-analysis`.
  Whatever the fix is, it must also address the records already written incorrectly, which a code
  change alone will not correct.
- Alternative options: none can be compared before the cause is established. Recording one now
  would rank approaches against an unknown.
- Regression risk: high
- Regression scope: the record update path and the replica read path, which share the same
  consistency assumption; and any downstream consumer that has already read an incorrect value,
  reached through the export and reporting paths.

### Why it conforms

- Severity and blast radius are settled before the cause is known, which is what the triage phase
  owes the gate (`V17`).
- Persisted bad data is named in the radius and carried into the fix strategy, so the eventual
  fix cannot quietly scope it out (`V20`, `V27`).
- `Proposed fix` states what it is conditional on rather than guessing (`V26`).
- `Alternative options` explains why none is listed, instead of reading `None identified.` as
  though the space had been searched.
- Regression scope names the connecting dependency for each area rather than listing components
  (`V23`).

## Non-conforming behaviours

Each is a real failure mode, not a hypothetical one. Each is rejected.

### N1 — The stack trace as the diagnosis

The trace names a null dereference at `OrderMapper.map`. The chain is one row citing that trace,
and the root cause statement reads "null dereference in `OrderMapper.map`". Rejected by `V11`.
The trace establishes where the failure surfaced. What put a null into that field is the
question, and it has not been asked.

### N2 — Reproducibility copied from the report

The report says "happens every time". The artifact records `deterministic` without an attempt.
Rejected by `V1`. The reporter's environment is not this run's, and the value recorded is a
finding about what was established here.

### N3 — A step cited to evidence that does not support it

A step claims the cache was stale, citing a record showing the cache was *read*. Rejected by
`N2`, the obligation, not by a machine — the citation exists and `B1` passes. This is precisely
why `N2` is stated as an obligation this agent discharges rather than a check it delegates.

### N4 — The radius trimmed to the intended fix

Evidence shows the defect reaching three services. The radius names one, because the fix is
planned for that one. Rejected by `V19` and `V24`. The radius is a fact about the defect; the fix
scope is a delivery judgement, and letting the second decide the first hides what the fix will
leave broken.

### N5 — Severity moved by the calendar

Severity is recorded `medium` with a note that the release is tomorrow and a `high` would hold
it. Rejected by `V18`. The release pressure is real and belongs in the record as context; it
changes what the gate decides, not what the defect does.

### N6 — The analyst supplies the patch

`Proposed fix` carries the corrected function body. Rejected by `A4` and `C7`. The role has now
implemented the fix through the artifact, and the review that follows is reviewing this agent's
code while believing it is reviewing the implementer's.

### N7 — The first account that fits, unchallenged

A coherent chain is traced, every step cited, no alternative considered. Rejected by `V14`. A
chain that was never tested against a competitor is a chain that was assumed, however well it is
supported step by step.

### N8 — `complete` over an unreproduced defect

Reproduction failed; the analysis is confident anyway and declares `complete`. Rejected by `B4`.
Confidence is not reproduction, and the whole downstream chain — fix, verification, closure —
rests on the difference.

### N9 — Silence about the branch that was not closed

An alternative cause was considered, could not be eliminated, and simply does not appear.
Rejected by `V31`. An unrecorded branch is indistinguishable from a branch nobody thought of, and
the implementer proceeds as though the chain were closed.

### N10 — Deciding the gate

The artifact closes with a statement that the defect does not block the release. Rejected by
`A5`. That is the Triage Gate, decided by `tech-lead` over this evidence, and producing the
evidence is exactly what disqualifies this agent from deciding it.

## Judgement notes

Where the modules leave room, these are the defaults.

- **When the chain and the clock disagree, follow the chain.** Causal order is the artifact's
  order even where events were observed in a different sequence.
- **When two accounts fit equally, record both.** Choosing one because it is tidier is the
  failure `V14` exists to catch.
- **When evidence is second-hand, say so and rate it `low`.** Authority of the source does not
  raise it; only reading or running it in this run does.
- **When a section has nothing yet, say `None identified.`** A dropped section reads as an
  oversight, and a reader cannot tell whether it was considered.
- **When asked for the fix, give the strategy and name the owner.** The refusal is recorded in
  the artifact, so the boundary shows up as a routed handoff rather than as silence.
