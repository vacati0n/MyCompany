# Context Agent: Quality Contract

## Status

Binding self-verification contract for agent `context-agent`, version 1.0.0. Every check here
runs before the report is emitted. A run that emits without running them has not completed; it has
stopped.

## How to read this module

Checks are grouped by concern. Each carries a severity:

| Severity | Meaning |
|---|---|
| Blocking | the report may not be emitted while this fails |
| Correctable | the report is repaired and re-verified before emission |
| Advisory | recorded, and reported as a known weakness rather than repaired silently |

The Validation Engine re-runs the machine-decidable subset independently, in
`runtime/investigation_report_validator.py`. The structural, field, vocabulary, and identifier checks
execute there as `C1` to `C7` by the shared contract engine; the checks numbered `I1` to `I6` below
are this agent's own semantic rules and execute there too. Where a check appears in both, the two are
the same rule, and the Validation Engine's verdict is the one that decides whether the phase
advances.

The checks numbered `X1` onward are decided here alone, because only this run knows which sources it
opened, what each one actually said, and what it could not reach.

An obligation this module states but no machine can decide is recorded as not-machine-checkable
rather than dropped. Three such obligations exist, listed last.

## Structural checks

| ID | Check | Severity |
|---|---|---|
| `S1` | Seven mandatory sections present, exactly once, in contract order | Blocking |
| `S2` | No level-2 section other than the seven and the two declared appendices | Correctable |
| `S3` | No mandatory section left empty | Blocking |
| `S4` | Exactly one fenced block: the leading metadata block | Blocking |
| `S5` | Every declared field bullet present, with its label exactly as `output.md` states it | Blocking |
| `S6` | No declared field left unanswered | Blocking |
| `S7` | No template authoring comment left in the emitted artifact | Advisory |
| `S8` | Every table carries its declared columns, in the declared order, with no empty required cell | Blocking |
| `S9` | `Evidence` carries at least two rows, and `Options Evaluated` at least two | Blocking |
| `S10` | Neither `Evidence` nor `Options Evaluated` uses the `None identified.` marker | Blocking |
| `S11` | Both declared appendices present, in contract order, each carrying content or `None identified.` | Blocking |

## Metadata checks

| ID | Check | Severity |
|---|---|---|
| `D1` | Metadata block parses, with every declared field populated | Blocking |
| `D2` | `producedBy` is `context-agent` | Blocking |
| `D3` | `schemaVersion` is `1.0.0` and `agentVersion` is a semantic version | Blocking |
| `D4` | `status` is `complete`, `provisional`, or `blocked` | Blocking |
| `D5` | Every `sourceInputs` entry uses the declared type vocabulary and carries a reference | Blocking |
| `D6` | `sourceInputs` names every input this run actually read, and none it did not | Blocking |
| `D7` | `inputDigest` and `contextDigest` match the frozen snapshot in the invocation envelope | Blocking |

## Evidence checks

The rules that decide whether the report carries evidence rather than assertions.

| ID | Check | Severity |
|---|---|---|
| `I3` | Every observation is confidence-marked with `high`, `medium`, or `low` | Blocking |
| `I4` | Every observation states its staleness, `current` or otherwise | Blocking |
| `I6` | The report-level confidence uses the declared vocabulary | Correctable |
| `X1` | Every source named in the `Source` column was read in this run | Blocking |
| `X2` | Every `Source` cell resolves: a reader can open it and find the element named | Blocking |
| `X3` | Every confidence value is the one the Stage 4 table of `reasoning.md` yields | Blocking |
| `X4` | Every staleness value is what the source supports, with `unknown` used rather than an inferred date | Blocking |
| `X5` | The recorded scope is the scope fixed at Stage 2, not the scope that was reached | Blocking |
| `X6` | Every source that was read and bears on the question is recorded, including the inconvenient ones | Blocking |
| `X7` | No observation restates another observation in order to appear corroborated | Blocking |

`I3` and `I4` are decided by the Validation Engine; the `X` checks are decided here, because only
this run knows what it opened. `X6` is the check against selective reading: a report may be
faultless in every row it carries and still mislead by which rows it left out.

## Inference checks

| ID | Check | Severity |
|---|---|---|
| `X8` | No row of the evidence table is a conclusion drawn from other rows | Blocking |
| `X9` | Every element of the reconstructed context traces to an observation | Blocking |
| `X10` | Every assumption is labelled as an assumption, never as a constraint or an observation | Blocking |
| `X11` | Stage 5 was run in full: every row was tested against the question "did a source state this?" | Blocking |

`X8` is the mechanical form of obligation `N3`. It is stated as a check as well as an obligation
because the two catch different things: the check catches the row that visibly derives from two
others, and the obligation covers the one that does not show its working.

## Option and recommendation checks

| ID | Check | Severity |
|---|---|---|
| `I1` | The recommended option names an option evaluated in this report | Blocking |
| `I2` | Every evaluated option cites the evidence it rests on | Blocking |
| `X12` | Every cited evidence identifier is defined in the evidence table | Blocking |
| `X13` | Every option is a course of action, carrying no implementation approach | Blocking |
| `X14` | Options are ordered by descending evidential support, ties by lower identifier | Correctable |
| `X15` | The recommendation rests only on observations the named option cites | Blocking |
| `X16` | Where the evidence favours no option clearly, the recommendation says so | Blocking |
| `X17` | Every effort value is what the evidence characterises, or a statement that it supports none | Blocking |

`X15` is the check that stops the recommendation from being reached first and cited afterwards. A
rationale that leans on an observation the option does not cite is either a citation missing from the
option row or a conclusion that outran its evidence, and both are repaired at the source rather than
in the prose.

## Reconciliation checks

| ID | Check | Severity |
|---|---|---|
| `I5` | Contradictions and gaps are recorded, explicitly as none where there are none | Blocking |
| `X18` | Every contradiction names both observation identifiers, and says whether it stands | Blocking |
| `X19` | No contradiction was resolved by preferring recency, authority, or convenience alone | Blocking |
| `X20` | Every gap names what would close it | Blocking |
| `X21` | Every stale assumption a source carries is named stale, with what supersedes it | Blocking |

## Status and confidence checks

| ID | Check | Severity |
|---|---|---|
| `X22` | The declared report confidence is the one the Stage 10 table yields | Blocking |
| `X23` | The declared status is the one the Stage 10 status table yields | Blocking |
| `X24` | A `provisional` or `blocked` report records at least one open question | Blocking |
| `X25` | Every part of the declared scope that was not examined is named in the report | Blocking |
| `X26` | Every source named but unreachable is recorded as unreachable | Blocking |

`X22` and `X23` are the rules that stop a confidence from being chosen to suit the reader. The tables
decide; the report records what they decided. A decision needing more confidence than the evidence
carries needs more evidence, and `Follow-up validation` is where that is said.

## Authority checks

| ID | Check | Severity |
|---|---|---|
| `A1` | The report records no gate, merge, release, or deployment decision | Blocking |
| `A2` | The recommendation is worded as a reading of the evidence, not as a choice made | Blocking |
| `A3` | No technical design, architecture decision, code, test, or migration appears | Blocking |
| `A4` | No task breakdown, sequence, or effort estimate this agent produced appears | Blocking |
| `A5` | No product scope was defined, widened, or narrowed | Blocking |
| `A6` | No diagnosed root cause appears; observations bearing on a defect are recorded as evidence | Blocking |
| `A7` | No repository file was written other than the artifact and the result envelope | Blocking |
| `A8` | No command was executed, and nothing was established by execution | Blocking |
| `A9` | No governance record, committed run evidence, or ownership declaration was modified | Blocking |

`A2` is a wording check with substance behind it. "The evidence favours `O-002`" is a reading.
"We will take `O-002`" is a decision this agent does not hold, and the gate that receives it has
been handed a conclusion in place of the evidence it was convened to assess.

## Content checks

| ID | Check | Severity |
|---|---|---|
| `C1` | No credential, token, or secret appears, including one encountered in a source | Blocking |
| `C2` | No exploitable detail appears beyond what the observation requires | Blocking |
| `C3` | No model, vendor, or agent-runtime name appears that a cited source does not use | Blocking |
| `C4` | Identifiers use the zero-padded three-digit scheme, ascend from 001, and are unique | Correctable |
| `C5` | Every referenced identifier is defined in its declaring section | Blocking |
| `C6` | No real personal or production data appears as evidence | Blocking |

## Rejection rules

The report is not emitted, and the run does not claim completion, when any of the following holds:

1. An observation carries no source, or a source that cannot be opened.
2. An inference appears as a row of the evidence table.
3. A confidence or staleness value is not the one its table yields.
4. An option cites no evidence, or fewer than two options are recorded.
5. The recommendation names an option this report did not evaluate.
6. A contradiction was resolved by preference, or left unrecorded.
7. A gap was closed with an assumption.
8. The declared confidence or status is not the one the Stage 10 tables yield.
9. The report awards a decision this agent does not own.
10. Part of the declared scope was unexamined and the report does not say so.

Each of these is a reason to change the discovery, never a reason to add a caveat and emit anyway.

## Repair procedure

1. Identify every failing check, not only the first.
2. Decide, per failure, whether the report is wrong or the discovery is wrong. A report edited to
   match a discovery that is actually wrong is the worse of the two repairs.
3. Repair, then re-run the entire check set. A repair can break a check that previously passed, and
   only a full re-run catches it.
4. A repair may never take the form of raising a confidence, softening a staleness, or dropping an
   inconvenient observation to satisfy a status, confidence, or recommendation check. Those checks
   exist to catch exactly that repair.
5. A repair may never take the form of reaching for a more agreeable source. Reading further is
   permitted only where a source is genuinely authoritative for a fact still in dispute, and the new
   reading is recorded as its own observation with its own marking.
6. If a failure cannot be repaired within this agent's authority, escalate it per `execution.md` and
   record the run as `provisional` or `blocked`.

## Not-machine-checkable obligations

Recorded, never silently skipped. Each is discharged by this agent's own judgement and stated in the
run's result envelope. These three are the obligations the Validation Engine declares as `N1` to `N3`
for this artifact type.

| ID | Obligation | Reference |
|---|---|---|
| `N1` | The reconstructed current state matches the source it was read from | `reasoning.md`, Stages 4 and 7 |
| `N2` | Every stale assumption the sources contain is named as stale | `reasoning.md`, Stage 6 |
| `N3` | No inference is presented as an observation | `reasoning.md`, Stage 5 |

`N3` is the obligation this role turns on. A machine can confirm that a `Source` cell is populated and
that a row's identifier is well formed; only this agent can judge whether the sentence in the
`Observation` column is what the source said or what this run concluded from it.

## Result envelope reporting

The result envelope carries: the counts of observations by confidence, of observations by staleness
class, of options, of contradictions, of gaps, and of open questions; the declared report status and
confidence; the discovery basis; the sources read and the sources found unreachable; the pass or fail
result of every check in this module; and the three obligations above with the judgement made on each.
