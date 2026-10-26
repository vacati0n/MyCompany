```yaml
plan:
  planId: PLAN-MC16-run-535f3f938f32
  sourceInputs:
    - type: feature-request
      reference: runs/inputs/mc-16-feature-request.md
  producedBy: planner
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  inputDigest: sha256:04f86d51a3d9db84789afc446e0fec4b
  contextDigest: sha256:2de1c2c2ba837051eb22fa9c512b030c
  upstreamArtifact: runs/run-535f3f938f32/states/scope-and-acceptance/artifacts/scope-definition.md (sha256:30a501cb157980ab7ad14e038c3834d6, Scope Gate approved by the CEO, CEO-D-1307)
  citationRule: the approved scope's own identifiers are cited as "scope item n", "scope exclusion n", "scope criterion n" and "scope decision n"
```

## Executive Summary

This plan delivers the MC-16 toolchain that lets the operator (the CEO now, a human employee later) turn one pilot product into one ready-to-review 9:16 video package, which the CEO approves by hash and which the tool never publishes. Success is the approved scope's 40 criteria verified on a sample generated only by repository code, plus the Vietnamese operator guide and the kit files aligned with the CEO's decisions on the call to action and the operator's real voice. The work is 23 tasks in 13 execution waves: the technical design comes first, then the RK-016 part 2 vertical render proof and the deterministic pieces (fact sheet check, banned-term check, script generator, ledger, recording admission), then composition, packaging, the offline sample run, verification and closure. The highest-impact risk is R-004: if the delivered renderer cannot produce vertical output with correctly stacked Vietnamese diacritics, composition stops until a design change or a component the CEO approves by name. The plan status is complete, with no blocking open question.

## Business Objectives

- BO1. The operator gets one ready-to-review 9:16 video package per product without assembling it by hand; measured by a package produced end to end from the code-generated sample (S-001, S-007, S-029).
- BO2. The CEO's per-video review is reduced to the items only the CEO can attest, because sourced facts, banned content, disclosure and the AI-label overlay are machine-checked before review; measured by the refusal results and the checklist pre-fill (S-003, S-004, S-005, S-027).
- BO3. The pilot's standing commitments hold for each package: CEO approval by hash before any publication, no text-to-speech, no voice cloning, no reused recording, no generative model in the toolchain, and the pilot's video limits; measured by the approval, recording and ledger refusal results (S-009, S-011, S-012, S-015, S-026).
- BO4. Running the tool costs nothing and touches nothing frozen: no network call, no download, no metered call, no change to frozen Wave 12 or Wave 13 material or the company store; measured by the network-guard count and the change-set scan (S-010, S-013, S-014).
- BO5. The operator can run the tool from Vietnamese guidance and a kit that matches the tool and the CEO's decisions; measured by review of the guide and the kit files against the delivered tool (S-029, S-030, S-031).

## Technical Objectives

- TO1. Fact sheet acceptance covers the eleven header fields, the row fields, the screenshot files and banned classes (a) to (g), and each refusal names the row, field or term; verified by scope criteria 1 to 7 (traces BO2).
- TO2. Script generation is deterministic, in kit Part B order, with byte-exact opening and closing disclosures, a call to action from the template's fixed list, and one sourced Part E row per sentence; verified by scope criteria 8 to 14 (traces BO2, BO3).
- TO3. Composition starts only after the vertical-output and typeface-coverage pre-checks pass, and yields 1080x1920 output with image motion, the presenter clip, captions in their recorded windows and the overlay on screen throughout; verified by scope criteria 19 to 27 and 37 (traces BO1, BO2).
- TO4. A package is content-addressed: the video hash, six input digests and the provenance list recompute exactly, and an approval is void after a one-byte change; verified by scope criteria 28 to 30 and 33 (traces BO1, BO3).
- TO5. Admission refuses reused, overlong or multiple recordings, a landscape presenter clip, and ledger-limit breaches; verified by scope criteria 15 to 18, 24 and 38 to 40 (traces BO3).
- TO6. The toolchain and its tests run offline with no language-model, speech-recognition or speech-synthesis component, add no media file to the repository, and need neither the company store database nor a container service; verified by scope criteria 34 to 36 and the T-012 run record (traces BO4; A-005).

## Scope

### In Scope

- S-001 One product to one ready-to-review 9:16 video on the delivered pipeline: T-001, T-005.
- S-002 Per-video operator inputs (fact sheet, images, recording, presenter clip): T-002, T-011, T-020.
- S-003 Fact sheet check with banned content refused naming row and term: T-002, T-003.
- S-004 Deterministic script with verbatim disclosures and Part E source lines: T-004.
- S-005 9:16 composition with motion, presenter, burned-in captions and the disclosure overlay: T-005.
- S-006 RK-016 pre-check of vertical output and glyph coverage before composition: T-006, T-007.
- S-007 Output package with video hash, provenance, input digests and checklist: T-008.
- S-008 Reuse of the delivered renderer, coverage check, approval by hash and held-stage refusal: T-001, T-005, T-007, T-009.
- S-009 No account, sign-in, seller contact, purchase or publication by agents or the tool: T-009.
- S-010 No download without the CEO's approval by name: T-006, T-012.
- S-011 No generative model in the toolchain: T-004, T-010.
- S-012 No text-to-speech, no voice cloning, reused recording refused: T-011.
- S-013 Zero running cost, no metered call: T-012.
- S-014 Frozen Wave 12 and Wave 13 material and the company store untouched: T-014, T-018.
- S-015 The CEO approves each publication individually: T-009.
- S-016 The change runs through implement-feature with its gates: T-013.
- S-017 The tooling works and is tested: T-014.
- S-018 The test sample comes only from repository code, with no download: T-015.
- S-019 Committed and pushed on the session branch, compare link to the CEO: T-016.
- S-021 Fact sheet header and row completeness, screenshot file present (scope criteria 1 to 4): T-002, T-017.
- S-022 Recording admission rules (scope criteria 15 to 18): T-011, T-018.
- S-023 Caption windows from pause detection, manual timing fallback (scope criteria 26, 37): T-010.
- S-024 Call to action from a fixed no-price list replacing kit Part B row 6 (scope criterion 13): T-003, T-004, T-019.
- S-025 Landscape presenter clip refused (scope criterion 24): T-020.
- S-026 Ledger limits: 12 videos, two per product, distinct focus text (scope criteria 38 to 40): T-018.
- S-027 Checklist pre-fill of exactly 04 B3, B5, B8 (scope criteria 31, 32): T-021.
- S-028 Zero network attempts, no excluded components, no media files added (scope criteria 34 to 36): T-012, T-014.
- S-029 The operator runs the tool and may record in their own real voice (CEO-D-1307 item 2): T-022, T-023.
- S-030 Kit file 03 Part B row 6 and Part F, and kit file 04 item B13, match the tool and the CEO's decisions: T-019, T-022.
- S-031 Operator guide in Vietnamese: T-023.
- S-032 RK-016 part 2 proven by a rendered test (part 1 already measured): T-006.
- S-033 The .NET test run needs neither the company store database nor a container service: T-012, T-017.

### Out of Scope

- Publishing, scheduling, re-posting, sign-in, upload or account action on any service (scope exclusion 1): the CEO publishes; agents never act on accounts.
- Opening, scraping or downloading any product page or platform asset (scope exclusion 2): facts come only from the fact sheet; platform terms bar collection.
- Generating the presenter clip, the presenter tool and its account (scope exclusion 3): the only generative element, made by the CEO outside the toolchain.
- Text-to-speech, voice cloning and speech recognition (scope exclusion 4): barred by CEO-D-1304 and CEO-D-1305.
- Language-model text of any kind (scope exclusion 5): the generator is deterministic by the CEO's decision CEO-D-1306 item 1.
- Detecting or acting on a stop event (scope exclusion 6): an operational decision of the CEO.
- Changes to frozen Wave 12 or Wave 13 material or the company store database (scope exclusion 7): frozen by CEO decision.
- Checklist 04 items that are CEO facts or actions (scope exclusion 8): outside the tool's knowledge.
- Batch production, more than one presenter, non-Vietnamese captions (scope exclusion 9): pilot is one product, one presenter, Vietnamese.
- Presenter account management (scope exclusion 10): outside the repository.

### Deferred

- Detection of a re-encoded or spliced copy of a used recording: enters when the CEO approves a comparison component by name (scope decision 4 keeps digest equality only).
- A render-time or throughput target: enters when product-owner states one (A-007).
- A package field naming who recorded the narration: enters when the CEO requires it (A-008).

## Assumptions

| ID | Assumption | Basis | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | The delivered, manifest-verified renderer can emit 1080x1920 output with no new component or download | S-006, S-032 | T-005 waits on a design change or a component the CEO approves by name; Q-002 opens | architect (T-001), proven by T-006 |
| A-002 | The installed media tools can generate the sample media and measure pauses by signal level with no added component | S-018, S-023 | T-010 and T-015 need a new component and the CEO's approval by name | architect |
| A-003 | The sample's presenter clip is a code-generated portrait stand-in; scope criteria 23 and 24 are verified against it | S-011, S-018 | Verification of those criteria waits on a real clip from the CEO | qa |
| A-004 | Only the implementing role writes repository files in this run (orchestrator's briefing, framework defect 1), so the guide and kit changes are authored by dev-1-implement | S-020, S-031, S-034 | T-019, T-022 and T-023 move to documentation | orchestrator |
| A-005 | The MC-16 automated tests can run in test projects that need neither the company store database nor a container service | S-033 | T-012 and T-014 cannot produce evidence on this machine | architect |
| A-006 | The pilot ledger lives outside the company store database; the triggered "database" area is lexical and implies no schema work | S-014, S-026 | A ledger in the company store breaches the freeze; T-018 changes | architect |
| A-007 | The triggered "performance" area comes from the slow pan or zoom wording and implies no performance target | plan-wide | A stated target adds a measurement task | product-owner |
| A-008 | The tool neither records nor checks who recorded the narration (CEO-D-1307 item 2 allows the employee's real voice) | S-029 | T-008 gains an operator field | product-owner |
| A-009 | Pushing the session branch and sending the compare link are orchestrator closure actions after the Closure Gate | S-019 | T-016 changes owner | orchestrator |

## Risks

| ID | Class | Trigger | Impact | Likelihood | Affects | Mitigation | Owner |
|---|---|---|---|---|---|---|---|
| R-001 | requirement | The call-to-action list used by the generator differs from kit file 03 Part B row 6 | A script carries a sentence the template does not hold; scope criterion 13 fails | medium | T-004, T-019 | T-004 acceptance requires equality with the T-019 list; T-014 checks it | dev-1-implement |
| R-002 | requirement | A render-time target or a recorder-identity field is required after planning (A-007, A-008) | Added measurement task; T-008 package fields change | low | T-005, T-008 | Return to execution-planning per the workflow's recovery rule | product-owner |
| R-003 | requirement | The operator submits a re-encoded copy of a used recording | Digest equality passes it (scope decision 4) | low | T-011, T-021 | Checklist item B13 stays unticked for the CEO's attestation (T-021) | product-owner |
| R-004 | technical | T-006's probe is not 1080x1920, or its rendered frame mis-stacks a tone mark over a circumflex or breve (A-001) | T-005 blocked; design change or approved component needed | medium | T-005, T-006, T-007 | T-006 runs in wave 2, before composition; Q-002 to the CEO | architect |
| R-005 | technical | Detected pauses differ from sentence count minus one on a clean recording, or the installed tools cannot measure them (A-002) | Frequent manual timing; T-010 rework | medium | T-010, T-015 | Manual timing path (scope criterion 37) in T-010; threshold fixed in T-001 | architect |
| R-006 | technical | A string of scope criterion 7 gives the wrong result under whole-word, NFC matching | False refusals, or banned text reaching a script | medium | T-003, T-004 | T-003 acceptance covers scope criteria 5 and 7; T-017 maps them | dev-1-implement |
| R-007 | technical | The CEO's first real presenter clip fails a check the stand-in never exercised (A-003) | Defect found after handoff | low | T-014, T-015, T-020 | T-023 states the clip requirements; residual risk named in T-013 | qa |
| R-008 | dependency | T-006 finds a needed component absent from the installed set | T-005 waits on the CEO's approval by name | low | T-005 | Q-002 through the orchestrator; no download before approval | orchestrator |
| R-009 | dependency | A real operator input takes a form the code-generated sample did not cover | First real package refused or mis-built | medium | T-002, T-011, T-020, T-023 | Refusals name the failing field; T-023 documents input forms | dev-1-implement |
| R-010 | security | An operator-supplied file name or cell points outside the input set or carries control content | Reads or writes outside the package; corrupted render | low | T-002, T-005, T-010 | T-002 refuses names outside the input set; T-017 includes negative cases | dev-1-implement |
| R-011 | security | Video bytes change after an approval record is written | Unapproved bytes pass as approved | low | T-009 | T-009 refuses on hash mismatch (scope criterion 33) | dev-1-implement |
| R-012 | operational | The sample run opens a socket, a test needs the company store or a container service, or the change set touches frozen paths (A-005, A-006) | Evidence invalid; freeze breached | low | T-012, T-014, T-018 | T-012 network guard; T-014 change-set scan | qa |
| R-013 | delivery | One implementing agent carries 18 tasks, or a non-implementing role is asked to write a repository file (A-004) | Review load; owner changes | medium | plan-wide | Wave order with the spike first; T-014 verifies per criterion | tech-lead |
| R-014 | delivery | The orchestrator cannot push the session branch (A-009) | T-016 changes owner | low | T-016 | Reassign at the Closure Gate | orchestrator |

## Task Breakdown

### T-001 Settle the technical design for the MC-16 toolchain

- Owner: architect
- Complexity: L (confidence: medium)
- Depends on: none
- Traces to: S-001, S-008, S-032
- Status: ready
- Description: Done when an approved technical design records a decision on each of these eleven items the scope deferred: (1) the machine-readable fact sheet form; (2) the fixed sentence frames; (3) the final call-to-action list; (4) where the banned-term classes are held; (5) the vertical render route on the delivered renderer; (6) the character-coverage mechanism; (7) the pause-detection method and threshold; (8) the image motion; (9) the ledger location; (10) the network guard for the sample run; (11) the test project placement.
- Acceptance Criteria:
  - The design names one decision for each of the eleven items listed above
  - The design adds no component outside the installed set, or names each one for the CEO's approval
  - tech-lead records the Design Gate decision
- Gate: Design Gate

### T-002 Accept or refuse a product fact sheet

- Owner: dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-001, T-003
- Traces to: S-002, S-003, S-021
- Status: ready
- Description: Done when a fact sheet in the T-001 form is accepted only if its header, rows, sources and screenshot files are complete and free of banned content, and each refusal names the row, field or term.
- Acceptance Criteria:
  - Tests for scope criteria 1, 2, 3 and 4 pass
  - A fact row with a banned term is refused naming the row and the term (scope criterion 5, row case)
  - A screenshot name resolving outside the input set is refused
- Gate: Review Gate

### T-003 Provide the banned-term check for classes (a) to (g)

- Owner: dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-001
- Traces to: S-003, S-024
- Status: ready
- Description: Done when one check, shared by fact rows, operator overrides and script sentences, refuses terms of the seven classes of scope criterion 5 by whole-word, case-insensitive matching after NFC normalization, naming the term.
- Acceptance Criteria:
  - One example per class (a), (b), (c), (d), (e), (f), (g) is refused naming the term; a clean text passes
  - Each string of scope criterion 7 gives its stated result
  - A money amount is refused and the bare word "giá" passes (scope decision 8)
- Gate: Review Gate

### T-004 Generate the sourced script from an accepted fact sheet

- Owner: dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-002, T-003, T-019
- Traces to: S-004, S-011, S-024
- Status: ready
- Description: Done when the generator deterministically emits the kit Part B script and its Part E source lines from an accepted fact sheet, with no generative model.
- Acceptance Criteria:
  - Tests for scope criteria 6, 8, 9, 10, 11, 12, 13 and 14 pass
  - The call-to-action sentence used equals an entry of the kit file 03 Part B row 6 list delivered by T-019
  - The invoked-tool record of a generation run lists no language-model component
- Gate: Review Gate

### T-005 Compose the 9:16 video from admitted inputs

- Owner: dev-1-implement
- Complexity: L (confidence: low)
- Depends on: T-007, T-010, T-015, T-020
- Traces to: S-001, S-005, S-008, A-001
- Status: assumption-dependent
- Description: Done when the delivered renderer, with no second renderer (scope decision 1), produces the vertical video showing the product images in motion, the presenter clip, the captions in their windows and the disclosure overlay, in the caption style of scope decision 13 and the motion of scope decision 15.
- Acceptance Criteria:
  - Tests for scope criteria 21, 22, 23, 25 and 27 pass on the sample output
  - Each caption is on screen only inside its window at 0.5-second sampling (scope criterion 26, frame part)
  - The run record names the renderer used and it is the delivered one
- Gate: Review Gate

### T-006 Prove RK-016 part 2 with a rendered vertical frame

- Owner: dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-001
- Traces to: S-006, S-010, S-032, A-001
- Status: assumption-dependent
- Description: Done when a test renders, through the delivered renderer and the installed typeface, a vertical output carrying Vietnamese text with stacked marks, and records the measured dimensions and the rendered frame as evidence.
- Acceptance Criteria:
  - The probed output is 1080 pixels wide and 1920 pixels high
  - The rendered frame shows a tone mark stacked over a circumflex and over a breve, judged by qa from the kept frame
  - No component was downloaded; a failure raises Q-002 instead
- Gate: Review Gate

### T-007 Stop composition when a pre-composition check fails

- Owner: dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-004, T-006
- Traces to: S-006, S-008
- Status: ready
- Description: Done when composition starts only after the vertical-output check and the delivered character-coverage check pass over the script's caption characters and the overlay string, and the run record names a failing check.
- Acceptance Criteria:
  - Test for scope criterion 19 passes: a landscape target stops composition and the run record names the vertical check
  - Test for scope criterion 20 passes: the run record lists each drawn character with its coverage result and zero are uncovered
  - After a failed pre-check run, no composition output file exists (scope decision 6)
- Gate: Review Gate

### T-008 Assemble the output package

- Owner: dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-005, T-018
- Traces to: S-007, A-008
- Status: assumption-dependent
- Description: Done when a package holds the video, its SHA-256, the provenance list, the six input digests and a ledger entry, and the tool has no publishing path.
- Acceptance Criteria:
  - Test for scope criterion 28 passes: the recorded video SHA-256 equals the recomputed hash
  - Test for scope criterion 29 passes: zero on-screen or spoken sentences lack a provenance mapping
  - Test for scope criterion 30 passes: each of the six input digests equals its recomputed digest
  - The ledger of T-018 holds the new package's entry after assembly
- Gate: Review Gate

### T-009 Bind the CEO's publication approval to the video hash

- Owner: dev-1-implement
- Complexity: S (confidence: high)
- Depends on: T-008
- Traces to: S-008, S-009, S-015
- Status: ready
- Description: Done when the delivered owner approval by hash accepts an approval only when its hash equals the recomputed video hash, and the tool offers no publish, upload or sign-in action.
- Acceptance Criteria:
  - An approval record whose hash equals the recomputed video hash is accepted
  - After one byte of the approved video changes, the approval is refused (scope criterion 33)
  - The change set contains no network client, upload or sign-in path, judged by dev-2-reviewer
- Gate: Review Gate

### T-010 Derive caption windows from recording pauses with a manual-timing fallback

- Owner: dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-004, T-011
- Traces to: S-011, S-023, A-002
- Status: assumption-dependent
- Description: Done when caption windows come from pauses measured by signal processing, a count mismatch stops the run and requests manual timings, and a manual timing file is validated against the script and the recording.
- Acceptance Criteria:
  - Test for scope criterion 26 passes: pause count equals sentences minus one, and the run-together sample stops before captioning
  - Tests for scope criterion 37 pass
  - No speech-recognition component appears in the invoked-tool record
- Gate: Review Gate

### T-011 Admit a recording under the one-file, duration and freshness rules

- Owner: dev-1-implement
- Complexity: S (confidence: high)
- Depends on: T-018
- Traces to: S-002, S-012, S-022
- Status: ready
- Description: Done when a recording is admitted only as one file of at most 60.0 seconds whose digest no other ledger entry holds, and each refusal names its rule.
- Acceptance Criteria:
  - Test for scope criterion 15 passes: a recording whose digest another video holds is refused naming that video
  - Tests for scope criteria 16 and 17 pass: 60.0 seconds is accepted; 61.0, 299.9 and 300.0 seconds are refused
  - Test for scope criterion 18 passes: two recording files for one video are refused
- Gate: Review Gate

### T-012 Run the end-to-end sample offline under a network guard

- Owner: dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-009, T-015, T-021
- Traces to: S-010, S-013, S-028, S-033, A-005
- Status: assumption-dependent
- Description: Done when the code-generated sample runs through the toolchain to a package and checklist under a guard that fails on a socket opening, and its run record holds the network-attempt count, the dependency list and the invoked-tool log.
- Acceptance Criteria:
  - The recorded network-attempt count is zero (scope criterion 34)
  - The dependency list and the invoked-tool log are kept for scope criterion 35
  - The MC-16 automated tests pass without the company store database or a container service running
- Gate: Review Gate

### T-013 Publish the MC-16 release note

- Owner: documentation
- Complexity: S (confidence: high)
- Depends on: T-014
- Traces to: S-016
- Status: ready
- Description: Done when the release note states what the toolchain does, its refusals, the kit changes, the operator guide, and the residual risks R-003, R-007 and R-009.
- Acceptance Criteria:
  - The release note cites the T-014 verification report
  - The release note names residual risks R-003, R-007 and R-009 with their owners
  - The release note states that the tool publishes nothing and lists the kit changes of T-019 and T-022
- Gate: Closure Gate

### T-014 Verify the scope criteria against recorded evidence

- Owner: qa
- Complexity: M (confidence: low)
- Depends on: T-012, T-017, T-019, T-022, T-023
- Traces to: S-014, S-017, S-028, A-003
- Status: assumption-dependent
- Description: Done when a verification report records, per row of the T-017 matrix, the result and its evidence, and the change-set checks below are recorded.
- Acceptance Criteria:
  - Each of scope criteria 1 to 40, as enumerated in T-017, has a recorded pass or fail with evidence
  - The change set touches no path of frozen Wave 12 or Wave 13 material, and adds zero image, audio or video files (scope criterion 36)
  - The operator guide and kit files 03 and 04 agree with the delivered tool and with CEO-D-1306 item 2 and CEO-D-1307 item 2
- Gate: Verification Gate

### T-015 Generate the verification sample from repository code

- Owner: dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-001
- Traces to: S-018, A-002, A-003
- Status: assumption-dependent
- Description: Done when repository code produces at test time the sample media the T-017 matrix needs: drawn product images, tone recordings with set pauses and a run-together variant, recordings at 60.0, 61.0, 299.9 and 300.0 seconds, and portrait and landscape stand-in clips.
- Acceptance Criteria:
  - The sample is produced with no download and no speech synthesis
  - Zero media files from the sample are tracked in the repository
  - Each sample need named in the T-017 matrix is produced by the generator
- Gate: Review Gate

### T-016 Deliver the session branch compare link to the CEO

- Owner: orchestrator
- Complexity: XS (confidence: low)
- Depends on: T-013
- Traces to: S-019, A-009
- Status: assumption-dependent
- Description: Done when the reviewed change set is committed and pushed on the session branch and the compare link is recorded in the run record and given to the CEO.
- Acceptance Criteria:
  - The remote session branch holds the change set's final commit
  - The run record holds the compare link
  - The pushed final commit is the one the T-014 verification report names
- Gate: Closure Gate

### T-017 Design the acceptance test matrix for the scope criteria

- Owner: qa
- Complexity: M (confidence: medium)
- Depends on: none
- Traces to: S-021, S-022, S-023, S-024, S-025, S-026, S-027, S-028, S-033
- Status: ready
- Description: Done when a matrix maps each of scope criteria 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39 and 40 to a test level, its sample need and its evidence.
- Acceptance Criteria:
  - The matrix has 40 rows, one per criterion listed above
  - Negative paths include a file name outside the input set (R-010)
  - No row needs the network, the company store database or a container service
- Gate: Verification Gate

### T-018 Keep the pilot ledger of packages

- Owner: dev-1-implement
- Complexity: S (confidence: low)
- Depends on: T-001
- Traces to: S-014, S-022, S-026, A-006
- Status: assumption-dependent
- Description: Done when a ledger outside the company store records each package's product, focus text and recording digest, and refuses a package that breaks the pilot limits.
- Acceptance Criteria:
  - Test for scope criterion 38 passes: a thirteenth package is refused
  - Test for scope criterion 39 passes: a third package of one product is refused naming the product
  - Test for scope criterion 40 passes: a repeated focus text is refused and a different one passes
  - The recording digests it holds are the ones T-011 compares against
- Gate: Review Gate

### T-019 Hold the no-price call-to-action list in kit file 03 Part B row 6

- Owner: dev-1-implement
- Complexity: XS (confidence: low)
- Depends on: T-001
- Traces to: S-024, S-030, A-004
- Status: assumption-dependent
- Description: Done when kit file 03 Part B row 6 holds the fixed call-to-action list set by T-001 (scope decision 8 proposes the first entry), in Vietnamese, with no price, discount or urgency wording, and the former price guidance is gone.
- Acceptance Criteria:
  - The list has at least one entry, and each entry passes the T-003 check
  - Row 6 contains no money amount, discount or urgency wording
  - Each entry tells the viewer to tap the cart or the product link for details (CEO-D-1306 item 2)
- Gate: Review Gate

### T-020 Refuse a presenter clip that is not portrait

- Owner: dev-1-implement
- Complexity: XS (confidence: high)
- Depends on: T-001
- Traces to: S-002, S-025
- Status: ready
- Description: Done when a landscape presenter clip is refused before composition and the refusal names the clip.
- Acceptance Criteria:
  - Test for scope criterion 24 passes: the landscape stand-in clip is refused and the refusal names the clip
  - The portrait stand-in clip is admitted
  - A refused clip leaves no composition output file
- Gate: Review Gate

### T-021 Pre-fill the kit 04 checklist with the tool's decisions

- Owner: dev-1-implement
- Complexity: S (confidence: high)
- Depends on: T-008
- Traces to: S-027
- Status: ready
- Description: Done when the package's checklist ticks only items B3, B5 and B8 with their check results attached, and leaves the 24 items of scope criterion 32 unticked.
- Acceptance Criteria:
  - Test for scope criterion 31 passes: exactly B3, B5 and B8 are ticked, each with its check result attached
  - Test for scope criterion 32 passes: the 24 listed items carry zero ticks
  - Each item's wording in the generated checklist equals its wording in kit file 04
- Gate: Review Gate

### T-022 Align the kit voice wording with the operator's real voice

- Owner: dev-1-implement
- Complexity: XS (confidence: low)
- Depends on: T-001
- Traces to: S-029, S-030, A-004
- Status: assumption-dependent
- Description: Done when kit file 03 Part F and kit file 04 item B13 state, in Vietnamese, that the narration is the real recorded voice of the CEO or of the employee operating the tool, recorded fresh for the video, with no text-to-speech and no voice cloning (CEO-D-1305, CEO-D-1307 item 2).
- Acceptance Criteria:
  - Part F and item B13 no longer restrict the voice to the CEO alone
  - Both keep the fresh-recording, no text-to-speech and no-cloning rules
  - Part F and item B13 state the same voice rule in Vietnamese, with no contradiction between them
- Gate: Review Gate

### T-023 Write the Vietnamese operator guide

- Owner: dev-1-implement
- Complexity: S (confidence: low)
- Depends on: T-012, T-022
- Traces to: S-029, S-031, A-004
- Status: assumption-dependent
- Description: Done when a Vietnamese guide lets the operator take one product to one package: the inputs and their forms, the 60-second limit, the clear pause between sentences, the manual timing path, each refusal and its remedy, approval by hash, and the fact that the tool publishes nothing.
- Acceptance Criteria:
  - Each refusal the T-012 run record can emit appears in the guide with its remedy
  - The guide's voice wording matches T-022
  - The guide is written in Vietnamese and states that the tool publishes nothing and that the CEO approves each video by hash
- Gate: Review Gate

## Dependencies

### 8.1 Dependency Edges

| From | To | Type | Justification |
|---|---|---|---|
| T-001 | T-002 | contract | The fact sheet form is defined by the design |
| T-003 | T-002 | produces-consumes | Fact rows are screened by the banned-term check |
| T-001 | T-003 | contract | Where the term classes are held is a design decision |
| T-002 | T-004 | produces-consumes | The generator consumes an accepted fact sheet |
| T-003 | T-004 | produces-consumes | Script sentences and overrides pass the banned-term check |
| T-019 | T-004 | produces-consumes | The call to action comes from the template's list |
| T-007 | T-005 | policy-gate | Composition may start only after the pre-checks pass |
| T-010 | T-005 | produces-consumes | Captions need their windows |
| T-015 | T-005 | produces-consumes | Composition is tested on the generated sample |
| T-020 | T-005 | produces-consumes | Composition consumes an admitted presenter clip |
| T-001 | T-006 | decision-gate | The vertical render route is fixed by the design |
| T-004 | T-007 | produces-consumes | The coverage check runs over the script's caption characters |
| T-006 | T-007 | decision-gate | The vertical check rests on the proven render route |
| T-005 | T-008 | produces-consumes | The package holds the composed video |
| T-018 | T-008 | produces-consumes | Assembly writes a ledger entry |
| T-008 | T-009 | produces-consumes | Approval binds to the package's video hash |
| T-004 | T-010 | produces-consumes | The expected pause count derives from the sentence count |
| T-011 | T-010 | produces-consumes | Pauses are measured in the admitted recording |
| T-018 | T-011 | produces-consumes | Freshness compares against ledger recording digests |
| T-009 | T-012 | produces-consumes | The sample run exercises approval by hash |
| T-015 | T-012 | produces-consumes | The sample run uses the generated sample |
| T-021 | T-012 | produces-consumes | The sample run emits the pre-filled checklist |
| T-014 | T-013 | produces-consumes | The release note cites the verification report |
| T-012 | T-014 | verification | qa verifies the sample run's evidence |
| T-017 | T-014 | produces-consumes | Verification follows the test matrix |
| T-019 | T-014 | verification | qa verifies the call-to-action list |
| T-022 | T-014 | verification | qa verifies the kit voice wording |
| T-023 | T-014 | verification | qa verifies the operator guide |
| T-001 | T-015 | contract | The sample forms follow the design's test placement |
| T-013 | T-016 | policy-gate | Delivery to the CEO follows the Closure Gate |
| T-001 | T-018 | contract | The ledger location is a design decision |
| T-001 | T-019 | decision-gate | The architect writes the final call-to-action list |
| T-001 | T-020 | policy-gate | Implementation starts after the Design Gate |
| T-008 | T-021 | produces-consumes | The checklist results come from the package's checks |
| T-001 | T-022 | policy-gate | Implementation starts after the Design Gate |
| T-012 | T-023 | produces-consumes | The guide documents the refusals the run record emits |
| T-022 | T-023 | produces-consumes | The guide reuses the kit voice wording |
| external: CEO, via the orchestrator | T-005 | external | Applies only if T-006 finds a needed component absent: approval by name before any download |

### 8.2 External Dependencies

| Responsible party | What is needed | Blocks |
|---|---|---|
| CEO, relayed by orchestrator | Approval by name of any component T-006 finds missing (Q-002, R-008) | T-005, only on that condition |

### 8.3 Implementation Order

- Wave 1: T-001, T-017
- Wave 2: T-003, T-006, T-015, T-018, T-019, T-020, T-022
- Wave 3: T-002, T-011
- Wave 4: T-004
- Wave 5: T-007, T-010
- Wave 6: T-005
- Wave 7: T-008
- Wave 8: T-009, T-021
- Wave 9: T-012
- Wave 10: T-023
- Wave 11: T-014
- Wave 12: T-013
- Wave 13: T-016

Topological order (ties by ascending identifier): T-001, T-003, T-002, T-006, T-015, T-017, T-018, T-011, T-019, T-004, T-007, T-010, T-020, T-005, T-008, T-009, T-021, T-012, T-022, T-023, T-014, T-013, T-016.

## Suggested Workflow

Selected workflow: implement-feature

Selected because: the request delivers new functionality, and the run is already routed to it with the Scope Gate approved (CEO-D-1307).

| Phase | Tasks |
|---|---|
| solution-design-and-risk-assessment | T-001 |
| implementation | T-002, T-003, T-004, T-005, T-006, T-007, T-008, T-009, T-010, T-011, T-012, T-015, T-018, T-019, T-020, T-021, T-022, T-023 |
| quality-review | T-014, T-017 (Q-001) |
| documentation-and-release-handoff | T-013, T-016 |

| Gate | Required owners |
|---|---|
| Scope Gate | product-owner, business-analyst (approved) |
| Planning Gate | tech-lead, orchestrator |
| Design Gate | architect, tech-lead (tech-lead decides; producer exclusion) |
| Review Gate | dev-2-reviewer, qa |
| Verification Gate | qa |
| Closure Gate | orchestrator, documentation |

## Required Capabilities

### 10.1 Agent Capabilities

| Capability | Tasks | Owning agent | Proficiency |
|---|---|---|---|
| technical-approach-definition | T-001 | architect | Primary |
| architecture-decision-authoring | T-001 | architect | Primary |
| implementation-delivery | T-002, T-003, T-004, T-005, T-006, T-007, T-008, T-009, T-010, T-011, T-012, T-015, T-018, T-019, T-020, T-021, T-022, T-023 | dev-1-implement | Primary |
| validation-design | T-017 | qa | Primary |
| quality-verification | T-014 | qa | Primary |
| documentation | T-013 | documentation | Primary |
| run-coordination | T-016 | orchestrator | Primary |

### 10.2 Required Skills

| Skill | File | Tasks | Level |
|---|---|---|---|
| S01 | skills/architecture/clean-architecture-checklist.md | T-001 | Primary |
| S02 | skills/business/domain-modeling.md | T-001, T-017 | Secondary |
| S03 | skills/dotnet/engineering-playbook.md | T-002, T-003, T-004, T-005, T-007, T-008, T-009, T-010, T-011, T-018, T-020, T-021 | Primary |
| S06 | skills/database/database-engineering.md | T-018 | Advisory |
| S07 | skills/testing/testing-strategy.md | T-012, T-014, T-015, T-017 | Primary |
| S08 | skills/performance/performance-engineering.md | T-014 | Advisory |
| S09 | skills/security/secure-engineering.md | T-001, T-002, T-009, T-014 | Advisory |
| S10 | skills/git/git-collaboration.md | T-016 | Secondary |
| S11 | skills/logging/observability-logging.md | T-007, T-012 | Secondary |
| S12 | skills/error-handling/error-handling-strategy.md | T-002, T-003, T-010, T-011 | Secondary |

## Acceptance Criteria

1. BO1: A package built from the code-generated sample holds a 1080x1920 video, its SHA-256, the provenance list, the input digests and the pre-filled checklist; evidence: T-014 results for scope criteria 21, 28, 29, 30.
2. BO2: Each refusal class is demonstrated (header field, row field, missing screenshot, banned classes (a) to (g), unmapped sentence); evidence: T-014 results for scope criteria 1, 2, 3, 4, 5, 6, 7.
3. BO2: The checklist pre-fill ticks exactly 04 B3, B5, B8 and leaves the 24 listed items unticked; evidence: inspection recorded for scope criteria 31, 32.
4. BO3: A one-byte change voids the approval; a reused recording, a thirteenth video, a third video of one product and a repeated focus text are refused; evidence: T-014 results for scope criteria 15, 33, 38, 39, 40.
5. BO3: The script opens and closes with the byte-exact disclosure and its call to action is a no-price entry of the kit list; evidence: T-014 results for scope criteria 8, 9, 13.
6. BO4: The sample run's network-attempt count is zero, its dependency list and tool log hold no language-model, speech-recognition or speech-synthesis component, and the change set adds no media file and touches no frozen path; evidence: T-012 run record and T-014 change-set scan.
7. BO5: The Vietnamese operator guide and kit files 03 and 04 match the delivered tool and the CEO's decisions CEO-D-1306 item 2 and CEO-D-1307 item 2; evidence: T-014 review entry.
8. BO1, BO2, BO3: Scope criteria 1 to 40, as enumerated in T-017, each carry a recorded pass; evidence: the T-014 verification report.

## Definition of Done

- [ ] Plan acceptance criteria 1, 2, 3, 4, 5, 6, 7 and 8 are verified with recorded evidence
- [ ] Planning, Design, Review, Verification and Closure Gates are approved with owners recorded
- [ ] Task acceptance criteria of T-001 to T-023 are satisfied or formally waived
- [ ] Assumptions A-001 to A-009 are confirmed or converted to recorded decisions
- [ ] Risks R-001 to R-014 are closed or accepted with named owners
- [ ] Open questions Q-001 and Q-002 are closed or explicitly accepted
- [ ] The T-013 release note is published
- [ ] Durable outcomes are recorded to memory per `memory/memory-governance.md`

## Open Questions

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| Q-001 | qa owns no phase in the implement-feature Phase Model; T-014 and T-017 are mapped to quality-review, where qa owns the Review and Verification Gates. Is that the intended placement? | no | orchestrator | T-014, T-017 |
| Q-002 | If T-006 finds the delivered renderer cannot emit vertical output with the installed set, which component does the CEO approve by name? | no (conditional) | orchestrator | T-005, T-006 |

## Traceability Matrix

Statements are normalized from the feature request (S-001 to S-020, document order), then from the approved scope (S-021 to S-028), the CEO's decisions CEO-D-1306 and CEO-D-1307 (S-029, S-030) and the orchestrator's planning guidance (S-031 to S-034). Repository context named no modules; the paths in the orchestrator's briefing were confirmed present, so no rescan was made. Affected areas: security applies (R-010, R-011); database and performance are lexical triggers (A-006, A-007).

| Statement | Class | Covered by |
|---|---|---|
| S-001 One product to one ready-to-review 9:16 video, reusing the delivered pipeline | outcome | T-001, T-005 |
| S-002 Operator supplies fact sheet, images, recording, presenter clip | constraint | T-002, T-011, T-020 |
| S-003 Fact sheet check; banned content refused naming row and term | outcome | T-002, T-003 |
| S-004 Deterministic script, verbatim disclosures, Part E lines | outcome | T-004 |
| S-005 9:16 composition with motion, presenter, captions, overlay | outcome | T-005 |
| S-006 RK-016 pre-check first | outcome | T-006, T-007 |
| S-007 Output package; nothing publishes | outcome | T-008 |
| S-008 Reuse delivered components | constraint | T-001, T-005, T-007, T-009 |
| S-009 No account, sign-in, contact, purchase or publication | constraint | T-009 |
| S-010 No download without approval by name | constraint | T-006, T-012 |
| S-011 No generative model in the toolchain | constraint | T-004, T-010 |
| S-012 No text-to-speech, no cloning, reused recording refused | constraint | T-011 |
| S-013 Zero running cost, no metered call | constraint | T-012 |
| S-014 Frozen waves and company store untouched | constraint | T-014, T-018 |
| S-015 The CEO approves each publication | constraint | T-009 |
| S-016 Runs through implement-feature with gates | outcome | T-013 |
| S-017 Tooling works and is tested | outcome | T-014 |
| S-018 Sample only from repository code | constraint | T-015 |
| S-019 Committed, pushed, compare link to the CEO | outcome | T-016 |
| S-020 Subagents briefed on framework defects | context | A-004 |
| S-021 Fact sheet header and row completeness | outcome | T-002, T-017 |
| S-022 Recording admission rules | outcome | T-011, T-017, T-018 |
| S-023 Pause-detected caption windows, manual fallback | outcome | T-010, T-017 |
| S-024 No-price call to action from a fixed list | outcome | T-003, T-004, T-017, T-019 |
| S-025 Landscape presenter clip refused | outcome | T-017, T-020 |
| S-026 Ledger limits | outcome | T-017, T-018 |
| S-027 Checklist pre-fill of B3, B5, B8 only | outcome | T-017, T-021 |
| S-028 Offline, no excluded components, no media files | outcome | T-012, T-014, T-017 |
| S-029 Operator may record in own real voice | constraint | T-022, T-023 |
| S-030 Kit files 03 and 04 updated to match | outcome | T-019, T-022 |
| S-031 Operator guide in Vietnamese | outcome | T-023 |
| S-032 RK-016 part 2 proven by a rendered test | outcome | T-006 |
| S-033 Test run without company store or container service | constraint | T-012, T-017 |
| S-034 One implementing agent in this worktree | context | A-004 |

Closure counts: forward 34 of 34 statements covered; backward 23 of 23 tasks traced; lateral 9 of 9 assumptions, 14 of 14 risks and 38 of 38 edges resolve.
