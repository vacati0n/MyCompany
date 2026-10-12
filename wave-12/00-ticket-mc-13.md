# MC-13: Wave 12: 3D animation drawn by code - item 001 made watchable as a long 16:9 video and short vertical cuts, produced in the company store at no cost

- Source: local://MASTER-PLAN.md#35-development-waves
- Issue type: Story   Priority: High
- Labels: implement, wave-12, 3d-animation, motion-graphics, production, shorts
- Routed as: /implement (feature-request)

## Description

WAVE 12 — 3D ANIMATION DRAWN BY CODE: ITEM 001 MADE WATCHABLE, AS A LONG 16:9 VIDEO AND SHORT VERTICAL CUTS, PRODUCED IN THE COMPANY STORE AT NO COST

Waves 0 to 11 are Completed (Wave 11: `run-9d83536ecd16`, the 22 graphics of item 001 drawn by code with Remotion 4.0.534, Chrome Headless Shell 157.0.8080.0 and Inter 4.1, installed outside the repository under `C:/Users/vuhoangcao/MediaCompanyRun/graphics/` with full hash manifests; every graphic deterministic and traced to its recorded source; the owner approved the 22 graphics with their final-frame hashes). Wave 12 builds on all of them and must not re-create any of them. At the Wave 11 head: version 1.8.0, eleven schema resources, 1076 tests pass live in the demonstration store, architecture suite 101 (confirm by counting). The company store holds item 001 versions 2 and 3 only; Wave 11 ran no company-store production and the eleventh schema resource is not yet applied there.

### WHY — THE OWNER'S DECISIONS OF 2026-10-11 AND 2026-10-12 (`CEO-D-1004`, `CEO-D-1005`, at the end of `research/ceo-decision-record.md`)

The owner judged the video too dull to attract viewers, after watching two competitor reels with very high engagement (a 17.6 s vertical reel built around a recurring cat character, 507K reactions; a 30.2 s vertical 3D-animated episodic frog series, 3.1M reactions, 50K shares). What drives them: short vertical format, a recurring character, episode numbering, a hook in the first second, and visual storytelling that does not depend on speech. **The owner decided that Wave 12 makes item 001 engaging with a lot of 3D animation drawn by code, using free tools only, and produces it in the company store in both formats.** Decision block: `CEO-D-1100`–`CEO-D-1199`, `CEO-C-1100`–`CEO-C-1199`.

### GOAL

`produce` renders item 001 in the company store with **3D animation drawn by code** replacing the 2D graphics where the owner chooses and filling the 18 clip positions (now labelled placeholders) with animated 3D scenes, as **one long 16:9 video and several short vertical 9:16 cuts (15–30 s each)** from the same scenes; at **USD 0.00 metered spend, with no generative model of any kind** (free tier or otherwise), deterministic where the renderer allows (checked by hash, or recorded honestly as not observed to repeat), every on-screen fact still quoted from a recorded source, and the narration path unchanged.

### WHAT TO BUILD

1. **3D renderer candidates measured first hand early** (owner-approved downloads): React Three Fiber / Three.js through Remotion's three-dimensional package inside the existing pipeline; Blender (GPL) driven by script; CC0 model libraries (e.g. Poly Haven, Quaternius, Kenney) or models built in code. The machine has **no discrete GPU** (Intel Core Ultra 7 165H, integrated graphics): measure WebGL in the headless browser (software or ANGLE) and Blender CPU/Eevee render speed at 1080p before the design rests on them.
2. **A recurring visual character or world** for the channel, proposed by the scope with options and previews for the owner to choose — nothing about the subject's facts invented.
3. **Scene specifications as data** (like Wave 11's graphic specifications), niche-agnostic, with motion timed inside each beat's measured span; the number and text rule of Wave 11 applies to every on-screen text.
4. **Short vertical cuts**: which beats become shorts, their hook, on-screen text, captions from the narration actually used, episode numbering — proposed by the scope for the owner.
5. **Owner review before production**: a contact sheet and previews of the 3D scenes and of each short, rendered from the demonstration store; the owner's approval recorded with hashes (the Wave 11 approval-file capability extended).
6. **The company-store run**: on the owner's go, after a fresh backup, apply the eleventh and any new schema resource alone, verify every installation, then produce item 001 (a new version) in both formats; one decoded video per output, runtime measured from the file, held short of publish-ready.
7. **Carried from Wave 11**: the known issues in `wave-11/release-note.md` routed to the implementer or tech lead that touch rendering, `produce` or the review command.
8. **Owner documentation**: how to preview, approve and re-produce; what changed on screen.

### NOT IN THIS WAVE

Any generative model or metered service (image, video, 3D-model or voice generation included, free tiers included); stock footage or music subscriptions; publication, upload or channel creation; changes to narration sources; a second item or a second channel.

### BINDING CONSTRAINTS CARRIED FORWARD

1. **Nothing publishes.** The structural absences on the upload path stay; the held-stage refusal stays.
2. Budget USD 77.41 per month; company metered ceiling USD 34.42; the item cap of USD 5.95 keeps its counted total of USD 0.007695 (estimate).
3. **Code first, AI when necessary** — and this wave, no AI component at all for visuals (`CEO-D-1004`).
4. Owner approval remains a transition-table state; D-002 is not relaxed.
5. One legal entity, several channels; no second channel.
6. No on-screen number, threshold, scale or relationship invented; every on-screen fact quoted from a recorded source under the Wave 11 rule.
7. Demonstrations and the live test suite use the demonstration store only. The company store `mediacompany` holds real records; any run against it is the orchestrator's, on the owner's go, after a backup.
8. Downloading a library, tool, model asset or font is an owner-approved action, named with its source, size and licence before it happens; asset licences must allow commercial use without attribution conflicts (CC0 preferred).
9. Niche-agnostic and re-pointable: scenes are drawn from specifications, not from code specific to bees.
10. Never modify the installed folders under `C:/Users/vuhoangcao/MediaCompanyRun/` from a phase; verify them against their manifests.

### FRAMEWORK DEFECTS TO BRIEF EVERY SUBAGENT ON

All in `research/framework-defects.md` (twelve). Give every agent its own staging folder, never brief a non-implementing role to write a repository file, and require the list whenever an agent claims a fix covers "every" path.

### PRICING BASIS

Metered spend for item 001 in this wave: none. Local compute is real and unmeasured; record it as unmeasured, never as zero. Development cost and per-item cost are separate quantities and must not be netted.

## Acceptance criteria

_Not provided in the ticket. To be defined and approved at the scope gate before implementation starts._
