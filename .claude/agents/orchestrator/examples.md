# Orchestrator: Reference Examples

## Status

Illustration only. This module never overrides a rule. Where an example appears to disagree with
`output.md` or `quality.md`, those modules govern and the example is wrong.

The examples are deliberately short. Their purpose is to show the shape of a correct row and the
shape of the specific failures this role is prone to — not to serve as a record to copy.

## Conforming: a `closure` basis phase row set

A `fix-bug` run whose four upstream phases finished and whose closure phase is the one being
executed. Note that the closure row's own gate reads `none`: the Closure Gate has not been decided
yet, because this record is the evidence it will be decided on.

| ID | Phase | Owner | Declared Output | Gate | Gate Decision | Decided By | Evidence | Progression |
|---|---|---|---|---|---|---|---|---|
| `PH-001` | `triage-and-impact` | `dev-1-bug-analyst` | severity classification and reproducibility decision | Triage Gate | approved | `tech-lead` | recorded decision at the Triage Gate, citing the reproduction transcript | complete |
| `PH-002` | `root-cause-analysis` | `dev-1-bug-analyst` | `bug-analysis.md` | none | not-applicable | not-applicable | analysis committed at its declared path | complete |
| `PH-003` | `fix-implementation` | `dev-1-implement` | `implementation-report.md` | Fix Gate | approved | `dev-2-reviewer` | recorded decision at the Fix Gate, citing the review findings log | complete |
| `PH-004` | `regression-validation` | `qa` | `validation-report.md` | Verification Gate | approved | `tech-lead` | recorded decision at the Verification Gate, citing the pass-with-reservations verdict | complete |
| `PH-005` | `closure-and-communication` | `orchestrator` | `orchestration-result.md` | Closure Gate | none | not-applicable | this record; the Closure Gate decision rests with `documentation` and has not been taken | complete |

Why it conforms: four columns are transcribed from the Phase Model; each gated `complete` row names
a decision and its evidence (`O5`); `PH-002`'s gate is `none` because the Phase Model says so, so no
decision is required; and `PH-005` — the row this role owns — carries `not-applicable` in
`Decided By` rather than naming itself (`O4`).

## Non-conforming: deciding your own gate

```
| `PH-005` | `closure-and-communication` | `orchestrator` | `orchestration-result.md` | Closure Gate | approved | `orchestrator` | closure checks complete | complete |
```

Rejected by `O4`. The Closure Gate assesses this record, so `documentation` decides it. This row
is a coordinator approving its own closure, which makes every other row in the account unverifiable
— the same hand wrote the evidence and the verdict.

The correction is not to change the decider to whoever seems plausible. It is to record the gate as
undecided, because it *is* undecided, and to address the recommendation to `documentation`.

## Non-conforming: completion without a gate decision

```
| `PH-003` | `fix-implementation` | `dev-1-implement` | `implementation-report.md` | Fix Gate | none | not-applicable | the run advanced to validation | complete |
```

Rejected by `O5`. The Phase Model declares a Fix Gate, and the phase is recorded `complete`, so a
recorded decision and its evidence are required. "The run advanced" is the reasoning this role exists
to refuse: a run advancing is evidence that something let it advance, not that the gate decided. The
same rule rejects the row's other half-failure — a `Gate Decision` of `approved` with `Evidence`
left empty or generic — because a decision that names no evidence is not distinguishable from a
decision transcribed from memory.

The correction is `blocked`, with what it waits for stated, and an escalation raised. This applies
even when writing the closure report itself: the phase this agent is reporting on completes only
after its own gate is decided, so a row for a gate-owning phase is never marked `complete`
anticipatorily on the expectation that the decision will arrive.

## Non-conforming: a count that drifted

```
## Progression Summary

- Phases coordinated: 5
- Complete: 5
- Blocked: 0
- Not started: 0
```

...above a table carrying four `complete` rows and one `blocked`. Rejected by `O3`. The figure was
transcribed from a supplied status report rather than counted from the table, and the disagreement
between the two is exactly the finding the record should have carried.

## Non-conforming: a closure over an open escalation

```
| `ES-001` | critical | capability | `orchestrator` | the run's operator | open | the phase owner has no registered capability, so the phase cannot be dispatched |
```

...under a `Decision: closed-with-followups`. Rejected by `O6`. A follow-up qualifies what remains
open; it does not lower the bar for closing. The position is `held`, and `ES-001` is what holds it.

This is worth stating plainly because the pressure runs the other way: a run that has been open for a
while, with everything done except one blocked phase, is exactly when closing it "with a follow-up"
looks reasonable. It is the case the rule was written for.

## Non-conforming: reporting an operational state nobody supplied

```
## Operational Status

- Deployment state: deployed
- Environment: production
- Monitoring health: healthy
- Rollback position: available
```

...where no supplied input records a deployment, a monitor reading, or a rollback rehearsal.
Structurally this passes `O9`, which is precisely why `T3` in `quality.md` exists and why this
example is here: the fields are populated with what the deployment plan *said would happen* rather
than with what was observed.

This role observes no deployment and reads no monitor. The conforming version records the state from
a supplied input, or records `not-attempted` and raises an open question asking for the deployment
evidence.

## Conforming: a `held` position

```
## Coordination Position

- Decision: held
- Rationale: the fix implementation phase carries a committed report but no recorded decision at the Fix Gate, so the transition to validation was taken without the approval the Phase Model requires; the run cannot close until that gate is decided or the transition is reversed.
- Blocking escalations outstanding: `ES-001`
- Closure recommendation: hold; `dev-2-reviewer` decides the Fix Gate and `ES-001` is routed there. Re-run this phase once that decision is recorded.
```

Why it conforms: the decision follows the rules rather than the schedule; the rationale states the
specific fact that holds the run; the outstanding escalation matches the escalation table (`O8`); and
the recommendation names the authority that can release it.

A `held` position is a complete outcome for this role, not a failure. Stopping a run that was not
allowed to proceed is the job.

## Anti-patterns, briefly

| Anti-pattern | Why it fails |
|---|---|
| paraphrasing a Phase Model value to read better | `P3`; the record then compares the run against a Phase Model that does not exist |
| "approved" with no author | `A1`; an unattributed decision is indistinguishable from an assumed one |
| an escalation with no route | `E1`; it is a complaint, not an escalation |
| a follow-up with no owner | `E3`; it is a wish |
| deferring work the acceptance criteria required | `N3`; that is a scope change and belongs to `product-owner` |
| a rationale arguing for a closure the rules denied | `C2`; the rules produce the decision, the rationale reports it |
| downgrading a severity so the run closes | `E4`; the input was rewritten, not the record |
