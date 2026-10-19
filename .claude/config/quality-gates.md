# Configuration: Quality Gates

## Decision Policy

Every gate below is decided by a human by default. A team may opt in to runtime
auto-approval for gates whose evidence is unambiguously clean via
`config/gate-policy.json` (`mode: auto-on-clean-evidence`) — see `gate-policy.md` for
the conditions, the audit-trail guarantees, the per-gate `pinned` escape hatch, and a
worked example of a run where three of four gates auto-approve and one escalates to a
human over a blocking open question.

## Gate 1: Scope Readiness

- Acceptance criteria are explicit and testable.
- Dependencies and non-goals are documented.

## Gate 2: Architecture Readiness

- Design aligns with Clean Architecture boundaries.
- External contracts and migrations are defined.

## Gate 3: Implementation Quality

- Coding standards followed.
- Tests cover success, failure, and edge paths.
- Reviewer findings resolved.

## Gate 4: Operational Readiness

- Observability and alerting are validated.
- Rollback strategy is documented and feasible.

## Gate 5: Release Readiness

- Release notes complete and accurate.
- QA verdict is go.
- Risk acceptance is explicit.
