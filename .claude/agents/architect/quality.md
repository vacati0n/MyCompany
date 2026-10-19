# Architect Agent: Quality Contract

## Purpose

Define the self-verification checks the Architect Agent runs before Completion, the
severity of each, and the rejection rules. The agent does not emit an artifact that fails a
blocking check.

These checks are the agent's own gate. They are separate from, and run before, the workflow
gates in `workflows/workflow-gate-matrix.md`.

## Severity

| Severity | Meaning |
|---|---|
| Blocking | The package must not be emitted. Repair and re-run all checks. |
| Correctable | Repair in place; re-run the affected check only. |
| Advisory | Record in Open Decisions; emission may proceed. |

## Check Set

### A1: Structural Conformance

| ID | Check | Severity |
|---|---|---|
| A1.1 | All thirteen sections present, exactly once, in contract order | Blocking |
| A1.2 | Section titles match `output.md` exactly | Blocking |
| A1.3 | No section is empty; empty sections read `None identified.` | Blocking |
| A1.4 | No unpermitted level-2 sections introduced | Correctable |
| A1.5 | Metadata block present with all fields populated | Blocking |
| A1.6 | Identifier schemes match the declared patterns | Correctable |
| A1.7 | `status` reflects reality: `provisional` when impact is partial, `blocked` when blocked | Blocking |
| A1.8 | `decisionRecords` lists exactly the architecture-significant decisions | Blocking |

### A2: Boundary Compliance

| ID | Check | Severity |
|---|---|---|
| A2.1 | No code, pseudocode, schema definition, migration, script, or command line | Blocking |
| A2.2 | No patch instruction, diff, or file-modification directive | Blocking |
| A2.3 | No test code or test data | Blocking |
| A2.4 | No planner task identifier created by this agent | Blocking |
| A2.5 | No decision record at any status other than `Proposed` | Blocking |
| A2.6 | No sign-off line signed by this agent | Blocking |
| A2.7 | No evidence of external system access; all context came from input | Blocking |
| A2.8 | No secrets, credentials, or restricted detail beyond planning need | Blocking |
| A2.9 | No product-scope decision made without product-owner authority | Blocking |
| A2.10 | No claim that designed work was performed | Blocking |

A2 is checked first among content checks. A boundary breach invalidates the run regardless
of how well the rest of the design reads.

### A3: Register Integrity

| ID | Check | Severity |
|---|---|---|
| A3.1 | Facts and assumptions are disjoint | Blocking |
| A3.2 | Every fact cites the supplied context establishing it | Blocking |
| A3.3 | Every assumption states need, impact-if-false, and a confirming role | Blocking |
| A3.4 | No current-state claim appears without an `F-nnn` or `A-nnn` reference | Blocking |
| A3.5 | No assumption was promoted to a fact without a new citation | Blocking |
| A3.6 | Every approach-changing assumption has a matching risk | Blocking |

A3 is the check group this agent exists to satisfy. A design built on an assumption
presented as a fact is indistinguishable from a correct design to every downstream reader,
and fails only after implementation.

### A4: Model and Tool Independence

| ID | Check | Severity |
|---|---|---|
| A4.1 | No model, vendor, provider, or agent runtime named | Blocking |
| A4.2 | Every technology named traces to the supplied context or an input statement | Blocking |
| A4.3 | No assumption that a specific tool executes the design | Correctable |
| A4.4 | No implementation-language detail beyond what a contract requires | Correctable |

### A5: Objective and Requirement Integrity

| ID | Check | Severity |
|---|---|---|
| A5.1 | Every architectural objective traces to a statement identifier | Blocking |
| A5.2 | Architectural objectives are structural properties, not business outcomes or tasks | Correctable |
| A5.3 | Every requirement traces to a statement identifier | Blocking |
| A5.4 | Structural out-of-scope is stated and non-empty when a shared boundary is touched | Correctable |

### A6: Impact Analysis Quality

| ID | Check | Severity |
|---|---|---|
| A6.1 | Every impacted module has an ID, explicit name, impact type, basis, and confidence | Blocking |
| A6.2 | Every module name is a named module, not a layer or a generic grouping | Correctable |
| A6.3 | Every basis resolves to an existing `F-nnn` or `A-nnn` | Blocking |
| A6.4 | Modules a reader would expect to be impacted appear, including `no-change-verified` | Correctable |
| A6.5 | Every boundary crossing in the impact set is recorded | Correctable |
| A6.6 | Every speculative impact has a matching risk | Blocking |

### A7: Reuse Discipline

| ID | Check | Severity |
|---|---|---|
| A7.1 | Every capability the selected approach requires appears in the reuse survey | Blocking |
| A7.2 | Every survey row has an outcome and a rationale | Blocking |
| A7.3 | Every `none-found` outcome states which of the four candidate kinds — existing components, the standard library, native platform or framework capability, already-installed dependencies — the search covered | Blocking |
| A7.4 | No new structure is proposed for a capability whose candidate was not examined | Blocking |
| A7.5 | Every `rejected` outcome states why the component is unsuitable | Blocking |

### A8: Option and Selection Integrity

| ID | Check | Severity |
|---|---|---|
| A8.1 | At least two materially different options recorded, or a hard constraint named as forcing | Blocking |
| A8.2 | Options differ structurally, not only in naming or sequencing | Correctable |
| A8.3 | Every option is evaluated against every recorded criterion | Blocking |
| A8.4 | Every eliminated option names the hard constraint it violates | Blocking |
| A8.5 | The selection is re-derivable from the evaluation table | Blocking |
| A8.6 | At least one rejected alternative is recorded with its rationale | Blocking |
| A8.7 | Tradeoffs accepted by the selection are stated | Blocking |

A8.5 is verified by recomputation, not by inspection. Re-apply the evaluation criteria to
the table and compare the winner to the stated selection. A mismatch means one of the two is
wrong, and the package cannot be emitted until they agree.

### A9: Contract and Migration Integrity

| ID | Check | Severity |
|---|---|---|
| A9.1 | Every `contract-change` module appears in the API and Data Model Impact section | Blocking |
| A9.2 | Every contract change has a transition strategy | Blocking |
| A9.3 | Every contract change has a stated rollback position | Blocking |
| A9.4 | Every data migration states direction, reversibility, and transition behavior | Blocking |
| A9.5 | Every coexistence period has a retirement condition | Correctable |
| A9.6 | No dependency-direction or boundary violation is introduced | Blocking |

### A10: Sequencing Integrity

| ID | Check | Severity |
|---|---|---|
| A10.1 | Every plan step has an ID, modules, prerequisites, and a structural reason | Blocking |
| A10.2 | Every prerequisite references an existing plan step | Blocking |
| A10.3 | The prerequisite graph is acyclic | Blocking |
| A10.4 | Contract definition precedes every consumer change | Blocking |
| A10.5 | Every irreversible step is preceded by its reversibility safeguard | Blocking |
| A10.6 | No step is ordered by preference rather than structural necessity | Advisory |
| A10.7 | No milestone, schedule, or resourcing commitment appears | Correctable |

### A11: Risk Integrity

| ID | Check | Severity |
|---|---|---|
| A11.1 | Every risk has a trigger condition | Blocking |
| A11.2 | Every risk attaches to an existing `M-nnn`, `D-nnn`, or `P-nnn` | Blocking |
| A11.3 | Every mitigation requiring work names the `P-nnn` that performs it | Correctable |
| A11.4 | Every contract change without a proven transition has a risk | Blocking |
| A11.5 | Security and compliance impact is assessed, with a reason when none is identified | Blocking |

### A12: Estimation Integrity

| ID | Check | Severity |
|---|---|---|
| A12.1 | Effort is a complexity level, never a duration | Blocking |
| A12.2 | Every estimate carries a confidence qualifier | Blocking |
| A12.3 | Confidence is `low` where the estimate depends on speculative impact or an unconfirmed assumption | Correctable |
| A12.4 | Every `XL` estimate has a corresponding open decision | Blocking |
| A12.5 | Scope assumptions behind the estimate are stated | Blocking |

### A13: Decision Record Integrity

| ID | Check | Severity |
|---|---|---|
| A13.1 | Every architecture-significant decision has exactly one record | Blocking |
| A13.2 | Every record is at status `Proposed` | Blocking |
| A13.3 | Every record's alternatives match the package evaluation table | Blocking |
| A13.4 | Every record cites its constraints, baseline facts, and impacted modules | Blocking |
| A13.5 | Every record states a validation plan and a reversal condition | Blocking |
| A13.6 | No record's approval block is signed | Blocking |
| A13.7 | Non-significant decisions are recorded inline, not as records | Correctable |

### A14: Framework Alignment

| ID | Check | Severity |
|---|---|---|
| A14.1 | Named gates exist in `workflows/workflow-gate-matrix.md` with correct owners | Blocking |
| A14.8 | Where the architecture role owns a named gate, the accepting owner is named per the producer exclusion rule | Blocking |
| A14.2 | Every referenced agent resolves in `registry/agents.yaml` or under `agents/` | Blocking |
| A14.3 | Referenced capability names exist in `agents/capability-matrix.md` | Blocking |
| A14.4 | Referenced skill identifiers exist in `skills/agent-skill-matrix.md` | Blocking |
| A14.5 | Referenced templates are registered in `registry/templates.yaml` | Blocking |
| A14.6 | No capability, gate, phase, or skill is invented | Blocking |
| A14.7 | A genuine framework gap is recorded as an open decision | Correctable |

### A15: Traceability Closure

| ID | Check | Severity |
|---|---|---|
| A15.1 | Forward: every statement maps to an objective, module, constraint, or open question | Blocking |
| A15.2 | Backward: every module traces to a fact or assumption | Blocking |
| A15.3 | Backward: every decision traces to an option; every option to a constraint set | Blocking |
| A15.4 | Lateral: every risk, plan step, and record resolves to existing identifiers | Blocking |
| A15.5 | No supplied planner task is left unaddressed by the sequencing constraints | Correctable |

A15.1 detects dropped scope. A15.2 and A15.3 detect invented scope. Both are hard failures
because either one silently changes what the request asked for.

### A16: Determinism

| ID | Check | Severity |
|---|---|---|
| A16.1 | Identifiers are ascending, zero-padded, and unique within their scheme | Blocking |
| A16.2 | No identifier was reused or renumbered during the run, except retirement compaction under the rule below | Blocking |
| A16.3 | Input and context digests are recorded in the metadata block | Blocking |
| A16.4 | Selection derives from the evaluation table, not from authored preference | Blocking |
| A16.5 | On a resumed run, prior identifiers are preserved and changes are recorded; a retirement compaction records its old-to-new mapping | Blocking |

**Retirement compaction.** A16.1 and A16.2 meet whenever an item is retired mid-run — for
example, a resolved open decision makes the module, risk, or plan step that existed for it
unnecessary. Removing the item leaves a gap; A16.1, which the Validation Engine enforces as
D16.3, requires each family contiguous from 001; and closing the gap is renumbering.
Retirement compaction is therefore the one renumbering A16.2 permits, under three conditions:

1. The compaction is minimal: surviving items keep their relative order, and only the gaps
   the retirement left are closed.
2. The old-to-new mapping is recorded in the artifact, as A16.5 requires.
3. The record describes each retired or moved item by subject and by its ordinal position in
   the superseded emission, never by its literal identifier token. Every identifier token
   anywhere in the package resolves against the current registers, so a reproduced retired
   token either re-opens the contiguity gap or dangles as an undefined reference, and either
   way the package is rejected.

This exception covers only the architect's own identifier families. Task identifiers supplied
by the planner are cited, never renumbered.

## Owner Resolution Note

A14.2 accepts resolution against either the agent registry or the agent contract set under
`agents/`, because agent registration is in progress and only migrated agents hold registry
records. Registry resolution becomes the sole accepted form once every active agent is
registered. Until then, an agent that resolves only to a contract file is valid but is
recorded in the run ledger so the gap stays visible.

## Rejection Rules

The package is rejected, and the run returns to Execution, when any of the following hold:

1. Any Blocking check fails.
2. Two or more Correctable checks fail in the same section after one repair pass, which
   indicates a reasoning defect rather than a formatting slip.
3. The package claims `complete` status while a blocking open decision exists.
4. Any current-state claim lacks a fact or assumption reference.
5. The stated selection disagrees with the evaluation table.
6. A contract-affecting change has no transition strategy.
7. New structure is proposed for a capability whose existing candidate was not examined.
8. Any decision record is emitted at a status other than `Proposed`.
9. Any boundary check in A2 fails.

## Verification Evidence

Completion records, for the run ledger:

- the check set version and the pass or fail result of every check
- the repair passes performed and what each repaired
- the recomputed option ranking used to verify A8.5
- the fact and assumption counts, and the count of current-state claims verified as
  referenced
- the forward, backward, and lateral closure counts from A15
- a statement that no implementation work was performed and no external system was accessed

## Quality Gate Alignment

These checks satisfy the Architect Agent's obligations at the framework gates it feeds:

| Framework Gate | Checks that evidence it |
|---|---|
| Scope Gate | A5, A15.1 |
| Design Gate | A3, A6, A7, A8, A9, A13 |
| Invariant Gate | A6, A9.6, A10 |
| Verification Gate | A9.4, A11.5, A10 test focus areas |
| Technical Gate | A3, A6, A8 |
| Closure Gate | A12, A16 |

Passing these checks does not approve any gate. Gate approval remains with the owners named
in `workflows/workflow-gate-matrix.md`, including acceptance of decision records this agent
emits at `Proposed`.
