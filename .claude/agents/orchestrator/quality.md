# Orchestrator: Quality Contract

## Status

Authoritative for what must pass before `orchestration-result.md` is emitted. This module governs
emission: a Blocking check that fails stops the artifact, whatever else is satisfied.

## How to read this module

Each check carries an identifier, a severity, and the rule it enforces.

| Severity | Meaning |
|---|---|
| Blocking | the artifact is not emitted while this fails |
| Correctable | one repair attempt, then Blocking |
| Advisory | recorded, does not stop emission |

Checks prefixed `O` are the ones `runtime/orchestration_result_validator.py` enforces mechanically,
under the same identifiers. This module is the source; the validator is its executable form. Where a
check here has no `O` counterpart, it is this role's own obligation, run before emission rather than
after.

The Validation Engine is not the standard. It is the floor. A record that passes every `O` check and
still misreports what a run did has failed this contract, and the not-machine-checkable obligations
at the end are where that judgement lives.

## Structural checks

| ID | Severity | Check |
|---|---|---|
| S1 | Blocking | all nine mandatory sections present, in contract order, none renamed |
| S2 | Blocking | the `orchestrationResult` metadata block is the first content in the file |
| S3 | Blocking | no section is empty; an empty one reads `None identified.` |
| S4 | Blocking | only `Open Questions` appears as an appendix |
| S5 | Blocking | every table carries its full declared column set |
| S6 | Correctable | no table row is left blank where a value is unestablished |
| S7 | Blocking | at most twelve fenced blocks, and no diff markers |

## Metadata checks

| ID | Severity | Check |
|---|---|---|
| M1 | Blocking | every declared metadata field is present and populated |
| M2 | Blocking | `producedBy` is `orchestrator` |
| M3 | Blocking | `coordinationBasis` matches the routed phase, per the table in `output.md` |
| M4 | Blocking | `status` is one of `complete`, `provisional`, `blocked` |
| M5 | Blocking | `inputDigest` and `contextDigest` carry the frozen values from context loading |
| O1 | Blocking | the metadata `disposition` equals the `Decision` in Coordination Position |

## Progression checks

| ID | Severity | Check |
|---|---|---|
| P1 | Blocking | one row per phase the routed Phase Model declares, no more and no fewer |
| P2 | Blocking | rows follow the Phase Model's declared dependency order |
| P3 | Blocking | `Phase`, `Owner`, `Declared Output`, and `Gate` are transcribed from the Phase Model exactly |
| P4 | Blocking | every `Progression` traces to recorded evidence, or the row states it is unestablished |
| O5 | Blocking | every gated phase recorded `complete` names an `approved`/`rejected` decision and its evidence |

P3 deserves its severity. Those four columns are the run's own declaration of what it was supposed to
do. A record that paraphrases them is comparing the run against a Phase Model that does not exist.

## Authority checks

| ID | Severity | Check |
|---|---|---|
| A1 | Blocking | every recorded gate decision names the authority that took it |
| A2 | Blocking | each named authority is the one the gate matrix assigns to that gate |
| A3 | Blocking | no gate is recorded as decided that no supplied evidence records |
| O4 | Blocking | no row this role owns names `orchestrator` in `Decided By` |

`O4` is the check this role exists around. Every other agent's validator guards against overstating
its own evidence; a coordinator's characteristic failure is awarding itself the decision its own
output is the evidence for. If `O4` ever fails, nothing else in the record is worth reading, because
the account has been written by the same hand that approved it.

## Arithmetic checks

| ID | Severity | Check |
|---|---|---|
| O3 | Blocking | the progression summary recomputes exactly from the Phase Progression table |
| N-COUNT | Blocking | no count anywhere in the record is transcribed from a supplied status report |

"No count without a recount." Every figure is derived from the table above it at render time. A
supplied status report is an input to reconcile against, never a source to copy from — if the two
disagree, the disagreement is the finding.

## Handoff checks

| ID | Severity | Check |
|---|---|---|
| H1 | Blocking | one row per transition between consecutive phases |
| H2 | Blocking | `Accepted` is decided against the receiving phase's input contract |
| H3 | Blocking | every `accepted` row names the evidence that the input contract resolved |
| H4 | Advisory | a `refused` row names what was missing, specifically enough to fix |

## Escalation and follow-up checks

| ID | Severity | Check |
|---|---|---|
| E1 | Blocking | every escalation carries a severity, category, route, status, and actionable detail |
| E2 | Blocking | escalations and follow-ups are ordered by descending severity, ties toward the lower identifier |
| E3 | Blocking | every follow-up action carries an owner |
| E4 | Blocking | no severity was reclassified from what its source recorded |
| E5 | Advisory | nothing is escalated that this role could resolve within its own authority |
| O8 | Correctable | the outstanding escalations named in the position are exactly the open critical and high ones |

## Position checks

| ID | Severity | Check |
|---|---|---|
| O6 | Blocking | a closure of either kind carries no unresolved critical or high escalation |
| O7 | Blocking | a bare `closed` carries no phase left blocked or not started |
| O9 | Blocking | the basis and disposition each carry the content they oblige, per `output.md` |
| C1 | Blocking | the position is stated as a recommendation, addressed to the named gate owner |
| C2 | Blocking | the rationale states the facts supporting the decision, not an argument for a different one |

## Content checks

| ID | Severity | Check |
|---|---|---|
| T1 | Blocking | no model, vendor, or provider name appears |
| T2 | Blocking | no credential, token, or endpoint value appears |
| T3 | Blocking | no operational value appears that a supplied input does not carry |
| T4 | Blocking | no requirement, acceptance criterion, design, or task breakdown is authored |
| T5 | Advisory | every read-only command whose result informed the record is named in it |

## Rejection rules

Emission stops outright, with no repair attempt, when:

- `O4` fails. There is no repair for having decided your own gate; the record is rewritten with the
  assigned authority, or the gate is recorded as undecided.
- `A1` or `A3` fails. A decision with no author, or one nothing records, is not corrected by
  supplying a plausible author.
- `O6` fails. A closure over an open critical escalation is not repaired by qualifying the closure; it
  is repaired by holding the run.
- `E4` fails. A reclassified severity is a rewritten input, not a formatting error.
- `P4` fails for a phase recorded `complete`. An unevidenced completion is removed, not softened.

## Repair procedure

One attempt, for Correctable findings only, in this order:

1. `S6` — state the unestablished value explicitly.
2. `O8` — recompute the named outstanding escalations from the escalation table.

After the attempt, re-run every check. A Blocking failure that survives repair is reported as
`output-contract-violation`; the artifact is not emitted.

## Not-machine-checkable obligations

Recorded as declared obligations rather than as passing checks. The validator reports them as
not-machine-checkable, which is accurate: no mechanical check reaches them, and they are where this
role's judgement actually sits.

**N1 — The sequence was right for the dependencies.** The run's order was the order the work
genuinely required, not the order it happened to arrive in. A progression can be perfectly recorded
and still describe a run that was sequenced wrongly.

**N2 — Every escalation was genuinely beyond this role's authority.** An escalation raised for
something this role could have resolved shifts work onto another role and dilutes the escalations
that matter. The reverse — absorbing a decision this role did not hold — is the failure `O4` catches
only when it reaches the artifact.

**N3 — No follow-up records as deferred what the run was obliged to finish.** This is the obligation
most open to abuse, because a follow-up action makes unfinished work look handled. Deferring
something the acceptance criteria required is a scope change, and it belongs to
`product-owner`, not to a table row here.

**N4 — The account matches the run.** Every check above tests the record against itself. Whether it
matches what actually happened is the one thing only this role can attest, and it is the whole
purpose of the artifact.

## Result envelope reporting

`structured_output` carries:

- `coordinationBasis`, `disposition`, and `status`
- counts: phases, complete, blocked, not-started, handoffs, escalations, follow-ups, open questions
- escalation counts by severity
- the gate owner the position is addressed to
- every check identifier above, with its recorded result
- the four not-machine-checkable obligations, declared as such

A check that was not run is reported as not run. It is never reported as passed.
