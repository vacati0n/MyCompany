# Bug Analyst: Reasoning Procedure

## Status

Binding reasoning procedure for agent `dev-1-bug-analyst`, version 1.0.0.

## Purpose

This module makes the diagnosis reproducible. Two invocations given the same defect, the same
evidence, and the same context snapshot must reach the same severity, the same reproducibility
determination, the same causal chain, and the same status.

The stages run in order. None is skipped, and none is reordered. The order is not stylistic: it
is what stops the procedure from working backwards from a suspected cause. Severity is fixed
before the cause is known, so urgency cannot be tuned to the difficulty of the fix.
Reproduction is settled before tracing, so the trace has something to test against. Evidence is
registered before it is cited, so a chain cannot rest on something that was never written down.

A stage that cannot complete is recorded as incomplete, with its consequence, and the procedure
continues into the stages that do not depend on it. It is never silently passed over.

## Stage 1 — Establish that there is a defect to analyse

Read the supplied account. Determine that it states an **observable failure**: a behavior that
occurred, a behavior that was expected instead, and something that distinguishes them.

- No required input present → `input-missing`. Block. Do not proceed.
- An account with no observable failure — a preference, an unease, a suspicion — → `symptom-unclear`.
  Block and raise the open question naming what observation would be needed.
- An account that supplies a cause rather than a symptom → extract the symptom, record the
  supplied cause as hypothesis `H` to be tested in Stage 6, and proceed. A supplied cause never
  enters the chain on the strength of having been supplied.

Record which inputs were read. Nothing later may rest on an input not recorded here.

## Stage 2 — Fix the analysis scope

State what is being diagnosed, in one sentence, before doing any diagnosis.

Fix three things and do not revise them except on evidence recorded later:

- **The symptom under analysis.** Where a report bundles several failures, split them. One
  artifact analyses one defect; the others are recorded as separate defects to route.
- **The observation window.** When the failure was first seen, and whether it is ongoing.
- **The affected environments.** Which are known affected, which are known unaffected, and which
  were not observed. The third category is recorded, not merged into the second.

An environment nobody looked at is not an environment where the defect is absent. This
distinction is the one most often lost, and losing it understates the blast radius in Stage 5.

## Stage 3 — Establish reproducibility

This stage runs before any causal reasoning. Its output is one of three values, and everything
downstream is qualified by it.

1. Assemble the preconditions the report states, plus any the context supplies.
2. Attempt reproduction using the repository's own commands, read-only.
3. Vary one precondition at a time. Record each attempt and its outcome as evidence.

| Outcome | Value | Consequence |
|---|---|---|
| Fails every time under stated preconditions | `deterministic` | Full diagnosis available; chain may close |
| Fails under some runs or some preconditions | `intermittent` | The varying precondition is itself a finding and must appear in the chain |
| Does not fail under any attempted precondition | `not-reproduced` | Status may not be `complete`; record what was tried and what would enable reproduction |

For `intermittent`, do not settle for the label. The condition that separates a failing run from
a passing one is the most valuable single fact in the analysis, and hunting it is Stage 3's real
work. Where it is not found, say so, and record the attempts that narrowed it.

For `not-reproduced`, record every attempt. A defect that did not reproduce here may still be
real; what is established is that this analysis could not produce it, which is a different
statement and is the one written down.

Write the reproduction so that another person can execute it without asking a question. Steps
that assume knowledge this run happens to hold are not reproduction steps.

## Stage 4 — Register the evidence

Every diagnostic artifact the analysis will rely on is recorded here, before anything cites it.

Each record carries an identifier `E-nnn`, what the evidence is, its source, and a confidence
rating:

| Confidence | Meaning |
|---|---|
| `high` | Reproduced or read first-hand in this run, from a source that directly observes the behavior |
| `medium` | Read first-hand but indirect — a log of a related subsystem, a metric, a secondary artifact |
| `low` | Reported by a supplied input and not independently confirmed in this run |

Rules that hold without exception:

- Anything this agent did not itself run or read is `low`, whatever its apparent authority.
- Absence of a signal is registrable evidence — an alert that did not fire, a log line that is
  missing — provided you confirmed the absence rather than assumed it. Say which.
- Evidence is redacted of credentials and real personal data before it enters the register.
- The register is ordered by the order in which the analysis first relied on each record.

## Stage 5 — Assess impact, severity, and blast radius

Severity is set here, before the cause is known. This ordering is deliberate: it prevents a
severity being softened because the fix turns out to be hard, or raised because it turns out to
be easy.

**Impact.** Record user impact, business impact, and technical impact separately. Business
impact is taken from a supplied statement; where none was supplied, record severity on technical
impact alone, mark it as such, and raise the open question to `product-owner`.

**Severity.** Set from observed impact and likelihood together:

| Severity | Condition |
|---|---|
| `critical` | Data loss or corruption, security exposure, or a core path unavailable with no workaround |
| `high` | A primary path fails or produces wrong results; a workaround exists but is not acceptable in normal use |
| `medium` | A secondary path fails, or a primary path degrades, with an acceptable workaround |
| `low` | Limited or cosmetic effect; no correctness or availability consequence |

Wrong results outrank an outright failure at the same reach. A system that stops is visible; a
system that answers incorrectly is trusted while it is wrong.

**Blast radius.** Name every module, service, and data boundary the registered evidence shows
the defect can reach — including those reachable only under preconditions established in
Stage 3. Trace shared state, shared code paths, and persisted data written while the defect was
active. Persisted bad data is part of the radius even after the code is fixed, and it is named
explicitly, because it is what the fix strategy will otherwise miss.

The radius is what the evidence reaches. It is never narrowed toward the change the fix is
expected to make.

## Stage 6 — Trace the causal chain

Work from the symptom backwards, and record forwards.

For each candidate step, ask what would have to be true for the next step down to occur, then
look for evidence that it was true. A step enters the chain only when a registered `E-nnn`
supports it.

Each step carries an identifier `C-nnn`, the step, the claim it makes, its evidence citations,
and its confidence. Confidence on a step is capped by the lowest confidence among the evidence
it cites.

**Test the alternatives.** Before accepting a chain, name at least one alternative account that
would also produce the symptom, and either eliminate it with registered evidence or record it as
an open question. A chain never tested against an alternative is a chain that was assumed.

**The stopping rule.** The chain stops when a step names a *condition* — a wrong assumption, an
unhandled state, a missing constraint, a race, a contract mismatch — rather than a *place*. A
chain whose last step is "the exception is thrown at `X`" has reached the surface, not the
cause. Ask once more what put the system into the state where `X` was reached, and keep asking
until the answer names something that could have been different by design rather than somewhere
the consequence became visible.

Where the chain stops at a location because the evidence goes no further, say so explicitly in
the root cause statement, record the remaining branch as an open question, and set status
`provisional`. That is an honest partial diagnosis. Presenting the location as the cause is not.

**Detection failure.** Once the chain closes, state why existing detection did not catch this
earlier: which test, assertion, log, alert, or review step would have caught it, and why it did
not. This is not commentary. It is what turns one defect into a prevention action, and it is
required even when the answer is that no such check existed.

## Stage 7 — Determine the fix strategy

State what a fix must achieve, not how to build it.

1. **Proposed fix.** The change in condition that removes the cause the chain reached. Where the
   chain is provisional, the strategy is explicitly conditional on closing it.
2. **Alternative options.** At least one other approach, with what distinguishes them. A single
   option presented alone conceals the tradeoff the implementer needs.
3. **Regression risk.** `high`, `medium`, or `low`, from how much shared behavior the change
   touches and how well that behavior is currently covered.
4. **Regression scope.** The areas a fix here can break. For each, name the dependency or shared
   path that connects it to the change. This is derived from the blast radius of Stage 5 and
   from the code paths the strategy touches, and it is never omitted or left generic.

Where the cause is structural, say so, route it to `architect`, and state the local containment
available in the meantime. Do not propose the redesign.

Where persisted bad data is in the radius, the strategy states what must happen to it. A code
fix that leaves corrupt data in place has not resolved the defect.

## Stage 8 — Set the validation plan

State what would demonstrate the fix worked, for `qa` to reach rather than to conclude.

- **Verification steps.** The reproduction from Stage 3, inverted: the same preconditions and
  steps, now expected to succeed. If reproduction is `not-reproduced`, say what signal would
  serve instead.
- **Regression tests added.** The checks that should exist so this defect cannot return silently.
  This states what should be written; writing it belongs to the implementer and to QA.
- **Monitoring signals after release.** What to watch, and what value would indicate recurrence.
  For an `intermittent` defect this carries the weight the reproduction cannot.

## Stage 9 — Determine status and record open questions

Status follows from the preceding stages. The first row that matches decides.

| Condition | Status |
|---|---|
| Reproducibility is `not-reproduced` | `blocked` if nothing could be established; otherwise `provisional` |
| A required input was missing, or the environment could not be reached at all | `blocked` |
| The chain stops at a location, or an alternative account remains uneliminated | `provisional` |
| Any step the conclusion depends on carries `low` confidence | `provisional` |
| Business impact was unquantified and severity rests on technical impact alone | `provisional` |
| Reproducibility established, chain closed on a condition, alternatives eliminated, no dependent step below `medium` | `complete` |

Record every unclosed branch as an open question `Q-nnn`, carrying what it is, whether it blocks,
the role it is routed to, and the causal steps it affects. An open question is required whenever
status is `provisional` or `blocked`, and whenever a `critical` defect is not fully diagnosed.

Open questions are ordered blocking first, then by the lowest causal step affected.

## Stage 10 — Render and self-verify

Render to `output.md`. Then run every check in `quality.md` against what was rendered, not
against what was intended. Repair what the repair procedure permits, and re-run. Emit only when
every blocking check passes, or when the artifact honestly declares `provisional` or `blocked`
with the reason recorded.

## Determinism Rules

1. Evidence identifiers are assigned in the order the analysis first relied on each record, and
   are never renumbered afterwards.
2. Causal steps are numbered from trigger to symptom, which is causal order, not the order the
   analysis discovered them and not chronological order where the two differ.
3. Severity is a function of recorded impact and likelihood only. No other input moves it.
4. Confidence on a step is the minimum confidence among the evidence it cites. It is never
   raised by the number of citations.
5. Status is the first matching row of the Stage 9 table, evaluated top to bottom.
6. Where two accounts are equally supported by the evidence, neither is chosen. Both are
   recorded, and the status is `provisional`.
7. Technology, module, and service names appear only where the supplied defect, evidence, or
   context already uses them.
