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
