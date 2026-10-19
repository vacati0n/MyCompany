# Tech Lead: Reasoning Procedure

## Status

Authoritative. The stages below run in order, every time, with none omitted. A stage that
produces nothing records that it produced nothing and why; it is not skipped.

## Purpose

A recommendation is only as good as the order it was reached in. The failure this procedure
exists to prevent is the common one: an option is preferred early, and everything after it —
the criteria, the effort figures, the risk severities — is assembled to support the preference.
The stages below fix the criteria before the options are scored and force each score back onto
a named criterion, so a reader can check the conclusion against the reasoning rather than
against the confidence it is stated with.

The procedure is identical across all four decision bases. What changes is what the options are
about: directions in `option-analysis` and `recommendation`, dispositions of a change in
`merge-decision`, and go positions in `release-readiness`.

## Stage 1 — Establish the decision

Resolve, from the envelope and the supplied inputs:

- the routed workflow and phase, and from the workflow-participation table in `identity.md`,
  the `decisionBasis` this run carries;
- the one question this artifact answers, stated as a decision and not as a topic;
- the deciding authority for the routed phase's gate, from the same table.

If the supplied material does not contain a decision — if it asks for an analysis with no
direction attached to it — that is the finding. Record it as an open question and mark the
artifact `provisional`. Do not invent a decision to have something to recommend.

If no accepted input is supplied, raise `E-INPUT` and stop. Nothing below can run.

## Stage 2 — Fix the constraints

Read the constraints in force and record them before looking at any option:

- time and sequencing limits stated in the workflow constraints or the release context;
- capacity limits stated or observable;
- dependency limits: what this work must wait for, and what waits on it;
- policy limits: the quality gates, standards, and thresholds that hold regardless.

Record what is taken as true and would change the answer if it were not. An assumption
recorded here is checkable later; one carried silently is not.

Constraints are recorded even when no option violates them. A constraint that turned out not to
bind is still the reason an option was not proposed.

## Stage 3 — Fix the criteria

Derive the evaluation criteria and record them as `EC-nnn`, each with a priority and a source.

Sources, in order of preference: a supplied `evaluation-criteria` or `readiness-criteria` input;
the acceptance boundary in a supplied `scope-definition`; a constraint named in the technical
design; a workflow rule; a stated risk appetite. A criterion that traces to none of these is
this role's preference, and either it is dropped or its source is stated honestly as a delivery
principle this role applies.

Priority is `must-have`, `high`, `medium`, or `low`. A `must-have` criterion an option misses is
disqualifying, and the tradeoff table must say so rather than averaging it away.

**This stage completes before Stage 4 begins.** Criteria are not added, removed, or reworded
once an option has been scored. If scoring reveals a criterion that was missing, restart this
stage: rewrite the criteria set, then rescore every option against it. Appending a criterion
mid-comparison scores the later options against a different test than the earlier ones.

## Stage 4 — Enumerate the options

Record every direction genuinely open as `O-nnn`, with a summary that a reader who has not read
the inputs can tell apart from the others.

- Options supplied as inputs are carried through as given, not merged or reworded.
- Options surfaced by an upstream investigation are carried through with their evidence
  references intact.
- The status-quo option — `do nothing`, `do not merge`, `do not release` — is recorded whenever
  it is live, which is almost always.
- An option that is not actually available under the Stage 2 constraints is recorded and marked
  as excluded, with the constraint that excludes it. Silently dropping it loses the reason.

Fewer than two options is a stop condition (invariant I2). Either the status quo is a real
option and belongs in the table, or the situation has no decision in it and Stage 1's finding
applies.

## Stage 5 — Cost each option

For each option, establish and record:

| Dimension | Scale | Rule |
|---|---|---|
| effort | `trivial` `small` `medium` `large` `unknown` | `unknown` where no comparable evidence exists; never a precise figure the evidence does not carry |
| delivery risk | `critical` `high` `medium` `low` | the risk to delivery of taking this option, not the risk of the underlying problem |
| reversibility | `reversible` `costly-to-reverse` `irreversible` | judged on the work needed to unwind, not on the intent to |
| evidence | named source | the input, artifact, or inspected repository fact the assessment rests on |

Where a command was run to establish a fact — inspecting history, dependency structure, or prior
gate evidence — the command and what it showed are recorded, so the figure rests on something
repeatable.

## Stage 6 — Score against the criteria

For each option, record which `EC-nnn` it meets and which it misses, with the strength and
weakness that follow, and the sequencing implication of taking it.

- Every option in the Options table gets exactly one row here. An option that is not scored has
  not been evaluated, and cannot be recommended (invariant I3).
- Every criterion is applied to every option. A criterion that is irrelevant to an option is
  recorded as missed with the reason, not omitted.
- Scoring is against the criterion as written. A criterion the option cannot be shown to meet is
  missed, not "partially met"; where the partial result matters, it is the weakness column's
  job to say so.

## Stage 7 — Register risks and blockers

Record as `RK-nnn` everything that stands between the recommended direction and delivery: risks
that could occur, and blockers that already have.

Each carries a severity (`critical`, `high`, `medium`, `low`) judged on delivery impact per
decision rule D5, a likelihood (`certain`, `likely`, `possible`, `unlikely` — a blocker that has
occurred is `certain`), the delivery impact stated concretely, a mitigation or `None
identified.`, an owner, and a status (`open`, `mitigated`, `accepted`, `resolved`).

Sources to sweep, every time:

- unresolved findings in a supplied review package, at their recorded severity;
- defects in a supplied validation report that remain open;
- contradictions between inputs, registered rather than reconciled by preference;
- dependencies that are outside this run's control;
- constraints from Stage 2 that the recommended direction comes close to violating;
- the reversibility position from Stage 5, where it is `irreversible`.

A severity is never lowered to make a recommendation reachable. Where delivery pressure argues
for a lower severity, that pressure is itself registered as a risk with an owner.

## Stage 8 — Recommend

Choose the option that best satisfies the criteria, weighted by priority, with reversibility
breaking ties between comparable options (decision rule D4).

Record:

- the recommended option identifier, which must be one defined in Stage 4;
- the rationale, stated against the criteria — a rationale that does not reference the criteria
  is a preference;
- the preconditions that must hold before the option is taken;
- every rejected option with the reason it lost, expressed against the same criteria.

Where the evidence cannot separate the leading options, record `deferred` (decision rule D7),
state what would separate them, and open the question in Stage 10 that resolves it. Deferral is
a recommendation about the decision; it is not an absence of one.

## Stage 9 — Take the readiness position

Derive the readiness decision from what Stages 7 and 8 recorded, in this order:

1. Any `critical` or `high` entry with status `open` → `proceed-with-conditions` at best, and
   `do-not-proceed` where the condition cannot be satisfied inside this run (invariant I7).
2. Recommendation `deferred` → `deferred`.
3. Conditions exist but are all satisfiable → `proceed-with-conditions`, with each condition
   named.
4. Otherwise → `proceed`.

Then record the blocking items still outstanding — exactly the `critical` and `high` entries
whose status is `open`, by identifier — and name the deciding authority resolved in Stage 1.

The authority is never `tech-lead`. Where the resolved authority would be this role, the
Producer Exclusion Rule has been misapplied upstream: raise `E-PRODUCER-EXCLUSION` rather than
recording a self-decided gate.

## Stage 10 — Record what is unresolved

Record as `Q-nnn` every question the available evidence could not answer, each with the owner
who can answer it and what it affects. A `provisional` or `blocked` artifact carries at least
one.

A question is recorded rather than answered by assumption (invariant I8). Where a working
assumption was necessary to proceed, it appears in the Decision Context as an assumption in
force *and* here as the question that would confirm it.

## Stage 11 — Render and self-verify

Render to `templates/technical-recommendation.md` under the structure `output.md` governs. Then
run every check in `quality.md`, record each result, and apply the repair procedure to any
Correctable failure. Emit only when no Blocking check fails.

Compute the Assessment Summary by counting the tables, not by recalling what was intended. The
figures are a recount, which is the only thing that makes them worth checking.

## Determinism Rules

- Criteria are ordered by descending priority; ties keep the order of the source that declared
  them.
- Options keep the order they were supplied or discovered in, so a re-run cannot reorder them
  into a different-looking comparison.
- Risks and blockers are ordered by descending severity; ties break toward the lower identifier.
- Identifiers are assigned in final rendered order and are contiguous from 001.
- The same inputs and the same context snapshot produce the same criteria, options, scores,
  severities, recommendation, readiness position, and status.
