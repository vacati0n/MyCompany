```yaml
plan:
  planId: PLAN-2026-0005
  sourceInputs:
    - type: feature-request
      reference: tasks/MC-6/input.md
  producedBy: planner
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  inputDigest: sha256:ef19a2ff431ab6553dccf4f5e12e3083
  contextDigest: sha256:62e7676afa2df10b3af32629f294e88a
```

## Executive Summary

This plan decomposes the sustained-rate capability and its instrumentation into twenty-three tasks across six execution waves, for the CEO and sole owner who decides whether the company can sustain its committed publishing rate and who alone can discharge what currently blocks publication. Success is that the reasoning tier a route states reaches every operation the composed production path records, that throughput, queue, record-source and approval-effort readings each come from the company's own records and resolve to observed, observed zero or unmeasured, that one entry point carries an item through the publishing sequence to composition and no further, and that the rate closes testable rather than tested. Six architect decision tasks precede ten implementation tasks, six verification tasks and one documentation task; the first wave holds the six decision tasks, which have no unmet dependency. The highest-impact risk is that a count read from a seeded demonstration store is later read as an observation of the company's own rate, so the orchestrator's reading at the Scope Gate that sizing waits for a production series is carried as an assumption with a matching risk and a routed question, and every demonstration depth is labelled a demonstration parameter. A second structural fact bears on the publishing entry point: under the undischarged preconditions every dispatch on the company's real recorded state is refused at gate evaluation, so composition is demonstrable only over a throwaway store, and that reading is also registered as an assumption and a question. Plan status is complete: no open question blocks decomposition, and each of the four questions is recorded against the narrower outcome this plan builds.

## Business Objectives

1. The owner can tell, for each operation the production path records, which reasoning tier the admitting route stated it serves, so the split the per-item cost and the monthly envelope rest on becomes testable by a production series. Received by the CEO and sole owner. Measured by every operation recorded over a route that states a tier carrying that tier as served, and by the absence marker appearing only where the route states none. Traces to S-001, S-008.
2. The owner can read how much production work the company held, claimed, retried, escalated and completed over a period, and can tell a period observed to hold none from a period never measured. Received by the owner and by the operator roles who plan against those figures. Measured by every such quantity resolving to exactly one of observed, observed zero or unmeasured. Traces to S-002, S-012.
3. The owner can read the item dossier, the production-path stage outcomes, the supply audits and the compliance determinations from what the company itself recorded. Received by the owner and by the later change that first publishes. Measured by four of four sources answering from recorded state. Traces to S-003, S-013.
4. An item can be carried through the whole publishing sequence by one entry point while nothing publishes. Received by the owner, who approves every publication individually. Measured by the entry point stopping at the sequence's terminal position with every refusal named against the condition that refused it. Traces to S-004, S-006, S-007.
5. The owner can read the effort one approval took as three components, each reporting what was recorded whatever happened to the others. Received by the owner, who carries the cost of approval. Measured by a missing mark making only its own component unmeasured. Traces to S-005.
6. No decision is taken against a number the company never observed. Received by the owner, who decides whether to sustain the rate. Measured by no sizing figure, required depth, target rate, clip count or approval threshold appearing anywhere, and by every demonstration depth carrying its demonstration-parameter label. Traces to S-009, S-015.
7. The company's refusal behaviour and spend position stay unweakened. Received by the owner, who alone can discharge what currently refuses and authorise spend. Measured by the structural absences, the coded preconditions, the owner-approval state, the boundary assertions and the committed spend being unchanged after the change. Traces to S-006, S-007, S-008, S-010, S-011, S-014.

## Technical Objectives

1. A route's stated reasoning tier survives the round trip through the record store in both shapes, stated and not stated, with neither folding into the other and with no derivation from the requested tier reachable anywhere. Verified by round-trip demonstration against a live record store. Traces to business objective 1.
2. Every throughput and queue quantity is an instance of the closed three-case measurement union, and a quantity introduced outside it fails the solution build naming the offending type. Verified by the build-time boundary assertions together with a side-by-side demonstration of an observed zero and an unmeasured reading. Traces to business objective 2.
3. The four record sources are reachable from the composed system through read ports the composition root wires, with no composer answerable only from records handed to it. Verified by demonstration through the composed system against a live record store. Traces to business objective 3.
4. The publishing workflow definition keeps composition as its last declared position, and the production entry point adds no position after it and no component the transport assertion forbids. Verified by the boundary assertions over the built output. Traces to business objective 4 and business objective 7.
5. The approval-effort reading derives each component from its own recorded marks, with no component derived from another and no combined figure. Verified by demonstration over approval measurements with each mark missing in turn. Traces to business objective 5.
6. Every type this change adds sits inside the scope of the build-time boundary assertions, and deterministic work stays incapable of a model call. Verified by a solution build that runs the assertions and fails on a seeded violation. Traces to business objective 7.
7. No delivered output, grouping or label carries a fixed-subject assumption, a sizing threshold or a figure derived from a demonstration parameter. Verified by review of every delivered output and statement. Traces to business objective 6.

## Scope

### In Scope

- S-001: An operation recorded by the fully composed production path names the reasoning tier the admitting route stated it serves, shows the absence marker only where that route states none, and never substitutes the requested tier. Delivered by T-001, T-002, T-007 and T-008, verified by T-017.
- S-002: How much production work the company held, claimed, retried, escalated and completed over a period is readable from its own records, each quantity stating whether it was observed, observed as zero, or never measured. Delivered by T-003 and T-009, verified by T-018.
- S-003: The item dossier, the production-path stage outcomes, the supply audits and the compliance determinations are answerable from the running system's own recorded state rather than only from records handed to it. Delivered by T-004, T-010, T-011, T-012 and T-013, verified by T-019.
- S-004: One production entry point carries an item through the whole publishing sequence to the position the declared sequence ends at, and no further, with every refusal recorded against the condition that refused it. Delivered by T-005 and T-014, verified by T-020.
- S-005: The effort one owner approval took is readable as three components whose measurement states are independent. Delivered by T-006 and T-015, verified by T-021.
- S-006: Nothing publishes; the five structural absences that leave the upload path unable to fire are unchanged and none becomes a configuration value. Delivered by T-005 and T-014, verified by T-020 and T-022.
- S-007: The three coded preconditions stay encoded and undischarged, each refusing from recorded state, with an unknown state folding to not-satisfied while staying visible in the refusal. Delivered by T-014, verified by T-020 and T-022.
- S-008: The monthly envelope stays at USD 77.41, the authorised single-metered-operation exception stays unspent, no task depends on spend, and every figure resting on the assumed reasoning-tier split is labelled an assumption. Delivered by T-008, verified by T-017 and T-022, recorded by T-023.
- S-009: No clip count is invented, and no clean-record threshold is derived from the single existing approval measurement. Delivered by T-012 and T-015, verified by T-019 and T-021.
- S-010: Owner approval stays a state in the gate transition table and absent from configuration, and nothing becomes a route around it. Delivered by T-005 and T-014, verified by T-022.
- S-011: Deterministic work stays structurally incapable of a model call, proved by the boundary assertions that run on every solution build and fail it, with the two recorded limits of those assertions carried as stated. Delivered by T-016, verified by T-022.
- S-012: No quantity is added outside the closed three-case measurement union. Delivered by T-003, T-009 and T-016, verified by T-018.
- S-013: The five completed waves are built upon and none is re-created. Delivered by T-001 and T-004, verified by T-022.
- S-014: The subject stays a recorded experiment and every delivered output stays re-pointable. Delivered by T-009 and T-012, verified by T-022.
- S-015: No required buffer depth, concurrency figure or sustainable-rate claim is recorded as a threshold, and the rate closes testable rather than tested. Delivered by T-003 and T-009, verified by T-018, recorded by T-023.

### Out of Scope

- Proof of the sustained three-items-per-week rate, and any produced, published or audience-observed series offered as that proof. Excluded because the proof needs produced and published items, which need spend and owner action that are not discharged.
- Publication of any kind, including upload, channel creation, account creation, purchase, authenticated subscription session and render. Excluded because the no-publish constraint is carried forward unchanged.
- Discharging any of the three coded preconditions, or making any of them satisfiable from configuration. Excluded because each refuses from recorded state and only the owner can discharge it.
- Any clip count for the audited subjects, and any supply figure standing in for one. Excluded because zero clip counts were obtained across the fourteen audited subjects and none may be invented.
- A clean-record threshold, target or pass line for approval effort. Excluded because the single existing measurement is a lower bound of the weakest class and the threshold is the owner's to set.
- A measured value for the assumed reasoning-tier split, and any figure presented as settling it. Excluded because settling the split needs a production series, which needs produced items and therefore spend.
- The known issues carried open from the previous two release records other than the five this change's path touches. Excluded because each stays with the owner its release record named.
- Any spend against the monthly envelope, a second stock library, and any drawing of the authorised exception. Excluded because nothing this change delivers requires a metered operation.
- An authoritative version string for the repository. Excluded because no supplied input establishes one and the question is routed to the technical lead.

### Deferred

- A sizing claim of any kind, including a required buffer depth, a concurrency figure and a sustainable-rate assertion. Brought into scope when a production series exists with an observed cycle time, failure rate and rework rate.
- Accepting a demonstration depth as provisional evidence of the rate. Brought into scope if the owner records that decision at the wave report, under Q-001.
- Closing the two recorded limits of the boundary assertions, that a reference declared and never used still passes and that a single-project build does not start the target. Brought into scope when either is raised as a request of its own.

## Assumptions

| ID | Assumption | Basis | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | Any sizing claim waits until a production series exists; a queue demonstration is a capability demonstration only; its depth, and every count read from a seeded demonstration store, is a demonstration parameter labelled an assumption; and the rate closes testable rather than tested. This is the orchestrator's reading at the Scope Gate, restated in the dispatch briefing because a gate rationale reaches no downstream phase, and the owner may overturn it | S-015 | A task recording a demonstration depth as provisional evidence is added, and the criteria of T-003, T-009, T-018 and T-023 change | omn-product-owner, for the requesting owner at the wave report |
| A-002 | Verification tasks are owned by omn-dev-2-reviewer because the role the decomposition rule names owns no phase in the routed workflow, so a task owned by it would never be dispatched | plan-wide | Ownership of six verification tasks moves and the phase mapping changes, or the verification is never dispatched | omn-orchestrator |
| A-003 | The resolution boundary writes the served tier from the admitted route on every outcome path, including the non-provider paths, so the stated-tier demonstration needs no provider route, credential or spend | S-001 | The demonstration needs a provider route, becomes contingent on spend, and blocks on an owner decision | architect |
| A-004 | The queue's claim records and stage records already hold what the held, claimed, retried, escalated and completed counts need, and a quantity those records cannot state reads unmeasured rather than requiring a new record | S-002 | A record-creation task is inserted ahead of T-009 and the change widens into a completed wave | architect |
| A-005 | The four record sources can be given a home that the existing production path writes to, without re-creating any step of that path | S-003 | T-010, T-011, T-012 and T-013 widen into the production wave and further build tasks are added | architect |
| A-006 | The approval record keeps each of its marks individually, so each effort component is derivable from its own marks alone | S-005 | A record-change task is inserted ahead of T-015 | architect |
| A-007 | On the company's real recorded state every dispatch is refused at gate evaluation, so the entry point reaches composition only over a throwaway demonstration store in which the three conditions are recorded satisfied with evidence and an owner verdict is recorded through the delivered gate service, and that store discharges nothing the owner holds | S-004 | The terminal-position criterion is demonstrable only in its refusal form, and the criteria of T-014 and T-020 narrow | omn-product-owner |
| A-008 | Every demonstration runs against a throwaway record store that it drops and recreates, so no row a demonstration writes enters the company's own records | S-013 | A demonstration row lands in an append-only record that refuses correction, and restore-to-point becomes the only reversal | omn-tech-lead |

## Risks

| ID | Class | Trigger | Impact | Likelihood | Affects | Mitigation | Owner |
|---|---|---|---|---|---|---|---|
| R-001 | requirement | The owner overturns the Scope Gate reading in A-001 at the wave report | A task recording a demonstration depth as provisional evidence is added and four tasks' criteria change | low | T-003, T-009, T-018, T-023 | Q-001 routes the reading to the owner, and T-023 records it as an interpretation rather than as an owner decision | omn-product-owner |
| R-002 | requirement | A count read from a seeded demonstration store is later read as an observation of the company's own work or rate | A sizing figure with nothing behind it enters a decision about sustaining the rate | medium | T-018, T-023 | T-018 requires every demonstration depth labelled a demonstration parameter, and T-023 records that no delivered figure establishes the rate | omn-documentation |
| R-003 | requirement | The queue records cannot state a count the throughput reading needs, as A-004 assumes they can | A record-creation task is inserted and the change widens into a completed wave | low | T-003, T-009 | T-003 names the record each quantity is read from before T-009 builds to it, and a quantity with no record reads unmeasured rather than prompting a new record | architect |
| R-004 | requirement | A home for the four record sources needs a writer the existing production path does not have, contrary to A-005 | The four source tasks widen into the production wave | medium | T-004, T-010, T-011, T-012, T-013 | T-004 records the writer for each source, and any source that needs a new writer is raised to the owner rather than built silently | architect |
| R-005 | requirement | The owner does not accept a throwaway demonstration store as reaching the terminal position, contrary to A-007 | The entry point's terminal-position criterion closes in refusal form only | medium | T-014, T-020 | Q-004 routes the question, and T-014 is required to end at the named refusal on real recorded state in either branch | omn-product-owner |
| R-006 | requirement | A served-tier output presents a ratio other than the one decided, or over a period holding fewer than two served-tier records | A reader takes a ratio the design never intended as the measured split | medium | T-008, T-017 | T-002 decides the ratio under Q-002 before T-008 presents it, and T-017 checks the fewer-than-two rule | architect |
| R-007 | technical | A route stating no tier is read back as stating a default tier, or a stated tier is read back as none | Every served tier on the composed path misattributes, as the observed-zero round-trip defect did in the previous change | medium | T-007, T-017 | T-001 fixes that absence means the route cannot state one, and T-017 round-trips both shapes through a live store | architect |
| R-008 | technical | A throughput or queue quantity is added with a value slot in its unmeasured case or outside the closed union | An unmeasured period renders as a number and the discipline fails silently | high | T-009, T-016, T-018 | T-003 names each quantity's case before build, and T-016 extends the boundary assertions over every type this change adds | architect |
| R-009 | technical | The entry point acquires a position after composition, a type name the transport assertion forbids, or a path around owner approval | An absence the whole no-publish property rests on is breached | low | T-005, T-014, T-020, T-022 | T-005 names the condition before build, T-016 keeps the assertions over the new types, and T-020 and T-022 verify the absences | architect |
| R-010 | technical | Making the approval components independent changes the approval measurement that other readers of the approval series depend on, or the marks are not kept individually as A-006 assumes | An existing approval reading changes meaning, or a record-change task is inserted | medium | T-006, T-015, T-021 | T-006 decides where the independence is realised and names every reader of the measurement it touches | architect |
| R-011 | technical | The resolution boundary does not write the served tier on a non-provider path, contrary to A-003 | The stated-tier demonstration becomes contingent on spend | low | T-007, T-017 | T-001 confirms the outcome paths that write the served tier before T-007 builds, and any spend stays with the owner | architect |
| R-012 | security | A newly recorded source carries a credential, a secret or customer-identifying content into a record the analytics surface reads | Restricted content leaves the company's records through a reporting surface | low | T-010, T-011, T-012, T-013, T-019 | T-019 checks every recorded field of the four sources for restricted content | omn-dev-2-reviewer |
| R-013 | operational | A verification task is assigned to a role that owns no phase in the routed workflow | The verification never runs and the change closes with unverified criteria | medium | plan-wide | A-002 assigns verification to the role that owns the quality-review phase, and the Suggested Workflow mapping is the check | omn-orchestrator |
| R-014 | operational | A demonstration is pointed at a store holding the company's own records rather than a throwaway one | A demonstration row enters an append-only record that refuses correction | low | T-017, T-018, T-019, T-020, T-021 | A-008 binds every demonstration to a throwaway store, and each verification task records the store it ran against | omn-tech-lead |
| R-015 | delivery | A new type is checked only by a single-project build, or is referenced by a declaration that is never used | The boundary assertions do not start, or pass a reference they cannot see, and a violation goes unrefused | medium | T-016, T-022 | T-022 takes its evidence from a solution build, and both recorded limits are carried as stated rather than claimed closed | omn-tech-lead |
| R-016 | delivery | The closing phase blocks at capability resolution, as the routed workflow's resolution rules still record | T-023 does not run and the testable-not-tested boundary is not recorded | low | T-023 | The previous change closed all six phases of this workflow, so the block is watched for at dispatch rather than presumed | omn-orchestrator |

## Task Breakdown

### T-001 Decide how a route records the reasoning tier it states

- Owner: architect
- Complexity: S (confidence: low)
- Depends on: none
- Traces to: S-001, S-013, A-003
- Status: assumption-dependent
- Description: A recorded design states how a route records the reasoning tier it states it serves, how the composed registry carries it to the resolution boundary, and what an existing route that states none reads as.
- Acceptance Criteria:
  - The design states that a route recording no tier means the route cannot state one, and that this reads as the absence marker rather than as any tier.
  - The design states how the stated tier reaches the admitted route the resolution boundary reads, with no derivation from the requested tier reachable anywhere.
  - The design names every outcome path at the resolution boundary that writes the served tier, and confirms or corrects that the non-provider paths write it.
  - The design states what every route already recorded reads as after the change, so no existing route acquires a tier it never stated.
- Gate: Design Gate

### T-002 Decide which tier ratio a served-tier output presents

- Owner: architect
- Complexity: XS (confidence: medium)
- Depends on: none
- Traces to: S-001, S-008
- Status: ready
- Description: A recorded decision names the ratio a served-tier output presents where more than one is computable from the recorded pair, answering Q-002.
- Acceptance Criteria:
  - The decision names one ratio and states it in terms of the recorded requested and served values alone.
  - The decision states that the ratio needs no assumed split as an input.
  - The decision states that no ratio is presented over a period holding fewer than two served-tier records.
  - The decision states whether the ratio the previous implementation defined is kept or replaced.
- Gate: Design Gate

### T-003 Define the throughput and queue quantities read for a period

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: none
- Traces to: S-002, S-012, S-015, A-001, A-004
- Status: assumption-dependent
- Description: A recorded design names each throughput and queue quantity the change reads for a period, the record it is read from, and the case of the measurement union each resolves to, with no sizing figure among them.
- Acceptance Criteria:
  - The design names each quantity for held, claimed, retried, escalated and completed work, and the recorded source each is read from, answering Q-003 on what counts as held.
  - The design states, for each quantity, the condition under which it reads observed, observed zero or unmeasured, and the detail an unmeasured reading names.
  - The design states that any quantity needing a cycle time, a failure rate or a rework rate reads unmeasured while no such observation exists.
  - The design states no required buffer depth, concurrency figure, target rate or pass line, and labels any demonstration depth it names a demonstration parameter resting on A-001.
- Gate: Design Gate

### T-004 Decide where the four record sources are recorded and read

- Owner: architect
- Complexity: L (confidence: low)
- Depends on: none
- Traces to: S-003, S-013, A-005
- Status: assumption-dependent
- Description: A recorded design states, for each of the item dossier, the production-path stage outcomes, the supply audits and the compliance determinations, where it is recorded, which step writes it, and how the composed system reads it.
- Acceptance Criteria:
  - The design names, for each of the four sources, its recorded home, its writer and its read port.
  - The design states, for each source, what a read resolves to when the source holds nothing, naming what was looked for and where.
  - The design states how each source's round trip preserves the measurement case it was written in, so no case collapses into another on the way through the store.
  - The design records that no step of a completed wave is re-created, or names the step that would be and raises it to the owner.
- Gate: Design Gate

### T-005 Decide the entry point that drives the publishing sequence to composition

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: none
- Traces to: S-004, S-006, S-007, S-010, A-007
- Status: assumption-dependent
- Description: A recorded design names the single production entry point that carries an item through every declared position of the publishing sequence, and states the condition that keeps composition terminal.
- Acceptance Criteria:
  - The design names the entry point and states that it adds no position to the publishing workflow definition.
  - The design states what the entry point records at each refusal, naming the precondition that refused and the recorded state it refused from, including an unknown state.
  - The design states the recorded state under which the unit reaches composition, and that on the company's real recorded state every dispatch is refused, answering Q-004.
  - The design states that the entry point grants, records, satisfies and shortcuts no owner approval, and reads the three preconditions from recorded state only.
- Gate: Design Gate

### T-006 Decide how approval effort resolves each component independently

- Owner: architect
- Complexity: S (confidence: low)
- Depends on: none
- Traces to: S-005, S-009, A-006
- Status: assumption-dependent
- Description: A recorded decision states where each approval-effort component's measurement state is derived from its own recorded marks, and names every reader of the approval measurement the decision touches.
- Acceptance Criteria:
  - The decision states which marks each of the review, queue and rework components is derived from.
  - The decision states that a missing mark makes only the components derived from it unmeasured.
  - The decision names every existing reader of the approval measurement and states whether its reading changes.
  - The decision states no threshold, target or pass line against any component.
- Gate: Design Gate

### T-007 Record each route's stated tier through the composed registry

- Owner: omn-dev-1-implement
- Complexity: S (confidence: low)
- Depends on: T-001
- Traces to: S-001, A-003
- Status: assumption-dependent
- Description: A route can be recorded stating a reasoning tier or stating none, and the composed registry constructs every admitted route with the tier it recorded.
- Acceptance Criteria:
  - A route recorded stating a tier is constructed by the composed registry stating that same tier.
  - A route recorded stating none is constructed stating none, and never stating a default tier.
  - Every operation the composed path records over a route stating a tier carries that tier as served and the request's tier as requested.
  - No path from the requested tier to the served tier exists in the delivered change.
- Gate: none

### T-008 Present the decided tier ratio on the served-tier outputs

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-002
- Traces to: S-001, S-008
- Status: ready
- Description: Every served-tier output presents only the ratio T-002 decided, together with the statement of what its record set does and does not establish.
- Acceptance Criteria:
  - The tier ratio output presents the ratio T-002 decided and states its definition where it is presented.
  - A period holding fewer than two served-tier records presents no ratio.
  - Every output carrying a served tier states what the recorded set does and does not establish, and every figure resting on the assumed split is labelled an assumption.
  - The statement cannot be suppressed by a configuration value or a display option.
- Gate: none

### T-009 Deliver throughput and queue quantities per period from recorded work

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-003
- Traces to: S-002, S-012, S-014, S-015, A-001, A-004
- Status: assumption-dependent
- Description: Held, claimed, retried, escalated and completed work for a period is readable from the company's own records, each quantity in exactly one case of the measurement union as T-003 defines it.
- Acceptance Criteria:
  - Each quantity T-003 names is readable for a period from the record T-003 names for it.
  - A period whose count was taken and found empty reads as an observed zero, and a period with no observation reads as unmeasured naming what was looked for and where.
  - Any quantity that would need an unobserved cycle time, failure rate or rework rate reads unmeasured.
  - No delivered quantity states a required buffer depth, a target rate or a pass line, and no grouping or label assumes the current subject is permanent.
- Gate: none

### T-010 Make the item dossier answerable from recorded state

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-004
- Traces to: S-003, A-005
- Status: assumption-dependent
- Description: The item dossier is recorded where T-004 decides and is read by the composed system through its read port, rather than only from a dossier handed to a composer.
- Acceptance Criteria:
  - A dossier written through the composed system is read back through its read port.
  - A read over a store holding no dossier resolves to unmeasured naming what was looked for and where, with no placeholder in its place.
  - A dossier read back resolves to the measurement case it was written in, for each of the three cases.
  - A dossier recorded for one item version answers for that version only and never for another version of the same item.
- Gate: none

### T-011 Make production-path stage outcomes answerable from recorded state

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-004, T-010
- Traces to: S-003, S-007, A-005
- Status: assumption-dependent
- Description: Every production-path stage outcome, including every refusal, is recorded where T-004 decides and is read by the composed system through its read port.
- Acceptance Criteria:
  - Every recorded stage outcome for an item is read back through the composed system, including every refusal.
  - Every refusal read back names the precondition that refused and the recorded state it refused from, including an unknown state.
  - A stage with nothing recorded reads unmeasured and is never inferred from a later stage's outcome.
  - A stage outcome read back resolves to the measurement case it was written in, for each of the three cases.
- Gate: none

### T-012 Make supply audits answerable from recorded state

- Owner: omn-dev-1-implement
- Complexity: S (confidence: low)
- Depends on: T-004
- Traces to: S-003, S-009, S-014, A-005
- Status: assumption-dependent
- Description: Supply-audit results are recorded where T-004 decides and are read by the composed system through their read port, one entry per audited subject.
- Acceptance Criteria:
  - An audit result written through the composed system is read back through its read port, one entry per audited subject.
  - A subject whose clip count was never established reads as unestablished and carries no number in its place.
  - An audit result read back resolves to the measurement case it was written in, for each of the three cases.
  - No grouping or label in the supply reading assumes the current subject is permanent.
- Gate: none

### T-013 Make compliance determinations answerable from recorded state

- Owner: omn-dev-1-implement
- Complexity: S (confidence: low)
- Depends on: T-004
- Traces to: S-003, A-005
- Status: assumption-dependent
- Description: Compliance determination outcomes are recorded where T-004 decides and are read by the composed system through their read port, one entry per determination.
- Acceptance Criteria:
  - A determination written through the composed system is read back through its read port as its own entry.
  - A determination with no recorded outcome reads unmeasured rather than as a negative outcome.
  - A determination read back resolves to the measurement case it was written in, for each of the three cases.
  - No determination outcome is inferred from another determination's outcome.
- Gate: none

### T-014 Deliver the entry point that drives an item to composition

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-005
- Traces to: S-004, S-006, S-007, S-010, A-007
- Status: assumption-dependent
- Description: One production entry point advances an item through every declared position of the publishing sequence as T-005 designs it, stopping at composition where recorded state admits it and at the named refusal where it does not.
- Acceptance Criteria:
  - Over recorded state admitting it, the entry point advances an item through every declared position to composition and leaves the unit in a terminal claim state.
  - Over the company's real recorded state, the entry point stops at the refusal and records the precondition that refused, with an unknown state folding to not-satisfied while staying visible.
  - The entry point adds no position to the publishing workflow definition and declares no component the transport assertion forbids.
  - The entry point reads each precondition and the owner-approval state from recorded state, and no configuration value reaches either.
- Gate: none

### T-015 Deliver approval effort with independent component states

- Owner: omn-dev-1-implement
- Complexity: S (confidence: low)
- Depends on: T-006
- Traces to: S-005, S-009, A-006
- Status: assumption-dependent
- Description: The approval-effort reading reports review, queue and rework each in the case its own recorded marks support, as T-006 decides.
- Acceptance Criteria:
  - With one mark missing, the components not derived from it report what was recorded and only the dependent component reads unmeasured.
  - Each unmeasured component names which mark was missing.
  - No component is derived from another, and no combined figure is reported.
  - No threshold, target or pass line appears on any component or anywhere the reading travels.
- Gate: none

### T-016 Extend the boundary assertions over every type this change adds

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-007, T-008, T-009, T-010, T-011, T-012, T-013, T-014, T-015
- Traces to: S-011, S-012
- Status: ready
- Description: Every type this change adds falls inside the build-time boundary assertions that run on every solution build and fail it, covering the measurement union, the no-model path and the structural absences.
- Acceptance Criteria:
  - Every analytics type this change adds sits in a scanned namespace or on the named carrier list, and a bare numeric or undeclared string member added to one fails the solution build naming the offending type.
  - Every deterministic type this change adds is covered by the no-model-path assertion, and a model-dependent reference introduced into one fails the solution build.
  - The assertions still hold composition as the last position of every workflow definition the build declares.
  - The two recorded limits of the assertions are stated unchanged rather than claimed closed.
- Gate: none

### T-017 Verify the composed path records the route's stated tier as served

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-007, T-008
- Traces to: S-001, S-008, A-003, A-008
- Status: assumption-dependent
- Description: Recorded evidence shows every operation the composed path records over a route stating a tier carrying that tier as served, both route shapes surviving the round trip, and the ratio rule holding.
- Acceptance Criteria:
  - Evidence from a live throwaway store shows requested and served values read back out of the store differing where the route differs from the request, with zero served values copied from the request.
  - Evidence shows a route stating a tier and a route stating none each read back in the shape it was written.
  - Evidence shows no ratio presented over a period holding fewer than two served-tier records, and every served-tier output stating what its record set does and does not establish.
  - Evidence shows the demonstration committed no spend and drew nothing on the authorised exception.
- Gate: Review Gate, Verification Gate

### T-018 Verify throughput quantities resolve to three distinguishable cases

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-009, T-016
- Traces to: S-002, S-012, S-015, A-001, A-008
- Status: assumption-dependent
- Description: Recorded evidence shows every throughput and queue quantity resolving to exactly one case of the measurement union, an observed zero and an unmeasured reading rendering apart, and no sizing figure published.
- Acceptance Criteria:
  - Evidence from a solution build shows a throughput quantity introduced outside the union failing the build and the check naming the offending type.
  - Evidence from a live throwaway store shows an observed-zero period and an unmeasured period read side by side and rendering differently.
  - Evidence shows every depth and count seeded for the demonstration labelled a demonstration parameter resting on A-001, with no acceptance threshold or rate claim derived from it.
  - Evidence shows no published quantity stating a required buffer depth, a target rate or a pass line.
- Gate: Review Gate, Verification Gate

### T-019 Verify the four record sources answer from recorded state

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-010, T-011, T-012, T-013
- Traces to: S-003, S-009, A-005, A-008
- Status: assumption-dependent
- Description: Recorded evidence shows each of the four record sources answering through the composed system from recorded state, four of four, in each measurement case.
- Acceptance Criteria:
  - Evidence from a live throwaway store shows each of the four sources read through the composed system, four of four, with none answerable only from a record handed to it.
  - Evidence from an empty store shows each source reading unmeasured and naming what was looked for and where, with no count, zero or placeholder in its place.
  - Evidence shows a record written to each source resolving on read-back to the case it was written in, for each of the three cases.
  - Evidence shows no clip count and no restricted content in any field read back from the four sources.
- Gate: Review Gate, Verification Gate

### T-020 Verify the entry point ends at composition with every refusal named

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-014, T-016
- Traces to: S-004, S-006, S-007, A-007, A-008
- Status: assumption-dependent
- Description: Recorded evidence shows the entry point reaching composition and no further where recorded state admits it, and refusing by the named condition where it does not.
- Acceptance Criteria:
  - Evidence from a live throwaway store shows an item advanced through every declared position to composition, and the unit left in a terminal claim state with no position declared after it.
  - Evidence shows each of the three preconditions unset in turn producing a refusal that names it individually, and one precondition recorded unknown folding to not-satisfied while staying visible.
  - Evidence from a solution build shows the boundary assertions passing over the entry point and holding composition terminal.
  - Evidence states which recorded state each run used, so a composition reached over demonstration state is not read as a discharged precondition.
- Gate: Review Gate, Verification Gate

### T-021 Verify approval components report their states independently

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-015
- Traces to: S-005, S-009, A-008
- Status: assumption-dependent
- Description: Recorded evidence shows each approval-effort component reporting its own case whatever happened to the others, with no threshold anywhere.
- Acceptance Criteria:
  - Evidence from each of the three marks missing in turn shows the other components reporting what was recorded and only the dependent component unmeasured.
  - Evidence shows each unmeasured component naming the missing mark.
  - Evidence shows no component derived from another and no combined figure reported.
  - Evidence shows no threshold, target or pass line on any component or in any statement published with the reading.
- Gate: Review Gate, Verification Gate

### T-022 Confirm every carried-forward refusal and boundary holds after the change

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: high)
- Depends on: T-014, T-016
- Traces to: S-006, S-007, S-008, S-010, S-011, S-013, S-014
- Status: ready
- Description: Recorded evidence shows the five structural absences, the three undischarged preconditions, the owner-approval state, the spend position and the boundary assertions unchanged after the delivered change.
- Acceptance Criteria:
  - Evidence shows each of the five structural absences still structural and none of the five expressible as a configuration value in the delivered configuration set.
  - Evidence shows each of the three coded preconditions still refusing from recorded state and none satisfiable from configuration.
  - Evidence shows owner approval still a state in the gate transition table, absent from configuration, and reachable through no operation the change adds.
  - Evidence from a solution build shows the boundary assertions running and failing the build on a seeded violation, with the two recorded limits stated as limits.
  - Evidence shows committed spend of zero against the unchanged monthly envelope, no completed wave's capability re-created, and no delivered output assuming the current subject is permanent.
- Gate: Review Gate, Verification Gate

### T-023 Record what the change makes testable and what it leaves untested

- Owner: omn-documentation
- Complexity: S (confidence: high)
- Depends on: T-017, T-018, T-019, T-020, T-021, T-022
- Traces to: S-001, S-002, S-008, S-015, A-001
- Status: assumption-dependent
- Description: The release record states that the rate closes testable and not tested, what each delivered reading measures and reports unmeasured, and which figures rest on an assumption.
- Acceptance Criteria:
  - The record states that nothing in the change establishes that the system can sustain three items a week, and that the rate is testable rather than tested.
  - The record labels every demonstration depth and seeded count a demonstration parameter, and records the Scope Gate reading behind it as an interpretation the owner may overturn.
  - The record states that stated tiers now reach the composed path and that the assumed reasoning-tier split remains untested.
  - The record carries forward, unresolved, the known issues this change did not close.
- Gate: Closure Gate

## Dependencies

### 8.1 Dependency Edges

| From | To | Type | Justification |
|---|---|---|---|
| T-001 | T-007 | contract | The route record and the registry bind to the stated-tier meaning the design fixes |
| T-002 | T-008 | decision-gate | Which ratio the served-tier output presents changes with the decision |
| T-003 | T-009 | contract | The delivered quantities bind to the names, sources and cases the design fixes |
| T-004 | T-010 | contract | The dossier's home, writer and read port are the ones the design fixes |
| T-004 | T-011 | contract | The stage outcomes' home, writer and read port are the ones the design fixes |
| T-004 | T-012 | contract | The supply audits' home, writer and read port are the ones the design fixes |
| T-004 | T-013 | contract | The determinations' home, writer and read port are the ones the design fixes |
| T-010 | T-011 | produces-consumes | The delivered stage-outcome composer takes the item dossier as its input, so the recorded dossier must be readable first |
| T-005 | T-014 | contract | The entry point binds to the condition and refusal recording the design fixes |
| T-006 | T-015 | contract | The approval reading binds to the derivation the decision fixes |
| T-007 | T-016 | produces-consumes | The boundary assertions must cover the types the stated-tier task adds |
| T-008 | T-016 | produces-consumes | The boundary assertions must cover the types the ratio task adds |
| T-009 | T-016 | produces-consumes | The boundary assertions must cover the quantity types the throughput task adds |
| T-010 | T-016 | produces-consumes | The boundary assertions must cover the types the dossier task adds |
| T-011 | T-016 | produces-consumes | The boundary assertions must cover the types the stage-outcome task adds |
| T-012 | T-016 | produces-consumes | The boundary assertions must cover the types the supply-audit task adds |
| T-013 | T-016 | produces-consumes | The boundary assertions must cover the types the determination task adds |
| T-014 | T-016 | produces-consumes | The boundary assertions must cover the types the entry-point task adds |
| T-015 | T-016 | produces-consumes | The boundary assertions must cover the types the approval task adds |
| T-007 | T-017 | verification | The verification judges the stated-tier records this task produced |
| T-008 | T-017 | verification | The verification judges the ratio and statement this task produced |
| T-009 | T-018 | verification | The verification judges the throughput quantities this task produced |
| T-016 | T-018 | verification | The build-refusal evidence judges the assertions this task extended |
| T-010 | T-019 | verification | The verification judges the dossier source this task produced |
| T-011 | T-019 | verification | The verification judges the stage-outcome source this task produced |
| T-012 | T-019 | verification | The verification judges the supply-audit source this task produced |
| T-013 | T-019 | verification | The verification judges the determination source this task produced |
| T-014 | T-020 | verification | The verification judges the entry point this task produced |
| T-016 | T-020 | verification | The terminal-position evidence judges the assertions this task extended |
| T-015 | T-021 | verification | The verification judges the approval reading this task produced |
| T-014 | T-022 | verification | The confirmation judges whether the entry point weakened any absence or precondition |
| T-016 | T-022 | verification | The confirmation judges the boundary assertions this task extended |
| T-017 | T-023 | produces-consumes | The record states what the stated-tier evidence established |
| T-018 | T-023 | produces-consumes | The record states what the throughput evidence established and labels its demonstration parameters |
| T-019 | T-023 | produces-consumes | The record states what the record-source evidence established |
| T-020 | T-023 | produces-consumes | The record states what the entry-point evidence established |
| T-021 | T-023 | produces-consumes | The record states what the approval evidence established |
| T-022 | T-023 | produces-consumes | The record states what the confirmation evidence established |

### 8.2 External Dependencies

None identified. No task waits on a party outside the plan's authority: the four owner actions that stand undischarged block first publication rather than any task here, and the owner's answers to Q-001 and Q-004 bear on criteria this plan builds in their narrower form.

### 8.3 Implementation Order

- Wave 1: T-001, T-002, T-003, T-004, T-005, T-006
- Wave 2: T-007, T-008, T-009, T-010, T-012, T-013, T-014, T-015
- Wave 3: T-011, T-017, T-021
- Wave 4: T-016, T-019
- Wave 5: T-018, T-020, T-022
- Wave 6: T-023

## Suggested Workflow

Selected workflow: implement-feature

Selected because the change delivers new capability against an approved scope with measurable acceptance criteria, and the run is already routed through this workflow's phase model.

| Phase | Tasks |
|---|---|
| scope-and-acceptance | none; the phase is complete and its artifact is this plan's upstream input |
| execution-planning | none; this plan is the phase's own output |
| solution-design-and-risk-assessment | T-001, T-002, T-003, T-004, T-005, T-006 |
| implementation | T-007, T-008, T-009, T-010, T-011, T-012, T-013, T-014, T-015, T-016 |
| quality-review | T-017, T-018, T-019, T-020, T-021, T-022 |
| documentation-and-release-handoff | T-023 |

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
| architecture-analysis | T-001, T-002, T-003, T-004, T-005, T-006 | architect | Primary |
| technical-approach-definition | T-001, T-003, T-004, T-005, T-006 | architect | Primary |
| structural-risk-analysis | T-004, T-005 | architect | Primary |
| implementation-delivery | T-007, T-008, T-009, T-010, T-011, T-012, T-013, T-014, T-015, T-016 | omn-dev-1-implement | Primary |
| quality-verification | T-017, T-018, T-019, T-020, T-021, T-022 | omn-dev-2-reviewer | Primary |
| code-review | T-020, T-022 | omn-dev-2-reviewer | Primary |
| documentation | T-023 | omn-documentation | Primary |
| release-communication | T-023 | omn-documentation | Primary |

### 10.2 Required Skills

| Skill | File | Tasks | Level |
|---|---|---|---|
| S01 | skills/architecture/clean-architecture-checklist.md | T-001, T-003, T-004, T-005, T-016 | Advisory |
| S02 | skills/business/domain-modeling.md | T-003, T-004, T-006, T-009, T-015 | Primary |
| S03 | skills/dotnet/engineering-playbook.md | T-007, T-008, T-009, T-010, T-011, T-012, T-013, T-014, T-015, T-016 | Secondary |
| S06 | skills/database/database-engineering.md | T-001, T-004, T-007, T-009, T-010, T-011, T-012, T-013 | Secondary |
| S07 | skills/testing/testing-strategy.md | T-017, T-018, T-019, T-020, T-021, T-022 | Secondary |
| S08 | skills/performance/performance-engineering.md | T-003, T-009, T-018 | Advisory |
| S09 | skills/security/secure-engineering.md | T-019, T-022 | Advisory |
| S11 | skills/logging/observability-logging.md | T-009, T-023 | Advisory |
| S12 | skills/error-handling/error-handling-strategy.md | T-014, T-020 | Advisory |

## Acceptance Criteria

1. Every operation the composed production path records over a route stating a reasoning tier carries that tier as served, the absence marker appears only where the route states none, and zero served values are copied from the request. Verifies business objective 1. Evidence: the round-trip and store read-back recorded in T-017.
2. No served-tier output presents a ratio over a period holding fewer than two served-tier records, and every such output states what its record set does and does not establish. Verifies business objective 1. Evidence: the output review recorded in T-017 and the released record produced by T-023.
3. Every throughput and queue quantity resolves to exactly one of observed, observed zero or unmeasured, an observed zero and an unmeasured reading render differently, and a quantity outside the union fails the solution build naming its type. Verifies business objective 2. Evidence: the side-by-side demonstration and the build refusal recorded in T-018.
4. Each of the four record sources answers from the company's recorded state, four of four, reads unmeasured naming what was looked for when empty, and returns each written case unchanged. Verifies business objective 3. Evidence: the composed-system, empty-store and round-trip demonstrations recorded in T-019.
5. One entry point carries an item to composition and no further, and every refusal names the condition that failed, with an unknown condition folding to not-satisfied while staying visible. Verifies business objective 4. Evidence: the demonstrations and the boundary-assertion outcome recorded in T-020.
6. With any one approval mark missing, only the dependent component reads unmeasured, and it names the missing mark. Verifies business objective 5. Evidence: the mark-by-mark demonstration recorded in T-021.
7. No required buffer depth, concurrency figure, target rate, clip count or approval threshold appears in any delivered output, and every demonstration depth is labelled a demonstration parameter. Verifies business objective 6. Evidence: the labelling review recorded in T-018, the threshold review recorded in T-021, and the released record produced by T-023.
8. The five structural absences, the three undischarged preconditions, the owner-approval state and the boundary assertions are unchanged, and committed spend is zero against the unchanged envelope. Verifies business objective 7. Evidence: the confirmation recorded in T-022.

## Definition of Done

- [ ] All eight plan acceptance criteria are verified with recorded evidence
- [ ] Design Gate, Review Gate, Verification Gate and Closure Gate are approved with owners recorded
- [ ] Every task acceptance criterion is satisfied or formally waived with the waiver recorded
- [ ] Every assumption is confirmed by its named role or converted to a recorded decision
- [ ] Every risk is closed or accepted with its named owner recorded
- [ ] Every open question is closed or explicitly accepted by its named owner
- [ ] Committed spend is recorded as zero and the authorised exception is recorded as unspent
- [ ] The release record and the release-impact notes are published
- [ ] Durable outcomes are recorded to memory per `memory/memory-governance.md`

## Open Questions

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| Q-001 | Does the owner uphold the Scope Gate reading that any sizing claim waits until a production series exists and that a queue demonstration depth is a demonstration parameter only, or does the owner accept a demonstration depth as provisional evidence of the rate? Recorded as non-blocking because this plan builds the restrictive reading; the expansive reading adds a task. | no | omn-product-owner | T-003, T-009, T-018, T-023 |
| Q-002 | Which ratio does a served-tier output present, where more than one is computable from the recorded requested and served pair and no supplied input states the intended one? | no | architect | T-002, T-008, T-017 |
| Q-003 | Which recorded state counts as held work for a period: work waiting to be claimed, a stage recorded as held, or both reported as separate quantities? | no | architect | T-003, T-009 |
| Q-004 | Does a demonstration over a throwaway store, in which the three conditions are recorded satisfied with evidence and an owner verdict is recorded through the delivered gate service, count as reaching the terminal position, given that on the company's real recorded state every dispatch is refused at gate evaluation? Recorded as non-blocking because the entry point is required to end at the named refusal on real state in either branch. | no | omn-product-owner | T-005, T-014, T-020 |

## Traceability Matrix

| Statement | Covered by |
|---|---|
| S-001 | T-001, T-002, T-007, T-008, T-017, T-023, A-003, Q-002 |
| S-002 | T-003, T-009, T-018, T-023, A-004, Q-003 |
| S-003 | T-004, T-010, T-011, T-012, T-013, T-019, A-005 |
| S-004 | T-005, T-014, T-020, A-007, Q-004 |
| S-005 | T-006, T-015, T-021, A-006 |
| S-006 | T-005, T-014, T-020, T-022 |
| S-007 | T-005, T-011, T-014, T-020, T-022 |
| S-008 | T-002, T-008, T-017, T-022, T-023 |
| S-009 | T-006, T-012, T-015, T-019, T-021 |
| S-010 | T-005, T-014, T-022 |
| S-011 | T-016, T-022 |
| S-012 | T-003, T-009, T-016, T-018 |
| S-013 | T-001, T-004, T-022, A-008 |
| S-014 | T-009, T-012, T-022 |
| S-015 | T-003, T-009, T-018, T-023, A-001, Q-001 |

Statement register, as normalised from the supplied feature request and the upstream scope definition of this run, with S-001 to S-005 matching the upstream in-scope items in order: S-001 stated reasoning tier reaches every operation on the composed path; S-002 throughput and queue quantities per period in three cases; S-003 four record sources answerable from recorded state; S-004 one entry point through the publishing sequence to its terminal position; S-005 approval-effort components with independent states; S-006 nothing publishes and no structural absence becomes a configuration value; S-007 the three coded preconditions encoded and undischarged; S-008 no spend and every split-derived figure labelled an assumption; S-009 no invented clip count and no derived approval threshold; S-010 owner approval a gate transition state absent from configuration; S-011 deterministic work incapable of a model call, proved on every solution build; S-012 no quantity outside the closed three-case union; S-013 the five completed waves built upon and none re-created; S-014 the subject a recorded experiment and every output re-pointable; S-015 no sizing threshold and the rate closed testable rather than tested.
