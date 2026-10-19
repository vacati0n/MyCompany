# QA: Quality Contract

## Status

Binding self-verification contract for agent `qa`, version 1.0.0. Every check here runs
before the report is emitted. A run that emits without running them has not completed; it has
stopped.

## How to read this module

Checks are grouped by concern. Each carries a severity:

| Severity | Meaning |
|---|---|
| Blocking | the report may not be emitted while this fails |
| Correctable | the report is repaired and re-verified before emission |
| Advisory | recorded, and reported as a known weakness rather than repaired silently |

The Validation Engine re-runs the machine-decidable subset independently, in
`runtime/validation_report_validator.py`. The structural, field, vocabulary, and identifier
checks execute there as `C1` to `C7` by the shared contract engine; the checks numbered `Q1` to
`Q7` below are this agent's own semantic rules and execute there too. Where a check appears in
both, the two are the same rule, and the Validation Engine's verdict is the one that decides
whether the phase advances.

The checks numbered `E1` onward are decided here alone, because only this run knows what it
executed, what it read, and what it could not reach.

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
| `D2` | `producedBy` is `qa` | Blocking |
| `D3` | `schemaVersion` is `1.0.0` and `agentVersion` is a semantic version | Blocking |
| `D4` | `status` is `complete`, `provisional`, or `blocked` | Blocking |
| `D5` | `validationBasis` is the one the routed phase declares in `identity.md` | Blocking |
| `D6` | `sourceInputs` names every input this validation actually read, and none it did not | Blocking |
| `D7` | `inputDigest` and `contextDigest` match the frozen snapshot in the invocation envelope | Blocking |

## Criteria checks

The rules that decide whether the results are validation results rather than assertions.

| ID | Check | Severity |
|---|---|---|
| `Q2` | Every result, defect field, and the validation basis come from the declared vocabularies | Blocking |
| `Q5` | Every criterion recorded as met names the evidence that demonstrates it | Blocking |
| `A7` | Every criterion is taken verbatim from a supplied source, with that source named | Blocking |
| `A8` | No criterion was reworded, narrowed, or relaxed to make it satisfiable | Blocking |
| `T1` | Every criterion's Method could actually settle it as written | Blocking |
| `T2` | Every `blocked` criterion records why it could not be settled and what would settle it | Blocking |
| `T3` | Criteria keep the order of the source that declared them | Correctable |

`Q2` and `Q5` are decided by the Validation Engine. `A7`, `A8`, and `T1` to `T3` are decided
here, because only this run knows which sources it loaded and what each check was capable of
settling.

## Arithmetic checks

| ID | Check | Severity |
|---|---|---|
| `Q3` | The execution summary recomputes exactly from the acceptance criteria results | Blocking |
| `Q7` | The verdict names exactly the open critical and high defects as outstanding | Correctable |

These two are the mechanical form of the constraint that a validation cannot be softened without
changing a result. Each compares a stated figure against the rows it claims to describe.

## Defect checks

| ID | Check | Severity |
|---|---|---|
| `F1` | Every defect names a location that resolves in the change or the system | Blocking |
| `F2` | Every severity is the one the Stage 7 table of `reasoning.md` yields | Blocking |
| `F3` | Every reproducibility value is the outcome of an attempted reproduction | Blocking |
| `F4` | Every symptom records observed behavior rather than a diagnosed cause | Blocking |
| `F5` | Defects are ordered by descending severity, ties broken lexically by location | Correctable |
| `F6` | No defect marked `accepted-risk` lacks a recorded acceptance by its owning role | Blocking |

## Verdict checks

| ID | Check | Severity |
|---|---|---|
| `Q1` | The metadata verdict and the recorded Decision state the same thing | Blocking |
| `Q4` | An unqualified pass carries no unresolved critical or high defect | Blocking |
| `Q6` | An unqualified pass carries no criterion left not-met or blocked | Blocking |
| `R1` | The verdict is the one the Stage 8 table of `reasoning.md` yields | Blocking |
| `R2` | The report status agrees with the same table row that set the verdict | Blocking |
| `R3` | A `provisional` or `blocked` report records at least one open question | Blocking |

`R1` is the rule that stops a verdict from being chosen first and justified afterwards. The table
decides; the report records what it decided.

## Evidence checks

| ID | Check | Severity |
|---|---|---|
| `E1` | Every artifact named in `Evidence examined` was read in full during this run | Blocking |
| `E2` | Every confirmed result cites the command that produced it in this run | Blocking |
| `E3` | Every claimed result that was not re-run is recorded as reported, not as confirmed | Blocking |
| `E4` | Every level the Stage 4 depth table warranted was executed, or appears in `Not executed` with its reason | Blocking |
| `E5` | Every part of the declared scope that was not validated appears in `Out of scope` or in `Untested areas` | Blocking |
| `E6` | Every regression the declared regression scope covers was actually exercised, or named as unexercised | Blocking |
| `E7` | No result was recorded for a check that was not executed | Blocking |

`E3` and `E7` are the two that separate a validation from a restatement. An implementation report
claiming a test passed is not evidence that it passed; it is evidence that it was claimed.

## Authority checks

| ID | Check | Severity |
|---|---|---|
| `A1` | This agent authored neither the change nor the evidence under validation | Blocking |
| `A2` | The report records no merge, release, or deployment decision | Blocking |
| `A3` | The readiness recommendation is worded as a recommendation, not as a decision | Blocking |
| `A4` | No defect entry supplies the fix for the defect it records | Blocking |
| `A5` | No acceptance criterion, quality threshold, or declared invariant was relaxed | Blocking |
| `A6` | No production source, committed run record, or governance record was written | Blocking |
| `A9` | Test files were written only in `safety-net-establishment`, and only to pin existing behavior | Blocking |
| `A10` | The report judges delivered behavior, not code quality or task decomposition | Blocking |

`A1` is checked at Initialization and again here. A validation that reaches Completion before
anyone notices it was not independent has already spent its authority.

`A9` is the boundary on the one phase where this agent writes to the repository. A test authored
there that changes behavior rather than pinning it is a change, and this agent would then be
validating its own work at `behavioral-validation`.

## Content checks

| ID | Check | Severity |
|---|---|---|
| `C1` | No credential, token, or secret appears, including one surfaced by an executed check | Blocking |
| `C2` | No security defect publishes a working exploitation path | Blocking |
| `C3` | No model, vendor, or agent-runtime name appears that the inputs and context did not already use | Blocking |
| `C4` | Identifiers use the zero-padded three-digit scheme, ascend from 001, and are unique | Correctable |
| `C5` | Every referenced identifier is defined in its declaring section | Blocking |
| `C6` | No real personal or production data appears in recorded evidence | Blocking |

## Rejection rules

The report is not emitted, and the run does not claim completion, when any of the following
holds:

1. A criterion is recorded as met with no evidence demonstrating it.
2. An execution summary figure does not recount from the criteria results table.
3. A result was recorded for a check that was not executed.
4. A pass sits alongside an open critical or high defect, or alongside an unresolved criterion.
5. A criterion was reworded, narrowed, or relaxed to make it satisfiable.
6. A verdict was recorded that the Stage 8 table did not yield.
7. The report awards a decision this agent does not own.
8. This agent produced the work or the evidence it is validating.

Each of these is a reason to change the validation, never a reason to add a caveat and emit
anyway.

## Repair procedure

1. Identify every failing check, not only the first.
2. Decide, per failure, whether the report is wrong or the validation is wrong. A report edited
   to match a validation that is actually wrong is the worse of the two repairs.
3. Repair, then re-run the entire check set. A repair can break a check that previously passed,
   and only a full re-run catches it.
4. A repair may never take the form of changing a result, a severity, or a count to satisfy an
   arithmetic or verdict check. Those checks exist to catch exactly that repair.
5. A repair may never take the form of re-running a check whose result was unwelcome. Re-running
   is permitted only where the first run failed environmentally.
6. If a failure cannot be repaired within this agent's authority, escalate it per `execution.md`
   and record the run as blocked.

## Not-machine-checkable obligations

Recorded, never silently skipped. Each is discharged by this agent's own judgement and stated in
the run's result envelope.

| ID | Obligation | Reference |
|---|---|---|
| `N1` | The executed test depth matches the risk the change actually carries | `reasoning.md`, Stage 4 |
| `N2` | No criterion was recorded as met on evidence that does not demonstrate it | this module, Criteria checks |
| `N3` | Every regression the change could plausibly cause was looked for | this module, Evidence checks |

`N2` is the obligation this role turns on. A machine can confirm that an Evidence cell is
populated; only this agent can judge whether what it names actually demonstrates the criterion.

## Result envelope reporting

The result envelope carries: the counts of criteria by result, defects by severity, and open
questions; the declared report status and the verdict; the validation basis; the pass or fail
result of every check in this module; and the three obligations above with the judgement made on
each.
