# MC-12: Wave 11: real graphics drawn by code - item 001's 22 motion graphics rendered on the company's machine from its shot list and claims, replacing the text cards, at no cost

- Source: local://MASTER-PLAN.md#35-development-waves
- Issue type: Story   Priority: High
- Labels: implement, wave-11, graphics, motion-graphics, production
- Routed as: /implement (feature-request)

## Description

WAVE 11 — REAL GRAPHICS DRAWN BY CODE: ITEM 001'S 22 MOTION GRAPHICS RENDERED ON THE COMPANY'S MACHINE FROM ITS SHOT LIST AND CLAIMS, REPLACING THE TEXT CARDS, AT NO COST

Waves 0 to 10 are Completed (Wave 10: `run-dfcae1756c32`, PR 10). Wave 11 builds on all of them and must not re-create any of them. At the Wave 10 head: version 1.7.0, ten schema resources, 934 tests pass live in the demonstration store, architecture suite 88 (confirm by counting; every previous ticket's counts were stale by the time they were read). The `produce` command renders item 001 to a verified video narrated by the company's own voice (the in-house Piper model, or the CEO's registered recording); the company store holds item 001 version 3, produced 2026-10-10 in own mode, 629.733 s, held short of publish-ready.

### WHY — THE OWNER'S DECISION OF 2026-10-10 (`CEO-D-903`, at the end of `research/ceo-decision-record.md`)

The video exists, but its picture is a skeleton: the 22 graphic positions (`GFX-01` to `GFX-22` in `wave-2/item-001/shot-list.md` and `item-material.json`) are text cards that display the shot list's own words, and the 18 clip positions are labelled placeholders. **The owner decided that Wave 11 draws the 22 graphics for real, by code, on the company's machine** — the plan view of nest and resource, the annotated figure of eight, the oscillation trace against a time axis, and so on, each as the shot list specifies — so the video becomes watchable. Decision block: `CEO-D-1000`–`CEO-D-1099`, `CEO-C-1000`–`CEO-C-1099`.

### GOAL

`produce` renders item 001 with **22 real graphics drawn by code** in place of the text cards, each graphic traced to its shot-list row and to the claims it depicts (`claim-to-source.md`), every number on screen quoted from a recorded claim, never invented; at **USD 0.00 metered spend**, with no model call of any kind for the graphics, deterministic (the same inputs give the same frames, checked by hash), and the narration path of Wave 10 unchanged.

### WHAT TO BUILD

1. **A graphic specification per position**, derived from the shot list and claims: what is drawn (shapes, axes, labels, annotations), every label and number quoted from a recorded source with its citation; where the shot list leaves a visual choice open, the scope phase states it and the owner decides — no quantity, scale or relationship may be invented.
2. **A code renderer** for those specifications (vector drawing to frames, or ffmpeg filter graphs, or a library the design justifies — the scope proposes candidates with licences; any download is owner-approved by name, source, size and licence first), running through the existing one process starter and writing only through the one writer, with a bound per graphic.
3. **Motion where the shot list asks for it** (e.g. a run drawn over time), timed inside its beat's measured span from the narration actually used; still otherwise.
4. **Determinism and provenance**: each graphic's frames hashed; the same specification renders the same bytes; the specification, the renderer and its version recorded with every artifact.
5. **A visual check the owner can review** before any production: a contact sheet or a short preview of all 22 graphics, from the demonstration store.
6. **Assembly unchanged in shape**: one video, decoded end to end with zero errors, runtime measured from the file, held short of publish-ready; the 18 clip positions stay labelled placeholders.
7. **Carried from Wave 10**: the known issues in `wave-10/release-note.md` routed to the implementer or tech lead that touch the Design stage, the produce command or the render.
8. **Owner documentation**: how to preview the graphics, how to re-produce item 001, what changed on screen.

### NOT IN THIS WAVE

Any metered service or model call (image generation included); stock footage, music or any subscription; publication, upload, channel creation; changes to narration sources; a second item or a second channel.

### BINDING CONSTRAINTS CARRIED FORWARD

1. **Nothing publishes.** The structural absences on the upload path stay; the held-stage refusal stays.
2. Budget USD 77.41 per month; company metered ceiling USD 34.42; the item cap of USD 5.95 keeps its counted total of USD 0.007695 (estimate).
3. **Code first, AI when necessary.** The graphics are code; no AI component is added.
4. Owner approval remains a transition-table state; D-002 is not relaxed.
5. One legal entity, several channels; no second channel.
6. No threshold, target, scale, colour rule, number or relationship on screen may be invented; every on-screen fact is quoted from a recorded claim.
7. Demonstrations and the live test suite use the demonstration store only. The company store `mediacompany` holds real records (item 001 versions 2 and 3); any run against it is the orchestrator's, on the owner's go, after a backup.
8. Downloading a library, font or tool is an owner-approved action, named with its source, size and licence before it happens.
9. Niche-agnostic and re-pointable: the renderer draws from specifications, not from code specific to bees.

### FRAMEWORK DEFECTS TO BRIEF EVERY SUBAGENT ON

All in `research/framework-defects.md` (twelve). Give every agent its own staging folder, never brief a non-implementing role to write a repository file, and require the list whenever an agent claims a fix covers "every" path.

### PRICING BASIS

Metered spend for item 001 in this wave: none. Local compute is real and unmeasured; record it as unmeasured, never as zero. Development cost and per-item cost are separate quantities and must not be netted.

## Acceptance criteria

_Not provided in the ticket. To be defined and approved at the scope gate before implementation starts._
