# Template Catalog

## Purpose

Provide a controlled index of templates and intended usage points.

| Template | Used In | Owner |
|---|---|---|
| `execution-plan.md` | execution planning; produced by the Planner Agent | planner |
| `technical-design.md` | design and architecture phases; produced by the Architect Agent | architect |
| `implementation-plan.md` | implementation planning | tech-lead |
| `execution-report-template.md` | workflow execution closure and runtime reporting | orchestrator |
| `bug-analysis.md` | defect diagnosis and closure | dev-1-bug-analyst |
| `investigation-report.md` | research and investigations | context-agent |
| `technical-recommendation.md` | the delivery decision phases of investigate, research, review-pull-request, and release | tech-lead |
| `release-note.md` | release communication | documentation |
| `pull-request.md` and `pr.md` | change review packages | dev-2-reviewer |
| `postmortem.md` | incident learning | tech-lead |
| `architecture-decision-record.md` | durable architecture decisions; emitted at status Proposed | architect |
| `framework-change-proposal.md` | the governance record of one framework-internal change, routed by `config/self-hosting-profile.md`; instances live under `proposals/` | orchestrator |

## Governance

- Prefer one canonical template per artifact type in new workflows.
- Keep aliases only for backward compatibility.
- `execution-plan.md` is the canonical planning handoff artifact. Its binding structural
  contract is `agents/planner/output.md`; the template renders that contract.
- `implementation-plan.md` remains the tech-lead planning worksheet and is not a
  substitute for `execution-plan.md` at workflow handoff boundaries.
- `technical-recommendation.md` is the canonical delivery decision artifact. One type carries
  all six tech-lead phases, because each is a criteria-based comparison of named options closing
  with a recommendation; the `decisionBasis` field carries the lens. Its binding structural
  contract is `agents/tech-lead/output.md`, and
  `runtime/technical_recommendation_validator.py` decides conformance — including that the
  recommended option is one the artifact evaluated and that the deciding authority is not the
  producer.
- `technical-design.md` is the canonical architecture handoff artifact. Its binding
  structural contract is `agents/architect/output.md`; `design.md` remains a legacy alias.
- `architecture-decision-record.md` is emitted by `architect` at status `Proposed` only.
  Acceptance is recorded by the Design Gate owners, not by the producing agent.
- `framework-change-proposal.md` is the canonical record of a framework-internal change. Its
  binding governance contract is `config/self-hosting-profile.md`, whose Evidence Rule fixes the
  run artifacts an instance must link; `runtime/change_proposal_validator.py` decides
  conformance and re-reads the linked run rather than trusting the author.
