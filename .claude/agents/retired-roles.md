# Retired Roles Register

## Objective

Record every role identifier that appears in framework documents but is not part of the
active framework design, together with the reason it was retired, the role that supersedes
it, and how remaining references resolve.

A role is **active** when it owns at least one phase in an active workflow Phase Model. A
role that owns no phase, holds no registry record, and has no contract on disk is not an
unimplemented agent; it is an identifier the framework never adopted. This register is the
authoritative resolution for such identifiers, so that no reader and no runtime path is left
resolving a name that does not exist.

## Retirement Decisions

| Role | Retired on | Superseded by | Reason |
|---|---|---|---|
| `backend-developer` | 2026-08-19 | `dev-1-implement` | technology specialization is carried by skills, not by agents |
| `omn-planning-generate-clarification-questions` | 2026-08-19 | `business-analyst` | task-shaped identifier, not a role; duplicated an owned capability |

Each decision is recorded in `registry/agents.yaml` as a record at status `retired`, so a
runtime attempt to load one fails with an explicit policy failure naming the status rather
than an unresolved-identifier failure.

A bare `orchestrator` identifier was also retired on 2026-08-19, as a duplicate of the
coordination role then named `omn-orchestrator`. When agent identifiers dropped the `omn-`
prefix, that role took the name `orchestrator`, so the retirement record was withdrawn: the
identifier now names the active coordination agent (`agents/orchestrator.agent.md`).

## `backend-developer`

- Status: retired
- Retired on: 2026-08-19
- Superseding agent: `dev-1-implement` (`agents/dev-1-implement.agent.md`)
- Reason: the framework expresses technology specialization through skills, not through
  stack-specific agents

`backend-developer` was carried as a "specialized execution profile". The framework has one
Primary owner of Implementation Delivery, `dev-1-implement`, which is deliberately
stack-neutral. Stack-specific expertise reaches it through the skill registry — for example
`skills/dotnet/engineering-playbook.md`, `skills/react/react-engineering.md`, and
`skills/avalonia/desktop-ux-guidelines.md` — resolved per phase by the Skill Registry Loader.
Introducing a stack-named agent would create a second Primary owner for a capability the
matrix assigns to exactly one agent, which the capability model does not permit.

Evidence:

- `dev-1-implement` is the sole Primary for Implementation Delivery in
  `agents/capability-matrix.md` and owns the implementation phase of `implement-feature`,
  `fix-bug`, and `refactor`.
- No active workflow Phase Model names `backend-developer` as an owner agent or participant.
- No `agents/backend-developer.md` and no `agents/backend-developer/` module set has ever
  existed.

## `omn-planning-generate-clarification-questions`

- Status: retired
- Retired on: 2026-08-19
- Superseding agents: `business-analyst` for requirement clarification;
  `product-owner` for acceptance arbitration
- Reason: the identifier names a task, not a role, and duplicated a capability already owned

The identifier describes an activity — generating clarification questions — rather than an
accountable role. That activity is a declared capability, `requirement-clarification`, held
by `business-analyst`, which produces `requirement-framing.md` and is contracted to name
every gap and unresolved assumption rather than close it by inference. Where clarification
concerns acceptance boundaries rather than requirements, `product-owner` arbitrates.

Retaining the identifier as an agent also broke the ownership model: its row in
`agents/capability-matrix.md` claimed Primary for Product Scope and Acceptance alongside
`product-owner` and `business-analyst`, giving one column three Primary owners.

Evidence:

- `registry/agents.yaml` records `requirement-clarification` among the capabilities of
  `business-analyst`.
- No active workflow Phase Model names the identifier as an owner agent or participant.
- No contract file for the identifier has ever existed.

## Reference Resolution Rule

Where a retired identifier still appears in a document, it names the superseding role in the
table above. Two reference classes are deliberately preserved rather than rewritten.

**Executed agent contract modules.** `agents/planner/` and `agents/architect/` module files
are digest-pinned by committed run evidence: each invocation envelope records the digest of
every module loaded, and `runtime/verify_vertical_slice.py` check `C6` fails on drift.
Rewriting them would invalidate the provenance record of a run that actually executed.
Their machine-readable collaboration and escalation fields — held in `manifest.yaml`, which
carries no recorded digest — have been rewired to `orchestrator`, so runtime resolution
reads the active identifier. The prose references inside the pinned modules are a deferred
migration that lands the next time those contracts are revised and re-run.

**Historical evidence and dated reports.** Everything under `runs/` and every dated document
under `reports/` records what was true when it was written. These are audit records, not
active references, and are never edited in place.

Unbackticked prose naming "orchestrator" as an English role name — as in "escalate to
orchestrator and tech lead" in command specifications — is role language, not an identifier
reference, and is consistent with "tech lead" appearing beside it. It is left as written.

## Related Records

- `agents/agent-catalog.md` routes intent to active agents and points here for retired ones.
- `agents/capability-matrix.md` carries a row for every active agent only.
