```yaml
plan:
  planId: PLAN-2026-0001
  sourceInputs:
    - type: feature-request
      reference: tasks/MC-2/input.md
  producedBy: planner
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  inputDigest: sha256:82d439273250e194b15f25dfa1c38ab1
  contextDigest: sha256:62e7676afa2df10b3af32629f294e88a
```

## Executive Summary

This plan decomposes the first delivery wave of the company foundation for the CEO and sole owner: the accounting, permission, authority and configuration record that one channel's operation runs inside, bounded by `SCOPE-2026-0001` (twelve in-scope items, twelve exclusions, twenty-eight acceptance criteria). Success is that cost per item and the monthly total are computable from the record against the approved envelope, that the permission basis of every asset is answerable from the record alone, that the reserved approval and the separation of duties cannot be bypassed, and that no capability is served by a source a provider's terms forbid. The work is thirty-two tasks in eight execution waves: eight settling tasks owned by `architect`, nineteen realization tasks owned by `omn-dev-1-implement`, three verification tasks owned by `omn-qa`, one review task and one documentation task. Wave 1 is the routing position, the permission-basis record contract, the record contract and the two verification-design tasks, so the one recorded blocker to building is settled before anything that consumes capability access begins. The highest-impact risk is `R-001`: a source the providers' terms forbid re-entering the routing position, which would put the production accounts at termination risk; the consumer-subscription account pool is excluded at `X-012` and appears nowhere in this plan. Nothing in this wave publishes, creates a channel or account, or commits spend. Effort is stated as relative complexity only, because no operating baseline exists. Status is `complete`: eleven open questions stand and none blocks decomposition.

## Business Objectives

- `BO-1` Cost accountability: the CEO can state cost per item and the monthly total for one channel against the approved envelope of USD 77.41 from the company's own record, measured by the rollup produced from recorded operations alone and its stated variance. Traces `S-002`, `S-011`, `S-012`.
- `BO-2` Rights defensibility: any party who asks can be shown, for a finished item, the basis on which the company was permitted to use each of its assets, measured by that question being answerable from the record alone. Traces `S-003`, `S-004`.
- `BO-3` Lawful capacity: the company consumes no capability through a source a provider's terms forbid, measured by every forbidden source resolving to no reachable route. Traces `S-001`.
- `BO-4` Reserved authority: the owner's approval and the separation of duties cannot be bypassed by any role, measured by each forbidden privilege act being refused and the refusal recorded. Traces `S-005`, `S-006`, `S-008`.
- `BO-5` Measured relaxation: the CEO can later decide whether to relax per-publication approval from measured minutes rather than from assumption, measured by every approval carrying elapsed minutes from the first one onward. Traces `S-008`.
- `BO-6` Operable configuration: the company can state and change what it operates without changing how it operates, measured by a price, limit or budget change taking effect as configuration and by no unit of work being held behind something it does not depend on. Traces `S-009`, `S-010`, `S-012`.

## Technical Objectives

- `TO-1` Route resolution is total over the capabilities the company consumes and empty over the forbidden-source list. Verified by `AC-001` and `AC-002`. Traces `BO-3`.
- `TO-2` Every operation, including failed and retried ones, is accounted exactly once with complete attribution. Verified by `AC-003`. Traces `BO-1`.
- `TO-3` Cost rollups and budget utilization are computed from recorded operations alone. Verified by `AC-004` and `AC-027`. Traces `BO-1`.
- `TO-4` An absent, unverified or expired permission basis resolves as not permitted, and an unregistered channel blocks the same state. Verified by `AC-005`, `AC-006` and `AC-007`. Traces `BO-2`.
- `TO-5` The recorded history is append-only: an amendment is refused and the refusal is itself recorded. Verified by `AC-009`. Traces `BO-2`.
- `TO-6` Authority is least-privilege and self-approval of a control a role is subject to is unreachable. Verified by `AC-010`, `AC-011` and `AC-012`. Traces `BO-4`.
- `TO-7` Credentials are absent from every surface the company produces, are scoped and short-lived, are held for one provider, account or channel only, and rotate and revoke without redeployment. Verified by `AC-013` to `AC-016`. Traces `BO-4`.
- `TO-8` An approval record binds to an exact version of an item and carries its elapsed minutes. Verified by `AC-018` and `AC-019`. Traces `BO-4` and `BO-5`.
- `TO-9` Work whose output is fully determined by its inputs and a rule completes with zero AI cost attached. Verified by `AC-017`. Traces `BO-1`.
- `TO-10` Register, price, limit and budget changes take effect as configuration, with prior values retained and verification dates carried. Verified by `AC-020` and `AC-021`. Traces `BO-6`.
- `TO-11` Every unit of work resolves to the stages it passed, no failure ends unretried and unescalated, and independent work progresses independently. Verified by `AC-022`, `AC-023` and `AC-024`. Traces `BO-6`.
- `TO-12` A measure that needs a revenue parameter is reported as not yet available with the parameter named, never as a value. Verified by `AC-025` and `AC-026`. Traces `BO-1`.

## Scope

### In Scope

Statements `S-001` to `S-012` are the in-scope items of `SCOPE-2026-0001`, carried with identical identifiers so that this plan and the boundary resolve against one another; their full text is referenced by that identifier and digest rather than restated. The boundary's acceptance criteria are cited here as `AC-001` to `AC-028`, where `AC-nnn` is that artifact's `A-nnn`; the `A-nnn` register in this plan is assumptions only.

- `S-001` Capability-to-provider routing under commercial terms, with primary, secondary and emergency routes and no reachable forbidden source — `T-001`, `T-002`, `T-028`, `T-030`.
- `S-002` Per-operation accounting with full attribution, and cost per item and monthly rollup — `T-003`, `T-004`, `T-005`, `T-029`, `T-030`.
- `S-003` The permission basis of every asset, its registration state, and the release block both impose — `T-006`, `T-007`, `T-008`, `T-028`, `T-030`.
- `S-004` The unalterable recorded history and the permission answer for a finished item — `T-009`, `T-010`, `T-011`, `T-030`, `T-032`.
- `S-005` Least-privilege separation, the copyright block, the publishing limits and the refusal of self-approval — `T-012`, `T-013`, `T-014`, `T-028`, `T-031`.
- `S-006` Credential holding, scoping, rotation, revocation and release-credential reachability — `T-015`, `T-016`, `T-017`, `T-029`, `T-031`.
- `S-007` The named set of work determined by its inputs and a rule, carrying zero AI cost — `T-003`, `T-018`, `T-029`.
- `S-008` Owner approval and send-back recording with elapsed minutes from the first approval — `T-014`, `T-019`, `T-029`.
- `S-009` The operating registers of channels, departments, roles and capabilities, changeable as configuration — `T-020`, `T-021`, `T-022`, `T-032`.
- `S-010` The work lifecycle, its stage outcomes, its failure policy and independent progress — `T-023`, `T-024`, `T-028`.
- `S-011` The measurable-now reporting surface with deferred measures named — `T-025`, `T-029`.
- `S-012` Department and channel budgets with threshold alerts, and the protection of non-cuttable controls — `T-020`, `T-026`, `T-027`, `T-031`.

### Out of Scope

- `X-001` Publishing to any platform, the upload path and the publication schedule — excluded by the supplied request, and no channel exists to publish to. `T-014` delivers the gate's refusal semantics and no release path.
- `X-002` Creating a channel, a platform account or a payment account — one payment account is permitted per payee name and the payee position must be settled with qualified local counsel first.
- `X-003` Any spend commitment or purchase, including the standing annual licensing commitment — excluded by the supplied request; every unit price must be re-fetched at first hand before any spend, and the standing commitment waits on a metered first item.
- `X-004` Producing an item end to end — this wave delivers the record the pipeline runs inside, not the pipeline.
- `X-005` The compliance determinations and the originality, disclosure and suitability judgements — each is a judgement about a produced item and this wave produces none.
- `X-006` Revenue and every revenue-denominated measure — no revenue parameter is observed and none can be derived.
- `X-007` Audience analytics beyond what a platform returns, benchmark-driven capability selection, an executive reporting layer and autonomous planning — each depends on a scale and a recorded history that do not yet exist.
- `X-008` Human-contributor roles, their approval routes and any human-labour cost line — the approved envelope is exclusive of human labour.
- `X-009` A second channel, a second language and additional publishing platforms — one channel on one platform is fixed.
- `X-010` The technical approach, system structure, data model and technology selection — reserved to `architect`, with the stack a CEO decision at the Design Gate. No task in this plan decides it.
- `X-011` Short-form items within the committed rate — the envelope and its ledger are computed on long-form items.
- `X-012` Consumer chat subscriptions, multiplied personal accounts and shared credentials as routable capacity — two providers prohibit programmatic access outright and the third routes business use away from the consumer product. This design appears nowhere in this plan.

### Deferred

- Generalised company, agent, model-registry and workflow abstractions beyond what one channel needs as configuration — brought into scope when a second channel, department set or workforce configuration is authorised.
- Numeric per-task quality floors and the per-item cost target that replace the structural floor and the envelope comparison — brought into scope when the first complete item has been metered (`Q-002`).
- The clean-record threshold that would relax per-publication owner approval — brought into scope when the approval baseline this wave instruments has been recorded and the CEO decides on it (`Q-003`).
- Verification of the routing position against live provider routes rather than against the recorded position — brought into scope when production accounts under commercial terms exist and an operating authorisation outside this change is given.

## Assumptions

| ID | Assumption | Basis | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | The technology selection is taken at the Design Gate before any implementation task starts, and the stack recorded in the framework's own technical context does not bind this greenfield product | plan-wide | The eight settling tasks reopen against a chosen stack and every realization estimate is re-made | `architect`, with the CEO deciding at the Design Gate |
| A-002 | Acceptance evidence for the accounting criteria may be produced over recorded operation sets that commit no spend | `S-002` | `AC-003` and `AC-004` need a spend authorisation the boundary excludes at `X-003`, and `T-030` gains an external blocker | `omn-product-owner` |
| A-003 | The routing position can be settled and inspected without production accounts existing yet | `S-001` | `T-001` and `T-015` wait on account provisioning by the CEO, and wave 1 stalls | CEO |
| A-004 | No task in this plan depends on the payee position; it blocks banking, not building | plan-wide | The wave acquires an external blocker with no owner inside the plan | CEO |
| A-005 | The set of work determined by its inputs and a rule is the one carried from TRC-2026-0001, restricted to the work this wave delivers, with the remainder named but not realized here | `S-007` | The named set does not match what `AC-017` requires and `T-018` re-scopes | `architect` |
| A-006 | Capability names and route rows are carried from TRC-2026-0001 rather than re-derived in this wave | `S-001` | `T-001` becomes a derivation task rather than a settling task and wave 1 lengthens | `architect` |
| A-007 | Effort is expressible as relative complexity only; no absolute duration or date is asserted anywhere in this plan | plan-wide | Only the estimate scale changes; no task, dependency or order changes | `omn-tech-lead` |

## Risks

| ID | Class | Trigger | Impact | Likelihood | Affects | Mitigation | Owner |
|---|---|---|---|---|---|---|---|
| R-001 | requirement | A source the providers' terms forbid — consumer subscriptions, multiplied personal accounts or shared credentials — re-enters the routing position or becomes reachable | Capacity obtained by prohibited means puts the production accounts at termination risk and breaches `X-012` | low | T-001, T-002 | The forbidden-source list is settled at `T-001`, realized as a refusal at `T-002` and demonstrated at `T-028` and `T-030` | architect |
| R-002 | requirement | A revenue-denominated measure is added to the reported set before a revenue parameter is observed | The surface shows placeholders a reader mistakes for measurements, which `X-006` forbids | low | T-025 | The reported set is fixed to the measurable-now list at `T-025` and inspected at `T-029` | omn-product-owner |
| R-003 | requirement | The named set of rule-determined work does not match the one carried from TRC-2026-0001 once this wave's boundary is applied | `AC-017` cannot be satisfied as written and `T-018` re-scopes | low | T-003, T-018 | `A-005` is confirmed while `T-003` is settled, before `T-018` starts | architect |
| R-004 | requirement | Acceptance evidence for the accounting criteria turns out to require live provider operations | `AC-003` and `AC-004` cannot be evidenced without a spend authorisation the boundary excludes | medium | T-004, T-005, T-030 | `A-002` is confirmed before `T-030` starts; otherwise the conflict is escalated rather than resolved by spending | omn-product-owner |
| R-005 | technical | A capability resolves to the generation interface withdrawn on 2026-09-24 | The capability fails with no fallback and the failure is found in operation rather than in design | low | T-001, T-002 | The withdrawn interface is carried on the forbidden-source list at `T-001` and refused at `T-002` | architect |
| R-006 | technical | The record contract is settled, or the recorded history delivered, after a component that must record a refusal | Refusals are taken without a record and `AC-009`, `AC-012` and `AC-014` cannot be evidenced | medium | T-010, T-013, T-014, T-016, T-019, T-024 | `T-009` and `T-010` precede every component that records a refusal, as the edges in 8.1 require | architect |
| R-007 | dependency | The routing position is not settled, or must be re-derived rather than carried, before a task that consumes capability access begins | Every task that attributes an operation to a route is blocked or is built against a position that later changes | medium | T-002, T-003, T-004, T-015, T-020 | `T-001` is the only wave-1 task on that path and no successor starts before it closes | architect |
| R-008 | dependency | The technology selection reserved to the Design Gate is not taken when the design phase closes | No implementation-phase task can start and the wave stalls at the gate | medium | T-002, T-004, T-005, T-007, T-008, T-010, T-011, T-013, T-014, T-016, T-017, T-018, T-019, T-021, T-022, T-024, T-025, T-026, T-027 | The eight settling tasks are stack-independent, so the gate decides on complete evidence; recorded as an external dependency in 8.2 | omn-tech-lead |
| R-009 | dependency | The payee position stays unsettled with qualified local counsel | Channel creation and the payment account stay blocked in the later change; no task in this plan is affected | medium | plan-wide | `X-002` excludes channel creation and `A-004` records that no task here depends on it; the party who settles it is the CEO on qualified local counsel, named as the responsible party in 8.2, and the owner named here is the agent accountable for carrying and escalating that external prerequisite | omn-orchestrator |
| R-010 | dependency | A spend is committed without a first-hand re-fetch of the unit price | The envelope comparison rests on an unverified price and the re-verification constraint is breached | low | T-021, T-022 | Every register entry carries a verification date and no entry lacking one is presented as current, per `AC-020` | omn-orchestrator |
| R-011 | dependency | Production accounts under commercial terms do not exist when the routing position is verified | `AC-001` can be inspected against the recorded position but not demonstrated against live routes | medium | T-001, T-030 | `A-003`; live-route verification is deferred rather than assumed, and the recorded position remains inspectable; the party who holds the accounts is the CEO, named as the responsible party in 8.2, and the owner named here is the agent accountable for carrying and escalating that external prerequisite | omn-orchestrator |
| R-012 | security | A credential value reaches workforce context, a prompt, an artifact, a log or run evidence | `AC-013` fails, and rotation cannot recall what has already been disclosed | medium | T-015, T-016, T-017, T-030 | The protection standard is settled at `T-015`, realized at `T-016` and swept over every produced surface at `T-029` and `T-030` | architect |
| R-013 | security | An exception route lets a role approve a control it is itself subject to | The separation of duties is nominal and `AC-012` fails | medium | T-012, T-013 | Self-approval is refused at `T-013` and demonstrated per affected role at `T-028` | architect |
| R-014 | operational | Library registration state is not a precondition of a releasable state when the first item approaches release | An item reaches a releasable state before registration, and revenue earned before registration is unrecoverable | medium | T-008 | Registration state blocks the releasable state at `T-008`, per `AC-007` | omn-product-owner |
| R-015 | operational | Approval-minutes instrumentation is delivered after the first owner approval is taken | The baseline the clean-record threshold must be derived from is unrecoverable for the approvals already taken | medium | T-014, T-019 | `T-019` completes before any owner approval is taken; the gate's approval step and its instrumentation are ordered together | omn-product-owner |
| R-016 | delivery | The twenty-seven settling and realization tasks and their coupling exceed what one implementation phase can carry | The wave slips or is cut, and a cut falls on a control the boundary records as non-cuttable | medium | plan-wide | The eight waves in 8.3 expose the parallel capacity; intake is decided at the Planning Gate, and `T-027` makes a reduction of a non-cuttable control refusable | omn-tech-lead |

## Task Breakdown

Numbering groups tasks by the lowest in-scope statement each serves, ordered within a group as settling, then realization; the cross-cutting verification, review and documentation tasks serve every statement and are numbered after them. Complexity is the relative scale of `reasoning.md` R9 and is anchored to the relative ordering recorded under Delivery Impact in `runs/run-258e0a3415d2/states/recommendation/artifacts/technical-recommendation.md`. No day count, duration or date is asserted: `AS-003` records that no operating baseline exists, and no supplied source supports an absolute estimate.

### T-001 Settle the capability-to-provider routing position

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: none
- Traces to: S-001, A-003, A-006
- Status: assumption-dependent
- Description: The routing position is settled for every capability the company consumes, naming one primary, one secondary and one emergency production account held under commercial terms, together with the forbidden-source list, so that no later task depends on capability access that is not yet resolved. This is the one recorded item that blocks building.
- Acceptance Criteria:
  - Every capability the company consumes carries exactly one primary, one secondary and one emergency route, each held under commercial terms, per `AC-001`.
  - The forbidden-source list is recorded and carries the consumer-subscription pool, multiplied personal accounts, shared credentials and the generation interface withdrawn on 2026-09-24, and the settled position contains none of them.
  - The degraded outcome when both the primary and the secondary route are unavailable is stated for every capability, per `AC-002`.
- Gate: Design Gate

### T-002 Realize capability route resolution and forbidden-source refusal

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-001, T-010
- Traces to: S-001
- Status: ready
- Description: A request for a capability resolves to its primary route, falls to its secondary and then to its emergency route when the preceding route is unavailable, and resolves to no route at all when it names a forbidden source; every resolution and every failure carries a recorded reason. Resolution is determined by its inputs and the recorded position, so no model call is made.
- Acceptance Criteria:
  - A request naming a forbidden source resolves to zero reachable routes, per `AC-001`.
  - With both the primary and the secondary route made unavailable, the work takes the emergency route or is recorded as held at its declared quality, and never fails without a recorded reason, per `AC-002`.
- Gate: Review Gate

### T-003 Define the operation accounting and attribution contract

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: T-001
- Traces to: S-002, S-007, A-005
- Status: assumption-dependent
- Description: The contract for accounting an operation is settled: the attributes every operation carries, the rule that accounts each operation exactly once including failed and retried ones, and the named set of work that is determined by its inputs and a rule and therefore carries no AI cost.
- Acceptance Criteria:
  - The contract names consumption, cost, duration and the attribution to item, channel, department, workforce role, capability and the capability actually used, per `AC-003`.
  - The contract states the once-only accounting rule for failed and retried operations.
  - The named set of rule-determined work is recorded and matches the one carried from TRC-2026-0001 as restricted by this wave's boundary, per `AC-017`.
- Gate: Design Gate

### T-004 Realize per-operation accounting with complete attribution

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-002, T-003
- Traces to: S-002, A-002
- Status: assumption-dependent
- Description: Every operation the company performs is accounted exactly once with its consumption, cost, duration and full attribution, including operations that failed and operations that were retried. Accounting is determined by its inputs and the contract, so no model call is made.
- Acceptance Criteria:
  - Over a recorded operation set, the count of operations with a missing or incomplete attribution is zero, per `AC-003`.
  - A failed operation and a retried operation each appear exactly once in the account.
- Gate: Review Gate

### T-005 Realize cost rollup per item and per month against the envelope

- Owner: omn-dev-1-implement
- Complexity: S (confidence: low)
- Depends on: T-004
- Traces to: S-002, A-002
- Status: assumption-dependent
- Description: Cost per item and the monthly total follow from the recorded operations alone and are presented against the approved envelope with the variance stated and every figure labelled an estimate until it is a measurement. The arithmetic is determined by its inputs, so no model call is made.
- Acceptance Criteria:
  - The rollup over a recorded operation set equals the same arithmetic computed independently, per `AC-004`.
  - The presentation states the envelope of USD 77.41 per month split 34.42 metered and 42.99 standing, the variance, and the estimate-or-measurement status of every figure, per `AC-004`.
- Gate: Review Gate

### T-006 Define the asset permission-basis record contract

- Owner: architect
- Complexity: S (confidence: medium)
- Depends on: none
- Traces to: S-003
- Status: ready
- Description: The contract for recording why the company is permitted to use an asset is settled, including which fields are mandatory, how an incomplete record resolves, and how a channel's registration state on a library is carried.
- Acceptance Criteria:
  - The contract names source, creator, licence type and reference, commercial-use and modification permission, attribution requirement, platform restrictions, expiry, proof of licence, assessed risk, and the verifier's identity and date, per `AC-005`.
  - The contract states that a record missing any field resolves as not permitted rather than as permitted, per `AC-005`.
  - The contract carries a channel's registration state per library with its date, per `AC-007`.
- Gate: Design Gate

### T-007 Realize the asset permission-basis record

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-006
- Traces to: S-003
- Status: ready
- Description: Every asset the company holds against an item carries its permission basis as the contract defines it, and an incomplete record resolves as not permitted. The record and its join are determined by their inputs and the contract, so no model call is made.
- Acceptance Criteria:
  - Inspection of a populated record set containing a deliberately incomplete record shows the incomplete record resolving as not permitted, per `AC-005`.
  - Every asset held against an item resolves to exactly one permission-basis record.
- Gate: Review Gate

### T-008 Realize the rights preconditions of a releasable state

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-007, T-012
- Traces to: S-003
- Status: ready
- Description: An item cannot reach a releasable state while any of its assets lacks a verified, unexpired permission basis or while its channel is unregistered on a library one of its assets comes from, and the block names what is missing. The precondition is determined by its inputs and the record, so no model call is made.
- Acceptance Criteria:
  - An item holding a deliberately unlicensed asset cannot reach a releasable state, and the block names the asset and the missing basis, per `AC-006`.
  - An item whose channel is recorded as unregistered on one library cannot reach a releasable state, per `AC-007`.
- Gate: Review Gate

### T-009 Define the unalterable record contract

- Owner: architect
- Complexity: S (confidence: medium)
- Depends on: none
- Traces to: S-004
- Status: ready
- Description: The contract for the company's recorded history is settled: which actions are recorded as important, what each entry carries, and that a written entry cannot be altered or removed.
- Acceptance Criteria:
  - The contract names who, what, when, why, inputs, outputs, decision, cost and risk as the content of every recorded entry, per `AC-009`.
  - The contract states that an attempt to alter or remove a written entry is refused and that the attempt is itself recorded, per `AC-009`.
- Gate: Design Gate

### T-010 Realize the unalterable recorded history

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-009
- Traces to: S-004
- Status: ready
- Description: The company's recorded history accepts entries as the contract defines them and refuses amendment, and every component that must record a decision, a refusal or a use writes to it. Recording is determined by its inputs and the contract, so no model call is made.
- Acceptance Criteria:
  - Inspection of a recorded entry set shows every entry carrying the contracted content, and an attempted amendment is refused and the refusal recorded, per `AC-009`.
  - The refused amendment appears as its own entry naming the entry it was attempted against, and that original entry is returned unchanged afterwards, per `AC-009`.
- Gate: Review Gate

### T-011 Realize the permission answer for a finished item

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-007, T-010
- Traces to: S-004
- Status: ready
- Description: For any finished item, the recorded history states for each of its assets the basis on which the company was permitted to use it, the role that verified it and the date, answerable with no reference to any source outside the record. The answer is assembled by join, so no model call is made.
- Acceptance Criteria:
  - The permission question for a finished item is answered from the recorded history alone, with no reference outside it, per `AC-008`.
  - The answer covers every asset the item holds and names, for each, the role that verified its basis and the date of that verification, per `AC-008`.
- Gate: Review Gate

### T-012 Define the authority and refusal model

- Owner: architect
- Complexity: M (confidence: medium)
- Depends on: T-009
- Traces to: S-005
- Status: ready
- Description: The authority model is settled: which responsibilities exist, what each is permitted to do, what a releasable state is and what prevents it, and how a refusal is recorded. It carries no human-contributor role.
- Acceptance Criteria:
  - The model gives the copyright responsibility the permission to block a release and no permission that releases, per `AC-010`.
  - The model denies the publishing responsibility any route that overrides a standing block, that releases without a complete approval set, or that uses an approval bound to another version, per `AC-011`.
  - The model states that no role may approve an exception to a control it is itself subject to, for every role subject to a control, per `AC-012`.
- Gate: Design Gate

### T-013 Realize least-privilege permissions and the refusal of self-approval

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-010, T-012
- Traces to: S-005
- Status: ready
- Description: Each responsibility holds only the permissions the model grants it, an attempt outside them is refused, and the refusal is recorded. Permission resolution is determined by its inputs and the model, so no model call is made.
- Acceptance Criteria:
  - An attempt by the copyright responsibility to release is refused and recorded, and inspection shows it holds no releasing permission, per `AC-010`.
  - A self-approval attempt is refused and recorded for every role subject to a control, per `AC-012`.
- Gate: Review Gate

### T-014 Realize the publishing gate's blocking and approval semantics

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-013
- Traces to: S-005, S-008
- Status: ready
- Description: The publishing gate carries the mandatory owner-approval step and refuses to pass while a copyright block stands, while the approval set is incomplete, or when the approval presented is bound to a different version of the item. It carries no release path, because publication is excluded at `X-001`. The gate's checks are determined by their inputs, so no model call is made.
- Acceptance Criteria:
  - All three refusals — a standing copyright block, an incomplete approval set, and an approval bound to another version — are demonstrated and each is recorded, per `AC-011` and `AC-019`.
  - No route from the gate reaches a publishing platform.
- Gate: Review Gate

### T-015 Define the credential protection standard

- Owner: architect
- Complexity: M (confidence: medium)
- Depends on: T-001
- Traces to: S-006
- Status: ready
- Description: The protection standard for credentials is settled, answering `Q-007`: where credentials are held, how a task receives one scoped to it and no wider, how release credentials are gated, how rotation and revocation take effect without redeployment, and how each use is recorded.
- Acceptance Criteria:
  - The standard states that no credential value appears in workforce context, prompts, artifacts, logs or run evidence, per `AC-013`.
  - The standard states one holder per provider, account and channel, and the effect of rotation and revocation on the next use without redeployment, per `AC-016`.
  - The standard states the condition under which a release credential is reachable, per `AC-015`.
- Gate: Design Gate

### T-016 Realize credential holding, scoping, rotation and revocation

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-010, T-015
- Traces to: S-006
- Status: ready
- Description: Credentials are held as the standard requires, each task receives one scoped to it and valid only for its declared lifetime, no credential is held for more than one provider, account or channel, rotation and revocation take effect on the next use without redeployment, and every use is recorded.
- Acceptance Criteria:
  - A sweep of workforce context, prompts, artifacts, logs and run evidence against the held credential identifiers returns zero occurrences, per `AC-013`.
  - A credential presented after expiry and one presented outside its scope are each refused and the refusal recorded, per `AC-014`.
  - A rotation and a revocation each take effect on the next use with no redeployment, and the credential-to-holder mapping shows no credential spanning two providers, accounts or channels, per `AC-016`.
- Gate: Review Gate

### T-017 Realize release-credential reachability bound to the passed gate

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-014, T-016
- Traces to: S-006
- Status: ready
- Description: A credential that would permit a release is reachable only by the publishing responsibility and only once the release gate has passed; any other request is refused and recorded.
- Acceptance Criteria:
  - A request from a role other than the publishing responsibility is refused and recorded, per `AC-015`.
  - A request made before the gate passes is refused and recorded, per `AC-015`.
- Gate: Review Gate

### T-018 Realize the register of rule-determined work carrying zero AI cost

- Owner: omn-dev-1-implement
- Complexity: S (confidence: low)
- Depends on: T-003, T-004
- Traces to: S-007, A-005
- Status: assumption-dependent
- Description: The set of work whose output is fully determined by its inputs and a rule is named in what the company operates, and no AI cost attaches to any of it.
- Acceptance Criteria:
  - The named set matches the one carried from TRC-2026-0001 as restricted by this wave's boundary, per `AC-017`.
  - Measured over a recorded run set, the AI cost attached to the named set is zero, per `AC-017`.
- Gate: Review Gate

### T-019 Realize approval and send-back recording with elapsed minutes

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-010, T-014
- Traces to: S-008
- Status: ready
- Description: Every owner approval and send-back is recorded from the first one onward with the item, the exact version it was bound to, the verdict, the reason and the elapsed minutes, so that the relaxation threshold can later rest on a measured baseline. This must be in place before the first owner approval is taken.
- Acceptance Criteria:
  - Over the recorded approval set, the count of approvals or send-backs lacking an elapsed-minutes value is zero, per `AC-018`.
  - Each record names the item and the exact version the verdict was bound to, per `AC-018`.
- Gate: Review Gate

### T-020 Define the configuration model for registers and budgets

- Owner: architect
- Complexity: M (confidence: medium)
- Depends on: T-001, T-003
- Traces to: S-009, S-012
- Status: ready
- Description: The configuration model is settled: what the company records about the channels, departments, roles and capabilities it operates, how a price or limit change takes effect without redeployment while retaining the prior value and its validity dates, and how a budget and its utilization thresholds are declared.
- Acceptance Criteria:
  - The model carries, per capability, its price, limit, source and verification date, per `AC-020`.
  - The model states how a change takes effect as configuration alone and how the prior value and its validity dates are retained, per `AC-021`.
  - The model declares budgets per department and per channel with utilization thresholds at 50, 75, 90 and 100 per cent, per `AC-027`.
- Gate: Design Gate

### T-021 Realize the operating registers of channels, departments, roles and capabilities

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-020
- Traces to: S-009
- Status: ready
- Description: The company produces from its own records what it currently operates, with each role's permitted actions and each capability's price, limit, source and verification date, and presents no entry lacking a verification date as current.
- Acceptance Criteria:
  - The produced listing matches what the company is configured to operate, per `AC-020`.
  - No entry lacking a verification date is presented as current, per `AC-020`.
- Gate: Review Gate

### T-022 Realize price and limit change as configuration

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-021
- Traces to: S-009
- Status: ready
- Description: A change to a capability's price or limit takes effect through configuration alone, with no redeployment and no change to how any role operates, and the prior value stays retrievable with its validity dates.
- Acceptance Criteria:
  - A price change takes effect with no redeployment and no change to any role's operation, per `AC-021`.
  - The prior value remains retrievable with its validity dates, per `AC-021`.
- Gate: Review Gate

### T-023 Define the work lifecycle and failure-handling model

- Owner: architect
- Complexity: M (confidence: medium)
- Depends on: T-009
- Traces to: S-010
- Status: ready
- Description: The lifecycle model is settled: the positions a unit of production work passes, the outcome recorded at each, the retry, backoff and escalation policy for a failed step, and the rule by which work that depends on nothing in common is not held behind other work.
- Acceptance Criteria:
  - The model names every lifecycle position and the outcome recorded at each, per `AC-022`.
  - The model states the retry, backoff and escalation policy under which no failure ends in neither, per `AC-023`.
  - The model states the independence rule that `AC-024` requires.
- Gate: Design Gate

### T-024 Realize the work lifecycle with stage outcomes and failure handling

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-010, T-023
- Traces to: S-010
- Status: ready
- Description: Every unit of production work carries its lifecycle position and the outcome of each stage it passed, a failed step is recorded and retried or escalated within its declared policy, and independent work progresses independently. Retry, backoff and failover are determined by the policy, so no model call is made.
- Acceptance Criteria:
  - The stage history of a completed unit resolves to every position it passed and the outcome of each, with none missing, per `AC-022`.
  - Over an induced-failure set, the count of failures ending in neither a retry nor an escalation is zero, per `AC-023`.
  - Two units of work that depend on nothing in common progress without either waiting on the other, per `AC-024`.
- Gate: Review Gate

### T-025 Realize the measurable-now reporting surface

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-005, T-019, T-024
- Traces to: S-011
- Status: ready
- Description: The company reports exactly the measures computable before a revenue parameter is observed, and reports each revenue-denominated measure as not yet available with the parameter it waits on named.
- Acceptance Criteria:
  - The reported measure set equals the measurable-now list and contains no revenue-denominated measure, per `AC-025`.
  - While every deferred denominator is absent, each deferred measure is shown as not yet available with its awaited parameter named, and never as zero, a dash or any placeholder a reader could mistake for a measurement, per `AC-026`.
- Gate: Review Gate

### T-026 Realize department and channel budgets with threshold alerts

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-004, T-020, T-021
- Traces to: S-012
- Status: ready
- Description: Each department and channel carries a configurable budget whose utilization is tracked from the recorded cost, and each threshold crossing raises an alert naming the budget, the threshold and the utilization. The arithmetic is determined by its inputs, so no model call is made.
- Acceptance Criteria:
  - Driving recorded cost past 50, 75, 90 and 100 per cent in turn raises one alert at each crossing naming the budget, the threshold and the utilization, per `AC-027`.
  - Each department and each channel resolves to exactly one configurable budget whose utilization is computed from the recorded operations alone, per `AC-027`.
- Gate: Review Gate

### T-027 Realize the protection of non-cuttable controls

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-013, T-026
- Traces to: S-012
- Status: ready
- Description: The controls recorded as non-cuttable are marked as such and cannot be disabled or reduced by a cost decision; the attempt is refused and recorded, and every cost decision records the controls it did not touch.
- Acceptance Criteria:
  - An attempt to disable or reduce a non-cuttable control by a cost decision is refused and recorded, per `AC-028`.
  - Inspection of a recorded cost decision shows the controls it did not touch, per `AC-028`.
- Gate: Review Gate

### T-028 Design the demonstration set for every refusal and failover

- Owner: omn-qa
- Complexity: M (confidence: medium)
- Depends on: none
- Traces to: S-001, S-003, S-005, S-006, S-008, S-009, S-010, S-012
- Status: ready
- Description: A demonstration is designed for every criterion the boundary verifies by demonstration, including each forbidden-source refusal, the failover with both routes unavailable, each block on a releasable state, each forbidden privilege act, each credential refusal, the version-bound approval refusal, the price change, the threshold crossings and the refused reduction, with the evidence each demonstration must produce.
- Acceptance Criteria:
  - Every boundary criterion whose verification method is a demonstration has exactly one designed demonstration naming its setup, its expected refusal or outcome, and the evidence it produces.
  - Each designed demonstration is judgeable by a role other than the one that performs it.
- Gate: Verification Gate

### T-029 Design the measurement set for every counted criterion

- Owner: omn-qa
- Complexity: M (confidence: medium)
- Depends on: none
- Traces to: S-002, S-003, S-004, S-006, S-007, S-008, S-010, S-011
- Status: ready
- Description: A measurement is designed for every criterion the boundary verifies by measurement or inspection, including the attribution completeness count, the independently recomputed rollup, the credential sweep, the zero-AI-cost measurement over the named set, the elapsed-minutes completeness count, the induced-failure count and the reported measure set inspection, each with the population it runs over.
- Acceptance Criteria:
  - Every boundary criterion whose verification method is a measurement or an inspection has exactly one designed measurement naming its population, its counting rule and its expected result.
  - Each designed measurement is reproducible by a role other than the one that performs it.
- Gate: Verification Gate

### T-030 Record the verification evidence for the wave

- Owner: omn-qa
- Complexity: L (confidence: low)
- Depends on: T-008, T-011, T-017, T-018, T-022, T-025, T-027, T-028, T-029
- Traces to: S-001, S-002, S-003, S-004, S-005, S-006, S-007, S-008, S-009, S-010, S-011, S-012, A-002
- Status: assumption-dependent
- Description: The designed demonstrations and measurements are carried out against what the wave delivered and their evidence is recorded, so that each of the twenty-eight boundary criteria resolves to recorded evidence rather than to assertion.
- Acceptance Criteria:
  - Each of `AC-001` to `AC-028` resolves to recorded evidence naming the demonstration or measurement it came from.
  - Every criterion that could not be evidenced is recorded as not evidenced with the reason, rather than omitted.
- Gate: Verification Gate

### T-031 Review the wave against the credential and least-privilege constraints

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: medium)
- Depends on: T-013, T-017, T-027
- Traces to: S-005, S-006, S-012
- Status: ready
- Description: What the wave delivered is reviewed against the credential constraints and the least-privilege separation the boundary requires, and against the prohibition on reducing a non-cuttable control, with findings recorded.
- Acceptance Criteria:
  - The review records a finding or an explicit clearance for each of the credential constraints and each of the three forbidden privilege acts.
  - The review records that no delivered route reduces a control marked non-cuttable.
- Gate: Review Gate

### T-032 Publish the documentation of the foundation's operating record

- Owner: omn-documentation
- Complexity: S (confidence: medium)
- Depends on: T-030, T-031
- Traces to: S-004, S-009
- Status: ready
- Description: The documentation states what the company operates, how its record answers the permission question for a finished item, and what the wave's recorded evidence and review found, with the release-impact note for the next change.
- Acceptance Criteria:
  - The documentation names the operating registers, the recorded history and the permission answer, each resolvable to the record that produces it.
  - The release-impact note names what this wave does not deliver, citing `X-001`, `X-002` and `X-003`.
- Gate: Closure Gate

## Dependencies

### 8.1 Dependency Edges

| From | To | Type | Justification |
|---|---|---|---|
| T-001 | T-002 | decision-gate | The routes and the forbidden-source list the resolver enforces are fixed by the settled position |
| T-010 | T-002 | produces-consumes | The recorded reason a resolution or failure produces is written to the recorded history |
| T-001 | T-003 | decision-gate | Attribution to the capability actually used presumes the settled route set |
| T-002 | T-004 | produces-consumes | The route actually used is an attribute of the accounted operation |
| T-003 | T-004 | contract | The accounting record binds to the attribution contract |
| T-004 | T-005 | produces-consumes | The rollup is computed from accounted operations alone |
| T-006 | T-007 | contract | The asset record binds to the permission-basis contract |
| T-007 | T-008 | produces-consumes | The block reads the asset's permission basis and its channel's registration state |
| T-012 | T-008 | contract | The releasable state the block prevents is defined by the authority model |
| T-009 | T-010 | contract | The recorded history binds to the record contract |
| T-007 | T-011 | produces-consumes | The permission answer reads the asset permission records |
| T-010 | T-011 | produces-consumes | The permission answer is assembled from the recorded history |
| T-009 | T-012 | contract | Every refusal the authority model defines is a recorded entry |
| T-010 | T-013 | produces-consumes | Each refused attempt is written to the recorded history |
| T-012 | T-013 | contract | The permissions realized bind to the authority model |
| T-013 | T-014 | produces-consumes | The gate's blocking and approval checks are permission decisions |
| T-001 | T-015 | decision-gate | The credential holder set is the account set the routing position names |
| T-010 | T-016 | produces-consumes | Every credential use and refusal is a recorded entry |
| T-015 | T-016 | contract | Credential handling binds to the protection standard |
| T-014 | T-017 | decision-gate | Reachability is conditioned on the release gate having passed |
| T-016 | T-017 | produces-consumes | A release credential is a scoped case of a held credential |
| T-003 | T-018 | contract | The register names the work the attribution contract marks as carrying no AI cost |
| T-004 | T-018 | verification | Zero attached AI cost is read from the accounted operations |
| T-010 | T-019 | produces-consumes | Approvals and send-backs are recorded entries |
| T-014 | T-019 | produces-consumes | An approval record is produced by the gate's approval step |
| T-001 | T-020 | produces-consumes | Capability price, limit and source rows come from the settled routing position |
| T-003 | T-020 | contract | Budget utilization is computed from accounted cost |
| T-020 | T-021 | contract | The registers bind to the configuration model |
| T-021 | T-022 | produces-consumes | A price or limit change is a change to a register entry |
| T-009 | T-023 | contract | Stage outcomes, failures and escalations are recorded entries |
| T-010 | T-024 | produces-consumes | Each stage outcome and each failure is a recorded entry |
| T-023 | T-024 | contract | The lifecycle realized binds to the lifecycle model |
| T-005 | T-025 | produces-consumes | Cost per item and the monthly total are reported measures |
| T-019 | T-025 | produces-consumes | Minutes per owner approval is a reported measure |
| T-024 | T-025 | produces-consumes | Planned, produced, completed and delayed volume is read from the lifecycle |
| T-004 | T-026 | produces-consumes | Utilization is computed from accounted cost |
| T-020 | T-026 | contract | Budgets and their thresholds bind to the configuration model |
| T-021 | T-026 | produces-consumes | A budget attaches to a department or channel register entry |
| T-013 | T-027 | contract | Refusing a cost-driven reduction is an authority decision |
| T-026 | T-027 | produces-consumes | A cost decision acts on a budget |
| T-008 | T-030 | verification | The evidence verifies the delivered rights preconditions |
| T-011 | T-030 | verification | The evidence verifies the delivered permission answer |
| T-017 | T-030 | verification | The evidence verifies the delivered credential reachability |
| T-018 | T-030 | verification | The evidence verifies the delivered register of rule-determined work |
| T-022 | T-030 | verification | The evidence verifies the delivered configuration change |
| T-025 | T-030 | verification | The evidence verifies the delivered reported measure set |
| T-027 | T-030 | verification | The evidence verifies the delivered protection of non-cuttable controls |
| T-028 | T-030 | contract | The evidence is recorded against the designed demonstrations |
| T-029 | T-030 | contract | The evidence is recorded against the designed measurements |
| T-013 | T-031 | verification | The review assesses the least-privilege separation delivered |
| T-017 | T-031 | verification | The review assesses the credential reachability delivered |
| T-027 | T-031 | verification | The review assesses that no delivered route reduces a non-cuttable control |
| T-030 | T-032 | produces-consumes | The documentation cites the recorded evidence |
| T-031 | T-032 | produces-consumes | The documentation records the review outcome |

### 8.2 External Dependencies

| Responsible party | What is needed | Blocks |
|---|---|---|
| CEO, at the Design Gate | The technology selection reserved to that gate, without which no realization task can start | T-002, T-004, T-005, T-007, T-008, T-010, T-011, T-013, T-014, T-016, T-017, T-018, T-019, T-021, T-022, T-024, T-025, T-026, T-027 |
| CEO | Production accounts under commercial terms for each capability, if the routing position is to be verified against live routes rather than against the recorded position | None in this plan; the live-route verification is deferred, and `R-011` carries the consequence |
| omn-orchestrator | First-hand re-fetch of every unit price and re-verification of platform policy before any spend | None in this plan, because `X-003` commits no spend; the verification date carried at T-021 and T-022 records when it was last done |
| CEO, on qualified local counsel | The payee tax and withholding position | None in this plan; it blocks the channel and payment account excluded at `X-002`, per `A-004` |

### 8.3 Implementation Order

Topological order over 8.1, ties inside a wave by ascending identifier. Waves express what may run in parallel, not what must.

- Wave 1: T-001, T-006, T-009, T-028, T-029
- Wave 2: T-003, T-007, T-010, T-012, T-015, T-023
- Wave 3: T-002, T-008, T-011, T-013, T-016, T-020, T-024
- Wave 4: T-004, T-014, T-021
- Wave 5: T-005, T-017, T-018, T-019, T-022, T-026
- Wave 6: T-025, T-027
- Wave 7: T-030, T-031
- Wave 8: T-032

Reconciliation with the sequencing constraints recorded under Delivery Impact in the recommendation. Kept: the routing position is settled before anything that consumes capability access, which is that artifact's condition for discharging the one recorded blocker to building; the rights and licence ledger with library registration as a coded precondition is realized early, at waves 2 and 3; the approval-minutes instrumentation is delivered with the gate's approval step rather than after it; the payee position precedes channel creation and blocks nothing here; and no route reaches the generation interface withdrawn on 2026-09-24. Departed from, with reasons: that artifact places the single-video production chain third and the compliance gate pack fourth, and both are excluded from this change at `X-004` and `X-005`, so neither is planned here and neither may be inferred from this order; it places the cost rollup and the audit log last, whereas the record contract and the recorded history are settled and delivered in waves 1 and 2, because `AC-009`, `AC-012` and `AC-014` require refusals taken by earlier components to be recorded, and a history delivered after them would leave those refusals unrecorded — `R-006` carries that consequence; and it places the publishing gate with an idempotent upload, whereas `X-001` removes the upload path and leaves the gate's refusal semantics only.

## Suggested Workflow

Selected workflow: `implement-feature`

Selected because the change delivers new functionality against an approved scope boundary, is neither a defect resolution nor an internal quality change, and its phases already own the settling, realization, verification and handoff work this plan decomposes. This section recommends; it starts nothing.

| Phase | Tasks |
|---|---|
| scope-and-acceptance | Closed upstream by `SCOPE-2026-0001`; no task in this plan |
| execution-planning | This plan; no task in this plan |
| solution-design-and-risk-assessment | T-001, T-003, T-006, T-009, T-012, T-015, T-020, T-023 |
| implementation | T-002, T-004, T-005, T-007, T-008, T-010, T-011, T-013, T-014, T-016, T-017, T-018, T-019, T-021, T-022, T-024, T-025, T-026, T-027 |
| quality-review | T-028, T-029, T-030, T-031 |
| documentation-and-release-handoff | T-032 |

| Gate | Required owners |
|---|---|
| Planning Gate | omn-tech-lead, omn-orchestrator |
| Design Gate | omn-architect, omn-tech-lead |
| Review Gate | omn-dev-2-reviewer, omn-qa |
| Verification Gate | omn-qa |
| Closure Gate | omn-orchestrator, omn-documentation |

## Required Capabilities

### 10.1 Agent Capabilities

| Capability | Tasks | Owning agent | Proficiency |
|---|---|---|---|
| architecture-analysis | T-001, T-003, T-006, T-009, T-012, T-015, T-020, T-023 | architect | Primary |
| technical-approach-definition | T-001, T-003, T-006, T-009, T-012, T-015, T-020, T-023 | architect | Primary |
| architecture-decision-authoring | T-001, T-015 | architect | Primary |
| structural-risk-analysis | T-001, T-012, T-015, T-023 | architect | Primary |
| implementation-delivery | T-002, T-004, T-005, T-007, T-008, T-010, T-011, T-013, T-014, T-016, T-017, T-018, T-019, T-021, T-022, T-024, T-025, T-026, T-027 | omn-dev-1-implement | Primary |
| validation-design | T-028, T-029 | omn-qa | Primary |
| quality-verification | T-030 | omn-qa | Primary |
| code-review | T-031 | omn-dev-2-reviewer | Primary |
| governance-enforcement | T-031 | omn-dev-2-reviewer | Primary |
| documentation | T-032 | omn-documentation | Primary |

### 10.2 Required Skills

| Skill | File | Tasks | Level |
|---|---|---|---|
| S01 | skills/architecture/clean-architecture-checklist.md | T-001, T-003, T-006, T-009, T-012, T-015, T-020, T-023 | Primary |
| S02 | skills/business/domain-modeling.md | T-003, T-006, T-009, T-012, T-020, T-023 | Primary |
| S06 | skills/database/database-engineering.md | T-007, T-010, T-011, T-021 | Secondary |
| S07 | skills/testing/testing-strategy.md | T-028, T-029, T-030 | Primary |
| S09 | skills/security/secure-engineering.md | T-012, T-013, T-015, T-016, T-017, T-031 | Primary |
| S11 | skills/logging/observability-logging.md | T-004, T-010, T-019, T-024, T-025, T-026 | Secondary |
| S12 | skills/error-handling/error-handling-strategy.md | T-002, T-023, T-024 | Secondary |

## Acceptance Criteria

1. Every capability the company consumes resolves to a primary, a secondary and an emergency production route, and every forbidden source resolves to no reachable route. Verifies `BO-3`. Evidence: the inspection and the failover demonstration recorded at T-030 against `AC-001` and `AC-002`.
2. Cost per item and the monthly total are produced from recorded operations alone and stated against the approved envelope with their variance. Verifies `BO-1`. Evidence: the independently recomputed rollup recorded at T-030 against `AC-003` and `AC-004`.
3. Work named as determined by its inputs and a rule carries zero AI cost. Verifies `BO-1`. Evidence: the measurement over a recorded run set recorded at T-030 against `AC-017`.
4. The reported measure set carries no revenue-denominated measure, and each deferred measure names the parameter it waits on. Verifies `BO-1`. Evidence: the inspection recorded at T-030 against `AC-025` and `AC-026`.
5. For a finished item, the basis on which the company was permitted to use each of its assets is answerable from the record alone. Verifies `BO-2`. Evidence: the record review recorded at T-030 against `AC-005` and `AC-008`.
6. An item holding an asset without a verified permission basis, or whose channel is unregistered on a library its assets come from, cannot reach a releasable state. Verifies `BO-2`. Evidence: the two demonstrations recorded at T-030 against `AC-006` and `AC-007`.
7. An attempt to alter or remove a written record entry is refused and the attempt is itself recorded. Verifies `BO-2`. Evidence: the refused-amendment demonstration recorded at T-030 against `AC-009`.
8. Each of the three forbidden privilege acts — a copyright release, a publishing override of a standing block or of an incomplete approval set, and a self-approved exception — is refused and recorded. Verifies `BO-4`. Evidence: the demonstrations recorded at T-030 against `AC-010`, `AC-011` and `AC-012`, with the review findings at T-031.
9. No credential value appears on any surface the company produces, and a release credential is reachable only by the publishing responsibility and only after the gate passes. Verifies `BO-4`. Evidence: the sweep and the two refusal demonstrations recorded at T-030 against `AC-013` to `AC-016`.
10. Every owner approval and send-back carries its elapsed minutes from the first one onward and binds to an exact version of the item. Verifies `BO-5`. Evidence: the completeness measurement and the version-mismatch demonstration recorded at T-030 against `AC-018` and `AC-019`.
11. What the company operates is producible from its own records, and a price or limit change takes effect as configuration with the prior value retained. Verifies `BO-6`. Evidence: the listing inspection and the price-change demonstration recorded at T-030 against `AC-020` and `AC-021`.
12. Every unit of work resolves to the stages it passed, no failure ends unretried and unescalated, and independent work progresses independently. Verifies `BO-6`. Evidence: the stage-history inspection, the induced-failure measurement and the independence demonstration recorded at T-030 against `AC-022`, `AC-023` and `AC-024`.
13. Each budget raises an alert at each utilization threshold, and an attempt to reduce a non-cuttable control by a cost decision is refused and recorded. Verifies `BO-6` and `BO-4`. Evidence: the threshold demonstrations and the refused-reduction demonstration recorded at T-030 against `AC-027` and `AC-028`.

## Definition of Done

- [ ] All thirteen plan acceptance criteria are verified with evidence recorded at T-030
- [ ] Each of the boundary's twenty-eight criteria resolves to recorded evidence or to a recorded reason it could not be evidenced
- [ ] The Design Gate, Review Gate, Verification Gate and Closure Gate are approved with owners recorded, and no gate is approved by the role that produced the evidence it assesses
- [ ] Task acceptance criteria are satisfied or formally waived, with the waiver recorded against the task identifier
- [ ] Assumptions A-001 to A-007 are each confirmed by the named role or converted to a recorded decision
- [ ] Risks R-001 to R-016 are closed or accepted with named owners
- [ ] Open questions Q-001 to Q-011 are closed or explicitly accepted as carried forward
- [ ] Documentation and the release-impact note are published at T-032
- [ ] No publication, channel creation, account creation or spend commitment occurred during the wave
- [ ] Durable outcomes are recorded to memory per `memory/memory-governance.md`

## Open Questions

`Q-001` to `Q-008` are carried from `SCOPE-2026-0001` with their identifiers unchanged; `Q-009` to `Q-011` are raised by this plan.

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| Q-001 | What near-duplicate distance and source-similarity limit mark an item as insufficiently original, derived from the first produced items rather than assumed | no | omn-product-owner | plan-wide; no task in this plan, the judgements being excluded at `X-005` |
| Q-002 | What numeric per-task quality floors and per-item cost target replace the structural floor and the envelope comparison | no | omn-product-owner, for CEO ratification | T-005, T-025 |
| Q-003 | What clean-record threshold relaxes per-publication owner approval, proposed from the baseline this wave instruments, and does the CEO accept it | no | CEO, on a proposal from omn-product-owner | T-019 |
| Q-004 | What target values apply to profit and to audience growth, which no operating baseline and no observed revenue parameter currently allow to be set | no | CEO | T-025 |
| Q-005 | Will human contributors work alongside the AI workforce, and under whose approval | no | CEO | T-012, T-020 |
| Q-006 | Does the committed rate include short-form items, and which programme threshold route is taken | no | CEO | T-005 |
| Q-007 | What protection standard applies to the production-account credentials, given that credential sharing is prohibited by every provider whose terms were examined | no | architect | T-015, T-016, T-017 |
| Q-008 | What retention and privacy expectations apply to research material, analytics, recorded history and audience data | no | omn-product-owner | T-010 |
| Q-009 | Does the technology selection taken at the Design Gate split or merge any of the settling tasks, in particular where one record contract would otherwise span more than one store | no | architect | T-006, T-009, T-020, T-023 |
| Q-010 | Can the acceptance evidence for the accounting criteria be produced over recorded operation sets that commit no spend, as `A-002` assumes | no | omn-product-owner | T-004, T-005, T-030 |
| Q-011 | Who decides the implement-feature Verification Gate, given that the gate matrix names omn-qa as its only owner and omn-qa produces the evidence that gate assesses, which the Producer Exclusion Rule forbids it from approving | no | omn-orchestrator | T-030 |

## Traceability Matrix

| Statement | Covered by |
|---|---|
| S-001 | T-001, T-002, T-028, T-030; A-003, A-006; R-001, R-005, R-007, R-011 |
| S-002 | T-003, T-004, T-005, T-029, T-030; A-002; R-004 |
| S-003 | T-006, T-007, T-008, T-028, T-029, T-030; R-014 |
| S-004 | T-009, T-010, T-011, T-029, T-030, T-032; R-006 |
| S-005 | T-012, T-013, T-014, T-028, T-030, T-031; R-013 |
| S-006 | T-015, T-016, T-017, T-029, T-030, T-031; R-012; Q-007 |
| S-007 | T-003, T-018, T-029, T-030; A-005; R-003 |
| S-008 | T-014, T-019, T-028, T-029, T-030; R-015; Q-003 |
| S-009 | T-020, T-021, T-022, T-028, T-030, T-032; R-010 |
| S-010 | T-023, T-024, T-028, T-030 |
| S-011 | T-025, T-029, T-030; R-002; Q-004 |
| S-012 | T-020, T-026, T-027, T-028, T-030, T-031; R-016 |
