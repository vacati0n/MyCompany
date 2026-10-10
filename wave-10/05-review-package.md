```yaml
reviewPackage:
  packageId: RP-2026-0011
  reviewReference: MC-11, the company's own voice, first at head 3215cc0 and re-verified at correction cycle 1, head 1ed872a, on branch feature/mc-11-wave-10-the-company-s-own in the feature worktree .worktrees/mc-11-feature, the seven implementation commits 4d15e2c to 3215cc0 over the decision-record head 89c4cb4 and the correction commit 1ed872a (base main 85273a3), quality-review phase of run-dfcae1756c32
  sourceInputs:
    - type: code-diff
      reference: git diff 89c4cb4..3215cc0 (52 files); the full source of the narration rules, the narration domain, the produce service, the media tool, the installation verifier, the recording registrar, the plan, the host production commands, the tenth schema resource and the production adapters' diff
    - type: test-evidence
      reference: runs/run-dfcae1756c32/states/implementation/artifacts/implementation-report.md (IR-2026-0011, read in full), and the suites, host commands, media-tool measurements and datastore reads executed in this review, listed under Test Adequacy Assessment
    - type: design-reference
      reference: runs/run-dfcae1756c32/states/solution-design-and-risk-assessment/artifacts/technical-design.md (the decision paragraphs for D-001 to D-008 and the measurement and assertion paragraphs), architecture-decision-record-D-006.md, and the nineteen verification tasks T-035 to T-053 of runs/run-dfcae1756c32/states/execution-planning/artifacts/execution-plan.md
    - type: standards-checklist
      reference: the Scope, Planning and Design Gate rulings and the owner's decisions of 2026-10-10 as restated by the orchestrator, the twelve measured framework defects, and the review hunt order of the briefing
    - type: build-inputs
      reference: config/production-settings.sample.json, wave-10/owner-guide-own-voice.md and db/README.md at 3215cc0, and the orchestrator's real-model run log and output (read and probed only)
  producedBy: omn-dev-2-reviewer
  agentVersion: 1.1.0
  schemaVersion: 1.0.0
  status: complete
  verdict: approve
  inputDigest: sha256:c9c7a0e1cc60114088ac45b33ba4aaca
  contextDigest: sha256:7c56e10872c3a10df579c4c56767bfbe
```

## Metadata

- Review ID: RP-2026-0011
- Reviewer: omn-dev-2-reviewer
- Change under review: MC-11 at 1ed872a: correction cycle 1 (commit 1ed872a, answering CR-001, CR-002, CR-004 and CR-005, and the orchestrator's no-change ruling on CR-003) over the seven implementation commits 4d15e2c, 760779d, 0a5ad26, 8653e9a, 4927854, 2a256b0 and 3215cc0 over the orchestrator's decision-record commits 0322864, 4bc0029 and 89c4cb4; version 1.7.0; tenth resource db/010-own-voice.sql
- Review date: 2026-10-10

## Review Scope

- In scope: The delivered change against the accepted design and every gate ruling, hunted in the briefing's order: the source rule and the vendor bar over every entry point; the bytes-and-hashes round trip for recordings and the model; decoded measurement and every media-tool invocation on the narration path; every timeout, hold and clock; the process and file boundary; money and labels; the tenth resource over a company-shaped store and the next item version; publication; the owner guide and the settings sample run as written; the implementer's six standing objections; the nineteen verification tasks T-035 to T-053.
- Out of scope: The real voice model was never run by this review (forbidden; its acceptance of the isolated interpreter and argument list is judged from the orchestrator's run log and the files it produced); the company store mediacompany was never connected, so the tenth resource and the own run there are judged from the company-shaped integration test and a demonstration-store version-2-then-version-3 run; the implementer's defect reintroductions (re-running them would modify repository source); a line-by-line read of the item-material loader, the timeline rule and the management composers outside their diffs, examined through their executed tests; acceptance validation, which belongs to omn-qa.
- Evidence reviewed: The implementation report in full, and its correction-cycle update; the full diff 3215cc0..1ed872a (five files) and the whole InstallationVerifier.cs at 1ed872a, every file read listed; the diff 89c4cb4..3215cc0 and the full source of NarrationRules.cs, Narration.cs, ProduceItemService.cs, ExternalMediaTool.cs, InstallationVerifier.cs, RecordingRegistrar.cs, ProductionPlan.cs, ProductionCommands.cs, 010-own-voice.sql, the settings sample and the owner guide; the diffs of ProductionRules.cs, Program.cs, SchemaInstaller.cs, NpgsqlProductionAdapters.cs, VendorCalls.cs, ManagementComposers.cs, ArtifactWriter.cs, ProductionIntegrationTests.cs and db/README.md; the company-shaped tenth-resource test and the company-designation test; the design's decision paragraphs D-001 to D-008 and record D-006; the orchestrator's plan-only and own logs of the real-model run; every command result listed under Test Adequacy Assessment.

## Findings

| ID | Severity | Category | Location | Requirement | Finding | Correction Request | Status |
|---|---|---|---|---|---|---|---|
| `F-001` | medium | correctness | src/MediaCompany.Production/InstallationVerifier.cs:384-421 and 428-453 | Design decision D-006 (values read from the very bytes hashed); the Planning Gate ruling that every recorded hash is over the bytes actually loaded | Each installed-files record is hashed from one read (lines 112-115) and then read from disk a second time to list the entries verified (line 389) and the metadata hash expected (line 439), so the entry list is parsed from bytes other than the bytes whose hash matched; a record replaced between the two reads would have its forged entries verified while the record reads as matching. Resolved at 1ed872a: every file both hashed and parsed is read once and parsed from the bytes hashed (pyvenv.cfg, the voice configuration, the model card, the runtime record and the six dependency records); METADATA is read once, hashed against the value in the hashed record bytes and parsed from the same bytes; the base interpreter, the model, the interpreter and each record entry's target are hashed only; no file is read twice; a new test replaces the record between hash and parse and is refused | `CR-001` | resolved |
| `F-002` | low | correctness | config/production-settings.sample.json:17 with wave-10/owner-guide-own-voice.md:115 | Plan task T-047 and the briefing: every documented command works as written; scope item S-012 | The sample leaves narrationVoice null and does not mark it as a value to set, while the guide lists fake mode as runnable in a demonstration store; a copy made as the sample instructs refuses fake mode with exit 2 (reproduced: "no narration voice is configured"). Resolved at 1ed872a: narrationVoice is a <...> value read as unset, the about text and the guide's fake row say fake mode needs it; reproduced: the sample's placeholder refuses fake mode with exit 2, a set voice renders (exit 0, 14 operations booked 0.00 USD, the delivered fake video sha256 396692af...1679) | `CR-002` | resolved |
| `F-003` | low | standards | config/production-settings.sample.json:23-43 | Planning Gate ruling on where the expected hashes live: in the run's settings file outside the repository | The repository sample carries this machine's installation paths and the orchestrator's first-hand expected hashes (the implementer's objection 1); no code reads the sample and every value fails closed, so there is no behavioural consequence, but the ruled placement of the trust anchor is not what was delivered. Accepted at correction cycle 1 by the orchestrator's ruling (no change): the hashes are public in the owner's decision record, the trust anchor stays the run's settings outside the repository, and the sample's about text and the guide now say to check each value against the decision record | `CR-003` | accepted-risk |
| `F-004` | low | maintainability | src/MediaCompany.Host/ProductionCommands.cs:476-505 | Clean-architecture checklist (S01) necessity ladder, existence question; design decision D-002 keeps only the vendor adapter and the service's vendor path | ReadinessAsync has no caller after the host refuses metered mode before composing anything (deviation V-007) and no test covers it; it reads the admission ledger and credential variable names for a path the vendor bar makes unreachable. Resolved at 1ed872a: ReadinessAsync is removed; the build and the 934 tests pass without it | `CR-004` | resolved |
| `F-005` | low | correctness | wave-10/owner-guide-own-voice.md:109-141 | Plan task T-047 and scope item S-012: the guide's commands per mode and store work as written | The guide names the demonstration database for plan-only and own runs and recommends the real-model run there, but never states install and prepare --designation demonstration; on a fresh or test-dropped demonstration store every produce command it lists refuses ("records no designation"). Resolved at 1ed872a: the guide states install and prepare --designation demonstration before any produce there, and that the live suite drops the demonstration schema; both steps reproduced (exit 0 and 0) | `CR-005` | resolved |

## Severity Summary

- Critical: 0
- High: 0
- Medium: 1
- Low: 4

## Standards and Architecture Conformance

- Coding standards: The .NET engineering, error-handling and logging playbooks applied to every diff read: argument lists, never shell lines; every refusal names its cause; process error lines truncated at 200; no swallowed exception on the narration path. Conforms, apart from F-004.
- Architecture rules: One process starter and one file writer, held by the solution build's architecture suite (88 pass, the 80 delivered kept); the own composition builds no capability boundary, broker or network client (ProductionCommands.cs:337-343); the vendor entry below the rule is internal and visible only to the two test assemblies (AssemblyInfo.cs); the source rule reads no setting. Vendor-yielding paths enumerated and closed: produce metered (host refusal before composition, line 285), own and plan-only (the rule never yields the vendor), fake (the rule yields the fake only on a demonstration store and the guard refuses it elsewhere), register-recording and verify-model (no narration), the service's RunAsync in metered mode (refused selection), RunBelowTheRuleAsync (no production caller). Conforms.
- Security criteria: The security playbook (S09): no credential read on the own path; the vendor's error text screened of every request-header value, bearer values and token-shaped runs before truncation to 200; release documents and model files refused inside the repository; the model's core files held read-only while loaded; no binary column. Conforms, apart from the round-trip gap in F-001; the local database password the guide prints is the delivered development value already in db/README.md.
- Exceptions requested: None identified.

## Test Adequacy Assessment

- Test evidence reviewed: Correction cycle 1, confirmed in this run at 1ed872a: dotnet test MediaCompany.slnx against mediacompany_demo, 934 executed, 934 passed, 0 failed (domain 48, deterministic 449, capability 90, architecture 88, production 32, persistence 227); dotnet run verify-model as the guide writes it over a copy of the sample naming the real installation, exit 0, 1,832 entries verified, 863 without a hash, no process started; install 0, prepare demonstration 0, fake with the sample's placeholder 2, fake with a voice set 0. First pass, Confirmed in this run: dotnet test MediaCompany.slnx against mediacompany_demo at 3215cc0, 933 executed, 933 passed, 0 failed (domain 48, deterministic 449, capability 90, architecture 88, production 31, persistence 227). The built host against mediacompany_demo: install 0; a second full install 2; prepare demonstration 0 (26 rows); full install over the designated store 2 naming install --from 10; install --from 10 0; install --from 1 2; register-recording of 13 test-rendered 24-bit 48 kHz stereo tones with a fixture release 0; plan-only 0 naming Recording per part; metered 2 naming the decision of 2026-10-10; fake 2 (F-002); own 0: version 2, a 1,610,943-byte video sha256 633b6797...49af9, h264 plus aac, 0 decode error lines, narration 2,864,160 decoded samples equal to the 13 parts' sum, registered, stored and run-part hashes equal, part 1 decoding sample-identical to its source, 0 operations; a stored copy changed after registration, then plan-only 2 and own 2 naming the beat and both hashes; an incomplete-release registration, then plan-only 2 naming each absent field (deviation V-008); the model stand-in over a fixture installation: verify-model 0, plan-only 0, own 0 opening version 3 after version 2, a 7,946,847-byte video sha256 590da67d...22ed1, 0 decode errors, 12,777,027 decoded narration samples equal to the parts' sum, provenance with the metadata-read name and licence, the configured licence labelled CONFIGURED, the argument list and not observed to repeat, 0 operations; dotnet run verify-model and plan-only as the guide writes them, over a copy of the sample naming the real installation, 0 and 0, 1,832 entries verified, 863 without a hash, no process started. The orchestrator's real-model video and narration probed and decoded here: sha256 cd406a0c...dfda5b, 634.233 s container, AAC stream 634.218 s, 0 decode error lines, narration 13,984,512 decoded samples equal to the parts' sum, -15.6 LUFS. Claimed and not confirmed here: the baseline 857 at 89c4cb4, the no-store run of 753 plus 174 skipped, and the eleven defect reintroductions.
- Coverage of changed behavior: Every changed behaviour is exercised by an executed check: the rule over its full input product, the vendor bar and the fake bar; registration and every refusal; both own sources end to end; a replaced recording, a model swap, the part bound and a mid-run registration; decoded measurement against a lying header; the exact and bit-exact join; the screened error; the labels and the total line; the tenth resource over a company-shaped store with a failed metered version and its USD 0.007695 operation, applied alone and re-applied; the commands' exit codes. The two vendor-path tests are rewritten below the rule as the Planning Gate ruled, every assertion kept (verified against the diff).
- Gaps requiring new tests: The record re-read of F-001 is now covered (ARecordReplacedBetweenItsHashAndItsParseIsVerifiedFromTheBytesHashed); no test runs an own production end to end in a store designated company (only plan-only there; this review's version-3-after-version-2 run was in a demonstration store), handed to omn-qa.

## Correction Requests

| ID | Addresses | Required change | Blocking | Owner |
|---|---|---|---|---|
| `CR-001` | `F-001` | The record entries verified and the metadata hash expected are read from the same bytes whose SHA-256 matched the configured value, with a check that fails when they are not (closed at 1ed872a) | no | omn-dev-1-implement |
| `CR-002` | `F-002` | The sample and the guide agree on fake mode: either the sample marks narrationVoice as a value to set for fake mode, or the guide states that fake mode needs it (closed at 1ed872a) | no | omn-dev-1-implement |
| `CR-003` | `F-003` | The placement of the expected hashes follows the orchestrator's answer to Q-001: either they leave the repository sample, or the ruling is recorded as amended (closed by the orchestrator's no-change ruling of correction cycle 1) | no | omn-orchestrator |
| `CR-004` | `F-004` | ReadinessAsync either regains a caller with a covering test or is removed with the host's unreachable metered composition (closed at 1ed872a) | no | omn-dev-1-implement |
| `CR-005` | `F-005` | The guide states the demonstration store's install and prepare steps before any produce command run there, and that the live test suite drops the demonstration schema (closed at 1ed872a) | no | omn-dev-1-implement |

## Residual Risk

- Accepted risk: The record entries of the runtime and its dependencies are verified before the first part and after the last, not held while each part loads, as design decision D-006 accepted at the Design Gate (owner omn-tech-lead). The expected hashes kept in the repository sample (F-003), accepted by the orchestrator. The base interpreter's own libraries and standard library and the 863 hashless entries load unverified (Q-002), ruled a recorded residual risk by the orchestrator, carried as known issue R-002 of the implementation report and stated in the guide's section on what verification does not cover.
- Unmitigated risk: The rendered audio stream is AAC-encoded and can end up to one video frame earlier than the narration (a 48 kHz fixture lost 22 ms; the real run lost none), the delivered basis the Planning Gate kept. A registration superseded by a later one with an incomplete release falls back to the model with only a passed-over line. A demonstration store reused for another run versions from 2 again, so a shared output root can meet an already promoted file.
- Monitoring required: The company-store own run's plan must name InHouseModel for all 13 parts and the run must open version 3; its booked spend must stay USD 0.00 and the cap's counted total USD 0.007695; the provenance's argument list and repeatability statement are read back from the store after the run.

## Verdict

- Decision: approve
- Rationale: The Stage 8 table's row 'no finding is open' applies after correction cycle 1: F-001, F-002, F-004 and F-005 are resolved and confirmed by execution, F-003 is accepted by the orchestrator's recorded ruling, test evidence was reviewed and reproduced at 1ed872a, and the whole scope was examined or excluded with its reason.
- Blocking findings outstanding: None identified.
- Readiness recommendation: Recommended to the Review Gate owner as ready: no finding is open and no question blocks; the company-store run's monitoring items above remain the orchestrator's. This is a recommendation; the gate decision belongs to the Review Gate's owner, omn-qa as second owner for evidence this agent produced.

## Open Questions

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| `Q-001` | Does the repository's settings sample keep the installation's paths and expected hashes, contrary to the ruling that they live only in the run's settings outside the repository? Answered at correction cycle 1 by the orchestrator: no change; the trust anchor stays the run's settings outside the repository. | no | omn-orchestrator | `F-003`, `CR-003` |
| `Q-002` | Should the verified set extend to the base interpreter's runtime library and standard library and to the record entries listed without a hash, given that the hashed base python.exe is a launcher? Answered at correction cycle 1: recorded residual risk (known issue R-002), stated in the guide; no extension now. | no | omn-tech-lead | Residual Risk |
