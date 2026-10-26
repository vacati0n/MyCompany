# MC-16: Production toolchain for the Shopee Video affiliate pilot - one product to one ready-to-review vertical video

- Source: local://research/ceo-decision-record.md#ceo-d-1301 to #ceo-d-1305
- Issue type: Feature   Priority: Highest
- Labels: affiliate, shopee-video, pilot, toolchain, vertical-video, vietnamese
- Routed as: /implement (implement-feature)

## Description

### WHY

The CEO approved the MC-15 recommendation (`CEO-D-1301`, run `run-8d7e366341bf`) and its addendum (`CEO-D-1302`): a
Shopee Affiliate pilot as an individual KOC on Shopee Video, home and kitchen niche, real product images, an AI
presenter labelled "Video tạo bởi AI" that introduces products from sourced facts and never claims use. `CEO-D-1303`
rules that sellers consented to product-image use. `CEO-D-1304` makes the presenter lip-sync to the CEO's own recorded
Vietnamese voice (no text-to-speech). `CEO-D-1305` confirms no voice cloning: each video uses a fresh real recording.

The pilot kit (`pilot-affiliate-vn/pilot-kit/00-07`) is paper: a script template, an approval checklist, a
measurement sheet. Today the CEO would assemble every video by hand. MC-16 builds the tooling that turns one product
into one ready-to-review 9:16 video, reusing the delivered deterministic pipeline on `main`.

### INPUTS, PER VIDEO (all supplied by the CEO)

1. A **product fact sheet**: Part A of `pilot-kit/03-mau-kich-ban-nguoi-dan-ai.md` in a machine-readable form; every
   fact carries its source (which section of the product page, the CEO's screenshot file).
2. The **product images** the CEO supplies (files on disk).
3. The **CEO's own Vietnamese voice recording** of the approved script (MP3 or WAV, under 5 minutes, fresh for this
   video).
4. The **AI-presenter clip** the CEO generates in the presenter tool (HeyGen or D-ID) from that recording.

### LIKELY SCOPE (the scope phase bounds it, with acceptance criteria)

1. **Product fact sheet format and check.** Facts with sources only. A fact or field carrying content the template
   bans (Part A "Không đưa vào bảng" and Part C "Không bao giờ nói": price, discount, sold count, stars; absolutes
   such as "nhất", "duy nhất", "tốt nhất", "số một"; health, skin, hair, body, food-safety, antibacterial, weight-loss;
   use or review wording; other platforms' names) is rejected, naming the row and the banned term.
2. **Script generator.** Fills Part B of the template in Vietnamese from the fact sheet, deterministically (no
   language model). Disclosure sentence at the start and at the end, verbatim from the template. Never claims use; no
   health, skin or body claims. Emits Part E (the source line for each sentence) for the CEO to tick. The CEO then reads
   the approved script aloud.
3. **9:16 composition.** Product images with motion, the presenter clip, burned-in Vietnamese captions from the script
   actually recorded, and the disclosure overlay "Video tạo bởi AI · Có liên kết tiếp thị" on screen for the whole
   video (R11). **First, the RK-016 check:** that the delivered renderer does vertical output and that the installed
   face covers every Vietnamese glyph the captions and overlay draw (`FontCoverage`). The research left this check to
   the architect.
4. **Output package.** The video file with its SHA-256, the provenance list (every on-screen and spoken sentence to
   its fact row and source), the input digests, and the per-publication checklist of `pilot-kit/04` pre-filled where
   the tool can decide an item, for the CEO's approval by hash under `D-002`. Nothing publishes.

### WHAT THE COMPANY ALREADY HOLDS — REUSE IT

- The deterministic pipeline on `main` (Waves 1-11): Remotion and a headless Chrome driven from .NET under `src/`
  (`MediaCompany.Production`, `ExternalMediaTool*`, `RendererInstallationVerifier`, `GraphicsPlan`), the drawing
  program under `graphics/`, `FontCoverage` in `src/MediaCompany.Deterministic/Production/FontCoverage.cs`, the
  provenance of every on-screen fact, owner approval by hash, the held-stage refusal.
- Installed on this machine, verified by manifest, approved in Waves 10-11 (`CEO-D-1001`):
  `C:/Users/vuhoangcao/MediaCompanyRun/graphics/` (Remotion `node_modules`, `chrome-headless-shell-win64`, Inter 4.1
  with manifests), ffmpeg and ffprobe through WinGet, Node 24, .NET 10.

### HARD CONSTRAINTS

1. Agents never create accounts, sign in (Shopee, HeyGen, D-ID or any other), contact sellers, buy anything or publish
   anything.
2. No download of any asset, package or tool without the CEO's explicit approval, each one named.
3. The only generative element in a video is the presenter clip, which the CEO makes. The toolchain itself calls no
   generative model (no language model, no speech recognition model, no image model), so `CEO-D-903` and `CEO-D-1004`
   still hold for the studio and frozen Waves 12/13 keep their rules.
4. No text-to-speech anywhere (`CEO-D-1304`). No voice cloning; a recording already used for another video is refused
   (`CEO-D-1305`).
5. Budget USD 77.41/month, metered ceiling USD 34.42; the toolchain costs nothing to run (no metered call).
6. Frozen Wave 12 (MC-13) and Wave 13 (MC-14) are not touched. The company store (`mediacompany` database) is not
   touched.
7. Every publication is approved by the CEO individually (`D-002`); the CEO carries the legal position (`D-018`).

### DONE MEANS

- MC-16 runs through `implement-feature` with its gates; the CEO decides where a gate needs the owner.
- The tooling works and is tested on a sample made only from fixtures already in the repository or generated by
  repository code (no download).
- Committed and pushed on the session branch; the CEO gets the compare link.

### FRAMEWORK DEFECTS TO BRIEF EVERY SUBAGENT ON

All in `research/framework-defects.md`. Every agent gets its own staging folder; a non-implementing role writes no
repository file; an agent claiming a finding covers "every" case lists the cases.

## Acceptance criteria

_To be defined by the scope phase and approved at the Scope Gate._
