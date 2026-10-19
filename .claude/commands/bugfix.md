# Command Specification: /bugfix

## Purpose
Resolve a defect through triage, root-cause analysis, fix, and regression verification.

## Inputs
- Bug summary and observed behavior.
- Reproduction steps or evidence.
- Severity and business impact.

## Workflow Triggered
Fix Bug (`workflows/fix-bug.md`).

## Expected Outputs
- Root-cause report.
- Corrective code change with regression test.
- Closure record and release impact update.

## Success Criteria
- Defect is no longer reproducible.
- Root cause is documented with evidence.
- Regression checks pass.

## Failure Handling
- Escalate when reproduction is not possible.
- Re-open diagnosis if fix validation fails.
- Roll back unsafe changes and restart remediation.
