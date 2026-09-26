# Architecture Decision Record D-002

## Metadata

- ADR ID: D-002
- Title: Stack recommendation for the Design Gate
- Date: 2026-09-26
- Status: Proposed
- Owners: the owner decides under C-017; Design Gate accepting owner `omn-tech-lead` (F-014)
- Related Work Items: MC-2 note on technology choice; the scope definition's exclusion of mechanism and its matching scope decision

## Context

- Problem statement: the repository is greenfield and no prior artifact selects a language, runtime, datastore or hosting model, yet the system is a long-running asynchronous job pipeline with durable state, an audit record that must answer years later, per-operation cost accounting and a human approval step in the middle of the flow.
- Business and technical constraints: C-004, C-005, C-011, C-012, C-015, C-017 and C-020.
- Current architecture baseline: F-001, F-002 and F-003, with assumptions A-001, A-004 and A-005.

## Decision

- Selected option: O-005, as a recommendation only.
- Decision statement: the recommended stack is the .NET runtime with PostgreSQL as the single datastore holding relational state, the append-only record and the durable job queue as a transactional claim table, hosted as one long-running service on a single node. The selection itself belongs to the owner at the Design Gate under C-017; this record recommends and does not decide. The decisive criteria are that exactly-once accounting under C-004 survives a crash only if the state change and the queue entry commit together, and that the arithmetic under C-005 must stay exact and re-derivable after a price row is superseded. Operator familiarity, recorded at A-001, is applied only as the tiebreaker between O-005 and O-007.
- Scope of impact: M-017 and the claim mechanism inside M-013. Every other module named in this design is stack-independent.

## Alternatives Considered

1. O-006, the Python runtime with PostgreSQL for state and a dedicated broker for the queue.
- Benefits: matches the surrounding tooling recorded at A-001, with a broad adapter ecosystem.
- Risks: the state change and the queue entry commit to separate systems, so C-004 needs an outbox, and compile-time enforcement of the dependency direction D-005 relies on is weaker.
- Why not selected: it adds a second operable component against C-015 and moves an invariant out of the datastore into application code.

2. O-007, the .NET runtime with a single-file datastore and an in-process queue.
- Benefits: the cheapest shape to operate, one file and one process, which C-015 rewards.
- Risks: single-writer concurrency limits the independent progress C-020 requires, and the absence of an exact decimal type puts C-005 arithmetic in application code where the audit cannot see the rounding.
- Why not selected: it trades two named quality attributes for operability that O-005 already provides adequately.

## Consequences

- Positive outcomes expected: the stack-dependent surface is one port pair and the hosting shape, so a different selection changes M-017 and the claim mechanism and nothing else.
- Tradeoffs accepted: a relational datastore is a heavier operational unit than a single file for a one-person company, and familiarity is used only as a tiebreaker rather than as a criterion in its own right.
- Risks introduced: R-005, R-011, R-016, R-017 and R-019.

## Validation Plan

- Metrics to monitor: modules outside M-017 and M-013 requiring change under a different selection, expected zero; cost figures whose arithmetic cannot be re-derived from a retained price row, expected zero.
- Verification checkpoints: the port definitions at P-008 before any datastore-specific realization, and the first metered item measured against assumption A-005.
- Rollback or reversal conditions: if assumption A-004 proves false, or the first metered item shows volume materially above assumption A-005, the options are re-evaluated against the same six criteria and this record is superseded rather than amended.

## Approval

- Architect: unsigned; the producing role does not accept its own record.
- Tech Lead: unsigned.
- Product Owner (if scope-impacting): not scope-impacting; the selection is reserved to the owner under C-017.
