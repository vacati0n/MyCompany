# MC-6: Wave 5: the sustained-rate capability, and the measurement that would make the rate decidable

- Source: local://MASTER-PLAN.md#35-development-waves
- Issue type: Story   Priority: High
- Labels: implement, wave-5, throughput, instrumentation
- Routed as: /implement (feature-request)

## Description

WAVE 5 — BUILD THE SUSTAINED-RATE CAPABILITY; THE RATE ITSELF IS NOT PROVABLE YET

Waves 0 (`run-258e0a3415d2`), 1 (`run-dd80173faaad`), 2 (`run-3a58551ee912`), 3 (`run-ad369fe67ded`) and 4 (`run-5d5e6bd74c36`) are all Completed. Wave 5 builds on all five and must not re-create any of them. The source tree carries 89 files under `src/` (82 C# sources and 7 project files) and 27 under `tests/` (22 and 5); **489 tests pass against a live PostgreSQL 17** (450 pass and 39 skip with a recorded reason without one), and the architecture suite is at 46 and now executes on every solution build.

### THE TENSION IN THIS WAVE, STATED BEFORE ANYTHING ELSE

MASTER-PLAN section 35 specifies Wave 5 as **three videos per week — prove the sustained rate for one channel**, with the note that *the decision to sustain follows the approval-minutes baseline rather than preceding it*.

**That proof is unreachable under the constraints now in force, and the reason is not a scheduling problem.** Proving a sustained rate requires producing and publishing items. Producing an item requires commissioned narration audio and downloaded licensed clips; publishing requires a channel in the programme. **Both are spend, and publication, account creation and channel creation are reserved to the owner and undischarged.** Wave 4 closed having committed USD 0.00, as did Waves 2 and 3.

**This ticket therefore scopes the capability and not the proof**, on the same discipline that made the last two waves work: Wave 3 built the entire publishing capability and published nothing; Wave 4 built analytics for a channel with no audience and shipped the revenue-derived displays dark. **Do not scope a wave whose own constraints foreclose its stated obligation.** If the scope phase judges that even the capability cannot be bounded without owner action, **raise it as a blocking question rather than absorbing it** — the Wave 3 scope agent did exactly that on an unsatisfiable obligation and was right.

### GOAL

Build what a sustained three-items-per-week rate would require of the system, and instrument what would make the rate **decidable on observation rather than on assumption** when the owner discharges the blockers.

### WHAT IS MEASURABLE AND BUILDABLE NOW, WITHOUT SPEND

1. **The reasoning-tier column the routes table does not have.** This is the highest-value single change carried out of Wave 4 and it costs nothing. The capability boundary already writes the served tier from the admitted route, and the operation record already carries both fields with an explicit absence marker. **But the `routes` table declares no reasoning-tier column and the registry constructs every route without one**, so on the fully composed production path **every recorded served tier is the absence marker, whatever route serves it.** The mechanism is proven and the production path cannot feed it. Closing that gap is what would finally make the assumed reasoning-tier split testable by a production series rather than by one demonstration.

2. **The four record sources that have no home.** Wave 4's review recorded at medium severity that the item dossier, production-path stage outcomes, supply audits and compliance determinations have **no table, no read port, no adapter and no production caller**, so their analytics composers are unreachable from the composed system and are exercised only against supplied records. **This is the architect's decision and it travels at medium**: the Wave 4 reviewer expressly refused to lower it on the orchestrator's decision to defer, on the ground that a scheduling decision is not evidence and that deferral by an orchestrator is not risk acceptance by the role that owns it. That reasoning was accepted and stands.

3. **The production caller that drives the publishing sequence end to end**, carried unresolved from Wave 3's release note.

4. **Throughput, buffer depth and queue behaviour** as capability and instrumentation: what the system would have to hold, claim and retry to sustain three items a week, measured on its own records.

5. **The approval-effort defect Wave 4 raised and nothing tracks**: the approval-effort composer reports all three components unmeasured when only one mark is missing.

### WHAT IS NOT MEASURABLE NOW, AND MUST NOT BE SIMULATED

Anything requiring a produced item, a published item, an audience or a revenue observation. **The same rule that governed the last two waves governs here: a zero meaning "none occurred" and a zero meaning "never measured" are different values and must not render identically.** Wave 4 made that structural — a closed three-case union in which the unmeasured case carries no value field of any kind, so a null-coalescing default is not expressible. **Extend that discipline; do not weaken it, and do not add a quantity outside it.**

### BINDING CONSTRAINTS CARRIED FORWARD

1. **Nothing publishes.** No upload, no channel creation, no account creation, no purchase, no subscription session, no render. **Five independent structural absences** leave the upload path unable to fire; **none may become a configuration value and none may be weakened.**
2. **The three coded preconditions are encoded and NOT discharged**: library registration not done, payment account absent, two-step verification unconfirmed. Each refuses from recorded state, and *unknown* folds to not-satisfied while staying visible in the refusal. Only the owner can discharge them.
3. **Budget USD 77.41 per month**, unchanged. **The authorised single-metered-operation exception is still unspent and still available** — Wave 4 met its obligation at zero cost, because the served tier proved to be a property of the admitted route rather than of a provider response. The second stock library is not authorised. Re-fetch every unit price immediately before it is applied.
4. **Code first, AI when necessary.** Deterministic work stays structurally incapable of a model call. The boundary assertions now run on every **solution** build and fail it; note two recorded limits — a reference **declared and never used** still passes, because the assertions read retained references rather than the project file, and a **single-project** build does not start the target.
5. **Owner approval remains a state in the gate transition table and absent from configuration.** Nothing may become a route around it.
6. **No clip count may be invented, ever.** Zero were obtained across 14 audited subjects.
7. **No clean-record threshold** may be derived from the single 1 min 58 s approval measurement — a lower bound of the weakest class, taken on a held item by an owner who already knew its contents, with no queue time and no rework. The threshold stays with the owner until a series exists that includes at least one approval which returned a change request.
8. **Niche-agnostic and re-pointable.** The subject is a recorded experiment, not the final business bet.

### CARRIED IN, NOT RESOLVED

- **20 known issues from the Wave 4 release note**, all open, 9 carrying open questions routed to `architect` (4), `omn-tech-lead` (3), `omn-product-owner` (1) and `omn-qa` (1).
- **15 of the 16 known issues from the Wave 3 release note.** One is answered: nothing is recorded at the gate transition the Wave 3 implementation declined to write, and the stage-outcome surface reports that step as unmeasured, naming the dispatch record and audit entry that do exist rather than inferring a gate state.
- **The assumed reasoning-tier split remains untested.** One record establishes that the mechanism records what it claims to record; it settles no ratio. The per-item variable cost and the monthly envelope both still rest on it. **Three waves have now closed with that true.**
- **Supply is unestablished** for both the species and the behaviour.
- **The approval-minutes series does not exist.**
- **The repository declares no version anywhere.** The Wave 4 release record derived `1.1.0` and published the derivation as a known issue rather than asserting it; that question is routed to `omn-tech-lead`.

### FRAMEWORK DEFECTS TO BRIEF EVERY SUBAGENT ON

Measured in this programme, not theoretical. `research/framework-defects.md` carries all ten in full.

1. **`declared_side_effects` takes bare written file paths only.** Prose there is misclassified as an undeclared write and **has permanently halted a run**. For a reviewer: list only the files you wrote, **never the paths of the change under review**.
2. **No foreign identifier tokens in framework artifacts.** An identifier from outside the run fails two mutually exclusive ways and no token form passes both. **Source-qualified prefixes do not help** — the scanners use a word-boundary pattern and a hyphen is a word boundary. Cite descriptively.
3. **Write at least three acceptance criteria per task.** The field parser swallows the first nested bullet, so one parses as none and two parse as one. Note this applies to **nested-bullet** fields, not to table-row criteria.
4. **Do not sign a decision record.** Signing is the gate's act.
5. **Do not express a requirement trace as a range.** Enumerate.
6. **The string "claude" must not appear in any framework artifact**, including file paths and branch names.
7. **A version field must be a recognisable version string**, not a ticket label.
8. **Read the validator source rather than guessing** when a check names only its first offender. Every phase of Wave 4 ran its validator read-only over its own draft before handing back, and every one passed first time.
9. **A gate decision's rationale reaches no downstream phase.** It is written only to the run ledger, the event log and the state store, none of which a dispatch prompt names. **An orchestrator answering a blocking question at a gate must restate the answer in the next phase's dispatch briefing**, and should expect a careful agent to treat it as an assumption rather than as established.
10. **`omn-qa` owns no phase in `implement-feature`**, so a task assigned to it maps to no executing phase and is **never dispatched**. This is the shape of the failure that left the carve-out unspent in Wave 3. It is the opposite edge of the withdrawn claim that the Verification Gate is undecidable: `omn-qa` is eligible to **decide** that gate precisely because it produces none of the evidence the gate assesses. **The Verification Gate has now been decided normally on four runs by three sessions.**

Also recorded and lower-severity: the documentation output contract permits four fenced code blocks while its validator caps them at one; and the release-note `withheld` verdict is unreachable.

### PRICING BASIS

Meter against option O-002 — 377,000 input and 33,000 output tokens, USD 2.647440 per item variable, USD 42.99 per month standing, USD 5.95 per item all-in at 13 items per month. **Every figure derived from the assumed reasoning-tier split is an assumption, not a measurement, and must be labelled as such.** The USD 1.58 figure is superseded and comparing against it manufactures a false overrun. Development cost and per-item cost are separate quantities and must not be netted.

**Measuring the wave's own cost.** A resumed agent's reported token figure is **cumulative for that agent, not incremental for the attempt**. This was an inference in Wave 3 and is **confirmed as of Wave 4**, by two independent meters on three resumed agents agreeing to within one percent. Take each agent's final figure; the rework is the delta.
