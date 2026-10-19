# Planner Agent: Quality Contract

## Purpose

Define the self-verification checks the Planner Agent runs before Completion, the
severity of each, and the rejection rules. The agent does not emit an artifact that fails
a blocking check.

These checks are the agent's own gate. They are separate from, and run before, the
workflow gates in `workflows/workflow-gate-matrix.md`.

## Severity

| Severity | Meaning |
|---|---|
| Blocking | The plan must not be emitted. Repair and re-run all checks. |
| Correctable | Repair in place; re-run the affected check only. |
| Advisory | Record in Open Questions; emission may proceed. |

## Check Set

### Q1: Structural Conformance

| ID | Check | Severity |
|---|---|---|
| Q1.1 | All twelve mandatory sections present, exactly once, in contract order | Blocking |
| Q1.2 | Section titles match `output.md` exactly | Blocking |
| Q1.3 | No section is empty; empty sections read `None identified.` | Blocking |
| Q1.4 | No unpermitted level-2 sections introduced | Correctable |
| Q1.5 | Metadata block present with all fields populated | Blocking |
| Q1.6 | Identifier schemes match `S-nnn`, `A-nnn`, `R-nnn`, `T-nnn`, `Q-nnn` | Correctable |
| Q1.7 | `status` reflects reality: `partial` when phased, `blocked` when blocked | Blocking |

### Q2: Boundary Compliance

| ID | Check | Severity |
|---|---|---|
| Q2.1 | No code, pseudocode, schema, migration, script, or command line | Blocking |
| Q2.2 | No patch instruction, diff, or file-modification directive | Blocking |
| Q2.3 | No test code or test data | Blocking |
| Q2.4 | No architecture decision presented as settled where `architect` holds authority | Blocking |
| Q2.5 | No evidence of external system access; all ticket content came from input | Blocking |
| Q2.6 | No secrets, credentials, tokens, or restricted content | Blocking |
| Q2.7 | No claim that a planned task was performed | Blocking |
| Q2.8 | Task descriptions state completion conditions, not implementation approach | Correctable |

Q2 is checked first among content checks. A boundary breach invalidates the run regardless
of how well the rest of the plan reads.

### Q3: Model and Tool Independence

| ID | Check | Severity |
|---|---|---|
| Q3.1 | No model, vendor, or provider named anywhere in the artifact | Blocking |
| Q3.2 | No language, framework, library, or runtime named unless present in the inputs or loaded context | Correctable |
| Q3.3 | No assumption that a specific tool executes the plan | Correctable |
| Q3.4 | Every named technology traces to a statement identifier | Correctable |

### Q4: Objective Integrity

| ID | Check | Severity |
|---|---|---|
| Q4.1 | Business and technical objectives are disjoint | Correctable |
| Q4.2 | Every business objective traces to a statement identifier | Blocking |
| Q4.3 | Every technical objective traces to a business objective or an assumption | Blocking |
| Q4.4 | Business objectives express outcomes, not activities | Correctable |
| Q4.5 | Every objective states how it is measured or verified | Correctable |

### Q5: Task Quality

| ID | Check | Severity |
|---|---|---|
| Q5.1 | Every task has identifier, owner, complexity, confidence, dependencies, trace, status, description, and acceptance criteria | Blocking |
| Q5.2 | Every `Owner` resolves to a registered record in `registry/agents.yaml` or to an agent contract under `agents/` | Blocking |
| Q5.3 | Exactly one owner per task | Blocking |
| Q5.4 | Every task satisfies the five R6 validity conditions | Blocking |
| Q5.5 | No task title joins two independently verifiable outcomes | Correctable |
| Q5.6 | Every task acceptance criterion is verifiable by an agent other than the planner | Blocking |
| Q5.7 | Every `XL` task carries an open question explaining why it was not decomposed | Blocking |
| Q5.8 | Every `blocked` task names its blocking open question | Blocking |
| Q5.9 | Confidence is `low` wherever the task depends on an unconfirmed assumption | Correctable |
| Q5.10 | Validation and documentation work exists as separate tasks with correct owners | Correctable |

### Q6: Dependency and Order Integrity

| ID | Check | Severity |
|---|---|---|
| Q6.1 | Dependency graph is acyclic | Blocking |
| Q6.2 | Every edge references existing tasks, or one task and a named external party | Blocking |
| Q6.3 | Every edge carries a type and a justification | Blocking |
| Q6.4 | Implementation order recomputes exactly from the edge table | Blocking |
| Q6.5 | Wave 1 contains exactly the tasks with no unmet dependency | Blocking |
| Q6.6 | Ties within a wave are ordered by ascending identifier | Correctable |
| Q6.7 | Every external dependency has a named responsible party and a matching risk | Blocking |
| Q6.8 | No edge encodes preference rather than necessity | Advisory |

Q6.4 is verified by recomputation, not by inspection. Recompute the topological order from
the edge table and compare it to the stated order. A mismatch means one of the two is
wrong, and the plan cannot be emitted until they agree.

### Q7: Assumption and Risk Integrity

| ID | Check | Severity |
|---|---|---|
| Q7.1 | Every assumption has basis, impact-if-false, and a confirming role | Blocking |
| Q7.2 | Every assumption whose failure changes task structure has a matching risk | Blocking |
| Q7.3 | Every risk has a trigger condition | Blocking |
| Q7.4 | Every risk attaches to task identifiers or is marked `plan-wide` | Blocking |
| Q7.5 | Every mitigation requiring work names the task that performs it | Correctable |
| Q7.6 | No inference appears in the artifact without a registered assumption | Blocking |

### Q8: Traceability Closure

| ID | Check | Severity |
|---|---|---|
| Q8.1 | Forward: every in-scope statement maps to a task, assumption, or open question | Blocking |
| Q8.2 | Backward: every task traces to a statement or assumption | Blocking |
| Q8.3 | Lateral: every risk, assumption, and edge resolves to existing identifiers | Blocking |
| Q8.4 | No in-scope item lacks a task | Blocking |
| Q8.5 | No scope item appears in two subsections | Correctable |

Q8.1 detects dropped scope. Q8.2 detects invented scope. Both are hard failures because
either one silently changes what the requester asked for.

### Q9: Framework Alignment

| ID | Check | Severity |
|---|---|---|
| Q9.1 | Suggested workflow is a registered workflow identifier | Blocking |
| Q9.2 | Every task maps to exactly one workflow phase | Correctable |
| Q9.3 | Named gates exist in `workflows/workflow-gate-matrix.md` with correct owners | Blocking |
| Q9.4 | Capability names exist in `agents/capability-matrix.md` | Blocking |
| Q9.5 | Skill identifiers exist in `skills/agent-skill-matrix.md` | Blocking |
| Q9.6 | No capability, gate, phase, or skill is invented | Blocking |
| Q9.7 | A genuine framework gap is recorded as an open question | Correctable |

### Q10: Acceptance and Closure

| ID | Check | Severity |
|---|---|---|
| Q10.1 | Every plan acceptance criterion traces to a business objective | Blocking |
| Q10.2 | Every acceptance criterion names its verifying evidence | Blocking |
| Q10.3 | Definition of Done items are objectively checkable | Blocking |
| Q10.4 | No Definition of Done item depends on planner judgment after handoff | Correctable |
| Q10.5 | Open questions carry owners and blocking status | Blocking |

### Q11: Determinism

| ID | Check | Severity |
|---|---|---|
| Q11.1 | Identifiers are ascending, zero-padded, and unique | Blocking |
| Q11.2 | No identifier was reused or renumbered during the run, except retirement compaction under the rule below | Blocking |
| Q11.3 | Input and context digests are recorded in the metadata block | Blocking |
| Q11.4 | Order derives from the graph, not from authored sequence | Blocking |
| Q11.5 | On a resumed run, prior identifiers are preserved and changes are recorded; a retirement compaction records its old-to-new mapping | Blocking |

**Retirement compaction.** Q11.1 and Q11.2 meet whenever an item is retired mid-run — for
example, a resolved open question makes the task that existed to ask it, or an assumption it
rested on, unnecessary. Removing the item leaves a gap; Q11.1, which the Validation Engine
enforces as V10.2, requires the family contiguous from 001; and closing the gap is
renumbering. Retirement compaction is therefore the one renumbering Q11.2 permits, under
three conditions:

1. The compaction is minimal: surviving items keep their relative order, and only the gaps
   the retirement left are closed.
2. The old-to-new mapping is recorded in the artifact, as Q11.5 requires.
3. The record describes each retired or moved item by subject and by its ordinal position in
   the superseded emission, never by its literal identifier token. Every `T-nnn`, `A-nnn`,
   `S-nnn`, `R-nnn`, or `Q-nnn` token anywhere in the artifact resolves against the current
   registers, so a reproduced retired token either re-opens the contiguity gap or dangles as
   an undefined reference, and either way the artifact is rejected.

After compaction, a number that was reassigned names only its new subject. The mapping record
is what keeps the two emissions readable side by side.

## Owner Resolution Note

Q5.2 accepts resolution against either the agent registry or the agent contract set under
`agents/`, because agent registration is in progress and only migrated agents hold
registry records. Registry resolution becomes the sole accepted form once every active
agent is registered. Until then, an owner that resolves only to a contract file is valid
but is recorded in the run ledger so the gap stays visible.

## Rejection Rules

The artifact is rejected, and the run returns to Execution, when any of the following hold:

1. Any Blocking check fails.
2. Two or more Correctable checks fail in the same section after one repair pass, which
   indicates a reasoning defect rather than a formatting slip.
3. The plan claims `complete` status while an unresolved blocking open question exists.
4. The plan contains a dependency cycle.
5. The stated implementation order disagrees with the dependency graph.
6. Any in-scope statement has no corresponding task, assumption, or open question.
7. Any task has no owner, or names an owner that does not resolve under Q5.2.
8. Any boundary check in Q2 fails.

## Verification Evidence

Completion records, for the run ledger:

- the check set version and the pass or fail result of every check
- the repair passes performed and what each repaired
- the recomputed topological order used to verify Q6.4
- the forward, backward, and lateral closure counts from Q8
- a statement that no implementation work was performed and no external system was accessed

## Quality Gate Alignment

These checks satisfy the Planner Agent's obligations at the framework gates it feeds:

| Framework Gate | Checks that evidence it |
|---|---|
| Scope Gate | Q4, Q8, Q10.1 |
| Design Gate | Q2.4, Q6, Q9 |
| Verification Gate | Q5.6, Q10.2 |
| Closure Gate | Q10.3, Q11 |

Passing these checks does not approve any gate. Gate approval remains with the owners
named in `workflows/workflow-gate-matrix.md`.
