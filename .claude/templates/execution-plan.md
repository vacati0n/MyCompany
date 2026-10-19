# Template: Execution Plan

## Usage

Canonical artifact template for `execution-plan.md`, produced by agent `planner`.
The binding structural and semantic contract is `agents/planner/output.md`.
Where this template and that contract differ, the contract governs.

Section titles, section order, and identifier schemes are fixed. Sections are never
omitted. A section with nothing to report reads `None identified.`

Identifier schemes: statements `S-nnn`, assumptions `A-nnn`, risks `R-nnn`,
tasks `T-nnn`, open questions `Q-nnn`. All zero-padded to three digits and ascending.

---

```yaml
plan:
  planId:
  sourceInputs:
    - type:          # feature-request | user-story | jira-ticket | epic | product-requirement
      reference:     # supplied reference, or "inline"
  producedBy: planner
  agentVersion:
  schemaVersion: 1.0.0
  status:            # complete | partial | blocked
  inputDigest:
  contextDigest:
```

## Executive Summary

<!-- Four to eight sentences: what is being built and for whom; the outcome that defines
success; task count and execution waves; the highest-impact risk; plan status and, when
not complete, what blocks it. Introduces nothing that does not appear in a later section. -->

## Business Objectives

<!-- Outcomes, not activities. Each states who receives the outcome and how it is
measured, and traces to a statement identifier. -->

-

## Technical Objectives

<!-- System properties, not tasks. Each states how it is verified and traces to a business
objective or to an assumption explaining why it stands alone. Disjoint from the section
above. -->

-

## Scope

### In Scope

<!-- Each item maps to at least one task. -->

-

### Out of Scope

<!-- Each item states why it is excluded. -->

-

### Deferred

<!-- Each item states the condition that would bring it into scope. -->

-

## Assumptions

| ID | Assumption | Basis | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | | | | |

## Risks

| ID | Class | Trigger | Impact | Likelihood | Affects | Mitigation | Owner |
|---|---|---|---|---|---|---|---|
| R-001 | | | | | | | |

<!-- Class: requirement | technical | dependency | security | operational | delivery.
Affects: task identifiers, or "plan-wide". A risk without a trigger or an attachment does
not appear. -->

## Task Breakdown

<!-- One subsection per task, ascending by identifier. Every field is present on every
task. Descriptions state the completion condition, never the implementation approach. -->

### T-001 <imperative title>

- Owner:
- Complexity:            <!-- XS | S | M | L | XL (confidence: high | medium | low) -->
- Depends on:            <!-- task identifiers, or "none" -->
- Traces to:             <!-- statement or assumption identifiers -->
- Status:                <!-- ready | blocked | assumption-dependent -->
- Description:
- Acceptance Criteria:
  -
  -
- Gate:                  <!-- gate name from workflows/workflow-gate-matrix.md, or "none" -->

## Dependencies

### 8.1 Dependency Edges

| From | To | Type | Justification |
|---|---|---|---|
| | | | |

<!-- Type: produces-consumes | decision-gate | contract | verification | external |
policy-gate. Preference is not a dependency. The graph must be acyclic. -->

### 8.2 External Dependencies

| Responsible party | What is needed | Blocks |
|---|---|---|
| | | |

### 8.3 Implementation Order

<!-- Topological order over 8.1, presented as waves. Must be recomputable from 8.1 alone.
Ties within a wave are ordered by ascending identifier. Waves express what may run in
parallel, not what must. -->

- Wave 1:
- Wave 2:

## Suggested Workflow

Selected workflow:

Selected because:

| Phase | Tasks |
|---|---|
| | |

| Gate | Required owners |
|---|---|
| | |

<!-- Workflow is selected from registered workflows only. This section recommends; it does
not start a workflow. -->

## Required Capabilities

### 10.1 Agent Capabilities

| Capability | Tasks | Owning agent | Proficiency |
|---|---|---|---|
| | | | |

### 10.2 Required Skills

| Skill | File | Tasks | Level |
|---|---|---|---|
| | | | |

<!-- Only capability names from agents/capability-matrix.md and skill identifiers from
skills/agent-skill-matrix.md. A genuine gap is an open question, not an invention. -->

## Acceptance Criteria

<!-- Traces to business objectives, not to tasks, so that completing every task without
achieving the outcome is detectable. Each criterion names its verifying evidence. -->

1.
2.

## Definition of Done

- [ ] All plan acceptance criteria are verified with recorded evidence
- [ ] All mandatory gates are approved with owners recorded
- [ ] Task acceptance criteria are satisfied or formally waived
- [ ] Assumptions are confirmed or converted to recorded decisions
- [ ] Risks are closed or accepted with named owners
- [ ] Open questions are closed or explicitly accepted
- [ ] Documentation and release-impact notes are published
- [ ] Durable outcomes are recorded to memory per `memory/memory-governance.md`

## Open Questions

<!-- Required whenever any question exists. -->

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| Q-001 | | | | |

## Traceability Matrix

<!-- Required when status is partial or blocked. Maps every statement to the task,
assumption, or open question that covers it. -->

| Statement | Covered by |
|---|---|
| S-001 | |
