# Template: Validation Report

## Usage

Canonical artifact template for `validation-report.md`: the single artifact type that carries
a validation's acceptance results, its defects, its regression assessment, and its verdict.

Five phases across four workflows ask for a validation output. All five now name the file in
their Output Artifact column:

| Workflow | Phase | Producer |
|---|---|---|
| `fix-bug` | `regression-validation` | `qa` |
| `refactor` | `safety-net-establishment` | `qa` |
| `refactor` | `behavioral-validation` | `qa` |
| `review-pull-request` | `test-risk-validation` | `qa` |
| `release` | `candidate-validation` | `qa` |

They are one artifact type. Each is a criterion-by-criterion validation result over a defined
validation scope, carrying the defects it found and closing with a readiness verdict, and the
Validation Engine (`runtime/validation_report_validator.py`) judges all of them against this
one contract. The Validation Basis field is what distinguishes a regression run from a safety
net baseline or a release candidate check; the structure does not change with it.

A validation report is not a review package. A review judges whether a change is *sound* —
whether the code is correct, conformant, and maintainable. A validation judges whether the
delivered behavior *meets its criteria* — whether what was asked for is what the system now
does, and whether anything that used to work stopped working. The two produce different
artifacts because they answer different questions and are decided by different roles.

Section titles, section order, and identifier schemes are fixed. Sections are never omitted.
A section with nothing to report reads `None identified.`

Identifier schemes: acceptance criteria `AC-nnn`, defects `DF-nnn`, open questions `Q-nnn`.
All zero-padded to three digits and ascending.

A validation report quotes the output of the checks it ran, so fenced blocks and diff excerpts
are permitted here. The prohibition this template does keep is the one every agent contract
shares: no model, vendor, or provider is named.

The binding behavioural contract is the module set under `agents/qa/` — `output.md` for
structure and `quality.md` for the checks. Four QA rules are enforced mechanically: a criterion
with no evidence cannot be recorded as met, an execution count cannot drift from the results it
summarises, an open critical or high defect cannot be passed away, and a verdict cannot claim a
scope the report never validated.

---

```yaml
validationReport:
  reportId:
  validationReference:
  validationBasis:   # regression | safety-net | behavioral-parity | test-risk | release-candidate
  sourceInputs:
    - type:          # implementation-report | review-package | technical-design | bug-analysis | scope-definition | test-evidence | acceptance-criteria | regression-targets | packaging-evidence
      reference:     # supplied reference, or "inline"
  producedBy:        # qa
  agentVersion:
  schemaVersion: 1.0.0
  status:            # complete | provisional | blocked
  verdict:           # pass | pass-with-reservations | fail
  inputDigest:
  contextDigest:
```

## Metadata

- Validation ID:
- Validator:
- Change under validation:
- Validation date:

## Validation Scope

- In scope:            <!-- the behavior, criteria, and regression surface this run validated -->
- Out of scope:        <!-- what was deliberately not validated, and by whose decision -->
- Evidence examined:   <!-- the artifacts read and the checks run, each named -->

## Test Strategy

- Risk basis:          <!-- what made this the right depth of validation for this change -->
- Levels executed:     <!-- unit | integration | end-to-end | performance | security, as executed -->
- Environment:         <!-- where the checks ran, and how it differs from target -->
- Not executed:        <!-- levels deliberately skipped, with the reason -->

## Acceptance Criteria Results

<!-- One row per criterion actually validated. Method names how it was checked; Evidence
     names what proves the result. A criterion with no evidence may not read `met`. -->

| ID | Criterion | Source | Method | Result | Evidence |
|---|---|---|---|---|---|

## Execution Summary

<!-- These figures must recompute exactly from the Acceptance Criteria Results table. -->

- Criteria validated:
- Met:
- Not met:
- Blocked:

## Defects

<!-- Defects this validation found. Reproducibility is what separates a defect from an
     observation, so it is recorded per row rather than assumed. -->

| ID | Severity | Category | Location | Symptom | Reproducibility | Status |
|---|---|---|---|---|---|---|

## Regression Assessment

- Regression scope:            <!-- the existing behavior this change could have broken -->
- Regressions detected:        <!-- what broke, or `None identified.` -->
- Coverage of changed behavior: <!-- which altered behavior the executed checks actually reach -->
- Untested areas:              <!-- reachable behavior no executed check covers -->

## Residual Risk

- Accepted risk:
- Unmitigated risk:
- Monitoring required:

## Verdict

- Decision:                        <!-- pass | pass-with-reservations | fail -->
- Rationale:
- Blocking defects outstanding:    <!-- the open critical and high defects, by ID, or `None identified.` -->
- Readiness recommendation:        <!-- a recommendation, never a decision; go/no-go rests with the gate owner -->

## Open Questions

<!-- `Q-nnn` items, or `None identified.` A provisional or blocked report carries at least one. -->
