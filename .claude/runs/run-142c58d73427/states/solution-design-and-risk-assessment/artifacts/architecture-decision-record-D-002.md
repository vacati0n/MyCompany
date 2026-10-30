# Architecture Decision Record

## Metadata

- ADR ID: D-002
- Title: Block format
- Date: 2026-10-30
- Status: Proposed
- Owners: tech-lead (accepting Design Gate owner under the producer exclusion rule)
- Related Work Items: MC-19, run run-142c58d73427, technical design TD-MC19-PAGE-BUTTON, planner tasks T-003, T-014

## Context

- Problem statement: the block crosses from an untrusted page into the app. It must carry text, a yes or no Mall value and lists. It must also allow five malformed kinds to be refused (C-014).
- Business and technical constraints: C-012, C-013, C-014, C-015.
- Current architecture baseline: F-004 (1 MiB body cap), F-009 (shared parser labels), F-010 (page rule).
- Choice in plain words: the button copies a short readable block that begins with a fixed name. The app checks every part and refuses the whole block if anything is wrong.

## Decision

- Selected option: O-004.
- Decision statement: a fixed ASCII marker, then one strict JSON object as the design's D-002 row describes, bounded by D-014 and versioned by D-006.
- Scope of impact: M-001, M-004.

## Alternatives Considered

1. O-005: MC-18 labelled text with a header line
- Benefits: reuses the shared parser.
- Risks: no types or lists.
- Why not selected: eliminated by C-014.

2. O-006: the same object, compactly encoded
- Benefits: an opaque look.
- Risks: unreadable to the operator; larger.
- Why not selected: meets every hard constraint and loses at operability.

## Consequences

- Positive outcomes expected: every SC-24 kind is refusable; types are enforced.
- Tradeoffs accepted: a second format exists beside pasted page text.
- Risks introduced: R-014, R-019.

## Validation Plan

- Metrics to monitor: refusal codes per malformed kind; parse time on the 255 KiB block.
- Verification checkpoints: P-003, P-008 (SC-20, SC-22 to SC-24).
- Rollback or reversal conditions: revise if a valid fixture block is refused, or a malformed one is accepted.

## Approval

- Architect: unsigned (producer; excluded from accepting)
- Tech Lead: unsigned
- Product Owner (if scope-impacting): unsigned
