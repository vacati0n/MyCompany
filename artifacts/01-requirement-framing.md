```yaml
requirementFraming:
  framingId: FRAME-2026-0001
  subject: AI-operated multi-channel media company operating system
  sourceInputs:
    - type: business-intent
      reference: tasks/MC-1/input.md
  producedBy: omn-business-analyst
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: provisional
  framingVerdict: partially-framed
  requirementCount: 67
  inputDigest: sha256:3abf07f0cddfb904a8a6e131f1bdee80
  contextDigest: sha256:a9651d8bef4354f700f76ff4e9933028
```

## Metadata

- Subject: AI-operated multi-channel media company operating system
- Requested by: the human owner, identified in `business-intent` as the company's CEO and owner
- Decision owner: `omn-product-owner`, as the deciding owner of the investigate Framing Gate
- Workflow phase: problem-framing
- Framing date: 2026-09-18

## Business Context

- Business intent: Establish a small media company that one human owner owns and that an AI workforce operates, earning net profit by producing original video content, publishing it across multiple channels and platforms, building audiences, qualifying for platform monetization, and doing so under explicit financial, quality, copyright, and platform-compliance controls.
- Problem statement: There is no operating arrangement under which a single owner can run a multi-channel video business without supervising individual videos, and without that arrangement the business cannot hold the controls its earnings depend on: verified rights to every asset it publishes, compliance with each platform's own rules, a quality floor, attributable cost, and continuity when a capability the work depends on becomes unavailable.
- Affected stakeholders: the human owner as ultimate authority and weekly reviewer; the operations, technology, finance, strategy, editorial, creative, production, compliance, distribution, and analytics roles of the AI workforce described in `business-intent`; the audience of each channel; the publishing platforms whose policy regimes govern monetization; rights holders of any third-party asset; the providers of the AI capacity the workforce consumes.
- Current-state pain: As supplied, the company does not yet operate, so no baseline of revenue, cost, audience, or throughput exists; `business-intent` states the cost of getting the arrangement wrong rather than the cost of a present operation, naming dependence on third-party content, single-provider dependence, unnecessary AI expense, unverifiable rights, publication without compliance gates, and owner time consumed by per-video supervision. The absence of a supplied baseline is recorded at `Q-004` and `AS-003`.

## Target Outcomes

The outcomes the business is buying. One row per outcome, stated as a business result rather
than as a capability to build. Requirements are aligned to these rows and to nothing else.

| ID | Outcome | Measure | Business Driver |
|---|---|---|---|
| `O-001` | The company earns sustainable net profit from its published output | Net profit, profit per channel, and cost per unit of revenue, as named in `business-intent` | Net profit is the stated primary business objective |
| `O-002` | Audiences receive content of genuine value and keep returning | Views, click-through rate, retention, watch time, subscribers, and returning viewers | Audience value is the condition on which earnings rest |
| `O-003` | Published content is the company's own original work | No measure supplied; recorded as absent, see `Q-002` | Originality is a stated first principle and the basis of the earning model |
| `O-004` | The company carries no unmanaged copyright exposure | Copyright incidents, and the proportion of published assets whose permission basis is established | Copyright safety is a stated constraint on profit |
| `O-005` | Every channel stays compliant with each platform it publishes on and eligible for monetization | Policy incidents and monetization status per channel | Monetization eligibility is the revenue precondition |
| `O-006` | Planned production is delivered on schedule and at the required quality | Production throughput, planned against published volume, and delay count | Operational efficiency is a stated first principle |
| `O-007` | The company's AI expenditure stays proportionate to the value it produces | Cost per video, cost per 1,000 views, cost per unit of revenue, and total AI spend | AI cost efficiency is a stated first principle |
| `O-008` | The company operates many channels without redesigning itself and without interruption when a capability becomes unavailable | Number of channels operated under one unchanged arrangement, and continuity through provider or quota failure | Scalability and provider independence are stated first principles |
| `O-009` | The owner governs the business rather than the workforce, while keeping authority over major decisions | Owner time to understand the company position at the weekly review, and the proportion of reserved decisions taken with approval | The stated goal that the owner manages the business, not the agents |
| `O-010` | The company improves its content and its economics from recorded evidence | No measure supplied; recorded as absent, see `Q-003` | The stated requirement that the company evolve on evidence |

## Requirements

One row per expectation. Every requirement names the outcome it serves, so a requirement
that traces to nothing is either an outcome this artifact failed to declare or a requirement
that belongs to another change.

| ID | Requirement | Type | Outcome Ref | Priority |
|---|---|---|---|---|
| `R-001` | Channel lifecycle transitions follow evidence-based rules and are not triggered by a single video's result | functional | `O-001` | must |
| `R-002` | Every cost is attributed to the video, channel, department, workforce role, and capability that incurred it | functional | `O-001` | must |
| `R-003` | Forecasts and expected values are labelled as estimates carrying their uncertainty and are never presented as recorded fact | non-functional | `O-001` | must |
| `R-004` | Operating profit is determinable for the company and for each channel from recorded revenue and every cost category | functional | `O-001` | must |
| `R-005` | Revenue is recorded against the channel that earned it and the source it came from | functional | `O-001` | must |
| `R-006` | Profit allocation between owner profit, reinvestment, risk reserve, and operating budget is configurable, and no allocation rate is fixed without owner approval | functional | `O-001` | should |
| `R-007` | Before publication each video is verified for content, visual, audio, policy, and asset-clearance quality, independently of the work assessed | functional | `O-002` | must |
| `R-008` | Each channel declares its target audience, audience needs, content pillars, brand position, differentiation, and monetization strategy before content is produced for it | functional | `O-002` | must |
| `R-009` | Factual claims in published content are verified against identified reliable sources, their provenance is retained, and unverifiable or conflicting claims are flagged rather than published as fact | functional | `O-002` | must |
| `R-010` | Performance data is collected per video and per channel after publication wherever the platform makes it available | functional | `O-002` | must |
| `R-011` | Titles, thumbnails, and metadata represent the content accurately and are neither deceptive nor spam | non-functional | `O-002` | must |
| `R-012` | Every content idea is scored on its expected economic value rather than on expected views alone | functional | `O-002` | should |
| `R-013` | Each script is checked for suspicious similarity to its source material before production proceeds | functional | `O-003` | must |
| `R-014` | Every published script, narration, and narrative structure is the company's original work rather than a rewrite of another creator's material | non-functional | `O-003` | must |
| `R-015` | The company's earning capability does not depend on re-uploaded, minimally transformed, or scraped third-party content | non-functional | `O-003` | must |
| `R-016` | Visual, graphic, and audio assets are original, properly licensed, verified public domain, or generated where commercial use is permitted | non-functional | `O-003` | must |
| `R-017` | Copyright and compliance controls are not reduced, deferred, or bypassed in order to reduce cost | non-functional | `O-004` | must |
| `R-018` | Every third-party asset carries a rights record naming source, creator, licence type and reference, commercial-use and modification permission, attribution requirement, platform restrictions, expiry, proof of licence, assessed risk, and who verified it and when | functional | `O-004` | must |
| `R-019` | For every asset in a video the company can state the basis on which it is permitted to use it, and where that basis cannot be established the video is not published | functional | `O-004` | must |
| `R-020` | No role approves an exception to a control it is itself subject to | non-functional | `O-004` | must |
| `R-021` | Rights review is performed independently of the role that created or selected the asset, and the reviewing role can block publication but cannot publish | non-functional | `O-004` | must |
| `R-022` | Rights uncertainty results in rejection or escalation, and no transformation argument serves as a standing permission rule | non-functional | `O-004` | must |
| `R-023` | A video is published only when content, quality, copyright, and policy approvals all hold, and failure of any critical check blocks publication | functional | `O-005` | must |
| `R-024` | Before a platform is used, its current official rules on monetization eligibility, copyright, AI-generated content, reused and repetitious content, disclosure, and advertiser suitability are recorded from official sources with the source and the verification date | functional | `O-005` | must |
| `R-025` | Each recorded policy statement is labelled as official policy, documented recommendation, industry practice, or inference, and no policy is recorded that no source establishes | non-functional | `O-005` | must |
| `R-026` | Platform rules are treated as platform-specific, and a rule established for one platform is not applied to another without its own source | non-functional | `O-005` | must |
| `R-027` | Publishing is auditable: what was published, where, when, with which metadata and settings, and on whose approval | functional | `O-005` | must |
| `R-028` | Synthetic or AI-assisted content is disclosed wherever the publishing platform requires disclosure | functional | `O-005` | must |
| `R-029` | Recorded platform policy carries a last-verified date, and a platform policy change is raised as an alert that triggers re-verification | functional | `O-005` | should |
| `R-030` | A failed production or publishing step is detected, recorded, and retried or escalated, never silently dropped | non-functional | `O-006` | must |
| `R-031` | Each channel's publishing commitment is met from a production buffer, so publication does not depend on same-day production | functional | `O-006` | must |
| `R-032` | Every video moves through the company's defined production lifecycle and the outcome of each stage is recorded | functional | `O-006` | must |
| `R-033` | Normal production does not run on the company's weekly non-operating day, and only the named critical emergency operations run then | functional | `O-006` | must |
| `R-034` | Planned, produced, published, and delayed volume, queue depth, bottlenecks, failures, and workforce utilization are reported each week | functional | `O-006` | must |
| `R-035` | Work that does not depend on another step is not delayed by it | non-functional | `O-006` | must |
| `R-036` | Rejection rate, revision rate, copyright rejection rate, and policy rejection rate are measured | functional | `O-006` | should |
| `R-037` | A task never runs below its declared quality floor, and where no available capability meets that floor the task waits or is escalated | non-functional | `O-007` | must |
| `R-038` | Cost per video, per channel, per 1,000 views, per 1,000 watch minutes, per subscriber, and per unit of revenue are derivable | functional | `O-007` | must |
| `R-039` | Each department and channel operates under a configurable budget whose utilization is tracked against defined thresholds, and a breach is raised as an alert | functional | `O-007` | must |
| `R-040` | Each task declares a minimum acceptable quality, and the capability used for it is the least costly one that reliably meets that quality | functional | `O-007` | must |
| `R-041` | Every AI operation produces a cost record carrying consumption, cost, duration, and resulting quality, attributed to task, role, capability, channel, video, and department | functional | `O-007` | must |
| `R-042` | Work that deterministic means can perform reliably is not performed at AI cost | non-functional | `O-007` | must |
| `R-043` | A change in how capability is sourced is decided on total cost of ownership, including hardware, maintenance, engineering time, reliability, and opportunity cost, rather than on headline price | non-functional | `O-007` | should |
| `R-044` | Capability prices and limits are held as updatable configuration rather than as fixed company assumptions | non-functional | `O-007` | should |
| `R-045` | Identical work already performed and still valid is reused rather than repeated at cost | non-functional | `O-007` | should |
| `R-046` | Redundant, underused, overloaded, or uneconomic workforce roles are identifiable so that they can be merged or retired | functional | `O-007` | should |
| `R-047` | The capability, cost, latency, quality, success rate, and availability of each workforce role and each available capability are recorded, measured on representative tasks, and reviewed periodically | functional | `O-007` | should |
| `R-048` | Additional channels are operated without redesigning the company system | non-functional | `O-008` | must |
| `R-049` | Capacity is never obtained by means a provider's terms prohibit, and the company remains viable if multi-account capacity becomes unavailable | non-functional | `O-008` | must |
| `R-050` | Each channel is an independent business unit with its own identity, niche, audience, language, brand, content pillars, schedule, budget, revenue, cost, profit, risk profile, and monetization status | functional | `O-008` | must |
| `R-051` | No critical capability depends on a single provider, account, capability instance, or runtime | non-functional | `O-008` | must |
| `R-052` | Remaining capacity, utilization, reset time, and queued demand are known for each capacity source | functional | `O-008` | must |
| `R-053` | Required capacity is forecast from planned channel and video volume and compared against available capacity | functional | `O-008` | must |
| `R-054` | The company keeps operating through provider failure, quota exhaustion, unavailability, rate limiting, outage, quality degradation, and cost increase whenever the required quality can still be met | non-functional | `O-008` | must |
| `R-055` | Workforce capability is shared across channels, with channel-specific configuration supplying audience, brand, strategy, language, tone, visual identity, and schedule | non-functional | `O-008` | must |
| `R-056` | A recommendation put to the owner states the opportunity, its reasons, the required investment, the expected result, the main risks, at least one alternative, and the decision required | functional | `O-009` | must |
| `R-057` | Decisions reserved to the owner are never taken without the owner's approval | non-functional | `O-009` | must |
| `R-058` | Each role holds only the permissions its responsibilities require | non-functional | `O-009` | must |
| `R-059` | Every important action is recorded with who, what, when, why, inputs, outputs, decision, cost, and risk | functional | `O-009` | must |
| `R-060` | Every workforce role has defined responsibilities, inputs, outputs, permissions, constraints, quality criteria, failure conditions, escalation rules, and a cost budget | functional | `O-009` | must |
| `R-061` | Risks are registered by category and severity, and severity determines handling: critical blocks, high requires human review, medium adds further automated checks, low proceeds normally | functional | `O-009` | must |
| `R-062` | Routine operations proceed without owner involvement, and only exceptional cases escalate, along a defined escalation path | non-functional | `O-009` | must |
| `R-063` | The owner can understand the company's financial position, channel performance, production status, and risk position from a single weekly review | non-functional | `O-009` | must |
| `R-064` | Owner-facing reporting carries the measures that inform an owner decision and omits measures that do not | non-functional | `O-009` | should |
| `R-065` | After each video the company records what worked, what failed, and why, across topic, hook, title, thumbnail, audience, length, retention, distribution, timing, production cost, and revenue | functional | `O-010` | must |
| `R-066` | Company, channel, video, and role knowledge is retained and retrievable, so that a decision draws on the prior knowledge relevant to it | functional | `O-010` | must |
| `R-067` | A change to content, production, or cost approach is decided from recorded evidence rather than from preference or from copying the highest-view result | non-functional | `O-010` | should |

## Acceptance Intent

What acceptance would have to demonstrate. This is the intent a criterion is later written
against, not the criterion itself: it names what must be shown, and leaves the threshold and
verification design to the phases that own them.

| ID | Acceptance Intent | Requirement Ref | Demonstrated By | Priority |
|---|---|---|---|---|
| `AI-001` | A pause, scale, or archive decision for a channel rests on a run of recorded results rather than on one video | `R-001` | A channel lifecycle decision alongside the period of results it cites | must |
| `AI-002` | Any recorded cost resolves to the video, channel, department, role, and capability that incurred it | `R-002` | A cost record traced to its full attribution | must |
| `AI-003` | A forecast shown to the owner is distinguishable from recorded actuals and carries its uncertainty | `R-003` | An owner-facing report presenting estimates and actuals separately | must |
| `AI-004` | Operating profit can be produced for the company and for each channel over a closed period | `R-004` | A company and per-channel profit statement | must |
| `AI-005` | Recorded revenue resolves to the channel that earned it and the source it came from | `R-005` | A revenue breakdown by channel and source | must |
| `AI-006` | Allocation rates can be changed by configuration, and a change traces to owner approval | `R-006` | An allocation change with the approval it rested on | should |
| `AI-007` | A video reaching publication carries a completed verification across content, visual, audio, policy, and asset clearance, performed by a party other than the one that did the work | `R-007` | The verification record held for a published video | must |
| `AI-008` | A channel producing content has a recorded audience, needs, pillars, brand position, differentiation, and monetization strategy that predate that content | `R-008` | A channel definition dated before its first produced item | must |
| `AI-009` | A factual claim in a published video resolves to an identified source, and an unverifiable or conflicting claim is visibly flagged | `R-009` | The research provenance held for a published video | must |
| `AI-010` | A published video and its channel carry the performance data the platform makes available | `R-010` | A video and channel performance report | must |
| `AI-011` | A published title, thumbnail, and metadata set matches the content it presents | `R-011` | A review of published items against their content | must |
| `AI-012` | An approved idea carries a score built from demand, cost, monetization, risk, originality, and fit rather than from expected views alone | `R-012` | The scoring record behind an approved idea | should |
| `AI-013` | A script entering production carries a recorded similarity check against its source material | `R-013` | The originality check held for a produced script | must |
| `AI-014` | A published script is the company's own work rather than a restatement of another creator's script | `R-014` | The originality assessment held for a published video | must |
| `AI-015` | Published output and the revenue it earns come from original or licensed material rather than from re-used third-party content | `R-015` | A composition review of published output by asset origin | must |
| `AI-016` | Every asset in a published video is original, licensed, verified public domain, or generated where commercial use is permitted | `R-016` | The asset inventory of a published video with each asset's basis | must |
| `AI-017` | A cost reduction decision left the copyright and compliance controls intact | `R-017` | A cost decision recording the controls it did not touch | must |
| `AI-018` | Any third-party asset in use resolves to a complete rights record naming who verified it and when | `R-018` | A rights record for an asset drawn from published work | must |
| `AI-019` | For any published asset the company can state why it may use it, and an asset lacking that basis is absent from published output | `R-019` | A publication blocked because the basis could not be established | must |
| `AI-020` | An exception to a control was approved by a party other than the one the control applies to | `R-020` | An exception record showing the approving party | must |
| `AI-021` | A rights rejection blocks publication, and the rejecting role holds no permission to publish | `R-021` | A blocked video alongside the permissions held by the rejecting role | must |
| `AI-022` | An asset of uncertain rights was rejected or escalated rather than published | `R-022` | A rejection or escalation record for an uncertain asset | must |
| `AI-023` | A published video carries content, quality, copyright, and policy approvals, and a video failing a critical check did not publish | `R-023` | The approval set of a published video alongside a blocked example | must |
| `AI-024` | Before a platform carried company content, its current rules on the named subjects were recorded with source and date | `R-024` | The policy record held for a platform in use | must |
| `AI-025` | Each recorded policy statement shows whether it is official policy, documented recommendation, industry practice, or inference | `R-025` | A policy record showing the classification of each statement | must |
| `AI-026` | A rule applied to a platform resolves to a source established for that platform | `R-026` | A cross-platform comparison showing each rule's own source | must |
| `AI-027` | A publication resolves to what was published, where, when, with which settings, and on whose approval | `R-027` | The audit entry held for a publication | must |
| `AI-028` | Content published to a platform that requires disclosure carries that disclosure | `R-028` | A published item on a disclosure-requiring platform | must |
| `AI-029` | Policy records show when they were last verified, and a policy change produced an alert that led to re-verification | `R-029` | A policy change alert alongside the re-verification it triggered | should |
| `AI-030` | A failed production or publishing step appears as a recorded failure that was retried or escalated | `R-030` | The failure record and the resolution that followed it | must |
| `AI-031` | A scheduled publication was met from work completed before its due day | `R-031` | A publication compared with the completion date of its production | must |
| `AI-032` | A video resolves to the lifecycle stages it passed and their outcomes, with none missing | `R-032` | The stage history of a published video | must |
| `AI-033` | No normal production activity is recorded on the non-operating day, and any activity recorded there falls in a named emergency category | `R-033` | The activity record for a non-operating day | must |
| `AI-034` | The weekly operations report shows planned, produced, published, and delayed volume, queue depth, bottlenecks, failures, and utilization | `R-034` | A weekly operations report | must |
| `AI-035` | Work with no dependency on another step is not shown waiting on it | `R-035` | A production timeline for a video | must |
| `AI-036` | Rejection, revision, copyright rejection, and policy rejection rates are available for a closed period | `R-036` | A quality summary for a closed period | should |
| `AI-037` | A task that could not be served at its declared quality is shown waiting or escalated rather than completed below that quality | `R-037` | A queued or escalated task alongside its declared quality floor | must |
| `AI-038` | Cost per video, per channel, per 1,000 views, per 1,000 watch minutes, per subscriber, and per unit of revenue can be produced for a period | `R-038` | A cost efficiency summary | must |
| `AI-039` | Each department and channel shows its budget, its utilization, and an alert where a threshold was crossed | `R-039` | A budget utilization report including a threshold breach | must |
| `AI-040` | Each task type shows its declared minimum quality and the capability chosen to meet it | `R-040` | A task catalogue with declared quality and selected capability | must |
| `AI-041` | Any AI operation resolves to a cost record carrying consumption, cost, duration, quality, and its attribution | `R-041` | A cost record for an operation drawn from a produced video | must |
| `AI-042` | Work completed by deterministic means carries no AI cost | `R-042` | A task catalogue showing which work is performed without AI capability | must |
| `AI-043` | A sourcing change is accompanied by a total cost of ownership comparison rather than a price comparison | `R-043` | The comparison held behind a sourcing decision | should |
| `AI-044` | A price or limit change is applied by configuration without changing how the business operates | `R-044` | A price update alongside the unchanged operation around it | should |
| `AI-045` | Work already performed and still valid is shown reused rather than repeated at cost | `R-045` | A reuse record set against the cost it avoided | should |
| `AI-046` | Redundant, underused, overloaded, or uneconomic roles can be identified from recorded performance and cost | `R-046` | A workforce review listing candidates to merge or retire | should |
| `AI-047` | Each role and capability shows recorded cost, latency, quality, success rate, and availability measured on representative tasks | `R-047` | A capability comparison used in a periodic review | should |
| `AI-048` | A new channel begins operating through configuration rather than through redesign of the company system | `R-048` | A channel added and operating under the existing arrangement | must |
| `AI-049` | Capacity in use is consistent with each provider's terms, and the company still operates when only single-account capacity is available | `R-049` | A capacity inventory alongside the terms basis for each source | must |
| `AI-050` | Each channel shows its own identity, audience, brand, pillars, schedule, budget, revenue, cost, profit, risk profile, and monetization status | `R-050` | A channel record set | must |
| `AI-051` | Each critical capability shows at least one alternative able to carry it at the required quality | `R-051` | A map of critical capabilities to their alternatives | must |
| `AI-052` | Remaining capacity, utilization, reset time, and queued demand are available for each capacity source | `R-052` | A capacity view for a point in time | must |
| `AI-053` | Forecast capacity demand for planned volume is set against available capacity | `R-053` | A capacity forecast compared with the available pool | must |
| `AI-054` | Production continued through a provider failure or quota exhaustion where the required quality could still be met | `R-054` | An incident record showing work continuing through the failure | must |
| `AI-055` | Two channels are served by the same capability under different channel configuration | `R-055` | The configuration of two channels against the shared roles | must |
| `AI-056` | A recommendation put to the owner carries opportunity, reasons, investment, expected result, risks, an alternative, and the decision required | `R-056` | A recommendation as it was presented to the owner | must |
| `AI-057` | A reserved decision resolves to the owner's approval given before it took effect | `R-057` | A reserved decision alongside its approval record | must |
| `AI-058` | Each role's permissions match its responsibilities, with no permission it does not need | `R-058` | A permission listing per role | must |
| `AI-059` | An important action resolves to who, what, when, why, inputs, outputs, decision, cost, and risk | `R-059` | An audit entry for an action drawn from a produced video | must |
| `AI-060` | Each role shows its responsibilities, inputs, outputs, permissions, constraints, quality criteria, failure conditions, escalation rules, and cost budget | `R-060` | A role definition set | must |
| `AI-061` | A registered risk shows its category and severity, and the handling it received matches that severity | `R-061` | The risk register alongside the handling applied to each entry | must |
| `AI-062` | Routine operations completed without owner involvement, and the escalations that occurred followed the defined path | `R-062` | A period's escalation record set against its operational volume | must |
| `AI-063` | The owner can state the company's financial, channel, production, and risk position after one weekly review | `R-063` | A weekly executive brief alongside the owner's confirmation of what it told them | must |
| `AI-064` | Each measure in owner-facing reporting connects to a decision the owner takes | `R-064` | A reporting review mapping each measure to the decision it informs | should |
| `AI-065` | Each published video carries a recorded review of what worked, what failed, and why, across the named dimensions | `R-065` | A post-publication review for a video | must |
| `AI-066` | A decision shows the prior company, channel, video, or role knowledge it drew on | `R-066` | A decision record citing the retained knowledge it used | must |
| `AI-067` | A change to content, production, or cost approach resolves to the evidence behind it | `R-067` | A change decision alongside its experiment or its measured performance | should |

## Framing Boundaries

The edge of the framing. A named exclusion prevents requirement drift that an unstated one
does not.

| ID | Excluded Concern | Reason | Revisit Trigger |
|---|---|---|---|
| `B-001` | Technical approach, system structure, component and interface design, data model, and technology selection | `business-intent` supplies interface names, queue names, an entity list, and an architecture diagram; each is a mechanism, and the technical approach belongs to `architect` | A supplied mechanism turns out to constrain what must be true rather than how it is achieved |
| `B-002` | Work decomposition into waves, tasks, sequence, and estimates | The supplied wave plan and first-deliverable ordering are planning decisions owned by `planner` | The business states a sequencing constraint as a requirement in its own right |
| `B-003` | Which requirements are in or out of the first delivery | Scope is a decision owned by `omn-product-owner`; this framing states the problem, not the response to it | A scope decision removes or adds a target outcome |
| `B-004` | Acceptance thresholds and verification design, including quality floors, similarity limits, budget rates, and capacity targets | Thresholds belong to `omn-product-owner` and verification design to `omn-qa` | The business supplies a threshold, which is then recorded against the outcome it measures |
| `B-005` | The content of any platform's current policy | This agent reaches no external system, so no policy content can be established here; the framing requires that it be established, and `omn-context-agent` carries that in technical-discovery | Official policy sources are supplied as an input to this framing |
| `B-006` | Selection of AI capacity sources, plans, account quantities, and prices | `business-intent` asks for a recommended stack with options and trade-offs, which is option analysis owned by `omn-tech-lead` and a decision owned by the human owner | A sourcing limit is stated by the business as a rule the company must hold to |
| `B-007` | Legal determination of copyright, fair use, defamation, or jurisdictional exposure | The framing records the business rule of rejecting or escalating on uncertainty; it makes no legal judgement, and none sits within this agent's authority | Qualified legal input is supplied, see `Q-009` |
| `B-008` | The structure and content of the requested master plan document and architecture diagram | These are downstream deliverables about the company, not conditions the company must satisfy | The business states a reporting obligation the operating company itself must meet |
| `B-009` | Revenue, break-even, and return forecasts | Producing them needs market and historical data no supplied input carries, and `business-intent` requires that such figures not be presented as fact | Market or historical performance data is supplied, see `Q-010` |

## Assumptions

Every assumption states the basis it rests on. An assumption with no basis is an invented
requirement wearing an assumption's clothes.

| ID | Assumption | Basis | Confidence | Impact If False |
|---|---|---|---|---|
| `AS-001` | The requester and ultimate decision authority is the single human owner described in the supplied intent | `business-intent` states that the human user is the company's CEO and owner and the ultimate authority | high | The approval routes in `R-057` and the escalation path in `R-062` name the wrong authority |
| `AS-002` | The supplied intent is the complete statement of the business need for this framing | It is the only input the invocation envelope supplies, and it presents itself as the company's master statement | medium | Requirements the business holds are absent from the set, and the gate reads coverage that does not exist |
| `AS-003` | No channel, revenue, cost, or audience baseline exists yet | `business-intent` describes a company to be established and a first stage carrying research and architecture rather than operation | medium | Outcome measures stated in absolute terms should have been stated relative to an existing baseline |
| `AS-004` | The organization chart and role list describe how responsibility is allocated, not how many roles must exist | `business-intent` states explicitly that the chart is not to be read as one agent per box and that responsibilities may be combined | high | `R-060` is read as mandating a fixed workforce composition, which would be a design decision this framing does not hold |
| `AS-005` | The figures in the supplied intent, such as example production costs, capacity percentages, candidate provider combinations, and allocation rates, are illustrative | `business-intent` labels them as examples and forbids hard-coding prices, policy, and allocation rates | high | Illustrative figures are carried downstream as targets or as recorded business rules |
| `AS-006` | This framing concerns a new line of business rather than the existing software delivery product described in the supplied product context | The supplied business intent describes a media company and names no relationship to the existing product goals or success metrics | low | The outcomes are aligned to the wrong product, and the two sets of success measures compete; see `Q-001` |
| `AS-007` | The stated publishing rate per channel and the six-day operating week are business commitments rather than illustrations | `business-intent` states them as the schedule, sets an explicit non-operating day, and permits only the choice of specific days to be optimised | medium | Throughput and capacity requirements in `R-031`, `R-033`, and `R-053` are framed against a commitment the business did not make |

## Open Questions

| ID | Question | Blocking | Owner | Needed By |
|---|---|---|---|---|
| `Q-001` | Does this framing sit inside the existing product context, whose goals and success metrics concern software delivery, or does it open a separate line of business with its own measures? | yes | `omn-product-owner` | Framing Gate |
| `Q-002` | Which measure tells the business that the original-content outcome `O-003` is being achieved? No measure is supplied | no | `omn-product-owner` | Framing Gate |
| `Q-003` | Which measure tells the business that the continuous-improvement outcome `O-010` is being achieved? No measure is supplied | no | `omn-product-owner` | Framing Gate |
| `Q-004` | What target values apply to profit, audience growth, quality floors, cost per video, and capacity? None are supplied, and no operating baseline exists to derive them from | no | `omn-product-owner` | scope definition, before option-analysis |
| `Q-005` | What are the current official policies of each intended publishing platform? They are not supplied, this agent reaches no external source, and `R-023` through `R-029` all depend on them | yes | `omn-context-agent` | technical-discovery |
| `Q-006` | Which platforms will the company publish on, and in which markets and languages? The intent names candidates and leaves the set open | no | `omn-product-owner` | technical-discovery |
| `Q-007` | Which revenue sources are in the business model at launch? Platform advertising is named as primary and sponsorship, affiliate, and digital products as possible, without a decision | no | `omn-product-owner` | option-analysis |
| `Q-008` | Where is the line between a routine decision the workforce takes alone and a decision reserved to the owner, and what makes an investment, budget increase, or strategic change major? Without it `R-057` and `R-062` cannot both be applied in operation | yes | `omn-product-owner` | Framing Gate |
| `Q-009` | Who is accountable for the legal determinations that `R-022` escalates out of the workforce, given that the intent names escalation but no legal authority other than the owner? | no | `omn-product-owner` | technical-discovery |
| `Q-010` | What funding is available at start, and what cost envelope must the first channel operate within? No figure is supplied | no | `omn-product-owner` | option-analysis |
| `Q-011` | Does the weekly non-operating day in `R-033` apply to publication as well as to production, given that publishing targets are set by weekday? | no | `omn-product-owner` | Framing Gate |
| `Q-012` | Will human contributors work alongside the AI workforce, and under whose approval? The intent lists external contributor costs while describing the workforce as entirely AI | no | `omn-product-owner` | Framing Gate |
| `Q-013` | What retention and privacy expectations apply to research material, analytics, audit records, and audience data? The intent names privacy as a risk category but states no expectation | no | `omn-product-owner` | technical-discovery |
| `Q-014` | What protection is required for the platform and provider account credentials the company must hold? The intent forbids sharing them but states no handling expectation | no | `architect` | technical-discovery |
| `Q-015` | What volume and turnaround expectations apply beyond the stated per-channel publishing rate, and to how many channels must the company scale? The intent names channel counts only as illustration | no | `omn-product-owner` | option-analysis |

## Handoff

- Downstream owner: `omn-context-agent`, which consumes this framing in the `technical-discovery` phase of this run, after the Framing Gate.
- Gate: the investigate Framing Gate, decided by `omn-product-owner`; under the Producer Exclusion Rule this agent co-owns that gate but does not decide it, because this artifact is the evidence assessed there.
- Evidence for the gate: the ten target outcomes `O-001` to `O-010` with their measures and the two measures recorded absent; the sixty-seven requirements `R-001` to `R-067`, each typed and aligned to an outcome; the acceptance intent `AI-001` to `AI-067` bounding every requirement; the nine framing boundaries `B-001` to `B-009`; the seven assumptions `AS-001` to `AS-007` with their bases; and the fifteen open questions `Q-001` to `Q-015`, of which `Q-001`, `Q-005`, and `Q-008` are blocking and cap the verdict at partially-framed. The supplied intent is referenced by digest in the metadata block and is not restated here.
- Deferred to downstream: the technical approach, system structure, and data model to `architect` per `B-001`; scope and acceptance thresholds to `omn-product-owner` per `B-003` and `B-004`; wave, task, and sequence decomposition to `planner` per `B-002`; establishment of current platform policy to `omn-context-agent` per `B-005`; capacity sourcing options and financial forecasting to `omn-tech-lead` per `B-006` and `B-009`; verification design to `omn-qa` per `B-004`; and legal determination to the authority named in answer to `Q-009` per `B-007`.
