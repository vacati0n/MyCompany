```yaml
releaseNote:
  releaseId: RN-2026-016
  version: 0.0.0+e264a7a
  sourceInputs:
    - type: verification-report
      reference: runs/run-535f3f938f32/states/quality-review/artifacts/attempt-2/review-package.md
    - type: verification-report
      reference: runs/run-535f3f938f32/state.json (Review Gate and Verification Gate decisions and rationales)
    - type: final-change-summary
      reference: runs/run-535f3f938f32/states/implementation/artifacts/attempt-2/implementation-report.md
    - type: final-change-summary
      reference: runs/run-535f3f938f32/states/implementation/artifacts/implementation-report.md
    - type: stakeholder-list
      reference: research/ceo-decision-record.md (entries 1304 to 1309)
    - type: architecture-context
      reference: runs/run-535f3f938f32/task-context.yaml
    - type: architecture-context
      reference: pilot-affiliate-vn/mc-16/03-huong-dan-van-hanh.md
    - type: architecture-context
      reference: pilot-affiliate-vn/pilot-kit/04-danh-sach-duyet-dang-bai.md
  producedBy: documentation
  agentVersion: 1.2.0
  schemaVersion: 1.0.0
  status: complete
  releaseVerdict: handed-off
  inputDigest: sha256:60b5ae6a453889fa3647086f7f2c6331
  contextDigest: sha256:4a5bfc355c7a1d63a52636724d0108fe
```

## Metadata

- Version: 0.0.0+e264a7a
- Release date and time: 2026-10-26T18:18:23Z, when the Verification Gate was approved and the change became ready for handoff (gate record in run state). The Closure Gate is not yet decided.
- Environment: None reached. Nothing was deployed and nothing was published. The handed-off change is commits fc21199 (the toolchain) and e264a7a (the review corrections) on the MC-16 session branch. The only video made so far is the sample package in the operator's local sample folder, built from code-generated fixtures.
- Release owner: orchestrator, which decides the Closure Gate this note feeds. Publishing any video stays the CEO's own act, video by video. The decision record says so and entries 1304 to 1309 leave that rule unchanged.

## Highlights

- Feature additions: A new offline tool, MediaCompany.Pilot. It has five commands: check, script, produce, approve and verify. The operator gives it one fact sheet, three product images, one fresh recording of at most 60.0 s in a real human voice, and one portrait presenter clip. From those, the tool builds a 1080x1920 (9:16) video package ready for the CEO to review. The package contains: the video and its SHA-256; a provenance entry for each sentence; digests of the inputs; copies of the exact input bytes it checked; a preview frame; and the kit 04 checklist. In that checklist only B8 (the "Video tạo bởi AI · Có liên kết tiếp thị" overlay for the whole video) is pre-ticked. B3 and B5 carry the tool's note and are left for the CEO to tick (decision record entry 1309 item 2). The script is written by fixed code from the fact sheet. Captions are timed from the pauses in the recording, or from a timing file the operator writes. A long caption is split into two-line pages, each shown for at least 1.2 s (entry 1309 item 1). The CEO approves each video against its SHA-256, and the approval is void after any one-byte change. The tool also refuses: a recording already used for another video, a thirteenth video, a third video of one product, a repeated focus, and settings looser than the CEO's values (implementation reports IR-2026-0032 and IR-2026-0033). The toolchain calls no generative model, no text-to-speech, no speech recognition and no network. The presenter clip, the only generative element, is made by the CEO outside the tool. A Vietnamese operator guide, pilot-affiliate-vn/mc-16/03-huong-dan-van-hanh.md, goes with the tool.
- Bug fixes: None identified. The tool is new. The twelve corrections raised by the first review were made before handoff, so they are part of the feature above and fix nothing that had been delivered before.
- Improvements: Pilot kit file 03 now matches the tool and the CEO's decisions. Part B row 6 is a call to action with no price, and Part F says the recording is the operator's own real voice with no text-to-speech and no voice cloning. Kit file 04 item B13 names the operator's real voice. Kit 03 also changed its header bullet, the Part B column heading, Part C and the Part D example; the product owner has not yet accepted those four edits (see K-011).

## Technical Changes and Compatibility

- API or contract changes: These contracts are new and none replaces an existing one. The five console commands, with exit codes 0 (done), 1 (usage), 2 (refused) and 3 (tool failed). New JSON forms for the settings, the fact sheet, the package manifest, the approval record and the pilot ledger. Refusals printed as "TỪ CHỐI [code]" with the reason in Vietnamese. The second commit, e264a7a, added seven refusal codes (override-first-person, caption-page-too-short, settings-looser-than-ceo, ledger-truncated, ledger-entry-missing, package-file-not-plain, package-ledger-mismatch). It also added a required settings member for the minimum caption page time, and an inputs folder inside each package.
- Database or migration impact: None in the company store, which was not touched. The pilot ledger is a new hash-chained file, pilot-ledger.json, with its previous generation pilot-ledger.previous.json. Both live in the configured pilot folder outside the repository. Existing pilot ledgers are read unchanged, and no migration is needed.
- Configuration changes: The operator copies pilot-affiliate-vn/mc-16/vi-du-cai-dat.json to a place outside the repository and sets local paths. Those paths include the pilot folder (refused if it is inside the repository) and the configured SHA-256 of ffmpeg, ffprobe and the font. The CEO's values (from entries 1308 and 1309) may be made stricter but never looser:
  - caption text 56 px
  - overlay text 40 px
  - safe area: left 60, right 180, top 200, bottom 480
  - clip tolerance 0.5 s
  - minimum caption page time 1.2 s

  A looser settings file is refused with settings-looser-than-ceo.
- Backward compatibility notes: The implementation report states that no existing signature, schema or contract changed, and that frozen Wave 12 and Wave 13 material was not touched. One case needs action. A settings file made from the example in the first commit, fc21199, lacks the settings member for the minimum caption page time. The tool refuses that file until the operator adds the member, as the current example and guide section 1 show.

## Operational Notes

- Deployment considerations: Nothing is deployed and nothing publishes. The operator builds the tool once with dotnet build src/MediaCompany.Pilot (guide section 1). The tool uses only components already installed: ffmpeg, ffprobe and the Inter font. It needs no download, no network, no Docker and no company store. A video is published only after the CEO runs approve against its SHA-256, runs verify, and posts the verified video.mp4 by hand.
- Monitoring and alerts: A healthy run prints "ĐẠT [command]" for each step, and verify prints the same SHA-256 that was approved. Watch for these, as the review package names them:
  - the CEO's own B3 and B5 ticks on every package
  - any ledger-truncated or ledger-entry-missing refusal, which signals K-002 or tampering
  - the pilot folder and its previous ledger generation

  On the first real recordings, also watch for pauses-not-found and caption-page-too-short refusals (K-008, K-012).
- Rollback criteria: Stop producing and publish nothing when any of these happens: verify reports a SHA-256 other than the approved one; approval-void appears; a ledger refusal appears; or a real recording or presenter clip fails in a way the generated sample never showed. The change wrote nothing to the company store or to any published channel, so outside the pilot folder there is nothing to undo. Reverting the code is a decision for the orchestrator and is not recorded here.

## Validation Summary

- Test status: The reviewer (dev-2-reviewer, review package RP-2026-002) gave the verdict approve-with-corrections. In that review it re-ran the build (0 warnings, 0 errors; Architecture tests 101/101) and the pilot tests (251/251: 216 unit plus 35 integration). The orchestrator, deciding for qa, first rejected the Review Gate over four high findings. After the corrections it approved the gate, with two low findings left open (K-001, K-002). The orchestrator, again for qa, approved the Verification Gate on the following evidence:
  - the pilot tests above, and the existing non-Docker suites at baseline: Capability 90/90, Deterministic 453/453, Domain 48/48, Production 132 passed and 1 skipped. The implementer reported these; the reviewer did not re-run them.
  - an end-to-end sample made from generated fixtures only, whose video the orchestrator measured independently: SHA-256 c13e81d43bc6cbf0ba5d94712b5cf90e2e467312b829d04cb02801b75abd3c29, h264 1080x1920 with aac audio, 17.4 s
  - zero sockets owned by the tool's job processes
  - no media file added to the repository
  - nine refusals demonstrated: banned term; recording over 60 s; reused recording; landscape clip; clip length mismatch; caption page too short; non-plain package file; void approval; hash mismatch

  Not validated: a real presenter clip, and a real voice recording.
- Known risk acceptance: The CEO accepted, in decision record entry 1309, that B3 and B5 rest on the CEO's own reading of the script, and that long captions are split into two-line pages of at least 1.2 s. The orchestrator, deciding the Review Gate for qa, accepted K-001 and K-002 as non-blocking. The Scope Gate approval (entry 1307 item 1) accepted that recording reuse is detected by the file's digest only. No role has accepted the other known issues.
- Post-release checks: Before the first real publication: make the first real recording and confirm that pause detection finds one pause between each pair of sentences; confirm that the CEO's real presenter clip passes the portrait check and the 0.5 s length check; read the script and tick B3 and B5 personally; run verify immediately before posting.

## Known Issues

| ID | Issue | Impact | Workaround | Tracking |
|---|---|---|---|---|
| `K-001` | The first-person word list includes "em", "anh" and "chị", so an override that uses them as ordinary nouns, such as "em bé" or "tiếng Anh", is refused | The operator's override is refused with override-first-person; no wrong video results | Rewrite the override without those words, or drop the override and keep the generated sentence | Review package RP-2026-002, first correction request and first open question, owner product-owner, non-blocking |
| `K-002` | The package's copy of its ledger entry is written non-atomically | If a produce run is interrupted while writing that copy, the copy is unreadable and the tool refuses every later run until someone repairs the pilot folder by hand | No documented recovery; stop, do not delete or recreate the ledger, and call the person responsible (guide section 7) | Review package RP-2026-002, second correction request, owner dev-1-implement, non-blocking |
| `K-003` | When a video code is produced again, the earlier package stays ready and can still be approved | The CEO could approve an older package of the same video code | No safeguard in the tool; approve only the package path that the latest produce printed | Review package RP-2026-002, second open question, owner architect |
| `K-004` | The price, use-claim and comparison checks work from closed word lists, so new phrasings can pass | A price, use or comparison claim could reach the CEO's review unflagged | The CEO reads the script against its Part E source lines and ticks B3 and B5 personally | Accepted by the CEO, decision record entry 1309 item 2; review package residual risk |
| `K-005` | Deleting the whole pilot folder, packages included, cannot be detected | The 12-video limit, the per-product limits and the recording-reuse check would restart from zero | Never delete or edit anything in the pilot folder (guide section 8) | Implementation report IR-2026-0033, fourth open question, owner architect |
| `K-006` | The zero-socket evidence has limits: the harness samples every 20 ms, so a socket opened and closed within one sample is not seen; a process started before it joins the job would not be counted; and the in-process telemetry reading of zero proves little | The no-network result is strong but not absolute; it also rests on file-only inputs and a static reference check | None needed in operation | Review package residual risk (unmitigated); implementation report IR-2026-0033 residual risks |
| `K-007` | The staging check (input-changed) has no direct test | A regression in input staging could go unnoticed by a dedicated test; no wrong behavior is known | An integration test checks that the composed inputs equal the hashed bytes | Implementation report IR-2026-0033 residual risks |
| `K-008` | At the sample's speaking pace the closing sentence's caption pages last 1.597 s and 1.403 s, close to the 1.2 s minimum | A faster real voice is refused with caption-page-too-short, naming the sentence | Record again and speak that sentence more slowly (guide section 5), or fix timing.txt | Implementation report IR-2026-0033 residual risks |
| `K-009` | The tool also refuses the older spellings "sức khoẻ" and "an toàn cho sức khoẻ", which go beyond the decided list | A fact that uses those spellings is refused as health wording | Remove the health wording; it is banned in either spelling | Implementation report IR-2026-0033, first open question, owner product-owner |
| `K-010` | A held package gets no ledger entry, and approval refuses any package without one; this design is not yet accepted | Held packages do not count toward the limits and cannot be approved | Fix the cause of the hold and run produce again | Implementation report IR-2026-0033, second open question, owner architect |
| `K-011` | Kit 03 edits beyond row 6 and Part F (header bullet, Part B column heading, Part C, Part D example) await acceptance | The kit text may change again after the product owner decides | None | Implementation report IR-2026-0033, third open question, owner product-owner |
| `K-012` | Pause detection has been proven only on generated tones; its threshold (-35 dB noise floor, 0.35 s minimum gap) must be set again from the first real recording | Breaths or room noise in a real voice may give the wrong pause count, and the run then stops before captioning | Record again with clear pauses, or supply timing.txt (guide section 6) | Implementation report IR-2026-0032 residual risks and follow-up work; no tracked item for the re-setting, so an open question is raised to the orchestrator |
| `K-013` | Only a code-generated portrait stand-in has been tested as the presenter clip; no real clip has been | A real export's frame rate, rotation or audio may fail or behave differently | None until the CEO supplies a real clip; the portrait check and the 0.5 s tolerance stay in place | Implementation report IR-2026-0032 residual risks; run plan assumption that this waits on a real clip from the CEO |
| `K-014` | Kit 04 item B7 still says "Bản ghi của CEO", although an employee operator may now record in their own voice (entry 1307) | An employee's recording does not literally match B7's wording | The CEO decides how to tick B7 until it is reworded like B13 | Implementation report IR-2026-0032 follow-up work; no tracked item, so an open question is raised to the product owner |
| `K-015` | Reuse is detected by file digest only, so a re-encoded or trimmed copy of a used recording passes; the tool also does not check that the spoken words match the script | A reused or misread recording can reach the CEO's review | The operator records fresh and reads exactly; the CEO listens before approving | Scope decisions accepted at the Scope Gate (entry 1307 item 1) |
| `K-016` | No version is declared for the tool, so this note uses 0.0.0+e264a7a, which carries the commit as build metadata | Readers identify this delivery by commit, not by a release number | Cite commit e264a7a | None; an open question is raised to tech-lead |

## Communication

- Stakeholders notified: This note is addressed to the CEO, who approves every publication and is the current operator, and to a future employee operator. The decision record (entries 1304 to 1309) names both roles. It is also addressed to the orchestrator, which owns the Closure Gate and delivers the branch compare link (run plan). The orchestrator role is implied by the phase; no communication list was supplied. Distribution belongs to the orchestrator.
- Support handoff notes: Some states look like faults but are the tool working as designed:
  - a first line "GIỮ LẠI" means a held package; do not approve it
  - B3 and B5 are unticked on purpose
  - approval-void and approval-hash-mismatch after any change to video.mp4 (re-saving or re-encoding included) mean the old approval no longer applies
  - "LỖI CÔNG CỤ" means ffmpeg or ffprobe did not run; a replaced ffmpeg, ffprobe or font file fails its configured SHA-256 check

  Where to look first: the refused folder in the pilot folder holds the record of every refused produce run, and the sample folder's sample-transcript.txt shows a full good run. The sample package path and digest quoted in the guide change every time the sample is regenerated. After a ledger refusal, never create a new ledger.

### Tóm tắt cho CEO

- Không có gì được đăng. Công cụ chỉ tạo gói video để CEO xem; CEO duyệt từng video theo mã SHA-256, và lần duyệt mất hiệu lực nếu video đổi dù chỉ một byte.
- Công cụ không dùng mô hình tạo sinh, không giọng máy (TTS), không nhận dạng giọng nói, không mạng. Người dẫn AI là phần tạo sinh duy nhất, do CEO làm bên ngoài công cụ.
- B3 (không nói đã dùng) và B5 (không nhắc giá) do CEO tự nghe và tự tích; công cụ chỉ tích sẵn B8.
- Hạn chế: danh sách từ cấm có hạn (K-004); câu thay thế có "em", "anh" bị từ chối (K-001); bản sao mục sổ ghi không nguyên tử (K-002); máy đo socket lấy mẫu mỗi 20 ms (K-006); ngưỡng tìm chỗ ngừng phải đặt lại từ bản ghi thật đầu tiên (K-012); mục B7 của kit 04 có thể cần sửa chữ giống B13 (K-014).
- Việc CEO cần làm: cung cấp đầu vào thật (bảng thông tin, ảnh, bản ghi giọng thật mới, đoạn phim người dẫn dọc). Đoạn phim người dẫn thật chưa được thử; mới chỉ thử bằng đoạn phim thay thế do code tạo ra (K-013).
