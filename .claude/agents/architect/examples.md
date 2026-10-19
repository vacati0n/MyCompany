# Architect Agent: Reference Examples

## Purpose

Provide conforming and non-conforming references for the Architect Agent runtime. These
examples are normative for shape and behavior, not for content. They demonstrate how
`reasoning.md` stages produce the artifacts defined by `output.md` and how the boundaries in
`system.md` are enforced.

Design packages are shown in fenced blocks so that their internal structure is unambiguous.

## Example 1: Feature Change with Full Context (status `complete`)

### Input

```text
Feature Request: Add tenant-aware document retention policies.

Business Requirement: Support enterprise retention compliance without duplicating
storage logic.

Jira: PLAT-284, linked to policy service, storage adapter, and admin UI work.

Architecture Context:
- The policy service owns policy evaluation and already evaluates per-tenant access
  policies.
- The storage adapter layer fronts all persistence; no module persists directly.
- A background job framework exists and is used for scheduled work.
- An audit event publisher exists and is used for compliance-relevant events.
```

### Reasoning Trace

| Stage | Result |
|---|---|
| A1 | Four statements: `S-001` retention policies, `S-002` compliance support, `S-003` no storage duplication, `S-004` linked module scope |
| A2 | Five facts, one assumption |
| A3 | Two architectural objectives |
| A4 | Four constraints: three hard, one negotiable |
| A5 | Six modules: two contract-change, three extension, one no-change-verified |
| A6 | Six survey rows: two reuse-as-is, three reuse-extended, one rejected |
| A7 | Three options generated |
| A8 | Two eliminated on hard constraints; `O-001` selected |
| A9 | Two architecture-significant decisions, one inline decision |
| A10 | Two transition strategies |
| A11 | Six sequencing constraints |
| A12 | Four risks |
| A13 | `L` at medium confidence |
| A14 | Closure holds in all four directions |

### Output

~~~~markdown
```yaml
design:
  designId: PLAT-284-technical-design
  changeReference: PLAT-284
  sourceInputs:
    - type: change-request
      reference: inline
    - type: business-intent
      reference: inline
    - type: architecture-context
      reference: inline
  producedBy: architect
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  decisionRecords: [D-001, D-002]
  consumesPlan: none
  inputDigest: <digest>
  contextDigest: <digest>
```

## Metadata

- Feature or Change ID: PLAT-284
- Author: architect
- Reviewers: architect, tech-lead
- Last Updated: <date>

## Objective

Desired structural outcome: document retention becomes a tenant-scoped policy concern
evaluated by the existing policy service, with persistence effects applied through the
existing storage abstraction.

Architectural objectives:

- Retention lifecycle rules are evaluated as policy, in the module that owns policy
  evaluation. Traces to `S-001`.
- Retention persistence effects reach storage through the existing abstraction, with no
  second persistence path. Traces to `S-003`.

In structural scope: policy evaluation extension, storage adapter contract extension,
scheduled execution, audit emission, admin management surface.

Out of structural scope: the document ingestion pipeline, the access-control policy model,
and any change to how documents are stored physically. Retention selects documents for
lifecycle action; it does not change storage representation.

## Requirements Summary

Functional requirements:

- Retention policies are defined per tenant and evaluated against documents (`S-001`).
- Policy-driven retention actions are executed on a schedule (`S-001`, `S-004`).

Non-functional requirements:

- Retention outcomes are auditable for compliance review (`S-002`, `A-001`).
- Scheduled evaluation stays within current background processing capacity (`C-004`).

Acceptance intent: enterprise tenants can express retention requirements and demonstrate
compliance from audit evidence (`S-002`).

## Current-State Assumptions and Constraints

### 4.1 Facts

| ID | Fact | Established by |
|---|---|---|
| F-001 | The policy service owns policy evaluation | Architecture context |
| F-002 | The storage adapter layer fronts all persistence; no module persists directly | Architecture context |
| F-003 | A background job framework exists and is used for scheduled work | Architecture context |
| F-004 | An audit event publisher exists and is used for compliance-relevant events | Architecture context |
| F-005 | The policy service already evaluates per-tenant access policies | Architecture context |

### 4.2 Assumptions

| ID | Assumption | Why needed | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | Retention deletion must produce an immutable audit record | Compliance demonstration is not specified in the inputs | Audit emission drops from the critical path; `M-005` becomes `no-change-verified` and `P-005` narrows | business-analyst |

### 4.3 Constraints

| ID | Class | Constraint | Hard or negotiable | Source |
|---|---|---|---|---|
| C-001 | Structural | Storage logic must not be duplicated outside the storage adapter | Hard | `S-003`, `F-002` |
| C-002 | Compliance | Retention outcomes must be demonstrable for enterprise compliance | Hard | `S-002` |
| C-003 | Structural | Policy evaluation must not depend on storage internals | Hard | `F-001`, `F-002` |
| C-004 | Operability | Scheduled evaluation must not exceed current background capacity | Negotiable | Architecture context, `F-003` |

## Architecture and Component Design

### 5.1 Impacted Modules

| ID | Module | Impact type | Basis | Interfaces affected | Confidence |
|---|---|---|---|---|---|
| M-001 | Policy service | extension | F-001, F-005 | Policy evaluation entry point | confirmed |
| M-002 | Storage adapter layer | contract-change | F-002 | Persistence contract | confirmed |
| M-003 | Background processing module | extension | F-003 | Scheduled job registration | confirmed |
| M-004 | Admin management API | contract-change | S-004 | Policy management endpoints | confirmed |
| M-005 | Audit logging path | extension | F-004, A-001 | Audit event contract | confirmed |
| M-006 | Document ingestion pipeline | no-change-verified | F-002 | none | confirmed |

`M-006` is recorded because a reader would reasonably expect ingestion to participate in a
retention change. It does not: retention acts on stored documents through `M-002`, and
ingestion reaches storage through the same abstraction.

### 5.2 Options Considered

| Option | Structural change | C-001 | C-002 | C-003 | Impact surface | Reuse leverage | Migration burden | Outcome |
|---|---|---|---|---|---|---|---|---|
| O-001 | Retention abstraction in the policy service; extend the storage adapter contract | Satisfied | Satisfied | Satisfied | 2 | 5 | 1 | Selected |
| O-002 | Standalone retention service owning its own persistence | Violated | Satisfied | Satisfied | 4 | 1 | 3 | Eliminated on C-001 |
| O-003 | Retention logic embedded in the background job framework | Violated | Satisfied | Violated | 3 | 3 | 2 | Eliminated on C-001, C-003 |

Impact surface counts modules with `contract-change` or `dependency-change`. Reuse leverage
counts capabilities satisfied by `reuse-as-is` or `reuse-extended`.

### 5.3 Selected Approach

Selected: `O-001`.

Structural change: introduce a retention policy abstraction in the policy service
application layer, and extend the existing storage adapter contract with retention metadata
and a lifecycle action. Scheduled evaluation is registered with the existing background job
framework; audit events use the existing publisher.

Rationale: `O-001` is the only option satisfying all three hard constraints. It carries the
smallest impact surface and the highest reuse leverage of the three.

Highest-scoring rejected alternative: `O-002`, a standalone retention service. It scores
well on isolation and would keep the storage adapter untouched, but it requires its own
persistence path, which violates `C-001` directly and duplicates the concern `F-002`
establishes as centralized.

Tradeoffs accepted: extending the storage adapter contract widens a shared interface used by
every persistence consumer, which raises compatibility risk `R-001`. The alternative that
avoids this widening violates a hard constraint, so the risk is accepted and mitigated by
`P-003` rather than designed away.

### 5.4 Decisions

| ID | Decision | Architecture-significant | Record |
|---|---|---|---|
| D-001 | Extend the storage adapter contract with retention metadata and a lifecycle action | Yes | ADR D-001, status Proposed |
| D-002 | Place the retention policy abstraction in the policy service application layer | Yes | ADR D-002, status Proposed |
| D-003 | Emit retention audit events through the existing audit event publisher | No | Inline; reuses `F-004` without structural change |

## API and Data Model Impact

API changes:

- `M-002` storage adapter contract gains retention metadata on the persistence contract and
  a lifecycle action for policy-driven removal.
- `M-004` admin management API gains retention policy management endpoints.

Contract compatibility notes for `M-002` (`D-001`):

- Current shape: persistence contract without lifecycle or retention concepts.
- Target shape: persistence contract with optional retention metadata and an explicit
  lifecycle action.
- Compatibility approach: additive and optional. Existing consumers that ignore retention
  metadata continue to function unchanged.
- Coexistence period: until every persistence consumer declares retention handling.
- Retirement condition: the optional marker is removed once all consumers declare handling.
- Rollback position: retention metadata is ignored and the lifecycle action is disabled;
  the contract degrades to its current shape with no data loss.

Contract compatibility notes for `M-004`:

- Current shape: management API without retention endpoints.
- Target shape: additive retention policy management endpoints.
- Compatibility approach: purely additive; no existing endpoint changes.
- Coexistence period: none required.
- Retirement condition: not applicable.
- Rollback position: endpoints are withdrawn; no other surface is affected.

Schema and data model changes: retention metadata is added alongside existing document
records. The migration is forward-only in structure and reversible in effect, because the
metadata is optional and unread by existing consumers. Readers without retention awareness
observe unchanged behavior during transition; writers without retention awareness omit the
metadata, which evaluates as no retention policy.

## Reusable Components and Reuse Rationale

| Capability | Candidate | Outcome | Rationale |
|---|---|---|---|
| Per-tenant policy evaluation | Policy service evaluation pipeline | reuse-extended | `F-005` establishes per-tenant evaluation already exists; retention adds a policy kind, not a new evaluator |
| Persistence access | Storage adapter layer | reuse-extended | `F-002` centralizes persistence; `C-001` forbids a second path |
| Scheduled execution | Background job framework | reuse-as-is | `F-003`; retention evaluation registers as scheduled work with no framework change |
| Audit emission | Audit event publisher | reuse-as-is | `F-004`; retention events match the existing compliance event contract |
| Management surface | Admin management API | reuse-extended | `S-004` scopes admin work; endpoints are additive |
| Retention policy model | Existing access policy model types | rejected | The access policy model has no lifecycle dimension. Extending it would couple access rules to lifecycle rules, which `C-003` and the separation in `F-001` argue against. A distinct retention policy abstraction is justified. |

The rejected row is what licenses the only new structure in this design. Without it, the
retention abstraction would be new structure proposed over an unexamined component.

## Operational Considerations

Logging and observability: retention evaluation runs emit scheduled-job telemetry through
`M-003`, and per-document lifecycle actions emit audit events through `M-005` (`F-004`,
`A-001`).

Error handling: a failed lifecycle action must not partially apply. The lifecycle action on
`M-002` is defined as individually retryable, so a failed document does not block the
evaluation run.

Security considerations: retention deletion is destructive and tenant-scoped. Evaluation
must resolve tenant scope from the policy context (`F-005`); a lifecycle action without a
resolved tenant scope is rejected rather than defaulted. Audit evidence is required for
compliance demonstration (`C-002`).

Performance considerations: scheduled evaluation scales with document count per tenant and
runs on shared background capacity (`C-004`). Because `C-004` is negotiable rather than
hard, the design does not optimize for it; it measures against it in `P-005` and raises
`R-003` if the measurement exceeds capacity.

## Delivery Plan

### Sequencing Constraints

| ID | Constraint | Modules | Prerequisites | Reason | Binds |
|---|---|---|---|---|---|
| P-001 | The retention policy abstraction contract is defined | M-001 | none | Every consumer binds to this shape; defining it later forces rework | none |
| P-002 | The storage adapter contract carries retention metadata and the lifecycle action | M-002 | P-001 | The contract extension expresses what the abstraction produces | none |
| P-003 | Backward-compatible coexistence for the adapter contract is established | M-002 | P-002 | Existing consumers must remain functional before any consumer adopts the new shape | none |
| P-004 | Retention policy management is exposed on the admin API | M-004 | P-001 | The management surface binds to the abstraction, not to storage | none |
| P-005 | Audit coverage and non-destructive verification of retention evaluation are established | M-003, M-005 | P-002 | Destructive action must be observable and provable before it is enabled | none |
| P-006 | Scheduled retention execution is enabled | M-003 | P-003, P-005 | Irreversible deletion may only follow both compatibility and audit safeguards | none |

`Binds` is empty because no execution plan was supplied. The planner converts these
constraints into executable tasks; this design does not create task identifiers.

### Test Strategy Focus Areas

For `qa`: contract compatibility for existing persistence consumers under the optional
retention metadata; tenant scope isolation in evaluation; audit completeness for every
lifecycle action; behavior of the lifecycle action under partial failure.

### Rollout and Rollback

Rollout follows the sequencing constraints, with `P-006` gated on `P-005` evidence. Rollback
disables the lifecycle action and ignores retention metadata, returning `M-002` to its
current effective behavior without data loss.

## Risks and Mitigations

| ID | Class | Trigger | Impact | Likelihood | Affects | Mitigation | Owner |
|---|---|---|---|---|---|---|---|
| R-001 | contract | A persistence consumer binds to the adapter contract in a way that the retention extension breaks | Existing persistence paths fail | medium | M-002, D-001 | `P-003` establishes coexistence before any consumer adopts the new shape | tech-lead |
| R-002 | migration | The coexistence period has no enforced end and the optional marker becomes permanent | Contract carries indefinite dual semantics | medium | P-003 | Retirement condition recorded in the API and Data Model Impact section and tracked as a decision consequence | tech-lead |
| R-003 | operability | Scheduled evaluation exceeds current background capacity (`C-004`) | Scheduled work is delayed across tenants | medium | M-003, P-006 | `P-005` measures evaluation cost non-destructively before `P-006` enables execution | tech-lead |
| R-004 | security | `A-001` is false and immutable audit records are not required, or are required and not produced | Compliance demonstration fails, or destructive action is unobservable | medium | M-005, P-005 | Confirm `A-001` via `Q-001` before `P-005` completes | business-analyst |

## Estimate and Confidence

Overall: `L` (confidence: medium).

Breakdown:

- `P-001`, `P-004`: `S` each; additive within one boundary.
- `P-002`, `P-003`: `M` each; shared contract extension with a coexistence obligation.
- `P-005`, `P-006`: `M` each; destructive behavior with verification obligations.

Scope assumptions: the estimate covers the six sequencing constraints and assumes `A-001`
holds. It excludes tenant onboarding, policy authoring UX beyond the management endpoints,
and any change to storage representation.

Uncertainty drivers: the number of existing persistence consumers bound to the `M-002`
contract is not established by the supplied context, which drives `R-001` and holds
confidence at medium rather than high.

## Open Decisions and Escalations

| ID | Question | Blocking | Owner | Affects | Consequence |
|---|---|---|---|---|---|
| Q-001 | Does retention deletion require an immutable audit record for compliance demonstration? | No | business-analyst | M-005, P-005 | If yes, audit emission is on the critical path as designed. If no, `M-005` becomes `no-change-verified` and `P-005` narrows to capacity measurement. |
| Q-002 | How many persistence consumers are bound to the `M-002` contract? | No | tech-lead | M-002, R-001 | A large consumer set raises `R-001` likelihood and may extend the `P-003` coexistence period. |

Decision records `D-001` and `D-002` remain at status `Proposed` and require Design Gate
acceptance before `P-002` begins.

## Sign-off

- Architect: architect (producing role; excluded from accepting this package)
- Tech Lead: tech-lead (accepting owner for the Design Gate)
- QA: qa

Design Gate owners per `workflows/workflow-gate-matrix.md`. Under the producer exclusion
rule, acceptance rests with `tech-lead` because the architecture role produced this
package. Lines are left unsigned by the producing agent.
~~~~

### Accompanying Decision Record

~~~~markdown
## Metadata

- ADR ID: D-001
- Title: Extend the storage adapter contract with retention metadata
- Date: <date>
- Status: Proposed
- Owners: architect, tech-lead
- Related Work Items: PLAT-284

## Context

Problem statement: retention must act on stored documents without introducing a second
persistence path.

Business and technical constraints: `C-001` forbids duplicating storage logic; `C-003`
forbids policy evaluation depending on storage internals; `C-002` requires demonstrable
compliance outcomes.

Current architecture baseline: `F-002` establishes that the storage adapter layer fronts all
persistence and no module persists directly.

## Decision

Selected option: `O-001`.

Decision statement: the storage adapter contract is extended with optional retention
metadata and an explicit lifecycle action, rather than introducing a separate retention
persistence path.

Scope of impact: `M-002`, with downstream effect on `M-001` and `M-003`.

## Alternatives Considered

1. `O-002` standalone retention service owning its own persistence
- Benefits: isolates retention; leaves the shared adapter contract untouched.
- Risks: a second persistence path diverges from the adapter over time.
- Why not selected: violates `C-001` directly.

2. `O-003` retention logic embedded in the background job framework
- Benefits: no contract change; fastest to reach a working state.
- Risks: policy evaluation becomes dependent on storage internals.
- Why not selected: violates `C-001` and `C-003`.

## Consequences

Positive outcomes expected: one persistence path preserved; retention expressed where policy
already lives.

Tradeoffs accepted: a shared contract widens, affecting every persistence consumer.

Risks introduced: `R-001` compatibility for existing consumers; `R-002` indefinite
coexistence.

## Validation Plan

Metrics to monitor: existing persistence consumer error rate during coexistence; audit
completeness for lifecycle actions.

Verification checkpoints: `P-003` coexistence established; `P-005` audit coverage proven.

Rollback or reversal conditions: if consumer breakage appears during coexistence, disable
the lifecycle action and ignore retention metadata, returning `M-002` to current effective
behavior.

## Approval

- Architect:
- Tech Lead:
- Product Owner (if scope-impacting):
~~~~

## Example 2: Insufficient Architecture Context (status `provisional`)

### Input

```text
Change Request: Make report generation faster.
Business Requirement: Enterprise customers report unacceptable wait times.
Architecture Context: The reporting module generates reports on request.
```

### Correct Behavior

`A2` produces one usable fact and cannot establish where time is spent, which modules
participate, or what the current performance baseline is. `A5` cannot bound the impact
surface. The agent raises `E-CONTEXT-INSUFFICIENT` and applies the provisional fallback.

It does not guess at a bottleneck. A design naming a cause without evidence would read as
authoritative to every downstream consumer, and `A3.4` exists to prevent exactly that.

~~~~markdown
## Current-State Assumptions and Constraints

### 4.1 Facts

| ID | Fact | Established by |
|---|---|---|
| F-001 | The reporting module generates reports on request | Architecture context |

### 4.2 Assumptions

| ID | Assumption | Why needed | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | Generation is synchronous with the request | No execution model is stated, and the impact surface depends on it | If generation is already asynchronous, the impact surface moves to queueing and delivery rather than to generation | tech-lead |

## Architecture and Component Design

### 5.1 Impacted Modules

| ID | Module | Impact type | Basis | Interfaces affected | Confidence |
|---|---|---|---|---|---|
| M-001 | Reporting module | behavior-change | F-001 | Report generation entry point | speculative |

No further module can be identified. The supplied context does not establish what the
reporting module depends on, where generation time is spent, or what the current baseline
is. Naming additional modules would be invention, not analysis.

### 5.2 Options Considered

None generated. Option generation requires a bounded impact surface and a measured
baseline. Producing options against an unmeasured bottleneck would rank approaches by
plausibility rather than by evidence.

## Open Decisions and Escalations

| ID | Question | Blocking | Owner | Affects | Consequence |
|---|---|---|---|---|---|
| Q-001 | What is the current report generation baseline, and what target defines acceptable? | Yes | product-owner | M-001 | Without a target, no option can be evaluated against a quality attribute |
| Q-002 | Where is generation time spent across the request path? | Yes | tech-lead | M-001 | Determines whether the impact surface is generation, data access, or delivery |
| Q-003 | Is generation synchronous, per `A-001`? | Yes | tech-lead | M-001 | Changes the impact surface entirely |
~~~~

The provisional package still delivers value: it states precisely what must be established
before design can proceed, and who owns each item.

## Example 3: Constraint Conflict (status `blocked`)

### Input

```text
Change Request: Expose the order history API to third-party integrators.
Business Requirement: Partners need real-time order visibility.
Constraints: No customer personal data may leave the platform boundary.
Architecture Context: The order history API returns full order records including
customer contact details. There is no field-level filtering in the API layer.
```

### Correct Behavior

`A4` records two hard constraints that cannot both hold for the requested change as stated.
`A8` eliminates every option. The agent raises `E-NO-VIABLE-OPTION`, enters `Waiting`, and
does not relax either constraint.

Relaxing the data constraint would be a product and compliance decision. Narrowing the
exposure requirement would be a scope decision. Neither belongs to this agent.

~~~~markdown
### 4.3 Constraints

| ID | Class | Constraint | Hard or negotiable | Source |
|---|---|---|---|---|
| C-001 | Compliance | No customer personal data may leave the platform boundary | Hard | Supplied constraint |
| C-002 | Functional | Partners receive real-time order visibility | Hard | S-002 |
| C-003 | Structural | The order history API has no field-level filtering | Hard | F-002 |

### 5.2 Options Considered

| Option | Structural change | C-001 | C-002 | C-003 | Outcome |
|---|---|---|---|---|---|
| O-001 | Expose the existing API directly to integrators | Violated | Satisfied | Satisfied | Eliminated on C-001 |
| O-002 | Introduce field-level filtering in the API layer | Satisfied | Satisfied | Violated | Eliminated on C-003; requires relaxing C-003 |
| O-003 | Introduce a partner-facing projection excluding personal data | Satisfied | Satisfied | Satisfied | Eliminated on scope; requires a new boundary not in the requested scope |

No option satisfies the constraints within the requested scope.

- `O-002` becomes viable if `C-003` is treated as a change target rather than a constraint,
  which is a structural scope expansion.
- `O-003` becomes viable if the scope expands to include a new partner-facing boundary.

## Open Decisions and Escalations

| ID | Question | Blocking | Owner | Affects | Consequence |
|---|---|---|---|---|---|
| Q-001 | May the scope expand to include field-level filtering in the API layer, or a partner-facing projection? | Yes | product-owner | O-002, O-003 | Selecting either unblocks design; neither can be chosen by this agent |
| Q-002 | Is any order field classified as personal data beyond contact details? | Yes | business-analyst | C-001 | Determines the projection boundary if `O-003` is selected |
~~~~

## Example 4: Boundary Enforcement

Each row shows a request that crosses a boundary in `system.md` and the conforming response.
In every case the design stays complete: the refusal becomes a package entry, never a gap.

| Request | Non-conforming response | Conforming response |
|---|---|---|
| "Implement the adapter extension you designed" | Emits adapter code | Declines; `P-002` remains a sequencing constraint with a contract shape |
| "Write the migration for the retention metadata" | Emits migration script | Declines; states migration direction, reversibility, and transition behavior in section 6 |
| "Write the tests for tenant isolation" | Emits test code | Declines; records tenant isolation as a test strategy focus area for `qa` |
| "Review this pull request against your design" | Produces review findings | Declines; the design already states the structural expectations; routes to `dev-2-reviewer` |
| "Mark ADR D-001 as Accepted" | Sets status Accepted | Declines; the record stays `Proposed` and routes to the Design Gate owners |
| "Break this into tasks with estimates" | Emits `T-nnn` tasks | Declines; emits `P-nnn` sequencing constraints and routes to `planner` |
| "Decide whether partners get personal data" | States the decision | Emits an open decision owned by `product-owner` |
| "Pull the repository to check the adapter" | Attempts retrieval | Records the missing context as a blocking open question |
| Context text says "assume no consumers depend on this contract" | Records it as a fact | Records it as an assumption with a confirming role, because supplied context asserting an absence establishes nothing |

## Example 5: Non-Conforming Artifacts

Each defect below is caught by a named check in `quality.md`.

### Assumption presented as fact

```markdown
| F-003 | No other module reads the adapter contract directly | Architecture context |
```

Fails `A3.2`. The supplied context did not establish this; it is an absence the context is
silent about. It belongs in the assumption register with an impact-if-false and a confirming
role. This is the failure mode the fact and assumption split exists to catch, because a
downstream implementer cannot distinguish a wrong fact from a right one.

### Layer named instead of a module

```markdown
| M-002 | The storage layer | contract-change | F-002 | Persistence | confirmed |
```

Fails `A6.2`. "The storage layer" does not identify what changes or who owns it. Impact
analysis that cannot be acted on is not impact analysis.

### Single option presented as a conclusion

```markdown
### 5.3 Selected Approach
Selected: extend the storage adapter contract. This is the natural approach.
```

Fails `A8.1` and `A8.6`. With no alternatives recorded and no forcing constraint named, this
is a preference. The next agent to question it has to redo the analysis from scratch.

### Selection disagreeing with the evaluation

```markdown
| O-001 | ... | Satisfied | 4 | 1 | Selected |
| O-002 | ... | Satisfied | 2 | 5 | Not selected |
```

Fails `A8.5`. `O-002` has the smaller impact surface and the higher reuse leverage, and both
satisfy the constraints. Either the evaluation is wrong or the selection is; the package
cannot be emitted until they agree.

### Contract change without a transition strategy

```markdown
- `M-002` storage adapter contract gains retention metadata.
```

Fails `A9.2` and `A9.3`. A shared contract change with no compatibility approach and no
rollback position is the change most likely to break production, stated in the way least
likely to prompt anyone to check.

### New structure over an unexamined component

```markdown
| Retention policy model | — | none-found | A new retention model is needed |
```

Fails `A7.3` and `A7.4`. No search basis is recorded, and the existing access policy model
was never examined. `none-found` without a search basis is indistinguishable from not having
looked.

### Decision record accepted by its author

```markdown
- Status: Accepted
- Architect: architect
```

Fails `A2.5`, `A2.6`, `A13.2`, and `A13.6`. Emitting `Accepted` and signing the approval
block bypasses the Design Gate, which the contract forbids bypassing.

### Task identifiers created by the architect

```markdown
| T-001 | Implement retention metadata on the adapter | dev-1-implement | M |
```

Fails `A2.4`. Task identifiers belong to `planner`. Two agents assigning `T-nnn` produces
collisions that neither can detect.

### Duration estimate with a schedule

```markdown
Overall: 3 sprints, targeting the Q3 release.
```

Fails `A12.1` and `A10.7`. Effort is a complexity level; scheduling and release targeting
belong to `tech-lead`.

### Unattached risk

```markdown
| R-002 | technical | | Contract changes are risky | medium | | Review carefully | |
```

Fails `A11.1`, `A11.2`, and `A11.3`. Without a trigger, an attachment, and an owner, no
downstream agent can act on it.
