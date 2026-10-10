# MC-11: Wave 10: the company's own voice - narration from a human recording the company holds or an open-licensed voice model run in house, so item 001 is produced with a real voice and no vendor

- Source: local://MASTER-PLAN.md#35-development-waves
- Issue type: Story   Priority: High
- Labels: implement, wave-10, narration, in-house-voice, production
- Routed as: /implement (feature-request)

## Description

WAVE 10 — THE COMPANY'S OWN VOICE: NARRATION FROM A HUMAN RECORDING THE COMPANY HOLDS, OR FROM AN OPEN-LICENSED VOICE MODEL RUN IN HOUSE, SO ITEM 001 IS PRODUCED WITH A REAL VOICE AND NO VENDOR

Waves 0 to 9 are Completed (Wave 9: `run-7ae81c0de400`, PR 9). Wave 10 builds on all of them and must not re-create any of them. At the Wave 9 head: version 1.6.0, nine schema resources, 857 tests pass live in the demonstration store, architecture suite 80 (confirm by counting; every previous ticket's counts were stale by the time they were read). The `produce` command renders item 001's recorded package to a verified video file; its narration today comes only from a pay-per-use speech vendor (live) or a fake (demonstration).

### WHY — THE OWNER'S DECISION OF 2026-10-10 (`CEO-D-805`, at the end of `research/ceo-decision-record.md`)

The first metered attempt (2026-10-10 07:50 UTC, company store, item version 2) stopped at the first narration request: the speech vendor answered HTTP 429; the run stopped without retry; the attempt is booked at its worst case USD 0.007695, labelled estimate; the vendor's error body was not recorded, so the cause is unknown. No video was produced. **The owner then decided the company's voice must not depend on that vendor.** Narration comes from the company itself, in both forms:

1. **A human recording**: a person working for the company records the narration; the pipeline imports the recorded audio, checks it and assembles the video from it.
2. **A voice model run in house**: an open-licensed speech model running on the company's own machine, used when no human recording exists — no vendor, no per-use charge, no content leaving the machine.

The speech vendor's route is not attempted again. Decision block: `CEO-D-900`–`CEO-D-999`, `CEO-C-900`–`CEO-C-999`.

### GOAL

`produce` turns item 001 into a rendered video whose narration is the company's own — the imported human recording when one is registered for the item, otherwise the in-house voice model — at **USD 0.00 metered spend**, every narration source recorded with its provenance, and the choice between the two deterministic and visible in the plan before anything runs.

### WHAT TO BUILD

1. **Narration sources as a closed set**: human recording, in-house model, (existing) vendor route, fake. The plan states which source each part uses and why. The vendor route stays in code but is not selectable for item 001 while the owner's decision stands.
2. **Human recording import**: register a recorded audio file (whole narration or per beat, as the item material's beats define) against an item version; probe and decode it through the existing one process starter; measure duration and loudness from the file and record them as observations; record its hash, the performer's recorded identity and the rights position (the company holds the recording and the performer's release — **an owner question if no release is recorded; nothing is assumed**). No loudness or duration target may be invented; if a target is needed it is raised as an owner question. A recording whose duration departs from the narration text's expectation is reported, not silently stretched.
3. **In-house voice model**: select an open-licensed text-to-speech model and voice that run locally on Windows without a GPU requirement (the scope phase proposes candidates with their licences for weights, code and voices, and their run-time requirements; the owner confirms before any download). Run it through a narrow, architecture-tested boundary (the existing one process starter or a sibling the design justifies), deterministically where the model allows, writing only through the one writer; record the model, its version, its licence and the voice with every artifact. The model files are installed outside the repository; their hashes are recorded.
4. **Assembly unchanged in shape**: the rendered video, its decode check and its runtime measured from the file, exactly as Wave 9 delivered, with the narration now from the company's own source.
5. **The vendor error body**: carried defect — when a vendor answers non-success, record its status and its error code or message (never a credential, truncated and screened), so a cause like the 429 of 2026-10-10 is knowable. Cover it with a test against a stub.
6. **Carried from Wave 9**: F-011 (the weekly controller-decision label wording in a demonstration store); the plan's total line printing "not verified first hand" after a first-hand re-fetch; the items in `wave-9/release-note.md` routed to the implementer or tech lead that touch narration, providers or the produce command.
7. **Owner documentation**: how to record (format, sample rate, file naming per beat), how to register a recording, how the in-house model is installed and verified, and the exact commands.

### NOT IN THIS WAVE

Any call to the speech vendor or any other metered service; any publication, upload, channel creation or subscription; real motion graphics (the 22 stills stay text cards — a later wave); stock footage or music; voice cloning of any real person without a recorded release; any executive or narrative model call (`CEO-D-700`).

### BINDING CONSTRAINTS CARRIED FORWARD

1. **Nothing publishes.** The structural absences on the upload path stay; the held-stage refusal stays.
2. Budget USD 77.41 per month; company metered ceiling USD 34.42; the `CEO-D-800` cap of USD 5.95 keeps its counted total of USD 0.007695 (estimate) and is not spent further.
3. **Code first, AI when necessary.** The in-house model is the one AI component this wave adds, and it runs locally.
4. Owner approval remains a transition-table state; D-002 is not relaxed.
5. One legal entity, several channels; no second channel.
6. No threshold, target, cadence, loudness level, duration tolerance or rights position may be invented.
7. Demonstrations and the live test suite use the demonstration store only. **The company store `mediacompany` now holds the first real records** (installed and prepared 2026-10-10, backed up before the run, item version 2 failed at Audio); no phase, test or demonstration touches it. Any further run against it is the orchestrator's, on the owner's go.
8. Downloading a model or tool is an owner-approved action, named with its source, size and licence before it happens.
9. Niche-agnostic and re-pointable.

### FRAMEWORK DEFECTS TO BRIEF EVERY SUBAGENT ON

All in `research/framework-defects.md` (twelve). Give every agent its own staging folder, and never brief a non-implementing role to write a repository file.

### PRICING BASIS

Metered spend for item 001 in this wave: none. Local compute and the owner's recording time are real costs that are not metered; record them as unmeasured, never as zero. Development cost and per-item cost are separate quantities and must not be netted.

## Acceptance criteria

_Not provided in the ticket. To be defined and approved at the scope gate before implementation starts._
