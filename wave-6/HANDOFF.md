# Wave 6 — handoff to a new session

**Written 2026-10-08 by the session that ran Wave 5. Read this first.**

---

## Start here

```
git checkout main && git pull
omn-agent run MC-7 --target "D:/Project/MyCompany" --approve
omn-agent run MC-7 --target "D:/Project/MyCompany" --show
omn-agent branch MC-7 --target "D:/Project/MyCompany"
```

`MC-7` is **already created, routed to `implement-feature`, and planned**. Its ticket is at
`.omn-agent/tasks/MC-7/input.md` and a copy is committed at `wave-6/00-ticket-mc-7.md`.

**Always pass `--target "D:/Project/MyCompany"`.** Framework state lives there, is untracked,
and is shared across worktrees. **The base checkout at `D:/Project/MyCompany` is on `develop`
and carries only `prompt.txt`** — all repository content is in the feature worktree that
`omn-agent branch` creates under `.worktrees/`. Cite that path in every briefing; the Wave 5
planner caught the orchestrator citing a `research/` path that did not exist at the base.

**Check that `main` carries Wave 5 before relying on the default branch base.** Wave 5 was
merged and pushed on branch `wave-5-sustained-rate` and PR'd to `main`; if the PR has not merged,
pass `--base wave-5-sustained-rate`.

## Before the first dispatch — two infrastructure steps that cost Wave 5 ten days

1. **Start Docker Desktop by hand and wait for the daemon**, then `docker start mediacompany-pg`.
   The demonstrations now run in the **separate database `mediacompany_demo`**
   (`docker exec mediacompany-pg createdb -U postgres mediacompany_demo` if it does not exist);
   the suite destroys the schema of whatever database the test connection string names. If the
   daemon never answers and `wsl -l -v` shows `docker-desktop` Stopped, kill every Docker
   process, `wsl --shutdown`, relaunch.
2. **Check the organisation's monthly spend limit.** An HTTP 429 killed two agents mid-correction
   on 2026-09-28 and the run waited ten days. Resumed agents pick up where they stopped, but each
   death costs a context reload.

---

## Where the programme is

| Wave | Run | State |
|---|---|---|
| 0–4 | see `wave-5/HANDOFF.md` | Completed |
| 5 | `run-423e990b743d` | Completed, 6/6 — sustained-rate capability testable, not tested; nothing spent |

**Code:** 91 C# + 7 project files under `src/`, 30 + 5 under `tests/` (count them; every ticket's
figures have been stale when read). **573 tests pass against a live PostgreSQL 17** in
`mediacompany_demo`; 494 pass and 73 skip without one. Architecture suite **47**, runs on every
solution build.

**Nothing has ever been published and nothing has ever been bought. Five waves, USD 0.00.**
The authorised single-operation exception is **unspent and still available**.

---

## What Wave 6 is, and the tension that shapes it

MASTER-PLAN §35: **multi-channel, channels 1–3 with shared agents**, with two cross-wave
conditions — *approval-workload re-examination precedes any second channel* and *channel
isolation is settled before, not during*. **Neither is dischargeable**: the re-examination needs
the approval-minutes series (does not exist; one 1 min 58 s measurement, no threshold derivable);
channel isolation is the CEO's corporate decision and no artifact records it. So `MC-7` scopes
**the multi-channel capability — per-channel configuration, budget, analytics partition, approval
queue — and not the second channel.** If the scope phase finds even that unbounded, it should
raise a blocking question; it has been right to do so twice.

**The CEO was asked in the Wave 5 report whether to settle channel isolation now.** Check the
answer before scoping; if none, the ticket says to build configuration flexible to both answers
and record the question open.

## The eight carried items the ticket names (all in `wave-5/release-note.md`)

1. Month-period finality over the operation record (cost readings, tier ratio) — extend the
   record horizon or state the limit structurally. Architect. Accepted risk in Wave 5.
2. The recorder's production caller — no stage handler exists. Architect.
3. The unclaimed-rest path has no check. Implementer.
4. Stage rows carry instants from two clocks. Architect, before any cycle-time measure.
5. The audit-chain fork — two appenders can read the same head hash. Pre-Wave-5, latent. Architect.
6. The gate service's Draft transition and the missing awaiting-rights-check writer — no delivered
   path brings a new item to awaiting owner approval; the entry point reaches composition only
   over a 4-row fixture store. Closing this makes it exercisable on real state.
7. A declared version — the repository has none; two release records derived one. One build
   property. Tech lead.
8. Everything partitions by channel without leaving the three-case union.

---

## The discipline, now with three structural layers — extend, never weaken

- **Closed three-case union** (Wave 4): observed value / observed zero (no amount field) /
  unmeasured (no value field). Build-time assertion names the offending type.
- **Row shape checks** (Wave 4 review fix): the row encodes the case; collapsed shapes unwritable.
- **Record horizon** (Wave 5 review fix): the datastore stamps every audit entry at or above a
  horizon under a shared hold held to commit; a check refuses any entry below it from any writer;
  the reader takes the horizon exclusively without waiting (three tries) or reads unmeasured.
  **An observed period is final by construction.** Verified under 6 concurrent writers.

**Generalise:** a property enforced on the way out is not enforced until the round trip is
covered — *and until every clock involved is the same clock.*

---

## Gate ownership under the Producer Exclusion Rule

| Gate | Producer | Decide as |
|---|---|---|
| Scope | `omn-product-owner` | `omn-business-analyst` |
| Planning | `planner` | `omn-tech-lead` |
| Design | `architect` | `omn-tech-lead` |
| Review | `omn-dev-2-reviewer` | `omn-qa` |
| Verification | `omn-dev-2-reviewer` | `omn-qa` — decided normally on five runs now |
| Closure | `omn-documentation` | `omn-orchestrator` |

Step-level CLI: `omn-agent run MC-7 --target ... --dispatch --phase <name> --approve`;
`--complete --phase <name> --approve`; `--gate "<Gate Name>" --decision approve --owner-role
<role> --decided-by "<who>" --rationale "$(cat file)" --approve`. Every side-effecting step
needs `--approve`. Write every rationale to a scratchpad file first.

**The registered agents are not session subagent types.** Dispatch `general-purpose` agents told
to follow the generated `dispatch-prompt.md` and to read any `.claude/` path under `.omn-agent/`
where it really lives. **Resume an agent for its correction cycle with SendMessage to its id**;
its token figure is then cumulative.

## Brief every subagent on the eleven framework findings

`research/framework-defects.md` has all eleven. The ones that cost attempts: bare paths only in
`declared_side_effects` (reviewer: never the change's paths); no foreign identifier tokens, cite
descriptively; three criteria per nested-bullet task; never sign; never a range; never the
string `claude`; version fields take version strings; **read the validator source and run it
read-only over the draft** (12/12 phases first time across Waves 4–5); gate rationale reaches no
downstream phase, restate it; `omn-qa` owns no phase; **Finding 11** — a stale
`failure-envelope.json` of class gate-approval-required sits in the next phase's folder when a
phase is completed before its gate is decided, and is never cleared; tell agents to ignore it.

**Writing files:** the editor tool refuses both the base-repository path and the feature-worktree
path from a session worktree — for framework artifacts *and code*. Author in the scratchpad and
copy in. A quoted-delimiter heredoc breaks whenever its body contains a quoted delimiter (the
orchestrator hit this writing a briefing that quoted the warning about it).

---

## What is owed to the owner, and what is owed by them

**Owed to the owner** — bring back, do not decide: any spend beyond the exception; any
publication, account or channel creation; the clean-record threshold; the approval-workload
figure; channel isolation; and whether `CEO-D-400` (no sizing claim until a series) stands.

**Owed by the owner, blocking first publication, none discharged:** library registration on
every library; the payment account; two-step verification; a library login session for real clip
counts (zero obtained, none may be invented).

---

## Reporting

**Every chat reply and status update to the CEO in Vietnamese, as well as the report file.** The
chat is the status update. Code, identifiers, paths, commits, PR text, dispatch prompts and
framework artifacts stay English. Precedents: `wave-2` to `wave-5/bao-cao-ceo.md`. Produce a cost
ledger; take each agent's final figure, rework is the delta, read the share against the
validator-rejection count.

**Identifier allocation:** Wave 5 took `CEO-D-400`–`499` and `CEO-C-400`–`499`, using only
`CEO-D-400`. Take a fresh block and state which in your first decision record.

---

## The habit that mattered most

**Every recorded agent objection in this programme has been correct, across five waves, without
exception** — 26 in Wave 5 alone, 9 of them orchestrator errors. Invite objections explicitly in
every dispatch prompt, then check the objection instead of defending the instruction.
