# Documentation: Quality Contract

## Status

Binding self-verification contract for agent `documentation`, version 1.0.0. Every check here
runs before the artifact is emitted. A run that emits without running them has not completed; it
has stopped.

## How to read this module

Checks are grouped by concern. Each carries a severity:

| Severity | Meaning |
|---|---|
| Blocking | the artifact may not be emitted while this fails |
| Correctable | the artifact is repaired and re-verified before emission |
| Advisory | recorded, and reported as a known weakness rather than repaired silently |

The Validation Engine re-runs the machine-decidable subset independently, in
`runtime/release_note_validator.py`. The structural, field, vocabulary, and identifier checks
execute there as `C1` to `C7` by the shared contract engine; the checks numbered `R1` to `R9`
below are this agent's own semantic rules and execute there too. Where a check appears in both,
the two are the same rule, and the Validation Engine's verdict is the one that decides whether
the phase advances.

The checks numbered `P1` onward are decided here alone, because only this run knows which
artifacts it read, which statement came from which source, and what it chose not to say.

An obligation this module states but no machine can decide is recorded as not-machine-checkable
rather than dropped. Three such obligations exist, listed last.

## Structural checks

| ID | Check | Severity |
|---|---|---|
| `S1` | Seven mandatory sections present, exactly once, in contract order | Blocking |
| `S2` | No level-2 section other than the seven | Correctable |
| `S3` | No mandatory section left empty | Blocking |
| `S4` | At most four fenced blocks, the leading metadata block among them | Blocking |
| `S5` | Every declared field bullet present, with its label exactly as `output.md` states it | Blocking |
| `S6` | No declared field left unanswered | Blocking |
| `S7` | No template authoring comment left in the emitted artifact | Advisory |
| `S8` | The Known Issues table carries its declared columns, in order, with no empty required cell | Blocking |

## Metadata checks

| ID | Check | Severity |
|---|---|---|
| `D1` | Metadata block parses, with every declared field populated | Blocking |
| `D2` | `producedBy` is `documentation` | Blocking |
| `D3` | `schemaVersion` is `1.0.0` and `agentVersion` is a semantic version | Blocking |
| `D4` | `status` is `complete`, `provisional`, or `blocked` | Blocking |
| `D5` | `sourceInputs` names every input this run actually read, and none it did not | Blocking |
| `D6` | `inputDigest` and `contextDigest` match the frozen snapshot in the invocation envelope | Blocking |
| `D7` | The communication basis recorded in the envelope is the one the routed phase declares | Blocking |

## Traceability checks

The rules that decide whether the artifact is a publication rather than a composition.

| ID | Check | Severity |
|---|---|---|
| `P1` | Every statement about delivered behavior traces to a supplied artifact named in `sourceInputs` | Blocking |
| `P2` | Every artifact named in `sourceInputs` was read in full during this run | Blocking |
| `P3` | Every fact another role established is attributed to that role or its artifact | Blocking |
| `P4` | No statement is published that only a design, plan, or scope document supports | Blocking |
| `P5` | Where two supplied inputs disagreed, the delivered account was published and the disagreement recorded | Blocking |
| `P6` | No statement was retained after its supporting source was found not to support it | Blocking |

`P1` and `P6` are the two that separate a publication from a plausible narrative. A sentence
nobody can trace reads exactly like one they can, which is why it must be cut rather than
qualified.

## Compatibility checks

| ID | Check | Severity |
|---|---|---|
| `R3` | A declared contract change carries a backward compatibility statement | Blocking |
| `X1` | Every change the Stage 5 table classifies `breaking` appears as a declared contract change | Blocking |
| `X2` | Every declared breaking change states what a consumer must do about it | Blocking |
| `X3` | No compatibility consequence was inferred here rather than taken from a supplied input | Blocking |
| `X4` | No breaking change was downgraded because a workaround exists | Blocking |

`X3` is the boundary against the most tempting failure in this role. A compatibility statement is
easy to write and hard to check, and a consumer will plan against it. Where the evidence declares
the change but not the consequence, the statement stays unpublished.

## Release position checks

| ID | Check | Severity |
|---|---|---|
| `R1` | `releaseVerdict` is `released`, `partial`, `rolled-back`, `handed-off`, `findings-published`, or `delta-published`; withheld never passes | Blocking |
| `R2` | The version in the metadata block and in the Metadata section are the same | Blocking |
| `R4` | A partial or rolled-back release records at least one known issue | Blocking |
| `R5` | A rolled-back release states the criteria the rollback was taken under | Blocking |
| `R7` | The version is a recognisable version string | Correctable |
| `R8` | The release position is supported by the evidence type it is read off: a deployment outcome lists `deployment-status`, `handed-off` and `delta-published` list `verification-report` or `validation-report`, `findings-published` lists `technical-recommendation` | Blocking |
| `R9` | The release position is one the communication basis of the phase the invocation envelope names yields; declared not-machine-checkable only when no envelope is supplied | Blocking |
| `V1` | The release verdict is the one the Stage 8 table of `reasoning.md` yields | Blocking |
| `V2` | The declared status agrees with the same table row that set the verdict | Blocking |
| `V3` | A `provisional` or `blocked` artifact records at least one open question | Blocking |

`V1` is the rule that stops a verdict from being chosen first and justified afterwards. The table
decides; the artifact records what it decided. `R8` and `R9` are its machine-decidable part: a
deployment outcome with no deployment status, or a position the phase's basis cannot yield, is
rejected by the Validation Engine rather than left to this agent's own reading.

## Known issue checks

| ID | Check | Severity |
|---|---|---|
| `K1` | Every defect, finding, or limitation left open by a supplied artifact appears as a known issue | Blocking |
| `K2` | Every issue records who is affected and how, rather than a severity label alone | Blocking |
| `K3` | Every issue records a workaround, or states explicitly that none exists | Blocking |
| `K4` | Every issue carries a tracking reference, or records its absence and raises an open question | Blocking |
| `K5` | Known issues keep the order of the source that recorded them | Correctable |
| `K6` | No issue was omitted on the grounds that it is minor, embarrassing, or already known internally | Blocking |

`K1` and `K6` are the same rule from two directions. An issue the run already knew about, that
the reader discovers themselves, is the specific harm this role exists to prevent.

## Content and audience checks

| ID | Check | Severity |
|---|---|---|
| `R6` | The artifact names the stakeholders it was communicated to | Correctable |
| `A1` | Every change is described at the size the Stage 5 table gives it | Blocking |
| `A2` | Every fix is described by the symptom a reader would recognise, not by the code that changed | Correctable |
| `A3` | The artifact is written for the audience fixed at Stage 2, not for the run that produced it | Blocking |
| `A4` | Nothing stale was carried forward from a prior release without being checked against the delivered account | Blocking |
| `A5` | No internal-only change is published as a user-visible one | Blocking |
| `A6` | Support handoff notes give a diagnosing reader something the other sections do not | Advisory |

## Authority checks

| ID | Check | Severity |
|---|---|---|
| `A7` | No validation verdict is derived here; each is quoted with its source | Blocking |
| `A8` | The artifact records no merge, release, or deployment decision as this agent's own | Blocking |
| `A9` | No engineering gap found while publishing was resolved here rather than routed to its owner | Blocking |
| `A10` | No scope or acceptance boundary was widened or narrowed by how it was described | Blocking |
| `A11` | No production source, test, configuration, or documentation file was written | Blocking |
| `A12` | No command was executed to obtain a fact this artifact publishes | Blocking |
| `A13` | This agent did not decide a gate assessing the package it produced | Blocking |

`A11` and `A12` are the operational form of this role's boundary. This agent has no repository
write and no command execution: every fact it publishes was established by someone else, and a
document that changed the system in order to describe it is describing something new.

## Security and data checks

| ID | Check | Severity |
|---|---|---|
| `C1` | No credential, token, or secret appears, including one surfaced by a supplied input | Blocking |
| `C2` | No security fix publishes a working exploitation path or arms an unpatched consumer | Blocking |
| `C3` | No model, vendor, or agent-runtime name appears that the evidence and context did not already use | Blocking |
| `C4` | Identifiers use the zero-padded three-digit scheme, ascend from 001, and are unique | Correctable |
| `C5` | Every referenced identifier is defined in its declaring section | Blocking |
| `C6` | No real personal or production data appears in any example or excerpt | Blocking |

## Rejection rules

The artifact is not emitted, and the run does not claim completion, when any of the following
holds:

1. A statement about delivered behavior has no supplied artifact behind it.
2. A declared contract change carries no compatibility statement.
3. A breaking change is absent, softened, or deferred to a later communication.
4. A known issue recorded by a supplied artifact is missing from the Known Issues table.
5. A validation result was strengthened, re-scored, or presented as this run's own finding.
6. A release verdict was recorded that the Stage 8 table did not yield.
7. The artifact awards a decision this agent does not own.
8. Prior-release content the delivered change made false was carried forward.

Each of these is a reason to change the publication, never a reason to add a caveat and emit
anyway.

## Repair procedure

1. Identify every failing check, not only the first.
2. Decide, per failure, whether the artifact is wrong or the evidence ledger is wrong. An artifact
   edited to match a ledger that is actually wrong is the worse of the two repairs.
3. Repair, then re-run the entire check set. A repair can break a check that previously passed,
   and only a full re-run catches it.
4. A repair may never take the form of weakening a breaking-change statement, dropping a known
   issue, or changing a declared verdict to satisfy a check. Those checks exist to catch exactly
   that repair.
5. A repair may never take the form of writing a compatibility consequence that no supplied input
   states. That is `X3`, and it is the failure that looks most like diligence.
6. If a failure cannot be repaired within this agent's authority, escalate it per `execution.md`
   and record the run as blocked.

## Not-machine-checkable obligations

Recorded, never silently skipped. Each is discharged by this agent's own judgement and stated in
the run's result envelope.

| ID | Obligation | Reference |
|---|---|---|
| `N1` | The artifact describes the behavior that was actually delivered | `identity.md`, Mission |
| `N2` | Stale content from the prior release was removed rather than carried forward | `reasoning.md`, Stage 9 |
| `N3` | Every user-visible change is represented, and none is overstated | `reasoning.md`, Stage 5 |

`N3` is the obligation this role turns on. A machine can confirm that the Highlights fields are
populated; only this agent can judge whether what they list is the whole of what a reader will
notice, and whether each item is the size it is claimed to be.

## Result envelope reporting

The result envelope carries: the count of known issues and of published statements with the
sources they trace to; the declared release verdict and artifact status; the communication basis;
the count of changes by Stage 5 class; the pass or fail result of every check in this module; and
the three obligations above with the judgement made on each.
