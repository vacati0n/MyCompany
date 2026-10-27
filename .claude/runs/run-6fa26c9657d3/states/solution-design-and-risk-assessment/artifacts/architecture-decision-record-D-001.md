# Architecture Decision Record

## Metadata

- ADR ID: D-001
- Title: Where the product-page read runs
- Date: 2026-10-27
- Status: Proposed
- Owners: tech-lead (accepting Design Gate owner under the producer exclusion rule); the CEO decides at the Design Gate
- Related Work Items: MC-18, run run-6fa26c9657d3, technical design TD-MC18-READ-LINK, planner tasks T-001, T-004, T-012, T-015

## Context

- Problem statement: the read is the first piece of the pilot that connects outside the computer. It must start only when the operator clicks, reach only allowed Shopee hosts, carry no cookie and imitate no browser, while the MC-16 toolchain stays unchanged with zero sockets (C-009 to C-015, C-020, C-023).
- Business and technical constraints: C-009, C-010, C-012, C-015, C-017, C-020, C-023, C-025, C-026.
- Current architecture baseline: the web app is one executable on 127.0.0.1 behind a request guard (F-002, F-003); every route runs through one answer wrapper (F-004); the toolchain's network guard covers only the toolchain's own code (F-022); no package is referenced (F-001).
- Choice for the CEO, in plain words:
  - Option A (proposed, O-001): the read is a new part of the web app you already run. It only starts when you press the button on step 1. It uses its own fresh connection for each click and closes it when the read ends. Nothing is added to the video toolchain.
  - Option B (O-005): a small separate program does the reading, started by the web app for each click. Same safety, but one more program to build, find and check on your computer.
  - Options C, D and E break your rules and are not offered: reading from your browser would send your own Shopee cookies; adding the read to the toolchain changes it and gives it network access; driving a hidden browser imitates a person.

## Decision

- Selected option: O-001.
- Decision statement: the read runs inside the web app process as new components (M-001 to M-006) reached only through new guarded routes (M-007); one read at a time app-wide; a fresh HTTP stack per click with cookies, proxy, automatic redirects and decompression off, disposed when the read ends; the toolchain is not changed.
- Scope of impact: M-001, M-002, M-003, M-004, M-005, M-006, M-007, M-008, M-016.

## Alternatives Considered

1. O-002: the page's own script fetches Shopee from the operator's browser
- Benefits: no server-side HTTP code.
- Risks: the browser sends the operator's own Shopee cookies and browser headers; the content policy must be opened.
- Why not selected: eliminated by C-010.

2. O-003: a new read verb in the MC-16 toolchain
- Benefits: one place for all video-input logic.
- Risks: the toolchain changes and gains sockets.
- Why not selected: eliminated by C-020.

3. O-004: drive the installed headless browser to render the page
- Benefits: script-rendered pages would show content.
- Risks: an automated browser presents itself as a person's browser and fetches scripts, styles and fonts.
- Why not selected: eliminated by C-012.

4. O-005: a separate read helper executable started per click
- Benefits: the network code lives in its own process, apart from the listener.
- Risks: a second executable to build, locate and digest-check; a new web-to-helper link.
- Why not selected: meets every hard constraint but loses at impact surface, 3 modules against 2.

## Consequences

- Positive outcomes expected: every read passes the MC-17 guard and answer wrapper; the toolchain keeps zero sockets (SC-26); one executable to set up.
- Tradeoffs accepted: outbound HTTPS code shares the process that owns the loopback listener; the listener itself is unchanged.
- Risks introduced: R-001, R-009, R-014, R-015.

## Validation Plan

- Metrics to monitor: outbound connections per click against the read log; toolchain job processes' socket count (zero); listener count on 127.0.0.1 (one).
- Verification checkpoints: P-003 (sender against the fixture), P-008 (guarded read route), P-009 (SC-18, SC-25, SC-26, SC-27 harness runs).
- Rollback or reversal conditions: move to O-005 if the harness shows the web process opening a connection outside a read, or if the outbound stack cannot be kept from affecting the listener.

## Approval

- Architect: unsigned (producer; excluded from accepting)
- Tech Lead: unsigned
- Product Owner (if scope-impacting): unsigned
