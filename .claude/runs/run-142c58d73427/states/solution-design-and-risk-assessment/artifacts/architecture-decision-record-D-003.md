# Architecture Decision Record

## Metadata

- ADR ID: D-003
- Title: Field finding by visible-text anchors
- Date: 2026-10-30
- Status: Proposed
- Owners: tech-lead (accepting Design Gate owner under the producer exclusion rule); the CEO answers Q-002
- Related Work Items: MC-19, run run-142c58d73427, technical design TD-MC19-PAGE-BUTTON, planner tasks T-003, T-024

## Context

- Problem statement: no agent may open a real page (C-023). The button must find fields on a page known only from the CEO's screenshot (F-024), the MC-18 labels (F-009) and the ticket. It must never guess (C-001, C-032).
- Business and technical constraints: C-001, C-005, C-012, C-030, C-032.
- Current architecture baseline: F-009, F-023, F-024.
- Choice in plain words: the button looks for the words you see, such as "CHI TIẾT SẢN PHẨM", "MÔ TẢ SẢN PHẨM", "Phân loại" and "Xem Shop". If it cannot find a field, it leaves that field empty for you.

## Decision

- Selected option: O-007.
- Decision statement: the anchors and rules of the design's D-003 and D-011 rows. Missed fields are absent. No cookie, storage or account area is read.
- Scope of impact: M-002, M-013.

## Alternatives Considered

1. O-008: generated class names and element paths
- Benefits: precise on one page version.
- Risks: breaks on restyling; nothing grounds it, since no agent may view the page.
- Why not selected: meets every hard constraint and loses at reuse leverage (0 to 1).

2. O-009: hidden structured data first
- Benefits: possibly more fields.
- Risks: values that are not shown.
- Why not selected: eliminated by C-032.

## Consequences

- Positive outcomes expected: findings trace to visible text; they hold through restyling.
- Tradeoffs accepted: a real page with other wording fills fewer fields.
- Risks introduced: R-004, R-005, R-006, R-013, R-028.

## Validation Plan

- Metrics to monitor: fields filled of the ten kinds on the CEO's try.
- Verification checkpoints: P-004, P-008 (SC-01, SC-02, SC-04, SC-06, SC-16 to SC-18), P-009 (SC-42).
- Rollback or reversal conditions: revise the anchors in a later change if the CEO's try fills fewer than the kinds the screenshot shows.

## Approval

- Architect: unsigned (producer; excluded from accepting)
- Tech Lead: unsigned
- Product Owner (if scope-impacting): unsigned
