# Superseded Shared Contract Modules

## Objective

Record the shared agent-contract documents that the runtime module-set architecture
superseded, the normative surface that now carries each obligation, and how remaining
references resolve.

This register is the authoritative resolution for `agents/agent-contract.md` and
`agents/agent-lifecycle.md`. It exists for the same reason `agents/retired-roles.md` exists:
a reader or a runtime path that meets one of these names must find a decision, not a missing
file.

## Supersession Decision — `SC-001`

| Field | Value |
|---|---|
| Decision ID | `SC-001` |
| Title | Shared agent-contract and agent-lifecycle modules superseded by the runtime module-set architecture |
| Date | 2026-08-19 |
| Status | Accepted |
| Decided by | `tech-lead` (accepting owner), recorded by operator |
| Arises from | Reconciliation finding `M-1`, `reports/wave-1-reconciliation-report-2026-08-19.md` |
| Related | `agents/retired-roles.md`, ADR `D-001` and `D-002` of `run-93b302cbdb28` |

### Context

Two shared documents carried the agent contract baseline before the runtime module set
existed:

- `agents/agent-contract.md` — the Standard Agent Contract every agent satisfied: required
  properties, input and output contracts, error classes, escalation obligations.
- `agents/agent-lifecycle.md` — lifecycle states, internal gates, retry behaviour, and the
  escalation path an invocation moves through.

Both applied to **single-file** agents, whose one `agents/<id>.md` specification could not
carry that material itself. Twelve agents have since migrated to runtime module sets, in which
the same obligations are carried by named modules with declared roles:

| Obligation | Was | Now |
|---|---|---|
| Normative contract shape every agent satisfies | `agents/agent-contract.md` | `domain-model/agent-specification.md` |
| This agent's implementation of that contract | `agents/agent-contract.md` + `agents/<id>.md` | `agents/<id>/identity.md`, module role `agent-contract` |
| Lifecycle states, gates, retry, escalation | `agents/agent-lifecycle.md` | `agents/<id>/execution.md`, module role `execution-lifecycle` |
| Operating charter and invariants | `agents/agent-contract.md` | `agents/<id>/system.md`, module role `operating-charter` |

The migration removed the two shared files. It did not update the references to them, which is
the defect this decision closes. No content was lost: `identity.md` carries the full contract
section set per agent, and `execution.md` carries the lifecycle.

### Decision

`agents/agent-contract.md` and `agents/agent-lifecycle.md` are **superseded, not restored**.

1. The shared normative baseline is `domain-model/agent-specification.md`. Every manifest
   already names it as `metadata.specificationRef`; that field is the single shared reference.
2. A manifest's `metadata.contractRef` names `identity.md` — the module whose declared role is
   `agent-contract` and which implements the contract for that agent.
3. A manifest's `metadata.lifecycleRef` names `execution.md` — the module whose declared role
   is `execution-lifecycle`.
4. A host entry point's bootstrap procedure loads the agent's own module set in the declared
   `loadOrder`, rather than two shared files.
5. Neither superseded path is recreated. A reference to either resolves here.

### Rationale

Reconstructing the shared files would reintroduce two sources of truth for material the module
set already carries per agent, which is the duplication the module-set architecture removed.
The three-point alternative — restore, keep both, deprecate later — was rejected because a
restored file with no reader is a document that drifts silently.

Pointing `contractRef` and `lifecycleRef` at module-set members rather than at
`domain-model/agent-specification.md` keeps the two fields meaningful: `specificationRef`
answers "what contract shape does this agent satisfy", `contractRef` answers "where is this
agent's implementation of it", and `lifecycleRef` answers "where is its lifecycle". Collapsing
all three onto the specification would make two of the fields redundant.

### Scope of impact

45 files carried a reference. All are migrated by this decision:

- 12 `manifest.yaml` — `contractRef` and `lifecycleRef` retargeted
- 12 `identity.md` — the "Implements" line retargeted to the specification
- 12 `execution.md` — the `contractVersion` verification retargeted to the specification
- 3 `system.md` — the governing-document statement retargeted
- 2 `*.agent.md` — bootstrap step retargeted to the module set
- `agents/README.md` — the legacy single-file guidance retargeted
- `runtime/artifact_contract.py`, `runtime/artifact_lib.py` — check reference strings
- `runtime/fixtures/review-package.md` — a finding row's requirement reference

### Consequences

- Positive: no dangling normative reference; one shared baseline; each field carries distinct
  meaning.
- Negative: a reader who knows the old paths must follow one indirection through this register.
- Neutral: no runtime behaviour changes. `framework_runtime.py` never resolved
  `metadata.contractRef` or `metadata.lifecycleRef`; only an output's `contractRef` is
  resolved, and that field is unaffected.

### Validation

- No file under `.claude/` outside `runs/`, `reports/`, and this register references either
  superseded path.
- `verify_registry_coverage.py`, `verify_validators.py`, and `verify_vertical_slice.py` hold
  their verdicts.
- Every migrated `contractRef` and `lifecycleRef` resolves to a file on disk that the
  manifest's own `loadOrder` declares.

## Reference resolution

| Superseded path | Resolve to |
|---|---|
| `agents/agent-contract.md` | `domain-model/agent-specification.md` for the normative shape; `agents/<id>/identity.md` for a given agent's implementation |
| `agents/agent-lifecycle.md` | `agents/<id>/execution.md` |
