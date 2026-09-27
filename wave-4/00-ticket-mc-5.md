# MC-5: Wave 4: analytics, instrumented on what is measurable and dark on what is not

- Source: local://MASTER-PLAN.md#35-development-waves
- Issue type: Story   Priority: High
- Labels: implement, wave-4, analytics, instrumentation
- Routed as: /implement (feature-request)

## Description

WAVE 4 — ANALYTICS, WITH THE REVENUE-DERIVED DISPLAYS SHIPPED DARK

Wave 0 (`run-258e0a3415d2`), Wave 1 (`run-dd80173faaad`), Wave 2 (`run-3a58551ee912`) and Wave 3 (`run-ad369fe67ded`) are all Completed. Wave 4 builds on all four and must not re-create any of them. The source tree carries 89 production files and 27 test files; 438 tests pass against a live PostgreSQL 17.

### GOAL

Build video analytics, channel analytics, and the cost, profit and learning surfaces named in MASTER-PLAN section 35 for Wave 4 — **instrumenting only what is measurable now, and shipping the revenue-derived displays dark.**

### THE CONSTRAINT THAT SHAPES THIS WHOLE WAVE

MASTER-PLAN section 27.5 establishes that **six of the specified metrics are uncomputable today**: revenue, RPM, profit per video, profit per channel, ROI, and cost per dollar of revenue. All three primary business metrics are among them. Each requires advertising revenue that cannot exist until the programme thresholds are met, and **no supplied source states a revenue-per-thousand-views parameter, so even a modelled version cannot be produced.**

Section 72 of the same brief forbids building "a dashboard full of meaningless metrics". **Displaying those six now produces exactly that, and worse — it invites decisions taken against placeholder numbers that look like measurements.**

**Therefore: the revenue-derived displays are BUILT AND SHIPPED DARK.** They become visible when the first revenue parameter is **observed rather than assumed**. Deferring them costs nothing.

**This is the same discipline the previous two waves were graded on**, and it should be enforced structurally rather than editorially: a figure derived from an assumption must be incapable of presenting itself as a measurement.

### WHAT THERE IS NO DATA FOR, AND WHY THAT IS THE POINT

**The channel has published nothing.** It is not in the programme, has no subscribers, no watch hours, no views, no impressions and no revenue. Wave 3 built the publishing capability and deliberately published nothing; the one produced item remains held.

So Wave 4 is building **an analytics capability with no audience data to analyse**. Scope it accordingly:

- **Measurable now, from the company's own records:** cost per item and per period from the operation records; the reasoning tier requested and actually served; approval minutes with review, queue and rework separated; production-path stage outcomes and refusals; supply-audit results; compliance determination outcomes.
- **Not measurable now, and not to be simulated:** everything requiring a published item or an audience.

**An analytics surface fed by no observation must say so.** A zero that means "none occurred" and a zero that means "never measured" are different values and must not render identically — Wave 3 established this rule for approval components and it generalises here.

### THE CARVE-OUT THAT IS STILL UNSPENT

The owner authorised **one narrow, named exception** to the no-spend constraint so that at least one served reasoning tier is recorded. **Wave 3 did not use it.** The capability is built: the design makes the served tier readable from the admitted route, the meter carries both fields and an explicit absence marker, and an implementation defect that would have made the measurement meaningless — a test double silently dropping both tier fields — was found and fixed.

**Wave 4 is the natural place to spend it, and it is squarely an analytics concern.** What it must produce: at least one operation record carrying both the tier requested and the tier actually served, the latter read from the admitted route and **never inferred from the request**, with an explicit absence marker where a route cannot state one.

**And the caveat must survive into every artifact:** one record establishes that the mechanism records what it claims to record. **It does not settle the assumed 290,000 / 87,000 split**, which needs a production series. Two waves have now closed with every figure derived from that split still an assumption rather than a measurement.

### BINDING CONSTRAINTS CARRIED FORWARD

1. **Nothing publishes.** No upload, no channel creation, no account creation, no purchase, no subscription session, no render. The five structural absences that make the upload path unable to fire are load-bearing and must not be weakened — none of them may become a configuration value.
2. **The three coded preconditions are encoded and NOT discharged**: library registration not done, payment account does not exist, two-step verification unconfirmed. Each refuses from recorded state, and unknown folds to not-satisfied while staying visible in the refusal. Discharging them needs the owner.
3. **Budget USD 77.41 per month**, unchanged. The second stock library is not authorised. Re-fetch every unit price immediately before it is applied; the next policy re-verification is due 2026-10-26.
4. **Code first, AI when necessary.** Deterministic tasks must be structurally incapable of a model call, verified by the build-time boundary test. Analytics is almost entirely deterministic work and should be structurally incapable of a model call.
5. **Owner approval remains a state in the gate transition table and absent from configuration.** Analytics must not become a route around it.
6. **Niche-agnostic and re-pointable.** The subject is a recorded experiment, not the final business bet.

### CARRIED IN, NOT RESOLVED

- **16 known issues** from the Wave 3 release note, including: transport behaviour wholly unexercised because no transport exists; the transport screen's fixed name-list limitation; no production caller yet driving the publishing sequence end to end; and the architecture-suite count at the corrected head being unrecorded in run evidence.
- **One open question with the architect**: what, if anything, should be recorded at the gate transition the implementation declined to write.
- **Supply is unestablished for both the species and the behaviour.** Zero clip counts were obtained across 14 audited subjects. **No clip count may be invented under any circumstances.**
- **The approval-minutes series does not exist.** One measurement of 1 min 58 s exists, taken on a held item by an owner who already knew its contents with no queue time. It is a lower bound of the weakest class. **No clean-record threshold may be derived from it**, and the threshold question stays with the owner until a series exists that includes at least one approval which returned a change request.

### FRAMEWORK DEFECTS TO BRIEF EVERY SUBAGENT ON

These are measured in this programme, not theoretical. `research/framework-defects.md` carries them in full.

1. **`declared_side_effects` takes bare written file paths only.** Prose there is misclassified as an undeclared write and has permanently halted a run. For a reviewer: list only the files you wrote, **never the paths of the change under review**.
2. **No foreign identifier tokens in framework artifacts.** An identifier from another document fails two mutually exclusive ways and no token form passes both. **Source-qualified prefixes do not help** — the scanners use a word-boundary pattern and a hyphen is a word boundary. Cite descriptively.
3. **Write at least three acceptance criteria per task.** The field parser swallows the first nested bullet, so one written criterion parses as none and two parse as one.
4. **Do not sign a decision record.** Signing is the gate's act; the check enforces it.
5. **Do not express a requirement trace as a range.** Enumerate the identifiers.
6. **The string "claude" must not appear in any framework artifact**, including file paths and branch names.
7. **A version field must be a recognisable version string**, not a ticket label.
8. **Read the validator source rather than guessing** when a check names only its first offender. Five agents have avoided wasted attempts this way.

### PRICING BASIS

Meter against option O-002 — 377,000 input and 33,000 output tokens, USD 2.647440 per item variable, USD 42.99 per month standing, USD 5.95 per item all-in at 13 items per month. The USD 1.58 figure is superseded and comparing against it manufactures a false overrun. Development cost and per-item cost are separate quantities and must not be netted.

**Note on measuring the wave's own cost:** a resumed agent's reported token figure appears to be **cumulative for that agent, not incremental for the attempt**. Summing every report double-counts. Take each agent's final figure; the rework is the delta. Both prior waves land at about 7% rework on that reading.
