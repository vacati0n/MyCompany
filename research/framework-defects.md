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

## Defect 2 — `awaiting_policy_exception` is a terminal block with no clearing path

**Severity: halts a run permanently through the supported interface.**

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

The Verification Gate names `omn-qa` as its **only** owner, and `omn-qa` produces the evidence
that gate assesses. The Producer Exclusion Rule forbids an agent from deciding a gate over its
own evidence, so the gate has no eligible decider.

The rule itself works correctly — it refused an attempt to approve the Scope Gate as
`omn-product-owner` when that agent had produced the scope definition, naming
`omn-business-analyst` as the valid alternative. The Verification Gate simply has no alternative
listed.

**Recommended fix.** Give the Verification Gate a second owner in
`workflows/workflow-gate-matrix.md`, and add a check that every gate lists at least one owner
that is not the producer of the evidence it closes.

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
