# Architecture Decision Record D-001

## Metadata

- ADR ID: D-001
- Title: Mediated capability resolution over a three-tier routing position
- Date: 2026-09-26
- Status: Proposed
- Owners: Design Gate owners `omn-architect` and `omn-tech-lead`; under the Producer Exclusion Rule acceptance rests with `omn-tech-lead` (F-014)
- Related Work Items: MC-2; execution plan tasks `T-001`, `T-002`, `T-028` and `T-030`; the upstream blocking risk recorded as `RK-004`

## Context

- Problem statement: capacity must never be obtained by means a provider's terms forbid, no critical capability may rest on one provider, and a critical task must never run below its quality floor. Each of these must hold whichever component is calling, which is what makes them structural rather than procedural.
- Business and technical constraints: C-001, C-002, C-003, C-004, C-013 and C-015.
- Current architecture baseline: F-001, F-004, F-005, F-006, F-007, F-020, F-021 and F-022.

## Decision

- Selected option: O-002.
- Decision statement: every component reaches an external capability only through one mediating resolution boundary, which resolves over an admitted route table of exactly three tiers per capability class and an independently checked forbidden-source register, applies the quality-floor filter before tier ordering, and holds or escalates rather than downgrading. The emergency tier is a typed union of provider route, hold-and-escalate, and non-AI substitute.
- Scope of impact: M-001, M-002, M-003 and M-004, with M-013 and M-015 as consumers of the capability contract.

## Alternatives Considered

1. O-001, direct provider calls from every consumer behind a shared client library.
- Benefits: the fewest components and no indirection.
- Risks: a forbidden source and an unmetered operation are both reachable from any consumer.
- Why not selected: eliminated, because it violates C-001 and C-004.

2. O-003, consumers retaining adapter dependencies behind a mandatory interceptor pipeline.
- Benefits: satisfies every hard constraint while the pipeline is correctly configured, with the second-smallest impact surface.
- Risks: a directly constructed adapter is a compiling, working, unmetered path to a provider.
- Why not selected: enforcement would be true of the configuration rather than of the build, and C-001 and C-004 are verified against the build.

3. O-004, one independent router per capability class.
- Benefits: no shared boundary whose failure stops every capability.
- Risks: the floor and the refusal are enforced nine times, and drift between them is invisible.
- Why not selected: the largest impact surface in the family, no reuse leverage, and nine components to operate against C-015.

## Consequences

- Positive outcomes expected: `AC-001` and `AC-002` become structural properties rather than configured ones, the upstream blocking risk closes, and the consumer-subscription account pool is absent by construction (F-022).
- Tradeoffs accepted: one boundary is a single point of failure for every capability, and a held critical task stops the pipeline rather than degrading silently.
- Risks introduced: R-001, R-004 and R-010.

## Validation Plan

- Metrics to monitor: reachable routes for each forbidden-source entry, expected zero; resolutions returning a route below the request's floor, expected zero; failures carrying no recorded reason, expected zero.
- Verification checkpoints: route admission at P-001, and the both-routes-unavailable demonstration per capability class designed at P-015.
- Rollback or reversal conditions: if a capability's terms position makes three commercially reachable tiers impossible and the typed union is rejected at the gate, the tier rule of C-002 must be renegotiated before this record can stand.

## Approval

- Architect: unsigned; the producing role does not accept its own record.
- Tech Lead: unsigned.
- Product Owner (if scope-impacting): not scope-impacting.
