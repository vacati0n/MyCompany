# MC-10: Wave 9: the autonomy capability - planning, scheduling, channel lifecycle, experiments, budget allocation and learning as recorded proposals over recorded data - with nothing enacted without a recorded owner approval

- Source: local://MASTER-PLAN.md#35-development-waves
- Issue type: Story   Priority: High
- Labels: implement, wave-9, autonomy, proposals, channel-lifecycle
- Routed as: /implement (feature-request)

## Description

WAVE 9 — BUILD THE AUTONOMY CAPABILITY AS PROPOSALS OVER RECORDED DATA; NOTHING ENACTS WITHOUT A RECORDED OWNER APPROVAL

Waves 0 (`run-258e0a3415d2`), 1 (`run-dd80173faaad`), 2 (`run-3a58551ee912`), 3 (`run-ad369fe67ded`), 4 (`run-5d5e6bd74c36`), 5 (`run-423e990b743d`), 6 (`run-cdce6ebe03ac`), 7 (`run-56bcc037b633`) and 8 (`run-79ce8c121936`) are all Completed. Wave 9 builds on all nine and must not re-create any of them. At the Wave 8 head the solution carries 117 C# sources and 7 project files under `src/` and 42 C# sources and 5 project files under `tests/` (tracked files; confirm by counting, because every previous ticket's counts were stale by the time they were read). **736 tests pass against a live PostgreSQL 17** in the separate demonstration database (582 pass and 148 skip with a recorded reason without one); the architecture suite is at 63 and executes on every solution build. Eight ordered schema resources. The repository declares version 1.5.0.

### THE TENSION IN THIS WAVE, STATED BEFORE ANYTHING ELSE

MASTER-PLAN section 35 specifies Wave 9 as **the autonomous company: autonomous planning, autonomous scheduling, channel lifecycle, experimentation, budget allocation, continuous learning. The human CEO remains the ultimate authority, and D-002 relaxes only by an explicit recorded CEO decision.**

**There is still no production series, no recorded operation and no published item** (the company store holds zero tables). Wave 8's weekly brief reads unmeasured or recorded on every line. Autonomy over nothing measured is planning over assumptions; a budget allocator with no observed spend allocates configured numbers; a learning system with no outcomes learns nothing.

**This ticket therefore scopes the capability as proposals**: the planner, the scheduler, the channel lifecycle, the experiment record, the budget allocator and the learning record each produce **recorded proposals** over the recorded data Waves 4 to 8 deliver, each carrying its measurement case and the readings it rests on, and **none of them enacts anything**: enacting stays a transition-table state that only the owner's recorded approval reaches (D-002 unrelaxed). A proposal over an unmeasured reading is not made and says so. Unless the owner records a decision at the start of the wave, **no metered call, no channel creation and no publication**; if the scope phase judges the capability unbounded without that decision, **raise it as a blocking question**.

### GOAL

Make autonomy reviewable before it is trusted: every autonomous decision the company could take exists first as a recorded, deterministic, labelled proposal the CEO can approve or send back from the weekly brief, and nothing changes in production, budget or channels without that approval.

### WHAT IS MEASURABLE AND BUILDABLE NOW, WITHOUT SPEND

1. **Planning and scheduling proposals**: a weekly production plan and schedule proposed from the recorded registers, the master plan's committed rate (a programme figure, labelled recorded) and the recorded constraints (the dated platform changes, the coded preconditions, the owner's debts). Wave 8 found no production plan recorded anywhere; this wave gives it a recorded home whose entries start as proposals.
2. **Channel lifecycle as a transition table**: the states a channel moves through (proposed, experiment, live, paused, retired) and the guards on each transition — the payee position (`CEO-D-500`), the approval-workload re-examination before any second channel, library registration before first publication — with every transition to a live state requiring the owner's recorded approval. No channel is created, configured as live or routed to.
3. **The experiment record**: hypothesis, the reading it is judged on, the minimum observation count (only where an owner decision records one; otherwise the experiment cannot conclude and says so), and its outcome, held in the measurement union.
4. **Budget allocation proposals**: per-channel and per-department allocations proposed within the owner's decided ceiling (USD 34.42 metered, `CEO-D-702`) and the USD 77.41 monthly budget, never above either, never enacted without approval, never over an unmeasured spend reading presented as headroom.
5. **Continuous learning as recorded lessons**: lessons recorded from observed outcomes only, each citing its observations; with no observations, the learning record is empty and the brief says so.
6. **The brief and dashboard**: Wave 8's one-read brief gains a bounded proposals section; the dashboard still never approves, configures or publishes.
7. **Carried from Wave 8**: the items in `wave-8/release-note.md` routed to the implementer or tech lead, and any owner answers recorded as `CEO-D-8xx`.

### WHAT IS NOT MEASURABLE NOW, AND MUST NOT BE SIMULATED

Any production, throughput, quality, approval-minutes, revenue or per-item cost figure; any experiment outcome; any learned lesson; any trend, forecast or allocation justified by a series that does not exist. A proposal resting on a configured number must say so.

### BINDING CONSTRAINTS CARRIED FORWARD

1. **Nothing publishes.** The five structural absences on the upload path stay.
2. The three coded preconditions stay encoded and undischarged; satisfied rows only as fixtures in self-dropping demonstration stores.
3. **Budget USD 77.41 per month**, unchanged; the company metered ceiling is USD 34.42 (`CEO-D-702`); the authorised single-metered-operation exception unspent and available.
4. **Code first, AI when necessary**; proposals are deterministic; no executive or narrative model call (`CEO-D-700` stands until the first video exists).
5. **Owner approval remains a transition-table state**, absent from configuration; D-002 is not relaxed; no autonomous path reaches production, budget, channel or publication state.
6. **One legal entity, several channels** (`CEO-D-500`); no second channel is created, configured as live or routed to.
7. Ten comparable runs per task before an evidence ranking (`CEO-D-701`); the corpus is unpopulated.
8. No clip count, threshold, cadence, observation minimum, channel budget amount, channel configuration value, level-to-tier mapping or sizing claim may be invented.
9. Niche-agnostic and re-pointable.

### FRAMEWORK DEFECTS TO BRIEF EVERY SUBAGENT ON

All eleven in `research/framework-defects.md`, plus the twelfth measured in Wave 8: a recorded policy exception does not widen the permitted write set at completion, so a re-dispatched phase that declares the excepted path is rejected again. Give every agent its own staging folder, and never brief a non-implementing role to write a repository file.

### PRICING BASIS

Meter against option O-002 — USD 2.647440 per item variable, USD 42.99 per month standing, USD 5.95 per item all-in at 13 items per month. Every figure derived from the assumed reasoning-tier split is an assumption and must be labelled as such. Development cost and per-item cost are separate quantities and must not be netted. Wave 8 closed at the figure recorded in `wave-8/cost-ledger.md`.

## Acceptance criteria

_Not provided in the ticket. To be defined and approved at the scope gate before implementation starts._
