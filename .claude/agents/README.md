# Agents Specification

## Agent Contract

Each agent is a bounded role with explicit decision authority, inputs, outputs,
constraints, and success criteria.

The mandatory baseline contract is defined in `domain-model/agent-specification.md`.
All agent specifications must conform before activation. The shared `agent-contract.md` and
`agent-lifecycle.md` modules that once carried it are superseded; see
`agents/superseded-contracts.md`.

## Naming Convention

- Identifiers are lowercase kebab-case and must match
  `^[a-z0-9]+(-[a-z0-9]+)*$`, as enforced by `registry/agents.yaml`.
- Identifiers carry no prefix. Role-scoped agents use the format `<domain>-<role>`.
  Examples: `dev-1-implement`, `dev-2-reviewer`.
- Framework-level agents use the plain role name.
  Examples: `planner`, `architect`, `orchestrator`.
- The retired `omn-` prefix survives only in historical run evidence and in the
  retired-role records of `agents/retired-roles.md`.
- Filenames and folder names must match agent identifiers exactly.

## Agent Layout

An agent is authored in one of two layouts. Both satisfy the same contract.

### Runtime Module Set (preferred)

```text
agents/<identifier>/
  manifest.yaml   runtime descriptor, capabilities, authority scope, versioning
  system.md       operating charter and invariants
  identity.md     Standard Agent Contract, all mandatory sections
  reasoning.md    deterministic reasoning procedure
  execution.md    lifecycle states, gates, retry, escalation
  output.md       output contract for the agent deliverable
  quality.md      self-verification checks and rejection rules
  examples.md     conforming and non-conforming references
```

`manifest.yaml` is the entrypoint. It declares `loadOrder`, and the runtime loads
modules in that order. `agents/planner/` and `agents/architect/` are the reference
implementations.

An agent whose responsibilities border another agent declares a `boundaries` block in its
manifest, naming the neighbouring agent and the responsibility split. See
`agents/architect/manifest.yaml` for the pattern.

### Single File (legacy, superseded)

`agents/<identifier>.md` containing every required section. No active agent uses this layout: all
twelve are runtime module sets. Retained only as a description of the shape the superseded files
under `agents/superseded-contracts.md` carry. New agents use the runtime module set.

## Internal Phase Gates

A runtime agent may define phase gates inside its own execution, declared in
`execution.md`. These are self-verification checkpoints, not framework gates.

Internal gate names must not collide with any gate name in
`workflows/workflow-gate-matrix.md`. A collision makes it impossible to tell, in an
artifact or a log line, whether an agent-internal checkpoint or a governance gate is
meant. Passing an internal gate never approves a framework gate.

## Registration

Registration has two independent layers, and an agent needs both before the runtime can
dispatch it.

**Host registration** is `agents/<identifier>.agent.md`: frontmatter carrying `name`,
`description`, `tools`, and `model`, followed by an adapter body that loads the agent's
authoritative contract. `name` must equal the agent identifier, and the frontmatter must
parse as YAML, so a bare `: ` inside an unquoted `description` breaks the registration. The
host resolves an agent by identifier from its scan of these files, and the runtime's
`host-subagent` adapter can load the same file directly in bootstrap mode. The adapter is an
entry point, never a contract: it loads the module set declared by the agent's
`manifest.yaml` in its `loadOrder`. Every active agent is a runtime module set; the legacy
single-file shape is superseded, per `agents/superseded-contracts.md`.

**Registry registration** is a record in `registry/agents.yaml` with a resolvable
`specificationPath`. Without it an agent cannot be routed, and its identifier will not
resolve as a task owner in an execution plan or as a phase owner in a workflow Phase Model.

Every agent that owns a phase in an active workflow Phase Model now carries both layers: a host
registration and a registry record, backed by a runtime module set and a registered validator for
the artifact its phases emit. That combination is what makes those phases execute, and every phase
owner has all of it.

A registered agent does not, on its own, make every phase it owns dispatchable, and the runtime
still enforces that. Where a Phase Model states a phase's Output Artifact as a decision or a
package rather than a named file, no validator can be registered against it, and the phase blocks
with reason `awaiting_contract_reconciliation` even though its owner is complete — the gap being in
the Output Artifact column rather than in the agent. Where a phase names an artifact its owner does
not declare among its `outputs`, it blocks the same way. No active phase is in either state today;
both remain live failure modes for any row authored from here on, which is why the Output Artifact
column must name a file and the owning manifest must declare it.

No count is stated here, because a count in prose goes stale the moment an agent lands.
`verify_registry_coverage.py` check `C6` reports the current per-phase verdict, with a recorded
reason for every blocker, and it is the authority on which phases dispatch today.

## Required Sections

Every agent contract must include, exactly once and in contract order
(in `identity.md` for a runtime module set, or in the single file for legacy agents):

- Identity
- Mission
- Scope
- Inputs
- Outputs
- Decision Making
- Constraints
- Collaboration Rules
- Error Handling
- Escalation
- Completion
- Examples

## Execution Model

- One primary owner per task.
- Supporting agents are assigned by workflow phase and gate requirements.
- Orchestrator resolves role conflicts and escalation paths.
- Agent outputs must map to approved templates registered in `registry/templates.yaml`.
- An agent that declares runtime modules must pass its own quality checks before handoff.

## Communication Rules

- Communicate with deterministic, evidence-based statements.
- Escalate unresolved ambiguity to the designated decision owner.
- Record externally relevant decisions in memory or decision artifacts.
- Do not bypass workflow gates or ownership boundaries.
