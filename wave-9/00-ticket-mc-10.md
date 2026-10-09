# MC-10: Wave 9: the production capability - item 001 rendered to a video file end to end at zero spend against fake providers, ready for one owner-released metered run under a USD 5.95 cap - nothing publishes

- Source: local://MASTER-PLAN.md#35-development-waves
- Issue type: Story   Priority: High
- Labels: implement, wave-9, production, providers, render
- Routed as: /implement (feature-request)

## Description

WAVE 9 — BUILD THE PRODUCTION CAPABILITY THAT TURNS ITEM 001 INTO A RENDERED VIDEO FILE, PROVEN AT ZERO SPEND AGAINST FAKE PROVIDERS, AND READY FOR ONE OWNER-RELEASED METERED RUN UNDER A USD 5.95 CAP; NOTHING PUBLISHES

Waves 0 (`run-258e0a3415d2`), 1 (`run-dd80173faaad`), 2 (`run-3a58551ee912`), 3 (`run-ad369fe67ded`), 4 (`run-5d5e6bd74c36`), 5 (`run-423e990b743d`), 6 (`run-cdce6ebe03ac`), 7 (`run-56bcc037b633`) and 8 (`run-79ce8c121936`) are all Completed. Wave 9 builds on all of them and must not re-create any of them. At the Wave 8 head the solution carries 117 C# sources and 7 project files under `src/` and 42 C# sources and 5 project files under `tests/` (tracked files; confirm by counting). **736 tests pass against a live PostgreSQL 17** in the separate demonstration database (582 pass and 148 skip with a recorded reason without one); the architecture suite is at 63. Eight ordered schema resources. Version 1.5.0.

### WHY THE WAVE CHANGED FOCUS — THE OWNER'S DECISIONS OF 2026-10-09, RECORDED AT THE END OF `research/ceo-decision-record.md` IN THE FIRST COMMIT OF THIS WAVE

MASTER-PLAN section 35 names Wave 9 "the autonomous company". The owner decided instead, at the start of this wave:

1. **The first metered video is authorised**: produced, **never published**, with a **hard cap of USD 5.95 of metered spend for that video**, all attempts, retries and failed calls included, inside the USD 34.42 company metered ceiling. This is a new authority, separate from the earlier single-metered-operation exception, which stays unspent.
2. **Build the capability first, spend nothing in the phases.** Every phase of this wave runs at USD 0.00 against fake providers. The owner holds billed API accounts and will set the credentials on this machine **after** the wave reports the exact variable names; the single metered run is then executed by the orchestrator, outside the phases, only on the owner's explicit go. No phase performs, plans to perform or tests against a real metered call.
3. **No subscriptions are bought.** Option O-002's standing services (stock library, music library, the per-character narration vendor's entitlement; USD 42.99 per month) are not available. The pipeline must not depend on them for this video.
4. The autonomy capability (planning, scheduling, channel lifecycle, experiments, budget allocation, learning) is **deferred to a later wave**.

Also answered (Wave 8 owner questions): the report week stays the UTC week; the CTO recommendation list has **seven** items; the orchestrator records platform-policy re-verification results once per wave. Price capture at admission and the register writer remain open.

### WHAT STANDS BETWEEN THE CODE AND A VIDEO TODAY (orchestrator survey, confirm before relying on it)

- The only provider adapter (`src/MediaCompany.Capability/Providers/HttpProviderAdapter.cs`) posts a fixed body to a configured URI, reads invented usage headers and discards the response body; the gateway outcome carries an operation record and **no content**. No vendor request format, response parsing or usage parsing exists. The credential broker attaches only a bearer header.
- Of the twelve stages (`src/MediaCompany.Domain/Production/ProductionStage.cs`) only the copyright check has a handler. No stage produces media. Nothing renders; the host states that nothing writes a file. The host has no production command and passes no provider endpoints.
- **Item 001 is already scripted and recorded** under `wave-2/item-001/`: the subject is the owner's (the honey bee waggle dance), the narration text is final (11,096 characters), the shot list specifies 22 original motion graphics, 18 stock clips (three unsourced) and six thumbnail candidates, and every claim is attributed. Production, Audio and Copyright check are recorded Held.
- `ffmpeg` has been installed on this machine for the owner (winget package `Gyan.FFmpeg`); no code uses it yet.

### GOAL

One command turns item 001's recorded package into a rendered video file with recorded runtime, every stage outcome and every operation recorded, the cost booked against a per-item cap — demonstrated end to end at USD 0.00 with fake providers in the demonstration store, and switchable to the real providers by configuration and owner-set credentials alone, with no code change between the zero-spend run and the metered run.

### WHAT TO BUILD

1. **Content through the provider boundary.** The gateway outcome carries the produced content (text, audio bytes, image bytes) beside its operation record. Vendor adapters for pay-per-use services the owner can bill without a subscription: a reasoning vendor's messages API and a speech-and-image vendor's speech and image endpoints, each with the vendor's real request shape, authentication header scheme, response parsing and usage parsing from the response itself (no invented headers). The credential broker supports per-vendor header schemes. Excluded-service rules (RK-012: no service that forbids automated access; no narration terms that take a licence over customer content) are applied by the routing table, not by convention.
2. **Fake providers** with the same contracts that return deterministic content and recorded zero cost, so the whole pipeline runs in tests and demonstrations at USD 0.00. A fake provider is selectable only by configuration that cannot be confused with a real endpoint; a metered run can never silently fall back to a fake, and a fake can never be booked as a metered observation.
3. **Stage handlers** for the stages item 001 needs: Audio (narration from the recorded narration text), Design (the 22 motion graphics rendered by code from the shot list — deterministic, no model call), Thumbnail (image generation from the recorded thumbnail specification, or code-rendered where the specification allows), Production (assembly). Script and research are **read from the recorded package, not regenerated** unless the scope phase shows a stage needs a model call; any model call must be justified against "code first, AI when necessary".
4. **Assembly and render** with `ffmpeg`, invoked from one narrow, architecture-tested process boundary: timeline from the shot list, narration, motion graphics, stills and thumbnails into one video file; the runtime measured from the rendered file and recorded. The 18 stock clips are **absent** in this video (no subscription); the cut substitutes recorded, labelled placeholders or code-rendered cards, and the item stays held short of publish-ready under its existing conditions (library unregistered, clips unsourced). No music.
5. **Artifact storage.** Produced files are written only under one configured output root outside the tracked tree, by one writer the architecture suite constrains; content addresses or hashes recorded with each artifact. The prohibition on writing files is narrowed to exactly this, not removed.
6. **The per-item cap.** A run-level cap of USD 5.95 enforced through the existing admission, booking and cost controller: every call is admitted against the cap before it is made, the run stops at the cap rather than exceeding it, and a stopped run is a recorded outcome. Unit prices come from recorded configuration with their source and date; the price-capture rule stays as Wave 8 left it (open owner question).
7. **A `produce` host command** for one item: `--fake` (default in tests and demonstrations) and a metered mode that refuses unless every required credential variable is present, the cap is recorded, and the store is the one named for the run. The command prints, before any call, the planned operations, their estimated cost under the configured prices, and the cap.
8. **The weekly brief and dashboard** read the produced item, its operations and its spend from the existing single read: a fake run is labelled as a demonstration and never counted as observed spend.
9. **Documentation for the owner**: the exact credential variable names, how to set them, the dry-run and metered commands, and what the run will and will not do.
10. **Carried from Wave 8**: items in `wave-8/release-note.md` routed to the implementer or tech lead that touch production, providers, routing or the cost controller; the owner answers above encoded where code reads them (seven recommendation items, UTC week — confirm no change is needed).

### NOT IN THIS WAVE

The autonomy capability; any publication, upload, channel creation or channel configured as live; any subscription, account creation or purchase; any metered call inside a phase; stock footage, music, AI video cutaways; any executive or narrative model call (`CEO-D-700` stands for reports); any production, quality, throughput or revenue figure from the fake run presented as observed.

### BINDING CONSTRAINTS CARRIED FORWARD

1. **Nothing publishes.** The five structural absences on the upload path stay.
2. The three coded preconditions stay encoded and undischarged; satisfied rows only as fixtures in self-dropping demonstration stores.
3. Budget USD 77.41 per month; company metered ceiling USD 34.42; per-item cap USD 5.95 for this video.
4. **Code first, AI when necessary.**
5. Owner approval remains a transition-table state, absent from configuration; D-002 is not relaxed.
6. One legal entity, several channels; no second channel.
7. Ten comparable runs per task before an evidence ranking; one video does not make a ranking.
8. No clip count, threshold, cadence, observation minimum, channel budget amount, channel configuration value, level-to-tier mapping or sizing claim may be invented.
9. Niche-agnostic and re-pointable: nothing in the pipeline is specific to bees or to item 001 beyond its recorded package.
10. Demonstrations and the live test suite use the demonstration store only; the company store holds no tables and stays untouched in every phase.

### FRAMEWORK DEFECTS TO BRIEF EVERY SUBAGENT ON

All eleven in `research/framework-defects.md`, plus the twelfth measured in Wave 8: a recorded policy exception does not widen the permitted write set at completion. Give every agent its own staging folder, and never brief a non-implementing role to write a repository file.

### PRICING BASIS

Option O-002 remains the planning basis (USD 2.647440 per item variable). This video deviates from it — pay-per-use speech instead of the per-character subscription vendor, no stock, no music, no AI cutaways — and its estimate must be recomputed from the configured unit prices and labelled an estimate. Development cost and per-item cost are separate and must not be netted. Wave 8 closed at the figure recorded in `wave-8/cost-ledger.md`.

## Acceptance criteria

_Not provided in the ticket. To be defined and approved at the scope gate before implementation starts._
