# Architecture Decision Record: D-001

## Metadata

- ADR ID: D-001
- Title: The pilot toolchain is a new console project referencing only MediaCompany.Deterministic
- Status: Proposed
- Date: 2026-10-26
- Owners: architect (producer); tech-lead (accepting Design Gate owner)
- Related package: technical-design.md, run run-535f3f938f32, selected option O-002

## Context

- The toolchain must carry no language-model, speech-recognition or text-to-speech component (C-015), must leave the company store untouched (C-018) and must make zero network connection attempts (C-014).
- MediaCompany.Production implements the voice-model port by running the in-house text-to-speech model and holds that model's installation verifier (F-004), so referencing M-007 brings a text-to-speech component into the dependency list.
- MediaCompany.Host references Capability, Credentials, Persistence and Production (F-028), and Capability builds an HTTP client, so M-016 carries both networking and the company store.
- The coverage check FontCoverage is a pure function in MediaCompany.Deterministic (F-003), which references only Domain and Application with no package (F-002); scope decision 1 requires its reuse (C-017).
- The existing networking-reference check lives in Architecture.Tests, whose project carries the company host closure (F-020).

## Decision

- M-001 MediaCompany.Pilot is a new console project under src/ that references only M-006, and through it M-015 and MediaCompany.Domain.
- M-001 may not reference M-007, M-008, M-016, MediaCompany.Capability, MediaCompany.Credentials, any networking assembly, or any package.
- M-001 contains no type implementing the voice-model port (A-006).
- M-002 MediaCompany.Pilot.Tests holds a static closure test that enumerates M-001's referenced assemblies transitively and fails on any assembly outside the allowed list, and on any of the networking assemblies named by F-020.
- M-003 gains the two project entries; no other project changes.

## Alternatives Considered

- O-005: a pilot mode inside MediaCompany.Production with a Host command. It reuses ExternalMediaTool, the delivered approval and the provenance types directly, but it puts the text-to-speech implementation in the dependency list (C-015, F-004) and its approval path is read in the company store (C-018, F-017). Eliminated.
- O-004: a Python tool outside the solution. It has no .NET dependency at all, but it cannot reuse FontCoverage (C-017) and runs under an interpreter covered by no manifest (C-023). Eliminated.
- O-001 and O-003: these would sit on the same project boundary as this decision. They are eliminated on the render route (C-014), not on the boundary.

## Consequences

- Positive: M-001's closure is small and checkable by a test. The C-015 proof is structural rather than behavioural. The company store cannot be reached by construction (C-018).
- Negative: process-start, approval, held-stage and provenance rules are carried into M-001 rather than referenced. Implementation duplicates their logic in small form (R-004 covers one copy, the call-to-action list).
- Risk: if the voice-model port interface in M-015 is counted as a text-to-speech component (R-005), the reference to M-006 must be dropped and FontCoverage copied (Q-006).

## Validation Plan

- Metrics to monitor:
  - Number of assemblies in M-001's transitive closure outside the allowed list (target 0).
  - Number of types in the closure implementing the voice-model port (target 0).
- Verification checkpoints:
  - The M-002 closure test passes before the first component is added (P-001).
  - The dependency list of the sample run is checked for C-015 (P-008).
- Rollback or reversal conditions:
  - A no answer to Q-006: remove the M-006 reference, and record a superseding decision for the coverage check.
  - Removal of the toolchain: delete M-001, M-002 and their M-003 entries. No other module is affected.

## Approval

- Architect: unsigned; producer, excluded from accepting
- Tech Lead: unsigned; Design Gate decision pending
