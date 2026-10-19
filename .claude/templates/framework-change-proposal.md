# Template: Framework Change Proposal

## Usage

Canonical artifact template for `framework-change-proposal.md`, the governance record of one
framework-internal change routed by `config/self-hosting-profile.md`. Instances live under
`proposals/` and are named `framework-change-proposal-<proposal-id>.md`.

A proposal is not a description of a change. It is the evidence that the change was routed,
executed, and gated by the framework itself: every claim it makes about a run is a link the
Validation Engine resolves against the run the runtime wrote. `runtime/change_proposal_validator.py`
rejects a proposal whose links do not resolve, whose run was reached by a different command than
the profile routes, whose phase disposition does not cover the routed workflow, or whose release
checklist result omits a mandatory item.

Template version 1.0.0. Section titles, section order, and identifier schemes are fixed. Sections
are never omitted. A section with nothing to report reads `None identified.` where the contract
permits it; `Run Evidence`, `Phase Disposition`, and `Release Checklist Result` do not permit it,
because a change with no evidence is the thing this artifact exists to prevent.

Identifier scheme: open items `O-nnn`, zero-padded to three digits and ascending.

The binding governance contract is `config/self-hosting-profile.md`. Its Evidence Rule fixes the
`E-n` rows of `Run Evidence`, and its Completion Rule fixes what `Phase Disposition` must account
for.

---

```yaml
frameworkChangeProposal:
  proposalId:
  changeClass:       # a change class declared by the profile Routing Table
  routedCommand:     # the command the profile routes that class to, without the leading slash
  routedWorkflow:    # that command's primaryWorkflow
  runId:             # the run carrying this change
  profileRef: config/self-hosting-profile.md
  profileVersion: 1.0.0
  producedBy:        # orchestrator | operator
  agentVersion:
  schemaVersion: 1.0.0
  status:            # complete | provisional | blocked
```

## Metadata

- Proposal identifier:            <!-- equals the metadata block -->
- Change title:
- Change class:                   <!-- equals the metadata block -->
- Routed command:
- Run identifier:                 <!-- equals the metadata block -->
- Authored on:

## Authoring Baseline

<!-- The framework state this proposal was authored against, per `GD-001` in
`config/self-hosting-profile.md`. A proposal is a point-in-time record: every claim it makes
about run status and workflow dispatchability is judged against this baseline, never against
current state. A proposal authored before `GD-001` carries no baseline, and the validator
reconstructs one from the run's append-only transition log. Later capability growth is
reported as informational drift, and never invalidates a record that was accurate when
written. -->

- Authored at:                    <!-- ISO 8601 UTC; the instant the claims below were true -->
- Runtime version:
- Dispatchable phases framework-wide:   <!-- N of M, at the instant above -->
- Routed workflow dispatchable phases:  <!-- phase identifiers, or `None identified.` -->

## Change Statement

- Objective:
- In scope:
- Out of scope:
- Acceptance basis:

## Scope Classification

<!-- One row per representative path the change touches. Scope Rule is the rule identifier from
the profile that decides that path. The validator recomputes every row against the profile. -->

| Path | Scope Rule | Decision | Note |
|---|---|---|---|
| | | | |

## Routing Decision

- Change class:
- Selector satisfied by:
- Command:
- Primary workflow:
- Entry phase:
- Required inputs supplied:

## Run Evidence

<!-- One row per Evidence ID in the profile Evidence Rule. Link is repository-relative and must
resolve. Status records what the link shows. -->

| Evidence ID | Link | Status |
|---|---|---|
| `E-1` | | |

## Phase Disposition

<!-- Every phase of the routed workflow, in Phase Model order. Runtime Status is what state.json
records. Disposition is what actually happened, including the recorded blocked reason where the
phase blocked. Performed By names the agent or the operator. -->

| Phase | Owner Agent | Runtime Status | Disposition | Performed By | Evidence |
|---|---|---|---|---|---|
| | | | | | |

## Gate Decisions

| Gate | Decision | Owner Role | Decided By | Rationale |
|---|---|---|---|---|
| | | | | |

## Verification

<!-- One row per check run against the delivered change. Command is the exact command, or
`inspection` where the check is not machine-checkable. -->

| Check | Command | Result |
|---|---|---|
| | | |

## Risk and Rollback

- Blast radius:
- Risk assessment:
- Rollback procedure:

## Release Checklist Result

<!-- One row per item of validation/framework-release-checklist.md. Every mandatory item appears,
whatever its result. -->

| Item | Requirement | Result | Evidence |
|---|---|---|---|
| | | | |

## Open Items

| ID | Item | Owner | Disposition |
|---|---|---|---|
| `O-001` | | | |

## Sign-off

- Proposed by:
- Accepted by:
- Acceptance basis:
