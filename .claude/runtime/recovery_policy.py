#!/usr/bin/env python3
"""Recovery Controller: failure classification, retry policy, and failure envelopes.

Implements the three things `config/execution-engine.md` and `config/runtime.md` require of a
recovery path and that the runtime previously left to the operator:

  1. **Classification.** Every failure is named by a failure class, and the class -- not the
     call site -- decides whether the failure may be retried and what the alternative is.
     The table is the Failure Classification Matrix in
     `config/execution-engine.md#failure-classification-matrix`, extended with the classes
     `config/runtime.md#failure-classes` names that the matrix does not row out.
  2. **A bounded retry policy.** Attempts are capped, delays are exponential with jitter, and
     a class the policy marks non-retryable is never scheduled for one. The default profile
     is `config/runtime.md#retry-policy`, quoted field for field.
  3. **A structured failure envelope.** Every blocked transition emits one. It carries the
     classification, the retry position, the impacted artifacts, the required decision, and
     the exact action that clears the block, so a blocked run explains itself without anyone
     reading the transition log.

This module decides policy and never applies it. It performs no transition, holds no state,
and imports neither the state engine nor the runtime, so the policy can be reasoned about, and
tested, independently of the machine that obeys it. `framework_runtime.py` maps a returned
`Classification` onto the transition its own state tables permit.

Determinism
-----------
`config/runtime.md` specifies jitter of plus or minus twenty percent. A random draw would make
the same failure produce a different envelope on every evaluation, which would break the replay
guarantee the rest of the runtime is built on: re-running a command over committed state must
produce the same fingerprint. So the jitter is derived from the work item's own idempotency
key. It is stable per work item, uniformly spread across work items, and reproducible -- which
is what jitter is for. It is not unpredictable, and nothing here needs it to be.
"""

from __future__ import annotations

import hashlib
import json
from dataclasses import dataclass, asdict
from datetime import datetime, timedelta, timezone
from pathlib import Path

ENVELOPE_SCHEMA = "framework.runtime/failure-envelope.v1"
LEDGER_SCHEMA = "framework.runtime/recovery-ledger.v1"

# Recovery actions, `config/runtime.md#recovery-actions` plus the remediation action the
# aggregation row of the classification matrix names.
RETRY, ROLLBACK, ESCALATE, ABORT, REMEDIATE = (
    "retry", "rollback", "escalate", "abort", "remediate")

# Default retry profile. Every field is the one `config/runtime.md#retry-policy` declares;
# `max_delay_seconds` and `circuit_breaker_threshold` come from the retry profile model in
# `config/execution-engine.md#retry-policy-model`.
RETRY_PROFILE = {
    "max_attempts": 3,
    "backoff_strategy": "exponential",
    "base_delay_seconds": 2,
    "multiplier": 2,
    "max_delay_seconds": 60,
    "jitter_ratio": 0.2,
    "circuit_breaker_threshold": 3,
    "policy_ref": "config/runtime.md#retry-policy",
}


@dataclass(frozen=True)
class FailurePolicy:
    """One row of the Failure Classification Matrix."""

    failure_class: str
    detection_point: str
    retryable: bool
    default_action: str
    escalation_trigger: str
    blocked_reason: str          # config/task-queue.md blocked reason, open set
    required_decision_type: str  # config/execution-engine.md#escalation-contract
    basis: str


def _row(failure_class, detection_point, retryable, action, trigger, reason, decision,
         basis="config/execution-engine.md#failure-classification-matrix"):
    return FailurePolicy(failure_class, detection_point, retryable, action, trigger,
                         reason, decision, basis)


RUNTIME_BASIS = "config/runtime.md#failure-classes"

# Keys are the failure classes `RuntimeError_` already raises, plus the three the matrix names
# that no call site raised before this slice: transport loss, worker loss, and gate rejection.
FAILURE_POLICIES = {
    "request-validation-failure": _row(
        "request-validation-failure", "initialization", False, ABORT, "none",
        "awaiting_input_correction", "input-correction"),
    "context-integrity-failure": _row(
        "context-integrity-failure", "hydration", False, ABORT,
        "repeated source corruption", "awaiting_context_repair", "context-repair"),
    "invocation-transport-failure": _row(
        "invocation-transport-failure", "adapter boundary", True, RETRY,
        "circuit breaker open", "awaiting_recovery_task", "recovery-authorisation"),
    "output-schema-failure": _row(
        "output-schema-failure", "validation", True, RETRY,
        "repeated malformed output", "awaiting_recovery_task", "recovery-authorisation"),
    "workflow-contract-violation": _row(
        "workflow-contract-violation", "validation", False, ROLLBACK,
        "second occurrence in same run", "awaiting_contract_reconciliation",
        "contract-reconciliation"),
    "gate-rejection": _row(
        "gate-rejection", "gate state", False, ROLLBACK, "blocker marked unresolved",
        "awaiting_recovery_task", "rollback-authorisation"),
    "aggregation-conflict": _row(
        "aggregation-conflict", "aggregation", False, REMEDIATE, "critical conflict persists",
        "awaiting_recovery_task", "conflict-resolution"),
    "worker-loss": _row(
        "worker-loss", "queue control", True, RETRY, "repeated worker loss",
        "awaiting_recovery_task", "recovery-authorisation"),
    # Named by config/runtime.md rather than rowed out by the matrix. Neither is retryable:
    # a capability that is not registered is not going to appear on a second attempt, and a
    # policy violation is not a transient fault.
    "missing-capability-failure": _row(
        "missing-capability-failure", "resolution", False, ESCALATE,
        "capability absent from the registry", "awaiting_capability_registration",
        "capability-registration", RUNTIME_BASIS),
    "policy-failure": _row(
        "policy-failure", "policy decision", False, ESCALATE,
        "policy exception requested", "awaiting_policy_exception", "policy-exception",
        RUNTIME_BASIS),
    # `config/runtime.md#failure-classes` groups missing capability and missing dependency
    # into one class. They need different rows here, because they clear differently: a
    # capability is registered, whereas a dependency is produced by another work item. Both
    # are non-retryable, since neither appears because this item tried again.
    "dependency-failure": _row(
        "dependency-failure", "queue control", False, ESCALATE,
        "upstream work item failed terminally", "awaiting_recovery_task",
        "recovery-authorisation", RUNTIME_BASIS),
    # A gate holding its successor is a classified condition, not an error: the gate is doing
    # exactly what `config/execution-engine.md` requires of a control state. It is rowed here
    # so that every blocked transition has a class, a required decision type, and an owner.
    "gate-approval-required": _row(
        "gate-approval-required", "gate state", False, ESCALATE,
        "no decision recorded within the deadline", "awaiting_human_decision",
        "gate-approval", "config/execution-engine.md#human-escalation"),
}

# Blocked reason for a guard-raised block, keyed by the failure class the guard reported.
# This is the table `framework_runtime.py` used to hold inline; it lives here now so the
# guard path and the recovery path cannot disagree about what a class means.
BLOCK_REASON_BY_FAILURE_CLASS = {
    cls: pol.blocked_reason for cls, pol in FAILURE_POLICIES.items()
}


@dataclass
class Classification:
    """The recovery decision for one failure, and the evidence for it."""

    failure_class: str
    detection_point: str
    retryable: bool
    action: str                  # the action actually chosen, after budget and breaker
    default_action: str          # the action the class declares before those are applied
    escalation_trigger: str
    blocked_reason: str
    required_decision_type: str
    basis: str
    attempt: int
    attempts_charged: int
    attempts_lost: int
    max_attempts: int
    attempts_remaining: int
    budget_exhausted: bool
    breaker_open: bool
    charged: bool                # does this failure spend the retry budget
    next_delay_seconds: float | None
    available_at: str | None
    reason: str                  # one sentence: why this action, in these circumstances

    def to_dict(self) -> dict:
        return asdict(self)


def now() -> datetime:
    return datetime.now(timezone.utc)


def iso(dt: datetime) -> str:
    return dt.isoformat(timespec="seconds").replace("+00:00", "Z")


def deterministic_jitter(seed: str, ratio: float) -> float:
    """A stable multiplier in [1-ratio, 1+ratio], derived from `seed`.

    See the module docstring: the runtime's replay guarantee forbids a random draw here.
    """
    if not ratio:
        return 1.0
    digest = hashlib.sha256((seed or "-").encode("utf-8")).hexdigest()
    unit = int(digest[:8], 16) / 0xFFFFFFFF          # [0, 1]
    return 1.0 + (unit * 2 - 1) * ratio              # [1-ratio, 1+ratio]


def backoff_seconds(attempt: int, *, seed: str = "", profile: dict | None = None) -> float:
    """Delay before the attempt numbered `attempt + 1`, per the retry profile.

    The cap is applied after the jitter, not before it. Jittering a capped value would let the
    upper jitter band exceed `max_delay_seconds`, which would make the field the profile calls a
    maximum not one.
    """
    p = profile or RETRY_PROFILE
    raw = p["base_delay_seconds"] * (p["multiplier"] ** max(0, attempt - 1))
    jittered = raw * deterministic_jitter(seed, p["jitter_ratio"])
    return round(min(jittered, p["max_delay_seconds"]), 3)


def classify(failure_class: str, *, attempt: int, attempts_lost: int, max_attempts: int,
             charged: bool = True, seed: str = "", recurrences: int = 0,
             profile: dict | None = None) -> Classification:
    """Classify one failure and choose the least destructive valid recovery action.

    `charged` is False for a failure that produced nothing to judge: `config/task-queue.md`
    makes lease expiry a recovery classification that does not itself mean failure, so a
    transport loss must not spend a budget meant for results the Validation Engine rejected.

    `recurrences` is how many times this same class has already been classified for this work
    item. It drives the escalation triggers the matrix states as "repeated ...", which is what
    stands in for a circuit breaker in a runtime with no breaker service.
    """
    p = profile or RETRY_PROFILE
    pol = FAILURE_POLICIES.get(failure_class)
    if pol is None:
        # An unknown class is not silently treated as transient. Escalating is the
        # least destructive action that cannot lose evidence.
        pol = _row(failure_class, "unclassified", False, ESCALATE,
                   "failure class is not in the classification matrix",
                   "awaiting_policy_exception", "policy-exception",
                   "recovery_policy.py: unclassified failure")

    charged_attempts = attempt - attempts_lost
    remaining = max(0, max_attempts - charged_attempts)
    exhausted = pol.retryable and remaining <= 0
    breaker_open = pol.retryable and recurrences >= p["circuit_breaker_threshold"]

    if not pol.retryable:
        action = pol.default_action
        reason = (f"class {pol.failure_class!r} is non-retryable at detection point "
                  f"{pol.detection_point!r}, so the declared action {action!r} applies "
                  f"without consulting the retry budget")
        delay = avail = None
    elif breaker_open:
        action, delay, avail = ESCALATE, None, None
        reason = (f"class {pol.failure_class!r} is retryable, but it has now been classified "
                  f"{recurrences} time(s) for this work item, which meets the escalation "
                  f"trigger {pol.escalation_trigger!r}")
    elif exhausted:
        action, delay, avail = ESCALATE, None, None
        reason = (f"class {pol.failure_class!r} is retryable, but {charged_attempts} of "
                  f"{max_attempts} charged attempts are spent, so the retry budget is "
                  f"exhausted and the failure escalates instead")
    else:
        action = RETRY
        delay = backoff_seconds(attempt, seed=seed, profile=p)
        avail = iso(now() + timedelta(seconds=delay))
        reason = (f"class {pol.failure_class!r} is retryable and {remaining} of "
                  f"{max_attempts} charged attempt(s) remain, so one further attempt is "
                  f"scheduled after {delay}s of {p['backoff_strategy']} backoff")

    return Classification(
        failure_class=pol.failure_class,
        detection_point=pol.detection_point,
        retryable=pol.retryable,
        action=action,
        default_action=pol.default_action,
        escalation_trigger=pol.escalation_trigger,
        blocked_reason=pol.blocked_reason,
        required_decision_type=pol.required_decision_type,
        basis=pol.basis,
        attempt=attempt,
        attempts_charged=charged_attempts,
        attempts_lost=attempts_lost,
        max_attempts=max_attempts,
        attempts_remaining=remaining,
        budget_exhausted=bool(exhausted),
        breaker_open=bool(breaker_open),
        charged=bool(charged),
        next_delay_seconds=delay,
        available_at=avail,
        reason=reason,
    )


def classify_validation_outcome(report_dict: dict, undeclared_side_effects: list) -> str:
    """Name the failure class for a rejected artifact.

    Three different faults arrive at the same detection point, and the matrix gives them
    different actions, so they must be told apart before an action is chosen:

      - an undeclared side effect is a **policy violation**. The agent wrote somewhere its
        constraints did not permit, which no repair pass can undo and no retry should paper
        over.
      - a structural failure the Validation Engine rates `Blocking` is an **output schema
        failure**: the artifact exists and its defects are named, which is precisely the
        "schema-correctable output omission" the retry-eligibility list admits.
      - only `Correctable` failures are the same class, and reach the same action, for the
        same reason.

    A workflow contract violation is not decidable from a validation report: it is raised by
    the resolution chain, before an artifact exists.
    """
    if undeclared_side_effects:
        return "policy-failure"
    return "output-schema-failure"


# --------------------------------------------------------------------------- envelope


def failure_envelope(*, run_id: str, item: dict, classification: Classification,
                     reason_code: str, detail: str, occurrence: int,
                     detected_by: str, guard: str | None = None,
                     evidence: dict | None = None, owner_roles: list | None = None,
                     impacted_artifacts: list | None = None,
                     clearing_action: str | None = None,
                     resulting_status: str | None = None) -> dict:
    """Build the canonical failure envelope for one blocked or classified transition.

    The field set is the union of what two specifications require, because a blocked
    transition is exactly the moment both apply:

      - `config/runtime.md#recovery-ledger-requirements`: failure class and detection point,
        chosen action and reason, retry attempt count and next timeout, impacted state and
        artifact references.
      - `config/execution-engine.md#escalation-contract`: escalation id, run, state, reason
        code, required decision type, evidence bundle reference, proposed options, and a
        default fallback action.

    `clearing_action` is the runtime's own addition: the exact command that resolves the
    block. A structured failure that does not say what to do next is a diagnosis without a
    prescription.
    """
    return {
        "schema": ENVELOPE_SCHEMA,
        "envelope_id": f"FE-{run_id}-{item['state_id']}-{occurrence:02d}",
        "escalation_id": f"ESC-{run_id}-{item['state_id']}-{occurrence:02d}",
        "run_id": run_id,
        "work_item_id": item["work_item_id"],
        "state_id": item["state_id"],
        "work_type": item["work_type"],
        "owner_agent_id": item.get("owner_agent_id"),
        "phase_index": item.get("phase_index"),
        "detected_at": iso(now()),
        "detected_by": detected_by,
        "detection_point": classification.detection_point,
        "occurrence": occurrence,
        "failure_class": classification.failure_class,
        "reason_code": reason_code,
        "guard": guard,
        "detail": detail,
        "classification": classification.to_dict(),
        "retry": {
            "attempt": item.get("attempt"),
            "attempts_lost": item.get("attempts_lost"),
            "attempts_charged": classification.attempts_charged,
            "max_attempts": classification.max_attempts,
            "attempts_remaining": classification.attempts_remaining,
            "budget_exhausted": classification.budget_exhausted,
            "breaker_open": classification.breaker_open,
            "next_delay_seconds": classification.next_delay_seconds,
            "available_at": classification.available_at,
            "idempotency_key": item.get("idempotency_key"),
            "profile_ref": RETRY_PROFILE["policy_ref"],
        },
        "resulting_status": resulting_status,
        "blocked_reason": classification.blocked_reason,
        "required_decision_type": classification.required_decision_type,
        "owner_roles": owner_roles or [],
        "impacted_artifacts": impacted_artifacts or [],
        "evidence_bundle_ref": evidence or {},
        "proposed_options": _options(classification),
        "default_fallback_action": (
            "hold the work item blocked and await a human decision"
            if classification.action != RETRY
            else "schedule one further attempt under the retry profile"),
        "clearing_action": clearing_action,
        "status": "open",
    }


# Options per failure class, where the class admits a more specific set than its action does.
CLASS_OPTIONS = {
    "missing-capability-failure": [
        "register the owning agent and its contract, then resume",
        "reassign the phase to a registered owner",
        "abort the run"],
    "gate-approval-required": [
        "approve the gate and proceed",
        "reject the gate and roll back the evidence it assesses",
        "request additional evidence before deciding"],
    # "approve an exception and proceed" is deliberately absent: no runtime surface records
    # such an exception, and an option that names no executable path is a false prescription.
    # The rollback is the executable path: `rollback` re-enters the producing phase under a
    # recorded human authorisation and re-arms this gate for a fresh decision.
    "gate-rejection": [
        "authorise a rollback: rebuild the rejected evidence and re-submit it to the gate",
        "abort the run"],
    "dependency-failure": [
        "recover the upstream work item that failed",
        "supply the missing input directly",
        "abort the run"],
    "policy-failure": [
        "record a policy exception with the owning role",
        "require the agent to withdraw the prohibited side effect",
        "abort the run"],
    "context-integrity-failure": [
        "restore the corrupted context source and start a new run",
        "abort the run"],
    "request-validation-failure": [
        "correct the request inputs and start a new run",
        "abort the run"],
}


def _options(c: Classification) -> list:
    """The Human Decision Outcomes of `config/execution-engine.md` this failure admits.

    Ordered least destructive first, which is the order the Recovery Controller is required to
    prefer. A generic list would be worse than none: an operator reading "approve the proposed
    path" against an unregistered capability learns nothing about what to approve.
    """
    if c.action == RETRY:
        return [f"await the scheduled attempt (available at {c.available_at})",
                "reclaim the work item now and re-dispatch",
                "abort the run"]
    if c.budget_exhausted or c.breaker_open:
        return ["repair the rejected output and clear the block for one further attempt",
                "reassign the phase owner",
                "abort the run"]
    if c.failure_class in CLASS_OPTIONS:
        return CLASS_OPTIONS[c.failure_class]
    if c.action == ROLLBACK:
        return ["reject and roll back to the last accepted state",
                "approve an exception and proceed",
                "abort the run"]
    if c.action == ABORT:
        return ["correct the run inputs and start a new run", "abort the run"]
    return ["approve the proposed path", "request additional evidence", "abort the run"]


# --------------------------------------------------------------------------- ledger


class RecoveryLedger:
    """Append-only recovery and escalation ledger for one run.

    `config/execution-engine.md#execution-context` lists the recovery ledger and the
    escalation ledger as mutable partitions of the execution context. One file carries both,
    because in this runtime every escalation is the outcome of a classification: an entry is a
    classification, its chosen action, and -- once it happens -- its resolution.
    """

    FILENAME = "recovery-ledger.json"

    def __init__(self, run_dir: Path):
        self.path = run_dir / self.FILENAME

    def read(self) -> dict:
        if not self.path.exists():
            return {"schema": LEDGER_SCHEMA, "entries": []}
        return json.loads(self.path.read_text(encoding="utf-8"))

    def entries(self) -> list:
        return self.read().get("entries") or []

    def occurrences(self, state_id: str, failure_class: str | None = None) -> int:
        return sum(1 for e in self.entries()
                   if e.get("state_id") == state_id
                   and (failure_class is None or e.get("failure_class") == failure_class))

    def append(self, envelope: dict) -> dict:
        data = self.read()
        data["entries"].append(envelope)
        data["updated_at"] = iso(now())
        tmp = self.path.with_suffix(".json.tmp")
        tmp.write_text(json.dumps(data, indent=2), encoding="utf-8")
        tmp.replace(self.path)
        return envelope

    def resolve(self, state_id: str, *, resolution: str, resolved_by: str) -> int:
        """Mark every open entry for `state_id` resolved. Returns how many were closed.

        Resolution is recorded on the entry rather than by deleting it: the ledger is the
        audit trail of what the run recovered from, so an entry that is removed once the
        block clears would erase the evidence that anything was recovered at all.
        """
        data = self.read()
        closed = 0
        for e in data.get("entries") or []:
            if e.get("state_id") == state_id and e.get("status") == "open":
                e["status"] = "resolved"
                e["resolved_at"] = iso(now())
                e["resolution"] = resolution
                e["resolved_by"] = resolved_by
                closed += 1
        if closed:
            data["updated_at"] = iso(now())
            tmp = self.path.with_suffix(".json.tmp")
            tmp.write_text(json.dumps(data, indent=2), encoding="utf-8")
            tmp.replace(self.path)
        return closed
