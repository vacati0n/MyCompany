# Workflow Specification: Fix Bug

## Goal
Restore intended behavior through evidence-based diagnosis, targeted remediation, and regression prevention.

## Entry Conditions
- Defect report includes observable symptom and business impact.
- Reproduction context or equivalent diagnostic evidence is available.
- Severity classification has an identified owner.

## Participating Agents
- dev-1-bug-analyst
- dev-1-implement
- dev-2-reviewer
- qa
- tech-lead
- documentation
- orchestrator

## Phase Model

Canonical, machine-resolvable phase identifiers for this workflow. Phase order is the row
order of this table. Phase identifiers are the routing keys used by
`config/agent-routing.md`, by agent manifests (`supportedWorkflows[].phase`), and by the
runtime gateway in `runtime/framework_runtime.py`.

No owning agent of this workflow ships a runtime manifest, so every identifier is derived
from the state names in `workflows/workflow-engine.md`, which is the state-machine form of
this same workflow, and from the phase names in `skills/agent-skill-matrix.md`.

| Phase | Owner Agent | Participation | Input | Output Artifact | Gate | Required Skills |
|---|---|---|---|---|---|---|
| `triage-and-impact` | `dev-1-bug-analyst` | primary | defect report, symptom evidence, business impact statement | `bug-analysis.md`, carrying the severity classification and reproducibility decision at status `provisional` | Triage Gate | S11, S12, S02 |
| `root-cause-analysis` | `dev-1-bug-analyst` | primary | `bug-analysis.md`, logs, traces, code context | `bug-analysis.md` | none | S03, S06, S08, S12 |
| `fix-implementation` | `dev-1-implement` | primary | `bug-analysis.md`, affected modules, regression targets | `implementation-report.md` | Fix Gate | S03, S06, S10, S12 |
| `regression-validation` | `qa` | primary | `implementation-report.md`, defect acceptance criteria | `validation-report.md` | Verification Gate | S07, S09, S11 |
| `closure-and-communication` | `orchestrator` | primary | `validation-report.md`, known issue status | `orchestration-result.md`, carrying the closure record and release-impact communication package | Closure Gate | S10, S11 |

### Phase Identifier Sources

| Phase | Identifier source |
|---|---|
| `triage-and-impact` | Declared by `agents/dev-1-bug-analyst/manifest.yaml`, `supportedWorkflows[fix-bug].phase`. Consistent with the `Triage` state in `workflows/workflow-engine.md` and the Skill Matrix phase name. |
| `root-cause-analysis` | Declared by `agents/dev-1-bug-analyst/manifest.yaml`, `supportedWorkflows[fix-bug].phase`. Consistent with the `RootCauseAnalysis` state in `workflows/workflow-engine.md`. |
| `fix-implementation` | Declared by `agents/dev-1-implement/manifest.yaml`, `supportedWorkflows[fix-bug].phase`. |
| `regression-validation` | Declared by `agents/qa/manifest.yaml`, `supportedWorkflows[fix-bug].phase`. |
| `closure-and-communication` | Declared by `agents/orchestrator/manifest.yaml`, `supportedWorkflows[fix-bug].phase`. |

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
- All five phases of this workflow are dispatchable today. `triage-and-impact` and
  `root-cause-analysis` resolve the full capability chain through the `dev-1-bug-analyst`
  record in `registry/agents.yaml`; `fix-implementation` and `regression-validation` resolve
  theirs through `dev-1-implement` and `qa`; and `closure-and-communication` resolves
  its own through the `orchestrator` record, its runtime module set, and the registered
  validator for `orchestration-result.md`. Each waits on its predecessor rather than blocking
  on capability. `triage-and-impact` blocked at `G1-CAPABILITY` with reason
  `awaiting_contract_reconciliation` until the Output Artifact column above named a decision
  rather than a file; it now names `bug-analysis.md`, which the owning manifest declares and a
  registered validator decides, and the two phases emit that one artifact type at different
  statuses rather than two types. That was a gap in this table, not in the agent, and it is why
  the column's contract is now checked rather than assumed: `verify_validators.py` check `V5`
  fails any row here whose Output Artifact the Validation Engine cannot decide, and check `V6`
  tests the two readers of that column against each other. See `runtime/README.md` for the
  implemented surface, and `verify_registry_coverage.py` check `C6` for the per-phase verdict,
  which is the authority on which phases dispatch today.
- The order of this table is the run's dependency order. The runtime derives hard and soft
  edges from the Input and Output Artifact columns, so changing those columns changes the
  sequencing the runtime enforces.

## Execution Order

Prose form of the Phase Model above. The identifiers in parentheses are canonical.

1. Triage defect severity, scope, and reproducibility (`triage-and-impact`).
2. Perform root-cause analysis and identify fix strategy (`root-cause-analysis`).
3. Implement correction and add regression tests (`fix-implementation`).
4. Validate against symptom and adjacent risk paths (`regression-validation`).
5. Close with issue documentation and release impact summary
   (`closure-and-communication`).

## Deliverables
- Triage and root-cause report.
- Corrective code changes with regression tests.
- Verification evidence and defect closure record.
- Updated known issue status and release note entries.

## Exit Criteria
- Defect is no longer reproducible in target environment.
- Root cause is documented with supporting evidence.
- Regression coverage exists for the failure path.

## Failure Recovery
- Escalate when reproduction cannot be established.
- Re-open diagnosis when fix fails validation.
- Roll back unsafe corrections and re-enter design of fix strategy.
- Escalate recurring failures to tech-lead for architectural review.

## Approval Gates

Gate names are the canonical ones in `workflows/workflow-gate-matrix.md`, which is the
authority the runtime reads for gate ownership.

- Triage Gate: dev-1-bug-analyst and tech-lead.
- Fix Gate: dev-2-reviewer.
- Verification Gate: qa and dev-2-reviewer.
- Closure Gate: orchestrator and documentation.

The Producer Exclusion Rule decides who signs. The bug analyst produces the triage package,
so the Triage Gate decision rests with tech-lead; qa produces the verification
evidence, so the Verification Gate decision rests with dev-2-reviewer; and
orchestrator produces the closure record, so the Closure Gate decision rests with
documentation.

