# Template: Review Package

## Usage

Canonical artifact template for `review-package.md`: the single artifact type that carries a
review's findings, its correction requests, and its verdict.

Five phases across four workflows ask for a review output. Every one of them now names the
file in its Output Artifact column:

| Workflow | Phase | Name in the Phase Model | Producer |
|---|---|---|---|
| `implement-feature` | `quality-review` | `review-package.md` | `dev-2-reviewer` |
| `review-pull-request` | `code-quality-review` | `review-package.md` | `dev-2-reviewer` |
| `review-pull-request` | `structural-compliance` | `review-package.md` | `architect` |
| `code-quality-scan` | `repository-quality-scan` | `review-package.md` | `dev-2-reviewer` |
| `release` | `artifact-packaging` | `review-package.md` | `dev-2-reviewer` |

They are one artifact type. Each is a severity-classified findings set over a defined review
scope, closing with a readiness decision, and the Validation Engine
(`runtime/review_package_validator.py`) judges all of them against this one contract. The
Category column is what distinguishes a code-quality review from a structural and security
assessment; the structure does not change with the lens.

Section titles, section order, and identifier schemes are fixed. Sections are never omitted.
A section with nothing to report reads `None identified.`

Identifier schemes: findings `F-nnn`, correction requests `CR-nnn`, open questions `Q-nnn`.
All zero-padded to three digits and ascending.

Unlike a plan or a design, a review package may quote the code it reviews, so fenced blocks
and diff excerpts are permitted here. The prohibition this template does keep is the one
every agent contract shares: no model, vendor, or provider is named.

The binding behavioural contracts are the module set under `agents/dev-2-reviewer/` —
`output.md` for structure and `quality.md` for the checks — and, for the structural lens,
`agents/architect/quality.md`. Three reviewer rules are enforced mechanically: a
critical or high finding cannot be approved away, a severity count cannot drift from the
findings it summarises, and missing test evidence cannot coexist with an approval.

---

```yaml
reviewPackage:
  packageId:
  reviewReference:
  sourceInputs:
    - type:          # code-diff | pull-request-diff | test-evidence | design-reference | standards-checklist | architecture-rules | security-criteria | build-inputs
      reference:     # supplied reference, or "inline"
  producedBy:        # dev-2-reviewer | architect
  agentVersion:
  schemaVersion: 1.0.0
  status:            # complete | provisional | blocked
  verdict:           # approve | approve-with-corrections | reject
  inputDigest:
  contextDigest:
```

## Metadata

- Review ID:
- Reviewer:
- Change under review:
- Review date:

## Review Scope

- In scope:
- Out of scope:
- Evidence reviewed:              <!-- diffs, test runs, design references actually read -->

## Findings

<!-- One row per finding. Severity is critical | high | medium | low, per the `severity`
vocabulary in `rule-engine.md`. Category names the lens: correctness, maintainability,
standards, architecture, security, test-adequacy, packaging — or, for a
repository-quality-scan, the junk-detection lens: duplication, dead-code, over-abstraction,
generated-noise, legacy-drift, reviewability. Location is a file path with a
line reference where one exists. Requirement names the standard, rule, or acceptance
criterion the finding is measured against; a finding with no requirement is an opinion.
Status is open | resolved | accepted-risk. -->

| ID | Severity | Category | Location | Requirement | Finding | Correction Request | Status |
|---|---|---|---|---|---|---|---|
| `F-001` | | | | | | `CR-001` | |

## Severity Summary

<!-- Counts over the Findings table above. They are recomputed by the validator, so they
cannot drift from the findings they summarise. -->

- Critical:
- High:
- Medium:
- Low:

## Standards and Architecture Conformance

- Coding standards:
- Architecture rules:
- Security criteria:
- Exceptions requested:            <!-- an approved exception, or `None identified.` -->

## Test Adequacy Assessment

- Test evidence reviewed:          <!-- never `None identified.` alongside an approval -->
- Coverage of changed behavior:
- Gaps requiring new tests:

## Correction Requests

<!-- One row per required change. Addresses names the finding identifiers it closes.
Blocking is yes | no: a blocking request must be closed before progression. -->

| ID | Addresses | Required change | Blocking | Owner |
|---|---|---|---|---|
| `CR-001` | `F-001` | | | |

## Residual Risk

- Accepted risk:
- Unmitigated risk:
- Monitoring required:

## Verdict

- Decision:                        <!-- approve | approve-with-corrections | reject -->
- Rationale:
- Blocking findings outstanding:   <!-- finding identifiers, or `None identified.` -->
- Readiness recommendation:

## Open Questions

<!-- Appendix. Required whenever status is provisional or blocked. -->

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
