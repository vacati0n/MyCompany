```yaml
design:
  designId: DESIGN-2026-0011
  changeReference: MC-11
  sourceInputs:
    - type: change-request
      reference: tasks/MC-11/input.md
    - type: execution-plan
      reference: runs/run-dfcae1756c32/states/execution-planning/artifacts/execution-plan.md
  producedBy: architect
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  decisionRecords: [D-001, D-002, D-003, D-004, D-005, D-006, D-007, D-008, D-009, D-010, D-011]
  consumesPlan: runs/run-dfcae1756c32/states/execution-planning/artifacts/execution-plan.md
  inputDigest: sha256:2cd04b8d993ad4cb47094c2e3cc9807e
  contextDigest: sha256:f65fadccf1c3d16e5951ed9cbe601cbb
```

## Metadata

- Feature or Change ID: MC-11, the company's own voice: narration of a production from a human recording the company holds, or from the owner-approved open-licensed speech model run on the company's machine, so item 001 is produced with a real voice and no vendor
- Author: architect
- Reviewers: omn-tech-lead, omn-dev-2-reviewer
- Last Updated: at emission in the solution-design-and-risk-assessment phase of run-dfcae1756c32, first emission; no prior identifier exists to preserve
- Upstream: the scope definition (digest sha256:e4b36a6957db10061b8d418e1a4416b7) and the execution plan (digest sha256:482e24c47608e827cf70a8f1f0419f9a) of this run. The plan's statement register S-001 to S-018 is adopted as this design's statement register in the plan's order, cited and not restated. The scope's acceptance criteria are cited as "criterion" with their ordinal, and the scope's, the plan's and the gates' own decisions, questions and risks descriptively, because their identifier prefixes collide with this package's registers.
- Repository rescan: the task context names no relevant module or file, so a read-only rescan was made of the source, test, schema and configuration roots and the item 001 package in the feature worktree D:/Project/MyCompany/.worktrees/mc-11-feature at commit 89c4cb4, and of the installed runtime and voice files outside the repository (named in F-027 to F-032), read only. No datastore was read, no process of the model or the media tool was run, nothing was downloaded and no repository file was written.

## Objective

- Desired outcome: one produce command, in a new own-source mode, narrates every part of a production from exactly one of the company's own sources, a registered recording with a recorded release or else the owner-approved in-house model verified by hash, names that source and why for every part before anything runs, records each narration file's provenance and its duration and loudness measured from decoded audio beside the script's expectation without adjusting anything, books USD 0.00, and renders the video as before in shape; the vendor narration route stays in the build and is never chosen while the owner's decision of 2026-10-10 stands.
- Architectural objectives:
  - A narration source is one member of a closed set of four, chosen by one pure rule in the rule-determined assembly over the recorded registration, the verified model state, the mode and the store designation; no path through the rule yields the vendor. Traces to S-001, S-002.
  - The rule is re-derived before every part, and a run whose source would differ from the planned one stops before that part. Traces to S-001.
  - The in-house model runs only through the one delivered process-starting type, under its own bound on the monotonic clock, with no record, transaction or datastore command open and no network client constructed. Traces to S-003.
  - Every hash recorded for a model file or a recording is computed over the very bytes the process loads or the tool decodes, through a handle that denies replacement while the bytes are used. Traces to S-004, S-005, S-007.
  - Provenance values are read from the installed files that were hashed, never from configuration. Traces to S-004, S-005.
  - A recording enters the store only as one stored copy per beat written by the one writer, measured from that copy, under a release record whose absent fields read absent. Traces to S-006, S-007.
  - Every narration duration is decoded samples over the decoded sample rate and every loudness one named standard measure, reported beside the script's own expectation and never acted on. Traces to S-008, S-009.
  - An own-source production reaches no capability boundary, books no operation and leaves the item cap's counted total unchanged. Traces to S-010.
  - The store gains the own mode and the registration, measurement and provenance records through one additive tenth resource, applicable alone to a store holding nine. Traces to S-011, S-013.
  - The owner and the orchestrator have commands and settings for registration, verification and production with no secret and no default in code. Traces to S-012, S-013.
  - A vendor's non-success body reaches every sink screened and truncated; demonstration labels never read observed; the plan's total line states only what its price rows record. Traces to S-014, S-015, S-016.
  - Every delivered assertion and live test stays, extended and never weakened, and no quantity, target or rights position is invented. Traces to S-017, S-018.
- In scope:
  - The source set, the selection rule, the own mode and its store admission, and the vendor bar.
  - Recording registration, the model installation verification, the model run through the one starter, provenance and the repeatability record.
  - Decoded-audio measurement, the duration report and the joining rule.
  - The tenth schema resource and the installer's application of it alone.
  - The commands, the settings, the orchestrator's ordered company-store steps, the screened error body, the demonstration labels, the total line and the added boundary assertions.
- Out of scope:
  - Any vendor or metered call, any download, any run of the real model inside a phase, and any phase connection to the company store.
  - A re-admission path for the vendor narration route.
  - Any loudness, duration, sample-rate or format target, normalisation, resampling, stretch, pad or trim by the pipeline.
  - Mixing sources inside one production, whole-file recordings and automatic alignment.
  - Voice cloning, training or fine-tuning; redistribution of the runtime, its phonemiser or the voice.
  - Publication, upload and any change to the held-stage refusal; motion graphics, stock footage and music.
  - Render repeatability, and the previous wave's vendor-path request and call-size issues.

## Requirements Summary

- Functional requirements:
  - One source from a closed set of four narrates a whole production, chosen by a fixed rule and printed per part with its reason before anything runs. Traces to S-001.
  - The vendor route is never chosen while the owner's decision stands; it stays in the build. Traces to S-002.
  - With no usable recording, the verified in-house model narrates on the company's machine with no network and nothing held open. Traces to S-003.
  - Every model-produced file carries its provenance and an observed repeatability statement. Traces to S-004.
  - The model is used only when the installed files match the hashes recorded at installation. Traces to S-005.
  - One file per beat is registered with its hash, the performer and the release or their recorded absence. Traces to S-006.
  - Only the registered bytes narrate, and never without a recorded release. Traces to S-007.
  - Duration and loudness are measured from decoded audio and reported beside the script's expectation. Traces to S-008.
  - The video is assembled as before in shape, decoded end to end, its runtime measured from the file. Traces to S-009.
  - The owner has exact commands for recording, registration, verification and production. Traces to S-012.
  - The orchestrator can produce item 001 in the company store after the phases on the owner's go. Traces to S-013.
  - A vendor's non-success response records its status and screened code or message. Traces to S-014.
  - Demonstration controller-decision labels never read observed. Traces to S-015.
  - The plan's total line states what its price rows record. Traces to S-016.
- Non-functional requirements:
  - USD 0.00 metered spend, cap total unchanged, local compute and recording time unmeasured. Traces to S-010.
  - Every phase and test in the demonstration store only, with no network and no download. Traces to S-011.
  - Every wait bounded and ended under a named outcome; every recorded instant the datastore's. Traces to S-003, S-007.
  - Delivered tests and assertions kept and extended; nothing invented. Traces to S-017, S-018.
- Acceptance criteria:
  - The fifty-two criteria of the upstream scope definition and the seven plan-level criteria of the supplied execution plan are this design's acceptance intent, cited rather than restated; they bear on S-001, S-002, S-003, S-004, S-005, S-006, S-007, S-008, S-009, S-010, S-011, S-012, S-013, S-014, S-015, S-016, S-017 and S-018.
  - The task-level criteria of the fifteen design tasks the plan assigns to this agent, T-001 to T-015 as the plan numbers them, are met by sections five, six, eight and nine; this bears on S-001 and S-018.

## Current-State Assumptions and Constraints

Paths are relative to the feature worktree unless they name the installed voice folder outside the repository. Gate rulings and owner decisions reached this phase only through the dispatch briefing; those not also readable in the decision record are registered as assumptions.

### 4.1 Facts

| ID | Fact | Established by |
|---|---|---|
| F-001 | The produce service narrates each split part through the capability boundary, stores the returned content and probes it for its duration | src/MediaCompany.Production/ProduceItemService.cs:325-380 |
| F-002 | Every producing mode is refused when no narration voice is configured, and when the item has no recorded cap | src/MediaCompany.Production/ProduceItemService.cs:138-153 |
| F-003 | Narration is split at beat openings, then paragraphs, then sentences under the configured character maximum, the parts concatenating to the text exactly | src/MediaCompany.Deterministic/Production/ProductionRules.cs:166-203 |
| F-004 | The one process starter runs the renderer and the probe located by settings, as an argument list, under a per-invocation bound on a cancellation timer, terminating the process tree past it, holding no transaction | src/MediaCompany.Production/ExternalMediaTool.cs:8-19, 179-225 |
| F-005 | The probe reads stream kinds and the container's format duration field, and every part, the joined narration and the rendered video take their duration from it | src/MediaCompany.Production/ExternalMediaTool.cs:122-143; src/MediaCompany.Production/ProduceItemService.cs:341, 359, 580 |
| F-006 | The narration join re-encodes the concatenated parts to 16-bit PCM under the decode bound | src/MediaCompany.Production/ExternalMediaTool.cs:82-97 |
| F-007 | The one writer stages, flushes, promotes once without overwrite, re-reads and hashes the stored file, and refuses an output root inside, equal to or containing the repository; its path resolution and containment members are internal to the production assembly | src/MediaCompany.Production/ArtifactWriter.cs:8-53, 97-110, 147-194 |
| F-008 | The architecture suite asserts exactly one process-starting type and exactly one file-writing type across production assemblies, counting any file-stream constructor as a write site | tests/MediaCompany.Architecture.Tests/ProductionBoundaryTests.cs:21-60, 196-222 |
| F-009 | The production assembly references the domain, the ports, the rule-determined and the capability assemblies only, and the produce path reaches no publication type, both asserted | tests/MediaCompany.Architecture.Tests/ProductionBoundaryTests.cs:133-165; src/MediaCompany.Production/MediaCompany.Production.csproj |
| F-010 | The store guard admits fake mode on a demonstration store only, metered mode on the named company store only, plan-only on any designated store, and refuses an undesignated store | src/MediaCompany.Deterministic/Production/ProductionRules.cs:347-385 |
| F-011 | Production versions record a mode checked to fake or metered and a designation, write-once and datastore-stamped; artifacts record path, length, hash and a measured duration in milliseconds | db/009-production.sql:40-56, 244-288 |
| F-012 | The install command applies every ordered resource; the first creates tables without an existence guard, and the store guide states the first four cannot be re-applied | src/MediaCompany.Persistence/SchemaInstaller.cs:75-86; db/001-schema.sql:25; db/README.md:47-50 |
| F-013 | The ninth resource re-applies alone changing nothing, as a delivered test applies it | tests/MediaCompany.Persistence.Tests/ProductionIntegrationTests.cs:98-130 |
| F-014 | The settings reader requires the provider-call bound and the character maximum for every mode, and the settings hold no model member | src/MediaCompany.Host/ProductionCommands.cs:43-66; src/MediaCompany.Production/ProductionSettings.cs:9-60 |
| F-015 | Plan-only composes neither writer nor starter; fake mode composes the demonstration boundary; metered mode composes the vendor boundary after the named preconditions | src/MediaCompany.Host/ProductionCommands.cs:212-300; src/MediaCompany.Production/MeteredPreconditions.cs |
| F-016 | The plan's total line always prints configured unit prices as not verified first hand, and each price row as re-fetched first hand before any spend | src/MediaCompany.Production/ProductionPlan.cs:88-113 |
| F-017 | Price rows store a source and a verification date; the re-fetch date member is not stored and no column marks a first-hand verification | src/MediaCompany.Domain/Registry/Registry.cs:108-125; db/001-schema.sql:82-97 |
| F-018 | A vendor non-success is recorded as its status number alone; the body is read into one buffer and discarded | src/MediaCompany.Capability/Providers/VendorCalls.cs:46-50, 74-85 |
| F-019 | An attempt's failure reason reaches the availability record, the operation record and the audit entry, and through the produce service the Audio stage summary and the console | src/MediaCompany.Capability/CapabilityGateway.cs:506-512, 662-712; src/MediaCompany.Production/ProduceItemService.cs:441-448, 690 |
| F-020 | The speech, messages and image paths share the one non-success constructor and keep the outgoing message in scope when they call it | src/MediaCompany.Capability/Providers/SpeechAudioAdapter.cs:58-74; MessagesAdapter.cs:69-85; ImageGenerationAdapter.cs:56-72 |
| F-021 | Controller-reading and candidate line labels embed rendered quantity descriptions (amount, booked spend, observed quality, observed cost), which read observed zero in a demonstration store while only the line's own figure is demonstrated | src/MediaCompany.Deterministic/Analytics/ManagementComposers.cs:206-209, 560-576; wave-9/release-note.md (the review's one open low finding) |
| F-022 | Produced-item lines read the latest production version's mode text and count the item's operations across every version | src/MediaCompany.Persistence/NpgsqlCompanyRecordReader.cs:834-899; src/MediaCompany.Deterministic/Analytics/ManagementComposers.cs:165-200 |
| F-023 | Two delivered tests drive a metered production through the produce service with a stub vendor: the zero-spend end-to-end test, which also asserts plan-only prints an estimate, and the cancellation-during-a-vendor-call test | tests/MediaCompany.Persistence.Tests/ProductionIntegrationTests.cs:463-620, 653-701 |
| F-024 | No other test project uses the produce service; the vendor paths' own tests sit below it | rescan of tests for the produce service and its commands |
| F-025 | Item 001's material holds 13 beats whose openings delimit the narration; whitespace-split words per beat sum to 1,929, the longest beat 274 words, the normalised text 11,096 characters | wave-2/item-001/item-material.json; wave-2/item-001/narration.txt, counted read-only by this phase |
| F-026 | The script records the delivery rate assumed, 150 words per minute, the narration duration at that rate, 12 min 51 s, and the specified runtime 13 min 39 s; the material quotes only the specified runtime from it | wave-2/item-001/script.md:17-20; wave-2/item-001/item-material.json |
| F-027 | The runtime's command line reads input files line by line, skipping empty lines, writes one WAV file when given an output file, and plays audio when given none and a player exists | C:/Users/vuhoangcao/MediaCompanyRun/voice/venv/Lib/site-packages/piper/__main__.py, read only |
| F-028 | The command line searches data directories only when the given model path does not exist and never downloads; it parses the configuration option but loads the voice without it, so the configuration read is always the model path plus ".json" | piper/__main__.py; piper/voice.py:123-146, read only |
| F-029 | Generation reads noise scale, length scale and noise width from the voice configuration when not passed, peak-normalises each sentence by default, and defaults volume to 1 and sentence silence to 0 | piper/__main__.py; piper/voice.py:369-380, 509-520, read only |
| F-030 | The voice configuration states dataset cori, language en_GB, quality high, 22,050 Hz, one speaker, inference 0.667, 1 and 0.8; the model card states the dataset's licence as public domain and states no weights or voice licence | voice/model/en_GB-cori-high.onnx.json; voice/model/MODEL_CARD, read only |
| F-031 | The runtime's package metadata states name piper-tts, version 1.8.0, licence GPL-3.0-or-later; its RECORD lists 475 entries with SHA-256 values, the console launcher among them | voice/venv/Lib/site-packages/piper_tts-1.8.0.dist-info/METADATA and RECORD, read only |
| F-032 | The environment's configuration names a base interpreter outside the environment, which its launchers start | voice/venv/pyvenv.cfg, read only |
| F-033 | The owner's decision record holds the SHA-256 of the voice model, its configuration and the model card, and the dependency list | research/ceo-decision-record.md, the owner's decision of 2026-10-10 installing the voice |
| F-034 | The owner's decision record states 175 s for the whole narration on this CPU with default settings, different bytes between two default runs, 10 min 31 s of audio and -15.5 LUFS integrated by the media tool's R128 meter, and the choice of defaults passed explicitly | research/ceo-decision-record.md, the same decision |
| F-035 | Fakes are built only by the demonstration factory member, asserted | tests/MediaCompany.Architecture.Tests/ProductionBoundaryTests.cs:167-195; src/MediaCompany.Capability/Providers/DemonstrationProviders.cs:18 |
| F-036 | Every production record is one short transaction stamped by the datastore, and an ending is recorded on an uncancellable token | src/MediaCompany.Production/ProduceItemService.cs:213-267, 657-718 |
| F-037 | Additive port members with default implementations are a delivered precedent | src/MediaCompany.Application/Ports/PersistencePort.cs:34, 249 |
| F-038 | The production ledger offers opening a version and recording an artifact; the production reader offers package, pricing, stages and artifacts | src/MediaCompany.Application/Ports/PersistencePort.cs:119-133; src/MediaCompany.Application/Ports/ProductionPorts.cs:28-53 |
| F-039 | The beat timeline divides each beat's measured duration among its stills in whole frames | src/MediaCompany.Deterministic/Production/ProductionRules.cs:295-345 |
| F-040 | The produce result documents exit codes 0, 2, 3, 4, 5 and 6 | src/MediaCompany.Production/ProduceItemService.cs:31-47 |
| F-041 | The credential exchange's public contract attaches a credential to an outgoing message and nothing else | src/MediaCompany.Credentials/CredentialBroker.cs:70-76 |
| F-042 | The settings sample carries vendor members, a 1,500-character maximum and no model member | config/production-settings.sample.json |
| F-043 | The repository declares version 1.6.0 once, asserted by the suite | Directory.Build.props |
| F-044 | The media tool 9.0.2 and its probe are installed and located by configuration; the machine has no graphics processor | the dispatch briefing's environment facts |

### 4.2 Assumptions

| ID | Assumption | Why needed | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | The Scope Gate and Planning Gate rulings are as the briefing restates them, including the vendor bar at selection, decoded durations with the video runtime on the delivered basis, the total line wording, structural network evidence and hashes held in the run's settings | Every decision rests on them | Selection, measurement, verification and commands change | omn-orchestrator |
| A-002 | The owner's decisions of 2026-10-10 bind as recorded: own narration, no vendor again, cap total USD 0.007695 estimate, the CEO records one file per beat, no loudness or format target, the approved runtime and voice, the release form, default settings passed explicitly | The rule, provenance and registration follow them | A rights position, a target or a source rule changes | omn-product-owner |
| A-003 | The orchestrator's first-hand SHA-256 values of 2026-10-10 for the runtime's RECORD, the console launcher and the environment's interpreter launcher are as briefed | The settings carry expected values for them | Verification refuses the model, or verifies against an unmeasured value | omn-orchestrator |
| A-004 | The environment's interpreter, started in isolated mode with bytecode writing off and the runtime run as a module, loads the runtime from the environment's own packages | The model process is started that way | The first real run fails at Audio; the console launcher with a scrubbed environment replaces it | omn-orchestrator |
| A-005 | On this platform a read handle that denies write and delete sharing keeps the file from being changed, replaced or renamed while open, and does not stop a child process opening or executing it | The hash names the bytes loaded | A swap between hash and load is possible; a re-hash after the process exits detects it instead | omn-dev-1-implement |
| A-006 | The installed media tool reports, in one decode pass over an audio stream, its decoded sample count, sample rate, channel count and EBU R128 integrated loudness | Duration and loudness come from one decode | A second decode to raw samples counted by the starter supplies the count | omn-dev-1-implement |
| A-007 | Generating one part on this CPU takes no longer than the measured 175 s for the whole narration, because a part loads the same voice once and carries at most 274 of the 1,929 words | The model-run bound rests on it | A healthy part passes the bound and the stage fails | omn-orchestrator |
| A-008 | The company store at the post-phase run is as recorded on 2026-10-10: nine resources, item version 2 failed at Audio, one operation booked at USD 0.007695 estimate | The tenth resource and the cap reading meet that state | The application proof and the cap reading differ | omn-orchestrator |
| A-009 | The CEO's thirteen beat recordings will share one sample rate, channel count and sample format | The join is exact only then | A mixed set is refused before narration until the owner decides | omn-product-owner |
| A-010 | No delivered test other than those in F-023 asserts the produce service's plan-only output or the narration of a metered run, and none counts production modes | The rewrite list is complete | A further delivered test fails and joins the list | omn-dev-1-implement |
| A-011 | The live suite passes 857 and the architecture suite 80 at the Wave 9 head, as reported; the implementer re-runs the baseline | The no-regression comparison needs a baseline | The baseline is re-recorded first | omn-dev-1-implement |

### 4.3 Constraints

| ID | Class | Constraint | Hard or negotiable | Source |
|---|---|---|---|---|
| C-001 | functional | One source from four narrates a whole production; the vendor never while the owner's decision stands; the fake in a demonstration store only | hard | S-001, S-002, A-001 |
| C-002 | security | No settings value alone re-admits the vendor | hard | S-002, A-001 |
| C-003 | functional | Every recorded hash is over the bytes decoded, measured, assembled or loaded | hard | S-004, S-007, A-001 |
| C-004 | functional | Duration and loudness come from decoded audio, never a header or configuration, and are never acted on | hard | S-008, A-001 |
| C-005 | quality-attribute | No record, transaction or datastore command is open while a process runs, a file is hashed or a decode runs; every wait is bounded and ends named; every recorded instant is the datastore's | hard | S-003, S-007 |
| C-006 | structural | Every delivered assertion and live test stays; assertions are extended, never weakened; tests are rewritten only as the Planning Gate ruled | hard | S-017, A-001 |
| C-007 | security | No phase downloads or runs the real model; own-source production constructs no network client and passes no text off the machine | hard | S-003, S-011 |
| C-008 | functional | No target, tolerance, format requirement, rights position, performer identity or quantity is invented; an absent field reads absent | hard | S-007, S-018 |
| C-009 | security | Expected installation hashes live in the run's settings outside the repository and are never written to a store before the go | hard | S-005, A-001 |
| C-010 | compliance | No model, voice or runtime file and no release document is stored in the repository or the store | hard | S-005, S-006 |
| C-011 | compliance | Nothing publishes and the item stays held | hard | S-009 |
| C-012 | migration | The tenth resource is additive and write-once, changes no delivered row, and reaches the company store only through the orchestrator after a backup | hard | S-011, S-013, A-008 |
| C-013 | structural | The production assembly's first-party references stay as asserted; the rule-determined assembly references the domain and the ports only | hard | F-009 |
| C-014 | security | No secret or token reaches a record or an output | hard | S-014 |
| C-015 | functional | An own-source production books no non-zero operation, leaves the cap's counted total unchanged and prints local compute and recording time unmeasured | hard | S-010 |
| C-016 | operability | Executables, files, expected hashes and bounds come from configuration with no default in code | hard | S-012, S-013 |
| C-017 | structural | Minimum necessary change, reuse before new structure | negotiable | S-017 |

## Architecture and Component Design

### 5.1 Impacted Modules

| ID | Module | Impact type | Basis | Interfaces affected | Confidence |
|---|---|---|---|---|---|
| M-001 | Domain production records: ProductionMode, the new NarrationSource and the registration, release, measurement and provenance records | contract-change | F-011, F-040 | ProductionMode gains the own member; four new records | confirmed |
| M-002 | Rule-determined production rules: StoreGuard, NarrationSplitter and the new NarrationSourceRule, RecordingSetCheck and DurationExpectation | behavior-change | F-003, F-010 | Guard admits the own mode; a beat-only split; three pure functions | confirmed |
| M-003 | Application ports: IMediaTool, the new IVoiceModel, IProductionLedger, IProductionReader | contract-change | F-037, F-038 | A measurement member; a model member; default-implemented record members; a latest-registration read | confirmed |
| M-004 | MediaCompany.Production ExternalMediaTool | extension | F-004, F-005, F-006 | Model invocation, decoded measurement and a common-format join | confirmed |
| M-005 | MediaCompany.Production ProduceItemService and its ProductionRun | behavior-change | F-001, F-002, F-036 | Per-source narration, the own mode, per-part re-derivation; voice and cap required only for boundary sources | confirmed |
| M-006 | MediaCompany.Production ProductionPlan | behavior-change | F-016, F-017 | Per-part source lines, the own total line, price-row wording | confirmed |
| M-007 | MediaCompany.Production ProductionSettings and the host settings reader | contract-change | F-014, F-042 | Vendor-only members optional; a model section; the model part bound | confirmed |
| M-008 | MediaCompany.Production new InstallationVerifier and VerifiedInstallation | extension | F-028, F-031, F-032, A-005 | File-only verification holding read handles | confirmed |
| M-009 | MediaCompany.Production new RecordingRegistrar | extension | F-007, F-025 | Registration of one stored copy per beat | confirmed |
| M-010 | MediaCompany.Production ArtifactWriter | no-change-verified | F-007 | none; staging, promotion, hashing and containment reused | confirmed |
| M-011 | MediaCompany.Production ItemPackageLoader and LoadedItem | behavior-change | F-026 | Reads the script's rate and duration rows | confirmed |
| M-012 | Persistence NpgsqlProductionLedger and NpgsqlProductionReader | extension | F-011, F-038 | Registration, measurement and provenance writes; latest-registration read | confirmed |
| M-013 | Persistence SchemaInstaller | behavior-change | F-012, F-013 | Tenth resource; applying from an ordinal; refusing a full install over a designated store | confirmed |
| M-014 | Schema: the tenth ordered resource | contract-change | F-011, F-012 | Own mode, narration source column, four new tables | confirmed |
| M-015 | Persistence NpgsqlCompanyRecordReader produced-item read | behavior-change | F-022 | Reads the narration source | confirmed |
| M-016 | Deterministic analytics ManagementComposers | behavior-change | F-021, F-022 | Demonstration label wording; own-production line text | confirmed |
| M-017 | Capability providers VendorCalls and the speech, messages and image adapters | behavior-change | F-018, F-019, F-020, F-041 | Screened, truncated code or message | confirmed |
| M-018 | Host ProductionCommands and Program | contract-change | F-012, F-015 | Registration, verification, own mode, applying from an ordinal | confirmed |
| M-019 | Architecture suite ProductionBoundaryTests | behavior-change | F-008, F-009 | Added assertions | confirmed |
| M-020 | Test projects: production and persistence tests and a new stand-in executable project | extension | F-023, F-024 | Two rewritten vendor-path tests; a stand-in model process | confirmed |
| M-021 | Settings sample, the Wave 10 owner guide and the store guide | extension | F-012, F-042 | Repository documents | confirmed |
| M-022 | Capability boundary CapabilityGateway | no-change-verified | F-019 | none; own sources never reach it | confirmed |
| M-023 | Credentials CredentialBroker and ICredentialExchange | no-change-verified | F-041 | none | confirmed |
| M-024 | Publication path and release credential path | no-change-verified | F-009 | none | confirmed |
| M-025 | Item 001 package and item material | no-change-verified | F-025, F-026 | none; read only | confirmed |
| M-026 | Demonstration providers, the fake source | no-change-verified | F-035 | none | confirmed |
| M-027 | MeteredPreconditions | no-change-verified | F-015 | none; the vendor path below the rule | confirmed |
| M-028 | BeatTimeline | no-change-verified | F-039 | none; consumes decoded beat durations | confirmed |
| M-029 | Directory.Build.props version | behavior-change | F-043 | Version string, if the proposal is ruled | speculative |

Boundary crossings: production assembly to the operating system through the one starter (media tool and, new, the model's interpreter) and the one writer; production assembly to installed files outside the repository through read-only handles (verifier, registrar's release document hash); host to datastore (registration, version, artifacts, measurements, provenance); no crossing to any network from own-source production.

### 5.2 Options Considered

| Option | Structural change | Hard constraints | Impact surface | Reuse leverage | Quality attributes | Migration burden | Operability | Outcome |
|---|---|---|---|---|---|---|---|---|
| O-001 | Pure source rule in the rule-determined assembly; own mode; model run by the one delivered starter starting the environment's interpreter in isolated mode; verifier holding read handles; recordings copied by the one writer; decoded measurement; tenth resource applied alone; vendor bar in code at selection; screen at the one non-success constructor | Satisfies every hard constraint, C-001 to C-016 | 5 contract-change, 0 dependency-change | 27 rows | One starter and one writer kept; isolated model environment | 5 contract changes | Three commands added, one option | selected |
| O-002 | As O-001, but a sibling type starts the model process and the one-starter assertion names two types | Satisfies every hard constraint, C-001 to C-016 | 5 contract-change, 0 dependency-change | 26 rows: the delivered bound and tree termination are not reused | A second operating-system boundary type | 5 contract changes | Same | not selected, loses on reuse under C-017 |
| O-003 | As O-001, but the vendor bar is a settings value listing admitted sources | Violates C-002: a settings edit re-admits the vendor | 5 contract-change, 0 dependency-change | 27 rows | Bar removable by editing a file | 5 contract changes | One more setting | eliminated: violates C-002 |
| O-004 | As O-001, but registered recordings are read in place from the operator's folder, hashed at each read | Satisfies every hard constraint, C-001 to C-016 | 5 contract-change, 0 dependency-change | 26 rows: the writer's promote-once copy is not reused | Bytes outside the writer's control between hash and decode | 5 contract changes | The operator must not touch the folder | not selected, loses on reuse under C-017 |
| O-005 | As O-001, but an installation command writes the expected hashes into the store | Violates C-009: a store write before the go | 5 contract-change, 0 dependency-change | 27 rows | Hashes travel with the store | 6 contract changes | One more command | eliminated: violates C-009 |
| O-006 | As O-001, but narration durations keep the delivered container-field probe | Violates C-004: a header duration | 4 contract-change, 0 dependency-change | 27 rows: the probe reused, the measurement not | Durations unmeasured | 4 contract changes | None added | eliminated: violates C-004 |
| O-007 | As O-001, but the starter runs the runtime's console launcher with the inherited environment | Satisfies every hard constraint, C-001 to C-016 | 5 contract-change, 0 dependency-change | 26 rows: the interpreter's isolated mode is not reused | Inherited interpreter variables may redirect the code that runs | 5 contract changes | Same | not selected, loses on reuse under C-017 |
| O-008 | As O-001, but a recording set of mixed formats is resampled to the first file's format when joined | Violates C-008: a format target the owner did not set | 5 contract-change, 0 dependency-change | 27 rows | Joined audio differs from its parts | 5 contract changes | No refusal | eliminated: violates C-008 |

### 5.3 Selected Approach

- Selected: O-001, under every hard constraint, keeping the one delivered starter and the one delivered writer.
- Structural change:
  - Source set and rule (D-001). The domain gains NarrationSource with exactly four members, Recording, InHouseModel, Fake and Vendor; no other value is constructible or printable, and the tenth resource checks the same four. NarrationSourceRule in the rule-determined assembly is a pure function over the mode, the store designation, the latest registration's state for the item and package version (absent; present without a complete release; present with release and every stored file at its registered hash; present with release and a file missing or changed) and the model's verification state (not configured; verified; refused with findings). Order: fake mode yields Fake on a demonstration store; metered mode yields a refusal naming the owner's decision of 2026-10-10; the own mode and plan-only yield Recording when the registration has a complete release and verified files, refuse naming the beat file and both hashes when a released registration's file is changed or missing, pass the recording over naming each absent release field when the release is incomplete, then yield InHouseModel when the model is verified, and otherwise refuse naming why. The result carries the chosen source or the refusals, and every passed-over reason, so the plan prints one line per part naming the source and the reason; two plans over equal inputs print equal lines because the rule reads nothing else. The own mode forms one part per beat by a beat-only member of NarrationSplitter (thirteen for item 001); the character maximum and the voice apply to the boundary sources only. Before each part the run re-reads the latest registration and re-verifies the model, re-derives the source and, if it differs from the planned source or the registration identifier changed, records the Audio stage failed naming planned and derived source and ends with exit 3 before producing that part. A store that changes between plan and run therefore never mixes sources.
  - Vendor bar (D-002). No branch of the rule yields Vendor, and the rule reads no setting; the refusal text names the owner's decision of 2026-10-10 that the company's narration is its own. Re-admission needs a new recorded owner decision and a code change adding a branch; this change delivers no re-admission path. The speech, messages and image paths, the boundary and every test of them stay. ProduceItemService keeps the boundary narration path as the narrator for the Fake and Vendor sources and gains an internal entry, visible to the persistence tests, that runs a production with an explicitly named source below the rule. Dispositions under the Planning Gate ruling: the zero-spend end-to-end test is rewritten so its metered section drives that entry with the Vendor source, labelled the vendor path under test, every request and booking assertion kept, and its plan-only section asserts the own-source plan's no-metered-spend line while the vendor estimate it asserted moves to a direct composition of the vendor plan with the same assertion; the cancellation-during-a-vendor-call test drives the same entry with the Vendor source, every assertion kept; a new test proves the rule yields no Vendor over the full product of its finite inputs. The implementation report lists each rewritten test by name with its old and new form.
  - Own mode and store admission (D-003). ProductionMode gains an own member, offered by the produce command as its own mode. StoreGuard admits the own mode on a company store with matching identity and on a demonstration store, refuses it on an undesignated store, and keeps every delivered verdict for fake, plan-only and metered. Metered mode stays admitted by the guard on the company store and is refused by the rule, so the refusal names the owner's decision rather than the store. The own mode needs no cap and no gateway; the cap is printed when recorded.
  - Recording registration (D-004). RecordingRegistrar in the production assembly registers one folder against the item and package version named by the settings' material. RecordingSetCheck, a pure function over the folder's file names and the material's beat numbers, accepts exactly one file named by its two-digit beat number (any extension) per beat and refuses, naming each beat or file affected, a missing beat, an extra beat number, a duplicate beat, a file not named by a beat (a whole-narration file among them) and any other file. Each accepted file is copied by the one writer into a registration folder under the output root, promoted once and hashed over the stored bytes; the stored copy is probed (exactly one audio stream and no video stream), decoded end to end (zero errors) and measured; then re-hashed, and a change since promotion refuses. The registration records, on one short transaction after all files pass, the item, package version, each beat's stored path, SHA-256, length, container, codec, sample rate, channels, sample format, decoded samples, duration and loudness, the performer's name and the release record: document reference, document date, performer's name, the document's SHA-256 and the no-training term, each recorded absent when not given, never filled. The release document's hash is computed by reading the document from a path outside the repository, refused inside it, and the document is copied nowhere. Readers of a registered recording's bytes, each checking the hash: the registration's measurement (the stored copy, re-hashed after measuring); the plan, which re-hashes every stored copy without starting a process; the production's copy of each stored file into its run, whose promoted part artifact must carry the registered hash; the part measurement and the join, which read that part artifact; and a re-hash of every part artifact after the join. Any mismatch stops the production before narration, or records the Audio stage failed mid-run, naming the file and both hashes.
  - Model boundary (D-005). ExternalMediaTool, the one starter, gains the model invocation behind the new IVoiceModel port, starting the environment's interpreter located by setting, in isolated mode with bytecode writing off, running the runtime as a module. Its argument list is, in order: the model file's full path, the configuration file's full path, the part's text file and the part's output file (both plain names in staging), and all six generation settings explicitly (length scale, noise scale, noise width scale, sentence silence, volume, and the no-normalise flag only when normalisation is off); never a voice name, a data directory, a device flag, a speaker number, raw or directory output. The text reaches it only through the text file the writer stages, never an argument. Its working directory is the run's staging folder; it runs under the model part bound with no transaction held; its output is promoted and hashed by the one writer after it exits. The exact argument list is recorded with each artifact. Tests start a stand-in console executable built by a test project, which accepts the same argument list, writes a WAV whose length follows the text, and can be deterministic, non-deterministic, slow past a bound or failing; no test runs the real model.
  - Installation verification (D-006). The run's settings file, outside the repository, names five files and their expected SHA-256 values: the interpreter started, the model file, the configuration file, the model card and the runtime's RECORD. InstallationVerifier refuses, naming the file, a configuration path that is not the model path plus ".json" (F-028), any of the five resolving inside the repository root (the writer's containment members reused), a missing file, and a differing hash with both values. It verifies every RECORD entry carrying a hash against the file it names, and reads the runtime's name, version and licence from the package metadata, itself a verified RECORD entry, and the voice's identity, sample rate and inference defaults from the verified configuration. Before each part it opens the interpreter, model, configuration and RECORD files with read access denying write and delete sharing, hashes each through its open handle, and holds the handles in a VerifiedInstallation until the part's process exits, so the bytes hashed are the bytes loaded (A-005). The RECORD entries are verified once before the first part and once after the last; a change ends the stage failed. Plan-only runs the whole verification, reading files only, and prints every file with both hashes.
  - Measurement and expectation (D-007). IMediaTool gains a measurement member: one decode pass of the media tool over the audio stream reporting its decoded sample count, sample rate, channel count, sample format and EBU R128 integrated loudness (ITU-R BS.1770 gating, unit LUFS, the tool's R128 meter); duration is decoded samples over the decoded sample rate, recorded with the basis "decoded sample count over sample rate"; container and codec come from the probe's stream fields. A loudness the meter cannot state is recorded as not measurable with the tool's reason, never zero. Every narration part of every source and the joined narration are measured so; the rendered video keeps the delivered container-field runtime, labelled "container duration of a file decoded end to end with zero errors". The join writes PCM in the parts' common sample rate, channel count and sample format with no filter, so it neither resamples nor requantises; a set whose measured formats differ refuses before narration naming each part's format (Q-007). The joined narration's decoded sample count must equal the sum of its parts' to within one sample, one audio frame of the joining format. The expectation reads the script quoted by the material: the loader finds the script's rows labelled for the assumed delivery rate and for the narration duration at that rate and quotes them; words are the whitespace split of each beat's span; expected seconds are words times 60 over the rate, to one decimal and otherwise unrounded (771.6 s whole, printed beside the script's own 12 min 51 s). Each beat and the whole report measured, expected and the signed difference measured minus expected in seconds, with no pass, fail or tolerance; a missing rate row reads not stated. No rate is written into production source or the material.
  - Provenance and repeatability (D-008). Each model-produced part records, read from the verified files and the run: model name and version and code licence from the package metadata; voice name composed from the configuration's language code, dataset and quality; voice dataset licence from the model card; weights licence and voice licence read "not stated in the installed files" because no installed file states them (F-030, Q-006); the six generation settings as passed, each beside the voice's default read from the configuration where it states one; the SHA-256 of the interpreter, model, configuration, model card and RECORD as computed through the held handles in that run; the count of RECORD entries verified; the argument list; and the repeatability statement. The run generates the first part twice with identical settings and compares the bytes: identical records "repeats" for that part, different records "not observed to repeat" with both hashes; every other part records "not observed to repeat: generated once". A recording-sourced part records its registration, beat, performer and release. No quality rating is recorded for an own source (Q-001).
  - Timeouts, holds and clocks (D-009). The enumeration and proposals for ruling:
    - Default command timeout, 30 seconds, every statement of every record transaction; ends in a datastore timeout named as the stage's failure.
    - Production version opening, each stage record, each artifact, measurement and provenance record, and the registration: each one short transaction of its own after the file or process work it records; nothing is held across a process, a hash or a decode.
    - Audit chain head, exclusive from each record's append to its commit, under the 30-second timeout; the record horizon shared by each writer to commit.
    - Version key, artifact key, registration key and provenance key: waited only behind an uncommitted insert of the same key, under 30 seconds; a duplicate ends named.
    - Model part bound, new, proposed 350 seconds on the monotonic clock per process, twice the measured 175 s for the whole narration (A-007), the floor being 175 s; past it the process tree is terminated, staging for that part discarded, the Audio stage recorded failed naming the part and the bound, exit 3. The repeat generation runs under the same bound.
    - Installation handles: held on four files from each part's hash until its process exits or is terminated; a hold on files, not on the store.
    - Hashing of the voice model (114,219,352 bytes), the RECORD entries and the recordings: in process, reading local files, no transaction, cancellable by the operator; no separate bound proposed.
    - Registration probe, the delivered probe bound of 60 s; registration decode and measurement, the delivered decode bound of 60 minutes, borrowed as the join borrows it, with no new setting; production part measurement, the decode bound.
    - Join, decode bound; render, render bound of 60 minutes; rendered probe and version line, probe bound; rendered decode, decode bound; still, still bound; all unchanged.
    - Provider-call bound, credential handle, scope locks and admission transaction: not reached by an own source; unchanged for the boundary sources.
    - Management read, 2-second lock and 5-second statement bounds, unchanged.
    - Operator cancellation: terminates the process in flight, records the interrupted stage failed on an uncancellable token, exit 6, as delivered.
    - Clocks: every recorded instant is the datastore's insert instant; the process clock is never recorded; bounds use monotonic elapsed time; every narration duration is decoded samples over sample rate; the rendered runtime is the container field as labelled; no wall-clock elapsed time is recorded.
  - Tenth resource (D-010). One additive ordered resource after the nine: widens the production version mode check to the own mode; adds a nullable narration source column checked to the four members and consistent with the mode (fake with Fake, metered with Vendor, own with Recording or InHouseModel), null on rows recorded before it, read as the delivered mode's source; creates, empty, write-once and stamped by the delivered stamping function: recording registrations (identifier, item, package version, performer's name, the five release fields, each nullable), registered beat files (registration, beat, stored path, SHA-256 and the measured properties), narration measurements (one per narration artifact: decoded samples, sample rate, channels, sample format, codec, container, duration, basis, loudness, its measure and unit or why not measurable, words and expected seconds where a beat) and narration provenance (one per own-source part artifact: the D-008 fields). No column holds a document or a model file. SchemaInstaller gains applying the resources from a named ordinal, used to apply the tenth alone to a store holding nine; a full install over a store already holding the designation table is refused naming that option.
  - Commands, settings and the orchestrator's steps (D-011). The host offers: a registration command taking the settings, the recordings folder and, each optional, the performer's name, the release document reference, its date, the release document's path (hashed, never copied) and the no-training term, exiting 0 registered, 2 refused before anything is recorded naming every finding, 3 a tool or store failure named; a verify-model command taking the settings, reading files only, exiting 0 verified printing every file with both hashes and the runtime's metadata, 2 refused naming every finding; the produce command with modes own, fake, plan-only and metered and its delivered exit codes, metered always 2 naming the owner's decision; the install command with an option naming the first resource to apply. New settings, none defaulted in code: the model section (interpreter, model, configuration, model card and RECORD paths; their five expected hashes; the six generation settings; the model part bound in seconds). The provider-call bound, character maximum, voice, endpoints and demonstration providers become optional and are required by name only in the modes that use them. The orchestrator's ordered steps: a fresh backup of the company store; readings of every delivered table's rows, item 001's versions and stages, its operations and the cap's counted total; the tenth resource applied alone and re-applied, with the readings equal after; the run's settings written outside the repository with the expected hashes from the owner's decision record and the orchestrator's first-hand values; verify-model; recommended, an own-mode production of item 001 against the demonstration database with the real model, labelled demonstration and never a company record (Q-010); plan-only against the company store naming the model for every part, the hashes verified and no metered spend; the owner's go; the own-mode production; readings after, including every artifact's provenance and measurements, the unchanged cap total, no new operation, and the recorded argument lists and the architecture assertion as the structural evidence of zero network requests.
  - Own-source cost (D-012, inline). An own source books no operation and reaches neither reservation nor booking, so the cap's counted total is unchanged. Unmeasured wording, never zero, in: the plan's total line ("no metered spend is planned, USD 0.00; local compute unmeasured", or recording time for a recording); each part's plan line; the Audio stage summary; the run summary; the produced-item operations line's text for an own-mode production.
  - Error body (D-013, inline). The one non-success constructor reads, from the one body buffer, a code or message: the first string member among an error object's code, type and message, or top-level code, message and detail, of a JSON body; otherwise the body as text when it is valid UTF-8 without control characters. It then removes every value the credential exchange attached to the sent message's authentication headers and its bearer form, replaces any bearer value and any token-shaped run (twenty or more characters of letters, digits, hyphen, underscore or dot mixing letters and digits, and any three dot-joined base64url segments) with a redaction marker, strips control characters and truncates to the ruled length, proposed 200 characters. An empty body reads "no error code or message was readable (empty body)", a non-text body "(not text)". The screened text joins the status in the failure reason, so every sink of F-019 receives only screened text.
  - Labels (D-014, inline). In a store designated demonstration, the controller-reading and candidate labels describe each embedded quantity through the demonstration figure's rendering, and the candidate label names "quality" and "cost" rather than "observed quality" and "observed cost"; in a company store every label reads as delivered.
  - Total line (D-015, inline). The vendor plan's total line states "ESTIMATE at configured unit prices; each price row as recorded: kind, source, verified on date; never netted with development cost", asserting neither first-hand nor other verification, because no recorded field distinguishes them (F-017); each price row's line drops "re-fetched first hand"; the line is unchanged when no price row is used; the own plan prints the D-012 line.
  - Assertions (D-016, inline). Added: NarrationSource has exactly four members; the one-starter and one-writer assertions unchanged and covering every new type; no type of the own-source path (the rule, the registrar, the verifier, the own narrators) constructs or names a network client, socket or the capability boundary, with a probe; the model's interpreter is located only by setting, the delivered executable scan covering it; no file in the repository tree carries a model, voice-configuration, model-card, package-record or runtime-launcher name, with a seeded probe folder; the tenth resource declares no binary column, with a seeded probe text; the production assembly's references unchanged. Every delivered assertion kept; the architecture count rises by the added facts.
- Rationale: O-001, O-002, O-004 and O-007 satisfy every hard constraint and tie at five contract-change and no dependency-change modules; O-001 has 27 reuse rows and each of the other three 26, so O-001 is selected on reuse leverage before quality is reached.
- Highest-scoring rejected alternative and why it lost: O-002, O-004 and O-007 tie at 26 reuse rows and the tie breaks to the lowest identifier, O-002; it loses on reuse because a sibling starter duplicates the delivered bound and process-tree termination instead of reusing them, and it widens the one-starter assertion to two named types.
- Tradeoffs accepted:
  - Every own-source production loads the voice once per part and generates the first part twice; recorded as R-013.
  - A mixed-format recording set is refused rather than converted; recorded as R-006.
  - Weights and voice licence fields read not stated until the owner records them; recorded as R-007.
  - Plan-only previews the own mode, so the vendor estimate is printed only below the rule; recorded as R-022.
  - The interpreter's base executable and the dependencies' code are verified only if the hashed-set extension is ruled; recorded as R-002.

### 5.4 Decisions

| ID | Decision | Architecture-significant | Record |
|---|---|---|---|
| D-001 | A closed four-member source set and one pure selection rule, re-derived before every part, under O-001 | Yes | architecture-decision-record-D-001.md |
| D-002 | The vendor bar at selection in code, no re-admission path, two delivered tests rewritten below the rule, under O-001 | Yes | architecture-decision-record-D-002.md |
| D-003 | An own production mode and its store admission, metered refused by the rule, under O-001 | Yes | architecture-decision-record-D-003.md |
| D-004 | Registration of one stored copy per beat by the one writer, with the release fields recorded or absent, under O-001 | Yes | architecture-decision-record-D-004.md |
| D-005 | The model run by the one starter: isolated interpreter, explicit files and settings, text by file, under O-001 | Yes | architecture-decision-record-D-005.md |
| D-006 | Installation verification from settings, five files and the RECORD entries, hashed through held handles, under O-001 | Yes | architecture-decision-record-D-006.md |
| D-007 | Decoded-sample durations, one named loudness measure, an exact common-format join and the script's expectation, under O-001 | Yes | architecture-decision-record-D-007.md |
| D-008 | Provenance read from verified files, a repeat check on the first part, no quality rating, under O-001 | Yes | architecture-decision-record-D-008.md |
| D-009 | The timeout, hold and clock enumeration with the model part bound proposed for ruling, under O-001 | Yes | architecture-decision-record-D-009.md |
| D-010 | The tenth resource and applying it alone, under O-001 | Yes | architecture-decision-record-D-010.md |
| D-011 | The commands, the settings and the orchestrator's ordered steps, under O-001 | Yes | architecture-decision-record-D-011.md |
| D-012 | Own-source cost: no operation booked, unmeasured wording in every cost sink, under O-001 | No | None; recorded inline in 5.3 |
| D-013 | The vendor error body screened and truncated at the one non-success constructor, under O-001 | No | None; recorded inline in 5.3 |
| D-014 | Demonstration controller-decision label wording, under O-001 | No | None; recorded inline in 5.3 |
| D-015 | The total line states each price row's recorded source and date, under O-001 | No | None; recorded inline in 5.3 |
| D-016 | The added boundary assertions, under O-001 | No | None; recorded inline in 5.3 |
| D-017 | The version is proposed as 1.7.0 for ruling, under O-001 | No | None; routed as Q-008 |

## API and Data Model Impact

- API changes:
  - M-001: ProductionMode gains the own member; new NarrationSource with four members; new records for a registration, a registered beat file, a release, a narration measurement and a narration provenance.
  - M-003: IMediaTool gains the measurement member; new IVoiceModel with the model invocation; IProductionLedger gains registration, measurement and provenance members with default implementations; IProductionReader gains the latest registration read.
  - M-007: ProductionSettings' provider-call bound and character maximum become optional; a model section and the model part bound are added.
  - M-014: the tenth resource as in D-010.
  - M-018: the host gains the registration and verification commands, the own mode, and the install option.
- Contract compatibility notes:
  - M-001 transition strategy. Current shape: three modes, no source. Target shape: four modes and a four-member source. Compatibility approach: members appended with new values; every switch over the mode gains its branch; delivered values unchanged. Coexistence period: none. Retirement condition: none. Rollback position: removing the own member and the source with the code that uses them.
  - M-003 transition strategy. Current shape: delivered ports. Target shape: additive members, default-implemented where delivered doubles implement the port (F-037); the new port realised by the one starter. Coexistence period: none. Retirement condition: none. Rollback position: removing the members and their realisations.
  - M-007 transition strategy. Current shape: every vendor member required. Target shape: vendor members optional and refused by name in the modes needing them; a model section required by the own mode. Compatibility approach: delivered settings files and test initialisers stay valid. Coexistence period: none. Retirement condition: none. Rollback position: making the members required again.
  - M-014 transition strategy. Current shape: nine resources, two modes recorded. Target shape: ten resources, three modes recorded, a nullable source column and four new tables. Compatibility approach: checks widened only, the column nullable, tables created only where absent; delivered rows keep their values and read their source from their mode. Coexistence period: none. Retirement condition: none. Rollback position: dropping the tenth resource's tables, column and widened checks in reverse order, lossless while they hold only demonstration rows; a company store holding a real registration, measurement or provenance is restored from the step-one backup, never dropped.
  - M-018 transition strategy. Current shape: eight commands. Target shape: ten, with a produce mode and an install option added. Compatibility approach: delivered commands and options unchanged, except that a full install over a designated store now refuses. Coexistence period: none. Retirement condition: none. Rollback position: removing the two commands, the mode and the option.
- Schema or migration changes:
  - Production version mode check widened to the own mode and a nullable narration source column checked to four members and consistent with the mode; direction forward; reversible by dropping the column and restoring the check while no row carries the own mode; delivered rows untouched, readers reading null as the mode's source.
  - Four new write-once, datastore-stamped tables for registrations, registered beat files, narration measurements and narration provenance, created empty; direction forward; reversible by dropping them in reverse order while they hold only demonstration rows; readers before the resource read not recorded and the own mode refuses naming the missing resource. Writers during transition: only the new code writes them.

## Reusable Components and Reuse Rationale

| Capability | Candidate | Outcome | Rationale |
|---|---|---|---|
| Starting the model under a bound with tree termination | ExternalMediaTool process run | reuse-extended | The model invocation is added to the one starter (F-004) |
| Staging, promoting and hashing produced audio | ArtifactWriter | reuse-as-is | Model parts and recording copies are promoted and hashed as delivered (F-007) |
| Copying registered recordings under the output root | ArtifactWriter copy into staging and promote | reuse-as-is | One stored copy per beat, never overwritten (F-007) |
| Refusing files inside the repository | ArtifactWriter resolution and containment | reuse-as-is | Internal to the same assembly (F-007) |
| Hashing files | SHA-256 in the standard library | reuse-as-is | As the writer hashes |
| Verifying the runtime's files | The RECORD's own SHA-256 entries | reuse-as-is | The package records its files' hashes (F-031) |
| Reading package metadata and voice configuration | Text and JSON readers in the standard library | reuse-as-is | No new package |
| Holding files against replacement | Platform file handles with a sharing mode | reuse-as-is | Read access, denying write and delete (A-005) |
| Decode with zero errors | ExternalMediaTool decode | reuse-as-is | Registration and the rendered file (F-004) |
| Stream kinds, container and codec | ExternalMediaTool probe | reuse-extended | Codec and container fields added; duration not read from it for narration (F-005) |
| Decoded duration and loudness | The installed media tool's statistics and R128 meter | reuse-extended | A measurement member on the one starter (F-034, F-044) |
| Joining parts | ExternalMediaTool join | reuse-extended | Common sample format instead of 16-bit (F-006) |
| Beat parts | NarrationSplitter beat openings | reuse-extended | A beat-only member (F-003) |
| Beat timeline | BeatTimeline | reuse-as-is | Consumes decoded beat durations (F-039) |
| Store admission | StoreGuard | reuse-extended | The own mode admitted (F-010) |
| Source selection | StoreGuard, route resolution, the standard library, the platform, installed dependencies | none-found | Searched existing components, the standard library, the platform and installed dependencies; no rule selects a narration source |
| Recording set naming check | MaterialCheck, the standard library, the platform, installed dependencies | none-found | Searched existing components, the standard library, the platform and installed dependencies; no check matches file names to beats |
| Expectation from the script | ItemPackageLoader quotations | reuse-extended | The script is already a quoted source (F-026) |
| Production records | NpgsqlProductionLedger | reuse-extended | Registration, measurement and provenance writes (F-038) |
| Additive port members | Default-implemented members | reuse-as-is | Delivered precedent (F-037) |
| Write-once, datastore-stamped tables | The ninth resource's refusal and stamping functions | reuse-as-is | The tenth reuses them (F-011) |
| Ordered install | SchemaInstaller | reuse-extended | Tenth resource and applying from an ordinal (F-012) |
| Demonstration rendering of a quantity | LineFigure demonstration rendering | reuse-as-is | Used inside labels (F-021) |
| Produced-item lines | ManagementComposers produced-item lines | reuse-extended | Own-mode text (F-022) |
| Vendor non-success record | VendorCalls non-success constructor | reuse-extended | Screened code or message (F-018) |
| The secret to screen | The authentication headers on the sent message | reuse-as-is | The exchange attached them; no contract change (F-041, F-020) |
| Vendor path and fake | The speech path, the boundary and the demonstration composition | reuse-as-is | Kept below the rule (F-035) |
| Exit codes and recorded endings | ProduceResult and the uncancellable stage records | reuse-as-is | (F-040, F-036) |
| Process environment isolation | The interpreter's own isolated mode | reuse-as-is | An option of the installed interpreter |
| Stand-in model process for tests | Existing doubles, the media tool, the standard library, installed dependencies | none-found | Searched existing components, the standard library, the platform and installed dependencies; none accepts the runtime's argument list; a test-built console stand-in |
| A second process-starting type | A sibling starter | rejected | Duplicates the bound and termination and widens the one-starter assertion (O-002) |
| Reading recordings in place | The operator's folder | rejected | Outside the writer's promote-once control between hash and decode (O-004) |
| The console launcher | The runtime's launcher with the inherited environment | rejected | Inherited interpreter variables could redirect the code that runs (O-007) |
| The rate in the material | Item material quotation | rejected | The Planning Gate ruled the rate cited from the script and not added to the material (A-001) |

## Operational Considerations

- Logging and observability updates: the produce command prints the plan with one line per part naming source and reason, the model's files with both hashes, the generation settings and the bounds; each part's generation, measurement and artifact; the duration report and the loudness per part and whole, labelled measured, expected or specified; registration and verification print every finding by name; no narration text, release document content or secret is printed (M-005, M-008, M-009, M-018).
- Error handling strategy: refusals before anything recorded exit 2 naming every finding; a stage failure, a bound passed or a source changed between plan and run records the stage failed and exits 3; an operator cancellation exits 6 as delivered; every failure names the file, part or bound and both hashes where one differs (M-005, M-018, C-005).
- Security considerations: no network client is constructed on the own-source path and the model is given explicit local files, never a name it could look up or fetch (C-007, M-004); the interpreter runs isolated from inherited variables (D-005); installed files are hashed through handles that deny replacement (C-003, M-008); no model, runtime or release document is stored in the repository or the store (C-010, M-014); the vendor error body is screened of the attached secret and token-shaped strings before any sink (C-014, M-017); the vendor stays barred in code (C-002, M-002); the copyleft runtime is used in house and not distributed.
- Performance considerations: one process per part loads the voice each time, and the first part is generated twice; the whole narration was measured at 175 s on this CPU (F-034), and no further throughput figure is claimed; hashing about 114 MB per part is in process with no hold on the store (C-005).
- Deployment and operability: the orchestrator applies the tenth resource alone after a backup and writes the model section into the run's settings outside the repository (M-013, M-021, C-012); every new setting is required where used with no default in code (C-016).

## Delivery Plan

### Sequencing Constraints

| ID | Constraint | Modules | Prerequisites | Reason | Binds |
|---|---|---|---|---|---|
| P-001 | The Design Gate rulings on the model part bound, the truncation length, the repeat check, the quality rating and the hashed set precede any code applying them | M-007, M-008, M-017 | none | A quantity is ruled before it is coded | T-001, T-002, T-003, T-004, T-005, T-006, T-007, T-008, T-009, T-010, T-011, T-012, T-013, T-014, T-015 |
| P-002 | The live baseline is recorded before any change | M-020 | none | A comparison needs its base first | T-016 |
| P-003 | The domain records and port contracts exist before any realisation or consumer | M-001, M-003 | P-001 | Contract definition precedes consumers | T-017 |
| P-004 | The source rule, the guard's own admission, the set check, the expectation and the beat split exist before the service uses them | M-002 | P-003 | Rules precede their callers | T-019 |
| P-005 | The tenth resource and the installer's application from an ordinal exist before any adapter writes the new tables | M-013, M-014 | P-003 | Schema precedes writers | T-018 |
| P-006 | The persistence members read and write the new records before registration or production use them | M-012, M-015 | P-005 | Adapters precede callers | T-018 |
| P-007 | The measurement member exists before registration and production measure | M-004 | P-003 | The boundary precedes its consumers | T-020 |
| P-008 | The installation verifier exists before any model run | M-008 | P-003 | Verification precedes the load it protects | T-022 |
| P-009 | Registration exists before a recording-sourced production | M-009 | P-006, P-007 | Data precedes its consumer | T-021 |
| P-010 | The model invocation through the one starter, with held handles, exists before model-sourced narration | M-004, M-008 | P-007, P-008 | The boundary is verified before work assumes it | T-023 |
| P-011 | Recording-sourced narration in the produce path is the first own-source path | M-005, M-006, M-011 | P-004, P-006, P-009 | The path without the model process is proven first | T-025, T-027, T-035, T-043 |
| P-012 | Model-sourced narration and provenance follow it | M-005 | P-010, P-011 | It reuses the recording path's measurement, join and records | T-024, T-026, T-036, T-040 |
| P-013 | The two delivered vendor-path tests are rewritten only once the rule refuses the vendor in the service, with the never-vendor test | M-005, M-020 | P-004, P-011 | A rewrite follows the behaviour it accommodates | T-037, T-038 |
| P-014 | The host commands and the settings follow the services they compose | M-007, M-018 | P-011, P-012 | Composition after composed parts | T-028, T-033 |
| P-015 | The carried items stand apart from both own-source paths | M-006, M-016, M-017 | P-001 | No dependency on the narration path | T-029, T-030, T-031, T-048, T-049, T-050 |
| P-016 | The boundary assertions cover every new type after the types exist | M-019 | P-012, P-014 | Verification of the boundary after it is built | T-032, T-039, T-045 |
| P-017 | The settings sample and the guide follow the commands | M-021 | P-014 | Documents describe delivered commands | T-034, T-047 |
| P-018 | The tenth resource is proven over a store of the company store's recorded shape before the orchestrator applies it after a backup | M-013, M-014 | P-005 | The reversibility safeguard precedes the irreversible step | T-051 |
| P-019 | The round-trip, store-isolation and binding-constraint verifications follow every path they read | M-020 | P-012, P-013, P-016 | Evidence after behaviour | T-041, T-042, T-044, T-046, T-052, T-053 |
| P-020 | The release record and the orchestrator's company-store run follow every verification | M-021 | P-017, P-018, P-019 | Record and run after evidence | T-054 |

### Test Strategy Focus Areas

- Round trip of every hash: a byte changed before each reader of a registered recording; a model, configuration, interpreter or RECORD-listed file swapped before verification, during a held run (the swap refused by the platform) and between parts; each run stopping by name with both hashes (M-008, M-009, M-005), for omn-dev-2-reviewer.
- Clocks and holds: a stand-in model outlasting a short bound, terminated, its stage failed; open-transaction observations during the stand-in, a long decode and a large-file hash, each finding none; every instant equal to the datastore's with the process clock a day ahead and behind (M-004, M-005).
- Measurement: a fixture whose container states a different duration from its decoded samples; tones of known level; a joined narration equal to its parts within one sample; half and double expectation reported and never acted on; a mixed-format set refused (M-004, M-002).
- Rule and bar: the full table of mode, designation, registration and model states with each outcome; two plan-only runs printing equal lines; the rule never yielding the vendor; a store holding a priced vendor route and a set credential variable making zero requests in every mode (M-002, M-005).
- Assertions each failing a solution build on a seeded probe, and the architecture count after the change against the baseline (M-019).

### Rollout and Rollback

- Rollout: merged behind no flag; phases demonstrate in the demonstration database with stand-ins only; after the phases the orchestrator follows D-011's ordered steps, the real model first running against the demonstration database and then, on the owner's go, against the company store.
- Rollback: revert the change and drop the tenth resource's objects in a demonstration store; in the company store restore the backup taken in the first step, which predates every tenth-resource row.

## Risks and Mitigations

| ID | Class | Trigger | Impact | Likelihood | Affects | Mitigation | Owner |
|---|---|---|---|---|---|---|---|
| R-001 | structural | The isolated interpreter does not load the runtime from the environment (A-004) | The first real run fails at Audio | medium | D-005 | Demonstration-database run with the real model before the go; fallback to the launcher with a scrubbed environment, Q-010, P-020 | omn-orchestrator |
| R-002 | security | The launchers start a base interpreter outside the environment, and the dependencies' code is outside the runtime's RECORD (F-032) | Provenance names verified files while other code also ran | medium | D-006 | Q-005 extends the hashed set with first-hand values | omn-tech-lead |
| R-003 | security | A held handle does not deny replacement, or blocks the child (A-005) | A swap between hash and load, or no model start | low | D-006 | Stand-in test replacing a held file in P-010; re-hash after exit as fallback | omn-dev-1-implement |
| R-004 | contract | The media tool's decode pass reports no decoded sample count (A-006) | Duration has no decoded basis | medium | D-007 | A raw-sample decode counted by the starter, P-007 | omn-dev-1-implement |
| R-005 | operability | The model part bound is ruled too short, or the machine is busy (A-007) | A healthy part ends failed | low | D-009 | Bound is a setting; Q-002 | omn-tech-lead |
| R-006 | contract | The CEO's beat files differ in sample rate, channels or sample format (A-009) | The recording-sourced production is refused before narration | medium | D-007 | The guide asks for one format; Q-007 | omn-product-owner |
| R-007 | security | No installed file states the weights or voice licence (F-030) | Two provenance fields read not stated | high | D-008 | Q-006 | omn-product-owner |
| R-008 | migration | The tenth resource alters a delivered row or fails over the company store's state (A-008) | The company store is damaged before the run | low | D-010 | Backup first; proof over the company shape in P-018 | omn-orchestrator |
| R-009 | migration | A full install is run against the company store (F-012) | The first resource fails part-way over real rows | low | D-010 | Full install over a designated store refused, naming the option | omn-dev-1-implement |
| R-010 | contract | A rewritten vendor-path test loses an assertion, or a further delivered test needs rewriting (A-010) | A vendor-path property goes unchecked | medium | D-002 | Old and new forms listed by name; reviewer compares in P-013 | omn-dev-2-reviewer |
| R-011 | security | The screen misses a secret form or truncation keeps a fragment | A credential is recorded | low | M-017 | Exact attached values removed before truncation; a scan of every sink in P-015 | omn-dev-1-implement |
| R-012 | structural | A registration recorded mid-run changes the derived source | A model run stops part-way | low | D-001 | Intended stop, exit 3, the stage naming both sources | omn-dev-1-implement |
| R-013 | performance | One load per part and a repeated first part lengthen the run | A longer company-store run | low | D-008 | Q-004 | omn-tech-lead |
| R-014 | contract | A reader meets the own mode or a null source on a delivered row | A produced-item line is misread | low | M-015 | Null reads the mode's source, labelled recorded before the tenth resource | omn-dev-1-implement |
| R-015 | delivery | Eighteen statements exceed one implementation pass | Should-have items slip | medium | P-015 | Carried items sit off both own-source paths | omn-orchestrator |
| R-016 | security | The release document path lies in the repository or the document is copied | Personal material retained | low | D-004 | Refused inside the repository; read only to hash | omn-dev-1-implement |
| R-017 | operability | Another item's script has no rate row | The expectation reads not stated | low | D-007 | Never invented; stated as not stated | omn-dev-1-implement |
| R-018 | delivery | The version proposal is not ruled | The version assertion and records disagree | low | M-029 | Q-008 | omn-tech-lead |
| R-019 | operability | The runtime's per-sentence peak normalisation is read as the pipeline's | The no-level-change rule looks broken | low | D-008 | Recorded as a generation setting per the owner's decision | omn-documentation |
| R-020 | operability | A test names a database other than the demonstration database | That schema is dropped | low | P-019 | Every verification records its target store | omn-orchestrator |
| R-021 | contract | The configuration path differs from the model path plus ".json" (F-028) | The loaded configuration is not the hashed one | low | D-006 | Verification refuses naming both paths | omn-dev-1-implement |
| R-022 | contract | Plan-only no longer prints the vendor estimate | A delivered plan-only assertion fails | high | D-002 | Rewritten under P-013 with the vendor plan composed directly | omn-dev-1-implement |
| R-023 | structural | A gate ruling or owner decision restated in A-001 or A-002 is overturned, or a briefed value in A-003 differs | Selection, verification or measurement decisions change shape | low | D-001 | The phase returns to design rather than absorbing it | omn-orchestrator |

## Estimate and Confidence

- Overall: L (confidence: medium). Cross-boundary change across six production assemblies and the host with a tenth schema resource, all additive, and one new process invocation.
- Breakdown:
  - P-001, P-002, P-003, P-004: M (confidence: medium).
  - P-005, P-006, P-018: M (confidence: medium); additive tables over a populated store.
  - P-007, P-008, P-010: L (confidence: low); decoded measurement and held handles rest on A-005 and A-006.
  - P-009, P-011, P-012, P-013: L (confidence: medium); the produce path's per-source restructuring.
  - P-014, P-015, P-016, P-017, P-019, P-020: M (confidence: medium).
- Scope assumptions:
  - Should-have statements S-014, S-015 and S-016 are included.
  - The bounds and the truncation length are ruled as proposed or set as settings (A-007).
  - The model and the environment start as A-004 states.
- Uncertainty drivers:
  - Whether the tool's decode pass reports decoded samples (A-006).
  - Whether held handles deny replacement without blocking the child (A-005).
  - The hashed-set extension ruling (Q-005).

## Open Decisions and Escalations

| ID | Question | Blocking | Owner | Affects | Consequence |
|---|---|---|---|---|---|
| Q-001 | Should a narration source record a quality rating? Proposed: no rating recorded for an own source; the vendor route keeps its recorded zero | no | omn-tech-lead | D-008 | No: as designed; yes: a rating field and its source must be named |
| Q-002 | Is the model part bound 350 s, twice the measured 175 s, or the floor of 175 s? | no | omn-tech-lead | D-009 | The ruled value becomes the setting; otherwise the setting stays required with no default |
| Q-003 | Is the screened error text truncated at 200 characters? | no | omn-tech-lead | D-013 | The ruled length is applied |
| Q-004 | Is the first part generated twice in every model-sourced production for the repeat check? | no | omn-tech-lead | D-008 | Yes: as designed; no: every part records not observed to repeat, generated once |
| Q-005 | Is the hashed set extended to the environment's configuration, the base interpreter it names and each installed dependency's RECORD, with values the orchestrator measures first hand? | no | omn-tech-lead | D-006 | Yes: five more expected hashes in settings; no: the four ruled files plus the runtime's RECORD entries |
| Q-006 | Does the owner record the voice weights' and the voice's licence with their source, or do both fields read not stated in the installed files? | no | omn-product-owner | D-008 | Recorded: read from a recorded owner value with its source; otherwise not stated |
| Q-007 | Is a recording set of mixed sample rate, channels or sample format refused, or does the owner name a format to convert to? | no | omn-product-owner | D-007 | Refused: as designed; a named format: a conversion step and its record are added |
| Q-008 | Is the version 1.7.0? | no | omn-tech-lead | M-029 | Yes: the assertion follows; no: 1.6.0 stays |
| Q-009 | Are the eleven records at status Proposed accepted at the Design Gate? | no | omn-tech-lead | D-001, D-011 | Accepted: implementation proceeds; otherwise this phase repeats |
| Q-010 | Does the orchestrator run an own-mode production with the real model against the demonstration database before the company-store run? | no | omn-orchestrator | D-005, D-011 | Yes: A-004 is shown before the go; no: the company-store run is the first real run |

## Sign-off

- Architect: unsigned; the producing agent does not decide its own gate
- Tech Lead: omn-tech-lead, accepting owner of the Design Gate under the producer exclusion rule; unsigned
- QA: not required at the Design Gate

## Appendix A: Objections recorded

1. The runtime's command line parses the configuration option but loads the voice without it; the configuration actually read is the model path plus ".json" (F-028). The ruled hash of "the configuration file passed to the process" names the loaded file only when the two paths are equal, so verification refuses otherwise (D-006).
2. Both environment launchers start a base interpreter outside the environment (F-032), and the runtime's RECORD does not cover its dependencies' code; the four-file set ruled at the Planning Gate does not identify every file that runs. Extension proposed for ruling (Q-005), with first-hand values, never invented.
3. No installed file states the voice weights' or the voice's licence (F-030), while scope criterion 12 asks for both "as installed"; they read not stated unless the owner records them (Q-006).
4. The plan's verification of the tenth resource and its fifth plan-level criterion say the ordered resources are re-applied; the first four delivered resources cannot be (F-012). The tenth is applied alone through an install option, and a full install over a designated store is refused (D-010).
5. The plan's duration-report task expects the whole expectation to read 12 min 51 s, while the Planning Gate fixed 771.6 s; both are printed, the computed figure beside the script's own quoted figure (D-007).
6. The plan's assumption on the rate says it is quoted into the item material, while the Planning Gate ruled it cited from the script and not added to the material; the design reads the script's own row at load time and adds no rate to production source or material (D-007).
7. The delivered join re-encodes to 16-bit PCM (F-006), which would requantise a 24-bit or floating-point recording; the join writes the parts' common sample format (D-007).
8. A recording set of mixed formats cannot be joined without resampling or requantising, which changes the measured audio; with no format target from the owner it is refused before narration and the question goes to the owner (Q-007).
9. The plan's design task on commands names a model-installation record command; under the Planning Gate ruling the installation record is the run's settings section the orchestrator writes, so no such command is delivered (D-011).
10. The adapter cannot read the configured secret by design (F-041); the screen removes the values the exchange attached to the sent message, which are that secret as sent (D-013).
11. Fake mode still requires the voice and the character maximum, as delivered (F-002); they become optional only for the own mode, and fake behaviour is unchanged.
12. The produced-item operations line counts every version's operations (F-022); after an own-source production it still shows item 001's earlier estimated operation, correctly, with the own-mode text added (D-012).
13. With the owner's default settings the runtime peak-normalises each sentence (F-029); the recorded loudness is of that output, and the pipeline applies no further level change (D-008).
