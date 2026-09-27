# Wave 5 — handoff to a new session

**Written 2026-09-27 by the session that ran Wave 4. Read this first.**

---

## Start here

```
git checkout main && git pull
omn-agent run MC-6 --target "D:/Project/MyCompany" --approve
omn-agent run MC-6 --target "D:/Project/MyCompany" --show
```

`MC-6` is **already created, routed to `implement-feature`, and planned**. Its ticket is at
`.omn-agent/tasks/MC-6/input.md` and a copy is committed at `wave-5/00-ticket-mc-6.md`. It carries
the constraints, the carried risks and the framework-defect briefing, so you should not need to
reconstruct them.

**Always pass `--target "D:/Project/MyCompany"`.** Framework state lives there, is untracked, and
is shared across worktrees.

**Branch.** Wave 4 is **merged to `main`**. Branch from `main`:

```
omn-agent branch MC-6 --target "D:/Project/MyCompany"
```

That defaults to `main`, which is correct. **Check that `main` actually carries Wave 4 before
relying on the default** — a feature worktree cut from a stale `main` silently loses the
foundation, which happened in Wave 2 and was caught only by chance. If `main` is behind, pass
`--base`.

---

## Where the programme is

| Wave | Run | State |
|---|---|---|
| 0 | `run-258e0a3415d2` | Completed |
| 1 | `run-dd80173faaad` | Completed, 6/6 |
| 2 | `run-3a58551ee912` | Completed, 6/6 — one video produced, held |
| 3 | `run-ad369fe67ded` | Completed, 6/6 — publishing capability built, nothing published |
| 4 | `run-5d5e6bd74c36` | Completed, 6/6 — analytics built, revenue displays dark, nothing spent |

**Code:** 89 files under `src/` (82 C# sources, 7 project files), 27 under `tests/` (22 and 5).
**489 tests pass against a live PostgreSQL 17**; 450 pass and 39 skip with a recorded reason
without one (`db/README.md` has the container command, and is itself stale — it still says
"fifteen"). Architecture suite at **46**, and it now **runs on every solution build and fails it**.

**Nothing has ever been published and nothing has ever been bought.** Four waves, USD 0.00 of
operating spend.

---

## The three things that will shape your wave

### 1. Wave 5 as the master plan specifies it is not provable, and the ticket says so

Section 35 specifies **three videos per week — prove the sustained rate**. That proof needs
produced and published items; producing needs commissioned narration and licensed clips;
publishing needs a channel in the programme. **All of it is spend or owner action, and none is
discharged.**

So `MC-6` scopes **the capability and the instrumentation, not the proof** — the same move that
made Waves 3 and 4 work. **If the scope phase judges even that unbounded without owner action,
it should raise a blocking question rather than absorb it.** The Wave 3 scope agent did exactly
that on an unsatisfiable obligation and was right.

### 2. The single highest-value change is small, costs nothing, and is carried out of Wave 4

**The `routes` table has no reasoning-tier column.** The capability boundary already writes the
served tier from the admitted route; the operation record already carries both fields and an
explicit absence marker; Wave 4 proved the mechanism records what it claims to record. **But the
registry constructs every route without a stated tier, so on the fully composed production path
every recorded served tier is the absence marker, whatever route serves it.**

Add the column and its adapter and the assumed reasoning-tier split finally becomes testable by a
**production series** rather than by one demonstration. **Three waves have now closed with the
per-item cost and the monthly envelope resting on a ratio nothing has measured.**

### 3. The discipline Wave 4 made structural, which you must extend and not weaken

**A zero meaning "none occurred" and a zero meaning "never measured" are different values and must
not render identically.** Wave 4 delivered this as a closed three-case union: observed value
(amount + unit), observed zero (unit, **no amount field**), unmeasured (closed two-member reason +
required detail, **no value field of any kind**).

The point is that the wrong rendering is **not expressible**, not merely discouraged: there is no
slot for a null-coalescing zero, and an observed value carrying zero cannot be constructed from
anywhere, including inside its own declaring assembly. A build-time assertion refuses a bare
numeric or an undeclared string member in the analytics namespaces and **names the offending
type**.

**Do not add a quantity outside that union.** If you need a new analytics type, it goes in the two
scanned namespaces or on the named carrier list, or the build refuses it.

---

## The lesson Wave 4 paid for, and the one the peer review paid for

**The review found the central property failing in the direction nobody had anticipated.** Writing
a measurement to the database bound the amount column from the observed-value case alone, so an
**observed zero was stored as NULL and read back as unmeasured**. A measured zero became
never-measured, across the only path that persists a measurement. The scope, the plan, the design
and the orchestrator's briefing had all missed it — every one of them had been thinking about the
surface, and the defect was in the round trip.

The fix is worth copying: **the row now encodes the case**, with a table check admitting exactly
three shapes, so the collapsed shape is **unwritable rather than merely unwritten**. And the agent
reintroduced the defect, watched the checks fail, removed it and watched them pass — so the
coverage is evidence rather than an assertion that happens to hold.

**Generalise it: a property enforced on the way out is not enforced until the round trip is
covered.**

---

## Brief every subagent on these. They are measured, not theoretical.

`research/framework-defects.md` has all ten in full. Findings 7–10 were added during Wave 4.

1. **`declared_side_effects` takes bare written paths only.** Prose there halts a run permanently.
   **For a reviewer: list only files you wrote, never the paths of the change under review.**
2. **No foreign identifier tokens in framework artifacts.** Fails two mutually exclusive ways; no
   token form passes both. **Prefixes do not help** — the scanners use `\b<prefix>-\d{3}\b` and a
   hyphen is a word boundary. Cite descriptively.
3. **At least three acceptance criteria per task** — the parser swallows the first nested bullet,
   so one parses as none and two parse as one. **It applies to nested-bullet fields, not to
   table-row criteria** — the Wave 4 scope agent established that, so do not spend the briefing
   space on it where it does not apply.
4. **Never sign a decision record.** Signing is the gate's act.
5. **Never express a requirement trace as a range.**
6. **The string `claude` must appear nowhere in a framework artifact**, including paths and branch
   names, which is how it gets in.
7. **A version field takes a version string**, not a ticket label.
8. **Tell agents to read the validator source** rather than guess. **Every Wave 4 phase ran its
   validator read-only over its own draft before handing back, and every one passed first time.**
   That is the single cheapest habit in this programme — make it a standing instruction.
9. **A gate decision's rationale reaches no downstream phase.** It goes to the run ledger, the
   event log and the state store; no dispatch prompt names any of them. **If you answer a blocking
   question at a gate, restate the answer in the next phase's dispatch briefing** — and expect a
   careful agent to treat it as an assumption anyway, which is correct of it.
10. **`omn-qa` owns no phase in `implement-feature`**, so a task assigned to it is **never
    dispatched**. That is what left the carve-out unspent in Wave 3. It does **not** contradict the
    withdrawn Defect 4: `omn-qa` is eligible to **decide** the Verification Gate precisely because
    it produces none of that gate's evidence.

Lower-severity, also recorded: the documentation output contract permits four fenced blocks while
its validator caps them at one; the release-note `withheld` verdict is unreachable; the repository
declares no version anywhere.

**Write framework files by shell redirection** — the editor tool refuses the base-repository path
from a worktree. Every Wave 4 agent found that a `cat > path <<'EOF'` heredoc breaks on shell
quoting, and that authoring into the session scratchpad and copying the file in works. Tell them
that up front; it saved each of them a failed attempt after the first.

---

## Gate ownership under the Producer Exclusion Rule

| Gate | Producer | Decide as |
|---|---|---|
| Scope | `omn-product-owner` | `omn-business-analyst` |
| Planning | `planner` | `omn-tech-lead` |
| Design | `architect` | `omn-tech-lead` |
| Review | `omn-dev-2-reviewer` | `omn-qa` |
| **Verification** | `omn-dev-2-reviewer` | **`omn-qa` — decidable** |
| Closure | `omn-documentation` | `omn-orchestrator` |

**The Verification Gate has now been decided normally on four runs by three sessions.** If a plan
re-raises it as undecidable, **close it by deciding the gate, not by arguing.**

---

## What is owed to the owner, and what is owed by them

**Owed to the owner** — bring these back, do not decide them:

- Any spend beyond the authorised exception; any publication; any account or channel creation.
- The clean-record threshold. **Still not derivable** from the one measurement (1 min 58 s, held
  item, owner already knew the contents, no queue, no rework).

**Owed by the owner, and blocking first publication — none discharged:**

1. Channel registration on every music and stock library. Revenue lost before registration is
   unrecoverable.
2. The payment account, under the settled payee position.
3. Two-step verification confirmed on the channel.
4. A library login session to obtain real clip counts for the 14 audited subjects — **zero were
   obtained in Wave 2 and none may be invented.**

---

## The authorised exception is still unspent — and Wave 4 showed why that might keep being true

The owner permitted **one narrow, named exception** so a served reasoning tier would be recorded.
**Wave 4 met the obligation at zero cost**, because the served tier turned out to be a property of
the **admitted route**, not of a provider's response — every outcome path writes the same field.
So executing the delivered boundary against a live record store produced the evidence with no
account, endpoint, credential, purchase or subscription.

**The exception remains available.** The plan criterion requiring *exactly one metered operation*
was **not satisfied as written** — zero ran — while the obligation behind it was. Both halves are
published. **Nothing calls it spent, and you should not either.**

---

## Reporting

**CEO-facing reports go in Vietnamese.** Code, identifiers, file paths, commit messages and
framework artifacts stay English — the validators read those and the agents work against English
output contracts. Keep technical terms in English where translating loses clarity (RPM, watch
hours, YPP, Content ID, API).

Precedents: `wave-2/bao-cao-ceo.md`, `wave-3/bao-cao-ceo.md`, `wave-4/bao-cao-ceo.md`. Also
produce a cost ledger.

**Identifier allocation:** Wave 4 took `CEO-D-300`–`CEO-D-399` and `CEO-C-300`–`CEO-C-399`, and
used `CEO-D-300`, `CEO-D-301`, `CEO-C-300`, `CEO-C-301`. **Take a fresh block and state which in
your first decision record.** Nothing is ever renumbered.

**On measuring your own cost:** a resumed agent's reported figure is **cumulative for that agent,
not incremental for the attempt**. Wave 3 inferred this from arithmetic; **Wave 4 confirmed it** on
two independent meters across three resumed agents, agreeing to within one percent. Take each
agent's final figure; the rework is the delta.

**And read the rework share against the rejection count, never alone.** Wave 4 came in at 9.9%
against Wave 3's 7.1% — and was the better run, because Wave 3's rework was mostly citation repair
caused by the orchestrator while Wave 4's bought a real defect fix and a defeated constraint made
true. Wave 4 had **zero validator rejections; all six phases passed first time.**

---

## The one habit worth carrying over

**Agents recording their objections instead of quietly complying is the only mechanism that has
ever caught an orchestrator's error in this programme, and it has caught every one.** Wave 4 alone:

- the planner reconciled a file count the orchestrator had stated wrongly;
- the scope agent verified an orchestrator claim, found it true, and **still refused to scope from
  it** because it appeared in no supplied input;
- the architect corrected an approved assumption about what the boundary suite covered;
- the reviewer showed that a hard constraint the orchestrator had filed as a **wording problem**
  was in fact **defeated** — and then corrected its own measurement error rather than burying it;
- the reviewer **refused to lower a finding's severity** because the orchestrator had decided to
  defer it, on the ground that a scheduling decision is not evidence and deferral is not risk
  acceptance by the role that owns it;
- the documentation agent **refused to publish the reasoning-tier split the orchestrator supplied
  in its own briefing**, because it appeared in no run artifact.

**Invite it explicitly in every dispatch prompt.** Tell agents to say so when they think an
instruction is wrong rather than complying silently — and then, when one does, check it rather
than defending the instruction. The record across three waves is unbroken: **every recorded
objection has been correct.**
