# Architecture Decision Record

## Metadata

- ADR ID: D-003
- Title: Where the read's off switch lives and how it starts
- Date: 2026-10-27
- Status: Proposed
- Owners: tech-lead (accepting Design Gate owner under the producer exclusion rule); the CEO decides at the Design Gate
- Related Work Items: MC-18, run run-6fa26c9657d3, technical design TD-MC18-READ-LINK, planner tasks T-004, T-019; open question Q-002

## Context

- Problem statement: CEO decision 1313 requires one switch that turns the read off and leaves paste mode working; SC-28 to SC-30 require it on a settings screen, kept across restarts (C-016).
- Business and technical constraints: C-016, C-020.
- Current architecture baseline: the app record is form version 1 and treats another version as absent (F-012); cai-dat.json is validated by the toolchain, which refuses unknown members (F-013); the only settings view is the setup view, shown only before setup (F-014).
- Choice for the CEO, in plain words:
  - Option A (proposed, O-009): a new "Cài đặt" screen, reached from the top bar, holds one on/off switch saved in its own small file. If that file is missing or damaged the read counts as off. Proposed default: off until you turn it on.
  - Option B (O-010): store the switch in the app's existing record. That record would need a new version, and your current setup would be forgotten once, so you would run setup again.
  - Option C is not offered: the toolchain's settings file refuses new entries and the toolchain must not change.
  - Separate question Q-002: should the switch start off (recommended) or on?

## Decision

- Selected option: O-009.
- Decision statement: one on/off value in its own file in the app folder at form version 1, set on a new settings view; missing, unreadable or another version reads as off; default off until the operator turns it on; off removes the read button, refuses the read route and leaves paste and typing working.
- Scope of impact: M-006, M-007, M-008.

## Alternatives Considered

1. O-010: a new member of the app record at form version 2
- Benefits: one file for app state.
- Risks: an existing record reads as absent and forces setup again.
- Why not selected: loses at impact surface, 1 module (M-012) against 0.

2. O-011: a new member of cai-dat.json
- Benefits: the operator's settings in one file.
- Risks: the toolchain reader refuses the file.
- Why not selected: eliminated by C-020.

## Consequences

- Positive outcomes expected: SC-28 to SC-30 hold; a damaged file fails safe; no re-setup.
- Tradeoffs accepted: one more small file in the app folder; nothing switches off by itself on a Shopee warning (X-007).
- Risks introduced: R-021.

## Validation Plan

- Metrics to monitor: outbound connections with the switch off (zero); saves accepted with the switch off.
- Verification checkpoints: P-007 (switch store and view), P-009 (SC-28, SC-29, SC-30 browser and harness runs).
- Rollback or reversal conditions: change only the default if the CEO answers Q-002 with "on"; move to O-010 only if a later change bumps the app record version anyway.

## Approval

- Architect: unsigned (producer; excluded from accepting)
- Tech Lead: unsigned
- Product Owner (if scope-impacting): unsigned; the CEO answers Q-002
