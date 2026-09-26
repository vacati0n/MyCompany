# Wave 2 — the real cost ledger

**Run `run-3a58551ee912` · MC-3 · opened 2026-09-26.** Live document: rows are added as the run
consumes them. **Every figure here is a MEASUREMENT unless the Basis column says ESTIMATE.**

The ticket's constraint 7 asks for the real tokens, characters, images and generated seconds
this first video consumes, against the ~USD 1.58 estimate for a ten-minute video carried from
`research/ai-capacity-dossier.md`. This file is that comparison, and it is also the evidence for
the first of the four things Wave 2 must prove.

---

## 1. What is actually meterable here, and what is not

Recorded first because it bounds everything below, and because *whether the ledger can be
evidenced at all* is itself part of what this wave tests.

| Quantity | Meterable in Wave 2? | Why |
|---|---|---|
| **Orchestration + agent tokens** | **Partly — floor only** | The harness exposes the *context window* of a session, not cumulative billed input/output tokens per request. Context size is a **floor**, not the billed total: every turn re-sends its context, so billed input tokens exceed the final context reading by a large and unrecorded multiple. Subagent consumption is not exposed to the parent at all. |
| **Narration characters** | **Yes — exact** | Derived by counting the finished script. No API call needed to know the number. |
| **Images / graphics** | **Yes — exact count** | The shot list and graphics specification enumerate them. |
| **Generated video seconds** | **Yes — exact, and it is zero** | D-006 caps AI video at sparing cutaways; this video commissions none (see §3). |
| **Actual USD charged** | **No — and correctly so** | Wave 2 commits no spend (D-014). Nothing is billed. Unit prices are applied to measured quantities to produce a *priced* ledger, not an invoice. |

**[FINDING — carry to the CEO report]** The token line is the one quantity the company wants
most and the one its current tooling measures worst. Wave 1's architecture already answers this
properly: decision record D-006 records the operation at the resolution boundary, **per attempt,
in the same transaction as the work, with the applied price row referenced**. That is exactly
the instrument missing here. **Wave 2 is being metered by hand precisely because the Wave 1
metering boundary is not yet running the pipeline.** The estimate-versus-actual comparison
therefore becomes trustworthy only once production routes through M-001/M-005 — and that is an
argument for doing so before volume, not after.

## 2. Token measurements taken

Context-window readings from the orchestrating session, `run-3a58551ee912`. Each is a floor.

### 2.1 Orchestration session (floor only)

| # | Point in the run | Tokens in context | Basis |
|---|---|---|---|
| T-1 | Required reading complete; phase 1 dispatched | 153,003 | MEASURED |

### 2.2 Phase agents — the real per-phase consumption

**This is the measurement that matters, and it is exact.** The harness reports each dispatched
subagent's total token consumption on completion, so unlike the orchestration line these are
billed totals rather than context floors.

| Phase | Agent | Tokens | Tool calls | Wall clock | Basis |
|---|---|---|---|---|---|
| 1 `scope-and-acceptance` | `omn-product-owner` | **257,876** | 50 | 16 min 6 s | MEASURED |
| 2 `execution-planning` | `planner` | **313,533** | 62 | 24 min 25 s | MEASURED |
| 3 `solution-design-and-risk-assessment` attempt 1 | `architect` | **312,889** | 67 | 26 min 16 s | MEASURED — **rejected at validation** |
| 3 attempt 2 (repair) | `architect` (resumed) | **332,728** | 9 | 3 min 20 s | MEASURED — **78/78 PASS** |
| | **Phase 3 total** | **645,617** | 76 | 29 min 36 s | Two attempts for one artifact |
| 4 `implementation` | `omn-dev-1-implement` | _reported by the harness on completion_ | | | Mixed capex/opex — see `C-002` |
| 5 `quality-review` | `omn-dev-2-reviewer` | | | | |
| 6 `documentation-and-release-handoff` | `omn-documentation` | | | | |
| | **Total** | | | | |

**[FINDING — rework is a real cost line and no estimate contains it.]** Phase 3's first attempt
consumed **312,889 tokens and was rejected by the validation engine** on two blocking checks:
`D4.3` (a requirement expressed its trace as a range, `AC-001` to `AC-032`, which is not a
traceable statement identifier) and `D17.6` (a decision record was emitted with a **signed**
approval block, which the Producer Exclusion Rule forbids — signing is the gate's act, not the
producing agent's).

Both are genuine artifact defects rather than framework defects, and **the validator catching
them is the apparatus working**. But the accounting consequence is that the phase costs attempt
1 *plus* attempt 2, and **every cost estimate the company holds prices a single clean pass.**
The retry budget is three attempts, so a phase's worst case is roughly three times its nominal
cost. Recorded because a rework rate is a real parameter of what a governed pipeline costs, and
this run is the only place it has ever been observed.

**Mitigation used — and the measurement contradicts the reasoning for it, which is worth
recording honestly.** Attempt 2 resumed the *same* agent with its context intact and a precise
statement of the two failed checks, on the reasoning that a fresh agent would re-pay the whole
context cost to fix two formatting defects. **On tokens that reasoning was wrong.** The repair
consumed **332,728 tokens against attempt 1's 312,889** — *more*, not less, because resuming
replays an accumulated context that had grown past the original. Phase 3 therefore cost
**645,617 tokens to produce one artifact**, slightly over twice its clean-pass cost.

Where the resume did win decisively was **wall-clock and tool calls: 3 min 20 s and 9 tool calls
against 26 min 16 s and 67**, because the agent already knew the design and only had to edit.
So the right statement is: **resuming buys time, not tokens.** For a rework whose cost the
company cares about in money, that distinction matters, and the intuition that "reusing context
is cheaper" is false here.

**The rework parameter, stated:** one phase of six needed a second attempt, and that phase cost
**2.06×** its clean-pass cost. One observation is not a rate, but it is the only observation
anyone has.

**[FINDING, corrected 2026-09-26 — the first framing of this was wrong and the correction
matters more than the original.]** Phase 1 consumed **257,876 tokens** to produce a scope
definition. An earlier draft of this ledger set that against the per-video content estimate and
implied the unit economics were in trouble. **That comparison was invalid and is withdrawn.**
Two separate quantities were being netted against each other:

| | What it is | How it recurs |
|---|---|---|
| **Development cost (capex)** | Framework tokens spent on the six governed phases — deciding what to build, bounding it, planning, designing, reviewing, documenting. Phase 1 is wholly this. | **Once per change**, amortised over every video the resulting pipeline ever produces. Not a per-video multiplier. |
| **Operating cost (opex)** | Tokens the production path spends turning a subject into a finished item. | **Once per video**, 13 times a month. |

**The per-video ledger already budgets governance**, which the earlier framing missed. Of the
377,000 input / 33,000 output tokens per video in `artifacts/03-option-analysis.md`, the
governance share is explicitly itemised: compliance gate pack 30,000/3,000; CEO approval package
assembly 8,000/1,500; quality-control review 25,000/2,500; fact-check 60,000/3,000 — **123,000
input and 9,500 output tokens, about a third of the per-video budget, already allocated to
governance.** The concern that governance was unbudgeted per video does not survive contact with
the ledger.

**What survives, and is still worth having:** development cost is genuinely absent from every
estimate the company holds. Nobody has costed what it takes to build and later change this
system. That is a real gap, it should be watched across waves, and Wave 2 is the first
measurement of it — but it is capex, and presenting it as a per-video figure would misinform the
CEO in the direction of alarm.

**The thing that would actually be alarming, and what this metering is really for.** If the
*built* pipeline needs a per-video governed structure heavier than 377,000 input tokens — for
instance if the compliance determinations need several adversarial passes rather than one L3
call — then per-video cost rises and the approved ledger is wrong. **A measured per-video total
materially above 377,000 input tokens is the finding that escalates.** That is the test this
ledger exists to run, and it is not yet answerable.

## 2.5 The estimate this is measured against, decomposed

From `research/ai-capacity-dossier.md` §F.3, EXPECTED case, carried unchanged. Stated here so
the comparison is against a decomposition rather than against a single number.

| Stage | Input tokens | Output tokens | Cost |
|---|---|---|---|
| Research | 150,000 | 8,000 | $0.3800 |
| Scripting | 50,000 | 6,000 | $0.1600 |
| Fact-check | 60,000 | 3,000 | $0.1500 |
| SEO (Haiku) | 10,000 | 2,000 | $0.0200 |
| QC (Haiku) | 25,000 | 2,500 | $0.0375 |
| Web search (8 calls) | — | — | $0.0800 |
| **LLM subtotal** | **295,000** | **21,500** | **$0.8275** |
| Thumbnails, 6 × gpt-image-2.5 high | — | — | $0.3161 |
| TTS, 8,700 chars × $0.05/1,000 | — | — | $0.4350 |
| **ESTIMATED TOTAL** | | | **$1.5786 → ~$1.58** |

**⚠ The comparison target is NOT USD 1.58, and both the ticket and the scope definition carry
the stale figure.** This needs correcting before any variance is reported, because comparing
against $1.58 would overstate an overrun that may not exist.

- **$1.58** is `research/ai-capacity-dossier.md` §F.3's EXPECTED case: **295,000 input / 21,500
  output** tokens covering research, scripting, fact-check, SEO and QC only.
- **The CEO-approved figure is option O-002** in `artifacts/03-option-analysis.md`, which
  supersedes it with a fuller ledger — it adds shot-list and clip selection, motion-graphic
  briefs, caption polish, the compliance gate pack, cutaway prompts and CEO approval package
  assembly. **377,000 input / 33,000 output tokens, USD 2.647440 per video variable, USD 42.99
  per month standing, USD 77.41 per month total, USD 5.95 per video all-in at 13 videos/month.**

The $77.41 envelope the ticket enforces is O-002's own total, so the ticket is enforcing O-002's
budget while quoting the older, narrower per-video number beside it. `S-006` inherited the same
figure from the ticket.

**Measured cost is therefore reported against O-002**, with $1.58 shown only as the superseded
figure it is. Recorded as an open item for the CEO report: the ticket and `S-006` should be
corrected so no later reader re-derives a false overrun from them.

## 2.6 The L3 versus L1/L2 split — NOT measurable in Wave 2, and why that matters

The approved ledger assumes **290,000 input at L3 and 87,000 at L1/L2**, and the whole
cost-control thesis — route each task to the cheapest model that holds the quality floor — rests
on that ratio being roughly right. It is the most useful breakdown the metering could produce.

**It cannot be produced in Wave 2. Recorded as a negative result rather than left blank.**

**Reason, verified rather than assumed.** All twelve registered framework agents declare
`model: inherit` in their host adapter files (`.omn-agent/agents/*.agent.md`, line 5 of each).
Every phase therefore executes at the orchestrating session's model. There is no tier
assignment, no routing decision and no per-task model selection anywhere in the framework as
installed, so there is no split to measure — the denominator does not exist.

**Three consequences, and the third is the one that matters.**

1. **Every framework phase runs at the most capable and most expensive tier available**, because
   `inherit` resolves to whatever the operator happens to be running. Nothing routes a cheap
   task to a cheap model.
2. **The measured development cost in §2.2 is therefore an upper bound**, not a representative
   figure. A tiered framework would cost less for the same work, by an amount nobody can yet
   state.
3. **The cost-control thesis is untested, not merely unmeasured.** Wave 1's decision record
   D-012 specifies that a capability request names "a capability class, a reasoning tier, a
   quality floor… and never a provider or a model", and the resolution boundary M-001 is what
   reads that tier and routes accordingly. **That boundary is designed but not executing**, so
   the ratio the budget depends on has never been exercised by anything.

**[FINDING for the CEO report.]** The 290k/87k split is an assumption carrying the company's
entire cost-control argument, and Wave 2 establishes that **nothing in the system currently
tests it**. This is not a defect in the framework — the framework builds software, it is not the
production pipeline — but it does mean the first real evidence for or against the routing design
arrives only when M-001 is executing and recording tier per operation. Until then the $2.647
per-video figure rests on an unexercised assumption, and that should be said plainly rather than
carried as though it were measured.

## 3. Quantities the finished video commissions

**Populated 2026-09-26 from the committed script and shot list of item `MC3-ITEM-001`.** Every
count below is exact and first-hand: each is a property of an artifact in `wave-2/item-001/` that
can be recounted by anyone holding this repository.

| Quantity | Count | Unit price (re-fetch date) | Cost | Basis |
|---|---|---|---|---|
| Narration characters | **11,096** | $0.05 / 1,000 chars (re-verified 2026-09-26, `reverification-2026-09-26.md` U-5) | **$0.554800** | **MEASURED** — exact count of `wave-2/item-001/narration.txt` |
| Original motion graphics | **22** | in-house, no marginal API cost | **$0.00** | **MEASURED** — enumerated in `shot-list.md` §1 |
| Licensed stock clips | **18** | included in the $30 Storyblocks subscription | **$0.00 marginal** | **MEASURED** — enumerated in `shot-list.md` §2 |
| AI-generated video seconds | **0** | n/a | **$0.00** | **MEASURED — none commissioned.** See note below. |
| Thumbnail images | **6** | $0.05268 each, gpt-image-2.5 high (re-verified 2026-09-26, U-4) | **$0.316080** | **MEASURED** — enumerated in `shot-list.md` §4 |
| | | **Commissioned-media subtotal** | **$0.870880** | |

**Three notes on how to read these figures.**

**1. The narration count is characters, not bytes.** The file is 11,130 bytes and 11,096
characters; the difference is multi-byte punctuation. The metered unit is the character, so
11,096 is the figure, and quoting the byte count would overstate the cost by 0.3%.

**2. `$0.00` on graphics and clips is a marginal cost, not a free lunch.** The 22 graphics are
rendered from the item's own specification, and `render` and `encode` are members of the
rule-determined set, so they carry zero capability cost by the dependency-direction property
rather than by assertion. The 18 clips are covered by the $30/month standing subscription in §4;
they add nothing per item, which is exactly what a standing line is for. **No second library was
used, so the unauthorised $16.50 Envato line is not incurred.**

**3. This subtotal is the media half of the item's cost only.** The token half is not measurable
at billed level — see §1 and §2.6 — so the item's ledger is **incomplete by construction**, and
`A-015` is satisfied for three of its four quantities and not the fourth. That is recorded as a
gap, not netted away.

### The three quantities that could NOT be measured, recorded rather than left blank

| Quantity | Why not obtained | What would obtain it |
|---|---|---|
| Billed input/output tokens for item production | The harness exposes context size, not cumulative billed tokens per request; and phases 4–5 mix capability-building with item production, so any split is a judgement call rather than a measurement (correction `C-002`) | Production routed through `M-001`/`M-005`, recording one operation per attempt with its applied price row |
| Owner-approval elapsed minutes | The item has not reached the gate — it is held short of publish-ready by three stages | The item reaching publish-ready and an approval being taken on it |
| Committed libraries' actual holdings | No clip count was obtained; an authenticated subscription is required and this role may not reach the library directly | A literal single-token query inside an authenticated session via the resolution boundary |

**None of these three has been estimated, extrapolated or inferred.** An invented supply figure in
particular would silently defeat `A-024` to `A-026`, and acting on unverified counts is the
documented route by which this format publishes generated animals as documentary evidence.

### 3.1 The comparison (`A-016`), against O-002 and not against the superseded figure

Every figure labelled, as the criterion requires.

| Figure | Amount | Label |
|---|---|---|
| Commissioned media, this item | **$0.870880** | **MEASUREMENT** — exact counts at re-verified unit prices |
| Item token cost | **not measurable** | Absent, with its reason recorded above — not zero |
| **Item total** | **≥ $0.870880** | **FLOOR, not a total** — the media half only |
| Approved per-item variable cost, option O-002 | $2.647440 | ESTIMATE — 377,000 input / 33,000 output tokens |
| Approved all-in per video at 13/month | $5.95 | ESTIMATE |
| Monthly envelope | $77.41 | ESTIMATE — unchanged, $34.42 metered + $42.99 standing |
| Superseded per-ten-minute figure | $1.5786 | **SUPERSEDED** — 295,000/21,500 tokens, covering research, scripting, fact-check, SEO and QC only |

**The variance cannot honestly be stated.** A variance needs a total on both sides, and the
measured side is a floor. What can be said is that the **media half alone consumes $0.870880 of
the $2.647440 approved per-item variable cost — 32.9% of it** — which leaves $1.776560 for the
token half before the approved figure is breached.

**The escalation test stands and is not yet answerable.** The trigger that matters is measured
per-item *input tokens* materially above 377,000. Nothing here answers it, because the token half
is unmeasured. Note also that the 290,000/87,000 L3-to-L1/L2 split underneath the $2.647440 figure
is itself an **assumption, not a measurement** (correction `C-003`), so both sides of this
comparison are softer than they look and both are labelled accordingly.

**Why the comparison is not run against $1.58.** Correction `C-001`: the superseded figure covers a
narrower scope, and measuring against it would report a roughly 68% overrun that exists only
because the wrong target was quoted. `S-006` and the ticket still carry the stale figure and are
deliberately not rewritten, because they passed their gate and a recorded correction beside a
gated artifact is worth more than a clean-looking artifact carrying an untraceable edit.

**Why the AI-video line is zero, and why that is a decision rather than an omission.** D-006
permits sparing AI cutaways. This video commissions none, for two reasons that are stronger
than cost. First, §3.4 of the niche analysis established that AI-generated "wildlife" is the
specific way this format fails — publishing a synthetic animal as documentary evidence is a
credibility failure on a science channel, not a budget overrun. Second, it keeps the
synthetic-media disclosure determination clean: YouTube's disclosure trigger includes *"AI
generated extra footage of a real place"*, so commissioning zero generated footage removes the
only element of this production that would have required disclosure on that ground. **The
narration is synthetic but sits on the other side of the line** — it makes no real person appear
to say anything and depicts no event that did not occur. That determination is owed as evidence
at `A-023`, not assumed here.

## 4. Standing monthly envelope, unchanged

| Line | Monthly | Status |
|---|---|---|
| Storyblocks Unlimited All Access | $30.00 | Committed |
| Uppbeat | $6.99 | Committed |
| Remainder for all AI text, narration and imagery | ~$40.42 | |
| **Approved envelope** | **$77.41** | **Unchanged. The +$16.50 Envato second library is NOT authorised.** |

**RK-002 stands:** every unit price is re-fetched immediately before it is applied, and the
re-fetch date is recorded in the table above. Next scheduled policy re-verification: 2026-10-26.
