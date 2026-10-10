```yaml
design:
  designId: DESIGN-2026-0010
  changeReference: MC-10
  sourceInputs:
    - type: change-request
      reference: tasks/MC-10/input.md
    - type: execution-plan
      reference: runs/run-7ae81c0de400/states/execution-planning/artifacts/execution-plan.md
  producedBy: architect
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  decisionRecords: [D-001, D-002, D-003, D-004, D-005, D-006, D-007, D-008, D-010, D-011, D-012, D-013, D-014, D-015, D-016]
  consumesPlan: runs/run-7ae81c0de400/states/execution-planning/artifacts/execution-plan.md
  inputDigest: sha256:a763d073a6efa0241277048ada902f89
  contextDigest: sha256:f65fadccf1c3d16e5951ed9cbe601cbb
```

## Metadata

- Feature or Change ID: MC-10, the production capability: item 001's recorded package rendered to one video file end to end at zero spend against fake providers, ready for one owner-released metered run under a per-item cap of USD 5.95, with nothing published
- Author: architect
- Reviewers: omn-tech-lead, omn-dev-2-reviewer
- Last Updated: at emission in the solution-design-and-risk-assessment phase of run-7ae81c0de400, first emission; no prior identifier exists to preserve
- Upstream: the scope definition (digest sha256:1a2280e9723a272aae24f53bf714e6e9) and the execution plan (digest sha256:7e95924c287ab213ac057344621805cc) of this run. The statement register S-001 to S-025 of the plan is adopted as this design's statement register in the plan's order, cited here and restated nowhere; the scope's acceptance criteria are cited as "criterion" with their number and the scope's own decisions descriptively, because their identifier prefixes collide with this package's registers.
- Repository rescan: the task context names no relevant module or file, so a read-only rescan was made of the source, test and schema roots, the item 001 package and the previous release record in the feature worktree D:/Project/MyCompany/.worktrees/mc-10-feature at commit 969c124 on the feature branch for this wave. No datastore was read, no external system was reached and no repository file was written.

## Objective

- Desired outcome: one owner command turns a recorded item package into one rendered, decodable video file under one configured output root, its runtime measured from the file, every stage outcome, operation and artifact recorded on the datastore's clock, every call admitted against the item's cap at its worst case before it is made with that worst case held durably until the booking reconciles it, at USD 0.00 against fakes in a demonstration store, and switchable to the owner's billed vendors by configuration and owner-set credentials alone in the company store only; nothing publishes.
- Architectural objectives:
  - A completed capability call hands back the content it produced beside its operation record through the one capability boundary, and no other outcome carries content. Traces to S-003.
  - Each vendor path is an internal adapter of the capability assembly speaking the vendor's own request, authentication, response and usage shapes, its secret applied by the credential exchange under the account's recorded scheme and present only on the outgoing message. Traces to S-004, S-019.
  - An excluded service is refused by recorded routing data, the forbidden-source register and the route's recorded terms positions, before any request is sent. Traces to S-005.
  - Fake providers exist only in a demonstration composition that holds no network client and reads no credential variable, and a metered composition can construct none. Traces to S-006.
  - A store carries a recorded designation; fake mode runs only against a demonstration store and metered mode only against the designated company store whose identity matches configuration, and every demonstration figure is labelled by that designation. Traces to S-006, S-016, S-020.
  - Every call under an item cap is admitted against the item's remaining cap and the company ceiling at a worst case durably reserved before the call and reconciled by the booking, so an attempt lost, failed, timed out or unrecorded keeps counting at its worst case. Traces to S-014, S-015, S-021.
  - A route is priced when every unit kind its model is billed by has a price in force, and the datastore costs every billed kind. Traces to S-015, S-025.
  - Every timeout and hold on the produce path is enumerated with its value, holder and named end; no transaction or hold is open while an external process runs; every recorded instant is the datastore's. Traces to S-010, S-021.
  - Stage outcomes of a production are recorded once per stage on a new item version per production, in execution order, listed in the closed order, so a re-run never rewrites a recorded outcome. Traces to S-001, S-012.
  - Subject-specific content enters only as reviewable item material beside the package, quoting its sources; production source stays item-agnostic. Traces to S-008, S-009, S-018.
  - Exactly one type starts an external process and exactly one type writes files, both in a new production assembly the boundary suite pins. Traces to S-010, S-011, S-013.
  - The produce and prepare host commands compose either mode from configuration alone and refuse before any call naming each unmet precondition. Traces to S-001, S-016, S-017.
  - The brief and the dashboard read the produced item from the delivered single read, and the register records the owner's answers by supersession. Traces to S-020, S-022.
  - Every delivered boundary assertion and live test stays, extended and never weakened, and no quantity is invented. Traces to S-002, S-023, S-024.
- In scope:
  - The capability boundary's outcome, the three vendor adapters and three fakes, the credential scheme, the excluded-service refusal and the billed-kind pricing rule.
  - The per-item cap, its durable reservation and reconciliation, the channel-without-amount reading and the carried recovery items.
  - The store designation, the produce and prepare commands, the item package load and the item material.
  - The new production assembly with the Audio, Design, Thumbnail and Production handlers, the one process starter and the one file writer; the ninth schema resource.
  - The brief and dashboard lines, the register entries and the added boundary assertions.
- Out of scope:
  - Any metered call in a phase, test or demonstration, and the single metered run itself.
  - Publication, upload, channel creation or configuration, purchase, subscription and discharge of any coded precondition.
  - Stock footage, music, generated cutaways, the macro-frame thumbnail image and any reasoning or image call in item 001's run.
  - Re-admission of deferred requests, the price-capture rule, a register writer and every other open owner question.
  - Any change to the publication path, the release credential path or the delivered report computations.
  - Publication-grade finishing: editorial cut, phrase-level cue timing, mastering, captions and renditions.

## Requirements Summary

- Functional requirements:
  - One fake-mode command in a fresh demonstration store yields one rendered file, twelve stage outcomes on the production's item version, one operation per invocation and no regeneration of script, research or claims. Traces to S-001.
  - A completed call returns content whose recorded length and hash equal the received bytes; every other outcome returns none. Traces to S-003.
  - Narration is requested from the speech vendor in its own shapes, with the secret never exposed. Traces to S-004.
  - An excluded service is never selected and sends nothing. Traces to S-005.
  - Fakes are deterministic, zero cost, network-free and unreachable from metered mode. Traces to S-006.
  - Audio from the whole recorded narration with its duration measured from the audio; 22 code-drawn graphics; five code-drawn thumbnails and one placeholder; an assembly with labelled placeholders at 18 clip positions and no music. Traces to S-007, S-008, S-009, S-010.
  - One process starter located by configuration; one writer under one output root outside the repository; nothing publishes and the item stays held. Traces to S-011, S-012, S-013.
  - Every call admitted against the cap at its worst case; every attempt counts; the cap holds across runs and concurrent runs; a channel without an amount is governed by the ceiling and the cap. Traces to S-014.
  - Every metered operation booked from dated, sourced prices in every billed kind; an unreported usage never booked at zero. Traces to S-015.
  - Fake, plan-only and metered modes; the plan, estimate and cap printed first; metered refusals naming each condition; owner documentation of exact variable names. Traces to S-016, S-017, S-025.
  - The messages and image paths exist with the speech path's properties, unused by item 001's run. Traces to S-019.
  - Brief and dashboard lines for the produced item; register answers by supersession; recovered and unrecorded attempts counted and stopping production. Traces to S-020, S-021, S-022.
- Non-functional requirements:
  - USD 0.00, no real endpoint and the company store untouched in every phase. Traces to S-002.
  - Item-agnostic production source; nothing invented; delivered assertions and tests kept. Traces to S-018, S-023, S-024.
  - Every wait bounded and ended under a named outcome; every recorded instant on the datastore's clock. Traces to S-010, S-021.
- Acceptance criteria:
  - The fifty-nine criteria of the upstream scope definition and the eight plan-level criteria of the supplied execution plan are this design's acceptance intent, cited rather than restated; they bear on S-001, S-002, S-003, S-004, S-005, S-006, S-007, S-008, S-009, S-010, S-011, S-012, S-013, S-014, S-015, S-016, S-017, S-018, S-019, S-020, S-021, S-022, S-023, S-024 and S-025.
  - The task-level criteria of the nineteen design tasks the plan assigns to this agent, T-001 to T-019, are met by sections five, six, eight and nine; this bears on S-001 and S-021.

## Current-State Assumptions and Constraints

Paths below are relative to the feature worktree; each fact cites the site the rescan read. Gate rulings and owner decisions reached this phase only through the dispatch briefing and are registered as assumptions.

### 4.1 Facts

| ID | Fact | Established by |
|---|---|---|
| F-001 | The boundary's completed outcome carries an operation record and alerts and no content | src/MediaCompany.Capability/CapabilityGateway.cs:25 |
| F-002 | The provider adapter port and the attempt record are internal to the capability assembly; the attempt carries success, units, cost basis, duration, failure reason and signal, no content | src/MediaCompany.Capability/Providers/ProviderAdapter.cs:10-46 |
| F-003 | The only adapter posts a fixed body, reads usage from invented headers, discards the body and records a failed, timed-out or transport-failed attempt at zero units as a measurement; no test names it | src/MediaCompany.Capability/Providers/HttpProviderAdapter.cs:55-145; rescan of tests |
| F-004 | The boundary opens its admission transaction before resolution, reserves the booking instant, calls the provider with the transaction open, mints the operation identifier before the call and records non-cancellably after it | src/MediaCompany.Capability/CapabilityGateway.cs:144-404 |
| F-005 | A metered admission tries the company scope's and then the channel scope's transaction-scoped advisory lock without waiting, and is deferred as admission-in-progress when either is held | src/MediaCompany.Persistence/NpgsqlAdmissionLedger.cs:143-168; src/MediaCompany.Capability/CapabilityGateway.cs:153-229 |
| F-006 | An attempt whose admission transaction is lost is recorded on a fresh transaction under a fresh reservation, without scope holds, at a command timeout of twice the provider-call bound; failure there throws a named exception carrying operation, attribution, route, model, units and admitted instant | src/MediaCompany.Capability/CapabilityGateway.cs:414-476; src/MediaCompany.Capability/IncurredAttemptNotRecordedException.cs:8-76 |
| F-007 | The provider-call bound is the 60-second client timeout set in the composition root and passed to the boundary | src/MediaCompany.Host/CompositionRoot.cs:110; src/MediaCompany.Capability/CapabilityGatewayFactory.cs:62-66 |
| F-008 | No command timeout is set by the host, so the data source's 30-second default applies to every statement except the recovery transaction's | src/MediaCompany.Host/CompositionRoot.cs:52; src/MediaCompany.Persistence/NpgsqlUnitOfWork.cs:25-68 |
| F-009 | The management read is repeatable read and read only with a 2-second lock bound and a 5-second statement bound, each ending under a named outcome | src/MediaCompany.Persistence/NpgsqlCompanyRecordReader.cs:39-42, 97-128 |
| F-010 | A credential variable name is the prefix MEDIACOMPANY_SECRET_, the account identifier uppercased with hyphen and dot as underscore, a double underscore, then the channel identifier or GLOBAL where no channel is given | src/MediaCompany.Credentials/CredentialBrokerFactory.cs:25, 73-81 |
| F-011 | The boundary passes the request's attribution channel to every credential issuance, so the channel-scoped name is always used today | src/MediaCompany.Capability/CapabilityGateway.cs:342-353 |
| F-012 | A credential handle lives 5 minutes for one use, issued and checked on the process clock | src/MediaCompany.Credentials/CredentialBrokerFactory.cs:28; src/MediaCompany.Credentials/CredentialBroker.cs:188-218; src/MediaCompany.Host/CompositionRoot.cs:14-17, 102-106 |
| F-013 | The credential exchange attaches the secret only as a bearer authorization header; no per-vendor scheme exists | src/MediaCompany.Credentials/CredentialBroker.cs:210-275 |
| F-014 | The secret store port is internal to the credentials assembly, whose internals are visible to the capability assembly and its tests | src/MediaCompany.Credentials/CredentialBroker.cs:72; src/MediaCompany.Credentials/CredentialBrokerFactory.cs:3-4 |
| F-015 | The host reads one connection-string variable, offers check, install, registers, report, weekly and dashboard, and composes no provider endpoint | src/MediaCompany.Host/Program.cs:18-34, 174 |
| F-016 | The schema installer applies eight ordered resources with no database-name guard and no drop | src/MediaCompany.Persistence/SchemaInstaller.cs:19-78 |
| F-017 | Live tests destroy the schema of whatever database their connection variable names, identified only by convention | tests/MediaCompany.Persistence.Tests/DatastoreCollection.cs:20-44; tests/MediaCompany.Persistence.Tests/PostgresIntegrationTests.cs:41-42 |
| F-018 | No store designation table, column or check exists | rescan of db and src |
| F-019 | Price rows are temporal, carry source and verification date, and admit input, output, cached, per-operation and per-second kinds only | db/001-schema.sql:82-97 |
| F-020 | The operation row holds input, output, cached and other units; its stored generated cost covers the first three only | db/001-schema.sql:253-311 |
| F-021 | The recorder marks the cost not stated when a consumed kind lacks a price in force or any other unit is consumed, and meets a duplicate operation identifier with a named already-recorded exception | src/MediaCompany.Persistence/NpgsqlOperationRecorder.cs:112-216 |
| F-022 | A model enters the boundary's price table only when input, output and cached prices are all in force, and resolution removes unpriced routes under the price-not-in-force reason | src/MediaCompany.Capability/CapabilityGateway.cs:683-701; src/MediaCompany.Deterministic/Routing/RouteResolver.cs:182-192, 395-396 |
| F-023 | Resolution step 4 estimates a route from the request's estimated input, output and cached units, ignores other units, and keeps a route only when the estimate is within the request's ceiling and the headroom | src/MediaCompany.Deterministic/Routing/RouteResolver.cs:411-450 |
| F-024 | Headroom is the lesser of the channel budget's remaining amount, zero where no budget row exists, and the company allotment less booked spend | src/MediaCompany.Persistence/NpgsqlAdmissionLedger.cs:73-89 |
| F-025 | The controller refuses metered work for a channel with no amount and for unstated spend, defers at 90 percent and refuses at 100 percent; the company basis is a code constant of USD 34.42 | src/MediaCompany.Deterministic/Accounting/CostController.cs:62-185; src/MediaCompany.Domain/Accounting/Budget.cs:49-54 |
| F-026 | Budget rows require a positive amount and have channel or department scope only | db/001-schema.sql:325-334 |
| F-027 | The forbidden-source kinds are a closed set of five, matched by identifier against account, model or substitute, the kind carried on the refusal | src/MediaCompany.Domain/Capabilities/ForbiddenSource.cs:7-26; src/MediaCompany.Deterministic/Routing/RouteResolver.cs:452-478; db/001-schema.sql:144-169 |
| F-028 | Routes are one per capability per tier with a terms basis and verification date; provider accounts are one per provider | db/001-schema.sql:60-70, 113-138 |
| F-029 | The capability classes include narration, still images and editorial reasoning | src/MediaCompany.Domain/Capabilities/CapabilityClass.cs:7-18 |
| F-030 | Stage evidence is write-once with one row per item, version and stage, under a dossier header keyed by item and version that references the item only | db/005-sustained-rate.sql:41-64, 162-175 |
| F-031 | Stage evidence rows carry an instant the caller supplies from the process clock | src/MediaCompany.Persistence/NpgsqlDossierWriter.cs:53-68; src/MediaCompany.Deterministic/Services/ItemDossierRecorder.cs:125 |
| F-032 | The closed stage set lists Production before Audio and nothing enforces a recording order | src/MediaCompany.Domain/Production/ProductionStage.cs:12-47 |
| F-033 | The publish-ready predicate refuses a missing, pending or failed stage only; a held stage passes it | src/MediaCompany.Deterministic/Production/PublishReadyPredicate.cs:118-139 |
| F-034 | The copyright check handler records succeeded or failed only | src/MediaCompany.Deterministic/Production/CopyrightCheckStageHandler.cs:69 |
| F-035 | The barred-term screen holds ten terms in code and screens title, description and tags | src/MediaCompany.Deterministic/Production/BarredTermScreen.cs:23-101 |
| F-036 | The narration file is 11,096 characters only after line endings are normalised to line feeds, 11,185 raw, with no beat markers | wave-2/item-001/narration.txt; tools/verify_item_package.py:42, 144 |
| F-037 | The shot list specifies 22 graphics, 18 clips of which three are unsourced, six thumbnail candidates of which one is the macro-frame, no thumbnail file name or alternative text; the script marks 13 beats by paragraph cues and states the specified runtime 13 min 39 s from an assumed delivery rate | wave-2/item-001/shot-list.md; wave-2/item-001/script.md:62-153; wave-2/item-001/metadata-and-thumbnails.md:80-96 |
| F-038 | The item dossier records version 1 with Production, Audio and Copyright check held | wave-2/item-001/item-dossier.md:3, 13-28 |
| F-039 | No code reads the item package; item 001 was never loaded into a store | rescan of src and tests |
| F-040 | No production code writes a file, starts a process or records an artifact | rescan of src and db |
| F-041 | The boundary suite counts 39 facts and 24 theory cases, works by reflection and its own IL decoding, reads no source file, and scans process, file, clock and writer reach for eight management types only | tests/MediaCompany.Architecture.Tests/BoundaryTests.cs:1100-1248, 1371-1469 |
| F-042 | A type-name scan forbids transport, upload, egress and client fragments in every type of every production assembly, and exported capability names may not hold adapter or client | tests/MediaCompany.Architecture.Tests/BoundaryTests.cs:187-188, 323-346 |
| F-043 | The rule-determined assembly references only the domain and the ports, and its closure excludes the capability, credentials and persistence assemblies | tests/MediaCompany.Architecture.Tests/BoundaryTests.cs:70-103 |
| F-044 | The boundary checks run after the architecture project builds, also within the solution build, and a named property skips them | tests/MediaCompany.Architecture.Tests/MediaCompany.Architecture.Tests.csproj:24-33 |
| F-045 | Production configuration keys may not contain capability, transport, upload, gate or refusal fragments | src/MediaCompany.Application/Production/StageHandler.cs:92-116 |
| F-046 | Every production assembly carries version 1.5.0, asserted by the suite | tests/MediaCompany.Architecture.Tests/BoundaryTests.cs:860-861; Directory.Build.props |
| F-047 | The unit-of-work port carries a default-implemented overload, a precedent for additive port members that leave existing doubles compiling | src/MediaCompany.Application/Ports/PersistencePort.cs:16-35 |
| F-048 | The owner-decision register is write-once with supersession and carries open entries four, seven, seventeen, eighteen, nineteen, twenty and twenty-one | db/008-management.sql; wave-8/release-note.md known issues |
| F-049 | Writers hold the record horizon shared to commit and the audit chain head is taken at the first append, after a provider call returns | db/005-sustained-rate.sql:193-251; src/MediaCompany.Capability/CapabilityGateway.cs:69-78 |
| F-050 | A held outcome escalates at the reserved instant plus the request's hold timeout and holds nothing | db/008-management.sql:276-288; src/MediaCompany.Capability/CapabilityGateway.cs:195-229 |
| F-051 | The previous release record carries the recovered attempt booked without scope holds, the unrecorded incurred attempt not durable, the transaction held across a call up to 60 seconds and the recovery timeout question | wave-8/release-note.md known issues |
| F-052 | The external media tool, version 9.0.2, is installed in user scope and on the path of new shells; nothing in the host locates it | the dispatch briefing's environment facts; src/MediaCompany.Host/Program.cs |
| F-053 | Provider account and model identifiers are text; item identifiers are unique identifiers | src/MediaCompany.Domain/Identifiers.cs:31, 73, 78 |
| F-054 | The capability assembly's internals are visible to its own tests and to the persistence tests | src/MediaCompany.Capability/CapabilityGatewayFactory.cs:6, 12 |
| F-055 | Test doubles are named with a fake or test prefix or a less suffix, and no stub HTTP handler exists | tests/MediaCompany.Capability.Tests/Fakes.cs |
| F-056 | The script records the subject list committed to the audit | wave-2/item-001/script.md:28 |

### 4.2 Assumptions

| ID | Assumption | Why needed | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | The Scope Gate and Planning Gate rulings and the owner's decisions of 2026-10-09 are as the dispatch briefing restates them | Every decision rests on them | Cap, store, credential and render decisions change | omn-orchestrator |
| A-002 | The vendors' request, authentication, response and usage shapes are fixed from the implementing role's recorded knowledge in dated fixtures; the speech endpoint returns audio bytes with no usage in its body and bills by input characters | Adapters and booking need a shape | The first metered call fails or books wrong units | omn-orchestrator |
| A-003 | The speech vendor has a per-request text limit, recorded with its fixture source and date | The narration split needs a maximum | Requests are refused or split wrongly | omn-orchestrator |
| A-004 | The datastore version in use can change a stored generated column's expression in place, recomputing existing rows to the same value | The cost of new kinds is computed by the datastore | A second generated column carries the added kinds | omn-dev-1-implement |
| A-005 | The installed tool writes byte-identical stills for identical inputs with bit-exact output, one thread and a fixed font file | Graphics and thumbnail hashes repeat | Repeated-run hash criteria fail | omn-dev-1-implement |
| A-006 | A narration request at the configured character maximum completes inside the 60-second provider-call bound | The bound stays unchanged for speech | Requests time out after the vendor charged | omn-tech-lead |
| A-007 | The five structural absences are the four the suite labels plus the rule that no publishing key reaches a control | The new assertions must keep all five | An absence is unguarded | omn-tech-lead |
| A-008 | The owner's five answers of 2026-10-09 settle register entries four, seven, seventeen, eighteen and nineteen | The register transcription names them | The wrong entries are superseded | omn-product-owner |
| A-009 | A request's estimated units are upper bounds of what the vendor bills, exact for character-billed speech | The worst case must bound the charge | A booking exceeds its reservation | omn-orchestrator |
| A-010 | No delivered test asserts the transaction count of an admission or the absence of rows in tables this change adds | Capped admissions open a companion transaction | A delivered test fails | omn-dev-1-implement |
| A-011 | Each beat's opening paragraph and each cue can be quoted as a unique substring of the normalised narration | Beat spans come from the material | Placement falls back to a refusal naming the beat | omn-dev-1-implement |
| A-012 | The company store's identity is its database name, configured for the host, and only the orchestrator runs the preparation there | The metered guard compares identity | A wrong store is accepted or refused | omn-orchestrator |

### 4.3 Constraints

| ID | Class | Constraint | Hard or negotiable | Source |
|---|---|---|---|---|
| C-001 | functional | The per-item cap is never passed: every attempt that may have been charged, including one lost with its transaction or whose recording fails, counts before any later admission of the item decides | hard | S-014, S-021, A-001 |
| C-002 | structural | Every delivered contract, assertion and live test stays; the one reserved booking instant per admission is kept | hard | S-023, F-004 |
| C-003 | security | A fake is never constructible in a metered composition and no metered call falls back to one | hard | S-006 |
| C-004 | structural | The rule-determined assembly references only the domain and the ports | hard | F-043 |
| C-005 | security | Every phase, test and demonstration spends nothing, reaches no real endpoint and leaves the company store untouched | hard | S-002, A-001 |
| C-006 | compliance | Nothing publishes and the five structural absences stay | hard | S-012, A-007 |
| C-007 | structural | One type starts external processes, one type writes files, only under an output root outside the repository, identified without starting a process | hard | S-011, S-013, A-001 |
| C-008 | quality-attribute | No transaction or hold is open while an external process runs; every wait is bounded and ends named | hard | S-010, S-021 |
| C-009 | security | Metered mode accepts the designated company store only; fake mode accepts a demonstration store only | hard | S-016, A-001 |
| C-010 | security | A secret exists only on the outgoing message and in no output | hard | S-004 |
| C-011 | functional | No clip count, threshold, budget amount, configuration value or sizing claim is invented, and no fake figure is presented as observed | hard | S-024 |
| C-012 | functional | Production source holds no subject term and no item identifier | hard | S-018 |
| C-013 | functional | No reasoning or image call in item 001's run; code first | hard | S-001, S-009, A-001 |
| C-014 | quality-attribute | Narration, graphics and thumbnails repeat byte for byte across fake runs | negotiable | S-006, S-008 |
| C-015 | migration | Schema changes are one additive ordered resource; no stored row is rewritten or deleted | hard | S-023 |
| C-016 | operability | Executables, output root, repository root and company identity come from configuration, never from source | hard | S-011, S-013, S-016 |
| C-017 | structural | Minimum necessary change: reuse before new structure | negotiable | S-023 |

## Architecture and Component Design

### 5.1 Impacted Modules

| ID | Module | Impact type | Basis | Interfaces affected | Confidence |
|---|---|---|---|---|---|
| M-001 | Domain capabilities: CapabilityRequest, EstimatedUnits, ForbiddenSourceKind, RefusalReason | contract-change | F-022, F-023, F-027 | EstimatedUnits gains character and image units; two refusal reasons; one forbidden kind | confirmed |
| M-002 | Domain accounting and registry: UnitCounts, PriceUnitKind, ModelPrice, route and account records | contract-change | F-019, F-020, F-028 | Character and image unit kinds; model billed kinds; route terms positions; account credential scope and scheme | confirmed |
| M-003 | Domain production and dossier: new produced-item records | extension | F-030, F-040 | Artifact record, item material, production plan, item cap reading | confirmed |
| M-004 | Application ports: IAdmissionLedger, IDossierWriter and new ports | contract-change | F-047, F-031 | Default-implemented reservation member; stamped stage member; artifact, designation, package and media-tool ports | confirmed |
| M-005 | Capability boundary: ICapabilityGateway, CapabilityOutcome, CapabilityGateway | contract-change | F-001, F-004, F-006 | Completed carries content and a recovered marker; capped admissions reserve durably; unknown charges book at worst case | confirmed |
| M-006 | Capability providers: IProviderAdapter, ProviderAttempt, vendor adapters and fakes | behavior-change | F-002, F-003 | Attempt carries content and whether its charge is known; the generic adapter is retired | confirmed |
| M-007 | Capability composition: CapabilityGatewayFactory, ProviderEndpoint | contract-change | F-007, F-054 | Endpoint names its vendor contract; a demonstration factory member | confirmed |
| M-008 | Credentials: CredentialRequest, ScopedHandle, ICredentialExchange, CredentialBroker | contract-change | F-010, F-011, F-013 | Request and handle carry the account's scheme; exchange applies it | confirmed |
| M-009 | Deterministic accounting: CostController | behavior-change | F-025 | A channel without an amount under a recorded item cap reads no refusal | confirmed |
| M-010 | Deterministic routing: RouteResolver and ResolutionInputs | behavior-change | F-022, F-023, F-027 | Billed-kind pricing, worst case in every kind, item-cap filter, terms-position refusal | confirmed |
| M-011 | Deterministic production: PublishReadyPredicate | contract-change | F-033 | A held stage refuses under a new named refusal | confirmed |
| M-012 | Deterministic production: new pure members | extension | F-036, F-037 | Narration splitter, beat timeline composer, material check, plan composer, store guard | confirmed |
| M-013 | Persistence: NpgsqlAdmissionLedger | behavior-change | F-005, F-024 | Snapshot reads cap, counted total and open reservations; reservation written on a companion transaction | confirmed |
| M-014 | Persistence: NpgsqlOperationRecorder | behavior-change | F-020, F-021 | Costs character and image units | confirmed |
| M-015 | Persistence: new adapters and NpgsqlDossierWriter | extension | F-030, F-031, F-039 | Artifact recorder, designation reader, package and preparation writer, cap register, stamped stage write | confirmed |
| M-016 | Schema resources: the ninth ordered resource | contract-change | F-016, F-019, F-020, F-048 | New tables, kinds, columns and register entries | confirmed |
| M-017 | Persistence: SchemaInstaller | behavior-change | F-016 | Applies nine resources | confirmed |
| M-018 | Host: Program and CompositionRoot | contract-change | F-007, F-015 | Produce and prepare commands; settings; mode composition | confirmed |
| M-019 | New assembly MediaCompany.Production: ProduceItemService and the Audio, Design, Thumbnail and Production handlers | extension | F-040, F-043 | IStageHandler implementations; produce service | confirmed |
| M-020 | MediaCompany.Production: ExternalMediaTool, the one process starter | extension | F-040, F-052 | Media-tool port realisation | confirmed |
| M-021 | MediaCompany.Production: ArtifactWriter, the one file writer | extension | F-040 | Artifact-store port realisation | confirmed |
| M-022 | Architecture suite: BoundaryTests | behavior-change | F-041, F-042, F-044 | Added and extended assertions | confirmed |
| M-023 | Company record read and management composers | behavior-change | F-009 | Produced-item lines in the one read | confirmed |
| M-024 | Item material file beside the item 001 package | extension | F-036, F-037, F-056 | A reviewable data file | confirmed |
| M-025 | Recorded preparation configuration and the owner guide | extension | F-010, F-015 | Repository documents and a sample configuration | confirmed |
| M-026 | Test projects: capability, persistence and a production test project | extension | F-054, F-055 | Stub vendor handler, stand-in tool, doubles | confirmed |
| M-027 | Publication path: Deterministic.Publication and the publication services | no-change-verified | F-042 | none | confirmed |
| M-028 | BarredTermScreen | no-change-verified | F-035 | none; reused for file names and recorded alternative text | confirmed |
| M-029 | Credential release path | no-change-verified | F-013 | none | confirmed |
| M-030 | Item 001 recorded package files | no-change-verified | F-036, F-038 | none; read only | confirmed |
| M-031 | Directory.Build.props version | behavior-change | F-046 | Version string, if the proposal is ruled | speculative |

Boundary crossings: host to datastore (designation, preparation, admission, booking, artifacts); capability boundary to vendor endpoints over the network, metered composition only; production assembly to the operating system through the one process starter and the one writer; credential exchange to the process environment, metered composition only.

### 5.2 Options Considered

| Option | Structural change | Hard constraints | Impact surface | Reuse leverage | Quality attributes | Migration burden | Operability | Outcome |
|---|---|---|---|---|---|---|---|---|
| O-001 | Content on the completed outcome; internal vendor adapters with per-account scheme; fakes only in a demonstration factory; durable reservation on a companion transaction for capped admissions; store designation; per-production item versions; item material file; a new production assembly holding the produce service, handlers, the one process starter and the one writer; stills and timeline through the installed tool; ninth resource | Satisfies every hard constraint, C-001 to C-016 | 9 contract-change, 0 dependency-change | 32 rows | Library testable apart from the executable; host stays a composition root | 9 contract changes | Two commands, nine settings | selected |
| O-002 | As O-001, but the produce service, handlers, process starter and writer live in the host executable | Satisfies every hard constraint, C-001 to C-016 | 9 contract-change, 0 dependency-change | 32 rows | Production path testable only by referencing the executable; the host stops being composition only | 9 contract changes | Same | not selected, loses on quality under C-017 |
| O-003 | As O-001, but the cap is held only by the delivered scope holds, with no durable reservation | Violates C-001: a lost or unrecorded attempt is invisible to the next admission | 9 contract-change, 0 dependency-change | 32 rows | Window after a loss | 9 contract changes | None added | eliminated: violates C-001 |
| O-004 | As O-001, but no transaction is open during the call and admission and booking take two instants | Violates C-002: the one reserved booking instant is dropped | 10 contract-change, 0 dependency-change | 30 rows | Shorter holds | 10 contract changes | Month-boundary bookings | eliminated: violates C-002 |
| O-005 | As O-001, but fakes are built by the metered factory for endpoints carrying a reserved value | Violates C-003: a fake is constructible in a metered composition | 9 contract-change, 0 dependency-change | 32 rows | Mode by value, not by construction | 9 contract changes | One factory | eliminated: violates C-003 |
| O-006 | As O-001, but re-runs record in a separate attempt record beside dossier version 1 | Satisfies every hard constraint, C-001 to C-016 | 9 contract-change, 0 dependency-change | 31 rows: the dossier version key is not reused | Two places hold a stage outcome | 9 contract changes | Readers join two records | not selected, loses on reuse under C-017 |
| O-007 | As O-001, but stills are drawn in process by a new drawing package | Satisfies every hard constraint, C-001 to C-016 | 9 contract-change, 1 dependency-change | 31 rows: the installed tool is not reused for stills | Font and renderer behaviour from a second engine | 9 contract changes | A new package to keep current | not selected, loses on impact under C-017 |
| O-008 | As O-001, but the Audio and other handlers sit in the rule-determined assembly | Violates C-004: the Audio handler must reach the capability boundary | 9 contract-change, 1 dependency-change | 32 rows | Breaks a delivered assertion | 9 contract changes | None added | eliminated: violates C-004 |
| O-009 | As O-001, but the tracked tree is found by asking version control | Violates C-007: a second process-starting site | 9 contract-change, 0 dependency-change | 32 rows | Exact tracked-tree test | 9 contract changes | Needs version control installed | eliminated: violates C-007 |

### 5.3 Selected Approach

- Selected: O-001, under every hard constraint, with the production path in a new assembly.
- Structural change:
  - Content (D-001). CapabilityOutcome.Completed gains an optional content member, absent by default so every delivered construction compiles: the content kind (text, audio or image), the media type the vendor declared, the bytes as one read-only buffer, their length and their SHA-256 hash. ProviderAttempt gains the same member and a charge-known flag. The adapter reads the response body once into one buffer, hashes that buffer and hands that buffer on; no second copy is made or hashed. Held, Refused and Substituted carry no content by construction because only Completed has the member; a failed attempt is Completed with outcome failed in its operation and no content. Content size is bounded by the client's response buffer limit, set in the composition root. The operation record does not store content; the caller's artifact record stores length and hash.
  - Vendor contracts (D-002). Three internal sealed adapters in the providers namespace: the speech path and the image path of the speech-and-image vendor and the messages path of the reasoning vendor. Each builds the vendor's request body from the request's typed payload, posts to the endpoint path in its fixture, and parses the body: audio bytes for speech, decoded image bytes and usage for images, text and usage for messages. Usage is read only from the response body; a body without usage marks the charge unknown. A timeout, transport failure, non-success status or usage-less success marks the charge unknown; a credential refusal before sending marks it known at zero because nothing was sent. CapabilityRequest gains an optional typed payload (narration text with voice and format; prompt text with an output-unit bound; image prompt with size) so no adapter invents a body. ProviderEndpoint gains the vendor contract it serves; the factory builds the matching adapter. The delivered generic adapter is retired (F-003). Each account records its credential scope (company or channel) and its authentication scheme; the boundary passes no channel for a company-level account, so its variable name is the GLOBAL form (F-010); CredentialRequest and ScopedHandle carry the scheme; the exchange applies either a bearer authorization header or the vendor's key header, and the adapter adds the vendor's non-secret version header. No public member returns the secret; the exchange remains the only writer of it. Fixtures are recorded by the implementing role with source and date; first-hand verification stays the orchestrator's before the go (A-002).
  - Excluded services (D-003). ForbiddenSourceKind gains a customer-content-licence member beside the delivered automated-access member. Each route gains two recorded terms positions, automated access (permits, prohibits, not recorded) and customer content (no licence taken, licence taken, not recorded), each with an evidence reference and date beside the delivered terms basis (F-028). Resolution step 1 refuses under the forbidden-source reason, carrying the matching kind, when the register names the route's account or model as delivered, or when a provider route's recorded position prohibits automated access or takes a licence, or when a narration or still-image route records either position as not recorded. A refusal sends nothing and is recorded with its zero-cost operation as delivered.
  - Fakes (D-004). Three internal sealed fake adapters, one per vendor contract, in the capability assembly. CapabilityGatewayFactory gains a demonstration member taking only the unit of work: it builds the fakes, a broker over an internal demonstration secret store returning a fixed non-secret placeholder (F-014), and no network client; it reads no credential variable. The metered member builds vendor adapters only and has no fake type in reach. Fakes key on the same accounts as the real route rows, because the schema allows one account per provider (F-028). Each fake returns content as a pure function of the request payload: speech returns a mono PCM wave whose sample count is a fixed function of the text length and whose samples are a fixed function of its characters; images return a fixed-size PNG from a fixed palette; messages return a fixed text. Each reports explicit zero units measured, so its operation costs zero and is stated.
  - Store designation (D-005). The ninth resource adds a one-row write-once designation record: demonstration or company, with the datastore's recording instant. The test kit and the demonstration preparation write demonstration; only the orchestrator's preparation against the company store writes company. A store guard in M-012, a pure function over the designation read, the connected database name and the configured company identity, admits fake mode only on demonstration and metered mode only on company with matching identity; an absent designation refuses both. Every reader labels figures from a demonstration store as demonstration and never as observed; fakes record no benchmark observation, so comparable-run counts stay zero. The metered path below the host command is tested through ProduceItemService with the vendor adapters over a stub handler against a demonstration store, which the service accepts because the guard sits in the host command only; the guard itself is tested over values, never connecting to the company store.
  - Per-item cap (D-006). The ninth resource adds a write-once item cap record (item, amount, currency, source statement naming the owner's decision, recording instant) and a write-once reservation record (operation identifier, item, route, model, worst-case units by kind, worst-case amount, admitted instant). The admission snapshot reads, in its one statement, the item's cap, its counted total (stated cost of its booked operations over all time plus the worst case of every reservation without a booked operation) and open reservations within the company's month. Resolution removes a route whose worst case passes the item's remaining cap under a new item-cap refusal naming cap, counted total and worst case; headroom subtracts open reservations, and for a channel with no budget row it is the company remaining rather than zero (F-024). For a capped item the controller's channel reading with no amount takes no action and records no amount, so no channel deferral applies; the company reading still defers and refuses. When the selected route is a provider route of a capped item, the boundary writes the reservation on a companion transaction from the unit of work and commits it before issuing the credential, while the admission transaction keeps its holds and its reserved instant; the booking on the admission transaction reconciles by inserting the operation under the reservation's identifier. An attempt whose charge is unknown is booked at the reservation's worst-case units with the estimate basis. A reservation whose operation is never booked keeps counting at its worst case. Uncapped admissions keep the delivered path exactly (C-002).
  - Pricing (D-007). PriceUnitKind gains character and image kinds; models gain a recorded set of billed kinds, absent meaning input, output and cached as delivered. A model enters the price table when every billed kind has a price in force; an unbilled kind needs none. UnitCounts and EstimatedUnits gain character and image units. The operation row gains character and image units and their applied prices, and the stored cost covers all five kinds (A-004); a consumed kind without a price leaves the cost not stated as delivered. The plan's estimate is the same arithmetic over the configured prices in force, labelled estimate and never netted with development cost.
  - Time, holds and clocks (D-008). The enumeration and the proposals for ruling:
    - Default command timeout, 30 seconds, every statement except the recovery transaction's; ends in a datastore timeout that the produce service names as a datastore failure of the stage.
    - Provider-call bound, 60 seconds, held by the admission transaction with the record horizon shared, the company and channel scope locks and, for a capped item, nothing on the companion transaction, which has committed; ends as a charge-unknown attempt booked at worst case. Proposed for ruling: unchanged for every vendor path, narration requests limited by a configured character maximum (A-006).
    - Recovery command timeout, 120 seconds, twice the bound; ends in the named unrecorded-attempt failure; its reservation keeps counting.
    - Scope locks, tried and never waited on; held to the admission's commit; a contender ends deferred.
    - Record horizon, shared by every writer to its commit; the exclusive throughput read does not wait.
    - Audit chain head, exclusive from the first append after the call to commit, waited under the 30-second timeout.
    - Stage-record key, operation key, artifact key and reservation key, each waited only behind an uncommitted insert of the same key, under the 30-second timeout; a duplicate ends named.
    - Management read, 2-second lock and 5-second statement bounds, unchanged.
    - Held request's hold timeout: holds nothing; the produce service stops at a held outcome and never waits on it.
    - Credential handle, 5 minutes and one use on the process clock; issued after the reservation commits and used within the call; ends refused with nothing sent.
    - Companion reservation transaction, 30-second timeout; a failure before the call ends the stage with no call.
    - Render bound, proposed 60 minutes, monotonic elapsed time, no transaction open; ends by terminating the process tree, discarding staging and recording Production failed naming the bound.
    - Probe bound, proposed 60 seconds per probe; still bound, proposed 60 seconds per still; decode bound, proposed 60 minutes; each ends like the render.
    - Clocks: operations, reservations, admission and cap decisions take the reserved datastore instant; stage rows of a production, artifact rows, production version headers, cap and designation rows take the datastore's insert instant; the process clock decides only handle validity and is never recorded as an instant of order; bounds use monotonic elapsed time; runtime and durations come only from probes of the produced files.
  - Narration (D-009, inline). The loader normalises the narration's line endings to line feeds and nothing else, matching the package verifier (F-036), and records its length and hash. The splitter cuts first at beat boundaries from the item material, then within a beat at paragraph, then sentence boundaries, keeping each separator at the end of the preceding part, so the parts concatenate to the recorded text exactly; a sentence above the maximum refuses the plan, naming it. Each part is one operation; its worst case is its character count times the character price in force. Audio parts are concatenated by one decode and encode with no inserted silence; each part, each beat and the whole are measured by probe; no rate assumption derives any duration.
  - Stage outcomes (D-010). Each production opens a new dossier version, one above the highest recorded for the item, writing a version header naming the production's mode; a concurrent production meets the version key and ends named. The eight carried stages are recorded with the outcomes version 1 records, each citing it; the four produced stages are recorded when each ends, in execution order Design, Audio, Thumbnail, Production, because assembly consumes narration; the closed set and its listing order are unchanged and every reader lists by it. Production ends held naming the unsourced clips after a clean render, or failed; Thumbnail ends held naming the placeholder candidate; a cap stop records the stage held naming the cap and total. IDossierWriter gains a member whose instant is the datastore's. The predicate gains a held-stage refusal naming each held stage and its summary. A re-run opens a further version; no recorded outcome is rewritten.
  - Item material (D-011). A JSON file beside the package holds, quoted from the package: the subject terms from the script's subject list; each beat's number, title and opening paragraph quoted from the narration; each graphic's identifier, beat, title and displayed lines, each numeral or named quantity carrying the record and quotation it comes from; each clip's identifier, beat and label; each thumbnail candidate's identifier, its displayed text quoted from the record, and its alternative text only where recorded, otherwise recorded absent; the macro-frame candidate marked placeholder. The loader checks every quotation against its source and refuses naming the first mismatch. Graphics and placeholders of a beat divide that beat's measured span equally in shot-list order.
  - Placement (D-012). MediaCompany.Production references the domain, the ports, the rule-determined assembly and the capability assembly; it holds ProduceItemService, the four handlers, ExternalMediaTool and ArtifactWriter. The host references it and composes it.
  - Process boundary (D-013). ExternalMediaTool is the only type starting a process. It starts two executables, the renderer and the probe, each from its own setting, refusing before any capability call when a setting is absent or names no file. Argument classes: still drawing, narration concatenation, render, probe of streams and duration, full decode to a null sink with errors counted. Arguments are passed as a list, never a shell line; text reaches the tool through files under staging, never through arguments; inputs are files under the output root plus the configured font file. Its working directory is a staging folder under the output root; each run records the tool's version line. A render uses the configured render profile, proposed for ruling at 1920 by 1080 pixels and 30 frames per second, so one frame is one thirtieth of a second.
  - Artifact store (D-014). ArtifactWriter is the only type writing files. It refuses an output root that is inside, equal to or containing the configured repository root by comparing full normalised paths, case-insensitively on this platform, starting no process. It writes content to a staging file, flushes to disk, closes, moves it into place, re-reads the stored file to hash it, and only then the artifact is recorded with item, version, stage, role, relative path, length, hash and the datastore's instant. A tool-written file is written into staging, promoted and hashed by the writer after the tool exits. An interrupted write leaves a staging file and no record; staging is cleared at the next run's start.
  - Produce command (D-015). The command takes an item and one of fake, plan-only or metered. It reads the designation and the package, composes the plan, and prints before any call: each planned operation with its route, worst-case units and estimated cost, the total labelled estimate, the cap with its source and the counted total, and the bounds in force. Plan-only stops there with no call and no file. Fake mode composes the demonstration factory. Metered mode refuses before any call, naming each unmet condition: a missing credential variable for a planned account, printed by its exact name; no cap recorded; a demonstration, undesignated or misidentified store; an unpriced planned route; a controller refusal or deferral read from the snapshot; a fake configured for a planned account. One build serves all modes.
  - Preparation (D-016). A prepare command loads, from a recorded configuration file and the package folder, the company, the channel without any budget amount or configuration value, the accounts with fixed identifiers, scope and scheme, the models with billed kinds, the routes with terms positions, the prices with source and date, the item, dossier version 1 with its recorded components and twelve outcomes, the item material, the cap and the designation. It inserts where absent and refuses naming any differing row. The orchestrator runs it against the company store after the install; phases run it only against demonstration stores. The exact variable names follow from the fixed account identifiers and are printed by the guide and by the metered refusal alike.
  - Brief and dashboard (D-017, inline). The one read gains the latest production per item: its stage outcomes, operation count and booked amount, cap, counted total and remaining, open reservations, measured runtime beside the specified one, each figure labelled demonstration in a demonstration store; with no production the line reads not recorded naming the register.
  - Recovery (D-018, inline). The recovered outcome carries a recovered marker; the produce service makes no further call after it or after the unrecorded-attempt failure, whose facts are printed and whose reservation remains durable in the store. The recovery timeout stays twice the bound.
  - Register (D-019, inline). The ninth resource inserts decided entries superseding entries four, seven, seventeen, eighteen and nineteen, citing the owner's decisions of 2026-10-09; twenty and twenty-one stay open; no report computation changes.
  - Assertions (D-020, inline). Added: one process starter across all production assemblies with a probe; one file writer with a probe; no hard-coded executable path; subject terms from the item material absent from production assembly strings, names and source text; no reach from the produce service to publication, dispatch, release credential or channel-configuration types; the production assembly added to the upload, transport and network theories; fakes internal and sealed. Delivered assertions unchanged except the assembly lists, which grow.
- Rationale: O-001, O-002, O-006 and O-007 satisfy every hard constraint. O-007 carries one dependency-change module more; O-001, O-002 and O-006 tie at nine contract-change modules; O-006 has 31 reuse rows against 32; O-001 and O-002 tie on reuse and O-001 leads on quality attributes, keeping the host a composition root and the production path testable as a library.
- Highest-scoring rejected alternative and why it lost: O-002 ties O-001 on hard constraints, impact surface and reuse; it loses on quality because the production path, the process starter and the writer would be reachable only by referencing the executable and the host would cease to be composition only.
- Tradeoffs accepted:
  - Every narration booking carries the estimate basis because the speech response reports no usage (A-002); recorded as R-007.
  - A reservation whose operation is never booked counts at its worst case until an owner-directed settlement exists; recorded as R-005.
  - Uncapped admissions keep the delivered path without a durable reservation; recorded as R-004.
  - The item cap filter adds a refusal reason readers must list; recorded as R-012.
  - A production's item version differs from the version the package records, so readers name the version; recorded as R-010.

### 5.4 Decisions

| ID | Decision | Architecture-significant | Record |
|---|---|---|---|
| D-001 | The completed outcome carries content, length and hash over the one buffer received, under O-001 | Yes | architecture-decision-record-D-001.md |
| D-002 | Three internal vendor adapters with body-read usage, charge-unknown marking, per-account scheme and company-level names, under O-001 | Yes | architecture-decision-record-D-002.md |
| D-003 | Excluded services refused by a new forbidden kind and recorded route terms positions, under O-001 | Yes | architecture-decision-record-D-003.md |
| D-004 | Fakes built only by a demonstration factory with no network client and no credential read, under O-001 | Yes | architecture-decision-record-D-004.md |
| D-005 | A write-once store designation with a host guard per mode and labelling by designation, under O-001 | Yes | architecture-decision-record-D-005.md |
| D-006 | A recorded item cap with a durable worst-case reservation on a companion transaction reconciled by the booking, under O-001 | Yes | architecture-decision-record-D-006.md |
| D-007 | Character and image kinds, recorded billed kinds and the datastore costing all five, under O-001 | Yes | architecture-decision-record-D-007.md |
| D-008 | The timeout, hold and clock enumeration with proposed bounds for ruling, under O-001 | Yes | architecture-decision-record-D-008.md |
| D-009 | Narration split at beat, paragraph and sentence boundaries, durations by probe, under O-001 | No | None; recorded inline in 5.3 |
| D-010 | A new item version per production, execution-order recording on the datastore clock, and a held-stage refusal, under O-001 | Yes | architecture-decision-record-D-010.md |
| D-011 | Item material as a JSON file beside the package quoting its sources, under O-001 | Yes | architecture-decision-record-D-011.md |
| D-012 | The production path in a new MediaCompany.Production assembly, under O-001 | Yes | architecture-decision-record-D-012.md |
| D-013 | One process starter for renderer and probe, located by settings, under O-001 | Yes | architecture-decision-record-D-013.md |
| D-014 | One writer: staging, flush, promote, re-read hash, then record; repository containment refusal, under O-001 | Yes | architecture-decision-record-D-014.md |
| D-015 | The produce command's modes, printed plan and named refusals, under O-001 | Yes | architecture-decision-record-D-015.md |
| D-016 | The prepare command loading recorded configuration and the package, under O-001 | Yes | architecture-decision-record-D-016.md |
| D-017 | Produced-item lines in the one read, labelled by designation, under O-001 | No | None; recorded inline in 5.3 |
| D-018 | Recovered marker stops production; unrecorded attempts stay reserved, under O-001 | No | None; recorded inline in 5.3 |
| D-019 | Register answers by supersession in the ninth resource, under O-001 | No | None; recorded inline in 5.3 |
| D-020 | The added boundary assertions, under O-001 | No | None; recorded inline in 5.3 |
| D-021 | The version is proposed as 1.6.0 for ruling, under O-001 | No | None; routed as Q-007 |

## API and Data Model Impact

- API changes:
  - M-001: EstimatedUnits gains character and image units, defaulting to zero; CapabilityRequest gains an optional typed payload; RefusalReason gains the item-cap reason; ForbiddenSourceKind gains the customer-content-licence kind.
  - M-002: UnitCounts gains character and image units by init members; PriceUnitKind gains two kinds; registry records gain billed kinds, terms positions, credential scope and scheme.
  - M-004: IAdmissionLedger gains a reservation member with a default implementation; IDossierWriter gains a datastore-stamped stage member; new ports for artifacts, designation, package and preparation, the media tool and the artifact store.
  - M-005 and M-006: Completed gains content and a recovered marker; ProviderAttempt gains content and charge-known.
  - M-007: ProviderEndpoint gains its vendor contract; the factory gains a demonstration member.
  - M-008: CredentialRequest and ScopedHandle gain the scheme; the exchange applies it.
  - M-011: PublishReadyRefusal gains the held-stage member.
  - M-018: the host gains produce and prepare commands and nine settings.
- Contract compatibility notes:
  - M-001, M-002, M-005, M-006, M-008 transition strategy. Current shape: delivered records and unions. Target shape: added optional members with defaults. Compatibility approach: every addition is an init member or default-valued, so delivered constructions and pattern matches compile and behave as before. Coexistence period: none. Retirement condition: none. Rollback position: reverting the members with the code that sets them.
  - M-004 transition strategy. Current shape: delivered ports. Target shape: additive members, default-implemented where delivered doubles implement the port, after the delivered overload precedent (F-047). Coexistence period: none. Retirement condition: none. Rollback position: removing the members and their realisations.
  - M-007 transition strategy. Current shape: endpoints build the generic adapter. Target shape: endpoints name a contract; the generic adapter is retired. Compatibility approach: no delivered caller passes an endpoint (F-015). Coexistence period: none. Retirement condition: the generic adapter is removed in this change. Rollback position: restoring it.
  - M-011 transition strategy. Current shape: held passes. Target shape: held refuses by name. Compatibility approach: no delivered test or row relies on held passing (F-033 rescan). Coexistence period: none. Retirement condition: none. Rollback position: removing the refusal.
  - M-016 transition strategy. Current shape: eight ordered resources. Target shape: a ninth appended, creating objects only where absent. Coexistence period: none. Retirement condition: none. Rollback position: dropping the ninth resource's objects in reverse order, lossless while they hold only demonstration rows.
  - M-018 transition strategy. Current shape: six commands. Target shape: eight. Compatibility approach: delivered commands unchanged. Coexistence period: none. Retirement condition: none. Rollback position: removing the two commands.
- Schema or migration changes:
  - New write-once tables for the designation, item caps, reservations and artifacts, and for production version headers; direction forward; reversible by dropping them; readers before the resource read not recorded and both production modes refuse naming the missing resource.
  - Price and operation unit kinds widened to character and image; operation columns for those units and their applied prices with zero defaults; the stored cost's expression covers five kinds (A-004); direction forward; reversible while no row carries the new kinds; existing rows keep their values and their cost.
  - Model billed kinds and route terms positions and account scope and scheme added as nullable columns; absent reads as delivered behaviour; direction forward; reversible by dropping the columns.
  - Forbidden kind and refusal reason checks widened; register decided entries inserted where absent, superseding five open entries; direction forward; reversible by dropping the new entries while no reader depends on them. Writers during transition: only the new code writes the new columns and tables.

## Reusable Components and Reuse Rationale

| Capability | Candidate | Outcome | Rationale |
|---|---|---|---|
| One boundary to every capability | ICapabilityGateway and CapabilityGateway | reuse-extended | Content and the reservation are added inside the delivered admission (F-004) |
| One-statement admission snapshot | NpgsqlAdmissionLedger read | reuse-extended | Cap, counted total and open reservations join the same statement (F-005) |
| Scope serialisation without waiting | The tried scope locks | reuse-as-is | Unchanged (F-005) |
| One booking instant and one operation identifier | The reserved instant and pre-minted identifier | reuse-as-is | The reservation keys on the same identifier (F-004) |
| Recovery after a lost transaction | The recovery path and its named failure | reuse-extended | Marks the outcome recovered; the reservation makes it count (F-006) |
| Worst-case filter before a call | Resolution step 4 | reuse-extended | Five kinds, open reservations and the item cap (F-023) |
| Spend thresholds | CostController | reuse-extended | Capped channel with no amount (F-025) |
| Sourced, dated temporal prices | The price rows | reuse-extended | Two kinds added (F-019) |
| Exact cost arithmetic | The stored generated cost | reuse-extended | Five kinds (F-020) |
| Stated or unstated cost | The recorder's verdict | reuse-extended | Two kinds priced (F-021) |
| Excluded-service refusal | The forbidden-source register and step 1 | reuse-extended | A sixth kind and route positions (F-027) |
| Route terms record | The routes terms basis and date | reuse-extended | Two positions with evidence (F-028) |
| Scoped one-use credential | CredentialBroker and the GLOBAL name form | reuse-extended | Scheme and company scope (F-010) |
| Credential-free fake issuance | The internal secret store port | reuse-extended | A demonstration store inside the boundary (F-014) |
| One outcome per stage per version | The dossier stage evidence | reuse-extended | A datastore-stamped member and a version per production (F-030) |
| Opening a dossier version | ItemDossierRecorder | reuse-extended | Opens production versions (F-031) |
| Holding an item short of publish-ready | PublishReadyPredicate | reuse-extended | Held refuses (F-033) |
| Barred-term screening | BarredTermScreen | reuse-as-is | Screens file names and recorded alternative text as metadata surfaces (F-035) |
| Write-once and horizon checks | The delivered amendment triggers | reuse-as-is | Applied to the new tables (F-049) |
| Owner register with supersession | The register | reuse-as-is | New decided entries (F-048) |
| One bounded management read | NpgsqlCompanyRecordReader | reuse-extended | Produced-item statements (F-009) |
| Four-case lines | The management composers and rendering | reuse-extended | Produced-item lines |
| Ordered schema install | SchemaInstaller | reuse-extended | Ninth resource (F-016) |
| Reflection and IL scans | BoundaryTests helpers | reuse-extended | New assertions reuse the decoder (F-041) |
| Starting the external tool | System.Diagnostics.Process in the standard library | reuse-as-is | Wrapped by the one starter; no existing component starts processes (F-040) |
| Media drawing, concatenation, render, probe, decode | The installed external tool | reuse-as-is | Already installed (F-052) |
| Hashing stored bytes | SHA-256 in the standard library | reuse-as-is | As the audit chain uses |
| Atomic file placement | File streams and moves in the standard library | reuse-as-is | Flush to disk then move |
| Reading item material and configuration | System.Text.Json in the standard library | reuse-as-is | No new package |
| Bounded network calls | The client with its 60-second timeout | reuse-as-is | Provider-call bound (F-007) |
| Command exit discipline | ManagementConsole exit codes | reuse-extended | Produce exits named |
| Line-ending rule for narration | The package verifier's normalisation | reuse-as-is | The rule, not the script (F-036) |
| Deterministic fake audio | Existing components, the standard library, the platform and installed dependencies | none-found | No audio encoder in code or the standard library; the tool may not run inside a fake; a small wave writer inside the fake |
| In-process still drawing | A new drawing package | rejected | A new dependency where the installed tool serves (O-007) |
| Tracked-tree test | Version control | rejected | Starts a second process (O-009) |
| Generic provider adapter | HttpProviderAdapter | rejected | Fixed body, invented headers, zero-unit failures (F-003) |

## Operational Considerations

- Logging and observability updates: the produce command prints the plan, each stage's start and named end, each operation's identifier, outcome and booked amount, each artifact's path, length and hash, the tool's version line and the measured runtime beside the specified one, labelled measured or specified (M-018, M-019); no content, prompt body or secret is printed.
- Error handling strategy: every failure ends under a named outcome: refusals before any call with exit 2 naming each condition; a stage failure records the stage failed and exits 3; a cap stop records the stage held naming cap and total and exits 4; an unrecorded attempt prints its facts and exits 5 (M-018, M-019, C-008).
- Security considerations: secrets stay on the outgoing message only (C-010, M-008); fake mode reads no credential variable (M-007); vendor endpoints come from recorded configuration and are reached only by the metered composition (C-005); the process starter takes arguments as a list and text through files, and executables only from settings validated as existing files (M-020, C-016); excluded services are refused by data (M-010); nothing publishes and the release credential path is unchanged (M-029, C-006).
- Performance considerations: metered calls stay serialised company-wide one at a time (F-005), acceptable for one item; content is buffered once per call; render and decode run outside every transaction under their bounds (C-008); no throughput figure is claimed (C-011).
- Deployment and operability: the orchestrator installs nine resources and runs prepare against the company store before the go; settings are the connection string, company identity, output root, repository root, renderer, probe, font file, render profile and bounds (M-018, M-025).

## Delivery Plan

### Sequencing Constraints

| ID | Constraint | Modules | Prerequisites | Reason | Binds |
|---|---|---|---|---|---|
| P-001 | The domain records and port contracts exist before any realisation or consumer | M-001, M-002, M-003, M-004 | none | Contract definition precedes consumers | T-001, T-002, T-003, T-004, T-005, T-006, T-007, T-010, T-011, T-013, T-020 |
| P-002 | The ninth resource and installer exist before any adapter writes its tables | M-016, M-017 | P-001 | Schema precedes writers | T-015, T-018, T-022, T-023, T-034, T-039 |
| P-003 | The rule-determined changes exist before the boundary decides by them | M-009, M-010, M-011, M-012 | P-001 | Rules precede the boundary using them | T-009, T-014, T-024 |
| P-004 | The persistence adapters read and write the new records before the boundary or loader uses them | M-013, M-014, M-015 | P-002 | Adapters precede callers | T-016, T-026 |
| P-005 | The credential scheme exists before any vendor adapter attaches a secret | M-008 | P-001 | The exchange precedes its callers | T-033 |
| P-006 | The boundary's content, reservation and charge-unknown booking exist before any stage calls it | M-005, M-006, M-007 | P-003, P-004, P-005 | Boundary verified before work assumes it | T-017, T-021, T-036, T-037, T-043, T-044, T-045, T-059 |
| P-007 | The Design Gate rulings on the call, render, probe, still and decode bounds and the render profile precede any task applying them | M-005, M-020 | none | A bound must be ruled before it is coded | T-008, T-012, T-061, T-063 |
| P-008 | The writer and the process starter exist before any stage writes or renders | M-020, M-021 | P-001, P-007 | Boundary precedes consumers | T-025, T-028, T-051, T-053 |
| P-009 | The item material and the package load exist before any stage reads them | M-024, M-015 | P-002 | Data precedes consumers | T-029 |
| P-010 | The handlers and the produce service in fake and plan-only modes exist before metered mode | M-019, M-012 | P-006, P-008, P-009 | The zero-spend path is proven before the metered path | T-027, T-030, T-031, T-032, T-042, T-046, T-047, T-048, T-049, T-050, T-058 |
| P-011 | The host commands, the store guard and metered mode follow the fake path | M-018, M-025 | P-010 | Metered mode is the irreversible path; its safeguards precede it | T-035, T-041, T-054, T-055, T-056, T-057 |
| P-012 | The produced-item lines follow the records they read | M-023 | P-004, P-010 | Reader after writer | T-038, T-060 |
| P-013 | The boundary assertions cover every new type after the types exist | M-022 | P-008, P-010, P-011 | Verification of the boundary after it is built | T-019, T-040, T-052, T-062, T-064 |
| P-014 | The release record follows every verification | M-025 | P-011, P-013 | Record after evidence | T-065 |

### Test Strategy Focus Areas

- Round trip of the cap: stub prices binding mid-production; failed, retried, timed-out, usage-less, recovered and unrecorded attempts; a concurrent pair; each counted at worst case, the call past the cap never made (M-005, M-013), for omn-dev-2-reviewer.
- Clocks and holds: a stalled stand-in provider at the bound, a stalled stand-in tool at each bound, an open-transaction observation during a stand-in render, a skewed process clock during a run (M-005, M-020).
- Content and hashes: content length and hash equal received bytes; recorded hashes equal stored files; an injected interruption leaves no record; repeated fake runs repeat hashes (M-005, M-021).
- Modes and guard: plan-only writes and calls nothing; fake mode reaches no network with fake variables set; each metered refusal by name; the guard over values; one build in both modes (M-018, M-012).
- Boundary assertions each failing on a seeded probe in a solution build, and the count after the change (M-022).

### Rollout and Rollback

- Rollout: merged behind no flag; phases demonstrate in demonstration stores only; the orchestrator installs nine resources and runs prepare against the company store immediately before the owner-released run, after first-hand price and terms verification.
- Rollback: revert the change and drop the ninth resource's objects; a company store holding a real reservation or operation is kept and never dropped.

## Risks and Mitigations

| ID | Class | Trigger | Impact | Likelihood | Affects | Mitigation | Owner |
|---|---|---|---|---|---|---|---|
| R-001 | contract | A fixture misstates a vendor shape (A-002) | The first metered call fails or books wrong units | medium | D-002 | First-hand verification before the go, Q-003 | omn-orchestrator |
| R-002 | performance | A narration request does not complete inside 60 seconds (A-006) | A charged attempt books at worst case and the run stops | medium | D-008 | Ruling Q-001; configured character maximum; P-007 | omn-tech-lead |
| R-003 | structural | The companion transaction deadlocks or waits behind the admission's holds | A capped admission ends in a datastore timeout | low | D-006 | It takes no scope lock and only the shared horizon hold; tested in P-006 | omn-dev-1-implement |
| R-004 | contract | An uncapped provider admission keeps the delivered path | A loss there can pass the company ceiling by one attempt | low | D-006 | Metered mode requires a cap; Q-005 | omn-tech-lead |
| R-005 | operability | A reservation is never reconciled | The item's remaining cap shrinks by the worst case | low | D-006 | Named in the brief line; settlement left to the owner, Q-009 | omn-product-owner |
| R-006 | contract | A delivered test asserts a transaction count or new-table absence (A-010) | A delivered test fails | low | M-005 | Companion only for capped items; full live suite in P-013 | omn-dev-1-implement |
| R-007 | contract | Every narration booking carries the estimate basis (A-002) | No narration cost reads measured | high | D-002 | Labelled; a fixture with body usage switches to measured | omn-product-owner |
| R-008 | migration | The datastore cannot change the generated cost's expression (A-004) | The cost of new kinds is not stored | low | M-016 | A second generated column for added kinds, read with the first | omn-dev-1-implement |
| R-009 | performance | Stills or audio differ between runs (A-005) | Repeated-run hash criteria fail | medium | D-013 | Bit-exact flags, one thread, fixed font, recorded tool version | omn-dev-1-implement |
| R-010 | contract | A reader assumes the package's version is the produced version | A reader lists version 1 outcomes as the production's | medium | D-010 | Every line names its version; the brief reads the latest production | omn-dev-1-implement |
| R-011 | security | A secret reaches a log, record, exception or output | Credential disclosure | low | D-002 | The exchange alone writes it; a fake-secret search over every output in P-006 | omn-dev-1-implement |
| R-012 | contract | A reader of refusal reasons meets the new item-cap reason | An unlisted reason renders unknown | low | M-001 | The held and readings checks widened in the ninth resource | omn-dev-1-implement |
| R-013 | security | The output root is reached through a link inside the repository | A file lands in the tracked tree | low | D-014 | Links resolved before containment; refused when unresolvable | omn-dev-1-implement |
| R-014 | operability | A beat or cue quotation is not unique (A-011) | The plan refuses naming the beat | low | D-011 | Material review and the loader's check in P-009 | omn-dev-1-implement |
| R-015 | security | The fifth structural absence is not the publishing key rule (A-007) | An absence is unguarded | low | D-020 | Q-008 | omn-tech-lead |
| R-016 | delivery | The scope exceeds one implementation pass | Should-have items slip | medium | P-010 | Messages and image adapters, brief lines and register sit off the zero-spend path | omn-orchestrator |
| R-017 | operability | The render, probe or decode bound is ruled too short | A healthy render ends failed | medium | D-008 | Bounds are settings; Q-002 | omn-tech-lead |
| R-018 | security | A configured executable names something other than the tool | An arbitrary process is started | low | D-013 | Settings validated as existing files; the version line recorded | omn-dev-1-implement |
| R-019 | contract | The channel-without-amount reading applies only under a cap | Uncapped metered work still refuses for such a channel | medium | M-009 | Q-004 | omn-product-owner |
| R-020 | delivery | The version proposal is not ruled | The version assertion and records disagree | low | M-031 | Q-007 | omn-tech-lead |
| R-021 | security | Alternative text is invented for thumbnails | An unreviewed text is screened as recorded | low | D-011 | Recorded absent; Q-006 | omn-product-owner |
| R-022 | operability | A test names a database other than the demonstration database | That schema is dropped | low | P-010 | Every verification records its target and the company store's table count | omn-orchestrator |

## Estimate and Confidence

- Overall: L (confidence: medium). Cross-boundary change across seven production assemblies, a new assembly and a ninth schema resource with additive transitions.
- Breakdown:
  - P-001, P-002, P-003, P-004: M (confidence: medium).
  - P-005, P-006: L (confidence: medium); the companion reservation and charge-unknown booking carry the round trip.
  - P-007: XS (confidence: high); a ruling.
  - P-008, P-009, P-010: L (confidence: low); process, determinism and placement depend on A-005 and A-011.
  - P-011, P-012, P-013, P-014: M (confidence: medium).
- Scope assumptions:
  - Should-have statements S-019, S-020, S-021 and S-022 are included.
  - The vendor shapes are taken from recorded fixtures (A-002).
  - The bounds are ruled as proposed or as settings (A-006).
- Uncertainty drivers:
  - Byte-identical stills and audio across runs (A-005).
  - The generated cost expression change (A-004).
  - Delivered tests meeting the companion transaction (A-010).

## Open Decisions and Escalations

| ID | Question | Blocking | Owner | Affects | Consequence |
|---|---|---|---|---|---|
| Q-001 | Does the 60-second provider-call bound stand for narration, with requests limited by a configured character maximum? | no | omn-tech-lead | D-008 | Yes: as designed; no: a narration-specific bound, with the recovery timeout at twice it |
| Q-002 | Are the render and decode bounds of 60 minutes, the probe and still bounds of 60 seconds, and the 1920 by 1080, 30 frames per second profile ruled? | no | omn-tech-lead | D-008, D-013 | Ruled values become the settings; otherwise the settings stay required with no default |
| Q-003 | Is the implementing role's recorded knowledge the accepted fixture source, with first-hand verification the orchestrator's precondition of the go? | no | omn-orchestrator | D-002 | Yes: as designed; no: fixtures wait for a verified source |
| Q-004 | Does admission against the ceiling and cap alone apply only where the item has a recorded cap? | no | omn-product-owner | M-009 | Yes: as designed; no: every channel without an amount is admitted against the ceiling |
| Q-005 | Should the durable reservation extend to uncapped provider admissions? | no | omn-tech-lead | D-006 | Yes: delivered tests are revisited; no: as designed |
| Q-006 | Does the owner supply thumbnail alternative text, or does it stay recorded absent? | no | omn-product-owner, architect | D-011 | Supplied: added as item material and screened; absent: file names alone are screened |
| Q-007 | Is the version 1.6.0? | no | omn-tech-lead | M-031 | Yes: the assertion follows; no: 1.5.0 stays |
| Q-008 | Which assertion is the fifth structural absence? | no | omn-tech-lead | D-020 | The named assertion is kept explicitly |
| Q-009 | How is a never-reconciled reservation settled? | no | omn-product-owner | D-006 | Until answered it counts at worst case |
| Q-010 | Are the fifteen records at status Proposed accepted at the Design Gate? | no | omn-tech-lead | D-001, D-016 | Accepted: implementation proceeds; otherwise this phase repeats |

## Sign-off

- Architect: unsigned; the producing agent does not decide its own gate
- Tech Lead: omn-tech-lead, accepting owner of the Design Gate under the producer exclusion rule; unsigned
- QA: not required at the Design Gate

## Appendix A: Objections recorded

1. The publish-ready predicate does not refuse a held stage (F-033); the package dossier's statement that three held stages refuse is wrong, so criterion 33 needs the held-stage refusal of D-010.
2. The copyright check handler never records held (F-034); criterion 33's copyright hold is met only by carrying version 1's recorded outcome.
3. The narration is 11,096 characters only after line-ending normalisation; raw it is 11,185 (F-036); D-009 adopts the verifier's rule.
4. Resolution already filters a provider route's estimate against the company remaining (F-023, F-024); the plan's premise that the controller decides on booked spend only is partly wrong; the real gaps are estimates that are not upper bounds, other units ignored, and zero headroom for a channel with no budget row.
5. A channel with no budget row reads zero headroom (F-024), so the owner's ceiling ruling also needs the reading change in D-006, not only the controller change.
6. The suite labels four structural absences, not five (A-007, Q-008).
7. No delivered assertion reads source files or scans process or file use assembly-wide (F-041); D-020's assertions are new, not extensions.
8. The broker supports the GLOBAL name, but the boundary always passes a channel (F-011); the account's recorded scope in D-002 is needed for the Planning Gate's ruling to hold.
9. One account per provider (F-028) means fakes cannot be separate accounts; they reuse the real route rows in demonstration stores (D-004).
10. Stage rows are stamped by the process clock (F-031); D-010 adds a datastore-stamped member and leaves delivered rows as recorded.
11. Every narration booking will carry the estimate basis if the speech response carries no usage (R-007).
