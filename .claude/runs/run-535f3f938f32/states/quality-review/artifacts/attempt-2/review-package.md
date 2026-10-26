```yaml
reviewPackage:
  packageId: RP-2026-002
  reviewReference: run-535f3f938f32 quality-review attempt 2; commit e264a7a (corrections git diff fc21199..e264a7a; whole change git diff cc3bac4..e264a7a -- src tests MediaCompany.slnx pilot-affiliate-vn)
  sourceInputs:
    - type: implementation-report
      reference: runs/run-535f3f938f32/states/implementation/artifacts/attempt-2/implementation-report.md
    - type: feature-request
      reference: runs/inputs/mc-16-feature-request.md
    - type: business-intent
      reference: runs/inputs/mc-16-business-intent.md
    - type: code-diff
      reference: git diff cc3bac4..e264a7a -- src tests MediaCompany.slnx pilot-affiliate-vn
    - type: test-evidence
      reference: inline (dotnet build and dotnet test executed in this run)
    - type: design-reference
      reference: runs/run-535f3f938f32/task-context.yaml; research/ceo-decision-record.md entries 1308 and 1309
  producedBy: dev-2-reviewer
  agentVersion: 1.1.0
  schemaVersion: 1.0.0
  status: complete
  verdict: approve-with-corrections
  inputDigest: sha256:2db5c88681c0e441d8827951f5b1ee89
  contextDigest: sha256:75bb3dd26a2c83c8267d5f109eb9d6fe
```

## Metadata

- Review ID: RP-2026-002
- Reviewer: dev-2-reviewer
- Change under review: commit e264a7a, the corrections to the MC-16 pilot toolchain MediaCompany.Pilot raised by the attempt-1 review, re-reviewed together with the whole change against base cc3bac4
- Review date: 2026-10-27

## Review Scope

- In scope: every file of the correction diff (Admission, BannedTermCheck, CaptionLayout, Face, FactSheet, HeldData and held-data.json, Ledger, Package, PilotSettings, Producer, CorrectionTests, IntegrationTests, LayoutLedgerAndKitTests, PilotTestEnvironment, SampleRunTests, SocketTableHarness, vi-du-cai-dat.json, 03-huong-dan-van-hanh.md); the closure of each of the twelve attempt-1 correction requests; regressions the corrections introduced; and the files attempt 1 read only in part (JobObject.cs, FaceMetrics.cs, PilotTestEnvironment.cs, vi-du-bang-thong-tin.json, the operator guide), now read in full
- Out of scope: the decision record entry 1309 text itself (a governance record, read only as the requirement); the Capability, Deterministic, Domain and Production regression suites and the 2026-10-27 sample run, which were not re-run and stand as claimed; this exclusion is safe because the corrections touch only the pilot project and its tests, whose suite was re-run
- Evidence reviewed: the correction diff and the whole-change diff named in the metadata block; the attempt-2 implementation report (Implementation Summary, Change Set, Test Evidence, Verification Results, Deviations and Tradeoffs, Residual Risk, Open Questions); decision record entries 1308 and 1309; `dotnet build MediaCompany.slnx --no-restore` and `dotnet test tests/MediaCompany.Pilot.Tests --no-build` executed in this run; `git status` of src, tests and the kit before and after the commands (unchanged)

## Findings

| ID | Severity | Category | Location | Requirement | Finding | Correction Request | Status |
|---|---|---|---|---|---|---|---|
| `F-001` | high | correctness | src/MediaCompany.Pilot/CaptionLayout.cs:137 | Decision record entry 1309 item 1 (two-line pages, at least 1.2 s each, a short window refused naming the sentence) | Attempt-1 paging deviation closed: entry 1309 accepts paging; each page gets the minimum plus a floored share and all boundaries round alike, so no page is under 1.2 s; a short window is refused at line 159 | None identified. | resolved |
| `F-002` | high | correctness | src/MediaCompany.Pilot/FactSheet.cs:243 | CEO decision record entry 1301 item 4 (never claims use); decision record entry 1309 item 2 (B3 not pre-ticked) | Attempt-1 use-claim gap closed: an override carrying a first-person or class (d) word is refused as override-first-person, and Package.cs:131 now notes B3 without ticking it | None identified. | resolved |
| `F-003` | high | correctness | src/MediaCompany.Pilot/HeldData/held-data.json:24 | Scope class (a) amounts; decision record entry 1309 item 2 (B5 not pre-ticked) | Attempt-1 price gap closed: the pattern now matches vnđ, vnd, usd, nghìn, ngàn, triệu, tr, đ, k, ₫ and $ before or after the number, and B5 is noted, never ticked | None identified. | resolved |
| `F-004` | high | correctness | src/MediaCompany.Pilot/PilotSettings.cs:211 | Decision record entry 1308 items 1 and 3; decision record entry 1309 item 1 | Attempt-1 enforcement gap closed: RefuseLooserThanCeo refuses caption under 56 px, overlay under 40 px, any margin under 60/180/200/480, tolerance above 0.5 s or NaN, page minimum under 1.2 s | None identified. | resolved |
| `F-005` | medium | correctness | src/MediaCompany.Pilot/HeldData/held-data.json:24 | Scope criterion that whole-word matching does not refuse ordinary text | Attempt-1 "1 đồng hồ" false positive closed by a negative lookahead on "hồ"; "1 đồng" alone is still refused | None identified. | resolved |
| `F-006` | medium | correctness | src/MediaCompany.Pilot/Ledger.cs:167 | CEO decision on fresh recordings (decision record entry 1305) | Attempt-1 supersession gap closed: RefuseReusedRecording now searches every chain entry, superseded ones included | None identified. | resolved |
| `F-007` | medium | security | src/MediaCompany.Pilot/Ledger.cs:77 | Orchestrator decision on the ledger correction (reconcile with package copies; refuse a chain shorter than the previous generation) | Attempt-1 ledger-loss gap closed: Open refuses a deleted ledger, a chain not prefixed by the previous generation, and a package entry copy the chain lacks or that cannot be read | None identified. | resolved |
| `F-008` | medium | security | src/MediaCompany.Pilot/Package.cs:245 | Design approval decision (held package refused); secure-engineering input validation | Attempt-1 manifest-trust gap closed: VideoPath (line 222) requires a plain name inside the package and RequireLedgerAgreement checks copy hash, video digest, status, entry hash and chain | None identified. | resolved |
| `F-009` | low | correctness | src/MediaCompany.Pilot/Admission.cs:199 | Scope criterion that a timing-file refusal names the first failing sentence | Attempt-1 timing gap closed: lines are judged in sentence order, a missing or extra line names its sentence, and non-finite values are refused (line 217) | None identified. | resolved |
| `F-010` | low | correctness | src/MediaCompany.Pilot/HeldData/held-data.json:123 | Scope criterion that whole-word matching does not refuse ordinary text | The first-person list holds "em" and "anh", so an override naming "em bé" or "tiếng Anh" is refused; the report records this for "anh" and "chị" only | `CR-001` | open |
| `F-011` | low | maintainability | src/MediaCompany.Pilot/Producer.cs:141 | Design rationale that admitted inputs are hashed once and the hashed bytes reused | Attempt-1 re-read gap closed: the face is staged from its hashed bytes (line 135) and each input is read once, compared with its admitted digest, and composed from the package copy | None identified. | resolved |
| `F-012` | low | correctness | src/MediaCompany.Pilot/Producer.cs:209 | Design ledger safeguard of an atomic write; error-handling recovery criteria | The package entry copy is written non-atomically; an interrupted write leaves an unreadable copy, and Ledger.cs:123 then refuses every later run until the folder is repaired by hand | `CR-002` | open |
| `F-013` | low | test-adequacy | tests/MediaCompany.Pilot.Tests/CorrectionTests.cs:1 | Testing-strategy rule that each changed behavior has a covering check | Attempt-1 test gaps closed: CorrectionTests covers every listed gap (price spellings, "đồng hồ", use-claim overrides, supersession, ledger loss, edited manifest, CEO minimums, page minimum, rejoin) | None identified. | resolved |
| `F-014` | low | test-adequacy | tests/MediaCompany.Pilot.Tests/SocketTableHarness.cs:17 | Design zero-socket evidence by process-job membership | Attempt-1 harness gap closed: membership is read from a harness job (nested jobs included), never by name, with the test process as positive control; the 20 ms window stays a recorded residual | None identified. | resolved |

## Severity Summary

- Critical: 0
- High: 4
- Medium: 4
- Low: 6

## Standards and Architecture Conformance

- Coding standards: .NET engineering playbook and the repository build rules (TreatWarningsAsErrors, nullable enabled); conformant, the build has 0 warnings and 0 errors
- Architecture rules: clean-architecture checklist and the design's static closure; conformant, the pilot still references only the deterministic project, no new package or project was added, and Architecture.Tests passed 101/101 in the build run; the paging deviation is now accepted by decision record entry 1309
- Security criteria: secure-engineering skill; conformant for the attempt-1 cases (containment, no filter injection, protocol whitelist, approval void on a byte change) and now for ledger reconciliation, manifest file-name containment and ledger agreement on approve and verify; F-012 is a fail-closed availability defect, not an exposure
- Exceptions requested: the first-person list decided by the coordinator (implementation report deviation on matching rules) needs product-owner acceptance for its common-noun refusals; nothing else

## Test Adequacy Assessment

- Test evidence reviewed: confirmed in this run by `dotnet build MediaCompany.slnx --no-restore` (succeeded, 0 warnings, 0 errors; Architecture.Tests 101/101) and `dotnet test tests/MediaCompany.Pilot.Tests --no-build` (251 passed, 0 failed, 0 skipped, 1 m 40 s, matching the claimed 216 unit plus 35 integration); claimed and not confirmed: the other regression suites and the sample run
- Coverage of changed behavior: CorrectionTests covers 15 price spellings and 7 non-money facts, 9 refused and 3 accepted overrides, each looser setting and the stricter values, page minimum with 5 window cases plus 2 refusals and the word-for-word rejoin, 10 malformed timing lines plus missing and extra lines, 7 ledger cases (supersession and reconciliation), 13 approval-agreement cases; integration checks the page minimum, the runtime rejoin check, the proof frame and composition from package copies
- Gaps requiring new tests: an override using "em bé" or "tiếng Anh" (F-010); an interrupted or unreadable package entry copy at produce time (F-012); approval of a superseded but still ready package for the same video code

## Correction Requests

| ID | Addresses | Required change | Blocking | Owner |
|---|---|---|---|---|
| `CR-001` | `F-010` | Common nouns such as "em bé" and "tiếng Anh" are either accepted in an override or their refusal is recorded as accepted by the scope owner | no | product-owner |
| `CR-002` | `F-012` | An interrupted package entry copy write cannot leave the pilot folder refusing every later run without a documented recovery | no | dev-1-implement |

## Residual Risk

- Accepted risk: B3 and B5 rest on the CEO's own reading of the script, accepted by the CEO in decision record entry 1309 item 2
- Unmitigated risk: deleting the whole pilot folder resets limits and reuse (implementation report deviation on ledger safeguards, its question to the architect still open); a superseded package stays ready and approvable for its video code; a socket opened and closed within one 20 ms sample is unseen by the harness
- Monitoring required: the CEO's B3 and B5 ticks on each package; the pilot folder and its previous ledger generation; any ledger-entry-missing refusal, which signals F-012 or tampering

## Verdict

- Decision: approve-with-corrections
- Rationale: The Stage 8 table yields approve-with-corrections with status complete because only low findings remain open, the whole scope was reviewed, and test evidence was confirmed
- Blocking findings outstanding: None identified.
- Readiness recommendation: this reviewer recommends to qa, the Review Gate owner, that the change may progress with CR-001 and CR-002 tracked as non-blocking; the gate decision is qa's

## Open Questions

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| `Q-001` | Is refusing an override that uses "em", "anh" or "chị" as a common noun accepted for the pilot? | no | product-owner | F-010, CR-001 |
| `Q-002` | Should approval refuse a ready package whose ledger entry has been superseded by a re-run of the same video code? | no | architect | Residual Risk |
