```yaml
design:
  designId: TD-MC19-PAGE-BUTTON
  changeReference: runs/inputs/mc-19-feature-request.md
  sourceInputs:
    - type: change-request
      reference: runs/inputs/mc-19-feature-request.md
    - type: business-intent
      reference: runs/inputs/mc-19-business-intent.md
    - type: execution-plan
      reference: runs/run-142c58d73427/states/execution-planning/artifacts/execution-plan.md
  producedBy: architect
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  decisionRecords: [D-001, D-002, D-003, D-004, D-005, D-006]
  consumesPlan: runs/run-142c58d73427/states/execution-planning/artifacts/execution-plan.md
  inputDigest: sha256:dafb14cda42ba741867068b9506aa8b1
  contextDigest: sha256:11b68469cf9e593c79e0dc76de8cb64f
```

## Metadata

- Feature or Change ID: MC-19, run run-142c58d73427, phase solution-design-and-risk-assessment, repair attempt 2 (invocation inv-142c58d73427-03-002). This package is the design that planner task T-001 asks for. Its Test Strategy table is the criterion-to-fixture map that planner task T-002 asks for.
- Author: architect
- Reviewers: tech-lead (accepting Design Gate owner), product-owner (the CEO answers), qa, dev-1-implement, dev-2-reviewer
- Last Updated: 2026-10-30
- Upstream evidence, cited by identifier and digest:
  - The scope definition (sha256:3bb9dc195f8f7d1a8f56e92dc281b020). Its acceptance criteria are cited as SC-nn, in the plan's numbering. Its exclusions are cited as X-nnn and its scope decisions as SD-n.
  - The execution plan (sha256:06aa3ad381d3b61aae7823b2b468042f in the task context). It is cited by T-nnn, "plan assumption n" and "plan risk n".
  - The CEO's Scope Gate decision (CEO decision 1319) and the Planning Gate record, both as restated in the orchestrator briefing, sections 6 and 7. Earlier CEO decisions are cited descriptively as "CEO decision 1313" to "CEO decision 1318".
  - The orchestrator's dispatch of this phase, cited as "orchestrator dispatch".
- Decision records: six separate files at status Proposed, architecture-decision-record-D-001.md to architecture-decision-record-D-006.md, in this phase's artifacts folder (answer to Q-004 at attempt 2).
- Repair record (attempt 2): CEO decisions are cited descriptively; each functional requirement line names its statement; 5.3 names one selected option and its highest-scoring rejected alternative; the data-migration line states direction, reversibility and transition behaviour; the decision records moved from an appendix of this package into six files; a misspelt view name in the statement register is corrected. No identifier was renumbered.
- Plan assumptions settled (T-001 acceptance):
  - Plan assumption 1 is carried as A-001.
  - Plan assumption 2 is carried unchanged; the orchestrator confirms it.
  - Plan assumption 3 is split into A-002 and A-003. Neither is measured in this phase, because this phase runs no command (Q-005).
  - Plan assumption 4 is carried as A-004. The fixture grounding is answered in D-003 and Q-002.
  - Plan assumption 5 is carried as A-007.
  - Plan assumption 6 is replaced by D-004.
  - Plan assumption 7 is confirmed by F-015 and F-016.
  - Plan assumption 8 is carried as A-006 and settled in D-003.
  - Plan assumption 9 is confirmed by D-013: files only, no database.
  - Plan assumptions 10, 11 and 12 are carried unchanged; the orchestrator owns them.
  - Plan assumption 13 is replaced by A-013, because F-021 records no host names or paths.
  - Plan assumption 14 is carried as A-008.
  - Plan assumption 15 is confirmed by F-008 and settled in D-010.
  - Plan assumption 16 is confirmed by F-003 and F-004: the paste is posted by the app's own page, with the exact Origin and the per-start token.
- Plan risks reconciled: each plan risk the architect owns is carried, closed or attached in section 10. Plan risks 1, 3, 13, 14, 16 to 18, 29 and 30 stay the orchestrator's or product-owner's.
- Repository rescan: the task context names no files. The following were read to establish 4.1:
  - src/MediaCompany.Pilot.Web: Routes.cs, FillRecord.cs, ProductText.cs, ReadLink.cs, ProductRead.cs, ReadSender.cs, RequestGuard.cs, WebContext.cs, Program.cs, Pages/app.js, Pages/index.html
  - tests/MediaCompany.Pilot.Web.Tests: ReadFixture.cs, ReadHost.cs, BrowserCase.cs, ReadCases.cs
  - src/MediaCompany.Pilot: FactSheet.cs, Producer.cs
  - the MC-16 example fact sheet
  - the MC-18 design (run-6fa26c9657d3)

  Reason: MC-19 builds on MC-18 parts that the context does not name.
- Skills applied: S01, S03, S06, S09, S02, S08, S12.
  - Database area: lexical only; no database is touched (D-013).
  - Performance area: real for the bounds on the hand-over, the button and the image fetch (D-014).
  - Security area: real; see Operational Considerations and R-013 to R-019.
- No implementation work was performed. No command was executed. No external system was accessed and no live page was read.

## Objective

- Desired outcome: on a shopee.vn product page in the operator's own Chrome, one press of a bookmark-bar button copies one bounded block. One paste at step 1 of the open video fills the same fields and candidate rows that MC-18 fills, under a new mark. The unchanged MC-16 check decides the save. The app never requests a shopee.vn page (S-001, S-002, S-004, S-013).
- Architectural objectives:
  - A button that is a fixed script held in the repository and generated by the app into one bookmark address. It loads no code, sends no request, acts on no page control, and acts only on shopee.vn product pages (S-004, S-007, S-016, S-021).
  - One versioned hand-over contract, the block. It is read as untrusted data by one reader that bounds it before parsing and refuses it whole (S-005).
  - One entry point for the block: the existing guarded paste route. The request guard is unchanged (S-005, S-013).
  - The existing read switch governs the block exactly as it governs the MC-18 read (S-006).
  - Image egress only for image addresses the block carries, through the MC-18 image phase and host rule (S-003).
  - A test-only allowance of the fixture origin, passed through the existing internal seam, so the same button code runs on fixture pages and the shipped app has no bypass (S-009, S-021).
- In scope:
  - New block reader and button builder components in src/MediaCompany.Pilot.Web.
  - The repository-held button script.
  - The paste route's block path and a button route.
  - A third fill source.
  - Step 1 and "Cài đặt" page parts.
  - The internal seam's fixture-origin allowance.
  - New fixtures and cases in tests/MediaCompany.Pilot.Web.Tests.
- Out of scope:
  - Any change to src/MediaCompany.Pilot or its tests, the fact-sheet check or its per-row screenshot rule (S-008, X-006).
  - Any change to the request guard, its content policy or the listener (S-013, X-007).
  - Any app request to a shopee.vn page. Reading any page other than the pressed one. Pages on other sites (X-001, X-004, X-014).
  - Sign-in, cookies, stored data or account data (S-012, X-002).
  - A screenshot taken by a tool (X-005).
  - Steps 2 to 7 beyond receiving picked images (X-010).
  - Removing MC-18 read or paste (X-008).
  - The mediacompany database, MC-13 and MC-14 material (X-009). Merging (X-012).
  - Browsers other than Chrome (S-017).
  - The CEO's real-page try, which is the CEO's act, recorded by the orchestrator (S-011).

## Requirements Summary

- Functional requirements:
  - One press and one paste fill product name, classification, shop name and Mall, page address and press date, equal to the page after NFC (S-001; C-001).
  - One candidate row per specification, equal to the page after NFC (S-001; C-001).
  - Price and description shown for reference only (S-001; C-002).
  - The own image addresses offered for picking (S-001, S-003; C-003).
  - At most two operator actions (S-018; C-004).
  - Non-product pages and a missing open video end in a Vietnamese refusal with nothing filled (S-001, S-015; C-005, C-006).
  - The MC-18 fill behaviour applies, with a new mark: confirmation per field and per row, unsupplied fields left to the operator, at most three rows (S-002; C-007).
  - The unchanged check decides the save (S-002; C-008).
  - One screenshot is named on every button-filled row in one action (S-014; C-009).
  - The switch refuses the block, and paste and typing still work (S-006; C-017).
  - "Cài đặt" offers the current button with Vietnamese drag instructions (S-007; C-018, C-019).
- Non-functional requirements:
  - The block is bounded: 256 KiB, 30 addresses, 100 rows, one product, current version. It is refused whole otherwise and shown only as text (S-005; C-013 to C-015, C-020).
  - The guard is unchanged (S-013; C-016).
  - Image egress follows the MC-18 rules, and no request goes to a shopee.vn page (S-003; C-010).
  - A press makes zero requests, loads zero code and activates zero controls, and no account data is handed over (S-004, S-012; C-011, C-012).
  - The toolchain is unchanged; the 251 and 53 tests pass; one listener on 127.0.0.1 (S-008; C-021).
  - Tests run offline with no media and no package; no agent opens a Shopee host (S-009; C-022, C-023).
  - The test seam is unreachable from the shipped app (S-021; C-026). Chrome only (S-017; C-027).
- Acceptance criteria: SC-01 to SC-42, with SC-19 withdrawn and its converse applied (S-012), as set by product-owner and decided at the Scope Gate (CEO decision 1319). Each is mapped in the Test Strategy Focus Areas table of the Delivery Plan.

## Current-State Assumptions and Constraints

### 4.1 Facts

| ID | Fact | Established by |
|---|---|---|
| F-001 | The web project is an executable with no package reference; its internals are visible to the test assembly only | src/MediaCompany.Pilot.Web project file; MC-18 design module for the project file |
| F-002 | The web app listens on the IPv4 loopback address only | src/MediaCompany.Pilot.Web/Program.cs |
| F-003 | The request guard needs a loopback peer and Host 127.0.0.1:port. GET and HEAD pass. POST needs exact Origin and the per-start token header. The content policy allows scripts and connections only from the app's own origin and images only from the app's own origin and blob addresses | src/MediaCompany.Pilot.Web/RequestGuard.cs |
| F-004 | The paste route takes a text and refuses empty text and text with no product. It parses with the shared text parser, clears waiting read images and saves a paste fill record. It works with the switch off. JSON bodies are capped at 1 MiB | src/MediaCompany.Pilot.Web/Routes.cs |
| F-005 | The read route refuses with a Vietnamese outcome when the switch is off, and allows one read at a time app-wide | src/MediaCompany.Pilot.Web/Routes.cs, WebContext.cs |
| F-006 | Step-1 routes take the video code from the query of the request | src/MediaCompany.Pilot.Web/Routes.cs |
| F-007 | The fill record is at form version 1 and holds: a source of read or paste, link, final address, time, fields with a confirmed flag, candidate rows, price, description, saved images, not-fetched images and not-found names. Saving refuses an unconfirmed filled field, naming the source as the product page or the pasted text. The list of operator-only members includes shopNameAndMall | src/MediaCompany.Pilot.Web/FillRecord.cs |
| F-008 | The read switch is its own file in the app folder. It is off by default and set on the "Cài đặt" view | src/MediaCompany.Pilot.Web/FillRecord.cs, Pages/app.js |
| F-009 | The shared text parser anchors on "Tên sản phẩm", "Phân loại", "CHI TIẾT SẢN PHẨM" and "MÔ TẢ SẢN PHẨM". It ends sections at headings such as "SẢN PHẨM TƯƠNG TỰ". Row source sections take the form "Chi tiết sản phẩm: label". Description is capped at 4000 characters; values are NFC | src/MediaCompany.Pilot.Web/ProductText.cs |
| F-010 | The link rule takes the host in lower-case ASCII form without a trailing dot. Page hosts are shopee.vn and names ending .shopee.vn, except the affiliate host. Image hosts add names ending .susercontent.com. An image address must be https, with no user info, no port and no IP literal. The page path drops the query and fragment | src/MediaCompany.Pilot.Web/ReadLink.cs |
| F-011 | The image phase sends one request per image with no redirect. It keeps a file only with a PNG or JPEG signature and names it anh-doc-NN in the per-code read-image folder. The first image-phase stop ends fetching and names the rest. At most 20 images per read | src/MediaCompany.Pilot.Web/ProductRead.cs, ProductText.cs |
| F-012 | Each outbound request is written to the read log in the web folder. Bounds: 120 s per click, 5 MiB per page, 40 MiB per image, no retry | src/MediaCompany.Pilot.Web/ReadSender.cs |
| F-013 | Step 1 shows a mark with a confirm button beside each filled field and row; marks disappear when a value is edited. The image picker shows thumbnails from the app's guarded read-image route and allows at most three read images. A fourth fact row is refused. Each row has its own screenshot chooser, which uploads through the screenshot route | src/MediaCompany.Pilot.Web/Pages/app.js |
| F-014 | "Cài đặt" is a view reached from the top bar after setup; it holds the read switch | src/MediaCompany.Pilot.Web/Pages/index.html, Pages/app.js |
| F-015 | The fact sheet holds shopNameAndMall as one header text ("Tên gian hàng và Mall hay không"), with example form "shop, không phải Mall". Each row's screenshot is resolved on its own, with no uniqueness check | src/MediaCompany.Pilot/FactSheet.cs; pilot-affiliate-vn/mc-16/vi-du-bang-thong-tin.json |
| F-016 | The producer de-duplicates screenshot files, ignoring case, before staging | src/MediaCompany.Pilot/Producer.cs |
| F-017 | The read transport seam is an internal type. It is passed only through the internal start path; the shipped entry point never passes it. Tests assert that it is internal and that four argument forms are refused | src/MediaCompany.Pilot.Web/WebContext.cs, Program.cs; tests ReadCases.cs |
| F-018 | The test program has a code-generated HTTPS fixture on 127.0.0.1, reached through the seam. It also has a fixture-host mode that runs the seam-composed app for browser and Browser-pane runs | tests/MediaCompany.Pilot.Web.Tests/ReadFixture.cs, ReadHost.cs |
| F-019 | The headless shell is digest-checked. Its launch maps every name except 127.0.0.1 to not-found, uses no proxy and accepts extra flags | tests/MediaCompany.Pilot.Web.Tests/BrowserCase.cs |
| F-020 | In test mode the app serves a test driver script and a result route | src/MediaCompany.Pilot.Web/Routes.cs; tests BrowserCase.cs |
| F-021 | The address harness samples the TCP and UDP tables about every 20 ms. It records addresses, ports and owner, never host names or paths | tests/MediaCompany.Pilot.Web.Tests/AddressHarness.cs; MC-18 design facts |
| F-022 | At MC-18 close the toolchain has 251 passing tests and the web app 53 | orchestrator briefing section 5 |
| F-023 | Both MC-18 live reads got a script-rendered page shell with no product data and stopped with no product | feature request WHY; orchestrator briefing section 2 |
| F-024 | The CEO's screenshot of a product page shows: product name, price, shop with a Mall badge, classification buttons, a "CHI TIẾT SẢN PHẨM" section of label and value rows, and a "MÔ TẢ SẢN PHẨM" description | orchestrator dispatch |
| F-025 | The app's page sets every shown text as text, never as markup | src/MediaCompany.Pilot.Web/Pages/app.js |
| F-026 | The toolchain's network guard covers only the toolchain's own code; toolchain jobs own zero sockets | MC-18 design facts on the network guard (run-6fa26c9657d3) |

### 4.2 Assumptions

| ID | Assumption | Why needed | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | The base class library holds what the app side needs: a strict JSON reader with a depth limit, SHA-256 and file APIs. The button is plain browser JavaScript (plan assumption 1) | C-022 for every option | A package needs the CEO's approval by name (R-001) | dev-1-implement |
| A-002 | Chrome runs a bookmark-bar script address on a shopee.vn product page whatever that page's content policy (plan assumption 3) | D-003, D-007 | No hand-over on real pages; paste mode stays (R-002) | dev-1-implement on a strict-policy fixture; the CEO's try |
| A-003 | On a bookmark press, Chrome lets either the asynchronous clipboard write or the older copy command on a selected box succeed (plan assumption 3) | D-007 keeps two actions | The visible box needs Ctrl+C, a third action (R-003) | dev-1-implement on the fixture; the CEO's try |
| A-004 | A real product page shows these anchors as visible text: the product name as the page's main heading; the "Phân loại" label beside its variant buttons; a shop block holding the "Xem Shop" control and any Mall badge; and the "CHI TIẾT SẢN PHẨM" and "MÔ TẢ SẢN PHẨM" headings (plan assumption 4, grounded on F-024 and F-009) | D-003 field finding | Fields are missed and left for the operator (R-004) | orchestrator, from the CEO's try (SC-42) |
| A-005 | The selected variant button carries a standard pressed or selected state, not only a styling class | D-003 classification | Classification stays empty and is never guessed (R-005) | orchestrator, from the CEO's try |
| A-006 | The product's own images are those in the gallery inside the product region, meaning the region that holds the main heading and not the "CHI TIẾT SẢN PHẨM" heading. Other products' images sit in later sections (plan assumption 8) | D-003 own images | Other images are offered, or none are (R-006) | orchestrator, from the CEO's try; SC-04 fixture |
| A-007 | The headless shell can open a plain-HTTP fixture page on 127.0.0.1 and run the button address there. A fixture probe script can capture the copied text and count requests, clicks, submissions and navigations, with no package (plan assumption 5) | Browser route for SC-01, SC-05, SC-08, SC-09, SC-25, SC-31, SC-41 | Browser evidence needs another route (R-007) | dev-1-implement |
| A-008 | The Browser pane can open a fixture product page from fixture-host mode on 127.0.0.1 and run the button address taken from "Cài đặt" (plan assumption 14) | SC-41 route | SC-41 rests on the headless runs alone (R-008) | orchestrator |
| A-009 | Chrome fires a paste event on the app page for Ctrl+V while step 1 is shown, even when no text field has focus | D-001 one paste is one action | The operator clicks the paste box first (R-009) | dev-1-implement on the fixture |
| A-010 | The rendered page shows the price as a đồng amount near the main heading, before the shop block | D-003 price for reference | Price is absent; it is reference only, so no fact is lost (R-004) | orchestrator, from the CEO's try |
| A-011 | Real gallery image addresses are on hosts ending .susercontent.com or on shopee.vn (the example in CEO decision 1315) | D-005 fetchable images | Images are named for upload at step 2 (R-010) | orchestrator, from the CEO's try |
| A-012 | Gallery addresses as shown may be reduced-size variants; the button carries them as shown and never rewrites them | D-003 never guesses | Picked images may be small; the operator uploads at step 2 (R-011) | orchestrator, from the CEO's try |
| A-013 | SC-21 is shown by pairing the read log's hosts and paths (F-012) with the harness's remote addresses and sample times, because F-021 records no host names (replaces plan assumption 13) | SC-21, SC-28, SC-36 to SC-38 evidence route | SC-21 needs another route (R-012) | qa |
| A-014 | Reading only the product region and the two anchored sections keeps any signed-in account name out of the block. The button reads no cookie or storage interface at all | C-012 | An account name is copied (R-013) | dev-2-reviewer by inspection; SC-18 fixture |
| A-015 | An MC-18 build treats a fill record with an unknown source value as unreadable, therefore absent | Rollback position for M-003 | A rolled-back build shows a stale fill record (R-020) | dev-1-implement |

### 4.3 Constraints

| ID | Class | Constraint | Hard or negotiable | Source |
|---|---|---|---|---|
| C-001 | functional | One press and paste fill name, classification, shop and Mall, page address, press date and one candidate row per specification, equal to the page after NFC; nothing invented | hard | SC-01, SC-02 (S-001) |
| C-002 | functional | Price and description are shown for reference only and never saved by the tool | hard | SC-03, CEO decision 1314 (S-001) |
| C-003 | functional | Only own images are offered; up to three picked; only those enter the input set | hard | SC-04, SC-15 (S-001, S-003) |
| C-004 | functional | At most two operator actions from the open page to filled, confirmable fields | hard | SC-05 (S-018) |
| C-005 | functional | A shop, search or nameless page fills nothing and shows a Vietnamese reason | hard | SC-06 (S-001) |
| C-006 | functional | No open video means the block is refused and nothing is kept | hard | SC-07, CEO decision 1319 item 5 (S-015) |
| C-007 | functional | A new Vietnamese mark on each filled field; confirmation before save; unsupplied fields left; at most three rows | hard | SC-08 to SC-10, SC-12 (S-002) |
| C-008 | functional | The check result on filled values equals that on typed values | hard | SC-11, X-006 (S-002) |
| C-009 | functional | One screenshot chosen once is named on every button-filled row in one action | hard | SC-13, CEO decision 1319 item 4 (S-014) |
| C-010 | security | Only carried image addresses on allowed hosts are fetched, https, one request each, no redirect, PNG or JPEG; zero requests to any shopee.vn page | hard | SC-14, SC-21, CEO decisions 1315 and 1318 (S-003, S-004) |
| C-011 | security | A press sends zero requests, loads zero code and activates zero controls | hard | SC-16, SC-17, SC-33 (S-004, S-016) |
| C-012 | security | The block carries no account name, cookie or stored value; a signed-in page hands over normally | hard | SC-18, SC-19 converse (S-012) |
| C-013 | security | A block with more than one product is refused | hard | SC-20 (S-004) |
| C-014 | security | Over 256 KiB is refused before parsing; over 30 addresses or 100 rows is refused whole; five malformed kinds are refused | hard | SC-22 to SC-24, CEO decision 1319 item 9 (S-005) |
| C-015 | security | Block text is shown as text; zero scripts run; zero elements are created from it | hard | SC-25 (S-005) |
| C-016 | security | The guard is unchanged; foreign Origin, missing token, old token or foreign Host gets 403; no direct page-to-app call | hard | SC-26, SC-27, X-007, CEO decision 1319 item 2 (S-013) |
| C-017 | operability | One switch governs read and block. Off means refusal and zero egress, while paste and typing still save. No new switch | hard | SC-28 to SC-30 (S-006) |
| C-018 | operability | "Cài đặt" shows the current button with drag instructions, and that button works on the fixture | hard | SC-31 (S-007) |
| C-019 | functional | Every new text is Vietnamese with reason and next step | hard | SC-32 (S-007) |
| C-020 | security | The button code is fixed in the bookmark; a block from an out-of-date button is refused, asking for the new button | hard | SC-33, CEO decision 1319 item 6 (S-016) |
| C-021 | structural | Toolchain unchanged with 251 tests; 53 web tests pass; one listener on 127.0.0.1; toolchain jobs own zero sockets | hard | SC-34 to SC-37 (S-008) |
| C-022 | operability | Offline tests on code-generated fixtures; zero media files and zero packages | hard | SC-38, SC-39, X-011 (S-009) |
| C-023 | compliance | No agent opens shopee.vn or any Shopee host | hard | SC-40 (S-009) |
| C-024 | functional | The Browser pane shows one press ending in filled, confirmable fields, and a refusal with the switch off | hard | SC-41 (S-010) |
| C-025 | compliance | The CEO's single real-page try is recorded with ten kinds stated | hard | SC-42 (S-011) |
| C-026 | security | The fixture allowance is unreachable from the shipped app, and the button code under test is the shipped code | hard | orchestrator dispatch, plan risk 24 (S-021) |
| C-027 | structural | Chrome is the only target browser | hard | CEO decision 1319 item 7 (S-017) |
| C-028 | structural | The mediacompany database, MC-13 and MC-14 are untouched | hard | X-009 (S-009) |
| C-029 | operability | This phase writes only its artifact, its decision records and its envelope | hard | framework defects finding 12 (S-020) |
| C-030 | quality-attribute | Field finding should survive restyling: visible-text anchors rather than generated class names | negotiable | orchestrator dispatch, plan risk 15 (S-021) |
| C-031 | quality-attribute | The button address stays small enough to drag and keep as a bookmark, at or under 32 KiB | negotiable | orchestrator dispatch (S-021) |
| C-032 | functional | The button reads what the page shows, not hidden page data | hard | feature request operator step 3, CEO decision 1318 item 1 (S-001, S-004) |
| C-033 | security | The button acts only on shopee.vn pages, and the app accepts only shopee.vn page addresses | hard | SC-24, X-014, orchestrator dispatch (S-021) |

## Architecture and Component Design

### 5.1 Impacted Modules

| ID | Module | Impact type | Basis | Interfaces affected | Confidence |
|---|---|---|---|---|---|
| M-001 | Block reader (new, in MediaCompany.Pilot.Web): marker, size bound, strict parse, version and button mark, bounds, page-address rule | extension | F-004, F-010, A-001 | Called by M-004 with the pasted text | confirmed |
| M-002 | Button script (new, a repository-held plain script in MediaCompany.Pilot.Web) and button builder | extension | F-014, F-024, A-002 to A-006, A-010, A-012, A-014 | One bookmark address; the block on the clipboard | speculative |
| M-003 | Fill record and confirmation rule (FillRecord.cs) | contract-change | F-007, F-015, A-015 | Third source value; shopNameAndMall filled for that source | confirmed |
| M-004 | MediaCompany.Pilot.Web routes (Routes.cs) | contract-change | F-004, F-005, F-006 | Paste route gains the block path and its refusals; new guarded GET button route | confirmed |
| M-005 | Image phase (ProductRead.cs, ReadSender.cs, link rule in ReadLink.cs) | extension | F-010, F-011, F-012 | Called with the block's addresses and page address | confirmed |
| M-006 | Step-1 and "Cài đặt" page parts (index.html, app.js, styles) | extension | F-013, F-014, F-025, A-009 | Paste listener, new mark, one-action screenshot, button and instructions | speculative |
| M-007 | Request guard and content policy (RequestGuard.cs) | no-change-verified | F-003 | None; the block arrives as a guarded POST from the app's own page | confirmed |
| M-008 | Start-up and internal seam (Program.cs, WebContext.cs, the read transport seam) | extension | F-017 | The internal seam gains a test-only fixture-origin allowance; the shipped entry point is unchanged | confirmed |
| M-009 | Read switch store | no-change-verified | F-008 | None; consulted by M-004 for the block | confirmed |
| M-010 | Shared text parser (ProductText.cs) | no-change-verified | F-009 | None; text without the marker takes the MC-18 path | confirmed |
| M-011 | MC-16 toolchain src/MediaCompany.Pilot (check, fact sheet, producer) | no-change-verified | F-015, F-016, F-026 | Consumed unchanged | confirmed |
| M-012 | MC-16 toolchain tests | no-change-verified | F-022 | None; 251 must pass | confirmed |
| M-013 | MediaCompany.Pilot.Web.Tests: fixture product pages, probe, button runs, fixture-host additions, new cases | extension | F-018, F-019, F-020, A-007, A-008 | Plain-HTTP fixture page server on 127.0.0.1; seam-composed app | speculative |
| M-014 | Address harness (AddressHarness.cs) | no-change-verified | F-021 | Used unchanged, paired with the read log (A-013) | confirmed |
| M-015 | Pilot folder layout (Workspace.cs) | no-change-verified | F-007, F-011 | Fill record and read-image folder reused | confirmed |
| M-016 | Steps 2 to 7 routes and pages | no-change-verified | F-011, F-013 | Picked read images arrive as at MC-18 | confirmed |
| M-017 | mediacompany database, MediaCompany.Persistence, MC-13 and MC-14 material | no-change-verified | F-007 | None; files only | confirmed |
| M-018 | Web project file | no-change-verified | F-001 | No package; the button script is a page resource | confirmed |

Boundary crossings in the selected approach:
- Operator's Chrome tab: M-002 runs inside the shopee.vn page and writes to the system clipboard.
- Clipboard to the app page (M-006): one paste.
- M-006 to M-004 over loopback, as a guarded POST (M-007 unchanged).
- M-004 to M-009, M-001, M-003 and M-005, in process.
- M-005 to image hosts over HTTPS. This is the existing MC-18 crossing.
- M-003 and M-005 write to M-015.
- M-013 to M-008 through the internal start path, test only.

### 5.2 Options Considered

Eight decision families, each evaluated on its own. The columns follow the A8 order.
- Impact surface counts modules with contract-change or dependency-change.
- Reuse leverage counts existing capabilities of the family met by reuse (section 7).
- Quality attributes count the family's negotiable constraints that are met.
- "Eliminated" names the hard constraint an option violates. "Not selected" marks an option that meets every hard constraint and loses at the named later criterion.

| Option | Family | Structural change | Hard constraints | Impact surface | Reuse leverage | Quality attributes | Migration burden | Operability | Outcome |
|---|---|---|---|---|---|---|---|---|---|
| O-001 | D-001 hand-over entry | The existing guarded paste route recognises the block marker and takes a block path; other text follows MC-18 | all met | 1 (M-004) | 4 | none recorded | 0 | high: one entry, one refusal point | selected |
| O-002 | D-001 hand-over entry | A new guarded block route beside the paste route | all met | 1 (M-004) | 3 | none recorded | 0 | medium: paste route must still refuse marked text | not selected: lost at reuse leverage (3 to 4) |
| O-003 | D-001 hand-over entry | The shopee.vn page posts the block straight to 127.0.0.1 | C-016 violated: foreign origin, guard widened | 2 (M-004, M-007) | 2 | none recorded | 1 | low | eliminated (C-016) |
| O-004 | D-002 block format | A fixed leading marker, then one strict JSON object with typed members, unknown members refused | all met | 1 (M-004) | 2 | none recorded | 0 | high: readable, typed | selected |
| O-005 | D-002 block format | MC-18 labelled text with a header line, through the shared parser | C-014 violated: no types, so the wrong-type refusal cannot exist | 1 (M-004) | 2 | none recorded | 0 | medium | eliminated (C-014) |
| O-006 | D-002 block format | The same object, encoded compactly | all met | 1 (M-004) | 2 | none recorded | 0 | low: the operator cannot read what was copied; a third larger | not selected: lost at operability |
| O-007 | D-003 field finding | Visible-text anchors: main heading, "Phân loại", shop block, the two section headings, product-region gallery | all met | 0 | 1 | C-030: 1 of 1 | 0 | high | selected |
| O-008 | D-003 field finding | The page's generated class names and element paths | all met | 0 | 0 | C-030: 0 of 1 | 0 | low: breaks on restyling | not selected: lost at reuse leverage (0 to 1) |
| O-009 | D-003 field finding | The page's hidden structured data first, text anchors second | C-032 violated: hidden data may differ from what is shown | 0 | 1 | C-030: 1 of 1 | 0 | medium | eliminated (C-032) |
| O-010 | D-004 test seam | The internal seam carries a fixture-origin allowance used by the builder's host list and by the page-address rule; the shipped entry point passes none | all met | 0 | 2 | none recorded | 0 | high | selected (only viable) |
| O-011 | D-004 test seam | The test browser maps shopee.vn to the fixture by resolver rule, with no app seam | C-024 violated: the Browser pane cannot map names | 0 | 1 | none recorded | 0 | medium | eliminated (C-024) |
| O-012 | D-004 test seam | A command-line switch or setting that admits fixture origins | C-026 violated: reachable by the operator | 1 (M-004) | 1 | none recorded | 0 | high | eliminated (C-026) |
| O-013 | D-004 test seam | The button has no host check; only the app's rule refuses | C-033 violated: the button acts on any site | 0 | 2 | none recorded | 0 | high | eliminated (C-033) |
| O-014 | D-005 image fetch | At paste, fetch the carried allowed addresses (up to 20) for thumbnails; only picked images enter the input set | all met | 1 (M-004) | 3 | none recorded | 0 | high: picture picker | selected |
| O-015 | D-005 image fetch | Fetch only the three picked, picked by position number without pictures | all met | 1 (M-004) | 1 | none recorded | 0 | low: match by number against the Shopee tab | not selected: lost at reuse leverage (1 to 3) |
| O-016 | D-006 version | Format version plus a button mark, a short SHA-256 of the button script embedded at generation; the app accepts only the current pair | all met | 1 (M-004) | 1 | C-031: 1 of 1 | 0 | high: any script change retires old buttons | selected |
| O-017 | D-006 version | Format version only, raised by hand | all met | 1 (M-004) | 0 | C-031: 1 of 1 | 0 | medium: depends on a manual raise | not selected: lost at reuse leverage (0 to 1) |
| O-018 | D-006 version | The bookmark loads its code from the app at each press | C-011 and C-020 violated: code loaded at a press | 2 (M-004, M-007) | 0 | C-031: 1 of 1 | 0 | low | eliminated (C-020) |
| O-019 | D-007 clipboard | Asynchronous clipboard write, then the copy command on a selected box, then a visible selected box asking for Ctrl+C; a self-removing Vietnamese notice | all met | 0 | 0 | none recorded | 0 | high | selected |
| O-020 | D-007 clipboard | Asynchronous clipboard write only; on failure a message | all met | 0 | 0 | none recorded | 0 | low: no hand-over when refused | not selected: lost at operability |
| O-021 | D-007 clipboard | A browser dialogue showing the block to copy | C-004 violated: a dialogue adds actions | 0 | 0 | none recorded | 0 | low | eliminated (C-004) |
| O-022 | D-008 one screenshot | A step-1 control uploads one file once through the screenshot route and names it on every button-filled row | all met | 0 | 1 | none recorded | 0 | high | selected (only viable) |
| O-023 | D-008 one screenshot | A sheet-level screenshot accepted by a changed check | C-021 violated: toolchain changed (X-006) | 1 (M-011) | 0 | none recorded | 1 | medium | eliminated (C-021) |

Recomputed ranking, with criteria in A8 order:
- D-001: O-001 over O-002 (reuse leverage 4 to 3).
- D-002: O-004 over O-006 (tied through migration burden; O-004 wins operability).
- D-003: O-007 over O-008 (reuse leverage 1 to 0).
- D-004: O-010 is the only option meeting C-024, C-026 and C-033.
- D-005: O-014 over O-015 (reuse leverage 3 to 1).
- D-006: O-016 over O-017 (reuse leverage 1 to 0).
- D-007: O-019 over O-020 (tied through migration burden; O-019 wins operability).
- D-008: O-022 is the only option meeting C-021.

### 5.3 Selected Approach

- Selected: O-001 (hand-over entry, D-001), the structural choice the other seven families sit inside; their selections are the rows the table in 5.2 marks selected.
- Structural change:
  - Setup, once. "Cài đặt" (M-006) shows a draggable button. The new button route (M-004) builds it: M-002 reads the repository-held script and embeds the format version, the button mark and the page-host list.
  - The press. On a shopee.vn product page, the script reads the visible anchors (D-003), builds one block (D-002), copies it (D-007) and shows a Vietnamese notice. It sends nothing and touches no control.
  - The paste. At step 1 of the open video, the page sees the marker in a paste and posts the text to the guarded paste route (M-004). The route checks, in order:
    1. the switch (M-009)
    2. the open video
    3. the size
    4. parse, version and mark, and bounds (M-001)
    5. the page-address rule

    It then replaces the video's fill record with a button-source record (M-003) and fetches the carried images through the MC-18 image phase (M-005).
  - After the paste, the operator confirms each field and row, picks images and names one screenshot on every button-filled row in one action (D-008). The unchanged check decides the save.
  - Text without the marker takes the MC-18 paste path unchanged.
  - No toolchain file and no guard rule changes.
- Rationale:
  - The selected entry keeps one guarded door, so the switch refusal for the block sits in one place. A marked block pasted into the MC-18 paste box cannot slip past the switch.
  - The strict typed format (D-002) is the only readable format that can refuse a wrong-typed field.
  - Text anchors (D-003) trace to what the CEO's screenshot shows (F-024).
  - The internal seam allowance (D-004) is the only seam allowed by C-024, C-026 and C-033.
  - Fetching at paste (D-005) reuses the MC-18 picker with thumbnails unchanged.
  - The button mark (D-006) retires old buttons automatically.
  - The clipboard chain (D-007) keeps the second action a paste in most cases.
- Highest-scoring rejected alternative and why it lost: O-002 ties O-001 on every hard constraint and on impact surface (1 against 1), and loses at reuse leverage (3 against 4). It also leaves the paste route needing the same marker refusal, which makes two refusal points for one rule.
- Tradeoffs accepted:
  - The paste route's contract grows: a second path and new refusal codes. The MC-18 text path is kept byte-for-byte in behaviour (R-021).
  - Under D-005, up to 20 image requests per paste go to allowed image hosts, against 3 for fetching only the picked images. The CEO decides at the Design Gate (Q-001).
  - The button's host check and the app's page-address rule are duplicated on purpose (defence in depth). The fixture allowance reaches both only through the internal seam (D-004).
  - Chrome's behaviour on a real page (A-002, A-003, A-009) is not measured in this phase. It is measured on the fixture at P-002 and P-004, and the CEO's try (SC-42) is the first real evidence (Q-005).

### 5.4 Decisions

| ID | Decision | Architecture-significant | Record |
|---|---|---|---|
| D-001 | Hand-over entry: the guarded paste route recognises the fixed block marker and takes the block path in M-004; any other text keeps the MC-18 path; the step-1 page posts the paste immediately (D-015); guard unchanged (O-001; C-016, C-017; F-003, F-004; M-004, M-006) | yes | architecture-decision-record-D-001.md |
| D-002 | Block format: a fixed ASCII marker naming the format, then one JSON object holding the format version, button mark, page address, press date, product name, and optionally classification, shop name with Mall flag, specification rows (attribute, value, section), price text, description text and image addresses; unknown members, wrong types and trailing content refused (O-004; C-013, C-014; F-004, F-009; M-001) | yes | architecture-decision-record-D-002.md |
| D-003 | Field finding by visible-text anchors (A-004 to A-006, A-010): name from the main heading; classification from the selected button beside "Phân loại"; shop and Mall from the shop block holding "Xem Shop"; rows from label and value pairs under "CHI TIẾT SẢN PHẨM" up to the next section heading, section given as "Chi tiết sản phẩm: label"; description under "MÔ TẢ SẢN PHẨM"; price as the first đồng amount in the product region; own images from the product-region gallery, as shown; page address without query or fragment; a missed field is absent, never guessed; no cookie or storage interface is read (O-007; C-001, C-012, C-032; F-009, F-024; M-002) | yes | architecture-decision-record-D-003.md |
| D-004 | Test seam: the existing internal seam carries an exact fixture-origin allowance; the button builder adds it to the generated host list and the page-address rule accepts it; the shipped entry point passes none; fixture pages are served over plain HTTP on 127.0.0.1 by the test program (O-010; C-024, C-026, C-033; F-017, F-018; M-002, M-008, M-013) | yes | architecture-decision-record-D-004.md |
| D-005 | Image fetch: after a block is accepted and while the switch is on, the image phase fetches the carried addresses that pass the link rule (D-016), up to 20, one at a time app-wide; the picker shows them; only picked ones are placed on save (O-014; C-003, C-010; F-010 to F-013; M-004, M-005) | yes | architecture-decision-record-D-005.md |
| D-006 | Version: the block carries format version 1 and the button mark (the first 12 hexadecimal characters of the SHA-256 of the button script as held), both embedded by the builder; an earlier version or another mark is refused, asking the operator to drag the new button; an unknown later version is refused as unknown (O-016; C-011, C-020, C-031; F-001, F-014; M-002, M-004) | yes | architecture-decision-record-D-006.md |
| D-007 | Clipboard: the asynchronous write first; on refusal the copy command on an off-screen selected box; on refusal again a visible selected read-only box with "Nhấn Ctrl+C để chép"; then a self-removing Vietnamese notice naming the product and the next step "Mở ứng dụng và dán ở bước 1"; all shown text is set as text (O-019) | no | inline |
| D-008 | One screenshot: after a block fill, step 1 shows one chooser, "Một ảnh chụp cho mọi dòng từ nút"; one file is uploaded once through the existing screenshot route and named on every row that is button-filled and holds no other screenshot, including button rows added afterwards; typed rows are untouched; the check is unchanged (F-015, F-016) (O-022) | no | inline |
| D-009 | Fill: source "button" with the mark "Từ nút đọc trang" and confirmation wording "nút đọc trang"; shopNameAndMall filled as "shop, Mall" or "shop, không phải Mall" (F-015) only when the shop block is found, and not on the operator-only list for this source; read date from the block's press date when it is a valid date not after today; page path via the MC-18 rule; price and description reference only; rows as candidates, at most three kept (O-001) | no | inline |
| D-010 | Paste-route order for a marked text: more than one marker gives the one-product refusal; then switch off gives a refusal naming "Cài đặt"; no video open gives a refusal naming the step; then the size; then the D-014 bounds and the D-002 and D-006 checks; a missing name gives a refusal; a page address failing the page-host rule gives a refusal. Each refusal has a Vietnamese reason, next step and bracketed code, and fills nothing (O-001) | no | inline |
| D-011 | Non-product page: the button copies only when the main heading and the "CHI TIẾT SẢN PHẨM" heading are both present and the host passes its list; otherwise it copies nothing and its notice names the reason (not a product page, or not shopee.vn) (O-007) | no | inline |
| D-012 | "Cài đặt": the button is shown as a draggable link named "Đọc trang Shopee" with Vietnamese steps: show the bookmark bar with Ctrl+Shift+B, drag, then press it on a product page. A read-only box holds the same address for a hand-made bookmark. The format version and mark are shown. A click on it inside the app is blocked by the unchanged content policy, and the view says to drag it. The address is at most 32 KiB (C-031) (O-016) | no | inline |
| D-013 | Files only: the existing fill record and read-image folder; no new store; the mediacompany database is untouched (O-001) | no | inline |
| D-014 | Bounds: UTF-8 size of the pasted text 256 KiB, checked before parsing; JSON depth limit 8; 30 addresses; 100 rows; description 4000 characters as MC-18; each text member 4000 characters; the button stops collecting at 30 addresses and 100 rows and says so in its notice; image phase bounds as F-012 (O-004) | no | inline |
| D-015 | Paste listener: while the app page is open, a paste whose text begins with the marker is taken from the paste event and posted at once with the open video's code, whatever field has focus; text without the marker is left to the normal paste behaviour (O-001) | no | inline |
| D-016 | Image address rule at paste: the MC-18 link rule's image check against the block's page address (https, allowed host, no user info, port or IP literal); an address whose path equals the pressed page's path is refused; a refused address is named, not requested (O-014) | no | inline |

## API and Data Model Impact

- API changes (M-004; all on 127.0.0.1 under the unchanged guard, M-007):
  - Changed POST paste route. The request body is unchanged. A text beginning with the marker takes the block path, with the D-010 refusals. On success the reply carries the fill record, as the paste reply does today, plus image outcomes.
  - New guarded GET button route. It returns the button address, its size, the format version and the button mark for "Cài đặt".
  - Fill record (M-003): a third source value, "button". Confirmation wording for that source. shopNameAndMall may be a filled field.
- Transition strategy for M-004:
  - Current shape: MC-18 paste route (F-004).
  - Target shape: the same body; a second path selected only by the marker; new refusal codes; one new GET route.
  - Compatibility approach: additive. Text without the marker behaves exactly as at MC-18 close, so the 53 web tests stay valid (C-021). The page and routes ship in one build, so no older consumer exists.
  - Coexistence period: none beyond the build.
  - Retirement condition: not applicable; nothing is retired.
  - Rollback position: removing the block path and button route returns MC-18 behaviour. A bookmark left in the bar then copies a block that the MC-18 paste parser refuses with no product.
- Transition strategy for M-003:
  - Current shape: form version 1 with source read or paste (F-007).
  - Target shape: form version 1 with source read, paste or button.
  - Compatibility approach: additive enumerated value. MC-19 reads every MC-18 record unchanged.
  - Coexistence period: none beyond the build.
  - Retirement condition: not applicable.
  - Rollback position: an MC-18 build treats a button-source record as unreadable, so absent (A-015, R-020). The operator re-fills step 1 by paste or typing.
- Schema or migration changes:
  - None in any database (D-013).
  - No new file kind and no existing data rewritten: migration direction not applicable, fully reversible.
  - Transition behaviour: the fill record keeps form version 1 and gains one source value written only by new fills; existing read and paste records are read unchanged; an MC-18 build treats a button-source record as absent (A-015).
- Block data model (description, not a schema): as D-002, bounded by D-014 and versioned by D-006. The block is a transport form only. It is never stored; only the fill record it produces is stored.

## Reusable Components and Reuse Rationale

| Capability | Candidate | Outcome | Rationale |
|---|---|---|---|
| Guarded entry for the block | MC-17 request guard (F-003) | reuse-as-is | The paste is a POST from the app's own page with Origin and token |
| Entry route and body cap | MC-18 paste route (F-004) | reuse-extended | Gains the marker-selected block path (D-001) |
| Vietnamese refusal outcomes | MC-17 answer wrapper (F-004) | reuse-as-is | D-010 refusals use it |
| Strict structured parsing | Platform JSON reader (A-001) | reuse-as-is | Depth limit, typed reads, unknown members refused |
| Button mark | Platform SHA-256 (A-001) | reuse-as-is | Already used by the test program (F-019) |
| Page-host and image-host rule | MC-18 link rule (F-010) | reuse-as-is | The page-address rule and D-016 |
| Image fetching, naming, signatures, bounds, log | MC-18 image phase and sender (F-011, F-012) | reuse-extended | Called with the block's addresses |
| Thumbnails and picking | MC-18 read-image route and picker (F-013) | reuse-as-is | D-005 |
| Fill, marks, confirmation, candidates | MC-18 fill record and step-1 marks (F-007, F-013) | reuse-extended | Third source and mark (D-009) |
| Switch | MC-18 read switch (F-008) | reuse-as-is | Consulted by the block path (D-010) |
| One-action screenshot upload | MC-17 screenshot route (F-013) | reuse-as-is | One upload, many rows (D-008) |
| Settings place for the button | "Cài đặt" view (F-014) | reuse-extended | Gains the button and instructions |
| Field anchors vocabulary | MC-18 shared text parser labels (F-009) | reuse-extended | The same Vietnamese anchors, used in the button |
| Field finding on a rendered page | none | none-found | Searched existing components, the standard library, native browser capability and installed dependencies; no in-page reader exists; a package is excluded (C-022) |
| Clipboard copy with fallback | none | none-found | Searched the same four kinds; native browser clipboard and copy command are used directly (D-007) |
| Test seam | MC-18 internal read transport seam (F-017) | reuse-extended | Gains the fixture-origin allowance (D-004) |
| Fixture-origin admission by flag | MC-17 command-line switches | rejected | Reachable by the operator (C-026) |
| Fixture product pages | MC-18 HTTPS fixture (F-018) | rejected | The headless shell would not trust its code-made certificate without a browser flag; a plain-HTTP loopback page is a secure context for the clipboard (A-007) |
| Fixture page server | Platform HTTP listener on 127.0.0.1 | reuse-as-is | Code-generated pages, no media (C-022) |
| Browser runs | Digest-checked headless shell (F-019) | reuse-as-is | Plus a fixture probe script (A-007) |
| Browser-pane run | MC-18 fixture-host mode (F-018) | reuse-extended | Serves the fixture product pages too (A-008) |
| Egress evidence | Address harness with read log (F-012, F-021) | reuse-as-is | A-013 |
| Fact-sheet check | Toolchain check (F-015) | reuse-as-is | The unchanged authority (C-008) |

## Operational Considerations

- Logging and observability (M-004, M-005):
  - Every image request of a block fill is a read-log line (F-012).
  - The route log records the paste route as for every route.
  - The block text is never logged: only its size, its outcome code and its count of addresses and rows.
- Error handling strategy (S12):
  - Every D-010 refusal is an expected outcome: a Vietnamese reason, next step and bracketed code, with nothing filled (C-019).
  - The image phase keeps MC-18 behaviour: no retry; the first image-phase stop keeps the filled facts and names the remaining images (F-011).
  - A button-side failure (not a product page, not shopee.vn, clipboard refused) is shown in the button's own notice and copies nothing or falls back (D-007, D-011).
- Security considerations (S09):
  - The block is untrusted, even from the operator's own press (R-014):
    - its size is checked before parsing; the parse is strict and depth-limited (C-014, D-014)
    - its text is shown only as text (F-025, C-015)
    - every filled value needs confirmation (C-007)
  - The guard and content policy are unchanged; the shopee.vn page never calls 127.0.0.1 (C-016, M-007).
  - The button reads no cookie, storage interface or account area (D-003, A-014). A signed-in page hands over normally (C-012).
  - Image egress happens only for carried addresses under the MC-18 rule, and never for the page's own path (C-010, D-016). One residual is recorded: a crafted block could name another shopee.vn path as an image (R-017).
  - The seam is internal and is never passed by the shipped entry point. A shipped-executable case shows a 127.0.0.1 page address refused (C-026, D-004, R-016).
  - The clipboard holds only public page facts (R-015).
  - Compliance: the CEO carries the terms risk. The switch is the stop condition (C-017). No agent opens a Shopee host (C-023).
- Performance considerations (S08):
  - Bounded by C-014 through D-014.
  - The button walks only the product region and the two anchored sections.
  - The image phase keeps the F-012 bounds and runs one at a time app-wide (F-005).
- Deployment or operability impact:
  - The same single web executable.
  - The operator drags the button once from "Cài đặt", and again after any button change (D-006, R-022).
  - The switch stays off until the operator turns it on (F-008).
  - No database, service or browser extension (D-013, X-011). The S06 database area changes nothing.

## Delivery Plan

### Sequencing Constraints

| ID | Constraint | Modules | Prerequisites | Reason | Binds |
|---|---|---|---|---|---|
| P-001 | The Design Gate decision on D-001 to D-006 and on Q-001 and Q-002 exists before any producer or consumer of the block | M-001, M-002, M-003, M-004 | none | Contract definition precedes every consumer | T-001, T-002 |
| P-002 | The fixture page server, the probe, the seam's fixture-origin allowance and Chrome measurements of A-002, A-003, A-007 and A-009 on a fixture exist before the button is built against them | M-008, M-013 | P-001 | Verification of a boundary precedes work that assumes it | T-024 |
| P-003 | The block reader with marker, size, parse, version, mark, bounds and page-address rule exists before any fill consumes a block | M-001, M-004 | P-001 | Contract definition precedes every consumer | T-014, T-016 |
| P-004 | The button script and builder exist before "Cài đặt" shows the button | M-002, M-004 | P-001, P-002 | The settings view consumes the builder's output | T-003 |
| P-005 | The button fill source, its mark wording, the switch refusal and the confirmation rule for that source exist before the page shows marks | M-003, M-004, M-009 | P-003 | Contract definition precedes every consumer | T-004, T-006, T-007, T-009, T-018 |
| P-006 | The image phase is wired to the accepted block before the picker offers button images | M-004, M-005 | P-005 | The picker consumes fetched images | T-011 |
| P-007 | The step-1 paste listener, text-only display, one-action screenshot and "Cài đặt" button exist before browser verification | M-006 | P-004, P-005 | Every page part the browser cases exercise must exist | T-008, T-015, T-020 |
| P-008 | Criterion cases, harness runs, the 251 and 53 suites, browser runs and the review are complete on the full change | M-011, M-012, M-013, M-014 | P-006, P-007 | Release evidence assumes every boundary holds | T-005, T-010, T-012, T-013, T-017, T-019, T-021, T-022, T-023, T-025, T-026 |
| P-009 | The release note, the CEO's single real-page try and the compare link follow the evidence | M-002, M-011 | P-008 | Release evidence precedes release; the try follows quality review (S-011) | T-027, T-028 |

No step is irreversible: every change is additive in files the build owns, and the rollback positions in section 6 hold at each step.

### Test Strategy Focus Areas

Levels used:
- component: the reader and builder in the test process
- app: the shipped executable, or the seam-composed app, over loopback
- browser: the headless shell on a plain-HTTP fixture page with the probe, and the app page with the test driver
- harness: the address harness with the read log
- inspection: a reviewer reads the code or files
- record: the orchestrator's Browser-pane record, or the CEO's report

Every fixture is produced by test code at test time.

| Criterion | Design element | Level | Fixture case | Evidence |
|---|---|---|---|---|
| SC-01 | D-003, D-009 | browser | product page: heading, selected variant, Mall shop, five rows, price, description, gallery | block and filled fields equal the fixture after NFC, zero differences |
| SC-02 | D-003, D-009 | browser | no variant selected, non-Mall shop | classification empty; "shop, không phải Mall"; zero invented values |
| SC-03 | D-009 | app | product block | saved sheet holds neither price nor description |
| SC-04 | D-003 | browser | five gallery images, three other-product images under "SẢN PHẨM TƯƠNG TỰ" | block holds 5 of 5 and 0 of 3 |
| SC-05 | D-001, D-007, D-015 | browser and record | product page with step 1 open | actions counted: one press, one paste (A-003, A-009) |
| SC-06 | D-011 | browser | shop page; search page; nameless product page | 3 of 3: nothing copied, Vietnamese notice, zero fields filled |
| SC-07 | D-010 | app | valid block with no video code | refusal names opening a video; no fill record anywhere |
| SC-08 | D-009 | browser | product page | new marks N of N; typed 0; edited unmarked |
| SC-09 | D-009 | browser | product page | save refused at N minus 1, accepted at N |
| SC-10 | D-009 | app | product block | refusal names a field from the SC-10 list; accepted after filling |
| SC-11 | M-011 | app | eight blocks, banned classes (a) to (g) and a money amount | 8 of 8 equal to the typed result |
| SC-12 | F-013 | browser | five-row block | fourth row refused |
| SC-13 | D-008 | browser | product page, one screenshot file made in code | one choice; every button-filled row names it; save accepted |
| SC-14 | D-005, D-016 | app (seam) | block with the seven SC-14 addresses; a redirecting image; a non-image | 2 of 7 fetched; zero requests for 5; redirect and non-image rejected |
| SC-15 | D-005 | browser | five-image block | input folder holds the picked only; step 2 reports none missing |
| SC-16 | D-003 | browser | product page with fixture request log | zero requests during the press |
| SC-17 | D-003 | browser | probe counting clicks, submissions, navigations | zero; page address unchanged |
| SC-18 | D-003 | browser | signed-in page with planted account name, cookie and stored value | zero occurrences; only the listed kinds |
| SC-19 converse | D-003 | browser | the same signed-in page | block copied and accepted normally |
| SC-20 | D-010 | component and app | two blocks in one text | refused naming the one-product rule |
| SC-21 | D-005, D-016 | harness | press, pick and save on the seam app | read-log hosts and paths paired with harness addresses (A-013); zero page requests |
| SC-22 | D-014 | component and app | 255 KiB and 257 KiB blocks | accepted; refused before parsing |
| SC-23 | D-014 | component and app | 31 addresses; 101 rows | refused whole, nothing filled |
| SC-24 | D-002, D-010 | component and app (shipped executable for the address case) | no marker object; unknown version; missing name; wrong type; 127.0.0.1 page address | 5 of 5 refused, nothing filled |
| SC-25 | D-009, F-025 | browser | markup and script in each of the seven text members | probe count zero; shown text equal |
| SC-26 | M-007 | app | foreign Origin; no token; earlier token; foreign Host | 4 of 4 get 403, nothing filled |
| SC-27 | M-007 | inspection and app | none | guard tests unchanged and passing; zero bypasses |
| SC-28 | D-010 | browser and harness | switch off | refusal names the switch; zero outbound |
| SC-29 | F-004 | browser | switch off; pasted text and typed sheet | two saves accepted |
| SC-30 | D-010, M-009 | inspection and browser | switch toggled | one setting governs read and block |
| SC-31 | D-004, D-012 | browser | address taken from "Cài đặt" on the seam app | run on the product page; SC-01 comparison |
| SC-32 | D-007, D-012 | inspection | none | every new text read against the rule |
| SC-33 | D-006 | browser and component | request log during the press; earlier-version block; other-mark block | zero script requests; both refused asking for the new button |
| SC-34 | M-011, M-012 | inspection | none | toolchain diff empty; 251 passing |
| SC-35 | M-004, M-010 | app | none | 53 passing; additions only |
| SC-36 | M-007, M-008 | harness | sample run | one listener on 127.0.0.1 |
| SC-37 | M-011 | harness | sample run with a block and produce | toolchain jobs own zero sockets |
| SC-38 | M-013 | harness and inspection | the fixtures of SC-01, SC-04, SC-06, SC-14, SC-18, SC-22, SC-23, SC-24, SC-25 | zero connections off 127.0.0.1; each case found in test code |
| SC-39 | M-018, M-013 | inspection | none | zero media files, zero package references |
| SC-40 | C-023 | inspection | none | implementation, review and documentation reports state zero |
| SC-41 | D-004 | record | fixture-host mode with the product page; switch off | orchestrator's Browser-pane record: save accepted; refusal shown (A-008) |
| SC-42 | C-025 | record | the CEO's real page | 10 of 10 kinds stated; zero agent accesses |

T-002 routes:
- Browser route (SC-01, SC-05, SC-08, SC-09, SC-25, SC-31, SC-41): the installed headless shell (F-019) and a fixture probe. No package is needed. A-007 is recorded as unconfirmed until P-002.
- Socket-harness route (SC-21, SC-28, SC-36, SC-37, SC-38): the address harness (F-021) paired with the read log (F-012). Plan assumption 13 is not confirmed as worded and is replaced by A-013.

### Rollout and Rollback

- Rollout: the change lands on the MC-19 session branch. The operator rebuilds, opens "Cài đặt", drags the button and turns the switch on (F-008).
- Rollback:
  - Turning the switch off refuses every block at once, without a rebuild.
  - Removing the MC-19 additions returns the MC-18 build. Button-source fill records then read as absent (A-015). The bookmark's blocks are refused as text with no product.

## Risks and Mitigations

| ID | Class | Trigger | Impact | Likelihood | Affects | Mitigation | Owner |
|---|---|---|---|---|---|---|---|
| R-001 | structural | A needed capability is absent from the base class library (A-001 false) | A package needs the CEO's approval by name | low | M-001, P-003 | P-003 builds the reader first | dev-1-implement |
| R-002 | operability | Chrome refuses to run the bookmark address on a real page (A-002 false) | No hand-over on real pages; paste mode stays the path | medium | D-003, M-002 | Strict-policy fixture at P-002; the CEO's try at P-009 | dev-1-implement |
| R-003 | operability | Both clipboard writes are refused on a press (A-003 false) | The visible box needs Ctrl+C, a third action against C-004 | medium | D-001, M-002 | Measured at P-002; outcome recorded for the CEO | dev-1-implement |
| R-004 | operability | Real page anchors differ from the fixture (A-004 or A-010 false) | Fields are missed and left for the operator | medium | D-003, M-002 | Missed fields are named; the CEO's try records each kind (P-009); Q-002 | architect |
| R-005 | operability | The selected variant has no standard state (A-005 false) | Classification stays empty | medium | D-003 | Left for the operator; never guessed | dev-1-implement |
| R-006 | security | Own images cannot be told from others (A-006 false) | Other products' images are offered, or none | medium | D-003, D-005 | Product-region rule; SC-04 fixture at P-008; the operator picks | dev-2-reviewer |
| R-007 | delivery | The headless shell cannot run the button or the probe cannot capture the block (A-007 false) | Browser evidence needs another route | low | M-013, P-002 | P-002 proves the route first | dev-1-implement |
| R-008 | delivery | The Browser pane cannot run the button on the fixture (A-008 false) | SC-41 rests on the headless runs | low | D-004, P-008 | Fixture-host mode serves the pages | orchestrator |
| R-009 | operability | No paste event fires without a focused field (A-009 false) | The operator clicks the paste box first | low | D-001, M-006 | Measured at P-002; the paste box stays | dev-1-implement |
| R-010 | operability | Real gallery hosts are outside the rule (A-011 false) | Images are named for upload at step 2 | low | D-005 | As MC-18; a CEO decision to widen | product-owner |
| R-011 | operability | Gallery addresses are small variants (A-012 false hope) | Picked images are small | medium | D-005 | Upload at step 2 as today | product-owner |
| R-012 | delivery | The read log paired with the harness is not accepted for SC-21 (A-013 false) | SC-21 evidence incomplete | low | M-014, P-008 | Q-006 | qa |
| R-013 | security | An account name appears inside the product region (A-014 false) | Breach of CEO decision 1313 item 2 | low | D-003, M-002 | Anchored reads only; SC-18 fixture; review at P-008 | dev-2-reviewer |
| R-014 | security | Another page or program puts a crafted block on the clipboard and the operator pastes it (plan risk 25) | Unreviewed text enters step 1 | low | D-002, M-001 | Bounds, strict parse, version and mark, text only, per-field confirmation | dev-2-reviewer |
| R-015 | security | Another program reads the clipboard (plan risk 26) | Public page facts exposed; no account data | low | D-007 | No account data in the block (C-012) | dev-2-reviewer |
| R-016 | security | The fixture allowance is reachable in the shipped app (plan risk 24) | A non-shopee.vn page address is accepted | low | D-004, M-008, P-002 | Internal type; shipped-executable refusal case (SC-24); inspection | dev-2-reviewer |
| R-017 | security | A crafted block names another shopee.vn page path as an image address | One request to a shopee.vn page, answered non-image and discarded, against C-010 | low | D-005, P-006 | D-016 refuses the pressed page's own path; one request, no cookie; review at P-008 | dev-2-reviewer |
| R-018 | security | Block text is turned into page elements (plan risk 19) | Script runs in the app page | medium | M-006, P-007 | Text only (F-025); content policy unchanged; SC-25 | dev-2-reviewer |
| R-019 | security | An oversized or deep block reaches the parser (plan risk 20) | Resource exhaustion | low | M-001, P-003 | Size before parsing; depth limit (D-014) | dev-1-implement |
| R-020 | contract | A rolled-back MC-18 build meets a button-source fill record (A-015 false) | A stale or failing fill display | low | M-003 | The operator re-fills step 1; tested at P-005 | dev-1-implement |
| R-021 | contract | The block path changes MC-18 paste behaviour | SC-35 fails | low | M-004, P-003 | Marker-only selection; 53 tests at P-008 | dev-2-reviewer |
| R-022 | operability | An out-of-date button stays in the bar (plan risk 28) | Its blocks are refused until the operator drags again | medium | D-006 | Vietnamese refusal names the fix; "Cài đặt" shows the current one | dev-1-implement |
| R-023 | operability | Shopee warns the account while the button is in use (plan risk 27) | The CEO's terms risk materialises | low | D-001 | The one switch refuses blocks (D-010) | product-owner |
| R-024 | delivery | The CEO chooses fetching only picked images at Q-001 | D-005 changes to O-015 | medium | D-005, P-001 | Decided at P-001 before P-006 | tech-lead |
| R-025 | delivery | The separate-record-file validation rule is applied to this package | Validation reports missing record files | low | P-001 | Answered at attempt 2: six separate record files written (Q-004) | orchestrator |
| R-026 | delivery | Chrome behaviour stays unmeasured in this phase (T-001 asks for a measurement) | Chrome evidence arrives only at P-002 | medium | P-002 | Q-005; the design returns if the copy fails | dev-1-implement |
| R-027 | delivery | An unmerged earlier branch changes after the MC-19 session branch is cut (plan risk 14) | Rebase and re-run of the 251 and 53 tests | low | P-008 | Re-run at P-008 | orchestrator |
| R-028 | operability | Shopee changes its layout later (plan risk 15) | The button finds fewer fields | high | D-003 | Paste mode kept (SC-35); a block with no name refused | architect |
| R-029 | delivery | The compare link shows earlier unmerged commits (plan risk 30) | MC-19 review confused with earlier work | high | P-009 | The message states the branch base | orchestrator |

## Estimate and Confidence

- Overall: L (confidence: low). The change adds code that runs inside a page this team cannot open, and it rests on unconfirmed A-002 to A-006, A-009 and A-010.
- Breakdown:
  - M-001: S (confidence: high); a bounded strict reader.
  - M-002: M (confidence: low); field finding on an unseen real layout (A-004 to A-006).
  - M-003, M-004: S (confidence: medium).
  - M-005: XS (confidence: high); a new caller.
  - M-006: M (confidence: medium); paste listener, mark, screenshot control, settings button.
  - M-008: XS (confidence: medium).
  - M-013: M (confidence: low); plain-HTTP fixture pages, probe, browser cases (A-007, A-008).
- Scope assumptions: the toolchain and guard are unchanged (C-016, C-021); only the base class library is used (C-022); Q-001 keeps O-014; Q-002 keeps the fixture grounding.
- Uncertainty drivers: the real page's anchors and Chrome's clipboard and bookmark behaviour, which only the fixture measurement and the CEO's try can show.

## Open Decisions and Escalations

E-AUTHORITY events are Q-001 to Q-003, each a product choice the CEO answers. One E-BOUNDARY event is recorded as Q-004.

| ID | Question | Blocking | Owner | Affects | Consequence |
|---|---|---|---|---|---|
| Q-001 | After a paste, should the app fetch the product's own images straight away so you can pick from pictures (recommended), or fetch only the three you pick, chosen by number without pictures, as the ticket's step 4 reads? The CEO answers | non-blocking | product-owner | D-005, P-006 | Pictures: up to 20 image requests per paste, as MC-18. Picked only: 3 requests, picker by number; D-005 moves to O-015 |
| Q-002 | Ground the test page on your screenshot, the MC-18 paste text and the ticket's list (recommended), with your single real-page try as the first real check; any missed field is then fixed in a later change from your report. The CEO answers (answers the scope's fixture-grounding question) | non-blocking | product-owner | D-003, P-002 | Yes: as designed. No: the CEO supplies another grounding source, never by an agent opening shopee.vn |
| Q-003 | Mark wording "Từ nút đọc trang" and Mall wording "shop, Mall" or "shop, không phải Mall" (recommended), or other Vietnamese words. The CEO answers | non-blocking | product-owner | D-009 | Words change; nothing structural |
| Q-004 | The orchestrator's dispatch says the decision records go inside this package and no other file is written. The validator expects one separate record file per significant decision. Should the records stay in Appendix B, or should the architect be re-dispatched to write the six record files? | non-blocking | orchestrator | P-001, D-001 to D-006 | Answered at attempt 2: the orchestrator withdrew the inside-the-package instruction; six record files are written and the appendix is removed |
| Q-005 | T-001 asks for Chrome's behaviour measured on the fixture; this phase executes nothing. A-002, A-003, A-007 and A-009 are measured at P-002 and recorded in the implementation report | non-blocking | dev-1-implement | P-002, D-001, D-007 | Confirmed: as designed. Copy refused: the design returns to the architect before P-004 |
| Q-006 | Is the read log paired with the address harness accepted as SC-21 evidence (A-013)? | non-blocking | qa | M-014, P-008 | Yes: as designed. No: another evidence route |
| Q-007 | Design Gate acceptance of D-001 to D-006, each at Proposed. tech-lead accepts under the producer exclusion rule, with the CEO's answers to Q-001 to Q-003 recorded | non-blocking | tech-lead | D-001, D-002, D-003, D-004, D-005, D-006 | Accepted: implementation starts at P-001. Not accepted: back to the architect |

## Sign-off

- Architect: unsigned; producer of this package and its records, so excluded from accepting under the Producer Exclusion Rule
- Tech Lead: unsigned; accepting Design Gate owner for this package and for D-001 to D-006
- Product Owner: unsigned; product confirmation of Q-001 to Q-003 (the CEO answers)
- QA: unsigned; review of the test focus areas and of Q-006

## Appendix A: Statement Register

| ID | Statement | Source | Maps to |
|---|---|---|---|
| S-001 | One press on an open shopee.vn product page fills step 1 with name, classification, shop and Mall, page address, press date and candidate rows; price and description for reference; own images offered | Scope item 1 | C-001 to C-003, C-005, C-032, M-002 |
| S-002 | Button-filled fields carry a new mark and are confirmed; at most three rows; unsupplied fields stay; the check decides | Scope item 2 | C-007, C-008, M-003 |
| S-003 | Up to three images are picked; only carried addresses on allowed hosts are fetched under MC-18 rules | Scope item 3 | C-003, C-010, M-005 |
| S-004 | The button reads only its page and acts on nothing; no account data; one product; the app requests no shopee.vn page | Scope item 4 | C-010 to C-013, C-032 |
| S-005 | The block is untrusted, bounded, parsed as data, shown as text, refused whole, and accepted only through the guard | Scope item 5 | C-014 to C-016, M-001 |
| S-006 | The read switch is the one off switch for the block | Scope item 6 | C-017, M-009 |
| S-007 | "Cài đặt" shows the button with Vietnamese instructions; new text is Vietnamese | Scope item 7 | C-018, C-019, M-006 |
| S-008 | MC-18 paths keep working; toolchain unchanged with zero sockets; loopback listener only | Scope item 8 | C-021, M-010, M-011 |
| S-009 | Offline code-generated fixtures; no media; no package; no agent opens a Shopee host | Scope item 9 | C-022, C-023, C-028, M-013 |
| S-010 | The button is shown working in the Browser pane, and refused with the switch off | Scope item 10 | C-024 |
| S-011 | The CEO presses once on a real page after quality review; the outcome is recorded | Scope item 11 | C-025 |
| S-012 | A signed-in page is allowed; no account data is handed over | CEO decision 1319 item 1 | C-012 |
| S-013 | Hand-over through the clipboard; no direct page-to-app call; guard not widened | CEO decision 1319 item 2 | C-016, M-007 |
| S-014 | One screenshot named on every button-filled row in one action | CEO decision 1319 item 4 | C-009 |
| S-015 | A video must be open, otherwise the block is refused | CEO decision 1319 item 5 | C-006 |
| S-016 | Button code fixed in the bookmark; an out-of-date button is refused | CEO decision 1319 item 6 | C-011, C-020 |
| S-017 | Chrome only | CEO decision 1319 item 7 | C-027 |
| S-018 | No measure beyond at most two actions and the Browser-pane run | CEO decision 1319 item 8 | C-004 |
| S-019 | Committed and pushed on the MC-19 session branch; the CEO gets the compare link | Feature request, done means | P-009 |
| S-020 | Non-implementing roles write only their artifact, decision records and envelope at the paths their dispatch names | Orchestrator briefing section 1; orchestrator dispatch at attempt 2 | C-029, Q-004 |
| S-021 | Text anchors over class names; missed fields left; clipboard with fallback and a Vietnamese notice; bookmark generation, version and size; the button acts only on shopee.vn pages; a test seam with no production bypass; image fetch only for carried addresses | Orchestrator dispatch | C-026, C-030, C-031, C-033, M-008 |
| S-022 | The design phase measures which hand-over works in the installed browser against the fixture | Feature request, open point "Hand-over path" | Q-005 |

Traceability closure:
- Forward: 22 of 22 statements map to a constraint, module or question.
- Backward: 18 of 18 modules trace to facts or assumptions. 16 of 16 decisions trace to options, and 23 of 23 options to constraints.
- Lateral: every R, P and Q identifier resolves to an existing identifier.
