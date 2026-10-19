# Reviewer: Quality Contract

## Status

Binding self-verification contract for agent `dev-2-reviewer`, version 1.0.0. Every check
here runs before the package is emitted. A run that emits without running them has not
completed; it has stopped.

## How to read this module

Checks are grouped by concern. Each carries a severity:

| Severity | Meaning |
|---|---|
| Blocking | the package may not be emitted while this fails |
| Correctable | the package is repaired and re-verified before emission |
| Advisory | recorded, and reported as a known weakness rather than repaired silently |

The Validation Engine re-runs the machine-decidable subset independently, in
`runtime/review_package_validator.py`. The structural, field, vocabulary, and identifier checks
execute there as `C1` to `C7` by the shared contract engine; the checks numbered `P1` to `P7`
below are this agent's own semantic rules and execute there too. Where a check appears in both,
the two are the same rule, and the Validation Engine's verdict is the one that decides whether
the phase advances.

The checks numbered `R1` onward are decided here alone, because only this run knows what it
read, what it ran, and what it declined to judge.

An obligation this module states but no machine can decide is recorded as not-machine-checkable
rather than dropped. Three such obligations exist, listed last.

## Structural checks

| ID | Check | Severity |
|---|---|---|
| `S1` | Nine mandatory sections present, exactly once, in contract order | Blocking |
| `S2` | No level-2 section other than the nine and the `Open Questions` appendix | Correctable |
| `S3` | No mandatory section left empty | Blocking |
| `S4` | At most twelve fenced blocks, the leading metadata block among them | Blocking |
| `S5` | Every declared field bullet present, with its label exactly as `output.md` states it | Blocking |
| `S6` | No declared field left unanswered | Blocking |
| `S7` | No template authoring comment left in the emitted artifact | Advisory |
| `S8` | Every table carries its declared columns, in the declared order, with no empty required cell | Blocking |

## Metadata checks

| ID | Check | Severity |
|---|---|---|
| `D1` | Metadata block parses, with every declared field populated | Blocking |
| `D2` | `producedBy` is `dev-2-reviewer` | Blocking |
| `D3` | `schemaVersion` is `1.0.0` and `agentVersion` is a semantic version | Blocking |
| `D4` | `status` is `complete`, `provisional`, or `blocked` | Blocking |
| `D5` | `sourceInputs` names every input this review actually read | Blocking |
| `D6` | `inputDigest` and `contextDigest` match the frozen snapshot in the invocation envelope | Blocking |

## Findings checks

The rules that decide whether the findings are findings rather than preferences.

| ID | Check | Severity |
|---|---|---|
| `P2` | Every finding uses the declared severity, category, and status vocabularies | Blocking |
| `F1` | Every finding names a location that resolves in the reviewed change | Blocking |
| `F2` | Every finding names a requirement drawn from the loaded standards | Blocking |
| `F3` | Every severity is the one the Stage 6 table of `reasoning.md` yields | Blocking |
| `F4` | Findings are ordered by descending severity, ties broken lexically by location | Correctable |
| `F5` | No finding marked `accepted-risk` lacks a recorded acceptance by its owning role | Blocking |

`P2` is decided by the Validation Engine. `F1` to `F5` are decided here, because only this run
knows which standards it loaded and which locations it opened.

## Arithmetic checks

| ID | Check | Severity |
|---|---|---|
| `P3` | The severity summary recomputes exactly from the findings table | Blocking |
| `P5` | Every open critical or high finding is addressed by a correction request | Blocking |
| `P7` | The verdict names exactly the open critical and high findings as outstanding | Correctable |

These three are the mechanical form of the constraint that a review cannot be softened without
changing a finding. Each one compares a stated figure against the rows it claims to describe.

## Verdict checks

| ID | Check | Severity |
|---|---|---|
| `P1` | The metadata verdict and the recorded Decision state the same thing | Blocking |
| `P4` | An unqualified approval carries no unresolved critical or high finding | Blocking |
| `P6` | Approval is withheld when no test evidence was reviewed | Blocking |
| `R1` | The verdict is the one the Stage 8 table of `reasoning.md` yields | Blocking |
| `R2` | The package status agrees with the same table row that set the verdict | Blocking |
| `R3` | A `provisional` or `blocked` package records at least one open question | Blocking |

`R1` is the rule that stops a verdict from being chosen first and justified afterwards. The
table decides; the package records what it decided.

## Evidence checks

| ID | Check | Severity |
|---|---|---|
| `E1` | Every artifact named in `Evidence reviewed` was read in full during this run | Blocking |
| `E2` | Every confirmed result cites the command that produced it in this run | Blocking |
| `E3` | Every claimed result that was not confirmed is recorded as claimed, not as confirmed | Blocking |
| `E4` | Every changed behavior with no covering check appears in `Gaps requiring new tests` | Blocking |
| `E5` | Every part of the declared scope that was not examined appears in `Out of scope` or as unreviewed | Blocking |

## Authority checks

| ID | Check | Severity |
|---|---|---|
| `A1` | This agent authored neither the change nor the evidence under review | Blocking |
| `A2` | The package records no merge, release, or gate decision | Blocking |
| `A3` | The readiness recommendation is worded as a recommendation, not as a decision | Blocking |
| `A4` | No correction request supplies the implementation of the change it requires | Blocking |
| `A5` | No acceptance criterion, quality threshold, or declared invariant was relaxed | Blocking |
| `A6` | No production source, test file, committed run record, or governance record was written | Blocking |

`A1` is checked at Initialization and again here. A review that reaches Completion before
anyone notices it was not independent has already spent its authority.

## Content checks

| ID | Check | Severity |
|---|---|---|
| `C1` | No credential, token, or secret appears, including one copied from the reviewed diff | Blocking |
| `C2` | No security finding publishes a working exploitation path | Blocking |
| `C3` | No model, vendor, or agent-runtime name appears that the inputs and context did not already use | Blocking |
| `C4` | Identifiers use the zero-padded three-digit scheme, ascend from 001, and are unique | Correctable |
| `C5` | Every referenced identifier is defined in its declaring section | Blocking |
| `C6` | Every finding addresses the change rather than the author | Blocking |

## Rejection rules

The package is not emitted, and the run does not claim completion, when any of the following
holds:

1. A finding carries no requirement it is measured against.
2. A severity summary figure does not recount from the findings table.
3. An open critical or high finding carries no correction request.
4. An approval sits alongside an open critical or high finding, or alongside absent test evidence.
5. A verdict was recorded that the Stage 8 table did not yield.
6. The package awards a decision this agent does not own.
7. This agent produced the work or the evidence it is judging.

Each of these is a reason to change the review, never a reason to add a caveat and emit anyway.

## Repair procedure

1. Identify every failing check, not only the first.
2. Decide, per failure, whether the package is wrong or the review is wrong. A package edited
   to match a review that is actually wrong is the worse of the two repairs.
3. Repair, then re-run the entire check set. A repair can break a check that previously passed,
   and only a full re-run catches it.
4. A repair may never take the form of changing a severity to satisfy an arithmetic or verdict
   check. Those checks exist to catch exactly that repair.
5. If a failure cannot be repaired within this agent's authority, escalate it per `execution.md`
   and record the run as blocked.

## Not-machine-checkable obligations

Recorded, never silently skipped. Each is discharged by this agent's own judgement and stated in
the run's result envelope.

| ID | Obligation | Reference |
|---|---|---|
| `N1` | Every high-risk issue in the change under review was actually found | `identity.md#success-outcome` |
| `N2` | No finding substitutes reviewer opinion for a policy-backed standard | this module, Findings checks |
| `N3` | No severity was downscaled without evidence that lowered it | this module, Findings checks |

## Result envelope reporting

The result envelope carries: the counts of findings by severity, correction requests, and open
questions; the declared package status and the verdict; the pass or fail result of every check
in this module; and the three obligations above with the judgement made on each.
