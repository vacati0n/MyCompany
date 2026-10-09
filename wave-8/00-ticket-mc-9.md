# MC-9: Wave 8: the AI-management capability - weekly executive reports, rule-based recommendations and the CEO dashboard as code over recorded data - with no executive model call without an owner decision

- Source: local://MASTER-PLAN.md#35-development-waves
- Issue type: Story   Priority: High
- Labels: implement, wave-8, ai-management, weekly-reporting, ceo-dashboard
- Routed as: /implement (feature-request)

## Description

WAVE 8 — BUILD THE AI-MANAGEMENT CAPABILITY AS CODE OVER RECORDED DATA; NO EXECUTIVE MODEL CALL WITHOUT AN OWNER DECISION

Waves 0 (`run-258e0a3415d2`), 1 (`run-dd80173faaad`), 2 (`run-3a58551ee912`), 3 (`run-ad369fe67ded`), 4 (`run-5d5e6bd74c36`), 5 (`run-423e990b743d`), 6 (`run-cdce6ebe03ac`) and 7 (`run-56bcc037b633`) are all Completed. Wave 8 builds on all eight and must not re-create any of them. At the Wave 7 head the solution carries 107 C# sources and 7 project files under `src/` and 39 C# sources and 5 project files under `tests/` — confirm by counting, because every previous ticket's counts were stale by the time they were read. **690 tests pass against a live PostgreSQL 17** in the separate demonstration database (555 pass and 129 skip with a recorded reason without one); the architecture suite is at 58 and executes on every solution build. Seven ordered schema resources. The repository declares version 1.4.0.

### THE TENSION IN THIS WAVE, STATED BEFORE ANYTHING ELSE

MASTER-PLAN section 35 specifies Wave 8 as **AI management: COO, CTO, CFO agents; weekly reporting; recommendations; CEO dashboard.** Sections 8 and 64 (and the deferral table) say the COO, CTO and CFO are **reporting and analysis agents run weekly against recorded data**, carrying no production permissions, and that **until the sustained rate exists the weekly reports are generated from recorded data by code, with one L3 pass for the recommendation narrative**. The CEO interface is a surface, not an agent. The weekly review must be absorbable in 10 to 15 minutes (section 34).

**There is no sustained rate, no production series and no recorded operation in the company store** (it holds zero tables). Every reading a weekly report would show is unmeasured today. An L3 recommendation narrative is a metered model call, which is spend; seven waves have spent USD 0.00 and the single-metered-operation exception is unspent.

**This ticket therefore scopes the capability as code**: the COO, CTO and CFO weekly reports and the risk summary composed deterministically from the recorded readings Waves 4 to 7 deliver, every figure carrying its measurement case (observed, observed zero, unmeasured, or recorded for configured amounts) and rendering an unmeasured reading visibly as unmeasured; recommendations as deterministic, labelled rules over those readings (never a model call, never a configured number presented as a measurement); and a read-only CEO dashboard surface over the same single read. **No executive model call and no L3 narrative pass** unless the owner records a decision authorising it; if the scope phase judges the capability unbounded without that decision, **raise it as a blocking question**.

### GOAL

Make the weekly review real as code: the CEO can open one report built from one consistent read of the company's records, see each figure's measurement case, and see which owner decisions and questions are open — in 10 to 15 minutes — without any model call.

### WHAT IS MEASURABLE AND BUILDABLE NOW, WITHOUT SPEND

1. **The weekly report set** (sections 32 to 34): COO operational report, CTO weekly report (cost and quality per task, the cost controller's actions, the selection basis — configured ordering or evidence — and the platform-policy re-verification status until 2027-02-01), CFO financial summary (company and per-channel budget readings against the recorded allotment and envelope), channel performance and the risk summary. Every line from **one read at one datastore instant**; the report's own period finality stated.
2. **Recommendations as deterministic rules**: each names the reading it rests on and that reading's measurement case; a rule over an unmeasured reading produces no recommendation and says so. The CTO's qualitative-review label applies until ten comparable runs per task exist (section 18).
3. **The CEO dashboard**: a read-only surface over the report read; owner approval remains a transition-table state and the dashboard never approves, configures or publishes.
4. **The open-decisions register**: owner questions and decisions surfaced in the report from a recorded source, not from prose.
5. **Carried from Wave 7**: the items in `wave-7/release-note.md` routed to the implementer or tech lead (the stale two-session evidence row, the price-capture versus re-fetch question, the deferred request not re-admitted, the decision reading columns' numeric bound).

### WHAT IS NOT MEASURABLE NOW, AND MUST NOT BE SIMULATED

Any production, throughput, quality, approval-minutes, revenue or per-item cost figure; any recommendation narrative; any trend over a series that does not exist. A zero meaning none occurred and a zero meaning never measured are different values and must not render identically.

### BINDING CONSTRAINTS CARRIED FORWARD

1. **Nothing publishes.** The five structural absences on the upload path stay.
2. The three coded preconditions stay encoded and undischarged; satisfied rows only as fixtures in self-dropping demonstration stores.
3. **Budget USD 77.41 per month**, unchanged; the cost controller's interim basis is the USD 34.42 metered allotment (recorded, not observed); the authorised single-metered-operation exception unspent and available.
4. **Code first, AI when necessary**; the deterministic boundary assertions run on every solution build; the executive reports are deterministic.
5. **Owner approval remains a transition-table state**, absent from configuration; the executive layer carries no production permission.
6. **One legal entity, several channels** (`CEO-D-500`); no second channel is created, configured as live or routed to.
7. **No metered benchmark** was run in Wave 7 (`CEO-D-600`); the corpus is unpopulated and every ranking is the labelled configured ordering.
8. No clip count, clean-record threshold, approval-workload figure, sizing claim, channel budget amount, channel configuration value, observation threshold or level-to-tier mapping may be invented.
9. Niche-agnostic and re-pointable.

### FRAMEWORK DEFECTS TO BRIEF EVERY SUBAGENT ON

All eleven in `research/framework-defects.md`. Give every agent its own staging folder.

### PRICING BASIS

Meter against option O-002 — USD 2.647440 per item variable, USD 42.99 per month standing, USD 5.95 per item all-in at 13 items per month. Every figure derived from the assumed reasoning-tier split is an assumption and must be labelled as such. Development cost and per-item cost are separate quantities and must not be netted. Wave 7 closed at 2,912,242 tokens with 17.0 percent rework and zero validator rejections.

## Acceptance criteria

_Not provided in the ticket. To be defined and approved at the scope gate before implementation starts._
