# Command Specification: /release

## Purpose
Prepare and execute a safe production release with controlled risk.

## Inputs
- Release scope and target version.
- Deployment window.
- Rollback and communication plan.

## Workflow Triggered
Release (`workflows/release.md`).

## Expected Outputs
- Release readiness report.
- Deployment execution and health status.
- Final release notes and follow-up actions.

## Success Criteria
- Deployment completes within health thresholds.
- No unresolved critical release blockers.
- Stakeholder communication is complete.

## Failure Handling
- Trigger rollback on breached health criteria.
- Pause rollout and return to validation on anomalies.
- Escalate operational incidents to tech lead.
