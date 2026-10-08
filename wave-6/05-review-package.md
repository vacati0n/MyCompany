```yaml
reviewPackage:
  packageId: RP-2026-0004
  reviewReference: MC-7, the multi-channel change at its corrected head cea41d3 on branch feature/mc-7-wave-6-the-multi-channel-capability in the feature worktree at .worktrees/mc-7-feature, one correction commit over the first-pass head 721ea92, quality-review phase of run-cdce6ebe03ac, re-verified after the correction cycle
  sourceInputs:
    - type: implementation-report
      reference: runs/run-cdce6ebe03ac/states/implementation/artifacts/implementation-report.md
    - type: feature-request
      reference: tasks/MC-7/input.md
    - type: design-reference
      reference: the seven decision records of this run's solution-design phase, D-001, D-002, D-003, D-006, D-007, D-008 and D-009
    - type: standards-checklist
      reference: runs/run-cdce6ebe03ac/task-context.yaml, its scope items and its eighteen design constraints
    - type: code-diff
      reference: the change from be3581e to 721ea92 read in the first pass, and the correction diff from 721ea92 to cea41d3 read in full in this pass
    - type: test-evidence
      reference: the executions performed in both passes, listed under Test Adequacy Assessment
    - type: build-inputs
      reference: throwaway copies extracted from 721ea92 and from cea41d3 into the reviewer's own staging folder outside the repository, each carrying the same two probe checks
  producedBy: omn-dev-2-reviewer
  agentVersion: 1.1.0
  schemaVersion: 1.0.0
  status: complete
  verdict: approve-with-corrections
  inputDigest: sha256:195a97c641e0ed18f119031734fd7a22
  contextDigest: sha256:7c56e10872c3a10df579c4c56767bfbe
```

## Metadata

- Review ID: RP-2026-0004
- Reviewer: omn-dev-2-reviewer
- Change under review: MC-7 at its corrected head cea41d3 in the feature worktree, delivered by implementation report IR-2026-0007 as updated in place, against the approved technical design and its seven decision records, read with the gate answers, the owner's decision on channel isolation recorded 2026-10-08, and the orchestrator's correction-cycle rulings restated in the re-verification message: latest gate transition by datastore order, report lines from one consistent read, headroom and alert stamps on the datastore's month, and this review's probe evidence admitted
- Review date: 2026-10-09

## Review Scope

- In scope: the correction commit read in full against the six first-pass findings and the orchestrator's rulings, each finding judged closed or open by reading and by execution; the corrected gate order, the one-read report, the booking-month headroom and the resume send-back each attacked again for the two-clock class, a new fork, a deadlock and a snapshot gap; the one-time renumbering of existing gate rows; the implementer's four objections rated on their merits; and the first-pass scope where the correction touched it.
- Out of scope: acceptance-criterion validation and release thresholds, which belong to omn-qa; the merge and release decisions, which belong to omn-tech-lead; product scope, which belongs to omn-product-owner; the known issues carried from earlier release records except where this change touches them; the provider path of the capability boundary, read and exercised only through the capability suite's fakes because no adapter is registrable and executing it needs a provider and a credential, both excluded; the build-time IL scan's recorded limits, not re-measured; the first-pass exclusions not touched by the correction commit stand as stated in the first pass, namely the domain read models, the profile model, the composition root and the boundary-test bodies read in their diffs only, and the delivered fixture re-pointing judged by executed results.
- Evidence reviewed: first pass, as recorded then — the sixth schema resource, the appender, the gate service, the channel adapters, the channel composers, the rights-check step, the copyright-check handler, the profile service, the channel test kit, the diffs of every other changed production file, the seven decision records and the implementation report; this pass, read in full — the correction diff to the sixth schema resource, the budget reader port and its adapter, the capability gateway, the reporting service, the rights-check step, the channel adapters, the gate ledger, the capability suite and its fakes, the schema static check, and the seven new record-store checks, with the remaining readers of the gate-transition record searched for any ordering by instant. Executions are listed under Test Adequacy Assessment.

## Findings

| ID | Severity | Category | Location | Requirement | Finding | Correction Request | Status |
|---|---|---|---|---|---|---|---|
| `F-001` | high | correctness | db/006-multi-channel.sql:224 | Decision record D-008 and the hard design constraint that every gate change starts from the recorded current state; the previous wave's one-clock rule | Closed and confirmed by execution. Latest is now a datastore position taken from a sequence inside the check, under the item-row hold, and the check, the gate ledger and the approval listing all order by it; the in-memory double already decides by commit order, so the implementer's objection that it needed no change is correct. The first-pass probe rerun on cea41d3: after the verdict with an earlier instant the state reads Approved, and the contradictory send-back is refused as a changed recorded state. The renumbering orders only rows that existed before the resource, by the instant every earlier reader already used, so their meaning is kept; every new row is positioned after them; no reader of the record orders state by instant any more. | `CR-001` | resolved |
| `F-002` | high | correctness | src/MediaCompany.Deterministic/Services/ReportingService.cs:61 | The hard design constraint that every additive company cost figure equals the sum of its channel figures exactly; decision record D-003 | Closed and confirmed by execution. The report now reads one source, the partition, and takes every company line from the partition's company row. The implementer's objection holds: the read is two statements, not one, but both run either under the exclusive horizon hold, while no operation can be stamped because every writer needs the horizon shared, or in one repeatable-read snapshot in the fallback; the cost lines come from one grouping-sets statement in both paths, so the fallback is additive too. The first-pass probe rerun on cea41d3 with an operation interleaved after the read: company line 0.01050000, channel lines summing to 0.01050000. | `CR-002` | resolved |
| `F-003` | medium | correctness | src/MediaCompany.Capability/CapabilityGateway.cs:115 | Decision record D-006; the previous wave's one-clock rule | Closed for the two-clock defect. Headroom is read for the datastore's booking month, decided in the same statement as the amount on the datastore's clock and horizon, and alerts carry the stored operation instant; a capability check asserts the booking-month read. The added port member is a deviation from the design's rule against new members on delivered ports, necessary and sound as recorded. What remains is one clock read at two instants, admission before the provider call and booking after it, which is carried under Residual Risk; the path stays unexecuted end to end, as the implementer's objection states. | `CR-003` | resolved |
| `F-004` | medium | correctness | src/MediaCompany.Deterministic/Services/RightsCheckStep.cs:150 | Decision record D-009 | Closed and confirmed by execution. Where recorded evidence reads releasable and the presentation refuses for rights, the step sends the version back naming the disagreement; any other refusal is reported as it stands. A record-store check drives the resume case to the send-back. | `CR-004` | resolved |
| `F-005` | medium | test-adequacy | tests/MediaCompany.Persistence.Tests/MultiChannelIntegrationTests.cs:578 | This agent's reasoning procedure, an executed check for each changed behaviour; the validation plans of decision records D-007 and D-008 | Closed. Executed checks now cover eight concurrent gate writers of one item version, a transition with an earlier caller instant, metered operations interleaved both ways with appenders beside month and throughput closures inside a deadlock timeout, the report's additivity on the store with a write between reads, and the resume case. | `CR-005` | resolved |
| `F-006` | low | standards | src/MediaCompany.Deterministic/Services/RightsCheckStep.cs:118 | Decision record D-001's rule that an absent value reads absent and never a default | Closed. A refused run reports the unrun check as absent, and a record-store check asserts it. | `CR-006` | resolved |
| `F-007` | low | standards | db/006-multi-channel.sql:3 | The resource's own statement of what it alters, writes and how it reverses; the design constraint that every schema change is additive with every row keeping its meaning | New in the correction. The resource's header still says that no delivered table is altered and that the only row it writes is the chain-head row, while it now adds a column to the delivered gate-transition record and back-fills every existing row; its reversal statement does not say that dropping the order column becomes lossy once a transition is recorded after the resource, since that order is no longer derivable from the instants. The order column is also nullable and the readers sort it descending, which places a null first; a null arises only if the check is disabled, which already defeats the from-state check. No behavioural consequence in a supported path. | `CR-007` | open |

## Severity Summary

- Critical: 0
- High: 2
- Medium: 3
- Low: 2

## Standards and Architecture Conformance

- Coding standards: the .NET engineering, error-handling and logging playbooks and the repository's conventions were applied; conformant, with the stale resource header in `F-007`; the build and every suite pass at cea41d3.
- Architecture rules: the accepted design, its seven decision records, the orchestrator's correction rulings and the clean-architecture checklist's seven review questions were applied; conformant on D-003, D-006, D-008 and D-009 after the corrections; one deviation, the booking-month member on the delivered budget reader port, is necessary for the ruling on headroom and is recorded as such; module placement, the channel key set, the company-level payee, the three-case union, the read-only approval views, the booking check and the chain-head hold are unchanged and conformant.
- Security criteria: the security engineering playbook and the integrity constraints on the audit chain, the gate, owner approval and the payee were applied; the gate history no longer admits two transitions out of one state for any caller instant, the audit chain stays linear for every writer, no per-channel payee or approval key is admissible, the five structural absences and the deterministic boundary hold in the executed boundary suite, and nothing publishes; no credential appears in the change.
- Exceptions requested: None identified.

## Test Adequacy Assessment

- Test evidence reviewed: first pass at 721ea92 — dotnet test MediaCompany.slnx with the test connection variable naming Database=mediacompany_demo, 612 executed and 612 passed; without the variable, 606 collected, 516 passed, 90 skipped; the two probe checks demonstrating the first two findings. This pass at cea41d3, executed here — dotnet test MediaCompany.slnx with the same variable, 619 executed and 619 passed (domain 48, deterministic 348, capability 41, architecture 52, persistence 130), exit 0; dotnet test MediaCompany.slnx --no-build without the variable, 613 collected, 517 passed, 96 skipped, 0 failed, exit 0; both confirm the updated account; dotnet test of the persistence project with a name filter over a throwaway copy of cea41d3 carrying the same two probe checks, both executed and passing with the corrected outcomes; the feature worktree status was clean after every run.
- Coverage of changed behavior: every correction is exercised by an executed check, five on the store and one in the capability suite; the booking-month headroom is exercised through the capability fakes only, because the provider path cannot run in this build.
- Gaps requiring new tests: None identified beyond the provider path, which needs a provider and a credential this change may not use; carried under Residual Risk and owned by omn-dev-1-implement when that path is authorised, since omn-qa owns no phase in this workflow.

## Correction Requests

| ID | Addresses | Required change | Blocking | Owner |
|---|---|---|---|---|
| `CR-001` | `F-001` | Closed at cea41d3: an item version's recorded current state is the state its last datastore-serialised transition wrote, for every writer and whatever instant a caller supplies, shown by an executed store check | yes | omn-dev-1-implement |
| `CR-002` | `F-002` | Closed at cea41d3: every company line of the report and the channel lines beside it come from one consistent read, shown by an executed store check with an interleaved operation | yes | omn-dev-1-implement |
| `CR-003` | `F-003` | Closed at cea41d3: headroom, evaluation and alert instant are decided for one month on the datastore's clock | no | omn-dev-1-implement |
| `CR-004` | `F-004` | Closed at cea41d3: a resumed step whose evidence and presentation disagree reaches the send-back rest | no | omn-dev-1-implement |
| `CR-005` | `F-005` | Closed at cea41d3: executed checks cover concurrent gate writers, metered operations with appenders and closures, report additivity on the store and the resume case | no | omn-dev-1-implement |
| `CR-006` | `F-006` | Closed at cea41d3: a refused run reports its unrun stage as absent | no | omn-dev-1-implement |
| `CR-007` | `F-007` | The sixth resource states truthfully that it adds and back-fills a column on the delivered gate-transition record, and that dropping it is lossy once a later transition exists; the order column admits no null, or its readers place a null last | no | omn-dev-1-implement |

## Residual Risk

- Accepted risk: None identified; no role has accepted a finding of this package.
- Unmitigated risk: admission reads headroom for the booking month before the provider call and the operation is booked after it, so a call spanning a month end is admitted against one month and charged to the next, on one clock and only across a boundary; the provider path stays unexecuted end to end; re-applying the sixth resource while gate writers are in flight could set the order sequence below a position an uncommitted transition holds, so the resource must be applied before any process starts; two transitions of one item version with an identical caller instant still meet the delivered key on the instant and surface as an unnamed uniqueness refusal; audited and metered transactions serialise on the chain head and keep month readings not final longer under sustained traffic; a late operation is booked into the next month; the single-reader scan cannot see a channel key built at run time.
- Monitoring required: the count of gate transitions out of one recorded state per item version, expected one; the difference between each company report line and the sum of its channel lines, expected exactly zero; deadlocks reported by the datastore under concurrent audited and metered writes, expected zero; alerts whose period differs from the month of the admission read, once a provider path exists.

## Verdict

- Decision: approve-with-corrections
- Rationale: Test evidence was reviewed and reproduced, no critical or high finding is open, the declared scope was examined, and one low finding is open, so the sixth row of the adjudication table matches first and yields approve-with-corrections at complete.
- Blocking findings outstanding: None identified.
- Readiness recommendation: This agent recommends that the gate owner let the change progress, with `CR-007` carried as a non-blocking improvement and the residual risks above stated in the release record; the decision belongs to the gate owner named in the workflow gate matrix.

## Open Questions

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| `Q-001` | Which order defines an item version's latest gate state? Answered by the orchestrator's ruling: the datastore's order, a position assigned under the item-row hold, never a caller instant; applied at cea41d3. | no | architect | `F-001`, `CR-001` |
| `Q-002` | Is the probe evidence produced by the repository's test command over a throwaway copy admitted? Answered by the orchestrator: yes. | no | omn-tech-lead | `F-001`, `F-002` |
