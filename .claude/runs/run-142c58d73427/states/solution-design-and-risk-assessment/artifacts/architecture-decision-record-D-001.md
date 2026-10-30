# Architecture Decision Record

## Metadata

- ADR ID: D-001
- Title: Hand-over entry through the guarded paste route
- Date: 2026-10-30
- Status: Proposed
- Owners: tech-lead (accepting Design Gate owner under the producer exclusion rule); the CEO answers Q-001 to Q-003
- Related Work Items: MC-19, run run-142c58d73427, technical design TD-MC19-PAGE-BUTTON, planner tasks T-004, T-016, T-018

## Context

- Problem statement: the CEO chose the clipboard (S-013). The block must enter step 1 through the unchanged guard. The switch must refuse it, and MC-18 paste must stay as delivered (C-016, C-017, C-021).
- Business and technical constraints: C-004, C-006, C-016, C-017, C-021.
- Current architecture baseline: F-003 (guard), F-004 (paste route works with the switch off), F-005 (switch refusal on read), F-006 (video code in the query).
- Choice in plain words: you press the button on Shopee, then press Ctrl+V in the app. The app takes it through the same safe door as pasted text, and the off switch closes that door for button text only.

## Decision

- Selected option: O-001.
- Decision statement: the paste route recognises the block marker and takes the block path with the D-010 checks. Other text keeps the MC-18 path. The page posts a marked paste immediately (D-015).
- Scope of impact: M-004, M-006, M-009.

## Alternatives Considered

1. O-002: a new guarded block route
- Benefits: the paste route stays untouched in code.
- Risks: the paste route still needs the marker refusal, so the rule is checked in two places.
- Why not selected: meets every hard constraint and loses at reuse leverage (3 to 4).

2. O-003: the shopee.vn page posts straight to 127.0.0.1
- Benefits: one action.
- Risks: foreign origin; guard widened.
- Why not selected: eliminated by C-016.

## Consequences

- Positive outcomes expected: one guarded entry, one switch refusal point, and two actions in total.
- Tradeoffs accepted: the paste route's contract gains a second path.
- Risks introduced: R-009, R-021, R-023.

## Validation Plan

- Metrics to monitor: actions per product (two); refusals with the switch off (all blocks); MC-18 web tests (53 passing).
- Verification checkpoints: P-003, P-005, P-008 (SC-05, SC-07, SC-26 to SC-30, SC-35).
- Rollback or reversal conditions: move to O-002 if marker selection changes any MC-18 paste outcome.

## Approval

- Architect: unsigned (producer; excluded from accepting)
- Tech Lead: unsigned
- Product Owner (if scope-impacting): unsigned
