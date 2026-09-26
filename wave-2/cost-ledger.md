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

| # | Point in the run | Tokens in context | Delta | Basis |
|---|---|---|---|---|
| T-1 | Required reading complete; phase 1 dispatched | 153,003 | — | MEASURED |

## 3. Quantities the finished video commissions

Populated once the script and shot list exist.

| Quantity | Count | Unit price (re-fetch date) | Cost | Basis |
|---|---|---|---|---|
| Narration characters | _pending script_ | | | |
| Original motion graphics | _pending shot list_ | | | |
| Licensed stock clips | _pending shot list_ | included in subscription | $0.00 marginal | |
| AI-generated video seconds | **0** | n/a | **$0.00** | Decided: none commissioned |
| Thumbnail images | _pending_ | | | |

## 4. Standing monthly envelope, unchanged

| Line | Monthly | Status |
|---|---|---|
| Storyblocks Unlimited All Access | $30.00 | Committed |
| Uppbeat | $6.99 | Committed |
| Remainder for all AI text, narration and imagery | ~$40.42 | |
| **Approved envelope** | **$77.41** | **Unchanged. The +$16.50 Envato second library is NOT authorised.** |

**RK-002 stands:** every unit price is re-fetched immediately before it is applied, and the
re-fetch date is recorded in the table above. Next scheduled policy re-verification: 2026-10-26.
