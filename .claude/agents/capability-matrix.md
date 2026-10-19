# Agent Capability Matrix

## Legend

- Primary: agent is accountable owner for this capability.
- Secondary: agent contributes materially but is not the default owner.
- Unsupported: capability is outside the agent's intended scope.

## Matrix

| Agent | Orchestration and Routing | Context Discovery | Product Scope and Acceptance | Execution Planning | Architecture and Design | Implementation Delivery | Defect Analysis | Quality Verification | Code Review and Governance | Documentation and Communication | Release Readiness and Operations |
|---|---|---|---|---|---|---|---|---|---|---|---|
| orchestrator | Primary | Secondary | Secondary | Secondary | Secondary | Unsupported | Secondary | Secondary | Secondary | Secondary | Secondary |
| context-agent | Unsupported | Primary | Secondary | Unsupported | Secondary | Unsupported | Secondary | Unsupported | Unsupported | Unsupported | Secondary |
| product-owner | Unsupported | Secondary | Primary | Secondary | Secondary | Unsupported | Unsupported | Secondary | Unsupported | Secondary | Secondary |
| business-analyst | Unsupported | Secondary | Primary | Secondary | Secondary | Unsupported | Secondary | Unsupported | Unsupported | Unsupported | Unsupported |
| planner | Secondary | Secondary | Secondary | Primary | Unsupported | Unsupported | Unsupported | Secondary | Unsupported | Secondary | Unsupported |
| architect | Secondary | Secondary | Secondary | Secondary | Primary | Unsupported | Secondary | Secondary | Secondary | Secondary | Secondary |
| tech-lead | Secondary | Secondary | Secondary | Secondary | Primary | Secondary | Secondary | Secondary | Secondary | Unsupported | Primary |
| dev-1-implement | Unsupported | Unsupported | Secondary | Unsupported | Secondary | Primary | Secondary | Secondary | Unsupported | Unsupported | Secondary |
| dev-1-bug-analyst | Unsupported | Secondary | Secondary | Unsupported | Secondary | Secondary | Primary | Secondary | Unsupported | Unsupported | Secondary |
| dev-2-reviewer | Unsupported | Unsupported | Secondary | Unsupported | Secondary | Secondary | Secondary | Primary | Primary | Secondary | Secondary |
| qa | Unsupported | Secondary | Secondary | Unsupported | Secondary | Unsupported | Secondary | Primary | Secondary | Unsupported | Primary |
| documentation | Unsupported | Secondary | Secondary | Unsupported | Unsupported | Unsupported | Unsupported | Secondary | Unsupported | Primary | Secondary |

## Capability Identifiers

Matrix columns are coarse ownership groupings. Registry records and execution plans
reference fine-grained capability identifiers. This mapping is authoritative for the
`Required Capabilities` section of `templates/execution-plan.md` and for the
`capabilities` array in `registry/agents.yaml`.

| Matrix Column | Capability Identifiers |
|---|---|
| Orchestration and Routing | `workflow-routing`, `run-coordination` |
| Context Discovery | `context-discovery` |
| Product Scope and Acceptance | `scope-definition`, `acceptance-authority` |
| Execution Planning | `requirement-analysis`, `task-decomposition`, `dependency-analysis`, `execution-sequencing`, `complexity-estimation`, `risk-identification`, `execution-planning` |
| Architecture and Design | `architecture-analysis`, `impact-analysis`, `technical-approach-definition`, `reuse-assessment`, `option-evaluation`, `architecture-decision-authoring`, `sequencing-guidance`, `structural-risk-analysis`, `architecture-estimation` |
| Implementation Delivery | `implementation-delivery` |
| Defect Analysis | `defect-analysis`, `root-cause-analysis` |
| Quality Verification | `quality-verification`, `validation-design` |
| Code Review and Governance | `code-review`, `governance-enforcement` |
| Documentation and Communication | `documentation`, `release-communication` |
| Release Readiness and Operations | `release-readiness` |

New capability identifiers are added here before they may be referenced by an agent
manifest, a registry record, or an execution plan. An agent that needs a capability absent
from this table records a framework gap rather than inventing the identifier.

## Coverage Notes

- `planner` is the only Primary owner of Execution Planning and is the entry point for
  feature delivery workflows.
- `planner` is Unsupported for Architecture and Design, Implementation Delivery, Defect
  Analysis, Code Review and Governance, and Release Readiness. These boundaries are
  enforced by `agents/planner/system.md` and verified by check `Q2` in
  `agents/planner/quality.md`.
- `architect` is the active Primary owner of Architecture and Design. It is Unsupported for
  Implementation Delivery only; its Secondary ratings reflect analysis and expectation
  setting rather than execution. Boundaries against `planner`, `tech-lead`, and
  `dev-2-reviewer` are declared in `agents/architect/manifest.yaml`.
- `backend-developer` and `omn-planning-generate-clarification-questions`
  are retired and carry no row. None owns a phase in any active Phase Model and none has a
  contract under `agents/`. `agents/retired-roles.md` records the retirement reason and the
  superseding owner for each; `registry/agents.yaml` carries each at status `retired`.

## Recommendations for Balancing Responsibilities

1. Reduce concentration in release operations by assigning explicit secondary release ownership to `orchestrator` and `dev-2-reviewer` for non-deployment release gates.
2. Strengthen context-to-delivery continuity by designating `context-agent` as Secondary for quality verification evidence preparation.
3. Lower architect and tech-lead overlap by clarifying that `architect` owns strategic design decisions while `tech-lead` owns delivery feasibility and operational tradeoffs.
4. Improve documentation resilience by assigning `product-owner` as explicit Secondary for user-impact communication and `qa` as Secondary for validation evidence sections.
5. Increase defect feedback efficiency by making `dev-1-implement` Secondary in post-incident analysis writeups with `dev-1-bug-analyst` remaining Primary.
6. Add quarterly matrix calibration tied to workflow retrospectives to adjust Primary/Secondary assignments based on observed bottlenecks.
7. Keep the matrix closed over active agents only: every phase owner in an active Phase Model holds exactly one row, and a retired identifier holds none. Capability resolution is then total over the set the runtime can actually dispatch.
