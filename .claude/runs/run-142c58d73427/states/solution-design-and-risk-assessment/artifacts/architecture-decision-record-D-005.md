# Architecture Decision Record

## Metadata

- ADR ID: D-005
- Title: When images are fetched
- Date: 2026-10-30
- Status: Proposed
- Owners: tech-lead (accepting Design Gate owner under the producer exclusion rule); the CEO answers Q-001
- Related Work Items: MC-19, run run-142c58d73427, technical design TD-MC19-PAGE-BUTTON, planner tasks T-011, T-012

## Context

- Problem statement: the operator picks up to three images (C-003). The app page cannot show Shopee images itself, because the content policy allows only its own origin (F-003). Egress is limited to carried image addresses (C-010).
- Business and technical constraints: C-003, C-010, C-017.
- Current architecture baseline: F-003, F-010, F-011, F-012, F-013.
- Choice in plain words: either the app fetches the product's pictures right after your paste so you pick from thumbnails (up to 20 small requests), or it fetches only the three you pick, chosen by number.

## Decision

- Selected option: O-014.
- Decision statement: after acceptance, with the switch on, the image phase fetches the allowed carried addresses (up to 20) under D-016. Only picked images are placed on save.
- Scope of impact: M-004, M-005, M-006.

## Alternatives Considered

1. O-015: fetch only the picked images
- Benefits: 3 requests instead of up to 20; matches the ticket's step 4 wording.
- Risks: picking by number without pictures.
- Why not selected: meets every hard constraint and loses at reuse leverage (1 to 3).

## Consequences

- Positive outcomes expected: the MC-18 picker and the MC-18 rules are used unchanged.
- Tradeoffs accepted: more image requests per product.
- Risks introduced: R-010, R-011, R-017, R-024.

## Validation Plan

- Metrics to monitor: image requests per paste; requests to shopee.vn pages (zero).
- Verification checkpoints: P-006, P-008 (SC-14, SC-15, SC-21).
- Rollback or reversal conditions: move to O-015 if the CEO chooses it at Q-001.

## Approval

- Architect: unsigned (producer; excluded from accepting)
- Tech Lead: unsigned
- Product Owner (if scope-impacting): unsigned
