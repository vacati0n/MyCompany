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

---

## Defect 4 — Verification Gate ownership — **PARTLY WITHDRAWN: misdiagnosed for `implement-feature`**

**Status: the `implement-feature` claim was wrong, and this note previously overstated it.**
Raised by the Wave 2 session on 2026-09-26 and verified here independently the same day.

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
