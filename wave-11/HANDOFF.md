# Wave 11 — handoff to a new session

**Written 2026-10-10 by the session that ran Wave 10. Read this first.**

---

## Start here

```
git checkout main && git pull
omn-agent run MC-12 --target "D:/Project/MyCompany" --approve
omn-agent run MC-12 --target "D:/Project/MyCompany" --show
omn-agent branch MC-12 --target "D:/Project/MyCompany"
```

`MC-12` is **already created, routed to `implement-feature`, and planned**. Its ticket is at
`.omn-agent/tasks/MC-12/input.md`; a copy is committed at `wave-11/00-ticket-mc-12.md`.

**Always pass `--target "D:/Project/MyCompany"`.** All work happens in the feature worktree
`omn-agent branch` creates under `.worktrees/`; cite that path in every briefing. Check `main`
carries Wave 10 (PR 10) first. If the session's own worktree is behind `main`, sync it first.

## What Wave 11 is

The owner decided on 2026-10-10 (`CEO-D-903`) that item 001's 22 graphics are drawn for real, by
code, on the company's machine, from the shot list and the recorded claims, replacing the text
cards. No model call, no metered spend, no subscription. Read `CEO-D-805` to `CEO-D-903` at the end
of `research/ceo-decision-record.md`.

## Ask the owner early (AskUserQuestion, one call)

- **The renderer**: the scope phase proposes candidates (a drawing library, ffmpeg filter graphs, an
  SVG-to-frames path) with licences, sizes and run-time requirements on this machine; the owner
  approves any download by name, source, size and licence before it happens.
- **Visual choices the shot list leaves open** (palette, typeface, scale of a plan view): nothing
  may be invented; offer options with previews where possible.
- Whether the owner wants a contact sheet of all 22 graphics to approve before the company-store run.

## State of the company store

`mediacompany` holds item 001 versions 2 (Metered, failed at Audio, one estimate operation USD
0.007695) and 3 (Own, InHouseModel, rendered 629.733 s, held), ten schema resources, 73 tables.
Latest backup: `C:/Users/vuhoangcao/MediaCompanyRun/backup/mediacompany-20261010T125616Z-before-own-voice-run.dump`
(taken before the tenth resource and version 3). **No phase, test or demonstration touches it**;
demonstrations stay in `mediacompany_demo`. Any run against it is the orchestrator's, on the owner's
explicit go, after a fresh backup.

## The in-house voice (installed in Wave 10)

`C:/Users/vuhoangcao/MediaCompanyRun/voice/` (Piper 1.8.0, `en_GB-cori-high`); orchestrator settings
`C:/Users/vuhoangcao/MediaCompanyRun/settings.company.json` (company output root) and
`settings.own.json` (demonstration output root). `verify-model` must still exit 0 before any run.
Narration takes about 5.5 minutes per production of item 001 on this machine.

## Before the first dispatch

1. Check `docker ps`; if the daemon is down, start Docker Desktop, then `docker start mediacompany-pg`.
2. In Git Bash, `docker exec ... /tmp/...` paths get rewritten: set `MSYS_NO_PATHCONV=1`.
3. Author briefings in the session scratchpad; the editor tool refuses
   `D:/Project/MyCompany/.worktrees/...` from a session worktree, so copy files in with the shell.
   Heredocs with apostrophes inside `$(...)` broke the Bash tool twice: write long briefings with the
   Write tool and substitute placeholders with `sed`.

## How Wave 10 ran — repeat it

- Briefing = phase header + every gate rationale so far + common block. Wave 10's `common.md`, gate
  texts and briefings are in that session's scratchpad:
  `C:/Users/VUHOAN~1/AppData/Local/Temp/claude/D--Project-MyCompany--claude-worktrees-chay-wave-10-9e83c3/1fbc9ca0-3968-48ae-a2b6-746e4536ef61/scratchpad/`.
- Correction cycle = SendMessage to the same agent id, then to the reviewer.
- Gate owners: Scope → `omn-business-analyst`; Planning, Design → `omn-tech-lead`; Review,
  Verification → `omn-qa`; Closure → `omn-orchestrator`.
- **51 objections in Wave 10, all correct.** Ten waves unbroken.
- **Kept from Wave 9 and it worked:** when an agent claims a fix covers "every" path, require the
  list. Wave 10's rework fell to 3.0 percent.
- **New this wave and worth repeating:** install and measure a downloaded tool first hand early
  (with the owner's approval) so the design rests on measurements; run the real thing once in the
  demonstration store before the company store.
- **Watch:** keep your own gate rulings consistent with later briefings (Wave 10's implementation
  briefing contradicted the Planning Gate on where expected hashes live), and align the briefing's
  final-reply format with the dispatch prompt's.

## Reporting

Every chat reply and status update to the CEO in **Vietnamese**, as well as the report file. Refer
to the owner as "CEO", not by a gendered pronoun. Precedent: `wave-10/bao-cao-ceo.md`,
`wave-10/cost-ledger.md`.
