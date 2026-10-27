```yaml
plan:
  planId: PLAN-run-022d0492a75a-MC-17
  sourceInputs:
    - type: feature-request
      reference: runs/inputs/mc-17-feature-request.md
  producedBy: planner
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  inputDigest: sha256:a14fe9e6638fa76cbc0a7ce39f5550fd
  contextDigest: sha256:2de1c2c2ba837051eb22fa9c512b030c
  upstreamArtifacts:
    - scope-definition: runs/run-022d0492a75a/states/scope-and-acceptance/artifacts/scope-definition.md (sha256:1df5443872bc4d8fa40dc575de03fe03), approved at the Scope Gate as CEO-D-1310
  citationConventions:
    - S-001 to S-018 are the scope-definition's approved scope items, adopted verbatim as statements; S-019 to S-021 are added below
    - SC-01 to SC-30 cite the scope-definition's acceptance criteria 1 to 30, in that artifact's table order
    - X-001 to X-011 cite the scope-definition's exclusions; the scope phase's four questions are cited by their answers, CEO-D-1310 items 1 to 4
  affectedAreas: security (declared; S-014, S-015); performance (one bound only, the 1 s live check of SC-04, A-011); database (not affected, X-009, A-010)
  repositoryContext: task context named no modules; the orchestrator briefing named src/MediaCompany.Pilot, so its file list was read once to confirm the path exists; no wider scan
```

## Executive Summary

MC-17 puts a Vietnamese local web front end on the delivered MC-16 pilot toolchain so that a non-technical operator (the CEO now, an employee later) can make one pilot video step by step, record the narration in the browser, and have the CEO approve it by hash, without editing a data file or typing a command. Success is measured by one condition only (CEO-D-1310 item 4): on a code-generated sample the operator reaches steps 1 to 7 in the installed browser, including the record step, and the verify line shows the approved SHA-256. The work is 28 tasks in 17 execution waves: three design-phase tasks, seventeen implementation tasks, six quality-review tasks and two handoff tasks. The toolchain keeps every decision; the front end adds no pass path and the toolchain's source, checks, settings values and 251 tests stay unchanged. The highest-impact risk is R-014: a foreign web page open in the same browser could drive a production or an approval through the loopback address, which T-020 prevents and T-019 verifies. Plan status is complete; no blocking question stands, and the three non-blocking questions Q-001 to Q-003 are owned.

## Business Objectives

- BO-1: A non-technical operator produces one pilot video end to end through guided Vietnamese steps, with no data file edited and no command typed after the one-time set-up. Receiver: the operator. Measured by SC-29 (the success measure) and SC-30. Traces S-001 to S-013, S-018, S-019.
- BO-2: Every video stays the CEO's individual approval, given in person or by written instruction, and nothing is published by the app. Receiver: the CEO. Measured by SC-18 and SC-19. Traces S-011, S-012, S-020.
- BO-3: The pilot's compliance checks (banned terms, 60 s limit, recording reuse, clip tolerance, safe area, caption page, approval by hash) keep deciding each video exactly as delivered. Receiver: the CEO, who carries the legal position. Measured by SC-25 and SC-26. Traces S-002, S-006, S-016.
- BO-4: The operator's computer gains no exposure beyond one loopback listener: nothing downloaded, nothing sent off the machine, no request from another page acted on. Receiver: the CEO. Measured by SC-21 to SC-24. Traces S-014, S-015.

## Technical Objectives

- TO-1: Every outcome the front end shows (fact sheet check, banned terms, script, admission, production, timing, approval, verify) equals the toolchain's result on the same input. Verified by SC-25. Traces BO-3.
- TO-2: The front end listens on 127.0.0.1 only and opens no outbound connection; the toolchain and the media tool processes it starts own zero sockets. Verified by SC-21, SC-22. Traces BO-4.
- TO-3: A state-changing request (produce, timing save, approval) is accepted only from the app's own page on this computer. Verified by SC-24. Traces BO-4, BO-2.
- TO-4: The change adds zero package references and the pages reference zero external resources. Verified by SC-23. Traces BO-4.
- TO-5: The toolchain's source, checks, limits, settings values and tests are unchanged, and its 251 tests pass. Verified by SC-26. Traces BO-3.
- TO-6: Every screen and refusal message is Vietnamese under the SC-20 rule. Verified by SC-20. Traces BO-1.
- TO-7: The automated tests and the sample run need no microphone, no network and no media file in the repository. Verified by SC-27, SC-28. Traces BO-1, BO-3.
- TO-8: The first-run set-up writes a settings file the toolchain accepts, with the CEO-fixed numbers written as decided and not editable. Verified by T-026 acceptance and SC-30. Traces BO-1, BO-3.

## Scope

### In Scope

- S-001 to S-018: the scope-definition's eighteen approved items (fact sheet form, live banned terms, images, script ticks, browser recording, recording admission, presenter clip, production, timing editor, preview and checklist, approval by hash, verify and post path, Vietnamese screens, local-only operation, origin-restricted state changes, toolchain decides, offline tests, end-to-end sample run). Tasks: see Task Breakdown traces.
- S-019: On first run a Vietnamese screen asks for the pilot folder, finds ffmpeg, ffprobe and the typeface, computes their SHA-256 and writes the settings file; the CEO-D-1308 and CEO-D-1309 numbers are written as decided and are not editable (CEO-D-1310 item 2). Task: T-026.
- S-020: An employee operator may press approve on the CEO's written instruction, and the CEO's name is recorded in the approval (CEO-D-1310 item 3). Tasks: T-014, T-015.
- S-021: The change is committed and pushed on the session branch, and the CEO receives the compare link against main stating that the branch is cut from the unmerged MC-16 branch (feature request, done means). Tasks: T-027, T-028.

### Out of Scope

- X-001 publishing and account actions: agents never publish or act on accounts.
- X-002 generating the presenter clip and any voice, speech, text-to-speech, cloning, language-model or metered service: CEO-D-1304, CEO-D-1305, CEO-D-1307.
- X-003 reading product pages or platform data: facts come only from the operator.
- X-004 hand-editing script sentences: the tool writes the script (CEO-D-1306 item 1).
- X-005 accounts, multiple users, access from another device or address: one operator on one computer.
- X-006 changes to the toolchain's checks, limits, refusal rules, settings values or MC-16 known issues: the front end weakens nothing.
- X-007 editing the CEO-fixed settings numbers from the app: CEO-D-1310 item 2 writes them as decided.
- X-008 batch, multi-video, multi-presenter or non-Vietnamese production: pilot scope.
- X-009 frozen Wave 12 and Wave 13 material, the company store database and its container host: frozen by CEO decision.
- X-010 merging any branch into main and any release or closure decision: owned by gate owners and the CEO.
- X-011 kit 04 item B7 rewording and the open kit 03 edits: outside the app.

### Deferred

- Recording the employee's own name or a reference to the CEO's written instruction in the approval record: in scope when Q-001 is answered asking for it.
- A Vietnamese operator guide for the web app replacing the command-line steps of the MC-16 guide: in scope on a product-owner decision.
- A business measure beyond SC-29, such as minutes from fact sheet to approved video: in scope when the CEO states a measure with a threshold.

## Assumptions

| ID | Assumption | Basis | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | The front end builds and runs on the installed .NET 10 SDK and ASP.NET Core 10 shared framework with zero added package references (an orchestrator build on this machine used zero packages) | S-014 | A package needs the CEO's approval by name before T-017 can start | architect |
| A-002 | The installed ffmpeg converts a browser take into a toolchain-accepted format deterministically (the orchestrator measured identical SHA-256 over two conversions) | S-006 | The admitted digest is unstable, SC-11 fails, and T-009 is redesigned | architect |
| A-003 | The front end sits in its own assembly outside the toolchain's network-guarded assembly closure, so the toolchain's guard, source and 251 tests stay unchanged | S-016 | The guard or tests must change, which X-006 excludes; a CEO change request is needed | architect |
| A-004 | The first-run set-up writes the settings file in the format the toolchain already reads, and the toolchain's own validation still refuses values looser than the CEO's | S-019 | A toolchain settings change is needed, which X-006 excludes; T-026 changes | architect |
| A-005 | An approval pressed by an employee records the CEO's name through the toolchain's existing approver-name field; the app records no further identity unless Q-001 asks for it | S-020 | The approval record changes, possibly inside the toolchain (X-006); T-015 changes | product-owner |
| A-006 | Starting the app needs no typed command line; any launcher is prepared within the one-time set-up | S-018 | SC-30 fails; a start-up task is added | architect |
| A-007 | The recording flow can be tested with a code-generated audio stream and no microphone, using the installed browser only, with no package downloaded and no network connection | S-017 | SC-28 needs a CEO-approved download or a manual evidence route (Q-002); T-024 changes | qa |
| A-008 | A complete one-video sample (fact sheet entries, three images, screenshots, a narration take with sentence pauses, a portrait clip) can be generated by code with installed tools, adding no media file to the repository | S-018 | The sample run needs external media; T-023 is redesigned | qa |
| A-009 | The Vietnamese reason and next step for each of the fourteen refusal codes come from the MC-16 operator guide's refusal table, with the toolchain's line shown unchanged beside them | S-008 | A wording task for product-owner is added before T-011 | product-owner |
| A-010 | The triggered database and performance areas need no work beyond the SC-04 bound: the company store is untouched and no other performance expectation is stated | plan-wide | Database or performance design and test tasks are added | architect |
| A-011 | The toolchain's banned-term check, reached from the front end, answers within 1 s of the last keystroke | S-002 | Design work on the live check is added; SC-04 fails | architect |

## Risks

| ID | Class | Trigger | Impact | Likelihood | Affects | Mitigation | Owner |
|---|---|---|---|---|---|---|---|
| R-001 | requirement | The CEO's answer to Q-001 is pending at T-015 or asks for more than the CEO's name in the approval | T-015 rework; a toolchain record change would need a CEO change request (X-006) | medium | T-014, T-015 | T-014 obtains the answer before T-015 | product-owner |
| R-002 | requirement | Starting the app requires a typed command (A-006 false) | SC-30 fails and BO-1 is unmet | medium | T-001, T-017, T-025 | T-001 designs start-up; T-025 verifies it | architect |
| R-003 | requirement | The refusal table lacks a Vietnamese reason or next step for one of the fourteen codes (A-009 false) | T-011 waits on new wording | low | T-011 | T-001 maps each code to its table row; gaps go to product-owner | product-owner |
| R-004 | technical | A needed capability is absent from the installed shared framework (A-001 false) | T-017 waits on a CEO package approval | low | T-001, T-017 | T-001 records the no-package design | architect |
| R-005 | technical | Browser-take conversion is not deterministic or not accepted by the toolchain (A-002 false) | Reuse refusal by digest is unreliable; SC-11 fails | low | T-009 | T-001 confirms A-002; T-004 verifies SC-11 | architect |
| R-006 | technical | The front end enters the toolchain's guarded closure or needs a toolchain change (A-003 or A-004 false) | X-006 conflict; toolchain tests at risk | low | T-001, T-018, T-026, T-022 | T-001 confirms the boundary; T-022 verifies SC-26 | architect |
| R-007 | technical | The live banned-term result arrives later than 1 s after the last keystroke (A-011 false) | SC-04 fails | medium | T-005 | T-001 designs the live check path; T-004 measures it | architect |
| R-008 | technical | The no-microphone recording test needs a downloaded automation package (A-007 false) | SC-28 cannot be met without a CEO decision | medium | T-002, T-024 | T-002 names the approach or raises Q-002 | qa |
| R-009 | technical | Part of the sample cannot be generated by code (A-008 false) | The sample run and SC-29 are held | low | T-023, T-025 | T-002 names every fixture and its generator | qa |
| R-010 | technical | The design needs persistence beyond pilot-folder files or touches the company store (A-010 false) | Database design and gate work added; X-009 conflict | low | T-001 | T-001 states the persistence boundary | architect |
| R-011 | dependency | The unmerged MC-16 branch changes after this branch is cut | Rebase and a re-run of the toolchain tests | low | plan-wide | T-022 re-runs the 251 tests on the final base; T-028 states the base | orchestrator |
| R-012 | dependency | The CEO's Design Gate decision is delayed or declines the design | All implementation waits or returns to design | low | T-017, T-023 | T-001 maps every SC to a design element for the decision | orchestrator |
| R-013 | dependency | A required installed tool (ffmpeg, ffprobe, typeface, browser with MediaRecorder) is missing or its digest changes | Set-up or the sample run stops | low | T-026, T-025 | T-026 names a missing tool in Vietnamese | architect |
| R-014 | security | A foreign page open in the same browser sends a produce, timing or approval request to the loopback app | An unapproved production or approval, against D-002 of the decision record | medium | T-020, T-011, T-012, T-015 | T-020 refuses foreign origins; T-019 verifies SC-24 | architect |
| R-015 | security | The listener binds beyond 127.0.0.1, or the app or a job process opens an outbound connection | CEO-D-1310 item 1 breached | low | T-017, T-018 | T-019 verifies SC-21, SC-22 | architect |
| R-016 | security | An uploaded file name carries path segments | A write outside the pilot folder | medium | T-003, T-006, T-009, T-010 | T-001 defines the upload trust boundary; T-019 and T-021 check it | architect |
| R-017 | operational | An employee approves without the CEO's written instruction | A publication without the CEO's approval | low | T-015 | T-015 states the rule on the approval screen; residual risk accepted by the CEO | product-owner |
| R-018 | delivery | The compare link against main also shows the unmerged MC-16 commits | Review of MC-17 is confused with MC-16 | high | T-028 | T-028 states the branch base in the message | orchestrator |

## Task Breakdown

### T-001 Produce the MC-17 technical design package

- Owner: architect
- Complexity: L (confidence: medium)
- Depends on: none
- Traces to: S-001, S-005, S-006, S-014, S-015, S-016, S-019, S-020
- Status: ready
- Description: A technical design exists that states the front end's boundary with the delivered toolchain, its network exposure, its request-origin rule, the browser recording capture and its one conversion, the first-run set-up, start-up without a typed command, the upload trust boundary, the persistence boundary and the approval record content.
- Acceptance Criteria:
  - A-001, A-002, A-003, A-004, A-006, A-010 and A-011 are each recorded as confirmed, or replaced by a decision record at status Proposed
  - Each of SC-01 to SC-30 maps to at least one design element
  - The design adds zero package references, or names each one for a CEO decision
  - The design changes no toolchain source, check, limit, settings value or test (X-006)
- Gate: Design Gate

### T-002 Produce the MC-17 validation plan

- Owner: qa
- Complexity: M (confidence: medium)
- Depends on: T-001
- Traces to: S-001, S-002, S-003, S-004, S-005, S-006, S-007, S-008, S-009, S-010, S-011, S-012, S-013, S-014, S-015, S-016, S-017, S-018
- Status: ready
- Description: A validation plan maps each of SC-01 to SC-30 to one test level and one evidence item, with every fixture produced by code.
- Acceptance Criteria:
  - 30 of 30 criteria carry a test level and an evidence item
  - The recording-flow test needs no microphone, no network connection and no downloaded package (A-007 confirmed), or Q-002 is raised naming the package
  - Every fixture the plan names is produced by code, and A-008 is recorded as confirmed or not
- Gate: Design Gate

### T-003 Build the fact-sheet form step

- Owner: dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-018
- Traces to: S-001, S-013
- Status: ready
- Description: The operator enters every header and per-row field of the fact sheet and attaches each row's screenshot in a Vietnamese form, the toolchain's fact sheet check decides, and no data file is edited by hand.
- Acceptance Criteria:
  - SC-01, SC-02 and SC-03 pass in the change's automated tests
  - Labels and messages of the step meet the SC-20 rule
- Gate: Review Gate

### T-004 Verify the step-screen acceptance criteria

- Owner: qa
- Complexity: L (confidence: medium)
- Depends on: T-005, T-012, T-016
- Traces to: S-001, S-002, S-003, S-004, S-005, S-006, S-007, S-008, S-009, S-010, S-011, S-012, S-013
- Status: ready
- Description: A verification record states pass or fail, with evidence, for each of SC-01 to SC-20 on the built app.
- Acceptance Criteria:
  - 20 of 20 criteria have a recorded result and the evidence item T-002 names
  - SC-04 records the measured time from last keystroke to the shown term
  - Every fail names the task that owns the correction
- Gate: Verification Gate

### T-005 Show banned terms live in the form

- Owner: dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-003
- Traces to: S-002, A-011
- Status: assumption-dependent
- Description: As the operator types a fact value or an override, and on each script sentence, the form names every banned term with its class as the toolchain's own check and list decide, and the page holds no copy of the list.
- Acceptance Criteria:
  - SC-04 and SC-05 pass in the change's automated tests
  - The resources the app serves contain no copy of the banned-term list
- Gate: Review Gate

### T-006 Build the product images step

- Owner: dev-1-implement
- Complexity: S (confidence: high)
- Depends on: T-003
- Traces to: S-003, S-013
- Status: ready
- Description: The operator attaches exactly the three images named in the fact sheet, and the step names any missing or unnamed file before production is offered.
- Acceptance Criteria:
  - SC-06 passes in the change's automated tests
  - Labels and messages of the step, including the missing-file and unnamed-file messages, meet the SC-20 rule
- Gate: Review Gate

### T-007 Build the script review step

- Owner: dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-003
- Traces to: S-004, S-013
- Status: ready
- Description: The toolchain's generated script appears sentence by sentence in its order with each source line, and the next step opens only when every source line is ticked; no sentence is editable (X-004).
- Acceptance Criteria:
  - SC-07 passes in the change's automated tests
  - The step offers zero controls that edit a sentence
- Gate: Review Gate

### T-008 Build the in-browser narration recording step

- Owner: dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-007
- Traces to: S-005, S-013
- Status: ready
- Description: The operator records the narration in the installed browser with a visible timer; the take stops by itself at 60.0 s, plays back from start to end, and a re-record replaces it; upload stays an option, and no voice is generated.
- Acceptance Criteria:
  - SC-08 and SC-09 pass in the change's automated tests
  - The change contains zero calls to a speech, voice or generative service (X-002)
- Gate: Review Gate

### T-009 Admit the narration recording to the toolchain

- Owner: dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-008
- Traces to: S-006, A-002
- Status: assumption-dependent
- Description: An uploaded recording reaches the toolchain byte for byte; a browser take is converted once into a toolchain-accepted format; the toolchain's reuse check decides on the admitted file's digest.
- Acceptance Criteria:
  - SC-10 and SC-11 pass in the change's automated tests
  - No uploaded file is converted in any test case
- Gate: Review Gate

### T-010 Build the presenter clip step

- Owner: dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-009
- Traces to: S-007, S-013
- Status: ready
- Description: The operator attaches the presenter clip, and the step shows the toolchain's portrait result and the length difference from the admitted recording in seconds.
- Acceptance Criteria:
  - SC-12 passes in the change's automated tests
  - The tolerance applied is the toolchain's 0.5 s setting; the app holds no tolerance value of its own
- Gate: Review Gate

### T-011 Run production with Vietnamese outcomes

- Owner: dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-006, T-010, T-020
- Traces to: S-008, S-013, A-009
- Status: assumption-dependent
- Description: The operator starts production, sees progress until the toolchain ends, and sees the toolchain's line unchanged with a Vietnamese reason and next step, for success and for each of the fourteen refusal codes.
- Acceptance Criteria:
  - SC-13 passes in 15 of 15 cases
  - A produce request is accepted only under T-020's rule
- Gate: Review Gate

### T-012 Build the manual timing editor

- Owner: dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-011
- Traces to: S-009, S-013
- Status: ready
- Description: When production stops for pauses not matching the sentence count, an editor with one start and one end per sentence opens, and the toolchain checks what the operator enters.
- Acceptance Criteria:
  - SC-14 passes in the change's automated tests
  - A timing save is accepted only under T-020's rule
- Gate: Review Gate

### T-013 Build the preview and kit checklist step

- Owner: dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-011
- Traces to: S-010, S-013
- Status: ready
- Description: The produced video plays at 9:16 from the package's video file beside its frame image and SHA-256, with the 27-item kit 04 checklist showing B8 pre-ticked and B3, B5 and the other 24 items unticked.
- Acceptance Criteria:
  - SC-15 and SC-16 pass
  - Labels and messages of the step, including the checklist item texts, meet the SC-20 rule
- Gate: Review Gate

### T-014 Obtain the CEO's answer on the approval record content

- Owner: product-owner
- Complexity: XS (confidence: high)
- Depends on: none
- Traces to: S-011, S-020
- Status: ready
- Description: Q-001 is closed by a recorded CEO answer, or is recorded as accepted with A-005 standing.
- Acceptance Criteria:
  - The answer states which names and references an employee-pressed approval records
  - The answer is cited by its decision record entry identifier
- Gate: Design Gate

### T-015 Build approval by hash

- Owner: dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-013, T-014
- Traces to: S-011, S-020, A-005
- Status: assumption-dependent
- Description: Approval is offered only with 26 of 26 operator items ticked; it sends the SHA-256 shown beside the preview with the CEO's name as approver, also when an employee presses approve on the CEO's written instruction; the toolchain recomputes and decides.
- Acceptance Criteria:
  - SC-17 and SC-18 pass
  - Every approval record the tests produce names the CEO
  - The approval screen states in Vietnamese that approval is the CEO's, in person or by written instruction
- Gate: Review Gate

### T-016 Show the verify result and the file to post

- Owner: dev-1-implement
- Complexity: S (confidence: high)
- Depends on: T-015
- Traces to: S-012, S-013
- Status: ready
- Description: After approval the page shows the toolchain's verify line with the approved SHA-256 and approver, and the path of the video file to post by hand, with no action that sends the file anywhere.
- Acceptance Criteria:
  - SC-19 passes, with zero send controls and zero send routes
  - Labels and messages of the step meet the SC-20 rule, with the toolchain's verify line shown unchanged beside its Vietnamese label
- Gate: Review Gate

### T-017 Stand up the loopback-only front end

- Owner: dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-001, T-002
- Traces to: S-013, S-014, A-001, A-003, A-006
- Status: assumption-dependent
- Description: The front end starts on the operator's computer without a typed command, listens on 127.0.0.1 only, opens in the installed browser on a Vietnamese home screen, loads nothing from another address, and adds no package reference.
- Acceptance Criteria:
  - The listener address list contains 127.0.0.1 only
  - Zero package references added against the prior version (SC-23)
  - Zero external resource addresses in the served pages
  - The toolchain's network guard and assembly closure are unchanged
- Gate: Review Gate

### T-018 Connect the front end to the delivered toolchain

- Owner: dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-026
- Traces to: S-014, S-016, A-003
- Status: assumption-dependent
- Description: Each decision the front end shows comes from the delivered toolchain's check, script, produce, approve and verify verbs or its banned-term and admission checks, with result line and exit status passed through unchanged, and the processes it starts own zero sockets.
- Acceptance Criteria:
  - Zero toolchain source or test files changed in the diff
  - Socket count of the started job processes is zero under the toolchain's socket harness (SC-22)
  - For each verb, the shown result line equals the printed line
- Gate: Review Gate

### T-019 Verify network exposure and request-origin refusal

- Owner: qa
- Complexity: M (confidence: medium)
- Depends on: T-012, T-016, T-023
- Traces to: S-014, S-015
- Status: ready
- Description: A verification record states the result, with evidence, for SC-21 to SC-24 during the sample run.
- Acceptance Criteria:
  - SC-21 to SC-24 each have a recorded result: socket harness record with its sampling interval, manifest comparison, request log, pilot-folder hashes before and after
  - An upload whose name carries path segments writes nothing outside the pilot folder (R-016)
- Gate: Verification Gate

### T-020 Refuse state-changing requests from other origins

- Owner: dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-017
- Traces to: S-015
- Status: ready
- Description: Produce, timing save and approval requests are accepted only from the app's own page on this computer; any other is refused and changes no file.
- Acceptance Criteria:
  - The six foreign requests of SC-24 per kind are refused in the change's automated tests
  - Pilot-folder file hashes are equal before and after the refused requests
- Gate: Review Gate

### T-021 Review the MC-17 change

- Owner: dev-2-reviewer
- Complexity: L (confidence: medium)
- Depends on: T-005, T-012, T-016, T-023
- Traces to: S-014, S-015, S-016
- Status: ready
- Description: A review package records severity-classified findings and a verdict for the whole change, including the security review of the loopback exposure and the upload trust boundary.
- Acceptance Criteria:
  - Every changed file is covered by the review
  - Every finding carries a severity
  - The review records whether any toolchain check, limit or setting is weakened
  - A verdict is recorded
- Gate: Review Gate

### T-022 Verify the toolchain keeps deciding

- Owner: qa
- Complexity: M (confidence: medium)
- Depends on: T-016
- Traces to: S-016
- Status: ready
- Description: A verification record shows the app's result equals the toolchain's on every refusal and approval fixture, and the toolchain's tests pass unchanged on the final branch base.
- Acceptance Criteria:
  - SC-25 shows zero differences
  - SC-26 shows 251 passing, and the toolchain test folder shows additions only
- Gate: Verification Gate

### T-023 Generate the code-built pilot sample

- Owner: dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-002
- Traces to: S-017, S-018, A-008
- Status: assumption-dependent
- Description: Code produces a complete one-video sample outside the repository: fact sheet entries, three images, screenshots, a narration take with sentence pauses and a portrait clip within tolerance.
- Acceptance Criteria:
  - The sample is produced by code on a machine with no network connection
  - Zero media files are added to the repository (SC-27)
- Gate: Review Gate

### T-024 Verify the tests run offline without a microphone

- Owner: qa
- Complexity: M (confidence: low)
- Depends on: T-005, T-012, T-016, T-023
- Traces to: S-017, A-007
- Status: assumption-dependent
- Description: A verification record states the result, with evidence, for SC-27 and SC-28.
- Acceptance Criteria:
  - SC-27: scan of added files finds zero media files, and each fixture is produced by test code
  - SC-28: the recording-flow test passes on a machine with no microphone, with socket count zero
- Gate: Verification Gate

### T-025 Run the sample end to end in the browser

- Owner: qa
- Complexity: M (confidence: low)
- Depends on: T-005, T-012, T-016, T-023
- Traces to: S-018, A-006
- Status: assumption-dependent
- Description: A recorded browser run on the code-built sample reaches steps 1 to 7, including the record step and the first-run set-up, and the verify line shows the approved SHA-256.
- Acceptance Criteria:
  - SC-29: seven of seven steps reached; approved and verified SHA-256 equal
  - SC-30: the walkthrough log shows zero command lines typed and zero JSON files edited, start-up included
- Gate: Verification Gate

### T-026 Build the first-run setup screen

- Owner: dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-017
- Traces to: S-019, A-004
- Status: assumption-dependent
- Description: On first run a Vietnamese screen takes the pilot folder, finds ffmpeg, ffprobe and the typeface, computes their SHA-256 and writes a settings file the toolchain accepts, with the CEO-fixed numbers written as decided.
- Acceptance Criteria:
  - The written settings file passes the toolchain's own settings validation
  - The screen has zero input controls for safe area, caption size, overlay size, clip tolerance or caption page minimum (X-007)
  - A missing tool is named in Vietnamese
  - A pilot folder inside the repository is refused
- Gate: Review Gate

### T-027 Write the MC-17 release note

- Owner: documentation
- Complexity: S (confidence: high)
- Depends on: T-004, T-019, T-021, T-022, T-024, T-025
- Traces to: S-021
- Status: ready
- Description: A release note records each SC result with its evidence, the known issues, the residual risks with owners, and the branch base.
- Acceptance Criteria:
  - 30 of 30 SC results are cited with their evidence
  - Every open risk is listed with its owner
  - The note states that the branch is cut from the unmerged MC-16 branch
- Gate: Closure Gate

### T-028 Deliver the compare link to the CEO

- Owner: orchestrator
- Complexity: XS (confidence: high)
- Depends on: T-027
- Traces to: S-021
- Status: ready
- Description: The change is pushed on the session branch, and the CEO holds the compare link against main with the branch base stated; nothing is merged (X-010).
- Acceptance Criteria:
  - The compare link is delivered to the CEO
  - The message states that the base is the unmerged MC-16 branch
  - The main branch is unchanged by this run
- Gate: Closure Gate

## Dependencies

### 8.1 Dependency Edges

| From | To | Type | Justification |
|---|---|---|---|
| CEO (owner of the unmerged MC-16 branch) | T-001 | external | The design binds to the delivered toolchain as it stands on that branch |
| T-001 | T-002 | produces-consumes | The validation plan maps the design's elements to tests |
| T-001 | T-017 | produces-consumes | The front end realizes the design |
| T-002 | T-017 | contract | The build's tests bind to the validation plan |
| CEO (Design Gate decision) | T-017 | policy-gate | Implementation starts only after the Design Gate |
| T-002 | T-023 | produces-consumes | The sample realizes the fixtures the plan names |
| CEO (Design Gate decision) | T-023 | policy-gate | Implementation starts only after the Design Gate |
| CEO (answer to Q-001) | T-014 | external | Only the CEO can answer Q-001 |
| T-017 | T-020 | contract | The origin rule guards the front end's requests |
| T-017 | T-026 | produces-consumes | The set-up screen is served by the front end |
| CEO (operator machine installs) | T-026 | external | The set-up finds the installed media tools and typeface |
| T-026 | T-018 | produces-consumes | The toolchain reads the settings file the set-up writes |
| T-018 | T-003 | contract | The form's check is the toolchain's fact sheet check |
| T-003 | T-005 | produces-consumes | The live check runs on values typed in the form |
| T-003 | T-006 | produces-consumes | The image step reads the fact sheet's images-used field |
| T-003 | T-007 | produces-consumes | The script is generated from the entered fact sheet |
| T-007 | T-008 | produces-consumes | The operator records the ticked script |
| T-008 | T-009 | produces-consumes | Admission takes the browser take |
| T-009 | T-010 | produces-consumes | The clip check needs the admitted recording's length |
| T-006 | T-011 | produces-consumes | Production needs the three images |
| T-010 | T-011 | produces-consumes | Production needs the admitted clip |
| T-020 | T-011 | contract | The produce request is guarded by the origin rule |
| T-011 | T-012 | produces-consumes | The editor opens on production's pause-mismatch stop |
| T-011 | T-013 | produces-consumes | Preview shows the produced package |
| T-013 | T-015 | produces-consumes | Approval sends the SHA-256 shown beside the preview |
| T-014 | T-015 | decision-gate | The approval record content depends on the CEO's answer |
| T-015 | T-016 | produces-consumes | Verify runs on the approved video |
| T-005 | T-004 | verification | T-004 verifies SC-04 and SC-05 |
| T-012 | T-004 | verification | T-004 verifies SC-14 |
| T-016 | T-004 | verification | T-004 verifies SC-19 and the steps before it |
| T-012 | T-019 | verification | SC-24 covers timing save |
| T-016 | T-019 | verification | SC-21 to SC-24 run on the complete app |
| T-023 | T-019 | produces-consumes | SC-21 and SC-22 are measured during the sample run |
| T-005 | T-021 | verification | The review covers the live check |
| T-012 | T-021 | verification | The review covers the timing editor |
| T-016 | T-021 | verification | The review covers the complete step flow |
| T-023 | T-021 | verification | The review covers the sample generator |
| T-016 | T-022 | verification | SC-25 runs refusal and approval fixtures through the complete app |
| T-005 | T-024 | verification | SC-27 scans the complete change |
| T-012 | T-024 | verification | SC-27 scans the complete change |
| T-016 | T-024 | verification | SC-28 tests the record flow within the complete app |
| T-023 | T-024 | produces-consumes | SC-27 inspects the code-built fixtures |
| T-005 | T-025 | verification | The sample run passes through step 1's live check |
| T-012 | T-025 | verification | The sample run may stop at the timing editor |
| T-016 | T-025 | verification | The sample run ends at verify |
| T-023 | T-025 | produces-consumes | The run uses the code-built sample |
| CEO (operator machine installs) | T-025 | external | The run uses the installed browser and media tools |
| T-004 | T-027 | produces-consumes | The note cites SC-01 to SC-20 results |
| T-019 | T-027 | produces-consumes | The note cites SC-21 to SC-24 results |
| T-021 | T-027 | produces-consumes | The note cites the review verdict |
| T-022 | T-027 | produces-consumes | The note cites SC-25 and SC-26 results |
| T-024 | T-027 | produces-consumes | The note cites SC-27 and SC-28 results |
| T-025 | T-027 | produces-consumes | The note cites SC-29 and SC-30 results |
| T-027 | T-028 | produces-consumes | The pushed branch carries the release note |

### 8.2 External Dependencies

| Responsible party | What is needed | Blocks |
|---|---|---|
| CEO (owner of the unmerged MC-16 branch) | The delivered toolchain unchanged on that branch during MC-17 (R-011) | T-001 |
| CEO (Design Gate decision) | Approval of the T-001 design (R-012) | T-017, T-023 |
| CEO (answer to Q-001) | Approval record content for an employee-pressed approval (R-001) | T-014 |
| CEO (operator machine installs) | As owner of the operator machine: installed .NET 10 SDK with ASP.NET Core 10, ffmpeg, ffprobe, the typeface and a browser with MediaRecorder (R-013) | T-026, T-025 |

### 8.3 Implementation Order

Waves are computed over the task-to-task edges of 8.1; an external edge holds its target task within that task's wave until the party in 8.2 delivers.

- Wave 1: T-001, T-014
- Wave 2: T-002
- Wave 3: T-017, T-023
- Wave 4: T-020, T-026
- Wave 5: T-018
- Wave 6: T-003
- Wave 7: T-005, T-006, T-007
- Wave 8: T-008
- Wave 9: T-009
- Wave 10: T-010
- Wave 11: T-011
- Wave 12: T-012, T-013
- Wave 13: T-015
- Wave 14: T-016
- Wave 15: T-004, T-019, T-021, T-022, T-024, T-025
- Wave 16: T-027
- Wave 17: T-028

## Suggested Workflow

Selected workflow: `implement-feature`

Selected because: MC-17 is new functionality (a front end on the delivered toolchain), routed as `/implement`; this plan recommends the workflow and does not start it.

| Phase | Tasks |
|---|---|
| `solution-design-and-risk-assessment` | T-001, T-002, T-014 |
| `implementation` | T-003, T-005, T-006, T-007, T-008, T-009, T-010, T-011, T-012, T-013, T-015, T-016, T-017, T-018, T-020, T-023, T-026 |
| `quality-review` | T-004, T-019, T-021, T-022, T-024, T-025 |
| `documentation-and-release-handoff` | T-027, T-028 |

| Gate | Required owners |
|---|---|
| Scope Gate | product-owner, business-analyst (decided: CEO-D-1310) |
| Planning Gate | tech-lead, orchestrator (this plan is its evidence) |
| Design Gate | architect, tech-lead; producer exclusion leaves tech-lead; the CEO decides per the feature request |
| Review Gate | dev-2-reviewer, qa |
| Verification Gate | qa (see Q-003) |
| Closure Gate | orchestrator, documentation |

## Required Capabilities

### 10.1 Agent Capabilities

| Capability | Tasks | Owning agent | Proficiency |
|---|---|---|---|
| technical-approach-definition | T-001 | architect | Primary |
| architecture-decision-authoring | T-001 | architect | Primary |
| validation-design | T-002 | qa | Primary |
| acceptance-authority | T-014 | product-owner | Primary |
| implementation-delivery | T-003, T-005, T-006, T-007, T-008, T-009, T-010, T-011, T-012, T-013, T-015, T-016, T-017, T-018, T-020, T-023, T-026 | dev-1-implement | Primary |
| quality-verification | T-004, T-019, T-022, T-024, T-025 | qa | Primary |
| code-review | T-021 | dev-2-reviewer | Primary |
| release-communication | T-027 | documentation | Primary |
| run-coordination | T-028 | orchestrator | Primary |

### 10.2 Required Skills

| Skill | File | Tasks | Level |
|---|---|---|---|
| S01 | skills/architecture/clean-architecture-checklist.md | T-001, T-021 | Primary |
| S02 | skills/business/domain-modeling.md | T-014 | Secondary |
| S03 | skills/dotnet/engineering-playbook.md | T-001, T-003, T-005, T-006, T-007, T-008, T-009, T-010, T-011, T-012, T-013, T-015, T-016, T-017, T-018, T-020, T-023, T-026 | Primary |
| S06 | skills/database/database-engineering.md | T-001 | Advisory |
| S07 | skills/testing/testing-strategy.md | T-002, T-004, T-019, T-022, T-024, T-025 | Primary |
| S08 | skills/performance/performance-engineering.md | T-004 | Advisory |
| S09 | skills/security/secure-engineering.md | T-001, T-017, T-018, T-019, T-020, T-021 | Primary |
| S10 | skills/git/git-collaboration.md | T-027, T-028 | Secondary |
| S11 | skills/logging/observability-logging.md | T-011, T-027 | Advisory |
| S12 | skills/error-handling/error-handling-strategy.md | T-011, T-012 | Secondary |

## Acceptance Criteria

1. BO-1 (success measure, CEO-D-1310 item 4): on a code-generated sample the operator reaches steps 1 to 7 in the installed browser, including the record step, and the verify line shows the approved SHA-256 (SC-29). Evidence: the reviewer's recorded browser run with equal hashes.
2. BO-1: during the sample run the operator types no command line and edits no JSON file after the first-run set-up, start-up included (SC-30). Evidence: the walkthrough log.
3. BO-1: SC-01 to SC-20 each pass on the built app. Evidence: the step verification record with one evidence item per criterion.
4. BO-2: every approval names the CEO and is decided by the toolchain on the shown SHA-256; a one-byte change is refused; the app has no send control or route (SC-18, SC-19). Evidence: browser run, approval record count, route inspection.
5. BO-3: zero cases where the app shows a pass the toolchain refuses, and the 251 toolchain tests pass unchanged (SC-25, SC-26). Evidence: fixture comparison log and test run output.
6. BO-3: the tests run offline on code-built fixtures without a microphone, and no media file is added (SC-27, SC-28). Evidence: file scan and test run on a machine with no microphone.
7. BO-4: listener on 127.0.0.1 only, zero outbound connections, zero sockets in job processes, zero added packages and external resources, and foreign state-changing requests refused with the pilot folder unchanged (SC-21 to SC-24). Evidence: socket harness record, manifest comparison, request log and folder hashes.

## Definition of Done

- [ ] Plan acceptance criteria 1 to 7 are verified with recorded evidence
- [ ] Scope, Planning, Design, Review, Verification and Closure Gates are decided, with the deciding owner recorded for each
- [ ] Every task's acceptance criteria are satisfied, or formally waived with the waiver recorded
- [ ] A-001 to A-011 are each confirmed or converted to a recorded decision
- [ ] R-001 to R-018 are each closed or accepted with a named owner
- [ ] Q-001 to Q-003 are each closed or explicitly accepted with the owner recorded
- [ ] The release note is published in the run evidence
- [ ] Durable outcomes are recorded to memory per `memory/memory-governance.md`

## Open Questions

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| Q-001 | When an employee presses approve on the CEO's written instruction, does the approval record carry only the CEO's name, or also the employee's name and a reference to the instruction? | non-blocking | product-owner (the CEO answers) | T-014, T-015 |
| Q-002 | If the no-microphone recording-flow test (SC-28) cannot run without a downloaded browser-automation package, does the CEO approve that package by name, or accept a recorded manual run as the evidence? | non-blocking | product-owner (the CEO answers) | T-002, T-024 |
| Q-003 | Framework gap: the implement-feature Verification Gate names qa as its only owner, while qa produces the verification evidence (T-004, T-019, T-022, T-024, T-025); under the Producer Exclusion Rule, who decides it? | non-blocking | orchestrator | T-004, T-019, T-022, T-024, T-025 |
