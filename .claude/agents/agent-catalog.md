# Agent Catalog

## Objective

Provide a quick routing map from intent to the right specialist agent.

## Intent Routing

| Intent | Primary Agent | Supporting Agents |
|---|---|---|
| Engineering planning and task decomposition | planner | product-owner, business-analyst, tech-lead |
| Feature implementation | dev-1-implement | planner, architect, dev-2-reviewer, qa |
| Bug fixing | dev-1-bug-analyst | dev-1-implement, qa, tech-lead |
| Architectural change | architect | tech-lead, dev-2-reviewer, planner |
| Product and requirement clarification | product-owner | business-analyst |
| Documentation updates | documentation | dev-2-reviewer, product-owner |
| Repository quality scan | dev-2-reviewer | tech-lead |
| Release preparation | tech-lead | qa, orchestrator, documentation |
| Cross-cutting orchestration | orchestrator | all relevant specialists |

## Host Entry Points

Every agent named as a phase owner by an active workflow Phase Model is host-invocable
through `agents/<agent-id>.agent.md`.

| Agent | Entry point | Contract layout | Registry record |
|---|---|---|---|
| planner | `agents/planner.agent.md` | runtime module set | present |
| architect | `agents/architect.agent.md` | runtime module set | present |
| product-owner | `agents/product-owner.agent.md` | runtime module set | present |
| business-analyst | `agents/business-analyst.agent.md` | runtime module set | present |
| context-agent | `agents/context-agent.agent.md` | runtime module set | present |
| tech-lead | `agents/tech-lead.agent.md` | runtime module set | present |
| dev-1-bug-analyst | `agents/dev-1-bug-analyst.agent.md` | runtime module set | present |
| dev-1-implement | `agents/dev-1-implement.agent.md` | runtime module set | present |
| dev-2-reviewer | `agents/dev-2-reviewer.agent.md` | runtime module set | present |
| qa | `agents/qa.agent.md` | runtime module set | present |
| documentation | `agents/documentation.agent.md` | runtime module set | present |
| orchestrator | `agents/orchestrator.agent.md` | runtime module set | present |

## Retired Roles

`backend-developer` and `omn-planning-generate-clarification-questions` are
retired. None owns a phase in any active Phase Model, none has a contract on disk, and each
holds a `retired` record in `registry/agents.yaml` so the runtime rejects it by status rather
than failing to resolve it. `agents/retired-roles.md` records the reason and superseding role
for each; route work as follows.

| Retired identifier | Route to |
|---|---|
| `backend-developer` | `dev-1-implement` |
| `omn-planning-generate-clarification-questions` | `business-analyst` |

## Escalation Rules

- Escalate unresolved design conflicts to `architect`.
- Escalate schedule or delivery risk to `tech-lead` and `orchestrator`.
- Escalate acceptance ambiguity to `product-owner`.
- Route requirement-to-task decomposition to `planner` before implementation begins.
- Route impact analysis and technical approach to `architect` before implementation begins.
