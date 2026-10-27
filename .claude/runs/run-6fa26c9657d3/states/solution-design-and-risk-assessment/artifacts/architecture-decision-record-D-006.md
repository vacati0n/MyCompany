# Architecture Decision Record

## Metadata

- ADR ID: D-006
- Title: How every link and redirect hop is checked before connecting
- Date: 2026-10-27
- Status: Proposed
- Owners: tech-lead (accepting Design Gate owner under the producer exclusion rule); the CEO decides at the Design Gate
- Related Work Items: MC-18, run run-6fa26c9657d3, technical design TD-MC18-READ-LINK, planner tasks T-015, T-017

## Context

- Problem statement: CEO decision 1314 item 2 allows short-link redirects only while every hop is an allowed host, at most five, never affiliate.shopee.vn; SC-24 lists six link kinds that must open zero connections (C-002, C-011, C-014).
- Business and technical constraints: C-002, C-011, C-013, C-014, C-015.
- Current architecture baseline: the web app has no outbound code (F-001, F-022); the platform HTTP client follows redirects itself unless told not to.
- Choice for the CEO, in plain words:
  - Option A (proposed, O-018): the tool checks every address, the one you paste and every address Shopee redirects to, before it connects. Only https, only shopee.vn and its subdomains, never affiliate.shopee.vn, at most five steps; anything else stops the read.
  - Option B is not offered: letting the system follow redirects on its own would connect to an address before it is checked.
  - There is nothing for you to choose here; the record exists so the reviewer can check it.

## Decision

- Selected option: O-018.
- Decision statement: automatic redirects off; https only (a link without scheme or with http is rewritten to https before any connection; an http hop stops); no user info, explicit port or IP literal; host compared in lower-case ASCII form without a trailing dot; page hosts are shopee.vn and names ending .shopee.vn except affiliate.shopee.vn and names under it; checked on the link and every hop before connecting; at most five hops; a hop to a sign-in path stops; the connect step refuses loopback, private, link-local, unspecified and multicast addresses.
- Scope of impact: M-001, M-002.

## Alternatives Considered

1. O-019: the stack follows redirects itself and the final address is checked
- Benefits: less code.
- Risks: a connection opens to a hop before it is checked.
- Why not selected: eliminated by C-014.

## Consequences

- Positive outcomes expected: SC-02, SC-21 and SC-24 hold with zero connections to refused names; a Shopee name resolving to a local address is a stop.
- Tradeoffs accepted: short links that need page script stop and paste follows (A-005).
- Risks introduced: R-005, R-006, R-019.

## Validation Plan

- Metrics to monitor: connections per refused link kind (zero); hops per read (at most five).
- Verification checkpoints: P-002 (rule and connect check proven first), P-009 (SC-02, SC-21, SC-24 cases).
- Rollback or reversal conditions: none for the rule itself, which the hard constraints fix; the host list changes only by a CEO decision.

## Approval

- Architect: unsigned (producer; excluded from accepting)
- Tech Lead: unsigned
- Product Owner (if scope-impacting): unsigned
