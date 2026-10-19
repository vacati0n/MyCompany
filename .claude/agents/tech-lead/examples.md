# Tech Lead: Reference Examples

## Status

Illustrative. `output.md` governs structure and `quality.md` governs emission; where an example
here appears to differ from either, the module wins and the example is wrong.

Excerpts are abridged to the sections that carry the point being made. A real artifact carries
all ten sections and the metadata block.

## Example 1 — A direction recommended, with the condition that gates it

### Input

An `investigation-report.md` establishing that the retry path strands a run under one failure
class, with two options surfaced. Workflow constraints state the release date is fixed and one
downstream team depends on the current interface.

### Conforming output, abridged

## Evaluation Criteria

| ID | Criterion | Why it matters | Priority | Source |
|---|---|---|---|---|
| `EC-001` | No run is stranded under any failure class | The defect under investigation is exactly this | must-have | `investigation-report.md` |
| `EC-002` | The current interface is preserved | A downstream team depends on it this cycle | must-have | workflow constraints |
| `EC-003` | Deliverable before the fixed release date | The date is stated as fixed, not preferred | high | workflow constraints |
| `EC-004` | Reversible within one run | Recovery paths are where an error compounds | medium | delivery principle applied by this role |

## Options

| ID | Option | Summary | Effort | Delivery risk | Reversibility | Evidence |
|---|---|---|---|---|---|---|
| `O-001` | Do nothing | Leave the retry path as it is | trivial | high | reversible | `investigation-report.md` `E-003` |
| `O-002` | Bound the retry and classify the exhausted case | Add a terminal classification and a bounded delay | small | low | reversible | `investigation-report.md` `E-001`, `E-004` |
| `O-003` | Replace the recovery controller | Rebuild classification and dispatch together | large | high | costly-to-reverse | `investigation-report.md` `E-002` |

## Tradeoff Analysis

| Option | Criteria met | Criteria missed | Strengths | Weaknesses | Sequencing implication |
|---|---|---|---|---|---|
| `O-001` | `EC-002`, `EC-003`, `EC-004` | `EC-001` | Costs nothing now | Leaves the stranding defect live, which is the must-have | None; the defect stays open into the release |
| `O-002` | `EC-001`, `EC-002`, `EC-003`, `EC-004` | None. | Closes the defect inside the current interface | Leaves the wider classification design untouched | Must land before the release branch is cut |
| `O-003` | `EC-001`, `EC-004` | `EC-002`, `EC-003` | Fixes the class of defect, not the instance | Breaks the interface a downstream team depends on this cycle | Would push past the fixed date and force a downstream migration |

## Assessment Summary

- Criteria applied: 4
- Options evaluated: 3
- Risks and blockers recorded: 2
- Blocking items open: 1

## Recommendation

- Recommended option: `O-002`
- Rationale: `O-002` is the only option meeting every must-have criterion — it closes `EC-001` without breaking `EC-002` — and it does so at small effort inside `EC-003`; `O-001` fails the must-have outright and `O-003` fails two.
- Preconditions: `RK-001` must reach `mitigated` before the release branch is cut.
- Options rejected: `O-001`, because it misses `EC-001`, the criterion the investigation was opened over; `O-003`, because it misses `EC-002` and `EC-003`, and the interface break lands on a team that has not planned for it.

## Readiness

- Recommended decision: proceed-with-conditions
- Conditions to satisfy: `RK-001` mitigated and re-verified before the release branch is cut.
- Blocking items outstanding: `RK-001`
- Deciding authority: `orchestrator`

### Why it conforms

- The criteria are sourced. Three trace to supplied inputs; the fourth is declared honestly as a
  delivery principle this role applies, rather than dressed as a requirement.
- The status quo is a real option with a real score, not a placeholder.
- The rationale argues against the criteria by identifier, so it can be checked rather than
  agreed with.
- Every rejected option carries the criterion it missed.
- A high blocker is open, so the decision is conditional rather than `proceed` — check `T6`.
- `Blocking items outstanding` names exactly the open critical-and-high set — check `T7`.
- The deciding authority is `orchestrator`, not the producer — check `T8`.

## Example 2 — A merge decision that says no

### Input

A `review-package.md` carrying two high findings, one unresolved, and a `validation-report.md`
whose verdict is `pass-with-reservations`.

### Conforming output, abridged

## Options

| ID | Option | Summary | Effort | Delivery risk | Reversibility | Evidence |
|---|---|---|---|---|---|---|
| `O-001` | Merge now | Take the change as it stands | trivial | high | costly-to-reverse | `review-package.md` findings table |
| `O-002` | Merge after corrections | Require `F-004` corrected, then merge | small | low | reversible | `review-package.md` `F-004`; `validation-report.md` verdict |
| `O-003` | Do not merge | Return the change to implementation | medium | medium | reversible | `review-package.md` findings table |

## Risk and Blocker Register

| ID | Risk or blocker | Severity | Likelihood | Delivery impact | Mitigation | Owner | Status |
|---|---|---|---|---|---|---|---|
| `RK-001` | Finding `F-004` is unresolved and high | high | certain | The path it names is the one the change alters | Correct `F-004` and re-review | `dev-1-implement` | open |
| `RK-002` | The validation carries a reservation on an untested path | medium | possible | A regression could reach production unobserved | Extend coverage to the altered path | `qa` | open |

## Recommendation

- Recommended option: `O-002`
- Rationale: `O-002` clears `EC-001` — no high finding travels — at small effort, where `O-001` carries `RK-001` into the trunk and `O-003` costs a full return cycle for a correction the change itself can absorb.
- Preconditions: `F-004` corrected and re-reviewed; the reservation in the validation report closed or accepted by its owner.
- Options rejected: `O-001`, because merging with `RK-001` open misses `EC-001`; `O-003`, because the correction is bounded and returning the change misses `EC-003` on delivery time without a corresponding gain.

## Readiness

- Recommended decision: do-not-proceed
- Conditions to satisfy: `RK-001` resolved, then re-present for the Merge Gate.
- Blocking items outstanding: `RK-001`
- Deciding authority: `orchestrator`

### Why it conforms

- The options are dispositions of the change, which is what `merge-decision` compares.
- The recommended option and the readiness decision are consistent: `O-002` is *merge after
  corrections*, so the change does not proceed now — the recommendation names the direction and
  the readiness names the position on today's gate.
- `RK-001` is `certain`, because a finding that exists has already occurred.
- The severities are the reviewer's, carried through unchanged — check `A4`.

## Example 3 — Deferred, because the evidence cannot separate the options

### Situation

Two options survive the criteria with identical scores. The measurement that would separate them
was never taken and cannot be taken inside this run.

### Conforming output, abridged

## Recommendation

- Recommended option: deferred
- Rationale: `O-002` and `O-003` meet every must-have criterion and differ only on `EC-004`, which turns on a throughput figure no supplied input carries and no permitted command can establish; recommending either would be a preference stated as a judgement.
- Preconditions: `Q-001` answered.
- Options rejected: `O-001`, because it misses `EC-001`.

## Readiness

- Recommended decision: deferred
- Conditions to satisfy: the throughput measurement in `Q-001`, after which this recommendation is re-run.
- Blocking items outstanding: None identified.
- Deciding authority: `orchestrator`

## Open Questions

| ID | Question | Owner | Affects |
|---|---|---|---|
| `Q-001` | What is the sustained throughput of the current path under peak load? | `qa` | Separates `O-002` from `O-003` on `EC-004` |

### Why it conforms

- The deferral states what would separate the options and who can supply it — check `N5`.
- The metadata carries `status: provisional`, so the `Open Questions` appendix is required and
  present.
- No blocker is open, so `Blocking items outstanding` reads `None identified.` — `T7` passes on
  the empty set.
- The recommendation is honest about being a non-recommendation rather than picking one and
  hedging the rationale.

## Example 4 — Nothing is wrong, and the artifact still says what it examined

### Situation

A release readiness assessment where every prior gate passed, no defect is open, and the answer
is go.

### Conforming output, abridged

## Risk and Blocker Register

| ID | Risk or blocker | Severity | Likelihood | Delivery impact | Mitigation | Owner | Status |
|---|---|---|---|---|---|---|---|
| `RK-001` | The candidate was validated against a fixture set, not a production-like data volume | medium | possible | A volume-sensitive regression would surface after deployment | Monitor the two altered paths for the first release window | `orchestrator` | accepted |

## Assessment Summary

- Criteria applied: 5
- Options evaluated: 2
- Risks and blockers recorded: 1
- Blocking items open: 0

## Readiness

- Recommended decision: proceed
- Conditions to satisfy: None identified.
- Blocking items outstanding: None identified.
- Deciding authority: `qa`

### Why it conforms

- A clean assessment still records what was examined and what it accepted. `RK-001` is real,
  `medium`, and `accepted` with an owner — it is not omitted for being non-blocking.
- `Blocking items open: 0` is a counted zero, not an assumed one.
- `proceed` is permitted here precisely because no critical or high row is open — check `T6`.
- The deciding authority for the Readiness Gate is `qa`, per the gate matrix.

## Non-conforming behaviours

### N1 — A recommendation for an option that was never evaluated

`Recommended option: O-004` where the Options table defines `O-001` to `O-003`. Caught by `T4`,
and by the identifier check that finds a reference with no definition. The reader has no way to
know what `O-004` was, which makes the rationale uncheckable.

### N2 — Criteria assembled after the preference

The criteria set contains one criterion that only the recommended option could satisfy, sourced
to nothing. Not mechanically catchable, which is why it is `NMC1`. The tell is a `Source` cell
that names this artifact, or names nothing.

### N3 — A summary edited apart from its tables

`Options evaluated: 4` over a table with three rows. Caught by `T3`. A count that is not a
recount is a claim about what the author meant to do.

### N4 — A proceed over an open blocker

`Recommended decision: proceed` while `RK-002` is high and open. Caught by `T6`. This is the
shape delivery pressure takes when it reaches the artifact: everything above the verdict line
stays honest, and the verdict line does the lying.

### N5 — A severity lowered to reach a recommendation

A reviewer's high finding recorded here as medium so that `T6` passes. Caught by `A4` on
inspection against the source, and by the reviewer on handoff. The severity belongs to the role
that classified it.

### N6 — The producer deciding its own gate

`Deciding authority: tech-lead`. Caught by `T8`. This role decides more gates than any
other, which is exactly why it may not decide the ones over its own evidence.

### N7 — A gate decision recorded as taken

`Readiness: merge approved` instead of a recommendation to a named authority. Caught by `A1`.
The wording is not a style question: an artifact that reads as a decision will be treated as one.

### N8 — One option, presented as an evaluation

A single option with a long rationale. Caught by `T5` and by the two-row floor on the Options
table. A comparison needs something to compare against, and the status quo almost always is one.

### N9 — Designing instead of recommending

The artifact carries a component diagram and a proposed module boundary. Caught by `A3`. The
structural position is the architect's; this artifact cites it.

## Judgement notes

The checks catch what is decidable by inspection. They do not catch a criteria set that was
quietly shaped around a preferred answer, an option that was never enumerated because nobody
wanted to cost it, or an effort figure that is optimistic rather than evidenced. Those are
`NMC1` to `NMC4`, and they are the part of this role that is actually hard. The mechanical
checks exist so that the judgement is what is being examined, rather than the arithmetic.
