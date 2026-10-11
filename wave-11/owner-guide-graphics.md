# Owner guide — graphics drawn by code

This guide is for the owner. It says how the 22 graphics of item 001 are now drawn, how to verify the
installed renderer, how to preview the graphics (the contact sheet and the preview video), how the
graphics are approved, how item 001 is re-produced in the company store afterwards, what changed on
screen and what did not, with the exact commands and their exit codes. **It holds no secret.** No
graphics command reads a credential, calls a model, calls a vendor or reaches a network; every
graphic is drawn on this machine by the renderer the owner approved on 2026-10-11 (Remotion 4.0.534
under its free licence, used with no licence key; Chrome Headless Shell 157.0.8080.0; Inter 4.1).

Every command below is run from the repository's root on this machine, with the connection string
of the store it acts on set in the environment (it names a database, never a vendor secret):

```powershell
$env:MEDIACOMPANY_CONNECTION_STRING = "Host=localhost;Port=55432;Database=<mediacompany_demo or mediacompany>;Username=postgres;Password=mediacompany-dev"
dotnet run --project src/MediaCompany.Host -- <command> <options>
```

`<settings.json>` is a copy of `config/production-settings.sample.json` kept **outside the
repository** with every `<...>` value set. Its `graphics` section names the four manifests of the
installed graphics folder, each with the SHA-256 the owner's decision record states for it, Node,
the browser, the two faces, and the bounds ruled at the Design Gate (installation hashing 300 s,
bundling 120 s, each render 120 s plus 0.5 s a frame, per-frame readiness 30 s, concurrency 4).
**Check each expected hash against the decision record** (`research/ceo-decision-record.md`): the
run's settings file is the trust anchor, not the repository sample.

---

## 1. What changed on screen

Each graphic position used to show a text card (white text on a dark colour, drawn by the media
tool). It now shows a graphic drawn from its specification (`wave-2/item-001/graphics/gfx-NN.json`)
on a light paper background (#F5F1E8) with dark line work (#1F2328), a secondary grey (#5F646B), one
accent colour (#A8481A) for the element each graphic focuses on, and a light shade (#DEDCD5) for
regions. These values are proposed for the owner's approval at the contact-sheet review; changing
them changes the style file only, never code. Every word and number on screen is quoted from the
shot list, the narration, the claim record or the owner's decision record; "schematic, not to scale"
marks every graphic whose geometry is schematic. Twelve graphics move; ten are still.

| Position | Kind | What is now on screen |
|---|---|---|
| GFX-01 | still | *Apis mellifera* in italic, "the western honey bee"; plan view: nest and resource joined by a dashed line, "~2 km", an obstacle region, "no line of sight", "no shared landmark" |
| GFX-02 | motion | the straight waggle run drawn first, then the two return loops in turn, each labelled |
| GFX-03 | still | an oscillation trace labelled "13–15 Hz" over a time axis with no scale |
| GFX-04 | still | the run at an angle from a dashed vertical, "angle θ"; a bracket "duration t", "how long it lasts" |
| GFX-05 | motion | outdoor frame, plan view: the sun's azimuth, then the resource bearing, then the angle θ between them |
| GFX-06 | motion | comb frame, elevation view: the vertical and "gravity", then the run rotating out to θ |
| GFX-07 | motion | both frames side by side in turn, the same θ in each, then "θ preserved across them" |
| GFX-08 | motion | three worked cases in turn: straight up ("fly toward the sun"), straight down ("fly directly away from it"), "60°" right of vertical |
| GFX-09 | motion | the sun's azimuth rotating through the day beside the dance angle rotating to track it, "nine o'clock" then "noon" |
| GFX-10 | still | waggle duration against distance: a near-linear line inside a band for the subspecies slope, no conversion figure |
| GFX-11 | motion | a marker flying along a dashed flight path over ground marks while a bar of accumulated image motion grows |
| GFX-12 | motion | a marker in a narrow patterned tunnel, then two bars: "the few metres they had actually travelled" and "danced for a distance far greater" |
| GFX-13 | still | "the metre" beside "accumulated image motion", an arrow between them, "Unit substitution" |
| GFX-14 | motion | the vector growing from the nest at θ, then "t", then the arrival region: "a region, not a point" |
| GFX-15 | still | a comb with "a small motorised device" performing a run, a dashed path to "the specified location" |
| GFX-16 | motion | the outbound vector drawn first, then the search pattern |
| GFX-17 | still | a shaded spread of run angles around "the true bearing", no points |
| GFX-18 | motion | a near cone with a wide angle and its region, then a far cone with a narrower angle and a wider region |
| GFX-19 | still | two panels: "a constraint, not a design" (a spread on a point) and "a patch of flowers is not a point" (a spread on a patch); "contested and unresolved in the literature" |
| GFX-20 | still | two panels: patchy resources, "substantial"; dense evenly spread resources, "about as well without it" |
| GFX-21 | motion | five boxes appearing in turn: measure, encode, transmit, decode, fly |
| GFX-22 | still | the end card: the real citations of the claim record, readable |

**Unchanged:** the 18 clip positions still show their labelled placeholders (stock footage remains
the owner's later decision), the thumbnails are drawn as before, the narration path is unchanged,
and nothing is published.

## 2. Verify the installed renderer (reads files only)

```powershell
dotnet run --project src/MediaCompany.Host -- verify-graphics --settings <settings.json>
```

Exit **0**: every installed file of the renderer, the browser, the faces and Node hashes to its
manifest, no unlisted file is present where a render loads from, every specification passes its
check and every drawn character is in its face; the renderer's, browser's, Node's and faces'
identities are printed as read from their verified bytes. Exit **2**: refused, naming every finding
(a changed file with both hashes, an unlisted file, a manifest that differs from its expected hash,
a specification fault, a character no face covers). It starts no process, writes no file and reads
no datastore. It takes a few minutes: it hashes about 15,000 installed files.

## 3. Preview the graphics (demonstration store only)

The preview is made in the **demonstration** store, never the company store.

```powershell
$env:MEDIACOMPANY_CONNECTION_STRING = "Host=localhost;Port=55432;Database=mediacompany_demo;Username=postgres;Password=mediacompany-dev"
dotnet run --project src/MediaCompany.Host -- install
dotnet run --project src/MediaCompany.Host -- prepare --settings <settings.json> --designation demonstration
dotnet run --project src/MediaCompany.Host -- produce --mode fake --settings <settings.json>
dotnet run --project src/MediaCompany.Host -- review-graphics --settings <settings.json> --version <the version produce printed>
```

- `produce` prints the plan (renderer, browser, Node and faces with their hashes, the licence as
  configured with its source, USD 0.00 planned), narrates, measures the narration, then draws every
  graphic **twice** in separate processes and compares the two renders byte for byte, printing each
  graphic's render time as observed on this machine, then assembles the video as before. Exit **0**
  produced; **2** refused before any call; **3** a stage failed, named; **6** cancelled.
- `review-graphics` builds, under the output root's `<item>/v<version>/review/` folder:
  `contact-sheet.png` (all 22 graphics in four columns, each labelled with its position and its
  shot-list row, scaled from its full-size final frame), `frames/gfx-NN-final.png` (each graphic's
  full-size final frame, a byte-for-byte copy) and `graphics-preview.mp4` (the 22 graphics at their
  production spans, video only). It prints every hash the owner's approval needs. Exit **0**
  reviewed; **2** refused before any process (including any store not designated demonstration, or
  a version already reviewed); **3** a step failed, named.

## 4. Approve the graphics

Look at the contact sheet and the preview. Your approval (or a requested change) is recorded by the
orchestrator in the decision record and in an **approval file** under the company run folder,
holding the date, the approver, the decision record entry, the demonstration store and version, the
sheet's, preview's and style file's hashes, the four manifest hashes and, for each of the 22
graphics, its specification hash, its kind, its final-frame hash and, for a still, its still hash.
The file's own SHA-256 is recorded in the decision record and set in the settings' `graphics.approvalFile`.
A requested change of colour or stroke changes `config/graphics-style.json`; a requested change of
a graphic changes its specification; either is re-rendered and reviewed again.

## 5. Re-produce item 001 in the company store (after the approval and a fresh backup)

Preconditions, in this order: **(1)** the owner's contact-sheet approval is recorded and the approval
file is written with its hash in the decision record; **(2)** a **fresh backup** of the company store
is taken. Then:

```powershell
$env:MEDIACOMPANY_CONNECTION_STRING = "Host=localhost;Port=55432;Database=mediacompany;Username=postgres;Password=mediacompany-dev"
dotnet run --project src/MediaCompany.Host -- install --from 11
dotnet run --project src/MediaCompany.Host -- verify-graphics --settings <settings.json>
dotnet run --project src/MediaCompany.Host -- produce --mode plan-only --settings <settings.json>
dotnet run --project src/MediaCompany.Host -- produce --mode own --settings <settings.json>
```

- `install --from 11` applies the eleventh resource alone (the graphics roles and the graphic
  provenance); it changes no delivered row and re-applies changing nothing. Exit **0**; **2** refused.
- `produce --mode plan-only` prints the plan with the graphics section and the approval's status;
  it starts no process and writes nothing. It refuses (exit **2**) without the approval file, or
  where the file's hash, a specification's hash or the style file's hash differs from what was
  approved.
- `produce --mode own` draws each graphic **once**: every still must equal its approved bytes (a
  difference fails the Design stage naming both hashes, exit **3**); every motion graphic is
  **re-timed** from this run's narration and recorded as re-timed and not compared, since the
  narration is the company's own and its beat durations differ from the preview's. Exit codes as in
  section 3.

## 6. What a graphics run will and will not do

- It books no operation: booked metered spend USD 0.00; local compute is recorded as unmeasured.
- It calls no model of any kind and no capability; it passes no licence key, so the renderer sends
  no usage event; it never downloads a browser; nothing is published.
- It writes only under the output root: every temporary, profile and log file of the renderer and
  the browser is in the run's staging folder; nothing is written under the installed folders or in
  the repository, and the installation is verified again after the last render, so a file left
  there fails the run by name.
- Staging holds about a gigabyte of frames during a demonstration run; it is cleared at the next
  run's start.
