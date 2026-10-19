# Framework Runtime

Executable runtime for the AI Engineering Framework. This directory holds the **minimum
viable execution path** for Vertical Slices 1 to 3 and nothing more.

```
Slice 1   /implement -> implement-feature -> execution-planning -> planner   -> execution-plan.md
Slice 2   /implement -> implement-feature -> solution-design-... -> architect -> technical-design.md
Slice 3'  /implement -> implement-feature -> implementation      -> dev-1-implement
                                                                 -> implementation-report.md
Slice 4'  /implement -> implement-feature -> scope-and-acceptance -> product-owner
                                                                 -> scope-definition.md
Slice 3   one run, every phase: a persisted state machine, guarded transitions, and gates
Slice 4   the registered surface: 9 commands over 7 workflows, 36 routable phases
Slice 5   what happens when it goes wrong: 6 validated artifact types, classified failures,
          bounded retry, and a structured envelope for every blocked transition
```

Slices 1 and 2 proved that a registered agent can actually execute. Slice 3 makes a run a
**workflow** rather than an invocation: one run now carries one durable work item per
phase and per gate, moves them through a guarded state machine, hands artifacts from one
phase to the next, and refuses to duplicate work it has already committed.

Slice 4 widens what can be routed rather than what can execute. Every active command resolves
to an active workflow, every active workflow publishes a Phase Model the Task Router reads, and
every phase owner is host-invocable. Execution still reaches three phases, because a phase needs
an agent registry record and a registered validator before it can be dispatched; the other 33
block with a recorded reason. `verify_registry_coverage.py` reports both numbers, and
`reports/registry-coverage-report-2026-08-18.md` explains each blocker.

Slice 5 is about the paths a successful run never takes. Three things changed. Every artifact
type the framework's Phase Models name as a file now has a registered validator, so no phase
blocks for want of a way to judge what came back. Every failure is classified against the
Failure Classification Matrix before anything is done about it, and a retryable one is retried
under a bounded, exponential policy rather than waiting for an operator. And every blocked
transition emits a structured failure envelope that names the class, the retry position, the
decision required, and the command that clears it. `verify_validators.py` proves the coverage
and that each validator actually decides; `verify_recovery.py` induces the failures and proves
the recovery.

## Files

| File | Role |
|---|---|
| `framework_runtime.py` | Resolution chain, phase graph, transition guards, invocation gateway, run ledger, output aggregator |
| `state_engine.py` | Persisted work-item state machine: statuses, transition tables, idempotency keys, replay detection |
| `recovery_policy.py` | Recovery Controller: failure classification matrix, retry profile and backoff, failure envelopes, recovery ledger |
| `plan_validator.py` | Validation Engine for `execution-plan.md` against the Planner contract |
| `design_validator.py` | Validation Engine for `technical-design.md` against the Architect contract |
| `artifact_contract.py` | Declarative artifact contract engine, executing the contracts the four validators below declare as data |
| `bug_analysis_validator.py` | Validation Engine for `bug-analysis.md` |
| `investigation_report_validator.py` | Validation Engine for `investigation-report.md` |
| `release_note_validator.py` | Validation Engine for `release-note.md` |
| `review_package_validator.py` | Validation Engine for `review-package.md`, the one artifact type every review phase emits |
| `artifact_lib.py` | Shared artifact parsing used by every validator |
| `task_context.py` | Task Context: the compact, runtime-owned shared state a phase reads first (`runs/<run>/task-context.yaml`), the affected-area derivation, the skill-dispatch verdict, the upstream section map, and the parallel-group computation |
| `execution_metrics.py` | Execution metrics: the per-run performance trace (`runs/<run>/execution-metrics.json`) and the per-dispatch context estimate under the legacy and the progressive rules |
| `fixtures/` | Conforming reference artifact per type, for the validator proof. See `fixtures/README.md` |
| `verify_vertical_slice.py` | Executable proof that one phase really executed |
| `verify_multi_phase.py` | Executable proof that the run is a multi-phase state machine, and that re-running it changes nothing; the re-run (`M14`) executes in a disposable copy of the working tree, so the run it proves is never written |
| `verify_registry_coverage.py` | Coverage proof over the registered surface: command routing, Phase Model publication, phase-owner invocability, phase skills, gate decidability, and per-phase dispatchability with reasons |
| `verify_validators.py` | Coverage proof over the Validation Engine: every artifact a Phase Model names has a validator, every validator accepts a conforming artifact, and every validator rejects a mutated one by the named check |
| `verify_recovery.py` | Recovery proof by failure injection: a rejected artifact repaired on retry, a retry budget exhausted and bounded, a policy violation never retried, and a gate rejection rolled back under a recorded human authorisation and rebuilt with the rejected evidence kept immutable |
| `self_hosting.py` | Executable form of `config/self-hosting-profile.md`: scope classification, change-class routing, evidence expansion, release-checklist parsing, and per-command dispatchability |
| `change_proposal_validator.py` | Validation Engine for `framework-change-proposal.md`, the governance record of a framework-internal change. Re-reads the run the proposal names rather than trusting what it claims |
| `verify_self_hosting.py` | Proof that the self-hosting operating mode holds: the profile routes, the evidence requirement is enforceable, every recorded change was carried by a conforming run, and no run inside the window is unaccounted for; `--release-checklist` runs each command item in its own disposable copy of the working tree and proves by content digest of every file git lists (`S11`) that the working tree, and each copy, was left unchanged |
| `close_legacy_run.py` | Migration utility: closes a pre-slice-3 run directory, which carries no state store and so cannot be closed by `complete`. Delete once no such run remains |
| `demo/` | Demo mode (`--demo`, runtime 0.8.0). Telemetry: `context.py` the per-run switch, `recorder.py` the Recorder that turns events, envelopes, and host tool hooks into millisecond-stamped markers, `timeline.py` markers to scenes, `narration.py` the text-to-speech engine chain, `captions.py` the spoken lines as timed cues, subtitles, and a script. Filming the real application: `obs_ws.py` a standard-library obs-websocket client, `capture.py` window capture and the capture manifest, `capture_helper.py` the detached recorder process, `overlays.py` the presentation frame, `editor.py` the edit decision list and the ffmpeg cut. Drawing the run instead: `renderer.py`, `video_builder.py`, `deck.py`. `hooks.settings.json` is the optional host tool-hook wiring. See **Demo mode** below |

Requires Python 3.10+ and `pyyaml`. No other dependency.

## Usage

`--command` selects the routing command and defaults to `implement` for a **new** run. For a run
that already exists, the run's own record is the authority: `plan_run` reads `command_id` back from
the run, and a `--command` that disagrees with it is refused. Every subcommand below that takes
`--run-id` may therefore omit `--command`, whatever command the run was submitted under. Before
runtime 0.4.1 the parser default was applied instead, which re-planned the run under
`implement-feature` and corrupted its state store; `proposals/framework-change-proposal-FC-002.md`
carries the repair and the two runs it cost.

```bash
# 1. Materialize the run: one work item per phase, one per gate, guards evaluated
#    --command defaults to implement; pass it for any other lifecycle, e.g. --command refactor
python .claude/runtime/framework_runtime.py plan \
    --input feature-request=.claude/runs/inputs/<request>.md \
    --input change-request=.claude/runs/inputs/<request>.md \
    --input business-intent=.claude/runs/inputs/<intent>.md \
    --input architecture-context=.claude/runs/inputs/<context>.md

# 2. Ask the runtime what to do next. It prints the exact next command.
python .claude/runtime/framework_runtime.py next --run-id <run-id>

# 3. Lease one phase and build its envelope
python .claude/runtime/framework_runtime.py dispatch --run-id <run-id> --phase <phase>

# 4. The host dispatches the registered subagent using the emitted dispatch prompt.
#    The agent writes the artifact and its result envelope.

# 5. Ingest the result, validate it, and commit the transition
python .claude/runtime/framework_runtime.py complete --run-id <run-id> --phase <phase>

# 5b. A rejected artifact needs no command: the Recovery Controller classifies it and, while
#     the retry budget holds, schedules the next attempt itself. `next` reports the deadline.
#     Use `release` for the two cases the runtime cannot decide: an adapter holding a lease it
#     will never report against, and a block only a human can declare resolved.
python .claude/runtime/framework_runtime.py release --run-id <run-id> --phase <phase>     --reason tool_failure --detail "<what happened>"

# 6. Record the human gate decision that unblocks the next phase
python .claude/runtime/framework_runtime.py gate --run-id <run-id> \
    --gate "Planning Gate" --decision approve \
    --owner-role tech-lead --decided-by <who> --rationale "<why>"

# 6b. A rejected gate is a classified rollback, and the rollback is a second human decision.
#     `rollback` authorises it: the completed downstream cone of --target (default: the phase
#     the gate closes) -- the target and every completed phase that hard-depends on it, the
#     closed phase always among them -- is superseded: re-entered as a new attempt under its
#     existing idempotency key, its committed artifact kept immutable -- and the gate is
#     re-armed on the same work item for a fresh decision. Same owner-role and Producer
#     Exclusion checks as `gate`; refused once a phase in range has spent its charged
#     attempts, and refused while an approved gate stands over any phase in range (the closed
#     phase included), since a completed gate cannot be re-armed. The rejection's envelope
#     prints this command as its clearing action with only `--decided-by` left to fill;
#     `next` names it first while the rejection stands, and `recovery` derives it live.
python .claude/runtime/framework_runtime.py rollback --run-id <run-id> \
    --gate "Planning Gate" --target execution-planning \
    --owner-role tech-lead --decided-by <who> --rationale "<why>"

# 7. Inspect and prove
python .claude/runtime/framework_runtime.py status --run-id <run-id>
python .claude/runtime/verify_vertical_slice.py --run-id <run-id> --slice architect
python .claude/runtime/verify_multi_phase.py  --run-id <run-id>

# 7b. `status` on a TTY renders a colorized phase -> step -> gate tree with status icons
#     (done/running/pending/blocked/retrying/failed) instead of the plain table; off a
#     TTY (piped, redirected, or `NO_COLOR` set) it is the same plain table as always,
#     byte for byte. `--no-color`/`--color` override the TTY check either way; `--json`
#     prints the same tree as a machine-readable `framework.runtime/status-view.v1`
#     document instead; `--compact` stops after the table/tree, before the transitions,
#     recovery ledger, and events sections.
python .claude/runtime/framework_runtime.py status --run-id <run-id> --json
python .claude/runtime/framework_runtime.py status --run-id <run-id> --compact --no-color

# Why a run is not proceeding: every classified failure, in full
python .claude/runtime/framework_runtime.py recovery --run-id <run-id> [--open-only]

# What the run cost: the execution metrics (also written at every completion and carried by
# the final report). `--json` prints the whole document.
python .claude/runtime/framework_runtime.py metrics --run-id <run-id>

# Loading controls. `next` always lists every phase that may be dispatched alongside the one
# it names; `dispatch --load-profile full` restores the pre-0.7.0 read-everything behaviour
# for one dispatch; `--affected-area` declares a domain the task touches so the matching
# domain-conditional skill is read (repeatable, additive, persisted on the run).
python .claude/runtime/framework_runtime.py dispatch --run-id <run-id> --phase <phase> \
    --load-profile full
python .claude/runtime/framework_runtime.py plan --run-id <run-id> --affected-area database

# Prove the registered surface and the Validation Engine, independently of any run
python .claude/runtime/verify_registry_coverage.py
python .claude/runtime/verify_validators.py
python .claude/runtime/verify_recovery.py          # induces its own failures
```

Steps 3 to 6 repeat until `next` reports that nothing is actionable. Every command is safe
to repeat: see **Idempotency and replay** below.

## Run identity

`run_id` is a digest of the command, the workflow, and the full set of supplied inputs. The
**phase is not part of the key**. A run is one request travelling through one workflow, so
submitting the same request twice resolves to the same run and re-enters the state it
already reached, rather than starting a parallel run that would repeat every side effect.

Changing any supplied input produces a different `run_id`, because a changed request is a
different request.

## The state model

Every work item holds exactly one of seven persisted statuses:

| Status | Meaning | Canonical `task-queue.md` state |
|---|---|---|
| `pending` | enqueued, not leased | `Ready` when every guard passes, else `Waiting` |
| `leased` | lease issued to the adapter, invocation not yet started | `Executing` |
| `running` | invocation started, result outstanding | `Executing` |
| `retrying` | a classified retryable failure, waiting out its backoff | `Retrying` |
| `completed` | result accepted and committed (terminal, except under an authorised rollback) | `Completed` |
| `failed` | no further automatic progress is permitted (terminal, except a rejected gate under an authorised rollback) | `Failed` |
| `blocked` | an explicit, reasoned blocker | `Blocked` |

`retrying` carries `available_at`, the instant the work item becomes dispatchable again.
`config/task-queue.md` separates `Retrying` from `Waiting` by history rather than by
eligibility -- a retrying item has already executed -- so the two are not folded together. A
dispatch before the deadline is refused and reports the deadline; the scheduler promotes the item
to `pending` on the first evaluation after it passes.

Every blocked item records both a `blocked_reason` and a `blocked_by`, naming what raised
the block. The scheduler clears only blocks it raised itself: a guard-raised block lifts as
soon as the guard passes, while a block raised by the Validation Engine over a rejected
artifact persists until an operator clears it through `release`. Without that distinction a
rejection would evaporate on the next evaluation, because the guards ask whether a phase
*can* run, not whether its last result was accepted.

The seven are a projection of the eight canonical task states in `config/task-queue.md`, not
a competing vocabulary; each work item persists its `queue_status` alongside its status.
`Cancelled` is the one canonical state this runtime does not implement.

Two transition tables govern movement, one per work type. Any pair absent from a table is
forbidden, which is what makes the terminal statuses terminal: no automatic path leaves
`completed` or `failed`. Exactly two human-authorised pairs do, both recorded in the tables
under their own triggers and both written only by the `rollback` command.

```
state work item                                      gate work item
  pending   -> leased | blocked                        pending -> blocked | completed | failed
  leased    -> running | pending | retrying | blocked  blocked -> completed | failed | pending
  running   -> completed | failed | retrying |         failed  -> pending   (rollback_authorised)
               pending | blocked
  retrying  -> pending | blocked | failed
  blocked   -> pending | failed
  completed -> pending   (superseded; engine-checked: requires a supersession record)
```

`superseded` re-enters a committed phase as attempt n+1 under the same idempotency key: the
payload digest keeps the phase-canonical artifact path, so the key names the unit of work and
not one attempt of it. The prior `completion` block moves unmodified into the item's
append-only `supersessions` list beside the authorisation, and attempt n+1 writes its artifact
under `states/<phase>/artifacts/attempt-<n+1>/` -- the same filename, a different directory --
so attempt 1's artifact is never rewritten and `verify_multi_phase.py` M12 checks every
superseded completion exactly as it checks the current one. The per-attempt invocation,
result, ledger, and validation files are snapshotted under `states/<phase>/attempts/<n>/`
before the next dispatch overwrites them. `rollback_authorised` re-arms the rejected gate on
the same work item: the rejection moves into the gate record's append-only
`decision_history`, `decision` returns to null, and the gate blocks again for a fresh human
decision once the rebuilt evidence lands. A gate carrying a `decision_history` is never
auto-approved, whatever precedent other runs supply.

A gate never leases and never runs: `config/execution-engine.md` requires gates to be
explicit control states, so a gate is committed by a recorded decision, not an invocation.

`failed` is reserved for a work item that produced nothing at all *and* has no attempt left. An
artifact that exists but fails validation moves to `retrying` while the budget holds, and to
`blocked` with reason `awaiting_recovery_task` once it does not, because `config/task-queue.md`
forbids retrying a failed task in place and a rejected artifact is still repairable evidence.

## Transition guards

A `pending` state work item may be leased only when every guard passes. A guard returns
`pass`, `wait`, or `block`; the first blocking verdict decides.

| Guard | Question | Failure |
|---|---|---|
| `G1-CAPABILITY` | Does the full chain resolve to something invocable: active record, manifest declaring this workflow and phase, host registration, resolvable phase skills, registered validator? | `block` |
| `G2-CONTEXT` | Is a context slice declared for this phase? | `block` |
| `G3-PREDECESSOR` | Have all hard predecessors completed? | `wait`, or `block` if one failed terminally |
| `G4-GATE` | Does every gate closing a hard predecessor carry an approved decision? | `block`, `awaiting_human_decision`; or `awaiting_recovery_task` (class `gate-rejection`, carrying the gate name) when one was rejected |
| `G5-INPUT` | Is the owning agent's input contract satisfiable from the supplied inputs plus completed upstream artifacts? | `block` |

Guards are a pure function of persisted state, so re-evaluating them produces the same
verdicts and no additional transitions. A guard-raised block whose guards later fall to a
`wait` -- which happens to a successor, and to an undecided gate, when the phase they depend on
is superseded by a rollback -- returns the item to `pending` and resolves its envelope, so a
stale block never holds a run and a gate is never decidable over evidence being rebuilt.

### Where the dependency graph comes from

It is derived from the workflow specification, never configured here. For each phase:

1. an earlier phase whose **Output Artifact** is named in this phase's **Input** column
   creates a hard edge;
2. failing that, the immediately preceding row creates one, because
   `workflows/implement-feature.md` declares that phase order is row order;
3. an edge is downgraded to **soft** when the Input column declares an explicit
   alternative (` or `) that the supplied inputs already satisfy.

That third rule is why `execution-planning` can start from a supplied feature request
without waiting on `scope-and-acceptance`, exactly as its Input column permits, while
`solution-design-and-risk-assessment`, whose Input column names `execution-plan.md` with no
alternative, must wait.

### Gates

Gate ownership is read from `workflows/workflow-gate-matrix.md`. The runtime never invents
a decision: an undecided gate holds its successor in `blocked` until one is recorded.
Recording requires the owner role being exercised **and** who actually recorded it, and the
Producer Exclusion Rule is enforced, including through the retired `omn-` prefix that older
run evidence uses for role identifiers.

The decider is a human by default. Under `config/gate-policy.json`'s
`auto-on-clean-evidence` mode (specified in `config/gate-policy.md`, overridable per
invocation with `--gate-policy`), the runtime itself may record an approval for a
decision-eligible gate whose evidence is unambiguously clean -- attributed
`runtime:auto-policy`, through the same `record_gate_decision` path as a human decision,
with the evaluated conditions and thresholds in the record. Any condition failing, a
pinned gate, or a workflow/agent version with no approved precedent keeps the human path
unchanged.

## Failure classification, retry, and recovery

No failure is handled at the call site that noticed it. It is named by a failure class,
classified by `recovery_policy.py`, and the returned action is mapped onto whichever transition
the state tables permit. The table is the Failure Classification Matrix in
`config/execution-engine.md`, extended with the classes `config/runtime.md` names that the
matrix does not row out:

| Failure class | Detection point | Retryable | Action when not retried |
|---|---|---|---|
| `invocation-transport-failure` | adapter boundary | yes | escalate |
| `output-schema-failure` | validation | yes | escalate |
| `worker-loss` | queue control | yes | escalate |
| `request-validation-failure` | initialization | no | abort |
| `context-integrity-failure` | hydration | no | abort |
| `workflow-contract-violation` | validation | no | rollback |
| `gate-rejection` | gate state | no | rollback -- executed by `rollback` under a recorded human authorisation |
| `aggregation-conflict` | aggregation | no | remediate |
| `missing-capability-failure` | resolution | no | escalate |
| `policy-failure` | policy decision | no | escalate |
| `dependency-failure` | queue control | no | escalate |
| `gate-approval-required` | gate state | no | escalate |

A class absent from the table escalates. Treating an unrecognised failure as transient would
retry something nobody has reasoned about, so the default is the action that cannot lose
evidence.

Three faults arrive at the same detection point and must be told apart before an action is
chosen. An **undeclared side effect** is a policy violation: the agent wrote outside its
permitted set, which no repair pass undoes. A **structural defect the Validation Engine named**
is the schema-correctable omission the retry-eligibility list admits. A **workflow contract
violation** is neither, and is raised by the resolution chain before an artifact exists.

### The retry profile

`config/runtime.md#retry-policy`, field for field: three attempts per state, exponential backoff
from a two-second base, doubling, capped at sixty seconds, jitter of plus or minus twenty
percent. The cap applies after the jitter, so `max_delay_seconds` is a maximum.

The jitter is derived from the work item's own idempotency key rather than drawn at random. A
random draw would make the same failure produce a different envelope on each evaluation, which
would break the replay guarantee the rest of the runtime rests on. Deterministic jitter is
stable per work item and spread across work items, which is what jitter is for; it is not
unpredictable, and nothing here needs it to be.

### Attempts, and what they are charged to

Every dispatch increments `attempt`. Attempts split into two counters, because they mean
different things:

- an attempt whose adapter never reported produced nothing to judge, so it increments
  `attempts_lost` and is **not** charged against the retry budget. `config/task-queue.md` makes
  lease expiry a recovery classification that does not itself mean failure. What bounds
  repeated loss is the escalation trigger, not the budget;
- an attempt that produced a result the Validation Engine classified **is** charged.

The idempotency key is preserved across attempts, because another attempt at the same payload is
the same unit of work.

### What `release` is still for

Two situations the runtime cannot decide on its own:

- **an adapter holding a lease it will never report against.** Nothing crossed the boundary, so
  the runtime cannot tell a slow adapter from a lost one. `release` declares it lost; the class
  is `worker-loss`, which is retryable, so the item moves to `retrying` under the profile;
- **a block only a human can declare resolved.** The operator is asserting the condition is
  gone, which is a control action rather than a classification, so the item returns to `pending`
  at once and its open envelopes are marked resolved. This path is charged, and it is refused
  once the charged attempts reach `max_attempts`.

### The repair pass

A phase dispatched after a rejected attempt carries that rejection forward: the envelope gains a
`prior_validation` block, and the dispatch prompt gains a repair-pass section naming every
failed check, its severity, its quality reference, and the Validation Engine's finding, together
with the artifacts already on disk. The runtime states *what* was rejected and *what exists*;
how to repair it belongs to the agent's own quality contract, so no repair instruction is ever
authored here.

## Structured failure envelopes

Every blocked transition emits one, whether the block came from a guard, from a rejected
artifact, from an exhausted budget, or from a rejected gate. The field set is the union of what
two specifications require of that moment: the recovery ledger requirements in
`config/runtime.md` (failure class and detection point, chosen action and reason, retry attempt
count and next timeout, impacted state and artifact references) and the escalation contract in
`config/execution-engine.md` (escalation id, run, state, reason code, required decision type,
evidence bundle reference, proposed options, default fallback action).

One field is the runtime's own addition. `clearing_action` is the exact command that resolves the
condition, because a structured failure that does not say what to do next is a diagnosis without
a prescription.

Envelopes are written twice, for two readers:

- `states/<phase>/failure-envelope.json` -- the current condition on one work item, which is
  what an operator looking at one stuck phase reads;
- `recovery-ledger.json` -- append-only, run-wide, and what an auditor reads. Entries are marked
  resolved in place rather than removed: an entry that vanished on resolution would erase the
  evidence that anything was recovered at all.

`framework_runtime.py recovery --run-id <id>` prints them.

## Validation Engine coverage

Every artifact type the active Phase Models name as a file has a registered validator. The map is
keyed by artifact type rather than by phase, so it also carries one type no phase emits:

| Artifact | Validator | Emitting phases |
|---|---|---|
| `execution-plan.md` | `plan_validator.py` | `implement-feature/execution-planning` |
| `technical-design.md` | `design_validator.py` | `implement-feature/solution-design-and-risk-assessment`, `refactor/scope-invariants-and-risk-profile` |
| `bug-analysis.md` | `bug_analysis_validator.py` | `fix-bug/root-cause-analysis` |
| `investigation-report.md` | `investigation_report_validator.py` | `investigate/technical-discovery`, `research/technical-validation` |
| `release-note.md` | `release_note_validator.py` | `implement-feature/documentation-and-release-handoff`, `investigate/publication`, `research/findings-publication`, `review-pull-request/documentation-impact`, `release/communication-and-post-release` |
| `review-package.md` | `review_package_validator.py` | `implement-feature/quality-review`, `review-pull-request/code-quality-review`, `code-quality-scan/repository-quality-scan` |
| `implementation-report.md` | `implementation_report_validator.py` | `implement-feature/implementation`, `fix-bug/fix-implementation`, `refactor/refactor-implementation` |
| `framework-change-proposal.md` | `change_proposal_validator.py` | no phase; a governance artifact validated outside any run |

The first two are hand-written, because the Planner and Architect contracts declare their own
numbered check sets and a validator for those must reproduce that numbering exactly. The other
four have no such check set: their owning agents ship prose contracts rather than a `quality.md`
module, so the authority is the canonical template, the agent's declared outputs and decision
rules, and the framework registries. Those rules are declared as data and executed by
`artifact_contract.py`, rather than reimplemented four times.

Making them decidable required the three bare templates to carry provenance and identifiers.
`templates/bug-analysis.md`, `templates/investigation-report.md`, and `templates/release-note.md`
are now at version 1.1.0: each gained the leading metadata block every validated artifact type
carries, and the tables that turn its agent's own decision rules into something checkable -- an
evidence register a causal claim must cite, a confidence and staleness column per observation, a
known-issue row per shipped issue. Section titles and order are unchanged.
`templates/review-package.md` is new and records why four differently-named review outputs are
one artifact type.

`verify_validators.py` proves both directions: a conforming artifact of every type is accepted,
and the same artifact with one declared mutation is rejected *by the named check*. A validator
that accepts everything and a validator that rejects everything both report a result, and
neither is a decision.

`release_note_validator.py` decides the release position against evidence, not membership.
documentation 1.2.0 keys it on the phase's communication basis: only
`communication-and-post-release` states a deployment outcome (`released`, `partial`,
`rolled-back`), read off a supplied deployment status, and the other four phases publish
`handed-off`, `findings-published`, or `delta-published`. `R8` requires the evidence type each
position is read off among `sourceInputs`; `R9` maps the invocation envelope's `state_id` to its
basis and is declared not-machine-checkable only when no envelope is supplied, on the runtime
path and on the validator's own `--envelope` command line alike. A committed note produced by an
earlier documentation version is set aside by `verify_validators.py` as produced under a
superseded contract and reported by count (`SUPERSEDED_CONTRACTS`); the validator exempts nothing,
and such notes stay as recorded.

## Idempotency and replay

Each work item carries an `idempotency_key` derived from the run, the phase, the owning
agent and its version, and the payload digest (context slice plus narrowed inputs). The
attempt number is excluded, so a retry preserves the key.

| Repeated call | Behaviour |
|---|---|
| `plan` on an existing run | re-enters the run; no work item is recreated, no event emitted |
| `dispatch` of a completed phase | suppressed; recorded as a replay |
| `dispatch` of a leased or running phase with an unchanged payload | suppressed; the existing envelope stands |
| `dispatch` with a *changed* payload against a held lease | refused as a context-integrity failure |
| `complete` of a completed phase whose artifact is unchanged | suppressed; the prior outcome is reprinted |
| `complete` of a completed phase whose artifact has since been edited | refused: committed evidence is immutable |
| aggregation over an unchanged run | no second package, no second event |

A suppressed call writes one bookkeeping entry to `state.json` and nothing else: no event,
no transition, no file. That entry is the evidence that the repetition was seen and
suppressed. The save that records it also refreshes the run's stamps and guard details and
rebuilds `task-context.yaml`, so a replay is never byte-neutral. `verify_multi_phase.py` check
M14 therefore re-executes the runtime against a disposable copy of the working tree holding the
run, with the copy as the runtime's root, proves the run's fingerprint in the copy unchanged,
names the files the replay's bookkeeping rewrote there, and removes the copy; the run itself is
never written.

## What a dispatch asks an agent to read

Runtime 0.7.0 changed how much of the framework an agent reads per dispatch, not what it
does. The policy is `config/runtime.md` (Task Context, Progressive Module Loading, Context
Loading, Conditional Skill Dispatch); this is what the runtime emits for it.

- **Task context first.** `runs/<run>/task-context.yaml` is rebuilt from persisted state at
  every dispatch, completion, gate decision, and rollback (`task_context.py`), and written
  only when its content changed. It carries the run's objective, scope, acceptance criteria,
  decisions, constraints, changed files, risks, open questions, completed phases, gate
  decisions, repository context, and parallel groups as one-line facts keyed by the source
  artifact's own identifiers -- deterministic table parsing, never summarisation, and only
  from artifacts the Validation Engine accepted. The dispatch prompt names it as the first
  read after the envelope and names, per upstream artifact, the sections to read first and the
  sections to open only on demand (`UPSTREAM_SECTIONS`).
- **Two module tiers.** `capability_bindings.load_profile` splits the manifest's `loadOrder`
  by declared role: core (`system.md`, `reasoning.md`, `output.md`, `quality.md`) is read in
  full up front; on-demand (`identity.md`, `execution.md`, `examples.md`) carries a
  `load_when` trigger and is read the moment it applies. The runtime performs the
  Initialization checks the lifecycle modules ask for and records them under
  `capability_bindings.contract_checks`. `--load-profile full` restores the old behaviour.
- **Read hints on the frozen slice.** Membership and digests are unchanged; each member now
  carries `read: required | on-demand | runtime-resolved` with a reason, and a member both
  the base and the phase slice name is frozen once.
- **Skill dispatch.** `skill_dispatch[]` gives every resolved code `required` or
  `not-triggered` with its basis; domain-conditional codes follow the task context's
  `affected_areas`, unknown never narrows, and security is always required in review and
  validation phases.
- **Parallel groups.** `plan` prints the topological levels of the hard-dependency graph;
  `next` lists every phase dispatchable alongside the one it names and reports a phase that
  could start early on a soft edge as a choice that forgoes the predecessor's artifact.
- **Metrics.** `execution-metrics.json` and the final report's Execution Metrics section carry
  the per-run trace, with a per-dispatch byte estimate under the legacy and the progressive
  rules; `verify_*` scripts are unchanged.

## Run layout

```
runs/
  inputs/<request>.md                   operator-supplied input
  <run-id>/
    execution-request.json              control-plane request
    state.json                          work items, gates, ordered transition log, replays
    task-context.yaml                   the compact shared state every phase reads first
    execution-metrics.json              the run's performance trace
    run-ledger.json                     run status, per-phase index, recovery summary
    events.jsonl                        append-only canonical progress events
    recovery-ledger.json                append-only recovery and escalation ledger
    completion-package.md               Output Aggregator package, run-wide
    demo/                               only when the run was planned with --demo
      demo-context.json                 the switch and the presentation options
      markers.jsonl                     append-only execution markers, millisecond-stamped
      recorder.log                      what the demo layer swallowed rather than raise
      narration/, narration.wav         per-scene voice segments and the assembled track
      capture/                          only when the run filmed the real application
        session.mp4                     the raw screen recording
        capture-manifest.json           backend, window, rectangle, and the instant the
                                        recording's own clock read zero
      edit/                             overlay frames and the filtergraph of the last cut
      presentation_demo.mp4|.gif|.html  the compiled presentation (see Demo mode)
      presentation_demo.srt             the narration as subtitles
      presentation_demo-script.md       the narration as a script, to read before presenting
      presentation_demo-poster.png      a still, for slides
      build-manifest.json               outputs, fallbacks and their reasons, engine, timings
    states/<phase>/
      context-snapshot.json             frozen context slice with per-file digests
      invocation-envelope.json          canonical Agent Invocation Envelope
      dispatch-prompt.md                what the adapter sends to the agent
      adapter-prompt.md                 bootstrap mode only
      state-ledger.json                 per-phase execution ledger
      result-envelope.json              canonical Agent Result Envelope, written by the agent
      validation-report.json            Validation Engine output, per check
      failure-envelope.json             current classified failure, when the phase is not clear
      artifacts/<artifact>              the agent's declared output
    gates/<gate-slug>/
      failure-envelope.json             the same, for a gate held or rejected
```

Runs produced before slice 3 keep the older flat layout, in which the run root held one
phase's ledger and artifacts. `verify_vertical_slice.py` reads both.

## Demo mode

`plan --demo` records a run for an autonomous end-to-end
demonstration and compiles the presentation itself when the run completes. Runtime 0.8.0.

The film is an edit of the real application on screen. The markers are not the picture; they
are the **clock and the script** that decide where to cut it.

```
  RunLedger.emit ──(one stat on runs/<run>/demo/demo-context.json)──▶ demo.on_event
                                                                            │
      dispatch-prompt.md ─┐                                                 ▼
      invocation-envelope ┼─ read from disk at event time ─▶  Recorder  ─▶  markers.jsonl
      result-envelope     │                                     ▲             │
      validation-report  ─┘        host PostToolUse hook ───────┘             │
                                                                              │
  demo --record-start ─▶ OBS (window capture) ─┐                              │
                         else ffmpeg (gdigrab) ┴─▶ capture/session.mkv        │
                                               └─▶ capture-manifest.json      │
                                                   (t0: the recording's zero) │
                                                                              ▼
                                        editor: marker timestamp - t0 = frame ▼
                                        ┌─────────────────────────────────────┐
                                        │  head 1x │ condensed Nx │ tail 1x   │  per beat
                                        └─────────────────────────────────────┘
                       overlays (frame, rail, lower thirds) + narration + cards
                                                   │
                                                   ▼
                                         presentation_demo.mp4

  no recording ─▶ the drawn dashboard instead ─▶ mp4, else GIF, always an HTML deck
```

- **Switch.** `runs/<run>/demo/demo-context.json`. Absent by default; the runtime's only cost
  without `--demo` is one `stat()` per emitted event, and the demo package is never imported.
  `--demo` on a run already in flight throws the switch and backfills the events already
  persisted; `demo --run-id <run> --backfill --build` does the same for any historical run.
- **Markers.** One JSON object per observed fact: `marker_id`, `seq`, `timestamp` (UTC,
  millisecond), `t_ms` since the first marker, `agent_name`, `action_type`, `phase`, `title`,
  `payload`, `visual_snapshot_text` (the dashboard rendered as text at that instant),
  `source` (`runtime` / `host-hook` / `backfill`), `source_event_id`. The exchange with an
  agent is captured at the adapter boundary, which is the only place the runtime sees it: the
  dispatch prompt (bytes, digest, excerpt, token estimate) at `invocation_started`, the result
  envelope (status, artifact refs, structured output excerpt, the agent's own prose note) at
  `invocation_completed`. Token figures are the runtime's context estimate (bytes / 4) and
  are labelled as estimates; envelopes older than 0.7.0 are measured at recording time.
- **Host tool calls.** Merge `runtime/demo/hooks.settings.json` into the host's settings to post
  every tool call to `framework_runtime.py demo --hook`; it records a `tool_invocation`
  marker against the run named in `runs/.active-demo` and is inert otherwise. That file
  carries a `<framework-dir>` placeholder in its command, because a file on disk cannot know
  the name of the directory it was copied into; `demo --print-hook` prints the
  same block with the path resolved for this tree. Unmerged, the markers carry the runtime's
  own events and nothing the host did, which is a thinner film, not a broken one.
- **Recording.** `plan --demo --demo-record` starts the camera as part of planning, before
  the run's first event is emitted, so the film opens on the run being accepted rather than
  on the first step after it; `demo --record-start` does the same for a run that already
  exists. Either way the command returns at once and the recording keeps running, owned by a
  detached helper, while the run proceeds.
  `--record-stop` finalises it, and a run that completes while still filming stops its own
  camera first. Two backends: **OBS Studio** over obs-websocket, which captures the window
  itself through Windows Graphics Capture and is unaffected by anything in front of it, and
  **ffmpeg gdigrab** over the window's rectangle, which needs no setup but records what is on
  screen there, so the window is raised first and refuses to record if it will not come
  forward. Because that backend films a *place on the screen* rather than a window, anything
  that later comes in front of it -- or the bare desktop, if the window is minimised -- is
  filmed instead. The recorder therefore watches which window is actually in front for the
  whole take, tries to take the window back when it loses it, and records every span during
  which it did not have it. The editor excludes those spans: a beat running into one is cut
  back to where the window was still in front, a beat wholly inside one is dropped, and the
  build manifest says how many seconds were withheld. A demo is made to be published, and
  whatever else was on that screen would be published with it.
  OBS is used when its WebSocket server is enabled (OBS, Tools, WebSocket Server
  Settings); the backend actually used is named in the capture manifest. Only the target
  window's rectangle is ever recorded, and its resolution, position, backend, and the instant
  its clock read zero are all in `capture/capture-manifest.json`.
- **The cut.** Every marker's video time is `timestamp - started_at`, so each beat of the run
  has a frame. A span at or under the pace's threshold plays untouched; a longer one keeps its
  head and tail at real speed and condenses the middle, with a badge naming the factor.
  Nothing is fabricated and nothing is hidden. The edit decision list is written into
  `build-manifest.json` for review.
- **Pace and explanation.** `--demo-pace relaxed` with `--demo-explain`, the default, is for a
  viewer meeting the framework once: every beat is held long enough to take in, waiting is
  condensed far less, and the narration explains what each step *means* in plain language
  instead of naming it. In that mode the script is recorded first and the picture is paced to
  it, so the film runs to minutes rather than to a target — a two-minute run becomes a
  five-minute explanation. A beat given more narration than it has footage is first
  un-condensed, then slowed to a third speed, and only then held on its last frame, in that
  order, so the picture keeps moving while there is film left to show. A concept is explained
  the first time it appears and simply named thereafter. `--demo-pace brisk --demo-no-explain`
  is the short highlight reel for an audience that already knows the framework.
- **A beat with no footage of its own.** A dispatch and the response to it can be markers a
  fraction of a second apart. In explaining mode such a beat borrows the seconds immediately
  after it, which is the same recording and is what happened next; it never takes from a beat
  that cannot spare them.
- **The frame.** The capture is inset in a 1920x1080 composition: brand bar, a workflow rail
  showing every phase and gate with the live one lit, a lower third naming the beat, a
  progress bar, and opening and closing cards carrying the run's own numbers. All of it is
  drawn with Pillow into one overlay track, so no ffmpeg font configuration is involved.
- **Sound.** Narration only. The recording's own audio is never mapped into the output, so
  whatever was playing on the machine during the take is not published. Voices are tried in
  the order edge-tts, pyttsx3, Windows SAPI, macOS `say`, espeak-ng, and delivered a little
  below the engine's own pace; `--demo-speech-rate` moves that between -6 and 6.
- **Captions, three ways.** Every spoken line is also shown on screen as it is said, so the
  film can be followed with the sound off. The timing is not guessed: the narration is
  synthesised before the picture is cut, so each line's real duration is known, and
  `captions.py` splits it where a reader would pause and gives each cue a share of that
  duration in proportion to how long it takes to say. The same cues are written beside the
  film as `presentation_demo.srt` for playing it elsewhere, and as
  `presentation_demo-script.md`, a script a presenter can read beforehand: what is on screen
  at each point, what is said over it, and where in the film it falls. `--demo-no-captions`
  leaves the picture clean; the subtitle file and the script are written either way.
- **Fallbacks, recorded.** No recording: the drawn dashboard. A failed edit: the drawn
  dashboard. No ffmpeg: animated GIF. No Pillow: HTML deck with text frames. Every skipped
  stage lands in `build-manifest.json` with its reason, and a demo failure of any kind is
  logged to `demo/recorder.log`, never raised into the run.

`<framework-dir>` below is this framework directory's real name, `.claude` in the framework's
own checkout.

```bash
# film a run and let it cut itself when the run completes
python <framework-dir>/runtime/framework_runtime.py plan --demo --demo-record \
    --input feature-request=<path>
#   ... drive the run in the window; the camera is already rolling ...
python <framework-dir>/runtime/framework_runtime.py demo --run-id <run> --record-stop --build

# or start the camera separately, against a run that already exists
python <framework-dir>/runtime/framework_runtime.py demo --run-id <run> --record-start

python <framework-dir>/runtime/framework_runtime.py demo --run-id <run> --snapshot   # markers so far
python <framework-dir>/runtime/framework_runtime.py demo --run-id <run> --build --drawn   # no footage
python <framework-dir>/runtime/framework_runtime.py demo --run-id <run> --backfill --build  # any run

# re-cut the same footage for a different audience, without recording again
python <framework-dir>/runtime/framework_runtime.py demo --run-id <run> --build \
    --pace brisk --no-explain          # short, for people who know the framework
python <framework-dir>/runtime/framework_runtime.py demo --run-id <run> --build \
    --pace relaxed --speech-rate -2    # longer and slower still

# print the host tool-hook wiring block with the framework directory resolved
python <framework-dir>/runtime/framework_runtime.py demo --print-hook
```

A recording made by hand carries no start instant, so `--build --capture-offset <seconds>`
states how far into the file the run's first marker falls and the rest follows from it.

Optional dependencies: `Pillow` for every drawn pixel (present in most Python installs),
`ffmpeg` on the PATH (or `imageio-ffmpeg`, or node's `ffmpeg-static`) for capture and for the
cut, OBS Studio for true window capture, and one of `edge-tts` / `pyttsx3` for a voice where
the OS offers none. None is required; each absence is a recorded fallback, though without
ffmpeg there is no video at all and the HTML deck is the whole output.

## What is implemented

Named against the components in `config/runtime.md` and `config/execution-engine.md`:

| Component | Status | Note |
|---|---|---|
| Workflow Resolver | implemented | `registry/commands.yaml` -> `registry/workflows.yaml`, 9 active commands over 7 active workflows |
| Task Router | implemented | Phase Model table of the routed workflow; all 7 active workflows publish one |
| Agent Registry Loader | implemented | record -> manifest -> module set in declared `loadOrder` |
| Skill Registry Loader | implemented | manifest and phase-mandatory codes, registry resolution rule |
| Context Loader | implemented | frozen, content-addressed, narrowed per phase; every member carries a read hint and every resolved skill a dispatch verdict |
| Task Context | implemented | `task-context.yaml`, rebuilt from persisted state at every transition; the first thing a dispatched agent reads |
| Agent Invocation Gateway | implemented | canonical Agent Invocation Envelope |
| Agent Adapter | implemented | one adapter: `host-subagent` |
| State Engine | implemented | six persisted statuses, two transition tables, ordered transition log |
| Execution Context Store | implemented | run ledger, per-phase ledgers, artifact ledger, append-only `events.jsonl` |
| Task Queue | partial | dependency evaluation, leases, blocking, bounded retry with backoff, idempotent re-dispatch. No lanes, no priority, no visibility timeout, no lease expiry |
| Phase-to-phase state transfer | implemented | completed upstream artifacts enter downstream input contracts under the producing manifest's output identifier |
| Gate control states | implemented | gate work items, ownership from the gate matrix, Producer Exclusion Rule enforced |
| Validation Engine | implemented | six artifact types, one per file the active Phase Models name: `execution-plan.md`, `technical-design.md`, `bug-analysis.md`, `investigation-report.md`, `release-note.md`, `review-package.md` |
| Output Aggregator | implemented | run-wide completion package, provenance manifest, transition log |
| Observability Service | partial | canonical progress events, the transition log, and the recovery ledger; no metrics pipeline |
| Recovery Controller | implemented | every failure is classified against the matrix, the least destructive valid action is chosen, and a structured failure envelope plus a recovery-ledger entry is written. The `rollback` action of a gate rejection is executed by the `rollback` command under a recorded human authorisation (supersession of the target phases, re-arm of the gate); remediation is decided but not executed, and nothing rewinds a state without that authorisation |
| Retry | implemented | classified eligibility, the `Retrying` state, exponential backoff with deterministic jitter, a bounded budget split between worker loss and charged results, automatic scheduling, and a repair pass that carries the rejection back to the agent |
| Cancellation | not implemented | no `Cancelled` state |
| Human Escalation Service | partial | escalations are opened, classified, reasoned, closed by a recorded decision, and carry a required decision type, proposed options, and a clearing action; there is no case queue, notification, or deadline |
| Memory Loader | not implemented | runs declare memory hydration as not requested |
| Metrics pipeline | partial | `execution-metrics.json` per run, rebuilt at every completion and aggregation and by `metrics`; no cross-run store |

## The adapter boundary

The runtime core never talks to a model. It builds the canonical envelope and hands it to
an adapter, exactly as `config/execution-engine.md` requires.

One adapter exists: `host-subagent`. It dispatches the subagent registered by
`agents/<agent-id>.agent.md`. That registration is an **entry point**, not a contract: it
loads `agents/<agent-id>/manifest.yaml` and the modules that manifest declares. There
remains exactly one authoritative contract per agent, under `agents/<agent-id>/`.

The adapter has two dispatch modes. Both use the same single registration file.

| Mode | How the host receives the adapter definition | When to use |
|---|---|---|
| `native` | The host resolves the agent by identifier from its own scan of `agents/*.agent.md`. | Default. Requires the host to have scanned the file, which happens at host session start. |
| `bootstrap` | The runtime reads the same file, strips its frontmatter, and supplies the body as the instruction prompt for a generic host subagent. | When the registration was added after the host session started, so the identifier is not yet resolvable. |

Neither mode copies agent contract text. `bootstrap` loads the registration from disk at
dispatch time and writes it to `runs/<run-id>/states/<phase>/adapter-prompt.md`.

## Known gaps recorded by this slice

1. **Every phase resolves capability and context; the blocking mechanism remains.** This gap is
   closed. All 37 declared phases across the eight active workflows clear both `G1-CAPABILITY`
   and `G2-CONTEXT`: each of the twelve phase owners holds a record in `registry/agents.yaml`, a
   manifest, a module set, and a registered validator for the artifact its phases emit, and every
   phase declares a context slice. What has not changed is the runtime's behaviour when a phase
   cannot resolve. It does not skip such a phase: it enqueues it, blocks it with a recorded
   reason, and reports it as an open escalation, so a run reports what it could not do rather
   than appearing to have done it. `verify_registry_coverage.py` check `C6` reports the per-phase
   verdict and is the authority on it; the historical counts and reasons are in
   `reports/registry-coverage-report-2026-08-18.md`, read against the date it carries.
2. **Gate decisions are recorded, not solicited.** There is no notification path, no case
   queue, and no timeout. An undecided gate holds its successor indefinitely.
3. **Retry is scheduled, not driven.** Classification, backoff, and promotion are the
   runtime's, and a rejected artifact no longer waits for an operator. But the runtime has no
   process of its own: a deadline is evaluated when a command next reads the run, so an
   unattended run does not advance past its own backoff. `next` reports the deadline; nothing
   fires at it. The part of this gap that read "rollback is decided but not executed" is
   closed: a gate rejection's `rollback` verdict is executed by the `rollback` command under a
   recorded human authorisation, proven by `verify_recovery.py` Run D. What remains decided
   rather than executed is the `remediate` action of an aggregation conflict, and the
   `rollback` verdict of a `workflow-contract-violation`, which is raised before any phase
   has committed and so has nothing to supersede.
4. **No lease expiry timer.** `lease_expires_at` is carried on every work item but never set or
   enforced. A lost adapter leaves a work item `running` until an operator runs `release`, which
   is an explicit action rather than a timeout. What follows the reclaim *is* the runtime's --
   `worker-loss` is classified and the retry is scheduled under the profile -- but declaring the
   worker lost in the first place is still the operator's.
5. **`workflows/workflow-engine.md` has no planning state.** Its Implement Feature state
   machine goes `ScopeAlignment -> SolutionDesign`, omitting execution planning, and so
   disagrees with `workflows/implement-feature.md`. The registry points at
   `workflows/implement-feature.md`, so routing is unambiguous, but the two documents
   should be reconciled.
6. **Phase identifiers are canonical for every active workflow.** All eight publish a Phase
   Model table, so `plan` builds a phase graph for any of them, and the frozen context slice
   now adds the routed workflow's own specification rather than naming `implement-feature`
   for every run. A context slice is declared for all 37 phases, so no phase blocks at
   `G2-CONTEXT` today. One limit remains: `workflows/workflow-engine.md` names a different owner
   than the Phase Model for the refactor scope state and for two analysis states in
   `investigate` and `research`; the Phase Models follow the agent manifests and the
   registry, which are the machine authorities, and each records the divergence.
7. **`review-package.md` is routed from five phases, and all five dispatch.**
   `review-pull-request/code-quality-review` named it first; `implement-feature/quality-review`,
   `release/artifact-packaging`, `review-pull-request/structural-compliance`, and
   `code-quality-scan/repository-quality-scan` now name it too,
   each of the earlier four replacing a prose cell that column previously carried. They were always one artifact type
   -- `templates/review-package.md` records that every review phase emits a severity-classified
   findings set closing with a readiness decision -- and one validator judges all of them.

   The declaration is only half of an output contract: `resolve_output_contract` also reads the
   owning agent's manifest `outputs`. That half now exists for both producers.
   `dev-2-reviewer` holds an active record in `registry/agents.yaml`, a manifest at
   `agents/dev-2-reviewer/manifest.yaml` declaring `review-package.md` as its required output,
   a seven-module set, and a `quality.md`. `architect` declares the same artifact as a conditional
   output, for `review-pull-request/structural-compliance` only, which is the phase
   `review_package_validator` already contracted it to produce: the validator's `producers` tuple
   names both agents, and the structural lens is carried by the `architecture` and `security`
   categories rather than by a separate artifact type. The reviewer slice is proven end to end by
   `verify_vertical_slice.py --slice reviewer` against
   `runs/run-93b302cbdb28/states/quality-review/`.
8. **Self-hosting decides the route and holds the record; executing a phase is now possible for
   all of them.** `config/self-hosting-profile.md` routes every framework-internal change, and
   `verify_self_hosting.py` proves the route resolved, the change proposal validates, and the run
   satisfied the profile's Completion Rule. All 36 of 36 phases are dispatchable, so every routed
   command can reach every phase of its workflow; what a verifier still cannot prove from the
   profile alone is that an agent, rather than the operator, did the work on any given past run.
   Work for a phase that does block is performed by the operator against the run and recorded per
   phase in the change proposal, which is why `operator` remains a permitted `producedBy` on that
   artifact type. That permission is a fallback for a recorded blocker, not a standing second
   producer.

   The Evidence Rule follows the same honesty. `E-6` and `E-7` require a phase artifact and its
   validation report only where the routed workflow has a dispatchable phase, and
   `change_proposal_validator.py` check `F12` resolves that through the runtime's own capability
   chain rather than accepting the author's claim.
9. **A validator is necessary for a phase to dispatch, and is not sufficient.** This is the
   design principle the rollout established, and it still governs every row authored from here on.
   A registered, manifest-backed agent cannot dispatch a phase whose Phase Model names its Output
   Artifact as a decision or a package rather than a file, because no validator can be registered
   against a decision; nor can it dispatch a phase naming a file its own manifest does not declare
   among `outputs`; nor one for which no context slice is declared. Each of those is an
   independent condition, and each was observed blocking a phase under an owner that was otherwise
   complete -- `fix-bug/triage-and-impact` and later `review-pull-request/structural-compliance`
   on the output contract, and the five reconciled publication and review phases on the context
   slice, which only surfaced once their output contract resolved and `G1-CAPABILITY` stopped
   returning first. All are now closed. The reason a phase blocks is narrower and more specific
   than "the agent is missing"; `verify_registry_coverage.py` reports it per phase.
