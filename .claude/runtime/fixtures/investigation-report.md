```yaml
investigation:
  investigationId: INV-2026-0017
  decisionReference: DEC-0093
  sourceInputs:
    - type: investigation-question
      reference: inline
    - type: context-sources
      reference: inline
  producedBy: context-agent
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  confidence: medium
  inputDigest: sha256:fixture-input-not-run-produced
  contextDigest: sha256:fixture-context-not-run-produced
```

## Metadata

- Investigation ID: INV-2026-0017
- Owner: context agent
- Requested by: tech lead
- Decision deadline: 2026-08-25

## Objective

- Decision to support: whether lease expiry should be enforced by a timer or left as an explicit operator action.
- Key question: what actually stalls a run today when an adapter stops reporting, and how long does it stall for?
- Scope boundaries: the queue control path only; validation, gates, and aggregation are out of scope.

## Context

- Relevant systems and components: the task queue, the state engine, and the recovery path that reclaims a lease.
- Related incidents or prior findings: one prior stall recorded against a run whose adapter never reported.
- Constraints and assumptions: no background scheduler exists, so anything time-triggered needs a process that does not exist yet.

## Evidence

| ID | Source | Observation | Confidence | Staleness |
|---|---|---|---|---|
| `E-001` | `config/task-queue.md` lease model | lease expiry is specified to trigger recovery classification, not failure | high | current |
| `E-002` | `runtime/state_engine.py` work item fields | `lease_expires_at` is carried on every work item but never set | high | current |
| `E-003` | `runs/` transition logs | every reclaim recorded so far was an explicit operator action | high | current |
| `E-004` | prior stall record | the one observed stall lasted until an operator noticed it, with no upper bound | medium | as of 2026-08-11 |

## Options Evaluated

| ID | Option | Benefits | Risks | Effort | Evidence |
|---|---|---|---|---|---|
| `O-001` | keep reclaim explicit and operator-driven | no new process, and every recovery stays attributable to a person | an unattended run stalls without bound | none, already built | `E-002`, `E-003` |
| `O-002` | set `lease_expires_at` at dispatch and enforce it on the next runtime invocation | bounded stalls with no background process, because enforcement rides existing commands | a run nobody touches is never re-evaluated, so the bound only holds under use | small | `E-001`, `E-002` |
| `O-003` | run a background scheduler that sweeps expired leases | true time bound independent of operator action | introduces a process the runtime does not have, and with it liveness and concurrency concerns | large | `E-001`, `E-004` |

## Recommendation

- Recommended option: `O-002`
- Rationale: it bounds the stall the evidence actually records without adding a process, and it degrades to the current behaviour rather than to a new failure mode when nobody is driving the run.
- Preconditions: dispatch must write an expiry, and every command that reads run state must evaluate it.
- Risks requiring monitoring: leases that expire while the adapter is still working, which would reclaim work that was about to report.

## Next Steps

- Immediate actions: record the expiry at dispatch time and evaluate it wherever guards are already re-evaluated.
- Decision owner and deadline: tech lead, by 2026-08-25.
- Follow-up validation: confirm that a reclaimed-then-reported adapter is refused rather than accepted twice.

## Contradictions and Gaps

- `config/task-queue.md` specifies automatic expiry while `runtime/README.md` records that no timer exists. Both are accurate statements about different things: one is the target, one is the current state. The gap is real and this report does not close it.
- No evidence establishes a distribution of adapter runtimes, so no expiry value is derivable from what was observed. Choosing one is a policy decision, not a finding.

## Open Questions

None identified.
