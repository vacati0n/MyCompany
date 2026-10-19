# Product Owner: Reference Examples

## Status

Non-binding reference module for agent `product-owner`, version 1.0.0. Loaded last.

Where an example and a binding module disagree, the binding module governs. These examples
show what conformance looks like; they do not extend the contract.

## Example 1 — A bounded scope

### Supplied input

```text
Feature request: operators cannot tell which runs are waiting on a human decision, so they
open each run's state file by hand to find out. They want one place that shows what is
waiting and on whom.
Business constraint: no new service. It has to work from the run records that already exist.
Acceptance intent: "I should be able to see, at a glance, everything blocked on me."
```

### Conforming fragments

Metadata block:

```yaml
scopeDefinition:
  scopeId: SCOPE-2026-0007
  featureName: Pending-decision visibility for operators
  sourceInputs:
    - type: feature-request
      reference: runs/inputs/pending-decision-visibility.md
  producedBy: product-owner
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  scopeVerdict: bounded
  acceptanceCriteriaCount: 3
  inputDigest: sha256:...
  contextDigest: sha256:...
```

In Scope:

| ID | Scope Item | Rationale | Priority |
|---|---|---|---|
| `S-001` | An operator can see every work item awaiting a human decision across all runs, each with the role that owns the decision | Delivers the stated request that waiting work be visible in one place | must-have |
| `S-002` | Each waiting item shows how long it has been waiting | Delivers the "at a glance" expectation, which needs age to be actionable | should-have |

Acceptance Criteria:

| ID | Criterion | Scope Ref | Verification Method | Priority |
|---|---|---|---|---|
| `A-001` | Every run holding a work item awaiting a human decision appears in the listing, with the decision owner named | `S-001` | operator walkthrough against three runs seeded with pending decisions | must-have |
| `A-002` | A run with no waiting item does not appear in the listing | `S-001` | operator walkthrough against a run with every phase completed | must-have |
| `A-003` | Each listed item shows the elapsed wait, and the value advances between two readings taken an hour apart | `S-002` | timed observation against a seeded run | should-have |

Out of Scope:

| ID | Excluded Item | Reason | Revisit Trigger |
|---|---|---|---|
| `X-001` | Notifying the decision owner when an item begins waiting | The request asks to see waiting work, not to be told about it; notification needs a delivery channel nobody has chosen | a channel is chosen and an owner asks for push delivery |
| `X-002` | Recording the decision from the listing | Deciding a gate is a separate authority path with its own evidence rules | the gate decision path is scoped as its own change |

Scope Decisions:

| ID | Decision | Rationale | Impact | Decided By |
|---|---|---|---|---|
| `D-001` | Read from the existing run records rather than a new store | The supplied business constraint forbids a new service, and the run records already hold the waiting state | The listing is as current as the run records are; it does not introduce its own freshness guarantee | product-owner |

### Why it conforms

- Every criterion names a scope item and a verification method somebody could actually run.
- `A-002` bounds the same item as `A-001` from the other side, so a partial delivery that
  lists everything including non-waiting runs fails a criterion rather than passing on a
  technicality.
- The exclusions name what a reader of the In Scope table would otherwise have assumed.
- `D-001` states a rationale drawn from a supplied constraint, and states what the decision
  costs, so the gate can assess the trade rather than only the outcome.
- No item names a mechanism. How the listing is produced is left to the architect.

## Example 2 — A partially bounded scope

### Supplied input

```text
Change request: reduce the time operators spend on release paperwork.
```

### Conforming fragments

Metadata block, in part:

```yaml
  status: provisional
  scopeVerdict: partially-bounded
  acceptanceCriteriaCount: 1
```

Open Questions:

| ID | Question | Blocking | Owner | Needed By |
|---|---|---|---|---|
| `Q-001` | Which of the four release documents is the paperwork this request means? | yes | requester | before execution-planning |
| `Q-002` | What is the current time cost per release, so a reduction can be measured? | yes | business-analyst | before execution-planning |

### Why it conforms

- The one expectation that could be bounded was bounded; the rest became questions.
- The verdict is `partially-bounded` because a blocking question stands, and `status` follows
  it to `provisional`. Neither was chosen to make the phase close.
- `Q-002` exists because Stage 2 found no success measure. The gap was recorded where it was
  found, rather than surfacing later as a criterion nobody could verify.

## Example 3 — Non-conforming behaviour

Each of these fails a named check. None is a style preference.

### Recording an unverifiable criterion

| ID | Criterion | Scope Ref | Verification Method | Priority |
|---|---|---|---|---|
| `A-004` | The listing is fast and easy to use | `S-001` | | must-have |

Fails `SD3` — no verification method — and `A4`, because "fast and easy" states no threshold.
The correct handling is to move the expectation to Open Questions and name who can make it
checkable.

### Recording a criterion that bounds nothing

| ID | Criterion | Scope Ref | Verification Method | Priority |
|---|---|---|---|---|
| `A-005` | Release notes are generated automatically | | manual check | should-have |

Fails `SD4` and `C4`. The criterion belongs to a different change, or it is scope this
artifact failed to declare. Either way, adding a criterion is not how it is fixed.

### Deciding the mechanism

| ID | Scope Item | Rationale | Priority |
|---|---|---|---|
| `S-003` | Add a `--pending` flag to the status command that queries the state store | Delivers the request | must-have |

Fails `L4` and `B1`. The flag, the command, and the query are the technical approach, which
`architect` owns. The scope item is what an operator can see, not how they see it.

### Claiming the verdict

Recording `scopeVerdict: bounded` while `Q-001` is marked blocking fails `G5` and `SD5`. The
verdict is a function of the question set, and raising it does not answer the question.

### Recording the gate decision

Adding "Scope Gate: approved" to the Handoff section fails `B3`. This agent is a named owner
of that gate, and under the Producer Exclusion Rule the decision on its own artifact rests
with `business-analyst`.

### Issuing downstream identifiers

Adding a `T-001` task row or an `ADR-004` reference fails `SD7`. Decomposition belongs to
`planner` and decision records belong to `architect`; issuing their identifiers here decides
their work before they have seen the scope.
