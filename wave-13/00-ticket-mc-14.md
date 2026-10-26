# MC-14: Wave 13: studio direction - every scene directed against one company template, moving in one continuous take on the narration's beat, checked frame by frame before the long render

- Source: local://research/code-drawn-video-techniques-2026-10-26.md
- Issue type: Story   Priority: High
- Labels: implement, wave-13, studio-direction, motion-graphics, 3d-animation, shorts
- Routed as: /implement (feature-request)
- Starts: **only after MC-13 (Wave 12) is merged** (`CEO-D-1200`)

## Description

WAVE 13 — STUDIO DIRECTION: THE MOTION GRAMMAR OF THE BEST CODE-DRAWN VIDEOS, IN THE COMPANY'S OWN TEMPLATE, AT NO COST

Waves 0 to 12 are Completed when this wave starts. Wave 12 (MC-13) delivers 3D animation drawn by code for item 001, a long 16:9 video and short vertical cuts, with the owner's approval recorded by hashes. Wave 13 builds on that pipeline and must not re-create any of it. Confirm the version, schema resources and test counts at the Wave 12 head by counting; do not copy them from this ticket.

### WHY — THE OWNER'S DECISION OF 2026-10-26 (`CEO-D-1200`, at the end of `research/ceo-decision-record.md`)

The owner judged that competitors are moving much faster, after a study of 703 published prompts for videos drawn by code with Claude Opus 5.5 (`research/code-drawn-video-techniques-2026-10-26.md`). Those videos use the company's own method (animation written as code, rendered frame by frame); the gap is **direction**: how a scene is specified, how motion is timed, and how the render is checked before it is long. **The owner decided that Wave 12 is not touched and that every studio-direction technique of that study comes in this wave.**

### GOAL

Every scene of an item is written against **one company scene-direction template** and rendered with the techniques of the study's §3 (T1 to T13), so item 001 is produced again in both formats with motion that is continuous, beat-timed and physically eased, at **USD 0.00 metered spend, with no generative model of any kind**, deterministic where the renderer allows (checked by hash, or recorded honestly as not observed to repeat), every on-screen fact still quoted from a recorded source, and the narration path unchanged.

### WHAT TO BUILD

1. **The company scene-direction template**, written in the company's own words (never a prompt copied from the study's source): inputs (the recorded sources and assets the scene may use), direction (one concept line, palette by fixed values, type scale, camera language, safe border, a **Banned** list), structure (the beat grid and the scene chain), build rules, and the checks before the long render. Every item's scene specifications are validated against it as data. Niche-agnostic.
2. **Closed-form springs** in the closed motion set (T2): a value that changes target several times is the sum of one spring per change, so every value stays a pure function of the frame index (T1). The present smoothstep-and-hold remains selectable.
3. **One continuous take** (T3): a scene grows out of the previous one (shape morph, colour flood, camera carry); no cuts and no crossfades unless the specification names one. Transitions are specification data, not code specific to a subject.
4. **The narration's beat grid** (T4): beats taken from the measured word timings of the narration actually used; something moves on every beat; no hold longer than a limit the scope sets (the study's entries use 0.5 to 1 s).
5. **The hook and the loop for shorts** (T5, T6): the hook lands in the first second, word by word on the beat; a short's last frame equals its first frame, checked by hash.
6. **Motion blur by sub-frame averaging** (T7), deterministic: **measured first hand on this machine** (no discrete GPU) at 3 and at 6 sub-frames before the design rests on it; the scope chooses with the measured render times in hand.
7. **Simulation scenes** (T10): an explainer scene may run a real simulation of the subject (for example a real random walk) seeded and deterministic, driven **only by recorded figures**, with uncertainty shown where the source states it. A recurring character and visual gags may appear; nothing about the subject's facts is invented.
8. **Strict design system as data** (T9): palette, one or two fonts with set weights and tracking, safe border; the owner-approved Wave 11 palette is the starting point.
9. **Checks before the long render** (T8): a contact sheet of one frame per beat, then a probe of at least 20 frames checked for overlap, clutter and text outside the safe border; the long render is refused until both pass. Burned-in captions (T11) and all aspect ratios from one scene description (T13) are kept from Wave 12 and checked under the new grammar.
10. **Owner review before production**: contact sheets and previews of the redirected scenes and every short, rendered from the demonstration store; the owner's approval recorded with hashes (the Wave 11/12 approval file extended).
11. **The company-store run**: on the owner's go, after a fresh backup, apply any new schema resource alone, verify every installation, then produce item 001 (a new version) in both formats; one decoded video per output, runtime measured from the file, held short of publish-ready.
12. **Carried from Wave 12**: the known issues of the Wave 12 release note that touch rendering, `produce` or the review command; and, carried by the Wave 12 orchestrator, **the Wave 11 2D render path still listens on all network interfaces**.
13. **Owner documentation**: how to write a scene against the template, preview, approve and re-produce; what changed on screen.

### DEFERRED, NOT IN THIS WAVE UNLESS THE OWNER DECIDES AT THE SCOPE GATE

**Sound effects placed by measured peak and a loudness-normalised mix** (T12): they need free sound or music files, each an owner-approved download. The scope proposes it with named sources, sizes and licences; the owner rules.

### NOT IN THIS WAVE

Any generative model or metered service (image, video, 3D-model, music or voice generation included, free tiers included); stock music beat grids; product-UI showreel subjects; copying any third-party prompt verbatim; publication, upload or channel creation; changes to narration sources; a second item or a second channel; any change to Wave 12's delivered records.

### BINDING CONSTRAINTS CARRIED FORWARD

1. **Nothing publishes.** The structural absences on the upload path stay; the held-stage refusal stays.
2. Budget USD 77.41 per month; company metered ceiling USD 34.42; the item cap of USD 5.95 keeps its counted total (confirm at the Wave 12 head).
3. **Code first, AI when necessary** — and for visuals no AI component at all (`CEO-D-903`, `CEO-D-1004`).
4. Owner approval remains a transition-table state; D-002 is not relaxed.
5. One legal entity, several channels; no second channel.
6. No on-screen number, threshold, scale or relationship invented; every on-screen fact quoted from a recorded source under the Wave 11 rule, simulations included.
7. Demonstrations and the live test suite use the demonstration store only. The company store `mediacompany` holds real records; any run against it is the orchestrator's, on the owner's go, after a backup.
8. Downloading a library, tool, font or asset is an owner-approved action, named with its source, size and licence before it happens (CC0 preferred).
9. Niche-agnostic and re-pointable: scenes are drawn from specifications, not from code specific to bees.
10. Never modify the installed folders under `C:/Users/vuhoangcao/MediaCompanyRun/` from a phase; verify them against their manifests.
11. The template must not make output templated: the platform's inauthentic-content policy penalises templated, minimally transformed output (`MASTER-PLAN.md` §1, finding 2). The scope states how two items directed by the same template stay visibly distinct.

### FRAMEWORK DEFECTS TO BRIEF EVERY SUBAGENT ON

All in `research/framework-defects.md`. Give every agent its own staging folder, never brief a non-implementing role to write a repository file, and require the list whenever an agent claims a fix covers "every" path.

### PRICING BASIS

Metered spend for item 001 in this wave: none. Local compute is real and unmeasured (motion blur multiplies it); record it as measured or unmeasured, never as zero. Development cost and per-item cost are separate quantities and must not be netted.

## Acceptance criteria

_Not provided in the ticket. To be defined and approved at the scope gate before implementation starts._
