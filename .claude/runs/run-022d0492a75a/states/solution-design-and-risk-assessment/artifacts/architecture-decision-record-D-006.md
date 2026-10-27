# Architecture Decision Record

## Metadata

- ADR ID: D-006
- Title: How the recording flow is tested with no microphone and no download
- Date: 2026-10-27
- Status: Proposed
- Owners: tech-lead (accepting Design Gate owner under the producer exclusion rule); the CEO's reading of SC-23 is recorded at the Design Gate through Q-002
- Related Work Items: MC-17, run run-022d0492a75a, technical design TD-MC17-PILOT-WEB, planner tasks T-002, T-019, T-024

## Context

- Problem statement: the record, stop at 60 s, playback and re-record flow must pass on a computer with no microphone, offline, with fixtures made by code (C-017), adding zero package references (C-013).
- Business and technical constraints: C-011, C-013, C-017, C-028.
- Current architecture baseline: the installed, manifest-checked headless browser shell supports fake capture (F-025); the toolchain tests use test packages at central versions (F-018); the socket harness records owners, not addresses (F-017).
- Choice for the CEO, in plain words:
  - Option A (proposed, O-018): the tests use the browser already installed and approved (CEO-D-1001), fed a sound file made by code instead of a microphone. The tests are a small program that needs no extra package, so the change adds no package at all.
  - Option B (O-019): the same browser test, but written with the test library the MC-16 tests already use. Nothing is downloaded, as it is already on this computer, but the new test project lists it as a package, which a strict reading of SC-23 forbids.

## Decision

- Selected option: O-018.
- Decision statement: M-003 is a zero-package test program; it starts M-001 with a test-only route, and the installed headless shell after checking its digest against its manifest, with fake capture from a code-generated audio file with set pauses and non-loopback name resolution blocked; a same-origin test page drives the real recording page and posts results to the test-only route; the toolchain's harness source is compiled in unchanged, plus a variant that keeps addresses for SC-21.
- Scope of impact: M-003, M-007 (no change), M-011, M-015.

## Alternatives Considered

1. O-019: test project using the installed test packages
- Benefits: the repository's usual test runner.
- Risks: package references in a new project file.
- Why not selected: eliminated by C-013 under A-008; preferred if the CEO reads SC-23 as "nothing downloaded".

2. O-020: browser driven through its remote debugging protocol
- Benefits: direct control of the browser.
- Risks: a debugging listener in the browser and a protocol client to build.
- Why not selected: lower reuse leverage.

3. O-021: downloaded browser-automation package
- Benefits: a ready automation library.
- Risks: a download.
- Why not selected: eliminated by C-013.

4. O-022: manual recording evidence
- Benefits: no automation.
- Risks: needs a microphone and a person.
- Why not selected: eliminated by C-017.

## Consequences

- Positive outcomes expected: SC-08, SC-09 and SC-28 are tested offline; SC-23 holds under its strict reading.
- Tradeoffs accepted: a test program outside the repository's usual test framework; the browser's own loopback connection to the app is expected in the harness (A-011).
- Risks introduced: R-017, R-018, R-023.

## Validation Plan

- Metrics to monitor: harness rows by owner and address during the test; take lengths; digest of each take.
- Verification checkpoints: P-013, P-015.
- Rollback or reversal conditions: move to O-019 if the CEO answers Q-002 "no"; revisit if fake capture cannot drive the page (A-007 false).

## Approval

- Architect: unsigned (producer; excluded from accepting)
- Tech Lead: unsigned
- Product Owner (if scope-impacting): unsigned; the CEO answers Q-002
