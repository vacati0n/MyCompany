```yaml
plan:
  planId: PLAN-2026-0010
  sourceInputs:
    - type: feature-request
      reference: tasks/MC-11/input.md
  producedBy: planner
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  inputDigest: sha256:903ea015370d41bb3e0329640e0f5671
  contextDigest: sha256:62e7676afa2df10b3af32629f294e88a
```

## Executive Summary

This plan decomposes the company's own narration into fifty-four tasks across twelve execution waves, for the CEO and sole owner, who wants item 001 produced as a verified video narrated in a voice the company owns, with no vendor, no per-use charge and nothing leaving its machine, and wants to replace that voice later with the CEO's own recording by registering it. Success is that one produce command narrates every part from exactly one own source, a registered recording with a recorded release or else the owner-approved in-house model verified by hash, names that source and why for every part before anything runs, records each narration file's provenance and its duration and loudness measured from decoded audio beside the script's expectation without adjusting anything, books USD 0.00 metered spend, and renders the video exactly as before in shape; this is proven in the demonstration store against a tone-fixture recording and an in-test model stand-in, and prepared for the orchestrator's post-phase run of item 001 in the company store. Fifteen architect decision tasks lead, nineteen implementation tasks and nineteen verification tasks follow, and one documentation task closes; the recording-sourced end-to-end production is delivered in wave 5 (T-025) and evidenced in wave 6 (T-035), the model-sourced one in wave 7 (T-026) and wave 8 (T-036), with the three carried should-have items (T-029, T-030, T-031) off both paths. The highest-impact risk is the round-trip and same-clock defect class this programme has missed until review in five waves: a recording's hash taken over other bytes than those decoded and assembled, a model file's hash or identity taken from installation records or configuration while another file is loaded, durations read from a container header as the delivered probe does, and the model run or a long decode held inside a transaction or the 30-second command timeout (R-006, R-007, R-008, R-009, R-010); T-009 enumerates every timeout, hold and clock first and T-052 verifies the round trip. Eight facts from the delivered source and the scope are raised as objections: scope criterion 5 bars the vendor in any mode while two delivered integration tests drive a metered production through a stub vendor that criterion 7 keeps passing (Q-002); the delivered probe reads every duration from the container's header field (Q-003); no price row marks a first-hand verification (Q-004); no method is stated for the orchestrator to evidence zero network requests of the real model process (Q-005); where installation hashes live and which files form the runtime are unstated (Q-006); plan-only must state the model verified while starting no process (Q-007); the script's assumed rate is not quoted in item material (Q-008); and the delivered produce path refuses every producing mode without the vendor's voice setting and splits narration under the vendor's character maximum (R-014). A decision the owner took during this phase (2026-10-10, recorded at the end of the decision record at commit 89c4cb4) is carried as A-012: the approved runtime and voice are already installed outside the repository with the voice files' hashes recorded, item 001 is narrated with the voice's default settings passed explicitly and recorded, and that output was measured first hand not to repeat, so the model's installation leaves the post-phase work and only the company-store run remains. Plan status is complete: every in-scope statement is decomposed, and none of the nine open questions blocks decomposition.

## Business Objectives

1. The owner receives item 001 as a finished, decodable video narrated in the company's own in-house voice, at USD 0.00 metered spend, with no vendor and no narration text leaving the machine. Received by the CEO and sole owner. Measured by the orchestrator's post-phase run record in the company store and, inside the phases, by a model-stand-in production in the demonstration store. Traces to S-003, S-009, S-010, S-013.
2. The owner can replace the model voice with the CEO's own recording by recording one file per beat and registering it with a release, with no change to how the video is made. Received by the CEO as performer and owner. Measured by a recording-sourced production in the demonstration store and the owner guide's commands run as documented. Traces to S-006, S-007, S-012.
3. The owner sees, before anything runs, which source narrates each part and why, and the vendor is never chosen while the owner's decision of 2026-10-10 stands. Received by the owner and the orchestrator. Measured by the printed plan over fixed fixtures and a store holding a priced vendor route. Traces to S-001, S-002.
4. Every narration in the company's records traces to a performer under a recorded release or to the owner-approved model under recorded licences with the hashes of what ran, and its measured duration and loudness stand beside the script's expectation with nothing adjusted or invented. Received by the owner. Measured by a read of the recorded provenance and measurements of each source's production. Traces to S-004, S-005, S-008, S-018.
5. The company store holding the first real records stays untouched by every phase, while the orchestrator receives everything it needs to produce item 001 there on the owner's go. Received by the owner and the orchestrator. Measured by the test configuration, the schema change proven on a demonstration store of the company store's shape, and the release record's run steps. Traces to S-011, S-013.
6. The owner can know why a vendor refused, without any credential being recorded, and never misreads a demonstration figure or a price's verification. Received by the owner. Measured by stub-vendor records, the weekly report over a demonstration store and the plan's total line over price rows. Traces to S-014, S-015, S-016.
7. The company's software stays trustworthy and re-pointable: the ten completed waves are built upon, every control is extended and none weakened. Received by the owner and every later item. Measured by the live suite and the architecture suite in the demonstration store. Traces to S-017.

## Technical Objectives

1. A narration source is one of a closed set of four values, chosen by one deterministic rule over registrations, verified model state, item version and store designation, which never yields the vendor while the owner's recorded decision stands and yields the fake only in a demonstration store; the run re-derives the plan's source and stops before producing a part whose source would differ. Verified by table-driven rule tests, repeated plan-only runs and a plan-versus-provenance round trip. Traces to business objectives 1 and 3, and to A-001.
2. A recording is registered only as one decodable single-audio-stream file per beat, with the SHA-256 of the bytes later decoded, measured and assembled, the performer's identity and the release record or their recorded absence; a changed file or an absent release stops a production before narration. Verified by fixture registration tests and a one-byte change after registration. Traces to business objectives 2 and 4.
3. The in-house model runs only through an architecture-tested process boundary under its own bound on the monotonic clock, with no record, transaction or datastore command open and no network reachable, on model files whose SHA-256, computed over the files given to the process, equals the hash recorded at installation. Verified by stand-in processes that outlast the bound, swap a file, or attempt a request. Traces to business objectives 1 and 4, and to A-006.
4. Every model-produced narration file records the model, version, the four licences, the voice, the generation settings and the hash of every file loaded, and states repeatability only as observed. Verified by reading the provenance of deterministic and non-deterministic stand-in runs. Traces to business objective 4.
5. Duration and loudness of every narration source are measured from decoded audio by named measures with their basis and unit, reported beside the expectation from the script's assumed rate as a signed difference, and never acted on; parts are joined without gain change, stretch, pad or trim. Verified by fixtures whose header disagrees with their audio and tones of known level. Traces to business objective 4, and to A-008.
6. The assembly renders one file with one video and one audio stream, decoded end to end with zero errors, its beat timeline built from the measured part durations of the source used, with the item held short of publish-ready. Verified by recording and stand-in productions in the demonstration store. Traces to business objective 1.
7. An own-source production books no non-zero operation, leaves the item cap's counted total unchanged, and prints local compute and recording time as unmeasured; a store guard admits own-source production in a company or demonstration store and the fake in a demonstration store only. Verified by booked-operation and cap readings and guard unit tests. Traces to business objectives 1 and 5.
8. A tenth schema resource gives the store the own-source mode and the registration, installation and provenance records, applies cleanly over the nine delivered resources and the company store's recorded rows, and changes none of them. Verified on a demonstration store prepared to the company store's recorded shape. Traces to business objective 5, and to A-011.
9. A vendor's non-success response records its status and its screened, truncated code or message in every sink it reaches; demonstration controller-decision labels never read as observed; the plan's total line states the verification its price rows record. Verified against stub vendors, a demonstration store and price-row fixtures. Traces to business objective 6.
10. Every timeout, hold and clock on the narration path is enumerated, every wait ends under a named outcome, and every recorded instant is the datastore's. Verified by stalled stand-ins, an open-transaction observation and a skewed process clock. Traces to business objectives 1 and 4.
11. The boundary assertions cover every type this change adds, including the model boundary, the source set's four members and the absence of any publication or vendor path from own-source production, and none is weakened. Verified by a solution build failing on a seeded violation of each. Traces to business objective 7.

## Scope

### In Scope

- S-001: Every narration of a production comes from exactly one of a closed set of four sources, chosen by a fixed rule, and the plan printed before anything runs states every part's source and why. Designed by T-001, delivered by T-017, T-025 and T-026, verified by T-036.
- S-002: The vendor narration route can never be chosen while the owner's decision of 2026-10-10 stands, though it stays in the software; re-admitting it takes a new recorded owner decision. Designed by T-002, delivered by T-017, verified by T-037.
- S-003: With no usable recording, the narration is generated on the company's machine by the installed, owner-approved model and voice, with no network, no metered charge and nothing held open while it runs. Designed by T-005 and T-009, delivered by T-023 and T-026, verified by T-036 and T-039.
- S-004: Every model-produced narration file carries its provenance, including the hash of every model file actually loaded and whether its output was observed to repeat. Designed by T-008, delivered by T-024, verified by T-040.
- S-005: The model is used only when the installed files are the owner-approved runtime and voice whose hashes were recorded at installation. Designed by T-006, delivered by T-022, verified by T-041.
- S-006: An operator can register a recorded narration as one file per beat, recording its hash, the performer's identity and the release record, after it was probed and decoded end to end. Designed by T-004, delivered by T-021, verified by T-042.
- S-007: A production narrates only from the very bytes registered, and never from a recording without a recorded release. Designed by T-004 and T-009, delivered by T-025, verified by T-043 and T-052.
- S-008: Duration and loudness of every narration source are measured from decoded audio, recorded as observations and reported beside the text's expectation, with no target, tolerance or adjustment. Designed by T-007, delivered by T-020 and T-027, verified by T-044.
- S-009: The video is assembled exactly as before in shape, rendered, decoded end to end and its runtime measured from the file, narrated from the company's own source. Designed by T-007 and T-014, delivered by T-025 and T-026, verified by T-035 and T-036.
- S-010: An own-source production books USD 0.00 metered spend, leaves the item cap's counted total at USD 0.007695 estimate, and records local compute and recording time as unmeasured. Designed by T-010, delivered by T-025, verified by T-035 and T-036.
- S-011: Own-source production can run against the company store and a demonstration store, while every phase, test and demonstration runs in the demonstration store only and reaches no network. Designed by T-003, delivered by T-018 and T-019, verified by T-046.
- S-012: The owner has documentation for recording, registering, installing and verifying the model, and producing an item, with exact commands. Designed by T-015, delivered by T-033 and T-034, verified by T-047.
- S-013: After the phases, the orchestrator can produce item 001 in the company store with the approved in-house voice at USD 0.00 on the owner's go, using only documented commands. Designed by T-003, T-006 and T-015, delivered by T-018, T-022, T-028 and T-033, verified by T-047 and T-051, recorded by T-054.
- S-014: A vendor's non-success response records its status and its error code or message, truncated and screened so that no credential or token is recorded. Designed by T-011, delivered by T-029, verified by T-048.
- S-015: In a demonstration store, no controller-decision label of the weekly report reads as an observation of the company's spend. Designed by T-012, delivered by T-030, verified by T-049.
- S-016: The plan's total line states the verification state its price rows record. Designed by T-013, delivered by T-031, verified by T-050.
- S-017: The ten completed waves are built upon and none re-created; every test passing live at the Wave 9 head still passes; the boundary assertions are extended and never weakened. Designed by T-014, baselined by T-016, delivered by T-032, verified by T-045 and T-053.
- S-018: No threshold, target, tolerance, loudness level, format requirement, rights position or performer identity is invented, and no figure from a stand-in, a tone fixture or a fake is presented as observed. Verified by T-053, recorded by T-054.

### Out of Scope

- Any call to the speech vendor or any other metered service, in any phase, test, demonstration or the company-store run. Excluded by the owner's decision of 2026-10-10 that the company's narration is its own.
- The run of item 001 against the company store inside any phase, and any phase connection to the company store. Excluded because every run there is the orchestrator's, on the owner's go, after the phases.
- Downloading any model, voice, package or tool in a phase. Excluded because a download is an owner-approved act the orchestrator performs; tests use stand-ins created inside the test.
- Publication, upload, channel creation, subscription, and any change to the held-stage refusal. Excluded by the no-publish constraint.
- Real motion graphics, stock footage and music; the 22 stills stay text cards and the clip positions labelled placeholders. Excluded by the request.
- Cloning, training or fine-tuning a voice on the CEO's recording or any real person's voice. Excluded because no release names voice-model use.
- Any loudness, duration-tolerance, sample-rate or format target, normalisation, time stretch, pad or trim. Excluded by the owner's answers of 2026-10-10.
- Mixing sources inside one production. Excluded because the owner's order is model first, then the whole recording replacing it.
- A whole-file recording with beat instants, and any automatic alignment of a recording to beats. Excluded by the owner's answer of one file per beat.
- The previous wave's vendor-path issues on request shape, call size within the provider-call bound, pre-send cancellation and the metered host run. Excluded because the vendor route is not used.
- Asserting that the rendered video's bytes repeat across runs. Excluded as a carried residual risk; only narration repetition is recorded, and only as observed.
- Redistributing the model, its runtime or its phonemiser. Excluded because the copyleft obligations bind only on distribution.
- Any executive or narrative model call. Excluded by the owner's carried decision.

### Deferred

- The orchestrator's run of item 001 in the company store with the in-house voice. Brought into scope when this change closes, the orchestrator has taken a fresh backup, applied the tenth resource, entered the installation hashes where the design places them, run plan-only, and the owner gives the explicit go; the model itself is already installed.
- Producing item 001 again from the CEO's recording. Brought into scope when the CEO has recorded thirteen beat files and the signed release is recorded, and the owner gives the go.
- A loudness or duration target. Brought into scope when the owner sets one from measured results.
- Re-admitting the vendor narration route. Brought into scope by a new recorded owner decision.

## Assumptions

| ID | Assumption | Basis | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | The Scope Gate approved the scope with rulings that reached this phase only through the dispatch briefing: one source narrates a whole production (recording with recorded release, else verified in-house model, else refusal; the fake only in a demonstration store; the vendor never, by recorded decision rather than a settings value); duration and loudness are measured from decoded audio, never from a header, and never acted on, duration reported beside 150 words a minute as a signed difference with no tolerance; every recorded hash is over the bytes actually decoded or loaded; repeatability is recorded only as observed; the model run holds no record or transaction and has its own monotonic bound; a new production mode is needed for own-source narration in the company store, applied there by the orchestrator after a backup before the post-phase run; the quality-rating question goes to omn-tech-lead at the Design Gate | S-001 | The rule, measurement, provenance and mode tasks change shape | omn-business-analyst |
| A-002 | The owner's decisions of 2026-10-10 bind every phase: the company's narration is its own and the vendor is not attempted again; the cap keeps its counted USD 0.007695 estimate; the CEO records personally, one file per beat; item 001 is produced first with the model; no loudness and no format target; the approved model is the runtime 1.8.0 with its phonemiser and the voice en_GB-cori-high, GPL accepted for in-house use; the release form is a dated CEO-signed statement not permitting model training, held outside the repository | S-002 | An owner-gated quantity or a rights position enters the plan, or a source rule changes | omn-product-owner |
| A-003 | Verification tasks are owned by omn-dev-2-reviewer because omn-qa owns no phase in the routed workflow | plan-wide | Nineteen verification tasks are never dispatched | omn-orchestrator |
| A-004 | The owner guide and the settings sample are repository files, written by omn-dev-1-implement; omn-documentation, which writes no repository file, produces the release record as its phase artifact | S-012 | The guide or the sample is never written, or a non-implementing role writes a repository file | omn-orchestrator |
| A-005 | Every new configured quantity (the model-run bound, any registration decode or measurement bound, the error-body truncation length) and the quality-rating question are proposed by architect tasks and ruled at the Design Gate, where omn-tech-lead is a listed owner, because omn-tech-lead owns no phase | S-003 | T-009 and T-011 are not ruled, and T-020, T-021, T-023 and T-029 have no value to apply | omn-orchestrator |
| A-006 | The model path is built and evidenced only against stand-ins created inside the test, including stand-in processes started through the boundary; the real model produces company records first in the orchestrator's post-phase run, its trial runs of 2026-10-10 being no company record | S-003 | A phase downloads or runs the real model, or the boundary is never exercised by a real process | omn-orchestrator |
| A-007 | Every test and demonstration names the separate demonstration database only, and the live suite is never pointed at any other database | S-011 | The named store's schema is dropped or the company store is written | omn-orchestrator |
| A-008 | The per-beat expectation counts each beat's words as the script's whole count was taken (1,929 whitespace-separated words) at the script's recorded 150 words a minute, quoted into item material from the script rather than written into production source | S-008 | Per-beat expectations differ from the script's whole figure, or a rate enters production source | architect |
| A-009 | The fake source is the delivered demonstration tone through the delivered demonstration composition, unchanged in behaviour | S-001 | The fake source needs its own build task | architect |
| A-010 | The hashes the run verifies against are those the owner's decision record holds for the installed voice files, entered into the installation record through the path this change delivers before the plan-only run; runtime files enter that record only as T-006 decides | S-005 | The run has no installation record to verify against, or verifies against a value nobody measured | omn-orchestrator |
| A-011 | At the post-phase run the company store is as recorded on 2026-10-10: nine resources, the prepared rows, item version 2 failed at Audio with one operation booked at USD 0.007695 estimate; the backup taken before that attempt is not the backup required before the tenth resource | S-013 | The tenth resource meets an unproven state, or the cap's counted total differs | omn-orchestrator |
| A-012 | The owner decided on 2026-10-10, during this phase, to install the model now: the orchestrator installed runtime 1.8.0 with its resolved dependencies in a virtual environment on Python 3.14.0 and the voice files at a recorded revision, outside the repository, recording the SHA-256 of the voice model, its configuration and its model card in the decision record; the orchestrator's trial (not a company record) measured about 175 s for the whole narration on the CPU, 16-bit mono output at 22,050 Hz, 10 min 31 s of audio and different bytes between two default-setting runs; item 001 is narrated with the voice's default settings, passed explicitly and recorded with every artifact, the runtime's own output normalisation recorded as a generation setting, and the pipeline applies no further level change | S-004 | The design rests on unmeasured run time, or provenance omits a setting, or repeatability is claimed | omn-orchestrator |

## Risks

| ID | Class | Trigger | Impact | Likelihood | Affects | Mitigation | Owner |
|---|---|---|---|---|---|---|---|
| R-001 | requirement | A Scope Gate ruling restated in A-001 is overturned | Rule, measurement, provenance or mode tasks change | low | plan-wide | An overturned ruling returns the plan to this phase rather than being absorbed | omn-business-analyst |
| R-002 | requirement | Scope criterion 5 bars the vendor in any mode, while two delivered integration tests drive a metered production through a stub vendor and criterion 7 keeps them passing (Q-002) | Either criterion is unmet, or a delivered assertion is silently changed | high | T-002, T-017, T-037, T-053 | T-002 states where the bar sits and how each delivered test is dispositioned, recorded for the Design Gate | architect |
| R-003 | requirement | The rendered video's runtime keeps the container-field basis the delivered probe reads while narration moves to decoded audio (Q-003) | Two runtime bases in one record, or criterion 29 read as unmet | medium | T-007, T-020, T-035, T-036 | T-007 states the basis of every duration and runtime and labels it | architect |
| R-004 | requirement | Item material quotes no delivery rate, and the rounding of 1,929 words at 150 a minute (771.6 s against the script's printed 12 min 51 s) is unstated (Q-008) | Expectations differ from the script, or a rate is written into production source | medium | T-007, T-027, T-044 | T-007 states the rate's quoted source, the word rule and the rounding | architect |
| R-005 | requirement | No price row field marks a first-hand verification (Q-004) | The total line's statement has no recorded basis | medium | T-013, T-031, T-050 | T-013 names the field and its meaning before any wording changes | architect |
| R-006 | technical | Registration hashes one file and production decodes, measures or assembles another, the file is replaced after registration, or the tool reads a path after it was hashed | A recorded hash names bytes that were not narrated | high | T-004, T-021, T-025, T-043, T-052 | T-004 binds the hash to the bytes every later reader uses; T-052 changes a byte between each pair of readers | architect |
| R-007 | technical | A model file's hash is taken from the installation record or computed over a different file than the process is given, or a file is swapped between check and load | Provenance names a model that did not run | high | T-006, T-022, T-023, T-041, T-052 | T-006 states which bytes are hashed and when relative to the load; T-052 swaps a file | architect |
| R-008 | technical | The model's name, version, licences or voice are recorded from configuration rather than from what is installed and run | Provenance contradicts the files loaded | medium | T-006, T-008, T-024, T-040 | T-006 and T-008 state the source of every provenance field | architect |
| R-009 | technical | Duration or loudness is read from a container header or configuration, as the delivered probe reads every duration from the container's duration field | Durations, the timeline and the report rest on unmeasured figures | high | T-007, T-020, T-027, T-044 | T-007 names the decoded-audio measures; T-044 uses a fixture whose header disagrees | architect |
| R-010 | technical | The model run, a long decode or measurement, or hashing a voice of about 114 MB runs inside a transaction or under the 30-second command timeout, or has no bound | Records wait or a run ends unnamed | high | T-009, T-021, T-022, T-023, T-052 | T-009 enumerates every timeout, hold and clock; T-052 observes open transactions during a stalled stand-in | architect |
| R-011 | technical | A recording is registered or a model file changes between the printed plan and the run | The plan names one source and another narrates | medium | T-001, T-025, T-026, T-036 | T-001 states the run's re-derivation and stop; T-036 changes the state between plan and run | architect |
| R-012 | technical | The approved model with the owner's chosen default settings was measured first hand to produce different bytes on each run, while a record claims repetition | A false reproducibility claim | high | T-008, T-024, T-040 | T-008 records repeatability only from a recorded comparison | architect |
| R-013 | technical | Parts of differing sample rate or channel count are resampled, re-levelled or padded when joined, as the delivered join re-encodes | The assembled duration or loudness departs from its parts | medium | T-007, T-020, T-027, T-044 | T-007 states the join and the one-frame comparison; T-044 joins mixed-format fixtures | architect |
| R-014 | technical | The delivered produce path refuses every producing mode without the vendor's voice setting, and splits narration under the vendor's per-request character maximum | An own-source run is refused, or model parts do not align with beats | medium | T-001, T-005, T-025, T-026 | T-001 and T-005 state which settings apply to each source | architect |
| R-015 | technical | A model runner becomes a second process-starting type unasserted, or the model stand-in is in-process so the boundary is never exercised | The one-starter assertion weakens or the bound is unproven | medium | T-005, T-014, T-023, T-032, T-039 | T-014 states the starter set; T-039 uses a stand-in process | architect |
| R-016 | technical | Applying the ordered resources to a store holding real rows alters a delivered row, or the tenth resource fails over the company store's recorded state | The company store is damaged before the run | medium | T-003, T-018, T-051 | T-051 applies twice to a demonstration store of the company store's shape and compares every delivered row | architect |
| R-017 | security | A vendor error body echoing a secret, a bearer value or a token reaches the operation record, the availability record, the audit entry, the stage summary or console output | Credential disclosure in the store | medium | T-011, T-029, T-048 | T-011 lists every sink and the screening rules; T-048 scans every recorded field | architect |
| R-018 | security | The model runtime fetches a voice by name or reaches the network in any other way | Narration text or a request leaves the machine | medium | T-005, T-015, T-023, T-039 | T-005 states how the run reaches no network; T-015 states how the orchestrator evidences it (Q-005) | architect |
| R-019 | security | A model, voice or runtime file sits in the tracked tree, or the signed release document is stored in the repository or the record store | Copyleft or personal material is distributed or retained | low | T-004, T-006, T-021, T-022, T-041, T-042 | T-004 and T-006 keep both outside; T-041 and T-042 inspect the tree and schema | architect |
| R-020 | security | A fake source is admitted against a company store, or the vendor is re-admitted by a settings edit | A demonstration figure reaches the company's records or the vendor is called | low | T-002, T-017, T-019, T-037, T-046 | T-002 and T-003 make both structural; T-037 and T-046 test each | architect |
| R-021 | security | A performer identity or release reference is filled in where none was recorded | A rights position is invented | low | T-004, T-021, T-043 | T-004 records absence explicitly; T-043 registers with every optional field missing | architect |
| R-022 | operational | A test or demonstration names a store other than the separate demonstration database, contrary to A-007 | The named store is dropped or the company store is written | low | T-016, T-035, T-036, T-046, T-047, T-051, T-053 | Each verification records its target store | omn-orchestrator |
| R-023 | operational | A verification task is assigned to a role that owns no phase, contrary to A-003 | Verification never runs | medium | plan-wide | A-003 assigns verification to the quality-review owner | omn-orchestrator |
| R-024 | operational | A new configured quantity is not ruled because omn-tech-lead owns no phase, contrary to A-005 | Bounds or the truncation length have no ruling | low | T-009, T-011 | The orchestrator obtains each ruling at the Design Gate | omn-orchestrator |
| R-025 | dependency | A file of the installation of 2026-10-10 changes before the run, or the runtime's resolved dependencies differ from the decision record's list | The model source is refused at the run, or provenance names other code than ran | low | T-022, T-054 | T-022 refuses on any differing file; the release record lists the expected hashes and the dependency list | omn-orchestrator |
| R-026 | dependency | The company store's state at the run differs from A-011 | The tenth resource or the cap reading meets an unproven state | low | T-051, T-054 | The release record lists the readings the orchestrator takes before applying anything | omn-orchestrator |
| R-027 | dependency | The installed media tool is absent, a different version, or not located by its setting | Measurement and registration tests are not run | low | T-020, T-021, T-035 | Tests needing the tool are reported not-run with the reason, never as passed | omn-orchestrator |
| R-028 | dependency | The demonstration database is unavailable for the live suite | Live criteria cannot be evidenced | low | T-016, T-035, T-036, T-053 | Verification records the not-run count and reason, never a pass | omn-orchestrator |
| R-029 | dependency | The CEO's signed release is not recorded when the recording exists | The recording cannot replace the model voice | low | T-054 | The release record states the release as the precondition of the recording-sourced run | omn-orchestrator |
| R-030 | dependency | The tenth resource is applied to the company store without a fresh backup, or before the plan-only run's readings | A failed application cannot be reversed | low | T-051, T-054 | The release record orders backup, readings, application, installation record, plan-only and go | omn-orchestrator |
| R-031 | delivery | Eighteen statements exceed one implementation pass | Should-have items slip and the change closes partial | medium | plan-wide | T-029, T-030 and T-031 sit off both end-to-end paths, so they can yield first | omn-orchestrator |
| R-032 | delivery | A new type is checked only by a single-project build, or the assertion count is not pinned | A boundary violation reaches a commit | medium | T-032, T-053 | T-053 takes its evidence from a solution build failing on a seeded violation | omn-dev-2-reviewer |
| R-033 | delivery | The owner guide is assigned to a role that writes no repository file, contrary to A-004 | The owner has no guide when the change closes | low | T-034, T-047 | A-004 assigns it to the implementing role | omn-orchestrator |
| R-034 | requirement | Where installation hashes are recorded, which files form the runtime, and how plan-only states them verified without starting a process are unstated (Q-006, Q-007) | The install-and-verify path or the company-store plan-only run cannot be documented | medium | T-006, T-015, T-022, T-028 | T-006 decides all three before the commands are designed | architect |

## Task Breakdown

### T-001 Decide the closed narration-source set and its selection rule

- Owner: architect
- Complexity: M (confidence: medium)
- Depends on: none
- Traces to: S-001, S-003, S-007, A-001, A-009
- Status: assumption-dependent
- Description: A recorded design states the four source values, the one rule choosing a production's source from registrations, verified model state, item version and store designation, each refusal and its named reason, the per-part plan line, how the run re-derives the planned source and stops before a part whose source would differ, and which delivered settings (the vendor's voice and per-request character maximum) apply to which source.
- Acceptance Criteria:
  - The design names exactly four source values and states that no other value can be recorded or printed.
  - The design states the rule's order: complete recording with recorded release, else verified model, else refusal, the fake only in a demonstration store, the vendor never.
  - The design states the inputs the rule reads and that two plans over equal inputs print equal lines.
  - The design states how a change between plan and run is detected and that the run stops before that part.
  - The design states how parts are formed for each source and that the vendor's voice setting is not required for an own source.
- Gate: Design Gate

### T-002 Decide how the owner's recorded decision bars the vendor narration route

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: T-001
- Traces to: S-002, A-001, A-002
- Status: assumption-dependent
- Description: A recorded design states where the bar lives so that no settings value alone re-admits the vendor, how the plan and the refusal name the owner's decision of 2026-10-10, what a future re-admission requires, and how each delivered test that drives a metered production through a stub vendor is dispositioned against scope criteria 5 and 7 (Q-002).
- Acceptance Criteria:
  - The design names the recorded decision the bar reads and states that no settings file value is read by it.
  - The design lists every delivered test that reaches the vendor route from production and states each one's disposition.
  - The design states that the vendor route's code and its contract tests stay in the build.
  - The design states what a re-admission needs and that this change delivers no re-admission path.
- Gate: Design Gate

### T-003 Decide the own-source production mode and the store change it needs

- Owner: architect
- Complexity: M (confidence: medium)
- Depends on: T-001
- Traces to: S-011, S-013, A-001, A-011
- Status: assumption-dependent
- Description: A recorded design states the form of the production mode under which own-source narration is recorded, the store guard's admissions for every mode, source and designation, the tenth schema resource's additions to the production version's closed mode set, and how that resource reaches the company store through the orchestrator after a backup without changing a delivered row.
- Acceptance Criteria:
  - The design states every mode, source and designation combination and whether each is admitted.
  - The design states that own-source production is admitted in a company and a demonstration store, and the fake in a demonstration store only.
  - The design states each schema addition and that it is additive, write-once and leaves every delivered row unchanged.
  - The design states the order backup, readings, application for the company store, performed by the orchestrator only.
- Gate: Design Gate

### T-004 Decide how a recording is registered and bound to its bytes

- Owner: architect
- Complexity: L (confidence: medium)
- Depends on: none
- Traces to: S-006, S-007, A-002
- Status: assumption-dependent
- Description: A recorded design states how one file per beat is named and matched to the item material's beat numbers, every refusal (missing, extra, duplicate, whole-narration, undecodable, wrong streams) and its named finding, the measured properties recorded, the release record's fields and their recorded absence, where the registered bytes live, and how the SHA-256 recorded binds to the bytes every later reader decodes, measures and assembles.
- Acceptance Criteria:
  - The design states the naming rule and the refusal for each malformed set, naming each beat affected.
  - The design states that acceptance requires an end-to-end decode with zero errors, exactly one audio stream and no video stream.
  - The design lists every reader of a registered recording's bytes and states the hash check each performs.
  - The design states that the release record holds only the document reference, date, performer's name, document hash and the no-training term, and that the document itself is stored nowhere in the repository or the store.
  - The design states that an absent field is recorded absent and printed absent, never filled.
- Gate: Design Gate

### T-005 Decide the in-house model's process boundary

- Owner: architect
- Complexity: L (confidence: low)
- Depends on: none
- Traces to: S-003, A-005, A-006
- Status: assumption-dependent
- Description: A recorded design states whether the model runs through the one delivered process starter or a justified sibling, what executable it starts and how it is located by configuration, how text reaches it without an argument carrying the narration, how the voice file is given as a file rather than a name the runtime could fetch, how each generation setting is passed explicitly rather than left to the runtime's defaults, where its output is written and adopted by the one writer, how parts are formed, and how a stand-in process exercises the boundary in tests.
- Acceptance Criteria:
  - The design names every type permitted to start a process after this change and the executables each starts.
  - The design states that no record, transaction or datastore command is open while the model runs.
  - The design states how the run reaches no network and how a test proves zero requests.
  - The design states how the model's output file is adopted by the one writer and hashed over the stored bytes.
  - The design states the stand-in process tests use and that no test runs the real model.
- Gate: Design Gate

### T-006 Decide how the model installation is recorded and verified

- Owner: architect
- Complexity: L (confidence: low)
- Depends on: none
- Traces to: S-004, S-005, S-013, A-002, A-010
- Status: assumption-dependent
- Description: A recorded design states which installed files form the approved runtime and voice for hashing, where the hashes recorded at installation live (Q-006), how every file is re-hashed over the bytes the process is given before any narration, how the runtime's version, licences and voice identity are established from what is installed rather than from configuration, how a location inside the repository is refused, and how plan-only states the model verified while starting no process and writing no file (Q-007).
- Acceptance Criteria:
  - The design lists the files hashed and states why each is in the set.
  - The design states where the installation hashes are recorded and who writes them, and that no phase writes them to the company store.
  - The design states the moment each file is hashed relative to the process loading it and how a swap between them is refused or recorded.
  - The design states the refusal naming the file and both hashes for a missing or differing file.
  - The design states how plan-only verifies the hashes without starting a process.
- Gate: Design Gate

### T-007 Decide how narration is measured and compared with its expectation

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: none
- Traces to: S-008, S-009, A-001, A-008
- Status: assumption-dependent
- Description: A recorded design states the decoded-audio duration measure and its recorded basis, the one named standard loudness measure and its unit, how parts are joined with no gain change, stretch, pad or trim, the one-audio-frame comparison of the joined narration with its parts, the per-beat and whole expectations from the script's quoted rate with the word rule and rounding (Q-008), the signed difference with no tolerance, and the basis of the rendered video's runtime (Q-003).
- Acceptance Criteria:
  - The design names the duration measure and states that no container or header duration field is used for narration.
  - The design names the loudness measure and its unit and states that no value is ever acted on.
  - The design states where the rate is quoted from and the word rule and rounding that yield 12 min 51 s for the whole.
  - The design states the join and the frame of the one-frame comparison.
  - The design states the rendered video runtime's basis and its label.
- Gate: Design Gate

### T-008 Decide the provenance recorded with each model-produced narration file

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: T-005, T-006
- Traces to: S-004, A-001, A-005, A-012
- Status: assumption-dependent
- Description: A recorded design states every provenance field (model name, version, code licence, weights licence, voice licence, voice dataset licence, voice name, every generation setting passed including the runtime's output normalisation, each loaded file's hash), the source each field is read from, the repeatability statement and the recorded comparison that alone permits it, which parts are generated twice for that comparison, and the disposition of the carried quality-rating question (Q-001).
- Acceptance Criteria:
  - The design lists every field, states it is non-empty, and names its source in the installed files or the run.
  - The design states that a hash field is the one computed over the file given to the process in that run.
  - The design states the comparison that permits "repeats" and that otherwise "not observed to repeat" is recorded.
  - The design records the proposed answer to Q-001 for omn-tech-lead's ruling.
- Gate: Design Gate

### T-009 Decide every timeout, hold and clock on the narration path

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: T-004, T-005, T-006, T-007
- Traces to: S-003, S-007, S-008, A-001, A-005, A-012
- Status: assumption-dependent
- Description: A recorded design enumerates every timeout, hold and clock on registration, installation verification, the model run, measurement, joining and assembly, including the 30-second command timeout, the delivered probe, decode, still and render bounds, the model run's own bound on the monotonic clock with process termination past it, the hashing of large files, every transaction and its span, and the datastore instant every record carries, and proposes each new bound's value for ruling, resting the model-run bound on the orchestrator's first-hand trial of about 175 s for the whole narration on this machine's CPU (A-012).
- Acceptance Criteria:
  - The design lists every timeout and hold with its value or its proposed value for ruling.
  - The design states that no transaction or record is held while a process runs, a file is hashed or a decode runs.
  - The design states the outcome recorded when each bound passes, including the stage failed when the model run's bound passes.
  - The design names the one clock every recorded instant and every bound uses.
- Gate: Design Gate

### T-010 Decide how own-source narration is costed and printed

- Owner: architect
- Complexity: S (confidence: medium)
- Depends on: T-001
- Traces to: S-010, A-001
- Status: assumption-dependent
- Description: A recorded design states whether own-source narration books any operation and at what cost, that the item cap's counted total is untouched, how the plan's total line states that no metered spend is planned, and how local compute and recording time appear as unmeasured, never as zero, wherever cost is printed or recorded.
- Acceptance Criteria:
  - The design states that no operation with a non-zero cost is booked for an own source.
  - The design states that the cap reservation and booking paths are not reached by an own source.
  - The design lists every place cost is printed or recorded for such a production and the unmeasured wording in each.
- Gate: Design Gate

### T-011 Decide how a vendor's non-success body is screened and recorded

- Owner: architect
- Complexity: S (confidence: medium)
- Depends on: none
- Traces to: S-014, A-005
- Status: assumption-dependent
- Description: A recorded design states which members of a non-success body are read for a code or message, the truncation length proposed for ruling, the screening rules for the configured secret value, a bearer value and token-shaped strings, the wording for an empty or unreadable body, and every sink the screened text reaches.
- Acceptance Criteria:
  - The design lists every sink by name: the operation record's failure reason, the availability record's reason, the audit entry's reason, the production stage summary and command output.
  - The design states each screening rule and that screening happens before any sink receives the text.
  - The design states the proposed truncation length for ruling and the empty-body wording.
- Gate: Design Gate

### T-012 Decide the wording of controller-decision labels in a demonstration store

- Owner: architect
- Complexity: XS (confidence: medium)
- Depends on: none
- Traces to: S-015
- Status: ready
- Description: A recorded design states the label wording of each controller-decision line of the weekly report in a store designated demonstration, so that no label embeds a description reading as observed, and confirms company-designated labels read as before.
- Acceptance Criteria:
  - The design states the wording for every controller-decision line in a demonstration store.
  - The design states that labels in a company store are unchanged.
  - The design names the delivered description that embeds the observed wording today.
- Gate: Design Gate

### T-013 Decide what the plan's total line states about price verification

- Owner: architect
- Complexity: XS (confidence: low)
- Depends on: none
- Traces to: S-016
- Status: assumption-dependent
- Description: A recorded design names the recorded price-row field that states a first-hand verification and its meaning (Q-004), and the total line's wording when every price row it uses records one and when any does not.
- Acceptance Criteria:
  - The design names the field read and states which recorded value counts as first-hand.
  - The design states both wordings, the second naming the recorded verification date and source.
  - The design states that the line is unchanged when no price row is used.
- Gate: Design Gate

### T-014 Decide the boundary assertions this change adds

- Owner: architect
- Complexity: S (confidence: medium)
- Depends on: T-005
- Traces to: S-009, S-017, A-001
- Status: assumption-dependent
- Description: A recorded design lists every architecture assertion added or extended: the source set's four members, the permitted process-starting types, the one writer, no network client reachable from the model path, no vendor reachable from own-source production, model files outside the tracked tree, and the delivered publication absences, each with the seeded violation that must fail it.
- Acceptance Criteria:
  - The design lists every new type and the delivered scans it must pass.
  - The design states each new assertion and its seeded probe.
  - The design states that no delivered assertion is weakened or removed.
- Gate: Design Gate

### T-015 Decide the commands and settings the owner and the orchestrator use

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: T-003, T-004, T-006
- Traces to: S-012, S-013, A-004, A-010
- Status: assumption-dependent
- Description: A recorded design states the registration, model-installation record, model verification and produce commands with their options and documented exit codes, the settings members each reads, the settings sample's own-source content holding no secret and no vendor credential, the ordered steps of the orchestrator's company-store run, and how that run evidences zero network requests (Q-005).
- Acceptance Criteria:
  - The design states every command, its options and its exit codes.
  - The design states every new settings member and that none has a default in code.
  - The design states the orchestrator's ordered steps from backup to the post-run readings.
  - The design states the evidence of zero network requests the orchestrator records.
- Gate: Design Gate

### T-016 Record the live baseline at the Wave 9 head

- Owner: omn-dev-1-implement
- Complexity: XS (confidence: high)
- Depends on: none
- Traces to: S-017, A-007
- Status: ready
- Description: The executed counts of the full live suite and the architecture suite at the feature branch's starting head are recorded in the demonstration store before any change, with not-run tests and their reasons.
- Acceptance Criteria:
  - The record states passed, failed and not-run counts of the full suite, executed against the demonstration database only.
  - The record states the architecture suite's executed count.
  - The record names the head commit and the target database.
- Gate: Review Gate, Verification Gate

### T-017 Deliver the closed source set and the selection rule

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-001, T-002
- Traces to: S-001, S-002
- Status: ready
- Description: The four source values and the selection rule exist as the design fixes, the rule never yields the vendor while the recorded decision stands and yields the fake only for a demonstration store, and every refusal names its reason.
- Acceptance Criteria:
  - The rule returns the documented source for every combination in the design's table.
  - No path through the rule returns the vendor while the decision stands.
  - Every refusal names the owner's decision, the missing release or the model's verification failure as its reason.
- Gate: Review Gate, Verification Gate

### T-018 Deliver the tenth schema resource

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-003, T-004, T-006, T-008, T-010
- Traces to: S-004, S-006, S-011, S-013
- Status: ready
- Description: An ordered tenth schema resource adds the own-source mode and the registration, release, installation and provenance records the designs fix, write-once and stamped by the datastore, applied after the nine delivered resources and reapplying cleanly.
- Acceptance Criteria:
  - A fresh demonstration store installs the ten resources in order and records nothing.
  - Reapplying the ten resources changes no row.
  - No delivered table, constraint or row changes other than the additions the design states.
  - The resource stores no release document and no model file.
- Gate: Review Gate, Verification Gate

### T-019 Deliver the store guard's admission of own-source production

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-003, T-017
- Traces to: S-011
- Status: ready
- Description: The store guard admits own-source production against a company-designated and a demonstration store and refuses the fake against a company store, as the design's table states.
- Acceptance Criteria:
  - Every mode, source and designation combination returns the design's verdict.
  - A refused combination names its reason before any call or file.
  - The delivered guard verdicts for the delivered modes are unchanged.
- Gate: Review Gate, Verification Gate

### T-020 Deliver measurement of duration and loudness from decoded audio

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-007, T-009
- Traces to: S-008
- Status: ready
- Description: Every narration file's duration and loudness are measured from its decoded audio by the named measures, through the one process boundary under its ruled bound, and returned with their basis and unit.
- Acceptance Criteria:
  - A fixture whose header states a different duration yields the decoded duration.
  - Tone fixtures of known level yield the expected loudness value, measure name and unit.
  - No measurement holds a transaction, and a measurement past its bound ends under its named outcome.
- Gate: Review Gate, Verification Gate

### T-021 Deliver recording registration

- Owner: omn-dev-1-implement
- Complexity: L (confidence: medium)
- Depends on: T-004, T-018, T-020
- Traces to: S-006, S-007
- Status: ready
- Description: An operator can register one file per beat against an item version; each file is decoded end to end and measured before acceptance; the registration records the SHA-256 of the bytes later used, the measured properties, the performer's identity and the release record or their recorded absence.
- Acceptance Criteria:
  - A complete thirteen-file set for item 001 registers and records each file's hash, container, codec, sample rate, channels, duration and loudness.
  - Missing, extra, duplicate and whole-narration sets are refused naming each beat.
  - Truncated files and files carrying a video stream are refused naming the finding.
  - Absent performer and release fields are recorded and printed absent.
- Gate: Review Gate, Verification Gate

### T-022 Deliver the model installation record and its verification

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-006, T-018
- Traces to: S-005, S-013
- Status: assumption-dependent
- Description: The installation hashes of the approved runtime and voice can be recorded as the design fixes, and before any narration every installed file is re-hashed and compared, refusing the model source on any missing or differing file and on a location inside the repository.
- Acceptance Criteria:
  - A matching stand-in installation verifies and names every file and hash.
  - A missing file and a one-byte-changed file each refuse naming the file and both hashes.
  - A configured location inside the repository root is refused.
  - Verification reads files only and starts no process.
- Gate: Review Gate, Verification Gate

### T-023 Deliver the in-house model run through the process boundary

- Owner: omn-dev-1-implement
- Complexity: L (confidence: low)
- Depends on: T-005, T-009, T-022
- Traces to: S-003
- Status: assumption-dependent
- Description: The model runs part by part through the boundary the design fixes, on the verified files, under its own monotonic bound, with no record or transaction open and no network reachable, its output adopted by the one writer.
- Acceptance Criteria:
  - A stand-in process produces one file per planned part, each recorded over its stored bytes.
  - A stand-in outlasting a short bound is terminated and the stage recorded failed.
  - No transaction is open while the stand-in runs.
  - The stand-in run makes zero network requests.
- Gate: Review Gate, Verification Gate

### T-024 Deliver model provenance and the repeatability record

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-008, T-023
- Traces to: S-004
- Status: assumption-dependent
- Description: Every model-produced narration file records each provenance field the design lists, read from its stated source, and states repeatability only from a recorded comparison.
- Acceptance Criteria:
  - Every field is non-empty for every narration file of a stand-in run.
  - Each recorded file hash equals the hash of the file given to the stand-in process in that run.
  - A deterministic stand-in records "repeats" and a non-deterministic one records "not observed to repeat".
- Gate: Review Gate, Verification Gate

### T-025 Deliver recording-sourced narration in the produce path

- Owner: omn-dev-1-implement
- Complexity: L (confidence: medium)
- Depends on: T-010, T-017, T-019, T-020, T-021
- Traces to: S-001, S-007, S-009, S-010
- Status: ready
- Description: The produce command prints one source line per part before anything runs, re-derives the source at run time, narrates from the registered bytes after re-checking their hash, measures parts from decoded audio, assembles and renders as before, books no non-zero operation and requires no vendor setting.
- Acceptance Criteria:
  - A demonstration-store production from a registered tone-fixture recording renders one decodable file with one video and one audio stream.
  - The plan prints a source and reason for every part before any process starts or file is written.
  - A byte changed after registration stops the production before narration naming both hashes.
  - Booked metered spend is USD 0.00 and the cap's counted total is unchanged.
- Gate: Review Gate, Verification Gate

### T-026 Deliver model-sourced narration in the produce path

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-024, T-025
- Traces to: S-001, S-003, S-009, S-010
- Status: assumption-dependent
- Description: With no usable recording, the produce command narrates every part through the verified model stand-in, records provenance, measures from decoded audio, assembles and renders as before, and books no non-zero operation.
- Acceptance Criteria:
  - A demonstration-store production with an in-test model stand-in renders one decodable file with one video and one audio stream.
  - The part texts concatenate exactly to the recorded narration and are joined with no inserted silence.
  - The planned source equals the recorded provenance of every narration file.
  - Booked metered spend is USD 0.00 and local compute is printed unmeasured.
- Gate: Review Gate, Verification Gate

### T-027 Deliver the duration report beside the script's expectation

- Owner: omn-dev-1-implement
- Complexity: S (confidence: low)
- Depends on: T-007, T-025
- Traces to: S-008
- Status: assumption-dependent
- Description: For the whole narration and each beat, the record states the measured duration, the expectation from the quoted rate and the signed difference in seconds, with no pass, fail or tolerance, for whichever source narrated.
- Acceptance Criteria:
  - The whole expectation reads 12 min 51 s for item 001.
  - Each beat's line states measured, expected and signed difference.
  - Audio at half and at double the expectation is reported and never refused or adjusted.
- Gate: Review Gate, Verification Gate

### T-028 Deliver the registration, model and produce commands

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-015, T-021, T-022, T-026
- Traces to: S-012, S-013
- Status: ready
- Description: The host offers the registration, installation-record, verification and own-source produce commands the design fixes, each refusing before any call or file naming each unmet precondition, and each exiting with its documented code.
- Acceptance Criteria:
  - Each command runs in the demonstration store with its documented exit code.
  - Plan-only against a store with a verified stand-in installation names the model for every part and the hashes verified.
  - No command needs a secret or a vendor credential.
- Gate: Review Gate, Verification Gate

### T-029 Deliver the screened vendor error record

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-011
- Traces to: S-014
- Status: ready
- Description: A vendor non-success response records its status and its screened, truncated code or message in every sink the design lists, and states when no code or message was readable.
- Acceptance Criteria:
  - A stub 429 with a coded body records the status and the code within the ruled length.
  - A body echoing the configured secret, a bearer value or a token-shaped string leaves none of them in any sink.
  - An empty and a binary body record the status and the unreadable statement.
- Gate: Review Gate, Verification Gate

### T-030 Deliver the demonstration label wording

- Owner: omn-dev-1-implement
- Complexity: XS (confidence: medium)
- Depends on: T-012
- Traces to: S-015
- Status: ready
- Description: Controller-decision labels of the weekly report read as the design states in a demonstration store and as before in a company store.
- Acceptance Criteria:
  - No demonstration-store label embeds an observed-reading description.
  - Company-store labels equal their delivered wording.
  - The line's figure tagging is unchanged.
- Gate: Review Gate, Verification Gate

### T-031 Deliver the plan's total-line verification statement

- Owner: omn-dev-1-implement
- Complexity: XS (confidence: low)
- Depends on: T-013
- Traces to: S-016
- Status: assumption-dependent
- Description: The plan's total line states "not verified first hand" only when a price row it uses records no first-hand verification, and otherwise names the recorded date and source.
- Acceptance Criteria:
  - Rows without the recorded verification print the not-verified wording.
  - Rows with it print the recorded date and source.
  - The estimate label and the never-netted statement are unchanged.
- Gate: Review Gate, Verification Gate

### T-032 Deliver the boundary assertions

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-014, T-021, T-023, T-026
- Traces to: S-009, S-017
- Status: ready
- Description: The architecture suite carries every assertion the design lists, each failing on its seeded probe, with every delivered assertion kept.
- Acceptance Criteria:
  - Each new assertion fails a solution build on its seeded violation and passes without it.
  - The source set's member count and the permitted process-starting types are asserted.
  - The architecture suite's executed count is at least the baseline's plus the additions.
- Gate: Review Gate, Verification Gate

### T-033 Deliver the settings sample for own-source production

- Owner: omn-dev-1-implement
- Complexity: XS (confidence: medium)
- Depends on: T-015, T-028
- Traces to: S-012, S-013, A-004
- Status: ready
- Description: The repository's settings sample states every member own-source production reads, each placeholder to be set outside the repository, and holds no secret, no vendor credential and no model file path inside the repository.
- Acceptance Criteria:
  - Every member the commands read appears in the sample with its meaning.
  - The sample holds no secret value and needs no vendor credential for own-source production.
  - Reading the sample with its placeholders set runs every documented command.
- Gate: Review Gate, Verification Gate

### T-034 Deliver the owner guide

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-028, T-033
- Traces to: S-012, S-013, A-004
- Status: ready
- Description: A repository guide states how to record one file per beat, how to register a recording with its release and performer, how the approved model is installed outside the repository and its hashes recorded and verified, and the exact produce commands for each mode and store.
- Acceptance Criteria:
  - The recording section states WAV recommended, any file accepted that decodes with zero errors and holds one audio stream and no video stream, and every property measured rather than required.
  - Every command quoted matches the delivered command and its documented exit codes.
  - The guide names the approved runtime 1.8.0 and voice and states installation outside the repository.
  - The guide holds no secret and no target, tolerance or rights position the owner has not decided.
- Gate: Review Gate, Verification Gate

### T-035 Verify the zero-spend production from a registered recording

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: medium)
- Depends on: T-025
- Traces to: S-007, S-009, S-010, A-003, A-007
- Status: assumption-dependent
- Description: Recorded evidence supports scope criteria 29, 30, 32, 33 and 34 for the recording source in the demonstration store.
- Acceptance Criteria:
  - The command output of one production ending with one rendered file and exit success.
  - Evidence that the file holds one video and one audio stream, decodes with zero errors and its runtime is stated beside 13 min 39 s.
  - Evidence that every still sits within its beat's measured span from the recorded part durations.
  - Readings of booked operations, the cap's counted total before and after, and the target store named.
- Gate: Review Gate, Verification Gate

### T-036 Verify the zero-spend production from the model stand-in

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: medium)
- Depends on: T-026
- Traces to: S-001, S-003, S-009, S-010, A-003, A-006
- Status: assumption-dependent
- Description: Recorded evidence supports scope criteria 4, 8, 29, 30, 32, 33 and 34 for the model source with an in-test stand-in in the demonstration store.
- Acceptance Criteria:
  - The command output of one production ending with one rendered file and exit success.
  - Evidence that the part texts concatenate to the recorded narration and the file count equals the planned parts.
  - Evidence that the printed plan's source equals every narration file's recorded provenance, and that a state change between plan and run stops the run.
  - Readings of booked operations, the cap's counted total and the unmeasured local compute line.
- Gate: Review Gate, Verification Gate

### T-037 Verify the source set and the selection rule

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: medium)
- Depends on: T-026
- Traces to: S-001, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports scope criteria 1, 2 and 3.
- Acceptance Criteria:
  - Evidence of the four-member set from a unit test and an architecture test.
  - Two plan-only runs over fixed fixtures printing identical per-part lines.
  - A table over recording present, incomplete and without release, model verified, absent and mismatched, and each designation, with the outcome of each row.
- Gate: Review Gate, Verification Gate

### T-038 Verify that the vendor route is never chosen

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-026
- Traces to: S-002, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports scope criteria 5, 6 and 7, with the disposition of each delivered metered-production test as the design recorded it.
- Acceptance Criteria:
  - Evidence over a store holding a priced vendor route and a configured credential variable, in every mode, of no vendor source and zero stub requests.
  - Evidence that changing every settings value naming the route leaves the plan excluding it, with the owner's decision named.
  - The full suite's result with every vendor contract test passing.
- Gate: Review Gate, Verification Gate

### T-039 Verify the model boundary, its bound and its isolation from the network

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-023, T-032
- Traces to: S-003, A-003, A-006
- Status: assumption-dependent
- Description: Recorded evidence supports scope criteria 9, 10 and 11.
- Acceptance Criteria:
  - The architecture suite failing on a probe type that starts a process outside the permitted types.
  - A stand-in process outlasting a short bound, terminated, its stage recorded failed, with no open transaction observed during the run.
  - Zero network requests from a stand-in run under a counting handler or denied network.
- Gate: Review Gate, Verification Gate

### T-040 Verify model provenance and observed repeatability

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-026
- Traces to: S-004, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports scope criteria 12, 13 and 14.
- Acceptance Criteria:
  - A read of every provenance field for every narration file of a stand-in run, each non-empty.
  - A file swapped between approval and run, with the run refusing or recording the swapped hash, never the approved one.
  - Deterministic and non-deterministic stand-ins with their recorded repeatability statements.
- Gate: Review Gate, Verification Gate

### T-041 Verify the model installation check

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: medium)
- Depends on: T-022
- Traces to: S-005, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports scope criteria 15 and 16.
- Acceptance Criteria:
  - A matching fixture verifying, and a missing and a one-byte-changed file each refusing with the file and both hashes named.
  - A location inside the repository root refused.
  - An inspection of the tracked tree finding no model, voice or runtime file.
- Gate: Review Gate, Verification Gate

### T-042 Verify recording registration

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: medium)
- Depends on: T-021
- Traces to: S-006, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports scope criteria 17, 18, 19 and 20.
- Acceptance Criteria:
  - Item 001's thirteen beats with complete, missing, extra, duplicate and whole-file fixture sets and each outcome.
  - A release record read holding only the approved form's fields, and an inspection of the tracked tree and schema finding no release document.
  - Tone fixtures rendered inside the test, a truncated file and a file with a video stream, with each outcome.
  - A registration record read with every field non-empty or explicitly absent.
- Gate: Review Gate, Verification Gate

### T-043 Verify that a production narrates only the registered bytes under a recorded release

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: medium)
- Depends on: T-025
- Traces to: S-007, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports scope criteria 21, 22 and 23.
- Acceptance Criteria:
  - A one-byte change after registration stopping the production before narration with both hashes named.
  - A registration without a release never selected, the plan naming the missing release.
  - A registration with every optional field missing, printed absent.
- Gate: Review Gate, Verification Gate

### T-044 Verify narration measurement and the duration report

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-026, T-027
- Traces to: S-008, A-003, A-008
- Status: assumption-dependent
- Description: Recorded evidence supports scope criteria 24, 25, 26, 27 and 28 for both own sources.
- Acceptance Criteria:
  - A fixture whose header disagrees with its decoded audio, measured by its decoded duration with the basis recorded.
  - Tone fixtures of known level with value, measure name and unit recorded.
  - The joined narration's measured duration equal to the sum of its parts within one audio frame, with no gain change.
  - The duration report read for half and double expectation, with no tolerance figure anywhere.
- Gate: Review Gate, Verification Gate

### T-045 Verify that nothing publishes

- Owner: omn-dev-2-reviewer
- Complexity: XS (confidence: high)
- Depends on: T-032
- Traces to: S-009, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports scope criterion 31.
- Acceptance Criteria:
  - The delivered structural-absence assertions passing after the change.
  - The held-stage test passing, the item held short of publish-ready after each own-source production.
  - Evidence that no new type reaches a publication, upload or channel-configuration path.
- Gate: Review Gate, Verification Gate

### T-046 Verify store admission and store isolation

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: medium)
- Depends on: T-019, T-035, T-036
- Traces to: S-011, A-003, A-007
- Status: assumption-dependent
- Description: Recorded evidence supports scope criteria 35, 36 and 37.
- Acceptance Criteria:
  - Store-guard unit results over every designation and mode, with the fake refused against a company store.
  - An inspection of test configuration and connection records naming only the demonstration database.
  - An inspection of test sources and the implementation report's commands finding no download and no real model run.
- Gate: Review Gate, Verification Gate

### T-047 Verify the owner guide and settings sample by running every documented command

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: medium)
- Depends on: T-034
- Traces to: S-012, S-013, A-003, A-004
- Status: assumption-dependent
- Description: Recorded evidence supports scope criteria 38, 39, 40, 41 and 43.
- Acceptance Criteria:
  - A review of the recording section against the registration rules and the measured properties.
  - The output and exit code of every documented command run in the demonstration store, the verify command against a stand-in installation.
  - A review finding no code change, secret or vendor credential needed to produce item 001 with the in-house voice.
- Gate: Review Gate, Verification Gate

### T-048 Verify the screened vendor error record

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: medium)
- Depends on: T-029
- Traces to: S-014, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports scope criteria 44, 45 and 46.
- Acceptance Criteria:
  - A stub 429 with a coded body and the recorded status and code within the ruled length.
  - A scan of every recorded field and output after stubs echoing a fake secret, a bearer value and a token-shaped string, finding none.
  - An empty and a binary body with the recorded status and unreadable statement.
- Gate: Review Gate, Verification Gate

### T-049 Verify the demonstration label wording

- Owner: omn-dev-2-reviewer
- Complexity: XS (confidence: medium)
- Depends on: T-030
- Traces to: S-015, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports scope criteria 47 and 48.
- Acceptance Criteria:
  - A demonstration store holding a fake run's operations, with no controller-decision label reading as observed.
  - A company-designated fixture store with labels equal to their delivered wording.
  - The target store named for each.
- Gate: Review Gate, Verification Gate

### T-050 Verify the plan's total line

- Owner: omn-dev-2-reviewer
- Complexity: XS (confidence: medium)
- Depends on: T-031
- Traces to: S-016, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports scope criterion 49.
- Acceptance Criteria:
  - Price rows without the recorded first-hand verification printing the not-verified wording.
  - Price rows with it printing the recorded date and source.
  - The field read named as the design fixed it.
- Gate: Review Gate, Verification Gate

### T-051 Verify the tenth resource on a store of the company store's recorded shape

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-018
- Traces to: S-011, S-013, A-003, A-011
- Status: assumption-dependent
- Description: Recorded evidence shows the ten ordered resources apply, through the delivered schema-install path, to a demonstration store first brought to the company store's recorded shape, and reapply, changing no delivered row.
- Acceptance Criteria:
  - A demonstration store holding nine resources, a prepared item, a failed version 2 and one booked estimate operation, its rows read before.
  - The tenth resource applied and the ordered resources reapplied, each ending without error.
  - Every delivered row read after equal to before, and the cap's counted total unchanged.
- Gate: Review Gate, Verification Gate

### T-052 Verify the round trip of every timeout, hold, clock and hash on the narration path

- Owner: omn-dev-2-reviewer
- Complexity: L (confidence: low)
- Depends on: T-021, T-023, T-026
- Traces to: S-003, S-004, S-007, S-008, A-001, A-003
- Status: assumption-dependent
- Description: Recorded evidence shows every item T-009 enumerated ends under its named outcome and every recorded hash, duration and identity is over the bytes and files actually used.
- Acceptance Criteria:
  - For each reader of a registered recording, a byte changed before that reader and the run stopping by name.
  - A model file swapped between verification and load, refused or recorded with the swapped hash.
  - Open-transaction observations during a stalled stand-in, a long decode and a large-file hash, each finding none.
  - Every recorded instant equal to the datastore's with the process clock skewed a day ahead and behind.
- Gate: Review Gate, Verification Gate

### T-053 Confirm the binding constraints hold after the change

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-016, T-027, T-029, T-030, T-031, T-032, T-034
- Traces to: S-017, S-018, A-003, A-007
- Status: assumption-dependent
- Description: Recorded evidence shows the full suite and the architecture suite against the baseline, zero spend, no network reached, no download, the company store untouched, and no invented quantity or figure presented as observed.
- Acceptance Criteria:
  - The full live suite's executed counts against the baseline, every baseline pass still passing, run in the demonstration database only.
  - A solution build failing on a seeded violation of each new assertion.
  - Review evidence that no target, tolerance, format requirement, rights position or performer identity is recorded outside test fixtures.
  - Evidence that every stand-in, tone or fake figure is labelled demonstration.
- Gate: Review Gate, Verification Gate

### T-054 Record what the change delivers and the orchestrator's post-phase run

- Owner: omn-documentation
- Complexity: M (confidence: low)
- Depends on: T-035, T-036, T-037, T-038, T-039, T-040, T-041, T-042, T-043, T-044, T-045, T-046, T-047, T-048, T-049, T-050, T-051, T-052, T-053
- Traces to: S-013, S-018, A-004, A-010, A-011
- Status: assumption-dependent
- Description: The release record states what the demonstration productions evidenced, labelled demonstration, the ordered steps of the orchestrator's company-store run with the readings each takes, the conditions scope criteria 42, 50, 51 and 52 set, and every open owner question unanswered.
- Acceptance Criteria:
  - The record orders the steps: fresh backup, readings, tenth resource applied, installation hashes entered, verification, plan-only, the owner's go, production, post-run readings.
  - The record states zero spend in the phases and the cap's counted total unchanged.
  - The record labels every stand-in and tone figure demonstration and presents none as observed.
  - The record states the installation of 2026-10-10 and its recorded hashes as the expected values, the orchestrator's trial figures as trial and not company records, and the release as the precondition of a recording-sourced run.
- Gate: Closure Gate

## Dependencies

### 8.1 Dependency Edges

| From | To | Type | Justification |
|---|---|---|---|
| T-001 | T-002 | decision-gate | The bar is placed in or beside the rule that design fixes |
| T-001 | T-003 | decision-gate | The mode's form depends on the sources that design names |
| T-001 | T-010 | decision-gate | Costing depends on which sources reach the capability boundary |
| T-005 | T-008 | contract | Generation settings recorded are those the boundary passes |
| T-006 | T-008 | contract | Hash fields are those the verification design computes |
| T-004 | T-009 | decision-gate | Registration's decodes and holds are enumerated from that design |
| T-005 | T-009 | decision-gate | The model run's bound and holds are enumerated from that design |
| T-006 | T-009 | decision-gate | Large-file hashing is enumerated from that design |
| T-007 | T-009 | decision-gate | Measurement decodes are enumerated from that design |
| T-005 | T-014 | decision-gate | The permitted process-starting types come from that design |
| T-003 | T-015 | contract | Commands bind to the mode that design fixes |
| T-004 | T-015 | contract | The registration command binds to that design |
| T-006 | T-015 | contract | The record and verify commands bind to that design |
| T-001 | T-017 | contract | The rule is built as designed |
| T-002 | T-017 | contract | The bar is built as designed |
| T-003 | T-018 | contract | The mode set is extended as designed |
| T-004 | T-018 | contract | Registration and release records are shaped as designed |
| T-006 | T-018 | contract | Installation records are shaped as designed |
| T-008 | T-018 | contract | Provenance records are shaped as designed |
| T-010 | T-018 | contract | Any cost record for own sources is shaped as designed |
| T-003 | T-019 | contract | Admissions follow that design's table |
| T-017 | T-019 | produces-consumes | The guard reads the source set that task delivers |
| T-007 | T-020 | contract | The measures are those that design names |
| T-009 | T-020 | contract | Measurement runs under the ruled bounds |
| T-004 | T-021 | contract | Registration follows that design |
| T-018 | T-021 | produces-consumes | Registration writes the records that resource creates |
| T-020 | T-021 | produces-consumes | Registration records the measurements that task delivers |
| T-006 | T-022 | contract | Recording and verification follow that design |
| T-018 | T-022 | produces-consumes | Installation hashes are written where that resource creates |
| T-005 | T-023 | contract | The run follows that boundary design |
| T-009 | T-023 | contract | The run applies the ruled bound and no-hold rule |
| T-022 | T-023 | produces-consumes | The run starts only on files that verification passes |
| T-008 | T-024 | contract | Provenance fields follow that design |
| T-023 | T-024 | produces-consumes | Provenance records what the run loaded and produced |
| T-010 | T-025 | contract | The produce path books and prints as designed |
| T-017 | T-025 | produces-consumes | The produce path applies the delivered rule |
| T-019 | T-025 | produces-consumes | The produce path is admitted by the delivered guard |
| T-020 | T-025 | produces-consumes | Part durations come from the delivered measurement |
| T-021 | T-025 | produces-consumes | The produce path narrates registered recordings |
| T-024 | T-026 | produces-consumes | Model narration carries the delivered provenance |
| T-025 | T-026 | produces-consumes | The model path extends the source-aware produce path |
| T-007 | T-027 | contract | Expectations follow that design |
| T-025 | T-027 | produces-consumes | The report reads the measured parts of a production |
| T-015 | T-028 | contract | Commands follow that design |
| T-021 | T-028 | produces-consumes | The registration command exposes the delivered registration |
| T-022 | T-028 | produces-consumes | The record and verify commands expose the delivered verification |
| T-026 | T-028 | produces-consumes | The produce command exposes both own sources |
| T-011 | T-029 | contract | Screening follows that design |
| T-012 | T-030 | contract | Wording follows that design |
| T-013 | T-031 | contract | The statement follows that design |
| T-014 | T-032 | contract | Assertions follow that design |
| T-021 | T-032 | produces-consumes | Assertions cover the registration types |
| T-023 | T-032 | produces-consumes | Assertions cover the model boundary |
| T-026 | T-032 | produces-consumes | Assertions cover the own-source produce path |
| T-015 | T-033 | contract | The sample states the members that design fixes |
| T-028 | T-033 | produces-consumes | The sample serves the delivered commands |
| T-028 | T-034 | produces-consumes | The guide quotes the delivered commands |
| T-033 | T-034 | produces-consumes | The guide refers to the delivered sample |
| T-025 | T-035 | verification | Evidences the recording-sourced production |
| T-026 | T-036 | verification | Evidences the model-sourced production |
| T-026 | T-037 | verification | The rule table needs both own sources delivered |
| T-026 | T-038 | verification | The bar is evidenced over the complete produce path |
| T-023 | T-039 | verification | Evidences the model boundary |
| T-032 | T-039 | verification | The process-start probe needs the delivered assertion |
| T-026 | T-040 | verification | Provenance is read from a model-sourced production |
| T-022 | T-041 | verification | Evidences installation verification |
| T-021 | T-042 | verification | Evidences registration |
| T-025 | T-043 | verification | Evidences the registered-bytes round trip in production |
| T-026 | T-044 | verification | Measurement is evidenced for the model source |
| T-027 | T-044 | verification | Evidences the duration report |
| T-032 | T-045 | verification | Evidences the publication absences after the change |
| T-019 | T-046 | verification | Evidences the guard |
| T-035 | T-046 | verification | Store isolation is read from the recording production's evidence |
| T-036 | T-046 | verification | Store isolation is read from the model production's evidence |
| T-034 | T-047 | verification | Evidences the guide and its commands |
| T-029 | T-048 | verification | Evidences the screened record |
| T-030 | T-049 | verification | Evidences the label wording |
| T-031 | T-050 | verification | Evidences the total line |
| T-018 | T-051 | verification | Evidences the tenth resource over the company store's shape |
| T-021 | T-052 | verification | Registration readers are round-tripped |
| T-023 | T-052 | verification | The model run's bound and file swap are round-tripped |
| T-026 | T-052 | verification | Production readers and instants are round-tripped |
| T-016 | T-053 | verification | The suite is compared with the recorded baseline |
| T-027 | T-053 | verification | Every implementation leaf is complete before the constraint check |
| T-029 | T-053 | verification | Every implementation leaf is complete before the constraint check |
| T-030 | T-053 | verification | Every implementation leaf is complete before the constraint check |
| T-031 | T-053 | verification | Every implementation leaf is complete before the constraint check |
| T-032 | T-053 | verification | The seeded-violation build needs the delivered assertions |
| T-034 | T-053 | verification | The guide is reviewed for invented quantities |
| T-035 | T-054 | produces-consumes | The record states what T-035 evidenced |
| T-036 | T-054 | produces-consumes | The record states what T-036 evidenced |
| T-037 | T-054 | produces-consumes | The record states what T-037 evidenced |
| T-038 | T-054 | produces-consumes | The record states what T-038 evidenced |
| T-039 | T-054 | produces-consumes | The record states what T-039 evidenced |
| T-040 | T-054 | produces-consumes | The record states what T-040 evidenced |
| T-041 | T-054 | produces-consumes | The record states what T-041 evidenced |
| T-042 | T-054 | produces-consumes | The record states what T-042 evidenced |
| T-043 | T-054 | produces-consumes | The record states what T-043 evidenced |
| T-044 | T-054 | produces-consumes | The record states what T-044 evidenced |
| T-045 | T-054 | produces-consumes | The record states what T-045 evidenced |
| T-046 | T-054 | produces-consumes | The record states what T-046 evidenced |
| T-047 | T-054 | produces-consumes | The record states what T-047 evidenced |
| T-048 | T-054 | produces-consumes | The record states what T-048 evidenced |
| T-049 | T-054 | produces-consumes | The record states what T-049 evidenced |
| T-050 | T-054 | produces-consumes | The record states what T-050 evidenced |
| T-051 | T-054 | produces-consumes | The record states what T-051 evidenced |
| T-052 | T-054 | produces-consumes | The record states what T-052 evidenced |
| T-053 | T-054 | produces-consumes | The record states what T-053 evidenced |
| omn-orchestrator, for the demonstration database | T-016 | external | The baseline needs the separate demonstration database served |
| omn-orchestrator, for the demonstration database | T-035 | external | The live production needs the separate demonstration database served |
| omn-orchestrator, for the demonstration database | T-036 | external | The live production needs the separate demonstration database served |
| omn-orchestrator, for the demonstration database | T-053 | external | The full live suite needs the separate demonstration database served |
| omn-orchestrator, for the installed media tool | T-020 | external | Decoded measurement runs the media tool installed and located by configuration |

### 8.2 External Dependencies

| Responsible party | What is needed | Blocks |
|---|---|---|
| omn-orchestrator, for the demonstration database | PostgreSQL 17 serving the separate demonstration database, and no other database named by any test or demonstration (R-022, R-028) | T-016, T-035, T-036, T-053 |
| omn-orchestrator, for the installed media tool | The media tool at version 9.0.2 with its probe, already installed with the owner's approval and located by configuration (R-027) | T-020 |
| The owner, through omn-orchestrator | A fresh backup of the company store, the tenth resource applied to it, the installation hashes of 2026-10-10 entered where the design places them, a plan-only run, and the explicit go; the runtime and voice are already installed outside the repository (R-025, R-026, R-030) | none in this plan; the post-phase run after this change closes |
| The CEO | Thirteen beat recordings and the signed release in the approved form (R-029) | none in this plan; the recording-sourced run of item 001 |

### 8.3 Implementation Order

- Wave 1: T-001, T-004, T-005, T-006, T-007, T-011, T-012, T-013, T-016
- Wave 2: T-002, T-003, T-008, T-009, T-010, T-014, T-029, T-030, T-031
- Wave 3: T-015, T-017, T-018, T-020, T-048, T-049, T-050
- Wave 4: T-019, T-021, T-022, T-051
- Wave 5: T-023, T-025, T-041, T-042
- Wave 6: T-024, T-027, T-035, T-043
- Wave 7: T-026
- Wave 8: T-028, T-032, T-036, T-037, T-038, T-040, T-044, T-052
- Wave 9: T-033, T-039, T-045, T-046
- Wave 10: T-034
- Wave 11: T-047, T-053
- Wave 12: T-054

The recording-sourced end-to-end production is delivered in wave 5 (T-025) and evidenced in wave 6 (T-035); the model-sourced one is delivered in wave 7 (T-026) and evidenced in wave 8 (T-036). No should-have task (T-029, T-030, T-031) and no command or documentation task lies on either path. The orchestrator's post-phase preparation is complete when T-051 (wave 4) has proven the tenth resource over the company store's shape, T-033 and T-034 (waves 9 and 10) have delivered the settings sample and the guide, and T-047 (wave 11) has run every documented command.

## Suggested Workflow

Selected workflow: implement-feature

Selected because the change delivers new capability against an approved scope with measurable acceptance criteria, and the run is already routed through this workflow's phase model.

| Phase | Tasks |
|---|---|
| scope-and-acceptance | none; the phase is complete and its artifact is this plan's upstream input |
| execution-planning | none; this plan is the phase's own output |
| solution-design-and-risk-assessment | T-001, T-002, T-003, T-004, T-005, T-006, T-007, T-008, T-009, T-010, T-011, T-012, T-013, T-014, T-015 |
| implementation | T-016, T-017, T-018, T-019, T-020, T-021, T-022, T-023, T-024, T-025, T-026, T-027, T-028, T-029, T-030, T-031, T-032, T-033, T-034 |
| quality-review | T-035, T-036, T-037, T-038, T-039, T-040, T-041, T-042, T-043, T-044, T-045, T-046, T-047, T-048, T-049, T-050, T-051, T-052, T-053 |
| documentation-and-release-handoff | T-054 |

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
| architecture-analysis | T-001, T-002, T-003, T-004, T-005, T-006, T-007, T-008, T-009, T-010, T-011, T-012, T-013, T-014, T-015 | architect | Primary |
| technical-approach-definition | T-004, T-005, T-006, T-007, T-008, T-015 | architect | Primary |
| option-evaluation | T-002, T-003, T-005, T-006, T-007 | architect | Primary |
| structural-risk-analysis | T-004, T-005, T-006, T-009, T-014 | architect | Primary |
| reuse-assessment | T-001, T-003, T-010, T-011, T-012, T-013 | architect | Primary |
| implementation-delivery | T-016, T-017, T-018, T-019, T-020, T-021, T-022, T-023, T-024, T-025, T-026, T-027, T-028, T-029, T-030, T-031, T-032, T-033, T-034 | omn-dev-1-implement | Primary |
| quality-verification | T-035, T-036, T-037, T-038, T-039, T-040, T-041, T-042, T-043, T-044, T-045, T-046, T-047, T-048, T-049, T-050, T-051, T-052, T-053 | omn-dev-2-reviewer | Primary |
| validation-design | T-037, T-044, T-051, T-052 | omn-dev-2-reviewer | Primary |
| code-review | T-038, T-041, T-042, T-045, T-046, T-047, T-053 | omn-dev-2-reviewer | Primary |
| documentation | T-054 | omn-documentation | Primary |
| release-communication | T-054 | omn-documentation | Primary |

### 10.2 Required Skills

| Skill | File | Tasks | Level |
|---|---|---|---|
| S01 | skills/architecture/clean-architecture-checklist.md | T-001, T-003, T-005, T-014, T-032 | Advisory |
| S02 | skills/business/domain-modeling.md | T-001, T-002, T-004, T-007, T-008, T-010, T-015 | Primary |
| S03 | skills/dotnet/engineering-playbook.md | T-017, T-018, T-019, T-020, T-021, T-022, T-023, T-024, T-025, T-026, T-027, T-028, T-029, T-030, T-031, T-032 | Secondary |
| S06 | skills/database/database-engineering.md | T-003, T-009, T-018, T-051 | Secondary |
| S07 | skills/testing/testing-strategy.md | T-016, T-035, T-036, T-037, T-038, T-039, T-040, T-041, T-042, T-043, T-044, T-045, T-046, T-047, T-048, T-049, T-050, T-051, T-052, T-053 | Secondary |
| S09 | skills/security/secure-engineering.md | T-004, T-005, T-006, T-011, T-022, T-023, T-029, T-048 | Advisory |
| S10 | skills/git/git-collaboration.md | T-016, T-034, T-054 | Advisory |
| S11 | skills/logging/observability-logging.md | T-029, T-054 | Advisory |
| S12 | skills/error-handling/error-handling-strategy.md | T-009, T-021, T-023 | Advisory |

## Acceptance Criteria

1. After the phases, the orchestrator's run of item 001 in the company store with the owner-approved model ends with exit code 0, one rendered video decoded end to end with zero errors, model narration in every part with the provenance of the approved runtime and voice and the hashes recorded at installation, booked metered spend USD 0.00, the cap's counted total at USD 0.007695 estimate, zero network requests and the item held short of publish-ready; inside the phases, the model-stand-in production in the demonstration store shows the same properties. Verifies business objective 1. Evidence: the orchestrator's plan-only and run records and readings for scope criteria 42, 50, 51 and 52, and the evidence recorded in T-036, T-039 and T-040.
2. In the demonstration store, a registered tone-fixture recording of thirteen beat files with a recorded release narrates a production that renders one decodable file from exactly the registered bytes, and the owner guide's registration and produce commands run as documented. Verifies business objective 2. Evidence: the evidence recorded in T-035, T-042, T-043 and T-047.
3. Two plan-only runs over the same state print the same source and reason for every part before any process or file, and over a store holding a priced vendor route with a configured credential variable no mode chooses the vendor or makes a request, the plan naming the owner's decision. Verifies business objective 3. Evidence: the evidence recorded in T-037 and T-038.
4. Every narration artifact of both demonstration productions records its source's provenance or registration and release, hashes over the bytes used, and duration and loudness from decoded audio beside the 12 min 51 s expectation as a signed difference, with no target, tolerance or adjustment recorded anywhere. Verifies business objective 4. Evidence: the evidence recorded in T-040, T-041, T-044, T-052 and T-053.
5. No test, demonstration or phase connects to the company store, the tenth resource applies and reapplies over a store of the company store's recorded shape changing no delivered row, and the release record orders every step of the orchestrator's run. Verifies business objective 5. Evidence: the evidence recorded in T-046 and T-051, and the release record produced by T-054.
6. A stub vendor's 429 records its status and screened code, no echoed secret or token survives in any sink, no demonstration controller-decision label reads as observed, and the plan's total line states the recorded verification of its price rows. Verifies business objective 6. Evidence: the evidence recorded in T-048, T-049 and T-050.
7. Every test passing at the baseline still passes in the demonstration store, the architecture suite grows by the new assertions with each failing on its seeded violation, and nothing publishes. Verifies business objective 7. Evidence: the baseline recorded by T-016 and the evidence recorded in T-045 and T-053.

## Definition of Done

- [ ] All seven plan acceptance criteria are verified with recorded evidence, criterion 1 by the orchestrator's post-phase run record
- [ ] Design Gate, Review Gate, Verification Gate and Closure Gate are approved with owners recorded
- [ ] Every task acceptance criterion is satisfied or formally waived with the waiver recorded
- [ ] Every assumption is confirmed by its named role or converted to a recorded decision
- [ ] Every risk is closed or accepted with its named owner recorded
- [ ] Every open question is closed or explicitly accepted by its named owner
- [ ] Metered spend in the phases is recorded as USD 0.00 and the cap's counted total as USD 0.007695 estimate
- [ ] No phase connection to the company store and no download is recorded in any phase's evidence
- [ ] The owner guide, the settings sample and the release record with its release-impact notes are published
- [ ] Durable outcomes are recorded to memory per `memory/memory-governance.md`

## Open Questions

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| Q-001 | Should a narration source record a quality rating at all, given the vendor route records rated quality 0 to mean none claimed? Carried from the scope; prepared in T-008 and ruled at the Design Gate (A-005). | no | omn-tech-lead | T-008, T-024 |
| Q-002 | Scope criterion 5 bars the vendor in any mode and store, while the delivered zero-spend production test and the cancellation-during-vendor-call test drive a metered production through a stub vendor and assert its requests, and criterion 7 keeps the vendor's existing tests passing; where does the bar sit, and how is each delivered test dispositioned without silently changing an assertion? | no | architect | T-002, T-017, T-038, T-053 |
| Q-003 | The delivered probe reads every duration, parts, the joined narration and the rendered video, from the container's duration field; narration moves to decoded audio by scope criterion 24, while criterion 29 keeps the runtime "measured from the file" as delivered; does the video runtime keep the container-field basis, labelled, or move to decoded media? | no | architect | T-007, T-020, T-035, T-036 |
| Q-004 | Price rows record a source, a verified-on date and a re-fetch date, and no field marks a first-hand verification; which recorded value does the total line read as first hand for scope criterion 49? | no | architect | T-013, T-031, T-050 |
| Q-005 | Scope criterion 51 requires zero network requests in the orchestrator's company-store run of the real model, which no test can show; what evidence does the orchestrator record, and how is the runtime kept from fetching a voice by name? | no | architect | T-005, T-015, T-054 |
| Q-006 | The owner's decision record holds hashes of the three voice files only; where does the run's installation record live: in the store, which makes entering it a write to the company store before the go, or in a file outside the repository, which the run would then trust as configuration; and are the runtime environment's files (its executable and packages) hashed, and if so which? | no | architect | T-006, T-018, T-022, T-028 |
| Q-007 | Plan-only in the company store must name the model's hashes as verified (scope criterion 42) while plan-only starts no process and writes no file; can the runtime's version and the voice's identity be established from installed files alone? | no | architect | T-006, T-022, T-028 |
| Q-008 | Item material quotes no delivery rate, and 1,929 words at 150 a minute is 771.6 s while the script prints 12 min 51 s; is the rate quoted into item 001's material from the script, and what word rule and rounding give each beat's expectation? | no | architect | T-007, T-027, T-044 |
| Q-009 | What values govern the model-run bound, any registration decode or measurement bound, and the error-body truncation length? Non-blocking: the architect proposes in T-009 and T-011 and omn-tech-lead rules at the Design Gate; no value is invented here. | no | omn-tech-lead | T-009, T-011, T-020, T-021, T-023, T-029 |

## Traceability Matrix

| Statement | Covered by |
|---|---|
| S-001 | T-001, T-017, T-025, T-026, T-036, T-037, A-001, A-009 |
| S-002 | T-002, T-017, T-038, A-002, Q-002 |
| S-003 | T-005, T-009, T-023, T-026, T-036, T-039, T-052, A-005, A-006 |
| S-004 | T-006, T-008, T-018, T-024, T-040, T-052, A-012, Q-001 |
| S-005 | T-006, T-022, T-041, A-010, Q-006, Q-007 |
| S-006 | T-004, T-018, T-021, T-042 |
| S-007 | T-001, T-004, T-009, T-021, T-025, T-035, T-043, T-052 |
| S-008 | T-007, T-009, T-020, T-027, T-044, T-052, A-008, Q-003, Q-008 |
| S-009 | T-007, T-014, T-025, T-026, T-032, T-035, T-036, T-045 |
| S-010 | T-010, T-025, T-026, T-035, T-036 |
| S-011 | T-003, T-018, T-019, T-046, T-051, A-007 |
| S-012 | T-015, T-028, T-033, T-034, T-047, A-004 |
| S-013 | T-003, T-006, T-015, T-018, T-022, T-028, T-033, T-034, T-047, T-051, T-054, A-010, A-011, Q-005 |
| S-014 | T-011, T-029, T-048 |
| S-015 | T-012, T-030, T-049 |
| S-016 | T-013, T-031, T-050, Q-004 |
| S-017 | T-014, T-016, T-032, T-053 |
| S-018 | T-053, T-054 |

Statement register, normalised from the supplied feature request and the upstream scope definition of this run (digest sha256:e4b36a6957db10061b8d418e1a4416b7): the first sixteen statements match the upstream in-scope items in order and carry the same identifiers; S-017 carries the request's requirement that the completed waves are built upon and none re-created, and S-018 its binding constraint that no threshold, target, tolerance or rights position is invented. Q-001 carries the scope's one question under the same identifier and owner; Q-002 to Q-009 are raised here. Upstream acceptance criteria are cited by ordinal in the order the scope definition lists them, because their identifiers share this plan's assumption prefix. The in-house model is named where the approved scope and the owner's decision name it, because scope criteria 12 and 40 require that name to be read from the provenance and the guide. Repository rescan: the task context names no relevant module or file, so the delivered source in the feature worktree was read before planning as the dispatch briefing directs; at head 4bc0029 the counts are 135 C# sources and 8 project files under the source tree, nine schema resources, and 57 test attributes in the architecture project, matching the scope's count.
