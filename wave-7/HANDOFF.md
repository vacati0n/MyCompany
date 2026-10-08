# Wave 7 — handoff to a new session

**Written 2026-10-09 by the session that ran Wave 6. Read this first.**

---

## Start here

```
git checkout main && git pull
omn-agent run MC-8 --target "D:/Project/MyCompany" --approve
omn-agent run MC-8 --target "D:/Project/MyCompany" --show
omn-agent branch MC-8 --target "D:/Project/MyCompany"
```

`MC-8` is **already created, routed to `implement-feature`, and planned**. Its ticket is at
`.omn-agent/tasks/MC-8/input.md`; a copy is committed at `wave-7/00-ticket-mc-8.md`.

**Always pass `--target "D:/Project/MyCompany"`.** Framework state lives there, untracked, shared
across worktrees. **The base checkout is now on `main`** (it was on `develop` until Wave 6), but
all work still happens in the feature worktree `omn-agent branch` creates under `.worktrees/`;
cite that path in every briefing. **Check `main` carries Wave 6** (PR from
`feature/mc-7-wave-6-the-multi-channel-capability`) before relying on the default base.

## Before the first dispatch

1. Docker Desktop running, `docker start mediacompany-pg`; demonstrations use **`mediacompany_demo`
   only**. The company store `mediacompany` holds **no tables** (inspected 2026-10-08) — nothing
   has ever been installed into it.
2. Check the organisation's monthly spend limit (an HTTP 429 cost Wave 5 ten days).

---

## Where the programme is

| Wave | Run | State |
|---|---|---|
| 0–5 | see `wave-5/HANDOFF.md`, `wave-6/HANDOFF.md` | Completed |
| 6 | `run-cdce6ebe03ac` | Completed, 6/6 — multi-channel capability built; no second channel; nothing spent |

**Code:** 98 C# + 7 project files under `src/`, 35 + 5 under `tests/` (count them). **619 tests
pass live**; 517 pass + 96 skip without a store. Architecture suite **52**. **Version 1.3.0
declared** in `Directory.Build.props`.

**Six waves, USD 0.00.** The authorised single-operation exception is **unspent**.

## The question put to the CEO for Wave 7 — check the answer before scoping

`wave-6/bao-cao-ceo.md` section 7 asks whether to spend the single-operation exception (or authorise
a new small amount) on **the first real benchmark** in Wave 7. If the answer is yes, record it as a
new decision in a fresh block (`CEO-D-600`–`699`, `CEO-C-600`–`699`) before the scope phase and
restate it in every briefing; if no or no answer, the ticket stands: capability only, no metered run.
Also check the Wave 6 report's open owner questions (channel one's budget amount and whether channel
amounts must sum within USD 77.41; channel one's configuration values; a recorded home for the
standing charge; whether `CEO-D-400` stands).

## Owner decisions in force from Wave 6

- **`CEO-D-500` (the CEO's own): one legal entity, several channels.** Payee company-level, never
  per channel; channels related for platform enforcement; one payment account to open.
- **`CEO-C-500`:** the per-channel attribute list is the union of `CEO-D-500`'s list and master plan
  17.5; the approval queue is a view over gate state, never configuration.

---

## How Wave 6 ran — repeat it

- **Briefings = phase header + accumulated gate rulings + a common block** (environment facts, owner
  decisions restated, the eleven framework defects, objections invited). Written to the scratchpad
  and handed to a `general-purpose` agent as "read this file first and follow it exactly".
- **Give every agent its own staging folder** (`scratchpad/agents/<phase>/`). A shared folder let
  the planner pick up the scope phase's result envelope.
- **Gate rationales to a file, passed as `"$(cat file)"`.** Restate every gate answer downstream.
- **Correction cycle = SendMessage to the same agent id**, then SendMessage to the reviewer for
  re-verification. Two resumes in Wave 6; 4.4 percent rework.
- **The editor tool and some heredocs refuse**: author long Vietnamese or quote-heavy text with the
  Write tool in the scratchpad and copy it in.
- Gate ownership as in `wave-6/HANDOFF.md` (Scope → `omn-business-analyst`; Planning, Design →
  `omn-tech-lead`; Review, Verification → `omn-qa`; Closure → `omn-orchestrator`).
- **48 agent objections in Wave 6, all correct; 3 were orchestrator errors.** Six waves unbroken.

## Generalised lesson, now confirmed twice

**A property enforced on the way out is not enforced until the round trip is covered — and until
every clock involved is the same clock.** Wave 5's high finding and both of Wave 6's high findings
(gate order by caller instant; report lines from separate reads) were this class, and every phase
before review missed them. Tell the Wave 7 reviewer to hunt for it first; a benchmark record
stamped by the caller would be the next instance.

## Reporting

Every chat reply and status update to the CEO in **Vietnamese**, as well as the report file. Code,
identifiers, paths, commits, PR text, dispatch prompts and framework artifacts stay English.
Precedent: `wave-6/bao-cao-ceo.md`, `wave-6/cost-ledger.md`.
