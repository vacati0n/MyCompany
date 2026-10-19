# Agent Specification: Architect (Superseded)

## Status

- Status: superseded
- Superseded on: 2026-08-18
- Superseded by: `agents/architect/` (agent `architect`, version 1.0.0)
- Retained for: reference during one major cycle, per
  `domain-model/agent-specification.md` backward-compatibility rules

This file is no longer the authoritative contract for agent `architect`. It contains no
contract sections and must not be loaded by the runtime.

## Where the Contract Now Lives

The Architect Agent is implemented as a runtime module set. Load
`agents/architect/manifest.yaml` and follow its declared `loadOrder`.

| Concern | Module |
|---|---|
| Runtime descriptor, capabilities, authority scope, boundaries | `agents/architect/manifest.yaml` |
| Operating charter and invariants | `agents/architect/system.md` |
| Standard Agent Contract, all mandatory sections | `agents/architect/identity.md` |
| Deterministic reasoning procedure | `agents/architect/reasoning.md` |
| Lifecycle states, gates, retry, escalation | `agents/architect/execution.md` |
| Output contract for the design package and decision records | `agents/architect/output.md` |
| Self-verification checks and rejection rules | `agents/architect/quality.md` |
| Conforming and non-conforming references | `agents/architect/examples.md` |

## Migration Notes

- The Agent ID `architect` is preserved. No downstream reference needs to change.
- Agent version remains `1.0.0`; the migration is structural, not behavioral, and the prior
  contract's responsibilities are carried forward in full.
- Contract sections previously defined here now live in `agents/architect/identity.md`.
- The prior free-form architecture package is replaced by the thirteen-section technical
  design package rendered from `templates/technical-design.md`, plus architecture decision
  records at status `Proposed`.
- Three responsibilities are now bounded explicitly against neighbouring agents. The
  executable task breakdown belongs to `planner`; delivery feasibility and scheduling belong
  to `tech-lead`; verifying concrete changes belongs to `dev-2-reviewer`. See
  `manifest.yaml` `boundaries`.
- Decision records are emitted at status `Proposed` only. Acceptance belongs to the Design
  Gate owners.
- Discovery is through `registry/agents.yaml`, record `architect`, whose
  `specificationPath` points at the runtime folder.

## Compatibility

Any process that resolved this file by path should resolve
`agents/architect/manifest.yaml` instead. This stub will be removed when the next major
contract cycle closes.
