```yaml
plan:
  planId: PLAN-run-142c58d73427-MC-19
  sourceInputs:
    - type: feature-request
      reference: runs/inputs/mc-19-feature-request.md
  producedBy: planner
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  inputDigest: sha256:87ce06050698fc87d17cf66f7049a8ed
  contextDigest: sha256:2de1c2c2ba837051eb22fa9c512b030c
  upstreamArtifacts:
    - "scope-definition at runs/run-142c58d73427/states/scope-and-acceptance/artifacts/scope-definition.md (sha256:3bb9dc195f8f7d1a8f56e92dc281b020), approved at the Scope Gate by the CEO as CEO-D-1319"
  gateDecisionsRestated:
    - "Scope Gate approved by the CEO (CEO-D-1319), restated by the orchestrator briefing because the runtime does not carry gate rationale downstream; the entry was read in the repository decision record and carries the same nine answers (A-002)"
    - "Signed-in page allowed; the hand-over never carries account, cookie or stored data (CEO-D-1319 item 1)"
    - "Hand-over through the clipboard: the button copies one block and the operator pastes it once at step 1; no direct call from the shopee.vn page to 127.0.0.1; the request guard is not widened (CEO-D-1319 item 2)"
    - "Fixture pages in tests and the Browser pane; after quality review the CEO alone presses the button once on a real page and the orchestrator records the report; no agent opens shopee.vn (CEO-D-1319 item 3)"
    - "One screenshot named on every button-filled row in one action, check unchanged (item 4); a video must be open (item 5); button code fixed in the bookmark, an out-of-date button refused (item 6); Chrome only (item 7); no business measure beyond at most two actions per product (item 8); bounds 256 KiB, 30 image addresses, 100 specification rows, refused whole (item 9)"
    - "The scope's fixture-grounding question stays with the architect at the Design Gate (Q-004)"
  citationConventions:
    - "S-001 to S-011 are the scope-definition's approved scope items, adopted as statements in that artifact's order; S-012 to S-019 are added below from CEO-D-1319 and the feature request's done means"
    - "SC-01 to SC-42 cite the scope-definition's acceptance criteria 1 to 42, in that artifact's table order; SC-19 (refuse a signed-in page) is withdrawn by CEO-D-1319 item 1 and replaced by its converse under S-012"
    - "X-001 to X-014 cite the scope-definition's exclusions; SD-1 to SD-10 cite its scope decisions 1 to 10; its nine questions are cited by their answers in CEO-D-1319 or by this plan's own questions"
  affectedAreas: "security (S-004, S-005, S-012, S-013, S-016; untrusted hand-over, request guard, image egress, the operator's signed-in session); database not affected (X-009, A-009); performance not triggered"
  repositoryContext: "task context named no modules; the file list of src/MediaCompany.Pilot.Web was read once to confirm the MC-18 parts the scope reuses (fill record, paste text parser, request guard, read switch); no wider scan"
```

## Executive Summary

MC-19 gives the operator of the Vietnamese pilot web app a one-press button in their own Chrome browser: pressed on an open shopee.vn product page, it copies that page's visible facts as one block, and one paste at step 1 of the open video fills the same fields and candidate rows that MC-18's read and paste fill. Success is the done condition the CEO approved: in the Browser pane against a code-generated fixture, one press and one paste end in filled, confirmable fields that the operator confirms and the unchanged MC-16 check accepts, and with the switch off a press ends in a refusal (SC-41), in at most two operator actions (SC-05). The work is 28 tasks in 11 execution waves: two design tasks, thirteen implementation tasks, eleven quality-review tasks and two closing tasks; verification is owned by dev-2-reviewer because qa owns no phase in implement-feature (A-010). The app sends no request to any shopee.vn page, the hand-over is bounded untrusted data shown only as text and accepted only through the unchanged request guard, and image egress stays under the MC-18 rules. The highest-impact risk is R-002, a real page laid out differently from the fixture, which no agent may check; the CEO's single real-page try, recorded by the orchestrator, is its first real evidence, and paste mode stays the fallback. Plan status is complete; no blocking question stands, and the five non-blocking questions Q-001 to Q-005 are owned.

## Business Objectives

- BO-1: The operator gets one product's facts and image choices from the page already open in their own browser into step 1 with at most two actions, one press and one paste. Receiver: the operator. Measured by SC-01 to SC-07 and the done condition SC-41. Traces S-001, S-010, S-013, S-015, S-018.
- BO-2: Every fact the button supplies is reviewed by the operator and judged by the unchanged fact-sheet check before it is saved. Receiver: the CEO, who reviews each fact and approves each video. Measured by SC-08 to SC-13. Traces S-002, S-014.
- BO-3: The company's exposure under Shopee's terms stays inside the bounds the CEO accepted: the button reads only the page it is pressed on and acts on nothing there, no account data leaves the page, the app sends nothing to shopee.vn pages, image egress stays under the MC-18 rules, one switch stops it, and no agent opens a Shopee host. Receiver: the CEO, who carries the terms risk. Measured by SC-14 to SC-18, SC-20, SC-21, SC-28 to SC-30, SC-38 to SC-40. Traces S-003, S-004, S-006, S-009, S-012.
- BO-4: The operator's machine and the app stay safe from whatever the hand-over carries. Receiver: the operator. Measured by SC-22 to SC-27. Traces S-005, S-013.
- BO-5: Step 1 keeps working as delivered at MC-18 close, with the toolchain unchanged. Receiver: the operator. Measured by SC-34 to SC-37. Traces S-008.
- BO-6: The operator sets the button up once from the app with Vietnamese instructions, and an out-of-date button cannot fill a sheet. Receiver: the operator. Measured by SC-31 to SC-33. Traces S-007, S-016, S-017.
- BO-7: The CEO sees the button work once on a real page and can review the delivered change and its evidence before any merge. Receiver: the CEO. Measured by SC-42 and the compare link with the release note. Traces S-011, S-019.

## Technical Objectives

- TO-1: A press on a product page places on the clipboard one versioned block holding only the listed kinds, equal to the page's values after Unicode NFC normalisation, while loading zero code, sending zero requests, activating zero controls and reading no account name, cookie or stored value. Verified by SC-01, SC-02, SC-04, SC-06, SC-16 to SC-18, SC-33. Traces BO-1, BO-3.
- TO-2: A pasted block fills step 1 of the open video through the MC-18 fill record with a new source mark, per-field confirmation, at most three rows and a one-action screenshot, and the unchanged check decides the save. Verified by SC-01 to SC-13. Traces BO-1, BO-2.
- TO-3: The app accepts a block only within the bounds, well-formed, single-product and of the current version, only through the unchanged request guard, and shows its text only as text. Verified by SC-20, SC-22 to SC-27, SC-33. Traces BO-4.
- TO-4: Only picked image addresses the block carries are fetched, from shopee.vn or hosts ending .susercontent.com, one request each, no redirect, PNG or JPEG only, and zero requests go to any shopee.vn page. Verified by SC-14, SC-15, SC-21. Traces BO-3.
- TO-5: The existing read switch refuses the hand-over and no new switch is added. Verified by SC-28 to SC-30. Traces BO-3.
- TO-6: The toolchain shows zero file changes with 251 tests passing, the 53 web tests pass, one listener stays on 127.0.0.1 and toolchain jobs own zero sockets. Verified by SC-34 to SC-37. Traces BO-5.
- TO-7: The change's tests run offline against code-generated fixtures on 127.0.0.1, adding zero media files and zero package references. Verified by SC-38, SC-39. Traces BO-3.
- TO-8: "Cài đặt" offers the current button with Vietnamese drag instructions, and every new text meets the Vietnamese language rule. Verified by SC-31, SC-32. Traces BO-6.

## Scope

### In Scope

- S-001: With one product page open on shopee.vn in their own browser, the operator presses the button once and step 1 of the open video is filled with product name, classification, shop name and Mall, page address, press date and one candidate row per specification; price and description are shown for reference; the product's own image addresses are offered for picking. Tasks: T-001, T-003, T-004, T-005.
- S-002: Every button-filled field and row carries a visible mark of the new source, saves only after the operator confirms each, at most three rows enter the sheet, unsupplied fields stay for the operator, and the MC-16 check decides the save unchanged. Tasks: T-006, T-007, T-008, T-009, T-010.
- S-003: The operator picks up to three offered images; the app fetches only image files whose addresses the hand-over carries, from shopee.vn or hosts ending .susercontent.com, under the MC-18 image rules; only the picked images reach the input set. Tasks: T-011, T-012.
- S-004: The button reads only the page it is pressed on and acts on nothing there, hands over no account, cookie or stored data, one product per press; the app sends no request to any shopee.vn page for this feature. Tasks: T-003, T-013, T-022.
- S-005: The hand-over is untrusted input: size-bounded, parsed as data, never executed, shown only as text, refused whole when malformed or over a bound, accepted only through the unchanged request guard. Tasks: T-014, T-015, T-016, T-017, T-022.
- S-006: The read switch stays the one off switch: with it off the hand-over is refused and paste mode and typing still complete step 1. Tasks: T-018, T-019.
- S-007: "Cài đặt" shows the button to drag to the bookmark bar with short Vietnamese instructions, and every new text is Vietnamese. Tasks: T-020, T-021, T-022.
- S-008: Read-from-link, paste mode and typing work as at MC-18 close; the MC-16 toolchain is unchanged with zero sockets; the app listens on 127.0.0.1 only. Task: T-023.
- S-009: Tests run offline against code-generated fixture pages on 127.0.0.1, no media file and no package is added, and no agent opens shopee.vn or any Shopee host. Tasks: T-024, T-025.
- S-010: The button is shown working in the Browser pane against a fixture product page, ending in filled, confirmable fields, and with the switch off ending in a refusal. Task: T-026.
- S-011: The CEO presses the button once on one real product page in the CEO's own browser after quality review, and the outcome is recorded. Task: T-027.
- S-012: The button may be pressed on a page where the operator is signed in to Shopee; the hand-over still carries no account name, cookie or stored data (CEO-D-1319 item 1). Tasks: T-003, T-013.
- S-013: The hand-over goes through the clipboard: one press copies one block and one paste at step 1 delivers it; no direct call from the shopee.vn page to 127.0.0.1, and the guard is not widened (CEO-D-1319 item 2). Tasks: T-004, T-016.
- S-014: One screenshot file chosen once is named on every button-filled row in one action, with the MC-16 check unchanged (CEO-D-1319 item 4). Tasks: T-008, T-010.
- S-015: A hand-over fills step 1 of the open video only and is refused when no video is open (CEO-D-1319 item 5). Tasks: T-004, T-005.
- S-016: The button's code is fixed in the bookmark and loads no code at a press; a block from an out-of-date button is refused with a request to drag the new one (CEO-D-1319 item 6). Tasks: T-014, T-020, T-021.
- S-017: The button must work in Chrome, the browser the CEO uses (CEO-D-1319 item 7). Tasks: T-001, T-020, T-026.
- S-018: No business measure is added beyond at most two operator actions per product and the Browser-pane demonstration (CEO-D-1319 item 8). Tasks: T-005, T-026.
- S-019: The change is committed and pushed on the MC-19 session branch, and the CEO receives the compare link (feature request, done means). Task: T-028.

### Out of Scope

- X-001 reading any page other than the one pressed on, and batch, repeated, scheduled or automatic presses: ticket hard constraint 1.
- X-002 sign-in by any tool, Shopee cookies, stored data or account details, affiliate.shopee.vn, affiliate links, any action on the operator's account: CEO-D-1313 item 2.
- X-003 CAPTCHA solving, forged headers, signatures or user agent, IP rotation, proxies, Shopee's internal data interface, a hidden browser inside the app: the CEO kept these bounds.
- X-004 any request by the app to a shopee.vn page for this feature, and any download except image files of the pressed product: CEO-D-1318 item 2.
- X-005 a screenshot taken by the button or the app: the button cannot take one; S-014 covers the operator's own screenshot.
- X-006 changes to the MC-16 fact-sheet check, its per-row screenshot requirement, banned-term list, price rule or any toolchain behaviour: the check stays the authority.
- X-007 widening the request guard, a cross-origin permission, a route without the per-start token, or a listener off 127.0.0.1: SD-2 and CEO-D-1319 item 2.
- X-008 removing or changing MC-18 read-from-link, paste mode or hand typing: SD-5.
- X-009 publishing, accounts, sellers, buying; frozen MC-13 and MC-14 material and the mediacompany database: frozen by CEO decision.
- X-010 changes to steps 2 to 7 beyond receiving the picked images of S-003: the ticket changes step 1 only.
- X-011 a package, CDN script, browser extension or any browser install other than one bookmark; a generative model or speech recognition: ticket hard constraint 7.
- X-012 merging any branch into main and any release, readiness or closure decision: owned by gate owners and the CEO.
- X-013 watching for Shopee's answer or account warnings and switching off automatically: the stop condition is the CEO's act.
- X-014 pages on any site other than shopee.vn: CEO-D-1318 names shopee.vn only.
- The signed-in refusal of SC-19: withdrawn by CEO-D-1319 item 1, which allows signed-in pages.

### Deferred

- A direct hand-over from the page to 127.0.0.1 in one action: in scope on a CEO decision after a Design Gate risk assessment, as X-007 requires.
- Support for browsers other than Chrome: in scope on a CEO decision naming the browser.
- A business measure beyond the action count, such as minutes per product: in scope when the CEO states one.
- A Vietnamese operator-guide page for the button beyond the "Cài đặt" instructions: in scope on a product-owner decision.

## Assumptions

| ID | Assumption | Basis | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | The app's new parts need only the installed .NET base class library and the button only plain browser script written in the repository, with zero added package references | S-009 | A package needs the CEO's approval by name before implementation starts | architect |
| A-002 | The Scope Gate answers restated by the orchestrator briefing are the CEO's recorded decision; checked here against the repository decision record, same nine answers | plan-wide | Tasks are built for a hand-over path or bound the CEO did not choose | orchestrator |
| A-003 | Chrome runs a bookmark-bar button on a shopee.vn product page and lets one press place one block on the clipboard | S-013, S-017 | No hand-over on real pages; paste mode stays the path; T-003 changes | architect |
| A-004 | A fixture grounded on the CEO's screenshot, the MC-18 paste fixture and the ticket's facts is close enough to a real page for the button's field finding | S-001 | The button misses fields on real pages; T-003 and T-024 change | architect |
| A-005 | The headless browser the web tests already drive can run the button on a fixture page and read the copied block, with no package download | S-009, S-010 | Automated evidence for SC-01, SC-05, SC-41 needs another route; T-024 and T-026 change | architect |
| A-006 | The button can be run on fixture pages on 127.0.0.1 while the app refuses a page address not on shopee.vn, through a seam only test code can set | S-009 | Offline tests of the page-address rule need another route; T-024 is redesigned | architect |
| A-007 | The unchanged MC-16 check accepts one screenshot file named on several rows, as the CEO's Scope Gate record states | S-014 | SC-13 needs a check change, which X-006 excludes; T-008 waits on a CEO decision | architect |
| A-008 | The page lets the product's own images be told apart from other products' images shown on it | S-001 | Own images cannot be offered safely; SC-04 fails; T-003 changes | architect |
| A-009 | The triggered database area needs no work: the mediacompany database is untouched and step 1 keeps its file storage | plan-wide | Database design and gate work are added; X-009 conflict | architect |
| A-010 | Verification tasks are owned by dev-2-reviewer, because qa owns no phase in implement-feature, and qa decides the Review and Verification Gates, as the MC-18 Planning Gate settled | plan-wide | Verification tasks move to qa and are never dispatched | orchestrator |
| A-011 | The orchestrator records the Browser-pane run of the done condition, and dev-2-reviewer runs the automated headless browser tests and cites that record, as the MC-18 Planning Gate settled | S-010 | T-026 also records the Browser-pane run itself | orchestrator |
| A-012 | Recording the CEO's real-page try, the commit, the push and the compare link are orchestrator closing acts; no agent task opens shopee.vn | S-011, S-019 | An agent task with a real-page access is added, which CEO-D-1318 forbids | orchestrator |
| A-013 | The MC-16 and MC-17 socket harness records the app's outbound connections with remote host and path, and its listeners, as at MC-18 close | S-008 | SC-21 and SC-36 need another evidence route; T-012 and T-023 change | architect |
| A-014 | The Browser pane can run the button on a fixture product page served on 127.0.0.1 | S-010 | The done condition is shown by the automated browser tests only | orchestrator |
| A-015 | The existing read switch can govern the hand-over with no new setting and no toolchain change | S-006 | SC-30 conflicts; T-018 changes | architect |
| A-016 | The paste reaches the app through a route that passes the unchanged guard, because it happens on the app's own page (SD-2) | S-005, S-013 | A guard change needs a CEO decision under X-007; T-016 changes | architect |

## Risks

| ID | Class | Trigger | Impact | Likelihood | Affects | Mitigation | Owner |
|---|---|---|---|---|---|---|---|
| R-001 | requirement | The briefing's restatement differs from the recorded Scope Gate decision (A-002 false) | Tasks build a path the CEO did not choose | low | plan-wide | A-002 was checked against the decision record; the Planning Gate confirms | orchestrator |
| R-002 | requirement | A real product page lays out the facts differently from the fixture (A-004 false) | The button misses fields on real pages; paste mode becomes the common path | medium | T-003, T-024 | T-001 grounds the fixture (Q-004); the CEO's try records each kind (T-027); T-023 keeps paste mode verified | architect |
| R-003 | requirement | The converse of SC-19 is not accepted at the Planning Gate (Q-005) | An acceptance criterion and a verification change | low | T-013 | The orchestrator settles Q-005 at the Planning Gate | product-owner |
| R-004 | technical | Chrome blocks the button or its clipboard write on a real shopee.vn page (A-003 false) | No hand-over on real pages | medium | T-003 | T-001 measures Chrome on the fixture; the CEO's try records the real outcome; paste mode stays | architect |
| R-005 | technical | The headless browser cannot run the button or read the copied block (A-005 false) | Automated evidence needs another route | medium | T-002, T-024, T-026 | T-002 names the route and records A-005 | architect |
| R-006 | technical | Own images cannot be told apart from other images (A-008 false) | Other products' images are offered; SC-04 fails | medium | T-003 | T-001 defines own-image identification; T-005 verifies SC-04 | architect |
| R-007 | technical | The check refuses one file named on several rows (A-007 false) | SC-13 needs a check change outside scope | low | T-008 | T-001 confirms A-007 or raises it for a CEO decision | architect |
| R-008 | technical | The socket harness records no remote host or path (A-013 false) | SC-21 and SC-36 evidence is incomplete | low | T-012, T-023 | T-002 names the evidence route | architect |
| R-009 | technical | The read switch cannot govern the hand-over without a new setting (A-015 false) | SC-30 conflicts | low | T-018 | T-001 places the switch rule | architect |
| R-010 | technical | The paste route cannot pass the unchanged guard (A-016 false) | A guard change needs a CEO decision | low | T-016 | T-001 states the route; X-007 holds until the CEO decides | architect |
| R-011 | technical | A needed capability is absent from the base class library (A-001 false) | Implementation waits on a CEO package approval | low | T-001 | T-001 records the no-package design or names the package | architect |
| R-012 | technical | The design needs storage beyond step 1's files or touches the mediacompany database (A-009 false) | Database work and an X-009 conflict | low | T-001 | T-001 states the storage boundary | architect |
| R-013 | dependency | The CEO's Design Gate decision is delayed or declines the design | All implementation waits or returns to design | low | T-024 | T-001 maps every applicable SC to a design element | orchestrator |
| R-014 | dependency | The unmerged MC-16, MC-17 or MC-18 branch changes after this branch is cut | Rebase and a re-run of the 251 and 53 tests | low | plan-wide | T-023 re-runs both suites on the final branch base | orchestrator |
| R-015 | dependency | Shopee changes its page layout at any time | The button stops finding fields; the operator falls back to paste mode | high | plan-wide | T-023 keeps paste mode verified; T-014 refuses a block with no product name | architect |
| R-016 | dependency | The CEO makes no real-page try (A-012) | S-011 closes unexercised | medium | T-027 | T-027 records a try count of zero | orchestrator |
| R-017 | dependency | The Browser pane cannot run the button on the fixture (A-014 false) | The done condition rests on the automated browser tests alone | low | T-026 | T-026 states the route used; Q-003 | orchestrator |
| R-018 | dependency | Chrome, the headless browser or the .NET SDK on the operator machine is changed or missing | Browser evidence needs a CEO decision or a manual run | low | T-026 | T-026 records the versions used | orchestrator |
| R-019 | security | Text in the hand-over is run as script or turned into page elements at step 1 | Script execution in the app's page on 127.0.0.1 | medium | T-015 | T-015 criteria; T-017 verifies SC-25; T-022 reviews | architect |
| R-020 | security | An oversized or malformed hand-over reaches the parser | Resource exhaustion or a partly filled sheet | medium | T-014 | T-014 refuses before parsing and whole; T-017 verifies; T-022 reviews | architect |
| R-021 | security | The button copies an account name, cookie or stored value from a signed-in page | Breach of CEO-D-1313 item 2 | medium | T-003 | T-003 criteria; T-013 verifies SC-18 and the signed-in fixture; T-022 reviews | architect |
| R-022 | security | A route carrying the hand-over skips or widens the request guard | A foreign page fills step 1 | medium | T-016 | T-016 criteria; T-017 verifies SC-26 and SC-27; T-022 reviews | architect |
| R-023 | security | An image address passes the host rule through a look-alike host, plain http, a data or script address, or a redirect | Egress beyond the CEO's bounds | medium | T-011 | T-011 criteria; T-012 verifies SC-14; T-022 reviews | architect |
| R-024 | security | The fixture seam is reachable in the shipped app (A-006 false) | The app accepts a page address off shopee.vn | low | T-024 | T-024 confines the seam to test code; T-022 reviews | architect |
| R-025 | security | Another page or program places a crafted block on the clipboard and the operator pastes it | Unreviewed text enters step 1 | low | T-014, T-015 | T-014 bounds and format; T-015 text only; every field still confirmed (T-007) | architect |
| R-026 | security | Another program on the machine reads the copied block from the clipboard | Exposure of public page facts; no account data per SC-18 | low | T-003 | T-003 keeps account data out; T-022 reviews | architect |
| R-027 | operational | Shopee answers adversely or warns the account while the button is in use | The CEO's terms risk materialises; nothing switches off by itself (X-013) | low | T-018 | T-018 makes the one switch refuse the hand-over | product-owner |
| R-028 | operational | An out-of-date button stays in the bookmark bar after an update | Its block is refused until the operator drags the new one | medium | T-014, T-020 | T-014 refuses it in Vietnamese; T-020 shows the current button | architect |
| R-029 | delivery | A task is owned by a role with no phase in implement-feature (A-010, A-011 or A-012 false) | The task is never dispatched and its evidence is missing | low | T-005, T-010, T-012, T-013, T-017, T-019, T-021, T-023, T-025, T-026, T-028 | Verification owned by dev-2-reviewer; T-028 is the orchestrator's closing act; Q-001, Q-002, Q-003 | orchestrator |
| R-030 | delivery | The compare link against main also shows the unmerged MC-16, MC-17 and MC-18 commits | MC-19 review is confused with earlier work | high | T-028 | T-028 states the branch base in the message | orchestrator |

## Task Breakdown

### T-001 Produce the MC-19 technical design package

- Owner: architect
- Complexity: L (confidence: medium)
- Depends on: none
- Traces to: S-001, S-003, S-004, S-005, S-006, S-012, S-013, S-014, S-015, S-016, S-017
- Status: ready
- Description: A technical design exists stating the hand-over block's format, version and bounds; how the button finds each handed-over kind and the product's own images; what a press does on a page with no product; how the paste reaches step 1 through the unchanged guard; how the button is run on fixture pages on 127.0.0.1 while the app accepts only shopee.vn page addresses; when picked images are fetched; the new source mark; how one screenshot names every button-filled row; how the read switch governs the hand-over; what grounds the fixture; and Chrome's behaviour measured on the fixture.
- Acceptance Criteria:
  - A-001, A-003, A-004, A-005, A-006, A-007, A-008, A-009, A-013, A-015 and A-016 are each recorded as confirmed, or replaced by a decision record at status Proposed
  - Each of SC-01 to SC-18 and SC-20 to SC-42 maps to at least one design element, and the converse of SC-19 under S-012 maps to one
  - The design adds zero package references, changes zero MC-16 toolchain files and zero request-guard rules, or names each exception for a CEO decision
  - Q-004 is answered with the grounding of the fixture page stated
- Gate: Design Gate

### T-002 Map every applicable acceptance criterion to a fixture case

- Owner: architect
- Complexity: M (confidence: medium)
- Depends on: T-001
- Traces to: S-009, S-010, S-012
- Status: ready
- Description: A validation map gives each of SC-01 to SC-18 and SC-20 to SC-42, and the converse of SC-19, one test level, one code-generated fixture case where one is needed, and one evidence item, so the build's tests and the quality-review verification bind to the same cases.
- Acceptance Criteria:
  - 41 of 41 applicable criteria and the SC-19 converse carry a test level and an evidence item
  - The fixture cases SC-38 lists (those of SC-01, SC-04, SC-06, SC-14, SC-18, SC-22, SC-23, SC-24, SC-25) are all named, each produced by code at test time
  - The browser-test route for SC-01, SC-05, SC-08, SC-09, SC-25, SC-31 and SC-41 needs no downloaded package, and A-005 is recorded as confirmed or not
  - The socket-harness route for SC-21, SC-28, SC-36, SC-37 and SC-38 is named, and A-013 is recorded as confirmed or not
- Gate: Design Gate

### T-003 Copy one hand-over block from the open product page with one press

- Owner: dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-001, T-024
- Traces to: S-001, S-004, S-012, S-016, A-003, A-004, A-008
- Status: assumption-dependent
- Description: Pressed on a product page, the button places on the clipboard one block of the current format version holding only name, classification, shop name, Mall, specification rows, price, description, the product's own image addresses and the page address; it loads no code, sends no request, activates no control and reads no account name, cookie or stored value; on a page with no product nothing reaches step 1 and a Vietnamese reason is shown.
- Acceptance Criteria:
  - On the fixture product page the copied values equal the fixture's after Unicode NFC normalisation, zero differences (SC-01), and the no-variant, non-Mall fixture yields an empty classification and Mall no (SC-02)
  - On the fixture with five own images and three other products' images, exactly the five own addresses are copied (SC-04)
  - During a press zero requests leave the page, zero code is loaded (SC-16, SC-33), and zero clicks, submissions or navigations are recorded (SC-17)
  - On the fixture carrying a signed-in account name, a cookie and a stored value, the block holds none of the three and is still copied (SC-18, S-012)
  - On the shop, search and nameless-product fixtures, zero fields are filled and a Vietnamese reason is shown, 3 of 3 (SC-06)
- Gate: Review Gate

### T-004 Fill step 1 of the open video from one pasted hand-over

- Owner: dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-003, T-014
- Traces to: S-001, S-013, S-015
- Status: ready
- Description: One paste of an accepted block at step 1 of the open video fills product name, classification, shop name, Mall, page address, press date and one candidate row per specification; price and description are shown for reference and held in no saved field; own image addresses are offered for picking; unsupplied fields stay empty; with no video open the block is refused and nothing is kept.
- Acceptance Criteria:
  - SC-01 and SC-02 hold at step 1 with zero differences, reached in two operator actions, one press and one paste (SC-05)
  - The saved sheet holds the price and description in zero fields (SC-03), and 5 of 5 own images and 0 of 3 other images are offered (SC-04)
  - After a paste the SC-10 unsupplied fields are empty and saving is refused naming one of them
  - With no video open a block is refused in Vietnamese naming the step to take, and zero fields are filled in any video afterwards (SC-07)
- Gate: Review Gate

### T-005 Verify the press-and-fill criteria

- Owner: dev-2-reviewer
- Complexity: M (confidence: medium)
- Depends on: T-004
- Traces to: S-001, S-015, S-018
- Status: ready
- Description: A verification record states pass or fail, with the evidence item T-002 names, for each of SC-01 to SC-07 on the built change.
- Acceptance Criteria:
  - 7 of 7 criteria carry a recorded result and its evidence item
  - SC-05 records the counted operator actions, at most two
  - SC-06 records three fixtures with zero fields filled, and SC-07 records the no-video refusal
  - Every fail names the task that owns the correction
- Gate: Verification Gate

### T-006 Mark the source of every button-filled field

- Owner: dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-004
- Traces to: S-002
- Status: ready
- Description: Each field and row filled from a pasted block shows a Vietnamese mark of the new source, distinct from the MC-18 read and paste marks; typed fields show none; editing a filled field removes its mark.
- Acceptance Criteria:
  - On the fixture, N of N button-filled fields and rows carry the new mark, distinct from both MC-18 marks (SC-08)
  - Typed fields carry zero marks (SC-08)
  - An edited filled field carries no mark afterwards, and the mark text meets the SC-32 language rule
- Gate: Review Gate

### T-007 Require confirmation of every button-filled field before save

- Owner: dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-006
- Traces to: S-002
- Status: ready
- Description: A sheet with N button-filled fields and rows saves only after the operator confirms all N, a refusal names an unconfirmed one, and at most three candidate rows enter the sheet as at MC-18 close.
- Acceptance Criteria:
  - With N minus 1 confirmed, saving is refused naming an unconfirmed field or row (SC-09)
  - With N confirmed, saving is accepted (SC-09)
  - On a fixture with five specification rows, adding a fourth row is refused (SC-12)
- Gate: Review Gate

### T-008 Name one screenshot on every button-filled row in one action

- Owner: dev-1-implement
- Complexity: S (confidence: low)
- Depends on: T-004
- Traces to: S-002, S-014, A-007
- Status: assumption-dependent
- Description: The operator chooses one screenshot file once and, in one action, it is named on every button-filled row; typed rows are untouched; the unchanged check decides the save.
- Acceptance Criteria:
  - One choice names the file on N of N button-filled rows (SC-13)
  - Rows the operator typed keep their own screenshot field unchanged by the action
  - The unchanged check accepts the sheet, with zero changes to the check (SC-13)
- Gate: Review Gate

### T-009 Route button-filled facts through the unchanged fact-sheet check

- Owner: dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-007, T-008
- Traces to: S-002
- Status: ready
- Description: A sheet filled from a pasted block is decided by the same MC-16 fact-sheet check and the same live banned-term warning as a typed sheet, with zero changes to the check, the banned-term list or the price rule.
- Acceptance Criteria:
  - For the eight fixtures of SC-11, pressed and typed results are equal in term, class and result, 8 of 8
  - The live warning names term and class for button-filled values as it does for typed values
  - Zero files of the MC-16 toolchain change against the branch base (SC-34)
- Gate: Review Gate

### T-010 Verify the mark, confirmation, screenshot and check criteria

- Owner: dev-2-reviewer
- Complexity: M (confidence: medium)
- Depends on: T-009
- Traces to: S-002, S-014
- Status: ready
- Description: A verification record states the result, with evidence, for SC-08 to SC-13.
- Acceptance Criteria:
  - 6 of 6 criteria carry a recorded result and its evidence item
  - SC-09 records the refusal at N minus 1 and the acceptance at N, and SC-11 records 8 of 8 comparisons
  - SC-13 records one file choice, the rows naming it and the accepted save
- Gate: Verification Gate

### T-011 Bring the operator's picked images from the hand-over's addresses

- Owner: dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-004
- Traces to: S-003
- Status: ready
- Description: Only image addresses the accepted block carries are fetched, only from shopee.vn or hosts ending .susercontent.com, one request each, with no redirect followed and a file kept only with a PNG or JPEG signature; the operator picks up to three and only those enter the input set; no request goes to any shopee.vn page.
- Acceptance Criteria:
  - Of the seven SC-14 addresses, 2 are fetchable and the other 5 get zero requests and are named; a redirecting and a non-image fixture are each rejected (SC-14)
  - The input set holds exactly the picked images and step 2 reports no image missing (SC-15)
  - Zero requests go to any shopee.vn page, the pressed page's address included, during press, pick and save (SC-21)
- Gate: Review Gate

### T-012 Verify the image and egress criteria

- Owner: dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-009, T-011
- Traces to: S-003, A-013
- Status: assumption-dependent
- Description: A verification record states the result, with evidence, for SC-14, SC-15 and SC-21 during a sample run of press, pick and save.
- Acceptance Criteria:
  - SC-14: the fixture image server log shows 2 of 7 fetched and zero requests for the other 5, with saved-file signatures
  - SC-15: the input folder holds the picked images and no other
  - SC-21: a socket harness record with remote hosts and paths shows only allowed image hosts for carried addresses
- Gate: Verification Gate

### T-013 Verify that a press acts on nothing and hands over no account data

- Owner: dev-2-reviewer
- Complexity: M (confidence: medium)
- Depends on: T-004
- Traces to: S-004, S-012
- Status: ready
- Description: A verification record states the result, with evidence, for SC-16, SC-17, SC-18, SC-20 and the converse of SC-19 under S-012.
- Acceptance Criteria:
  - SC-16 and SC-17: the browser request log and the page event probe during a press, counts zero
  - SC-18: zero occurrences of the three planted markers and zero fields outside the listed kinds
  - The signed-in fixture hands over normally with no account data, and the SC-20 two-product block is refused with zero fields filled
- Gate: Verification Gate

### T-014 Refuse a malformed, over-bound or out-of-date hand-over whole

- Owner: dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-001, T-024
- Traces to: S-005, S-016
- Status: ready
- Description: A block over 256 KiB, over 30 image addresses or over 100 specification rows, malformed, carrying more than one product or from an earlier format version is refused whole with a Vietnamese reason and fills nothing; a block within the bounds is accepted.
- Acceptance Criteria:
  - A 257 KiB block is refused before parsing and a 255 KiB block is accepted (SC-22)
  - Blocks with 31 addresses or 101 rows are refused whole (SC-23), and a two-product block is refused naming the one-product rule (SC-20)
  - The five malformed blocks of SC-24 are refused 5 of 5 with a Vietnamese reason, zero fields filled
  - A block of an earlier format version is refused asking the operator to drag the new button (SC-33)
- Gate: Review Gate

### T-015 Show every handed-over text only as text

- Owner: dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-004
- Traces to: S-005
- Status: ready
- Description: Every text field of an accepted block (name, classification, shop, specification attribute, specification value, price, description) is shown at step 1 character for character as text, and nothing in it runs or becomes a page element.
- Acceptance Criteria:
  - The SC-25 block's seven text fields are shown with zero differences from the block
  - The execution probe records zero script runs (SC-25)
  - Zero page elements are created from the block's text (SC-25)
- Gate: Review Gate

### T-016 Admit a hand-over only through the unchanged request guard

- Owner: dev-1-implement
- Complexity: S (confidence: low)
- Depends on: T-004
- Traces to: S-005, S-013, A-016
- Status: assumption-dependent
- Description: Every request that carries a block into the app passes the MC-17 request guard unchanged: exact origin, exact host and the per-start token; the guard accepts exactly what it accepted at MC-18 close.
- Acceptance Criteria:
  - Requests with a foreign Origin, no token, an earlier start's token or a foreign Host are refused with 403 and fill nothing, 4 of 4 (SC-26)
  - The guard grants no cross-origin permission and accepts exactly the origins, hosts and tokens of MC-18 close (SC-27)
  - The existing guard tests pass unmodified (SC-27)
- Gate: Review Gate

### T-017 Verify the untrusted-input criteria

- Owner: dev-2-reviewer
- Complexity: M (confidence: medium)
- Depends on: T-014, T-015, T-016
- Traces to: S-005
- Status: ready
- Description: A verification record states the result, with evidence, for SC-22 to SC-27.
- Acceptance Criteria:
  - SC-22 to SC-24: each fixture block's result recorded, with zero fields filled on every refusal
  - SC-25: probe count zero and zero text differences recorded
  - SC-26 and SC-27: 4 of 4 statuses recorded, and the unchanged guard test run attached
- Gate: Verification Gate

### T-018 Refuse the hand-over while the read switch is off

- Owner: dev-1-implement
- Complexity: S (confidence: low)
- Depends on: T-004
- Traces to: S-006, A-015
- Status: assumption-dependent
- Description: With the existing read switch off, a pasted block is refused with a Vietnamese message naming the switch and fills nothing, while paste mode and hand typing still save; the one setting governs both the MC-18 read and the hand-over.
- Acceptance Criteria:
  - With the switch off a block is refused in Vietnamese naming the switch, zero fields filled, zero outbound connections (SC-28)
  - With the switch off, paste mode and hand typing each save a sheet the check accepts (SC-29)
  - Exactly one setting turns off both paths and this change adds zero switches (SC-30)
- Gate: Review Gate

### T-019 Verify the off switch

- Owner: dev-2-reviewer
- Complexity: S (confidence: medium)
- Depends on: T-018
- Traces to: S-006
- Status: ready
- Description: A verification record states the result, with evidence, for SC-28 to SC-30.
- Acceptance Criteria:
  - SC-28: a browser run with the switch off and a socket harness count of zero
  - SC-29: two saves accepted with the switch off, one by paste and one by hand typing
  - SC-30: code inspection finds one setting, and a browser run toggling it changes both paths
- Gate: Verification Gate

### T-020 Show the current button with Vietnamese drag instructions in "Cài đặt"

- Owner: dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-003
- Traces to: S-007, S-016, S-017
- Status: ready
- Description: "Cài đặt" shows the current button with short Vietnamese instructions to drag it to Chrome's bookmark bar, and the shown button is the one T-003 delivers, of the current format version.
- Acceptance Criteria:
  - "Cài đặt" shows the button and the drag instructions (SC-31)
  - The shown button's address, run on the fixture product page, performs the hand-over of SC-01 (SC-31)
  - A block from the shown button is accepted as the current version, and the instructions meet the SC-32 language rule
- Gate: Review Gate

### T-021 Verify the settings button and its fixed code

- Owner: dev-2-reviewer
- Complexity: S (confidence: medium)
- Depends on: T-014, T-020
- Traces to: S-007, S-016
- Status: ready
- Description: A verification record states the result, with evidence, for SC-31 and SC-33.
- Acceptance Criteria:
  - SC-31: a browser run taking the button from "Cài đặt" and running it on the fixture, with the field comparison of SC-01
  - SC-33: fixture and browser request logs with zero script requests during a press
  - SC-33: the earlier-version fixture block refused with the drag-the-new-button message
- Gate: Verification Gate

### T-022 Review the MC-19 change

- Owner: dev-2-reviewer
- Complexity: L (confidence: medium)
- Depends on: T-009, T-011, T-015, T-016, T-018, T-020
- Traces to: S-004, S-005, S-007
- Status: ready
- Description: A review package records severity-classified findings and a verdict for the whole change, including a security review of the button's page reading, the block parser and bounds, text-only display, the guard on the hand-over route, image egress and the test seam, and a language reading of every new text.
- Acceptance Criteria:
  - Every changed file is covered by the review, and every finding carries a severity and the task that owns its correction
  - The code inspections SC-27, SC-30 and SC-39 require are recorded: the guard and every new route, the single switch setting, and no generative model or speech recognition
  - Every new label, instruction, message and refusal is read against SC-32, zero English words outside bracketed codes, file names and web addresses
  - A verdict is recorded
- Gate: Review Gate

### T-023 Verify the delivered behaviour and bounds are unchanged

- Owner: dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-009, T-011, T-015, T-016, T-018, T-020
- Traces to: S-008, A-013
- Status: assumption-dependent
- Description: A verification record states the result, with evidence, for SC-34 to SC-37 on the final branch base.
- Acceptance Criteria:
  - SC-34: zero toolchain file changes against the branch base, and 251 toolchain tests passing with none removed or weakened
  - SC-35: 53 existing web tests passing with none removed or weakened, and the web test folder showing additions only
  - SC-36 and SC-37: one listener on 127.0.0.1, and zero sockets in the toolchain job processes with the sampling interval recorded
- Gate: Verification Gate

### T-024 Serve every button fixture from code-generated loopback pages

- Owner: dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-002
- Traces to: S-009, A-005, A-006
- Status: assumption-dependent
- Description: Test code serves on 127.0.0.1, generated at test time, the fixture product, shop, search, nameless, signed-in and image pages and the fixture blocks the validation map names; the button runs on them; the app's shopee.vn page-address rule reaches them only through a seam that test code alone can set.
- Acceptance Criteria:
  - Every case SC-38 lists exists and is produced by test code
  - Zero media files are added to the repository; fixture images are generated in code (SC-39)
  - No setting reachable by the operator points the app at fixture pages, and the shipped page-address and image-host rules are unchanged by the seam (R-024)
- Gate: Review Gate

### T-025 Verify the offline test bounds

- Owner: dev-2-reviewer
- Complexity: M (confidence: medium)
- Depends on: T-009, T-011, T-015, T-016, T-018, T-020
- Traces to: S-009
- Status: ready
- Description: A verification record states the result, with evidence, for SC-38 to SC-40.
- Acceptance Criteria:
  - SC-38: a socket harness record over the test run shows zero connections off 127.0.0.1, and each listed fixture case is present
  - SC-39: a scan of added files finds zero media files, and project and package files show zero added package references against the branch base
  - SC-40: the implementation report and the review package each state their live Shopee accesses, count zero
- Gate: Verification Gate

### T-026 Verify the done condition with automated browser tests

- Owner: dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-009, T-018
- Traces to: S-010, S-017, S-018, A-005, A-011, A-014
- Status: assumption-dependent
- Description: Automated headless browser tests show one press and one paste ending in filled, confirmable fields that are confirmed and saved with the check accepting, and with the switch off one press ending in a refusal; the record cites the orchestrator's Browser-pane record of the same two paths (SC-41).
- Acceptance Criteria:
  - Both paths are reached in the automated tests: one save accepted and one refusal shown
  - The orchestrator's Browser-pane record is cited, showing both paths and an operator action count of at most two
  - Zero connections leave 127.0.0.1 during the runs, and the browser and SDK versions used are recorded
- Gate: Verification Gate

### T-027 Write the MC-19 release note

- Owner: documentation
- Complexity: S (confidence: low)
- Depends on: T-005, T-010, T-012, T-013, T-017, T-019, T-021, T-022, T-023, T-025, T-026
- Traces to: S-011, A-012
- Status: assumption-dependent
- Description: A release note records each applicable SC result with its evidence, SC-19 as withdrawn by the CEO's signed-in answer, the review verdict, residual risks with owners, the CEO's real-page try as recorded by the orchestrator or that none was made, and the branch base.
- Acceptance Criteria:
  - 41 of 41 applicable SC results are cited with their evidence, and SC-19 is stated as withdrawn with its converse's result
  - The real-page try is stated: zero, or one with each of the ten handed-over kinds stated as filled or not and zero agent accesses (SC-42)
  - Every open risk is listed with its owner, and the note states the branch is cut from the unmerged MC-18 branch
- Gate: Closure Gate

### T-028 Deliver the compare link to the CEO

- Owner: orchestrator
- Complexity: XS (confidence: high)
- Depends on: T-027
- Traces to: S-019
- Status: ready
- Description: The change is committed and pushed on the MC-19 session branch, and the CEO holds the compare link with the branch base stated; nothing is merged (X-012).
- Acceptance Criteria:
  - The compare link is delivered to the CEO
  - The message states that the base is the unmerged MC-18 branch on the unmerged MC-17 and MC-16 branches
  - The main branch is unchanged by this run
- Gate: Closure Gate

## Dependencies

### 8.1 Dependency Edges

| From | To | Type | Justification |
|---|---|---|---|
| CEO (owner of the unmerged MC-16, MC-17 and MC-18 branches) | T-001 | external | The design binds to the toolchain, web app, guard and fill record as they stand on those branches |
| T-001 | T-002 | produces-consumes | The validation map assigns the design's elements to fixture cases |
| T-002 | T-024 | produces-consumes | The fixture pages serve the cases the map names |
| CEO (Design Gate decision) | T-024 | policy-gate | Implementation starts only after the Design Gate |
| T-001 | T-003 | contract | The block format and field finding are defined by the design |
| T-024 | T-003 | produces-consumes | The button is run on the fixture pages |
| T-001 | T-014 | contract | Format version and bounds are defined by the design |
| T-024 | T-014 | produces-consumes | The refusals are tested on the fixture blocks |
| T-003 | T-004 | produces-consumes | The fill takes the block the button copies |
| T-014 | T-004 | contract | The fill consumes only an accepted block |
| T-003 | T-020 | produces-consumes | "Cài đặt" shows the button T-003 delivers |
| T-004 | T-005 | verification | SC-01 to SC-07 verify the press and fill |
| T-004 | T-006 | produces-consumes | Marks apply to button-filled fields |
| T-004 | T-008 | produces-consumes | The screenshot is named on button-filled rows |
| T-004 | T-011 | produces-consumes | Image fetching takes the accepted block's addresses |
| T-004 | T-013 | verification | SC-16 to SC-18 and SC-20 verify the press through the fill |
| T-004 | T-015 | produces-consumes | Text-only display applies to the filled text |
| T-004 | T-016 | produces-consumes | The guarded route is the one the fill uses |
| T-004 | T-018 | contract | The switch governs the hand-over fill |
| T-014 | T-021 | verification | SC-33 verifies the earlier-version refusal |
| T-020 | T-021 | verification | SC-31 verifies the settings button |
| T-006 | T-007 | contract | Confirmation applies to the marked fields |
| T-014 | T-017 | verification | SC-22 to SC-24 verify the refusals |
| T-015 | T-017 | verification | SC-25 verifies text-only display |
| T-016 | T-017 | verification | SC-26 and SC-27 verify the guard |
| T-018 | T-019 | verification | SC-28 to SC-30 verify the switch |
| T-007 | T-009 | produces-consumes | The check receives the confirmed sheet |
| T-008 | T-009 | produces-consumes | The check receives rows naming the screenshot |
| T-009 | T-010 | verification | SC-08 to SC-13 verify the path to save |
| T-009 | T-012 | verification | The sample run saves a sheet |
| T-011 | T-012 | verification | SC-14, SC-15 and SC-21 verify image fetching |
| T-009 | T-022 | verification | The review covers the fill-to-check path |
| T-011 | T-022 | verification | The review covers image egress |
| T-015 | T-022 | verification | The review covers text-only display |
| T-016 | T-022 | verification | The review covers the guard |
| T-018 | T-022 | verification | The review covers the switch |
| T-020 | T-022 | verification | The review covers the settings button and its text |
| T-009 | T-023 | verification | SC-34 to SC-37 run on the complete change |
| T-011 | T-023 | verification | SC-34 to SC-37 run on the complete change |
| T-015 | T-023 | verification | SC-34 to SC-37 run on the complete change |
| T-016 | T-023 | verification | SC-34 to SC-37 run on the complete change |
| T-018 | T-023 | verification | SC-34 to SC-37 run on the complete change |
| T-020 | T-023 | verification | SC-34 to SC-37 run on the complete change |
| T-009 | T-025 | verification | SC-38 and SC-39 scan the complete change |
| T-011 | T-025 | verification | SC-38 and SC-39 scan the complete change |
| T-015 | T-025 | verification | SC-38 and SC-39 scan the complete change |
| T-016 | T-025 | verification | SC-38 and SC-39 scan the complete change |
| T-018 | T-025 | verification | SC-38 and SC-39 scan the complete change |
| T-020 | T-025 | verification | SC-38 and SC-39 scan the complete change |
| T-009 | T-026 | verification | The done condition saves a sheet the check accepts |
| T-018 | T-026 | verification | The done condition ends in a refusal with the switch off |
| orchestrator (records the Browser-pane run of the done condition) | T-026 | external | The verification cites the orchestrator's Browser-pane record (A-011) |
| CEO (operator machine installs) | T-026 | external | The tests use the installed Chrome, headless browser and .NET SDK |
| T-005 | T-027 | produces-consumes | The note cites SC-01 to SC-07 results |
| T-010 | T-027 | produces-consumes | The note cites SC-08 to SC-13 results |
| T-012 | T-027 | produces-consumes | The note cites SC-14, SC-15, SC-21 results |
| T-013 | T-027 | produces-consumes | The note cites SC-16 to SC-18, SC-20 and the SC-19 converse |
| T-017 | T-027 | produces-consumes | The note cites SC-22 to SC-27 results |
| T-019 | T-027 | produces-consumes | The note cites SC-28 to SC-30 results |
| T-021 | T-027 | produces-consumes | The note cites SC-31 and SC-33 results |
| T-022 | T-027 | produces-consumes | The note cites the review verdict and SC-32 |
| T-023 | T-027 | produces-consumes | The note cites SC-34 to SC-37 results |
| T-025 | T-027 | produces-consumes | The note cites SC-38 to SC-40 results |
| T-026 | T-027 | produces-consumes | The note cites the SC-41 result |
| CEO (presses the button once on a real page; the orchestrator records the report) | T-027 | external | The note records the SC-42 outcome, or that no try was made |
| T-027 | T-028 | produces-consumes | The pushed branch carries the release note |

### 8.2 External Dependencies

| Responsible party | What is needed | Blocks |
|---|---|---|
| CEO (owner of the unmerged MC-16, MC-17 and MC-18 branches) | Those branches unchanged during MC-19 (R-014) | T-001 |
| CEO (Design Gate decision) | Approval of the T-001 design (R-013) | T-024 |
| orchestrator (records the Browser-pane run of the done condition) | One recorded Browser-pane run of both done-condition paths on the fixture (R-017) | T-026 |
| CEO (operator machine installs) | As owner of the operator machine: the installed Chrome, headless browser and .NET SDK (R-018) | T-026 |
| CEO (presses the button once on a real page; the orchestrator records the report) | One press on one real product page in the CEO's own browser after quality review, reported and recorded, no agent access (R-016) | T-027 |

### 8.3 Implementation Order

Waves are computed over the task-to-task edges of 8.1; an external or policy-gate edge holds its target task within that task's wave until the party in 8.2 delivers, and the real-page edge releases T-027 when the CEO's try is recorded or the CEO declines to make one.

- Wave 1: T-001
- Wave 2: T-002
- Wave 3: T-024
- Wave 4: T-003, T-014
- Wave 5: T-004, T-020
- Wave 6: T-005, T-006, T-008, T-011, T-013, T-015, T-016, T-018, T-021
- Wave 7: T-007, T-017, T-019
- Wave 8: T-009
- Wave 9: T-010, T-012, T-022, T-023, T-025, T-026
- Wave 10: T-027
- Wave 11: T-028

## Suggested Workflow

Selected workflow: `implement-feature`

Selected because: MC-19 is new functionality on step 1 of the delivered web app, routed as `/implement`; this plan recommends the workflow and does not start it.

| Phase | Tasks |
|---|---|
| `solution-design-and-risk-assessment` | T-001, T-002 |
| `implementation` | T-003, T-004, T-006, T-007, T-008, T-009, T-011, T-014, T-015, T-016, T-018, T-020, T-024 |
| `quality-review` | T-005, T-010, T-012, T-013, T-017, T-019, T-021, T-022, T-023, T-025, T-026 |
| `documentation-and-release-handoff` | T-027, T-028 (T-028 is the orchestrator's closing act, see Q-001) |

| Gate | Required owners |
|---|---|
| Scope Gate | product-owner, business-analyst (decided: the CEO, CEO-D-1319) |
| Planning Gate | tech-lead, orchestrator (this plan is its evidence) |
| Design Gate | architect, tech-lead; producer exclusion leaves tech-lead; the CEO decides per the feature request |
| Review Gate | dev-2-reviewer, qa; producer exclusion leaves qa for the review package |
| Verification Gate | qa |
| Closure Gate | orchestrator, documentation |

## Required Capabilities

### 10.1 Agent Capabilities

| Capability | Tasks | Owning agent | Proficiency |
|---|---|---|---|
| technical-approach-definition | T-001 | architect | Primary |
| architecture-decision-authoring | T-001 | architect | Primary |
| validation-design | T-002 | architect | Secondary |
| implementation-delivery | T-003, T-004, T-006, T-007, T-008, T-009, T-011, T-014, T-015, T-016, T-018, T-020, T-024 | dev-1-implement | Primary |
| quality-verification | T-005, T-010, T-012, T-013, T-017, T-019, T-021, T-023, T-025, T-026 | dev-2-reviewer | Primary |
| code-review | T-022 | dev-2-reviewer | Primary |
| release-communication | T-027 | documentation | Primary |
| run-coordination | T-028 | orchestrator | Primary |

### 10.2 Required Skills

| Skill | File | Tasks | Level |
|---|---|---|---|
| S01 | skills/architecture/clean-architecture-checklist.md | T-001, T-022 | Primary |
| S02 | skills/business/domain-modeling.md | T-002 | Secondary |
| S03 | skills/dotnet/engineering-playbook.md | T-001, T-003, T-004, T-006, T-007, T-008, T-009, T-011, T-014, T-015, T-016, T-018, T-020, T-024 | Primary |
| S06 | skills/database/database-engineering.md | T-001 | Advisory |
| S07 | skills/testing/testing-strategy.md | T-002, T-005, T-010, T-012, T-013, T-017, T-019, T-021, T-023, T-024, T-025, T-026 | Primary |
| S09 | skills/security/secure-engineering.md | T-001, T-003, T-011, T-013, T-014, T-015, T-016, T-017, T-022, T-024 | Primary |
| S10 | skills/git/git-collaboration.md | T-027, T-028 | Secondary |
| S11 | skills/logging/observability-logging.md | T-027 | Advisory |
| S12 | skills/error-handling/error-handling-strategy.md | T-014, T-018 | Secondary |

## Acceptance Criteria

1. BO-1 (the done condition): in the Browser pane against the fixture, one press and one paste end in filled, confirmable fields that the operator confirms and saves with the check accepting, and with the switch off one press ends in a refusal, in at most two operator actions (SC-41, SC-05). Evidence: the orchestrator's Browser-pane record and the automated browser test results.
2. BO-1: a press and a paste fill the listed fields equal to the fixture, keep price and description out of the sheet, offer exactly the product's own images, refuse non-product pages, and refuse a block when no video is open (SC-01 to SC-07). Evidence: the press-and-fill verification record with fixture comparisons.
3. BO-2: every button-filled field is marked and confirmed before save, at most three rows enter, one screenshot names every button-filled row in one action, and the check result on filled values equals the result on typed values (SC-08 to SC-13). Evidence: browser runs at N minus 1 and N confirmations, the 8-fixture comparison and the screenshot run.
4. BO-3: a press sends zero requests, activates no control and hands over no account data, on a signed-in fixture too; a two-product block is refused; the app fetches only carried image addresses from allowed hosts under the MC-18 rules and sends zero requests to shopee.vn pages (SC-14 to SC-18, SC-20, SC-21, SC-19 converse). Evidence: request logs, event probe, marker inspection, image server log and socket harness records.
5. BO-3: with the switch off the block is refused and paste and typing still save, one setting governs both paths; tests run offline with zero media files and zero packages, and agents made zero Shopee accesses (SC-28 to SC-30, SC-38 to SC-40). Evidence: switch-off browser runs, code inspection, test-run socket record, added-file scan and the access statements in the reports.
6. BO-4: over-bound, malformed, multi-product and out-of-date blocks are refused whole, handed-over text is shown only as text, and every hand-over request passes the unchanged guard (SC-22 to SC-27). Evidence: fixture block results, execution probe and the guard test run.
7. BO-5: the toolchain is unchanged with 251 tests passing, the 53 web tests pass, one listener stays on 127.0.0.1 and toolchain jobs own zero sockets (SC-34 to SC-37). Evidence: diff, test output and socket harness records.
8. BO-6: "Cài đặt" shows the current button with Vietnamese instructions, the shown button performs the hand-over on the fixture, it loads zero code at a press, and every new text meets the language rule (SC-31 to SC-33). Evidence: the settings-button run, request logs and the review's language reading.
9. BO-7: the CEO's real-page try is recorded with each of the ten kinds stated, or recorded as not made, and the CEO holds the compare link with the release note citing 41 of 41 applicable results (SC-42). Evidence: the orchestrator's record, the delivered message and the release note.

## Definition of Done

- [ ] Plan acceptance criteria 1 to 9 are verified with recorded evidence
- [ ] Scope, Planning, Design, Review, Verification and Closure Gates are decided, with the deciding owner recorded for each
- [ ] Every task's acceptance criteria are satisfied, or formally waived with the waiver recorded
- [ ] A-001 to A-016 are each confirmed or converted to a recorded decision
- [ ] R-001 to R-030 are each closed or accepted with a named owner
- [ ] Q-001 to Q-005 are each closed or explicitly accepted with the owner recorded
- [ ] The release note records the CEO's real-page try: zero, or one with the ten kinds stated
- [ ] The release note is published in the run evidence
- [ ] Durable outcomes are recorded to memory per `memory/memory-governance.md`

## Open Questions

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| Q-001 | Framework gap: orchestrator owns no phase in implement-feature, so T-028 and the recording of the CEO's real-page try are closing acts the orchestrator performs rather than dispatched tasks; does the Planning Gate accept T-028 mapped to `documentation-and-release-handoff` on that basis, as at MC-18? | non-blocking | orchestrator | T-027, T-028 |
| Q-002 | Framework gap: qa owns no phase in implement-feature, so the verification tasks are owned by dev-2-reviewer (A-010) while qa decides the Review and Verification Gates; does the Planning Gate confirm this assignment, as at MC-18? | non-blocking | orchestrator | T-005, T-010, T-012, T-013, T-017, T-019, T-021, T-023, T-025, T-026 |
| Q-003 | Does the orchestrator record the Browser-pane run of the done condition, with dev-2-reviewer running the automated headless browser tests and citing that record (A-011), as at MC-18? | non-blocking | orchestrator | T-026 |
| Q-004 | No agent may view a real page: what grounds the fixture page's layout? The scope recommends the CEO's screenshot, the MC-18 paste fixture and the ticket's facts, with the CEO's try as the first real check; answered by the architect in the design | non-blocking | architect | T-001, T-003, T-024 |
| Q-005 | The CEO's answer allowing signed-in pages withdraws SC-19; does the gate accept its converse (a signed-in fixture hands over normally with no account data) in its place, making 41 applicable criteria? | non-blocking | product-owner | T-002, T-013, T-027 |
