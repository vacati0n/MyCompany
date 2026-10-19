# Agent Specification: Planner (Superseded)

## Status

- Status: superseded
- Superseded on: 2026-08-18
- Superseded by: `agents/planner/` (agent `planner`, version 1.0.0)
- Retained for: reference during one major cycle, per
  `domain-model/agent-specification.md` backward-compatibility rules

This file is no longer the authoritative contract for agent `planner`. It contains no
contract sections and must not be loaded by the runtime.

## Where the Contract Now Lives

The Planner Agent is implemented as a runtime module set. Load
`agents/planner/manifest.yaml` and follow its declared `loadOrder`.

| Concern | Module |
|---|---|
| Runtime descriptor, capabilities, authority scope | `agents/planner/manifest.yaml` |
| Operating charter and invariants | `agents/planner/system.md` |
| Standard Agent Contract, all mandatory sections | `agents/planner/identity.md` |
| Deterministic reasoning procedure | `agents/planner/reasoning.md` |
| Lifecycle states, gates, retry, escalation | `agents/planner/execution.md` |
| Output contract for `execution-plan.md` | `agents/planner/output.md` |
| Self-verification checks and rejection rules | `agents/planner/quality.md` |
| Conforming and non-conforming references | `agents/planner/examples.md` |

## Migration Notes

- The Agent ID `planner` is preserved. No downstream reference needs to change.
- Agent version remains `1.0.0`; the migration is structural, not behavioral, and the
  prior contract's responsibilities are carried forward in full.
- Contract sections previously defined here now live in `agents/planner/identity.md`.
- The prior free-form plan package is replaced by the fixed twelve-section
  `execution-plan.md` artifact, rendered from `templates/execution-plan.md`.
- Escalation targets name `architect` for structural concerns, matching the registered
  agent set.
- Discovery is through `registry/agents.yaml`, record `planner`, whose
  `specificationPath` points at the runtime folder.

## Compatibility

Any process that resolved this file by path should resolve
`agents/planner/manifest.yaml` instead. This stub will be removed when the next major
contract cycle closes.
