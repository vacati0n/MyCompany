# Wave 3 — cost ledger, and a correction to Wave 2's

**Run `run-ad369fe67ded` · MC-4 · 2026-09-27 · Completed, 6/6 phases, 6/6 gates.**

---

## 1. ⚠ CORRECTION — Wave 2's reported token total was wrong, and so was its rework share

**Wave 2's ledger reported 3,146,022 tokens and a 45.7% rework share. Both are overstated. The
correct figures are 1,836,117 tokens and roughly 7.0% rework.**

### The error

The harness reports a subagent's token consumption on each completion. When an agent is **resumed**
to repair an artifact, the figure it reports appears to be **cumulative for that agent**, not
incremental for that attempt. Wave 2's ledger **summed every report**, counting the base
consumption once per attempt.

### The evidence that it is cumulative

The arithmetic gives it away. Wave 2's design phase:

| Attempt | Reported | Tool calls | Duration |
|---|---|---|---|
| 1 | 312,889 | 67 | 26 min 16 s |
| 2 (resumed) | 332,728 | **9** | **3 min 20 s** |

**332,728 tokens across 9 tool calls in 3 minutes is not credible**; a delta of 19,839 across 9
calls is ordinary. The same shape appears in every resumed phase, in both waves. Compare Wave 3's
documentation phase, which was never resumed: 224,985 tokens across 38 tool calls — about 6,000
tokens per call, against the 37,000 per call the incremental reading would require above.

### The corrected figures

| | Wave 2 | Wave 3 |
|---|---|---|
| As reported at the time (sum of every report) | 3,146,022 | — |
| **Correct total (sum of each agent's final figure)** | **1,836,117** | **1,930,677** |
| **Rework (sum of the deltas)** | **127,908** | **137,333** |
| **Rework share** | **7.0%** | **7.1%** |

### What this changes, and what it does not

- **The headline governance cost drops by 42%.** Wave 2 cost about 1.84M tokens, not 3.15M.
- **The rework finding collapses from alarming to unremarkable.** "Nearly half the governance cost
  was rework" was wrong. It is about **7%**, and the two waves agree to within a tenth of a point —
  which is itself the more interesting result, because two independent runs landing at 7.0% and
  7.1% suggests a stable property rather than noise.
- **What survives untouched:** every rejection was a genuine defect and none was a false positive;
  the most expensive single invocation was still the post-review correction cycle; and resuming an
  agent still buys wall-clock rather than tokens — the deltas are small but the time saved is
  large (3 min against 26).
- **Still development capex**, not per-video cost. Nothing here divides by one item.

**Confidence.** The cumulative reading is inferred from the arithmetic, not from harness
documentation. It is strongly supported — the incremental reading requires token-per-tool-call
rates that differ by a factor of six between resumed and unresumed agents — but it is an
inference, and it is labelled as one. **If it is wrong, the original figures stand and this
correction should be withdrawn.**

---

## 2. Wave 3, per phase

| Phase | Agent | Attempts | Final tokens | Tool calls | Outcome |
|---|---|---|---|---|---|
| 1 `scope-and-acceptance` | `omn-product-owner` | 3 | 298,107 | 87 | 33/33 PASS |
| 2 `execution-planning` | `planner` | 1 | 267,701 | 53 | 45/45 PASS, first attempt |
| 3 `solution-design-and-risk-assessment` | `architect` | 1 | 328,481 | 66 | **78/78 PASS, first attempt, no repair pass** |
| 4 `implementation` | `omn-dev-1-implement` | 2 | 523,293 | 219 | 32/32 PASS, first attempt; one post-review correction cycle |
| 5 `quality-review` | `omn-dev-2-reviewer` | 1 | 288,110 | 101 | 31/31 PASS |
| 6 `documentation-and-release-handoff` | `omn-documentation` | 1 | 224,985 | 38 | 32/32 PASS, first attempt |
| | **TOTAL** | | **1,930,677** | **564** | |

**Four of six phases passed validation on the first attempt**, against two of six in Wave 2. The
two that did not: phase 1, rejected twice — **both rejections caused by this session instructing
the agent to cite foreign identifiers by token**, not by the agent's judgement; and phase 4, which
passed validation first time and was re-opened by the peer review's blocking finding.

**Rework: 137,333 tokens, 7.1%.** Composed of phase 1's citation repairs (65,081) and phase 4's
post-review correction cycle (72,252).

---

## 3. The carve-out, and what it did and did not establish

The owner permitted **one narrow, named exception** to the no-spend constraint so that a served
reasoning tier could be recorded — the first test of the routing mechanism the company's entire
cost-control argument rests on.

**Status at close: the served-tier count was not produced in this run.** The plan assigned the
metered carve-out and its count to a role that did not run, and the release note records it among
the 16 known issues rather than claiming it. **The carve-out remains unspent and available.**

**What the wave did deliver against it:** the design makes the served tier readable via an optional
stated tier on the admitted route, the meter already carries both fields and the absence marker,
and the implementation fixed a real defect that would have made the measurement meaningless — **a
delivered test double silently dropped both reasoning-tier fields**, so the boundary wiring was
untested rather than passing. That defect was found only because coverage was added.

**Therefore the untested-ratio finding stands unchanged.** Every figure derived from the assumed
290,000/87,000 split — including the per-item variable cost and the monthly envelope — remains an
**assumption, not a measurement**. This is the second wave to close with that sentence true.

---

## 4. A measurement this session took that the run does not record

**The architecture suite is at 43 passing at the corrected head.** I ran it directly, twice —
before the correction cycle it was 38, after it 43.

**The release note publishes 38, not 43, and that is correct.** The figure appears in no supplied
artifact: the review package measured 38 at the three pre-correction commits, and the
implementation report states the 438 solution-suite figures without restating an architecture count
at the final head. The documentation agent **declined to publish a number the orchestrator had
personally measured**, because publishing it would have failed traceability, and raised the gap as
a known issue with a post-release check instead.

That was the right call and it is recorded here rather than argued with. **This file is not run
evidence**, so it does not close the known issue; it records the measurement and its provenance so
whoever discharges that check knows what to expect.

---

## 5. The standing envelope, unchanged

| Line | Monthly |
|---|---|
| Stock library | $30.00 |
| Music | $6.99 |
| Narration entitlement | $6.00 |
| Remainder for all AI text, narration and imagery | ~$34.42 |
| **Approved envelope** | **$77.41** |

**Nothing was spent in Wave 3.** No upload, no channel, no account, no purchase, no subscription
session, no render. The carve-out is authorised and unused.
