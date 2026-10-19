# Workflow Specification: Investigate

## Goal
Provide a compatibility workflow for research-oriented discovery and decision support.

## Entry Conditions
- Investigation question and decision owner are defined.
- Scope, timeline, and constraints are documented.
- Required context sources are accessible.

## Participating Agents
- planner
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

Identifiers are not invented here. `problem-framing` and `technical-discovery` are the
identifiers the planner and architect manifests already declare for this workflow, and
`option-analysis` and `recommendation` are the identifiers `agents/tech-lead/manifest.yaml`
declares for it; the remaining identifier is derived from the state name in
`workflows/workflow-engine.md`, which is the state-machine form of this same workflow.

| Phase | Owner Agent | Participation | Input | Output Artifact | Gate | Required Skills |
|---|---|---|---|---|---|---|
| `problem-framing` | `business-analyst` | primary | investigation question, decision owner, scope and constraints | `requirement-framing.md` | Framing Gate | S02 |
| `technical-discovery` | `context-agent` | primary | `requirement-framing.md`, context sources, technical data access | `investigation-report.md` | Technical Gate | S01, S03, S06, S11 |
| `option-analysis` | `tech-lead` | primary | `investigation-report.md`, constraints, evaluation criteria | `technical-recommendation.md` | none | S01, S08, S09 |
| `recommendation` | `tech-lead` | primary | `technical-recommendation.md`, `investigation-report.md`, risk posture, effort estimates | `technical-recommendation.md` | Recommendation Gate | S02, S08 |
| `publication` | `documentation` | primary | `technical-recommendation.md`, decision dependencies | `release-note.md`, on the `findings` communication basis | none | S10, S11 |

### Phase Identifier Sources

| Phase | Identifier source |
|---|---|
| `problem-framing` | Declared by `agents/business-analyst/manifest.yaml`, `supportedWorkflows[investigate].phase`, as primary participation. `agents/planner/manifest.yaml` declares the same identifier as supporting participation. |
| `technical-discovery` | Declared by `agents/context-agent/manifest.yaml`, `supportedWorkflows[investigate].phase`, as primary participation. `agents/architect/manifest.yaml` declares the same identifier as supporting participation, which is where the identifier originated. |
| `option-analysis` | Declared by `agents/tech-lead/manifest.yaml`, `supportedWorkflows[investigate].phase`. Consistent with the `OptionEvaluation` state in `workflows/workflow-engine.md` and the Skill Matrix phase name. |
| `recommendation` | Declared by `agents/tech-lead/manifest.yaml`, `supportedWorkflows[investigate].phase`. Consistent with the `Recommendation` state in `workflows/workflow-engine.md`. |
| `publication` | Derived from the `Publication` state in `workflows/workflow-engine.md`, and now declared by `agents/documentation/manifest.yaml`, `supportedWorkflows[investigate].phase`. |

### Ownership Reconciliation

`workflows/workflow-engine.md` names the architecture role as owner of its `OptionEvaluation`
state. The architect manifest binds its participation in this workflow to
`technical-discovery` and to that phase only, and a manifest is a machine authority the
runtime enforces. `option-analysis` is therefore owned by `tech-lead`, with `architect`
supporting it and deciding the Technical Gate. Elevating `architect` to owner of a second
phase of this workflow requires an architect contract change, which is out of scope here and
recorded in `reports/registry-coverage-report-2026-08-18.md`.

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
- Four phases of this workflow are dispatchable today: `problem-framing`,
  `technical-discovery`, `option-analysis`, and `recommendation`. Each owner holds an active record in `registry/agents.yaml`, a runtime
  module set that declares this workflow and phase, a host registration, resolvable
  phase-mandatory skills, a registered validator for its output artifact, and a declared context
  slice. Only `publication` still blocks at `G1-CAPABILITY` with reason
  `awaiting_capability_registration`: `documentation` is host-invocable through
  `agents/<agent-id>.agent.md` but holds no record in `registry/agents.yaml`. See
  `runtime/README.md` for the implemented surface, and `verify_registry_coverage.py` check `C6`
  for the per-phase verdict.
- The order of this table is the run's dependency order. The runtime derives hard and soft
  edges from the Input and Output Artifact columns, so changing those columns changes the
  sequencing the runtime enforces.

## Execution Order

Prose form of the Phase Model above. The identifiers in parentheses are canonical.

1. Frame investigation objective and success criteria (`problem-framing`).
2. Gather technical and business evidence (`technical-discovery`).
3. Evaluate alternatives and tradeoffs (`option-analysis`).
4. Recommend preferred option with impact analysis (`recommendation`).
5. Publish findings and decision dependencies (`publication`).

## Deliverables
- Investigation brief.
- Evidence and confidence record.
- Option analysis summary.
- Recommendation and next-step plan.

## Exit Criteria
- Recommendation is evidence-backed and decision-ready.
- Assumptions and risks are explicit.
- Stakeholders can proceed with implementation or defer with rationale.

## Failure Recovery
- Reframe scope when evidence is insufficient.
- Extend data collection for low-confidence conclusions.
- Escalate conflicting findings for architectural or business arbitration.

## Approval Gates

Gate names are the canonical ones in `workflows/workflow-gate-matrix.md`, which is the
authority the runtime reads for gate ownership.

- Framing Gate: business-analyst and product-owner.
- Technical Gate: context-agent and architect. Named `Evidence Gate` in earlier
  revisions of this specification.
- Recommendation Gate: tech-lead and orchestrator.

Under the Producer Exclusion Rule the business analyst produces the framing package, so the
Framing Gate decision rests with product-owner; the context agent produces the evidence
package, so the Technical Gate decision rests with the architecture role; and tech-lead
produces the recommendation, so the Recommendation Gate decision rests with
orchestrator.

