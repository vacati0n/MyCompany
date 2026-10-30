# Architecture Decision Record

## Metadata

- ADR ID: D-003
- Title: Which requests the loopback web app accepts
- Date: 2026-10-27
- Status: Proposed
- Owners: tech-lead (accepting Design Gate owner under the producer exclusion rule); the CEO decides at the Design Gate
- Related Work Items: MC-17, run run-022d0492a75a, technical design TD-MC17-PILOT-WEB, planner tasks T-017, T-019, T-020

## Context

- Problem statement: any web page open in the same browser can send requests to 127.0.0.1, including a page under a foreign host name that resolves to 127.0.0.1. Produce, timing save and approval must be accepted only from the app's own page (C-014), and the app must listen on 127.0.0.1 only (C-011).
- Business and technical constraints: C-011, C-014, C-030.
- Current architecture baseline: the shared framework provides the web server and host filtering (F-022, A-001); the toolchain approves only against the video's recomputed hash (F-014).
- Choice for the CEO, in plain words:
  - Option A (proposed, O-009): the app listens only on this computer's own address and checks three things on every request that changes something: the address typed in the browser is the app's own, the request comes from the app's own page, and it carries a secret number the app creates fresh each time it starts. Any one failing refuses the request.
  - Option B (O-010): the app checks only that the request comes from its own page. Simpler, but one check decides alone.

## Decision

- Selected option: O-009.
- Decision statement: the listener binds 127.0.0.1 only and the app stops at start-up if any other address is bound; every request passes a host allow-list naming 127.0.0.1 and the app's port; each of the seventeen state-changing routes in the design's section 6 also needs an Origin equal to the app's own and the per-start token in a custom header; no cross-origin policy is offered; pages carry an own-origin content policy, no framing and microphone permission for the app's own origin only; read routes change no state.
- Scope of impact: M-001, M-002.

## Alternatives Considered

1. O-010: Origin check only
- Benefits: one check; no token to embed.
- Risks: one browser-supplied header decides alone (C-030).
- Why not selected: lower reuse leverage (no host filtering) and fails C-030.

2. O-011: token only
- Benefits: no header comparison.
- Risks: a page under a foreign host name resolving to 127.0.0.1 can read the page and its token.
- Why not selected: eliminated by C-014.

## Consequences

- Positive outcomes expected: the six SC-24 requests are refused and the pilot folder is unchanged; nothing outside the computer can reach the app (SC-21).
- Tradeoffs accepted: a program already running on the computer can still read the token (residual under X-005); the app must be reloaded after a restart to get the new token.
- Risks introduced: R-006, R-007, R-010, R-012.

## Validation Plan

- Metrics to monitor: listener addresses at start-up; refusals by reason in the log.
- Verification checkpoints: P-002, P-013 (six SC-24 requests; harness with addresses).
- Rollback or reversal conditions: if 127.0.0.1 is not a secure context for microphone capture (A-005 false), the allow-list is revisited with the CEO while the listener stays on 127.0.0.1.

## Approval

- Architect: unsigned (producer; excluded from accepting)
- Tech Lead: unsigned
- Product Owner (if scope-impacting): unsigned
