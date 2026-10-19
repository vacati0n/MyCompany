# Documentation: Reference Examples

## Status

Non-binding reference examples for agent `documentation`, version 1.0.0.

These illustrate the contract in `output.md` and the procedure in `reasoning.md`. Where an example
appears to disagree with either, that module governs and the example is wrong.

Each example is abridged: only the sections that carry the point are shown. A real artifact always
carries all seven sections.

## Example 1 — A clean release that still says what it does not cover

### Input

An `implementation-report.md` for a delivered change, a `validation-report.md` with verdict
`pass-with-reservations` carrying one blocked criterion, and a deployment status recording that
every change in scope reached the shared runtime host.

### Conforming output, abridged

## Validation Summary

- Test status: validated by `qa` in `validation-report.md` with verdict pass-with-reservations; nine criteria met, one blocked because no current path reaches the aggregation-conflict branch.
- Known risk acceptance: the unreachable branch was accepted as residual risk by the tech lead.
- Post-release checks: confirm the retry ledger records one entry per classification and no duplicates.

## Known Issues

| ID | Issue | Impact | Workaround | Tracking |
|---|---|---|---|---|
| `K-001` | the aggregation-conflict branch has no reachable path and so was never exercised | a caller who reaches it by a future route gets untested behavior | none; the branch is currently unreachable | recorded as `Q-001` in `validation-report.md` |

### Why it conforms

- The validation result is quoted with its verdict and its source named, not summarised as
  "validated": check `A7`.
- The blocked criterion becomes a known issue rather than disappearing because the verdict was
  still a pass. That is `K1`, and it is the whole reason a qualified pass is published as
  qualified.
- The impact states what happens to a person, not the word `low`: check `K2`.
- The workaround states that none exists, which is a complete answer. An empty cell would fail
  `K3`.

## Example 2 — A breaking change in a release that otherwise reads well

### Input

An `implementation-report.md` declaring a changed request contract, an `architect`-supplied
compatibility statement, and a deployment status recording a full release.

### Conforming output, abridged

## Highlights

- Feature additions: batch submission accepts up to 500 items per request, where it previously accepted 50.
- Bug fixes: a rejected item no longer silently drops the rest of its batch.
- Improvements: None identified.

## Technical Changes and Compatibility

- API or contract changes: the batch endpoint now rejects a request carrying the `legacyMode` flag, which was previously ignored.
- Database or migration impact: none; no stored shape changed.
- Configuration changes: none required.
- Backward compatibility notes: a caller still sending `legacyMode` receives a validation error where the flag was previously ignored, so every such caller must remove the flag before upgrading; the supplied architecture statement records no fallback path.

### Why it conforms

- The breaking change is in the note, in the section a reader checks before upgrading, not in a
  footnote under the feature that motivated it: checks `X1` and `A1`.
- The compatibility statement says what the caller observes and what they must do: check `X2`.
- It is taken from the supplied architecture statement rather than reasoned out here, and it
  records that no fallback exists rather than suggesting one: check `X3`.
- The larger batch limit is a feature and the dropped-item fix is a fix. Neither absorbs the
  breaking change into a friendlier framing.

## Example 3 — A partial release, published as partial

### Situation

The deployment status records that two of four changes reached the target environment and the
remaining two were held when a dependency check failed mid-deployment.

### Conforming output, abridged

## Known Issues

| ID | Issue | Impact | Workaround | Tracking |
|---|---|---|---|---|
| `K-001` | the export scheduler change did not deploy; the prior scheduler is still running | exports still run on the old hourly cadence rather than the new configurable one | configure the cadence at the caller until the change lands | tracked by the orchestrator as a held work item |
| `K-002` | the notification template change did not deploy and depends on the scheduler change | notification timing continues to match the old cadence, which is consistent but not what the release announced | none needed; the two are consistent while both are held | tracked with `K-001` |

## Operational Notes

- Deployment considerations: the two held changes must deploy together; deploying the notification change alone would announce a cadence the scheduler does not implement.
- Monitoring and alerts: the scheduler version is the signal that tells an operator which cadence is actually running.
- Rollback criteria: revert the two landed changes if export volume diverges from the prior baseline, since the mixed state was not validated as a target configuration.

### Why it conforms

- The verdict is `partial` because the Stage 8 table yields it from the supplied deployment
  status, and `R4` requires the known issues that explain it.
- The second issue is published even though it is a consequence of the first and causes no visible
  problem while both are held. An operator who deploys one of them needs to know: check `K6`.
- The rollback criteria describe the actual mixed state rather than restating a generic policy:
  check `R5` and the `Rollback criteria` contract in `output.md`.

## Example 4 — A provisional note, compatibility consequence unstated

### Situation

The implementation report declares that a stored date field changed from local time to UTC.
Nothing supplied states what an existing consumer of that field will observe.

### Conforming output, abridged

## Technical Changes and Compatibility

- API or contract changes: the `completedAt` field on the run record is now stored in UTC, where it was previously stored in the host's local time.
- Database or migration impact: existing rows retain their original values; no migration is declared by any supplied input.
- Configuration changes: none declared.
- Backward compatibility notes: not established. No supplied input states what a consumer reading `completedAt` across the change boundary will observe, or whether existing rows are to be interpreted as local. Routed to `architect` as `Q-001`; this note is provisional until it is answered.

### Why it conforms

- The change is published, because it is declared by the evidence and a consumer needs to know it
  happened.
- The consequence is not published, because no input states it. Writing "existing rows are
  unaffected" would be the easiest sentence here and the most damaging: it would be checked by
  nobody and planned against by everyone. That is exactly `X3`.
- The artifact declares itself `provisional` and carries the open question naming the role that
  owns the answer: checks `V2` and `V3`.

## Non-conforming behaviours

### N1 — A validation verdict strengthened in the retelling

The validation report records `pass-with-reservations` with one blocked criterion. The note reads
"fully validated". This publishes a stronger claim than the evidence supports and takes a
judgement `qa` holds. Caught by `A7`.

### N2 — A breaking change demoted because a workaround exists

The compatibility notes read "callers can simply remove the flag", and the change is filed under
Improvements. A workaround is published alongside a breaking change, never instead of it. Caught
by `X4` and `A1`.

### N3 — A compatibility statement invented at publication time

No supplied input states the consequence, so the note reasons one out from the change description
and states it plainly. It reads like diligence and functions as an unapproved design position.
Caught by `X3`.

### N4 — A known issue omitted as minor

A low-severity defect the validation report left open is dropped, on the grounds that no user will
notice. Whether a user notices is not this agent's judgement to make silently, and the reader who
does notice has no way to know it was already known. Caught by `K1` and `K6`.

### N5 — Prior-release wording carried forward

The Operational Notes are copied from the last release because the deployment process "did not
really change". The delivered change added a required configuration value, so the copied text is
now false. Caught by `A4`.

### N6 — Internal work published as a feature

A thin release is padded by listing a refactor of an internal module under Feature additions. No
user can reach it, so it overstates the change. Caught by `A5`.

### N7 — A gap resolved in prose

The implementation report and the validation report disagree about which environments a change
covers. The note picks the wider one and writes a smoothing sentence. The disagreement is real and
belongs to the roles that produced the two artifacts. Caught by `P5` and `A9`.

### N8 — A fix described by its code

"Corrected the boundary condition in the retry delay calculation" tells a reader who never saw the
source nothing. What was wrong for them was that retries stopped after a burst. Caught by `A2`.

### N9 — The release decision restated as the note's own

The note reads "we have decided to ship this partially". The decision belongs to `tech-lead`
and the note communicates it, attributed. Caught by `A8`.

### N10 — Deciding a gate over its own package

The Communication Gate assesses the package this agent wrote, and this agent records its approval.
Producer exclusion moves that decision to `product-owner`. Caught by `A13`.

## Judgement notes

Three judgements recur, and none of them is settled by any check in `quality.md`.

**What counts as user-visible.** The Stage 5 table classifies a change by whether anyone outside
the run can reach it. The hard cases are changes that alter timing, ordering, or error text
without altering the contract. The test that works: would a reader who has already built something
against this system behave differently if they knew? If yes, it is visible.

**How much a support engineer needs.** Support handoff notes fail in both directions — empty, or a
second copy of the technical section. The content that earns its place is what someone will
misdiagnose without it: the state that looks broken and is not, the error that means something
other than what it says.

**When to say less.** Where the evidence is thin, the instinct is to write around it. The contract
requires the opposite. A short accurate sentence with an open question beside it is worth more
than a full paragraph a reader cannot rely on, and it is the only version that stays true.
