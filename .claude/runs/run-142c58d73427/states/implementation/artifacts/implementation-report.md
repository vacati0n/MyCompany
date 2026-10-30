```yaml
implementationReport:
  reportId: IR-2026-0036
  changeReference: MC-19 one-press button in the operator's own Chrome that copies a shopee.vn product page's visible facts for one paste at step 1 of the MC-17 web app, run run-142c58d73427, invocation inv-142c58d73427-04-001
  sourceInputs:
    - type: feature-request
      reference: runs/inputs/mc-19-feature-request.md
    - type: technical-design
      reference: runs/run-142c58d73427/states/solution-design-and-risk-assessment/artifacts/technical-design.md
    - type: execution-plan
      reference: runs/run-142c58d73427/states/execution-planning/artifacts/execution-plan.md
  producedBy: dev-1-implement
  agentVersion: 1.1.0
  schemaVersion: 1.0.0
  status: complete
  workflowPhase: implementation
  verificationStatus: verified
  inputDigest: sha256:a81eb08b4f4c6f1ab9a61977dadd6ca0
  contextDigest: sha256:f19a60b2df50db05b4d00e11d9687b71
```

## Metadata

- Report ID: IR-2026-0036
- Change reference: MC-19 one-press button in the operator's own Chrome that copies a shopee.vn product page's visible facts for one paste at step 1 of the MC-17 web app, run run-142c58d73427, invocation inv-142c58d73427-04-001
- Workflow phase: implementation
- Status: complete
- Verification status: verified
- Review status: pending-review

## Implementation Summary

- Change intent: The operator drags the "Đọc trang Shopee" button from the app's "Cài đặt" view to Chrome's bookmark bar once. Then, on a shopee.vn product page open in their own browser, one press copies one block of that page's visible facts to the clipboard: product name, classification, shop name and Mall, page address, press date, the "CHI TIẾT SẢN PHẨM" rows and the product's image addresses. One Ctrl+V at step 1 of the open video fills the same MC-18 fill record, with the new source mark "Từ nút đọc trang". Each filled field and row must be confirmed ("Xác nhận") before the save. The app fetches only the block's own image addresses, under the MC-18 image rules. One screenshot can be named on the button-filled rows in one action. The unchanged toolchain check still decides the fact sheet. The press sends nothing, loads no code, clicks nothing and reads no cookie or stored value. The app sends nothing to any shopee.vn page. The MC-18 read switch, off by default, also governs the paste of a block.
- Approach taken: The button is one plain script file, Pages/nut-doc-trang.js. It is embedded in the app assembly and never served as a page. ButtonBuilder.cs turns it into the bookmark address: comment lines dropped, the configuration placed, the result percent-encoded after "javascript:". The button mark is the first 12 hexadecimal digits of the SHA-256 of the script as held (D-006). The script finds fields by visible-text anchors (D-003) and copies through the chain of D-007: the asynchronous clipboard write; then the copy command on an off-screen selected box; then a visible selected box with "Nhấn Ctrl+C để chép". BlockReader.cs reads a pasted block strictly and in a fixed order (D-002, D-010, D-014, D-016). The guarded /api/paste route sends any text that starts with the marker down the new block path (D-001, D-015); any other text keeps the MC-18 paste path unchanged. ProductRead.cs gains FromBlock, which reuses the MC-18 image phase, now extracted as FetchImages (D-005). FillRecord.cs gains FromButtonBlock, the "button" source and its wording (D-009). The test seam adds one internal member, the exact fixture origin, on the internal transport (D-004). Necessity ladder: rung 1, each change-set entry C-001 to C-017 implements a Design Gate decision named in its Design Ref (D-001 to D-016), and nothing beyond them was built. Rung 2 reuses, as they are: the request guard, the read switch, the fill record and its confirmation rule, the MC-18 image phase and image-host rule, the read log, the outcome display, the fact-sheet and banned-term checks, the socket harness, the digest-checked headless shell, its launch helper and the runner. Rung 3 is the .NET base class library for SHA-256, JSON, the strict reader and the test-side DevTools client (ClientWebSocket on 127.0.0.1). Rung 7 adds three production files, because no existing component builds a bookmark, runs in the operator's browser or reads a block. It also adds five test files, because the fixture pages, the DevTools client and the cases have no MC-18 counterpart. No package, script library, font or framework dependency was added, and no project file changed.
- Design reference: technical-design.md decisions D-001 to D-016, with delivery steps P-001 to P-009. Plan tasks 003, 004, 006, 007, 008, 009, 011, 014, 015, 016, 018, 020 and 024. Success criteria SC-01 to SC-42; SC-19 is withdrawn and its converse applies. Scope Gate decision CEO-D-1319. Design Gate decision CEO-D-1320: images fetched after the paste for picking, the fixture grounded on the CEO's screenshot, and the source mark wording.
- Out of scope:
  - No agent opened shopee.vn or any Shopee host. No command in this phase connected off 127.0.0.1; the pages the button ran on were the code-generated fixture pages listed under C-012, served on 127.0.0.1.
  - The CEO's single press on a real page (SC-42) and the orchestrator's Browser-pane record (SC-41) come after quality review and belong to the CEO and the orchestrator.
  - The release note and the compare link follow this phase.
  - src/MediaCompany.Pilot and tests/MediaCompany.Pilot.Tests are byte-unchanged.
  - MC-13 and MC-14 material and the company store database were not touched.
  - Nothing was signed in to, bought, published or downloaded.

## Change Set

| ID | Path | Change Type | Purpose | Design Ref |
|---|---|---|---|---|
| C-001 | src/MediaCompany.Pilot.Web/Pages/nut-doc-trang.js | added | The button script. It runs in the operator's browser on a shopee.vn page (or the exact fixture origin in tests) and finds fields by visible-text anchors. It copies one block (marker line plus JSON at format version 1, with the button mark) through the asynchronous write, then the copy command, then the visible box with "Nhấn Ctrl+C để chép". It caps the block at 30 images, 100 rows, 4000 characters per text and 256 KiB, with Vietnamese notices. It refuses pages off shopee.vn and pages with no product name and no "CHI TIẾT SẢN PHẨM". Its notices are set through textContent. Relative image addresses are resolved through a detached anchor element (see V-006) | D-002; D-003; D-006; D-007; D-011; D-014 |
| C-002 | src/MediaCompany.Pilot.Web/ButtonBuilder.cs | added | Builds the bookmark address from the embedded script: comment lines dropped, configuration (format version, mark and, only through the internal seam, the fixture origin) placed, result percent-encoded after "javascript:", at most 32 KiB. The mark is the first 12 hexadecimal digits of the SHA-256 of the script as held | D-004; D-006; D-012 |
| C-003 | src/MediaCompany.Pilot.Web/BlockReader.cs | added | Strict block reader. Checks, in order: size bound (256 KiB); marker line; JSON (one object, depth at most 8, no trailing content, no repeated member); format version and button mark; unknown members; member types; bounds (30 images, 100 rows, 4000 characters); product name; page address rule (https shopee.vn or a subdomain, no affiliate host, port, user name or IP literal; or the exact fixture origin in tests). Refusal codes are block-* with a Vietnamese reason | D-002; D-006; D-010; D-014; D-016 |
| C-004 | src/MediaCompany.Pilot.Web/ReadSender.cs | modified | The internal transport gains one optional member, the exact fixture origin, which the shipped app never sets | D-004 |
| C-005 | src/MediaCompany.Pilot.Web/FillRecord.cs | modified | FromButtonBlock builds the fill record with the source "button": name, classification, page address, shopNameAndMall and the read date (a press date later than today, or not yyyy-MM-dd, is left for the operator); the operator-only member list per source; and the confirmation wording "được điền từ nút đọc trang" for the button source | D-009 |
| C-006 | src/MediaCompany.Pilot.Web/ProductRead.cs | modified | The MC-18 image phase is extracted as FetchImages and called by both the read and the new FromBlock. FromBlock fetches at most 20 of the block's own image addresses under the MC-18 image rules and the address rule of D-016, which also refuses the page's own address. It requests no page | D-005; D-016 |
| C-007 | src/MediaCompany.Pilot.Web/Routes.cs | modified | /api/paste sends text that starts with the marker to PasteBlock. PasteBlock refuses, in order: more than one block, switch off, no video open, then the reader's refusals, each filling nothing. An accepted block replaces the video's fill record under the app-wide read gate. The new GET /api/button returns the bookmark address, its size, the format version and the mark. Both routes sit behind the unchanged MC-17 guard | D-001; D-010; D-012; D-015 |
| C-008 | src/MediaCompany.Pilot.Web/Pages/index.html | modified | The "Cài đặt" view gains the button section with Vietnamese drag instructions. Step 1 gains the button note and the one-action screenshot control for button-filled rows | D-008; D-012 |
| C-009 | src/MediaCompany.Pilot.Web/Pages/app.js | modified | Source names for the new mark, the paste listener on step 1, the block paste call and its outcome display, and the one-screenshot upload that names one file on each button-filled row, including rows added later. The settings button is loaded from /api/button. A click on the button inside the app is cancelled with "Hãy kéo nút này lên thanh dấu trang…" (see V-001) | D-008; D-009; D-012; D-015 |
| C-010 | src/MediaCompany.Pilot.Web/Pages/app.css | modified | Plain style for the bookmark link in "Cài đặt"; nothing fetched | D-012 |
| C-011 | tests/MediaCompany.Pilot.Web.Tests/Cdp.cs | added | A minimal DevTools protocol client (BCL ClientWebSocket) over the headless shell's loopback debugging port: commands, evaluation, mouse clicks, Ctrl key chords and the page's request events | D-004 |
| C-012 | tests/MediaCompany.Pilot.Web.Tests/ButtonFixture.cs | added | A plain-HTTP page server on 127.0.0.1 and the code-generated fixture site, grounded on the CEO's screenshot. Pages: the product page; non-Mall, signed-in, 34-image and 106-row, markup-in-text and no-name pages; a shop page and a search page; and a probe that counts clicks, submissions, copies, unloads and navigations | D-004 |
| C-013 | tests/MediaCompany.Pilot.Web.Tests/ButtonCases.cs | added | The button case class, the headless-shell wrapper with its socket harness, and program case 067 (the clipboard measurement for the design's clipboard question 005) | D-007 |
| C-014 | tests/MediaCompany.Pilot.Web.Tests/ButtonServerCases.cs | added | Program cases 068 to 076: builder, reader, route order, good block and refusals, image addresses, save, banned terms, guard, and the shipped executable | D-001 to D-016 |
| C-015 | tests/MediaCompany.Pilot.Web.Tests/ButtonBrowserCases.cs | added | Program cases 077 (the whole flow in the headless shell) and 078 (the other fixture pages) | D-003; D-007; D-008; D-009; D-011; D-012; D-014 |
| C-016 | tests/MediaCompany.Pilot.Web.Tests/ReadHost.cs | modified | The internal app start gains a fixture-origin argument that is passed into the internal transport | D-004 |
| C-017 | tests/MediaCompany.Pilot.Web.Tests/Runner.cs | modified | Registers the button cases after the MC-18 read cases | D-004 |

## Test Evidence

| ID | Test | Type | Covers | Command | Result |
|---|---|---|---|---|---|
| T-001 | The web app and its test program build: 0 warnings, 0 errors | static | C-001, C-002, C-003, C-004, C-005, C-006, C-007, C-011, C-012, C-013, C-014, C-015, C-016, C-017 | dotnet build tests/MediaCompany.Pilot.Web.Tests | pass |
| T-002 | Program cases 001 to 008. Case 008's page scan (no external address, no @import, no CSS url) passes over the new nut-doc-trang.js after V-006; its networking deny list still finds networking only in ReadSender.cs; no package reference, no media file | unit | C-001, C-004, C-008, C-009, C-010 | dotnet run --no-build --project tests/MediaCompany.Pilot.Web.Tests | pass |
| T-003 | The 36 MC-17 cases pass unchanged with the paste route, settings view and step 1 changed | regression | C-007, C-008, C-009, C-010, C-017 | dotnet run --no-build --project tests/MediaCompany.Pilot.Web.Tests | pass |
| T-004 | MC-18 program cases 050 to 066 pass unchanged: the read, the paste of page text (case 056 fills exactly what the read fills), the switch, the image-phase stops and the guard on the read routes, after FetchImages was extracted | regression | C-004, C-005, C-006, C-007, C-016 | dotnet run --no-build --project tests/MediaCompany.Pilot.Web.Tests | pass |
| T-005 | Program case 067, the clipboard measurement on the fixture product page in the headless shell (D-007 and the design's clipboard question 005). Five runs (A0, A, B, C, D) each ended with the 1151-character block on the clipboard:<br>- A0, the shell's own default: the asynchronous write is refused with "Write permission denied", and the press ends by the copy command (1 copy event).<br>- A, a press with the write permission desktop Chrome grants a focused page: the asynchronous write works, 0 copy events.<br>- B, the asynchronous write refused: the copy command works.<br>- C, both refused: the visible box is focused with 1151 of 1151 characters selected, the operator's Ctrl+C copies, and the box is removed.<br>- D, no user activation: ended by the copy command.<br>The page server saw only the page and its probe script; 0 browser TCP rows off loopback | end-to-end | C-001, C-002, C-011, C-012, C-013 | dotnet run --no-build --project tests/MediaCompany.Pilot.Web.Tests | pass |
| T-006 | Program case 068, builder. The mark equals the first 12 hexadecimal digits of the script's SHA-256; the shipped address is at most 32 KiB; the shipped button carries no fixture origin while the test build carries the exact one; 26 request, navigation, click, submit, cookie and storage interfaces are absent from the script | unit | C-001, C-002 | dotnet run --no-build --project tests/MediaCompany.Pilot.Web.Tests | pass |
| T-007 | Program case 069, reader alone: 59 cases, 59 as expected.<br>- The 256 KiB bound at the byte (255 KiB, exactly 256 KiB and 256 KiB plus one byte).<br>- Marker and JSON forms.<br>- Version and mark (format version 0, 2, text or missing; another mark or none).<br>- Unknown members and wrong types.<br>- 30 and 31 images, 100 and 101 rows, 4000 and 4001 characters.<br>- No or blank name.<br>- 14 page addresses, including http, the affiliate host, ports 8443 and 443, an IP literal, a user name, look-alike hosts, a javascript address, and the fixture origin with and without the seam.<br>- 6 pairs proving the check order | unit | C-003 | dotnet run --no-build --project tests/MediaCompany.Pilot.Web.Tests | pass |
| T-008 | Program cases 070 and 071.<br>- Case 070, route order in 7 combinations: two blocks give block-two-products before the switch-off or missing-video refusals; switch off gives block-read-off (naming "Cài đặt") before a missing video or a broken block; no video gives block-no-video before a broken block. None fills anything or connects.<br>- Case 071, good block: name, classification, page address, shop "Gian hàng mẫu MC, Mall" and the press date filled; 5 rows offered; the five own images fetched with no page request. Variants give the expected fields with 0 requests: not Mall, no shop, no classification, a press date tomorrow or malformed (left out), an earlier date, no images. Ten refusals leave the earlier fill byte-unchanged | integration | C-003, C-005, C-006, C-007 | dotnet run --no-build --project tests/MediaCompany.Pilot.Web.Tests | pass |
| T-009 | Program case 072, image addresses.<br>- Of seven addresses, the two own images are fetched. The other five are named with a Vietnamese reason and get no request: http, a foreign host, a port, an IP address and the page's own address.<br>- A redirect (image-redirect) and a JSON answer (not-image) each end the image phase with the facts and the first image kept | integration | C-006, C-007 | dotnet run --no-build --project tests/MediaCompany.Pilot.Web.Tests | pass |
| T-010 | Program cases 073 and 074.<br>- Case 073: an empty operator field is refused by name. With N = 8 marks (productName, classification, pagePath, shopNameAndMall, readDate and three rows), none confirmed and N-1 confirmed are each refused fill-unconfirmed with the "nút đọc trang" wording; N confirmed is accepted. Picked images are placed byte-equal; price and description are never saved.<br>- Case 074: eight banned-term values, classes (a) to (g) plus a money amount, carried in block rows give the typed save's code and line, 8 of 8 | integration | C-005, C-007 | dotnet run --no-build --project tests/MediaCompany.Pilot.Web.Tests | pass |
| T-011 | Program case 075, the guard on /api/paste with a block. Nine forms are refused 403 with nothing filled and no connection: a foreign Origin, another port, localhost, a "null" Origin and no Origin (origin-not-allowed); no token, a wrong token and an earlier run's token (token-not-valid); and a foreign Host (host-not-allowed). The guarded control request is accepted | integration | C-007 | dotnet run --no-build --project tests/MediaCompany.Pilot.Web.Tests | pass |
| T-012 | Program case 076, the shipped executable started as a child process: /api/button carries no fixture origin and the same mark as the builder; a block whose page is on 127.0.0.1 is refused block-page-not-allowed; a shopee.vn block with no images fills with 0 requests; the app's socket rows are 3, with 0 off loopback | integration | C-002, C-003, C-004, C-007 | dotnet run --no-build --project tests/MediaCompany.Pilot.Web.Tests | pass |
| T-013 | Program case 077, the whole flow in the headless shell.<br>- The button is taken from "Cài đặt"; a click on it inside the app does not run it.<br>- Pressed on the fixture product page, the probe shows no click, submission, unload or navigation caused by the press, 0 page requests and 0 page-server requests.<br>- Ctrl+V at step 1 fills N = 8 fields and rows marked "Từ nút đọc trang". Only the five own images (mc18-own-1 to 5) are fetched. One screenshot (chup-1.png) is named on the 3 button-filled rows in one action.<br>- N-1 confirmations are refused naming row F3 with the "nút đọc trang" wording; N are accepted.<br>- With the switch off: block-read-off and 0 requests. With no video: block-no-video.<br>- Browser TCP rows: 8, with 0 off loopback | end-to-end | C-001, C-002, C-005, C-006, C-007, C-008, C-009, C-010, C-011, C-012, C-015 | dotnet run --no-build --project tests/MediaCompany.Pilot.Web.Tests | pass |
| T-014 | Program case 078, the other pages in the headless shell:<br>- The non-Mall page copies 697 characters.<br>- The shop page, the search page and the page with no product name copy nothing, with the "không phải trang của một sản phẩm" notice.<br>- A page on the app's own origin copies nothing, with the "không phải shopee.vn" notice.<br>- The signed-in page copies 1157 characters, with the account, cookie, stored values and SPC_U absent, and the reader accepts it.<br>- The 34-image and 106-row page copies 30 images and 100 rows with both notices (10447 characters).<br>- Markup in the page text is shown as text in the notice and in the app; no element is made | end-to-end | C-001, C-003, C-009, C-012, C-015 | dotnet run --no-build --project tests/MediaCompany.Pilot.Web.Tests | pass |
| T-015 | MC-16 pilot regression: 251 of 251, unchanged | regression | C-004, C-016 | dotnet test tests/MediaCompany.Pilot.Tests | pass |
| T-016 | Toolchain unchanged: no diff under src/MediaCompany.Pilot or tests/MediaCompany.Pilot.Tests. The eight new files are source files only, with no media file and no project-file change | static | C-001, C-002, C-003, C-011, C-012, C-013, C-014, C-015 | git diff --quiet HEAD -- src/MediaCompany.Pilot tests/MediaCompany.Pilot.Tests; git status --porcelain | pass |

## Verification Results

- Verification method: The web app and its test program were built. The whole test program then ran end to end:
  - The MC-17 cases ran against the built app.
  - The MC-18 read cases ran against the loopback TLS fixture.
  - The new button cases ran against code-generated fixture pages on a plain-HTTP page server on 127.0.0.1. The button ran in the digest-checked headless shell, driven over its loopback debugging port, and the app was started through the internal start path with the exact fixture origin.
  - Program case 076 ran the shipped executable.

  The socket harness watched the shell and the app. The MC-16 pilot suite was re-run, and the toolchain folders were compared with the last commit. The first full run gave 64 passed, 1 failed, 65 run in 484 s. Program case 008's page scan matched the URL constructor in the new script. The script was changed (V-006), rebuilt, and the whole program was run again; that second run is the result below.
- Commands executed: `dotnet build tests/MediaCompany.Pilot.Web.Tests`; `dotnet run --no-build --project tests/MediaCompany.Pilot.Web.Tests` (twice, before and after V-006); `dotnet test tests/MediaCompany.Pilot.Tests` (twice, before and after V-006); `git diff --quiet HEAD -- src/MediaCompany.Pilot tests/MediaCompany.Pilot.Tests`; `git status --porcelain`
- Result summary:
  - Evidence entries: 16 executed, 16 passed, 0 failed.
  - Build: 0 warnings, 0 errors.
  - Web test program: 65 of 65 in 485 s on the second run (the 36 MC-17 cases, MC-18 program cases 050 to 066, and program cases 067 to 078). The baseline before the change was 53 of 53.
  - MC-16 pilot: 251 of 251, both before V-006 (3 min 6 s) and after it (6 min 34 s).
  - Toolchain diff: empty.
  - Clipboard measurement: the five runs A0, A, B, C and D each ended with the block on the clipboard.
  - Button mark on this checkout: 419110b95aab. Shipped bookmark address: 17782 bytes of 32768.
  - Connections off 127.0.0.1 in any run: 0. Live presses or reads: 0.
- Unverified areas: none

## Deviations and Tradeoffs

| ID | Deviation | Design element | Rationale | Escalation |
|---|---|---|---|---|
| V-001 | Besides the unchanged content policy that blocks a javascript: address, app.js cancels a click on the button inside the app and shows "Hãy kéo nút này lên thanh dấu trang…" (C-009) | D-012 | D-012 says the policy blocks a click and the view tells the operator to drag. Cancelling the click shows that message where the click happens, instead of a silent policy refusal. The policy is unchanged | not-required |
| V-002 | The MC-18 image phase was extracted from ProductRead.Run as FetchImages, so the read and the block path share one implementation (C-006) | D-005 | Two copies of the image rules could drift. MC-18 program cases 050 to 066 pass unchanged after the extraction (T-004) | not-required |
| V-003 | The browser cases drive the headless shell over its DevTools protocol on the loopback debugging port (`--remote-debugging-port=0`), using the BCL ClientWebSocket (C-011) | D-004 test seam | A press needs real user activation, clipboard access and key chords, which the MC-17 command-line browser case cannot give. The port is on 127.0.0.1 only, and the harness shows 0 rows off loopback | not-required |
| V-004 | Program case 076 checks the shipped surface by starting the shipped executable as a child process, not through the internal start path (C-014) | D-004 | Only the shipped executable proves the shipped app carries no fixture origin | not-required |
| V-005 | In the measurement and the whole-flow case, the test grants the clipboard write permission that desktop Chrome gives a focused page. The headless shell has no permission prompt and refuses by default. Run A0 records that default, and the chain still ends with the block copied (C-013, C-015) | D-007 | The grant is the documented desktop behaviour; A0 shows the chain also works without it. Real Chrome on shopee.vn is not measured here (R-002) | Raised to qa as an open question |
| V-006 | The script resolves a relative image address through a detached anchor element instead of the URL constructor (C-001) | D-003; D-016 | The MC-17 page scan in program case 008 forbids the text "url(" in any page file. A detached anchor resolves the address without loading anything, and case 008 stays unchanged. The app still applies the D-016 address rule to each address it receives | not-required |

## Boundary Compliance

- Module boundaries preserved:
  - src/MediaCompany.Pilot and tests/MediaCompany.Pilot.Tests have no diff (T-016).
  - Networking in the app is still confined to ReadSender.cs (T-002).
  - The button script holds none of the 26 request, navigation, click, submit, cookie or storage interfaces (T-006).
  - The fact sheet is still decided by the toolchain's check, and banned terms by its held list (T-010).
  - The fixture origin travels only on the internal transport; the shipped executable carries none (T-012).
  - The request guard is not widened (T-011).
- Public interface changes:
  - New loopback route GET /api/button, behind the MC-17 guard.
  - /api/paste now also accepts a block that starts with the marker line; any other text keeps the MC-18 paste path.
  - New refusal codes: block-two-products, block-read-off, block-no-video, block-too-large, block-unreadable, block-wrong-type, block-old-button, block-unknown-version, block-unknown-member, block-too-many-images, block-too-many-rows, block-text-too-long, block-no-name, block-page-not-allowed.
  - New fill source "button", shown as "Từ nút đọc trang".
  - The block format is version 1, carrying the button mark.
  - The app's command-line options and exit codes are unchanged, and so are the toolchain's verbs, exit codes and refusal codes.
- Data or migration impact:
  - None in the company store.
  - The MC-18 fill record per video gains the "button" source value and the shopNameAndMall field; existing records still read.
  - No new switch: the MC-18 switch file governs the block path.
- Declared side effects: The 17 files of the change set (C-001 to C-017), plus this artifact and the result envelope under runs/run-142c58d73427/states/implementation/. Test runs wrote only to temporary folders under the system temp folder; build outputs went to bin and obj. The browser cases wrote to the machine's clipboard (R-001). No media file was added to the repository, and no network connection off 127.0.0.1 was made.

## Residual Risk

| ID | Risk | Likelihood | Impact | Mitigation in place |
|---|---|---|---|---|
| R-001 | The browser cases write to and clear the Windows clipboard of the machine that runs them | high | Text the person running the tests had copied is lost | The cases only run on demand. The block holds fixture data only. The clipboard is set to a known value before each run |
| R-002 | Chrome's clipboard behaviour on a real shopee.vn page is not measured. The page's own policies or a refused permission could change which path of the chain runs | medium | The press may end at the visible box and need the operator's Ctrl+C | The chain ended with the block copied in each of the five measured runs (A0, A, B, C, D), including no activation and both automatic paths refused (T-005). The CEO's real-page try (SC-42) is the first real evidence |
| R-003 | The fixture page is a model of the real page, grounded on the CEO's screenshot, not a copy. Real anchors or structure may differ | medium | Fields could be missed or the page read as not a product page | Each filled field and row (N = 8 in T-010 and T-013) must be confirmed. A page without the anchors copies nothing with a Vietnamese notice (T-014). The operator can still paste page text or type |
| R-004 | The button mark is the hash of the script's bytes as checked out. A checkout that converts line endings differently gives another mark | low | Buttons made from another checkout are refused as old (block-old-button) | The failure is safe: the refusal asks for the new button from "Cài đặt". Case 068 checks the mark against the bytes actually held |
| R-005 | The image-host rule accepts any host ending .susercontent.com, as in MC-18 | low | An image could come from any host with that suffix that the page shows | Only the block's own addresses are fetched: at most 20, https only, no port, user name or IP literal, never the page's own address, no redirect, PNG or JPEG signature, the 40 MiB bound (T-009) |
| R-006 | The operator could paste a block taken from another product into the wrong video | low | Wrong facts would be offered for the open video | Each filled value is marked and needs confirmation against the page, and the page address is filled from the block |

## Handoff Notes

- Reviewer focus areas:
  - The button script nut-doc-trang.js: its anchors, the copy chain and the absence of request, navigation, click, cookie and storage use.
  - The reader's check order and bounds in BlockReader.cs.
  - The route order in PasteBlock in Routes.cs.
  - The page-address and image-address rules (D-016).
  - The confirmation wording in FillRecord.cs.
  - The one-screenshot action in app.js.
  - The Vietnamese texts, against the design's wording rule (SC-32).
  - The test-side clipboard grant (V-005).
- Follow-up work:
  - After quality review, the orchestrator makes the Browser-pane record on a fixture page (SC-41).
  - The CEO presses the button once on a real product page, and the orchestrator records the outcome (SC-42).
  - Setup for the operator:
    1. Start the app with `dotnet run --project src/MediaCompany.Pilot.Web` (http://127.0.0.1:47817).
    2. Open "Cài đặt", turn on "Bật đọc trang sản phẩm", and drag "Đọc trang Shopee" to Chrome's bookmark bar.
    3. Open a video at step 1, press the button on the product page, and paste with Ctrl+V.
  - The release note and the compare link are still to be written.
- Documentation impact: The Vietnamese operator guide should gain a section on setting up the button, the press and the paste, the "Từ nút đọc trang" marks, the one-screenshot action and the refusal messages; its owner decides. This report records no review verdict and no gate decision. The change set and the declared side effects name the same 17 files, and no external system was accessed.

## Open Questions

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| Q-001 | Does the headless-shell measurement (T-005), which grants the clipboard permission desktop Chrome gives and records the shell's refusing default as run A0, close the design's clipboard question 005 for this phase, with the CEO's real-page try (SC-42) as the first real-Chrome evidence (V-005)? | non-blocking | qa | C-001, C-013 |
