```yaml
design:
  designId: TD-MC18-READ-LINK
  changeReference: runs/inputs/mc-18-feature-request.md
  sourceInputs:
    - type: feature-request
      reference: runs/inputs/mc-18-feature-request.md
    - type: business-intent
      reference: runs/inputs/mc-18-business-intent.md
    - type: execution-plan
      reference: runs/run-6fa26c9657d3/states/execution-planning/artifacts/execution-plan.md
  producedBy: architect
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  decisionRecords: [D-001, D-002, D-003, D-004, D-005, D-006]
  consumesPlan: runs/run-6fa26c9657d3/states/execution-planning/artifacts/execution-plan.md
  inputDigest: sha256:48ec0e7500d6980dd0ecfd2228f760ae
  contextDigest: sha256:11b68469cf9e593c79e0dc76de8cb64f
```

## Metadata

- Feature or Change ID: MC-18, run run-6fa26c9657d3, invocation inv-6fa26c9657d3-03-001, phase solution-design-and-risk-assessment; this package is the design planner task T-001 asks for, and its Test Strategy table is the criterion-to-fixture map planner task T-002 asks for.
- Author: architect
- Reviewers: tech-lead (accepting Design Gate owner), product-owner (the CEO answers), qa, dev-1-implement, dev-2-reviewer
- Last Updated: 2026-10-27
- Upstream evidence cited by identifier and digest: the scope definition (sha256:0379ea41a66f31f30be2c251b71b0fa3), cited as SC-nn for its acceptance criteria (the plan's numbering), X-nnn for its exclusions and "scope decision n" for its decisions; the execution plan (sha256:e07b36b1b94d4b934905cfe7f1d722bb), cited by T-nnn, "plan assumption n" and "plan risk n"; CEO decisions 1313 and 1314 (research/ceo-decision-record.md); the orchestrator briefing and dispatch of this phase, cited as "orchestrator briefing".
- Answer to the planner's open question on expected yield: given in 5.3 under "Expected yield on real pages"; it rests on A-002 and is confirmed only by the orchestrator's single live read (Q-006).
- Plan assumptions settled: plan assumption 1 carried as A-001; 2 replaced by A-002; 3 carried as A-008 and settled in D-004; 4 carried as A-003; 5 carried unchanged (SC-34 is the only measure, CEO decision 1314 item 4); 6 settled by D-003 (own file, no toolchain change, F-012, F-013); 7 replaced by A-015 because F-017 records no host names; 8 carried as A-004 and settled in D-005 at fixture level; 9 settled by D-012 (files only, no database work); 10 carried as A-011; 11 confirmed by S-024; 12 carried unchanged (verification owned by dev-2-reviewer, orchestrator briefing); 13 carried as A-012; 14 carried as A-005.
- Repository rescan: the task context names no modules or files, so src/MediaCompany.Pilot.Web, tests/MediaCompany.Pilot.Web.Tests, src/MediaCompany.Pilot/FactSheet.cs, Directory.Build.props and the MC-17 design were read to establish 4.1. Reason: the change builds on the MC-17 web app and the context named none of its files.
- Skills applied: S01, S03, S06, S09, S02, S08, S12. Database area: lexical only; no database is touched (D-012). Performance area: real for the per-request and per-click bounds (C-026, D-014). Security area: real (Operational Considerations; R-005 to R-012).
- No implementation work was performed, no external system was accessed and no live page was read.

## Objective

- Desired outcome: at step 1 the operator pastes one link, presses one button, and gets a fact sheet form filled from that product's public shopee.vn page with its own images waiting to be picked; when Shopee refuses, the read stops and paste mode fills the same fields offline; the MC-16 check stays the only authority (S-001 to S-004, S-011).
- Architectural objectives:
  - One new egress boundary inside the web app process, opened only by an operator's read click through the MC-17 request guard, reaching only allowed hosts, closed when the read ends (S-006, S-009, S-021).
  - A host-and-hop admission rule checked before every connection, independent of where the test fixture is (S-007, S-009, S-023).
  - A request identity with no cookie store, no proxy, no automatic redirect and a fixed header list that names the tool honestly (S-007, S-008, S-026).
  - One text parser shared by the read path and the paste path, so both fill the same fields by construction (S-004, S-005).
  - A per-video fill record outside the fact sheet carrying source marks and confirmations, so the unchanged check still decides (S-003, S-011, S-025).
  - One persisted switch outside the toolchain's files that removes the egress boundary while paste mode keeps working (S-012, S-020).
  - A test transport seam reachable only from test code (S-015).
- In scope: new read, parse, image, fill-record and switch components inside src/MediaCompany.Pilot.Web; new and changed step-1 routes and page parts; a settings view; the pilot-folder additions; new tests and the fixture server in tests/MediaCompany.Pilot.Web.Tests.
- Out of scope:
  - Any change to src/MediaCompany.Pilot or tests/MediaCompany.Pilot.Tests, its fact-sheet check, banned-term list, price rule or settings form (S-011, X-006).
  - Steps 2 to 7 beyond receiving the picked images (X-010).
  - Reading any other page, batch or background reads, sign-in, cookies, affiliate.shopee.vn, CAPTCHA solving, signed product endpoints, proxies (S-006, S-007, S-008, X-001 to X-003).
  - A screenshot taken by the tool (X-005); monitoring Shopee's answer or switching off automatically (S-020, X-007).
  - The mediacompany database, MC-13 and MC-14 material, publishing (S-014, X-009); merging any branch (S-017, X-012).
  - The single live read, which is the orchestrator's closing act (S-024).

## Requirements Summary

- Functional requirements:
  - One click fills name, classification, page address, read date and specification rows; price and description shown, not saved (S-001, S-002, S-025; C-001, C-003).
  - Short links followed only within allowed hosts, at most five hops (S-023; C-002).
  - Own images saved, picked by the operator, only the three picked enter the input set; disallowed images named for upload (S-002, S-010, S-022; C-004).
  - Source mark per filled field and confirmation before save; unsupplied fields left to the operator (S-003, S-025; C-005).
  - Unchanged check and live warning over filled values (S-011; C-006, C-025).
  - Stop on the nine signals with no retry, Vietnamese reason and paste offer (S-004, S-008; C-007, C-028).
  - Paste mode and hand typing fill and save offline (S-004, S-005; C-008).
  - One switch on a settings screen (S-012, S-020; C-016).
- Non-functional requirements:
  - Egress bounds: one product per click, no cookie, no proxy, honest headers, host rule, idle silence, toolchain at zero sockets, listener on 127.0.0.1 only (S-006 to S-009, S-021, S-026; C-009 to C-015, C-030).
  - Offline tests, no media file, no package, seam unreachable from the shipped app (S-013, S-015; C-017, C-022).
  - Bounded time and size per request and per click (S-004, S-027; C-026).
- Acceptance criteria: SC-01 to SC-34 as set by product-owner and approved at the Scope Gate (CEO decision 1314 item 4; S-016); each is mapped to its design element, test level, fixture case and evidence in the Test Strategy Focus Areas table of the Delivery Plan.

## Current-State Assumptions and Constraints

### 4.1 Facts

| ID | Fact | Established by |
|---|---|---|
| F-001 | The web project is an executable at net10.0 with no package reference, references only the toolchain project, and declares no internals visibility | src/MediaCompany.Pilot.Web/MediaCompany.Pilot.Web.csproj, Directory.Build.props |
| F-002 | The web app listens on the IPv4 loopback address only, checks its bound addresses at start and exits otherwise, holds a named single-instance lock, and its serve path is private | src/MediaCompany.Pilot.Web/Program.cs |
| F-003 | The request guard requires a loopback peer and Host equal to 127.0.0.1 and the port, passes GET and HEAD, requires exact Origin and the per-start token header on POST, answers 405 otherwise, and sets a content policy with connect-src 'self' and img-src 'self' blob: | src/MediaCompany.Pilot.Web/RequestGuard.cs |
| F-004 | Every route runs through one answer wrapper that turns refusals into Vietnamese outcomes; JSON bodies are capped at 1 MiB and image uploads at 40 MiB; the route log records method, path, status, code, exit and duration only | src/MediaCompany.Pilot.Web/Routes.cs |
| F-005 | Saving facts normalises to NFC, checks the sheet before images, places waiting screenshots, then writes bang-thong-tin.json into the per-video input folder | src/MediaCompany.Pilot.Web/Routes.cs, FactSteps.cs |
| F-006 | The image route accepts only the images the saved sheet names in its images-used field, with .png, .jpg or .jpeg | src/MediaCompany.Pilot.Web/Routes.cs, FactSteps.cs |
| F-007 | The live check route answers with the toolchain's held banned-term check | src/MediaCompany.Pilot.Web/FactSteps.cs |
| F-008 | The fact sheet holds formVersion, videoCode, productName, subjectNoun, classification, pagePath, shopNameAndMall, readDate, niche, imagesUsed, productVideoNumber, videoFocus, facts and callToAction; a row holds label, subject noun, attribute, value, source section, screenshot and override; the reader refuses unknown members, requires 1 to 3 rows and refuses banned terms in subject, attribute, value and override | src/MediaCompany.Pilot/FactSheet.cs |
| F-009 | Every stored name passes the toolchain's plain-name rule | src/MediaCompany.Pilot.Web/Names.cs |
| F-010 | The pilot folder holds inputs per video code, takes, web (uploads, screenshots per code, a per-video state file at form version 1) and runs | src/MediaCompany.Pilot.Web/Workspace.cs |
| F-011 | Screenshots wait under the web folder per code and are copied into the input folder when the sheet is saved | src/MediaCompany.Pilot.Web/FactSteps.cs |
| F-012 | The app record is form version 1 in the app folder; its reader refuses unmapped members and treats another version as absent | src/MediaCompany.Pilot.Web/AppRecord.cs |
| F-013 | Setup writes cai-dat.json, which the toolchain's own settings reader validates and which refuses unknown members | src/MediaCompany.Pilot.Web/Setup.cs, MC-17 design facts on the settings form |
| F-014 | The setup view is shown only while the app is unconfigured; no settings view exists after configuration | src/MediaCompany.Pilot.Web/Pages/index.html, Pages/app.js |
| F-015 | Step 1 shows a header grid, a call to action, 1 to 3 fact rows, the live result and a save button | src/MediaCompany.Pilot.Web/Pages/index.html |
| F-016 | The shipped executable accepts --port, --app-folder, --desktop-folder, --no-browser, --test-mode and --test-result; test mode adds a test driver script and a result route | src/MediaCompany.Pilot.Web/WebContext.cs, Routes.cs |
| F-017 | The address harness samples the TCP and UDP tables about every 20 ms for tracked processes and records local and remote addresses, ports, state and owner, never host names | tests/MediaCompany.Pilot.Web.Tests/AddressHarness.cs |
| F-018 | The web tests start the web executable as a separate process with --test-mode and their own app folder, and call it with an HTTP client with proxy and automatic redirects off | tests/MediaCompany.Pilot.Web.Tests/Support.cs, BrowserCase.cs |
| F-019 | The headless browser shell is digest-checked and started with no-proxy and background-networking-off flags | tests/MediaCompany.Pilot.Web.Tests/BrowserCase.cs |
| F-020 | The web test project is a console executable with no package that references the web project | tests/MediaCompany.Pilot.Web.Tests/MediaCompany.Pilot.Web.Tests.csproj |
| F-021 | At the start of MC-18 the toolchain has 251 passing tests and the web app 36 | orchestrator briefing |
| F-022 | The toolchain's network guard checks only the assembly closure below the toolchain, and its socket telemetry is created only by produce, which the web app never calls in process | MC-17 technical design (run-022d0492a75a), facts on the network guard and the process-boundary decision |

### 4.2 Assumptions

| ID | Assumption | Why needed | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | The base class library holds what the read needs: HTTP client, URI and IDN parsing, JSON reading, TLS and in-code certificate creation for the fixture (plan assumption 1) | C-017 for every option | A package needs the CEO's approval by name (R-001) | dev-1-implement |
| A-002 | An unmodified request for a real product page most often returns a script shell or a verify or sign-in page; at best its server text holds page title, description, image and a structured product block, never the specification table (replaces plan assumption 2) | Sets the expected yield and the paste-first page design | More fields fill on real pages; no structural change (R-015) | orchestrator, by the single live read |
| A-003 | Text a browser copies from a product page carries Vietnamese section labels usable as anchors (plan assumption 4) | The shared parser locates fields by label | Paste fills fewer fields (R-016) | orchestrator, by the single live read |
| A-004 | A product's own images are those its structured data or page image tag declares, distinct from images of other products on the page (plan assumption 8) | D-005 fetches declared images only | Own images are not found and are uploaded at step 2 (R-017) | orchestrator, by the single live read |
| A-005 | Shopee short links resolve through ordinary redirect answers (plan assumption 14) | D-006 follows hops without page script | Real short links stop and paste mode follows (R-019) | orchestrator, by the single live read |
| A-006 | Shopee product image files are served from hosts ending in .susercontent.com (the example in CEO decision 1314 item 1) | D-005 names the image hosts | Real images are named for upload (R-018) | product-owner (the CEO answers) |
| A-007 | With cookies, proxy, automatic redirects and decompression off, the platform HTTP stack sends only Host and the headers the read sets | C-012 header list | An unlisted header leaves the machine (R-008) | dev-2-reviewer, by the fixture header log |
| A-008 | A transport seam held internal to the web assembly and visible only to the test assembly cannot be set by any argument, environment variable, file or route of the shipped app (plan assumption 3) | C-022 | A read could be pointed at any host (R-007) | dev-2-reviewer |
| A-009 | SC-01's "one fact row per specification" is met when every specification is offered as a candidate row and the operator keeps up to three, because the unchanged check allows 1 to 3 rows (F-008) | D-013 | The check would need a change, against X-006 (R-020) | product-owner (the CEO answers) |
| A-010 | A switch that starts off until the operator turns it on, and reads as off when its file is missing or unreadable, is acceptable | D-003 default | The default is set on; no structural change (R-021) | product-owner (the CEO answers) |
| A-011 | The live banned-term check answers within 1 s for filled values as for typed ones (plan assumption 10) | C-025 | Live check redesign (R-013) | qa |
| A-012 | The installed headless shell can run the step-1 browser tests against the fixture host (plan assumption 13) | P-009 browser evidence | Browser evidence needs a CEO decision or a manual run (R-025) | qa |
| A-013 | The read date the check accepts is the ISO calendar date the MC-16 fact sheets use | D-008 fills the read date | Saving refuses the filled read date (R-022) | dev-1-implement |
| A-014 | A sign-in wall and a CAPTCHA page are recognisable by held path and page markers | D-010 stop recognition | Such a page stops as "no readable product", with a less precise reason (R-023) | orchestrator, by the single live read |
| A-015 | SC-25 is evidenced by the read log's host names and times paired with the harness's remote addresses and sample times | F-017 records no host names | SC-25 needs another evidence route (R-024) | qa |
| A-016 | The test program can start the web app composed with the transport seam in a process it owns, through an internal start path, because it already references the web project (F-020) | D-004 for app-level and browser tests | The seam reaches component tests only (R-002) | dev-1-implement |
| A-017 | The SC-34 Browser-pane run may use the test program's fixture host, since the shipped app cannot reach the fixture | D-004 and SC-34 | SC-34 needs another route (R-026) | qa |
| A-018 | A code-generated certificate can be trusted for the fixture only through the seam's own certificate check, leaving the shipped trust path unchanged | D-004 | The fixture needs plain HTTP, which C-022 forbids (R-002) | dev-1-implement |

### 4.3 Constraints

| ID | Class | Constraint | Hard or negotiable | Source |
|---|---|---|---|---|
| C-001 | functional | One click fills product name, classification, page address, read date and specification rows equal to the page after NFC | hard | SC-01 (S-001, S-002) |
| C-002 | functional | A full link and a short link to the same product fill identical values; more than five hops stops | hard | SC-02, CEO decision 1314 item 2 (S-023) |
| C-003 | functional | Read price and description are shown marked as read and never placed in a saved sheet field by the tool | hard | SC-03, scope decision 4 (S-025) |
| C-004 | functional | Only the product's own images are fetched, saved byte-equal and picked; only the three picked enter the input set; an image on a disallowed host is named for upload | hard | SC-04 to SC-06, scope decision 5 (S-002, S-010, S-022) |
| C-005 | functional | Every filled field shows a Vietnamese source mark; saving is refused until each is confirmed; unsupplied fields stay empty for the operator | hard | SC-07 to SC-09, scope decisions 3 and 8 (S-003, S-025) |
| C-006 | functional | Filled values meet the unchanged check with the same result as typed values | hard | SC-10, SC-11 (S-011) |
| C-007 | functional | Each of the nine stop signals ends the read with zero further requests, no retry and paste mode offered | hard | SC-12, SC-13, scope decision 6 (S-004, S-008, S-025) |
| C-008 | functional | Paste fills the same fields as the read with no connection; hand typing still works and the 36 web tests pass | hard | SC-15 to SC-17, scope decision 2 (S-004, S-005) |
| C-009 | security | One click requests only the link, its hops, the one page and its own images; nothing reads while idle; two links are refused | hard | SC-18, SC-19 (S-006) |
| C-010 | security | No request carries a Cookie or Authorization header; no cookie is kept | hard | SC-20 (S-007) |
| C-011 | security | affiliate.shopee.vn and its subdomains are never connected | hard | SC-21 (S-007, S-023) |
| C-012 | security | Every header is on the list accepted at the Design Gate; nothing imitates a browser or carries a signature | hard | SC-22, CEO decision 1313 item 2 (S-008, S-026) |
| C-013 | security | No proxy, one local address per read, no address pool | hard | SC-23 (S-008) |
| C-014 | security | The six link kinds of SC-24 open zero connections | hard | SC-24 (S-009, S-023) |
| C-015 | security | Egress goes only to allowed hosts between a click and its result; toolchain processes own zero sockets; one listener on 127.0.0.1 | hard | SC-25 to SC-27 (S-009, S-021) |
| C-016 | operability | One setting on a settings screen turns the read off and holds across restarts; off means no button, a refused route, zero egress, and paste and typing still save | hard | SC-28 to SC-30, scope decision 7 (S-012, S-020) |
| C-017 | operability | Tests run offline against a code-generated fixture covering the SC-31 cases; zero media files and zero package references | hard | SC-31, SC-32 (S-013, S-015) |
| C-018 | compliance | No agent reads a live shopee.vn page; the one live read is the orchestrator's | hard | SC-33, CEO decision 1314 item 3 (S-015, S-024) |
| C-019 | functional | Step 1 is shown working in the Browser pane against the fixture, read and blocked-to-paste | hard | SC-34 (S-016) |
| C-020 | structural | The MC-16 toolchain is unchanged; steps 2 to 7 change only to receive the picked images | hard | SC-11, X-006, X-010 (S-005, S-011) |
| C-021 | structural | The mediacompany database, MC-13 and MC-14 material are untouched; nothing publishes | hard | X-009 (S-014) |
| C-022 | security | The seam that points reads at the fixture is unreachable from the shipped app | hard | plan risk 17 (S-015) |
| C-023 | security | A read is started only by a guarded request from the app's own page | hard | plan risk 19, F-003 (S-005, S-006) |
| C-024 | security | A fetched image name never becomes a path; only PNG or JPEG content is kept; size is bounded | hard | plan risk 20, F-006 (S-010) |
| C-025 | quality-attribute | The live warning over filled values answers within 1 s | negotiable | SC-10 (S-011) |
| C-026 | quality-attribute | Each request and each click have a time and size bound, with 30 s per request | negotiable | SC-12 (S-004, S-027) |
| C-027 | operability | This phase writes only its staging artifacts | hard | framework defects finding 12 (S-018) |
| C-028 | functional | Stop messages and new labels are Vietnamese with reason and next step | hard | SC-14 (S-004) |
| C-029 | operability | The change lands on the MC-18 session branch and merges nowhere | hard | X-012 (S-017) |
| C-030 | quality-attribute | The request names the tool honestly, so the site can tell an automated reader from a person | negotiable | SC-22 wording "the reading component's own value" (S-008, S-019) |

## Architecture and Component Design

### 5.1 Impacted Modules

| ID | Module | Impact type | Basis | Interfaces affected | Confidence |
|---|---|---|---|---|---|
| M-001 | Link admission and host rule (new, in MediaCompany.Pilot.Web) | extension | F-001, A-005 | Called by M-007 with the pasted link and by M-002 before every hop | confirmed |
| M-002 | Read sender (new, in MediaCompany.Pilot.Web): per-click HTTP stack, header list, bounds, stop recognition | dependency-change | F-001, F-022, A-001, A-007 | New outbound HTTPS to allowed hosts; transport seam (D-004) | confirmed |
| M-003 | Page reader and shared text parser (new, in MediaCompany.Pilot.Web) | extension | F-008, A-002, A-003, A-013, A-014 | Read result handed to M-005 and M-007 | speculative |
| M-004 | Read image saver (new, in MediaCompany.Pilot.Web) | extension | F-006, F-009, F-011, A-004, A-006 | Writes the per-code read-image waiting folder | speculative |
| M-005 | Fill record and confirmations (new, in MediaCompany.Pilot.Web) | extension | F-008, F-010 | Per-video fill record read by M-007 | confirmed |
| M-006 | Read switch store (new, in MediaCompany.Pilot.Web) | extension | F-012, F-013, F-014, A-010 | Switch file in the app folder | confirmed |
| M-007 | MediaCompany.Pilot.Web routes (Routes.cs, FactSteps.cs) | contract-change | F-004, F-005, F-011 | New read, paste, confirm, read-image and switch routes; facts save gains a refusal and image placement; video status gains the fill record | confirmed |
| M-008 | MediaCompany.Pilot.Web page set (index.html, app.js, styles) | extension | F-014, F-015 | Link field, read button, paste area, marks, confirmations, price and description panel, image picker, settings view | confirmed |
| M-009 | MediaCompany.Pilot.Web request guard and content policy | no-change-verified | F-003 | New POST routes pass the existing guard; read images are served from the app's origin | confirmed |
| M-010 | MediaCompany.Pilot.Web start-up (Program.cs, WebContext.cs) | extension | F-002, F-016, A-016 | Internal start path taking the transport seam; listener and command-line switches unchanged | speculative |
| M-011 | MediaCompany.Pilot.Web project file | extension | F-001, F-020 | Internals visible to the test assembly only; no package | confirmed |
| M-012 | App record form (AppRecord.cs) | no-change-verified | F-012 | None; the switch lives in its own file | confirmed |
| M-013 | Pilot settings file cai-dat.json | no-change-verified | F-013 | None | confirmed |
| M-014 | MC-16 toolchain src/MediaCompany.Pilot (fact-sheet check, banned-term check, network guard) | no-change-verified | F-008, F-022 | Consumed unchanged | confirmed |
| M-015 | MC-16 toolchain tests tests/MediaCompany.Pilot.Tests | no-change-verified | F-021 | None; 251 must pass | confirmed |
| M-016 | Pilot folder layout outside the repository | extension | F-010, F-011 | Adds a read-image waiting folder per code, a fill record per video and a read log in the web folder | confirmed |
| M-017 | MediaCompany.Pilot.Web.Tests (fixture server, seam use, fixture host mode, new cases) | extension | F-017 to F-020, A-012, A-016, A-018 | Starts the fixture and the seam-composed app on loopback | speculative |
| M-018 | Address harness (AddressHarness.cs) | no-change-verified | F-017 | Used unchanged; paired with the read log (A-015) | confirmed |
| M-019 | Steps 2 to 7 routes and pages | no-change-verified | F-006 | Step 2 finds the picked images already in the input folder | confirmed |
| M-020 | mediacompany database, MediaCompany.Persistence, MC-13 and MC-14 material | no-change-verified | F-010 | None | confirmed |

Boundary crossings in the selected approach: page to M-007 over loopback (guarded, M-009); M-007 to M-001, M-002, M-003, M-004, M-005 in process; M-002 to Shopee hosts over HTTPS (the one new external crossing); M-004 and M-005 to M-016 (file writes inside the pilot folder); M-007 to M-014 in process for the unchanged checks; M-017 to M-010 through the internal start path (test only).

### 5.2 Options Considered

Six decision families, each evaluated on its own. Columns follow the A8 order; impact surface counts modules with contract-change or dependency-change; reuse leverage counts the section 7 capabilities of the family met by reuse-as-is or reuse-extended (for D-001, the thirteen product-code rows; the five test-side rows are equal across its options and are not counted); quality attributes count the family's negotiable constraints met. Outcome "eliminated" marks an option that violates the named hard constraint; "not selected" marks an option that meets every hard constraint and loses at the named later criterion.

| Option | Family | Structural change | Hard constraints | Impact surface | Reuse leverage | Quality attributes | Migration burden | Operability | Outcome |
|---|---|---|---|---|---|---|---|---|---|
| O-001 | D-001 where the read runs | Read components inside the web process behind the MC-17 guard, a fresh HTTP stack per click | all met | 2 (M-002, M-007) | 13 | C-025, C-026: 2 of 2 | 1 | high: one process | selected |
| O-002 | D-001 where the read runs | The page's own script fetches Shopee from the operator's browser | C-010 violated: the browser sends the operator's own Shopee cookies; C-012 violated: browser headers | 1 | 6 | 1 of 2 | 1 | medium: content policy opened | eliminated (C-010) |
| O-003 | D-001 where the read runs | A new read verb in the MC-16 toolchain | C-020 and C-015 violated: toolchain changed and given sockets | 3 | 8 | 2 of 2 | 2 | medium | eliminated (C-020) |
| O-004 | D-001 where the read runs | Drive the installed headless browser to render the page | C-012 violated: an automated browser presents as a person's browser; C-004 violated: scripts, styles and fonts fetched | 2 | 7 | 1 of 2 | 1 | low | eliminated (C-012) |
| O-005 | D-001 where the read runs | A separate read helper executable started per click, talking to the web app over its standard streams | all met | 3 (helper egress, web-to-helper link, M-007) | 11 | 2 of 2 | 1 | medium: a second executable to build, locate and check | not selected: lost at impact surface |
| O-006 | D-002 request identity | Honest tool user agent plus a fixed minimal header list | all met | 0 | 1 | C-030: 1 of 1 | 0 | high | selected |
| O-007 | D-002 request identity | No user agent header (the HTTP client default) plus the same list | all met | 0 | 1 | C-030: 0 of 1 | 0 | high | not selected: lost at quality attributes |
| O-008 | D-002 request identity | A browser's user agent and browser-like headers | C-012 violated | 0 | 1 | 0 of 1 | 0 | high | eliminated (C-012) |
| O-009 | D-003 off switch | Own switch file in the app folder, edited on a new settings view; missing or unreadable reads as off | all met | 0 | 2 | none recorded | 0 | high | selected |
| O-010 | D-003 off switch | A new member of the app record at form version 2 | all met | 1 (M-012) | 2 | none recorded | 1 | low: an old record reads as absent and forces setup again (F-012) | not selected: lost at impact surface |
| O-011 | D-003 off switch | A new member of cai-dat.json | C-020 violated: the toolchain reader refuses unknown members (F-013) | 1 | 1 | none recorded | 1 | low | eliminated (C-020) |
| O-012 | D-004 test seam | Internal transport seam (where to connect, which fixture certificate to trust) passed only by test code through an internal start path | all met | 0 | 3 | none recorded | 0 | high | selected (only viable) |
| O-013 | D-004 test seam | A command-line switch or environment variable on the shipped executable pointing reads at the fixture | C-022 violated: reachable like --test-mode (F-016) | 0 | 3 | none recorded | 0 | high | eliminated (C-022) |
| O-014 | D-004 test seam | A plain-HTTP fixture with the host rule relaxed under a test flag | C-022 and C-014 violated: shipped code holds a weaker rule | 0 | 2 | none recorded | 0 | medium | eliminated (C-022) |
| O-015 | D-005 image hosts | Image files from shopee.vn subdomains and hosts ending .susercontent.com, only the addresses the product's structured data declares | all met | 0 | 2 | none recorded | 0 | high: images arrive with the read | selected |
| O-016 | D-005 image hosts | Any host the page names for an image | C-004 and C-015 violated: other hosts and other products' images | 0 | 2 | none recorded | 0 | high | eliminated (C-015) |
| O-017 | D-005 image hosts | shopee.vn subdomains only | all met | 0 | 2 | none recorded | 0 | low: real images named for upload at step 2 (A-006) | not selected: lost at operability |
| O-018 | D-006 host and hop rule | Automatic redirects off; the rule checked on the link and on every hop before connecting, at most five hops; a connect-time check refuses non-public addresses | all met | 0 | 2 | none recorded | 0 | high | selected (only viable) |
| O-019 | D-006 host and hop rule | The stack follows redirects itself and the final address is checked | C-011 and C-014 violated: a connection opens before the hop is checked | 0 | 2 | none recorded | 0 | high | eliminated (C-014) |

Recomputed ranking, criteria in A8 order: D-001 O-001 over O-005 (impact surface 2 to 3); D-002 O-006 over O-007 (quality attributes 1 to 0); D-003 O-009 over O-010 (impact surface 0 to 1); D-004 O-012 (only option meeting C-022); D-005 O-015 over O-017 (tied through migration burden, O-015 wins operability); D-006 O-018 (only option meeting C-014).

### 5.3 Selected Approach

- Selected: O-001 (where the read runs, D-001), the structural choice the other five families sit inside.
- Per-decision selections: D-002 O-006, D-003 O-009, D-004 O-012, D-005 O-015, D-006 O-018.
- Structural change: five new components inside the web app (M-001 to M-006), reached only through new guarded routes in M-007 and new step-1 page parts in M-008. A read click passes M-006 (switch on), M-001 (one link, allowed host), M-002 (one fresh HTTP stack, hop by hop, stop at the first signal), M-003 (structured data and visible text through the shared parser), M-004 (declared own images saved under generated names), then M-005 (fill record). Paste goes straight to M-003's shared parser and M-005, opening no connection. Saving facts keeps the MC-17 path and the unchanged check, adding one refusal for an unconfirmed filled field and placing picked read images. No toolchain file changes.
- Expected yield on real pages (answer to the planner's open question on yield; resting on A-002, unconfirmed until Q-006):
  - The read sends one honest request and calls no signed product endpoint, runs no page script, forges no header.
  - Most likely outcome: a script shell with no product data, or a verify or sign-in page; both end the read with "no readable product" or the matching stop, and paste mode is offered.
  - Best case: the server text carries a page title, description and image tag and a structured product block; the read then fills product name, page address and read date, shows price and description, and fetches the declared own images.
  - Classification and specification rows are rarely present in server text; they come from paste.
  - Paste mode is therefore expected to be the common path, and the read a best-effort shortcut; the page design leads with the link field and keeps the paste area one click away.
- Rationale: O-001 meets every hard constraint with the smallest impact surface, keeps every read behind the MC-17 guard and answer wrapper, and reuses the workspace, plain-name rule, waiting-folder pattern and live check. O-006 is honest and satisfies SC-22's "own value". O-009 needs no form-version change. O-012 is the only seam C-022 allows. O-015 matches CEO decision 1314 item 1. O-018 is the only rule that checks a hop before connecting.
- Highest-scoring rejected alternative and why it lost: O-005 ties O-001 on hard constraints and loses at impact surface (3 against 2), because the helper adds its own egress module and a link to the web app; it would also cost operability (a second executable to build, locate and digest-check at setup).
- Tradeoffs accepted:
  - Outbound HTTPS code runs inside the process that owns the loopback listener; the toolchain's guard is unaffected (F-022) and the listener is unchanged (M-010).
  - The honest user agent lets Shopee recognise and refuse the tool; a refusal is a stop, and paste mode follows (D-002).
  - Specification rows beyond three are offered as candidates, not saved, because the unchanged check allows 1 to 3 rows (A-009, D-013).
  - App-level and Browser-pane runs against the fixture use the test program's fixture host, not the shipped app (A-017).

### 5.4 Decisions

| ID | Decision | Architecture-significant | Record |
|---|---|---|---|
| D-001 | The read runs inside the web app process as new components behind the MC-17 guard, one read at a time app-wide, with a fresh HTTP stack per click disposed when the read ends (O-001) | yes | architecture-decision-record-D-001.md |
| D-002 | Request identity in M-002: user agent "MediaCompanyPilot/1.0 (doc-trang-san-pham)"; Accept "text/html" for the page and "image/jpeg, image/png" for images; Host set by the stack; nothing else; no Cookie, Authorization, Referer, Origin, client hints, fetch-metadata, X- or signature header; cookies, proxy, automatic redirects and decompression off; meets C-010, C-012, C-013, C-030 on the baseline of F-001, F-018, F-022 (O-006) | yes | architecture-decision-record-D-002.md |
| D-003 | Off switch: one on/off value in its own file in the app folder at form version 1, set on a new settings view reached from the top bar after setup; missing, unreadable or another version reads as off; default off until the operator turns it on (O-009) | yes | architecture-decision-record-D-003.md |
| D-004 | Test seam: an internal transport seam (connect-target resolver and fixture certificate check) given to the read sender only through an internal start path visible to the test assembly; the shipped entry point never passes it and no argument, variable, file or route can set it; the test program offers a fixture host mode for app, browser and Browser-pane runs (O-012) | yes | architecture-decision-record-D-004.md |
| D-005 | Image hosts: image files only, from shopee.vn subdomains and hosts ending .susercontent.com, only the addresses the product's structured product block or page image tag declares, never a page from an image host, no redirect followed for an image (O-015) | yes | architecture-decision-record-D-005.md |
| D-006 | Host and hop rule: https only (a link without scheme or with http is rewritten to https before any connection; an http hop stops); no user info, no explicit port, no IP literal; host compared in lower-case ASCII form without a trailing dot; page hosts are shopee.vn and names ending .shopee.vn except affiliate.shopee.vn and names under it; checked on the link and every hop before connecting; at most five hops; a hop to a sign-in path stops; the shipped connect step refuses loopback, private, link-local, unspecified and multicast addresses (O-018) | yes | architecture-decision-record-D-006.md |
| D-007 | Fill record per video in the web folder at form version 1: source (read or paste), admitted link, final page address, read time, each filled field with its value and confirmed flag, candidate rows, price and description text, saved image names with their source address, and image addresses not fetched with the reason; a guarded confirm route sets one flag; saving facts with a fill record refuses, naming the first filled field whose value still equals the filled value and is unconfirmed; an edited value counts as typed; with no fill record saving is unchanged (O-001) | no | inline |
| D-008 | Field filling: product name and page address from the structured block, page title or labelled text; page address is the final admitted address without query or fragment; read date is the day of the read (A-013); classification and specifications from labelled text; source section is the Vietnamese label found; subject noun, screenshot and override of each row stay empty; values NFC-normalised (O-001) | no | inline |
| D-009 | Read images are named by the app (anh-doc-01 onward, extension from the content's own signature), wait in a per-code read-image folder, are served to the picker by a guarded read route, and the ones the sheet names in images-used are copied into the input folder when the sheet is saved, as screenshots are (F-011) (O-001) | no | inline |
| D-010 | Stop recognition: the nine SC-12 signals (403; 429; a hop to a sign-in path; a sign-in wall page; a CAPTCHA page; no readable product; 5xx; no connection, including name and TLS failures; no answer within 30 s) plus host-rule stops, more than five hops, any other status outside 200 and 3xx, a non-HTML page and an over-size answer; each ends the read with no further request, a Vietnamese reason and next step, a code in square brackets and the paste offer; an image-phase stop keeps the filled facts and names the remaining images for upload (O-001) | no | inline |
| D-011 | A read log in the web folder: one line per outbound request with time, host, path without query, status, byte count and stop code; never headers, cookies or page text; the MC-17 route log is unchanged (O-001) | no | inline |
| D-012 | Files only: no database, no new store; the mediacompany database is untouched (O-001) | no | inline |
| D-013 | Every specification found is offered as a candidate row; the operator keeps up to three, as the unchanged check requires (A-009) (O-001) | no | inline |
| D-014 | Bounds: 30 s per request; 120 s per click; page answer up to 5 MiB; up to 20 own images per read, each up to 40 MiB as at step 2; structured-data reading with a depth limit; a second click while a read runs is refused in Vietnamese (O-001) | no | inline |
| D-015 | Link admission refuses a field holding more than one link, naming the one-product rule; the page never starts a read on paste or on typing, only on the button (O-001) | no | inline |

## API and Data Model Impact

- API changes (M-007; all on 127.0.0.1, all POST routes under the MC-17 guard of exact Origin and per-start token):
  - New POST routes: read from link (refused in Vietnamese when the switch is off), parse pasted text (works with the switch off, opens no connection), confirm a filled field, set the read switch.
  - New GET routes: read switch state; one waiting read image for the picker.
  - Changed POST route, facts save: same request body; adds a refusal naming an unconfirmed filled field when a fill record exists, and places the picked read images into the input folder.
  - Changed GET route, video status: adds the fill record summary as a new member.
- Contract compatibility notes and transition strategy for M-007:
  - Current shape: the MC-17 route set of F-004 to F-007.
  - Target shape: the same routes and bodies, plus the routes above, one added refusal code and one added response member.
  - Compatibility approach: additive; with no fill record the facts save behaves exactly as today, so the 36 web tests stay valid (C-008); the page set M-008 ships in the same build as the routes, so no older consumer exists.
  - Coexistence period: none beyond the build; the routes and the page are served by one process.
  - Retirement condition: not applicable; nothing is retired.
  - Rollback position: removing the new components, routes and page parts returns the MC-17 behaviour; fill records, read images and the switch file left in the pilot and app folders are ignored by the MC-17 build.
- Schema or migration changes:
  - None in any database (D-012).
  - New files only, with no prior data: migration direction not applicable, fully reversible, each carrying a form version so a later reader refuses a form it does not know.
- Data model of the new files (description, not a schema):
  - Read switch file in the app folder: form version and one on/off value (D-003).
  - Fill record per video in the web folder: as D-007.
  - Read-image waiting folder per code: app-named PNG or JPEG files (D-009).
  - Read log in the web folder: as D-011.
  - The fact sheet form and the app record form are unchanged (M-012, M-014).

## Reusable Components and Reuse Rationale

| Capability | Candidate | Outcome | Rationale |
|---|---|---|---|
| Guarded local routes | MC-17 request guard (F-003) | reuse-as-is | New POST routes inherit Origin and token checks (C-023) |
| Vietnamese refusal outcomes | MC-17 answer wrapper (F-004) | reuse-as-is | Stops and refusals become outcomes the page already shows |
| HTTP requests | Platform HTTP client (A-001) | reuse-as-is | Named in the feature request; cookies, proxy, redirects and decompression switched off |
| URL and international host parsing | Platform URI parser | reuse-as-is | Lower-case ASCII host form for the rule (D-006) |
| Host and hop rule | none | none-found | Searched existing components, the standard library, framework capability and installed dependencies; no rule for these hosts exists |
| Structured product data | Platform JSON reader | reuse-as-is | Reads the structured block with a depth limit (D-014) |
| HTML to visible text and label anchors | none | none-found | Searched existing components, the standard library, framework capability and installed dependencies; no HTML parser in the base library, and a package is excluded (C-017) |
| Stop recognition markers | none | none-found | Searched the same four kinds; held as data in the web app (D-010) |
| Plain names for stored files | Toolchain plain-name rule via PlainName (F-009) | reuse-as-is | Every generated image name passes it (C-024) |
| Waiting images placed on save | Screenshot waiting pattern (F-011) | reuse-extended | Same placement on facts save for read images (D-009) |
| Pilot folder paths | Workspace (F-010) | reuse-extended | Gains the read-image folder, fill record and read log paths |
| Image type check | MC-17 image extensions (F-006) | reuse-extended | Extended with a content-signature check before naming |
| Fact sheet check | Toolchain fact-sheet check through facts save (F-005, F-008) | reuse-as-is | The unchanged authority (C-006) |
| Live banned-term warning | MC-17 live check route (F-007) | reuse-as-is | Runs on filled values as on typed ones (C-025) |
| NFC normalisation | MC-17 facts save normalisation (F-005) | reuse-as-is | Same normalisation for read and paste values |
| Switch storage | App record (F-012) | rejected | A new member needs form version 2 and an old record would force setup again |
| Switch storage | cai-dat.json (F-013) | rejected | The toolchain reader refuses unknown members (C-020) |
| Switch storage | Platform file API in the app folder | reuse-as-is | Own small file (D-003) |
| Settings view | MC-17 setup view (F-014) | rejected | Shown only while unconfigured; a new view is added in M-008 |
| Read log | MC-17 route log (F-004) | rejected | Its content rule excludes operator-supplied text; the read log holds hosts and paths |
| Egress evidence by address | Address harness (F-017) | reuse-as-is | Paired with the read log (A-015) |
| Fixture HTTPS server | Platform HTTP listener with TLS and in-code certificate (A-001, A-018) | reuse-as-is | Code-generated, loopback only (C-017) |
| Fixture images | Platform byte writing of minimal PNG and JPEG content | reuse-as-is | Generated in code; no media file (C-017) |
| Browser tests | Installed headless shell (F-019) | reuse-as-is | Already digest-checked by MC-17 tests |
| Test host | Web test console program (F-020) | reuse-extended | Gains the fixture server and fixture host mode |

## Operational Considerations

- Logging and observability updates: the read log (D-011, M-016) gives one line per outbound request; the MC-17 route log records the read, paste and confirm routes as for every route (F-004); SC-25 evidence pairs the read log with the address harness (A-015, M-018).
- Error handling strategy (S12):
  - Every stop is an expected outcome, not a fault: a Vietnamese reason, next step, code and paste offer, HTTP 200 to the page with ok false (D-010, C-007, C-028).
  - No retry anywhere, including after a timeout or a 5xx (C-007).
  - An image-phase stop keeps the filled facts and names the remaining images (D-010).
  - A file write fault takes the MC-17 file-error outcome (F-004).
  - A fill record that cannot be read is treated as absent for display and blocks nothing the MC-17 save would allow; a switch file that cannot be read means off (D-003).
- Security considerations (S09):
  - Egress only on a guarded click (C-023, M-009), only to hosts the rule admits before each connection (D-006), never to affiliate.shopee.vn (C-011), with a connect-time refusal of non-public addresses so a Shopee name resolving to this computer or the local network is a stop (R-006).
  - No cookie store, no Authorization, no proxy, no automatic redirect; an honest fixed header list (D-002, C-010, C-012, C-013).
  - Fetched content is untrusted: page text is shown in the page as text, never as markup (R-011); images are named by the app, kept only with a PNG or JPEG signature, bounded in size and count (C-024, D-014).
  - The content policy is unchanged: the page never contacts Shopee; read images are served from the app's origin (M-009).
  - The test seam is internal and never set by the shipped entry point (C-022, D-004).
  - Compliance: the CEO carries the terms risk (S-019); the switch is the stop condition (S-020); the single live read is the orchestrator's (C-018).
- Performance considerations (S08): bounded by C-026 through D-014; the live check over filled values is bounded by C-025 (A-011); one read at a time app-wide.
- Deployment or operability impact: the same single web executable and setup; the switch starts off until the operator turns it on in the settings view (D-003, A-010); no database, container or service (D-012). The S06 database area changes nothing.

## Delivery Plan

### Sequencing Constraints

| ID | Constraint | Modules | Prerequisites | Reason | Binds |
|---|---|---|---|---|---|
| P-001 | The Design Gate decision on D-001 to D-006 and the read-result shape (fields, source marks, stop codes) exist before any producer or consumer | M-001, M-003, M-005, M-007 | none | Contract definition precedes every consumer | T-001, T-002 |
| P-002 | The host and hop rule, the connect-time address check and the internal transport seam exist and pass against the fixture before any request is sent | M-001, M-002, M-010, M-011, M-017 | P-001 | Verification of a boundary precedes work that assumes it | T-015, T-017, T-021 |
| P-003 | The read sender with the accepted header list, no cookie store, no proxy, no automatic redirect, bounds and stop recognition exists before the page reader consumes answers | M-002 | P-002 | The reader relies on the sender's guarantees | T-012, T-015 |
| P-004 | The shared text parser exists before both the page reader and the paste path use it | M-003 | P-001 | SC-15 equality holds only if both paths share one parser | T-003, T-014 |
| P-005 | The image saver with generated names, signature check, size and count bounds and the waiting folder exists before any image is fetched | M-004, M-016 | P-003 | Containment precedes every write | T-006 |
| P-006 | The fill record, the confirm route and the facts-save refusal exist before the page shows marks and confirmations | M-005, M-007, M-008 | P-001, P-004 | Contract definition precedes every consumer | T-004, T-007, T-008, T-009 |
| P-007 | The read switch store and settings view exist before the read route is offered | M-006, M-007, M-008 | P-001 | The stop condition is the safeguard that precedes egress | T-004, T-019 |
| P-008 | The guarded read route composes the admitted link, sender, reader, image saver and fill record; the step-1 page gains the link field, read button, paste area and picker | M-007, M-008, M-009 | P-003, P-004, P-005, P-006, P-007 | Every component the route composes must exist | T-004 |
| P-009 | Fixture cases for SC-01 to SC-34, the address harness with the read log, the 251 toolchain and 36 web tests, and the browser runs are complete | M-014, M-015, M-017, M-018 | P-008 | Release evidence assumes every boundary holds | T-005, T-010, T-011, T-013, T-016, T-018, T-020, T-022, T-023 |
| P-010 | The release note and compare link follow the evidence and state the toolchain unchanged | M-014, M-017 | P-009 | Release evidence precedes release | T-024, T-025 |

### Test Strategy Focus Areas

Levels: component (read components in the test process against the fixture through the seam), app (fixture host started by the test program, called over loopback), browser (installed headless shell on the fixture host), harness (address harness with the read log), inspection (reviewer reads code or files), record (orchestrator's Browser-pane record).

| Criterion | Design element | Level | Fixture case | Evidence |
|---|---|---|---|---|
| SC-01 | D-008, M-003 | app | product page with structured block and labelled specifications | field-by-field comparison after NFC |
| SC-02 | D-006 | app | short link with two allowed hops; chain of six hops | identical fields; six-hop chain stopped |
| SC-03 | D-007, C-003 | app | product page with price and description | saved sheet holds neither |
| SC-04 | D-005, D-009 | app | five own images declared, three other-product images on the page | five digests equal; request log without the three |
| SC-05 | D-009 | browser | five-image product | input folder holds the three picked only; step 2 reports none missing |
| SC-06 | D-005 | app | one declared image on a disallowed host | zero requests to it; name shown; step 2 upload accepted |
| SC-07 | D-007, M-008 | browser | read fixture and paste fixture | N of N marks; typed fields unmarked |
| SC-08 | D-007 | app | read fixture | refusal at N minus 1 confirmations; accepted at N |
| SC-09 | D-008 | app | read fixture | save refused naming a field of SC-09's list; accepted after filling |
| SC-10 | M-007, F-007 | app | eight pages, classes (a) to (g) and a money amount | read and typed results equal 8 of 8; live answer timed |
| SC-11 | M-014, M-015 | inspection | none | toolchain folder diff empty; 251 passing |
| SC-12 | D-010 | app | 403; 429; sign-in redirect; sign-in wall; CAPTCHA page; no product; 5xx; closed port; silent server | 9 of 9 stopped with paste offered; silent case stopped by 30 s |
| SC-13 | D-010, D-001 | app | the nine SC-12 cases | request log shows nothing after each stop and no form submission |
| SC-14 | D-010, M-008 | inspection | the nine SC-12 cases | messages and labels read against the rule |
| SC-15 | M-003, P-004 | component | visible text of the read fixture | paste equals read on the listed fields |
| SC-16 | M-003, D-001 | harness | paste fixture | outbound count zero |
| SC-17 | M-007 | app | none | 36 web tests pass; additions only |
| SC-18 | D-005, D-015 | app | page carrying other product, shop, category and search links | request log holds only the listed set |
| SC-19 | D-015 | harness | idle app with a link entered; two-link field | zero outbound over 10 minutes; refusal |
| SC-20 | D-002 | app | fixture that sets a cookie in its answer | header log without Cookie or Authorization |
| SC-21 | D-006 | app | affiliate link; hop to affiliate host | zero connections to that name |
| SC-22 | D-002 | app and inspection | every read fixture | header log equals the accepted list; header code read |
| SC-23 | D-002, D-001 | app and inspection | proxy set in the test host environment at a fixture listener | zero proxy connections; one source address per read |
| SC-24 | D-006 | component and app | non-Shopee host; xshopee.vn; shopee.vn.example.test; IP address; another country's Shopee; hop to each | 6 of 6 refused with zero connections |
| SC-25 | D-011, M-018 | harness | sample run with a read | connections only to allowed hosts between click and result (A-015) |
| SC-26 | M-014 | harness | sample run with a read and produce | toolchain job processes own zero sockets |
| SC-27 | M-009, M-010 | harness | sample run | one listener on 127.0.0.1 |
| SC-28 | D-003 | browser and harness | switch off | no button; direct route refused; zero outbound |
| SC-29 | D-003, M-003 | browser | switch off; paste and typed sheets | two saves accepted |
| SC-30 | D-003 | browser and inspection | switch off, app restarted | still off; one setting in code |
| SC-31 | M-017 | harness and inspection | all fixture cases above | zero connections off 127.0.0.1; listed cases produced by test code |
| SC-32 | M-011, M-017 | inspection | none | no media file and no package reference added |
| SC-33 | C-018 | inspection | none | implementation and review reports state zero live reads |
| SC-34 | D-004 | record | fixture host with read fixture and blocked fixture | orchestrator's Browser-pane record: both paths saved (A-017) |

### Rollout and Rollback

- Rollout: the new components, routes, page parts, project-file declaration and tests land on the MC-18 session branch (C-029); the operator rebuilds, and the read stays off until turned on in the settings view (D-003).
- Rollback: turning the switch off stops all egress the same day without a rebuild; removing the MC-18 additions returns the MC-17 build, and the leftover switch file, fill records, read images and read log are ignored.

## Risks and Mitigations

| ID | Class | Trigger | Impact | Likelihood | Affects | Mitigation | Owner |
|---|---|---|---|---|---|---|---|
| R-001 | structural | A needed capability is absent from the base class library (A-001 false) | A package needs the CEO's approval by name | low | M-002, P-002 | P-002 builds the sender and fixture first | dev-1-implement |
| R-002 | structural | The test program cannot start the seam-composed app, or the fixture certificate cannot be trusted through the seam only (A-016 or A-018 false) | The seam reaches component tests only; app-level fixture runs need redesign | low | M-010, M-017, D-004, P-002 | P-002 proves the internal start path before other tests depend on it | dev-1-implement |
| R-003 | contract | A page cached from MC-17 calls facts save after the upgrade | No harm: with no fill record the save is unchanged | low | M-007, P-006 | Additive contract; same build serves page and routes | dev-1-implement |
| R-004 | contract | The facts-save refusal fires without a fill record | MC-17 hand typing breaks; SC-17 fails | low | M-007, P-006 | Refusal only when a fill record exists; 36 web tests in P-009 | dev-2-reviewer |
| R-005 | security | A link or hop passes the rule through a look-alike name, international form, trailing dot, user info, port, IP literal or another country's site (plan risk 16) | Egress beyond the CEO's bounds | medium | D-006, P-002 | Rule on lower-case ASCII host before each connection; SC-24 cases in P-002 | dev-2-reviewer |
| R-006 | security | An allowed name resolves to a loopback or local-network address | A read reaches this computer or the local network | low | D-006, P-002 | Connect-time refusal of non-public addresses | dev-2-reviewer |
| R-007 | security | The seam is reachable from the shipped app (A-008 false) | A read could be pointed at any host | low | M-010, D-004, P-002 | Internal type, test-only start path, inspection in P-009 | dev-2-reviewer |
| R-008 | security | The stack adds an unlisted header, keeps a cookie or honours a system proxy (A-007 false) | Breach of CEO decision 1313 item 2 | low | D-002, P-003 | Header log and proxy fixture in P-009 | dev-2-reviewer |
| R-009 | security | A foreign web page posts a read to the loopback app (plan risk 19) | A read with no operator click | low | M-007, M-009, P-008 | MC-17 guard on every read POST; foreign-Origin case in P-009 | dev-2-reviewer |
| R-010 | security | A fetched image has a path in its name, is not an image, or is very large (plan risk 20) | A write outside the folder or a non-image kept | medium | M-004, P-005 | Generated names, signature check, bounds | dev-1-implement |
| R-011 | security | Page text carries markup or script | Script runs in the app page | medium | M-008, P-008 | Values shown as text only; content policy unchanged | dev-2-reviewer |
| R-012 | security | A hostile page is huge or deeply nested | Memory exhaustion | low | M-003, D-014 | Size cap and depth limit | dev-1-implement |
| R-013 | performance | The live answer over filled values exceeds 1 s (A-011 false) | SC-10 fails | low | M-008, P-009 | Measured in P-009 | qa |
| R-014 | performance | Five slow hops and many images reach the per-click bound | The read stops before all images | medium | D-014 | Image-phase stop keeps facts and names remaining images | dev-1-implement |
| R-015 | operability | Real pages give no readable product (A-002) | The read rarely fills; paste is the common path | high | D-001, M-003 | Paste mode first-class; yield confirmed by the single live read (Q-006) | orchestrator |
| R-016 | operability | Real copied text lacks the expected labels (A-003 false) | Paste fills fewer fields | medium | M-003 | Unfound fields named for the operator; labels widened after the live read (Q-006) | dev-1-implement |
| R-017 | operability | Real pages declare no own images in structured data (A-004 false) | No images saved; step 2 uploads | medium | D-005, M-004 | Images named for upload as today | product-owner |
| R-018 | operability | Real image hosts are not under .susercontent.com (A-006 false) | Images named for upload | medium | D-005 | Q-003 to widen the list | product-owner |
| R-019 | operability | Real short links need page script (A-005 false) | Short links stop at paste | medium | D-006 | Operator pastes the full product link | orchestrator |
| R-020 | operability | The CEO reads SC-01 as all specifications saved as rows (A-009 false) | Conflicts with the unchanged 1-to-3 row check | medium | D-013 | Q-004 | product-owner |
| R-021 | operability | Shopee warns the account while the read is on; nothing switches off by itself (X-007) | The CEO's terms risk materialises | medium | D-003 | The CEO turns the switch off the same day; default off (Q-002) | product-owner |
| R-022 | operability | The check refuses the filled read date (A-013 false) | Every read save is refused | low | M-003, P-006 | Read date form tested in P-009 | dev-1-implement |
| R-023 | operability | A real sign-in or CAPTCHA page has unknown markers (A-014 false) | It stops as "no readable product" with a less precise reason | medium | D-010 | Still a stop with no retry; markers widened after the live read (Q-006) | dev-1-implement |
| R-024 | delivery | The read log paired with the harness is not accepted for SC-25 (A-015 false) | SC-25 evidence incomplete | low | M-018, P-009 | Q-005 | qa |
| R-025 | delivery | The headless shell cannot run the step-1 browser tests (A-012 false) | Browser evidence needs a CEO decision or a manual run | low | M-017, P-009 | MC-17 browser route reused | qa |
| R-026 | delivery | The Browser-pane run may not use the fixture host (A-017 false) | SC-34 has no route, since the shipped app cannot reach the fixture | low | D-004, P-009 | Q-007 | qa |
| R-027 | delivery | The Design Gate decision is delayed or declines the design | Implementation waits or returns to design | medium | P-001 | Q-008 | tech-lead |
| R-028 | delivery | The unmerged MC-16 or MC-17 branch changes after the MC-18 session branch is cut | Rebase and re-run of the 251 and 36 tests | medium | P-009 | Re-run in P-009 | orchestrator |

## Estimate and Confidence

- Overall: L (confidence: low), a change that adds the first external network crossing to the web app and rests on unconfirmed A-002 to A-006, A-016 and A-017.
- Breakdown:
  - M-001, M-002: M (confidence: medium); host rule, sender, stops.
  - M-003: M (confidence: low); parser over unknown real text (A-002, A-003).
  - M-004, M-005, M-006: S each (confidence: medium).
  - M-007, M-008: M (confidence: medium); five new routes, settings view, marks, picker.
  - M-010, M-011, M-016: XS (confidence: low for M-010, which rests on A-016; high for M-011 and M-016).
  - M-017: M (confidence: low); HTTPS fixture, seam host, 34-criterion cases (A-016, A-018).
- Scope assumptions: the toolchain is unchanged (C-020); one read at a time (D-014); only the base class library (C-017); answers to Q-002 and Q-004 confirm A-010 and A-009.
- Uncertainty drivers: real page content and markers, which only the orchestrator's single live read can show; composing the app with the seam in the test process; HTTPS fixture trust.

## Open Decisions and Escalations

E-AUTHORITY events are recorded as Q-001 to Q-004, each a product choice the CEO answers. No E-BOUNDARY event occurred.

| ID | Question | Blocking | Owner | Affects | Consequence |
|---|---|---|---|---|---|
| Q-001 | Should the read name itself honestly ("MediaCompanyPilot/1.0 (doc-trang-san-pham)", recommended) or send no user agent at all? The CEO answers. | non-blocking | product-owner | D-002 | Honest name: O-006 as designed. None: O-007; the header list loses one line |
| Q-002 | Should the switch start off until the operator turns it on (recommended), or start on? The CEO answers. | non-blocking | product-owner | D-003 | Off: as designed. On: the default value changes; nothing else |
| Q-003 | Are image hosts ending .susercontent.com enough, or should the list name other Shopee image hosts? The CEO answers. | non-blocking | product-owner | D-005 | Enough: as designed. Wider: names added to the held list |
| Q-004 | Is SC-01 met when every specification is offered and the operator keeps up to three rows, since the unchanged check allows 1 to 3 (A-009)? The CEO answers. | non-blocking | product-owner | D-013 | Yes: as designed. No: SC-01 needs amending, since X-006 forbids changing the check |
| Q-005 | Is the read log paired with the address harness accepted as SC-25 evidence (A-015)? | non-blocking | qa | M-018, P-009 | Yes: as designed. No: another evidence route |
| Q-006 | Real-page yield, labels, markers and short links are confirmed only by the orchestrator's single live read on a CEO-named link | non-blocking | orchestrator | M-003, D-005, D-006, D-010 | Confirmed: as designed. Not: paste mode remains the working path; labels and markers widened |
| Q-007 | May the SC-34 Browser-pane run use the test program's fixture host (A-017)? | non-blocking | qa | D-004, P-009 | Yes: as designed. No: SC-34 needs a product-owner amendment |
| Q-008 | Design Gate acceptance of D-001 to D-006, all at Proposed; tech-lead accepts under the producer exclusion rule, with the CEO's answers to Q-001 to Q-004 recorded | non-blocking | tech-lead | D-001, D-002, D-003, D-004, D-005, D-006 | Accepted: implementation starts at P-001. Not accepted: the design returns to the architect |

## Sign-off

- Architect: unsigned; producer of this package and its records, so excluded from accepting under the Producer Exclusion Rule
- Tech Lead: unsigned; accepting Design Gate owner for this package and for D-001 to D-006
- Product Owner: unsigned; product confirmation of Q-001 to Q-004 (the CEO answers)
- QA: unsigned; review of the test focus areas and of Q-005 and Q-007

## Appendix A: Statement Register

| ID | Statement | Source | Maps to |
|---|---|---|---|
| S-001 | At step 1 the operator pastes an affiliate or product link and presses one button | Feature request, operator step 1 | C-001, D-015 |
| S-002 | The tool reads the public shopee.vn page's name, price, classification, specifications, description and images, fills the sheet and saves the images | Feature request, operator step 2 | C-001, C-004, M-003 |
| S-003 | Every filled field is marked; the operator reviews each fact; nothing is accepted without review | Feature request, operator step 3 | C-005, M-005 |
| S-004 | A block, sign-in, CAPTCHA or unparseable page stops the read and offers paste mode, which fills the same fields | Feature request, operator step 4 | C-007, C-008, C-026, C-028 |
| S-005 | Reuse the MC-17 step 1 form, live check, image steps and request guard; the MC-16 check stays the authority | Feature request, reuse section | C-008, C-020, C-023 |
| S-006 | One product per click; no crawling, scans, batch or scheduled reads | Feature request, hard constraint 1 | C-009, C-023 |
| S-007 | No sign-in, cookies or session; affiliate.shopee.vn never opened | Feature request, hard constraint 2 | C-010, C-011 |
| S-008 | No CAPTCHA solving, impersonation, forged headers or signatures, faked user agent, IP rotation or proxy; a block means stop | Feature request, hard constraint 3 | C-007, C-012, C-013, C-030 |
| S-009 | Egress only to shopee.vn hosts during the read; toolchain zero sockets; listener on 127.0.0.1 | Feature request, hard constraint 4 | C-014, C-015 |
| S-010 | Downloads relaxed only for the supplied product's images | Feature request, hard constraint 5 | C-004, C-024 |
| S-011 | Fetched facts pass the unchanged MC-16 check; the CEO reviews facts and approves each video | Feature request, hard constraint 6 | C-006, C-020, C-025 |
| S-012 | One switch turns the read off and leaves paste mode working | Feature request, hard constraint 7 | C-016 |
| S-013 | No package or CDN script; no generative model, speech recognition or metered service | Feature request, hard constraint 8 | C-017 |
| S-014 | Agents never sign in or publish; MC-13, MC-14 and the mediacompany database untouched | Feature request, hard constraint 9 | C-021 |
| S-015 | Tests offline against a code-generated fixture; no media file; agents do not read shopee.vn | Feature request, hard constraint 10 | C-017, C-018, C-022 |
| S-016 | The step is shown working in the Browser pane against the fixture, including a block that falls back to paste | Feature request, done means | C-019 |
| S-017 | Committed and pushed on the MC-18 session branch; the CEO gets the compare link | Feature request, done means | C-029 |
| S-018 | A non-implementing role writes no repository file; "every" claims list their cases | Feature request, framework defects | C-027 |
| S-019 | The CEO carries the terms risk of reading pages | Business intent, CEO decision 1313 item 5 | C-030 |
| S-020 | On an adverse answer or account warning the read is switched off the same day | Business intent, CEO decision 1313 item 6 | C-016, Q-002 |
| S-021 | The local-only rule is relaxed only for the read step; video composition stays offline | Business intent, CEO decision 1313 item 3 | C-015 |
| S-022 | Image files of the supplied product may come from Shopee image hosts outside shopee.vn, never a page | CEO decision 1314 item 1 | C-004, Q-003 |
| S-023 | Short-link redirects are followed only while every hop is allowed, at most five, stopping on any other host | CEO decision 1314 item 2 | C-002, C-011, C-014 |
| S-024 | The single live read on a CEO-named link is the orchestrator's act, never an agent's | CEO decision 1314 item 3 | C-018, Q-006 |
| S-025 | Price and description shown, not saved; screenshot per fact by the operator; unsupplied fields left; no retry | CEO decision 1314 item 4 | C-003, C-005, C-007 |
| S-026 | The architect answers the expected yield; a forged signature or header is never an option | Orchestrator briefing, Planning Gate answer | C-012, M-003 |
| S-027 | The database, performance and security areas are triggered for this change | Task context, affected areas | C-026, D-012 |

Traceability closure: forward 27 of 27 statements mapped; backward 20 of 20 modules traced to facts or assumptions, 15 of 15 decisions to options, 19 of 19 options to constraint sets; lateral 28 of 28 risks, 10 of 10 plan steps and 6 of 6 records resolve; planner tasks T-001 to T-025 each bound by a plan step.
