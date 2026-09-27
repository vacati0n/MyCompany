# Wave 4 — handoff to a new session

**Written 2026-09-27 by the session that ran Waves 2 and 3. Read this first.**

---

## Start here

```
omn-agent run MC-5 --target "D:/Project/MyCompany" --approve
```

`MC-5` is **already created and planned**, routed to `implement-feature`. Its ticket is at
`.omn-agent/tasks/MC-5/input.md` and a copy is committed at `wave-4/00-ticket-mc-5.md`. It already
carries the constraints, the carried risks and the framework-defect briefing, so you should not
need to reconstruct them.

**Always pass `--target "D:/Project/MyCompany"`.** Framework state lives there, is untracked, and
is shared across worktrees.

**Branch.** All work is on `claude/trusting-shirley-90ea75`. Wave 3's feature branch is merged;
nothing is outstanding. Implementation will refuse to dispatch without a bound feature branch —
create it with `omn-agent branch MC-5 --target "D:/Project/MyCompany" --base claude/trusting-shirley-90ea75`.
**Pass `--base` explicitly.** It defaults to `main`, which does not carry Waves 2–4.

---

## Where the programme actually is

| Wave | Run | State |
|---|---|---|
| 0 | `run-258e0a3415d2` | Completed |
| 1 | `run-dd80173faaad` | Completed, 6/6 |
| 2 | `run-3a58551ee912` | Completed, 6/6 — one video produced, held |
| 3 | `run-ad369fe67ded` | Completed, 6/6 — publishing capability built, nothing published |

**Code:** 89 production files, 27 test files. **438 tests pass against a live PostgreSQL 17**
(411 pass and 27 skip without one; `db/README.md` has the container command). Architecture suite
at 43.

**Nothing has ever been published.** The channel exists, is blank, is not in the programme, and has
no audience. That is the single most important fact for an analytics wave.

---

## The three things that will shape your wave

### 1. There is no audience data, and simulating it is the failure mode

Wave 4 is analytics for a channel that has published nothing. **Six of the specified metrics are
uncomputable** — revenue, RPM, profit per video, profit per channel, ROI, cost per dollar of
revenue — including all three primaries. No source states a revenue-per-thousand-views parameter,
so even a modelled version cannot be produced. MASTER-PLAN §27.5 establishes this; §72 forbids the
dashboard it would produce.

**Ship those displays dark.** They light when a revenue parameter is *observed*, not assumed.

**And the generalisation that has held for two waves: a zero meaning "none occurred" and a zero
meaning "never measured" are different values and must not render identically.** Wave 3 enforced
this for approval components; analytics is where it matters most.

### 2. The carve-out is authorised and still unspent

The owner permitted **one narrow, named exception** to the no-spend rule so that one served
reasoning tier gets recorded. Wave 3 built the capability and did not use it. **Wave 4 is the
natural place, and it is an analytics concern.**

Why it matters more than its size: `C-003` records that **the company's entire cost-control
argument rests on a 290,000 / 87,000 tier split that nothing has ever tested.** Two waves have now
closed with every figure derived from it — including `$2.647440` per item and the `$77.41`
envelope — still an assumption rather than a measurement.

**One record proves the mechanism, not the ratio.** Say so.

### 3. Your own cost measurement has a trap in it

**A resumed agent's reported token figure is cumulative for that agent, not incremental for the
attempt.** I summed every report in Wave 2 and overstated the total by 71% (3.15M against the
correct 1.84M) and the rework share by a factor of six (45.7% against ~7%). The correction and its
evidence are in `wave-3/cost-ledger.md` §1.

**Take each agent's final figure; the rework is the delta.** Both waves land at ~7% on that
reading, which is a more interesting result than the wrong one was.

This is an inference from the arithmetic, not from documentation. It is strongly supported but
labelled as an inference — if you find it is wrong, say so.

---

## Brief every subagent on these. They are measured, not theoretical.

`research/framework-defects.md` has them in full. The ones that actually cost attempts:

1. **`declared_side_effects` takes bare written paths only.** Prose there halts a run permanently.
   **For a reviewer: list only files you wrote, never the paths of the change under review** —
   this has already halted one run in this programme.
2. **No foreign identifier tokens in framework artifacts.** Fails two mutually exclusive ways; no
   token form passes both. **Prefixes do not help** — the scanners use `\b<prefix>-\d{3}\b` and a
   hyphen is a word boundary, so `CEO-D-203` still matches the `D-` family. Cite descriptively.
   **Two of my three phase-1 rejections in Wave 3 were caused by me instructing an agent to cite
   by token.** Do not repeat that.
3. **At least three acceptance criteria per task.** The parser swallows the first nested bullet:
   one written parses as none, two parse as one.
4. **Never sign a decision record.** Signing is the gate's act.
5. **Never express a requirement trace as a range.**
6. **The string `claude` must appear nowhere in a framework artifact** — including paths and branch
   names, which is how it usually gets in.
7. **A version field takes a version string, not a ticket label.**
8. **Tell agents to read the validator source** rather than guess when a check names only its first
   offender. Five have avoided wasted attempts this way.

**Write framework files by shell redirection** — the editor tool refuses the base-repository path
from a worktree.

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

**On the Verification Gate: defect 4 is withdrawn and proven wrong three times.** It claimed the
gate is undecidable because `omn-qa` owns it alone while producing its evidence. False for this
workflow — phase 5 is owned by `omn-dev-2-reviewer`, which produces `review-package.md`. The real
instance is `fix-bug`, already fixed. **If a plan re-raises it as an open question, close it by
deciding the gate, not by arguing.**

---

## What is owed to the owner, and what is owed by them

**Owed to the owner** — bring these back, do not decide them:

- Any spend beyond the authorised carve-out; any publication; any account or channel creation.
- The clean-record threshold that would relax per-publication approval. **Not derivable from the
  one existing measurement** (1 min 58 s, on a held item, owner already knew the contents, no
  queue, no rework). The threshold question stays open until a series exists that includes at
  least one approval which returned a change request.

**Owed by the owner, and blocking first publication — none discharged:**

1. Channel registration on every music and stock library. Revenue lost before registration is
   unrecoverable.
2. The payment account, under the settled payee position.
3. Two-step verification confirmed on the channel.
4. A library login session to obtain real clip counts for the 14 audited subjects — **zero were
   obtained in Wave 2 and none may be invented.**

Each needs the owner to sign in or verify identity. No agent can do them.

---

## Reporting

**CEO-facing reports go in Vietnamese.** Code, identifiers, file paths, commit messages and
framework artifacts stay English — the framework validators read those and the agents work against
English output contracts. Keep technical terms in English where translating loses clarity (RPM,
watch hours, YPP, Content ID, API).

Precedents: `wave-2/bao-cao-ceo.md` and `wave-3/bao-cao-ceo.md`.

**Identifier allocation:** this session's block was `D-200`–`D-299`; `D-200`, `D-201`, `D-202` and
`CEO-D-203` are taken. Take a fresh block and say which in your first decision record. Nothing is
ever renumbered.

---

## The one habit worth carrying over

The most valuable thing in both waves was not any artifact. It was that **agents recorded their
objections instead of quietly complying**, and three times the objection was right and I was wrong:

- the implementation agent that read the source rule and found my `honey bee` supply claim false;
- the documentation agent that refused to publish an explanation I offered because no supplied
  artifact established it;
- the documentation agent that refused to publish a test count **I had personally measured**,
  because it appeared in no run evidence.

**Dispatch prompts should invite that explicitly** — tell agents to say so when they think an
instruction is wrong, rather than complying silently. It is the only mechanism that has caught the
orchestrator's own errors, and it caught them every time.
