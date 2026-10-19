# Configuration: Agent Routing

## Routing Principles

- Route by task intent, not by preferred persona.
- Keep one accountable primary agent per stage.
- Add secondary agents only when they reduce risk.

## Intent to Primary Agent

- Feature -> `product-owner`, then `planner` for execution planning.
- Planning -> `planner`.
- Architecture and technical design -> `architect`.
- Bug -> `dev-1-bug-analyst`.
- Research -> `business-analyst`, then `context-agent` for evidence.
- Investigation of current-state behavior -> `business-analyst`, then `context-agent`.
- Refactor -> `architect` for scope and invariants, then `dev-1-implement`.
- Review -> `dev-2-reviewer`.
- Verification and testing -> `qa`.
- Documentation -> `documentation`.
- Release -> `tech-lead`.
- Deployment execution and run closure -> `orchestrator`.

Routing at the intent level selects the entry command. Ownership inside a run is not decided
here: it is the Owner Agent column of the routed workflow's Phase Model, which the Task Router
reads. Where this list and a Phase Model disagree, the Phase Model governs.

For a change to the framework itself, `config/self-hosting-profile.md` governs the entry command
instead of this list. Its Scope Rule decides which changes those are, and its Routing Table maps
each to one command. This list remains the authority for every change outside that scope, and the
Phase Model still governs ownership inside a run either way.

`commands/command-catalog.md` maps each intent to its command and primary workflow. Refactor
scope ownership moved to `architect` when `registry/workflows.yaml` and
`agents/architect/manifest.yaml` assigned it that phase; the tech lead decides the Invariant
Gate over that output rather than producing it.

## Escalation Rules

- Architecture ambiguity -> escalate to `architect`.
- Acceptance ambiguity -> escalate to `product-owner`.
- Quality gate failure -> escalate to `qa` and `tech-lead`.
- Planning ambiguity or contradictory requirements -> escalate from `planner` to `product-owner`.
