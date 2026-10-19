# Workflow Specification: Implement Feature

## Goal
Deliver new functionality that satisfies business scope, architectural standards, and quality gates.

## Entry Conditions
- Approved feature scope and measurable acceptance criteria.
- No unresolved critical clarification questions.
- Architecture constraints and non-functional targets are available.

## Participating Agents
- planner
- product-owner
- business-analyst
- architect
- tech-lead
- dev-1-implement
- dev-2-reviewer
- qa
- documentation
- orchestrator

## Phase Model

Canonical, machine-resolvable phase identifiers for this workflow. Phase order is the row
order of this table. Phase identifiers are the routing keys used by
`config/agent-routing.md`, by agent manifests (`supportedWorkflows[].phase`), and by the
runtime gateway in `runtime/framework_runtime.py`.

Identifiers are not invented here. Where an owning agent ships a runtime manifest, the
identifier is the one that manifest already declares, and this table reproduces it.

| Phase | Owner Agent | Participation | Input | Output Artifact | Gate | Required Skills |
|---|---|---|---|---|---|---|
| `scope-and-acceptance` | `product-owner` | primary | feature request, acceptance intent, business constraints | `scope-definition.md` | Scope Gate | S02, S07 |
| `execution-planning` | `planner` | primary | `scope-definition.md`, or an accepted planner input type | `execution-plan.md` | Planning Gate | S02, S01, S07 |
| `solution-design-and-risk-assessment` | `architect` | primary | `execution-plan.md`, technical constraints, risk targets | `technical-design.md` | Design Gate | S01, S03, S06, S09 |
| `implementation` | `dev-1-implement` | primary | `technical-design.md`, coding standards, test baseline | `implementation-report.md` | none | S03, S06, S10, S11, S12 |
| `quality-review` | `dev-2-reviewer` | primary | `implementation-report.md`, code diff, design references | `review-package.md` | Review Gate, Verification Gate | S07, S09, S08 |
| `documentation-and-release-handoff` | `documentation` | primary | verification report, change summary | `release-note.md`, on the `release-handoff` communication basis | Closure Gate | S11, S10, S12 |

### Phase Identifier Sources

| Phase | Identifier source |
|---|---|
| `scope-and-acceptance` | Declared by `agents/product-owner/manifest.yaml`, `supportedWorkflows[implement-feature].phase`. |
| `execution-planning` | Declared by `agents/planner/manifest.yaml`, `supportedWorkflows[implement-feature].phase`. |
| `solution-design-and-risk-assessment` | Declared by `agents/architect/manifest.yaml`, `supportedWorkflows[implement-feature].phase`. |
| `implementation` | Declared by `agents/dev-1-implement/manifest.yaml`, `supportedWorkflows[implement-feature].phase`. |
| `quality-review` | Declared by `agents/dev-2-reviewer/manifest.yaml`, `supportedWorkflows[implement-feature].phase`. |
| `documentation-and-release-handoff` | Derived from the Execution Order step and the Skill Matrix phase name, and now declared by `agents/documentation/manifest.yaml`, `supportedWorkflows[implement-feature].phase`. |

### Resolution Rules

- A phase resolves to exactly one owner agent. Secondary participants attach through gates,
  reviews, and escalation, never through phase ownership.
- An owner agent that ships a runtime manifest must declare this workflow and this phase
  identifier in `supportedWorkflows`. The runtime rejects a mismatch rather than guessing.
- A phase whose required skills are not all resolvable through `registry/skills.yaml` is
  blocked at skill resolution and does not start, per `skills/skill-resolver.md`.
- Every phase in this table is enqueued as a work item when a run starts. A phase whose
  owner agent has no registered capability is not skipped: it is blocked with a recorded
  reason and reported as an open escalation, so a run reports what it could not do rather
  than reporting success over a partial traversal.
- Phase execution is implemented for `scope-and-acceptance`, `execution-planning`,
  `solution-design-and-risk-assessment`, `implementation`, and `quality-review`. The
  remaining phase blocks at capability resolution. See `runtime/README.md` for the
  implemented surface.
- The Output Artifact of `quality-review` names `review-package.md` rather than the prose
  pair `review findings log, verification report` that earlier revisions carried. They were
  always one artifact: `templates/review-package.md` records that every review phase in the
  framework emits a severity-classified findings set closing with a readiness decision, and
  `runtime/review_package_validator.py` judges all of them against that one contract. The
  column now names the file, so the runtime can resolve a validator for it and the phase is
  dispatchable rather than blocked at `G1-CAPABILITY`.
- The order of this table is the run's dependency order. The runtime derives hard and soft
  edges from the Input and Output Artifact columns, so changing those columns changes the
  sequencing the runtime enforces.


## Execution Order

Prose form of the Phase Model above. The identifiers in parentheses are canonical.

1. Scope definition and acceptance alignment (`scope-and-acceptance`).
2. Execution planning and task decomposition (`execution-planning`).
3. Solution design and risk assessment (`solution-design-and-risk-assessment`).
4. Implementation with automated test coverage (`implementation`).
5. Peer review and corrective iteration (`quality-review`).
6. QA verification and release documentation handoff (`documentation-and-release-handoff`).

## Deliverables
- Scope definition (`templates/scope-definition.md`) produced by `product-owner`,
  carrying the bounded scope, the explicit non-goals, and the acceptance criteria.
- Execution plan (`templates/execution-plan.md`) produced by `planner`.
- Technical design package (`templates/technical-design.md`) produced by `architect`,
  with architecture decision records at status Proposed where decisions are significant.
- Implemented code and test evidence.
- Review package (`templates/review-package.md`) produced by `dev-2-reviewer`,
  carrying the severity-classified findings, the correction requests, and the verdict.
- Release note draft and operational notes.

## Exit Criteria
- All acceptance criteria are verified.
- Critical and major defects are resolved or formally accepted.
- Documentation and release-impact notes are complete.

## Failure Recovery
- Return to scope stage when acceptance is ambiguous.
- Return to `execution-planning` when task decomposition or dependencies are invalidated.
- Return to design stage when structural risks are discovered.
- Return to implementation stage when review or QA fails.
- Escalate unresolved blockers to orchestrator and tech-lead.

## Approval Gates

Gate names are the canonical ones in `workflows/workflow-gate-matrix.md`, which is the
authority the runtime reads for gate ownership.

- Scope Gate: product-owner and business-analyst.
- Planning Gate: tech-lead and orchestrator, on evidence produced by planner.
- Design Gate: architect and tech-lead.
- Review Gate: dev-2-reviewer and qa.
- Verification Gate: qa.
- Closure Gate: orchestrator and documentation.

Earlier revisions of this specification named a single `Quality Gate` for review and QA. The
Phase Model has always routed the two gates the gate matrix owns, `Review Gate` and
`Verification Gate`, and this list now names them. The Review Gate carries qa as a second
owner because dev-2-reviewer produces the findings that gate assesses, and the Producer
Exclusion Rule forbids approving one's own output.

