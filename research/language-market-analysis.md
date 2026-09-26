# Language Market Analysis — English vs Vietnamese for Channel 1

**Prepared:** 2026-09-26
**Question:** Should the company's first YouTube channel be English-language or Vietnamese-language?
**Trigger:** The 2026-09-26 re-verification established that a Vietnam-resident payee (D-007) has **no US tax treaty available**, so US-sourced YouTube earnings face the 30% default withholding. The CEO asked whether D-008 (English) should be reconsidered.

**Answer in one line: No. The ranking does not flip, and it cannot flip within the arithmetic — the Vietnamese RPM required to overtake English exceeds Vietnam's entire measured advertiser CPM. D-008 stands.**

---

## 0. How to read this file

| Label | Meaning |
|---|---|
| **[OFFICIAL]** | First-party page (Google, IRS, Amazon, Companies House). URL and date given. |
| **[THIRD-PARTY]** | A named external measurement or dataset. Source, date and stated methodology given. |
| **[INDUSTRY PRACTICE]** | Widely repeated creator-economy convention with no single authoritative source. |
| **[INFERENCE/ESTIMATE]** | Reasoning or arithmetic performed here. **Every revenue number in this file is an ESTIMATE.** |

**YouTube does not publish RPM.** There is no first-party RPM table and there never has been. Every RPM figure below is [THIRD-PARTY] or [INDUSTRY PRACTICE], the sources disagree with each other by wide margins, and §1.4 says so explicitly rather than averaging the disagreement away.

**All assumptions are named inline with an ID (`A1`…`A9`) and collected in §5.6 so they can be changed and the arithmetic re-run.**

---

## 1. RPM by market

### 1.1 The one dataset with a stated method

**[THIRD-PARTY] Dynamoi, "YouTube AdSense RPM — 84 markets", data through 2026-09-22.**
Source: <https://dynamoi.com/data/youtube-adsense-rpm>
Stated method: *"aggregated, anonymized rates"* from *"connected YouTube Analytics accounts"*, *"normalized to RPM and playback CPM per 1,000 views, updated regularly as new statements arrive."* It does not publish a sample size, a niche breakdown, or a confidence interval. It is the only source found that states where its numbers come from at all, which is why it anchors this analysis.

| Market | Creator RPM (USD/1,000 views) | Playback CPM |
|---|---|---|
| Australia | $7.31 | $15.19 |
| Canada | $5.39 | $10.80 |
| United States | $5.36 | $12.99 |
| United Kingdom | $5.21 | $12.20 |
| Philippines | $0.74 | $1.46 |
| Thailand | $0.38 | $1.49 |
| **Vietnam** | **$0.18** | **$1.11** |
| Indonesia | $0.14 | $1.04 |

**The headline ratio: US RPM / Vietnam RPM = 5.36 / 0.18 ≈ 29.8×.** [INFERENCE/ESTIMATE from the table above]

India is absent from the 84 published markets as at the collection date — noted so the gap is not mistaken for a zero.

### 1.2 RPM by niche (this varies more than country does)

**[THIRD-PARTY] MilX, "YouTube CPM and RPM rates in 2026", dated 2026-03-16.**
Source: <https://milx.app/en/trends/youtube-cpm-rpm-rates-2026-average-niches-countries-more>
Stated method: *"real-world YouTube RPM by niche"* from *"real MilX data"*. **No sample size or collection method is disclosed.** Treat as an ordered ranking that is probably right, with magnitudes that are probably soft.

| Niche | CPM | RPM |
|---|---|---|
| Personal Finance | $15–$40 | **$5–$20** |
| Business & Marketing | $12–$35 | **$5–$15** |
| Tech & Productivity | $10–$30 | **$4–$12** |
| Education & How-to | $6–$22 | **$3–$8** |
| Fitness & Wellness | $5–$18 | $2–$7 |
| Fashion & Beauty | $4–$15 | $1.50–$6 |
| Gaming | $2–$9 | $0.50–$4 |
| Entertainment & Vlogs | $2–$10 | $0.50–$3 |

**[THIRD-PARTY] Corroboration, vidIQ (2026):** *"Finance, insurance, and legal niches pay the highest YouTube CPMs ($15–50), while gaming and entertainment sit at $1-8"*; personal finance carries *"creator-reported RPMs of $10 to $20 on long-form educational videos."* Source: <https://vidiq.com/blog/post/most-profitable-youtube-niches/>

**Range is 10–40× from bottom niche to top niche — wider than the country spread within Tier 1.** Niche choice is a bigger RPM lever than anything else on this page, *within* a language. It is not a lever that can close a 30× country gap.

### 1.3 A caution the CEO should weigh against every figure above

**[OFFICIAL POLICY] The AI-persona rule constrains which of those niches are available at all.** Per `reverification-2026-09-26.md` item 9, re-verified verbatim 2026-09-26: channels using AI personas on **health, legal issues, finances or politics** *"will not be allowed to monetize."* Source: <https://support.google.com/youtube/answer/14328491>

**[INFERENCE]** The company's plan is AI-operated with synthetic narration. **The three highest-RPM niches in §1.2 — personal finance, business/finance, legal — are therefore substantially or wholly out of reach.** The realistically addressable band for this company is **Education & How-to ($3–$8) and Tech & Productivity ($4–$12)**, not the $10–$20 personal-finance top of market. This analysis models the addressable band, not the headline band. **This constraint applies equally to both language options and so does not change the ranking — but it does change the absolute numbers, and it is the reason this file does not quote $15 RPM anywhere.**

### 1.4 Where the sources disagree, and what is done about it

They disagree badly on Tier 1, and the disagreement is not resolvable from public material:

| Source | US figure | Note |
|---|---|---|
| Dynamoi (2026-09-22) | RPM **$5.36**, playback CPM **$12.99** | Method stated |
| MilX (2026-03-16) | CPM **~$14.67** | Close to Dynamoi's CPM |
| YTface (Feb 2026) | CPM **$36.03**, derived RPM ~$19.82 | Method: *"Industry averages + advertiser demand modeling"* — i.e. modelled, not measured. <https://www.ytface.com/cpm-rates-by-country> |
| TubeAnalytics (2026) | Median RPM **~$2.30** across 300 channels; median channel monetized only **53%** of views | All-niche, all-geography median |

**[INFERENCE] YTface's $36.03 US CPM is an outlier by ~2.8× against two other 2026 sources and is self-described as modelled. It is recorded here and not used.** Dynamoi's CPM ($12.99) and MilX's ($14.67) agree to within 13%, so the CPM side of the market is reasonably well-triangulated; the RPM side is not.

**[THIRD-PARTY] The TubeAnalytics median of $2.30 is the most sobering number in this section** and deserves attention: it is a *median across real channels*, including their non-monetized views and their rest-of-world audience. It is roughly half of Dynamoi's US-market RPM, because Dynamoi reports RPM *for views from that market* whereas $2.30 is RPM *for a whole channel's mixed audience*. **These two numbers are consistent, not contradictory — and the blended-RPM model in §5 is precisely the bridge between them.** The model in §5 lands at $2.66–$3.20 blended, which sits just above the $2.30 median. That is a reassuring sanity check, not a coincidence: it says the model is not optimistic.

**[INDUSTRY PRACTICE] Conversion convention:** YouTube retains 45% of long-form ad revenue and pays the creator 55%, so creator RPM ≈ 0.55 × playback CPM × (share of views that served an ad). Widely stated across all sources above; consistent with Dynamoi's own US pair ($5.36 / $12.99 = 0.41, implying ~75% monetized playback). This convention is used once, in §6.2, to establish a **ceiling** on Vietnamese RPM — not to generate a forecast.

### 1.5 Niche-adjusted RPMs used in the model

**[INFERENCE/ESTIMATE]** Combining §1.1 (country) with §1.2 (niche, restricted to the addressable band per §1.3):

| Parameter | Value | Derivation |
|---|---|---|
| `A1` US RPM, education/tech explainer | **$6.00** | Dynamoi US all-niche $5.36, uplifted modestly toward the MilX Education ($3–8) / Tech ($4–12) overlap. |
| `A2` Other Tier-1 English RPM (UK/CA/AU/IE/NZ) | **$5.50** | Dynamoi UK $5.21, CA $5.39, AU $7.31 → mean $5.97; discounted for the smaller markets not individually measured. |
| `A3` Rest-of-world RPM (English content) | **$0.60** | Between Dynamoi's Philippines ($0.74) and Thailand ($0.38) / Indonesia ($0.14); English-language RoW views skew toward higher-income non-Tier-1 markets (Germany, Netherlands, Nordics, Singapore, Gulf) so the upper end is used. |
| `A4` Vietnam in-country RPM, education/tech | **$0.35** | Dynamoi Vietnam all-niche **$0.18**, uplifted ~1.9× for niche — the same relative uplift applied to the US in `A1`. **Deliberately generous to the Vietnamese case.** |

**Every one of `A1`–`A4` is an ESTIMATE.** `A4` is set high on purpose: if the Vietnamese option still loses on a generous assumption, the conclusion is robust.

---

## 2. Typical audience geography for an English-language channel

This is the parameter that sizes the withholding haircut, so it is handled carefully and given as a range, not a point.

### 2.1 What can actually be established

**[THIRD-PARTY] The US is ~9.6% of global YouTube users.** 259 million US users against *"over 2.70 billion monthly active users worldwide as of 2026"* — 259/2,700 = 9.6%. Global Media Insight, dated 2026-06-26, citing Statista / DataReportal / YouTube reports. <https://www.globalmediainsight.com/blog/youtube-users-statistics/> (Same source: India 518M, Indonesia 151M, Brazil 149M, Vietnam 62.1M, Philippines 59.6M.)

**[THIRD-PARTY] The US is ~21% of global YouTube traffic.** *"In April 2026, 21.07% of YouTube traffic came from the United States."* Reported via Sprout Social's 2026 statistics roundup. <https://sproutsocial.com/insights/youtube-stats/>

**[INFERENCE] These two figures are consistent and informative together:** the US is 9.6% of users but 21% of traffic, so US users watch roughly 2.2× the global per-user average. The 21% figure is the right global baseline for a *view-weighted* share.

**[OFFICIAL — no benchmark exists]** YouTube publishes no benchmark for the geographic distribution of a new channel's views. Its own documentation only tells creators where to find their own figure (Studio → Audience → Top geographies). Several third-party guides were checked (Growati 2026-08-08, TubeRanker, TubeAnalytics) and **none publishes a cross-channel distribution** — Growati offers only the hypothetical *"If two thirds of your watch time is North American."* **This is a genuine evidence gap and is stated as one rather than filled with a fabricated benchmark.**

### 2.2 The range used, and why

**[INFERENCE/ESTIMATE]** An English-language channel over-indexes to the US relative to the 21% global baseline, because ~79% of global traffic includes the entire non-English-speaking world. But it does *not* approach 100%, because English is the second language of a very large share of YouTube's audience and YouTube's recommendation surface is global by default.

| Scenario | US | Other Tier-1 English (UK/CA/AU/IE/NZ) | Rest of world | Rationale |
|---|---|---|---|---|
| **Low-US** | 15% | 20% | 65% | Below the 21% global baseline; realistic for a channel whose topic travels internationally (tech, science, how-to) and that gets early traction in India/SEA/Philippines, where English comprehension is high and YouTube usage is enormous. |
| **`A5` Central** | **30%** | **20%** | **50%** | ~1.4× the global 21% baseline. |
| **High-US** | 45% | 20% | 35% | A channel on US-specific subject matter with US-centric SEO. Above this is achievable but not a planning assumption for a channel with no publication history. |

**Two things make the low end more likely than creators expect, and both are worth the CEO's attention:**

1. **[THIRD-PARTY]** Vietnamese creators' own channels already get *"more than 50% of the total watch time for content produced by Vietnamese channels"* from **international** audiences (Google, reported 2026-09-18 following the first YouTube Festival in Vietnam, 2026-09-16). <https://en.vneconomy.vn/vietnam-has-over-180000-youtube-channels-with-more-than-10000-subscribers.htm> — a Vietnam-operated channel's distribution is not confined to its home market in either direction.
2. **[INFERENCE]** A new channel has no subscriber base, so nearly all early views arrive through browse/suggested and search — surfaces that are language-matched but not country-matched. Geographic concentration typically *increases* as a subscriber base forms, not at launch.

**§6.1 shows that this uncertainty does not matter to the decision**, which is the most useful thing about it.

---

## 3. The withholding mechanics — first-party

All of §3 is **[OFFICIAL]**, read 2026-09-26.

### 3.1 What is withheld and on what base

Source: <https://support.google.com/youtube/answer/10391362>

- Scope: withholding applies *"on YouTube earnings from viewers in the U.S. from ad views, YouTube Premium, Super Chat, Super Stickers, Super Thanks, and Channel Memberships."*
- With US tax info submitted: *"tax withholding rates are between 0-30% on earnings you get from U.S. viewers"*, depending on treaty eligibility and country of residence.
- With US tax info submitted and **no treaty**: *"the tax rate without a tax treaty is 30% of earnings from viewers in the U.S."*

**The base is US-viewer-sourced earnings only.** Views from the UK, Canada, Australia, India, Germany, the Philippines and everywhere else are **outside the base entirely**. This is the single most important fact in the file and it is first-party.

### 3.2 Vietnam has no treaty

**[OFFICIAL]** The IRS A-to-Z list of United States income tax treaties **does not include Vietnam**. Under V it lists **Venezuela only**. Read 2026-09-26. <https://www.irs.gov/businesses/international-businesses/united-states-income-tax-treaties-a-to-z>

**[INFERENCE]** Read with §3.1: a Vietnam-resident payee who correctly submits US tax info has no treaty rate available and lands on the **30% default, applied to the US-viewer share only**. Neither source states this combination; it is reasoning from the two quoted first-party pages. This confirms the finding already recorded in `reverification-2026-09-26.md` §4.1.

### 3.3 What must be submitted

**[OFFICIAL]** AdSense → **Payments → Payments info → Manage settings** → scroll to **"Payments profile"** → edit next to **"United States tax info"** → **"Manage tax info"**, then select the applicable form. For a non-US individual this is the **W-8BEN**. Source: <https://support.google.com/youtube/answer/10391362>, corroborated at <https://support.google.com/youtube/answer/10390801>

### 3.4 The non-submission penalty — materially worse, and worth stating plainly

**[OFFICIAL]** If no tax info is submitted, *"Google may be required to withhold using the maximum tax rate"*:

| Payee type | Rate if no tax info submitted | Base |
|---|---|---|
| **Individual** | **24%** | ***total earnings worldwide*** |
| Business | 30% | US earnings |

**[INFERENCE/ESTIMATE] Why this matters more than the 30% headline.** Under the central case (§5.2), correct submission costs $0.54 per 1,000 views, which is **16.9% of gross revenue**. Non-submission as an individual costs **24% of gross revenue** — about **1.4× worse**, and it applies to every UK, Canadian, Indian and German view as well.

**Worse still at low US share.** In the low-US scenario (15% US), correct submission costs ~9.5% of gross; non-submission still costs 24%. **There, failing to file is ~2.5× worse than the treaty-less rate.**

> **This is the one genuinely actionable finding in the file, and it applies identically whichever language is chosen. Submitting the W-8BEN in AdSense is free, takes minutes, and is worth 7–15 percentage points of gross revenue. It must be done before the first payment threshold is reached.**

### 3.5 A correction to the framing, made honestly

The brief frames the comparison as *"a partial haircut on a high RPM versus no meaningful haircut on a much lower RPM — not 70% versus 100%."* **That framing is correct in structure and this analysis carries it through. But one refinement is owed:**

**[INFERENCE/ESTIMATE] The US share of *revenue* is much larger than the US share of *views*, because US views are the high-RPM ones.** In the central case, US views are 30% of views but **56% of gross revenue** ($1.80 of $3.20). So the effective haircut on total revenue is **16.9%**, not the ~9% one would get by naively multiplying 30% × 30%.

The haircut is real, and larger than a casual reading suggests. **It is still nowhere near large enough to change the answer** — §6 shows why.

---

## 4. Would a treaty-country entity change it, and at what cost

> **This is not tax advice, and nothing in this section should be acted on without qualified tax counsel in both Vietnam and the proposed jurisdiction.** What follows establishes the *shape of the trade* so the CEO can see whether it is even worth paying counsel to explore. No structure is recommended.

### 4.1 What rate a treaty typically obtains

**[OFFICIAL]** Google's own page states the range is *"between 0-30%"* with a treaty claim. <https://support.google.com/youtube/answer/10391362>

**[INDUSTRY PRACTICE]** For the *"Other Copyright Royalties"* category that covers YouTube Partner Program earnings, most major-economy treaties (UK, Ireland, Netherlands, Germany, Australia, Canada) yield a **0% withholding rate**; some yield an intermediate rate — **India's treaty gives 15%**, for example. The W-8BEN treaty section offers three checkboxes — *services*, *motion picture & TV royalties*, *other copyright royalties* — and industry guidance is to claim all three where the treaty permits.

**[INFERENCE] So the ceiling on what an entity can save is the whole 30% — up to 0%, not to some middling rate.** That is the most favourable possible reading of the entity case, and §4.3 still finds against it.

### 4.2 Two traps worth naming before anyone pays for advice

1. **[OFFICIAL] The obvious "offshore" jurisdictions do not work.** The IRS treaty list contains **neither Singapore nor Hong Kong**. An entity in either has exactly the same 30% exposure as Vietnam. Jurisdictions that *are* on the list and are commonly used include the **United Kingdom, Ireland, the Netherlands, Australia and Canada** — all substantive, all with real compliance cost. <https://www.irs.gov/businesses/international-businesses/united-states-income-tax-treaties-a-to-z>
2. **[THIRD-PARTY] A registered address is not substance.** *"A Companies House registered office satisfies the statutory requirement but does not constitute substantive presence… Virtual office services… are generally not sufficient for HMRC substance purposes on their own."* (UK formation-agent guidance, 2026.) A treaty claim rests on the entity being tax-resident in the treaty country, which is a facts-and-circumstances question about where it is actually managed — and it would be managed from Vietnam. **This is precisely the question for counsel and it is the one most likely to sink the structure.**

### 4.3 What it costs — and the break-even

**[THIRD-PARTY] Indicative UK Ltd annual cost, 2026 UK accountancy market:**

| Component | Annual |
|---|---|
| Year-end accounts + CT600 + Companies House filing, straightforward company | £500–£1,500 + VAT |
| Basic ongoing compliance package | £800–£1,500 |
| Full service (VAT, payroll, planning) | £3,000+ |

Sources: <https://sleek.com/uk/resources/limited-company-accountant-cost/>, <https://ltd-companies.co.uk/resources/accountant-for-limited-company-cost-uk/>

**[OFFICIAL/THIRD-PARTY] Plus:** a UK registered office, a director service address, and **director identity verification — existing directors must verify by 18 November 2026**, with failure a criminal offence that blocks the confirmation statement and can lead to strike-off.

**[INFERENCE/ESTIMATE]** Call the all-in range **USD $1,300–$4,000/year = $108–$333/month**, excluding the cost of the counsel needed to determine whether the treaty claim survives §4.2 at all, and excluding any Vietnamese tax consequence of the structure.

**The break-even — this is the number that closes the question:**

The entity saves the withholding, i.e. **$0.54 per 1,000 views** in the central case (§5.2).

```
Break-even monthly views = entity monthly cost ÷ $0.54 per 1,000 views × 1,000

  At $108/month (cheapest end):   108 ÷ 0.54 × 1,000 =   200,000 views/month
  At $333/month (full service):   333 ÷ 0.54 × 1,000 =   616,667 views/month
```

**[INFERENCE/ESTIMATE] Against the ~29,100 views/month needed to cover the $77.41 cost envelope (§5.4), a treaty entity does not pay for itself until roughly 7× to 21× that volume — 200,000 to 617,000 views per month, or 15,000–47,000 views per video at 13 videos/month.** At the envelope-break-even volume the entity *saves* about **$15.72/month** against a cost of $108–$333/month: it is **net negative by $92 to $317 every month**.

**Conclusion of §4: the entity question is not close, and it is not a launch question. Revisit it only if the channel sustainably clears ~200,000 views/month, and even then only with counsel on §4.2. Do not spend money on it now.**

---

## 5. The worked comparison

Every line below is arithmetic the reader can re-run. Assumptions carry IDs and are collected in §5.6.

### 5.1 The model

```
For each audience segment i:
    gross_RPM       = Σ ( view_share_i × RPM_i )
    withheld        = 0.30 × ( view_share_US × RPM_US )        ← US segment ONLY
    net_RPM         = gross_RPM − withheld
    views_to_cover  = 77.41 ÷ net_RPM × 1,000
```

Withholding is applied **only** to the US line. That is the whole point of the exercise.

### 5.2 Option A — English (central case)

| Segment | View share | RPM | Gross revenue per 1,000 views |
|---|---|---|---|
| United States | 30% (`A5`) | $6.00 (`A1`) | 0.30 × 6.00 = **$1.80** |
| Other Tier-1 English | 20% (`A5`) | $5.50 (`A2`) | 0.20 × 5.50 = **$1.10** |
| Rest of world | 50% (`A5`) | $0.60 (`A3`) | 0.50 × 0.60 = **$0.30** |
| | | **Gross RPM** | **$3.20** |

```
US-sourced revenue        = $1.80   ( = 56.3% of gross revenue, from 30% of views )
Withholding @ 30%         = 0.30 × $1.80              = $0.54
NET RPM (English)         = $3.20 − $0.54             = $2.66
Effective haircut         = $0.54 ÷ $3.20             = 16.9% of gross
```

**→ Option A: $2.66 net revenue per 1,000 views. [INFERENCE/ESTIMATE]**

### 5.3 Option B — Vietnamese (central case)

Vietnamese-language content is **not** 100% Vietnam-resident audience. Per §2.2, Google reports >50% of Vietnamese-creator watch time comes from overseas. Part of that overseas audience is the US-resident Vietnamese diaspora — **whose views are US-sourced and therefore also withheld.** The Vietnamese option is not withholding-free.

| Segment | View share (`A6`) | RPM | Gross revenue per 1,000 views |
|---|---|---|---|
| Vietnam (in-country) | 70% | $0.35 (`A4`) | 0.70 × 0.35 = **$0.245** |
| US (Vietnamese diaspora) | 5% | $2.50 (`A7`) | 0.05 × 2.50 = **$0.125** |
| Other overseas | 25% | $0.40 (`A8`) | 0.25 × 0.40 = **$0.100** |
| | | **Gross RPM** | **$0.470** |

```
US-sourced revenue        = $0.125
Withholding @ 30%         = 0.30 × $0.125            = $0.0375
NET RPM (Vietnamese)      = $0.470 − $0.0375         = $0.4325  ≈ $0.43
Effective haircut         = 8.0% of gross
```

**→ Option B: $0.43 net revenue per 1,000 views. [INFERENCE/ESTIMATE]**

`A7` ($2.50 for US diaspora views) is set well below the $6.00 English US RPM because advertiser competition for **Vietnamese-language ad inventory served in the US** is a fraction of that for English-language US inventory. It is an estimate with no direct source and is flagged as the weakest number in the model — but it is generous to Option B and moving it does not change anything (§6.3).

### 5.4 Side by side

| | **Option A — English** | **Option B — Vietnamese** |
|---|---|---|
| Gross RPM | $3.20 | $0.47 |
| US-sourced share of **views** | 30% | 5% |
| US-sourced share of **revenue** | 56.3% | 26.6% |
| Withholding @ 30% | $0.54 | $0.04 |
| Effective haircut on gross | **16.9%** | **8.0%** |
| **Net RPM after withholding** | **$2.66** | **$0.43** |
| **Ratio** | **6.15× Option B** | — |

**Monthly views required to cover the USD 77.41 envelope (13 videos/month, D-006/D-007 cost base, unchanged at first-party 2026-09-26):**

```
Option A (English):     77.41 ÷ 2.66 × 1,000 =  29,102 views/month  =  2,239 views/video
Option B (Vietnamese):  77.41 ÷ 0.43 × 1,000 = 180,023 views/month  = 13,848 views/video
```

**[INFERENCE/ESTIMATE] The Vietnamese channel must attract 6.2× the audience to reach the same break-even.** At 13 videos/month it needs ~13,850 views per video against ~2,240 — the difference between a plausible early-channel result and a result that would already be a success.

### 5.5 Sanity check against an independent benchmark

The model's English gross RPM of **$3.20** sits just above TubeAnalytics' **$2.30 median RPM across 300 real channels** (§1.4) — which is an all-niche, all-geography, all-monetization-rate median including entertainment and gaming channels. **A niche-restricted education/tech channel landing ~39% above the all-niche median is plausible and not aggressive.** The model is not flattering Option A.

### 5.6 Assumption register — change these and re-run

| ID | Assumption | Value | Confidence | Effect if wrong |
|---|---|---|---|---|
| `A1` | US RPM, education/tech explainer | $6.00 | Medium | See §6.1 — ranking is insensitive |
| `A2` | Other Tier-1 English RPM | $5.50 | Medium | Minor |
| `A3` | Rest-of-world RPM, English content | $0.60 | **Low** | Moderate — see §6.1 `x = 0` row |
| `A4` | Vietnam in-country RPM, niche-adjusted | $0.35 | Medium (set generously) | See §6.2 — **capped by CPM** |
| `A5` | English geography split 30/20/50 | — | **Low** (no benchmark exists) | **None — see §6.1** |
| `A6` | Vietnamese geography split 70/5/25 | — | Low | Minor |
| `A7` | US RPM on Vietnamese-language inventory | $2.50 | **Very low** | See §6.3 — bounded |
| `A8` | Other-overseas RPM, Vietnamese content | $0.40 | Low | Minor |
| `A9` | Cost envelope | $77.41/month | **High** — [OFFICIAL], all 12 unit prices re-verified 2026-09-26 | Scales break-even linearly |

---

## 6. Sensitivity — where does the ranking flip?

### 6.1 Flip on US-audience share: it does not, and it *cannot*

Hold `A1`–`A3` and other-Tier-1 at 20%, and let US share `x` vary with rest-of-world absorbing the change:

```
gross_RPM(x) = 6.00x + (0.20 × 5.50) + 0.60 × (0.80 − x)
             = 6.00x + 1.10 + 0.48 − 0.60x
             = 5.40x + 1.58

withheld(x)  = 0.30 × 6.00x = 1.80x

net_RPM(x)   = 5.40x + 1.58 − 1.80x
             = 3.60x + 1.58          ← coefficient on x is POSITIVE
```

| US share `x` | Gross RPM | Withheld | **Net RPM** | Views/month to cover $77.41 |
|---|---|---|---|---|
| 0% | $1.58 | $0.00 | **$1.58** | 48,994 |
| 10% | $2.12 | $0.18 | **$1.94** | 39,902 |
| 15% (low-US) | $2.39 | $0.27 | **$2.12** | 36,514 |
| **30% (central)** | **$3.20** | **$0.54** | **$2.66** | **29,102** |
| 45% (high-US) | $4.01 | $0.81 | **$3.20** | 24,191 |
| 60% | $4.82 | $1.08 | **$3.74** | 20,698 |

**Two findings, and the second is the important one:**

1. **Across the entire plausible range, Option A's net RPM ($2.12–$3.20) never comes within 4.9× of Option B's $0.43.** There is no US-share value in the table at which the ranking flips.
2. **`net_RPM` is strictly *increasing* in US share (slope +3.60).** More US audience is unambiguously better **even after the 30% withholding**, because 70% of a $6.00 US view ($4.20) still overwhelmingly beats the $0.60 rest-of-world view it displaces. **Withholding never makes a US view undesirable, and it never will while US RPM exceeds ~1.43× the rest-of-world RPM** (the point at which 0.70 × RPM_US = RPM_RoW). The actual ratio is 10×.

**[INFERENCE] This dismantles the intuition that prompted the question.** The 30% is not a reason to steer away from the US audience. It is a tax on the best asset the channel has.

**Extreme test:** even at `x = 0` — an English channel with **literally zero US views and therefore zero withholding** — net RPM is **$1.58**, still **3.7× Option B's $0.43**. The ranking survives the complete removal of the US audience.

### 6.2 Flip on Vietnamese RPM: arithmetically impossible

Solve for the Vietnam in-country RPM `r` that would make Option B match Option A:

```
net_RPM_B(r) = 0.70r + [0.05 × 2.50 × 0.70] + [0.25 × 0.40]
             = 0.70r + 0.0875 + 0.100
             = 0.70r + 0.1875

Match central Option A ($2.66):   0.70r = 2.4725  →  r = $3.53
Match Option A at 0% US ($1.58):  0.70r = 1.3925  →  r = $1.99
```

**Required: a Vietnam in-country RPM of $1.99–$3.53.** Now test that against measurement:

```
Measured Vietnam creator RPM        (Dynamoi, 2026-09-22)  =  $0.18
Measured Vietnam playback CPM       (Dynamoi, 2026-09-22)  =  $1.11
Absolute theoretical ceiling on Vietnam RPM
   = 0.55 × $1.11  (creator share × CPM, 100% of views serving ads)
   = $0.61
```

> **The flip requires a Vietnamese RPM of $1.99 at the absolute minimum. The theoretical ceiling on Vietnamese RPM — the figure that would obtain if every single view served an ad at the measured advertiser CPM — is $0.61. The required RPM exceeds the entire amount advertisers pay for that inventory, by a factor of 3.3× at best and 5.8× at the central case.**

**[INFERENCE] This is not a close call that could be moved by a better estimate. It is a structural impossibility given the measured CPM.** For the ranking to flip, Vietnamese advertiser demand would have to roughly quintuple — a change in the Vietnamese advertising market, not a change in this channel's strategy.

**Even at the ceiling:** run Option B at the impossible-best `r = $0.61`:
```
net_RPM_B = 0.70 × 0.61 + 0.1875 = $0.615
```
**$0.615 against Option A's worst case of $1.58 — Option A still wins by 2.6×.**

### 6.3 Flip on the weakest assumption (`A7`, US diaspora RPM)

`A7` is the least-evidenced number in the file. Bound it: even if Vietnamese-language US inventory earned the **full English US RPM of $6.00** (implausible, but it bounds the case):

```
net_RPM_B = 0.70 × 0.35 + [0.05 × 6.00 × 0.70] + 0.100
          = 0.245 + 0.210 + 0.100 = $0.555
```
**$0.56 against Option A's $2.66. No flip.** `A7` cannot carry the decision.

### 6.4 Flip on cost envelope

The $77.41 envelope (`A9`, [OFFICIAL], all 12 unit prices re-verified 2026-09-26 per `reverification-2026-09-26.md` §3) **scales the break-even volume linearly and affects both options identically.** It cannot change the ranking. It only changes how many views each option needs.

### 6.5 The honest summary of §6

**There is no combination of plausible values for `A1`–`A8` under which the Vietnamese option produces more revenue per view than the English option.** The ranking holds:

- at 0% US audience share (no withholding at all),
- at the theoretical maximum Vietnamese RPM permitted by the measured Vietnamese CPM,
- and at both simultaneously ($1.58 vs $0.615 — still 2.6×).

**The gap is ~6× at central values and never narrows below ~2.6× under any stress applied here. The withholding finding, while real and correctly established, is roughly 17% of gross revenue on a revenue base that is 6× larger. It does not come close to closing a 6× gap. The earlier decision stands.**

---

## 7. Non-revenue factors

### 7.1 Competition and content saturation

| | English | Vietnamese |
|---|---|---|
| **Audience pool** | ~2.7bn global YouTube users, majority reachable in English as first or second language [THIRD-PARTY, GMI 2026-06-26] | 62.1M Vietnamese YouTube users; YouTube reaches **93% of Vietnamese internet users daily** [THIRD-PARTY, Google via VnEconomy 2026-09-18] |
| **Supply** | Vast. The most saturated content market on earth. | **>180,000 Vietnamese channels with 10,000+ subscribers** [THIRD-PARTY, Google, 2026-09-18] — dense relative to a 62M-user market |
| **Verdict** | Harder to break in; the addressable ceiling is ~40× larger | Easier to break in; the ceiling is low and, per §6.2, monetizes at ~3% of the English rate |

**[INFERENCE] Saturation genuinely favours Vietnamese, and this is the strongest argument on Option B's side. It is also not decisive, for a reason worth stating plainly:** Vietnam has 180,000+ channels above 10k subscribers competing for a 62M-user market that pays $0.18 RPM. That is **not an empty market — it is a crowded small one.** The saturation advantage is smaller than it first appears, and it is bought at a 6× revenue penalty per view. A channel would need the saturation advantage to deliver >6× the views to break even on the trade — and §5.4 says that means ~13,850 views per video rather than ~2,240.

**[THIRD-PARTY] Partial counter to the saturation argument for English:** creator-economy research consistently identifies *"English-language content covering non-Western cultures, products, and experiences"* as persistently undersupplied (OutlierKit, 2026). **[INFERENCE] The CEO's Vietnam base is a content asset in the English market, not only in the Vietnamese one** — a route that captures the Tier-1 RPM while differentiating on perspective. This is the strategically interesting reading of the situation and is picked up in §8.

### 7.2 The CEO's native-language advantage

**[INFERENCE] This is real but largely neutralised by the operating model, and that deserves saying clearly.**

Per D-002, the CEO's role is **per-video approval**, not writing. Per D-006, the pipeline is AI-generated script over licensed stock with synthetic narration. Native fluency would matter enormously for a human-presented channel; in an AI-operated pipeline it converts mainly into **better editorial judgement at the approval gate** — catching tonal errors, cultural misfires and low-quality phrasing that an English-language review might miss.

**Against that, three things push the other way:**
- **[OFFICIAL]** Vietnamese-language synthetic narration quality and Vietnamese-language LLM script quality are both materially behind English at every provider in the routed stack. No first-party benchmark was located for the specific models in `ai-capacity-dossier.md`, so **this is [INFERENCE] and should be tested before it is relied on** — but it is the consistent direction of the evidence.
- The approval-gate advantage is **symmetric in cost**: reviewing Vietnamese is easier for the CEO, but the CEO already reviews English professionally (this decision record is in English).
- **[INFERENCE]** The native-language advantage would be decisive if the constraint were *production quality*. The constraint identified here is *monetization rate*, which fluency does not touch.

### 7.3 Fact-checking and sourcing availability — this is a substantive finding, not a footnote

**[THIRD-PARTY] Vietnam ranks 174th of 180 in the RSF 2026 World Press Freedom Index**, score 21.15, **last in Southeast Asia** — behind Myanmar (166), Laos (154) and Cambodia (151). RSF: *"independent journalism is effectively banned"*; authorities use penal code Articles 109, 117 and 331 to prosecute journalists and bloggers for offences such as *"anti-state propaganda."* <https://rsf.org/en/country/vietnam>; reported at <https://thevietnamese.org/2026/04/viet-nam-ranks-last-in-southeast-asia-2026-world-press-freedom-index/>

**[INFERENCE] Three operational consequences, and they compound:**

1. **The Vietnamese-language sourcing pool for a fact-checked explainer format is structurally thin.** The company's format (D-006) depends on citable, verifiable sources. Vietnamese-language sources are predominantly state-aligned; independent Vietnamese-language outlets exist largely in diaspora and carry their own reliability questions. English-language sourcing — journals, government statistics offices, standards bodies, wire services, court records — is the deepest in the world and is what the routed research stack (Brave Search at $0.005/search, `reverification-2026-09-26.md` U-3) is effectively tuned for.
2. **Vietnamese-language content faces a legal risk surface English-language content does not.** A Vietnam-resident operator publishing Vietnamese-language content on current affairs, health or economics is publishing into a jurisdiction with an active prosecution record for exactly that. **This is a risk to the operator, not only to the channel.**
3. **This interacts badly with the §1.3 AI-persona rule.** The highest-RPM niches — finance, health, legal, politics — are already closed to an AI-operated channel by YouTube policy. In Vietnamese they are *additionally* constrained by Vietnamese law and by sourcing scarcity. **The addressable topic space is narrower in Vietnamese than in English, on top of being worth ~3% as much per view.**

**[OFFICIAL — carried gap]** Vietnamese advertising-law disclosure obligations for sponsored/affiliate content, and any Vietnamese rule on AI-media disclosure, remain **unestablished** (`reverification-2026-09-26.md` §4.2). A Vietnamese-language channel aimed at Vietnamese viewers would make that gap **immediately load-bearing**; an English-language channel aimed at Tier-1 audiences leaves it a background item. **Choosing Vietnamese would promote an unresolved legal gap onto the critical path before launch.**

### 7.4 Affiliate — where the gap genuinely narrows

Affiliate is in the launch revenue model (D-005) and **earns before YPP thresholds are met**, so in year one it may matter more than ad RPM. This is the one section where Option B is competitive, and it is presented as such.

**English route — Amazon Associates US:**
- **[OFFICIAL]** The US Operating Agreement contains **no country-of-residence restriction**; the only geographic bar is US sanctions (§4). <https://affiliate-program.amazon.com/help/operating/agreement>
- **[OFFICIAL]** *"The US Internal Revenue Service requires Amazon to withhold up to 30% from non-US associates earning referral commissions"*, reducible only by *"a completed W-8BEN with a valid claim of treaty benefits"* — **which Vietnam does not have.** <https://affiliate-program.amazon.com/help/node/topic/GPFZ6W6CF4E5BD9V>
- **[THIRD-PARTY]** Vietnam is **not** among the ~52 countries with direct local bank transfer; practical routes are Payoneer (virtual USD account), cheque, or Amazon gift card. Payoneer adds fees and FX spread.
- **[THIRD-PARTY]** 3 qualifying sales within 180 days of signup are required to keep the account active.

> **[INFERENCE] An important asymmetry the CEO should see: unlike YouTube ad revenue, Amazon Associates commissions may be treated as *wholly* US-sourced, so the 30% would apply to 100% of that line rather than to a ~56% slice.** Whether commissions earned by a non-US person performing all activity outside the US are in fact US-source income is **genuinely contested and is a counsel question** — Amazon's own wording is the hedged *"up to 30%"*. **This is the one place in the whole analysis where the withholding finding bites harder on the English option than the headline suggests, and it is recorded rather than buried.**

**Vietnamese route — Shopee / Lazada / TikTok Shop Vietnam:**
- **[THIRD-PARTY]** Shopee affiliate commissions run ~1–8% by category (electronics at the bottom, fashion/beauty at the top), up to ~10% on livestream-driven direct orders. Lazada runs ~1–10%, up to ~12% for newly acquired customers. TikTok Shop ~5–15%.
- **[THIRD-PARTY]** Shopee introduced a **5% technical support fee across Singapore, Malaysia, Thailand and Vietnam in February 2026**.
- **[INFERENCE]** Paid in VND to a local bank, **no US withholding, no Payoneer friction, no W-8BEN** — operationally much simpler for a Vietnam-resident payee.

**[INFERENCE/ESTIMATE] Illustrative per-conversion comparison — ROUGH, and label it as such:**

```
English / Amazon US:
  $60 basket × 3% commission            = $1.80
  less 30% withholding (if US-source)   = $1.26 net    [and possibly less, after Payoneer/FX]

Vietnamese / Shopee VN:
  $25 basket × 5% commission            = $1.25
  less 5% technical support fee         = $1.19 net    [no withholding]
```

**[INFERENCE] Per conversion, the two are roughly comparable — this is the one metric on which the Vietnamese option is not badly beaten.** But two things still favour English on the affiliate line: the **conversion pool** (Amazon US serves the highest-disposable-income consumer market on earth against a far larger addressable audience), and **basket-size growth** (US baskets scale into high-ticket categories where a 3% commission is $15–$30; Vietnamese e-commerce baskets do not).

**[OFFICIAL — carried gap]** Whether affiliate links alone trigger YouTube's paid-promotion declaration **remains unestablished** (`reverification-2026-09-26.md` §4.2) — it is the one carried gap directly load-bearing on the revenue model. The documented operating posture is unchanged and applies to **both** options: **declare paid promotion in Studio and disclose in-video regardless**, since the declaration is free and costs neither reach nor revenue.

**[OFFICIAL]** YouTube announced on 2026-09-23 that it is *"expanding our affiliate program to 35 countries by the end of the year"*, with YouTube Shopping tags said to *"drive more than twice as many clicks as adding links in the description."* <https://blog.youtube/news-and-events/made-on-youtube-creator-monetization-shopping/> — **the post does not establish whether Vietnam is among the 35**, and the plan's affiliate revenue does not depend on it. Worth re-checking at the 2026-10-26 pass.

### 7.5 Non-revenue factors, scored

| Factor | Favours | Weight | Note |
|---|---|---|---|
| Ad RPM | **English, ~6×** | **Decisive** | §5, §6 |
| Addressable audience size | **English, ~40×** | High | 2.7bn vs 62M |
| Competition / saturation | **Vietnamese** | Medium | Real, but a crowded *small* market (§7.1) |
| CEO native-language advantage | **Vietnamese** | Low–Medium | Largely neutralised by the AI pipeline (§7.2) |
| AI narration / script quality | **English** | Medium | [INFERENCE] — test before relying on it |
| Fact-checking & sourcing depth | **English, strongly** | High | RSF 174/180 (§7.3) |
| Operator legal risk | **English** | **High** | Penal code Arts. 109/117/331 (§7.3) |
| Unresolved Vietnamese-law gaps | **English** | High | Choosing Vietnamese puts them on the critical path (§7.3) |
| Affiliate per-conversion value | **Neutral** | Medium | ~$1.26 vs ~$1.19 (§7.4) |
| Affiliate conversion pool & basket growth | **English** | Medium | §7.4 |
| Affiliate operational simplicity | **Vietnamese** | Low | No W-8BEN, no Payoneer, VND local (§7.4) |
| Affiliate withholding base | **Vietnamese** | Medium | Possibly 100% US-source on Amazon (§7.4) |
| YPP pre-2027 threshold capture | **Neutral** | High | Applies to whichever launches first (D-008) |

**[INFERENCE] Non-revenue factors do not offset the revenue gap — they reinforce it.** The two factors favouring Vietnamese (saturation, native fluency) are Medium and Low–Medium; the factors favouring English include two rated High that are independent of revenue entirely (sourcing depth, operator legal risk).

---

## 8. Recommendation

### **Keep D-008. Launch channel 1 in English. The withholding finding does not change the decision and is not close to changing it.**

**The reasoning, in order of weight:**

1. **The arithmetic does not flip and cannot flip.** Net of the 30% withholding, English yields **~$2.66 per 1,000 views** against Vietnamese **~$0.43** — **6.2×**. For Vietnamese to catch up, in-country Vietnamese RPM would need to reach **$1.99–$3.53**, against a **theoretical ceiling of $0.61** set by Vietnam's own measured playback CPM of $1.11. **The required figure exceeds what advertisers pay for the inventory.** (§6.2)
2. **The withholding is smaller than it feels.** It costs ~**16.9% of gross revenue**, not 30% — and it is levied on a revenue base **6× larger**. A 17% haircut on 6× is still **5.1×**. (§5.2)
3. **Withholding does not argue for avoiding US audience — it argues the opposite.** Net RPM *rises* with US share (slope +3.60). Even after 30%, a US view is worth ~7× a rest-of-world view. **The intuition that prompted this question is backwards.** (§6.1)
4. **The conclusion survives removing the US entirely.** At **0% US views and zero withholding**, English still returns $1.58 — **3.7× Vietnamese**. There is no geography assumption that rescues Option B. (§6.1)
5. **Non-revenue factors reinforce rather than offset.** Sourcing depth and operator legal risk both favour English at High weight, independent of money. Choosing Vietnamese would promote two unresolved Vietnamese legal gaps onto the pre-launch critical path. (§7.3)
6. **No entity restructuring.** A treaty entity breaks even only above **200,000–617,000 views/month**, against ~29,100 to cover the envelope. It is net negative by **$92–$317/month** at launch volumes. (§4.3)

### Act on these now — both are free and both are language-independent

1. **Submit the W-8BEN in AdSense before the first payment threshold.** Correct submission costs 16.9% of gross in the central case; non-submission as an individual costs **24% of *worldwide* gross** — **1.4× worse centrally, ~2.5× worse at low US share.** This is the single highest-value action in the file and it takes minutes. (§3.4)
2. **Confirm the resulting rate in the AdSense payee profile itself, not by inference.** A profile-level statement is better evidence than either the Google help page or the IRS list. This was already the documented recommendation in `reverification-2026-09-26.md` §4.1 and is restated here because this analysis rests on it.

### Consider (does not change D-008)

**[INFERENCE]** Per §7.1, the differentiating play is **English-language content that uses the Vietnam vantage point** — Southeast Asian technology, manufacturing, supply chains, economics, or comparative how-to. That captures Tier-1 RPM while converting the CEO's genuine positional advantage into a content moat rather than a language choice. It costs nothing to adopt and is compatible with everything in D-006 and D-008.

### Conditions that would change this recommendation

| Condition | Threshold | Why it would matter |
|---|---|---|
| Vietnamese playback CPM rises sharply | Vietnam CPM > **~$4.00** (from $1.11) | Would lift the Vietnamese RPM ceiling (§6.2) past the flip point. Requires a ~4× change in Vietnamese advertiser demand. Re-check Dynamoi annually. |
| Realised English RPM comes in far below model | Actual blended net RPM < **$1.00** after 3 months of real data | Would mean `A1`–`A3` are badly wrong. Even then, check against realised Vietnamese data — not against this model. |
| Channel sustainably exceeds ~200,000 views/month | 200k+/month sustained | Makes the **treaty-entity** question live (§4.3). Still a question for counsel, not a language question. |
| Vietnamese-language channel pursued for non-revenue reasons | CEO decision | Legitimate — but it should be recorded as a **strategic/audience** decision, not a financial one. This analysis says it cannot be justified on revenue. |
| Affiliate becomes the dominant revenue line before YPP | Affiliate > 70% of revenue | Narrows the gap to roughly parity per conversion (§7.4). Would warrant re-running §5 with affiliate weighted, **not** a language change on its own. |

**D-008 requires no amendment.** This file should be appended to the decision record as the evidence that the question was asked properly and answered with arithmetic.

---

## 9. What this does not settle

**Read this section before acting on anything above.**

1. **The Vietnamese domestic tax position is NOT established, and this file does not improve it.** Whether Circular 40/2021's 5% VAT + 2% PIT and the VND 100 million threshold survive Vietnam's 2026 Tax Administration, PIT and VAT laws **remains unverified to first-party standard** (`reverification-2026-09-26.md` §4.2; D-007). The only available material is professional-firm secondary summary. **[INFERENCE] This matters to this analysis specifically: Vietnamese domestic tax applies to the *net-of-US-withholding* amount under either language option, so it scales both sides and is unlikely to change the ranking — but it is unquantified, and "unlikely to change the ranking" is reasoning, not evidence.** **Qualified Vietnamese tax counsel is required before revenue arrives.** This is a blocker for banking, not for building. It was a blocker on 2026-09-18, it is a blocker today, and nothing here has moved it.

2. **No RPM figure in this file is first-party, and none can be.** YouTube publishes no RPM table. Every RPM is [THIRD-PARTY] or [INDUSTRY PRACTICE], the sources disagree by up to 2.8× on the same market (§1.4), and only one of them (Dynamoi) states its method. **The 6× gap is robust because it is an order-of-magnitude gap resting on two independently-measured CPMs, not because the inputs are precise.** Do not quote $2.66 or $0.43 as though they were measurements.

3. **Audience geography for a new English channel has no published benchmark.** `A5` (30/20/50) is [INFERENCE/ESTIMATE] built from the 21% global US traffic share and the 9.6% user share. Multiple third-party guides were checked and none publishes a cross-channel distribution. **This is a real gap.** It happens not to matter (§6.1 shows the ranking is insensitive across 0–60%), but a different question resting on the same parameter would be on thin ice.

4. **The AI-generation quality gap between English and Vietnamese is [INFERENCE], not measured.** §7.2 asserts that Vietnamese synthetic narration and script generation lag English in the routed stack. **No first-party benchmark for the specific routed models was located.** If a Vietnamese channel were ever seriously considered, this must be tested, not assumed.

5. **Whether Amazon Associates commissions are US-source income for a non-US person is genuinely contested** (§7.4). Amazon says only *"up to 30%"*. This determines whether the affiliate line carries a 30% haircut on 100% of commissions or none at all. **Counsel question.** It is material to year-one revenue, because affiliate earns before YPP thresholds (D-005).

6. **Whether a treaty entity's claim would survive is a counsel question, not settled here.** §4 establishes the *cost* and the *break-even* and finds against it on economics alone. It does **not** establish that a UK/Irish entity managed from Vietnam would obtain treaty residence at all — §4.2 flags substance as the likely failure point. **Nothing in §4 is tax advice.**

7. **Vietnamese advertising-law disclosure obligations and any Vietnamese AI-media disclosure rule remain unestablished** (`reverification-2026-09-26.md` §4.2). Not attempted here; it needs local counsel, not a fetch. It stays off the critical path only because English is recommended.

8. **Whether affiliate links alone trigger YouTube's paid-promotion declaration remains unestablished.** Carried from `reverification-2026-09-26.md` §4.2. The mitigation (always declare) is free and applies to both options.

9. **No view-volume forecast is made or implied anywhere in this file.** §5.4 gives the volume each option *needs* to cover $77.41. It says nothing about whether either will achieve it. **Break-even volume is not a forecast, and this file contains no basis for one.**

---

## 10. Sources

**First-party [OFFICIAL]**
- YouTube — U.S. tax requirements for YouTube earnings: <https://support.google.com/youtube/answer/10391362> (read 2026-09-26)
- YouTube — Submitting your U.S. tax info to Google: <https://support.google.com/youtube/answer/10390801> (read 2026-09-26)
- YouTube — Altered or synthetic content / AI personas: <https://support.google.com/youtube/answer/14328491> (re-verified 2026-09-26)
- YouTube blog — Made on YouTube creator monetization & shopping, 2026-09-23: <https://blog.youtube/news-and-events/made-on-youtube-creator-monetization-shopping/>
- IRS — United States income tax treaties A to Z: <https://www.irs.gov/businesses/international-businesses/united-states-income-tax-treaties-a-to-z> (read 2026-09-26; **Vietnam absent; Singapore and Hong Kong absent**)
- Amazon Associates — Operating Agreement: <https://affiliate-program.amazon.com/help/operating/agreement>
- Amazon Associates — Non-US person tax information: <https://affiliate-program.amazon.com/help/node/topic/GPFZ6W6CF4E5BD9V>

**[THIRD-PARTY]**
- Dynamoi — YouTube AdSense RPM, 84 markets, data through 2026-09-22: <https://dynamoi.com/data/youtube-adsense-rpm> *(only source with a stated collection method)*
- MilX — YouTube CPM/RPM rates 2026 by niche & country, 2026-03-16: <https://milx.app/en/trends/youtube-cpm-rpm-rates-2026-average-niches-countries-more>
- vidIQ — Highest-paying YouTube niches 2026: <https://vidiq.com/blog/post/most-profitable-youtube-niches/>
- YTface — CPM rates by country, Feb 2026 (*modelled, not measured; recorded as an outlier and not used*): <https://www.ytface.com/cpm-rates-by-country>
- TubeAnalytics — State of YouTube monetization 2026 (median RPM ~$2.30 / 53% monetized playback across 300 channels): <https://www.tubeanalytics.net/blog/state-of-youtube-monetization-2026>
- Global Media Insight — YouTube statistics 2026, users by country, 2026-06-26: <https://www.globalmediainsight.com/blog/youtube-users-statistics/>
- Sprout Social — YouTube statistics 2026 (US = 21.07% of traffic, April 2026): <https://sproutsocial.com/insights/youtube-stats/>
- VnEconomy — Google figures from the first YouTube Festival in Vietnam, 2026-09-18: <https://en.vneconomy.vn/vietnam-has-over-180000-youtube-channels-with-more-than-10000-subscribers.htm>
- Reporters Without Borders — Vietnam country profile / 2026 World Press Freedom Index (174/180, score 21.15): <https://rsf.org/en/country/vietnam>
- The Vietnamese Magazine — Vietnam ranks last in Southeast Asia, 2026 WPFI, 2026-04: <https://thevietnamese.org/2026/04/viet-nam-ranks-last-in-southeast-asia-2026-world-press-freedom-index/>
- Freedom House — Vietnam, Freedom in the World 2026: <https://freedomhouse.org/country/vietnam/freedom-world/2026>
- Sleek UK — Limited company accountant cost 2026: <https://sleek.com/uk/resources/limited-company-accountant-cost/>
- Ltd-Companies.co.uk — Accountant for limited company cost UK 2026: <https://ltd-companies.co.uk/resources/accountant-for-limited-company-cost-uk/>
- Shopee Help Centre — Affiliate Program commission rates: <https://help.shopee.com.my/10/article/124012-%5BENG%5D-Shopee-Affiliate-Program-Commission-Rates>
- Digital in Asia — Shopee/Lazada/TikTok Shop fees across SEA 2026: <https://digitalinasia.com/shopee-lazada-tiktok-shop-fees-2026/>

**Internal (read, not modified)**
- `research/reverification-2026-09-26.md`
- `research/ceo-decision-record.md`

---

*Analysis dated 2026-09-26. Every revenue figure is an ESTIMATE. The withholding mechanics in §3 are first-party and quoted. The RPM inputs are third-party, disagree with one another, and are labelled accordingly. The conclusion rests on an order-of-magnitude gap (~6×, never below ~2.6× under stress), not on the precision of any single input — which is why it holds despite the weakness of the inputs. This file does not constitute tax advice; §4 and §9 identify what requires qualified counsel.*
