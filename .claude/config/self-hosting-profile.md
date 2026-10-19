# Configuration: Self-Hosting Command Profile

## Purpose

One command profile, `framework-internal-change`, that decides how a change to this framework is
routed. It answers three questions mechanically, so a framework change cannot be made ad hoc by
default:

1. Is this change framework-internal? (`## Scope Rule`)
2. Which command carries it? (`## Routing Table`)
3. What evidence must the change proposal carry before the change is accepted?
   (`## Evidence Rule`, `## Completion Rule`)

The profile is executable. `runtime/self_hosting.py` parses the tables below and resolves a
routing decision against `registry/commands.yaml` and `registry/workflows.yaml`, so a routing row
naming a command the framework cannot actually route is a verification failure rather than a
document that merely reads well:

```bash
python .claude/runtime/self_hosting.py classify --path .claude/workflows/release.md
python .claude/runtime/self_hosting.py route --intent capability-addition
```

## Profile Identity

| Field | Value |
|---|---|
| Profile identifier | `framework-internal-change` |
| Version | 1.0.0 |
| Status | active |
| Owner | Architecture |
| Applies to | every change whose touched paths satisfy `## Scope Rule` |
| Authority over | command selection, required inputs, required evidence, acceptance |
| Not authority over | phase ownership, gate ownership, artifact contracts, which stay with the routed workflow's Phase Model, `workflows/workflow-gate-matrix.md`, and the owning agent's contract |

There is exactly one profile. A second profile would mean two answers to "which command carries
this change", which is the ambiguity this profile exists to remove.

## Scope Rule

A change is **framework-internal** when at least one touched path matches an inclusion rule and
that path matches no exclusion rule. Exclusions take precedence over inclusions.

| Rule | Path pattern | Decision | Reason |
|---|---|---|---|
| `SR-1` | `.claude/**` | in-scope | Every framework surface the runtime resolves lives here |
| `SR-2` | `decision-matrix.md`, `quality-gates.md`, `rule-engine.md`, `working-memory.md` | in-scope | Repository-root governance documents that agent contracts cite |
| `SR-3` | `.claude/runs/**` | out-of-scope | Run evidence, written by the runtime during execution |
| `SR-4` | `.claude/reports/**` | out-of-scope | Dated evidence reports, emitted by an increment rather than routed as one |
| `SR-5` | `.claude/proposals/**` | out-of-scope | The change proposal is the governance record of a routed change, not a second change |
| `SR-6` | `.claude/runtime/__pycache__/**` | out-of-scope | Interpreter output, not a source surface |

`SR-3` to `SR-6` are the exemption set, and it is closed: a path is exempt only when a rule in
this table exempts it. The first three exist for one reason — routing the evidence of a routed
change would recurse without terminating, because the run carrying the change writes that
evidence. Nothing is exempt for being small, urgent, or obvious.

A change touching both in-scope and out-of-scope paths is framework-internal. That is the normal
shape: an increment edits the framework and writes its own evidence.

## Routing Table

Every framework-internal change resolves to exactly one change class, and every change class
resolves to exactly one command. The first row whose Selector holds decides.

| Change Class | Selector | Command | Primary Workflow | Required Inputs | Entry Phase |
|---|---|---|---|---|---|
| `decision-support` | The change cannot be specified yet: current-state behaviour or the option set is unknown | `/investigate` | `investigate` | `problem-statement` | `problem-framing` |
| `external-research` | The change depends on evidence that does not exist inside this repository | `/research` | `research` | `problem-statement` | `research-framing` |
| `defect-repair` | A registered capability behaves other than its contract declares | `/bugfix` | `fix-bug` | `defect-report` | `triage-and-impact` |
| `structure-preserving-change` | Structure or wording changes; no contract, routing, or runtime behaviour changes | `/refactor` | `refactor` | `change-request`, `business-intent`, `architecture-context` | `scope-invariants-and-risk-profile` |
| `capability-addition` | The framework gains a capability, a contract, a registry record, or a runtime behaviour it did not have | `/implement` | `implement-feature` | `feature-request`, `change-request`, `business-intent`, `architecture-context` | `scope-and-acceptance` |
| `change-review` | An authored framework change is being assessed before acceptance | `/review` | `review-pull-request` | `review-request` | `code-quality-review` |
| `framework-release` | A framework version is being published to its consumers | `/release` | `release` | `release-candidate` | `readiness-assessment` |

Reading order matters. `decision-support` and `external-research` precede the delivery classes
because a change that cannot yet be specified must not be routed as though it could.
`structure-preserving-change` precedes `capability-addition` because the narrower claim, that
nothing observable changes, is the one that has to be defended.

`/document` and `/test` are phase-scoped commands, reachable inside a routed run rather than as
an entry class of their own; `commands/command-catalog.md` records the phases they own.

### Routing Resolution

- A routing row resolves when its Command has an active record in `registry/commands.yaml`, that
  record's `primaryWorkflow` equals the Primary Workflow column, and the Entry Phase is a phase
  the workflow's Phase Model declares. `runtime/verify_self_hosting.py` asserts all three.
- Required Inputs are the input types the run is submitted with. Submitting fewer is not a
  routing shortcut: the owning agent's input contract narrows what it reads, and a missing
  required type blocks the entry phase at `G5-INPUT` with a recorded reason. The
  `structure-preserving-change` row lists `business-intent` for exactly that reason: the first
  run submitted under this profile without it blocked at `G5-INPUT`, because the architect input
  contract requires it in both workflows the agent serves.
- Where the routed workflow cannot execute a phase, the phase blocks with a recorded reason and
  the operator executes it against the run, per `## Completion Rule`. Blocking is a recorded
  state, never a silent skip, and never grounds for leaving the framework command behind.

## Evidence Rule

A change proposal is the governance record of a framework change. It is authored from
`templates/framework-change-proposal.md`, stored under `proposals/`, and validated by
`runtime/change_proposal_validator.py`. It must link the run artifacts the framework itself
produced; prose describing a run that no artifact supports is rejected.

| Evidence ID | Required link | Why it is required |
|---|---|---|
| `E-1` | `runs/<run-id>/execution-request.json` | Proves which command and workflow the change was routed through, and under which inputs |
| `E-2` | `runs/<run-id>/run-ledger.json` | Proves the run is the framework's own record rather than a directory named like one |
| `E-3` | `runs/<run-id>/events.jsonl` | Carries the ordered canonical events of the run |
| `E-4` | `runs/<run-id>/state.json` | Carries per-phase disposition, including every blocked phase and its reason |
| `E-5` | `runs/<run-id>/completion-package.md` | The aggregated run outcome the proposal summarises |
| `E-6` | At least one `runs/<run-id>/states/<phase>/artifacts/<artifact>` | Proves a phase executed and produced a contracted artifact |
| `E-7` | The validation report of every executed phase | Proves the Validation Engine accepted each artifact rather than the author asserting it |

Every link is checked for resolution against the filesystem, and `E-1` is checked for agreement:
the run's `command_id` must equal the command this profile routes the declared change class to. A
proposal linking a real run reached by a different command is rejected, because it would document
self-hosting that did not happen.

`E-6` and `E-7` are required **where the routed workflow has at least one dispatchable phase**.
All seven workflows this profile routes now have one, so the condition holds everywhere and both
links are mandatory for every change class. The conditional wording is kept because the condition,
not the current capability set, is the rule: it was written when four of the seven had no
dispatchable phase and a run of one produced no artifact for any author to link, however diligent
— requiring the link anyway would have made those four classes unroutable to completion, which is
how the rule was first written and what the first `defect-repair` change routed under it
discovered. The waiver below is now unreachable in practice, and reaching it again would mean a
capability regression.

Where the routed workflow has no dispatchable phase, `E-6` and `E-7` are satisfied by the run's
own record that this is so: every phase blocked with a recorded reason in `E-4`, and the aggregated
statement of it in `E-5`. The condition is not the author's to assert.
`runtime/change_proposal_validator.py` decides it by resolving each phase of the routed workflow
through the runtime's own capability chain, exactly as `verify_registry_coverage.py` does, so a
proposal cannot claim a workflow was undispatchable when it was not.

## Completion Rule

A framework change is **completed in self-hosting mode** when all of the following hold. This is
what `runtime/verify_self_hosting.py` decides, so the term carries one meaning.

| ID | Condition |
|---|---|
| `C-1` | The change was classified by `## Scope Rule` and routed by `## Routing Table` before the work began |
| `C-2` | A run exists whose `command_id` is the routed command and whose `workflow_id` is that command's primary workflow |
| `C-3` | Every phase of the routed workflow is enqueued, and each is either executed with a validated artifact or blocked with a recorded reason |
| `C-4` | Every executed phase was validated by its registered validator and accepted by the runtime |
| `C-5` | The run carries an aggregated completion package |
| `C-6` | A change proposal exists that satisfies `## Evidence Rule` and passes `change_proposal_validator.py` |
| `C-7` | Every mandatory item of `validation/framework-release-checklist.md` is recorded with its result |

`C-3` is the honest half of the rule, and it is written as a disjunction on purpose. Every one of
the thirty-six phases is dispatchable today, so a phase that blocks now does so for a reason other
than a missing capability — an undecided gate ahead of it, a terminally failed predecessor, or an
artifact its validator rejected. Where a phase does block, work for it is performed by the operator
against the run and recorded in the proposal's Phase Disposition section, naming the phase, the
blocked reason the runtime recorded, and who performed it. That path stays in the profile because
`C-3` describes what a run must account for, not what the current capability set happens to be:
self-hosting means the framework decides the route, holds the gates, and keeps the record.

## Point-in-Time Evidence Rule

### Decision `GD-001`

| Field | Value |
|---|---|
| Decision ID | `GD-001` |
| Title | Change proposals and their governance evidence are point-in-time records, not live assertions |
| Date | 2026-08-19 |
| Status | Accepted |
| Decided by | `tech-lead` (accepting owner), recorded by operator |
| Arises from | Reconciliation finding `A-1`, `reports/wave-1-reconciliation-report-2026-08-19.md` |

### Context

A change proposal states what a run did and what the framework could do at the moment the
change was authored. Before this decision, `change_proposal_validator.py` resolved several of
those statements against the framework **as it is now**: `F6` compared each phase disposition
the proposal recorded against the current `state.json`, and `F12` resolved whether the routed
workflow had a dispatchable phase through the current capability chain.

That was sound while the framework was static. It stopped being sound the moment capability
grew. The Wave 1 rollout took dispatchable phases from 3 to 29, which:

- flipped `run-3e22f11cb34d` phases from `blocked` to `pending`, because the scheduler clears
  a guard-raised block as soon as the guard passes, so `FC-001`'s accurate record of a blocked
  phase began failing `F6`;
- removed the no-dispatchable-phase carve-out that `FC-002` had correctly relied on for `E-6`
  and `E-7`, so an accurate proposal began failing `F12`.

Both proposals described the world correctly when written. The world moved. Nothing about
either change became wrong.

The same defect appears outside proposal validation. `verify_vertical_slice.py` check `C6`
failed a historical run because the agent's module digests no longer match those frozen in the
run's invocation envelope — a correct observation of current-state divergence, reported as a
failure of a past proof.

### Decision

Governance evidence is evaluated **against the baseline it recorded**, never against
current state.

1. A change proposal declares an **authoring-time baseline**: the runtime version, the
   dispatchable and blocked phase counts, and the per-phase dispositions of the run it
   documents, as they stood when the proposal was authored.
2. `F6` compares recorded phase dispositions against that baseline. Where a proposal predates
   this decision and carries no baseline, `F6` reconstructs it from the run's own transition
   log, which is append-only, rather than from current `state.json`.
3. `F12` resolves dispatchability against the baseline's recorded counts, not the live
   capability chain.
4. A historical record is **never amended** because the runtime evolved. Divergence between a
   recorded baseline and current state is reported as **informational drift**, not as failure.
5. Current-state verification remains the job of `verify_registry_coverage.py`,
   `verify_validators.py`, and `verify_recovery.py`. Those report what the framework can do
   now, and they are the only authority on that question.

### Consequences

- `FC-001` and `FC-002` remain valid historical records, unamended.
- A proposal authored after this decision carries its own baseline, so it stays checkable
  forever without reference to a moving framework.
- The separation is explicit: proposals answer "what happened", verifiers answer "what is true
  now". Neither is evidence for the other's question.
- A reader comparing a baseline to current state sees drift reported, which is the intended
  signal that capability grew.

## Bootstrap Exception

This profile could not be routed by itself: it did not exist when the increment that authored it
began. That increment is recorded as the bootstrap rather than as a self-hosted change, in
`reports/self-hosting-operating-mode-report-2026-08-18.md`. It is the only such exception, it
applies to no later change, and no framework change after it is exempt from `## Completion Rule`.

## Precedence

- Where this profile and a routed workflow's Phase Model disagree about ownership, the Phase Model
  governs. This profile selects the route; it does not own what happens inside it.
- Where this profile and `config/agent-routing.md` disagree about the entry command for an intent,
  this profile governs for framework-internal changes only. `agent-routing.md` remains the
  authority for every change outside `## Scope Rule`.
- Where a required evidence link cannot be produced because a phase blocked, the block is recorded
  under `C-3`. The evidence requirement is not waived; the run's own record satisfies it.

## Recorded Framework Changes

Every change proposal accepted under this profile, with the run that carried it. `## Evidence
Rule` requires each proposal to link its run artifacts; this index records which run that is, so
a reader can reach the evidence without opening every proposal to find out. It is a directory,
not a second authority: `runtime/verify_self_hosting.py` discovers proposals by globbing
`proposals/`, and a row here neither admits a proposal nor exempts one.

| Proposal | Change Class | Routed Command | Run | Run Evidence |
|---|---|---|---|---|
| `FC-001` | `capability-addition` | `/implement` | `run-3e22f11cb34d` | `runs/run-3e22f11cb34d/` |
| `FC-002` | `defect-repair` | `/bugfix` | `run-c9bdf5dbca7a` | `runs/run-c9bdf5dbca7a/` |
| `FC-003` | `structure-preserving-change` | `/refactor` | `run-0db4765d0eab` | `runs/run-0db4765d0eab/` |
| `FC-004` | `capability-addition` | `/implement` | `run-93b302cbdb28` | `runs/run-93b302cbdb28/` |
| `FC-005` | `defect-repair` | `/bugfix` | `run-27e36c138498` | `runs/run-27e36c138498/` |
| `FC-006` | `capability-addition` | `/implement` | `run-09099de97613` | `runs/run-09099de97613/` |
| `FC-015` | `capability-addition` | `/implement` | `run-ae91e085f481` | `runs/run-ae91e085f481/` |
