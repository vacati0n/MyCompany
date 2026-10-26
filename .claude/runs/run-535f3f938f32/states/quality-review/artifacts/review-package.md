```yaml
reviewPackage:
  packageId: RP-2026-001
  reviewReference: run-535f3f938f32 quality-review; commit fc21199 (git diff cc3bac4..fc21199 -- src tests MediaCompany.slnx pilot-affiliate-vn)
  sourceInputs:
    - type: implementation-report
      reference: runs/run-535f3f938f32/states/implementation/artifacts/implementation-report.md
    - type: feature-request
      reference: runs/inputs/mc-16-feature-request.md
    - type: business-intent
      reference: runs/inputs/mc-16-business-intent.md
    - type: code-diff
      reference: git diff cc3bac4..fc21199 -- src tests MediaCompany.slnx pilot-affiliate-vn
    - type: test-evidence
      reference: inline (dotnet build and dotnet test executed in this run)
    - type: design-reference
      reference: runs/run-535f3f938f32/task-context.yaml (technical design decisions, scope criteria, Design Gate record)
  producedBy: dev-2-reviewer
  agentVersion: 1.1.0
  schemaVersion: 1.0.0
  status: complete
  verdict: approve-with-corrections
  inputDigest: sha256:bb88e8cd79d281afd004d8892d7da5a4
  contextDigest: sha256:75bb3dd26a2c83c8267d5f109eb9d6fe
```

## Metadata

- Review ID: RP-2026-001
- Reviewer: dev-2-reviewer
- Change under review: commit fc21199, the MC-16 pilot toolchain MediaCompany.Pilot with its tests, the kit 03/04 updates and the Vietnamese operator guide, against base cc3bac4
- Review date: 2026-10-27

## Review Scope

- In scope: every changed file under src/MediaCompany.Pilot (BannedTermCheck, VietnameseText, FactSheet, Script, Admission, Ledger, MediaTool, Composition, CaptionLayout, PilotSettings, Producer, Package, NetworkGuard, Program, Face, HeldData/held-data.json), both pilot csproj files, tests/MediaCompany.Pilot.Tests (FactAndScriptTests, LayoutLedgerAndKitTests, IntegrationTests, SampleRunTests, SocketTableHarness), the kit 03 and kit 04 diffs, and pilot-affiliate-vn/mc-16/vi-du-cai-dat.json
- Out of scope: the other regression suites (Capability, Deterministic, Domain, Production) were not re-run and their results stand as claimed by the implementation report; the MC16_SAMPLE_ROOT sample package and its ffprobe were not re-produced; 03-huong-dan-van-hanh.md, vi-du-bang-thong-tin.json, JobObject.cs, the full body of FaceMetrics and PilotTestEnvironment.cs were read only in part, so those parts are unreviewed and are recorded for qa; this exclusion is safe because the findings below rest on code paths read in full
- Evidence reviewed: the code diff named in the metadata block; the implementation report (Implementation Summary, Change Set, Test Evidence, Verification Results, Deviations and Tradeoffs, Open Questions); the task context carrying the technical design decisions, the scope criteria and the Design Gate record (decision record entry 1308); `dotnet build MediaCompany.slnx --no-restore` and `dotnet test tests/MediaCompany.Pilot.Tests --no-build` executed in this run; a read-only byte comparison of the disclosure sentence across held data, the test literal, kit 03 and the scope text

## Findings

| ID | Severity | Category | Location | Requirement | Finding | Correction Request | Status |
|---|---|---|---|---|---|---|---|
| `F-001` | high | correctness | src/MediaCompany.Pilot/CaptionLayout.cs:132 | Scope decision of one caption per script sentence and the design's refusal of an over-long sentence | Captions are paged as consecutive two-line pages inside the sentence window (Composition.cs:103-118); mechanism is sound but departs from the approved design and was escalated only to the architect | `CR-001` | open |
| `F-002` | high | correctness | src/MediaCompany.Pilot/FactSheet.cs:241 | CEO decision record entry 1301 item 4 (the video never claims use); kit 03 Part C; scope criterion on checklist pre-fill from checks | An override passes when it contains the value and no listed class (d) term, so "Tôi đã dùng ..." or kit 03 Part D's own counter-example "Mình dùng mỗi sáng" passes, and Package.cs:122-139 then ticks B3 | `CR-002` | open |
| `F-003` | high | correctness | src/MediaCompany.Pilot/HeldData/held-data.json:24 | Scope class (a): amounts with the currency symbol are banned; kit 03 Parts A and C; B5 pre-fill from checks | The money pattern misses "99.000 VNĐ" (vnđ matches neither vnd nor đ), "99 nghìn" and "1 triệu", yet Package.cs:122-139 ticks B5 "không nhắc giá" | `CR-003` | open |
| `F-004` | high | correctness | src/MediaCompany.Pilot/PilotSettings.cs:183 | Decision record entry 1308 items 1 and 3 (phone-screen safe area; 0.5 s clip tolerance) | Validate accepts any non-negative tolerance and any non-negative margins (lines 183-190); only the example file carries the entry-1308 values, so the report's claim that they are enforced is not supported | `CR-004` | open |
| `F-005` | medium | correctness | src/MediaCompany.Pilot/HeldData/held-data.json:24 | Scope criterion that whole-word matching does not refuse ordinary text (the "giá đỡ" intent) | The money pattern matches "1 đồng" inside "1 đồng hồ" (clock), a home-niche product fact, and refuses a legitimate sentence | `CR-005` | open |
| `F-006` | medium | correctness | src/MediaCompany.Pilot/Ledger.cs:114 | CEO decision on fresh recordings (decision record entry 1305); scope criterion on refusing a reused recording | RefuseReusedRecording consults only Current (line 67, last entry per code), so the recording of a superseded ready package can be reused for a different video code | `CR-006` | open |
| `F-007` | medium | security | src/MediaCompany.Pilot/Ledger.cs:76 | Design ledger safeguards (ledger-loss risk); secure-engineering integrity criteria | A deleted ledger or a tail-truncated chain verifies as valid and silently resets the 12/2 limits and the reuse check; package entry copies are never reconciled | `CR-007` | open |
| `F-008` | medium | security | src/MediaCompany.Pilot/Package.cs:161 | Design approval decision (a held package is refused); secure-engineering input validation | Approval trusts the editable manifest Status and VideoFile (lines 161-172, 197) with no cross-check against the ledger entry and no plain-name containment of VideoFile | `CR-008` | open |
| `F-009` | low | correctness | src/MediaCompany.Pilot/Admission.cs:200 | Scope criterion that a timing-file refusal names the first failing sentence | A line-count mismatch names the counts rather than a sentence; NumberStyles.AllowDecimalPoint parsing of non-numeric tokens was not verified | `CR-009` | open |
| `F-010` | low | maintainability | src/MediaCompany.Pilot/Producer.cs:120 | Design rationale that admitted inputs are hashed once and the hashed bytes are reused | The face is copied from disk after hashing and inputs are re-read and re-hashed at copy (lines 143-148), opening a time-of-check gap the design meant to close | `CR-010` | open |
| `F-011` | low | test-adequacy | tests/MediaCompany.Pilot.Tests/FactAndScriptTests.cs:1 | Testing-strategy rule that each changed behavior has a covering check | No test exercises VNĐ, "1 đồng hồ", a use-claim override, reuse after supersession, a deleted ledger, an edited manifest status, or settings beyond the entry-1308 values | `CR-011` | open |
| `F-012` | low | test-adequacy | tests/MediaCompany.Pilot.Tests/SocketTableHarness.cs:17 | Design zero-socket evidence by process-job membership; testing-strategy evidence fidelity | The harness samples sockets by process name every 20 ms (lines 17, 69) rather than by job membership, and SocketTelemetry zero is trivially true because no networking assembly loads | `CR-012` | open |

## Severity Summary

- Critical: 0
- High: 4
- Medium: 4
- Low: 4

## Standards and Architecture Conformance

- Coding standards: .NET engineering playbook and the repository build rules (TreatWarningsAsErrors, nullable enabled); conformant, build has 0 warnings and 0 errors; the InvariantGlobalization override to false in both pilot csproj files is justified (NFC normalization needs ICU, probed at runtime in VietnameseText.cs) and sound
- Architecture rules: clean-architecture checklist and the design's static closure (Pilot, Deterministic, Application, Domain only, no networking reference, no voice-model port implementation); conformant per NetworkGuard and Architecture.Tests 101/101; caption paging deviates from the design (F-001)
- Security criteria: secure-engineering skill; conformant for operator file-name containment (FactSheet.cs:287-313), filter injection (operator text reaches ffmpeg only through textfile with expansion=none, Composition.cs:136), the protocol whitelist on every input (MediaTool.cs:151-196), tool digest checks, cleared environment and job-bounded processes, approval void on any byte change; non-conformant on enforcement of entry-1308 rules (F-004), ledger integrity (F-007) and manifest trust (F-008)
- Exceptions requested: caption paging (F-001) requires acceptance by the architect for the design and by the product-owner, with the CEO, for the one-caption-per-sentence scope decision

## Test Adequacy Assessment

- Test evidence reviewed: confirmed in this run by `dotnet build MediaCompany.slnx --no-restore` (succeeded, 0 warnings, 0 errors; Architecture.Tests 101/101) and `dotnet test tests/MediaCompany.Pilot.Tests --no-build` (156 passed, 0 failed, 0 skipped, about 1 m 42 s, matching the claimed 125 unit plus 31 integration); claimed and not confirmed: the Capability, Deterministic, Domain and Production regression suites and the sample package ffprobe
- Coverage of changed behavior: covered are NFC and decomposed input, the disclosure bytes at start and end, the listed false-positive strings (nhất định, giá đỡ điện thoại, 1,5 lít, 220V, nhỏ hơn), one example per banned class a to g, recording at 60.0/61.0/299.9/300.0 s and two-file refusal, digest reuse, clip size and length tolerance cases, pause detection, ledger limits, chain break and supersede, approval and one-byte void, the B3/B5/B8 tick set, the safe-area pixel test over four outside regions, the guard closure, and all five verbs
- Gaps requiring new tests: "VNĐ", "nghìn" and "triệu" amounts; "1 đồng hồ"; an override claiming use; reuse of a superseded recording; a deleted or truncated ledger; an edited manifest Status or VideoFile; settings with margins or tolerance beyond the entry-1308 values; a page minimum on-screen time; a runtime equality of paged captions with the script

## Correction Requests

| ID | Addresses | Required change | Blocking | Owner |
|---|---|---|---|---|
| `CR-001` | `F-001` | Caption paging is either accepted as a recorded design and scope deviation or replaced by the approved behavior, and each page has a stated minimum on-screen time | yes | architect |
| `CR-002` | `F-002` | No script sentence that claims or implies use passes the check, and B3 is ticked only when that holds | yes | dev-1-implement |
| `CR-003` | `F-003` | Every common Vietnamese price spelling is refused, and B5 is ticked only when that holds | yes | dev-1-implement |
| `CR-004` | `F-004` | Settings outside the entry-1308 safe area or above the 0.5 s tolerance are refused at load | yes | dev-1-implement |
| `CR-005` | `F-005` | A product fact naming "đồng hồ" with a number is not refused as a price | no | dev-1-implement |
| `CR-006` | `F-006` | A recording digest recorded in any ledger entry, superseded or not, is refused for another video code | no | dev-1-implement |
| `CR-007` | `F-007` | Loss or truncation of the ledger is detected before limits or reuse are judged | no | architect |
| `CR-008` | `F-008` | Approval accepts only a package whose status and video file agree with its ledger entry and whose file name stays in the package folder | no | dev-1-implement |
| `CR-009` | `F-009` | A timing-file refusal names the first failing sentence and non-numeric values are refused | no | dev-1-implement |
| `CR-010` | `F-010` | The bytes copied into the package are the bytes that were hashed | no | dev-1-implement |
| `CR-011` | `F-011` | Each gap listed under Test Adequacy has an executed check | no | dev-1-implement |
| `CR-012` | `F-012` | The zero-socket evidence observes the tool's own processes by membership, or the residual is recorded as accepted | no | qa |

## Residual Risk

- Accepted risk: None identified.
- Unmitigated risk: after the corrections, the banned list remains a closed word list, so a novel phrasing of price or use can still pass and the CEO checklist stays the final control; the zero-socket evidence keeps a 20 ms sampling window unless CR-012 lands
- Monitoring required: the CEO's checklist review of B3 and B5 on each package; the ledger file and its previous generation in the pilot folder

## Verdict

- Decision: approve-with-corrections
- Rationale: The Stage 8 table yields approve-with-corrections with status complete because four high findings remain open while test evidence was reviewed and confirmed
- Blocking findings outstanding: F-001, F-002, F-003, F-004
- Readiness recommendation: this reviewer recommends to qa, the Review Gate owner, that the change not progress until CR-001 to CR-004 close, and that qa weigh the medium findings and the listed test gaps

## Open Questions

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| `Q-001` | Is paging the disclosure and long sentences across two-line pages accepted against one caption per sentence? | yes | product-owner | F-001, CR-001 |
| `Q-002` | Should the class (d) and class (a) lists cover implied use and every price spelling, or is the checklist the control? | no | product-owner | F-002, F-003 |
| `Q-003` | How should ledger loss be detected, given the ledger is the only cross-run record? | no | architect | F-007, CR-007 |
