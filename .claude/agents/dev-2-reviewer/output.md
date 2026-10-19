# Reviewer: Output Contract

## Status

Binding output contract for agent `dev-2-reviewer`, version 1.0.0. Governs
`templates/review-package.md` where the two disagree.

## Deliverable

One artifact: `review-package.md`, written to the path the invocation envelope names in
`expected_output_schema.artifact_path`.

The package is the review. The change it assesses stays in the repository, unmodified by this
agent. A package that carries a corrected version of the code instead of an account of what is
wrong with it is non-conforming, because producing the correction would end the independence
the review rests on.

Five review phases across four workflows name their output differently in prose. They are one
artifact type, and `templates/review-package.md` records why: each is a severity-classified
findings set over a defined scope, closing with a readiness decision. The Category column
carries the lens; the structure does not change with it.

## Structural Contract

Nine mandatory sections, each present exactly once, in this order:

| # | Section | Shape | Empty form |
|---|---|---|---|
| 1 | Metadata | four field bullets | never empty |
| 2 | Review Scope | three field bullets | never empty |
| 3 | Findings | table, at least one row | never empty |
| 4 | Severity Summary | four field bullets, each an integer | never empty |
| 5 | Standards and Architecture Conformance | four field bullets | never empty |
| 6 | Test Adequacy Assessment | three field bullets | never empty |
| 7 | Correction Requests | table, at least one row | never empty |
| 8 | Residual Risk | three field bullets | never empty |
| 9 | Verdict | four field bullets | never empty |

`Open Questions` is the only permitted appendix. No other level-2 section may be introduced.

A leading fenced YAML block under key `reviewPackage` carries the provenance metadata.

A review that found nothing still fills the Findings and Correction Requests tables: it records
the scope examined as a row whose severity is `low` and whose status is `resolved` only when
that is true, and otherwise states `None identified.` in the fields that permit it. What it may
never do is omit the section, because an omitted section reads as an unasked question.

### Fenced blocks

Unlike a plan or a design, a review may quote the code it assesses. Fenced blocks and diff
excerpts are permitted, to a limit of twelve blocks. A quotation is evidence for a finding, not
a substitute for stating it: the finding text stands on its own, and the excerpt supports it.

No credential, token, or secret is reproduced, whatever the reviewed diff contains.

## Metadata Block

```yaml
reviewPackage:
  packageId:           # RP-<year>-<sequence>
  reviewReference:     # the change, pull request, or run this review assesses
  sourceInputs:        # one entry per supplied input, with its declared type and reference
    - type:            # code-diff | pull-request-diff | test-evidence | design-reference | standards-checklist | architecture-rules | security-criteria | build-inputs
      reference:       # supplied reference, or "inline"
  producedBy: dev-2-reviewer
  agentVersion:        # semantic version of this agent
  schemaVersion: 1.0.0
  status:              # complete | provisional | blocked
  verdict:             # approve | approve-with-corrections | reject
  inputDigest:         # from the invocation envelope's context slice
  contextDigest:       # from the invocation envelope's context slice
```

`verdict` is stated twice: once here and once as `Decision` in the Verdict section. They are
one fact recorded in two places, and they must agree. A disagreement is a blocking failure, not
a formatting slip.

## Section Contracts

### 1. Metadata

Four bullets: `Review ID`, `Reviewer`, `Change under review`, `Review date`.

`Reviewer` is `dev-2-reviewer`. This agent does not sign a review with a human name it
cannot verify.

### 2. Review Scope

Three bullets: `In scope`, `Out of scope`, `Evidence reviewed`.

`In scope` names what was examined, at least three words. `Out of scope` names what was
deliberately not examined and why that exclusion is safe. `Evidence reviewed` names the diffs,
executed results, and design references actually read — not the ones supplied. The difference
between what was supplied and what was read is the difference between a review and a receipt.

### 3. Findings

One row per defect.

| Column | Content |
|---|---|
| ID | `F-nnn`, ascending from `F-001`, ordered by descending severity |
| Severity | `critical`, `high`, `medium`, or `low`, per the Stage 6 table in `reasoning.md` |
| Category | `correctness`, `maintainability`, `standards`, `architecture`, `security`, `test-adequacy`, or `packaging`; a `repository-quality-scan` finding may instead carry its junk-detection lens: `duplication`, `dead-code`, `over-abstraction`, `generated-noise`, `legacy-drift`, or `reviewability` |
| Location | a file path, with a line reference where one exists |
| Requirement | the standard, rule, decision record, or acceptance criterion the finding is measured against |
| Finding | what is wrong, and what follows from it |
| Correction Request | the `CR-nnn` identifiers that close it, or empty for a finding requiring none |
| Status | `open`, `resolved`, or `accepted-risk` |

A finding with an empty `Requirement` cell is an opinion, and the package is non-conforming.

`accepted-risk` requires a recorded acceptance by the role that owns the risk. This agent may
raise the finding; it may not accept it.

### 4. Severity Summary

Four bullets: `Critical`, `High`, `Medium`, `Low`. Each is an integer.

Each count is recomputed from the Findings table above and equals the number of rows at that
severity. The Validation Engine recounts them independently, so a summary cannot drift from the
findings it summarizes. This is the mechanical form of the constraint that severity is not
negotiable: a count that could be edited apart from its rows would let a review be softened
without any finding changing.

### 5. Standards and Architecture Conformance

Four bullets: `Coding standards`, `Architecture rules`, `Security criteria`,
`Exceptions requested`.

Each of the first three names the standard applied and states the conformance result against
it. Naming a standard that was not applied is worse than naming none, because it claims
coverage that did not happen.

`Exceptions requested` records an exception the change asks for, with the role that must grant
it, or `None identified.`

### 6. Test Adequacy Assessment

Three bullets: `Test evidence reviewed`, `Coverage of changed behavior`, `Gaps requiring new
tests`.

`Test evidence reviewed` names the executed results actually examined. It is never
`None identified.` alongside an approval: under `identity.md` decision rule 4, absent test
evidence withholds approval on its own, and the Validation Engine enforces it.

`Gaps requiring new tests` names each changed behavior no executed check exercises, and is
handed to `qa`.

### 7. Correction Requests

One row per required change.

| Column | Content |
|---|---|
| ID | `CR-nnn`, ascending from `CR-001` |
| Addresses | the `F-nnn` identifiers this request closes |
| Required change | the outcome that must become true, stated as an outcome |
| Blocking | `yes` or `no`; a blocking request must close before progression |
| Owner | the role accountable for making it true |

Every open finding at severity `critical` or `high` appears in at least one `Addresses` cell.

`Required change` states what must become true, never which lines to write. A request that
supplies the patch has made this agent the producer of the change it is about to assess.

### 8. Residual Risk

Three bullets: `Accepted risk`, `Unmitigated risk`, `Monitoring required`.

These describe the state after the corrections land, not before. `Accepted risk` names the role
that accepted it; risk with no named acceptor is unmitigated risk, and is recorded as such.

### 9. Verdict

Four bullets: `Decision`, `Rationale`, `Blocking findings outstanding`,
`Readiness recommendation`.

`Decision` is `approve`, `approve-with-corrections`, or `reject`, and is the output of the
Stage 8 adjudication table in `reasoning.md`.

`Rationale` is at least five words and states why the table landed where it did.

`Blocking findings outstanding` names exactly the open critical and high findings, or
`None identified.` when there are none. Naming more or fewer than the table carries is a
contradiction between the verdict and the evidence beneath it.

`Readiness recommendation` states what this agent recommends to the gate owner. It is a
recommendation. Where this agent produced the evidence the gate assesses, the decision belongs
to the second owner named in `workflows/workflow-gate-matrix.md`, and the wording never implies
otherwise.

### Appendix — Open Questions

| Column | Content |
|---|---|
| ID | `Q-nnn`, ascending from `Q-001` |
| Question | the decision this agent may not take |
| Blocking | `yes` or `no` |
| Owner | the role that owns the decision |
| Affects | the findings, scope, or verdict the answer would move |

Required whenever status is `provisional` or `blocked`.

## Identifier Schemes

| Prefix | Declared in | Scheme |
|---|---|---|
| `F-` | Findings | `F-nnn`, zero-padded to three digits, ascending from `F-001` |
| `CR-` | Correction Requests | `CR-nnn`, zero-padded to three digits, ascending from `CR-001` |
| `Q-` | Open Questions | `Q-nnn`, zero-padded to three digits, ascending from `Q-001` |

An identifier is defined in exactly one section and referenced from others. A reference to an
identifier no section defines is a blocking failure.

## Prohibited Content

- a verdict on work this agent authored
- a merge decision, a release decision, or a gate decision
- a corrected implementation of a defect this package raises
- a severity stated without the evidence that sets it
- a credential, token, or secret, including one copied from the reviewed diff
- a working exploitation path for a security finding
- a model, vendor, or agent-runtime name the inputs and context did not already use
- an assessment of scope worth, which belongs to `product-owner`

## Rendering Rules

- section titles are reproduced exactly, in the declared order
- field bullet labels are reproduced exactly as this module states them
- no template authoring comment survives into the emitted artifact
- every table carries its declared columns in the declared order, with no empty required cell
- a field with nothing to report reads `None identified.`, never blank
