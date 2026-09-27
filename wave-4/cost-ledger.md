# Wave 4 — cost ledger

**Run `run-5d5e6bd74c36` · MC-5 · 2026-09-27 · Completed, 6/6 phases, 6/6 gates.**

---

## 1. The token-accounting correction is now confirmed, not inferred

Wave 3's ledger corrected Wave 2's token total downward by 42% on the reasoning that **a resumed
agent's reported figure is cumulative for that agent, not incremental for the attempt**. It
labelled that an inference from arithmetic rather than from documentation, and said plainly that
if it were wrong the original figures should stand and the correction be withdrawn.

**It is not wrong. This wave measured it directly, on two independent meters.**

The implementation agent was resumed once, for the review's correction cycle. Two meters reported
it, and they agree:

| Reading | After first pass | After correction pass | Interpretation |
|---|---|---|---|
| Harness `subagent_tokens` | 446,016 | **559,010** | cumulative |
| The agent's own remaining-budget counter | ~443,000 | ~**553,000** | cumulative |

The agent described its second figure, unprompted, as *"for this agent as a whole"*. The reviewer,
resumed once for the same reason, reported ~349,000 against a harness figure of 352,474 — the same
shape again.

**Two meters that do not consult each other, agreeing to within 1%, on three resumed agents.** The
inference is discharged. **Take each agent's final figure; the rework is the delta.**

---

## 2. Wave 4, per phase

| Phase | Agent | Final tokens | Framework attempts | Duration | Validation |
|---|---|---|---|---|---|
| 1 `scope-and-acceptance` | `omn-product-owner` | 187,279 | 1/3 | 14m 20s | 33/33 PASS |
| 2 `execution-planning` | `planner` | 208,176 | 1/3 | 16m 58s | 45/45 PASS |
| 3 `solution-design-and-risk-assessment` | `architect` | 311,321 | 1/3 | 22m 54s | 78/78 PASS |
| 4 `implementation` | `omn-dev-1-implement` | 559,010 | 1/3 | 41m 49s | 32/32 PASS |
| 5 `quality-review` | `omn-dev-2-reviewer` | 352,474 | 1/3 | 52m 53s | 31/31 PASS |
| 6 `documentation-and-release-handoff` | `omn-documentation` | 241,145 | 1/3 | 15m 14s | 32/32 PASS |
| | **TOTAL** | **1,859,405** | | **2h 44m** | **251/251** |

**Every phase passed validation on its first framework attempt — six of six.** Wave 2 managed two
of six; Wave 3 managed four of six.

**On the attempt counts.** The runtime records `1/3` for every phase because both correction cycles
were **host-level resumes of the same invocation**, not framework retries. That is an honest
nuance and not a claim of perfection: the work was reopened twice.

---

## 3. Rework — the share went up, and that is the good news

| | Wave 2 | Wave 3 | **Wave 4** |
|---|---|---|---|
| Total tokens | 1,836,117 | 1,930,677 | **1,859,405** |
| Rework | 127,908 | 137,333 | **183,885** |
| **Rework share** | 7.0% | 7.1% | **9.9%** |
| Phases passing validation first time | 2 / 6 | 4 / 6 | **6 / 6** |
| Rework caused by validator rejection | most | most | **none** |

Composition of the 183,885: the implementation correction cycle (112,994) and the reviewer's
re-verification of it (70,891).

**The two numbers move in opposite directions and both moved the right way.** Validator rejections
went to zero, and every token of rework bought something: a real high-severity defect in delivered
code, and a defeated hard constraint made true. Wave 3's rework was largely **citation repair
caused by the orchestrator instructing an agent to cite by token** — cost that bought nothing.
Wave 4's rework bought two fixes that no validator would ever have caught.

**A rework share is not a quality measure on its own.** 7.1% of wasted effort is worse than 9.9%
of effort that found a defect. Read the two rows together or not at all.

---

## 4. The carve-out — unspent, and the reason is the interesting part

The owner authorised **one narrow, named exception** to the no-spend rule so that one served
reasoning tier would be recorded. **Wave 3 did not spend it. Wave 4 did not spend it either, and
this time the obligation behind it was met anyway.**

Reading the delivered boundary showed that **the served tier is a property of the admitted route,
not of a provider's response**: every outcome path writes the route's stated tier, including the
non-provider paths. The evidence was therefore obtainable by executing the delivered boundary
against a live record store over an admitted route — **at zero cost, with no provider account, no
endpoint, no credential, no purchase and no subscription**, and without weakening the empty
endpoint position.

| | |
|---|---|
| Spend committed this wave | **USD 0.00** |
| Authorised exception | **unspent, still available** |
| Monthly envelope | **USD 77.41, unchanged** |

**The plan criterion requiring that exactly one metered operation be run is not satisfied as
written — zero were run.** The obligation behind it is. Both halves are published in the release
record, and nothing anywhere calls the exception spent.

**What the evidence establishes, and what it does not:**

- **Establishes:** the mechanism records what it claims to record. Requested and served tiers are
  written and read back out of the store, deliberately different so a value copied from the
  request is excluded, with an explicit absence marker where the route states none.
- **Does not establish the assumed reasoning-tier split.** Untouched, unmeasured, and still the
  basis of the per-item variable cost and the monthly envelope. **This is the third consecutive
  wave to close with that sentence true.**
- **Does not establish anything about a provider.** The record came from a non-provider path. That
  the provider path behaves identically rests on it reading the same field — a property of the
  source, not an observation.

**And the limit that decides what the whole line is worth today:** the `routes` table declares
**no reasoning-tier column at all**, and the registry constructs every route without one. On the
fully composed production path, **every recorded served tier is the absence marker, whatever route
serves it.** The mechanism is proven; the production path cannot yet feed it. The release record
names that column as the highest-value single change for this line.

---

## 5. Measurements taken outside run evidence

Recorded here, where they belong, because **this file is not run evidence**.

1. **The orchestrator executed the test suite directly**: 489 executed, 489 passed, 0 skipped with
   the datastore. This one *is* also in run evidence — the implementer and the reviewer measured it
   separately and all three agree exactly — so the release record publishes it.

2. **The orchestrator performed an adversarial injection.** A bare `decimal` added to a delivered
   analytics read model made `dotnet build MediaCompany.slnx` **fail**, naming the assertion that
   refused it; removing it restored a clean build reporting 46 assertions. **The reviewer declined
   to certify this**, correctly, because writing a bare numeric into source lies outside its
   contract. The release record publishes it as **claimed, not confirmed**, and the build-time
   property stands on the reviewer's own executed checks instead. That is the second time in this
   programme an agent has refused to ratify an orchestrator's personal measurement, and it was
   right both times.

---

## 6. The standing envelope, unchanged

| Line | Monthly |
|---|---|
| Stock library | $30.00 |
| Music | $6.99 |
| Narration entitlement | $6.00 |
| Remainder for all AI text, narration and imagery | ~$34.42 |
| **Approved envelope** | **$77.41** |

**Nothing was spent in Wave 4.** No upload, channel, account, purchase, subscription session or
render. Four waves have now closed without committing a cent of the operating budget, and the
authorised exception has survived two waves unspent.

**Development cost and per-item cost remain separate quantities and are not netted.** Nothing in
this ledger divides by an item: the channel has published nothing.
