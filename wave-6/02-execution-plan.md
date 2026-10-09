```yaml
plan:
  planId: PLAN-2026-0006
  sourceInputs:
    - type: feature-request
      reference: tasks/MC-7/input.md
  producedBy: planner
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  inputDigest: sha256:52af060278b94230e48a5eb0ed2036e2
  contextDigest: sha256:62e7676afa2df10b3af32629f294e88a
```

## Executive Summary

This plan decomposes the multi-channel capability and its instrumentation into forty tasks across six execution waves, for the CEO and sole owner, who decides whether a second channel ever exists and who must be able to take the approval-workload re-examination on what the company's own records observed. Success is that a further channel becomes a recorded owner decision plus configuration and nothing else, that the payee is one company-level fact no channel can differ on, that every analytics, budget and approval reading is readable per channel and for the company inside the closed three-case measurement union, and that the eight carried items read closed or carry their limit structurally, while no channel is created, configured as live or routed to in the company's own store. Eleven architect decision tasks and one technical-lead decision task precede fourteen implementation tasks, thirteen verification tasks and one documentation task; the first wave holds the twelve decision tasks and the one check the scope requires for the entry point's unclaimed-rest path, none of which has an unmet dependency. The highest-impact risk is a collapse of the observed-zero and unmeasured cases once readings are partitioned, because a channel known to the register with an empty count and a channel or period never measured must render differently and the partition multiplies the places the collapse can occur. Two further facts from the delivered source shape the plan: the company ceiling of USD 77.41 includes a standing charge that the operation record does not carry, and the library-registration attribute the scope lists as per-channel configuration is also a coded precondition that must never become satisfiable from configuration; both are registered as assumptions with matching risks and routed questions. Plan status is complete: every in-scope statement is decomposed, and none of the six open questions blocks decomposition.

## Business Objectives

1. Bringing a further channel into being costs the owner a recorded decision plus configuration and no further development, and a channel so brought into being is neither live nor routed to. Received by the CEO and sole owner. Measured by a fixture channel being brought into being in a throwaway store with zero source changes and every reading partitioning for it. Traces to S-001, S-013.
2. The company carries one payee for every channel, so no channel can be configured to differ on payment, and each channel's risk profile tells the owner the channels are related through that payee. Received by the owner, who accepted the shared-payee enforcement exposure. Measured by the payment-account condition resolving identically for every channel and by the shared-payee statement read on every channel's risk profile. Traces to S-002.
3. The owner can read every analytics, cost and throughput figure the company publishes for one channel or for the whole company, and can tell a channel observed to hold none from a channel never measured. Received by the owner and the shared operator roles that plan across channels. Measured by every published reading resolving per channel to exactly one of observed, observed zero or unmeasured, and by additive company figures equalling the sum of their channels. Traces to S-003, S-015, S-017.
4. The owner can see how much of each channel's budget, and of the company ceiling across all channels, the recorded work has consumed, at the four thresholds, without a configured number standing in for a measurement. Received by the owner and the budget holders. Measured by each utilisation and threshold reading stating its measurement case and by a channel with no recorded amount reading unmeasured. Traces to S-004, S-014.
5. The owner can take the approval-workload re-examination on observation once a real approval series exists, because the items awaiting approval and the effort each approval took are readable per channel, while approval itself stays uniform across channels. Received by the owner, who approves every publication individually. Measured by the per-channel listing equalling the gate state and by approval counts, change-request counts and effort components reading per channel in their measurement cases with no threshold anywhere. Traces to S-005, S-006.
6. The owner can rely on what the company's records say: a month figure presented as final does not change afterwards, the audit chain does not break under concurrent writers, the record sources are written by production code, and no stage duration mixes two clocks unannounced. Received by the owner and every later change that reads these records. Measured by the finality demonstration, the concurrency demonstration, the production-path demonstration and the clock demonstration. Traces to S-007, S-008, S-009, S-011.
7. A new item can be carried to the owner's approval on real recorded state through the rights check, so the entry point is exercisable without a fixture gate state, while the dispatch it then reaches is still refused. Received by the owner. Measured by an audited Draft to awaiting-rights-check to awaiting-owner-approval sequence over a store holding no fixture gate-state row. Traces to S-010, S-013.
8. Every release record cites the version the repository declares rather than one it derived. Received by the owner and every reader of a release record. Measured by one declared version string present in the build properties, the built assemblies and the release record. Traces to S-012.
9. The company's refusal behaviour, spend position and completed capability stay unweakened by the change. Received by the owner, who alone can discharge a precondition, authorise spend or create a channel. Measured by the structural absences, the coded preconditions, the owner-approval state, the boundary assertions, the committed spend and the delivered threshold tracking being unchanged or only extended after the change. Traces to S-013, S-014, S-016.

## Technical Objectives

1. Each of the ten per-channel attributes is held and read per channel, an attribute never recorded reads as not recorded rather than as a default, and no per-channel value reaches a gate state, a precondition, a payee or a routing decision. Verified by round-trip demonstration over two fixture channels and review of the delivered configuration set. Traces to business objective 1 and business objective 9.
2. The payment-account condition is resolved for every channel from a single company-level record, and no record shape or configuration key can hold a per-channel payee value. Verified by demonstration before and after the company-level record changes and by schema review. Traces to business objective 2.
3. Every published reading type takes channel as a partition key, every per-channel quantity is a member of the closed three-case union, and a quantity added outside it fails the solution build naming the offending type. Verified by the build-time membership assertion and a side-by-side demonstration of the two zero-like cases. Traces to business objective 3.
4. Budget utilisation and threshold readings per channel and for the company resolve from the recorded operation cost into the three cases, with each crossing raising exactly one alert however often evaluated. Verified by repeated evaluation over fixture channels with recorded operations. Traces to business objective 4.
5. The per-channel approval listing is derived from gate state alone and exposes no action that changes gate state, and approval counts, change-request counts and effort components resolve per channel into the three cases. Verified by demonstration over items in several gate states and by review of the listing's actions. Traces to business objective 5.
6. Every month reading over the operation record, the per-channel partitions and budget utilisation included, is governed by one finality rule or by one stated structural limit. Verified by committing an operation into a month after a reading of it. Traces to business objective 6.
7. Concurrent audit appenders produce one linear chain in which every appender's entry appears exactly once or its refusal is reported. Verified by a concurrency demonstration followed by chain verification. Traces to business objective 6.
8. Every gate state change passes the gate transition table, and the awaiting-rights-check state has a writer. Verified by the delivered table check and a refused presentation from Draft. Traces to business objective 7.
9. The build properties declare exactly one version string that the built assemblies carry. Verified by inspection of the build properties and the built output. Traces to business objective 8.
10. Every type this change adds sits inside the build-time boundary assertions, and deterministic work stays incapable of a model call. Verified by a solution build that runs the assertions and fails on a seeded violation. Traces to business objective 9.

## Scope

### In Scope

- S-001: Each channel holds its own audience, brand, voice, tone, language, visual identity, content strategy, schedule, library registration and risk profile, readable per channel; a further channel is brought into being by configuration alone and is neither live nor routed to. Delivered by T-001 and T-013, verified by T-027.
- S-002: The payee and the payment account are one company-level fact shared by every channel, the payment-account condition resolves identically for every channel, no channel can be configured to differ on it, and each channel's risk profile states that the channels share one payee. Delivered by T-002 and T-014, verified by T-028.
- S-003: Every analytics, cost and throughput reading the company publishes is readable per channel and for the company, inside the closed three-case measurement union. Delivered by T-003 and T-015, verified by T-029.
- S-004: Utilisation of each channel's budget, and of the company ceiling across all channels, is readable per period at the 50, 75, 90 and 100 percent thresholds from the recorded operation cost, each reading stating its measurement case. Delivered by T-004 and T-016, verified by T-030.
- S-005: Items awaiting owner approval are listable per channel from gate state alone, and owner approval stays a state in the gate transition table with nothing in configuration naming, granting or bypassing it. Delivered by T-005 and T-017, verified by T-031.
- S-006: The effort each owner approval took, and the number of approvals and change requests per period, are readable per channel, each stating its measurement case. Delivered by T-006 and T-018, verified by T-032.
- S-007: A month reading over the operation record, the cost readings and the tier ratio included, is final by construction once its month closes or states structurally that it may still change. Delivered by T-007 and T-019, verified by T-033.
- S-008: At least one production code path writes to the record sources through the recorder. Delivered by T-008 and T-020, verified by T-034.
- S-009: Two audit appenders in flight at once cannot both append after the same head. Delivered by T-009 and T-021, verified by T-035.
- S-010: A new item is brought from Draft to awaiting owner approval on real recorded state through the awaiting-rights-check state, with no transition the table does not admit. Delivered by T-010 and T-022, verified by T-036.
- S-011: The entry point's unclaimed-rest path is exercised by a check, and no stage duration is read across instants from two clocks without stating it. Delivered by T-011, T-023 and T-024, verified by T-037.
- S-012: The repository declares one version, which release records read rather than derive. Delivered by T-012 and T-025, verified by T-038, recorded by T-040.
- S-013: Nothing publishes, no channel is created, configured as live or routed to in the company's own store, the five structural absences on the upload path stay structural, and the three coded preconditions stay encoded and undischarged with unknown folding to not-satisfied while staying visible. Delivered by T-001, T-005, T-010 and T-022, verified by T-039.
- S-014: Committed spend stays zero against the unchanged USD 77.41 monthly ceiling, the authorised single-metered-operation exception stays unspent, and no channel budget amount or channel configuration value is recorded in the company's own store. Delivered by T-001, T-004, T-013 and T-016, verified by T-039, recorded by T-040.
- S-015: The closed three-case union, the build-time membership assertion naming the offending type, the table checks admitting exactly the permitted row shapes and the record horizon are extended and never weakened. Delivered by T-003, T-015 and T-026, verified by T-029 and T-039.
- S-016: The six completed waves are built upon and none is re-created, the delivered department-and-channel threshold tracking included, and deterministic work stays structurally incapable of a model call with the three recorded limits of the boundary assertions carried as stated. Delivered by T-004, T-016 and T-026, verified by T-039.
- S-017: No quantity is invented: no clip count, sizing or sustainable-rate claim, measured value for the assumed reasoning-tier split, approval threshold or workload figure, or lit revenue-derived per-channel figure, and the subject stays a re-pointable recorded experiment. Delivered by T-003 and T-006, verified by T-039, recorded by T-040.

### Out of Scope

- Creating a second or third channel in the company's own store or on any platform, configuring one as live, or routing work to one. Excluded because the supplied request forbids it and the owner's decision on channel isolation recorded 2026-10-08 authorises no channel creation; fixture channels exist only in throwaway demonstration stores.
- Publication of any kind, including upload, account creation, purchase, authenticated subscription session and render. Excluded because the no-publish constraint is carried forward unchanged.
- Discharging any of the three coded preconditions, or making any of them satisfiable from configuration for any channel. Excluded because each refuses from recorded state and only the owner can discharge it.
- The approval-workload re-examination itself, and any approval-workload figure, clean-record threshold, target or pass line for approval effort. Excluded because one approval measurement of the weakest class exists and the owner reserves both until a series including a change request exists.
- Separate legal entities, a payee or payment account per channel, and any configuration under which channels could differ in payee. Excluded because the owner settled one legal entity with one payee for every channel.
- The content of any channel's configuration, including channel one's audience, brand, voice, niche, schedule and budget amount. Excluded because choosing a channel's identity and budget is the owner's, routed under Q-002 and Q-003.
- Lighting any revenue-derived per-channel figure, profit per channel included. Excluded because no observed revenue parameter is recorded and partitioning does not make a figure measurable.
- Any spend, a second stock library, and any drawing of the authorised single-metered-operation exception. Excluded because nothing in scope needs a metered operation to demonstrate.
- Any clip count, supply figure, sizing or sustainable-rate claim, and any measured value for the assumed reasoning-tier split. Excluded because none was observed and none may be simulated.
- The known issues carried open from the previous three release records other than the eight items this scope names. Excluded because each stays with the owner its release record named.

### Deferred

- Recording channel one's budget amount and configuration values in the company's own store. Brought into scope when the owner records them in answer to Q-002 and Q-003.
- Recording standing commitments against the company ceiling, so that the company-level utilisation covers more than the operation record. Brought into scope if the answer to Q-001 requires it and the owner records the standing commitment as incurred.
- Closing the three recorded limits of the boundary assertions: a reference declared and never used still passes, a single-project build does not start the target, and a named property bypasses it. Brought into scope when any is raised as a request of its own.

## Assumptions

| ID | Assumption | Basis | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | The Scope Gate approved the scope as bounded and ruled that nothing in this change records a channel budget amount or channel configuration values in the company's own store, and that a channel without a recorded amount reads unmeasured. The ruling reached this phase only through the dispatch briefing, because a gate rationale reaches no downstream phase | S-004 | A task recording a budget amount or a configuration value is added, and the criteria of T-004, T-016, T-030 and T-040 change | omn-business-analyst |
| A-002 | The owner's decision on channel isolation recorded 2026-10-08, one legal entity with one payee and one payment account for every channel, binds this change although the supplied request still describes the isolation question as open | S-002 | The payee tasks are replaced by a per-channel payee design and the risk-profile statement changes | omn-product-owner |
| A-003 | Verification tasks are owned by omn-dev-2-reviewer because the role the decomposition rule names owns no phase in the routed workflow, so a task owned by it would never be dispatched | plan-wide | Ownership of thirteen verification tasks moves and the phase mapping changes, or the verification is never dispatched | omn-orchestrator |
| A-004 | omn-tech-lead owns no phase in the routed workflow, so its version decision is planned in the solution-design phase and taken no later than the Design Gate, where it is a listed owner, before the implementation task that declares the version | S-012 | The version decision is not dispatched, and T-025 waits or declares a version nobody decided | omn-orchestrator |
| A-005 | Library registration as a per-channel attribute is the recorded per-channel registration register already delivered, not a configuration value, so the library-registration precondition stays unsatisfiable from configuration | S-001 | The precondition becomes satisfiable from a configuration value, breaching the exclusion on discharging preconditions, and the attribute needs its own design task | architect |
| A-006 | The operation record carries metered per-operation cost only, and the standing monthly charge inside the USD 77.41 ceiling is not an operation, so the company-level utilisation read from recorded operation cost covers only metered work and must say so | S-004 | The company-level reading needs a further recorded source for standing commitments, and a task is inserted ahead of T-016 | architect |
| A-007 | Every per-channel month reading over the operation record, budget utilisation included, falls under the one finality rule or stated limit the operation record receives, so partitioning multiplies the readings that rule governs rather than needing a rule of its own | S-007 | Separate finality designs are needed for the partitioned and budget readings, and further decision and build tasks are added | architect |
| A-008 | The records every published reading is read from carry, or reach by an existing relation, the channel of the item they concern, so partitioning needs no new attribution on a delivered record; a reading whose record cannot reach a channel reads unmeasured per channel rather than prompting a new record | S-003 | A record-change task is inserted ahead of T-015 and the change widens into a completed wave | architect |
| A-009 | Every demonstration runs against a throwaway store in the separate demonstration database that drops itself, so no fixture channel, fixture budget or satisfied condition row enters the company's own store | S-013 | A demonstration row lands in an append-only record that refuses correction, and a fixture channel becomes a created channel | omn-tech-lead |
| A-010 | The recorder's first production caller can be a path that performs no metered operation, so the production-caller criterion is demonstrable at zero spend | S-008 | The first caller needs a metered operation, becomes contingent on spend, and blocks on an owner decision | architect |
| A-011 | Whether channel one is recorded in the company's own store, and whether any payment-account observation is recorded there, is not established by this phase, which read no store; the plan demonstrates over fixture channels only and treats the history to be preserved as possibly empty | S-002 | The payee conversion must carry recorded history the plan did not size, and T-014 widens | omn-tech-lead |

## Risks

| ID | Class | Trigger | Impact | Likelihood | Affects | Mitigation | Owner |
|---|---|---|---|---|---|---|---|
| R-001 | requirement | The owner records a channel budget amount, or rules that channel amounts must sum within the ceiling, before or during this change, contrary to A-001 | A recording task is added and the budget readings' criteria change | low | T-004, T-016, T-030 | Q-002 routes the question; T-016 reads a channel with no recorded amount as unmeasured whatever the answer | omn-product-owner |
| R-002 | requirement | The library-registration attribute is delivered as a configuration value, contrary to A-005 | A coded precondition becomes satisfiable from configuration | medium | T-001, T-013, T-039 | Q-006 routes the reading; T-001 states which attributes are configuration and which are recorded state, and T-039 checks no precondition is satisfiable from configuration | architect |
| R-003 | requirement | The company-level utilisation read from operation cost alone is read as the company's whole consumption of the ceiling, contrary to A-006 | Utilisation is understated by the standing charge once that charge is incurred | medium | T-004, T-016, T-030 | Q-001 routes the coverage question; T-016 states on the reading what the operation record does and does not carry | architect |
| R-004 | requirement | A per-channel schedule or content-strategy value is read by a component as a due time, a trigger or a routing target | A channel brought into being by configuration becomes routed to or live | low | T-001, T-013, T-027 | T-001 states that no per-channel value is read as a trigger, and T-027 checks zero routed jobs for the fixture channel | architect |
| R-005 | requirement | The owner records a corporate structure other than one legal entity, contrary to A-002 | The payee tasks and the risk-profile statement are replaced | low | T-002, T-014, T-028 | The decision record is cited in T-002; any reversal returns the plan to this phase | omn-product-owner |
| R-006 | technical | A published reading is read from a record that cannot reach a channel, contrary to A-008 | A record-change task is inserted and the change widens into a completed wave | medium | T-003, T-015, T-029 | T-003 names, for every published reading, the record it partitions over and how that record reaches a channel | architect |
| R-007 | technical | A company figure differs from the sum of its per-channel figures, through rounding, application-side arithmetic or a record reaching no channel | The additivity the owner reads company figures by fails silently | medium | T-003, T-015, T-016, T-029 | T-003 names every additive quantity and keeps money arithmetic where the delivered design keeps it, and T-029 checks exact equality | architect |
| R-008 | technical | A channel known to the register with an empty count and a channel or period never measured render identically in a partitioned reading | An unmeasured channel reads as a zero and the discipline fails silently | high | T-003, T-015, T-016, T-018, T-029, T-030, T-032 | T-003 states the condition for each case per channel before build, and T-029, T-030 and T-032 render both cases side by side | architect |
| R-009 | technical | The conversion of the payment-account condition to a company-level fact loses a recorded observation, or leaves a per-channel row still read, contrary to A-011 | History is lost from an append-only discipline, or two channels resolve the condition differently | medium | T-002, T-014, T-028 | Q-004 routes the conversion to the design; T-028 demonstrates before and after the company-level record changes | architect |
| R-010 | technical | Making the presentation for owner approval pass the transition table refuses a delivered caller or fixture that presents from Draft | Delivered checks fail and must be re-pointed through the rights check | medium | T-010, T-022, T-036 | T-010 names every delivered caller of the presentation before T-022 builds | architect |
| R-011 | technical | The finality rule leaves most per-channel month readings unmeasured while operations are in flight, or governs some month readings and not others, contrary to A-007 | Readings the owner needs read unmeasured, or a reading presented as final later changes | medium | T-007, T-015, T-016, T-019, T-033 | T-007 lists every month reading over the operation record the rule governs, and T-033 reviews each against it | architect |
| R-012 | technical | Serialising audit appenders deadlocks against the shared hold on the record horizon or against the horizon move a throughput read makes | Writers stall or abort, and throughput reads return unmeasured more often | medium | T-009, T-021, T-035 | T-009 states the hold order against the record horizon, and T-035 runs concurrent appenders together with throughput reads | architect |
| R-013 | technical | The recorder's first production caller needs a metered operation, contrary to A-010 | The production-caller criterion becomes contingent on spend | low | T-008, T-020, T-034 | T-008 names a caller that performs no metered operation or raises the conflict to the owner, and T-034 checks zero metered operations | architect |
| R-014 | technical | A single-clock rule for stage rows changes the instants delivered rows carry, or leaves earlier mixed rows readable as durations | A cycle-time reading mixes clocks without saying so | low | T-011, T-023, T-037 | T-011 states what rows recorded before the change read as | architect |
| R-015 | security | The per-channel approval listing exposes an action that changes gate state, or a configuration key names, grants or skips owner approval | A route around owner approval exists | low | T-005, T-017, T-031 | T-005 states the listing is read-only over gate state, and T-031 reviews every action and key | architect |
| R-016 | security | A payee or payment-account value becomes expressible per channel in a record shape or a configuration key | Two channels of one company can differ on payee, contrary to the owner's decision | low | T-001, T-002, T-014, T-028 | T-002 fixes the single company-level record, and T-028 reviews every delivered shape and key | architect |
| R-017 | security | A writer appends to the audit record without the serialisation the appender adopts | The chain can still fork and verification reports a break between genuine entries | medium | T-009, T-021, T-035 | T-009 states whether the control binds every writer or the appender only, and T-035 verifies the chain after concurrent writes | architect |
| R-018 | security | The per-channel configuration surface admits a key reaching a gate state, a refusal, a precondition or the payee | A control or precondition becomes a setting | low | T-001, T-013, T-039 | T-001 bars every such key, and T-039 reviews the delivered configuration set | architect |
| R-019 | operational | A verification task is assigned to a role that owns no phase in the routed workflow | The verification never runs and the change closes with unverified criteria | medium | plan-wide | A-003 assigns verification to the role that owns the quality-review phase, and the Suggested Workflow mapping is the check | omn-orchestrator |
| R-020 | operational | The version decision is not dispatched because its owner holds no phase, contrary to A-004 | T-025 declares a version nobody decided, or waits indefinitely | medium | T-012, T-025 | Q-005 names the owner, and the orchestrator obtains the decision at the Design Gate | omn-orchestrator |
| R-021 | operational | A demonstration is pointed at a store holding the company's own records, or a fixture channel is left in a store that is kept, contrary to A-009 | A demonstration row enters an append-only record, or a fixture channel becomes a created channel | low | T-027, T-028, T-029, T-030, T-031, T-032, T-033, T-034, T-035, T-036 | Each verification task records the store it ran against and that the store held no table afterwards | omn-tech-lead |
| R-022 | delivery | A new type is checked only by a single-project build, referenced by a declaration that is never used, or built with the named bypass property | The boundary assertions do not see the type and a violation goes unrefused | medium | T-026, T-039 | T-039 takes its evidence from a solution build, and the three limits are carried as stated rather than claimed closed | omn-tech-lead |
| R-023 | delivery | The closing phase blocks at capability resolution, as the routed workflow's resolution rules still record | T-040 does not run and the release record is not produced | low | T-040 | The previous five changes closed this phase, so the block is watched for at dispatch rather than presumed | omn-orchestrator |
| R-024 | delivery | Twelve scope items in one change exceed what one implementation pass completes | Should-have and could-have items slip and the change closes partial | medium | plan-wide | The scope's priorities are kept on every task, and the carried items are independent of the must-have tasks in the dependency graph | omn-tech-lead |

## Task Breakdown

### T-001 Decide how each channel's attributes are held and read

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: none
- Traces to: S-001, S-013, S-014, A-005
- Status: assumption-dependent
- Description: A recorded design states, for each of the ten per-channel attributes, whether it is held as per-channel configuration or read from a recorded per-channel register, how it is read back per channel, and what an attribute never recorded reads as, answering Q-006.
- Acceptance Criteria:
  - The design names, for each of audience, brand, voice, tone, language, visual identity, content strategy, schedule, library registration and risk profile, where it is held and how it is read per channel.
  - The design states that an attribute never recorded for a channel reads as not recorded, never as a default value.
  - The design states that library registration stays recorded state, and that no per-channel value can satisfy a coded precondition.
  - The design states that no per-channel value is read as a due time, a trigger, a routing target, a gate state, a refusal or a payee.
  - The design states that a channel brought into being by configuration is neither live nor routed to, and that no value for channel one is recorded by this change.
- Gate: Design Gate

### T-002 Decide how the payee becomes one company-level fact

- Owner: architect
- Complexity: S (confidence: low)
- Depends on: none
- Traces to: S-002, A-002, A-011
- Status: assumption-dependent
- Description: A recorded design states how the payment-account condition, recorded per channel today, is resolved from one company-level record for every channel without losing recorded history, answering Q-004.
- Acceptance Criteria:
  - The design names the single company-level record the payment-account condition is resolved from for every channel of the company.
  - The design states what happens to payment-account observations already recorded per channel, with none lost.
  - The design states that no record shape or configuration key can hold a per-channel payee or payment-account value.
  - The design states how each channel's risk profile reads that the channels share one payee, and why that statement cannot be configured away.
  - The design states that the other two coded preconditions stay per channel and unchanged.
- Gate: Design Gate

### T-003 Define the per-channel partition of every published reading

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: none
- Traces to: S-003, S-015, S-017, A-008
- Status: assumption-dependent
- Description: A recorded design lists every reading the company publishes, the record each is partitioned over, how that record reaches a channel, and the condition under which each per-channel quantity reads observed, observed zero or unmeasured.
- Acceptance Criteria:
  - The design lists every published reading and states that none is company-only after the change.
  - The design names, for each reading, the record it is read from and the relation by which that record reaches a channel.
  - The design states, per channel, when a quantity reads observed zero and when it reads unmeasured, and the detail an unmeasured reading names.
  - The design names every additive quantity whose company figure must equal the sum of its per-channel figures.
  - The design keeps every revenue-derived figure dark and adds no quantity outside the closed three-case union.
- Gate: Design Gate

### T-004 Decide how budget utilisation reads per channel and for the company

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: none
- Traces to: S-004, S-014, S-016, A-001, A-006
- Status: assumption-dependent
- Description: A recorded design extends the delivered department-and-channel threshold tracking with the measurement case of each reading and a company-level reading against the monthly ceiling, answering Q-001, without re-creating the delivered tracking.
- Acceptance Criteria:
  - The design states the condition under which each channel's utilisation and each of the four thresholds read observed, observed zero or unmeasured.
  - The design states that a channel with no recorded budget amount reads unmeasured naming the missing amount, and that no percentage is computed against an unrecorded amount.
  - The design states where the company ceiling is read from and what the company-level utilisation covers, including whether standing commitments are inside it.
  - The design states that the delivered idempotent alert per budget, period and threshold is kept, and names every delivered element it extends rather than replaces.
- Gate: Design Gate

### T-005 Decide the per-channel approval listing over gate state

- Owner: architect
- Complexity: S (confidence: medium)
- Depends on: none
- Traces to: S-005, S-013
- Status: ready
- Description: A recorded design states how the items awaiting owner approval are listed per channel as a view derived from each item's gate state alone.
- Acceptance Criteria:
  - The design states that the listing is derived only from the recorded gate state and the item's channel.
  - The design states that the listing exposes no action that changes a gate state.
  - The design states that no configuration key names, grants, skips or routes around owner approval.
  - The design states that approval stays uniform across channels and only the listed items vary by channel.
- Gate: Design Gate

### T-006 Decide the per-channel approval-workload readings

- Owner: architect
- Complexity: S (confidence: medium)
- Depends on: none
- Traces to: S-006, S-017
- Status: ready
- Description: A recorded design states how the approval count, the change-request count and each of the three delivered approval-effort components read per channel and period, each in one of the three measurement cases.
- Acceptance Criteria:
  - The design names the record each count and component is read from and how it reaches a channel.
  - The design states that a channel with no recorded approval in a period reads unmeasured in every effort component, never zero minutes.
  - The design states that the delivered independence of the three effort components is kept.
  - The design states no threshold, target, pass line or workload figure for approval effort.
- Gate: Design Gate

### T-007 Decide the finality rule over the operation record

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: none
- Traces to: S-007, A-007
- Status: assumption-dependent
- Description: A recorded design either extends finality to the month readings over the operation record or states the limit structurally on each such reading, and lists every month reading the rule governs.
- Acceptance Criteria:
  - The design lists every month reading over the operation record, naming the cost readings, the tier ratio, their per-channel partitions and budget utilisation.
  - The design states one rule, or one structural limit, that governs every listed reading.
  - The design states what a reading taken before its month is final reads as, and that a reading presented as final is unchanged by any later operation.
  - The design states how the rule relates to the delivered record horizon, which is extended and not weakened.
- Gate: Design Gate

### T-008 Decide where the recorder's first production caller lives

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: none
- Traces to: S-008, A-010
- Status: assumption-dependent
- Description: A recorded design names the production code path that first writes a record source through the recorder, and states that it performs no metered operation.
- Acceptance Criteria:
  - The design names the production caller and the record source or sources it writes through the recorder.
  - The design states that the caller performs zero metered operations, or names the conflict and raises it to the owner.
  - The design states that the caller adds no position after composition in the publishing sequence.
  - The design names every delivered component the caller reuses rather than re-creates.
- Gate: Design Gate

### T-009 Decide the concurrency control for audit appending

- Owner: architect
- Complexity: S (confidence: medium)
- Depends on: none
- Traces to: S-009
- Status: ready
- Description: A recorded design states how two appenders in flight at once are prevented from appending after the same head, and what an appender that loses the race experiences.
- Acceptance Criteria:
  - The design states the control that makes the audit chain linear under concurrent appenders.
  - The design states whether the control binds every writer of the audit record or the appender only.
  - The design states that an appender losing the race appends exactly once or reports its refusal, and is never silently dropped.
  - The design states the hold order against the record horizon's shared hold, so that no deadlock is introduced.
- Gate: Design Gate

### T-010 Decide the path from Draft to awaiting owner approval

- Owner: architect
- Complexity: S (confidence: medium)
- Depends on: none
- Traces to: S-010, S-013
- Status: ready
- Description: A recorded design names the writer of the awaiting-rights-check state and states how the presentation for owner approval is refused from any state the gate transition table does not admit.
- Acceptance Criteria:
  - The design names the component that brings a Draft item to awaiting-rights-check and the condition under which it does.
  - The design states that every gate state change, the presentation for owner approval included, passes the gate transition table.
  - The design names every delivered caller or check that presents an item for owner approval and how each reaches the presentation afterwards.
  - The design states that no edge from Draft bypasses the rights check and that the dispatch an item then reaches is still refused by each undischarged condition.
- Gate: Design Gate

### T-011 Decide the clock rule for stage rows

- Owner: architect
- Complexity: XS (confidence: medium)
- Depends on: none
- Traces to: S-011
- Status: ready
- Description: A recorded decision states either that every stage row's entry and exit instants come from one clock or that every reading over stage durations states the two-clock limit.
- Acceptance Criteria:
  - The decision names which of the two outcomes is delivered.
  - The decision states what stage rows recorded before the change read as.
  - The decision states that no stage duration is computed by this change.
  - The decision names the deferred cycle-time measure as the first reading the rule binds.
- Gate: Design Gate

### T-012 Decide the version string the repository declares

- Owner: omn-tech-lead
- Complexity: XS (confidence: low)
- Depends on: none
- Traces to: S-012, A-004
- Status: assumption-dependent
- Description: A recorded decision names the one version string the repository declares, answering Q-005.
- Acceptance Criteria:
  - The decision names exactly one version string.
  - The decision states whether the version previously derived by release records is adopted or superseded.
  - The decision is recorded before the implementation task that declares the version starts.
  - The decision states that release records read the declared version rather than derive one.
- Gate: Design Gate

### T-013 Deliver per-channel attributes readable per channel

- Owner: omn-dev-1-implement
- Complexity: L (confidence: low)
- Depends on: T-001
- Traces to: S-001, S-014, A-005
- Status: assumption-dependent
- Description: Each of the ten per-channel attributes is held and read per channel as T-001 designs it, and an attribute never recorded reads as not recorded.
- Acceptance Criteria:
  - Each attribute recorded for one channel reads back for that channel and changes zero values read for any other channel.
  - An attribute never recorded for a channel reads as not recorded, with no default value in its place.
  - Library registration reads from the recorded per-channel register, and no per-channel value satisfies a coded precondition.
  - No channel configuration value and no channel budget amount is recorded in the company's own store by any code, seed or migration this task delivers.
- Gate: none

### T-014 Deliver the company-level payee fact

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-002, T-013
- Traces to: S-002, A-002, A-011
- Status: assumption-dependent
- Description: The payment-account condition resolves for every channel from the single company-level record T-002 designs, and each channel's risk profile reads that the channels share one payee.
- Acceptance Criteria:
  - The payment-account condition read for any two channels of one company resolves from the same record and to the same standing.
  - No record shape or configuration key this task delivers holds a per-channel payee or payment-account value.
  - Every payment-account observation recorded before the change remains readable after it.
  - Every channel's risk profile reads that the channels share one payee, and no configuration value removes the statement.
- Gate: none

### T-015 Partition every published reading by channel

- Owner: omn-dev-1-implement
- Complexity: L (confidence: low)
- Depends on: T-003, T-007
- Traces to: S-003, S-015, A-008
- Status: assumption-dependent
- Description: Every published reading other than the budget and approval readings is readable per channel and for the company as T-003 defines it, each per-channel quantity inside the closed three-case union.
- Acceptance Criteria:
  - Every reading T-003 lists, other than the budget and approval readings, is readable for a named channel and for the company.
  - A channel whose count was taken and found empty reads observed zero, and a channel or period with no observation reads unmeasured naming what was looked for.
  - For every additive quantity T-003 names, the company figure equals the sum of the per-channel figures exactly.
  - Every per-channel month reading over the operation record is governed by the rule T-007 decides.
- Gate: none

### T-016 Deliver budget utilisation per channel and for the company

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-003, T-004, T-007
- Traces to: S-004, S-016, A-001, A-006, A-007
- Status: assumption-dependent
- Description: Each channel's utilisation and the company-level utilisation against the monthly ceiling read per period at the four thresholds, each in its measurement case, by extending the delivered threshold tracking.
- Acceptance Criteria:
  - Each channel's utilisation and each of the four thresholds resolve to exactly one of observed, observed zero or unmeasured.
  - A channel with no recorded budget amount reads unmeasured naming the missing amount, and no percentage is computed against it.
  - The company-level reading tracks the total recorded cost of all channels against the USD 77.41 ceiling at the four thresholds and states what the operation record does and does not carry.
  - Each crossing still raises exactly one alert however often it is evaluated, and the delivered tracking is extended rather than replaced.
- Gate: none

### T-017 Deliver the per-channel approval listing

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-005
- Traces to: S-005
- Status: ready
- Description: Items awaiting owner approval are listed per channel from gate state alone, as T-005 designs it.
- Acceptance Criteria:
  - The listing for a channel contains exactly that channel's items whose gate state is awaiting owner approval.
  - The listing contains zero items in any other gate state and zero items of another channel.
  - The listing exposes no action that changes a gate state.
  - No configuration key this task delivers names, grants, skips or routes around owner approval.
- Gate: none

### T-018 Deliver approval-workload readings per channel

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-003, T-006
- Traces to: S-006, S-017
- Status: ready
- Description: The approval count, the change-request count and each approval-effort component read per channel and period as T-006 designs them, each in its measurement case.
- Acceptance Criteria:
  - The approval count and the change-request count read per channel and period, each in one of the three cases.
  - Each of the three effort components reads per channel and period from its own recorded marks.
  - A channel with no recorded approval in a period reads unmeasured in every effort component, never zero minutes.
  - No threshold, target, pass line or workload figure appears on any reading or in any statement published with it.
- Gate: none

### T-019 Deliver the finality rule over the operation record

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-007
- Traces to: S-007, A-007
- Status: assumption-dependent
- Description: Every month reading over the operation record that T-007 lists is governed by the rule or the stated limit T-007 decides.
- Acceptance Criteria:
  - A month reading taken before its month is final reads as not yet final, or is prevented, as T-007 decides.
  - A reading presented as final is unchanged by an operation committed into its month afterwards.
  - The cost readings and the tier ratio are governed by the same rule or limit.
  - The delivered record horizon and its checks are unchanged or extended, never weakened.
- Gate: none

### T-020 Deliver the recorder's first production caller

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-008
- Traces to: S-008, A-010
- Status: assumption-dependent
- Description: The production code path T-008 names writes to at least one record source through the recorder, with no test or fixture writing for it.
- Acceptance Criteria:
  - Driving the production path writes the record source T-008 names through the recorder.
  - The record written reads back through its delivered read port in the case it was written in.
  - The production path performs zero metered operations.
  - The production path adds no position after composition in the publishing sequence.
- Gate: none

### T-021 Deliver linear audit appending under concurrency

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-009
- Traces to: S-009
- Status: ready
- Description: Concurrent audit appenders produce one linear chain under the control T-009 decides.
- Acceptance Criteria:
  - With at least two appenders in flight, zero pairs of entries share a predecessor head.
  - An appender losing the race appends exactly once or reports its refusal.
  - The record horizon's shared hold and its check are unchanged.
  - No amendment or deletion path is added to the audit record.
- Gate: none

### T-022 Deliver the path from Draft through the rights check to owner approval

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-010
- Traces to: S-010, S-013
- Status: ready
- Description: A new item recorded in Draft reaches awaiting owner approval through awaiting-rights-check on real recorded state, and every gate state change passes the transition table, as T-010 designs it.
- Acceptance Criteria:
  - A new item recorded in Draft reaches awaiting-rights-check and then awaiting owner approval, each transition audited.
  - A presentation for owner approval from Draft is refused.
  - The gate transition table admits no edge from Draft that bypasses the rights check.
  - The dispatch the item then reaches is refused by each undischarged first-publication condition, named individually.
- Gate: none

### T-023 Deliver the stage-row clock rule

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-011
- Traces to: S-011
- Status: ready
- Description: Stage rows carry instants from one clock, or every reading over stage durations states the two-clock limit, as T-011 decides.
- Acceptance Criteria:
  - Under the one-clock outcome, zero rows leave before they enter with the process clock set one day behind.
  - Under the stated-limit outcome, every reading over stage durations carries the two-clock statement.
  - No stage duration is computed by this task.
  - Stage rows recorded before the change read as T-011 states.
- Gate: none

### T-024 Exercise the entry point's unclaimed-rest path in a suite

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: none
- Traces to: S-011
- Status: ready
- Description: At least one check in a suite drives the entry point into its unclaimed-rest path, where another worker claims the unit between two positions.
- Acceptance Criteria:
  - A check in a suite reaches the unclaimed-rest path of the entry point.
  - The check asserts what the resting unit reads as in the waiting and claimed readings.
  - The check passes in the suite run that the change records.
  - The check asserts that a second drive starts a new unit rather than resuming the resting one, or names the behaviour it observes instead.
- Gate: none

### T-025 Declare the decided version in the build properties

- Owner: omn-dev-1-implement
- Complexity: XS (confidence: low)
- Depends on: T-012
- Traces to: S-012, A-004
- Status: assumption-dependent
- Description: The build properties declare the one version string T-012 decides, and the built assemblies carry it.
- Acceptance Criteria:
  - The build properties declare exactly one version, and it is a version string.
  - Every built assembly carries the declared version.
  - No other version declaration exists in the repository.
  - The declared string equals the one T-012 decided.
- Gate: none

### T-026 Extend the boundary assertions over every type this change adds

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-013, T-014, T-015, T-016, T-017, T-018, T-019, T-020, T-021, T-022, T-023
- Traces to: S-015, S-016
- Status: ready
- Description: Every type this change adds falls inside the build-time boundary assertions that run on every solution build and fail it.
- Acceptance Criteria:
  - Every analytics type this change adds is inside the membership assertion, and a quantity added outside the union fails the solution build naming the offending type.
  - Every deterministic type this change adds is covered by the no-model-path assertion.
  - The assertions still hold composition as the last position of every declared workflow.
  - The three recorded limits of the assertions are stated unchanged rather than claimed closed.
- Gate: none

### T-027 Verify a fixture channel is brought into being by configuration alone

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-013, T-014, T-015, T-016, T-017, T-018
- Traces to: S-001, S-013, A-003, A-009
- Status: assumption-dependent
- Description: Recorded evidence supports the first, second and third upstream criteria: per-channel attributes round-trip independently, and a fixture channel brought into being in a throwaway store with zero source changes partitions in every reading and stays refused and unrouted.
- Acceptance Criteria:
  - Evidence from a throwaway store holding two fixture channels shows each of the ten attributes changed on one channel and zero values changed on the other.
  - Evidence shows the fixture second channel brought into being with zero source changes, and every reading delivered under S-003, S-004, S-005 and S-006 partitioning for it.
  - Evidence shows the fixture channel refused by all three first-publication conditions, the target of zero routed jobs, and reading each unrecorded attribute as not recorded.
  - Evidence names the store each run used and shows it held no table afterwards.
- Gate: Review Gate, Verification Gate

### T-028 Verify the payee resolves identically for every channel

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-014
- Traces to: S-002, A-003, A-009, A-011
- Status: assumption-dependent
- Description: Recorded evidence supports the fourth, fifth and sixth upstream criteria: one company-level payment-account record for every channel, no per-channel payee value anywhere, and the shared-payee statement on every risk profile.
- Acceptance Criteria:
  - Evidence over two fixture channels shows the payment-account condition resolving identically before and after the company-level record changes.
  - Evidence from review of the delivered record shapes and configuration set shows zero per-channel payee or payment-account values.
  - Evidence shows every fixture channel's risk profile stating the shared payee, with no configuration value able to remove it.
  - Evidence shows payment-account observations recorded before the change still readable after it.
- Gate: Review Gate, Verification Gate

### T-029 Verify every published reading partitions by channel inside the union

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-015, T-026
- Traces to: S-003, S-015, A-003, A-009
- Status: assumption-dependent
- Description: Recorded evidence supports the seventh, eighth and ninth upstream criteria: no published reading is company-only, per-channel quantities stay in the union, company figures sum exactly, and the two zero-like cases render apart.
- Acceptance Criteria:
  - Evidence from a solution build shows a per-channel quantity introduced outside the union failing the build and the check naming the offending type.
  - Evidence from review of the published reading set shows zero company-only readings.
  - Evidence over two fixture channels with recorded operations shows each additive company figure equal to the sum of its per-channel figures exactly.
  - Evidence shows an observed-zero channel and an unmeasured channel read side by side and rendering differently.
- Gate: Review Gate, Verification Gate

### T-030 Verify budget readings per channel and for the company

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-016
- Traces to: S-004, A-001, A-003, A-006, A-009
- Status: assumption-dependent
- Description: Recorded evidence supports the tenth, eleventh and twelfth upstream criteria: per-channel and company utilisation at the four thresholds in their measurement cases, one alert per crossing, and unmeasured where no amount is recorded.
- Acceptance Criteria:
  - Evidence over a fixture channel with recorded operations crossing each threshold, evaluated repeatedly, shows each reading in exactly one case and one alert per crossing.
  - Evidence over a fixture channel with no recorded budget shows an unmeasured reading naming the missing amount and zero percentages computed.
  - Evidence over two fixture channels shows the company total equal to the sum of the per-channel costs and tracked against the USD 77.41 ceiling at the four thresholds.
  - Evidence shows the company-level reading stating what the operation record does and does not carry.
- Gate: Review Gate, Verification Gate

### T-031 Verify the approval listing is a view over gate state

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-017, T-022
- Traces to: S-005, A-003, A-009
- Status: assumption-dependent
- Description: Recorded evidence supports the thirteenth, fourteenth and fifteenth upstream criteria: each channel's listing holds exactly its awaiting items, the union equals the company's set, and nothing in configuration or the listing routes around owner approval.
- Acceptance Criteria:
  - Evidence over two fixture channels holding items in several gate states shows each listing holding exactly that channel's items awaiting owner approval.
  - Evidence shows the union of the listings equal to the company's set of items awaiting owner approval, with no item listed twice.
  - Evidence from review shows zero configuration keys naming, granting, skipping or routing around owner approval, and no listing action changing a gate state.
  - Evidence shows the delivered table check refusing a gate state change outside the transition table.
- Gate: Review Gate, Verification Gate

### T-032 Verify approval-workload readings per channel

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-018
- Traces to: S-006, S-017, A-003, A-009
- Status: assumption-dependent
- Description: Recorded evidence supports the sixteenth, seventeenth and eighteenth upstream criteria: counts and effort components per channel in their cases, unmeasured where no approval was recorded, and no threshold anywhere.
- Acceptance Criteria:
  - Evidence over approvals recorded for two fixture channels, one returning a change request, shows each count and component reading per channel in one of the three cases.
  - Evidence over a fixture channel with no recorded approval shows every effort component unmeasured and never zero minutes.
  - Evidence from review shows no threshold, target, pass line or workload figure on any approval reading or published statement.
  - Evidence labels every seeded approval a demonstration parameter rather than an observation of the company's approval workload.
- Gate: Review Gate, Verification Gate

### T-033 Verify month readings over the operation record are final or say so

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-015, T-016, T-019
- Traces to: S-007, A-003, A-007, A-009
- Status: assumption-dependent
- Description: Recorded evidence supports the nineteenth and twentieth upstream criteria: a reading before finality reads not final or is prevented, a final reading never changes, and every month reading over the operation record sits under the one rule.
- Acceptance Criteria:
  - Evidence from committing an operation into a month after a reading of it shows the earlier reading either not final or unchanged.
  - Evidence from review shows the cost readings, the tier ratio, their per-channel partitions and budget utilisation all under the same rule or limit.
  - Evidence shows zero month readings over the operation record outside the rule.
  - Evidence shows the delivered record horizon and its checks unweakened.
- Gate: Review Gate, Verification Gate

### T-034 Verify a production path writes through the recorder at zero metered operations

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-020, T-026
- Traces to: S-008, A-003, A-009, A-010
- Status: assumption-dependent
- Description: Recorded evidence supports the twenty-first and twenty-second upstream criteria: a production path writes a record source through the recorder with no test or fixture writing for it, at zero metered operations and with no position after composition.
- Acceptance Criteria:
  - Evidence from driving the production path against a throwaway store shows the record source written through the recorder and read back.
  - Evidence shows zero writes from a test or fixture needed for that source.
  - Evidence from a solution build shows the boundary assertions passing over the caller and composition still the last position.
  - Evidence shows zero metered operations recorded by the run.
- Gate: Review Gate, Verification Gate

### T-035 Verify concurrent appenders produce one unbroken chain

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-021
- Traces to: S-009, A-003, A-009
- Status: assumption-dependent
- Description: Recorded evidence supports the twenty-third and twenty-fourth upstream criteria: concurrent appenders leave no shared predecessor head, verification reports no broken link, and each appender's entry appears once or its refusal is reported.
- Acceptance Criteria:
  - Evidence from at least two concurrent appenders against a throwaway store shows zero pairs of entries sharing a predecessor head.
  - Evidence shows chain verification over the result reporting zero broken links.
  - Evidence counting entries per appender shows each present exactly once or its refusal reported.
  - Evidence from appenders run together with throughput reads shows no deadlock, and labels every probe count a probe parameter.
- Gate: Review Gate, Verification Gate

### T-036 Verify an item reaches owner approval through the rights check

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-022
- Traces to: S-010, S-013, A-003, A-009
- Status: assumption-dependent
- Description: Recorded evidence supports the twenty-fifth, twenty-sixth and twenty-seventh upstream criteria: an audited path from Draft through the rights check on real recorded state, a refused bypass, and a dispatch still refused by each condition.
- Acceptance Criteria:
  - Evidence over a throwaway store holding no fixture gate-state row shows the audited transition sequence from Draft through awaiting-rights-check to awaiting owner approval.
  - Evidence shows a presentation attempted from Draft refused, and the table admitting no edge from Draft that bypasses the rights check.
  - Evidence over the same recorded state with every condition at its production value shows the dispatch refused by each undischarged condition, named individually.
  - Evidence shows zero transitions the table does not admit.
- Gate: Review Gate, Verification Gate

### T-037 Verify the two carried low findings are closed

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-023, T-024
- Traces to: S-011, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports the twenty-eighth and twenty-ninth upstream criteria: the unclaimed-rest path is exercised by a passing check, and stage durations never mix clocks unannounced.
- Acceptance Criteria:
  - Evidence from inspection of the suite and its passing run shows a check reaching the unclaimed-rest path.
  - Evidence from a skewed process clock shows zero stage rows leaving before entering, or evidence from review shows every reading over stage durations stating the two-clock limit.
  - Evidence states which outcome T-011 decided and that the evidence matches it.
  - Evidence labels the skewed clock offset a probe parameter rather than an observation.
- Gate: Review Gate, Verification Gate

### T-038 Verify the declared version reaches the built output

- Owner: omn-dev-2-reviewer
- Complexity: XS (confidence: low)
- Depends on: T-025
- Traces to: S-012, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports the thirtieth and thirty-first upstream criteria: one declared version string, carried by the built assemblies.
- Acceptance Criteria:
  - Evidence from inspection of the build properties shows exactly one declared version, and it is a version string.
  - Evidence from inspection of the built output shows every assembly carrying the declared version.
  - Evidence shows the declared version equal to the string T-012 decided.
  - Evidence shows no other version declaration in the repository.
- Gate: Review Gate, Verification Gate

### T-039 Confirm every carried-forward refusal and boundary holds after the change

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-013, T-014, T-016, T-020, T-022, T-026
- Traces to: S-013, S-014, S-015, S-016, S-017, A-003
- Status: assumption-dependent
- Description: Recorded evidence shows the structural absences, the undischarged preconditions, the owner-approval state, the spend position, the boundary assertions and the completed waves unchanged or only extended after the delivered change.
- Acceptance Criteria:
  - Evidence shows each of the five structural absences still structural and none expressible as a configuration value.
  - Evidence shows each of the three coded preconditions still refusing from recorded state, none satisfiable from configuration, and no channel created, live or routed to in the company's own store.
  - Evidence from a solution build shows the boundary assertions running and failing the build on a seeded violation, with the three recorded limits stated as limits.
  - Evidence shows committed spend of zero, the authorised exception unspent, no channel budget amount or configuration value recorded in the company's own store, and no completed wave's capability re-created.
  - Evidence shows no clip count, sizing claim, measured tier split, approval threshold or lit revenue-derived figure in any delivered output.
- Gate: Review Gate, Verification Gate

### T-040 Record what the change delivers and what it leaves to the owner

- Owner: omn-documentation
- Complexity: M (confidence: low)
- Depends on: T-027, T-028, T-029, T-030, T-031, T-032, T-033, T-034, T-035, T-036, T-037, T-038, T-039
- Traces to: S-012, S-013, S-014, S-017, A-001
- Status: assumption-dependent
- Description: The release record states the declared version, what each delivered reading measures and reports unmeasured, and what the change leaves to the owner, supporting the thirty-second upstream criterion.
- Acceptance Criteria:
  - The record states the declared version and records no derived one.
  - The record states that no second channel exists, that channel one's budget amount and configuration values are the owner's, and that the approval-workload re-examination still waits on a series.
  - The record states what the company-level budget reading covers and labels every seeded count a demonstration parameter.
  - The record carries forward, unresolved, the known issues this change did not close.
- Gate: Closure Gate

## Dependencies

### 8.1 Dependency Edges

| From | To | Type | Justification |
|---|---|---|---|
| T-001 | T-013 | contract | The per-channel attributes bind to where and how the design holds and reads each one |
| T-002 | T-014 | contract | The payee fact binds to the single company-level record and the conversion the design fixes |
| T-013 | T-014 | produces-consumes | The shared-payee statement is read on the per-channel risk profile the attribute task delivers |
| T-003 | T-015 | contract | The partitioned readings bind to the reading list, sources and case conditions the design fixes |
| T-007 | T-015 | contract | Per-channel month readings over the operation record bind to the finality rule the design fixes |
| T-003 | T-016 | contract | The budget readings bind to the per-channel case conditions the partition design fixes |
| T-004 | T-016 | contract | The budget readings bind to the measurement cases and company ceiling the design fixes |
| T-007 | T-016 | contract | Budget utilisation is a month reading over the operation record and binds to the finality rule |
| T-005 | T-017 | contract | The listing binds to the view the design fixes |
| T-003 | T-018 | contract | The approval readings bind to the per-channel case conditions the partition design fixes |
| T-006 | T-018 | contract | The approval readings bind to the sources and cases the design fixes |
| T-007 | T-019 | contract | The finality rule delivered is the one the design fixes |
| T-008 | T-020 | contract | The production caller is the one the design names |
| T-009 | T-021 | contract | The appending control is the one the design fixes |
| T-010 | T-022 | contract | The writer of awaiting-rights-check and the table check are the ones the design fixes |
| T-011 | T-023 | decision-gate | Which of two outcomes the stage-row task delivers changes with the decision |
| T-012 | T-025 | decision-gate | The version declared is the one the decision names |
| T-013 | T-026 | produces-consumes | The boundary assertions must cover the types the attribute task adds |
| T-014 | T-026 | produces-consumes | The boundary assertions must cover the types the payee task adds |
| T-015 | T-026 | produces-consumes | The boundary assertions must cover the quantity types the partition task adds |
| T-016 | T-026 | produces-consumes | The boundary assertions must cover the quantity types the budget task adds |
| T-017 | T-026 | produces-consumes | The boundary assertions must cover the types the listing task adds |
| T-018 | T-026 | produces-consumes | The boundary assertions must cover the quantity types the approval-workload task adds |
| T-019 | T-026 | produces-consumes | The boundary assertions must cover the types the finality task adds |
| T-020 | T-026 | produces-consumes | The boundary assertions must cover the production caller the recorder task adds |
| T-021 | T-026 | produces-consumes | The boundary assertions must cover the types the appending task adds |
| T-022 | T-026 | produces-consumes | The boundary assertions must cover the types the gate-path task adds |
| T-023 | T-026 | produces-consumes | The boundary assertions must cover the types the stage-row task adds |
| T-013 | T-027 | verification | The verification judges the per-channel attributes this task produced |
| T-014 | T-027 | verification | The fixture channel's payment-account refusal is the one this task produced |
| T-015 | T-027 | verification | The fixture channel must partition in the readings this task produced |
| T-016 | T-027 | verification | The fixture channel must partition in the budget readings this task produced |
| T-017 | T-027 | verification | The fixture channel must partition in the listing this task produced |
| T-018 | T-027 | verification | The fixture channel must partition in the approval readings this task produced |
| T-014 | T-028 | verification | The verification judges the payee fact this task produced |
| T-015 | T-029 | verification | The verification judges the partitioned readings this task produced |
| T-026 | T-029 | verification | The build-refusal evidence judges the assertions this task extended |
| T-016 | T-030 | verification | The verification judges the budget readings this task produced |
| T-017 | T-031 | verification | The verification judges the listing this task produced |
| T-022 | T-031 | verification | The table-check evidence judges the transition check this task produced |
| T-018 | T-032 | verification | The verification judges the approval readings this task produced |
| T-015 | T-033 | verification | The review covers the per-channel month readings this task produced |
| T-016 | T-033 | verification | The review covers the budget utilisation this task produced |
| T-019 | T-033 | verification | The verification judges the finality rule this task produced |
| T-020 | T-034 | verification | The verification judges the production caller this task produced |
| T-026 | T-034 | verification | The terminal-position evidence judges the assertions this task extended |
| T-021 | T-035 | verification | The verification judges the appending control this task produced |
| T-022 | T-036 | verification | The verification judges the gate path this task produced |
| T-023 | T-037 | verification | The verification judges the stage-row rule this task produced |
| T-024 | T-037 | verification | The verification judges the check this task produced |
| T-025 | T-038 | verification | The verification judges the version declaration this task produced |
| T-013 | T-039 | verification | The confirmation judges whether the attributes made any precondition or control configurable |
| T-014 | T-039 | verification | The confirmation judges whether the payee change weakened the payment-account refusal |
| T-016 | T-039 | verification | The confirmation judges whether any budget amount was recorded in the company's own store |
| T-020 | T-039 | verification | The confirmation judges whether the production caller committed any spend |
| T-022 | T-039 | verification | The confirmation judges whether the gate path weakened owner approval or a precondition |
| T-026 | T-039 | verification | The confirmation judges the boundary assertions this task extended |
| T-027 | T-040 | produces-consumes | The record states what the fixture-channel evidence established |
| T-028 | T-040 | produces-consumes | The record states what the payee evidence established |
| T-029 | T-040 | produces-consumes | The record states what the partition evidence established |
| T-030 | T-040 | produces-consumes | The record states what the budget evidence established and what the company reading covers |
| T-031 | T-040 | produces-consumes | The record states what the listing evidence established |
| T-032 | T-040 | produces-consumes | The record states what the approval-workload evidence established |
| T-033 | T-040 | produces-consumes | The record states what the finality evidence established |
| T-034 | T-040 | produces-consumes | The record states what the production-caller evidence established |
| T-035 | T-040 | produces-consumes | The record states what the concurrency evidence established |
| T-036 | T-040 | produces-consumes | The record states what the gate-path evidence established |
| T-037 | T-040 | produces-consumes | The record states what the low-finding evidence established |
| T-038 | T-040 | produces-consumes | The record states the declared version the evidence confirmed |
| T-039 | T-040 | produces-consumes | The record states what the confirmation evidence established |

### 8.2 External Dependencies

None identified. No task waits on a party outside the plan's authority: the four owner actions that stand undischarged block first publication rather than any task here, and the owner's answers to Q-002 and Q-003 bear on values this change deliberately does not record.

### 8.3 Implementation Order

- Wave 1: T-001, T-002, T-003, T-004, T-005, T-006, T-007, T-008, T-009, T-010, T-011, T-012, T-024
- Wave 2: T-013, T-015, T-016, T-017, T-018, T-019, T-020, T-021, T-022, T-023, T-025
- Wave 3: T-014, T-030, T-031, T-032, T-033, T-035, T-036, T-037, T-038
- Wave 4: T-026, T-027, T-028
- Wave 5: T-029, T-034, T-039
- Wave 6: T-040

## Suggested Workflow

Selected workflow: implement-feature

Selected because the change delivers new capability against an approved scope with measurable acceptance criteria, and the run is already routed through this workflow's phase model.

| Phase | Tasks |
|---|---|
| scope-and-acceptance | none; the phase is complete and its artifact is this plan's upstream input |
| execution-planning | none; this plan is the phase's own output |
| solution-design-and-risk-assessment | T-001, T-002, T-003, T-004, T-005, T-006, T-007, T-008, T-009, T-010, T-011, T-012 |
| implementation | T-013, T-014, T-015, T-016, T-017, T-018, T-019, T-020, T-021, T-022, T-023, T-024, T-025, T-026 |
| quality-review | T-027, T-028, T-029, T-030, T-031, T-032, T-033, T-034, T-035, T-036, T-037, T-038, T-039 |
| documentation-and-release-handoff | T-040 |

| Gate | Required owners |
|---|---|
| Scope Gate | omn-product-owner, omn-business-analyst |
| Planning Gate | omn-tech-lead, omn-orchestrator |
| Design Gate | omn-architect, omn-tech-lead |
| Review Gate | omn-dev-2-reviewer, omn-qa |
| Verification Gate | omn-qa |
| Closure Gate | omn-orchestrator, omn-documentation |

## Required Capabilities

### 10.1 Agent Capabilities

| Capability | Tasks | Owning agent | Proficiency |
|---|---|---|---|
| architecture-analysis | T-001, T-002, T-003, T-004, T-005, T-006, T-007, T-008, T-009, T-010, T-011 | architect | Primary |
| technical-approach-definition | T-001, T-002, T-003, T-004, T-007, T-008, T-009, T-010 | architect | Primary |
| reuse-assessment | T-004, T-008 | architect | Primary |
| structural-risk-analysis | T-002, T-007, T-009, T-010 | architect | Primary |
| release-readiness | T-012 | omn-tech-lead | Primary |
| implementation-delivery | T-013, T-014, T-015, T-016, T-017, T-018, T-019, T-020, T-021, T-022, T-023, T-024, T-025, T-026 | omn-dev-1-implement | Primary |
| quality-verification | T-027, T-028, T-029, T-030, T-031, T-032, T-033, T-034, T-035, T-036, T-037, T-038, T-039 | omn-dev-2-reviewer | Primary |
| code-review | T-028, T-031, T-039 | omn-dev-2-reviewer | Primary |
| documentation | T-040 | omn-documentation | Primary |
| release-communication | T-040 | omn-documentation | Primary |

### 10.2 Required Skills

| Skill | File | Tasks | Level |
|---|---|---|---|
| S01 | skills/architecture/clean-architecture-checklist.md | T-001, T-002, T-003, T-004, T-008, T-026 | Advisory |
| S02 | skills/business/domain-modeling.md | T-001, T-002, T-003, T-004, T-005, T-006, T-010, T-013, T-014 | Primary |
| S03 | skills/dotnet/engineering-playbook.md | T-013, T-014, T-015, T-016, T-017, T-018, T-019, T-020, T-021, T-022, T-023, T-024, T-025, T-026 | Secondary |
| S06 | skills/database/database-engineering.md | T-002, T-004, T-007, T-009, T-014, T-016, T-019, T-021 | Secondary |
| S07 | skills/testing/testing-strategy.md | T-024, T-027, T-028, T-029, T-030, T-031, T-032, T-033, T-034, T-035, T-036, T-037, T-038, T-039 | Secondary |
| S08 | skills/performance/performance-engineering.md | T-009, T-021, T-035 | Advisory |
| S09 | skills/security/secure-engineering.md | T-002, T-005, T-009, T-028, T-031, T-035, T-039 | Advisory |
| S10 | skills/git/git-collaboration.md | T-012, T-025, T-040 | Advisory |
| S11 | skills/logging/observability-logging.md | T-015, T-040 | Advisory |
| S12 | skills/error-handling/error-handling-strategy.md | T-021, T-022 | Advisory |

## Acceptance Criteria

1. A fixture second channel is brought into being in a throwaway store with zero source changes, every per-channel attribute round-trips without touching another channel, every delivered reading partitions for it, and it stays refused by all three conditions and routed to by zero jobs. Verifies business objective 1. Evidence: the round-trip and fixture-channel demonstrations recorded in T-027.
2. The payment-account condition resolves identically for every channel from one company-level record, no per-channel payee value exists, and every risk profile states the shared payee. Verifies business objective 2. Evidence: the before-and-after demonstration and the shape review recorded in T-028.
3. No published reading is company-only, every per-channel quantity is in the closed union, additive company figures equal their channel sums exactly, and an observed-zero channel and an unmeasured channel render differently. Verifies business objective 3. Evidence: the build refusal, the reading-set review and the side-by-side demonstration recorded in T-029.
4. Per-channel and company utilisation read at the four thresholds in their measurement cases, each crossing raises one alert, a channel with no recorded amount reads unmeasured, and the company reading states its coverage. Verifies business objective 4. Evidence: the repeated-evaluation and no-budget demonstrations recorded in T-030.
5. Each channel's approval listing equals its items awaiting owner approval, the listings' union equals the company's set, approval counts and effort components read per channel in their cases, and no threshold or route around approval exists. Verifies business objective 5. Evidence: the demonstrations and reviews recorded in T-031 and T-032.
6. A month reading over the operation record presented as final never changes, the audit chain shows no shared predecessor under concurrent appenders, a production path writes through the recorder at zero metered operations, and stage durations never mix clocks unannounced. Verifies business objective 6. Evidence: the demonstrations recorded in T-033, T-034, T-035 and T-037.
7. A new item reaches awaiting owner approval through awaiting-rights-check on real recorded state, a presentation from Draft is refused, and the dispatch it then reaches is refused by each condition named individually. Verifies business objective 7. Evidence: the audited sequence and refusals recorded in T-036.
8. The repository declares exactly one version string, the built assemblies carry it, and the release record states it without a derived one. Verifies business objective 8. Evidence: the inspections recorded in T-038 and the release record produced by T-040.
9. The five structural absences, the three undischarged preconditions, the owner-approval state, the boundary assertions and the delivered threshold tracking are unchanged or extended, committed spend is zero, and no channel, budget amount or channel configuration value is recorded in the company's own store. Verifies business objective 9. Evidence: the confirmation recorded in T-039.

## Definition of Done

- [ ] All nine plan acceptance criteria are verified with recorded evidence
- [ ] Design Gate, Review Gate, Verification Gate and Closure Gate are approved with owners recorded
- [ ] Every task acceptance criterion is satisfied or formally waived with the waiver recorded
- [ ] Every assumption is confirmed by its named role or converted to a recorded decision
- [ ] Every risk is closed or accepted with its named owner recorded
- [ ] Every open question is closed or explicitly accepted by its named owner
- [ ] Committed spend is recorded as zero and the authorised exception is recorded as unspent
- [ ] The company's own store is recorded as holding no channel, budget amount or channel configuration value written by this change
- [ ] The release record and the release-impact notes are published
- [ ] Durable outcomes are recorded to memory per `memory/memory-governance.md`

## Open Questions

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| Q-001 | The USD 77.41 ceiling includes a USD 42.99 standing monthly charge, and the operation record carries metered per-operation cost only. Does the company-level utilisation track recorded operation cost alone against the ceiling and state that standing commitments are outside it, or must standing commitments be recorded against the ceiling? Recorded as non-blocking because the plan builds the first reading with its coverage stated; the second adds a task. | no | architect | T-004, T-016, T-030 |
| Q-002 | What monthly budget amount does the owner record for channel one, and must the amounts recorded for all channels together stay within the ceiling or only be tracked against it? Recorded as non-blocking because no channel budget amount is recorded by this change and a channel without one reads unmeasured. | no | omn-product-owner | T-004, T-016 |
| Q-003 | Which audience, brand, voice and other per-channel values does the owner record for channel one? Recorded as non-blocking because no channel configuration value is recorded in the company's own store by this change. | no | omn-product-owner | T-001, T-013 |
| Q-004 | How do the payment-account observations already recorded per channel become the single company-level fact without losing their history? | no | architect | T-002, T-014, T-028 |
| Q-005 | Which version string does the repository declare? | no | omn-tech-lead | T-012, T-025, T-038 |
| Q-006 | The scope lists library registration among the per-channel configured attributes, while the library-registration precondition must never be satisfiable from configuration. Is the attribute the recorded per-channel registration register, as this plan assumes, rather than a configuration value? | no | architect | T-001, T-013, T-039 |

## Traceability Matrix

| Statement | Covered by |
|---|---|
| S-001 | T-001, T-013, T-027, A-005, Q-003, Q-006 |
| S-002 | T-002, T-014, T-028, A-002, A-011, Q-004 |
| S-003 | T-003, T-015, T-029, A-008 |
| S-004 | T-004, T-016, T-030, A-001, A-006, Q-001, Q-002 |
| S-005 | T-005, T-017, T-031 |
| S-006 | T-006, T-018, T-032 |
| S-007 | T-007, T-019, T-033, A-007 |
| S-008 | T-008, T-020, T-034, A-010 |
| S-009 | T-009, T-021, T-035 |
| S-010 | T-010, T-022, T-036 |
| S-011 | T-011, T-023, T-024, T-037 |
| S-012 | T-012, T-025, T-038, T-040, A-004, Q-005 |
| S-013 | T-001, T-005, T-010, T-022, T-027, T-036, T-039, T-040, A-009 |
| S-014 | T-001, T-004, T-013, T-016, T-039, T-040, A-001 |
| S-015 | T-003, T-015, T-026, T-029, T-039 |
| S-016 | T-004, T-016, T-026, T-039 |
| S-017 | T-003, T-006, T-018, T-032, T-039, T-040 |

Statement register, normalised from the supplied feature request and the upstream scope definition of this run, with S-001 to S-012 matching the upstream in-scope items in order and the remaining five carrying the binding constraints: S-001 per-channel attributes and a channel by configuration alone; S-002 one company-level payee; S-003 every published reading partitioned by channel; S-004 budget utilisation per channel and against the company ceiling; S-005 per-channel approval listing over gate state; S-006 per-channel approval counts and effort; S-007 finality over the operation record; S-008 the recorder's production caller; S-009 the audit-chain fork closed; S-010 Draft to owner approval through the rights check; S-011 the two carried low findings; S-012 one declared version; S-013 nothing publishes, no channel created or live, absences and preconditions unchanged; S-014 zero spend, ceiling unchanged, no channel amount or configuration value recorded; S-015 the union, the membership assertion, the table checks and the horizon extended and never weakened; S-016 completed waves built upon and deterministic work incapable of a model call; S-017 no invented quantity and the subject re-pointable. Upstream acceptance criteria are cited by ordinal in the order the scope definition lists them, because their identifiers share this plan's assumption prefix.
