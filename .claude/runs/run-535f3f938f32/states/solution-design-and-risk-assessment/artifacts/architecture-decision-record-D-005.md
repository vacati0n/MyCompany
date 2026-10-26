# Architecture Decision Record: D-005

## Metadata

- ADR ID: D-005
- Title: Layered network guard and proof of no generative or speech component
- Status: Proposed
- Date: 2026-10-26
- Owners: architect (producer); tech-lead (accepting Design Gate owner)
- Related package: technical-design.md, run run-535f3f938f32, selected option O-002, deferred item 10

## Context

- The sample run must make zero network connection attempts under a harness that fails on any socket opening (C-014). It must cost nothing (C-020).
- The dependency list and the invoked-tool log must hold no language-model, speech-recognition or text-to-speech component (C-015).
- The installed renderer folder holds the speech-recognition and speech-synthesis integration packages, which are never loaded (F-012).
- The existing networking-reference check is name-based and lives in Architecture.Tests (F-020).
- The media tool is assumed to open no socket when every input is a local file (A-004).
- No package may be added (C-019).

## Decision

The guard has these layers. All of them must pass for the run to count as evidence.

1. Static closure check (M-002): M-001's transitive references hold only the assemblies allowed by D-001, none of the networking assemblies of F-020, and no implementation of the voice-model port.
2. In-process listener (M-001): a listener on the platform socket and name-resolution telemetry event sources counts every connect and resolve. A non-zero count marks the package held and is written to the run record.
3. Protocol allow-list (M-001): every media tool and probe start carries a protocol allow-list holding only the file protocol, so any other protocol is refused by the tool itself.
4. Job-scoped socket sampling (M-002 harness): the sample run starts M-001 as a child in a job. The harness samples the platform TCP and UDP tables for every member process and fails on any row.
5. Component evidence (M-002): the run's dependency list is compared with an allow-list. The invoked-tool log must name only the configured media tool and probe executables, and their configured SHA-256 values. Any Node, browser or other executable fails the run. The installed renderer folder is never named in M-001's settings.

## Alternatives Considered

- O-001 and O-003: these would need module-list inspection of the render bundle and loopback sockets for the browser. That cannot pass layer 4 (C-014). Eliminated.
- O-004: a Python tool would need its own guard without the platform telemetry sources. Eliminated on C-017.
- O-005: the company host closure contains an HTTP client and the text-to-speech implementation (C-015, F-004). Eliminated.
- A single-layer guard (static check only) was considered within O-002 and rejected. It proves references, not runtime behaviour of the external media tool (A-004).

## Consequences

- Positive: C-014 and C-015 are proved by independent evidence. A gap in one layer, such as a missed short-lived connection in sampling (R-016), is covered by the others. No package is added (C-019).
- Negative: the harness and the listener add test code to M-002 and a small runtime component to M-001.
- Risk: if the media tool opens a socket despite the allow-list (R-002), no option meets C-014. This is escalated through the orchestrator.

## Validation Plan

- Metrics to monitor:
  - Socket telemetry count per run (target 0).
  - Socket-table rows owned by job members in the sample run (target 0).
  - Executables in the invoked-tool log outside the configured two (target 0).
- Verification checkpoints:
  - Layer 1 passes at P-001. Layers 2 to 5 pass on the sample run at P-008, before the release evidence at P-011.
  - A negative test proves each layer fails when its condition is broken. Examples: an allowed-list violation in a test-only assembly, and a protocol other than file named in a generated argument list.
- Rollback or reversal conditions:
  - Any layer reports a connection on the sample run: the run's package is held, implementation stops, and the finding is escalated through the orchestrator.
  - The CEO accepts loopback sockets under Q-005: record a superseding decision that scopes layer 4 to non-loopback addresses for the O-003 fallback only.

## Approval

- Architect: unsigned; producer, excluded from accepting
- Tech Lead: unsigned; Design Gate decision pending
