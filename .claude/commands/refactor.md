# Command Specification: /refactor

## Purpose
Improve code maintainability while preserving observable behavior.

## Inputs
- Refactor target and rationale.
- Behavioral invariants.
- Risk tolerance and timeline.

## Workflow Triggered
Refactor (`workflows/refactor.md`).

## Expected Outputs
- Refactor plan and invariants.
- Code restructuring with tests.
- Verification evidence for behavior parity.

## Success Criteria
- Invariants remain unchanged.
- Quality metrics improve or remain acceptable.
- No unresolved critical regressions.

## Failure Handling
- Revert changes that break invariants.
- Expand safety tests when hidden coupling appears.
- Escalate structural risks to architect.
