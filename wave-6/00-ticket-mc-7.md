WAVE 6 — BUILD THE MULTI-CHANNEL CAPABILITY; THE SECOND CHANNEL ITSELF IS NOT AUTHORISED

Waves 0 (`run-258e0a3415d2`), 1 (`run-dd80173faaad`), 2 (`run-3a58551ee912`), 3 (`run-ad369fe67ded`), 4 (`run-5d5e6bd74c36`) and 5 (`run-423e990b743d`) are all Completed. Wave 6 builds on all six and must not re-create any of them. The source tree carries 91 C# sources and 7 project files under `src/` and 30 C# sources and 5 project files under `tests/` at the Wave 5 head; confirm by counting, because every previous ticket's counts were stale by the time they were read. **573 tests pass against a live PostgreSQL 17** in a separate demonstration database (494 pass and 73 skip with a recorded reason without one), and the architecture suite is at 47 and executes on every solution build.

### THE TENSION IN THIS WAVE, STATED BEFORE ANYTHING ELSE

MASTER-PLAN section 35 specifies Wave 6 as **multi-channel: channels 1, 2, 3 using shared agents**, with two conditions that bind regardless of wave: **re-examination of the CEO approval workload precedes any second channel**, and **channel isolation is a corporate-structure question (section 17.5) that must be settled before, not during, this wave**.

**Neither condition can be discharged by this wave.** The approval-workload re-examination needs the approval-minutes series, which does not exist: one measurement of 1 min 58 s exists, taken on a held item by an owner who already knew its contents, and the owner's standing decision reserves the clean-record threshold until a series exists that includes at least one change request. Channel isolation is the owner's corporate decision and no run artifact records it. And channel 1 has never published: five waves, nothing uploaded, nothing bought, USD 0.00.

**This ticket therefore scopes the capability and not the second channel**, on the discipline that made Waves 3, 4 and 5 work: build what a shared workforce serving several channels would require of the system — per-channel configuration, per-channel brand and voice, per-channel budget tracking, per-channel analytics partitioning, per-channel approval queues — and instrument what would make the approval-workload re-examination decidable on observation. **No second channel is created, configured as live, or routed to.** If the scope phase judges that even the capability cannot be bounded without owner action, **raise it as a blocking question rather than absorbing it**; the Wave 3 and Wave 5 scope agents did exactly that and were right both times.

### GOAL

Make the company's per-channel configuration real and re-pointable, so that a second channel is a recorded owner decision plus configuration and nothing else, while every quantity the re-examination needs is measurable on the company's own records and labelled by its measurement case.

### WHAT IS MEASURABLE AND BUILDABLE NOW, WITHOUT SPEND

1. **Channel as the partition key of every analytics reading.** Wave 4 and Wave 5 readings are channel-agnostic or single-channel by construction. Every reading must partition by channel without a quantity leaving the closed three-case measurement union.
2. **Per-channel budget tracking** at the 50/75/90/100 percent thresholds (section 27.6), reading cost from the operation record, each threshold an observed, observed-zero or unmeasured value, never a configured number presented as a measurement.
3. **The month-period finality rule over the operation record.** Wave 5 gave the audit record a record horizon so an observed period is final by construction. The operation record — the cost readings and the tier ratio — has no such rule and is carried as an accepted risk. Extend the horizon or state the limit structurally; the architect owns it.
4. **The recorder's production caller.** The four record sources are written only by the recorder and the recorder has no production caller because no stage handler exists. Deciding where the first stage handler lives is the architect's.
5. **The two Wave 5 low findings**: the entry point's unclaimed-rest path has no check; a stage row carries instants from two clocks and the deferred cycle-time measure must state before using such rows.
6. **The audit-chain fork**, latent since before Wave 5: two appenders in flight can read the same head hash. Routed to architect.
7. **The gate service's transition from Draft that the table does not allow, and the absence of any writer of the awaiting-rights-check state.** No delivered path brings a new item to awaiting owner approval; the Wave 5 entry point reaches composition only over a fixture store. Closing this is what makes the entry point exercisable on real recorded state.
8. **A declared version.** The repository declares none; two release records have derived one and published the derivation as a known issue. Routed to omn-tech-lead; one line in the build properties closes it.

### WHAT IS NOT MEASURABLE NOW, AND MUST NOT BE SIMULATED

Anything requiring a produced item, a published item, an audience, a revenue observation, a second channel, or an approval-minutes series. **The same rule governs: a zero meaning none occurred and a zero meaning never measured are different values and must not render identically.** The closed three-case union, the build-time membership assertion that names the offending type, the table checks admitting exactly the permitted row shapes, and the record horizon are all structural. **Extend them; do not weaken them, and do not add a quantity outside them.**

### BINDING CONSTRAINTS CARRIED FORWARD

1. **Nothing publishes.** No upload, no channel creation, no account creation, no purchase, no subscription session, no render. Five independent structural absences leave the upload path unable to fire; none may become a configuration value and none may be weakened.
2. **The three coded preconditions are encoded and NOT discharged**: library registration not done, payment account absent, two-step verification unconfirmed. Each refuses from recorded state; unknown folds to not-satisfied while staying visible in the refusal. Satisfied rows exist only as fixtures in demonstration stores that drop themselves.
3. **Budget USD 77.41 per month**, unchanged. **The authorised single-metered-operation exception is still unspent and still available.** The second stock library is not authorised. Re-fetch every unit price immediately before it is applied.
4. **Code first, AI when necessary.** Deterministic work stays structurally incapable of a model call; the boundary assertions run on every solution build. Three recorded limits: a reference declared and never used still passes; a single-project build does not start the target; a named property bypasses it.
5. **Owner approval remains a state in the gate transition table and absent from configuration.** A per-channel approval queue is a view over that state, never a route around it.
6. **No clip count may be invented, ever.** Zero were obtained across 14 audited subjects.
7. **No clean-record threshold and no approval-workload figure** may be derived from the single measurement. Both stay with the owner until a series exists.
8. **No sizing claim and no sustainable-rate claim.** Wave 5 delivered the rate as testable, not tested, under the orchestrator's reading of the owner's decisions; that reading is open to the owner's reversal and until then binds.
9. **Niche-agnostic and re-pointable.** The subject is a recorded experiment, not the final business bet. Per-channel configuration is the mechanism of re-pointing.

### CARRIED IN, NOT RESOLVED

- **38 known issues from the Wave 5 release record**, all open, including the two new low findings, the accepted month-period finality risk, the audit-chain fork, and the version question.
- **14 of the 20 Wave 4 known issues** still open (4 resolved, 2 narrowed), and **15 of the 16 Wave 3 known issues**.
- **The assumed reasoning-tier split remains untested.** The routes table now carries a stated tier and the composed path records it; **the split is testable by a production series and no series exists.** Fourth consecutive wave with that true.
- **Supply is unestablished** for both the species and the behaviour.
- **The approval-minutes series does not exist.**
- **Owed by the owner and blocking first publication, none discharged**: library registration on every music and stock library; the payment account under the settled payee position; two-step verification on the channel; a library login session to obtain real clip counts.

### FRAMEWORK DEFECTS TO BRIEF EVERY SUBAGENT ON

Measured in this programme, not theoretical. `research/framework-defects.md` carries all eleven in full.

1. **`declared_side_effects` takes bare written file paths only.** Prose there has permanently halted a run. For a reviewer: list only the files you wrote, never the paths of the change under review.
2. **No foreign identifier tokens in framework artifacts.** Source-qualified prefixes do not help: a hyphen is a word boundary. Cite descriptively. Note that the release-note validator machine-checks only the `K-` and vendor tokens; the rest of the rule is convention the other validators enforce.
3. **At least three acceptance criteria per task** in nested-bullet fields; the parser swallows the first nested bullet. Not applicable to table-row criteria.
4. **Do not sign a decision record.**
5. **Do not express a requirement trace as a range.**
6. **The lowercase name of the session-tooling vendor — the word that names every session worktree directory — must not appear in any framework artifact**, including file paths and branch names, which is how it gets in.
7. **A version field must be a version string.**
8. **Read the validator source rather than guessing**, and run it read-only over the draft before handing back. Every phase of Waves 4 and 5 passed first time on that habit: twelve of twelve.
9. **A gate decision's rationale reaches no downstream phase.** Restate every gate answer in the next phase's briefing, and expect a careful agent to treat it as an assumption.
10. **`omn-qa` owns no phase in `implement-feature`**; a task assigned to it is never dispatched. It decides the Review and Verification Gates, which have now been decided normally on five runs.
11. **A stale `failure-envelope.json` of class gate-approval-required is left in the next phase's folder** when a phase is completed before its gate is decided, and is never cleared. Tell every agent to ignore it once its dispatch prompt exists.

Also recorded and lower-severity: the documentation output contract permits four fenced blocks while its validator caps them at one; the release-note withheld verdict is unreachable; the editor tool refuses both the base-repository path and the feature-worktree path from a session worktree, so framework and code files alike are authored in the scratchpad and copied in; a quoted-delimiter heredoc breaks whenever its body contains a quoted delimiter.

### PRICING BASIS

Meter against option O-002 — 377,000 input and 33,000 output tokens, USD 2.647440 per item variable, USD 42.99 per month standing, USD 5.95 per item all-in at 13 items per month. **Every figure derived from the assumed reasoning-tier split is an assumption, not a measurement, and must be labelled as such.** The USD 1.58 figure is superseded. Development cost and per-item cost are separate quantities and must not be netted.

**Measuring the wave's own cost.** A resumed agent's reported token figure is **cumulative for that agent, not incremental for the attempt**; take each agent's final figure, the rework is the delta, and read the rework share against the validator-rejection count rather than alone. Wave 5 closed at 2,835,446 tokens with 13.8 percent rework and zero validator rejections; roughly two percent of that was infrastructure waste from an organisation spend limit and a hung container runtime, not agent or framework cost.
