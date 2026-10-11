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
item's four recorded sources (the shot list, the narration, the claim record and the owner's
decision record, found from the item package and never named by a specification), each on whole
words and whole figures (a figure is never cut out of a longer one); "schematic, not to scale"
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
identities are printed as read from their verified bytes, with the style file's hash, the drawing
program's digest and the render script's hash the approval file records. Exit **2**: refused, naming
every finding (a changed file with both hashes, an unlisted file, a `node_modules` folder in any
parent folder of the installed renderer folder, a manifest that differs from its expected hash, a
specification fault, a character no face covers). It starts no process, writes no file and reads
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

- `install` creates the store's tables on a store that has none (exit **0**). On a store **already
  installed** it exits **2**, printing "the install is refused: this store already holds the store
  designation table ..."; then run instead

  ```powershell
  dotnet run --project src/MediaCompany.Host -- install --from 11
  ```

  which applies the eleventh resource alone and, where it is already applied, changes nothing (exit
  **0**), and continue with `prepare`.
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
orchestrator in the decision record and in an **approval file** under the company run folder. The
file's own SHA-256 is recorded in the decision record and set in the settings'
`graphics.approvalFile` (`{ "path": "<the file>", "sha256": "<its SHA-256>" }`). A requested change
of colour or stroke changes `config/graphics-style.json`; a requested change of a graphic changes its
specification; either is re-rendered and reviewed again, and the approval file is written anew.

The approval file is one JSON object in exactly this shape; the company plan refuses any other
(exit **2**, "is not an approval file of the recorded shape", naming the fault):

| Member | Required | Value |
|---|---|---|
| `date` | yes | the approval's date, as text |
| `approver` | yes | who approved, as text |
| `styleSha256` | yes | the style file's SHA-256, as `verify-graphics` prints it |
| `drawingProgramDigest` | yes | the drawing program's digest, as `verify-graphics` prints it |
| `renderScriptSha256` | yes | the render script's SHA-256, as `verify-graphics` prints it |
| `graphics` | yes | one entry per graphic, below; each position once |
| `decisionRecordEntry`, `demonstrationStore`, `demonstrationVersion`, `contactSheetSha256`, `previewSha256`, `manifests` | no | recorded for the reader; the plan does not compare them |

Each entry of `graphics` holds exactly `position` (`GFX-01` to `GFX-22`), `specificationSha256`,
`kind` (`still` or `motion`, lower case), `finalFrameSha256` (the graphic's final frame as
`review-graphics` prints it) and `stillSha256`: for a still, equal to its `finalFrameSha256` (a
still is its final frame); for a motion graphic, `null` or absent. Every SHA-256 is 64 lower-case
hexadecimal characters. Any other member is refused by name.

The company plan (`produce --mode plan-only` and `produce --mode own` in the company store) compares,
**before any render**, the style file, the drawing program, the render script and every
specification with the approved hashes, and each graphic's kind with the approved kind; **after
rendering**, every graphic's final frame (a still, and a motion graphic's end state) must equal its
approved `finalFrameSha256`, or the Design stage fails naming both hashes.

The complete file for the graphics the owner approved on 2026-10-11 (`CEO-D-1003`), with the hashes
of this repository's files as `verify-graphics` prints them and the final frames the decision record
lists (a change to any of these files changes its hash, and the file is written anew):

```json
{
  "date": "2026-10-11",
  "approver": "the CEO",
  "decisionRecordEntry": "CEO-D-1003",
  "demonstrationStore": "mediacompany_demo",
  "demonstrationVersion": 2,
  "contactSheetSha256": "51ce3f5b935201b669fb9bbd71f07fab7a4d0dc25c52cb77c7bb3c79703d0f28",
  "previewSha256": "e25532133f1a669170e74f32bc142eb734cc183a5ebf2d0ba9d451af927b66c6",
  "styleSha256": "bab8e25ac8f7100e3f47812fd35a603819b222ed51a3a6738458d73a4426403a",
  "drawingProgramDigest": "f2ee2c44ebbb3c39a4d4814e4c52f806d19b9c2e0a40620c1d3b04c44dff01ec",
  "renderScriptSha256": "8747449891f2554f67cac8eb0cb31f9d819bc01cdcc7b20887ad0cc15a45cba6",
  "graphics": [
    {
      "position": "GFX-01",
      "specificationSha256": "443ffd98a6480c20a74d4f5f690b9b12656224e86cb7d46c60b164c44ec9fb1f",
      "kind": "still",
      "finalFrameSha256": "92d9b1eeeba32e5d52293c39fa4fd6faa28884904fd794fd970f2613e5000ecb",
      "stillSha256": "92d9b1eeeba32e5d52293c39fa4fd6faa28884904fd794fd970f2613e5000ecb"
    },
    {
      "position": "GFX-02",
      "specificationSha256": "49413280357b66429208e43a7e76a3ee2b4d6243b8b971b97319078a97b8b0b3",
      "kind": "motion",
      "finalFrameSha256": "db50637296ec19baf93ebdb2b7bee76c6341b7f19703170c4aaaf7b629d84fb5",
      "stillSha256": null
    },
    {
      "position": "GFX-03",
      "specificationSha256": "745bf230bbee589a4348413e1cfed2d4fd6d99df9e533af47561c45f42d20b72",
      "kind": "still",
      "finalFrameSha256": "30d19ce25f6bf733e74805b7f2376f4d042ce1d0f3157014789ce1313b316ffa",
      "stillSha256": "30d19ce25f6bf733e74805b7f2376f4d042ce1d0f3157014789ce1313b316ffa"
    },
    {
      "position": "GFX-04",
      "specificationSha256": "167e0aa58d79dfe6e124c8037be19456ebd337d3d3f4b64a23a481dc7ab93d9b",
      "kind": "still",
      "finalFrameSha256": "3aa594f360e8c8e845c0c18f26d63a17f0f1573834ef359a6710c78bd35db092",
      "stillSha256": "3aa594f360e8c8e845c0c18f26d63a17f0f1573834ef359a6710c78bd35db092"
    },
    {
      "position": "GFX-05",
      "specificationSha256": "957d232aea94e7d8fa34dda992d9aea88b9ee9f83feb6510ed6d90ffaa9b508d",
      "kind": "motion",
      "finalFrameSha256": "02cd68b54e9b3085c3c4b519b9b377f82a37ba86c395c3810a6138b5291a84fa",
      "stillSha256": null
    },
    {
      "position": "GFX-06",
      "specificationSha256": "8ba86fcca1526fd7f4a7fbcc36de805feca6b7fb132b7405f13c9544b5153298",
      "kind": "motion",
      "finalFrameSha256": "4b03dbf65ae686c5a7f24c287c67768bde24cb515a04e830502bd20d03504316",
      "stillSha256": null
    },
    {
      "position": "GFX-07",
      "specificationSha256": "d69fe00e5da44b2154720b053c2d18ab7821924a0e820358b099b70ac0b4a1b9",
      "kind": "motion",
      "finalFrameSha256": "cbf7e0f3cd9e702427fc12d7a5873e71d2ab220bfed56c017aa5586c8b118d23",
      "stillSha256": null
    },
    {
      "position": "GFX-08",
      "specificationSha256": "edcd4a4939c91f161f69ae5197b01636d4de8f06b025f5a4c5a468db77cdada1",
      "kind": "motion",
      "finalFrameSha256": "8261a07c036cd20e8dfeea283ec751309b889b2d58e4d3c35ece67e176bbb368",
      "stillSha256": null
    },
    {
      "position": "GFX-09",
      "specificationSha256": "61a08b48d36bb98d07859c19ee18ebabc040392fd043b37a244c5e1262287e3a",
      "kind": "motion",
      "finalFrameSha256": "03f947f20126020681c330e5318447891e9a531de7558899017705b4945ec253",
      "stillSha256": null
    },
    {
      "position": "GFX-10",
      "specificationSha256": "adf792ba2cd20f8e828ff76e37a38de590500a37f46504dff057acf169228c91",
      "kind": "still",
      "finalFrameSha256": "e7fbe762d35b9e40f7277147de698e66e3cf09297b83cf343c0863d5661eda23",
      "stillSha256": "e7fbe762d35b9e40f7277147de698e66e3cf09297b83cf343c0863d5661eda23"
    },
    {
      "position": "GFX-11",
      "specificationSha256": "71e6e41803cd40ebc08c69f1661c3a6ca2b27b68ce40ca4a96c66f0565332a7f",
      "kind": "motion",
      "finalFrameSha256": "1f109e162079bd8aaa3f1113feb9bb152f1e2d904db9d574e59106508ed4fc43",
      "stillSha256": null
    },
    {
      "position": "GFX-12",
      "specificationSha256": "0151faaeea9d8d83108fc4bdfb78c77fa59a189254d91b7c8b281d765366ae43",
      "kind": "motion",
      "finalFrameSha256": "8f44cad0d0b38eaaf5f26bef9b1990312804a5b8651e3f9e1aa44a93ace86b49",
      "stillSha256": null
    },
    {
      "position": "GFX-13",
      "specificationSha256": "2271420caaf924fd16ae32ffc51763beaef250058aed18970c732a937d416a15",
      "kind": "still",
      "finalFrameSha256": "6fdaf44c061c3ce4fb21d8b2abdbcddede11d0d2449ce893912be8d596d7d569",
      "stillSha256": "6fdaf44c061c3ce4fb21d8b2abdbcddede11d0d2449ce893912be8d596d7d569"
    },
    {
      "position": "GFX-14",
      "specificationSha256": "7734faeadb80853c11f5d77a6838d2851615e9253ae15cb5cc47010725be8b70",
      "kind": "motion",
      "finalFrameSha256": "02a0752863fb512451d831a477829d59f225940cdc38855bb85c29a95d1209ef",
      "stillSha256": null
    },
    {
      "position": "GFX-15",
      "specificationSha256": "d62dda35027331ccb3ef69a850091d1b951a6b12110ee59b04fd3665544c97e6",
      "kind": "still",
      "finalFrameSha256": "2566b916c141de5fc7b9a5e84316f32a240d859bad6558923b2c2f5fd41607b4",
      "stillSha256": "2566b916c141de5fc7b9a5e84316f32a240d859bad6558923b2c2f5fd41607b4"
    },
    {
      "position": "GFX-16",
      "specificationSha256": "03fe8206855592854766a353f63dc627c137da807023b149ee7581b18a6d7ab3",
      "kind": "motion",
      "finalFrameSha256": "d3b57bf876f9f7420af30841f566f2200487f72dd87f1dd76d99a2f1f4b41cae",
      "stillSha256": null
    },
    {
      "position": "GFX-17",
      "specificationSha256": "6b2665f40cecea2984ba88af12e0c98fb86dbb950fc3b819f5f3566a314f4dd0",
      "kind": "still",
      "finalFrameSha256": "098a95bed87486d2ece61cb65ac635191d8b1030b6b907d8526a1bfc31223bab",
      "stillSha256": "098a95bed87486d2ece61cb65ac635191d8b1030b6b907d8526a1bfc31223bab"
    },
    {
      "position": "GFX-18",
      "specificationSha256": "98e8b44018c6fe62738731f11963ac88667b3fd4c1c9620cc4f90e2a675a5dce",
      "kind": "motion",
      "finalFrameSha256": "3c34ff575e0317655d5d1ec3897c3787e52ff605b8ce1e90dfabaa5e26a01281",
      "stillSha256": null
    },
    {
      "position": "GFX-19",
      "specificationSha256": "0ed45d32edd43fa8c48bbeebe0b9cb294c3d3b3174180de85f11de78019900d5",
      "kind": "still",
      "finalFrameSha256": "34ebe77b92be0933df56428ec6fa54a15d1ffe3b3ba5774471348e58429119a6",
      "stillSha256": "34ebe77b92be0933df56428ec6fa54a15d1ffe3b3ba5774471348e58429119a6"
    },
    {
      "position": "GFX-20",
      "specificationSha256": "4c5f826b8c4c25d455f80a410610a29b585293a38d37747d51c9a83c075f84c4",
      "kind": "still",
      "finalFrameSha256": "9b06940efc7fbb65c4bfb53e6081f523a5c3933d40d4f38431a8ae62aa843357",
      "stillSha256": "9b06940efc7fbb65c4bfb53e6081f523a5c3933d40d4f38431a8ae62aa843357"
    },
    {
      "position": "GFX-21",
      "specificationSha256": "715b03f5385d93bf73a119c728dc94abdce8e6031b84c7c2a6721053c3b8f960",
      "kind": "motion",
      "finalFrameSha256": "3ee1a8f7ffc380c5949b8abd641dc41c6add0fc56199fb43de016068c74d04d0",
      "stillSha256": null
    },
    {
      "position": "GFX-22",
      "specificationSha256": "ed72d07fd46d7d22895e9e572a53c23302098907fb8094aab6e3f05b849a3c5c",
      "kind": "still",
      "finalFrameSha256": "d56fb9a1e7b72aee286bb865ae12907557157bc1e59df18fa3507f2fd74f8122",
      "stillSha256": "d56fb9a1e7b72aee286bb865ae12907557157bc1e59df18fa3507f2fd74f8122"
    }
  ]
}
```

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
  it starts no process and writes nothing. It refuses (exit **2**) without the approval file, where
  the file is not of the shape in section 4, or where the file's hash, the style file's, the drawing
  program's, the render script's or a specification's hash, or a graphic's kind, differs from what
  was approved.
- `produce --mode own` draws each graphic **once**: every still must equal its approved bytes, and
  every motion graphic, **re-timed** from this run's narration (the company's own, whose beat
  durations differ from the preview's), must end on its approved final frame; a difference fails the
  Design stage naming both hashes, exit **3**. The frames of a motion graphic before its final state
  are not compared. Exit codes as in section 3.

## 6. What a graphics run will and will not do

- It books no operation: booked metered spend USD 0.00; local compute is recorded as unmeasured.
- It calls no model of any kind and no capability; it passes no licence key, so the renderer sends
  no usage event; it never downloads a browser; nothing is published.
- It writes only under the output root: every temporary, profile and log file of the renderer and
  the browser is in the run's staging folder; nothing is written under the installed folders or in
  the repository, and the installation is verified again after the last render, so a file left
  there fails the run by name.
- It loads only verified files: every module the render loads is a file of the installed renderer
  folder (or the staged drawing program), and a module resolving anywhere else, such as a
  `node_modules` folder in a parent folder, refuses the run by name.
- Staging holds about a gigabyte of frames during a demonstration run; it is cleared at the next
  run's start.
