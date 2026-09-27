# omn-agent framework defects found while running MC-1 and MC-2

Found 2026-09-18 and 2026-09-26 driving two runs through the installed framework
(omn-agent 0.1.0, runtime `framework_runtime.py` v0.8.0, installed at `.omn-agent/`).
All belong upstream in the framework payload at `D:/Project/claude-framework`, not in this
repository. Three local bridges are in place here and **should be reverted once the upstream
fix ships**.

A background task was opened for the first of these: *"Fix omn-agent investigate workflow
input-contract drift"*. Defects 2 to 4 were found after it was opened and are not in its brief.

---

## Defect 1 — Producer/consumer input-identifier drift (three instances, both workflows)

**Severity: blocks every run of both workflows at the affected phase.**

**Status on 2026-09-26: STILL OPEN.** Re-checked against the framework payload at
`D:/Project/claude-framework/omn_agent/_bundled_payload/`. Neither `omn-context-agent` nor
`omn-tech-lead` declares the producer identifiers in `inputs.accepted` there, so the upstream
fix has not landed for this defect even though Defect 2 has. **All three local bridges remain
necessary and must stay in place for now.**

`upstream_inputs()` (`runtime/framework_runtime.py`, ~line 1587) offers an upstream artifact
under **the producing agent's own output identifier**. Where the consumer's manifest does not
declare that identifier in `inputs.accepted` or `inputs.optional`, the phase blocks at
`G5-INPUT` with `no accepted input type supplied` — even though the workflow's own Phase Model
declares exactly that handoff.

| # | Workflow | Handoff | Producer emits | Consumer accepted | Result |
|---|---|---|---|---|---|
| 1a | investigate | `problem-framing` → `technical-discovery` | `requirement-framing` | `framed-objective`, `research-brief`, `investigation-question`, `research-question` | blocked |
| 1b | investigate | `option-analysis` → `recommendation` (same agent both sides) | `technical-recommendation` | `investigation-report`, `review-package`, `validation-report`, `technical-design` | blocked |
| 1c | investigate | `recommendation` → `publication` | `technical-recommendation` | `implementation-report`, `validation-report`, `review-package`, `deployment-status` | blocked |

Note 1b: a **same-agent phase-to-phase refinement** fails, because `omn-tech-lead` does not
accept its own output as an input.

Note that `omn-context-agent`'s own `minimumSatisfaction` prose says *"a framed objective for
the `technical-discovery` phase of investigate"* — so `requirement-framing` **is** the intended
framed objective. Only the identifier was never reconciled.

**The `--input TYPE=PATH` workaround does not apply to an existing run:** run identity is
derived from the supplied input set, so supplying one is refused with
`--run-id ... does not match the identity derived from the supplied inputs`.

**Local bridges applied here** (each commented `LOCAL BRIDGE (2026-09-18)` in place; originals
backed up to the session scratchpad):

- `.omn-agent/agents/omn-context-agent/manifest.yaml` — accepts `requirement-framing`
- `.omn-agent/agents/omn-tech-lead/manifest.yaml` — accepts `technical-recommendation`
- `.omn-agent/agents/omn-documentation/manifest.yaml` — accepts `technical-recommendation`

**Recommended fix.** Adding identifiers one at a time will drift again. Prefer either making
`upstream_inputs()` offer the artifact under the **phase input role named by the workflow**
rather than the producer's output identifier, or introducing an explicit alias between artifact
identifiers and phase input roles so the Phase Model is the single authority for handoffs.
Then audit **every** workflow the same way — a script over the manifests and the Phase Model
tables would catch all of it at once — and add a regression check to
`runtime/verify_registry_coverage.py`, which already does per-phase verdicts at check `C6`.

---

## Defect 2 — `awaiting_policy_exception` had no clearing path — **FIXED UPSTREAM 2026-09-26**

**Status: resolved.** Verified in the installed runtime on 2026-09-26. A `policy-exception`
subcommand now exists in `runtime/framework_runtime.py` (`cmd_policy_exception`, line 4736;
argparse registration line 5931) and is exposed by the CLI as
`omn-agent run <KEY> --policy-exception --phase <phase> --owner-role <role> --decided-by <who>
--rationale "<text>"`. `BLOCK_REASON_GUIDANCE` line 1879 now reads *"record a policy exception
decision with the owning role, using `policy-exception`"* — the old "No runtime command clears
this" wording is gone. It shipped broadly as recommended below, including recording the
decision as first-class evidence at `runs/<run>/states/<phase>/policy-exception.json`, and it
refuses any block reason other than `awaiting_policy_exception`, so it is safe to attempt.
Note the runtime version string is still 0.8.0 — the fix landed without a version bump, which
makes it easy to miss. **Use the supported command; the manual state-engine transition below is
no longer necessary and should not be repeated.** Authored by the Wave 1 session in the framework
payload at `D:/Project/claude-framework` (runtime plus `cli.py` and `runner.py`), with two guards
both exercised: the role must be one the envelope lists, and an agent may not except itself from
its own write scope. Hygiene note: those four files were swept into commit `2e9e99e` "add new
version" alongside roughly 527 unrelated files, so the fix is hard to find in history. Reported by the Wave 2 session and verified
here independently.

The original finding is kept below for the record.

**Severity as originally found: halted a run permanently through the supported interface.**

`BLOCK_REASON_GUIDANCE` states: *"record a policy exception decision with the owning role. **No
runtime command clears this.**"* There is no `framework_runtime.py` subcommand for it.
`gate` applies to gates, not states; `rollback` requires `--gate` and a gate rejection;
`release` requires a lease. Re-dispatch **replays the stored failure** rather than re-evaluating
against corrected evidence, so repairing the underlying cause does not clear the block.

Worse, the block can be raised by a **false positive** (see Defect 3), so a run can be
permanently halted by a formatting mistake.

**Cleared here** by calling the state engine's own authorised `blocked → pending`
transition (`reason_code="enqueued"`, `actor_type="human"`) with the decision recorded in the
transition detail and a `policy_exception` field. `state_engine.TRANSITIONS` already declares
that pair with the trigger `blocker_cleared`; only the CLI surface is missing. Note
`blocker_cleared` is a *transition label*, not a member of `REASON_CODES`, which is a closed set.

**Recommended fix.** Add a `policy-exception` subcommand taking `--run-id`, `--phase`,
`--decided-by`, `--owner-role` and `--rationale`, applying that existing transition and
recording the decision as first-class evidence beside gate decisions.

### FIXED upstream, 2026-09-26 — `policy-exception` implemented

The recommended fix was implemented in the framework payload at `D:/Project/claude-framework`,
after the CEO chose it over applying another local bridge. **This defect is closed**, and the
local bridge is no longer needed for it.

| File | Change |
|---|---|
| `.claude/runtime/framework_runtime.py` | `cmd_policy_exception` + its `policy-exception` subparser; `clearing_action` now names the concrete command for `awaiting_policy_exception`; the static `CLEARING_ACTION` text no longer says "No runtime command clears this" |
| `omn_agent/_bundled_payload/runtime/framework_runtime.py` | same file, synced |
| `omn_agent/cli.py` | `--policy-exception` flag on `run` |
| `omn_agent/runner.py` | wires the flag to the runtime subcommand, with its own approval prompt and mode-exclusivity check |

It applies the transition the state engine already declared, `blocked -> pending` under
`blocker_cleared`, resolves the failure envelope, emits `escalation_resolved`, and writes
`states/<phase>/policy-exception.json` as first-class evidence carrying the owner role, the
decider, the rationale and the side effects the exception was granted over.

**Two guards, both exercised before first real use:**

- The decision must name a role the envelope lists, where it lists any.
- It may not be recorded by the agent whose own writes raised the block. Attempting
  `--owner-role omn-dev-2-reviewer` on a block raised by `omn-dev-2-reviewer` is refused:
  *"A write scope an agent can except itself from is advisory, which is what the scope is not."*
  This is the same principle the Producer Exclusion Rule states for gates.

A phase that is not blocked replays rather than erroring, and a phase blocked on a different
reason is refused with that reason's own clearing action quoted back.

**Used once, for real**, to clear the MC-2 phase-5 block described under defect 3's second
instance: recorded by `omn-orchestrator`, decided by the CEO, with the false-positive finding as
the rationale. The run then completed all six phases.

---

## Defect 3 — `declared_side_effects` classifies a prose note as an undeclared write

**Severity: false-positive policy failure; combined with Defect 2, halts the run.**

The runtime reads every element of `declared_side_effects` as a written path and compares it to
`permitted_writes`. `omn-context-agent` filed an honest disclosure sentence as a third element
alongside its two real paths. The sentence matched no permitted path, so it was classified as an
undeclared side effect: `failure_class: policy-failure`, `blocked_reason:
awaiting_policy_exception` — while the validation engine simultaneously recorded
*"0 blocking and 0 correctable failure(s)"* and the artifact passed 31/31.

Filesystem check confirmed only the two permitted paths were written. No policy was breached.

**SECOND INSTANCE, 2026-09-26 — and this one is not a formatting mistake.** Reported by the Wave 1
session. A reviewer that *honestly reports which source paths a correction cycle touched* still
triggers the block. `omn-dev-2-reviewer` raised four high findings, the change was corrected
inside the review cycle — which is exactly what the `resolved` finding status exists to record —
and the result envelope declared the corrected paths. Those were `omn-dev-1-implement` writes,
but the reviewer's permitted writes are its own two artifacts, so **eighteen paths it did not
write were classified as undeclared writes by it**. Validation had just passed 31/31.

This is a **design gap, not a misfiled note**: the framework has nowhere for a reviewer to record
that *the change under review moved while it was being reviewed*. The first instance could be
fixed by writing the note in the right field. This one cannot — the information is real, it is
the reviewer's to report, and no field exists for it.

**Recommended fix.** Validate `declared_side_effects` entries as paths at emission and reject a
non-path entry with a correctable error naming the right field, rather than silently
reclassifying it as an undeclared write at policy-decision time. Agent output contracts should
also name where a production-method disclosure belongs — `structured_output` worked here.

### Second instance, found 2026-09-26 driving MC-2 phase 5

Same halt, different cause, and this one is not a formatting mistake.

`omn-dev-2-reviewer` reviewed the Wave 1 change, raised four high findings, and the change was
corrected inside the review cycle — which is what a review cycle is for, and what the output
contract's `resolved` finding status exists to record (*"the change already closed it during this
review cycle"*).

The reviewer's result envelope then declared the corrected source paths in
`declared_side_effects`, as an honest account of what had moved. The reviewer's `permitted_writes`
are its two artifacts alone, correctly, because a reviewer may not write production code. So
eighteen paths the reviewer did **not** write were classified as undeclared writes *by the
reviewer*, and the phase halted at `awaiting_policy_exception` — while the Validation Engine had
just accepted the artifact at **31/31 checks**.

The field means "paths this invocation wrote". The corrected paths were `omn-dev-1-implement`
writes taken inside the review cycle. Moving them to
`structured_output.correction_cycle.corrected_paths` is the correct shape and was applied.

**What this exposes beyond defect 3.** The framework has nowhere for a reviewer to record that the
change under review moved while it was being reviewed. The `resolved` status says a finding
closed; nothing says which paths closed it, and the one field that looks right is the field that
halts the run. Either the output contract should name a field for it, or the result envelope
should carry a `related_changes` list that is recorded rather than policy-checked.

**How it was cleared.** No bridge this time. The host's permission layer refused the direct
state-engine call, correctly, because it is a write into governance state outside the supported
CLI — so the missing command was built instead. The CEO chose the upstream fix over another
bridge, `policy-exception` was implemented in the payload (see defect 2), and the block was
cleared through the supported surface with the decision recorded as evidence. The run then
completed all six phases.

**Still a defect.** The clearing path now exists, but the cause remains: a reviewer that honestly
reports which paths a correction cycle touched still triggers a policy block, and still needs a
human to say it was a false positive. The fix above makes that recoverable, not unnecessary.

---

## Defect 4 — WITHDRAWN. `implement-feature` Verification Gate is decidable after all

**Status: not a defect. Recorded as a prediction on 2026-09-26, disproved the same day by
actually deciding the gate. Kept for the record; see the correction at the end of this entry.**

The original entry follows as written.

**What this note originally claimed:** that `implement-feature`'s Verification Gate is
undecidable because `omn-qa` is its only owner while `omn-qa` produces the evidence it assesses.

**Why that is wrong.** `workflows/implement-feature.md` line 43 shows phase `quality-review` is
owned by **`omn-dev-2-reviewer`**, and its output artifact `review-package.md` closes **both**
the Review Gate and the Verification Gate. The specification says so directly at lines 131–134:
*"The Review Gate carries omn-qa as a second owner because omn-dev-2-reviewer produces the
findings that gate assesses, and the Producer Exclusion Rule forbids approving one's own
output."* So the producer of the Verification Gate's evidence is `omn-dev-2-reviewer`, not
`omn-qa`. `omn-qa` is therefore **eligible**, and sole ownership is not a contradiction.

**Where the defect was real — and it was already fixed.** In `fix-bug`, `omn-qa` owns
`regression-validation` and genuinely does produce the verification evidence. The amendment
table at the foot of `workflows/workflow-gate-matrix.md` records:
*"fix-bug | Verification Gate | added omn-dev-2-reviewer as second owner | omn-qa owns
`regression-validation` and produces the verification evidence"*. This note generalised that
`fix-bug` case to `implement-feature`, where ownership differs.

**Consequence for anyone reading this.** Do not treat the `implement-feature` Verification Gate
as blocked in advance. Attempt it as `omn-qa`: the runtime enforces the Producer Exclusion Rule
itself and names the valid alternative owner when it refuses, so an attempt is informative and
costs nothing. D-010's "known obstacle" paragraph in `research/ceo-decision-record.md`, and the
same warning carried into the Wave 1 and Wave 2 session briefs, overstate what is ahead.

**NOW PROVEN, 2026-09-26.** No longer a reading. The Wave 1 session reached the gate, decided it
as `omn-qa`, and **the runtime accepted**. The entry is fully withdrawn for `implement-feature`.
It was written from `fix-bug`'s shape, where `omn-qa` does own the producing phase, and applied
to a workflow where it does not.

**Superseded note:** this had been a reading of the workflow specification and the gate matrix,
not a runtime result. The Wave 2 session reaches phase 5 later and will have the runtime
adjudicate it. The remaining recommendation below stands on its own merits regardless.

**Recommended fix that still applies.** Add a check that every gate lists at least one owner that
is not the producer of the evidence it closes. That check would have caught the real `fix-bug`
instance mechanically, and would have prevented this note from being written.

### Correction, 2026-09-26: the premise looks wrong for `implement-feature`

This defect was recorded as a prediction, before the run reached phase 5. On reaching it, the
premise does not appear to hold.

The Producer Exclusion Rule binds the producer of the evidence a gate assesses. In
`implement-feature`, the phase that produces that evidence is `quality-review`, and the run's own
work-item table shows its owner is **`omn-dev-2-reviewer`**, not `omn-qa`. `omn-qa` owns no phase
in this workflow, so it produces nothing the Verification Gate assesses, and the rule does not
exclude it.

The gate matrix's own rationale column says as much, twice, and distinguishes the two cases:

| Row | Rationale recorded in the matrix |
|---|---|
| `implement-feature` Review Gate, second owner `omn-qa` added | *"omn-dev-2-reviewer owns `quality-review` and produces the findings log the gate assesses"* |
| `fix-bug` Verification Gate, second owner added | *"omn-qa owns `regression-validation` and produces the verification evidence"* |

In `fix-bug` the exclusion genuinely bites, because `omn-qa` owns the producing phase. In
`implement-feature` it does not. The single-owner Verification Gate row is therefore probably
fine as it stands, and the defect as written mistook `fix-bug`'s shape for `implement-feature`'s.

**CONFIRMED, 2026-09-26.** Once the policy block was cleared and the run moved, the Verification
Gate was decided as `omn-qa` and **the runtime accepted it**. The Producer Exclusion Rule did not
fire, because `omn-qa` produces nothing in this workflow. There is no defect here.

**This entry is therefore withdrawn as a defect** and kept as a record of the mistake: it was
written from `fix-bug`'s shape, where `omn-qa` does own the producing phase, and applied to
`implement-feature`, where it does not. A prediction recorded in the same register as three
observed failures reads like a fourth, and cost the next session a planned workaround for an
obstacle that was never there.

The recommended fix's second half still stands on its own merits — a check that every gate lists
an owner who is not the producer of its evidence would have settled this mechanically instead of
by reading, and would have caught the mistake at the time it was written rather than a session
later.

---

## Defect 5 — `plan_validator` field parser absorbs the first following bullet

**Severity: false-positive blocking failure on a conforming artifact.**

The task-field parser (`^\s*[-*]\s*<field>\s*:\s*(.*)$`, where `\s*` spans the newline) absorbs
the first bullet after a field line into that field's value. A task carrying **exactly one**
acceptance criterion therefore validates as carrying none, failing blocking checks `V4.1`
(`Q5.1`, all eight fields present) and `V4.8` (`Q5.6`, at least one acceptance criterion).

Observed on `execution-plan.md` for `T-010`, `T-011`, `T-026` — the only three tasks of 32 with
a single criterion. Worked around by giving each a genuinely distinct second criterion, which
improved the artifact but is not the fix.

**Recommended fix.** Anchor the field pattern to the line so it cannot span a newline, and add a
validator fixture with a single-bullet field.

---

## Not a defect — worth a design discussion

`omn-context-agent`'s `authorityScope.prohibitedPurposes` includes *"external network or service
access"*, and its charter is repository and product-state reconstruction. That makes
`/investigate` unable to serve any investigation whose evidence is **external** — market data,
vendor pricing, third-party platform policy, regulatory text. In this run the blocking question
was current platform policy, and the agent could only have recorded the whole question as a gap.

Worked around by gathering the evidence at host level and placing it in the repository as
citable source files, which the agent then read within its contract. That worked well and may be
the right pattern — but it is undocumented, and it leaves the provenance of those files outside
the frozen context slice (raised by the agent itself as an open question to `omn-orchestrator`).

The prohibition looks deliberate, so this should be resolved by decision rather than by quietly
widening the agent's authority: either document the supply-external-evidence-as-repository-input
pattern, or add a research-capable role.


---

## Defect 4 — CONFIRMED WITHDRAWN by a second independent run (Wave 2, `run-3a58551ee912`)

**Status: not a defect for `implement-feature`. Proven twice, by two sessions, on two runs.**

The Wave 2 session predicted this from the workflow specification on 2026-09-26 and owed an
empirical result. **That result is now in: the `implement-feature` Verification Gate was decided
as `omn-qa` on 2026-09-27 and the runtime accepted it.** No Producer Exclusion refusal, no
alternative owner named, no bridge applied.

That is the second independent confirmation — the Wave 1 session reached the same gate and
decided it the same way on its own run.

**Why the original claim was wrong**, restated so it is not re-raised a third time. The defect
held that the gate is undecidable because `omn-qa` is its only owner while producing the evidence
it assesses. The second half is false for this workflow:

- `workflows/implement-feature.md` line 43 — phase `quality-review` is owned by
  **`omn-dev-2-reviewer`**, whose Output Artifact `review-package.md` closes **both** the Review
  Gate and the Verification Gate.
- The same file's gate-ownership notes say it outright: *"The Review Gate carries omn-qa as a
  second owner because omn-dev-2-reviewer produces the findings that gate assesses, and the
  Producer Exclusion Rule forbids approving one's own output."*

So `omn-qa` is **not** the producer here and is eligible. The real instance is `fix-bug`, where
`omn-qa` does own `regression-validation` and genuinely produces the verification evidence — and
that instance was **already fixed**, by adding `omn-dev-2-reviewer` as a second owner
(`workflows/workflow-gate-matrix.md`, amendment table). The original note generalised the
`fix-bug` case to a workflow with different ownership.

**Consequence for the record.** Any brief, ticket or decision note warning that this gate blocks
a run is overstating what is ahead. Two runs have now passed through it normally.

**The Producer Exclusion Rule itself works correctly**, and Wave 2 saw it enforced at a level
below human attention: the design validator's check `D17.6` rejected a decision record the
producing agent had **signed**, because signing is the gate's act and not the producer's. The
rule is not only a gate-time check — it is embedded in artifact validation.


---

## Finding 6 — cross-document identifier citation is unsafe inside framework artifacts, in BOTH directions

**Severity: blocks a phase whenever an artifact cites an identifier from another document. Found
twice in two runs; the second time it failed the artifact two different ways at once.**

An artifact that cites an identifier belonging to another document fails validation, and there is
**no form of the citation that succeeds**:

| Case | Check | Why it fails |
|---|---|---|
| The foreign identifier has no local definition | `C6.2` — *"Every referenced identifier is defined in its declaring section"* | Resolution is against the artifact's **own** register. A foreign `D-201` is simply undefined. |
| The foreign identifier collides with a register another role owns | `SD7` — *"The artifact issues no task, change, or design identifier owned downstream"* | Citing the owner record's corrections `C-001`–`C-003` was read as the scope phase **issuing implementation change-set identifiers**, i.e. acting outside its authority. |

**Both checks scan the token. Qualifying the citation in prose does not help** — "owner decision
`D-201` of the business decision record" fails exactly as bare `D-201` does.

**The two failure modes are mutually exclusive as fixes.** Defining the foreign identifier locally
to satisfy `C6.2` would make the artifact *issue* it, which is what `SD7` forbids. There is no
token form that passes both.

### The only safe form is descriptive citation with no token

Refer to foreign material by **what it says**, never by its number: "the owner's decision that
approvals are exercised against the built surface on the held item" rather than `D-201`.
Provenance is preserved by the rationale stating who decided it, which is what a reader needs.

### ⚠ This refines the `branch : identifier : title` convention adopted as `C-004`

That convention — invented in the Wave 2 session and adopted as the general citation rule — **puts
the identifier token in the text.** It is correct for **repository documents**, which no validator
scans, and **wrong for framework artifacts**, which every validator does.

**The rule should be split by destination:**

- **Repository documents** (decision records, research, ledgers, reports): cite by identifier,
  qualified by document. Unambiguous and checkable by a human reader.
- **Framework artifacts** (anything under a run's `artifacts/`): **cite descriptively, with no
  foreign identifier token of any family.** The artifact's registers are its own and nothing else
  may appear in them.

### How this was found, which is the part worth keeping

The Wave 3 scope agent's **first** emission carried zero foreign `D-` identifiers and passed
**53/53**. The orchestrator then instructed it to cite `D-201` and `D-202` by token. The agent
complied but **recorded the deviation in its envelope's `C4` note rather than leaving it silent**,
stating that these were the only identifiers in the artifact not defined in its own sections. The
validator then failed it on exactly that, and additionally on the `C-00n` collision the
instruction had not anticipated.

**The agent was right, the instruction was wrong, and the disagreement was recoverable only
because the agent wrote it down instead of quietly obeying.** That is the second time in this
programme an agent's recorded objection has turned out to be the correct position.

**Recommended fix upstream.** Either give artifacts a declared `external_references` section that
`C6.2` and `SD7` both exempt, or have both checks resolve a citation's *namespace* before judging
it. Until then, descriptive citation is the working answer and should be stated in the agent
output contracts, because every agent will otherwise rediscover this the same way.


---

## Defect 5 — CORRECTED. The recorded remedy was itself insufficient.

**The original entry understated the defect and recommended a fix that does not fully work.**
Established 2026-09-27 by reading the parser source rather than re-applying the workaround.

**The mechanism, exactly.** The task-field pattern is
`^\s*[-*]\s*Acceptance Criteria\s*:\s*(.*)$`. The `\s*` after the colon is **greedy and spans
the newline and the following indent**, so `(.*)` swallows the **first nested bullet** into the
field's value.

**Therefore the arithmetic is one worse than recorded:**

| Written criteria | Surviving the parse | Result |
|---|---|---|
| 1 | **0** | Fails `V4.1` and `V4.8` |
| 2 | **1** | Passes, with **zero margin** |
| 3 | 2 | Passes with a real second criterion |

**The original note said the workaround was "giving each a genuinely distinct second criterion".
That yields ONE surviving criterion.** It clears the blocking checks — which is why it appeared to
work — but it leaves the task with a single assessable criterion, which is not what the author
intended and not what the gate reviewer sees.

**Observed live.** The Wave 3 planner's first draft had **seven tasks at two written criteria
each**, all of which would have parsed to one. It added a genuinely distinct **third** condition
to each — never padding; each a separately verifiable condition, for example a queue time of zero
recorded as zero rather than omitted, an attempt whose record cannot be written leaving no
attempt, and a step added without registration failing the build-time check rather than passing
silently.

**Also worth recording:** the Wave 2 execution plan passed at 70/70 with **20 of its 45 tasks at
exactly two written criteria**. Those twenty each carried one surviving criterion. Nothing failed,
and nobody would have known.

**Corrected guidance until the parser is fixed: write at least THREE acceptance criteria per
task**, so at least two survive. **Recommended fix unchanged and still right:** anchor the field
pattern so it cannot span a newline, and add a validator fixture with a single-bullet field —
plus, now, a fixture asserting that a two-bullet field yields two.
