```yaml
bugAnalysis:
  analysisId: BA-2026-0041
  defectReference: DEF-4471
  sourceInputs:
    - type: defect-report
      reference: inline
    - type: logs
      reference: inline
  producedBy: dev-1-bug-analyst
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  severity: high
  reproducibility: deterministic
  inputDigest: sha256:fixture-input-not-run-produced
  contextDigest: sha256:fixture-context-not-run-produced
```

## Metadata

- Bug ID: DEF-4471
- Reporter: platform on-call rotation
- Severity: high
- Status: complete

## Symptom Summary

- Observed behavior: a work item reclaimed after adapter loss is dispatched a second time while the first lease is still recorded as held.
- Expected behavior: reclaiming a work item clears the recorded lease before the item becomes dispatchable again.
- First observed date: 2026-08-11
- Affected environments: local runtime, shared runtime host

## Reproduction

- Preconditions: a run holding one leased state work item whose adapter has not reported.
- Steps to reproduce: lease the work item, abandon the adapter without writing a result envelope, then reclaim and dispatch the same phase twice in succession.
- Reproduction frequency: deterministic
- Evidence: see the register below.

### Evidence Register

| ID | Evidence | Source | Confidence |
|---|---|---|---|
| `E-001` | two `work_item_leased` events carrying the same attempt number | `events.jsonl` of the affected run | high |
| `E-002` | `lease_expires_at` still populated after the reclaim transition committed | `state.json` work item record | high |
| `E-003` | the second dispatch wrote a fresh invocation envelope over the first | per-phase state directory listing | medium |

## Impact Assessment

- User impact: an operator sees two dispatch instructions for one phase and cannot tell which one is authoritative.
- Business impact: recovery from adapter loss becomes manual, which removes the reliability the reclaim path exists to provide.
- Technical impact: two invocation envelopes exist for one attempt, so the audit trail no longer maps one attempt to one envelope.
- Blast radius: every phase reclaimed after adapter loss, in every workflow, on any run that reaches a lease.

## Root Cause Analysis

- Root cause statement: the reclaim path returned the work item to a dispatchable status without clearing the lease fields it had set, so the next dispatch treated the stale lease as its own.
- Why detection failed earlier: no check compared the lease fields against the status they belong to, and every prior run reclaimed at most once.

### Causal Chain

| ID | Step | Claim | Evidence | Confidence |
|---|---|---|---|---|
| `C-001` | adapter loss | the adapter held a lease and never wrote a result envelope | `E-001` | high |
| `C-002` | reclaim | the status transition committed while `lease_expires_at` stayed populated | `E-002` | high |
| `C-003` | re-dispatch | the dispatch path read the stale lease as a live one and issued a second envelope | `E-002`, `E-003` | medium |

## Fix Strategy

- Proposed fix: clear the lease fields inside the same transition that returns the work item to a dispatchable status, so no intermediate state carries both.
- Alternative options: validate lease consistency at dispatch time instead, which detects the fault later and leaves the inconsistent state committed.
- Regression risk: medium
- Regression scope: the reclaim path, the dispatch idempotency check, and any replay detection that reads the lease fields.

## Validation Plan

- Verification steps: reclaim a lost work item, assert the lease fields are null, then dispatch and assert exactly one invocation envelope exists for the new attempt.
- Regression tests added: a negative check that refuses a dispatch against a work item carrying both a dispatchable status and a populated lease.
- Monitoring signals after release: count of dispatches whose work item carried a stale lease, expected to be zero.

## Closure

- Resolution summary: lease fields are cleared transactionally with the status change, and the inconsistent combination is now refused rather than tolerated.
- Linked PR and release: recorded by the closure package for this run.
- Prevention actions: the state engine treats lease fields as owned by the status, so a future path cannot set one without the other.

## Open Questions

None identified.
