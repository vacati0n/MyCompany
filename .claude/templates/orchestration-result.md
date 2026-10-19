# Template: Orchestration Result

## Usage

Canonical artifact template for `orchestration-result.md`: the single artifact type that carries
a coordination outcome — how a run's phases progressed, which handoffs were accepted, which
gates carry a recorded decision and who took it, what was escalated, and what the run's closure
or operational position now is.

Three phases across three workflows ask for a coordination output. All three name the file in
their Output Artifact column:

| Workflow | Phase | Producer | Coordination basis |
|---|---|---|---|
| `fix-bug` | `closure-and-communication` | `orchestrator` | `closure` |
| `refactor` | `closure-and-debt-record` | `orchestrator` | `debt-closure` |
| `release` | `deployment-execution` | `orchestrator` | `deployment` |

They are one artifact type. Each is a phase-by-phase progression record over a defined
coordination scope, carrying the handoffs it accepted, the escalations it raised, and the
follow-up work it leaves behind, closing with a coordination position. The Coordination Basis
field is what distinguishes a defect closure from a debt closure or a deployment record; the
structure does not change with it. The Validation Engine
(`runtime/orchestration_result_validator.py`) judges all three against this one contract.

An orchestration result is not a release note. A release note tells a reader outside the run
what changed and what it means for them. An orchestration result records what the run *did*:
which phase ran, in what order, against which gate, decided by whom. The two are produced by
different roles at different phases because they answer different questions, and the release
note is written from this record rather than in place of it.

An orchestration result is also not a decision it had no authority to take. This role coordinates
progression and records the decisions others made; it does not award itself the gates its own
evidence is assessed at. That boundary is the Producer Exclusion Rule in
`workflows/workflow-gate-matrix.md`, and it is enforced mechanically here: a gate whose evidence
this role produced may not be recorded as decided by this role.

Section titles, section order, and identifier schemes are fixed. Sections are never omitted.
A section with nothing to report reads `None identified.`

Identifier schemes: phases `PH-nnn`, handoffs `HO-nnn`, escalations `ES-nnn`, follow-up actions
`FU-nnn`, open questions `Q-nnn`. All zero-padded to three digits and ascending.

An orchestration result quotes the gate evidence and command output it read, so fenced blocks are
permitted here. The prohibition this template keeps is the one every agent contract shares: no
model, vendor, or provider is named.

The binding behavioural contract is the module set under `agents/orchestrator/` —
`output.md` for structure and `quality.md` for the checks. Five coordination rules are enforced
mechanically: no phase is recorded as complete without a gate decision and the evidence behind
it, no progression count may drift from the table it summarises, no gate over this role's own
evidence may be recorded as decided by this role, an unqualified closure carries no open critical
or high escalation, and a rolled-back or held position must state what holds it.

---

```yaml
orchestrationResult:
  resultId:
  coordinationReference:
  coordinationBasis:   # closure | debt-closure | deployment
  sourceInputs:
    - type:            # validation-report | review-package | implementation-report | release-note | deployment-plan | rollback-plan | known-issue-status | documentation-updates | gate-evidence | workflow-state
      reference:       # supplied reference, or "inline"
  producedBy:          # orchestrator
  agentVersion:
  schemaVersion: 1.0.0
  status:              # complete | provisional | blocked
  disposition:         # closed | closed-with-followups | held | rolled-back
  inputDigest:
  contextDigest:
```

## Metadata

- Orchestration ID:
- Coordinator:
- Run under coordination:
- Coordination date:

## Coordination Scope

- In scope:            <!-- the phases, handoffs, and gates this record accounts for -->
- Out of scope:        <!-- what this record deliberately does not account for, and by whose decision -->
- Evidence examined:   <!-- the artifacts read and the gate records inspected, each named -->

## Phase Progression

<!-- One row per phase this run enqueued, in the run's dependency order. Gate Decision and
     Decided By carry the recorded decision, never one taken here. A phase recorded `complete`
     whose gate is not `none` must name both the decision and the evidence for it. Decided By
     may not be `orchestrator` on any row whose Owner is `orchestrator`. -->

| ID | Phase | Owner | Declared Output | Gate | Gate Decision | Decided By | Evidence | Progression |
|---|---|---|---|---|---|---|---|---|

## Progression Summary

<!-- These figures must recompute exactly from the Phase Progression table. -->

- Phases coordinated:
- Complete:
- Blocked:
- Not started:

## Handoffs

<!-- The artifact transfers this coordination accepted or refused. Acceptance is what makes a
     transition real, so it is recorded per row rather than assumed. -->

| ID | From | To | Artifact | Accepted | Evidence |
|---|---|---|---|---|---|

## Escalations

<!-- What this coordination could not unblock itself, and where it went. An escalation this
     role resolved on its own authority is recorded with the authority it used. -->

| ID | Severity | Category | Raised By | Routed To | Status | Detail |
|---|---|---|---|---|---|---|

## Follow-Up Actions

<!-- Work this run leaves behind: deferred items, technical debt delta, and post-release
     actions. A `closed-with-followups` disposition carries at least one row. -->

| ID | Category | Description | Owner | Severity | Status |
|---|---|---|---|---|---|

## Operational Status

<!-- The deployment and monitoring position. A `deployment` basis must populate these fields;
     a closure basis reads `Not applicable.` -->

- Deployment state:    <!-- not-attempted | deployed | partially-deployed | rolled-back | not-applicable -->
- Environment:         <!-- where the recorded state applies, and how it differs from target -->
- Monitoring health:   <!-- the observed health indicators, each named, or `Not applicable.` -->
- Rollback position:   <!-- whether rollback was exercised, is armed, or is unavailable, and why -->

## Coordination Position

- Decision:                         <!-- closed | closed-with-followups | held | rolled-back -->
- Rationale:
- Blocking escalations outstanding: <!-- the open critical and high escalations, by ID, or `None identified.` -->
- Closure recommendation:           <!-- a recommendation, never a decision; the Closure or Deployment Gate rests with its declared owner -->

## Open Questions

<!-- `Q-nnn` items, or `None identified.` A provisional or blocked record carries at least one. -->
