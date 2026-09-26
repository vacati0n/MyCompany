# Architecture Decision Record D-004

## Metadata

- ADR ID: D-004
- Title: Credential material never enters the caller's scope
- Date: 2026-09-26
- Status: Proposed
- Owners: Design Gate owners `omn-architect` and `omn-tech-lead`; acceptance rests with `omn-tech-lead` (F-014)
- Related Work Items: MC-2 constraint on credentials, carrying the Technical Gate decision; execution plan tasks `T-015`, `T-016` and `T-017`

## Context

- Problem statement: credentials must live in a dedicated store, never appear in agent context, prompts, artifacts, logs or run evidence, be short-lived and scoped per task, be reachable for release only by the publishing role and only after the gate passes, never be shared across providers, accounts or channels, rotate and revoke without redeployment, and have every use audited. This record answers the credential-protection question the scope definition and the Framing Gate record as owed by this agent.
- Business and technical constraints: C-007, C-008 and C-011.
- Current architecture baseline: F-017 and F-018, with assumption A-007.

## Decision

- Selected option: O-002, on its credential dimension.
- Decision statement: a dedicated secret store sits outside the application process, and the broker is its only dependent. The broker issues an opaque short-lived scoped handle per task, bound to the job and stage identity, the capability, the route's provider account, a ttl and a use bound. The adapter exchanges that handle for the provider credential at the transport layer, after the request object has been assembled, so the value is never in scope where a prompt, artifact or log could read it. The credential holder key is the pair of provider account and channel and is unique, so sharing is unrepresentable. The secret reference is resolved at issuance time and never at start-up, so rotation takes effect on the next issuance and revocation invalidates live handles at their next presentation without redeployment. Release credentials are a distinct class whose issuance predicate requires the publishing role, a gate-pass token naming the exact item version, and zero open blocks. Every issuance and presentation is audited by identity and never by value.
- Scope of impact: M-010, M-003 and M-007, together with one new external dependency.

## Alternatives Considered

1. O-001, on its credential dimension: credentials injected into adapters at start-up from process configuration.
- Benefits: the simplest possible path.
- Risks: rotation and revocation require a restart, and the value sits in process configuration where any emission path can reach it.
- Why not selected: eliminated, because it violates C-008.

2. O-003, on its credential dimension: each module fetching its own credential from the store per task.
- Benefits: no broker to build.
- Risks: every module gains a store dependency, so the credential surface becomes the whole system and the never-in-context guarantee rests on every module behaving.
- Why not selected: it maximizes the surface the constraint exists to minimize.

3. O-004, on its credential dimension: a credential path per capability router.
- Benefits: each router owns its own provider relationship.
- Risks: nine issuance paths to audit and nine places a value could leak.
- Why not selected: rejected for the same surface reason, compounded by the operability cost against C-015.

## Consequences

- Positive outcomes expected: `AC-013` to `AC-016` become verifiable by sweep and demonstration, and credential sharing is unrepresentable rather than discouraged.
- Tradeoffs accepted: a new external dependency and an additional operable component against C-015, and a transport-layer exchange that constrains how adapters may be written.
- Risks introduced: R-002, R-007 and R-018.

## Validation Plan

- Metrics to monitor: occurrences of any held credential identifier across agent context, prompts, artifacts, logs and run evidence, expected zero; secret references with more than one holder, expected zero; rotations or revocations requiring a restart, expected zero.
- Verification checkpoints: the issuance contract at P-005, before any adapter holds a provider credential, and the sweep at each verification pass designed at P-015.
- Rollback or reversal conditions: if assumption A-007 fails and no dedicated store exists in the selected hosting shape, this record is withdrawn and the standard is re-derived rather than weakened in place, which is the consequence carried at Q-005.

## Approval

- Architect: unsigned; the producing role does not accept its own record.
- Tech Lead: unsigned.
- Product Owner (if scope-impacting): not scope-impacting.
