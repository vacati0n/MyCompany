```yaml
technicalRecommendation:
  recommendationId: TL-2026-0044
  decisionReference: INV-4417
  decisionBasis: recommendation
  sourceInputs:
    - type: investigation-report
      reference: inline
    - type: workflow-constraints
      reference: inline
    - type: scope-definition
      reference: inline
  producedBy: tech-lead
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  recommendedOption: O-002
  readinessDecision: proceed-with-conditions
  inputDigest: sha256:fixture-input-not-run-produced
  contextDigest: sha256:fixture-context-not-run-produced
```

## Metadata

- Recommendation ID: TL-2026-0044
- Decision owner: orchestrator
- Requested by: orchestrator, on behalf of the run that opened INV-4417
- Decision date: 2026-08-19

## Decision Context

- Decision to make: whether to bound the retry path now, rebuild the recovery controller, or carry the stranding defect into the next release.
- Delivery constraints: the release date is fixed and stated as such; one downstream consumer depends on the current recovery interface for this cycle; no additional capacity is available before the branch is cut.
- Assumptions in force: the failure class established by the investigation is the only one that strands a run, and the downstream consumer's dependency is unchanged for the remainder of the cycle. Both would change the answer if false, and the second is tracked as an open question upstream.

## Evaluation Criteria

| ID | Criterion | Why it matters | Priority | Source |
|---|---|---|---|---|
| `EC-001` | No run is stranded under any established failure class | This is the defect the investigation was opened over | must-have | investigation-report INV-4417 |
| `EC-002` | The current recovery interface is preserved this cycle | A downstream consumer depends on it and has not planned a migration | must-have | workflow constraints |
| `EC-003` | Deliverable before the fixed release date | The date is recorded as fixed, not preferred | high | workflow constraints |
| `EC-004` | The change can be unwound inside one run | A recovery path is where an error compounds fastest | medium | delivery principle applied by this role |

## Options

| ID | Option | Summary | Effort | Delivery risk | Reversibility | Evidence |
|---|---|---|---|---|---|---|
| `O-001` | Carry the defect | Leave the retry path unchanged and record the defect against the next cycle | trivial | high | reversible | investigation-report INV-4417 evidence E-003 |
| `O-002` | Bound the retry and classify the exhausted case | Add a terminal classification for the exhausted budget and a bounded delay, inside the current interface | small | low | reversible | investigation-report INV-4417 evidence E-001 and E-004 |
| `O-003` | Rebuild the recovery controller | Replace classification and dispatch together, resolving the class of defect rather than the instance | large | high | costly-to-reverse | investigation-report INV-4417 evidence E-002 |

## Tradeoff Analysis

| Option | Criteria met | Criteria missed | Strengths | Weaknesses | Sequencing implication |
|---|---|---|---|---|---|
| `O-001` | `EC-002`, `EC-003`, `EC-004` | `EC-001` | Costs nothing in this cycle and disturbs no consumer | Leaves the stranding defect live, which is the must-have the investigation was opened over | None; the defect travels into the release and the recovery backlog grows |
| `O-002` | `EC-001`, `EC-002`, `EC-003`, `EC-004` | None. | Closes the defect without touching the interface a consumer depends on | Leaves the wider classification design untouched, so the class of defect can recur elsewhere | Must land before the release branch is cut, ahead of candidate validation |
| `O-003` | `EC-001`, `EC-004` | `EC-002`, `EC-003` | Resolves the class of defect rather than this instance of it | Breaks the interface a downstream consumer depends on this cycle, at large effort | Pushes past the fixed date and forces a downstream migration to be scheduled first |

## Risk and Blocker Register

| ID | Risk or blocker | Severity | Likelihood | Delivery impact | Mitigation | Owner | Status |
|---|---|---|---|---|---|---|---|
| `RK-001` | The bounded delay is unverified against the exhausted-budget path | high | certain | An incorrect bound strands runs in a second way rather than the first | Extend the recovery proof to the exhausted-budget path and re-run it before the branch is cut | qa | open |
| `RK-002` | The classification recorded on exhaustion is consumed by one downstream reader that was not surveyed | medium | possible | A consumer reading the old classification would mis-route a recovered run | Survey the consumers of the classification field and record the result against the change | context-agent | mitigated |

## Assessment Summary

- Criteria applied: 4
- Options evaluated: 3
- Risks and blockers recorded: 2
- Blocking items open: 1

## Recommendation

- Recommended option: `O-002`
- Rationale: `O-002` is the only option that meets both must-have criteria, closing `EC-001` without breaking `EC-002`, and it does so at small effort well inside `EC-003`; `O-001` misses `EC-001` outright, and `O-003` misses `EC-002` and `EC-003` together.
- Preconditions: `RK-001` reaches `mitigated` on re-run evidence before the release branch is cut.
- Options rejected: `O-001`, because it misses `EC-001`, the criterion the investigation was opened over, and defers the cost rather than removing it; `O-003`, because it misses `EC-002` and `EC-003`, and the interface break lands on a consumer that has not planned a migration.

## Delivery Impact

- Effort and capacity: small, absorbed inside the current cycle by the implementation role already holding the recovery path; no additional capacity is required.
- Sequencing constraints: must land before the release branch is cut and before candidate validation runs, because the validation reads the classification this change writes.
- Dependencies: the recovery proof, which supplies the evidence `RK-001` is closed on, and the downstream consumer survey named in `RK-002`.
- Reversal plan: reversible within one run by restoring the prior bound and removing the terminal classification; no persisted state carries the new classification forward.

## Readiness

- Recommended decision: proceed-with-conditions
- Conditions to satisfy: `RK-001` mitigated on re-run evidence and recorded before the release branch is cut.
- Blocking items outstanding: `RK-001`
- Deciding authority: orchestrator

## Open Questions

None identified.
