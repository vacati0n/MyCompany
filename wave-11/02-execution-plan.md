```yaml
plan:
  planId: PLAN-2026-0011
  sourceInputs:
    - type: feature-request
      reference: tasks/MC-12/input.md
  producedBy: planner
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  inputDigest: sha256:e54685a9be2a241869522709f489cc79
  contextDigest: sha256:62e7676afa2df10b3af32629f294e88a
```

## Executive Summary

This plan decomposes item 001's real graphics into forty-eight tasks across twelve execution waves, for the CEO and sole owner, who wants the 22 graphic positions of the company's first video to show the diagrams its shot list describes, drawn by code on the company's machine at no metered cost and with no model call, every label and figure quoted from a recorded source, and reviewed by the owner on a contact sheet and preview before anything is produced in the company store. Success is that one produce run in the demonstration store draws all 22 graphics from recorded specifications with the owner-approved renderer, browser and font verified by hash, moves exactly the twelve motion graphics inside their beats' spans measured from the narration actually used, records each graphic's specification, output and file hashes with a sameness statement taken only from two compared renders, assembles one decodable video as before in shape, books USD 0.00, and yields the contact sheet and preview; then the orchestrator, prepared by this change, produces item 001 in the company store on the owner's go. Fourteen architect decision tasks and one baseline lead, fifteen implementation tasks follow, seventeen verification tasks evidence them and one documentation task closes; the first real graphic is drawn end to end through produce in wave 5 (T-022) and evidenced in wave 6 (T-031), motion from measured narration in wave 7 (T-023), all 22 in wave 8 (T-024), and the owner's contact sheet and preview in wave 9 (T-026), verified in wave 10 (T-041). The highest-impact risk is the round-trip and same-clock defect class this programme has missed until review: a specification hashed in one form and drawn from another, a renderer's identity taken from configuration or a lock file while other files run, a graphic hashed over other bytes than the assembly reads, motion timed from the script rather than measured beats while the delivered run draws graphics before narration, and a render, a browser or its cache left running, held inside a transaction or the 30-second command timeout, or writing into the installed renderer (R-006, R-007, R-008, R-009, R-010, R-019); T-010 enumerates every timeout, hold and clock first and T-035 and T-038 verify the round trips. Thirteen facts from the delivered code, the installed renderer and the approved scope are raised as open questions routed to the design phase, none blocking decomposition: which files are "every renderer file" when installation hashes were recorded for four files only (Q-001), where the bundler's cache and the browser's writes go when the installed folder must never change (Q-002), whether the renderer's own loopback server counts as a network request (Q-003), what evidence shows no system font was loaded (Q-004), how a tile of one contact-sheet image can be byte-identical to a full frame (Q-005), and how motion enters an assembly built for stills (Q-007). Plan status is complete: every in-scope statement is decomposed, and none of the thirteen open questions blocks decomposition.

## Business Objectives

1. The owner receives item 001 as a watchable explainer: each of its 22 graphic positions shows the drawn diagram its shot-list row describes, moving where the row or script cue asks and still otherwise, in one decodable video with the clip positions still labelled placeholders. Received by the CEO and sole owner. Measured by the demonstration-store production and the owner's contact-sheet review, then the orchestrator's company-store run record. Traces to S-001, S-004, S-009.
2. Every fact, label and figure on screen is defensible: quoted from a recorded source with its citation, never invented, with every open visual choice drawn schematically and disclosed or fixed by a recorded owner answer. Received by the owner and the channel's audience. Measured by the specification check over the recorded package and the round trip from quotation to drawn frame. Traces to S-002, S-003.
3. The owner can trust that a graphic in the company's records is the one approved: what it was drawn from and by which files is recorded by hash, and sameness is claimed only where it was observed. Received by the owner. Measured by reading each graphic artifact's provenance and the comparison of approved still hashes with the company-store run. Traces to S-005.
4. The graphics cost the company nothing metered, call no model, run only the renderer, browser and font the owner approved, and leave nothing running and nothing outside the company's output folder. Received by the owner. Measured by booked operations, the cap total, the printed plan and summary, and process and file evidence of demonstration runs. Traces to S-006, S-007.
5. The owner sees all 22 graphics, at the spans they will occupy, before any production against the company store. Received by the CEO. Measured by the contact sheet and preview produced from the demonstration store and the owner's recorded approval. Traces to S-008.
6. The company store holding real records stays untouched by every phase, while the orchestrator receives everything needed to produce item 001 there with drawn graphics on the owner's go, and the owner can repeat it from documentation. Received by the owner and the orchestrator. Measured by test configuration, the store shape change proven on a store of the company store's shape, and every documented command run as documented. Traces to S-011, S-012, S-013.
7. The company's software stays trustworthy and re-pointable: the renderer serves any item's specifications, the ten completed waves are built upon, the carried known issues touching the graphics are dispositioned by name, and no control is weakened. Received by the owner and every later item. Measured by the live and architecture suites in the demonstration store and the release record. Traces to S-010, S-014, S-015.

## Technical Objectives

1. Each graphic position has exactly one recorded specification that quotes its shot-list row verbatim, names its beat and its row's claims, carries every drawn text element with its source path and verbatim quotation, marks every open visual choice schematic or cites the owner answer fixing it, and is refused by a check naming any missing, extra, altered, unquoted or unmarked element. Verified by specification-check tests with injected faults over the recorded package. Traces to business objective 2, and to A-008.
2. The text drawn on every frame is exactly the specification's quotations, rendered as text and never as markup, with every glyph from the approved font file; a character the font lacks refuses the render naming it. Verified by text-handover tests, a fixture font lacking a glyph, and an architecture scan of the drawing code for on-screen literals. Traces to business objective 2.
3. Every graphic render is started only by the delivered single process-starting type, runs under its own configured bound on the monotonic clock with process-tree termination that leaves no Node, browser or encoder process, holds no record, transaction or datastore command open, and writes only under the output root through the single writer or into its staging folder, never into the installed renderer or the repository. Verified by stand-in processes that outlast the bound, spawn a grandchild or write outside staging, and by architecture tests. Traces to business objective 4, and to A-005 and A-006.
4. The renderer, browser, font and drawing-code files are verified before the first graphic against hashes recorded at installation, held so they cannot be swapped between check and load, and the identity and version recorded are read from those verified bytes, never from configuration or a lock file. Verified by a fixture file swapped between verification and render. Traces to business objectives 3 and 4.
5. Each graphic artifact records its specification hash over the exact bytes the renderer read, its output hash over the exact bytes the assembly reads, the renderer's name, version and file hashes, the font's name, version and hash, and a sameness statement taken only from two renders compared byte for byte. Verified by round-trip tests that alter the specification, a renderer file or the output between the two readers, and by deterministic and non-deterministic stand-ins. Traces to business objective 3, and to A-011.
6. Motion graphics change frames only inside their segment, whose bounds come from the beat durations the same run measured from decoded narration; still graphics show identical frames throughout; the timeline's segment boundaries for given beat durations are unchanged. Verified by two stand-in narrations with different beat durations and a regression test over the delivered timeline rule. Traces to business objective 1, and to A-012.
7. The assembly renders one file with exactly one video and one audio stream, decoded end to end with zero errors, its runtime measured from the file beside the specified runtime, the clip placeholders and thumbnails as delivered and the item held short of publish-ready. Verified by demonstration-store runs. Traces to business objective 1.
8. A graphics production books no operation, makes no capability call, prints USD 0.00 booked and local compute unmeasured, and reaches no network beyond what the design defines as the renderer's own loopback. Verified by booked-operation and cap readings, a counting stub, an architecture test and recorded process arguments. Traces to business objective 4.
9. A demonstration-only command produces one contact sheet of all 22 graphics, each tile identical as the design defines to its graphic's final frame, and one preview video of the 22 graphics at their production spans, decoded with zero errors; it is refused against the company store. Verified by hash comparison and store-guard tests. Traces to business objective 5.
10. An eleventh schema resource gives the store the graphic records the design names, applies alone over the ten delivered resources and the company store's recorded rows, changes none of them and re-applies changing nothing. Verified on a demonstration store prepared to the company store's recorded shape. Traces to business objective 6, and to A-010.
11. The drawing code holds no subject term of the item material, no graphic-position identifier and no on-screen text literal, imports none of the renderer's speech or speech-recognition client packages, and renders a fixture specification set for another subject unchanged; the architecture suite grows by these assertions and none is weakened. Verified by architecture tests failing on a seeded violation of each and a non-subject fixture render. Traces to business objective 7.

## Scope

### In Scope

- S-001: Each of item 001's 22 graphic positions shows a drawn graphic (shapes, axes, labels, annotations) as its shot-list row specifies, in place of the text card, with the content each of the scope's twenty-two per-graphic criteria states as amended by the owner's answers of 2026-10-11. Designed by T-001 and T-014, delivered by T-022, T-023 and T-024, verified by T-031 and T-032.
- S-002: Every graphic is drawn from a recorded specification for its position that traces each element to its shot-list row and each label and number to a quoted recorded source with its citation; every open visual choice is drawn schematically and disclosed or taken from a recorded owner answer. Designed by T-001, delivered by T-016 and T-024, verified by T-032 and T-033.
- S-003: The text and numbers on each rendered frame are exactly the quoted text of its specification, the theta renders from the approved font, and no other font is used. Designed by T-001 and T-008, delivered by T-020 and T-021, verified by T-033.
- S-004: Graphics whose row or script cue asks for motion move, timed inside their on-screen span within the beat's span measured from the narration actually used in the same run; the others are still. Designed by T-004 and T-005, delivered by T-023, verified by T-034.
- S-005: Each graphic's output is hashed together with the bytes of the specification, renderer files and font file actually used; sameness is claimed only where two renders were compared byte for byte. Designed by T-003 and T-006, delivered by T-018 and T-025, verified by T-035, T-036 and T-037.
- S-006: Every graphic render is started through the single process boundary, bounded on its own, holds no record or transaction, leaves no process running, reaches no network, and writes only through the single file writer. Designed by T-002, T-010 and T-011, delivered by T-019, verified by T-038 and T-039.
- S-007: Producing the graphics books USD 0.00 metered spend, makes no model call, uses only the owner-approved renderer and font, and records local compute unmeasured. Designed by T-003 and T-013, delivered by T-018 and T-027, verified by T-040.
- S-008: Before any company-store production, the owner can review a contact sheet of all 22 graphics and a preview, both rendered from the demonstration store. Designed by T-009, delivered by T-026, verified by T-041.
- S-009: The video is assembled as before in shape: one file decoded end to end with zero errors, runtime measured from the file, 18 labelled clip placeholders, held short of publish-ready, and the narration path unchanged. Designed by T-004 and T-005, delivered by T-022 and T-024, verified by T-042.
- S-010: The renderer draws from specifications only and carries nothing specific to the item's subject, so another item's specifications render without a code change. Designed by T-001, T-002 and T-012, delivered by T-020 and T-028, verified by T-043.
- S-011: Graphics production runs against the company store as well as a demonstration store, while every phase, test and demonstration uses the demonstration store only. Designed by T-007, delivered by T-017 and T-026, verified by T-044.
- S-012: The owner has documentation stating how to preview the graphics, how to re-produce item 001, and what changed on screen, with the exact commands. Designed by T-013, delivered by T-029 and T-030, verified by T-046.
- S-013: After the phases, the orchestrator can produce item 001 in the company store with the drawn graphics at USD 0.00, after the owner's recorded contact-sheet approval and a fresh backup, using only the documented commands. Designed by T-003, T-007 and T-013, delivered by T-017, T-018, T-027, T-029 and T-030, verified by T-045 and T-046, recorded by T-048.
- S-014: The previous release record's known issues that touch the graphics, the Design stage, the produce command or the render are dispositioned by name in this change's release record. Designed by T-004 and T-006, recorded by T-048.
- S-015: The ten completed waves are built upon and none re-created; every test passing live at the Wave 10 head still passes; the boundary assertions are extended and never weakened. Baselined by T-015, designed by T-012, delivered by T-028, verified by T-047.

### Out of Scope

- Any model call or metered service for any part of the graphics or the video, including image or video generation and any speech or speech-recognition model; the delivered vendor image path stays uncalled. Excluded by the request and the owner's decision of 2026-10-10.
- Stock footage, music and any subscription; the 18 clip positions stay labelled placeholders. Excluded by the request.
- Redrawing the six thumbnail candidates. Excluded because the request and the owner's decision name the 22 graphics only.
- Any download, install, update or reinstall by a phase, including package installs, browsers and fonts, and any write into the installed renderer, browser or font folders. Excluded by binding constraint eight; the orchestrator installed the approved files on 2026-10-11.
- Any paid renderer licence or licence key. Excluded by the owner's decision of 2026-10-11 to use the free licence with no key.
- The run of item 001 against the company store inside any phase, and any phase connection to the company store. Excluded by binding constraint seven.
- Publication, upload, channel creation, or any change to the held-stage refusal. Excluded by binding constraint one.
- Any change to the narration sources, the in-house voice, its settings, the narration join or its measurements. Excluded because the narration path of the previous wave is unchanged.
- Any on-screen quantity, scale, threshold, colour rule, number or relationship not quoted from a recorded source or fixed by a recorded owner answer, including the one-second-per-kilometre figure the owner did not admit. Excluded by binding constraint six.
- Three-dimensional engines, illustration or character styling. Excluded because every row is a plan, elevation, plot, schematic or block diagram in the item's data-visualisation idiom.
- Asserting that the whole rendered video's bytes repeat across runs. Excluded as a carried residual risk; only graphic outputs are compared.
- Aligning a graphic's entry or motion to a phrase inside its beat. Excluded because no within-beat timing is measured and no model may be used.
- A second item, a second channel, or re-specifying item 001's script, shot list or claims. Excluded by the request.
- Carried known issues that touch narration, model verification, the suite or the release only. Excluded because the narration path and the release process are unchanged.
- Redistributing the renderer, the browser or the font outside the company. Excluded because use is internal.

### Deferred

- The orchestrator's run of item 001 in the company store with drawn graphics. Brought into scope when this change closes, the owner has recorded approval of the contact sheet naming its hash and of the palette, the orchestrator has taken a fresh backup, applied the eleventh resource alone, written the run's settings outside the repository, run the renderer verification and plan-only, and the owner gives the explicit go.
- Re-timing rules finer than a graphic's segment, such as phrase alignment. Brought into scope by an owner decision admitting a within-beat timing source.
- Graphics for thumbnails or for clip positions. Brought into scope by an owner decision opening them.
- Any figure the owner did not admit, including a conversion figure for the duration graphic. Brought into scope when the owner records the value with its source.

## Assumptions

| ID | Assumption | Basis | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | The Scope Gate approved the scope (14 items, 16 exclusions, 71 criteria, 12 decisions, 17 questions; validator 33 of 33) with rulings that reached this phase only through the dispatch briefing: the three blocking questions and the owner questions were answered by the owner on 2026-10-11; the Design stage is a step of the produce service with no stage handler, and the vendor image adapter stays uncalled; timing motion from measured narration, which needs graphics rendered after Audio, goes to architect and omn-tech-lead at the Design Gate; the graphics use the approved font while the clip placeholders stay unchanged unless the design shows a reason; the implementer re-runs the full live suite as a baseline; the new artifact roles need a store shape change applied to the company store by the orchestrator after a backup | S-001 | Ordering, store, baseline or font tasks change shape | omn-business-analyst |
| A-002 | The owner's decisions of 2026-10-10 and 2026-10-11 bind every phase: graphics drawn by code with no model call; renderer Remotion 4.0.534 under its free licence with no licence key; Chrome Headless Shell 157.0.8080.0; font Inter 4.1; all installed by the orchestrator outside the repository with the lock file, the browser executable and two font files hashed; admitted on screen with their citation "~2 km", "60°", "nine o'clock" and "noon", not the per-kilometre figure; the species name on the first graphic; the twentieth graphic's narration words; a shaded spread with no points; a narrowing angle with a widening region; "schematic, not to scale" on every schematic graphic; no time scale on the oscillation trace; twelve motion and ten still graphics as listed; no statistic on graphics 17 to 20; the end card with real citations only; a preview at production spans; motion re-timed in the company-store run while stills equal the approved renders; palette values proposed by the design and approved at the contact-sheet review | S-007 | An owner-gated value enters the plan, or a specification or renderer task changes | omn-product-owner |
| A-003 | Verification tasks are owned by omn-dev-2-reviewer because omn-qa owns no phase in the routed workflow | plan-wide | Seventeen verification tasks are never dispatched | omn-orchestrator |
| A-004 | The owner guide and the settings sample are repository files written by omn-dev-1-implement; omn-documentation, which writes no repository file, produces the release record as its phase artifact | S-012 | The guide or the sample is never written, or a non-implementing role writes a repository file | omn-orchestrator |
| A-005 | Every new configured quantity (the per-graphic render bound, any bundling bound, the contact-sheet and preview bounds, the renderer verification's hashing bound) is proposed by architect tasks and ruled at the Design Gate, where omn-tech-lead is a listed owner, because omn-tech-lead owns no phase | S-006 | T-010 is not ruled, and T-019 and T-026 have no value to apply | omn-orchestrator |
| A-006 | The default test suite uses an in-test stand-in renderer process started through the boundary; a test that needs the installed renderer is explicitly marked so and skips cleanly, naming why, when it is absent; demonstrations use the installed files read-only and nothing installed is modified | S-006 | A phase changes the installed renderer, or the default suite depends on a browser | omn-orchestrator |
| A-007 | Every test and demonstration names the separate demonstration database only, and the live suite is never pointed at any other database | S-011 | The named store's schema is dropped or the company store is written | omn-orchestrator |
| A-008 | The 22 specifications are authored in the implementation phase from the recorded package (shot list, claim record, narration) and the owner's decision record, and item 001's recorded package is not changed | S-002 | Specification tasks re-specify the item, contrary to the exclusions | architect |
| A-009 | The drawing code that turns a specification into frames is the company's own code under version control, run with the installed packages; where it lives and how it is bundled are decided by T-002 | S-010 | The architecture assertions over drawing code and the re-pointability test have no sources to read | architect |
| A-010 | At the post-phase run the company store is as recorded after the previous wave's run: ten resources, item 001 versions 2 and 3, with the cap's counted total at USD 0.007695 estimate; the eleventh resource is applied by the orchestrator after a fresh backup | S-013 | The eleventh resource meets an unproven state, or the cap reading differs | omn-orchestrator |
| A-011 | The orchestrator's trial of 2026-10-11 (bundling 8.3 s cold and 2.5 s warm, 90 PNG frames in 9.7 to 12.9 s, an H.264 file in 8.7 to 9.8 s at concurrency 1, frames and file byte-identical over two runs, a bundler cache of about 170 MB under the installed packages) is first-hand evidence for design proposals and not a company record; render time for 22 graphics at production spans is unmeasured | S-005 | Bounds rest on an unmeasured figure, or sameness is claimed from the trial | omn-orchestrator |
| A-012 | Graphics consume the beat durations the delivered Audio stage measures from decoded narration, and the delivered timeline rule (each beat's measured span shared equally among its graphic and clip positions) is unchanged | S-004 | Motion timing or segment boundaries need a narration or timeline change, contrary to the exclusions | architect |

## Risks

| ID | Class | Trigger | Impact | Likelihood | Affects | Mitigation | Owner |
|---|---|---|---|---|---|---|---|
| R-001 | requirement | A Scope Gate ruling or owner answer restated in A-001 or A-002 is overturned | Specification, ordering, store or renderer tasks change | low | plan-wide | An overturned ruling returns the plan to this phase rather than being absorbed | omn-business-analyst |
| R-002 | requirement | One contact-sheet image cannot hold tiles byte-identical to full frames (Q-005) | Scope criterion 51 read as unmet, or identity redefined silently | high | T-009, T-026, T-041 | T-009 states what identity means for a tile and how it is checked | architect |
| R-003 | requirement | The on-screen number rule is undecidable once admitted figures and citation years appear (Q-006) | The number-set test passes or fails on a reading, not a rule | medium | T-001, T-016, T-033 | T-001 states which characters count as a number and how each is matched to its quotation | architect |
| R-004 | requirement | The owner rejects the palette or a graphic at the contact-sheet review (Q-011) | Specifications or drawing code change and the review repeats | medium | T-014, T-024, T-026, T-041, T-048 | T-014 proposes values before drawing; palette lives in data so a change re-renders without a code change | architect |
| R-005 | requirement | A specification fills an open visual choice by a phase's own judgement, such as an angle value, a ratio or a tick | An invented quantity reaches the screen | medium | T-001, T-016, T-024, T-033 | T-016 refuses a quantity neither marked schematic nor citing an owner answer; T-033 injects one | architect |
| R-006 | technical | A specification is hashed in one form and drawn from another, re-serialised or with defaults filled in by the drawing code | Provenance names a specification that was not drawn | high | T-001, T-006, T-016, T-022, T-035 | T-006 binds the hash to the bytes the renderer reads; T-035 alters the specification between hash and render | architect |
| R-007 | technical | The renderer's or font's identity or version is taken from configuration or the lock file, or a file is swapped between verification and load | Provenance names files that did not run | high | T-003, T-018, T-025, T-035 | T-003 states which bytes are hashed, read and held; T-035 swaps a fixture file | architect |
| R-008 | technical | A graphic is hashed over one file while the assembly or the contact sheet reads another, re-encoded or re-extracted | The recorded hash names bytes that were not shown | high | T-005, T-009, T-022, T-023, T-035 | T-005 names the file each reader takes; T-035 alters an output before assembly | architect |
| R-009 | technical | Motion is timed from the script's rate, beat starts or runtime, or graphics are still drawn before the narration is measured, as the delivered run orders Design before Audio (Q-008) | Motion falls outside its segment or follows an expectation | high | T-004, T-005, T-023, T-034 | T-004 places motion after measurement; T-034 uses two stand-in narrations and altered script values | architect |
| R-010 | technical | A render, a bundling step or renderer-file hashing runs inside a transaction or the 30-second command timeout, has no bound, or leaves a Node, browser or encoder process running, notably a browser whose parent already exited | Records wait, a run ends unnamed, or processes leak | high | T-010, T-019, T-022, T-038 | T-010 enumerates every timeout, hold and clock; T-038 enumerates processes after a stand-in that outlasts its bound and spawns a grandchild | architect |
| R-011 | technical | Sameness is claimed without two renders, or output depends on the clock, locale, time zone, installed system fonts, an unrecorded seed or browser state | A false determinism claim | medium | T-006, T-025, T-036 | T-006 records sameness only from a comparison; T-036 renders under two locales and time zones | architect |
| R-012 | technical | A character the approved font lacks is drawn by a system fallback font without refusal (Q-004) | A font other than the approved one reaches the frame | medium | T-008, T-021, T-033 | T-008 states the coverage check before render; T-033 uses a fixture font lacking a glyph | architect |
| R-013 | technical | Motion segments do not join the delivered still-tuned segment list, or joining them moves a segment boundary (Q-007) | The assembly fails or the timeline changes | medium | T-005, T-023, T-042 | T-005 states the assembly's input form; T-042 runs the timeline regression with fixed durations | architect |
| R-014 | technical | Rendering 22 graphics at production spans, twice for comparison, takes longer than a bound or makes a run impractical; the time is unmeasured (A-011, Q-010) | Runs end at a bound, or the comparison is dropped | medium | T-006, T-010, T-025, T-031 | T-010 proposes bounds from the trial and T-031 records measured times as observations | architect |
| R-015 | technical | The eleventh resource alters a delivered row or fails over the company store's recorded state | The company store is damaged before the run | medium | T-007, T-017, T-045 | T-045 applies it twice to a store of the company store's shape and compares every delivered row | architect |
| R-016 | technical | The drawing code holds a subject term, a position identifier or an on-screen literal, or lives where the architecture suite does not read it (A-009) | Re-pointability and the text round trip are unproven | low | T-002, T-012, T-020, T-028, T-043 | T-012 names the sources scanned; T-043 renders a non-subject fixture set | architect |
| R-017 | security | A speech-vendor or speech-recognition client package of the renderer's dependency tree is imported or called, or a licence key or telemetry option is passed (Q-012) | A model or vendor call, or a usage event, contrary to the owner's decisions | medium | T-002, T-011, T-012, T-028, T-040 | T-012 asserts the import set; T-040 inspects recorded arguments | architect |
| R-018 | security | A render reaches the network: a browser download when the browser path is absent or wrong, a browser update or service request, or a font or asset fetched by address (Q-003) | A request leaves the machine | medium | T-002, T-011, T-019, T-039 | T-011 states how each is prevented and evidenced; T-039 evidences it | architect |
| R-019 | security | The bundler's cache, the browser's profile or temporary files are written into the installed renderer folder, the system temporary folder or the repository (Q-002) | An installed file changes, a hashed file set drifts, or files escape the single writer | high | T-002, T-003, T-019, T-038 | T-002 places every write under the run's staging folder; T-038 compares the installed folders before and after | architect |
| R-020 | security | Specification text reaches the drawing code as markup or script, or an argument reaches a shell | Injected content renders or executes | low | T-001, T-020, T-033 | T-001 states text is handed as data; T-033 injects markup in a fixture | architect |
| R-021 | operational | A test or demonstration names a store other than the separate demonstration database, contrary to A-007 | The named store is dropped or the company store is written | low | T-015, T-031, T-041, T-044, T-046, T-047 | Each verification records its target store | omn-orchestrator |
| R-022 | operational | A verification task is assigned to a role that owns no phase, contrary to A-003 | Verification never runs | medium | plan-wide | A-003 assigns verification to the quality-review owner | omn-orchestrator |
| R-023 | operational | A new configured quantity is not ruled because omn-tech-lead owns no phase, contrary to A-005 | Bounds have no ruling | low | T-010, T-019, T-026 | The orchestrator obtains each ruling at the Design Gate | omn-orchestrator |
| R-024 | operational | A default-suite test starts the real browser or renderer, or a test marked as needing the installed renderer fails instead of skipping when it is absent, contrary to A-006 | The suite depends on installed files or modifies them | medium | T-019, T-028, T-036, T-047 | T-028 asserts the marking; T-047 runs the suite with the renderer path unset | omn-dev-2-reviewer |
| R-025 | dependency | An installed renderer, browser or font file changes before the run, or no expected hash is recorded for a file the design names (Q-001) | The verification refuses at the run, or verifies against a value nobody measured | medium | T-003, T-018, T-031, T-048 | T-003 names the files and where expected values are recorded; the release record lists them for the orchestrator | omn-orchestrator |
| R-026 | dependency | The demonstration database is unavailable for the live suite | Live criteria cannot be evidenced | low | T-015, T-031, T-041, T-047 | Verification records the not-run count and reason, never a pass | omn-orchestrator |
| R-027 | dependency | The installed media tool is absent, a different version, or not located by its setting | Assembly and decode evidence are not run | low | T-031, T-042 | Tests needing the tool are reported not-run with the reason, never as passed | omn-orchestrator |
| R-028 | dependency | The owner's contact-sheet approval is not recorded with its hash before the run, the company store differs from A-010, or the eleventh resource is applied without a fresh backup (Q-013) | The run proceeds unapproved or cannot be reversed | low | T-013, T-045, T-048 | The release record orders approval, backup, readings, resource, settings, verification, plan-only and go | omn-orchestrator |
| R-029 | delivery | Fifteen statements and forty-eight tasks exceed one implementation pass | The contact sheet or the guide slips and the change closes partial | medium | plan-wide | The first real graphic (T-022) and the contact sheet (T-026) precede the guide; the should-have statement is recorded only in T-048 | omn-orchestrator |

## Task Breakdown

### T-001 Decide the graphic specification's form, location and check

- Owner: architect
- Complexity: L (confidence: medium)
- Depends on: none
- Traces to: S-002, S-003, S-010, A-002, A-008
- Status: assumption-dependent
- Description: A recorded design states where each item's graphic specifications live, the closed set of drawable elements and their fields, how every text element carries its source path and verbatim quotation, how a schematic choice is marked and disclosed with the owner's wording and how an owner answer is cited, how a seed is a specification value if any is used, which characters count as an on-screen number and how citation years and admitted figures are matched to quotations (Q-006), how specification text reaches the drawing code as data never markup, how the specification bytes the renderer reads are the bytes hashed, and every refusal the check names.
- Acceptance Criteria:
  - The design names the specification's location per item and states that no specification content lives in drawing code.
  - The design lists every element type and states each field's source, including text, schematic marks and owner-answer citations.
  - The design states the number rule over quotations, admitted figures and citation years, and the check's refusal naming the offending element.
  - The design states that the check reads the shot list, claim record and narration as recorded and names a row whose claims disagree with the claim record's carried-by column.
  - The design states how text is handed to the drawing code as data and that no markup in a quotation is interpreted.
- Gate: Design Gate

### T-002 Decide how a graphic render is started and confined

- Owner: architect
- Complexity: L (confidence: low)
- Depends on: none
- Traces to: S-006, S-010, A-002, A-006, A-009
- Status: assumption-dependent
- Description: A recorded design states which executable the single process-starting type starts for a render and with which argument list, where the company's drawing code and render script live and how they are bundled with the installed packages without writing into the installed folders, where the bundler's cache, the browser's profile and every temporary file are written (Q-002), how the browser path is always passed so no browser is fetched, how locale, time zone and concurrency are fixed for the child processes, which installed packages may be loaded and how the speech and speech-recognition clients are kept out (Q-012), and how files the render produces reach the output root through the single writer.
- Acceptance Criteria:
  - The design names the started executable, its argument list as a list, its working directory and its environment, and states that no shell line is used.
  - The design lists every file location the render, the bundler and the browser write and states that each lies under the run's staging folder.
  - The design states that no file under the installed renderer, browser or font folders is created or changed by a run.
  - The design lists the packages the drawing code and render script may import and names the excluded speech and speech-recognition clients.
  - The design states how a missing or wrong browser path is refused before any process starts.
- Gate: Design Gate

### T-003 Decide how the renderer, browser and font installation is recorded and verified

- Owner: architect
- Complexity: L (confidence: low)
- Depends on: T-002
- Traces to: S-005, S-007, S-013, A-002, A-006
- Status: assumption-dependent
- Description: A recorded design states which files form "every renderer file" (the Node executable, the render script and drawing code, the installed packages including the encoder binaries, the browser folder, the font file) and why, where each expected SHA-256 is recorded given the decision record holds hashes for the lock file, the browser executable and two font files only (Q-001), how the bundler cache is excluded from or placed outside the verified set, how files are held between verification and load as the delivered model verification holds its files, where the renderer's name and version and the font's name and version are read from verified bytes, and the verification command reading files only.
- Acceptance Criteria:
  - The design lists the verified file set by path pattern and states for each class whether it is hashed per run, per installation record or not at all, with the reason.
  - The design names where expected values are recorded, outside the repository, and who records them for files the decision record does not hash.
  - The design states that recorded identity and version come from verified bytes, never from configuration or the lock file.
  - The design states how a file swapped between verification and render is detected, naming both hashes.
  - The design names the verification command, states that it starts no process, and states its exit codes.
- Gate: Design Gate

### T-004 Decide where graphics render in a run relative to narration, and the Design stage's form

- Owner: architect
- Complexity: M (confidence: medium)
- Depends on: none
- Traces to: S-004, S-009, S-014, A-001, A-012
- Status: assumption-dependent
- Description: A recorded design states where still and motion graphics render relative to the Audio stage, given the delivered run executes Design before Audio and motion needs the measured beat durations (Q-008), which stage records each graphic artifact and outcome, how a graphic failure is recorded and ends the run, and how the Design stage stands against the delivered stage-handler contract for the release record.
- Acceptance Criteria:
  - The design states the execution order of graphic rendering and narration and names the stage recording each graphic.
  - The design states that no motion graphic is rendered before the beat durations it uses are measured in the same run.
  - The design states the stage-handler position in one sentence the release record can quote.
  - The design states each new stage outcome and exit code a graphic failure produces.
- Gate: Design Gate

### T-005 Decide motion timing and how the assembly consumes still and motion graphics

- Owner: architect
- Complexity: L (confidence: low)
- Depends on: T-004
- Traces to: S-004, S-005, S-009, A-012
- Status: assumption-dependent
- Description: A recorded design states how a motion graphic's frame count and motion window derive from its segment in the delivered timeline, that no script rate, beat start or specified runtime is read, the form of a graphic's output (single image, frame sequence or video) and its final frame, how the delivered still-tuned segment list takes motion segments (Q-007), and that the file the assembly reads for each graphic is the file whose hash is recorded.
- Acceptance Criteria:
  - The design states the motion window's first and last frame relative to the segment's measured bounds.
  - The design names every input of timing and states that the script's rate, beat starts and specified runtime are not among them.
  - The design names the output form of still and motion graphics and the exact file each later reader takes.
  - The design states how segment boundaries for given beat durations stay identical to the delivered rule.
- Gate: Design Gate

### T-006 Decide graphic provenance and the sameness record

- Owner: architect
- Complexity: M (confidence: medium)
- Depends on: T-001, T-003, T-005
- Traces to: S-005, S-014, A-011
- Status: assumption-dependent
- Description: A recorded design states every provenance field of a graphic artifact (specification hash, output hash, renderer name, version and file hashes, font name, version and hash, sameness statement) and the source of each, when a graphic is rendered twice and compared and what is recorded when it does not repeat (Q-010), how a company-store still is compared with its approved demonstration render, and how the previous record's entry on render repeatability is narrowed to graphic outputs.
- Acceptance Criteria:
  - The design lists every provenance field with the bytes or file it is computed from.
  - The design states that sameness is recorded only from two compared renders and names the wording when outputs differ, with both hashes.
  - The design states which graphics are rendered twice in demonstration and company-store runs.
  - The design states how a still's approved hash is recorded and compared at the company-store run.
- Gate: Design Gate

### T-007 Decide the store shape change for graphic records

- Owner: architect
- Complexity: M (confidence: medium)
- Depends on: T-006, T-009
- Traces to: S-005, S-011, S-013, A-001, A-010
- Status: assumption-dependent
- Description: A recorded design states the eleventh schema resource: the artifact roles added to the closed role set (motion graphic output, contact sheet, preview, and any other the design names), the graphic provenance record and its constraints, that it applies alone after the ten delivered resources and re-applies changing nothing, that it changes no delivered row, and its reversal.
- Acceptance Criteria:
  - The design lists every role, table and constraint the resource adds and states that no delivered row changes.
  - The design states how the resource is applied alone and that a second application changes nothing.
  - The design states the reversal for a store holding demonstration rows only and for the company store after a backup.
- Gate: Design Gate

### T-008 Decide how only the approved font draws every character

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: T-002
- Traces to: S-003, A-002
- Status: assumption-dependent
- Description: A recorded design states which font file of the approved family is loaded and how, how every character of a specification is checked against that file's glyph coverage before any render with a refusal naming the character, how the browser is kept from falling back to an installed system font, and what evidence establishes that no system or fallback font drew a character, since the browser keeps no loaded-file record (Q-004).
- Acceptance Criteria:
  - The design names the font file and states that its hash is the one recorded with every graphic.
  - The design states the coverage check, its input and its refusal message naming the character.
  - The design states the evidence a demonstration records that no system font drew a character.
- Gate: Design Gate

### T-009 Decide the contact sheet and preview

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: T-005, T-010
- Traces to: S-008, A-002
- Status: assumption-dependent
- Description: A recorded design states the command producing the contact sheet and preview from a demonstration-store production, its refusal against the company store, the sheet's layout of 22 tiles labelled with position number and row text, what identity between a tile and its graphic's final frame means and how it is checked given one image of scaled tiles cannot be byte-identical to full frames (Q-005), the preview's composition at production spans, and the bounds and holds of both runs.
- Acceptance Criteria:
  - The design names the command, its inputs and its refusal against a company store.
  - The design states the tile identity rule and the comparison that checks it.
  - The design states that the preview shows the 22 graphics in position order at the spans of the same production, with one video stream.
  - The design states where the sheet's and preview's hashes are printed for the owner's approval record.
- Gate: Design Gate

### T-010 Decide every timeout, hold and clock on the graphics path

- Owner: architect
- Complexity: M (confidence: medium)
- Depends on: T-002, T-003, T-004, T-005
- Traces to: S-006, A-005, A-011
- Status: assumption-dependent
- Description: A recorded design enumerates every timeout and hold the graphics path touches (the 30-second datastore command timeout, each record's short transaction and the datastore's instant, the delivered probe, decode, still and render bounds, and the new per-graphic render, bundling, renderer-file hashing, contact-sheet and preview invocations), proposes a value for each new bound from the orchestrator's trial (Q-009), and states how process-tree termination reaches a browser and an encoder started by the render's own process.
- Acceptance Criteria:
  - The design lists every timeout, hold and clock by name and file, each with its value or proposed value labelled configured.
  - The design states that no render, bundling or hashing runs inside a transaction or a datastore command.
  - The design states how a browser or encoder whose parent exited is found and terminated past a bound.
  - The design states which clock each recorded instant and each bound uses.
- Gate: Design Gate

### T-011 Decide how a graphic render reaches no network and how that is evidenced

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: T-002
- Traces to: S-006, S-007, A-002
- Status: assumption-dependent
- Description: A recorded design states every way a render could reach the network (a browser fetch when no browser path is given, browser services and updates, assets or fonts loaded by address, a licence key's usage event) and how each is prevented, whether the renderer's own loopback server serving the bundle to the browser counts as a network request under scope criterion 44 (Q-003), and what a demonstration and the orchestrator's company-store run record as evidence without any phase changing the machine's security settings.
- Acceptance Criteria:
  - The design lists each network path and its prevention.
  - The design states the loopback position and the evidence recorded, labelled structural where it is not observed.
  - The design states that no licence key and no telemetry option is passed and how the argument list is recorded.
- Gate: Design Gate

### T-012 Decide the boundary assertions this change adds

- Owner: architect
- Complexity: S (confidence: medium)
- Depends on: T-001, T-002
- Traces to: S-006, S-007, S-010, S-015, A-006, A-009
- Status: assumption-dependent
- Description: A recorded design lists every architecture assertion added or extended: the single process-starting type and single writer over the new types, no capability gateway on the drawing path, no subject term, position identifier or on-screen literal in the drawing code and render script, no import of the excluded client packages, the contact-sheet command's demonstration-only guard, and the marking of tests that need the installed renderer; it states which delivered assertion each extends and that none is weakened.
- Acceptance Criteria:
  - The design lists each assertion with the sources it reads, including sources in a language other than the solution's own.
  - The design names each delivered assertion extended and states that its existing cases are unchanged.
  - The design states a seeded violation for each new assertion.
- Gate: Design Gate

### T-013 Decide the commands, settings and guide contents for the owner and the orchestrator

- Owner: architect
- Complexity: M (confidence: medium)
- Depends on: T-003, T-007, T-009
- Traces to: S-007, S-012, S-013, A-004, A-010
- Status: assumption-dependent
- Description: A recorded design states the commands and their exit codes the owner and the orchestrator use (renderer verification, plan-only naming the renderer and font with verified hashes, the licence as recorded with its source and USD 0.00 planned, the contact sheet and preview, produce, applying the eleventh resource alone), the settings entries for the Node executable, render script, renderer folder, browser, font and bounds with no default in code, where the owner's contact-sheet approval with its hash is recorded (Q-013), and the guide's sections including one line per graphic position on what changed.
- Acceptance Criteria:
  - The design lists every command with its arguments and exit codes.
  - The design lists every new settings entry and states that each lives outside the repository with no default in code.
  - The design states the orchestrator's ordered post-phase steps from approval to readings after the run.
  - The design states the guide's sections and that each command is run as documented in verification.
- Gate: Design Gate

### T-014 Propose the palette values for the owner's contact-sheet review

- Owner: architect
- Complexity: XS (confidence: high)
- Depends on: none
- Traces to: S-001, A-002
- Status: ready
- Description: A recorded design proposes exact background, line-work and single accent colour values within the owner's light paper-style background, dark line work and one accent colour, states where the values live so that a change re-renders without a code change, and states that no colour carries a meaning not quoted from a recorded source.
- Acceptance Criteria:
  - The design states three exact colour values and the role of each.
  - The design states that the values are data read by the drawing code, not literals in it.
  - The design states that no colour encodes a quantity or category not quoted from a recorded source.
- Gate: Design Gate

### T-015 Record the live baseline at the Wave 10 head

- Owner: omn-dev-1-implement
- Complexity: S (confidence: high)
- Depends on: none
- Traces to: S-015, A-001, A-007
- Status: ready
- Description: The full live suite has been run against the separate demonstration database at the Wave 10 head before any change, and its totals, the architecture suite's case count, the source and test file counts and the schema resource count are recorded as the baseline every later verification compares with.
- Acceptance Criteria:
  - The baseline records passed, failed and skipped counts of the full live suite with the database name it targeted.
  - The baseline records the architecture suite's case count by counting, stated beside the reported figure of 88.
  - The baseline records the head commit, version 1.7.0, and the counts of sources, tests and schema resources.
- Gate: none

### T-016 Deliver the graphic specification format and its check

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-001
- Traces to: S-002, S-003, S-010, A-008
- Status: assumption-dependent
- Description: The specification form T-001 decided exists with a check that reads the recorded package and refuses, naming the element or row, a missing, extra or altered row, an unquoted or misquoted text element, an unadmitted number, an unmarked quantity, and a row whose claims disagree with the claim record.
- Acceptance Criteria:
  - The check accepts a complete fixture set and refuses each injected fault with a message naming it.
  - The check reads the specification bytes once and exposes the hash of exactly those bytes.
  - No subject term of the item material appears in the check's code.
- Gate: none

### T-017 Deliver the eleventh schema resource

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-007
- Traces to: S-005, S-011, S-013, A-010
- Status: assumption-dependent
- Description: The eleventh schema resource T-007 decided exists in order after the ten delivered ones, applies alone through the delivered installer, re-applies changing nothing, and its header records its additions and reversal.
- Acceptance Criteria:
  - Applying it to a demonstration store holding the ten resources succeeds and a second application changes nothing.
  - Every role, table and constraint T-007 lists exists after application and no delivered row differs.
  - The store guide records the resource and the command that applies it alone.
- Gate: none

### T-018 Deliver the renderer installation verification and its command

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-003
- Traces to: S-005, S-007, S-013, A-006
- Status: assumption-dependent
- Description: The verification T-003 decided reads files only, hashes every file of the decided set against its configured expected value, refuses naming the file and both hashes, holds the files for the render, reads the renderer's and font's identity and version from verified bytes, and is reachable as the decided command.
- Acceptance Criteria:
  - Over fixture files made in the test, the command reports each file verified and exits as designed; a changed byte refuses naming both hashes.
  - The verification starts no process and writes no file.
  - Identity and version are read from verified fixture bytes, and a differing configured value does not reach the report.
- Gate: none

### T-019 Deliver the graphic render through the one process starter

- Owner: omn-dev-1-implement
- Complexity: L (confidence: low)
- Depends on: T-002, T-010, T-011
- Traces to: S-006, A-005, A-006
- Status: assumption-dependent
- Description: The single process-starting type starts a graphic render as T-002 decided, with the argument list, environment, working directory and write locations decided, under the per-graphic bound T-010 ruled on the monotonic clock, terminating the whole tree past it, holding no transaction, and passing no licence key or telemetry option; an in-test stand-in render process exercises it.
- Acceptance Criteria:
  - A stand-in render started through the boundary produces its output in staging and is promoted by the single writer.
  - A stand-in that outlasts a short bound ends under the bound outcome and no process of its tree remains, including a grandchild it started.
  - A refused setting (absent Node path, browser path, render script or font) stops before any process starts, naming the setting.
  - The recorded argument list contains no licence key and no telemetry option.
- Gate: none

### T-020 Deliver the drawing program for specification elements

- Owner: omn-dev-1-implement
- Complexity: L (confidence: medium)
- Depends on: T-001, T-002, T-008, T-014
- Traces to: S-001, S-003, S-010, A-009
- Status: assumption-dependent
- Description: The company's drawing code draws every element type T-001 lists from a specification and the palette values, sets each text element from the specification as text only, loads only the approved font, and holds no subject term, position identifier or on-screen literal.
- Acceptance Criteria:
  - Each element type renders from a non-subject fixture specification.
  - The text drawn equals the specification's quotations, and markup inside a quotation is drawn literally.
  - The drawing code imports only the packages T-002 lists.
- Gate: none

### T-021 Deliver the font coverage refusal

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-008, T-016
- Traces to: S-003, A-002
- Status: assumption-dependent
- Description: Before any render, every character of a specification is checked against the approved font file's coverage as T-008 decided, and a character the font lacks refuses the render naming the character and the graphic.
- Acceptance Criteria:
  - A fixture font lacking one glyph refuses a specification using it, naming the character.
  - The theta and every character of item 001's specifications pass against the approved font's coverage.
  - The check reads the same font bytes whose hash is recorded.
- Gate: none

### T-022 Deliver the first drawn graphic end to end through produce

- Owner: omn-dev-1-implement
- Complexity: L (confidence: low)
- Depends on: T-004, T-016, T-018, T-019, T-020
- Traces to: S-001, S-006, S-009, A-002, A-006
- Status: assumption-dependent
- Description: A produce run in the demonstration store draws one still graphic position from its recorded specification with the installed approved renderer, verified before the render, places it at its stage in the order T-004 decided, records it through the single writer, and assembles one decodable video; the remaining positions render as delivered until T-024.
- Acceptance Criteria:
  - The run's specification for that position passes the check and its graphic artifact is recorded with the hash of the file the assembly reads.
  - The rendered video has one video and one audio stream, decodes with zero errors, and its graphic segment shows the drawn graphic.
  - The run's output records no file under the installed renderer, browser or font folders and none in the repository.
- Gate: none

### T-023 Deliver motion graphics timed from the measured narration

- Owner: omn-dev-1-implement
- Complexity: L (confidence: low)
- Depends on: T-005, T-017, T-022
- Traces to: S-004, S-009, A-012
- Status: assumption-dependent
- Description: Motion graphics render after the run's beat durations are measured, each with its frame count and motion window derived from its segment as T-005 decided, and the assembly takes still and motion graphics from the very files whose hashes are recorded, with segment boundaries unchanged.
- Acceptance Criteria:
  - With a stand-in narration, a motion graphic's first and last changing frame lie inside its segment.
  - Two stand-in narrations with different beat durations yield different timings, each inside its segment.
  - Changing the script's rate, beat starts or specified runtime in a fixture leaves every graphic output identical.
- Gate: none

### T-024 Deliver the remaining specifications so all 22 positions are drawn

- Owner: omn-dev-1-implement
- Complexity: L (confidence: medium)
- Depends on: T-021, T-023
- Traces to: S-001, S-002, S-009, A-002, A-008
- Status: assumption-dependent
- Description: All 22 of item 001's graphic positions have specifications passing the check, each meeting its per-graphic scope criterion as amended by the owner's answers, exactly the twelve listed motion graphics move, and no graphic position renders a text card.
- Acceptance Criteria:
  - Exactly 22 specifications exist, one per position, and the check passes over them.
  - A demonstration produce run records a drawn graphic artifact for every position and no text card at any graphic position.
  - Every on-screen number in the 22 specifications is matched to its quotation and citation under the rule T-001 states.
- Gate: none

### T-025 Deliver graphic provenance and the sameness record

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-006, T-017, T-022
- Traces to: S-005, A-011
- Status: assumption-dependent
- Description: Every graphic artifact records the provenance fields T-006 lists on one short transaction after its file is promoted, and the sameness statement is recorded from two compared renders where T-006 decides, or as not observed to repeat with both hashes.
- Acceptance Criteria:
  - After a demonstration run every graphic's provenance fields read back non-empty from the store.
  - A deterministic stand-in records sameness and a non-deterministic stand-in records not observed to repeat with both hashes.
  - No provenance value is read from configuration where T-006 names a file.
- Gate: none

### T-026 Deliver the contact sheet and preview command

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-009, T-017, T-024, T-025
- Traces to: S-008, S-011, A-002
- Status: assumption-dependent
- Description: The command T-009 decided produces, in a demonstration store only, one contact sheet of 22 labelled tiles meeting the tile identity rule and one preview video of the 22 graphics at the production's spans, each recorded with its hash, and is refused against a company store.
- Acceptance Criteria:
  - In the demonstration store the command produces the sheet and the preview and prints their hashes.
  - Each tile meets the identity rule T-009 states against its graphic's final frame.
  - Against a store designated company the command exits refused before any process starts.
- Gate: none

### T-027 Deliver plan printing and cost lines for graphics

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-013, T-018, T-022
- Traces to: S-007, S-013, A-002
- Status: assumption-dependent
- Description: Plan-only names the renderer and font with their verified hashes, the renderer's licence as recorded with its source, and USD 0.00 planned for graphics, starting no process; the run's summary prints booked metered spend USD 0.00 and local compute for the graphics as unmeasured.
- Acceptance Criteria:
  - Plan-only over fixture files prints the renderer's and font's names, versions and verified hashes and the licence with its source.
  - Plan-only starts no process and writes no file.
  - The summary and every printed cost line state graphics local compute as unmeasured, never zero.
- Gate: none

### T-028 Deliver the boundary assertions

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-012, T-019, T-020, T-026
- Traces to: S-006, S-007, S-010, S-015, A-006
- Status: assumption-dependent
- Description: Every assertion T-012 lists exists in the architecture suite, each failing on its seeded violation, and every delivered assertion keeps its cases unchanged.
- Acceptance Criteria:
  - Each new assertion fails when its seeded violation is introduced and passes without it.
  - The architecture suite's case count exceeds the baseline by the new cases and no delivered case is removed or weakened.
  - Tests needing the installed renderer are marked and skip, naming why, when its path is unset.
- Gate: none

### T-029 Deliver the settings sample entries for the renderer, browser and font

- Owner: omn-dev-1-implement
- Complexity: XS (confidence: high)
- Depends on: T-013, T-018, T-019
- Traces to: S-012, S-013, A-004
- Status: assumption-dependent
- Description: The repository's settings sample carries every new entry T-013 lists with placeholder paths and no expected hash value, naming where the orchestrator takes each value from.
- Acceptance Criteria:
  - Every new settings entry appears in the sample with its meaning.
  - No real path under the company run folder and no expected hash value is written into the sample.
  - Reading the sample with placeholders filled starts verification without a code change.
- Gate: none

### T-030 Deliver the owner guide for previewing and re-producing item 001

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-024, T-026, T-027, T-029
- Traces to: S-012, S-013, A-004
- Status: assumption-dependent
- Description: An owner guide states with exact commands how to verify the renderer, produce the contact sheet and preview in the demonstration store, re-produce item 001 in the company store after the recorded approval and a fresh backup, and what changed on screen, one line per graphic position, with what remains a placeholder.
- Acceptance Criteria:
  - The guide carries every command T-013 lists with its expected exit codes.
  - The guide names the owner's contact-sheet approval and the fresh backup as preconditions of the company-store run.
  - The guide carries 22 lines on what changed on screen and names the clip placeholders and thumbnails as unchanged.
- Gate: none

### T-031 Verify the first drawn graphic end to end in the demonstration store

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: medium)
- Depends on: T-022
- Traces to: S-001, S-006, S-009, A-006, A-011
- Status: assumption-dependent
- Description: Independent evidence shows the first drawn graphic produced end to end through produce in the demonstration store with the installed renderer, its render time recorded as an observation, and nothing written under the installed folders.
- Acceptance Criteria:
  - The evidence records the run's database name, exit code, the graphic artifact's hash and the video's decode result.
  - The evidence records the installed folders' file listing and hashes before and after the run as equal.
  - The evidence records the measured render time labelled as observed on this machine.
- Gate: Review Gate

### T-032 Verify the twenty-two drawn graphics against their criteria

- Owner: omn-dev-2-reviewer
- Complexity: L (confidence: medium)
- Depends on: T-024, T-025
- Traces to: S-001, S-002, S-004, A-002
- Status: assumption-dependent
- Description: Independent evidence shows each of the 22 graphics meets its per-graphic scope criterion as amended by the owner's answers, the specification set meets the scope's specification criteria, and frames change across the span of exactly the twelve listed motion graphics.
- Acceptance Criteria:
  - The evidence lists 22 rows, each naming its criterion, its specification check result and its frame-hash result.
  - The evidence shows the claim cross-check reporting no unanswered disagreement after the owner's answer on the species name.
  - The evidence shows ten still graphics with identical frames throughout their spans.
- Gate: Verification Gate

### T-033 Verify that the text and figures on every frame trace to quotations

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: medium)
- Depends on: T-021, T-024
- Traces to: S-002, S-003, A-002
- Status: assumption-dependent
- Description: Independent evidence shows the text handed to the drawing code equals each specification's quotations, injected unquoted labels, unadmitted numbers, unmarked quantities and markup are refused or drawn literally as designed, every on-screen number matches its quotation and citation, and a character the font lacks is refused.
- Acceptance Criteria:
  - The evidence records each injected fault and the refusal naming it.
  - The evidence lists every on-screen number across the 22 specifications with its quotation and citation.
  - The evidence records the fixture-font refusal and the system-font evidence T-008 names.
- Gate: Verification Gate

### T-034 Verify motion timing from the measured narration

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: medium)
- Depends on: T-023
- Traces to: S-004, A-012
- Status: assumption-dependent
- Description: Independent evidence shows every motion graphic's first and last changing frame inside its segment for two stand-in narrations with different beat durations, and identical graphic outputs when the script's rate, beat starts or specified runtime change.
- Acceptance Criteria:
  - The evidence records each motion graphic's window against its segment for both narrations.
  - The evidence records identical output hashes under altered script values.
  - The evidence records that no motion graphic rendered before its beat durations were measured in the run.
- Gate: Verification Gate

### T-035 Verify the hash round trips of specification, renderer files and graphic output

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: medium)
- Depends on: T-018, T-025
- Traces to: S-005, A-006
- Status: assumption-dependent
- Description: Independent evidence shows a specification altered between hashing and rendering stops the render or is recorded with its new hash, a renderer or font fixture file swapped between verification and render refuses naming both hashes, and a graphic output altered between render and assembly stops the run naming both hashes.
- Acceptance Criteria:
  - The evidence records each of the three alterations and the outcome naming both hashes.
  - The evidence shows no recorded hash ever names bytes other than those read by the next reader.
  - The evidence lists every reader pair the design names and the test covering each.
- Gate: Verification Gate

### T-036 Verify sameness and independence from clock, locale, time zone and system fonts

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-025
- Traces to: S-005, A-006, A-011
- Status: assumption-dependent
- Description: Independent evidence shows the deterministic and non-deterministic stand-ins recorded as designed, and a demonstration render of the approved renderer under two locales and two time zones giving equal output hashes, with any seed found only as a specification value.
- Acceptance Criteria:
  - The evidence records both stand-in outcomes with their sameness statements.
  - The evidence records the two-locale and two-time-zone hashes of the approved renderer, marked as needing the installed renderer.
  - The evidence records a search of the 22 specifications and the drawing code for seeds, clocks and random sources.
- Gate: Verification Gate

### T-037 Verify graphic provenance read back from the store

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: medium)
- Depends on: T-017, T-025
- Traces to: S-005, A-002
- Status: assumption-dependent
- Description: Independent evidence shows every graphic artifact of a demonstration run reads back with every provenance field T-006 lists non-empty and equal to an independent recomputation over the files.
- Acceptance Criteria:
  - The evidence lists 22 graphic artifacts with every provenance field.
  - The evidence recomputes each output and specification hash and finds them equal.
  - The evidence shows renderer and font identity matching the verified files, not configuration.
- Gate: Verification Gate

### T-038 Verify the process boundary, bounds, holds and writes

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: medium)
- Depends on: T-019, T-022, T-028
- Traces to: S-006, A-005, A-006
- Status: assumption-dependent
- Description: Independent evidence shows only the single process-starting type starts a render, a stand-in outlasting its bound leaves no process of its tree including a grandchild, no transaction or datastore command is open during a stand-in render, every produced file lies under the output root through the single writer, and an output root inside the repository is refused.
- Acceptance Criteria:
  - The evidence records a process enumeration after the bound with zero remaining processes of the tree.
  - The evidence records the open-transaction observation during a stand-in render as zero.
  - The evidence lists every timeout, hold and clock T-010 enumerates with the test covering each.
  - The evidence records the installed folders unchanged by a run.
- Gate: Verification Gate

### T-039 Verify that a graphic render reaches no network

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-019, T-024
- Traces to: S-006, S-007, A-002
- Status: assumption-dependent
- Description: Independent evidence shows, as T-011 decided and without any phase changing the machine's security settings, that a demonstration render makes no request beyond the defined loopback, passes no licence key or telemetry option, and never fetches a browser.
- Acceptance Criteria:
  - The evidence records the method used and labels structural evidence as structural.
  - The evidence records the argument list of a demonstration render.
  - The evidence records a render with the browser path removed refused before any process starts.
- Gate: Verification Gate

### T-040 Verify zero spend, no model call and approved files only

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: medium)
- Depends on: T-024, T-027
- Traces to: S-007, A-002
- Status: assumption-dependent
- Description: Independent evidence shows a demonstration production with drawn graphics books no operation, prints USD 0.00 booked and local compute unmeasured, leaves the cap's counted total unchanged, makes zero capability calls with the vendor image path unreached, imports none of the excluded client packages, and that no phase downloaded or installed anything.
- Acceptance Criteria:
  - The evidence records booked operations and the cap total before and after the run.
  - The evidence records a counting stub's zero calls and the import scan's result.
  - The evidence records the implementation report's commands showing no download or install.
- Gate: Verification Gate

### T-041 Verify the contact sheet and preview

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: medium)
- Depends on: T-026
- Traces to: S-008, A-002, A-007
- Status: assumption-dependent
- Description: Independent evidence shows the contact sheet and preview produced in the demonstration store as designed, each tile meeting the identity rule, the preview's one video stream decoded with zero errors at the production's spans, the command refused against a company store, and both files' hashes ready for the owner's approval record.
- Acceptance Criteria:
  - The evidence records each tile's comparison result against its graphic's final frame.
  - The evidence records the preview's stream count, decode result and duration against the production's graphic spans.
  - The evidence records the refusal against a store designated company.
- Gate: Verification Gate

### T-042 Verify the assembly and the narration path unchanged

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: medium)
- Depends on: T-024
- Traces to: S-009, A-012
- Status: assumption-dependent
- Description: Independent evidence shows the produced video has exactly one video and one audio stream decoded with zero errors and its runtime measured from the file beside the specified runtime, the clip placeholders and thumbnails as delivered, the item held short of publish-ready, every delivered narration test passing unmodified, and every segment boundary unchanged for fixed beat durations.
- Acceptance Criteria:
  - The evidence records the stream counts, decode result and measured runtime of a demonstration run.
  - The evidence records the delivered narration tests passing with no modification in their files.
  - The evidence records the timeline regression over fixed durations before and after the change.
- Gate: Verification Gate

### T-043 Verify re-pointability of the renderer

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: medium)
- Depends on: T-016, T-020, T-028
- Traces to: S-010, A-009
- Status: assumption-dependent
- Description: Independent evidence shows a fixture specification set for a different subject renders through the same code with no change, and the drawing code holds no subject term of the item material and no position identifier.
- Acceptance Criteria:
  - The evidence records the non-subject fixture render's outputs.
  - The evidence records the subject-term scan's sources and result.
  - The evidence records that no code file changed between the item 001 and fixture renders.
- Gate: Verification Gate

### T-044 Verify store isolation and admission

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: high)
- Depends on: T-017, T-024, T-026
- Traces to: S-011, A-007
- Status: assumption-dependent
- Description: Independent evidence shows every test and demonstration of this change connected only to the demonstration database, a production with drawn graphics is admitted on a company-designated store in own mode as on a demonstration store, and the fake source and the contact-sheet command stay refused there.
- Acceptance Criteria:
  - The evidence lists every test configuration and demonstration connection with its database name.
  - The evidence records the store-guard unit results over designation and mode.
  - The evidence records the contact-sheet refusal on a company designation.
- Gate: Verification Gate

### T-045 Verify the eleventh resource on a store of the company store's recorded shape

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: medium)
- Depends on: T-017
- Traces to: S-013, A-010
- Status: assumption-dependent
- Description: Independent evidence shows the eleventh resource applied alone, and again, to a demonstration store prepared to the company store's recorded shape, with every delivered row unchanged and the second application changing nothing.
- Acceptance Criteria:
  - The evidence records the prepared store's ten resources and rows before application.
  - The evidence records a row-by-row comparison after each application showing no delivered row changed.
  - The evidence records the second application's outcome as changing nothing.
- Gate: Verification Gate

### T-046 Verify the owner guide and settings sample by running every documented command

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: medium)
- Depends on: T-030
- Traces to: S-012, S-013, A-004
- Status: assumption-dependent
- Description: Independent evidence shows each command the guide documents run in the demonstration store with the settings sample's entries filled exits as documented, and the guide and sample need no code change, no secret and no network to produce item 001 with drawn graphics.
- Acceptance Criteria:
  - The evidence lists every documented command with its observed exit code beside the documented one.
  - The evidence records the guide's 22 on-screen lines checked against the per-graphic criteria.
  - The evidence records that the company-store steps name the approval and the fresh backup first.
- Gate: Verification Gate

### T-047 Confirm the binding constraints hold after the change

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: medium)
- Depends on: T-015, T-024, T-025, T-026, T-027, T-028, T-030
- Traces to: S-007, S-015, A-001, A-006, A-007
- Status: assumption-dependent
- Description: Independent evidence shows every test passing at the baseline still passes in the demonstration store, the default suite runs with the renderer path unset, nothing publishes, no phase downloaded anything or touched the company store or the installed folders, and no on-screen figure lacks a recorded source.
- Acceptance Criteria:
  - The evidence compares the full live suite's totals with the baseline T-015 recorded.
  - The evidence records the default suite's result with the renderer path unset and the skipped tests named.
  - The evidence records the delivered structural-absence and held-stage tests passing.
- Gate: Review Gate

### T-048 Record what the change delivers and the orchestrator's post-phase run

- Owner: omn-documentation
- Complexity: M (confidence: medium)
- Depends on: T-004, T-041, T-045, T-046, T-047
- Traces to: S-013, S-014, A-010
- Status: assumption-dependent
- Description: The release record states what the change delivers, dispositions by name the previous record's entries on the text cards, render repeatability, the Design stage against the stage-handler contract and the audio stream ending up to one frame early, records metered spend USD 0.00 and local compute unmeasured, and orders the orchestrator's post-phase steps: the owner's contact-sheet and palette approval with hashes, fresh backup, readings, eleventh resource alone, settings written outside the repository, renderer verification, plan-only, the owner's go, production and readings after the run.
- Acceptance Criteria:
  - The record names each carried entry and its disposition.
  - The record orders every post-phase step with the scope criteria each step evidences.
  - The record lists the expected-hash sources the orchestrator uses and labels every demonstration figure as a demonstration.
- Gate: Closure Gate

## Dependencies

### 8.1 Dependency Edges

| From | To | Type | Justification |
|---|---|---|---|
| T-002 | T-003 | decision-gate | The verified file set depends on what the render starts and loads |
| T-004 | T-005 | decision-gate | Timing depends on where graphics render relative to narration |
| T-001 | T-006 | contract | The specification hash binds to the specification form |
| T-003 | T-006 | contract | File hash fields are those the verification computes |
| T-005 | T-006 | contract | Output hash fields name the output form |
| T-006 | T-007 | decision-gate | Store records hold the provenance fields |
| T-009 | T-007 | decision-gate | Store roles include the contact sheet and preview |
| T-002 | T-008 | decision-gate | Font loading depends on how the browser is started |
| T-005 | T-009 | contract | Tiles are final frames of the decided output form |
| T-010 | T-009 | contract | The sheet and preview runs take their bounds from the enumeration |
| T-002 | T-010 | decision-gate | Render and bundling holds are enumerated from that design |
| T-003 | T-010 | decision-gate | Renderer-file hashing holds are enumerated from that design |
| T-004 | T-010 | decision-gate | Stage holds are enumerated from the run order |
| T-005 | T-010 | decision-gate | Motion render holds are enumerated from the timing design |
| T-002 | T-011 | decision-gate | Network paths depend on how the render is started |
| T-001 | T-012 | decision-gate | Literal and subject assertions read the decided drawing inputs |
| T-002 | T-012 | decision-gate | Starter, writer and import assertions read the decided render sources |
| T-003 | T-013 | contract | Commands include the decided verification |
| T-007 | T-013 | contract | Commands include applying the decided resource |
| T-009 | T-013 | contract | Commands include the decided contact-sheet command |
| T-001 | T-016 | contract | The check implements the decided form |
| T-007 | T-017 | contract | The resource implements the decided store change |
| T-003 | T-018 | contract | The verification implements the decided file set |
| T-002 | T-019 | contract | The invocation implements the decided start and confinement |
| T-010 | T-019 | contract | The invocation applies the ruled bound |
| T-011 | T-019 | contract | The invocation applies the network prevention |
| T-001 | T-020 | contract | The drawing code draws the decided element types |
| T-002 | T-020 | contract | The drawing code imports only the decided packages |
| T-008 | T-020 | contract | The drawing code loads the decided font |
| T-014 | T-020 | produces-consumes | The drawing code reads the proposed palette values |
| T-008 | T-021 | contract | The refusal implements the decided coverage check |
| T-016 | T-021 | produces-consumes | The refusal reads characters from checked specifications |
| T-004 | T-022 | contract | The graphic renders at the decided stage |
| T-016 | T-022 | produces-consumes | The run draws a checked specification |
| T-018 | T-022 | produces-consumes | The run verifies installed files before the first graphic |
| T-019 | T-022 | produces-consumes | The run starts the render through the delivered invocation |
| T-020 | T-022 | produces-consumes | The run draws with the delivered drawing code |
| T-005 | T-023 | contract | Motion follows the decided timing and assembly form |
| T-017 | T-023 | produces-consumes | Motion outputs are recorded under the added role |
| T-022 | T-023 | produces-consumes | Motion extends the delivered graphic path |
| T-021 | T-024 | produces-consumes | All specifications pass the font coverage refusal |
| T-023 | T-024 | produces-consumes | The twelve motion graphics need the motion path |
| T-006 | T-025 | contract | Provenance implements the decided fields |
| T-017 | T-025 | produces-consumes | Provenance is recorded in the added records |
| T-022 | T-025 | produces-consumes | Provenance attaches to the delivered graphic artifacts |
| T-009 | T-026 | contract | The command implements the decided sheet and preview |
| T-017 | T-026 | produces-consumes | The sheet and preview are recorded under the added roles |
| T-024 | T-026 | produces-consumes | The sheet needs all 22 graphics |
| T-025 | T-026 | produces-consumes | The sheet prints hashes from recorded provenance |
| T-013 | T-027 | contract | Plan lines follow the decided commands |
| T-018 | T-027 | produces-consumes | Plan-only prints the verification's hashes |
| T-022 | T-027 | produces-consumes | The summary covers the delivered graphic path |
| T-012 | T-028 | contract | Assertions implement the decided list |
| T-019 | T-028 | produces-consumes | Assertions read the delivered invocation |
| T-020 | T-028 | produces-consumes | Assertions read the delivered drawing code |
| T-026 | T-028 | produces-consumes | The demonstration-only assertion reads the delivered command |
| T-013 | T-029 | contract | The sample carries the decided entries |
| T-018 | T-029 | produces-consumes | The sample names the verification's settings |
| T-019 | T-029 | produces-consumes | The sample names the invocation's settings |
| T-024 | T-030 | produces-consumes | The guide's on-screen lines describe the 22 delivered graphics |
| T-026 | T-030 | produces-consumes | The guide documents the delivered sheet command |
| T-027 | T-030 | produces-consumes | The guide documents plan-only output |
| T-029 | T-030 | produces-consumes | The guide refers to the sample's entries |
| T-022 | T-031 | verification | Evidences the first drawn graphic |
| T-024 | T-032 | verification | Evidences the 22 graphics |
| T-025 | T-032 | verification | Frame and output hashes come from provenance |
| T-021 | T-033 | verification | Evidences the font refusal |
| T-024 | T-033 | verification | Evidences the quotations of the 22 specifications |
| T-023 | T-034 | verification | Evidences motion timing |
| T-018 | T-035 | verification | Evidences the renderer-file swap refusal |
| T-025 | T-035 | verification | Evidences specification and output hash round trips |
| T-025 | T-036 | verification | Evidences the sameness record |
| T-017 | T-037 | verification | Reads provenance from the added records |
| T-025 | T-037 | verification | Evidences recorded provenance |
| T-019 | T-038 | verification | Evidences the invocation's bound and termination |
| T-022 | T-038 | verification | Evidences no open transaction during a produce render |
| T-028 | T-038 | verification | Evidences the starter and writer assertions |
| T-019 | T-039 | verification | Evidences the invocation's argument list |
| T-024 | T-039 | verification | Evidences a full demonstration render |
| T-024 | T-040 | verification | Evidences spend and calls of a full production |
| T-027 | T-040 | verification | Evidences printed cost lines |
| T-026 | T-041 | verification | Evidences the sheet and preview |
| T-024 | T-042 | verification | Evidences the assembly of all 22 graphics |
| T-016 | T-043 | verification | Evidences the check over a non-subject set |
| T-020 | T-043 | verification | Evidences the drawing code over a non-subject set |
| T-028 | T-043 | verification | Evidences the subject-term assertion |
| T-017 | T-044 | verification | Evidences admission with the added records |
| T-024 | T-044 | verification | Evidences a production on a company designation |
| T-026 | T-044 | verification | Evidences the sheet command's refusal |
| T-017 | T-045 | verification | Evidences the resource over the company store's shape |
| T-030 | T-046 | verification | Evidences every documented command |
| T-015 | T-047 | verification | Compares with the recorded baseline |
| T-024 | T-047 | verification | Reads the finished graphics path |
| T-025 | T-047 | verification | Reads the finished provenance path |
| T-026 | T-047 | verification | Reads the finished sheet command |
| T-027 | T-047 | verification | Reads the finished plan and summary |
| T-028 | T-047 | verification | Reads the finished assertions |
| T-030 | T-047 | verification | Reads the finished guide |
| T-004 | T-048 | produces-consumes | The record quotes the stage-handler position |
| T-041 | T-048 | produces-consumes | The record names the sheet and preview hashes for approval |
| T-045 | T-048 | produces-consumes | The record cites the resource evidence |
| T-046 | T-048 | produces-consumes | The record cites the command evidence |
| T-047 | T-048 | produces-consumes | The record cites the constraint evidence |
| omn-orchestrator, for the demonstration database | T-015 | external | The baseline runs against the demonstration database |
| omn-orchestrator, for the demonstration database | T-031 | external | The first-graphic run uses the demonstration database |
| omn-orchestrator, for the demonstration database | T-041 | external | The sheet and preview come from a demonstration production |
| omn-orchestrator, for the demonstration database | T-047 | external | The live suite runs against the demonstration database |
| omn-orchestrator, for the installed renderer, browser and font | T-031 | external | The first graphic renders with the installed files |
| omn-orchestrator, for the installed renderer, browser and font | T-036 | external | The two-locale render uses the installed files |
| omn-orchestrator, for the installed renderer, browser and font | T-041 | external | The sheet's graphics render with the installed files |
| omn-orchestrator, for the installed media tool | T-031 | external | The assembly and decode run the installed media tool |
| omn-orchestrator, for the installed media tool | T-042 | external | Stream counts and decode run the installed media tool |

### 8.2 External Dependencies

| Responsible party | What is needed | Blocks |
|---|---|---|
| omn-orchestrator, for the demonstration database | PostgreSQL 17 serving the separate demonstration database, and no other database named by any test or demonstration (R-021, R-026) | T-015, T-031, T-041, T-047 |
| omn-orchestrator, for the installed renderer, browser and font | The renderer packages, browser and font installed on 2026-10-11 outside the repository, unchanged, with an expected-hash record for every file T-003 names that the decision record does not already hash (R-025, Q-001) | T-031, T-036, T-041 |
| omn-orchestrator, for the installed media tool | The media tool at version 9.0.2 with its probe, installed and located by configuration (R-027) | T-031, T-042 |
| The owner, through omn-orchestrator | The recorded approval of the contact sheet and palette naming their hashes, a fresh backup of the company store, the eleventh resource applied alone, the run's settings written outside the repository, renderer verification, plan-only, and the explicit go (R-004, R-028) | none in this plan; the post-phase run after this change closes |

### 8.3 Implementation Order

- Wave 1: T-001, T-002, T-004, T-014, T-015
- Wave 2: T-003, T-005, T-008, T-011, T-012, T-016
- Wave 3: T-006, T-010, T-018, T-020, T-021
- Wave 4: T-009, T-019
- Wave 5: T-007, T-022
- Wave 6: T-013, T-017, T-031
- Wave 7: T-023, T-025, T-027, T-029, T-045
- Wave 8: T-024, T-034, T-035, T-036, T-037
- Wave 9: T-026, T-032, T-033, T-039, T-040, T-042
- Wave 10: T-028, T-030, T-041, T-044
- Wave 11: T-038, T-043, T-046, T-047
- Wave 12: T-048

The first real graphic is drawn end to end through produce in the demonstration store in wave 5 (T-022) and evidenced in wave 6 (T-031); motion timed from measured narration follows in wave 7 (T-023), all 22 graphics in wave 8 (T-024), and the owner's contact sheet and preview in wave 9 (T-026), evidenced in wave 10 (T-041) for the owner's review. The orchestrator's post-phase preparation is complete when T-045 (wave 7) has proven the eleventh resource over the company store's shape, T-018 (wave 3) and T-029 (wave 7) have delivered the renderer verification command and the settings sample entries for the Node executable, render script, renderer, browser and font, T-027 (wave 7) has delivered plan-only naming the verified files, T-030 (wave 10) the guide, and T-046 (wave 11) has run every documented command.

## Suggested Workflow

Selected workflow: implement-feature

Selected because the change delivers new capability against an approved scope with measurable acceptance criteria, and the run is already routed through this workflow's phase model.

| Phase | Tasks |
|---|---|
| scope-and-acceptance | none; the phase is complete and its artifact is this plan's upstream input |
| execution-planning | none; this plan is the phase's own output |
| solution-design-and-risk-assessment | T-001, T-002, T-003, T-004, T-005, T-006, T-007, T-008, T-009, T-010, T-011, T-012, T-013, T-014 |
| implementation | T-015, T-016, T-017, T-018, T-019, T-020, T-021, T-022, T-023, T-024, T-025, T-026, T-027, T-028, T-029, T-030 |
| quality-review | T-031, T-032, T-033, T-034, T-035, T-036, T-037, T-038, T-039, T-040, T-041, T-042, T-043, T-044, T-045, T-046, T-047 |
| documentation-and-release-handoff | T-048 |

| Gate | Required owners |
|---|---|
| Scope Gate | omn-product-owner, omn-business-analyst |
| Planning Gate | omn-tech-lead, omn-orchestrator |
| Design Gate | omn-architect, omn-tech-lead |
| Review Gate | omn-dev-2-reviewer, omn-qa |
| Verification Gate | omn-qa |
| Closure Gate | omn-orchestrator, omn-documentation |

## Required Capabilities

### 10.1 Agent Capabilities

| Capability | Tasks | Owning agent | Proficiency |
|---|---|---|---|
| architecture-analysis | T-001, T-002, T-003, T-004, T-005, T-006, T-007, T-008, T-009, T-010, T-011, T-012, T-013, T-014 | architect | Primary |
| technical-approach-definition | T-001, T-002, T-003, T-005, T-008, T-009, T-013 | architect | Primary |
| option-evaluation | T-002, T-004, T-005, T-009, T-011 | architect | Primary |
| structural-risk-analysis | T-002, T-003, T-006, T-010, T-011, T-012 | architect | Primary |
| reuse-assessment | T-003, T-004, T-007, T-010, T-012 | architect | Primary |
| implementation-delivery | T-015, T-016, T-017, T-018, T-019, T-020, T-021, T-022, T-023, T-024, T-025, T-026, T-027, T-028, T-029, T-030 | omn-dev-1-implement | Primary |
| quality-verification | T-031, T-032, T-033, T-034, T-035, T-036, T-037, T-038, T-039, T-040, T-041, T-042, T-043, T-044, T-045, T-046, T-047 | omn-dev-2-reviewer | Primary |
| validation-design | T-032, T-034, T-035, T-036, T-038, T-045 | omn-dev-2-reviewer | Primary |
| code-review | T-038, T-040, T-043, T-044, T-046, T-047 | omn-dev-2-reviewer | Primary |
| documentation | T-048 | omn-documentation | Primary |
| release-communication | T-048 | omn-documentation | Primary |

### 10.2 Required Skills

| Skill | File | Tasks | Level |
|---|---|---|---|
| S01 | skills/architecture/clean-architecture-checklist.md | T-002, T-003, T-004, T-012, T-028 | Advisory |
| S02 | skills/business/domain-modeling.md | T-001, T-006, T-009, T-013, T-014, T-016, T-024 | Primary |
| S03 | skills/dotnet/engineering-playbook.md | T-016, T-017, T-018, T-019, T-020, T-021, T-022, T-023, T-024, T-025, T-026, T-027, T-028 | Secondary |
| S06 | skills/database/database-engineering.md | T-007, T-010, T-017, T-045 | Secondary |
| S07 | skills/testing/testing-strategy.md | T-015, T-031, T-032, T-033, T-034, T-035, T-036, T-037, T-038, T-039, T-040, T-041, T-042, T-043, T-044, T-045, T-046, T-047 | Secondary |
| S08 | skills/performance/performance-engineering.md | T-010, T-031 | Advisory |
| S09 | skills/security/secure-engineering.md | T-002, T-003, T-011, T-018, T-019, T-020, T-039, T-040 | Advisory |
| S10 | skills/git/git-collaboration.md | T-015, T-030, T-048 | Advisory |
| S11 | skills/logging/observability-logging.md | T-027, T-048 | Advisory |
| S12 | skills/error-handling/error-handling-strategy.md | T-010, T-019, T-021 | Advisory |

## Acceptance Criteria

1. In the demonstration store, a produce run of item 001 shows a drawn graphic at all 22 positions meeting each per-graphic scope criterion as amended by the owner's answers, exactly the twelve listed motion graphics moving inside segments bounded by the run's measured beat durations, one video decoded end to end with zero errors with 18 labelled clip placeholders and the item held short of publish-ready; after the phases, the orchestrator's company-store run shows the same. Verifies business objective 1. Evidence: the evidence recorded in T-031, T-032, T-034 and T-042, and the orchestrator's run record for scope criterion 67.
2. Every on-screen text element and number across the 22 graphics matches a verbatim quotation with its source and citation, every schematic element carries the owner's disclosure, each injected unquoted or unmarked element is refused naming it, and no font other than the approved one draws a character. Verifies business objective 2. Evidence: the evidence recorded in T-032 and T-033.
3. Every graphic artifact reads back its specification, output, renderer, browser and font hashes equal to independent recomputation, each round-trip alteration is refused naming both hashes, sameness is recorded only from compared renders, and in the company-store run each still's output hash equals its approved demonstration render. Verifies business objective 3. Evidence: the evidence recorded in T-035, T-036 and T-037, and the orchestrator's reading for scope criterion 69.
4. A demonstration production with drawn graphics books no operation, prints USD 0.00 and local compute unmeasured, leaves the cap's counted total at USD 0.007695 estimate, makes no capability call, passes no licence key, reaches no network beyond the design's defined loopback, leaves no process running and changes no installed file, and no phase downloaded anything. Verifies business objective 4. Evidence: the evidence recorded in T-031, T-038, T-039 and T-040.
5. A contact sheet of 22 labelled tiles meeting the identity rule and a preview of the 22 graphics at their production spans are produced from the demonstration store with their hashes, and the owner's approval naming those hashes is recorded before any company-store production. Verifies business objective 5. Evidence: the evidence recorded in T-041 and the owner's approval record the release record names.
6. No test, demonstration or phase connects to the company store; the eleventh resource applies and re-applies over a store of the company store's shape changing no delivered row; every documented command exits as documented; and the release record orders every step of the orchestrator's run. Verifies business objective 6. Evidence: the evidence recorded in T-044, T-045 and T-046, and the release record produced by T-048.
7. A non-subject fixture specification set renders through unchanged code, every test passing at the baseline still passes, the architecture suite grows by the new assertions each failing on its seeded violation, nothing publishes, and the release record dispositions the carried entries on text cards, render repeatability, the Design stage's form and the audio stream's end. Verifies business objective 7. Evidence: the baseline recorded by T-015, the evidence recorded in T-043 and T-047, and the release record produced by T-048.

## Definition of Done

- [ ] All seven plan acceptance criteria are verified with recorded evidence, the company-store parts of criteria 1 and 3 by the orchestrator's post-phase run record
- [ ] Design Gate, Review Gate, Verification Gate and Closure Gate are approved with owners recorded
- [ ] Every task acceptance criterion is satisfied or formally waived with the waiver recorded
- [ ] Every assumption is confirmed by its named role or converted to a recorded decision
- [ ] Every risk is closed or accepted with its named owner recorded
- [ ] Every open question is closed or explicitly accepted by its named owner
- [ ] Metered spend in the phases is recorded as USD 0.00, local compute as unmeasured, and the cap's counted total as USD 0.007695 estimate
- [ ] No phase connection to the company store, no download and no change under the installed renderer, browser or font folders is recorded in any phase's evidence
- [ ] The owner guide, the settings sample entries and the release record with its release-impact notes are published
- [ ] Durable outcomes are recorded to memory per `memory/memory-governance.md`

## Open Questions

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| Q-001 | Scope criterion 35 requires the SHA-256 of "every renderer file" taken from the files loaded, and criterion 48 verification against hashes recorded at installation; the owner's decision record hashes only the lock file, the browser executable and two font files, while a render loads the Node executable, the company's render script and drawing code, about 236 MB of installed packages including encoder binaries, and the browser folder's libraries and resources. Which files form the verified set, and where are expected values for the rest recorded and by whom? | no | architect | T-003, T-018, T-035 |
| Q-002 | The orchestrator's trial showed the bundler writing about 170 MB of cache under the installed packages, and the browser writes a profile and temporary files; scope criterion 43 requires every file a render produces under the output root through the single writer, and nothing installed may change. Where do those writes go, and how is an already present cache excluded from the verified set? | no | architect | T-002, T-003, T-019, T-038 |
| Q-003 | The renderer serves the bundle to the browser over its own loopback server; does scope criterion 44's "zero network requests" exclude loopback, and what evidence of no other request does a demonstration record without any phase changing the machine's security settings? | no | architect | T-011, T-039 |
| Q-004 | Scope criterion 29 verifies no system font through "the renderer's loaded-file record", but the browser enumerates installed system fonts at start and keeps no loaded-file record; what evidence establishes that no system or fallback font drew a character? | no | architect | T-008, T-021, T-033 |
| Q-005 | Scope criterion 50 asks one contact-sheet image of 22 labelled tiles and criterion 51 each tile byte-identical to its graphic's final frame; a labelled or scaled tile inside one image cannot be byte-identical to a full 1920 by 1080 frame. Is identity defined over decoded pixels of a full-scale tile region, or are tiles kept as separate files the sheet composes? | no | architect | T-009, T-026, T-041 |
| Q-006 | Scope criterion 30 states the on-screen numbers are "today only claim four's 13–15 Hz", which predates the owner's admission of "~2 km", "60°", "nine o'clock" and "noon" with their citations, and the end card's citations carry publication years; which characters count as an on-screen number, and how is each matched to its quotation so the extraction test is decidable? | no | architect | T-001, T-016, T-033 |
| Q-007 | The delivered assembly joins PNG stills through a segment list and encodes with a still-image tuning; how do motion graphics enter the assembly so the file the assembly reads is the file whose hash is recorded (criterion 36) and every segment boundary stays identical (criterion 56)? | no | architect | T-005, T-023, T-042 |
| Q-008 | The delivered run executes Design before Audio, while motion timed from the measured narration needs graphics after Audio (ruled to the Design Gate as the scope gate's eighth objection); does Design move after Audio, split into still and motion parts, or does motion render at Production, and how does the Design stage then stand against the stage-handler contract (the scope's architect question)? | no | architect | T-004, T-022, T-023, T-048 |
| Q-009 | What values govern the per-graphic render bound, any bundling bound, the renderer-file hashing bound and the contact-sheet and preview bounds? Non-blocking: architect proposes in T-010 from the orchestrator's trial and omn-tech-lead rules at the Design Gate (A-005); no value is invented here. | no | omn-tech-lead | T-010, T-019, T-026 |
| Q-010 | Rendering every graphic twice for the sameness record doubles graphic render time, which at production spans of about 375 s is unmeasured beyond the trial's rate; is the comparison made for every graphic in every run, in demonstration runs only, or for a named subset, and how is the company-store run's still equality to approved renders (criterion 69) recorded? | no | architect | T-006, T-025, T-036, T-037 |
| Q-011 | Does the owner approve the palette values T-014 proposes and every graphic at the contact-sheet review, and if not, which change does the owner record? Non-blocking for decomposition; a rejection re-renders from data (R-004). | no | omn-product-owner | T-014, T-024, T-041, T-048 |
| Q-012 | The installed dependency tree carries the renderer's client packages for a speech vendor and a speech-recognition model, pulled in by its studio and command-line packages; which packages may the render script and drawing code load, and how is it shown that those clients are never imported or called? | no | architect | T-002, T-012, T-028, T-040 |
| Q-013 | Where is the owner's contact-sheet approval naming its hash recorded (scope criterion 66): in the company store, which makes recording it a write before the go, or in the decision record outside any store, and how does the company-store run read the approved still hashes it compares against? | no | architect | T-006, T-013, T-048 |

## Traceability Matrix

| Statement | Covered by |
|---|---|
| S-001 | T-014, T-020, T-022, T-024, T-031, T-032, A-002 |
| S-002 | T-001, T-016, T-024, T-032, T-033, A-008, Q-006 |
| S-003 | T-001, T-008, T-016, T-020, T-021, T-033, Q-004 |
| S-004 | T-004, T-005, T-023, T-032, T-034, A-012, Q-008 |
| S-005 | T-003, T-006, T-007, T-017, T-018, T-025, T-035, T-036, T-037, A-011, Q-001, Q-010 |
| S-006 | T-002, T-010, T-011, T-012, T-019, T-022, T-028, T-031, T-038, T-039, A-005, A-006, Q-002, Q-003, Q-009 |
| S-007 | T-003, T-011, T-012, T-013, T-018, T-027, T-028, T-040, T-047, A-002, Q-012 |
| S-008 | T-009, T-026, T-041, Q-005, Q-011 |
| S-009 | T-004, T-005, T-022, T-023, T-024, T-031, T-042, Q-007 |
| S-010 | T-001, T-002, T-012, T-016, T-020, T-028, T-043, A-009 |
| S-011 | T-007, T-017, T-026, T-044, A-007 |
| S-012 | T-013, T-029, T-030, T-046, A-004 |
| S-013 | T-003, T-007, T-013, T-017, T-018, T-027, T-029, T-030, T-045, T-046, T-048, A-010, Q-013 |
| S-014 | T-004, T-006, T-048 |
| S-015 | T-012, T-015, T-028, T-047, A-001 |

Statement register, normalised from the supplied feature request and the upstream scope definition of this run (digest sha256:ec59d3eeff2c8e7cf38f64a495cc31db): the first fourteen statements match the upstream in-scope items in order and carry the same identifiers; S-015 carries the request's requirement that the ten completed waves are built upon and none re-created. The scope's open questions were answered by the owner on 2026-10-11 and are carried as A-002; Q-001 to Q-013 are raised here and none reuses a scope question's subject under its identifier. Upstream acceptance criteria are cited by ordinal in the order the scope definition lists them, because their identifiers share this plan's assumption prefix. The renderer, browser and font are named where the owner's decision of 2026-10-11 names them. Repository rescan: the task context names no relevant module or file, so the delivered source in the feature worktree at head 19e2107 was read as the dispatch briefing directs: the one process starter starts the media tool's renderer and probe and the in-house model's interpreter, draws each graphic as a centred text still on a dark colour source with the configured font, and joins stills through a segment list encoded with still-image tuning; the produce service runs Design, Audio, Thumbnail, Production in that order and records each graphic as the existing still role; the closed artifact role set in the ninth resource has seven roles and the tenth adds none; the model installation verifier hashes and holds configured files and reads identity from verified bytes; counts are 140 C# sources and 8 project files under the source tree, 61 C# sources and 7 project files under the test tree, ten schema resources, and 65 test attributes in the architecture project, matching the scope's 60 single and 5 data-driven methods. The installed renderer folder was read without change: its manifest pins the four renderer packages at 4.0.534 and the two interface packages at 19.3.0, and the trial's render script bundles from its own folder and passes the browser path explicitly.
