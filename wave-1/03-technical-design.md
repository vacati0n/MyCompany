```yaml
design:
  designId: TDP-2026-0001
  changeReference: tasks/MC-2/input.md
  sourceInputs:
    - type: change-request
      reference: tasks/MC-2/input.md
    - type: execution-plan
      reference: runs/run-dd80173faaad/states/execution-planning/artifacts/execution-plan.md
    - type: business-intent
      reference: runs/run-dd80173faaad/states/scope-and-acceptance/artifacts/scope-definition.md
    - type: architecture-context
      reference: runs/run-258e0a3415d2/states/option-analysis/artifacts/technical-recommendation.md
  producedBy: architect
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  decisionRecords: [D-001, D-002, D-003, D-004, D-005, D-006, D-007]
  consumesPlan: runs/run-dd80173faaad/states/execution-planning/artifacts/execution-plan.md
  inputDigest: sha256:f0fc4ec86188022c0a5b90b691fa703c
  contextDigest: sha256:f65fadccf1c3d16e5951ed9cbe601cbb
```

## Metadata

- Feature or Change ID: MC-2, Wave 1: Company Foundation
- Author: `architect` (agent), invocation `inv-dd80173faaad-03-001`
- Reviewers: `omn-tech-lead` as the accepting owner of the Design Gate under the Producer Exclusion Rule, with `omn-qa` and `omn-dev-2-reviewer`
- Last Updated: 2026-09-26

Citation convention. Bare `F-`, `A-`, `C-`, `M-`, `O-`, `D-`, `P-`, `R-` and `Q-` identifiers in this package belong to this package. Acceptance criteria are cited by the execution plan's `AC-` identifiers, which name the same twenty-eight criteria the scope definition carries. Upstream evaluation criteria and risks are cited as `EC-` and `RK-`, and upstream decisions and open questions are named in words rather than by identifier, so that no upstream token collides with a register here.

## Objective

- Desired outcome: a foundation in which every external capability is reached through one mediating boundary that selects the route, holds the quality floor, obtains the credential, meters the operation and records the reason, so that cost, permission, authority and capacity are properties of the structure rather than of the caller's discipline.

- Architectural objectives:
  - A single capability-resolution boundary is the only path from any component to an external capability, and no component holds a dependency on a provider adapter. Traces S-002, S-006.
  - The quality floor is an attribute of the request evaluated before tier ordering, so no code path exists that returns a route below the floor. Traces S-002.
  - Forbidden sources are unreachable by two independent refusals, at admission and at resolution, so a configuration error alone cannot open a path. Traces S-002.
  - Every external operation is recorded by the same boundary that performs it, in the same transaction, so accounting cannot be bypassed or double-counted. Traces S-003.
  - Authority is a closed enumerated action set held as data, so an absent permission is structurally absent rather than merely unexercised. Traces S-005.
  - Credential material never enters the process region that holds agent context; only opaque scoped handles do. Traces S-006.
  - The record is append-only and self-explaining: the permission answer and the cost derivation are resolvable from the record with no reference outside it. Traces S-004, S-019.
  - Work whose output is determined by its inputs and a rule sits in a module with no dependency on the resolution boundary, so a model call is not expressible from it. Traces S-007.
  - The owner-approval state is a transition in the gate state machine and is absent from the configuration surface, so it cannot be configured away. Traces S-008, S-011.
  - Stack-dependent surface is confined to one persistence boundary, one job-claiming mechanism and the hosting shape, so the pending selection changes as little as possible. Traces S-010.
  - Registers, prices, limits, budgets and routes are temporal configuration rows, so a change takes effect without redeployment and the prior value stays retrievable. Traces S-017, S-013.

- In scope: the module boundaries and contracts of the nineteen modules in 5.1; the capability routing position, its tier semantics and its forbidden-source register; the Wave 1 data model, its relationships and the indexes the two acceptance queries need; the authority and refusal model; the credential protection standard, which discharges the credential-protection question the scope definition and the Framing Gate record as owed by `architect`; the sequencing constraints delivery must respect; and a stack recommendation with recorded alternatives. Traces S-001.

- Out of scope: publishing to any platform and the upload path; channel, platform and payment account creation; any spend commitment; the production pipeline that runs inside this foundation; the compliance and originality judgements themselves; revenue-denominated measures; the agent framework's own runtime, which serves a different line of business (F-015); the executable task breakdown, which `planner` owns; and the stack selection itself, which this package recommends and the Design Gate decides (C-017). Traces S-009, S-014.

## Requirements Summary

- Functional requirements:
  - Resolve every consumed capability to a primary, secondary and emergency route under commercial terms, and to no route at all for a forbidden source. Traces S-002; verified by `AC-001` and `AC-002`.
  - Account for every AI operation exactly once with its full attribution tuple, and roll cost up per item and per month. Traces S-003, S-012; verified by `AC-003` and `AC-004`.
  - Record the permission basis of every asset and refuse a releasable state without it or without library registration. Traces S-018; verified by `AC-005`, `AC-006` and `AC-007`.
  - Answer, for any finished item, why each asset was permitted, from the record alone. Traces S-004; verified by `AC-008` and `AC-009`.
  - Enforce the three least-privilege refusals. Traces S-005; verified by `AC-010`, `AC-011` and `AC-012`.
  - Hold credentials per the Technical Gate rules. Traces S-006; verified by `AC-013` to `AC-016`.
  - Carry zero AI cost on rule-determined work and name that set. Traces S-007; verified by `AC-017`.
  - Record every owner approval and send-back with its elapsed minutes, bound to an exact version. Traces S-008, S-020; verified by `AC-018` and `AC-019`.
  - Produce the operating registers and take price, limit and register changes as configuration. Traces S-017; verified by `AC-020` and `AC-021`.
  - Carry a work lifecycle with stage outcomes, retry, escalation and independent progress. Traces S-015; verified by `AC-022`, `AC-023` and `AC-024`.
  - Report only measurable-now measures and name each deferred one. Traces S-016; verified by `AC-025` and `AC-026`.
  - Carry department and channel budgets with threshold alerts and protect the non-cuttable controls. Traces S-013; verified by `AC-027` and `AC-028`.

- Non-functional requirements:
  - Never downgrade a task below its declared quality floor; hold or escalate instead. Traces S-002.
  - The cost derivation must remain re-derivable after the prices that produced it have changed. Traces S-003, S-017.
  - Rotation and revocation take effect without redeployment. Traces S-006.
  - Operational complexity is a cost borne by one person. Traces S-011.
  - Contract versioning is explicit and failure semantics, timeouts and retries are documented. Traces S-001.
  - Nothing in this change publishes, creates an account or commits spend. Traces S-009, S-014.

- Acceptance criteria: the twenty-eight criteria `AC-001` to `AC-028` are cited as the acceptance intent of this design and are not restated; the twelve technical objectives `TO-1` to `TO-12` are their planning-side expression and are likewise cited. Traces S-014.

## Current-State Assumptions and Constraints

### 4.1 Facts

| ID | Fact | Established by |
|---|---|---|
| F-001 | No build manifest with local dependency references exists in the repository, so no application dependency graph can be derived | `dependency-map.md`, "No manifests detected"; TRC-2026-0001 Decision Context |
| F-002 | No prior artifact selects a language, runtime, datastore or hosting model for this system | `tasks/MC-2/input.md`, "Note on technology choice" |
| F-003 | The repository's declared technology context names the .NET ecosystem as runtime, Clean Architecture as architecture target, and explicit contract versioning with documented timeout and retry behaviour as integration expectations | `context/technical-context.md` |
| F-004 | TRC-2026-0001 records a capability-to-provider routing position stated as primary, secondary and emergency over nine capability classes, and replaces the seat-quota dashboard with spend against the tier cap, per-capability health and per-item cost | TRC-2026-0001 Delivery Impact, Dependencies |
| F-005 | The forbidden-source list is: consumer chat subscriptions driven programmatically; multiple personal accounts held to multiply an allowance; credential sharing; the image service that forbids automated access outright; the generation interface listed for shutdown on 2026-09-24 | TRC-2026-0001 Dependencies; execution plan `T-001` |
| F-006 | Risk `RK-004` is recorded open and owned by `architect`, and is named the only item that blocks building | TRC-2026-0001 risk register; `tasks/MC-2/input.md` constraint 1 |
| F-007 | Every model capability is metered rather than seat-priced, so holding a secondary route warm carries no standing cost | TRC-2026-0001 Dependencies |
| F-008 | TRC-2026-0001 names the set of tasks whose output is fully determined by their inputs and a rule and which therefore call no model | TRC-2026-0001 Delivery Impact |
| F-009 | The approved envelope is USD 77.41 a month, 34.42 metered and 42.99 standing, and every unit price is re-fetched immediately before any spend | `tasks/MC-2/input.md` settled decisions; `AC-004` |
| F-010 | The technical approach, structure, data model and technology selection are deferred to `architect`, with the selection a CEO decision at the Design Gate | scope definition SCOPE-2026-0001, its exclusion on mechanism and its scope decision on the same point |
| F-011 | The supplied execution plan carries 32 tasks in 8 implementation waves, and `T-001`, owned by `architect`, has no predecessor | execution plan Task Breakdown and Implementation Order |
| F-012 | The publishing platform is the one concentration with no fallback, fixed to one platform and one channel by a CEO decision taken above the architecture | TRC-2026-0001 Dependencies |
| F-013 | Minutes per owner approval are established by no supplied source and no operating baseline exists | TRC-2026-0001 risk `RK-005`; SCOPE-2026-0001 scope decisions |
| F-014 | The implement-feature Design Gate is owned by `omn-architect` and `omn-tech-lead`, and the Producer Exclusion Rule places acceptance of this package with `omn-tech-lead` | `workflows/workflow-gate-matrix.md` |
| F-015 | The only existing structure in the repository is the agent framework's own module set; no application module exists, and the media company is a separate line of business from the product `context/product-context.md` describes | F-001; CEO decision record, first round |
| F-016 | SCOPE-2026-0001 records 12 in-scope items, 12 exclusions, 28 acceptance criteria and 12 scope decisions, at verdict `bounded` | SCOPE-2026-0001 metadata block |
| F-017 | The credential protection standard is recorded as owed by `architect` and needed before the routing capability is implemented | SCOPE-2026-0001 open questions; CEO decision record, still-open table |
| F-018 | One narration vendor class takes a licence over customer content including voice by default, and one image service forbids automated access outright | TRC-2026-0001 risk `RK-012` |
| F-019 | The company is operated by the CEO and sole owner, who is the mandatory approver of every publication and the weekly reviewer | SCOPE-2026-0001 Business Context; CEO decision record, individual-approval decision |
| F-020 | No published provider rate limit binds at the committed rate; the binding limits are the tier's monthly spend cap, the owner approval workload and per-channel licence registration | TRC-2026-0001 headroom finding |
| F-021 | The emergency tier recorded for high-stakes review is to hold the item and escalate to the CEO, and for several capabilities it is a non-AI substitute rather than a provider route | TRC-2026-0001 Dependencies |
| F-022 | The scope definition excludes consumer chat subscriptions, multiplied personal accounts and shared credentials as routable capacity | SCOPE-2026-0001 exclusion on capacity sources and its matching scope decision |

### 4.2 Assumptions

| ID | Assumption | Why needed | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | The operator's production experience is the .NET ecosystem with PostgreSQL and SQLite, and the surrounding tooling in this repository is Python | Operator familiarity is one of the criteria applied to the stack options in 5.2 | The familiarity tiebreaker no longer applies; the decisive criteria stand and the ranking is re-derived without it | omn-tech-lead |
| A-002 | Commercial provider interfaces return per-operation usage counts for input, output and cached units in their responses | Cost per operation is recorded as a measurement rather than an estimate | The meter records an estimate with a declared uncertainty, and `AC-003` is satisfied against estimates rather than measurements | omn-orchestrator |
| A-003 | Every capability class has at least three distinct commercially reachable routes at the time the position is admitted | The three-tier rule of `AC-001` is satisfiable as three provider routes for every capability | The emergency tier for the affected capability is a hold or a non-AI substitute rather than a route, which the typed union permits but which changes what `AC-001` is verified against | omn-orchestrator |
| A-004 | The deployment target is a single node operated by one person, with no multi-node coordination requirement | The job-claiming mechanism and the hosting shape in the stack options rest on it | The datastore-native claiming argument weakens and a broker-backed option is re-weighted upward | omn-tech-lead |
| A-005 | Wave 1 throughput is the recorded monthly capacity at thirteen items a month, so throughput does not differentiate the stack options | Throughput is excluded as a differentiating attribute among the stack options | The queue and datastore options are re-weighted for throughput and the recommendation may change | omn-qa |
| A-006 | The recorded history must remain answerable for years, and no supplied source states a retention period | The record carries a retention-class attribute from the outset rather than acquiring one later | A retention policy arriving later must be applied to entries that must not change, which an append-only store cannot do in place | omn-product-owner |
| A-007 | A dedicated secret store external to the application process is available to be provisioned in the selected hosting shape | The credential standard rests on a store the application does not implement | The broker would have to hold secrets inside the application boundary, which the Technical Gate rules forbid, and the standard would have to be re-derived | omn-tech-lead |

### 4.3 Constraints

| ID | Class | Constraint | Hard or negotiable | Source |
|---|---|---|---|---|
| C-001 | security | No capacity is obtained by means a provider's terms prohibit; every forbidden-source entry resolves to zero reachable routes | hard | S-002, F-005, F-022 |
| C-002 | structural | Every capability class carries exactly one primary, one secondary and one emergency route, each held under commercial terms, one production account per provider | hard | S-002, F-004 |
| C-003 | quality-attribute | No task runs below its declared quality floor; where no route at or above the floor is available the work is held or escalated | hard | S-002, F-016 |
| C-004 | functional | Every AI operation, including failed and retried ones, is accounted exactly once with its full attribution tuple | hard | S-003 |
| C-005 | functional | Cost per item and the monthly total are computed from recorded operations alone and compared against USD 77.41, split 34.42 and 42.99 | hard | S-003, S-012, F-009 |
| C-006 | compliance | For any asset the permission basis is answerable from the record alone, with no reference outside it | hard | S-004, S-018 |
| C-007 | security | The copyright responsibility can block but cannot release; the publishing responsibility cannot override a block nor release without a complete approval set; no role approves an exception to a control it is subject to | hard | S-005 |
| C-008 | security | Credentials live in a dedicated secret store, never in agent context, prompts, artifacts, logs or run evidence; a short-lived scoped token per task; release credentials reachable only by the publishing role and only after the gate passes; none shared across providers, accounts or channels; rotation and revocation without redeployment; every use audited | hard | S-006, F-017 |
| C-009 | functional | Work whose output is fully determined by its inputs and a rule carries zero AI cost and is named | hard | S-007, F-008 |
| C-010 | functional | Owner approval is a mandatory step in the publication gate that cannot be configured away, with elapsed minutes recorded from the first approval | hard | S-008, S-011, F-019 |
| C-011 | operability | Register, price, limit, budget and route changes take effect as configuration without redeployment, with prior values retained and validity dates carried | hard | S-017 |
| C-012 | migration | The repository is greenfield; nothing is migrated and no stack is assumed from the framework's own | hard | S-010, F-001, F-002 |
| C-013 | structural | This change publishes nothing, creates no channel or account and commits no spend | hard | S-009, S-014, F-010 |
| C-014 | compliance | The record is append-only; an amendment attempt is refused and the refusal is itself recorded | hard | S-019 |
| C-015 | operability | The system is operated by one person, so each additional operable component is a cost | negotiable | F-019, A-004 |
| C-016 | quality-attribute | An approval binds to an exact item version; an approval presented for another version is refused | hard | S-020 |
| C-017 | structural | The technology selection is a CEO decision at the Design Gate; this agent recommends and does not decide | hard | S-010, F-010, F-014 |
| C-018 | operability | Budget utilization is tracked at 50, 75, 90 and 100 per cent and each crossing raises an alert naming budget, threshold and utilization | hard | S-013 |
| C-019 | compliance | Controls recorded as non-cuttable cannot be disabled or reduced by a cost decision | hard | S-013, F-016 |
| C-020 | functional | Every unit of work resolves to the stages it passed; no failure ends unretried and unescalated; independent work progresses independently | hard | S-015 |
| C-021 | functional | Only measurable-now measures are reported; each deferred measure is named with the parameter it waits on and never shown as a value | hard | S-016 |
| C-022 | compliance | An absent, unverified or expired permission basis resolves as not permitted, and an unregistered channel blocks a releasable state | hard | S-018 |
| C-023 | structural | The design delivers the Wave 1 register, queue, workflow, record, accounting and routing set the request names, and no capability beyond it | hard | S-001, F-016 |

## Architecture and Component Design

### 5.1 Impacted Modules

| ID | Module | Impact type | Basis | Interfaces affected | Confidence |
|---|---|---|---|---|---|
| M-001 | Capability Router | extension | F-006 | `CapabilityRequest`, `CapabilityResolution` | confirmed |
| M-002 | Route Policy Register | extension | F-004 | `RouteAdmission`, `ForbiddenSourceRegister` | confirmed |
| M-003 | Provider Adapter Set | extension | A-003 | `ProviderAdapter`, transport credential attachment | confirmed |
| M-004 | Availability and Quota Ledger | extension | F-007 | `RouteAvailability`, circuit state transitions | confirmed |
| M-005 | Operation Meter | extension | A-002 | `OperationRecord` | confirmed |
| M-006 | Cost Rollup and Budget Monitor | extension | F-009 | `CostRollup`, `BudgetThresholdAlert` | confirmed |
| M-007 | Audit Log | extension | A-006 | `AuditEntry` append-only writer, amendment refusal | confirmed |
| M-008 | Authority Model | extension | F-016 | `ActionSet`, `PermissionEvaluation`, `ExceptionApproval` | confirmed |
| M-009 | Publication gate state machine | extension | F-019 | `GateTransition`, `Approval`, `GatePassToken` | confirmed |
| M-010 | Credential Broker | extension | F-017 | `ScopedTokenIssuance`, `TokenPresentation` | confirmed |
| M-011 | Registry Module | extension | F-016 | `Company`, `Channel`, `Department`, `Agent`, `AgentCapability`, `Model`, `ModelPrice` | confirmed |
| M-012 | Configuration Register | extension | F-016 | `ConfigurationVersion` with validity interval | confirmed |
| M-013 | Job and Workflow Engine | extension | F-016 | `WorkflowDefinition`, `Job`, `JobStage`, `StageOutcome` | confirmed |
| M-014 | Deterministic Task Library | extension | F-008 | the named rule-determined operations; no capability interface | confirmed |
| M-015 | Asset and Rights Ledger | extension | F-016 | `Asset` permission basis, `LibraryRegistration` | confirmed |
| M-016 | Reporting Surface | extension | F-016 | `MeasurableNowReport` | confirmed |
| M-017 | Persistence Adapter Set | extension | F-002 | `PersistencePort`, `JobClaimPort` | speculative |
| M-018 | Agent framework runtime module set | no-change-verified | F-015 | none; this design names no framework module as a dependency | confirmed |
| M-019 | Product surface described in `context/product-context.md` | no-change-verified | F-015 | none; a different line of business with its own measures | confirmed |

Boundary crossings recorded: consumer modules to M-001, which is the only path to an external capability; M-001 to M-010 for credential acquisition, handles only; M-003 to the provider boundary, the only egress; M-010 to the dedicated secret store, the only module holding that dependency; every module to M-017, the only path to durable state; M-013 to M-009, because the gate is a state inside the lifecycle. M-014 crosses to none of M-001, M-003 or M-010, which is the property D-005 records.

### 5.2 Options Considered

Criteria are applied in the fixed order of the reasoning procedure: hard-constraint satisfaction, impact surface measured as the count of modules carrying a contract or dependency change, reuse leverage measured as capabilities satisfied without new structure, quality-attribute satisfaction, migration burden, and operability. O-001 to O-004 are whole-foundation architectures and differ structurally on six dimensions at once: where capability access is mediated, how authority is enforced, where credentials live, how deterministic work is isolated, where the operation is recorded, and how the owner-approval step is held. O-005 to O-007 are the stack options, ranked separately because C-017 reserves that selection to the Design Gate; the architecture chosen from the first group is realized identically on any of them except at M-017 and the claim mechanism inside M-013. Technology names trace to F-003 and A-001.

| Option | Structural change | Hard constraints | Impact surface | Reuse leverage | Quality attributes | Migration burden | Operability | Outcome |
|---|---|---|---|---|---|---|---|---|
| O-001 | Direct provider calls from every consumer behind a shared client library; authority by call-site convention; credentials injected at start-up from process configuration; the consumer records the operation after the call returns; rule-determined work marked by naming convention; owner approval a configuration flag | eliminated: violates C-001, C-004, C-007, C-008, C-009 and C-010 | 16 consumers | 1, the shared client | every guarantee is caller discipline, and a forbidden source, an unmetered call and an unrotatable credential are all reachable | none, greenfield | fewest components | eliminated |
| O-002 | One mediating resolution boundary owns route selection, floor, credential acquisition, metering and audit emission; a closed action set with gate predicates and a transition table in code; broker-issued opaque scoped handles exchanged at the transport layer; the operation recorded at the boundary in the same transaction, per attempt; deterministic work isolated by dependency direction with a build-time boundary test; owner approval a transition state | pass | 3, being M-001, M-002 and M-003 | 2, M-005 and M-007 reused by every capability | floor, refusal, exactly-once and non-configurability are structural rather than configured | none, greenfield | one boundary plus the secret store | selected |
| O-003 | Consumers retain adapter dependencies behind a mandatory interceptor; an external policy engine evaluates authority at runtime; each module fetches its own credential per task; the consumer records the operation; a runtime guard rejects model calls from named tasks; an external service holds the approval step | rejected: weakens C-001, C-004, C-008, C-009 and C-010 to configured rather than structural properties, and costs C-015 | 5 | 2 | a directly constructed adapter is a working, unmetered path, and runtime-mutable rules make the mandatory step configurable in substance | none, greenfield | three components to operate | rejected |
| O-004 | One independent router per capability class with policy duplicated in each, otherwise as O-002 | rejected: C-001 and C-003 are enforced nine times with invisible drift, and C-015 pays for nine components | 9 | 0 | consistency of floor and refusal depends on nine implementations agreeing | none, greenfield | nine components to operate | rejected |
| O-005 | .NET runtime; PostgreSQL as the single datastore holding relational state, the append-only record and the durable job queue as a transactional claim table; one long-running service on one node | pass | 1, being M-017 | 3, the queue, temporal rows and exact decimal from the datastore | the transactional enqueue removes the dual write behind C-004, exact decimal serves C-005, and index aggregation answers the years-later query | none, greenfield | one datastore, one service, plus the secret store | recommended |
| O-006 | Python runtime; PostgreSQL for state; a dedicated broker for the durable job queue | rejected: the state change and the queue entry commit to separate systems, so C-004 needs an outbox, and C-015 pays for a second infrastructure component | 1, being M-017 | 2, temporal rows and exact decimal | weaker compile-time enforcement of the dependency direction D-005 relies on, and the exactly-once invariant moves into application code | none, greenfield | two infrastructure components plus the service | rejected |
| O-007 | .NET runtime; a single-file datastore holding state and an in-process queue | rejected: single-writer concurrency limits the independent progress C-020 requires, and the absence of an exact decimal type moves C-005 arithmetic into application code | 1, being M-017 | 2, the queue and temporal rows | cheapest to operate, but money rounding is invisible to the audit and concurrent claiming is constrained | none, greenfield | lowest, one file and one process | rejected |

### 5.3 Selected Approach

- Selected: O-002.

- Structural change: a mediating capability-resolution boundary, M-001, becomes the single egress path to every external capability. It reads the routing position and the forbidden-source register from M-002, the route availability from M-004, the quality and price facts from M-011, and obtains an opaque scoped handle from M-010; it writes the operation record through M-005 and the reason through M-007 in the same transaction as the work it performed. Consumers depend on a capability contract and on nothing below it. M-014 sits outside that dependency cone entirely. Authority, M-008, and the publication gate, M-009, are separate structures whose refusals are data-checked, and M-009 holds the owner-approval state in its transition table rather than in M-012. Durable state reaches the datastore only through M-017, which is the one boundary the stack decision moves.

- Rationale: the hard constraints C-001, C-003, C-004, C-007, C-008, C-009 and C-010 all take the same form, a property that must hold no matter which caller is involved. A property of that form is only reliably held by a structure that gives the caller no alternative path. On each of its six dimensions O-002 removes the alternative path rather than guarding it: it removes the direct provider dependency, removes the action from the permission set, removes the secret value from the caller's scope, removes the dependency that would make a model call expressible from rule-determined work, removes the window between the operation and its record, and removes the configuration key that could switch the approval off. O-005 is recommended because two of these properties are datastore-dependent: exactly-once under C-004 survives a crash only if the state change and the queue entry commit together, which a datastore-native claim table gives and a separate broker does not; and the arithmetic under C-005 must stay exact and re-derivable years later, which an exact decimal type in the datastore gives and an application-side approximation does not. Operator familiarity, recorded at A-001, agrees with that outcome but is applied only as the tiebreaker between O-005 and O-007, both of which trace to F-003 and A-001; it does not decide the ranking.

- Highest-scoring rejected alternative and why it lost: O-003. It has the second-smallest impact surface and its interceptor satisfies every hard constraint while the pipeline is correctly configured. It loses on quality-attribute satisfaction, because enforcement depends on every consumer obtaining its adapter through the pipeline, and a directly constructed adapter is a compiling, working, unmetered path to a provider. C-001 and C-004 would then be true of the system as configured rather than true of the system as built, and `AC-001` and `AC-003` verify the latter. Among the stack options the highest-scoring rejected alternative is O-007, which is the cheapest to operate, as C-015 rewards, but which weakens C-020 and C-005 in exchange.

- Tradeoffs accepted:
  - One mediating boundary is a single point of failure for every capability; accepted because that boundary is also the single point at which the floor, the refusal and the metering are enforced. R-004 carries the consequence.
  - Denormalizing item and period onto the cost row costs write-time work and a redundancy that must stay correct; accepted so the rollup under C-005 is an index aggregation rather than a join through lifecycle history years later. R-013 carries the consequence.
  - The dedicated secret store is a new external dependency and an additional operable component against C-015; accepted because the rotation-without-redeploy and never-in-context rules of C-008 are not satisfiable inside the application boundary. R-007 and R-018 carry the consequence.
  - Recommending one stack while the decision is reserved leaves part of the design provisional; accepted by confining the movable surface to M-017, the claim mechanism inside M-013 and the hosting shape. R-005 and R-019 carry the consequence.
  - Four independent enforcement layers for one property is deliberate duplication; accepted because each of the three refusals under C-007 is verified by demonstration, and a single layer leaves the demonstration resting on one code path.

- Stack recommendation, which the Design Gate decides and this agent does not, under C-017: O-005 is recommended, with O-006 and O-007 recorded as the alternatives considered. The architecture above is realized identically on any of the three except at M-017 and the claim mechanism inside M-013, which is the whole of the surface the decision moves. Recorded as a decision at D-002 and carried as an open decision at Q-001.

### 5.4 Decisions

| ID | Decision | Architecture-significant | Record |
|---|---|---|---|
| D-001 | Capability access is mediated by a single resolution boundary over a three-tier routing position and an independently checked forbidden-source register | yes | `architecture-decision-record-D-001.md` |
| D-002 | The stack recommended to the Design Gate is O-005, with O-006 and O-007 recorded as alternatives; the selection is the CEO's | yes | `architecture-decision-record-D-002.md` |
| D-003 | Least privilege is enforced by four independent structural layers rather than by policy checks | yes | `architecture-decision-record-D-003.md` |
| D-004 | Credential material never enters the caller's scope; the broker issues opaque scoped handles exchanged at the transport layer against a dedicated external store | yes | `architecture-decision-record-D-004.md` |
| D-005 | The zero-AI-cost property is a dependency-direction property verified by a build-time boundary test | yes | `architecture-decision-record-D-005.md` |
| D-006 | Operations are recorded at the resolution boundary in the same transaction as the work, per attempt, with the applied price row referenced | yes | `architecture-decision-record-D-006.md` |
| D-007 | Owner approval is a state in the gate transition table, absent from configuration, with elapsed minutes derived from two recorded timestamps | yes | `architecture-decision-record-D-007.md` |
| D-008 | Route availability is an explicit recorded state per route with recorded transitions, not a property inferred from recent failures | no | inline |
| D-009 | The emergency tier is a typed union of provider route, hold-and-escalate, and non-AI substitute, per F-021 | no | inline |
| D-010 | Price, limit, register, budget and route rows are temporal and never updated in place | no | inline |
| D-011 | A retry is its own operation record with its own outcome, so exactly-once means one record per attempt | no | inline |
| D-012 | The capability request names a capability class, a reasoning tier, a quality floor, a context requirement, a cost ceiling, a criticality and an attribution set, and never a provider or a model | no | inline |

## API and Data Model Impact

- API changes: no existing contract changes, because no application contract exists (F-001, F-002); no module in 5.1 carries a contract change, so no transition strategy is owed and none is recorded. The new contracts introduced are `CapabilityRequest` and `CapabilityResolution` in M-001, `RouteAdmission` and `ForbiddenSourceRegister` in M-002, `ProviderAdapter` in M-003, `RouteAvailability` in M-004, `OperationRecord` in M-005, `CostRollup` and `BudgetThresholdAlert` in M-006, `AuditEntry` in M-007, `ActionSet`, `PermissionEvaluation` and `ExceptionApproval` in M-008, `GateTransition`, `Approval` and `GatePassToken` in M-009, `ScopedTokenIssuance` and `TokenPresentation` in M-010, `ConfigurationVersion` in M-012, `WorkflowDefinition`, `Job` and `JobStage` in M-013, and `PersistencePort` and `JobClaimPort` in M-017.

- Contract compatibility notes: every new contract carries an explicit version from first emission, per the integration expectation in F-003, so a later change is a version rather than a silent shape change. `PersistencePort` and `JobClaimPort` are defined before any datastore-specific realization at P-008 precisely because the stack decision is pending; a consumer written against the datastore rather than the port would have to be rewritten when the Design Gate decides. Rollback position for each new contract while no consumer exists: withdrawal of the contract and its single realization, which is complete reversal. Once consumers exist, a superseding version coexists with its predecessor until every consumer names the new version, and the predecessor retires when the last reference to it is gone.

Capability routing, the blocking position recorded upstream as `RK-004` (D-001, M-001, M-002, M-004). The capability request declares what is needed, never who provides it (D-012): capability class, reasoning tier, quality floor, context requirement, cost ceiling, criticality, and the attribution set of item, channel, department and agent. The nine capability classes are those recorded in F-004: editorial reasoning, high-stakes review, bulk classification, narration, still images, generated cutaways, stock footage, music and sound effects, and web search.

Route resolution is a deterministic function with no model call, and is one of the named members of the C-009 set:

1. Reject the request outright if it names a forbidden source; the refusal is recorded and resolution ends (C-001).
2. Take the capability's admitted routes and keep only those whose recorded quality rating is at or above the request's floor and whose context capacity meets the request's requirement. This filter runs before tier ordering, so a route below the floor is never a candidate and no branch exists that admits one (C-003).
3. Remove routes whose provider account is disabled or revoked, and routes whose availability state is not `serving`.
4. Remove routes whose recorded unit price applied to the request's estimated units would exceed the cost ceiling, or would take the governing budget past 100 per cent (C-005, C-018).
5. Order the survivors primary, then secondary, then emergency, and resolve to the first.
6. On an empty survivor set, do not downgrade. A critical request is queued as `held-awaiting-capacity` carrying the floor, the reason and the routes tried, and escalates to the CEO on its declared hold timeout. A non-critical request is held on the same terms unless its own declared policy permits a reduced floor, in which case it runs at the reduced floor with the reduction recorded and the item barred from a releasable state until a role re-verifies it.

Route admission in M-002 is the second and independent refusal: a route whose provider account or interface matches a forbidden-source entry cannot be admitted to the table at all, so an admission-time check and a resolution-time check would both have to fail before a forbidden path existed (C-001).

Behaviour on each named condition, all recorded in M-007 with who, what, when and why:

| Condition | Detection | Behaviour | Recorded |
|---|---|---|---|
| Quota exhaustion | provider signal, or the account's spend and unit ledger reaching its recorded cap | state becomes `quota-exhausted` with the recorded reset point, and resolution restarts from the next tier at or above the floor | reason and reset point |
| Provider outage | consecutive adapter failures crossing the route's declared threshold | circuit opens, state becomes `outage`, resolution restarts, and a half-open probe runs on the recorded schedule | reason and probe schedule |
| Rate limit | provider signal | honour the signalled wait inside the request's deadline, then re-resolve rather than wait past it | attempt, wait and outcome |
| Quality degradation | a recorded verdict that delivered quality fell below the route's rated quality | the route's effective rating drops to the observed level, removing it from every request whose floor is above it | observation and new rating |
| No route at or above the floor | empty survivor set at step 6 | hold or escalate, never a downgrade | floor, reason, routes tried |
| Forbidden source named | register match at step 1, or refused admission | resolves to zero routes | refusal |

The emergency tier is a typed union (D-009): a provider route; hold-and-escalate, which is the recorded emergency position for high-stakes review (F-021); or a non-AI substitute, being motion graphics for a cutaway, held licensed stills for an image, publication without music, or a manual research queue for search, each of which routes to a member of M-014 and therefore carries zero AI cost.

The operating surface replaces the seat-quota bars the brief illustrated with what F-004 records: monthly spend against the tier's published cap, per-capability health and error rate, and per-item cost against the envelope. The consumer-subscription account pool appears nowhere in this design, and F-022 with the forbidden-source register of F-005 is the boundary that keeps it out.

Schema and data model changes. A new model; no existing data model is affected (F-001). Entities, their identity, and the relationships that carry the two acceptance queries:

| Entity | Identity | Key attributes | Relationships |
|---|---|---|---|
| Company | company | name, operating state | owns Channel, Department, Budget, Configuration |
| Channel | channel | platform, language, registration state | belongs to Company, has LibraryRegistration, attributed on AgentCost |
| Department | department | name, budget holder | belongs to Company, owns Agent, attributed on AgentCost |
| Agent | agent | workforce role, department, role reference | belongs to Department, holds AgentCapability and Role |
| AgentCapability | agent and capability class | declared quality floor, cost ceiling, criticality | associates Agent to CapabilityClass, read by M-001 |
| CapabilityClass | capability class | the nine classes of F-004, reasoning tier | targeted by Route and AgentCapability |
| ProviderAccount | provider account | provider, commercial terms basis, verification date, status | one per provider, owns Model, referenced by Route and by the credential holder |
| Model | model | provider account, rated quality, context capacity, modality | belongs to ProviderAccount, priced by ModelPrice, referenced by Route and AgentCost |
| ModelPrice | model, unit kind and validity interval | unit price, currency, source, verification date | temporal rows over Model, referenced by AgentCost |
| Route | capability class and tier | target union of model, hold-and-escalate or substitute; admission state; terms basis | binds CapabilityClass to Model or to a substitute |
| ForbiddenSource | kind and identifier | reason, evidence reference | checked at Route admission and at resolution |
| RouteAvailability | route and effective-from | state, reason, reset or probe point | state history over Route |
| WorkflowDefinition | workflow | stages, failure policy | instantiated by Job |
| Job | job | item, channel, workflow, lifecycle position | has JobStage, parent of AgentRun |
| JobStage | job and stage | outcome, attempt count, escalation state | belongs to Job |
| AgentRun | run | job stage, agent, started, ended, outcome | parent of AgentCost |
| AgentCost | operation | run, model, route, task reference, input units, output units, cached units, other unit counts, applied ModelPrice reference, computed cost, duration, outcome, measurement-or-estimate marker, and the attribution columns item, channel, department, agent and capability class | one row per attempt (D-011), the rollup source |
| Budget | budget | scope of department or channel, period, amount, threshold set | raises BudgetAlert |
| BudgetAlert | budget, period and threshold | utilization, raised-at | idempotent per budget, period and threshold |
| Asset | asset | item, source, creator, licence type and reference, commercial-use permission, modification permission, attribution requirement, platform restrictions, expiry, proof-of-licence reference, assessed risk, verifier identity, verification date | belongs to Item, joined to LibraryRegistration by library and channel |
| LibraryRegistration | channel and library | registered-on date | gates the releasable state |
| Role, Permission, RolePermission | role, action | action over resource kind, from a closed set | held by Agent, evaluated by M-008 |
| Control, ControlSubjectRole | control | non-cuttable marker, subject roles | subject role is required, blocks self-approval |
| ExceptionRequest, ExceptionApproval | exception | requester, approver, control, verdict | approver is not the requester and is not subject to the control |
| Block | block | item, placed-by role, reason, state | mutated only by the copyright permission set |
| Approval | item, item version and gate | approver, verdict, reason, presented-at, decided-at, elapsed minutes | unique per item version and gate |
| AuditEntry | entry | actor, action, subject, time, reason, inputs reference, outputs reference, decision, cost reference, risk, retention class, previous-entry hash, entry hash | append-only chain |
| Configuration | key, scope and validity interval | value, version, changed-by, reason | temporal rows, prior values retained |

Indexes, named against the queries that need them: AgentCost by item and occurred-at covering cost and units, for the cost-per-item rollup; AgentCost by channel and period and by department and period covering cost, for budget utilization and the monthly total; AgentCost by capability class and period and by model and period, for the breakdown `AC-003` verifies; Asset by item, for the per-item permission set; LibraryRegistration by channel and library, for the registration join; AuditEntry by subject and time and by entry hash, for the permission answer and the chain check; Approval by item, item version and gate, unique, for the version binding of C-016; BudgetAlert by budget, period and threshold, unique, for the idempotent crossing of C-018; Route by capability class and tier, unique, for the three-tier rule of C-002; and RouteAvailability by route and effective-from descending, for current state in one seek.

Acceptance query one, cost per video rolled up from recorded operations (C-005, `AC-004`). AgentCost carries the item and the period directly, written at the same time as the row and derived from the run's job, so the rollup is a single aggregation over the item and occurred-at index with no join through lifecycle history. The monthly total is the same aggregation over the period, presented against 77.41 with the 34.42 metered and 42.99 standing split named and the variance stated. Because the row stores the applied ModelPrice reference rather than only the computed figure, the arithmetic stays re-derivable after the price has changed, which is what makes the record answerable years later (A-006).

Acceptance query two, for any asset why the company was permitted to use it (C-006, `AC-008`). The permission basis is held on the Asset row itself rather than by reference to an external system, so the answer is Item to Asset for the basis, Asset to LibraryRegistration by library and channel for the registration proof, and Asset to AuditEntry by subject for the verification event with its verifier and date. Three indexed reads and no external lookup. An Asset missing any basis field resolves as not permitted rather than as permitted (C-022), so the query cannot return an incomplete answer that reads as a complete one.

Migration. Initial creation only; there is nothing to migrate (C-012, F-001). Direction is forward from an empty store. Reversibility: while no production record exists, teardown is complete reversal; once the first append-only entry is written the store is not correctable in place (C-014), so reversal is restore-to-point from the safeguard established at P-012, and the routing position reverts by re-admitting its retained prior configuration version (C-011, D-010). Reader and writer behaviour during transition: none, because no reader and no writer exist before creation; subsequent temporal changes to ModelPrice and Configuration are additive rows, so a reader of a prior validity interval continues to resolve the value that was in force.

## Reusable Components and Reuse Rationale

| Capability | Candidate | Outcome | Rationale |
|---|---|---|---|
| Capability-to-provider routing | existing components, standard library, native platform capability, installed dependencies | none-found | All four candidate kinds searched; F-001 records no manifest and no installed dependency, and the framework module set serves a different line of business (F-015). The position exists as a recorded table upstream but as no component |
| Circuit breaking, retry and backoff | native platform resilience capability of the selected runtime, installed dependencies | reuse-as-is | A resilience capability is native to both candidate runtimes; M-004 holds the recorded state and delegates the mechanism rather than implementing it |
| Durable job queue | native datastore capability, a dedicated broker, standard library, installed dependencies | reuse-as-is | O-005 reuses the datastore's own transactional claim capability, which is what removes the dual write behind C-004; O-006 would reuse a broker instead |
| Secret storage | a dedicated secret store as a native platform capability, existing components, standard library, installed dependencies | reuse-as-is | The store is provisioned, not built (A-007). Building one inside the application is what C-008 forbids, and the product choice is Q-005 |
| Short-lived scoped token issuance | native lease or dynamic-secret capability of the secret store, standard library | reuse-extended | The store's own capability is extended with the task binding, ttl and use bound C-008 requires, and the extension is the M-010 contract rather than a new store |
| Append-only, tamper-evident record | native datastore write constraints, standard library hashing, existing components | reuse-extended | Datastore constraints plus standard-library hashing give the chain, and M-007 extends them with the amendment refusal and the refusal record C-014 requires |
| Role and permission evaluation | existing components, standard library, native platform authorization capability, installed dependencies | rejected | The native authorization capabilities of both candidate runtimes evaluate an open set of claims, whereas C-007 needs a closed enumerated action set so that an absent action is unrepresentable, which those capabilities cannot express |
| Workflow state machine | existing components, standard library, native platform capability, installed dependencies | none-found | All four candidate kinds searched; none found (F-001). A general engine was considered and is not proposed, because the lifecycle of C-020 is small and the gate transition table must be code for C-010 |
| Per-operation metering | provider billing export, standard library, existing components, native platform telemetry | rejected | A billing export cannot carry the attribution tuple of C-004 nor count failed attempts, and platform telemetry is not a durable accounting record, so neither candidate can hold the property the constraint requires |
| Temporal configuration with retained prior values | native datastore capability, standard library, existing components, installed dependencies | reuse-extended | Validity intervals are a datastore capability, and M-012 extends them with the change reason and the no-redeploy read path C-011 requires |
| Exact decimal money arithmetic | native datastore type, standard library decimal type | reuse-as-is | Reused from the datastore in O-005 and O-006; in O-007 it falls back to the standard library, which is the quality-attribute gap recorded against that option |
| Reporting surface | existing components, standard library, native platform capability, installed dependencies | none-found | All four candidate kinds searched; none found (F-001). M-016 is a read over M-005 and M-006 and introduces no new store |
| Deterministic media, timing and hashing operations | standard library, installed dependencies, native platform capability, existing components | reuse-as-is | The operations F-008 names are standard-library and platform work, and M-014 is the boundary that holds them rather than a reimplementation of them |

## Operational Considerations

- Logging and observability updates: M-007 is the system of record and is written in the same transaction as the state change it records, so no observable action exists without its entry (C-014, M-005, M-006). The operating surface carries what F-004 records, being monthly spend against the tier cap, per-capability health and error rate, and per-item cost against the envelope, and M-016 reports only the measurable-now set, naming each deferred measure with the parameter it waits on (C-021). Route state transitions, credential issuances and presentations, gate verdicts, refusals and held work each produce an entry carrying who, what, when, why, inputs, outputs, decision, cost and risk.

- Error handling strategy: failures at the resolution boundary are classified as transient, rate-limited, quota-exhausted, outage, quality-degraded, or refusal (M-001, M-004, C-020). Transient and rate-limited failures retry at the transport layer inside the request's deadline; quota and outage failures do not retry on the same route but re-resolve from the next tier at or above the floor; a refusal never retries. Every failure carries a recorded reason, and every failed step ends either retried within its declared policy or escalated, so none ends in neither, which is what `AC-023` measures. A held request is a first-class state with a floor, a reason and a hold timeout, not an error. Each retry is its own operation record (D-011), which is how exactly-once under C-004 stays well defined under retry.

- Security considerations: the credential standard C-008 and F-017 require, which discharges the credential-protection question the scope definition and the Framing Gate record as owed by this agent (M-010, M-003, D-004). A dedicated secret store sits outside the application process (A-007); M-010 is the only module holding a dependency on it, every other module depends on the scoped-handle contract, and the direction is verified by the same build-time boundary test D-005 uses. M-010 issues a short-lived scoped handle per task, bound to the job and stage identity, the capability, the route's provider account, a ttl and a use bound; the handle is opaque and carries no secret value. M-003 exchanges the handle for the provider credential at the transport layer, after the request object has been assembled, so the credential is never in scope at prompt-assembly time and cannot be serialized into a prompt, an artifact, a log or run evidence. Log, artifact and evidence emission passes a redaction boundary keyed on held credential identifiers, and a sweep over those surfaces asserts zero occurrences, which is the verification `AC-013` names. Release credentials are a distinct class whose issuance predicate requires the actor's role to be the publishing role, a gate-pass token naming the exact item version, and zero open blocks, and they are issued nowhere else (C-007, C-010). The credential holder key is the pair of provider account and channel and is unique, so no secret reference can be held by a second holder and sharing is unrepresentable rather than discouraged. Rotation and revocation are store-side writes; M-010 resolves the secret reference at issuance time and never at start-up, so a rotation takes effect on the next issuance and a revocation invalidates live handles at their next presentation, with no redeployment and no restart (R-007). Every issuance and every presentation writes an audit entry carrying the handle identity and the holder, never the value.

  Least privilege under C-007 (M-008, M-009, D-003) is enforced in four independent layers. First, the closed enumerated action set, in which the copyright role's set holds the block-place, block-clear and asset-verify actions and does not hold the publish action, and the publishing role's set is disjoint on every Block action, so overriding a block is not an expressible action. Second, the gate predicate on publish, which requires an open-block count of zero and a complete approval set including the owner approval bound to the exact version. Third, the transition table, in which the published state is reachable only from approved, which is reachable only from awaiting-owner-approval, with no bypass transition and no representation in M-012. Fourth, the credential issuance predicate above. Exception approval carries a write-time invariant: the approver may not be the requester, may not be a subject role of the control being excepted, and may not hold the action the exception would grant; the subject-role relation is a required attribute, so a control with no subject role cannot be admitted (R-008). Non-cuttable controls carry a marker no cost decision can clear, and every cost decision records the controls it did not touch (C-019).

  Code first and AI when necessary under C-009 (M-014, D-005): the named set is the one F-008 records, being render, mux, encode, transcode, loudness normalisation and aspect conform; subtitle timing by forced alignment, caption emission and chapter timestamp arithmetic; the asset and rights ledger and the join proving every published asset carries a licence record; the library registration state machine and its precondition on first publication; the publication gate's blocking, release and approval-token verification; schedule arithmetic for the operating week and the production buffer; cost metering, the per-item rollup and the headroom calculation against the tier cap; provider routing, quota and spend accounting, failover selection, retry, backoff and circuit breaking; exact and near-duplicate detection by content and perceptual hashing; metadata population from templates and thumbnail variant compositing; and audit logging with the evidence each gate verdict rests on. The idempotent upload F-008 also names is excluded here by C-013. All of these live in M-014, which holds no dependency on M-001, M-003 or M-010, so a model call is not expressible from them, and the zero-AI-cost check `AC-017` names is a measurement over cost rows attributed to that set plus the boundary test.

  The owner approval step under C-010 (M-009, D-007): awaiting-owner-approval is a state every item passes; the clock starts on state entry when the approval package is presented and stops at the verdict, and the elapsed minutes are derived from the two recorded timestamps rather than entered, so the figure is a measurement from the first approval onward (F-013). Send-backs are recorded with their reason. The approval row is unique per item, item version and gate, so an approval cannot be reused for a changed version (C-016). The measured distribution is the baseline from which the clean-record threshold is later proposed; this design instruments it and sets no number, because F-013 records that no supplied source establishes one.

- Performance considerations: throughput is not a binding quality attribute at the committed rate (F-020, A-005), so the performance constraints that matter are queryability over a record that must answer years later (A-006, C-005) and the cost of the write path. The rollup is an index aggregation over denormalized item and period attribution rather than a join through lifecycle history (R-013), the current route state is one seek on route and effective-from descending, and the permission answer is three indexed reads. The write path adds one operation record and one audit entry per attempt inside the transaction that performs the work, which is the cost C-004 buys exactly-once with.

- Deployment and operability impact: one deployable service and one datastore under O-005, against C-015. Configuration changes to prices, limits, registers, budgets and routes take effect without redeployment through the temporal rows of M-012 (C-011), and credential rotation and revocation likewise (C-008). The dedicated secret store is the one additional operable component and is the accepted cost recorded in 5.3. Nothing in this change publishes, creates an account or commits spend (C-013).

## Delivery Plan

### Sequencing Constraints

| ID | Constraint | Modules | Prerequisites | Reason | Binds |
|---|---|---|---|---|---|
| P-001 | The routing position and the forbidden-source register are settled and admitted before any module resolves a capability | M-001, M-002, M-003 | none | Resolution is total only over an admitted table, and a consumer built before it embeds a provider choice the position must own | T-001, T-002 |
| P-002 | The append-only record contract exists before any module that must record a refusal | M-007 | none | Refusals taken by earlier components must be recorded, and a record delivered after them leaves them unrecorded | T-009, T-010 |
| P-003 | The operation-accounting contract, its attribution tuple and its unit set exist before the boundary emits operations | M-005, M-001 | P-002 | An operation recorded without the full tuple cannot be re-attributed afterwards | T-003, T-004 |
| P-004 | The closed action set and the role assignments exist before the gate predicates are realized | M-008, M-009 | P-002 | The gate evaluates actions that must already be enumerable, and an open set makes the publisher's absent block action unprovable | T-012, T-013, T-014 |
| P-005 | The credential standard and the broker issuance contract exist before any adapter holds a provider credential | M-010, M-003 | P-001, P-002 | An adapter built first would take the credential as a parameter, which is the shape the standard forbids | T-015, T-016, T-017 |
| P-006 | The temporal configuration model exists before prices, limits, budgets or routes are stored | M-012, M-011, M-002 | P-002 | A value stored without validity dates cannot be re-derived at audit time, and retro-fitting temporality alters records that must not change | T-020, T-021, T-022 |
| P-007 | The dependency direction keeping M-014 free of M-001 and M-003 is established and verified before deterministic work is realized | M-014, M-001, M-003 | P-001 | The zero-cost property is a dependency-direction property, and verifying it afterwards finds a violation only once cost has been incurred | T-018 |
| P-008 | The persistence and job-claim ports are defined before any datastore-specific realization | M-017, M-013 | P-006 | The stack selection is taken at the Design Gate, and modules written against a datastore rather than a port would be rewritten | T-002, T-024 |
| P-009 | The work lifecycle and its failure policy exist before the publication gate is placed inside it | M-013, M-009 | P-004 | The gate is a state in the lifecycle, and a gate defined outside it holds a parallel state the lifecycle cannot account for | T-023, T-024 |
| P-010 | The asset permission-basis record and the library registration state exist before the rights precondition of the gate is realized | M-015, M-009 | P-002, P-006 | The precondition evaluates a record that must already resolve as not permitted when incomplete | T-006, T-007, T-008 |
| P-011 | Budget scopes and thresholds exist before the threshold evaluation of the meter is realized | M-006, M-005 | P-003, P-006 | A crossing is evaluated against a budget that must exist at the moment of the write | T-026, T-005 |
| P-012 | The backup and restore position of the record store is established before the first append-only entry is written | M-007, M-017 | P-008 | An append-only store has no in-place correction, so the reversibility safeguard must precede the irreversible step | T-010, T-030 |
| P-013 | The reporting surface is realized only after the inputs of its measure set exist | M-016 | P-003, P-011 | A surface built first would report placeholders, which `AC-026` forbids | T-025, T-019 |
| P-014 | The protection of non-cuttable controls and the register of rule-determined work are in place before the wave is reviewed against the credential and least-privilege constraints | M-008, M-014, M-006 | P-004, P-007, P-011 | The review assesses controls that must already exist in the form it assesses them in | T-011, T-027, T-031, T-032 |
| P-015 | The demonstration and measurement sets are designed against the contracts of P-001 to P-006 rather than against their realizations | M-001, M-005, M-008, M-010 | P-001, P-003, P-004, P-005 | A demonstration written against a realization verifies that realization rather than the constraint | T-028, T-029, T-030 |

### Test Strategy Focus Areas

- Forbidden-source refusal demonstrated at both admission and resolution, and demonstrated to be independent of one another.
- Both primary and secondary made unavailable, per capability class, including the capabilities whose emergency tier is a hold or a non-AI substitute.
- The floor property verified as the absence of a below-floor candidate rather than as a rejected candidate: a request whose floor exceeds every route resolves to held, with the floor and the routes tried recorded.
- Exactly-once accounting under an induced crash between the operation and its record, and under retry, confirming one record per attempt.
- The three least-privilege refusals, each demonstrated against all four enforcement layers rather than the first that fires.
- The credential sweep over agent context, prompts, artifacts, logs and run evidence; rotation and revocation taking effect without a restart; a release-credential request from another role and one made before the gate passes.
- Zero AI cost over the named deterministic set, together with the build-time boundary test that makes the call inexpressible.
- Approval version binding, and the elapsed-minutes derivation from the two recorded timestamps rather than from an entered value.
- Budget crossings at each of the four thresholds in turn, idempotent per budget and period.
- The two acceptance queries answered from the record alone, including after a price row has been superseded.
- The amendment refusal on the append-only record, and the refusal itself appearing as an entry.

### Rollout and Rollback

- Rollout: nothing is published, no channel or account is created and no spend is committed (C-013), so rollout is the creation of the record store, the admission of the routing position and the forbidden-source register as configuration, and the exercise of the demonstration and measurement sets before any capability is resolved against a live account.
- Rollback: while no production record exists, teardown is complete reversal. After the first append-only entry, reversal is restore-to-point from the safeguard established at P-012; the routing position reverts by re-admitting its retained prior configuration version; and prices, limits and budgets revert the same way, because none is updated in place (D-010, C-011).

## Risks and Mitigations

| ID | Class | Trigger | Impact | Likelihood | Affects | Mitigation | Owner |
|---|---|---|---|---|---|---|---|
| R-001 | security | A provider's terms change, or a route's terms basis is not re-verified within the recorded cadence | Capacity obtained by prohibited means, and production accounts at termination risk | medium | M-002 | Every Route carries a terms basis and a verification date, and a route past its cadence is not admitted; performed at P-001 | omn-orchestrator |
| R-002 | security | A new emission path is added that does not pass the redaction boundary | A credential value reaches a prompt, artifact, log or run evidence, breaching C-008 | medium | M-010 | Handles rather than values, so no value is in scope at emission, plus the sweep; P-005 | omn-dev-2-reviewer |
| R-003 | structural | A convenience dependency from M-014 onto M-001 or M-003 is added during implementation | A rule-determined task calls a model, `AC-017` fails, and cost rises silently | medium | M-014 | Dependency direction verified as a build-time boundary test, plus the cost measurement over the named set; P-007 | omn-dev-2-reviewer |
| R-004 | operability | Primary and secondary are both unavailable for a capability whose emergency tier is hold-and-escalate | The item stops and the owner is escalated, against the committed rate | medium | M-001 | Held is a first-class state with a reason and a declared timeout, and capability health shows it before the deadline; P-001 | omn-tech-lead |
| R-005 | contract | The Design Gate selects a stack other than the recommendation | M-017 and the claim mechanism in M-013 are realized differently | medium | D-002 | The movable surface is confined to M-017, the claim mechanism and hosting, and is named, with ports preceding realization; P-008 | omn-tech-lead |
| R-006 | migration | A ModelPrice row is superseded and the superseded row is not retained | Cost per item cannot be re-derived and `AC-021` fails | low | M-011 | Prices are temporal rows never updated in place, and the cost row stores the applied price reference; P-006 | omn-dev-1-implement |
| R-007 | operability | An adapter resolves and caches a credential at start-up | Rotation or revocation requires a restart, breaching C-008 | medium | M-010 | Issuance per task with a ttl shorter than the rotation interval, and no start-up resolution path in the broker contract; P-005 | omn-dev-2-reviewer |
| R-008 | security | A control is admitted without its subject-role relation | A role approves an exception to a control it is subject to and `AC-012` fails | medium | M-008 | The subject-role relation is a required attribute, so such a control cannot be admitted; P-004 | omn-dev-2-reviewer |
| R-009 | operability | Assumption A-002 proves false at first-hand adapter verification | Cost is recorded as an estimate rather than a measurement | low | M-005 | The cost row carries a measurement-or-estimate marker so the report states which; P-003 | omn-orchestrator |
| R-010 | structural | Assumption A-003 proves false at route admission for some capability | That capability's emergency tier is a hold or a substitute rather than a third provider route | medium | M-002 | The emergency tier is a typed union, so the position is expressible and visible rather than fudged; P-001 | omn-tech-lead |
| R-011 | operability | Assumption A-004 proves false and more than one node is required | Datastore-native claiming and the single-node hosting shape are re-weighted | low | D-002 | Claiming sits behind the job-claim port, so a broker-backed realization substitutes without changing consumers; P-008 | omn-tech-lead |
| R-012 | security | A later change edits the gate transition table | The mandatory owner-approval step of C-010 is lost | low | M-009 | The state is code rather than configuration, so removal is a code change the Review Gate sees, and release credentials still require the gate-pass token; P-004 | omn-dev-2-reviewer |
| R-013 | performance | The operation record reaches a volume at which an unindexed scan is the rollup path | The rollup `AC-004` verifies becomes expensive | low | M-006 | Item and period denormalized at write time with the covering indexes named in the data model; P-003 | omn-dev-1-implement |
| R-014 | structural | A later decision co-locates the company system with the framework repository | M-018 becomes an impacted module and the separation recorded at F-015 no longer holds | low | M-018 | The design names no framework module as a dependency, so the boundary is already clean, and the question is carried at Q-007 | omn-tech-lead |
| R-015 | security | A retention expectation arrives after records exist, as assumption A-006 anticipates | An append-only store cannot apply it to entries in place | medium | M-007 | Every entry carries a retention class from the first write, so a policy applies prospectively without altering entries, and the question is carried at Q-003 | omn-product-owner |
| R-016 | delivery | Assumption A-001 proves false and the operator's production experience is not as recorded | The familiarity tiebreaker among the stack options no longer applies | low | D-002 | Familiarity is applied only as the tiebreaker and the decisive criteria are stated separately, so the ranking survives its removal | omn-tech-lead |
| R-017 | performance | The first metered item shows volume materially above assumption A-005 | The queue and datastore options are re-weighted for throughput | low | M-013 | Metering of the first item precedes the sustained rate, and the ports keep the substitution local; P-008 | omn-qa |
| R-018 | contract | Assumption A-007 proves false and no dedicated secret store is available in the selected hosting shape | The credential standard of D-004 cannot be realized as designed | low | M-010 | Carried at Q-005 as a Design Gate decision taken together with the stack, so the two are not decided apart; P-005 | omn-tech-lead |
| R-019 | structural | The shape of M-017 is recorded as speculative while the stack decision is open | Any realization written against it before the gate decides may be discarded | medium | M-017 | The port contracts are defined and the realization is deferred until the gate decides; P-008 | omn-tech-lead |

## Estimate and Confidence

- Overall: `L` (confidence: medium). A cross-boundary change introducing seventeen new modules and one new external dependency, with no migration burden at all because the repository is greenfield (C-012, F-001). It is not `XL`, because no structural uncertainty remains in the stack-independent surface, and the one pending selection is bounded to a named surface and is a gate decision rather than an unresolved design question.

- Breakdown:
  - Capability routing and provider access, M-001 to M-004: `L` (confidence: medium). The resolution function is small and deterministic, and the weight sits in the admission rules, the availability state machine and the typed emergency tier.
  - Accounting and budgets, M-005 and M-006: `M` (confidence: medium). Bounded by one contract and its indexes, with the exactly-once property under crash as the hard part.
  - Record and configuration, M-007 and M-012: `M` (confidence: medium). Append-only chaining and temporal rows are reused capabilities extended with refusal semantics.
  - Authority and publication gate, M-008 and M-009: `L` (confidence: medium). Four enforcement layers, each demonstrable, plus the exception invariant.
  - Credential broker, M-010: `M` (confidence: low). Depends on assumption A-007 and on Q-005, and the low confidence follows the unconfirmed assumption.
  - Registries, lifecycle and rights, M-011, M-013 and M-015: `M` (confidence: medium).
  - Deterministic library and reporting, M-014 and M-016: `S` (confidence: medium). Mostly reused standard-library and platform work behind one boundary.
  - Persistence adapters, M-017: `M` (confidence: low). Speculative until the Design Gate decides, as R-005 and R-019 record.

- Scope assumptions: the estimate covers the nineteen modules of 5.1 within the boundary C-023 states and excludes everything listed as out of scope in the Objective section; it assumes the routing position is admitted as recorded in F-004 with the emergency tiers of F-021; it assumes a single node and one operator (A-004); it assumes provider responses carry usage counts (A-002); and it assumes the workload of A-005.

- Uncertainty drivers: the pending stack selection and its effect on M-017 and on the claim mechanism (Q-001, R-005); the availability of a dedicated secret store in the selected hosting shape (A-007, Q-005, R-018); whether every capability has three commercial routes (A-003, R-010); whether usage counts are returned (A-002, R-009); and the absence of any operating baseline, which F-013 records and which is why no figure here is expressed as a duration.

## Open Decisions and Escalations

| ID | Question | Blocking | Owner | Affects | Consequence |
|---|---|---|---|---|---|
| Q-001 | Which stack does the owner select at the Design Gate: the recommended O-005, or O-006 or O-007 as recorded? | no | omn-tech-lead | D-002 | O-005 leaves the design as written; O-006 realizes the job-claim port against a broker and C-004 then needs an outbox; O-007 moves money arithmetic into application code and limits the independent progress C-020 requires |
| Q-002 | Is the deployment single-node and single-operator, as assumption A-004 states? | no | omn-tech-lead | M-013 | Single node leaves the recommendation standing; more than one node re-weights datastore-native claiming and materializes R-011 |
| Q-003 | What retention and privacy expectations apply to the recorded history, given assumption A-006? | no | omn-product-owner | M-007 | A stated policy applies prospectively through the retention class; no policy leaves entries accumulating with no defensible basis for removal and R-015 standing |
| Q-004 | Does every capability class have three distinct commercial routes at admission, and for which is the emergency tier a hold or a substitute? | no | omn-orchestrator | M-002 | Three routes everywhere lets `AC-001` be verified against provider routes; fewer means it is verified against the typed union, which must be accepted explicitly rather than assumed |
| Q-005 | Which dedicated secret store is provisioned, given that it is a new external dependency bounded by the stack decision? | no | omn-tech-lead | M-010 | A store with a lease or dynamic-secret capability lets M-010 extend it as designed; none available means the credential standard must be re-derived and D-004 reopens |
| Q-006 | Do commercial provider interfaces return per-operation usage counts, as assumption A-002 states? | no | omn-orchestrator | M-005 | Returned means cost is a measurement; not returned means cost is an estimate and every report must say so |
| Q-007 | Is the company system hosted in this repository alongside the framework, or in its own? | no | omn-tech-lead | M-018 | Separate leaves M-018 and M-019 no-change-verified; co-located makes the framework runtime an impacted module and the boundary is re-analyzed |
| Q-008 | The seven decision records D-001 to D-007 stand at Proposed and require Design Gate acceptance before the realization tasks begin | no | omn-tech-lead | D-001 | Acceptance binds delivery to the sequencing constraints; non-acceptance returns the affected option to the evaluation table in 5.2 |

No authority or boundary error event occurred in this run. The stack selection was not taken here because C-017 reserves it, and it is recorded at Q-001 rather than decided, which is the charter's required response and not an error event.

## Sign-off

- Architect: `architect` produced this package and does not sign its own gate. Unsigned.
- Design Gate: owned by `omn-architect` and `omn-tech-lead` (F-014). Under the Producer Exclusion Rule the producing role cannot accept, so acceptance of this package and of the seven records at Proposed rests with `omn-tech-lead`. Unsigned.
- Tech Lead: `omn-tech-lead`. Unsigned.
- QA: `omn-qa`, for the verification implications and the test focus areas in the Delivery Plan. Unsigned.
- Product confirmation: `omn-product-owner`, for Q-003 only, which restates a scope-level open question. Unsigned.
- Technology selection: reserved to the owner under C-017 and recorded at Q-001. Unsigned.
