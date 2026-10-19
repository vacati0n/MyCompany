# Architect Agent: Output Contract

## Purpose

Define the binding structure and semantics of the Architect Agent's deliverables. The
primary artifact is the technical design package, rendered from
`templates/technical-design.md`. The conditional artifact is the architecture decision
record, rendered from `templates/architecture-decision-record.md`.

Where a template and this contract differ, this contract governs and the template is
corrected.

## Artifacts

| Property | Technical Design | Decision Record |
|---|---|---|
| Filename | `technical-design.md` | `architecture-decision-record.md` |
| Template | `templates/technical-design.md` | `templates/architecture-decision-record.md` |
| Required | Always | One per architecture-significant decision |
| Emitted status | n/a | `Proposed` only |
| Producer | `architect` | `architect` |
| Consumers | `tech-lead`, implementation agents, `dev-2-reviewer`, `qa`, `planner` | Design Gate owners, `documentation` |
| Schema version | 1.0.0 | 1.0.0 |

## Structural Rules

1. The thirteen sections appear exactly once each, in the order below, as level-2 headings
   with the exact titles given.
2. No section is omitted. A section with nothing to report contains `None identified.` and,
   where the absence is meaningful, one line explaining why.
3. No additional level-2 sections are introduced. Supplementary material belongs in the
   Metadata block or in an appendix beneath Sign-off.
4. Identifier schemes are fixed: statements `S-nnn`, facts `F-nnn`, assumptions `A-nnn`,
   constraints `C-nnn`, modules `M-nnn`, options `O-nnn`, decisions `D-nnn`, risks `R-nnn`,
   plan steps `P-nnn`, open questions `Q-nnn`. All zero-padded to three digits and ascending.
5. Cross-references use bare identifiers, so downstream agents can resolve them by exact
   string match.
6. Planner task identifiers `T-nnn` may be referenced when an execution plan was supplied,
   and may never be created.
7. Agent references use registered agent identifiers exactly as they appear in
   `registry/agents.yaml` and `agents/`.

## Metadata Block

The package opens with a metadata block before the Objective section.

```yaml
design:
  designId: <stable identifier for this design>
  changeReference: <supplied reference or "inline">
  sourceInputs:
    - type: <change-request | business-intent | architecture-context | execution-plan>
      reference: <supplied reference or "inline">
  producedBy: architect
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: <complete | provisional | blocked>
  decisionRecords: [<D-nnn identifiers, or empty>]
  consumesPlan: <execution plan reference, or "none">
  inputDigest: <digest of the frozen input snapshot>
  contextDigest: <digest of the architecture context snapshot>
```

`status` is `provisional` when the confirmed fact set could not support full impact
analysis, `blocked` when a blocking open decision prevents approach selection, and
`complete` otherwise.

## Section Contracts

### 1. Metadata

The block above, plus author, reviewers, and last-updated fields carried from the template.

### 2. Objective

**Purpose.** State what the architecture must achieve and what it must not.

**Contains.** The desired structural outcome, the architectural objectives with their
statement traces, what is in structural scope, and what is explicitly out.

**Rules.** Architectural objectives are structural properties, not business outcomes and
not tasks. Every objective traces to a statement identifier. The out-of-scope list bounds
the impact surface and is never empty when the change touches a shared boundary.

### 3. Requirements Summary

**Purpose.** Record what the design must satisfy.

**Contains.** Functional requirements, non-functional requirements, and acceptance intent,
each traced to a statement identifier.

**Rules.** A requirement with no statement trace is invented scope. Where acceptance intent
was supplied by `planner` or `product-owner`, it is cited rather than restated.

### 4. Current-State Assumptions and Constraints

**Purpose.** Separate what is known from what is assumed, before anything is designed on
top of either.

**Format.** Three tables.

**4.1 Facts** — `F-nnn`, the property of the current system, and the supplied context that
establishes it.

**4.2 Assumptions** — `A-nnn`, what is assumed, why it is needed, what changes if it is
false, and who can confirm it.

**4.3 Constraints** — `C-nnn`, class, source, hard or negotiable, and the statement or
context it derives from.

**Rules.** Facts and assumptions are disjoint. No claim about the current system appears
anywhere in the package without an `F-nnn` or `A-nnn` reference. A fact without a citation
is an assumption; misclassifying one as the other is the failure this section exists to
prevent.

### 5. Architecture and Component Design

**Purpose.** State the impact surface and the selected approach.

**Format.** Four parts.

**5.1 Impacted Modules** — a table.

| Column | Content |
|---|---|
| ID | `M-nnn` |
| Module | Explicit module name |
| Impact type | contract-change, behavior-change, extension, dependency-change, operational-impact, no-change-verified |
| Basis | `F-nnn` or `A-nnn` establishing the impact |
| Interfaces affected | Named interfaces, or none |
| Confidence | confirmed or speculative |

**5.2 Options Considered** — the evaluation table from reasoning stage A8, one row per
option `O-nnn`, one column per criterion, with eliminated options marked and the violated
constraint named.

**5.3 Selected Approach** — the selected option, the structural change it makes, the
rationale, the highest-scoring rejected alternative and why it lost, and the tradeoffs
accepted.

**5.4 Decisions** — a table of `D-nnn`, the decision statement, whether it is
architecture-significant, and the decision record reference when one exists.

**Rules.** Every module is named explicitly; a layer name is not a module. Modules a reader
would expect to be impacted appear with `no-change-verified` rather than being omitted. The
selected approach must be re-derivable from 5.2; a selection that disagrees with the
evaluation table is a defect. At least one rejected alternative appears unless a hard
constraint forced the selection, in which case the constraint is named.

### 6. API and Data Model Impact

**Purpose.** Make contract change and migration obligations explicit.

**Contains.** API changes with the affected `M-nnn`; contract compatibility notes; schema
and data model changes; and for each contract change, the transition strategy: current
shape, target shape, compatibility approach, coexistence period, retirement condition, and
rollback position.

**Rules.** Every `contract-change` module in 5.1 appears here with a transition strategy. A
contract-affecting change without one is rejected. Data migrations state direction,
reversibility, and reader and writer behavior during transition.

### 7. Reusable Components and Reuse Rationale

**Purpose.** Show that existing structure was surveyed before new structure was proposed.

**Format.** A table.

| Column | Content |
|---|---|
| Capability | The capability the change requires |
| Candidate | The existing component considered |
| Outcome | reuse-as-is, reuse-extended, rejected, none-found |
| Rationale | Why it is suitable, why it was rejected, or where the search looked |

**Rules.** Every capability the selected approach requires appears here. A `none-found`
outcome states the search basis. New structure may be proposed only where the outcome is
`rejected` or `none-found`; new structure proposed over an unexamined component is a
boundary violation.

### 8. Operational Considerations

**Purpose.** State what the change means at runtime.

**Contains.** Logging and observability updates, error handling strategy, security
considerations, performance considerations, and deployment or operability impact, each
referencing the `M-nnn` or `C-nnn` it derives from.

**Rules.** Security and compliance impact is assessed for every change, and `None
identified.` requires a stated reason. Performance claims reference a constraint or a
quality attribute rather than asserting an expectation.

### 9. Delivery Plan

**Purpose.** State the order structural correctness requires, without producing tasks.

**Format.** A table of sequencing constraints.

| Column | Content |
|---|---|
| ID | `P-nnn` |
| Constraint | What must be structurally true |
| Modules | The `M-nnn` involved |
| Prerequisites | Prior `P-nnn`, or none |
| Reason | The structural necessity creating the order |
| Binds | Planner `T-nnn` this constrains, when an execution plan was supplied |

Followed by test strategy focus areas for `qa`, and the rollout and rollback strategy.

**Rules.** These are constraints, not tasks. No `T-nnn` identifier is created here. A step
ordered by preference rather than structural necessity is removed. Milestones and schedules
belong to `tech-lead` and do not appear.

### 10. Risks and Mitigations

**Purpose.** Expose what can invalidate the design, attached to where it bites.

**Format.** A table.

| Column | Content |
|---|---|
| ID | `R-nnn` |
| Class | structural, contract, migration, security, performance, operability, delivery |
| Trigger | The condition under which the risk materializes |
| Impact | Consequence for the approach, contract, or operation |
| Likelihood | high, medium, low |
| Affects | `M-nnn`, `D-nnn`, or `P-nnn` |
| Mitigation | The response; if it requires work, the `P-nnn` that performs it |
| Owner | Accountable agent |

**Rules.** A risk without a trigger, or without an attachment, does not appear. Every
speculative impact, every approach-changing assumption, and every contract change without a
proven transition strategy has a corresponding risk.

### 11. Estimate and Confidence

**Purpose.** State architectural effort and uncertainty without implying a commitment.

**Contains.** The overall level `XS` through `XL` with a confidence qualifier, the per-module
or per-plan-step breakdown, the scope assumptions the estimate rests on, and the factors
driving uncertainty.

**Rules.** Effort is expressed as a complexity level, never as duration; scheduling belongs
to `tech-lead`. Confidence is `low` wherever the estimate depends on a speculative
impact or an unconfirmed assumption. An `XL` estimate requires a corresponding open decision
stating what must resolve first.

### 12. Open Decisions and Escalations

**Purpose.** Record what remains unresolved and who owns it.

**Format.** A table of `Q-nnn`, the question, blocking or non-blocking, the owning agent,
the affected `M-nnn`, `D-nnn`, or `P-nnn`, and the consequence of each plausible answer.

**Rules.** Every `E-AUTHORITY` and `E-BOUNDARY` event produces an entry. Every decision
record still at `Proposed` that blocks execution appears here with its Design Gate owners.
A blocking entry sets package status to `blocked`.

### 13. Sign-off

**Purpose.** Name who must approve before execution.

**Contains.** The Design Gate owners from `workflows/workflow-gate-matrix.md`, plus product
confirmation and QA review lines when the package requires them.

**Rules.** The architect never signs its own gate. Sign-off lines are left unsigned by this
agent. Where the architecture role also appears in the gate's owner list, the producer
exclusion rule in `workflows/workflow-gate-matrix.md` applies and the accepting owner is
named explicitly.

## Decision Record Contract

One record per architecture-significant decision from reasoning stage A9.

| Template Section | Binding Content |
|---|---|
| Metadata | ADR ID matching `D-nnn`, title, date, `Status: Proposed`, owners, related work items |
| Context | The problem, the constraints `C-nnn` that bound it, and the current baseline facts `F-nnn` |
| Decision | The selected option `O-nnn`, the decision statement, and the impacted `M-nnn` |
| Alternatives Considered | Every other option from 5.2, with benefits, risks, and why not selected |
| Consequences | Expected outcomes, tradeoffs accepted, and risks introduced with `R-nnn` references |
| Validation Plan | Metrics, verification checkpoints, and the reversal condition |
| Approval | Left unsigned; acceptance belongs to the Design Gate owners |

**Rules.** Status is `Proposed` on emission, without exception. `Accepted`, `Superseded`,
and `Rejected` are set by the approving authority, not by this agent. Alternatives are
carried from the evaluation table rather than re-derived, so the record and the package
cannot diverge.

## Prohibited Content

Neither artifact may contain:

- production code, configuration, schema definitions, migrations, or scripts
- pseudocode intended for direct translation into implementation
- patch instructions, diffs, or file-modification directives
- test code or test data
- planner task identifiers created by this agent
- a decision record at any status other than `Proposed`
- current-state claims without an `F-nnn` or `A-nnn` reference
- model, vendor, or agent-runtime names
- technology names absent from the supplied context or inputs
- secrets, credentials, or restricted architecture detail beyond planning need
- effort expressed as duration
- schedules, milestones, or resourcing commitments

## Determinism Contract

Given identical approved inputs and an identical architecture context snapshot, two runs
produce artifacts with an identical section set, identical identifier assignments, an
identical impacted-module set, an identical option set, the same selected approach, and the
same decision records. Wording may differ; structure, classification, and selection may not.
