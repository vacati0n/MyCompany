```yaml
plan:
  planId: PLAN-2026-0009
  sourceInputs:
    - type: feature-request
      reference: tasks/MC-10/input.md
  producedBy: planner
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  inputDigest: sha256:36c0c7375cc1bc44da2fe481546f87ea
  contextDigest: sha256:62e7676afa2df10b3af32629f294e88a
```

## Executive Summary

This plan decomposes the production capability into sixty-five tasks across ten execution waves, for the CEO and sole owner, who must be able to release the company's first video as one bounded act after seeing the same video produced end to end at no cost. Success is that one command turns item 001's recorded package into a decodable video file under one output root, with its runtime measured from the file, every stage outcome and every operation recorded, the spend counted against a USD 5.95 per-item cap reserved at worst case before each call and reconciled at booking, demonstrated at USD 0.00 with fake providers in the demonstration store, and switchable to the owner's billed pay-per-use vendors by configuration and owner-set credentials alone, with nothing published. Nineteen architect decision tasks lead; twenty-two implementation tasks, twenty-three verification tasks and one documentation task follow, and the zero-spend end-to-end run (T-032, verified by T-042) has no vendor adapter on its path, so the vendor adapters are built and proven against recorded fixtures and stub transports beside it rather than before it. The highest-impact risk is the round-trip and same-clock defect class this programme has missed until review in four waves: the delivered adapter books a timed-out or usage-less attempt at zero units as a measurement, the controller decides on booked spend only so a call whose worst case passes the cap or the ceiling is admitted, and the render, the stage records and the credential handle each run on a different clock or bound from the admission (R-006, R-007, R-010, R-011); T-008 enumerates every timeout, hold and clock first, and T-063 verifies the round trip. Seven facts from the delivered source and the scope are raised as objections: metered-mode criteria require stub-transport metered runs that the company-store-only refusal forbids in any test (Q-008); credential variable names embed a channel identifier that does not exist until the company store holds a channel row, and nothing loads the company store's records beyond its schema (Q-004); one outcome per stage per item version blocks a re-run (Q-007); the probe, the decode and the renderer's own output writing sit outside a literal reading of one process starter and one file writer (Q-006); a delivered name scan forbids the transport fragment in every production type; the documentation phase writes no repository file, so the owner guide is an implementation task (A-005); and the vendor shapes can only be fixed from recorded knowledge, never at first hand, inside a phase (Q-009). Plan status is complete: every in-scope statement is decomposed, and none of the ten open questions blocks decomposition.

## Business Objectives

1. The owner sees the company's first video produced end to end at no cost before any money moves: one command yields one decodable video file with its runtime measured from the file and every stage outcome and operation recorded. Received by the CEO and sole owner. Measured by a recorded fake-mode run in a fresh demonstration store. Traces to S-001, S-002, S-007, S-008, S-009, S-010.
2. The owner can release the single metered run as one bounded act, knowing before any call what will be called, its estimate labelled as such, the cap, and the exact credential variables to set, with no code change between the zero-spend and the metered run. Received by the owner and by the orchestrator who executes the run. Measured by the plan-only output, the metered refusal naming each unmet condition, and the owner guide compared with that output. Traces to S-004, S-016, S-017, S-025.
3. The owner's spend on the video never passes USD 5.95 and every attempt, retry, failure and recovery counts, inside the USD 34.42 company ceiling. Received by the owner. Measured by stub-priced runs in which the cap binds mid-production, per attempt kind and for a concurrent pair. Traces to S-014, S-015, S-021.
4. Nothing the run produces can be published, the item stays held short of publish-ready under its existing conditions, and no excluded service is ever used. Received by the owner. Measured by the publish-ready refusal after a complete run, the boundary assertions, and refusals with an excluded service as the only candidate. Traces to S-005, S-012.
5. The owner never mistakes a demonstration for an observation, and sees the produced item, its operations and its spend against the cap in the weekly brief and the dashboard. Received by the owner. Measured by the brief and dashboard over a demonstration store after a fake run. Traces to S-006, S-020.
6. The company's records about produced work stay trustworthy and its software stays re-pointable to any later item, with every delivered control extended and none weakened. Received by the owner and every later item. Measured by stored-file hashes recomputed after the run, the architecture suite, and the live suite in the demonstration store. Traces to S-003, S-011, S-013, S-018, S-023, S-024.
7. The owner's billed reasoning and image accounts are usable by a later item without further build. Received by the owner. Measured by contract checks of both paths against dated fixtures and fakes. Traces to S-019.
8. The owner reads the decisions of 2026-10-09 as answered in the register and the two still-open questions as open. Received by the owner. Measured by a read of the register entries beside the decision record. Traces to S-022.

## Technical Objectives

1. A completed capability call returns its content with its operation record, and the content's recorded length and hash equal the bytes the caller receives; any other outcome returns no content. Verified by tests against fakes and stub transports. Traces to business objectives 1 and 6.
2. Each vendor path sends the vendor's request shape under its own authentication scheme and reads billed units from the response body, with the secret never leaving the transport message. Verified by contract tests against dated recorded fixtures and a fake-secret search over every output. Traces to business objectives 2 and 7, and to A-008.
3. Fake providers return deterministic content at zero cost, reach no network, are selectable only in fake mode against a demonstration store, and are never reached by a metered run. Verified by a failing-on-network transport, hash comparison over two runs and mode-mismatch refusals. Traces to business objective 5, and to A-009.
4. Every call is admitted against the item's remaining cap and the company ceiling at its worst case before it is made, inside the admission transaction, and its booking reconciles that reservation; an attempt whose charge is unknown is booked at its admitted worst case labelled estimate, never at zero. Verified by stub-priced runs per attempt kind and a concurrent pair. Traces to business objective 3.
5. Every unit kind a vendor bills a request by is costed from a price carrying its source and date, a route is selectable only when every billed kind is priced, and an unbilled kind needs no price. Verified by booking tests with and without the cached-unit price. Traces to business objective 3.
6. The Audio, Design, Thumbnail and Production stages produce their artifacts from the recorded package and its traced item material, deterministically where no vendor is called, with zero capability calls outside narration. Verified by call-log counts and repeated-run hashes. Traces to business objectives 1 and 6, and to A-006 and A-011.
7. Exactly one type starts an external process, under a stated time bound, with no datastore transaction open while it runs; exactly one type writes files, only under the configured output root, recording each artifact once completely written with the hash of the stored bytes; the runtime is measured from the rendered file. Verified by architecture tests, an injected interruption, a stalled stand-in renderer and an open-transaction observation. Traces to business objectives 1 and 6.
8. The produce command offers fake, plan-only and metered modes, prints the plan, the estimate and the cap before any call, and refuses metered work before any call naming each unmet precondition, including any store other than the designated company store. Verified per mode and per refusal condition. Traces to business objective 2.
9. Every timeout, hold and clock on the produce path is enumerated, each wait ends under a named outcome, and every instant the cap, the booking and the stage records decide by is taken on one clock. Verified by a stalled provider, a held admission, a lost transaction and a skewed process clock during a run. Traces to business objective 3.
10. The boundary assertions cover every type this change adds, including the process starter, the file writer, the subject-term scan and the absence of any publication path, and none is weakened. Verified by a solution build failing on a seeded violation of each. Traces to business objectives 4 and 6.
11. The brief, the dashboard and the register read the produced item, its operations, its spend against the cap and the answered decisions from the delivered single read, with demonstration lines labelled. Verified over a demonstration store after a fake run. Traces to business objectives 5 and 8.

## Scope

### In Scope

- S-001: One owner command turns a recorded item package into a rendered video file, recording every stage outcome and every operation, without regenerating script, research or claims. Designed by T-010, T-014 and T-015, delivered by T-026, T-031 and T-032, verified by T-042.
- S-002: Every phase, test and demonstration runs at USD 0.00, reaches no real vendor endpoint, and leaves the company store untouched. Verified by T-042, T-046 and T-064.
- S-003: A completed call hands back its content with its operation record. Designed by T-001, delivered by T-020, verified by T-043.
- S-004: Narration from a pay-per-use speech vendor in its real request, authentication, response and usage shapes, the secret never exposed. Designed by T-002, delivered by T-033, verified by T-044.
- S-005: A service forbidding automated access, and a narration service taking a licence over customer content, can never be selected. Designed by T-003, delivered by T-034, verified by T-045.
- S-006: Fake providers with the same contracts, deterministic at zero cost, selectable only in a mode reaching no network, never used by a metered run, never counted as observed spend or evidence. Designed by T-004 and T-005, delivered by T-021 and T-022, verified by T-046.
- S-007: The Audio stage produces narration audio from the whole recorded narration text and records its measured duration. Designed by T-009, delivered by T-027, verified by T-047.
- S-008: The Design stage produces the 22 specified motion graphics deterministically, with no capability call and no quantity the item's record does not state. Designed by T-011, delivered by T-029, verified by T-048.
- S-009: The Thumbnail stage produces the five diagram and typographic candidates without a capability call and holds the macro-frame candidate as a labelled placeholder. Designed by T-011, delivered by T-030, verified by T-049.
- S-010: The Production stage assembles narration, graphics and a labelled placeholder at every stock-clip position into one file with no music, recording the runtime measured from the file beside the specified runtime. Designed by T-008 and T-012, delivered by T-031, verified by T-050.
- S-011: The external renderer is started from exactly one place, located by configuration, and nothing else starts a process. Designed by T-012 and T-019, delivered by T-028 and T-040, verified by T-051.
- S-012: A produced item stays held short of publish-ready, and nothing reachable from production publishes, uploads or configures a channel. Designed by T-010 and T-019, delivered by T-031 and T-040, verified by T-052.
- S-013: Produced files are written only under one configured output root outside the tracked tree, by one writer, each recorded once completely written with a hash of the stored bytes. Designed by T-013, delivered by T-025 and T-040, verified by T-053.
- S-014: Every call is admitted against the item's remaining cap at its worst case, every attempt counts, the cap holds across runs and concurrent runs, reaching it stops production as a recorded outcome, and a channel with no budget amount is admitted against the company ceiling and the cap alone. Designed by T-006, delivered by T-024, verified by T-054 and T-063.
- S-015: Every metered operation books a stated cost from dated, sourced unit prices in every unit kind its vendor bills, the priced-route rule applies, and an unreported usage is never booked at zero. Designed by T-007, delivered by T-023, verified by T-055.
- S-016: The produce command offers fake, plan-only and metered modes, prints the plan, its estimate and the cap before any call, and in metered mode accepts the company store only and refuses naming each unmet precondition. Designed by T-005, T-014 and T-015, delivered by T-022, T-032 and T-035, verified by T-056.
- S-017: Owner documentation states the exact credential variable names, how to set them, the plan-only, fake and metered commands, and what a run will and will not do. Designed by T-014 and T-015, delivered by T-041, verified by T-057.
- S-018: Nothing in the production path is specific to item 001 or its subject beyond its recorded package and the item material traced to it. Designed by T-011 and T-019, delivered by T-026 and T-040, verified by T-058.
- S-019: The reasoning vendor's messages path and the speech-and-image vendor's image path exist with the speech path's contract properties, proven against fakes and recorded responses, unused by item 001's run. Designed by T-002, delivered by T-036, verified by T-059.
- S-020: The weekly brief and the dashboard show the produced item, its stage outcomes, operations and spend against the cap from the existing single read, fake runs labelled demonstration. Designed by T-005 and T-016, delivered by T-038, verified by T-060.
- S-021: A recovered attempt and an incurred attempt whose recording fails both count against the cap and stop production, every call and the render run inside stated bounds, and nothing in the records is held while the video renders. Designed by T-006, T-008 and T-017, delivered by T-037, verified by T-061 and T-063.
- S-022: The register shows the owner's five answers of 2026-10-09 as answered and price capture and the register writer as open, with no change to the week or the seven-rule catalogue. Designed by T-018, delivered by T-039, verified by T-062.
- S-023: The nine completed waves are built upon and none re-created; every test passing live at the Wave 8 head still passes; the boundary assertions are extended and never weakened. Designed by T-019, delivered by T-040, verified by T-064.
- S-024: No clip count, threshold, budget amount, channel configuration value, tier mapping or sizing claim is invented, and no figure from a fake run is presented as observed. Verified by T-064, recorded by T-065.
- S-025: The metered run's estimate is recomputed from the configured unit prices and labelled an estimate, and development cost and per-item cost are never netted. Designed by T-007 and T-014, delivered by T-032, verified by T-055, recorded by T-065.

### Out of Scope

- Any metered call in a phase, test or demonstration, and the single metered run itself. Excluded by the owner's decision of 2026-10-09 that the orchestrator executes the run outside the phases on the owner's explicit go.
- Publication, upload, channel creation, a channel configured as live, account creation, purchase, any subscription, and discharging any coded precondition. Excluded by the no-publish constraint, the coded preconditions and the owner's decision against subscriptions.
- Stock footage, music or score, generated video cutaways, voice cloning and any synthetic persona. Excluded because no subscription is bought and the recorded synthetic-media determination rests on an unidentifiable synthesised voice.
- Regenerating script, research, claims or metadata, any reasoning call in item 001's run, and any executive or narrative model call for reports. Excluded because the package is final and attributed and the owner's decision against report model calls stands.
- Image generation for the macro-frame thumbnail candidate. Excluded by the owner's decision of 2026-10-09 that it stays a placeholder.
- The autonomy capability. Excluded by the owner's decision of 2026-10-09 deferring it.
- Answering any open owner question, including channel one's budget amount, one unstated cost refusing the month, the tier mapping, deferred re-admission, channel configuration values, the channel-sum rule, the standing charge's home, the sizing decision, price capture and the register writer. Excluded because each answer is the owner's.
- Installing any schema resource into the company store, or writing any record to it, in any phase. Excluded by the carried constraint that the company store stays untouched; the orchestrator installs it immediately before the owner-released run.
- Re-fetching unit prices and verifying vendor terms at first hand inside a phase. Excluded because no phase reaches an external system; both are the orchestrator's preconditions of the go.
- The previous release record's known issues that do not bear on production, providers, routing or the cost controller, and a writer or cadence for platform-policy re-verification results. Excluded by the Scope Gate's carried set and the owner's answer naming the orchestrator as recorder.
- Publication-grade finishing: editorial review, phrase-level cue timing, colour or sound mastering, captions, renditions and upload packaging. Excluded because the video is never published.

### Deferred

- The single metered run of item 001. Brought into scope when this change closes, the orchestrator has installed and loaded the company store, re-fetched every unit price and verified both vendors' terms at first hand, the owner has set the credential variables, and the owner gives the explicit go.
- The macro-frame thumbnail candidate as an image. Brought into scope when the owner reverses the placeholder decision with a re-made synthetic-media determination.
- Stock clips at the 18 placeholder positions and the music bed. Brought into scope when a subscription is authorised.
- Re-admission of a deferred metered request. Brought into scope when the owner and omn-tech-lead record a re-admission rule.

## Assumptions

| ID | Assumption | Basis | Impact if false | Confirmed by |
|---|---|---|---|---|
| A-001 | The Scope Gate approved the scope with these rulings, which reached this phase only through the dispatch briefing: item 001's metered run plans narration only; the reasoning and image paths are built and proven against fakes and unused by the run; the 18 clip positions carry visible placeholders and there is no music; rendered means under the output root, decoding end to end with zero errors, exactly one video and one audio stream, runtime measured from the file within one frame; admission reserves the worst case and booking reconciles it, and no response is booked at zero units; no record or transaction is held while rendering and the render has a stated bound; stage order and the item material go to the architect and the narration call bound to omn-tech-lead at the Design Gate; no phase makes a metered call or touches the company store | S-001 | The cap, render, placeholder and stage tasks change shape | omn-business-analyst |
| A-002 | The owner's decisions of 2026-10-09 bind every phase: the first metered video is authorised under a USD 5.95 cap inside the USD 34.42 ceiling; every phase runs at USD 0.00; no subscription is bought; the pricing rule for unit kinds is approved; a channel with no budget amount is governed by the ceiling and the cap alone; the metered run books into the company store; the macro-frame thumbnail stays a placeholder; the billed accounts are the reasoning vendor and the speech-and-image vendor, both pay-per-use | S-002 | A metered task, an owner-gated dependency or an invented quantity enters the plan | omn-product-owner |
| A-003 | Verification tasks are owned by omn-dev-2-reviewer because omn-qa owns no phase in the routed workflow | plan-wide | Twenty-three verification tasks are never dispatched, or their ownership and phase mapping move | omn-orchestrator |
| A-004 | The rulings routed to omn-tech-lead, the narration call against the 60-second provider-call bound and the render's time bound, are prepared by architect tasks and ruled at the Design Gate, where omn-tech-lead is a listed owner, because omn-tech-lead owns no phase | S-021 | T-008 and T-009 are not ruled, and T-027, T-028 and T-031 have no bound to apply | omn-orchestrator |
| A-005 | The owner documentation is a repository file, so it is written by omn-dev-1-implement in the implementation phase; omn-documentation, which writes no repository file, produces the release record as its phase artifact | S-017 | The owner guide is never written, or a non-implementing role writes a repository file | omn-orchestrator |
| A-006 | The motion graphics, the thumbnail candidates and the placeholder cards are drawn through the one external-renderer boundary rather than through a new drawing dependency, so the Design and Thumbnail builds consume that boundary | S-008 | If the architect chooses an in-process drawing route, the edges from T-028 to T-029 and T-030 fall away and the process-boundary scope narrows | architect |
| A-007 | Every test and demonstration runs in the separate demonstration database and the company store holds zero tables after every phase; the records the metered run needs reach the company store only through the orchestrator's pre-run install and load outside the phases, by a path this change delivers and exercises only against a demonstration store | S-002 | A fixture or a load reaches the company store, or the metered run has no records to run against | omn-product-owner |
| A-008 | The vendors' request, response, authentication and usage shapes are fixed in dated reference fixtures from the implementing role's recorded knowledge of each vendor's published documentation, each citing its source and date, and are verified at first hand only by the orchestrator before the go | S-004 | A fixture misstates a live shape, and the metered run fails or books wrongly | omn-orchestrator |
| A-009 | The fake providers and the vendor adapters sit inside the capability boundary assembly and are reachable only through the gateway, so a fake reaches no network by construction | S-006 | A fake or an adapter becomes constructible outside the boundary, or the network-egress assertions must change | architect |
| A-010 | The metered run's planned operations are narration requests only; the reasoning and image paths are exercised only against fakes and recorded fixtures | S-019 | The estimate, the plan print and the cap reservation change | omn-product-owner |
| A-011 | The beat boundaries and each graphic's content are held as reviewable item material beside the recorded package, each entry quoting its source in the shot list, the script, the narration or the claim record, and no subject term enters production source | S-018 | Graphics show unsourced quantities, or the subject-term scan fails | architect |
| A-012 | Item 001 enters a fresh demonstration store from its recorded package through a load path this change delivers, with the package files under the second wave's item folder as its only source | S-001 | The end-to-end run has no item to produce, or a fixture stands in for the package | architect |

## Risks

| ID | Class | Trigger | Impact | Likelihood | Affects | Mitigation | Owner |
|---|---|---|---|---|---|---|---|
| R-001 | requirement | A Scope Gate ruling restated in A-001 is overturned | The cap, render, placeholder or stage tasks change | low | plan-wide | Any overturned ruling returns the plan to this phase rather than being absorbed | omn-business-analyst |
| R-002 | requirement | Metered mode refuses every store but the company store, while upstream criteria 38, 44, 49 and 50 need stub-transport metered runs in tests that may not touch the company store (Q-008) | The criteria are unverifiable, or the refusal is weakened to verify them | high | T-005, T-022, T-035, T-054, T-055, T-056 | T-005 states how metered behaviour is tested without the refusal admitting any store a run can reach | architect |
| R-003 | requirement | The credential variable name embeds the channel's identifier, minted when a channel row is written, and nothing loads the company store's channel, item, account, route, price and cap records beyond its schema (Q-004) | The owner guide cannot state exact names, and metered mode refuses for want of records | high | T-014, T-015, T-026, T-035, T-041, T-057 | T-015 decides the load path and how the names become fixed before the guide is written | architect |
| R-004 | requirement | Beat boundaries exist only as cue phrases in the script's prose and the graphics need content beyond the one-line shot list (Q-002, A-011) | Graphics invent quantities or placement cannot be checked | high | T-011, T-029, T-031, T-048, T-050 | T-011 states each graphic's and beat's material with its quoted source; T-048 reviews it | architect |
| R-005 | requirement | The closed stage order places Production before Audio, and the stage record admits one outcome per stage per item version (Q-001, Q-007) | Outcomes are recorded out of order, or a re-run after a cap stop or failed render cannot record | medium | T-010, T-031, T-032, T-042 | T-010 decides order and re-run recording without silently reordering the closed set | architect |
| R-006 | technical | An attempt whose vendor charge is unknown, a client timeout, a transport failure or a response without usage, is booked at zero units as a measurement, as the delivered adapter does today | The cap misses a charged attempt and a stated zero is read as observed | high | T-002, T-006, T-024, T-033, T-054, T-055 | T-006 books every unknown charge at its admitted worst case labelled estimate; T-054 and T-055 test each attempt kind | architect |
| R-007 | technical | The controller decides on booked spend only, so a call whose worst case passes the cap or the company ceiling is admitted while booked spend is below the threshold | Spend passes USD 5.95 or USD 34.42 by one call | high | T-006, T-024, T-054 | T-006 admits each call against remaining cap and ceiling at its worst case; T-054 binds the cap mid-production | architect |
| R-008 | technical | A reservation is invisible to a concurrent run of the same item, or a recovered attempt is booked without the scope holds, as the previous release record carries | Two runs or a recovery together pass the cap | medium | T-006, T-017, T-024, T-037, T-054 | T-006 and T-017 state the hold each path takes; T-054 runs a concurrent pair | architect |
| R-009 | technical | One narration request exceeds the 60-second provider-call bound under which the admission transaction is held, or the vendor's per-request text limit (Q-003) | Narration times out after the vendor charged, or the text is truncated | medium | T-008, T-009, T-027, T-033, T-061 | T-009 splits the text under the ruled bound; T-061 stalls a stand-in provider | architect |
| R-010 | technical | A transaction, a scope hold or a record is held open while the renderer runs, or the render, the probe or the decode has no bound | Admissions and reads wait, and the 30-second command timeout ends a recording unnamed | high | T-008, T-012, T-031, T-050, T-063 | T-008 enumerates every timeout and hold and states the render bound; T-063 observes open transactions during a render | architect |
| R-011 | technical | Stage records are stamped by the process clock, the credential handle expires on the process clock, operations book at the datastore's reserved instant, and the runtime is read from configuration rather than the file | Records disagree in order and the runtime is not measured | high | T-008, T-010, T-031, T-063 | T-008 names the one clock each decision uses; T-063 skews the process clock during a run | architect |
| R-012 | technical | An artifact is recorded before it is fully written, or its hash is computed over a different file than the renderer wrote | A recorded hash names bytes that do not exist | medium | T-013, T-025, T-031, T-053 | T-013 records only after the write completes and hashes the stored bytes; T-053 injects an interruption | architect |
| R-013 | technical | Measuring the runtime, decoding the file end to end and measuring narration duration each require starting the external tool again (Q-006) | The single process starter is widened unreviewed, or a measurement is taken from configuration | medium | T-012, T-028, T-051 | T-012 states every executable the one boundary starts and how each is configured | architect |
| R-014 | technical | Refusing an output root inside the tracked tree is implemented by asking version control, which starts a second process | A second process-starting site appears | medium | T-013, T-025, T-053 | T-013 states how the tracked tree is identified without a process | architect |
| R-015 | technical | Rendered still or encoded outputs differ between two runs through thread scheduling, embedded encoder metadata or font resolution | The repeated-run hash criteria fail | medium | T-012, T-029, T-030, T-046, T-048, T-049 | T-012 fixes the inputs that determinism depends on; T-048 and T-049 compare hashes | architect |
| R-016 | technical | The delivered price table, the cost arithmetic and the recorder cost only input, output and cached units, and the stored unit-kind check admits no character unit | The speech route is unselectable, or its operation is booked unstated and refuses the month under the interim rule | high | T-007, T-023, T-055 | T-007 states the unit kinds, the arithmetic and the schema resource; T-055 books each | architect |
| R-017 | technical | A delivered name scan forbids the transport fragment in every production type and the production key rule forbids fragments such as capability and transport | A fake provider or a setting named after the scope's wording fails the build or invites a weakening | medium | T-004, T-019, T-021, T-040 | T-019 lists the new types and keys against every delivered scan | architect |
| R-018 | technical | Text at request seams is trimmed, normalised or re-joined differently from the recorded narration | The character-for-character criterion fails | medium | T-009, T-027, T-047 | T-009 states the split and the reassembly; T-047 reconstructs the requests | architect |
| R-019 | security | A secret value reaches a log line, an exception message, an operation record or command output, or a per-vendor header scheme exposes it | Credential disclosure | medium | T-002, T-033, T-036, T-044 | T-002 keeps the secret on the transport message only; T-044 searches every output for a fake secret | architect |
| R-020 | security | A fake provider is reachable in metered mode, a failed metered call falls back to a fake, or a test reaches a real endpoint | A fake is booked as observed spend, or real spend occurs in a phase | low | T-004, T-021, T-035, T-046 | T-004 makes the modes exclusive; T-046 uses a transport failing on any non-stand-in request | architect |
| R-021 | security | The produce command reaches an upload, publication or channel-configuration path, or the process boundary starts an executable named by an unvalidated setting | Publication becomes reachable, or an arbitrary process can be started | low | T-012, T-019, T-028, T-040, T-052 | T-019 extends the absences to the produce command; T-012 validates the configured executable | architect |
| R-022 | security | The excluded-service rule is held by convention, or the customer-content licence has no forbidden-source kind, as the delivered register lacks one | An excluded narration service can be selected | low | T-003, T-034, T-045 | T-003 adds the kind and the routing rows' terms positions; T-045 refuses each kind | architect |
| R-023 | operational | A test or demonstration names a store other than the separate demonstration database, contrary to A-007 | The named store's schema is dropped or the company store is written | low | T-042, T-046, T-050, T-054, T-055, T-056, T-060, T-063, T-064 | Each verification records its target store and the company store's table count afterwards | omn-orchestrator |
| R-024 | operational | A verification task is assigned to a role that owns no phase, contrary to A-003 | Verification never runs | medium | plan-wide | A-003 assigns verification to the quality-review owner | omn-orchestrator |
| R-025 | operational | The bound rulings are not taken because omn-tech-lead owns no phase, contrary to A-004 | The narration split and the render bound have no ruling | low | T-008, T-009 | The orchestrator obtains both rulings at the Design Gate | omn-orchestrator |
| R-026 | dependency | A vendor shape fixed from recorded knowledge (A-008) differs from the live service, which only the orchestrator verifies at first hand before the go (Q-009) | The metered run fails at its first call or books wrong units | medium | T-002, T-033, T-036, T-044 | Each fixture cites its source and date; the release record lists first-hand verification as a precondition of the go | omn-orchestrator |
| R-027 | dependency | The renderer installed on the build machine is absent, a different version, or not located by the configured setting | The command refuses, or renders differ in hash | low | T-028, T-031, T-050 | T-028 refuses before any call naming the setting; T-050 records the version used | omn-orchestrator |
| R-028 | dependency | The demonstration database is unavailable for the live suite | Live criteria cannot be evidenced | low | T-042, T-064 | Verification records the skipped count and reason, never a pass | omn-orchestrator |
| R-029 | delivery | Twenty-five statements exceed one implementation pass | Should-have items slip and the change closes partial | medium | plan-wide | Should-have tasks T-036, T-037, T-038 and T-039 sit off the end-to-end path, so they can yield first | omn-orchestrator |
| R-030 | delivery | A new type is checked only by a single-project build, or the assertion count is not pinned, as the previous release record carries | A boundary violation reaches a commit unrefused | medium | T-040, T-064 | T-064 takes its evidence from a solution build failing on a seeded violation | omn-dev-2-reviewer |
| R-031 | delivery | The owner guide is assigned to a role that writes no repository file, contrary to A-005 | The owner has no guide when the change closes | low | T-041, T-057 | A-005 assigns it to the implementing role | omn-orchestrator |
| R-032 | requirement | The package records no alternative text for the thumbnail candidates (Q-010) | Alternative text is invented, or the barred-term criterion covers file names only | medium | T-011, T-030, T-049 | T-011 states the alternative text's source; the owner is asked rather than a text invented | architect |

## Task Breakdown

### T-001 Decide how a completed call hands back its content

- Owner: architect
- Complexity: M (confidence: medium)
- Depends on: none
- Traces to: S-003, A-001
- Status: assumption-dependent
- Description: A recorded design states the outcome member that carries produced text, audio or image bytes beside the operation record, what length and hash are recorded for it, and that a refused, held, deferred, substituted or failed outcome carries none.
- Acceptance Criteria:
  - The design names the content kinds carried and the outcome cases that carry content.
  - The design states which length and hash are recorded and that both are taken over the bytes the caller receives.
  - The design states that every non-completed outcome carries no content and still carries its recorded outcome.
  - The design states how content larger than memory comfort is handed on without a second copy being hashed.
- Gate: Design Gate

### T-002 Decide the vendor contracts and each vendor's authentication scheme

- Owner: architect
- Complexity: L (confidence: low)
- Depends on: T-001
- Traces to: S-004, S-019, A-008, A-010
- Status: assumption-dependent
- Description: A recorded design states, for the speech path and for the messages and image paths, the request shape, endpoint path, authentication header scheme, response parsing and usage parsing from the response body, the dated reference fixtures each rests on, and how the credential broker applies a per-vendor scheme without the secret leaving the transport message, answering Q-009.
- Acceptance Criteria:
  - The design lists, per path, the request fields, the endpoint path, the header scheme and the billed units read from the response body, with zero values read from invented headers.
  - The design names each reference fixture with its source and date, and states that first-hand verification stays with the orchestrator before the go.
  - The design states, per path, what an attempt with no reported usage, a client timeout or a transport failure records.
  - The design states how the broker selects a header scheme per vendor and that no public member returns or logs the secret.
- Gate: Design Gate

### T-003 Decide how excluded services are refused by the routing table

- Owner: architect
- Complexity: S (confidence: medium)
- Depends on: none
- Traces to: S-005, A-002
- Status: assumption-dependent
- Description: A recorded design states the forbidden-source kind for narration terms taking a licence over customer content beside the delivered automated-access kind, the refusal reason named for each, and the terms positions every speech and image routing row records with evidence and date.
- Acceptance Criteria:
  - The design names both excluded kinds and the named refusal reason for each.
  - The design states that a refusal sends zero requests and records its outcome.
  - The design lists the automated-access and customer-content positions each routing row carries, with evidence reference and date.
  - The design states the schema change, if any, and what existing rows read as.
- Gate: Design Gate

### T-004 Decide how fake providers are selected and isolated

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: T-001
- Traces to: S-006, A-009
- Status: assumption-dependent
- Description: A recorded design states how a fake provider with the vendor contract returns deterministic content at zero cost, the configuration that selects it and cannot be confused with a real endpoint, that a metered run can never reach or fall back to it, and that it reaches no network whatever credential values are set.
- Acceptance Criteria:
  - The design names the selecting configuration and shows it cannot name a real endpoint.
  - The design states that metered mode with a fake configured refuses before the first call, and that a failed metered call is followed by zero fake calls.
  - The design states the deterministic content each fake returns and its recorded zero cost.
  - The design names every new type and key and shows none fails a delivered name or key scan.
- Gate: Design Gate

### T-005 Decide store designation and demonstration labelling

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: none
- Traces to: S-006, S-016, S-020, A-002, A-007
- Status: assumption-dependent
- Description: A recorded design states how a store is designated demonstration or company, that fake runs record only in demonstration stores labelled demonstration, that metered mode accepts the designated company store only, how every fake operation contributes zero to observed spend, benchmark observations and comparable-run counts, and how metered behaviour is tested without the refusal admitting any store a run can reach, answering Q-008.
- Acceptance Criteria:
  - The design states the designation's record and who writes it, and that no phase writes it to the company store.
  - The design states the refusals of each mode for each designation.
  - The design states how a fake operation is labelled and read by every reader of spend, benchmarks and comparable runs.
  - The design states how upstream criteria 38, 44, 49 and 50 are evidenced without a test touching the company store and without weakening the refusal.
- Gate: Design Gate

### T-006 Decide the per-item cap's admission and reconciliation

- Owner: architect
- Complexity: L (confidence: low)
- Depends on: T-007
- Traces to: S-014, S-021, A-001, A-002
- Status: assumption-dependent
- Description: A recorded design states where the per-item cap and its source are recorded, how each call is admitted at its worst case against the item's remaining cap and the company ceiling inside the admission transaction, how the booking reconciles that reservation, how an unknown charge is booked at the admitted worst case labelled estimate, how the cap holds across runs and concurrent runs, the cap-reached outcome, and how a channel with no budget amount is admitted against the ceiling and the cap alone.
- Acceptance Criteria:
  - The design states the worst-case figure for each call and the snapshot it is decided on.
  - The design lists failed, retried, timed-out, recovered and usage-less attempts and how each counts against the cap.
  - The design states the hold that serialises two runs of one item and that no admission waits on it.
  - The design states that the controller's channel reading for a channel with no amount neither refuses nor records an amount, and that the 90-percent deferral cannot stop production before the cap.
  - The design states the cap-reached outcome, the total it names, and that the next call is not made.
- Gate: Design Gate

### T-007 Decide how further unit kinds are costed under the approved pricing rule

- Owner: architect
- Complexity: M (confidence: medium)
- Depends on: none
- Traces to: S-015, S-025, A-002
- Status: assumption-dependent
- Description: A recorded design states the unit kinds each vendor path bills by, how a route is priced when every billed kind has a price in force, how the datastore's arithmetic states a cost in those kinds, how an estimate is computed from the configured prices and labelled, and the schema resource carrying the change.
- Acceptance Criteria:
  - The design lists the billed unit kinds per path and the price each needs.
  - The design states that an unbilled kind needs no price and a billed kind without a price refuses under a named reason with zero requests sent.
  - The design states the cost arithmetic for each kind and what existing rows read as.
  - The design states how the plan's estimate is computed, labelled estimate, and kept apart from development cost.
- Gate: Design Gate

### T-008 Decide every timeout, hold and clock on the produce path

- Owner: architect
- Complexity: L (confidence: low)
- Depends on: none
- Traces to: S-010, S-021, A-001, A-004
- Status: assumption-dependent
- Description: A recorded design first enumerates every timeout and hold the produce path meets, the 30-second default command timeout, the 60-second provider-call bound under which the admission transaction holds the record horizon and the scope holds, the recovery command timeout at twice that bound, the management read's statement and lock bounds, the deferred request's hold timeout, the credential handle lifetime, the chain head and the stage-record key, then states the render, probe and decode bounds proposed for ruling, answering Q-003 and Q-005, that no transaction is open while the renderer runs, and the one clock every instant is decided on.
- Acceptance Criteria:
  - The design lists every timeout and hold with its value, its holder and what ends a wait on it under a named outcome.
  - The design states the proposed render, probe and decode bounds and the provider-call bound for narration for ruling by omn-tech-lead at the Design Gate.
  - The design states that no datastore transaction or hold is open while an external process runs.
  - The design names the clock for each stage record, operation, artifact record, cap decision and credential handle, and shows no two decide one order.
- Gate: Design Gate

### T-009 Decide how narration is requested and its duration measured

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: T-002, T-008
- Traces to: S-007, A-001
- Status: assumption-dependent
- Description: A recorded design states how the whole recorded narration text is split into requests within the vendor's per-request limit and the ruled call bound, how the parts reassemble character for character with nothing added, and how the narration's duration is measured from the produced audio rather than from a delivery-rate assumption.
- Acceptance Criteria:
  - The design states the split rule and shows the concatenated requests equal the recorded text, all 11,096 characters.
  - The design states how seams are joined in audio and that no seam adds or drops text.
  - The design states how duration is measured from the audio and that the words-per-minute assumption derives no duration.
  - The design states the number of narration operations the plan prints and the worst case of each.
- Gate: Design Gate

### T-010 Decide how stage outcomes are recorded in the closed order

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: none
- Traces to: S-001, S-012, A-001
- Status: assumption-dependent
- Description: A recorded design answers Q-001 and Q-007: how the four produced stages record outcomes when assembly consumes the narration while the closed order places Production before Audio, how the eight other stages carry the outcome item 001's dossier records, how a re-run of one item version records given one outcome per stage per version, and which held conditions keep the item short of publish-ready.
- Acceptance Criteria:
  - The design states the recording order and whether any reorder of the closed set is a recorded decision.
  - The design states, for each of the twelve stages, the outcome item 001's run records and the evidence it cites.
  - The design states what a second run of the same item version records after a cap stop or a failed render.
  - The design names the Production hold for unsourced clips and the Copyright check hold for the unregistered library.
- Gate: Design Gate

### T-011 Decide how beats, graphics and thumbnail material are held

- Owner: architect
- Complexity: L (confidence: low)
- Depends on: none
- Traces to: S-008, S-009, S-018, A-006, A-011
- Status: assumption-dependent
- Description: A recorded design answers Q-002 and Q-010: how beat boundaries and the content of each of the 22 graphics and five thumbnail candidates are held as reviewable item material quoting their source, with nothing added, how placeholders name each clip and its beat, the source of thumbnail alternative text, and that no subject term enters production source.
- Acceptance Criteria:
  - The design states the item material's form and location beside the package, and that each entry quotes its source.
  - The design states how a beat's narration span is located from the material and the produced audio.
  - The design states the alternative text's source, or that the owner is asked, with no text invented.
  - The design states how the subject-term scan reads production source and excludes item material.
- Gate: Design Gate

### T-012 Decide the external-renderer boundary

- Owner: architect
- Complexity: L (confidence: low)
- Depends on: T-008, T-013
- Traces to: S-010, S-011, A-006
- Status: assumption-dependent
- Description: A recorded design answers Q-006: the one type that starts an external process, every executable it starts and the setting locating each, its working directory under the output root, its stated bound and failure outcome, the timeline built from the shot list and the item material, the placeholder cards, the full decode and the runtime probe, and the inputs fixed for repeatable output.
- Acceptance Criteria:
  - The design names the one process-starting type and every executable and argument class it may start.
  - The design states how an absent or missing configured executable is refused before any capability call, with zero hard-coded executable paths.
  - The design states that a render past its bound or ending in failure leaves zero artifacts recorded and records the Production outcome with the failure.
  - The design states how the runtime is measured from the rendered file to within one frame and recorded beside the specified 13 min 39 s.
- Gate: Design Gate

### T-013 Decide the artifact store

- Owner: architect
- Complexity: M (confidence: medium)
- Depends on: none
- Traces to: S-013, A-001
- Status: assumption-dependent
- Description: A recorded design states the one configured output root, how a root inside the repository's tracked tree is identified and refused without starting a process, the one writer, that an artifact is recorded only once completely written with the hash of the stored bytes, how a file the renderer writes is adopted under the same rule, and what an interrupted write leaves.
- Acceptance Criteria:
  - The design states how the tracked tree is identified and that no process is started to identify it.
  - The design names the one file-writing type and the artifact record's fields.
  - The design states the ordering of write, flush, hash and record, and that an interrupted write leaves zero records.
  - The design states how a renderer-written file is hashed after the renderer exits and before it is recorded.
- Gate: Design Gate

### T-014 Decide the produce command's modes and refusals

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: T-005, T-006, T-015
- Traces to: S-001, S-016, S-017, S-025
- Status: assumption-dependent
- Description: A recorded design states the fake, plan-only and metered modes, the plan printed before any call with each operation's estimate and the total labelled estimate and the cap with its source, each metered precondition and the message naming it, the exact credential variable names, and that one build serves both modes by configuration alone.
- Acceptance Criteria:
  - The design lists the printed plan's lines and states they precede the first call in every mode.
  - The design lists every metered refusal condition: a missing credential variable, an unrecorded cap, a demonstration store or any store other than the company store, an unpriced route, a controller refusal and a fake configured.
  - The design states that plan-only mode makes zero capability calls and writes zero files.
  - The design states each credential variable name exactly as printed.
- Gate: Design Gate

### T-015 Decide how the metered run's records reach the company store

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: none
- Traces to: S-016, S-017, A-007, A-012
- Status: assumption-dependent
- Description: A recorded design answers Q-004: how the company, channel, item package, provider accounts, models, routes with terms positions, dated prices and the cap reach a store through a load path this change delivers, run in the phases only against a demonstration store and by the orchestrator against the company store after its install, and how the channel identifier the credential variable name embeds becomes fixed before the owner guide is written.
- Acceptance Criteria:
  - The design lists every record the metered run needs and the source of each.
  - The design states that no phase runs the load against the company store.
  - The design states how the channel identifier is fixed, or how the variable name stops depending on it, without changing what a credential handle scopes.
  - The design states that the load records no channel budget amount or configuration value.
- Gate: Design Gate

### T-016 Decide the produced item's lines in the brief and the dashboard

- Owner: architect
- Complexity: S (confidence: medium)
- Depends on: T-005, T-006
- Traces to: S-020
- Status: ready
- Description: A recorded design states the lines that show the produced item, its stage outcomes, its operations and its spend against the cap from the delivered single read, how demonstration lines are labelled, and that they add zero to observed spend.
- Acceptance Criteria:
  - The design lists each new line and the reading it rests on.
  - The design states that every line is taken in the delivered single read and that no read holds a write path.
  - The design states the demonstration label and that a fake operation contributes zero to observed spend.
  - The design states each line's case over a store with no produced item.
- Gate: Design Gate

### T-017 Decide the carried recovery items against the cap

- Owner: architect
- Complexity: M (confidence: low)
- Depends on: T-006
- Traces to: S-021, A-001
- Status: assumption-dependent
- Description: A recorded design states how an attempt recovered after a lost admission transaction counts against the cap under the hold the cap takes, how an incurred attempt whose recording fails stops production with a named failure carrying the attempt's facts, and the recovery command timeout against the ruled call bound.
- Acceptance Criteria:
  - The design states the hold the recovery takes and that it cannot pass the cap with another admission.
  - The design states that production makes zero further calls after a recovered or unrecorded attempt.
  - The design states the named failure's facts and where they are durable.
  - The design states the recovery command timeout and its relation to the call bound.
- Gate: Design Gate

### T-018 Decide the register entries the owner's answers change

- Owner: architect
- Complexity: XS (confidence: high)
- Depends on: none
- Traces to: S-022, A-002
- Status: assumption-dependent
- Description: A recorded design lists the register entries recorded answered by the owner's decisions of 2026-10-09, the entries left open, and confirms the report week and the seven-rule catalogue need no change.
- Acceptance Criteria:
  - The design lists the five answered entries with the decision answering each.
  - The design lists price capture and the register writer as open with their owners.
  - The design states that no report computation changes.
  - The design states how the entries reach a store without editing a recorded entry in place.
- Gate: Design Gate

### T-019 Decide the boundary assertions this change adds

- Owner: architect
- Complexity: M (confidence: medium)
- Depends on: T-004, T-012, T-013
- Traces to: S-011, S-012, S-013, S-018, S-023
- Status: ready
- Description: A recorded design lists the assertions extended for one process starter, one file writer, zero subject terms in production source, zero upload, publication or channel-configuration paths reachable from the produce command, and every new type and key checked against the delivered scans, with none weakened.
- Acceptance Criteria:
  - The design names each new assertion and the seeded violation that fails it.
  - The design lists every delivered scan the new types meet and states none is relaxed.
  - The design states how the assertions run in a solution build.
  - The design states the architecture suite's expected count after the change.
- Gate: Design Gate

### T-020 Deliver the content-carrying outcome at the capability boundary

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-001
- Traces to: S-003
- Status: ready
- Description: The capability boundary returns produced content with its operation record as the design fixes, and every other outcome returns none.
- Acceptance Criteria:
  - A completed outcome carries content whose recorded length and hash equal the received bytes.
  - Refused, held, deferred, substituted and failed outcomes carry no content and their recorded outcome.
  - Every delivered capability test still passes.
  - No delivered outcome case is removed.
- Gate: Review Gate, Verification Gate

### T-021 Deliver the deterministic fake providers

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-004, T-020
- Traces to: S-006, A-009
- Status: assumption-dependent
- Description: Fakes for the speech, messages and image paths return deterministic content at zero cost through the boundary, selectable only as the design fixes, with a contract suite every vendor adapter later passes.
- Acceptance Criteria:
  - Two invocations with one input return identical content hashes.
  - A fake makes zero network requests with every credential variable set to a fake value.
  - The contract suite runs against each fake and passes.
  - No new type or key fails a delivered name or key scan.
- Gate: Review Gate, Verification Gate

### T-022 Deliver store designation and demonstration labelling

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-005
- Traces to: S-006, S-016, S-020, A-007
- Status: assumption-dependent
- Description: A store carries the designation the design fixes, fake operations are labelled demonstration, and every reader of spend, benchmarks and comparable runs counts them at zero.
- Acceptance Criteria:
  - A fake operation reads labelled demonstration at zero cost.
  - Observed spend, benchmark observations and comparable-run counts read unchanged after a fake run.
  - The designation is written only in demonstration stores by any test or demonstration.
  - The company store is not touched.
- Gate: Review Gate, Verification Gate

### T-023 Deliver costing in every billed unit kind

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-007
- Traces to: S-015, S-025
- Status: ready
- Description: Routes are selectable and operations booked in every unit kind the design fixes, through the schema change that design states, applied after the eight delivered resources.
- Acceptance Criteria:
  - A route billed only in priced kinds is selectable with no cached-unit price.
  - A route billing a kind with no price in force is refused under a named reason with zero requests sent.
  - An operation books a stated cost from prices carrying source and verification date.
  - Rows recorded before the resource read as the design states.
- Gate: Review Gate, Verification Gate

### T-024 Deliver the per-item cap's admission and reconciliation

- Owner: omn-dev-1-implement
- Complexity: L (confidence: low)
- Depends on: T-006, T-023
- Traces to: S-014, S-021
- Status: assumption-dependent
- Description: Every call is admitted at its worst case against the item's remaining cap and the company ceiling and reconciled at booking as the design fixes, a channel with no budget amount is governed by the ceiling and the cap alone, and reaching the cap ends production as a recorded outcome.
- Acceptance Criteria:
  - With stub prices binding mid-production, the counted total never exceeds USD 5.95 and the call that would pass it is not made.
  - An attempt with unknown charge is booked at its admitted worst case labelled estimate.
  - A call the controller refuses or defers is not made though it fits the remaining cap.
  - A channel with no budget amount is admitted within ceiling and cap, and zero channel amounts are recorded.
- Gate: Review Gate, Verification Gate

### T-025 Deliver the one artifact writer under the output root

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-013
- Traces to: S-013
- Status: ready
- Description: One type writes files, only under the configured output root, and records each artifact once completely written with the hash of the stored bytes, as the design fixes.
- Acceptance Criteria:
  - A root inside the tracked tree is refused before any call.
  - Each recorded hash equals the hash recomputed from the stored file.
  - An injected interruption leaves zero records for the file.
  - No process is started by the writer.
- Gate: Review Gate, Verification Gate

### T-026 Deliver the recorded item package load

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-011, T-015
- Traces to: S-001, S-018, A-011, A-012
- Status: assumption-dependent
- Description: A load path places a recorded item package, its item material and the records a run needs into a store, as the design fixes, and is exercised only against demonstration stores.
- Acceptance Criteria:
  - Loading item 001 into a fresh demonstration store yields every record the run reads.
  - The load records no channel budget amount or configuration value.
  - Production source holds zero subject terms of item 001.
  - The load is refused against a store not designated as the design states.
- Gate: Review Gate, Verification Gate

### T-027 Deliver the Audio stage

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-009, T-010, T-020, T-025
- Traces to: S-007
- Status: assumption-dependent
- Description: The Audio stage obtains narration for the whole recorded text through the boundary, stores it as an artifact and records the duration measured from the produced audio.
- Acceptance Criteria:
  - The concatenated request texts equal the recorded narration text, all 11,096 characters.
  - The recorded duration equals the duration measured from the produced audio.
  - Zero durations derive from the words-per-minute assumption.
  - Every call admitted is recorded as one operation.
- Gate: Review Gate, Verification Gate

### T-028 Deliver the external-process boundary

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-012, T-025
- Traces to: S-011
- Status: assumption-dependent
- Description: One type starts the configured external executables under the ruled bound, with no datastore transaction open, and ends past its bound or on failure under a named outcome.
- Acceptance Criteria:
  - An absent setting or a missing executable is refused before any capability call, naming the setting.
  - A stand-in exceeding the bound ends under a named outcome with zero artifacts recorded.
  - Zero datastore transactions are open while the process runs.
  - The source holds zero hard-coded executable paths.
- Gate: Review Gate, Verification Gate

### T-029 Deliver the Design stage's 22 graphics

- Owner: omn-dev-1-implement
- Complexity: L (confidence: low)
- Depends on: T-011, T-025, T-028
- Traces to: S-008, A-006, A-011
- Status: assumption-dependent
- Description: The Design stage produces one graphic segment per specified graphic from the item material, deterministically, with zero capability calls.
- Acceptance Criteria:
  - Exactly 22 segments exist, each carrying the identifier it implements.
  - Two productions from one input yield identical hashes.
  - The fake-provider call log shows zero calls from the stage.
  - Every numeral shown traces to an item-material entry quoting its source.
- Gate: Review Gate, Verification Gate

### T-030 Deliver the Thumbnail stage

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-011, T-025, T-028
- Traces to: S-009, A-006
- Status: assumption-dependent
- Description: The Thumbnail stage produces the five diagram and typographic candidates without a capability call and a placeholder naming the macro-frame candidate.
- Acceptance Criteria:
  - Five images and one placeholder naming its candidate are stored as artifacts.
  - Zero image-generation calls are made.
  - File names and alternative text pass the delivered barred-term screen with zero hits.
  - Two productions from one input yield identical hashes.
- Gate: Review Gate, Verification Gate

### T-031 Deliver the Production assembly and render

- Owner: omn-dev-1-implement
- Complexity: L (confidence: low)
- Depends on: T-008, T-010, T-026, T-027, T-028, T-029
- Traces to: S-010, S-012, A-001
- Status: assumption-dependent
- Description: The Production stage assembles narration, graphics and a labelled placeholder at every clip position into one file under the output root, with no music, records the runtime measured from the file and records the stage held for unsourced clips.
- Acceptance Criteria:
  - The file decodes end to end with zero errors and carries one video and one audio stream.
  - The recorded runtime equals the probed duration within one frame, shown beside the specified runtime and labelled.
  - All 18 clip positions carry a placeholder naming the clip and its beat, and each sits within its beat's measured span.
  - The Production stage records held, naming the unsourced clips.
- Gate: Review Gate, Verification Gate

### T-032 Deliver the produce command in fake and plan-only modes

- Owner: omn-dev-1-implement
- Complexity: L (confidence: low)
- Depends on: T-014, T-021, T-022, T-024, T-030, T-031
- Traces to: S-001, S-016, S-025
- Status: assumption-dependent
- Description: One fake-mode invocation against a fresh demonstration store holding item 001 yields one rendered file and exits successfully, and plan-only mode prints the plan and makes no call.
- Acceptance Criteria:
  - The plan, each estimate and the total labelled estimate, and the cap with its source print before the first call.
  - The fake run ends with one rendered file and twelve recorded stage outcomes, with zero manual steps.
  - Plan-only mode makes zero capability calls and writes zero files.
  - The publish-ready predicate refuses the item afterwards, naming the Production and Copyright check holds.
- Gate: Review Gate, Verification Gate

### T-033 Deliver the speech vendor adapter

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-002, T-021
- Traces to: S-004, A-008
- Status: assumption-dependent
- Description: The speech path sends the vendor's request shape under its authentication scheme, parses audio and billed units from the response, and passes the contract suite the fake passes, against dated fixtures and a stub transport.
- Acceptance Criteria:
  - Request body, endpoint path and header scheme match the dated reference fixture.
  - Billed units are read from the response body, with zero values from invented headers.
  - A timeout, a transport failure and a usage-less response each record as T-002 fixes, never as a zero measurement.
  - A fake secret appears in zero log lines, operation records, exception messages and outputs.
- Gate: Review Gate, Verification Gate

### T-034 Deliver the excluded-service refusal

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-003
- Traces to: S-005
- Status: ready
- Description: Both excluded kinds are refused by the routing table under named reasons and every speech and image routing row records its terms positions.
- Acceptance Criteria:
  - With an excluded service as the only candidate, the call is refused under its named reason.
  - Zero requests are sent on a refusal.
  - Each speech and image routing row carries both positions with evidence and date.
  - Delivered forbidden-source behaviour is unchanged.
- Gate: Review Gate, Verification Gate

### T-035 Deliver the produce command's metered mode

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-015, T-032, T-033, T-034
- Traces to: S-016, S-017
- Status: assumption-dependent
- Description: Metered mode refuses before any call naming each unmet precondition, accepts the designated company store only, and reaches a stub transport by configuration and fake credentials alone with no code change from fake mode.
- Acceptance Criteria:
  - Each refusal condition the design lists refuses before the first call, naming it.
  - A fake configured in metered mode refuses before the first call.
  - One build reaches a stub transport in metered mode and fakes in fake mode.
  - Missing credential variables are printed by their exact names.
- Gate: Review Gate, Verification Gate

### T-036 Deliver the messages and image vendor adapters

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-033
- Traces to: S-019, A-008, A-010
- Status: assumption-dependent
- Description: The reasoning vendor's messages path and the image path pass the contract suite against dated fixtures, each with its fake, and item 001's run calls neither.
- Acceptance Criteria:
  - Each path passes the contract suite against its dated fixture and its fake.
  - Each path reads billed units from the response body.
  - Item 001's fake run records zero messages or image calls.
  - A fake secret appears in no output of either path.
- Gate: Review Gate, Verification Gate

### T-037 Deliver the carried recovery behaviour against the cap

- Owner: omn-dev-1-implement
- Complexity: M (confidence: low)
- Depends on: T-017, T-024
- Traces to: S-021
- Status: assumption-dependent
- Description: An attempt recovered after a lost admission transaction counts against the cap under the designed hold, and an incurred attempt whose recording fails stops production with a named failure.
- Acceptance Criteria:
  - An injected transaction loss leaves the recovered attempt counted against the cap.
  - Production makes zero further calls after a recovered or unrecorded attempt.
  - An injected recording failure ends under a named failure carrying the attempt's facts.
  - The recovery runs under the designed command timeout.
- Gate: Review Gate, Verification Gate

### T-038 Deliver the produced item's lines in the brief and the dashboard

- Owner: omn-dev-1-implement
- Complexity: S (confidence: medium)
- Depends on: T-016, T-032
- Traces to: S-020
- Status: ready
- Description: The brief and the dashboard show the produced item, its stage outcomes, operations and spend against the cap from the delivered single read, with demonstration lines labelled.
- Acceptance Criteria:
  - The lines read from one read over a demonstration store after a fake run.
  - Fake lines are labelled demonstration and add zero to observed spend.
  - No management path gains a write path, a clock or an action.
  - Delivered management tests pass unchanged.
- Gate: Review Gate, Verification Gate

### T-039 Deliver the register entries

- Owner: omn-dev-1-implement
- Complexity: XS (confidence: high)
- Depends on: T-018
- Traces to: S-022
- Status: ready
- Description: The register shows the five answered entries answered and price capture and the register writer open, with the week and the seven-rule catalogue unchanged.
- Acceptance Criteria:
  - Each answered entry cites the decision answering it.
  - Price capture and the register writer read open with their owners.
  - The report-week and seven-rule tests pass unchanged.
  - No recorded entry is edited in place.
- Gate: Review Gate, Verification Gate

### T-040 Deliver the boundary assertions

- Owner: omn-dev-1-implement
- Complexity: M (confidence: medium)
- Depends on: T-019, T-025, T-028, T-035, T-036
- Traces to: S-011, S-012, S-013, S-018, S-023
- Status: ready
- Description: The architecture suite fails on a second process starter, a second file writer, a subject term in production source or a publication path reachable from the produce command, and every delivered assertion still holds.
- Acceptance Criteria:
  - Each new assertion fails on its seeded violation in a solution build.
  - The architecture suite counts at least 63 tests and no delivered assertion is removed or relaxed.
  - The five structural absences on the upload path hold.
  - Every type this change adds is inside the scans' reach.
- Gate: Review Gate, Verification Gate

### T-041 Deliver the owner's metered-run guide

- Owner: omn-dev-1-implement
- Complexity: S (confidence: low)
- Depends on: T-035
- Traces to: S-017, A-005
- Status: assumption-dependent
- Description: A repository guide states the exact credential variable names as metered mode prints them, how to set each, the plan-only, fake and metered commands, and that a run publishes nothing, buys nothing and stops at the cap.
- Acceptance Criteria:
  - Every variable name matches the metered refusal output exactly.
  - The guide holds zero secret values.
  - The guide states the three commands and what each will and will not do.
  - The guide lists the orchestrator's preconditions of the go: install and load, price re-fetch, terms verification.
- Gate: Review Gate, Verification Gate

### T-042 Verify the zero-spend end-to-end run

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-032
- Traces to: S-001, S-002, A-003, A-007
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criteria 1, 2, 3 and 4 from a fake-mode run in a fresh demonstration store.
- Acceptance Criteria:
  - The recorded command output of one fake run ending with one rendered file and exit success.
  - A read of twelve stage outcomes, the four produced ones citing artifacts and the eight others citing the dossier.
  - Evidence that operation records equal provider invocations, and zero calls for the seven unproduced stages.
  - The target store named and the company store's table count after the run.
- Gate: Review Gate, Verification Gate

### T-043 Verify the content-carrying outcome

- Owner: omn-dev-2-reviewer
- Complexity: XS (confidence: medium)
- Depends on: T-020
- Traces to: S-003, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criteria 8 and 9.
- Acceptance Criteria:
  - Evidence that recorded length and hash equal the received bytes over fake and stub transports.
  - Evidence per non-completed outcome of no content and a recorded outcome.
  - Evidence that no delivered capability test changed its assertion.
  - Evidence that no network request left the process.
- Gate: Review Gate, Verification Gate

### T-044 Verify the speech contract and secret handling

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-033
- Traces to: S-004, A-003, A-008
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criteria 10 and 11.
- Acceptance Criteria:
  - Contract-test evidence against each recorded fixture.
  - Review evidence of each fixture's source and date.
  - Evidence that zero units are read from invented headers.
  - Evidence that a fake secret appears in zero outputs across a full run.
- Gate: Review Gate, Verification Gate

### T-045 Verify the excluded-service refusals

- Owner: omn-dev-2-reviewer
- Complexity: XS (confidence: medium)
- Depends on: T-034
- Traces to: S-005, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criteria 12 and 13.
- Acceptance Criteria:
  - Evidence per excluded kind of a named refusal.
  - Evidence of zero requests sent on each refusal.
  - Review evidence of every speech and image routing row's positions with evidence and date.
  - Evidence that delivered forbidden-source tests pass.
- Gate: Review Gate, Verification Gate

### T-046 Verify fake isolation and labelling

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-032, T-035
- Traces to: S-002, S-006, A-003, A-007
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criteria 6, 14, 15, 16 and 17.
- Acceptance Criteria:
  - Evidence from a transport failing on any non-stand-in request across the suite and the demonstrations.
  - Evidence of identical narration, graphics and thumbnail hashes over two fake runs.
  - Evidence that metered mode with a fake refuses before the first call and a failed metered call is followed by zero fake calls.
  - Evidence that fake operations read demonstration at zero and add zero to observed spend, benchmark observations and comparable-run counts.
- Gate: Review Gate, Verification Gate

### T-047 Verify the Audio stage

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-027
- Traces to: S-007, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criteria 18 and 19.
- Acceptance Criteria:
  - Evidence reconstructing the requests to the recorded text, character for character.
  - Evidence comparing the recorded duration with a measurement of the audio.
  - Evidence that no duration derives from the delivery-rate assumption.
  - Evidence that seams add and drop no text.
- Gate: Review Gate, Verification Gate

### T-048 Verify the Design stage's graphics

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-029
- Traces to: S-008, A-003, A-011
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criteria 20, 21 and 22.
- Acceptance Criteria:
  - Evidence of exactly 22 segments with their identifiers and zero capability calls.
  - Review evidence that every numeral and named quantity appears in the narration, the claim record or the shot list.
  - Evidence of identical hashes over two productions.
  - Review evidence that each item-material entry quotes its source.
- Gate: Review Gate, Verification Gate

### T-049 Verify the Thumbnail stage

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-030
- Traces to: S-009, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criteria 23 and 24.
- Acceptance Criteria:
  - Evidence of five images and one placeholder naming its candidate.
  - Evidence of zero image-generation calls.
  - Evidence of zero barred terms in file names and alternative text through the delivered screen.
  - Evidence of the alternative text's recorded source.
- Gate: Review Gate, Verification Gate

### T-050 Verify the rendered file

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-031
- Traces to: S-010, A-003, A-007
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criteria 25, 26, 27, 28 and 29.
- Acceptance Criteria:
  - Evidence of a full decode with zero errors and exactly one video and one audio stream.
  - Evidence comparing the recorded runtime with a probe of the file within one frame, both labelled.
  - Review evidence of the timeline and an automated count of 18 placeholders, each within its beat's span, and narration only on the audio stream.
  - Evidence of zero open datastore transactions during a stand-in render.
- Gate: Review Gate, Verification Gate

### T-051 Verify the process boundary

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: medium)
- Depends on: T-031, T-040
- Traces to: S-011, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criteria 30, 31 and 32.
- Acceptance Criteria:
  - Architecture evidence that one type starts a process and a seeded second fails the suite.
  - Evidence of a refusal naming the setting before any call when it is absent or names a missing executable.
  - Scan evidence of zero hard-coded executable paths.
  - Evidence that a render past its bound or failing leaves zero artifacts and records the failure.
- Gate: Review Gate, Verification Gate

### T-052 Verify that nothing publishes

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: medium)
- Depends on: T-032, T-040
- Traces to: S-012, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criteria 33 and 34.
- Acceptance Criteria:
  - Evidence that the publish-ready predicate refuses the item after a complete run with both named holds.
  - Architecture evidence of zero upload, publication or channel-configuration paths reachable from the produce command.
  - Evidence that the five structural absences hold.
  - Evidence that the three coded preconditions remain undischarged.
- Gate: Review Gate, Verification Gate

### T-053 Verify the artifact store

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: medium)
- Depends on: T-031, T-040
- Traces to: S-013, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criteria 35, 36 and 37.
- Acceptance Criteria:
  - Evidence that every produced file lies under the root and a tracked-tree root is refused before any call.
  - Evidence that every recorded hash equals a hash recomputed from the stored file, including the rendered file.
  - Evidence that an injected interruption leaves zero records.
  - Architecture evidence that one type writes files and a seeded second fails the suite.
- Gate: Review Gate, Verification Gate

### T-054 Verify the per-item cap

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-024, T-035
- Traces to: S-014, A-003, A-007
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criteria 38, 39, 40, 41, 42 and 43.
- Acceptance Criteria:
  - Evidence with stub prices that the counted total never exceeds USD 5.95 and a cap-reached outcome names the total.
  - Evidence per attempt kind, failed, retried, timed out, recovered and usage-less, of its count against the cap.
  - Evidence that a run of a capped item is refused before any call and a concurrent pair counts at most the cap.
  - Evidence for a channel with no budget amount of admission within ceiling and cap, refusal past either, and zero amounts recorded.
- Gate: Review Gate, Verification Gate

### T-055 Verify booking in every billed unit kind

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-023, T-033, T-035
- Traces to: S-015, S-025, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criteria 44, 45 and 46.
- Acceptance Criteria:
  - Evidence that every operation of a stub-transport metered production books a stated cost from dated, sourced prices.
  - Evidence that a response reporting no usage books its admitted worst case labelled estimate.
  - Evidence of the priced-route rule with and without a cached-unit price, and a named refusal for an unpriced billed kind.
  - Evidence that the printed estimate is labelled estimate and holds no development cost.
- Gate: Review Gate, Verification Gate

### T-056 Verify the produce command's modes

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-035
- Traces to: S-016, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criteria 47, 48, 49 and 50.
- Acceptance Criteria:
  - Evidence of output order in every mode, plan before first call.
  - Evidence that plan-only mode makes zero calls and writes zero files.
  - Evidence per refusal condition of a refusal before any call naming it.
  - Evidence that one build reaches a stub transport in metered mode by configuration alone.
- Gate: Review Gate, Verification Gate

### T-057 Verify the owner guide

- Owner: omn-dev-2-reviewer
- Complexity: XS (confidence: medium)
- Depends on: T-041
- Traces to: S-017, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criteria 51 and 52.
- Acceptance Criteria:
  - Review evidence comparing each variable name with the metered refusal output.
  - Review evidence of zero secret values.
  - Review evidence of the three commands and the statements that a run publishes nothing, buys nothing and stops at the cap.
  - Review evidence that the preconditions of the go are listed.
- Gate: Review Gate, Verification Gate

### T-058 Verify the production path is re-pointable

- Owner: omn-dev-2-reviewer
- Complexity: XS (confidence: medium)
- Depends on: T-040
- Traces to: S-018, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criterion 53.
- Acceptance Criteria:
  - Scan evidence of zero subject terms and zero item 001 identifiers in production source.
  - Evidence that a seeded subject term fails the scan.
  - Review evidence that subject content sits only in item material.
  - Evidence that the delivered re-pointing tests pass.
- Gate: Review Gate, Verification Gate

### T-059 Verify the messages and image paths

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: low)
- Depends on: T-036
- Traces to: S-019, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criterion 54.
- Acceptance Criteria:
  - Contract-test evidence for each path against its dated fixture.
  - Evidence that each path's fake passes the same contract suite.
  - Evidence that item 001's run made no call on either path.
  - Evidence that a fake secret appears in no output of either path.
- Gate: Review Gate, Verification Gate

### T-060 Verify the produced item in the brief and the dashboard

- Owner: omn-dev-2-reviewer
- Complexity: S (confidence: medium)
- Depends on: T-038
- Traces to: S-020, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criterion 55.
- Acceptance Criteria:
  - Evidence over a demonstration store after a fake run of every new line from one read.
  - Evidence that fake lines read demonstration and add zero to observed spend.
  - Architecture evidence that no management path gained a write path, a clock or an action.
  - A recorded demonstration of the brief and the dashboard.
- Gate: Review Gate, Verification Gate

### T-061 Verify the carried recovery and the call bound

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-031, T-033, T-037
- Traces to: S-021, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criteria 56, 57 and 58.
- Acceptance Criteria:
  - Evidence with an injected transaction loss that the recovered attempt counts and zero further calls follow.
  - Evidence with an injected recording failure of a named failure carrying the attempt's facts.
  - Evidence with a stalled stand-in provider that each call ends within the stated bound.
  - Evidence that the plan states the bound.
- Gate: Review Gate, Verification Gate

### T-062 Verify the register entries

- Owner: omn-dev-2-reviewer
- Complexity: XS (confidence: high)
- Depends on: T-039
- Traces to: S-022, A-003
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criterion 59.
- Acceptance Criteria:
  - Evidence of the five answered entries citing their decisions.
  - Evidence of price capture and the register writer open.
  - Evidence that the report-week and seven-rule tests pass unchanged.
  - Evidence that no entry was edited in place.
- Gate: Review Gate, Verification Gate

### T-063 Verify the round trip of every timeout, hold and clock

- Owner: omn-dev-2-reviewer
- Complexity: L (confidence: low)
- Depends on: T-031, T-035, T-037
- Traces to: S-010, S-014, S-021, A-003
- Status: assumption-dependent
- Description: Recorded evidence shows that every timeout and hold T-008 enumerates ends under a named outcome during a run, that the cap's reservation and booking agree across every attempt kind, and that one clock decides every recorded order.
- Acceptance Criteria:
  - Evidence of a held admission and a stalled provider during a run, each ending under a named outcome inside the 30-second command timeout or its stated bound.
  - Evidence of zero open transactions and holds while a stand-in render runs.
  - Evidence with the process clock skewed a day each way that stage records, operations and artifact records keep one order.
  - Evidence comparing each attempt's reservation with its booking, including a timeout after a stub charge.
- Gate: Review Gate, Verification Gate

### T-064 Confirm the binding constraints hold after the change

- Owner: omn-dev-2-reviewer
- Complexity: M (confidence: low)
- Depends on: T-036, T-037, T-038, T-039, T-040, T-041
- Traces to: S-002, S-023, S-024, A-002, A-003, A-007
- Status: assumption-dependent
- Description: Recorded evidence supports upstream criteria 5 and 7 and shows zero spend, the exception unspent, the controls unchanged and no invented quantity.
- Acceptance Criteria:
  - Evidence of a full live run in the demonstration store with every Wave 8 live test passing and the architecture suite at least 63.
  - Read-only evidence that the company store holds zero tables.
  - Evidence that committed spend is zero and the single-metered-operation exception unspent.
  - Review evidence that no clip count, threshold, budget amount, channel configuration value, tier mapping or sizing claim is recorded outside demonstration stores.
- Gate: Review Gate, Verification Gate

### T-065 Record what the change delivers and leaves to the owner

- Owner: omn-documentation
- Complexity: M (confidence: low)
- Depends on: T-042, T-043, T-044, T-045, T-046, T-047, T-048, T-049, T-050, T-051, T-052, T-053, T-054, T-055, T-056, T-057, T-058, T-059, T-060, T-061, T-062, T-063, T-064
- Traces to: S-017, S-024, S-025, A-005
- Status: assumption-dependent
- Description: The release record states what the fake run produced, labelled demonstration, the metered run's estimate labelled estimate beside the cap, the exact credential variable names, the preconditions left to the orchestrator and the owner, and every open owner question unanswered.
- Acceptance Criteria:
  - The record labels every figure from the fake run a demonstration parameter and presents none as observed.
  - The record states the estimate from configured prices, labelled estimate, apart from development cost.
  - The record lists install and load of the company store, price re-fetch, terms verification, credential setting and the owner's go as preconditions.
  - The record states zero spend and the exception unspent.
- Gate: Closure Gate

## Dependencies

### 8.1 Dependency Edges

| From | To | Type | Justification |
|---|---|---|---|
| T-001 | T-002 | contract | Each vendor path returns content in the outcome shape that design fixes |
| T-001 | T-004 | contract | A fake returns content in the outcome shape that design fixes |
| T-007 | T-006 | decision-gate | The worst case is computed in the billed unit kinds that design fixes |
| T-002 | T-009 | contract | The split respects the vendor's per-request limit that design records |
| T-008 | T-009 | decision-gate | The narration split depends on the call bound that design proposes for ruling |
| T-008 | T-012 | decision-gate | The render boundary applies the bounds and the no-open-transaction rule that design states |
| T-013 | T-012 | contract | The renderer works under the output root and adoption rule that design fixes |
| T-005 | T-014 | decision-gate | The metered refusals depend on the store designation that design fixes |
| T-006 | T-014 | contract | The plan prints the worst case and the cap that design fixes |
| T-015 | T-014 | decision-gate | The exact credential variable names depend on how the channel identifier is fixed |
| T-005 | T-016 | contract | The demonstration label on each line is the one that design fixes |
| T-006 | T-016 | contract | Spend against the cap is read as that design records it |
| T-006 | T-017 | decision-gate | The recovery's count against the cap uses the hold that design states |
| T-004 | T-019 | contract | The assertions cover the fake types and keys that design names |
| T-012 | T-019 | contract | The single-starter assertion names the type that design fixes |
| T-013 | T-019 | contract | The single-writer assertion names the type that design fixes |
| T-001 | T-020 | contract | The build binds to the design T-001 records |
| T-004 | T-021 | contract | The build binds to the design T-004 records |
| T-020 | T-021 | produces-consumes | A fake returns content through the outcome that build delivers |
| T-005 | T-022 | contract | The build binds to the design T-005 records |
| T-007 | T-023 | contract | The build binds to the design T-007 records |
| T-006 | T-024 | contract | The build binds to the design T-006 records |
| T-023 | T-024 | produces-consumes | The worst case is priced in the unit kinds that build delivers |
| T-013 | T-025 | contract | The build binds to the design T-013 records |
| T-011 | T-026 | contract | The build binds to the design T-011 records |
| T-015 | T-026 | contract | The build binds to the design T-015 records |
| T-009 | T-027 | contract | The build binds to the design T-009 records |
| T-010 | T-027 | contract | The build binds to the design T-010 records |
| T-020 | T-027 | produces-consumes | Narration content arrives through the outcome that build delivers |
| T-025 | T-027 | produces-consumes | The audio is stored by the writer that build delivers |
| T-012 | T-028 | contract | The build binds to the design T-012 records |
| T-025 | T-028 | produces-consumes | The process works under the output root that build delivers |
| T-011 | T-029 | contract | The build binds to the design T-011 records |
| T-025 | T-029 | produces-consumes | Graphics are stored by the writer that build delivers |
| T-028 | T-029 | produces-consumes | Graphics are drawn through the process boundary that build delivers (A-006) |
| T-011 | T-030 | contract | The build binds to the design T-011 records |
| T-025 | T-030 | produces-consumes | Thumbnails are stored by the writer that build delivers |
| T-028 | T-030 | produces-consumes | Thumbnails are drawn through the process boundary that build delivers (A-006) |
| T-008 | T-031 | contract | The build binds to the design T-008 records |
| T-010 | T-031 | contract | The build binds to the design T-010 records |
| T-026 | T-031 | produces-consumes | Assembly reads the item material that load places |
| T-027 | T-031 | produces-consumes | Assembly consumes the narration audio and its measured duration |
| T-028 | T-031 | produces-consumes | The render runs through the process boundary that build delivers |
| T-029 | T-031 | produces-consumes | Assembly consumes the 22 graphic segments |
| T-014 | T-032 | contract | The build binds to the design T-014 records |
| T-021 | T-032 | produces-consumes | Fake mode runs through the fakes that build delivers |
| T-022 | T-032 | produces-consumes | Fake mode checks the store designation that build delivers |
| T-024 | T-032 | produces-consumes | Every call of the run is admitted through the cap that build delivers |
| T-030 | T-032 | produces-consumes | The run produces the thumbnails that build delivers |
| T-031 | T-032 | produces-consumes | The run ends in the render that build delivers |
| T-002 | T-033 | contract | The build binds to the design T-002 records |
| T-021 | T-033 | produces-consumes | The adapter passes the contract suite that the fake build delivers |
| T-003 | T-034 | contract | The build binds to the design T-003 records |
| T-015 | T-035 | contract | The build binds to the design T-015 records |
| T-032 | T-035 | produces-consumes | Metered mode extends the command that build delivers |
| T-033 | T-035 | produces-consumes | Metered mode reaches the speech path that build delivers |
| T-034 | T-035 | produces-consumes | Metered refusals include the excluded-service refusal that build delivers |
| T-033 | T-036 | produces-consumes | Both paths reuse the per-vendor authentication and contract suite that build delivers |
| T-017 | T-037 | contract | The build binds to the design T-017 records |
| T-024 | T-037 | produces-consumes | The recovered attempt counts against the cap that build delivers |
| T-016 | T-038 | contract | The build binds to the design T-016 records |
| T-032 | T-038 | produces-consumes | The lines read the produced item that the command records |
| T-018 | T-039 | contract | The build binds to the design T-018 records |
| T-019 | T-040 | contract | The build binds to the design T-019 records |
| T-025 | T-040 | produces-consumes | The single-writer assertion names the writer that build delivers |
| T-028 | T-040 | produces-consumes | The single-starter assertion names the boundary that build delivers |
| T-035 | T-040 | produces-consumes | The publication-path assertion scans the command that build delivers |
| T-036 | T-040 | produces-consumes | The scans cover the adapter types that build delivers |
| T-035 | T-041 | produces-consumes | The guide quotes the variable names metered mode prints |
| T-032 | T-042 | verification | The evidence covers what T-032 delivers |
| T-020 | T-043 | verification | The evidence covers what T-020 delivers |
| T-033 | T-044 | verification | The evidence covers what T-033 delivers |
| T-034 | T-045 | verification | The evidence covers what T-034 delivers |
| T-032 | T-046 | verification | The evidence covers what T-032 delivers |
| T-035 | T-046 | verification | The evidence covers what T-035 delivers |
| T-027 | T-047 | verification | The evidence covers what T-027 delivers |
| T-029 | T-048 | verification | The evidence covers what T-029 delivers |
| T-030 | T-049 | verification | The evidence covers what T-030 delivers |
| T-031 | T-050 | verification | The evidence covers what T-031 delivers |
| T-031 | T-051 | verification | The evidence covers what T-031 delivers |
| T-040 | T-051 | verification | The evidence covers what T-040 delivers |
| T-032 | T-052 | verification | The evidence covers what T-032 delivers |
| T-040 | T-052 | verification | The evidence covers what T-040 delivers |
| T-031 | T-053 | verification | The evidence covers what T-031 delivers |
| T-040 | T-053 | verification | The evidence covers what T-040 delivers |
| T-024 | T-054 | verification | The evidence covers what T-024 delivers |
| T-035 | T-054 | verification | The evidence covers what T-035 delivers |
| T-023 | T-055 | verification | The evidence covers what T-023 delivers |
| T-033 | T-055 | verification | The evidence covers what T-033 delivers |
| T-035 | T-055 | verification | The evidence covers what T-035 delivers |
| T-035 | T-056 | verification | The evidence covers what T-035 delivers |
| T-041 | T-057 | verification | The evidence covers what T-041 delivers |
| T-040 | T-058 | verification | The evidence covers what T-040 delivers |
| T-036 | T-059 | verification | The evidence covers what T-036 delivers |
| T-038 | T-060 | verification | The evidence covers what T-038 delivers |
| T-031 | T-061 | verification | The evidence covers what T-031 delivers |
| T-033 | T-061 | verification | The evidence covers what T-033 delivers |
| T-037 | T-061 | verification | The evidence covers what T-037 delivers |
| T-039 | T-062 | verification | The evidence covers what T-039 delivers |
| T-031 | T-063 | verification | The evidence covers what T-031 delivers |
| T-035 | T-063 | verification | The evidence covers what T-035 delivers |
| T-037 | T-063 | verification | The evidence covers what T-037 delivers |
| T-036 | T-064 | verification | The evidence covers what T-036 delivers |
| T-037 | T-064 | verification | The evidence covers what T-037 delivers |
| T-038 | T-064 | verification | The evidence covers what T-038 delivers |
| T-039 | T-064 | verification | The evidence covers what T-039 delivers |
| T-040 | T-064 | verification | The evidence covers what T-040 delivers |
| T-041 | T-064 | verification | The evidence covers what T-041 delivers |
| T-042 | T-065 | produces-consumes | The record states what T-042 evidenced |
| T-043 | T-065 | produces-consumes | The record states what T-043 evidenced |
| T-044 | T-065 | produces-consumes | The record states what T-044 evidenced |
| T-045 | T-065 | produces-consumes | The record states what T-045 evidenced |
| T-046 | T-065 | produces-consumes | The record states what T-046 evidenced |
| T-047 | T-065 | produces-consumes | The record states what T-047 evidenced |
| T-048 | T-065 | produces-consumes | The record states what T-048 evidenced |
| T-049 | T-065 | produces-consumes | The record states what T-049 evidenced |
| T-050 | T-065 | produces-consumes | The record states what T-050 evidenced |
| T-051 | T-065 | produces-consumes | The record states what T-051 evidenced |
| T-052 | T-065 | produces-consumes | The record states what T-052 evidenced |
| T-053 | T-065 | produces-consumes | The record states what T-053 evidenced |
| T-054 | T-065 | produces-consumes | The record states what T-054 evidenced |
| T-055 | T-065 | produces-consumes | The record states what T-055 evidenced |
| T-056 | T-065 | produces-consumes | The record states what T-056 evidenced |
| T-057 | T-065 | produces-consumes | The record states what T-057 evidenced |
| T-058 | T-065 | produces-consumes | The record states what T-058 evidenced |
| T-059 | T-065 | produces-consumes | The record states what T-059 evidenced |
| T-060 | T-065 | produces-consumes | The record states what T-060 evidenced |
| T-061 | T-065 | produces-consumes | The record states what T-061 evidenced |
| T-062 | T-065 | produces-consumes | The record states what T-062 evidenced |
| T-063 | T-065 | produces-consumes | The record states what T-063 evidenced |
| T-064 | T-065 | produces-consumes | The record states what T-064 evidenced |
| omn-orchestrator, for the installed renderer | T-028 | external | The boundary is exercised against the renderer installed on the build machine and located by configuration |
| omn-orchestrator, for the demonstration database | T-042 | external | The live end-to-end run needs the separate demonstration database served |
| omn-orchestrator, for the demonstration database | T-064 | external | The full live suite needs the separate demonstration database served |

### 8.2 External Dependencies

| Responsible party | What is needed | Blocks |
|---|---|---|
| omn-orchestrator, for the installed renderer | The renderer installed on the build machine at version 9.0.2, user scope, reachable through the configured setting; already installed with the owner's approval (R-027) | T-028 |
| omn-orchestrator, for the demonstration database | PostgreSQL 17 serving the separate demonstration database, and no other database named by any test or demonstration (R-023, R-028) | T-042, T-064 |
| The owner, through omn-orchestrator | Credential variables set, the company store installed and loaded, every unit price re-fetched and both vendors' terms verified at first hand, and the explicit go (R-003, R-026) | none in this plan; the metered run after this change closes |

### 8.3 Implementation Order

- Wave 1: T-001, T-003, T-005, T-007, T-008, T-010, T-011, T-013, T-015, T-018
- Wave 2: T-002, T-004, T-006, T-012, T-020, T-022, T-023, T-025, T-026, T-034, T-039
- Wave 3: T-009, T-014, T-016, T-017, T-019, T-021, T-024, T-028, T-043, T-045, T-062
- Wave 4: T-027, T-029, T-030, T-033, T-037
- Wave 5: T-031, T-036, T-044, T-047, T-048, T-049
- Wave 6: T-032, T-050, T-059, T-061
- Wave 7: T-035, T-038, T-042
- Wave 8: T-040, T-041, T-046, T-054, T-055, T-056, T-060, T-063
- Wave 9: T-051, T-052, T-053, T-057, T-058, T-064
- Wave 10: T-065

The zero-spend end-to-end run is delivered in wave 6 (T-032) and evidenced in wave 7 (T-042); no vendor adapter, metered-mode or should-have task lies on that path, so the speech adapter (T-033) and the messages and image adapters (T-036) are proven against dated fixtures and stub transports beside it, never ahead of it.

## Suggested Workflow

Selected workflow: implement-feature

Selected because the change delivers new capability against an approved scope with measurable acceptance criteria, and the run is already routed through this workflow's phase model.

| Phase | Tasks |
|---|---|
| scope-and-acceptance | none; the phase is complete and its artifact is this plan's upstream input |
| execution-planning | none; this plan is the phase's own output |
| solution-design-and-risk-assessment | T-001, T-002, T-003, T-004, T-005, T-006, T-007, T-008, T-009, T-010, T-011, T-012, T-013, T-014, T-015, T-016, T-017, T-018, T-019 |
| implementation | T-020, T-021, T-022, T-023, T-024, T-025, T-026, T-027, T-028, T-029, T-030, T-031, T-032, T-033, T-034, T-035, T-036, T-037, T-038, T-039, T-040, T-041 |
| quality-review | T-042, T-043, T-044, T-045, T-046, T-047, T-048, T-049, T-050, T-051, T-052, T-053, T-054, T-055, T-056, T-057, T-058, T-059, T-060, T-061, T-062, T-063, T-064 |
| documentation-and-release-handoff | T-065 |

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
| architecture-analysis | T-001, T-002, T-003, T-004, T-005, T-006, T-007, T-008, T-009, T-010, T-011, T-012, T-013, T-014, T-015, T-016, T-017, T-018, T-019 | architect | Primary |
| technical-approach-definition | T-001, T-002, T-004, T-006, T-007, T-009, T-012, T-013, T-014, T-015 | architect | Primary |
| reuse-assessment | T-001, T-003, T-006, T-016, T-017, T-018 | architect | Primary |
| option-evaluation | T-005, T-010, T-011, T-012, T-015 | architect | Primary |
| structural-risk-analysis | T-006, T-008, T-012, T-013, T-019 | architect | Primary |
| implementation-delivery | T-020, T-021, T-022, T-023, T-024, T-025, T-026, T-027, T-028, T-029, T-030, T-031, T-032, T-033, T-034, T-035, T-036, T-037, T-038, T-039, T-040, T-041 | omn-dev-1-implement | Primary |
| quality-verification | T-042, T-043, T-044, T-045, T-046, T-047, T-048, T-049, T-050, T-051, T-052, T-053, T-054, T-055, T-056, T-057, T-058, T-059, T-060, T-061, T-062, T-063, T-064 | omn-dev-2-reviewer | Primary |
| validation-design | T-046, T-054, T-063 | omn-dev-2-reviewer | Primary |
| code-review | T-044, T-048, T-051, T-052, T-053, T-058, T-064 | omn-dev-2-reviewer | Primary |
| documentation | T-065 | omn-documentation | Primary |
| release-communication | T-065 | omn-documentation | Primary |

### 10.2 Required Skills

| Skill | File | Tasks | Level |
|---|---|---|---|
| S01 | skills/architecture/clean-architecture-checklist.md | T-001, T-004, T-012, T-013, T-019, T-040 | Advisory |
| S02 | skills/business/domain-modeling.md | T-005, T-006, T-007, T-010, T-011, T-014, T-015, T-018 | Primary |
| S03 | skills/dotnet/engineering-playbook.md | T-020, T-021, T-022, T-023, T-024, T-025, T-026, T-027, T-028, T-029, T-030, T-031, T-032, T-033, T-034, T-035, T-036, T-037, T-038, T-039, T-040 | Secondary |
| S06 | skills/database/database-engineering.md | T-005, T-006, T-007, T-008, T-017, T-022, T-023, T-024, T-037 | Secondary |
| S07 | skills/testing/testing-strategy.md | T-042, T-043, T-044, T-045, T-046, T-047, T-048, T-049, T-050, T-051, T-052, T-053, T-054, T-055, T-056, T-057, T-058, T-059, T-060, T-061, T-062, T-063, T-064 | Secondary |
| S08 | skills/performance/performance-engineering.md | T-008, T-009, T-012, T-031, T-063 | Advisory |
| S09 | skills/security/secure-engineering.md | T-002, T-003, T-004, T-012, T-033, T-035, T-044, T-046 | Advisory |
| S10 | skills/git/git-collaboration.md | T-013, T-041, T-065 | Advisory |
| S11 | skills/logging/observability-logging.md | T-033, T-065 | Advisory |
| S12 | skills/error-handling/error-handling-strategy.md | T-006, T-008, T-017, T-028, T-037 | Advisory |

## Acceptance Criteria

1. In a fresh demonstration store, one fake-mode invocation of the produce command for item 001 yields one decodable file under the output root with one video and one audio stream, its runtime measured from the file within one frame beside the specified runtime, twelve stage outcomes, one operation per invocation, zero network requests beyond in-process stand-ins and USD 0.00 labelled demonstration. Verifies business objective 1. Evidence: the run and evidence recorded in T-042, T-046, T-047, T-048, T-049 and T-050.
2. Plan-only mode prints every planned operation, each estimate and the total labelled estimate, and the cap with its source with zero calls and zero files; metered mode refuses before any call naming each unmet condition and the exact variable names; the owner guide quotes those names exactly; and one build reaches a stub transport in metered mode by configuration alone. Verifies business objective 2. Evidence: the demonstrations and review recorded in T-044, T-055, T-056 and T-057, and the release record produced by T-065.
3. With stub prices binding mid-production, the total counted against the cap never exceeds USD 5.95 across failed, retried, timed-out, recovered and usage-less attempts and a concurrent pair, no unknown charge is booked at zero, and every timeout and hold on the path ends under a named outcome. Verifies business objective 3. Evidence: the evidence recorded in T-054, T-055, T-061 and T-063.
4. After a complete run the publish-ready predicate refuses the item naming the Production and Copyright check holds, no publication path is reachable from the produce command, the five structural absences hold, and an excluded service as the only candidate is refused with zero requests. Verifies business objective 4. Evidence: the evidence recorded in T-045 and T-052.
5. Every operation of a fake run reads demonstration at zero and adds zero to observed spend, benchmark observations and comparable-run counts, and the brief and the dashboard show the produced item, its operations and its spend against the cap from one read. Verifies business objective 5. Evidence: the evidence recorded in T-046 and T-060.
6. Every recorded hash equals the hash of the stored file, one type starts processes and one type writes files, production source holds no subject term, every Wave 8 live test still passes with the architecture suite at 63 or more, and the company store holds zero tables. Verifies business objective 6. Evidence: the evidence recorded in T-043, T-051, T-053, T-058 and T-064.
7. The messages and image paths pass the same contract checks as the speech path against dated fixtures and fakes, and item 001's run calls neither. Verifies business objective 7. Evidence: the evidence recorded in T-059.
8. The register shows the five answers of 2026-10-09 answered and price capture and the register writer open, with the week and the seven-rule catalogue unchanged. Verifies business objective 8. Evidence: the evidence recorded in T-062.

## Definition of Done

- [ ] All eight plan acceptance criteria are verified with recorded evidence
- [ ] Design Gate, Review Gate, Verification Gate and Closure Gate are approved with owners recorded
- [ ] Every task acceptance criterion is satisfied or formally waived with the waiver recorded
- [ ] Every assumption is confirmed by its named role or converted to a recorded decision
- [ ] Every risk is closed or accepted with its named owner recorded
- [ ] Every open question is closed or explicitly accepted by its named owner
- [ ] Committed spend is recorded as USD 0.00 and the single-metered-operation exception as unspent
- [ ] The company store is recorded as holding zero tables after every phase
- [ ] The owner guide and the release record, with its release-impact notes, are published
- [ ] Durable outcomes are recorded to memory per `memory/memory-governance.md`

## Open Questions

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| Q-001 | The closed stage order places Production before Audio while assembly consumes the narration; how are outcomes recorded without silently reordering the closed set, or is a reorder a recorded design decision? Carried from the scope. | no | architect | T-010, T-031, T-032 |
| Q-002 | Beat boundaries exist only as cue phrases in the script's prose and the graphics need content beyond the one-line shot list; how is each held as reviewable item material quoting its source, with nothing added? Carried from the scope. | no | architect | T-011, T-026, T-029, T-031 |
| Q-003 | Can each narration call complete inside the 60-second provider-call bound under which the admission transaction is held, or does the bound change for speech? Carried from the scope; prepared by T-008 and T-009 and ruled at the Design Gate (A-004). | no | omn-tech-lead | T-008, T-009, T-027, T-033 |
| Q-004 | Nothing loads the company store's company, channel, item, account, route, price and cap records beyond the schema the orchestrator installs, and the delivered credential variable name embeds the channel's identifier, minted when its row is written; how do the records reach the company store, and how are exact variable names stated before that row exists? | no | architect, with omn-orchestrator for the pre-run load | T-015, T-026, T-035, T-041 |
| Q-005 | What time bounds govern the render, the probe and the full decode, which have none today? Non-blocking: the architect proposes in T-008 and omn-tech-lead rules at the Design Gate; no value is invented here. | no | architect | T-008, T-012, T-028, T-031 |
| Q-006 | Measuring the runtime, decoding the file end to end and measuring narration duration each start the external tool again, and the renderer writes the rendered file itself; does one process-starting type covering every such run, and a renderer-written file adopted by the one writer, satisfy the scope's single-starter and single-writer items? | no | architect | T-012, T-013, T-028, T-040 |
| Q-007 | The stage record admits one outcome per stage per item version; what does a second run of the same version record after a cap stop or a failed render, and are the package's recorded Held outcomes loaded as stage evidence before the run? | no | architect | T-010, T-026, T-032 |
| Q-008 | Metered mode accepts the company store only and no test may touch it, while upstream criteria 38, 44, 49 and 50 require stub-transport metered production in tests; how is metered behaviour evidenced without the refusal admitting any store a run can reach? | no | architect, with omn-product-owner | T-005, T-022, T-035, T-054, T-055, T-056 |
| Q-009 | No phase reaches an external system, so the dated reference fixtures of each vendor's published shapes can only record the implementing role's knowledge with its source and date; is that the accepted source, with first-hand verification left to the orchestrator before the go? | no | omn-orchestrator | T-002, T-033, T-036, T-044 |
| Q-010 | The recorded package states no alternative text for the thumbnail candidates, while upstream criterion 24 screens it; is it item material to be authored and reviewed, or does the owner supply it? | no | omn-product-owner, with architect | T-011, T-030, T-049 |

## Traceability Matrix

| Statement | Covered by |
|---|---|
| S-001 | T-010, T-014, T-015, T-026, T-031, T-032, T-042, A-001, A-012, Q-001, Q-007 |
| S-002 | T-042, T-046, T-064, A-002, A-007 |
| S-003 | T-001, T-020, T-043 |
| S-004 | T-002, T-033, T-044, A-008, Q-009 |
| S-005 | T-003, T-034, T-045 |
| S-006 | T-004, T-005, T-021, T-022, T-046, A-009 |
| S-007 | T-009, T-027, T-047, Q-003 |
| S-008 | T-011, T-029, T-048, A-006, A-011, Q-002 |
| S-009 | T-011, T-030, T-049, Q-010 |
| S-010 | T-008, T-012, T-031, T-050, T-063, Q-005 |
| S-011 | T-012, T-019, T-028, T-040, T-051, Q-006 |
| S-012 | T-010, T-019, T-031, T-040, T-052 |
| S-013 | T-013, T-025, T-040, T-053, Q-006 |
| S-014 | T-006, T-024, T-054, T-063 |
| S-015 | T-007, T-023, T-055 |
| S-016 | T-005, T-014, T-015, T-022, T-032, T-035, T-056, Q-004, Q-008 |
| S-017 | T-014, T-015, T-041, T-057, T-065, A-005, Q-004 |
| S-018 | T-011, T-019, T-026, T-040, T-058, A-011 |
| S-019 | T-002, T-036, T-059, A-010 |
| S-020 | T-005, T-016, T-038, T-060 |
| S-021 | T-006, T-008, T-017, T-037, T-061, T-063, A-004 |
| S-022 | T-018, T-039, T-062 |
| S-023 | T-019, T-040, T-064 |
| S-024 | T-064, T-065 |
| S-025 | T-007, T-014, T-032, T-055, T-065 |

Statement register, normalised from the supplied feature request and the upstream scope definition of this run (digest sha256:1a2280e9723a272aae24f53bf714e6e9): the first twenty-two statements match the upstream in-scope items in order and carry the same identifiers; the last three carry binding constraints of the request, namely S-023 the completed waves built upon with every Wave 8 live test passing and the boundary assertions extended and never weakened, S-024 no invented quantity and no fake figure presented as observed, and S-025 the estimate recomputed from configured unit prices and labelled, with development cost and per-item cost never netted. Q-001, Q-002 and Q-003 carry the scope's three questions under the same identifiers and owners; Q-004 to Q-010 are raised here. Upstream acceptance criteria are cited by ordinal in the order the scope definition lists them, because their identifiers share this plan's assumption prefix. Repository rescan: the task context names no relevant module or file, so the delivered source in the feature worktree was read before planning as the dispatch briefing directs; the counts match the request at 117 and 7 under the source tree and 42 and 5 under the test tree, with eight schema resources.
