# Platform Policy Dossier — AI-Operated Multi-Channel Video Media Company

**Compiled:** 2026-09-18
**Last verified (all rows unless stated otherwise):** 2026-09-18
**Platforms covered:** YouTube (long-form + Shorts), TikTok, Facebook, Instagram
**Purpose:** Evidence base for an architecture decision. Accuracy and provenance prioritised over completeness of prose.

## How to read the labels

Every substantive statement in this document carries one of four tags:

| Tag | Meaning |
|---|---|
| **[OFFICIAL POLICY]** | Stated on a first-party platform page. URL given. |
| **[DOCUMENTED RECOMMENDATION]** | First-party guidance or best-practice, not an enforceable rule. |
| **[INDUSTRY PRACTICE]** | Widely reported, third-party sourced. Named source given. Never the sole basis for a policy claim. |
| **[INFERENCE]** | The compiler's reasoning from the above. **Not policy.** |

Where a fact could not be established from a first-party source, this document says so explicitly rather than guessing. See **Contradictions and gaps**.

---

# 1. YOUTUBE

## 1.1 Monetization eligibility

### Current thresholds (in force on 2026-09-18)

**[OFFICIAL POLICY]** There are two tiers of the YouTube Partner Program (YPP).

**Tier 1 — "Expanded YPP" / fan funding + shopping (500 subscribers):**
Eligibility is 500 subscribers, **3 valid public uploads in the last 90 days**, and **either** 3,000 qualified watch hours in the last 12 months **or** 3 million qualified Shorts views in the last 90 days.
Unlocks: channel memberships, Super Chat & Super Stickers, Super Thanks, Jewels/gifts, and YouTube Shopping. It does **not** unlock ad revenue share.
Source: https://support.google.com/youtube/answer/13429240 (verified 2026-09-18)

**Tier 2 — Full YPP / ad + Premium revenue share (1,000 subscribers):**
1,000 subscribers **and either** 4,000 qualified public watch hours in the last 12 months **or** 10 million qualified Shorts views in the last 90 days.
Source: https://support.google.com/youtube/answer/72851 (verified 2026-09-18)

**[OFFICIAL POLICY]** Additional gating conditions for either tier:
- Live in a country/region where YPP is available.
- Have an active AdSense for YouTube account linked to the channel.
- 2-Step Verification enabled on the Google Account.
- **No active Community Guidelines strikes on the channel.**
- Follow the YouTube channel monetization policies.
Source: https://support.google.com/youtube/answer/72851 (verified 2026-09-18)

**[OFFICIAL POLICY]** "Qualified watch hours" counts watch time "gained from long-form videos you've set public." Private videos, unlisted content, Shorts, and unlisted livestreams are excluded.
Source: https://support.google.com/youtube/answer/72851 (verified 2026-09-18)

### Country availability

**[OFFICIAL POLICY]** YouTube publishes an alphabetical list of areas where YPP sign-up is available; the page does not state a total count. Russia is explicitly excluded: "Given the recent suspension of Google advertising systems in Russia, we'll be pausing the creation of new Russian accounts on AdSense, AdMob and Google Ad Manager … creators in Russia won't be able to complete new YPP sign-ups at this time."
Source: https://support.google.com/youtube/answer/7101720 (verified 2026-09-18)

**[INFERENCE]** For a multi-entity media company, the operating entity's country of tax/payment residence — not the audience's country — determines YPP availability and AdSense payability. This should be settled before channel creation because AdSense country is difficult to change later. *This is reasoning, not a quoted policy.*

### The 1 February 2027 changes — VERIFIED

The brief asked for this to be confirmed against both `blog.youtube` and `support.google.com`. Both were fetched. **The reported summary is substantially correct but materially incomplete, and one widely-repeated framing ("existing partners are grandfathered") is misleading.**

| Claim in the brief | Verdict | First-party wording |
|---|---|---|
| Effective 1 Feb 2027 | **Confirmed** | "Starting February 1, 2027, we are introducing updates to the YouTube Partner Program" — support.google.com/youtube/answer/12843009 |
| 8,000 qualified watch hours / 20M Shorts views for new entrants | **Confirmed, with an addition** | "Starting Feb. 1, 2027, YPP entry thresholds for new creators are changing to 8,000 qualified watch hours in the last 365 days, or 20M qualified Shorts views in the last 90 days, **in addition to still needing 1k subscribers**." — support.google.com/youtube/answer/12843009 |
| Existing partners grandfathered | **Confirmed but narrower than reported** | "If you are already in YPP, your status is not impacted by this update." This sentence is about **YPP membership status / entry thresholds only**. It does **not** exempt existing partners from the new Shorts revenue-share floor or the new activity requirement (both below). |
| Fan-funding tier at 500 subs unchanged | **Confirmed** | The support page restates the unchanged tier as "500 subscribers and either 3,000 qualified watch hours in the last year or 3M qualified Shorts views in the last 90 days." The blog states thresholds for Fan Funding and shopping products "remain unchanged." — blog.youtube + support.google.com/youtube/answer/12843009 |
| 10M Shorts views floor for Shorts revenue sharing | **Confirmed — and it applies to everyone, not only new entrants** | "To earn each month from the Shorts Creator Pool, creators will now need to maintain 10M qualified Shorts views over the last 90 days." And: "If you miss this threshold, you will not be removed from YPP and this will not impact other YPP earnings, including long-form video." — support.google.com/youtube/answer/12843009. The blog phrases it as: "creators who have 10 million qualified Shorts views over the last 90 days will be eligible for ads and subscription revenue sharing on Shorts. Channels below this threshold remain in YPP and continue earning on long-form content, with Shorts revenue sharing automatically resuming once they cross 10 million views again." |

**[OFFICIAL POLICY] — Not in the brief, and important.** A new **channel activity requirement** also takes effect 1 Feb 2027 and applies to existing partners: "Most creators in YPP already meet YPP activity thresholds, but those that drop below will now have an extended 90-day window to meet either of these requirements to stay in the program: 1,000 qualified watch hours in the past 365 days, or 1 million qualified Shorts views in the last 90 days." A channel is considered active if it meets 1,000 qualified watch hours in the past 365 days, **or** 1 million qualified Shorts views in the last 90 days, **or** uploads 2 long-form videos or 5 Shorts every 90 days.
Source: https://support.google.com/youtube/answer/12843009 (verified 2026-09-18)

**[OFFICIAL POLICY]** Terms deadline: creators must "accept the updated terms in YouTube Studio by January 31, 2027." Per the blog, the new terms "take effect on February 1, 2027." Search-snippet text (not confirmed by the compiler in the page body) states that failure to accept means earnings stop from 1 Feb 2027 — see **Contradictions and gaps**.
Sources: https://support.google.com/youtube/answer/12843009 ; https://blog.youtube/news-and-events/youtube-partner-program-updates-2027-new-opportunities-earn/ (verified 2026-09-18)

**[OFFICIAL POLICY]** Other changes announced in the same blog post (published **10 Aug 2026**), effective 1 Feb 2027:
- Revenue split: long-form 55% of the distributed pool; Shorts 45% of the distributed pool.
- YouTube Premium: 30% of net subscription revenue to the creator pool; **Premium Lite: 60%** of net subscription revenue.
- Premium Lite expands to all countries where Premium is available.
- Targeted Shorts ads placed to five or fewer channels carry "a direct 45% revenue share from those placements."
- New incentive programs (shopping bonuses, production credits, trend activations) and a Fan Funding terms migration to a Commerce Product Module.
Sources: https://blog.youtube/news-and-events/youtube-partner-program-updates-2027-new-opportunities-earn/ ; https://support.google.com/youtube/answer/12843009 (verified 2026-09-18)

**[INFERENCE]** The practical effect for a new AI-operated Shorts channel launched after 1 Feb 2027 is a **two-gate structure**: 20M qualified Shorts views/90d to enter YPP at all, then a *separate, continuously re-tested* 10M qualified Shorts views/90d to keep earning from the Shorts pool. A channel can therefore be "in YPP" and earning nothing from Shorts. Launching and reaching 1,000 subs + 10M Shorts views **before** 1 Feb 2027 secures entry under the old threshold — but does **not** exempt the channel from the 10M ongoing Shorts floor. *This is reasoning from the two quoted rules, not a policy statement.*

## 1.2 Copyright

### Two distinct systems

**[OFFICIAL POLICY]** **Content ID** is an automated matching system available "to copyright owners who meet specific criteria"; they "must own exclusive rights to a substantial body of original material that is frequently uploaded to YouTube." On a match the rights holder may: block the video from being viewed; monetize it by running ads against it "and sometimes sharing revenue with the uploader"; or track its viewership statistics. Actions can be geography-specific.
Source: https://support.google.com/youtube/answer/2797370 (verified 2026-09-18)

**[OFFICIAL POLICY]** **Copyright strikes** are different: a strike is issued when "your content was removed due to a legal copyright removal request." "Copyright claims are different from copyright strikes. If you get a Content ID claim on your video, it typically doesn't result in a copyright strike."
Source: https://support.google.com/youtube/answer/2814000 (verified 2026-09-18)

### Strike consequences

**[OFFICIAL POLICY]**
- Strikes 1 and 2: content removed; after completing Copyright School the strike expires in **90 days**.
- Strike 3: "Your account, along with any associated channels, is subject to termination" and "you can't create new YouTube channels."
- Live stream removed for copyright: channel gets a copyright strike and "your live streaming access will be restricted for 7 days."
- Copyright School consists of "4 questions about how copyright works on YouTube."
Source: https://support.google.com/youtube/answer/2814000 (verified 2026-09-18)

**[OFFICIAL POLICY]** Three ways to resolve a strike: "Complete Copyright School and wait 90 days"; "Get a retraction" from the claimant; "Submit a valid counter notification" where the removal was erroneous or qualifies as fair use.
Source: https://support.google.com/youtube/answer/2814000 (verified 2026-09-18)

### Content ID dispute path and its escalation risk

**[OFFICIAL POLICY]** Valid grounds to dispute: you have "all the necessary rights to the content in your video"; the use qualifies as a copyright exception such as fair use; or "your video was misidentified or an error was made." Invalid grounds include giving credit, owning a purchased copy, or choosing not to monetize.
Source: https://support.google.com/youtube/answer/2797454 (verified 2026-09-18)

**[OFFICIAL POLICY]** After a dispute the claimant "has 30 days to respond" and may release the claim, reinstate it, submit a copyright removal request (which removes the video **and issues a copyright strike**), or do nothing — in which case "the claim on your video will expire." If a reinstated claim is appealed, the claimant "has 7 days to respond"; to keep the video down after an appeal they must submit a copyright removal request. "If you were previously monetizing the video, your monetization settings will be restored automatically when all claims on your video are released."
Source: https://support.google.com/youtube/answer/2797454 (verified 2026-09-18)

**[OFFICIAL POLICY]** "Repeated or malicious abuse of the dispute process can result in penalties against your video or channel."
Source: https://support.google.com/youtube/answer/2797454 (verified 2026-09-18)

**[INFERENCE]** The critical asymmetry for an automated pipeline: a Content ID claim costs only revenue on one video, but **escalating that claim via dispute → appeal converts a revenue-loss event into a possible channel-terminating strike**. An AI-operated system must therefore treat "auto-dispute all claims" as a prohibited default behaviour. *Reasoning from the quoted escalation path.*

### Commercial-use rights — relevant to stock / licensed footage pipelines

**[OFFICIAL POLICY]** "Ensure that you have all the necessary rights to commercially use all visual and audio elements in your content." "You cannot monetize third-party content that you've purchased unless its rights owner grants you commercial use rights." "You must have explicit written permission granting you commercial use rights at any time by the rights holder." "The use of any commercial sound recording, such as an instrumental, karaoke recording, or live concert performance by the artist is not eligible for monetization." Some rights owners require crediting the creator or proof of purchase.
Source: https://support.google.com/youtube/answer/2490020 (verified 2026-09-18)

**[INFERENCE]** A pipeline that sources B-roll from stock libraries must retain per-asset licence evidence, because the burden is on the uploader to produce written commercial-use permission on demand. A "scrape and narrate" pipeline fails this test twice over — on copyright and on reused content. *Reasoning from the quoted requirement.*

### Repeat-claim consequences on monetization

**[OFFICIAL POLICY]** Channel monetization is conditioned on following the copyright policies; "Violation of our YouTube channel monetization policies may result in monetization being suspended or permanently disabled." An active Community Guidelines strike also blocks YPP eligibility.
Sources: https://support.google.com/youtube/answer/1311392 ; https://support.google.com/youtube/answer/72851 (verified 2026-09-18)

**Could not verify:** YouTube does not publish a numeric threshold of Content ID claims that triggers channel-level demonetization. The compiler found no first-party statement of such a number and does not assert one.

### Community Guidelines strikes (distinct again from copyright strikes)

**[OFFICIAL POLICY]** "The first violation is typically only a warning," expiring after 90 days with optional policy training — but "if your content violates the same policy within that 90 day window, the warning may not expire and your channel may be given a strike." Strike 1: no uploads, live streams, scheduled premieres, custom thumbnails, posts or playlists for **one week**; "After the 1-week period, we restore full privileges automatically, but the strike remains on your channel for 90 days." Strike 2: "you will not be allowed to post content for 2 weeks." "Each strike will not expire until 90 days from the time it was issued." Strike 3: "3 strikes in the same 90-day period may result in your channel being permanently removed from YouTube."
Source: https://support.google.com/youtube/answer/2802032 (verified 2026-09-18)

**[OFFICIAL POLICY]** The YPP eligibility page separately requires "no active Community Guidelines strikes on your channel."
Source: https://support.google.com/youtube/answer/72851 (verified 2026-09-18)

**[INFERENCE]** Three independent 3-strike systems run in parallel (copyright strikes, Community Guidelines strikes, and the separate monetization-policy suspension track). A single automated publishing bug that pushes non-compliant content across many videos in a short window can trip the 90-day counters on several at once. Publishing velocity is therefore itself a risk variable. *Reasoning from the three quoted regimes.*

### YPP removal, appeal and re-application

**[OFFICIAL POLICY]** On suspension from YPP, "you can appeal this decision within 21 days or re-apply to the program 90 days after suspension." Where YouTube gives advance notice that "your channel is scheduled for suspension," the creator has "7 days to submit your video appeal," the suspension is paused during review, and a response is expected "within 14 days." "Severe violations of our YouTube channel monetization policies may result in monetization being permanently disabled on any of your accounts." Reinstatement may follow 3 months of compliance with no new violations.
Source: https://support.google.com/youtube/answer/1727191 (verified 2026-09-18)

**[OFFICIAL POLICY]** "You should not create new (or use existing) channels to get around these restrictions, or apply to YPP with related channels during your suspension period."
Source: https://support.google.com/youtube/answer/1311392 (verified 2026-09-18)

**[INFERENCE]** This is the single most consequential sentence in the dossier for a **multi-channel** operator. A network of channels sharing an operator, AdSense account, or infrastructure is plausibly "related." A monetization suspension on one channel may therefore propagate. *Reasoning — YouTube does not define "related channels" on this page, so the blast radius is not established. See Contradictions and gaps.*

### Multi-channel account structure — directly relevant to a channel network

**[OFFICIAL POLICY]** "You can also monetize more than one YouTube channel using the same AdSense for YouTube account." But: "You can only have one AdSense or AdSense for YouTube account under the same payee name per AdSense Terms and Conditions or AdSense for YouTube Terms of Service, as applicable." "Duplicate accounts will not be approved and monetization will be turned off for the associated YouTube channel." "If you're found to have a duplicate account, your newly created AdSense for YouTube account will be disapproved."
Source: https://support.google.com/youtube/answer/9914702 (verified 2026-09-18)

**[INFERENCE]** A multi-channel operator therefore cannot legitimately isolate channels by creating one AdSense account per channel under the same payee — that is precisely the duplicate-account case. Isolation requires genuinely distinct legal payees, which is a corporate-structure decision, not a technical one. **[INDUSTRY PRACTICE]** Creator-forum reports (e.g. the YouTube Community thread "Suspended due to related channel — YPP blocked across all channels linked to AdSense," April 2026, support.google.com/youtube/thread/426926399) describe suspensions propagating across all channels sharing an AdSense account. This is user-reported, not a YouTube statement, and the compiler treats it as unconfirmed signal only.

## 1.3 AI-generated / synthetic content

### Is AI content allowed?

**[OFFICIAL POLICY]** Yes — AI use is not itself disqualifying. YouTube's monetization rule is about originality, not about the tool: content must "Be your original creation" and "Not be mass-produced, generic, repetitive, or manipulative."
Source: https://support.google.com/youtube/answer/1311392 (verified 2026-09-18)

### Mandatory disclosure — "Altered or synthetic content"

**[OFFICIAL POLICY]** Disclosure is required when content is **realistic** and meaningfully altered or synthetically generated. "Creators must disclose GenAI content that: Makes a real person appear to say or do something they didn't do."
Source: https://support.google.com/youtube/answer/14328491 (verified 2026-09-18)

**[OFFICIAL POLICY]** Examples given that **must** be disclosed:
- AI generated music.
- "AI generated extra footage of a real place, like a video of a surfer in Maui."
- AI generated realistic videos of real professional athletes.
- "Making it appear as if someone gave advice that they did not actually give."
- Realistic depiction of a weather event that did not occur.
- Making workers appear to turn away patients.
- "Depicting a public figure stealing something they did not steal" / admitting to crimes they did not commit.
- Making a real person appear arrested or imprisoned.
Source: https://support.google.com/youtube/answer/14328491 (verified 2026-09-18)

**[OFFICIAL POLICY]** Examples given that do **not** require disclosure:
- Clearly unrealistic content — "Someone riding a unicorn through a fantastical world"; green screen depicting floating in space; AI-generated animation inside a fully animated video.
- Minor/production edits — beauty filters; colour adjustment or lighting filters; special effects such as blur or vintage filters; video upscaling; audio enhancement; caption creation.
- Productivity uses — AI assistance generating scripts, thumbnails, titles or ideas.
- **"Cloning one's own voice."**
- Gameplay footage.
Source: https://support.google.com/youtube/answer/14328491 (verified 2026-09-18)

### Where the disclosure appears

**[OFFICIAL POLICY]** The creator sets "Altered or synthetic content" in the YouTube Studio upload workflow (the "Attributes" / AI-use field). Viewers see it in "The 'How this content was made' section in the expanded video description"; for more sensitive/realistic material the label can appear "on the video player itself."
Sources: https://support.google.com/youtube/answer/14328491 ; https://support.google.com/youtube/answer/15447836 (verified 2026-09-18)

**[OFFICIAL POLICY]** The topic categories that trigger the more prominent on-player label are named first-party in the announcement blog post (published 18 Mar 2024): "for videos that touch on more sensitive topics — like health, news, elections, or finance — we'll also show a more prominent label on the video itself."
Source: https://blog.youtube/news-and-events/disclosing-ai-generated-content/ (verified 2026-09-18)

*Note: the current Help Centre pages fetched do not restate this four-category list; it is carried by the 2024 blog post. Treat the list as first-party but not re-confirmed in the live Help Centre. See Contradictions and gaps.*

### Automatic detection and labelling

**[OFFICIAL POLICY]** YouTube may apply the label itself. "When content is undisclosed, in some cases, YouTube may take action to reduce the risk of harm to viewers by proactively applying a label that creators will not have the option to remove." YouTube also auto-labels content made with its own generative tools (e.g. DreamScreen) and will "carry forward disclosures from tools and creators available with secure Content Credentials (C2PA) 2.1 or higher that indicate the entire video was made with AI."
Source: https://support.google.com/youtube/answer/15447836 (verified 2026-09-18)

**[INFERENCE]** Because C2PA 2.1+ provenance metadata is carried forward automatically, the **choice of generation tool determines whether disclosure is optional in practice**. A pipeline using a C2PA-signing generator will be labelled regardless of what the operator ticks. *Reasoning from the quoted carry-forward rule.*

### Penalty for not disclosing

**[OFFICIAL POLICY]** "Creators who consistently choose not to disclose this information may be subject to manual application of a label, or penalties from YouTube, including removal of content or suspension from the YouTube Partner Program."
Source: https://support.google.com/youtube/answer/14328491 (verified 2026-09-18)

### Disclosure does not cost reach or money

**[OFFICIAL POLICY]** "Disclosing AI content won't limit a video's audience or impact its eligibility to earn money."
Source: https://support.google.com/youtube/answer/14328491 (verified 2026-09-18)

### Likeness and voice

**[OFFICIAL POLICY]** **Likeness detection** "helps creators find content on YouTube where their face appears to be altered or generated by AI." Enrolment requires being over 18 and a Channel Owner or Manager, plus identity verification by government-issued ID and a selfie video ("the verification process may take up to 5 days to complete"). It currently performs "a one-time search of newly uploaded videos, to identify videos that potentially contain the face of each creator." Voice is not yet operational: "We aim to extend likeness detection to audio in the near future." On a match, the enrolled creator may submit a likeness removal request, submit a copyright removal request, or archive the match. YouTube "consider[s] several factors when assessing likeness removal requests, including whether the content is parody or satire."
Source: https://support.google.com/youtube/answer/16440338 (verified 2026-09-18)

**[INDUSTRY PRACTICE]** Reporting (TechCrunch, 21 Apr 2026, "YouTube expands its AI likeness detection technology to celebrities"; and secondary trade coverage) describes a 2026 expansion of enrolment beyond YPP members and to public figures. The compiler confirmed the current *eligibility* wording first-party (over 18 + Channel Owner/Manager, no YPP requirement stated) but did **not** find a first-party page stating the expansion dates. Treat specific expansion dates as unverified.

**[OFFICIAL POLICY]** Voice misuse without an enrolled likeness asset is handled through the privacy complaint process rather than likeness detection.
Source: https://support.google.com/youtube/answer/16440338 (verified 2026-09-18)

### Synthetic media that misleads

**[OFFICIAL POLICY]** The misinformation policy prohibits "Content that has been technically manipulated or doctored in a way that misleads users" where there is serious risk of egregious harm — e.g. inaccurately translated subtitles that inflame geopolitical tensions, or video altered to make it appear a government official is dead. Enforcement: first violation is a warning (expiring after 90 days with policy training); repeat violations within 90 days draw a strike; three strikes within 90 days can terminate the channel; severe cases can terminate immediately.
Source: https://support.google.com/youtube/answer/10834785 (verified 2026-09-18)

**[OFFICIAL POLICY]** The advertiser-friendly guidelines address deepfakes under adult content: "Deepfakes are synthetic media that have been digitally manipulated to replace one person's likeness convincingly with that of another," and prohibit "Promoting the creation or distribution of content that has been digitally altered or generated to be sexually explicit."
Source: https://support.google.com/youtube/answer/6162278 (verified 2026-09-18)

### AI personas related to sensitive topics — a hard demonetization rule

**[OFFICIAL POLICY]** This is a named sub-category of the **inauthentic content** monetization policy. It covers "channels using AI-generated personas to deliver information on sensitive topics" — the sensitive topics named are **health, legal issues, finances, and politics**. The rule: **"channels uploading this content will not be allowed to monetize."** Examples given of what is not allowed:
- "An AI 'doctor' providing medical diagnoses, health advice, or wellness remedies."
- "AI-generated podcast hosts offering financial guidance, investment tips, or wealth management advice."
- "AI personas giving legal advice or interpreting laws."
Source: https://support.google.com/youtube/answer/1311392 (verified 2026-09-18)

**[INFERENCE]** Note the wording is **channel-level** ("channels uploading this content will not be allowed to monetize"), not video-level. A finance-explainer or health-explainer channel fronted by a synthetic host is therefore an existential format, not a per-video risk. The obvious mitigations are (a) do not use a persona that presents as a human expert, or (b) stay out of health/legal/finance/politics. *Reasoning from the quoted rule.*

## 1.4 Reused / inauthentic / unoriginal content — the decisive section

**[OFFICIAL POLICY]** The two policies are **separate and both live**. The July 2025 change touched only the first:

> "July 15, 2025: We're making a minor update to our 'repetitious content' policy to better clarify this includes content that is repetitive or mass-produced. We are also renaming this policy from 'repetitious content' to 'inauthentic content.'"

Source: https://support.google.com/youtube/answer/1311392 (verified 2026-09-18)

**[OFFICIAL POLICY]** The governing principle: content should "Be your original creation" and "Not be mass-produced, generic, repetitive, or manipulative."
Source: https://support.google.com/youtube/answer/1311392 (verified 2026-09-18)

**[OFFICIAL POLICY]** The **inauthentic content** policy has four enforced sub-categories: (1) generic or repetitive content; (2) reused content; (3) unsatisfying or off-putting content (content relying on "emotionally manipulative formulas" or "designed to shock or surprise viewers for the sole purpose of getting views"); (4) AI personas related to sensitive topics (§1.3 above).
Source: https://support.google.com/youtube/answer/1311392 (verified 2026-09-18)

### Generic / repetitive (mass-production) — verbatim

**ALLOWED [OFFICIAL POLICY]:**
- "Same intro and outro for your videos, but the bulk of your content is different."
- "Similar content, like a series following a set of characters across episodes or a channel that does product reviews, but in which each video has a distinct storyline, focus, or concept."

**NOT ALLOWED [OFFICIAL POLICY]:**
- "Similar or repetitive content with low educational value, commentary, narratives, or minimal variation across videos."
- "Videos where characters are put in the same situation over and over again with the same outcome."
- "Image slideshows, templated storylines, or scrolling text with minimal or no narrative, commentary, or educational value."
- **"AI-generated content made with generic or unoriginal templates giving the impression of mass production without adding the creator's original, authentic insights or perspective."**

Source: https://support.google.com/youtube/answer/1311392 (verified 2026-09-18)

### Reused content — verbatim

**ALLOWED [OFFICIAL POLICY]:**
- "Using clips for a critical review."
- "Replays of a sports tournament where you explain the moves a competitor did to succeed."
- "Reaction videos where you comment on the original video."
- "Edited footage from other creators where you add a storyline and commentary."

**NOT ALLOWED [OFFICIAL POLICY]:**
- "Clips of moments from your favorite show edited together with little or no narrative."
- "Content that gets views from mostly non-verbal reactions to your videos without added voice commentary."
- **"Content that exclusively features readings of other materials you did not originally create, like text from websites or news feeds."**
- "Songs modified to change the pitch or speed, but are otherwise identical to the original."

Source: https://support.google.com/youtube/answer/1311392 (verified 2026-09-18)

**[OFFICIAL POLICY]** Scope: the reused-content definition covers channels that "repurpose content that's already on YouTube or another online source" "without adding significant original commentary, substantive modifications, or educational or entertainment value."
Source: https://support.google.com/youtube/answer/1311392 (verified 2026-09-18)

### The single most useful "allowed" sentence for this business

**[OFFICIAL POLICY]** Under "unsatisfying or off-putting content," YouTube gives this as an **allowed** example:

> "Content that expresses your unique creative voice, like using AI to visualize a unique character and narrative you invented"

Other allowed examples in that sub-category: content with cohesive storylines not relying solely on shock value; bringing an authentic perspective when building on a popular format; using creative tools to help deliver a unique, well-researched narrative.
Not allowed: repeatedly disturbing themes without a coherent narrative; generic templates relying on emotionally manipulative themes; videos lacking a narrative arc that stitch unrelated AI clips together; deceptive imagery suggesting fake celebrity deaths or disasters.
Source: https://support.google.com/youtube/answer/1311392 (verified 2026-09-18)

**[INFERENCE]** Read together with the "generic or unoriginal templates" prohibition, YouTube's operative test is not *"was AI used?"* but *"is there an invented, specific, non-interchangeable creative artefact behind this video?"* The discriminator is **per-video creative specificity**, not production method or volume per se. *Reasoning from the two quoted bullets.*

### Shorts-specific ineligibility

**[OFFICIAL POLICY]** Shorts *views* can be individually ruled ineligible for the Creator Pool. Ineligible categories named:
- "Non-original Shorts, such as unedited clips from others' movies or TV shows."
- "reuploading other creators' content from YouTube or other platform."
- "compilations with no original content added."
- "Artificial or fake views of Shorts, such as from automated click or scroll bots."
- "Views of Shorts that are inconsistent with our advertiser-friendly content guidelines."
Source: https://support.google.com/youtube/answer/12504220 (verified 2026-09-18)

**[INFERENCE]** This matters for threshold planning: if the qualifying metric (10M/20M "qualified" Shorts views) excludes views on reused Shorts, an AI pipeline's headline view count can materially overstate its progress toward YPP eligibility. *Reasoning — YouTube does not explicitly state that "qualified Shorts views" and "eligible Creator Pool views" use the same exclusion list, so this link is not verified. See Contradictions and gaps.*

### Spam / deceptive practices — the adjacent Community Guidelines risk

**[OFFICIAL POLICY]** The spam policy prohibits using "automated tools or AI to churn out high volumes of similar content with minimal changes," "coordinated mass-production and technical manipulation to bypass filters," "Repetitive or templated content aimed at artificially inflating engagement," "Re-posting material from other websites or platforms, or other videos without adding anything of your own," and "Maliciously misleading titles, thumbnails, descriptions, or imagery to trick users into clicking." Penalties escalate to strikes, monetization suspension and channel/account termination; three strikes within 90 days terminates the channel.
Source: https://support.google.com/youtube/answer/2801973 (verified 2026-09-18)

**[INFERENCE]** This is the sharpest distinction in the whole dossier and is frequently missed. **Inauthentic/reused content costs you monetization. Spam & deceptive practices costs you the channel.** Mass-produced AI video can sit on either side of that line depending on volume, similarity and metadata honesty. *Reasoning from the two quoted enforcement regimes.*

## 1.5 Disclosure requirements

**[OFFICIAL POLICY]** AI: see §1.3 — the "Altered or synthetic content" attribute in Studio.
Source: https://support.google.com/youtube/answer/14328491

**[OFFICIAL POLICY]** Paid promotion: "If you feature branded content, sponsorships, endorsements, or other commercial relationships in your videos, you have to let YouTube know by selecting the paid promotion button in your video details." This "adds a disclosure label that appears at the beginning of your video." "You and the partners you work with are also responsible for understanding and complying with all applicable legal requirements" (the page references the FTC in the US).
Source: https://support.google.com/youtube/answer/154235 (verified 2026-09-18)

**[OFFICIAL POLICY]** Branded Content Policy: branded content must be declared via the paid promotion declaration in Studio; "Failure to disclose may result in automatic labeling and potential content removal or penalties." Branded content may still earn ad revenue if the channel is in YPP and the content follows advertiser-friendly guidelines. Prohibited branded-content categories: "Recreational drugs or paraphernalia," "Weapons or ammunition," "Hacking software," "Counterfeit products," "An academic essay-writing service." Restricted categories requiring Google certification of the brand partner: "Alcohol," "Financial Services," "Healthcare and Medicines," "Gambling," "Elections and Political Content."
Source: https://support.google.com/youtube/answer/17596007 (verified 2026-09-18)

**Could not verify:** Neither the paid-promotion page nor the Branded Content Policy page fetched addresses **affiliate links** specifically. The compiler found no first-party YouTube rule stating whether affiliate links alone trigger the paid-promotion declaration. **[INFERENCE]** FTC endorsement rules independently require affiliate disclosure in the US regardless of what YouTube requires — but that is US law, not YouTube policy, and was not verified against an FTC source for this dossier.

## 1.6 Advertiser-friendly / brand-safety restrictions

**[OFFICIAL POLICY]** Fourteen guideline categories limit or remove ad revenue:
1. Inappropriate language
2. Violence
3. Adult content
4. Shocking content
5. Harmful acts and unreliable content
6. Hateful & derogatory content
7. Recreational drugs and drug-related content
8. Firearms-related content
9. Controversial issues
10. Sensitive events
11. Enabling dishonest behavior
12. Inappropriate content for kids and families
13. Incendiary and demeaning
14. Tobacco-related content

Three ad states apply: green "This content can earn ad revenue"; yellow "This content will receive limited ad earnings"; red "This content will receive no ad earnings." A self-certification questionnaire is completed at upload. On appeals: "our systems don't always get it right, but you can request human review of decisions made by our automated systems."
Source: https://support.google.com/youtube/answer/6162278 (verified 2026-09-18)

**[OFFICIAL POLICY]** "All content monetizing with ads must follow our advertiser-friendly content guidelines."
Source: https://support.google.com/youtube/answer/1311392 (verified 2026-09-18)

**[OFFICIAL POLICY]** Self-certification: "When you have access to Self-Certification, we'll ask you to self-rate your videos against our advertiser-friendly content guidelines." "We can typically determine how accurate you are after you've rated 20 videos." Critically: **"If we see repeated, egregious inaccuracies based on our advertiser-friendly content guidelines, your channel's eligibility in the YouTube Partner Program may be reviewed."** For a limited-ads decision the creator can "click Request review to get one of our policy specialists to make a final monetization decision," but "Once a human reviewer decides, the monetization status can't be changed."
Source: https://support.google.com/youtube/answer/7687980 (verified 2026-09-18)

**[INFERENCE]** An automated upload pipeline that blindly answers "None of the above" on every self-certification questionnaire is a direct route to "repeated, egregious inaccuracies" and a YPP eligibility review. Self-certification must be driven by actual content classification, not a hardcoded default. *Reasoning from the quoted rule.*

**[OFFICIAL POLICY]** Appeal of a limited/no-ads decision: **one appeal per video**; "Appeals can take up to 7 days"; "After your one appeal, the reviewer's decision is final, and the video's monetization status won't change." Appeal availability is conditional — "You only get the option to appeal if your video is eligible" (no minimum-view figure is stated on the page).
Source: https://support.google.com/youtube/answer/7083671 (verified 2026-09-18)

**[OFFICIAL POLICY]** AI/synthetic content is **not** one of the fourteen categories. The only synthetic-media reference in the advertiser-friendly guidelines is sexually explicit deepfakes under adult content (§1.3).
Source: https://support.google.com/youtube/answer/6162278 (verified 2026-09-18)

## 1.7 Major risks for an AI-operated YouTube channel

All of the following are **[INFERENCE]** drawn from the first-party rules quoted above, ranked by severity.

1. **Channel-level demonetization for templated AI output.** The "AI-generated content made with generic or unoriginal templates giving the impression of mass production" bullet is written almost exactly to describe a high-volume AI pipeline. Enforcement is at channel level, with a 21-day appeal window and a 90-day re-application wait.
2. **Multi-channel contagion.** "Do not … apply to YPP with related channels during your suspension period" implies suspension can follow the operator across a channel network; the boundary of "related" is undefined.
3. **Escalation from spam policy to termination.** Automated high-volume near-duplicate uploading is explicitly named in the spam policy, where the penalty is strikes and termination rather than merely demonetization.
4. **Sensitive-topic personas.** Any synthetic host giving health, legal, financial or political information demonetizes the channel outright.
5. **Auto-dispute of Content ID claims converting revenue loss into strikes.**
6. **Auto-labelling via C2PA removes the operator's discretion** over whether content is publicly marked as AI — a brand/positioning issue rather than a monetization one, since disclosure is stated not to affect earnings.
7. **The 2027 double gate on Shorts** (20M to enter, 10M/90d continuously to earn), which is unforgiving for a portfolio of many small channels and favours fewer, larger ones.

---

# 2. TIKTOK

**Version warning.** **[OFFICIAL POLICY]** TikTok's Community Guidelines **v2026H2** were released 25 Aug 2026 and take effect **24 September 2026** — six days after this dossier's verification date. Current CG pages carry the banner "On September 24, 2026, we are updating our Community Guidelines…". The AIGC section and the Unoriginal Content / IP section are **unchanged** between the current v2025H2 and the incoming v2026H2; one disclosure-enforcement clause does change (§2.5).
Source: https://www.tiktok.com/safety/en/policies-and-engagement/overview?cgversion=2026H2update (verified 2026-09-18)

## 2.1 Monetization eligibility

### Creator Rewards Program — account thresholds

**[OFFICIAL POLICY]** Creators must:
- Be based in a country where the program is available, with an account registered there.
- Have a TikTok account in good standing, "including no history of repeatedly or irresponsibly violating our Community Guidelines and Terms of Service policies and engaging in malicious or fraudulent activities."
- Have a **Personal Account**. "Business Accounts and accounts belonging to political or government organizations aren't eligible."
- Have authentic account information (real name and date of birth).
- Be **at least 18 years old (or 19 in South Korea)**.
- Have **at least 10,000 followers**.
- Have **at least 100,000 video views in the last 30 days**.
- Post original content eligible for rewards, and **videos that are at least one minute long**.

Sources: https://www.tiktok.com/support/faq_detail?id=7581821550694013452 ; https://www.tiktok.com/creator-academy/article/creator-rewards-program (verified 2026-09-18)

**[OFFICIAL POLICY]** Country availability is **exactly eight countries**: "The program is currently open to creators in the United States, United Kingdom, Germany, Japan, South Korea, France, Mexico, and Brazil, so you must be based in one of these countries and have an account registered there."
Source: https://www.tiktok.com/creator-academy/article/creator-rewards-program (verified 2026-09-18)

**[OFFICIAL POLICY]** Application is via TikTok Studio; "You'll receive a reply within approximately 3 days." Rejected applicants "may appeal within 30 days or you can re-apply 30 days after the appeal period."
Source: https://www.tiktok.com/support/faq_detail?id=7581821550694013452 (verified 2026-09-18)

### Video-level qualification

**[OFFICIAL POLICY]** To collect rewards, content must:
- "Post original and high-quality content that is filmed, designed, and produced entirely by yourself."
- "Be at least one minute long."
- "Be uploaded after joining the Creator Rewards Program."
- "Have at least **1,000 qualified For You feed views**."
- "Adhere to our Community Guidelines, Terms of Service, and Copyright Policy."
- Not be a Duet or Stitch; not be created using Photo Mode; not be an ad, paid promotion, sponsored content, or a video linked to a Series.
- **[DOCUMENTED RECOMMENDATION]** "ensure your videos are 1080p or higher."

Sources: https://www.tiktok.com/support/faq_detail?id=7581821550694013452 ; https://www.tiktok.com/creator-academy/article/creator-rewards-program (verified 2026-09-18)

**[OFFICIAL POLICY]** "Qualified views are unique video views from the For You feed and exclude views with fraud, paid views, disliked views, views with less than 5 seconds watched, promoted views, and artificial views." Same-account views count once. Search views require at least 30 seconds watched. Views must come from one of the eight eligible countries.
Sources: https://www.tiktok.com/support/faq_detail?id=7581821550694013452 ; https://www.tiktok.com/creator-academy/article/creator-rewards-program (verified 2026-09-18)

**[OFFICIAL POLICY]** "Rewards are calculated based on qualified views and rewards per 1,000 qualified views (also known as RPM)." Total = Standard Reward + Additional Reward, the latter judged on whether content is "well-crafted," "engaging," and "specialized."
Source: https://www.tiktok.com/support/faq_detail?id=7581821550694013452 (verified 2026-09-18)

### Other TikTok surfaces

**[OFFICIAL POLICY]** TikTok Shop Affiliate (US): minimum **1,000 followers**, 18+, US-based, identity verification required; posting limits effective 11 May 2026 of up to 30 shoppable short videos/day and 60 shoppable photo posts/day. Creators under 5,000 followers sit in a restricted Pilot Program (3 shoppable videos/day, 3 shoppable LIVEs/week).
Source: https://seller-us.tiktok.com/university/essay?knowledge_id=6939143037667118 (verified 2026-09-18)

**[OFFICIAL POLICY]** LIVE: 18+ to go LIVE; 18+ (19 in South Korea) to send or receive Gifts; **1,000 followers** to go LIVE, which "may vary across regions."
Source: https://www.tiktok.com/support/faq_detail?id=7543604790438451768 (verified 2026-09-18)

**Could not verify:** **No first-party TikTok page states any revenue-share percentage for Pulse or Pulse Premiere.** The widely-repeated 50/50 figure traces to 2022 trade press only. **[INDUSTRY PRACTICE]** Axios / Ad Age coverage of the May 2022 launch is the origin. Do not treat it as TikTok policy. The only published Pulse eligibility statement is **[OFFICIAL POLICY]** "Creators and publishers with at least 100k followers will be eligible in the initial stage of this program" (https://newsroom.tiktok.com/en-us/tiktok-pulse-is-bringing-brands-closer-to-community-and-entertainment, 2 May 2022) — which is four years old and may be stale.

## 2.2 Copyright

**[OFFICIAL POLICY]** The governing document is the **Intellectual Property Policy**, "Released March 27, 2025 — Effective April 26, 2025," which merges copyright and trademark. "We do not allow any content that infringes copyright." It recognises "the fair use doctrine in the United States, permitted acts of fair dealing in the European Union (EU), and other equivalent exceptions under applicable local laws in other countries."
Source: https://www.tiktok.com/legal/page/global/copyright-policy/en (verified 2026-09-18)

**[OFFICIAL POLICY]** Repeat infringer policy, verbatim: "We have adopted and reasonably implemented an intellectual property repeat infringer policy under which we, in appropriate circumstances, ban the account of a user who repeatedly commits copyright infringement. We may exercise our discretion to immediately ban any account in cases of severe copyright violations. We reserve the right to refuse any account holder whose account was used for improper activities from opening a new TikTok account."
Source: https://www.tiktok.com/legal/page/global/copyright-policy/en (verified 2026-09-18)

**[OFFICIAL POLICY]** There **is** a copyright-specific strike system: "we issue a strike to someone if their content was removed due to copyright infringement… There is a strike limit for each IP type, after which we'll permanently remove the account. We count strikes for copyright and trademark infringements separately… Accrued strikes will expire from your record after 90 days. We may also remove strikes if the copyright infringement report is retracted or your appeal is approved."
Source: https://www.tiktok.com/support/faq_detail?id=7543604786688563768 (verified 2026-09-18)

**Precision point / could not verify:** TikTok never states the numeric strike limit as a rule. The number **3** appears only inside TikTok's own worked example ("if you obtain 3 strikes for copyright infringement and one strike for trademark infringement, your account will be banned"). Cite it as an example, not a published threshold. TikTok also does not publish a numeric threshold for general Community Guidelines strikes.

**[OFFICIAL POLICY]** LIVE: "If a user infringes copyright by using the LIVE feature, we may also temporarily restrict their access to LIVE feature."
Source: https://www.tiktok.com/legal/page/global/copyright-policy/en (verified 2026-09-18)

**[OFFICIAL POLICY]** Counter-notification: available in-app; "where appropriate and authorised by law, we will forward your entire appeal to the original reporter, including any contact information you provide… The copyright claimant may use this information to file a lawsuit against you." Reinstatement "is at TikTok's sole discretion." And: "We delete the removed content after a period of time… After the deletion, your content can no longer be reinstated."
Source: https://www.tiktok.com/legal/page/global/copyright-policy/en (verified 2026-09-18)

**Could not verify:** TikTok publishes **no number of business days** for counter-notification restoration anywhere on its copyright pages. The commonly cited "10–14 business days" is the US DMCA statutory default (17 U.S.C. §512(g)(2)(C)), not a TikTok statement.

**[OFFICIAL POLICY]** Monetization consequence: "Rewards already collected on removed videos will be deducted from your balance."
Source: https://www.tiktok.com/support/faq_detail?id=7581821550694013452 (verified 2026-09-18)

### Music — Commercial Music Library vs general library

**[OFFICIAL POLICY]** The core rule, verbatim: "Commercial Sounds are the only sounds made available on TikTok for Commercial Uses. Otherwise, no rights are granted to make Commercial Uses of any other sounds (music and non-music) on or through TikTok." Scope is TikTok-only: "Commercial Uses outside of TikTok are not permitted… Any uses outside of TikTok are subject to you obtaining separate permission and licensing the necessary rights directly from the Commercial Sound rights holder(s)." And the liability clause: users "are responsible for any claims and disputes that arise from… your unauthorized, unlicensed use of sounds contained in the non-commercial 'General Music Library'… you shall indemnify and hold harmless TikTok."
Source: https://www.tiktok.com/legal/page/global/commercial-music-library-user-terms/en (verified 2026-09-18)

**[OFFICIAL POLICY]** "Businesses cannot use the general music library for commercial usage. Businesses should instead use the Commercial Music Library for all commercial TikTok activities," covering organic content (including duet/react/stitch), video ads, and branded content. The CML is "a pre-cleared global music library of 1 million songs," free to use. Enforced by product design: a Business Account "will only see sounds from the Commercial Music Library."
Sources: https://ads.tiktok.com/help/article/commercial-music-library ; https://ads.tiktok.com/help/article/how-to-use-the-commercial-music-library (verified 2026-09-18)

**[OFFICIAL POLICY]** Creator Rewards interaction: content "that contains lip syncs or copyrighted music that plays for over one minute" is not original, and "Videos under the program that contain copyrighted music for over one minute are at risk of being muted."
Source: https://www.tiktok.com/support/faq_detail?id=7581821550694013452 (verified 2026-09-18)

**[INFERENCE]** These two rules conflict operationally: the Commercial Music Library is only fully surfaced to **Business Accounts**, but Business Accounts are **ineligible for Creator Rewards**. A single TikTok account cannot simultaneously hold the safest commercial-music posture and the rewards-eligible account type. *Reasoning from the two quoted rules — TikTok does not address the tension anywhere.*

## 2.3 AI-generated / synthetic content

### Is AI content allowed?

**[OFFICIAL POLICY]** Yes, with a labelling duty for realism: "We welcome creativity, including when it comes from new digital tools like generative artificial intelligence (AI)… we require creators to label AI-generated or significantly edited content that shows realistic-looking scenes or people. Unlabeled content may be removed, restricted, or labeled by our team, depending on the harm it could cause."
Source: https://www.tiktok.com/safety/en/policies-and-engagement/integrity-authenticity (verified 2026-09-18)

**[OFFICIAL POLICY]** "On TikTok, we require people to disclose realistic AI-generated content (AIGC), so that they can express their creativity while providing context for viewers."
Source: https://www.tiktok.com/tns-inapp/pages/ai-generated-content (verified 2026-09-18)

### What must and must not be labelled — the decisive carve-outs

**[OFFICIAL POLICY]** Disclosure **IS** needed when: a face is replaced with someone else's; AI tools make it look like someone said something they didn't; a background, object or person is added or removed in a misleading way; AI-generated audio mimics the voice of a real person.

**[OFFICIAL POLICY]** Disclosure is **NOT** needed when: making small edits like colour correction, reframing or cropping; **using artistic styles, like anime**; **"Using generic text-to-speech (TTS) narration, when the TTS isn't a recognizable voice of a known individual."**

Source: https://www.tiktok.com/safety/en/policies-and-engagement/integrity-authenticity (verified 2026-09-18)

**[INFERENCE]** This is the most commercially significant carve-out across all four platforms. TikTok's labelling trigger is **realism about real people and scenes**, not "AI was used." A pipeline built on stylised (non-photoreal) visuals plus generic TTS narration sits **outside** TikTok's mandatory-labelling scope entirely. *Reasoning from the two quoted lists.*

### Prohibited even when labelled

**[OFFICIAL POLICY]** Not allowed: likeness of private figures without consent; sexualized, fetishized or victimizing depictions; AI likenesses made to bully or harass; and AIGC that misleads about a matter of public importance, including content made to look like it comes from a real news source, a crisis event, a public figure degraded or linked to criminal behavior, "a public figure taking political stances, supporting products, or commenting on public issues they haven't actually addressed," and "a political endorsement or condemnation that never happened."
Source: https://www.tiktok.com/safety/en/policies-and-engagement/integrity-authenticity (verified 2026-09-18)

### Label mechanics and automatic labelling

**[OFFICIAL POLICY]** Two labels exist. The manual one reads "**Creator labeled as AI-generated**" and is set at post time (More options → AI-generated content). The automatic one reads "**AI-generated**": "TikTok may automatically apply the 'AI-generated' label to content that we identify as completely generated or significantly edited with AI. This may happen when a creator uses TikTok AI effects or uploads AI-generated content that was made on certain platforms."
Sources: https://www.tiktok.com/tns-inapp/pages/ai-generated-content ; https://www.tiktok.com/support/faq_detail?id=7636670084747893268 (verified 2026-09-18)

**[OFFICIAL POLICY]** The automatic mechanism is **C2PA Content Credentials**. TikTok is "the first video sharing platform to implement their Content Credentials technology"; from 9 May 2024 it began "expanding auto-labeling to AIGC created on some other platforms by launching the ability to read Content Credentials," and "we'll also start attaching Content Credentials to TikTok content, which will remain on content when downloaded." TikTok states it has "required creators to label realistic AIGC for over a year" as of that date.
Source: https://newsroom.tiktok.com/en-us/partnering-with-our-industry-to-advance-ai-transparency-and-literacy (9 May 2024, verified 2026-09-18)

**[OFFICIAL POLICY]** TikTok also uses **invisible watermarking**: "'Invisible watermarks' add another layer of safeguards with a robust technological 'watermark' that only we can read, making it harder for others to remove."
Source: https://newsroom.tiktok.com/more-ways-to-spot-shape-and-understand-ai-content (19 Nov 2025, verified 2026-09-18)

**[OFFICIAL POLICY]** The label is **irreversible**: "Note that adding the label to your content won't affect the distribution of your video, and **you can't remove the label after posting**." For auto-labels: "Once your content is labeled as AI-generated with an auto label, you won't be able to remove the label from your post."
Sources: https://www.tiktok.com/tns-inapp/pages/ai-generated-content ; https://www.tiktok.com/support/faq_detail?id=7636670084747893268 (verified 2026-09-18)

**[OFFICIAL POLICY]** Mislabelling is itself a violation: "Misleadingly labeling unaltered content with this label is a violation of our Terms of Service and may result in the removal of content."
Source: https://www.tiktok.com/support/faq_detail?id=7636670084747893268 (verified 2026-09-18)

### Terms of Service §3.10 — automated use of TikTok's own AI features

**[OFFICIAL POLICY]** TikTok's US Terms of Service (Last updated **15 July 2026**), §3.10 "Using our generative AI features," states that you agree not to:
> "Use generative AI-enabled features via any automated system or software, including automated 'bots,' unless otherwise authorized,
> Represent, imply or otherwise create an impression that your Output is human-generated or otherwise generated without the use of AI, including by removing, obscuring, or altering any watermarks, content-authenticating metadata, or other marking or disclosure applied to or associated with your Output,
> Provide, create, or otherwise use Input or Output in a fraudulent manner or to deceive, mislead or impersonate others,
> Interfere with, disable, or circumvent any restrictions, filters, controls or safety measures on our Platform, including our generative AI-enabled features."

Source: https://www.tiktok.com/legal/page/us/terms-of-service/en (verified 2026-09-18)

**Important scoping note — [INFERENCE].** §3.10 opens "We may provide and make available to you generative AI-enabled features… for you to provide Input and generate Output." Read in context, the automation prohibition governs **TikTok's own generative AI features**, not the act of uploading video generated elsewhere. However, the second bullet — on removing or altering watermarks and content-authenticating metadata — is broader in effect, because TikTok reads C2PA on upload. *This scoping is the compiler's reading; TikTok does not state the boundary explicitly.*

**[OFFICIAL POLICY]** Separately, §3.4 prohibits users from "use[ing] TikTok Content…, another user's content or generative AI-enabled features **for commercial purposes** unless permitted by TikTok USDS Joint Venture or the user, respectively."
Source: https://www.tiktok.com/legal/page/us/terms-of-service/en (verified 2026-09-18)

### Likeness detection

**[OFFICIAL POLICY]** TikTok offers likeness detection to "certain verified accounts and creators who are aged 18 or older." It "only scans content marked or detected as AI-generated," begins only after enrolment, and can detect only enrolled users. Enrolment requires government ID and a facial scan.
Source: https://www.tiktok.com/support/faq_detail (Likeness Detection for AIGC) (verified 2026-09-18)

### Does AI content affect Creator Rewards eligibility?

**Could not verify — significant negative finding.** There is **no first-party TikTok statement that AI-generated content is categorically ineligible for the Creator Rewards Program.** The CRP support page, the CRP US legal terms (updated 20 July 2026), the Creator Academy eligibility article, the video-eligibility article and the Originality Policy were each checked; none mentions AI. **[INFERENCE]** The exposure is indirect but real: templated, minimally-edited or slideshow AIGC is caught squarely by the **originality** and **low-quality** rules in §2.4, which *are* explicit about monetization ineligibility. TikTok did not need an AI-specific monetization rule because the originality rule already reaches the same conduct.

## 2.4 Unoriginal / duplicated / reused content

### The Originality Policy — what counts as unoriginal

**[OFFICIAL POLICY]** "The main types of unoriginal content" (last updated 24 Dec 2025):
1. **"Content copied completely from others"** — "You cannot directly use content from other sources without permission."
2. **"Content that is largely repurposed from another source without adding any creative edits"** — avoid "Simply adding subtitles based entirely on the original audio" and "Only adding basic text summaries to other creators' content."
3. **"Content combined from multiple sources with little or no additional information or content value"** — avoid "Only splicing clips together" and "Inserting random, unrelated elements with no purpose."
4. **"Content with someone else's visible watermark or superimposed logo"** — "If your repurposed content includes a watermark or logo, in most cases, it does not count as original content."

Source: https://www.tiktok.com/creator-academy/article/tiktok-originality-policy (verified 2026-09-18)

**[OFFICIAL POLICY]** Consequences, verbatim:
- "Unoriginal content may be removed from the For You feed, making it harder to discover."
- "Any content that violates our Community Guidelines, which includes Unoriginal Content and QR Codes, is removed and made ineligible for recommendation."
- **"Unoriginal content is also ineligible for TikTok's monetization programs like the Creator Rewards Program, which uses 'originality' as a key metric in its rewards formula."**

Source: https://www.tiktok.com/creator-academy/article/tiktok-originality-policy (verified 2026-09-18)

### Creator Rewards — what is NOT original

**[OFFICIAL POLICY]** "Content not considered original under the program includes, but is not limited to:"
- "Duet or Stitch videos."
- "Content copied completely from others and with others' watermarks."
- **"Content reproduced from others with only slight modifications, including videos that are sped up or contain filters, fixed texts, or stickers."**
- "Content that contains different videos or pictures originating from other people, creators, or sources without new and personal ideas."
- **"Content that contains looping videos, single or multiple photos, or only text overlays."**
- "Content that contains lip syncs or copyrighted music that plays for over one minute."

Source: https://www.tiktok.com/support/faq_detail?id=7581821550694013452 (verified 2026-09-18)

**[OFFICIAL POLICY]** The positive standard: "Original content under the Creator Rewards Program is quality content that's designed, filmed, and produced by you that showcases your expertise, talent, or creativity."
Source: https://www.tiktok.com/support/faq_detail?id=7581821550694013452 (verified 2026-09-18)

### The account-level disqualification thresholds — hard numbers

**[OFFICIAL POLICY]** Content ineligible for the For You feed is also ineligible for Creator Rewards. The named ineligible factors include:
- **Unoriginal:** "Your account info or video content is copied from others or has minimal original input or edits."
- **Low quality:** "Your account info or video content includes split screens, meaningless reactions, low-quality images, or slide videos."
- **Advertising:** "Your profile or account info contains personal or business contact information."
- **Clickbait:** "Your profile, account info, or content lacks meaningful value and is primarily designed to attract followers, likes, or advertisement clicks."
- Inappropriate; Disturbing; Security issue (artificial engagement, manipulation of the rewards system).
- **"Content violation threshold: Your account has 5 video violations in the past 30 days."**
- **"Account violation threshold: Your account has 5 total account violations, which will make you permanently ineligible for the program."**

Source: https://www.tiktok.com/creator-academy/article/creator-rewards-program (verified 2026-09-18)

**[OFFICIAL POLICY]** Forfeiture: "If you no longer have access to the Creator Rewards Program due to violations that affect your account standing, such as your TikTok account being permanently banned, you will forfeit any outstanding rewards from the program."
Source: https://www.tiktok.com/creator-academy/article/creator-rewards-program (verified 2026-09-18)

### Account-level For You feed suppression without any violation

**[OFFICIAL POLICY]** Community Guidelines, Accounts and Features: "Sometimes, accounts that don't break the rules still post a lot of content that's ineligible for the FYF. In those cases, we may make the account and its content ineligible for the FYF and harder to find."
Source: https://www.tiktok.com/safety/en/policies-and-engagement/accounts-features (verified 2026-09-18)

**[INFERENCE]** This is the most dangerous clause on TikTok for a volume operation, because it requires no violation, produces no strike, and therefore offers no obvious appeal trigger. Since Creator Rewards requires 1,000 **qualified For You feed views** per video, account-level FYF suppression zeroes revenue while leaving the account nominally in good standing. *Reasoning from the quoted clause plus the qualified-view definition.*

### What IS allowed — TikTok's own remediation guidance

**[DOCUMENTED RECOMMENDATION]** "If your video is flagged as unoriginal, you're most likely missing those personal touches… **Appear onscreen!** Starring in your content or adding your own voice-overs are great ways to express your original opinion and make your video more personalized." Also: "Provide extra background information and details for an in-depth explanation of the content you're interpreting"; "Reconstruct your material with extra editing that showcases the plot or narrative in a new light. Rearrange clips, insert your original opinions."
Source: https://www.tiktok.com/creator-academy/article/tiktok-originality-policy (verified 2026-09-18)

**[DOCUMENTED RECOMMENDATION]** TikTok provides an **Account Check** tool to check originality status, and **Content check lite**, a pre-publication automated check against the FYF Eligibility Standards — with the caveat that "our check is only preliminary and does not guarantee that your content complies."
Sources: https://www.tiktok.com/creator-academy/article/tiktok-originality-policy ; https://www.tiktok.com/creator-academy/article/content-check-lite (verified 2026-09-18)

**[INFERENCE]** Note the important asymmetry with Meta: TikTok explicitly names **"adding your own voice-overs"** as a cure for unoriginality, whereas Meta explicitly names voiceover that merely narrates as **not** a cure (§3.4). The same asset can therefore be original on TikTok and unoriginal on Meta. *Reasoning from the two quoted first-party positions.*

## 2.5 Disclosure requirements

**[OFFICIAL POLICY]** "If you're posting commercial content on TikTok, you must clearly disclose it using the content disclosure setting." Disclosure is required when promoting your own business, product or service, and when "Posting branded content, including reviews or endorsements, and receiving any kind of incentive in exchange."
Source: https://www.tiktok.com/safety/en/policies-and-engagement/regulated-commercial-activities (verified 2026-09-18)

**[OFFICIAL POLICY]** The toggle has two modes: selecting **"Your brand"** shows viewers **"Promotional content"**; selecting **"Branded content"** shows **"Paid partnership"** and allows tagging a brand partner. "If you don't display the proper disclosure, we may remove or restrict your posts." The label cannot be changed once published. "Turning on the content disclosure setting won't affect the distribution of your post in feeds on TikTok."
Source: https://www.tiktok.com/support/faq_detail?id=7636670088170904085 (verified 2026-09-18)

**[OFFICIAL POLICY]** **Enforcement change on 24 Sept 2026.** Current wording (v2025H2) tags undisclosed commercial content **FYF INELIGIBLE**: "If commercial content isn't disclosed using the content disclosure setting, it will be ineligible for the FYF… Repeated failure to make a disclosure can lead to your account being temporarily restricted from posting content, or can lead to an account ban." From 24 Sept (v2026H2) this softens to: "If we find commercial content that hasn't been properly disclosed, we may reduce its visibility or apply the content disclosure setting," with the FYF-INELIGIBLE tag removed. The account-ban language for repeated failure is **unchanged**.
Sources: https://www.tiktok.com/safety/en/policies-and-engagement/regulated-commercial-activities and the same URL with `?cgversion=2026H2update` (verified 2026-09-18)

### Branded Content Policy — new, and it explicitly covers affiliate links

**[OFFICIAL POLICY]** Published **4 August 2026**, **effective 31 August 2026**. "Branded content ('Branded Content') is content that promotes or reviews a third-party brand or its products or services in exchange for payment or any other incentive." It expressly includes:
- a product or service gifted to you by or on behalf of a brand;
- a brand, product or service you were paid to post about (money or gift);
- **"A product or service for which you will receive a commission on any sales (for instance, via an affiliate link or promotional code)"**;
- a brand you have or have had a commercial relationship with, such as a brand ambassador.

Source: https://www.tiktok.com/legal/page/global/bc-policy/en (verified 2026-09-18)

**[OFFICIAL POLICY]** "When posting Branded Content, you must enable the commercial content disclosure toggle." And a clarity rule: "You must ensure that the product or service you are promoting is sufficiently clear, without requiring viewers to access your profile page or any links. For instance, you should explicitly identify the product or service verbally and/or in the text caption." Enforcement: "we may remove the content or impose other restrictions."
Source: https://www.tiktok.com/legal/page/global/bc-policy/en (verified 2026-09-18)

**[OFFICIAL POLICY]** 15 **Prohibited Industries** for branded content: adult and sexual products/services; animals; cigarettes, tobacco and nicotine; dating and live video applications; drug-related products (incl. CBD); financial services (pyramid schemes, MLM, payday loans, get-rich-quick); pharmaceuticals, healthcare and medicine (incl. telehealth, therapy, infant formula, teeth whitening, cosmetic clinics); **political advertising** (incl. creators compensated for branded political content); products enabling dishonest behaviour; **professional services (accounting, legal, immigration)**; sensitive religious content; weapons, ammunition or explosives; weight loss products or services; counterfeit products; other prohibited products/services.
Source: https://www.tiktok.com/legal/page/global/bc-policy/en (verified 2026-09-18)

**[OFFICIAL POLICY]** Restricted industries (alcohol, dating apps, energy drinks, trailers, financial services, OTC and prescription medicines, vitamin supplements, gambling, underwear, government advertising) are allowed only by invitation from an approved brand: "All brand partners must receive explicit permission from TikTok… The brand partner must have a Registered Business Account… Brands may only partner with content creators through the TikTok One platform." Restricted branded content is shown only in the market where posted and may be age-restricted.
Source: https://www.tiktok.com/legal/page/global/bc-policy/en (verified 2026-09-18)

**[OFFICIAL POLICY]** TikTok Shop creators must "clearly disclose that your post is sponsored, and that you have a commercial relationship with the Merchant," per applicable law "including the US FTC's Guides Concerning the Use of Endorsements and Testimonials in Advertising"; and "in addition to your independent legal obligation… TikTok requires that you use the Branded Content Toggle."
Source: https://seller-us.tiktok.com/university/essay?knowledge_id=6314510387906350 (verified 2026-09-18)

## 2.6 Advertiser-friendly / brand safety

**[OFFICIAL POLICY]** TikTok's **Inventory Filter** has three tiers — **Expanded / Standard / Limited** (not "Full/Standard/Limited"):
- **Expanded Inventory** — "Your ads will not appear next to explicitly inappropriate content, but they may appear next to content that features mature themes."
- **Standard Inventory** — "Your ads will appear next to content that is appropriate for most brands and may contain some mature themes."
- **Limited Inventory** — "Your ads will appear next to content that doesn't contain mature themes."

The 14 categories: Military content; Terrorism; Disrespectful religion and culture; Adult sexual content; Drugs and drug paraphernalia; Illegal services and activity; Private personal information and privacy infringement; Weapons, ammunition, and explosives; Obscenity and profanity; Sensational and shocking content; Piracy and infringement; Discriminatory content; Tobacco products and smoking; Alcohol; Political content.
Source: https://ads.tiktok.com/help/article/tiktok-inventory-filter (verified 2026-09-18)

**[OFFICIAL POLICY]** Additional advertiser controls: **Category Exclusion** (exactly four blockable categories — Youth content, Gambling and lotteries, Violent video games, Combat sports); **Vertical Sensitivity** (one of 11 verticals); **Video Exclusion List** (up to 200,000 video IDs); **Profile Feed Exclusion List** (up to 500 usernames).
Source: https://ads.tiktok.com/help/article/about-brand-safety-hub (verified 2026-09-18)

**[OFFICIAL POLICY]** The **For You Feed Eligibility Standards** page itself contains no category list; FYF-ineligible categories are distributed through the Community Guidelines. The bullets most relevant here are: **"Reused or unoriginal content posted without creative edits, such as clips that show someone else's watermark or logo"** and **"Low-quality or minimally edited content, such as short clips made from GIFs only."**
Sources: https://www.tiktok.com/safety/en/policies-and-engagement/fyf-standards ; https://www.tiktok.com/safety/en/policies-and-engagement/integrity-authenticity (verified 2026-09-18)

**[OFFICIAL POLICY]** There is **no** For You feed category named "mass-produced," "AI slop," or "spam content." The nearest hooks are the unoriginal/low-quality bullets, the Deceptive Behaviors and Fake Engagement rules, and the AIGC labelling rules.

**[OFFICIAL POLICY]** The Deceptive Behaviors section does name automation directly. Not allowed: "Spam, such as: Using automation to run many accounts or send repetitive content · Posting a large amount of irrelevant material… Using AI or bot accounts to drive traffic." And: "We strictly prohibit automation tools, scripts, or other tricks designed to bypass our systems. These can result in content removal, account bans, or other enforcement."
Source: https://www.tiktok.com/safety/en/policies-and-engagement/integrity-authenticity (verified 2026-09-18)

**[OFFICIAL POLICY]** The **Creator Code of Conduct** (last updated 11 Sept 2026) states that "Access to these programs is a privilege, not a right," and prohibits "collusion, cheating, DDoSing, **multiple-account abuse**, or **use of VPNs to circumvent our systems**." Penalty: "For severe violations, we may temporarily restrict your access to our Creator Programs for three months… For egregious behavior, the restriction may be permanent."
Source: https://www.tiktok.com/creator-academy/article/creator-code-of-conduct (verified 2026-09-18)

## 2.7 Major risks for an AI-operated TikTok channel

All **[INFERENCE]** from the rules quoted above.

1. **The 5-violation cliff.** "5 video violations in the past 30 days" disqualifies; "5 total account violations" makes the account **permanently ineligible**. Mass production is exactly the regime that hits a 5-in-30 threshold fastest, because a template defect does not produce one bad video — it produces every video.
2. **Account-level FYF suppression with no violation and no appeal trigger.** Revenue goes to zero while the account stays "in good standing."
3. **Originality is a scored input to the payout formula**, not merely a gate. RPM can degrade before any formal flag appears, so the channel can be economically dead before it is administratively flagged.
4. **The Photo Mode / slideshow trap.** The cheapest AI format — stills plus TTS plus captions — is excluded three separate ways (Photo Mode, "single or multiple photos," "slide videos").
5. **Multiple-account abuse and VPN use are named prohibitions** in the Creator Code of Conduct — directly relevant to a channel network and to reaching the eight eligible countries from outside them.
6. **Business Account vs Creator Rewards is a forced architectural choice** (§2.2).
7. **Labelling is irreversible in both directions**, and must be decided correctly before posting.

---

# 3. META — FACEBOOK AND INSTAGRAM

## 3.1 Monetization eligibility

### The legacy Facebook programs have ended

**[OFFICIAL POLICY]** "In-stream Ads, Ads on Reels, and the Performance Bonus Program ended on August 31, 2025, which is the last day creators were able to earn from any of these monetization programs. Eligible creators will be invited to the Facebook Content Monetization program."
Source: https://www.facebook.com/business/help/1049081556813520 (verified 2026-09-18)

**[OFFICIAL POLICY]** "In-stream ads for Live is ending on June 15, 2026. After that date, you won't be able to access the In-stream ads for Live program or any of its features."
Source: https://www.facebook.com/business/help/267128784014981 (verified 2026-09-18)

### Facebook Content Monetization

**[OFFICIAL POLICY]** It is "an invite-only program that lets you earn money from the performance of your eligible public reels, photos, Stories, and text posts."
Source: https://www.facebook.com/business/help/1049081556813520 (verified 2026-09-18)

**Could not verify — important negative finding.** Meta publishes **no numeric follower, watch-minute, view, video-count or account-age threshold** for Facebook Content Monetization. The widely-quoted legacy figures (10,000 followers / 600,000 watch minutes in 60 days / 5 videos) come from pages that now redirect to the new content and are **not** restated anywhere current. This dossier will not present them as current. **[INFERENCE]** Thresholds presumably exist but are held internally and surfaced per account in Meta Business Suite.

**[OFFICIAL POLICY]** The one published account-age number is in the Partner Monetisation Policies: an established presence "for at least **30 days**."
Source: https://www.facebook.com/business/help/169845596919485 (verified 2026-09-18)

**[OFFICIAL POLICY]** Published content-level minimums: "Reels must be a minimum of 10 seconds"; "Stories must be a minimum of 5 seconds"; "If watch time is less than 5 seconds for any piece of content, it does not qualify for monetisation."
Source: https://www.facebook.com/business/help/1049081556813520 (verified 2026-09-18)

**[OFFICIAL POLICY]** The one published numeric route in is **Creator Fast Track** (announced 18 March 2026): $1,000/month for 100,000+ followers on Instagram, TikTok or YouTube; $3,000/month for 1M+ on at least one; three months guaranteed pay plus immediate access to Facebook Content Monetization.
Source: https://about.fb.com/news/2026/03/creator-fast-track-grow-your-audience-earn-money-on-facebook/ (verified 2026-09-18)

### The two policy layers

**[OFFICIAL POLICY]** "Partner Monetisation Policies … apply at the account level; they address the behaviour of your account as a whole." "Content Monetisation Policies … apply at the content level. They address the content of each individual reel or post." PMP was formerly "Monetisation Eligibility Standards"; CMP was formerly "Content Guidelines for Monetisation." Both apply to "all Pages, profiles in professional mode, events and groups on Facebook."
Sources: https://www.facebook.com/business/help/185404538833362 ; https://www.facebook.com/business/help/169845596919485 (verified 2026-09-18)

**[OFFICIAL POLICY]** Enforcement asymmetry: for Community Standards, "access to monetisation programmes can be removed after a **single** violation"; for PMP, "**Consistent** violations … could lead to a temporary or permanent ban"; for CMP, "**Repeated** violations … could lead to a temporary or permanent ban." And: "Individual reel violations … may result in that specific reel being unable to earn money. However, serious or repeated violations on individual reels could cause your entire account to lose access."
Source: https://www.facebook.com/business/help/1979171292197867 (verified 2026-09-18)

**[OFFICIAL POLICY]** Other PMP clauses of note: **"Monetise authentic engagement"** prohibits behaviour that boosts followers, views or engagement to generate revenue, "includ[ing] manufactured sharing, which is coordinated distribution of content, often for compensation and **high-volume crossposting**." Facebook profiles outside professional mode are ineligible. Politicians, candidates, parties and government agencies are ineligible.
Source: https://www.facebook.com/business/help/169845596919485 (verified 2026-09-18)

### Country and language availability

**[OFFICIAL POLICY]** Facebook Content Monetization ads are available in a published list of countries/territories and in 37 languages. The country list includes Algeria, Argentina, Australia, Austria, Bangladesh, Belgium, Bolivia, Brazil, Bulgaria, Canada, Chile, Colombia, Cyprus, Czech Republic, DR Congo, Denmark, Dominican Republic, Ecuador, Egypt, El Salvador, France, Georgia, Germany, Ghana, Greece, Guatemala, Honduras, Hong Kong, Hungary, India, Indonesia, Iraq, Ireland, Israel, Italy, Japan, Jordan, Kenya, Malaysia, Mexico, Morocco, Nepal, New Zealand, Nigeria, Norway, Peru, Poland, Portugal, Puerto Rico, Romania, Saudi Arabia, Singapore, South Africa, South Korea, Spain, Sri Lanka, Sweden, Switzerland, Taiwan, Tanzania, Thailand, The Netherlands, The Philippines, Tunisia, Turkey, Uganda, Ukraine, United Arab Emirates, United Kingdom, United States. **Vietnam is not on the list.** Vietnamese **is** on the language list. "Note: Reels in multiple languages may not be eligible for monetisation."
Source: https://www.facebook.com/business/help/267128784014981 (verified 2026-09-18 by direct extraction of the page's rendered list)

### Instagram monetization

**[OFFICIAL POLICY]** Instagram's options and published thresholds:

| Tool | Threshold | Source |
|---|---|---|
| Gifts / Stars | **500 followers**, 18+, professional account | https://www.facebook.com/help/instagram/738469380549477 |
| Subscriptions | **10,000 followers**, 18+ | https://www.facebook.com/help/instagram/478012211024479 |
| Creator Marketplace | **1,000 followers**, 18+, public professional account | https://www.facebook.com/help/instagram/1389278101788752 |
| Branded content / partnership ads | "sufficient follower count" — **no number published** | https://www.facebook.com/help/instagram/1372533836927082 |
| Bonuses | **Invite-only**; Reels bonus = 5,000,000 views across 3 consecutive months (Japan, South Korea, US) | https://www.facebook.com/help/instagram/708013994693013 |
| Affiliate | Relaunched **24 March 2026**; no thresholds or rates published | https://creators.instagram.com/blog/new-ways-to-earn-making-reels-shoppable |

(All verified 2026-09-18.) **[OFFICIAL POLICY]** Gifts pay **$0.01 USD per Star** (https://www.facebook.com/legal/stars_terms). **[OFFICIAL POLICY]** "For reels rewards, only original newly created content counts," and branded content is **ineligible** for bonuses (https://www.facebook.com/help/instagram/434406642308284).

**[OFFICIAL POLICY — verified by absence]** Instagram has **no in-stream or pre-roll ad revenue share**. Three first-party pages (creators.instagram.com/earn-money, creators.instagram.com/lab/content-monetization, facebook.com/help/instagram/427415519366046) each enumerate the same closed set of tools with no ad-revenue-share product.

**[INDUSTRY PRACTICE — flagged as unsupported]** Blog claims that Instagram pays "55% Reels ad revenue share at 10,000 followers" or "~$30 per 1M views" appear on no Meta page and contradict Instagram's own published product list. Do not rely on them.

## 3.2 Copyright

**[OFFICIAL POLICY]** **Rights Manager** "is a tool that identifies videos on Facebook and Instagram, including Live videos, that match rights holders' copyrighted content." Rights holders upload reference files; on a match they may **block**, **claim available ad earnings**, **monitor**, or **report as an IP violation**. Meta separately uses **Audible Magic**, "which flags uploaded videos and prevents them from being viewed by others when an audio match is detected."
Source: https://transparency.meta.com/reports/intellectual-property/protecting-intellectual-property-rights/ (verified 2026-09-18)

**[OFFICIAL POLICY]** Rights Manager access is by **application**, not automatic, and requires verified rights, an eligible catalogue and a clean infringement record. Ad earnings are "not available to all rights holders yet."
Source: https://creators.facebook.com/tools/rights-manager/ (verified 2026-09-18)

**[OFFICIAL POLICY]** Revenue effect on the uploader: "If another person or business claims ownership of some of the content that you posted, that person or business may receive a portion of the earnings for that content. While your content is being reviewed, a portion or all of your payout will be withheld pending the resolution on the ownership claim."
Source: https://www.facebook.com/business/help/1049081556813520 (verified 2026-09-18)

**[OFFICIAL POLICY]** Takedown mechanics: only the copyright owner or an authorised representative may file. Meta may remove reported content "promptly" and without contacting the poster first, and the reported party receives the rights owner's name and email address. Counter-notification is available "if the content was removed because of a mistake or misidentification," is forwarded with the filer's contact details, and if no court action is notified Meta "will restore or cease disabling eligible content under the DMCA. This process can take **up to 14 working days**." Restored content **"will not be counted against you under our Repeat Infringer policy."**
Sources: https://www.facebook.com/help/325058084212425 ; https://www.facebook.com/help/1900735080058381 ; https://www.facebook.com/help/265723950293778 (verified 2026-09-18)

**[OFFICIAL POLICY]** "It's possible to infringe someone else's copyright, even if you don't intend to do so." Credit, disclaimers and having purchased the content do not prevent infringement.
Source: https://www.facebook.com/help/225191540826940 (verified 2026-09-18)

### Strikes — and a widely misunderstood gap

**[OFFICIAL POLICY]** Meta runs a numeric strike ladder, but it is the **Community Standards** system: 1 strike = warning; 2–6 = feature restrictions; 7 = 1-day content-creation restriction; 8 = 3 days; 9 = 7 days; 10+ = 30 days. "All strikes on Facebook or Instagram expire after one year."
Sources: https://transparency.meta.com/enforcement/taking-action/restricting-accounts/ ; https://transparency.meta.com/enforcement/taking-action/counting-strikes/ (verified 2026-09-18)

**[OFFICIAL POLICY]** The IP side is a **separate, explicitly discretionary repeat-infringer policy with no published number**: "If you repeatedly post content that violates intellectual property rights, then your: Account may be disabled, Page may be removed, Group may be removed."
Sources: https://www.facebook.com/help/350712395302528 ; https://www.facebook.com/help/instagram/1586774981367195 (verified 2026-09-18)

**Could not verify:** whether the numeric Community Standards strike ladder applies to IP removals. Neither transparency.meta.com strike page mentions intellectual property. **[INFERENCE]** Meta has no published "3 strikes" copyright counter analogous to YouTube's; any specific number quoted for Meta copyright strikes is not sourced to a first-party page.

### Music

**[OFFICIAL POLICY]** Meta Music Guidelines, effective **26 March 2024**: "Use of music for commercial or non-personal purposes in particular is prohibited unless you have obtained appropriate licences." Music-focused content is banned — you may not "create a music listening experience for yourself or for others… This includes live videos," and "your content may be blocked and your page, profile or group may be deleted." "There should always be a visual component to your content; recorded audio should not be the primary purpose." "The greater the density of music in content, the more likely it may be limited (e.g. blocked, muted or ineligible for Music Revenue Share)."
Source: https://www.facebook.com/legal/music_guidelines (verified 2026-09-18)

**[OFFICIAL POLICY]** The operative rule for monetized video — **the 90-second rule**: "Music in the audio library labelled as Licensed music or Royalty-free is eligible to monetise." "**You can only monetise clips that feature less than 90 seconds of applicable licensed music selections. Royalty-free audio does not have this restriction.**" Use Royalty-free "if you wish to post reels longer than 90 seconds, if you are using the reel for a commercial purpose such as a brand deal, or if you plan to boost the post." "You may not be eligible for monetisation on music content if you are using full songs or if your reel consists mostly of licensed music." Audio not labelled Licensed or Royalty-free "run[s] the risk of copyright enforcement from other rights holders at a later date."
Source: https://www.facebook.com/business/help/821453195885988 (verified 2026-09-18)

**[OFFICIAL POLICY]** The **Meta Sound Collection** grants "a non-exclusive, royalty-free license to use the SC Audio Content for commercial or non-commercial purposes" on Meta products — but "You may not perform, distribute, make available or otherwise use the SC Audio Content **separately from the Meta Company Products**."
Source: https://www.facebook.com/sound/collection/terms (verified 2026-09-18)

**[INFERENCE]** Meta Sound Collection is the safest audio for monetized Meta video, but its licence does **not** extend to re-uploading the same edit to YouTube or TikTok. Combined with TikTok's equivalent TikTok-only CML licence (§2.2), **a cross-posting pipeline cannot use either platform's free music library on the other platform.** Cross-platform audio must be independently licensed. *Reasoning from the two quoted licence scopes.*

## 3.3 AI-generated / synthetic content

### The label and its history

**[OFFICIAL POLICY]** The current label is **"AI info"**, renamed from "Made with AI" on **1 July 2024** because the earlier labels "weren't always aligned with people's expectations and didn't always provide enough context." From 12 September 2024, "For content that we detect was only modified or edited by AI tools, we are moving the 'AI info' label to the post's menu."
Source: https://about.fb.com/news/2024/04/metas-approach-to-labeling-ai-generated-content-and-manipulated-media/ (verified 2026-09-18)

### Automatic detection

**[OFFICIAL POLICY]** Meta reads **IPTC metadata**, **C2PA** Content Credentials and **invisible watermarks** (including FAIR's Stable Signature), covering generators from Google, OpenAI, Microsoft, Adobe, Midjourney and Shutterstock. The stated limitation, verbatim: "While companies are starting to include signals in their image generators, they haven't started including them in AI tools that generate audio and video at the same scale, **so we can't yet detect those signals**."
Source: https://about.fb.com/news/2024/02/labeling-ai-generated-images-on-facebook-instagram-and-threads/ (verified 2026-09-18)

**[OFFICIAL POLICY]** Meta now also operates **"Content Seal," "our invisible watermarking system,"** surviving cropping, compression, resizing and screenshotting, with "We plan to extend Content Seal to video soon."
Source: https://ai.meta.com/blog/introducing-muse-image-muse-video-msl/ (7 July 2026, verified 2026-09-18)

### What creators MUST disclose

**[OFFICIAL POLICY]** "We require people to disclose, using our AI-disclosure tool, whenever they post organic content with **photorealistic video or realistic-sounding audio** that was digitally created or altered, and we may apply penalties if they fail to do so."
Source: https://transparency.meta.com/policies/community-standards/misinformation/ (verified 2026-09-18)

**[OFFICIAL POLICY]** Examples that must be labelled include a realistic-looking video of people at a market, an audio file of two people talking, "a song created using AI-generated vocals," and — directly relevant here — **"a reel narrated with a realistic AI-generated voiceover."** Images are explicitly exempt from the self-disclosure duty: "Meta does not require you to label images that have been created or modified with AI. Images will still receive a label if Meta's systems detect that they were AI-generated."
Source: https://www.facebook.com/help/7434563519957988 (verified 2026-09-18)

**Could not verify:** the penalty. The entire published statement is "There may be penalties if you do not label content as required." Meta specifies no strike count, duration or distribution effect.

### The AI-generated profile label

**[OFFICIAL POLICY]** A separate label applies to synthetic presenters: the **"AI-generated profile" label** means "that this account regularly posts content featuring an AI-generated person instead of a real human." It appears on the profile, in search, next to Feed/Reels/Stories content, and next to comments and messages. Enforcement is concrete: accounts believed to be AI-generated profiles that don't self-label "may see limits to their account's reach by becoming **ineligible to appear in recommendations** until they've added the profile label." Voluntarily adding it does not reduce reach.
Source: https://www.facebook.com/help/instagram/1555776438852001 (verified 2026-09-18)

**[OFFICIAL POLICY]** Instagram's Recommendations Guidelines separately make not-recommendable "accounts that repeatedly share realistic AI-generated depictions of a person — unless an AI disclosure label has been added."
Source: https://www.facebook.com/help/instagram/313829416281232 (verified 2026-09-18)

### Manipulated media

**[OFFICIAL POLICY]** "**Content Digitally Created or Altered that May Mislead.** For content that does not otherwise violate the Community Standards, we may place an informative label on the face of content – or reject content submitted as an advertisement – when the content is a photorealistic image or video, or realistic sounding audio, that was digitally created or altered and creates a particularly high risk of materially deceiving the public on a matter of public importance." Structurally, this is a **labelling** provision — there is no "we remove" bullet under Manipulated Media, consistent with Meta ceasing manipulated-video removals in July 2024.
Source: https://transparency.meta.com/policies/community-standards/misinformation/ (verified 2026-09-18)

### Can AI content be monetized on Meta?

**[OFFICIAL POLICY — negative finding]** The strings "AI", "artificial intelligence", "AI-generated" and "generative AI" appear **zero times** in all four governing monetization documents: the Facebook Content Monetisation Policies, the Facebook Partner Monetisation Policies, the Facebook Original Content Guidelines, and the Instagram Content Monetisation Policies. AI is also absent from the July 2025 unoriginal-content announcement and its March 2026 successor. There is **no monetization policy hosted on transparency.meta.com at all** — `/policies/monetization/` returns 404.
Sources: https://www.facebook.com/business/help/1348682518563619 ; https://www.facebook.com/business/help/169845596919485 ; https://www.facebook.com/business/help/262834734651607 ; https://www.facebook.com/business/help/2635536099905516 ; https://creators.facebook.com/blog/combating-unoriginal-content/ ; https://about.fb.com/news/2026/03/rewarding-original-creators-on-facebook/ (verified 2026-09-18)

**[INFERENCE]** Meta has **no published ban on monetizing AI-generated content.** The gates are **originality** and **disclosure**, not the use of AI tools. **[INDUSTRY PRACTICE]** The "AI slop crackdown" framing of the July 2025 post (Tubefilter, Social Media Today, TechCrunch) is the press's characterisation, not Meta's wording.

## 3.4 Unoriginal / reused content — the decisive section for Meta

### The announcements

**[OFFICIAL POLICY]** 14 July 2025: "Unoriginal content reuses or repurposes another creator's content repeatedly without crediting them, taking advantage of their creativity and hard work." And the penalty: "Accounts that improperly reuse someone else's videos, photos or text posts repeatedly will not only **lose access to Facebook monetization programs for a period of time**, but will also **receive reduced distribution on everything they share**." Enforcement stats for H1 2025: ~500,000 accounts actioned for spammy behaviour; ~10 million fake profiles impersonating creators removed.
Source: https://creators.facebook.com/blog/combating-unoriginal-content/ (verified 2026-09-18)

*Note: there is no about.fb.com version of this post; that URL 404s.*

**[OFFICIAL POLICY]** 13 March 2026: "Simply watching along, reacting with facial expressions, stitching multiple clips together, or narrating what's already on screen — without adding anything meaningful — is considered unoriginal."
Source: https://about.fb.com/news/2026/03/rewarding-original-creators-on-facebook/ (verified 2026-09-18)

### The binding rule

**[OFFICIAL POLICY]** Partner Monetisation Policies, clause "Share original content": "Content creators, publishers and third-party providers can only monetise content that they created or were involved in the creation of, or that directly features the creator, publisher or third-party provider. Content that is unoriginal or reproduced without making meaningful enhancements (commentary, parody, creative editing etc.) cannot be monetised."
Source: https://www.facebook.com/business/help/169845596919485 (verified 2026-09-18)

**[OFFICIAL POLICY]** On **Instagram** this sits one level higher: **"Unoriginal content"** is a **prohibited category** in the Instagram Content Monetisation Policies, under "The following types of content are ineligible to monetise."
Source: https://www.facebook.com/business/help/2635536099905516 (verified 2026-09-18)

### What IS transformative — verbatim

**[OFFICIAL POLICY]** "Producing content that contains portions of third-party material can still be eligible for Facebook recommendations when you meaningfully enhance the content with additional educational, critical, or entertainment value." Examples of meaningful enhancement:
- "Remixes or overlays that feature **your own on-screen presence as the focus of the video**, and also add new information, commentary, or storyline improvements **beyond simply narrating what happens, making facial expressions, or watching along**."
- "Selectively interspersing third-party clips in content you filmed to tell a new story and/or add relevant critiques, insights, or opinions… **so long as original commentary remains the focus of your video**."
- "For photo memes, text edited onto the image that adds **at least two new pieces of information** or changes the interpretation of the original image. Describing the image with text or in the description alone does not qualify as a meaningful enhancement."

Source: https://www.facebook.com/business/help/262834734651607 (verified 2026-09-18)

### What is NOT transformative — verbatim

**[OFFICIAL POLICY]** "Third-party content that is posted without substantial changes is considered unoriginal and will receive reduced distribution." Two named behaviours:
- **Duplicating content:** "Posting content that already exists on Facebook or that belongs to another creator or entity, with no meaningful role in creating it."
- **Minor editorialization:** "Posting content containing clips or images that the respective Page or profile did not film or produce, with only minor changes." Examples of minor changes:
  - Superimposing borders
  - **Inserting logos or graphics, including watermarks**
  - Adding only an on-screen text caption or title
  - **Adding background music**
  - Changing a video's speed
  - Adding subtitle transcripts
  - "Simply describing (in text, on-screen, **via voiceover** or in the description) what happens in a photo or video with no other meaningful additions"
  - "Reaction videos that merely involve watching along or voiceover without added commentary, information, insights, or perspective."
  - **"Basic compilations that involve splicing third-party clips or photos together without adding your own narrative or new information"**
  - "Inserting an intro or outro without meaningful changes to the meaning or viewing experience of the core content"

Source: https://www.facebook.com/business/help/262834734651607 (verified 2026-09-18)

**[OFFICIAL POLICY]** Critically: "Meta's original content guidelines are separate from the intellectual property policies," and **"This policy can still apply to copyrighted or licensed content."** Owning or licensing the footage does not make it original.
Sources: https://www.facebook.com/business/help/262834734651607 ; https://www.facebook.com/business/help/3382366608650437 (verified 2026-09-18)

### Penalties — with money forfeiture

**[OFFICIAL POLICY]** "In most cases, the Limited Originality of Content violation carries a **90-day demonetization penalty**. Any additional content that doesn't follow this policy during that 90-day period may result in the extension of the demonetization penalty period." Content "will not be recommended to audiences that do not follow your Page or profile," and you may be "ineligible to onboard to new monetization programs during the enforcement period." "Repeated violations may result in permanent removal of eligibility." And a direct warning: **"You should not bulk-delete the existing content on your Page or profile in an attempt to expedite your access to monetization."**

The money clause, verbatim: **"If your Page or profile is demonetized for limited originality, Meta may withhold accrued but unpaid earnings. If your appeal is successful, withheld earnings may be released on the next scheduled payout date. If your appeal is denied or you take no action after 90 days, we may not issue payment to you."**
Source: https://www.facebook.com/business/help/3382366608650437 (verified 2026-09-18)

### Instagram's equivalent — with a hard number

**[OFFICIAL POLICY]** "When we find two or more identical pieces of content on Instagram, we will only recommend the original one." "If we find a copied version of original content, we will add a label linking to the original creator, which will remain visible to followers of the account that copied it." And the numeric rule: **"accounts that repeatedly (10 or more times in the last 30 days) post content from other Instagram users that they didn't create or enhance in a material way will not be shown in surfaces where we recommend content."** Significantly altered versions — memes, parodies, new voiceovers, remixes — are not replaced.
Source: https://creators.instagram.com/blog/recommendations-and-originality (30 April 2024, verified 2026-09-18)

**[OFFICIAL POLICY]** Extended to photos and carousels on 30 April 2026: "accounts that primarily post unoriginal content in photos or carousel posts … will no longer be shown in places where we recommend content." Recovery occurs when "most of their recently posted photos, carousels, and reels are considered original in a 30-day period." Existing followers still receive the content.
Source: https://creators.instagram.com/blog/rewarding-original-creators-on-instagram (verified 2026-09-18)

**[OFFICIAL POLICY]** Instagram's memorable test: "**if someone could remove your contribution to your post or reel, and the content would virtually be the same, it probably needs more of you in it.**"
Source: https://creators.instagram.com/original-content-guidelines (verified 2026-09-18)

## 3.5 Disclosure requirements

**[OFFICIAL POLICY]** "We define branded content as a creator or publisher's content that features or is influenced by a business partner for an exchange of value, such as monetary payment or free gifts." "Branded content may only be posted with the use of the branded content tool, and creators must use the branded content tool to tag the featured third-party product, brand or business partner with their prior permission." And, directly adverse to an outsourced content operation: **"Creators cannot accept anything of value to post content that does not feature themselves or that they were not involved in creating."**
Source: https://www.facebook.com/policies/brandedcontent/ (verified 2026-09-18)

**[OFFICIAL POLICY]** "Exchange of value" explicitly includes monetary payment, free products or gifts, loaned products, **and affiliate commissions earned through marketing links**. The label is not required when posting about your own products.
Source: https://www.facebook.com/help/instagram/616901995832907 (verified 2026-09-18)

**[OFFICIAL POLICY]** Format restrictions on branded content: no pre-/mid-/post-roll ads in branded video or audio; no banner ads in videos or images; no title cards within a video's first three seconds; interstitial/mid/end cards "must not persist for longer than three consecutive seconds."
Source: https://www.facebook.com/policies/brandedcontent/ (verified 2026-09-18)

**[INFERENCE]** A tension Meta does not address: branded content is **ineligible for Instagram bonuses**, so correctly labelling an affiliate post as a paid partnership removes it from bonus payouts. *Reasoning from the two quoted rules.*

**[INDUSTRY PRACTICE]** Practitioner guidance holds that Meta's built-in label alone is insufficient for US FTC purposes and should be paired with an explicit in-caption `#ad`. Not verified against ftc.gov; this is third-party interpretation, not Meta policy.

AI disclosure: see §3.3.

## 3.6 Advertiser-friendly / brand-safety restrictions

**[OFFICIAL POLICY]** The Content Monetisation Policies have four buckets. Framing: "content appropriate for Facebook in general is not necessarily appropriate for monetisation."

**Prohibited formats — "cannot be monetised"** (verified verbatim by direct extraction from the page):
1. **Static videos** — "Content that contains one static image and little to no motion."
2. **Static image polls** — "Content posted for the sole purpose of increasing engagement by asking people to react to questions posed by the content."
3. **Slideshows of images** — "Content that primarily displays static images played in succession."
4. **Looping videos** — "Content that loops and displays the same segment multiple times. Looping content can include GIFs and content of varying lengths."
5. **Text montages** — "Content that primarily displays still or moving images with overlaid text."
6. **Embedded ads** — "Content that already includes embedded pre-roll, mid-roll, post-roll or banner ads where Facebook offers ad placements."

**Prohibited behaviours:** **Engagement bait** — "Content that incentivizes people to click a link or respond to a post through likes, comments or shares"; and soliciting engagement.

**Prohibited categories — "ineligible to monetise":** **Misinformation** — "Content that has been rated false by a third-party fact checker"; **Misleading medical information** — "Content that contains medical claims that have been disproven by an expert organization. Including, but not limited to, anti-vaccination claims."

**Restricted categories — "may face reduced or restricted monetisation":** Debated social issues; Tragedy or conflict; Objectionable activity (including **copyright infringement**); Sexual or suggestive activity; Strong language; Explicit content.

Source: https://www.facebook.com/business/help/1348682518563619 (verified 2026-09-18)

*(Instagram's CMP mirrors this, adding **Unoriginal content** to Prohibited categories and Judicial proceedings to Objectionable activity — https://www.facebook.com/business/help/2635536099905516.)*

**[OFFICIAL POLICY]** Four-tier advertiser rating: "OK for almost all advertisers"; "OK for most advertisers" ("available to fewer advertisers… which can affect your earnings"); "OK for very few advertisers" ("can severely affect your earnings"); "Blocked" ("won't be available to any advertisers and can't be monetised"). Key line: "content which meets our Community Standards and Partner Monetisation Policies may still only be valuable to a small number of advertisers."
Source: https://www.facebook.com/business/help/2279248852143449 (verified 2026-09-18)

**[OFFICIAL POLICY]** Appeals: "An appeal can only be submitted **once** for each demonetised reel." "Appeals are typically reviewed in **seven days or less**."
Source: https://www.facebook.com/business/help/1979171292197867 (verified 2026-09-18)

## 3.7 Major risks for an AI-operated Meta page

All **[INFERENCE]** from the rules quoted above.

1. **The prohibited formats may disqualify the output by construction.** A stock-image-plus-motion-plus-captions pipeline is a **text montage** or **slideshow of images** on its face. These "cannot be monetised" outright, regardless of originality, disclosure or compliance. This is the most underappreciated Meta risk and must be resolved before anything else matters.
2. **Templating is the exact fingerprint of "minor editorialisation."** Meta's non-transformative list reads like a description of a templated pipeline, and the penalty is a **90-day demonetization** that is extendable, escalates to permanent, and can **forfeit accrued but unpaid earnings**.
3. **Licensing does not cure unoriginality.** "This policy can still apply to copyrighted or licensed content."
4. **High-volume crossposting is named in the PMP** as "manufactured sharing" — an **account-level** violation, which is harder to recover from than a content-level one.
5. **Entry is gated by invitation with no published thresholds**, and Meta is actively expanding distribution for original content while demoting the rest.
6. **AI voiceover triggers mandatory disclosure** — Meta's own example is "a reel narrated with a realistic AI-generated voiceover" — and a synthetic presenter triggers the **AI-generated profile label**, without which the account becomes ineligible for recommendations.
7. **Background music is simultaneously a non-transformative edit and a licensing tripwire** (the 90-second rule).
8. **Single-strike exposure at the Community Standards level**, with one appeal per reel.

---

# 4. VIETNAM-MARKET SPECIFICS

This section covers only what differs for a Vietnamese-language channel operated from Vietnam. Where verification was not possible, that is stated rather than filled in.

## 4.1 Monetization availability for a Vietnam-based payee

| Platform | Vietnam status | Evidence |
|---|---|---|
| **YouTube (YPP)** | **Available.** Vietnam appears in YouTube's published YPP availability list, alongside Thailand, the Philippines, Indonesia, Malaysia and Singapore. | **[OFFICIAL POLICY]** https://support.google.com/youtube/answer/7101720 (verified 2026-09-18) |
| **TikTok Creator Rewards** | **NOT available.** The program is limited to exactly eight countries — US, UK, Germany, Japan, South Korea, France, Mexico, Brazil. Vietnam is not among them, and qualified views only count when they come from those eight countries. | **[OFFICIAL POLICY]** https://www.tiktok.com/creator-academy/article/creator-rewards-program (verified 2026-09-18) |
| **TikTok branded content / Shop** | **Available.** Vietnam appears repeatedly in the Branded Content Policy's market-specific lists (e.g. permitted 18+ for alcohol, financial services, gambling and vitamin supplements; prohibited for OTC and prescription medicines). TikTok operates a Vietnam seller portal (seller-vn.tiktok.com). | **[OFFICIAL POLICY]** https://www.tiktok.com/legal/page/global/bc-policy/en (verified 2026-09-18) |
| **Facebook Content Monetization** | **NOT available by country.** Vietnam does **not** appear in Meta's published country/territory list for Facebook Content Monetization ads. **Vietnamese does appear in the supported language list.** | **[OFFICIAL POLICY]** https://www.facebook.com/business/help/267128784014981 (verified 2026-09-18 by direct extraction of the rendered list) |
| **Instagram** | **Could not verify.** No first-party Instagram country-availability list for Gifts, Subscriptions or Bonuses was located that either includes or excludes Vietnam. Not established. | — |

**[INFERENCE] — the single most important Vietnam finding.** A Vietnamese-language channel and a Vietnam-based payee are **different constraints**, and Meta separates them explicitly: Vietnamese is a monetizable *language* while Vietnam is not a monetizable *country*. The published rules therefore permit Vietnamese-language content monetized by an entity resident in a listed country, but not by a Vietnam-resident payee. The same logic applies to TikTok Creator Rewards, which additionally requires "an account registered there" and a payment account in the creator's own name — so relocating the payee alone is insufficient, and using a VPN to appear in-country is separately prohibited by the Creator Code of Conduct (§2.6). *This is reasoning from the quoted country/language lists; no platform states the combination explicitly.*

## 4.2 Payment and tax for a Vietnam-based payee

**[OFFICIAL POLICY — Vietnamese law, third-party summarised]** Vietnam taxes individual business revenue above **VND 100 million per year**, at **5% VAT plus 2% personal income tax on taxable revenue** for digital information content services. The framework is Circular **40/2021/TT-BTC** (issued 1 June 2021), which expressly covers individuals earning from e-commerce and "providing digital information content products and services."
Sources: https://assets.kpmg.com/content/dam/kpmg/vn/pdf/tax-alert/2021/6/TA-Circular-40-on-VAT-PIT-of-business-individuals-EN.pdf ; https://vietnam.acclime.com/news-insights/circular-40-guiding-vat-pit-and-tax-administration-for-business-households-and-business-individuals/ (verified 2026-09-18)

**Sourcing caveat:** these are professional-firm summaries (KPMG Vietnam, Acclime), not the Ministry of Finance's own English text. No official MOF English-language source for Circular 40 was retrieved, and it has **not** been verified whether the rates or the VND 100 million threshold have been amended since 2021 — note that Vietnam enacted new Tax Administration, PIT and VAT laws with 2026 effect, which were not reviewed for this dossier. **Treat the rates as indicative and obtain Vietnamese tax advice before relying on them.**

**[INDUSTRY PRACTICE]** Reported practice distinguishes creators paid via a Vietnamese partner/MCN of Google, Facebook or TikTok (the organisation withholds and declares) from creators paid directly by the foreign platform (the individual self-declares). Third-party sourced; not verified first-party.

**Could not verify:** Google's AdSense tax-withholding treatment for a Vietnam-resident payee (the US tax-info requirement under Chapter 3 withholding, and whether the US–Vietnam position produces a reduced treaty rate on US-sourced viewership earnings). The relevant AdSense help page was not located and confirmed. This is a material commercial variable and should be resolved directly.

## 4.3 Local content regulation

**[OFFICIAL POLICY — Vietnamese law, third-party summarised]** **Decree 147/2024/ND-CP** on the management, provision and use of internet services and online information was issued **9 November 2024** and took effect **25 December 2024**, replacing Decree 72/2013/ND-CP. Key provisions:
- A cross-border provider is in scope if it leases data storage in Vietnam **or** receives **100,000 or more total visits per month from Vietnam for six consecutive months**.
- In-scope providers must notify the Authority of Broadcasting and Electronic Information (ABEI) of contact details and main server location **within 60 days** of crossing the threshold.
- Violating content must be removed **within 24 hours** of an authority request; user complaints handled **within 48 hours**.
- Social network providers must **authenticate user accounts via Vietnamese mobile phone number or ID number**, and "only verified accounts can post information (write posts or comments, livestream)." Account verification via phone or ID is required where the livestream feature is used for commercial purposes.

Sources: https://www.tilleke.com/insights/a-closer-look-at-vietnams-decree-147-on-internet-services-and-online-information/ ; https://www.vietnam-briefing.com/news/vietnams-new-internet-regulation-decree-147-2024.html/ ; https://assets.kpmg.com/content/dam/kpmg/vn/pdf/Legal-Update/2025/02/decree-147-on-internet-services-en.pdf (verified 2026-09-18)

**Sourcing caveat:** these are law-firm and professional-services analyses (Tilleke & Gibbins, KPMG Vietnam, Vietnam Briefing) of a named decree with a stated number and effective date — clearly authoritative as secondary sources, but **not** the official Vietnamese-government text, which was not retrieved in English. Article numbers were not independently confirmed.

**[OFFICIAL POLICY — as reported by the above sources]** The obligations of Decree 147 fall principally on **platforms**, not on individual creators. The Tilleke analysis states it "does not explicitly impose separate obligations on individual content creators or publishers."

**[INFERENCE]** The indirect effect on an AI-operated Vietnamese channel is nonetheless real: because platforms must verify accounts against a Vietnamese phone number or ID before the account can post or livestream, **an automated publishing pipeline operating Vietnamese-facing accounts will be bound to identified natural persons.** That constrains a "many disposable accounts" architecture more than platform policy alone would. *Reasoning from the quoted verification requirement.*

**Could not verify:** Vietnamese advertising-law disclosure obligations for sponsored or affiliate content (Law on Advertising and its amendments), and whether any Vietnamese rule specifically addresses AI-generated or synthetic media disclosure. These were not established and are not asserted. **Local counsel should confirm the current position before launch** — this dossier covers platform policy, not Vietnamese media law.

---

# 5. PLATFORM POLICY MATRIX

*Every cell below is drawn from the first-party sources cited in §1–§4 and carries the same verification date. The Source column lists the governing pages; full URLs appear in the body sections.*

| Platform | Monetization Requirements | Copyright Rules | AI Content Rules | Reused Content Rules | Disclosure Requirements | Advertiser Restrictions | Major Risks | Source | Last Verified |
|---|---|---|---|---|---|---|---|---|---|
| **YouTube (long-form)** | Tier 1: 500 subs + 3 public uploads/90d + (3,000 watch hrs/12mo OR 3M Shorts views/90d) → fan funding only. Tier 2: 1,000 subs + (4,000 watch hrs/12mo OR 10M Shorts views/90d) → ads + Premium. **From 1 Feb 2027 new entrants need 1,000 subs + (8,000 watch hrs/365d OR 20M Shorts views/90d).** Existing partners' membership grandfathered. New activity floor applying to all: 1,000 watch hrs/365d OR 1M Shorts views/90d OR 2 long-form / 5 Shorts per 90d. Terms must be accepted by 31 Jan 2027. Requires eligible country, AdSense, 2-step verification, no active CG strikes. Vietnam eligible. | Content ID claim (revenue to claimant, normally no strike) is separate from a copyright strike (legal removal). 3 strikes in 90d terminates the account "along with any associated channels." Strikes expire 90d after Copyright School. Dispute → claimant has 30d; appeal → 7d; claimant may escalate to takedown = strike. Repeated dispute abuse penalised. Must hold written commercial-use rights to all audio and visual elements. | AI allowed; originality is the test, not the tool. Must disclose realistic altered/synthetic content. Not required for: unrealistic content, minor edits, AI scripts/captions/thumbnails, **cloning one's own voice**. Label in expanded description; prominent player label for health, news, elections, finance. Auto-applied via C2PA 2.1+ and YouTube's own tools; auto-labels cannot be removed. **Disclosure does not affect reach or earnings.** Consistent non-disclosure → content removal or YPP suspension. **AI personas delivering health, legal, finance or political information: the channel cannot monetize.** | Two separate live policies. **Inauthentic content** (renamed from "repetitious content" 15 Jul 2025) — content must "Be your original creation" and "Not be mass-produced, generic, repetitive, or manipulative"; explicitly bans "AI-generated content made with generic or unoriginal templates giving the impression of mass production." **Reused content** — bans repurposing without significant original commentary. ALLOWED: critical review, reaction with commentary, edited footage with added storyline and commentary, "using AI to visualize a unique character and narrative you invented." NOT: clips with little narrative, non-verbal reactions, "readings of other materials you did not originally create." | Paid promotion checkbox in Studio → disclosure label at video start. Branded Content Policy: prohibited (drugs, weapons, hacking software, counterfeits, essay services); restricted, requiring Google certification of the brand (alcohol, financial services, healthcare, gambling, elections). AI disclosure via the Studio "altered or synthetic content" attribute. **Affiliate links: not addressed on any first-party page located.** | 14 categories limit or remove ad revenue: inappropriate language; violence; adult; shocking; harmful/unreliable; hateful; drugs; firearms; controversial issues; sensitive events; enabling dishonest behavior; inappropriate for kids/families; incendiary and demeaning; tobacco. Green/yellow/red states. Self-certification inaccuracy can trigger a YPP eligibility review. One appeal per video, ≤7 days, decision final. | Channel-level demonetization for templated AI output; "related channels" contagion across a network; spam-policy escalation to termination rather than mere demonetization; sensitive-topic AI personas; auto-disputing Content ID claims converting revenue loss into strikes; the 2027 Shorts double gate. | support.google.com/youtube/answer/ 72851, 12843009, 1311392, 14328491, 15447836, 6162278, 2814000, 2797370, 2797454, 2490020, 2801973, 2802032, 1727191, 7101720, 13429240, 9914702, 7687980, 7083671, 12504220, 154235, 17596007, 16440338, 10834785; blog.youtube (2027 YPP post, 10 Aug 2026; AI disclosure post, 18 Mar 2024) | 2026-09-18 |
| **YouTube Shorts** | Same YPP tiers as above. Shorts-specific: **from 1 Feb 2027, a continuously re-tested 10M qualified Shorts views/90d is required to earn from the Shorts Creator Pool — and this applies to existing partners too, not only new entrants.** Falling below does not remove you from YPP and does not affect long-form earnings; Shorts revenue sharing resumes automatically on re-crossing. Revenue split: Shorts 45% of the distributed pool, long-form 55%. Targeted Shorts ads placed to five or fewer channels carry a direct 45% share. | As long-form. | As long-form. | As long-form, plus Shorts-specific view ineligibility: "Non-original Shorts, such as unedited clips from others' movies or TV shows"; "reuploading other creators' content from YouTube or other platform"; "compilations with no original content added"; artificial or bot views; views inconsistent with advertiser-friendly guidelines. | As long-form. | As long-form. | Views can be individually ruled ineligible, so headline view counts may materially overstate progress toward the 10M/20M thresholds. A channel can sit inside YPP and earn nothing from Shorts. | support.google.com/youtube/answer/12504220, 12843009 | 2026-09-18 |
| **TikTok** | **Creator Rewards Program:** 18+ (19 in South Korea); **Personal Account only** (Business, political and government accounts ineligible); **10,000 followers**; **100,000 video views in the last 30 days**; account in good standing; authentic account info; public account; videos **at least 1 minute**. **Only eight countries: US, UK, Germany, Japan, South Korea, France, Mexico, Brazil — Vietnam excluded**, and qualified views only count from those eight. Per video: ≥1,000 qualified For You feed views; not a Duet or Stitch; not Photo Mode; not an ad, paid promotion or sponsored content. Rewards = Standard (qualified views × RPM) + Additional. Shop Affiliate: 1,000 followers, US. LIVE: 1,000 followers. | IP Policy effective 26 Apr 2025. A copyright-specific strike system runs separately from trademark; strikes expire after 90 days; "There is a strike limit for each IP type, after which we'll permanently remove the account" — **the number 3 appears only inside TikTok's own worked example, not as a published rule.** Discretionary repeat-infringer ban; immediate ban for severe violations. Counter-notification forwarded in full to the claimant; reinstatement at TikTok's sole discretion; **no published day count**. Rewards on removed videos are clawed back. **Music: "Commercial Sounds are the only sounds made available on TikTok for Commercial Uses"; businesses cannot use the general library; the CML licence is TikTok-only.** Copyrighted music running over 1 minute makes content non-original and risks muting. | AI allowed. Must label AIGC or significantly edited content showing **realistic** people or scenes. **Explicitly NOT required for: artistic styles such as anime; "generic text-to-speech (TTS) narration, when the TTS isn't a recognizable voice of a known individual"; minor edits.** Two labels: manual "Creator labeled as AI-generated" and automatic "AI-generated" applied via **C2PA Content Credentials** and invisible watermarking. Labels are **irreversible** after posting. Labelling does not affect distribution. Mislabelling non-AI content is itself a ToS violation. Unlabelled realistic AIGC "may be removed, restricted, or labeled." Prohibited even when labelled: private-figure likeness without consent, fake news-source framing, crisis events, fabricated public-figure endorsements. **ToS §3.10 bans automated use of TikTok's own generative AI features and bans removing or altering watermarks and content-authenticating metadata.** No first-party rule makes AIGC categorically ineligible for Creator Rewards. | **Originality Policy:** unoriginal = copied completely; largely repurposed without creative edits (subtitles-only, basic text summaries); combined from multiple sources without added value (splicing only); **containing someone else's visible watermark or logo**. Unoriginal content is For You feed–ineligible **and "ineligible for TikTok's monetization programs like the Creator Rewards Program, which uses 'originality' as a key metric in its rewards formula."** The CRP non-original list adds: Duet/Stitch; "slight modifications, including videos that are sped up or contain filters, fixed texts, or stickers"; "looping videos, single or multiple photos, or only text overlays"; lip syncs. **Hard thresholds: 5 video violations in 30 days → disqualification; 5 total account violations → permanently ineligible.** Account-level FYF suppression is possible **with no violation at all**. ALLOWED per TikTok: appear onscreen, **add your own voice-overs**, add background explanation, restructure with real editing. | Content disclosure toggle mandatory for commercial content. "Your brand" → viewers see "Promotional content"; "Branded content" → "Paid partnership". Label cannot be changed after posting; the toggle does not affect distribution. **Branded Content Policy (published 4 Aug 2026, effective 31 Aug 2026) expressly covers affiliate links and promo codes.** The product must be identifiable without clicking through. 15 prohibited industries; 11 restricted (invitation from an approved brand, via TikTok One only). From 24 Sept 2026 undisclosed commercial content moves from FYF-ineligible to "we may reduce its visibility"; the account-ban risk for repeated failure is unchanged. | **Inventory Filter: Expanded / Standard / Limited** across 14 categories (military; terrorism; disrespectful religion and culture; adult sexual; drugs; illegal services; privacy infringement; weapons; obscenity and profanity; sensational and shocking; piracy and infringement; discriminatory; tobacco; alcohol; political). Category Exclusion (4 categories), Vertical Sensitivity (11 verticals), Video Exclusion List (200,000 IDs), Profile Feed Exclusion List (500 usernames). **No For You feed category named "mass-produced" or "AI slop" exists.** | The 5-violation cliff, permanent at account level; account-level FYF suppression with no violation and no appeal trigger; originality scored into RPM so revenue decays before any flag; the Photo Mode / slideshow trap; **multiple-account abuse and VPN use are named prohibitions**; the Business Account vs Creator Rewards forced choice; irreversible labelling decisions. | tiktok.com/support/faq_detail?id= 7581821550694013452, 7543604786688563768, 7636670084747893268, 7636670088170904085; tiktok.com/creator-academy/article/ creator-rewards-program, tiktok-originality-policy, creator-code-of-conduct, content-check-lite; tiktok.com/legal/page/global/ copyright-policy/en, bc-policy/en, commercial-music-library-user-terms/en; tiktok.com/legal/page/us/terms-of-service/en; tiktok.com/safety/en/policies-and-engagement/ integrity-authenticity, regulated-commercial-activities, fyf-standards, accounts-features; tiktok.com/tns-inapp/pages/ai-generated-content; ads.tiktok.com/help/article/ tiktok-inventory-filter, commercial-music-library, about-brand-safety-hub; newsroom.tiktok.com (C2PA, 9 May 2024; watermarking, 19 Nov 2025) | 2026-09-18 |
| **Facebook** | Legacy In-stream Ads, Ads on Reels and the Performance Bonus **ended 31 Aug 2025**; In-stream ads for Live ended **15 Jun 2026**. **Facebook Content Monetization is invite-only with NO published numeric thresholds.** The only published numbers: established presence ≥30 days; reels ≥10s; Stories ≥5s; watch time under 5s does not qualify. Creator Fast Track (18 Mar 2026): $1,000/mo at 100K+ followers on another platform, $3,000/mo at 1M+. Two policy layers — PMP (account level) and CMP (content level). Published country list: **Vietnam absent; Vietnamese present as a supported language.** | Rights Manager matches video, audio, images and Live across Facebook and Instagram; rights holders may block, claim ad earnings, monitor or report. Audible Magic handles audio matching. A claimant may take earnings, and payouts are withheld during review. **No published numeric copyright strike counter** — a discretionary repeat-infringer policy ("account may be disabled, Page may be removed"). The numeric 1–10+ strike ladder is the Community Standards system and does not mention IP. Counter-notification restoration "up to 14 working days"; restored content is not counted under the repeat-infringer policy. Music: commercial or non-personal use prohibited without licence; **only clips with under 90 seconds of licensed music can be monetized**; Royalty-free and Meta Sound Collection are exempt but licensed **for Meta products only**. | "AI info" label, renamed from "Made with AI" on 1 Jul 2024. **Must self-disclose photorealistic video or realistic-sounding audio** — Meta's own example includes **"a reel narrated with a realistic AI-generated voiceover."** **Images are exempt from the disclosure duty** but are still auto-labelled if detected. Auto-detection via IPTC, C2PA and invisible watermarks, plus Meta's own "Content Seal"; Meta states it **cannot yet detect third-party AI audio and video signals at scale**. AI-edited-only content has its label moved into the three-dot menu. Manipulated Media is a **labelling**, not a removal, provision. Penalty for non-disclosure published only as "There may be penalties." **AI appears zero times across all four monetization policy documents — there is no published ban on monetizing AI content.** | July 2025 and March 2026 announcements. PMP clause "Share original content": you may only monetize content "that they created or were involved in the creation of, or that directly features" you; "Content that is unoriginal or reproduced without making meaningful enhancements (commentary, parody, creative editing etc.) cannot be monetised." TRANSFORMATIVE: on-screen presence as the focus plus new information or commentary **beyond narrating, facial expressions or watching along**; interspersing third-party clips in footage you filmed with original commentary as the focus; photo memes adding **at least two new pieces of information**. NOT transformative: borders; logos or **watermarks**; on-screen captions; **background music**; speed changes; subtitle transcripts; **voiceover that merely describes**; watch-along reactions; **basic compilations of spliced clips**; intro/outro only. **"This policy can still apply to copyrighted or licensed content."** Penalty: **90-day demonetization**, extendable, permanent on repetition, non-recommendable, and **accrued unpaid earnings may be withheld and never paid**. Do not bulk-delete to speed recovery. | Branded content tool mandatory; "exchange of value" includes gifts, loaned products **and affiliate commissions**. **"Creators cannot accept anything of value to post content that does not feature themselves or that they were not involved in creating."** Extensive prohibited and restricted industry lists; several require written Meta pre-authorisation. Format limits: no embedded ads; no title cards in the first 3 seconds; interstitials ≤3 seconds. AI disclosure as above. | CMP **prohibited formats that cannot be monetized: static videos; static image polls; slideshows of images; looping videos; text montages; embedded ads.** Prohibited behaviours: engagement bait; soliciting engagement. Prohibited categories: misinformation; misleading medical information. Restricted: debated social issues; tragedy or conflict; objectionable activity (including **copyright infringement**); sexual or suggestive activity; strong language; explicit content. Four-tier advertiser rating down to "Blocked." One appeal per reel, reviewed in ≤7 days. | **The prohibited formats may disqualify templated AI video by construction** (text montage / slideshow / static video); templating matches "minor editorialisation" almost exactly; licensing does not cure unoriginality; high-volume crossposting is an account-level PMP violation; entry is invite-only with no published thresholds; AI voiceover triggers mandatory disclosure; background music is both a non-transformative edit and a licensing tripwire; a single Community Standards violation can remove monetization. | facebook.com/business/help/ 1049081556813520, 169845596919485, 1348682518563619, 262834734651607, 3382366608650437, 267128784014981, 1979171292197867, 185404538833362, 2279248852143449, 821453195885988; facebook.com/policies/brandedcontent/; facebook.com/legal/music_guidelines; facebook.com/sound/collection/terms; facebook.com/help/ 7434563519957988, 325058084212425, 265723950293778, 350712395302528, 225191540826940, 1900735080058381; transparency.meta.com/policies/community-standards/misinformation/, /enforcement/taking-action/counting-strikes/, /reports/intellectual-property/…; about.fb.com (Feb 2024, Apr 2024, Mar 2026 originality, Mar 2026 Fast Track); creators.facebook.com/blog/combating-unoriginal-content/ | 2026-09-18 |
| **Instagram** | **No in-stream or pre-roll ad revenue share exists.** Gifts/Stars: **500 followers**, 18+, professional account ($0.01 per Star). Subscriptions: **10,000 followers**. Creator Marketplace: **1,000 followers**. Bonuses: invite-only (Reels bonus = 5M views across 3 consecutive months in Japan, South Korea, US); **"For reels rewards, only original newly created content counts"**; branded content is ineligible for bonuses. Affiliate relaunched 24 Mar 2026 with no published thresholds. Vietnam availability **could not be verified**. | Rights Manager and the repeat-infringer policy operate on Instagram as on Facebook, with the same counter-notification path and the same absence of a published numeric IP strike count. | The same Meta "AI info" regime. Plus the **"AI-generated profile" label** for accounts that "regularly post content featuring an AI-generated person instead of a real human" — accounts that do not self-label "may see limits to their account's reach by becoming ineligible to appear in recommendations until they've added the profile label." The Recommendations Guidelines separately make not-recommendable "accounts that repeatedly share realistic AI-generated depictions of a person — unless an AI disclosure label has been added." | **"Unoriginal content" is a PROHIBITED CATEGORY in Instagram's Content Monetisation Policies** — ineligible to monetize, one level stricter than Facebook's placement of the same rule in the PMP. "When we find two or more identical pieces of content on Instagram, we will only recommend the original one," with a label linking to the original creator. The hard numeric rule: **"accounts that repeatedly (10 or more times in the last 30 days) post content from other Instagram users that they didn't create or enhance in a material way will not be shown in surfaces where we recommend content."** Extended to photos and carousels 30 Apr 2026. Recovery when most of the last 30 days' posts are original. The test: "if someone could remove your contribution to your post or reel, and the content would virtually be the same, it probably needs more of you in it." | Paid partnership label mandatory; the same "exchange of value" definition including affiliate commissions. Violating branded posts are removed, with review requestable within 24–48 hours. Creators "may lose the ability to monetize" if a connected entity repeatedly violates. | Instagram's CMP mirrors Facebook's, **adding Unoriginal content to the Prohibited categories** and Judicial proceedings to Objectionable activity. | The 10-posts-in-30-days recommendation cutoff is the tightest published numeric originality rule on any platform in this dossier; AI-persona accounts must self-label or lose all recommendation surfaces; with no ad-revenue-share product, revenue depends entirely on gifts, subscriptions, brand deals and invite-only bonuses. | facebook.com/business/help/2635536099905516; facebook.com/help/instagram/ 738469380549477, 478012211024479, 1389278101788752, 708013994693013, 434406642308284, 616901995832907, 313829416281232, 1555776438852001, 366220201089101, 1586774981367195; creators.instagram.com/blog/recommendations-and-originality, /blog/rewarding-original-creators-on-instagram, /original-content-guidelines, /blog/new-ways-to-earn-making-reels-shoppable; facebook.com/legal/stars_terms | 2026-09-18 |

---

# 6. WHAT THIS MEANS FOR AN AI-OPERATED CHANNEL

Everything in this section is **[INFERENCE]** — design conclusions drawn from the first-party rules quoted above. None of it is platform policy.

## 6.1 The one-sentence version

No platform bans AI. **Every platform bans the thing that cheap AI pipelines actually produce.** The binding constraint is never "was this generated?" — it is "is there a specific, non-interchangeable creative contribution that a reviewer can point to, and would the video be materially different without it?"

## 6.2 What makes AI content monetizable versus not

The four platforms converge on the same test from different directions. Read together:

| Platform | Its own phrasing of the test |
|---|---|
| YouTube | "Content that expresses your unique creative voice, like using AI to visualize a unique character and narrative you invented" — versus "generic or unoriginal templates giving the impression of mass production." |
| TikTok | "designed, filmed, and produced by you that showcases your expertise, talent, or creativity" — versus "minimal original input or edits." |
| Facebook | Enhancement must go "beyond simply narrating what happens, making facial expressions, or watching along." |
| Instagram | "If someone could remove your contribution to your post or reel, and the content would virtually be the same, it probably needs more of you in it." |

**Monetizable AI content has:** an invented premise, character or narrative specific to that video; commentary or analysis that is the *focus* rather than a layer over someone else's footage; per-video creative variation a human reviewer would recognise as deliberate; and source material the operator either created or holds written commercial-use rights to.

**Non-monetizable AI content has:** a fixed visual template with only the script varying; third-party footage with narration that describes rather than argues; slideshows, text montages, looping clips or static images; another platform's watermark; and no identifiable authorial position.

**The critical trap.** Three of the four platforms treat *licensed* third-party footage as still unoriginal. Meta says so explicitly: "This policy can still apply to copyrighted or licensed content." Buying a stock licence solves copyright and does **nothing** for originality. **These are two independent gates and the pipeline must pass both.** A team that budgets for stock licensing and assumes it has solved the reused-content problem has solved neither half of it.

## 6.3 A per-platform publishing gate

A pipeline should refuse to publish unless the checks below pass. Each is derived from a quoted rule.

**All platforms — block on:**
- A third-party watermark or logo visible anywhere in frame.
- A source asset lacking recorded written commercial-use rights.
- Output that is a slideshow, a static image with token motion, a looping segment, or a text-over-image montage.
- No per-video creative artefact (invented premise, argument or character) recorded in metadata.

**YouTube — additionally:**
- Set the "altered or synthetic content" attribute correctly. Realism about real people, places or events triggers it; cloning **your own** voice does not.
- Drive self-certification from real content classification, never a hardcoded "None of the above" — repeated inaccuracy triggers a YPP eligibility review.
- **Hard block:** any synthetic persona presenting as a human expert on health, legal, finance or politics. Channel-fatal, not video-level.
- Never auto-dispute Content ID claims. Disputes must be human-reviewed, because escalation converts a revenue loss into a channel-terminating strike.

**TikTok — additionally:**
- Video ≥1 minute, ≥1080p, not a Duet or Stitch, not Photo Mode.
- Copyrighted music must not run past 1 minute.
- Label only if the output shows realistic people or scenes; stylised visuals plus generic TTS need no label. The decision is irreversible after posting, so it must be correct pre-publish.
- Account type must be Personal, and the operator must genuinely be resident in one of the eight eligible countries. No VPN geo-positioning.
- Track a rolling 30-day violation count and halt publishing at 3, to stay clear of the 5-violation cliff.

**Meta — additionally:**
- Disclose if the reel contains photorealistic video **or a realistic AI voiceover**. This catches most templated pipelines.
- If a synthetic presenter is used, apply the AI-generated profile label voluntarily — it costs no reach, and its absence costs all recommendation surfaces.
- Keep licensed music under 90 seconds, or use Royalty-free / Meta Sound Collection — and never reuse that audio off-platform.
- Do not crosspost the same asset at high volume across owned properties; that is "manufactured sharing" at the account level.

## 6.4 Channel-level (fatal) versus video-level (recoverable)

This distinction should drive the architecture more than any threshold number.

**Channel/account-level — fatal or near-fatal:**
- **YouTube:** inauthentic-content and reused-content enforcement (YPP suspension, 21-day appeal, 90-day re-application wait); AI personas on sensitive topics; 3 copyright strikes in 90 days (terminates the channel **and associated channels**); spam-policy termination.
- **TikTok:** 5 account violations = **permanently ineligible** for Creator Rewards; account-level For You feed suppression **with no violation and no appeal trigger**; IP repeat-infringer ban; Creator Code of Conduct restriction for multiple-account abuse or VPN use (three months, or permanent).
- **Facebook:** 90-day demonetization for limited originality — extendable, permanent on repetition, and **with forfeiture of accrued unpaid earnings**; a single Community Standards violation can remove monetization access; PMP violations are account-level by definition.
- **Instagram:** 10+ unoriginal reposts in 30 days removes the account from all recommendation surfaces.

**Video-level — recoverable:**
- YouTube: yellow-icon limited ads (one appeal, ≤7 days, then final); individual Content ID claims; individual Shorts views ruled ineligible.
- TikTok: individual video disqualification from rewards (appealable); a muted video.
- Facebook/Instagram: an individual reel demonetized (one appeal, ≤7 days).

**The operational consequence.** Video-level failures are a cost of doing business and can be managed statistically. Channel-level failures are existential, and they are almost always reached by *accumulation* of video-level failures. Because an automated pipeline produces **correlated** errors — a template defect affects every output, not one — the realistic failure mode is crossing a channel-level threshold within days. **Publishing velocity is itself a risk variable.** The pipeline needs a circuit breaker keyed to rolling violation counts, not merely a pre-publish content check.

## 6.5 Structural consequences for a multi-channel design

- **Channel isolation is a corporate-structure question, not a technical one.** YouTube allows many channels per AdSense account but only one AdSense account per payee name, and tells suspended operators not to "apply to YPP with related channels." Genuine isolation therefore requires distinct legal payees, decided before launch because AdSense country and payee are hard to change later.
- **TikTok forces a binary choice** between Business Account (full Commercial Music Library access) and Personal Account (Creator Rewards eligibility). Decide before scaling.
- **Cross-posting is penalised three separate ways:** Meta's "high-volume crossposting" clause; YouTube's and TikTok's reused-content rules (a TikTok watermark makes a YouTube Short non-original, and vice versa); and the fact that neither Meta's nor TikTok's free music library is licensed for use on the other platform. **A cross-platform pipeline needs platform-native renders with independently licensed audio, not one master re-uploaded four times.**
- **The originality cures are not portable.** TikTok names "adding your own voice-overs" as a remedy for unoriginality; Meta names voiceover that merely narrates as explicitly *not* a remedy. The same asset can be original on one platform and unoriginal on another — so compliance must be evaluated per platform, not once.
- **Geography is a first-order constraint, not an afterthought.** For a Vietnam-based operation, YouTube is the only one of the four with confirmed first-party monetization availability for a Vietnam-resident payee (§4.1).

---

# 7. CONTRADICTIONS AND GAPS

## 7.1 Where first-party sources disagree with each other

| Topic | Source A | Source B | Assessment |
|---|---|---|---|
| YouTube 2027 entry threshold | blog.youtube: "New creators applying for YPP will need 8,000 qualified watch hours in the last 365 days, or 20 million qualified Shorts views" | support.google.com/youtube/answer/12843009 adds "**in addition to still needing 1k subscribers**" | The support page is the more complete statement. The blog omits the subscriber requirement, which is plausibly how some coverage came to report the change as subscriber-free. |
| YouTube 500-sub tier restatement | answer/13429240: 500 subs **+ 3 valid public uploads in the last 90 days** + (3,000 watch hrs OR 3M Shorts views) | answer/12843009 restates the unchanged tier as "500 subscribers and either 3,000 qualified watch hours… or 3M qualified Shorts views" — **omitting the 3-uploads requirement** | Treat answer/13429240 as authoritative; the 2027 page carries an abbreviated restatement. |
| YouTube "grandfathering" | blog.youtube and support page: "This update won't impact creators already in YPP" / "your status is not impacted" | The same support page imposes a 10M/90d Shorts floor "to earn each month from the Shorts Creator Pool" and a new activity requirement, neither limited to new entrants | **Not a contradiction so much as a widely-misread scope.** Grandfathering covers YPP *membership*, not Shorts revenue sharing and not the activity floor. Most third-party coverage collapses these. |
| TikTok CRP payout threshold | Help Centre: **$10 USD** | US legal terms and Creator Academy: **$50.00 USD** | Unresolved. For US creators the July 2026 legal terms are the safer authority. |
| TikTok CRP appeal window | Help Centre: **30 days** for all disqualifications | Creator Academy (two articles): **80 days** for video, 30 days for account | Unresolved. Assume 30 days to be safe. |
| TikTok Commercial Music Library size | ads.tiktok.com: "1 million songs" | Creator Academy: "over 1.5 million tracks" | Immaterial in itself, but it indicates the two page families are not maintained together. |
| TikTok CML access | ads.tiktok.com: Business Accounts see **only** the CML | Creator Academy: "All TikTok users have access to the CML" | Unresolved and **material** to the Business-vs-Personal account decision. |
| TikTok undisclosed commercial content, from 24 Sept 2026 | Regulated Commercial Activities (v2026H2): "we may reduce its visibility" | Accounts & Features (v2026H2) still says "remove it from the FYF"; ads.tiktok.com help still describes a 24-hour window then FYF-ineligibility | The v2026H2 sections have not been harmonised. **Do not rely on the apparent softening.** |
| TikTok Shop affiliate follower minimum | Creator Eligibility Policy: **1,000 followers** | Pilot Program page calls **5,000** the "traditional requirement" | Most consistent reading: 1,000 to bind as an affiliate, under 5,000 places you in the restricted Pilot. Neither page states this reconciliation. |
| Meta Subscriptions fee | facebook.com/business/help/310335859546716: "Meta will not take any fees from Subscriptions until the end of 2025" | No successor statement located | The page is stale on its face. Current fee status **could not be verified**. |

## 7.2 Where a policy is vague or undefined

- **YouTube does not define "related channels."** The monetization policy says not to "apply to YPP with related channels during your suspension period," but never states whether shared ownership, shared AdSense, shared infrastructure or merely shared content style makes channels related. **This is the single largest undefined risk for a multi-channel operator** and it cannot be resolved from published sources.
- **No platform publishes a numeric tolerance for copyright *claims*** before channel-level demonetization. YouTube publishes strike thresholds but not claim thresholds; TikTok says "there is a strike limit for each IP type" without stating it as a rule; Meta publishes no IP strike number at all.
- **Meta publishes no penalty schedule for undisclosed AI content.** The complete published statement is "There may be penalties if you do not label content as required."
- **Meta publishes no numeric eligibility thresholds** for Facebook Content Monetization. Invitation criteria appear to be opaque by design.
- **"Meaningful enhancement," "significant original commentary," "creative edits" and "unique creative voice"** are judgement standards with worked examples but no bright line. Every platform reserves discretion, and no automated pre-publish check can fully anticipate a human reviewer.
- **TikTok's account-level FYF suppression clause** describes an outcome ("we may make the account and its content ineligible for the FYF") with no stated ratio, threshold, notification or appeal route.
- **YouTube does not state whether "qualified Shorts views"** — the YPP threshold metric — uses the same exclusion list as "eligible Creator Pool views." The two are described on different pages in different words, so it is **not established** whether views on reused Shorts count toward the 10M/20M thresholds. For a Shorts-led plan this is a material planning gap.

## 7.3 What could not be established

Explicitly unverified. None of these should be treated as known.

1. **Whether YouTube monetization suspension propagates across channels sharing an AdSense account.** Creator-forum reports describe exactly this (YouTube Community thread "Suspended due to related channel — YPP blocked across all channels linked to AdSense," April 2026). **This remains unconfirmed third-party signal, not policy, and is not upgraded anywhere in this dossier.** YouTube's own pages state only the one-AdSense-account-per-payee rule and the "related channels" warning.
2. **The consequence of not accepting YouTube's updated terms by 31 Jan 2027.** Search snippets state that earnings stop from 1 Feb 2027; that sentence was not confirmed in the page body of either first-party source.
3. **YouTube's four-category prominent-label list** (health, news, elections, finance) is carried by the March 2024 blog post but is **not restated** in the current Help Centre pages fetched.
4. **Whether TikTok AIGC affects Creator Rewards eligibility.** Five first-party pages were checked; none mentions AI. Exposure runs through the originality rules, not an AI-specific rule.
5. **Any TikTok Pulse or Pulse Premiere revenue-share percentage.** No first-party page states one; the 50/50 figure is 2022 trade press only. Whether a creator-side Pulse program still operates at all could not be established.
6. **TikTok's numeric copyright strike limit**, and its general Community Guidelines strike threshold. Neither is published as a rule.
7. **TikTok's counter-notification restoration timeline.** No day count is published; "10–14 business days" is the DMCA statutory default, not a TikTok statement.
8. **Whether Meta's numeric Community Standards strike ladder applies to IP removals.** Meta does not say either way.
9. **Meta's Rights Manager ad-earnings split** and any claim window — third-party snippets only.
10. **Whether Music Revenue Sharing's historical 20% creator split survives** into Facebook Content Monetization, following the end of in-stream ads on 31 Aug 2025.
11. **Instagram monetization availability in Vietnam.** No first-party country list was located.
12. **Google AdSense tax-withholding treatment for a Vietnam-resident payee**, including any US treaty position on US-sourced viewership earnings. Material and unresolved.
13. **Whether Vietnam's Circular 40/2021 rates and the VND 100 million threshold remain current**, given Vietnam's new Tax Administration, PIT and VAT laws with 2026 effect, which were not reviewed.
14. **Vietnamese advertising-law disclosure obligations** for sponsored or affiliate content, and whether any Vietnamese rule addresses AI-generated media disclosure. Not established; local counsel required.
15. **YouTube's affiliate-link position.** Neither the paid-promotion page nor the Branded Content Policy addresses affiliate links. US FTC rules apply independently, but that is law rather than YouTube policy and was not verified against an FTC source here.
16. **The exact date of YouTube's 2026 "AI personas" clarification.** The rule's text is confirmed first-party; third-party coverage dates the clarification to mid-July 2026, but the Help Centre page carries only the 15 July 2025 rename stamp.

## 7.4 Third-party claims this dossier explicitly rejects

These circulate widely and are **not supported by any first-party page**:

- Instagram pays "55% Reels ad revenue share at 10,000 followers" or "~$30 per 1M views." Instagram has **no ad-revenue-share product at all**.
- Facebook Content Monetization requires "10,000 followers / 600,000 watch minutes in 60 days / 5 videos." These come from pages that now redirect; Meta publishes no current numeric thresholds.
- TikTok Pulse pays a 50% revenue share.
- A "70% visual similarity" detection threshold on Meta, or specific reach-change percentages for originals versus aggregators.
- A distinct TikTok rule against "mass-produced AI content" or "AI slop." No such For You feed category exists; the applicable rules are the unoriginal and low-quality ones.
- That Meta's July 2025 announcement was an AI crackdown. The word "AI" does not appear in it, nor in its March 2026 successor, nor in any of Meta's four monetization policy documents.

## 7.5 Methodological limitations of this dossier

- **JavaScript-rendered pages.** TikTok's Community Guidelines and Help Centre, and Meta's Business Help Centre, return only navigation chrome to ordinary fetchers. Their content was recovered by rendering the pages in a browser and, where accordions remained collapsed, extracting the embedded markup payload directly. Quotes so obtained are verbatim but were not re-verified against a second rendering.
- **Geolocation.** Some Meta pages defaulted to Vietnamese; `?locale=en_US` was used to force English. Country-specific variation in what these pages display has not been ruled out.
- **Negative findings.** Several important conclusions here are *absences* — for example that "AI" appears nowhere in Meta's monetization policies, or that Instagram has no ad revenue share. An absence is weaker evidence than a positive statement, because it can be defeated by a single page not located. Each such finding names the specific pages checked.
- **Community and forum pages** on support.google.com could not be rendered and were not used as a basis for any policy statement.

## 7.6 Time-sensitivity warnings

- **TikTok Community Guidelines v2026H2 take effect 24 September 2026** — six days after this dossier's verification date. The AIGC and unoriginal-content sections are unchanged; the commercial-disclosure enforcement wording changes (§2.5).
- **YouTube's YPP changes take effect 1 February 2027**, with a terms-acceptance deadline of 31 January 2027.
- **Meta's In-stream ads for Live ended 15 June 2026.**
- **TikTok's Branded Content Policy took effect 31 August 2026** and is therefore very recent.
- **Facebook Content Monetization is still rolling out by invitation**, so its published surface may change materially without notice.

**Recommended re-verification cadence: monthly until 1 February 2027.** Three of the four platforms changed material policy within the twelve months preceding this dossier, and the two largest changes affecting this business — YouTube's 2027 thresholds and Meta's originality enforcement — are both still in motion.
