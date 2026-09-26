# Architecture Decision Record D-007

## Metadata

- ADR ID: D-007
- Title: Owner approval as a transition state, not a configuration value
- Date: 2026-09-26
- Status: Proposed
- Owners: Design Gate owners `omn-architect` and `omn-tech-lead`; acceptance rests with `omn-tech-lead` (F-014)
- Related Work Items: MC-2 approval-instrumentation constraint and its settled individual-approval decision; the upstream risk `RK-005`; execution plan tasks `T-014` and `T-019`

## Context

- Problem statement: the owner approves every publication individually, this is a mandatory human step rather than a configurable default, it relaxes only by an explicit recorded later decision, and the minutes it costs must be measured from the first approval so a relaxation threshold can later rest on a baseline rather than on an assumption.
- Business and technical constraints: C-010, C-011 and C-016.
- Current architecture baseline: F-013 and F-019.

## Decision

- Selected option: O-002, on its approval dimension.
- Decision statement: the awaiting-owner-approval state sits in the gate's transition table; the published state is reachable only through it; the table is code and the state has no representation in the configuration register, so the step cannot be configured away. The approval row is unique per item, item version and gate, so an approval cannot be reused for a changed version. The elapsed minutes are derived from a presented-at timestamp written on state entry and a decided-at timestamp written at the verdict, rather than entered, so the figure is a measurement from the first approval onward. Send-backs are recorded with their reason. Release credential issuance additionally requires a gate-pass token naming the exact version, so a bypass of the state still has no credential.
- Scope of impact: M-009, M-010 and M-007, with M-012 affected by exclusion.

## Alternatives Considered

1. O-001, on its approval dimension: a configuration flag requiring owner approval, defaulted on.
- Benefits: trivial to build, and a later relaxation would be a configuration change.
- Risks: a flag is by definition configurable away, which is the one thing the standing decision forbids.
- Why not selected: eliminated, because it violates C-010.

2. O-003, on its approval dimension: an external service holding the approval step.
- Benefits: a purpose-built review surface for the owner.
- Risks: the mandatory step and its evidence live outside the record that must answer for them, and it is a second component against C-015.
- Why not selected: rejected, because it separates the approval from the audit record `AC-018` measures.

3. O-004, on its approval dimension: an approval check performed by each capability router before release work.
- Benefits: the check sits next to the work it gates.
- Risks: the mandatory step becomes nine checks, and the elapsed-minutes measurement has nine possible clocks.
- Why not selected: rejected, because `AC-018` and `AC-019` need one recorded approval per item version, not a set of agreeing checks.

## Consequences

- Positive outcomes expected: `AC-018` and `AC-019` are satisfied, and the measured distribution becomes the baseline from which the clean-record threshold is later proposed rather than assumed.
- Tradeoffs accepted: relaxing the step later is a code change rather than a configuration change, which is the intended cost, and the owner remains the throughput limit the upstream risk names; this design measures that limit rather than removing it.
- Risks introduced: R-012.

## Validation Plan

- Metrics to monitor: approvals lacking an elapsed-minutes value, expected zero; approvals accepted for an item version other than the one they were bound to, expected zero; configuration keys able to bypass the state, expected zero.
- Verification checkpoints: the action set and gate predicates at P-004, the gate placed inside the lifecycle at P-009, and the measurement set designed at P-015.
- Rollback or reversal conditions: only an explicit recorded owner decision relaxes the step, and such a decision supersedes this record rather than amending it.

## Approval

- Architect: unsigned; the producing role does not accept its own record.
- Tech Lead: unsigned.
- Product Owner (if scope-impacting): unsigned; the standing individual-approval decision is the one this record implements.
