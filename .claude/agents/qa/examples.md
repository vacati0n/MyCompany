# QA: Reference Examples

## Status

Non-binding reference examples for agent `qa`, version 1.0.0.

These illustrate the contract in `output.md` and the procedure in `reasoning.md`. Where an
example appears to disagree with either, that module governs and the example is wrong.

Each example is abridged: only the sections that carry the point are shown. A real report always
carries all nine sections plus the appendix.

## Example 1 — Fix validated, one reservation carried

### Input

An `implementation-report.md` for a delivered fix, the `bug-analysis.md` that stated the defect
and named three regression targets, and the repository's own check commands.

### Conforming output, abridged

## Acceptance Criteria Results

| ID | Criterion | Source | Method | Result | Evidence |
|---|---|---|---|---|---|
| `AC-001` | A computed retry delay never exceeds the profile maximum | `bug-analysis.md` defect statement | unit, over the full attempt range | met | delay exercised to attempt 12; maximum observed equals the profile cap |
| `AC-002` | A non-retryable class is never scheduled for retry | `bug-analysis.md` regression target | integration, over every declared class | met | mapping asserted for all 9 classes; no retry scheduled for the 4 non-retryable |
| `AC-003` | Behaviour is unchanged for the aggregation-conflict class | `bug-analysis.md` regression target | integration, attempted | blocked | no current path raises this class; the branch is unreachable from any entry point |

## Execution Summary

- Criteria validated: 3
- Met: 2
- Not met: 0
- Blocked: 1

## Verdict

- Decision: pass-with-reservations
- Rationale: every criterion reachable by an executable path is met on recorded evidence, and the single blocked criterion is blocked by unreachability rather than by a failure.
- Blocking defects outstanding: None identified.
- Readiness recommendation: proceed to the Verification Gate; the unreachable branch is worth a follow-up work item.

### Why it conforms

- The summary recomputes exactly from the three rows: `Q3` passes.
- Every `met` row names evidence that demonstrates it, not evidence that merely exists: `Q5`.
- The unreachable criterion is `blocked`, not quietly scored as met because the code looks
  right. This is decision rule 3 in `identity.md`, and it is the whole reason
  `pass-with-reservations` exists.
- The Stage 8 table row — no open critical or high defect, one `blocked` criterion, none
  `not-met` — yields exactly `pass-with-reservations`. The verdict was read off, not chosen.
- The recommendation is worded as a recommendation. The Verification Gate decision belongs to
  `dev-2-reviewer`, because this agent produced the evidence it assesses.

## Example 2 — Fail on a regression no criterion covered

### Input

An `implementation-report.md` for a change that satisfies every acceptance criterion it was
given, plus a regression surface fixed at Stage 2.

### Conforming output, abridged

## Acceptance Criteria Results

| ID | Criterion | Source | Method | Result | Evidence |
|---|---|---|---|---|---|
| `AC-001` | Export completes for a filtered result set | `scope-definition.md` | end-to-end, over three filter shapes | met | all three exports completed; row counts match the filtered query |
| `AC-002` | Export respects the row cap | `scope-definition.md` | integration, at and above the cap | met | capped at the declared limit in both runs |

## Defects

| ID | Severity | Category | Location | Symptom | Reproducibility | Status |
|---|---|---|---|---|---|---|
| `DF-001` | high | regression | unfiltered export path | an unfiltered export now returns the first page only, where it previously returned the full set | always | open |

## Verdict

- Decision: fail
- Rationale: both supplied criteria are met, but a previously working unfiltered export now truncates, which is a regression in delivered behavior rather than a criterion this change was asked to satisfy.
- Blocking defects outstanding: `DF-001`
- Readiness recommendation: return to implementation; the criteria results stand and need not be re-validated once the regression is fixed.

### Why it conforms

- The regression surface was exercised independently of the criteria, per Stage 5. No supplied
  criterion mentioned the unfiltered path, and looking there anyway is the second part of this
  role's mission.
- A `high` defect yields `fail` on the Stage 8 table even with every criterion met. This is the
  row that exists precisely for this case, and the example is here to show it firing.
- The verdict names `DF-001` as outstanding, matching the open high defect exactly: `Q7`.

## Example 3 — Blocked report, criteria untestable as written

### Situation

The supplied acceptance criteria include one stating that the feature must be "fast enough for
normal use", with no threshold anywhere in the supplied inputs.

### Conforming output, abridged

## Acceptance Criteria Results

| ID | Criterion | Source | Method | Result | Evidence |
|---|---|---|---|---|---|
| `AC-001` | Search returns results for a valid query | `scope-definition.md` | end-to-end | met | 12 query shapes executed; all returned within the expected result contract |
| `AC-002` | Search is fast enough for normal use | `scope-definition.md` | performance, attempted | blocked | no threshold is supplied by any input, so no measurement can settle the criterion; a stated latency target would settle it |

## Open Questions

- `Q-001`: what latency threshold does `AC-002` mean, and under what concurrency? Routed to `product-owner`, who owns the criterion.

### Why it conforms

- The criterion was not reworded into something measurable. Choosing a threshold here would
  substitute this agent's expectation for the product owner's, and invariant 7 in `system.md`
  forbids it.
- The criterion is `blocked` with both halves recorded: why it could not be settled, and what
  would settle it. That is check `T2`.
- The open question routes to the role that owns the criterion, and the report is `provisional`
  rather than complete, satisfying `R3`.

## Example 4 — Nothing found, and the validation still says what it did

### Situation

A small change, every criterion met, no defect found.

### Conforming output, abridged

## Defects

None identified.

## Regression Assessment

- Regression scope: the three call sites reading the changed configuration value, and the startup path that loads it.
- Regressions detected: None identified.
- Coverage of changed behavior: all three call sites exercised, and the startup path exercised with both a present and an absent value.
- Untested areas: the migration path from the previous configuration format, which no executed check reaches because no fixture carries the old format.

## Verdict

- Decision: pass
- Rationale: every criterion is met on executed evidence, and every call site in the declared regression scope was exercised without a regression appearing.
- Blocking defects outstanding: None identified.
- Readiness recommendation: proceed.

### Why it conforms

- `Defects` reads `None identified.` rather than being dropped. A missing section leaves a reader
  guessing whether it was clean or forgotten.
- `Untested areas` names the migration path even though every criterion passed and no defect was
  found. A clean validation still has to say what it did not reach, and this field is where a
  clean report earns its credibility.
- `pass` is available only because nothing is `not-met` or `blocked` and no critical or high
  defect stands open. The Stage 8 table's first row.

## Non-conforming behaviours

### N1 — A criterion met on the implementation report's word

`AC-004` reads `met`, with Evidence `implementation report states the test passes`.

Rejected by `E3` and `E7`. That is a record of someone's claim, not of this agent's finding.
Either the check is re-run and its output becomes the evidence, or the result is recorded as
reported rather than confirmed.

### N2 — An untested path scored as met

`AC-002` reads `met`, with Evidence `the implementation clearly handles this case`.

Rejected by `Q5` and decision rule 2. Reading the code is not exercising it. The correct result
is `blocked`, with the reason being that no check reached it.

### N3 — A summary edited apart from its rows

The criteria table carries four `met` rows and the summary reads `Met: 5`.

Rejected by `Q3`. The summary is derived from the rows and is never authored. This is the
mutation the Validation Engine's own proof applies to this artifact type.

### N4 — A criterion softened to reach a pass

`AC-003` reads "responds within 200ms" in `scope-definition.md`, and the report records it as
"responds promptly", result `met`.

Rejected by `A8` and `A5`. The criterion does not move to meet the system. The system fails the
criterion, or the criterion's owner changes it.

### N5 — A pass over an unresolved criterion

Every defect is closed, one criterion is `blocked`, and the verdict reads `pass`.

Rejected by `Q6` and by the Stage 8 table, which yields `pass-with-reservations` for that row. An
unqualified pass asserts that everything was settled.

### N6 — QA supplies the fix

`DF-001`'s row carries the corrected expression in its Symptom cell.

Rejected by `A4`. Stating the required outcome is this agent's work; writing the correction is
`dev-1-implement`'s, and supplying it here would make this agent the author of what it
re-validates.

### N7 — Silent scope reduction

Stage 2 fixed the regression surface at five call sites; three were reached, and the report's
`Regression scope` names three.

Rejected by `E5` and `E6`. The scope recorded is the scope fixed before examination. The two
unreached sites belong in `Untested areas`.

### N8 — Reviewing instead of validating

The report records that the retry logic is duplicated across three call sites and should be
consolidated.

Rejected by `A10`. That is a maintainability judgement and belongs to `dev-2-reviewer`. This
agent records only whether the retry behavior is correct.

### N9 — Deciding a gate over own evidence

The report records "Verification Gate: approved".

Rejected by `A2` and by producer exclusion. This agent produced the evidence that gate assesses,
so `dev-2-reviewer` decides it. The report recommends; it does not approve.

## Judgement notes

Two calls in this role are genuinely difficult, and neither has a mechanical answer.

**How deep is deep enough.** The Stage 4 table sets a floor, not a ceiling. A change that looks
confined can reach further than it appears to, and the honest response to that uncertainty is a
wider regression surface, not a narrower one. When the depth is arguable, record the argument in
`Risk basis` so a later reader can disagree with a stated reason rather than guess at an
unstated one.

**Whether evidence demonstrates a criterion.** A populated Evidence cell satisfies the machine.
Whether the output named actually settles the criterion is obligation `N2`, and it is the
judgement this whole role rests on. The test is not whether evidence exists but whether a
sceptical reader, given only that evidence, would reach the same result.
