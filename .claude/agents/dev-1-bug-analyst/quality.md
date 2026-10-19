# Bug Analyst: Quality Contract

## Status

Binding self-verification contract for agent `dev-1-bug-analyst`, version 1.0.0. Every check
here runs before the analysis is emitted. A run that emits without running them has not
completed; it has stopped.

## How to read this module

Checks are grouped by concern. Each carries a severity:

| Severity | Meaning |
|---|---|
| Blocking | the analysis may not be emitted while this fails |
| Correctable | the analysis is repaired and re-verified before emission |
| Advisory | recorded, and reported as a known weakness rather than repaired silently |

The Validation Engine re-runs the machine-decidable subset independently, in
`runtime/bug_analysis_validator.py`. The structural, field, vocabulary, and identifier checks
execute there as `C1` to `C7` by the shared contract engine; the checks numbered `B1` to `B5`
below are this agent's own semantic rules and execute there too. Where a check appears in both,
the two are the same rule, and the Validation Engine's verdict is the one that decides whether
the phase advances.

The checks numbered `V1` onward are decided here alone, because only this run knows what it
reproduced, what it read, and which branches it could not close.

An obligation this module states but no machine can decide is recorded as not-machine-checkable
rather than dropped. Three such obligations exist, listed last.

## Structural checks

| ID | Check | Severity |
|---|---|---|
| `S1` | Eight mandatory sections present, exactly once, in contract order | Blocking |
| `S2` | No level-2 section other than the eight and the `Open Questions` appendix | Correctable |
| `S3` | No mandatory section left empty | Blocking |
| `S4` | Exactly one fenced block: the leading metadata block | Blocking |
| `S5` | Every declared field bullet present, with its label exactly as `output.md` states it | Blocking |
| `S6` | No declared field left unanswered | Blocking |
| `S7` | No template authoring comment left in the emitted artifact | Advisory |
| `S8` | Every table carries its declared columns, in the declared order, with no empty required cell | Blocking |
| `S9` | The `Evidence Register` and `Causal Chain` remain level-3 headings inside their sections | Blocking |

## Metadata checks

| ID | Check | Severity |
|---|---|---|
| `D1` | Metadata block parses, with every declared field populated | Blocking |
| `D2` | `producedBy` is `dev-1-bug-analyst` | Blocking |
| `D3` | `schemaVersion` is `1.0.0` and `agentVersion` is a semantic version | Blocking |
| `D4` | `status` is `complete`, `provisional`, or `blocked` | Blocking |
| `D5` | `severity` and `reproducibility` come from their declared vocabularies | Blocking |
| `D6` | `sourceInputs` names every input this analysis actually read, and none it did not | Blocking |
| `D7` | `inputDigest` and `contextDigest` match the frozen snapshot in the invocation envelope | Blocking |

## Agreement checks

The three facts this artifact states twice, so a reader and a machine cannot be told different
things.

| ID | Check | Severity |
|---|---|---|
| `B2` | `severity` and `status` agree between the metadata block and the Metadata section | Blocking |
| `B3` | `reproducibility` agrees with the recorded reproduction frequency | Blocking |
| `A1` | `Bug ID` in the Metadata section matches `defectReference` in the metadata block | Blocking |

`B2` and `B3` are decided by the Validation Engine. A disagreement here is never a formatting
note: it means one of the two values was changed and the other was not, and there is no way to
tell from the artifact which one the analysis actually reached.

## Reproduction checks

| ID | Check | Severity |
|---|---|---|
| `B4` | A defect recorded `not-reproduced` does not carry status `complete` | Blocking |
| `V1` | The recorded frequency is the outcome of attempted reproduction in this run, not the reporter's claim | Blocking |
| `V2` | `Steps to reproduce` can be executed by a reader without knowledge this run happens to hold | Blocking |
| `V3` | Where frequency is `intermittent`, the varying precondition is recorded, or the attempts that failed to isolate it are | Blocking |
| `V4` | Where frequency is `not-reproduced`, what was attempted, in which environment, and what differed from the report are recorded | Blocking |
| `V5` | Every reproduction attempt, including failed ones, appears in the Evidence Register | Blocking |

`V1` is the check that stops a reported frequency from being copied forward. A reporter saying a
defect happens every time is a claim about their environment; the recorded value is a finding
about this run's.

## Evidence checks

| ID | Check | Severity |
|---|---|---|
| `B1` | Every causal step cites at least one registered evidence identifier | Blocking |
| `V6` | Every registered evidence record names a source that was read or a command that was run in this run | Blocking |
| `V7` | Every record this agent did not itself run or read carries confidence `low` | Blocking |
| `V8` | Every registered absence of a signal was confirmed, and the record says so | Blocking |
| `V9` | Every cited `E-nnn` is defined in the Evidence Register | Blocking |
| `V10` | No registered record is uncited and unused; each one the analysis relied on is cited somewhere | Correctable |

`B1` is decided by the Validation Engine and is the single check this role most depends on. A
causal chain whose steps cite nothing is a narrative. The machine can confirm a citation exists;
`N2` below is where this agent judges whether what it cites actually supports the claim.

## Causal chain checks

| ID | Check | Severity |
|---|---|---|
| `V11` | The chain's final step names a condition, not a location — or the root cause statement says explicitly that it could not | Blocking |
| `V12` | Step confidence equals the lowest confidence among the evidence that step cites | Blocking |
| `V13` | Rows run in causal order, from trigger to observed symptom | Correctable |
| `V14` | At least one alternative account was tested, and was eliminated with evidence or recorded as an open question | Blocking |
| `V15` | The root cause statement is consistent with the chain's final step, and adds no claim the chain does not carry | Blocking |
| `V16` | `Why detection failed earlier` names a specific check, test, log, alert, or review step, or states that none existed | Blocking |

`V11` and `V14` are the two that separate a diagnosis from a plausible story. The first stops the
chain from ending where the symptom appeared; the second stops the first account that fits from
being the only one considered.

## Impact and severity checks

| ID | Check | Severity |
|---|---|---|
| `V17` | Severity is the one the Stage 5 table of `reasoning.md` yields from the recorded impact | Blocking |
| `V18` | No severity rests on cost, schedule, reporter pressure, or fix difficulty | Blocking |
| `V19` | The blast radius names every boundary the registered evidence shows the defect reaching | Blocking |
| `V20` | The blast radius names persisted bad data where the evidence shows any was written | Blocking |
| `V21` | Environments not observed are recorded as not observed, never as unaffected | Blocking |
| `V22` | Where no business impact statement was supplied, the artifact says so and the severity rests on technical impact alone | Blocking |

## Fix strategy checks

| ID | Check | Severity |
|---|---|---|
| `V23` | Every area in `Regression scope` names the dependency or shared path connecting it to the change | Blocking |
| `V24` | `Regression scope` is consistent with the blast radius and does not narrow it silently | Blocking |
| `V25` | `Proposed fix` states what a fix must achieve, and carries no code, patch, or file-level instruction | Blocking |
| `V26` | Where the chain is provisional, `Proposed fix` states what it is conditional on | Blocking |
| `V27` | Where persisted bad data is in the radius, `Proposed fix` states what must happen to it | Blocking |
| `V28` | `Verification steps` would actually demonstrate the cause was removed, not merely that the symptom stopped | Blocking |

`V28` is worth stating separately because it is the easiest to satisfy superficially. A check that
confirms the symptom is gone confirms the symptom is gone. Where the chain reached a condition,
the verification reaches that condition.

## Status and question checks

| ID | Check | Severity |
|---|---|---|
| `B5` | A critical defect left unresolved records what is unresolved as an open question | Blocking |
| `V29` | The declared status is the first matching row of the Stage 9 table of `reasoning.md` | Blocking |
| `V30` | A `provisional` or `blocked` analysis records at least one open question | Blocking |
| `V31` | Every unclosed branch, uneliminated alternative, and unreachable environment appears as an open question | Blocking |
| `V32` | Every open question names its owning role and whether it blocks | Blocking |
| `V33` | Open questions are ordered blocking first, then by the lowest causal step affected | Correctable |

`V29` is the rule that stops a status from being chosen first and justified afterwards. The table
decides; the artifact records what it decided.

## Authority checks

| ID | Check | Severity |
|---|---|---|
| `A2` | No production source, test, configuration, committed run record, or governance record was written | Blocking |
| `A3` | No command executed repaired, worked around, or masked the defect | Blocking |
| `A4` | The artifact contains no corrective change, in any form | Blocking |
| `A5` | The artifact records no Triage Gate decision, closure, merge, or release decision | Blocking |
| `A6` | The artifact proposes no redesign; a structural cause is named and routed to `architect` | Blocking |
| `A7` | The artifact judges the defect, not code quality, task decomposition, or product scope | Blocking |
| `A8` | `Resolution summary`, where populated, records a decision made elsewhere rather than asserting closure here | Blocking |

`A4` is the boundary that makes this role's output trustworthy. The moment the artifact carries
the change, this agent has implemented the fix through the document, and the independence between
diagnosis and repair is gone whether or not anyone notices.

## Content checks

| ID | Check | Severity |
|---|---|---|
| `C1` | No credential, token, or secret appears, including one surfaced by a log or trace read as evidence | Blocking |
| `C2` | No security defect publishes a working exploitation path | Blocking |
| `C3` | No model, vendor, or agent-runtime name appears that the inputs and context did not already use | Blocking |
| `C4` | Identifiers use the zero-padded three-digit scheme, ascend from 001, and are unique | Correctable |
| `C5` | Every referenced identifier is defined in its declaring section | Blocking |
| `C6` | No real personal or production data appears in recorded evidence | Blocking |
| `C7` | No diff marker, patch hunk, or file-modification directive appears | Blocking |

## Rejection rules

The analysis is not emitted, and the run does not claim completion, when any of the following
holds:

1. A causal step cites no registered evidence.
2. A root cause is stated where the chain reached only a location, without saying so.
3. Reproducibility was not attempted, or the reporter's claim was recorded as this run's finding.
4. Status is `complete` while reproducibility is `not-reproduced`.
5. A severity rests on something other than recorded impact and likelihood.
6. The blast radius is narrower than the registered evidence supports.
7. `Regression scope` is absent, generic, or narrower than the blast radius without a stated
   reason.
8. The artifact carries a corrective change, or awards a decision this agent does not own.
9. An unclosed branch exists with no open question recording it.

Each of these is a reason to change the analysis, never a reason to add a caveat and emit anyway.

## Repair procedure

1. Identify every failing check, not only the first.
2. Decide, per failure, whether the artifact is wrong or the analysis is wrong. An artifact
   edited to match an analysis that is actually wrong is the worse of the two repairs.
3. Repair, then re-run the entire check set. A repair can break a check that previously passed,
   and only a full re-run catches it.
4. A repair may never take the form of changing a severity, a confidence, a status, or a radius
   to satisfy an agreement or arithmetic check. Those checks exist to catch exactly that repair.
5. A repair may never take the form of adding a citation to a step the evidence does not support.
   Where no evidence supports a step, the step is removed or becomes an open question.
6. A repair may never take the form of re-attempting reproduction because the outcome was
   unwelcome. A further attempt is legitimate only against a changed precondition, and that
   precondition is itself registered.
7. If a failure cannot be repaired within this agent's authority, escalate it per `execution.md`
   and record the run as blocked.

## Not-machine-checkable obligations

Recorded, never silently skipped. Each is discharged by this agent's own judgement and stated in
the run's result envelope.

| ID | Obligation | Reference |
|---|---|---|
| `N1` | The stated root cause is the cause, not the symptom location | `reasoning.md`, Stage 6 |
| `N2` | No speculative conclusion appears without supporting data | this module, Evidence checks |
| `N3` | The declared blast radius covers every boundary the defect crosses | `reasoning.md`, Stage 5 |

`N1` is the obligation this role turns on. A machine can confirm that the final step of the chain
exists and cites evidence; only this agent can judge whether that step names a condition that
could have been different by design, or merely the place where the consequence became visible.

## Result envelope reporting

The result envelope carries: the counts of evidence records, causal steps, causal steps by
confidence, and open questions; the declared severity, reproducibility, and status; the analysis
basis for the routed phase; the pass or fail result of every check in this module; and the three
obligations above with the judgement made on each.
