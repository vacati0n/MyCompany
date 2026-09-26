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
| 2 `execution-planning` | `planner` | _running_ | | | |
| 3 `solution-design-and-risk-assessment` | `architect` | | | | |
| 4 `implementation` | `omn-dev-1-implement` | | | | |
| 5 `quality-review` | `omn-dev-2-reviewer` | | | | |
| 6 `documentation-and-release-handoff` | `omn-documentation` | | | | |
| | **Total** | | | | |

**[EARLY FINDING — and it is the uncomfortable one.]** Phase 1 alone consumed **257,876 tokens**
to produce a scope definition. The carried estimate of **~USD 1.58 for a ten-minute video**
covers the *content* generation — script text, narration characters, images. It does not cover
the **framework's own orchestration overhead**, which is what this row measures, and on this
evidence that overhead is not a rounding error against the content cost. Six phases at this
order of magnitude is a materially different number from $1.58, and the comparison the ticket
asks for must be stated on both bases or it will mislead:

- **Cost per video of the content itself** — what $1.58 estimated, and what recurs per video.
- **Cost per video of running the governed pipeline** — the six-phase framework overhead, which
  is what a *governed* video costs and which the estimate never included.

The second is the number that decides whether 13 videos/month fits USD 77.41. **It is not yet
established and must not be asserted until phase 6 closes the total.** Recorded now so the
finding is not reverse-engineered later.

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

**The comparison that matters, stated now so it is not constructed to fit the answer later.**
The estimate budgets **316,500 tokens in total** for the entire content pipeline — research,
scripting, fact-check, SEO and QC combined. **Phase 1 of this run consumed 257,876 tokens to
produce a scope definition**, which is 81% of that whole-pipeline budget for a single governance
phase that writes no script and no narration.

These are not the same quantity and must not be netted against each other:

- The estimate prices the **content work** — the tokens that turn a subject into a script.
- The measurement prices the **governance work** — the tokens the framework spends deciding
  what to build, bounding it, planning it, designing it, reviewing it and documenting it.

Wave 2 is the first time anyone has measured the second. **The honest reading available so far
is that the estimate was never wrong about content; it was silent about governance.** Whether
that silence matters depends on the total, which is not yet in, and on how much of the
governance cost is a one-off for the first video versus recurring for the thirteenth. **Both
questions stay open until phase 6.**

## 3. Quantities the finished video commissions

Populated once the script and shot list exist.

| Quantity | Count | Unit price (re-fetch date) | Cost | Basis |
|---|---|---|---|---|
| Narration characters | _pending script_ | $0.05 / 1,000 chars (re-verified 2026-09-26, `reverification-2026-09-26.md` U-5) | | |
| Original motion graphics | _pending shot list_ | in-house, no marginal API cost | | |
| Licensed stock clips | _pending shot list_ | included in the $30 Storyblocks subscription | $0.00 marginal | |
| AI-generated video seconds | **0** | n/a | **$0.00** | **MEASURED — none commissioned.** See note below. |
| Thumbnail images | _pending_ | $0.05268 each, gpt-image-2.5 high | | |

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
