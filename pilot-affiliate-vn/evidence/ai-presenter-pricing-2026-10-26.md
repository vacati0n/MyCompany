# AI-presenter (talking avatar) tools: pricing and terms evidence

- Purpose: evidence for the Vietnamese Shopee Video affiliate pilot (research ticket MC-15)
- Access date for every URL below: 2026-10-26
- Method: public pages only, read with WebFetch/WebSearch and the built-in browser for JS-heavy pricing pages. No sign-in, no trial, no form, no download. Cookie banners were not accepted.
- Need being tested: about 12 videos of 30-60 s in 9:16, so 6-12 output minutes a month. One consistent synthetic presenter, not a real or celebrity likeness, speaking natural Vietnamese with lip-sync, used on one account. Metered ceiling USD 34.42/month.
- Labels: **UNVERIFIED** means no first-party quote was found. "Derived" means simple arithmetic on quoted first-party figures.
- FX: no first-party exchange rate was used. VND to USD conversions use about 26,300 VND/USD and are **UNVERIFIED**.

---

## 1. HeyGen

Sources:
- P1 https://www.heygen.com/pricing (served as /vi-vn/pricing)
- P2 https://help.heygen.com/en/articles/15126059-how-to-use-credits-on-heygen
- P3 https://www.heygen.com/terms
- P4 https://help.heygen.com/en/articles/11391932-voice-languages-we-support
- P5 https://developers.heygen.com/docs/avatar-from-prompt
- P6 https://www.heygen.com/moderation-policy
- P7 https://www.heygen.com/blog/how-to-use-public-avatars-in-heygen
- P8 https://www.heygen.com/text-to-speech/vietnamese

| Item | Finding | Quote and source |
|---|---|---|
| Cheapest fitting plan | Creator: USD 29 monthly, USD 24/month billed annually. No VND price; the vi-VN page shows USD. | P1 FAQ: "the Creator plan costs $29/month (or $24/month if you pay annually)" |
| Credits | 600 credits/month. Monthly subscribers roll unused credits over one month. | P1: "600 tín dụng"; "Monthly subscribers' unused credits roll over for one more month." |
| Credit burn | Avatar III video look 4/min, Avatar IV photo look 16/min, Avatar IV video look 31/min, Avatar V 48/min. Derived: 12 min of Avatar IV video look = 372 credits, and 12 min of Avatar V = 576 credits. Both fit within 600. | P2: "Avatar IV Video Look: 31 credits per minute"; "Avatar V Video Look: 48 credits per minute" |
| Overage | Creator has no credit packs. Upgrade to Pro, which is USD 49 for 1,000 credits. Business can buy packs; no pack price is shown. | P1: "Creator and Pro users can upgrade to a higher plan or tier for more credits. Business users can purchase one-time credit packs" |
| Vietnamese | Vietnamese is in the platform voice list. The Creator plan has 175 languages. Lip-sync of avatars to Vietnamese TTS is not specifically documented. **UNVERIFIED**: quality must be tested. | P4: "Vietnamese." P1: "Hơn 175 ngôn ngữ và phương ngữ". P8 notes Northern and Southern accent choice: "Northern delivery is the broadcast standard used in national media" |
| Watermark | Removed on Creator. Free plan is watermarked. | P1: "Xóa thương hiệu HeyGen"; Free: "3 video mỗi tháng, có hình mờ" |
| Commercial use | Paid plans own the output. Free-plan output is non-commercial. | P3: "you own all rights in your User Input or User Output"; Free: "may not be sold, sublicensed, redistributed, monetized" |
| Unique avatar | Prompt-to-avatar makes a fully synthetic character. Model training costs 60 credits; each Look costs 1 credit. Creator includes 1+ custom video avatar. Stock exclusivity is not addressed, and outputs may resemble other users' outputs. | P5: "The character is fully synthetic, so no real person is depicted and no consent step applies." P2: "One model training costs 60 credits." P3: "User Output may not be unique" |
| AI provenance | **UNVERIFIED**. A search summary claimed an invisible watermark and provenance metadata, but the fetched EU AI Act help page does not say so. Synthesia's comparison page lists HeyGen AI transparency as "Not built in" (a competitor claim). The terms put disclosure on the user. | P3: "you must proactively disclose that such User Output was created using artificial intelligence technologies" (to the extent law requires) |
| Advertising, testimonial terms | No ban on product promotion. Stock avatars are cleared for commercial use. Misleading use and health misinformation are banned. Real people need consent. | P7: "public avatars are cleared for commercial use". P3: "Use or distribute User Output in a misleading way" (prohibited). P6: avatars may not represent "real individuals, including celebrities or public figures, without their explicit consent" |
| Free tier | 3 videos/month, 1 minute max, watermarked, non-commercial | P1 |
| 9:16 | Not quoted from a first-party page; **UNVERIFIED** | n/a |

## 2. D-ID (Studio)

Sources:
- D1 https://www.d-id.com/pricing/studio/ (browser; Monthly toggle read through the page DOM)
- D2 https://help.d-id.com/hc/en-us/articles/31350883819537-What-are-the-copyright-and-commercial-use-rights-for-my-videos
- D3 https://help.d-id.com/hc/en-us/articles/31350550554641-How-do-I-change-or-remove-the-watermark-on-my-videos
- D4 https://www.d-id.com/faqs/ (search result)
- D5 https://www.d-id.com/resources/generate-better-ai-presenters-from-text/ (search result)

| Item | Finding | Quote and source |
|---|---|---|
| Cheapest fitting plan | Pro: USD 29 monthly, or USD 16/month billed annually (USD 191 per year). Lite (USD 5.9 monthly, USD 4.7 annual) is excluded because it is personal use only. | D1: "$16/month $191 billed annually (45% discount)"; monthly view: "$29/month" |
| Minutes | 15 min/month on Pro. No rollover. Billed in 15-second steps. Derived: a 40 s video bills as 45 s. | D1: "15 min. / month for Videos, Agents, Video Translate & API". D4: "rounded up to the nearest 15-second interval"; "unused minutes become void" |
| Overage | No per-minute overage price is shown. Next tier is Advanced: USD 196 monthly, USD 108/month annual, 100 min. **UNVERIFIED**: no top-up price was found. | D1 |
| Vietnamese | Native Vietnamese TTS is not confirmed on a first-party page (**UNVERIFIED**). Video Translate's target list omits Vietnamese. Studio accepts uploaded audio (MP3/WAV, up to 5 min), so Vietnamese audio from Vbee or FPT.AI can drive the lip-sync. | Search result from D-ID help: "upload an audio file for your avatar to speak"; "Maximum length: 5 minutes" |
| Watermark | Pro keeps a visible generic "AI" mark. Only Advanced and Enterprise can replace it. | D3: "Pro – Generic AI watermark appears in the bottom left corner" |
| Commercial use | Pro has commercial use, including ads. | D2: "Pro, Advanced, Enterprise, Launch, Scale – For commercial use ... You're free to use your videos in ads, marketing" |
| Unique avatar | 3 Personal Avatars on Pro. A "Generate from text" presenter is available (guide is about 8 months old). Personal avatars need recorded consent. | D1: "3 Personal Avatars". D5 (search): "Click Generate from text" |
| AI provenance | The visible AI watermark is itself the provenance mark. C2PA: **UNVERIFIED**. | D3 |
| Advertising terms | Ads are allowed on commercial plans (D2). No testimonial-specific rule was found. | D2 |
| Free tier | 14-day trial, 3 minutes, full-screen watermark, personal-use licence | D1: "Full-screen watermark"; "Personal use license" |

## 3. Synthesia

Sources:
- S1 https://www.synthesia.io/pricing (WebFetch and browser render disagree; see the price row)
- S2 https://www.synthesia.io/legal/acceptable-use-policy
- S3 https://help.synthesia.io/en/articles/11517389-why-can-t-i-use-stock-avatars-for-my-content
- S4 https://docs.synthesia.io/docs/personal-avatars (search result)
- S5 https://www.synthesia.io/alternatives/synthesia-vs-heygen

| Item | Finding | Quote and source |
|---|---|---|
| Cheapest fitting plan | Starter. The browser render showed "$29 $18 per month, $216 billed yearly". WebFetch of the same URL returned USD 19/month (first month USD 9) and USD 14/month annual (USD 168). The page may vary by region or test. **Price UNVERIFIED** between the two. | S1 |
| Minutes | 10 free min/month plus 1,250 credits, about 22 min/month in total. 100 credits per minute. | S1 FAQ: "roughly 12 more minutes of typical video, about ~22 minutes a month in total" |
| Vietnamese | Vietnamese is listed for personal avatars (docs). It is not named on the languages page. | S4 (search): list "... Uzbek, Vietnamese, Zulu" |
| Watermark | Logo removed on Starter | S1: "Remove Synthesia logo"; Basic: "Videos carry a Synthesia watermark" |
| Commercial use | Not stated on the pricing page | n/a |
| Unique avatar | Personal avatars are included with paid plans (3 / 5 / unlimited) but depict a consenting real individual. Studio avatars cost USD 1,000/year. | S1: "Personal avatars are included with paid plans"; "Paid add-on ($1,000/year)" |
| AI provenance | C2PA and an AI label on every video (vendor claim) | S5: "Every Synthesia video can automatically carry a C2PA content credential and an AI label." |
| **Advertising and endorsement (blocking)** | Stock avatars may not endorse products, and may not appear in paid, boosted or promoted ads without written consent. Endorsement by stock avatars is allowed only on Enterprise. | S3: stock avatars can't "Endorse products or services". S2: prohibits "a Stock Avatar in content for "promoted", "boosted", or "paid" advertising" absent consent; endorsement allowed only "on enterprise Service plans" |
| Free tier | Basic: 10 min/month, watermarked | S1 |

## 4. Vidnoz

Source: V1 https://www.vidnoz.com/pricing.html (browser)

| Item | Finding | Quote and source |
|---|---|---|
| Cheapest fitting plan | Starter: USD 26.99 monthly, USD 19.99/month billed yearly | V1: "$26.99 $19.99/mo"; "$19.99/mo Billed yearly" |
| Credits | 450/month, no rollover. Standard video costs 0.5 credit/s; Expressive Avatar costs 2 credits/s. Derived: 15 min standard, or 3.75 min expressive. | V1 FAQ: "Vidnoz AI Video Generation 0.5 credits per 1s"; "Expressive Avatar 2 credits per 1s"; "the unused credits do not carry over" |
| Overage | Add-on credit packs exist; price not captured. **UNVERIFIED** | V1 FAQ |
| Vietnamese | Not named on any first-party page found. **UNVERIFIED** | n/a |
| Watermark | None on Starter | V1: "No watermark"; Free row: "With Vidnoz watermark" |
| Commercial use | "Full Commercial License" row exists, but the per-plan ticks did not render. **UNVERIFIED** per plan. | V1 |
| Unique avatar | Avatar Lite: 3 on Starter. Photo avatars: 100/day. Avatar Pro digital twin: USD 299/year add-on. | V1: "Avatar Pro ... $299/yr ... Billed yearly" |
| AI provenance | **UNVERIFIED** | n/a |
| Refund | No refund on monthly subscriptions | V1: "Refund requests for a 1-month subscription will not be accepted." |
| Free tier | 720p, 3 min per video, watermark; credits/day showed 0 | V1 |

## 5. Hedra

Sources:
- H1 https://www.hedra.com/pricing (browser; the Studio tab showed only API pricing)
- H2 https://www.hedra.com/models/video/hedra/character-3
- H3 https://www.hedra.com/uses/ai-talking-avatar
- H4 https://www.hedra.com/api-terms
- hedra.com/plans redirects to sign-in, so it was not read.

| Item | Finding | Quote and source |
|---|---|---|
| Pricing | Pay-as-you-go from a prepaid balance, billed per second. Character-3 costs 2.5 cents/s at 540p, 5 cents/s at 720p and 6.25 cents/s at 1080p. Derived for 12 min: USD 18, USD 36 and USD 45. For 6 min: USD 9, USD 18 and USD 22.5. Studio subscription tiers (Basic USD 15, Creator USD 30) are **UNVERIFIED** (third-party only). | H1: "No subscription, no seats, no minimum."; "Avatar & lip-sync from 2.5¢ / second". H2: 540p 2.5¢, 720p 5¢, 1080p 6.25¢ |
| 9:16 | Supported | H2 lists "9:16" |
| Vietnamese | Audio-driven, any language. Use Vbee or FPT.AI audio. | H3: "in any language, for as long as the audio runs" |
| Watermark / commercial | Paid use is unwatermarked with commercial rights (marketing page). The API terms are more restrictive (see the next row). | H3: "commercial rights and unwatermarked output come with every one of them" |
| **API terms (risk)** | API access is limited to personal or internal business use (page about 2 years old) | H4: "for Customer's personal or internal business use only" |
| Unique avatar | Can generate the character first, then animate it | H3: "generate the character first and use that" |
| AI provenance | **UNVERIFIED** | n/a |

## 6. Captions / Mirage

Sources:
- C1 https://www.captions.ai/pricing
- C2 https://captions.ai/help/docs/project/ai-creator

- Max plan: USD 24.99/month (iOS pricing), 500 AI credits. C1: "Create digital twins or custom AI actors".
- **Blocking:** Vietnamese is not in the AI Creator language list. The list ends "Swedish, Tamil, Turkish, and Ukrainian." (C2). Lipdub also omits Vietnamese.
- Commercial use: C2: "all videos made using AI Creator can be used for commercial use."
- Watermark and provenance: not stated (**UNVERIFIED**).

## 7. Akool

Source: A1 https://akool.com/pricing (browser)

- Starter: USD 12/month billed yearly (USD 144), 200 credits. Pro: USD 30 monthly or USD 21 yearly, 600 credits. Both say "No watermark".
- **Blocking:** the licence on Starter, Pro and Pro Max is personal. A1: "License: personal". A business licence starts at Business: USD 249 monthly, USD 174.30/month yearly.
- Vietnamese and credit cost per minute: **UNVERIFIED**. A1 says "over 150 languages"; Vietnamese is not named.

## 8. Vietnamese providers

### Vbee AIVoice (TTS only; no avatar video product found)

Source: B1 https://vbee.vn/pricing (browser)

| Plan | Price | Allowance | Quote |
|---|---|---|---|
| Miễn phí (free) | 0 | 3,000 points/day (about 4.2 min) | "3.000 điểm mỗi ngày"; "~3.000 ký tự/ngày (~4,2 phút)" |
| Tiêu chuẩn | 169,000 VND/month; 459,000 VND/year (about 39,000/month) | 125K points/month (about 2.9 h of basic voice) | "đ169.000/tháng"; "đ459.000/năm"; "~125K ký tự/tháng (~2,9 giờ)" |
| Đặc biệt | 239,000 VND/month; 599,000 VND/year | 250K points; advanced voices; professional voice clone | "Giọng AI nâng cao" |

- "Không chèn audio quảng cáo" (no inserted ad audio) is listed as a paid-tier feature. This implies free-tier output may carry inserted ad audio (inference).
- Commercial use: a Vbee blog states voices can be used commercially (search result). The terms page was not read, so this is **UNVERIFIED** from the terms.
- USD equivalent of Tiêu chuẩn: about USD 6.4/month, or about USD 1.5/month on annual billing (**UNVERIFIED** FX).

### FPT.AI TTS

Source: F1 https://docs.fpt.ai/docs/en/speech/documentation/tts-pricing/ (© 2025)

- Free tier: 100,000 characters/month, slow and queued. That is enough for 12 min of script (about 10-12K characters, an estimate).
- Premium: 500,000 VND per pack of 1,500,000 characters.
- No commercial-use terms are on the page (**UNVERIFIED**). No avatar video product was found.

### Viettel AI and Zalo AI

No public self-serve avatar-video or presenter pricing was found. Viettel needs a sales quote. Zalo TTS is an API with 4 voices and no price shown. **UNVERIFIED** / not usable for a self-serve pilot.

## 9. Pipeline option (Vietnamese TTS plus a lip-sync tool)

| Pipeline | Monthly cost for 12 min (derived) | Notes |
|---|---|---|
| Vbee Tiêu chuẩn annual + D-ID Pro annual | about USD 1.5 + USD 16 = about USD 17.5/month; cash up front about USD 17.5 + USD 191 | Vietnamese is guaranteed by uploaded audio. Visible "AI" corner mark. Generated-from-text presenter. |
| FPT.AI free + D-ID Pro monthly | USD 29 | FPT free-tier commercial terms are unknown |
| Vbee or FPT + Hedra Character-3 540p (API) | about USD 18 for 12 min (USD 9 for 6 min) + TTS | Pure metered spend. 540p is low resolution; 720p costs USD 36 for 12 min, which is over the ceiling. API terms say "personal or internal business use only". |

---

## Comparison table

| Tool | Plan | USD/month (monthly / annual) | Minutes/month | Vietnamese | Watermark | Commercial use | Unique avatar |
|---|---|---|---|---|---|---|---|
| HeyGen | Creator | 29 / 24 | 600 credits: about 19 min Avatar IV video look, 12.5 min Avatar V, 37 min Avatar IV photo | Yes, voice listed; avatar lip-sync quality unverified | No | Yes (own output) | Yes: prompt-generated synthetic avatar (training 60 credits) |
| D-ID | Pro | 29 / 16 | 15 (15-s rounding) | Native TTS unverified; yes via uploaded audio | Visible generic "AI" mark | Yes, ads allowed | Yes: "Generate from text"; 3 personal avatars |
| Synthesia | Starter | 29 or 19 / 18 or 14 (conflicting renders) | about 22 | Yes (personal avatars) | No | Not stated | Personal avatar = a real consenting person only; **stock avatars may not endorse products** |
| Vidnoz | Starter | 26.99 / 19.99 | 15 standard / 3.75 expressive | Unverified | No | Row present, per-plan unverified | Avatar Lite x3 / photo avatar; Avatar Pro USD 299/yr |
| Hedra | API pay-as-you-go | about 18 (540p) / 36 (720p) for 12 min | metered | Yes via audio (any language) | Not on paid (marketing) | Marketing page yes; API terms "personal or internal business use" | Yes: generate character |
| Captions | Max | 24.99 / n/a | 500 credits (rate unknown) | **No** (AI Creator list) | Unverified | Yes (AI Creator) | AI twin / custom actor |
| Akool | Starter / Pro | n/a / 12; 30 / 21 | 200-600 credits | Unverified | No | **Personal licence only** below Business | Instant avatars x2-3 |
| Vbee | Tiêu chuẩn | about 6.4 / about 1.5 (169k / 459k VND per year) | TTS only, about 2.9 h | Yes (native) | n/a | Blog says yes; terms unverified | n/a (voice only) |
| FPT.AI | Free / Premium pack | 0 / 500k VND per pack | TTS 100k chars/month free | Yes (native) | n/a | Unverified | n/a |

## Top 2 picks within USD 34.42

1. **HeyGen Creator: USD 24/month annual or USD 29 monthly.** It is the only all-in-one tool where Vietnamese voice, no watermark, owned commercial output and a fully synthetic prompt-generated presenter are all documented first-party. 600 credits cover 12 min even on Avatar V (576 credits). The prompt-generated avatar removes the "same stock avatar across accounts" risk and the real-likeness consent issue. Risks: Vietnamese lip-sync quality is untested. Avatar provenance or C2PA is unconfirmed, so the Shopee "Video tạo bởi AI" label must be applied manually. Credit-burn rates are recent and may change.
2. **D-ID Pro (USD 16/month annual, or USD 29 monthly) with Vbee Tiêu chuẩn TTS (about USD 1.5/month annual).** This is the cheapest documented path, at about USD 17.5/month. Vietnamese quality comes from a native Vietnamese TTS (Vbee, with Northern and Southern voices), uploaded as audio, so it does not depend on the avatar vendor's Vietnamese voice. Commercial use, including ads, is explicit. A presenter generated from text gives a unique face. Trade-offs: a visible "AI" mark sits bottom-left on Pro, which aligns with AI disclosure but is not removable. 15 min/month with 15-s rounding fits 12 videos of 30-60 s. Annual billing needs about USD 191 up front.

The metered-only alternative is Hedra Character-3 at 540p with Vbee or FPT audio, at about USD 9-18/month. It is not a top pick because of the API-terms wording "personal or internal business use only" and the 540p resolution.

## Possible blockers

- **Vietnamese lip-sync quality is unverified for every avatar tool.** No vendor documents avatar lip-sync quality for Vietnamese TTS, and tonal accuracy matters. A free-tier test is needed before committing. That test needs sign-in, which was out of scope here.
- **Stock-avatar restrictions:** Synthesia bans stock avatars from endorsing products. Its only alternative is a personal avatar of a real consenting person, which breaks the "not a real likeness" rule, so Synthesia is effectively excluded. All vendors' stock avatars are shared across customers, so a generated unique avatar is required anyway.
- **Excluded on terms or language:** Captions (no Vietnamese in AI Creator) and Akool (personal licence below USD 174-249/month).
- **Free tiers are not usable for publishing:** HeyGen Free is non-commercial; D-ID Trial is personal use with a full-screen watermark; Vidnoz Free is watermarked; Vbee Free likely has inserted ad audio.
- **Provenance:** only Synthesia claims C2PA, and only D-ID has a visible AI mark on the target plan. With HeyGen, the AI label must be added on Shopee manually. HeyGen's terms also require AI disclosure where law requires it.
- **Billing practicalities (UNVERIFIED):** USD card payment by a Vietnam-resident individual, foreign-transaction fees, and any VAT on foreign digital services may raise the effective price. Annual plans need cash up front (D-ID USD 191, HeyGen USD 288 by derivation).
- **Budget classification:** HeyGen and D-ID are flat subscriptions, not metered spend. Whether they count against the USD 34.42 metered ceiling or the fixed lines of the USD 77.41 total is a budget decision. Only Hedra's API is truly metered.
- **Not quoted first-party:** HeyGen 9:16 support, D-ID native Vietnamese TTS, Vidnoz Vietnamese and per-plan commercial licence, and Hedra Studio tiers.
