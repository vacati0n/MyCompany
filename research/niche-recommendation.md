# Niche Recommendation — Channel 1

**Decision-support analysis for the CEO. Commissioned 2026-09-26; written 2026-09-26.**
**Blocks Wave 2. Does not block Wave 1 (D-009/D-010 — the foundation is niche-agnostic).**

| Field | Value |
|---|---|
| Question | Which YouTube niche for one English-language, AI-operated channel at 3 videos/week, Tier-1 audience |
| Constraints treated as settled | D-006 (visual sourcing), D-007 (Vietnam payee, YouTube only), D-008 (one English channel), the $77.41 envelope, the originality regime, the AI-persona rule |
| Method reused, not reinvented | `language-market-analysis.md` §5.1 blended-RPM / net-of-withholding / break-even model |
| Re-weighting applied | Coordinator instruction 2026-09-26: view volume and **time-to-threshold** now outrank RPM per view; operator moat down-weighted; mass-appeal entertainment and broad-appeal edutainment added as candidates |
| Verdict | **Primary: engineering and infrastructure explainers. Runner-up: broad-appeal science / natural-world edutainment.** |

**Labels.** **[OFFICIAL POLICY]** / **[OFFICIAL]** = a first-party page, URL given, read on the stated date. **[THIRD-PARTY]** = a named external source. **[INDUSTRY PRACTICE]** = widely-stated convention with no single authority. **[INFERENCE/ESTIMATE]** = reasoning or arithmetic by this analysis.

> **Every revenue and volume figure in this file is an ESTIMATE.** No RPM here is first-party — YouTube publishes no RPM table. No view forecast here is a forecast; the volume numbers say what each option *needs*, or what comparable channels *have achieved*, not what this channel *will* do.

---

## 0. The three findings that matter most

Read these first. Two of them change the framing the CEO was given.

**Finding 1 — the finance boundary is narrower than feared, and it turns on *persona*, not *topic*.** The rule's own second sentence is: *"This includes any content that presents itself as a human expert providing advice to viewers on topics such as health, legal issues, finances, or politics."* **[OFFICIAL POLICY]** Two predicates must both hold: an **AI-generated persona**, and that persona **presenting as a human expert giving advice**. Industrial economics, supply-chain and macro analysis delivered as unattributed narration over graphics, with no synthetic host and no advice to the viewer, does not meet either predicate. **But the outer scope sentence is broader** — *"channels that use AI-generated personas to deliver information on sensitive topics"* — and "This includes" is an inclusion, not a definition. §1 sets out the boundary and the controls. **Do not treat this as a clean win; treat it as a survivable exposure with named controls.**

**Finding 2 — the 2027-02-01 threshold capture is, on the arithmetic, already lost. Plan for 8,000 watch hours, not 4,000.** **[OFFICIAL POLICY]** *"If you are already in YPP, your status is not impacted by this update."* (<https://support.google.com/youtube/answer/12843009>, read 2026-09-26.) Grandfathering is by **membership**, not by application or by having started. The channel is not launched; the niche is being chosen today. **[THIRD-PARTY]** a new channel typically needs 6–18 months to 1,000 subscribers (3–6 months best case for a tightly-niched, SEO-strong channel). **[INFERENCE/ESTIMATE]** Reaching 1,000 subs *and* 4,000 qualified watch hours *and* passing YPP review inside ~4 months from a cold start is an extreme-tail outcome. D-008 recorded the pre-2027 threshold as "the one deadline that rewards starting now" — that reward should now be treated as **upside, not plan**. §3 models it.

**Finding 3 — the CEO's volume argument is right in direction and wrong in currency. The threshold is denominated in *watch hours*, not views.** **[INFERENCE/ESTIMATE]** A 14-minute explainer at 45% average view duration yields **0.105 watch hours per view**. A 5-minute entertainment piece at 50% yields **0.042**. The entertainment format therefore needs **2.5× the views** to make the same threshold progress — and Shorts views do not count toward long-form watch hours at all (the Shorts route is a separate 20M-views-in-90-days gate, which is out of reach). **Long-form explainer content is the most watch-hour-efficient format available under D-006.** This is the strongest quantified answer to "entertainment gets more views", and it does not depend on any RPM estimate. §3.2.

---

## 1. The finance / politics boundary — where it actually sits

This was flagged as the priority question because it decides admissibility for several candidates. It was resolved against first-party text, not optimistically.

### 1.1 The rule, verbatim

**[OFFICIAL POLICY]** From YouTube channel monetization policies, sub-category **"AI personas related to sensitive topics"** of the **inauthentic content** policy. Read 2026-09-26 at <https://support.google.com/youtube/answer/1311392>; the same wording appears at <https://support.google.com/youtube/answer/14328491> and was re-verified verbatim in `reverification-2026-09-26.md` item 9.

Scope sentence:

> "This policy refers to channels that use AI-generated personas to deliver information on sensitive topics."

Inclusion sentence:

> "This includes any content that presents itself as a human expert providing advice to viewers on topics such as health, legal issues, finances, or politics."

The three examples given of what is not allowed:

> - "An AI 'doctor' providing medical diagnoses, health advice, or wellness remedies."
> - "AI-generated podcast hosts offering financial guidance, investment tips, or wealth management advice."
> - "AI personas giving legal advice or interpreting laws."

Penalty: **"channels uploading this content will not be allowed to monetize."** Channel-level, not video-level.

**[THIRD-PARTY]** Trade coverage of the July 2026 clarification corroborates the text and adds one operational detail no first-party page states: *"Any YouTube channel that has too much of any of these three types of content will not be able to monetize."* — Tubefilter, 2026-07-13, <https://www.tubefilter.com/2026/07/13/youtube-inauthentic-content-monetization-policy-update/>; TechCrunch, 2026-07-20, <https://techcrunch.com/2026/07/20/youtube-clarifies-policies-around-ai-slop-and-upsetting-videos/>. **The "too much" threshold is not published and is not quantified anywhere first-party.**

### 1.2 What this establishes, and what it does not

**[INFERENCE — reasoning from the quoted text]**

| Question | Answer | Confidence |
|---|---|---|
| Does the rule target the topic, or the persona? | **The persona.** Every operative noun is a persona: "AI 'doctor'", "AI-generated podcast hosts", "AI personas". The topics are the *aggravating* condition, not the trigger. | High — the grammar is unambiguous |
| Does the rule reach **personal financial advice**? | **Yes, squarely.** "investment tips", "wealth management advice" are named. | High |
| Does it reach **macroeconomic, industry, supply-chain or business analysis**? | **Not on the inclusion sentence.** Industry analysis is not "advice to viewers", and an unattributed narrator is not "a human expert". | **Medium** — see the residual below |
| Could it reach such content anyway? | **Yes, if a persona exists.** The scope sentence says "deliver information on sensitive topics", which is broader than "advice". A named synthetic host with a claimed background, discussing markets, is inside the outer scope even without advice. | **This is the residual risk and it is real** |
| Does it apply to a channel with **no persona at all**? | **[INFERENCE] No — predicate 1 fails and the rule cannot attach.** A voice-over with no character, no name, no claimed credential, no avatar, is not an "AI-generated persona". | Medium-High. Not stated first-party; reasoned from the text. |

**The honest summary.** The rule targets *synthetic experts dispensing advice*, not *subject matter*. But YouTube did not write a definition with clean edges, and the outer sentence is broader than the inclusion. The safe engineering move is to **fail the first predicate deliberately**, so the topic question never has to be litigated.

### 1.3 Controls that make the boundary safe — recommended as binding

**[INFERENCE/ESTIMATE] — design controls, not policy.** These should be recorded as channel-level rules in the publishing gate, whichever niche is chosen:

1. **No persona.** No named host, no avatar, no claimed credential, no biography, no "I'm a former engineer at…". Narration is unattributed voice-over. The channel brand is a publication, not a person.
2. **No second person imperative on a sensitive topic.** Never "you should", "consider buying", "here's what to do with your portfolio". Third-person, past-and-present-tense description only.
3. **No securities, no tickers, no valuations, no price targets, no "is X a good investment".** These are the named examples and they are not worth the argument.
4. **Attribute every claim to a named external source on screen.** The narrator reports what the IMF, the port authority, the standards body or the filing said. A reporter is not an expert.
5. **Disclose synthetic narration** where the disclosure regime requires it. **[OFFICIAL POLICY]** *"Disclosing AI content won't limit a video's audience or impact its eligibility to earn money."* <https://support.google.com/youtube/answer/14328491>, read 2026-09-26. Disclosure is free; silence is not.

**[INFERENCE] With controls 1–5 in force, candidates 1 (industrial economics) and 2 (engineering/infrastructure) are admissible.** Candidate 6 (geopolitics) remains exposed, but fails on a different constraint first — see §4.6.

### 1.4 The adjacent constraint that does bite: advertiser suitability

**[OFFICIAL POLICY]** <https://support.google.com/youtube/answer/6162278>, read 2026-09-26. Wars, conflicts and political topics are **not** listed as inherently ad-ineligible. "Sensitive events" turns on treatment, not topic: green includes *"news reporting, documentary content or discussions about a sensitive event"*; red is content that *"profits from or exploits a sensitive event."* One standing exception is named: *"Due to the war in Ukraine, content that exploits, dismisses, or condones the war is ineligible for monetization until further notice."*

**[INFERENCE]** So a supply-chain video about the Red Sea or a chokepoint disruption is monetizable if it is documentary in treatment. A video whose hook is the conflict itself is on yellow-ads ground. **This is a treatment rule the editorial gate can enforce; it is not a niche-level bar.**

---

## 2. Evidence base

### 2.1 RPM — the best-sourced dataset found, and where it disagrees with the file we are reusing

**[THIRD-PARTY] AIR Media-Tech, 2026.** <https://air.io/en/air-data-findings/which-youtube-niche-makes-the-most-money-in-2026-ranked-by-real-rpm-and-cpm> and <https://air.io/en/air-data-findings/how-much-does-youtube-really-pay-in-2026-real-rpm-data-from-300-channels>. **Stated method:** *"the real YouTube Analytics of 300 channels AIR Media-Tech works with, every monetized month between May 2025 and May 2026: 3,595 channel-months in total"*, using *"verified RPM, CPM, ad-load, monetized-playback, and view data straight from Studio"*, across 13 niches, channels from 10,000 to 50 million subscribers. **This is the only niche RPM source located that states a sample size and a period.**

| Niche | Median RPM (blended, all geographies) | Note from the source |
|---|---|---|
| **Education & Science** | **$10.22** | Highest of the 13 |
| **Entertainment** | **$2.43** | 40 channels; spread $0.88–$5.02 |
| **Gadgets & Tech** | **$2.33** | CPM $5.73, *"but only a $2.33 median RPM, because just a third of its views ever reach an advertiser"* |
| All-niche median | **$2.30** | — |
| **Kids** | **≈$0.34** | Stated as *"roughly thirty times"* less than Education |

**[INFERENCE] Two cautions before anyone quotes these.**

1. **These are blended whole-channel RPMs for established channels (10k–50M subs), not Tier-1 segment RPMs and not new-channel RPMs.** They are *higher* than what a new channel with a 50% rest-of-world audience will see. They are the right source for **ranking niches against each other**; they are the wrong source for **sizing this channel's revenue**.
2. **The $2.30 all-niche median is almost certainly the same underlying dataset that `language-market-analysis.md` §1.4 attributed to "TubeAnalytics" (median $2.30, 300 channels).** Treat it as **one** observation, not two independent corroborations. The prior file's sanity check is therefore slightly weaker than it reads — though its conclusion is unaffected, because the check was that the model lands *above* the median and it does.

**Sources that conflict, recorded and not used as anchors.** **[THIRD-PARTY]** MilX (2026-03-16) ranks *Tech & Productivity* ($4–12 RPM) **above** Education & How-to ($3–8); AIR's measured data puts *Gadgets & Tech* ($2.33) **far below** Education & Science ($10.22). <https://milx.app/en/trends/youtube-cpm-rpm-rates-2026-average-niches-countries-more>. Content-marketing pages claiming "$18–25 RPM for tech" (e.g. <https://virlo.ai/blog/highest-rpm-niches-on-youtube>, <https://fluxnote.io/guides/youtube-rpm-by-niche-usa-2026>) publish no method and are not used. **AIR is preferred over MilX because AIR gives a mechanism for the gap — monetized-playback rate — and MilX gives none.**

### 2.2 Stock footage availability under D-006 — a hard gate, tested

**[OFFICIAL]** Counts read directly from Adobe Stock video search result pages, 2026-09-26. **Caveat:** the committed library is **Storyblocks Unlimited All Access ($30/mo, re-verified `reverification-2026-09-26.md` §3)**, which **[THIRD-PARTY]** holds *"over 7 million"* clips — a smaller library than Adobe's. **Storyblocks' own search pages could not be read (403 to both fetch and browser render), so these counts are a proxy for the *shape* of supply, not the committed library's exact depth.** The ordering is what matters, and the ordering is decisive.

| Query | Adobe Stock video results | Reads on |
|---|---|---|
| `wildlife` | **2,912,274** | 2026-09-26 |
| `nature landscape timelapse` | **275,228** | 2026-09-26 |
| `container port` | **72,497** | 2026-09-26 |
| `bridge construction engineering` | **62,891** | 2026-09-26 |
| `data center server room` | **56,474** | 2026-09-26 |
| `programmer coding screen` | **53,249** | 2026-09-26 |
| `ancient vietnam history` | **3,693** | 2026-09-26 |
| `semiconductor fab` | **1,443** | 2026-09-26 |
| `vietnam factory worker` | **898** | 2026-09-26 |

**[INFERENCE] This table is the single most useful piece of evidence in the file, and it damages the strategic asset.**

- **Infrastructure, ports, energy, data centres and civil engineering are abundantly supplied.** 50k–70k clips per major subject is enough for 13 videos a month indefinitely, with room to avoid visual repetition — which matters directly to the originality regime.
- **Nature and wildlife are supplied at a different order of magnitude entirely** — 2.9M clips. Visual supply is not a constraint there at all.
- **The Vietnam/SEA vantage, taken literally, is the *thinnest* visual supply of anything tested.** 898 clips for `vietnam factory worker` and 1,443 for `semiconductor fab`. **A channel whose every video must be illustrated with Southeast Asian industrial footage will exhaust its visual supply and start looking templated — which is the exact failure mode the originality regime punishes at channel level.** The moat is real editorially and thin visually.
- **History fails the gate outright.** 3,693 clips for `ancient vietnam history`, and stock footage of historical events does not and cannot exist. The genre runs on archive, which is third-party footage, which D-006 bars.

### 2.3 Originality and the enforcement record

**[OFFICIAL POLICY]** The allowed example that the recommended format is built to match, verbatim: *"Similar content, like a series following a set of characters across episodes or a channel that does product reviews, but in which each video has a distinct storyline, focus, or concept."* And the prohibited one: *"AI-generated content made with generic or unoriginal templates giving the impression of mass production without adding the creator's original, authentic insights or perspective."* <https://support.google.com/youtube/answer/1311392>, read 2026-09-26.

**[THIRD-PARTY] The enforcement is not theoretical.** In January 2026 YouTube terminated **16 channels with a combined ~35 million subscribers and ~4.7 billion lifetime views** under the inauthentic content policy; reporting describes the common pattern as *"AI handled every step of production (scripting, voiceover, visuals, and publishing) with zero human editorial input"*. Named examples include Screen Culture and KH Studio (AI-spliced fake movie trailers) and CuentosFascinantes (5.9M subs, 1.2bn views). Sources: <https://outlierkit.com/resources/youtube-ai-slop-crackdown-2026/>, <https://thenextweb.com/news/youtube-ai-slop-crackdown-faceless-creators-collateral-damage>, <https://www.hollywoodreporter.com/business/digital/faceless-creators-youtube-ai-damage-1236617586/>.

**[THIRD-PARTY]** On where the slop is densest: *"generic motivation quotes, copied Reddit stories, broad scary stories, celebrity facts, and generic AI tool listicles are high-risk because many channels use the same hooks, visuals, and scripts"*; and the counter-observation that *"the channels still growing well in 2026 are the ones using AI to save time on production while keeping something a bot can't fake, an original opinion, a specific data point, a researched angle."* <https://shortsfast.com/blog/saturated-faceless-youtube-niches-2026/>, <https://outlierkit.com/blog/youtube-ai-crackdown>.

**[INFERENCE] Two consequences.** First, the slop wave has *raised* the bar in exactly the low-research genres (compilation, listicle, generic history, motivation) and *lowered* competitive pressure in the high-research genres, because slop cannot produce a correct load-path explanation or a sourced throughput figure. Second, **an automated pipeline produces correlated failures** — if the template is wrong it is wrong 13 times a month, and the penalty is channel-level. Format variety is not a nicety here; it is the principal survival control.

### 2.4 Made-for-kids exposure

**[OFFICIAL POLICY]** The determination factors, verbatim: *"Subject matter of the video (e.g. educational content for preschoolers). Whether children are your intended audience… Whether the video includes characters, celebrities, or toys that appeal to children, including animated characters or cartoon figures. Whether the language of the video is intended for children to understand. Whether the video includes activities that appeal to children… Whether the video includes songs, stories, or poems for children. Any other information you may have to help determine your video's audience, like empirical evidence of the video's audience."* <https://support.google.com/youtube/answer/9528076>, read 2026-09-26.

**[OFFICIAL POLICY]** Personalised advertising is removed on content set as made for kids. <https://support.google.com/youtube/answer/9713557>.

**[THIRD-PARTY]** The revenue consequence: *"$1–3 RPM versus $5–15 for general-audience content"*, with creators reporting drops *"by more than 95%"* in some cases and more conservative sources putting it at 50–80%. AIR's measured Kids median of ≈$0.34 is the harshest reading and the best-sourced one. <https://www.techtimes.com/articles/320340/20260713/ai-kids-cartoon-gold-rush-has-hidden-tax-coppa-cuts-revenue-80.htm>, <https://gyre.pro/blog/how-to-monetize-a-youtube-kids-channel>.

**[INFERENCE] The exposure is concentrated in exactly the "curiosity edutainment" space the coordinator asked to be added.** The first "how things work" channel surfaced in search describes itself as *"made for curious kids ages 7 to 11… trains, vehicles, big machines, and simple science using calm visuals, gentle storytelling"*. **Cartoon-style animation, simple-language narration, machines-and-animals subject matter and gentle pacing are four of YouTube's own named factors simultaneously.** The drift is not hypothetical; it is the genre's centre of gravity. Anything in this space must be deliberately engineered for adults — numbers on screen, technical vocabulary, adult framing, no cartoon characters — and even then "empirical evidence of the video's audience" means YouTube can reclassify on observed behaviour.

**Animal / wildlife variant:** commissioned separately as `research/animal-niche-analysis.md` per D-012. **As at the time of writing it had not landed in `research/`.** Its conclusions on format, stock depth and made-for-kids exposure supersede anything said here about that variant, and are not duplicated.

---

## 3. The re-weighted economics: before the threshold, and after

The coordinator is correct that RPM is worth nothing before YPP entry, and that time-to-threshold is therefore a financial variable. This section models it.

### 3.1 The threshold, and who is grandfathered

**[OFFICIAL POLICY]** <https://support.google.com/youtube/answer/12843009> and <https://support.google.com/youtube/answer/72851>, read 2026-09-26:

- Now: **1,000 subscribers AND (4,000 qualified public watch hours in 12 months OR 10M qualified Shorts views in 90 days)**, plus eligible country, AdSense, 2-step verification, advanced features, no active Community Guidelines strikes.
- *"Starting Feb. 1, 2027, YPP entry thresholds for new creators are changing to 8,000 qualified watch hours in the last 365 days, or 20M qualified Shorts views in the last 90 days"* — the 1,000-subscriber requirement is unchanged.
- *"If you are already in YPP, your status is not impacted by this update."*
- *"To continue fully monetizing your content, review and accept the updated terms in YouTube Studio by January 31, 2027."*

**[INFERENCE] The grandfathering test is membership, not application and not effort.** The page does not say what happens to an applicant who is mid-review on 1 February 2027, and **this analysis could not establish it** — see §9.

### 3.2 Watch-hour efficiency by format — the currency correction

**[INFERENCE/ESTIMATE]** Assumptions: average view duration 40–50% of runtime is the consistently-stated healthy band (**[THIRD-PARTY]** vidIQ, Team5PM via <https://humbleandbrag.com/blog/youtube-audience-retention-benchmarks>, <https://vidiq.com/blog/post/average-view-duration/>); 45% is used as the central case. Runtimes are typical for each format.

```
watch_hours_per_view = runtime_minutes × AVD% ÷ 60
views_for_threshold  = threshold_hours ÷ watch_hours_per_view
```

| Format | Runtime | AVD | **Hours / view** | Views for 4,000 h | Views for **8,000 h** |
|---|---|---|---|---|---|
| Long-form engineering / infrastructure explainer | 14 min | 45% | **0.1050** | 38,100 | **76,200** |
| Science / edutainment explainer | 10 min | 45% | **0.0750** | 53,300 | **106,700** |
| Consumer-tech explainer | 9 min | 40% | **0.0600** | 66,700 | **133,300** |
| Industrial-economics deep dive | 16 min | 40% | **0.1067** | 37,500 | **75,000** |
| Mass-appeal entertainment | 5 min | 50% | **0.0417** | 96,000 | **192,000** |
| Shorts-led | — | — | — | 10M Shorts views / 90d | **20M Shorts views / 90d** |

**[INFERENCE] Read across the last column. The entertainment format needs 2.5× the views of the engineering format to reach the same gate, and the Shorts route needs a volume that no channel in this business's cost class reaches.** The CEO's volume argument survives only if entertainment can deliver more than 2.5× the views of a long-form explainer *and* survive §2.3. §4 finds it cannot do the second.

### 3.3 Time to threshold — three trajectories

**[THIRD-PARTY] Benchmarks used.** New channels under 1,000 subs average **50–200 views per video**; 1k–10k subs average **200–2,000**; educational/eLearning channels see **150–500 views in the first 48 hours**; a video at ~450 first-week views reaches ~2,000 in its first year through search and suggested; YouTube's profiling takes *"90 days and 12 to 15 videos"*. <https://humbleandbrag.com/blog/new-youtube-channel-average-views>. Time to 1,000 subscribers: **6–18 months typical, 3–6 months for niche-focused channels with strong SEO**; time to 4,000 watch hours: **6–12 months typical**. <https://touhfa.art/blog/growth/how-long-to-get-1000-youtube-subscribers/>, <https://vidiq.com/blog/post/how-to-generate-4000-hours-watch-time-youtube/>.

**[INFERENCE/ESTIMATE] Monthly channel-view trajectories** (all views in that month, new uploads plus back catalogue; 13 videos/month throughout; a long-form explainer at 0.105 h/view):

| Month | Weak | Central | Strong |
|---|---|---|---|
| 1–3 (cum.) | 2,500 | 13,000 | 30,000 |
| 4–6 (cum.) | 10,000 | 62,000 | 150,000 |
| 7–9 (cum.) | 28,000 | 175,000 | 420,000 |
| 10–12 (cum.) | 61,000 | 432,000 | 950,000 |
| **Cum. watch hours at M12** | **6,400** | **45,400** | **99,800** |
| **Month 8,000 h is crossed** | **~M13–14** | **~M7** | **~M5** |
| **Month 1,000 subs is plausibly crossed** | **M15–18+** | **M8–12** | **M5–7** |
| **Binding constraint** | watch hours *and* subs | **subscribers** | subscribers |

**[INFERENCE] Two conclusions, both uncomfortable and both load-bearing.**

1. **In the central and strong cases, the binding constraint is not watch hours — it is 1,000 subscribers.** Watch hours accumulate faster than subscribers for a long-form explainer channel, because search and suggested traffic watches without subscribing. **This inverts part of the CEO's argument: for the recommended format, raw view volume is not the bottleneck; audience *loyalty* is.** That favours a channel with a recognisable recurring format and a reason to return — which is an argument for pillars (§6), not for entertainment.
2. **Nothing here reaches YPP membership by 2027-02-01.** Even the strong case crosses 1,000 subs around month 5–7 from launch, and the channel is not launched. **Budget for the 8,000-hour world.** The one action that remains free and worth doing is accepting the updated terms in Studio before 31 January 2027 if the channel exists by then, and submitting the W-8BEN (`language-market-analysis.md` §3.4 — worth 7–15 points of gross revenue and still the highest-value free action available).

**Cost of the pre-revenue period. [INFERENCE/ESTIMATE]** At $77.41/month, 7 months to threshold costs **$542**, 12 months costs **$929**, 18 months costs **$1,393**. This is the number the affiliate line has to attack.

### 3.4 Affiliate before the threshold — quantified both ways

**[THIRD-PARTY] Conversion benchmarks.** YouTube affiliate links convert at **2.3%**; the average affiliate link on YouTube converts at **3.2%**, with **review-style videos at 4.1%**; YouTubers including product reviews earn **67% more** from affiliate links than general-content creators; affiliate CTR on YouTube runs **2–10%, most channels 4–5%**; top performers reach 5–10% conversion by audience relevance. <https://wecantrack.com/insights/youtube-affiliate-marketing-statistics/>, <https://wecantrack.com/insights/affiliate-conversion-statistics/>, <https://wecantrack.com/insights/affiliate-click-through-rate-statistics/>. **[INFERENCE] These benchmarks are drawn predominantly from commerce-oriented affiliate sites and are likely biased upward for a general-interest video channel. They are used as an upper-leaning band, not a forecast.**

**Net per conversion: $1.26** — carried unchanged from `language-market-analysis.md` §7.4 (Amazon US, $60 basket × 3%, less 30% withholding if US-source). **[OFFICIAL — carried gap]** whether those commissions are US-source for a non-US person is contested and is a counsel question.

**[INFERENCE/ESTIMATE] Affiliate revenue per 1,000 views, by audience intent:**

```
affiliate_RPM = 1,000 × link_CTR × conversion_rate × $1.26
```

| Audience type | Example | Link CTR | Conv. | **Affiliate $/1,000 views** |
|---|---|---|---|---|
| **High intent** — buying decision in play | Consumer-tech explainer, tool review | 4.0% | 4.1% | **$2.07** |
| **Medium intent** — adjacent purchases exist | Engineering/infrastructure (books, models, CAD tools, courses, measurement gear) | 2.0% | 3.0% | **$0.76** |
| **Low-medium** — indirect | Industrial economics (books, data services) | 1.5% | 2.5% | **$0.47** |
| **Low intent** — nothing to sell | Science / nature / entertainment | 0.5% | 2.0% | **$0.13** |

**[INFERENCE] The counter-argument the coordinator asked to be tested holds, and it is significant.** For a medium-intent explainer, affiliate is worth ~$0.76 per 1,000 views **from day one**, against ad revenue of $0.00 until month 7–14. Over a 12-month pre-threshold period on the central trajectory (432,000 cumulative views), that is **~$328** — about **35% of the $929 pre-revenue cost**. For a high-intent niche it would be ~$894, i.e. **essentially self-funding**. For entertainment or nature it is **~$56 over the same year — negligible**.

**This is the strongest single argument against pure entertainment that does not depend on policy:** entertainment has no pre-threshold revenue at all, so it must be carried at full cost for the whole run-up, while needing 2.5× the views to end it.

**It is also the strongest argument *for* consumer tech**, and the honest reason consumer tech scores as well as it does in §4 despite a measured RPM of $2.33.

### 3.5 Post-threshold ad economics, by candidate

**Method: `language-market-analysis.md` §5.1, unchanged.** Geography split `A5` = 30% US / 20% other Tier-1 / 50% rest of world. `A3` (rest-of-world RPM) = $0.60 throughout. Withholding at 30% on the **US segment only** (Vietnam has no US treaty — **[OFFICIAL]** IRS treaty list, <https://www.irs.gov/businesses/international-businesses/united-states-income-tax-treaties-a-to-z>). `A1`/`A2` re-set per niche.

```
gross_RPM      = 0.30×A1 + 0.20×A2 + 0.50×0.60
withheld       = 0.30 × (0.30 × A1)
net_RPM        = gross_RPM − withheld
views_to_cover = 77.41 ÷ net_RPM × 1,000
```

| Candidate | `A1` US | `A2` Tier-1 | Gross RPM | Withheld | **Net ad RPM** | **Break-even (ad only)** | + affiliate | **Break-even (combined)** |
|---|---|---|---|---|---|---|---|---|
| Engineering / infrastructure — **low** | $5.00 | $4.50 | $2.70 | $0.45 | **$2.25** | 34,400/mo | $0.76 | 25,720/mo |
| Engineering / infrastructure — **central** | $6.00 | $5.50 | $3.20 | $0.54 | **$2.66** | **29,102/mo** | $0.76 | **22,635/mo** |
| Engineering / infrastructure — **high** | $8.00 | $7.00 | $4.10 | $0.72 | **$3.38** | 22,902/mo | $0.76 | 18,678/mo |
| Science / edutainment (adult-classified) | $6.50 | $6.00 | $3.45 | $0.585 | **$2.87** | 26,972/mo | $0.13 | 25,803/mo |
| Science / edutainment (**if made-for-kids**) | $0.90 | $0.80 | $0.73 | $0.081 | **$0.65** | **119,092/mo** | $0.13 | 99,244/mo |
| Industrial economics (SEA) | $6.00 | $5.50 | $3.20 | $0.54 | **$2.66** | 29,102/mo | $0.47 | 24,732/mo |
| Consumer tech | $4.50 | $4.00 | $2.45 | $0.405 | **$2.05** | 37,761/mo | $2.07 | **18,789/mo** |
| Software / developer | $6.00 | $5.50 | $3.20 | $0.54 | **$2.66** | 29,102/mo | $1.20 | 20,054/mo |
| Mass-appeal entertainment | $2.50 | $2.30 | $1.51 | $0.225 | **$1.285** | **60,242/mo** | $0.13 | 54,681/mo |

**[INFERENCE/ESTIMATE] Every cell above is an estimate.** `A1` values are set from §2.1 with the following reasoning, stated so the CEO can move them: the engineering/infrastructure central case **is** the `language-market-analysis.md` education/tech explainer case, carried over unchanged, which is why its break-even reproduces 29,102 exactly; science/edutainment is set marginally higher because AIR puts Education & Science top of its 13-niche table; consumer tech is set *below* the explainer band because AIR measures $2.33 and gives a mechanism (*"just a third of its views ever reach an advertiser"*), despite MilX ranking it higher; entertainment is set at roughly half the explainer band from AIR's $2.43 median against the $2.30 all-niche median; the made-for-kids row is scaled from AIR's Kids figure of ≈$0.34.

**The two numbers worth staring at: mass-appeal entertainment needs 2.07× the monthly views of the recommended niche just to cover $77.41, and a made-for-kids misclassification needs 4.09×.**

---

## 4. Scored comparison

### 4.1 Scoring method — stated so the CEO can disagree with a weight

- Nine criteria. Weights sum to **100** and reflect the coordinator's re-weighting: volume and time-to-threshold outrank RPM; operator fit is deliberately small.
- Each candidate scores **0–5** per criterion. Weighted total is out of **500**; the percentage is shown for readability.
- **Criteria 4 (policy admissibility) and 5 (visual sourcing under D-006) are gates first and scores second.** A score of **0 or 1** on either is **disqualifying regardless of the total**, because both are channel-level existential constraints, not trade-offs.
- **To change the verdict, change a weight and re-run the row.** The weights are the argument; the totals are arithmetic.

| # | Criterion | Weight | Why this weight |
|---|---|---|---|
| 1 | **View-volume ceiling / mass appeal** | 18 | The CEO's argument, taken seriously. Nothing works without audience. |
| 2 | **Time to YPP threshold** (watch-hour efficiency × plausible volume) | 17 | Pre-threshold, RPM is worth zero. §3. |
| 3 | **Originality defensibility at 13 videos/month** | 16 | Channel-level penalty; correlated failure in an automated pipeline. §2.3 |
| 4 | **Policy admissibility** (persona rule, advertiser suitability, made-for-kids) | 14 | **Gate.** Existential, not gradual. §1, §2.4 |
| 5 | **Visual sourcing under D-006 at budget** | 12 | **Gate.** A niche whose visuals do not exist is not a niche. §2.2 |
| 6 | **RPM band after threshold** | 8 | Down-weighted per the re-weighting; matters only after the gate is passed. |
| 7 | **Affiliate / purchase intent** | 7 | The only pre-threshold revenue that exists (D-005). §3.4 |
| 8 | **Evergreen vs news-cycle** | 5 | A news-dependent format cannot buffer production; the pipeline needs a queue. |
| 9 | **Operator fit** (engineering background, Vietnam vantage) | 3 | Down-weighted. Counts only where it survives a mass-appeal format. |

### 4.2 The table

| Criterion (weight) | **A.** SEA manufacturing & industrial econ | **B.** Engineering & infrastructure | **C.** Software / developer | **D.** Consumer tech | **E.** Science & natural world (edutainment) | **F.** History & geopolitics of Asia | **G.** Mass-appeal entertainment |
|---|---|---|---|---|---|---|---|
| 1 Volume ceiling (18) | 2 | **4** | 2 | 4 | **5** | 3 | **5** |
| 2 Time to threshold (17) | 2 | **4** | 2 | 3 | **4** | 3 | 2 |
| 3 Originality at 13/mo (16) | **5** | 4 | 4 | 2 | 3 | 3 | **1** ⚠ |
| 4 Policy admissibility (14) ⛔gate | 3 | **5** | **5** | 4 | 3 | 2 | 2 |
| 5 Visual sourcing (12) ⛔gate | 2 | **5** | 3 | 2 | **5** | **1** ⚠ | 2 |
| 6 RPM band (8) | 4 | 4 | 4 | 2 | **5** | 3 | 1 |
| 7 Affiliate intent (7) | 2 | 3 | 3 | **5** | 1 | 1 | 1 |
| 8 Evergreen (5) | 3 | **5** | 2 | 1 | **5** | **5** | 2 |
| 9 Operator fit (3) | **5** | 4 | **5** | 3 | 1 | 3 | 1 |
| **Weighted total / 500** | **292** | **424** | **318** | **300** | **383** | **258** | **220** |
| **%** | 58.4% | **84.8%** | 63.6% | 60.0% | **76.6%** | 51.6% | 44.0% |
| **Admissible?** | Yes, with §1.3 controls | **Yes** | Yes | Yes | Yes, with MFK engineering | **NO — fails gate 5** | **NO — fails gate 3** |

### 4.3 Why each score, briefly

**A — SEA manufacturing, supply chains, industrial economics (292).** The best originality score on the board: this content genuinely cannot be templated, and slop cannot fake a sourced throughput figure. It is also the candidate the operator is best placed to make. It loses on three things that are not opinions: **visual supply is the thinnest tested** (898 / 1,443 clips, §2.2); the audience ceiling is demonstrably modest — **[THIRD-PARTY]** Asianometry, the strongest channel in exactly this space, reached ~270k subscribers over years (<https://screenlace.com/how-asianometry-grew-to-270k-subscribers-on-youtube>) — and a ceiling like that is a slow road to 1,000 subscribers, let alone 8,000 hours; and tariff/trade content is partly news-cycle-bound, which a 13-a-month pipeline cannot buffer. **The moat is real. The moat is small. Per the brief's own test, a moat that caps the audience below threshold is not an asset.**

**B — Engineering and infrastructure explainers (424). Recommended.** It is the only candidate that scores ≥4 on *both* volume and every gate. Volume is proven, not hoped: **[THIRD-PARTY]** Real Engineering ~5.02M subscribers, Wendover Productions >4.9M with 282 videos and >822M views as at 2026-06-19 (<https://en.wikipedia.org/wiki/Brian_McManus_(YouTuber)>, <https://en.wikipedia.org/wiki/Sam_Denby>, <https://socialblade.com/youtube/channel/UC9RM-iSvTu1uPJb8X5yp3EQ/realtime>). It has the best watch-hour efficiency of any admissible format (§3.2). Visual supply is 50k–70k clips per subject and data visualisation is *native* to the subject rather than decoration — which is the cheapest route to "original motion graphics and data visualisation" under D-006. No finance/health/legal/politics exposure. Adult-framed by default, so made-for-kids risk is low. Genuinely evergreen. And it absorbs the Vietnam vantage as **one pillar of five** rather than as the whole channel, which keeps the moat without the audience ceiling or the stock-footage wall.

**C — Software and developer explainers (318).** Clean on policy, strong on operator fit, and the operator's ability to catch factual errors matters more here than anywhere. It fails on volume and durability: the developer audience is small relative to the watch-hour requirement, and framework churn makes most of the catalogue perishable — the opposite of what a 13-a-month pipeline needs. **One note the CEO should have:** screen recording of the operator's own IDE is original footage, is not on-location filming, not a presenter on camera and not a third-party clip, so **it is arguably permitted under D-006 and would fix the visual problem**. That is an interpretation, not a decision, and it belongs to whoever owns D-006.

**D — Consumer technology explainers (300).** Best affiliate economics on the board by a wide margin ($2.07 per 1,000 views, §3.4), and on combined break-even it is actually competitive (18,789 views/month). It fails on two structural points. First, **[OFFICIAL — measured]** AIR's $2.33 median with *"just a third of its views ever reach an advertiser"* — the ad line is much weaker than the marketing literature claims. Second and worse, **D-006 removes the format's core act**: no presenter, no on-location filming, no third-party clips means the channel can never show a product being used. A consumer-tech channel built only from stock and graphics is a specification-reader, which is close to *"readings of other materials you did not originally create"* — a named NOT ALLOWED example. It is also the most news-cycle-bound candidate and among the most slop-flooded.

**E — Science and natural-world edutainment (383). Runner-up.** The highest volume ceiling, the highest RPM band, unlimited visual supply, fully evergreen. Three things keep it second. **Made-for-kids drift is structural, not incidental** (§2.4), and the downside is a 4× worse break-even — 119,092 views/month. **Affiliate is near-zero** ($0.13 per 1,000), so the whole pre-threshold period is carried at full cost. **And the competitive set is Kurzgesagt-class**, where the differentiator is animation budget: **[THIRD-PARTY]** Kurzgesagt publishes roughly one video a month with bespoke animation; this business publishes thirteen a month on $77.41. **[INFERENCE]** Competing on production value there is not available; competing on research depth is, but that is the engineering candidate's game with worse visuals-to-argument fit. The coordinator asked whether this genuinely reconciles volume with the constraints. **Partly — and it is a legitimate choice — but it reconciles volume with *policy* while giving up the pre-threshold revenue line and taking on the largest quantified economic risk in the set.**

**F — History and geopolitics of Asia (258). NOT ADMISSIBLE.** It fails gate 5 before the political boundary is even reached: historical footage does not exist in stock libraries (3,693 clips, and those are modern shots of old places), and the genre runs on archive, which is third-party footage barred by D-006. Generic maps-and-ken-burns is the single most recognisable AI-slop signature in 2026. Politics is also named in the persona rule, and while §1.3's controls would probably hold, there is no reason to buy that risk for a genre that cannot be illustrated.

**G — Mass-appeal entertainment (220). NOT ADMISSIBLE — and this is the direct answer to the CEO's steer.** It fails gate 3. Entertainment formats under D-006 reduce to stock-footage-plus-narration compilations, which is templated by construction. **[OFFICIAL POLICY]** the named prohibitions match it almost word for word: *"Image slideshows, templated storylines, or scrolling text with minimal or no narrative"*, *"Videos where characters are put in the same situation over and over again with the same outcome"*, and *"AI-generated content made with generic or unoriginal templates giving the impression of mass production"*. **[THIRD-PARTY]** the January 2026 terminations removed 16 channels with 35M subscribers doing exactly this. On top of that: worst watch-hour efficiency (2.5× the views needed, §3.2), lowest measured RPM ($2.43), highest made-for-kids exposure, and no affiliate line. **[INFERENCE] The volume premium is real but it is the only thing that is, and it is not large enough: entertainment would need to out-view the recommended niche by roughly 2.5× on watch hours and 2.07× on break-even simultaneously, while running the one risk that ends the company rather than costing it money.**

**What of the CEO's argument survives?** The valid core — *reach matters more than rate before the threshold* — survives intact and has been honoured: it is why candidate B outranks candidate A by 132 points despite A having the better moat and the better originality score. **The steer was right about the direction and wrong about the destination. The volume advantage is real, but only in the edutainment form, and the most watch-hour-efficient edutainment form is engineering and infrastructure, not entertainment.**

---

## 5. Recommendation

### Primary: **Engineering and infrastructure explainers** — how built systems actually work, why they were built that way, and what happens when they fail.

Ports, container logistics, semiconductors and fabs, power grids and generation, bridges and tunnels, water and sanitation, data centres and undersea cable, rail and aviation infrastructure, heavy manufacturing processes.

### Runner-up: **Broad-appeal science and natural-world edutainment**, adult-framed.

### What makes the difference — four things, in order

1. **Watch-hour efficiency.** 0.105 hours/view against 0.075 for the edutainment format and 0.042 for entertainment. In a world where the gate is denominated in hours and the 8,000-hour threshold is now the planning assumption, this is worth more than any RPM difference on the table.
2. **Affiliate exists.** ~$0.76 per 1,000 views from day one (tools, reference books, measurement equipment, CAD and simulation software, technical courses), against ~$0.13 for science/nature. Over a 12-month pre-threshold run on the central trajectory that is ~$328 against ~$56 — the difference between recovering a third of the pre-revenue cost and recovering none of it.
3. **Made-for-kids risk is low by construction, not by discipline.** Load paths, tolerances, throughput figures and failure analysis are not child-directed subject matter under any of YouTube's named factors. The edutainment runner-up has to be actively engineered away from the kids band and can still be reclassified on *"empirical evidence of the video's audience"*.
4. **It keeps the strategic asset without paying for it.** The Vietnam/SEA vantage becomes one recurring pillar of five (§6, pillar 3) rather than the channel's whole identity — so the moat is retained, the operator's engineering competence is used every week, and the channel is not hostage to an 898-clip stock library or to a 270k-subscriber genre ceiling.

**What would make the runner-up the right answer instead.** If the made-for-kids question is resolved cleanly in the negative by `animal-niche-analysis.md`, and the CEO weights raw volume ceiling above pre-threshold cash — i.e. raises criterion 1 from 18 to ~30 and cuts criterion 7 to ~2 — candidate E overtakes candidate B. **That is a legitimate disagreement about a weight and the CEO is entitled to it.** It is not a disagreement about the arithmetic.

---

## 6. Content pillars for the recommended niche

Five recurring formats, 13 videos/month. **The design goal is YouTube's own ALLOWED example, matched deliberately:** *"a series following a set of characters across episodes or a channel that does product reviews, but in which each video has a distinct storyline, focus, or concept."* Each pillar is a *format*, not a *template*: the structure recurs, the storyline, focus and concept do not. **[OFFICIAL POLICY]** <https://support.google.com/youtube/answer/1311392>

| # | Pillar | Per month | What recurs | What must differ every time |
|---|---|---|---|---|
| 1 | **System Anatomy** — one built system, end to end | 4 | The walk-through structure; the cutaway diagram | The system, the constraint it solves, the engineering decision at its heart |
| 2 | **Failure Post-mortem** — what broke, why, and what changed afterwards | 3 | Timeline → mechanism → consequence | The failure, the physics, the standard or code that changed |
| 3 | **Made in Asia** — the Vietnam/SEA vantage | 3 | Regional framing; on-the-ground sourcing | The product, the supply chain, the economics — no repetition of region-generic footage |
| 4 | **The Number** — one statistic, unpacked in data visualisation | 2 | Chart-led narrative, original graphics | The dataset, the source, the counter-intuitive finding |
| 5 | **Engineering Decisions** — why A was chosen over B | 1 | The two-option comparison frame | The trade-off, the era, the constraint that decided it |

**Example titles** (illustrative; each has a distinct storyline, focus and concept):

1. *System Anatomy* — "How 20,000 containers come off a ship in 18 hours"; "The machine that makes every chip: inside an EUV lithography scanner"; "What actually happens in the 40 milliseconds before your lights go out"; "The undersea cable repair ship, and why the internet depends on eleven of them"
2. *Failure Post-mortem* — "Tacoma Narrows, recalculated: what aeroelastic flutter actually is"; "The Texas grid in February 2021: a failure of gas, not wind"; "Why the Millennium Bridge wobbled, and the maths nobody had done"
3. *Made in Asia* — "Why your $1,200 phone is assembled 40 km from Hanoi"; "The port that decided Vietnam's export economy"; "What Samsung actually builds in Bac Ninh, and what it does not"
4. *The Number* — "One chart explains why ports stopped working in 2021"; "3.2 gigawatts: the number that decides where data centres get built"; "The 47-day figure that reshaped global shipping insurance"
5. *Engineering Decisions* — "Why the world gave up on the double-decker airliner"; "Concrete or steel: the choice every bridge engineer makes once"

**[INFERENCE] Why this survives the originality regime at 13/month.** Five distinct formats mean no more than four videos a month share a structure. Every video carries an original argument, original data visualisation and a sourced factual claim — which is precisely the *"original, authentic insights or perspective"* the prohibition requires be present. And the correlated-failure risk is spread across five templates rather than concentrated in one. **This is the principal control, and it is a design decision, not a review step.**

---

## 7. Break-even re-run — recommended niche

**Method unchanged from `language-market-analysis.md` §5.1.** Cost envelope `A9` = **$77.41/month** (**[OFFICIAL]**, all twelve unit prices re-verified 2026-09-26, `reverification-2026-09-26.md` §3). Geography `A5` = 30% US / 20% other Tier-1 / 50% rest of world. `A3` = $0.60. Withholding 30% on the US segment only.

**Central case — identical inputs to the established education/tech explainer case, because that is what this niche is:**

| Segment | Share | RPM | Gross per 1,000 views |
|---|---|---|---|
| United States | 30% | $6.00 (`A1`) | $1.80 |
| Other Tier-1 English | 20% | $5.50 (`A2`) | $1.10 |
| Rest of world | 50% | $0.60 (`A3`) | $0.30 |
| | | **Gross RPM** | **$3.20** |

```
US-sourced revenue    = $1.80                      ( 56.3% of gross, from 30% of views )
Withholding @ 30%     = 0.30 × $1.80     = $0.54
NET ad RPM            = $3.20 − $0.54    = $2.66
Effective haircut     = $0.54 ÷ $3.20    = 16.9% of gross

Break-even, ad only   = 77.41 ÷ 2.66 × 1,000  =  29,102 views/month  =  2,239 views/video
```

**→ The established figure reproduces exactly: $2.66 net, 29,100 views/month. [INFERENCE/ESTIMATE]**

**With the affiliate line (D-005), which the prior model omitted:**

```
Affiliate RPM (medium intent, §3.4)       = $0.76 / 1,000 views
Combined net RPM                          = $2.66 + $0.76  = $3.42
Break-even, combined  = 77.41 ÷ 3.42 × 1,000  =  22,635 views/month  =  1,741 views/video
```

**Band across the RPM range:**

| Case | `A1` | Net ad RPM | Break-even ad only | Combined net RPM | **Break-even combined** |
|---|---|---|---|---|---|
| Low | $5.00 | $2.25 | 34,404/mo | $3.01 | 25,720/mo (1,978/video) |
| **Central** | **$6.00** | **$2.66** | **29,102/mo** | **$3.42** | **22,635/mo (1,741/video)** |
| High | $8.00 | $3.38 | 22,902/mo | $4.14 | 18,678/mo (1,437/video) |

**Pre-threshold period — the number the break-even table does not show. [INFERENCE/ESTIMATE]**

```
Ad revenue before YPP entry               = $0.00, always
Affiliate during pre-threshold (central trajectory, 12 months, 432,000 cum. views)
                                          = 432 × $0.76  ≈  $328
Cost over 12 months                       = 12 × $77.41  =  $929
Net pre-threshold position                ≈  −$601
```

At 7 months (central case for 8,000 hours, though **subscribers are the binding constraint** — §3.3): cost $542, affiliate on ~175,000 cumulative views ≈ $133, net ≈ **−$409**.

**[INFERENCE] Read together: the break-even view count is modest and reachable — 1,741 views per video is within the "educational channel, 150–500 views in 48 hours plus a year of search traffic" band. The thing that costs money is not the break-even level; it is the ~7–14 months of full cost before the ad line switches on at all.** That is the number the CEO should hold, and it is roughly **$400–$900** in aggregate, partly recoverable through affiliate.

---

## 8. What would make this fail

Honestly, and in order of how likely each is to actually happen.

1. **The channel never reaches 1,000 subscribers.** §3.3 finds that for this format, subscribers bind before watch hours do. A search-and-suggested-driven explainer channel earns views without earning loyalty. If the pillars do not produce a reason to return, the channel can accumulate 15,000 watch hours and still be outside YPP. **This is the most likely failure mode and it is not a policy failure — it is a product failure.**
2. **The pipeline produces a recognisable house style and it reads as templated.** Thirteen videos a month from one automated pipeline will converge unless something actively prevents it. YouTube's prohibition is written for exactly this and the penalty is channel-level. **The five-pillar design (§6) is the control; if the pillars collapse into one format in practice, the control is gone.**
3. **The engineering is wrong and the audience notices.** This niche's audience contains practising engineers. An AI-assisted script that gets a load path, a thermodynamic bound or a throughput figure wrong will be corrected in the comments, publicly, and the channel's authority does not survive many of those. The operator's domain competence is the mitigation and it is real — but it is competence in software, not in civil, electrical or process engineering. **The competence transfer is partial and should not be overclaimed.**
4. **The Vietnam pillar runs out of footage.** 898 clips is not enough for 36 videos a year with visual variety. Pillar 3 may need to be reduced from 3/month to 1–2/month, or supplemented by original motion graphics carrying more of the visual load than the other pillars.
5. **Made-for-kids reclassification on observed audience.** Lower probability here than for the runner-up, but not zero: *"empirical evidence of the video's audience"* is a factor YouTube applies, and big-machines content attracts children. Mitigation is adult framing and technical vocabulary from video one.
6. **The persona-rule residual materialises.** If YouTube reads the outer scope sentence broadly — "deliver information on sensitive topics" — and treats any synthetic narration on economics as within it, then pillar 3 and pillar 4 are exposed. §1.3's controls are designed so that the *first* predicate fails, which should be decisive. **But this is an inference from policy wording, not a first-party confirmation, and it is the one admissibility risk that is channel-fatal rather than costly.**
7. **Ad rates come in materially below the model.** `language-market-analysis.md` sets the trigger: realised blended net RPM below $1.00 after three months of real data means `A1`–`A3` are wrong. That would push break-even past 77,000 views/month and require a re-run, not a patch.
8. **Stock licence exposure.** **[OFFICIAL]** Storyblocks' post-cancellation use rights **could not be verified** (`ai-capacity-dossier.md`); music licences do not auto-clear Content ID and **do not reimburse revenue lost before channel registration**. Register the channel on the allowlist on day one. This is not a niche risk, but it is a way this niche's economics get quietly worse.

---

## 9. What this does not settle, and who must settle it

| # | Open item | Who settles it | Needed by |
|---|---|---|---|
| 1 | **Whether an applicant mid-YPP-review on 2027-02-01 is judged at 4,000 or 8,000 hours.** No first-party page states it; §3.1. The recommendation does not depend on it because the arithmetic says neither is reachable — but if the CEO wants to chase the 4,000 gate, this must be established first. | Re-verification pass (due 2026-10-26), first-party | Before any launch-date decision |
| 2 | **The persona-rule residual** (§1.2, §8.6). The narrow reading is an inference from the inclusion sentence; the outer scope sentence is broader. No first-party clarification exists and no "too much" threshold is published. | CEO risk acceptance, on the §1.3 controls; re-check monthly | Before first publication |
| 3 | **Made-for-kids exposure for the runner-up, and for animal/nature content generally.** Commissioned as `research/animal-niche-analysis.md` per D-012; **not landed as at the time of writing**. Its findings govern candidate E and supersede §2.4 on that variant. | The animal-niche analysis | Before candidate E could be chosen |
| 4 | **Whether Storyblocks' actual library depth matches the Adobe proxy in §2.2.** Storyblocks 403s to both fetch and browser render. The *ordering* is almost certainly right; the *counts* are not Storyblocks'. | A manual search inside the subscription, on day one | Before committing pillar 3's volume |
| 5 | **Whether screen recording of the operator's own machine is permitted under D-006** (§4.3, candidate C). It is not on-location filming, not a presenter, not a third-party clip — but D-006 does not name it. | Whoever owns D-006 (CEO) | Only if candidate C is revisited |
| 6 | **Whether Amazon Associates commissions are US-source income for a non-US person.** Carried unchanged from `language-market-analysis.md` §7.4. Determines whether the affiliate line in §3.4 and §7 carries a 30% haircut or none. Material: the affiliate line is ~22% of combined revenue in the central case. | Qualified counsel | Before affiliate revenue accumulates |
| 7 | **Whether affiliate links alone trigger YouTube's paid-promotion declaration.** Carried gap. Mitigation (always declare) is free and costs neither reach nor revenue. | Re-verification; interim posture is "always declare" | Standing |
| 8 | **The Vietnamese tax position** (`RK-003`, narrowed by D-011 to the household-business regime). Unchanged by this analysis. Blocks banking, not building. | Qualified Vietnamese counsel | Before revenue arrives |
| 9 | **Every RPM and every view figure in this file.** No RPM here is first-party and none can be. The AIR dataset is the best-sourced found and it measures established channels, not new ones. The trajectories in §3.3 are constructed, not observed. **Do not quote $2.66, 22,635, or "month 7" as though they were measurements.** | Real Studio data after 3 months | Re-run at 3 months post-launch |
| 10 | **Target values and the "clean record" bar** — `Q-004` from the Framing Gate, still open. This analysis assumes per-video CEO approval (D-002) continues throughout, which sets the ceiling on sustainable output at the 13/month already committed. | omn-product-owner → CEO | Wave 2 scoping |

---

## 10. Sources

**First-party [OFFICIAL] / [OFFICIAL POLICY]** — all read 2026-09-26
- YouTube channel monetization policies (inauthentic content; AI personas on sensitive topics; reused content): <https://support.google.com/youtube/answer/1311392>
- YouTube — disclosing altered or synthetic content: <https://support.google.com/youtube/answer/14328491>
- YouTube — YPP changes effective 1 Feb 2027; terms acceptance by 31 Jan 2027: <https://support.google.com/youtube/answer/12843009>
- YouTube — YPP eligibility: <https://support.google.com/youtube/answer/72851>
- YouTube — advertiser-friendly content guidelines: <https://support.google.com/youtube/answer/6162278>
- YouTube — determining whether content is "made for kids": <https://support.google.com/youtube/answer/9528076>
- YouTube — ads on content set as made for kids: <https://support.google.com/youtube/answer/9713557>
- IRS — United States income tax treaties A to Z (Vietnam absent): <https://www.irs.gov/businesses/international-businesses/united-states-income-tax-treaties-a-to-z>
- Adobe Stock video search result counts (§2.2), read directly 2026-09-26: <https://stock.adobe.com/search/video>

**[THIRD-PARTY]**
- AIR Media-Tech — RPM by niche, 300 channels / 3,595 channel-months, May 2025–May 2026 *(only niche RPM source with a stated sample and period)*: <https://air.io/en/air-data-findings/which-youtube-niche-makes-the-most-money-in-2026-ranked-by-real-rpm-and-cpm> · <https://air.io/en/air-data-findings/how-much-does-youtube-really-pay-in-2026-real-rpm-data-from-300-channels>
- MilX — CPM/RPM by niche, 2026-03-16 *(conflicts with AIR on tech; recorded, not used as anchor)*: <https://milx.app/en/trends/youtube-cpm-rpm-rates-2026-average-niches-countries-more>
- Tubefilter, 2026-07-13 — inauthentic content policy update: <https://www.tubefilter.com/2026/07/13/youtube-inauthentic-content-monetization-policy-update/>
- TechCrunch, 2026-07-20 — YouTube clarifies AI slop policies: <https://techcrunch.com/2026/07/20/youtube-clarifies-policies-around-ai-slop-and-upsetting-videos/>
- OutlierKit — the January 2026 terminations (16 channels, ~35M subs, ~4.7bn views): <https://outlierkit.com/resources/youtube-ai-slop-crackdown-2026/> · <https://outlierkit.com/blog/youtube-ai-crackdown>
- The Next Web / Hollywood Reporter — collateral damage to faceless creators: <https://thenextweb.com/news/youtube-ai-slop-crackdown-faceless-creators-collateral-damage> · <https://www.hollywoodreporter.com/business/digital/faceless-creators-youtube-ai-damage-1236617586/>
- ShortsFast — saturated faceless niches 2026: <https://shortsfast.com/blog/saturated-faceless-youtube-niches-2026/>
- Humble&Brag — new-channel view, CTR and retention benchmarks: <https://humbleandbrag.com/blog/new-youtube-channel-average-views> · <https://humbleandbrag.com/blog/youtube-audience-retention-benchmarks>
- vidIQ — average view duration; 4,000 watch hours: <https://vidiq.com/blog/post/average-view-duration/> · <https://vidiq.com/blog/post/how-to-generate-4000-hours-watch-time-youtube/>
- Time to 1,000 subscribers (6–18 months typical): <https://touhfa.art/blog/growth/how-long-to-get-1000-youtube-subscribers/>
- wecantrack — YouTube affiliate conversion, CTR and content-format statistics 2026: <https://wecantrack.com/insights/youtube-affiliate-marketing-statistics/> · <https://wecantrack.com/insights/affiliate-conversion-statistics/> · <https://wecantrack.com/insights/affiliate-click-through-rate-statistics/>
- Made-for-kids revenue impact: <https://www.techtimes.com/articles/320340/20260713/ai-kids-cartoon-gold-rush-has-hidden-tax-coppa-cuts-revenue-80.htm> · <https://gyre.pro/blog/how-to-monetize-a-youtube-kids-channel>
- Channel scale benchmarks: Real Engineering (<https://en.wikipedia.org/wiki/Brian_McManus_(YouTuber)>), Wendover Productions (<https://en.wikipedia.org/wiki/Sam_Denby>, <https://socialblade.com/youtube/channel/UC9RM-iSvTu1uPJb8X5yp3EQ/realtime>), Asianometry (<https://screenlace.com/how-asianometry-grew-to-270k-subscribers-on-youtube>)
- Storyblocks library size (~7M clips): <https://photutorial.com/storyblocks-review/>

**Internal, reused not restated**
- `research/language-market-analysis.md` — §5.1 model, §5.6 assumption register, §3 withholding, §7.4 affiliate net-per-conversion
- `research/platform-policy-dossier.md` — §1.3, §1.4 originality regime
- `research/reverification-2026-09-26.md` — §3 the $77.41 envelope; item 9 the persona rule
- `research/ceo-decision-record.md` — D-001 to D-012
- `research/ai-capacity-dossier.md` — stock and music licence terms, Storyblocks/Uppbeat routing
- `research/animal-niche-analysis.md` — **commissioned, not landed at time of writing**; governs the animal/wildlife variant
