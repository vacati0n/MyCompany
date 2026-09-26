# AI CAPACITY & COST DOSSIER

**Purpose:** evidence base for choosing the AI provider stack of an AI-operated video media company.
**Verification date for every price in this document: 2026-09-18.**
**Prepared by:** research agent, via live first-party page fetches.

---

## 0. How to read this document

Every factual statement carries one of three tags:

| Tag | Meaning |
|---|---|
| **[OFFICIAL]** | Taken from a first-party vendor page (pricing page, docs, or legal terms) fetched live on 2026-09-18. URL given. |
| **[THIRD-PARTY]** | Taken from a non-vendor source (press, aggregator, search snippet). Treat as indicative; re-verify before committing spend. |
| **[INFERENCE/ESTIMATE]** | Derived by this document's arithmetic or judgement. Not a quoted price. |

**Quality tiers** used throughout (a convention for this dossier, not a vendor term) — **[INFERENCE/ESTIMATE]**:

| Tier | Meaning | Typical tasks |
|---|---|---|
| **L0** | Mechanical / deterministic | String formatting, dedup, regex, file moves. No LLM needed. |
| **L1** | Cheap model, low stakes | Tagging, classification, timestamping, keyword extraction, chapter splits. |
| **L2** | Mid model, published-adjacent | SEO packs, descriptions, summarisation, first-draft outlines, QC triage. |
| **L3** | Strong model, on-camera quality | Narration scripting, fact-check reasoning, editorial judgement. |
| **L4** | Frontier model, high stakes | Legal/medical/financial claim review, complex multi-source research, escalated fact disputes. |

**Prices change.** Everything below has a URL. Re-run the fetches before any commitment.

---

## A. Text / reasoning LLM providers

### A.1 Anthropic (Claude)

**[OFFICIAL]** Source: <https://platform.claude.com/docs/en/about-claude/pricing> (fetched 2026-09-18)
**[OFFICIAL]** Source: <https://platform.claude.com/docs/en/about-claude/models/overview> (fetched 2026-09-18)

| Model | API ID | Input $/MTok | Output $/MTok | 5m cache write | 1h cache write | Cache read (hit) | Context | Max output |
|---|---|---|---|---|---|---|---|---|
| Claude Fable 5.1 | `claude-fable-5-1` | $10 | $50 | $12.50 | $20 | $0.25 (0.025x) | 1M | 128K |
| Claude Mythos 5.1 (limited availability) | — | $10 | $50 | $12.50 | $20 | $0.25 (0.025x) | — | — |
| **Claude Opus 5** | `claude-opus-5` | **$5** | **$25** | $6.25 | $10 | $0.50 | 1M | 128K |
| Claude Opus 4.8 / 4.7 / 4.6 / 4.5 | various | $5 | $25 | $6.25 | $10 | $0.50 | 1M (4.6+) | — |
| **Claude Sonnet 5** | `claude-sonnet-5` | **$2** | **$10** | $2.50 | $4 | $0.20 | 1M | 128K |
| Claude Sonnet 4.6 / 4.5 | various | $3 | $15 | $3.75 | $6 | $0.30 | — | — |
| **Claude Haiku 4.5** | `claude-haiku-4-5` | **$1** | **$5** | $1.25 | $2 | $0.10 | 200K | 64K |

Notes, all **[OFFICIAL]** from the pricing page:

- **Sonnet 5 pricing is now permanent.** Quote: *"The $2/$10 per million input/output token pricing for Claude Sonnet 5, announced at launch as introductory pricing through August 31, 2026, is now the standard price. The previously scheduled increase to $3/$15 per million input/output tokens on September 1, 2026 will not occur."*
- **Batch API = 50% off both input and output.** Opus 5 batch: $2.50 in / $12.50 out. Sonnet 5 batch: $1 in / $5 out. Haiku 4.5 batch: $0.50 in / $2.50 out.
- **Caching multipliers:** 5-minute write 1.25x base input; 1-hour write 2x base input; cache read 0.1x base input (0.025x on Fable 5.1 / Mythos 5.1). *"These multipliers stack with other pricing modifiers, including the Batch API discount."*
- **Long context is not surcharged:** *"Claude 4.6 and later models ... include the full 1M token context window at standard pricing. (A 900k-token request is billed at the same per-token rate as a 9k-token request.)"* This is a material difference from Gemini and matters for research-heavy pipelines.
- **Tokenizer caveat (cost-relevant):** *"Claude 4.7 and later models ... use a newer tokenizer ... This tokenizer produces approximately 30% more tokens for the same text."* So Opus 5 / Sonnet 5 headline per-token prices are **not** directly comparable to Sonnet 4.6-and-earlier per-token prices on the same input text. **[INFERENCE/ESTIMATE]:** effective cost is roughly 1.3x the naive token-count arithmetic when migrating from a 4.6-era model.
- **Web search server tool:** $10 per 1,000 searches, plus token cost of the retrieved content. **Web fetch tool: no additional charge** beyond tokens.
- **Fast mode** (research preview, Opus 5 / Opus 4.8 only): $10 in / $50 out per MTok. Not available with Batch.
- **US-only data residency** (`inference_geo: "us"`, Claude 4.6+): 1.1x multiplier on all token categories.

**Rate limits** — **[OFFICIAL]** <https://platform.claude.com/docs/en/api/rate-limits>

| Tier | Monthly spend cap | Opus 5 / Sonnet 5 / Haiku 4.5 RPM | ITPM | OTPM |
|---|---|---|---|---|
| Start | $500 | 1,000 | 2,000,000 | 400,000 |
| Build | $1,000 | 5,000 | 5,000,000 | 1,000,000 |
| Scale | $200,000 | 10,000 | 10,000,000 | 2,000,000 |
| Custom | no cap | by arrangement | — | — |

Critically for a high-volume pipeline: *"For most Claude models, only uncached input tokens count toward your ITPM rate limits"* — `cache_read_input_tokens` do **not** count. **[OFFICIAL]** This roughly multiplies usable throughput by the cache hit rate.

Batch API limits (shared across models): Start 200,000 requests in queue; Build 300,000; Scale 500,000; max 100,000 requests per batch. **[OFFICIAL]**

**Output ownership (API / commercial):** **[OFFICIAL]** <https://www.anthropic.com/legal/commercial-terms> — *"Customer (a) retains all rights to its Inputs, and (b) owns its Outputs. Anthropic disclaims any rights it receives to the Customer Content under these Terms."* and *"Subject to Customer's compliance with these Terms, Anthropic hereby assigns to Customer its right, title and interest (if any) in and to Outputs."* The Commercial Terms also expressly contemplate using the Services *"to power products and services Customer makes available to its own customers and end users."*

### A.2 OpenAI

**[OFFICIAL]** Source: <https://developers.openai.com/api/docs/pricing> (fetched 2026-09-18)

| Model | Input $/MTok | Cached input $/MTok | Output $/MTok |
|---|---|---|---|
| gpt-6-astra | $10.00 | $1.00 | $50.00 |
| gpt-5.6-sol | $4.00 | $0.40 | $20.00 |
| gpt-5.6-terra | $2.00 | $0.20 | $12.00 |
| **gpt-5.6-luna** | **$0.20** | **$0.02** | **$1.20** |
| gpt-5.5 | $5.00 | $0.50 | $30.00 |
| gpt-5.4 | $2.50 | $0.25 | $15.00 |
| gpt-5.4-mini | $0.75 | $0.075 | $4.50 |
| gpt-5 | $1.25 | $0.125 | $10.00 |
| o3 | $2.00 | $0.50 | $8.00 |
| o3-mini | $1.10 | $0.55 | $4.40 |
| o1 | $15.00 | $7.50 | $60.00 |

**Context / output limits** — **[OFFICIAL]** <https://developers.openai.com/api/docs/models>: gpt-6-astra, gpt-5.6-sol, gpt-5.6-terra and gpt-5.6-luna all have a **1.05M** context window and **128K** max output tokens.

**Batch API** — **[OFFICIAL]** <https://developers.openai.com/api/docs/guides/batch>: *"50% cost discount compared to synchronous APIs"*, completion window 24h — *"Each batch completes within 24 hours (and often more quickly)."*

**Cached input** is already priced as a separate column (10% of input price on the 5.6/6 line) — **[OFFICIAL]**, same pricing page.

**Regional/data-residency endpoints:** 10% uplift for eligible models released after 2026-03-05 — **[OFFICIAL]**, same pricing page.

**Rate limits / usage tiers** — **[OFFICIAL]** <https://developers.openai.com/api/docs/guides/rate-limits>:

| Tier | Qualification | Monthly usage limit |
|---|---|---|
| Free | user in allowed geography | $100 |
| Tier 1 | "$5 paid" | $100 |
| Tier 2 | "$50 paid" | $500 |
| Tier 3 | "$100 paid" | $1,000 |
| Tier 4 | "$250 paid" | $5,000 |
| Tier 5 | "$1,000 paid" | "$200,000 / month" |

**Could not verify:** per-model RPM/TPM numbers. The docs state *"Rate limits vary by the model being used"* and direct you to your account settings rather than publishing a table. Do not plan throughput off a remembered number — check the console.

**Output ownership:** **[OFFICIAL]** <https://openai.com/policies/row-terms-of-use/> (Terms of Use, effective January 1, 2026) — *"you (a) retain your ownership rights in Input and (b) own the Output. We hereby assign to you all our right, title, and interest, if any, in and to Output."* Note the immediately following caveat: *"Due to the nature of our Services and artificial intelligence generally, output may not be unique and other users may receive similar output ... Our assignment above does not extend to other users' output."*

### A.3 Google Gemini

**[OFFICIAL]** Source: <https://ai.google.dev/gemini-api/docs/pricing> (fetched 2026-09-18)

| Model | Input $/MTok | Output $/MTok | Context cache $/MTok | Batch |
|---|---|---|---|---|
| Gemini 3.8 / 3.7 / 3.6 Flash | $0.75 (rises to $1.50 on 2027-01-01) | $3.75 (rises to $7.50) | $0.075 (rises to $0.15) | 50% off |
| Gemini 3.5 Flash | $1.50 | $9.00 | $0.15 | $0.75 in / $4.50 out |
| Gemini 3.5 Flash-Lite | $0.30 | $2.50 | $0.03 | $0.15 in / $1.25 out |
| Gemini 3.1 Flash-Lite | $0.25 (text/image/video), $0.50 (audio) | $1.50 | $0.025 | $0.125 in / $0.75 out |
| Gemini 3.1 Pro Preview | $2.00 (≤200k) / $4.00 (>200k) | $12.00 (≤200k) / $18.00 (>200k) | $0.20 / $0.40 | $1.00–$2.00 in / $6.00–$9.00 out |
| Gemini 2.5 Pro | $1.25 (≤200k) / $2.50 (>200k) | $10.00 / $15.00 | $0.125 / $0.25 | $0.625–$1.25 in / $5.00–$7.50 out |
| Gemini 2.5 Flash | $0.30 (text/img/video), $1.00 (audio) | $2.50 | $0.03 / $0.10 | $0.15–$0.50 in / $1.25 out |
| **Gemini 2.5 Flash-Lite** | **$0.10** (text/img/video), $0.30 (audio) | **$0.40** | $0.01 / $0.03 | **$0.05 in / $0.20 out** |

Notes, all **[OFFICIAL]** from that page:

- **Batch mode: 50% off standard pricing.**
- **Long context IS surcharged** on Pro-class models: >200k input tokens roughly doubles the input rate and raises output ~1.5x. Contrast with Claude, which does not surcharge. This matters for research-heavy prompts.
- **Cache storage is billed by time**, unlike Anthropic: e.g. $1.00/MTok/hour on Flash-class, $4.50/MTok/hour on 2.5 Pro / 3.1 Pro. Budget for this if you keep large caches warm.
- **Priority tier: 1.8x standard rates** across all models.
- **Google Search grounding tool:** 5,000 free requests/month on Gemini 3.x, then **$14 per 1,000 requests**. (More expensive per search than Anthropic's $10/1,000.)
- **Price increase scheduled 2027-01-01** on the 3.6/3.7/3.8 Flash line (doubling). Budget for it.

**Rate limits** — **[OFFICIAL]** <https://ai.google.dev/gemini-api/docs/rate-limits>. **Could not verify per-model RPM/TPM/RPD:** Google does not publish the numbers on that page (*"Rate limits depend on a variety of factors (such as your usage tier) and can be viewed in Google AI Studio"*). What IS published:

| Tier | Qualification | Spend cap |
|---|---|---|
| Free | "Active project or free trial" | — |
| Tier 1 | "Set up and link an active billing account" | $250; $10 per 10 minutes |
| Tier 2 | "Paid $100 + 3 days from first successful payment" | $2,000; $50 per 10 minutes |
| Tier 3 | "Paid $1,000 + 30 days from first successful payment" | $20,000–$100,000+; $200 per 10 minutes |

**Output ownership and data use** — **[OFFICIAL]** <https://ai.google.dev/gemini-api/terms>:
- *"Google won't claim ownership over that content. You acknowledge that Google may generate the same or similar content for others and that we reserve all rights to do so."*
- **Unpaid Services:** *"Google uses the content you submit ... to provide, improve, and develop Google products and services."*
- **Paid Services:** *"Google doesn't use your prompts ... or responses to improve our products."*
- Scope restriction worth noting: *"Use of Google AI Studio and Gemini API is for developers building with Google AI models for professional or business purposes, not for consumer use."*

**[INFERENCE/ESTIMATE] — operational implication:** if the company uses the Gemini **free tier** for any production work, its scripts and research prompts are training data. For a media company whose script bank is the main asset, free-tier Gemini should be barred from anything proprietary.

### A.4 Low-cost / open-weight-hosted options

#### DeepSeek — **[OFFICIAL]** <https://api-docs.deepseek.com/quick_start/pricing/>

| | `deepseek-flash` (V4.1-Flash) | `deepseek-v4-pro` (V4-Pro-0813) |
|---|---|---|
| Context | 1M tokens | 1M tokens |
| Max output | 384K | 384K |
| Input, cache **hit**, off-peak | **$0.003** /MTok | $0.022 /MTok |
| Input, cache hit, peak | $0.006 | $0.044 |
| Input, cache **miss**, off-peak | **$0.15** | $0.66 |
| Input, cache miss, peak | $0.30 | $1.32 |
| Output, off-peak | **$0.60** | $1.98 |
| Output, peak | $1.20 | $3.96 |

Off-peak = 50% of peak, applied automatically. Peak is **01:00–04:00 and 06:00–10:00 UTC, Mon–Fri**; everything else is off-peak — i.e. off-peak is the majority of the week, which suits an overnight batch pipeline. Cache hit is a **50x** discount on input, the single largest cost lever found anywhere in this dossier. **Rate limits are expressed as concurrency, not RPM/TPM** — `deepseek-flash` 2,500 concurrent, `deepseek-v4-pro` 500 concurrent, per account regardless of key count (<https://api-docs.deepseek.com/quick_start/rate_limit>). **No separate batch-API discount was found on the official pricing page — do not assume one exists.**

#### Alibaba Qwen (Model Studio, Singapore/international) — **[OFFICIAL]** <https://www.alibabacloud.com/help/en/model-studio/model-pricing>

| Model | Input $/MTok | Output $/MTok |
|---|---|---|
| **Qwen3.7-Flash** | **$0.03 – $0.20** (tiered 0–32K / 32K–256K / 256K–1M) | **$0.13 – $0.80** |
| Qwen-Flash | $0.05 – $0.25 | $0.40 – $2.00 |
| Qwen3.7-Plus | $0.40 – $1.20 | $1.60 – $4.80 |
| Qwen3-Max | $1.20 – $3.00 | $6.00 – $15.00 |
| Qwen3.8-Max (flagship) | $2.00 | $6.00 |

Cache hit billed at **10%** of standard input; explicit cache *creation* at 125%. **Batch = 50%** of real-time on both directions. Free quota 1M tokens per model for 90 days (Singapore endpoint). **Trap:** tiering is applied to the *whole* request — a 260K-token prompt reprices every token at the top tier, not just the overflow.

#### Mistral — **[OFFICIAL]** <https://mistral.ai/pricing/api>

| Model | Input $/MTok | Output $/MTok |
|---|---|---|
| **Ministral 3 (3B)** | **$0.10** | **$0.10** |
| Ministral 3 (8B) | $0.15 | $0.15 |
| **Mistral Small 4** | **$0.15** | **$0.60** |
| Mistral Large 3 | $0.50 | $1.50 |
| Mistral Medium 3.5 | $1.50 | $7.50 |

Cached input **−90%**; **batch = half price**. Also relevant to this company: **Voxtral TTS $0.016 / 1,000 characters** and OCR 4.1 at $4 / 1,000 pages. **Could not verify** Mistral context windows or rate limits — they are not on the pricing page.

#### Groq — **[OFFICIAL]** <https://console.groq.com/docs/models>

| Model | Context | Speed | Input $/MTok | Output $/MTok |
|---|---|---|---|---|
| `openai/gpt-oss-20b` | 131,072 | ~1,000 t/s | **$0.075** | **$0.30** |
| `openai/gpt-oss-120b` | 131,072 | ~500 t/s | **$0.15** | **$0.60** |
| `qwen/qwen3.8-27b` (preview) | 131,042 | ~450 t/s | $0.80 | $4.00 |
| `llama-3.1-8b-instant` | 131,072 | ~560 t/s | **"Contact Sales"** | **"Contact Sales"** |
| `llama-3.3-70b-versatile` | 131,072 | ~280 t/s | **"Contact Sales"** | **"Contact Sales"** |
| Whisper Large V3 Turbo | — | — | $0.04 / hour of audio | — |

**Could not verify:** Groq has removed public per-token pricing for the Llama models. Widely-quoted figures (Llama 3.1 8B at $0.05/$0.08; Llama 3.3 70B at $0.59/$0.79) are **[THIRD-PARTY]** and should be treated as stale. **Batch: 50% discount** with a configurable 24-hour-to-7-day window (**[OFFICIAL]** <https://console.groq.com/docs/batch>). Free-tier rate limits are tight — e.g. `gpt-oss-120b` at 30 RPM / 8K TPM / 1K RPD / 200K TPD (**[OFFICIAL]** <https://console.groq.com/docs/rate-limits>); developer-tier numbers did not render and are **unverified**.

#### Together AI — **[OFFICIAL]** <https://www.together.ai/pricing>

| Model | Input $/MTok | Output $/MTok |
|---|---|---|
| Llama 3.1 8B Instruct Lite | $0.14 | $0.14 |
| gpt-oss-120B | $0.15 | $0.60 |
| Qwen3.8 Flash | $0.15 | $0.47 |
| DeepSeek V4.1 Flash | $0.30 | $1.20 |
| Llama 3.3 70B | $1.04 | $1.04 |

Note Together resells DeepSeek V4.1 Flash at **2x DeepSeek's own off-peak input price** — go direct. Batch: *"up to 50% off serverless rates"*, 24h best-effort, **but only on selected models** (**[OFFICIAL]** <https://docs.together.ai/docs/batch-inference>). Together publishes **no fixed rate limits**: *"Dynamic rate limits adjust with usage"* — you must read the `x-ratelimit-reset` header (**[OFFICIAL]** <https://docs.together.ai/docs/rate-limits>).

#### OpenRouter (aggregator) — **[OFFICIAL]** <https://openrouter.ai/models?order=pricing-low-to-high>, <https://openrouter.ai/docs/faq>

Cheapest open-weight entries today: Inference.net Schematron V2 Turbo $0.03 in / $0.15 out (128K); Schematron V2 Small $0.05 / $0.23; DeepSeek V4.1 Flash $0.15 / $0.60 (1.05M). **Caveat:** the Schematron models are purpose-built HTML-to-JSON extractors, **not** general chat models — do not read them as general-purpose $0.03 models. OpenRouter takes **no markup on usage** but charges **5.5% on Stripe credit purchases** ($0.80 min) / 5% USDC, and 5% on BYOK above the monthly allowance.

#### Z.ai / GLM — **[OFFICIAL]** <https://docs.z.ai/guides/overview/pricing>

| Model | Input $/MTok | Cached input | Output $/MTok |
|---|---|---|---|
| **GLM-4.7-Flash** | **FREE** | — | **FREE** |
| GLM-4.7-FlashX | $0.07 | $0.01 | $0.40 |
| GLM-5.3-Flash | $0.15 | $0.03 | $0.50 |
| GLM-5.3 / 5.2 | $1.40 | $0.26 | $4.40 |

The free Flash tier is the cheapest credible general-purpose option found. Context windows are not published per-model (only GLM-4-32B-0414-128K states 128K) — **unverified**.

#### Moonshot / Kimi — **[OFFICIAL]** <https://platform.kimi.ai/docs/pricing/chat>

`kimi-k3`: 1,048,576 context; input $0.30 cache-hit / $3.00 cache-miss; output **$15.00**. Not a low-cost option at the frontier tier — k3 output is 25x `deepseek-flash`.

#### Cheapest-output league table — **[INFERENCE/ESTIMATE]** (compiled from the [OFFICIAL] figures above)

| Rank | Provider / model | Output $/MTok | With batch |
|---|---|---|---|
| 0 | Z.ai GLM-4.7-Flash | $0.00 | — |
| 1 | Mistral Ministral 3 (3B) | $0.10 | **$0.05** |
| 2 | Alibaba Qwen3.7-Flash (0–32K tier) | $0.13 | **$0.065** |
| 3 | Groq gpt-oss-20b | $0.30 | $0.15 |
| 4 | Z.ai GLM-4.7-FlashX | $0.40 | — |
| 5 | Groq / Together gpt-oss-120b | $0.60 | $0.30 (Groq) |
| 6 | DeepSeek-flash, off-peak | $0.60 | no batch tier |

**Realistic floor for a credible general-purpose model is ~$0.05–0.15 per 1M output tokens.** For an L1/L2 workload this is effectively free relative to TTS and imagery.

### A.5 Web-search cost (a real line item for a research pipeline)

| Provider | Price | Tag | Source |
|---|---|---|---|
| Anthropic web search tool | $10 / 1,000 searches (+ token cost of results) | [OFFICIAL] | <https://platform.claude.com/docs/en/about-claude/pricing> |
| Anthropic web fetch tool | $0 extra (tokens only) | [OFFICIAL] | same |
| Google Search grounding | 5,000 free/mo (Gemini 3.x), then $14 / 1,000 | [OFFICIAL] | <https://ai.google.dev/gemini-api/docs/pricing> |
| Brave Search API | "$5 per 1,000 requests"; "$5 in free monthly credits" | [OFFICIAL] | <https://brave.com/search/api/> |
| Exa search | "$7 / 1k requests" standard; deep-lite/deep "$12 / 1k"; deep-reasoning "$15 / 1k"; contents "$1 / 1k pages, per content type" | [OFFICIAL] | <https://exa.ai/pricing> |

**[INFERENCE/ESTIMATE]:** Brave at $5/1k is the cheapest credible general search API here, roughly half Anthropic's built-in tool. For a 10-video-per-day operation doing ~10 searches per video, the difference is ~$15/month — real but not decisive. The bigger cost driver is the **tokens** of fetched pages, not the search calls.

---

## B. Consumer subscriptions vs API

### B.1 The plans and what they cost

**Anthropic / Claude** — **[OFFICIAL]** <https://claude.com/pricing>, <https://support.claude.com/en/articles/8325606-what-is-the-pro-plan>, <https://support.claude.com/en/articles/11049741-what-is-the-max-plan>

| Plan | Price | Stated usage |
|---|---|---|
| Free | $0 | Basic web/desktop/mobile chat |
| Pro | $17/mo annual, **$20/mo monthly** | *"More usage per session than the Free plan"*; session limit *"will reset every five hours"*; *"a weekly usage limit that applies across all models"* |
| Max 5x | **$100/mo** | *"five times the Pro plan's per-session usage allowance"* |
| Max 20x | **$200/mo** | *"20 times the Pro plan's per-session usage allowance"* |
| Team (standard seat) | $20/seat/mo annual, $25 monthly | More than Pro |
| Team (premium seat) | $100/seat/mo annual, $125 monthly | *"5x more usage than standard seats"* |
| Enterprise | $20/seat/mo annual **plus usage at API rates** | Custom spend limits |

Anthropic publishes **no message or token counts**. The Pro page also reserves discretion: additional capacity measures *"such as weekly and monthly caps or model and feature usage, at our discretion."* **[OFFICIAL]**

**OpenAI / ChatGPT** — **[OFFICIAL]** <https://help.openai.com/en/articles/6950777-what-is-chatgpt-plus>, <https://help.openai.com/en/articles/9793128-what-is-chatgpt-pro>

| Plan | Price | Stated usage |
|---|---|---|
| Free | $0 | — |
| Go | $8/mo | **[THIRD-PARTY]** — not verified on a first-party page in this pass |
| Plus | **$20/mo** | *"a subscription plan that provides enhanced access to the ChatGPT web app for $20/month"*; *"Plus subscriptions may include usage limits such as message caps, especially during high demand. These limits may vary based on system conditions."* |
| Pro $100 (Pro 5X) | **$100/mo** | *"Pro $100 unlocks 5x higher usage than Plus"* |
| Pro $200 (Pro 20X) | **$200/mo** | *"Pro $200 unlocks 20x usage than Plus"* |

**Material operational fact, [OFFICIAL]:** *"As of September 10, 2026, we're temporarily pausing new sign-ups and upgrades to the ChatGPT Pro $200 plan (Pro 20X). This includes sign-ups and upgrades from Free, Go, Plus, or Pro $100."* Also: *"Currently, we do not support annual billing."* A capacity plan that assumes you can buy Pro 20X seats today is invalid.

**Google** — **[OFFICIAL]** <https://gemini.google/subscriptions/>, <https://one.google.com/about/google-ai-plans/>

The first-party pages geolocate. The fetch on 2026-09-18 returned **Vietnam pricing**, which is what I can honestly cite as first-party:

| Plan | Price (VND, [OFFICIAL] VN storefront) | Price (USD, [THIRD-PARTY]) | Gemini usage | Google Flow credits/mo |
|---|---|---|---|---|
| Free | ₫0 | $0 | baseline | — |
| Google AI Plus | ₫132,000/mo | ~$4.99 | "2x" free tier | 200 |
| Google AI Pro | ₫489,000/mo | ~$19.99 | "4x" free tier | 1,000 |
| Google AI Ultra 5x | ₫2,250,000/mo | ~$99.99 | "5x Pro" | 10,000 |
| Google AI Ultra 20x | ₫5,500,000/mo | ~$199.99 | "20x Pro" | 25,000 |

USD figures are **[THIRD-PARTY]** (search results incl. Engadget and aggregator pages) — **I could not load a US-locale Google storefront to confirm them.** Treat USD as indicative.

**[OFFICIAL]** from the Google One comparison table: all paid tiers list a **1M token context window**; video generation on every consumer tier is **Gemini Omni Flash** (not the top Veo/Omni model); Flow Music credits are 3,000 / 10,000 / 30,000 / 30,000 per month by tier. AI credits can be topped up: *"If you reach your plan's limit, Google AI Pro and Google AI Ultra members can purchase AI credits to get extra usage in Google Flow and Google Antigravity"* and *"AI credits are deducted based on standard API pricing for the specific model and complexity of request."* **[OFFICIAL]** <https://support.google.com/googleone/answer/14534406>

### B.2 The decisive question: may a subscription be used as production capacity?

Short answer, developed in full in **Section E (Terms-of-Service findings)**: **no for Anthropic consumer plans, no for ChatGPT consumer plans, and unresolved-but-unfavourable for Google consumer plans.** All three sets of terms prohibit programmatic access or programmatic extraction of output from the consumer products. The API is the only compliant production path for text/reasoning.

**[INFERENCE/ESTIMATE] — the arbitrage that does not exist.** A Claude Max 20x seat is $200/month. If a subscription could be driven programmatically at, say, 40M Sonnet-5-equivalent input tokens and 3M output tokens a month, that same volume on the API would cost 40 × $2 + 3 × $10 = **$110** — *less* than the subscription. Subscriptions only look cheap at the very top of their allowance with frontier models; at realistic media-pipeline volumes the API is often cheaper anyway, so the ToS restriction costs the company little and removes an existential account-termination risk. This is the single most important finding in the dossier: **there is no meaningful cost reason to take the ToS risk.**

---

## C. Media generation costs

### C.1 Text-to-speech / voice cloning

#### Price per unit

| Provider / voice tier | Price | Unit | Tag | Source |
|---|---|---|---|---|
| **Google Cloud TTS — Standard** | **$4.00** (first **4M chars/month free**) | per 1M characters | [OFFICIAL] | <https://cloud.google.com/text-to-speech/pricing> |
| **Google Cloud TTS — WaveNet** | **$4.00** (first 4M chars/month free) | per 1M characters | [OFFICIAL] | same |
| Google Cloud TTS — Neural2 | $16.00 (first 1M free) | per 1M characters | [OFFICIAL] | same |
| Google Cloud TTS — Chirp 3 HD | $30.00 (first 1M free) | per 1M characters | [OFFICIAL] | same |
| Google Cloud TTS — Instant custom voice | $60.00 | per 1M characters | [OFFICIAL] | same |
| Google Cloud TTS — Studio | $160.00 | per 1M characters | [OFFICIAL] | same |
| Gemini 2.5 Flash TTS | $0.50 /1M text-in + **$10.00 /1M audio-out tokens** | tokens (25 audio tokens = 1 s ⇒ **≈$0.90 per hour of audio**, [INFERENCE/ESTIMATE]) | [OFFICIAL] | same / <https://ai.google.dev/gemini-api/docs/pricing> |
| Gemini 3.1 Flash TTS / 2.5 Pro TTS | $1.00 /1M in + $20.00 /1M audio-out | ⇒ ≈$1.80 per hour of audio [INFERENCE/ESTIMATE] | [OFFICIAL] | same |
| **OpenAI tts-1** | **$15.00** | per 1M characters | [OFFICIAL] | <https://developers.openai.com/api/docs/pricing> |
| OpenAI tts-1-hd | $30.00 | per 1M characters | [OFFICIAL] | same |
| OpenAI gpt-4o-mini-tts | $0.60 /1M text-in + $12.00 /1M audio-out tokens | tokens | [OFFICIAL] | same |
| **Azure Neural (Standard Voice)** | **$15.00**; free tier 0.5M chars/month | per 1M characters | [OFFICIAL] | <https://azure.microsoft.com/en-us/pricing/details/cognitive-services/speech-services/> |
| Azure Custom Neural Voice — synthesis | $24.00 (neural HD $48.00) | per 1M characters | [OFFICIAL] | same |
| Azure CNV — voice model training | $52 per compute hour, **up to $936 per training** | — | [OFFICIAL] | same |
| Azure CNV — **endpoint hosting** | **$4.04 per model per hour** (≈**$2,949/month** per always-on custom voice, [INFERENCE/ESTIMATE]) | — | [OFFICIAL] | same |
| Azure commitment tiers | 80M chars $960/mo; 400M $3,900/mo; 2,000M $15,000/mo | — | [OFFICIAL] | same |
| **ElevenLabs API — Flash / Turbo** | **$0.05 per 1,000 chars** (= $50/1M) | per 1K characters | [OFFICIAL] | <https://elevenlabs.io/pricing/api> |
| **ElevenLabs API — v3 / v2 Multilingual** | **$0.10 per 1,000 chars** (= $100/1M) | per 1K characters | [OFFICIAL] | same |
| ElevenLabs API — v3 Conversational | $0.05 per 1,000 chars | per 1K characters | [OFFICIAL] | same |
| Mistral Voxtral TTS | $0.016 per 1,000 chars (= $16/1M) | per 1K characters | [OFFICIAL] | <https://mistral.ai/pricing/api> |

**ElevenLabs subscription tiers** — **[OFFICIAL]** <https://elevenlabs.io/pricing>: Free $0 (10,000 credits), Starter **$6** (30,000), Creator **$22** (121,000), Pro **$99** (600,000), Scale **$299** (1.8M), Business **$990** (6M). Annual billing = *"you pay for 10 months (annual price = monthly price × 10)."* At 1 credit per character for V2 Multilingual, **[INFERENCE/ESTIMATE]** effective rates are **$0.165–$0.200 per 1,000 characters** monthly-billed — i.e. **the subscription is 1.7–4x more expensive per character than ElevenLabs' own API.** For a production narration pipeline, buy the API, not credits.

**Feature gating** — **[OFFICIAL]** same page: **Commercial License starts at Starter ($6/mo)**; **Instant Voice Cloning at Starter**; **Professional Voice Cloning at Creator ($22/mo)**; WAV/PCM 44.1 kHz via API at Pro; concurrent requests 2/3/5/10/15/25 by tier.

#### Commercial-use rights and ownership (TTS)

- **ElevenLabs** — **[OFFICIAL]** <https://elevenlabs.io/terms-of-use> §1(c): *"(i) if you access or use our Services free of charge … you may only use the Services for non-commercial purposes; (ii) if you access or use our Services through a paid subscription plan … you may use the Services for commercial purposes."* §4(c): *"as between you and ElevenLabs, you retain all rights in and to your Output."* **But §4(d) is the catch** — you grant ElevenLabs *"a license to use, reproduce, modify, adapt, publish, translate, create derivative works from, distribute … and use your Content to provide the Services … to improve the Services, and to develop new services and products,"* explicitly including your **voice** and *"other indicia of your persona."* **ElevenLabs trains on your scripts and audio by default.** Also §3: *"If your account is closed or terminated, you will forfeit all unused credits."*
- **ElevenLabs Prohibited Use Policy** — **[OFFICIAL]** <https://elevenlabs.io/use-policy> §5 bans replicating another person's voice without consent, and bans use *"in a manner intended to deceive others about whether the voice was generated by artificial intelligence."* §6 bans election/candidate impersonation *"regardless of whether authorization was obtained."*
- **OpenAI** — **[OFFICIAL]** <https://openai.com/policies/business-terms/> §4.1: *"Customer: (a) retains all ownership rights in Input; and (b) owns all Output. OpenAI hereby assigns to Customer all OpenAI's right, title, and interest, if any, in and to Output."* §4.2: *"OpenAI will not use Customer Content to develop or improve the Services, unless Customer explicitly agrees to such use."* This is a genuine differentiator against ElevenLabs. **However** the consumer ToU prohibited-use list includes *"Represent that Output was human-generated when it was not"* — directly relevant to unlabelled AI narration.
- **Google Cloud TTS billing trap** — **[OFFICIAL]**: *"The total number of characters in the input string are counted for billing purposes, including spaces and newline characters. All Speech Synthesis Markup Language (SSML) tags (except the `<mark>` tag) are also included in the character count."* Heavy SSML prosody markup inflates the bill against the raw script length.
- **Azure** — Custom Neural Voice and Personal Voice are **limited-access, approval-gated features**; you cannot simply buy them. **[OFFICIAL]** same pricing page.
- **Could not verify:** ElevenLabs overage / pay-as-you-go rates (help-centre article returns HTTP 403; the PAYG doc says only *"API usage has lower rates than UI usage"* and publishes no numbers); Google Cloud and Azure output-ownership contract language was not fetched and quoted this session.

### C.2 Image generation

| Provider / model | Price per image | Tag | Source |
|---|---|---|---|
| **FLUX.2 [klein] 4B** | **$0.014** (1 MP) | [OFFICIAL] | <https://bfl.ai/pricing> |
| FLUX.2 [klein] 9B | $0.015 (1 MP) | [OFFICIAL] | same |
| FLUX.2 [pro] | $0.03 (1 MP) | [OFFICIAL] | same |
| FLUX.1 [dev] | $0.025 | [OFFICIAL] | same |
| FLUX.1 Kontext [pro] / FLUX 1.1 [pro] | $0.04 | [OFFICIAL] | same |
| FLUX.1 Kontext [max] | $0.08 | [OFFICIAL] | same |
| **OpenAI gpt-image-2.5 (Sunburst/Flare) — low** | **$0.00588** @1024² (196 output tokens) | [OFFICIAL] | <https://developers.openai.com/api/docs/guides/image-generation> (vendor's own calculator) |
| **OpenAI gpt-image-2.5 — medium** | **$0.01317** @1024² (439 tokens) | [OFFICIAL] | same |
| OpenAI gpt-image-2.5 — high | $0.05268 @1024² (1,756 tokens) | [OFFICIAL] | same |
| OpenAI gpt-image-2.5 — xhigh / max | $0.09366 / $0.21072 @1024² | [OFFICIAL] | same |
| **Imagen 4 Fast / Imagen 3 Fast** | **$0.02** | [OFFICIAL] | <https://cloud.google.com/vertex-ai/generative-ai/pricing> |
| Imagen 4 / Imagen 3 | $0.04 | [OFFICIAL] | same |
| Imagen 4 Ultra | $0.06 | [OFFICIAL] | same |
| **Gemini 3.1 Flash Lite Image** | **$0.0336** @1K | [OFFICIAL] | <https://ai.google.dev/gemini-api/docs/pricing> |
| Gemini 3.1 Flash Image (Nano Banana 2) | $0.045 @0.5K · $0.067 @1K · $0.101 @2K · $0.151 @4K | [OFFICIAL] | same |
| Gemini 3 Pro Image (Nano Banana Pro) | $0.134 @1K–2K · $0.24 @4K | [OFFICIAL] | same |
| Gemini 2.5 Flash Image | $0.039 — **deprecated, shuts down 2026-10-02** | [OFFICIAL] | same |
| Stable Diffusion 3.5 Flash | $0.025 (2.5 credits @ $0.01) | [OFFICIAL] | <https://platform.stability.ai/pricing> |
| Stable Image Core / SD 3.5 Medium | $0.03 / $0.035 | [OFFICIAL] | same |
| Stable Image Ultra | $0.08 | [OFFICIAL] | same |
| Adobe Firefly Image 5 | ≈$0.050 on Pro Plus (10 credits @ $0.005) — **[INFERENCE/ESTIMATE]** from official credit costs | [OFFICIAL] inputs | <https://www.adobe.com/products/firefly/plans.html>, <https://helpx.adobe.com/creative-cloud/apps/generative-ai/generative-credits-faq.html> |
| Adobe Firefly Image 4 Ultra | ≈$0.100 on Pro Plus (20 credits) — **[INFERENCE/ESTIMATE]** | [OFFICIAL] inputs | same |
| **Midjourney** | **No per-image price — you buy GPU time.** Basic $10/mo (3.3 Fast GPU hr), Standard $30, Pro $60, Mega $120; annual = 20% off; extra GPU time $4/hr | [OFFICIAL] | <https://docs.midjourney.com/hc/en-us/articles/27870484040333-Comparing-Midjourney-Plans> |

#### Commercial use / who owns the output (images)

| Vendor | Ownership mechanism | Revenue threshold | Trains on your content | Indemnity |
|---|---|---|---|---|
| **OpenAI** | **Assigns** all right/title/interest to you (ToU; Services Agreement §4.1) | None | **No** on API/business (§4.2) | Service-infringement only; **excludes Customer Content** |
| **Stability AI (hosted API)** | **Assigns** — §4(a): *"we assign to you all of our right, title, and interest (if any) in the Outputs"* | **None for the API.** Self-hosted weights: **$1M/yr** triggers Enterprise License | not verified | none found |
| **Black Forest Labs (FLUX)** | Disclaims, does not assign — §1.2: *"We claim no ownership rights in and to Your Content"*; commercial use of Output carved out of the general ban | None on API; self-hosting needs a tiered Open Weights License | not verified | none found |
| **Google (Imagen / Gemini Image)** | Gemini API terms: *"Google won't claim ownership over that content"* | None | Free tier **yes**; paid tier **no** | **Could not verify** GCP output-ownership clause or Veo/Imagen IP indemnity this session |
| **Adobe Firefly** | Commercial use affirmed for non-beta Adobe-trained models | None | not verified | **Yes — but enterprise contract only, Adobe-trained models only, betas excluded** |
| **Midjourney** | *"You own all Assets"* — **conditional** | **YES — $1,000,000 USD/yr** | Perpetual, irrevocable, sublicensable licence granted to Midjourney | **None. Explicit no-warranty of TITLE or NON-INFRINGEMENT; you indemnify them.** |

**Midjourney is the single worst legal fit in this dossier for an automated video company.** **[OFFICIAL]** ToS (version effective **May 27, 2026**) <https://docs.midjourney.com/hc/en-us/articles/32083055291277-Terms-of-Service> §4:

> *"You own all Assets You create with the Services to the fullest extent possible under applicable law. There are some exceptions: … **If you are a company or any employee of a company with more than $1,000,000 USD a year in revenue, you must be subscribed to a "Pro" or "Mega" plan to own Your Assets.**"*

> *"By using the Services, You grant to Midjourney, its affiliates, successors, and assigns a perpetual, worldwide, non-exclusive, sublicensable no-charge, royalty-free, irrevocable copyright license to reproduce, prepare derivative works of, publicly display, publicly perform, sublicense, and distribute the Content You input into the Services, as well as any Assets produced by You through the Service. This license survives termination."*

> *"Midjourney is an open community … **By default, Your Content is publicly viewable and remixable.**"* (Stealth Mode is Pro/Mega only.)

> *"**You may not use automated tools to access, interact with, or generate Assets through the Services.**"* — **this alone disqualifies Midjourney from an automated pipeline.** There is no official Midjourney API; third-party "Midjourney API" vendors are in breach.

> *"Only one user may use the Services per registered account. Each user of the Services may only have one account."*

**Adobe's indemnity is narrower than the marketing.** **[OFFICIAL]** <https://www.adobe.com/products/firefly/plans.html>: *"Outputs from Adobe Firefly AI models are safe for commercial use … **You're responsible for determining whether content generated by partner models is appropriate for your project.** Eligible enterprise customers can also receive IP indemnification for select Adobe Firefly outputs."* Firefly now routes to partner models (Nano Banana 2, FLUX.2 Pro, ChatGPT Image 2, Veo 3.1, Kling) that are **not** covered, betas are excluded, and the indemnity requires an enterprise entitlement. **Could not verify** the precise indemnity scope — `helpx.adobe.com/enterprise/using/generative-ai-indemnification.html` returns 404.

### C.3 AI video generation

| Provider / model | Price | Unit | Max clip | Audio | Commercial use | Tag / Source |
|---|---|---|---|---|---|---|
| **Veo 3.1 Lite** | **$0.05** (720p) / $0.08 (1080p) | per second, with audio | **8 s** (extendable to 148 s, 720p only) | Yes, always on | Paid API only; Google claims no ownership | [OFFICIAL] <https://ai.google.dev/gemini-api/docs/pricing>, <https://ai.google.dev/gemini-api/docs/veo> |
| Veo 3.1 Fast | $0.10 (720p) / $0.12 (1080p) / $0.30 (4K) | per second, with audio | 8 s | Yes | same | [OFFICIAL] same |
| Veo 3.1 Standard | $0.40 (720p/1080p) / $0.60 (4K) | per second, with audio | 8 s | Yes | same | [OFFICIAL] same |
| Veo 3.1 **silent** (Vertex only) | $0.20 / $0.40 (4K); Fast silent $0.08–$0.25; Lite silent $0.03–$0.05 | per "1 count" | 8 s | No | same | [OFFICIAL] <https://cloud.google.com/vertex-ai/generative-ai/pricing> — ⚠️ Vertex never defines "1 count"; that it equals one second is an **[INFERENCE/ESTIMATE]** (the numbers match Gemini's per-second rates exactly) |
| **OpenAI Sora 2 / 2-pro** | $0.10 / $0.30–$0.70 | per second | 16–20 s | — | — | **[OFFICIAL] — but see the shutdown warning below** |
| **Runway Gen-4 Turbo** | **5 credits/s = $0.05/s** ($0.25 per 5 s clip) | per second | not verified | likely none | **All tiers, including Free** | [OFFICIAL] <https://docs.dev.runwayml.com/guides/pricing/>, <https://runway.com/terms-of-use> |
| **Runway Gen-4.5** | **12 credits/s = $0.12/s** ($0.60 per 5 s clip) | per second | **could not verify** | **could not verify** | All tiers | [OFFICIAL] same |
| Runway plans | Free $0 (125 one-time credits); Standard $15/mo ($12 annual); Pro $35 ($28); Max $95 ($76) | — | — | — | Watermark removed from Standard up | [OFFICIAL] <https://runway.com/pricing> |
| **Luma Ray3.2 API** | 5 s clip **$0.15 / $0.30 / $1.20** (540p/720p/1080p); 10 s $0.45 / $0.90 / $3.60 | per clip | 5–10 s, extendable to 30 s | **No** (add audio post-hoc) | **Paid tiers only** | [OFFICIAL] <https://lumalabs.ai/api/pricing>, <https://lumalabs.ai/learning-hub/ray3-faq> |
| **Kling 3.0** | 4K = 30 credits/s; ~20 credits per 720p video. Standard $6.99/mo (660 cr), Pro $25.99 (3,000), Premier $64.99 (8,000), Ultra $127.99 (26,000) | credits | not verified | — | **Members only** | [OFFICIAL] <https://kling.ai/blog/kling-video-3-0-credit-cost-guide>, <https://kling.ai/docs/payment-policy> — recurring/annual prices **could not be verified** (membership page is client-rendered) |
| **MiniMax Hailuo 2.3-Fast** | 0.7 points per 768p/6 s clip; points $0.224–$0.266 ⇒ **≈$0.16–$0.19 per 6 s clip** | per clip | 10 s, 1080p max | — | **could not verify** | [OFFICIAL] <https://platform.minimax.io/docs/guides/pricing-video>; per-clip dollar figure is **[INFERENCE/ESTIMATE]** |
| **Pika** | Free $0; Starter $10/mo ($8 annual); Creator $35 ($28); Fancy $95 ($76) | — | — | — | **Creator and above only — Free *and* Starter are non-commercial** | [OFFICIAL] <https://pika.art/pricing> |
| ByteDance Seedance 2.5 | **no first-party price verified**; Runway resells at $0.20 / $0.30 / $0.68 per second (480p/720p/1080p) | per second | — | — | **not verified** | [OFFICIAL] (Runway's price for a ByteDance model) |
| FLUX 3 Video (BFL) | Draft $0.06/s HD; text/image→video $0.17/s HD, $0.29 FHD, $0.40 QHD, $0.80 UHD | per second | *"Clips up to 20 seconds"*; *"Audio is included at no extra charge"* | Yes | BFL disclaims ownership | [OFFICIAL] <https://bfl.ai/pricing> |

> ### 🚨 OpenAI Sora shuts down on 2026-09-24 — six days from this dossier's date
> **[OFFICIAL]** <https://developers.openai.com/api/docs/deprecations> lists a shutdown date of **2026-09-24** for the Videos API and `sora-2`, `sora-2-pro` and all their snapshots, with **"Recommended replacement: —"**. Verbatim: *"On March 24th, 2026, we notified developers using the Videos API and Sora 2 video generation model aliases and snapshots of their deprecation and removal from the API on September 24, 2026."* **Do not architect on Sora.** (Reports that the consumer Sora app was discontinued on 2026-04-26 could not be verified first-party — help.openai.com returns 403 on that article.)

**Veo specifics** — **[OFFICIAL]** <https://ai.google.dev/gemini-api/docs/veo>: *"Veo 3.1 is a model for generating 8-second videos (720p, 1080p, or 4k) with natively generated audio."* Extension is *"7 seconds and up to 20 times"* to a maximum of 148 seconds but *"limited to 720p videos."* 24 fps, one video per request. *"Generated videos are stored on the server for 2 days, after which they are removed."* *"Videos created by Veo are watermarked using SynthID"* — **no documented opt-out**.

**Veo via consumer plans is credit-metered** — **[OFFICIAL]** <https://support.google.com/flow/answer/16526234>: Veo 3.1 Quality = **100 Flow credits** per generation, Fast = 20 (10 on Ultra), Lite = 10 (5 on Ultra). With Google AI Pro's 1,000 credits/month that is **≈10 Quality generations per month** — i.e. **80 seconds of video**. A consumer plan is not production video capacity by any reading.

**Commercial-use gating at a glance:**

| Provider | Free tier commercial? | First tier granting commercial use |
|---|---|---|
| **Runway** | **YES** — *"the Company does not restrict your commercial use of your Outputs"*, no tier condition | all tiers (free output is watermarked) |
| Google Veo (API) | n/a — no free API tier | any paid API use |
| **Kling** | **NO** — non-members must watermark/attribute | Standard |
| **Luma** | **NO** — *"personal use only"*; watermarks *"cannot be removed, even if you later upgrade"* | Plus |
| **Pika** | **NO** — Free *and* Starter both excluded | Creator ($28–35) |
| Hailuo / Seedance | **could not verify** | **could not verify** |

Runway's grant is the cleanest — **[OFFICIAL]** <https://runway.com/terms-of-use>: *"Company does not claim ownership of any of your Inputs or Outputs"* and *"Subject to your compliance with the Agreement, the Company does not restrict your commercial use of your Outputs."* Note the broad licence back: *"a non-exclusive, irrevocable, perpetual, worldwide, royalty-free, fully paid, transferable, sublicensable right and license to use any Inputs and Outputs."*

Luma's grant is the most fragile — **[OFFICIAL]** <https://lumalabs.ai/legal/tos>: *"Customer … can only use the Outputs for commercial purposes if the Outputs were produced during an active Subscription Term under Customer's paid subscription allowing for the commercial use of those Outputs."* **Commercial rights attach at time of generation and do not retroactively cover earlier output.** ⚠️ Luma also serves **two contradictory live pricing pages** (<https://lumalabs.ai/dream-machine/pricing> shows Plus $30 / Pro $90 / Ultra $300 with commercial use on all; <https://lumalabs.ai/learning-hub/payments-subscriptions> still shows Free / Lite $9.99 non-commercial / Plus $29.99 / Unlimited $94.99). **Could not determine which governs billing.**

### C.4 Music and SFX

#### Royalty-free subscription libraries

| Service | Price | Tag | Source |
|---|---|---|---|
| **Epidemic Sound** | ⚠️ **UNRESOLVED PRICE CONFLICT.** Browser render of the pricing page gave Creator $5.99/mo annual ($8.99 monthly), Pro $11.99 annual ($29.99 monthly), Business $20 annual ($50 monthly), Business Plus from $100/mo. Domain-restricted search of the same site's comparison pages gave Creator $9.99 annual / $17.99 monthly and Pro $16.99 / $39.99. **These cannot both be current.** | [OFFICIAL] both, conflicting | <https://www.epidemicsound.com/pricing/> |
| **Artlist** | ⚠️ **UNRESOLVED PRICE CONFLICT.** Pricing card: Music & SFX *"Starting at $9.99/month"*, **Max $50.66/mo billed annually**, Max Business $399/mo. Artlist's own blog/help pages say **Max starts at $29.99/month billed annually**. | [OFFICIAL] both, conflicting | <https://artlist.io/pricing> |
| **Uppbeat** | Essentials **$6.99/mo** (1 channel), Creator **$8.99/mo** (3), Pro **$14.99/mo** (10, adds client projects + commercial digital + WAV + 4K stock video), Business **$34.99/mo excl. tax, yearly only** (10). These are yearly-discounted monthly-equivalents; undiscounted monthly **could not be verified** | [OFFICIAL] | <https://uppbeat.io/pricing> |
| Soundstripe | Pro *"Plans start as low as $9.99/mo"*; one-time licences Personal $49, Digital $199, Expanded from $399, All Media from $1,249. Other tier prices **could not be verified** | [OFFICIAL] | <https://www.soundstripe.com/library/pricing> |

**Epidemic tier scope** — **[OFFICIAL]** <https://www.epidemicsound.com/how-it-works/which-plan-is-right-for-you/>: Creator = *"Publish soundtracked content across social media platforms, websites, and podcasts"* + *"Monetize one channel per platform."* Pro adds *"digital ads"* and *"Monetize up to three channels per platform."* Business adds games/apps and company events. **OTT/TV broadcast and indemnification are Pro Plus / Business Plus / Enterprise only.** SFX (250,000 tracks) included at every tier.

**Perpetuity after cancellation — Epidemic, [OFFICIAL]** <https://www.epidemicsound.com/policy/creator-subscription/>:
> *"whatever Production you have completed, uploaded, and published on your Online Personal Channels during the Subscription Period … will remain licensed for you to use forever."*
> *"…after expiration or termination of the Subscription Period you may not use the Licensed Work(s) to create any new Productions … even if you have downloaded such Licensed Work(s) during the Subscription Period."*

**Perpetuity after cancellation — Artlist, [OFFICIAL]** <https://artlist.io/help-center/privacy-terms/artlist-license> §2:
> *"Once you create Projects using downloaded Assets and publish them in any media during your subscription term, you can keep using your Projects in the same media and monetize them forever, even after your subscription has expired."*
> *"When your subscription expires, those Projects can remain published in any media, but any new projects will not be covered."*

**⚠️ THE CONTENT ID FINDING — no library auto-clears, and none reimburse.**

Epidemic, **[OFFICIAL]** same licence:
> *"You are responsible for clearing the Productions and/or relevant Personal Online Channels with Epidemic Sound… You may clear one channel/page/profile/feed/etc. per platform. Without correct Clearing, Epidemic Sound is unable to tell a licensed Production from unlicensed use."*
> *"Epidemic Sound reserves the right to fully monetize unlicensed use of the Licensed Works on YouTube… Epidemic Sound will have no responsibility, and will not reimburse you, for any demonetization of Productions by Epidemic Sound for any period prior to such Productions having been correctly cleared."*

Artlist, **[OFFICIAL]** §7:
> *"If you don't add your channel or YouTube Projects URL to the clearlist, you may receive claims from Artlist Ltd and will not be able to monetize your videos… Once you add your channel… any claims from Artlist Ltd will be cleared… Please note that you will not be reimbursed for lost monetization for the period during which your channel… was not listed on the Clearlist."*

Uppbeat advertises *"All plans are copyright-safe for worry-free monetization"* (**[OFFICIAL]** <https://uppbeat.io/pricing>) but uses the same per-channel safelist model (1 / 3 / 10 / 10 channels by tier).

**Operational consequence for a multi-channel AI video company — [INFERENCE/ESTIMATE]:** these licences are really priced by *"how many channels may I allowlist."* A company running 5 channels cannot use an Epidemic Creator or Artlist Social plan at all, and must register every channel on day one or forfeit revenue permanently. Other clauses that bite: Artlist §5 forbids you from registering assets in Content ID yourself; §4 caps downloads at 40 songs / 100 SFX per day; §9 makes **you** liable for PRO royalties on broadcast; **§11 requires companies with 50+ employees to hold Max Business.** Epidemic's Creator tier is *"intended solely for your own personal use"*, bans corporate channels, third-party work, paid media ads, **any edits beyond cut/loop/fade**, and use *"in any third-party AI tool."*

#### AI music generators

| Service | Price | Commercial use | Tag / Source |
|---|---|---|---|
| **Suno** | Free **$0** (50 credits/day, no downloads, *"No commercial rights"*); **Pro $8/month annual** (2,500 credits, 20 downloads/mo); **Premier $24/month annual** (10,000 credits, 60 downloads/mo). The stated savings ($24/yr, $72/yr) imply monthly rates of **$10 and $30** — **[INFERENCE/ESTIMATE]**, the monthly figures are not printed | **Paid tiers only**, and only via an approved **Download** from the monthly allocation | [OFFICIAL] <https://suno.com/pricing>, <https://suno.com/terms> |
| **Udio** | Free $0 (10 cr/day), Standard $10/mo ($8 annual), Pro $30/mo ($24 annual) | ⚠️ **Moot — downloads are disabled** | [OFFICIAL] <https://www.udio.com/pricing>, <https://help.udio.com/en/articles/12683565> |
| **Google Lyria 3.5** | **$0.08 per song** | not verified | [OFFICIAL] <https://ai.google.dev/gemini-api/docs/pricing> |
| Google Lyria 3 Clip Preview (30 s) | $0.04 per song | not verified | [OFFICIAL] same |

**Suno ownership — [OFFICIAL]** <https://suno.com/terms>: *"Suno hereby assigns to you all of its right, title and interest in and to any Output owned by Suno and generated from Submissions made by you."* Free tier: *"you covenant and agree that you will only use such Outputs for your lawful, personal and non-commercial purposes."* **But the disclaimer immediately undercuts the assignment:** *"Due to the nature of machine learning, Suno makes no representation or warranty to you that any copyright will vest in any Output."* Suno assigns whatever it holds while declining to say anything exists to assign. *"Nothing in this paragraph permits commercial use of any Remix."*

**⚠️ Udio has disabled exports — [OFFICIAL]** <https://help.udio.com/en/articles/12683565>: *"downloading of audio, video, and stems has been disabled"* following the UMG partnership, with no restoration timeline stated. Subscriptions are still sold. **Udio is unusable for video production regardless of licence.** Its commercial-rights language per tier **could not be verified** (the pricing page carries none).

### C.5 Stock footage — realistic monthly cost for a channel

| Service | Price | Tag | Source |
|---|---|---|---|
| **Envato Elements** | **Core $16.50/month**, Plus $39/month, Ultimate $168/month, Enterprise custom — *"lifetime commercial license for all stock assets & AI generations"*. **Annual prices could not be verified** | [OFFICIAL] | <https://elements.envato.com/pricing> |
| **Storyblocks** | Essentials **$21/mo billed annually**, **Unlimited All Access $30/mo billed annually**, Small Business $40/mo, Business quote-only. **Month-to-month prices could not be verified**; there is no separate video-only tier on the current page | [OFFICIAL] | <https://www.storyblocks.com/pricing> |
| **Adobe Stock** | ⚠️ **PARTIAL CONFLICT.** Two independent reads of the same page agree on **$59.99/mo (25 credits, 3 videos)** and **$199.99/mo (750 credits, 25 videos)** but disagree on the entry tier ($29.99 vs $39.99) and on per-video credit cost (*"8 credits each"* vs *"8 – 20 credits each"*). 150-credit pack $359.99. 20% discount on 4K purchases; **per-clip 4K dollar price is not stated** | [OFFICIAL], internally conflicting | <https://stock.adobe.com/plans> |
| **Artgrid** | **Price could not be verified** (page is client-rendered). An artgrid.io-domain snippet gives *"Plans starting from $24.92 a month"*, ~$299/yr unlimited, $359.90/yr for 4K–8K + ProRes, $599/yr for RAW/LOG. **Tier names and monthly prices unverified** | [THIRD-PARTY-grade] | <https://artgrid.io> |
| **Shutterstock video** | **NO PRICE VERIFIED.** The pricing page 403s to fetch; a browser render confirmed tier *structure* (Video downloads at 5/10/20 clips/month; Unlimited Plus with 100 AI credits/mo) but **the dollar amounts never rendered**. A site snippet mentioned *"as little as $8.33 per clip"* on annual prepay — unconfirmed | **could not verify** | <https://www.shutterstock.com/pricing/video> |
| **Getty / iStock video** | **NOTHING OBTAINED.** Page returned chrome only | **could not verify** | <https://www.istockphoto.com/plans-and-pricing/video> |
| Pexels / Pixabay (free) | $0, no attribution required, commercial use permitted | [OFFICIAL] | <https://www.pexels.com/license/>, <https://pixabay.com/service/license-summary/> |

**Licence scope:**

- **Adobe Stock is the clearest grant of the set** — **[OFFICIAL]** <https://stock.adobe.com/license-terms>: *"An Adobe Stock perpetual, worldwide license allows you to use your licensed asset in all media, including print, presentations, broadcasts, websites, and on social media sites."* Standard licence permits *"Post the asset to a website or social media site with no limitation on views"* — **monetized YouTube is fine with no view cap.** The 500,000-viewer cap attaches only to email marketing, mobile advertising, and broadcast/digital programmes.
- **Envato Elements** has the clearest post-cancellation rule (help centre 403s; the following is an **[OFFICIAL-domain paraphrase, not certified verbatim]**): each download registers to one project; a licence becomes perpetual once the item has been incorporated into a *completed* End Product during an active subscription, but *"If you cancel your subscription and have not completed your End Product, the license for the Item is terminated."* **Practical rule: register everything and publish before you cancel.**
- **Storyblocks — the weakest point in this whole section.** The primary licence pages are JS-rendered and returned empty. The help centre defines royalty-free as *"no ongoing royalties or residuals after downloading and using assets **under an active subscription**"* and confirms *"No, you can't re-download assets with a cancelled subscription."* **Whether continued *use* of already-published assets survives cancellation could not be verified.** Broadcast/TV/streaming/OTT/feature film are **Business-only**. Prohibited: reselling, scripted downloading, stockpiling, use as logos, and **AI training**.
- **⚠️ Artgrid's licence contradicts itself.** **[OFFICIAL]** <https://cdn.artgrid.io/footage-images/LicenseAgreement.pdf> §3.g: *"The License is valid in perpetuity if the Works have been downloaded with a valid subscription."* §4.b: *"Upon termination of the subscription for any reason whatsoever, the Agreement and the License by its virtue will also be terminated and you hereby undertake to immediately cease downloading Works **and using them**."* These are not reconcilable on the face of the text; the PDF is undated and §11.b reserves unilateral changes. §3.b does explicitly permit *"video sharing sites (such as Vimeo, YouTube and so on)."* §5 forbids posting a **raw unedited clip** as a standalone video. §10 indemnification runs **against** the licensee. **Do not treat Artgrid post-cancellation rights as settled.**
- **Pexels / Pixabay:** the real caveat is **indemnity, not licence text** — neither offers the legal indemnification that Storyblocks, Adobe and Shutterstock sell, neither guarantees model or property releases, and both have had AI-generated and improperly-uploaded content pass through. Fine for filler B-roll; poor as a sole source where revenue is at stake.

**[INFERENCE/ESTIMATE] — realistic monthly floor for one monetized channel:** **~$16.50** (Envato Elements Core, which bundles stock + music + SFX) to **~$37** (Storyblocks Unlimited $30 + Uppbeat Essentials $6.99), before any AI video generation spend. For a five-channel operation, the multi-channel allowlist limits push you to Artlist Pro / Epidemic Pro-class tiers and Storyblocks Small Business, realistically **$70–$120/month** total.

---

## D. Local / open-source on a single consumer GPU

### D.1 What a 24GB card actually runs in 2026

**[THIRD-PARTY]** <https://localllm.in/blog/best-local-llms-24gb-vram> — measured single-stream decode, Q4_K_M:

| Model | Engine | Decode @ 8K ctx | Decode @ 64K ctx |
|---|---|---|---|
| Qwen3.6 35B A3B (MoE, ~3B active) | llama.cpp | **65.6 t/s** | — |
| Qwen3.6 35B A3B | Ollama | 16.8 t/s | 12.4 t/s (22.7 with `--n-cpu-moe 12`) |
| Gemma 4 26B A4B (MoE, ~4B active) | Ollama | **54.0 t/s** | **47.7 t/s** |
| Qwen3.6 27B (dense, best quality in tier) | llama.cpp | 13.7 t/s | 12.0 t/s |
| Qwen3.6 27B | Ollama | 10.9 t/s | **2.6 t/s** |
| Gemma 4 31B (dense) | Ollama | 7.6 t/s | 3.1 t/s — **OOMs at 16K+ in llama.cpp** |

Prefill is fast — **~1,694 t/s at 16K (Qwen, llama.cpp + FlashAttention), ~2,000 t/s (Gemma)**. That asymmetry (prefill roughly 50x decode) is the one place local economics genuinely shine.

Sizing rule of thumb **[THIRD-PARTY]**: at Q4-class quantization budget **~0.5 GB VRAM per billion parameters plus 20–40% for KV cache**. KV cache, not weights, is what OOMs you.

**Caveat:** all figures above are **single-stream**. Aggregate throughput with vLLM/SGLang continuous batching is a multiple of this; a ~10x factor is used below and is an **[INFERENCE/ESTIMATE]**, not a measurement.

### D.2 Hardware prices — the decisive finding

**The 2026 consumer GPU market is severely dislocated, and this is what kills the local case.**

| GPU | VRAM | TDP | Price (Sept 2026) | Source |
|---|---|---|---|---|
| RTX 5090 | 32 GB | 575 W | **new $6,899.99 / used $3,868** — ~245% above the $1,999 MSRP (as of 2026-09-15) | [THIRD-PARTY] <https://videocardprices.com/card/nvidia-rtx-5090/> |
| RTX 4090 | 24 GB | 450 W | **used $2,700 (Amazon) / new $2,073 (eBay)**, as of 2026-09-18 | [THIRD-PARTY] <https://gpudojo.com/rtx-4090> |
| RTX 4090 | 24 GB | 450 W | used ~$2,500–3,010, +36.8% in 30 days | [THIRD-PARTY] BestValueGPU — **fetch returned HTTP 429; snippet-sourced, lower confidence** |
| RTX 3090 | 24 GB | 350 W | used ~$1,050–$1,343 | [THIRD-PARTY] resaleprices.com / BestValueGPU — **snippet-sourced, lower confidence** |

Complete single-used-4090 workstation: **under $4,000** **[THIRD-PARTY]** <https://www.promptquorum.com/local-llms/local-llm-workstation-build>.

### D.3 Power and electricity

**[OFFICIAL — US EIA]** <https://www.eia.gov/electricity/monthly/epm_table_grapher.php?t=epmt_5_6_a>, June 2026 US average retail electricity price:

- Residential **18.34 c/kWh** · Commercial **14.19 c/kWh** · Industrial **9.17 c/kWh**

The commonly cited "$0.12/kWh US average" is now **stale by ~18%**. This dossier uses **commercial 14.19 c/kWh**.

**[INFERENCE/ESTIMATE]** system wall power: **450 W under load / 80 W idle** for a 3090 box, **550 W / 100 W** for a 4090 box. These sit between vendor TDP and a measured maxed-rig figure of 770–820 W **[THIRD-PARTY]** <https://www.promptquorum.com/local-llms/local-llm-power-consumption>. LLM decode is memory-bandwidth-bound and rarely pins the GPU at full TDP.

### D.4 TCO arithmetic — Build A (used RTX 3090, 24GB)

All arithmetic is shown so it can be audited and re-run. Derived figures are **[INFERENCE/ESTIMATE]**; input prices are [OFFICIAL] as cited above.

```
CAPITAL
  Used RTX 3090                                         $1,350
  CPU + mobo + 64GB DDR5 + 2TB NVMe + 1kW PSU + case    $1,150
  -----------------------------------------------------------
  Total capital                                         $2,500
  Amortized over 3 years:  $2,500 / 3 =                   $833.33 /yr

POWER RATES (commercial, $0.1419/kWh)
  Load: 0.450 kW x $0.1419 = $0.063855 per load-hour
  Idle: 0.080 kW x $0.1419 = $0.011352 per idle-hour

ANNUAL ELECTRICITY
   2 h/day:   730 x 0.063855 + 8,030 x 0.011352 =  $46.61 + $91.16 = $137.77
   8 h/day: 2,920 x 0.063855 + 5,840 x 0.011352 = $186.46 + $66.30 = $252.76
   24/7   : 8,760 x 0.063855                                       = $559.37

OPS / MAINTENANCE
  Hobby   : $0/yr        (your time valued at zero)
  Business: 2 h/month x $75/h loaded = $150/mo =                   $1,800/yr

TOKEN PRODUCTION PER YEAR
  Single-stream @ 30 tok/s sustained output:
     2 h/day:   730 h x 3,600 x 30 =    78.84 M
     8 h/day: 2,920 h x 3,600 x 30 =   315.36 M
     24/7   : 8,760 h x 3,600 x 30 =   946.08 M
  Batched via vLLM @ 300 tok/s aggregate  [ESTIMATE: ~10x single-stream]:
     2 h/day: 788.4 M | 8 h/day: 3,153.6 M | 24/7: 9,460.8 M
```

**Cost per 1M generated tokens — Build A:**

| Utilisation | Annual cost | Single-stream 30 t/s | Batched 300 t/s |
|---|---|---|---|
| **Hobby (ops $0)** | | | |
| 2 h/day | $833.33 + $137.77 = **$971.10** | **$12.32** | **$1.23** |
| 8 h/day | $833.33 + $252.76 = **$1,086.09** | **$3.44** | **$0.344** |
| 24/7 | $833.33 + $559.37 = **$1,392.70** | **$1.47** | **$0.147** |
| **Business (ops $1,800/yr)** | | | |
| 2 h/day | **$2,771.10** | **$35.15** | **$3.51** |
| 8 h/day | **$2,886.09** | **$9.15** | **$0.915** |
| 24/7 | **$3,192.70** | **$3.37** | **$0.337** |
| **Marginal** (hardware sunk, ops $0, 24/7) | $559.37 | **$0.591** | **$0.0591** |

**Build B (used RTX 4090, $4,000 all-in, 550 W load):** amortized $1,333.33/yr; 24/7 electricity $683.67; hobby annual $2,017.00. At ~50 t/s single-stream → 1,576.8 M tokens/yr → **$1.28 /MTok**; at ~500 t/s batched → **$0.128 /MTok**.

### D.5 Local vs API — the honest verdict

Three-year cash out for Build A run 24/7, hobby case: **$2,500 hardware + 3 × $559.37 electricity = $4,178.11.**

| What $4,178.11 buys as API output tokens | Tokens |
|---|---|
| Qwen3.7-Flash batch @ $0.065/MTok | 64,279 M |
| Mistral Ministral 3 (3B) batch @ $0.05/MTok | 83,562 M |
| Groq gpt-oss-20b batch @ $0.15/MTok | 27,854 M |
| DeepSeek-flash off-peak @ $0.60/MTok | 6,963 M |

| What the local box produces in the same 3 years | Tokens |
|---|---|
| Single-stream 30 t/s, 24/7 | 2,838 M |
| Batched 300 t/s, 24/7 | 28,382 M |

**Conclusions — [INFERENCE/ESTIMATE]:**

1. **Single-stream local loses catastrophically.** 2,838M produced vs 6,963M purchasable from the *most expensive* of the cheap APIs, and vs 64,279M from Qwen Flash batch — roughly **23x worse** than the cheapest credible API.
2. **Perfectly batched local roughly ties Groq gpt-oss-20b batch** (28,382M vs 27,854M). That is the break-even, and it demands a 24GB card saturated with vLLM 24/7/365 for three years with operations time valued at exactly zero.
3. **Against the genuinely cheapest tier it still loses by 2–3x** even under those heroic assumptions.
4. **In the business case it is not close:** $0.337/MTok batched-24/7 vs $0.065/MTok Qwen Flash batch — API is ~5x cheaper.
5. **The one genuine local advantage is input tokens.** Prefill at ~1,700–2,000 t/s vs 30–65 t/s decode means input costs roughly 1/50th the wall-clock of output. On a 20:1 input:output extraction workload, DeepSeek cache-miss costs $3.00 + $0.60 = $3.60 per 1M output tokens, which local batched ($0.337) beats. **But DeepSeek's $0.003/MTok cache-hit input destroys that advantage** the moment prompts repeat, dropping the same workload to $0.66. **Prompt caching is the local rig's real enemy.**
6. **The reason local looks bad in 2026 is the GPU market, not the technology.** If 24GB cards return to ~$700, Build A's hobby/batched/24-7 figure drops from $0.147 to roughly $0.09 per MTok and the case becomes genuinely interesting again.

**Local still wins on things that are not cost:** data never leaves the building; no per-request rate limits; deterministic latency; offline operation; a fixed predictable bill; no vendor deprecating your model. If any of those is a hard requirement the TCO argument is moot — but it is not a savings play.

### D.6 Where a 24GB local model is and is not good enough

**Good enough (L0/L1, and some L2):**
- Structured extraction, classification, entity pulls, sentiment, routing, intent tagging — Qwen3.6 35B A3B / Gemma 4 26B A4B at 47–66 t/s, near-parity with frontier models for these tasks. That commercial providers serve this work at $0.03/MTok tells you how cheap it is.
- Deterministic reformatting: JSON↔YAML, markdown cleanup, template filling, caption/subtitle normalisation.
- Redaction and PII scrubbing — privacy and capability arguments align; this is the strongest genuine local case.
- Short-context code work (Qwen3.6-27B scores 77.2 SWE-bench Verified **[THIRD-PARTY]** <https://benchlm.ai/best/local-llm>).
- **First-pass triage in front of an API** — run local, escalate the hard 10%. This is the architecture that actually pays.

**Not good enough (L3/L4):**
- **Long-context research.** Qwen3.6-27B drops to **2.6 t/s at 64K** in Ollama; Gemma 4 31B **OOMs past 16K** in llama.cpp. A nominal 262K context window is meaningless when the KV cache does not fit. DeepSeek gives 1M context at $0.15/MTok, or $0.003 cached.
- **Nuanced long-form scriptwriting.** Q4 quantization costs register, voice consistency, and the ability to hold a through-line over thousands of tokens. You get prose that reads as competent prose.
- **Fact-checking.** A 27B model at 4 bits has compressed away exactly the long-tail facts you would be checking, and will hallucinate confidently with no signal.
- **Multi-step agentic work.** Per-step reliability gaps compound into a completion-rate cliff over a 15-step trajectory.

---

## E. Cost table — best-value option per capability

All prices verified **2026-09-18**. "Quality tier" uses the L0–L4 convention defined in Section 0 and is an **[INFERENCE/ESTIMATE]** judgement, not a vendor claim.

| Capability | Best-value option | Price per unit | Quality tier | Commercial-use rights | Source URL |
|---|---|---|---|---|---|
| Bulk classification / tagging / extraction | Z.ai **GLM-4.7-Flash** | **$0.00** in / $0.00 out per MTok | L1 | Not verified — assume none until checked | <https://docs.z.ai/guides/overview/pricing> |
| Same, with a commercial-grade contract | Alibaba **Qwen3.7-Flash** (batch) | $0.015 in / **$0.065** out per MTok | L1 | Paid Model Studio terms | <https://www.alibabacloud.com/help/en/model-studio/model-pricing> |
| Cheap mid-tier reasoning | **Gemini 2.5 Flash-Lite** (batch) | **$0.05** in / **$0.20** out per MTok | L2 | Paid tier: content not used for training | <https://ai.google.dev/gemini-api/docs/pricing> |
| Long-context research synthesis | **DeepSeek-flash** off-peak, cache-hit | **$0.003** in (cache hit) / $0.60 out per MTok, **1M ctx** | L2–L3 | Not verified | <https://api-docs.deepseek.com/quick_start/pricing/> |
| Editorial scripting / fact-check | **Claude Sonnet 5** | **$2** in / **$10** out per MTok; batch $1/$5; cache read $0.20; **1M ctx, no long-context surcharge** | L3 | **Customer owns Outputs; Anthropic assigns its rights** | <https://platform.claude.com/docs/en/about-claude/pricing> |
| High-stakes claim review / escalation | **Claude Opus 5** | **$5** in / **$25** out per MTok; batch $2.50/$12.50 | L4 | Same as above | same |
| Fast cheap L1 with a first-party contract | **Claude Haiku 4.5** | **$1** in / **$5** out per MTok; batch $0.50/$2.50 | L1–L2 | Same as above | same |
| Web search for research | **Brave Search API** | **$5 per 1,000 requests** | L0 | Commercial API | <https://brave.com/search/api/> |
| TTS — bulk narration | **Google Cloud TTS Standard/WaveNet** | **$4 per 1M characters**, first **4M chars/month free** | L2 | GCP terms — **ownership clause not verified** | <https://cloud.google.com/text-to-speech/pricing> |
| TTS — good quality, clean contract | **OpenAI tts-1** | **$15 per 1M characters** | L2–L3 | **You own Output; OpenAI assigns.** API content not used for training | <https://developers.openai.com/api/docs/pricing> |
| TTS — best naturalness + voice cloning | **ElevenLabs API, Flash/Turbo** | **$0.05 per 1,000 chars** ($50/1M) | L4 | Paid = commercial OK; **you retain rights BUT ElevenLabs trains on your content by default** | <https://elevenlabs.io/pricing/api> |
| Voice cloning (professional) | ElevenLabs **Creator** tier | $22/mo minimum entitlement | L4 | Commercial from Starter; PVC from Creator; PVC is self-voice only | <https://elevenlabs.io/pricing> |
| Images — cheapest credible | **FLUX.2 [klein] 4B** | **$0.014 per 1 MP image** | L1–L2 | BFL disclaims ownership; commercial use permitted; **no revenue threshold** | <https://bfl.ai/pricing> |
| Images — best value with explicit assignment | **OpenAI gpt-image-2.5, medium** | **$0.01317 per 1024² image** | L2 | **OpenAI assigns all rights to you**; no threshold | <https://developers.openai.com/api/docs/guides/image-generation> |
| Images — thumbnail-grade quality | **OpenAI gpt-image-2.5, high** | **$0.05268 per 1024² image** | L3 | Same | same |
| Images — top quality | **Gemini 3 Pro Image** | $0.134 @1K–2K, $0.24 @4K | L4 | Google claims no ownership; paid tier not used for training | <https://ai.google.dev/gemini-api/docs/pricing> |
| Images — **avoid** | ~~Midjourney~~ | $10–$120/mo GPU time | L4 quality | **Automation forbidden; $1M revenue → Pro/Mega; no title/non-infringement warranty; output public by default** | <https://docs.midjourney.com/hc/en-us/articles/32083055291277-Terms-of-Service> |
| AI video — cheapest with audio | **Veo 3.1 Lite** | **$0.05 per second** (720p), 8 s max clip | L2–L3 | Paid API; Google claims no ownership; SynthID watermark, no opt-out | <https://ai.google.dev/gemini-api/docs/pricing> |
| AI video — cheapest overall | **Runway Gen-4 Turbo** | **$0.05 per second** ($0.25 per 5 s clip) | L2 | **Commercial use on every tier including Free** — the cleanest grant found | <https://docs.dev.runwayml.com/guides/pricing/> |
| AI video — cheapest per clip | MiniMax **Hailuo 2.3-Fast** | ≈**$0.16–$0.19 per 6 s 768p clip** [INFERENCE/ESTIMATE] | L2 | **Commercial terms could not be verified** | <https://platform.minimax.io/docs/guides/pricing-video> |
| AI video — **do not use** | ~~OpenAI Sora 2~~ | $0.10–$0.70 per second | — | **API shuts down 2026-09-24, no replacement** | <https://developers.openai.com/api/docs/deprecations> |
| AI music | **Google Lyria 3.5** | **$0.08 per song** | L2–L3 | **Commercial terms not verified** | <https://ai.google.dev/gemini-api/docs/pricing> |
| AI music with a stated licence | **Suno Pro** | $8/mo annual (20 downloads/mo ⇒ ≈$0.40/track) | L3 | Paid only; Suno assigns its rights **but warrants no copyright vests** | <https://suno.com/terms> |
| Licensed music + SFX | **Uppbeat Essentials** | **$6.99/mo** (1 channel safelisted) | L3 | Monetization OK; **you must safelist each channel; no reimbursement pre-registration** | <https://uppbeat.io/pricing> |
| Stock footage + music + SFX bundle | **Envato Elements Core** | **$16.50/month** | L3 | *"lifetime commercial license"*; perpetual only for **completed** End Products | <https://elements.envato.com/pricing> |
| Stock footage, unlimited | **Storyblocks Unlimited All Access** | **$30/month billed annually** | L3 | Royalty-free while subscribed; **post-cancellation use rights NOT verified**; broadcast/OTT is Business-only | <https://www.storyblocks.com/pricing> |
| Stock footage, strongest licence | **Adobe Stock** | $59.99/mo (25 credits ⇒ 3 videos) | L4 | *"perpetual, worldwide license … in all media"*; no view cap on social | <https://stock.adobe.com/license-terms> |
| Local inference | Used RTX 3090 build | $0.147–$3.37 per MTok output depending on utilisation | L1 only | You own everything | Section D |

---

## F. ESTIMATE — AI cost per 10-minute narrated video

> ### ⚠️ THIS IS AN ESTIMATE
> Every number below is an **[INFERENCE/ESTIMATE]** built from the **[OFFICIAL]** unit prices cited above. The token and character assumptions are mine and are stated explicitly so the arithmetic can be audited and re-run with different assumptions. It excludes editing software, hosting, storage, compute orchestration, and all human labour. It assumes **API access only** — see Section H for why consumer subscriptions are not an option.

### F.1 Stated assumptions

| Assumption | Value | Basis |
|---|---|---|
| Narration rate | 150 words per minute | industry convention for documentary-style narration |
| Script length | 10 min × 150 = **1,500 words** | derived |
| Characters of narration | 1,500 words × 5.8 chars/word incl. spaces = **8,700 characters** | derived; **Google bills spaces, newlines and SSML tags** so real usage will exceed this |
| Tokens in the final script | ~2,000 tokens (1 token ≈ 0.75 words) | Anthropic's published rule of thumb |
| Research: pages ingested | ~12 pages, ~150,000 input tokens | at Anthropic's stated ~2,500 tokens per 10 kB page |
| Web searches per video | 8 (EXPECTED) | judgement |
| Thumbnail candidates | 6 generated, 1 shipped (EXPECTED) | judgement — A/B testing thumbnails is standard practice |
| Script revision passes | 2 | judgement |
| Caching | assumed **off** in this model, so figures are conservative (high) | see F.5 |

### F.2 Itemised token budget (EXPECTED case)

| Task | Input tokens | Output tokens | Model |
|---|---|---|---|
| Research synthesis | 150,000 | 8,000 | Claude Sonnet 5 |
| Scripting (2 passes) | 50,000 | 6,000 | Claude Sonnet 5 |
| Fact-check | 60,000 | 3,000 | Claude Sonnet 5 |
| SEO pack (title, description, tags, chapters) | 10,000 | 2,000 | Claude Haiku 4.5 |
| QC review | 25,000 | 2,500 | Claude Haiku 4.5 |
| **Total** | **295,000** | **21,500** | |

LOW case assumes **half** these tokens (tight pipeline, aggressive caching) on the cheapest model. HIGH case assumes **double** these tokens on Claude Opus 5.

### F.3 The arithmetic

**LOW — $0.11 per video.** Cheapest credible stack: Gemini 2.5 Flash-Lite, Brave search, gpt-image-2.5 medium, Google Cloud TTS Standard.

```
Research + script + fact-check + SEO + QC, all on Gemini 2.5 Flash-Lite:
  input : 147,500 tok x $0.10/MTok  = $0.014750
  output:  10,750 tok x $0.40/MTok  = $0.004300
                                      ---------
                                      $0.019050

Web search: 5 x Brave @ $5/1,000            = $0.025000
Thumbnails: 2 x gpt-image-2.5 medium @$0.01317 = $0.026340
TTS      : 8,700 chars x $4/1M chars        = $0.034800
                                              ---------
LOW TOTAL                                     $0.105190  ->  ~$0.11
```
Note: Google's **first 4M characters/month are free**, which covers ~460 ten-minute scripts per month. At realistic volume the TTS line in the LOW case is effectively **$0.00**, taking LOW to **~$0.07**.

**EXPECTED — $1.58 per video.** Claude Sonnet 5 for editorial work, Haiku 4.5 for L1/L2, ElevenLabs Flash for narration.

```
Research   : 150,000 x $2/MTok  + 8,000 x $10/MTok = $0.300 + $0.080 = $0.3800
Scripting  :  50,000 x $2/MTok  + 6,000 x $10/MTok = $0.100 + $0.060 = $0.1600
Fact-check :  60,000 x $2/MTok  + 3,000 x $10/MTok = $0.120 + $0.030 = $0.1500
SEO (Haiku):  10,000 x $1/MTok  + 2,000 x  $5/MTok = $0.010 + $0.010 = $0.0200
QC  (Haiku):  25,000 x $1/MTok  + 2,500 x  $5/MTok = $0.025 + $0.0125= $0.0375
Web search : 8 x $10/1,000 (Anthropic tool)                          = $0.0800
                                                                       -------
LLM subtotal                                                           $0.8275

Thumbnails : 6 x gpt-image-2.5 high @ $0.05268                       = $0.3161
TTS        : 8,700 chars x $0.05/1,000 (ElevenLabs Flash API)        = $0.4350
                                                                       -------
EXPECTED TOTAL                                                         $1.5786  ->  ~$1.58
```

**HIGH — $6.44 per video.** Claude Opus 5 throughout, doubled token budget, 10 thumbnail candidates at top quality, ElevenLabs v3.

```
LLM (Opus 5): 590,000 x $5/MTok + 43,000 x $25/MTok = $2.950 + $1.075 = $4.025
Web search  : 20 x $10/1,000                                          = $0.200
Thumbnails  : 10 x Gemini 3 Pro Image @2K $0.134                      = $1.340
TTS         : 8,700 chars x $0.10/1,000 (ElevenLabs v3)               = $0.870
                                                                        ------
HIGH TOTAL                                                              $6.435  ->  ~$6.44
```

### F.4 Summary and the line item that dwarfs all of it

| | LOW | EXPECTED | HIGH |
|---|---|---|---|
| LLM (research, script, fact-check, SEO, QC) | $0.019 | $0.828 | $4.225 |
| Thumbnail images | $0.026 | $0.316 | $1.340 |
| TTS narration | $0.035 (≈$0 within free tier) | $0.435 | $0.870 |
| **AI cost per 10-minute video** | **$0.11** | **$1.58** | **$6.44** |

**⚠️ Now add AI-generated video and the model inverts completely — [INFERENCE/ESTIMATE]:**

| Visual strategy for 10 minutes | Cost | vs the EXPECTED $1.58 above |
|---|---|---|
| Stock footage + static images (amortized library subscription, ~$30/mo ÷ 30 videos) | **~$1.00** | comparable |
| 12 short AI cutaways (12 × 8 s = 96 s) at Veo 3.1 Lite $0.05/s | **$4.80** | 3x the entire rest of the video |
| 12 cutaways at Veo 3.1 Fast $0.10/s | **$9.60** | 6x |
| **Fully AI-generated 10 minutes** at Veo 3.1 Lite $0.05/s | **$30.00** | **19x** |
| Fully AI-generated at Veo 3.1 Fast $0.10/s | **$60.00** | 38x |
| Fully AI-generated at Veo 3.1 Standard $0.40/s | **$240.00** | 152x |

**This is the single most important architectural finding in the cost model.** Research, scripting, fact-checking, SEO, thumbnails and narration together cost **$1.58**. Generating the *pictures* with AI costs **$30–$240**. A 10-minute narrated format must be built on stock footage, static imagery, motion graphics or screen capture, with AI video used only as sparing accent cutaways. Anyone who budgets "AI video costs about $2 a video" has not priced the visuals.

Also note Veo's hard **8-second clip ceiling** — a 10-minute fully-AI video means **75 separate generations** stitched together, with the continuity and QC labour that implies.

### F.5 Sensitivity — what moves the number

**[INFERENCE/ESTIMATE]:**

- **Prompt caching is the biggest lever on the EXPECTED case.** The research and fact-check stages re-send the same source corpus. With Anthropic 5-minute caching at 0.1x read, the $0.300 research-input line falls to roughly $0.030 after the first call, cutting the LLM subtotal from $0.828 to about **$0.50**. Cached reads also **do not count toward ITPM rate limits**, so throughput improves at the same time.
- **The Batch API halves everything** on the LLM line for non-time-sensitive work. Applied to the EXPECTED case, $0.828 → about **$0.41**. A content pipeline that publishes on a schedule, not on demand, should be batch-first.
- **Together, caching + batch take EXPECTED from $1.58 to roughly $1.15**, with the mix shifting so that **TTS becomes the largest single line item.**
- **Switching narration from ElevenLabs Flash ($0.05/1k) to Google Standard ($4/1M = $0.0035/1k)** saves $0.40 per video — a 14x reduction on that line. Whether the voice quality difference is worth $0.40 per video is an editorial decision, not a cost one; at 300 videos/month it is $120/month.
- **Tokenizer caveat:** Claude Opus 5 / Sonnet 5 produce *"approximately 30% more tokens for the same text"* than Sonnet 4.6 and earlier **[OFFICIAL]**. If you benchmarked on a 4.6-era model, multiply your token counts by ~1.3 before using this table.

---

## G. ESTIMATE — AI cost per 60-second short

> ### ⚠️ THIS IS AN ESTIMATE — same caveats as Section F.

### G.1 Stated assumptions

| Assumption | Value |
|---|---|
| Narration rate | 160 wpm (shorts are read faster) |
| Script length | 60 s × 160 = **160 words** |
| Characters | 160 × 5.9 = **950 characters** |
| Research depth | ~3 sources, one angle — far lighter than long-form |
| Cover image | 1 (EXPECTED), 3 candidates (HIGH) |
| Web searches | 3 (EXPECTED) |

### G.2 Itemised token budget (EXPECTED case)

| Task | Input tokens | Output tokens | Model |
|---|---|---|---|
| Research | 20,000 | 1,500 | Claude Sonnet 5 |
| Scripting | 8,000 | 600 | Claude Sonnet 5 |
| Fact-check | 10,000 | 500 | Claude Sonnet 5 |
| SEO / hook / caption | 3,000 | 400 | Claude Haiku 4.5 |
| QC | 5,000 | 400 | Claude Haiku 4.5 |
| **Total** | **46,000** | **3,400** | |

### G.3 The arithmetic

```
LOW  (Gemini 2.5 Flash-Lite, Brave, gpt-image medium, Google TTS Standard)
  LLM   : 23,000 x $0.10/MTok + 1,700 x $0.40/MTok = $0.00230 + $0.00068 = $0.00298
  Search: 2 x $5/1,000                                                   = $0.01000
  Image : 1 x $0.01317                                                   = $0.01317
  TTS   : 950 chars x $4/1M                                              = $0.00380
                                                                           -------
  LOW TOTAL                                                                $0.02995  ->  ~$0.03

EXPECTED (Sonnet 5 + Haiku 4.5, Anthropic search, gpt-image high, ElevenLabs Flash)
  Research   : 20,000 x $2/MTok + 1,500 x $10/MTok = $0.040 + $0.015 = $0.0550
  Scripting  :  8,000 x $2/MTok +   600 x $10/MTok = $0.016 + $0.006 = $0.0220
  Fact-check : 10,000 x $2/MTok +   500 x $10/MTok = $0.020 + $0.005 = $0.0250
  SEO (Haiku):  3,000 x $1/MTok +   400 x  $5/MTok = $0.003 + $0.002 = $0.0050
  QC  (Haiku):  5,000 x $1/MTok +   400 x  $5/MTok = $0.005 + $0.002 = $0.0070
  Search     : 3 x $10/1,000                                         = $0.0300
  Image      : 1 x gpt-image-2.5 high $0.05268                       = $0.0527
  TTS        : 950 x $0.05/1,000                                     = $0.0475
                                                                       -------
  EXPECTED TOTAL                                                       $0.2442  ->  ~$0.24

HIGH (Opus 5, doubled tokens, 3 top-quality images, ElevenLabs v3)
  LLM   : 92,000 x $5/MTok + 6,800 x $25/MTok = $0.460 + $0.170 = $0.630
  Search: 6 x $10/1,000                                          = $0.060
  Images: 3 x Gemini 3 Pro Image $0.134                          = $0.402
  TTS   : 950 x $0.10/1,000                                      = $0.095
                                                                   -----
  HIGH TOTAL                                                       $1.187  ->  ~$1.19
```

### G.4 Summary — and again, video generation dominates

| | LOW | EXPECTED | HIGH |
|---|---|---|---|
| **AI cost per 60-second short (narration + stills)** | **$0.03** | **$0.24** | **$1.19** |

**[INFERENCE/ESTIMATE]** — if the short is fully AI-generated video:

| Visual strategy for 60 s | Cost | Total short |
|---|---|---|
| Stills / stock / motion graphics | ~$0.00–0.05 | **$0.24** |
| MiniMax Hailuo 2.3-Fast, 10 × 6 s clips @ ≈$0.175 | **$1.75** | **$1.99** — *commercial terms unverified* |
| Runway Gen-4 Turbo, 60 s @ $0.05/s | **$3.00** | **$3.24** — commercial use on all tiers |
| Veo 3.1 Lite, 60 s @ $0.05/s (8 clips) | **$3.00** | **$3.24** |
| Runway Gen-4.5, 60 s @ $0.12/s | **$7.20** | **$7.44** |
| Veo 3.1 Standard, 60 s @ $0.40/s | **$24.00** | **$24.24** |

**For shorts the ratio is even more extreme than for long-form: AI video generation is 12x to 100x the cost of everything else combined.** At $3.00 of generation for a 60-second AI-video short, a channel publishing 5 shorts a day spends **$450/month on video generation** versus **$36/month** on all text, images and narration. The video model choice, not the LLM choice, is the budget.

**[INFERENCE/ESTIMATE] — blended monthly illustration.** A channel publishing **2 long-form videos per week (8/month) + 5 shorts per day (150/month)**, EXPECTED tier, stock-footage visuals:

```
Long-form : 8 x $1.58                          = $ 12.64
Shorts    : 150 x $0.24                        = $ 36.00
Stock + music library (Storyblocks + Uppbeat)  = $ 36.99
                                                 -------
Monthly AI + licensing                           $ 85.63
```
Swap the shorts to fully AI-generated video at Runway Gen-4 Turbo and the same month becomes **$85.63 + 150 × $3.00 = $535.63** — a **6.3x increase** driven entirely by one decision.

---

## H. Terms-of-Service findings

This section answers two questions per provider: **(a) may consumer-subscription capacity be used for automated production?** and **(b) are multiple accounts permitted?** Where the terms are ambiguous, that is stated rather than resolved conveniently.

### H.1 Anthropic (Claude Free / Pro / Max)

**(a) Automated/programmatic access — PROHIBITED, explicitly and unambiguously.**
**[OFFICIAL]** <https://www.anthropic.com/legal/consumer-terms>, Section 3 ("Use of our Services"), listing what you may not do:

> *"Except when you are accessing our Services via an Anthropic API Key or where we otherwise explicitly permit it, to access the Services through automated or non-human means, whether through a bot, script, or otherwise."*

> *"To crawl, scrape, or otherwise harvest data or information from our Services other than as permitted under these Terms."*

This is the clearest statement of the three providers. The carve-out is *"via an Anthropic API Key"* — which is precisely the distinction between the consumer product and the API. **A Claude Pro or Max subscription cannot lawfully be driven by the company's pipeline.**

**(b) Multiple accounts — not flatly banned, but heavily constrained, and account *sharing* is banned.**
**[OFFICIAL]** Consumer Terms Section 2 ("Account creation and access"): *"You may not share your Account login information, Anthropic API key, or Account credentials with anyone else. You also may not make your Account available to anyone else."*

**[OFFICIAL]** Usage Policy <https://www.anthropic.com/legal/aup> prohibits: *"Circumvent a ban through the use of a different account, such as the creation of a new account, use of an existing account, or providing access to a person or entity that was previously banned"*; *"Coordinate malicious activity across multiple accounts to avoid detection or circumvent product guardrails"*; and *"Utilize automation in account creation or to engage in spammy behavior."*

**Honest reading — [INFERENCE/ESTIMATE]:** there is **no clause that says "one account per person"** in the Anthropic consumer terms. What is banned is ban-evasion, coordinated multi-account activity to circumvent guardrails, automated account creation, and credential sharing. **This is ambiguous at the edges.** A company buying several Pro seats for several real humans is plainly fine. A company creating several accounts to farm more capacity than one account allows sits squarely in *"circumvent product guardrails"* territory and is at minimum a high-risk reading. It is moot in practice, because (a) already forecloses automated use.

**(c) Output ownership is fine either way.** Consumer Terms: *"Subject to your compliance with our Terms, we assign to you all of our right, title, and interest—if any—in Outputs."* Commercial Terms (API/business): *"Customer … owns its Outputs"* and Anthropic assigns its interest. **[OFFICIAL]** Note the Consumer Terms contain **no affirmative clause authorising commercial use** — they assign ownership but say nothing granting a commercial licence. That gap is another reason the API's Commercial Terms, which expressly contemplate *"power[ing] products and services Customer makes available to its own customers and end users,"* are the right instrument.

**Verdict: consumer subscriptions MAY NOT be used as production capacity. Use the API.**

### H.2 OpenAI (ChatGPT Free / Go / Plus / Pro)

**(a) Automated/programmatic access — PROHIBITED.**
**[OFFICIAL]** <https://openai.com/policies/row-terms-of-use/>, Terms of Use effective **January 1, 2026**, under "What you cannot do":

> *"Automatically or programmatically extract data or Output (defined below)."*

> *"Interfere with or disrupt our Services, including circumvent any rate limits or restrictions or bypass any protective measures or safety mitigations we put on our Services."*

OpenAI restates this specifically in the context of paid subscription allowances — **[OFFICIAL]** <https://help.openai.com/en/articles/9793128-what-is-chatgpt-pro>: *"Usage must also adhere to our Terms of Use, which prohibits, among other things: Abusive usage, such as automatically or programmatically extracting data. Sharing your account credentials or making your account available to anyone else. **Reselling access or using ChatGPT to power third-party services.**"*

That last clause is decisive for a media company: using a ChatGPT subscription to power a production pipeline is *"using ChatGPT to power third-party services."*

**(b) Multiple accounts — sharing is banned; holding several accounts is NOT explicitly addressed.**
**[OFFICIAL]** Terms of Use, "Registration": *"You must provide accurate and complete information to register for an account to use our Services. You may not share your account credentials or make your account available to anyone else and are responsible for all activities that occur under your account."*

**Honest reading — [INFERENCE/ESTIMATE]:** the January 2026 Terms of Use contain **no "one account per person" clause** and no explicit prohibition on an individual or company holding multiple accounts. This is genuinely ambiguous. But it does not help: the circumvention clause (*"circumvent any rate limits or restrictions"*) covers multi-accounting undertaken to exceed a plan's allowance, and the programmatic-extraction ban forecloses the use case regardless of account count.

**(c) Supply constraint, [OFFICIAL]:** *"As of September 10, 2026, we're temporarily pausing new sign-ups and upgrades to the ChatGPT Pro $200 plan (Pro 20X)."* Even setting the terms aside, the highest consumer tier **cannot currently be purchased.**

**(d) Output ownership is clean:** *"you (a) retain your ownership rights in Input and (b) own the Output. We hereby assign to you all our right, title, and interest, if any, in and to Output."* **[OFFICIAL]** Applies to consumer and API alike. Business terms add *"OpenAI will not use Customer Content to develop or improve the Services."*

**(e) One prohibited-use clause a video company must read carefully:** *"Represent that Output was human-generated when it was not."* **[OFFICIAL]** This is an OpenAI contractual obligation independent of any platform's disclosure rules.

**Verdict: consumer subscriptions MAY NOT be used as production capacity. Use the API.**

### H.3 Google (Gemini Free / AI Plus / AI Pro / AI Ultra)

**(a) Automated access — prohibited, but via looser and more indirect language than the other two.**
**[OFFICIAL]** <https://policies.google.com/terms>, section "Don't abuse our services", prohibits:

> *"using automated means to access content from any of our services in violation of the machine-readable instructions"*

> *"spamming, hacking, or bypassing our systems or protective measures"*

> *"accessing or using our services or content in fraudulent or deceptive ways"*

**[OFFICIAL]** <https://policies.google.com/terms/generative-ai> (Generative AI Additional Terms) adds *"You may not use the Services to develop machine learning models or related technology"* and requires compliance with the Prohibited Use Policy, but **contains no clause specifically about programmatic access, output ownership, or commercial use.**

**Honest reading — this is the most ambiguous of the three, and I will not resolve it conveniently.** Google's prohibition is conditioned on violating *"machine-readable instructions"* (i.e. robots.txt-style signals) rather than being an absolute ban on scripted access, and the phrase *"bypassing our systems or protective measures"* is broad but not specific to rate limits. **I could not find a Google consumer-facing clause as explicit as Anthropic's "bot, script, or otherwise" or OpenAI's "automatically or programmatically extract data or Output."**

Two things nonetheless point the same way as the other providers:
1. **[OFFICIAL]** <https://ai.google.dev/gemini-api/terms>: *"Use of Google AI Studio and Gemini API is for developers building with Google AI models for professional or business purposes, **not for consumer use**."* Google itself draws the developer/consumer line and puts business use on the API side of it.
2. **[OFFICIAL]** Google Terms: *"using AI-generated content from our services to develop machine learning models or related AI technology"* is prohibited — narrower than a general automation ban, but a signal about how output may be reused.

**(b) Multiple accounts — NOT addressed.** **[OFFICIAL]** The Google Terms of Service contain no clause about one account per person or holding multiple accounts; they address only age requirements and account security. **This is a genuine gap, not a permission.** Google routinely enforces anti-abuse measures that are not spelled out in the ToS, and Google AI plans are sold with family sharing of *"up to 5 other family members"*, which implies an intended household rather than fleet model.

**(c) A hard, verifiable reason not to use consumer Google as production capacity, independent of the ToS:** **[OFFICIAL]** <https://support.google.com/flow/answer/16526234> — Google AI Pro grants **1,000 Flow credits/month** and a Veo 3.1 Quality generation costs **100 credits**, i.e. **≈10 generations = 80 seconds of video per month**. Even on Ultra 20x (25,000 credits) that is ≈250 generations, ≈33 minutes of video. **The consumer plans are not production video capacity at any tier.**

**(d) Free-tier data use is disqualifying for proprietary work.** **[OFFICIAL]** <https://ai.google.dev/gemini-api/terms>: on unpaid services *"Google uses the content you submit … to provide, improve, and develop Google products and services"* and *"human reviewers may read, annotate, and process your API input and output."* On paid services *"Google doesn't use your prompts … or responses to improve our products."*

**Verdict: ambiguous on the letter of the automation clause, unambiguous in practice.** Google's own terms route business use to the API, the consumer credit allowances are nowhere near production scale, and the free tier trains on your scripts. **Use the paid API.**

### H.4 Cross-provider summary

| Provider | Consumer subscription usable as automated production capacity? | Multiple accounts permitted? | Governing clause |
|---|---|---|---|
| **Anthropic** | **NO — explicitly prohibited.** *"Except when you are accessing our Services via an Anthropic API Key … to access the Services through automated or non-human means, whether through a bot, script, or otherwise"* | **Not explicitly banned, but ambiguous and risky.** No one-account rule exists; but ban-evasion, coordinated multi-account guardrail circumvention, automated account creation and credential sharing are all prohibited | Consumer Terms §2, §3; Usage Policy |
| **OpenAI** | **NO — explicitly prohibited.** *"Automatically or programmatically extract data or Output"*; and *"Reselling access or using ChatGPT to power third-party services"* is called out for Pro tiers. Also: Pro $200 sign-ups are paused as of 2026-09-10 | **Ambiguous.** No one-account-per-person clause in the Jan 2026 ToU. Credential sharing banned; rate-limit circumvention banned | ToU "Registration", "What you cannot do"; Pro tiers help article |
| **Google** | **AMBIGUOUS ON THE LETTER, NO IN PRACTICE.** The automation prohibition is conditioned on *"machine-readable instructions"* and is not as explicit as the other two. But Google's own API terms say the API is *"for developers … not for consumer use"*, and consumer Veo allowances (≈10 Quality generations/month on AI Pro) are nowhere near production scale | **NOT ADDRESSED AT ALL** in the Google ToS. Silence is not permission | Google ToS "Don't abuse our services"; Generative AI Additional Terms; Gemini API Additional Terms |

### H.5 The bottom line on subscriptions

**[INFERENCE/ESTIMATE]:** all three consumer products are legally unsuitable as production capacity, and it costs the company almost nothing to comply. Running the EXPECTED 10-minute pipeline at **$1.58 per video**, a single **$200/month Claude Max 20x** subscription would have to yield **127 videos a month** just to break even against buying the same work on the API — and the API path has no account-termination risk, no undisclosed weekly caps, published rate limits, a contract that expressly permits powering customer-facing products, and a commitment not to train on your content. **There is no cost case for the risk.**

### H.6 Media-vendor terms that bind just as hard

Three findings in Section C are ToS risks of the same order as the subscription question, and are restated here so they are not buried:

1. **Midjourney forbids automation outright** — *"You may not use automated tools to access, interact with, or generate Assets through the Services"* — makes ownership conditional on a **$1M revenue → Pro/Mega** subscription that binds *"any employee of a company"*, defaults your work to **publicly viewable and remixable** below Pro, grants itself a perpetual irrevocable sublicensable licence over your output, and disclaims all warranty of **title and non-infringement**. **[OFFICIAL]** ToS v. 2026-05-27. For an AI-operated video company this is disqualifying on four independent grounds.
2. **ElevenLabs trains on your content by default** (§4(d)), including your voice and *"other indicia of your persona"*, and free-tier output is non-commercial. Anything monetized needs Starter minimum; anything confidential needs Enterprise terms. **[OFFICIAL]**
3. **No music library auto-clears Content ID, and none reimburse pre-registration losses.** Epidemic and Artlist both say so in terms. Register every channel on day one. Artlist additionally requires **Max Business for companies with 50+ employees** and forbids you from registering assets in Content ID yourself. **[OFFICIAL]**

---

## I. What could not be verified

Recorded here so that nothing in this dossier is mistaken for a confirmed price.

**Prices I could not confirm from a first-party page:**
- **ChatGPT Go** monthly price ($8 is [THIRD-PARTY] only). Plus/Pro prices *are* [OFFICIAL] via the help centre.
- **Google AI plan prices in USD.** Both `one.google.com` and `gemini.google` geolocated to Vietnam and served VND. USD figures ($4.99 / $19.99 / $99.99 / $199.99) are [THIRD-PARTY]. Google's own *blog posts* confirm $19.99, $100 and $200 — but a blog is not a price sheet.
- **Groq per-token pricing for Llama 3.1 8B and Llama 3.3 70B** — the docs now say *"Contact Sales."* Circulating figures are stale [THIRD-PARTY].
- **Kling** recurring and annual subscription prices, and its full per-second credit table.
- **Shutterstock video** — no price obtained at all (page 403s; browser render never painted dollar amounts).
- **Getty / iStock video** — nothing obtained.
- **Artgrid** tier names and monthly prices.
- **Envato Elements** annual prices; **Storyblocks** month-to-month prices.
- **ElevenLabs** overage / pay-as-you-go rates (help-centre article 403s) and per-tier included characters on ElevenAPI other than Creator.
- **Adobe Firefly Services API** pricing — Adobe publishes none; it is enterprise sales only.
- **BFL Open Weights** Builder/Platform tier prices.
- **MiniMax Hailuo** consumer subscription prices.
- **ByteDance Seedance** — no first-party price at all; only Runway's resale rate.

**Live first-party sources that contradict each other (flagged, not resolved):**
- **Epidemic Sound** — the rendered pricing page and the site's own comparison pages give two different price sets.
- **Artlist Max** — pricing card says $50.66/mo annual; Artlist's own blog/help says $29.99/mo annual.
- **Adobe Stock** — two reads of the same page disagree on the entry tier ($29.99 vs $39.99) and on per-video credit cost (8 vs 8–20).
- **Luma** — two live pricing pages with different tiers, prices and commercial-use rules.

**Terms I could not quote from a first-party page:**
- Google Cloud Platform / Vertex AI **output-ownership** clause, and whether **Veo/Imagen output is covered by Google's generative-AI IP indemnity**.
- Azure **output ownership** and the Microsoft Customer Copyright Commitment as applied to Speech.
- **Adobe's indemnification scope** — `helpx.adobe.com/enterprise/using/generative-ai-indemnification.html` returns 404.
- **Lyria** commercial-use terms.
- **Udio** commercial rights per tier; **MiniMax/Hailuo** and **Seedance** commercial-use gating.
- Epidemic Sound **Pro and Business** licence texts (only the Creator licence was fetched).
- **Storyblocks** primary licence text (JS-rendered, returned empty) — in particular **whether use of already-published assets survives cancellation**.
- **Envato** verbatim licence (help centre 403s; the quoted rule is an official-domain paraphrase).

**Rate limits not published by the vendor:**
- **OpenAI** per-model RPM/TPM (docs defer to account settings).
- **Google Gemini** per-model RPM/TPM/RPD (docs defer to AI Studio).
- **Mistral**, **Alibaba Model Studio**, **Together AI** (dynamic, no fixed limits published), **OpenRouter** paid-model limits, **Groq** developer tier.

**Figures in this dossier that are my own estimates, not measurements:**
- The **300 t/s / 500 t/s batched local throughput** used in the TCO (all measured benchmarks in D.1 are single-stream).
- **450 W / 550 W** system wall power.
- All token and character counts in Sections F and G.
- The Vertex **"1 count" = 1 second** reading for Veo pricing.
- Per-character effective rates derived from ElevenLabs credit allowances.

---

*End of dossier. Verified 2026-09-18. Re-run every fetch before committing spend.*

