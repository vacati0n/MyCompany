# Workflow Specification: Research

## Goal
Reduce decision uncertainty by producing validated findings, evaluated options, and a recommendation.

## Entry Conditions
- Research question and decision scope are defined.
- Constraints, timeline, and stakeholders are identified.
- Existing context sources are accessible.

## Participating Agents
- business-analyst
- context-agent
- architect
- tech-lead
- product-owner
- documentation
- orchestrator

## Phase Model

Canonical, machine-resolvable phase identifiers for this workflow. Phase order is the row
order of this table. Phase identifiers are the routing keys used by
`config/agent-routing.md`, by agent manifests (`supportedWorkflows[].phase`), and by the
runtime gateway in `runtime/framework_runtime.py`.

`research-framing` is the identifier `agents/business-analyst/manifest.yaml` declares for
this workflow, and `option-synthesis` and `recommendation-draft` are the identifiers
`agents/tech-lead/manifest.yaml` declares for it. No other owning agent of this workflow
ships a runtime manifest, so every remaining identifier is derived from the state names in
`workflows/workflow-engine.md`, which is the state-machine form of this same workflow.

| Phase | Owner Agent | Participation | Input | Output Artifact | Gate | Required Skills |
|---|---|---|---|---|---|---|
| `research-framing` | `business-analyst` | primary | research question, decision scope, constraints and stakeholders | `requirement-framing.md` | Framing Gate | S02 |
| `technical-validation` | `context-agent` | primary | `requirement-framing.md`, technical sources, business evidence | `investigation-report.md` | Technical Validity Gate | S01, S03, S06, S11 |
| `option-synthesis` | `tech-lead` | primary | `investigation-report.md`, risk and effort criteria | `technical-recommendation.md` | none | S01, S08, S09 |
| `recommendation-draft` | `tech-lead` | primary | `technical-recommendation.md`, `investigation-report.md`, risk posture, implementation impact | `technical-recommendation.md` | Recommendation Gate | S02, S08 |
| `findings-publication` | `documentation` | primary | `technical-recommendation.md`, evidence references | `release-note.md`, on the `findings` communication basis | none | S10, S11 |

### Phase Identifier Sources

| Phase | Identifier source |
|---|---|
| `research-framing` | Declared by `agents/business-analyst/manifest.yaml`, `supportedWorkflows[research].phase`, as primary participation. The identifier matches the `ResearchFraming` state in `workflows/workflow-engine.md`. |
| `technical-validation` | Declared by `agents/context-agent/manifest.yaml`, `supportedWorkflows[research].phase`, as primary participation. The identifier matches the `TechnicalValidation` state in `workflows/workflow-engine.md`. |
| `option-synthesis` | Declared by `agents/tech-lead/manifest.yaml`, `supportedWorkflows[research].phase`. Consistent with the `OptionSynthesis` state in `workflows/workflow-engine.md`. |
| `recommendation-draft` | Declared by `agents/tech-lead/manifest.yaml`, `supportedWorkflows[research].phase`. Consistent with the `RecommendationDraft` state in `workflows/workflow-engine.md`. |
| `findings-publication` | Derived from the `FindingsPublication` state in `workflows/workflow-engine.md`, and now declared by `agents/documentation/manifest.yaml`, `supportedWorkflows[research].phase`. |

### Ownership Reconciliation

`workflows/workflow-engine.md` names the architecture role as owner of its `OptionSynthesis`
state. No architect manifest declares this workflow at all, and the runtime rejects a phase
whose owner manifest does not declare the workflow and phase it is routed for.
`option-synthesis` is therefore owned by `tech-lead`, with the architecture role
supporting it and deciding the Technical Validity Gate. This workflow overlaps `investigate`
by design: `investigate` is the compatibility entry point that the planner and architect
manifests declare, while this workflow is the canonical research lifecycle.

### Resolution Rules

- A phase resolves to exactly one owner agent. Secondary participants attach through gates,
  reviews, and escalation, never through phase ownership.
- An owner agent that ships a runtime manifest must declare this workflow and this phase
  identifier in `supportedWorkflows`. The runtime rejects a mismatch rather than guessing.
- A phase whose required skills are not all resolvable through `registry/skills.yaml` is
  blocked at skill resolution and does not start, per `skills/skill-resolver.md`.
- Every phase in this table is enqueued as a work item when a run starts. A phase whose
  owner agent has no registered capability is not skipped: it is blocked with a recorded
  reason and reported as an open escalation.
- Four phases of this workflow are dispatchable today: `research-framing`,
  `technical-validation`, `option-synthesis`, and `recommendation-draft`. Each owner holds an active record in `registry/agents.yaml`, a runtime
  module set that declares this workflow and phase, a host registration, resolvable
  phase-mandatory skills, a registered validator for its output artifact, and a declared context
  slice. Only `findings-publication` still blocks at `G1-CAPABILITY` with reason
  `awaiting_capability_registration`: `documentation` is host-invocable through
  `agents/<agent-id>.agent.md` but holds no record in `registry/agents.yaml`. See
  `runtime/README.md` for the implemented surface, and `verify_registry_coverage.py` check `C6`
  for the per-phase verdict.
- The order of this table is the run's dependency order. The runtime derives hard and soft
  edges from the Input and Output Artifact columns, so changing those columns changes the
  sequencing the runtime enforces.

## Execution Order

Prose form of the Phase Model above. The identifiers in parentheses are canonical.

1. Frame research objective and decision boundaries (`research-framing`).
2. Gather and validate technical and business evidence (`technical-validation`).
3. Evaluate options against risk, effort, and outcomes (`option-synthesis`).
4. Produce recommendation and implementation implications (`recommendation-draft`).
5. Publish findings and decision support summary (`findings-publication`).

## Deliverables
- Research brief and scope statement.
- Evidence log with confidence levels.
- Option comparison and tradeoff analysis.
- Recommendation report with next steps.

## Exit Criteria
- Recommendation is supported by traceable evidence.
- Risks and assumptions are explicit.
- Decision owner can approve or reject based on report quality.

## Failure Recovery
- Re-scope question when evidence is insufficient.
- Extend evidence collection when confidence is below threshold.
- Escalate conflicting findings to architect and product-owner.

## Approval Gates

Gate names are the canonical ones in `workflows/workflow-gate-matrix.md`, which is the
authority the runtime reads for gate ownership.

- Framing Gate: business-analyst and product-owner.
- Technical Validity Gate: context-agent and architect.
- Recommendation Gate: tech-lead and orchestrator.

Under the Producer Exclusion Rule the business analyst produces the framing package, so the
Framing Gate decision rests with product-owner; the context agent produces the validated
evidence set, so the Technical Validity Gate decision rests with the architecture role; and
tech-lead produces the recommendation report, so the Recommendation Gate decision rests
with orchestrator.