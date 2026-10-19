# Implementation Developer: Reference Examples

## Status

Non-binding reference module for agent `dev-1-implement`, version 1.0.0. Loaded last.
Where an example and a binding module disagree, the binding module governs and the example
is wrong.

Examples exist to settle judgement calls that prose leaves open: how much detail a change-set
purpose carries, what an honest verification claim looks like when evidence is incomplete,
and which failures are recorded rather than repaired.

## Example 1 — Feature implementation, fully verified

### Input

```text
Accepted technical design, element D-004: the run ledger must record the validator name
alongside the artifact path, so a completion can be re-validated without re-resolving the
agent manifest.
Execution plan task T-006: persist the validator name in the state ledger.
Coding standards: ledger fields are written by the same function that writes the record.
```

### Conforming output, abridged

```markdown
## Metadata

- Report ID: IR-2026-0031
- Change reference: T-006 / D-004
- Workflow phase: implementation
- Status: complete
- Verification status: verified
- Review status: pending-review

## Change Set

| ID | Path | Change Type | Purpose | Design Ref |
|---|---|---|---|---|
| `C-001` | `runtime/state_writer.py` | modified | Write the validator name into the state ledger record | `D-004` |
| `C-002` | `tests/test_state_writer.py` | added | Assert the ledger carries the validator name after a completion | `T-006` |

## Test Evidence

| ID | Test | Type | Covers | Command | Result |
|---|---|---|---|---|---|
| `T-001` | the ledger record carries the validator name | unit | `C-001`, `C-002` | `pytest tests/test_state_writer.py -k validator_name` | pass |
| `T-002` | the existing state-writer suite still passes | regression | `C-001` | `pytest tests/test_state_writer.py` | pass |
```

### Why it conforms

- Both change-set entries appear in a `Covers` cell, so `M1` passes.
- Each result was produced by the command beside it, so `E1` and `E2` hold.
- The design element is cited per entry rather than once for the whole change.
- `Verification status: verified` is claimed only because no area was left without evidence,
  which is what `M7` checks against the `Unverified areas` field.

## Example 2 — Defect fix with a failing witness first

### Input

```text
Accepted bug analysis: causal step C-003 establishes that the reclaim path leaves the lease
populated, so the next dispatch treats a stale lease as live.
Regression targets: the reclaim path and the dispatch idempotency check.
```

### Conforming output, abridged

```markdown
| ID | Test | Type | Covers | Command | Result |
|---|---|---|---|---|---|
| `T-001` | reclaim leaves no lease field populated | unit | `C-001` | `pytest tests/test_reclaim.py -k lease_cleared` | pass |
| `T-002` | reclaim then dispatch issues exactly one envelope | regression | `C-001`, `C-002` | `pytest tests/test_reclaim.py -k single_envelope` | pass |
```

### Why it conforms

- `T-001` is the check that failed before the change and passes after it. Stage 4 of
  `reasoning.md` requires that witness for a defect fix; a fix whose test would have passed
  beforehand has not been shown to fix anything.
- The regression targets the analysis named are each covered, so the fix's blast radius is
  measured rather than assumed.

## Example 3 — Partial verification, honestly reported

### Situation

Two of three accepted elements are implemented. The third depends on an environment this
agent may not reach, so its behaviour cannot be exercised here.

### Conforming output, abridged

```markdown
- Status: provisional
- Verification status: partially-verified

## Verification Results

- Verification method: the unit and regression suites for the two implemented elements were executed; the third element has no executable check in this environment.
- Commands executed: `pytest tests/test_router.py`
- Result summary: 18 tests executed, 18 passed, 0 failed.
- Unverified areas: the third element's behaviour against a live upstream, which no check in this repository exercises.

## Open Questions

| ID | Question | Blocking | Owner | Affected changes |
|---|---|---|---|---|
| `Q-001` | Which environment validates the third element before the Review Gate? | yes | qa | `C-003` |
```

### Why it conforms

- The claim matches the gap. `M7` fails a `verified` claim that names a gap, and fails a
  `partially-verified` claim that names none.
- `K1` requires the open question, because a provisional report that records no unresolved
  decision is reporting a settled run that is not settled.
- The status was derived from the Stage 7 table, not chosen because the work felt nearly done.

## Example 4 — Deviation recorded and escalated

### Situation

The accepted design places validation in the transport layer. The existing layering rule,
which the same design declares an invariant, forbids it.

### Conforming output, abridged

```markdown
| ID | Deviation | Design element | Rationale | Escalation |
|---|---|---|---|---|
| `V-001` | Validation implemented in the service layer in `C-002` rather than the transport layer | `D-002` placement of validation | The design's own layering invariant forbids the transport layer from holding domain rules; both cannot hold | escalated to architect, ESC-2026-0044 |
```

### Why it conforms

- The deviation cites `C-002`, so `M4` passes: the record names the change that embodies it.
- The escalation is named rather than marked `not-required`, because the departure changes
  what was accepted. `V2` fails the opposite choice.
- The agent did not resolve the contradiction by picking the more convenient reading and
  moving on. Choosing between two accepted statements is the architect's decision.

## Non-conforming behaviours

### N1 — A change-set entry with no covering evidence

```markdown
| `C-004` | `runtime/aggregator.py` | modified | Include blocked phases in the summary | `D-006` |
```

with no `T-nnn` whose `Covers` cell names `C-004`.

**Why it fails.** `M1` is blocking. The report would otherwise present four changes with the
authority of three tested ones. The repair is a covering check, not a footnote.

### N2 — A predicted result

```markdown
| `T-003` | the migration applies cleanly | integration | `C-005` | `pytest tests/test_migration.py` | pass |
```

recorded without running the command.

**Why it fails.** `E2` is blocking. `not-run` is an available, honest value, and a
`partially-verified` claim built on it is defensible. A fabricated `pass` is not.

### N3 — Silent design revision

Implementing a different approach than the one accepted, because it was simpler, and
recording nothing.

**Why it fails.** `M4` and `V2` are blocking, and the run should have entered Delegation.
The reviewer would be assessing a change against a design it no longer implements, and the
mismatch would surface at the gate rather than here.

### N4 — Self-awarded readiness

```markdown
- Reviewer focus areas: none; the change is ready to merge.
```

**Why it fails.** `M6` and `A1` are blocking. The merge decision belongs to the reviewer at
the gate. The correct content names where review is most valuable, which is never nowhere.

### N5 — Green by deletion

Removing or skipping a failing existing test so the suite passes.

**Why it fails.** `B4` is blocking. The failure is evidence about the change. Reporting it
as `fail` and setting the status to `provisional` is the conforming response; making it
disappear is not.

### N6 — An undeclared write

Editing a configuration file to make a test pass, and omitting it from the change set.

**Why it fails.** `B1` and `B2` are blocking, and the runtime's own side-effect comparison
rejects the completion independently. Every write is declared, or it did not happen honestly.

### N7 — Bare-citing an upstream artifact's identifiers

```markdown
The change removes the null dereference `C-004` identified as the proximate cause, and
resolves `Q-001` and `Q-002` from the root-cause analysis.
```

where `C-004`, `Q-001`, and `Q-002` are the upstream `bug-analysis.md`'s own identifiers,
and this report's Change Set defines only `C-001` through `C-002` and its Open Questions
section says `None identified.`

**Why it fails.** `C4` is blocking. `C-nnn` and `Q-nnn` are this report's own identifier
schemes, so a bare `C-004` in prose is a reference into this report's Change Set — where no
`C-004` is defined. The upstream artifact's namespace and this one's collide silently, and
the reader cannot tell a dangling local reference from an intended upstream citation. The
repair is descriptive or artifact-qualified: "the null dereference the root-cause analysis
identifies as its cause `C-004`" names the same finding without claiming it in this report's
namespace.

### N8 — An out-of-scope item reported as unverified under a `verified` claim

```markdown
verificationStatus: verified
```

```markdown
- Unverified areas: the bulk-import path, which this fix deliberately does not touch.
```

**Why it fails.** `M7` is blocking. A `verified` claim names no gap; naming one contradicts
the claim, and the Validation Engine rejects the pair without judging which half is honest.
A deliberately out-of-scope item is not an unverified area — it was never in scope to
verify. It belongs under `Out of scope` in the summary. `Unverified areas` is for in-scope
behavior the executed evidence does not reach, and when everything in scope is reached, the
correct value is `none`.

## Judgement notes

- A purpose written as `fix bug` is not a purpose. It states neither what changed nor what
  is now true. `S6` accepts it structurally, and the reviewer will not.
- `Out of scope` is not decoration. It is what stops the next reader from assuming coverage
  that was never attempted.
- A residual risk with a planned mitigation is follow-up work, not a mitigation. `Residual
  Risk` records what is in place; `Handoff Notes` records what is not yet.
- When the accepted change and the existing code disagree about something small, it is still
  a deviation. Size is not the test; departure from what was accepted is.
