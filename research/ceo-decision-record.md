# CEO Decision Record

> **READ THIS FIRST - five identifiers were renumbered on 2026-09-26.**
>
> Three sessions write to this record, and two of them independently allocated `D-011` to
> `D-015`. By CEO decision the **Wave 1 session's five were renumbered** to `D-018`-`D-022`
> (commit `21e9882`, now on `main`), with withdrawal markers left at the old positions
> carrying the mapping.
>
> **A bare `D-011`-`D-015` therefore means this branch's decision, unambiguously.** No other
> identifier was changed.

## What moved

| Was | Is now | Decision, Wave 1 session |
|---|---|---|
| `D-011` | **`D-018`** | Operate as an individual creator; the CEO carries the legal position personally |
| `D-012` | **`D-019`** | Two-phase payee structure; the launch channel already exists |
| `D-013` | **`D-020`** | The test channel is an experiment; the company's process is unchanged |
| `D-014` | **`D-021`** | Vietnamese disclosure gaps carried; affiliate disclosure becomes a standing rule |
| `D-015` | **`D-022`** | The first item carries no affiliate link |

Unchanged and unambiguous: `D-001`-`D-010` (shared history before the fork), `D-011`-`D-016`
(this branch), `D-017` (Wave 2, the video subject), `D-023` onward, and `C-001`-`C-006`.

## Allocation blocks - so this cannot happen a fourth time

Renumbering resolved the collision that existed. It does not prevent the next one, because
nothing stops two sessions reaching for the same next number again. One flat `D-nnn` space is
kept, as decided, and each session draws from **its own reserved block**:

| Session | Block |
|---|---|
| Wave 0 and orchestration (`claude/execute-prompt-txt-4a3ee4`) | `D-023`-`D-099` |
| Wave 1 (`claude/eloquent-taussig-724cc2`) | `D-100`-`D-199` |
| Wave 2 (video production) | `D-200`-`D-299` |
| Any later session | next free hundred, taken without asking |

No prefixes, one space, and no coordination needed - which matters, because the first two
collisions were caught only because the sessions happened to be talking to each other.

**Still open, and no numbering scheme fixes it:** the technical design and this record both use
bare `D-nnn`, for architecture decisions and business decisions respectively, so `D-011` is
ambiguous *by kind* rather than by origin. Resolve by naming the document when citing across
the two, or by adopting `CEO-D-nnn` against `ADR-D-nnn` if it starts to bite.

---

# Original record — Framing Gate, run `run-258e0a3415d2`

| Field | Value |
|---|---|
| Decision date | 2026-09-18 |
| Decided by | CEO / Owner (vuhoangcao@kms-technology.com) |
| Gate | investigate → Framing Gate (closes `problem-framing`) |
| Gate decision | **approved** |
| Evidence assessed | `requirement-framing.md`, FRAME-2026-0001, 10 outcomes / 67 requirements / 15 open questions |
| Recorded by | omn-agent runtime, `.omn-agent/runs/run-258e0a3415d2/` |

This record exists because the runtime stores a gate decision but not its rationale, so the
answers below would otherwise not reach the downstream phases. It is the authoritative
statement of what the CEO decided. Where it conflicts with an assumption in an upstream
artifact, this record governs.

---

## D-001 — Q-001: This is a separate line of business

**Decision.** The AI media company is a **separate line of business** from omniONE. The
omniONE product context, its goals and its success metrics do not govern it, and the two sets
of measures do not compete.

**Consequence.** `AS-006` in the framing (confidence: low) is resolved. Outcomes `O-001` to
`O-010` stand as the company's own measures. The framework's `context/product-context.md`
describes a different product and is not the product context for this work.

---

## D-002 — Q-008: Publication is CEO-approved, individually, at first

**Decision.** Until the quality, copyright and policy gates have demonstrated a clean record,
**the CEO approves every publication individually**. Every other routine production decision —
ideation, research, scripting, design, scheduling, cost routing within budget — is autonomous
and does not come to the CEO.

**Consequence.**

- The publishing gate carries a **mandatory human approval step** for every video. This is not
  a configuration default that the workforce may relax; it relaxes **only by an explicit,
  recorded, later CEO decision**.
- `R-057` (approval routes) and `R-062` (escalation path) can now both be applied: the
  routine/reserved boundary is *publication*, not a spend threshold.
- Design implication: the CEO review surface must make per-video approval cheap — a single
  screen carrying the video, its thumbnail, its metadata, its rights record and its gate
  verdicts — because at 3 videos/week/channel this is the CEO's main recurring workload, and
  it grows linearly with channel count. **The cost of this decision is the thing that must be
  re-examined before channel 2 launches.**
- A "clean record" threshold for relaxing this is **not yet defined** and remains open — see
  Q-004. It must be proposed to the CEO, not assumed.

---

## D-003 — Platform and market scope: all named platforms

**Decision.** In scope at launch: **YouTube (long-form and Shorts), TikTok, Facebook,
Instagram**, and a **Vietnamese-language channel alongside English**.

**Consequence.**

- Platform policy research must cover all four platforms, not YouTube alone, and the policy
  matrix must treat them as four distinct regimes.
- Two language markets means the brand system, the voice, the SEO surface and the
  fact-checking sources are per-channel configuration, not global constants.
- **Risk to surface, not to resolve here:** this is the widest possible launch scope for a
  company that has not yet published one video, and it sits in tension with the supplied
  intent's own MVP definition (one channel, one complete video, then 3 videos/week, *then*
  scale). The recommendation phase must state plainly what "all platforms at launch" costs in
  capacity, compliance surface and CEO approval time versus a sequenced rollout, and put that
  trade-off back to the CEO. The CEO decides; the workforce does not quietly narrow the scope.

---

## D-004 — Budget: no pre-set cap; compute it from the numbers

**Decision.** No monthly cost envelope is imposed up front. The CTO must **compute the
required AI and production capacity bottom-up** from the committed publishing rate, then
present cost **Options A (minimum cost) / B (balanced) / C (maximum reliability)** with their
trade-offs. The CEO sets the budget after seeing the numbers.

**Consequence.** `Q-010` (funding available) and `Q-004` (target values) stay open and are
answered *after* option-analysis, not before it. Capacity forecasting is therefore a blocking
input to the recommendation, and every figure in it must be auditable arithmetic with stated
assumptions — not a quoted total.

---

## D-005 — Revenue model at launch: platform advertising + affiliate

**Decision.** Two revenue sources are in the model at launch: **platform advertising revenue**
and **affiliate links**. Sponsorship / brand deals and fan funding / memberships are **out of
the launch model** and are not to be assumed in any projection.

**Consequence.**

- `Q-007` is answered.
- Affiliate revenue brings its own compliance obligation: affiliate and paid-promotion
  disclosure is now a **publishing gate check**, on every platform, not an optional nicety.
- Affiliate revenue is earnable **before** monetization thresholds are met; ad revenue is not.
  The financial model must not treat the two as arriving on the same timeline.
- Advertiser-suitability now has direct revenue consequences on both paths, so topic selection
  inherits a hard constraint from the revenue model.

---

---

# Second round — decisions taken on the investigation report

Decided 2026-09-18 by the CEO, after `investigation-report.md` (68 observations, 7
contradictions, 14 gaps) raised two blocking questions and returned one scope question. These
supersede nothing above; they answer what phase 2 asked.

## D-006 — Visual sourcing: licensed stock + original graphics, sparing AI cutaways

**Decision.** Videos are built from **licensed stock footage and music, original motion
graphics and data visualisation**, with **short AI-generated cutaways only where nothing else
fits**. Not mostly-AI-generated footage; not slideshows.

**Consequence.**

- Answers the investigation's blocking `Q-001`. The bottom-up capacity computation can now
  proceed against roughly the **$86/month** illustrative figure rather than **$536/month** —
  the two differed by a factor of six on this decision alone.
- AI *video generation* was the only line item that dominated the budget ($30–240 per 10
  minutes, against ~$1.58 for all text, images and narration combined). Capping it at sparing
  cutaways moves the company's cost centre from generation to **licensing**, which is a
  predictable monthly subscription rather than a per-second variable.
- This is also the **lowest-risk choice against the originality regime**, which is the
  constraint that can kill a channel: the platforms catch templated, minimally-transformed
  output by name. Original graphics and original narrative over licensed footage is the
  documented allowed case.
- Licensing now becomes a first-class operational obligation: per-channel allowlisting of
  music and stock licences, with the investigation noting that **revenue lost before
  registration is unrecoverable**.

## D-007 — Payee and entity: Vietnam-resident payee, YouTube only at first

**Decision.** Channels operate under a **Vietnam-resident payee**, and **YouTube is the only
platform at launch**.

**Consequence.**

- Answers the investigation's blocking `Q-002`. It is the only internally consistent pairing
  available: TikTok Creator Rewards excludes Vietnam, and Meta lists Vietnamese as a supported
  language while **Vietnam is not a supported country**. YouTube is the only platform with a
  confirmed monetizable route for a Vietnam-resident payee.
- One ad-payment account is permitted per payee name, so this must be settled **before any
  channel is created**. It now is.
- **Unresolved and material:** the Vietnamese tax position (rates, threshold, withholding on
  foreign-sourced earnings) could not be verified to first-party standard — the available
  material is professional-firm summary, and the 2021 instrument behind it was not checked
  against Vietnam's 2026 tax laws. **This requires qualified local counsel before revenue
  arrives.** It is not a blocker for building, but it is a blocker for banking.
- Revisit trigger: if the company later wants TikTok or Meta monetization, that is an
  entity-structure decision, not a technical one.

## D-008 — Launch scope: YouTube English first, prove it, then expand

**Decision.** **One channel, English, on YouTube.** One complete video, then 3 videos/week
sustained, then expand. This **supersedes D-003's all-platform, two-language scope.**

**Consequence.**

- Resolves the investigation's `Q-003` and Contradiction 2 in favour of the supplied intent's
  own MVP definition, which D-003 had contradicted.
- Collapses the compliance surface from four policy regimes to one, and holds per-video CEO
  approval workload (D-002) to a rate one person can actually sustain.
- Captures the **pre-1-February-2027 YouTube Partner Program threshold**, which is the one
  deadline that rewards starting now: as an unpublished company it is a new entrant, and new
  entrants after that date need 8,000 qualified watch hours or 20M Shorts views instead of
  4,000 / 10M.
- The Vietnamese-language channel and the other three platforms are **deferred, not cancelled**.
  Expansion reuses the same masters and the same shared agents; it is a configuration and
  compliance question at that point, not a redesign.

---

## Still open after this gate

These were **not** decided and must not be treated as settled by any downstream phase:

| ID | Open question | Answered by | Needed at |
|---|---|---|---|
| `Q-002` | Measure for the original-content outcome `O-003` | omn-product-owner | scope definition |
| `Q-003` | Measure for the continuous-improvement outcome `O-010` | omn-product-owner | scope definition |
| `Q-004` | Target values: profit, growth, quality floor, cost/video, capacity; and the "clean record" bar that would relax D-002 | omn-product-owner → CEO | option-analysis |
| `Q-005` | Current official platform policy content | supplied as research evidence (see `platform-policy-dossier.md`) | technical-discovery |
| `Q-009` | Who is accountable for legal determinations escalated out of the workforce | CEO | technical-discovery |
| `Q-010` | Funding available at start; cost envelope for channel 1 | CEO, after option-analysis | recommendation |
| `Q-011` | Does the non-operating day apply to publication as well as production | omn-product-owner | scope definition |
| `Q-012` | Will human contributors work alongside the AI workforce | CEO | scope definition |
| `Q-013` | Retention and privacy expectations for research, analytics, audit and audience data | omn-product-owner | technical-discovery |
| `Q-014` | Protection required for platform and provider account credentials | architect | technical-discovery |
| `Q-015` | Volume, turnaround and target channel count beyond the stated rate | omn-product-owner | option-analysis |

---

# Third round — architecture approved, implementation authorised

Decided 2026-09-26 by the CEO, after reviewing `wave-1/03-technical-design.md` and the seven
architecture decision records in `wave-1/decision-records/`.

## D-009 — The Wave 1 architecture is approved

**Decision.** The technical design is **approved as written**, together with all seven
architecture decision records D-001 to D-007, and the stack selection **O-005** (.NET runtime,
PostgreSQL as the single datastore holding relational state, the append-only record and the
durable job queue as a transactional claim table, one long-running service on one node, plus
the dedicated secret store).

**Consequence.**

- The hold recorded at the Design Gate on 2026-09-26 is **lifted**. That hold was a deliberate
  owner review, not a blocker, and the review is now complete.
- `RK-004`, the only open item that blocked building, is closed at design level and the design
  that closes it is now accepted. The consumer-subscription account pool from the supplied
  intent's §42 does not appear in the approved design and must not be reintroduced.
- Decision record D-002 is Accepted rather than Proposed; open question Q-001 is closed.
- The four structural enforcement properties are now binding on implementation: capability
  access only through the resolution boundary (D-001), least privilege by four independent
  structural layers (D-003), credential material never in the caller's scope (D-004), and the
  zero-AI-cost property as a dependency-direction rule verified by a build-time boundary test
  (D-005). Each was designed to remove the alternative path rather than guard it, and an
  implementation that reintroduces an alternative path fails the design, not merely a check.
- Owner approval per D-002 remains a state in the gate transition table and absent from
  configuration (D-007). It is not a setting and implementation must not make it one.

## D-010 — Wave 1 implementation runs in a separate session

**Decision.** Implementation is **authorised to begin**, and is to be carried out in a
**separate working session** rather than continuing in the session that produced Wave 0 and the
Wave 1 design.

**Consequence.**

- Run `run-dd80173faaad` continues from phase 4 (`implementation`) in the new session. Phases 1
  to 3 and the Scope, Planning and Design gates are complete and are not re-run.
- The authoritative run state stays where it is, at
  `D:/Project/MyCompany/.omn-agent/runs/run-dd80173faaad/`. It is shared across worktrees, so
  the new session drives the same run rather than starting its own.
- Known obstacle, recorded so the new session does not discover it late: the `implement-feature`
  **Verification Gate** names `omn-qa` as its only owner while `omn-qa` produces the evidence
  that gate assesses, which the Producer Exclusion Rule forbids. The gate is undecidable as the
  framework currently stands and needs a second owner in the framework payload. See
  `research/framework-defects.md`, defect 4.

## Still open, unchanged by this approval

`RK-001` controls before first publication · `RK-002` price re-fetch before any spend, next
policy pass due 2026-10-26 · `RK-003` Vietnamese tax position, blocks banking not building,
longest lead time · `RK-005` approval-minutes baseline · `RK-006` the 2027-02-01 threshold, the
only fixed date. The channel niche and content pillars remain unchosen and block Wave 2.

---

# Fourth round — niche direction and tax posture

Decided 2026-09-26 by the CEO.

## D-011 — Tax posture: individual or household business first, company only if it works

**Decision.** Operate initially as an **individual** or a **household business (hộ kinh doanh)**.
Incorporate a company only once the channel has proven itself.

**Consequence.**

- Correct for the stage. It avoids incorporation and annual compliance cost during the
  pre-revenue period, which is the period the recommendation identified as having no revenue
  against full cost. It also matches the earlier finding that a treaty-country entity is net
  negative at launch volumes — a UK Ltd only breaks even above 200k–617k views/month.
- **`RK-003` is not discharged by this decision — it is narrowed.** Qualified Vietnamese
  counsel is still required, but the question becomes specific: the household-business tax
  regime, its revenue threshold, and the rates that apply to foreign-sourced platform income.
  The figures previously found (a VND 100m threshold, 5% VAT + 2% PIT, from a 2021 instrument)
  are professional-firm secondary sourcing and were **not** verified against Vietnam's 2026 tax
  laws. Do not plan against them until counsel confirms.
- **`RK-006`'s sibling obligation becomes more urgent, not less.** The earlier analysis found
  that failing to submit the W-8BEN **as an individual** triggers **24% backup withholding on
  total worldwide earnings**, which is worse than the 30% that applies only to the US-sourced
  share. Submitting it is now a first-class precondition of the payment account, not a detail.
- **Migration cost to record now, so it is not discovered later.** One ad-payment account is
  permitted per payee name, and the payee is hard to change after the fact. Moving from an
  individual or household payee to a company payee later is a payee change, not a settings
  change. Counsel should be asked what that migration costs *before* the first account is
  created, so the decision to start simple is taken with its exit cost known.

## D-012 — Niche direction: animals, form not yet settled

**Decision.** The channel's subject is **animals** — either cute and funny animals, or the
animal world.

**Status: direction accepted, format NOT yet settled.** The two readings differ enough that
they are effectively different businesses, and one of them is foreclosed by the same policy
regime the whole plan is built to respect:

- **Compilation of cute and funny animal clips** is, in the ordinary case, assembled from other
  people's social-media footage. That is named directly by YouTube's reused-content policy,
  which prohibits content "compiled from other social media websites" without substantive
  original contribution, and it is the business model the supplied intent itself ruled out:
  re-uploading other people's videos and random social-media clips. It also conflicts with
  D-006, which sources visuals from licensed stock and original graphics.
- **Wildlife and animal-world content** built from licensed stock footage with an original
  script, original narration and original motion graphics is fully compatible with D-006 and
  with the originality regime.

Analysis commissioned 2026-09-26: `research/animal-niche-analysis.md`. It must establish the
format question, whether the committed stock libraries actually carry enough animal footage for
13 videos a month, the RPM band for this category, the re-run break-even, and in particular the
**made-for-kids exposure** — animal content can be classified as made for kids, which removes
personalised advertising and sharply reduces RPM. That is potentially the largest economic risk
in this niche and is not yet quantified.

This decision blocks **Wave 2**, not Wave 1. The foundation being built now is niche-agnostic.

---

# Fifth round — niche settled, Wave 2 authorised

Decided 2026-09-26 by the CEO.

## D-013 — Niche: science and the natural world, adult-framed edutainment

**Decision.** The channel's subject is **broad-appeal science and natural-world edutainment,
framed for an adult audience**. This supersedes the open format question left by D-012 and
settles the direction the CEO first expressed as "animals".

**Basis.** Scored 383/500 in `research/niche-recommendation.md`, runner-up to engineering and
infrastructure at 424/500. The CEO selected it over the higher-scored option. Recorded so the
trade is visible rather than implicit: this option **wins** on raw volume ceiling and RPM — which
is the CEO's stated priority of reaching a mass audience — and **loses** on three counts the
analysis named: made-for-kids drift, near-zero affiliate intent, and competing for attention
against Kurzgesagt-class animation budgets on USD 77.41/month.

**Consequence — production constraints that now become binding:**

- **Adult framing is a hard production rule, not a style preference.** Serious narration,
  scientific register, no cartoon styling, no child-directed language, no toy or nursery
  imagery. A made-for-kids designation removes personalised advertising and sharply reduces
  RPM, and it is the single largest economic risk in this subject.
- **Compilation is foreclosed.** Content assembled from third-party clips is named directly by
  the reused-content policy and is barred by D-006 regardless. Every item is original script
  and original narration over licensed stock, with original motion graphics.
- Affiliate revenue, which D-005 places in the launch model, is expected to be **weak** in this
  subject. The pre-threshold period should be planned on the assumption that it contributes
  little, so the ~7–14 months of full cost before advertising revenue is closer to the upper
  end of the USD 400–900 range than the lower.
- The moat is **treatment and scripting quality**, not subject matter. The subject is not
  scarce; the discipline is.

**Open and carried, not resolved by this decision.** `research/animal-niche-analysis.md` was
still in flight when this decision was taken. It is scoped to quantify the made-for-kids
exposure, test stock-footage supply at the specific-species level rather than in aggregate, and
re-run break-even for this subject. Its findings **refine the treatment rules and may tighten
them**; they do not reopen the niche. If it finds made-for-kids exposure inherent to the subject
rather than manageable by treatment, that is a material finding and returns to the CEO.

## D-014 — Wave 2 begins, in a separate session

**Decision.** Wave 2 is authorised and runs in its **own session**, as Wave 1 did.

**Scope, per the plan of record:** the complete production pipeline — Idea → Research → Script →
Design → Production → QC → Copyright → **publish-ready output**. The goal is **one complete
video, end to end**.

**Consequence.**

- **Wave 2 does not publish.** Publishing is Wave 3. The output is a publish-ready artifact
  held at the gate. Nothing in Wave 2 creates a channel or commits spend, so `RK-003` — the
  unverified Vietnamese tax position — does not block it.
- Wave 1's implementation phase is complete; its `quality-review` phase is blocked on the known
  `awaiting_policy_exception` defect and is being handled in the Wave 1 session. Wave 2 builds
  on the Wave 1 foundation and should not re-create it.
- D-002 still holds: the CEO approves the publication individually. In Wave 2 that approval
  step exists and is exercised, but it gates a publish-ready artifact rather than an upload.

---

# Sixth round — the niche is an experiment, not the business bet

Decided 2026-09-26 by the CEO, after the completed animal-niche analysis revised that option
from 383 to 326 of 500 and widened the gap to the engineering candidate from 41 to 98 points.

## D-015 — Science and the natural world stands, reframed as a deliberate experiment

**Decision.** **D-013 stands.** The first channel is adult-framed science and natural-world
edutainment. The CEO took this decision **with the revised score in hand**, not in ignorance of
it, and reframed what the first channel is for:

> The company is at its beginning. The first channel is an **experiment that proves the
> pipeline**, not the bet that decides the business. In time the company's own departments must
> find profitable niches themselves.

**Why this is a coherent decision rather than a rejection of the evidence.** The analysis ranked
niches by *expected profitability of that channel*. The CEO is optimising something different
and longer-lived: **the machine that produces channels.** Under that objective a 98-point gap on
one channel's economics is a smaller quantity than the value of learning the pipeline end to
end, and the subject that keeps the operator engaged through the first hard month has a value
the scoring model does not carry. The scoring model is not wrong; it was answering a narrower
question than the one being decided.

**Consequence.**

- **The first channel is a test instrument.** Its success measure is not revenue but whether
  the pipeline can produce a compliant, original, publish-ready video repeatedly and at a
  metered cost. Revenue-denominated judgement of this channel should be deferred accordingly.
- **Niche-agnostic construction is now a design requirement, not a preference.** The topic will
  change; the machine must not have to be rebuilt when it does. Favour components that are
  re-pointable over anything hard-wired to wildlife.
- **The engineering and infrastructure candidate is not discarded.** It scored 429 under
  volume-weighted scoring and remains the strongest known option. It is the natural candidate
  for channel two, or for this channel if the experiment shows the subject cannot carry the
  originality burden.
- **Autonomous niche discovery becomes an explicit company capability**, not an aspiration. The
  Strategy function must eventually do what the last three analyses did by hand: score
  candidate niches on demand, competition, production feasibility, policy admissibility and
  expected economics, and bring recommendations to the CEO. That is the supplied intent's
  Wave 9 ambition, given a concrete first job.

## D-016 — Budget holds at USD 77.41; the species gap is closed by topic selection

**Decision.** The **+USD 16.50/month second stock library is not authorised.** The envelope
stays at **USD 77.41**.

**Consequence.** Species depth is managed by **choosing well-filmed subjects and avoiding rare
species**, not by spending more. This costs breadth of topic, and it is recorded as a real cost
rather than waved away. It also has an unplanned benefit: the contaminated search results the
analysis found are concentrated in exactly the rare-species queries this constraint avoids.

## Production constraints now binding on Wave 2, from the completed analysis

- **Never source footage by clip count.** Rare-species results are contaminated by
  generative AI — one library returns 84 clips for a species filmed alive a handful of times,
  including a tropical storm of the same name. Every wildlife clip is verified against caption
  and provenance before entering a cut, and anything unconfirmed is rejected. A pipeline that
  publishes AI-generated animals as documentary evidence fails on credibility, not cost.
- **Clip counts are upper bounds of unknown looseness.** Search is fuzzy: one library silently
  answers `saola` with results for "saona", and a nonsense query returns 26 results.
- **Licensing does not cure reused content.** "Even if you have their permission" appears twice
  in the NOT ALLOWED list. The channel owns no footage under D-006, so the whole originality
  burden rests on script and original graphics. Test: if the video would still stand as an
  essay without the clips, it is on the right side of the policy; if the clips *are* the
  content, it is not.
- **Made-for-kids resolved in favour, conditionally.** Animals appear in neither YouTube's nor
  the FTC's factor lists. Nine treatment rules apply as production rules. The penalty is 31× on
  RPM and it disables the notification bell, end screens, playlists and comments — the whole
  subscriber machinery, and subscribers are the binding gate. The FTC pursues the uploader, so
  mis-declaring "not made for kids" is the riskier error.

**Break-even for this niche (ESTIMATE):** 32,525 views/month, about 2,502 per video. If
made-for-kids were to apply: 110,000–119,000 views/month.

---

# Seventh round — the first video's subject

Decided 2026-09-26 by the CEO, during Wave 2 (`run-3a58551ee912`), choosing between four options
each constrained by the species-level footage audit in `research/animal-niche-analysis.md` §3.3.

*(Filed as D-017. An earlier draft of this entry used D-015; that identifier was taken
concurrently by the decision above and the draft was withdrawn before it propagated. There is
one numbering space for CEO decisions and this is the next free identifier in it.)*

## D-017 — First video: the honey bee waggle dance, as a coordinate-encoding mechanism

**Decision.** The first video's subject is the **honey bee waggle dance** — how a returning
forager encodes the *direction and distance* of a resource as a vector another bee can read and
fly on. Treatment is pillar 1, **Mechanism**, format **F5**: original motion graphics carry the
argument, licensed footage illustrates it.

**Why this subject cleared the constraints**, recorded so the reasoning survives the choice:

- **Supply is real, not aggregate.** `honey bee` returns **1,213** clips on the committed
  library — a single, distinctive, unambiguous token, so it is one of the counts §3.3 classifies
  as trustworthy rather than an upper bound of unknown looseness. **No second library is needed,
  so D-016 is satisfied by the choice rather than strained by it.**
- **It sits away from the contaminated tail.** The species is common and well-filmed, so it is
  far from the rare-species queries where §3.4 showed clip counts are largely generative-AI
  assets, green-screen composites and name collisions.
- **The originality burden lands where the channel can carry it.** The waggle dance is an
  *argument* — an angle-to-sun vector diagram, a distance-to-duration encoding, an error
  distribution — and those graphics are original artefacts that carry the explanation rather
  than decorate it. It passes D-016's test outright: **the video would still stand as an essay
  with every clip removed.**
- **Made-for-kids exposure is at its lowest here.** The subject needs no named protagonist, no
  invented personality and no simplified register to be interesting, so K-1, K-2, K-4 and K-6
  are satisfied by the material rather than enforced against it.
- **The science is attributable**, which §8.5 makes a heavier obligation in this niche than in
  the alternative: von Frisch's decoding (Nobel Prize in Physiology or Medicine, 1973) and the
  modern literature on dance precision and error. Every claim can name a paper or an institution.

**Two cautions specific to this subject, binding on production:**

1. **`waggle dance` is a multi-word phrase, so its count is an upper bound of unknown
   looseness** under the fuzzy-matching finding. Clips labelled as the dance must be confirmed
   by eye to show the dance itself rather than generic hive activity. The trustworthy count is
   for `honey bee`; the count for the behaviour is not yet established and is owed at `A-024`.
2. **Bees are a subject whose default styling is cheerful**, which is precisely the drift K-1 to
   K-6 exist to stop. The treatment must resist it deliberately rather than by omission —
   documentary register, no warm-whimsical score, no anthropomorphic framing of the colony.

**Consequence.** The topic question the scope definition reserved to the CEO is closed. The
remaining production judgments — structure, pacing, shot selection, graphics treatment, and
runtime within the 10-minute floor and 12–14 minute target — are the workforce's, not the CEO's.

**Not decided by this.** Nothing here changes the niche, the budget, or the rule that Wave 2
ends at a publish-ready artifact held at the gate. D-014 and D-016 stand in full.
---
# Correction — the metering target in the Wave 2 ticket was wrong

Recorded 2026-09-26. Found by the Wave 2 session while verifying a cost claim; verified here.
This is a correction to an instruction, not a change of decision. Nothing the CEO approved
changes.

## C-001 — Wave 2 must meter against O-002, not against USD 1.58

**The error.** Ticket MC-3 (line 34 of `tasks/MC-3/input.md`) instructs Wave 2 to meter the
first video "against the estimate of about USD 1.58 per ten-minute video", and `S-006` of the
approved scope definition inherited that figure from the ticket. **The ticket was written in
this session and the figure is wrong for the purpose.**

**Why it is wrong.** USD 1.58 is the capacity dossier's EXPECTED case (`research/ai-capacity-dossier.md`
line 773): **295,000 input / 21,500 output tokens**, covering research, scripting, fact-check,
SEO and QC only. It is a narrower scope than the company actually committed to.

**What the CEO actually approved** at the Recommendation Gate is option **O-002**
(`artifacts/03-option-analysis.md` line 58):

| | O-002, approved |
|---|---|
| Per-video tokens | **377,000 input / 33,000 output** |
| Per-video variable cost | **USD 2.647440** |
| Standing licensing | USD 42.99/month |
| Monthly total at 13 videos | **USD 77.41** |
| All-in per video | **USD 5.95** |

The difference is not an error of arithmetic but of scope: O-002 adds the tasks that decisions
D-006 and D-002 create and that the dossier's EXPECTED case never modelled — stock shot-list and
clip selection, motion-graphic and data-visualisation briefs, caption and chapter polish, the
compliance gate pack, cutaway prompt authoring, and CEO approval package assembly.

**Consequence if left uncorrected.** Measuring the first video against USD 1.58 would report a
**roughly 68% overrun before a single token was counted** — an overrun that exists only because
the wrong target was quoted. That is exactly the kind of manufactured alarm the plan's labelling
discipline exists to prevent.

**The correction.** Wave 2 reports measured cost against **O-002**, with USD 1.58 shown as
superseded and its narrower scope named. `S-006` in the approved scope definition is **not**
rewritten: it passed its gate, the figure is traceable to the ticket, and quietly editing a
gated artifact would damage the audit trail more than the stale number does. The correction is
carried here instead and referenced from the Wave 2 report.

**The escalation test that replaces it.** If measured per-video input lands materially above
**377,000 tokens**, the approved ledger is wrong and that escalates to the CEO. That is the
trigger worth watching, and it is now the stated purpose of the metering.

## C-002 — Development cost and per-video cost are separate, and phases 4 and 5 are mixed

**Established while resolving the above.** The omn-agent framework is what *builds* the system;
the .NET service being built is what runs 13 times a month. Framework token consumption is
therefore **development capex amortised across every video the pipeline will ever produce**, not
a per-video operating cost. Reporting it as per-video would misinform in the direction of alarm.

Per-video governance is **already budgeted** and is not an unfunded addition: compliance gate
pack 30,000/3,000, CEO approval package assembly 8,000/1,500, QC review 25,000/2,500,
fact-check 60,000/3,000 — **123,000 input and 9,500 output, about a third of the per-video
ledger, already allocated to governance.**

**One honest qualification, flagged in advance rather than presented later as a clean
measurement.** The Wave 2 scope has phase 4 both build the capability and run one item through
it (`S-001` produces the item; `S-007` and `S-012` build the re-pointable mechanism). So the
capex/opex split is clean for phases 1, 2, 3 and 6, and **phases 4 and 5 are mixed**. Separating
item-production tokens from capability-building tokens inside them is a **judgement call, not a
measurement**, and the Wave 2 report says so.

**Development cost remains genuinely unbudgeted** in every estimate the company holds. Wave 2 is
its first measurement. Worth tracking across waves; not a reason to doubt the unit economics.

---

## C-003 — The cost-control thesis is untested, not merely unmeasured

Recorded 2026-09-26. Found by the Wave 2 session while attempting the L3 versus L1/L2
measurement; **verified independently here** before recording.

**What was asked for.** A measured split of L3 spend against L1/L2 spend, because the approved
ledger assumes 290,000 input tokens at L3 and 87,000 at L1/L2, and the company's entire
cost-control argument — *the cheapest model that reliably meets the required quality* — rests on
that ratio being roughly right.

**What was found: there is nothing to measure, because no tiering exists.** Verified here: all
twelve registered agents declare `model: inherit` at line 5 of their host adapter files in
`.omn-agent/agents/*.agent.md`, and a search of `config/`, `registry/` and `runtime/` returns no
tier assignment, no routing decision and no per-task model selection anywhere in the framework
as installed. Every phase executes at whatever model the orchestrating session is running.
**The denominator does not exist.** Recorded as a negative result rather than left blank.

**Three consequences.**

1. **Every framework phase runs at the most expensive tier available**, because `inherit`
   resolves to the operator's model. Nothing routes a cheap task to a cheap model.
2. **The development-cost figures in the Wave 2 ledger are an upper bound**, not a
   representative figure. A tiered framework would do the same work for less, by an amount
   nobody can yet state. If anything, capex has been **over**-reported, not under.
3. **The cost-control thesis has never been exercised — by anything, in any wave.** Wave 1's
   `D-012` specifies that a capability request names a capability class, a reasoning tier and a
   quality floor and *never* a provider or a model, and `M-001` is the boundary that reads that
   tier and routes on it. **That boundary is designed and built, but has not executed.** So the
   290,000/87,000 ratio carrying the company's cost argument, and the USD 2.647440 per-video
   figure that follows from it, rest on an assumption nothing has yet tested.

**This is not a framework defect.** omn-agent builds software; it is not the production
pipeline, and `inherit` is a reasonable default for a development tool. The company's own
pipeline is where tiering belongs, and that is where it was designed.

**How this must be reported until it changes.** The 290,000/87,000 split and every figure
derived from it — including USD 2.647440 per video and the USD 77.41 monthly envelope — are
**assumptions, not measurements**, and are to be labelled as such in every report. The first
real evidence for or against the routing design arrives only when `M-001` is running in the
production pipeline and recording the tier actually used per operation. **Making `M-001` record
tier per operation should be an explicit acceptance criterion of whichever wave first runs it**,
because without that record the thesis stays untestable however many videos are produced.

**What Wave 2 can still deliver, and it is not nothing:** per-phase totals with a stated
capex/opex allocation and the reasoning behind it; the exact narration character count and
graphics count from the finished item, priced at re-fetched rates; a zero on generated seconds;
and the per-video input total against the 377,000-token escalation test in `C-001`. That test
survives the absence of tiering intact and remains the trigger that matters most.

---

## C-004 — The decision record has forked: ten decisions share five identifiers

Recorded 2026-09-26. Raised by the Wave 2 session; **verified independently here** (`git merge-base`
returns `849835b`; both branches appended `D-011` to `D-015` after it).

**What happened.** Three sessions have been appending to one file that describes itself as the
authoritative record. Two of them allocated the same identifiers to different decisions:

| Id | This branch, `claude/execute-prompt-txt-4a3ee4` | Wave 1 branch, `claude/eloquent-taussig-724cc2` |
|---|---|---|
| `D-011` | Tax posture: individual or household business first | Operate as an individual creator; the CEO carries the legal position personally |
| `D-012` | Niche direction: animals, form not yet settled | Two-phase payee structure; the launch channel already exists |
| `D-013` | Niche: science and the natural world, adult-framed | The test channel is an experiment; the company's process is unchanged |
| `D-014` | Wave 2 begins, in a separate session | Vietnamese disclosure gaps carried; affiliate disclosure becomes a standing rule |
| `D-015` | Science and natural world reframed as a deliberate experiment | The first item carries no affiliate link |

`D-016` and `C-001`–`C-004` exist only here. `D-017`, the video subject, sits on top of this set.

**Nothing is lost and nothing contradicts.** These are ten distinct decisions that happen to
collide, and both sets are mutually compatible — the work can proceed on all of it without
choosing between them.

**What is broken is citation, and that is worse than a contradiction would be.** A contradiction
announces itself. This does not: "D-013" now names two different decisions depending on which
branch the reader holds, and every downstream artifact citing it has silently stopped being
unambiguous.

**The actual defect is not the collision. It is that three sessions write to one authoritative
record with no identifier-allocation mechanism.** This is the third collision in one day on the
same file; the first two were caught only because the sessions happened to be talking to each
other. A fourth is certain if nothing changes. This is a **process defect, not a framework one** —
omn-agent never claimed to arbitrate a shared document.

### Recommended resolution: do not renumber. Add a concordance and namespace going forward.

Renumbering is the obvious move and it is the wrong one, for the same reason `S-006` was left
alone in `C-001`: **existing citations live in artifacts that have already passed their gates.**
Renumbering silently invalidates them and produces a second, worse version of this problem. A
third session renumbering produces a third variant.

1. **Nothing already written is renumbered or renamed.** Both sets keep their identifiers.
2. **Cite by `branch : identifier : title` wherever ambiguity is possible** — the Wave 2 session
   already adopted this in `wave-2/carried-decisions.md` §1, and it is unambiguous under any
   later resolution.
3. **Namespace all future identifiers by originating wave** — `W0-D-018`, `W1-D-016`, `W2-D-018`.
   Collisions become impossible by construction rather than by coordination.
4. **Maintain a concordance table** — the table above is its first entry — at the head of the
   merged record, so a reader meeting a bare `D-013` can resolve it.
5. **When the branches merge, the concordance merges with them.** Both sets survive intact.

This is additive. It costs no rewriting, preserves every existing citation, and leaves the audit
trail whole.

**Not actioned unilaterally.** Renumbering or restructuring another session's record of CEO
decisions is not this session's to do, and the Wave 2 session correctly declined the same thing.
The recommendation above is put to the CEO; until it is decided, cite by branch, commit and title.

### Two facts relayed from the Wave 1 session that bear on earlier analysis

- **The launch channel already exists, is blank, and is not in YPP.** Grandfathering runs on YPP
  *membership*, not channel age, so it does not reach this channel. The **8,000-watch-hour
  planning figure in `C-001`'s sibling analysis is therefore confirmed correct on the facts**,
  not merely cautious. Note this also softens the ordering constraint recorded earlier that the
  payee position must precede channel creation — the channel exists; the binding step is the
  AdSense payee linkage, not the channel.
- **The first item carries no affiliate link.** This discharges one of the five compliance
  determinations as **positive recorded evidence** rather than as an unresolved negative, and it
  sidesteps the unverified YouTube position on whether affiliate links alone trigger the
  paid-promotion declaration.
---
# Fourth round — renumbered to D-018 to D-022

> **WITHDRAWN IDENTIFIERS — `D-011` to `D-015` on this branch.**
>
> These five decisions were recorded here as `D-011` to `D-015` and are now **`D-018` to
> `D-022`**. Nothing was deleted and no decision changed; only the identifiers moved.
>
> **Why.** Two numbering spaces grew from the same merge-base `849835b` without either
> knowing of the other, and five identifiers came to name two different decisions each. The
> other space — `claude/execute-prompt-txt-4a3ee4`, carried into the Wave 2 branch
> `claude/trusting-shirley-90ea75` — holds `D-011` to `D-017` plus `C-001` to `C-003`. This
> branch renumbers because that space has decisions stacked on top of the collision and is
> still being written, while this branch's run is closed. The Wave 2 session declined to
> renumber another session's record of the CEO's decisions, which was the right call, and the
> CEO decided this branch would move.
>
> **The mapping, once, so a stale citation resolves:**
>
> | Recorded here as | Now | Subject |
> |---|---|---|
> | `D-011` | **`D-018`** | Operate as an individual creator; the CEO carries the legal position personally |
> | `D-012` | **`D-019`** | Two-phase payee structure, and the launch channel already exists |
> | `D-013` | **`D-020`** | The test channel is an experiment; the company's process is unchanged |
> | `D-014` | **`D-021`** | The two Vietnamese disclosure gaps stay carried; affiliate disclosure becomes a standing rule |
> | `D-015` | **`D-022`** | The first item carries no affiliate link |
>
> A withdrawn identifier is never reissued on this branch. `D-011` to `D-017` mean what the
> other space says they mean.
>
> **Settled 2026-09-26.** The CEO confirmed that renumbering was what he intended. This
> renumbering **stands**; the competing prefix scheme proposed on
> `claude/execute-prompt-txt-4a3ee4` is **withdrawn**, and that branch's `W0-D-018` became
> `D-023`, leaving `D-018` to this branch. One flat `D-nnn` space, no prefixes.
>
> The reason that branch gave for not renumbering — that citations live in artifacts which
> have already passed their gates — was checked against the facts and did not hold for these
> five. Every bare `D-011` to `D-015` in a gated Wave 1 artifact belongs to the **technical
> design's own `D-nnn` space**, not to this record: that design's `D-011` is *"a retry is its
> own operation record"* and its `D-012` is *"the capability request... never names a provider
> or a model"*. Renumbering this record touched none of them.

### Identifier allocation — blocks, so the next collision cannot happen

Renumbering fixed the collision that existed. It does nothing about two sessions reaching for
the same next number tomorrow, which is how both of today's collisions happened. One flat
`D-nnn` space is kept, divided into blocks. **A session allocates inside its own block and
coordinates with nobody.**

| Session | Block |
|---|---|
| Wave 0 / orchestration (`claude/execute-prompt-txt-4a3ee4`) | `D-023` – `D-099` |
| **Wave 1 (this branch)** | **`D-100` – `D-199`** |
| Wave 2 (`claude/trusting-shirley-90ea75`) | `D-200` – `D-299` |
| Any later session | the next free hundred, taken without asking |

**The next identifier on this branch is `D-100`.** `D-018` to `D-022` above keep the numbers
they were given; the block applies to what comes next, not retroactively.

### Still open — ambiguity by kind, which no numbering scheme fixes

The technical design and this record **both** use bare `D-nnn`, for architecture decisions and
business decisions respectively. A reader meeting `D-011` cannot tell which is meant without
knowing which document they hold. Blocks fix ambiguity by **origin**; this is ambiguity by
**kind**, and it survives every scheme discussed today.

Resolvable by naming the document when citing across the two, or by `CEO-D-nnn` against
`ADR-D-nnn` if it starts to bite. Recorded as open rather than fixed, because changing either
namespace now would invalidate citations in artifacts that have genuinely passed their gates —
which is the argument that was wrong about this record and is right about that one.

Decided 2026-09-26 by the CEO, on a readiness check for Wave 3 that found `RK-003` to be the
longest-lead item on the critical path and the only one independent of Wave 2.

## D-018 — Operate as an individual creator; the CEO carries the legal position personally

**Decision.** The company has no budget at this stage to retain Vietnamese tax counsel. It will
operate **as an individual person publishing content on the launch platform**, with the **CEO as
the Vietnam-resident payee in his own name**. The CEO **personally accepts accountability** for
tax declaration and for the legal determinations the workforce escalates.

`RK-003` is therefore **accepted, not discharged**. The mitigation the risk register records —
*"qualified local counsel before the payment account is created"* — is **superseded** by this
decision. The risk stays open, at its recorded severity, with the CEO named as the acceptor.

> **Relationship to `D-011` in the other numbering space, which is not a duplicate.**
>
> `claude/execute-prompt-txt-4a3ee4` records a `D-011`, *"Tax posture: individual or household
> business first, company only if it works"*, carried into the Wave 2 branch. It is the same
> CEO answering the same subject, and the two agree on the posture: operate as an individual
> now, incorporate only once the channel has proven itself.
>
> **They disagree on one point, and this decision is the later one.** That record states
> *"`RK-003` is not discharged by this decision — it is narrowed. Qualified Vietnamese counsel
> is still required."* This decision states that there is **no budget to retain counsel** and
> that the CEO **accepts the risk personally instead**. Where the two are read together, this
> one governs on that point, because it was taken afterwards and on a fact the earlier one did
> not have.
>
> **What survives from the other record, and should be read with this one.** Two consequences
> it draws are not restated here and are not withdrawn by this decision:
> failing to submit the **W-8BEN as an individual** triggers **24% backup withholding on total
> worldwide earnings**, worse than the 30% that applies only to the US-sourced share, which
> makes submitting it a first-class precondition of the payment account rather than a detail;
> and moving from an individual or household payee to a company payee later is a **payee
> change, not a settings change**, whose cost should be known before the first account is
> created. The second of those is the migration cost `D-019` carries forward.

**What this settles.**

- **The payee position is settled.** The first of the two things §21.3 precondition 2 requires now
  has an answer: the payee is the CEO as an individual, resident in Vietnam. The payment account
  itself is created later; what had to be decided before the channel exists was *who the payee is*,
  because one account is permitted per payee name and duplicates are disapproved.
- **Open question `Q-007`, and `Q-009` from the first round, are closed.** Accountability for a
  legal determination escalated out of the workforce — including the audience designation and the
  disclosure judgements — rests with the CEO. The workforce escalates; it does not decide.
- **Wave 3 loses its one blocker that was independent of Wave 2.** What remains in front of Wave 3
  is Wave 2's own completion and the findings only Wave 2 produces.

**Why the timing costs little.** The plan already recorded that `RK-003` *blocks banking, not
building*, and that it *lands the moment revenue arrives*. Revenue is zero and stays zero until
partner-programme entry, which needs 1,000 subscribers and 4,000 qualified watch hours first. The
tax exposure being accepted here is therefore an exposure on an amount that does not yet exist.
The decision can be revisited when it first costs something, and the natural review point is
**before the first revenue**, not before the first publication.

**What this decision does NOT cover, and the CEO should know it.**

`RK-003` bundles four questions, and only three of them are about money:

| Question | When it bites | Covered by this decision |
|---|---|---|
| Whether the quoted rates and the annual revenue threshold survive Vietnam's 2026 tax laws | at first revenue | yes, accepted |
| The withholding treatment of foreign-sourced viewership earnings | at first revenue | yes, accepted |
| Vietnamese **advertising-law disclosure** obligations | at **first publication** | **no** |
| Whether any Vietnamese rule addresses **synthetic-media disclosure** | at **first publication** | **no** |

The last two are content-compliance questions wearing a tax label. They do not wait for revenue:
they land when the first video goes out, on a channel that publishes AI-assisted work. Nothing in
this decision answers them, and no budget is required to *read* them — unlike the tax position,
which needs an opinion, these are published rules that a competent reading can establish.

**Consequence carried, not solved.** The payee is now a natural person, and the plan records that
genuine isolation between channels requires **distinct legal payees**, decided before launch,
because the payment account's country and payee are hard to change later. This decision therefore
constrains any future multi-channel structure to sharing one payee unless it is revisited. That is
a Wave 6 concern, recorded here so it is not discovered there.

## Still open after this decision

`RK-001` controls before first publication · `RK-002` price re-fetch before any spend, next policy
pass due **2026-10-26** · `RK-003` **accepted by the CEO**, its two disclosure questions unanswered
and due at first publication · `RK-005` approval-minutes baseline, which Wave 2 produces ·
`RK-006` the **2027-02-01** threshold, the only fixed date, with capture treated as lost so 8,000
watch hours is the planning figure.

Wave 2 open questions `Q-001` (subject and angle), `Q-004` (budget for a second stock library) and
`Q-008` (audience-drift threshold, due before first publication) remain with the CEO.

## D-019 — Two-phase payee structure, and the launch channel already exists

**Decision, two parts.**

1. **The payee is staged, deliberately.** Channels launched now use the **CEO as an individual
   payee**, per D-018. Later channels will be created under a **company legal entity**. This is a
   recorded sequence, not a drift: the individual payee is the starting position, and the entity
   is the intended end state.
2. **The launch channel is an existing YouTube channel the CEO already holds**, rather than a
   newly created one.

**What part 1 settles.** The multi-channel isolation concern D-018 carried forward now has an
intended answer. The plan records that genuine isolation between channels requires **distinct
legal payees**, decided before launch, because the payment account's country and payee are hard
to change later. Staging the entity means channel 1 and any entity-held channel will sit under
**different payees by construction**, which is the isolation the plan asks for. What is not
solved, and is not solvable by sequencing, is that channel 1 itself stays on the individual payee
permanently unless it is migrated — and migration is the thing the plan records as hard.

**What part 2 changes, and it is not small.** Every precondition in §21.3 was written for a
channel that does not exist yet. An existing channel arrives with history, and history cuts both
ways:

| | Effect |
|---|---|
| Accumulated subscribers and qualified watch hours | count toward the Tier 2 threshold; the company may be closer to it than a standing start |
| Account age, 2-step verification | §21.3 precondition 3 may already be satisfied in part |
| Existing content on the channel | the inauthentic-content and reused-content policies are assessed at **channel** level, so prior uploads are inside the assessment, not outside it |
| Any prior copyright or community-guidelines strike | inherited. §21.3 precondition 3 requires **no active community-guidelines strike**; `RK-001` records that three copyright strikes in 90 days terminate the account "along with any associated channels" |
| Any channel-level made-for-kids designation | would contradict scope item `S-002`, which makes adult framing a hard gate condition rather than a preference |

**The decisive fact, and it is a fact rather than a decision.** The re-verified dossier is explicit
that the widely repeated "existing partners are grandfathered" framing is **misread**: the
first-party wording is *"If you are already in YPP, your status is not impacted by this update"*,
and that sentence covers **YPP membership only**. It does not exempt anyone from the new Shorts
revenue floor or the new activity requirement, and — critically here — **it does not apply to a
channel that is not already in YPP, however old that channel is.**

So `RK-006` resolves one of two ways, and which one is not yet established:

- **If the existing channel is already in YPP:** the 2027-02-01 threshold doubling does not apply
  to it. `RK-006`, recorded as the only fixed date in the plan and the driver of the 8,000-watch-hour
  planning figure, would **largely dissolve**. The remaining obligations would be accepting updated
  terms by **2027-01-31** and staying above the activity floor.
- **If it is not:** the channel is a new entrant for threshold purposes regardless of its age, the
  8,000-watch-hour planning figure stands, and its accumulated audience is a head start rather
  than an exemption.

This is the largest single open item in the plan by consequence, and it is answerable by looking
rather than by research. It is recorded here as **unestablished**, and no downstream phase may
assume either branch.

**Effect on Wave 2, which is running now.** Wave 2 publishes nothing, so it does not need the
channel to exist. But two of its scope items are **channel-scoped, not item-scoped**, and both
depend on which channel this is: `S-011`, which blocks publish-ready while the channel is
unregistered on any library its assets come from; and `S-002`, whose audience-drift monitoring
obligation and per-item audience designation are assessed against a channel that may already
carry a designation and an audience. Wave 2 should be told which channel it is producing for
before its implementation phase commits to an item.

### D-019 resolved — the branch is established, 2026-09-26

The CEO confirmed the channel's standing. Every item recorded above as unestablished now has an
answer, and the answers are uniform: **the channel is effectively blank.**

| Question | Answer | Consequence |
|---|---|---|
| Already in YPP? | **No** — the channel exists, monetization is not enabled | The grandfathering sentence does not reach it. It is a **new entrant** for threshold purposes regardless of when it was created |
| Subscribers and qualified watch hours | **New channel** | No head start. The 365-day watch-hour window has nothing in it because nothing has been published |
| Existing content | **None** | The channel-level inauthentic-content and reused-content exposure the previous entry raised **does not arise**. There is no prior upload inside the assessment |
| Active strikes | **None** | §21.3 precondition 3 is satisfied on its strike half. Two-step verification remains to be confirmed |
| Made-for-kids at channel level | **No** | No conflict with scope item `S-002`. Adult framing starts from a clean channel designation |

**`RK-006` resolves to the unfavourable branch, and nothing changes.** The channel is not in YPP,
so the 2027-02-01 doubling applies to it: entry needs 1,000 subscribers plus 8,000 qualified watch
hours in 365 days from that date. The plan already carries **capture treated as lost, with 8,000
watch hours as the planning figure**, and that position is now confirmed correct rather than
merely cautious. No recalculation follows, and the only fixed date stands where it was.

The practical reading: roughly eighteen weeks remain to 2027-02-01, from zero subscribers and zero
published minutes, on a rate that is not yet proven for a single item. Racing the earlier 4,000-hour
threshold would mean committing to a sustained rate before Wave 2 has measured whether one item is
affordable — which is the sequence `RK-005` and the metered-first-video constraint exist to prevent.
The recorded position holds.

**Risk removed, not deferred.** The inherited-history exposure the previous entry itemised is void
on the facts, not postponed. A blank channel carries none of it.

## D-020 — The test channel is an experiment; the company's process is unchanged

**Decision.** The channel is an **initial test channel**. The company's operating process does not
change on account of it, and will not change later. **The CEO approves channel creation.** The
departments coordinate the remaining work among themselves.

**What this confirms.** Scope item `S-012` already records the niche as *a recorded experiment to
prove the pipeline rather than the company's final business bet*, with the subject, pillars,
treatment conditions and library set held as **values the company can change** rather than as
capability wired to a subject. This decision extends that standing from the niche to the channel:
the channel is the experiment's vehicle, and re-pointing at a different channel later must not
require rebuilding anything.

**What this adds, and it is a real gap.** Channel creation becomes a **CEO-approved act**. The
closed action set the foundation operates today carries **fifteen actions and none of them creates
a channel** — Wave 1 excluded channel creation outright, so the capability is absent by design
rather than merely unexercised. Making it a CEO approval therefore means Wave 3 must:

- add a `ChannelCreate` action to the closed enumerated set;
- assign it to the **Owner role and to no other**, so that "a department creates a channel" is not
  an expressible action rather than a forbidden one, which is the form decision D-003 requires;
- record the approval the same way the publication gate records the owner's, so that *which channel
  was created, when, and on whose approval* is answerable from the record alone.

**The boundary that must not drift.** "The departments coordinate the remaining work among
themselves" governs **inter-departmental coordination**. It does not touch **D-002**, under which
the CEO approves **every publication individually**, and it does not touch **D-007**, which makes
that approval a state in the gate transition table and absent from configuration. The CEO's
statement that the process does not change is what keeps both standing. Recorded explicitly here
because a later reader meeting "departments work with each other" without this sentence could take
it for a relaxation of the publication gate, and that gate is the load-bearing control in the
entire design: it is layer two of the four least-privilege layers, it is the precondition of every
release credential, and it is the first of the four non-cuttable controls `RK-001` names.

## Still open after this decision

`RK-001` controls before first publication · `RK-002` price re-fetch before any spend, next policy
pass due **2026-10-26** · `RK-003` accepted by the CEO at D-018, its two disclosure questions —
Vietnamese advertising-law disclosure and whether any Vietnamese rule addresses synthetic-media
disclosure — unanswered and due at **first publication** · `RK-005` approval-minutes baseline,
which Wave 2 produces · `RK-006` confirmed at the 8,000-watch-hour branch.

Two-step verification on the test channel is unconfirmed and is the remaining half of §21.3
precondition 3. Wave 2 open questions `Q-001`, `Q-004` and `Q-008` remain with the CEO.

## D-021 — The two Vietnamese disclosure gaps stay carried; affiliate disclosure becomes a standing rule

**Decision, two parts.**

1. **Vietnamese advertising-law disclosure obligations, and any Vietnamese rule on AI-media
   disclosure, remain CARRIED at their recorded status.** They are not escalated, not blocking,
   and no work waits on them.
2. **Affiliate and paid-promotion disclosure is always made.** The company declares paid promotion
   in the platform's own Studio control **and** discloses in-video, on every item that carries an
   affiliate link or a sponsored placement, without first assessing whether any rule requires it.

**Why part 1 is the correct disposition rather than an omission.** The re-verification records both
gaps as *"still not established… it needs local counsel, not a fetch"*, and the language analysis
records that for an **English-language channel aimed at Tier-1 audiences** the gap is a
**background item**, becoming *"immediately load-bearing"* only for a Vietnamese-language channel
aimed at Vietnamese viewers. D-008 chose English, and one of the stated reasons was precisely that
choosing Vietnamese *"would promote an unresolved legal gap onto the critical path before launch."*
Carrying these two is therefore the position the plan was already built to hold, not a deferral of
something that should have been done.

**Why part 2 resolves the gap that is actually load-bearing.** The re-verification names the
platform's affiliate-link position — whether an affiliate link alone triggers the paid-promotion
declaration — as *"the one carried gap that is directly load-bearing for the plan's revenue
model."* It is unresolved at source: neither the paid-promotion page nor the Branded Content
Policy addresses affiliate links.

This decision does not resolve that uncertainty. It **removes the company's exposure to it**, which
is better, because the uncertainty is not the company's to resolve:

- Disclosing when no rule requires it costs nothing. The dossier confirms the declaration **does
  not affect reach or earnings**.
- Not disclosing when some rule does require it is a compliance failure on a channel where
  enforcement reaches the account.
- The asymmetry is total, so no per-item judgement is worth making. **Always disclosing is
  correct under every branch of an uncertainty nobody has resolved.**

The dossier also records, as **[INFERENCE] and explicitly not verified against a first-party
source**, that US endorsement rules would require affiliate disclosure independently of platform
policy — which would bear on this company, since an English Tier-1 channel means a US audience.
This decision is deliberately **not** made on that inference. It holds whether or not the inference
is sound, which is why it does not need to be verified before acting.

**What this settles in Wave 2.** Scope item `S-008` requires five compliance determinations, one of
which is **affiliate and paid-promotion disclosure**. That determination now has a fixed answer
rather than a per-item judgement: where the item carries an affiliate link or a sponsored
placement, disclosure is made both ways; where it carries neither, the determination records that
neither is present. Either way it resolves to recorded evidence rather than to *"cannot be
evidenced"*, which is one of the five discharged.

**What this means for the build, and it is a standing rule rather than a setting.** Following the
form the rest of the design takes, this is not a configuration value:

- Wave 3's publishing agent sets platform-specific settings, and the paid-promotion declaration is
  one of them. It is set **because the item carries an affiliate link**, not because a flag says to.
- The publication gate refuses a releasable state for an item that carries an affiliate link with
  no recorded disclosure, in the same way it refuses one whose assets lack a permission basis. The
  refusal names what is missing.
- The in-video half is a **production** obligation, not only a publishing one: an item that must
  carry a spoken or on-screen disclosure has to be produced with it, so the obligation reaches
  Wave 2's pipeline and not just Wave 3's upload.

This sits alongside, and does not disturb, the four controls `RK-001` records as not cuttable for
cost. It is a fifth standing rule of the same kind: adopted because it is free, kept because
dropping it would only ever save nothing.

**One question this raises, for the CEO.** Whether the **first item** carries an affiliate link at
all is not settled. The revenue model is platform advertising plus affiliate links, but the first
item is a pipeline experiment, and affiliate revenue is recorded as expected to be weak in this
subject. If the first item carries no affiliate link, the determination resolves trivially and the
in-video production obligation does not arise for it. Recorded as open rather than assumed either
way.

## Still open after this decision

`RK-001` controls before first publication · `RK-002` price re-fetch before any spend, next policy
pass due **2026-10-26** · `RK-003` accepted at D-018, its two disclosure questions **carried by
this decision** · `RK-005` approval-minutes baseline, which Wave 2 produces · `RK-006` confirmed at
the 8,000-watch-hour branch.

Two-step verification on the test channel is unconfirmed. Whether the first item carries an
affiliate link is open. Wave 2 open questions `Q-001`, `Q-004` and `Q-008` remain with the CEO.

## D-022 — The first item carries no affiliate link

**Decision.** The first item carries **no affiliate link and no sponsored placement**.

**What it settles.** The open question D-021 raised is closed. Of the five compliance
determinations scope item `S-008` requires, the **affiliate and paid-promotion** one now resolves
for this item by recording that neither is present — recorded evidence, not a finding that it
cannot be evidenced. The **in-video disclosure obligation does not arise**, so Wave 2 has nothing
to produce for it and the item's treatment is unaffected.

**What it does not change.** The standing rule at D-021 is untouched. It does not bind on this item
because its condition is absent, which is not the same as being relaxed: the first item that does
carry an affiliate link discloses both ways, with no fresh decision required.

**Consequence, stated because it is easy to miss.** The first item now has **no revenue path at
all** — none from platform advertising, which needs programme entry, and none from affiliate. It is
a pure cost and a pure measurement. That is what Wave 2 is for, and it matches the recorded
position that the annual licensing commitment is not signed until this wave's output has been
metered. It also means open question `Q-006`, the per-item cost target, will be settled against
cost alone, with no revenue side to weigh it against.

## Still open after this decision

`RK-001` controls before first publication · `RK-002` price re-fetch before any spend, next policy
pass due **2026-10-26** · `RK-003` accepted at D-018, its two disclosure questions carried at
D-021 · `RK-005` approval-minutes baseline, which Wave 2 produces · `RK-006` confirmed at the
8,000-watch-hour branch.

Two-step verification on the test channel is unconfirmed. Wave 2 open questions `Q-001` (subject
and angle), `Q-004` (budget for a second stock library) and `Q-008` (audience-drift threshold)
remain with the CEO.


---

## D-023 — Renumbering adopted, with allocation blocks to prevent recurrence

Decided 2026-09-26 by the CEO, approving the recommendation in `C-004`. **This is the first
identifier issued under the scheme it adopts.**

**Supersedes the scheme briefly recorded here earlier**, which rested on a misreading: this
session read "follow the proposal" as approving the no-renumbering option in `C-004`, and the
CEO clarified that renumbering was what was meant. That entry was allocated as `W0-D-018` and
is renumbered here to `D-023`, both because the prefix scheme it announced is withdrawn and
because `D-018` now belongs to the Wave 1 session. Nothing cited it.

**Decision.**

1. **The Wave 1 session's renumbering stands.** Its `D-011`-`D-015` are `D-018`-`D-022`, on
   `main` at `21e9882`, with withdrawal markers carrying the mapping. This branch's
   `D-011`-`D-015` are unchanged and are now unambiguous.
2. **One flat `D-nnn` space is kept.** No wave prefixes.
3. **Each session draws from its own reserved block**, per the table at the head of this
   record. This is what stops a fourth collision, which renumbering alone does not, and it
   needs no coordination between sessions.
4. **The concordance at the head becomes a what-moved table** rather than a two-column
   ambiguity table, since there is no longer an ambiguity to resolve.

**One correction carried forward rather than buried.** `C-004` argued against renumbering
because existing citations live in gated artifacts. The Wave 1 session checked and showed that
for its five this was not true - those citations belonged to the technical design's separate
`D-nnn` namespace, not to this record. The argument was general where the facts were specific,
and the check should have preceded the argument.

**Standing obligation this creates.** The underlying defect was never the collision; it was that
three sessions wrote to one authoritative record with **no identifier-allocation mechanism**, and
that the first two collisions were caught only because the sessions happened to be talking. Renumbering fixes the past; the reserved blocks fix the future. Any session joining later takes
the next free hundred and coordinates with nobody.

---

## C-005 — Two conflicting CEO instructions on the numbering fix. NOT RESOLVED.

Recorded 2026-09-26. **This session is not resolving this and has changed nothing.** A third
session acting unilaterally would produce a third variant, which is the failure mode both other
sessions have already declined.

**The conflict.** The CEO gave two different instructions, in two sessions, to two agents:

| Told to | Instruction | Acted on? |
|---|---|---|
| This session | approve `C-004`: **nothing is renumbered**, concordance plus `W0-`/`W1-`/`W2-` prefixes | Yes, `ad30589` |
| Wave 1 session | option A, **that side renumbers** | Yes, `21e9882`, its `D-011`-`D-015` moved to `D-018`-`D-022` with withdrawal markers carrying the mapping. **Fast-forwarded to `main`**, verified here |

Both were followed in good faith. They cannot both stand.

**A correction I owe, because it undercuts my own stated reason.** `C-004` rejected renumbering
because existing citations live in artifacts that have already passed their gates. The Wave 1
session checked, and **for its five that is not true**: every bare `D-011`-`D-015` citation on
its branch belongs to the *technical design's own* `D-nnn` namespace (its `D-011` is "a retry is
its own operation record"), not to the CEO record. Renumbering the CEO record touched none of
them. The only artifact citing its CEO `D-011`-`D-015` was Wave 2's `carried-decisions.md`,
corrected at the time. **My general argument does not apply to their specific case.** It does
still apply to mine, since `D-013` is cited in ticket MC-3, which `S-006` of a gated scope
definition inherited.

**Recommended resolution: take both, because they solve different problems.**

The renumbering is already done, already on `main`, and it *does* resolve the existing
collision: with their five moved, a bare `D-011`-`D-015` unambiguously means this branch's.
Reverting buys nothing and costs a revert.

But renumbering does **not** prevent the next collision. Only an allocation mechanism does. So:

1. **Accept the Wave 1 renumbering as the resolution of the existing collision.** Do not revert.
2. **Keep the namespace for everything new**, which is what stops this recurring.
3. **Rename this session's `W0-D-018`**, which now reads confusingly beside their renumbered
   `D-018`. Nothing cites it, so this costs nothing.
4. **Rewrite the concordance** to record what actually happened: which identifiers moved, where
   to, and why the table exists.

**One more collision that neither scheme fixes, flagged by the Wave 1 session and it is right.**
The technical design and the CEO record **both** use bare `D-nnn`, for architecture decisions
and business decisions respectively. A reader meeting `D-011` cannot tell which document it
belongs to. A *wave* prefix does not help, because the ambiguity is by **kind**, not by origin.
The fix is a kind prefix, `CEO-D-nnn` against `ADR-D-nnn`, possibly alongside the wave prefix.
Fold it into whichever scheme the CEO settles on, rather than meeting it later as a fourth
collision.

## C-006 — Wave 1: the router is built and verified; the thesis is untested; the instrument was broken

Recorded 2026-09-26 from the Wave 1 session's report. `main` verified here at `21e9882`
carrying 85 source and schema files.

**`C-003` stands unchanged.** The router `M-001` is the **media company's** capability boundary,
not the framework's. Wave 1 did not touch the framework's own agents, which remain
`model: inherit`. If the question is the framework's own token spend, Wave 1 leaves it exactly
where `C-003` found it.

**What exists and is verified.** Capability requests resolve over a three-tier admitted route
table, primary then secondary then emergency, with the **quality floor filtered before tier
ordering**, so a below-floor route is never a candidate rather than a rejected one. Per
operation it records route, model, capability class, the full attribution tuple, and one row per
attempt including failures and retries. Cost is a stored generated column computed from unit
counts and the unit prices **snapshotted onto the row**, so it stays re-derivable after a price
is superseded. 220 tests pass against a real PostgreSQL 17.

**What has never happened.** **Zero provider endpoints are configured. No live provider call has
ever been made. The cost table has never held a row produced by a real operation.** There is
**no measured cost from Wave 1 at all** - not a small number, none.

**The finding that matters most, and it strengthens `C-003` rather than softening it.** The
cost-recording path was **broken and nobody knew**: the insert used `MAX()` on a uuid column,
which PostgreSQL has no function for, so it threw `42883`. That defect **passed 205 unit tests,
a twelve-finding review, the Review Gate and the Verification Gate**, and was caught only when a
real database was put behind it, *after* the wave had been declared verified. Seven of fifteen
integration tests failed on that one defect.

**Read it as a finding about the quality system, not about one bug.** The governed pipeline
declared verified something that could not work. Test coverage and gate approval are not
evidence that a mechanism functions; only exercising it against the real dependency is. So
`C-003` is **understated**: the cost-control thesis is untested *and* the instrument built to
test it was broken on arrival.

**One cost control that is genuinely verified, and it needs no tiering.** Design decision
`D-005` makes rule-determined work **structurally incapable** of a model call: the deterministic
assembly holds no reference, direct or transitive, to the capability boundary or the credential
broker, so a model call is not expressible from it. Enforced by a build-time boundary test that
was **mutation-checked** - introducing a real cross-boundary dependency made three assertions
fail, removing it made them pass. Thirty-one named tasks sit inside that boundary. It works by
removing the possibility rather than by choosing a cheaper model, which is why it holds without
any of the tiering `C-003` finds absent.


---

# Wave 2 session decisions — block `D-200`–`D-299`

## D-200 — The honey bee subject stands on a corrected supply premise

**Decided 2026-09-26 by the CEO**, during Wave 2 (`run-3a58551ee912`), after the implementation
phase raised the correction at its `Q-002` and the Wave 2 session verified it independently.
**Closes `Q-002`.**

### The error, stated plainly

`D-017`'s supporting reasoning records **`honey bee` = 1,213 as "a single, distinctive,
unambiguous token, so it is one of the counts §3.3 classifies as trustworthy."** That is
**false**. `honey bee` is a **two-word query**.

`research/animal-niche-analysis.md` §3.3 states the rule verbatim: *"Only single, distinctive,
unambiguous tokens give a trustworthy read."* Its enumerated trustworthy list is nine single-token
species — pangolin, narwhal, bowerbird, okapi, platypus, binturong, cassowary, axolotl, tapir —
and **`honey bee` is not among them**. Seven further rows in the same table are multi-word and
carry the same defect (`grey wolf`, `humpback whale`, `bengal tiger`, `mantis shrimp`, `snow
leopard`, `naked mole rat`, `emperor penguin`), so the table's own header is inaccurate for eight
of its rows.

**The error was the Wave 2 session's**, made when presenting the options and repeated into
`D-017`'s reasoning and into the implementation dispatch. The implementation agent found it by
reading the source rule rather than accepting the instruction, and **declined to re-read the
evidence to agree with its brief** — which is the behaviour that caught it.

**It was material to the choice.** Two of the four options presented did have genuinely
trustworthy single-token priors — **`octopus` 1,582 and `elephant` 4,131** — and the recommended
option did not. The CEO chose on a supply claim that was wrong.

### The decision

**The subject stands. The supply premise is corrected, not repaired.**

**Basis:**

1. **`D-017`'s four other reasons are untouched** — distance from the generative-AI-contaminated
   rare-species tail, the originality burden landing where the channel can carry it, the lowest
   made-for-kids exposure of the options, and attributable science.
2. **No subject has a verified count, so switching buys a better *prior*, not a better *fact*.**
   The implementation phase audited **14 subjects and obtained zero counts** — the committed
   library requires an authenticated subscription and this role may not reach an external system
   directly. Changing subject would exchange one unverified position for another with a
   better-quality dated observation behind it.
3. **The exposure is structurally bounded, not merely accepted.** Only **three** waggle-run clips
   are unsourced, and **no claim in the script depends on them**: `GFX-02`, `GFX-03`, `GFX-04`,
   `GFX-15`, `GFX-16`, `GFX-17` and `GFX-18` carry those beats. If per-clip confirmation finds
   nothing, the three drop and nothing is lost. **Substituting generic hive activity is refused
   at the script**, which is what stops the failure mode the niche analysis warned about.
4. **Switching costs a full phase-4 rebuild** (~366,000 tokens plus rework) for that better prior.

### What is corrected, and what is not

- **Corrected:** supply for this subject is **unestablished for both the species and the
  behaviour**, not merely for the behaviour. `honey bee` = 1,213 is a **dated multi-word upper
  bound of unknown looseness**, admissible only as a prior observation with its date, never as a
  measurement.
- **Not corrected, because it was already right:** `waggle dance` is a multi-word phrase whose
  reported total is inadmissible, and supply for the behaviour must be confirmed clip by clip.
- **Still owed at `A-024`:** a per-subject count obtained first-hand before the script is
  committed. The script is committed; the count is not obtained. **That is a recorded gap, and it
  is the honest state of the wave.**

### The general lesson, recorded so it outlives this item

**A rule that classifies evidence is only as good as the check that the evidence meets it.** The
single-token rule was stated correctly, carried correctly into the analysis, and then applied to
a two-word query by three successive readers — the session presenting the options, the CEO
deciding on them, and the dispatch instructing the work — because everyone checked the *number*
against the rule and nobody checked the *query*. It was caught by the one reader whose brief told
it the answer and who went to the source anyway.


---

## D-201 — Wave 3 exercises the approval surface on the held item; the real series waits

**Decided 2026-09-27 by the CEO**, answering the blocking question `Q-001` of Wave 3's scope
definition (`run-ad369fe67ded`). **Closes `Q-001`.**

### The problem, which was an error in the ticket

MC-4 named "the real approval-minutes series" as a proof obligation of Wave 3. That obligation is
**unsatisfiable under Wave 3's own boundary**, and the scope agent blocked rather than absorbing
it:

> approval occurs at the publication gate → the gate is reached only by a publish-ready item →
> publish-ready requires a rendered cut → rendering requires commissioned narration audio and
> downloaded licensed clips → **both are spend, which Wave 3 does not commit.**

The ticket asked a wave to prove something its own constraint forecloses. **The error was in the
ticket, not in the boundary**, and the boundary is not moved to fix it.

### The decision

**Approvals are exercised against the built surface using the held item.** Every such approval is
measured, with review, queue and rework time separated.

**They are recorded as "approval-surface exercise", never as the real approval-minutes series.**
The distinction is load-bearing and must survive into every downstream artifact.

### Why this and not the alternatives

- **It tests what Wave 3 actually builds.** The value is in the *surface* — the gate transition
  table, the version binding, the refusal of a configuration bypass, the refusal of an approval
  bound to a different version, the refusal under retry, and the instrument itself. All of that
  is exercisable on a held item, and a defect found here is cheap to fix.
- **It costs nothing** and keeps the boundary the CEO set when authorising Wave 3 intact.
- **Dropping the obligation** would be honest but would leave the approval surface untested until
  the wave that publishes, where a defect is expensive.
- **Authorising the render** would move the boundary. Not chosen.

### The limit, stated so no later reader over-reads the figures

These measurements are of the **same weak class** as Wave 2's single data point: an owner
approving an item that **cannot ship**, who already knows its contents, with no queue time. They
establish that the instrument and the surface work. **They do not establish steady-state review
cost, and the clean-record threshold that would relax per-publication approval must not be
derived from them** — `D-002` requires that threshold to be proposed, not assumed, and `Q-008` of
the Wave 3 scope holds it open until a series exists that includes at least one approval which
returned a change request.

---

## D-202 — One metered operation may run through the resolution boundary, as a deliberate carve-out

**Decided 2026-09-27 by the CEO**, answering `Q-006` of Wave 3's scope definition.

### The decision

**At least one real operation may be run through the capability-resolution path during Wave 3, so
that a served reasoning tier is recorded.** This is a **narrow, named exception** to Wave 3's
no-spend constraint, not a relaxation of it.

**Everything else in the no-spend boundary stands unchanged:** no upload, no channel creation, no
account creation, no purchase, no subscription session, no render.

### Why it is worth an exception

`C-003` established that **the company's entire cost-control argument rests on a ratio nothing has
ever tested.** The approved budget assumes 290,000 input tokens at the high reasoning tier against
87,000 at the lower ones; `M-001` is the boundary designed to read a requested tier and route on
it; **it has never executed.** Every figure derived from that split — USD 2.647440 per item and
the USD 77.41 envelope — is therefore an assumption rather than a measurement.

Wave 3 is the first wave in a position to run it. The cost of doing so is negligible — well under
one cent for a single operation. **The cost of not doing so is another wave of sunk effort resting
on an untested assumption**, and the same question arriving later with more behind it.

### What this must produce

- **At least one operation record carrying both the tier requested and the tier actually served**,
  with the served tier read from the admitted route and **never inferred from the request**.
- **An explicit absence marker** where a route cannot state a tier, so an untiered route is
  visible as untiered rather than appearing to confirm the request.
- The applied unit price carrying a re-fetch date no earlier than the operation it priced
  (`RK-002`).

**This does not settle the 290,000/87,000 ratio.** One operation establishes that the mechanism
records what it claims to record. The ratio needs a production series. **Say that plainly rather
than presenting the first record as vindication of the budget.**


---

## CEO-D-203 — Source-qualified identifier prefixes, adopted for documents and NOT sufficient for framework artifacts

**Decided 2026-09-27 by the CEO**, after the by-kind ambiguity bit a third time. **This is the
first identifier issued under the scheme it adopts.**

### The decision

**New identifiers carry a source qualifier:**

| Family | Prefix | Meaning |
|---|---|---|
| Business decisions in this record | **`CEO-D-nnn`** | The owner's decisions |
| Architecture decisions in a technical design | **`ADR-D-nnn`** | Design decisions |
| Corrections in this record | **`CEO-C-nnn`** | Corrections to instructions or prior records |
| Change-set entries in an implementation report | `C-nnn`, unprefixed | Local to that report, never cited elsewhere |

**New identifiers only. Nothing already written is renumbered or renamed**, on the same reasoning
that settled the earlier fork: citations live in artifacts that have passed their gates, and a
silent move is worse than a visible mixed period.

### ⚠ IT DOES NOT DO WHAT IT APPEARS TO DO INSIDE FRAMEWORK ARTIFACTS

**Recorded prominently because the decision was taken expecting it would, and it will not.**

The validators scan with `` + prefix + `-\d{3}`. **A hyphen is a word boundary**, so a
qualifier in front of the token does not hide the token. Tested directly against the validator's
own pattern:

```
SD7   prefix 'C' MATCHES 'CEO-C-001'      <- still an authority-boundary failure
C6.2  family 'D' MATCHES 'CEO-D-203'      <- still an undefined-identifier failure
C6.2  family 'D' MATCHES 'ADR-D-001'      <- still an undefined-identifier failure
```

Worse, **`ADR-` is itself one of the three prefixes the authority check already scans**, so that
qualifier sits inside a namespace the framework has reserved for something else.

### What therefore holds

- **In repository documents** — this record, research files, ledgers, reports, plans — **the
  prefix works and is adopted.** It resolves the by-kind ambiguity for every human reader and for
  every cross-document join. That is a real gain and it is why the decision stands.
- **Inside framework artifacts** — anything under a run's `artifacts/` — **descriptive citation
  with no foreign identifier token of any family remains mandatory.** The prefix changes nothing
  there. An artifact citing `CEO-D-203` fails exactly as one citing `D-203` does.

### The cost this leaves unpaid, stated rather than hidden

The Wave 3 scope agent argued that descriptive citation **degrades as documents accumulate**: a
reader checking that a scope decision faithfully carries the owner's carve-out must currently find
it by description, which works while one obvious match exists and stops working once the record
holds several decisions on the same subject. **That argument is correct and this decision does not
answer it for framework artifacts.** It answers it only for documents.

### The fix that would actually close it — upstream, and now specified

Either:

1. **A declared `external_references` block** in the artifact contract, which `C6.2` and the
   authority checks both exempt — so a cross-document citation becomes *expressible* rather than
   merely undetectable; **or**
2. **Namespace-aware resolution**: have both checks resolve a citation's namespace before judging
   it, rather than pattern-matching the bare token.

Option 1 is the smaller change and the one to propose. **Until one ships, the split rule above is
the working answer**, and it should be stated in the agent output contracts so every agent does
not rediscover it by failing validation.

### How this was established

The ambiguity was recorded as open on 2026-09-26 with the note that it would be resolved by
prefixes *"if it starts to bite"*. It bit three times: twice in Wave 2 as citations resolving
silently to the wrong definition, and once in Wave 3 as a **legitimate upstream correction being
read as a downstream change-set entry, failing an authority-boundary check** — one level more
serious than a misresolution, because the artifact was judged to have exceeded its authority.

The insufficiency above was found by **reading the validator source and running its own regex**
before implementing the decision, rather than adopting the scheme and discovering it at the next
validation failure.

---

# Wave 4 session decisions — block `CEO-D-300`–`CEO-D-399`, corrections `CEO-C-300`–`CEO-C-399`

**This session takes `CEO-D-300`–`CEO-D-399` for decisions and `CEO-C-300`–`CEO-C-399` for
corrections**, under the allocation-block rule and the source-qualifier scheme. The previous
session's block ran to `CEO-D-203`. Nothing is renumbered.

The `CEO-C-3nn` range is taken explicitly because the token `CEO-C-001` already appears in this
record as an illustration inside the prefix decision's validator table, and a block starting at
300 cannot be confused with it.

---

## CEO-D-300 — The authorised no-spend exception does not extend to a further metered operation

**Decided 2026-09-27 by the orchestrating session at the Wave 4 Scope Gate**, answering the
blocking question the scope phase raised rather than absorbed. **Not a CEO decision — recorded
here because it interprets one, and the CEO may overturn it.**

### The question

The scope agent refused to set an acceptance threshold for *observing* a served reasoning tier,
because it could not do so without either absorbing owner-reserved spend or promising something
the route might not deliver. It asked: if the admitted route reports no served tier, does the
single authorised exception cover a **further** metered operation, or does the wave close with an
absence marker and no observed tier?

### The decision: no further operation

**Basis, and the reason this did not need to go to the CEO.** The decision authorising the
exception already requires *an explicit absence marker where a route cannot state a tier, so an
untiered route is visible as untiered rather than appearing to confirm the request*. That is the
owner having **already accepted the absence outcome as a legitimate product of the exception**.
Reading it that way settles the question without expanding any authority.

The expansive branch — spending a second operation — **remains the CEO's** and was not taken.

### What actually happened, which made the question moot

Nothing was spent at all. See `CEO-D-301`.

---

## CEO-D-301 — The served-tier obligation was met at zero cost, and the exception remains unspent

**Recorded 2026-09-27 by the orchestrating session.** This is a finding, not an authorisation.

### What was found

Reading the delivered capability boundary showed that **the served reasoning tier is a property of
the admitted route, not of a provider's response.** Every outcome path — hold-and-escalate,
non-AI-substitute, missing-adapter, credential-refused **and** the provider path — writes the same
field from the route. The two paths that reach no admitted route write null, correctly.

So the evidence the exception existed to buy was obtainable by **executing the delivered boundary
against a live record store over an admitted route**: no provider account, no endpoint, no
credential, no purchase, no subscription, and no weakening of the empty-endpoint position. Both
tiers were read back **out of the store**, deliberately different from each other so that a value
copied from the request would have been visible.

### The position now

- **Spend committed in Wave 4: USD 0.00.** The exception is **unspent and still available.**
- The plan criterion requiring *exactly one metered operation* is **not satisfied as written** —
  zero were run. The obligation behind it is satisfied. Both halves are published.
- **The assumed reasoning-tier split is untouched and still unmeasured.** One record establishes
  the recording mechanism, not the ratio. **Third consecutive wave closing with that true.**
- The record came from a **non-provider** path. That the provider path behaves identically rests
  on it reading the same field — a property of the source, not an observation of a provider.

### The limit that decides what this is worth today

**The `routes` table declares no reasoning-tier column at all**, and the route registry constructs
every route without one. On the **fully composed production path, every recorded served tier is
the absence marker**, whatever route serves it. The mechanism is proven; the production path
cannot yet feed it. Adding that column and its adapter is the highest-value single change for this
line and is the first thing Wave 5 should weigh.

---

## CEO-C-300 — A hard constraint was mistaken for a wording problem, by the orchestrator

**Recorded 2026-09-27. Found by the Wave 4 peer review. The error was the orchestrating
session's.**

### The error

The repository has no continuous-integration workflow, and the architecture boundary suite is an
ordinary test project. The orchestrator discovered this, and briefed both the implementing and
reviewing roles that the claim *"the build refuses"* was **overstated wording** to be corrected.

**That understated it.** The accepted design's hard constraint requires the no-model-path property
to be **proved by a build-time check rather than asserted**; the execution plan's task criterion
requires the check to **run as part of the build rather than on request**; and the ticket itself
says deterministic work is **verified by the build-time boundary test**. None of the three held.
A declared hard constraint and an accepted acceptance criterion were both **defeated**, not merely
described loosely — and the change account had recorded the condition only as an open question
about who should own a runner.

### The correction

The reviewer raised it as a high finding with a blocking correction request. As tech lead the
orchestrator decided to **make the property hold rather than restate it**. A build target now runs
the boundary assertions against the just-built assembly and fails the build when they fail,
bypassable only through a named and visible property. Verified by execution, including that an
incremental build does not skip it.

Two limits are recorded rather than claimed closed: **a reference declared and never used still
passes**, because the assertions read retained references rather than the project file, and **only
a solution build starts the target**, not a single-project build.

### The lesson, recorded so it outlives this wave

**An orchestrator who discovers an inconvenient fact is the person least likely to notice that it
defeats a constraint rather than merely complicating a sentence.** The orchestrator found this
fact, correctly, and then filed it under the wrong heading — as a thing agents should phrase
carefully — because that framing required nothing to change. The reviewer, reading the constraint
and the fact side by side with no stake in either, saw at once that one negated the other.

Two further orchestrator errors were caught the same way in the same run: the planner reconciled a
file count the orchestrator had stated wrongly, and the architect corrected an approved assumption
about what the boundary suite covered. **Every recorded objection in this programme has been
correct. The count is now in double figures and the record is unbroken.**

---

## CEO-C-301 — The Wave 3 token-accounting correction is confirmed, and no longer an inference

**Recorded 2026-09-27.**

Wave 3 corrected Wave 2's reported token total downward by 42% on the reasoning that a **resumed
agent's reported figure is cumulative for that agent, not incremental for the attempt**, and
explicitly labelled that an inference from arithmetic that should be withdrawn if wrong.

**It is confirmed.** Wave 4 resumed three agents and measured two independent meters that do not
consult each other — the harness figure and the agent's own remaining-budget counter. They agree
to within 1% in every case, and one agent described its own second figure, unprompted, as *"for
this agent as a whole"*.

**The rule stands: take each agent's final figure; the rework is the delta.** Wave 2's corrected
total of 1,836,117 and Wave 3's 1,930,677 are the figures of record.

---

# Wave 5 session decisions — block `CEO-D-400`–`CEO-D-499`, corrections `CEO-C-400`–`CEO-C-499`

**This session takes `CEO-D-400`–`CEO-D-499` for decisions and `CEO-C-400`–`CEO-C-499` for
corrections**, under the allocation-block rule and the source-qualifier scheme. The previous
session's block ran to `CEO-D-301` and `CEO-C-301`. Nothing is renumbered.

---

## CEO-D-400 — No sizing claim until a production series exists; a queue demonstration is a capability demonstration, not evidence of the rate

**Decided 2026-09-27 by the orchestrating session at the Wave 5 Scope Gate**, answering the
blocking question the scope phase raised rather than absorbed. **Not a CEO decision — recorded
here because it interprets existing ones, and the CEO may overturn it at the wave report.**

### The question

The scope agent bounded the instrumentation half of the ticket's goal and refused to bound the
other half — *build what a sustained three-items-per-week rate would require of the system* —
because stating that requirement as an acceptance threshold needs a cycle time, a failure rate
or a rework rate, none of which has ever been observed, and the same ticket forbids inventing
a quantity. It asked: what does the owner accept as evidence that the system can sustain the
rate — a demonstration of queue behaviour at a depth recorded as an assumption, or does any
sizing claim wait until a production series exists?

### The decision: the sizing claim waits; the demonstration is admitted only as capability

1. **Any sizing claim, required buffer depth, concurrency figure or sustainable-rate assertion
   waits until a production series exists** that includes observed cycle time, failure rate and
   rework rate. Nothing in Wave 5 asserts that the system can sustain three items a week.
2. **A demonstration of queue behaviour is admitted as a capability demonstration only.** The
   depth it runs at is a **demonstration parameter**, recorded and labelled as an assumption in
   every artifact that carries it. No acceptance threshold and no claim about the rate may be
   derived from it, and no artifact may present it as a measurement.
3. The wave therefore delivers the rate as **testable, not tested** — the same shape as Wave 3
   (publishing capability built, nothing published) and Wave 4 (analytics built, revenue dark).

### Why this did not need to go to the CEO first

The owner's standing decisions already settle it. The master plan states that *the decision to
sustain follows the approval-minutes baseline rather than preceding it*; the ticket forbids
inventing any quantity and requires the rate to become decidable *on observation rather than on
assumption*; and the clean-record threshold — the nearest relative of this question — is
already reserved to the owner until a series exists. Reading those together, a sizing threshold
asserted now would contradict all three. The restrictive reading is taken; the expansive
reading — accepting a demonstration depth as evidence of the rate — **remains the CEO's** and
was not taken.

### What the CEO is asked at the wave report

Whether this reading stands. If the CEO would rather accept a demonstration depth as
provisional evidence, that is a new decision and gets its own identifier; nothing here is
renumbered.

---

# Wave 6 session decisions — block `CEO-D-500`–`CEO-D-599`, corrections `CEO-C-500`–`CEO-C-599`

**This session takes `CEO-D-500`–`CEO-D-599` for decisions and `CEO-C-500`–`CEO-C-599` for
corrections**, under the allocation-block rule and the source-qualifier scheme. The previous
session's block used only `CEO-D-400`. Nothing is renumbered.

---

## CEO-D-500 — Channel isolation: one legal entity, several channels

**Decided 2026-10-08 by the CEO**, in the session that runs Wave 6, before the run's scope
phase, answering the question put in the Wave 5 wave report: *settle channel isolation now —
one legal entity or several — or build configuration flexible to both answers?* **This is the
owner's decision, not the orchestrator's reading.**

### The decision

**The company operates every channel under one legal entity.** There is one payee and one
payment account, shared by every channel. Channels are separated by configuration, not by
corporate structure: per-channel audience, brand, voice, content strategy, schedule, budget,
library registration, risk profile, analytics partition and approval queue, exactly the
per-channel list of master plan section 17.5.

### What it discharges, and what it does not

1. **It discharges the cross-wave condition that channel isolation is settled before, not
   during, the multi-channel wave.** The isolation question is closed: isolation is not
   structural at the payee.
2. **It does not discharge the approval-workload re-examination**, which still needs the
   approval-minutes series that does not exist. No second channel is created, configured as
   live or routed to in Wave 6; the ticket's scope stands.
3. **It authorises no spend, no account, no channel creation and no publication.** The payment
   account is still owed by the owner and still blocks first publication; under this decision
   there is exactly one such account to open, not one per channel.

### The consequence the system must carry

Master plan section 17.5 records, as an inference, that genuine isolation between channels
requires distinct payees. Under one payee the channels are **related** for the platform's
monetisation and enforcement purposes: an enforcement action against one channel may reach the
others. The decision accepts that exposure. The system must therefore model the payee as a
**company-level** fact shared by every channel, never as a per-channel value that could be
configured to differ, and the per-channel risk profile must be able to state that the channels
share one payee. Budget remains per channel for tracking and the company budget of USD 77.41
per month remains the ceiling across all of them.

---

## CEO-C-500 — Correction to CEO-D-500's wording on the per-channel list

**Recorded 2026-10-09 by the orchestrating session.** `CEO-D-500` described its per-channel list
as "exactly" the list of master plan section 17.5. It is not: the decision's list adds voice,
analytics partition and approval queue, and omits language, tone and visual identity. The scope
agent and the documentation agent both caught the difference. **The correct reading, used by the
whole of Wave 6, is the union of both lists**, with one qualification the Scope Gate made: the
approval queue is a per-channel *view* over the gate transition-table state, never configuration.
The owner's decision itself — one legal entity, several channels — is unchanged. Nothing is
renumbered.

---

## CEO-D-600 — No metered benchmark in Wave 7; the AI-economics capability only

**The CEO's own decision, taken 2026-10-09 at the start of the Wave 7 session**, answering the
question put in section 7 of `wave-6/bao-cao-ceo.md`: whether to spend the authorised
single-metered-operation exception, or authorise a new small amount, on the first real benchmark.

**Answer: no.** Wave 7 builds the AI-economics capability as the ticket `MC-8` scopes it — the
benchmark record, selection that refuses to rank on an unmeasured quantity, and the deterministic
cost controller — and **makes no metered model call**. The single-metered-operation exception
stays unspent and available. The benchmark corpus stays unpopulated; every benchmark quantity in
the company's own store reads unmeasured, and every ranking the router produces is the configured
ordering, labelled as such.

### What it discharges, and what it does not

1. **It discharges the open question of whether Wave 7 runs a metered benchmark.** It does not;
   no phase may plan, design or perform one, and no scope phase need raise it as blocking.
2. **It does not discharge the tier-split question.** The assumed reasoning-tier split behind
   every per-item cost figure remains untested for the fifth consecutive wave.
3. **It authorises no spend, no account, no channel creation and no publication.**

The decision block for Wave 7 is `CEO-D-600`–`CEO-D-699` and `CEO-C-600`–`CEO-C-699`.
