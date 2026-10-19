# Product Owner: Quality Contract

## Status

Binding self-verification contract for agent `product-owner`, version 1.0.0. Every
check here runs before the artifact is emitted. A run that emits without running them has
not completed; it has stopped.

## How to read this module

Checks are grouped by concern. Each carries a severity:

| Severity | Meaning |
|---|---|
| Blocking | the artifact may not be emitted while this fails |
| Correctable | the artifact is repaired and re-verified before emission |
| Advisory | recorded, and reported as a known weakness rather than repaired silently |

The Validation Engine re-runs the machine-decidable subset independently, in
`runtime/scope_definition_validator.py`. The structural, field, vocabulary, and identifier
checks are executed there as `C1` to `C7` by the shared contract engine; the checks numbered
`SD1` to `SD8` below are this agent's own semantic rules and carry the same identifiers on
both sides. Where a check appears in both, the two are the same rule, and the Validation
Engine's verdict is the one that decides whether the phase advances.

An obligation this module states but no machine can decide is recorded as
not-machine-checkable rather than dropped. Three such obligations exist, listed last.

## Structural checks

| ID | Check | Severity |
|---|---|---|
| `S1` | Nine mandatory sections present, exactly once, in contract order | Blocking |
| `S2` | No level-2 section other than the nine; this artifact permits no appendix | Correctable |
| `S3` | No mandatory section left empty | Blocking |
| `S4` | Exactly one fenced block, the leading metadata block | Blocking |
| `S5` | Every declared field bullet present, with its label exactly as `output.md` states it | Blocking |
| `S6` | No declared field left unanswered | Blocking |
| `S7` | No template authoring comment left in the emitted artifact | Advisory |
| `S8` | Every table carries its declared columns, in the declared order, with no empty required cell | Blocking |

## Metadata checks

| ID | Check | Severity |
|---|---|---|
| `D1` | Metadata block parses, with every declared field populated | Blocking |
| `D2` | `producedBy` is `product-owner` | Blocking |
| `D3` | `schemaVersion` is `1.0.0` and `agentVersion` is a semantic version | Blocking |
| `D4` | `status` is `complete`, `provisional`, or `blocked` | Blocking |
| `SD1` | `scopeVerdict` is `bounded`, `partially-bounded`, or `blocked` | Blocking |
| `SD2` | `acceptanceCriteriaCount` equals the number of rows in the Acceptance Criteria table | Blocking |
| `D5` | `featureName` and `status` agree between the metadata block and the body | Blocking |
| `D6` | `inputDigest` and `contextDigest` match the frozen snapshot in the invocation envelope | Blocking |

`SD2` is the rule that stops the count and the table from drifting apart. A downstream phase
that plans against the metadata and a gate that reads the table must be counting the same
criteria.

## Acceptance checks

The rules that decide whether the criteria are criteria rather than aspirations.

| ID | Check | Severity |
|---|---|---|
| `SD3` | Every acceptance criterion names the method that verifies it | Blocking |
| `SD4` | Every acceptance criterion names the in-scope item it bounds | Blocking |
| `A4` | Every criterion states a threshold somebody could disagree about the meeting of | Blocking |
| `A5` | Every criterion states a condition on behaviour, not a task to perform | Blocking |
| `A6` | Every declared in-scope item is bounded by at least one criterion | Blocking |
| `A7` | Each criterion bounds exactly one scope item | Correctable |

`SD3` and `SD4` are decided by the Validation Engine. `A4` to `A7` are decided here, because
only this run knows which expectations it started from.

## Scope integrity

The rules that decide whether the boundary is the one the request supports.

| ID | Check | Severity |
|---|---|---|
| `SD5` | A `partially-bounded` or `blocked` scope records at least one open question | Blocking |
| `SD8` | A `bounded` scope names at least one explicit exclusion | Correctable |
| `G1` | Every in-scope item traces to a supplied expectation or to a recorded scope decision | Blocking |
| `G2` | No expectation from the supplied inputs is absent from all three of scope, exclusions, and questions | Blocking |
| `G3` | The verdict is the one the Stage 7 question set of `reasoning.md` yields | Blocking |
| `G4` | `status` and `scopeVerdict` move together, per `output.md` | Blocking |
| `G5` | No blocking open question stands while the verdict is `bounded` | Blocking |

`G3` is the rule that stops a verdict from being chosen first and justified afterwards. The
question set decides; the artifact records what it decided.

## Value alignment

The rules that keep the artifact about the business outcome rather than about the solution.

| ID | Check | Severity |
|---|---|---|
| `L1` | `Business goal` states an outcome, with no mechanism named inside it | Blocking |
| `L2` | `Problem statement` states the problem without a solution inside it | Blocking |
| `L3` | `Success measure` is stated, or its absence is recorded as an open question | Blocking |
| `L4` | No in-scope item names an implementation route, technology, or component | Blocking |
| `L5` | Priorities are assigned from the declared vocabulary and reflect stated business value | Correctable |

## Decision checks

| ID | Check | Severity |
|---|---|---|
| `SD6` | Every scope decision carries a rationale a reviewer can assess | Blocking |
| `D7` | Every decision names who it was decided by | Blocking |
| `D8` | Every decision states its impact on the delivered outcome | Blocking |
| `D9` | Every exclusion states both a reason and a revisit trigger | Blocking |

## Authority boundary

| ID | Check | Severity |
|---|---|---|
| `SD7` | The artifact issues no task, change-set, or decision-record identifier | Blocking |
| `B1` | No technical design, structure, or technology decision is recorded | Blocking |
| `B2` | No task breakdown, estimate, wave, or delivery sequence is recorded | Blocking |
| `B3` | No gate decision, merge decision, or release readiness claim is recorded | Blocking |
| `B4` | No test strategy or validation plan is recorded; the verification method names the check, not the plan | Blocking |
| `B5` | No file was written outside the envelope's `permitted_writes` | Blocking |
| `B6` | No external system, tracker, or stakeholder was accessed | Blocking |

`SD7` is decided by the Validation Engine, by scanning for the identifier schemes downstream
phases own. `B1` to `B6` are decided here.

## Content checks

| ID | Check | Severity |
|---|---|---|
| `C1` | No diff, patch, hunk, or file-modification directive appears in the artifact | Blocking |
| `C2` | No model, vendor, or agent-runtime name appears that the inputs and context did not already use | Blocking |
| `C3` | Identifiers use the zero-padded three-digit scheme, ascend from 001, and are unique | Correctable |
| `C4` | Every referenced identifier is defined in its declaring section | Blocking |
| `C5` | No credential, token, secret, or customer-identifying content appears | Blocking |

## Rejection rules

The artifact is not emitted, and the run does not claim completion, when any of the
following holds:

1. An acceptance criterion has no verification method.
2. An acceptance criterion bounds no declared in-scope item.
3. The verdict is `bounded` while a blocking open question stands.
4. A supplied expectation appears in none of scope, exclusions, or questions.
5. A scope decision is recorded with no rationale behind it.
6. The artifact decides something a downstream phase owns.

Each of these is a reason to change the artifact or to raise a question, never a reason to
add a caveat and emit anyway.

## Repair procedure

1. Identify every failing check, not only the first.
2. Decide, per failure, whether the artifact is wrong or the boundary is wrong. An artifact
   edited to match a boundary that is actually wrong is the worse of the two repairs.
3. Repair, then re-run the entire check set. A repair can break a check that previously
   passed, and only a full re-run catches it.
4. If a failure cannot be repaired within this agent's authority, escalate it per
   `execution.md` and record the run as blocked.

## Not-machine-checkable obligations

Recorded, never silently skipped. Each is discharged by this agent's own judgement and
stated in the run's result envelope.

| ID | Obligation | Reference |
|---|---|---|
| `SN1` | Each acceptance criterion states a threshold the business would actually accept | `output.md#acceptance-criteria` |
| `SN2` | The recorded scope matches what the requester asked for, with no silent widening | this module, Scope integrity |
| `SN3` | The stated business goal is the goal the requester holds, not one inferred for them | this module, Value alignment |

## Result envelope reporting

The result envelope carries: the counts of in-scope items, exclusions, acceptance criteria,
scope decisions, and open questions; the declared status and scope verdict; the pass or fail
result of every check in this module; and the three obligations above with the judgement
made on each.
