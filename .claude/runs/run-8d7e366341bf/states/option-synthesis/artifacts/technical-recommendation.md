```yaml
technicalRecommendation:
  recommendationId: TR-MC15-001
  decisionReference: "MC-15 research question: affiliate pilot platform, content form and niche under CEO-D-1300"
  decisionBasis: option-analysis
  sourceInputs:
    - type: investigation-report
      reference: runs/run-8d7e366341bf/states/technical-validation/artifacts/investigation-report.md
  producedBy: tech-lead
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: provisional
  recommendedOption: O-003
  readinessDecision: proceed-with-conditions
  inputDigest: "sha256:ebf31efc76c7a3652515548dab9b5eb6"
  contextDigest: "sha256:c352a8419462923f3e7e2d58dd0271c0"
```

## Metadata

- Recommendation ID: TR-MC15-001
- Decision owner: orchestrator decides the Recommendation Gate at recommendation-draft (this phase has no gate); the CEO, as owner, rules on the pilot itself
- Requested by: CEO via /research, ticket MC-15, run run-8d7e366341bf, phase option-synthesis
- Decision date: 2026-10-26

## Decision Context

- Decision to make: Which platform, content form and niche the Vietnamese affiliate pilot should take first, and on what conditions, judged on risk, effort, cost, time to first commission and the CEO's personal time.
- Delivery constraints: One-person company; the CEO is the Vietnam-resident individual creator (D-018) and approves every publication (D-002); content in Vietnamese; budget USD 77.41 per month with a USD 34.42 metered ceiling, any change listed for owner approval; agents create no account, handle no credential, buy no sample, download nothing and publish nothing (R-008); Wave 12 3D work is frozen; conditions C1 to C12 of the architect's conditional Technical Validity Gate decision bind this phase; CEO eligibility, page re-capture and the reopened decisions stay preconditions to any CEO action (C12).
- Assumptions in force: The investigation report (provisional, confidence low) is the evidence base, read as data; no first-party commission rate exists on either platform (E-021, E-092), so every revenue number below is an ESTIMATE; third-party market items E-111 to E-119 inform demand only and are labelled; E-059, E-065, E-071, E-114 are low confidence and portal guidance is unverified (C7); first-party unquoted rules (E-024, E-076, E-084, E-085, E-087, E-089, E-101, E-102, parts of E-105, E-110) are never the sole basis of a decisive score (C8); no single PIT rate is adopted (C4); options are scored on three independent dimensions (platform O-001 to O-004, content form O-005 to O-007, niche O-008 to O-011) and the pilot combines one choice from each.

## Evaluation Criteria

| ID | Criterion | Why it matters | Priority | Source |
|---|---|---|---|---|
| `EC-001` | Compliant with platform rules and Vietnamese law under every counsel branch, with a named single-platform fallback | A breach risks account removal, liquidated damages or a legal finding against the CEO personally | must-have | Framing Gate criteria; C1, C2 |
| `EC-002` | Advertising and affiliate disclosure, and AI labels where they apply, on every publication | Disclosure is required whatever the influencer answer (Art. 23(2)(đ) E-047; E-028; E-031 to E-033; E-108) | must-have | R-014; C3; D-021 part 2 |
| `EC-003` | Cost within USD 77.41 per month and the USD 34.42 metered ceiling, any change listed for owner approval | The budget is a standing owner constraint | must-have | C11; budget record CEO-D-702 |
| `EC-004` | Content can be produced in Vietnamese with evidence it renders and reads correctly | The pilot audience and both platforms are Vietnamese | must-have | Framing Gate criteria; C11 |
| `EC-005` | Requires no CEO action before eligibility, page re-capture and the reopened decisions are settled | Synthesis cannot resolve these preconditions | must-have | C12; R-008 |
| `EC-006` | Short time to first valid commission | The pilot's purpose is to observe real earnings within 30 days | high | Dispatch criteria; R-019 to R-021, R-025 |
| `EC-007` | Low CEO personal time per published video | The CEO is the only person and the only face, hands and payee | high | Dispatch criteria; R-016 |
| `EC-008` | Low exposure to unoriginal-content, posting-limit, mass-creation, product-shown and AI-label rules | These rules remove videos, carts or accounts (E-029, E-039, E-040, E-107, E-109, E-031 to E-033, E-108) | high | C9 |
| `EC-009` | Low product-claim and restricted-category risk | Claims and category bans carry the heaviest penalties (E-036, E-037, E-050, E-106) | high | C9; R-018 |
| `EC-010` | Reversible at low cost if the pilot fails | A one-person pilot must be able to stop without a penalty (E-042; E-110 unquoted) | high | Dispatch criteria; S01 reversibility |
| `EC-011` | Demand evidence exists, labelled third-party; commission rate is not scored because it is unsourced | Demand informs the niche choice; C5 forbids scoring on third-party rates | medium | C5, C6; R-018 |
| `EC-012` | Reuses the delivered deterministic pipeline on main (src/, graphics/) rather than building new | Reuse lowers effort; adaptation belongs to architect (B-004) | medium | R-023; S01 reuse ladder |

## Options

| ID | Option | Summary | Effort | Delivery risk | Reversibility | Evidence |
|---|---|---|---|---|---|---|
| `O-001` | Platform: both TikTok Shop and Shopee | Run both; valid only if counsel or Shopee reads the competitor clause narrowly; fallback is O-003 | large | critical | costly-to-reverse | E-104, E-110 (unquoted), E-008, E-080, E-082 |
| `O-002` | Platform: TikTok Shop only | Affiliate commission needs 1,000 followers; below that Link-Share Only earns nothing from short video per low-confidence snippets | medium | high | reversible | E-006 to E-010, E-013, E-014, E-039 to E-042 |
| `O-003` | Platform: Shopee only, channel Shopee Video (KOC) | KOC has no follower minimum; paid twice weekly; 0.98% fee and 10% withholding from VND 250,000; channel status to confirm | medium | high | reversible | E-075, E-080, E-082, E-088, E-098, E-100, E-107 to E-109 |
| `O-004` | Platform: defer the pilot (status quo) | Publish nothing until every blocker is resolved; the report's defer reading, not a decision | trivial | low | reversible | IR defer reading; C12 |
| `O-005` | Form (a): real hands-only footage with samples | CEO films his own hands using a real sample, Vietnamese voice or captions, no generative model | small | medium | reversible | E-029, E-040, E-046, E-109 |
| `O-006` | Form (b): code-drawn graphics only | Reuse the deterministic renderer (graphics/render.mjs, graphics/drawing/elements.jsx) on product images and recorded facts | unknown | high | reversible | E-029, E-040, E-107, E-109; git ls-tree of main |
| `O-007` | Form (c): mix of hands-only footage and code-drawn fact overlays | Real footage carries the product; deterministic overlays show recorded facts with provenance; narration by the CEO | medium | medium | reversible | E-040, E-109, E-031 to E-033, E-108; git ls-tree of main |
| `O-008` | Niche: home and kitchen gadgets | Low-claim demonstrable products; strongest third-party demand signal | small | low | reversible | E-116, E-117 (third-party); E-036 |
| `O-009` | Niche: phone accessories | Low-claim demonstrable products; no niche-specific demand evidence | small | low | reversible | E-036; no demand source |
| `O-010` | Niche: cosmetics | High-claim; product notification and claim rules; use on skin under the influencer branch | small | high | reversible | E-036, E-050, E-106 |
| `O-011` | Niche: supplements | Banned in TikTok branded content; health-claim and doctor-imagery bans; account removal risk | small | critical | costly-to-reverse | E-037, E-042, E-106 |

## Tradeoff Analysis

| Option | Criteria met | Criteria missed | Strengths | Weaknesses | Sequencing implication |
|---|---|---|---|---|---|
| `O-001` | EC-002, EC-003, EC-004, EC-005, EC-006, EC-009, EC-011, EC-012 | EC-001, EC-007, EC-008, EC-010 | Meets 8 of 12; widest reach (E-111 third-party) | Fails must-have EC-001 under the broad clause reading; two accounts double CEO time; E-110 exposure | Only after a written narrow reading of E-104; start as O-003 and add TikTok later |
| `O-002` | EC-001, EC-002, EC-003, EC-004, EC-005, EC-009, EC-010, EC-011, EC-012 | EC-006, EC-007, EC-008 | Meets 9 of 12; no competitor clause issue; strong home-living signal (E-116 third-party) | First commission waits on 1,000 followers (E-008 to E-010); posting limit and CHR (E-039 to E-042) | Named single-platform fallback if Shopee Video is not a permitted channel |
| `O-003` | EC-001, EC-002, EC-003, EC-004, EC-005, EC-006, EC-007, EC-008, EC-009, EC-011, EC-012 | EC-010 | Meets 11 of 12; earns from the first video with no follower minimum (E-080); twice-weekly pay (E-088) | Channel status open (E-082 against E-083); E-110 termination and damages (unquoted); 10% withholding (E-100) | After CEO eligibility, page re-capture, Shopee's written channel answer and counsel answers |
| `O-004` | EC-001, EC-002, EC-003, EC-004, EC-005, EC-007, EC-008, EC-009, EC-010 | EC-006, EC-011, EC-012 | Meets 9 of 12; no cost, no exposure | Produces no commission and no measurement; the pivot CEO-D-1300 stalls | The outcome if a blocker resolves adversely with no fallback |
| `O-005` | EC-001, EC-002, EC-003, EC-004, EC-005, EC-006, EC-008, EC-009, EC-010, EC-011 | EC-007, EC-012 | Meets 10 of 12; satisfies the adverse influencer branch by design; original and product shown | Highest CEO filming time; no pipeline reuse; sample cost on every product | Samples bought after preconditions and owner budget ruling |
| `O-006` | EC-002, EC-003, EC-005, EC-007, EC-009, EC-010, EC-011, EC-012 | EC-001, EC-004, EC-006, EC-008 | Meets 8 of 12; lowest CEO time; reuses delivered renderer | Seller-image rights open; mostly-static risk (E-040); mass-creation (E-107); AI-label status open; Vietnamese rendering not examined; adaptation effort unknown; still needs a sample under the adverse influencer branch | Requires architect adaptation (vertical, Vietnamese glyphs) before use; not a pilot start |
| `O-007` | EC-001, EC-002, EC-003, EC-004, EC-005, EC-006, EC-008, EC-009, EC-010, EC-011, EC-012 | EC-007 | Meets 11 of 12; footage keeps it original and interactive; overlays reuse the pipeline and carry provenance | CEO filming time remains; overlay AI-label status open; overlays optional until rendering is checked | Start footage-only as O-005; add overlays once the architect confirms Vietnamese rendering |
| `O-008` | EC-001, EC-002, EC-003, EC-004, EC-005, EC-006, EC-007, EC-008, EC-009, EC-010, EC-011, EC-012 | None. | Meets 12 of 12; low claim risk; demonstrable by hand; E-116 third-party demand | Commission rate unsourced; AOV third-party low (E-115) | First niche |
| `O-009` | EC-001, EC-002, EC-003, EC-004, EC-005, EC-006, EC-007, EC-008, EC-009, EC-010, EC-012 | EC-011 | Meets 11 of 12; low claim risk; demonstrable | No demand evidence of its own | Second niche or substitute if home and kitchen rates prove low |
| `O-010` | EC-002, EC-003, EC-004, EC-005, EC-006, EC-007, EC-008, EC-010, EC-012 | EC-001, EC-009, EC-011 | Meets 9 of 12 | Notification and claim rules (E-050, E-106); skin use under the influencer branch | Excluded from the 30-day pilot |
| `O-011` | EC-002, EC-003, EC-004, EC-005, EC-007, EC-008, EC-010, EC-012 | EC-001, EC-006, EC-009, EC-011 | Meets 8 of 12 | Banned in TikTok branded content (E-037); health-claim bans (E-106); removal risk (E-042) | Excluded |

## Risk and Blocker Register

| ID | Risk or blocker | Severity | Likelihood | Delivery impact | Mitigation | Owner | Status |
|---|---|---|---|---|---|---|---|
| `RK-001` | Shopee competitor clause names TikTok (E-104); running both may breach Shopee terms | critical | possible | O-001 invalid under the broad reading; E-110 damages exposure | Run one platform only (O-003) until counsel or Shopee gives a narrow reading in writing | CEO with counsel | open |
| `RK-002` | Influencer duty, Law 75/2025 Art. 15a(3) (E-046, E-056, E-059 low): CEO must have used each product and announce before and during | high | possible | Every form needs a sample and use; code-drawn graphics lose their cost advantage | Cost every form with a sample (C2); O-005 and O-007 satisfy it by design; announce in every video | CEO with counsel | open |
| `RK-003` | PIT rate and withholder conflict (E-060 to E-068; E-071 low); TikTok highest rate without tax code (E-014) | high | likely | Net commission uncertain; over-withholding not assumed recoverable | Sensitivity at 0, 2, 5% and highest rate; Shopee 0.98% plus 10% from VND 250,000 (E-098, E-100) | CEO with counsel | open |
| `RK-004` | CEO eligibility unconfirmed: age 18+, chip CCCD with NFC, tax code, bank account (E-006, E-013, E-014, E-085, E-086) | high | possible | No account and no payout without them | CEO confirms each item privately before any account step; agents never see credentials | CEO | open |
| `RK-005` | Pages not re-captured; first-party status rests on the gatherer's record; C8 rules unquoted | high | certain | A rule may have changed or been misread; C8 items cannot decide a score | Re-capture every cited page with a quote before the Recommendation Gate | context-agent | open |
| `RK-006` | Reopened decisions D-005, D-007, D-008, D-021, D-022 not yet re-ruled | high | certain | The pilot contradicts standing decisions until the CEO rules | CEO rules on the proposed keep, amend or supersede set in this artifact | CEO | open |
| `RK-007` | Shopee Video's status as a permitted channel and KOC earning there unconfirmed (E-082 against E-083; E-084 unquoted) | high | possible | Leading option fails if the channel is not permitted | Written answer from Shopee; fallback O-002 | CEO | open |
| `RK-008` | Commission rates unsourced on both platforms (E-021, E-092); break-even inputs unknown | medium | certain | Revenue cannot be forecast; target may be missed | Read the rate table after account creation; measure in 30 days; treat every figure as ESTIMATE | CEO | open |
| `RK-009` | AI-label status of code-drawn overlays and of synthetic narration open (E-031 to E-033, E-108) | medium | possible | Unlabelled content deleted from 2026-09-23 (E-108) | Label when in doubt; no synthetic narration in the pilot; counsel or platform answer | CEO with counsel | open |
| `RK-010` | Reuse of seller product images or footage unresolved (E-029, E-107) | medium | possible | Stolen-content flag; fine up to VND 10 million (E-107) | Use only the CEO's own footage and recorded facts; no seller media until answered | CEO | open |
| `RK-011` | TikTok 1,000-follower barrier and Link-Share limits (E-008 to E-010, low) | medium | likely | Fallback O-002 has no near-term commission | Treat O-002 as a follower-building fallback, not a 30-day earner | CEO | open |
| `RK-012` | TikTok posting limit and CHR penalties (E-039 to E-042) | medium | possible | Reach throttled or account removed on the fallback | Interactive footage with product shown and spoken | CEO | open |
| `RK-013` | Shopee termination after 6 months without results and 30% liquidated damages (E-110, unquoted) | medium | possible | Exit cost if the pilot underperforms or breaches | Re-capture the clause (RK-005) and read it before sign-up | CEO | open |
| `RK-014` | Sample cost has no budget line; metered ceiling cannot fund purchases | medium | likely | Pilot cannot start without an owner budget ruling | Owner ruling on the reallocation proposed in Delivery Impact | CEO | open |
| `RK-015` | Self-purchase via own link and fraud voids (E-025, E-095, E-096) | low | unlikely | Voided commission and account action | Buy samples outside affiliate links; record each purchase | CEO | open |
| `RK-016` | Pipeline adaptation (vertical format, Vietnamese glyph coverage) not examined | low | likely | Overlays in O-007 delayed; O-006 not startable | Architect examines graphics/ and FontCoverage before overlays are used | architect | open |

## Assessment Summary

- Criteria applied: 12
- Options evaluated: 11
- Risks and blockers recorded: 16
- Blocking items open: 7

## Recommendation

- Recommended option: O-003
- Rationale: O-003 meets 11 of 12 criteria, including every must-have EC-001 to EC-005, and is the only platform option that meets EC-006 without the follower barrier of O-002 or the EC-001 failure of O-001; it is paired with form O-007 (11 of 12, footage-first so EC-001 and EC-008 hold under the adverse influencer branch) and niche O-008 (12 of 12), with O-009 as second niche; no score rests solely on a C8 unquoted rule or a third-party rate.
- Preconditions: RK-001 to RK-007 resolved or ruled by their owners; the CEO rules on the budget proposal (RK-014); disclosure and announcement in every video; samples bought outside affiliate links; no seller media; no synthetic narration.
- Options rejected: O-001 fails EC-001 under the broad clause reading (RK-001), held for later only on a written narrow reading; O-002 is the named single-platform fallback, not the lead, because EC-006 fails (RK-011); O-004 is the outcome only if blockers resolve adversely; O-005 is the starting mode of O-007, not a separate choice; O-006 fails EC-001, EC-004, EC-006 and EC-008; O-010 fails EC-001 and EC-009; O-011 fails EC-001, EC-006 and EC-009.
- Counsel branches (C1): competitor clause narrow keeps O-003 and allows O-001 later, broad keeps O-003 alone; influencer duty applies means sample and use for each product (already in O-007), does not apply leaves O-007 unchanged because disclosure stays; tax at 0, 2 or 5% or highest rate changes net only, see break-even; if Shopee Video is not a permitted channel the fallback is O-002.
- Standing decisions (proposed for the CEO's ruling): D-005 amend, affiliate becomes the lead revenue line for the pilot while disclosure stays a publishing check; D-007 amend, add Shopee Video Vietnam (TikTok Shop as fallback) with the Vietnam-resident payee unchanged, since the Creator Rewards exclusion does not touch affiliate; D-008 amend, add one Vietnamese pilot channel while the English YouTube channel stays first for YouTube and frozen, not cancelled; D-021 amend, part 2 (always disclose by platform control and in-video) kept, part 1 (Vietnamese gaps carried) superseded for the pilot because Vietnamese content puts the gap on the critical path, routed to counsel; D-022 keep for the frozen YouTube first item and not applied to the pilot, where every item carries a disclosed link.
- Coverage of "every": every counsel branch means competitor clause narrow or broad, influencer duty applies or not, PIT 0, 2, 5% or highest rate, channel permitted or not; every publication means each Shopee Video post, its caption and product link, any fallback TikTok post, and any re-post; livestream is out of pilot scope.

## Delivery Impact

- Effort and capacity: O-003 with O-007 is medium effort, almost all of it CEO time (account, samples, filming, approval); agent effort is small (scripts, captions, overlay facts with provenance); overlays add architect effort of unknown size (RK-016).
- Break-even formula: monthly net commission = sum over orders of (order net value × commission rate × (1 − void rate)) × net factor; Shopee net factor = 0.9902 for payouts below VND 250,000 and (1 − 0.0098) × (1 − 0.10) = 0.89118 at or above it (E-098, E-100; fee base assumed gross); TikTok net factor = 0.99 × (1 − t) for t of 0, 0.02, 0.05 or the highest rate, less USD 0.12 per withdrawal (E-014, E-015 2024 page); break-even orders = pilot incremental cost ÷ (AOV × rate × net factor).
- Break-even ESTIMATE: assumptions are 6 samples at AOV USD 7 (E-115 third-party, low) giving a pilot cost of USD 42, metered spend USD 0 (no generative model), void rate 0, Shopee factor 0.89118, and hypothetical rates not sourced; at 3.68% (E-093 third-party) 7 × 0.0368 × 0.89118 = USD 0.2296 per order, 42 ÷ 0.2296 = 183 orders; at 10% USD 0.6238 per order, 68 orders; at 20% USD 1.2477 per order, 34 orders.
- Budget proposal for owner approval: fund samples of up to USD 42 per month from the USD 42.99 standing line if uncommitted, or raise the monthly budget by that amount; the USD 34.42 metered ceiling is unchanged and unused by the pilot.
- Proposed 30-day target (CEO chooses one): tier 1, the measurement set recorded for every published video; tier 2, at least one valid commission paid out (proposed); tier 3, net commission at or above the pilot incremental cost (34 to 183 orders by the ESTIMATE).
- Measurements: views, link clicks, click-through rate, orders, conversion rate, AOV, void and return rate, commission rate per product from the dashboard, payout and withholding observed, CEO minutes per video, cost per video, warnings or takedowns.
- CEO personal actions: confirm age, chip CCCD with NFC and tax code; obtain counsel answers; ask Shopee in writing about the competitor clause and Shopee Video; create the KOC account and add bank details himself; read and record the rate table; choose products and buy samples with his own money outside affiliate links; use each product; film hands-only footage in Vietnamese; approve every publication with disclosure; record the measurements.
- Sequencing constraints: preconditions (RK-004 to RK-007) and counsel answers (RK-001 to RK-003) first; then the CEO's budget and standing-decision rulings; then account, samples and footage-only publication; overlays only after RK-016.
- Dependencies: counsel; Shopee support; context-agent page re-capture; architect for pipeline adaptation; planner for any task breakdown; commands run (read-only): git log --oneline -1 main (4acbd2a), git ls-tree -r --name-only main src graphics (161 files), python runtime/technical_recommendation_validator.py on this artifact.
- Reversal plan: stop publishing, withdraw the balance above VND 10,000 (E-087 unquoted), keep samples; fall back to O-002 or O-004; re-capture E-110 first so the exit cost is known.

## Readiness

- Recommended decision: proceed-with-conditions
- Conditions to satisfy: RK-001 resolved by a written narrow reading or O-001 stays excluded; RK-002 and RK-003 answered by counsel; RK-004 confirmed by the CEO; RK-005 pages re-captured with quotes before the Recommendation Gate; RK-006 standing decisions ruled by the CEO; RK-007 answered by Shopee in writing, else fallback O-002; budget ruling on RK-014; the CEO chooses the 30-day target.
- Blocking items outstanding: RK-001, RK-002, RK-003, RK-004, RK-005, RK-006, RK-007
- Deciding authority: orchestrator (Recommendation Gate at recommendation-draft; this phase has no gate); the CEO as owner rules on the pilot

## Open Questions

- Q-001: Does the Shopee competitor clause (E-104) bar a creator who also earns on TikTok Shop? Owner: counsel or Shopee. Affects O-001, RK-001.
- Q-002: Does Art. 15a(3) (E-046) apply to the CEO as an affiliate creator? Owner: counsel. Affects RK-002 and sample cost.
- Q-003: Which PIT rate applies and who withholds it? Owner: counsel. Affects RK-003 and the net factor.
- Q-004: Is Shopee Video a permitted channel for a KOC with no followers, and does it earn commission there? Owner: Shopee, asked by the CEO. Affects O-003, RK-007.
- Q-005: Do code-drawn overlays from recorded facts need an AI label? Owner: counsel or platform. Affects O-006, O-007, RK-009.
- Q-006: May seller product images be used in overlays? Owner: Shopee or the seller. Affects O-006, RK-010.
- Q-007: What category commission rates apply to the chosen niches? Owner: CEO from the login-gated table. Affects RK-008 and the break-even.
- Q-008: Will the owner fund samples from the standing line or raise the budget? Owner: CEO. Affects RK-014.
- Q-009: Does the delivered pipeline render vertical video with Vietnamese glyphs? Owner: architect. Affects O-007 overlays, RK-016.
