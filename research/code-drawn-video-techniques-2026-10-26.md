# Code-Drawn Video Techniques: What the Opus 5.5 Showcase Teaches the Studio

**Compiled:** 2026-10-26, on the CEO's request ("the competition is far ahead; learn from it and add it to the studio now").
**Source:** `github.com/yihui-dev/awesome-opus5-5-videos` at `92aee2f` (2026-10-13), MIT licence for the compilation.
703 entries, 496 with a complete prompt; categories motion 369, 3d 117, explainer 116, interactive 101; tags canvas 267,
svg 183, threejs 131, shader 92, gsap 86. Remotion is named in 96 prompts.
**Status:** research input, not a decision. Nothing here changes Wave 12 (MC-13) until the CEO rules on §5.

Labels follow `MASTER-PLAN.md`: **[OBSERVED]** is read directly from the prompts; **[INFERENCE]** is reasoning from them.

---

## 1. What these videos are

Every video in the collection is animation written as code by the model (HTML, Canvas, SVG, Three.js), rendered frame by
frame in a headless browser and encoded with ffmpeg **[OBSERVED]**. That is the studio's own method since Wave 11
(Remotion, headless Chrome, deterministic frames, `CEO-D-1004`). The gap is not the tool; it is **direction**: how a scene
is specified, how motion is timed, and how the render is checked before it is long **[INFERENCE]**.

## 2. The prompt shape the best entries share

The highest-quality prompts (the `twoclipping`, `verbove`, `rossaxbt`, `ultimaxbt` families) use the same five blocks
**[OBSERVED]**:

| Block | What it fixes | Studio equivalent today |
|---|---|---|
| `<inputs>` | The exact assets and facts asked for, with defaults if missing | Item dossier + recorded sources |
| `<direction>` | One concept line, palette as hex, one or two fonts, camera language, a **Banned** list | Style in the graphic specification (partial) |
| `<structure>` | A beat grid ("120 BPM, 7 bars, something happens on every beat") and the scene chain | Beat spans from measured narration (Wave 11) |
| `<build>` | Rendering rules: pure function of time, springs, motion blur, render order | `graphics/drawing/motion.js` (partial) |
| `<gotchas>` / export | Contact sheet first, probe frames, fix, then full render; all aspect ratios | Wave 12 contact sheet (partial) |

The **Banned** list is consistent across the strongest entries: bouncy easing, particle bursts, glows, lens flares,
camera shake, RGB split, crossfades, holds longer than 0.5–1 s, dead time, *"anything that looks like a template"*
**[OBSERVED]**. The last item matters to us beyond taste: templated, minimally transformed output is what the platform's
inauthentic-content policy penalises (`MASTER-PLAN.md` §1, finding 2) **[INFERENCE]**.

## 3. Techniques worth adopting, ranked by fit

Each is compatible with the binding constraints (no generative model, USD 0 metered, deterministic, nothing invented on
screen) unless the last column says otherwise.

| # | Technique [OBSERVED] | Why it raises quality | Fit with the studio |
|---|---|---|---|
| T1 | **Every value is a pure function of time** (`seek(t)` / `draw(t)`): no CSS transitions, no timers, no state carried between frames | Exact, repeatable frames | Already our rule (`motion.js`: "the frame index is the only time"). Keep. |
| T2 | **Closed-form springs**; a value that changes target several times is the **sum of one spring per change**, so it stays a pure function of time | Motion feels physical, not linear; still deterministic | `motion.js` uses one smoothstep then **holds**. Add a spring easing to the closed motion set. Low risk. |
| T3 | **One continuous take**: each scene grows out of the previous (a shape morphs, a colour floods, the camera carries into the next space); no cuts, no crossfades | The single biggest difference between "showreel" and "slideshow" look | Our 22 graphics are separate cards with a final-state hold. Needs scene-to-scene transitions in the specification. Medium. |
| T4 | **Something happens on every beat**; no hold longer than ~1 s | Retention; nothing static | Our beats are narration spans, not music. Use narration word timings as the grid. Medium. |
| T5 | **Hook in the first second**, word by word on the beat | Matches `CEO-D-1004`'s read of the competitor reels | Directly applies to the 9:16 shorts of Wave 12. Low. |
| T6 | **Loop: last frame equals first frame** | Shorts replay, which counts as watch time | Shorts only; trivial in a pure-function renderer. Low. |
| T7 | **Motion blur by sub-frame averaging** (3–6 sub-frames per frame at t−, t, t+, blended with ffmpeg `tmix`) | Removes the "computer" stutter of fast motion | Deterministic; costs 3–6× render time on a machine without a GPU. Measure first. |
| T8 | **Contact sheet of one frame per beat, then probe 20+ frames, fix clutter/overlap, then the full render** | Catches layout faults before hours of rendering | Wave 12 already has a contact sheet; make it per-beat and a gate before the long render. Low. |
| T9 | **Strict design system**: palette as fixed hex, one or two fonts with set weights and tracking, a safe border ("nothing at rest within 150 px of the sides") | Coherence; reads as a studio, not AI | Fits the owner-approved Wave 11 palette; add the safe border and type scale to the style data. Low. |
| T10 | **Simulate the real thing** in explainers ("the random walk should be an actual random walk"), real numbers with honest uncertainty, a little character, visual gags and callbacks | Educational and charming at once | Strong fit with our quotation rule: the simulation must use only recorded figures. The character is Wave 12 item 2. |
| T11 | **Burned-in captions** "since most people watch with the sound off" | Shorts are watched muted | Captions from the narration actually used are already Wave 12 item 4. |
| T12 | **Sound effects placed by measured peak, not file start**; mix loudness-normalised (e.g. -14 LUFS) | Hits feel synced | Needs free SFX/music: an owner-approved download each (constraint 8). Defer unless the CEO wants it. |
| T13 | **All aspect ratios rendered from one scene description** (1:1, 16:9, 9:16) | One build, every platform | Is Wave 12's design (one scene set, long + shorts). Keep. |

## 4. What not to copy

1. **Music-driven beat grids from stock songs** (Mixkit and similar): needs a licensed download per song; the narration is
   our clock **[INFERENCE]**.
2. **AI images, AI voice, AI music** used by some entries (6 tagged `ai-image`; the photon explainer asks for "the
   highest-quality text-to-speech model" and composed music): barred by `CEO-D-903` / `CEO-D-1004` and the unchanged
   narration path.
3. **Product-UI showreels** (cursor, buttons, Dynamic Island, album covers): the dominant genre in the collection, but not
   our content. Borrow the motion grammar, not the subject.
4. **The prompts themselves, verbatim.** The compilation is MIT, but each prompt is the work of its named author on X, and
   several are near-identical copies of one another (`twoclipping-402193`, `morse-369333`, `demonugc-525162`). The studio
   writes its own direction in its own words; this document paraphrases and quotes only short phrases.
5. **"Work autonomously, don't ask me"** production: our D-002 owner approval per publication stands.

## 5. Proposal for the CEO

Two routes; the CEO chooses.

**A. Fold the low-risk part into Wave 12 now (recommended).** T2 springs, T5 hook, T6 loop, T8 per-beat contact sheet as a
gate, T9 safe border and type scale, T11 already planned. These are additions to the motion set and the shorts
specification, inside MC-13's scope (3D scenes + shorts). Cost: a design addendum and one more implementation continuation
before the paused re-review; it delays MC-13 closure.

**B. Open Wave 13 for "studio direction".** T3 continuous take, T4 narration beat grid, T7 motion blur (measured on this
machine first), T10 simulation scenes, plus a company **scene-direction template** (inputs / direction with Banned list /
structure / build / checks) that every item's scene specification is written against. Routed as `/implement`, after MC-13
merges.

Recommended: **A for the shorts-facing items, B for the rest**, so Wave 12 ships shorts that hook and loop, and the deeper
change to how every scene is directed gets its own scope gate.
