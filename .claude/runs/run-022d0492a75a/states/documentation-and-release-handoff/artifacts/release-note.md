```yaml
releaseNote:
  releaseId: RN-2026-017
  version: 0.0.0+ef327e9
  sourceInputs:
    - type: verification-report
      reference: runs/run-022d0492a75a/states/quality-review/artifacts/review-package.md
    - type: verification-report
      reference: runs/run-022d0492a75a/states/quality-review/artifacts/review-corrections.md
    - type: verification-report
      reference: runs/run-022d0492a75a/state.json (Review Gate and Verification Gate decisions and rationales)
    - type: verification-report
      reference: inline (orchestrator briefing 2026-10-27, Browser-pane demonstration figures)
    - type: final-change-summary
      reference: runs/run-022d0492a75a/states/implementation/artifacts/implementation-report.md
    - type: stakeholder-list
      reference: research/ceo-decision-record.md (entries 1310 to 1313)
    - type: stakeholder-list
      reference: pilot-affiliate-vn/mc-17/00-ticket-mc-17.md
    - type: architecture-context
      reference: runs/run-022d0492a75a/task-context.yaml
    - type: architecture-context
      reference: src/MediaCompany.Pilot.Web (Program.cs, Routes.cs, RequestGuard.cs, Outcomes.cs, Setup.cs, Shortcut.cs, Pages/index.html, Pages/app.js)
    - type: architecture-context
      reference: pilot-affiliate-vn/mc-16/03-huong-dan-van-hanh.md
  producedBy: documentation
  agentVersion: 1.2.0
  schemaVersion: 1.0.0
  status: complete
  releaseVerdict: handed-off
  inputDigest: sha256:d6e89fe146c849636362e9a1f091b021
  contextDigest: sha256:4a5bfc355c7a1d63a52636724d0108fe
```

## Metadata

- Version: 0.0.0+ef327e9
- Release date and time: 2026-10-27T03:58:16Z, when the later of the Review Gate and the Verification Gate was approved (gate records in run state). The Closure Gate is not yet decided.
- Environment: None reached. Nothing was deployed and nothing was published. The handed-off change is commits bb5adb4 (the web app and its test program) and ef327e9 (the review corrections) on the MC-17 session branch. That branch is cut from the MC-16 branch, which is not yet merged, so a compare against main also shows the MC-16 commits (ticket, definition of done). The app runs only on the operator's own machine at 127.0.0.1.
- Release owner: orchestrator, which decides the Closure Gate this note feeds. Publishing any video stays the CEO's own act, video by video; decision record entries 1310 to 1313 leave that rule unchanged.

## Highlights

- Feature additions: A new local web app, MediaCompany.Pilot.Web, puts the MC-16 toolchain behind a Vietnamese page for a non-technical operator (implementation report IR-2026-0034). It guides seven steps: "1. Bảng thông tin", "2. Ảnh", "3. Kịch bản", "4. Ghi âm", "5. Phim người dẫn", "6. Tạo video", "7. Xem và duyệt". New parts that the command-line tool did not have:
  - a first-run screen, "Cài đặt lần đầu", that asks only for the pilot folder and the CEO's name and shows the CEO's numbers read-only (decision record entry 1310)
  - a form for the fact sheet, with a live warning on banned words as the operator types
  - recording in the browser, with a stop at 60 s, and an early check of the recording at step 4 (entry 1312)
  - a caption timing editor at step 6
  - the kit 04 checklist on screen; approval is refused until all 26 operator items are ticked
  - approval by the CEO, or by an employee on the CEO's written instruction, with the approver line fixed by entry 1311 item 4
  - a desktop shortcut, made on confirmation, that starts the app or reopens its page (entry 1311)

  The toolchain still decides every check, and `src/MediaCompany.Pilot` is unchanged by this delivery (review corrections record).
- Bug fixes: None identified. The app is new. The corrections from the review were made before handoff, so they are part of the feature above and fix nothing that had been delivered before.
- Improvements: None identified.

## Technical Changes and Compatibility

- API or contract changes: These contracts are new and none replaces an existing one. The executable MediaCompany.Pilot.Web takes the options --port, --app-folder, --desktop-folder and --no-browser. Its exit codes are: 0 (started, or already running), 1 (usage), and 4 (port in use, or a bind address that is not 127.0.0.1). The default port is 47817. The HTTP routes serve only this app's page, on 127.0.0.1. A request is refused when its host name or origin is not 127.0.0.1 at the app's port, or when it lacks the page token; so "localhost" in the address bar is refused. App refusals use the toolchain's form "TỪ CHỐI [code]" with a Vietnamese reason and next step. Two codes come from the corrections: ledger-busy and recording-check-busy (entry 1312).
- Database or migration impact: None in the company store, which was not touched. The pilot ledger keeps its MC-16 format. The app reads it only at the step-4 early check, read-only and never at the same time as a produce run (entry 1312). Only the toolchain writes it. No migration is needed.
- Configuration changes: The app keeps its own folder, %LocalAppData%\MediaCompany\PilotWeb. That folder holds cai-dat.json (pilot folder, CEO name, port), an inputs folder per video code, and the page state and log nhat-ky.log. The default pilot folder is %UserProfile%\MediaCompanyPilot. The CEO's values come from the MC-16 example settings file and are shown read-only. A looser settings file is still refused with settings-looser-than-ceo.
- Backward compatibility notes: The command-line tool of MC-16 is unchanged and still works on its own. Four app behaviours differ from using that tool by hand (implementation report deviations):
  - approval needs all 26 operator ticks
  - a changed toolchain file is refused with toolchain-changed until setup is saved again
  - replacing a recording deletes that video's caption timing file
  - a recording is read back through a page route on 127.0.0.1

## Operational Notes

- Deployment considerations: Nothing is deployed and nothing publishes. Someone technical builds the repository once. The operator then starts the app with the desktop shortcut "Thí điểm video MediaCompany", or with `dotnet run --project src/MediaCompany.Pilot.Web`, and the page opens at http://127.0.0.1:47817. Closing the app's window turns the app off. The app downloads nothing and calls no text-to-speech, no voice cloning, no network beyond 127.0.0.1 and no publishing service (ticket hard constraints; entry 1310). A video is published only when the CEO posts the approved, verified video.mp4 by hand.
- Monitoring and alerts: The review package names what to watch:
  - produce-busy lines and app-fault lines in nhat-ky.log
  - ledger refusals at step 4
  - the approver line of every approval, especially any line that names an employee

  Also watch on the first real use for a microphone error at step 4 (K-004).
- Rollback criteria: Close the app and return to the MC-16 command-line steps when any of these happens:
  - the page shows "Lỗi ứng dụng" more than once
  - a ledger refusal appears
  - "Kiểm lại video" reports a SHA-256 other than the approved one

  The app writes only to its own folder and to the pilot folder, the same as the toolchain; outside them there is nothing to undo. Reverting the code is a decision for the orchestrator and is not recorded here.

## Validation Summary

- Test status: The reviewer (dev-2-reviewer, review package RP-2026-002, at commit bb5adb4) gave the verdict approve-with-corrections, with one blocking correction and no critical finding. In that review it re-ran the build (0 warnings, 0 errors; Architecture tests 101/101), the pilot tests (251/251) and the web tests (31/31). After the corrections in ef327e9 the orchestrator, deciding for qa, approved the Review Gate and the Verification Gate on this evidence:
  - build 0 warnings and 0 errors; MediaCompany.Pilot.Tests 251/251; MediaCompany.Pilot.Web.Tests 36/36, run twice (180 s and 185 s), including a recording test with no microphone
  - the implementer's regression suites at baseline: Capability 90/90, Deterministic 453/453, Domain 48/48, Production 132 passed and 1 skipped
  - a Browser-pane demonstration of all seven steps on a code-generated sample with a faked microphone (orchestrator briefing)
    - recording of 21.8 s
    - a landscape clip refused with clip-not-portrait
    - produce passed with a 1080x1920 video of 21.767 s
    - 26 of 26 items ticked
    - approved by an employee on the CEO's instruction
    - "Kiểm lại video" passed

  Not validated: a real microphone, the operator's own browser, a real presenter clip, and the start-up refusal with exit code 4 (K-002, K-004).
- Known risk acceptance: The CEO accepted the step-4 early check that reads the ledger, serialised with produce (entry 1312). The orchestrator, deciding the Review Gate for qa, made these decisions:
  - kept the live-check coverage as a known issue (K-001)
  - left the exit-4 start-up check untested by design (K-002)
  - accepted that browser takes are cut to 60.000 s (K-011)
  - accepted the approver choice (K-009), under entries 1310 and 1311

  No role has accepted the other known issues.
- Post-release checks: Before the first real video:
  - on the CEO's machine, confirm the desktop shortcut starts the app, and confirm a real microphone records at step 4 in the operator's own browser
  - read the script and tick every checklist item personally
  - press "Kiểm lại video" immediately before posting

## Known Issues

| ID | Issue | Impact | Workaround | Tracking |
|---|---|---|---|---|
| `K-001` | The live warning on the fact sheet form covers only the fields the toolchain's check command scans: Chủ thể and Phân loại in the header, and each row's subject, attribute, value and override | A banned word in another field gets no warning while typing | None needed: the toolchain check at step 1 save and at produce stays authoritative and refuses it | Review package RP-2026-002, second correction request; kept unchanged by the orchestrator |
| `K-002` | The start-up refusal for a bind address other than 127.0.0.1 (exit code 4) has no test; a test would need a seam to bind elsewhere | A regression in that check would not be caught by a test; an environment endpoint override would bind briefly before the check stops the app | None needed in operation: the bind address is fixed in code | Review corrections record, sixth correction; review package residual risk |
| `K-003` | The no-network test reports the browser's UDP rows but does not assert on them, because they cannot be tied to the app | A UDP socket opened by the page would be reported, not failed | Read the reported rows in the test output | Implementation report IR-2026-0034 deviations and residual risks |
| `K-004` | No real microphone and not the operator's own browser have been tested; the demonstration used a faked microphone | Recording may fail on the operator's machine | Allow the microphone for the page, or upload a ready recording at step 4 | Implementation report IR-2026-0034 residual risks and follow-up |
| `K-005` | The socket harness samples every 20 ms, so a socket opened and closed within one sample is not seen | The no-network result is strong but not absolute | None needed in operation | Implementation report IR-2026-0034 residual risks |
| `K-006` | A program already running on the same machine could read the page token | Such a program could drive the app's page | Run the app only on the operator's own machine and account | Implementation report IR-2026-0034 residual risks |
| `K-007` | Progress at step 6 shows elapsed time only, not how much is left | The operator cannot tell how long produce still needs | Keep the page open until the step reports a result | Implementation report IR-2026-0034 residual risks |
| `K-008` | The desktop shortcut may fail on a locked-down machine | No shortcut is made; setup itself is unaffected (refusal shortcut-failed, now tested) | Start the app from its executable file, as the refusal message says | Implementation report IR-2026-0034 residual risks |
| `K-009` | An employee can choose "CEO tự bấm"; the app cannot check that a written instruction exists | The approver line could name the wrong presser | The CEO reads the approver line of each approval | Review package residual risk; accepted by the orchestrator under entries 1310 and 1311 |
| `K-010` | The CEO name can be changed by running setup again | Later approver lines would carry the new name | Only the CEO or the person responsible runs setup | Review package residual risk |
| `K-011` | A browser take longer than 60 s is cut to 60.000 s at conversion, not refused; an uploaded file over 60 s is still refused | The end of a long take is lost | Watch the timer and press stop before 60 s | Review package residual risk; accepted by the orchestrator |
| `K-012` | The step-4 early check reads the ledger inside the app; it waits for a running produce and then reports ledger-busy or recording-check-busy | Step 4 or step 6 may ask the operator to wait and press again | Wait for the other step to finish, then press again | Decision record entry 1312; review package residual risk |
| `K-013` | No version is declared for the app, so this note uses 0.0.0+ef327e9, which carries the commit as build metadata | Readers identify this delivery by commit, not by a release number | Cite commit ef327e9 | None; an open question is raised to tech-lead |
| `K-014` | The MC-16 operator guide still describes only the command line | An operator reading it does not learn of the web app | Use the new operator guide for the web app | Implementation report IR-2026-0034 open question, owner product-owner |

## Communication

- Stakeholders notified: This note is addressed to three readers:
  - the CEO, who approves every publication
  - the employee operator, who may press approve on the CEO's written instruction
  - the orchestrator, which owns the Closure Gate and delivers the compare link

  Decision record entries 1310 and 1311 and the ticket name the first two. The orchestrator is implied by the phase; no communication list was supplied. Distribution belongs to the orchestrator.
- Support handoff notes: Reading a Shopee product page from a link (decision record entry 1313) is not part of this delivery; it is planned for the next run, MC-18. A Vietnamese operator guide for the web app goes with this delivery and is placed by the orchestrator. The MC-16 toolchain is unchanged, so the known issues of release note RN-2026-016 still apply to every video. Some states look like faults but are the app working as designed:
  - "Giữ lại" means a held package; do not approve it
  - produce-busy, ledger-busy and recording-check-busy mean: wait, then press again
  - a page opened at "localhost" is refused; use 127.0.0.1
  - after the app restarts, the old page is refused until it is reloaded with F5

### Tóm tắt cho CEO

- Không có gì được đăng. Ứng dụng chạy trên máy của CEO ở 127.0.0.1, dẫn qua bảy bước và gọi đúng bộ công cụ MC-16; bộ công cụ không đổi.
- Ứng dụng không tải gì về, không dùng giọng máy (TTS), không nhân bản giọng, không gửi gì ra ngoài 127.0.0.1, không đăng bài.
- Duyệt: CEO tự bấm, hoặc nhân viên bấm theo chỉ thị bằng văn bản của CEO; dòng người duyệt ghi rõ tên. Phải tích đủ 26 mục mới duyệt được.
- Đã thử cả bảy bước bằng mẫu do code tạo và micrô giả; chưa thử micrô thật và trình duyệt của người dùng (K-004).
- Đọc trang Shopee từ đường dẫn không có ở đây; đó là MC-18.
