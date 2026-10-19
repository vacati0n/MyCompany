# Workflow Specification: Release

## Goal
Promote approved changes to target environments safely with clear ownership, observability, and rollback control.

## Entry Conditions
- Scope for release candidate is frozen.
- Required implementation, review, and QA gates are complete.
- Deployment and rollback plans are documented.

## Participating Agents
- tech-lead
- qa
- dev-2-reviewer
- documentation
- product-owner
- context-agent
- orchestrator

## Phase Model

Canonical, machine-resolvable phase identifiers for this workflow. Phase order is the row
order of this table. Phase identifiers are the routing keys used by
`config/agent-routing.md`, by agent manifests (`supportedWorkflows[].phase`), and by the
runtime gateway in `runtime/framework_runtime.py`.

Identifiers are not invented here. `readiness-assessment` is the identifier
`agents/tech-lead/manifest.yaml` declares for this workflow, and the architect manifest
declares it too, as supporting participation; the remaining identifiers are derived from the
state names in `workflows/workflow-engine.md`, which is the state-machine form of this same
workflow.

| Phase | Owner Agent | Participation | Input | Output Artifact | Gate | Required Skills |
|---|---|---|---|---|---|---|
| `readiness-assessment` | `tech-lead` | primary | frozen release scope, prior gate evidence, risk posture | `technical-recommendation.md` | Readiness Gate | S07, S08, S09 |
| `artifact-packaging` | `dev-2-reviewer` | primary | `technical-recommendation.md`, build inputs, versioning rules | `review-package.md`, carrying the packaging evidence under the `packaging` category | Artifact Gate | S10, S11, S12 |
| `candidate-validation` | `qa` | primary | `review-package.md`, target-like environment, validation checklist | `validation-report.md` | none | S07, S08, S11 |
| `deployment-execution` | `orchestrator` | primary | `validation-report.md`, deployment plan, rollback plan | `orchestration-result.md`, carrying the deployment status with monitoring health record | Deployment Gate | S11, S08, S12 |
| `communication-and-post-release` | `documentation` | primary | deployment status with monitoring health record, final change summary, stakeholder list | `release-note.md` and post-release action plan | Communication Gate | S02, S11 |

### Phase Identifier Sources

| Phase | Identifier source |
|---|---|
| `readiness-assessment` | Declared by `agents/tech-lead/manifest.yaml`, `supportedWorkflows[release].phase`. Also declared by `agents/architect/manifest.yaml` for this workflow, as supporting participation. |
| `artifact-packaging` | Derived from the `ArtifactPackaging` state in `workflows/workflow-engine.md`, and now declared by `agents/dev-2-reviewer/manifest.yaml`, `supportedWorkflows[release].phase`. |
| `candidate-validation` | Declared by `agents/qa/manifest.yaml`, `supportedWorkflows[release].phase`. |
| `deployment-execution` | Declared by `agents/orchestrator/manifest.yaml`, `supportedWorkflows[release].phase`. |
| `communication-and-post-release` | Derived from the `CommunicationPostRelease` state in `workflows/workflow-engine.md`, and now declared by `agents/documentation/manifest.yaml`, `supportedWorkflows[release].phase`. |

### Ownership Reconciliation

The architect manifest declares supporting participation in `readiness-assessment`, so the
architecture role contributes structural risk judgment to the readiness report that
`tech-lead` owns. Post-release monitoring, which `skills/agent-skill-matrix.md` previously
carried as a separate grouping, is the monitoring health record produced inside
`deployment-execution` and carried into `communication-and-post-release`; `context-agent`
and `dev-1-bug-analyst` support those phases without owning them.

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
- Four phases of this workflow are dispatchable today: `readiness-assessment`,
  `candidate-validation`, `deployment-execution`, and `communication-and-post-release`. Each
  owner holds an active record in `registry/agents.yaml`, a runtime module set that declares this
  workflow and phase, a host registration, resolvable phase-mandatory skills, a registered
  validator for its output artifact, and a declared context slice. One still blocks at
  `G1-CAPABILITY` with reason `awaiting_contract_reconciliation`: `artifact-packaging`, whose
  versioned-artifact output no validator covers.
- `deployment-execution` is dispatchable as a *coordination* phase, which is what its Phase Model
  row declares. `orchestrator` records the deployment state, monitoring health, and rollback
  position it is supplied, against the deployment and rollback plans it is given; it performs no
  deployment and reads no monitor itself, because no framework agent holds operational access.
  The dependency on that access is therefore a dependency of the release, carried into this phase
  as supplied evidence, and an orchestration result that reported an operational state no input
  carried would be rejected by its own quality contract. See `runtime/README.md` for the
  implemented surface, and `verify_registry_coverage.py` check `C6` for the per-phase verdict.
- The order of this table is the run's dependency order. The runtime derives hard and soft
  edges from the Input and Output Artifact columns, so changing those columns changes the
  sequencing the runtime enforces.

## Execution Order

Prose form of the Phase Model above. The identifiers in parentheses are canonical.

1. Confirm readiness, approvals, and unresolved risk posture (`readiness-assessment`).
2. Build and package release artifacts (`artifact-packaging`).
3. Validate release candidate in target-like conditions (`candidate-validation`).
4. Execute deployment and monitor health indicators (`deployment-execution`).
5. Publish release communication and post-release actions
   (`communication-and-post-release`).

## Deliverables
- Release readiness report.
- Versioned build and deployment artifacts.
- Validation evidence and monitoring checklist.
- Final release notes and post-release plan.

## Exit Criteria
- Deployment completes with healthy system indicators.
- No unresolved critical release blockers remain.
- Stakeholders receive complete release communication.

## Failure Recovery
- Trigger rollback when health thresholds are breached.
- Pause rollout and return to validation when anomalies occur.
- Escalate unresolved operational issues to tech-lead and orchestrator.

## Approval Gates

Gate names are the canonical ones in `workflows/workflow-gate-matrix.md`, which is the
authority the runtime reads for gate ownership.

- Readiness Gate: tech-lead and qa.
- Artifact Gate: dev-2-reviewer and tech-lead.
- Deployment Gate: orchestrator and tech-lead.
- Communication Gate: documentation and product-owner.

Under the Producer Exclusion Rule the deciding owner is qa for the Readiness Gate,
tech-lead for the Artifact and Deployment Gates, and product-owner for the
Communication Gate. The Artifact and Deployment Gates carry a second owner for that reason:
their first-listed owner produces the evidence the gate assesses.

