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
