```yaml
releaseNote:
  releaseId: RN-2026-018
  version: 0.0.0+58a22c5
  sourceInputs:
    - type: verification-report
      reference: runs/run-6fa26c9657d3/states/quality-review/artifacts/review-package.md
    - type: verification-report
      reference: runs/run-6fa26c9657d3/states/quality-review/artifacts/review-corrections.md
    - type: verification-report
      reference: runs/run-6fa26c9657d3/states/quality-review/artifacts/orchestrator-browser-and-live-read-record.md
    - type: verification-report
      reference: runs/run-6fa26c9657d3/state.json (gate records, Scope Gate to Closure Gate)
    - type: verification-report
      reference: inline (orchestrator dispatch briefing 2026-10-30, gate answers and the 434 s rerun figure)
    - type: final-change-summary
      reference: runs/run-6fa26c9657d3/states/implementation/artifacts/implementation-report.md
    - type: stakeholder-list
      reference: research/ceo-decision-record.md (entries 1313 to 1316)
    - type: architecture-context
      reference: pilot-affiliate-vn/mc-18/01-huong-dan-doc-trang.md
    - type: architecture-context
      reference: pilot-affiliate-vn/mc-17/01-huong-dan-ung-dung-web.md (lines 10 to 22 and section 9)
    - type: architecture-context
      reference: runs/run-022d0492a75a/states/documentation-and-release-handoff/artifacts/release-note.md
  producedBy: documentation
  agentVersion: 1.2.0
  schemaVersion: 1.0.0
  status: complete
  releaseVerdict: handed-off
  inputDigest: sha256:3f7fbb0f257e3a70f937e267c1151033
  contextDigest: sha256:4a5bfc355c7a1d63a52636724d0108fe
```

## Metadata

- Version: 0.0.0+58a22c5
- Release date and time: 2026-10-30T02:59:34Z, when the Verification Gate was approved, the later of the Review Gate (02:59:23Z) and the Verification Gate (gate records in run state). The Closure Gate is not yet decided.
- Environment: None reached. Nothing was deployed and nothing was published. The handed-off change is commits 6ab01fb (read from link, paste mode, settings switch, 13 offline cases) and 58a22c5 (the six review corrections) on the MC-18 session branch; f61d10f and 7bafc64 carry the ticket, plan and design only. That branch is cut from the MC-17 session branch, and neither is merged to main, so a compare against main also shows the MC-16 and MC-17 commits. The app runs only on the operator's own machine at 127.0.0.1.
- Release owner: orchestrator, which decides the Closure Gate this note feeds. Publishing any video stays the CEO's own act, video by video; decision record entries 1313 to 1316 leave that rule unchanged.

## Highlights

- Feature additions: Step 1 ("Bảng thông tin") of the MC-17 web app gains "Điền sẵn từ trang sản phẩm" (implementation report IR-2026-0035):
  - "Dán chữ của trang sản phẩm": the operator pastes text copied from the product page and presses "Điền từ chữ đã dán"; the app fills the fields it finds in that text through the same parser the read uses, with no network connection (case 056)
  - "Đọc trang": with the switch on, the operator pastes one shopee.vn product link or s.shopee.vn short link and presses once; the app reads that one public page and fills what it finds (cases 050 to 054)
  - a new "Cài đặt" view with "Bật đọc trang sản phẩm", off by default; paste and typing work whether it is on or off (case 057; decision record entry 1315)
  - each value filled by a read or a paste carries a mark and a "Xác nhận" button; saving with any mark unconfirmed is refused with fill-unconfirmed (case 055)
  - images read from the page are saved as anh-doc-01 onward and the operator picks up to three for the video (cases 054, 062)
  - price and description are shown for reference only and never saved in the fact sheet (case 055; entry 1314)

  The main operational fact of this release: on the one real Shopee page read so far, the read filled nothing. The single live read (entry 1316) made two requests, both to shopee.vn hosts, and stopped with no-product, because the page carries no product data unless a browser runs its scripts. Paste mode is therefore the expected everyday path on real Shopee pages, and "Đọc trang" is a shortcut that stops cleanly when the page cannot be read (orchestrator record, section 3).
- Bug fixes: None identified. The read is new. The review corrections were made before handoff, so they are part of the feature above and fix nothing delivered earlier.
- Improvements: None identified.

## Technical Changes and Compatibility

- API or contract changes: New, additive contracts on the app's loopback interface (IR-2026-0035, boundary compliance):
  - routes GET and POST /api/read-switch, POST /api/read, POST /api/paste, POST /api/read-confirm and GET /api/read-image, each behind the MC-17 request guard; the four POST routes refuse a foreign, other-port, localhost, null or missing Origin and a missing or wrong token with 403 and no effect, 28 of 28 (case 066)
  - new refusal and stop codes in the existing "TỪ CHỐI" / "DỪNG ĐỌC" form, for example no-product, access-refused, too-many-requests, captcha, sign-in-wall, link-not-allowed, hop-affiliate, read-off, read-busy, fill-unconfirmed; the full list is in IR-2026-0035
  - the fact-sheet save also refuses fill-missing, fill-field-unknown and fill-unconfirmed when a read or paste filled the form
  - the test program gains --fixture-host [--port N] [--switch on|off] [--root DIR]

  The app's command-line options and exit codes, and the toolchain's verbs, exit codes and refusal codes, are unchanged.
- Database or migration impact: None in the company store, which was not touched. New files: doc-trang-san-pham.json (the switch) in the app folder; in the pilot folder's web folder, doc-<video>.json (the fill record per video), read-images/<video>/ (images waiting for the pick) and doc-trang.log (the read log). Picked images are copied into the video's input folder on save, as uploaded images are. No migration is needed (IR-2026-0035).
- Configuration changes: One setting, "Bật đọc trang sản phẩm" in "Cài đặt", stored in doc-trang-san-pham.json. A missing, unreadable or other-version file reads as off (case 057). The request identity "MediaCompanyPilot/1.0 (doc-trang-san-pham)" and the bounds are fixed in code: at most 5 hops, 30 s per request, 120 s per click, 5 MiB per page, 40 MiB per image, 20 images (IR-2026-0035; entry 1315).
- Backward compatibility notes: An MC-17 installation upgraded to this build behaves as MC-17 until the switch is turned on, because a missing switch file reads as off and typed saves are accepted as before (case 057); the 28 MC-17 program cases 010 to 042 pass unchanged (IR-2026-0035). Two things differ from MC-17:
  - with the switch on, a "Đọc trang" click makes outbound HTTPS requests to shopee.vn hosts and to image hosts ending .susercontent.com; MC-17 made no connection off 127.0.0.1 (entries 1313 to 1315)
  - MC-17 program case 008, which asserted no networking type in the app, now denies every networking namespace and outbound type except the exact set the read sender declares; the orchestrator, deciding for qa, accepted this as the one change to an MC-17 case

  The MC-16 toolchain, src/MediaCompany.Pilot, has no change in any MC-18 commit (f61d10f, 7bafc64, 6ab01fb, 58a22c5; review corrections record).

## Operational Notes

- Deployment considerations: Nothing is deployed and nothing publishes. Someone technical rebuilds the repository at 58a22c5; the operator starts the app as in MC-17. The read stays off until the operator ticks "Bật đọc trang sản phẩm" in "Cài đặt". When on, the read contacts only shopee.vn hosts and image hosts ending .susercontent.com, one product per click, with no sign-in, no cookie, no proxy, no retry, and never affiliate.shopee.vn (entries 1313 to 1315; cases 050, 051, 052, 059, 065). No package was added (IR-2026-0035).
- Monitoring and alerts: The review package names the read log doc-trang.log (host, path, status, bytes, code) to watch during the pilot for 403, 429 and captcha codes. A healthy log has lines only after a "Đọc trang" click, only to shopee.vn hosts and .susercontent.com image hosts, and no line after a stop for the same click. Also watch for any warning from Shopee to the affiliate account (entry 1313, stop condition).
- Rollback criteria: Untick "Bật đọc trang sản phẩm", which leaves paste and typing working, the same day any of these happens:
  - Shopee sends any account warning, or answers the CEO's question on page reading unfavourably (entry 1313, stop condition)
  - the read log shows a host other than shopee.vn or a .susercontent.com image host, or a request with no click
  - a request follows a stop for the same click

  Reverting the code is a decision for the orchestrator and is not recorded here.

## Validation Summary

- Test status: The reviewer (dev-2-reviewer, review package RP-2026-003, at commit 6ab01fb) gave approve-with-corrections with one high finding, blocking (image-phase stops did not end the read), four medium and one low; it re-ran pilot 251/251 and web 49/49. The six corrections were applied in 58a22c5 with new cases 063 to 066 and two mutation checks (review corrections record; IR-2026-0035). The orchestrator, deciding for qa, approved the Review Gate and the Verification Gate on this evidence (gate records):
  - rerun at 58a22c5: MediaCompany.Pilot.Tests 251/251; MediaCompany.Pilot.Web.Tests 53/53 in 434 s (the 36 MC-17 cases plus cases 050 to 066; the implementer's own run took 455 s)
  - the reviewer's question on case 008 accepted, now that it fails closed; the question on case 061 accepted: case 061 runs a read plus the script verb, and produce under the socket harness is covered by case 042
  - a Browser-pane run on the fixture host: short link followed and 4 fields, 4 rows and 5 images filled; a blocked page stopped access-refused with no retry and opened paste; paste filled 2 fields and 4 rows; an incomplete save refused by the MC-16 check first
  - the single live read on the link the CEO named: 2 requests, both to shopee.vn hosts, stopped no-product, nothing filled, no image requested, no retry, switch off afterwards

  Not validated: a save from the Browser pane (K-013), paste of text copied from a real Shopee page (K-014), image fetches from a real image host (K-015).
- Known risk acceptance: The CEO accepted the risk of a fully automatic read and carries the Shopee-terms risk that page reading may be judged a breach, with account lock and lost commission (entry 1313). The CEO chose the switch off by default and the honest request identity (entry 1315). The orchestrator, deciding for qa, accepted the changed case 008 and the case 061 harness sample. No role has accepted the other known issues; the review package records no acceptor for K-001 to K-003.
- Post-release checks: Before the first real use on the CEO's machine:
  - confirm "Cài đặt" shows the switch off after start-up, and that a paste fills step 1 with the switch off
  - paste text copied from one real product page and check each filled value against that page before "Xác nhận" (K-014)
  - after any real read, read doc-trang.log against the healthy pattern above, untick the switch, and confirm doc-trang-san-pham.json reads off

## Known Issues

| ID | Issue | Impact | Workaround | Tracking |
|---|---|---|---|---|
| `K-001` | The image-host rule accepts any host ending .susercontent.com | A page could make the read fetch an image from any host under that suffix, wider than Shopee's own servers | None in the app; only images the product block declares are fetched, signature-checked, at most 20 and 40 MiB each | Review package RP-2026-003 residual risk; IR-2026-0035 residual risks; no acceptor recorded |
| `K-002` | The connect-time public-address check does not class IPv6 prefixes 64:ff9b::/96 and 2002::/16 as not public | A shopee.vn name resolving to such an address would be connected to rather than refused | None in the app | Review package RP-2026-003 residual risk; review corrections record, not changed |
| `K-003` | Turning the switch off during a read does not stop that read | The operator may see requests continue for up to 120 s after unticking | Wait for the read's result or stop message; the next click is refused while off | Review package RP-2026-003 residual risk; review corrections record, not changed |
| `K-004` | On the real Shopee page read, the read found no product data and stopped no-product | The operator should expect "Đọc trang" to fill nothing on real pages and to continue by paste | "Dán chữ của trang sản phẩm", or type by hand | Review package RP-2026-003 residual risk on real-page yield; answered by decision record entry 1316 and the orchestrator record |
| `K-005` | A link with a trailing dot is read, and its page address is recorded with the dot | The fact sheet's page address carries a stray dot | Remove the dot before reading, or correct the field, which then counts as typed | Review corrections record, not changed |
| `K-006` | The source scan of case 008 cannot see a networking type name assembled from string pieces at run time | A later change could reach the network outside the read sender without the scan failing | Review any web-app change that loads types by name | Review corrections record, not changed |
| `K-007` | Shopee may answer a read with 403, 429, a sign-in wall or a CAPTCHA | That click fills nothing; the app stops and never retries | Paste mode; do not press "Đọc trang" again at once | IR-2026-0035 residual risks |
| `K-008` | The fixture page is a model of a Shopee page, not a copy | A real page that does carry product data could fill fewer fields or offer odd candidate rows | Check each mark against the real page before "Xác nhận"; the MC-16 check still decides the sheet | IR-2026-0035 residual risks |
| `K-009` | The socket harness samples and can miss a socket that opens and closes between samples | The no-egress-while-idle evidence is strong but not absolute | Read log and fixture request counts back it | IR-2026-0035 residual risks |
| `K-010` | Anyone who can write the app folder can turn the switch on | Reads become possible without "Cài đặt"; a read still needs a pasted link and a click | Run the app on the operator's own account; check "Cài đặt" | IR-2026-0035 residual risks |
| `K-011` | On the fixture host only, step 1 first showed the read as off although the host started with it on; the first click said "Đã tắt" | A tester using the fixture host may see the switch state reversed once; no egress effect, the server reads the file on each read | Click again and read the message; the isolated live-read app behaved consistently | Orchestrator record, observation 1, for qa; no correction request opened, open question raised to qa |
| `K-012` | After a read, values it filled (page address, read date) stay in the form when the operator then pastes | A paste may leave an earlier read's values beside pasted ones | Check each field before "Xác nhận" (MC-18 operator guide, section 4.3) | Orchestrator record, observation 3, informational; open question raised to product-owner |
| `K-013` | A fact-sheet save was not exercised from the Browser pane, which cannot attach a local screenshot file | The save with picked read images was seen only by the automated headless browser | None needed; case 062 covers the save path | Orchestrator record, section 1 |
| `K-014` | No supplied record shows a paste of text copied from a real Shopee page; recorded pastes used the fixture's sample text | On the expected everyday path, real copied text may fill fewer fields than the fixture did | Type missing fields by hand; fields the text does not supply stay for the operator | None; open question raised to orchestrator |
| `K-015` | No real Shopee image host has been contacted; the live read stopped before any image | Whether a real read ever saves images is unknown | Upload images at step 2, as in MC-17 | Orchestrator record, section 2; no tracking item, open question raised to orchestrator |
| `K-016` | No version is declared for the app, so this note uses 0.0.0+58a22c5, the commit as build metadata | Readers identify this delivery by commit, not by a release number | Cite commit 58a22c5 | None; open question raised to tech-lead |

## Communication

- Stakeholders notified: This note is addressed to three readers:
  - the CEO, who decided entries 1313 to 1316, approves each fact and each video, and owns the stop condition
  - the employee operator, who uses step 1 (the MC-18 operator guide names "CEO và nhân viên vận hành")
  - the orchestrator, which owns the Closure Gate and delivers the compare link

  The orchestrator is implied by the phase; no communication list was supplied. Distribution belongs to the orchestrator.
- Support handoff notes: What looks broken but is not:
  - "DỪNG ĐỌC [no-product]" on a real Shopee page is the expected outcome (K-004); continue by paste
  - "Ứng dụng không thử đọc lại" is deliberate: a stop is never retried
  - fill-unconfirmed appears only after the MC-16 check passes; an incomplete sheet is refused first with the toolchain's own code (orchestrator record, observation 2, by design)
  - read-busy means another read is running; wait

  Where to look first: doc-trang.log in the pilot folder's web folder for what the read requested, and doc-trang-san-pham.json in the app folder (%LocalAppData%\MediaCompany\PilotWeb) for the switch. The Vietnamese operator guide for this step is pilot-affiliate-vn/mc-18/01-huong-dan-doc-trang.md, placed by the orchestrator. The MC-16 toolchain is unchanged, so the known issues of release note RN-2026-016 still apply to each video. Stale content, not carried forward:
  - release note RN-2026-017 says the app makes no network connection beyond 127.0.0.1; from 58a22c5 that holds only while the switch is off
  - the MC-17 operator guide says at line 10 that the page header "luôn" (always) reads "Không gửi gì ra ngoài", while its section 9 gives a different header text from MC-18; and section 9 is titled "Chưa có trong ứng dụng này" (not yet in this app) although it now describes delivered behaviour. Proposed correction for the guide's owner, product-owner: drop "luôn" from line 10 and retitle section 9, for example "Thêm từ MC-18"

### Tóm tắt cho CEO

- Không có gì được đăng. Bước 1 có thêm "Dán chữ của trang sản phẩm" và "Đọc trang"; đọc trang mặc định tắt, bật ở "Cài đặt".
- Lần đọc thật duy nhất (link CEO đưa): 2 yêu cầu, đều tới shopee.vn, dừng "no-product", không điền gì, không thử lại, đã tắt lại. Vì vậy cách làm thường ngày là dán chữ của trang.
- Không đăng nhập, không cookie, không mở affiliate.shopee.vn, không giả trình duyệt. Bộ công cụ MC-16 không đổi.
- Kiểm thử: 251/251 và 53/53. Chưa thử dán chữ chép từ trang Shopee thật (K-014) và chưa lấy ảnh từ máy ảnh thật của Shopee (K-015).
- Nếu Shopee cảnh báo tài khoản: bỏ tích "Bật đọc trang sản phẩm" ngay trong ngày.
