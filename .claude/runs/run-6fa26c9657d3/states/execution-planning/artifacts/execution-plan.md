```yaml
plan:
  planId: PLAN-run-6fa26c9657d3-MC-18
  sourceInputs:
    - type: feature-request
      reference: runs/inputs/mc-18-feature-request.md
  producedBy: planner
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  inputDigest: sha256:3be564296ce7dfbb7c47644756a9d6c5
  contextDigest: sha256:2de1c2c2ba837051eb22fa9c512b030c
  upstreamArtifacts:
    - "scope-definition at runs/run-6fa26c9657d3/states/scope-and-acceptance/artifacts/scope-definition.md (sha256:0379ea41a66f31f30be2c251b71b0fa3), approved at the Scope Gate by the CEO as CEO-D-1314"
  gateDecisionsRestated:
    - "Scope Gate approved by the CEO (CEO-D-1314), restated by the orchestrator briefing because the runtime does not carry gate rationale downstream"
    - "Image hosts: the read may fetch the supplied product's image files from Shopee image hosts outside shopee.vn, image files only, never a page (CEO-D-1314 item 1)"
    - "Short links: s.shopee.vn redirects are followed only while every hop is an allowed host, at most five hops, stopping on any other host including affiliate.shopee.vn (CEO-D-1314 item 2)"
    - "Live read: one real public product page, once, only on a link the CEO names in chat; done by the orchestrator after quality review, never by an agent task (CEO-D-1314 item 3)"
    - "Scope approved with its 34 acceptance criteria; price and description shown not saved; the operator attaches a screenshot per fact; unsupplied fields stay for the operator; a blocked read never retries (CEO-D-1314 item 4)"
  citationConventions:
    - "S-001 to S-011 are the scope-definition's approved scope items, adopted as statements in that artifact's order; S-012 to S-015 are added below"
    - "SC-01 to SC-34 cite the scope-definition's acceptance criteria 1 to 34, in that artifact's table order"
    - "X-001 to X-012 cite the scope-definition's exclusions; SD-1 to SD-8 cite its scope decisions 1 to 8; its six questions are cited by their answers in CEO-D-1314 or by this plan's own questions"
  affectedAreas: "security (S-007, S-008, S-012, S-013); database not affected (X-009, A-009); performance one bound only, the 1 s live warning of SC-10 (A-010)"
  repositoryContext: "task context named no modules; the file lists of src/MediaCompany.Pilot.Web and tests/MediaCompany.Pilot.Web.Tests were read once to confirm the paths the briefing names; no wider scan"
```

## Executive Summary

MC-18 adds a read step to step 1 of the Vietnamese pilot web app: the operator pastes one Shopee product or short link, presses one button, and the fact sheet form is filled from that product's public page while the product's own images are saved for the video, with paste mode as the full fallback. Success is the done condition the CEO approved (SC-34): in the Browser pane against the local fixture, one read fills step 1 and saves images, and one blocked read falls back to paste mode and saves, with every filled fact confirmed by the operator and decided by the unchanged MC-16 check. The work is 25 tasks in 11 execution waves: two design-phase tasks, eleven implementation tasks, nine quality-review tasks and two closing tasks; verification is owned by dev-2-reviewer because qa owns no phase in implement-feature (A-012). Every outbound request stays inside the CEO's bounds: allowed hosts only, at most five redirect hops, no cookie, credential, proxy or imitated header, no retry, and one switch turns the read off. The highest-impact risk is R-001: an unmodified read of a script-rendered real page may return no readable product, making paste mode the common path; the security risk with the widest reach is R-016, a host-rule bypass, which T-017 prevents and T-018 verifies. The single live read on a CEO-named link is an orchestrator act after quality review, planned as an external dependency of T-024. Plan status is complete; no blocking question stands, and the four non-blocking questions Q-001 to Q-004 are owned.

## Business Objectives

- BO-1: The operator gets one product's facts and own images into step 1 from one link and one click. Receiver: the operator. Measured by SC-01 to SC-06 and the done condition SC-34. Traces S-001, S-002, S-011, S-012, S-013.
- BO-2: Every fact a read or a paste supplies is reviewed by the operator and judged by the unchanged fact-sheet check before it is saved. Receiver: the CEO, who reviews each fact and approves each video. Measured by SC-07 to SC-11. Traces S-003, S-004.
- BO-3: Step 1 stays completable whatever Shopee does and whatever the switch says: a block falls back to paste mode, and with the read off paste mode and hand typing still save. Receiver: the operator. Measured by SC-12 to SC-17 and SC-28 to SC-30. Traces S-005, S-006, S-009.
- BO-4: The company's exposure under Shopee's terms stays inside the bounds the CEO accepted: one product per click, no sign-in, cookie or evasion, egress only to allowed hosts during a read, and live reads only on a CEO-named link. Receiver: the CEO, who carries the terms risk. Measured by SC-18 to SC-27, SC-31 to SC-33. Traces S-007, S-008, S-010, S-014.
- BO-5: The CEO can review the delivered change and its evidence before any merge. Receiver: the CEO. Measured by the compare link and the release note citing 34 of 34 results. Traces S-015.

## Technical Objectives

- TO-1: A read returns name, classification and specification rows equal to the page's values after Unicode NFC normalisation, and a paste returns the same rows; price and description are shown and saved in no field. Verified by SC-01 to SC-03, SC-15. Traces BO-1.
- TO-2: Only the supplied product's own image files, from allowed hosts or the allowed image hosts, are fetched and saved byte-equal, and only the three the operator picks enter the input set. Verified by SC-04 to SC-06. Traces BO-1.
- TO-3: Saving is refused until every filled field is confirmed, the check result on filled values equals the result on typed values, and the MC-16 toolchain's files and 251 tests are unchanged. Verified by SC-07 to SC-11. Traces BO-2.
- TO-4: Each of the nine stop signals ends the read with zero further requests and a Vietnamese reason, next step and paste-mode offer. Verified by SC-12 to SC-14. Traces BO-3.
- TO-5: Read requests carry no cookie, authorization, proxy or imitating header, reach only allowed hosts within five hops, occur only between a click and its result, while the listener stays on 127.0.0.1 and toolchain jobs own zero sockets. Verified by SC-18 to SC-27. Traces BO-4.
- TO-6: One persisted setting disables the read entirely while paste mode and hand typing still save. Verified by SC-28 to SC-30. Traces BO-3.
- TO-7: The change's tests run offline against an in-process loopback fixture server generated in code, adding zero media files and zero package references. Verified by SC-31, SC-32. Traces BO-4.

## Scope

### In Scope

- S-001: At step 1 the operator pastes one affiliate or product link and presses one button; the form is filled from that product's public page on shopee.vn (product name, classification, page address, read date, one fact row per specification); price and description are shown beside the form. Tasks: T-001, T-003, T-004, T-005.
- S-002: The product's own images are saved for the video, the operator picks the three the sheet uses, and only those reach the input set. Tasks: T-005, T-006.
- S-003: Every field filled from the page or a paste carries a visible source mark, and the sheet saves only after each filled field is confirmed. Tasks: T-007, T-008.
- S-004: Filled facts meet the MC-16 fact-sheet check and the live banned-term warning exactly as typed facts do, and the check is unchanged. Tasks: T-009, T-010, T-011.
- S-005: When Shopee refuses access, asks for sign-in, shows a CAPTCHA, gives no readable product, fails or does not answer, the read stops, says why in Vietnamese and offers paste mode. Tasks: T-012, T-013.
- S-006: In paste mode the operator pastes page text from their own browser and the same fields are filled with no network connection; hand typing stays available. Tasks: T-013, T-014.
- S-007: A read covers one product per click, with no sign-in, cookie or session, never on affiliate.shopee.vn, with no CAPTCHA solving, forged header, signature or user agent, IP rotation or proxy. Tasks: T-015, T-016.
- S-008: The app connects off the machine only to the allowed hosts and only while a read runs; the MC-16 toolchain and video composition keep zero sockets; the listener stays on 127.0.0.1. Tasks: T-017, T-018.
- S-009: One switch in one place turns the read off, and with it off paste mode and hand typing still complete step 1. Tasks: T-019, T-020.
- S-010: Tests run offline against a code-generated local fixture server; no media file and no package is added; agents read no live shopee.vn page during development. Tasks: T-021, T-022.
- S-011: The operator completes step 1 in the Browser pane against the local fixture: one read that fills and saves images, and one blocked read that falls back to paste mode and saves. Task: T-023.
- S-012: The read may fetch the supplied product's image files from Shopee image hosts outside shopee.vn (for example the susercontent.com hosts), image files only, never a page (CEO-D-1314 item 1). Tasks: T-006, T-017.
- S-013: Short links on s.shopee.vn are followed only while every hop is an allowed host, at most five hops, stopping on any other host including affiliate.shopee.vn (CEO-D-1314 item 2). Tasks: T-004, T-015, T-017.
- S-014: One real public product page may be read once, only on a link the CEO names in chat, by the orchestrator after quality review; no agent reads shopee.vn otherwise (CEO-D-1314 item 3). Tasks: T-022, T-024.
- S-015: The change is committed and pushed on the MC-18 session branch, and the CEO receives the compare link (feature request, done means). Tasks: T-024, T-025.

### Out of Scope

- X-001 reading any page other than the supplied product's, and batch, scheduled or background reads: CEO-D-1313 item 1.
- X-002 sign-in, cookies or session of any kind, opening affiliate.shopee.vn, affiliate link generation, commission or account data: CEO-D-1313 item 2.
- X-003 CAPTCHA solving, anti-bot evasion, IP rotation, proxies and repeated attempts past a block: CEO-D-1313 item 2; a retry is a further attempt to pass a block (SD-6).
- X-004 downloading anything except the supplied product's own images (other products' images, videos, page scripts, style sheets, fonts): CEO-D-1313 item 3 relaxes the no-download rule for those images only.
- X-005 a screenshot of the product page taken by the tool: the operator still attaches a screenshot per fact (CEO-D-1314 item 4).
- X-006 changes to the MC-16 fact-sheet check, banned-term list, price rule, script rules or any toolchain behaviour: the check stays the authority.
- X-007 monitoring Shopee's answer or account warnings and switching the read off automatically: the stop condition is the CEO's act.
- X-008 reading any site other than shopee.vn, including Shopee sites of other countries: CEO-D-1313 names shopee.vn only.
- X-009 publishing, accounts, sellers, buying; frozen MC-13 and MC-14 material and the mediacompany database: frozen by CEO decision.
- X-010 changes to steps 2 to 7 beyond receiving the images of S-002: the ticket changes step 1 only.
- X-011 a generative model, speech recognition, metered service, or any package, font or CDN script not approved by name: ticket hard constraint 8.
- X-012 merging any branch into main and any release, readiness or closure decision: owned by gate owners and the CEO.

### Deferred

- A business success measure with a threshold, such as the share of reads that fill without paste mode: in scope when the CEO states such a measure (A-005).
- A Vietnamese operator-guide update for the read step and paste mode: in scope on a product-owner decision.

## Assumptions

| ID | Assumption | Basis | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | The read, the parsers and the fixture server need only the installed .NET 10 base class library, with zero added package references | S-010 | A package needs the CEO's approval by name before implementation starts | architect |
| A-002 | A public product page fetched by an unmodified request carries the product's facts in a parseable form for at least some products | S-001 | Real reads stop at no readable product and paste mode becomes the only working path; T-003 changes | architect |
| A-003 | The allowed-host rule can be pointed at the loopback fixture server through a seam only test code can set, leaving the shipped rule unchanged | S-010 | Offline tests of host and redirect behaviour need a different route; T-021 is redesigned | architect |
| A-004 | Text a browser copies from a product page carries labels enough to locate name, classification and specification rows, so the code-generated paste fixture is representative | S-006 | Real pastes fill fewer fields; T-014's parser changes | architect |
| A-005 | The done condition SC-34 is the only success measure, as the CEO's Scope Gate approval added none | S-011 | An acceptance criterion and a verification task are added | product-owner |
| A-006 | The off switch is held in the web app's own settings and persists across restarts with no MC-16 toolchain change | S-009 | A toolchain change conflicts with X-006; T-019 changes | architect |
| A-007 | The MC-16 and MC-17 socket harness can record the app process's outbound connections with remote host and time, and its listeners | S-008 | SC-25 needs another evidence route; T-018 changes | architect |
| A-008 | The page's product data distinguishes the product's own images from other products' images shown on the same page | S-002 | Own images cannot be selected safely; SC-04 fails; T-003 and T-006 change | architect |
| A-009 | The triggered database area needs no work: the mediacompany database is untouched and step 1 keeps its existing file storage | plan-wide | Database design and gate work are added; X-009 conflict | architect |
| A-010 | The existing MC-17 live banned-term check answers within 1 s for read-filled values as it does for typed values | S-004 | SC-10 fails; design work on the live check is added | architect |
| A-011 | The live read on a CEO-named link is the orchestrator's act after quality review; no agent task reads shopee.vn | S-014 | An agent task with a live read is added, which CEO-D-1314 item 3 forbids for agents in this run | orchestrator |
| A-012 | Verification tasks are owned by dev-2-reviewer, because qa owns no phase in implement-feature and a qa-owned task would never be dispatched | plan-wide | Verification tasks move to qa and are not dispatched unless a phase is given to qa | orchestrator |
| A-013 | The installed headless browser the MC-17 tests drive can run the step 1 browser tests and supports the Browser-pane run with no package download | S-011 | SC-34 and the browser tests need a CEO decision or a manual run; T-023 changes | architect |
| A-014 | Shopee short links resolve through ordinary redirect responses, so following hops needs no page script | S-013 | Real short links always stop at paste mode; SC-02 is met on the fixture only | architect |

## Risks

| ID | Class | Trigger | Impact | Likelihood | Affects | Mitigation | Owner |
|---|---|---|---|---|---|---|---|
| R-001 | requirement | Real product pages give an unmodified read no readable product (A-002 false) | The read rarely fills; BO-1 holds on the fixture only and paste mode is the common path | high | T-001, T-003, T-004 | T-001 records the expected yield (Q-004); T-014 makes paste mode a full path; the orchestrator's live read records one real outcome | architect |
| R-002 | requirement | Real copied page text differs from the paste fixture (A-004 false) | Paste fills fewer fields and the operator types the rest | medium | T-014 | T-014 names every field not found; T-001 defines the parser's tolerance | architect |
| R-003 | requirement | The CEO asks for a business measure beyond SC-34 (A-005 false) | A plan criterion and a verification task are added | low | T-023, T-024 | The orchestrator raises it at the Planning Gate | product-owner |
| R-004 | technical | A needed capability is absent from the base class library (A-001 false) | Implementation waits on a CEO package approval | low | T-001, T-003, T-015 | T-001 records the no-package design or names the package | architect |
| R-005 | technical | Own images cannot be told apart from other products' images (A-008 false) | Other images are fetched against X-004; SC-04 fails | medium | T-003, T-006 | T-001 defines own-image identification; T-005 verifies SC-04 | architect |
| R-006 | technical | The socket harness records no host names or times (A-007 false) | SC-25 evidence is incomplete | medium | T-002, T-018 | T-002 names the evidence route for SC-25 | architect |
| R-007 | technical | The switch cannot persist without a toolchain change (A-006 false) | X-006 conflict | low | T-019 | T-001 places the setting in the web app | architect |
| R-008 | technical | The installed headless browser cannot run the browser tests or the Browser-pane run without a download (A-013 false) | SC-34 and browser-test evidence need a CEO decision or a manual run | low | T-002, T-023 | T-002 names the route; Q-003 settles who records the run | architect |
| R-009 | technical | The live warning on filled values takes longer than 1 s (A-010 false) | SC-10 fails | low | T-009 | T-010 measures the time; T-001 designs the path | architect |
| R-010 | technical | Real short links need page script to resolve (A-014 false) | Real short links always stop at paste mode | medium | T-015 | T-001 records it; SC-02 is still verified on the fixture | architect |
| R-011 | technical | The design needs storage beyond step 1's files or touches the mediacompany database (A-009 false) | Database work and an X-009 conflict | low | T-001 | T-001 states the storage boundary | architect |
| R-012 | dependency | The CEO's Design Gate decision is delayed or declines the design | All implementation waits or returns to design | low | T-017, T-021 | T-001 maps every SC to a design element for the decision | orchestrator |
| R-013 | dependency | The unmerged MC-16 or MC-17 branch changes after this branch is cut | Rebase and a re-run of the 251 and 36 tests | low | plan-wide | T-010 re-runs both suites on the final branch base | orchestrator |
| R-014 | dependency | The CEO names no link for the live read (A-011) | No real-page evidence; S-014 closes unexercised | medium | T-024 | T-024 records a live read count of zero | orchestrator |
| R-015 | dependency | Shopee changes its page structure or blocks reads at any time | Reads stop and fall back to paste mode | high | plan-wide | T-012 stops cleanly; T-014 keeps step 1 usable | architect |
| R-016 | security | A link or redirect hop passes the host rule through a look-alike host, an IP address, another country's site or a hop to a disallowed host | Egress beyond the CEO's bounds | medium | T-015, T-017 | T-017 refuses before connecting; T-018 verifies SC-24; T-011 reviews | architect |
| R-017 | security | The test seam that points reads at the fixture server is reachable in the shipped app (A-003 false) | A read could be pointed at an arbitrary host | low | T-017, T-021 | T-021 confines the seam to test code; T-011 reviews | architect |
| R-018 | security | The request sender keeps a cookie, honours a system proxy or adds a default header beyond the accepted list | Breach of CEO-D-1313 item 2 | medium | T-015 | T-015 criteria; T-016 verifies SC-20, SC-22, SC-23 | architect |
| R-019 | security | A foreign web page in the same browser triggers a read through the loopback app | A read with no operator click, against SC-19 | medium | T-004 | T-004 puts the read under the MC-17 request guard; T-011 reviews | architect |
| R-020 | security | A fetched image carries path segments in its name, is not an image, or is unbounded in size | A write outside the video folder or a non-image saved | medium | T-006 | T-006 criteria; T-011 reviews | architect |
| R-021 | operational | Shopee answers adversely or warns the account while the read is on | The CEO's terms risk materialises; nothing switches off by itself (X-007) | low | T-019 | T-019 gives one setting on the settings screen; T-024 states the stop condition | product-owner |
| R-022 | operational | The orchestrator's single live read meets a block, sign-in wall or CAPTCHA | No real fill is shown; no retry is allowed | medium | T-024 | Read once, stop, record the outcome in T-024 | orchestrator |
| R-023 | delivery | A task is owned by a role with no phase in implement-feature (A-012 false, or Q-001) | The task is never dispatched and its evidence is missing | low | T-005, T-010, T-013, T-016, T-018, T-020, T-022, T-023, T-025 | Verification owned by dev-2-reviewer; T-025 is performed by the orchestrator at closure; Q-001, Q-002 | orchestrator |
| R-024 | delivery | The compare link against main also shows the unmerged MC-16 and MC-17 commits | MC-18 review is confused with earlier work | high | T-025 | T-025 states the branch base in the message | orchestrator |

## Task Breakdown

### T-001 Produce the MC-18 technical design package

- Owner: architect
- Complexity: L (confidence: medium)
- Depends on: none
- Traces to: S-001, S-002, S-005, S-006, S-007, S-008, S-009, S-012, S-013
- Status: ready
- Description: A technical design exists stating, for the read step: how the host rule and the image-host rule apply to the link and to every redirect hop; the request header list proposed for acceptance at the Design Gate; how each of the nine stop signals is recognised; what the page parser and the paste parser take and return; how the product's own images are identified; where the one off switch lives; how tests reach the fixture server; the source marks and per-field confirmation; and the expected yield of an unmodified read on real pages.
- Acceptance Criteria:
  - A-001, A-002, A-003, A-004, A-006, A-008, A-009, A-010 and A-014 are each recorded as confirmed, or replaced by a decision record at status Proposed
  - Each of SC-01 to SC-34 maps to at least one design element
  - A request header list is proposed for the Design Gate with zero headers imitating another client and zero signatures (SC-22)
  - The design adds zero package references and changes zero MC-16 toolchain files, or names each exception for a CEO decision
  - Q-004 is answered, or recorded as open with paste mode stated as the expected common path
- Gate: Design Gate

### T-002 Map every acceptance criterion to a fixture case

- Owner: architect
- Complexity: M (confidence: medium)
- Depends on: T-001
- Traces to: S-001, S-002, S-003, S-004, S-005, S-006, S-007, S-008, S-009, S-010, S-011
- Status: ready
- Description: A validation map gives each of SC-01 to SC-34 one test level, one code-generated fixture case where one is needed, and one evidence item, so the build's tests and the quality-review verification bind to the same cases.
- Acceptance Criteria:
  - 34 of 34 criteria carry a test level and an evidence item
  - Every named fixture case is produced by code at test time, and the cases SC-31 lists (those of SC-02, SC-04, SC-06, SC-12, SC-15, SC-21, SC-24) are all present
  - The socket-harness evidence route for SC-16, SC-19, SC-25 to SC-28 and SC-31 is named, and A-007 is recorded as confirmed or not
  - The browser-test route for SC-05, SC-07, SC-08, SC-28 and SC-34 needs no downloaded package, and A-013 is recorded as confirmed or not
- Gate: Design Gate

### T-003 Parse a product page into the read result

- Owner: dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-001, T-021
- Traces to: S-001, S-002, A-002, A-008
- Status: assumption-dependent
- Description: Given the body of one product page, the parser returns the product name, classification, one row per specification (attribute, value as written, source section), price, description and the addresses of the product's own images, or reports that no readable product is present.
- Acceptance Criteria:
  - On the fixture product page the returned values equal the fixture's after Unicode NFC normalisation, zero differences (SC-01)
  - On the fixture page with five own images and three other products' images, exactly the five own image addresses are returned (SC-04)
  - On the fixture page with no readable product, the result is no readable product and no field is returned
  - The parser opens zero network connections in the change's automated tests
- Gate: Review Gate

### T-004 Fill step 1 from one link on one click

- Owner: dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-003, T-012
- Traces to: S-001, S-007, S-013
- Status: ready
- Description: At step 1 one pasted link and one button press fill product name, classification, page address, read date and one fact row per specification; price and description are shown marked as read and held in no saved field; unsupplied fields stay empty; no read starts without the click.
- Acceptance Criteria:
  - SC-01 and SC-03 pass in the change's automated tests
  - A full link and a short link to the same fixture product fill identical values (SC-02)
  - After a read, the unsupplied fields SC-09 lists are empty and saving is refused naming the first of them
  - A link field holding two links is refused naming the one-product rule, and an idle app with a link entered starts zero reads (SC-19)
  - A read request is accepted only under the MC-17 request guard (R-019)
- Gate: Review Gate

### T-005 Verify the read, image and review criteria

- Owner: dev-2-reviewer
- Complexity: M (confidence: medium)
- Depends on: T-006, T-009
- Traces to: S-001, S-002, S-003, S-012, S-013
- Status: ready
- Description: A verification record states pass or fail, with the evidence item T-002 names, for each of SC-01 to SC-09 on the built change.
- Acceptance Criteria:
  - 9 of 9 criteria carry a recorded result and its evidence item
  - SC-04 records the fixture server request log and the five saved-file digests
  - SC-08 records the refusal at N minus 1 confirmations and the acceptance at N
  - Every fail names the task that owns the correction
- Gate: Verification Gate

### T-006 Save the product's own images for the operator's pick

- Owner: dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-004
- Traces to: S-002, S-012, A-008
- Status: assumption-dependent
- Description: After a read, the product's own image files are fetched only from allowed hosts or the allowed image hosts, as image files only, and saved byte-equal into the video's folder; an image on any other host is named and not fetched; the operator picks three, and only those three reach step 2's input set.
- Acceptance Criteria:
  - 5 of 5 own images saved byte-equal, with zero requests for the three other images or any script, style sheet or font (SC-04)
  - The input set holds exactly the three picked images and step 2 reports no image missing (SC-05)
  - An image on a disallowed host receives zero requests, its name is shown, and the step 2 upload still accepts it (SC-06)
  - A response that is not an image file, or an image whose name carries path segments, writes nothing outside the video's folder (R-020)
- Gate: Review Gate

### T-007 Mark the source of every filled field

- Owner: dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-004, T-014
- Traces to: S-003
- Status: ready
- Description: Each field filled by a read or a paste (product name, classification, page address, read date, each fact row) shows a Vietnamese mark naming its source, and typed fields show none.
- Acceptance Criteria:
  - On the read fixture, N of N filled fields carry the read-from-page mark (SC-07)
  - On the paste fixture, N of N filled fields carry the from-paste mark (SC-07)
  - Typed fields carry zero marks, and the mark texts meet the SC-14 language rule
- Gate: Review Gate

### T-008 Require confirmation of every filled field before save

- Owner: dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-007
- Traces to: S-003
- Status: ready
- Description: A sheet with N filled fields saves only after the operator has confirmed all N, one by one, and a refusal names an unconfirmed field.
- Acceptance Criteria:
  - With N minus 1 confirmed, saving is refused naming an unconfirmed field (SC-08)
  - With N confirmed, saving is accepted (SC-08)
  - A typed field needs no confirmation, and the confirmation labels meet the SC-14 language rule
- Gate: Review Gate

### T-009 Route filled facts through the unchanged fact-sheet check

- Owner: dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-008
- Traces to: S-004, A-010
- Status: assumption-dependent
- Description: A sheet filled by a read or a paste is decided by the same MC-16 fact-sheet check and the same live banned-term warning as a typed sheet, with zero changes to the check, the banned-term list or the price rule.
- Acceptance Criteria:
  - For the eight fixtures of SC-10, read and typed results are equal in term, class and result, 8 of 8
  - The live warning names term and class within 1 s for read-filled values (SC-10)
  - Zero files of the MC-16 toolchain change against the branch base (SC-11)
- Gate: Review Gate

### T-010 Verify the check stays the authority

- Owner: dev-2-reviewer
- Complexity: M (confidence: medium)
- Depends on: T-009
- Traces to: S-004, S-006
- Status: ready
- Description: A verification record states the result, with evidence, for SC-10, SC-11 and SC-17 on the final branch base.
- Acceptance Criteria:
  - SC-10: 8 of 8 comparisons recorded, with the measured warning time
  - SC-11: zero toolchain file changes against the branch base, and 251 toolchain tests passing with none removed or weakened
  - SC-17: 36 existing web tests passing with none removed or weakened, and the web test folder showing additions only
- Gate: Verification Gate

### T-011 Review the MC-18 change

- Owner: dev-2-reviewer
- Complexity: L (confidence: medium)
- Depends on: T-006, T-009, T-019
- Traces to: S-004, S-007, S-008, S-009
- Status: ready
- Description: A review package records severity-classified findings and a verdict for the whole change, including a security review of outbound reads, the host rule, redirect handling, request headers, the request guard on the read route, the test seam and image file writes.
- Acceptance Criteria:
  - Every changed file is covered by the review
  - Every finding carries a severity and the task that owns its correction
  - The review records the code inspections SC-22, SC-23 and SC-30 require: the header-setting code, the absence of any proxy list or address pool, and the single switch setting
  - A verdict is recorded
- Gate: Review Gate

### T-012 Stop the read at the first stop signal

- Owner: dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-015
- Traces to: S-005
- Status: ready
- Description: On each of the nine stop signals SC-12 lists the read ends, sends no further request, shows in Vietnamese the reason and the next step, and offers paste mode.
- Acceptance Criteria:
  - 9 of 9 stop fixtures end the read with paste mode offered, and the no-answer case ends by 30 s (SC-12)
  - Zero requests follow a stop response, and zero requests submit a CAPTCHA answer or a sign-in form (SC-13)
  - Each stop message meets the SC-14 rule: Vietnamese, reason and next step, no English word outside bracketed codes, file names and web addresses
- Gate: Review Gate

### T-013 Verify the stop cases and paste mode

- Owner: dev-2-reviewer
- Complexity: M (confidence: medium)
- Depends on: T-009
- Traces to: S-005, S-006
- Status: ready
- Description: A verification record states the result, with evidence, for SC-12 to SC-16.
- Acceptance Criteria:
  - SC-12 and SC-13: per-case request logs for the nine fixtures, 9 of 9 stopped, zero requests after each stop
  - SC-14: the nine messages and the new step 1 labels read against the rule, zero English words outside the permitted items
  - SC-15 and SC-16: paste-versus-read comparison with zero differences, and a socket harness record of zero outbound during a paste run
- Gate: Verification Gate

### T-014 Parse pasted page text into the read fields

- Owner: dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-003
- Traces to: S-006, A-004
- Status: assumption-dependent
- Description: Text the operator pastes from their own browser fills product name, classification and each specification row as a read does, shows price and description, names each field it did not find, and opens no network connection.
- Acceptance Criteria:
  - The code-generated visible text of the fixture page fills the SC-15 fields with zero differences from the read of the same fixture
  - Each field not found in the paste is named to the operator in Vietnamese (SC-15)
  - Zero outbound connections occur during a paste fill and save (SC-16)
- Gate: Review Gate

### T-015 Send read requests within the CEO's bounds

- Owner: dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-001, T-017
- Traces to: S-007, S-013, A-014
- Status: assumption-dependent
- Description: Every request of a read carries only the header list accepted at the Design Gate with the reading component's own unmodified user agent, carries no cookie or authorization of any origin, uses no proxy, leaves from one local address, follows redirects hop by hop through the host rule up to five hops, and is never retried.
- Acceptance Criteria:
  - Zero Cookie and zero Authorization headers across all read fixtures, including after a fixture response sets a cookie (SC-20)
  - The header log equals the accepted list, with zero extra headers (SC-22)
  - A proxy set in the process environment receives zero connections, and each read's requests leave from one local address (SC-23)
  - A redirect chain longer than five hops stops with paste mode offered (SC-02)
- Gate: Review Gate

### T-016 Verify the request bounds of one read

- Owner: dev-2-reviewer
- Complexity: M (confidence: medium)
- Depends on: T-006
- Traces to: S-007
- Status: ready
- Description: A verification record states the result, with evidence, for SC-18 to SC-23.
- Acceptance Criteria:
  - SC-18: the request log of one click lists only the link, its hops, the product page and its own images, and a second product is read only after a second click
  - SC-19: a socket harness record over 10 idle minutes shows zero outbound, and the two-link fixture is refused
  - SC-20 to SC-23: the header log, the affiliate-host fixtures, the proxy fixture and the source-address records are each attached with their counts
- Gate: Verification Gate

### T-017 Admit only links and hops on the allowed hosts

- Owner: dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-001
- Traces to: S-008, S-012, S-013
- Status: ready
- Description: A link or redirect hop is admitted only when its host is shopee.vn or one of its subdomains other than affiliate.shopee.vn, or, for an image file of the supplied product only, an allowed Shopee image host; every other host is refused before any connection, with paste mode offered.
- Acceptance Criteria:
  - The six links of SC-24 open zero connections and are refused, 6 of 6
  - A link on affiliate.shopee.vn, or a redirect hop to it, opens zero connections to that host and offers paste mode (SC-21)
  - An image host is admitted for image files only; a page address on an image host is refused
- Gate: Review Gate

### T-018 Verify the egress of the app

- Owner: dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-006, T-009, T-019
- Traces to: S-008, S-012, S-013, A-007
- Status: assumption-dependent
- Description: A verification record states the result, with evidence, for SC-24 to SC-27 during a sample run that includes a read.
- Acceptance Criteria:
  - SC-24: 6 of 6 links refused with zero connections each
  - SC-25: a socket harness record with connection times and remote host names, showing zero outbound while idle and during steps 2 to 7
  - SC-26 and SC-27: zero sockets in the toolchain job processes with the sampling interval recorded, and one listener on 127.0.0.1
- Gate: Verification Gate

### T-019 Turn the read off with one setting

- Owner: dev-1-implement
- Complexity: S (confidence: low)
- Depends on: T-004
- Traces to: S-009, A-006
- Status: assumption-dependent
- Description: One setting on the app's settings screen turns the read on and off without a file edit or a command and holds across a restart; with it off the read button is not offered, a direct read request is refused in Vietnamese, and paste mode and hand typing still save.
- Acceptance Criteria:
  - With the switch off, a direct read request is refused in Vietnamese and zero outbound connections occur (SC-28)
  - With the switch off, paste mode and hand typing each save a sheet the check accepts (SC-29)
  - Exactly one setting governs the read, and its off state holds across a restart (SC-30)
- Gate: Review Gate

### T-020 Verify the off switch

- Owner: dev-2-reviewer
- Complexity: S (confidence: medium)
- Depends on: T-009, T-019
- Traces to: S-009
- Status: ready
- Description: A verification record states the result, with evidence, for SC-28 to SC-30.
- Acceptance Criteria:
  - SC-28: browser run and direct request with the switch off, with a socket harness count of zero
  - SC-29: two saves accepted with the switch off, one by paste and one by hand typing
  - SC-30: code inspection finds one setting, and a browser run off, restart, still off is recorded
- Gate: Verification Gate

### T-021 Serve every read fixture from a code-generated loopback server

- Owner: dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-002
- Traces to: S-010, A-003
- Status: assumption-dependent
- Description: Test code starts an in-process fixture server on 127.0.0.1 that serves, generated at test time, the product pages, redirect chains, stop answers, images and paste texts the validation map names, and the shipped app reaches it only through a seam that test code alone can set.
- Acceptance Criteria:
  - Every case SC-31 lists exists and is produced by test code
  - Zero media files are added to the repository; fixture images are generated in code (SC-32)
  - No setting reachable by the operator points the read at the fixture server, and the shipped host rule is unchanged by the seam (R-017)
- Gate: Review Gate

### T-022 Verify the offline test bounds

- Owner: dev-2-reviewer
- Complexity: M (confidence: medium)
- Depends on: T-006, T-009, T-019
- Traces to: S-010, S-014
- Status: ready
- Description: A verification record states the result, with evidence, for SC-31 to SC-33.
- Acceptance Criteria:
  - SC-31: a socket harness record over the test run shows zero connections off 127.0.0.1
  - SC-32: a scan of added files finds zero media files, and project and package files show zero added package references against the branch base
  - SC-33: the implementation report and the review package each state their live reads of shopee.vn, count zero
- Gate: Verification Gate

### T-023 Record the Browser-pane run of step 1

- Owner: dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-006, T-009, T-019
- Traces to: S-011, A-005, A-013
- Status: assumption-dependent
- Description: A recorded Browser-pane run against the local fixture shows one read that fills step 1 and saves images, and one blocked read that offers paste mode, after which paste and per-field confirmation save a sheet the check accepts (SC-34).
- Acceptance Criteria:
  - Both paths are reached in the recorded run
  - Two saves are accepted by the check
  - Zero connections leave 127.0.0.1 during the run
- Gate: Verification Gate

### T-024 Write the MC-18 release note

- Owner: documentation
- Complexity: S (confidence: low)
- Depends on: T-005, T-010, T-011, T-013, T-016, T-018, T-020, T-022, T-023
- Traces to: S-014, S-015, A-011
- Status: assumption-dependent
- Description: A release note records each SC result with its evidence, the review verdict, the residual risks with owners, the live read outcome on the CEO-named link or that none was made, and the branch base.
- Acceptance Criteria:
  - 34 of 34 SC results are cited with their evidence
  - The live read count is stated: zero, or one on the CEO-named link with its outcome and no retry
  - Every open risk is listed with its owner, and the note states the branch is cut from the unmerged MC-17 branch
- Gate: Closure Gate

### T-025 Deliver the compare link to the CEO

- Owner: orchestrator
- Complexity: XS (confidence: high)
- Depends on: T-024
- Traces to: S-015
- Status: ready
- Description: The change is committed and pushed on the MC-18 session branch, and the CEO holds the compare link with the branch base stated; nothing is merged (X-012).
- Acceptance Criteria:
  - The compare link is delivered to the CEO
  - The message states that the base is the unmerged MC-17 branch on the unmerged MC-16 branch
  - The main branch is unchanged by this run
- Gate: Closure Gate

## Dependencies

### 8.1 Dependency Edges

| From | To | Type | Justification |
|---|---|---|---|
| CEO (owner of the unmerged MC-16 and MC-17 branches) | T-001 | external | The design binds to the delivered toolchain and web app as they stand on those branches |
| T-001 | T-002 | produces-consumes | The validation map assigns the design's elements to fixture cases |
| T-002 | T-021 | produces-consumes | The fixture server serves the cases the map names |
| CEO (Design Gate decision) | T-021 | policy-gate | Implementation starts only after the Design Gate |
| T-001 | T-017 | contract | The host rule is defined by the design |
| CEO (Design Gate decision) | T-017 | policy-gate | Implementation starts only after the Design Gate |
| T-017 | T-015 | contract | Every request and hop passes the host rule |
| T-001 | T-015 | contract | The header list is the one accepted at the Design Gate |
| T-015 | T-012 | produces-consumes | Stop signals are recognised in the sender's responses |
| T-001 | T-003 | contract | The read result's fields are defined by the design |
| T-021 | T-003 | produces-consumes | The parser is tested on the fixture pages |
| T-003 | T-004 | produces-consumes | The form is filled from the read result |
| T-012 | T-004 | contract | The fill step offers paste mode on a stop |
| T-003 | T-014 | contract | Paste fills the same read result fields |
| T-004 | T-006 | produces-consumes | Image saving takes the image addresses of the read |
| T-004 | T-007 | produces-consumes | Marks apply to read-filled fields |
| T-014 | T-007 | produces-consumes | Marks apply to paste-filled fields |
| T-007 | T-008 | contract | Confirmation applies to the marked fields |
| T-008 | T-009 | produces-consumes | The check receives the confirmed sheet |
| T-004 | T-019 | contract | The switch governs the read button and route |
| T-006 | T-005 | verification | SC-04 to SC-06 verify image saving |
| T-009 | T-005 | verification | SC-07 to SC-09 verify marks, confirmation and save |
| T-009 | T-010 | verification | SC-10 and SC-11 verify the check path |
| T-006 | T-011 | verification | The review covers image writes |
| T-009 | T-011 | verification | The review covers the fill-to-check path |
| T-019 | T-011 | verification | The review covers the switch |
| T-009 | T-013 | verification | SC-15 and SC-16 verify paste fill and save |
| T-006 | T-016 | verification | SC-18 covers the image requests of one click |
| T-006 | T-018 | verification | SC-24 to SC-27 run on the complete read |
| T-009 | T-018 | verification | The sample run saves a sheet |
| T-019 | T-018 | verification | The sample run passes the switch |
| T-009 | T-020 | verification | SC-29 saves with the switch off |
| T-019 | T-020 | verification | SC-28 to SC-30 verify the switch |
| T-006 | T-022 | verification | SC-31 and SC-32 scan the complete change |
| T-009 | T-022 | verification | SC-31 and SC-32 scan the complete change |
| T-019 | T-022 | verification | SC-31 and SC-32 scan the complete change |
| T-006 | T-023 | verification | The run saves images |
| T-009 | T-023 | verification | The run saves two sheets through the check |
| T-019 | T-023 | verification | The run uses the complete step 1 |
| CEO (operator machine installs) | T-023 | external | The run uses the installed headless browser and .NET 10 SDK |
| T-005 | T-024 | produces-consumes | The note cites SC-01 to SC-09 results |
| T-010 | T-024 | produces-consumes | The note cites SC-10, SC-11, SC-17 results |
| T-011 | T-024 | produces-consumes | The note cites the review verdict |
| T-013 | T-024 | produces-consumes | The note cites SC-12 to SC-16 results |
| T-016 | T-024 | produces-consumes | The note cites SC-18 to SC-23 results |
| T-018 | T-024 | produces-consumes | The note cites SC-24 to SC-27 results |
| T-020 | T-024 | produces-consumes | The note cites SC-28 to SC-30 results |
| T-022 | T-024 | produces-consumes | The note cites SC-31 to SC-33 results |
| T-023 | T-024 | produces-consumes | The note cites the SC-34 result |
| CEO (names one link for the orchestrator's single live read) | T-024 | external | The note records the live read outcome, or that no link was named |
| T-024 | T-025 | produces-consumes | The pushed branch carries the release note |

### 8.2 External Dependencies

| Responsible party | What is needed | Blocks |
|---|---|---|
| CEO (owner of the unmerged MC-16 and MC-17 branches) | The delivered toolchain and web app unchanged on those branches during MC-18 (R-013) | T-001 |
| CEO (Design Gate decision) | Approval of the T-001 design, including the request header list (R-012) | T-017, T-021 |
| CEO (operator machine installs) | As owner of the operator machine: the installed .NET 10 SDK and headless browser (R-008) | T-023 |
| CEO (names one link for the orchestrator's single live read) | One named public product link; the orchestrator reads it once after quality review, with no retry (R-014, R-022) | T-024 |

### 8.3 Implementation Order

Waves are computed over the task-to-task edges of 8.1; an external or policy-gate edge holds its target task within that task's wave until the party in 8.2 delivers, and the live-read edge releases T-024 when the CEO names a link and it is read, or declines to name one.

- Wave 1: T-001
- Wave 2: T-002, T-017
- Wave 3: T-015, T-021
- Wave 4: T-003, T-012
- Wave 5: T-004, T-014
- Wave 6: T-006, T-007, T-019
- Wave 7: T-008, T-016
- Wave 8: T-009
- Wave 9: T-005, T-010, T-011, T-013, T-018, T-020, T-022, T-023
- Wave 10: T-024
- Wave 11: T-025

## Suggested Workflow

Selected workflow: `implement-feature`

Selected because: MC-18 is new functionality on step 1 of the delivered web app, routed as `/implement`; this plan recommends the workflow and does not start it.

| Phase | Tasks |
|---|---|
| `solution-design-and-risk-assessment` | T-001, T-002 |
| `implementation` | T-003, T-004, T-006, T-007, T-008, T-009, T-012, T-014, T-015, T-017, T-019, T-021 |
| `quality-review` | T-005, T-010, T-011, T-013, T-016, T-018, T-020, T-022, T-023 |
| `documentation-and-release-handoff` | T-024, T-025 (T-025 is the orchestrator's closing act, see Q-001) |

| Gate | Required owners |
|---|---|
| Scope Gate | product-owner, business-analyst (decided: the CEO, CEO-D-1314) |
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
| implementation-delivery | T-003, T-004, T-006, T-007, T-008, T-009, T-012, T-014, T-015, T-017, T-019, T-021 | dev-1-implement | Primary |
| quality-verification | T-005, T-010, T-013, T-016, T-018, T-020, T-022, T-023 | dev-2-reviewer | Primary |
| code-review | T-011 | dev-2-reviewer | Primary |
| release-communication | T-024 | documentation | Primary |
| run-coordination | T-025 | orchestrator | Primary |

### 10.2 Required Skills

| Skill | File | Tasks | Level |
|---|---|---|---|
| S01 | skills/architecture/clean-architecture-checklist.md | T-001, T-011 | Primary |
| S02 | skills/business/domain-modeling.md | T-002 | Secondary |
| S03 | skills/dotnet/engineering-playbook.md | T-001, T-003, T-004, T-006, T-007, T-008, T-009, T-012, T-014, T-015, T-017, T-019, T-021 | Primary |
| S06 | skills/database/database-engineering.md | T-001 | Advisory |
| S07 | skills/testing/testing-strategy.md | T-002, T-005, T-010, T-013, T-016, T-018, T-020, T-021, T-022, T-023 | Primary |
| S08 | skills/performance/performance-engineering.md | T-010 | Advisory |
| S09 | skills/security/secure-engineering.md | T-001, T-004, T-006, T-011, T-015, T-016, T-017, T-018, T-021 | Primary |
| S10 | skills/git/git-collaboration.md | T-024, T-025 | Secondary |
| S11 | skills/logging/observability-logging.md | T-012, T-024 | Advisory |
| S12 | skills/error-handling/error-handling-strategy.md | T-012, T-015 | Secondary |

## Acceptance Criteria

1. BO-1 (the done condition, CEO-D-1314 item 4): in the Browser pane against the fixture, one read fills step 1 and saves images, and one blocked read offers paste mode, after which paste and per-field confirmation save a sheet the check accepts (SC-34). Evidence: the recorded Browser-pane run with two accepted saves.
2. BO-1: a read fills the listed fields equal to the fixture and saves exactly the product's own images, of which the operator's three reach the input set (SC-01 to SC-06). Evidence: the read verification record with fixture comparisons, request logs and saved-file digests.
3. BO-2: every filled field is marked and confirmed before save, and the check result on filled values equals the result on typed values with the toolchain unchanged and its 251 tests passing (SC-07 to SC-11). Evidence: browser runs at N minus 1 and N confirmations, the 8-fixture comparison, the toolchain diff and test output.
4. BO-3: each of the nine stop signals ends the read and offers paste mode, paste fills the same fields offline, hand typing still works, and with the switch off paste and typing still save (SC-12 to SC-17, SC-28 to SC-30). Evidence: per-case request logs, paste comparison, socket harness records and the 36 web tests.
5. BO-4: every request stays inside the CEO's bounds: one product per click, no cookie, authorization, proxy or imitating header, allowed hosts only within five hops, outbound only during a read, one listener on 127.0.0.1 and zero toolchain job sockets (SC-18 to SC-27). Evidence: header and request logs, the six refused-link fixtures and socket harness records.
6. BO-4: the tests run offline with zero media files and zero added packages, and agents made zero live reads, with at most one live read on the CEO-named link made by the orchestrator (SC-31 to SC-33). Evidence: test-run socket record, added-file scan, package comparison and the live read statements in the reports and release note.
7. BO-5: the CEO holds the compare link with the release note citing 34 of 34 results and the branch base stated. Evidence: the delivered message and the release note.

## Definition of Done

- [ ] Plan acceptance criteria 1 to 7 are verified with recorded evidence
- [ ] Scope, Planning, Design, Review, Verification and Closure Gates are decided, with the deciding owner recorded for each
- [ ] Every task's acceptance criteria are satisfied, or formally waived with the waiver recorded
- [ ] A-001 to A-014 are each confirmed or converted to a recorded decision
- [ ] R-001 to R-024 are each closed or accepted with a named owner
- [ ] Q-001 to Q-004 are each closed or explicitly accepted with the owner recorded
- [ ] The live read count is recorded in the release note: zero, or one on the CEO-named link
- [ ] The release note is published in the run evidence
- [ ] Durable outcomes are recorded to memory per `memory/memory-governance.md`

## Open Questions

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| Q-001 | Framework gap: orchestrator owns no phase in implement-feature, so T-025 and the single live read are closing acts the orchestrator performs rather than dispatched tasks; does the Planning Gate accept T-025 mapped to `documentation-and-release-handoff` on that basis? | non-blocking | orchestrator | T-024, T-025 |
| Q-002 | Framework gap: qa owns no phase in implement-feature, so the verification tasks are owned by dev-2-reviewer (A-012) while qa decides the Verification Gate; does the Planning Gate confirm this assignment? | non-blocking | orchestrator | T-005, T-010, T-013, T-016, T-018, T-020, T-022, T-023 |
| Q-003 | Can dev-2-reviewer record the Browser-pane run of SC-34 itself, or does the orchestrator record it in the Browser pane with the reviewer citing that record? | non-blocking | orchestrator | T-023 |
| Q-004 | Without forged signatures, how much product data can an unmodified read obtain from a script-rendered real page, and is paste mode then the expected common path? | non-blocking | architect | T-001, T-003 |
