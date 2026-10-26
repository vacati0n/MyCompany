```yaml
technicalRecommendation:
  recommendationId: TR-MC15-002
  decisionReference: "MC-15 research recommendation under CEO-D-1300: affiliate pilot on Shopee and TikTok Shop in Vietnam, path, 30-day plan and owner decisions"
  decisionBasis: recommendation
  sourceInputs:
    - type: investigation-report
      reference: runs/run-8d7e366341bf/states/technical-validation/artifacts/investigation-report.md
    - type: technical-options
      reference: runs/run-8d7e366341bf/states/option-synthesis/artifacts/technical-recommendation.md
    - type: prior-gate-evidence
      reference: "pilot-affiliate-vn/gates/technical-validity-gate.md (repository root; conditions C1 to C12)"
    - type: prior-gate-evidence
      reference: "pilot-affiliate-vn/evidence/recapture-2026-10-26.md (repository root; C8 re-capture)"
    - type: workflow-constraints
      reference: "inline: Framing Gate and Technical Validity Gate decisions restated in the orchestrator dispatch"
  producedBy: tech-lead
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: provisional
  recommendedOption: O-003
  readinessDecision: proceed-with-conditions
  inputDigest: "sha256:ebf31efc76c7a3652515548dab9b5eb6"
  contextDigest: "sha256:f2c4ea84ec5c767f8af6efd2806736eb"
```

## Metadata

- Recommendation ID: TR-MC15-002
- Decision owner: orchestrator decides the Recommendation Gate on this artifact; the CEO, as owner, takes the owner decisions listed under Recommendation
- Requested by: CEO via /research, ticket MC-15, run run-8d7e366341bf, phase recommendation-draft
- Decision date: 2026-10-26

## Decision Context

- Decision to make: Which path the CEO should take for the Vietnamese affiliate pilot, in what order and with what fallback, what the first 30 days do and measure, and which personal actions, counsel points and owner decisions stand before the first publication.
- Delivery constraints: One-person company; the CEO is the Vietnam-resident individual creator and approves each publication individually; content in Vietnamese; budget USD 77.41 per month with a USD 34.42 metered ceiling, any change proposed for owner approval; agents create no account, handle no credential, buy no sample and publish nothing; Wave 12 and Wave 13 are frozen; Technical Validity Gate conditions C1 to C12 bind; first-party citations only, anything unsourced is unverified and goes to counsel; a 30-day target is proposed for the CEO to choose.
- Assumptions in force: The investigation report (provisional, confidence low) and the option-synthesis artifact TR-MC15-001 are the evidence base, read as data; the re-capture of 2026-10-26 quotes five first-party pages and gives 13 items confirmed (E-024, E-080, E-082, E-084, E-085, E-087, E-094, E-101, E-102, E-104, E-105, E-108, E-110), 2 changed (E-076, E-089) and 1 not found (E-100), so the dispatch summary's "12 confirmed" is read as 13; Shopee pages not re-captured (E-086, E-088, E-097, E-098, E-103, E-107, E-109) stay at the gatherer's record and are marked so where used; no first-party commission rate exists, so every revenue figure is an ESTIMATE; a risk is marked mitigated only where the recommended plan complies under every counsel answer, and stays open where some answer would require a different CEO action.

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
| `EC-010` | Reversible at low cost if the pilot fails | A one-person pilot must be able to stop without a penalty (E-042; E-110) | high | Dispatch criteria; S01 reversibility |
| `EC-011` | Demand evidence exists, labelled third-party; commission rate is not scored because it is unsourced | Demand informs the niche choice; C5 forbids scoring on third-party rates | medium | C5, C6; R-018 |
| `EC-012` | Reuses the delivered deterministic pipeline on main (src/, graphics/) rather than building new | Reuse lowers effort; adaptation belongs to architect (B-004) | medium | R-023; S01 reuse ladder |

## Options

| ID | Option | Summary | Effort | Delivery risk | Reversibility | Evidence |
|---|---|---|---|---|---|---|
| `O-001` | Platform: both TikTok Shop and Shopee | Run both; E-104 reach is undefined and Shopee alone sets the competitor list; valid only on a written narrow reading; fallback O-003 | large | critical | costly-to-reverse | E-104 and E-110 (re-captured 2026-10-26), E-008, E-080, E-082 |
| `O-002` | Platform: TikTok Shop only | Affiliate commission needs 1,000 followers; below that Link-Share Only earns nothing from short video per low-confidence snippets | medium | high | reversible | E-006 to E-010, E-013, E-014, E-024 (re-captured), E-039 to E-042 |
| `O-003` | Platform: Shopee only, channel Shopee Video (KOC) | KOC has no follower minimum; Shopee Video is named as affiliate media in the terms; 7-day cookie; PIT withheld per law, rate not stated | medium | medium | reversible | E-080, E-082, E-085, E-087, E-094, E-110 (re-captured); E-088, E-098 (not re-captured) |
| `O-004` | Platform: defer the pilot (status quo) | Publish nothing until every blocker is resolved; the report's defer reading, not a decision | trivial | low | reversible | IR defer reading; C12 |
| `O-005` | Form (a): real hands-only footage with samples | CEO films his own hands using a real sample, Vietnamese voice or captions, no generative model | small | medium | reversible | E-029, E-040, E-046, E-109 |
| `O-006` | Form (b): code-drawn graphics only | Reuse the deterministic renderer (graphics/render.mjs, graphics/drawing/elements.jsx) on product images and recorded facts | unknown | high | reversible | E-029, E-040, E-107, E-108 (re-captured), E-109; git ls-tree of main |
| `O-007` | Form (c): mix of hands-only footage and code-drawn fact overlays | Footage first; deterministic overlays of recorded facts added only after the architect checks rendering; narration by the CEO | medium | medium | reversible | E-040, E-109, E-031 to E-033, E-108 (re-captured); git ls-tree of main |
| `O-008` | Niche: home and kitchen gadgets | Low-claim demonstrable products; strongest third-party demand signal | small | low | reversible | E-116, E-117 (third-party); E-036, E-103 |
| `O-009` | Niche: phone accessories | Low-claim demonstrable products; no niche-specific demand evidence | small | low | reversible | E-036; no demand source |
| `O-010` | Niche: cosmetics | High-claim; product notification and claim rules; use on skin under the influencer branch | small | high | reversible | E-036, E-050, E-106 |
| `O-011` | Niche: supplements | Banned in TikTok branded content; health-claim and doctor-imagery bans; no Shopee commission on some supplements | small | critical | costly-to-reverse | E-037, E-042, E-103, E-106 |

## Tradeoff Analysis

| Option | Criteria met | Criteria missed | Strengths | Weaknesses | Sequencing implication |
|---|---|---|---|---|---|
| `O-001` | EC-002, EC-003, EC-004, EC-005, EC-006, EC-009, EC-011, EC-012 | EC-001, EC-007, EC-008, EC-010 | Meets 8 of 12; widest reach (E-111 third-party) | Quoted E-104 sets no reach and lets Shopee set the list; a breach exposes 30% of all amounts paid (E-110); two accounts double CEO time | Only after a written narrow reading of E-104; never while a Shopee account is active without it |
| `O-002` | EC-001, EC-002, EC-003, EC-004, EC-005, EC-009, EC-010, EC-011, EC-012 | EC-006, EC-007, EC-008 | Meets 9 of 12; no competitor clause issue; strong home-living signal (E-116 third-party) | First commission waits on 1,000 followers (E-008 to E-010); posting limit and CHR (E-039 to E-042) | Single-platform fallback; start only after Shopee activity ends or a written E-104 reading allows both |
| `O-003` | EC-001, EC-002, EC-003, EC-004, EC-005, EC-006, EC-007, EC-008, EC-009, EC-010, EC-011, EC-012 | None. | Meets 12 of 12; earns from the first video with no follower minimum (E-080); Shopee Video named as affiliate media (E-082 page 1); stopping without breach carries no stated cost (E-110 quoted) | PIT rate not stated on Shopee pages (E-100 not found); Shopee Video earning for a new KOC unconfirmed in-app; booking emails deemed accepted after 3 days (E-089) | After CEO eligibility, the owner rulings and the in-app attribution check |
| `O-004` | EC-001, EC-002, EC-003, EC-004, EC-005, EC-007, EC-008, EC-009, EC-010 | EC-006, EC-011, EC-012 | Meets 9 of 12; no cost, no exposure | Produces no commission and no measurement; the pivot CEO-D-1300 stalls | The outcome if a condition resolves adversely with no fallback |
| `O-005` | EC-001, EC-002, EC-003, EC-004, EC-005, EC-006, EC-008, EC-009, EC-010, EC-011 | EC-007, EC-012 | Meets 10 of 12; satisfies the adverse influencer branch by design; original and product shown | Highest CEO filming time; no pipeline reuse; sample cost on every product | The starting mode of O-007 |
| `O-006` | EC-002, EC-003, EC-005, EC-007, EC-009, EC-010, EC-011, EC-012 | EC-001, EC-004, EC-006, EC-008 | Meets 8 of 12; lowest CEO time; reuses delivered renderer | Seller-image rights open; mostly-static risk (E-040); repetitive and unoriginal patterns named on the Shopee AI page (E-108); Vietnamese rendering not examined | Requires architect adaptation before use; not a pilot start |
| `O-007` | EC-001, EC-002, EC-003, EC-004, EC-005, EC-006, EC-008, EC-009, EC-010, EC-011, EC-012 | EC-007 | Meets 11 of 12; footage keeps it original; overlays later reuse the pipeline and carry provenance | CEO filming time remains; overlay AI-label status open | Footage-only for the 30 days; overlays only after the architect's rendering check and a label answer |
| `O-008` | EC-001, EC-002, EC-003, EC-004, EC-005, EC-006, EC-007, EC-008, EC-009, EC-010, EC-011, EC-012 | None. | Meets 12 of 12; low claim risk; demonstrable by hand; E-116 third-party demand | Commission rate unsourced; AOV third-party low (E-115) | First niche |
| `O-009` | EC-001, EC-002, EC-003, EC-004, EC-005, EC-006, EC-007, EC-008, EC-009, EC-010, EC-012 | EC-011 | Meets 11 of 12; low claim risk; demonstrable | No demand evidence of its own | Second niche after day 30, or substitute if home and kitchen rates prove low |
| `O-010` | EC-002, EC-003, EC-004, EC-005, EC-006, EC-007, EC-008, EC-010, EC-012 | EC-001, EC-009, EC-011 | Meets 9 of 12 | Notification and claim rules (E-050, E-106); skin use under the influencer branch | Excluded from the pilot |
| `O-011` | EC-002, EC-003, EC-004, EC-005, EC-007, EC-008, EC-010, EC-012 | EC-001, EC-006, EC-009, EC-011 | Meets 8 of 12 | Banned in TikTok branded content (E-037); health-claim bans (E-106); no Shopee commission on some items (E-103) | Excluded |

## Risk and Blocker Register

| ID | Risk or blocker | Severity | Likelihood | Delivery impact | Mitigation | Owner | Status |
|---|---|---|---|---|---|---|---|
| `RK-001` | Shopee competitor clause E-104, quoted in full: names TikTok, Lazada, Tiki, Sendo; reach undefined; Shopee alone sets the list | critical | possible | O-001 invalid without a narrow written reading; a breach exposes 30% of amounts paid (E-110) | Shopee only; no competitor-platform seller link on any channel; no TikTok channel registered with Shopee; O-001 excluded until a written reading | CEO with counsel | mitigated |
| `RK-002` | Influencer duty, Law 75/2025 Art. 15a(3) (E-046, E-056, E-059 low): use each product and announce before and during | high | possible | Every form needs a sample and use; code-drawn graphics lose their cost advantage | Plan applies the adverse branch to every video: sample bought, used, announced before and during; holds under either counsel answer | CEO with counsel | mitigated |
| `RK-003` | PIT classification, rate and withholder unsettled; E-100 (10% from VND 250,000) not found on Shopee pages; Shopee withholds per PIT law, no rate stated | high | likely | Net commission uncertain; a business-income reading changes the sign-up form and annual filing | Net modelled at 0, 2, 5 and 10% PIT, or 5% VAT plus 5% PIT as a business individual (E-101); counsel answer before the first annual filing | CEO with counsel | open |
| `RK-004` | CEO eligibility unconfirmed: age 18+, chip CCCD, tax code, own domestic bank account matching CCCD (E-085 re-captured; E-086 not) | high | possible | No account and no payout without them; missing payment data for 6 months forfeits pay and ends the cooperation (E-087) | CEO confirms each item privately before any account step and enters payment data at sign-up; agents never see the numbers | CEO | open |
| `RK-005` | C8 first-party rules were unquoted and rested on the gatherer's record | high | certain | C8 items could not decide a score before the Recommendation Gate | Re-captured with quotes on 2026-10-26: 13 confirmed, E-076 and E-089 changed, E-100 not found; changes carried into RK-003, RK-013 | context-agent | resolved |
| `RK-006` | Reopened decisions D-005, D-007, D-008, D-021, D-022 not yet re-ruled | high | certain | The pilot contradicts standing decisions until the CEO rules | CEO rules on the proposed keep, amend or supersede set under Recommendation | CEO | open |
| `RK-007` | Shopee Video earning for a new KOC: terms name Shopee Video as affiliate media but set no Shopee Video rate or attribution rule | high | unlikely | Leading option falls back to O-002 if Shopee Video posts do not attribute commission | CEO confirms in-app product tagging to his affiliate account before the first publication; first order confirms it; fallback O-002 | CEO | mitigated |
| `RK-008` | Commission rates unsourced (E-021, E-092; rate varies by media channel); payout schedule E-088 not re-captured | medium | certain | Revenue cannot be forecast; target tier 3 may be missed | Read the rate table after sign-up; measure in 30 days; treat every figure as ESTIMATE | CEO | open |
| `RK-009` | AI-label rule E-108 confirmed with auto-labelling; reach to code-drawn overlays open | medium | possible | Unlabelled AI videos deleted from 2026-09-23; Shopee Video access may be lost | No generative content, no synthetic narration and no overlays in the 30 days; label when in doubt once overlays start | CEO with counsel | mitigated |
| `RK-010` | Reuse of seller product images or footage unresolved (E-029; E-107 not re-captured) | medium | possible | Stolen-content flag; fine up to VND 10 million (E-107) | Only the CEO's own footage and recorded facts; no seller media in the pilot | CEO | mitigated |
| `RK-011` | TikTok 1,000-follower barrier and Link-Share limits (E-008 to E-010, low) | medium | likely | Fallback O-002 has no near-term commission | Treat O-002 as a follower-building fallback, not a 30-day earner | CEO | open |
| `RK-012` | TikTok posting limit and CHR penalties (E-039 to E-042) | medium | possible | Reach throttled or account removed on the fallback | Interactive footage with product shown and spoken | CEO | open |
| `RK-013` | Shopee exit and booking terms, quoted: termination after 6 months without results; 30% of amounts paid as sanction; booking email accepted after 3 days' silence; booked posts not removable | medium | possible | A missed booking email binds the CEO to a project and to posts he cannot remove; breach costs 30% of amounts paid | Check email at least every 2 days and decline any KOL Booking within 3 days unless the owner rules otherwise; no sub-affiliates | CEO | open |
| `RK-014` | Sample cost has no budget line; metered ceiling cannot fund purchases | medium | likely | Pilot cannot start and RK-002 mitigation fails without an owner budget ruling | Owner ruling on the sample line proposed under Delivery Impact | CEO | open |
| `RK-015` | Self-purchase via own link and fraud voids (E-025, E-095, E-096) | low | unlikely | Voided commission and account action | Samples bought outside affiliate links, receipts kept | CEO | mitigated |
| `RK-016` | Pipeline adaptation (vertical format, Vietnamese glyph coverage) not examined | low | likely | Overlays in O-007 delayed; O-006 not startable | Architect examines graphics/ and FontCoverage before overlays are used | architect | open |

## Assessment Summary

- Criteria applied: 12
- Options evaluated: 11
- Risks and blockers recorded: 16
- Blocking items open: 3

## Recommendation

- Recommended option: O-003
- Rationale: O-003 now meets all 12 criteria, every must-have EC-001 to EC-005 included, and is the only platform option meeting EC-006 without the follower barrier of O-002 (EC-006 missed) or the EC-001 failure of O-001; the quoted E-110 shows that stopping without breach carries no stated cost, so EC-010 moves from missed to met; it is paired with form O-007 started footage-only (11 of 12, EC-001 and EC-008 hold under the adverse influencer branch) and niche O-008 (12 of 12), with O-009 second and O-002 as the single-platform fallback.
- Preconditions: CEO eligibility confirmed (RK-004); owner rulings on D-005, D-007, D-008, D-021, D-022 (RK-006), the sample line (RK-014), the sign-up form and the 30-day tier; the mitigations of RK-001, RK-002, RK-007, RK-009, RK-010 and RK-015 held as publishing rules; counsel engaged on Q-001 to Q-006.
- Options rejected: O-001 fails EC-001, EC-007, EC-008 and EC-010 because E-104 sets no reach (RK-001); O-002 fails EC-006, EC-007 and EC-008 and is the named fallback; O-004 fails EC-006, EC-011 and EC-012 and is the outcome only if conditions resolve adversely; O-005 fails EC-007 and EC-012 and is the starting mode of O-007; O-006 fails EC-001, EC-004, EC-006 and EC-008; O-010 fails EC-001, EC-009 and EC-011; O-011 fails EC-001, EC-006, EC-009 and EC-011.
- Path, order and fallback: 1) Shopee Affiliate KOC on Shopee Video, home and kitchen (O-003, O-007 footage-only, O-008); 2) after day 30, add O-009 or overlays only on measured results and RK-016; 3) fallback O-002 if RK-007 fails, started only after Shopee activity ends or a written E-104 reading permits both; 4) O-004 if eligibility or the owner rulings fail.
- First 30-day plan: before day 1 the preconditions above; days 1 to 3 the CEO signs up as an individual KOC, enters payment data, reads rates and checks Shopee Video attribution; days 4 to 7 he picks up to 6 low-claim products not on the no-commission list (E-103), buys and uses the samples; days 8 to 28 he publishes up to 2 footage-first Vietnamese videos per product, each published only after his own review, with in-video and caption disclosure; days 29 to 30 the results are read against the chosen tier.
- Measurements: per video views, product clicks, click-through rate, orders, conversion rate, AOV, commission per order, voids and returns, CEO minutes, sample cost, labels, warnings or takedowns; per period the rate per product from the dashboard, payouts, the amount withheld (the observed PIT rate), dispute notices within the 24-hour window (E-102) and booking emails.
- CEO personal actions, in order: 1) confirm age, chip CCCD, tax code and own bank account privately; 2) take the owner decisions below; 3) send counsel the open points; 4) create the Shopee Affiliate account himself, read and accept the terms, register no TikTok channel, enter payment data; 5) read and record rates and confirm Shopee Video attribution; 6) choose products and buy samples with his own money outside affiliate links; 7) use each product; 8) film hands-only footage with his own Vietnamese voice; 9) approve and publish each video himself with disclosure; 10) check email at least every 2 days and commission results daily; 11) record the measurements and rule on day 30.
- Open points for local counsel: competitor clause reach and a later TikTok account (Q-001); Art. 15a(3) scope for an affiliate (Q-002); PIT classification, rate, withholder and sign-up form now that E-100 is gone (Q-003); AI-label reach to code-drawn overlays under Law 134/2025 (Q-005); seller media reuse (Q-006); Vietnamese disclosure form and penalties under the amended advertising law, tied to D-021 part 1 (Q-012).
- Owner decisions: OD1 D-005 amend, affiliate the lead revenue line for the pilot; OD2 D-007 amend, add Shopee Affiliate VN with the CEO as Vietnam-resident individual payee, TikTok Shop fallback only; OD3 D-008 amend, add one Vietnamese Shopee Video pilot channel, English YouTube first and frozen; OD4 D-021 amend, keep part 2 (always disclose), supersede part 1 for the pilot and route to counsel; OD5 D-022 keep for the frozen YouTube first item, not applied to the pilot; OD6 sample line up to USD 42 per month; OD7 30-day target tier; OD8 sign up as individual (default, convertible per E-081) pending counsel; OD9 start before counsel answers Q-001 and Q-002, since the plan complies under both; OD10 decline Shopee bookings during the pilot; OD11 path O-003 with O-007 footage-first, niche O-008 then O-009, O-010 and O-011 excluded.
- Coverage of "every": every counsel branch means E-104 narrow or broad; Art. 15a(3) applies or not; PIT for a non-business individual at 0, 2, 5 or 10%, or 5% VAT plus 5% PIT as a business individual; Shopee Video attributes or not; every publication means each Shopee Video post, its caption and product tag, any fallback TikTok post, and any re-post; livestream is outside the pilot.

## Delivery Impact

- Effort and capacity: O-003 with O-007 footage-first is medium effort, almost all of it CEO time (sign-up, samples, use, filming, approval, measurement), with CEO minutes per video unknown until measured; agent effort is small (scripts, captions, disclosure text, measurement sheet); overlays add architect effort of unknown size (RK-016) and are outside the 30 days.
- Tax range (replaces the E-100 figure): Shopee net factor = (1 − 0.0098 − t) at worst and 0.9902 × (1 − t) at best, for t of 0, 2, 5 or 10% (E-060, E-061, E-070, E-071 guidance or low), or t = 10% as a business individual (5% VAT plus 5% PIT, E-101 confirmed); range 0.8902 to 0.9902; the 0.98% fee rests on E-098, not re-captured; TikTok's highest-rate case does not arise on Shopee because a tax code is mandatory (E-085).
- Break-even ESTIMATE: 6 samples at AOV USD 7 (E-115 third-party, low) give a pilot cost of USD 42; metered spend USD 0; void rate 0; at 3.68% (E-093 third-party) 165 to 184 orders, at 10% 61 to 68, at 20% 31 to 34, across net factors 0.9902 to 0.8902; rates are hypothetical, not sourced; arithmetic run with python -c, recorded under Dependencies.
- Budget proposal for owner approval: fund samples of up to USD 42 per month from the USD 42.99 standing line if uncommitted, or raise the monthly budget by that amount; the USD 34.42 metered ceiling is unchanged and unused.
- 30-day target (CEO chooses one): tier 1, the full measurement set for every published video; tier 2 (recommended), tier 1 plus at least one valid commission credited by day 30, because orders complete within 30 days of the visit (E-094) and the payout schedule is not re-captured (E-088); tier 3, net commission at or above the pilot cost (31 to 184 orders by the ESTIMATE).
- Sequencing constraints: eligibility and owner rulings first; sign-up and the attribution check before any sample is bought; samples used before filming; each publication cleared by the CEO individually; no TikTok Shop account while the Shopee account is active unless a written E-104 reading permits it; overlays only after RK-016 and Q-005.
- Dependencies: CEO; local counsel; Shopee support; context-agent for any further page re-capture; architect for pipeline adaptation; planner for any task breakdown; commands run (read-only): cat and sed of the run inputs, modules, gate record, re-capture and research/framework-defects.md; python -c arithmetic for the net factors and break-even; python runtime/technical_recommendation_validator.py on this artifact with the envelope.
- Reversal plan: stop publishing; stopping without breach carries no stated cost, and Shopee may end an account with no results for 6 months (E-110); keep payment data complete so earned balances above VND 10,000 are paid (E-087); keep the samples; accept no booking, since booked posts cannot be removed without Shopee's written request (E-089); then fall back to O-002 or O-004.

## Readiness

- Recommended decision: proceed-with-conditions
- Conditions to satisfy: RK-004 confirmed by the CEO; RK-006 standing decisions ruled by the CEO; RK-003 handled by the owner's sign-up-form ruling now and counsel's answer before the first annual filing; RK-014 sample line ruled; the CEO chooses the 30-day tier; the mitigations of RK-001, RK-002, RK-007, RK-009, RK-010, RK-013 and RK-015 held as publishing rules; the orchestrator judges whether the five-page re-capture discharges the C12 re-capture precondition (Q-010).
- Blocking items outstanding: RK-003, RK-004, RK-006
- Deciding authority: orchestrator (Recommendation Gate at recommendation-draft); the CEO as owner takes the owner decisions OD1 to OD11

## Open Questions

- Q-001: Does E-104 bind the person on unregistered channels, and may the CEO later run TikTok Shop while a Shopee account is active? Owner: counsel, with Shopee asked in writing by the CEO. Affects O-001, O-002 sequencing, RK-001.
- Q-002: Does Art. 15a(3) (E-046) apply to the CEO as an affiliate creator? Owner: counsel. Affects RK-002 and sample cost after the pilot.
- Q-003: Is Shopee affiliate commission business income for the CEO, at what PIT rate, who withholds, and must he sign up as a business individual? Owner: counsel. Affects RK-003, the net factor and OD8.
- Q-004: Do Shopee Video product tags attribute commission to a new KOC, and at what channel rate? Owner: CEO in-app, else Shopee support. Affects O-003, RK-007.
- Q-005: Do code-drawn overlays from recorded facts need the "Video tạo bởi AI" label? Owner: counsel or Shopee. Affects O-006, O-007, RK-009.
- Q-006: May seller product images be used in overlays? Owner: counsel, Shopee or the seller. Affects O-006, RK-010.
- Q-007: What category commission rates apply to the chosen products? Owner: CEO from the login-gated table. Affects RK-008 and the break-even.
- Q-008: Will the owner fund samples from the standing line or raise the budget? Owner: CEO. Affects RK-014, RK-002.
- Q-009: Does the delivered pipeline render vertical video with Vietnamese glyphs? Owner: architect. Affects O-007 overlays, RK-016.
- Q-010: Does the five-page re-capture discharge the C12 re-capture precondition, given that E-086, E-088, E-097, E-098, E-103, E-107 and E-109 were not re-captured? Owner: orchestrator. Affects the readiness conditions.
- Q-011: Which 30-day target tier does the CEO choose? Owner: CEO. Affects the day-30 reading.
- Q-012: What disclosure form and penalties apply under the amended advertising law for Shopee Video posts? Owner: counsel. Affects EC-002, D-021 part 1.
