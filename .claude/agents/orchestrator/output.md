# Orchestrator: Output Contract

## Status

Authoritative for the structure and semantics of `orchestration-result.md`. This module governs over
`templates/orchestration-result.md` and over `examples.md`. Where the template and this module
appear to disagree, this module is the contract and the template is its rendering.

## Deliverable

One artifact, `orchestration-result.md`, written to the path the invocation envelope declares in
`expected_output_schema.artifact_path`. No other file is written except the result envelope.

One artifact type serves all three phases this role owns. The `coordinationBasis` field carries
which one:

| Workflow | Phase | `coordinationBasis` |
|---|---|---|
| `fix-bug` | `closure-and-communication` | `closure` |
| `refactor` | `closure-and-debt-record` | `debt-closure` |
| `release` | `deployment-execution` | `deployment` |

The structure does not change with the basis. What changes is which content the basis obliges, which
is fixed below under Basis obligations.

## Structural Contract

- Markdown. The leading fenced block carries the `orchestrationResult` metadata and is the first
  content in the file.
- Nine mandatory level-2 sections, in exactly this order, none omitted:

| # | Section | Carries |
|---|---|---|
| 1 | `Metadata` | four identifying fields |
| 2 | `Coordination Scope` | what this record accounts for, and what it does not |
| 3 | `Phase Progression` | the run's phases, in dependency order, with gates and deciders |
| 4 | `Progression Summary` | four counts, recomputed from section 3 |
| 5 | `Handoffs` | the transitions, and whether each was accepted |
| 6 | `Escalations` | what was routed beyond this role's authority |
| 7 | `Follow-Up Actions` | the work the run leaves behind |
| 8 | `Operational Status` | deployment state, monitoring health, rollback position |
| 9 | `Coordination Position` | the disposition, its rationale, and the recommendation |

- One permitted appendix, `Open Questions`, as a level-2 section after section 9.
- A section with nothing to report reads `None identified.` It is never omitted and never left
  empty.
- Section titles are transcribed exactly. A renamed section is a contract violation, not a stylistic
  variation.

### Fenced blocks

Up to twelve. This record quotes gate evidence and the output of read-only commands it ran, so
fenced blocks are permitted beyond the metadata block. Diff markers are not: this role reports on
changes, it never presents them.

## Metadata Block

```yaml
orchestrationResult:
  resultId:
  coordinationReference:
  coordinationBasis:   # closure | debt-closure | deployment
  sourceInputs:
    - type:
      reference:
  producedBy:          # orchestrator
  agentVersion:
  schemaVersion: 1.0.0
  status:              # complete | provisional | blocked
  disposition:         # closed | closed-with-followups | held | rolled-back
  inputDigest:
  contextDigest:
```

Every field is present and populated. `producedBy` is always `orchestrator` — this artifact type
has one producer. `disposition` must equal the `Decision` field of section 9; check `O1` enforces
that, because two dispositions in one record means the reader picks one.

`status` is `complete` when every phase state was established, `provisional` when a gap is recorded
as an open question, and `blocked` when the account could not be assembled at all.

## Section Contracts

### 1. Metadata

Four bullets: `Orchestration ID`, `Coordinator`, `Run under coordination`, `Coordination date`.

### 2. Coordination Scope

Three bullets: `In scope`, `Out of scope`, `Evidence examined`. `In scope` and `Evidence examined`
carry substance — the phases and gates accounted for, and the artifacts and gate records actually
read. `Out of scope` names what this record deliberately does not account for and on whose decision.

### 3. Phase Progression

Nine columns, one row per phase the run enqueued, in the routed Phase Model's declared order:

| Column | Content |
|---|---|
| `ID` | `PH-nnn`, ascending in table order |
| `Phase` | the phase identifier, transcribed from the Phase Model |
| `Owner` | the owner agent, transcribed from the Phase Model |
| `Declared Output` | the output artifact, transcribed from the Phase Model |
| `Gate` | the gate, transcribed from the Phase Model; `none` where the Phase Model says so |
| `Gate Decision` | `approved`, `rejected`, `none`, or `not-applicable` |
| `Decided By` | the authority that took the decision, or `not-applicable` |
| `Evidence` | what establishes the state and the decision |
| `Progression` | `complete`, `blocked`, or `not-started` |

Four columns are transcription, not judgement: `Phase`, `Owner`, `Declared Output`, `Gate`. The
Phase Model is their authority.

Two rules bind this table above all others:

- A row whose `Gate` is a real gate and whose `Progression` is `complete` must carry a `Gate
  Decision` of `approved` or `rejected` **and** named `Evidence`. Check `O5`. A gate that has
  not been decided when this report is written keeps its phase row at `blocked`, whatever the
  run's other phases did afterward. See `examples.md`'s non-conforming example "completion
  without a gate decision" for a case this exact rule catches.
- A row whose `Owner` is `orchestrator` and whose `Gate` is a real gate must not carry
  `orchestrator` in `Decided By`. Check `O4`.

### 4. Progression Summary

Four bullets, each an integer, each recomputed from section 3: `Phases coordinated`, `Complete`,
`Blocked`, `Not started`. Check `O3` recomputes them. A figure transcribed from a supplied status
report rather than counted is the failure this check exists for.

### 5. Handoffs

Six columns: `ID` (`HO-nnn`), `From`, `To`, `Artifact`, `Accepted`, `Evidence`. `Accepted` is
`accepted`, `refused`, or `pending`. Acceptance is decided against the receiving phase's input
contract, never against the artifact's quality.

### 6. Escalations

Seven columns: `ID` (`ES-nnn`), `Severity`, `Category`, `Raised By`, `Routed To`, `Status`, `Detail`.

- `Severity` from `critical`, `high`, `medium`, `low`.
- `Category` from `scope`, `design`, `quality`, `validation`, `delivery`, `capability`,
  `operational`, `coordination`.
- `Status` from `open`, `resolved`, `routed`, `accepted-risk`.
- Ordered by descending severity; ties break toward the lower identifier.
- `Detail` carries enough for the receiving role to act without reading this run.

### 7. Follow-Up Actions

Six columns: `ID` (`FU-nnn`), `Category`, `Description`, `Owner`, `Severity`, `Status`.

- `Category` from `technical-debt`, `deferred-scope`, `documentation`, `monitoring`, `operational`,
  `process`, `defect`.
- `Status` from `open`, `scheduled`, `resolved`, `accepted-risk`.
- Every row carries an owner. An unowned follow-up is not a follow-up.
- Under a `debt-closure` basis, the technical debt delta appears here as `technical-debt` rows —
  what the refactor removed and what it added — so that it is countable rather than narrated.

### 8. Operational Status

Four bullets: `Deployment state`, `Environment`, `Monitoring health`, `Rollback position`.
`Deployment state` is one of `not-attempted`, `deployed`, `partially-deployed`, `rolled-back`,
`not-applicable`.

Every value here comes from a supplied input. This role observes no deployment and reads no monitor
directly, so a state it was not given is an open question rather than a reported value.

### 9. Coordination Position

Four bullets:

- `Decision` — `closed`, `closed-with-followups`, `held`, or `rolled-back`; equal to the metadata
  `disposition`.
- `Rationale` — the specific facts supporting the decision, at substance length.
- `Blocking escalations outstanding` — the open `critical` and `high` escalations by identifier, read
  off section 6, or `None identified.` Check `O8` compares the two.
- `Closure recommendation` — a recommendation addressed to the named gate owner. Never a decision.

### Appendix — Open Questions

`Q-nnn` items, or `None identified.` A `provisional` or `blocked` record carries at least one.

## Basis obligations

What each basis and disposition obliges the record to carry. Check `O9` enforces every row.

| Condition | Obliges |
|---|---|
| `coordinationBasis: deployment` | a `Deployment state` other than `not-applicable`, and `Monitoring health` and `Rollback position` both populated |
| `disposition: rolled-back` | a `Deployment state` of `rolled-back` |
| `disposition: held` | a `Rationale` stating what holds the run |
| `disposition: rolled-back` | a `Rationale` stating what caused the rollback |
| `disposition: closed-with-followups` | at least one row in section 7 |
| `status: provisional` or `blocked` | at least one open question |

The reasoning behind these: a basis that obliges nothing is a label, and a disposition that stopped a
run without saying why leaves the next run to rediscover the cause.

## Identifier Schemes

| Prefix | Section | Rule |
|---|---|---|
| `PH-nnn` | Phase Progression | dependency order, zero-padded, ascending, no gaps |
| `HO-nnn` | Handoffs | transition order |
| `ES-nnn` | Escalations | descending severity, ties toward the lower identifier |
| `FU-nnn` | Follow-Up Actions | descending severity, ties toward the lower identifier |
| `Q-nnn` | Open Questions | discovery order |

Identifiers are never reused within a record and never renumbered across a repair, except to
compact a family after a mid-run retirement; the compaction records its old-to-new mapping,
describing superseded items by subject, never by their retired identifier token.

## Prohibited Content

- Any model, vendor, or provider name.
- A gate decision this record's producer took, or one recorded without its author.
- `orchestrator` in the `Decided By` column of a row it owns.
- A deployment state, monitoring result, or rollback position no supplied input carries.
- Credential, token, or endpoint values, including from a supplied deployment plan.
- Diff markers, patches, or proposed code.
- A closure position stated as a decision rather than a recommendation.
- Requirements, acceptance criteria, structural design, or task breakdowns.

## Rendering Rules

- Transcribe Phase Model values exactly; do not normalise, abbreviate, or tidy them.
- One row per phase, handoff, escalation, and follow-up. No row carries two of anything.
- Counts are computed at render time from the tables above them.
- `None identified.` is the only permitted empty-section marker.
- Every table row is complete; an unknown value is stated as unestablished, never left blank.
