```yaml
implementationReport:
  reportId: IR-2026-0033
  changeReference: MC-16 pilot production toolchain corrections, run run-535f3f938f32, invocation inv-535f3f938f32-04-002
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

- Report ID: IR-2026-0033
- Change reference: MC-16 pilot production toolchain corrections, run run-535f3f938f32, invocation inv-535f3f938f32-04-002
- Workflow phase: implementation
- Status: complete
- Verification status: verified
- Review status: pending-review

## Implementation Summary

- Change intent: This attempt closes the twelve correction requests of the attempt-1 review, numbered 1 to 12, on top of the attempt-1 code. The attempt-1 report, IR-2026-0032, stays unchanged and still describes the toolchain as a whole. The corrections are these. Each caption page stays on screen for at least the configured 1.2 s, per decision record entry 1309. The tool pre-ticks only B8; B3 and B5 are left for the CEO, with a note that the tool's check found nothing. An override that speaks in the first person or claims use is refused. Every common price spelling is refused, and "đồng hồ" is not treated as money. A settings file that loosens decision record entry 1308 or 1309 is refused when it is loaded. A recording used by any ledger entry, superseded entries included, is refused for another video code. The ledger is reconciled each time it is read. Approval must agree with the ledger, and the video file must be a plain name. A bad timing file is refused naming the first failing sentence. The package holds and composes from the exact bytes that were hashed. The socket harness watches processes by job membership.
- Approach taken: Each correction is a narrow change inside the attempt-1 components, with no new project, component or package dependency. New limits come from the held data, so the CEO values live in one versioned place: the safe-area minimum, the clip tolerance maximum, the caption page minimum and the first-person list. Settings are compared with those values. Ledger reconciliation compares the current chain with its previous generation and with every package's entry copy, so a deleted, truncated or replaced ledger stops the run before limits or reuse are judged. Producer reads each input once and stages those bytes into the package's inputs folder. It refuses the run if the staged bytes differ from the admitted hash, and the media tool composes from the staged copies. The harness keeps no name list. It owns a job object, assigns each toolchain process to it, and reads the job's process list on every sample.
- Design reference: technical-design.md decisions D-002, D-003, D-004, D-005, D-006, D-008, D-011 and D-012; delivery steps P-005 to P-008; decision record entries 1308 and 1309; the attempt-1 review's correction requests 1 to 12.
- Out of scope: Correction requests 1 and 7 went to the architect and 12 went to qa. This attempt carries out what decision record entry 1309 (for request 1) and the coordinator's instructions (for requests 7 and 12) set. It does not decide the design. Plan tasks 14, 16 and 17 belong to other roles. MediaCompany.Production, graphics/, wave-12/, wave-13/, MC-13 and MC-14 material and the company store database were not touched. Kit 03 and kit 04 did not change in this attempt.

## Change Set

| ID | Path | Change Type | Purpose | Design Ref |
|---|---|---|---|---|
| C-001 | src/MediaCompany.Pilot/HeldData/held-data.json | modified | Money pattern for đ, đồng, vnđ, vnd, k, nghìn, ngàn, triệu, tr, ₫, $ and usd, excluding "đồng hồ" and number-unit forms such as "1,5 lít" and "220V". Adds the CEO minimums (safe area 60/180/200/480, clip tolerance 0.5 s, caption page 1.2 s) and the first-person list. The checklist pre-ticks only B8; B3 and B5 carry notes | D-004; D-012; decision record entries 1308 and 1309 |
| C-002 | src/MediaCompany.Pilot/HeldData.cs | modified | Types the new held values and the noted checklist items | D-004 |
| C-003 | src/MediaCompany.Pilot/PilotSettings.cs | modified | Adds the caption page minimum setting. On load, refuses any setting looser than the CEO value, naming it: caption size, overlay size, the four safe-area sides, clip tolerance (NaN included) and page minimum | D-002; D-013; decision record entries 1308 and 1309 |
| C-004 | src/MediaCompany.Pilot/CaptionLayout.cs | modified | Gives every page at least the minimum time and splits the remaining time by characters. Refuses a window too short for its pages, naming the sentence. Adds the rejoin of pages into the sentence | D-002; P-006; decision record entry 1309 |
| C-005 | src/MediaCompany.Pilot/Admission.cs | modified | Reads the timing file once and returns its bytes. Names the first failing sentence for a bad, missing or non-finite value, and names any extra line | D-008 |
| C-006 | src/MediaCompany.Pilot/BannedTermCheck.cs | modified | Matches first-person and use-claim terms as whole syllables after NFC | D-004 |
| C-007 | src/MediaCompany.Pilot/FactSheet.cs | modified | Refuses an override that contains a first-person or use-claim term, naming the word | D-006; D-004 |
| C-008 | src/MediaCompany.Pilot/Face.cs | modified | The admitted face carries the bytes that were hashed | D-010; D-012 |
| C-009 | src/MediaCompany.Pilot/Producer.cs | modified | Reads each input once and stages its hashed bytes into the package inputs folder, refusing the run if they differ from the admitted hash. Composes from the staged copies. Checks at run time that the pages rejoin into the script. Takes the frame from the disclosure's first page | P-006; P-007; D-012 |
| C-010 | src/MediaCompany.Pilot/Ledger.cs | modified | Reconciles the ledger on every read, against its previous generation and every package entry copy. Refuses reuse of a recording found in any entry | D-003; P-005 |
| C-011 | src/MediaCompany.Pilot/Package.cs | modified | Checklist renders B3 and B5 for the CEO with the tool's note. Approval and verification require a plain video file name and agreement with the package's ledger entry and the pilot ledger | D-012; P-007 |
| C-012 | pilot-affiliate-vn/mc-16/vi-du-cai-dat.json | modified | Example settings carry the 1.2 s caption page minimum | D-013; decision record entry 1309 |
| C-013 | pilot-affiliate-vn/mc-16/03-huong-dan-van-hanh.md | modified | Operator guide: the no-loosening rule, the price spellings, the override rule, B8-only pre-tick, inputs folder, ledger agreement, page timing, timing-file rules, the 14 refusal codes, reuse and reconciliation. The sample path and digest are updated | Plan task 23; decision record entry 1309 |
| C-014 | tests/MediaCompany.Pilot.Tests/PilotTestEnvironment.cs | modified | Test settings carry the caption page minimum | D-011 |
| C-015 | tests/MediaCompany.Pilot.Tests/LayoutLedgerAndKitTests.cs | modified | Approval fixture now lives under a pilot ledger. Updated expectations: checklist B8 only, the settings refusal code, the timing-file sentence name and the guide's code list | D-011; D-012 |
| C-016 | tests/MediaCompany.Pilot.Tests/IntegrationTests.cs | modified | 3.0 s spoken sentences. New tests for the page minimum, the run-time rejoin, the disclosure frame and composition from hashed bytes. Safe-area windows are updated. The harness control now works through job membership | D-011; P-008 |
| C-017 | tests/MediaCompany.Pilot.Tests/SampleRunTests.cs | modified | Each started process joins the harness job. Adds the refusals caption-page-too-short and package-file-not-plain. The transcript lists the job members; the test asserts that the media tools and the pilot ran inside the job | P-008; plan task 12 |
| C-018 | tests/MediaCompany.Pilot.Tests/SocketTableHarness.cs | modified | Rewritten to sample sockets owned by the members of a job object, with no name matching | D-005; P-008 |
| C-019 | tests/MediaCompany.Pilot.Tests/CorrectionTests.cs | added | Unit tests for correction requests 2 to 9 and 11: money spellings, first-person overrides, CEO minimums, page minimum, timing-file naming, ledger reconciliation, approval agreement, example settings | D-011 |

## Test Evidence

| ID | Test | Type | Covers | Command | Result |
|---|---|---|---|---|---|
| T-001 | Whole solution builds with warnings as errors, from the local package cache only, 0 warnings | static | C-001, C-002, C-003, C-004, C-005, C-006, C-007, C-008, C-009, C-010, C-011, C-019 | dotnet build MediaCompany.slnx --no-restore | pass |
| T-002 | MoneySpellingTests: 15 price spellings are found as class (a). 7 non-money facts are not found, among them "1,5 lít", "220V" and "1 đồng hồ". A fact row "1 đồng hồ treo tường" is accepted | unit | C-001, C-006 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit | pass |
| T-003 | FirstPersonOverrideTests: 9 overrides with a first-person or use-claim word are refused naming the word, among them "Mình dùng mỗi sáng" and "Tôi đã dùng". 3 sentences whose words only contain such a syllable pass. The held list equals the decided list | unit | C-001, C-006, C-007 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit | pass |
| T-004 | SettingsCeoMinimumTests: 10 looser settings refused naming each (caption size, overlay size, safe-area left, right, top and bottom, clip tolerance 0.6 and NaN, page minimum 1.1 and NaN). Stricter values accepted. The CEO values accepted. A looser file refused when read | unit | C-001, C-002, C-003 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit | pass |
| T-005 | Caption page minimum tests: in 5 windows every page stays at least the minimum and the pages fill the window. Too-short windows refused naming sentence 1 and sentence 4. The pages of each fixed sentence and of a generated script rejoin word for word | unit | C-004 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit | pass |
| T-006 | TimingFileNamingTests: 10 malformed values refused naming their sentence, among them NaN, Infinity and the infinity sign. A missing line names the first sentence without one. An earlier bad line is named before a count mismatch. An extra line is named | unit | C-005 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit | pass |
| T-007 | LedgerReconciliationTests: a superseded entry's recording refused for another code; normal reopen after each write. Refused: a deleted ledger, a ledger deleted with its previous generation, truncation below the previous generation, tail truncation caught by a package copy, and a replaced chain | unit | C-010 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit | pass |
| T-008 | ApprovalLedgerAgreementTests: refused: a held package edited to ready, 6 non-plain video file names, a renamed video, a missing entry copy, an edited copy, a pilot ledger lacking the entry, a moved package. An agreeing package is approved and verified | unit | C-011 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit | pass |
| T-009 | Checklist ticks only B8, leaves 26 unticked and notes B3 and B5. The guide lists the 14 refusal codes, each present in source. Example settings carry the page minimum. The approval fixture under the pilot ledger passes | unit | C-011, C-012, C-013, C-014, C-015 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit | pass |
| T-010 | Pilot Unit category as a whole, attempt-1 tests included: 216 of 216 | regression | C-014, C-015, C-019 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit | pass |
| T-011 | Produced video: every caption page stays at least 1.2 s on screen and the disclosure is paged. The pages were checked at run time to rejoin into the script | integration | C-004, C-009, C-016 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Integration | pass |
| T-012 | Package frame.png is taken during the disclosure's first page. The video is composed from the package's inputs copies, whose bytes equal the hashed bytes | integration | C-008, C-009, C-016 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Integration | pass |
| T-013 | Safe area in pixels with the paged call-to-action: all 60 frames stay black outside the safe area. The produced package checklist ticks only B8 | integration | C-004, C-011, C-016 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Integration | pass |
| T-014 | Socket harness positive control: a socket opened by a job member (the test process joined to the job) is seen | integration | C-018 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Integration | pass |
| T-015 | Pilot Integration category as a whole, attempt-1 tests included: 35 of 35 | regression | C-009, C-016, C-018 | dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Integration | pass |
| T-016 | End-to-end sample through the built executable into the cleared sample folder. Check, script, produce, approve and verify succeed. Refusals shown: banned-term, recording-too-long, recording-reused, clip-not-portrait, clip-length-mismatch, caption-page-too-short, package-file-not-plain, and approval-void plus approval-hash-mismatch after a one-byte change. Harness: 1095 samples, 73 job members (pilot, media tool, probe tool, console hosts), 0 rows | end-to-end | C-009, C-010, C-011, C-017, C-018 | MC16_SAMPLE_ROOT=C:\Users\vuhoangcao\MediaCompanyRun\pilot-sample dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter FullyQualifiedName~SampleRunTests | pass |
| T-017 | Proof frame written into the sample folder | end-to-end | C-016 | MC16_SAMPLE_ROOT=C:\Users\vuhoangcao\MediaCompanyRun\pilot-sample dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter FullyQualifiedName~TheProofFrame | pass |
| T-018 | Sample video probed: h264, 1080x1920, yuv420p, 17.400 s | end-to-end | C-009, C-017 | ffprobe -v error -select_streams v:0 -show_entries stream=codec_name,width,height,pix_fmt:format=duration -of csv=p=0 pilot/runs/*/video.mp4 | pass |
| T-019 | Architecture regression: 101 of 101 | regression | C-001, C-019 | dotnet test tests/MediaCompany.Architecture.Tests --no-build | pass |
| T-020 | Capability regression: 90 of 90 | regression | C-001 | dotnet test tests/MediaCompany.Capability.Tests --no-build | pass |
| T-021 | Deterministic regression: 453 of 453 | regression | C-008 | dotnet test tests/MediaCompany.Deterministic.Tests --no-build | pass |
| T-022 | Domain regression: 48 of 48 | regression | C-001 | dotnet test tests/MediaCompany.Domain.Tests --no-build | pass |
| T-023 | Production regression: 132 passed and 1 skipped of 133, the same skip as the baseline | regression | C-001 | dotnet test tests/MediaCompany.Production.Tests --no-build | pass |

## Verification Results

- Verification method: The solution was built offline. The pilot test project ran by category. The sample folder was cleared and the end-to-end sample re-run through the built executable, with every started process assigned to the harness job. The sample video was hashed and probed, and its frame.png was viewed. The five Docker-free test projects were re-run against the baseline.
- Commands executed: `dotnet build MediaCompany.slnx --no-restore`; `dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Unit`; `dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter Category=Integration`; `MC16_SAMPLE_ROOT=C:\Users\vuhoangcao\MediaCompanyRun\pilot-sample dotnet test tests/MediaCompany.Pilot.Tests --no-build --filter "FullyQualifiedName~SampleRunTests|FullyQualifiedName~TheProofFrame"`; `ffprobe -v error -select_streams v:0 -show_entries stream=codec_name,width,height,pix_fmt:format=duration -of csv=p=0 pilot/runs/*/video.mp4`; `sha256sum pilot/runs/*/video.mp4`; `dotnet test tests/MediaCompany.Architecture.Tests --no-build`; `dotnet test tests/MediaCompany.Capability.Tests --no-build`; `dotnet test tests/MediaCompany.Deterministic.Tests --no-build`; `dotnet test tests/MediaCompany.Domain.Tests --no-build`; `dotnet test tests/MediaCompany.Production.Tests --no-build`
- Result summary: 23 evidence entries executed, 23 passed, 0 failed. Build: 0 warnings, 0 errors. Pilot Unit: 216 of 216. Pilot Integration: 35 of 35. Total pilot tests: 251, up from 156 in attempt 1. Regression, each equal to its baseline: Architecture 101/101, Capability 90/90, Deterministic 453/453, Domain 48/48, Production 132 passed and 1 skipped of 133. Sample video: pilot/runs/*/video.mp4 in the one package folder under C:\Users\vuhoangcao\MediaCompanyRun\pilot-sample. Its SHA-256 is c13e81d43bc6cbf0ba5d94712b5cf90e2e467312b829d04cb02801b75abd3c29. Probe output: `h264,1080,1920,yuv420p`, duration 17.400000 s. Caption pages: the opening disclosure runs 0 to 1.597 s and 1.597 to 3.0 s; sentences 2 to 4 have one 3.0 s page each; the closing sentence runs 14.4 to 15.997 s and 15.997 to 17.4 s. The shortest page is 1.403 s. frame.png is taken at 0.798 s and shows the disclosure's first page inside the caption box, with the overlay at the top. The package checklist ticks B8 only; B3 and B5 read "CEO (công cụ đã soát)" and are unticked, each with the tool's note.
- Unverified areas: none

## Deviations and Tradeoffs

| ID | Deviation | Design element | Rationale | Escalation |
|---|---|---|---|---|
| V-001 | Captions stay paged, now with a configured minimum of 1.2 s per page. A sentence whose window cannot hold its pages is refused, naming the sentence (C-003, C-004, C-009) | The design's one caption of at most two lines per sentence, under D-002 and P-006 | Decision record entry 1309 accepted paging with this minimum; it replaces the attempt-1 open question | Raised in attempt 1 to the architect; settled by decision record entry 1309 |
| V-002 | The pre-filled checklist ticks only B8. B3 and B5 stay unticked with the tool's note (C-001, C-011) | D-012 checklist pre-fill from checks | Decision record entry 1309: the banned list is a closed word list, so the tool cannot assert those items | Settled by decision record entry 1309 |
| V-003 | First-person words in an override are matched as whole syllables. "anh" and "chị" are on the decided list, so an override that uses either as a common noun is refused too (C-001, C-006, C-007) | D-004 matching rules | The coordinator set the list and whole-syllable matching for correction request 2. Matching inside longer words would refuse ordinary text such as "kem" | not-required |
| V-004 | Deleting the whole pilot folder, packages included, cannot be detected; reconciliation works only from what remains in that folder (C-010) | D-003 ledger safeguards | The ledger is the only cross-run record. Any further anchor would be a new store, which is a design decision | Recorded as a residual for the architect under correction request 7 |

## Boundary Compliance

- Module boundaries preserved: MediaCompany.Pilot still references only MediaCompany.Deterministic. The attempt-1 closure check still passes in the Unit category, and the Architecture suite passed unchanged (T-019). The test harness uses operating-system job and table calls in the test project only.
- Public interface changes: Seven new refusal codes: override-first-person, caption-page-too-short, settings-looser-than-ceo, ledger-truncated, ledger-entry-missing, package-file-not-plain and package-ledger-mismatch. The tool also has internal codes for a staging difference (input-changed) and a page-rejoin difference (caption-text-changed). The settings JSON form requires a new member for the caption page minimum. A package now holds an inputs folder. The five verbs and the four exit codes are unchanged.
- Data or migration impact: None in the company store. A settings file without the caption page minimum member is refused until the operator adds it; the guide and the example settings show it. Existing pilot ledgers are read unchanged.
- Declared side effects: The 19 files of the change set, C-001 to C-019, plus this artifact and the result envelope under runs/run-535f3f938f32/states/implementation/. Outside the repository, runs wrote to the cleared sample folder C:\Users\vuhoangcao\MediaCompanyRun\pilot-sample and to temporary test folders. Build outputs went to the bin and obj folders.

## Residual Risk

| ID | Risk | Likelihood | Impact | Mitigation in place |
|---|---|---|---|---|
| R-001 | The banned list is still a closed word list, so a new phrasing of price or use can pass the tool | medium | A use or price claim could reach the CEO's review | B3 and B5 are left for the CEO; Part E source lines per sentence; the 15 tested price spellings |
| R-002 | The harness samples every 20 ms and could miss a socket that opens and closes between samples | low | A brief connection could go unseen in the evidence | Every input is limited to the file protocol; static closure check; in-process telemetry |
| R-003 | A process the toolchain starts before it is itself assigned to the job is not a member | low | Such a process's sockets would not be counted | Assignment happens as soon as the start call returns. The pilot loads settings and checks inputs before it starts any media tool, but nothing enforces that order; the transcript shows the media tool and probe tool as job members |
| R-004 | The in-process socket telemetry reading of zero is trivially true for a process that opens no socket API, so it adds little evidence | low | Overstated evidence if read alone | The job-membership harness (T-016) is the evidence of record |
| R-005 | input-changed has no direct test, because the comparison runs inside a local function | low | A regression in staging could go unseen by a dedicated test | T-012 checks that the composed inputs equal the hashed bytes |
| R-006 | The closing sentence's pages are 1.597 s and 1.403 s, close to the 1.2 s minimum, at the sample's speaking pace | medium | A faster real voice is refused with caption-page-too-short | Refusal names the sentence; guide section 5 tells the operator to speak that sentence more slowly |
| R-007 | Deleting the whole pilot folder, packages included, is not detectable (V-004) | low | Limits and reuse could restart from zero | Guide section 8 forbids deleting or editing the pilot folder |

## Handoff Notes

- Reviewer focus areas: Ledger.Open's reconciliation order and its refusal codes (correction request 7, architect). Package.RequireLedgerAgreement and VideoPath (request 8). The staging helper in Producer (request 10). The page-time split in CaptionLayout.Pages (request 1). PilotSettings.RefuseLooserThanCeo (request 4). The job-membership harness (request 12, qa).
- Follow-up work: The attempt-1 questions on the old-style "sức khoẻ" spellings, on held packages kept out of the ledger, and on the kit 03 edits are still open; they are carried below. A first real-voice recording should confirm the pause settings and the page minimum at a natural pace (R-006).
- Documentation impact: The operator guide pilot-affiliate-vn/mc-16/03-huong-dan-van-hanh.md now covers sections 1, 3, 4, 5, 6, 7 and 8 as listed under C-013. It quotes the new sample package path and digest, which change on every sample regeneration.

## Open Questions

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| Q-001 | Should class (c) keep the old-style spellings "sức khoẻ" and "an toàn cho sức khoẻ" beside the listed terms? Carried from attempt 1 | non-blocking | product-owner | C-001 |
| Q-002 | May a held package stay outside the ledger, so that only ready packages count toward the limits, with no entry copy in a held package? Approval now refuses any package without a ledger entry. Carried from attempt 1 | non-blocking | architect | C-010, C-011 |
| Q-003 | Are the attempt-1 kit 03 edits to the header bullet, the Part B column heading, Part C and the Part D example accepted alongside row 6 and Part F? Carried from attempt 1 | non-blocking | product-owner | C-013 |
| Q-004 | Is the residual of deleting the whole pilot folder (V-004, R-007) accepted, or should the ledger gain an anchor outside that folder? | non-blocking | architect | C-010 |
