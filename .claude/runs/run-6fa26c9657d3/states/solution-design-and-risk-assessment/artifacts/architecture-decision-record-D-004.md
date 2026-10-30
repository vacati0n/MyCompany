# Architecture Decision Record

## Metadata

- ADR ID: D-004
- Title: How tests point the read at the local fixture without the shipped app being able to
- Date: 2026-10-27
- Status: Proposed
- Owners: tech-lead (accepting Design Gate owner under the producer exclusion rule); the CEO decides at the Design Gate
- Related Work Items: MC-18, run run-6fa26c9657d3, technical design TD-MC18-READ-LINK, planner tasks T-015, T-017, T-021; open question Q-007

## Context

- Problem statement: tests must run offline against a code-generated fixture on 127.0.0.1 (C-017), yet the shipped read must reach only Shopee hosts and the shipped connect step refuses loopback addresses (D-006). Something must redirect test reads to the fixture, and nothing in the shipped app may reach it (C-022).
- Business and technical constraints: C-014, C-017, C-019, C-022.
- Current architecture baseline: the shipped executable accepts --test-mode and other switches (F-016); the test program already references the web project (F-020) and starts the app as a separate process (F-018); the project declares no internals visibility (F-001).
- Choice for the CEO, in plain words:
  - Option A (proposed, O-012): a hidden hook inside the code that only the test program can use. The app you run has no switch, setting or file that can turn it on. For the step-1 demonstration in the Browser pane, the test program starts its own copy of the app wired to the fixture.
  - Options B and C are not offered: a command-line switch or a "test" flag that loosens the rules would be reachable from the app you run, which your rules forbid.
  - There is nothing for you to choose here; the record exists so the reviewer can check it.

## Decision

- Selected option: O-012.
- Decision statement: an internal transport seam (connect-target resolver and fixture certificate check) given to the read sender only through an internal start path visible to the test assembly; the shipped entry point never passes it; no argument, environment variable, file or route can set it; the host and hop rule is checked on the real link names either way; the test program offers a fixture host mode for app, browser and Browser-pane runs.
- Scope of impact: M-002, M-010, M-011, M-017.

## Alternatives Considered

1. O-013: a command-line switch or environment variable pointing reads at the fixture
- Benefits: the existing separate-process test pattern unchanged.
- Risks: reachable from the shipped app, as --test-mode is.
- Why not selected: eliminated by C-022.

2. O-014: a plain-HTTP fixture with the host rule relaxed under a test flag
- Benefits: no certificate work.
- Risks: shipped code holds a weaker rule.
- Why not selected: eliminated by C-022.

## Consequences

- Positive outcomes expected: offline tests exercise the real host rule and header list; the shipped app has no path to the fixture.
- Tradeoffs accepted: app-level and Browser-pane fixture runs use the test program's host, not the shipped executable (A-017).
- Risks introduced: R-002, R-007, R-026.

## Validation Plan

- Metrics to monitor: connections off 127.0.0.1 during the test run (zero, SC-31).
- Verification checkpoints: P-002 (seam and start path proven first), P-009 (inspection that the shipped entry point never passes the seam; SC-31, SC-34).
- Rollback or reversal conditions: if the seam-composed app cannot be started by the test program (A-016 false), the design returns to the architect; O-013 and O-014 stay excluded.

## Approval

- Architect: unsigned (producer; excluded from accepting)
- Tech Lead: unsigned
- Product Owner (if scope-impacting): unsigned
