# Workflow Specification: Review Pull Request

## Goal
Provide structured multi-agent validation of proposed changes before merge or release promotion.

## Entry Conditions
- Pull request includes scope summary and linked requirement.
- Relevant tests have been executed and evidence is attached.
- Diff is stable and ready for review.

## Participating Agents
- dev-2-reviewer
- architect
- qa
- tech-lead
- documentation
- orchestrator

## Phase Model

Canonical, machine-resolvable phase identifiers for this workflow. Phase order is the row
order of this table. Phase identifiers are the routing keys used by
`config/agent-routing.md`, by agent manifests (`supportedWorkflows[].phase`), and by the
runtime gateway in `runtime/framework_runtime.py`.

Identifiers are not invented here. `structural-compliance` is the identifier the architect
manifest already declares for this workflow, `test-risk-validation` is the one the qa
manifest declares, and `merge-decision` is the one `agents/tech-lead/manifest.yaml`
declares; the remaining identifiers are derived from the state names in
`workflows/workflow-engine.md`, which is the state-machine form of this same workflow.

| Phase | Owner Agent | Participation | Input | Output Artifact | Gate | Required Skills |
|---|---|---|---|---|---|---|
| `code-quality-review` | `dev-2-reviewer` | primary | stable pull request diff, standards checklist, test evidence | `review-package.md` | Code Quality Gate | S03, S07, S09 |
| `structural-compliance` | `architect` | supporting | quality findings with correction requests, architecture rules, security criteria | `review-package.md`, carrying the structural and security findings under the `architecture` and `security` categories | Architecture Gate | S01, S06, S09 |
| `test-risk-validation` | `qa` | primary | `review-package.md`, test evidence, risk profile | `validation-report.md` | Verification Gate | S07, S08, S11 |
| `documentation-impact` | `documentation` | primary | `validation-report.md`, release implications | `release-note.md`, on the `documentation-delta` communication basis | none | S10, S11 |
| `merge-decision` | `tech-lead` | primary | `release-note.md`, unresolved findings list | `technical-recommendation.md` | Merge Gate | S07, S09, S10 |

### Phase Identifier Sources

| Phase | Identifier source |
|---|---|
| `code-quality-review` | Derived from the `QualityReview` state in `workflows/workflow-engine.md`, and now declared by `agents/dev-2-reviewer/manifest.yaml`, `supportedWorkflows[review-pull-request].phase`. |
| `structural-compliance` | Declared by `agents/architect/manifest.yaml`, `supportedWorkflows[review-pull-request].phase`. |
| `test-risk-validation` | Declared by `agents/qa/manifest.yaml`, `supportedWorkflows[review-pull-request].phase`. |
| `documentation-impact` | Derived from the `DocumentationImpact` state in `workflows/workflow-engine.md`, and now declared by `agents/documentation/manifest.yaml`, `supportedWorkflows[review-pull-request].phase`. |
| `merge-decision` | Declared by `agents/tech-lead/manifest.yaml`, `supportedWorkflows[review-pull-request].phase`. Consistent with the `MergeDecision` state in `workflows/workflow-engine.md`. |

### Ownership Reconciliation

`structural-compliance` is owned by `architect` while its Participation column reads
`supporting`, and both statements are accurate. The architect manifest declares supporting
participation in this workflow, because `dev-2-reviewer` drives the review end to end;
ownership of this one phase still belongs to the architecture role, because the structural
and security assessment is its output and no other registered agent may author it. The
Participation column reproduces the manifest rather than contradicting it.

`architect` is host-invocable and registry-resolvable, but this phase's output artifact is a
findings assessment that the architect manifest does not declare as an output and no
validator covers. Registering the review-package validator did not clear it, because that
artifact belongs to `code-quality-review`; the phase blocks at `G1-CAPABILITY` until the
architect manifest contracts an output for the assessment this phase produces and a validator
is registered for it.

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
- Three phases of this workflow are dispatchable today: `code-quality-review`,
  `test-risk-validation`, and `merge-decision`. Each owner holds an active record in
  `registry/agents.yaml`, a runtime module set that declares this workflow and phase, a host
  registration, resolvable phase-mandatory skills, a registered validator for its output
  artifact, and a declared context slice. Two still block at `G1-CAPABILITY`:
  `structural-compliance`, because `architect` declares no contracted output for the findings
  assessment this workflow asks of it, and `documentation-impact`, because `documentation`
  is host-invocable but holds no record in `registry/agents.yaml`. See `runtime/README.md` for
  the implemented surface, and `verify_registry_coverage.py` check `C6` for the per-phase
  verdict.
- The order of this table is the run's dependency order. The runtime derives hard and soft
  edges from the Input and Output Artifact columns, so changing those columns changes the
  sequencing the runtime enforces.

## Execution Order

Prose form of the Phase Model above. The identifiers in parentheses are canonical.

1. Perform initial quality and standards review (`code-quality-review`).
2. Perform architecture and security impact review (`structural-compliance`).
3. Validate test adequacy and risk coverage (`test-risk-validation`).
4. Confirm documentation and release implications (`documentation-impact`).
5. Decide merge readiness and required actions (`merge-decision`).

## Deliverables
- Review findings by severity and owner.
- Required corrections and optional improvements.
- Merge recommendation with risk summary.

## Exit Criteria
- All critical findings are resolved.
- Major findings are resolved or formally accepted.
- Test and documentation coverage is sufficient for scope.

## Failure Recovery
- Return PR to implementation when critical issues are found.
- Add focused validation tasks for unproven risk areas.
- Escalate unresolved disputes to tech-lead and orchestrator.

## Approval Gates

Gate names are the canonical ones in `workflows/workflow-gate-matrix.md`, which is the
authority the runtime reads for gate ownership.

- Code Quality Gate: dev-2-reviewer and tech-lead.
- Architecture Gate: architect and tech-lead.
- Verification Gate: qa and dev-2-reviewer.
- Merge Gate: tech-lead and orchestrator.

Each gate carries a second owner because the first owner produces the evidence that gate
assesses, and the Producer Exclusion Rule forbids approving one's own output. The deciding
owner is tech-lead for the Code Quality and Architecture Gates, dev-2-reviewer for
the Verification Gate, and orchestrator for the Merge Gate.