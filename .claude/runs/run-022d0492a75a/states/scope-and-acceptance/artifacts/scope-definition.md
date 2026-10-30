```yaml
scopeDefinition:
  scopeId: SCOPE-2026-0002
  featureName: MC-17 Vietnamese local web front end for the pilot video toolchain, with in-browser narration recording
  sourceInputs:
    - type: feature-request
      reference: runs/inputs/mc-17-feature-request.md
    - type: business-intent
      reference: runs/inputs/mc-17-business-intent.md
  producedBy: product-owner
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: provisional
  scopeVerdict: partially-bounded
  acceptanceCriteriaCount: 30
  inputDigest: sha256:04fe2efd4ac3b3e89cf216028f58d5e0
  contextDigest: sha256:8432aa0d2cb1bf16c655abffc4957991
```

## Metadata

- Feature name: MC-17 Vietnamese local web front end for the pilot video toolchain, with in-browser narration recording
- Requested by: the CEO, in chat on 2026-10-27 (business intent; the CEO is the current operator and approves each publication)
- Business goal: One pilot product becomes one reviewable, compliant 9:16 video that an operator can make, and the CEO can approve, without technical help, with the pilot's checks unchanged.
- Target outcome: Each pilot video is produced and approved through a guided Vietnamese path, with no data file edited and no command typed, and nothing is published.
- Scope decision date: 2026-10-27

## Business Context

- Problem statement: Producing one pilot video requires the operator to write a data file, arrange input folders and type command lines, which a non-technical operator cannot do unaided.
- Value hypothesis: A guided path that asks for each fact, product image, narration take and presenter clip in turn lets a non-technical operator complete one video, while the checks that decide the video stay as delivered.
- Affected users: The operator of the pilot, the CEO now and a human employee later, and the CEO as approver of each video. Viewers are reached only after the CEO publishes, which this change does not do.
- Success measure: The inputs state no measure beyond the done condition A-029; no business measure is supplied, recorded as open question Q-004.

## In Scope

| ID | Scope Item | Rationale | Priority |
|---|---|---|---|
| `S-001` | The operator enters the fact sheet's header fields, including the video code, and per fact row the label, subject noun, attribute, value as written on the page, source section and screenshot, in a form, without editing a data file | Ticket step 1 (fact sheet in a form); CEO request for step-by-step entry by a non-technical user | must-have |
| `S-002` | As the operator types, the form names any banned term in a fact value, an override or a script sentence, with the term and its banned class | Ticket step 1 (banned terms checked live); ticket constraint 4 (the check stays in the toolchain) | must-have |
| `S-003` | The operator attaches the three product images named in the fact sheet, and the form names any missing or unnamed image | Ticket step 2 (upload or pick the three images) | must-have |
| `S-004` | The operator sees the generated script sentence by sentence, each with its source line, and ticks each source line before the next step opens | Ticket step 3; CEO decision 1306 item 1 (the tool writes the script); the script's source lines in the pilot kit | must-have |
| `S-005` | The operator records the narration in the browser with a visible timer, playback of the take and re-recording, and the recording stops by itself at 60 s | Ticket step 4; CEO decision 1306 item 4 (60 s is a hard limit); ticket constraint 2 (no voice generation) | must-have |
| `S-006` | An uploaded recording reaches the toolchain unchanged, a recording made in the browser is converted once into a format the toolchain accepts, and a reused recording is refused | Ticket step 4 (upload stays an option; conversion by the installed media tool); digest-based reuse rule in the MC-16 release note (K-015); D-003 | must-have |
| `S-007` | The operator attaches the presenter clip, and the form shows the portrait check result and the length difference from the recording in seconds | Ticket step 5; CEO decision 1308 item 3 (clip and recording tolerance) | must-have |
| `S-008` | The operator starts production, sees progress, and sees each outcome as the toolchain states it, in Vietnamese, with the reason and the next step | Ticket step 6 (progress, and each refusal shown in plain Vietnamese with what to do next) | must-have |
| `S-009` | When pause detection does not match the sentence count, the form opens a timing editor with one start and one end per sentence, and the toolchain checks what the operator enters | Ticket step 6 (manual timing editor); CEO decision 1306 item 3 | must-have |
| `S-010` | The operator previews the produced video and the kit checklist, which shows the one item the toolchain pre-ticks, the two items the operator reads against the script, and the other items unticked | Ticket step 7 (preview the video and the kit 04 checklist); CEO decision 1309 item 2 | must-have |
| `S-011` | The operator ticks the 26 checklist items the toolchain leaves to the operator, and then approves the video by the SHA-256 shown beside the preview, with the approver's name; the toolchain decides the approval | Ticket step 7 (approves by hash); per-publication approval rule of the decision record; D-005 | must-have |
| `S-012` | After approval the operator sees the verify result and the path of the video file to post by hand, and the app offers no action that sends the video to any address | Ticket step 7 and constraint 5 (nothing publishes) | must-have |
| `S-013` | The home screen and the seven step screens, with their labels, buttons and messages, are in Vietnamese | Ticket labels (vietnamese); the feature request asks for a friendly interface for a non-technical user | must-have |
| `S-014` | The app runs on the operator's own computer, opens in the installed browser, and loads nothing from and sends nothing to another machine | Ticket constraints 1 and 3 (no download; microphone access needs a secure context on localhost); D-002, held by Q-001 | must-have |
| `S-015` | A request that changes state (produce, timing save, approval) is accepted only from the app's own page on this computer | Declared security area of the operator; D-006 | must-have |
| `S-016` | The app passes each produce, timing and approval request to the delivered pilot toolchain (MC-16), which decides; the app adds no pass path, and the toolchain's checks, limits and settings are unchanged | Ticket constraint 4 (front end only); D-001 | must-have |
| `S-017` | The automated tests and the sample run need no microphone, no network connection and no media file in the repository, and the recording flow is tested with a code-generated audio stream | Ticket constraint 7 (offline tests with code-generated fixtures) | must-have |
| `S-018` | The operator completes steps 1 to 7 end to end in the installed browser on a code-generated sample, and the approved video verifies | Ticket done means (shown working in the browser on a code-generated sample, including the record step) | must-have |

## Out of Scope

| ID | Excluded Item | Reason | Revisit Trigger |
|---|---|---|---|
| `X-001` | Publishing, scheduling, posting, uploading to, and sign-in or account actions on Shopee, HeyGen, D-ID or another service | Agents never publish or act on accounts; the CEO publishes each video by hand (ticket constraint 5; per-publication approval rule) | A separate CEO decision naming a publishing scope |
| `X-002` | Generating the presenter clip, and calls from the app to a presenter, voice, speech-recognition, text-to-speech, voice-cloning, language-model or metered service | Ticket constraint 2; CEO decisions 1304, 1305 and 1307 | A CEO decision naming the component and its download; no generative model inside the pilot |
| `X-003` | Opening, fetching or reading a product page or platform data; facts come only from what the operator enters | The pilot's fact rows come from the operator's reading of the page; the MC-16 scope excluded page access (kit Part B) | A counsel answer and a CEO decision naming the permitted source |
| `X-004` | Writing or editing script sentences by hand in the app | CEO decision 1306 item 1: the tool writes the script; the fact-row override the toolchain already accepts is not changed | A CEO decision |
| `X-005` | Accounts, sign-in, multiple users, and access from another computer, a phone or another network address | One operator on one computer; the loopback proposal D-002 and the no-account constraint | A CEO decision naming remote or multi-user access and its security review |
| `X-006` | Changes to the toolchain's checks, limits, refusal rules, settings values, or fixes to MC-16 known issues K-001 to K-016 | Ticket constraint 4: the front end weakens no check and the toolchain keeps deciding | A separate change request naming the check, limit or issue |
| `X-007` | Editing the CEO-approved settings numbers from the app: safe area, caption size, overlay size, clip tolerance and caption page minimum | The toolchain refuses settings looser than the CEO's values (CEO decisions 1308 and 1309) | Q-002 answered, and a CEO decision on the one-time set-up |
| `X-008` | Batch or multi-product production, more than one video in progress, more than one presenter, and captions or scripts in a language other than Vietnamese | Pilot scope: one product, one presenter, Vietnamese, one video at a time (MC-16 scope exclusion of batch and other languages) | A CEO decision after the day-30 reading of the pilot |
| `X-009` | Changes to frozen Wave 12 (MC-13) and Wave 13 (MC-14) material, to the company store database, or to Docker | Ticket constraint 6 (frozen by CEO decision) | A CEO unfreeze decision |
| `X-010` | Merging the MC-16 branch or this change's branch into main, and any release, readiness or closure decision | Gate and closure decisions belong to the gate owners; the branch is cut from the MC-16 branch, which is not merged | The Closure Gate, or a CEO decision |
| `X-011` | Rewording kit 04 item B7 and accepting the kit 03 edits left open by MC-16 (K-011, K-014) | Kit text is outside the app and not requested by the ticket | A separate product owner decision |

## Acceptance Criteria

| ID | Criterion | Scope Ref | Verification Method | Priority |
|---|---|---|---|---|
| `A-001` | The form holds the eleven header fields and the six fact-row fields; with each header field blanked in turn, and each row field blanked in turn on one row, the sheet is refused naming the blanked field | `S-001` | Seventeen fixtures (11 header, 6 row), each refused naming its field; one complete fixture accepted | must-have |
| `A-002` | A fact row with no screenshot attached, or whose screenshot name matches no attached file, is refused naming the row and the file name | `S-001` | Two fixtures, each refused naming the row and the file name | must-have |
| `A-003` | The stored text of each typed value equals the typed text after Unicode NFC normalization, with Vietnamese diacritics intact | `S-001` | Entry of a fixture value with diacritics; comparison of stored and typed text, zero differences | must-have |
| `A-004` | Typing one example term from each banned class (a) to (g) into a fact value shows the term and its class letter within 1 s of the last keystroke, for seven of seven classes | `S-002` | Browser run with the seven example terms; seven of seven shown with the term and class | must-have |
| `A-005` | For a fixture set of strings (the toolchain's banned-term fixtures plus the seven class examples), the form's live result equals the toolchain's check result | `S-002` | Run the set through the form's live check and the toolchain's check; comparison of term, class and result, zero differences | must-have |
| `A-006` | The image step accepts exactly the three files named in the fact sheet's images-used field; with two files, or with a file not named there, production is not offered and the form names the missing or extra file | `S-003` | Fixtures: two files, four files, one unnamed file (each blocked and named); the three named files (accepted) | must-have |
| `A-007` | The next step opens only when the number of ticked source lines equals the number of script sentences, and the sentences appear in the toolchain's script order | `S-004` | Browser run on a five-sentence fixture: four ticks keep the next step closed, five open it | must-have |
| `A-008` | The take stops by itself at 60.0 s, and a take longer than 60.0 s is refused naming its length | `S-005` | Browser run with a code-generated audio stream longer than 60 s: stop at 60.0 s, take length at most 60.0 s; upload fixtures of 60.0 s (accepted) and 61.0 s (refused) | must-have |
| `A-009` | After a take the operator can play it from start to end, and a re-record leaves exactly one take held for the video | `S-005` | Browser run: record, play, re-record; one take held, with a different SHA-256 after the re-record | must-have |
| `A-010` | An uploaded recording reaches the toolchain with the SHA-256 it had when uploaded; an uploaded file in a format the toolchain does not accept is refused with the accepted extensions listed, and is not converted | `S-006` | Upload fixture: the package's recorded input digest equals the upload's digest; a fixture in a non-accepted format refused with the list | must-have |
| `A-011` | A take recorded in the browser is converted once, the converted file's SHA-256 is the one the toolchain admits and checks for reuse, and the same converted file offered for a second video is refused naming the first video | `S-006` | Code-generated browser-format fixture converted; first video admitted; second video with the same file refused naming the first | must-have |
| `A-012` | A presenter clip is accepted only when portrait and within 0.5 s of the recording's length; a landscape clip is refused naming its pixel size, and a clip 0.6 s longer is refused naming the difference in seconds | `S-007` | Three fixtures: portrait within tolerance accepted; landscape refused with size; 0.6 s difference refused with 0.6 s | must-have |
| `A-013` | Production shows progress until the toolchain ends, and shows the toolchain's line unchanged for the success result and for each of the fourteen refusal codes of the operator guide's refusal table | `S-008` | One fixture per refusal code (14) and one success fixture; shown text equals printed text in 15 of 15 cases | must-have |
| `A-014` | When production stops for pauses not matching the sentence count, the timing editor opens with one start and one end field per sentence; entries are accepted under the toolchain's timing rules, and a refused entry names the first failing sentence | `S-009` | Fixture with two sentences run together: editor opens; fixture with known pauses: correct timings produce success; fixture with an end past the recording: refused naming the sentence | must-have |
| `A-015` | The checklist shows the 27 kit 04 items: B8 pre-ticked with the toolchain's note, B3 and B5 unticked with the toolchain's note, and the other 24 items unticked | `S-010` | Inspection of the rendered checklist against the kit 04 item list: 27 items, 1 ticked, 26 unticked | must-have |
| `A-016` | The produced video plays in the page at 9:16 proportions from the package's video file, and the package's frame image is shown | `S-010` | Browser check: element aspect ratio 9:16; SHA-256 of the played file equals the package manifest's value | must-have |
| `A-017` | Approval is offered only when 26 operator items are ticked: with 25 ticked it is not offered, with 26 it is | `S-011` | Browser run at 25 and at 26 ticks; the count reads "n of 26" | must-have |
| `A-018` | Approval sends the SHA-256 shown beside the preview with the approver's name; a video changed by one byte after preview is refused with the toolchain's hash-mismatch line, and no approval is recorded | `S-011` | Browser run: approval accepted with the shown hash; one-byte-changed fixture refused; approval record count unchanged | must-have |
| `A-019` | After approval the page runs verify and shows the verify line with the approved SHA-256 and the approver's name, and the path of the video file to post by hand; the page has no control that sends the file to any address | `S-012` | Comparison of displayed and approved SHA-256, zero differences; inspection of the page controls and the app's routes for a send function, zero found | must-have |
| `A-020` | The home screen, the seven step screens with their labels and buttons, and the fourteen refusal messages of A-013 show no English word apart from file names, SHA-256 and codes in square brackets | `S-013` | Reviewer reads the eight screens and the fourteen messages against the rule; zero English words outside the permitted items | must-have |
| `A-021` | The app listens only on the address 127.0.0.1, opens zero outbound connections during the sample run, and loads zero resources from any other address | `S-014` | Socket harness on the app process: listener address 127.0.0.1 only, outbound count zero; page resource list has zero outside addresses | must-have |
| `A-022` | The toolchain job processes started by the app own zero sockets during the sample run | `S-014` | The socket harness of the pilot toolchain applied to the app's job processes; count zero, with its sampling interval recorded | must-have |
| `A-023` | Nothing is downloaded: this change adds zero package references beyond the installed shared framework, and the page references zero external resources | `S-014` | Comparison of project and package manifest files with the prior version, zero additions; scan of page resources, zero external addresses | must-have |
| `A-024` | Six state-changing requests (produce, timing save and approval, each from a foreign origin and from a foreign host name) are refused, and the pilot folder's files are unchanged afterwards | `S-015` | Six requests of each kind sent as stated; each refused; file hashes of the pilot folder equal before and after | must-have |
| `A-025` | For the fourteen refusal fixtures and the approval fixtures of A-013 and A-018, the app's result equals the toolchain's result on the same input: zero cases where the app shows a pass the toolchain refuses | `S-016` | Each fixture run through the toolchain directly and through the app; comparison of result line and exit status, zero differences | must-have |
| `A-026` | The 251 existing toolchain tests pass after this change, and no toolchain test is removed or weakened | `S-016` | Run of the existing test suite, 251 passing; inspection of the toolchain's test folder shows additions only | must-have |
| `A-027` | This change adds zero media files to the repository, and the tests create their fixtures in code | `S-017` | Scan of files added by this change for media extensions, zero found; inspection that each fixture is produced by test code | must-have |
| `A-028` | The recording-flow test (record, stop at 60 s, playback, re-record) passes on a machine with no microphone attached, using a code-generated audio stream, with zero network connections opened | `S-017` | Test run on a machine with no microphone; socket count zero; test passes | must-have |
| `A-029` | On a code-generated sample the operator reaches steps 1 to 7 in the installed browser, including the record step, and the verify line shows the SHA-256 that was approved | `S-018` | Reviewer's recorded browser run: seven of seven steps reached; approved and verified SHA-256 equal, zero differences | must-have |
| `A-030` | During the sample run the operator types no command line and edits no JSON file after the one-time set-up (Q-002) | `S-018` | Walkthrough log of the sample run: zero command lines typed and zero JSON files edited by the operator | must-have |

## Constraints and Dependencies

- Business constraints: The CEO approves each publication individually and carries the legal position under the decision record; one presenter on one account; this change adds no spend; the operator may be a human employee, who may record the narration in their own real voice (CEO decision 1307).
- Regulatory or policy constraints: No text-to-speech, voice cloning or speech recognition, and a fresh real recording for each video (CEO decisions 1304, 1305 and 1307); the AI-label overlay and the disclosure at the start and end of each video; no use or review claims, health or body claims, money amounts, promotions or other platform names in any text, as the toolchain's banned-term list states; the counsel stop rule applies outside the app; agents create no account, sign in nowhere, contact no seller, buy nothing, download no asset and publish nothing.
- Delivery constraints: The change is cut from the MC-16 branch, which is not merged to main (X-010); nothing is downloaded (no package, font, script, CDN asset or model) unless the CEO approves it by name; tests run offline with code-generated fixtures; the company store, Wave 12 and Wave 13 material and Docker are untouched (X-009). Affected areas: database is triggered by the mention of the company store, which X-009 keeps untouched; performance is triggered by the word "slow", which describes image motion, and no performance expectation is supplied, so none is scoped; security is declared by the operator and is held by S-014, S-015 and A-021 to A-024.
- External dependencies: The installed .NET 10 SDK and ASP.NET Core 10 shared framework; ffmpeg and ffprobe, which the delivered toolchain checks against configured digests; a Chromium-based browser with the MediaRecorder interface; the delivered MC-16 pilot toolchain and its settings file; the presenter clip, made outside the app by the CEO in HeyGen or D-ID; the typeface the toolchain uses.

## Scope Decisions

| ID | Decision | Rationale | Impact | Decided By |
|---|---|---|---|---|
| `D-001` | The app is a front end on the delivered pilot toolchain: each produce, timing entry and approval is decided by the toolchain, and the app adds no pass path | Ticket constraint 4 keeps the checks, limits and approval in the toolchain, which the CEO approved; a second decision point would drift from those checks | A-025 and A-026 hold; the app may show a refusal the toolchain gives, never a pass it refuses | product-owner, on ticket constraint 4 |
| `D-002` | Proposed default, not decided: the app listens on 127.0.0.1 only, opens no outbound connection, and the toolchain's job processes keep zero sockets | Browser microphone access needs a secure context, which localhost provides, and a web app must listen on a socket; CEO decision 1308 fixed zero sockets for the toolchain, not for a new front end, so the smallest exposure is proposed for the CEO to decide | If the CEO declines, S-005, S-014 and S-018 are held and the verdict stays partially-bounded; A-021 and A-022 apply to the proposed default only | product-owner proposes; the CEO decides at the Scope Gate (Q-001) |
| `D-003` | A browser recording is converted once, by the installed media tool, into a format the toolchain accepts; an uploaded file is never converted, and an uploaded file in a format the toolchain does not accept is refused | The reuse check compares digests (K-015 of the MC-16 release note); re-encoding an upload would give a new digest and let a reused recording pass; the browser's own format is not accepted, so one conversion of the new take is needed | A-010 and A-011: the reuse check sees the operator's own bytes, and the admitted digest is the converted file's | product-owner, on the reuse rule in the decision record and the MC-16 release note |
| `D-004` | The live banned-term check in the form uses the toolchain's own check and list; the page holds no copy of the list | A second list would drift from the toolchain, and a page that disagrees with the toolchain would show a pass the toolchain refuses | A-005 is the test of this decision | product-owner, on ticket constraint 4 |
| `D-005` | Approval is offered only when the operator has ticked the 26 items the toolchain leaves to the operator; the app sends the SHA-256 shown beside the preview, and the toolchain recomputes it and compares | Ticket step 7 has the CEO tick B3, B5 and the other items, then approve by hash; a non-technical operator cannot copy a 64-character hash, and the comparison stays with the toolchain | A-017 and A-018; the approval still fails for any changed byte | product-owner, on ticket step 7 and CEO decision 1309 item 2 |
| `D-006` | A request that changes state (produce, timing save, approval) is accepted only from the app's own page on this computer; a request from another origin or host name is refused | The declared security area applies: a web page open in the same browser could otherwise call the loopback address and start a production or an approval, which the per-publication approval rule forbids; the mechanism belongs to the architect | A-024 | product-owner; mechanism for the architect |

## Open Questions

| ID | Question | Blocking | Owner | Needed By |
|---|---|---|---|---|
| `Q-001` | Do you accept that the app listens on 127.0.0.1 only, opens no outbound connection, and that the toolchain's job processes keep zero sockets (D-002)? Options: (a) yes, as proposed; (b) no socket of any kind, so the browser recording and the browser path cannot be built and this change is re-scoped | yes | CEO | Scope Gate |
| `Q-002` | Who prepares the one-time settings (paths and SHA-256 of the media tools and the typeface, and the pilot folder outside the repository) so that the operator never edits JSON? Options: (a) a technical helper prepares the file once and the app only reads it; (b) the app asks for the pilot folder on a first-run Vietnamese screen, finds the tool paths and computes their digests, with the CEO's numbers fixed; (c) the operator keeps editing the file, which leaves the ticket's aim unmet | yes | CEO | Scope Gate |
| `Q-003` | Who may press approve? The per-publication approval rule says the CEO approves each publication, but the toolchain records only a typed name. Options: (a) only the CEO approves, and the approval screen states that rule; (b) an employee operator may approve on the CEO's written instruction, with the CEO's name recorded | no | CEO | Design Gate |
| `Q-004` | Is the done condition (A-029) the only success measure for this change? Options: (a) yes, the done condition only; (b) add a measure the CEO states with a threshold, such as minutes from the fact sheet to an approved video | no | CEO | Scope Gate |

## Handoff

- Downstream owner: planner (execution planning runs in parallel and consumes this artifact after the Scope Gate); architect for the technical approach. The CEO answers Q-001 to Q-004.
- Gate: Scope Gate of implement-feature; owners product-owner and business-analyst, with the producer exclusion applying, so the decision on this artifact rests with business-analyst.
- Evidence for the gate: S-001 to S-018, X-001 to X-011, A-001 to A-030, D-001 to D-006 and Q-001 to Q-004 in this artifact. Status provisional with verdict partially-bounded; Q-001 and Q-002 are blocking and stand. No gate decision and no readiness claim are recorded here.
- Deferred to downstream: decomposition and sequencing (planner); the web server, the browser recording and its conversion, the request-origin check, the one-time set-up, the timing editor's mechanics and the ledger interaction (architect); the test strategy and validation plan (qa); the Scope Gate decision, and the answers to Q-001 to Q-004 (business-analyst and the CEO).
