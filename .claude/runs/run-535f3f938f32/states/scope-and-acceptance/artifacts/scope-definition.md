```yaml
scopeDefinition:
  scopeId: SCOPE-2026-0001
  featureName: MC-16 Shopee Video pilot production toolchain
  sourceInputs:
    - type: feature-request
      reference: runs/inputs/mc-16-feature-request.md
    - type: business-intent
      reference: runs/inputs/mc-16-business-intent.md
  producedBy: product-owner
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  scopeVerdict: bounded
  acceptanceCriteriaCount: 40
  inputDigest: sha256:ee4cb936cd638b900bf8c8bac1e840ba
  contextDigest: sha256:8432aa0d2cb1bf16c655abffc4957991
```

## Metadata

- Feature name: MC-16 Shopee Video pilot production toolchain
- Requested by: the CEO, as individual creator and sole approver of each publication (business intent, decision record entries 1301 to 1305)
- Business goal: The CEO can take one approved pilot product to one reviewable, compliant 9:16 video, with the compliance checks completed before the CEO reviews it.
- Target outcome: One vertical video package per product, approved by the CEO by hash; the tooling publishes nothing.
- Scope decision date: 2026-10-26

## Business Context

- Problem statement: Each pilot video is assembled by hand from a paper kit, so the banned-content, disclosure and fact-source rules are checked by eye for each video.
- Value hypothesis: Machine-checked rules plus one approval-by-hash record reduce the CEO's per-video checking to the items only the CEO can attest.
- Affected users: The operator who runs the tool and records the voice (the CEO now, a human employee later), and the CEO as the approver. Viewers are reached only after the CEO publishes, which this change does not do.
- Success measure: The inputs state no measure beyond the ticket's done condition (the tooling works and is tested on a fixture sample). Recorded as scope decision D-017.

## In Scope

| ID | Scope Item | Rationale | Priority |
|---|---|---|---|
| `S-001` | A product fact sheet is accepted only when each fact row carries a subject noun from the header, an attribute, a value as written on the page, its source section, and its screenshot file name; each named screenshot file exists in the input set; and the Part A header fields are complete | Ticket scope 1: facts with sources only; decision record entry 1306 item 1 (structured fact rows) | must-have |
| `S-002` | Banned content in a fact row, an operator override or a script sentence is refused, and the refusal names the row and the banned term; a script sentence that maps to no fact row or fixed sentence is refused | Ticket scope 1; kit Parts A and C; the CEO's rule against use claims and health claims (business intent item 4) | must-have |
| `S-003` | The script is generated deterministically, without a language model, in kit Part B order from the accepted fact sheet: the opening and closing disclosure sentences verbatim, one product introduction, one to three fact sentences, and one call-to-action sentence from a fixed list; the script never claims use of the product | Ticket scope 2; decision record entries 1306 items 1 and 2 | must-have |
| `S-004` | Each script sentence has a Part E source line naming its fact row or fixed sentence and its source section, for the operator to tick | Ticket scope 2: Part E source lines | must-have |
| `S-005` | A recording is admitted only as one file per video, not longer than 60 seconds, and is refused when its digest was already used by another video | Decision record entries 1305 and 1306 item 4; kit Part F | must-have |
| `S-006` | Composition does not start unless the output is vertical and the characters the captions and overlay draw are covered by the typeface the company already has installed (research risk RK-016) | Ticket scope 3, first step | must-have |
| `S-007` | The 9:16 composition shows the product images with motion and the presenter clip, at 1080x1920 | Ticket scope 3; business intent item 4 (real product images, AI presenter) | must-have |
| `S-008` | Vietnamese captions are burned in from the approved script's sentences in order; caption windows come from pauses detected in the recording, and when the pauses do not match the sentence count the operator supplies the timings by hand | Ticket scope 3; decision record entry 1306 item 3; scope decisions D-010 and D-011 | must-have |
| `S-009` | The disclosure overlay "Video tạo bởi AI · Có liên kết tiếp thị" is on screen for the whole video | Ticket scope 3; kit rule R11 | must-have |
| `S-010` | The output package holds the video file with its SHA-256, a provenance list tying each on-screen and spoken sentence to its fact row or fixed sentence, and the digest of each input | Ticket scope 4 | must-have |
| `S-011` | The package carries the checklist of kit file 04 pre-filled only for the items the tool decides, and leaves the other items unticked for the CEO | Ticket scope 4; scope decision D-005 | must-have |
| `S-012` | The CEO's publication approval is recorded against the video's SHA-256 and is void once any byte of the video changes; the tooling publishes nothing | Ticket scope 4; standing per-publication approval rule | must-have |
| `S-013` | The tooling runs with no network call, no language model, no speech recognition, no text-to-speech, and no download | Ticket hard constraints 1 to 5; decision record entry 1304 item 1 | must-have |
| `S-014` | The tooling refuses a package that would make the pilot exceed 12 videos, a third video for one product, or a second video of one product with the same focus text, counted from a ledger of packages | Scope decision D-012 (default accepted for now; the CEO confirms at the Scope Gate) | must-have |

## Out of Scope

| ID | Excluded Item | Reason | Revisit Trigger |
|---|---|---|---|
| `X-001` | Publishing, scheduling, re-posting, and any sign-in, upload or account action on Shopee, HeyGen, D-ID or another service | Agents never act on accounts or publish; the CEO publishes each video | A separate CEO decision naming a publishing scope |
| `X-002` | Opening, scraping, downloading or collecting any Shopee product page or asset; facts come only from the fact sheet | Kit Part B bars agent access to the product page; the platform's terms bar collecting platform information (evidence recaptured 2026-10-26) | A counsel answer and a CEO decision naming the permitted source |
| `X-003` | Generating the presenter clip, including the presenter tool and its account | The presenter clip is the only generative element and the CEO makes it outside the toolchain | A CEO decision; none inside the pilot |
| `X-004` | Text-to-speech, voice cloning, and speech recognition of any kind | Decision record entries 1304 item 1 and 1305; no recognition or synthesis component is permitted without approval by name | A CEO decision naming the component, with its download approved by name |
| `X-005` | Language-model text of any kind: paraphrase, translation, or generated captions | The generator is deterministic and uses no language model (ticket scope 2; decision record entry 1306 item 1) | None inside the pilot |
| `X-006` | Detecting or acting on a stop event (counsel's influencer finding, takedown within 24 hours, adverse platform answer) | Operational decisions the CEO takes under the addendum's stop rule (addendum option 14) | The CEO records a stop event |
| `X-007` | Any change to frozen Wave 12 (MC-13) or Wave 13 (MC-14) material, or to the company store database | Frozen by CEO decision; ticket hard constraint 6 | A CEO unfreeze decision |
| `X-008` | Checklist 04 items that are CEO facts or actions: the platform's AI-label toggle, product-card tagging, posting, scheduling, the day-10 start, account eligibility, counsel and platform questions, the measurement sheet | Outside the tool's knowledge or actions | None; these stay with the CEO |
| `X-009` | Batch or multi-product production, more than one presenter, and captions or scripts in a language other than Vietnamese | One product, one presenter, Vietnamese for the pilot (addendum option 18) | A CEO decision after the day-30 reading |
| `X-010` | Presenter account management: registration, enforcing one presenter across accounts, and reviewing the presenter's appearance | Outside the repository; the tool records the presenter identifier as an input digest only | A CEO decision |

## Acceptance Criteria

| ID | Criterion | Scope Ref | Verification Method | Priority |
|---|---|---|---|---|
| `A-001` | A fact row with an empty source section, or an empty screenshot file name, is refused, and the refusal names the row | `S-001` | Fixture tests: one fixture with an empty source section and one with an empty screenshot name are each refused naming the row; a complete fixture is accepted | must-have |
| `A-002` | A screenshot file name with no file in the input set is refused, and the refusal names the row and the file name | `S-001` | Fixture test: one row names a file absent from the input set; refusal names both | must-have |
| `A-003` | A fact sheet missing any of the eleven header fields (video code, product name, subject noun, classification, page path, shop name and Mall flag, read date, niche, images used, video number of the product, video focus) is refused, and the refusal names the field | `S-001` | Fixture tests: each of the eleven fields is blanked in turn; each blanked fixture is refused naming its field | must-have |
| `A-004` | A fact row missing its attribute, its value or its subject noun is refused, and the refusal names the row and the field | `S-001` | Fixture tests: the three row fields are blanked in turn; each blanked fixture is refused naming the row and the field | must-have |
| `A-005` | A fact row, an operator override or a script sentence containing a term of a banned class (a) to (g) is refused, and the refusal names the row and the term. Classes: (a) money and sales figures: amounts with the symbol "đ", the words "đồng" or "VND", amounts with the suffix "k" (for example "99k"), "giảm giá", "khuyến mãi", "mã giảm giá", "đã bán", "freeship", and star ratings (for example "5 sao"); (b) absolute words: "nhất", "duy nhất", "tốt nhất", "số một", "đỉnh nhất", "rẻ nhất"; (c) health, body and food-safety wording: "sức khỏe", "an toàn cho sức khỏe", "da", "tóc", "cơ thể", "kháng khuẩn", "không độc hại", "giảm cân", "an toàn thực phẩm"; (d) use or review wording: "mình đã dùng", "tôi dùng thử", "dùng rồi thấy", "trải nghiệm", "review", "đánh giá", "cảm nhận", "mê lắm", "xài ngon"; (e) other sales platforms: "TikTok", "Lazada", "Tiki", "Sendo"; (f) comparison: "so với", "hơn hẳn", and naming another shop or brand as better; (g) urgency: "chỉ hôm nay", "sắp hết hàng" | `S-002` | Fixture tests: one fixture row per class (a) to (g), each with one example term; seven of seven are refused naming the row and the term; one clean fixture passes | must-have |
| `A-006` | A script sentence that maps to no fact row and is not a fixed sentence is refused; this also refuses a claim about another shop or the platform that is not on the product page | `S-002` | Fixture test: one unmapped sentence is injected; the script is refused naming the sentence | must-have |
| `A-007` | Banned-term matching is whole-word, case-insensitive, after Unicode NFC normalization: "nhất định" passes; "nhất" alone is refused; "giá đỡ điện thoại" passes; "hơn" alone passes; "1,5 lít" passes | `S-002` | Fixture tests: each string is run through the check and its result compared with the stated result | must-have |
| `A-008` | The opening sentence of the script is exactly "Video tạo bởi AI. Video có liên kết tiếp thị, tôi nhận hoa hồng nếu bạn mua qua liên kết." (kit Part B row 1), byte for byte | `S-003` | String-equality test on the generated script's first sentence | must-have |
| `A-009` | The closing sentence of the script is the same disclosure string as A-008, byte for byte | `S-003` | String-equality test on the generated script's last sentence | must-have |
| `A-010` | Script order is: opening disclosure, product introduction, one to three fact sentences, call-to-action sentence, closing disclosure; a fact sheet with four fact rows is refused | `S-003` | Fixture tests: fact sheets with one, three and four fact rows; the first two produce the stated order, the third is refused | must-have |
| `A-011` | Generation is deterministic: two runs on the same fact sheet produce scripts with identical SHA-256 | `S-003` | Run the generator twice on one fixture and compare the hashes | must-have |
| `A-012` | Each spoken sentence is the generator's output from a fixed frame over the fact row's subject noun, attribute and value, with no change to the value; the sentence classes are: product introduction, each fact sentence, call-to-action, opening disclosure, closing disclosure. An operator override sentence is accepted only if it passes the banned-term check | `S-003` | Regenerate each sentence class from a fixture and compare the strings; fixture override with a banned term is refused, and a clean override passes | must-have |
| `A-013` | The call-to-action sentence is taken from a fixed list held in the template; the list has at least one entry, and each entry passes the banned-term check; a call to action containing a money amount is refused | `S-003` | Check each list entry against the banned-term check; fixture with a money amount in the call to action is refused | must-have |
| `A-014` | Part E has one row per script sentence; each row gives the fact row or fixed-sentence label and the source section text; no source cell is blank | `S-004` | Count comparison of Part E rows with script sentences; cell-emptiness check | must-have |
| `A-015` | A recording whose SHA-256 equals the SHA-256 recorded for another video is refused, and the refusal names that other video's identifier | `S-005` | Two packages built from one recording file under two video identifiers; the second is refused naming the first | must-have |
| `A-016` | A recording of 300 seconds or longer is refused (the upload ceiling, which stays enforced under A-017) | `S-005` | Duration check (ffprobe) on fixtures of 299.9 and 300.0 seconds | must-have |
| `A-017` | A recording longer than 60 seconds is refused; a recording of exactly 60.0 seconds is accepted | `S-005` | Duration check on fixtures of 60.0 and 61.0 seconds | must-have |
| `A-018` | Two recording files for one video are refused | `S-005` | Fixture test passing two recording files | must-have |
| `A-019` | Composition does not start when the vertical-output check or the character-coverage check fails, and the run record names the failing check | `S-006` | Fixture run with a landscape target: composition does not start and the record names the vertical check; a passing configuration starts | must-have |
| `A-020` | Zero characters drawn in the captions and overlay are uncovered by the installed typeface; the run record lists each drawn character with its coverage result | `S-006` | Coverage check run over the character set of the fixture captions and overlay; the record is inspected for uncovered entries | must-have |
| `A-021` | The output video is 1080 pixels wide and 1920 pixels high, with a 9:16 display aspect | `S-007` | ffprobe on the fixture output | must-have |
| `A-022` | Each of the three fixture product images shows motion: the frame-to-frame difference is non-zero during its display interval | `S-007` | Frame sampling and frame-difference comparison of the fixture output | must-have |
| `A-023` | The presenter clip is present in 100 percent of frames sampled every 0.5 seconds across the clip's duration | `S-007` | Frame sampling of the fixture output at 0.5-second steps | must-have |
| `A-024` | A presenter clip that is not portrait is refused | `S-007` | Fixture test with a landscape presenter clip | must-have |
| `A-025` | The caption text list equals the approved script's sentence list in order, with zero differences | `S-008` | Comparison of the caption list in the run record with the script | must-have |
| `A-026` | On a fixture with a clear pause between each pair of sentences, the detected pause count equals the sentence count minus one and each caption is on screen only within its window, zero violations. On a fixture where two sentences run together with no pause, the tool stops before captioning and requests manual timings | `S-008` | Fixture run with pauses of known length; frame sampling at 0.5-second steps against the windows; second fixture checked for the stop | must-have |
| `A-027` | The overlay text equals "Video tạo bởi AI · Có liên kết tiếp thị" exactly and is on screen in 100 percent of frames sampled every 0.5 seconds from the first frame to the last, plus the last frame | `S-009` | Frame sampling with an overlay-region presence check | must-have |
| `A-028` | The package holds the video file and a SHA-256 equal to the recomputed hash of that file | `S-010` | Hash recomputation and comparison | must-have |
| `A-029` | Zero on-screen or spoken sentences lack a mapping to a fact row or a fixed-sentence label | `S-010` | Count of the provenance list against the caption and script sentences | must-have |
| `A-030` | The package records a digest for each of six inputs (the fact sheet, the product images, the recording, the presenter clip, the approved script, and the manual timing file when one is used), and each recorded digest equals the recomputed digest | `S-010` | Digest recomputation and comparison | must-have |
| `A-031` | The checklist pre-fill ticks exactly three items, 04 B3, B5 and B8, each with the tool's check result attached | `S-011` | Inspection of the generated checklist | must-have |
| `A-032` | The checklist pre-fill leaves unticked exactly these 24 items: A1, A2, A3, A4, A5, A6, A7, A8, B1, B2, B4, B6, B7, B9, B10, B11, B12, B13, C1, C2, C3, C4, D1, D2; zero of them carry a tick | `S-011` | Inspection of the generated checklist | must-have |
| `A-033` | A CEO approval record is accepted only when its SHA-256 equals the recomputed hash of the video; a fixture with one byte of the video changed after approval is refused | `S-012` | Byte-change test on the fixture package | must-have |
| `A-034` | The sample run makes zero network connection attempts | `S-013` | Run under a harness that fails on any socket opening; recorded count is zero | must-have |
| `A-035` | The dependency list and the invoked-tool log contain zero language-model, speech-recognition or text-to-speech components | `S-013` | Inspection of the dependency list and the invoked-tool log | must-have |
| `A-036` | The verification sample is produced at test time from repository code only (ffmpeg generated sources, code-drawn images, synthesized tones), and MC-16 adds zero image, audio or video files to the repository | `S-013` | Scan of the repository's tracked files for media extensions; zero MC-16 matches | must-have |
| `A-037` | A manual timing file is accepted only when it gives one start and one end per script sentence, in order, within the recording's duration; otherwise it is refused naming the first failing sentence | `S-008` | Fixture tests: a complete file is accepted; a file with one sentence missing, and a file ending after the recording, are each refused naming the sentence | must-have |
| `A-038` | A package that would be the thirteenth video of the pilot is refused | `S-014` | Fixture ledger with twelve entries; the thirteenth package is refused | must-have |
| `A-039` | A package that would be the third video of one product is refused | `S-014` | Fixture ledger with two entries for one product; the third package is refused naming the product | must-have |
| `A-040` | A second video of one product with the same focus text as the first is refused | `S-014` | Fixture ledger with one entry; a package with identical focus text is refused and a package with different focus text passes | must-have |

## Constraints and Dependencies

- Business constraints: The CEO approves each publication individually. One presenter on one account. The tooling costs nothing to run; the budget and the metered ceiling are the CEO's and are not changed by this scope.
- Regulatory or policy constraints: Each video carries the AI-label overlay and the disclosure at its start and end. No use or review claims; no health, skin, hair or body claims; no money amounts, discounts or promotions; no other platform names; no text-to-speech and no voice cloning (decision record entries 1304 and 1305). The seller-permission route is replaced by the CEO's ruling of decision record entry 1303, with the CEO carrying the risk. The counsel stop rule applies outside the tool. Agents create no account, sign in nowhere, contact no seller, buy nothing and publish nothing.
- Delivery constraints: No download of any asset, package, font, model or tool without the CEO's approval by name. The toolchain uses only components already installed through the approved Waves 10 and 11 set. Frozen Wave 12 and Wave 13 material and the company store are untouched. Publication cannot start before day 10; that gate is the CEO's and lies outside this change. The tool is operated by the operator, who may later be a human employee.
- External dependencies: The operator supplies the fact sheet, the product images, the recording, the presenter clip and, when pause detection does not match, the manual timing file. The presenter tool (HeyGen or D-ID) and the CEO's account are outside the toolchain. Counsel and platform answers are outside the toolchain. The installed renderer, headless browser, typeface, media tools and runtime named in the ticket are existing dependencies.

## Scope Decisions

| ID | Decision | Rationale | Impact | Decided By |
|---|---|---|---|---|
| `D-001` | Existing components are reused: the delivered renderer, the character-coverage check, the owner approval by hash and the held-stage refusal. No second renderer or approval path is in scope | The ticket requires reuse, and the done condition is a tested pipeline on the existing base | The scope bounds behaviour; the mechanism stays with the architect | product-owner; CEO confirms at the Scope Gate |
| `D-002` | Banned-term matching is whole-word, case-insensitive, after Unicode NFC normalization | A substring match would refuse ordinary words that contain a banned term, such as "nhất định" or "giá đỡ điện thoại" | A-007 is the test | product-owner |
| `D-003` | Captions and the overlay are drawn only from the approved script and the fixed overlay string; the generator produces each spoken sentence from structured fact rows by a fixed frame, with no language model | The toolchain calls no generative model; the operator does not write sentences (decision record entry 1306 item 1) | The spoken and on-screen sentences are identical strings | product-owner, on the CEO's answer in decision record entry 1306 item 1 |
| `D-004` | Recording reuse is refused by digest equality of the file only | Detecting a re-encoded or spliced copy would need audio comparison or speech recognition, which the constraints exclude | A re-encoded copy of another video's recording passes; the CEO's fresh-recording attestation remains the control | product-owner; default accepted for now, the CEO confirms at the Scope Gate |
| `D-005` | The tool pre-fills only checklist 04 items B3, B5 and B8, which it can decide from the artifacts it holds | The other items need CEO facts (page match, the recording as read, account state) or CEO actions (posting) | The 24 items of A-032 stay unticked for the CEO | product-owner |
| `D-006` | The RK-016 check runs before composition, and a failure stops composition | The ticket places it first; the research left the mechanism to the architect, so only the behaviour is fixed here | No composition output exists for a failed check | product-owner |
| `D-007` | A recording longer than 60 seconds is refused; the limit is hard | The CEO chose a hard limit; the operator drops the optional third fact and records again (decision record entry 1306 item 4) | A-017 is must-have; the 300-second ceiling of A-016 becomes moot below it | CEO, decision record entry 1306 item 4 |
| `D-008` | The guidance sentence is replaced by a call-to-action sentence from a fixed list, which tells the viewer to tap the cart or the product link for details, with no price, discount or urgency wording. The bare word "giá" is not banned; money amounts are | The CEO's words in decision record entry 1306 item 2; the bare word would refuse products such as a phone stand | A-013 holds the list rule; the proposed first entry is "Bấm vào giỏ hàng hoặc link sản phẩm để xem chi tiết." and the architect writes the final list | CEO, decision record entry 1306 item 2 |
| `D-009` | One recording file per video; no concatenation of recordings | Decision record entry 1305 bans reuse of sentences from other videos' recordings | A-018 | product-owner |
| `D-010` | Caption windows come from pause detection in the recording (signal processing, no speech recognition); a mismatch between the pauses found and the sentence count stops the run until the operator supplies manual timings | The CEO's choice in decision record entry 1306 item 3 | S-008, A-026 and A-037 | CEO, decision record entry 1306 item 3 |
| `D-011` | Captions come from the approved script text as read by the operator; the operator's written attestation that the recording reads the script is the only check that it does | The narrow reading of the ticket's "script actually recorded", since speech recognition is excluded; this is the orchestrator's relayed default, not a CEO answer | The tool does not verify the spoken words | product-owner, on the orchestrator's default; the CEO confirms at the Scope Gate |
| `D-012` | The tool enforces the pilot's limits of 12 videos, two per product and distinct focus text from a ledger of packages; "no video posted today" stays with the CEO | Accepted default for the count rules | S-014 and A-038 to A-040; the ledger's location is for the architect | product-owner; default accepted for now, the CEO confirms at the Scope Gate |
| `D-013` | Caption style: the company's installed typeface, white text on a dark band in the lower third, at most two lines, one caption per script sentence. The caption must read on a phone over moving product images; one caption per sentence keeps each caption equal to one recorded sentence, so provenance stays one to one; the installed typeface is the one measured to cover Vietnamese (RK-016 part 1, 0 missing of 204 code points) | Legibility on a phone over moving images, and one-to-one provenance between captions and recorded sentences; the typeface choice rests on the measured coverage result | Style only; the mechanism stays with the architect | product-owner, on the orchestrator's default; the CEO confirms at the Scope Gate |
| `D-014` | A presenter clip that is not portrait is refused; the CEO regenerates it at 9:16 | Cropping or letterboxing a landscape presenter would cut or shrink the face, which the lip-sync check (checklist 04 item B9) judges; the tool exports 9:16 natively (test T1, criterion 5) | A-024 | product-owner, on the orchestrator's default; the CEO confirms at the Scope Gate |
| `D-015` | Product-image motion is a slow linear pan or zoom, with no transition that implies use | A slow pan or zoom shows the product image itself without altering it, and the image must show the tagged product (kit rule R12); no transition implies the product is being used (kit Part C, rule R4) | A-022 holds for any slow pan or zoom | product-owner, on the orchestrator's default; the CEO confirms at the Scope Gate |
| `D-016` | Class (f) of the banned terms is "so với", "hơn hẳn", and naming another shop or brand as better. The bare word "hơn" is not banned | The bare word would refuse ordinary sizes such as "nhỏ hơn"; the CEO did not answer the comparison question | A-005 and A-007 | product-owner, on the orchestrator's default; the CEO confirms at the Scope Gate |
| `D-017` | The success measure of this change is the ticket's done condition (the tool works and is tested on a fixture sample); the pilot's 30-day measure stays with the CEO's measurement sheet | The inputs state no other measure; this is the orchestrator's default | Business Context | product-owner, on the orchestrator's default; the CEO confirms at the Scope Gate |

## Open Questions

| ID | Question | Blocking | Owner | Needed By |
|---|---|---|---|---|
| `Q-001` | The tool writes the script for the operator to record, and the operator may later be a human employee. Is the employee allowed to record the voice? The CEO's decision names the CEO's own voice (decision record entry 1304), so a change needs a CEO decision. The tool does not check who records | no | CEO | Before any handover of recording to an employee |

## Handoff

- Downstream owner: planner (execution planning runs in parallel with this phase) and architect for the technical approach. The Scope Gate decision rests with business-analyst, since product-owner produced this artifact.
- Gate: Scope Gate of implement-feature (owners: product-owner, business-analyst; producer exclusion applies).
- Evidence for the gate: the identifiers S-001 to S-014, X-001 to X-010, A-001 to A-040, D-001 to D-017 and Q-001 in this artifact. Status complete with verdict bounded; no blocking open question stands. The four blocking questions of the earlier attempt are answered by decision record entry 1306 and by the coordinator's relayed answers, and are recorded as scope decisions D-003, D-007, D-008, D-010 and D-011. No gate decision and no readiness claim is recorded here.
- Deferred to downstream: decomposition and sequencing (planner); the renderer, the character-coverage mechanism, the pause-detection method and its threshold, the fixed frame wording, the ledger location, the motion implementation and the typeface choice (architect); the test strategy and validation plan (qa, against the testing skill); the Scope Gate decision (business-analyst).
