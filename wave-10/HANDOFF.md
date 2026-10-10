# Wave 10 — handoff to a new session

**Written 2026-10-10 by the session that ran Wave 9. Read this first.**

---

## Start here

```
git checkout main && git pull
omn-agent run MC-11 --target "D:/Project/MyCompany" --approve
omn-agent run MC-11 --target "D:/Project/MyCompany" --show
omn-agent branch MC-11 --target "D:/Project/MyCompany"
```

`MC-11` is **already created, routed to `implement-feature`, and planned**. Its ticket is at
`.omn-agent/tasks/MC-11/input.md`; a copy is committed at `wave-10/00-ticket-mc-11.md`.

**Always pass `--target "D:/Project/MyCompany"`.** All work happens in the feature worktree
`omn-agent branch` creates under `.worktrees/`; cite that path in every briefing. Check `main`
carries Wave 9 (PR 9) first.

## What Wave 10 is

The owner decided on 2026-10-10 (`CEO-D-805`) that the company's narration must be its own: **a
human recording** the company holds, imported by the pipeline, **or an open-licensed voice model run
in house** when no recording exists. No speech vendor. Read `CEO-D-800` to `CEO-D-805` at the end of
`research/ceo-decision-record.md`.

## Ask the owner early (AskUserQuestion, one call)

- **The in-house model and voice**: the scope phase proposes candidates with licences (weights,
  code, voices) and run-time requirements; the owner approves the download by name, source, size and
  licence before it happens (downloads need explicit permission).
- **The human recording**: who records, the performer's release (rights position), and whether
  item 001 waits for a recording or proceeds with the in-house voice first.
- Any loudness or duration target — none may be invented.

## State of the company store (new this wave)

`mediacompany` **holds real records** since 2026-10-10: nine schema resources installed, 26 prepared
rows, item version 2 opened by the first metered attempt, which failed at Audio with HTTP 429 from
the speech vendor and is booked at its worst case USD 0.007695 (estimate). Backup:
`C:/Users/vuhoangcao/MediaCompanyRun/backup/mediacompany-20261010T074424Z-before-first-metered-run.dump`
(taken before the attempt). Settings used: `C:/Users/vuhoangcao/MediaCompanyRun/settings.metered.json`.
**No phase, test or demonstration may touch the company store**; demonstrations stay in
`mediacompany_demo`. Any run against the company store is the orchestrator's, on the owner's go, and
the auto-mode classifier blocks it without the owner's explicit chat approval.

## Before the first dispatch

1. Docker Desktop has stopped by itself several times. Check `docker ps`; if the daemon is down,
   start Docker Desktop, then `docker start mediacompany-pg`.
2. In Git Bash, `docker exec ... /tmp/...` paths get rewritten: set `MSYS_NO_PATHCONV=1`.
3. Author briefings in the session scratchpad; the editor tool refuses
   `D:/Project/MyCompany/.worktrees/...` from a session worktree, so copy files in with the shell.

## How Wave 9 ran — repeat it, with one change

- Briefing = phase header + every gate rationale so far + common block. Wave 9's `common.md` and
  gate texts are in that session's scratchpad:
  `C:/Users/VUHOAN~1/AppData/Local/Temp/claude/D--Project-MyCompany--claude-worktrees-wave-9-39c695/f788858c-b4e8-4936-a009-e35d62d06b41/scratchpad/`.
- Correction cycle = SendMessage to the same agent id, then to the reviewer.
- Gate owners: Scope → `omn-business-analyst`; Planning, Design → `omn-tech-lead`; Review,
  Verification → `omn-qa`; Closure → `omn-orchestrator`.
- **66 objections in Wave 9, all correct.** Ten waves unbroken.
- **Change: when an agent claims a fix covers "every" reader or path, require the list.** Wave 9's
  first correction claimed every reader and fixed some; the review caught it at a cost of about 514k
  tokens.

## Reporting

Every chat reply and status update to the CEO in **Vietnamese**, as well as the report file.
Precedent: `wave-9/bao-cao-ceo.md`, `wave-9/cost-ledger.md`.
