```yaml
design:
  designId: TD-MC16-PILOT-TOOLCHAIN
  changeReference: runs/inputs/mc-16-feature-request.md
  sourceInputs:
    - type: feature-request
      reference: runs/inputs/mc-16-feature-request.md
    - type: business-intent
      reference: runs/inputs/mc-16-business-intent.md
    - type: execution-plan
      reference: runs/run-535f3f938f32/states/execution-planning/artifacts/execution-plan.md
  producedBy: architect
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  decisionRecords: [D-001, D-002, D-003, D-005]
  consumesPlan: runs/run-535f3f938f32/states/execution-planning/artifacts/execution-plan.md
  inputDigest: sha256:ec93d4b98dcd02f082904536fdeefdd7
  contextDigest: sha256:11b68469cf9e593c79e0dc76de8cb64f
```

## Metadata

- Feature or Change ID: MC-16, run run-535f3f938f32, invocation inv-535f3f938f32-03-001, phase solution-design-and-risk-assessment; this package is the design planner task T-001 asks for.
- Author: architect
- Reviewers: tech-lead (accepting Design Gate owner), qa, product-owner, dev-1-implement
- Last Updated: 2026-10-26
- Upstream evidence cited by identifier and digest: the scope definition (sha256:30a501cb157980ab7ad14e038c3834d6), cited as "scope item n", "scope criterion n", "scope decision n" and "scope exclusion n"; the execution plan as supplied (envelope input), cited by planner task T-nnn only.
- Repository rescan: the task context listed no relevant modules or files, so the code under src/, tests/, graphics/ and the kit files under pilot-affiliate-vn/pilot-kit/ were scanned to establish the facts in 4.1. Reason: the context named no module, and the design needs the dependency closure of every reuse candidate.
- Skills applied: S01, S03, S06, S09, S02, S08, S12. The database area is lexical: this design changes no schema and leaves the company store untouched (C-018). The performance area is lexical: no performance target is stated (Operational Considerations). The security area is real (Operational Considerations, R-006 to R-009, R-016).
- No implementation work was performed and no external system was accessed.

## Objective

- Desired outcome: one product becomes one ready-to-review 1080x1920 video package through a toolchain that runs offline, carries no generative or speech component, and binds the CEO's approval to the video's hash (S-001).
- Architectural objectives:
  - A standalone toolchain boundary whose dependency closure excludes the company store, the assembly that implements the in-house speech model, and every networking assembly (S-012, S-013, S-015).
  - A single composition route through the installed media tool that opens no socket and starts no script runtime or browser (S-006, S-007, S-009, S-024).
  - Content addressing at the package boundary, where every input and the output video are digested and an approval is valid only against the video's recomputed hash (S-008, S-016, S-020).
  - A deterministic text path from fact sheet to script whose term classes, exemptions, sentence frames and call-to-action list are held as versioned data inside the toolchain (S-004, S-005, S-018, S-021).
  - A pilot ledger held as a file outside the repository and outside the company store (S-020, S-025).
  - Test placement that separates pure unit tests from integration tests needing the installed media tool, with every media fixture generated at test time (S-017, S-023).
- In scope: the new toolchain project and its test project, the solution entries for both, kit file 03 Part B row 6 and Part F, kit file 04 item B13, and the use of the installed media tool, the installed face and FontCoverage unchanged.
- Out of scope:
  - Any change to MediaCompany.Production, MediaCompany.Persistence, MediaCompany.Host, MediaCompany.Capability, MediaCompany.Credentials, graphics/render.mjs or graphics/drawing (S-009, S-015).
  - The company store, frozen Wave 12 and Wave 13 material (S-015).
  - Generating the presenter clip, the presenter tool, presenter account management, and enforcing one presenter on one account (S-019).
  - Recording, recognising or checking who narrated a recording; the operator may be an employee reading in their own voice (S-027).
  - Publishing, sign-in, upload, seller contact and purchase (S-010).
  - Any newly downloaded component (S-011).

## Requirements Summary

- Functional requirements:
  - Accept a fact sheet only with the eleven header fields, complete fact rows and present screenshot files, and refuse banned content naming the row and the term (S-002, S-004).
  - Refuse banned classes (a) to (g) by whole-word, case-insensitive matching after NFC normalization in fact rows, overrides, script sentences and the call to action (S-004, S-018).
  - Generate the script deterministically in kit Part B order with byte-exact disclosures, one to three fact sentences, a call to action from the fixed list, and one Part E line per sentence (S-005, S-026).
  - Admit exactly one recording file per video, refuse it above 60.0 seconds or when its digest was recorded for another video (S-003, S-013, S-020).
  - Run the vertical-output and character-coverage checks before composition and stop on failure (S-007, S-024).
  - Compose 1080x1920 output with image motion, the presenter clip in every sampled frame, captions inside their windows, and the disclosure overlay in every frame (S-006).
  - Assemble a package with the video's SHA-256, the provenance list, six input digests and the kit 04 pre-fill (S-008).
  - Bind the approval to the video's hash, voided by any byte change, and publish nothing (S-008, S-010, S-016).
  - Refuse packages that break the pilot ledger limits (S-025).
- Non-functional requirements:
  - Zero network connection attempts in the sample run, and zero running cost (S-010, S-011, S-014).
  - No language-model, speech-recognition or text-to-speech component in the dependency list or the invoked-tool log (S-012, S-013).
  - No media file added to the repository (S-017).
  - The .NET test run needs neither the company store database nor a container service (S-023).
  - Only components already installed are used, each integrity-checked before use (S-009, S-011, S-022).
- Acceptance criteria:
  - Scope criteria 1 to 14 for the fact sheet, banned terms, script and Part E, as set by product-owner (S-002, S-004, S-005).
  - Scope criteria 15 to 18 and 38 to 40 for recording admission and the ledger (S-003, S-020, S-025).
  - Scope criteria 19 to 27 and 37 for pre-checks, composition, captions, timing and overlay (S-006, S-007).
  - Scope criteria 28 to 33 for the package, checklist and approval (S-008, S-016).
  - Scope criteria 34 to 36 for the network, component and media-file evidence (S-012, S-014, S-017).

## Current-State Assumptions and Constraints

### 4.1 Facts

| ID | Fact | Established by |
|---|---|---|
| F-001 | The solution holds src projects Application, Capability, Credentials, Deterministic, Domain, Host, Persistence, Production and test projects Architecture.Tests, Capability.Tests, Deterministic.Tests, Domain.Tests, GraphicsStandIn, Persistence.Tests, Production.Tests, VoiceStandIn | MediaCompany.slnx |
| F-002 | MediaCompany.Deterministic references only MediaCompany.Domain and MediaCompany.Application and no package | src/MediaCompany.Deterministic/MediaCompany.Deterministic.csproj |
| F-003 | FontCoverage reads a face's character map (formats 4 and 12) and name table from bytes and lists the uncovered characters of labelled texts, using only the standard library | src/MediaCompany.Deterministic/Production/FontCoverage.cs |
| F-004 | MediaCompany.Production's ExternalMediaTool implements the voice-model port by running the in-house text-to-speech model, and the same assembly holds that model's installation verifier | src/MediaCompany.Production/ExternalMediaTool.cs, InstallationVerifier.cs |
| F-005 | ExternalMediaTool is the one process starter: argument lists, a cleared environment, a job that ends every member when closed, configured bounds; it draws text through the media tool's text filter from a text file with expansion off and a staged face file | src/MediaCompany.Production/ExternalMediaTool.cs, ExternalMediaTool.Graphics.cs |
| F-006 | ProductionSettings names RendererPath "the renderer executable (the external media tool)" and ProbePath, refuses an output root inside the repository, gives no member a default in code, holds no manifest for the media tool, and the tool's version line is read by VersionLineAsync | src/MediaCompany.Production/ProductionSettings.cs, ExternalMediaTool.cs |
| F-007 | The delivered preview encodes at the configured frame rate and width by height, in the yuv420p pixel format with the fast-start flag, through RendererPath | src/MediaCompany.Production/ExternalMediaTool.Graphics.cs (RenderPreviewAsync) |
| F-008 | GraphicsSettings holds installed manifests for the renderer, browser, font and Node sets, which RendererInstallationVerifier checks file by file inside MediaCompany.Production | src/MediaCompany.Production/ProductionSettings.cs, RendererInstallationVerifier.cs |
| F-009 | graphics/render.mjs has a bundle mode and a render mode, confines module resolution to the installed renderer folder, writes the bundle's module list, selects one composition by a fixed identifier, renders frames to image files and refuses downloads | graphics/render.mjs |
| F-010 | The graphic provenance record hashes the bundle "as served" and is keyed to the company item and item version | src/MediaCompany.Domain/Production/Graphics.cs |
| F-011 | The installed Remotion 4.0.534 set holds the compositor package that OffthreadVideo needs, listed in the renderer manifest; a render with OffthreadVideo under render.mjs is not measured | pilot-affiliate-vn/mc-16/02-orchestrator-measurements.md, M2 |
| F-012 | The installed renderer folder holds the speech-recognition integration package and the speech-synthesis integration package, never loaded by the delivered drawing program | pilot-affiliate-vn/mc-16/02-orchestrator-measurements.md, M3 |
| F-013 | The installed media tool's silence detection at a -35 dB noise floor and 0.35 s minimum found a 0.6 s gap in a generated file (start 1.499909 s, end 2.100091 s) | pilot-affiliate-vn/mc-16/02-orchestrator-measurements.md, M4 |
| F-014 | The installed media tool holds zoom-pan, text-drawing, subtitle and styled-subtitle filters | pilot-affiliate-vn/mc-16/02-orchestrator-measurements.md, M5 |
| F-015 | Inter 4.1 Regular, Italic, SemiBold and Bold miss 0 of 204 Vietnamese code points; correct mark stacking needs a rendered frame | pilot-affiliate-vn/mc-16/01-rk-016-glyph-measurement.md |
| F-016 | BarredTermScreen holds a closed child-directed term set over title, description and tags, matched whole-word and case-insensitive under a timeout, with no exemption list | src/MediaCompany.Deterministic/BarredTermScreen.cs |
| F-017 | The delivered publication approval is bound to a company item version, and the graphics approval file is hashed against its configured value, parsed from the same buffer and read in the company store only | src/MediaCompany.Domain/Publication/Approval.cs, src/MediaCompany.Production/GraphicsPlan.cs |
| F-018 | PublishReadyPredicate refuses a stage recorded held, naming the stage and its summary, over company production records | src/MediaCompany.Deterministic/Production/PublishReadyPredicate.cs |
| F-019 | AuditChain hashes each company audit entry (actor, action, subject, cost reference, retention class) with the previous entry's hash | src/MediaCompany.Deterministic/Record/AuditChain.cs |
| F-020 | MediaCompany.Architecture.Tests references MediaCompany.Host and checks networking-assembly references by name | tests/MediaCompany.Architecture.Tests/BoundaryTests.cs and its project file |
| F-021 | MediaCompany.Persistence.Tests runs against the company store; MediaCompany.Deterministic.Tests references only Deterministic and Domain | tests/ project files and DatastoreCollection.cs |
| F-022 | Central package versions are Npgsql, the Microsoft extensions, the test SDK 17.10.0, xunit 2.9.0 and its runner 2.8.2 | Directory.Packages.props |
| F-023 | MediaCompany.Application declares the voice-model port as an interface | src/MediaCompany.Application/Ports/ProductionPorts.cs |
| F-024 | Kit 03 Part A holds the header fields and rows F1 to F3 with source section and screenshot file; Part B row 2 reads "Đây là [tên sản phẩm], [phân loại]."; row 6 is a guidance sentence naming price | pilot-affiliate-vn/pilot-kit/03-mau-kich-ban-nguoi-dan-ai.md |
| F-025 | Kit 04 holds items A1 to A8, B1 to B13, C1 to C4 and D1 to D2; B3 covers use and absolute words, B5 price and other platforms, B8 the overlay throughout | pilot-affiliate-vn/pilot-kit/04-danh-sach-duyet-dang-bai.md |
| F-026 | The installed, manifest-verified set is Remotion with a headless Chrome shell, Inter 4.1 with manifests, ffmpeg and ffprobe through WinGet, Node 24 and .NET 10 | feature request, "What the company already holds" (S-009) |
| F-027 | HeldFiles keeps read-share handles so the hashed bytes are the bytes read, inside MediaCompany.Production | src/MediaCompany.Production/HeldFiles.cs |
| F-028 | MediaCompany.Host references Domain, Application, Deterministic, Credentials, Capability, Persistence and Production, and Capability builds an HTTP client | src/MediaCompany.Host/MediaCompany.Host.csproj, src/MediaCompany.Capability/CapabilityGatewayFactory.cs |

### 4.2 Assumptions

| ID | Assumption | Why needed | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | Serving the render bundle and driving the browser (F-009, F-010) open loopback sockets that a harness failing on any socket opening counts | Decides whether O-001 and O-003 meet C-014 | They are not eliminated by C-014, yet still rank below O-002 on impact surface | qa |
| A-002 | "The delivered renderer" of scope decision 1 includes the installed media tool the production path configures as its renderer (F-005, F-006) | O-002 meets C-017 only under it | O-002 is eliminated and no option remains (R-001) | product-owner |
| A-003 | After NFC every Vietnamese letter drawn is one precomposed code point, so the media tool's text filter draws it from the character map with no mark positioning | C-025 for O-002 rests on it | Marks mis-stack; the fallback of Q-005 applies (R-003) | qa |
| A-004 | The media tool opens no socket when every input and output is a local file and its protocol allow-list holds only the file protocol | C-014 for O-002 rests on it | No option meets C-014 (R-002) | qa |
| A-005 | The silence threshold of F-013 separates sentence pauses in the operator's real room | Default pause parameters of D-008 | More runs need manual timing (R-011) | qa |
| A-006 | The voice-model port interface (F-023), with no implementation in the toolchain's closure, is not a text-to-speech component under C-015 | Lets M-001 reference MediaCompany.Deterministic for FontCoverage | FontCoverage cannot be reused as-is (R-005) | qa |
| A-007 | The presenter clip is made from the whole recording, starts with it and differs in length by at most the configured tolerance | The clip is placed from time zero | Lip movement and captions drift (R-014) | product-owner |
| A-008 | Kit 03 Part B row 6's guidance sentence was used in no published video | No coexistence period for M-004 | A coexistence note is added to M-004 | product-owner |
| A-009 | Lexical class (f) detection covers only "so với" and "hơn hẳn"; naming another shop or brand as better stays with the CEO's Part E review | Bounds D-004 | A held brand list is needed (R-017) | product-owner |
| A-010 | A configured per-line character maximum, measured on the P-003 frame, keeps each caption sentence within two lines | Caption rule of D-007 | Sentences are refused or overflow (R-015) | qa |
| A-011 | The operator runs the toolchain on the machine holding the installed set, with a pilot folder outside the repository | Ledger and package location of D-003 | The location setting changes | orchestrator |

### 4.3 Constraints

| ID | Class | Constraint | Hard or negotiable | Source |
|---|---|---|---|---|
| C-001 | functional | A fact sheet is accepted only with the eleven header fields, rows carrying subject noun, attribute, value as written, source section and screenshot name, and each screenshot present in the input set | hard | Scope item 1, criteria 1 to 4 (S-002) |
| C-002 | functional | Classes (a) to (g) are refused whole-word, case-insensitive, after NFC, naming row and term; "nhất định", "giá đỡ điện thoại", "hơn" and "1,5 lít" pass and "nhất" is refused | hard | Scope item 2, criteria 5 to 7, decisions 2 and 16 (S-004, S-018) |
| C-003 | functional | The script is deterministic, in Part B order, with byte-exact disclosures, one to three fact sentences from fixed frames with the value unchanged, and one Part E line per sentence | hard | Scope items 3 and 4, criteria 8 to 12 and 14 (S-005) |
| C-004 | functional | The call to action comes from a fixed no-price list held in kit 03 Part B row 6, each entry passing the banned-term check | hard | Scope criterion 13, decision 8 (S-005, S-026) |
| C-005 | functional | One recording file per video, at most 60.0 s, refused at 300 s or more, refused when its SHA-256 equals another video's | hard | Scope item 5, criteria 15 to 18, decisions 4, 7 and 9 (S-003, S-013, S-020) |
| C-006 | functional | Composition starts only after the vertical-output and character-coverage checks pass, and the run record lists each drawn character with its result | hard | Scope item 6, criteria 19 and 20, decision 6 (S-007) |
| C-007 | functional | Output is 1080x1920 at 9:16; each image moves by a slow linear pan or zoom; the presenter is in every 0.5 s sample; a non-portrait clip is refused | hard | Scope item 7, criteria 21 to 24, decisions 14 and 15 (S-006) |
| C-008 | functional | Captions equal the approved script's sentences, one per sentence, in windows from pause detection or a valid manual timing file; white on a dark band in the lower third, at most two lines, installed face | hard | Scope item 8, criteria 25, 26 and 37, decisions 10, 11 and 13 (S-006) |
| C-009 | functional | The overlay "Video tạo bởi AI · Có liên kết tiếp thị" is in every frame | hard | Scope item 9, criterion 27 (S-006) |
| C-010 | functional | The package holds the video, its SHA-256, a provenance entry for every sentence and digests of the six inputs | hard | Scope item 10, criteria 28 to 30 (S-008) |
| C-011 | functional | The checklist pre-fill ticks exactly B3, B5 and B8 with results and leaves the other 24 items unticked | hard | Scope item 11, criteria 31 and 32, decision 5 (S-008) |
| C-012 | security | An approval holds only when its SHA-256 equals the video's recomputed hash and is void after a one-byte change; nothing publishes | hard | Scope item 12, criterion 33 (S-008, S-016) |
| C-013 | functional | The ledger refuses a thirteenth video, a third video of one product, and a second video repeating the first one's focus | hard | Scope item 14, criteria 38 to 40, decision 12 (S-025) |
| C-014 | security | The sample run makes zero network connection attempts under a harness that fails on any socket opening | hard | Scope criterion 34 (S-010, S-011, S-014) |
| C-015 | compliance | The dependency list and the invoked-tool log hold no language-model, speech-recognition or text-to-speech component | hard | Scope criterion 35, exclusions 4 and 5 (S-012, S-013) |
| C-016 | compliance | No media file is added to the repository; the sample is generated at test time | hard | Scope criterion 36 (S-017) |
| C-017 | structural | The delivered renderer, coverage check, approval by hash and held-stage refusal are reused; no second renderer or approval path | hard | Scope decision 1 (S-009) |
| C-018 | structural | The company store and frozen Wave 12 and Wave 13 material are untouched | hard | Scope exclusion 7 (S-015, S-025) |
| C-019 | operability | Only already-installed components are used; nothing is downloaded and no package is added | hard | Feature request hard constraint 2 and T-001 acceptance (S-011, S-022) |
| C-020 | operability | The toolchain costs nothing to run and makes no metered call | hard | Feature request hard constraint 5 (S-014) |
| C-021 | operability | The .NET test run needs neither the company store database nor a container service | hard | Execution plan (S-023) |
| C-022 | security | Operator-named files resolve only inside the supplied input set | hard | Scope criterion 2 (S-002) |
| C-023 | quality-attribute | Every installed executable and face the toolchain runs or reads is integrity-checked before use | negotiable | Feature request reuse statement (S-009) |
| C-024 | quality-attribute | The RK-016 part 2 proof costs one rendered vertical frame with stacked marks | negotiable | Execution plan T-006 (S-024) |
| C-025 | quality-attribute | Stacked Vietnamese marks draw correctly in captions and overlay | negotiable | Feature request scope 3 (S-007, S-024) |
| C-026 | functional | The toolchain performs no account, sign-in, contact, upload or publication action | hard | Feature request hard constraints 1 and 7 (S-010, S-016) |
| C-027 | functional | The design records one decision for each of the eleven deferred items | hard | Execution plan T-001 (S-021) |

## Architecture and Component Design

### 5.1 Impacted Modules

| ID | Module | Impact type | Basis | Interfaces affected | Confidence |
|---|---|---|---|---|---|
| M-001 | MediaCompany.Pilot (new console project under src/) | extension | F-001, F-002, F-003, F-005 | Five operator verbs; fact sheet, timing file, script, package, approval record and ledger forms; settings file | confirmed |
| M-002 | MediaCompany.Pilot.Tests (new test project under tests/) | extension | F-021, F-022 | Unit and Integration test categories | confirmed |
| M-003 | MediaCompany.slnx | extension | F-001 | Solution project list gains M-001 and M-002 | confirmed |
| M-004 | Kit file 03, pilot-affiliate-vn/pilot-kit/03-mau-kich-ban-nguoi-dan-ai.md | contract-change | F-024 | Part B row 6 becomes the fixed call-to-action list; Part F voice wording | confirmed |
| M-005 | Kit file 04, pilot-affiliate-vn/pilot-kit/04-danh-sach-duyet-dang-bai.md | behavior-change | F-025 | Item B13 wording; item identifier column read by an M-002 consistency test | confirmed |
| M-006 | MediaCompany.Deterministic | no-change-verified | F-002, F-003 | FontCoverage consumed unchanged | confirmed |
| M-007 | MediaCompany.Production | no-change-verified | F-004, F-005 | None; not referenced by M-001 | confirmed |
| M-008 | MediaCompany.Persistence and the company store | no-change-verified | F-017, F-021 | None | confirmed |
| M-009 | graphics/render.mjs | no-change-verified | F-009 | None; not started by M-001 | confirmed |
| M-010 | graphics/drawing | no-change-verified | F-009 | None | confirmed |
| M-011 | MediaCompany.Architecture.Tests | no-change-verified | F-020 | None; the toolchain's boundary checks live in M-002 | confirmed |
| M-012 | Directory.Packages.props | no-change-verified | F-022 | None; no package added | confirmed |
| M-013 | Installed media tool (ffmpeg and ffprobe through WinGet) | no-change-verified | F-005, F-013, F-014 | Invoked unchanged by M-001 and M-002 | confirmed |
| M-014 | Installed renderer folder (Remotion set, headless Chrome shell, Node 24) | no-change-verified | F-011, F-012 | None; never started | confirmed |
| M-015 | MediaCompany.Application | no-change-verified | F-023, A-006 | None; in M-001's closure through M-006 | confirmed |
| M-016 | MediaCompany.Host | no-change-verified | F-028 | None; M-001 is a separate entry point | confirmed |

Boundary crossings in the selected approach: M-001 to M-013 (process start, file-only protocol); M-001 to the operator's input folder (read, contained); M-001 to the pilot folder outside the repository (package and ledger writes); M-002 to the repository's kit files M-004 and M-005 (read only).

### 5.2 Options Considered

| Option | Structural change | Hard constraints (C-014, C-015, C-017, C-018, C-019) | Impact surface | Reuse leverage | Quality attributes (C-023, C-024, C-025) | Migration burden | Operability | Outcome |
|---|---|---|---|---|---|---|---|---|
| O-001 | New M-001; new 9:16 composition beside graphics/drawing; render.mjs gains a composition choice and media inputs; presenter by OffthreadVideo; media tool encodes frames and adds the recording | C-014 violated (A-001); others met | 2 (M-004, M-009) | 14 | 2 of 3: C-024 unmet, the proof needs a full composition render with OffthreadVideo unmeasured (F-011) | 2 | low: Node, browser and media tool per run, about 1,800 browser frames per video | eliminated (C-014) |
| O-002 | New M-001; the installed media tool composes images with linear zoom or pan, the presenter overlay, drawn captions and overlay, and the recording as audio; no Node or browser | all met (C-017 under A-002, C-014 under A-004) | 1 (M-004) | 14 | 3 of 3: C-023 by configured digests, C-024 one drawn frame, C-025 under A-003 | 1 | high: one .NET process and media tool children, no socket | selected |
| O-003 | New M-001; render.mjs and a new composition draw caption and overlay plates as transparent images; the media tool composes images, presenter, plates and audio | C-014 violated (A-001); others met | 2 (M-004, M-009) | 14 | 2 of 3: C-024 unmet, the proof needs a browser render | 2 | medium: Node and browser run once per caption set | eliminated (C-014) |
| O-004 | Python tool under tools/ that starts the media tool and reads the face with its own reader | C-017 violated: the .NET coverage check FontCoverage cannot be reused; others met | 1 (M-004) | 12 | 2 of 3: C-023 unmet, the interpreter is under no manifest | 1 | medium: a second runtime to operate | eliminated (C-017) |
| O-005 | Pilot mode inside MediaCompany.Production with a Host command, reusing ExternalMediaTool and the delivered approval and provenance types | C-015 violated (F-004) and C-018 violated (F-017); others met | 3 (M-004, M-007, M-016) | 18 | 3 of 3 | 3 | low: company host configuration and store connection required | eliminated (C-015) |

Evaluation order (reasoning stage A8): hard constraints, then smaller impact surface, then higher reuse leverage, then more quality attributes met, then smaller migration burden, then operability. Reuse leverage counts the capabilities of section 7 met by reuse-as-is or reuse-extended under each option.

### 5.3 Selected Approach

- Selected: O-002, the only option meeting every hard constraint (C-014, C-015, C-017, C-018).
- Structural change: a new console project M-001 referencing only M-006, a new test project M-002, two solution entries in M-003, and the call-to-action list in M-004. M-001 holds these components: fact sheet reader and checker; banned-term check over held data; script generator with Part E; recording admission; pause detector with manual timing reader; pre-composition checks; composition plan; media tool invoker; package assembler with checklist pre-fill; approval by hash; pilot ledger; run guard. No existing module changes contract or dependencies.
- Rationale: O-002 opens no socket and starts no script runtime, so C-014 and C-015 are met by construction rather than by inspecting a bundle's module list; it reuses the media tool the delivered path already configures as its renderer (F-005, F-006, F-007), FontCoverage (F-003) and the staged-face text drawing (F-005); and its RK-016 part 2 proof is one drawn frame (C-024).
- Highest-scoring rejected alternative and why it lost: O-004 ranks second on the remaining criteria (impact surface 1, reuse 12) but is eliminated by C-017, because a Python tool cannot reuse the .NET coverage check. O-003 is the named fallback if the P-003 frame fails A-003: browser-drawn text plates composed by the media tool; it needs the CEO to accept loopback sockets under C-014 (Q-005).
- Tradeoffs accepted:
  - Text is drawn by the media tool's text filter without browser text shaping; correctness rests on NFC precomposed letters (A-003) and is proved by one frame (P-003).
  - The media tool has no installed manifest (F-006); integrity rests on configured executable digests and a recorded version line (D-013, Q-004).
  - The delivered approval, provenance, held-stage and process-start types are not referenced; their rules are carried into M-001 (section 7).
  - Module-list confinement of render.mjs is not exercised; the closure check and the invoked-tool log replace it as the proof for C-015.

### 5.4 Decisions

| ID | Decision | Architecture-significant | Record |
|---|---|---|---|
| D-001 | M-001 MediaCompany.Pilot is a new console project referencing only MediaCompany.Deterministic; it may not reference Production, Persistence, Capability, Credentials, Host or a networking assembly; tests live in M-002 (O-002) | yes | architecture-decision-record-D-001.md |
| D-002 | Vertical render route (item 5): the installed media tool composes the 1080x1920 video at the configured frame rate with the delivered encoder settings (F-007); layout, top to bottom: overlay band, image panel with a portrait presenter inset, caption band in the lower third; recording as the audio track; video length equals recording length; OffthreadVideo is not used; fallback O-003 under Q-005 (O-002) | yes | architecture-decision-record-D-002.md |
| D-003 | Ledger location (item 9): one append-only ledger file in the configured pilot folder outside the repository and the company store; entries chained by SHA-256, previous generation kept, atomic replace; each package holds a copy of its entry (O-002) | yes | architecture-decision-record-D-003.md |
| D-004 | Banned classes held (item 4): one versioned data resource in M-001 holds classes (a) to (g) exactly as scope criterion 5 lists them, the exemption list (first entry "nhất định") and the money pattern (a number followed by "đ", "đồng", "VND" or a "k" suffix); matching runs on syllable sequences after NFC and invariant lower-casing; one check serves rows, overrides, sentences and the call to action (O-002) | no | inline |
| D-005 | Network guard (item 10) and no-generative proof: static closure check in M-002, in-process socket telemetry listener in M-001, file-only protocol allow-list on every media tool start, job-scoped socket-table sampling in the harness, dependency-list and invoked-tool-log checks (O-002) | yes | architecture-decision-record-D-005.md |
| D-006 | Fact sheet form (item 1): one UTF-8 JSON document per video holding the eleven header fields of scope criterion 3, one to three rows (label F1 to F3, subject noun, attribute, value as written, source section, screenshot file name, optional override sentence) and an optional call-to-action choice; unknown members are refused (O-002) | no | inline |
| D-007 | Frames (item 2): introduction "Đây là [subject noun], [classification]."; fact "[Subject noun] có [attribute] [value]." or the row's override, which must contain the value unchanged and pass the check; disclosures verbatim. Call-to-action list (item 3): entry 1 "Bấm vào giỏ hàng hoặc link sản phẩm để xem chi tiết.", entry 2 "Thông tin chi tiết, bạn xem trong thẻ sản phẩm của video."; entry 1 by default (O-002) | no | inline |
| D-008 | Pause method and threshold (item 7): media tool silence detection at a -35 dB noise floor and 0.35 s minimum, both configured (F-013); silences touching the start or end are ignored; the interior count must equal sentences minus one, else the run stops and asks for a manual timing file (O-002) | no | inline |
| D-009 | Image motion (item 8): images in header order, each for an equal consecutive share of the duration, fitted inside the image panel, slow linear zoom-in to a configured maximum (proposed 108 percent) with linear pan as the configured alternative, hard cuts, no other transition (O-002) | no | inline |
| D-010 | Coverage mechanism (item 6): FontCoverage runs over every distinct code point of the NFC caption sentences and the overlay against the configured face, whose SHA-256 must equal its line in the installed font manifest; an uncovered or combining character stops the run before composition (O-002) | no | inline |
| D-011 | Test placement (item 11): M-002 holds a Unit category that starts no process and an Integration category that needs the installed media tool, located by settings, with fixtures generated at test time in a temporary folder outside the repository (O-002) | no | inline |
| D-012 | Approval and held status: the approval record names the video code and the SHA-256 the CEO reviewed; approval is refused for a held package or a hash mismatch; verification recomputes the hash and reports void on any difference (O-002) | no | inline |
| D-013 | Tool and face identity: configured SHA-256 values of the media tool, the probe and the face are checked before each run, and the media tool's version line is recorded in the run record (O-002) | no | inline |

## API and Data Model Impact

- API changes: M-001 adds five operator verbs: check a fact sheet, write the script, produce the package, record an approval, verify an approval. Each verb exits with a distinct status for refused input and for tool failure, and every refusal names the row, field, term, sentence or check. No existing API changes.
- Contract compatibility notes:
  - M-004 transition strategy. Current shape: Part B row 6 holds one guidance sentence naming price. Target shape: row 6 holds the two-entry call-to-action list of D-007, marked as the list the tool uses. Compatibility approach: the same change updates M-004 and M-001's held list, and an M-002 unit test fails when they differ; a script carrying the former sentence is refused as unmapped (scope criterion 6). Coexistence period: none (A-008). Retirement condition: the former sentence leaves row 6 when the list lands. Rollback position: revert M-004 and M-001's held list together; the test keeps them equal in either direction.
  - M-005: item B13 wording changes; the identifier column stays the same and is read only by the M-002 consistency test, so no consumer breaks.
  - New forms in M-001 (fact sheet, timing file, package manifest, approval record, ledger entry) each carry a form version member, so a later change is detected rather than misread.
- Schema or migration changes:
  - None in the company store (C-018), and no data migration.
  - The new file forms have no prior data. Migration direction is not applicable, and reversibility is total: removing M-001 leaves no reader or writer behind. The ledger file is created on first use. During any later form change, a reader refuses a form version it does not know.
- Data model of the new forms (description, not a schema):
  - Timing file: UTF-8, one line per script sentence giving sentence number, start seconds and end seconds; accepted only with one line per sentence, in order, ascending, not overlapping, inside the recording's duration (scope criterion 37).
  - Package: one folder per video code under the pilot folder, holding the video; a manifest with the video SHA-256, the six input digest groups, tool and face identity, every check result and status ready or held; the provenance list; the script and Part E; caption windows; the run record (drawn characters with coverage results, invoked-tool log, socket count); the checklist pre-fill; and a copy of the ledger entry.
  - Ledger entry: video code, product page path, product video number, focus text after NFC, recording SHA-256, video SHA-256, package folder, time written, previous entry hash, entry hash. A re-run for the same video code appends a superseding entry; the freshness check compares only against other video codes.
  - Approval record: video code, the SHA-256 the CEO reviewed, the approver's name as typed, and the date.

## Reusable Components and Reuse Rationale

| Capability | Candidate | Outcome | Rationale |
|---|---|---|---|
| Parse and write the fact sheet, timing file, manifest, ledger and approval forms | .NET standard library JSON reader and writer | reuse-as-is | Native platform capability; no package added (C-019) |
| NFC normalization and case folding | .NET standard library normalization and invariant lower-casing | reuse-as-is | Native platform capability, deterministic across runs |
| Banned-term check | BarredTermScreen (F-016) | rejected | Closed child-directed set over title, description and tags with no exemption list; cannot give the results C-002 requires; its whole-word, case-insensitive, time-bounded rule is carried |
| Script from fixed frames with Part E lines | none | none-found | Searched existing components (Deterministic, Domain, Production), the .NET standard library, platform capability and installed dependencies; no sentence-frame generator exists |
| Probe duration, stream size and orientation | Installed probe executable (F-005, F-026) | reuse-as-is | Already used by the delivered path; unchanged |
| Content digests | .NET standard library SHA-256 | reuse-as-is | Native platform capability |
| Pause detection | Installed media tool silence detection (F-013) | reuse-as-is | Measured on this machine; signal level only, no recognition |
| Manual timing file validation | none | none-found | Searched existing components, the .NET standard library, platform capability and installed dependencies; no timing-file reader exists |
| Character coverage | FontCoverage (F-003) | reuse-as-is | Pure function in MediaCompany.Deterministic; scope decision 1 names it |
| Installed face integrity | RendererInstallationVerifier (F-008) | rejected | Lives in the assembly that implements the in-house speech model (F-004) and verifies the renderer, browser and Node sets O-002 never runs; the face's digest is compared with its font-manifest line instead |
| Process start with argument list, bound and tree kill | ExternalMediaTool process starter (F-005) | rejected | Same assembly as the in-house speech model (F-004), so referencing it breaks C-015; its rules (argument list, cleared environment, job ending every member, bound) are carried |
| Process start | .NET standard library process API and the platform job object (F-005) | reuse-as-is | The primitives the delivered starter is built on |
| Vertical composition and encoding | Installed media tool configured as the delivered renderer, with the delivered encoder settings (F-006, F-007) | reuse-as-is | The delivered path already renders through it; only the frame size and inputs differ |
| Image motion | Installed media tool zoom-pan and scale-crop filters (F-014) | reuse-as-is | Present in the installed build |
| Caption and overlay drawing | Installed media tool text filter with text from file, expansion off, staged face (F-005, F-014) | reuse-as-is | The delivered drawing pattern; operator text never enters a filter description |
| Browser-rendered composition | graphics/render.mjs and graphics/drawing (F-009, F-010) | rejected | Serving the bundle and driving the browser open loopback sockets (A-001), breaking C-014; a second composition would also change render.mjs's contract |
| Approval by hash | Delivered publication approval and graphics approval file (F-017) | rejected | Both are read in the company store (C-018), and the graphics one lives in the speech-model assembly (F-004); the rule (named SHA-256, recomputed at check, void on any byte change, parsed from the hashed bytes) is carried |
| Held-stage refusal | PublishReadyPredicate held-stage outcome (F-018) | rejected | Evaluates company production records; the rule (a held package is never ready and its approval is refused naming the failing check) is carried |
| On-screen provenance | Graphic and narration provenance records (F-010) | rejected | Keyed to the company item and version in the company store (C-018); the pilot list keys each sentence to fact row, source section and screenshot |
| Ledger storage | Company store through MediaCompany.Persistence (F-021) | rejected | Why rejected: the company store is frozen and must not be touched (C-018), and the pilot ledger is not part of the company's audited production records, so writing to the store would mix pilot entries into them. What replaces it: the file-based, SHA-256-chained, append-only ledger of D-003, held in the configured pilot folder outside the repository and the store, with the previous generation kept and a copy of each entry in its package. What it costs: no database, schema or container, so the ledger works offline and the test run stays free of the store (C-021) |
| Ledger tamper evidence | AuditChain (F-019) | rejected | Its entry shape is the company audit record (actor, cost reference, retention class), while the ledger needs typed product, focus and digest fields; the chaining rule is carried with standard SHA-256 |
| Hashed bytes equal the bytes used | HeldFiles (F-027) | rejected | Lives in the speech-model assembly (F-004); M-001 reads each input once and hashes and uses that one buffer |
| Static networking-reference check | Architecture.Tests networking check (F-020) | rejected | Private to a test class whose project carries the company host closure (F-028); the same assembly list is applied in M-002 to M-001's closure |
| Runtime socket detection | .NET platform socket and name-resolution telemetry event sources, and the platform TCP and UDP tables | reuse-as-is | Native platform capability; no package added |
| Test framework | xunit and the test SDK at central versions (F-022) | reuse-as-is | Already pinned centrally |
| Generated fixtures | Installed media tool synthetic tone, silence and colour sources with text drawing (F-013, F-014) | reuse-as-is | The measurement of F-013 used a generated file; nothing is downloaded (C-019) |
| Path containment of operator names and output folders | .NET standard library path API, following the delivered output-root rule (F-006) | reuse-as-is | Native platform capability |
| Call-to-action list and checklist identifiers kept equal to the kit | none | none-found | Searched existing components, the .NET standard library, platform capability and installed dependencies; the kit files are prose tables with no reader |

## Operational Considerations

- Logging and observability updates: every produce run writes a run record into its package (M-001): each media tool invocation with executable path, SHA-256 and arguments; the media tool's version line (D-013); every drawn character with its coverage result (C-006); detected pauses and caption windows; the socket telemetry count (D-005); and every check result. Nothing is written to the company store (C-018).
- Error handling strategy: each refusal names the row, field, term, sentence, file or check (C-001, C-002, C-005); a failed pre-check stops before any composition output exists (C-006); a package with a failed check is marked held and cannot take an approval (D-012); a media tool failure or bound overrun ends the job and every member, following the delivered rule (F-005). Exit statuses separate refused input from tool failure.
- Security considerations:
  - Operator-named images, screenshots, recording, clip and timing file must be plain file names resolved inside the input folder; rooted or climbing names are refused (C-022, R-006).
  - Operator text reaches the media tool only through UTF-8 text files drawn with expansion off; filter descriptions are built from configured numbers and staged file names only, and control characters are refused (R-007).
  - Approval binds to the recomputed SHA-256 (C-012, R-008); the ledger is chained and verified before each read (D-003, R-012).
  - No network (C-014, D-005); no account, sign-in or publication action (C-026); configured digests guard the executables and face (C-023, D-013).
  - Compliance: no recognition or synthesis component is in M-001's closure or started by it, and the installed renderer folder holding such packages (F-012) is never named in M-001's settings (C-015).
- Performance considerations: no performance target is stated (S-014 sets only cost). Each run is bounded by configured process bounds following the delivered pattern (F-006), and the frame count follows the recording length, at most 60.0 s (C-005).
- Deployment or operability impact: M-001 runs from the repository build on the operator's machine (A-011), with a settings file outside the repository naming the media tool, probe, face, font manifest, pilot folder, thresholds, layout, frame rate, bounds and duration tolerance, none with a default in code (F-006). No container, database or service is needed (C-021). The S06 database area changes nothing: the ledger is a file (D-003).

## Delivery Plan

### Sequencing Constraints

| ID | Constraint | Modules | Prerequisites | Reason | Binds |
|---|---|---|---|---|---|
| P-001 | The Design Gate decision on D-001, D-002, D-003 and D-005 and the answer to Q-001 precede any module; M-001 then exists with its reference rule and the M-002 closure check before any component is added | M-001, M-002, M-003 | none | Verification of a boundary precedes work that assumes it holds | T-001, T-020, T-022 |
| P-002 | The media tool invocation path (argument list, configured digest check, file-only protocol allow-list, bound, job ending every member, invoked-tool log) exists before any component starts the media tool | M-001, M-013 | P-001 | Contract definition precedes every consumer | T-010, T-011 |
| P-003 | One 1080x1920 frame with stacked Vietnamese marks and the overlay, drawn through P-002's path and the staged face, is produced and probed, and the caption per-line maximum is measured, before the vertical check and composition rely on them | M-001, M-002, M-013 | P-002 | The vertical check rests on the proven route | T-006, T-007 |
| P-004 | The held data (term classes, exemptions, money pattern, frames, call-to-action list, checklist identifiers) exist, with M-004 row 6 carrying the same list, before the fact sheet check and the generator read them | M-001, M-004, M-005 | P-001 | Contract definition precedes every consumer | T-002, T-003, T-004, T-019 |
| P-005 | The ledger form, its location rule and its safeguards (chain, previous generation, atomic replace) exist before admission or packaging reads or appends | M-001 | P-001 | A reversibility safeguard precedes the first irreversible ledger write | T-011, T-018 |
| P-006 | The pre-composition checks (vertical, coverage of each drawn character, caption lines, portrait clip, duration tolerance) gate composition | M-001, M-006 | P-003, P-004 | A gate precedes the step it gates | T-005, T-007, T-020 |
| P-007 | Package digests, provenance and ready or held status exist before an approval binds to the video hash and before the checklist pre-fill reads check results | M-001 | P-005, P-006 | Approval binds to an assembled package | T-008, T-009, T-021 |
| P-008 | The network guard (in-process listener, job-scoped socket tables, protocol allow-list check) and the dependency-list check exist before the end-to-end sample run whose evidence they certify | M-001, M-002 | P-002 | Verification of a boundary precedes evidence that assumes it | T-012 |
| P-009 | Pause detection and manual timing validation exist before caption windows are drawn | M-001, M-013 | P-002, P-004 | Windows derive from the sentence count and the admitted recording | T-005, T-010 |
| P-010 | Fixture producers write only to a temporary folder outside the repository, and the test matrix assigns each scope criterion to the Unit or Integration category, before integration tests consume fixtures | M-002, M-013 | P-002 | No media enters the repository (C-016) | T-015, T-017 |
| P-011 | Verification evidence (sample run record, dependency list, invoked-tool log, repository media scan) exists before the release note and the branch delivery | M-002 | P-007, P-008, P-010 | Release evidence precedes release | T-013, T-014, T-016 |
| P-012 | The operator guide cites the verbs and refusal names the sample run emits | M-004, M-005 | P-008 | The guide documents what the run emits | T-023 |

### Test Strategy Focus Areas

- Unit category (M-002, no process start): the eleven header fields and row fields blanked in turn; one term per class (a) to (g); the five strings of scope criterion 7; money patterns against "1,5 lít" and "220V"; frames and byte-exact disclosures; one, three and four fact rows; script hash stability; Part E row count; timing file cases of scope criterion 37; ledger limits on a temporary folder; approval after a one-byte change; checklist pre-fill of exactly B3, B5 and B8; equality of the held call-to-action list with M-004 row 6 and of the held identifiers with M-005.
- Boundary checks (M-002): M-001's closure holds only M-001, M-006, M-015, MediaCompany.Domain and the platform; none of the networking assemblies of F-020; no type implementing the voice-model port (A-006).
- Integration category (M-002, installed media tool): recordings of 60.0, 61.0, 299.9 and 300.0 s; two recording files; digest reuse naming the other video; tones with set pauses and a run-together variant; portrait and landscape stand-in clips; 1080x1920 probe; motion by frame difference; presenter and overlay presence at 0.5 s steps; caption windows against the timing; six digests recomputed.
- Network evidence (P-008): the sample run starts M-001 as a child in a job; the harness fails on any socket-table row owned by a job member and on a non-zero telemetry count; every invocation in the tool log carries the file-only allow-list.
- Repository evidence: a scan of tracked files for media extensions finds none added by this change (C-016).

### Rollout and Rollback

- Rollout: M-001 and M-002 land on the session branch with M-003, M-004 and M-005 in the same change; the operator configures the settings file and pilot folder outside the repository; the first real package is reviewed with the threshold of D-008 checked against it (A-005).
- Rollback: remove M-001, M-002 and their M-003 entries and revert M-004 and M-005 together; no other module changes, and the pilot folder's packages and ledger remain readable evidence outside the repository.

## Risks and Mitigations

| ID | Class | Trigger | Impact | Likelihood | Affects | Mitigation | Owner |
|---|---|---|---|---|---|---|---|
| R-001 | structural | product-owner reads "the delivered renderer" of scope decision 1 as the browser renderer only (A-002 false) | O-002 breaks C-017 while O-001 and O-003 break C-014; no option remains without a relaxation | low | D-002, P-001 | Q-001 answered before implementation, held by P-001 | product-owner |
| R-002 | security | The media tool opens a socket although every input is local and the allow-list holds only files (A-004 false) | C-014 fails for every option | low | D-005, P-008 | P-008 harness detects it on the sample run; escalate through orchestrator | qa |
| R-003 | contract | The P-003 frame shows a mis-stacked mark or a missing precomposed glyph (A-003 false) | Captions illegible; D-002 moves to the O-003 fallback, which needs Q-005 | low | D-002, P-003 | P-003 proves it before composition is built | qa |
| R-004 | contract | The kit 03 row 6 list and M-001's held list differ | A script carries a sentence the template does not hold; scope criterion 13 fails | medium | M-004, P-004 | M-002 consistency test reads the kit; both change in one change | dev-1-implement |
| R-005 | structural | The voice-model port in M-015 is counted as a text-to-speech component (A-006 false) | M-001 cannot reference M-006, so FontCoverage reuse is lost | low | D-001, M-015 | Q-006; the closure test proves no implementation of the port | qa |
| R-006 | security | An operator-named file is rooted or climbs out of the input folder | Reads outside the input set; foreign files enter the package | medium | M-001, P-001 | Plain names only, resolved and contained, refused naming the row | dev-1-implement |
| R-007 | security | Operator text reaches a media tool filter description | Filter injection or wrong drawing | medium | D-002, P-002 | Text only through UTF-8 files with expansion off; descriptions from configured numbers; control characters refused | dev-1-implement |
| R-008 | security | Video bytes change after an approval record is written | Unapproved bytes pass as approved | low | D-012, P-007 | Verification recomputes the SHA-256 at every check; a mismatch voids the approval | qa |
| R-009 | security | A re-encoded or trimmed copy of another video's recording is submitted | The digest check passes it (scope decision 4) | medium | D-003 | Accepted residual; the fresh-recording item B13 stays unticked for the CEO | product-owner |
| R-010 | contract | Whole-word matching refuses ordinary text such as "da" meaning leather, "4k" as a resolution, or another compound of "nhất" | Genuine facts refused | medium | D-004, P-004 | Exemption list in held data; Q-002; override sentence path | product-owner |
| R-011 | operability | The threshold misses or splits pauses in the operator's room (A-005 false) | Frequent manual timing | medium | D-008, P-009 | Threshold re-set from the first real recording; manual timing path | qa |
| R-012 | operability | The ledger file is lost, edited or corrupted | Limits and freshness go unenforced | low | D-003, P-005 | Chain verified before every read; previous generation kept; each package holds its entry | dev-1-implement |
| R-013 | operability | The media tool executable is replaced (no installed manifest, F-006) | An unverified tool renders | low | D-013, P-002 | Configured SHA-256 checked before each run; version line recorded; Q-004 | orchestrator |
| R-014 | delivery | Clip and recording lengths differ beyond tolerance or start apart (A-007 false) | Lip movement and captions drift | medium | D-002, P-006 | Duration-tolerance refusal; Q-003 | product-owner |
| R-015 | delivery | A sentence needs more than two caption lines at the configured size (A-010 false) | The run stops on a real product | medium | D-007, P-006 | Refusal names the sentence; P-003 measures the per-line maximum; override sentence | dev-1-implement |
| R-016 | security | Socket-table sampling misses a short-lived connection | C-014 passes falsely | low | D-005, P-008 | Layered guard: allow-list, in-process listener, static closure check | qa |
| R-017 | structural | A brand comparison beyond "so với" and "hơn hẳn" reaches a script (A-009) | A comparison claim reaches a video | low | D-004 | CEO's Part E tick and checklist review; Q-007 | product-owner |

## Estimate and Confidence

- Overall: L (confidence: low), because the estimate rests on unconfirmed assumptions A-002, A-003, A-004 and A-006.
- Breakdown:
  - M-001: L (confidence: low); twelve components, one new module boundary, media tool invocation and composition.
  - M-002: M (confidence: medium); two categories, generated fixtures, network harness.
  - M-003: XS (confidence: high).
  - M-004 and M-005: XS (confidence: high); wording only.
  - P-003 render proof: S (confidence: low) until the frame is drawn.
- Scope assumptions:
  - One product, one presenter, Vietnamese captions, at most 60.0 s per video (C-005).
  - Only the installed set is used (C-019).
  - The answers to Q-001 and Q-006 confirm A-002 and A-006.
- Uncertainty drivers:
  - Text-filter mark stacking (A-003) and the socket behaviour of the media tool (A-004).
  - Real-room pause detection (A-005) and real presenter clips (A-007).

## Open Decisions and Escalations

E-AUTHORITY events are recorded as Q-001, Q-002, Q-003, Q-005 and Q-007, each owned by its authority. No E-BOUNDARY event occurred.

| ID | Question | Blocking | Owner | Affects | Consequence |
|---|---|---|---|---|---|
| Q-001 | Does "the delivered renderer" of scope decision 1 cover the installed media tool the production path configures as its renderer (A-002)? | non-blocking | product-owner | D-002, P-001 | Yes: O-002 stands. No: every option breaks a hard constraint, and the CEO relaxes scope decision 1 or criterion 34 for loopback sockets |
| Q-002 | Which exemptions does the held list carry beyond "nhất định", for example "da" as leather or "4k" as a resolution? | non-blocking | product-owner | D-004, P-004 | Listed: the text passes. Not listed: refused, and the operator uses an override or drops the row |
| Q-003 | Is 0.5 s the tolerance between clip and recording length, with both starting together (A-007)? | non-blocking | product-owner | D-002, P-006 | Yes: as designed. Another value: configured. An offset: a configured start offset is added |
| Q-004 | Should the installed media tool get an installed manifest like the renderer, browser, font and Node sets, hashing files already installed? | non-blocking | orchestrator | D-013, P-002 | Yes: M-001 checks against the manifest. No: configured executable digests stand |
| Q-005 | If the P-003 frame fails (A-003 false), does the CEO accept the browser renderer's loopback sockets for O-003's text plates under criterion 34, or approve another component by name? This answers the plan's open question on a T-006 failure | non-blocking | orchestrator | D-002, P-003 | Accept: O-003 plates route. Refuse: composition waits for a named component |
| Q-006 | Is the voice-model port interface, unimplemented in M-001's closure, outside the text-to-speech component of criterion 35 (A-006)? | non-blocking | qa | D-001, M-015 | Yes: as designed. No: M-001 drops the reference to M-006, which is a design change |
| Q-007 | Does class (f) beyond "so với" and "hơn hẳn" stay with the CEO's Part E review (A-009)? | non-blocking | product-owner | D-004 | Yes: as designed. No: a held brand list is added |
| Q-008 | Design Gate acceptance of D-001, D-002, D-003 and D-005, all at Proposed; tech-lead accepts under the producer exclusion rule | non-blocking | tech-lead | D-001, D-002, D-003, D-005 | Accepted: implementation starts at P-001. Not accepted: the design returns to the architect |

## Sign-off

- Architect: unsigned; producer of this package and its records, so excluded from accepting under the Producer Exclusion Rule
- Tech Lead: unsigned; accepting Design Gate owner for this package and for D-001, D-002, D-003 and D-005
- Product Owner: unsigned; product confirmation of Q-001, Q-002, Q-003 and Q-007
- QA: unsigned; review of the test focus areas and of Q-006

## Appendix A: Statement Register

| ID | Statement | Source |
|---|---|---|
| S-001 | One product becomes one ready-to-review 9:16 video, reusing the delivered deterministic pipeline | Feature request, Why |
| S-002 | The fact sheet is Part A of kit 03 in machine-readable form, each fact with its page section and screenshot file | Feature request, input 1 |
| S-003 | Per video, the operator supplies product image files, an own-voice Vietnamese recording under 5 minutes made fresh, and the presenter clip | Feature request, inputs 2 to 4 |
| S-004 | A fact or field carrying banned content is rejected, naming the row and the banned term | Feature request, likely scope 1 |
| S-005 | The script generator fills Part B deterministically with no language model, places the disclosure verbatim at start and end, and emits Part E | Feature request, likely scope 2 |
| S-006 | The 9:16 composition shows images with motion, the presenter clip, burned-in captions from the recorded script and the overlay for the whole video | Feature request, likely scope 3 |
| S-007 | First, the RK-016 check: vertical output from the delivered renderer, and coverage by the installed face of every glyph drawn | Feature request, likely scope 3 |
| S-008 | The package holds the video with its SHA-256, the provenance list, the input digests and the kit 04 checklist pre-filled, for approval by hash; nothing publishes | Feature request, likely scope 4 |
| S-009 | The delivered pipeline and the manifest-verified installed set are reused | Feature request, reuse section |
| S-010 | Agents and the tool create no account, sign in nowhere, contact no seller, buy nothing and publish nothing | Feature request, hard constraint 1 |
| S-011 | Nothing is downloaded without the CEO's approval by name | Feature request, hard constraint 2 |
| S-012 | The toolchain calls no generative model | Feature request, hard constraint 3 |
| S-013 | No text-to-speech, no voice cloning, and a recording already used for another video is refused | Feature request, hard constraint 4 |
| S-014 | The toolchain costs nothing to run | Feature request, hard constraint 5 |
| S-015 | Frozen Wave 12 and Wave 13 material and the company store are untouched | Feature request, hard constraint 6 |
| S-016 | The CEO approves every publication individually | Feature request, hard constraint 7 |
| S-017 | The tooling is tested on a sample from repository fixtures or generated by repository code | Feature request, done means |
| S-018 | The presenter reads only sourced facts, never claims use, and makes no skin, hair or body claim | Business intent, CEO decision record entry 1301 item 4 |
| S-019 | One presenter is used on one account | Business intent, entries 1301 item 4 and 1302 item OD18 |
| S-020 | Each video uses a fresh real recording; a digest already used is rejected | Business intent, entry 1305 |
| S-021 | The design records a decision on each of the eleven deferred items | Execution plan, T-001 |
| S-022 | The design adds no component outside the installed set, or names each one for the CEO's approval | Execution plan, T-001 acceptance |
| S-023 | The .NET test run needs neither the company store database nor a container service | Execution plan, scope statements |
| S-024 | RK-016 part 2 is proven by a rendered vertical frame | Execution plan, T-006 |
| S-025 | The pilot ledger lives outside the company store and enforces 12 videos, two per product and distinct focus | Execution plan, T-018 |
| S-026 | Kit 03 Part B row 6 and Part F, and kit 04 item B13, match the tool | Execution plan, T-019 and T-022 |
| S-027 | The operator may be an employee recording in their own real voice | Execution plan, T-022 and T-023 |
