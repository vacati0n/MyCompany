```yaml
releaseNote:
  releaseId: REL-2026-0033
  version: 0.4.0
  sourceInputs:
    - type: deployment-status
      reference: inline
    - type: final-change-summary
      reference: inline
  producedBy: documentation
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  releaseVerdict: released
  inputDigest: sha256:fixture-input-not-run-produced
  contextDigest: sha256:fixture-context-not-run-produced
```

## Metadata

- Version: 0.4.0
- Release date and time: 2026-08-18 09:00 UTC
- Environment: shared runtime host
- Release owner: orchestrator

## Highlights

- Feature additions: validator coverage for every core emitted artifact type, and a classified retry policy with bounded attempts.
- Bug fixes: a reclaimed work item no longer carries a stale lease into its next dispatch.
- Improvements: every blocked transition now emits a structured failure envelope naming the condition, the owner, and the action that clears it.

## Technical Changes and Compatibility

- API or contract changes: the persisted work item gains `available_at` and `retrying` as a status; the run directory gains `recovery-ledger.json`.
- Database or migration impact: none; run state is file-backed and a run written before this release is read unchanged.
- Configuration changes: none required; the retry profile defaults to the policy in `config/runtime.md`.
- Backward compatibility notes: a run created before this release has no `available_at` field and is treated as immediately available, so prior runs remain readable and completable.

## Operational Notes

- Deployment considerations: no process to restart; the runtime is invoked per command.
- Monitoring and alerts: the recovery ledger is the signal to watch, since it records every classification and its chosen action.
- Rollback criteria: revert if any prior run fails to load, or if a retry is scheduled for a failure class the policy marks non-retryable.

## Validation Summary

- Test status: the vertical-slice, multi-phase, registry-coverage, validator, and recovery proofs all report PASS.
- Known risk acceptance: retry scheduling is evaluated on command invocation rather than by a timer, so an unattended run is not advanced.
- Post-release checks: confirm the recovery ledger records one entry per classification and no duplicates.

## Known Issues

| ID | Issue | Impact | Workaround | Tracking |
|---|---|---|---|---|
| `K-001` | a scheduled retry only becomes dispatchable when a runtime command is next invoked | an unattended run does not advance on its own | invoke `next` on the run | recorded in `runtime/README.md` known gaps |

## Communication

- Stakeholders notified: tech lead, orchestrator, and the reviewer who holds the release gate.
- Support handoff notes: a phase reported as `retrying` is not stuck; its work item carries the timestamp at which it becomes dispatchable.
