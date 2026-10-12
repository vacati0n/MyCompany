# Wave 12 — handoff to a new session

**Written 2026-10-12 by the session that ran Wave 11. Read this first.**

---

## Start here

```
git checkout main && git pull
omn-agent run MC-13 --target "D:/Project/MyCompany" --approve
omn-agent run MC-13 --target "D:/Project/MyCompany" --show
omn-agent branch MC-13 --target "D:/Project/MyCompany"
```

`MC-13` is **already created, routed to `implement-feature`, and planned**. Its ticket is at
`.omn-agent/tasks/MC-13/input.md`; a copy is committed at `wave-12/00-ticket-mc-13.md`.

**Always pass `--target "D:/Project/MyCompany"`.** All work happens in the feature worktree
`omn-agent branch` creates under `.worktrees/`; cite that path in every briefing. Check `main`
carries Wave 11 (the MC-12 PR) first. If the session's own worktree is behind `main`, sync it first.

## What Wave 12 is

The owner judged item 001's video too dull and decided (`CEO-D-1004`, `CEO-D-1005`, end of
`research/ceo-decision-record.md`) that Wave 12 makes it engaging with **a lot of 3D animation drawn
by code, free tools only, no generative model of any kind (free tiers included)**, as **one long
16:9 video plus several short vertical 9:16 cuts (15–30 s)**, and **produces item 001 in the company
store** (Wave 11 ran no company-store production). The owner's reference: two competitor reels with
very high engagement — short, vertical, a recurring character, episode numbering, a hook in the
first second, visual storytelling that needs no speech.

## Ask the owner early (AskUserQuestion, one call)

- **The 3D toolset**: React Three Fiber / Three.js through Remotion's three-dimensional package (fits
  the Wave 11 pipeline), Blender by script, CC0 model libraries — each with licence, size and
  run-time needs; every download approved by name, source, size and licence.
- **A recurring character or world for the channel** — offer options with previews; it must not
  invent facts about the subject.
- **Which beats become shorts**, and whether the 2D graphics are replaced or kept beside the 3D scenes.

## Measure first hand early (the Wave 10 and 11 lesson)

The machine has **no discrete GPU** (Intel Core Ultra 7 165H, 22 logical processors, integrated
graphics). Before the design rests on any 3D renderer, install it (with approval) and measure a 1080p
scene: WebGL in Chrome Headless Shell (software or ANGLE), Blender CPU and Eevee; render twice and
compare bytes; check the installed folders are not written to. Wave 11's trial method is in the
decision record (`CEO-D-1001` and the orchestrator's notes).

## State of the stores and installations

- **Company store `mediacompany`:** item 001 versions 2 (metered, failed at Audio, USD 0.007695
  estimate) and 3 (own voice, text cards, held); **ten** schema resources — the eleventh
  (`db/011-graphics.sql`) is **not** applied there yet. Latest backup:
  `C:/Users/vuhoangcao/MediaCompanyRun/backup/mediacompany-20261010T125616Z-before-own-voice-run.dump`.
  Any run against it is the orchestrator's, on the owner's explicit go, after a fresh backup; the
  procedure is in `wave-11/owner-guide-graphics.md` section 5 (readings, `install --from 11`,
  settings from the decision record, verify, plan-only, produce, manifest check).
- **Renderer** (Wave 11): `C:/Users/vuhoangcao/MediaCompanyRun/graphics/` — `remotion/`, `browser/`,
  `font/`, `manifests/` (four SHA-256 manifests; their own hashes in the decision record). Never run
  with bundler caching on; never let a render write there; verify with `verify-graphics`.
- **Voice** (Wave 10): `C:/Users/vuhoangcao/MediaCompanyRun/voice/` (Piper 1.8.0, `en_GB-cori-high`).
- **The owner's approved 2D graphics:** final-frame hashes in `CEO-D-1003`; approval-file shape in
  `wave-11/owner-guide-graphics.md` section 4.

## Before the first dispatch

1. Check `docker ps`; if the daemon is down, start Docker Desktop, then `docker start mediacompany-pg`.
2. In Git Bash, set `MSYS_NO_PATHCONV=1` for `docker exec` paths.
3. Author briefings in the session scratchpad with the Write tool; the editor refuses
   `D:/Project/MyCompany/.worktrees/...` from a session worktree, so copy files in with the shell.
   **Never put back-quoted paths in an unquoted shell heredoc** (Wave 11: one ran as a command).
   Keep gate rationales ASCII-only (the command line mangled ° and –).
4. The full live suite takes about an hour; run it with `-m:1` if MSBuild child nodes crash.

## How Wave 11 ran — repeat it

- Briefing = phase header + every gate rationale so far + common block. Wave 11's `common.md`, gate
  texts and briefings are in that session's scratchpad:
  `C:/Users/VUHOAN~1/AppData/Local/Temp/claude/D--Project-MyCompany--claude-worktrees-wave-11-2530cc/010316c0-94b9-488d-8a9f-04a1466220ab/scratchpad/`.
- Correction cycle = SendMessage to the same agent id, then to the reviewer.
- Gate owners: Scope → `omn-business-analyst`; Planning, Design → `omn-tech-lead`; Review,
  Verification → `omn-qa`; Closure → `omn-orchestrator`.
- **64 objections in Wave 11, all correct or accepted.** Eleven waves unbroken.
- **Worked:** installing and measuring the renderer first hand before planning; full file manifests;
  showing the owner a contact sheet and preview before any production; asking the owner several
  visual questions at once with recommended defaults.
- **Watch:** the review's high finding took three cycles because "admit only quotations" was an
  unlisted completeness claim. For any matching or filtering rule, require the implementer's
  adversarial probe list (every edge, every token kind) before review. Align the briefing's
  final-reply format with the dispatch prompt's (third wave running).

## Reporting

Every chat reply and status update to the CEO in **Vietnamese**, as well as the report file. Refer
to the owner as "CEO", not by a gendered pronoun. Precedent: `wave-11/bao-cao-ceo.md`,
`wave-11/cost-ledger.md`. Merge the PR only on the CEO's explicit "merge".
