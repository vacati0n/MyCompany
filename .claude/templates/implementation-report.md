# Template: Implementation Report

## Usage

Canonical artifact template for `implementation-report.md`, produced by agent
`dev-1-implement` in three phases that emit the same artifact type:

| Workflow | Phase |
|---|---|
| `implement-feature` | `implementation` |
| `fix-bug` | `fix-implementation` |
| `refactor` | `refactor-implementation` |

Template version 1.0.0.

The report is the machine-checkable record of a change that was made: what changed, which
design element each change serves, what was executed to verify it, what was deliberately
not done, and what the reviewer must look at. It is not a substitute for the diff. The diff
is the change; this artifact is the evidence and the accounting that travels with it to the
Fix Gate, the Implementation Gate, and the implement-feature Review Gate.

Section titles, section order, and identifier schemes are fixed. Sections are never omitted.
A section with nothing to report reads `None identified.`

Identifier schemes: change-set entries `C-nnn`, test evidence `T-nnn`, deviations `V-nnn`,
residual risks `R-nnn`, open questions `Q-nnn`. All zero-padded to three digits and
ascending from 001.

The binding behavioural contract is the module set under `agents/dev-1-implement/`, and
`agents/dev-1-implement/output.md` governs where this template and that module disagree.
Four of its rules are enforced mechanically and are worth restating here: every change-set
entry carries test evidence, a deviation from the accepted design is recorded rather than
taken silently, a report claiming completion carries no failing verification, and the
producing agent never records its own review verdict.

---

```yaml
implementationReport:
  reportId:
  changeReference:
  sourceInputs:
    - type:          # technical-design | bug-analysis | validation-report | coding-standards | execution-plan
      reference:     # supplied reference, or "inline"
  producedBy: dev-1-implement
  agentVersion:
  schemaVersion: 1.0.0
  status:            # complete | provisional | blocked
  workflowPhase:     # implementation | fix-implementation | refactor-implementation
  verificationStatus: # verified | partially-verified | unverified
  inputDigest:
  contextDigest:
```

## Metadata

- Report ID:
- Change reference:
- Workflow phase:               <!-- implementation | fix-implementation | refactor-implementation; equals the metadata block -->
- Status:                       <!-- complete | provisional | blocked; equals the metadata block -->
- Verification status:          <!-- verified | partially-verified | unverified; equals the metadata block -->
- Review status:                <!-- always pending-review; this agent does not review its own work -->

## Implementation Summary

- Change intent:                <!-- what the change makes true that was not true before -->
- Approach taken:               <!-- the implementation route actually used -->
- Design reference:             <!-- the accepted design element set this change implements -->
- Out of scope:                 <!-- what was deliberately not changed, and why that is safe -->

## Change Set

<!-- One row per file the change touched. Design Ref names the design element, decision
record, plan task, or analysis step the change serves; a change that serves none is a
deviation and belongs in Deviations and Tradeoffs as well. -->

| ID | Path | Change Type | Purpose | Design Ref |
|---|---|---|---|---|
| `C-001` | | | | |

## Test Evidence

<!-- One row per automated check that exercises this change. Covers cites the change-set
identifiers the test exercises; every change-set identifier appears in at least one Covers
cell. Type is unit | integration | contract | regression | end-to-end | static. Result is
pass | fail | not-run. -->

| ID | Test | Type | Covers | Command | Result |
|---|---|---|---|---|---|
| `T-001` | | | `C-001` | | |

## Verification Results

- Verification method:          <!-- how the evidence above was produced -->
- Commands executed:            <!-- the exact commands, inline; never a fenced block -->
- Result summary:               <!-- counts: executed, passed, failed -->
- Unverified areas:             <!-- what the executed evidence does not reach -->

## Deviations and Tradeoffs

<!-- Every departure from the accepted design, standard, or plan, and every tradeoff taken
knowingly. Escalation names the record raised for it, or `not-required` when the deviation
is inside this agent's authority. A design change is never taken silently. -->

| ID | Deviation | Design element | Rationale | Escalation |
|---|---|---|---|---|

## Boundary Compliance

- Module boundaries preserved:  <!-- which boundaries the change respected, and how -->
- Public interface changes:     <!-- signature, schema, or contract changes, or none -->
- Data or migration impact:     <!-- schema, migration, and backfill effects, or none -->
- Declared side effects:        <!-- every write this invocation made -->

## Residual Risk

<!-- What remains risky after the change, for the reviewer and QA to weigh. -->

| ID | Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|---|

## Handoff Notes

- Reviewer focus areas:         <!-- where independent review is most valuable -->
- Follow-up work:               <!-- work this change makes possible or necessary -->
- Documentation impact:         <!-- what documentation the change invalidates or requires -->

## Open Questions

<!-- Appendix. Required whenever status is provisional or blocked, whenever verification
status is not verified, and whenever a deviation was escalated. -->

| ID | Question | Blocking | Owner | Affected changes |
|---|---|---|---|---|
