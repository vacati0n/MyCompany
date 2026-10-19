# Command Specification: /test

## Purpose
Execute targeted quality verification for active changes or release candidates.

## Inputs
- Scope under test.
- Test level requested (unit, integration, end-to-end, regression).
- Risk focus areas.

## Workflow Triggered
Review Pull Request (`workflows/review-pull-request.md`), whose `test-risk-validation` phase
this command owns.

`domain-model/command-specification.md` permits exactly one primary workflow per command, so
the primary mapping is the lifecycle whose purpose is verification of a specific change set.
Verification in other lifecycles is reached through their own commands and is not a second
mapping for this one:

| Lifecycle | Phase that carries verification | Entry command |
|---|---|---|
| Review Pull Request | `test-risk-validation` | `/test`, `/review` |
| Implement Feature | `quality-review` | `/implement` |
| Fix Bug | `regression-validation` | `/bugfix` |
| Refactor | `safety-net-establishment`, `behavioral-validation` | `/refactor` |
| Release | `candidate-validation` | `/release` |

## Expected Outputs
- Test execution summary.
- Defect list with severity and reproduction status.
- Quality risk assessment.

## Success Criteria
- Requested test suite completes with valid results.
- Critical-path scenarios pass.
- Blocking defects are identified and routed.

## Failure Handling
- Re-run failed tests after environment validation.
- Escalate flaky or non-deterministic failures.
- Return failing scope to implementation workflow.
