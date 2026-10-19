# Business Analyst: Quality Contract

## Status

Binding self-verification contract for `business-analyst`, version 1.0.0.

Every check below runs against the rendered `requirement-framing.md` before the run reports
completion. The result of every check is recorded in the result envelope, whether it passed
or failed.

## How to read this module

Each check carries an identifier, a severity, and the rule it enforces.

| Severity | Meaning |
|---|---|
| Blocking | The artifact does not conform. It is repaired, or the run reports `provisional` or `blocked`. |
| Correctable | The artifact is repaired in place and the checks re-run. A surviving failure lowers the verdict but does not by itself block. |
| Advisory | Recorded, never blocking. |

The Validation Engine (`../../runtime/requirement_framing_validator.py`) decides the subset of
these rules that is decidable by inspecting the file. The remainder is listed under
Not-machine-checkable obligations and is the agent's own responsibility, recorded rather than
assumed.

Machine-checked rules carry the identifier the validator reports: `C*` for the shared contract
engine, `RF*` for the rules specific to this artifact.

## Structural checks

| ID | Severity | Rule |
|---|---|---|
| C1 | Blocking | All nine mandatory sections are present, correctly titled, and in contract order |
| C2 | Blocking | Exactly one fenced block — the metadata — appears in the artifact |
| C3 | Blocking | Every mandatory section is non-empty, or carries the explicit `None identified.` marker where that is permitted |
| C4 | Blocking | Target Outcomes, Requirements, and Acceptance Intent each carry at least one row |
| C5 | Blocking | Every declared table carries its declared columns, spelled as the template spells them |
| C6.1 | Correctable | Identifiers use the zero-padded three-digit scheme |
| C6.2 | Blocking | Every referenced identifier is defined in its declaring section |
| C6.3 | Correctable | Identifiers are contiguous from `001` within each scheme |

## Metadata checks

| ID | Severity | Rule |
|---|---|---|
| C7 | Blocking | Every metadata key is present and populated |
| C8 | Blocking | `producedBy` is `business-analyst` |
| C9 | Blocking | `status` is one of `complete`, `provisional`, `blocked` |
| RF1 | Blocking | `framingVerdict` is one of `framed`, `partially-framed`, `blocked` |
| RF2 | Blocking | `requirementCount` equals the number of requirements recorded |

`RF2` exists because the count is one fact. A downstream phase that plans against the
metadata and a gate that reads the table must be counting the same requirements.

## Outcome alignment

| ID | Severity | Rule |
|---|---|---|
| RF3 | Blocking | Every requirement names a target outcome defined in Target Outcomes |
| RF4 | Blocking | Every requirement declares its type as `functional` or `non-functional` |

A requirement that serves no declared outcome is either an outcome the framing failed to
declare or a requirement belonging to another change. Either way the reader cannot tell what
it is for, so it is not recorded as settled.

## Testability

| ID | Severity | Rule |
|---|---|---|
| RF5 | Blocking | Every acceptance intent names a requirement defined in Requirements |
| RF6 | Blocking | Every requirement is referenced by at least one acceptance intent |

`RF6` is the testability floor. A requirement nothing would demonstrate cannot be accepted,
validated, or disputed; it is an open question wearing a requirement's clothes, and it belongs
in Open Questions instead.

## Framing integrity

| ID | Severity | Rule |
|---|---|---|
| RF7 | Blocking | A `partially-framed` or `blocked` verdict records at least one open question |
| RF8 | Correctable | A `framed` verdict names at least one framing boundary |

`RF7` catches the failure this role exists to prevent: a framing that reports itself
incomplete while recording nothing that is missing has hidden the gap rather than named it.

`RF8` is correctable rather than blocking because a framing with no exclusion is more often
an omission than a falsehood — but a framing whose boundary excludes nothing has described a
wish rather than drawn a line.

## Assumption checks

| ID | Severity | Rule |
|---|---|---|
| RF9 | Blocking | Every assumption carries a basis a reader can assess |
| RF10 | Correctable | Every assumption declares a confidence of `high`, `medium`, or `low` |

An assumption with no basis is an invented requirement in disguise, which is the one output
this role must never produce. Where no basis can be stated, the correct record is an open
question, not an assumption.

## Authority boundary

| ID | Severity | Rule |
|---|---|---|
| RF11 | Blocking | The artifact issues no task, change-set, or architecture-decision identifier |

The identifier schemes owned downstream are `T-nnn` (planner task breakdown), `C-nnn`
(implementation change set), and `ADR-nnn` (architecture decision record). A framing that
issues one has decided work it does not own — which is exactly the boundary the role
specification draws when it forbids bypassing architecture decision ownership and writing
production code.

## Content checks

| ID | Severity | Rule |
|---|---|---|
| C10 | Blocking | No template comment survives into the rendered artifact |
| C11 | Correctable | No declared field bullet is left empty |
| C12 | Blocking | No model, vendor, or agent-runtime name appears |

## Rejection rules

The artifact is not released, and the run does not report `framed`, when any of the
following holds:

1. A blocking check fails and cannot be repaired.
2. A requirement is recorded that no supplied input supports.
3. Two recorded requirements cannot both be satisfied.
4. A mechanism is recorded as though it were a requirement.
5. A known edge case was identified during Stage 4 and left out of the record.
6. An acceptance threshold or verification procedure was written into the artifact.
7. A gate decision on this artifact was recorded.
8. The verdict claims more than the recorded evidence supports.

## Repair procedure

Repair is bounded. It fixes the record; it never improves the verdict.

1. Re-render any section that failed a structural check, against the template.
2. Reconcile `requirementCount` to the table, never the table to the count.
3. Move an untestable requirement out of Requirements and into Open Questions; renumber
   nothing already assigned.
4. Move an unsupported assumption into Open Questions.
5. Re-run the full check set after every repair.
6. Where a blocking failure survives repair, lower the verdict and report the reason. Never
   raise the verdict to whatever currently passes.

## Not-machine-checkable obligations

These are the rules a file cannot decide about itself. Each is the agent's own obligation and
is recorded in the result envelope as satisfied or not.

| ID | Reference | Obligation |
|---|---|---|
| RN1 | `output.md#requirements` | Each requirement is one the supplied inputs actually support, not one inferred for the business |
| RN2 | `output.md#requirements` | The requirement set covers the known edge cases of the intent, with none silently dropped |
| RN3 | `output.md#target-outcomes` | The declared outcomes are the outcomes the requester holds, not ones substituted for them |
| RN4 | `output.md#acceptance-intent` | Each acceptance intent describes something acceptance could genuinely demonstrate |

## Result envelope reporting

`structured_output` carries:

- `framingVerdict`, and the `status` it pairs with
- counts: outcomes, requirements, requirements by type, acceptance intent, boundaries,
  assumptions, open questions, blocking open questions
- every check identifier above with its result
- every not-machine-checkable obligation with its self-reported result
- the identifiers of every item moved during repair, and the reason

A check that was not run is reported as not run. It is never reported as passed.
