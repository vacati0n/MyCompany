# Architecture Decision Record D-003

## Metadata

- ADR ID: D-003
- Title: Least privilege enforced by four independent structural layers
- Date: 2026-09-26
- Status: Proposed
- Owners: Design Gate owners `omn-architect` and `omn-tech-lead`; acceptance rests with `omn-tech-lead` (F-014)
- Related Work Items: MC-2 constraint on least-privilege permissions; execution plan tasks `T-012`, `T-013`, `T-014` and `T-031`

## Context

- Problem statement: the copyright responsibility must be able to block publication and unable to publish, the publishing responsibility must be unable to override a block or to release without a complete approval set, and no role may approve an exception to a control it is itself subject to. Each is verified by demonstrating a refusal, so each must fail in the structure rather than in a check a caller could skip.
- Business and technical constraints: C-007, C-010, C-016 and C-019.
- Current architecture baseline: F-016 and F-019.

## Decision

- Selected option: O-002, on its authority dimension.
- Decision statement: authority is a closed enumerated action set held as data; the gate evaluates predicates over that set; the transition table is code and holds the owner-approval state; and credential issuance is a fourth predicate. The copyright role's set holds the block-place, block-clear and asset-verify actions and not the publish action, and the publishing role's set is disjoint on every Block action, so overriding a block is not an expressible action. Exception approval carries a write-time invariant that the approver is neither the requester, nor a subject role of the control, nor a holder of the action the exception would grant, and the subject-role relation is a required attribute of a control.
- Scope of impact: M-008, M-009 and M-010.

## Alternatives Considered

1. O-001, on its authority dimension: policy checks written at each call site by convention.
- Benefits: nothing to build.
- Risks: an absent permission is unprovable, and a missed call site is a silent hole.
- Why not selected: eliminated, because it violates C-007.

2. O-003, on its authority dimension: an external policy engine evaluating rules at runtime.
- Benefits: rules change without a code change.
- Risks: runtime-mutable rules weaken the non-configurable property C-010 requires, and the engine is a second component against C-015.
- Why not selected: it converts a structural property into a configured one.

3. O-004, on its authority dimension: per-capability routers each carrying their own policy copy.
- Benefits: local reasoning inside each router.
- Risks: nine copies of the refusal logic, with drift invisible until a demonstration fails.
- Why not selected: rejected for duplicated enforcement and the operability cost against C-015.

## Consequences

- Positive outcomes expected: each of the three refusals fails in four independent places, so `AC-010`, `AC-011` and `AC-012` are demonstrable rather than asserted.
- Tradeoffs accepted: deliberate duplication, and an authority change becomes a code change wherever it touches the action set or the transition table.
- Risks introduced: R-008 and R-012.

## Validation Plan

- Metrics to monitor: actions in the publishing role's set intersecting the Block actions, expected zero; controls admitted without a subject role, expected zero; exception approvals where the approver is the requester or a subject role, expected zero.
- Verification checkpoints: the action set at P-004, the gate predicates at P-009, and the refusal demonstrations designed at P-015.
- Rollback or reversal conditions: if the closed action set cannot express a required operation without an escape hatch, the layering is re-derived before any hatch is added.

## Approval

- Architect: unsigned; the producing role does not accept its own record.
- Tech Lead: unsigned.
- Product Owner (if scope-impacting): not scope-impacting.
