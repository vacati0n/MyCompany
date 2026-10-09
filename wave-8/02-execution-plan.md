```yaml
plan:
  planId: PLAN-2026-0008
  sourceInputs:
    - type: feature-request
      reference: tasks/MC-9/input.md
  producedBy: planner
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  inputDigest: sha256:886055bdd1eb59f9d8a107653d106e9f
  contextDigest: sha256:62e7676afa2df10b3af32629f294e88a
```

## Executive Summary

This plan decomposes the AI-management capability into fifty-two tasks across ten execution waves, for the CEO and sole owner, who must take the weekly review of the company in 10 to 15 minutes from one consistent account of its records without paying for any model call. Success is that the COO, CTO and CFO reports, channel performance and the risk summary, the CEO brief and a read-only dashboard all rest on one read at one datastore instant with the period and its finality on the datastore's clock, that every figure reads as observed, observed zero, unmeasured or a recorded amount, that recommendations are a closed catalogue of labelled rules that abstain over an unmeasured reading, that open owner decisions come from a recorded register, and that the four carried items and the controller decision reader close, all at zero metered calls under the owner's decision of 2026-10-09. Sixteen architect decision tasks lead; eighteen implementation tasks, seventeen verification tasks and one documentation task follow. The highest-impact risk is the clock-and-round-trip defect class this programme has missed until review in three consecutive waves: a report composed from separate delivered reads at different instants, a period stamped on the process clock as the delivered report command does today, or a report read waiting on a hold an admission or a recovery keeps past the 30-second command timeout (R-009, R-010, R-011); a dedicated verification task covers the round trip on every surface. Four facts from the delivered source and the master plan are raised as objections: the delivered finality discipline raises the record horizon, a write, which a read-only dashboard cannot hold without a decision (Q-010); the delivered close-then-read path runs its read at the datastore's default isolation, so several statements in it are not one snapshot for writers outside the horizon discipline (R-009); the programme thresholds and dated changes the channel and risk lines count against are recorded only in the master plan (Q-009); and no recorded threshold yet anchors a COO rule (Q-008). Plan status is complete: every in-scope statement is decomposed, and none of the ten open questions blocks decomposition.

## Business Objectives

1. The owner takes the weekly review from one consistent account of the company's records, readable end to end within 15 minutes. Received by the CEO and sole owner. Measured by every line of every report, the brief and the dashboard carrying one datastore instant, and by the owner's timed reading at the wave report. Traces to S-001, S-010.
2. The owner can tell, for every figure, whether it was observed, observed as zero, never measured or merely recorded, and never mistakes one for another. Received by the owner. Measured by side-by-side renderings of each case on every surface. Traces to S-002.
3. The owner sees the company's operations, technology cost and selection basis, finances, channels and risks in the five weekly reports, including deferred work nobody re-admitted. Received by the owner. Measured by inspection of each rendered report over a fresh and a populated demonstration store. Traces to S-003, S-004, S-005, S-006, S-007, S-014.
4. The owner receives only recommendations that rest on a recorded reading and a recorded threshold or owner decision, and is told where a rule abstained, while model selection follows the owner's ten comparable runs. Received by the owner. Measured by firing and starving each rule over fixtures and by selection at nine and ten runs. Traces to S-008, S-013.
5. The owner sees which of their decisions and questions are open, decided or superseded, from a record rather than from prose, including the unresolved price-capture question where it stays open. Received by the owner. Measured by an entry-by-entry comparison of the register with the decision record and the previous wave's report. Traces to S-009, S-015.
6. The owner can look at the company's position without any action, spend or permission becoming reachable, and the company's refusal behaviour and controls stay unweakened. Received by the owner, who alone may approve, configure, spend or publish. Measured by the boundary assertions on every solution build, zero metered calls and zero spend. Traces to S-011, S-012, S-018, S-019, S-020, S-021.
7. The records the owner relies on stay trustworthy at their edges: an admission past the decision columns' bound ends in a named outcome, and the previous wave's change account no longer misstates its evidence. Received by the owner and any reader of the previous wave's record. Measured by a demonstration past the bound and inspection of the corrected record. Traces to S-016, S-017.

## Technical Objectives

1. The report set is composed from one transaction holding one snapshot at one datastore instant, with the period, its boundaries and its finality taken on the datastore's clock, and the read takes no hold that waits on any other transaction. Verified by a concurrent commit, a skewed process clock and a held admission during composition. Traces to business objective 1.
2. Every figure on every surface is a member of the delivered three-case union or of the recorded-amount type, and the four render distinctly. Verified by the build-time membership assertion and side-by-side rendering. Traces to business objective 2.
3. Each report line's case is decided by the delivered composer for the reading it rests on, fed from the one read, with no second site deciding a case. Verified by review comparing each line with its composer. Traces to business objective 3 and to A-008.
4. The recommendation catalogue is closed, enumerated and pure over the one read, each rule naming its reading, case and recorded anchor, and yielding identical output on identical input. Verified by repeated evaluation and review of the catalogue. Traces to business objective 4.
5. Selection applies the owner's recorded ten comparable runs per task as the evidence threshold and states the count. Verified by fixtures at nine and ten runs. Traces to business objective 4.
6. The register is an append-only record with supersession, read inside the one read and incapable of changing any threshold, budget, configuration value, approval or control. Verified by an in-place edit refused and before-and-after reads. Traces to business objective 5.
7. The dashboard renders the same composed read, holds no write path, no capability reference, no network listener and no secret, and every report path belongs to the deterministic set. Verified by the boundary assertions on every solution build. Traces to business objective 6 and to A-009.
8. The boundary assertions and the membership assertion cover every type this change adds, and none is weakened. Verified by a solution build failing on a seeded violation. Traces to business objective 6.
9. An admission past the decision columns' bound yields a named outcome, and the previous wave's change account carries a findable correction. Verified by demonstration and inspection. Traces to business objective 7.

## Scope

### In Scope

- S-001: Every line of the report set and of the brief from one read at one datastore instant, period and boundaries on the datastore's clock, finality stated. Designed by T-001 and T-002, delivered by T-017, verified by T-035.
- S-002: Every figure in exactly one of observed, observed zero, unmeasured naming what was looked for, or recorded amount naming where recorded, an observed zero never rendering like an unmeasured reading. Designed by T-003, delivered by T-018, verified by T-036.
- S-003: The COO weekly report's production, quality, operations, channel and finance lines, send-backs with reasons, queue and failure counts and held work. Designed by T-004, delivered by T-019, verified by T-037.
- S-004: The CTO weekly report's cost and quality per task, the controller's recorded actions, the selection basis with the count against the owner's ten, the qualitative-review label below it, and the re-verification status before 2027-02-01. Designed by T-005, T-006 and T-007, delivered by T-020, T-021 and T-022, verified by T-038.
- S-005: The CFO summary against the recorded USD 34.42 metered allotment and USD 77.41 envelope, the USD 42.99 standing charge separately, revenue-derived figures not yet earnable. Designed by T-008, delivered by T-023, verified by T-039.
- S-006: Channel performance, one line per channel the store holds, audience, cost, risk state and progress to the programme threshold. Designed by T-009, delivered by T-024, verified by T-040.
- S-007: The risk summary's copyright, policy, monetisation, technology and dated-change lines and each undischarged coded precondition. Designed by T-010, delivered by T-025, verified by T-041.
- S-008: A closed catalogue of fixed, labelled recommendation rules that abstain over an unmeasured reading. Designed by T-011, delivered by T-026, verified by T-042.
- S-009: A recorded register of open owner decisions and questions, listed in the report from the same read. Designed by T-012, delivered by T-027, verified by T-043.
- S-010: The CEO brief in its seven sections and order, each line tied to the decision it informs. Designed by T-013, delivered by T-028, verified by T-044.
- S-011: A read-only dashboard over the same read, offering no approving, sending back, configuring, recording or publishing action. Designed by T-014, delivered by T-029, verified by T-045.
- S-012: No model call and no production permission on any report, rule, register or dashboard path. Delivered by T-030, verified by T-046.
- S-013: Selection keeps the labelled configured ordering below ten comparable runs of every candidate and ranks on evidence at ten or more, stating the count. Designed by T-005, delivered by T-031, verified by T-047.
- S-014: Every deferred metered request not re-admitted visible in the COO report with reason and hold age, its re-admission an open register entry. Designed by T-004, delivered by T-019 and T-027, verified by T-037.
- S-015: The price-capture versus re-fetch conflict either settled by a recorded ruling delivered behaviour matches, or open in the register naming its owners. Prepared by T-015, delivered by T-032, verified by T-048.
- S-016: An admission decision past the decision reading columns' bound yields a named outcome. Designed by T-016, delivered by T-033, verified by T-049.
- S-017: A recorded correction to the previous wave's stale two-session evidence row. Delivered by T-034, verified by T-050.
- S-018: Zero model calls, zero metered calls, zero spend, the single-metered-operation exception unspent, the USD 77.41 envelope unchanged. Verified by T-046 and T-051, recorded by T-052.
- S-019: Nothing publishes; the five structural absences, the three undischarged coded preconditions, owner approval as a transition-table state, the company-level payee and the absence of a second channel all stay as delivered. Verified by T-051.
- S-020: The three-case union, the recorded-amount type, the record horizon, the delivered readings and the boundary assertions extended and never weakened; the eight completed waves built upon and none re-created. Delivered by T-017 and T-030, verified by T-051.
- S-021: No invented quantity; demonstrations only in self-dropping stores in the separate demonstration database; the company's own store untouched; the subject niche-agnostic. Verified by T-051, recorded by T-052.

### Out of Scope

- Any executive model call, recommendation-narrative pass, metered call, or drawing of the single-metered-operation exception. Excluded by the owner's decision of 2026-10-09 against an executive model call until the first video exists.
- Any production, throughput, quality, approval-minutes, revenue or per-item cost figure presented as measured, any trend, burn-rate projection or forecast, and any fixture value presented as the company's. Excluded because none exists and the request forbids simulating them.
- Any rule keyed on an unrecorded threshold, including a clean-record threshold, an approval-workload figure, a sizing figure or any observation threshold other than the owner's ten. Excluded because the request forbids inventing them.
- Any dashboard or report action that approves, sends back, configures, records, publishes or reaches a capability, and recording an owner decision through the dashboard. Excluded because the dashboard is read-only and owner approval stays a transition-table state.
- A network-reachable dashboard, a new web stack, remote or multi-user access, sign-in or user accounts. Excluded because no input asks for remote access and new external exposure needs a threat review this change has no cause to open.
- Unattended weekly scheduling and delivery by email, chat or any external channel. Excluded because reports are produced on request for any week.
- The COO, CTO or CFO as registered agents with any permission, and the CTO report's business-strategy and technology-opportunity sections. Excluded because the executives are deterministic reports and judgement sections wait on the narrative pass.
- Answering any open owner question, and automatic re-admission of deferred metered requests. Excluded because each answer is the owner's; the register makes them visible instead.
- Publication, upload, account creation, purchase, channel creation, configuring or routing to a channel, and discharging a coded precondition. Excluded by the no-publish constraint and the owner's single-entity decision of 2026-10-08.
- Rewriting the configured route register or its ratings. Excluded because selection reads the register and the corpus is unpopulated.
- The previous release record's other known issues beyond the four carried items and the controller decision reader. Excluded by the Scope Gate's ruling on the carried set.

### Deferred

- The recommendation-narrative pass and the CTO judgement sections. Brought into scope when the first video exists and the owner records a decision authorising the pass.
- The register, the re-verification record and the brief installed in the company's own store. Brought into scope when the owner authorises the first install of the company store with the eighth wave's resources (A-005).
- A COO rule keyed on a quantity no record holds today. Brought into scope when the owner records the threshold it would key on (Q-008).
- Re-admission of deferred metered requests. Brought into scope when the owner and omn-tech-lead record a re-admission rule.

## Assumptions

| ID | Assumption | Basis | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | The Scope Gate approved the scope with these rulings, which reached this phase only through the dispatch briefing: the narrative question is settled; the carried set is the four named items plus a reader for the controller's recorded decisions; the one read is one transaction at one datastore instant, its period from the datastore's clock, never waiting on an admission hold; the executives are deterministic reports; the owner's ten governs both the CTO label and the router; the dashboard form goes to the architect with no network surface | S-001 | The one-read design, the carried tasks and the dashboard tasks change | omn-business-analyst |
| A-002 | The owner's decisions of 2026-10-09 bind every phase: no executive, narrative or metered call is planned, designed or made; ten comparable runs per task is a recorded amount; the USD 34.42 metered allotment is the decided company ceiling | S-018 | A metered task, an owner-gated external dependency and a relabelling reversal enter the plan | omn-product-owner |
| A-003 | Verification tasks are owned by omn-dev-2-reviewer because omn-qa owns no phase in the routed workflow | plan-wide | Seventeen verification tasks are never dispatched, or their ownership and phase mapping move | omn-orchestrator |
| A-004 | The price-capture ruling routed to omn-tech-lead with the owner is prepared as an architect task and ruled at the Design Gate, where omn-tech-lead is a listed owner, because omn-tech-lead owns no phase | S-015 | T-015 is not dispatched and T-032 has no ruling to apply | omn-orchestrator |
| A-005 | Every demonstration and test runs in a self-dropping store in the separate demonstration database and the company's own store stays at zero tables, so the register and the re-verification record exist only in demonstration stores in this wave and the company's own brief cannot list open decisions until the owner authorises an install | S-021 | A fixture or a register load enters a kept store, or the company store is installed without the owner's authority | omn-product-owner |
| A-006 | The week boundary and time zone are the architect's proposal, ruled by omn-tech-lead at the Design Gate and reported to the owner at the wave report, and the proposal applies until the owner says otherwise | S-001 | The period definition and every period-bounded line change after build | omn-tech-lead |
| A-007 | No re-verification cadence is invented: the master plan's monthly re-verification cadence until 2027-02-01 marks a statement overdue only if the owner confirms it as the cadence, and until then the overdue part of the line reads unmeasured naming the missing cadence | S-004 | The overdue designation keys on an unconfirmed cadence, or the upstream criterion on overdue statements stays unmet | omn-product-owner |
| A-008 | Each line's case is decided by the delivered composer for the reading it rests on, fed with data taken in the one read; the delivered readings' own separate transactions are not reused for the report set | S-002 | Lines decide cases afresh, or the report set is assembled from reads at different instants | architect |
| A-009 | The dashboard renders the composed report of one read and takes no read of its own | S-011 | Tiles and lines can disagree in instant or case | architect |
| A-010 | The register's entries are loaded once from the decision record and the previous wave's report as recorded entries, and nothing is parsed from prose at report time | S-009 | The decisions-required section depends on prose, contrary to the scope | architect |
| A-011 | The recorded anchors available to rules are the four utilisation thresholds in code, the owner's ten, the decided ceiling, the envelope, the programme thresholds and dated changes once recorded, and open register entries with their interim rulings | S-008 | A report has no anchored rule, or a rule invents a threshold | architect |

## Risks

| ID | Class | Trigger | Impact | Likelihood | Affects | Mitigation | Owner |
|---|---|---|---|---|---|---|---|
| R-001 | requirement | A Scope Gate ruling restated in A-001 is overturned | The one-read design, the carried set or the dashboard boundary changes | low | plan-wide | Any overturned ruling returns the plan to this phase rather than being absorbed | omn-business-analyst |
| R-002 | requirement | The owner authorises an executive or metered call during the wave, contrary to A-002 | A metered task and an external dependency enter the plan | low | plan-wide | The plan returns to execution planning on any such decision | omn-orchestrator |
| R-003 | requirement | No recorded threshold or owner decision anchors a COO rule (Q-008, A-011) | The twenty-eighth upstream criterion is unmet, or a rule invents a threshold | medium | T-011, T-026, T-042 | T-011 lists the anchor of every rule and raises any missing one to the owner instead of inventing it | architect |
| R-004 | requirement | The programme thresholds and dated changes are not accepted as recorded (Q-009) | The progress and dated-change lines read not recorded | medium | T-009, T-010, T-024, T-025 | T-009 and T-010 state where each figure is recorded and the line's case when it is not | architect |
| R-005 | requirement | The owner does not confirm a re-verification cadence (Q-004, A-007) | The overdue part of the re-verification line reads unmeasured | medium | T-007, T-021, T-022, T-038 | T-007 states the line's reading with and without a confirmed cadence | omn-orchestrator |
| R-006 | requirement | The company store stays uninstalled through the wave (A-005) | The owner sees open decisions only over a demonstration store; the goal is met in demonstration only | high | T-012, T-027, T-043, T-052 | T-012 states how the register reaches the company store once an install is authorised, and T-052 records the gap | omn-product-owner |
| R-007 | requirement | The comparable-run unit differs between the report's count and selection's (Q-003) | The report labels a task qualitative while the router ranks it on evidence | medium | T-005, T-022, T-031 | T-005 defines one unit for both and both builds bind to it | architect |
| R-008 | requirement | A register entry is mis-transcribed from the decision record or the previous wave's report (A-010) | The register omits an open question or misstates an interim ruling | medium | T-012, T-027, T-043 | T-043 compares every entry with the two records | architect |
| R-009 | technical | Report lines are composed from separate delivered reads, or from several statements at the datastore's default isolation, which the delivered close-then-read path uses, so writers outside the horizon discipline land between statements | Lines show different instants, the defect class of the previous three waves | high | T-001, T-017, T-029, T-035 | T-001 states one snapshot across every source; T-035 commits concurrently during composition | architect |
| R-010 | technical | The period, its boundaries, finality or a hold age is taken from the process clock, as the delivered report command takes its period today | The period moves with a skewed process clock | high | T-002, T-017, T-019, T-035 | T-002 fixes every instant on the datastore's clock; T-035 skews the process clock a day each way | architect |
| R-011 | technical | The one read waits on a hold kept by an admission across a provider call, by a recovery under its 120-second command timeout, by a server-side transaction whose client was lost, or on the audit chain head | The report ends in an unnamed timeout under the 30-second command timeout | high | T-001, T-017, T-035 | T-001 states that no statement of the read waits on any hold of any length; T-035 holds an admission across a stand-in call | architect |
| R-012 | technical | The one read closes the month by raising the record horizon, a write, while the dashboard must hold no write path (Q-010) | The read-only boundary is breached, or finality is unstated | medium | T-001, T-014, T-029, T-045 | T-001 decides between closing and reading the horizon as stored, and T-014 states the dashboard's write position | architect |
| R-013 | technical | A single-snapshot read meets a concurrent closure of the horizon and fails on a serialisation conflict | The report fails with an unnamed datastore error | medium | T-001, T-017, T-035 | T-001 names the outcome of a conflict; T-035 provokes one | architect |
| R-014 | technical | An observed zero and an unmeasured reading render alike, or a recorded amount renders as observed | The owner reads a never-measured quantity as zero or a configured amount as a measurement | medium | T-003, T-018, T-036 | T-003 fixes one rendering per case; T-036 renders both side by side on every surface | architect |
| R-015 | technical | A line decides a case afresh rather than through its delivered composer, contrary to A-008 | Two sites decide one case and disagree | medium | T-001, T-004, T-006, T-008, T-017, T-036 | Each line design names its composer; T-036 compares every line with it | architect |
| R-016 | technical | Applying the owner's ten changes delivered selection, which applies no observation minimum today | Delivered selection checks fail and must be re-pointed | medium | T-005, T-031, T-047 | T-005 names every delivered check the threshold reaches | architect |
| R-017 | technical | Widening the decision reading columns or naming the outcome alters delivered rows or needs a further resource applied mid-month | Delivered history changes, or metered work is refused until month end | low | T-016, T-033, T-049 | T-016 states what existing rows read as and the resource route | architect |
| R-018 | technical | A report week spans a month end and a CFO line reads a booking month the week does not equal (A-006) | Spend is shown against the wrong month's ceiling | medium | T-002, T-008, T-023 | T-002 states which booking month each CFO line reads | architect |
| R-019 | security | A report, rule or dashboard path gains an action, a write path, a capability reference or a network listener | The read-only boundary is breached | low | T-014, T-029, T-030, T-045 | T-030 extends the boundary assertions; T-045 reviews the surface | architect |
| R-020 | security | Recording a register entry or issuing a recommendation changes a threshold, budget, configuration value, approval or control | A record grants what only the owner grants | low | T-012, T-027, T-043 | T-012 keeps the register informational; T-043 reads each before and after | architect |
| R-021 | security | The dashboard or the brief renders a credential, a handle or a secret | Disclosure on the owner's screen | low | T-014, T-029, T-045 | T-014 states what the surface may show; T-045 reviews it | architect |
| R-022 | operational | A verification task is assigned to a role that owns no phase, contrary to A-003 | Verification never runs | medium | plan-wide | A-003 assigns verification to the quality-review owner | omn-orchestrator |
| R-023 | operational | The price-capture ruling is not obtained because omn-tech-lead owns no phase, contrary to A-004 | T-032 has neither a ruling nor an open entry | low | T-015, T-032 | The orchestrator obtains the ruling at the Design Gate | omn-orchestrator |
| R-024 | operational | A demonstration or the live suite is pointed at a store other than the separate demonstration database, contrary to A-005 | The named store's schema is dropped or a fixture enters a kept store | low | T-035, T-036, T-037, T-038, T-039, T-040, T-041, T-042, T-043, T-044, T-045, T-046, T-047, T-048, T-049 | Each verification records its target store and the company store's table count afterwards | omn-orchestrator |
| R-025 | delivery | A new type is checked only by a single-project build or escapes the boundary assertions | The assertions do not see the type | medium | T-030, T-046 | T-046 takes its evidence from a solution build failing on a seeded violation | omn-dev-2-reviewer |
| R-026 | delivery | Seventeen scope items exceed one implementation pass | Could-have items slip and the change closes partial | medium | plan-wide | Each task keeps its scope priority, and the carried items are independent of the report builds in the graph | omn-orchestrator |
| R-027 | delivery | The owner's timed reading of the brief is not obtained before closure | The thirty-sixth upstream criterion stays unverified | medium | T-044, T-052 | T-052 records the criterion as awaiting the owner's reading at the wave report | omn-orchestrator |
| R-028 | delivery | The closing phase blocks at capability resolution, as the routed workflow's rules still record | The release record is not produced | low | T-052 | Seven previous changes closed this phase; the block is watched for at dispatch | omn-orchestrator |

## Task Breakdown

### T-001 Decide how the report set is read at one datastore instant

- Owner: architect
- Complexity: L (confidence: low)
- Depends on: none
- Traces to: S-001, S-020, A-001, A-008
- Status: assumption-dependent
- Description: A recorded design states how every source the report set rests on is read in one transaction holding one snapshot, what single datastore value is the report's instant, how finality is decided without waiting, and that no statement of the read waits on any hold, answering Q-010.
- Acceptance Criteria:
  - The design lists every source the reports, the brief and the dashboard read, including the register, the re-verification record and the recorded controller decisions, and states that all are read in one snapshot.
  - The design states the isolation of the read and why several statements in it see one state for writers outside the record horizon's discipline, given that the delivered close-then-read path reads at the default isolation.
  - The design names the datastore value stamped on every line as the instant, and shows it is the snapshot's own.
  - The design states whether the read raises the record horizon or reads it as stored, how finality follows either way, and what that means for a dashboard holding no write path.
  - The design states that no statement of the read waits on the horizon, a scope hold, the audit chain head or any row hold of any duration, and names the outcome of a serialisation conflict.
  - The design states, for each delivered reading, which composer decides its cases over data taken in the one read.
- Gate: Design Gate

### T-002 Decide the report period, its boundaries and its finality

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: none
- Traces to: S-001, A-006
- Status: assumption-dependent
- Description: A recorded design proposes the week boundary and time zone, states how a requested or current week is resolved on the datastore's clock, and states which booking month each month-bound line reads when a week spans a month end, answering Q-001 for ruling at the Design Gate.
- Acceptance Criteria:
  - The design states the week's first instant, its time zone and the half-open boundaries, on the datastore's clock.
  - The design states how the current week is resolved without the process clock, and how a past week is requested.
  - The design states which booking month every CFO and budget line reads for a week spanning a month end, and labels it on the line.
  - The design states when a week is final and what a non-final period's statement reads.
- Gate: Design Gate

### T-003 Decide how the four measurement cases render on every surface

- Owner: architect
- Complexity: S (confidence: medium)
- Depends on: none
- Traces to: S-002, S-020
- Status: ready
- Description: A recorded design states one rendering for each of observed, observed zero, unmeasured and recorded amount, used identically by the reports, the brief and the dashboard.
- Acceptance Criteria:
  - The design states each case's rendering, and that an unmeasured rendering carries no amount, unit or zero.
  - The design states that an observed zero renders as a measurement and never like an unmeasured reading.
  - The design states that a recorded amount names where it is recorded and never renders as observed.
  - The design states that no new measurement case is added and the delivered union and recorded-amount type are reused.
- Gate: Design Gate

### T-004 Decide the COO report's lines and the readings they rest on

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: none
- Traces to: S-003, S-014, A-008
- Status: assumption-dependent
- Description: A recorded design lists every production, quality, operations, channel and finance line of the COO report with the delivered reading and composer it rests on, including send-back reasons and deferred metered requests with reason and hold age.
- Acceptance Criteria:
  - The design lists every line under its section and names the reading and composer each rests on.
  - The design states that planned items read not recorded while no production plan is recorded, and that the committed rate is never presented as planned, produced or published.
  - The design states each rate's denominator and that the rate reads unmeasured where the denominator is unmeasured or no item reached the step.
  - The design states how each deferred metered request not re-admitted is listed with its reason, escalation state and hold age on the datastore's clock.
- Gate: Design Gate

### T-005 Decide the comparable-run unit and how selection applies the owner's ten

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: none
- Traces to: S-004, S-013, A-002
- Status: assumption-dependent
- Description: A recorded design states what one comparable run of one task is, how runs are counted per candidate, and how selection keeps the labelled configured ordering below ten and ranks on evidence at ten or more, answering Q-003.
- Acceptance Criteria:
  - The design defines one comparable run in terms of the recorded benchmark observation's facts, and states the one count used by both the report and selection.
  - The design states that the ten is the owner's recorded amount, rendered as recorded, never observed.
  - The design states that the configured ordering applies wherever any candidate has fewer than ten comparable runs, and that every selection states the count.
  - The design names every delivered selection check the threshold reaches, given that delivered selection applies no observation minimum.
- Gate: Design Gate

### T-006 Decide the CTO report's lines, recorded controller decisions included

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: T-005, T-007
- Traces to: S-004, A-008
- Status: assumption-dependent
- Description: A recorded design lists every CTO line with its reading and composer, including cost and quality per task, the reader of the controller's recorded decisions, the selection basis as the router recorded it and the qualitative-review label.
- Acceptance Criteria:
  - The design lists each per-task line with cost and quality in their cases and the count of comparable runs against the recorded ten.
  - The design states the reader of the controller's recorded decisions, listing reading, threshold, action, booking month and datastore instant for every decision booked in the period.
  - The design states that the selection basis is shown exactly as recorded and that a configured rating is never shown as observed quality.
  - The design states the re-verification line's place and its reading for periods either side of 2027-02-01 on the datastore's date.
- Gate: Design Gate

### T-007 Decide the platform-policy re-verification record

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: none
- Traces to: S-004, A-007
- Status: assumption-dependent
- Description: A recorded design states the record that holds platform-policy statements and their re-verification results, who writes it, how re-checked, changed and overdue statements are read from it, and what the line reads while nothing is recorded, answering the record part of Q-004.
- Acceptance Criteria:
  - The design states the record's facts per policy statement, its writer, and that it reuses the master plan's platform-policy entity where that holds.
  - The design states that the line reads unmeasured naming the missing record wherever no result is recorded.
  - The design states the overdue rule with and without an owner-confirmed cadence, inventing none.
  - The design states that recording a result changes no control in this wave.
- Gate: Design Gate

### T-008 Decide the CFO summary's lines on the decided company ceiling

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: none
- Traces to: S-005, A-002, A-008
- Status: assumption-dependent
- Description: A recorded design lists every CFO line, company and per-channel spend against the recorded allotment and envelope, the standing charge separately, revenue-derived figures not yet earnable, and every delivered output whose company-basis label changes from interim to the owner's decided ceiling.
- Acceptance Criteria:
  - The design lists each line with its reading and composer, and states that the allotment, envelope and standing charge render as recorded amounts never netted.
  - The design names every delivered output describing the allotment as interim, the cost controller's basis statement among them, and its new label.
  - The design states that a channel with no recorded budget amount reads its utilisation unmeasured naming the missing amount, never zero or exceeded.
  - The design states that revenue, profit and every revenue-derived figure read not yet earnable or unmeasured, never a zero amount.
- Gate: Design Gate

### T-009 Decide the channel performance report's lines

- Owner: architect
- Complexity: S (confidence: low)
- Depends on: none
- Traces to: S-006, A-008
- Status: assumption-dependent
- Description: A recorded design states one line per channel the store holds with audience, cost, risk state and progress to the programme threshold, where the programme thresholds and 2027-02-01 are recorded, and how days remaining are counted on the datastore's date, answering Q-009 for this report.
- Acceptance Criteria:
  - The design states that the report lists exactly the channels the store holds and creates, configures or routes to none.
  - The design states that audience figures read unmeasured while no source records them.
  - The design names where each programme threshold and the 2027-02-01 date are recorded, and the line's case if they are not.
  - The design states that days to 2027-02-01 are counted on the datastore's date.
- Gate: Design Gate

### T-010 Decide the risk summary's lines

- Owner: architect
- Complexity: S (confidence: low)
- Depends on: none
- Traces to: S-007, A-008
- Status: assumption-dependent
- Description: A recorded design lists the copyright, policy, monetisation, technology and dated-change lines with their recorded sources, each coded precondition's recorded state, and the next dated change counted on the datastore's date, answering Q-009 for this report.
- Acceptance Criteria:
  - The design names the recorded source of each line, and states that a line no source records reads unmeasured naming the absent source.
  - The design states that each of the three coded preconditions appears with its recorded state and none appears discharged in the company's own state.
  - The design names where each dated change is recorded and how the next is chosen and counted on the datastore's date.
  - The design states that no risk line asserts an incident, claim or strike count no record holds.
- Gate: Design Gate

### T-011 Decide the closed recommendation rule catalogue

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: T-004, T-006, T-008
- Traces to: S-008, A-011
- Status: assumption-dependent
- Description: A recorded design enumerates every rule, the reading it rests on, the recorded threshold or owner decision it keys on, its output fields, its abstention and its order, with at least one rule for each of the COO, CTO and CFO reports, answering Q-008 or raising it to the owner.
- Acceptance Criteria:
  - The design enumerates the closed catalogue and names each rule's reading, case requirement and recorded anchor, with zero rules keyed on an unrecorded threshold.
  - The design states that a rule whose reading is unmeasured issues nothing and states that it abstained, naming the reading.
  - The design states the deterministic evaluation order, so that two evaluations over one read yield identical output.
  - The design states each CTO recommendation's ten fields, and that a field it cannot state reads unmeasured or labelled an estimate.
  - The design names the anchor of each COO, CTO and CFO rule, or records the missing anchor as an owner question.
- Gate: Design Gate

### T-012 Decide where the open-decisions register is held and loaded from

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: none
- Traces to: S-009, S-014, A-005, A-010
- Status: assumption-dependent
- Description: A recorded design states the register's record and entry facts, its supersession rule, its place inside the one read, how it is loaded from the decision record and the previous wave's report, and how it reaches the company store once an install is authorised, answering Q-002.
- Acceptance Criteria:
  - The design states each entry's identifier, statement, kind, status, owner, recorded date, interim ruling and decider, and that every entry reads back identical.
  - The design states that a decided entry is never edited or reopened and that a change is a new entry naming the one it supersedes, binding every writer.
  - The design states that recording an entry changes no threshold, budget, configuration value, approval or control.
  - The design states the initial entries, every open owner question of the two records with its owner and interim ruling, the three owner decisions of 2026-10-09 as decided and the re-admission question open.
  - The design states how the company store would receive the register once the owner authorises an install, and that this wave installs nothing there.
- Gate: Design Gate

### T-013 Decide the brief's composition

- Owner: architect
- Complexity: S (confidence: medium)
- Depends on: T-012
- Traces to: S-010
- Status: ready
- Description: A recorded design states the brief's seven sections in order, the report line each brief line takes, the decision each informs, and that the decisions-required section lists exactly the open register entries from the same read.
- Acceptance Criteria:
  - The design lists the seven sections in the order company, channel performance, production, risk, CTO recommendations, COO recommendations, decisions required.
  - The design names the decision each brief line informs, and removes any line that informs none.
  - The design states that the decisions-required section is the open register entries of the one read and nothing from prose.
  - The design states that each brief line is the report line itself, not a recomputation.
- Gate: Design Gate

### T-014 Decide the read-only dashboard's form

- Owner: architect
- Complexity: S (confidence: low)
- Depends on: none
- Traces to: S-011, A-009
- Status: assumption-dependent
- Description: A recorded design states whether the dashboard is operator console text or a rendered file the owner opens, how it is reached from the operator entry point, and its read-only boundary, answering Q-006.
- Acceptance Criteria:
  - The design states the form and the entry-point command that produces it, with no network listener and no new web stack.
  - The design states that every tile is a report line of the same read, with the same instant and case.
  - The design states that the surface offers no action that approves, sends back, configures, records, publishes or reaches a capability, and holds no write path.
  - The design states that the awaiting-approval figure is the per-channel approval view over gate state, and that no credential, handle or secret is shown.
- Gate: Design Gate

### T-015 Prepare the price-capture ruling for the Design Gate

- Owner: architect
- Complexity: S (confidence: low)
- Depends on: none
- Traces to: S-015, A-004
- Status: assumption-dependent
- Description: A recorded design states the delivered price path, what capturing a unit price at admission and re-fetching it before use each mean for a booking, and the two routes the ruling can take, for omn-tech-lead to rule on with the owner at the Design Gate, answering Q-005 or recording it open.
- Acceptance Criteria:
  - The design states where the delivered path captures prices and where the booking applies them.
  - The design states, for each route, what delivered behaviour must match and what changes.
  - The design states the register entry that stands if no ruling is taken, naming its owners.
  - The design states that no record states the re-fetch rule honoured while the question is unresolved.
- Gate: Design Gate

### T-016 Decide the named outcome past the decision reading columns' bound

- Owner: architect
- Complexity: S (confidence: medium)
- Depends on: none
- Traces to: S-016
- Status: ready
- Description: A recorded design states the outcome an admission decision receives when its utilisation exceeds the decision reading columns' bound, and whether the columns change.
- Acceptance Criteria:
  - The design states whether such a decision is recorded or refused, and the named outcome the caller receives.
  - The design states that no unnamed datastore error reaches the caller.
  - The design states what existing rows read as, and the schema resource route.
  - The design states that the controller's mapping and thresholds are unchanged.
- Gate: Design Gate

### T-017 Deliver the one read of the company's records

- Owner: omn-dev-1-implement
- Complexity: L (confidence: low)
- Depends on: T-001, T-002
- Traces to: S-001, S-020, A-001, A-008
- Status: assumption-dependent
- Description: The report set reads every source in one transaction at one datastore instant, with the period, its boundaries and finality on the datastore's clock, as T-001 and T-002 design it, never waiting on any hold.
- Acceptance Criteria:
  - Every line composed from the read carries the same datastore instant.
  - With the process clock a day ahead or behind, the period, boundaries and instants are unchanged.
  - A read taken while another session holds the record horizon shared completes within the command timeout under its stated finality.
  - A serialisation conflict, if it occurs, yields the named outcome T-001 states.
  - The delivered report command no longer takes its period from the process clock.
- Gate: none

### T-018 Deliver the four-case rendering

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-003
- Traces to: S-002, S-020
- Status: ready
- Description: The reports, the brief and the dashboard render each of the four cases as T-003 designs it.
- Acceptance Criteria:
  - Each case renders as T-003 states, on every surface.
  - An unmeasured rendering carries no amount, unit or zero.
  - A recorded amount names where it is recorded.
  - No new measurement case or bare numeric member is added.
- Gate: none

### T-019 Deliver the COO report

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-004, T-017, T-018
- Traces to: S-003, S-014, A-008
- Status: assumption-dependent
- Description: The COO report carries the lines T-004 lists, each from the one read and naming the reading it rests on, including deferred metered requests not re-admitted.
- Acceptance Criteria:
  - Each section carries the lines T-004 lists, each naming its reading.
  - Planned items read not recorded while no production plan is recorded.
  - Each rate reads unmeasured where its denominator is unmeasured, and every recorded send-back reason is listed.
  - Each deferred request not re-admitted appears with reason, escalation state and hold age on the datastore's clock, and none is re-admitted.
- Gate: none

### T-020 Deliver the reading of recorded controller decisions

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-006, T-017
- Traces to: S-004
- Status: ready
- Description: The cost controller's recorded decisions are readable inside the one read, as T-006 designs the reader.
- Acceptance Criteria:
  - Every decision booked in the period reads back with its reading, threshold, action, booking month and datastore instant identical to what was recorded.
  - The reader has read members only.
  - A period with no decision reads as T-006 states, never as an observed zero of decisions it did not look for.
  - The reading is taken inside the one read.
- Gate: none

### T-021 Deliver the re-verification record

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-007, T-017
- Traces to: S-004, A-007
- Status: assumption-dependent
- Description: The platform-policy re-verification record exists as T-007 designs it, written and read through adapters and read inside the one read.
- Acceptance Criteria:
  - A recorded result reads back identical.
  - A store with no result reads the line unmeasured naming the missing record.
  - The overdue part reads as T-007 states with and without a confirmed cadence.
  - No resource or seed records a result in the company's store.
- Gate: none

### T-022 Deliver the CTO report

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-005, T-006, T-017, T-018, T-020, T-021
- Traces to: S-004, A-008
- Status: assumption-dependent
- Description: The CTO report carries the lines T-006 lists from the one read.
- Acceptance Criteria:
  - Each task shows cost and quality in their cases and its count of comparable runs against the recorded ten.
  - Every task below ten is labelled qualitative and none below ten is labelled evidence.
  - Every controller decision booked in the period is listed as recorded.
  - The selection basis reads exactly as the router recorded it, and the re-verification line reads as T-007 states for the period's datastore date.
- Gate: none

### T-023 Deliver the CFO summary on the decided company ceiling

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-008, T-017, T-018
- Traces to: S-005, A-002, A-008
- Status: assumption-dependent
- Description: The CFO summary carries the lines T-008 lists from the one read, and every output T-008 names describes the allotment as the owner's decided ceiling.
- Acceptance Criteria:
  - Company booked metered spend reads against the recorded USD 34.42 allotment and the recorded USD 77.41 envelope, with the USD 42.99 standing charge separately and never netted.
  - No output describes the allotment as interim.
  - An unbudgeted channel reads its utilisation unmeasured naming the missing amount, and a budgeted channel in its case.
  - Revenue-derived figures read not yet earnable or unmeasured, never a zero amount.
- Gate: none

### T-024 Deliver the channel performance report

- Owner: omn-dev-1-implement
- Complexity: S (confidence: low)
- Depends on: T-009, T-017, T-018
- Traces to: S-006, A-008
- Status: assumption-dependent
- Description: The channel performance report carries one line per channel the store holds, as T-009 designs it, from the one read.
- Acceptance Criteria:
  - Each channel the store holds has exactly one line, and no other channel appears.
  - Audience figures read unmeasured while no source records them.
  - Progress reads against the recorded programme thresholds, or reads as T-009 states where they are not recorded.
  - Days to 2027-02-01 are counted on the datastore's date, and no channel is created, configured or routed to.
- Gate: none

### T-025 Deliver the risk summary

- Owner: omn-dev-1-implement
- Complexity: S (confidence: low)
- Depends on: T-010, T-017, T-018
- Traces to: S-007, A-008
- Status: assumption-dependent
- Description: The risk summary carries the lines T-010 lists from the one read.
- Acceptance Criteria:
  - Each line resolves to one case, and a line no source records reads unmeasured naming the absent source.
  - Each coded precondition appears with its recorded state.
  - The next dated change is named and counted on the datastore's date.
  - No precondition reads discharged in the company's own state.
- Gate: none

### T-026 Deliver the recommendation rule catalogue

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-011, T-019, T-022, T-023
- Traces to: S-008, A-011
- Status: assumption-dependent
- Description: The closed catalogue T-011 enumerates evaluates over the readings of the one read and issues or abstains as designed, as a member of the deterministic set.
- Acceptance Criteria:
  - Every recommendation names its rule, reading, case and recorded anchor, labelled a rule output.
  - A rule whose reading is unmeasured issues nothing and states its abstention, naming the reading.
  - Two evaluations over one read yield identical recommendations in identical order.
  - Each CTO recommendation carries its ten fields, any unstated field unmeasured or labelled an estimate.
- Gate: none

### T-027 Deliver the open-decisions register

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-012, T-015, T-017
- Traces to: S-009, S-014, A-005, A-010
- Status: assumption-dependent
- Description: The register exists as T-012 designs it, loaded with its initial entries in demonstration stores and read inside the one read, carrying the price-capture question open unless T-015's ruling settles it.
- Acceptance Criteria:
  - Every entry reads back identical with all its facts.
  - An in-place edit of a decided entry is refused for every writer, and a supersession is a new entry naming the one it replaces.
  - Recording an entry leaves every threshold, budget, configuration value, approval and control unchanged.
  - The loaded entries match the decision record and the previous wave's report entry by entry, the re-admission question among them open.
- Gate: none

### T-028 Deliver the CEO brief

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-013, T-019, T-022, T-023, T-024, T-025, T-026, T-027
- Traces to: S-010
- Status: ready
- Description: The brief carries its seven sections in order from the one read, as T-013 designs it, produced on request for any week.
- Acceptance Criteria:
  - The seven sections appear in the stated order.
  - Every line names the decision it informs, and no line informs none.
  - The decisions-required section lists exactly the open register entries of the same read.
  - The brief is produced on request and is neither scheduled nor sent anywhere.
- Gate: none

### T-029 Deliver the read-only dashboard

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-014, T-028
- Traces to: S-011, A-009
- Status: assumption-dependent
- Description: The dashboard renders the composed read in the form T-014 designs, reached from the operator entry point, with no action and no write path.
- Acceptance Criteria:
  - Every tile equals its report line from the same read, with the same instant and case.
  - The surface offers no action that approves, sends back, configures, records, publishes or reaches a capability.
  - The awaiting-approval figure equals the per-channel approval view over gate state.
  - The surface opens no network listener and shows no credential, handle or secret.
- Gate: none

### T-030 Extend the boundary assertions over every added type

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-028, T-029
- Traces to: S-012, S-020
- Status: ready
- Description: The membership assertion and the boundary assertions cover every type this change adds, and every report, rule, register-reading and dashboard path is in the deterministic set with no write path to production, gate, configuration, budget or operation records.
- Acceptance Criteria:
  - The membership assertion covers every new figure-carrying type and fails a seeded bare numeric member, naming it.
  - Every composing, rule and rendering type sits in the deterministic set, and the assertion that the set cannot reach the capability boundary covers it.
  - An assertion fails a seeded write path or action on the dashboard, naming it.
  - A solution build runs every assertion, and no delivered assertion is weakened.
- Gate: none

### T-031 Deliver the owner's ten in selection

- Owner: omn-dev-1-implement
- Complexity: S (confidence: low)
- Depends on: T-005
- Traces to: S-013, A-002
- Status: assumption-dependent
- Description: Selection keeps the labelled configured ordering wherever any candidate has fewer than ten comparable runs and ranks on evidence at ten or more, as T-005 designs it.
- Acceptance Criteria:
  - At nine comparable runs of any candidate, selection equals the configured ordering, labelled configured, stating the count.
  - At ten or more for every candidate, all compared quantities observed, selection ranks on evidence and states the count.
  - No evidence ranking occurs below ten.
  - The route register is unchanged by any selection.
- Gate: none

### T-032 Apply the price-capture ruling

- Owner: omn-dev-1-implement
- Complexity: S (confidence: low)
- Depends on: T-015
- Traces to: S-015, A-004
- Status: assumption-dependent
- Description: Delivered price behaviour matches the ruling taken at the Design Gate, or, where none is taken, no record states the re-fetch rule honoured and the question stands open in the register.
- Acceptance Criteria:
  - Where a ruling is taken, the price path behaves as the ruling states.
  - Where none is taken, the register entry T-015 states is among the initial entries.
  - No record states the re-fetch rule honoured while the question is unresolved.
  - No metered call is made to show either route.
- Gate: none

### T-033 Deliver the named outcome past the column bound

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-016
- Traces to: S-016
- Status: ready
- Description: An admission decision whose utilisation exceeds the decision reading columns' bound is recorded or refused with the named outcome T-016 designs.
- Acceptance Criteria:
  - A fixture scope past the bound yields the named outcome.
  - No unnamed datastore error reaches the caller.
  - Existing rows read as T-016 states.
  - The controller's thresholds and mapping are unchanged.
- Gate: none

### T-034 Correct the previous wave's two-session evidence row

- Owner: omn-dev-1-implement
- Complexity: XS (confidence: high)
- Depends on: none
- Traces to: S-017
- Status: ready
- Description: The previous wave's change account carries a correction, findable from its two-session evidence row, stating that the check verifies deferral rather than waiting.
- Acceptance Criteria:
  - The correction is findable from the two-session evidence row.
  - The correction states that the check verifies deferral during the first call, not waiting.
  - No other row of the previous wave's change account is altered.
  - The correction names the date it was recorded.
- Gate: none

### T-035 Verify the one read on the datastore's clock

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-017, T-028, T-029
- Traces to: S-001, A-003, A-005
- Status: assumption-dependent
- Description: Recorded evidence supports the first, second, third, fourth and fifth upstream criteria on every surface, covering the round trip rather than the composition alone.
- Acceptance Criteria:
  - Review evidence lists every source of the read and every instant a surface shows, with the clock each is taken on.
  - Evidence of an operation, a gate transition and an alert committed during composition shows every affected line reflecting it or none.
  - Evidence with the process clock a day ahead and a day behind shows the period, boundaries and instants unchanged.
  - Evidence that a period stated final reads identical after a later commit, and a non-final period says so.
  - Evidence that a read during an admission held across a stand-in provider call making no metered call completes inside the 30-second command timeout, with zero unnamed timeouts and zero writers waiting on it.
- Gate: Review Gate, Verification Gate

### T-036 Verify the four cases on every surface

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-018, T-028, T-029, T-030
- Traces to: S-002, A-003, A-005, A-008
- Status: assumption-dependent
- Description: Recorded evidence supports the sixth, seventh, eighth, ninth and tenth upstream criteria.
- Acceptance Criteria:
  - Evidence that the membership assertion holds every figure of every surface in exactly one case.
  - Evidence of an observed zero and an unmeasured reading rendered side by side differently on the report and the dashboard.
  - Review evidence that no recorded amount renders as observed and each names where it is recorded.
  - Review evidence comparing each line's case with its delivered composer's, and evidence over a freshly installed store of no trend, projection or forecast.
- Gate: Review Gate, Verification Gate

### T-037 Verify the COO report

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-019
- Traces to: S-003, S-014, A-003, A-005
- Status: assumption-dependent
- Description: Recorded evidence supports the eleventh, twelfth, thirteenth and forty-fifth upstream criteria.
- Acceptance Criteria:
  - Inspection evidence of the five sections with every line naming its reading over a populated store.
  - Evidence over a fresh and a populated store that planned items read not recorded and the committed rate is never presented as planned, produced or published.
  - Evidence over fixture gate transitions that each rate reads unmeasured where its denominator is, and every send-back reason is listed.
  - Evidence over a fixture deferral of its reason, hold age and escalation state from the same read, and of zero automatic re-admissions.
- Gate: Review Gate, Verification Gate

### T-038 Verify the CTO report

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-022
- Traces to: S-004, A-003, A-005, A-007
- Status: assumption-dependent
- Description: Recorded evidence supports the fourteenth, fifteenth, sixteenth and seventeenth upstream criteria.
- Acceptance Criteria:
  - Evidence over fixture observations at nine and ten comparable runs of the label and count per task.
  - Round-trip evidence that every fixture controller decision is listed identical to what was recorded.
  - Evidence over fixtures in both bases that the selection basis reads as recorded and no configured rating shows as observed.
  - Evidence over periods either side of 2027-02-01, with and without a recorded re-verification, of the line's reading.
- Gate: Review Gate, Verification Gate

### T-039 Verify the CFO summary

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-023
- Traces to: S-005, A-003, A-005
- Status: assumption-dependent
- Description: Recorded evidence supports the eighteenth, nineteenth and twentieth upstream criteria.
- Acceptance Criteria:
  - Review evidence of every output stating the company basis, with zero describing the allotment as interim.
  - Evidence of the standing charge shown separately and never netted.
  - Evidence over one budgeted and one unbudgeted fixture channel of their utilisation readings.
  - Evidence that revenue-derived figures never read as a zero amount on the summary or the dashboard.
- Gate: Review Gate, Verification Gate

### T-040 Verify the channel performance report

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-024
- Traces to: S-006, A-003, A-005
- Status: assumption-dependent
- Description: Recorded evidence supports the twenty-first and twenty-second upstream criteria.
- Acceptance Criteria:
  - Evidence over a fresh store and fixture channels of exactly one line per channel held.
  - Evidence that audience figures read unmeasured while no source records them.
  - Evidence that days to 2027-02-01 count on the datastore's date.
  - Review evidence that no path creates, configures or routes to a channel.
- Gate: Review Gate, Verification Gate

### T-041 Verify the risk summary

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-025
- Traces to: S-007, A-003, A-005
- Status: assumption-dependent
- Description: Recorded evidence supports the twenty-third and twenty-fourth upstream criteria.
- Acceptance Criteria:
  - Evidence over a fresh store and fixtures of each line in one case, a line with no source reading unmeasured naming it.
  - Evidence of the next dated change named and counted on the datastore's date.
  - Evidence of each coded precondition with its recorded state over a fresh store and over fixture conditions.
  - Evidence that zero preconditions read discharged in the company's own state.
- Gate: Review Gate, Verification Gate

### T-042 Verify the recommendation rules

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-026
- Traces to: S-008, A-003, A-005, A-011
- Status: assumption-dependent
- Description: Recorded evidence supports the twenty-fifth, twenty-sixth, twenty-seventh, twenty-eighth and twenty-ninth upstream criteria.
- Acceptance Criteria:
  - Evidence firing each rule over fixtures, each output naming rule, reading, case and anchor.
  - Evidence making each rule's reading unmeasured in turn, each abstaining and naming the reading.
  - Evidence of two evaluations over one read yielding identical output in identical order.
  - Review evidence of the catalogue against the recorded thresholds and owner decisions, with at least one rule per executive report and every CTO recommendation's ten fields.
- Gate: Review Gate, Verification Gate

### T-043 Verify the open-decisions register

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-027
- Traces to: S-009, A-003, A-005, A-010
- Status: assumption-dependent
- Description: Recorded evidence supports the thirtieth, thirty-second, thirty-third and thirty-fourth upstream criteria.
- Acceptance Criteria:
  - Round-trip evidence of every entry's facts.
  - Evidence that an in-place edit of a decided entry is refused.
  - Evidence reading thresholds, budgets, configuration values, approvals and controls before and after recording an entry, unchanged.
  - Evidence comparing the loaded register entry by entry with the decision record and the previous wave's report.
- Gate: Review Gate, Verification Gate

### T-044 Verify the CEO brief

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-028
- Traces to: S-010, S-009, A-003, A-005
- Status: assumption-dependent
- Description: Recorded evidence supports the thirty-first and thirty-fifth upstream criteria and records the thirty-sixth as the owner's timed reading at the wave report.
- Acceptance Criteria:
  - Inspection evidence over a fresh and a populated store of the seven sections in order.
  - Evidence that every line names the decision it informs.
  - Review evidence of the composition path that the decisions-required section is the open register entries of the same read.
  - The evidence states that the timed reading is the owner's and records no reading on the owner's behalf.
- Gate: Review Gate, Verification Gate

### T-045 Verify the read-only dashboard

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-029, T-030
- Traces to: S-011, A-003, A-005, A-009
- Status: assumption-dependent
- Description: Recorded evidence supports the thirty-seventh, thirty-eighth, thirty-ninth and fortieth upstream criteria.
- Acceptance Criteria:
  - Evidence comparing every tile with its line over one read.
  - Build evidence that the boundary assertions find no action, write path or capability reference.
  - Evidence over fixture items awaiting approval that the figure equals the per-channel approval view.
  - Review evidence of zero network listeners and zero displayed credentials, handles or secrets.
- Gate: Review Gate, Verification Gate

### T-046 Verify no model call and no write on the report paths

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-030
- Traces to: S-012, S-018, A-003, A-005
- Status: assumption-dependent
- Description: Recorded evidence supports the forty-first, forty-second and forty-third upstream criteria from a solution build and a read-only check of the company store.
- Acceptance Criteria:
  - Solution-build evidence that every report, rule, register-reading and dashboard path is in the deterministic set and fails on a seeded violation.
  - Review evidence of zero writes to production, gate, configuration, budget or operation records and zero production permissions on those paths.
  - Evidence of every test target being the separate demonstration database.
  - Read-only evidence that the company store holds zero tables after the suite.
- Gate: Review Gate, Verification Gate

### T-047 Verify the owner's ten in selection

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-031
- Traces to: S-013, A-003, A-005
- Status: assumption-dependent
- Description: Recorded evidence supports the forty-fourth upstream criterion.
- Acceptance Criteria:
  - Evidence at nine comparable runs of the configured ordering, labelled, with the count.
  - Evidence at ten of an evidence ranking with the count.
  - Evidence of zero evidence rankings below ten.
  - Evidence that every re-pointed delivered selection check is listed with its reason.
- Gate: Review Gate, Verification Gate

### T-048 Verify the carried price-capture outcome

- Owner: omn-dev-2-reviewer
- Complexity: XS (confidence: low)
- Depends on: T-027, T-032
- Traces to: S-015, A-003, A-004
- Status: assumption-dependent
- Description: Recorded evidence supports the forty-sixth upstream criterion.
- Acceptance Criteria:
  - Inspection evidence of the ruling or of the open register entry naming its owners.
  - Review evidence of the price path against the ruling where one is taken.
  - Evidence that zero records state the re-fetch rule honoured while it is unresolved.
  - Evidence that no metered call was made.
- Gate: Review Gate, Verification Gate

### T-049 Verify the named outcome past the bound

- Owner: omn-dev-2-reviewer
- Complexity: XS (confidence: medium)
- Depends on: T-033
- Traces to: S-016, A-003, A-005
- Status: assumption-dependent
- Description: Recorded evidence supports the forty-seventh upstream criterion.
- Acceptance Criteria:
  - Evidence over a fixture scope past the bound of the named outcome.
  - Evidence of zero unnamed datastore errors reaching the caller.
  - Evidence that existing rows read as designed.
  - Evidence that the controller's thresholds and mapping are unchanged.
- Gate: Review Gate, Verification Gate

### T-050 Verify the change-account correction

- Owner: omn-dev-2-reviewer
- Complexity: XS (confidence: high)
- Depends on: T-034
- Traces to: S-017, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports the forty-eighth upstream criterion.
- Acceptance Criteria:
  - Inspection evidence that the correction is findable from the two-session evidence row.
  - Inspection evidence that it states deferral rather than waiting.
  - Evidence that no other row changed.
  - Evidence of the correction's recorded date.
- Gate: Review Gate, Verification Gate

### T-051 Confirm the binding constraints hold after the change

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-030, T-031, T-032, T-033, T-034
- Traces to: S-018, S-019, S-020, S-021, A-002, A-003, A-005
- Status: assumption-dependent
- Description: Recorded evidence from a solution build and review shows zero metered calls and zero spend, the absences, preconditions, owner-approval state and payee unchanged, the completed waves extended rather than re-created, and no invented quantity.
- Acceptance Criteria:
  - Evidence that committed spend is zero and the single-metered-operation exception unspent.
  - Review evidence that the five absences, the three preconditions, owner approval as a transition-table state and the company-level payee are unchanged, and no channel was created.
  - Review evidence that the union, the recorded-amount type, the horizon, the delivered readings and the boundary assertions are extended and none weakened.
  - Review evidence that no threshold, budget amount, channel configuration value, clip count, sizing figure or level-to-tier mapping is recorded outside self-dropping demonstration stores.
- Gate: Review Gate, Verification Gate

### T-052 Record what the change delivers and leaves to the owner

- Owner: omn-documentation
- Complexity: M (confidence: low)
- Depends on: T-035, T-036, T-037, T-038, T-039, T-040, T-041, T-042, T-043, T-044, T-045, T-046, T-047, T-048, T-049, T-050, T-051
- Traces to: S-018, S-021, A-002, A-005
- Status: assumption-dependent
- Description: The release record states what each report, rule and surface shows in the company's records today, which carried items closed, and what the change leaves to the owner.
- Acceptance Criteria:
  - The record states that every production, quality, approval-minutes and revenue line reads unmeasured or not recorded in the company's records, and labels every fixture a demonstration parameter.
  - The record states that the register and the re-verification record are not installed in the company store and what an install needs.
  - The record lists the owner questions this change surfaced, the week boundary, the re-verification cadence, the COO rule anchor and the programme thresholds' recording, with zero answered on the owner's behalf.
  - The record states zero spend, the exception unspent, and the owner's timed reading as awaited or as recorded by the owner.
- Gate: Closure Gate

## Dependencies

### 8.1 Dependency Edges

| From | To | Type | Justification |
|---|---|---|---|
| T-005 | T-006 | decision-gate | The CTO count is in the comparable-run unit that design fixes |
| T-007 | T-006 | contract | The re-verification line reads the record that design fixes |
| T-004 | T-011 | decision-gate | COO rules rest on the COO readings that design fixes |
| T-006 | T-011 | decision-gate | CTO rules rest on the CTO readings that design fixes |
| T-008 | T-011 | decision-gate | CFO rules rest on the CFO readings that design fixes |
| T-012 | T-013 | contract | The decisions-required section reads the register that design fixes |
| T-001 | T-017 | contract | The read binds to the snapshot, instant and finality that design fixes |
| T-002 | T-017 | contract | The read binds to the period that design fixes |
| T-003 | T-018 | contract | The rendering is the one that design fixes |
| T-004 | T-019 | contract | The COO lines are the ones that design lists |
| T-017 | T-019 | produces-consumes | The COO lines come from the one read |
| T-018 | T-019 | produces-consumes | The COO lines render their cases through the delivered rendering |
| T-006 | T-020 | contract | The decision reader is the one that design states |
| T-017 | T-020 | contract | The decision reading is taken inside the one read |
| T-007 | T-021 | contract | The record is the one that design states |
| T-017 | T-021 | contract | The record is read inside the one read |
| T-005 | T-022 | contract | The per-task count uses the unit that design fixes |
| T-006 | T-022 | contract | The CTO lines are the ones that design lists |
| T-017 | T-022 | produces-consumes | The CTO lines come from the one read |
| T-018 | T-022 | produces-consumes | The CTO lines render through the delivered rendering |
| T-020 | T-022 | produces-consumes | The CTO report lists the recorded decisions the reader delivers |
| T-021 | T-022 | produces-consumes | The re-verification line reads the delivered record |
| T-008 | T-023 | contract | The CFO lines and labels are the ones that design lists |
| T-017 | T-023 | produces-consumes | The CFO lines come from the one read |
| T-018 | T-023 | produces-consumes | The CFO lines render through the delivered rendering |
| T-009 | T-024 | contract | The channel lines are the ones that design lists |
| T-017 | T-024 | produces-consumes | The channel lines come from the one read |
| T-018 | T-024 | produces-consumes | The channel lines render through the delivered rendering |
| T-010 | T-025 | contract | The risk lines are the ones that design lists |
| T-017 | T-025 | produces-consumes | The risk lines come from the one read |
| T-018 | T-025 | produces-consumes | The risk lines render through the delivered rendering |
| T-011 | T-026 | contract | The catalogue is the one that design enumerates |
| T-019 | T-026 | produces-consumes | COO rules evaluate over the delivered COO readings |
| T-022 | T-026 | produces-consumes | CTO rules evaluate over the delivered CTO readings |
| T-023 | T-026 | produces-consumes | CFO rules evaluate over the delivered CFO readings |
| T-012 | T-027 | contract | The register is the one that design states |
| T-015 | T-027 | decision-gate | Whether the price-capture question is an open initial entry depends on the ruling |
| T-017 | T-027 | contract | The register is read inside the one read |
| T-013 | T-028 | contract | The brief is the composition that design states |
| T-019 | T-028 | produces-consumes | The brief takes the COO lines |
| T-022 | T-028 | produces-consumes | The brief takes the CTO lines |
| T-023 | T-028 | produces-consumes | The brief takes the CFO lines |
| T-024 | T-028 | produces-consumes | The brief takes the channel lines |
| T-025 | T-028 | produces-consumes | The brief takes the risk lines |
| T-026 | T-028 | produces-consumes | The brief takes the recommendations |
| T-027 | T-028 | produces-consumes | The brief lists the open register entries |
| T-014 | T-029 | contract | The dashboard takes the form that design states |
| T-028 | T-029 | produces-consumes | The dashboard renders the composed read |
| T-028 | T-030 | produces-consumes | The assertions must cover every type composed into the brief |
| T-029 | T-030 | produces-consumes | The assertions must cover the dashboard's types |
| T-005 | T-031 | contract | Selection applies the threshold as that design states |
| T-015 | T-032 | decision-gate | What is applied depends on the ruling |
| T-016 | T-033 | contract | The outcome is the one that design states |
| T-017 | T-035 | verification | Validates the one read |
| T-028 | T-035 | verification | The round trip is checked on the brief |
| T-029 | T-035 | verification | The round trip is checked on the dashboard |
| T-018 | T-036 | verification | Validates the rendering |
| T-028 | T-036 | verification | The cases are checked on the reports and the brief |
| T-029 | T-036 | verification | The cases are checked on the dashboard |
| T-030 | T-036 | verification | The membership criterion rests on the extended assertion |
| T-019 | T-037 | verification | Validates the COO report |
| T-022 | T-038 | verification | Validates the CTO report |
| T-023 | T-039 | verification | Validates the CFO summary |
| T-024 | T-040 | verification | Validates the channel report |
| T-025 | T-041 | verification | Validates the risk summary |
| T-026 | T-042 | verification | Validates the rules |
| T-027 | T-043 | verification | Validates the register |
| T-028 | T-044 | verification | Validates the brief |
| T-029 | T-045 | verification | Validates the dashboard |
| T-030 | T-045 | verification | The no-action criterion rests on the extended assertions |
| T-030 | T-046 | verification | The no-call and no-write criteria rest on the extended assertions |
| T-031 | T-047 | verification | Validates the threshold in selection |
| T-027 | T-048 | verification | The open-entry route is checked in the register |
| T-032 | T-048 | verification | Validates the applied ruling |
| T-033 | T-049 | verification | Validates the named outcome |
| T-034 | T-050 | verification | Validates the correction |
| T-030 | T-051 | verification | The confirmation takes its build evidence from the extended assertions |
| T-031 | T-051 | verification | The confirmation covers the selection change |
| T-032 | T-051 | verification | The confirmation covers the price path |
| T-033 | T-051 | verification | The confirmation covers the decision record change |
| T-034 | T-051 | verification | The confirmation covers the corrected record |
| T-035 | T-052 | produces-consumes | The record states what the one-read evidence established |
| T-036 | T-052 | produces-consumes | The record states what the case evidence established |
| T-037 | T-052 | produces-consumes | The record states what the COO evidence established |
| T-038 | T-052 | produces-consumes | The record states what the CTO evidence established |
| T-039 | T-052 | produces-consumes | The record states what the CFO evidence established |
| T-040 | T-052 | produces-consumes | The record states what the channel evidence established |
| T-041 | T-052 | produces-consumes | The record states what the risk evidence established |
| T-042 | T-052 | produces-consumes | The record states what the rule evidence established |
| T-043 | T-052 | produces-consumes | The record states what the register evidence established |
| T-044 | T-052 | produces-consumes | The record states what the brief evidence established |
| T-045 | T-052 | produces-consumes | The record states what the dashboard evidence established |
| T-046 | T-052 | produces-consumes | The record states what the boundary evidence established |
| T-047 | T-052 | produces-consumes | The record states what the selection evidence established |
| T-048 | T-052 | produces-consumes | The record states what the price evidence established |
| T-049 | T-052 | produces-consumes | The record states what the bound evidence established |
| T-050 | T-052 | produces-consumes | The record states what the correction evidence established |
| T-051 | T-052 | produces-consumes | The record states what the confirmation established |

### 8.2 External Dependencies

None identified. No task waits on a party outside the plan's authority: the owner's answers to Q-001, Q-004, Q-008 and Q-009 bear on lines the plan builds with a stated reading while unanswered, the price-capture ruling is taken at the Design Gate by a listed gate owner, and no task needs spend.

### 8.3 Implementation Order

- Wave 1: T-001, T-002, T-003, T-004, T-005, T-007, T-008, T-009, T-010, T-012, T-014, T-015, T-016, T-034
- Wave 2: T-006, T-013, T-017, T-018, T-031, T-032, T-033, T-050
- Wave 3: T-011, T-019, T-020, T-021, T-023, T-024, T-025, T-027, T-047, T-049
- Wave 4: T-022, T-037, T-039, T-040, T-041, T-043, T-048
- Wave 5: T-026, T-038
- Wave 6: T-028, T-042
- Wave 7: T-029, T-044
- Wave 8: T-030, T-035
- Wave 9: T-036, T-045, T-046, T-051
- Wave 10: T-052

## Suggested Workflow

Selected workflow: implement-feature

Selected because the change delivers new capability against an approved scope with measurable acceptance criteria, and the run is already routed through this workflow's phase model.

| Phase | Tasks |
|---|---|
| scope-and-acceptance | none; the phase is complete and its artifact is this plan's upstream input |
| execution-planning | none; this plan is the phase's own output |
| solution-design-and-risk-assessment | T-001, T-002, T-003, T-004, T-005, T-006, T-007, T-008, T-009, T-010, T-011, T-012, T-013, T-014, T-015, T-016 |
| implementation | T-017, T-018, T-019, T-020, T-021, T-022, T-023, T-024, T-025, T-026, T-027, T-028, T-029, T-030, T-031, T-032, T-033, T-034 |
| quality-review | T-035, T-036, T-037, T-038, T-039, T-040, T-041, T-042, T-043, T-044, T-045, T-046, T-047, T-048, T-049, T-050, T-051 |
| documentation-and-release-handoff | T-052 |

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
| architecture-analysis | T-001, T-002, T-003, T-004, T-005, T-006, T-007, T-008, T-009, T-010, T-011, T-012, T-013, T-014, T-015, T-016 | architect | Primary |
| technical-approach-definition | T-001, T-002, T-005, T-007, T-012, T-014, T-016 | architect | Primary |
| reuse-assessment | T-001, T-003, T-004, T-006, T-007, T-008 | architect | Primary |
| option-evaluation | T-002, T-014, T-015 | architect | Primary |
| structural-risk-analysis | T-001, T-012, T-016 | architect | Primary |
| implementation-delivery | T-017, T-018, T-019, T-020, T-021, T-022, T-023, T-024, T-025, T-026, T-027, T-028, T-029, T-030, T-031, T-032, T-033, T-034 | omn-dev-1-implement | Primary |
| quality-verification | T-035, T-036, T-037, T-038, T-039, T-040, T-041, T-042, T-043, T-044, T-045, T-046, T-047, T-048, T-049, T-050, T-051 | omn-dev-2-reviewer | Primary |
| code-review | T-036, T-045, T-046, T-051 | omn-dev-2-reviewer | Primary |
| documentation | T-052 | omn-documentation | Primary |
| release-communication | T-052 | omn-documentation | Primary |

### 10.2 Required Skills

| Skill | File | Tasks | Level |
|---|---|---|---|
| S01 | skills/architecture/clean-architecture-checklist.md | T-001, T-003, T-012, T-014, T-030 | Advisory |
| S02 | skills/business/domain-modeling.md | T-002, T-004, T-005, T-006, T-008, T-009, T-010, T-011, T-012, T-013 | Primary |
| S03 | skills/dotnet/engineering-playbook.md | T-017, T-018, T-019, T-020, T-021, T-022, T-023, T-024, T-025, T-026, T-027, T-028, T-029, T-030, T-031, T-032, T-033 | Secondary |
| S06 | skills/database/database-engineering.md | T-001, T-007, T-012, T-016, T-017, T-020, T-021, T-027, T-033 | Secondary |
| S07 | skills/testing/testing-strategy.md | T-035, T-036, T-037, T-038, T-039, T-040, T-041, T-042, T-043, T-044, T-045, T-046, T-047, T-048, T-049, T-050, T-051 | Secondary |
| S08 | skills/performance/performance-engineering.md | T-001, T-017, T-035 | Advisory |
| S09 | skills/security/secure-engineering.md | T-012, T-014, T-027, T-029, T-030, T-043, T-045, T-046 | Advisory |
| S10 | skills/git/git-collaboration.md | T-034, T-052 | Advisory |
| S11 | skills/logging/observability-logging.md | T-052 | Advisory |
| S12 | skills/error-handling/error-handling-strategy.md | T-001, T-017, T-033 | Advisory |

## Acceptance Criteria

1. Over a populated demonstration store every line of every report, the brief and the dashboard carries one datastore instant, a concurrent commit appears on every affected line or none, a skewed process clock moves nothing, a read during a held admission completes inside the command timeout, and the owner reads the brief end to end within 15 minutes. Verifies business objective 1. Evidence: the demonstrations recorded in T-035 and T-044 and the owner's timed reading at the wave report.
2. On every surface each figure is in exactly one of the four cases, an observed zero and an unmeasured reading render differently, no recorded amount renders as observed, and over a freshly installed store every unmeasurable line reads as its delivered reading decides. Verifies business objective 2. Evidence: the build assertion and demonstrations recorded in T-036.
3. The COO, CTO and CFO reports, channel performance and the risk summary each carry their lines in their cases from the one read, including the controller's recorded decisions, the selection basis with its count, the re-verification line and every deferred request not re-admitted. Verifies business objective 3. Evidence: the inspections and demonstrations recorded in T-037, T-038, T-039, T-040 and T-041.
4. Every recommendation names its rule, reading, case and recorded anchor, every rule over an unmeasured reading abstains naming it, repeated evaluation is identical, and selection ranks on evidence only at ten comparable runs or more. Verifies business objective 4. Evidence: the demonstrations and review recorded in T-042 and T-047.
5. The register lists every open owner question of the decision record and the previous wave's report with owner and interim ruling, the three decisions of 2026-10-09 as decided, refuses an in-place edit, changes no control, and the price-capture question is either settled by a ruling the price path matches or stands open in it. Verifies business objective 5. Evidence: the demonstrations and inspection recorded in T-043 and T-048.
6. The dashboard offers no action, write path, capability reference, network listener or secret, every report path is in the deterministic set, zero metered calls and zero spend occur, the company store holds zero tables, and the absences, preconditions, owner-approval state and payee are unchanged. Verifies business objective 6. Evidence: the build and review evidence recorded in T-045, T-046 and T-051, and the release record produced by T-052.
7. An admission past the decision columns' bound yields a named outcome, and the previous wave's change account carries a findable correction of its two-session row. Verifies business objective 7. Evidence: the demonstration and inspection recorded in T-049 and T-050.

## Definition of Done

- [ ] All seven plan acceptance criteria are verified with recorded evidence, the owner's timed reading recorded by the owner or recorded as awaited
- [ ] Design Gate, Review Gate, Verification Gate and Closure Gate are approved with owners recorded
- [ ] Every task acceptance criterion is satisfied or formally waived with the waiver recorded
- [ ] Every assumption is confirmed by its named role or converted to a recorded decision
- [ ] Every risk is closed or accepted with its named owner recorded
- [ ] Every open question is closed or explicitly accepted by its named owner
- [ ] Committed spend is recorded as zero and the single-metered-operation exception as unspent
- [ ] The company's own store is recorded as holding zero tables after the suite
- [ ] The release record and the release-impact notes are published
- [ ] Durable outcomes are recorded to memory per `memory/memory-governance.md`

## Open Questions

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| Q-001 | Which week boundary and time zone define a report period, and which booking month a CFO line reads when a week spans a month end? Non-blocking: the architect proposes, omn-tech-lead rules at the Design Gate, and the owner is told at the wave report. | no | architect, with the owner's confirmation routed by omn-orchestrator | T-002, T-017, T-023 |
| Q-002 | Where is the register held so that it is part of the one read, and how does the company see it while its own store holds no tables? | no | architect | T-012, T-027 |
| Q-003 | What counts as one comparable run of one task, the unit the owner's ten is counted in? | no | architect | T-005, T-022, T-031 |
| Q-004 | Who records platform-policy re-verification results, and at what cadence is a statement overdue? The scope's question cites a weekly obligation, but the master plan records the weekly obligation as the CTO report carrying the status and records the re-verification cadence itself as monthly until 2027-02-01; the owner is asked to confirm the cadence rather than have one inferred. | no | omn-orchestrator, for the requesting owner, with architect for the record | T-007, T-021, T-038 |
| Q-005 | Does capturing a unit price at admission honour the owner's rule of re-fetching it immediately before use, or must the booking re-fetch it? Non-blocking: ruled at the Design Gate, or carried open in the register. | no | omn-tech-lead, with the requesting owner | T-015, T-032, T-048 |
| Q-006 | Is the read-only dashboard operator console text or a rendered file the owner opens? | no | architect | T-014, T-029 |
| Q-007 | Did the request's parenthesis enumerate every carried item? Answered at the Scope Gate: the four named items plus a reader for the controller's recorded decisions; it is listed only so that the scope's question register reads through unchanged. | no | omn-tech-lead | T-020, T-032, T-033, T-034 |
| Q-008 | No COO threshold is recorded in code, configuration or an owner decision today, while the scope requires at least one rule per executive report. Which recorded anchor does a COO rule key on, for example escalated held work against its own recorded hold timeout or an open register entry's interim ruling, or must the owner record a threshold? | no | architect, with omn-product-owner | T-011, T-026, T-042 |
| Q-009 | The partner-programme thresholds and the dated changes on 2026-09-24, 2027-01-31 and 2027-02-01 appear nowhere in the delivered source and are recorded only in the master plan's monetisation and platform sections. Does the master plan count as their recording, so the progress and dated-change lines cite them as recorded amounts, or must the owner record them? | no | architect, with the owner's confirmation routed by omn-orchestrator | T-009, T-010, T-024, T-025 |
| Q-010 | The delivered finality discipline raises the record horizon, an update of the horizon row, before reading a month as final. Does the one read raise the horizon, so a dashboard reading through it holds a write path the scope forbids, or read the horizon as stored and state finality from it? | no | architect | T-001, T-014, T-017, T-029 |

## Traceability Matrix

| Statement | Covered by |
|---|---|
| S-001 | T-001, T-002, T-017, T-035, A-001, A-006, Q-001, Q-010 |
| S-002 | T-003, T-018, T-036, A-008 |
| S-003 | T-004, T-019, T-037 |
| S-004 | T-005, T-006, T-007, T-020, T-021, T-022, T-038, A-007, Q-003, Q-004 |
| S-005 | T-008, T-023, T-039, Q-001 |
| S-006 | T-009, T-024, T-040, Q-009 |
| S-007 | T-010, T-025, T-041, Q-009 |
| S-008 | T-011, T-026, T-042, A-011, Q-008 |
| S-009 | T-012, T-013, T-027, T-043, T-044, A-010, Q-002 |
| S-010 | T-013, T-028, T-044 |
| S-011 | T-014, T-029, T-045, A-009, Q-006, Q-010 |
| S-012 | T-030, T-046 |
| S-013 | T-005, T-031, T-047, Q-003 |
| S-014 | T-004, T-012, T-019, T-027, T-037 |
| S-015 | T-015, T-032, T-048, A-004, Q-005 |
| S-016 | T-016, T-033, T-049, Q-007 |
| S-017 | T-034, T-050, Q-007 |
| S-018 | T-046, T-051, T-052, A-002 |
| S-019 | T-051 |
| S-020 | T-001, T-003, T-017, T-018, T-030, T-051 |
| S-021 | T-051, T-052, A-005 |

Statement register, normalised from the supplied feature request and the upstream scope definition of this run (digest sha256:700fb72d5382bdd45f94d32fa3b0f872): the first seventeen statements match the upstream in-scope items in order and carry the same identifiers, and the last four carry the binding constraints, namely S-018 zero model, executive and metered calls and zero spend, S-019 nothing publishes and the controls stay as delivered, S-020 the union, recorded-amount type, horizon, delivered readings and boundary assertions extended and never weakened with the completed waves built upon, and S-021 no invented quantity and demonstrations in self-dropping stores only. The open questions Q-001, Q-002, Q-003, Q-004, Q-005, Q-006 and Q-007; Q-008, Q-009 and Q-010 are raised here. Upstream acceptance criteria are cited by ordinal in the order the scope definition lists them, because their identifiers share this plan's assumption prefix. Repository rescan: the task context names no relevant module or file, so the delivered source in the feature worktree was read before planning, as the dispatch briefing directs; the counts match the request at 107 and 7 under the source tree and 39 and 5 under the test tree, with seven schema resources.
