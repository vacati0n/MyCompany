# Architecture Decision Record

## Metadata

- ADR ID: D-002
- Title: How the read identifies itself to Shopee
- Date: 2026-10-27
- Status: Proposed
- Owners: tech-lead (accepting Design Gate owner under the producer exclusion rule); the CEO decides at the Design Gate
- Related Work Items: MC-18, run run-6fa26c9657d3, technical design TD-MC18-READ-LINK, planner tasks T-012, T-015; open question Q-001

## Context

- Problem statement: SC-22 asks that every request header be on a list accepted at the Design Gate, with the user agent set to the reading component's own value, and CEO decision 1313 forbids forged headers, a faked user agent and anything that imitates a browser (C-012, C-030).
- Business and technical constraints: C-010, C-012, C-013, C-030.
- Current architecture baseline: the web app has no outbound code and no package, so the read uses the platform HTTP client (F-001); the MC-17 tests already configure that client with proxy and automatic redirects off (F-018); the toolchain's network guard covers only the toolchain's own code and is unaffected by the web app's requests (F-022); with cookies, proxy, redirects and decompression off the client sends only Host and the headers the code sets (A-007).
- Choice for the CEO, in plain words:
  - Option A (proposed, O-006): every request says plainly that it comes from your tool, "MediaCompanyPilot/1.0 (doc-trang-san-pham)". Shopee can see it is a tool and may refuse it; a refusal stops the read and paste mode takes over.
  - Option B (O-007): send no name at all. Still honest about nothing, but Shopee cannot tell who is asking.
  - Option C is not offered: pretending to be a browser breaks your rule.
  - The rest of the list is fixed either way: Accept "text/html" for the page, "image/jpeg, image/png" for images, Host set by the system, and nothing else.

## Decision

- Selected option: O-006.
- Decision statement: in the read sender M-002, user agent "MediaCompanyPilot/1.0 (doc-trang-san-pham)"; Accept "text/html" for the page and "image/jpeg, image/png" for images; Host set by the stack; no Cookie, Authorization, Referer, Origin, client-hint, fetch-metadata, X- or signature header; cookies, proxy, automatic redirects and decompression off.
- Scope of impact: M-002 (dependency-change); M-017 records the header log the fixture checks.

## Alternatives Considered

1. O-007: no user agent header
- Benefits: the shortest possible header list.
- Risks: the site cannot tell an automated reader from anything else.
- Why not selected: meets every hard constraint but loses at quality attributes, 0 of C-030 against 1.

2. O-008: a browser's user agent and browser-like headers
- Benefits: possibly more pages answered.
- Risks: imitation of a browser, against CEO decision 1313.
- Why not selected: eliminated by C-012.

## Consequences

- Positive outcomes expected: SC-20, SC-22 and SC-23 hold by construction; the tool is identifiable, which supports the CEO's terms position (S-019).
- Tradeoffs accepted: an honest name makes a refusal more likely; paste mode is the expected common path (technical design 5.3).
- Risks introduced: R-008, R-015.

## Validation Plan

- Metrics to monitor: the fixture's header log for every request, compared with the accepted list.
- Verification checkpoints: P-003 (sender), P-009 (SC-20, SC-22, SC-23 cases).
- Rollback or reversal conditions: move to O-007 if the CEO answers Q-001 with "no name"; any header outside the list in the fixture log is a defect, never a reason to widen the list without a Design Gate decision.

## Approval

- Architect: unsigned (producer; excluded from accepting)
- Tech Lead: unsigned
- Product Owner (if scope-impacting): unsigned; the CEO answers Q-001
