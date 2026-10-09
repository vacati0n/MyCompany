# Wave 9 — handoff to a new session

**Written 2026-10-09 by the session that ran Wave 8. Read this first.**

---

## Start here

```
git checkout main && git pull
omn-agent run MC-10 --target "D:/Project/MyCompany" --approve
omn-agent run MC-10 --target "D:/Project/MyCompany" --show
omn-agent branch MC-10 --target "D:/Project/MyCompany"
```

`MC-10` is **already created, routed to `implement-feature`, and planned**. Its ticket is at
`.omn-agent/tasks/MC-10/input.md`; a copy is committed at `wave-9/00-ticket-mc-10.md`.

**Always pass `--target "D:/Project/MyCompany"`.** Framework state lives there, untracked, shared
across worktrees. All work happens in the feature worktree `omn-agent branch` creates under
`.worktrees/`; cite that path in every briefing. **Check `main` carries Wave 8** (PR from
`feature/mc-9-wave-8-the-ai-management-capability`) before relying on the default base; if the
owner has not merged it, ask with AskUserQuestion whether to merge it first (Wave 8 did, in one
click).

## Ask the CEO first — this question may change the whole wave

`wave-8/bao-cao-ceo.md` section 3.1 asks whether to **authorise producing the first metered video
(not published)**, at about USD 2.65 variable under O-002, within the USD 34.42 ceiling, with a cap
the owner names. The orchestrator recommended **yes**, and recommended that Wave 9 then **change
focus** to running that video rather than building autonomy over empty data.

- **If yes:** record it as `CEO-D-800` (block `CEO-D-800`–`899`, `CEO-C-800`–`899`) with the cap,
  **re-scope MC-10 before dispatch** (edit the inbox JSON and re-plan, or create a new ticket),
  and restate the decision in every briefing. The single-operation exception is a different,
  smaller authority; do not stretch it to cover a video.
- **If no or no answer:** the ticket stands: autonomy as recorded proposals, nothing enacted.

Ask it with AskUserQuestion in one call together with the `CEO-Q-700` questions (section 3.2): the
re-verification recorder and cadence, seven versus ten recommendation items, the UTC report week
(Monday 07:00 Vietnam time), price capture versus re-fetch, and the register writer.

## Before the first dispatch

1. Docker Desktop running and `docker ps` shows `mediacompany-pg` up. Demonstrations use
   **`mediacompany_demo` only**. The company store `mediacompany` holds **no tables** (checked
   read-only 2026-10-09).
2. Check the organisation's monthly spend limit.
3. The editor tool may refuse `D:/Project/MyCompany/.worktrees/...` from a session worktree;
   author in the scratchpad and copy in with the shell. A Bash heredoc whose body has an
   apostrophe inside backticks can break; use the Write tool for long briefings.

---

## Where the programme is

| Wave | Run | State |
|---|---|---|
| 0–7 | see earlier handoffs | Completed |
| 8 | `run-79ce8c121936` | Completed, 6/6 — AI-management capability as code; `weekly` and `dashboard` commands; no model call |

**Code (tracked):** 117 C# + 7 project files under `src/`, 42 + 5 under `tests/`. **736 tests pass
live**; 582 pass + 148 skip without a store. Architecture suite **63**. Eight ordered schema
resources. **Version 1.5.0.** **Eight waves, USD 0.00.** The authorised exception is **unspent**.

## Owner decisions in force

- **`CEO-D-700`:** no executive or narrative model call until the first video exists.
- **`CEO-D-701`:** ten comparable runs per task (master plan section 16, per `CEO-C-700`).
- **`CEO-D-702`:** the company metered ceiling is USD 34.42; the standing charge USD 42.99 has no
  recorded home.
- **`CEO-D-600`**, **`CEO-D-500` / `CEO-C-500`** as before. Open questions are in the register
  (dashboard shows 15) and in `CEO-Q-700`.

---

## How Wave 8 ran — repeat it, with two changes

- Briefings = phase header + accumulated gate rulings + common block (`common.md` in the Wave 8
  session scratchpad, `C:/Users/VUHOAN~1/AppData/Local/Temp/claude/D--Project-MyCompany--claude-worktrees-chay-wave-8-db355c/57864e9d-b205-46a2-b027-0cabe98d4d2e/scratchpad/`; reusable). One staging
  folder per agent. Gate rationales to files, passed as `"$(cat file)"`.
- Correction cycle = SendMessage to the same agent id, then to the reviewer. Two cycles in Wave 8.
- Gate ownership: Scope → `omn-business-analyst`; Planning, Design → `omn-tech-lead`; Review,
  Verification → `omn-qa`; Closure → `omn-orchestrator`.
- **53 agent objections, all correct; 5 were orchestrator errors.** Eight waves unbroken.
- **Change 1: never brief a non-implementing role to write a repository file.** Wave 8's
  documentation agent was told to commit `db/README.md`; the runtime rejected it (the first
  rejection in five waves). A policy exception does not widen the completion check (framework
  finding 12); recovery needed a re-dispatch. The orchestrator writes such files in the closing
  commit.
- **Change 2: when ruling on time or concurrency, enumerate every timeout and every hold first**
  — not only the one in front of you. Wave 8's Scope Gate weighed the 60-second hold but not the
  120-second recovery timeout or a lost client; the planner caught it before code, so it cost
  nothing, but it is the fourth wave running in which the class appeared.

## Reporting

Every chat reply and status update to the CEO in **Vietnamese**, as well as the report file. Code,
identifiers, paths, commits, PR text, dispatch prompts and framework artifacts stay English.
Precedent: `wave-8/bao-cao-ceo.md`, `wave-8/cost-ledger.md`.
