# Architecture Decision Record D-005

## Metadata

- ADR ID: D-005
- Title: Zero AI cost as a dependency-direction property
- Date: 2026-09-26
- Status: Proposed
- Owners: Design Gate owners `omn-architect` and `omn-tech-lead`; acceptance rests with `omn-tech-lead` (F-014)
- Related Work Items: MC-2 code-first constraint; the upstream criterion `EC-008`; execution plan tasks `T-018` and `T-029`

## Context

- Problem statement: work whose output is fully determined by its inputs and a rule must not call a model, must be named, and must be shown to carry zero AI cost. A rule enforced by review is a rule that holds until someone is in a hurry.
- Business and technical constraints: C-004 and C-009.
- Current architecture baseline: F-008 and F-020.

## Decision

- Selected option: O-002, on its deterministic-work dimension.
- Decision statement: the named set F-008 records lives in M-014, which holds no dependency on M-001, M-003 or M-010, and the direction is verified by a build-time boundary test. A model call is therefore not expressible from those modules, and the zero-cost check is a measurement over cost rows attributed to the named set together with the boundary test. The named set is render, mux, encode, transcode, loudness normalisation and aspect conform; subtitle timing by forced alignment, caption emission and chapter timestamp arithmetic; the asset and rights ledger and the join proving every published asset carries a licence record; the library registration state machine and its precondition on first publication; the gate's blocking, release and approval-token verification; schedule arithmetic for the operating week and the production buffer; cost metering, the per-item rollup and the headroom calculation against the tier cap; provider routing, quota and spend accounting, failover selection, retry, backoff and circuit breaking; exact and near-duplicate detection by content and perceptual hashing; metadata population from templates and thumbnail variant compositing; and audit logging with the evidence each gate verdict rests on.
- Scope of impact: M-014, with the direction constraint recorded against M-001 and M-003.

## Alternatives Considered

1. O-001, on its deterministic-work dimension: a naming convention for rule-determined tasks plus review.
- Benefits: no structure to build.
- Risks: the model call stays reachable, so zero cost is an expectation rather than a property.
- Why not selected: eliminated, because it violates C-009.

2. O-003, on its deterministic-work dimension: a runtime guard rejecting a model call originating in a named task.
- Benefits: catches the call without restructuring.
- Risks: it detects only after the dependency exists, so the property is a test result, and the guard is itself a component to maintain.
- Why not selected: a build-time impossibility is stronger than a runtime rejection and costs less to operate.

3. O-004, on its deterministic-work dimension: per-capability routers with deterministic helpers embedded in each.
- Benefits: locality between a capability and its rule-determined helpers.
- Risks: the deterministic code sits inside the modules that can call a provider, so the direction property cannot be stated at all.
- Why not selected: rejected, because it makes the property untestable rather than merely harder to hold.

## Consequences

- Positive outcomes expected: `AC-017` is satisfied by construction and measured for confirmation, and the named set is visible in the structure rather than only in a document.
- Tradeoffs accepted: M-014 cannot use the resolution boundary even where a model would be convenient, so a genuinely judgement-bearing task must be moved out of M-014 deliberately rather than absorbed into it.
- Risks introduced: R-003.

## Validation Plan

- Metrics to monitor: dependencies from M-014 onto M-001, M-003 or M-010, expected zero; AI cost attributed to the named set, expected zero.
- Verification checkpoints: the boundary test established at P-007 before deterministic work is realized, and run on every build thereafter; the measurement set designed at P-015.
- Rollback or reversal conditions: if a member of the named set proves not to be fully determined by its inputs and a rule, it is removed from the set by an explicit recorded change rather than by adding a dependency to M-014.

## Approval

- Architect: unsigned; the producing role does not accept its own record.
- Tech Lead: unsigned.
- Product Owner (if scope-impacting): not scope-impacting.
