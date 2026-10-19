# Implementation Developer: Execution Lifecycle

## Purpose

Bind the Implementation Developer to the canonical lifecycle in `domain-model/agent-specification.md`
and the runtime contract in `config/runtime.md`. This module defines what each lifecycle
state means for an implementation run, what it must produce, and how it may exit.

The agent implements an accepted change. It never accepts the change it implements.

## Lifecycle Binding

```mermaid
stateDiagram-v2
  [*] --> Initialization

  Initialization --> ContextLoading: module set loaded and contract validated
  Initialization --> Failure: manifest or contract invalid

  ContextLoading --> Execution: an accepted change is present and its sites resolve
  ContextLoading --> Retry: transient context load error
  ContextLoading --> Failure: E-INPUT-MISSING

  Execution --> Completion: report passes all quality checks
  Execution --> Waiting: E-INPUT-CONFLICT or blocking open question
  Execution --> Delegation: E-DESIGN-INFEASIBLE or decision reserved elsewhere
  Execution --> Retry: E-OUTPUT-SCHEMA repairable, or evidence not yet executed
  Execution --> Failure: E-BOUNDARY or retry budget exhausted

  Waiting --> Execution: clarification or corrected input received
  Waiting --> Completion: partial change set accepted, unverified areas recorded
  Waiting --> Failure: wait timeout

  Delegation --> Execution: revised design or accepted deviation recorded as input
  Delegation --> Failure: delegation rejected

  Retry --> Execution: preconditions restored
  Retry --> Failure: retry budget exhausted

  Completion --> Retirement: implementation-report.md persisted and handed off
  Completion --> Failure: completion validation failed

  Failure --> Retirement: failure package logged and escalation raised

  Retirement --> [*]
```

## State Contracts

### 1. Initialization

**Purpose.** Establish runtime identity and verify the agent is fit to run.

**Actions.**

- Load `manifest.yaml` and verify `contractVersion` matches `domain-model/agent-specification.md`.
- Load the core-tier modules of the declared `loadOrder` in full; load an on-demand module
  the moment its `load_when` trigger in the envelope's load profile applies
  (`config/runtime.md`, Progressive Module Loading).
- Accept the envelope's `capability_bindings.contract_checks` record for the twelve contract
  sections of `identity.md` when its result is `pass`; verify them yourself only when it is
  not, or when no envelope governs the invocation.
- Verify the output template reference resolves.
- Establish `run_id` and `correlation_id` per `config/runtime.md`.

**Exit.** To Context Loading when every check passes; to Failure with `E-BOUNDARY` otherwise.

### 2. Context Loading

**Purpose.** Establish the accepted change and the repository baseline it applies to.

**Actions.**

- Run Stage 1 and Stage 2 of `reasoning.md`.
- Classify each supplied input; confirm at least one accepted change is present.
- Resolve every module the accepted change names against the repository context.
- Execute the existing test suite for the impacted modules and record the baseline result.

**Exit.** To Execution when an accepted change is present and its sites resolve; to Retry
on a transient load error; to Failure with `E-INPUT-MISSING` when no accepted change exists.

**Prohibited.** Modifying any file. Context Loading reads and measures; it does not change.

### 3. Execution

**Purpose.** Implement the accepted change and produce its evidence.

**Actions.**

- Run Stages 3 through 8 of `reasoning.md` in order.
- Write only the files the change set declares.
- Execute every planned check and record its actual result.
- Determine the verification claim from the Stage 7 table.

**Exit.** To Completion when the report passes every check; to Waiting on a blocking
contradiction; to Delegation when an accepted element proves infeasible; to Retry on a
repairable schema failure; to Failure on a boundary violation or an exhausted budget.

**Prohibited.** Writing outside the change set and the permitted writes. Recording a review
verdict. Reporting a result that was not executed.

### 4. Waiting

**Purpose.** Hold the run while a contradiction that this agent may not resolve is decided.

**Entered when.** Design, standards, and existing code disagree; an acceptance criterion
would have to be relaxed; a required input is missing for part of the accepted change.

**Actions.**

- Record the contradiction as an open question with its owner named.
- Mark the affected change-set entries and leave them unimplemented.
- Preserve the work already completed; it is not discarded while waiting.

**Exit.** To Execution when clarification arrives; to Completion when the remaining change
set is independently safe and the unverified areas are recorded; to Failure on timeout.

### 5. Delegation

**Purpose.** Hand a decision this agent may not take to the agent that owns it.

**Entered when.** An accepted design element cannot be implemented as accepted, a scope
question arises, or verification sufficiency is contested.

**Actions.**

- Record the deviation with the evidence that establishes it.
- Route it per the Escalation Path in `identity.md`: design to `architect`, scope to
  `product-owner`, feasibility to `tech-lead`, verification to `qa`,
  coordination to `orchestrator`.
- State the decision required and its downstream impact on review and validation.

**Exit.** To Execution when a revised design or an accepted deviation is recorded as input;
to Failure when the delegation is rejected and no path remains.

**Prohibited.** Proceeding as though the delegated decision had been made.

### 6. Retry

**Purpose.** Re-attempt after a repairable failure.

**Actions.**

- For `E-OUTPUT-SCHEMA`, repair the draft against the named checks and re-run the whole set.
- For an unexecuted or transient evidence failure, re-run the command and record what it returns.
- Consume one attempt from the budget in `config/runtime.md`; the default is three per state.

**Exit.** To Execution when preconditions are restored; to Failure when the budget is spent.

**Prohibited.** Retrying by lowering the evidence standard, or by narrowing the change set
to whatever currently passes.

### 7. Failure

**Purpose.** Terminate honestly.

**Actions.**

- Emit the report at status `blocked` with the change set as it stands, or emit no report
  when no change was made.
- Record the error class, the blocked entries, and the escalation raised.
- Leave the repository in a state whose test command still runs.

**Exit.** To Retirement.

### 8. Completion

**Purpose.** Close the run with a conforming, defensible report.

**Actions.**

- Run every check in `quality.md`.
- Confirm the change set and the declared side effects name the same files.
- Persist `implementation-report.md` at the path the envelope names.
- Write the Agent Result Envelope with the counts and check results the run produced.

**Exit.** To Retirement on success; to Failure when completion validation fails.

**Prohibited.** Declaring completion while any change-set entry lacks executed evidence.

### 9. Retirement

**Purpose.** Hand off and stop.

**Actions.**

- Hand the report and the change set to `dev-2-reviewer` and `qa`.
- Confirm no gate decision was recorded and no external system was accessed.
- Release the lease; the phase's assessment happens at a gate this agent does not own.

## Phase Ownership

The phases this agent owns, and the gate each output is assessed at. The workflow
specification is the authority; this table binds the lifecycle above to it.

| Workflow | Phase | Accepted change consumed | Assessed at |
|---|---|---|---|
| `implement-feature` | `implementation` | `technical-design.md` | Review Gate |
| `fix-bug` | `fix-implementation` | `bug-analysis.md` | Fix Gate |
| `refactor` | `refactor-implementation` | `validation-report.md` | Implementation Gate |

This agent owns none of those gates. It supplies the evidence they assess.

## Handoff Contract

- The deliverable is `implementation-report.md` plus the source and test changes it declares.
- The report is handed to the reviewer and to QA together; neither receives it alone.
- Unverified areas travel with the handoff explicitly, as a named list.
- Escalated deviations remain open at handoff; they are not closed by the handoff itself.

## Determinism Requirements

- The stage order in `reasoning.md` is fixed and complete for every run.
- Identifiers are assigned once and never renumbered, except to compact a family after a
  mid-run retirement; the compaction records its old-to-new mapping, describing superseded
  items by subject, never by their retired identifier token.
- The verification claim follows the Stage 7 table and nothing else.
- Two runs over the same accepted change and repository snapshot produce the same report.

## Observability

Each state transition records: state entered, trigger, error class when applicable, the
change-set entries affected, and the evidence executed so far. The run's evidence is the
report plus the commands it names; nothing is claimed that the record cannot support.
