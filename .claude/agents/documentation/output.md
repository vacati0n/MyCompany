# Documentation: Output Contract

## Status

Binding structural and semantic contract for `release-note.md`, produced by agent
`documentation`, version 1.0.0, schema version 1.0.0.

The rendered form is `templates/release-note.md`. This module is the contract; the template is
the shape it renders to. The Validation Engine decides conformance in
`runtime/release_note_validator.py`.

## Deliverable

One artifact per run: `release-note.md`, written at the artifact path the phase declares.

One artifact type carries every publication output the framework asks for:

| Workflow | Phase | Communication basis | Release position |
|---|---|---|---|
| `implement-feature` | `documentation-and-release-handoff` | `release-handoff` | `handed-off` |
| `investigate` | `publication` | `findings` | `findings-published` |
| `research` | `findings-publication` | `findings` | `findings-published` |
| `review-pull-request` | `documentation-impact` | `documentation-delta` | `delta-published` |
| `release` | `communication-and-post-release` | `post-release` | `released` \| `partial` \| `rolled-back` |

Each is a statement of what was delivered, what it means for its audience, and what is still
known to be wrong. The basis records which question the artifact answered. The structure does not
change with it; what changes is which sections carry the weight. A `documentation-delta`
publication puts its substance in Technical Changes and Compatibility, a `post-release` one in
Operational Notes and Known Issues, and a `release-handoff` one in Communication.

The release position is the one the Stage 8 table of `reasoning.md` yields for the basis when its
evidence is present. Only `post-release` reads a deployment outcome, off the supplied deployment
status; the other three positions assert no merge, release, or deployment fact.

## Structural Contract

Seven mandatory level-2 sections, in this exact order, each present exactly once:

| # | Section | Content form |
|---|---|---|
| 1 | `Metadata` | four declared field bullets |
| 2 | `Highlights` | three declared field bullets |
| 3 | `Technical Changes and Compatibility` | four declared field bullets |
| 4 | `Operational Notes` | three declared field bullets |
| 5 | `Validation Summary` | three declared field bullets |
| 6 | `Known Issues` | table, at least one row |
| 7 | `Communication` | two declared field bullets |

No other level-2 section may appear.

Sections are never omitted and never left empty. A section with nothing to report reads
`None identified.` — including `Known Issues`, where a clean release states plainly that it ships
with nothing known outstanding rather than dropping the section and leaving a reader to infer
why it is missing.

`None identified.` in `Known Issues` is permitted only where the release verdict is `released`,
`handed-off`, `findings-published`, or `delta-published`. A release that did not fully land has
something to say about why, and check `R4` decides it.

### Fenced blocks

At most four, the leading metadata block among them. A release note may quote a configuration
value or a command an operator must run; it does not quote diffs, test output, or source. The
shared prohibition holds: no model, vendor, or provider name.

## Metadata Block

A single leading fenced YAML block, rooted at `releaseNote`. Every field is required and must be
populated.

| Field | Constraint |
|---|---|
| `releaseId` | unique identifier for this release or closure event |
| `version` | the version this note describes; a recognisable version string |
| `sourceInputs` | one entry per input actually read, each with `type` and `reference` |
| `producedBy` | `documentation` |
| `agentVersion` | semantic version of this agent |
| `schemaVersion` | `1.0.0` |
| `status` | `complete` \| `provisional` \| `blocked` |
| `releaseVerdict` | `released` \| `partial` \| `rolled-back` \| `handed-off` \| `findings-published` \| `delta-published` |
| `inputDigest` | from the frozen snapshot in the invocation envelope |
| `contextDigest` | from the frozen snapshot in the invocation envelope |

`sourceInputs` lists what was read, not what was available. An input listed but unread is a false
claim about the basis of everything published below it.

Declared `type` values are `deployment-status`, `monitoring-health-record`,
`final-change-summary`, `stakeholder-list`, `verification-report`, `validation-report`,
`technical-recommendation`, and `architecture-context`. An input of another kind is
recorded under the declared type that describes what it supplied.

The release position is read off one of these, and check `R8` decides that it was listed:
`released`, `partial`, and `rolled-back` need `deployment-status`; `handed-off` and
`delta-published` need `verification-report` or `validation-report`; `findings-published` needs
`technical-recommendation`.

## Section Contracts

### 1. Metadata

| Field | Content |
|---|---|
| `Version` | the version this note describes; equals metadata `version` exactly |
| `Release date and time` | when the release or closure event occurred |
| `Environment` | where it landed; for a position that asserts no deployment, where the published account stands, naming no environment as reached |
| `Release owner` | the role accountable for the release |

`Version` and metadata `version` are one fact stated twice. Check `R2` compares them, because a
note whose header and provenance disagree cannot be cited by the closure package that consumes
it.

### 2. Highlights

| Field | Content |
|---|---|
| `Feature additions` | behavior a user can now reach that they could not before |
| `Bug fixes` | previously wrong behavior that is now right, stated by the symptom that is gone |
| `Improvements` | existing behavior that is better without being new |

Each is classified by the Stage 5 table of `reasoning.md`, not by how the change felt to build. A
change classified `internal` appears in none of these three, and padding a thin release with
internal work is rejected by check `A5`.

A fix is described by the symptom a reader would have noticed, never by the code that changed. A
reader who never saw the symptom does not need the fix; a reader who did needs to recognise it.

### 3. Technical Changes and Compatibility

| Field | Content |
|---|---|
| `API or contract changes` | every interface, schema, or data-shape change, or `None identified.` |
| `Database or migration impact` | what a stored dataset requires, including that it requires nothing |
| `Configuration changes` | what a deployment must set, change, or remove |
| `Backward compatibility notes` | required whenever a contract change is declared |

This is the section that decides whether the note is safe to act on. A declared contract change
with `None identified.` or empty compatibility notes is rejected by check `R3`, because a
consumer reading that pair concludes nothing is required of them.

The compatibility statement records what a consumer relying on prior behavior will observe and
what they must do, from the Stage 6 procedure. Where a supplied input declares the change but not
its consequence, the consequence is not inferred here: the statement stays unpublished, the
artifact is `provisional`, and the question routes to `architect`.

### 4. Operational Notes

| Field | Content |
|---|---|
| `Deployment considerations` | what deploying this requires that the last one did not |
| `Monitoring and alerts` | what to watch, and what a healthy signal looks like |
| `Rollback criteria` | what would justify reverting. At least three words. |

`Rollback criteria` is never left as a formality. For a `rolled-back` release it records the
criteria the rollback was actually taken under, and check `R5` decides it.

### 5. Validation Summary

| Field | Content |
|---|---|
| `Test status` | what was validated and by whom, attributed to its source. At least three words. |
| `Known risk acceptance` | risk accepted, naming the role that accepted it |
| `Post-release checks` | what should be confirmed after this reaches its environment |

`Test status` quotes the supplied validation result with its source named. It never re-derives a
verdict, never promotes a qualified pass to a clean one, and never describes as validated
anything the validation report recorded as blocked or unreached. That judgement belongs to
`qa`, and taking it here is the failure decision rule 4 in `identity.md` exists to prevent.

Risk this agent noted alone is not accepted risk. `Known risk acceptance` names the accepting
role or reads `None identified.`

### 6. Known Issues

A table, at least one row, with these columns in this order:

| Column | Content |
|---|---|
| `ID` | `K-nnn`, in the order the source recorded the issues |
| `Issue` | what is wrong, in terms a reader outside the run can recognise |
| `Impact` | who is affected and how |
| `Workaround` | what they can do instead, or that there is none |
| `Tracking` | where the issue is recorded for resolution |

Every column is required in every row. `Impact` states consequence for a person, not severity as
a label: a reader deciding whether this affects them cannot act on the word `medium`.

A `Workaround` cell reading that none exists is a complete answer. An empty one is not.

An issue whose source records no tracking reference is published with its tracking stated as
absent and an open question raised. It is never dropped for being untracked.

### 7. Communication

| Field | Content |
|---|---|
| `Stakeholders notified` | who this was communicated to |
| `Support handoff notes` | what a support engineer needs that the sections above do not give them |

`Stakeholders notified` names the audience fixed at Stage 2. A communication with no stated
recipient has not been communicated, and check `R6` records it.

`Support handoff notes` is written for someone diagnosing a problem without the run in front of
them: what will look broken but is not, what a confusing state actually means, and where to look
first.

## Identifier Schemes

| Prefix | Declared in | Scheme |
|---|---|---|
| `K` | `Known Issues` | `K-nnn`, zero-padded, ascending from 001 |

Every identifier referenced anywhere in the artifact is defined in its declaring section. No
identifier is defined twice, and none is renumbered after assignment, except to compact a
family after a mid-run retirement; the compaction records its old-to-new mapping, describing
superseded items by subject, never by their retired identifier token.

## Prohibited Content

1. Any model, vendor, or agent-runtime name the evidence and context did not already use.
2. Any credential, token, or secret, including one surfaced by a supplied input.
3. A working exploitation path for a security fix, or detail that arms an unpatched consumer.
4. Production source, a diff, or a patch.
5. A merge, release, or deployment decision recorded as this agent's own.
6. A statement about delivered behavior with no supplied artifact behind it.
7. A validation verdict this agent derived rather than quoted.
8. Real personal or production data in an example, an excerpt, or a troubleshooting step.

## Rendering Rules

1. Section titles are used verbatim as this module states them.
2. Field bullet labels are used verbatim as this module states them.
3. Table columns appear in the declared order, with no empty required cell.
4. No template authoring comment survives into the emitted artifact.
5. Versions are rendered exactly as their establishing source states them.
6. `None identified.` is the only permitted empty-content marker.
