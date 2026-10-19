# Workflows Specification

## Workflow Lifecycle

Each workflow is phase-driven and must move through:

1. Intake and qualification.
2. Execution planning.
3. Controlled implementation or analysis.
4. Verification and review.
5. Closure and artifact publication.

## Required Phase Model

Every workflow specification publishes a `## Phase Model` section: a table whose rows are
`Phase | Owner Agent | Participation | Input | Output Artifact | Gate | Required Skills`, in
execution order, followed by a `### Phase Identifier Sources` subsection recording where each
identifier came from.

That table is the machine contract. The Task Router in `runtime/framework_runtime.py` reads it
for phase identifiers, owners, artifacts, gates, and required skills, and derives the run's
dependency graph from the Input and Output Artifact columns. `registry/workflows.yaml` declares
the requirement under `automation.resolution`, so an active workflow without the section can be
described but not routed.

Three rules constrain what may appear in it:

- Phase identifiers are lowercase kebab-case, and where the owning agent ships a runtime
  manifest, the identifier is the one that manifest declares in `supportedWorkflows[].phase`.
  The workflow reproduces it rather than inventing a second name for the same phase.
- A phase resolves to exactly one owner agent. Other participants attach through gates,
  reviews, and escalation.
- Every gate named in the Gate column has a row in `workflow-gate-matrix.md` for this workflow,
  with at least one owner that does not produce the evidence the gate assesses.

## Entry Conditions

- Task intent is classified and scoped.
- Required context is loaded and current.
- Primary ownership and participating agents are assigned.
- Applicable risks and constraints are identified.

## Exit Conditions

- Deliverables are complete and traceable.
- Mandatory gates are approved.
- Residual risks are documented.
- Required memory updates are recorded.

## Approval Gates

- Scope Gate
- Design or Strategy Gate
- Implementation or Analysis Gate
- Verification Gate
- Closure or Release Gate

Gate ownership is defined per workflow in `workflow-gate-matrix.md`.

## Recovery Rules

- On gate failure, pause phase transition and record failure rationale.
- Route remediation to owning agent and re-enter at the failed gate.
- Escalate unresolved blockers to Orchestrator and Tech Lead.
- Preserve auditability of retries, rollbacks, and re-approvals.
