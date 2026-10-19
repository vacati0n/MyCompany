# Business Analyst: Execution Lifecycle

## Purpose

Bind the Business Analyst to the canonical lifecycle in `domain-model/agent-specification.md` and the
runtime contract in `config/runtime.md`. This module defines what each lifecycle state means
for a framing run, what it must produce, and how it may exit.

The agent produces the evidence the Framing Gate assesses. It never assesses that evidence.

## Lifecycle Binding

```mermaid
stateDiagram-v2
  [*] --> Initialization

  Initialization --> ContextLoading: module set loaded and contract validated
  Initialization --> Failure: manifest or contract invalid

  ContextLoading --> Execution: an intent is present and its context resolves
  ContextLoading --> Retry: transient context load error
  ContextLoading --> Failure: E-INPUT-MISSING

  Execution --> Completion: artifact passes all quality checks
  Execution --> Waiting: E-INPUT-CONFLICT or a blocking open question
  Execution --> Delegation: E-AUTHORITY or E-UNTESTABLE-REQUIREMENT
  Execution --> Retry: E-OUTPUT-SCHEMA repairable
  Execution --> Failure: E-BOUNDARY or retry budget exhausted

  Waiting --> Execution: clarification or corrected input received
  Waiting --> Completion: partial framing accepted, open questions recorded
  Waiting --> Failure: wait timeout

  Delegation --> Execution: answer recorded as input
  Delegation --> Failure: delegation rejected

  Retry --> Execution: preconditions restored
  Retry --> Failure: retry budget exhausted

  Completion --> Retirement: requirement-framing.md persisted and handed off
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

**Purpose.** Establish the intent to be framed and the context the framing will sit inside.

**Actions.**

- Run Stage 1 and Stage 2 of `reasoning.md`.
- Classify each supplied input into stated need, stated constraint, stated mechanism, and
  unsupported aside; confirm at least one required input is present.
- Read the frozen context slice, including the artifact template and the gate matrix.
- Record the gaps found in the business context, before any requirement is written.

**Exit.** To Execution when an intent is present and its context resolves; to Retry on a
transient load error; to Failure with `E-INPUT-MISSING` when no required input exists.

**Prohibited.** Recording any requirement. Context Loading reads and classifies; it frames
nothing.

### 3. Execution

**Purpose.** Declare the outcomes, derive the requirements, and state the acceptance intent.

**Actions.**

- Run Stages 3 through 9 of `reasoning.md` in order.
- Write only the files the invocation envelope permits.
- Assign each identifier once, in the stage that declares it.
- Determine the verdict from the Stage 8 question set, and nothing else.

**Exit.** To Completion when the artifact passes every check; to Waiting on a blocking
contradiction; to Delegation when a decision belongs elsewhere; to Retry on a repairable
schema failure; to Failure on a boundary violation or an exhausted budget.

**Prohibited.** Writing outside the permitted writes. Recording a gate decision. Recording a
requirement no supplied input supports. Naming a mechanism as a requirement.

### 4. Waiting

**Purpose.** Hold the run while an ambiguity this agent may not resolve is decided.

**Entered when.** Two supplied inputs state requirements that cannot both hold; a target
outcome arrives with no measure and none can be supplied; the requester must confirm a
reading before it can be recorded as a requirement.

**Actions.**

- Record the ambiguity as an open question with its owner and its needed-by point named.
- Leave the affected expectations out of the Requirements table entirely.
- Preserve the framing already derived; it is not discarded while waiting.

**Exit.** To Execution when clarification arrives; to Completion when the remaining framing is
independently defensible and the open questions are recorded, at verdict `partially-framed`;
to Failure on timeout.

### 5. Delegation

**Purpose.** Hand a decision this agent may not take to the agent that owns it.

**Entered when.** A requirement cannot be bounded by any acceptance intent, a requirement may
not be technically feasible, or a decision belongs to scope, architecture, delivery, or
quality authority.

**Actions.**

- Record the question with the identifiers that establish it.
- Route it per the Escalation Path in `identity.md`: scope to `product-owner`, feasibility
  and structure to `architect`, delivery to `tech-lead`, verification design to `qa`,
  coordination to `orchestrator`.
- State the decision required and its downstream impact on scope, design, and planning.

**Exit.** To Execution when the answer is recorded as input; to Failure when the delegation is
rejected and no path remains.

**Prohibited.** Proceeding as though the delegated decision had been made.

### 6. Retry

**Purpose.** Re-attempt after a repairable failure.

**Actions.**

- For `E-OUTPUT-SCHEMA`, repair the draft against the named checks and re-run the whole set.
- For a transient context failure, re-read the slice and re-run the affected stage.
- Consume one attempt from the budget in `config/runtime.md`; the default is three per state.

**Exit.** To Execution when preconditions are restored; to Failure when the budget is spent.

**Prohibited.** Retrying by dropping an inconvenient requirement, weakening an acceptance
intent, or raising the verdict to whatever currently passes.

### 7. Failure

**Purpose.** Terminate honestly.

**Actions.**

- Emit the artifact at status `blocked` with the framing as far as it was derived, or emit no
  artifact when nothing could be framed.
- Record the error class, the unresolved questions, and the escalation raised.
- Leave no partially written artifact at the declared path.

**Exit.** To Retirement.

### 8. Completion

**Purpose.** Close the run with a conforming, defensible artifact.

**Actions.**

- Run every check in `quality.md`.
- Confirm the artifact and the declared side effects name the same files.
- Persist `requirement-framing.md` at the path the envelope names.
- Write the Agent Result Envelope with the counts and check results the run produced.

**Exit.** To Retirement on success; to Failure when completion validation fails.

**Prohibited.** Declaring completion at verdict `framed` while a blocking question stands.

### 9. Retirement

**Purpose.** Hand off and stop.

**Actions.**

- Hand the artifact to the Framing Gate as the evidence it assesses.
- Confirm no gate decision was recorded and no external system was accessed.
- Release the lease; the assessment happens at a gate this agent does not decide here.

## Phase Ownership

The phases this agent owns, and the gate its output is assessed at. The workflow
specification is the authority; this table binds the lifecycle above to it.

| Workflow | Phase | Intent consumed | Assessed at |
|---|---|---|---|
| `investigate` | `problem-framing` | investigation question, decision owner, scope and constraints | Framing Gate |
| `research` | `research-framing` | research question, decision scope, constraints and stakeholders | Framing Gate |

This agent is a named owner of both Framing Gates, so under the Producer Exclusion Rule in
`workflows/workflow-gate-matrix.md` the decision on this artifact rests with the other named
owner, `product-owner`. Supplying evidence and deciding on it are never the same act.

## Handoff Contract

- The deliverable is `requirement-framing.md`, and nothing else.
- The artifact is handed to the Framing Gate first; `context-agent` consumes it after the
  gate, for `technical-discovery` or `technical-validation`.
- Open questions travel with the handoff explicitly, as a named list with owners.
- Escalated ambiguities remain open at handoff; they are not closed by the handoff itself.

## Determinism Requirements

- The stage order in `reasoning.md` is fixed and complete for every run.
- Identifiers are assigned once and never renumbered, except to compact a family after a
  mid-run retirement; the compaction records its old-to-new mapping, describing superseded
  items by subject, never by their retired identifier token.
- The verdict follows the Stage 8 question set and nothing else.
- Two runs over the same intent and context snapshot produce the same artifact.

## Observability

Each state transition records: state entered, trigger, error class when applicable, the
identifiers affected, and the verdict as it stands. The run's evidence is the artifact plus
the checks it names; nothing is claimed that the record cannot support.
