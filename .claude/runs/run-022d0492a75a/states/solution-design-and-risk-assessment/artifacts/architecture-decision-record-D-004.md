# Architecture Decision Record

## Metadata

- ADR ID: D-004
- Title: How the first-run setup screen writes the toolchain's settings
- Date: 2026-10-27
- Status: Proposed
- Owners: tech-lead (accepting Design Gate owner under the producer exclusion rule); the CEO decides at the Design Gate
- Related Work Items: MC-17, run run-022d0492a75a, technical design TD-MC17-PILOT-WEB, planner task T-026

## Context

- Problem statement: on first run a Vietnamese screen must ask for the pilot folder, find ffmpeg, ffprobe and the typeface, compute their SHA-256 and write a settings file the toolchain accepts, with the CEO-fixed numbers written as decided and not editable (C-020).
- Business and technical constraints: C-015, C-020, C-022, C-028.
- Current architecture baseline: the settings form and its validation, which refuses looser numbers and a pilot folder inside the repository (F-012); the example settings file holding the CEO numbers and the installed paths (F-019); the face check against its manifest line (F-028).
- Choice for the CEO, in plain words:
  - Option A (proposed, O-012): the screen proposes a pilot folder, finds the tools and the typeface itself, shows each one's location and fingerprint, and copies your fixed numbers (safe area, text sizes, 0.5 s, 1.2 s) from the example settings file already in the repository. The numbers are shown but cannot be changed. The toolchain's own settings check approves the file before it is saved.
  - Option B (O-013): the same, but the app carries its own copy of your numbers. Two copies must then be kept equal.

## Decision

- Selected option: O-012.
- Decision statement: setup takes the pilot folder as text with a proposed default (A-006); finds ffmpeg and ffprobe in the WinGet links folder or on the search path, the face through its font manifest line, and the toolchain executable in its build output; finds the repository root by walking up from the app to the folder holding the repository marker; copies every number from the example settings file; validates in process with PilotSettings.Read and Validate; writes the settings file into the pilot folder and the app record into the local application-data folder; stores the CEO's name for D-005.
- Scope of impact: M-001, M-002, M-008, M-009, M-017.

## Alternatives Considered

1. O-013: CEO numbers held in the app
- Benefits: no dependency on the example file's location.
- Risks: a second copy of the numbers that can drift.
- Why not selected: lower reuse leverage.

2. O-014: settings prepared once by the orchestrator, no screen
- Benefits: no setup screen to build.
- Risks: the operator depends on an agent for every new computer.
- Why not selected: eliminated by C-020.

## Consequences

- Positive outcomes expected: the toolchain accepts the file because its own validation ran first; no number can be loosened from the app; a missing tool is named in Vietnamese.
- Tradeoffs accepted: setup depends on the repository build and the example file.
- Risks introduced: R-003, R-020.

## Validation Plan

- Metrics to monitor: setup refusals by item.
- Verification checkpoints: P-003; T-026 acceptance (settings accepted, no input for any CEO number, missing tool named, pilot folder inside the repository refused).
- Rollback or reversal conditions: move to O-013 if the example file is moved or its numbers stop matching the CEO's decisions.

## Approval

- Architect: unsigned (producer; excluded from accepting)
- Tech Lead: unsigned
- Product Owner (if scope-impacting): unsigned
