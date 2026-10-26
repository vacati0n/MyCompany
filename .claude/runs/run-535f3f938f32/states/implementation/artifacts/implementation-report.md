```yaml
implementationReport:
  reportId: IR-2026-0032
  changeReference: MC-16 pilot production toolchain, run run-535f3f938f32, invocation inv-535f3f938f32-04-001
  sourceInputs:
    - type: feature-request
      reference: runs/inputs/mc-16-feature-request.md
    - type: business-intent
      reference: runs/inputs/mc-16-business-intent.md
    - type: technical-design
      reference: runs/run-535f3f938f32/states/solution-design-and-risk-assessment/artifacts/technical-design.md
  producedBy: dev-1-implement
  agentVersion: 1.1.0
  schemaVersion: 1.0.0
  status: complete
  workflowPhase: implementation
  verificationStatus: verified
  inputDigest: sha256:81957731f6a1000c6b2dee1ab15ee115
  contextDigest: sha256:f19a60b2df50db05b4d00e11d9687b71
```

## Metadata

- Report ID: IR-2026-0032
- Change reference: MC-16 pilot production toolchain, run run-535f3f938f32, invocation inv-535f3f938f32-04-001
- Workflow phase: implementation
- Status: complete
- Verification status: verified
- Review status: pending-review

## Implementation Summary

- Change intent: An operator can now turn one fact sheet, three product images, one fresh real-voice recording of at most 60.0 s and one portrait presenter clip into a ready-to-review 1080x1920 video package, offline, through five verbs (check, script, produce, approve, verify). Each refusal names its code, row, term or check. The approval binds to the video's SHA-256. The kit and a Vietnamese operator guide match the tool and decision record entries 1304 to 1308.
- Approach taken: The design's O-002 route under D-001, D-002, D-003 and D-005, with the layout of decision record entry 1308. A new console project references only MediaCompany.Deterministic and reuses its FontCoverage for glyph coverage. The installed media tool composes the video in one pass with no browser and no socket. Ladder rungs: the new project and its test project are the ones D-001 and D-011 require. FaceMetrics (C-013) exists because no delivered component exposes advance widths, which caption wrapping needs. JobObject (C-011) is a copy of the delivered media-tool job pattern because D-001 forbids referencing Production (V-005). No new package dependency was added; all packages come from Directory.Packages.props and the local cache.
- Design reference: technical-design.md decisions D-001 to D-013, delivery steps P-001 to P-009, scope A-001 to A-040, plan tasks 1 to 15 and 18 to 23, and the layout of decision record entry 1308.
- Out of scope: Plan task 16 belongs to the orchestrator. Plan tasks 14 and 17 belong to qa; this run only supplied the tests they consume. MediaCompany.Production, graphics/, wave-12/, wave-13/, MC-13 and MC-14 material and the company store database were not touched, so frozen work stays intact. No account, sign-in, purchase or publication path exists in the tool. Kit 04 item B7 keeps its wording because no accepted element names it.

## Change Set

| ID | Path | Change Type | Purpose | Design Ref |
|---|---|---|---|---|
| C-001 | MediaCompany.slnx | modified | Adds the pilot project and its test project to the solution | D-001; D-011 |
| C-002 | src/MediaCompany.Pilot/MediaCompany.Pilot.csproj | added | Console project referencing only MediaCompany.Deterministic, with held data embedded and globalization enabled for NFC | D-001; V-002 |
| C-003 | src/MediaCompany.Pilot/HeldData/held-data.json | added | Versioned held data: disclosure, overlay, frames, call-to-action list, banned classes (a) to (g), exemptions, limits, the 27 checklist items | D-004; D-007; P-004; V-003 |
| C-004 | src/MediaCompany.Pilot/HeldData.cs | added | Loads and types the held data resource | D-004; P-004 |
| C-005 | src/MediaCompany.Pilot/PilotRefusal.cs | added | Refusal and tool-failure types plus the four exit codes | API impact: distinct exits |
| C-006 | src/MediaCompany.Pilot/VietnameseText.cs | added | NFC normalization with a self-check that refuses to run when NFC is a no-op | D-004; V-002 |
| C-007 | src/MediaCompany.Pilot/BannedTermCheck.cs | added | Whole-syllable banned-term and pattern matching with exemptions, after NFC and lower-casing | D-004; scope A-005, A-007 |
| C-008 | src/MediaCompany.Pilot/FactSheet.cs | added | Reads and checks the fact sheet form (11 header fields, 1 to 3 rows, override, call-to-action choice) | D-006; scope A-001 to A-004 |
| C-009 | src/MediaCompany.Pilot/Script.cs | added | Deterministic script generation, Part E rendering and approved-script mapping check | D-007; scope A-006, A-008 to A-014 |
| C-010 | src/MediaCompany.Pilot/PilotSettings.cs | added | Operator settings with the entry-1308 safe area and minimum sizes enforced, and the pilot folder held outside the repository | D-002; D-003; D-013 |
| C-011 | src/MediaCompany.Pilot/JobObject.cs | added | Job object that ends every media-tool process the run starts | P-002; V-005 |
| C-012 | src/MediaCompany.Pilot/MediaTool.cs | added | Digest-checked media-tool and probe invocation with a file-only protocol limit on every input, a time bound and a tool log | P-002; D-005; D-013 |
| C-013 | src/MediaCompany.Pilot/FaceMetrics.cs | added | Reads advance widths and line metrics from the face, for caption wrapping | P-003; D-002 |
| C-014 | src/MediaCompany.Pilot/Face.cs | added | Admits the face by manifest digest and checks coverage of every drawn character through FontCoverage | D-010; scope A-020 |
| C-015 | src/MediaCompany.Pilot/CaptionLayout.cs | added | Safe-area boxes, the box-inside-safe-area check, measured line wrapping and caption pages | D-002; P-006; V-001 |
| C-016 | src/MediaCompany.Pilot/Admission.cs | added | Recording and clip admission rules, pause detection and the timing-file fallback | D-008; scope A-015 to A-018, A-024, A-026, A-037 |
| C-017 | src/MediaCompany.Pilot/Ledger.cs | added | Hash-chained ledger outside the repository with limits, freshness and an atomic replace that keeps the previous generation | D-003; P-005; scope A-038 to A-040; V-004 |
| C-018 | src/MediaCompany.Pilot/Composition.cs | added | One-pass 1080x1920 composition: image motion, presenter inset, overlay and caption pages | D-002; D-009; scope A-021 to A-023, A-027; V-001 |
| C-019 | src/MediaCompany.Pilot/NetworkGuard.cs | added | Static reference-closure check and the in-process socket telemetry listener | D-005; scope A-034, A-035 |
| C-020 | src/MediaCompany.Pilot/Package.cs | added | Package manifest, input digests, checklist pre-fill, approval by hash and its verification | D-012; P-007; scope A-028 to A-033 |
| C-021 | src/MediaCompany.Pilot/Producer.cs | added | Orders pre-checks before composition, writes the package or the refused record, and appends to the ledger | P-006; P-007; scope A-019 |
| C-022 | src/MediaCompany.Pilot/Program.cs | added | The five operator verbs with Vietnamese messages and the four exit codes | API impact: five verbs |
| C-023 | tests/MediaCompany.Pilot.Tests/MediaCompany.Pilot.Tests.csproj | added | Test project with Unit and Integration categories | D-011 |
| C-024 | tests/MediaCompany.Pilot.Tests/PilotTestEnvironment.cs | added | Settings and test-time fixture generation in a temporary folder outside the repository | D-011; scope A-036 |
| C-025 | tests/MediaCompany.Pilot.Tests/FactAndScriptTests.cs | added | Unit tests for the fact sheet, banned terms and the script | D-004; D-006; D-007 |
| C-026 | tests/MediaCompany.Pilot.Tests/LayoutLedgerAndKitTests.cs | added | Unit tests for the layout, timing, ledger, approval, checklist, kit and guide consistency, and the network guard | D-003; D-005; D-012 |
| C-027 | tests/MediaCompany.Pilot.Tests/SocketTableHarness.cs | added | Samples the TCP and UDP tables for watched processes during a run | D-005; P-008 |
| C-028 | tests/MediaCompany.Pilot.Tests/IntegrationTests.cs | added | Media-level tests: proof frame, admission, pauses, produced-video checks, safe-area pixels, repository media scan | P-003; D-011; scope A-016 to A-036 |
| C-029 | tests/MediaCompany.Pilot.Tests/SampleRunTests.cs | added | End-to-end sample of all five verbs and six demonstrated refusals under the socket harness | P-008; plan task 12 |
| C-030 | pilot-affiliate-vn/pilot-kit/03-mau-kich-ban-nguoi-dan-ai.md | modified | Part B row 6 call-to-action list, operator real-voice wording and the 60 s single fresh recording in Part F | Transition strategy of the API impact section; plan tasks 19 and 22; V-006 |
| C-031 | pilot-affiliate-vn/pilot-kit/04-danh-sach-duyet-dang-bai.md | modified | Item B13 names the operator's real voice | Transition strategy of the API impact section; plan task 22 |
| C-032 | pilot-affiliate-vn/mc-16/03-huong-dan-van-hanh.md | added | Vietnamese operator guide citing only the verbs and refusal codes the sample run emitted | Plan task 23 |
| C-033 | pilot-affiliate-vn/mc-16/vi-du-cai-dat.json | added | Example settings with the entry-1308 safe area, the sizes and the installed tool and face digests | Plan task 23; D-013 |
| C-034 | pilot-affiliate-vn/mc-16/vi-du-bang-thong-tin.json | added | Example fact sheet in the D-006 form | Plan task 23; D-006 |

## Test Evidence

| ID | Test | Type | Covers | Command | Result |
|---|---|---|---|---|---|
| T-001 | Whole solution builds with warnings as errors, from the local package cache only | static | C-001, C-002, C-023 | dotnet build MediaCompany.slnx --no-restore | pass |
| T-002 | Held data loads; decomposed input is normalized before matching; the held checklist equals kit 04's 27 items | unit | C-002, C-003, C-004, C-006 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit | pass |
| T-003 | Banned classes (a) to (g), money and star patterns, whole-syllable matching and exemption "nhất định"; refusal names row and term | unit | C-003, C-005, C-007 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit | pass |
| T-004 | Fact sheet refusals: header fields, row fields, row count, missing screenshot, control characters, override that changes the value, unknown members | unit | C-005, C-008 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit | pass |
| T-005 | Script order, verbatim disclosures, identical SHA-256 on repeat, call to action from the list, Part E rows, unmapped sentence refused | unit | C-009 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit | pass |
| T-006 | Settings refusals: caption size 55 or overlay size 39, and a pilot folder inside the repository | unit | C-010 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit | pass |
| T-007 | Layout: overlay at the top and captions at the bottom of the safe area; refusals for a caption box outside the safe area, an inset over a text box, a safe area too narrow for the overlay; every fixed sentence's lines fit the derived width; disclosure paged at most two lines per page | unit | C-013, C-015 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit | pass |
| T-008 | Timing: interior pauses to windows, edge silences ignored, too few pauses refused, timing file accepted or refused for a missing sentence, wrong order or an end past the recording | unit | C-016 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit | pass |
| T-009 | Ledger: a changed entry breaks the chain and is refused; 13th video, 3rd per product and repeated focus refused; reused recording refused naming the video; a re-run supersedes its code's entry | unit | C-017 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit | pass |
| T-010 | Approval: recomputed digest accepted and verified; held package refused; one changed byte refused before approval and voids it after; checklist ticks exactly B3, B5, B8, leaves 24 unticked, and leaves B8 unticked on a failed overlay check | unit | C-020 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit | pass |
| T-011 | Network guard: closure equals the four allowed assemblies; foreign assembly found; argument lists with no limit, an http limit, a URL input, a lavfi input or fewer limits than inputs refused | unit | C-012, C-019 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit | pass |
| T-012 | Kit and examples: call-to-action list equals kit 03 row 6; checklist equals kit 04 item for item; example settings carry the entry-1308 values; example fact sheet accepted | unit | C-003, C-030, C-031, C-033, C-034 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit | pass |
| T-013 | Guide: a command line for each of the five verbs only; every listed refusal code exists in source; kit 03 Part F and guide state the 60 s ceiling and the real voice | unit | C-030, C-032 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit | pass |
| T-014 | Recording 60.0 s accepted; 61.0 s and 299.9 s refused as too long; 300.0 s refused at the upload ceiling; two files refused; reused digest refused naming the earlier video | integration | C-012, C-016, C-017 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Integration | pass |
| T-015 | Clip: landscape refused; differences of 1.0 s refused naming the seconds; 0.4 s accepted both ways; tool digest mismatch refused before any start | integration | C-011, C-012, C-016 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Integration | pass |
| T-016 | Pause detection gives 5 windows within 0.1 s on a paused recording; a continuous tone stops with pauses-not-found | integration | C-016 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Integration | pass |
| T-017 | Proof frame: all 12 vowels with 6 tones in both cases, đ/Đ and the overlay drawn at 1080x1920 in the admitted face, every character covered | integration | C-013, C-014 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Integration | pass |
| T-018 | Safe area in pixels: overlay and every page of the disclosure and both call-to-action entries drawn on black by the composition's text layer; all 50 frames stay black outside the safe area | integration | C-015, C-018 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Integration | pass |
| T-019 | Landscape target stops before composition: no package, refused record names frame-not-vertical | integration | C-010, C-021 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Integration | pass |
| T-020 | Produced video: ready; 1080x1920 at 9:16; motion in each image interval; presenter in every 0.5 s sample; overlay in every 0.5 s sample and the last frame; captions only inside their windows | integration | C-018, C-021 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Integration | pass |
| T-021 | Produced package: caption list equals the script; every sentence has a label and source; drawn characters covered; video and input digests recomputed equal; ledger entry hash valid; B3, B5, B8 ticked | integration | C-014, C-017, C-020, C-021 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Integration | pass |
| T-022 | Run opened no socket in process; tool log holds only ffmpeg and ffprobe with a protocol limit per input; zero model or speech names in the dependency list and tool log | integration | C-012, C-019, C-021 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Integration | pass |
| T-023 | Repository holds no media file under the three MC-16 folders; socket harness positive control sees a bound loopback socket | integration | C-024, C-027, C-028 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Integration | pass |
| T-024 | Sample run, built executable: check, script, produce, approve and verify succeed. Refusals shown: banned-term, recording-too-long, recording-reused, clip-not-portrait, clip-length-mismatch, and approval-void plus approval-hash-mismatch after a one-byte change. Zero socket-table rows across 1197 samples | end-to-end | C-022, C-024, C-025, C-026, C-027, C-029 | MC16_SAMPLE_ROOT=C:\Users\vuhoangcao\MediaCompanyRun\pilot-sample dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter FullyQualifiedName~SampleRunTests | pass |
| T-025 | Proof frame written into the sample folder | end-to-end | C-028 | MC16_SAMPLE_ROOT=C:\Users\vuhoangcao\MediaCompanyRun\pilot-sample dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter FullyQualifiedName~TheProofFrame | pass |
| T-026 | Sample video probed: h264, 1080x1920, yuv420p | end-to-end | C-018, C-029 | ffprobe -v error -select_streams v:0 -show_entries stream=width,height,codec_name,pix_fmt -of csv=p=0 pilot/runs/*/video.mp4 | pass |
| T-027 | Architecture regression, including the source scan over the new project: 101 of 101 | regression | C-001, C-002 | dotnet test tests/MediaCompany.Architecture.Tests --no-build | pass |
| T-028 | Capability regression: 90 of 90 | regression | C-001 | dotnet test tests/MediaCompany.Capability.Tests --no-build | pass |
| T-029 | Deterministic regression, including the reused FontCoverage: 453 of 453 | regression | C-001, C-014 | dotnet test tests/MediaCompany.Deterministic.Tests --no-build | pass |
| T-030 | Domain regression: 48 of 48 | regression | C-001 | dotnet test tests/MediaCompany.Domain.Tests --no-build | pass |
| T-031 | Production regression: 132 passed and 1 skipped of 133, the same skip as the baseline | regression | C-001 | dotnet test tests/MediaCompany.Production.Tests --no-build | pass |

## Verification Results

- Verification method: Built the solution offline. Ran the new test project by category: Unit starts no process; Integration generates every media fixture at test time in a temporary folder outside the repository. Ran the end-to-end sample through the built executable into C:\Users\vuhoangcao\MediaCompanyRun\pilot-sample under the socket-table harness, and probed the video. Re-ran the five Docker-free test projects against the baseline taken at context loading.
- Commands executed: `dotnet build MediaCompany.slnx --no-restore`; `dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit`; `dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Integration`; `MC16_SAMPLE_ROOT=C:\Users\vuhoangcao\MediaCompanyRun\pilot-sample dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter FullyQualifiedName~SampleRunTests`; `MC16_SAMPLE_ROOT=C:\Users\vuhoangcao\MediaCompanyRun\pilot-sample dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter FullyQualifiedName~TheProofFrame`; `ffprobe -v error -select_streams v:0 -show_entries stream=width,height,codec_name,pix_fmt -of csv=p=0 pilot/runs/*/video.mp4`; `dotnet test tests/MediaCompany.Architecture.Tests --no-build`; `dotnet test tests/MediaCompany.Capability.Tests --no-build`; `dotnet test tests/MediaCompany.Deterministic.Tests --no-build`; `dotnet test tests/MediaCompany.Domain.Tests --no-build`; `dotnet test tests/MediaCompany.Production.Tests --no-build`
- Result summary: 31 evidence entries executed, 31 passed, 0 failed. The build had 0 warnings and 0 errors. Pilot Unit: 125 of 125. Pilot Integration: 31 of 31, including the sample and proof tests. Regression: Architecture 101/101, Capability 90/90, Deterministic 453/453, Domain 48/48, Production 132 passed and 1 skipped of 133, each equal to its baseline. Sample video: video.mp4 in the one package folder under C:\Users\vuhoangcao\MediaCompanyRun\pilot-sample\pilot\runs. Its SHA-256 is 00723d00af70d97f8607efc37b8763a8847c3f2f237a3b6c4c120854727b0163, identical across two clean regenerations. It probes at 1080x1920 and 14.400 s with audio. Network guard: static closure passed, 0 in-process socket events, and 0 socket-table rows across 1197 samples. The extracted frame is frame.png in the package folder; the proof frame is proof-frame.png in the sample folder.
- Unverified areas: none

## Deviations and Tradeoffs

| ID | Deviation | Design element | Rationale | Escalation |
|---|---|---|---|---|
| V-001 | Captions are paged: a sentence longer than two lines is shown as consecutive pages of at most two lines inside its own window, with time split by character count (C-015, C-018) | The design's caption assumption of at most two lines per sentence, under D-002 and P-006 | Measured on the face at 56 px in the 792 px line of the entry-1308 safe area, the fixed disclosure sentence wraps to four lines. Refusing it would stop every run, and no override exists for a fixed sentence. A smaller size would relax the CEO's minimum | Raised: Q-001 to the architect as a design-infeasible element; the CEO's acceptance is routed through the orchestrator |
| V-002 | Both pilot projects set InvariantGlobalization to false, overriding Directory.Build.props, and the normalizer refuses to run when NFC is a no-op (C-002, C-006, C-023) | D-004 matching after NFC; repository build standard | Measured: under invariant globalization, string normalization returned decomposed input unchanged, so a decomposed banned term passed the check | not-required |
| V-003 | Class (c) also holds "sức khoẻ" and "an toàn cho sức khoẻ", the old-style tone placement of the listed terms (C-003) | D-004: classes exactly as scope criterion 5 lists them | The two spellings are different strings after NFC, so the listed term alone lets the common variant through | Raised: Q-002 to the product owner |
| V-004 | A package produced in held status gets no ledger entry; only ready packages are counted and are compared for freshness (C-017, C-021) | D-003, each package holds a copy of its entry | A held package cannot be approved or published. Counting it would use up one of the 12 videos and lock its recording against a corrected re-run | Raised: Q-003 to the architect |
| V-005 | The media-tool job pattern is copied into the pilot project instead of reused (C-011) | Reuse of the delivered media-tool invoker | D-001 forbids referencing Production, where the delivered pattern lives; Deterministic does not hold it | not-required |
| V-006 | Kit 03 also changes the header bullet, the Part B column heading, Part C's permitted list and the Part D worked example, besides row 6 and Part F (C-030) | Transition strategy: row 6 and Part F only | The worked example still spoke the former price sentence, which the tool now refuses as unmapped. The header and the column named the CEO alone as the reader, contrary to decision record entry 1307 | Raised: Q-004 to the product owner |

## Boundary Compliance

- Module boundaries preserved: MediaCompany.Pilot references only MediaCompany.Deterministic. Its closure is exactly MediaCompany.Application, MediaCompany.Deterministic, MediaCompany.Domain and MediaCompany.Pilot, checked by T-011. No Production, Persistence, Capability, Credentials, Host or networking reference. The Architecture suite passed unchanged (T-027).
- Public interface changes: New console verbs check, script, produce, approve and verify, with exit codes 0 done, 1 usage, 2 refused and 3 tool failed. New JSON forms for settings, fact sheet, package manifest, approval record and ledger. No existing signature, schema or contract changed.
- Data or migration impact: None in the company store. The ledger is a new file, pilot-ledger.json, with its previous generation pilot-ledger.previous.json, in the configured pilot folder outside the repository.
- Declared side effects: All 34 files of the change set, C-001 to C-034, plus this artifact and the result envelope at runs/run-535f3f938f32/states/implementation/. Runs also wrote outside the repository, to the sample folder C:\Users\vuhoangcao\MediaCompanyRun\pilot-sample and to temporary test folders under the system temporary directory. Build outputs went to the bin and obj folders.

## Residual Risk

| ID | Risk | Likelihood | Impact | Mitigation in place |
|---|---|---|---|---|
| R-001 | Pause detection is proven only on generated tones; breaths or room noise in a real voice may give more or fewer interior pauses | medium | The run stops before captioning | pauses-not-found refusal; timing.txt fallback validated per sentence; guide sections 5 and 6 |
| R-002 | Class (f) comparisons beyond the listed terms cannot be machine-detected | medium | A comparative claim could reach the CEO's review | Checklist items outside B3, B5 and B8 stay unticked for the CEO; Part E source lines per sentence |
| R-003 | Shaped text may draw slightly wider than the unkerned measured width | low | A line could touch the box edge | 24 px padding inside each box; the T-018 pixel test on the longest fixed sentences; unmapped characters counted at the widest advance |
| R-004 | The real lip-synced presenter clip is represented only by a code-generated portrait stand-in | medium | Frame rate, rotation or audio in a real export may differ from the stand-in | Portrait check on displayed size including rotation; 0.5 s length tolerance; the recording is always the sound track |
| R-005 | The socket-table sampler polls every 20 ms and could miss a shorter-lived socket | low | A brief connection might go unseen by the harness | In-process telemetry listener; file-only protocol limit on every input; static closure check |

## Handoff Notes

- Reviewer focus areas: The caption paging in CaptionLayout.Pages and Composition.TextLayer (V-001, Q-001). The held-data class (c) variants (V-003, Q-002). Held packages kept out of the ledger in Producer and Ledger (V-004). The copied job pattern (V-005). The kit 03 edits beyond row 6 and Part F (V-006). The protocol-limit check in MediaTool.RequireLocalInputs (R-005).
- Follow-up work: Answer Q-001 to Q-004; the first real publication should wait for the answer to Q-001, because paging is what the CEO will see on screen. Kit 04 item B7 still names only the CEO's recording and may need the same operator wording as B13. A first real-voice recording should be run to confirm the pause settings (R-001). Plan tasks 14 and 17 (qa) can consume T-002 to T-026 and the sample folder.
- Documentation impact: New operator guide pilot-affiliate-vn/mc-16/03-huong-dan-van-hanh.md and example settings and fact sheet. Kit 03 row 6 and Part F and kit 04 B13 now match the tool. The guide quotes the sample package path, which changes on every sample regeneration.

## Open Questions

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| Q-001 | Is paging a long caption sentence into consecutive two-line pages within its window acceptable, given that the disclosure needs four lines at 56 px in the entry-1308 safe area? | non-blocking | architect | C-015, C-018 |
| Q-002 | Should class (c) keep the old-style spellings "sức khoẻ" and "an toàn cho sức khoẻ" beside the listed terms? | non-blocking | product-owner | C-003 |
| Q-003 | May a held package stay outside the ledger, so that only ready packages count toward the limits and freshness, with no entry copy in a held package? | non-blocking | architect | C-017, C-021 |
| Q-004 | Are the kit 03 edits to the header bullet, the Part B column heading, Part C and the Part D example accepted alongside row 6 and Part F? | non-blocking | product-owner | C-030 |
