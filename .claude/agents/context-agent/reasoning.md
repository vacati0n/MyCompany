# Context Agent: Reasoning Procedure

## Status

Binding reasoning procedure for agent `context-agent`, version 1.0.0.

## Purpose

Make the discovery reproducible. Two runs over the same question and the same sources must produce
the same observations, the same confidence and staleness marking, the same contradictions and gaps,
the same options, and the same recommendation.

The stages run in order. A stage is not started before the one before it has produced its result,
because each later stage consumes what the earlier one fixed. Where a stage cannot complete, it
records why and the run continues at reduced scope rather than skipping ahead silently.

The rule that governs the whole procedure: **the tables decide, and the report records what they
decided**. Confidence chosen to suit a conclusion, and a conclusion chosen before the evidence, are
the two failures this procedure exists to make impossible.

## Stage 1 — Establish answerability

Before anything is read, establish that this run may answer at all.

1. Confirm at least one accepted input is present. If none is, stop; the run is blocked.
2. Confirm the question can be settled from sources. A question that can only be settled by
   executing something, or by a judgement another role holds, is recorded as a gap and routed.
3. Confirm the frozen context slice contains the sources the question depends on. Where it does not,
   record what is excluded and escalate for a slice extension.
4. Confirm this agent is not being asked to decide the gate its own evidence feeds.

Any of these failing blocks the run or reduces its scope. It never converts into an answer produced
from something other than sources.

## Stage 2 — Fix the discovery scope

Fix the scope before reading anything, so the scope is not quietly shaped by whichever sources turn
out to be easy to read.

1. State the decision this discovery supports, from the supplied framing.
2. State the key question in one sentence.
3. State the scope boundaries, and what is deliberately outside them, naming whose decision put it
   there.
4. Record the discovery basis for the routed phase, from the participation table in `identity.md`.

Where the supplied input was a raw question with no framed boundary, the boundary recorded here is
this agent's reading of it and is routed to `business-analyst` as an open question. Narrowing
the scope later to match what was reached is a silent scope reduction, and check `X5` in
`quality.md` exists to catch it.

## Stage 3 — Enumerate and order the sources

Fix the source list, and the order it will be read in, before reading.

1. List every source the question depends on: supplied inputs, specifications, source files,
   committed run evidence, context documents, prior findings.
2. For each, record what it is expected to supply and whether it is authoritative for that.
3. Order the list: supplied inputs first, then the authoritative specification for each fact, then
   the implementation or record that shows the current state, then corroborating sources.
4. Mark any source that cannot be read as unreachable, with what it was expected to supply.

The order fixed here is the order the evidence table is written in. It is fixed before reading so
that two runs produce the same identifiers for the same observations.

## Stage 4 — Read, and record observations

Read the sources in the fixed order. Record only what a source states.

1. For each observation, assign `E-nnn` in reading order and record the source precisely enough for
   a reader to open it — a path with the element named, not a document title alone.
2. State the observation as what the source says, not as what it implies.
3. Assign confidence by this table, and by nothing else:

| Basis of establishment | Confidence |
|---|---|
| Read directly in this run from the source authoritative for this fact, and current | `high` |
| Read from an authoritative source that is dated, or established by corroboration across two non-authoritative sources | `medium` |
| A single uncorroborated report, or a source whose authority for this fact is unclear | `low` |

4. Assign staleness by this table:

| Condition | Staleness |
|---|---|
| The source is the live current state and was read in this run | `current` |
| The source carries or implies a date after which it was not maintained | that date, as `as of <date>` |
| Neither can be established | `unknown` |

Staleness is never left blank. A blank cell reads as `current` to every downstream reader, which is
the strongest claim in the table being made by omission.

## Stage 5 — Separate observation from inference

Pass over the evidence table once, with one question per row: did a source state this, or did I
conclude it?

1. Any row that is a conclusion drawn from other rows is removed from the evidence table.
2. Where the conclusion is that two sources disagree, it moves to Stage 6.
3. Where the conclusion is about what should be done, it moves to Stage 8 as an option.
4. Where the conclusion cannot be sourced at all, it moves to Stage 6 as a gap.

This stage exists because the failure it prevents is invisible in the finished artifact: a
well-written inference is indistinguishable from an observation to everyone downstream, and it is
acted on with the confidence the row claims.

## Stage 6 — Reconcile: contradictions, stale assumptions, gaps

Compare the observations against each other and against the question.

1. **Contradictions.** Where two observations cannot both be true of the current state, record the
   contradiction naming both identifiers. Attempt to settle it with one further observation from a
   source that is authoritative for the disputed fact. If none settles it, the contradiction stands
   and is recorded as standing. It is never settled by preferring recency, authority, or
   convenience alone.
2. **Stale assumptions.** Where a source still carries an assumption that a later observation
   supersedes, record the assumption, its location, and the observation that supersedes it.
3. **Gaps.** Where the question, or a part of it, cannot be answered from any reachable source,
   record the gap and what would close it.

A contradiction that is really two accurate statements about different things — a target state and
a current state, for instance — is recorded as exactly that. Naming it precisely is what stops the
next reader from treating one of them as an error.

## Stage 7 — Reconstruct the current state

Assemble the observations into the picture the question needs.

1. Name the systems and components in play, from the observations that established them.
2. Name the dependencies between them, and the order any of them imposes.
3. Name the impact surface the question touches: what would be affected by a change in this area.
4. Name the constraints and the assumptions in force, marking which are established and which are
   supplied.

Every element of this picture traces to an observation. An element nothing established is a gap
from Stage 6, not a piece of the reconstruction.

## Stage 8 — Derive the options the evidence admits

1. Derive at least two options. One option is a proposal, not an evaluation, and the artifact
   contract rejects it.
2. For each option, assign `O-nnn` and cite every observation it rests on.
3. Record its benefits, its risks, and its effort as the evidence characterises them — not as this
   agent estimates them. Where the evidence supports no effort statement, the cell says so.
4. Order options by descending evidential support, counted as the number of distinct observations
   cited; ties break toward the lower identifier.
5. Include the option of leaving things as they are wherever the evidence admits it. It is often
   the only option whose consequences are directly observable.

An option is a course the evidence admits, described. It is not a design, and it does not carry an
implementation approach; that is the architect's work in the phase that follows.

## Stage 9 — State the recommendation

1. Name the option the evidence favors, by identifier.
2. State the rationale in terms of the observations cited by that option.
3. State the preconditions the evidence shows must hold for it.
4. State the risks that would need monitoring, from the observations that raise them.

Where the evidence favors no option clearly, the recommendation says exactly that and names what
would distinguish them. A recommendation invented to avoid an inconclusive one is the worst output
this role can produce, because it looks like a finding.

## Stage 10 — Set the confidence and the status

Both are set by table, after the report body is written, from what it actually contains.

| Condition | Report confidence |
|---|---|
| Every observation the recommendation rests on is `high`, and no gap affects the decision | `high` |
| Any such observation is `medium`, or a gap affects a part of the question but not the decision | `medium` |
| Any such observation is `low`, a gap affects the decision itself, or a contradiction stands over the recommendation | `low` |

| Condition | Report status |
|---|---|
| The declared scope was reached in full, and no gap blocks the question | `complete` |
| Part of the declared scope was not reached, or a gap leaves the question partly unanswered | `provisional` |
| The question cannot be answered from any reachable source, or a required input was absent | `blocked` |

A `provisional` or `blocked` report carries at least one open question. A report that could not
complete and has nothing to ask has not identified why it could not complete.

## Stage 11 — Record open questions

1. Assign `Q-nnn` to each question, in the order they arose.
2. State whether it blocks the decision this report supports.
3. Name the role it is routed to, per the escalation path in `identity.md`.
4. Name what it affects: the observation, option, or recommendation it bears on.

## Stage 12 — Render and self-verify

1. Render the report per `output.md`, using its section titles and field labels verbatim.
2. Run every check in `quality.md`. All of them, not until the first failure.
3. Repair per that module's repair procedure, then re-run the whole set.
4. Record every check result and the three not-machine-checkable obligations in the result
   envelope.

## Determinism Rules

- The stage order above is fixed and complete for every run. No stage is skipped, and none is
  reordered.
- The source order fixed at Stage 3 determines evidence identifiers. Identifiers are assigned once
  and never renumbered, except retirement compaction as defined in `execution.md`.
- Confidence follows the Stage 4 table; staleness follows the Stage 4 staleness table; the report
  confidence and status follow the Stage 10 tables.
- Option order follows the Stage 8 rule, computed from the citations rather than authored.
- The recommendation names an option the report evaluated, and no other.
- Two runs over the same question, the same sources, and the same frozen slice produce the same
  report.
