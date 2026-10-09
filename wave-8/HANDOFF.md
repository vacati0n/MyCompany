# Wave 8 — handoff to a new session

**Written 2026-10-09 by the session that ran Wave 7. Read this first.**

---

## Start here

```
git checkout main && git pull
omn-agent run MC-9 --target "D:/Project/MyCompany" --approve
omn-agent run MC-9 --target "D:/Project/MyCompany" --show
omn-agent branch MC-9 --target "D:/Project/MyCompany"
```

`MC-9` is **already created, routed to `implement-feature`, and planned**. Its ticket is at
`.omn-agent/tasks/MC-9/input.md`; a copy is committed at `wave-8/00-ticket-mc-9.md`.

**Always pass `--target "D:/Project/MyCompany"`.** Framework state lives there, untracked, shared
across worktrees. The base checkout is on `main`; all work happens in the feature worktree
`omn-agent branch` creates under `.worktrees/`; cite that path in every briefing. **Check `main`
carries Wave 7** (PR from `feature/mc-8-wave-7-the-ai-economics-capability`) before relying on the
default base; if not, branch from that branch.

## Before the first dispatch

1. Docker Desktop running, then `docker start mediacompany-pg` — **the container had stopped on
   2026-10-09** although Docker was up; check `docker ps` rather than assuming. Demonstrations use
   **`mediacompany_demo` only**. The company store `mediacompany` holds **no tables** (checked
   read-only 2026-10-09).
2. Check the organisation's monthly spend limit.
3. **The editor tool refuses `D:/Project/MyCompany/.worktrees/...`** from a session worktree (a
   hook says it is the base checkout). Author in the scratchpad and copy in with the shell.

---

## Where the programme is

| Wave | Run | State |
|---|---|---|
| 0–6 | see `wave-6/HANDOFF.md`, `wave-7/HANDOFF.md` | Completed |
| 7 | `run-56bcc037b633` | Completed, 6/6 — AI-economics capability built; no metered call; corpus unpopulated |

**Code:** 107 C# + 7 project files under `src/`, 39 + 5 under `tests/` (count them). **690 tests
pass live**; 555 pass + 129 skip without a store. Architecture suite **58**. Seven ordered schema
resources. **Version 1.4.0 declared.**

**Seven waves, USD 0.00.** The authorised single-operation exception is **unspent**.

## The question put to the CEO for Wave 8 — check the answer before scoping

`wave-7/bao-cao-ceo.md` section 7 asks whether to authorise the **weekly L3 pass for the
recommendation narrative** (a metered call). The orchestrator recommended no until the first
video exists. If yes, record it as a new decision in block `CEO-D-700`–`799` before the scope
phase and restate it in every briefing; if no or no answer, the ticket stands: code only. Ask with
AskUserQuestion at the start, as Wave 7 did — the CEO answered in one click.

Also check the Wave 7 owner questions (section 3.1 of the report): the observation count (the
orchestrator proposed master plan section 18's "ten comparable runs per task"), the company basis
(USD 34.42 interim), a channel with no budget amount, the level-to-tier mapping, one unstated cost
refusing the month, providers without a cached-unit price, deferred requests not re-admitted; and
the Wave 6 ones still open.

## Owner decisions in force

- **`CEO-D-600` (2026-10-09): no metered benchmark in Wave 7.** The corpus is unpopulated; every
  ranking is the labelled configured ordering.
- **`CEO-D-500` / `CEO-C-500`:** one legal entity, several channels; payee company-level; the
  approval queue is a view over gate state.
- Interim rulings the owner may overturn: company basis USD 34.42 metered allotment (recorded);
  the company allotment caps each estimate; a channel with no budget amount is refused metered work.

---

## How Wave 7 ran — repeat it, with one change

- **Briefings = phase header + accumulated gate rulings + common block** (scratchpad
  `common.md`), handed to a `general-purpose` agent as "read this file first and follow it
  exactly". One staging folder per agent. Gate rationales to files, passed as `"$(cat file)"`.
- **Correction cycle = SendMessage to the same agent id**, then to the reviewer for
  re-verification. Two cycles in Wave 7.
- Gate ownership: Scope → `omn-business-analyst`; Planning, Design → `omn-tech-lead`; Review,
  Verification → `omn-qa`; Closure → `omn-orchestrator`.
- **67 agent objections, all correct; 3 were orchestrator errors.** Seven waves unbroken.
- **The change: check every ruling that touches concurrency or time against every timeout and
  clock involved before sending it.** Wave 7's most expensive error (about 290,000 tokens) was a
  ruling to serialise by waiting on a lock, under a 30-second command timeout, while the holder
  could keep it for a 60-second provider call. The two-clock class has now appeared in three
  consecutive waves — the third time in the orchestrator's own ruling.

## Reporting

Every chat reply and status update to the CEO in **Vietnamese**, as well as the report file. Code,
identifiers, paths, commits, PR text, dispatch prompts and framework artifacts stay English.
Precedent: `wave-7/bao-cao-ceo.md`, `wave-7/cost-ledger.md`.
