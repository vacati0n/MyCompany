# Architecture Decision Record

## Metadata

- ADR ID: D-001
- Title: Where the MC-16 toolchain's code runs when the web app uses it
- Date: 2026-10-27
- Status: Proposed
- Owners: tech-lead (accepting Design Gate owner under the producer exclusion rule); the CEO's choice is recorded at the Design Gate through Q-001
- Related Work Items: MC-17, run run-022d0492a75a, technical design TD-MC17-PILOT-WEB, planner tasks T-017, T-018, T-020

## Context

- Problem statement: the web app must answer the banned-word check as the operator types, using the toolchain's own list (C-002), while every toolchain process keeps zero sockets (C-012) and the toolchain keeps deciding (C-015). The web app itself holds the one loopback listener (C-011).
- Business and technical constraints: C-002, C-011, C-012, C-015, C-027, C-028.
- Current architecture baseline: the toolchain's command line stops at the first error and has no class list for live use (F-001, F-008); its word check is a public, pure library call (F-007); its socket telemetry is process-wide and is created only by produce, failing the run on any socket event (F-006); its network guard checks only the closure below the toolchain assembly (F-005).
- Choice for the CEO, in plain words:
  - Option A (proposed, O-003): the web app uses the toolchain's own word list and checks directly to answer while you type, and converts recordings with the toolchain's own ffmpeg runner. Every decision that matters (check, script, produce, approve, verify) is still made by the toolchain program, started as a separate program that has no network connection at all.
  - Option B (O-004): the same, but even the as-you-type checks and the conversion run in a second small program with no network connection, so no MC-16 code ever runs inside the program that holds the 127.0.0.1 connection. Costs one more program to build and keep running.

## Decision

- Selected option: O-003.
- Decision statement: M-001 calls the toolchain library in process only for pure checks (word check, first-person check, fact sheet check, plain-name rule, NFC, timing file check, clip check), settings reading and validation, probe and conversion through MediaTool. It starts the toolchain executable as a child for check, script, produce, approve and verify, with an argument list, no shell, UTF-8 output and a kill-on-close job. M-001 never calls Producer or the toolchain's entry point.
- Scope of impact: M-001, M-004, M-005, M-006 (no change), M-016.

## Alternatives Considered

1. O-004: resident socket-free helper process for every library call
- Benefits: no MC-16 code runs in the process that owns the listener; meets the strictest reading of CEO-D-1310 item 1.
- Risks: a second resident process to supervise; an internal message form that must follow the library; one more failure point for the live check (A-010).
- Why not selected: ties O-003 on hard constraints, impact surface, reuse, quality attributes and migration, and loses on operability. Selected instead if the CEO chooses Option B (A-002 false).

2. O-001: all toolchain work in the web process, produce included
- Benefits: one process; no child start cost.
- Risks: produce's telemetry would count the listener's events and hold every package (F-006).
- Why not selected: eliminated by C-012.

3. O-002: toolchain reached only through its command line
- Benefits: no MC-16 code in the web process.
- Risks: no live class-by-class answer; a new verb would change the toolchain; a page copy of the list would drift.
- Why not selected: eliminated by C-002.

## Consequences

- Positive outcomes expected: the live check equals the toolchain's own result (SC-05); produce, approve and verify run exactly as on the command line, with their own guard and telemetry (SC-22, SC-25); no toolchain file changes (SC-26).
- Tradeoffs accepted: library code for pure checks, probe and conversion runs inside the listener's process; the recording reuse refusal appears at produce, because only the child opens the ledger.
- Risks introduced: R-001, R-003, R-008, R-013.

## Validation Plan

- Metrics to monitor: socket rows by owner during the sample run (app: listener and loopback only; every other job member: zero); live check latency against C-027.
- Verification checkpoints: P-001 (project boundary), P-004 (child path), P-013 (harness), P-015 (parity fixtures and an M-003 check that M-001 never references Producer or the entry point).
- Rollback or reversal conditions: reverse to O-004 if the CEO chooses Option B, if a socket row appears for any child, or if a toolchain library call used in process starts socket telemetry.

## Approval

- Architect: unsigned (producer; excluded from accepting)
- Tech Lead: unsigned
- Product Owner (if scope-impacting): unsigned; the CEO answers Q-001
