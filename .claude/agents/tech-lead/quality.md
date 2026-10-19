# Tech Lead: Quality Contract

## Status

Authoritative on what blocks emission. Every check below runs before the artifact is emitted,
and every result is recorded — including the passes, because a check that was never run and a
check that passed look identical in an artifact that only reports failures.

## How to read this module

Each check carries a severity:

| Severity | Meaning |
|---|---|
| **Blocking** | the artifact is not emitted while this fails; the run does not proceed |
| **Correctable** | the repair procedure runs; if it clears, emission proceeds and the repair is recorded |
| **Advisory** | recorded and reported; does not block |

The identifiers `T1` to `T8` name the semantic checks that
`runtime/technical_recommendation_validator.py` enforces. The structural checks the Validation
Engine contributes (`C1.x` to `C7.x`) are declared by `artifact_contract.py` and are not
restated here; this module states the rules that are this role's own.

A check that cannot be run — because the section it inspects is absent — fails. It does not
pass by default.

## Structural checks

| # | Check | Severity |
|---|---|---|
| S1 | All ten level-2 sections present, in contract order, with exact titles | Blocking |
| S2 | Leading fenced yaml metadata block present and parseable | Blocking |
| S3 | Every declared field bullet present in its section | Blocking |
| S4 | Every declared table present with its declared columns | Blocking |
| S5 | No table row leaves a required cell empty; `None.` is used instead | Blocking |
| S6 | `Open Questions` appendix present whenever status is `provisional` or `blocked` | Blocking |
| S7 | No diff markers, patch hunks, or quoted source changes | Blocking |
| S8 | At most three fenced blocks: the metadata block, plus quoted command output | Correctable |

## Metadata checks

| # | Check | Severity |
|---|---|---|
| M1 | Every metadata field present and populated | Blocking |
| M2 | `producedBy` is `tech-lead` | Blocking |
| M3 | `schemaVersion` is `1.0.0` and `agentVersion` is semantic | Blocking |
| M4 | `status` is `complete`, `provisional`, or `blocked` | Blocking |
| M5 | `decisionBasis` resolves from the routed phase, not from preference | Blocking |
| M6 | `sourceInputs` lists every supplied input the recommendation actually used | Blocking |

## Identifier checks

| # | Check | Severity |
|---|---|---|
| ID1 | Identifiers use the zero-padded three-digit scheme | Correctable |
| ID2 | Every referenced identifier is defined in its declaring section | Blocking |
| ID3 | No identifier is defined twice | Blocking |
| ID4 | Identifiers ascend from 001 without gaps | Correctable |

## Semantic checks

These are this role's decision rules and constraints, expressed as checks over the artifact.

**T1 — The metadata and the sections agree.** `recommendedOption` equals the Recommendation
section's `Recommended option`, and `readinessDecision` equals the Readiness section's
`Recommended decision`. **Blocking.**

A metadata block edited apart from the prose it summarises is how a consumer routing on metadata
and a reader reading prose come to different conclusions about the same artifact.

**T2 — Every value comes from a declared vocabulary.** `decisionBasis`, and every `Priority`,
`Effort`, `Delivery risk`, `Reversibility`, `Severity`, `Likelihood`, and `Status` cell, is a
member of the set `output.md` declares for it. **Blocking.**

A comparison whose dimensions are spelled freehand cannot be compared against another run of the
same phase, which is the whole point of running it the same way twice.

**T3 — The assessment summary recomputes.** Each of the four figures equals the count derived
from the tables above it: criteria rows, option rows, risk rows, and the critical-or-high rows
whose status is `open`. **Blocking.**

No count without a recount.

**T4 — The recommendation names an option that was evaluated.** `Recommended option` is either
`deferred` or an `O-nnn` defined in the Options table *and* assessed in the Tradeoff Analysis.
**Blocking.**

Recommending an unevaluated option means recommending something the reader cannot check — the
single failure mode this artifact exists to make impossible.

**T5 — Every evaluated option is assessed.** Every `O-nnn` in the Options table appears exactly
once in the Tradeoff Analysis, and at least two options are recorded. **Blocking.**

An option listed but never scored is a decoy: it makes the comparison look wider than it was.

**T6 — An unqualified proceed carries no open blocker.** Where `Recommended decision` is
`proceed`, no Risk and Blocker Register row is both `critical` or `high` in severity and `open`
in status. **Blocking.**

This is invariant I7. It is the check that stops delivery pressure from being resolved in the
verdict line.

**T7 — The outstanding blockers are exactly the open ones.** The `RK-nnn` identifiers named in
`Blocking items outstanding` are exactly the set of `critical` and `high` rows whose status is
`open`; `None identified.` is correct only when that set is empty. **Correctable.**

**T8 — The producer does not decide the gate.** `Deciding authority` is populated and is not
`tech-lead`. **Blocking.**

This is the Producer Exclusion Rule made mechanical. The role that decides more gates than any
other is exactly the role that most needs the exclusion enforced rather than remembered.

## Evidence checks

| # | Check | Severity |
|---|---|---|
| E1 | Every option's `Evidence` cell names a source, not a judgement | Blocking |
| E2 | Every criterion's `Source` cell names where the criterion came from | Blocking |
| E3 | Every risk carries an owner | Blocking |
| E4 | Every command whose result the artifact rests on is named in the artifact | Blocking |
| E5 | A claim derived from a provisional or low-confidence input carries that qualification | Advisory |

## Authority checks

| # | Check | Severity |
|---|---|---|
| A1 | No wording records a gate decision as taken | Blocking |
| A2 | No acceptance criterion is set, relaxed, or reinterpreted | Blocking |
| A3 | No structural design, decision record, or task breakdown appears | Blocking |
| A4 | No finding's severity is restated at a level other than its source's | Blocking |
| A5 | No repository file other than the artifact and the result envelope was written | Blocking |

## Content checks

| # | Check | Severity |
|---|---|---|
| N1 | No model, vendor, or provider name appears | Blocking |
| N2 | `Rationale` references the declared criteria rather than asserting a preference | Blocking |
| N3 | Every rejected option carries the reason it lost | Blocking |
| N4 | `Reversal plan` is populated; `irreversible` is stated rather than left blank | Correctable |
| N5 | A `deferred` recommendation states what would separate the options | Blocking |

## Rejection rules

The artifact is not emitted, and the run is reported blocked, when:

- fewer than two options are recorded;
- the recommended option is not defined in the Options table and is not `deferred`;
- the assessment summary disagrees with the tables it summarises;
- `Recommended decision` is `proceed` while a critical or high blocker is open;
- `Deciding authority` is `tech-lead`, absent, or names no role;
- any gate decision is recorded as taken;
- a criterion was added or reworded after the options were scored;
- a severity was lowered without evidence that lowers it;
- a repository file outside the permitted writes was modified.

The first, second, third, fourth, and fifth of these are the mechanical checks `T5`, `T4`, `T3`,
`T6`, and `T8`. The rest are obligations recorded below.

## Repair procedure

For a Correctable failure, in this order:

1. **ID1, ID4** — renumber identifiers in final rendered order, contiguous from 001, and update
   every reference to them.
2. **T7** — recompute the open critical-and-high set from the register and rewrite
   `Blocking items outstanding` from it. Never adjust the register to match the prose.
3. **N4** — supply the reversal plan, or state `irreversible` with what that costs.
4. **S8** — remove quoted output that is not load-bearing evidence.

Re-run every check after repair. A repair that trips a different check has not repaired
anything. Record each repair applied in the result envelope.

If a Blocking failure survives repair, stop: raise `E-OUTPUT`, report the failing check by
identifier, and emit nothing.

## Not-machine-checkable obligations

These are this role's judgement. They are declared so the record shows they were owed, not
inferred to have been met.

| # | Obligation |
|---|---|
| NMC1 | The criteria were fixed before the options were scored, and not adjusted to fit a preferred one |
| NMC2 | Every option genuinely open under the recorded constraints was enumerated, including the status quo |
| NMC3 | Each effort and severity position reflects what the evidence supports, not what the schedule needs |
| NMC4 | No quality gate was traded, weakened, or routed around to reach the recommendation |

## Result envelope reporting

`structured_output` carries exactly:

```
decisionBasis, recommendedOption, readinessDecision, status,
criteriaApplied, optionsEvaluated, risksRecorded, blockingItemsOpen,
severityCounts, decidingAuthority,
checksRun, checksPassed, blockingFailures, correctableRepairs,
notMachineCheckable, commandsRun, escalations, openQuestions
```

`checksRun` and `checksPassed` are counts of the checks in this module. `blockingFailures` is
empty on a successful emission, by construction. `notMachineCheckable` lists `NMC1` to `NMC4`,
so the obligations travel with the artifact rather than being assumed discharged.
