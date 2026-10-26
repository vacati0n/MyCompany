```yaml
requirementFraming:
  framingId: FRAME-2026-0001
  subject: Affiliate pilot research on TikTok Shop and Shopee in Vietnam
  sourceInputs:
    - type: research-question
      reference: runs/inputs/mc-15-research-question.md
    - type: business-intent
      reference: runs/inputs/mc-15-business-intent.md
  producedBy: business-analyst
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: provisional
  framingVerdict: partially-framed
  requirementCount: 26
  inputDigest: sha256:4ce44ea4cf2b08c1aee25150130fa9be
  contextDigest: sha256:4f9e487720a6b4c8ba6b207522451ff8
```

## Metadata

- Subject: Affiliate pilot research on TikTok Shop and Shopee in Vietnam
- Requested by: The company's CEO, as owner of the pivot decision recorded in the business-intent input
- Decision owner: The CEO approves the research recommendation, chooses the content form, and rules on the supersession list. The Framing Gate is decided by product-owner (see Handoff).
- Workflow phase: research-framing
- Framing date: 2026-10-26

## Business Context

- Business intent: Move the company's revenue line from advertising on long-form explainer video toward affiliate revenue, with TikTok Shop affiliate and Shopee Affiliate in Vietnam as the pilot that becomes the model. Research comes first. Nothing is built, opened, downloaded, or published until the owner approves the recommendation. The frozen Wave 12 and Wave 13 work is held, not cancelled.
- Problem statement: The owner judges that the current video path cannot earn money. The business needs research-backed answers on platforms, eligibility, commission, rules, law, content form, niche, economics, and the decisions the pivot reopens, so that an affiliate pilot can be approved or rejected before any build.
- Affected stakeholders: The CEO, as the individual creator who carries the legal position and approves each publication. The platforms are external parties and are not decision owners.
- Current-state pain: As supplied, the advertising path waits on partner-programme thresholds the company has not started toward. The monthly budget is USD 77.41 with a metered ceiling of USD 34.42. No revenue figure is supplied (see Q-003).

## Target Outcomes

| ID | Outcome | Measure | Business Driver |
|---|---|---|---|
| `O-001` | The owner can decide which platform or platforms to pilot, and in what order, from the eligibility, payout, and commission facts of each | Each platform's facts carry source and date; the recommendation names the order and its reasons | Decide before any build, account, or download |
| `O-002` | The owner knows the content, disclosure, and legal constraints a pilot publication must meet | Each rule and legal point carries its source class; points first-party text cannot settle are named for counsel | The individual creator carries the legal position |
| `O-003` | The owner can choose the content form from costed and risk-rated options | Each option shows CEO time and cost per video and a risk rating against O-002 | The visual form is left to research and the CEO chooses |
| `O-004` | The owner has a short ranked list of pilot niches | Two to four candidates ranked on the five criteria in R-018 | Fit with commission and risk |
| `O-005` | The owner can judge whether the pilot can pay for itself within the existing budget | Break-even per month stated as formula and ESTIMATE, set against the budget and ceiling; no revenue target supplied (Q-003) | Pilot economics against the existing budget |
| `O-006` | The owner knows which standing decisions the pivot keeps, amends, or supersedes, and what the delivered pipeline and frozen work contribute | Each named decision has a keep, amend, or supersede entry with a reason | Decisions the pivot reopens need a ruling |
| `O-007` | The owner has a 30-day pilot plan with the CEO's personal actions listed | Plan names its measurements; CEO actions and agent prohibitions are each listed | The pilot starts only on approval |

## Requirements

| ID | Requirement | Type | Outcome Ref | Priority |
|---|---|---|---|---|
| `R-001` | For each platform, the research states the eligibility conditions (age, residency, identity verification, follower or activity minimums, account type) and what an applicant who fails one of them may do | functional | `O-001` | must |
| `R-002` | For each platform, the research states the tax-code requirement, payout method and currency, payout threshold, and payout timing | functional | `O-001` | must |
| `R-003` | For each platform, the research states the commission structure by category, the attribution window, the conditions that void a commission, and the fees and deductions | functional | `O-001` | must |
| `R-004` | The research states a platform recommendation (TikTok Shop, Shopee, or both) with its order and reasons, and treats any other platform only as a short comparison | functional | `O-001` | must |
| `R-005` | Every figure and rule carries its source class, source, and date; a third-party figure is never the sole basis for a rule or a revenue number; a US-only rule is never stated as a Vietnamese rule | non-functional | `O-001` | must |
| `R-006` | A point with no Vietnamese first-party source is recorded as unsourced, not filled from a third-party source; where first-party and third-party sources disagree, both are stated | functional | `O-001` | must |
| `R-007` | Any completeness claim in the research names the cases it covers | non-functional | `O-001` | must |
| `R-008` | Producing the research opens no account, joins no programme, contacts no seller, buys no sample, downloads no tool or asset, and publishes nothing | non-functional | `O-001` | must |
| `R-009` | For each platform, the research states the rules for affiliate and shoppable content in Vietnam: disclosure labelling, unoriginal or reused content, AI-generated content labelling, posting limits, link and live-stream rules, and the conduct that restricts an account | functional | `O-002` | must |
| `R-010` | The research states which product categories, product footage, and claims are allowed, restricted, or prohibited for the pilot, including health, cosmetics, supplements, and children's products | functional | `O-002` | must |
| `R-011` | The research states the Vietnamese advertising and affiliate disclosure obligations as they apply to an individual affiliate, each cited to official legal text | functional | `O-002` | must |
| `R-012` | The research states personal income tax on affiliate commissions (who withholds, the rate, the thresholds) and whether a household-business registration is triggered, each cited to official legal text | functional | `O-002` | must |
| `R-013` | Each legal point that first-party text does not settle is recorded as unsettled and named for local counsel; no legal conclusion is inferred | functional | `O-002` | must |
| `R-014` | Affiliate disclosure applies to every pilot publication until Vietnamese law on it is established | non-functional | `O-002` | must |
| `R-015` | The research states at least three content-form options: real product footage shot by a person, graphics drawn by code from product images and recorded product facts, and a mix of the two | functional | `O-003` | must |
| `R-016` | For each option, the research states the originality rules of each platform that apply to it, and what observed, cited, top-earning Vietnamese affiliate content of that form looks like | functional | `O-003` | must |
| `R-017` | For each option, the research states the CEO's time per video, the production cost per video, and a risk rating against the constraints of O-002; an option that needs a generative model is named as such and priced separately | functional | `O-003` | must |
| `R-018` | The research gives two to four candidate categories for the pilot, each ranked on first-party commission, demand evidence, return and refund risk, claim and regulatory risk, and fit with each content-form option | functional | `O-004` | must |
| `R-019` | The research states monthly break-even as a formula and as an ESTIMATE, with each input (views per video, click-through, conversion, average order value, commission net of fees and tax) stated and its arithmetic shown | functional | `O-005` | must |
| `R-020` | The break-even is set against the monthly budget and the metered ceiling; any change to either that the research recommends is named for owner approval | functional | `O-005` | must |
| `R-021` | Each break-even input that has no source is named, with how the pilot would measure it in its first 30 days | functional | `O-005` | must |
| `R-022` | For each of the five standing decisions the pivot reopens, the research states keep, amend, or supersede, with the reason, for the owner to rule | functional | `O-006` | must |
| `R-023` | The research states which parts of the delivered pipeline the pilot would reuse, and what the frozen Wave 12 and Wave 13 work would contribute if unfrozen | functional | `O-006` | must |
| `R-024` | The research lists the decisions the owner must take before the pilot can start | functional | `O-006` | must |
| `R-025` | The research states a 30-day pilot plan for the recommended platform or platforms and content form, with the measurements the plan takes | functional | `O-007` | must |
| `R-026` | The research lists the CEO's personal actions (account creation, identity verification, samples) and the actions no agent may take (creating accounts, handling credentials) | functional | `O-007` | must |

## Acceptance Intent

| ID | Acceptance Intent | Requirement Ref | Demonstrated By | Priority |
|---|---|---|---|---|
| `AI-001` | Each platform's eligibility conditions are visible, each with a source, and the outcome for a failing applicant is stated | `R-001` | Per-platform eligibility section with sources and failure outcomes | must |
| `AI-002` | Each payout fact is stated per platform with its source and date | `R-002` | Per-platform payout section | must |
| `AI-003` | Commission by category, attribution window, void conditions, and fees are each stated per platform with source and date | `R-003` | Per-platform commission section | must |
| `AI-004` | The recommendation names the platform or platforms, the order, and the reasons, and any other platform appears only in a separate comparison | `R-004` | Recommendation section and separate comparison section | must |
| `AI-005` | Each figure and rule shows its source class, source, and date, and each US-derived statement is marked US-only | `R-005` | Source labels on every figure and rule | must |
| `AI-006` | Each unsourced point is marked unsourced, and each first-party and third-party conflict shows both sides | `R-006` | Unsourced markers and conflict entries in the evidence log | must |
| `AI-007` | Each completeness claim is followed by the list of cases it covers | `R-007` | Completeness statements with their case lists | must |
| `AI-008` | The research record shows no account, programme, seller contact, sample, download, or publication | `R-008` | Activity record for the research phase | must |
| `AI-009` | Each Vietnam platform rule cites its source for disclosure, originality, AI labelling, posting limits, link and live-stream rules, and restriction conduct | `R-009` | Per-platform content-rules section | must |
| `AI-010` | Allowed, restricted, and prohibited categories, footage, and claims are each stated, including the four named categories | `R-010` | Category and claim table | must |
| `AI-011` | Each advertising and affiliate disclosure obligation cites official legal text | `R-011` | Legal table with official citations | must |
| `AI-012` | Personal income tax treatment cites official legal text and covers withholding, rate, thresholds, and the household-business trigger | `R-012` | Tax section with citations | must |
| `AI-013` | Each point that first-party text does not settle appears as a counsel question, and no legal conclusion appears without a citation | `R-013` | Counsel question list | must |
| `AI-014` | The disclosure rule appears in the pilot plan as applying to every pilot publication | `R-014` | Pilot plan disclosure statement | must |
| `AI-015` | Each of the three named content-form options appears in the report | `R-015` | Content-form options section | must |
| `AI-016` | Each option shows the applicable originality rules and observed, cited examples | `R-016` | Per-option originality and examples entries | must |
| `AI-017` | Each option shows CEO time, cost per video, and risk rating, and any model-based option is labelled and priced separately | `R-017` | Costing and risk table per option | must |
| `AI-018` | Two to four categories appear, each with its ranking on the five criteria | `R-018` | Niche ranking table | must |
| `AI-019` | The break-even formula and ESTIMATE appear, with the arithmetic for each input | `R-019` | Break-even section | must |
| `AI-020` | The break-even is compared with the budget and the ceiling, and any recommended change is listed for approval | `R-020` | Budget comparison and change list | must |
| `AI-021` | Each unsourced input is listed with its first-30-day measure | `R-021` | Unsourced input list | must |
| `AI-022` | Each of the five standing decisions has a keep, amend, or supersede entry with its reason | `R-022` | Supersession table | must |
| `AI-023` | The reusable parts of the pipeline and the contribution of the frozen work each appear | `R-023` | Reuse section | must |
| `AI-024` | The owner-decision list covers each decision the report raises | `R-024` | Decision list | must |
| `AI-025` | The pilot plan names each measurement it takes and the window it takes it in | `R-025` | Pilot plan measurement table | must |
| `AI-026` | The CEO's personal actions and the agent-prohibited actions each appear as a list | `R-026` | Two-part action list | must |

## Framing Boundaries

| ID | Excluded Concern | Reason | Revisit Trigger |
|---|---|---|---|
| `B-001` | Acceptance thresholds, including what counts as sufficient sourcing and any revenue target | Thresholds belong to product-owner | Acceptance criteria are approved at the Framing Gate (Q-002) |
| `B-002` | Verification design for the research report | Verification design belongs to qa | A later phase requires a verification plan |
| `B-003` | Choice of content form | Decided by the CEO after the research recommendation | The owner rules on the recommendation |
| `B-004` | How the delivered pipeline would be adapted to any option | Technical approach belongs to architect | The owner approves a build |
| `B-005` | Task breakdown, sequence, and estimates for any pilot build | Decomposition belongs to planner | The owner approves the recommendation |
| `B-006` | Current-state facts about the delivered pipeline and the frozen branch | Current-state facts belong to context-agent | The technical-validation phase |
| `B-007` | Platforms other than TikTok Shop and Shopee in Vietnam, beyond a short comparison | Outside the research question | The owner widens the pilot |
| `B-008` | Legal conclusions | Belongs to local counsel; this research does not infer law | Counsel's written view is recorded |
| `B-009` | Any account, programme, seller contact, sample, download, build, or publication | Excluded from the research; each publication needs owner approval | The owner approves the recommendation |
| `B-010` | Changes to the frozen Wave 12 branch and the deferred Wave 13 ticket | Frozen, not cancelled | The owner unfreezes the work |
| `B-011` | The Framing Gate decision and any gate assessing this framing | Producer Exclusion Rule; product-owner decides | None within this framing |
| `B-012` | Expansion beyond the pilot (other markets, products, or channels) | The pilot is the first step | The owner decides after pilot evidence |

## Assumptions

| ID | Assumption | Basis | Confidence | Impact If False |
|---|---|---|---|---|
| `AS-001` | The Vietnamese first-party pages named in the research question are the correct first-party set | The research question names them as the evidence standard | medium | Evidence is drawn from the wrong set; R-005 is not met |
| `AS-002` | The Shopee commission range and the 5% fee from February 2026 are third-party figures to be re-verified, not facts | The research question labels them third-party and cites the language-market analysis | low | Commission and economics are wrong by the size of the error |
| `AS-003` | The US-only TikTok Shop rules in the platform-policy dossier do not apply to Vietnam | The research question states the dossier holds nothing first-party for Vietnam | medium | A US rule is carried into a Vietnam finding (R-005) |
| `AS-004` | The repository files the research question cites contain what it describes | The research question's summary; those files are outside the frozen context slice and were not read here (Q-008) | medium | Research starts from a wrong baseline |
| `AS-005` | The monthly budget of USD 77.41 and metered ceiling of USD 34.42 are current | Both supplied inputs state the same figures; no dated source is supplied | medium | Break-even is compared against a stale budget |
| `AS-006` | The CEO is the sole individual creator for the pilot | The individual-creator decision, as the research question states it | high | Account structure and legal position differ |
| `AS-007` | The named Vietnamese legal texts (advertising law and amendments, e-commerce decrees, consumer protection rules) are the relevant set | The research question names them | medium | Obligations are missed; counsel must confirm (B-008) |
| `AS-008` | The delivered pipeline has the capabilities the research question lists (narration, deterministic graphics, provenance, approval by hash, held-stage refusal) | The research question's description; not verified here | medium | The reuse analysis (R-023) is wrong |

## Open Questions

| ID | Question | Blocking | Owner | Needed By |
|---|---|---|---|---|
| `Q-001` | Through what channel does Vietnamese first-party evidence enter the run? The technical-validation role is prohibited from external network access and this agent may not access external systems, so no role in the workflow is permitted to fetch the named pages | yes | orchestrator | technical-validation |
| `Q-002` | What are the acceptance criteria for the research, which the research question defers to this gate, including what counts as sufficient sourcing? | yes | product-owner | Framing Gate |
| `Q-003` | What revenue target would make the pilot count as earning? None is supplied, so the break-even has no target to test against | no | CEO | recommendation-draft |
| `Q-004` | Which budget figure binds the pilot spend, the monthly budget of USD 77.41 or the metered ceiling of USD 34.42, and how do the two relate? | no | CEO | recommendation-draft |
| `Q-005` | Is the pilot content in Vietnamese or English? The language is not stated, and a standing decision sets the first channel as English | no | CEO | option-synthesis |
| `Q-006` | The CEO's residency, age, and tax status are not supplied, so the eligibility conditions in R-001 cannot be applied to the CEO | no | CEO | recommendation-draft |
| `Q-007` | Does the downstream input role of this artifact in technical-validation accept it? A recorded framework defect (identifier drift between a producer's output and a consumer's accepted inputs) was open at its last check | no | orchestrator | technical-validation |
| `Q-008` | The current text of the five standing decisions and the current state of the delivered pipeline and frozen branch are not in the supplied inputs; the research question's summaries are the only basis here | no | context-agent | technical-validation |
| `Q-009` | How do the Framing Gate decision and the answers to these questions reach the technical-validation dispatch? A recorded framework finding says a gate rationale reaches no downstream phase unless restated in its dispatch | no | orchestrator | technical-validation |

## Handoff

- Downstream owner: context-agent, for the technical-validation phase, after the Framing Gate.
- Gate: Framing Gate (research). Decided by product-owner under the Producer Exclusion Rule; business-analyst produced this evidence and records no decision on it.
- Evidence for the gate: this artifact at verdict `partially-framed` and status `provisional`. Blocking questions Q-001 and Q-002 need an owner's answer before the framing can be accepted. The Requirements table defines the research scope; B-001 and B-011 bound what this framing does not decide.
- Deferred to downstream: acceptance thresholds and verification design (product-owner, qa); the content-form choice (CEO); the reuse approach (architect); pilot task breakdown (planner); current-state facts about the pipeline and the decision text (context-agent); legal conclusions (local counsel). The Framing Gate decision and any answer to a blocking question must be restated in the technical-validation dispatch (Q-009).
