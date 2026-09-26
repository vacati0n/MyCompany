# Architecture Decision Record D-006

## Metadata

- ADR ID: D-006
- Title: Operations recorded at the boundary, in the transaction, per attempt
- Date: 2026-09-26
- Status: Proposed
- Owners: Design Gate owners `omn-architect` and `omn-tech-lead`; acceptance rests with `omn-tech-lead` (F-014)
- Related Work Items: MC-2 constraints on cost tracking and on the recorded history; execution plan tasks `T-003`, `T-004`, `T-005`, `T-009` and `T-010`

## Context

- Problem statement: every AI operation, including failed and retried ones, must be accounted exactly once with attribution to item, channel, department, agent, capability and the capability actually used; cost per item and the monthly total must follow from the records alone; and the record must still answer years later, after the prices that produced it have changed.
- Business and technical constraints: C-004, C-005, C-006, C-014 and C-018.
- Current architecture baseline: F-004, F-009 and F-020, with assumptions A-002 and A-006.

## Decision

- Selected option: O-002, on its accounting dimension.
- Decision statement: the resolution boundary writes the operation record in the same transaction as the work it performed; each attempt is its own record with its own outcome; the applied price row is referenced rather than only the computed figure; item and period attribution are denormalized onto the record at write time; the audit entry is written in the same transaction; and budget thresholds are evaluated on the write, raising one alert per budget, period and threshold. Cached units are counted and priced separately, so the caching sensitivity the upstream analysis records becomes measurable rather than asserted.
- Scope of impact: M-005, M-006, M-007 and M-017.

## Alternatives Considered

1. O-003, on its accounting dimension: the consumer records the operation after the call returns.
- Benefits: keeps the boundary simple and the record close to the caller that understands the work.
- Risks: a dual write, so a crash between the operation and the record loses a row and exactly-once becomes best effort.
- Why not selected: rejected, because it makes C-004 a probability rather than a property.

2. O-001, on its accounting dimension: reconciliation from a provider billing export.
- Benefits: figures that agree with the invoice by construction.
- Risks: an export cannot carry the attribution tuple and cannot count failed attempts.
- Why not selected: eliminated, because it violates C-004.

3. O-004, on its accounting dimension: each capability router keeping its own counters, aggregated later.
- Benefits: no shared write path.
- Risks: nine counters to reconcile, and the exactly-once guarantee becomes an aggregation convention.
- Why not selected: rejected, because C-004 and C-005 would rest on reconciliation rather than on a single recorded row per attempt.

## Consequences

- Positive outcomes expected: `AC-003`, `AC-004` and `AC-027` become computable from the records alone, and the derivation survives a price change, which is what makes the record answerable years later.
- Tradeoffs accepted: write-time work and a denormalization that must stay correct, and a transactional write path that couples the accounting to the datastore, which is part of why D-002 recommends the option it does.
- Risks introduced: R-006, R-009, R-013 and R-015.

## Validation Plan

- Metrics to monitor: operations with a missing or incomplete attribution, expected zero; records lost across an induced crash between the operation and its write, expected zero; cost figures not re-derivable from a retained price row, expected zero; duplicate alerts per budget, period and threshold, expected zero.
- Verification checkpoints: the accounting contract at P-003, the budget scopes at P-011, and the measurement set designed at P-015.
- Rollback or reversal conditions: if assumption A-002 fails and providers return no usage counts, the measurement-or-estimate marker carries the degradation and this record stands; if the transactional write proves infeasible in the selected stack, the record is superseded rather than relaxed to a dual write.

## Approval

- Architect: unsigned; the producing role does not accept its own record.
- Tech Lead: unsigned.
- Product Owner (if scope-impacting): not scope-impacting.
