# Architecture Decision Record

## Metadata

- ADR ID: D-002
- Title: How a browser recording becomes the file the toolchain admits
- Date: 2026-10-27
- Status: Proposed
- Owners: tech-lead (accepting Design Gate owner under the producer exclusion rule); the CEO decides at the Design Gate
- Related Work Items: MC-17, run run-022d0492a75a, technical design TD-MC17-PILOT-WEB, planner tasks T-008, T-009

## Context

- Problem statement: the browser records WebM/Opus (F-024), which the toolchain does not admit as a recording and would count as a presenter clip (F-009). The take must be converted once by the installed ffmpeg, and the converted file's digest must be the one the toolchain admits and checks for reuse (C-006).
- Business and technical constraints: C-005, C-006, C-015, C-016, C-028, C-029.
- Current architecture baseline: admitted recording types and the one-recording rule (F-009); the 60.0 s held limit (F-015); MediaTool's digest-checked, file-only ffmpeg runner (F-011); ffmpeg's WebM/Opus to WAV conversion measured reproducible (F-023).
- Choice for the CEO, in plain words:
  - Option A (proposed, O-005): when you accept a take, the app converts it once with the installed ffmpeg into a plain WAV sound file. The same take always gives the same file, so the reuse check by fingerprint works. The original browser take is kept in a separate folder, so the tool never mistakes it for the presenter clip. An uploaded recording is kept exactly as uploaded.
  - Option B (O-006): the app repackages the browser's sound without re-encoding it into an Ogg file. Smaller files, but nobody has measured that the same take always gives the same file.

## Decision

- Selected option: O-005.
- Decision statement: on take acceptance, M-001 converts the take once through MediaTool into mono 16-bit PCM WAV with fixed parameters, no metadata and length capped at the held recording limit; the WAV becomes the only recording in the per-video input folder; the raw take stays in the takes folder until replaced; uploads with an admitted extension are stored byte for byte; a re-record replaces the admitted recording only when the new take is accepted.
- Scope of impact: M-001, M-002, M-008, M-016.

## Alternatives Considered

1. O-006: stream copy into Ogg
- Benefits: no re-encoding; smaller file.
- Risks: reproducibility of the container bytes is unmeasured (C-029).
- Why not selected: meets one of the two recorded quality attributes against O-005's two.

2. O-007: page encodes WAV itself
- Benefits: no conversion process.
- Risks: conversion outside the installed ffmpeg.
- Why not selected: eliminated by C-006.

3. O-008: pass the WebM take unchanged
- Benefits: no conversion.
- Risks: the toolchain refuses it as a recording and counts it as a clip.
- Why not selected: eliminated by C-015.

## Consequences

- Positive outcomes expected: SC-08 to SC-11 hold with the toolchain deciding admission and reuse; the admitted digest is stable for evidence.
- Tradeoffs accepted: the browser take is re-encoded once; a take running past the held limit is cut at it, which is the CEO's hard stop.
- Risks introduced: R-015, R-016, R-019, R-022.

## Validation Plan

- Metrics to monitor: converted length against the held limit; digest equality across two conversions of one take.
- Verification checkpoints: P-009, P-013 (generated audio over 60 s, re-record with a different length), P-015 (second video with the same file refused by the toolchain).
- Rollback or reversal conditions: move to O-006 if the toolchain refuses a converted WAV, or if conversion proves non-reproducible on the operator's computer.

## Approval

- Architect: unsigned (producer; excluded from accepting)
- Tech Lead: unsigned
- Product Owner (if scope-impacting): unsigned
