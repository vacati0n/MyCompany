# Bug Analyst: Output Contract

## Status

Binding structural and semantic contract for `bug-analysis.md`, produced by agent
`dev-1-bug-analyst`, version 1.0.0, schema version 1.0.0.

The rendered form is `templates/bug-analysis.md`. This module is the contract; the template is
the shape it renders to. The Validation Engine decides conformance in
`runtime/bug_analysis_validator.py`.

## Deliverable

One artifact per run: `bug-analysis.md`, written at the artifact path the phase declares.

One artifact type carries both diagnostic phases:

| Workflow | Phase | Analysis basis | Depth required |
|---|---|---|---|
| `fix-bug` | `triage-and-impact` | `triage` | severity, blast radius, and reproducibility settled; causal chain may remain provisional |
| `fix-bug` | `root-cause-analysis` | `root-cause` | causal chain closed on a condition, or the reason it could not be |

The basis is recorded in the result envelope's `structured_output`, not in the artifact. The
metadata schema below is fixed by the template and this agent does not extend it.

Where both phases run in one run, the second renders a complete artifact that supersedes the
first. It does not render a delta, and it does not contradict a fact the first recorded without
registering the evidence that overturns it.

## Structural Contract

Eight mandatory level-2 sections, in this exact order, each present exactly once:

| # | Section | Content form |
|---|---|---|
| 1 | `Metadata` | four declared field bullets |
| 2 | `Symptom Summary` | four declared field bullets |
| 3 | `Reproduction` | four declared field bullets, then the `Evidence Register` table, at least one row |
| 4 | `Impact Assessment` | four declared field bullets |
| 5 | `Root Cause Analysis` | two declared field bullets, then the `Causal Chain` table, at least one row |
| 6 | `Fix Strategy` | four declared field bullets |
| 7 | `Validation Plan` | three declared field bullets |
| 8 | `Closure` | three declared field bullets |

One optional appendix is permitted: `Open Questions`. No other level-2 section may appear.

The `Evidence Register` and `Causal Chain` are level-3 headings inside sections 3 and 5. They are
not sections in their own right and are never promoted, moved, or split out.

Sections are never omitted and never left empty. A section with nothing to report reads
`None identified.` — including `Closure`, which is unresolved at the time most analyses are
written and states that plainly rather than disappearing.

### Fenced blocks

Exactly one: the leading metadata block. This artifact is read by an implementer who will change
code, so a pasted log, a quoted trace, or a fenced excerpt would put text next to a fix that
looks like an instruction to apply. Evidence is described in the register and cited by
identifier; it is not reproduced inline.

For the same reason no diff marker, patch hunk, or file-modification directive may appear. This
artifact states what is wrong and what a fix must achieve. The moment it carries the change
itself, this role has implemented the fix through the artifact rather than through the code, and
the separation the framework depends on is gone.

No model, vendor, or provider name appears anywhere in the artifact.

## Metadata Block

A single leading fenced YAML block, rooted at `bugAnalysis`. Every field is required and must be
populated.

| Field | Constraint |
|---|---|
| `analysisId` | unique identifier for this analysis |
| `defectReference` | the defect, incident, or issue this analysis is about |
| `sourceInputs` | one entry per input actually read, each with `type` and `reference` |
| `producedBy` | `dev-1-bug-analyst` |
| `agentVersion` | semantic version of this agent |
| `schemaVersion` | `1.0.0` |
| `status` | `complete` \| `provisional` \| `blocked` |
| `severity` | `critical` \| `high` \| `medium` \| `low` |
| `reproducibility` | `deterministic` \| `intermittent` \| `not-reproduced` |
| `inputDigest` | from the frozen snapshot in the invocation envelope |
| `contextDigest` | from the frozen snapshot in the invocation envelope |

`sourceInputs` lists what was read, not what was available. An input listed but unread is a false
claim about the basis of the diagnosis.

Three fields here are duplicated in the body by design — `severity` and `status` in the Metadata
section, `reproducibility` as the Reproduction section's frequency. The duplication exists so
that a reader and a machine cannot be told different things. Checks `B2` and `B3` decide the
agreement, and a mismatch is a blocking failure rather than a formatting note.

## Section Contracts

### 1. Metadata

| Field | Content |
|---|---|
| `Bug ID` | matches `defectReference` |
| `Reporter` | who or what raised the defect: a person, a team, a monitor, a check |
| `Severity` | `critical` \| `high` \| `medium` \| `low`; equals the metadata block |
| `Status` | `complete` \| `provisional` \| `blocked`; equals the metadata block |

### 2. Symptom Summary

| Field | Content |
|---|---|
| `Observed behavior` | what the system actually did, as an observation. At least four words. |
| `Expected behavior` | what it should have done instead, and on whose statement. At least four words. |
| `First observed date` | when the failure was first seen, or that this is unknown |
| `Affected environments` | known affected, known unaffected, and not observed, kept distinct |

`Observed behavior` describes behavior, not code. A symptom stated as "the null check is
missing" has skipped to a cause before the analysis has begun, and the reader can no longer tell
what was actually seen.

`Affected environments` never merges "not observed" into "unaffected". An environment nobody
looked at is not an environment where the defect is absent, and collapsing the two understates
the blast radius three sections later.

### 3. Reproduction

| Field | Content |
|---|---|
| `Preconditions` | the state that must hold before the steps are attempted |
| `Steps to reproduce` | executable steps another person can follow without asking. At least four words. |
| `Reproduction frequency` | `deterministic` \| `intermittent` \| `not-reproduced`; equals the metadata block |
| `Evidence` | one line naming the register below |

Where frequency is `intermittent`, the varying precondition — or the fact that it was not found,
and what was ruled out — belongs in `Preconditions`. Where it is `not-reproduced`, `Steps to
reproduce` records what was attempted, in which environment, and what differed from the report.
The field is never left as the reporter's steps unexecuted.

#### Evidence Register

Table columns, in order: `ID`, `Evidence`, `Source`, `Confidence`. At least one row.

| Column | Content |
|---|---|
| `ID` | `E-nnn`, backticked, zero-padded, ascending from `E-001` |
| `Evidence` | what the artifact shows, stated as an observation |
| `Source` | the file, command, log, trace, or supplied input it came from |
| `Confidence` | `high` \| `medium` \| `low`, per the Stage 4 scale in `reasoning.md` |

Every diagnostic artifact the analysis relies on appears here, and only what appears here may be
cited. Anything this agent did not itself run or read is `low`. A confirmed absence — an alert
that did not fire, a log line that is missing — is registrable, and the `Evidence` cell says the
absence was confirmed rather than assumed.

### 4. Impact Assessment

| Field | Content |
|---|---|
| `User impact` | what a user experiences, and how many are affected |
| `Business impact` | the consequence to the business, from the supplied statement; where none was supplied, says so |
| `Technical impact` | the consequence to the system: correctness, availability, data, or capacity |
| `Blast radius` | every module, service, and data boundary the evidence shows the defect can reach |

`Blast radius` names boundaries, not files. It includes anything reachable only under the
preconditions established in section 3, and it names persisted bad data written while the defect
was active — that data survives the code fix, and naming it here is what makes the fix strategy
account for it.

The radius is what the evidence reaches. It is never narrowed toward the change the fix is
expected to make; that is the Fix Strategy's `Regression scope`, and the two are separate.

### 5. Root Cause Analysis

| Field | Content |
|---|---|
| `Root cause statement` | the condition that made the failure possible, in one sentence. At least five words. |
| `Why detection failed earlier` | which check, test, log, alert, or review step would have caught it, and why it did not. At least four words. |

The root cause statement names a condition — a wrong assumption, an unhandled state, a missing
constraint, a race, a contract mismatch — not a location. A statement whose whole content is a
file, function, or line has recorded where the symptom surfaced, which section 3's register
already holds.

Where the chain could not reach a condition, the statement says so explicitly, in that sentence,
and status is `provisional`. "The failure surfaces at X and the state that produces X is not
established" is an honest partial diagnosis. Naming X as the cause is not.

`Why detection failed earlier` is required even when the answer is that no such check existed.
It is what turns one defect into a prevention action, and `None identified.` is not an answer to
it.

#### Causal Chain

Table columns, in order: `ID`, `Step`, `Claim`, `Evidence`, `Confidence`. At least one row.

| Column | Content |
|---|---|
| `ID` | `C-nnn`, backticked, zero-padded, ascending from `C-001` |
| `Step` | the point in the failure this row covers, named briefly |
| `Claim` | what is asserted to have been true at that point |
| `Evidence` | one or more backticked `E-nnn` identifiers from the register |
| `Confidence` | `high` \| `medium` \| `low`, capped by the lowest confidence among the cited evidence |

Rows run from trigger to observed symptom. That is causal order, not the order the analysis
discovered them, and not chronological order where the two differ.

Every row cites at least one registered `E-nnn`. Check `B1` decides this and it is blocking. A
row with no citation is an inference; it is removed, or it is recorded as an open question
naming what would settle it.

### 6. Fix Strategy

| Field | Content |
|---|---|
| `Proposed fix` | the change in condition that removes the cause. At least four words. |
| `Alternative options` | at least one other approach, with what distinguishes them |
| `Regression risk` | `high` \| `medium` \| `low` |
| `Regression scope` | the areas a fix here can break, each with the dependency that connects it. At least three words. |

`Proposed fix` states what a fix must achieve, not how to build it. It carries no code, no patch,
and no file-level instruction. Where the chain is provisional, it says what it is conditional on.

`Alternative options` reading `None identified.` asserts that no other approach exists, which is
rarely true and always worth stating deliberately. A single option presented alone conceals the
tradeoff the implementer needs to see.

`Regression scope` is never generic and never omitted. It derives from the blast radius in
section 4 and from the paths the proposed change touches. Where persisted bad data is in the
radius, what must happen to that data belongs in `Proposed fix`, not here.

### 7. Validation Plan

| Field | Content |
|---|---|
| `Verification steps` | what would demonstrate the fix worked. At least four words. |
| `Regression tests added` | the checks that should exist so this defect cannot return silently |
| `Monitoring signals after release` | what to watch, and what value would indicate recurrence |

`Verification steps` is normally the reproduction from section 3, inverted: the same
preconditions and steps, now expected to succeed. Where reproducibility is `not-reproduced`, it
names the signal that serves instead.

`Regression tests added` states what should be written. Writing it belongs to
`dev-1-implement` and executing it to `qa`; the field is a target for them to reach, not
a result this agent claims.

For an `intermittent` defect, `Monitoring signals after release` carries the weight the
reproduction cannot, and `None identified.` there is a gap rather than a clean answer.

### 8. Closure

| Field | Content |
|---|---|
| `Resolution summary` | how the defect was resolved, once it has been |
| `Linked PR and release` | the change and the release that carried it |
| `Prevention actions` | what stops this class of defect recurring, from `Why detection failed earlier` |

Most analyses are written before any of this exists. Those fields read `None identified.` or name
what is pending; the section is never dropped. Filling `Resolution summary` is not this agent's
decision to make alone — closure is a gate decision — and a resolution recorded here is a record
of a decision made elsewhere, never an assertion that the defect is closed.

### Appendix — Open Questions

Table columns, in order: `ID`, `Question`, `Blocking`, `Owner`, `Affected steps`.

Required whenever status is `provisional` or `blocked`, and whenever a `critical` defect is not
fully diagnosed. Check `B5` decides the last of these.

| Column | Content |
|---|---|
| `ID` | `Q-nnn`, backticked, zero-padded, ascending from `Q-001` |
| `Question` | what is unresolved, and what would resolve it |
| `Blocking` | `yes` or `no`: whether the fix can proceed without the answer |
| `Owner` | the role it is routed to, per the escalation path in `identity.md` |
| `Affected steps` | the `C-nnn` identifiers the answer would change, or `none` |

Rows are ordered blocking first, then by the lowest causal step affected.

## Identifier Schemes

| Prefix | Defined in | Form |
|---|---|---|
| `E-nnn` | `Reproduction`, Evidence Register | zero-padded to three digits, ascending from `E-001` |
| `C-nnn` | `Root Cause Analysis`, Causal Chain | zero-padded to three digits, ascending from `C-001` |
| `Q-nnn` | `Open Questions` | zero-padded to three digits, ascending from `Q-001` |

Identifiers are backticked wherever they appear. Each is defined in exactly one section and may
be referenced from anywhere. A referenced identifier that is never defined is a citation to
nothing, and the Validation Engine rejects it.

Identifiers are assigned once and never renumbered, including when a `root-cause` invocation
supersedes a `triage` artifact in the same run. The one exception is compaction after a
mid-run retirement within the same artifact; the compaction records its old-to-new mapping,
describing superseded items by subject, never by their retired identifier token.

## Prohibited Content

- Code, patches, diffs, or file-modification directives of any kind.
- A root cause stated as a location where the evidence reaches only a location.
- A causal step with no registered evidence citation.
- A severity justified by cost, schedule, or reporter pressure.
- A blast radius narrowed to match an intended fix scope.
- A verdict on whether the defect blocks the release; that is the Triage Gate's.
- A closure assertion this agent does not have the authority to make.
- Credentials, tokens, secrets, or real personal data, including inside quoted evidence.
- A working exploitation path for a security defect.
- Any model, vendor, or provider name.

## Rendering Rules

- Section titles and section order are fixed and match the template exactly.
- Field bullets use the template's labels verbatim, in the template's order.
- Enumerated values are lower case, unquoted, and drawn only from the declared vocabulary.
- Identifiers are backticked; nothing else in a table cell is.
- Every table carries its declared columns in the declared order, with no columns added.
- A section with nothing to report reads `None identified.` and is not deleted.
- The artifact is written at the path the invocation envelope declares, and nowhere else.
