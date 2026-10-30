# Architecture Decision Record

## Metadata

- ADR ID: D-006
- Title: Button version and mark
- Date: 2026-10-30
- Status: Proposed
- Owners: tech-lead (accepting Design Gate owner under the producer exclusion rule)
- Related Work Items: MC-19, run run-142c58d73427, technical design TD-MC19-PAGE-BUTTON, planner tasks T-014, T-020, T-021

## Context

- Problem statement: the button code lives in the bookmark and loads nothing (C-011). A block from an out-of-date button must be refused (C-020). The address must stay small (C-031).
- Business and technical constraints: C-011, C-020, C-031.
- Current architecture baseline: F-001 (no package), F-014 ("Cài đặt" view).
- Choice in plain words: each button carries a short fingerprint of its own code. If the app's button has changed, the app refuses blocks from the old one and asks you to drag the new one.

## Decision

- Selected option: O-016.
- Decision statement: the format version and the button mark are embedded by the builder. Only the current pair is accepted.
- Scope of impact: M-002, M-004, M-006.

## Alternatives Considered

1. O-017: format version only
- Benefits: simpler.
- Risks: a code change without a raised version goes undetected.
- Why not selected: meets every hard constraint and loses at reuse leverage (0 to 1).

2. O-018: code loaded from the app at each press
- Benefits: always current.
- Risks: code loaded at a press; a page-to-app call.
- Why not selected: eliminated by C-020.

## Consequences

- Positive outcomes expected: any script change retires old buttons.
- Tradeoffs accepted: the operator drags again after each button change.
- Risks introduced: R-022.

## Validation Plan

- Metrics to monitor: button address size (at most 32 KiB); refusals of old marks.
- Verification checkpoints: P-003, P-004, P-008 (SC-31, SC-33).
- Rollback or reversal conditions: move to O-017 if the mark cannot be embedded within C-031.

## Approval

- Architect: unsigned (producer; excluded from accepting)
- Tech Lead: unsigned
- Product Owner (if scope-impacting): unsigned
