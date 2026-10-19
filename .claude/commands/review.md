# Command Specification: /review

## Purpose
Run structured review of proposed changes before merge or release progression.

## Inputs
- Pull request scope.
- Code diff and linked requirements.
- Test evidence and risk notes.

## Workflow Triggered
Review Pull Request (`workflows/review-pull-request.md`).

## Expected Outputs
- Severity-based review findings.
- Required correction list.
- Merge readiness decision.

## Success Criteria
- Critical findings resolved.
- Test and documentation coverage is sufficient.
- Merge decision is explicit.

## Failure Handling
- Return pull request to implementation for fixes.
- Add targeted review passes for unresolved risks.
- Escalate disputes to tech lead and orchestrator.
