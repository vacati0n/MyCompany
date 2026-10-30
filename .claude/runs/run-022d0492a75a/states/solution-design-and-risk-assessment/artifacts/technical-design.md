```yaml
design:
  designId: TD-MC17-PILOT-WEB
  changeReference: runs/inputs/mc-17-feature-request.md
  sourceInputs:
    - type: feature-request
      reference: runs/inputs/mc-17-feature-request.md
    - type: business-intent
      reference: runs/inputs/mc-17-business-intent.md
    - type: execution-plan
      reference: runs/run-022d0492a75a/states/execution-planning/artifacts/execution-plan.md
  producedBy: architect
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  decisionRecords: [D-001, D-002, D-003, D-004, D-005, D-006]
  consumesPlan: runs/run-022d0492a75a/states/execution-planning/artifacts/execution-plan.md
  inputDigest: sha256:774300c5fff552255888b80292d4a999
  contextDigest: sha256:11b68469cf9e593c79e0dc76de8cb64f
```

## Metadata

- Feature or Change ID: MC-17, run run-022d0492a75a, invocation inv-022d0492a75a-03-001, phase solution-design-and-risk-assessment; this package is the design planner task T-001 asks for.
- Author: architect
- Reviewers: tech-lead (accepting Design Gate owner), product-owner (the CEO answers), qa, dev-1-implement, dev-2-reviewer
- Last Updated: 2026-10-27
- Upstream evidence cited by identifier and digest: the scope definition (sha256:1df5443872bc4d8fa40dc575de03fe03), cited as "scope item n", SC-nn for its acceptance criteria (the plan's numbering), X-nnn for its exclusions and "plan decision n" for its decisions; the execution plan (sha256:248ec10f122dd71dc03e0dcc3c422fbc), cited by T-nnn, TO-n, "plan assumption n" and "plan risk n"; the Scope Gate decision CEO decision 1310 (research/ceo-decision-record.md); the orchestrator briefing of this dispatch (gate answers and measurements), cited as "orchestrator briefing".
- Plan assumptions T-001 must settle: plan assumption 1 is carried as A-001; plan assumption 2 is confirmed by F-023 and settled in D-002; plan assumption 3 is confirmed by F-005 and settled in D-001; plan assumption 4 is confirmed by F-012 and settled in D-004; plan assumption 6 is carried as A-009 and settled in D-013; plan assumption 10 is settled by D-008 (files only, no database work); plan assumption 11 is carried as A-010 and settled in D-015.
- Repository rescan: the task context lists no relevant modules or files, so src/MediaCompany.Pilot, tests/MediaCompany.Pilot.Tests, Directory.Build.props, Directory.Packages.props, MediaCompany.slnx, pilot-affiliate-vn/mc-16 and the MC-16 run evidence were read to establish 4.1. Reason: the change builds on that toolchain and the context named none of its files.
- Skills applied: S01, S03, S06, S09, S02, S08, S12. Database area: lexical only; the design adds no database and leaves the company store untouched (C-018, D-008). Performance area: real for the live check bound (C-027) and production progress (D-009). Security area: real (Operational Considerations; R-007 to R-013).
- No implementation work was performed and no external system was accessed.

## Objective

- Desired outcome: a non-technical operator takes one pilot video from fact sheet to verified approval in a Vietnamese page on their own computer, recording the narration in the browser, while the delivered MC-16 toolchain keeps making every decision (S-001, S-002, S-019).
- Architectural objectives:
  - A new front-end boundary, its own project outside the toolchain's guarded assembly closure, that reaches the toolchain's decisions only through its command-line verbs and its pure checks only through the toolchain library, holding no copy of any rule, list or number (S-002, S-004, S-019).
  - One listening socket, on the IPv4 loopback address only, owned by the front-end process, and zero sockets owned by every toolchain, ffmpeg or ffprobe process started under it (S-018, S-033).
  - A request boundary that accepts a state-changing request only from the app's own page on this computer (S-039).
  - A conversion boundary at which a browser take becomes, once, a file in a recording format the toolchain already admits, with the raw take kept outside the toolchain's input folder (S-007, S-009, S-028).
  - A first-run boundary that produces a settings file the toolchain itself validates, with the CEO-fixed numbers sourced, not typed (S-034).
  - A test boundary that exercises the recording flow with no microphone, no network and no download (S-016, S-022, S-038).
- In scope: the new web project and its page set, its new test program, the solution entry, the pilot-folder layout the app adds, the app's own record file, and the use of the installed ffmpeg, ffprobe and headless browser shell unchanged.
- Out of scope:
  - Any change to src/MediaCompany.Pilot or tests/MediaCompany.Pilot.Tests, their checks, limits, refusal codes, settings form or approval form (S-019, X-006).
  - Editing the CEO-fixed settings numbers (S-034, X-007).
  - The company store, MediaCompany.Persistence, MediaCompany.Host, MediaCompany.Production, frozen Wave 12 and Wave 13 material and the container host (S-021, X-009).
  - Publishing, sign-in, accounts, remote or multi-user access, and more than one video at a time (S-020, X-001, X-005, X-008).
  - Generating the presenter clip and any voice, speech, cloning or metered component (S-017, X-002).
  - Hand-editing script sentences, reading product pages, kit text edits and fixes to MC-16 known issues (X-003, X-004, X-006, X-011).
  - Merging any branch into main (S-025, X-010).

## Requirements Summary

- Functional requirements:
  - Fact sheet form with every field and per-row screenshot; stored text is the typed text after NFC (S-003; C-001).
  - Live banned-term check naming term and class, answered by the toolchain's own check (S-004; C-002).
  - Images step accepting exactly the three named files (S-005; C-003); script step gated by one tick per sentence (S-006; C-004).
  - In-browser recording with a 60.0 s hard stop, playback and re-record; upload stays an option (S-007, S-008, S-030; C-005).
  - Upload passes unchanged; a browser take is converted once with the installed ffmpeg; reuse is refused by the toolchain (S-009, S-010, S-028; C-006).
  - Presenter clip checked for portrait and 0.5 s length difference (S-011, S-031; C-007).
  - Production with progress, the toolchain's line unchanged plus a Vietnamese reason and next step, and a timing editor (S-012, S-013, S-029; C-008).
  - Preview, 27-item checklist with B8 pre-ticked, approval by hash at 26 operator ticks, verify, and no publish control (S-014, S-023, S-032; C-009).
  - First-run setup screen (S-034; C-020); approval wording for the CEO and for an employee on the CEO's written instruction (S-035, S-037; C-021).
- Non-functional requirements:
  - Vietnamese screens and messages (S-001; C-010).
  - Loopback listener only, zero outbound connections, zero sockets in toolchain processes, zero external page resources (S-018, S-033; C-011, C-012).
  - Zero package references and nothing downloaded (S-016; C-013); state-changing requests only from the app's own page (S-039; C-014).
  - Tests offline with code-built fixtures and no microphone (S-022; C-017); live result within 1 s (S-004; C-027).
- Acceptance criteria: SC-01 to SC-30 as set by product-owner and approved at the Scope Gate (CEO decision 1310 item 4; S-001, S-036); each maps to a design element:

| Criterion | Design element |
|---|---|
| SC-01 | D-008 fact sheet in the per-video folder; in-process FactSheetCheck then the check verb (D-001) |
| SC-02 | D-001 screenshot names resolved by the toolchain's plain-name rule (F-008) |
| SC-03 | D-001 typed text normalized by the toolchain's NFC function before saving |
| SC-04 | D-015 live check over every typed field (C-027) |
| SC-05 | D-001 and D-015: the page holds no list; parity test in P-015 |
| SC-06 | D-008 images matched by name to the images-used field |
| SC-07 | D-012 tick state held by the app per script; next step gated on the count |
| SC-08 | D-002 timer stop at the held limit, length capped at conversion |
| SC-09 | D-002 one take per video; replacement only on accept (P-009) |
| SC-10 | D-002 uploads stored byte for byte |
| SC-11 | D-002 converted once; the toolchain's ledger refuses reuse |
| SC-12 | D-001 in-process Admission.Clip with the settings tolerance |
| SC-13 | D-009 and D-010 |
| SC-14 | D-011 |
| SC-15 | D-012 checklist read from the package |
| SC-16 | D-012 preview served from the package video, hash shown from the manifest |
| SC-17 | D-012 26 ticks, checked again on the server |
| SC-18 | D-005 approval through the approve verb with the shown hash |
| SC-19 | D-010 verify line shown; no publish control exists in M-002 |
| SC-20 | D-010 Vietnamese pages and the guide's wording (F-020) |
| SC-21 | D-003 listener and start-up self-check; P-013 address-recording harness |
| SC-22 | D-001 toolchain child under the app; P-013 job-scoped harness |
| SC-23 | D-007 and D-006 zero package references |
| SC-24 | D-003 request guard |
| SC-25 | D-001 verbs decide; parity fixtures in P-015 |
| SC-26 | C-015; M-004 to M-007 no-change-verified |
| SC-27 | D-006 fixtures built in code in a temporary folder |
| SC-28 | D-006 headless shell with code-generated audio |
| SC-29 | P-014 code-built sample and P-015 browser run |
| SC-30 | D-004 setup and D-013 launcher |

## Current-State Assumptions and Constraints

### 4.1 Facts

| ID | Fact | Established by |
|---|---|---|
| F-001 | The toolchain has verbs check, script and produce taking a settings path and a fact sheet path, approve taking a package folder, a name and a SHA-256, and verify taking a package folder; exit 0 done, 1 usage, 2 refused, 3 tool failed; one UTF-8 result line per outcome, refusals as "TỪ CHỐI [code]: message" | src/MediaCompany.Pilot/Program.cs |
| F-002 | The script verb writes script.json and script.md into the fact sheet's folder, and produce requires script.json in that input folder | src/MediaCompany.Pilot/Program.cs, Producer.cs |
| F-003 | MediaCompany.Pilot is an executable project referencing only MediaCompany.Deterministic, with no package, and turns invariant globalization off for NFC | src/MediaCompany.Pilot/MediaCompany.Pilot.csproj |
| F-004 | The repository builds net10.0 with warnings as errors and invariant globalization on by default | Directory.Build.props |
| F-005 | NetworkGuard checks the closure from the toolchain assembly downward against four allowed own assemblies and a networking list, reading the dependency file beside the toolchain assembly | src/MediaCompany.Pilot/NetworkGuard.cs |
| F-006 | SocketTelemetry is a process-wide listener for socket and name-resolution events, created only by Producer; any event fails the guard check and holds the package | src/MediaCompany.Pilot/NetworkGuard.cs, Producer.cs |
| F-007 | BannedTermCheck is public: Find returns each finding with place, class id, class name and term; FirstPerson checks overrides; Held runs over the embedded held data after NFC | src/MediaCompany.Pilot/BannedTermCheck.cs, VietnameseText.cs |
| F-008 | FactSheetCheck.Check takes fact sheet bytes and an input folder and refuses at the first error; the header and row field lists with Vietnamese labels are public; InputFiles.Resolve refuses a name that is not plain or a missing file | src/MediaCompany.Pilot/FactSheet.cs |
| F-009 | Admission accepts recordings ending .wav, .mp3, .m4a, .aac, .flac or .ogg and clips ending .mp4, .mov, .m4v, .mkv or .webm, needs exactly one of each in the input folder, refuses a recording over the held 60.0 s or whose digest the ledger holds for another video, and exposes Admission.Clip with a tolerance argument | src/MediaCompany.Pilot/Admission.cs |
| F-010 | CaptionTiming reads timing.txt (one start and end line per sentence), refuses pauses-not-found and timing-file-invalid, and exposes ReadTimingFile | src/MediaCompany.Pilot/Admission.cs |
| F-011 | MediaTool is public: its constructor checks the configured ffmpeg and ffprobe digests and version; each run is an argument list, a cleared environment, a file-only protocol allow-list, a bound and a kill-on-close job; processes start with redirected standard streams; the job type is internal | src/MediaCompany.Pilot/MediaTool.cs, JobObject.cs |
| F-012 | PilotSettings form version 1 requires every member and refuses unknown ones; Validate refuses settings-looser-than-ceo and pilot-folder-in-repository | src/MediaCompany.Pilot/PilotSettings.cs |
| F-013 | Produce writes a package folder under the pilot folder's runs folder named by video code and time, writes refusals under its refused folder, and appends a ledger entry only when ready | src/MediaCompany.Pilot/Producer.cs |
| F-014 | The approval record holds form version, video code, video SHA-256, approver text and date; Approve refuses package-held, approval-name-missing and approval-hash-mismatch, checks ledger agreement and stores the trimmed approver text unchecked; Verify refuses approval-void; checklist.md is a table of id, wording, who and result | src/MediaCompany.Pilot/Package.cs |
| F-015 | Held data holds 27 checklist items, B8 ticked by the tool, B3 and B5 noted and unticked, and the 60.0 s recording limit | src/MediaCompany.Pilot/HeldData.cs |
| F-016 | The ledger lives in the pilot folder with its previous generation and is opened by produce and by approve | src/MediaCompany.Pilot/Ledger.cs, Producer.cs, Package.cs |
| F-017 | SocketTableHarness is internal to the toolchain tests, observes processes by job membership, samples the TCP and UDP tables about every 20 ms, and records owners, not addresses | tests/MediaCompany.Pilot.Tests/SocketTableHarness.cs |
| F-018 | The toolchain test project uses the xunit, test SDK and runner packages at central versions | tests/MediaCompany.Pilot.Tests project file, Directory.Packages.props |
| F-019 | The example settings file holds a pilot folder outside the repository, ffmpeg and ffprobe in the WinGet links folder with digests, the face with its font manifest line, and the CEO numbers: safe area 60/180/200/480, caption 56, overlay 40, clip tolerance 0.5, page minimum 1.2 | pilot-affiliate-vn/mc-16/vi-du-cai-dat.json |
| F-020 | The operator guide lists fourteen refusal codes with a Vietnamese reason and remedy: banned-term, override-first-person, recording-too-long, recording-reused, clip-not-portrait, clip-length-mismatch, caption-page-too-short, settings-looser-than-ceo, ledger-truncated, ledger-entry-missing, package-file-not-plain, package-ledger-mismatch, approval-hash-mismatch, approval-void | pilot-affiliate-vn/mc-16/03-huong-dan-van-hanh.md |
| F-021 | MC-16 known issue K-003: an older package of the same video code remains approvable; K-015: reuse is detected by digest only | MC-16 release note, run run-535f3f938f32 |
| F-022 | A web-SDK net10.0 app builds here with zero packages; ASP.NET Core 10.0.12 shared framework is installed | orchestrator briefing, measurement |
| F-023 | The installed ffmpeg converts WebM/Opus to mono 16-bit PCM WAV with the same SHA-256 on two conversions | orchestrator briefing, measurement |
| F-024 | Chromium MediaRecorder emits WebM/Opus | orchestrator briefing, measurement |
| F-025 | The installed headless browser shell under the MediaCompanyRun graphics folder is approved (CEO decision 1001), has digests in browser.sha256, and supports Chromium fake media capture | orchestrator briefing, answer to planner open question 2 |
| F-026 | The solution file lists the src and tests projects, including MediaCompany.Pilot and MediaCompany.Pilot.Tests | MediaCompany.slnx |
| F-027 | The toolchain has 251 tests | feature request, reuse section |
| F-028 | Face.Admit checks the configured face's SHA-256 against its font manifest line | src/MediaCompany.Pilot/Face.cs |

### 4.2 Assumptions

| ID | Assumption | Why needed | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | The shared framework holds the web server, routing, static files, form uploads and host filtering the design uses (plan assumption 1) | C-013 for every option | A package needs the CEO's approval by name (R-002) | dev-1-implement |
| A-002 | "The MC-16 tool keeps zero sockets" (CEO decision 1310 item 1) means the toolchain program and the processes it starts, not toolchain library code the front end calls for pure checks and conversion (E-NFR-GAP) | Decides between O-003 and O-004 | O-003 is eliminated and O-004 selected (R-001) | product-owner (the CEO answers) |
| A-003 | Sockets the platform creates are not inheritable, so a child started with redirected streams holds no handle to the listener | C-012 for O-003 and O-004 | A child silently holds a listener handle (R-008) | qa |
| A-004 | The solution build leaves the toolchain executable with its dependency file in its own build output, where setup can find it | The child runs exactly as MC-16 runs | Setup refuses and names the missing file (R-003) | dev-1-implement |
| A-005 | The operator's browser treats the page at the loopback address 127.0.0.1 as a secure context and allows microphone capture | D-003 binds by address | Recording is blocked (R-010) | qa |
| A-006 | A browser folder picker does not reveal an absolute path to the page | Setup takes the pilot folder as text with a proposed default | A picker may be used; no structural change | dev-1-implement |
| A-007 | The headless shell, with fake capture from a code-generated audio file, records through MediaRecorder in a page served by the app (plan assumption 7) | D-006 route | SC-28 needs another route and a CEO decision (R-017) | qa |
| A-008 | SC-23's "zero package references" applies to every project this change adds, test projects included | Eliminates O-019 | O-019 becomes the preferred test host (R-023) | product-owner (the CEO answers) |
| A-009 | A desktop shortcut created once by the setup screen satisfies "no command line after the one-time set-up" (plan assumption 6) | D-013 launcher | A start-up task is added (R-020) | product-owner (the CEO answers) |
| A-010 | The in-process banned-term check answers within 1 s of the last keystroke over loopback (plan assumption 11) | C-027 for O-003 and O-004 | Live check redesign (R-013) | qa |
| A-011 | SC-28's "socket count zero" excludes the test browser's own loopback connection to the app under test | D-006 can pass SC-28 | SC-28 cannot pass with any browser route (R-018) | qa |
| A-012 | The operator's default browser is the Chromium-based browser with MediaRecorder the feature request names | D-002 relies on WebM/Opus takes (F-024) | Recording falls back to upload (R-019) | qa |

### 4.3 Constraints

| ID | Class | Constraint | Hard or negotiable | Source |
|---|---|---|---|---|
| C-001 | functional | Every fact sheet field in the form, rows with source section and screenshot, stored text equal to typed text after NFC | hard | SC-01 to SC-03 (S-003) |
| C-002 | functional | The live check names term and class using the toolchain's own check; the page holds no copy; results equal the toolchain's | hard | SC-04, SC-05, plan decision 4 (S-004, S-019) |
| C-003 | functional | The image step accepts exactly the three files named in the fact sheet | hard | SC-06 (S-005) |
| C-004 | functional | The next step opens only when ticks equal script sentences | hard | SC-07 (S-006) |
| C-005 | functional | The take stops at 60.0 s, plays back, and a re-record leaves exactly one take | hard | SC-08, SC-09 (S-007, S-008, S-030) |
| C-006 | functional | An upload reaches the toolchain unchanged; a browser take is converted once by the installed ffmpeg and that file's digest is the one admitted; reuse is refused | hard | SC-10, SC-11, plan decision 3 (S-009, S-010, S-028) |
| C-007 | functional | A clip is accepted only when portrait and within 0.5 s of the recording | hard | SC-12 (S-011, S-031) |
| C-008 | functional | Production shows progress and the toolchain's line unchanged with a Vietnamese reason and next step; a timing editor opens on a pause mismatch | hard | SC-13, SC-14 (S-012, S-013, S-029) |
| C-009 | functional | 27 checklist items with B8 pre-ticked; approval only at 26 operator ticks, with the shown hash; verify shown; no publish control | hard | SC-15 to SC-19, plan decision 5 (S-014, S-023, S-032) |
| C-010 | functional | Screens and messages are Vietnamese | hard | SC-20 (S-001) |
| C-011 | security | The app listens on 127.0.0.1 only, opens zero outbound connections, and its pages load zero external resources | hard | SC-21, CEO decision 1310 item 1 (S-018, S-033) |
| C-012 | security | Every toolchain, ffmpeg and ffprobe process started under the app owns zero sockets | hard | SC-22, CEO decision 1310 item 1 (S-018, S-033) |
| C-013 | operability | Zero package references are added and nothing is downloaded | hard | SC-23 (S-016) |
| C-014 | security | A state-changing request is accepted only from the app's own page on this computer | hard | SC-24, plan decision 6 (S-039) |
| C-015 | structural | The toolchain decides; its source, checks, limits, settings values and tests are unchanged and its 251 tests pass | hard | SC-25, SC-26, X-006 (S-002, S-015, S-019) |
| C-016 | compliance | No generative, speech, text-to-speech, cloning or metered component | hard | X-002 (S-010, S-017, S-027) |
| C-017 | operability | Tests run offline with fixtures built in code, add no media file, and need no microphone | hard | SC-27, SC-28 (S-022) |
| C-018 | structural | The company store, frozen Wave 12 and Wave 13 material and the container host are untouched | hard | X-009 (S-021) |
| C-019 | functional | Nothing publishes; no account, sign-in or contact action | hard | X-001 (S-020) |
| C-020 | functional | A first-run Vietnamese screen writes the settings file; the CEO-fixed numbers are written as decided and are not editable | hard | CEO decision 1310 item 2, X-007 (S-034) |
| C-021 | functional | The approval names the CEO; when an employee presses approve it also names the employee and the CEO's written instruction, all inside the existing approver text; the record form is unchanged | hard | CEO decision 1310 item 3, planner open question 1 answer (S-035, S-037) |
| C-022 | functional | Steps 1 to 7 run end to end in the browser on a code-built sample with no command line and no JSON edit after the one-time set-up | hard | SC-29, SC-30, CEO decision 1310 item 4 (S-001, S-024, S-036) |
| C-023 | functional | One operator, one computer, one video at a time | hard | X-005, X-008 (S-002) |
| C-024 | security | An uploaded file name never becomes a path; every stored file resolves inside its per-video folder | hard | plan risk 16 (S-039) |
| C-025 | operability | The change lands on the session branch cut from the unmerged MC-16 branch and merges nowhere | hard | X-010 (S-025) |
| C-026 | operability | This phase writes only its staging artifacts, no repository file | hard | orchestrator briefing, framework defects (S-026) |
| C-027 | quality-attribute | The live result arrives within 1 s of the last keystroke | negotiable | SC-04, plan assumption 11 (S-004) |
| C-028 | quality-attribute | Every installed executable is checked against its digest before use | negotiable | F-011, F-025 (S-015, S-038) |
| C-029 | quality-attribute | The converted take's digest is reproducible from the same take | negotiable | plan assumption 2 (S-009) |
| C-030 | security | No single browser-supplied request property alone decides acceptance of a state-changing request | negotiable | S09, plan risk 14 (S-039) |

## Architecture and Component Design

### 5.1 Impacted Modules

| ID | Module | Impact type | Basis | Interfaces affected | Confidence |
|---|---|---|---|---|---|
| M-001 | MediaCompany.Pilot.Web (new web project under src/) | extension | F-003, F-005, F-022, A-001 | Local page and request routes; toolchain verbs as a child; toolchain library in process | confirmed |
| M-002 | MediaCompany.Pilot.Web page set (Vietnamese pages, styles and page scripts served by M-001) | extension | F-022, F-024, A-005 | Same-origin requests to M-001 only | confirmed |
| M-003 | MediaCompany.Pilot.Web.Tests (new zero-package test program under tests/) | extension | F-017, F-018, F-025, A-007, A-008 | Starts M-001, the toolchain and the headless shell | speculative |
| M-004 | MediaCompany.Pilot command-line contract (verbs, exit statuses, result lines) | no-change-verified | F-001, F-002 | Consumed unchanged by M-001 | confirmed |
| M-005 | MediaCompany.Pilot library surface (BannedTermCheck, FactSheetCheck, InputFiles, VietnameseText, PilotSettings, HeldData, Admission, CaptionTiming, MediaTool, Face) | no-change-verified | F-007 to F-012, F-015, F-028 | Consumed unchanged in process by M-001 | confirmed |
| M-006 | MediaCompany.Pilot NetworkGuard and SocketTelemetry | no-change-verified | F-005, F-006 | None; M-001 sits above the guarded closure and never calls Producer | confirmed |
| M-007 | MediaCompany.Pilot.Tests, including SocketTableHarness | no-change-verified | F-017, F-027 | Harness source compiled unchanged into M-003 | confirmed |
| M-008 | Pilot folder layout outside the repository | extension | F-013, F-016, F-019 | Adds inputs, takes and web folders and the settings file beside the toolchain's runs, refused and ledger files | confirmed |
| M-009 | Pilot settings file form version 1 | no-change-verified | F-012, F-019 | Written by M-001, validated by the toolchain's own reader | confirmed |
| M-010 | Package approval record form | no-change-verified | F-014 | Approver text carries the D-005 wording; form unchanged | confirmed |
| M-011 | MediaCompany.slnx | extension | F-026 | Gains M-001 and M-003 | confirmed |
| M-012 | Directory.Build.props and Directory.Packages.props | no-change-verified | F-004, F-018 | None; M-001 sets globalization in its own project file | confirmed |
| M-013 | Company store with MediaCompany.Persistence, MediaCompany.Host, MediaCompany.Production and frozen Wave 12 and 13 material | no-change-verified | F-003, F-026 | None | confirmed |
| M-014 | MC-16 operator guide and kit files under pilot-affiliate-vn | no-change-verified | F-020 | Refusal wording read, not edited | confirmed |
| M-015 | Installed headless browser shell | operational-impact | F-025, A-007 | Started by M-003 only, digest-checked | speculative |
| M-016 | Installed ffmpeg and ffprobe | no-change-verified | F-011, F-023 | Started through MediaTool by M-001 (conversion, probe) and by the toolchain | confirmed |
| M-017 | App record file in the user's local application-data folder (new) | extension | A-006, F-012 | Holds pilot folder, settings path, toolchain path, port and CEO name | confirmed |

Boundary crossings in the selected approach: browser page to M-001 over loopback (guarded, D-003); M-001 to the toolchain executable as a child process (argument list, UTF-8 result line, exit status); M-001 to M-005 in process (pure checks, settings validation, probe and conversion through MediaTool); M-005's MediaTool to M-016 (child processes, file-only protocol); M-001 to M-008 (file writes contained in the pilot folder); M-003 to M-015 (test only).

### 5.2 Options Considered

Six decision families, each evaluated on its own. Columns follow the A8 order; reuse leverage counts the section 7 capabilities of that family met by reuse-as-is or reuse-extended; quality attributes count the family's negotiable constraints met.

| Option | Family | Structural change | Hard constraints | Impact surface | Reuse leverage | Quality attributes | Migration burden | Operability | Outcome |
|---|---|---|---|---|---|---|---|---|---|
| O-001 | D-001 process boundary | All toolchain work in the web process through the library, produce included | C-012 violated: produce would run its telemetry inside the listener's process (F-006) | 0 | 9 | C-027, C-028: 2 of 2 | 0 | one process | eliminated (C-012) |
| O-002 | D-001 process boundary | Toolchain reached only as a child through its verbs | C-002 violated: check stops at the first error with no class list (F-001, F-008); a new verb breaks C-015 | 1 (M-004 if a verb is added) | 4 | C-027: unmet; C-028 met: 1 of 2 | 0 | one process plus children | eliminated (C-002) |
| O-003 | D-001 process boundary | Pure checks, settings validation, probe and conversion in process through the library; check, script, produce, approve and verify as a toolchain child | all met (C-012 under A-002) | 0 | 9 | 2 of 2 (C-027 under A-010) | 0 | high: one resident process; children per action | selected |
| O-004 | D-001 process boundary | As O-003, but every library call runs in a resident socket-free helper process reached over its standard streams | all met | 0 | 9 | 2 of 2 (C-027 under A-010) | 0 | medium: two resident processes and an internal message form to keep in step with the library | rejected at operability; selected if A-002 is false |
| O-005 | D-002 recording | Take converted once on acceptance through MediaTool into mono 16-bit PCM WAV with fixed parameters and length capped at the held limit; raw take kept outside the input folder | all met | 0 | 3 | C-028, C-029: 2 of 2 (F-023) | 0 | high | selected |
| O-006 | D-002 recording | Take's audio copied without re-encoding into an Ogg file through MediaTool | all met | 0 | 3 | C-028 met; C-029 unmeasured: 1 of 2 | 0 | high | rejected at quality attributes |
| O-007 | D-002 recording | Page encodes WAV itself with the browser's audio processing | C-006 violated: not converted by the installed ffmpeg | 0 | 1 | 1 of 2 | 0 | high | eliminated (C-006) |
| O-008 | D-002 recording | WebM take passed to the toolchain unchanged | C-015 violated: .webm is not an admitted recording and counts as a clip (F-009) | 1 (M-005) | 2 | 1 of 2 | 1 | low | eliminated (C-015) |
| O-009 | D-003 request guard | Listener on 127.0.0.1 only; host allow-list on every request; exact Origin plus a per-start token on every state-changing request; no cross-origin policy | all met | 0 | 3 | C-030: 1 of 1 | 0 | high | selected |
| O-010 | D-003 request guard | Listener on 127.0.0.1 only; exact Origin on every state-changing request | all met | 0 | 2 | C-030: 0 of 1 | 0 | high | rejected at reuse leverage |
| O-011 | D-003 request guard | Listener on 127.0.0.1 only; per-start token only | C-014 violated: a page under a foreign host name resolving to 127.0.0.1 reads the token | 0 | 1 | 0 of 1 | 0 | high | eliminated (C-014) |
| O-012 | D-004 first-run setup | Setup screen locates tools, face and toolchain, takes the pilot folder, copies the CEO numbers from the repository's example settings file, validates with the toolchain's own reader | all met | 0 | 5 | C-028: 1 of 1 | 0 | high | selected |
| O-013 | D-004 first-run setup | As O-012, with the CEO numbers held as constants in the app | all met | 0 | 4 | C-028: 1 of 1 | 0 | medium: a second copy to keep equal | rejected at reuse leverage |
| O-014 | D-004 first-run setup | No setup screen; a settings file prepared once by the orchestrator | C-020 violated (CEO decision 1310 item 2) | 0 | 3 | 1 of 1 | 0 | low | eliminated (C-020) |
| O-015 | D-005 approval wording | One approver text: the CEO's name with a role mark, plus the employee's name and the written-instruction note when an employee presses approve | all met | 0 | 2 | none recorded | 0 | high | selected (only viable) |
| O-016 | D-005 approval wording | CEO's name in the approver text and a separate app record of employee and instruction beside the package | C-021 violated: outside the existing field | 1 (M-010) | 2 | none recorded | 1 | medium | eliminated (C-021) |
| O-017 | D-005 approval wording | CEO's name only, whoever presses | C-021 violated: the employee is not named | 0 | 2 | none recorded | 0 | high | eliminated (C-021) |
| O-018 | D-006 recording test | Installed headless shell with fake capture from a code-generated audio file, driven by a same-origin test page that posts results to a test-only route; tests in a zero-package test program | all met (C-013 under A-008) | 0 | 4 | C-028: 1 of 1 | 0 | high | selected |
| O-019 | D-006 recording test | Same browser route; tests in a new test project referencing the centrally pinned test packages | C-013 violated under A-008 | 0 | 5 | 1 of 1 | 0 | high | eliminated (C-013); preferred if A-008 is false |
| O-020 | D-006 recording test | Headless shell driven through its remote debugging protocol over a platform web-socket client | all met | 0 | 3 | 1 of 1 | 0 | medium: a debugging listener and a protocol client | rejected at reuse leverage (C-028: 1 of 1 met, less reuse than O-018) |
| O-021 | D-006 recording test | A downloaded browser-automation package | C-013 violated | 0 | 2 | 0 of 1 | 0 | medium | eliminated (C-013) |
| O-022 | D-006 recording test | Manual recording evidence by the operator | C-017 violated: needs a microphone | 0 | 1 | 0 of 1 | 0 | low | eliminated (C-017) |

Recomputed ranking, criteria in A8 order: D-001 O-003 over O-004 (tied through migration burden, O-003 wins operability); D-002 O-005 over O-006 (quality attributes 2 to 1); D-003 O-009 over O-010 (reuse 3 to 2); D-004 O-012 over O-013 (reuse 5 to 4); D-005 O-015 (only option meeting C-021); D-006 O-018 over O-020 (reuse 4 to 3).

### 5.3 Selected Approach

- Selected: O-003 (the process boundary, D-001), the structural choice the other five sub-decisions sit inside.
- Per-decision selections: D-002 O-005, D-003 O-009, D-004 O-012, D-005 O-015, D-006 O-018.
- Structural change: a new web project M-001 serving the page set M-002 on 127.0.0.1 only; it references the toolchain project for in-process pure checks, settings validation, probe and conversion, and starts the toolchain executable as a child for every decision (check, script, produce, approve, verify). A new zero-package test program M-003 and a solution entry M-011. Files only, in the pilot folder (M-008) and the app record (M-017). No existing module changes contract or dependencies.
- Rationale: O-003 is the only process boundary that answers the live check from the toolchain's own list (C-002) without running produce inside the listener's process (C-012, F-006), with one resident process to operate. O-005 gives a reproducible admitted file through the toolchain's own digest-checked ffmpeg runner (F-011, F-023). O-009 refuses the six SC-24 cases with no single header deciding. O-012 sources every CEO number from the repository's example file and lets the toolchain validate the result. O-015 is forced by C-021. O-018 tests recording with no microphone and no download.
- Highest-scoring rejected alternative and why it lost: O-004 ties O-003 on every criterion up to operability and loses there, because it adds a second resident process and a message form that must follow the library. It becomes the selection if the CEO reads CEO decision 1310 item 1 strictly (A-002, Q-001).
- Tradeoffs accepted:
  - Toolchain library code runs inside the process that owns the listener, for pure checks, probe and conversion only; produce, approve and verify never do (A-002).
  - The recording reuse refusal is shown at produce, because only the toolchain child opens the ledger (D-008).
  - The test program does not use the repository's test framework (A-008, Q-002).
  - Progress shows elapsed time and running state only, because the toolchain prints at the end (D-009).

### 5.4 Decisions

| ID | Decision | Architecture-significant | Record |
|---|---|---|---|
| D-001 | Process boundary: pure checks, settings validation, probe and conversion in process through the toolchain library; check, script, produce, approve and verify as a toolchain child with an argument list, no shell, UTF-8 output and a kill-on-close job; M-001 never calls Producer or the toolchain's entry point (O-003) | yes | architecture-decision-record-D-001.md |
| D-002 | Recording: the browser take is converted once, on acceptance, through MediaTool into mono 16-bit PCM WAV with fixed parameters, no metadata and length capped at the held limit; raw takes stay in the takes folder; uploads are stored byte for byte with an admitted extension; one recording per video (O-005) | yes | architecture-decision-record-D-002.md |
| D-003 | Request guard: listener on 127.0.0.1 only with a start-up check of the bound addresses; host allow-list on every request; exact Origin and a per-start token in a custom header on every state-changing request; no cross-origin policy; own-origin content policy (O-009) | yes | architecture-decision-record-D-003.md |
| D-004 | First-run setup: pilot folder as text with a proposed default; ffmpeg and ffprobe found in the WinGet links folder or on the search path; face found through its font manifest line; toolchain executable found in its build output; CEO numbers copied from the repository's example settings file; result validated by PilotSettings.Validate before it is written (O-012) | yes | architecture-decision-record-D-004.md |
| D-005 | Approval wording: CEO presses: "[CEO name] (CEO)"; employee presses: "[CEO name] (CEO; nhân viên [employee name] bấm duyệt theo chỉ thị bằng văn bản của CEO)"; names NFC and trimmed; a name with a control character, "(", ")" or ";" is refused by the app (O-015) | yes | architecture-decision-record-D-005.md |
| D-006 | Recording test: installed headless shell, digest-checked, with fake capture from a code-generated audio file and non-loopback name resolution blocked, driving the real recording page from a same-origin test page; zero-package test program; harness source reused unchanged plus an address-recording variant (O-018) | yes | architecture-decision-record-D-006.md |
| D-007 | M-001 uses the web SDK at net10.0 with no package, and turns invariant globalization off in its own project file so NFC matches the toolchain (F-003, F-004) (O-003) | no | inline |
| D-008 | Files only: the per-video input folder is the pilot folder's inputs folder plus the video code, which must pass the toolchain's plain-name rule; raw takes in the takes folder; tick state, last produced package and the log in the web folder; the app never writes the ledger, a package folder or the refused folder; no database (O-003, O-005) | no | inline |
| D-009 | Produce runs as one background job at a time; the page polls a read-only progress route showing elapsed time and running state; a second produce is refused in Vietnamese; the job ends at the app's bound with a tool-failure message (O-003) | no | inline |
| D-010 | Outcome display: the toolchain's line unchanged; for the fourteen codes of F-020 the guide's Vietnamese reason and next step; for any other refusal code a general next step; exit 1 shown as an app fault, exit 3 as a tool failure; verify line shown after approval (O-003) | no | inline |
| D-011 | Timing editor: opens on pauses-not-found and timing-file-invalid, is offered on caption-page-too-short, holds one start and end per sentence, is checked in process by CaptionTiming.ReadTimingFile, is saved as timing.txt in the input folder, then produce runs again (O-003) | no | inline |
| D-012 | Checklist and approval: the checklist is read from the package's checklist.md and approval is not offered if its 27 identifiers differ from held data; preview and hash come from the package the app's own latest produce wrote (K-003); the approval request carries the ticked identifiers, which the server checks equal the 26 operator items (O-003) | no | inline |
| D-013 | Launcher: setup creates a desktop shortcut on the operator's confirmation; a named single-instance lock makes a second start open the browser at the running address; the app opens the default browser at its own address on start (O-012) | no | inline |
| D-014 | Log: a local file in the web folder records route, refusal code, exit status and duration, never file contents or names typed by the operator (O-003) | no | inline |
| D-015 | Live check: the page sends each typed field after a short pause in typing; M-001 answers with BannedTermCheck.Find for every field and FirstPerson for overrides, naming field, term and class (O-003) | no | inline |

## API and Data Model Impact

- API changes: M-001 adds local routes reachable from 127.0.0.1 only, none used by another system:
  - Read routes (host allow-list only, no state change): pages and assets, progress, checklist, package video for preview, setup status.
  - State-changing routes (host allow-list, exact Origin and per-start token): setup save, shortcut creation, fact sheet save, live check, image upload, screenshot upload, script run, script tick save, take upload, take accept, recording upload, clip upload, produce start, timing save, checklist tick save, approval, verify.
- Contract compatibility notes:
  - M-004, M-005, M-009 and M-010 are consumed unchanged (C-015); no module has contract-change, so no transition strategy is required.
  - The approver text convention of D-005 lives inside the existing free-text field; a record written by the MC-16 command line stays valid and reads the same in verify.
  - M-001 parses two toolchain lines: the package folder from the produce success or held line, and the verify line; both forms are pinned by parity fixtures in P-015 (R-004).
- Schema or migration changes:
  - None in the company store (C-018); no data migration.
  - New files only, with no prior data: migration direction is not applicable, reversibility is total (removing M-001 leaves the toolchain's files readable), and every new file carries a form version so a later reader refuses a form it does not know.
- Data model of the new files (description, not a schema):
  - App record: pilot folder, settings file path, toolchain executable path and SHA-256, port, CEO name, form version.
  - Per-video input folder: the fact sheet in the toolchain's form, the three images, the screenshots, exactly one recording, at most one clip, script.json and script.md written by the script verb, and timing.txt when written.
  - Takes folder: the raw browser take of the current video until replaced.
  - Web folder: per video, the ticked script lines, the ticked checklist items and the last package folder produced by the app; the log.

## Reusable Components and Reuse Rationale

| Capability | Candidate | Outcome | Rationale |
|---|---|---|---|
| Web server, routing, static pages, uploads | ASP.NET Core shared framework (F-022) | reuse-as-is | Installed; builds with zero packages (A-001) |
| Host allow-list | Shared framework host filtering | reuse-as-is | Native framework capability (A-001) |
| Request token | Shared framework antiforgery | rejected | Pairs a cookie with a persisted key ring in the user profile; a token held only for one process start meets C-014 with no stored secret |
| Request token and Origin check | none | none-found | Searched existing components, the standard library, framework capability and installed dependencies after rejecting antiforgery; a small check in M-001 |
| Live banned-term check | BannedTermCheck.Find and FirstPerson (F-007) | reuse-as-is | The toolchain's own check and list (C-002) |
| NFC of typed text | VietnameseText.Nfc (F-007) | reuse-as-is | Same normalization as the toolchain |
| Fact sheet validation | FactSheetCheck.Check in process and the check verb (F-001, F-008) | reuse-as-is | In-process for the form, the verb as the decision |
| Plain file names | InputFiles.Resolve (F-008) | reuse-as-is | The toolchain's rule for every stored name (C-024) |
| Script generation | script verb (F-002) | reuse-as-is | Writes the files produce reads |
| Recording conversion | MediaTool (F-011) with the installed ffmpeg | reuse-as-is | Digest check, argument list, file-only protocol, kill-on-close job |
| Media probe for step checks | MediaTool.Probe (F-011) | reuse-as-is | Same probe the toolchain uses |
| Clip check | Admission.Clip (F-009) | reuse-as-is | Takes the settings tolerance; no copy of the rule |
| Recording admission in process | Admission.Recording (F-009) | rejected | Needs the ledger, which only the toolchain child opens (D-008); the step shows the probe length against the held limit, and produce decides |
| Recording limit | HeldData.Current (F-015) | reuse-as-is | The held 60.0 s, not a copy |
| Timing file check | CaptionTiming.ReadTimingFile (F-010) | reuse-as-is | Validates editor input before saving |
| Production, approval, verify | produce, approve and verify verbs (F-001) | reuse-as-is | The toolchain decides (C-015) |
| Refusal wording | Operator guide table (F-020) | reuse-as-is | Fourteen reasons and next steps carried into the pages |
| Checklist | Package checklist.md and held data (F-014, F-015) | reuse-as-is | Read from the package, checked against held data |
| Settings read and validation | PilotSettings.Read and Validate (F-012) | reuse-as-is | The toolchain's own reader |
| CEO numbers | Example settings file (F-019) | reuse-as-is | Already holds the CEO decision 1308 and CEO decision 1309 values |
| Face lookup | Font manifest and Face.Admit (F-028) | reuse-as-is | Same check the toolchain runs |
| Child process with kill-on-close | Toolchain job type (F-011) | rejected | Internal to the toolchain; exposing it changes the toolchain (C-015) |
| Child process with kill-on-close | Platform process API and job object | reuse-as-is | The primitives the toolchain job is built on |
| Single instance | Platform named lock | reuse-as-is | Native platform capability |
| Opening the browser and the desktop shortcut | Platform shell | reuse-as-is | Native platform capability |
| Approver text composition | none | none-found | Searched existing components, the standard library, framework capability and installed dependencies; the toolchain stores the text unchecked (F-014) |
| Socket evidence by owner | SocketTableHarness (F-017) | reuse-as-is | Source compiled unchanged into M-003 |
| Socket evidence with addresses | SocketTableHarness (F-017) | reuse-extended | A variant in M-003 reads the same tables keeping local and remote addresses; the harness file is unchanged |
| Browser without microphone | Installed headless shell (F-025) | reuse-as-is | Fake capture supported; manifest-checked |
| Browser automation | Downloaded automation package | rejected | Download breaks C-013 |
| Browser automation | Remote debugging protocol | rejected | Opens a debugging listener and needs a protocol client; the same-origin test page needs neither |
| Test host | xunit at central versions (F-018) | rejected | Adds package references under A-008 (Q-002) |
| Test host | Platform console program | reuse-as-is | Exit status carries the result |
| Generated fixtures | Platform audio file writing and the installed ffmpeg synthetic sources | reuse-as-is | MC-16 pattern; nothing added to the repository |
| Hashing | Platform SHA-256 | reuse-as-is | Native platform capability |
| App record and log storage | Platform file API and the local application-data folder | reuse-as-is | Native platform capability |

## Operational Considerations

- Logging and observability updates: D-014's local log in the web folder (M-008); the toolchain keeps its own run record and invoked-tool log inside each package (M-004); nothing reaches the company store (C-018).
- Error handling strategy (S12):
  - The toolchain's exit status decides the category: 0 done, 2 refused, 3 tool failure, 1 app fault (D-010).
  - A step pre-check failure in M-001 is shown in Vietnamese and stops before any toolchain call; the toolchain's own refusal remains authoritative (C-015).
  - A child that exceeds the app's bound is ended with its job, and the page shows a tool failure (D-009).
  - Setup refuses, in Vietnamese and naming the item, a missing ffmpeg, ffprobe, face, font manifest or toolchain executable, a digest mismatch, and each refusal of PilotSettings.Validate (D-004).
- Security considerations (S09):
  - Listener on 127.0.0.1 only, checked at start-up; any other bound address stops the app (C-011, D-003, R-007).
  - Request guard on the seventeen state-changing routes listed in section 6 (C-014, C-030, R-006).
  - Content policy allowing only the app's own origin and in-page media, no framing, no referrer, microphone only for the app's own origin (C-011).
  - Uploaded names are matched against names the fact sheet declares and then resolved with the toolchain's plain-name rule; an uploaded name is never used as a path (C-024, R-009).
  - Approval stays bound to the recomputed hash by the toolchain (F-014); the app sends the manifest value it shows; an employee approval names the employee (C-021, R-011).
  - The per-start token is held in memory only, never logged or stored (D-014).
  - Compliance: no generative, speech or metered component is referenced or started (C-016); nothing publishes (C-019).
- Performance considerations (S08): the live check is bounded by C-027 and answered in process (A-010, R-013); produce time is bounded by the toolchain's process bounds (F-011) and the app's own bound (D-009); uploads have per-route size ceilings no lower than the toolchain's own upload ceiling.
- Deployment or operability impact: the app runs from the repository build on the operator's computer (A-004), starts from the desktop shortcut (D-013, A-009), and needs no container, database or service (C-018). The S06 database area changes nothing (D-008).

## Delivery Plan

### Sequencing Constraints

| ID | Constraint | Modules | Prerequisites | Reason | Binds |
|---|---|---|---|---|---|
| P-001 | The Design Gate decision on D-001 to D-006 precedes any module; M-001 then exists as its own project with zero packages, referencing only the toolchain project, with its M-011 entry | M-001, M-005, M-011, M-012 | none | Verification of a boundary precedes work that assumes it | T-001, T-017 |
| P-002 | The 127.0.0.1 listener, its start-up address check and the D-003 guard exist before any state-changing route | M-001, M-002 | P-001 | A boundary precedes the routes it protects | T-017, T-020 |
| P-003 | Setup writes the app record and a settings file validated by the toolchain's reader before any route reads settings or starts the toolchain | M-001, M-008, M-009, M-017 | P-002 | Contract definition precedes every consumer | T-026 |
| P-004 | The toolchain child path (argument list, no shell, UTF-8 line, exit-status mapping, kill-on-close job, bound) and the in-process library calls exist before any step uses them | M-001, M-004, M-005, M-006 | P-003 | Contract definition precedes every consumer | T-018 |
| P-005 | The per-video folder layout and plain-name resolution exist before any file is stored | M-001, M-008 | P-004 | Containment precedes every write | T-003, T-006 |
| P-006 | The fact sheet is saved and passes the check verb before the script verb runs | M-001, M-002 | P-005 | The script verb reads the saved sheet (F-002) | T-003, T-005, T-007 |
| P-007 | The three images and the screenshots are stored under their declared names before produce can run | M-001, M-002 | P-005 | Produce reads them from the input folder | T-006 |
| P-008 | The script exists and every sentence is ticked before recording opens | M-001, M-002 | P-006 | A take is made after its script is approved (S-028) | T-007, T-008 |
| P-009 | The conversion path exists with raw takes outside the input folder; a new take replaces the admitted one only on accept, so the prior take survives until then | M-001, M-008, M-016 | P-008 | A reversibility safeguard precedes the replacement | T-008, T-009 |
| P-010 | The clip check reads the admitted recording's length | M-001, M-002 | P-009 | The tolerance compares two lengths | T-010 |
| P-011 | Produce, its refusal display and the timing editor exist after every input is stored | M-001, M-002 | P-007, P-010 | Produce needs every input | T-011, T-012 |
| P-012 | Checklist and preview read the produced package before approval is offered; D-005 wording and the 26-tick server check exist before the approval route; verify follows approval | M-001, M-002, M-010 | P-011 | Approval binds to an assembled package | T-013, T-014, T-015, T-016 |
| P-013 | The address-recording harness variant and the D-006 browser route exist before the evidence of SC-21, SC-22, SC-24 and SC-28 | M-003, M-007, M-015 | P-002 | Verification of a boundary precedes evidence that assumes it | T-002, T-019, T-024 |
| P-014 | The code-built sample is generated in a temporary folder outside the repository before the end-to-end run | M-003 | P-001 | No media enters the repository (C-017) | T-023 |
| P-015 | Parity fixtures, the unchanged 251 toolchain tests, the harness evidence and the browser sample run exist before the release note and delivery | M-003, M-004, M-007 | P-012, P-013, P-014 | Release evidence precedes release | T-004, T-021, T-022, T-025, T-027, T-028 |

### Test Strategy Focus Areas

- Parity (SC-05, SC-25): the toolchain's banned-term fixtures and the seven class examples through D-015 and through the toolchain; the fourteen refusal fixtures of F-020 and the approval fixtures through the toolchain directly and through the app, comparing line and exit status.
- Boundary (SC-21, SC-22): the harness tracks the app process; rows owned by the app must be the 127.0.0.1 listener and loopback connections only; rows owned by any other job member must be zero; an M-003 check that M-001 never references Producer or the toolchain entry point.
- Request guard (SC-24): produce, timing save and approval, each with a foreign Origin and with a foreign Host; pilot-folder hashes equal before and after.
- Recording (SC-08, SC-09, SC-11, SC-28): headless shell with a generated audio file longer than 60 s; stop at 60.0 s; playback; re-record with a different length; two conversions of one take give one digest; the second video with the same file is refused.
- Setup (T-026): settings accepted by the toolchain; no input for any CEO number; a missing tool named in Vietnamese; a pilot folder inside the repository refused.
- Repository (SC-23, SC-26, SC-27): project and package files compared with the prior version; toolchain folders unchanged; no media file added.

### Rollout and Rollback

- Rollout: M-001, M-002, M-003 and the M-011 entry land on the session branch cut from the MC-16 branch (C-025); the operator builds once, runs setup, and starts from the shortcut.
- Rollback: remove M-001, M-002, M-003 and their solution entries, the app record and the shortcut; the toolchain, its settings file, packages and ledger stay valid, and the command line works as before.

## Risks and Mitigations

| ID | Class | Trigger | Impact | Likelihood | Affects | Mitigation | Owner |
|---|---|---|---|---|---|---|---|
| R-001 | structural | The CEO reads CEO decision 1310 item 1 as covering toolchain library code (A-002 false) | O-003 is eliminated; a helper process is added | medium | D-001, P-001 | CEO choice in D-001 before P-001 (Q-001) | product-owner |
| R-002 | structural | A needed capability is absent from the shared framework (A-001 false) | A package needs the CEO's approval by name | low | M-001, P-001 | P-001 builds the empty project first | dev-1-implement |
| R-003 | structural | The toolchain executable or its dependency file is not in its build output (A-004 false) | The child cannot start or its guard reads nothing | low | D-004, P-004 | Setup checks the executable; each package's guard result is checked in P-015 | dev-1-implement |
| R-004 | contract | The toolchain's produce or verify line changes on the unmerged MC-16 branch | The app misreads the package folder or verify result | medium | M-004, P-015 | Parity fixtures in P-015 against the branch build | qa |
| R-005 | contract | An operator's name contains "(", ")", ";" or a control character | The app refuses the name | low | D-005 | Vietnamese message naming the character | product-owner |
| R-006 | security | A foreign page sends a produce, timing or approval request to the loopback app | An unapproved production or approval | low | D-003, P-002 | D-003 guard; SC-24 cases in P-013 | qa |
| R-007 | security | The listener binds beyond 127.0.0.1 through an environment or configuration override | CEO decision 1310 item 1 breached | low | D-003, P-002 | Explicit binding; start-up address check stops the app | qa |
| R-008 | security | A toolchain child inherits the listener handle (A-003 false) | A socket handle outside the owner tables | low | D-001, P-004 | Handle inspection of a child in P-013 | qa |
| R-009 | security | An uploaded file name carries path segments | A write outside the per-video folder | medium | D-008, P-005 | Declared names only, through the plain-name rule | dev-1-implement |
| R-010 | security | 127.0.0.1 is not a secure context in the operator's browser (A-005 false) | Microphone blocked | low | D-003, P-002 | Recording test in P-013; D-003 revisited with the CEO | qa |
| R-011 | security | An employee approves without the CEO's written instruction | A publication without the CEO's approval | medium | D-005 | The approval screen states the rule; the record names the employee | product-owner |
| R-012 | security | A program already on the computer reads the page token and forges a request | A forged approval | low | D-003 | Residual under X-005; the toolchain still requires the video's hash | product-owner |
| R-013 | performance | The live result arrives later than 1 s (A-010 false) | SC-04 fails | low | D-015, P-006 | In-process call; measured in P-015 | qa |
| R-014 | performance | Produce runs long with no intermediate output | The operator thinks the app hung | medium | D-009, P-011 | Elapsed time and running state; bound | dev-1-implement |
| R-015 | operability | A full-length take converts to slightly over 60.000 s | The toolchain refuses a correct take | medium | D-002, P-009 | Length capped at the held limit at conversion; tested in P-013 | qa |
| R-016 | operability | Conversion gives a different digest on another ffmpeg build | Test evidence differs; admission unaffected (converted once) | low | D-002 | The digest admitted is the stored file's | qa |
| R-017 | operability | The headless shell cannot drive capture in a page served by the app (A-007 false) | SC-28 not automated | low | D-006, P-013, M-003, M-015 | Driver opens the page top-level; else Q-002 route | qa |
| R-018 | operability | SC-28's socket count includes the browser's loopback connection (A-011 false) | SC-28 cannot pass | medium | D-006, P-013 | Q-003 | qa |
| R-019 | operability | The default browser lacks WebM/Opus MediaRecorder (A-012 false) | Recording unavailable | low | D-002 | Page says so in Vietnamese and offers upload | dev-1-implement |
| R-020 | operability | A desktop shortcut does not satisfy SC-30 (A-009 false) | A start-up task is added | low | D-013 | Q-004 | product-owner |
| R-021 | operability | An older package of the same code is approved (F-021) | The wrong video is approved | low | D-012, P-012 | Approval only for the app's latest package of the code | dev-1-implement |
| R-022 | operability | A raw take lands in the input folder | The toolchain counts it as a second clip (F-009) | medium | D-008, P-009 | Takes folder outside the input folder | dev-1-implement |
| R-023 | delivery | The CEO reads SC-23 as "nothing downloaded" (A-008 false) | The conventional test project is preferred | medium | D-006, P-013 | Q-002 | product-owner |
| R-024 | delivery | The MC-16 branch changes after this branch is cut | Rebase and toolchain re-test | medium | P-015 | Toolchain tests and parity fixtures re-run | orchestrator |
| R-025 | delivery | The compare link against main also shows the MC-16 commits | MC-17 review confused with MC-16 | high | P-015 | The release note and link message name the base | documentation |
| R-026 | delivery | The Design Gate decision is delayed or declines the design | Implementation waits or returns to design | medium | P-001 | Q-005 | tech-lead |

## Estimate and Confidence

- Overall: L (confidence: low), a cross-boundary change (browser, web process, toolchain child) resting on unconfirmed A-002, A-005, A-007 and A-008.
- Breakdown:
  - M-001: L (confidence: medium); seventeen guarded routes, child and in-process calls, setup.
  - M-002: M (confidence: medium); eight Vietnamese screens, recorder, timing editor, checklist.
  - M-003: M (confidence: low); harness variant, headless shell route, parity fixtures (A-007, A-011).
  - M-008, M-011, M-017: XS (confidence: high).
- Scope assumptions: one video at a time (C-023); the toolchain unchanged (C-015); only the installed set (C-013); answers to Q-001 and Q-002 confirm A-002 and A-008.
- Uncertainty drivers: the CEO's readings of CEO decision 1310 item 1 and SC-23; fake capture in the headless shell; secure-context treatment of 127.0.0.1.

## Open Decisions and Escalations

E-AUTHORITY events are recorded as Q-001, Q-002 and Q-004, each owned by its authority; the E-NFR-GAP of A-002 is Q-001. No E-BOUNDARY event occurred.

| ID | Question | Blocking | Owner | Affects | Consequence |
|---|---|---|---|---|---|
| Q-001 | Does "the MC-16 tool keeps zero sockets" allow the web app to run the toolchain's word check and conversion inside its own process (A-002)? The CEO answers. | non-blocking | product-owner | D-001, P-001 | Yes: O-003. No: O-004, adding a socket-free helper process |
| Q-002 | Does SC-23 forbid a new test project that references the test packages already installed (A-008)? The CEO answers. | non-blocking | product-owner | D-006, P-013 | Yes: O-018 zero-package test program. No: O-019 test project |
| Q-003 | Does SC-28's "socket count zero" exclude the test browser's loopback connection to the app (A-011)? | non-blocking | qa | D-006, P-013 | Yes: as designed. No: SC-28 needs a product-owner amendment |
| Q-004 | Is a desktop shortcut made once by the setup screen part of the one-time set-up of SC-30 (A-009)? The CEO answers. | non-blocking | product-owner | D-013 | Yes: as designed. No: a start-up task is added |
| Q-005 | Design Gate acceptance of D-001 to D-006, all at Proposed; tech-lead accepts under the producer exclusion rule, with the CEO's choices recorded | non-blocking | tech-lead | D-001, D-002, D-003, D-004, D-005, D-006 | Accepted: implementation starts at P-001. Not accepted: the design returns to the architect |

## Sign-off

- Architect: unsigned; producer of this package and its records, so excluded from accepting under the Producer Exclusion Rule
- Tech Lead: unsigned; accepting Design Gate owner for this package and for D-001 to D-006
- Product Owner: unsigned; product confirmation of Q-001, Q-002 and Q-004 (the CEO answers)
- QA: unsigned; review of the test focus areas and of Q-003

## Appendix A: Statement Register

| ID | Statement | Source | Maps to |
|---|---|---|---|
| S-001 | The operator is non-technical and enters data step by step in a friendly interface, never editing JSON or typing a command line | Feature request, Why | C-010, C-022 |
| S-002 | MC-17 is a local web front end on top of the MC-16 toolchain | Feature request, Why | C-015, C-023, M-001 |
| S-003 | Step 1: every fact sheet field in a form, each row with source section and screenshot | Feature request, step 1 | C-001 |
| S-004 | Banned terms are checked live as the operator types, naming term and class | Feature request, step 1 | C-002, C-027 |
| S-005 | Step 2: upload or pick the three images | Feature request, step 2 | C-003 |
| S-006 | Step 3: see the script and tick each sentence's Part E source line | Feature request, step 3 | C-004 |
| S-007 | Step 4: record in the browser; upload stays an option | Feature request, step 4 | C-005 |
| S-008 | A timer with a hard stop at 60 s, playback and re-record | Feature request, step 4 | C-005 |
| S-009 | The recording is converted with the installed ffmpeg | Feature request, step 4 | C-006, C-029 |
| S-010 | Never text-to-speech or cloning; a reused recording is refused | Feature request, step 4 | C-006, C-016 |
| S-011 | Step 5: upload the presenter clip; portrait and length within 0.5 s | Feature request, step 5 | C-007 |
| S-012 | Step 6: produce with progress and every refusal in plain Vietnamese with what to do next | Feature request, step 6 | C-008 |
| S-013 | A manual timing editor when pauses do not match the sentence count | Feature request, step 6 | C-008 |
| S-014 | Step 7: preview, kit 04 checklist, ticks, approval by hash | Feature request, step 7 | C-009 |
| S-015 | Reuse the toolchain, its tests, the MC-16 evidence and the installed set | Feature request, reuse section | C-015, C-028 |
| S-016 | Download nothing without the CEO's approval by name | Feature request, hard constraint 1 | C-013 |
| S-017 | No generative model, speech recognition or metered service | Feature request, hard constraint 2 | C-016 |
| S-018 | The app binds 127.0.0.1 only with no outbound connection; the toolchain stays at zero sockets | Feature request, hard constraint 3 | C-011, C-012 |
| S-019 | Front end only; no MC-16 check weakened; the tool keeps deciding | Feature request, hard constraint 4 | C-002, C-015 |
| S-020 | Nothing publishes; no sign-in or account action | Feature request, hard constraint 5 | C-019 |
| S-021 | Frozen Wave 12 and 13 and the company database are untouched | Feature request, hard constraint 6 | C-018 |
| S-022 | Offline tests, code-built fixtures, no media file, a recording test with no microphone | Feature request, hard constraint 7 | C-017 |
| S-023 | Every publication is approved by the CEO individually | Feature request, hard constraint 8 | C-009 |
| S-024 | The app is shown working in the browser on a code-built sample including recording | Feature request, done means | C-022 |
| S-025 | Committed and pushed; compare link notes the MC-16 base | Feature request, done means | C-025 |
| S-026 | A non-implementing role writes no repository file; "every" claims list their cases | Feature request, framework defects | C-026 |
| S-027 | Narration is the real voice of the CEO or the employee operator | Business intent, CEO decision 1304, CEO decision 1307 | C-016 |
| S-028 | Each video uses a fresh recording made after its script is approved; a used digest is rejected | Business intent, CEO decision 1305 | C-006, P-008 |
| S-029 | Captions are timed by pause detection, with manual timing on a mismatch | Business intent, CEO decision 1306 item 3 | C-008 |
| S-030 | A recording over 60 s is refused | Business intent, CEO decision 1306 item 4 | C-005 |
| S-031 | Safe areas, text sizes and the 0.5 s clip tolerance are CEO-fixed | Business intent, CEO decision 1308 | C-007, C-020 |
| S-032 | Caption pages of at least 1.2 s; only B8 pre-ticked | Business intent, CEO decision 1309 | C-009, C-020 |
| S-033 | One loopback listener; the MC-16 tool and its ffmpeg and ffprobe keep zero sockets | CEO decision 1310 item 1 | C-011, C-012, Q-001 |
| S-034 | A first-run Vietnamese setup screen writes the settings with CEO numbers not editable | CEO decision 1310 item 2 | C-020 |
| S-035 | An employee may press approve on the CEO's written instruction; the CEO's name is recorded | CEO decision 1310 item 3 | C-021 |
| S-036 | The success measure is SC-29 only | CEO decision 1310 item 4 | C-022 |
| S-037 | The approver text carries the CEO's name and, for an employee, the employee's name and the instruction; format unchanged | Orchestrator briefing, planner open question 1 answer | C-021 |
| S-038 | The installed headless shell may be used for the no-microphone test; nothing is downloaded | Orchestrator briefing, planner open question 2 answer | C-028, D-006 |
| S-039 | The operator declared the security area for this change | Task context, affected areas | C-014, C-024, C-030 |

Traceability closure: forward 39 of 39 statements mapped; backward 17 of 17 modules traced to facts or assumptions, 15 of 15 decisions to options, 22 of 22 options to constraint sets; lateral 26 of 26 risks, 15 of 15 plan steps and 6 of 6 records resolve; planner tasks T-001 to T-028 each bound by a plan step.
