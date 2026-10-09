# MC-8: Wave 7: the AI-economics capability - benchmark record, evidence-ready model selection and cost controller - with no metered benchmark run without an owner decision

- Source: local://MASTER-PLAN.md#35-development-waves
- Issue type: Story   Priority: High
- Labels: implement, wave-7, ai-economics, benchmark, cost-controller
- Routed as: /implement (feature-request)

## Description

WAVE 7 — BUILD THE AI-ECONOMICS CAPABILITY; NO METERED BENCHMARK RUNS WITHOUT AN OWNER DECISION

Waves 0 (`run-258e0a3415d2`), 1 (`run-dd80173faaad`), 2 (`run-3a58551ee912`), 3 (`run-ad369fe67ded`), 4 (`run-5d5e6bd74c36`), 5 (`run-423e990b743d`) and 6 (`run-cdce6ebe03ac`) are all Completed. Wave 7 builds on all seven and must not re-create any of them. The source tree carries 98 C# sources and 7 project files under `src/` and 35 C# sources and 5 project files under `tests/` at the Wave 6 head; confirm by counting, because every previous ticket's counts were stale by the time they were read. **619 tests pass against a live PostgreSQL 17** in the separate demonstration database (517 pass and 96 skip with a recorded reason without one); the architecture suite is at 52 and executes on every solution build. The repository declares version 1.3.0.

### THE TENSION IN THIS WAVE, STATED BEFORE ANYTHING ELSE

MASTER-PLAN section 35 specifies Wave 7 as **AI economics: model router refinement, cost controller, benchmarks, budget controls, dynamic model selection, cost optimisation**, and states that the benchmark corpus (section 16) becomes populated here, which is what makes dynamic selection an evidence-based rather than a configured ordering.

**Populating a benchmark corpus with real observations requires metered model calls, which is spend.** Six waves have spent USD 0.00. The authorised single-metered-operation exception is unspent; one operation populates no corpus. The assumed reasoning-tier split behind every per-item cost figure remains untested for the fifth consecutive wave, because no production series exists.

**This ticket therefore scopes the capability and not the population of the corpus**, on the discipline that made Waves 3 to 6 work: build what evidence-based model selection would require of the system — a benchmark record whose every quantity is observed, observed zero or unmeasured; a selection rule that reads that record and **refuses to rank on an unmeasured quantity**, falling back to the recorded configured ordering and saying so; a cost controller that acts on the per-channel and company budget readings Wave 6 delivered; and the instrumentation that would make the assumed tier split decidable on observation. **No metered benchmark runs** unless the owner records a decision authorising it; if the scope phase judges the capability unbounded without that decision, **raise it as a blocking question**.

### GOAL

Make model selection evidence-ready: the router can read a benchmark record and rank on observed quality and cost when observations exist, and visibly falls back to the configured ordering when they do not, with every quantity labelled by its measurement case.

### WHAT IS MEASURABLE AND BUILDABLE NOW, WITHOUT SPEND

1. **The benchmark record** (section 16): corpus entries, task class, model, route, observed quality score, observed cost, observed latency — each in the closed three-case union; table checks admitting exactly the permitted row shapes; the record horizon covering it.
2. **Selection that refuses to rank on the unmeasured.** A ranking over unmeasured quantities is a configured ordering and must be labelled as such, never presented as evidence.
3. **The cost controller**: acts on the Wave 6 per-channel and company budget readings at the 50/75/90/100 thresholds (downgrade tier, defer, refuse) as deterministic rules; never a model call; never a configured number presented as a measurement.
4. **Carried from Wave 6**: the booked-month admission window at the capability boundary (a call spanning a month end is admitted against one month and booked into the next); the sixth resource's header and nullable order column; the configuration store's six-base-key limit for the production and publishing keys; served-tier finality; the same-instant transition uniqueness error. All in `wave-6/release-note.md`.

### WHAT IS NOT MEASURABLE NOW, AND MUST NOT BE SIMULATED

Any benchmark score, latency or cost for a model that has not been called; the tier split; any per-item cost presented as measured. A zero meaning none occurred and a zero meaning never measured are different values and must not render identically.

### BINDING CONSTRAINTS CARRIED FORWARD

1. **Nothing publishes.** The five structural absences on the upload path stay.
2. The three coded preconditions stay encoded and undischarged; satisfied rows only as fixtures in self-dropping demonstration stores.
3. **Budget USD 77.41 per month**, unchanged; the authorised single-metered-operation exception unspent and available; re-fetch every unit price immediately before it is applied.
4. **Code first, AI when necessary**; the deterministic boundary assertions run on every solution build.
5. **Owner approval remains a transition-table state**, absent from configuration.
6. **One legal entity, several channels** (the owner's decision of 2026-10-08): the payee is company-level, never per channel. No second channel is created, configured as live or routed to.
7. No clip count, clean-record threshold, approval-workload figure, sizing claim, channel budget amount or channel configuration value may be invented.
8. Niche-agnostic and re-pointable.

### FRAMEWORK DEFECTS TO BRIEF EVERY SUBAGENT ON

All eleven in `research/framework-defects.md`. Give every agent its own staging folder.

### PRICING BASIS

Meter against option O-002 — USD 2.647440 per item variable, USD 42.99 per month standing, USD 5.95 per item all-in at 13 items per month. Every figure derived from the assumed reasoning-tier split is an assumption and must be labelled as such. Development cost and per-item cost are separate quantities and must not be netted. Wave 6 closed at 2,340,276 tokens with 4.4 percent rework and zero validator rejections.

## Acceptance criteria

_Not provided in the ticket. To be defined and approved at the scope gate before implementation starts._
