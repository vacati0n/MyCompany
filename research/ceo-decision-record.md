# CEO Decision Record — Framing Gate, run `run-258e0a3415d2`

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

# Fourth round — payee position settled by acceptance, not by counsel

Decided 2026-09-26 by the CEO, on a readiness check for Wave 3 that found `RK-003` to be the
longest-lead item on the critical path and the only one independent of Wave 2.

## D-011 — Operate as an individual creator; the CEO carries the legal position personally

**Decision.** The company has no budget at this stage to retain Vietnamese tax counsel. It will
operate **as an individual person publishing content on the launch platform**, with the **CEO as
the Vietnam-resident payee in his own name**. The CEO **personally accepts accountability** for
tax declaration and for the legal determinations the workforce escalates.

`RK-003` is therefore **accepted, not discharged**. The mitigation the risk register records —
*"qualified local counsel before the payment account is created"* — is **superseded** by this
decision. The risk stays open, at its recorded severity, with the CEO named as the acceptor.

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

## D-012 — Two-phase payee structure, and the launch channel already exists

**Decision, two parts.**

1. **The payee is staged, deliberately.** Channels launched now use the **CEO as an individual
   payee**, per D-011. Later channels will be created under a **company legal entity**. This is a
   recorded sequence, not a drift: the individual payee is the starting position, and the entity
   is the intended end state.
2. **The launch channel is an existing YouTube channel the CEO already holds**, rather than a
   newly created one.

**What part 1 settles.** The multi-channel isolation concern D-011 carried forward now has an
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

### D-012 resolved — the branch is established, 2026-09-26

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

## D-013 — The test channel is an experiment; the company's process is unchanged

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
pass due **2026-10-26** · `RK-003` accepted by the CEO at D-011, its two disclosure questions —
Vietnamese advertising-law disclosure and whether any Vietnamese rule addresses synthetic-media
disclosure — unanswered and due at **first publication** · `RK-005` approval-minutes baseline,
which Wave 2 produces · `RK-006` confirmed at the 8,000-watch-hour branch.

Two-step verification on the test channel is unconfirmed and is the remaining half of §21.3
precondition 3. Wave 2 open questions `Q-001`, `Q-004` and `Q-008` remain with the CEO.
