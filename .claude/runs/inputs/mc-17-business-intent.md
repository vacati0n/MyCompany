# Business intent for MC-17 (the owner's request and decisions, verbatim)

## The CEO's request, 2026-10-27, in chat

"người dùng là non tech user human vì vậy những việc như trong hướng dẫn vận hành công cụ gây khó khăn. hãy viết một tool web app có UI,UX frendly cho user nhập dữ liệu từng step bao gồm cả việc support ghi âm trực tiếp thay vì bắt user tải file."

---

## CEO-D-1304 — The AI presenter lip-syncs to the CEO's own recorded voice

**The CEO's own decision, 2026-10-26**, given in the chat with the orchestrator session, on the presenter form of
`CEO-D-1301` item 4 and the tool choice `OD12` of `CEO-D-1302`.

The CEO's words: "anh mở tài khoản shopee rồi . về tiếng việt là tiếng mẹ đẻ có thể dùng bản ghi âm của anh được" (I
have opened the Shopee account. As for Vietnamese, it is my native language, so my own recordings can be used). Asked
whether an AI face is still needed with a real voice, the CEO chose "Mặt AI + giọng anh" (AI face plus my voice) over
dropping the avatar.

1. **Voice.** The narration of every video is the CEO's own recorded voice, in Vietnamese. The AI face lip-syncs to
   that recording. No text-to-speech is used in the pilot, and Vbee is dropped.
2. **Proposed default, not the CEO's words: no voice cloning.** Each video uses a fresh real recording of its own
   approved script; no tool is given the CEO's voice to clone. This is the orchestrator's proposed default and stands
   only when the CEO confirms it.
3. **Tool (`OD12` revised, a proposal for the CEO).** Whichever of HeyGen and D-ID passes the lip-sync test (file
   `pilot-affiliate-vn/pilot-kit/07-kich-ban-thu-va-mo-ta-nguoi-dan.md`) driven by the CEO's own uploaded recording is
   bought, billed monthly at USD 29 (HeyGen Creator or D-ID Pro), from the USD 42.99 fixed line of `OD13`. The D-ID
   Pro plus Vbee path (about USD 35.43, ESTIMATE) falls away. What the evidence says
   (`pilot-affiliate-vn/evidence/ai-presenter-pricing-2026-10-26.md`):
   - D-ID Studio accepts uploaded audio, MP3 or WAV up to 5 minutes. The quote ("upload an audio file for your avatar to
     speak") comes from a D-ID help search result, not a fetched page. D-ID Pro keeps a visible generic "AI" mark in
     the bottom left corner (D3).
   - HeyGen's acceptance of uploaded audio is not quoted anywhere in the evidence: **UNVERIFIED**, settled by the test.
   - Whether either free tier or trial accepts uploaded audio is **UNVERIFIED**, settled by the test.
4. **Everything else stands.** The "Video tạo bởi AI" label is still turned on for every video, because the face is AI.
   Scripts use only sourced facts and never claim use of the product. The disclosure is spoken at the start and the
   end. The Article 15a(3) stop rule (`OD14`), `D-002`, and one presenter on one account (`OD18`) are unchanged.
5. **Fact reported, not verified:** the CEO reports opening "the Shopee account" on 2026-10-26. No agent has seen it;
   agents never sign in. Whether it is the Shopee Affiliate registration as an individual KOC, and so whether day 1 of
   the plan is 2026-10-26, is for the CEO to state in the measurement sheet. If it is, the "before day 1" steps
   (counsel pack sent, eligibility checked) are already due.
6. **Effect on the open questions, an inference for counsel and not a finding:** the presenter now speaks in the
   CEO's real voice, which may bear on whether the CEO is presenting the product personally under Article 15a(3)
   (`Q-002`, `Q-016`). The counsel pack asks this.

**Unchanged:** `D-002` (the CEO approves every publication), `D-018`; agents create no account, sign in nowhere,
contact no seller, buy nothing, download no asset and publish nothing.

---

## CEO-D-1305 — No voice cloning: every video uses a fresh real recording

**The CEO's own decision, 2026-10-26**, given in the chat with the MC-16 session, on the proposed default of
`CEO-D-1304` item 2.

Asked "Không nhân bản giọng: mỗi video dùng một bản ghi âm thật, ghi mới. CEO xác nhận hay bác bỏ?", the CEO chose
"Xác nhận".

1. **Confirmed.** No tool is given the CEO's voice to clone, and no voice-generation or "voice clone" feature is turned
   on in the presenter tool. Each video uses a fresh real recording of its own approved script, made after that script
   is approved; sentences are never spliced from another video's recording. `CEO-D-1304` item 2 stands as a decision,
   no longer as a proposal.
2. **Effect on the kit and on MC-16.** Part F of `pilot-affiliate-vn/pilot-kit/03-mau-kich-ban-nguoi-dan-ai.md` drops
   its "awaiting confirmation" note. The MC-16 toolchain refuses to package a video whose recording is not new for that
   video: a recording whose digest was already used by another video is rejected.

**Unchanged:** `D-002`, `D-018`, `CEO-D-1304` item 1 (no text-to-speech); agents create no account, sign in nowhere,
contact no seller, buy nothing, download no asset and publish nothing.

---

## CEO-D-1306 — MC-16 scope answers: the tool writes the script, a call to action without price, pause-detected captions, 60 seconds is a hard limit

**The CEO's own decision, 2026-10-26**, given in the chat with the MC-16 session on the four blocking questions of the
MC-16 scope phase (run `run-535f3f938f32`).

1. **The tool writes the script for the CEO to record.** Asked how each fact row becomes a spoken sentence when the
   toolchain may use no language model, the CEO's words: "viết kịch bản cho CEO thu âm. sau này sẽ handover lại cho
   nhân viên là human." (write the script for the CEO to record; later this will be handed over to a human employee).
   The orchestrator reads this as: the script generator produces every spoken sentence itself, deterministically, from
   the fact sheet; the CEO does not write sentences. The person who runs the tool and records is the operator (the CEO
   now, a human employee later); the per-publication approval stays the CEO's under `D-002`. Whether the employee may
   also record the voice is not decided here: `CEO-D-1304` names the CEO's voice, and a change needs a CEO decision.
2. **The call to action carries no price.** Asked about the template's guidance sentence, which says "giá", the CEO's
   words: "hãy dùng câu dễ dẫn dắt khách hàng bằng cách bấm vào giỏ hàng, bấm link để xem chi tiết...." (use a sentence
   that leads the customer to tap the cart or the link to see details). Part B row 6 of the script template is
   replaced by a call to tap the cart or the product link for details, with no price, discount or urgency wording.
   The bare word "giá" is not a banned term (it would refuse products such as "giá đỡ điện thoại"); money amounts,
   discounts, promotions, sold counts and ratings stay banned.
3. **Caption timing by pause detection.** The CEO chose the proposed default: the tool finds the pauses between
   sentences in the recording by signal processing (no speech recognition); the operator reads with a clear pause
   between sentences; when the pauses found do not match the sentence count, the tool stops and the operator writes the
   timings by hand.
4. **Sixty seconds is a hard limit.** The CEO chose "Chặn trên 60 giây": a recording longer than 60 seconds is refused;
   the operator drops the optional third fact and records again. The 5-minute upload ceiling is moot below it.

**Unchanged:** `D-002`, `D-018`, `CEO-D-1304`, `CEO-D-1305`; agents create no account, sign in nowhere, contact no
seller, buy nothing, download no asset and publish nothing.

---

## CEO-D-1307 — MC-16 Scope Gate approved; an employee operator may record in their own real voice

**The CEO's own decision, 2026-10-26**, at the Scope Gate of run `run-535f3f938f32` (MC-16).

1. **Scope approved** as the scope phase's third attempt states it, with the defaults it carries: captions in white on
   a dark band in the lower third, one per sentence; a presenter clip that is not portrait is refused; product images
   move by a slow pan or zoom; recording reuse is detected by the file's digest only; banned class (f) is "so với",
   "hơn hẳn" and naming another shop or brand as better, not the bare word "hơn".
2. **Employee voice.** Asked whether a human employee who later operates the tool may record the narration, the CEO
   chose "Được, giọng thật" (yes, in their real voice). This amends `CEO-D-1304` item 1: the narration is the real
   recorded voice of the CEO or of the employee operating the tool. Still no text-to-speech, no voice cloning, and a
   fresh recording for every video (`CEO-D-1305`). The presenter stays one presenter on one account (`OD18`). Whether
   a different human voice behind the same AI face bears on counsel questions Q-002 and Q-016 is an inference for
   counsel, not a finding.

**Unchanged:** `D-002` (the CEO approves every publication), `D-018`; agents create no account, sign in nowhere,
contact no seller, buy nothing, download no asset and publish nothing.

---

## CEO-D-1308 — MC-16 Design Gate approved, with phone-screen safe areas

**The CEO's own decision, 2026-10-26**, at the Design Gate of run `run-535f3f938f32` (MC-16).

1. **Design approved with safe areas.** The design composes the 1080x1920 (9:16) video with the installed ffmpeg only,
   with no browser and no socket, in a separate .NET tool that never touches the company store; the pilot ledger is a
   hash-chained file outside the repository. Asked first, the CEO answered "khung hinh dien thoai di dong vi da so
   nguoi dung xem reels hoac youtube short" (a mobile phone frame, because most people watch Reels or YouTube Shorts).
   Asked again with the design restated as that frame plus safe areas, the CEO chose "Duyệt kèm vùng an toàn".
   - Captions and the overlay "Video tạo bởi AI · Có liên kết tiếp thị" sit inside a safe area, clear of the
     right-hand action buttons (like, comment, cart) and the caption and description text at the bottom of the Shopee
     Video, Reels and Shorts players.
   - The text is large enough to read on a small screen.
   The tool still publishes nowhere.
2. **Fallback for Vietnamese marks.** If the proof frame shows ffmpeg drawing a Vietnamese mark wrongly, the CEO allows
   ahead of time ("Cho phép nội bộ") the fallback: the installed browser renderer draws only the caption plates as
   transparent images. It may open loopback sockets on 127.0.0.1 only, never a connection off the machine, and the
   package states it.
3. **Clip and recording tolerance.** The presenter clip and the recording start together, and their lengths may differ
   by at most 0.5 s ("0,5 giây"). Beyond that, the tool refuses and names the difference in seconds.

**Unchanged:** `D-002`, `D-018`, `CEO-D-1304` to `CEO-D-1307`; agents create no account, sign in nowhere, contact no
seller, buy nothing, download no asset and publish nothing.

---

## CEO-D-1309 — MC-16 quality review: long captions page in two-line pages; B3 and B5 stay for the CEO to tick

**The CEO's own decision, 2026-10-27**, on the quality review of run `run-535f3f938f32` (MC-16).

1. **Caption paging.** At 56 px inside the safe area of `CEO-D-1308`, the fixed disclosure sentence needs four lines.
   The CEO chose "Chia trang 2 dòng": a sentence too long for two lines is shown as consecutive two-line pages
   within the time window in which it is spoken. Each page stays on screen for at least 1.2 s. Where the window is
   too short for that, the tool refuses and names the sentence. This amends the scope rule of one caption, of at most
   two lines, per sentence.
2. **B3 and B5 are not pre-ticked.** The price and claim-of-use checks are closed word lists. They are widened (for
   example "VNĐ", "nghìn", "triệu", and first-person wording in an operator's override), but cannot catch every
   phrasing. The CEO chose "CEO tự tích": the package states that the tool's check found nothing, and leaves B3 ("no
   claim of use") and B5 ("no price") unticked for the CEO, who reads the script. Only B8 (the "Video tạo bởi AI"
   overlay for the whole video) is pre-ticked. This amends the scope rule that pre-ticked exactly B3, B5 and B8.

**Unchanged:** `D-002`, `D-018`, `CEO-D-1304` to `CEO-D-1308`; agents create no account, sign in nowhere, contact no
seller, buy nothing, download no asset and publish nothing.

---
