# AI MEDIA COMPANY MASTER PLAN

**Deliverable:** the required first deliverable of §75 of the CEO brief (`tasks/MC-1/input.md`).
**Wave:** 0 — Research and Architecture. **Status:** complete; no significant implementation yet.
**Compiled:** 2026-09-18. **Run:** `run-258e0a3415d2`.
**Governing decisions:** CEO Decision Record D-001 to D-008; Recommendation Gate approval of Option O-002.

### How to read this document

Two labelling conventions run throughout and are never collapsed into each other.

| Label | Meaning |
|---|---|
| **[OFFICIAL POLICY]** | Stated on a first-party platform or vendor page, cited in the source dossier. |
| **[DOCUMENTED RECOMMENDATION]** | First-party guidance or best practice. Not an enforceable rule. |
| **[INDUSTRY PRACTICE]** | Widely reported, third-party sourced. Never the sole basis for a policy claim. |
| **[INFERENCE]** | Reasoning from the above. **Not policy, and never promoted to policy.** |

| Label | Meaning |
|---|---|
| **ESTIMATE** | A computed figure over unit prices and assumptions that are stated so the arithmetic can be re-run. **No cost, capacity or revenue figure in this plan is a fact.** |
| **MEASURED** | A figure produced by instrumentation. There are none yet; the company has published nothing. |

Every unit price in this plan is dated 2026-09-18 and is a **secondary record**: no role in this run reached a first-party pricing page at first hand (`E-068`). Four live first-party pricing pages contradict themselves (`E-056`). The token ledger carries a declared uncertainty of roughly **±30%** because the current model generation is documented to produce approximately 30% more tokens for the same text (`E-047`, F.5 of the capacity dossier). Where a number is load-bearing, the arithmetic is shown or the artifact identifier is cited.

---

## 1. Executive Summary

**What is being built.** A small AI-operated media company: one human CEO owns it, a workforce of AI agents operates it, and it earns net profit by publishing original video under enforced copyright, quality, policy and cost controls. It is not an AI video generator. The distinction is load-bearing: the platform regime that decides whether this company earns anything penalises *generated output that is templated and minimally transformed*, not generated output as such.

**What has been decided and is not reopened here.**

| ID | Decision | Consequence carried into this plan |
|---|---|---|
| D-002 | The CEO approves **every publication individually** until a recorded clean-record threshold replaces it | The publishing gate carries a mandatory human approval step. Not a configurable default. |
| D-005 | Launch revenue is **platform advertising + affiliate links** only | Sponsorship and fan funding appear in no projection. Affiliate is earnable pre-threshold; advertising is not. |
| D-006 | Visuals are **licensed stock and music, original motion graphics and data visualisation, sparing short AI cutaways** | Moves the cost centre from generation to licensing. Cuts the illustrative monthly figure from ~$536 to ~$86 ESTIMATE. |
| D-007 | **Vietnam-resident payee; YouTube only at launch** | The only internally consistent pairing: TikTok's rewards programme excludes Vietnam, Meta's country list omits it. |
| D-008 | **One English-language YouTube channel.** One complete video, then 3/week, then expand | Supersedes D-003's four-platform, two-language scope, which is deferred, not cancelled. |
| Gate | **Option O-002 approved at USD 77.41/month** for channel one ($34.42 metered + $42.99 standing), all seven preconditions accepted | The committed operating configuration. ESTIMATE. |
| Override | The tech lead recommended cutting the nine-wave plan to the MVP (`RK-014`). **The CEO kept the full nine-wave plan** | §35 presents all nine waves as the plan of record. `RK-014` is recorded once, neutrally, as an accepted and unmitigated risk in §27. |

**The three findings that decide whether this company survives.**

1. **The visual-sourcing decision, not the model decision, sets the budget.** Research, scripting, fact-checking, search, thumbnails and narration together are about **$1.58** per ten-minute video ESTIMATE. Generating the *pictures* is **$30 to $240** for the same video ESTIMATE — 19x to 152x. For a sixty-second short, generated video is 12x to 100x everything else combined. D-006 is therefore the single highest-leverage decision on this page (`E-047`, `E-049`, `E-050`).

2. **The fatal enforcement is at channel and account level, and an automated pipeline produces correlated failures.** A third copyright strike makes the account *and any associated channels* subject to termination [OFFICIAL POLICY]. Templated mass-production is named and caught by the inauthentic-content and spam policies with penalties reaching the channel [OFFICIAL POLICY]. Synthetic personas delivering health, legal, finance or political information cannot monetize, stated at channel level [OFFICIAL POLICY]. Because a template defect affects every output rather than one, **publishing velocity is itself a risk variable** [INFERENCE]. The company has one channel and no second platform with a confirmed monetizable route for its payee jurisdiction.

3. **The CEO's own §42 account pool is foreclosed by provider terms, and most of the CEO's own §71 success metrics are uncomputable before programme entry.** Two of three major consumer AI subscriptions prohibit automated or programmatic access outright; the third routes business use away from the consumer product [OFFICIAL]. The consumer video allowance is about eighty seconds a month [OFFICIAL]. Separately, revenue, RPM, profit per video, profit per channel, ROI and cost per dollar of revenue all require advertising revenue that cannot exist until the partner-programme thresholds are met. Building those dials now produces exactly the dashboard of meaningless metrics §72 of the brief forbids. Both challenges, with their alternatives, are in §13, §27, §30 and §31.

**What the CEO is authorising.** A monthly envelope of **$77.41 ESTIMATE** for channel one: $34.42 metered and stoppable within a billing cycle, $42.99 standing, of which $30.00 is an annual-billing commitment that is **not signed until the first complete video has been produced and metered** against the ledger in §24.

**What is not yet established.** No revenue-per-thousand-views parameter exists in any source, so break-even is a formula and not a number (§24). No minutes-per-approval baseline exists, so the CEO's own throughput limit is unmeasured (§7). The Vietnamese tax position is below first-party standard and blocks banking, not building (§40).

---

## 2. Business Model

**The unit.** One English-language YouTube channel publishing three ten-minute narrated long-form videos a week, produced Monday to Saturday with Sunday a company-wide non-operating day, from a production buffer so publication never depends on same-day work (`R-031`, `R-033`, `E-061`).

**How money arrives, and when.**

| Source | Earnable from | Gate |
|---|---|---|
| Affiliate links | Day one | Disclosure on every carrying item. The platform's affiliate position is a **named gap** in the policy evidence (`RK-007`) — neither the paid-promotion page nor the branded-content policy addresses them. The conservative reading is adopted: declare paid promotion on every affiliate item until the position is established. |
| Platform advertising | Only after partner-programme entry | 1,000 subscribers **plus** either 4,000 qualified watch hours in 12 months or 10M qualified Shorts views in 90 days, before 2027-02-01. From 2027-02-01 a new entrant needs 8,000 watch hours or 20M Shorts views [OFFICIAL POLICY]. |

This company has published nothing, so it is a **new entrant**. The 2027-02-01 threshold is the only fixed date in the entire plan and it doubles the requirement. Updated programme terms must be accepted by 2027-01-31.

**Unit economics, all ESTIMATE.**

| Configuration | Variable/month | Standing/month | Total/month | Per video all-in |
|---|---|---|---|---|
| O-001 Minimum cost | $3.66 | $16.50 | **$20.16** | $1.55 |
| **O-002 Balanced — COMMITTED** | **$34.42** | **$42.99** | **$77.41** | **$5.95** |
| O-003 Maximum reliability | $107.22 | $126.98 | **$234.20** | $18.02 |

Thirteen videos a month (52 ÷ 12 weeks × 3 = 13). $34.42 ÷ 13 = $2.65 variable per video; ($34.42 + $42.99) ÷ 13 = $5.95 all-in. The full per-line derivation is in §24.

**Break-even is a formula, not a number.** No supplied source states a revenue per thousand views for the launch platform — the policy dossier does not even record it as unverified. It is carried as an open question (`Q-001`), not closed by an invented parameter.

```
break-even monthly views = (monthly total ÷ revenue per 1,000 views) × 1,000
cost per 1,000 views     = monthly total ÷ (monthly views ÷ 1,000)
```

Illustration at three unsourced parameter values spanning an order of magnitude, for O-002:

| Revenue per 1,000 views (unsourced) | Break-even monthly views |
|---|---|
| $2 | 38,705 |
| $4 | 19,353 |
| $8 | 9,676 |

At an illustrative 50,000 monthly views the cost per thousand views is $1.55 for O-002. **These figures describe a state the company has not reached**: they apply only after programme entry. Until then every option runs at full cost with zero advertising revenue.

**Watch-hour illustration, parameters unsourced.** 4,000 qualified watch hours = 240,000 watch minutes. Cumulative long-form views required = 240,000 ÷ average view duration in minutes. At 4 minutes that is 60,000 views; at 2 minutes, 120,000. No supplied source establishes an average view duration. Qualified watch hours count public long-form only.

**What the model does not contain.** Sponsorship, brand deals, fan funding, memberships and digital products, all excluded by D-005. Any projection that includes them is outside the approved model.

---

## 3. Platform Research

Research was conducted against first-party platform pages and compiled in `research/platform-policy-dossier.md`, verified 2026-09-18. Four platforms were researched because D-003 put all four in scope; D-008 narrowed the launch to YouTube. **The other three rows are retained** because expansion is deferred, not cancelled, and because §26 of the framing (`R-026`) forbids applying a rule established for one platform to another without its own source.

### 3.1 Platform policy matrix

*Every cell is drawn from first-party sources cited in the dossier and carries the same verification date.*

| Platform | Monetization Requirements | Copyright Rules | AI Content Rules | Reused Content Rules | Disclosure Requirements | Advertiser Restrictions | Major Risks | Source | Last Verified |
|---|---|---|---|---|---|---|---|---|---|
| **YouTube (long-form)** — LAUNCH PLATFORM | Tier 1: 500 subs + 3 public uploads/90d + (3,000 watch hrs/12mo OR 3M Shorts views/90d) → fan funding only, **no ad share**. Tier 2: 1,000 subs + (4,000 watch hrs/12mo OR 10M Shorts views/90d) → ads + Premium. **From 1 Feb 2027 new entrants need 1,000 subs + (8,000 watch hrs/365d OR 20M Shorts views/90d).** Existing partners' *membership* grandfathered. New activity floor applying to all: 1,000 watch hrs/365d OR 1M Shorts views/90d OR 2 long-form / 5 Shorts per 90d. Terms accepted by 31 Jan 2027. Requires eligible country, ad-payment account, 2-step verification, no active CG strikes. **Vietnam eligible.** | Content ID claim (revenue to claimant, normally no strike) is separate from a copyright strike (legal removal). **3 strikes in 90d terminates the account "along with any associated channels."** Strikes expire 90d after Copyright School. Dispute → claimant has 30d; appeal → 7d; claimant may escalate to takedown = strike. Repeated dispute abuse penalised. **Must hold written commercial-use rights to all audio and visual elements**; purchased third-party content may not be monetized without such a grant; commercial sound recordings are not monetizable. | AI allowed; **originality is the test, not the tool.** Must disclose realistic altered/synthetic content. Not required for: unrealistic content, minor edits, AI scripts/captions/thumbnails, cloning one's own voice. Auto-applied via C2PA 2.1+; auto-labels cannot be removed. **Disclosure does not affect reach or earnings.** Consistent non-disclosure → removal or YPP suspension. **AI personas delivering health, legal, finance or political information: the channel cannot monetize.** | Two separate live policies. **Inauthentic content** (renamed from "repetitious content" 15 Jul 2025) — must "Be your original creation", "Not be mass-produced, generic, repetitive, or manipulative"; bans "AI-generated content made with generic or unoriginal templates giving the impression of mass production", image slideshows, templated storylines with minimal narrative, and content exclusively featuring readings of material not originated by the creator. **Reused content** — bans repurposing without significant original commentary. ALLOWED: critical review; reaction with commentary; edited footage with added storyline and commentary; **"using AI to visualize a unique character and narrative you invented."** | Paid promotion checkbox in Studio → disclosure label at video start. Branded Content Policy: prohibited categories (drugs, weapons, hacking software, counterfeits, essay services); restricted categories requiring brand certification (alcohol, financial services, healthcare, gambling, elections). AI disclosure via the Studio "altered or synthetic content" attribute. **Affiliate links: not addressed on any first-party page located — a gap, not a permission.** | 14 categories limit or remove ad revenue: inappropriate language; violence; adult; shocking; harmful/unreliable; hateful; drugs; firearms; controversial issues; sensitive events; enabling dishonest behavior; inappropriate for kids/families; incendiary and demeaning; tobacco. Green/yellow/red states. **Self-certification inaccuracy can trigger a YPP eligibility review**; accuracy typically determinable after 20 rated videos. One appeal per video, ≤7 days, decision final. | Channel-level demonetization for templated AI output; **"related channels" contagion, a term the platform never defines**; spam-policy escalation to termination rather than mere demonetization; sensitive-topic AI personas; auto-disputing Content ID claims converting a revenue loss into a strike; the 2027 double gate. | support.google.com/youtube/answer/ 72851, 12843009, 1311392, 14328491, 15447836, 6162278, 2814000, 2797370, 2797454, 2490020, 2801973, 2802032, 1727191, 7101720, 13429240, 9914702, 7687980, 7083671, 12504220, 154235, 17596007, 16440338, 10834785; blog.youtube (2027 YPP post 10 Aug 2026; AI disclosure post 18 Mar 2024) | 2026-09-18 |
| **YouTube Shorts** | Same YPP tiers. Shorts-specific: **from 1 Feb 2027 a continuously re-tested 10M qualified Shorts views/90d is required to earn from the Shorts Creator Pool — and this applies to existing partners too.** Falling below removes neither YPP membership nor long-form earnings; sharing resumes automatically. Split: Shorts 45% of the distributed pool, long-form 55%. | As long-form. | As long-form. | As long-form, plus Shorts-specific view ineligibility: non-original Shorts; reuploads of other creators' content; compilations with nothing added; artificial views; views inconsistent with the advertiser guidelines. | As long-form. | As long-form. | **Views can be individually ruled ineligible, so headline counts may materially overstate progress toward the 10M/20M thresholds.** A channel can sit inside YPP and earn nothing from Shorts. | support.google.com/youtube/answer/12504220, 12843009 | 2026-09-18 |
| **TikTok** — deferred | Creator Rewards: 18+; **Personal Account only** (Business, political, government ineligible); 10,000 followers; 100,000 views in last 30d; good standing; public; videos **≥1 minute**. **Only eight countries: US, UK, Germany, Japan, South Korea, France, Mexico, Brazil — Vietnam excluded**, and qualified views count only from those eight. Per video: ≥1,000 qualified For You views; not Duet/Stitch/Photo Mode/ad/sponsored. Shop Affiliate: 1,000 followers, US. LIVE: 1,000 followers. | IP Policy effective 26 Apr 2025. Copyright strikes run separately from trademark; expire after 90d; "There is a strike limit for each IP type, after which we'll permanently remove the account" — **the number 3 appears only in the platform's worked example, not as a published rule.** Discretionary repeat-infringer ban. Rewards on removed videos clawed back. **"Commercial Sounds are the only sounds made available on TikTok for Commercial Uses"; the licence is TikTok-only.** | AI allowed. Must label AIGC showing **realistic** people or scenes. **Explicitly NOT required for: artistic styles such as anime; generic text-to-speech narration that is not a recognizable voice of a known individual; minor edits.** Manual and automatic labels via C2PA and invisible watermarking; **both irreversible after posting**; mislabelling unaltered content is itself a violation. ToS §3.10 bans automated use of the platform's own generative features and bans altering content-authenticating metadata. | **Originality Policy:** unoriginal = copied completely; largely repurposed without creative edits; combined from multiple sources without added value; carrying someone else's visible watermark. Unoriginal content is feed-ineligible **and ineligible for monetization programmes, with "originality" a key metric in the rewards formula.** Non-original list adds looping videos, single or multiple photos, text-overlay-only content, lip syncs. **Hard thresholds: 5 video violations in 30d → disqualification; 5 total account violations → permanently ineligible.** ALLOWED per the platform: appear onscreen, **add your own voice-overs**, restructure with real editing. | Content disclosure toggle mandatory for commercial content; label cannot be changed after posting. **Branded Content Policy (published 4 Aug 2026, effective 31 Aug 2026) expressly covers affiliate links and promo codes.** 15 prohibited industries; 11 restricted. From 24 Sept 2026 undisclosed commercial content moves from feed-ineligible to reduced visibility; account-ban risk for repeated failure unchanged. | Inventory Filter Expanded/Standard/Limited across 14 categories; Category Exclusion; Vertical Sensitivity; Video and Profile Feed Exclusion Lists. **No feed category named "mass-produced" or "AI slop" exists.** | The 5-violation cliff, permanent at account level; **account-level feed suppression with no violation and no appeal trigger**; originality scored into RPM so revenue decays before any flag; the Photo Mode / slideshow trap; **multiple-account abuse and VPN use are named prohibitions**; the Business-vs-Personal forced choice; irreversible labelling. | tiktok.com/support/faq_detail?id= 7581821550694013452, 7543604786688563768, 7636670084747893268, 7636670088170904085; creator-academy articles; tiktok.com/legal/page/global/ copyright-policy, bc-policy, commercial-music-library-user-terms; tiktok.com/safety policies; ads.tiktok.com help; newsroom.tiktok.com | 2026-09-18 |
| **Facebook** — deferred | Legacy In-stream Ads, Ads on Reels and the Performance Bonus **ended 31 Aug 2025**; In-stream for Live ended **15 Jun 2026**. **Facebook Content Monetization is invite-only with NO published numeric thresholds.** Only published numbers: established presence ≥30 days; reels ≥10s; Stories ≥5s. Creator Fast Track (18 Mar 2026): $1,000/mo at 100K+ followers elsewhere, $3,000/mo at 1M+. Two policy layers — account level and content level. Published country list: **Vietnam absent; Vietnamese present as a supported language.** | Rights Manager matches video, audio, images and Live across both Meta platforms; claimants may block, claim ad earnings, monitor or report; payouts withheld during review. **No published numeric copyright strike counter** — a discretionary repeat-infringer policy. Counter-notification restoration "up to 14 working days". Music: commercial use prohibited without licence; **only clips with under 90 seconds of licensed music can be monetized**; the free collections are licensed **for Meta products only**. | "AI info" label. **Must self-disclose photorealistic video or realistic-sounding audio** — the platform's own example is "a reel narrated with a realistic AI-generated voiceover." **Images are exempt from the self-disclosure duty** but still auto-labelled if detected. Auto-detection via IPTC, C2PA, invisible watermarks; the platform states it **cannot yet detect third-party AI audio and video at scale**. Penalty published only as "There may be penalties." **"AI" appears zero times across all four monetization policy documents — there is no published ban on monetizing AI content.** | You may only monetize content you created, were involved in creating, or that directly features you. "Content that is unoriginal or reproduced without making meaningful enhancements … cannot be monetised." NOT transformative: borders; logos or watermarks; on-screen captions; **background music**; speed changes; subtitle transcripts; **voiceover that merely describes**; watch-along reactions; **basic compilations of spliced clips**; intro/outro only. **"This policy can still apply to copyrighted or licensed content."** Penalty: **90-day demonetization**, extendable, permanent on repetition, and **accrued unpaid earnings may be withheld and never paid.** | Branded content tool mandatory; "exchange of value" includes gifts, loaned products **and affiliate commissions**. "Creators cannot accept anything of value to post content that does not feature themselves or that they were not involved in creating." Format limits: no embedded ads; no title cards in the first 3 seconds; interstitials ≤3 seconds. | **Prohibited formats that cannot be monetized at all: static videos; static image polls; slideshows of images; looping videos; text montages; embedded ads.** Prohibited behaviours: engagement bait. Prohibited categories: misinformation; misleading medical information. Restricted: debated social issues; tragedy or conflict; objectionable activity including copyright infringement; sexual or suggestive; strong language. One appeal per reel, ≤7 days. | **The prohibited formats may disqualify templated AI video by construction**; licensing does not cure unoriginality; **high-volume crossposting is an account-level violation**; entry is invite-only with no published thresholds; AI voiceover triggers mandatory disclosure; background music is both a non-transformative edit and a licensing tripwire; a single Community Standards violation can remove monetization. | facebook.com/business/help/ 1049081556813520, 169845596919485, 1348682518563619, 262834734651607, 3382366608650437, 267128784014981, 1979171292197867, 185404538833362, 2279248852143449, 821453195885988; facebook.com/policies/brandedcontent/; facebook.com/legal/music_guidelines; transparency.meta.com; about.fb.com; creators.facebook.com | 2026-09-18 |
| **Instagram** — deferred | **No in-stream or pre-roll ad revenue share exists.** Gifts/Stars: 500 followers. Subscriptions: 10,000 followers. Creator Marketplace: 1,000 followers. Bonuses: invite-only; **"For reels rewards, only original newly created content counts"**. Affiliate relaunched 24 Mar 2026 with no published thresholds. **Vietnam availability could not be verified.** | Rights Manager and the repeat-infringer policy as on Facebook, with the same absence of a published numeric IP strike count. | The same "AI info" regime, plus the **"AI-generated profile" label** for accounts that regularly post content featuring an AI-generated person instead of a real human — accounts that do not self-label **may become ineligible to appear in recommendations** until the label is added. | **"Unoriginal content" is a PROHIBITED CATEGORY in the Content Monetisation Policies** — one level stricter than Facebook's placement of the same rule. The hard numeric rule: **accounts that repeatedly (10 or more times in the last 30 days) post content from other users they did not create or enhance in a material way will not be shown where content is recommended.** Extended to photos and carousels 30 Apr 2026. | Paid partnership label mandatory; the same "exchange of value" definition including affiliate commissions. Violating posts removed, review requestable within 24–48 hours. | The Content Monetisation Policies mirror Facebook's, **adding Unoriginal content to the prohibited categories.** | **The 10-posts-in-30-days cutoff is the tightest published numeric originality rule on any platform here**; AI-persona accounts must self-label or lose all recommendation surfaces; with no ad-revenue-share product, revenue depends entirely on gifts, subscriptions, brand deals and invite-only bonuses. | facebook.com/business/help/2635536099905516; facebook.com/help/instagram/ 738469380549477, 478012211024479, 1389278101788752, 708013994693013, 434406642308284, 616901995832907, 313829416281232, 1555776438852001, 366220201089101, 1586774981367195; creators.instagram.com | 2026-09-18 |

### 3.2 What the research establishes that binds the architecture

- **The binding constraint is not that generation is used. It is that templated, minimally-transformed output is caught by name on every platform** [OFFICIAL POLICY, four platforms]. The allowed case is stated in the launch platform's own words: "using AI to visualize a unique character and narrative you invented."
- **The originality cures are not portable.** One platform names adding your own voice-over as a cure for unoriginality; another names voiceover that merely describes what happens as explicitly *not* a meaningful enhancement. The same asset can be original on one platform and unoriginal on another. Compliance is evaluated **per platform**, not once. This is Contradiction 5 in §27.3 and it is not an error in either source.
- **Channel-level enforcement is fatal; video-level enforcement is recoverable.** Full breakdown in §38.

### 3.3 Verification standing, and it is not first-party

**No role in this run reached any platform page at first hand** (`E-068`). Every policy statement above is a dated secondary record, which is why the investigation marks those observations *medium* rather than *high* confidence. The dossier itself records its methodological limits: several pages were recovered by browser rendering and extraction of embedded markup and were not re-verified against a second rendering; some Meta pages geolocated and were forced to English; several important conclusions are **absences**, which a single unlocated page would defeat; community and forum pages were not used as a basis for any policy statement.

**Re-verification cadence [DOCUMENTED RECOMMENDATION, from the dossier]: monthly until 2027-02-01**, with a pass before the 2026-09-24 guidelines change and again before the 2027-01-31 terms deadline. This is condition `RK-002` on the Recommendation Gate. Recorded platform policy carries a last-verified date and a policy change raises an alert that triggers re-verification (`R-029`).

### 3.4 What the research could not establish

Sixteen items are recorded as unestablished and must not be treated as known. The five that bear most on this company:

1. **Whether monetization suspension propagates across channels sharing one ad-payment account.** The platform never defines "related channels". Creator-forum reports describe exactly this propagation; the dossier explicitly declines to upgrade them from unconfirmed third-party signal. **This is the single unestablished item with the largest consequence for a multi-channel operation.**
2. **Whether "qualified Shorts views" uses the same exclusion list as creator-pool eligible views.** Any Shorts-led threshold plan may overstate its progress.
3. **The consequence of not accepting the updated terms by 2027-01-31.** Search snippets say earnings stop; not confirmed in either first-party page body.
4. **The launch platform's affiliate-link position** (`RK-007`).
5. **The Vietnamese tax position**: whether the quoted rates and the annual revenue threshold, resting on a 2021 instrument, survive Vietnam's new tax laws with 2026 effect; the withholding treatment of foreign-sourced viewership earnings; Vietnamese advertising-law disclosure obligations; and whether any Vietnamese rule addresses synthetic-media disclosure. Qualified local counsel is required (`RK-003`).

Six widely circulated third-party claims are explicitly rejected by the dossier as unsupported by any first-party page, including the claimed Instagram Reels ad revenue share (Instagram has no ad-revenue-share product at all) and the claimed Facebook Content Monetization numeric thresholds.

---

## 4. Copyright Strategy

Copyright is a **company-wide control system**, not a final QA checklist (§4 of the brief). It is the one control set that may never be reduced, deferred or bypassed to save money (`R-017`, `AI-017`).

### 4.1 What the company builds on

Under D-006: **licensed stock footage and music, original motion graphics and data visualisation, original scripts and narration, with short AI-generated cutaways only where nothing else fits.** Twenty-four seconds of cutaway per ten-minute video — three clips at the documented eight-second clip ceiling — is the budgeted allowance.

Preferred sources, in order: original scripts, storytelling, commentary, graphics and animation; properly licensed stock footage, music and sound effects; verified public-domain assets; AI-generated assets where commercial use is permitted by the vendor's terms.

Forbidden as a business basis: re-uploading other people's videos; movie, TV or sports broadcast footage; copyrighted music without appropriate rights; random images; scraped content; minimally transformed third-party content.

**"We added narration, therefore it is fair use" is not a business rule** and is not implemented anywhere in this system. Fair-use analysis is jurisdiction-dependent and sits outside the workforce's authority (`B-007`). Where rights are uncertain the pipeline **rejects or escalates** (`R-022`).

### 4.2 The commercial-use standard the platform actually imposes

[OFFICIAL POLICY] The uploader must hold **explicit written permission granting commercial use rights for all visual and audio elements**. Purchased third-party content may not be monetized without such a grant. Commercial sound recordings are not eligible for monetization.

The system must be able to answer, for every asset in every published video: *"Why are we legally and platform-policy permitted to use this?"* If the answer cannot be established, the video does not publish (`R-019`, `AI-019`).

### 4.3 Rights record — required fields

One record per third-party asset. These are the minimum fields from §5 of the brief, carried into `R-018` and `AI-018`.

| Field | Notes for this company |
|---|---|
| `AssetId` | Primary key; joined to every `Publication` that contains it |
| `AssetType` | footage / still / music / SFX / voice / font / generated-cutaway / generated-still |
| `Source` | Library or vendor |
| `Creator` | Where the licence names one |
| `LicenseType` | Subscription, perpetual, royalty-free, public domain, vendor-assigned output rights |
| `LicenseURL` | The specific licence text relied on, not the vendor homepage |
| `CommercialUseAllowed` | Boolean; false blocks publication |
| `ModificationAllowed` | Boolean |
| `AttributionRequired` | Boolean; if true, attribution text is a publishing-gate check |
| `PlatformRestrictions` | e.g. a platform-native music library licensed for that platform only |
| `ExpirationDate` | Subscription term end, where perpetuity is conditional |
| `ProofOfLicense` | Invoice, licence certificate or download receipt, stored |
| `CopyrightRisk` | low / medium / high / critical; critical blocks |
| `VerifiedBy` | The Copyright Officer role instance |
| `VerifiedAt` | Timestamp |

Two additional fields this company requires that the brief's list does not name, because the evidence forces them:

| Field | Why |
|---|---|
| `ChannelRegistrationState` | Music and stock licences are scoped by how many channels may be registered, and **revenue lost before registration is stated to be unrecoverable, with no reimbursement** [OFFICIAL, two named libraries]. Registration on every library is a coded precondition of first publication (`RK-008`). |
| `PostCancellationClause` | One library's post-cancellation use right is unverified; another library's own licence text contradicts itself on whether use survives cancellation (`RK-009`). The record names which clause each asset's right relies on, so the weaker reading can be applied deliberately rather than discovered later. |

An **Audio Rights Registry** (§37 of the brief) is not a separate store: it is the `AssetType in (music, SFX, voice)` projection of this table, carrying the same fields plus the platform-restriction field, which matters most for audio.

### 4.4 Separation of duties

The **Copyright Officer** is independent of the role that created or selected the asset (`R-021`).

| Can | Cannot |
|---|---|
| Inspect assets and licences | Approve its own exception |
| Reject an asset | Override platform policy |
| **Block publication** | **Publish** |
| Request replacement | Modify the asset |
| Escalate a rights question | Make a legal determination |

No role approves an exception to a control it is itself subject to (`R-020`).

### 4.5 Standing copyright rules that are not negotiable

1. **Never auto-dispute a content-matching claim.** [INFERENCE, from the dispute path in the policy dossier] Escalation converts a revenue loss into a possible copyright strike, and three strikes in 90 days makes the account and any associated channels subject to termination. A claim is a cost; a strike is existential. Disputes are escalated to the CEO with the rights record attached.
2. **Register the channel on every music and stock library before the first publication.** Unrecoverable otherwise.
3. **Exclude by name the one image service that forbids automated access outright** [OFFICIAL, vendor ToS]. It is disqualifying on four independent grounds: automation prohibited; ownership conditional on a higher-priced plan; a perpetual irrevocable sublicensable licence over inputs and outputs that survives termination; and a disclaimer of warranty of title and non-infringement.
4. **Choose narration terms that do not take a licence over customer content.** One leading voice vendor takes, by default, a licence to use customer content including voice and other indicia of persona to improve and develop its services [OFFICIAL] (`RK-012`).
5. **No music library auto-clears the platform's content-matching system.** [OFFICIAL] No library claims to.

---

## 5. Monetization Strategy

**One route, chosen deliberately.** YouTube Partner Program entry via the long-form watch-hour route, on an English-language channel with a Vietnam-resident payee. This is the only internally consistent pairing available (D-007): the second platform's rewards programme excludes Vietnam and counts qualified views only from eight named countries; the third platform's monetization country list omits Vietnam while listing Vietnamese as a supported language; the fourth platform's Vietnam availability could not be established.

**The gating arithmetic.**

| Milestone | Requirement | Deadline |
|---|---|---|
| Tier 1 (fan funding only, no ad share) | 500 subs + 3 valid public uploads in 90d + (3,000 watch hrs/12mo OR 3M Shorts views/90d) | — |
| Tier 2 (ads + Premium), **before 2027-02-01** | 1,000 subs + (4,000 qualified watch hrs/12mo OR 10M qualified Shorts views/90d) | — |
| Terms acceptance | Updated programme terms accepted in Studio | **2027-01-31** |
| Tier 2, **from 2027-02-01** | 1,000 subs + (**8,000** qualified watch hrs/365d OR **20M** qualified Shorts views/90d) | — |

**The grandfathering correction, because it is widely misread.** The statement that existing partners are unaffected covers **programme membership and entry thresholds only**. Two further requirements apply to everyone, including existing partners: a continuously re-tested 10M qualified Shorts views over the last 90 days to earn each month from the Shorts creator pool, and a new activity requirement of 1,000 qualified watch hours in 365 days, or 1M Shorts views in 90 days, or two long-form videos or five Shorts every 90 days. Both sides of this correction are first-party. The disagreement is between the evidence and the common reading of it (Contradiction 7, §27.3).

**Route selection is an open decision.** Whether the committed three videos a week are long-form only or include short-form is not settled (`Q-004`). It determines which threshold route is taken and changes the per-item unit cost by roughly an order of magnitude — the sixty-second short figures are $0.03 / $0.24 / $1.19 ESTIMATE against $0.11 / $1.58 / $6.44 for the ten-minute item. The plan of record assumes long-form only; the short-form figures are recorded and carried into no monthly total.

**Affiliate revenue is the only pre-threshold source** and carries a compliance obligation that is now a publishing-gate check on every item: affiliate and paid-promotion disclosure (D-005). The launch platform's affiliate position is a named gap; the conservative reading is adopted until counsel closes it.

**Advertiser suitability has direct revenue consequences on both paths**, so topic selection inherits a hard constraint from the revenue model. Fourteen advertiser-friendly categories limit or remove ad revenue. The creator self-rates every video; accuracy becomes determinable after roughly twenty rated videos, and repeated egregious inaccuracy can put programme eligibility under review [OFFICIAL POLICY]. The self-rating is therefore a pipeline judgement with evidence attached, not a checkbox.

**Three per-upload determinations** the platform imposes on every video regardless of configuration: the advertiser-suitability self-rating against fourteen categories; the altered-or-synthetic-content attribute; and the paid-promotion declaration. All three are in the compliance gate pack (§20) and all three appear on the CEO approval surface (§7).

**Deferred, not cancelled.** The three other platforms and the Vietnamese-language channel are deferred by D-008. Reopening them is an **entity-structure decision, not a technical one**, because none of the three has a confirmed monetizable route for a Vietnam-resident payee. The trade-off is recorded as `RK-013` and put back to the CEO as `Q-010`.

---

## 6. Organization

The chart below allocates **responsibility**, not headcount. Multiple boxes may be implemented by one agent where that is economically and technically appropriate (§6 of the brief, `AS-004`). The company's actual agent count is set in §12.

```
                            HUMAN CEO / OWNER
                                    |
                    +---------------+---------------+
                    |                               |
                   COO                             CTO
              (Operations)                    (Technology)
                    |                               |
        production scheduling              architecture
        workload allocation                model routing
        throughput / deadlines             cost optimization
        quality / publishing               observability
        failure recovery                   provider strategy
                    |                               |
                    +---------------+---------------+
                                    |
                          SHARED AI WORKFORCE
                                    |
        +---------------+-----------+-----------+---------------+
        |               |                       |               |
     STRATEGY        CREATIVE              PRODUCTION        FINANCE
     research        design                script            cost records
     audience        thumbnail             visual assembly   channel P&L
     ideation        motion graphics       narration         budget control
        |               |                       |               |
        +---------------+-----------+-----------+---------------+
                                    |
                              COMPLIANCE
                      Copyright / Policy / Fact
                        (independent; can block)
                                    |
                             DISTRIBUTION
                    (publishing; cannot override a block)
                                    |
                               ANALYTICS
```

**Three structural rules.**

1. **Agents are shared resources across channels** (`R-055`). Channel-specific configuration supplies audience, brand, content strategy, language, tone, visual identity and schedule. The company does not create one workforce per channel.
2. **Compliance is independent of production** and can block publication but cannot publish (`R-021`).
3. **The CEO sits above the chart and inside the publishing gate.** Under D-002 the CEO is a mandatory step in every publication, which is a deliberate and temporary departure from the brief's own goal that the CEO not manage individual videos. §7 states the cost of that and what ends it.

---

## 7. CEO Role

**Standing authority.** The CEO is the ultimate authority and the sole owner. Decisions reserved to the CEO and never taken without approval (`R-057`): major channel launch; major channel shutdown; major investment or budget increase; significant business-model change; legal or compliance exception; and — under D-002 — **every individual publication**.

**The weekly review.** Once a week the CEO reviews the COO report, the CTO report, the financial summary, channel performance and the risk summary, and should be able to understand the company's position in **10 to 15 minutes** (`R-063`). Format in §34.

**The per-publication approval, and its honest cost.** D-002 makes the CEO a mandatory gate on every video until the quality, copyright and policy gates have demonstrated a clean record. The threshold that would constitute a clean record is **not yet defined** and must be proposed to the CEO, not assumed (`Q-003`).

This is the binding capacity constraint of the whole company, ahead of any provider limit.

```
approval events per month = 13 ÷ (1 − send-back share)
  at the declared 10% send-back share for O-002:  13 ÷ 0.90 = 14.4 events/month
```

**The minutes per event are established by no supplied source and no operating baseline exists** (`AS-003`). No hours figure is asserted. What is asserted is the structure: at roughly twenty minutes per approval the CEO becomes the pipeline's throughput limit at the committed rate, and the limit tightens linearly with channel count (`RK-005`). The review surface is instrumented from the first video to record actual minutes per approval, so the threshold is set from measurement rather than assumption.

**What the approval surface must carry, on one screen**, so that per-video approval is cheap:

- the rendered video and its thumbnail
- title, description, chapters, tags
- the rights record for every asset in the item, with each licence basis
- the verdict of every gate: content, quality, copyright, policy, originality
- the three per-upload platform determinations pre-filled with their evidence: advertiser-suitability self-rating, altered-or-synthetic-content attribute, paid-promotion declaration
- the item's cost against the envelope
- one Approve, one Send back with reason

**Everything else is autonomous.** Ideation, research, scripting, design, scheduling and cost routing within budget do not come to the CEO (`R-062`). The routine/reserved boundary is *publication*, not a spend threshold.

**What ends this arrangement.** An explicit, recorded, later CEO decision against a proposed clean-record threshold. It relaxes no other way. Re-examination of the approval workload **precedes any second channel**, as the decision record directs.

---

## 8. COO Role

**Mission: make the company run.** The COO answers one question — *are we producing what we planned, at the required quality, on time, and within budget?*

| Responsibility | Concrete measure in this company |
|---|---|
| Production scheduling | Three items a week, Monday–Saturday, from a buffer; Sunday non-operating |
| Workload allocation | Queue depth per stage; no work waiting on a step it does not depend on (`R-035`) |
| Throughput | Planned vs produced vs published vs delayed |
| Quality | Rejection rate, revision rate, copyright rejection rate, policy rejection rate (`R-036`) |
| Publishing schedule | Buffer depth in items; days of cover |
| Failure recovery | No failed step silently dropped; every one retried or escalated (`R-030`) |
| Operational KPIs | Agent utilization; stage latency; bottleneck identification |
| Channel production capacity | Sustainable rate given the CEO approval constraint, not given provider limits |

**The COO's continuous loop** (§67 of the brief), run weekly: are videos on schedule; where is the bottleneck; which agent is overloaded; which is idle; where are quality failures occurring; which workflow creates unnecessary waiting; can tasks run in parallel; can production be buffered further; are channels consuming resources efficiently; what operational change improves throughput.

**The COO's non-negotiable.** Compliance and copyright controls are never cut to hit a schedule. A slipped publication is a cost; a channel-level enforcement outcome is terminal.

---

## 9. CTO Role

**Mission: make the company smarter, cheaper, more automated and more scalable.** The CTO is not merely a software architect.

| Domain | Responsibility |
|---|---|
| Technology | Architecture, infrastructure, automation, reliability, observability, security |
| AI | Capability routing, model selection, context optimization, prompt efficiency, caching, batching, provider selection |
| Economics | AI cost, production cost, infrastructure cost, cost/video, cost per unit of revenue, ROI |
| Business strategy | New channel and content opportunities, new production methods, cost-saving and automation opportunities, revenue opportunities |

**The CTO recommends; the CEO decides.** No major strategic decision is taken silently. Every recommendation put to the CEO carries: opportunity, reasons, required investment, expected production or result, main risks, at least one alternative, and the decision required — approve, reject or experiment (`R-056`, `AI-056`).

**The CTO's continuous optimization loop** (§66 of the brief), run weekly against the cost records: can this task be removed; can code replace AI; can a cheaper capability do it; can tasks be combined; can the result be cached; can it be batched; can output be reused; can context be reduced; can providers be switched; **does this activity actually create business value?**

**The COO↔CTO feedback loop is explicit and instrumented.** The canonical case: the COO reports production too slow → the CTO analyses the bottleneck from stage latency and cost records → finds a premium capability used for a simple task → moves it down a tier → the COO validates operational quality → the change is recorded with the evidence behind it (`R-067`). The loop runs on recorded evidence, not preference.

**At this company's scale, the CTO's honest first finding is that there is very little to optimize.** The variable line is $34.42 a month ESTIMATE. Halving it saves $17 a month. The two levers that actually matter at this stage are the caching lever, which is available and unused and would take O-002 from roughly $5.95 to roughly $5.69 per video ESTIMATE, and the licensing line, which is 56% of the total and is a subscription decision rather than a routing decision. §15 states this plainly rather than building a cost-optimization product for a $34 problem.

---

## 10. CFO Role

**Mission: know what everything costs and what every channel earns, to the video.**

| Responsibility | Requirement |
|---|---|
| Cost records | Every AI operation produces a cost record with consumption, cost, duration and resulting quality (`R-041`) |
| Attribution | Every cost resolves to video, channel, department, role and capability (`R-002`, `AI-002`) |
| Revenue recording | Revenue recorded against the channel that earned it and the source it came from (`R-005`) |
| Channel P&L | Operating profit determinable for the company and each channel over a closed period (`R-004`) |
| Budget control | Configurable budget per department and channel, utilization tracked at 50/75/90/100%, breach raises an alert (`R-039`) |
| Profit allocation | Owner profit, reinvestment, risk reserve, operating budget — configurable, **no rate fixed without CEO approval** (`R-006`) |
| Estimate discipline | Forecasts labelled as estimates carrying their uncertainty, never presented as recorded fact (`R-003`, `AI-003`) |

**The CFO's standing rule, which is also the brief's:** AI agents are **software and compute cost**, not payroll. They are attributed per video, per channel, per department, per agent and per model.

**What the CFO cannot yet produce, and says so rather than estimating it.** Revenue, RPM, profit per video, profit per channel, ROI and cost per dollar of revenue all require advertising revenue that cannot exist before programme entry. Until the first revenue parameter is *observed*, the CFO reports cost, budget utilization and the cost side of unit economics only, and reports break-even as a formula. See §27 `RK-015` and §31.

---

## 11. AI Workforce

The brief's §13 lists thirty-odd capabilities. The company does not create one agent per capability (§13 of the brief is explicit about this). It creates the smallest set of agents with clear responsibilities that covers every capability, and moves anything fully determined by its inputs and a rule out of the workforce entirely into deterministic code.

### 11.1 First cut: what is not an agent at all

The following are **deterministic code** and must never call a model, because their output is fully determined by their inputs and a rule (`R-042`, `EC-009`). This list is the single largest application of the brief's "code first, AI when necessary" principle in the whole plan.

- Render, mux, encode, transcode, loudness normalisation, aspect conform
- Subtitle timing by forced alignment; caption-file emission; chapter timestamp arithmetic
- The asset and rights ledger, and the join that proves every published asset carries a licence record
- The library registration state machine and its day-one precondition on first publication
- The publishing-gate state machine: blocking, release, approval-token verification, idempotent upload
- Schedule arithmetic for the Monday–Saturday week and the production buffer
- Cost metering: token, character, image and second counters; per-video rollup; headroom against the tier spend cap
- Provider routing, quota and spend accounting, failover selection, retry, backoff, circuit-breaking
- Exact and near-duplicate detection across the back catalogue by content hashing and perceptual hashing
- Metadata field population from templates; thumbnail variant compositing
- Audit logging and the evidence record each gate verdict rests on
- Every KPI, cost and budget calculation, and every "did we publish three this week" question

A model is called **only where a judgement is required**, and each judgement is recorded with the evidence it rested on, so a later reviewer can check it rather than repeat it.

### 11.2 The capability map

| Department | Capabilities from §13 of the brief | Where they land |
|---|---|---|
| Executive | CEO interface, COO, CTO, CFO | CEO interface is a **surface**, not an agent. COO/CTO/CFO are **reporting and analysis agents**, run weekly, not per video. |
| Strategy | Market research, audience research, content strategy, ideation | One **Strategy agent**, run per planning cycle, not per video |
| Editorial | Research, fact checking, scriptwriting, story editing, originality checking | **Research agent** and **Script agent**; **Fact-check** is a distinct, independent run; **originality checking** is code (near-duplicate detection) plus a judgement inside the compliance pack |
| Creative | Creative direction, design, thumbnail, motion graphics | One **Creative agent** producing shot lists, scene briefs and thumbnail concepts against the channel brand bible; rendering is code |
| Production | Video production, editing, voice, audio, rendering | **Narration** is a vendor capability, not an agent. Editing, audio and rendering are code driven by the Creative agent's briefs. |
| Compliance | Copyright Officer, Policy Officer, Fact Checker | **Copyright Officer** and **Policy Officer** are separate roles with separate permissions; both can block, neither can publish |
| Distribution | SEO/discovery, publishing | **SEO agent** for title, description, chapters and metadata; **publishing is code** behind the gate |
| Analytics | Performance analysis, experimentation, learning | One **Analytics agent**, run per period, not per video |

---

## 12. Agent Boundaries

### 12.1 The agent set

Ten agents. Each is a judgement-making role; everything else is code.

| AgentId | Name | Department | Runs | Quality floor |
|---|---|---|---|---|
| `AG-01` | Strategy | Strategy | Per planning cycle | L3 |
| `AG-02` | Research | Editorial | Per video | L3 |
| `AG-03` | Scriptwriter | Editorial | Per video (2 passes) | L3 |
| `AG-04` | Fact Checker | Editorial / Compliance | Per video, **independent of AG-03** | L3 |
| `AG-05` | Creative Director | Creative | Per video | L2 |
| `AG-06` | SEO / Discovery | Distribution | Per video | L1–L2 |
| `AG-07` | Copyright Officer | Compliance | Per video, **independent of AG-05** | L3 |
| `AG-08` | Policy Officer | Compliance | Per video | L3 |
| `AG-09` | Quality Control | Compliance | Per video, **independent of every producing agent** | L1–L2 |
| `AG-10` | Analytics & Learning | Analytics | Per period | L2 |

The COO, CTO and CFO are **reporting agents** that run weekly against recorded data, not per video. They are `AG-11`, `AG-12`, `AG-13` and they carry no production permissions.

### 12.2 Agent registry — required fields

Per §16 of the brief. The system must know what each agent is good at.

| Field | Notes |
|---|---|
| `AgentId` | |
| `Name` | |
| `Department` | |
| `Role` | One sentence |
| `Capabilities` | Capability keys the router understands, not provider names |
| `Model` | **Resolved at run time by the router**, recorded on the run, not configured on the agent |
| `ModelTier` | Declared minimum: L0–L4 |
| `CostPerRun` | Rolling measured average; empty until the first video |
| `AverageLatency` | Rolling measured |
| `QualityScore` | From benchmark runs and human rejection rate |
| `SuccessRate` | |
| `Availability` | Derived from the health of the capabilities it needs |
| `Permissions` | Least privilege; see §28 |

Every agent additionally carries, per §14 of the brief and `R-060`: role, responsibilities, inputs, outputs, tools, permissions, constraints, quality criteria, failure conditions, escalation rules, cost budget.

### 12.3 Separation of duties

| Agent | Can | Cannot |
|---|---|---|
| `AG-03` Scriptwriter | Write, revise | Publish; approve its own originality check |
| `AG-04` Fact Checker | Flag, block on unverifiable claim | Write or edit the script it checks |
| `AG-07` Copyright Officer | Inspect, reject, **block publication**, request replacement | Publish; approve its own exception; override platform policy; make a legal determination |
| `AG-08` Policy Officer | Determine disclosure, suitability rating, originality posture; **block** | Publish; relax a platform rule |
| `AG-09` Quality Control | Reject | Produce the work it assesses |
| Publishing (code) | Publish on a complete approval set including the CEO token | Override any block; publish without the CEO approval token |

### 12.4 Agent retirement

The CTO reviews the registry periodically to identify redundant, underutilized, expensive or overloaded agents and candidates to merge or retire (`R-046`). Two merges are already anticipated and deliberately **not** made now: `AG-06` SEO into `AG-03` Scriptwriter, and `AG-09` Quality Control into `AG-08` Policy Officer. Both are held apart until measured, because `AG-09`'s independence from producing agents is an acceptance requirement (`AI-007`) and merging it away would breach it.

---

## 13. Model Strategy

### 13.1 The routing principle

An agent declares **what capability it needs**, never which provider executes it (§42.1 of the brief). Business logic is never coupled to one model.

```
Agent  ->  Task  ->  Capability + quality floor + cost ceiling
                          |
                    MODEL ROUTER
                          |
        capability / quality / cost / availability / spend headroom
                          |
        CAPABILITY-TO-PROVIDER ROUTING TABLE  (primary / secondary / emergency)
                          |
                   Model runtime
```

### 13.2 Task complexity levels

| Level | Meaning | Examples in this company |
|---|---|---|
| **L0** | Deterministic — **no LLM** | Everything in §11.1: render, encode, caption timing, cost arithmetic, schedule arithmetic, duplicate hashing, gate state machine, uploads, KPI calculation |
| **L1** | Cheap model | Classification, tagging, metadata extraction, caption and chapter label polish, CEO approval package assembly |
| **L2** | Medium model | Shot-list and clip selection, motion-graphic and data-visualisation scene briefs, SEO pack, QC review, cutaway prompt authoring |
| **L3** | Premium model | Research synthesis, scripting, fact-check, the compliance gate pack, nuanced copyright risk analysis |
| **L4** | Highest reasoning, where justified | Company and channel strategy, architecture strategy, major cost optimization, escalated claim review. **Priced as an escalation exception, not a standing tier** |

Every task declares a `MinimumAcceptableQuality`. The router selects **the cheapest capability that reliably meets it** — never simply the cheapest (`R-037`, `R-040`). A task that cannot be served at its declared floor **waits or escalates; it never silently completes below the floor** (`AI-037`).

### 13.3 CHALLENGE — the §42 consumer-subscription account pool is foreclosed

**Problem.** §42.2 of the brief gives as a candidate stack four named consumer subscription plans; §42.5 illustrates the router scoring providers by remaining-capacity percentage across those consumer products; §42.11 puts per-provider capacity bars and quota reset times on the CEO dashboard. The architecture cannot be built this way.

**Why.** [OFFICIAL, from the vendors' own terms, `E-043`, `E-044`, `E-046`]

| Provider | Consumer subscription usable as automated production capacity? | Multiple accounts? |
|---|---|---|
| Provider A | **No — explicitly prohibited.** Automated or non-human access is prohibited except via its programmatic key | Not explicitly banned but ambiguous and risky; ban evasion, coordinated multi-account guardrail circumvention, automated account creation and credential sharing all prohibited |
| Provider B | **No — explicitly prohibited.** Programmatic extraction of Output prohibited; using the subscription to power third-party services called out. Its top tier's sign-ups are **paused as of 2026-09-10**, so a plan assuming those seats can be purchased is invalid | Ambiguous. No one-account clause; credential sharing and rate-limit circumvention banned |
| Provider C | **Ambiguous on the letter, no in practice.** Its own API terms say the API is for developers and not for consumer use, routing business use away from the consumer product | **Not addressed at all. Silence is not permission.** |

The consumer video allowance on the relevant plan is about **ten quality generations, roughly eighty seconds of video, per month** — against a requirement of 312 seconds a month at the committed rate.

**This is also an internal contradiction in the brief itself, and it is not resolved here by preferring the convenient clause.** §42.2 illustrates a consumer-subscription pool; §42.10 of the same document states that the system must not assume multiple personal accounts can multiply quotas, forbids account farming and circumvention, and requires the architecture to remain viable if account-based quota multiplication becomes unavailable. Both clauses are recorded (Contradiction 1, §27.3). The framing requirement that capacity never be obtained by prohibited means (`R-049`) applies to whichever is taken forward, and it settles the design question even though it does not settle the textual one.

**Cost.** None. There is no cost case for the risk. At the EXPECTED per-video figure of $1.58, a $200/month top-tier consumer subscription would have to yield **127 videos a month** to break even against buying the same work on the programmatic interface — and the programmatic path has no account-termination risk, no undisclosed weekly caps, published rate limits, a contract that expressly permits powering customer-facing products, and a commitment not to train on customer content.

**Alternative, and it is what this plan adopts.** One production account per provider under commercial terms, and a **capability-to-provider routing table** in place of an account pool. §13.4. The dashboard consequence is in §31.

**Recommendation.** Adopt the routing table. Record consumer chat subscriptions driven programmatically, multiple personal accounts held to multiply an allowance, and credential sharing as **sources forbidden as capacity**, so the design cannot drift back to them. This is `RK-004`, and it is **the one open blocker that blocks building**.

### 13.4 Capability-to-provider routing table

Primary / secondary / emergency per capability. Because every model capability is **metered rather than seat-priced, holding a secondary warm costs nothing standing** — which is what makes the no-single-dependency requirement (`R-051`) affordable at this scale.

| Capability | Primary | Secondary | Emergency | Notes |
|---|---|---|---|---|
| Editorial reasoning L3 | Editorial-grade commercial interface whose terms assign output rights to the customer | A second commercial interface under a different corporate group | Cheap open-weight hosted interface, **item held from publication and the reduced floor recorded** | Never silently downgrades |
| High-stakes review L4 | Frontier tier of the primary provider | Frontier tier of the secondary | **Hold the item and escalate to the CEO** | No cheap fallback exists at this floor |
| Bulk classification L1–L2 | Cheap first-party-contract model | Second cheap hosted model | Local 24GB inference build | The local build is adequate for L0–L1 and **explicitly inadequate for L3–L4** |
| Narration | Naturalness-leading per-character interface | Bulk per-character interface at roughly 1/14 the rate, lower naturalness | That interface's standard voice | Terms must not take a licence over customer content |
| Still images | Interface that assigns output rights | Interface that disclaims ownership and permits commercial use with no revenue threshold | Licensed stock stills already held | |
| Generated cutaways | Cheapest audio-bearing per-second interface | Cleanest commercial-grant per-second interface | **Drop the cutaway, substitute motion graphics** | The one capability whose total loss costs nothing |
| Stock footage | Subscription with the strongest grant | Second subscription | Free-licence libraries, background only, **no indemnity** | Second subscription is the only genuinely expensive redundancy — it sits in O-003, not O-002 |
| Music and SFX | Registered subscription library | Second registered library | **Publish without music** | Registration per channel, day one |
| Web search | Cheapest commercial search interface | Provider-integrated search tool | Manual research queue | |

**The one concentration with no fallback is the publishing platform itself**, fixed to one platform and one channel by D-007 and D-008. That is a decision taken above the architecture, not a gap in it, and its only mitigation is the originality and gate discipline that keeps the channel alive.

**Sources forbidden as capacity**, recorded so the design cannot drift back: consumer chat subscriptions driven programmatically; multiple personal accounts held to multiply an allowance; credential sharing; the image service that forbids automated access outright; the generation interface listed for shutdown on 2026-09-24. **Nothing routes to the shutting interface at any point.**

### 13.5 Model registry — required fields

Per §17 of the brief. **Pricing is never hard-coded permanently** (`R-044`); it is updatable configuration, and a price change is applied without changing how the business operates (`AI-044`).

| Field | Notes |
|---|---|
| `Provider` | |
| `Model` | |
| `Capabilities` | Capability keys, matching the routing table |
| `InputCost` / `OutputCost` | Per million tokens, or per character / image / second as the unit demands; **carries `SourceURL` and `LastVerified`** |
| `CachedReadCost` / `BatchCost` | Both are live levers (§15) |
| `ContextLimit` | |
| `Latency` | Measured, not vendor-claimed |
| `QualityBenchmark` | From the benchmark framework, §16 |
| `Availability` | |
| `TaskCompatibility` | Which declared quality floors it may serve |
| `OutputRightsClause` | Whether the vendor assigns output rights, disclaims ownership, or is silent |
| `TrainsOnCustomerContent` | Boolean. Decides whether proprietary scripts may be sent |
| `TermsPermitAutomation` | Boolean. **False excludes the row from every routing table** |

The last three fields are not in the brief's list. They are added because the evidence shows they decide eligibility more often than price does.

### 13.6 Provider / account table

Per §42.3 of the brief, which requires this table. Every figure is ESTIMATE over unverified unit prices.

| Provider role | Plan | Account qty | Monthly cost | Capacity at committed rate | Best tasks | Backup tasks | Risk |
|---|---|---|---|---|---|---|---|
| Editorial L3 + L1/L2 | Commercial API, pay-as-you-go, batch-priced | 1 | $12.13 metered ESTIMATE (13 × $0.466 + 13 × $0.06 search) | No published rate limit binds; a whole month of input consumes 2.45 min of an entry tier's 2M TPM input allowance | Research, scripting, fact-check, compliance pack | Classification | Price change; tier spend cap |
| Secondary reasoning | Second commercial API, different corporate group | 1 | $0.00 standing; metered on use | Warm at zero standing cost | Failover for all L3 | — | Untested until exercised |
| Narration | Per-character commercial API | 1 | $6.50 metered ESTIMATE (13 × $0.50) + $6.00 entitlement | 130,000 chars/month | Narration | — | **Vendor trains on customer content by default — must be contracted out** (`RK-012`) |
| Still images | Per-image commercial API | 1 | $5.48 metered ESTIMATE (13 × $0.42144) | 182 images/month | Thumbnails, in-video plates | — | Output-rights clause varies by vendor |
| Generated cutaways | Per-second commercial API | 1 | $15.60 metered ESTIMATE (13 × $1.20) | 312 s/month, 8-second clip ceiling | Short cutaways only | — | Highest unit cost in the stack; capped by D-006 |
| Web search | Commercial search API | 1 | Included in the $0.06/video line | 156 searches/month | Research, compliance checks | — | — |
| Local inference | Used 24GB build | 0 | $0.00 — **not purchased** | — | L0–L1 only | Emergency L1 | Ties the cheapest batched hosted tier at best; loses 2–3x against the genuinely cheapest, ~5x once operations time is valued |
| Stock footage | Unlimited subscription, annual billing | 1 | **$30.00 standing** | Unlimited | All footage | — | 12-month commitment; post-cancellation use right **unverified** |
| Music + SFX | Library, 1-channel registration | 1 | **$6.99 standing** | Unlimited | All music and SFX | — | Registration per channel; **pre-registration revenue unrecoverable** |
| **Total** | | **6 metered + 2 standing** | **$77.41 ESTIMATE** | | | | |

**On account count.** The answer to §42.3's question *"how many accounts do we actually need?"* is: **one per provider, and six providers.** Each additional account adds monthly cost, management complexity, authentication complexity, operational risk, terms-of-service risk and monitoring cost, and buys **no additional capacity at all**, because no published provider rate limit binds at the committed rate. Additional accounts would be pure cost against zero benefit, and in the consumer-subscription case would be prohibited as well.

### 13.7 Subscription vs API vs local

| | Verdict for this company |
|---|---|
| **Consumer subscription** | **Excluded.** Prohibited as automated production capacity by two of three providers explicitly and by the third in practice. No cost case (§13.3). |
| **Commercial API** | **Adopted for every model capability.** Programmatic, scalable, measurable, routable, metered, and stoppable within a billing cycle. Pay-per-use is the disadvantage the brief names; at this volume it is the cheapest path anyway. |
| **Local / open source** | **Not purchased.** A used 24GB build run continuously and perfectly batched roughly **ties** the cheapest batched hosted tier, loses by **2–3x** against the genuinely cheapest hosted tier, and by about **5x** in the business case once operations time is valued. The dossier names the dislocated 2026 graphics-card market, not the technology, as the cause. Its genuine non-cost advantages — data residency, no rate limits, deterministic latency, a fixed bill — are not advantages this company needs at 13 videos a month. It is retained as an **emergency L1 row in the routing table**, not as a purchase. |

Total cost of ownership, not headline price, decides any change of sourcing (`R-043`, `AI-043`).

---

## 14. Model Router

**Single responsibility:** select the cheapest capability that reliably completes the task at its declared quality floor, given current availability and spend headroom.

```
TASK (declares: capability, quality floor, cost ceiling, latency class)
 |
 +-- Can deterministic code solve it?  --YES-->  CODE  (no model, no cost record beyond compute)
 |
 NO
 |
 +-- Look up capability in the routing table
 |
 +-- PRIMARY available, healthy, within spend headroom, meets floor?  --YES--> route
 |
 NO
 +-- SECONDARY meets floor?                                            --YES--> route, record fallback
 |
 NO
 +-- EMERGENCY meets floor?
 |      YES -> route, record degraded mode, HOLD item from publication if floor is L3+
 |      NO  -> QUEUE the task
 |
 +-- Task business-critical and queued beyond its deadline? -> ESCALATE to CTO, then CEO
```

**Rules the router enforces, and they are not advisory.**

1. **A critical task never silently downgrades.** If no available capability meets the floor, the task queues or escalates (§42.7 of the brief, `AI-037`). A degraded-mode completion at L3 or above **holds the item from publication** and records the reduced floor on the run.
2. **Routing is keyed by capability, never by provider name.** A tier withdrawal, a price change or a shutdown is therefore configuration, not rework (`RK-010`).
3. **Batch-first for scheduled work.** The company publishes on a schedule, not on demand. The batch interface halves the model line. Synchronous routing is reserved for work whose same-day retry matters.
4. **Cache-first for repeated corpora.** Research and fact-check re-send the same source corpus; caching cuts that input line by roughly an order of magnitude on repeat calls, and cached reads do not count toward one provider's input rate limits, so throughput improves at the same time.
5. **Spend headroom is a routing input.** The router accounts monthly spend against the tier's published cap and against the CEO's envelope, and refuses a route that would breach the envelope without escalation.
6. **Every route is recorded** on the `AgentRun` with the capability requested, the provider selected, why, the cost, the latency and the resulting quality.

**What the router does *not* do at this scale.** It does not score providers by remaining consumer-seat quota, because there are no consumer seats (§13.3). It does not perform dynamic model selection from live benchmark scores; that is Wave 7 work, and until there is a measured benchmark corpus the routing table is a configured ordering, which is honest about what is known.
---

## 15. Cost Optimization

**The honest framing first.** The metered line is **$34.42 a month ESTIMATE** at the committed rate. The standing licensing line is **$42.99**, 56% of the total. Optimising the metered line aggressively saves tens of dollars a month. This section therefore states what is worth doing now, what is worth doing later, and what is not worth building at all — which is itself the brief's "code less but work well" principle applied to cost optimization.

### 15.1 Levers that are live now

| Lever | Effect | Status |
|---|---|---|
| **Deterministic automation** | The largest single lever. Everything in §11.1 runs at zero model cost. A model called for a deterministic task adds cost, latency and a failure mode for nothing | Applied by design |
| **Batching** | Halves the model line for scheduled work | Applied — O-002's model figures are already batch-priced |
| **Prompt caching** | Cuts the repeated-corpus input line by roughly an order of magnitude on repeat calls; cached reads do not count toward input rate limits | **Available and unused.** Applied to O-002 it takes the per-video figure from ~$2.65 to ~$2.42 and the monthly total from $77.41 to roughly **$74** ESTIMATE. Recorded as downside sensitivity, **not baked into any headline figure** |
| **Model routing** | Cheaper capability where the floor permits | Applied — L1/L2 work runs on a cheap first-party-contract model, L3 on an editorial-grade one, L4 priced as an escalation exception rather than a standing tier |
| **Context reduction** | Retrieve only what the decision needs; do not send history (§57 of the brief) | Applied by design in the memory architecture |
| **Reuse** | Templates, channel brand system, validated prompts, reusable production components — **never reuse copyrighted content improperly** | Applied. Note the tension in §15.3 |
| **Parallelization** | Independent stages do not wait on each other (`R-035`) | Applied |
| **Async processing** | Expensive capabilities are not held waiting | Applied |

### 15.2 The lever that dominates everything, and it is not a model lever

**Narration is the largest single line after the cutaways.** Switching narration from the naturalness-leading interface at $0.05 per 1,000 characters to the bulk interface at roughly $0.0035 per 1,000 saves about **$0.40 per video**, a 14x reduction on that line — $5.20 a month at 13 videos. Whether the voice-quality difference is worth $0.40 a video is an **editorial decision, not a cost one**, and it is therefore the CEO's or the Creative Director's, not the CTO's.

**The cutaway line is $1.20 per video, 45% of the entire variable cost.** It buys 24 seconds. Dropping cutaways entirely and substituting motion graphics — which is the emergency row in the routing table and costs nothing — removes $15.60 a month ESTIMATE. It is not done, because sparing cutaways are part of D-006's originality posture, not a garnish.

### 15.3 A cost optimization that must not be taken

**Template reuse is a cost lever that is also the single largest channel-level risk.** Reusing a validated structure across items is cheap and improves quality. Reusing it far enough that output reads as **templated mass production** is named and penalised at channel level on the launch platform. The line is not drawn by cost analysis; it is drawn by the originality discipline in §19, and the compliance gate pack is the control. **Compliance controls are never cut to save money** (`R-017`), and this is where that rule bites hardest, because the saving is real and the penalty is delayed.

### 15.4 What is not built now

A benchmarking-driven dynamic model-selection product, a cost-optimization recommendation engine and a self-tuning router are Wave 7 deliverables. Building them before there is a measured benchmark corpus would optimise a $34-a-month variable line against assumptions. The CTO's weekly cost review (§33) does this work by hand, from the cost records, until the volume justifies automating it.

---

## 16. Agent Benchmarking

**Purpose.** Answer, from evidence rather than preference, which capability is economically best for each task (`R-047`, `AI-047`).

**For each representative task, measure:** quality, cost, latency, success rate, failure rate, human rejection rate. Then compute **cost / quality efficiency** and review periodically.

**The benchmark corpus.** A fixed set of representative tasks with recorded inputs and recorded acceptable outputs, drawn from real production items once they exist:

| Task | Judged on |
|---|---|
| Research synthesis over a fixed source set | Coverage of the claims in the source set; absence of unsupported claims; provenance completeness |
| Script pass over a fixed research brief | Structure, hook, pacing, originality against the source material |
| Fact-check over a script with known planted errors | Detection rate; false-positive rate |
| Compliance gate pack over items with known policy issues | Detection of each issue; correctness of the disclosure and suitability determinations |
| SEO pack | Accuracy against content; absence of spam patterns |
| Shot-list and scene brief | Producibility from the licensed library without a second pass |

**Human rejection rate is the ground truth**, and under D-002 it is available for free: every CEO send-back is a labelled rejection with a reason. This is the one measurement the per-publication approval regime buys the company, and it should be captured deliberately rather than as a side effect.

**Honest status: the corpus is empty.** No video has been produced, so there is no benchmark data and no quality score in the agent registry. The first complete video creates the first row. Until then, the routing table is a **configured ordering justified by vendor quality tier and contract terms**, and this plan says so rather than presenting an unmeasured ordering as a benchmark result.

**Review cadence.** The CTO reviews cost/quality efficiency per task in the weekly report (§33) once there are at least ten comparable runs per task. Before that the review is qualitative and is labelled as such.

---

## 17. Channel Architecture

**Every channel is an independent business unit** (`R-050`). The architecture supports Channel 1 … Channel N **without redesign** (`R-048`, `AI-048`): a new channel is configuration, not code.

### 17.1 Channel record

| Field | Channel 1 value |
|---|---|
| `ChannelId` | CH-001 |
| `Name` | To be set by the Strategy agent at niche selection |
| `Niche` | Not yet selected — see §40 |
| `TargetAudience` / `AudiencePersona` / `AudienceNeeds` | Declared **before content is produced for the channel** (`R-008`, `AI-008`) |
| `Language` | English |
| `Brand` | Channel Brand Bible, §17.2 |
| `ContentPillars` | Declared before production |
| `PublishingSchedule` | 3/week, Mon–Sat operating, Sunday non-operating |
| `Budget` | $77.41/month ESTIMATE, set by the CEO under D-004 |
| `Revenue` / `Costs` / `Profit` | Recorded; revenue is $0 pre-threshold except affiliate |
| `RiskProfile` | High: single channel, single platform, no fallback route for the payee jurisdiction |
| `MonetizationStatus` | Pre-YPP. Target: Tier 2 before 2027-02-01 |
| `PayeeCountry` | Vietnam (D-007) — **set before the channel is created**, because one ad-payment account is permitted per payee name and duplicates are disapproved |
| `LibraryRegistrationState` | Must be `registered` on every library before first publication |

### 17.2 Channel Brand Bible

Per §34 of the brief, held as channel configuration and consumed by the Creative Director agent: visual language, typography, colour system, graphic style, thumbnail style, animation style, storytelling style, editing style, brand rules.

The brand bible is also a **compliance instrument**: it is where "no templated series", "every item carries an invented narrative", and the motion-graphic and data-visualisation house style are written down, which is what makes the originality posture auditable rather than aspirational.

### 17.3 Content strategy per channel

Declared before production (§30 of the brief): target audience, audience persona, audience needs, content pillars, content formats, brand position, differentiation, monetization strategy, risk profile.

The Strategy agent continuously investigates audience demand, content gaps, search intent, competition, monetization potential, production difficulty, copyright risk and advertiser suitability. **Advertiser suitability is a hard input, not a tiebreaker**, because fourteen named categories limit or remove ad revenue and the self-certification record affects programme eligibility.

### 17.4 Channel lifecycle

`IDEA → RESEARCH → EXPERIMENT → ACTIVE → GROWING → STABLE → UNDERPERFORMING → PAUSED → ARCHIVED`

Transitions follow evidence-based rules over a run of recorded results and are **never triggered by one video** (`R-001`, `AI-001`). The thresholds for each transition are not yet set, because no operating baseline exists (`Q-008`); they are a product-owner deliverable before the channel reaches `ACTIVE`.

### 17.5 Shared workforce, per-channel configuration

Agents are shared (`R-055`). What is per channel: audience, brand, content strategy, language, tone, visual identity, schedule, budget, library registration, risk profile. What is shared: every agent, the routing table, the model registry, the rights ledger schema, the gate definitions, the cost model.

**Channel isolation is a corporate-structure question, not a technical one.** [INFERENCE, from the one-account-per-payee rule and the undefined "related channels" term] Genuine isolation between channels requires distinct legal payees, decided before launch, because the payment account's country and payee are hard to change later. This is a live constraint on any future multi-channel plan and it is not solved by software.

---

## 18. Content Pipeline

The canonical lifecycle (§49 of the brief), with the CEO approval step D-002 inserts:

```
STRATEGY -> AUDIENCE RESEARCH -> IDEA -> IDEA SCORING -> APPROVAL
   -> RESEARCH -> SCRIPT -> ORIGINALITY CHECK -> DESIGN
   -> VIDEO PRODUCTION -> AUDIO -> THUMBNAIL
   -> QUALITY CONTROL -> COPYRIGHT CHECK -> POLICY CHECK
   -> [ CEO APPROVAL ]  <-- mandatory, D-002
   -> PUBLISH -> ANALYTICS -> LEARNING -> NEXT IDEAS
```

**Production sub-pipeline** (§36 of the brief): script → storyboard → visual assets → voice → editing → music/SFX → captions → rendering → video draft. Under D-006 "visual assets" means licensed stock clips selected against a shot list, original motion graphics and data visualisations rendered by code, and at most three eight-second AI cutaways.

### 18.1 Asynchronous job system

Queues, per §48 of the brief: `IdeaQueue`, `ResearchQueue`, `ScriptQueue`, `DesignQueue`, `ProductionQueue`, `QCQueue`, `CopyrightQueue`, `PolicyQueue`, `ApprovalQueue`, `PublishingQueue`, `AnalyticsQueue`. Agents consume jobs and produce artifacts. Independent jobs run in parallel; nothing waits on a step it does not depend on (`R-035`).

**The buffer is a requirement, not a nicety** (`R-031`). Publication never depends on same-day production. The buffer target is stated in items of cover, tracked by the COO, and it is what makes the Sunday non-operating day (`R-033`) compatible with a Monday publication.

### 18.2 Idea generation and scoring

Every idea is scored on audience demand, search potential, curiosity, CTR potential, retention potential, evergreen potential, production cost, monetization potential, competition, copyright risk, policy risk, originality and brand fit. **The primary score is not views** (`R-012`).

```
Expected Economic Value = Expected Revenue − Expected Production Cost − Expected Risk Cost
```

**These are not precise predictions and are labelled as estimates** (§31 and §74 of the brief, `R-003`). At this stage `Expected Revenue` cannot be computed at all, because no revenue-per-thousand-views parameter exists (`Q-001`). Until it does, idea scoring runs on the cost and risk terms plus the non-monetary demand signals, and the score is reported as a **ranking, not a currency figure**. Inventing a revenue parameter to make the formula produce a dollar amount is precisely the over-prediction §74 forbids.

### 18.3 Content research

Research agents find reliable sources, verify facts, identify conflicting information, collect references, distinguish fact from opinion, and identify claims needing further verification. **Research provenance is retained** and a factual claim in a published video resolves to an identified source; an unverifiable or conflicting claim is visibly flagged rather than published as fact (`R-009`, `AI-009`).

### 18.4 Scriptwriting

The Scriptwriter produces original scripts, hooks, narrative structure, narration, pacing, CTA, titles, descriptions and chapters. **The script must not be a rewrite of another creator's script** (`R-014`). Two passes are budgeted.

### 18.5 Design and thumbnails

Design produces thumbnails, graphics, illustrations, diagrams, backgrounds, infographics and motion graphics — original or properly licensed. Six thumbnail candidates are budgeted per item, one shipped. Thumbnails are evaluated on clarity, curiosity, readability, mobile visibility, brand consistency and click potential. **Deceptive thumbnails are prohibited** and a deceptive title, thumbnail or metadata set fails the quality gate (`R-011`).

### 18.6 SEO and discovery

Title, description, metadata, chapters, topic clustering, search intent. **No spam metadata** (`R-011`). The SEO pack is L1–L2 work; the judgement it contains is whether the metadata accurately represents the content, which is also a quality-gate check.

---

## 19. Copyright Pipeline

Two mechanisms, run at different points, both blocking.

### 19.1 Per-asset clearance — continuous, at selection time

```
Asset selected by Creative Director
   -> Is it original company work?          YES -> record as original, no rights record needed
   -> Is it generated?                      YES -> vendor terms check: commercial use permitted?
                                                   output rights assigned or disclaimed?
                                                   NO on either -> REJECT
   -> Is it third-party?                    YES -> rights record required, all 17 fields
                                                   CommercialUseAllowed = false      -> REJECT
                                                   ChannelRegistrationState != registered -> BLOCK
                                                   CopyrightRisk = critical           -> BLOCK
                                                   basis cannot be established        -> REJECT OR ESCALATE
```

Clearing at selection time rather than at QC is deliberate: rejecting an asset before it is cut into a timeline costs a re-selection; rejecting it at the gate costs a re-edit.

### 19.2 Per-video rights audit — at the gate, blocking

A deterministic join proves that **every asset present in the published render carries a licence record with `CommercialUseAllowed = true`, a valid `ProofOfLicense`, and `ChannelRegistrationState = registered`**. A missing row is not a warning; it blocks. This is the mechanism that answers "why are we permitted to use this?" for every asset, and it is code, not a model.

### 19.3 Originality check

Three layers, in increasing cost:

| Layer | Mechanism | Cost |
|---|---|---|
| 1 | **Exact and near-duplicate detection across the back catalogue**, by content hashing and perceptual hashing — script, narration, visual sequence, thumbnail | Code, zero model cost |
| 2 | **Similarity check of the script against its source material** (`R-013`, `AI-013`) | L3 judgement |
| 3 | **Originality self-assessment against the platform's own allowed/not-allowed language**, inside the compliance gate pack | L3 judgement, recorded with the reasoning |

Layer 1 exists because the risk this company actually faces is not plagiarism of an outside creator; it is **the pipeline templating itself**. An automated pipeline produces correlated output, and the back-catalogue duplicate check is the only control that catches that before a platform does.

### 19.4 Escalation

Rights uncertainty results in rejection or escalation, never in a transformation argument used as a standing permission rule (`R-022`). Escalated items go to the CEO with the rights record, the specific clause in doubt, and the Copyright Officer's reasoning. **Who is accountable for legal determinations escalated out of the workforce is an open question** (`Q-006` in §40) and, until it is answered, the escalation terminates at the CEO with a recommendation to obtain counsel rather than at an internal determination.

---

## 20. Quality Pipeline

Before publication, independently verify — independently meaning by a party other than the one that did the work (`R-007`, `AI-007`):

| Dimension | Checks |
|---|---|
| **Content** | Factual accuracy against retained provenance; coherence; originality; storytelling; audience value |
| **Visual** | Rendering integrity; audio/video synchronization; spelling; typography; thumbnail quality and accuracy |
| **Audio** | Narration quality; loudness normalisation; clipping; synchronization |
| **Policy** | Platform rules; advertiser suitability across the fourteen categories; community guidelines |
| **Copyright** | Every external asset: music, images, video, voice, fonts, source material |

**Failure of a critical check rejects the item.** No exceptions, no override path for a producing role.

### 20.1 The compliance gate pack

This is the single largest piece of judgement work in the pipeline and the line item O-001 removes and O-002 funds. It is one L3 run producing five recorded determinations, each with its evidence:

1. **Originality self-assessment** against the platform's own allowed and not-allowed language, plus the layer-1 duplicate result.
2. **Synthetic-media disclosure determination**: does this item contain realistic altered or synthetic content requiring the platform's disclosure attribute? Recorded with which element triggered it, or a recorded finding that none did.
3. **Affiliate and paid-promotion disclosure determination** (D-005). Conservative reading applied while the platform's affiliate position remains a gap.
4. **Advertiser-suitability self-rating** against the fourteen categories, with the reasoning for each non-green rating. Repeated inaccuracy puts programme eligibility under review, so this is a recorded judgement, not a checkbox.
5. **Sensitive-topic screen**: does the item present a synthetic persona delivering health, legal, finance or political information? If yes, **it does not publish**, because that is a channel-level monetization bar, not a video-level one.

Each determination is stored with the evidence it rested on so a later reviewer — or the CEO at approval — can check it rather than repeat it.

### 20.2 Deterministic quality checks

Loudness, clipping, sync drift, resolution, aspect, caption timing accuracy, spelling, missing-field validation and thumbnail dimensions are **all code**. No model is called to check whether a required field exists or whether loudness is within range. That is §20 of the brief applied literally.

---

## 21. Publishing Pipeline

### 21.1 The gate

```
CONTENT APPROVED
      +
QUALITY APPROVED
      +
COPYRIGHT APPROVED   (includes: library registration complete)
      +
POLICY APPROVED      (includes: all five compliance determinations)
      +
CEO APPROVAL TOKEN   <-- mandatory under D-002
      =
      PUBLISH

any critical gate fails  ->  BLOCK
```

The gate is a **deterministic state machine**. It does not reason; it checks that a complete, valid approval set exists and that the CEO approval token is present and bound to this exact render. Publishing code **cannot override a block** and cannot publish without the token (§28).

### 21.2 Publishing agent responsibilities

Upload, title, description, thumbnail, schedule, playlist, captions, metadata, platform-specific settings. Upload is **idempotent**, so a retry cannot double-publish.

**Publishing is auditable** (`R-027`, `AI-027`): what was published, where, when, with which metadata and settings, and **on whose approval**.

### 21.3 Coded preconditions of the first publication

Discharged on day one, before anything goes out:

1. **The channel is registered on every music and stock library.** Revenue lost before registration is unrecoverable, with no reimbursement (`RK-008`).
2. **The payee position is settled and the payment account exists.** One account per payee name; duplicates are disapproved and monetization is turned off for the associated channel. This precedes channel creation, not just publication.
3. **Two-step verification is enabled and there is no active community-guidelines strike**, both being programme prerequisites.

### 21.4 Schedule

Target Monday / Wednesday / Friday, or another schedule the system optimises on measured evidence. Whether the non-operating day applies to *publication* as well as to *production* is an open question (`Q-011` in §40); the plan of record schedules no Sunday publication, which is the conservative reading and costs nothing to reverse.

---

## 22. Analytics Pipeline

After publication, collect per video and per channel wherever the platform makes it available (`R-010`): impressions, CTR, views, watch time, average view duration, retention, subscribers, engagement, revenue, RPM, CPM, traffic source, audience, returning viewers.

Produce a **Video Performance Report** and a **Channel Performance Report**.

**What is collectable now and what is not.** Impressions, CTR, views, watch time, AVD, retention, subscribers, engagement, traffic source and audience are available from the first publication. **Revenue, RPM and CPM are all zero and will stay zero until programme entry**, and affiliate revenue arrives from a different system entirely on a different timeline. The analytics pipeline records this as a stated pre-threshold state rather than as null values that read like a data problem.

**The qualified-view caveat that must be carried into every threshold report.** Individual Shorts views can be ruled ineligible for the creator pool, and it is **not established** whether the qualified-view metric used for the entry threshold applies the same exclusion list. Headline view counts may therefore materially overstate progress toward the threshold. Any progress-to-threshold display carries that caveat on its face.

---

## 23. Learning System

After each video, record what worked, what failed and why, across topic, hook, title, thumbnail, audience, length, retention, distribution, timing, production cost and revenue (`R-065`, `AI-065`).

**Do not blindly copy the highest-view video** (§45 of the brief, `R-067`). A change to content, production or cost approach is decided from recorded evidence — an experiment or measured performance — not from preference and not from imitation of a single result.

### 23.1 Experimentation

| Field | |
|---|---|
| `Experiment` | What is being varied |
| `Hypothesis` | Stated before the run |
| `Variant` | |
| `Metric` | Declared before the run |
| `Result` | |
| `Confidence` | Stated honestly; at 13 items a month most single-variable tests will not reach a confident result quickly, and the system says so rather than reporting noise as a finding |
| `Decision` | Adopt / reject / continue |

Candidates: title, thumbnail, hook, length, narrative, visual style, topic angle, upload timing.

### 23.2 Memory architecture

Four scopes, retrieved rather than dumped into context (§57 of the brief, `R-066`):

| Scope | Contents |
|---|---|
| **Company memory** | Business policies, financial rules, **platform policies with their labels and verification dates**, company strategy |
| **Channel memory** | Audience, brand bible, content history, successful formats |
| **Video memory** | Research and provenance, script, assets, licences, production record, analytics |
| **Agent memory** | Lessons, recurring errors, performance |

**Do not dump all history into every agent context.** Retrieval only. This is a cost lever and a quality lever at the same time: irrelevant context both costs money and degrades judgement.

The **company knowledge system** holds business strategy, channel strategy, financial rules, platform policies, agent capabilities, model costs, lessons learned, experiments, historical performance, workflows, failures and successful patterns. The platform-policy portion is the same store the compliance gate pack reads from, so a policy re-verification updates the control and the knowledge base in one act.

---

## 24. Financial Model

Every figure in this section is an **ESTIMATE** over unit prices dated 2026-09-18 that no role verified at first hand, carrying a declared **±30% tokenizer uncertainty** on the token lines.

### 24.1 Per-video task ledger

Tasks marked *(supplied)* come from the capacity dossier's itemised budget; tasks marked *(extension)* are the tech lead's own assumptions covering the work D-006 and D-002 create that the supplied budget does not model.

| Task | Tier | Input tokens | Output tokens |
|---|---|---|---|
| Research synthesis *(supplied)* | L3 | 150,000 | 8,000 |
| Scripting, 2 passes *(supplied)* | L3 | 50,000 | 6,000 |
| Fact-check *(supplied)* | L3 | 60,000 | 3,000 |
| SEO / metadata pack *(supplied)* | L1–L2 | 10,000 | 2,000 |
| QC review *(supplied)* | L1–L2 | 25,000 | 2,500 |
| **Supplied subtotal** | | **295,000** | **21,500** |
| Stock shot-list and clip selection *(extension)* | L2 | 20,000 | 2,000 |
| Motion-graphic and data-visualisation scene briefs *(extension)* | L2 | 15,000 | 3,000 |
| Caption and chapter label polish *(extension)* | L1 | 5,000 | 1,000 |
| Compliance gate pack *(extension)* | L3 | 30,000 | 3,000 |
| Cutaway prompt authoring *(extension)* | L2 | 4,000 | 1,000 |
| CEO approval package assembly *(extension)* | L1 | 8,000 | 1,500 |
| **Extension subtotal** | | **82,000** | **11,500** |
| **PER-VIDEO TOTAL** | | **377,000** | **33,000** |
| of which L3/L4 | | 290,000 | 20,000 |
| of which L1/L2 | | 87,000 | 13,000 |

**Per-video non-token units:** 12 web searches (8 supplied + 4 compliance checks); 14 images (6 thumbnail candidates + 8 in-video still plates); 10,000 narration characters (8,700 supplied + a 15% re-synthesis allowance); 24 seconds of generated cutaway (3 clips at the documented 8-second ceiling). **Motion graphics and data visualisation are rendered by code and carry no model cost.**

### 24.2 Required capacity

| Period | Input tokens | Output tokens | Searches | Images | Narration chars | Generated video |
|---|---|---|---|---|---|---|
| Per video | 377,000 | 33,000 | 12 | 14 | 10,000 | 24 s |
| **Per week** (×3) | 1,131,000 | 99,000 | 36 | 42 | 30,000 | 72 s |
| **Per month** (×13) | 4,901,000 | 429,000 | 156 | 182 | 130,000 | 312 s |

**Headroom finding.** A whole month of input consumes **2.45 minutes** of an entry tier's two-million-tokens-per-minute input allowance. A whole month of output consumes **1.07 minutes** of its four-hundred-thousand-per-minute output allowance. A whole month of narration consumes **3.25%** of a four-million-character monthly free allowance. **No published provider rate limit binds at the committed rate.** The binding limits are the tier's monthly spend cap, the D-002 approval workload and the per-channel licensing registration — and only the first is a technical limit.

This is the direct answer to §42.8 of the brief, which asks the system to *calculate* required capacity rather than guess it. The answer for one channel is that capacity is not the constraint. The CEO is.

### 24.3 The committed arithmetic — O-002, per video

```
L3 work    : 290,000 in x $1.00/MTok (batch)  = $0.290000
             20,000 out x $5.00/MTok (batch)  = $0.100000
L1/L2 work :  87,000 in x $0.50/MTok (batch)  = $0.043500
             13,000 out x $2.50/MTok (batch)  = $0.032500
                                                ----------
             model subtotal                     $0.466000

Search     : 12 x $0.005                      = $0.060000
Images     :  6 thumbnails x $0.05268         = $0.316080
              8 stills     x $0.01317         = $0.105360
                                                ----------
                                                $0.421440
Narration  : 10,000 chars x $0.05 per 1,000   = $0.500000
Cutaways   : 24 s x $0.05 per second          = $1.200000
                                                ----------
PER VIDEO                                       $2.647440
```

```
Monthly variable : 13 x $2.647440             = $34.42
Standing         : stock (annual billing)       $30.00
                   music + SFX, 1 channel        $6.99
                   commercial narration          $6.00
                                                -------
                                                $42.99
MONTHLY TOTAL                                   $77.41
PER VIDEO ALL-IN : $77.41 / 13                = $ 5.95
```

With the caching lever applied: roughly **$2.42 per video and $74 a month** ESTIMATE. Recorded as downside sensitivity and **not baked into the headline**.

### 24.4 The three options side by side

| | O-001 Minimum | **O-002 Balanced (committed)** | O-003 Maximum reliability |
|---|---|---|---|
| Monthly variable | $3.66 | **$34.42** | $107.22 |
| Monthly standing | $16.50 | **$42.99** | $126.98 |
| **Monthly total** | **$20.16** | **$77.41** | **$234.20** |
| Per video all-in | $1.55 | **$5.95** | $18.02 |
| Multiple of O-001 | 1.0x | 3.8x | 11.6x |
| What it buys | A single-pass factual floor | An L3 fact-check **and** a separate L3 compliance gate pack; two model classes from different providers on the reasoning path | Frontier L4 review, dual-source verification, a second stock library, synchronous same-day retry |
| What it gives up | L4 claim review, second-opinion fact-check, generated cutaways, any redundancy above one model class | Standing L4 review; a second stock library | Cost discipline before any revenue exists |
| First thing that breaks at double volume | The originality margin | **The CEO approval workload** | The CEO approval workload, then the per-second generation line |

**Why O-002 and not O-001.** O-001 misses two must-have criteria: no critical capability may depend on a single provider (`EC-003`) — its fallback is the same model class as its primary — and the channel-level originality floor must be held (`EC-004`) — it funds neither an L4 claim review nor a second-opinion fact-check. **A missed must-have is disqualifying, not averaged away.** Its $57.25 a month of saving buys the removal of the review layer that protects the only channel the company has.

**Why O-002 and not O-003.** O-003 meets the same must-haves but costs $156.79 a month more in a period with no revenue, and it *adds* evidence exposure rather than bounding it: its second stock library's licence text contradicts itself on post-cancellation use, and its professional voice entitlement comes from a vendor class that takes a licence over customer voice and persona indicia by default. It is reachable from O-002 later **by adding one subscription rather than by rebuilding**, so nothing is foreclosed by not starting there.

### 24.5 Revenue and cost categories

**Revenue at launch** (D-005): platform advertising; affiliate. Nothing else appears in any projection.

**Costs**, per §50 of the brief: AI (LLM, image generation, video generation, TTS, STT, translation); software (editing, storage, rendering, analytics, automation); infrastructure (servers, GPU, cloud, bandwidth, backup); external (licensed footage, licensed music, freelancers, voice actors, other services). **The $77.41 figure covers AI and licensing only.** It explicitly excludes editing software, hosting, storage, compute orchestration and all human labour — the source dossier says so, and this plan repeats it rather than letting the number be read as a total cost of operation.

### 24.6 Profit allocation

Configurable rates for `OwnerProfit%`, `Reinvestment%`, `Reserve%`, `OperatingBudget%`, applied in the order: revenue → platform, payment and tax costs → operating expenses → production costs → risk reserve → reinvestment fund → owner profit. **No percentage is fixed without CEO approval** (`R-006`), and an allocation change traces to the approval it rested on (`AI-006`). No rates are proposed here, because there is no revenue to allocate.

### 24.7 Reversal cost

The direction is **costly to reverse, not irreversible**, and the cost is bounded and known. All model, search, image, narration and generation spend is metered and stops the moment calls stop, so **$34.42 a month reverses within a billing cycle**. The $42.99 standing line is the part that does not: $30.00 of it is quoted at an annual-billing rate, so reversal inside the term costs the remainder of up to twelve months. Because one library's post-cancellation right is unverified and another's is self-contradictory, **the reversal must assume the weaker reading**: complete and publish every end product during the active term.

What cannot be reversed at all: a channel-level enforcement outcome, and revenue forfeited before library registration. Both sit in the preconditions, not in the reversal plan.

---

## 25. Cost Attribution

**Every AI operation produces a cost record** (`R-041`). No exceptions, including failed and retried runs.

| Field | |
|---|---|
| `Agent` | |
| `Model` | As resolved by the router, not as configured |
| `Provider` | |
| `Task` | |
| `Capability` | The routing key requested |
| `InputTokens` / `OutputTokens` / `CachedTokens` | |
| `NonTokenUnits` | Characters, images, seconds, searches |
| `ApiCost` | |
| `ExecutionTime` | |
| `ResultQuality` | Where assessed |
| `Channel` / `Video` / `Department` | |
| `RunId` | Joins to `AgentRun` and `AuditLog` |
| `DegradedMode` | True where an emergency route was taken; joins to the hold decision |

Derivable from these records (`R-038`, `AI-038`): cost per video, per channel, per 1,000 views, per 1,000 watch minutes, per subscriber, and per unit of revenue.

**Four of those six are uncomputable today**, because they have view, watch-minute, subscriber or revenue denominators that are zero pre-publication and, for revenue, zero pre-threshold. The system computes them when the denominator becomes non-zero and reports them as *not yet available* until then. It does not show a divide-by-zero as a dash that reads like a measurement of nothing.

**Cost attribution is deterministic code.** No model is ever called to compute a cost (§20 of the brief).

---

## 26. Channel P&L

```
Revenue
  − AI costs
  − Production costs
  − Licensing costs
  − Infrastructure
  − Marketing
  − Other costs
  = Channel Operating Profit
```

Produced for the company and for each channel over a **closed period** (`R-004`, `AI-004`). Revenue resolves to the channel that earned it and the source it came from (`R-005`).

The question the CEO must be able to answer is **"which channels actually make money?"**, not "which channels have the most views".

**Channel 1's P&L today, stated honestly:**

| Line | Value |
|---|---|
| Revenue — advertising | $0.00. Not earnable until programme entry. |
| Revenue — affiliate | Earnable from first publication; no basis exists to estimate it. |
| AI costs | $34.42/month ESTIMATE |
| Licensing costs | $42.99/month ESTIMATE |
| Production, infrastructure, marketing, other | Not costed in this plan — the $77.41 figure excludes editing software, hosting, storage, compute orchestration and all human labour |
| **Channel operating profit** | **Negative by at least the full monthly cost, for the whole pre-threshold period.** |

That is not a forecast; it is the arithmetic consequence of D-005 and the programme thresholds. Every option runs at full cost with zero advertising revenue until entry. The financial model must not treat advertising and affiliate revenue as arriving on the same timeline.

---

## 27. Risk Management

### 27.1 Risk register

Categories per §55 of the brief: copyright, trademark, platform policy, monetization, defamation, misinformation, privacy, AI disclosure, reputation, financial.

**Severity determines handling** (`R-061`, `AI-061`), and the handling is enforced, not advisory:

| Severity | Handling |
|---|---|
| **Critical** | BLOCK |
| **High** | HUMAN REVIEW |
| **Medium** | ADDITIONAL AUTOMATED CHECKS |
| **Low** | NORMAL FLOW |

### 27.2 The fifteen recorded risks

Carried from the Recommendation Gate. Status on all: **open**.

| ID | Risk | Sev | Likelihood | Blocks | Owner | Mitigation |
|---|---|---|---|---|---|---|
| `RK-001` | Channel- and account-level enforcement accumulates against an automated pipeline: a third copyright strike makes the account and any associated channels subject to termination; templated mass-production is named and caught with penalties reaching the channel; synthetic personas on health, legal, finance or political topics are barred from monetization at channel level | **critical** | possible | first publish | product owner | Four controls, **none cuttable for cost**: the L3 compliance gate pack; per-publish CEO approval under D-002; the D-006 originality discipline (invented narrative and original graphics per item, no templated series, back-catalogue duplicate detection); and a standing rule never to auto-dispute a content-matching claim. Hold the committed rate; present no synthetic persona on a barred topic |
| `RK-002` | Every unit price and policy statement rests on a dated secondary record no role verified at first hand; four first-party pricing pages contradict themselves | high | certain | first purchase | orchestrator | Re-fetch every unit price before any spend; re-verify policy monthly until 2027-02-01, first pass before 2026-09-24. Treat every figure as an ESTIMATE |
| `RK-003` | The Vietnamese tax and withholding position is unverified: rates rest on a 2021 instrument unchecked against the 2026 tax laws, and the platform's withholding treatment for that residency was not located | high | certain | **banking** | CEO | Qualified local counsel **before the payment account is created**, which is before the first channel is created. No build work waits on it |
| `RK-004` | The brief's §42 account-pool illustration treats consumer chat subscriptions as routable production capacity, which provider terms foreclose and which the brief's own §42.10 contradicts | high | certain | **building** | architect | Adopt the capability-to-provider routing table (§13.4); one production account per provider under commercial terms; a dashboard of spend and per-capability health rather than seat quota |
| `RK-005` | Per-publish CEO approval is the binding capacity constraint ahead of any provider limit, and minutes per approval are established by no source | high | likely | sustaining the rate; any 2nd channel | product owner | Instrument the review surface from the first video; propose the clean-record threshold from a baseline over several approvals rather than assuming one |
| `RK-006` | No advertising revenue until programme entry, and for a new entrant the thresholds rise on **2027-02-01** from 4,000 watch hours / 10M Shorts views to 8,000 / 20M | high | certain | **banking** | CEO | Choose a threshold route now; accept updated terms before 2027-01-31; attempt entry before 2027-02-01 if reachable. Affiliate is the only pre-threshold source |
| `RK-007` | The launch platform's affiliate-link position is a named gap: neither the paid-promotion page nor the branded-content policy addresses them | medium | certain | — | product owner | Declare paid promotion on every affiliate item until the position is established; route to counsel with the other jurisdictional items |
| `RK-008` | Music and stock licences are registered per channel and **revenue lost before registration is unrecoverable** | medium | certain | first publish | product owner | Registration on every library is a coded precondition of first publication, discharged on day one |
| `RK-009` | Library rates require annual billing; one library's post-cancellation right is unverified and another's licence text contradicts itself | medium | likely | — | product owner | Prefer unambiguous perpetuity clauses; sign the annual commitment only after the first video is metered; publish every end product during the active term; record per asset which clause the right relies on |
| `RK-010` | One video-generation interface shuts down **2026-09-24** with no replacement; one cheap text tier's price doubles **2027-01-01** | medium | certain | — | architect | Nothing routes to the shutting interface. Keep the routing table keyed by capability so a tier change is configuration; re-price before the date and re-run the arithmetic |
| `RK-011` | The per-video ledger extends the supplied budget with the tech lead's own task assumptions, and the source records a **±30%** tokenizer caveat | medium | possible | — | QA | Meter actual tokens, characters, images and generated seconds on the first video and re-run the arithmetic before the rate is sustained |
| `RK-012` | One narration vendor class takes a licence over customer content including voice and persona indicia by default; one image service forbids automated access outright | medium | certain | — | architect | Exclude the forbidding service from every routing row; choose narration terms that do not use customer content for development, or keep the voice asset outside the vendor |
| `RK-013` | The four-platform, two-language scope of D-003, superseded by D-008, is reopened before the single channel is proved | medium | possible | — | CEO | Hold D-008. Reopening takes the compliance surface from one regime to four, scales the D-002 workload linearly, and adds three platforms **none of which has a confirmed monetizable route for the payee jurisdiction**. The decision is the CEO's and is put back as `Q-010` |
| `RK-014` | **Accepted, not mitigated — see §27.4** | medium | likely | — | CEO | Accepted by CEO override |
| `RK-015` | Most of the §71 success-metric set is uncomputable before programme entry | medium | certain | — | product owner | See §27.5 |

### 27.3 Recorded contradictions — all seven, carried

None is resolved by preferring the convenient side.

| # | Contradiction | Standing |
|---|---|---|
| **1** | The brief's §42 account-pool design treats named consumer subscription plans as routable production capacity, scores them by remaining-capacity percentage and puts capacity bars on the CEO dashboard; the vendor terms record that two of those providers prohibit programmatic access outright and the third routes business use away from the consumer product, with a consumer video allowance of about eighty seconds a month. **The brief's own §42.10 separately states that multiple personal accounts must not be assumed to multiply quotas and that the architecture must remain viable without account-based quota multiplication.** | **Stands. The disagreement is internal to the CEO's brief and is not resolved here by preferring either clause.** Both are recorded. The framing requirement that capacity never be obtained by prohibited means applies to whichever is taken forward, which settles the design without settling the text. |
| **2** | The approved four-platform, two-language launch scope of D-003 against the brief's own MVP definition of one channel, one complete video, then three a week before scaling. | Resolved **by a later CEO decision**, D-008, in favour of the sequenced definition. The deferred scope is `RK-013`. |
| **3** | The framework's product context describes software-delivery goals and metrics while the frozen context slice still declares that file a required member for this work. | Settled by D-001 in favour of a separate line of business. The slice declaration is recorded as unreconciled. |
| **4** | On the second platform, the commercial sound library is the only source of sounds available for commercial uses and a business account sees only that library — **while the rewards programme requires a personal account and excludes business accounts.** One account cannot hold both positions. Two of that platform's own pages additionally disagree on whether the commercial library is available to all users. | **Stands.** No further source settles it. Material to any future decision to launch on that platform. |
| **5** | The second platform's own remediation guidance names adding one's own voice-overs as a cure for unoriginality; the third platform names voiceover that merely describes what happens as explicitly **not** a meaningful enhancement. | **Stands, and it is two accurate statements about different things rather than an error.** Consequence: the same asset can be original on one platform and unoriginal on another. Compliance is evaluated per platform, never once. |
| **6** | Nine points on which live first-party **policy** pages disagree with one another, and four live first-party **pricing** pages that contradict themselves. | **Stand.** Neither set is resolved here, and neither is resolved by preferring recency or the more convenient page. |
| **7** | The widely repeated reading that existing partners are grandfathered against the 2027 change, against the source's own statement that grandfathering covers programme membership and entry thresholds only, with the 10M Shorts floor and the new activity requirement applying to everyone. | **Corrected rather than standing.** Both sides are first-party; the disagreement is between the evidence and the common reading of it. The corrected reading is the one used in §5. |

### 27.4 RK-014 — the nine-wave plan. Recorded once, neutrally.

**The recorded analysis.** The tech lead assessed the supplied nine-wave plan as building company-foundation registries, publishing scheduling, audience analytics, a model-router product, an AI executive layer and autonomous planning around a pipeline that has not yet produced one video. The recorded delivery impact: effort spent on waves whose value depends on a scale that does not exist consumes the only budget available in the pre-revenue period and delays the one date that rewards starting now, 2027-02-01; an AI executive layer in particular adds model spend and a second review surface without removing the CEO from the loop D-002 makes mandatory. The recommended mitigation was to cut the first wave to what the minimum-viable definition names and defer the analytics, multi-channel, AI-economics, AI-management and autonomy waves until a sustained rate and a revenue signal exist.

**The decision.** The CEO decided to keep the full nine-wave plan. §35 therefore presents all nine waves as the plan of record.

**Status: accepted, not mitigated.** Severity medium, likelihood likely, owner CEO. It is recorded here so that if the pre-revenue budget or the 2027-02-01 date comes under pressure, the cause is already on the register rather than discovered later.

### 27.5 CHALLENGE — most of the §71 metric set is uncomputable, and building it now breaks §72

**Problem.** §71 of the brief specifies eighteen tracked metrics and three primary business metrics. Of those, **revenue, RPM, profit per video, profit per channel, ROI and cost per dollar of revenue** — including all three primaries, net profit, profit per channel and cost per dollar of revenue — require advertising revenue that cannot exist until the programme thresholds are met. No supplied source states a revenue-per-thousand-views parameter, so even a modelled version cannot be produced.

**Why it matters.** §72 of the same brief forbids building "a dashboard full of meaningless metrics". Displaying those six now produces exactly that, and worse: it invites decisions taken against placeholder numbers that look like measurements.

**Cost of the challenge.** None. Deferring six displays costs nothing.

**Alternative.** Instrument in the first wave only what is measurable now and needed now, and add the rest when the first revenue parameter is **observed rather than assumed**:

| Measurable now, and needed now | Deferred until the denominator exists |
|---|---|
| Cost per video against the envelope | Revenue; RPM |
| Token, character, image and generated-second counters | Cost per 1,000 views; cost per 1,000 watch minutes; cost per subscriber |
| **Minutes per CEO approval** | Profit per video; profit per channel |
| Gate verdict counts, and send-back reasons | ROI; cost per dollar of revenue |
| Copyright and policy incident counts | |
| Production throughput: planned / produced / published / delayed | |
| Views, CTR, retention, watch time, subscribers — from first publication | |
| Progress to threshold, **carrying the qualified-view caveat** | |

**Recommendation.** Adopt the split. It satisfies §71's intent, which is that the company measure what drives net profit, without violating §72.

### 27.6 Dynamic budget control

Each department and channel has a configurable budget tracked at **50% / 75% / 90% / 100%** utilization, with a breach raising an alert (`R-039`). Underperformance triggers a COO + CTO review: reduce unnecessary cost, move suitable tasks to cheaper capabilities, reduce low-value experiments.

**Never cut copyright or compliance controls to save money** (§27 of the brief, `R-017`). The four controls named in `RK-001` are marked non-cuttable in configuration, and a cost decision records the controls it did not touch (`AI-017`).

---

## 28. Security

**Least privilege** (`R-058`, `AI-058`). Every role holds only the permissions its responsibilities require.

| Role | Holds | Explicitly does not hold |
|---|---|---|
| Scriptwriter | Draft read/write | Publishing; finance; asset licensing |
| Creative Director | Asset selection; brief write | Rights verification; publishing; finance |
| Copyright Officer | Asset and licence read; **block** | Publish; approve own exception; modify assets |
| Policy Officer | Content read; determination write; **block** | Publish; relax a platform rule |
| Quality Control | Read everything produced; reject | Produce or modify |
| Publisher (code) | Publish **on a complete approval set including the CEO token** | Override any block; publish without the token; modify content |
| Finance / CFO | Financial read/write | Content modification |
| COO / CTO reporting agents | Read across operations and cost | Any write to production artifacts; any publishing permission |
| CEO | Everything, including approval and override | — |

**Credential protection.** Credential sharing is prohibited by **every provider whose terms were examined** [OFFICIAL]. One production account per provider, credentials held in a secret store, never in agent context, never in a prompt, never in a log. The specific protection standard required is an open question routed to the architect (`Q-007` in §40) and must be settled before the router is implemented.

**Audit.** All important actions are audited (§60 of the brief, `R-059`): who, what, when, why, input, output, decision, cost, risk. The canonical example the brief gives is exactly the shape this system produces:

```
CopyrightOfficer  |  Rejected Asset #293
Reason: commercial licence could not be verified
```

**Privacy and retention** expectations for research material, analytics, audit records and audience data are **not established** (`Q-006` in §40). This matters more than it looks: the internet-services material reports that in-scope platforms must authenticate user accounts by Vietnamese mobile number or identity number before an account may post or livestream, which binds publishing accounts to identified natural persons. That material is **law-firm analysis, not the official text** [INDUSTRY PRACTICE], with article numbers not independently confirmed, and it is not treated as established.

---

## 29. Database Architecture

The entity list below is §59 of the brief, with the additions the evidence forces marked. Relationships and indexes are stated where they carry a control.

| Entity | Notes |
|---|---|
| `Company` | |
| `Department` | |
| `Agent` | Registry, §12.2 |
| `AgentCapability` | Capability keys, joined to the routing table |
| `Model` | Registry, §13.5 |
| `ModelPrice` | **Temporal**: `validFrom`, `validTo`, `sourceURL`, `lastVerified`. Never hard-coded (`R-044`) |
| `Channel` | §17.1, plus `PayeeCountry` and `LibraryRegistrationState` |
| `Audience` | |
| `ContentPillar` | |
| `Video` | |
| `VideoIdea` | Carries the full scoring record (`AI-012`) |
| `ResearchDocument` | Carries provenance; joined to every claim |
| `Script` | Versioned across passes |
| `Asset` | |
| `AssetLicense` | §4.3. **Unique index on (`AssetId`)**; the publishing gate's blocking join runs `Publication → Video → Asset → AssetLicense` and fails on any missing row |
| `Voice` | |
| `Music` | |
| `Thumbnail` | Candidates and the shipped one |
| `ProductionJob` | |
| `Workflow` | |
| `QualityCheck` | |
| `CopyrightReview` | |
| `PolicyReview` | |
| `Publication` | Carries the CEO approval token reference |
| `Platform` | |
| `PlatformPolicy` | **Addition.** One row per policy statement, carrying `label` (official / recommendation / practice / inference), `sourceURL`, `lastVerified`, `effectiveFrom`. This is the store the compliance gate pack reads and the re-verification job writes. Without it, `R-024` to `R-029` cannot be satisfied |
| `AnalyticsSnapshot` | |
| `Experiment` | |
| `Revenue` | By channel and by source |
| `Expense` | |
| `Budget` | With threshold state |
| `ChannelProfitLoss` | Derived over a closed period |
| `Risk` | Category, severity, handling applied |
| `Alert` | |
| `AgentRun` | Carries capability requested, provider selected, degraded-mode flag |
| `AgentCost` | §25 |
| `AuditLog` | Append-only |
| `Decision` | Including the CEO decision record; D-001 to D-008 are rows |
| `WeeklyReport` | |
| `Recommendation` | Carries the seven required fields of `R-056` |
| `CEOApproval` | **Addition.** One row per approval or send-back under D-002: video, render hash, verdict, reason, **minutes elapsed**. This is what closes `RK-005`, and it does not exist in the brief's entity list |
| `LibraryRegistration` | **Addition.** Per channel per library, with the date. The precondition of first publication |

**Indexes that carry a control**, not merely performance: the licence join above; `CEOApproval(videoId, renderHash)` unique, so an approval cannot be reused for a different render; `AgentCost(videoId)` and `AgentCost(channelId, period)` for the rollups; `PlatformPolicy(platform, lastVerified)` for the re-verification sweep; a content-hash and perceptual-hash index across the back catalogue for duplicate detection.

---

## 30. System Architecture

### 30.1 The §76 diagram, updated for what was actually decided

Two changes from the brief's version, both forced by evidence: the **account pool is replaced by a capability-to-provider routing table** (§13.3), and the publishing gate carries a **mandatory CEO approval step** (D-002).

```
                              HUMAN CEO / OWNER
                                      |
                +---------------------+---------------------+
                |                                           |
          WEEKLY REVIEW                          PER-PUBLICATION APPROVAL
        (10-15 min brief)                         (mandatory, D-002)
                |                                           |
     +----------+----------+                                |
     |                     |                                |
    COO                   CTO                               |
 OPERATIONS           TECHNOLOGY                            |
     |                     |                                |
     |              MODEL ROUTER                            |
     |            (capability-keyed)                        |
     |                     |                                |
     |     +---------------+---------------+                |
     |     |  CAPABILITY -> PROVIDER TABLE |                |
     |     |  primary / secondary / emerg. |                |
     |     |  one commercial account each  |                |
     |     |  NO consumer-subscription pool|                |
     |     +---------------+---------------+                |
     |                     |                                |
     |             COST CONTROLLER                          |
     |        (spend vs tier cap & envelope)                |
     |                     |                                |
     +----------+----------+                                |
                |                                           |
         AGENT ORCHESTRATOR                                 |
                |                                           |
     +----------+----------+----------+                     |
     v          v          v          v                     |
  STRATEGY  CREATIVE  PRODUCTION   FINANCE                  |
  research   design    script      cost records             |
  audience   thumbnail visual      channel P&L              |
  ideation   motion    narration   budget                   |
     |          |          |          |                     |
     +----------+----+-----+----------+                     |
                     v                                      |
                COMPLIANCE                                  |
        Copyright / Policy / Fact                           |
        (independent; can BLOCK; cannot publish)            |
                     |                                      |
                     v                                      |
              PUBLISHING GATE  <---------------------------+
        content + quality + copyright + policy
                  + CEO APPROVAL TOKEN
              any critical gate fails -> BLOCK
                     |
                     v
                 PUBLISHING
              (idempotent upload)
                     |
                     v
                  ANALYTICS
                     |
                     v
                  REVENUE
                     |
                     v
                  FINANCE
                     |
                     v
               LEARNING LOOP
                     |
                     v
                 COO + CTO
                     |
                     v
                    CEO
```

**Deterministic code layer**, running underneath all of the above and calling no model: render / mux / encode / loudness; forced-alignment captions and chapter arithmetic; the rights ledger and its blocking join; the library registration state machine; the gate state machine and approval-token verification; schedule and buffer arithmetic; cost metering and rollup; router accounting, retry, backoff, circuit-breaking; content and perceptual hashing; metadata templating; audit logging.

### 30.2 Provider abstractions

Per §15 of the brief, so the CTO can replace a provider without redesigning the business system: `IAgent`, `IAgentProvider`, `IModelProvider`, `IImageProvider`, `IVideoProvider`, `ITTSProvider`, `IStorageProvider`, `IPublishingProvider`. Each resolves through the routing table; **no business logic names a provider**.

### 30.3 CHALLENGE — what the brief's architecture asks for that this company should not build yet

Three items, each with the alternative.

| Brief asks for | Problem | Alternative | Cost / benefit |
|---|---|---|---|
| §42.11 CEO dashboard with per-provider capacity bars and quota reset times | There are no consumer seats, and at 13 videos a month a remaining-quota bar would **read full every day and tell the CEO nothing** | Replace with: monthly spend against the tier cap, spend against the CEO's envelope, per-capability health and error rate, and per-video cost | Costs nothing; removes a meaningless display §72 forbids |
| §18 + §22 a benchmarking-driven dynamic model-selection product | There is no benchmark corpus, because no video exists. It would optimise a $34/month variable line against assumptions | A configured routing table now, benchmarked ordering at Wave 7 once ten comparable runs per task exist | Defers effort; loses nothing, because the ordering would be the same |
| §8 + §64 an AI executive layer producing weekly reports | Adds model spend and a second review surface **without removing the CEO from the loop D-002 makes mandatory** | Waves 8 and 9 as planned, after the sustained rate. Until then the weekly reports are generated from recorded data by code, with one L3 pass for the recommendation narrative | Defers effort; the reports the CEO actually needs (§32–§34) are mostly arithmetic |

None of these is a reason not to build them later. Each is a reason not to build them **before there is data for them to operate on**.
---

## 31. CEO Dashboard

Designed against the brief's §62 list and corrected where a panel would be meaningless before programme entry (§27.5). Everything shown is either MEASURED or explicitly labelled ESTIMATE.

### Company
| Panel | Status today |
|---|---|
| Cost | Live from the cost records |
| AI cost | Live |
| Videos published | Live |
| Channels | 1 |
| Revenue / Profit / Cash | **Shown as "pre-threshold — advertising revenue not yet earnable"**, with affiliate shown separately once any arrives. Not shown as $0 in a field that reads like a measurement of performance |

### Channel
Views, subscribers, uploads, CTR, retention, growth, cost, **risk state** — all live from first publication.
Revenue, RPM and profit — deferred until the denominator exists.
**Progress to programme threshold** — subscribers against 1,000, and qualified watch hours against 4,000 (8,000 from 2027-02-01), with days remaining to 2027-02-01 and **the qualified-view caveat carried on the panel itself**.

### Production
Ideas, scripts, in production, in QC, in copyright review, **awaiting CEO approval**, scheduled, failed jobs. Buffer depth in items of cover.

### Alerts
Copyright risk; policy risk; **platform policy change detected** (from the re-verification sweep); budget threshold crossed; production backlog; publishing failure; technology failure; **library registration incomplete**; **a dated change approaching** (2026-09-24, 2027-01-31, 2027-02-01).

### AI capacity — what replaces the §42.11 panel

The brief specifies per-provider capacity bars and quota reset times. There are no consumer seats to draw bars for, and at this volume a remaining-quota bar would read full every day (§30.3). Replaced by:

```
SPEND          $41.18 of $77.41 envelope   [=========.......]  53%   ESTIMATE
               $34.42 metered / $42.99 standing
PER VIDEO      $5.95 all-in                (target: not yet set - Q-008)

CAPABILITY HEALTH
  editorial L3      OK    primary          0 fallbacks this week
  bulk L1/L2        OK    primary          0 fallbacks
  narration         OK    primary          0 fallbacks
  images            OK    primary          0 fallbacks
  cutaways          OK    primary          0 fallbacks
  stock footage     OK    registered
  music + SFX       OK    registered
  search            OK    primary

QUEUED TASKS   0        BLOCKED TASKS  0      DEGRADED-MODE HOLDS  0
PROVIDER FAILURES (7d)  0
DAYS OF BUFFER COVER    -
MINUTES / CEO APPROVAL  not yet measured  (RK-005)
```

`Estimated days until exhaustion` from the brief's list is replaced by **days until the envelope is consumed at the current burn rate**, which is the version of that question that has an answer when capacity is metered rather than rationed.

---

## 32. COO Weekly Report

| Section | Contents |
|---|---|
| **Production** | Planned / completed / published / delayed. Buffer depth and days of cover |
| **Quality** | Rejection rate; revision rate; copyright rejection rate; policy rejection rate; **CEO send-back rate and the reasons** |
| **Operations** | Queue length per stage; bottleneck; failures and their resolutions; agent utilization; degraded-mode routes taken and items held |
| **Channels** | Production capacity against the committed rate; publishing consistency; operational issues |
| **Finance** | Production cost for the week; budget utilization against threshold |
| **Recommendations** | What should the company change operationally next week |

Everything above the Recommendations row is computed by code from recorded data. **No model is called to count what was published** (§20 of the brief).

---

## 33. CTO Weekly Report

| Section | Contents |
|---|---|
| **Technology health** | Uptime; failures; bottlenecks; technical debt; provider failures and fallback frequency; degraded-mode incidents |
| **AI economics** | Total spend; spend by model, by agent, by channel; cost per video; cost per unit of revenue **when a revenue denominator exists** |
| **Capacity** | Utilization per capability; rate-limit events (expected: none at this volume); queue time; tasks delayed by capacity |
| **Cost optimization** | For each opportunity: current approach, current cost, alternative, expected saving, quality impact, implementation effort, risk |
| **Automation opportunities** | Which manual or model-priced operation should become code |
| **Technology opportunities** | New models, tools, architectural improvements — **including a re-price of every routing row against the model registry's `lastVerified`** |
| **Reliability** | Fallback frequency; quality degradation; provider failures |
| **Business strategy** | New channels, formats, production approaches, revenue opportunities, technology-driven advantages |
| **Recommendations** | Each in the §42.12 format below |

Every recommendation carries: current approach, problem, alternative, monthly cost difference, capacity difference, quality impact, implementation cost, operational risk, recommendation, **CEO decision required** (`R-056`).

Permitted recommendation types: add / remove account; change plan; change provider; move task to a different interface; move task to a local model; change model; change routing; combine agents; remove agent; cache result; **automate with code**.

**The weekly re-verification obligation lives here.** Until 2027-02-01 the CTO report carries the platform-policy re-verification status: which statements were re-checked, which changed, and which are overdue. A policy change raises an alert and triggers re-verification (`R-029`), and the change flows into `PlatformPolicy` and therefore into the compliance gate pack in one act.

---

## 34. CEO Executive Brief

Combining COO, CTO, Finance and Analytics. Target: the CEO understands the company's position in **10–15 minutes**.

```
====================
COMPANY
====================
Revenue            pre-threshold (advertising not yet earnable); affiliate: $--
Cost               $--        ESTIMATE basis, MEASURED actuals where metered
Profit             negative by the full monthly cost - expected, pre-threshold
AI spend           $--  of $77.41 envelope
Videos             -- published this week / -- this month
Channels           1

====================
CHANNEL PERFORMANCE
====================
Channel      Views   Subs   CTR   Retention   Cost   Progress to threshold
CH-001       --      --     --    --          $--    -- subs / 1,000
                                                     -- of 4,000 watch hrs
                                                     -- days to 2027-02-01

====================
PRODUCTION
====================
Planned  --    Produced  --    Published  --    Delayed  --
Buffer   -- items (-- days of cover)
Awaiting CEO approval  --      Avg minutes / approval  --

====================
RISK
====================
Copyright     -- incidents, -- open claims, -- strikes
Policy        -- incidents, -- send-backs on policy grounds
Monetization  -- (pre-threshold)
Technology    -- provider failures, -- degraded-mode holds
Dated         next: [2026-09-24 guidelines] [2027-01-31 terms] [2027-02-01 thresholds]

====================
CTO RECOMMENDATIONS
====================
1.  2.  3.

====================
COO RECOMMENDATIONS
====================
1.  2.  3.

====================
CEO DECISIONS REQUIRED
====================
1.  2.  3.
```

**Every measure in this brief connects to a decision the CEO takes** (`R-064`, `AI-064`). A measure that informs no decision is removed, which is the discipline that keeps the review inside fifteen minutes.

---

## 35. Development Waves

**Plan of record: all nine waves, per the CEO override on `RK-014`.** The recorded contrary analysis is stated once in §27.4 and is not re-argued here. Wave 0 is complete; this document is its deliverable.

### WAVE 0 — RESEARCH & ARCHITECTURE — **COMPLETE**

Delivered: platform policy research and the policy matrix (§3); business model (§2); organization (§6–§10); agent architecture (§11–§12); model routing architecture (§13–§14); cost architecture (§24–§26); copyright architecture (§4, §19); data model (§29); MVP plan (§36). No significant implementation.

### WAVE 1 — COMPANY FOUNDATION

Company, Channel, Agent, Model Registry, Agent Registry, Job Queue, Workflow, Audit Log, Cost Tracking, Configuration.

**Sequencing constraint carried into this wave from the Recommendation Gate:** the first-wave items that discharge a blocking condition run first — the **rights and licence ledger with library registration as a coded precondition of first publication**, and the **capability router with primary / secondary / emergency rows and spend metering**, which is what discharges `RK-004`, the one blocker that blocks building. The generalised registries and workflow abstractions are built in this wave as the plan of record requires; the ledger and the router are built **first within it**.

### WAVE 2 — ONE CHANNEL

The complete pipeline: Idea → Research → Script → Design → Production → QC → Copyright → Policy → publish-ready.
**Goal: ONE COMPLETE VIDEO END-TO-END.**

This wave also delivers the three things no arithmetic in this plan can produce: the **real** token, character, image and generated-second ledger; the **real** minutes per CEO approval; and whether the compliance determinations can be evidenced at all. The annual licensing commitment is not signed until this wave's output has been metered.

### WAVE 3 — PUBLISHING

Platform integration; scheduling; publishing; metadata; thumbnails; captions. Plus, specific to this company: the **publishing gate state machine carrying the mandatory D-002 approval step**, its approval-minutes instrumentation, and idempotent upload.

### WAVE 4 — ANALYTICS

Video analytics, channel analytics, revenue, cost, profit, learning. **The revenue-derived displays are built but shipped dark** until a revenue parameter is observed (§27.5), so the wave completes without producing the dashboard §72 forbids.

### WAVE 5 — 3 VIDEOS / WEEK

Prove the sustained rate for one channel. **The decision to sustain follows the approval-minutes baseline rather than preceding it** (`RK-005`).

### WAVE 6 — MULTI-CHANNEL

Channels 1, 2, 3 using shared agents. **Re-examination of the CEO approval workload precedes any second channel**, as the decision record directs. Channel isolation is a corporate-structure question (§17.5) and must be settled before, not during, this wave.

### WAVE 7 — AI ECONOMICS

Model router refinement, cost controller, benchmarks, budget controls, dynamic model selection, cost optimization. The benchmark corpus (§16) becomes populated here, which is what makes dynamic selection an evidence-based rather than a configured ordering.

### WAVE 8 — AI MANAGEMENT

COO, CTO, CFO agents; weekly reporting; recommendations; CEO dashboard.

### WAVE 9 — AUTONOMOUS COMPANY

Autonomous planning, autonomous scheduling, channel lifecycle, experimentation, budget allocation, continuous learning. **The human CEO remains the ultimate authority**, and D-002 relaxes only by an explicit recorded CEO decision, independently of how autonomous the rest of the company becomes.

### Cross-wave sequencing constraints, which bind regardless of wave

| Constraint | Reason |
|---|---|
| The payee position precedes channel creation | One payment account per payee name; duplicates disapproved |
| Library registration precedes first publication | Revenue lost before registration is unrecoverable |
| Price re-fetch precedes any purchase | No price here was verified at first hand |
| The metered first video precedes both the sustained rate and the annual licensing commitment | So the commitment is made against measurement |
| Terms acceptance precedes **2027-01-31**; entry attempted before **2027-02-01** if reachable | The only fixed date |
| Nothing routes to the interface listed for shutdown on **2026-09-24**, at any point | No replacement exists |
| Approval-workload re-examination precedes any second channel | Linear growth in the binding constraint |

---

## 36. MVP

The MVP is **not** "we have many agents". It is:

```
One Channel
+ One Complete Video
+ Original Content
+ Copyright Verification
+ Policy Verification
+ Publish-ready Output
+ Cost Tracking
```

Then prove **3 videos / week** before scaling.

**Definition of done for the MVP**, stated as what must be demonstrable rather than as a feature list:

| Must be demonstrable | Acceptance intent |
|---|---|
| The video's every asset resolves to a licence record with a commercial-use basis, and a deliberately unlicensed asset blocks publication | `AI-016`, `AI-019` |
| The script carries a recorded similarity check against its source material, and a back-catalogue duplicate check | `AI-013`, `AI-014` |
| Every factual claim resolves to an identified source; an unverifiable claim is flagged | `AI-009` |
| The five compliance determinations exist with their evidence, including the sensitive-topic screen | §20.1 |
| Quality verification was performed by a party other than the one that did the work | `AI-007` |
| The publishing gate blocked at least one deliberate failure case, and released only on a complete approval set including the CEO token | `AI-023` |
| Every AI operation produced a cost record resolving to video, channel, department, role and capability | `AI-002`, `AI-041` |
| The real token, character, image and generated-second ledger was metered and compared against §24.1 | `RK-011` |
| Minutes per CEO approval were recorded | `RK-005` |
| The channel was registered on every library **before** the publication | `RK-008` |

**What the MVP deliberately does not include:** multi-channel, audience analytics beyond what the platform returns, benchmark-driven routing, an AI executive layer, autonomy. Those are Waves 4–9 of the plan of record.

---

## 37. Scaling Strategy

Expansion is a **configuration and compliance question**, not a redesign (`R-048`). Additional channels reuse the same agents, the same routing table, the same ledger schema and the same gates, with per-channel configuration supplying audience, brand, strategy, language, tone, visual identity and schedule.

### What actually binds at each step

| Scale | AI capacity | Licensing | CEO approval under D-002 | Compliance surface | The real constraint |
|---|---|---|---|---|---|
| **1 channel** (committed) | 4.9M in / 429K out tokens per month; **no published rate limit binds** | $42.99/mo, 1-channel registration | ~14.4 approval events/month | 1 platform regime | **The CEO.** Nothing else is close |
| **3 channels** | ~14.7M in / 1.29M out per month; still well inside entry-tier limits | Licences are tiered by channel count — a 3-channel tier replaces the 1-channel one; the realistic floor rises toward the ~$70–120/month band the licensing evidence records for five channels | ~43 events/month | 1 regime if all on one platform | **The CEO, unless D-002 has been relaxed on a recorded clean-record threshold.** This is the step that forces that decision |
| **5 channels** | ~24.5M in / 2.1M out; approaching the point where the tier spend cap, not the rate limit, matters | ~$70–120/month ESTIMATE at the 5-channel tiers | ~72 events/month — **not sustainable by one person** | 1 regime; but **"related channels" contagion is undefined and unresolvable from published sources** | Approval, and **the undefined related-channel risk**, which is a corporate-structure question requiring distinct legal payees |
| **10 channels** | Spend cap and batch-window scheduling become real | 10-channel licence tiers exist; beyond them, per-channel registration cost grows | Impossible without relaxation | Still 1 regime if single-platform | **One ad-payment account per payee name.** Genuine channel isolation requires distinct legal payees, decided before launch |
| **20+ channels** | Multi-account commercial provisioning under commercial terms, which is permitted where consumer multi-accounting is not | Enterprise licence terms; one library requires a higher business tier for larger companies | Requires a mature, recorded relaxation of D-002 with an auditable substitute control | Multi-platform by then, so 2–4 regimes, **each evaluated separately because the originality cures are not portable** | Corporate structure and the compliance surface, not technology |

### Three things that do not scale by adding capacity

1. **CEO approval.** It grows linearly with channels and is already the binding constraint at one. The clean-record threshold that would relax D-002 is the single most valuable open question in the plan.
2. **Licence registration.** Every channel must be registered on every library before its first publication, and revenue lost before registration is unrecoverable. At N channels this is N × L registrations, each a coded precondition.
3. **The undefined "related channels" term.** The platform never defines it, and whether monetization suspension propagates across channels sharing one ad-payment account **is not established**. It is the unestablished item with the largest consequence for a multi-channel operation, and no amount of engineering resolves it — only a first-party statement, or distinct legal payees.

### Platform expansion

Deferred by D-008, not cancelled. Reopening it (`RK-013`, `Q-010`) takes the compliance surface from one regime to four and adds three platforms **none of which has a confirmed monetizable route for a Vietnam-resident payee**. It is an entity-structure decision. Expansion reuses the same masters and the same shared agents — but not the same renders: **a cross-platform pipeline needs platform-native renders with independently licensed audio, not one master re-uploaded four times**, because cross-posting is penalised three separate ways [INFERENCE, from the crossposting, reused-content and platform-scoped-music-licence rules].

---

## 38. Failure Scenarios

### 38.1 Channel-level — fatal or near-fatal

These end the company's only revenue route. All four are [OFFICIAL POLICY] on the launch platform.

| Scenario | Mechanism | Consequence |
|---|---|---|
| **Third copyright strike in 90 days** | Strikes accumulate from legal removal requests, not from Content ID claims. Disputing a claim can convert it into a strike if the claimant escalates to takedown | **The account and any associated channels are subject to termination**, and creating new channels is barred |
| **Templated mass-production caught by name** | The inauthentic-content policy requires content to be the creator's original creation and not mass-produced, generic, repetitive or manipulative, and expressly names AI-generated content made with generic or unoriginal templates giving the impression of mass production. The spam policy separately prohibits using automated tools to churn out high volumes of similar content with minimal changes | **Penalties reach the channel**: programme suspension with a 21-day appeal and a 90-day wait before re-application; spam-policy escalation reaches channel or account termination |
| **Synthetic persona on a barred topic** | Channels using synthetic personas to deliver information on **health, legal issues, finances or politics** cannot monetize. The rule is stated at channel level, with a synthetic doctor, financial adviser and legal adviser given as examples | **Channel-level monetization bar.** Not a video-level penalty; not curable by removing the video |
| **Consistent non-disclosure of synthetic content** | Disclosure is required for realistic altered or synthetically generated content | Content removal or programme suspension |

**The compounding factor that makes all of this worse for this company specifically.** An automated pipeline produces **correlated** failures, not independent ones: a template defect affects every output, not one. The realistic failure mode is therefore crossing a channel-level threshold **within days**, not gradually. **Publishing velocity is itself a risk variable.** The pipeline needs a circuit breaker keyed to rolling violation counts, not merely a pre-publish content check. [INFERENCE, stated as such, drawn from the quoted rules.]

The company has **one channel, on one platform, with no second platform offering a confirmed monetizable route for its payee jurisdiction**. There is no diversification to absorb a channel-level outcome.

### 38.2 Video-level — recoverable, and a cost of doing business

| Scenario | Consequence | Recovery |
|---|---|---|
| Limited ads (yellow icon) on a self-rating disagreement | Reduced or removed ad revenue on that item | **One appeal, typically ≤7 days, then the decision is final.** Repeated inaccuracy in self-certification can trigger a programme eligibility review, so the appeal is not free |
| An individual Content ID claim | Revenue on that item goes to the claimant | **Never auto-dispute.** Replace the asset or accept the loss; escalate to the CEO with the rights record |
| An individual Shorts view ruled ineligible | Progress toward the threshold overstated | None available; carry the caveat on every threshold display |

Video-level failures can be managed statistically. Channel-level failures cannot, and they are almost always reached by accumulation of video-level failures.

### 38.3 Operational and supply failures

| Scenario | Effect | Handling |
|---|---|---|
| Provider outage, quota exhaustion, rate limit, model unavailable, account unavailable | A capability is unavailable | Router fails over to secondary, then emergency. **If no route meets the quality floor, the task queues; if business-critical, it escalates** (`R-054`). At L3+ a degraded-mode completion **holds the item from publication** |
| Provider quality degradation | Output below floor, undetected | Caught by the independent QC and fact-check layers; recorded as a quality incident against that capability in the registry |
| Provider price increase | Envelope breach | Router refuses the route without escalation; CTO re-prices and re-runs the arithmetic (`RK-010`) |
| Interface shutdown | Capability lost | Routing keyed by capability makes this configuration, not rework. **Nothing routes to the interface shutting down on 2026-09-24** |
| Publication before library registration | **Revenue on that item forfeited permanently, with no reimbursement** | Prevented by a coded precondition, not by a checklist |
| Failed publishing step | Item not published, or double-published | Idempotent upload; failure recorded and retried or escalated, **never silently dropped** (`R-030`) |
| CEO unavailable | **Nothing publishes.** D-002 makes the CEO a hard dependency in the publishing path | Accepted consequence of D-002. The buffer absorbs short absences; a long absence stops publication, and that is the trade D-002 makes deliberately |
| Annual licence term ends or is cancelled | Right to keep already-published work online is **unverified on one library and self-contradictory on another** | Assume the weaker reading: complete and publish every end product during the active term. Record per asset which clause its right relies on |

### 38.4 Evidence and jurisdiction failures

| Scenario | Effect |
|---|---|
| A quoted unit price was wrong at the moment of commitment | Four first-party pricing pages contradict themselves; re-fetch before any spend (`RK-002`) |
| A platform policy changed and the change was missed | The compliance gate pack is enforcing a stale rule. Monthly re-verification until 2027-02-01 exists precisely for this |
| The 2027-02-01 threshold passes unmet | The watch-hour requirement doubles and the payback of every option lengthens by a period no supplied source quantifies |
| Vietnamese tax position turns out unfavourable or the withholding treatment differs | Lands the moment revenue arrives; the payment account and its country are hard to change afterwards (`RK-003`) |

---

## 39. Recovery Strategy

### 39.1 Circuit breakers — automatic, keyed to rolling counts

Because channel-level thresholds are reached by accumulation and an automated pipeline fails in a correlated way, the controls below trip **before** a threshold, not after.

| Trigger | Action |
|---|---|
| Any copyright strike | **Halt publishing immediately.** Escalate to the CEO. Publishing does not resume without an explicit CEO decision and a recorded root-cause finding |
| Any content-matching claim | Publish continues; the claim is escalated to the CEO with the rights record. **Never auto-disputed** |
| Two policy rejections in a rolling 30 days from the same cause | Pause the affected content pillar; re-run the compliance pack against the back catalogue for the same pattern |
| Back-catalogue duplicate detection fires above threshold | Block the item and raise a **template-drift alert**, because this is the leading indicator of the mass-production finding |
| Any degraded-mode completion at L3 or above | Item held from publication until re-run at floor |
| Envelope utilization crosses 90% | Alert; the router refuses new non-essential routes without escalation |
| CEO send-back rate rises above its baseline | COO + CTO review of where the pipeline's judgement is failing |

### 39.2 Recovery from each fatal scenario

| Scenario | Recovery path | Honest assessment |
|---|---|---|
| Programme suspension | 21-day appeal window; 90-day wait before re-application | The company has no revenue during either. Prevention is the strategy; recovery is a formality |
| Third copyright strike / account termination | **No recovery path exists.** Creating new channels is barred | This is why never auto-disputing a claim is a standing rule rather than a guideline |
| Channel-level monetization bar for a synthetic persona on a barred topic | Avoided entirely by the sensitive-topic screen in the compliance pack, which blocks rather than flags | Not a recovery scenario; a prevention scenario |
| Limited ads on a video | One appeal, ≤7 days, decision final | Track the self-rating accuracy rate; repeated inaccuracy risks a programme eligibility review, so appealing every yellow icon is itself a risk |

### 39.3 Operational recovery

- **Failed step**: detected, recorded, retried with backoff, escalated if retries exhaust. Never silently dropped (`R-030`).
- **Provider failure**: automatic failover along the routing table; the fallback is recorded; the CTO report carries fallback frequency.
- **Buffer depletion**: the COO's earliest warning. Days of cover is on the dashboard precisely so that recovery starts before a publication is missed.
- **Emergency operations on the non-operating day**: only infrastructure recovery, security incident, failed publishing recovery and critical platform notification. Any activity recorded on that day must fall in one of those four named categories (`AI-033`).

### 39.4 Reversal of the commitment itself

Covered in §24.7. The metered line reverses within a billing cycle; the standing line's annual term is the bounded cost of being wrong, and it is **not signed until the first video is metered**, which is what keeps that cost bounded.

---

## 40. Open Questions

Every open item, its owner, and the point at which it must be answered. **The blocking distinction is preserved and is not cosmetic.**

### 40.1 The blocking distinction

| Class | Items | Meaning |
|---|---|---|
| **BLOCKS BUILDING** | `RK-004` | The router cannot be implemented against a foreclosed design. The technical design must carry the capability-to-provider routing table and drop the consumer-subscription account pool. **This is the only item that blocks building**, and it is answerable in the phase immediately following Wave 0 |
| **BLOCKS BANKING** | `RK-003`, `RK-006` | Neither blocks a line of code. `RK-003` — the Vietnamese tax and withholding position, unverified to first-party standard — lands the moment revenue arrives, and the payment account and its country are hard to change afterwards. `RK-006` — the 2027-02-01 threshold — **carries the only fixed date in the plan.** The architecture, the pipeline and the first complete video can all be built while both stand open |
| **BLOCKS FIRST PUBLISH** | `RK-001`, `RK-008` | The four non-cuttable controls must stand in the pipeline, and the channel must be registered on every library, before anything goes out |
| **BLOCKS FIRST PURCHASE** | `RK-002` | Every unit price is re-fetched before any spend is committed |
| **BLOCKS SUSTAINING THE RATE** | `RK-005` | Does not block the first video; blocks the decision to sustain three a week, and blocks any second channel |

### 40.2 Open questions

| ID | Question | Owner | Must be answered by |
|---|---|---|---|
| `Q-A` | **What revenue per thousand views should the financial model assume for the launch platform?** No supplied source states one, and the policy evidence does not even record it as unverified. Break-even therefore stays a formula | product owner | Before any payback claim is made; before the O-001/O-002 ranking is revisited |
| `Q-B` | **How many minutes does one publication approval actually cost the CEO, and what clean-record threshold would relax D-002?** | product owner, for proposal to the CEO | Instrumented from the first approval; baselined over the first several; **before the decision to sustain 3/week, and before any second channel** |
| `Q-C` | **Do the committed three videos a week consist of long-form only, or does the rate include short-form?** Determines the threshold route and changes per-item cost by roughly an order of magnitude | product owner | Before Wave 2 begins, because the ledger and the pipeline differ |
| `Q-D` | **Can a role reach first-party sources to re-verify prices and policy on the monthly cadence the evidence recommends, and on whose schedule?** | orchestrator | **Before 2026-09-24**, then monthly until 2027-02-01, and before any spend |
| `Q-E` | **Who is accountable for the jurisdictional determinations** — payee tax and withholding, advertising-law disclosure for affiliate content, and any local rule on synthetic-media disclosure? | CEO | Before the payment account is created. **Blocks banking** |
| `Q-F` | **What protection is required for the production-account credentials**, given that credential sharing is prohibited by every provider whose terms were examined? | architect | Before the router is implemented |
| `Q-G` | **What quality floor and cost-per-video target apply**, so a later re-run of the arithmetic has a threshold to be measured against rather than only a number to compare? | product owner | Before Wave 2 completes, so the metered first video can be judged |
| `Q-H` | **Does the deferred four-platform, two-language scope of D-003 stay deferred until the single channel is proved at the sustained rate, and what evidence would reopen it?** This is the trade-off the decision record required to be put back to the CEO | CEO | Before any expansion work is scheduled. Affects `RK-013` |
| `Q-I` | **Which measure tells the business that the original-content outcome `O-003` is being achieved?** No measure was supplied | product owner | Scope definition, before Wave 2 |
| `Q-J` | **Which measure tells the business that the continuous-improvement outcome `O-010` is being achieved?** No measure was supplied | product owner | Scope definition, before Wave 4 |
| `Q-K` | **What target values apply to profit, audience growth, quality floors, cost per video and capacity?** None are supplied and no operating baseline exists to derive them from | product owner → CEO | Before the channel reaches `ACTIVE`; before any lifecycle transition rule can be written |
| `Q-L` | **Who is accountable for the legal determinations escalated out of the workforce**, given that escalation is required on rights uncertainty but no legal authority other than the CEO is named? | CEO | Before the first rights escalation, which can occur in Wave 2 |
| `Q-M` | **What retention and privacy expectations apply to research material, analytics, audit records and audience data?** Relevant because the reported Vietnamese account-verification regime binds publishing accounts to identified natural persons — **and that reporting is law-firm analysis, not the official text** | product owner | Before Wave 4 (analytics), and before any audience data is retained |
| `Q-N` | **Does the weekly non-operating day apply to publication as well as to production?** Publishing targets are set by weekday | product owner | Before the publishing schedule is fixed in Wave 3. Conservative reading applied meanwhile |
| `Q-O` | **Will human contributors work alongside the AI workforce, and under whose approval?** The brief lists external contributor costs while describing the workforce as entirely AI | CEO | Before Wave 1 budgets are set |
| `Q-P` | **What volume and turnaround expectations apply beyond the stated per-channel rate, and to how many channels must the company scale?** Channel counts appear in the brief only as illustration | product owner | Before Wave 6 |
| `Q-Q` | **Which of the brief's two disagreeing clauses — the §42.2 account-pool illustration or the §42.10 prohibition on quota multiplication — is the requirement the architecture must satisfy?** The design question is settled by the framing requirement that capacity never be obtained by prohibited means; **the textual contradiction inside the brief is not settled and is recorded as standing** (Contradiction 1) | business analyst / CEO | The design proceeds now; the textual reconciliation is for the record |
| `Q-R` | **Should the frozen context slice be amended to carry the three research files and the business intent as members**, so the provenance of observations drawn from them is fixed by digest as the other members are? | orchestrator | Housekeeping; does not block |

### 40.3 Unestablished facts that are not questions anyone can currently answer

These are recorded so no downstream reader mistakes them for known. Each names what would close it.

| Unestablished | Closed by |
|---|---|
| Whether monetization suspension propagates across channels sharing one ad-payment account; the platform never defines "related channels" | A first-party statement or a platform response. **The single unestablished item with the largest consequence for a multi-channel operation** |
| Whether the qualified-Shorts-view metric uses the same exclusion list as creator-pool eligible views | A first-party statement. Until then any Shorts-led threshold plan may overstate its progress |
| The consequence of not accepting the updated programme terms by 2027-01-31 | A first-party statement in the page body |
| The launch platform's affiliate-link position | A first-party statement, or counsel. Conservative reading applied meanwhile (`RK-007`) |
| Whether Vietnam's quoted tax rates and revenue threshold survive its 2026 tax laws; the withholding treatment of foreign-sourced viewership earnings; Vietnamese advertising-law disclosure obligations; any Vietnamese rule on synthetic-media disclosure | **Qualified Vietnamese counsel**, plus the first-party payment documentation |
| The output-ownership and indemnity clauses of several media vendors; the rate limits of six providers; twelve categories of price with no first-party page | Re-verification by a role able to reach first-party sources |

---

## 41. Recommended Next Steps

Ordered. Each names its owner and what it discharges.

### Immediately, before any spend

1. **Adopt the capability-to-provider routing table and drop the consumer-subscription account pool.** Owner: architect. Discharges `RK-004` — **the one open blocker that blocks building**. Output: the technical design for the router, with primary / secondary / emergency per capability, one production account per provider under commercial terms, and the forbidden-sources list recorded so the design cannot drift back.
2. **Re-fetch every unit price and re-verify every platform statement.** Owner: orchestrator, executed by a role able to reach first-party sources. Discharges `RK-002`. **First pass before 2026-09-24**, because a guidelines change lands that day, then monthly until 2027-02-01. Re-run the §24.3 arithmetic against whatever comes back; the $77.41 figure is an ESTIMATE until this is done.
3. **Settle the credential-protection standard.** Owner: architect. Answers `Q-F`. Must precede the router implementation.

### Before the first channel exists

4. **Obtain qualified Vietnamese counsel on the payee tax and withholding position.** Owner: CEO. Discharges `RK-003` — **blocks banking, not building.** It must precede creating the payment account, which under the one-account-per-payee rule precedes creating the channel. No Wave 1 or Wave 2 work waits on it.
5. **Name the authority accountable for legal determinations escalated out of the workforce.** Owner: CEO. Answers `Q-E` and `Q-L`. Until it is named, rights escalations terminate at the CEO with a recommendation to obtain counsel.
6. **Decide the threshold route and accept the updated programme terms.** Owner: CEO. Bounds `RK-006` — **blocks banking, and carries the only fixed date.** Terms before **2027-01-31**; entry attempted before **2027-02-01** if the route is reachable at the committed rate.

### Wave 1, in this order within the wave

7. **Build the rights and licence ledger with library registration as a coded precondition of first publication.** Owner: engineering. Discharges `RK-008` on day one of publishing.
8. **Build the capability router with spend metering.** Owner: engineering. Implements step 1.
9. **Build the remaining Wave 1 foundation** — Company, Channel, Agent, Model Registry, Agent Registry, Job Queue, Workflow, Audit Log, Cost Tracking, Configuration — per the plan of record.

### Wave 2, and what it must produce beyond a video

10. **Produce and meter one complete video end to end.** Owner: COO + engineering. It must produce three things this plan's arithmetic cannot: the **real** token, character, image and generated-second ledger against §24.1, closing `RK-011`; the **real** minutes per CEO approval, closing `RK-005`; and a demonstration that the five compliance determinations can be evidenced at all.
11. **Instrument the CEO approval surface to record minutes per approval from the very first approval.** Owner: engineering. This is the measurement that later supports a clean-record threshold proposal; retrofitting it loses the baseline.
12. **Sign the annual licensing commitment only after step 10.** Owner: CEO. This is what keeps the reversal cost bounded (§24.7).

### Decisions to put to the CEO, with the evidence ready

13. **The clean-record threshold that would relax D-002.** Owner: product owner, for CEO decision. Proposed from a baseline over several approvals, not assumed. **This is the single most valuable open question in the plan**, because the CEO is the binding capacity constraint at one channel and the constraint grows linearly.
14. **Long-form only, or long-form plus short-form?** Owner: product owner (`Q-C`). Determines the threshold route and the per-item unit cost.
15. **Does the deferred four-platform, two-language scope stay deferred?** Owner: CEO (`Q-H`, `RK-013`). The trade-off the decision record required to be put back: reopening takes the compliance surface from one regime to four, scales the approval workload linearly, and adds three platforms none of which has a confirmed monetizable route for the payee jurisdiction — with no additional revenue route.
16. **Target values and the quality floor.** Owner: product owner → CEO (`Q-K`, `Q-G`). Without them, the metered first video can be compared but not judged, and no channel lifecycle transition rule can be written.

### Standing obligations from the first day of operation

- **Never auto-dispute a content-matching claim.** Escalate with the rights record.
- **Never cut the four `RK-001` controls to save money**, at any point, under any budget pressure.
- **Re-verify platform policy monthly until 2027-02-01**, and feed every change into `PlatformPolicy`, which feeds the compliance gate pack in one act.
- **Hold the committed rate.** Publishing velocity is itself a risk variable, and raising it raises correlated-failure exposure faster than it raises revenue.
- **Label every cost, capacity and revenue figure as an ESTIMATE** until it is replaced by a measurement, and carry the ±30% tokenizer uncertainty wherever the token ledger is used.

---

## Appendix — Source artifacts

| Artifact | Contents relied on |
|---|---|
| `tasks/MC-1/input.md` | The CEO brief. §3 policy matrix requirement; §5 rights record fields; §14, §16, §17 registry fields; §42 capacity strategy; §59 entity list; §75 the 41 sections; §76 the architecture diagram |
| `.../problem-framing/artifacts/requirement-framing.md` | FRAME-2026-0001. 10 outcomes `O-001`–`O-010`; 67 requirements `R-001`–`R-067`; 67 acceptance intents `AI-001`–`AI-067`; 9 boundaries `B-001`–`B-009`; 7 assumptions `AS-001`–`AS-007`; 15 open questions |
| `.../technical-discovery/artifacts/investigation-report.md` | INV-2026-0001. 68 observations `E-001`–`E-068`; 7 contradictions; 7 stale assumptions; 14 gaps; 10 open questions |
| `.../option-analysis/artifacts/technical-recommendation.md` | TRC-2026-0001. The bottom-up capacity computation; the per-video token / character / image ledger; options `O-001`–`O-004` costed; the capability-to-provider routing table; risks `RK-001`–`RK-012` |
| `.../recommendation/artifacts/technical-recommendation.md` | TRC-2026-0002. The committed recommendation of O-002; 11 criteria `EC-001`–`EC-011`; 15 risks `RK-001`–`RK-015`; readiness conditions; sequencing |
| `research/ceo-decision-record.md` | D-001 to D-008. **Authoritative; governs where it conflicts with anything upstream** |
| `research/platform-policy-dossier.md` | Platform policy for four platforms with source labelling; §5 the platform policy matrix; §6.4 channel-level vs video-level; §7 contradictions and gaps |
| `research/ai-capacity-dossier.md` | Provider pricing; terms-of-service findings; the per-video cost ledger and its sensitivities; §I what could not be verified |

**Verification date of all policy and price statements: 2026-09-18.** None was verified at first hand in this run. Re-verification before any spend and monthly until 2027-02-01 is condition `RK-002`.
