# Architecture Decision Record: D-003

## Metadata

- ADR ID: D-003
- Title: The pilot ledger is a chained append-only file outside the repository and the company store
- Status: Proposed
- Date: 2026-10-26
- Owners: architect (producer); tech-lead (accepting Design Gate owner)
- Related package: technical-design.md, run run-535f3f938f32, selected option O-002, deferred item 9

## Context

- The ledger enforces three limits: at most 12 videos, at most two per product, and a distinct focus for a product's second video (C-013). It also refuses a recording digest that was used for another video (C-005).
- The company store is frozen (C-018). The delivered audit chain hashes company audit records (F-019) in the store, and Persistence tests need the store database (F-021), which the test run may not need (C-021).
- The delivered output-root rule refuses folders inside the repository (F-006). No media file may enter the repository (C-016).
- The operator's machine holds a pilot folder outside the repository (A-011).

## Decision

- One UTF-8 ledger file lives in the configured pilot folder outside the repository. M-001 refuses a location inside the repository or the company store.
- Each entry carries:
  - video code
  - product page path
  - product video number
  - focus text after NFC
  - recording SHA-256
  - video SHA-256
  - package folder
  - time written
  - previous entry hash
  - entry hash (SHA-256 over the entry fields and the previous hash)
- Before every read, the chain is verified; a break stops the run and names the first broken entry.
- A write keeps the previous generation and replaces the file atomically.
- Each package holds a copy of its own entry, so the ledger can be rebuilt from the packages.
- A re-run for the same video code appends a superseding entry. Freshness and limits are compared against other video codes only.

## Alternatives Considered

- O-005: ledger rows in the company store through M-008, using the delivered audit chain. This violates C-018 and C-021. Eliminated with its option.
- O-001 and O-003: these hold the same file ledger. They are eliminated on the render route (C-014), not on the ledger.
- O-004: a ledger in a Python tool. Eliminated on C-017.

## Consequences

- Positive: the ledger needs no database or container (C-021). Tampering and partial writes are detectable. The ledger can be rebuilt from package copies (R-012).
- Negative: digest equality cannot detect a re-encoded copy of a recording (R-009); that residual stays with the CEO's checklist review.
- Risk: if the ledger and all package copies are lost together, the limits cannot be enforced for past videos.

## Validation Plan

- Metrics to monitor:
  - Chain verification failures per run (target 0).
  - Refusals by limit (twelfth plus one, third per product, repeated focus, reused digest), each one observed in tests.
- Verification checkpoints:
  - Unit tests on a temporary folder covering all four refusals of C-005 and C-013, a broken chain, and a superseding re-run (P-005, M-002).
  - The sample run writes and re-reads its entry (P-011).
- Rollback or reversal conditions:
  - Reverse to the kept previous generation when the current file fails verification and no newer package copy exists.
  - Replace the file ledger by a superseding decision if the CEO later unfreezes the company store for the pilot.

## Approval

- Architect: unsigned; producer, excluded from accepting
- Tech Lead: unsigned; Design Gate decision pending
