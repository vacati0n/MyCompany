```yaml
reviewPackage:
  packageId: RP-2026-002
  reviewReference: run-022d0492a75a quality-review; commit bb5adb4 (src/MediaCompany.Pilot.Web, tests/MediaCompany.Pilot.Web.Tests, MediaCompany.slnx)
  sourceInputs:
    - type: implementation-report
      reference: runs/run-022d0492a75a/states/implementation/artifacts/implementation-report.md
    - type: feature-request
      reference: runs/inputs/mc-17-feature-request.md
    - type: business-intent
      reference: runs/inputs/mc-17-business-intent.md
    - type: code-diff
      reference: git show bb5adb4 -- src/MediaCompany.Pilot.Web tests/MediaCompany.Pilot.Web.Tests MediaCompany.slnx
    - type: test-evidence
      reference: inline (dotnet build, dotnet test and the web test program executed in this run)
    - type: design-reference
      reference: runs/run-022d0492a75a/states/solution-design-and-risk-assessment/artifacts/technical-design.md and architecture-decision-record-D-001.md to D-006.md
    - type: design-reference
      reference: runs/run-022d0492a75a/states/scope-and-acceptance/artifacts/scope-definition.md (acceptance criteria A-001 to A-030, cited as SC-01 to SC-30)
    - type: design-reference
      reference: research/ceo-decision-record.md, entries CEO-D-1310 and CEO-D-1311
  producedBy: dev-2-reviewer
  agentVersion: 1.1.0
  schemaVersion: 1.0.0
  status: complete
  verdict: approve-with-corrections
  inputDigest: sha256:444d55c234288e24a6fc286d950b2434
  contextDigest: sha256:75bb3dd26a2c83c8267d5f109eb9d6fe
```

## Metadata

- Review ID: RP-2026-002
- Reviewer: dev-2-reviewer
- Change under review: commit bb5adb4, the MC-17 local Vietnamese web app MediaCompany.Pilot.Web over the MC-16 toolchain, its zero-package test program MediaCompany.Pilot.Web.Tests and the two MediaCompany.slnx entries
- Review date: 2026-10-27

## Review Scope

- In scope: the web app sources Program.cs, RequestGuard.cs, PageAssets.cs, WebContext.cs, Routes.cs, MediaSteps.cs, FactSteps.cs, Workspace.cs, Names.cs, ToolchainRunner.cs, ChildJob.cs, ProduceJobs.cs, Setup.cs, Outcomes.cs, ChecklistView.cs, Shortcut.cs and WebLog.cs; the page files Pages/index.html and Pages/app.js; the MediaCompany.slnx diff; the toolchain sources the app calls into (InputFiles.Resolve in FactSheet.cs, Ledger.Open in Ledger.cs, Admission.Recording and Admission.Clip in Admission.cs). Each was checked against the six focus areas: no weakening of an MC-16 check, loopback security, no external resource or generative or speech service, the recording and approval flows, plain Vietnamese refusals with a next step, and test adequacy with no media file added.
- Out of scope: these files were read only in part or not at all. AppRecord.cs was read for its file paths and its JSON read and write only. WebRefusal.cs, both project files, Pages/app.css and the test-mode driver script were not read in full. Of the test program, FlowCases.cs was read in part, and UnitCases.cs, BrowserCase.cs, Support.cs, AddressHarness.cs and Runner.cs were not read in full. Their behaviour was judged from the executed results, case by case, in the Test Adequacy section. This exclusion is safe for the verdict because the open high finding rests on production code that was read in full. Also out of scope: the regression suites other than MediaCompany.Pilot.Tests and MediaCompany.Architecture.Tests, which were not re-run, and judging whether the acceptance criteria are met, which belongs to qa.
- Evidence reviewed:
  - the code diff named in the metadata block, for the files listed under In scope;
  - the implementation report: change set, test evidence, deviations V-001 to V-008, risks and open questions;
  - the technical design, read in full: facts, assumptions, constraints, modules, options, decisions D-001 to D-015, reuse table, operational considerations, delivery plan, risks R-001 to R-026 and open decisions;
  - the decision records D-001 and D-002, read in full;
  - the scope definition's acceptance criteria A-004, A-005, A-008, A-011 and A-024;
  - CEO-D-1310 and CEO-D-1311, and the feature request and business intent supplied in the envelope;
  - the three executed results named in the Test Adequacy section.

## Findings

| ID | Severity | Category | Location | Requirement | Finding | Correction Request | Status |
|---|---|---|---|---|---|---|---|
| `F-001` | high | architecture | src/MediaCompany.Pilot.Web/MediaSteps.cs:63 and :93 | ADR D-001: in process only pure checks, settings validation, probe and conversion. Technical design reuse table: "Recording admission in process ... rejected: needs the ledger, which only the toolchain child opens". Tradeoff: "the recording reuse refusal is shown at produce". CEO-D-1311 item 1 lists what may run in process. | Both take acceptance (Convert) and recording upload (AcceptUploadedRecording) call `Admission.Recording(..., Ledger.Open(pilotFolder, held), ...)` inside the listener's process. Ledger.Open is more than a pure check: it reads the ledger and its previous generation, reconciles both against every package folder under runs, and creates the pilot folder. The approved design rejected exactly this. The CEO's list of permitted in-process work is "live banned-term check, settings validation, probe and conversion"; ledger reconciliation is not on it. The implementation report records this as V-002 with escalation "not-required", so the boundary the CEO approved at the Design Gate was changed without the architect or the CEO deciding. Mechanism: the decision is still the toolchain's own code, and produce re-checks everything. Consequence: the process boundary now differs from the approved one. The design's reason for the boundary (only the child opens the ledger) no longer holds. Produce is the only operation serialised by the app, so a step-4 reconciliation can read the ledger and runs folder while a produce child is writing them. Whether that can raise a false ledger refusal was not established in this review. No ledger write was found in the app; D-008's "never writes the ledger" holds. Urgency: before the Review Gate. | CR-001 | open |
| `F-002` | medium | standards | src/MediaCompany.Pilot.Web/FactSteps.cs:29 | Technical design SC-04 mapping "D-015 live check over every typed field". D-015. Implementation report V-007. | The live check sends only the fields the check verb scans (header subject and classification; row subject, attribute, value and override), not every typed field. This is a declared, escalated deviation; the implementer raised it to the architect, still unanswered. It is not a high finding: A-004 (a fact value) and A-005 (parity with the toolchain's check) are both met by the narrower set, and test T-004 exercises seven of seven classes. It stays open because the design text and the code disagree until the architect decides. Urgency: decide before release. | CR-002 | open |
| `F-003` | medium | standards | src/MediaCompany.Pilot.Web/Routes.cs:441 | Technical design security considerations: "Uploaded names are matched against names the fact sheet declares and then resolved with the toolchain's plain-name rule". R-009 mitigation "Declared names only, through the plain-name rule". | UploadScreenshot resolves the name with PlainName.Require and an image-extension check, but never matches it against the fact sheet's declared screenshot names. The image upload route does (image-not-declared). The ordering explains why: screenshots are attached before the sheet is saved, and the sheet check needs them present. The departure is still not among V-001 to V-008. Containment (C-024) holds: the plain-name rule refuses separators, "..", ":" and control characters and requires the parent to equal the folder, and T-014 shows a path name refused. So no security rule is violated and this is not high. Consequence: any plain image name can be written into the input folder, including one that replaces a declared product image of the same name. Urgency: next correction cycle. | CR-003 | open |
| `F-004` | medium | correctness | src/MediaCompany.Pilot.Web/Routes.cs:750 | Technical design, error handling strategy (S12): every failure reaches a defined category. D-009: the job ends with a tool-failure message. | The background produce work catches only WebRefusal, IOException, InvalidOperationException and Win32Exception. ProduceJobs.Start (ProduceJobs.cs:60) runs it as a discarded task with no outer handler. Any other exception skips ProduceJobs.Finish, so `Running` stays true, the page polls forever, and every later produce is refused produce-busy until the app restarts. Examples: a JsonException from Workspace.LoadState on a damaged per-video state file, or an UnauthorizedAccessException from SaveState. The exception is also never logged. If the produce child succeeded, a ready package exists that the app never records as LastPackage. This is a maintainability defect that compounds: the stuck state persists across all videos. Urgency: next correction cycle. | CR-004 | open |
| `F-005` | low | maintainability | src/MediaCompany.Pilot.Web/Routes.cs:968 | Technical design D-012 (preview and hash from the package the app's own latest produce wrote) | The verify route builds videoPath from the fixed PackageFiles.Video name. The checklist and approve routes (Routes.cs:886 and :952) build it from the manifest's VideoFile. Two sources for one displayed path can disagree on a package whose manifest names another file. This is display text only; the toolchain's verify decides. | CR-005 | open |
| `F-006` | low | test-adequacy | tests/MediaCompany.Pilot.Web.Tests/FlowCases.cs | Skill S07 testing strategy (every changed behaviour has a covering check). Design R-007 mitigation "start-up address check stops the app" (owner qa). | Three changed behaviours have no executed check. (1) The start-up address check in Program.cs that exits 4 when a bound address is not 127.0.0.1; T-010 covers only the passing path. (2) The produce job's path for an exception outside the caught types (F-004). (3) The shortcut-failed refusal in Shortcut.cs. A search of the test sources for an environment endpoint override, exit 4 or shortcut-failed found none. Urgency: with the next test change. | CR-006 | open |

## Severity Summary

- Critical: 0
- High: 1
- Medium: 3
- Low: 2

## Standards and Architecture Conformance

- Coding standards: applied S03 (engineering playbook) and S12 (error handling, through the design's error-handling strategy). The build gives 0 warnings and 0 errors with warnings as errors. Rendering uses textContent only, with no innerHTML or eval in app.js. Children start with ArgumentList and UseShellExecute=false. Each toolchain start is preceded by a digest check (V-006), and all children run in a kill-on-close job object. Nonconforming: the produce job's exception handling (F-004) and the double source of the displayed video path (F-005).
- Architecture rules: applied ADRs D-001 to D-006, inline decisions D-007 to D-015, and CEO-D-1310 and CEO-D-1311. Conforming:
  - the listener is bound in code to 127.0.0.1 and the bound addresses are checked at start (D-003);
  - check, script, produce, approve and verify run as a toolchain child with an allow-listed verb set (D-001), and the app has no reference to Producer or the toolchain entry point (T-008);
  - takes are converted once through MediaTool, mono 16-bit PCM, bit-exact, no metadata, capped at 60.000 s (D-002, T-005);
  - setup copies every CEO number from the example settings file and validates with PilotSettings.Validate and Face.Admit, with no input for any number (D-004, T-007, T-012);
  - the two approver forms are composed exactly as CEO-D-1311 item 4 states (D-005, T-001);
  - the zero-package test program uses the installed headless shell (D-006, T-008, T-030);
  - the checklist is read from the package and compared with the held list, and approval needs the 26 operator items ticked, checked on the server (D-012, T-021, T-022);
  - the log holds no token, name, query or content (D-014, T-028).
  Nonconforming: the in-process ledger read and recording admission (F-001), and two departures from design text (F-002, F-003).
- Security criteria: applied S09 (secure engineering) and constraints C-011, C-012, C-014, C-024 and C-030. The request guard, checked in this order:
  1. Port 0 gives 503.
  2. The peer must be loopback.
  3. Host must equal exactly 127.0.0.1 and the port; this blocks DNS rebinding.
  4. GET and HEAD pass; any other non-POST method gives 405.
  5. Every POST needs Origin exactly equal to the app's own origin, plus a per-start 32-byte token in a custom header compared in fixed time; this blocks cross-site forgery.
  No single browser-supplied property decides (C-030). Responses carry a self-only content policy, frame denial, same-origin resource and opener policies, nosniff, no-store and microphone=(self). The pages link only /app.css and /app.js, and the test driver is served only in test mode. Uploaded and stored names go through the toolchain's plain-name rule (C-024). The video code is restricted by the same rule, and LastPackage is restricted to a direct child of runs. The executed socket evidence: T-042 found zero rows for every toolchain, ffmpeg and ffprobe process, and the app's own rows were loopback only. T-011 refused six foreign requests and changed no pilot file. No generative, speech or metered component is referenced or started (C-016). Hash approval, the 60 s limit, recording reuse, safe area, banned terms, the 0.5 s clip tolerance, the 1.2 s caption page and the settings floor all stay with the toolchain: T-017, T-019, T-020, T-023, T-024, T-025 and T-026 show the toolchain's own lines through the app. None of the eight MC-16 checks was found weakened. Residual: an environment or configuration endpoint could bind for the moment before the start-up check stops the app; the peer-loopback guard still refuses any remote request in that moment (F-006 notes the check is untested).
- Exceptions requested: V-002 (F-001) needs either an amendment to D-001 and the reuse table by the architect, with the CEO deciding if the architect reads CEO-D-1311 item 1 as not covering it, or its removal. V-007 (F-002) needs the architect's decision. Neither has been granted.

## Test Adequacy Assessment

- Test evidence reviewed: three runs executed in this review.
  - `dotnet build MediaCompany.slnx`: exit 0, 0 warnings, 0 errors; it also ran MediaCompany.Architecture.Tests, 101 passed, 0 failed.
  - `dotnet test tests/MediaCompany.Pilot.Tests --no-build`: 251 passed, 0 failed, 0 skipped, 2 m 13 s. This confirms the toolchain suite is unchanged in result.
  - `tests/MediaCompany.Pilot.Web.Tests/bin/Debug/net10.0/MediaCompany.Pilot.Web.Tests.exe`: "RESULT: 31 passed, 0 failed, 31 run (282 s)", exit 0. The cases were T-001 to T-028 except T-009, plus T-030, T-040, T-041 and T-042.
  - `git status --porcelain` captured before and after the web test run was identical, so no repository file was written by the tests.
  - `git show --stat bb5adb4` lists no file with a media extension (mp4, wav, webm, ogg, m4a, png, jpg, jpeg, mov, mp3).
- Coverage of changed behavior: covered by executed cases:
  - approver text (T-001); outcome mapping of the 14 guide codes and the exit statuses (T-002); the request guard in unit (T-003) and in flow (T-011, six foreign requests, 85 pilot files unchanged);
  - live-check parity for seven classes (T-004); the conversion argument list with the 60 s cap (T-005); NFC (T-006, T-014);
  - setup (T-007, T-012); static no-package, no-external-address and no-media checks (T-008); start on 127.0.0.1 with token and headers (T-010);
  - fact sheet refusals equal to the check verb (T-013, T-014); declared images (T-015); script ticks (T-016);
  - upload at 60.0 s kept and at 61.0 s refused (T-017); conversion digest stable (T-018); clip portrait and the 0.5 s tolerance (T-019);
  - produce, timing editor, caption page and produce-busy (T-020); checklist of 27 with B8 tool-ticked and B3 and B5 noted (T-021);
  - approval at 25 ticks refused and at 26 ticks with an employee approved by hash with the decided wording (T-022); hash mismatch (T-023); verify lines (T-024);
  - recording reuse (T-025); settings looser than the CEO's (T-026); single instance (T-027); log content (T-028);
  - browser recording with a fake microphone, stopping at 60 s by itself, with playback and re-record (T-030); ledger entry missing (T-040); ledger truncated (T-041); socket tables over the whole flow (T-042).
- Gaps requiring new tests: the start-up address check's stop path (exit 4) under a non-loopback binding; the produce job's handling of an exception outside the caught types; the shortcut-failed refusal (all F-006). A take accept and a recording upload while a produce job writes the ledger (F-001), if the in-process admission is kept. These are handed to qa.

## Correction Requests

| ID | Addresses | Required change | Blocking | Owner |
|---|---|---|---|---|
| `CR-001` | `F-001` | The process boundary in the code and the approved boundary agree. Either recording admission against the ledger no longer runs in the listener's process, and the step shows what the design approved (the probe length against the held limit, with produce deciding reuse); or the architect records an amendment to D-001 and the reuse table that admits an in-process ledger read, states why a concurrent produce cannot make it refuse wrongly, and obtains the CEO's decision where CEO-D-1311 item 1 does not already cover it. | yes | architect |
| `CR-002` | `F-002` | The live check's field set and the design text agree, through the architect's decision on V-007 being recorded. | no | architect |
| `CR-003` | `F-003` | A screenshot name can be stored only if it is declared for a fact row, or the architect records the departure from the design's uploaded-name rule as accepted. | no | dev-1-implement |
| `CR-004` | `F-004` | Every exception raised by a background produce job ends that job with a shown category and a log line, so a later produce is never refused produce-busy by a job that has stopped. | no | dev-1-implement |
| `CR-005` | `F-005` | Every route that shows the package video path takes it from the same source. | no | dev-1-implement |
| `CR-006` | `F-006` | An executed check exists for each of the three uncovered behaviours named in the finding. | no | qa |

## Residual Risk

- Accepted risk: None identified. No finding has a recorded acceptance by its owning role.
- Unmitigated risk:
  - An employee can choose either "CEO tự bấm" or the employee option. The approval record carries whichever text is chosen, and the app cannot verify a written instruction; this is inherent under X-005 (no accounts) and design R-011.
  - The CEO name used in every approver text can be changed by re-running setup.
  - A browser take longer than 60.0 s is shortened to 60.000 s at conversion rather than refused, as D-002 and R-015 designed. Whether this meets A-008 is for qa.
  - An environment endpoint override would bind briefly before the start-up check stops the app.
  - If F-001 is amended rather than removed, a step-4 ledger read can run concurrently with a produce child.
- Monitoring required: the app log's produce-busy and app-fault lines (F-004); any ledger refusal raised at step 4 rather than at produce (F-001); the approval records' approver texts against the CEO's written instructions (product-owner).

## Verdict

- Decision: approve-with-corrections
- Rationale: The Stage 8 table yields approve-with-corrections because one high finding is open (F-001). The change departs from the CEO-approved process boundary without escalation, while no critical finding exists and the security guard, socket evidence and toolchain parity were confirmed by executed tests.
- Blocking findings outstanding: F-001
- Readiness recommendation: Recommended to qa, as the Review Gate owner, that the change not advance to release until CR-001 is closed by the architect, and the CEO where needed. CR-004 to CR-005 are recommended for the next correction cycle and are not blocking. This is a recommendation; the gate decision is qa's.

## Open Questions

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| `Q-001` | Is in-process recording admission with a ledger read (V-002) accepted as an amendment to D-001 and the reuse table, and if the reading of CEO-D-1311 item 1 requires it, does the CEO approve it; or is it removed? | yes | architect | F-001, CR-001, verdict |
| `Q-002` | Does A-008's clause "a take longer than 60.0 s is refused naming its length" apply to browser takes? Today D-002 caps them at conversion to 60.000 s and does not refuse them. Or does it apply only to uploads, where T-017 shows the refusal? | no | qa | acceptance validation of A-008 |
| `Q-003` | Is it accepted that the "CEO tự bấm" option is selectable by whoever operates, and that the CEO name can be changed by re-running setup, given X-005 and R-011? | no | product-owner | Residual Risk |
