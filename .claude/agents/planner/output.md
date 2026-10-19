# Planner Agent: Output Contract

## Purpose

Define the binding structure and semantics of the Planner Agent's single deliverable,
`execution-plan.md`. The rendered form is `templates/execution-plan.md`. Where the
template and this contract differ, this contract governs and the template is corrected.

## Artifact

| Property | Value |
|---|---|
| Filename | `execution-plan.md` |
| Format | GitHub-flavored Markdown |
| Template | `templates/execution-plan.md` |
| Producer | `planner` |
| Consumers | `orchestrator`, `architect`, `tech-lead`, implementation agents, `qa` |
| Schema version | 1.0.0 |

## Structural Rules

1. The twelve mandatory sections appear exactly once each, in the order below, as level-2
   headings with the exact titles given.
2. No section is omitted. A section with nothing to report contains `None identified.`
   and, where the absence is meaningful, one line explaining why.
3. No additional level-2 sections are introduced. Supplementary material belongs in the
   Metadata block or in an appendix beneath Definition of Done.
4. Identifier schemes are fixed: statements `S-nnn`, assumptions `A-nnn`, risks `R-nnn`,
   tasks `T-nnn`, open questions `Q-nnn`. Numbering is zero-padded to three digits and
   ascending.
5. Cross-references use bare identifiers, so downstream agents can resolve them by exact
   string match.
6. Agent references use registered agent identifiers exactly as they appear in
   `registry/agents.yaml` and `agents/`.

## Metadata Block

The artifact opens with a metadata block before the Executive Summary.

```yaml
plan:
  planId: <stable identifier for this plan>
  sourceInputs:
    - type: <feature-request | user-story | jira-ticket | epic | product-requirement>
      reference: <supplied reference or "inline">
  producedBy: planner
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: <complete | partial | blocked>
  inputDigest: <digest of the frozen input snapshot>
  contextDigest: <digest of the loaded context snapshot>
```

`status` is `partial` when the phased fallback was applied, `blocked` when a blocking open
question prevents decomposition of in-scope work, and `complete` otherwise.

## Section Contracts

### 1. Executive Summary

**Purpose.** Let a downstream agent decide in one read whether this plan concerns it.

**Contains.** What is being built and for whom; the outcome that defines success; the
shape of the work in task count and execution waves; the highest-impact risk; the plan
status and, when not `complete`, what blocks it.

**Length.** Four to eight sentences.

**Prohibited.** Implementation approach, technology selection, and any content that does
not also appear in a later section. The summary summarizes; it never introduces.

### 2. Business Objectives

**Purpose.** State why the work is worth doing, in terms that remain true regardless of
implementation.

**Format.** A list. Each objective states the outcome, who receives it, and how it is
measured.

**Rules.** Each objective traces to at least one statement identifier. Objectives are
outcomes, not activities: "reduce checkout abandonment" is an objective, "add a
preferences endpoint" is not. No objective duplicates a technical objective.

### 3. Technical Objectives

**Purpose.** State what the system must become for the business objectives to hold.

**Format.** A list. Each objective states the required system property and how it is
verified.

**Rules.** Each objective traces to a business objective identifier or to an assumption
explaining why it stands alone. Objectives are properties, not tasks. Disjoint from
Business Objectives.

### 4. Scope

**Purpose.** Fix the boundary so downstream agents do not renegotiate it.

**Format.** Three labeled subsections: `In Scope`, `Out of Scope`, `Deferred`.

**Rules.** Every in-scope item maps to at least one task. Every out-of-scope item states
why it is excluded. Every deferred item states the condition that would bring it into
scope. An item never appears in two subsections.

### 5. Assumptions

**Purpose.** Make every gap-filling inference visible and falsifiable.

**Format.** A table.

| Column | Content |
|---|---|
| ID | `A-nnn` |
| Assumption | What is being assumed |
| Basis | The statement it fills a gap for, or `plan-wide` |
| Impact if false | What changes in the plan |
| Confirmed by | The agent or role that can confirm it |

**Rules.** No unregistered inference may appear anywhere in the artifact. Any assumption
whose failure changes task structure has a matching risk entry.

### 6. Risks

**Purpose.** Expose what can invalidate the plan, attached to where it bites.

**Format.** A table.

| Column | Content |
|---|---|
| ID | `R-nnn` |
| Class | requirement, technical, dependency, security, operational, delivery |
| Trigger | The condition under which the risk materializes |
| Impact | Consequence for scope, sequence, or acceptance |
| Likelihood | high, medium, low |
| Affects | Task identifiers, or `plan-wide` |
| Mitigation | The response; if it requires work, the task identifier that performs it |
| Owner | Accountable agent |

**Rules.** A risk without a trigger, or without an attachment, does not appear. Every
external dependency has a corresponding risk.

### 7. Task Breakdown

**Purpose.** The executable core of the plan.

**Format.** One level-3 subsection per task, in ascending identifier order.

```markdown
### T-001 <imperative title>

- Owner: <registered agent identifier>
- Complexity: <XS | S | M | L | XL> (confidence: <high | medium | low>)
- Depends on: <task identifiers, or "none">
- Traces to: <statement or assumption identifiers>
- Status: <ready | blocked | assumption-dependent>
- Description: <what must be true when this task is done, not how to do it>
- Acceptance Criteria:
  - <verifiable condition>
  - <verifiable condition>
- Gate: <gate name from workflows/workflow-gate-matrix.md, or "none">
```

**Rules.** Every field is present on every task. `Owner` names exactly one agent. Titles
are imperative and describe an outcome. Descriptions state the completion condition, never
the implementation approach. A task with an `XL` complexity is accompanied by an open
question explaining why it was not decomposed further. A `blocked` task names the blocking
open question identifier.

**Prohibited.** Code, pseudocode, schema definitions, command lines, file diffs, and any
directive that performs rather than describes the work.

### 8. Dependencies

**Purpose.** Make the ordering auditable and recomputable.

**Format.** Three parts.

**8.1 Dependency Edges** — a table.

| Column | Content |
|---|---|
| From | Predecessor task identifier, or external party |
| To | Successor task identifier |
| Type | produces-consumes, decision-gate, contract, verification, external, policy-gate |
| Justification | The prerequisite relationship that creates the edge |

**8.2 External Dependencies** — a table of prerequisites outside the plan's authority,
each with the responsible party, what is needed, and the blocked task identifiers.

**8.3 Implementation Order** — the topological order over 8.1, presented as execution
waves. Wave 1 contains all tasks with no unmet dependency; each later wave contains tasks
whose dependencies all resolve in earlier waves. Ties inside a wave are listed by ascending
identifier.

**Rules.** The graph is acyclic. Every edge references tasks that exist. The order in 8.3
must be recomputable from 8.1 alone; any discrepancy is a defect, not a preference. Waves
express what may run in parallel, not what must.

### 9. Suggested Workflow

**Purpose.** Route the plan into the framework without executing it.

**Format.** The selected workflow identifier, why it was selected, a phase-to-task mapping
table, and the gates from `workflows/workflow-gate-matrix.md` with their required owners.

**Rules.** The workflow is selected from registered workflows only. Every task maps to
exactly one phase. A task that maps to no phase is recorded as an open question describing
the framework gap. This section recommends; the Planner Agent never starts a workflow.

### 10. Required Capabilities

**Purpose.** Tell the runtime what must be loadable before execution begins.

**Format.** Two tables.

**10.1 Agent Capabilities** — capability name, the tasks requiring it, the owning agent,
and its proficiency from `agents/capability-matrix.md`.

**10.2 Required Skills** — skill identifier and file from `skills/agent-skill-matrix.md`,
the tasks requiring it, and whether it is Primary, Secondary, or Advisory for the run.

**Rules.** Only existing capability names and skill identifiers are used. A genuinely
missing capability is recorded as an open question, never invented here.

### 11. Acceptance Criteria

**Purpose.** Define what makes the requirement satisfied, independently of task completion.

**Format.** A numbered list, each criterion with the business objective it verifies and the
evidence that demonstrates it.

**Rules.** Criteria trace to business objectives, not to tasks, so that completing every
task without achieving the outcome is detectable. Each criterion is verifiable by
inspection, demonstration, or recorded evidence, and is judgeable by an agent other than
the planner. A criterion that cannot be verified at all becomes an open question.

### 12. Definition of Done

**Purpose.** State the closure conditions for the plan as a unit.

**Format.** A checklist covering, at minimum: every acceptance criterion verified; every
mandatory gate approved with owners recorded; task-level acceptance satisfied or formally
waived; open questions closed or explicitly accepted; residual risks accepted with owners;
documentation and release-impact notes updated; durable outcomes recorded to memory per
`memory/memory-governance.md`.

**Rules.** Every item is objectively checkable. No item depends on planner judgment after
handoff.

## Appendix Sections

The following may appear after Definition of Done, as level-2 headings, and are the only
permitted additions.

- `Open Questions` — table of `Q-nnn`, question, blocking or non-blocking, owning agent,
  and affected task identifiers. Required whenever any question exists.
- `Traceability Matrix` — the R13 closure evidence mapping statements to tasks,
  assumptions, and questions. Required when the run is `partial` or `blocked`.

## Prohibited Content

The artifact must never contain:

- production code, configuration, schema definitions, migrations, or scripts
- pseudocode intended for direct translation into implementation
- patch instructions, diffs, or file-modification directives
- test code or test data
- architecture decisions presented as settled when they are reserved to `architect`
- model, vendor, tool, or technology names absent from the inputs or loaded context
- secrets, credentials, tokens, or restricted ticket content
- estimates expressed in duration rather than complexity
- narrative prose in place of structured entries

## Determinism Contract

Given identical approved inputs and an identical context snapshot, two runs produce
artifacts with an identical section set, identical identifier assignments, identical
dependency edges, and an identical implementation order. Wording may differ; structure and
relationships may not.
