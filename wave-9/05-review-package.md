```yaml
reviewPackage:
  packageId: RP-2026-0010
  reviewReference: MC-10, the production capability, re-verified at head e2a4573 on branch feature/mc-10-wave-9-the-production-capability-item in the feature worktree .worktrees/mc-10-feature, correction cycle 2 (df7809e, 7330d32, a154c90, c210e64, 68d55e1, e2a4573) over c312eb4 (owner's narration-voice decision record) and the first-pass head e2b6715, quality-review phase of run-7ae81c0de400
  sourceInputs:
    - type: implementation-report
      reference: runs/run-7ae81c0de400/states/implementation/artifacts/implementation-report.md (IR-2026-0010, read in full in the first pass and its evidence counts re-read after correction cycle 2)
    - type: feature-request
      reference: tasks/MC-10/input.md, as supplied in the invocation envelope
    - type: design-reference
      reference: runs/run-7ae81c0de400/task-context.yaml (scope criteria, design decisions and gate rulings), runs/run-7ae81c0de400/states/scope-and-acceptance/artifacts/scope-definition.md (acceptance rows read), runs/run-7ae81c0de400/states/solution-design-and-risk-assessment/artifacts/architecture-decision-record-D-005.md, and the verification tasks owned by this agent in runs/run-7ae81c0de400/states/execution-planning/artifacts/execution-plan.md
    - type: standards-checklist
      reference: the Scope, Planning and Design Gate rulings, the owner's decisions of 2026-10-09 and 2026-10-10, the orchestrator's rulings on this package's correction requests and questions, and the twelve measured framework defects, as restated by the orchestrator
    - type: code-diff
      reference: git diff 969c124..e2b6715 (79 files) in the first pass and git diff c312eb4..e2a4573 (26 files) in full in this pass
    - type: test-evidence
      reference: the builds, suites, host commands, media-tool measurements and datastore reads executed in both passes, listed under Test Adequacy Assessment
  producedBy: omn-dev-2-reviewer
  agentVersion: 1.1.0
  schemaVersion: 1.0.0
  status: complete
  verdict: approve-with-corrections
  inputDigest: sha256:71b302f2f54bc2fc5d2ffbb9e57fb4ad
  contextDigest: sha256:7c56e10872c3a10df579c4c56767bfbe
```

## Metadata

- Review ID: RP-2026-0010
- Reviewer: omn-dev-2-reviewer
- Change under review: MC-10 at e2a4573: the fifteen implementation commits e91da78 to e2b6715, the owner's decision-record commit c312eb4, and correction cycle 2 (df7809e, 7330d32, a154c90, c210e64, 68d55e1, e2a4573) answering CR-001 to CR-010
- Review date: 2026-10-10

## Review Scope

- In scope: The delivered change against the accepted design and every ruling, hunted in the briefing's order in the first pass (money round trip and item cap, fake versus metered, the two-clock class, the process and file boundary, the vendor adapters, nothing publishes, the owner guide, the implementer's standing objections, the twenty-three plan verification tasks owned by this agent); in this pass, each correction verified independently against its request and the orchestrator's rulings, every host reader re-audited over a demonstration store, and the corrections hunted for regressions, the money round trip first (recorder billed-kind rule, reservation reconciliation by stated bookings, the fail-closed cap, the cancellation booking, the catch-all stage ends).
- Out of scope: Real vendor endpoints, shapes, prices and terms (first-hand verification is the orchestrator's precondition of the go and no phase may reach a vendor); host metered mode against the company store (never run; refusal reproduced on the demonstration store); the implementer's defect reintroductions (re-running them would modify repository source); a line-by-line read of the item material, the produced-item reader and the deterministic split and timeline rules, examined through their executed tests; acceptance validation against the criteria, which belongs to omn-qa.
- Evidence reviewed: First pass, as recorded at e2b6715: the implementation report in full, the full source or full diff of the gateway, the five vendor-path files, the produce service, the media tool, the writer, the settings and plan, the host production commands, the broker factory, the ninth schema resource, the two configuration files, the owner guide, the cost controller, the route resolver, the item cap, the admission ledger, the recorder and the management composers. This pass: the full diff c312eb4..e2a4573 of all 26 files (gateway, factory, rule catalogue, route resolver, reporting service, item cap, host production commands and entry point, admission ledger, company record reader, recorder, production adapters, writer, media tool, produce service, settings, sample settings, owner guide and the seven test files), and every execution listed under Test Adequacy Assessment.

## Findings

| ID | Severity | Category | Location | Requirement | Finding | Correction Request | Status |
|---|---|---|---|---|---|---|---|
| `F-001` | high | correctness | src/MediaCompany.Deterministic/Services/ReportingService.cs:57-110 (the report command, src/MediaCompany.Host/Program.cs report case) | Scope criterion A-017; designation decision D-005 (every reader labels demonstration figures); the orchestrator's correction-cycle ruling on fake figures read as observed | At e2b6715 the report command printed a demonstration store's fake spend as observed zero. Resolved at e2a4573: the report service reads the designation and every observed figure of a demonstration store renders as a demonstration figure with its case word; this pass's report over the fake-run store printed all seven measures, the cost variance included, as demonstration, and the registers command now names the designation first. | `CR-001` | resolved |
| `F-002` | high | correctness | src/MediaCompany.Persistence/NpgsqlOperationRecorder.cs:131-136 with src/MediaCompany.Capability/Providers/ImageGenerationAdapter.cs:99-101 | The owner's unit-kind pricing rule of 2026-10-09; scope criteria A-039 and A-044; cap decision D-006 | At e2b6715 an unbilled consumed kind left a measured image booking cost-unstated and its reservation reconciled out of the counted total. Resolved at e2a4573: the recorder costs only the kinds the model is recorded as billed by, an unbilled kind costing nothing, and a reservation stays counting at its worst case until a booking with a stated cost reconciles it; proven live by the measured image booking and the cost-unstated reservation tests, which pass in this pass's run. | `CR-002` | resolved |
| `F-003` | medium | correctness | src/MediaCompany.Capability/Providers/VendorCalls.cs:61 and src/MediaCompany.Capability/CapabilityGateway.cs:477 | Scope ruling that a possibly charged attempt books its admitted worst case; Design Gate ruling that every timeout and hold ends in a named outcome; scope criterion A-039 | At e2b6715 a cancellation during the vendor call left no operation record. Resolved at e2a4573: the gateway turns the cancellation into a failed attempt with an unknown charge, booked at its worst case labelled estimate on a non-cancellable recording, and produce records the stage failed and exits 6; covered at the boundary and live. | `CR-003` | resolved |
| `F-004` | medium | correctness | src/MediaCompany.Deterministic/Routing/RouteResolver.cs:500-509 and src/MediaCompany.Domain/Accounting/ItemCap.cs:33-37 | Cap decision D-006; the interim owner reading that one unstated cost refuses | Resolved at e2a4573: an operation of the item with an unstated cost and no reservation bounding it closes the cap before any provider route is admitted. Proven in memory and by a live test; live, the same-month cost controller refuses first, which is the stricter of the two and was accepted. | `CR-004` | resolved |
| `F-005` | medium | correctness | src/MediaCompany.Production/ProduceItemService.cs:205-207 and :307, src/MediaCompany.Host/ProductionCommands.cs:207, src/MediaCompany.Production/ExternalMediaTool.cs:185 | Produce-command decision (named ends with documented exit codes); scope ruling that a stage that cannot finish is recorded naming why | Resolved at e2a4573: after the version opens, every failure records the stage in progress failed on a non-cancellable write and ends with a documented code. Confirmed by execution in this pass: a renderer setting naming a non-executable file ended version 3 with Design recorded Failed naming the start failure, and exit 3. | `CR-005` | resolved |
| `F-006` | low | security | src/MediaCompany.Host/ProductionCommands.cs:70-74 and :282 | Security criteria of the secure-engineering playbook | Resolved at e2a4573: an endpoint that is not an absolute https address is refused by name, and the metered client follows no redirect, so a redirect is a non-success status booked at its worst case; covered by a stub test and a settings test. | `CR-006` | resolved |
| `F-007` | low | correctness | src/MediaCompany.Production/ArtifactWriter.cs:139-161 | Planning Gate ruling 7 | Resolved at e2a4573: every existing path component is expanded to its long name before containment is decided, and an unexpandable path is refused; a test spelling the output root short and the repository long is refused. | `CR-007` | resolved |
| `F-008` | low | standards | src/MediaCompany.Production/ExternalMediaTool.cs:36-85 | Design Gate bound enumeration | Resolved by the orchestrator's ruling as tech lead: the version check borrows the probe bound and the narration join the decode bound; both are now stated in code, in the printed plan's bounds line and in the owner guide, with no new setting. | `CR-008` | resolved |
| `F-009` | low | test-adequacy | tests/MediaCompany.Capability.Tests/VendorPathTests.cs:84-106 and tests/MediaCompany.Persistence.Tests/ProductionIntegrationTests.cs:358 | Plan verification tasks T-054, T-061 and T-063 | Resolved at e2a4573: a stalled stand-in under the real client timeout, the process clock a day ahead and a day behind, a cancellation mid-call, the measured image booking in the store and an unstated booking under a cap are each executed checks, all passing in this pass's run. | `CR-009` | resolved |
| `F-010` | low | standards | wave-9/owner-guide-metered-run.md:78-116 | Scope criterion A-052 and plan task T-057 | Resolved at e2a4573: the guide states that the demonstration provider is emptied and an https endpoint set before metered mode, that the orchestrator sets the owner's chosen voice, how a re-fetched price enters the company store through the preparation, the borrowed bounds and every exit code. | `CR-010` | resolved |
| `F-011` | low | correctness | src/MediaCompany.Deterministic/Analytics/ManagementComposers.cs:564 | Scope criterion A-017 and designation decision D-005 (no demonstration figure reads as observed in any reader) | Found in this pass's re-audit, present since e2b6715: in a demonstration store each controller-decision reading line of the weekly report embeds its booked spend in the line's label through the delivered description, so the label reads "booked spend observed zero USD" although the line's own figure is tagged demonstration. The demonstration tag on the figure limits the consequence to wording. | `CR-011` | open |

## Severity Summary

- Critical: 0
- High: 2
- Medium: 3
- Low: 6

## Standards and Architecture Conformance

- Coding standards: The .NET playbook and the clean-architecture checklist were applied to every file read in both passes; the corrections keep one build for every mode, add no default in code, write stage ends on a non-cancellable token and name every exit code; conformant except the label wording in F-011.
- Architecture rules: One process starter and one file writer stay pinned across all eight production assemblies (architecture suite 80 of 80); the metered member builds vendor adapters only over a no-redirect client and the demonstration member builds fakes only; the report service takes the designation reader as an optional port, keeping the rule-determined assembly free of persistence. The stage-handler deviation stays accepted for this wave as a known issue by the orchestrator's ruling.
- Security criteria: The secure-engineering playbook was applied: credentials stay per-use handles attached only to the outgoing message, endpoints are https only, redirects are not followed, and no secret appears in any output the suite checks or in the owner guide; conformant.
- Exceptions requested: None identified. The implementer's notes were judged: CR-007's explicit expansion is kept and tested; CR-004's fail-closed rule is proven in memory and live, with the cost controller refusing first in the same month; the transaction observer now counts client transactions only, which does not weaken the check, because every transaction the application opens is a client transaction and only the datastore's own maintenance workers are excluded, and on a failure it names what it saw; the report command now prints a case word before every figure in every store, a format change to a delivered console output with no machine consumer in the repository.

## Test Adequacy Assessment

- Test evidence reviewed: This pass, at e2a4573: dotnet build MediaCompany.slnx (0 errors, architecture suite in the build); dotnet test MediaCompany.slnx against mediacompany_demo only, 857 executed and 857 passed (domain 48, deterministic 423, capability 84, architecture 80, production 20, persistence 202), equal to the implementation report's claim; dotnet test MediaCompany.slnx --no-build without the variable, 688 passed and 163 skipped, 0 failed; the built host on a fresh mediacompany_demo: install, fake refused before preparation, prepare demonstration, prepare company refused, metered refused before any call naming four preconditions, plan-only, fake (exit 0, 14 operations); the rendered file re-measured: 9,185,227 bytes, sha256 396692af...1679 equal to both earlier runs, h264 1920x1080 at 30/1 with 19,973 frames read and aac, container 665.766667 s against audio 665.760 s, full decode with zero error bytes; 62 of 62 recorded artifact hashes and lengths recomputed equal; 14 operations at zero with stated costs, 14 reservations totalling 0.16644 USD all reconciled by stated bookings, 0 benchmark observations; check, report, weekly, dashboard and registers run over the demonstration store and audited line by line for observed figures; a fake run with the renderer setting naming a non-executable file ended with Design recorded Failed and exit 3; the company store read at 0 tables. First pass at e2b6715: 839 of 839 live and 675 passed with 158 skipped without a store, with the same file hash.
- Coverage of changed behavior: Every behavior the corrections changed is exercised by an executed check: the report and registers designation labels, the billed-kind booking and the measured image booking in the store, the reservation counting until a stated booking, the fail-closed cap, the cancellation booking and exit code, the stalled call under the real client bound, the redirect refusal and https-only endpoints, short-name containment, clock skew both ways, and the unbudgeted-channel rule under the ruling; the catch-all stage end was confirmed here by execution.
- Gaps requiring new tests: For omn-qa: a check that no reader label of a demonstration store embeds an observed-case description (F-011); an executed check of an unanticipated failure before the carried stages, which this pass confirmed by hand only.

## Correction Requests

| ID | Addresses | Required change | Blocking | Owner |
|---|---|---|---|---|
| `CR-001` | `F-001` | Closed at e2a4573 and confirmed: every figure every console reader composes from a store designated demonstration reads as a demonstration figure, proven over a fake-run store | yes | omn-dev-1-implement |
| `CR-002` | `F-002` | Closed at e2a4573 and confirmed: a measured booking states its cost when every billed kind is priced, and no booking of a capped item leaves the counted total below its admitted worst case | yes | omn-dev-1-implement |
| `CR-003` | `F-003` | Closed at e2a4573 and confirmed: an attempt interrupted by cancellation is booked at its worst case and the run records the stage end with a documented code | no | omn-dev-1-implement |
| `CR-004` | `F-004` | Closed at e2a4573 and confirmed: item-cap admission fails closed while an unbounded unstated operation exists | no | omn-dev-1-implement |
| `CR-005` | `F-005` | Closed at e2a4573 and confirmed by execution: every failure after the version opens ends with a recorded stage outcome and a documented code | no | omn-dev-1-implement |
| `CR-006` | `F-006` | Closed at e2a4573 and confirmed: https-only endpoints, no redirect followed | no | omn-dev-1-implement |
| `CR-007` | `F-007` | Closed at e2a4573 and confirmed: containment holds under short-name spellings | no | omn-dev-1-implement |
| `CR-008` | `F-008` | Closed by the tech-lead ruling and confirmed in code, printed plan and guide: the version check under the probe bound, the narration join under the decode bound | no | omn-tech-lead |
| `CR-009` | `F-009` | Closed at e2a4573 and confirmed: each named verification exists as a passing executed check | no | omn-dev-1-implement |
| `CR-010` | `F-010` | Closed at e2a4573 and confirmed: the guide's instructions reach a metered run as written and name the price path | no | omn-dev-1-implement |
| `CR-011` | `F-011` | In a store designated demonstration, no reader label embeds a figure described as observed; every embedded booked-spend description reads as demonstration, as the line figures already do | no | omn-dev-1-implement |

## Residual Risk

- Accepted risk: The stage-handler deviation and the zero rating and capacity of the narration route, accepted for this wave as known issues by the orchestrator's ruling; the report command's case-word format change in every store.
- Unmitigated risk: F-011's wording in demonstration stores; the vendor shape, price per character and terms positions remain unverified first hand and the real narration's fit inside the 60-second call bound is unproven (the orchestrator's preconditions of the go); a cancellation arriving before the request leaves the process is booked at worst case though nothing was sent, a conservative overstatement labelled estimate; a never-reconciled reservation counts at worst case with no settlement path (an open owner question).
- Monitoring required: On the metered run, compare each narration operation's booked character units and amount with its reservation and the vendor's own billing record, read the cap line after every part, confirm the company store's report labels figures as observed only there, and after any interrupted run list the item's open reservations.

## Verdict

- Decision: approve-with-corrections
- Rationale: Test evidence was reviewed and reproduced, all ten earlier findings are resolved and confirmed by execution, and one low finding remains open, so the Stage 8 row for open medium or low findings yields approve-with-corrections at complete.
- Blocking findings outstanding: None identified.
- Readiness recommendation: Recommended to the Review Gate owner as ready: no critical or high finding is open, CR-011 is non-blocking and may be carried with its owner named, and the remaining preconditions of the owner's metered go are the orchestrator's first-hand verifications. This is a recommendation; the gate decision belongs to its owner.

## Open Questions

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| `Q-001` | Answered by the orchestrator's ruling: the register records the owner's no-budget-channel decision as decided, and the unbudgeted-channel rule keeps issuing where metered work would still be refused, abstaining only where every item of the channel has a recorded cap; implemented and observed abstaining over the one capped item. | no | omn-product-owner | The management rule set |
| `Q-002` | Accepted for this wave as a known issue by the orchestrator's ruling: the four producing stages stay service steps rather than stage-handler implementations. | no | architect | Architecture rules conformance |
| `Q-003` | Accepted for this wave as a known issue by the orchestrator's ruling: the narration route records a rating and context capacity of zero, indistinguishable in the store from a real rating of zero. | no | omn-tech-lead | Correctness of route records |
