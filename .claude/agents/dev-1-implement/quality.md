# Implementation Developer: Quality Contract

## Status

Binding self-verification contract for agent `dev-1-implement`, version 1.0.0. Every
check here runs before the report is emitted. A run that emits without running them has
not completed; it has stopped.

## How to read this module

Checks are grouped by concern. Each carries a severity:

| Severity | Meaning |
|---|---|
| Blocking | the report may not be emitted while this fails |
| Correctable | the report is repaired and re-verified before emission |
| Advisory | recorded, and reported as a known weakness rather than repaired silently |

The Validation Engine re-runs the machine-decidable subset independently, in
`runtime/implementation_report_validator.py`. The structural, field, vocabulary, and
identifier checks are executed there as `C1` to `C7` by the shared contract engine; the
checks numbered `M1` to `M7` below are this agent's own semantic rules. Where a check
appears in both, the two are the same rule, and the Validation Engine's verdict is the one
that decides whether the phase advances.

An obligation this module states but no machine can decide is recorded as
not-machine-checkable rather than dropped. Four such obligations exist, listed last.

## Structural checks

| ID | Check | Severity |
|---|---|---|
| `S1` | Nine mandatory sections present, exactly once, in contract order | Blocking |
| `S2` | No level-2 section other than the nine and the `Open Questions` appendix | Correctable |
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
| `D2` | `producedBy` is `dev-1-implement` | Blocking |
| `D3` | `schemaVersion` is `1.0.0` and `agentVersion` is a semantic version | Blocking |
| `D4` | `status` is `complete`, `provisional`, or `blocked` | Blocking |
| `D5` | `workflowPhase` is one of the three phases this agent owns | Blocking |
| `D6` | `inputDigest` and `contextDigest` match the frozen snapshot in the invocation envelope | Blocking |

## Evidence checks

The rules that decide whether the report's claims are supported by what was executed.

| ID | Check | Severity |
|---|---|---|
| `M1` | Every change-set entry `C-nnn` is cited by at least one test-evidence `Covers` cell | Blocking |
| `M5` | Every test-evidence row uses the declared `Type` and `Result` vocabularies | Blocking |
| `M7` | The declared verification status agrees with the unverified areas the report names | Blocking |
| `E1` | Every `Command` cell names a command that was actually executed in this run | Blocking |
| `E2` | Every `Result` cell records what that command returned, never a prediction | Blocking |
| `E3` | The regression baseline established in Context Loading was re-run and compared | Blocking |
| `E4` | A pre-existing failure is recorded as a residual risk, with the baseline as its evidence | Correctable |

`M1`, `M5`, and `M7` are decided by the Validation Engine. `E1` to `E4` are decided here,
because only this run knows which commands it ran.

## Completion rule

| ID | Check | Severity |
|---|---|---|
| `M3` | A report at status `complete` records no `fail` and no `not-run` result, and claims `verified` | Blocking |
| `M2` | `workflowPhase`, `status`, and `verificationStatus` agree between the metadata block and the Metadata section | Blocking |
| `K1` | A `provisional` or `blocked` report records at least one open question | Blocking |
| `K2` | The verification status is the one the Stage 7 table of `reasoning.md` yields | Blocking |

`K2` is the rule that stops a status from being chosen first and justified afterwards. The
table decides; the report records what it decided.

## Deviation checks

| ID | Check | Severity |
|---|---|---|
| `M4` | Every deviation cites the change-set entry that embodies it | Blocking |
| `V1` | Every deviation names an escalation, or `not-required` when it sits inside this agent's latitude | Blocking |
| `V2` | Every departure that changes what was accepted names a raised escalation, never `not-required` | Blocking |
| `V3` | Every change-set entry whose Design Ref names no accepted element is covered by a deviation | Blocking |

## Boundary checks

| ID | Check | Severity |
|---|---|---|
| `B1` | Every file this change touched appears in the Change Set, and every file this invocation wrote appears in `Declared side effects` | Blocking |
| `B2` | No file was written outside the change set and the envelope's `permitted_writes` | Blocking |
| `B3` | No module boundary or layering rule named by the accepted change was crossed | Blocking |
| `B4` | No existing test was disabled, skipped, or weakened to obtain a passing result | Blocking |
| `B5` | No credential, token, or secret appears in the change or in the report | Blocking |
| `B6` | No committed run evidence and no governance record was modified; a registry or workflow edit the accepted design names is inside scope, and is declared | Blocking |
| `B7` | No change removes or weakens input validation at a trust boundary, error handling that prevents data loss, authorization or audit paths, accessibility, required observability, data integrity, or the tests that prove the change | Blocking |

## Authority checks

| ID | Check | Severity |
|---|---|---|
| `M6` | The report records no review verdict and no merge or release readiness claim | Blocking |
| `A1` | `Review status` is `pending-review` | Blocking |
| `A2` | No acceptance criterion, quality threshold, or declared invariant was relaxed | Blocking |
| `A3` | No scope decision, design revision, or gate decision was recorded by this agent | Blocking |

## Content checks

| ID | Check | Severity |
|---|---|---|
| `C1` | No diff, patch, hunk, or file-modification directive appears in the artifact | Blocking |
| `C2` | No model, vendor, or agent-runtime name appears that the inputs and context did not already use | Blocking |
| `C3` | Identifiers use the zero-padded three-digit scheme, ascend from 001, and are unique | Correctable |
| `C4` | Every referenced identifier is defined in its declaring section | Blocking |
| `C5` | `Reviewer focus areas` names at least one site carrying a deviation, boundary, or residual risk, when any exists | Correctable |

## Rejection rules

The report is not emitted, and the run does not claim completion, when any of the following
holds:

1. A change-set entry has no covering test-evidence entry.
2. A result was reported that no command in this run produced.
3. The status is `complete` while any evidence is failing or unrun.
4. A departure from the accepted design is present but unrecorded.
5. A file was written that the report does not declare.
6. The report awards a verdict this agent does not own.

Each of these is a reason to change the report or the change, never a reason to add a
caveat and emit anyway.

## Repair procedure

1. Identify every failing check, not only the first.
2. Decide, per failure, whether the report is wrong or the change is wrong. A report edited
   to match a change that is actually wrong is the worse of the two repairs.
3. Repair, then re-run the entire check set. A repair can break a check that previously
   passed, and only a full re-run catches it.
4. If a failure cannot be repaired within this agent's authority, escalate it per
   `execution.md` and record the run as blocked.

## Not-machine-checkable obligations

Recorded, never silently skipped. Each is discharged by this agent's own judgement and
stated in the run's result envelope.

| ID | Obligation | Reference |
|---|---|---|
| `N1` | Each change-set row describes the change actually made at that path | `output.md#change-set` |
| `N2` | Each test-evidence row exercises the change-set entries its `Covers` cell names | this module, Evidence checks |
| `N3` | No hidden side effect was introduced into critical business logic | this module, Boundary checks |
| `N4` | Every new abstraction, file, or dependency the change introduces is justified by the rung of the necessity and reuse ladder that required it, recorded in `Approach taken` | `output.md#implementation-summary` |

## Result envelope reporting

The result envelope carries: the counts of change-set entries, test-evidence entries,
deviations, residual risks, and open questions; the declared status and verification status;
the pass or fail result of every check in this module; and the four obligations above with
the judgement made on each.
