# Architecture Decision Record

## Metadata

- ADR ID: D-004
- Title: Test seam through the internal start path
- Date: 2026-10-30
- Status: Proposed
- Owners: tech-lead (accepting Design Gate owner under the producer exclusion rule)
- Related Work Items: MC-19, run run-142c58d73427, technical design TD-MC19-PAGE-BUTTON, planner tasks T-021, T-024, T-026

## Context

- Problem statement: the button acts only on shopee.vn pages, and the app accepts only shopee.vn page addresses (C-033). The tests use fixture pages on 127.0.0.1 (C-022), and the shipped app must have no bypass (C-026).
- Business and technical constraints: C-022, C-024, C-026, C-033.
- Current architecture baseline: F-017 (internal seam), F-018 (fixture-host mode), F-019 (headless shell).
- Choice in plain words: only the test program can tell the app "this test page counts as Shopee". Your app has no switch for that.

## Decision

- Selected option: O-010.
- Decision statement: the internal seam carries one exact fixture origin. The builder adds it to the button's host list, and the page-address rule accepts it. The shipped entry point passes none.
- Scope of impact: M-002, M-008, M-013.

## Alternatives Considered

1. O-011: map shopee.vn to the fixture in the test browser
- Benefits: no app seam.
- Risks: the Browser pane cannot do it.
- Why not selected: eliminated by C-024.

2. O-012: a command-line switch or setting
- Benefits: simple.
- Risks: reachable by the operator.
- Why not selected: eliminated by C-026.

3. O-013: no host check in the button
- Benefits: no allowance needed for the button.
- Risks: the button copies from any site.
- Why not selected: eliminated by C-033.

## Consequences

- Positive outcomes expected: the same script is tested; the shipped app refuses a 127.0.0.1 page address.
- Tradeoffs accepted: the generated address differs between the test and shipped builds only in the host list and the mark.
- Risks introduced: R-016.

## Validation Plan

- Metrics to monitor: argument forms refused; seam type visibility.
- Verification checkpoints: P-002, P-008 (SC-24 on the shipped executable; SC-31; SC-41; inspection).
- Rollback or reversal conditions: withdraw if any argument, variable, file or route of the shipped app reaches the allowance.

## Approval

- Architect: unsigned (producer; excluded from accepting)
- Tech Lead: unsigned
- Product Owner (if scope-impacting): unsigned
