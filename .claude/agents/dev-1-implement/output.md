# Implementation Developer: Output Contract

## Status

Binding output contract for agent `dev-1-implement`, version 1.0.0. Governs
`templates/implementation-report.md` and `examples.md` where they differ.

## Deliverable

One artifact: `implementation-report.md`, written to the path the invocation envelope
names in `expected_output_schema.artifact_path`.

The source and test files the report declares are changed in the repository, not embedded
in the artifact. The report is the accounting; the code is the change. An artifact that
contains the change instead of accounting for it is non-conforming.

## Structural Contract

Nine mandatory sections, each present exactly once, in this order:

| # | Section | Shape | Empty form |
|---|---|---|---|
| 1 | Metadata | six field bullets | never empty |
| 2 | Implementation Summary | four field bullets | never empty |
| 3 | Change Set | table, at least one row | never empty |
| 4 | Test Evidence | table, at least one row | never empty |
| 5 | Verification Results | four field bullets | never empty |
| 6 | Deviations and Tradeoffs | table | `None identified.` |
| 7 | Boundary Compliance | four field bullets | never empty |
| 8 | Residual Risk | table | `None identified.` |
| 9 | Handoff Notes | three field bullets | never empty |

`Open Questions` is the only permitted appendix. No other level-2 section may be introduced.

A leading fenced YAML block under key `implementationReport` carries the provenance
metadata. It is the only fenced block the artifact may contain.

## Metadata Block

```yaml
implementationReport:
  reportId:            # IR-<year>-<sequence>
  changeReference:     # the change, ticket, or run this report accounts for
  sourceInputs:        # one entry per supplied input, with its declared type and reference
  producedBy: dev-1-implement
  agentVersion:        # semantic version of this agent
  schemaVersion: 1.0.0
  status:              # complete | provisional | blocked
  workflowPhase:       # implementation | fix-implementation | refactor-implementation
  verificationStatus:  # verified | partially-verified | unverified
  inputDigest:         # from the invocation envelope's context slice
  contextDigest:       # from the invocation envelope's context slice
```

`status`, `workflowPhase`, and `verificationStatus` are stated twice: once here and once in
the Metadata section. They are one fact recorded in two places, and they must agree. A
disagreement is a blocking failure, not a formatting slip.

## Section Contracts

### 1. Metadata

Six bullets: `Report ID`, `Change reference`, `Workflow phase`, `Status`,
`Verification status`, `Review status`.

`Review status` is always `pending-review`. This agent does not review its own change, so
there is no other value it is entitled to write.

### 2. Implementation Summary

Four bullets: `Change intent`, `Approach taken`, `Design reference`, `Out of scope`.

`Change intent` states what is true after the change that was not true before. `Approach
taken` states the route actually used, not the route considered, and names the rung of the
necessity and reuse ladder in `skills/architecture/clean-architecture-checklist.md` that
justified any new abstraction, file, or dependency the change introduced; when the change
introduced none of those, it says so. `Out of scope` names what was deliberately not changed
and why leaving it is safe.

### 3. Change Set

One row per file the change touched.

| Column | Content |
|---|---|
| ID | `C-nnn`, ascending from `C-001` |
| Path | repository-relative path |
| Change Type | `added`, `modified`, `removed`, or `moved` |
| Purpose | what this file's change accomplishes, in one statement |
| Design Ref | the design element, decision record, analysis step, or plan task it serves |

A file changed for no accepted element still appears here, and its Design Ref names the
deviation that authorizes it. An unlisted file that was changed is an undeclared side
effect.

### 4. Test Evidence

One row per automated check that exercises the change.

| Column | Content |
|---|---|
| ID | `T-nnn`, ascending from `T-001` |
| Test | what the check asserts |
| Type | `unit`, `integration`, `contract`, `regression`, `end-to-end`, or `static` |
| Covers | the `C-nnn` identifiers this check exercises |
| Command | the exact command that produced the result |
| Result | `pass`, `fail`, or `not-run` |

Every `C-nnn` defined in the Change Set appears in at least one `Covers` cell. A change
with no covering evidence is an unverified change, and the report says so rather than
implying coverage.

`Result` records what the command actually returned. `not-run` is an honest value; a
predicted `pass` is not.

### 5. Verification Results

Four bullets: `Verification method`, `Commands executed`, `Result summary`,
`Unverified areas`.

`Commands executed` is written inline; the artifact permits no second fenced block.
`Result summary` carries counts: executed, passed, failed. `Unverified areas` names what
the executed evidence does not reach, or `none` when it reaches everything in scope.

`Unverified areas` and `verificationStatus` must agree: a `verified` claim names no gap, and
a `partially-verified` or `unverified` claim names at least one. A deliberately out-of-scope
item is not an unverified area — it belongs under `Out of scope` in the summary, never here.
See `examples.md`'s non-conforming example N8 for a case this exact rule catches.

### 6. Deviations and Tradeoffs

One row per departure from the accepted design, the standards, or the plan, and per
tradeoff taken knowingly.

| Column | Content |
|---|---|
| ID | `V-nnn` |
| Deviation | what was done differently, citing the `C-nnn` entries that embody it |
| Design element | the element, standard, or task departed from |
| Rationale | why the departure was necessary, in evidence terms |
| Escalation | the escalation raised, or `not-required` when the departure is inside this agent's latitude |

Every deviation cites at least one change-set identifier. A deviation with no change behind
it is a claim about the work rather than a record of it.

`None identified.` is the correct content when there was no departure. It is not a default.

### 7. Boundary Compliance

Four bullets: `Module boundaries preserved`, `Public interface changes`, `Data or migration
impact`, `Declared side effects`.

`Declared side effects` lists every file this invocation wrote, which is the Change Set plus
the artifact and result envelope paths. Where the change was produced outside this
invocation's permitted writes -- as happens when the framework changes its own declarations
ahead of the run that records them -- the field says so and names the record that governs
it, so the difference between the two sets is stated rather than left to be discovered.

### 8. Residual Risk

One row per condition still risky after the change: `R-nnn`, the risk, its likelihood, its
impact, and the mitigation actually in place. A mitigation that is planned rather than in
place is follow-up work, and belongs in Handoff Notes.

### 9. Handoff Notes

Three bullets: `Reviewer focus areas`, `Follow-up work`, `Documentation impact`.

`Reviewer focus areas` names where independent review is most valuable — normally the sites
where a deviation, a boundary, or a residual risk lands.

### Appendix — Open Questions

`Q-nnn`, the question, whether it blocks, its owner, and the change-set entries it affects.

Required whenever status is `provisional` or `blocked`, whenever verification status is not
`verified`, and whenever a deviation was escalated. `None identified.` otherwise.

## Identifier Schemes

| Prefix | Declared in | Meaning |
|---|---|---|
| `C-nnn` | Change Set | one changed file |
| `T-nnn` | Test Evidence | one automated check |
| `V-nnn` | Deviations and Tradeoffs | one departure or tradeoff |
| `R-nnn` | Residual Risk | one remaining risk |
| `Q-nnn` | Open Questions | one unresolved decision |

All are zero-padded to three digits, ascend from 001 without gaps, and are defined exactly
once in their declaring section. An identifier referenced anywhere in the artifact is
defined in its declaring section. These schemes are this report's own namespace: an upstream
artifact's identifiers (a bug analysis's `C-nnn` causes or `Q-nnn` questions) are never
bare-cited in prose here, because the same prefixes mean something else in this report — name
the upstream finding descriptively, or by artifact-qualified reference, instead. See
`examples.md`'s non-conforming example N7 for a case this exact rule catches.

## Prohibited Content

- diffs, patches, hunks, or file-modification directives — the change is in the repository
- code blocks other than the leading metadata block
- a review verdict, a merge decision, or a release readiness claim
- a model, vendor, or agent-runtime name absent from the inputs or the loaded context
- credentials, tokens, secrets, or restricted content copied from any source
- a predicted result presented as an executed one

## Rendering Rules

- Field bullets render as `- Label: value`, with the label exactly as this contract states it.
- Table headers render exactly as this contract states them, in the stated column order.
- A section with nothing to report reads `None identified.` where the shape permits it.
- Template authoring comments are removed from the emitted artifact.
