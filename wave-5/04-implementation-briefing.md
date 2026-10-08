# Phase 4 implementation briefing — dispatch verbatim to a general-purpose session subagent

You are acting as the registered agent `omn-dev-1-implement` for phase 4 (`implementation`) of run `run-423e990b743d`, ticket MC-6, in the omn-agent framework.

# Your instructions

Read and follow, in full and as the authority over everything below:
`D:/Project/MyCompany/.omn-agent/runs/run-423e990b743d/states/implementation/dispatch-prompt.md`

Write:
- artifact: `D:/Project/MyCompany/.omn-agent/runs/run-423e990b743d/states/implementation/artifacts/implementation-report.md`
- result envelope: `.../result-envelope.json` in the same phase directory
- **code and tests into the feature worktree**: `D:/Project/MyCompany/.worktrees/mc-6-feature` (branch `feature/mc-6-wave-5-the-sustained-rate-capability`). Commit as you go on that branch with clear messages. Do not touch any other worktree or branch. Commit messages must not contain the string `claude`.

Any `.claude/` path under `.omn-agent/` in framework config is a STALE install-root reference. Read the real file under `D:/Project/MyCompany/.omn-agent/`.

The `failure-envelope.json` in the implementation phase folder is stale (written before the Design Gate was decided; the gate is approved). Ignore it.

# Paths

The base checkout at `D:/Project/MyCompany` is on a branch carrying only a prompt file. **The repository content lives in the feature worktree** above. Cite that path, never any path containing the word `claude`.

Upstream artifacts (all approved, all passed validation first time):
- `D:/Project/MyCompany/.omn-agent/runs/run-423e990b743d/states/solution-design-and-risk-assessment/artifacts/technical-design.md` (78/78) plus four ADRs in the same folder — **your primary input; build exactly this design, under selected option O-002.**
- `.../execution-planning/artifacts/execution-plan.md` (45/45) — your tasks are T-007 to T-016 (build) and you also do verification-during-implementation as the plan assigns it to you.
- `.../scope-and-acceptance/artifacts/scope-definition.md` (33/33) — five in-scope items, fifteen criteria.
- `D:/Project/MyCompany/.omn-agent/tasks/MC-6/input.md` — the ticket. **Its file counts are stale** (the tree is now 87 C# + 7 project files under src, 25 + 5 under tests — confirm by counting).
- `D:/Project/MyCompany/.worktrees/mc-6-feature/research/ceo-decision-record.md` — AUTHORITATIVE where anything conflicts; Wave 5 block at the end.
- `D:/Project/MyCompany/.worktrees/mc-6-feature/research/framework-defects.md` — all ten findings.
- `D:/Project/MyCompany/.worktrees/mc-6-feature/wave-4/release-note.md` — twenty open known issues.
- `D:/Project/MyCompany/.worktrees/mc-6-feature/db/README.md` — record store. **A PostgreSQL 17 container `mediacompany-pg` should be running on port 55432** with the connection string the README gives (start it with the README's docker command if not). Run the full suite with `MEDIACOMPANY_TEST_CONNECTION_STRING` set so the datastore demonstrations execute rather than skip. Wave 4 closed at 489 passing; do not regress any.
- Precedent: the Wave 4 implementation report at `D:/Project/MyCompany/.omn-agent/runs/run-5d5e6bd74c36/states/implementation/artifacts/implementation-report.md` passed 32/32 first time. Match its shape.

# Gate rulings that reach you only through this briefing (a gate rationale reaches no downstream phase). Treat them as assumptions and say so where they bear on a decision.

**Scope Gate — sustained-rate evidence:** any sizing claim, buffer depth, concurrency figure or sustainable-rate assertion waits until a production series exists. A queue demonstration is a capability demonstration only; its depth and every count read from a seeded demonstration store is a demonstration parameter labelled as an assumption. No threshold and no claim about the rate may be derived from it. The wave delivers the rate as testable, not tested.

**Planning Gate — the publishing entry point demonstration:** acceptable only on these conditions — the store is throwaway and dropped by the demonstration itself; the three preconditions (library registration, payment account, two-step verification) are recorded as satisfied only as fixture rows written by that demonstration with the evidence fields the existing schema requires; nothing in production code, configuration, seed data or migration records any precondition as satisfied; the owner verdict is recorded through the existing gate service as the state in the transition table, never through configuration; the terminal position stays terminal with all five structural absences unchanged so nothing fires.

**Design Gate — rulings on the architect's objections, all accepted as correct:**
- The dossier recorder (`ItemDossierRecorder` and the dossier writer on the unit of work) is in scope, because nothing in production wrote any of the four sources.
- The review-mark case of the approval-effort criterion ("each of the three marks missing in turn") is shown as a **refused construction**, because review marks are mandatory in both the type and the table.
- A refused dispatch writes an attempt row and an audit entry, no dispatch record, and does not advance the job.
- The demonstration item for the entry point **carries no asset rows**, so the rights check passes without a per-asset registration fixture; only the three named precondition fixtures exist.
- The composition stage row left Pending by the dispatch path, and the missing `job.completed` entry, are fixed per the design (optional terminal-stage closure plus completion entry in the same transaction).
- The gate service's transition from Draft that the table does not allow, and the absence of any writer of the awaiting-rights-check state, are **out of scope** — carried as a known issue, and nothing you build may depend on that transition.

# Constraints you must hold — these are the programme's hard lines

1. **Nothing publishes.** No upload, channel creation, account creation, purchase, subscription session or render. The five structural absences on the upload path are unchanged; none becomes a configuration value.
2. **Preconditions encoded, undischarged** in production. Unknown folds to not-satisfied and stays visible in the refusal.
3. **No spend.** No provider account, endpoint, credential or network call. The tier column is fed from recorded route data; a non-provider substitute route carries a stated tier exactly as a provider route does, which is how the tier reaches a recorded operation end to end at zero cost. **The authorised exception stays unspent.**
4. **The closed three-case measurement union is extended, never weakened.** Every new quantity takes one of the three cases; the unmeasured case has no value field; the build-time membership assertion covers every new type by name. **The round trip is the property:** every persisted quantity's row encodes its case with a table check admitting exactly the permitted shapes. Wave 4's implementer reintroduced its defect, watched the checks fail, removed it and watched them pass — do the same for at least the tier column and one throughput quantity, and record it.
5. **The reasoning-tier split's numeric values appear in no supplied input.** Do not write them anywhere. The tier ratio is the agreement share as the design defines it, with the definition travelling as a required label.
6. **Code first, AI when necessary.** Deterministic work stays structurally incapable of a model call; the boundary assertions run on every solution build. Confirm the build-time target still fires after your change and that a solution build fails on a violation.
7. **Owner approval stays a state in the transition table, absent from configuration.**
8. **No clip count invented; no clean-record threshold derived.**
9. Record deviations honestly in the report. If a design decision cannot be implemented as written, say so and say what you did instead — do not silently substitute.

# Framework defects — measured, not theoretical

1. **`declared_side_effects` takes bare written file paths only.** List every file you wrote or changed, as bare paths, one per entry, nothing else in that field. This includes code and test files.
2. **No foreign identifier tokens in the report.** Cite the design's, the plan's, the scope's, the release note's and the decision record's identifiers descriptively in prose. Read `implementation_report_validator.py` to learn which tokens it admits (its own) and rejects. Wave 4's report solved this.
3. At least three items in any nested-bullet field the parser counts.
4. Never sign a decision record.
5. **Never express a requirement or test trace as a range.** Enumerate.
6. **The string `claude` must appear nowhere in the artifact**, including paths, nor in commit messages.
7. A version field takes a version string.
8. **READ THE VALIDATOR SOURCE** — `D:/Project/MyCompany/.omn-agent/runtime/implementation_report_validator.py` — and **run it read-only over your draft before handing back.** Every Wave 4 phase did this and passed first time.
9. A gate rationale reaches no downstream phase (handled above).
10. `omn-qa` owns no phase in this workflow.

# Writing framework files

**Write framework artifacts by shell redirection.** The editor tool refuses the base-repository path from a worktree, and a quoted-delimiter heredoc breaks on shell quoting whenever the body itself contains a quoted delimiter. **Author into your scratchpad directory and copy the file in.** Code files in the feature worktree can be written with the normal editor tools.

# The habit that matters most

**Every recorded agent objection in this programme has been correct, across four waves, without exception.** If you think an instruction above is wrong — including one of mine, including a gate ruling, including the design — **say so explicitly in your report rather than complying silently.** I will check it rather than defend it.

# Report back

When done, report: the artifact path; whether your read-only validator run passed and the count; the commits you made on the feature branch; the test totals before and after (pass/skip/fail against the live store, and the architecture-suite count); the reintroduce-and-watch-fail evidence; every deviation from the design; any objection; and your final total token figure for this agent.
