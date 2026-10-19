# Framework Release Checklist

## Purpose

The checklist a **framework update** must satisfy before it is published to its consumers.
`validation/framework-validation-checklist.md` asks whether the framework is well-formed. This
checklist asks a narrower question: whether *this* update may ship.

It is the checklist `config/self-hosting-profile.md` binds every framework-internal change to,
under Completion Rule `C-7`. Each routed change records its result for every mandatory item in
the Release Checklist Result section of its change proposal, and
`runtime/change_proposal_validator.py` check `F9` rejects a proposal that omits one.

## Scope

Applies to any change classified framework-internal by the profile's Scope Rule. It applies per
change, not per calendar release: the framework has no separate release train, so a framework
update is published the moment its change is accepted.

## Checklist Items

Machine-readable. `runtime/self_hosting.py checklist` parses this table, and
`runtime/verify_self_hosting.py --release-checklist` executes every item whose Verification is a
command and reports the result. `inspection` marks an item no command can decide; it is recorded
with the evidence the deciding role relied on, never with an unsupported assertion.

| ID | Requirement | Obligation | Owner Role | Verification |
|---|---|---|---|---|
| `FR-01` | Every active command resolves to an active workflow, every active workflow publishes a Phase Model, and every phase owner is host-invocable | mandatory | tech-lead | `python .claude/runtime/verify_registry_coverage.py` |
| `FR-02` | Every artifact type a Phase Model names has a registered validator, and every registered validator accepts a conforming artifact and rejects a mutated one | mandatory | qa | `python .claude/runtime/verify_validators.py` |
| `FR-03` | Failure classification, bounded retry, and failure envelopes still behave as the recovery policy declares | mandatory | qa | `python .claude/runtime/verify_recovery.py` |
| `FR-04` | Previously proven run evidence still verifies after the change, so no committed proof was invalidated | mandatory | qa | `python .claude/runtime/verify_vertical_slice.py --run-id run-c5a8d50d3238` |
| `FR-05` | The self-hosting profile resolves, the change-proposal contract is registered, and every recorded framework change validates against it | mandatory | orchestrator | `python .claude/runtime/verify_self_hosting.py` |
| `FR-06` | The change is carried by a run of the command the profile routes it to, with a change proposal linking that run's artifacts | mandatory | orchestrator | `python .claude/runtime/verify_self_hosting.py` |
| `FR-07` | The `RUNTIME_VERSION` constant in `runtime/framework_runtime.py` is raised when runtime behaviour changed, and left alone when it did not | mandatory | architect | inspection |
| `FR-08` | Documentation matches delivered behaviour: `runtime/README.md`, the folder descriptions in `README.md`, and any catalog the change touched | mandatory | documentation | inspection |
| `FR-09` | Every capability claim the change makes is backed by executable evidence under `runs/` or `reports/`, and every gap is recorded rather than omitted | mandatory | dev-2-reviewer | inspection |
| `FR-10` | Rollback is stated: what reverting this change requires, and what evidence would be invalidated by reverting it | mandatory | tech-lead | inspection |
| `FR-11` | The multi-phase state machine still proves out end to end | advisory | qa | `python .claude/runtime/verify_multi_phase.py --run-id run-c5a8d50d3238` |
| `FR-12` | A release note draft exists where the change alters behaviour a consumer of the framework depends on | advisory | documentation | inspection |
| `FR-13` | The lesson bundle conforms to OKF v0.2 §11 | advisory | qa | `python .claude/runtime/verify_okf_bundle.py` |

## Obligations

- **mandatory** — the change does not ship until the item passes, or until its failure is
  accepted at the gate that owns it, with the acceptance recorded in the change proposal.
- **advisory** — the item is run and recorded. A failure is a finding, not a stop.

`FR-11` is advisory for one recorded reason: `verify_multi_phase.py` builds its
illegal-transition probe in the system temp directory, which a sandboxed shell may refuse to
write. The check itself is not weaker than `FR-04`; its runnability is environment-dependent, and
an item that cannot be run everywhere must not silently become a blocker that is skipped.

`verify_self_hosting.py --release-checklist` runs every command item in its own disposable copy
of the working tree under the system temp directory, and its check `S11` fails if the repository,
or any item's copy, is changed by content. The checklist therefore never rewrites committed run
evidence, and running it needs a writable temp directory for every item, not only `FR-11`.

`FR-13` is advisory because the lesson bundle is evidence the framework derives, not a release
contract: a non-conforming bundle is a finding to repair with the next lesson write, and an
absent bundle (a checkout that has harvested nothing) passes.

## Owner Roles

Every Owner Role is an agent identifier the framework already carries, and each holds the item
matching its own contract: readiness and delivery risk with `tech-lead`, verification with
`qa`, structural judgment with `architect`, evidence sufficiency with `dev-2-reviewer`,
documentation with `documentation`, and run closure with `orchestrator`.

An item's owner may not be the role that produced the evidence the item assesses. That is the
Producer Exclusion Rule of `workflows/workflow-gate-matrix.md`, applied to the checklist: where
the change was authored by the operator on behalf of a blocked phase, the acceptance is recorded
under the owning role with the operator named as the performer, so the record never reads as
self-approval.

## Recording a Result

The change proposal records one row per item:

| Item | Requirement | Result | Evidence |
|---|---|---|---|
| `FR-01` | Registry coverage | pass | 6/6 checks, unresolved=0 |

Permitted results are `pass`, `fail`, `accepted`, and `not-applicable`. `accepted` records a
failure the owning gate accepted and requires a rationale in the Evidence cell.
`not-applicable` requires the reason the item cannot apply to this change; it is not a way to
avoid an item that merely failed.
