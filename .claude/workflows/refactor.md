# Workflow Specification: Refactor

## Goal
Improve internal code quality and maintainability while preserving externally observable behavior.

## Entry Conditions
- Refactor scope and target modules are defined.
- Behavioral invariants are explicitly documented.
- Baseline tests and key metrics are available.

## Participating Agents
- planner
- tech-lead
- architect
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
identifier is the one that manifest already declares, and this table reproduces it. The
remaining identifiers are derived from the state names in `workflows/workflow-engine.md`,
which is the state-machine form of this same workflow.

| Phase | Owner Agent | Participation | Input | Output Artifact | Gate | Required Skills |
|---|---|---|---|---|---|---|
| `scope-invariants-and-risk-profile` | `architect` | primary | change request, behavioral invariants, baseline metrics, architecture context | `technical-design.md` | Invariant Gate | S01, S02, S07 |
| `safety-net-establishment` | `qa` | primary | `technical-design.md`, existing tests, quality thresholds | `validation-report.md` | none | S07, S03 |
| `refactor-implementation` | `dev-1-implement` | primary | `validation-report.md`, coding standards | `implementation-report.md` | Implementation Gate | S03, S06, S12 |
| `behavioral-validation` | `qa` | primary | `implementation-report.md`, parity checklist, performance checks | `validation-report.md` | Regression Gate | S07, S08, S09 |
| `closure-and-debt-record` | `orchestrator` | primary | `validation-report.md`, documentation updates | `orchestration-result.md`, carrying the closure summary with technical debt delta and follow-up actions | Closure Gate | S10, S11 |

### Phase Identifier Sources

| Phase | Identifier source |
|---|---|
| `scope-invariants-and-risk-profile` | Declared by `agents/architect/manifest.yaml`, `supportedWorkflows[refactor].phase`. |
| `safety-net-establishment` | Declared by `agents/qa/manifest.yaml`, `supportedWorkflows[refactor].phase`, first of two refactor phases this agent owns. |
| `refactor-implementation` | Declared by `agents/dev-1-implement/manifest.yaml`, `supportedWorkflows[refactor].phase`. |
| `behavioral-validation` | Declared by `agents/qa/manifest.yaml`, `supportedWorkflows[refactor].phase`, second of two refactor phases this agent owns. |
| `closure-and-debt-record` | Declared by `agents/orchestrator/manifest.yaml`, `supportedWorkflows[refactor].phase`. |

### Ownership Reconciliation

- `workflows/workflow-engine.md` names `tech-lead` as owner of its `ScopeInvariants`
  state. The workflow record in `registry/workflows.yaml` and the architect manifest both
  assign scope, invariants, and risk profile to `architect`, and a manifest is a machine
  authority while that state table is prose. The Phase Model therefore routes `architect`,
  and the tech lead participates through the Invariant Gate rather than through phase
  ownership.
- `agents/planner/manifest.yaml` declares `scope-invariants-and-risk-profile` as its
  supporting phase for this workflow, matching the identifier the owning agent declares. It
  previously declared `scope-and-invariants`, which resolved to no row in this Phase Model;
  that mismatch is recorded against its point-in-time state in
  `reports/registry-coverage-report-2026-08-18.md` and is now closed. Supporting
  participation attaches through gates and reviews, never through phase ownership, so the
  corrected declaration leaves `architect` the sole owner of this phase.

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
- Every phase of this workflow is dispatchable today. `scope-invariants-and-risk-profile`
  resolves through `architect`, the two validation phases through `qa`,
  `refactor-implementation` through `dev-1-implement`, and `closure-and-debt-record`
  through the `orchestrator` record, its runtime module set, and the registered validator
  for `orchestration-result.md`. Each owner holds an active record in `registry/agents.yaml`, a
  runtime module set declaring this workflow and phase, a host registration, resolvable
  phase-mandatory skills, and a declared context slice. A phase that waits does so on its
  predecessor's output, not on capability. See `runtime/README.md` for the implemented surface,
  and `verify_registry_coverage.py` check `C6` for the per-phase verdict.
- The order of this table is the run's dependency order. The runtime derives hard and soft
  edges from the Input and Output Artifact columns, so changing those columns changes the
  sequencing the runtime enforces.

## Execution Order

Prose form of the Phase Model above. The identifiers in parentheses are canonical.

1. Define refactor scope, invariants, and risk profile
   (`scope-invariants-and-risk-profile`).
2. Strengthen the safety net with focused test coverage (`safety-net-establishment`).
3. Implement incremental refactor changes (`refactor-implementation`).
4. Validate behavioral parity and quality improvements (`behavioral-validation`).
5. Document outcomes and remaining technical debt (`closure-and-debt-record`).

## Deliverables
- Refactor plan with invariants and risk assumptions.
- Refactored code with supporting tests.
- Verification evidence for behavior preservation.
- Technical debt delta and follow-up recommendations.

## Exit Criteria
- Behavioral invariants remain unchanged.
- Quality metrics improve or remain within accepted limits.
- No unresolved critical regressions remain.

## Failure Recovery
- Revert offending changes when invariants fail.
- Expand tests when hidden coupling is discovered.
- Escalate architecture-level concerns to architect.

## Approval Gates

Gate names are the canonical ones in `workflows/workflow-gate-matrix.md`, which is the
authority the runtime reads for gate ownership.

- Invariant Gate: architect and tech-lead. Named `Scope Gate` in earlier revisions
  of this specification.
- Implementation Gate: dev-2-reviewer.
- Regression Gate: qa and dev-2-reviewer. Named `Validation Gate` in earlier
  revisions of this specification.
- Closure Gate: orchestrator and documentation.

The Producer Exclusion Rule applies twice here. `architect` produces the design package
assessed at the Invariant Gate, so acceptance rests with tech-lead. `qa` produces
the parity evidence assessed at the Regression Gate, so acceptance rests with
dev-2-reviewer. `orchestrator` produces the closure summary, so the Closure Gate
decision rests with documentation.

