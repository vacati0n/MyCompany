# Agent Skill Matrix

## Purpose

Define reusable skill coverage across all active agents and standard workflows.
This matrix ensures consistent capability planning, staffing decisions, and handoff quality.

## Skill Catalog

The Skill ID is the canonical, stable skill identifier used by agent manifests and by
workflow phase requirements. The registry identifier is the discovery key in
`registry/skills.yaml`, derived from the skill file name.

| Skill ID | Category | Skill File | Registry Identifier | Version | Registry Status |
|---|---|---|---|---|---|
| S01 | Architecture | architecture/clean-architecture-checklist.md | clean-architecture-checklist | 1.1.0 | active |
| S02 | Business | business/domain-modeling.md | domain-modeling | 1.0.0 | active |
| S03 | .NET | dotnet/engineering-playbook.md | engineering-playbook | 1.0.0 | active |
| S04 | Avalonia | avalonia/desktop-ux-guidelines.md | desktop-ux-guidelines | 1.0.0 | active |
| S05 | React | react/react-engineering.md | react-engineering | 1.0.0 | active |
| S06 | Database | database/database-engineering.md | database-engineering | 1.0.0 | active |
| S07 | Testing | testing/testing-strategy.md | testing-strategy | 1.0.0 | active |
| S08 | Performance | performance/performance-engineering.md | performance-engineering | 1.0.0 | active |
| S09 | Security | security/secure-engineering.md | secure-engineering | 1.0.0 | active |
| S10 | Git | git/git-collaboration.md | git-collaboration | 1.0.0 | active |
| S11 | Logging | logging/observability-logging.md | observability-logging | 1.0.0 | active |
| S12 | Error Handling | error-handling/error-handling-strategy.md | error-handling-strategy | 1.0.0 | active |

Every Skill ID in the catalogue above is registered and active, so every row resolves through
`registry/skills.yaml`. No unregistered row remains.

### Retired Skill Assets

Two skill files exist on disk and carry no Skill ID. Both are formally retired, recorded under
`retiredAssets` in `registry/skills.yaml` and excluded from active mapping per the Retired
lifecycle state in `domain-model/skill-specification.md`. `retiredAssets` is not a resolution
surface: the resolver reads `records` only, so neither asset can enter a resolved skill bundle.

| Asset | Status | Retired | Replacement | Reason |
|---|---|---|---|---|
| `cicd/release-automation.md` | retired | 2026-08-19 | S10, S07 | No Skill ID; satisfies neither the skill contract in `skill-governance.md` nor the approved category taxonomy, which has no CI/CD value. Release tagging is carried by S10, test-pass thresholds by S07, and the pipeline gates by `quality-gates.md`, which is gate authority rather than skill guidance. |
| `coding-style/dotnet-coding-style.md` | retired | 2026-08-19 | S03 | No Skill ID; satisfies neither the skill contract nor the taxonomy, which has no Coding Style value. Its standards restate S03, the duplicate-skill-intent conflict named in `skill-resolver.md`, resolved in favour of the registered skill. |

Neither asset is referenced by any agent manifest or by any phase of any active workflow, so
retirement removed no capability from any resolved skill bundle. Registering either one would
require a category taxonomy extension in `registry/skills.yaml` and a rewrite to the full skill
contract; retirement is recorded instead because the guidance already exists in the registered
skills named above.

## Registry Resolution

- `registry/skills.yaml` is the authority for skill identity metadata: Skill ID, category,
  version, status, and specification path. Skill files do not carry this metadata.
- The skill file named by `specificationPath` remains the sole source of skill behavior.
  Registration never alters guidance.
- A record is registered on any of three grounds: an active registered agent declares it, a
  workflow phase that the runtime routes declares it as mandatory, or this matrix records
  non-advisory coverage of it for an agent holding an active registry record. Registration
  status is tracked in the Skill Catalog above.
- A skill file that satisfies none of the three grounds is not left as an unregistered catalog
  row. It is either registered when a ground appears, or formally retired under `retiredAssets`
  with a replacement reference. Both dispositions are explicit, so no skill asset sits in an
  undeclared state, and a phase can never require a skill that resolves through this matrix but
  not through the registry.
- S03, S10, and S11 are registered on the second ground: no active agent manifest declares
  any of them, but `solution-design-and-risk-assessment` requires S03, `technical-discovery`
  retains S11 as mandatory, and S10 is mandatory for every routed implementation, closure, and
  release-communication phase in the tables below.
- S04 and S05 are registered on the third ground: no routed phase declares either as mandatory,
  and no agent manifest declares them, but this matrix records Secondary coverage of both for
  `dev-1-implement`, `qa`, and `documentation`, each of which holds an active
  registry record. They are platform-specific skills, so a routed phase requiring them is a
  property of the product under change rather than of the framework's own workflows;
  registration makes them resolvable in advance of that phase rather than after it.
- Registration on the second and third grounds records identity metadata only. It grants no
  skill to any agent: no manifest `skills` block changed, and under Declaration Precedence the
  manifest governs agent-scoped resolution, so no agent's resolved skill bundle changed.

### Declaration Precedence

For an agent that ships a runtime manifest, the manifest `skills` block is authoritative for
that agent's declared skill dependencies and proficiencies. This matrix remains authoritative
for workflow-phase requirements and for agents without a manifest. Where the two disagree, the
manifest wins for agent-scoped resolution and the delta is recorded below.

| Agent | Matrix non-advisory coverage not declared in manifest | Disposition |
|---|---|---|
| planner | none | Aligned |
| product-owner | none | Aligned |
| business-analyst | none | Aligned |
| context-agent | none | Aligned |
| dev-2-reviewer | none | Aligned |
| tech-lead | none | Aligned |
| architect | S03, S07, S11 | Manifest is intentionally narrower. Architect produces no code, tests, or telemetry, so these remain matrix-level coverage for review and escalation support, not declared dependencies. Elevation requires an architect contract change. |
| dev-1-implement | S02, S04, S05, S08 | Manifest is intentionally narrower. S02 and S08 remain matrix-level coverage: the implementer works to a design that already fixes the domain vocabulary and the performance objective. S04 and S05 apply only when the change under implementation targets an Avalonia or React surface, which no phase of any active workflow does today. Elevation requires a manifest change. |
| qa | S04, S05, S10 | Manifest is intentionally narrower. S10 remains matrix-level coverage because QA validates behavior rather than change isolation. S04 and S05 apply only to a UI surface under validation, as above. |
| documentation | S04, S05 | Manifest is intentionally narrower, on the same platform-specific ground as above. |

The S04 and S05 rows above are the matrix coverage that registers those two skills on the third
ground. Because the manifest governs agent-scoped resolution, registering them left every one of
these deltas exactly as it was: no manifest gained a skill, and no resolved bundle changed. A
routed phase that targets an Avalonia or React surface is what would convert a delta into a
declared manifest dependency.

## Proficiency Legend

- Primary: core skill required for expected outcomes.
- Secondary: applied frequently to improve quality and risk control.
- Advisory: used for review, validation, or escalation support.

## Agent to Skill Coverage

| Agent | S01 | S02 | S03 | S04 | S05 | S06 | S07 | S08 | S09 | S10 | S11 | S12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| orchestrator | Secondary | Secondary | Advisory | Advisory | Advisory | Advisory | Secondary | Secondary | Secondary | Secondary | Secondary | Secondary |
| context-agent | Advisory | Secondary | Advisory | Advisory | Advisory | Advisory | Advisory | Advisory | Advisory | Advisory | Secondary | Secondary |
| product-owner | Advisory | Primary | Advisory | Advisory | Advisory | Advisory | Secondary | Advisory | Advisory | Advisory | Advisory | Advisory |
| business-analyst | Advisory | Primary | Advisory | Advisory | Advisory | Advisory | Secondary | Advisory | Advisory | Advisory | Advisory | Secondary |
| planner | Advisory | Primary | Advisory | Advisory | Advisory | Advisory | Secondary | Advisory | Advisory | Advisory | Advisory | Advisory |
| architect | Primary | Secondary | Secondary | Advisory | Advisory | Secondary | Secondary | Secondary | Secondary | Advisory | Secondary | Secondary |
| tech-lead | Secondary | Secondary | Primary | Advisory | Advisory | Secondary | Secondary | Secondary | Secondary | Secondary | Secondary | Secondary |
| dev-1-implement | Secondary | Secondary | Primary | Secondary | Secondary | Secondary | Secondary | Secondary | Secondary | Secondary | Secondary | Primary |
| dev-1-bug-analyst | Secondary | Secondary | Primary | Advisory | Advisory | Secondary | Secondary | Secondary | Secondary | Advisory | Primary | Primary |
| dev-2-reviewer | Secondary | Secondary | Secondary | Advisory | Advisory | Secondary | Primary | Secondary | Primary | Secondary | Secondary | Secondary |
| qa | Advisory | Secondary | Secondary | Secondary | Secondary | Secondary | Primary | Secondary | Secondary | Secondary | Secondary | Secondary |
| documentation | Advisory | Secondary | Secondary | Secondary | Secondary | Advisory | Secondary | Advisory | Advisory | Advisory | Secondary | Secondary |

## Workflow Participation by Phase

Phase identifiers below are the canonical routing keys. Every active workflow publishes them
in its own Phase Model table, and this matrix reproduces them so that phase-mandatory skill
requirements stay keyed to the identifier the runtime actually routes. Where the owning agent
ships a runtime manifest, the identifier originates in that manifest's
`supportedWorkflows[].phase`; the workflow specification and this matrix reproduce it rather
than restate it.

The first agent listed in Primary Agents is the phase owner. A phase resolves to exactly one
owner; the remaining primary agents participate through gates, reviews, and escalation. Where
this matrix and a workflow Phase Model disagree on ownership, the Phase Model governs, because
that is the table the Task Router reads.

### Implement Feature

| Phase ID | Phase | Primary Agents | Required Skills |
|---|---|---|---|
| `scope-and-acceptance` | Scope and Acceptance | product-owner, business-analyst | S02, S07 |
| `execution-planning` | Execution Planning | planner | S02, S01, S07 |
| `solution-design-and-risk-assessment` | Solution Design | architect, tech-lead | S01, S03, S06, S09 |
| `implementation` | Implementation | dev-1-implement | S03, S06, S10, S11, S12 |
| `quality-review` | Quality Review | dev-2-reviewer, qa | S07, S09, S08 |
| `documentation-and-release-handoff` | Documentation and Release Handoff | documentation, orchestrator | S11, S10, S12 |

Every phase-mandatory skill in this table is registered, so each row resolves against
`registry/skills.yaml`. Every row also resolves at capability level: each owner agent holds an
active registry record, a manifest declaring the phase, and a registered validator for the
artifact the phase emits, so every phase in this table is dispatchable.
`verify_registry_coverage.py` check `C6` reports the current per-phase verdict and is the
authority on it.

### Fix Bug

| Phase ID | Phase | Primary Agents | Required Skills |
|---|---|---|---|
| `triage-and-impact` | Triage and Impact | dev-1-bug-analyst, tech-lead | S11, S12, S02 |
| `root-cause-analysis` | Root Cause Analysis | dev-1-bug-analyst, tech-lead | S03, S06, S08, S12 |
| `fix-implementation` | Fix Implementation | dev-1-implement | S03, S06, S10, S12 |
| `regression-validation` | Regression Validation | qa, dev-2-reviewer | S07, S09, S11 |
| `closure-and-communication` | Closure and Communication | orchestrator, documentation | S10, S11 |

### Investigate

S11 is retained as mandatory for `technical-discovery`: this workflow exists to reconstruct
current-state behavior, and observability evidence is the primary source for that
reconstruction. It is registered accordingly.

| Phase ID | Phase | Primary Agents | Required Skills |
|---|---|---|---|
| `problem-framing` | Problem Framing | business-analyst, product-owner, planner | S02 |
| `technical-discovery` | Technical Discovery | context-agent, architect | S01, S03, S06, S11 |
| `option-analysis` | Option Analysis | tech-lead, architect | S01, S08, S09 |
| `recommendation` | Recommendation | tech-lead, orchestrator | S02, S08 |
| `publication` | Publication | documentation, orchestrator | S10, S11 |

### Research

| Phase ID | Phase | Primary Agents | Required Skills |
|---|---|---|---|
| `research-framing` | Research Framing | business-analyst, product-owner | S02 |
| `technical-validation` | Technical Validation | context-agent, architect | S01, S03, S06, S11 |
| `option-synthesis` | Option Synthesis | tech-lead, architect | S01, S08, S09 |
| `recommendation-draft` | Recommendation Draft | tech-lead, orchestrator | S02, S08 |
| `findings-publication` | Findings Publication | documentation, product-owner | S10, S11 |

### Refactor

| Phase ID | Phase | Primary Agents | Required Skills |
|---|---|---|---|
| `scope-invariants-and-risk-profile` | Refactor Scope and Invariants | architect, tech-lead, planner | S01, S02, S07 |
| `safety-net-establishment` | Safety Net | qa, dev-1-implement | S07, S03 |
| `refactor-implementation` | Safe Implementation | dev-1-implement | S03, S06, S12 |
| `behavioral-validation` | Validation and Review | qa, dev-2-reviewer | S07, S08, S09 |
| `closure-and-debt-record` | Finalization | orchestrator, documentation | S10, S11 |

### Review Pull Request

| Phase ID | Phase | Primary Agents | Required Skills |
|---|---|---|---|
| `code-quality-review` | Code Quality Review | dev-2-reviewer, tech-lead | S03, S07, S09 |
| `structural-compliance` | Architecture and Security Review | architect, tech-lead | S01, S06, S09 |
| `test-risk-validation` | Test and Risk Validation | qa, dev-2-reviewer | S07, S08, S11 |
| `documentation-impact` | Documentation Impact | documentation | S10, S11 |
| `merge-decision` | Merge Decision | tech-lead, orchestrator | S07, S09, S10 |

### Release

| Phase ID | Phase | Primary Agents | Required Skills |
|---|---|---|---|
| `readiness-assessment` | Readiness Assessment | tech-lead, qa | S07, S08, S09 |
| `artifact-packaging` | Artifact Packaging | dev-2-reviewer, tech-lead | S10, S11, S12 |
| `candidate-validation` | Candidate Validation | qa, dev-2-reviewer | S07, S08, S11 |
| `deployment-execution` | Deployment Execution | orchestrator, tech-lead | S11, S08, S12 |
| `communication-and-post-release` | Release Communication and Post-release | documentation, product-owner, context-agent | S02, S11 |

### Coverage of Earlier Groupings

The Fix Bug, Investigate, Refactor, and Release tables previously carried coarser display-name
groupings. Two phases that those groupings folded away are now explicit, because the workflow
specifications and `workflows/workflow-engine.md` both declare them as owned states:
`safety-net-establishment` in Refactor and `candidate-validation` in Release. Investigate and
Research now separate the decision (`recommendation`, `recommendation-draft`) from its
distribution (`publication`, `findings-publication`), matching those state machines. No skill
requirement was removed: every code the earlier groupings declared still appears against the
phase that carries the work.

## Decision Rules

- Assign at least one Primary-skilled agent for every critical phase.
- If no Primary skill exists in current ownership, add a Secondary reviewer.
- For high-risk changes, include Security and Testing as mandatory review skills.
- For incident-driven work, include Logging and Error Handling in all stages.

## Maintenance

- Reassess matrix quarterly or after major architecture changes.
- Update skill ratings when role responsibilities change.
- Record deviations in release retrospective artifacts.
