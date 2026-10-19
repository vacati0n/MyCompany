# Framework Validation Checklist

## Phase 1: Folder Structure

- [ ] `.claude/agents`
- [ ] `.claude/skills`
- [ ] `.claude/workflows`
- [ ] `.claude/templates`
- [ ] `.claude/commands`
- [ ] `.claude/memory`
- [ ] `.claude/context`
- [ ] `.claude/config`
- [ ] `.claude/validation`
- [ ] `.claude/reports`

## Phase 2: Context

- [ ] product context exists
- [ ] technical context exists
- [ ] release context exists
- [ ] context map exists

## Phase 3: Agents

- [ ] agent specs exist for all active roles
- [ ] agent module readme exists
- [ ] routing catalog exists
- [ ] capability matrix exists and covers every registered agent
- [ ] every runtime agent declares a manifest with a resolvable load order
- [ ] every agent contract includes all mandatory contract sections exactly once
- [ ] every registered agent has a resolvable specificationPath

## Phase 4: Skills

- [ ] domain skill files exist
- [ ] agent-skill matrix exists
- [ ] skill governance exists
- [ ] skill catalog records a registry identifier and status for every Skill ID
- [ ] every skill referenced by a registered agent manifest resolves through the skill registry
- [ ] every registered skill has a resolvable specificationPath
- [ ] registry skill metadata matches the skill file it indexes

## Phase 5: Workflows

- [ ] core lifecycle workflows exist
- [ ] workflow module readme exists
- [ ] workflow gate matrix exists

## Phase 6: Templates

- [ ] template library exists
- [ ] template catalog exists

## Phase 7: Commands

- [ ] command specs exist
- [ ] command catalog exists

## Phase 8: Memory

- [ ] core memory categories exist
- [ ] memory governance exists

## Phase 9: Registries

- [ ] agent, workflow, skill, template, and command registries exist
- [ ] registry records validate against their recordSchema
- [ ] registry dependencies resolve to existing records
- [ ] no prohibited dependency cycle is present

## Phase 10: Validation and Reporting

- [ ] validation checklist exists
- [ ] dated validation report exists

## Phase 11: Executable Execution Validation

Phases 1 to 10 are specification checks, satisfied by inspection. This phase is different:
it is satisfied only by running the validator, and only against a real run.

Command:

```
python .claude/runtime/verify_vertical_slice.py --run-id <run-id>
```

Scope: Vertical Slice 1 only, `/implement` through `planner` to `execution-plan.md`. The
remaining phases, agents, and workflows are not covered and must not be inferred from a
pass here.

- [ ] C1 planner is registered in `registry/agents.yaml`
- [ ] C2 planner host registration exists and is host-compatible
- [ ] C3 planner module load order resolves from the manifest
- [ ] C4 planner manifest skills and phase-mandatory skills resolve
- [ ] C5 planner is invocable: the full routing chain resolves to a registered host entry point
- [ ] C6 planner executed: the run ledger records a completed real invocation
- [ ] C7 `execution-plan.md` was produced at the declared artifact path
- [ ] C8 the artifact conforms to the Planner output and quality contract
- [ ] C9 execution evidence exists and is complete
- [ ] C10 no Planner contract duplication exists

A check that cannot be evaluated against live run state fails. Nothing in this phase is
satisfied by a specification document alone.

## Phase 12: Runtime Baseline Freeze

Phase 11 proves one slice. This phase records the whole-framework baseline that was measured
after contract reconciliation, and states the bar a later change must clear. It is satisfied only
by running the six commands and comparing against the recorded numbers.

### Frozen baseline, measured 2026-08-19

| Measure | Value | Reported by |
|---|---|---|
| Active agents | 12 | `registry/agents.yaml`, `status: active` |
| Phase owners host-invocable | 12 of 12 | `verify_registry_coverage.py` `C3` |
| Declared phases | 36 across 7 active workflows | `C2` |
| Phases dispatchable | **36 of 36**, 0 blocked | `C6` |
| Phase-mandatory skill references resolving | 101 of 101 | `C4` |
| Gate references with a permitted decider | 28 of 28 | `C5` |
| Registered validators | 13 | `verify_validators.py` `V2` |
| Phase Model rows naming a decidable artifact | 36 of 36 | `V5` |
| Output Artifact reader agreement | `agree=36`, no drift | `V6` |
| Context slices declared | 36 of 36 phases | `framework_runtime.CONTEXT_SLICE_PHASE` |
| Rows recorded outstanding | 0 | `V5`, `OUTSTANDING_UNDECIDABLE_ROWS` |

### Acceptance criteria for a later change

- [ ] `python .claude/runtime/verify_registry_coverage.py` exits 0, `6/6 -- COVERED`, `C6` reports
      36 of 36 dispatchable and 0 blocked
- [ ] `python .claude/runtime/verify_validators.py` exits 0, `6/6 -- COVERED`, `V5` reports 0
      outstanding rows and `V6` reports `agree=36` with no `tokeniser-drift`
- [ ] `python .claude/runtime/verify_vertical_slice.py` exits 0, `10/10 -- PROVEN`
- [ ] `python .claude/runtime/verify_multi_phase.py` exits 0, `15/15 -- PROVEN`, event and
      transition counts unchanged and artifact digests unchanged
- [ ] `python .claude/runtime/verify_self_hosting.py` exits 0, `8/8 -- SELF-HOSTING`
- [ ] `python .claude/runtime/verify_recovery.py` exits 0, `41/41 -- RECOVERY PROVEN`
- [ ] A new or edited Phase Model row names exactly one `*.md` file in its Output Artifact cell.
      Two names or none both leave the phase undispatchable, and the two readers of that column
      agree only on the single-name case, per `V6`
- [ ] The artifact a row names is declared in the owning agent's manifest `outputs`, and has a
      registered validator in `framework_runtime.VALIDATORS`
- [ ] A new phase declares a context slice in `framework_runtime.CONTEXT_SLICE_PHASE`. Absent one,
      the phase clears `G1-CAPABILITY` and then blocks at `G2-CONTEXT`, which is the failure the
      reconciliation pass uncovered once the output contracts resolved
- [ ] A row corrected out of `OUTSTANDING_UNDECIDABLE_ROWS` is struck from that set in the same
      change. `V5` fails on a record that has outlived what it records, in both directions
- [ ] The exit code is read from the process, not from the tail of a pipe

### What this phase does not permit

A count in prose is not evidence. Where this file and a verifier disagree, the verifier decides
and this table is stale. Governance records under `proposals/` and dated files under `reports/`
are read against the baseline they carry, per `GD-001` in
`config/self-hosting-profile.md#point-in-time-evidence-rule`, and are never edited to match
current state.
