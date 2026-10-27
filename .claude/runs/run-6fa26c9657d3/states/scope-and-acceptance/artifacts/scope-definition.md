```yaml
scopeDefinition:
  scopeId: SCOPE-2026-0003
  featureName: MC-18 Read from link, the web app reads one public Shopee product page and fills the fact sheet
  sourceInputs:
    - type: feature-request
      reference: runs/inputs/mc-18-feature-request.md
    - type: business-intent
      reference: runs/inputs/mc-18-business-intent.md
  producedBy: product-owner
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: provisional
  scopeVerdict: partially-bounded
  acceptanceCriteriaCount: 34
  inputDigest: sha256:a7733876abd63993a6781fa3a8d25170
  contextDigest: sha256:8432aa0d2cb1bf16c655abffc4957991
```

## Metadata

- Feature name: MC-18 Read from link, the web app reads one public Shopee product page and fills the fact sheet
- Requested by: the CEO, by CEO decision 1313 of 2026-10-27, approved verbatim in Vietnamese and confirmed in the MC-17 session (business intent, digest sha256:9d45256f50d5ae9688f9ebe6b979310c; feature request, digest sha256:3be564296ce7dfbb7c47644756a9d6c5)
- Business goal: The operator gets a product's facts and images into step 1 from one link and one click, with every fact still checked and reviewed, and with the company's exposure under Shopee's terms held to the bounds the CEO accepted.
- Target outcome: For one product per click, the fact sheet form is filled from that product's public page and its images are saved for the video; when Shopee refuses the read, the operator finishes step 1 by pasting the page text; nothing is accepted without the operator's review.
- Scope decision date: 2026-10-27

## Business Context

- Problem statement: At step 1 of the pilot web app the operator copies every fact of the fact sheet by hand from the product page and saves the product images themselves, and the CEO wants this step fully automatic ("Tôi chấp nhận rủi ro và muốn tự động hoàn toàn").
- Value hypothesis: Reading the one product the operator names removes the hand copying from step 1, while the unchanged fact-sheet check and the operator's per-fact review keep each fact's quality, and paste mode keeps step 1 usable whenever Shopee refuses an automatic read.
- Affected users: The operator of the pilot (the CEO now, an employee later); the CEO as reviewer of each fact and approver of each video; the CEO's affiliate account, which carries the terms risk the CEO accepted in CEO decision 1313 item 5. Steps 2 to 7 of the web app and the MC-16 toolchain are affected only as named in `S-002` and `S-004`.
- Success measure: The inputs state the done condition only (step shown working in the Browser pane against the local fixture, including a block that falls back to paste mode, `A-034`); no business measure such as time saved or share of reads that succeed is supplied, recorded as `Q-004`.

## In Scope

| ID | Scope Item | Rationale | Priority |
|---|---|---|---|
| `S-001` | At step 1 the operator pastes one affiliate or product link and presses one button, and the form is filled from that product's public page on shopee.vn: product name, classification, page address, read date and one fact row per specification; price and description are shown beside the form | Ticket operator steps 1 and 2; CEO decision 1313 item 1 (name, price, classification, specifications, description); `D-004`, `D-008` | must-have |
| `S-002` | The product's own images are saved for the video, the operator picks the three the fact sheet uses, and only those three reach the video's input set | Ticket operator step 2; ticket hard constraint 5; CEO decision 1313 item 3; `D-005`; image hosts held by `Q-001` | must-have |
| `S-003` | Every field filled from the page or from a paste carries a visible mark of its source, and the sheet saves only after the operator has confirmed each filled field | Ticket operator step 3 (marked, reviewed, nothing accepted without review); CEO decision 1313 item 4; `D-003` | must-have |
| `S-004` | Filled facts meet the MC-16 fact-sheet check and the live banned-term warning exactly as typed facts do, and the check itself is unchanged | Ticket operator step 3 and hard constraint 6; CEO decision 1313 item 4 | must-have |
| `S-005` | When Shopee refuses access, asks for sign-in, shows a CAPTCHA, gives no readable product, fails or does not answer, the read stops, says why in Vietnamese, and offers paste mode | Ticket operator step 4 and hard constraint 3; CEO decision 1313 item 2 | must-have |
| `S-006` | In paste mode the operator pastes the page text copied from their own browser and the same fields are filled, with no network connection; typing every fact by hand stays available | Ticket operator step 4; CEO decision 1313 items 2 and 6; `D-002` | must-have |
| `S-007` | A read covers one product per click, and runs with no sign-in, no cookie, no session, never on affiliate.shopee.vn, with no CAPTCHA solving, no forged header, signature or user agent, no IP rotation and no proxy | Ticket hard constraints 1, 2 and 3; CEO decision 1313 items 1 and 2; `D-006` | must-have |
| `S-008` | The app connects off the machine only to the allowed hosts and only while a read runs; the MC-16 toolchain and video composition keep zero sockets; the app keeps listening on 127.0.0.1 only | Ticket hard constraint 4; CEO decision 1313 item 3; CEO decision 1310 item 1; `D-001`; hosts held by `Q-001` and `Q-002` | must-have |
| `S-009` | One switch in one place turns the read off, and with it off paste mode and hand typing still complete step 1 | Ticket hard constraint 7; CEO decision 1313 item 6 (stop condition); `D-007` | must-have |
| `S-010` | The tests run offline against a local fixture server generated in code, no media file and no package is added, and agents read no live shopee.vn page during development unless the CEO names one | Ticket hard constraints 8 and 10; held by `Q-003` | must-have |
| `S-011` | The operator completes step 1 in the Browser pane against the local fixture: one read that fills and saves images, and one blocked read that falls back to paste mode and saves | Ticket done means | must-have |

## Out of Scope

| ID | Excluded Item | Reason | Revisit Trigger |
|---|---|---|---|
| `X-001` | Reading any page other than the supplied product's: other products, the shop page, categories, search, recommendations; lists of links, batch reads, scheduled or background reads | CEO decision 1313 item 1 and ticket hard constraint 1 | A new CEO decision naming a wider read |
| `X-002` | Sign-in, cookies or session of any kind, opening affiliate.shopee.vn, generating affiliate links, reading commission or account data | CEO decision 1313 item 2 and ticket hard constraint 2 | A new CEO decision |
| `X-003` | CAPTCHA solving, any anti-bot evasion (forged headers, signatures or user agent), IP rotation, proxies, and repeated attempts to get past a block | CEO decision 1313 item 2 and ticket hard constraint 3; a retry after a block is a further attempt to pass it (`D-006`) | None; the CEO kept these bounds unchanged |
| `X-004` | Downloading anything except the supplied product's own images: other products' images, product videos, the page's scripts, style sheets or fonts | CEO decision 1313 item 3 relaxes the no-download rule of CEO decision 1303 for those images only | A new CEO decision |
| `X-005` | A screenshot of the product page taken by the tool; each fact row's screenshot stays attached by the operator | CEO decision 1313 item 1 lists what the read takes and a screenshot is not in it; the unchanged check still requires one per row | `Q-005` answered otherwise |
| `X-006` | Changes to the MC-16 fact-sheet check, the banned-term list, the price rule, the script rules or any toolchain behaviour | Ticket hard constraint 6; CEO decision 1313 item 4 | A separate change request naming the check |
| `X-007` | Watching for Shopee's answer to the company's written question on image use or for account warnings, and switching the read off automatically | The stop condition is the CEO's act on that day (CEO decision 1313 item 6); the inputs ask only for one switch | A CEO request for monitoring |
| `X-008` | Reading any site other than shopee.vn, including Shopee sites of other countries | CEO decision 1313 names shopee.vn only | A new CEO decision naming the site |
| `X-009` | Publishing, creating accounts, contacting sellers, buying; frozen MC-13 and MC-14 material and the mediacompany company database | Ticket hard constraint 9; per-publication approval rule unchanged | A CEO unfreeze or publishing decision |
| `X-010` | Changes to steps 2 to 7 beyond receiving the images of `S-002` | The ticket changes step 1 only | A separate change request |
| `X-011` | A generative model, speech recognition, a metered service, or a package, font or CDN script not approved by name | Ticket hard constraint 8 | A CEO approval naming the item |
| `X-012` | Merging this branch or the MC-16 and MC-17 branches into main, and any release, readiness or closure decision | Gate and closure decisions belong to the gate owners | The Closure Gate, or a CEO decision |

## Acceptance Criteria

| ID | Criterion | Scope Ref | Verification Method | Priority |
|---|---|---|---|---|
| `A-001` | On a code-generated fixture product page, one click fills product name, classification, page address, read date and one fact row per specification (attribute, value as written, source section), each equal to the fixture after Unicode NFC normalisation | `S-001` | Fixture read; field-by-field comparison with the fixture's values, zero differences | must-have |
| `A-002` | Under `Q-002` option (a), a full product link and a short link that redirects within the allowed hosts to the same fixture product fill identical values; a chain longer than the hop limit `Q-002` sets stops with paste mode offered | `S-001` | Two fixture links compared field by field, zero differences; over-limit chain fixture stopped | must-have |
| `A-003` | The read price and description are shown beside the form marked as read from the page, and no saved fact sheet field holds them unless the operator puts them in a row | `S-001` | Fixture read then save; inspection of the saved sheet, zero fields holding the price or description | must-have |
| `A-004` | For a fixture product with five own images on a page that also shows three images of other products, exactly the five own images are saved, byte-equal to the fixture, and zero other files are fetched | `S-002` | Fixture server request log and saved-file digests: 5 of 5 equal, zero requests for the 3 other images or any script, style sheet or font | must-have |
| `A-005` | The operator picks three of the saved images; the sheet names exactly those three, only those three enter the input set, and step 2 reports no image missing | `S-002` | Browser run on the five-image fixture; input folder holds the 3 picked images and no other saved image | must-have |
| `A-006` | An image whose address is on a host outside the hosts allowed after `Q-001` is not fetched, is named on the page, and can be uploaded at step 2 as today | `S-002` | Fixture with one image on a disallowed host: zero requests to it, its name shown, step 2 upload accepted | must-have |
| `A-007` | Each filled field (product name, classification, page address, read date, each fact row) shows a Vietnamese source mark, read from the page or from a paste; typed fields show none | `S-003` | Browser run on the read fixture and the paste fixture; filled fields marked N of N, typed fields marked 0 | must-have |
| `A-008` | With N filled fields, saving is refused naming an unconfirmed field while N minus 1 are confirmed, and accepted when N are confirmed | `S-003` | Browser run on the read fixture at N minus 1 and N confirmations | must-have |
| `A-009` | After a read, the fields the page does not supply (video code, subject noun, shop name and Mall, niche, video number of the product, video focus, each row's screenshot) are empty, and saving is refused naming the first until the operator fills them | `S-003` | Fixture read then save; refusal names a field from that list; after filling, save accepted | must-have |
| `A-010` | For eight fixture pages each carrying one banned term of classes (a) to (g) or one money amount in a specification value, the check result on the read-filled sheet equals the result on the same values typed, and the live warning names term and class within 1 s | `S-004` | Run the 8 fixtures read and typed; comparison of term, class and result, 8 of 8 equal; live warning timed | must-have |
| `A-011` | This change modifies zero files of the MC-16 toolchain, and its 251 existing tests pass with none removed or weakened | `S-004` | Diff of the toolchain folder against the branch base, zero changes; test run 251 passing | must-have |
| `A-012` | Each of nine fixture answers stops the read and offers paste mode: access refused (403), too many requests (429), redirect to a sign-in page, a sign-in wall page, a CAPTCHA page, a page with no readable product, a server error (5xx), no connection, no answer within 30 s | `S-005` | Nine fixtures, each stopped with paste mode offered, 9 of 9; the no-answer case stopped by 30 s | must-have |
| `A-013` | After each stop of `A-012` the read sends zero further requests, and no request ever submits a CAPTCHA answer or a sign-in form | `S-005` | Fixture server request log per case: zero requests after the stop response, zero form submissions | must-have |
| `A-014` | Each stop message of `A-012` and each new label and button of step 1 is in Vietnamese with the reason and the next step, with no English word apart from codes in square brackets, file names and web addresses | `S-005` | Reviewer reads the 9 messages and the new step 1 labels against the rule; zero English words outside the permitted items | must-have |
| `A-015` | Pasting the code-generated visible text of the fixture page fills product name, classification and every specification row with the values `A-001` reads from the same fixture, shows price and description, and names each field it did not find | `S-006` | Paste fixture compared with the read of the same fixture, zero differences on the listed fields | must-have |
| `A-016` | Paste mode fills and saves with zero outbound connections | `S-006` | Socket harness on the app process during a paste run; outbound count zero | must-have |
| `A-017` | Typing the fact sheet by hand still works, and the existing web app tests (36) pass with none removed or weakened | `S-006` | Test run, 36 passing; inspection of the web test folder shows additions only | must-have |
| `A-018` | One click requests only the supplied link, its redirect hops, the one product page and that product's own images; zero requests go to the other product, shop, category or search links the fixture page carries; a second product needs a second click | `S-007` | Fixture server request log for one click, zero requests outside the listed set; second product read only after a second click | must-have |
| `A-019` | With a link entered and no click, the app open for 10 minutes starts zero reads; a link field holding two links is refused naming the one-product rule | `S-007` | Socket harness over 10 idle minutes, zero outbound; two-link fixture refused | must-have |
| `A-020` | Across all read fixtures, zero requests carry a Cookie or Authorization header, including after the fixture sets a cookie in a response | `S-007` | Fixture server header log, zero Cookie and zero Authorization headers | must-have |
| `A-021` | A link on affiliate.shopee.vn, or a redirect hop to it, opens zero connections to that host and stops with paste mode offered | `S-007` | Two fixtures (direct link, redirect hop); zero connections to that host name; paste mode offered | must-have |
| `A-022` | Every request header the read sends is on the list accepted at the Design Gate; the user agent is the reading component's own unmodified value; zero headers imitate another client, carry a signature or were copied from a browser session | `S-007` | Fixture header log compared with the accepted list, zero extra; reviewer inspection of the code that sets headers | must-have |
| `A-023` | With a proxy set in the process environment pointing at a fixture listener, that listener receives zero connections; every request of a read leaves from one local address; no proxy list or address pool exists in the code | `S-007` | Proxy fixture connection count zero; request log source addresses, one per read; reviewer code inspection | must-have |
| `A-024` | Links to each of these open zero connections and are refused with paste mode offered: a non-Shopee host, a host ending in shopee.vn that is not a subdomain (xshopee.vn), shopee.vn as a left label (shopee.vn.example.test), an IP address, a Shopee site of another country, and a redirect hop from an allowed host to any of them | `S-008` | Six fixture links, zero connections each, 6 of 6 refused | must-have |
| `A-025` | During the sample run, every outbound connection of the app goes to an allowed host and falls between a read click and its shown result; zero outbound connections occur while idle and during steps 2 to 7 | `S-008` | Socket harness on the app process with connection times and remote host names recorded | must-have |
| `A-026` | The toolchain job processes, including produce and video composition, own zero sockets during a sample run that includes a read | `S-008` | The toolchain socket harness applied to the job processes; count zero, sampling interval recorded | must-have |
| `A-027` | The app listens on 127.0.0.1 only, and the read adds no listening socket | `S-008` | Socket harness listener list: one listener on 127.0.0.1, unchanged from MC-17 | must-have |
| `A-028` | With the switch off, the read button is not offered, a read request sent directly is refused in Vietnamese, and zero outbound connections occur | `S-009` | Browser run and direct request with the switch off; socket harness count zero | must-have |
| `A-029` | With the switch off, paste mode and hand typing each fill and save a fact sheet the check accepts | `S-009` | Browser run of both paths with the switch off; two saves accepted | must-have |
| `A-030` | Exactly one setting turns the read on and off; the operator sets it on the app's settings screen without editing a file or typing a command, and it holds across a restart | `S-009` | Reviewer code inspection, one setting found; browser run off, restart, still off | must-have |
| `A-031` | The tests of this change open zero connections off 127.0.0.1, and the code-generated fixture server serves at least the cases of `A-002`, `A-004`, `A-006`, `A-012`, `A-015`, `A-021` and `A-024` | `S-010` | Socket harness over the test run, outbound count zero; inspection that each listed case exists and is produced by test code | must-have |
| `A-032` | This change adds zero media files and zero package references (NuGet, npm or pip, or CDN script), and calls no generative model, speech recognition or metered service | `S-010` | Scan of added files for media extensions, zero; comparison of project and package files with the branch base, zero additions; code inspection | must-have |
| `A-033` | No agent reads a live shopee.vn page during development, except one read of the one link the CEO names if `Q-003` is answered yes | `S-010` | Implementation and review reports state each live read; count zero, or one on the named link | must-have |
| `A-034` | In the Browser pane against the fixture, one read fills step 1 and saves images, and one blocked read offers paste mode; after paste and per-field confirmation the sheet saves and the check accepts it | `S-011` | Reviewer's recorded Browser-pane run: both paths reached, two saves accepted | must-have |

## Constraints and Dependencies

- Business constraints: The CEO carries the terms risk as the individual creator (CEO decision 1313 item 5: Shopee's partner terms forbid partners collecting information on the platform; a breach can lock the affiliate account and lose commission; fines up to 10 million dong per unauthorised use of content); the CEO reviews each fact and approves each video before publication, unchanged; the stop condition (an adverse Shopee answer to the company's written question, or any account warning, turns the read off the same day) stands, and the written question is still sent; this change adds no spend.
- Regulatory or policy constraints: All bounds of CEO decision 1313 items 1 to 4 and of ticket hard constraints 1 to 10, unchanged and each carried by `A-001` to `A-034`; CEO decision 1303's no-download rule is relaxed only for the supplied product's images; CEO decision 1308's local-only rule is relaxed only for the read step; CEO decision 1310 keeps the listener on 127.0.0.1; agents create no account, sign in nowhere, contact no seller, buy nothing and publish nothing.
- Delivery constraints: the MC-18 session branch, cut from the MC-17 branch, which like MC-16 is not merged to main (`X-012`); no package, font or script downloaded unless the CEO approves it by name; tests offline with code-generated fixtures; the CEO decides at the Scope and Design Gates. Repository context: the task context names no relevant files, so `src/MediaCompany.Pilot.Web` FactSteps.cs, Routes.cs and Pages/app.js and `src/MediaCompany.Pilot` FactSheet.cs were read on the caller's direction to learn the step 1 fields and the check. Affected areas: database is triggered by the word in ticket constraint 9, which keeps the database untouched (`X-009`); security is affected (outbound connections, host allow-list, redirect handling, request headers), so skill S09 was read and the area is held by `S-007`, `S-008`, `A-020` to `A-025`.
- External dependencies: Shopee's public product pages on shopee.vn and, if `Q-001` allows, its image hosts; risk: Shopee product pages are largely rendered by script and its data interface is protected by anti-bot signatures, so an unmodified read may get little or no product data, forging the signatures is forbidden (`X-003`), and paste mode may be the common path (`D-002`, `Q-006`); the installed .NET 10 SDK and the installed headless browser named in the ticket; the delivered MC-16 toolchain and its fact-sheet check; the delivered MC-17 web app.

## Scope Decisions

| ID | Decision | Rationale | Impact | Decided By |
|---|---|---|---|---|
| `D-001` | The allowed hosts are shopee.vn and its subdomains except affiliate.shopee.vn; image hosts outside shopee.vn are excluded unless the CEO allows them at `Q-001` | CEO decision 1313 item 3 names shopee.vn hosts only, and the narrowest reading is held until the CEO widens it | If `Q-001` is no, images are uploaded at step 2 as today and `A-004` and `A-005` apply only to images on allowed hosts | product-owner; the CEO decides the widening at `Q-001` |
| `D-002` | Paste mode is a full path in its own right: it fills the same fields, works offline and with the read switched off | CEO decision 1313 items 2 and 6 make it the fallback and the state after the stop condition; reads may often fail (External dependencies risk) | `A-015`, `A-016`, `A-029`; step 1 stays usable whatever Shopee does | product-owner |
| `D-003` | Each filled field is confirmed by the operator one by one before the sheet saves | The ticket says the operator reviews each fact and nothing is accepted without review; a per-field confirmation makes that review checkable | `A-008`; one extra action per filled field | product-owner |
| `D-004` | Price and description are shown, marked as read, but are not placed in any saved field; the operator may put them in a row, where the check decides | The fact sheet has no price or description field, and the unchanged check refuses a money amount in a fact value, so a filled price row would be refused every time; the script carries no price (CEO decision 1306) | `A-003`; the price is visible to the operator but never enters the sheet unaided | product-owner; the CEO may reverse it at the Scope Gate |
| `D-005` | All the product's own images are saved for the video and the operator picks the three the sheet uses; only those enter the input set | MC-17 step 2 accepts exactly the three images the sheet names; extra files there would block production | `A-004`, `A-005`; step 2 is unchanged | product-owner |
| `D-006` | A read starts only on a click, ends at the first stop signal with no retry, and sends no cookie of any origin, including one set during the read | A block means stop (CEO decision 1313 item 2); a retry or a replayed cookie is a further attempt to pass a block or keep a session; the narrowest reading of no cookie or session | `A-013`, `A-019`, `A-020`; some reads that would succeed with a cookie stop and fall back to paste mode | product-owner |
| `D-007` | The off switch is set by the operator on the app's settings screen, without a file edit or a command, and holds across restarts | The stop condition must take effect the same day, and the operator is non-technical (the MC-17 goal of no file editing) | `A-030` | product-owner |
| `D-008` | Fields the page does not supply (video code, subject noun, shop name and Mall, niche, video number, video focus, screenshots) stay for the operator to fill | CEO decision 1313 item 1 lists what the read takes; shop name and the operator's choices are not in it | `A-009`; step 1 is not fully automatic for these fields | product-owner |

## Open Questions

| ID | Question | Blocking | Owner | Needed By |
|---|---|---|---|---|
| `Q-001` | Shopee serves product images from hosts outside shopee.vn (for example *.susercontent.com); may the read fetch the supplied product's own images from those hosts? Options: (a) yes, for that product's image files only, never for pages; (b) no, images stay step 2 uploads. Recommended: (a), since without it no image is saved automatically | yes | CEO | Scope Gate |
| `Q-002` | Affiliate links are commonly s.shopee.vn short links; may the read follow redirects? Options: (a) yes, only while every hop is an allowed host, at most five hops, stopping on any hop elsewhere including affiliate.shopee.vn; (b) no, only full product links are read. Recommended: (a) | yes | CEO | Scope Gate |
| `Q-003` | May an agent read one real public product page once, on a link the CEO names, to show the step working? Options: (a) yes, one read of that link; (b) no, the demo uses the local fixture and paste mode only. Recommended: (b), fixture plus paste mode | no | CEO | Scope Gate |
| `Q-004` | Is the done condition (`A-034`) the only success measure? Options: (a) yes; (b) add a measure with a threshold, such as minutes to a saved fact sheet or the share of reads that fill without paste mode. Recommended: (a) | no | CEO | Scope Gate |
| `Q-005` | The unchanged check needs a screenshot per fact row, and the read does not take one (`X-005`). Options: (a) the operator keeps attaching screenshots; (b) the CEO allows the tool to save a screenshot of the product page as the source. Recommended: (a), as CEO decision 1313 does not list a screenshot | no | CEO | Scope Gate |
| `Q-006` | Without forged signatures, how much product data can an unmodified read obtain from a page that is rendered by script, and is paste mode then the expected common path? The answer sets what `A-001` can expect on real pages | no | architect | Design Gate |

## Handoff

- Downstream owner: planner (execution planning runs in parallel and consumes this artifact after the Scope Gate); architect for the technical approach and `Q-006`. The CEO answers `Q-001` to `Q-005`.
- Gate: Scope Gate of implement-feature; owners product-owner and business-analyst, with the producer exclusion applying, so the decision on this artifact rests with business-analyst and the CEO's answers.
- Evidence for the gate: `S-001` to `S-011`, `X-001` to `X-012`, `A-001` to `A-034`, `D-001` to `D-008` and `Q-001` to `Q-006` in this artifact. Status provisional with verdict partially-bounded; `Q-001` and `Q-002` are blocking and stand. No gate decision and no readiness claim are recorded here.
- Deferred to downstream: decomposition and sequencing (planner); how the page is read, how the allowed hosts are enforced and directed to the fixture in tests, the header list `A-022` refers to, the parser for pages and pastes, the off switch's mechanism, and the source marks (architect); the test strategy (qa). Most uncertain: `S-001` and `S-006` (the risk in External dependencies and `Q-006`), then `S-002` (held by `Q-001`).
