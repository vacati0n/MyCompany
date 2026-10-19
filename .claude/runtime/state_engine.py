#!/usr/bin/env python3
"""Persisted state engine for multi-phase workflow runs.

Implements the component `config/runtime.md` and `config/execution-engine.md` name the
**State Engine**, together with the work-item lifecycle governed by `config/task-queue.md`.
Before this module the runtime executed exactly one workflow state per run. A run now
carries one work item per workflow phase, each with a durable status, a guarded transition
table, and an ordered transition log.

This module is deliberately free of any knowledge of agents, registries, adapters, or
validators. It owns four things and nothing else:

  1. the work-item status set and the legal transitions between statuses
  2. durable, atomic persistence of run state (`state.json`)
  3. the ordered transition log that is the primary evidence this slice produces
  4. idempotency keys and replay detection

Guard evaluation lives in `framework_runtime.py`, because guards must read the registries,
the workflow Phase Model, and the gate matrix. This module only accepts a guard verdict and
applies the transition it implies, so the state machine stays testable in isolation.

Status vocabulary
-----------------
The persisted statuses are the seven this runtime implements:

    pending, leased, running, retrying, completed, failed, blocked

`config/task-queue.md` is the governing specification for queue lifecycle, and it names
eight canonical task states. The seven above are a projection of that vocabulary, not a
competing one; `QUEUE_PROJECTION` records the mapping, and every persisted work item
carries its canonical `queue_status` alongside its runtime status:

    pending    -> Ready when every guard passes, Waiting while a dependency is unmet
    leased     -> Executing  (lease issued, invocation not yet started)
    running    -> Executing  (invocation started, result outstanding)
    retrying   -> Retrying   (a classified retryable failure, waiting out its backoff)
    completed  -> Completed  (terminal)
    failed     -> Failed     (terminal)
    blocked    -> Blocked    (an explicit, reasoned blocker; never a silent stall)

`Cancelled` is the one canonical queue state this runtime does not implement. No transition in
this module produces it, and `runtime/README.md` records the gap. Reason codes are the closed
canonical set from `config/progress-model.md`; blocked reasons are the open set from
`config/task-queue.md`, which permits new reason codes provided they map onto the canonical
lifecycle.

Terminality and authorised re-entry
-----------------------------------
`completed` and `failed` are terminal for every automatic purpose: no scheduler, retry, or
replay path leaves either. Exactly two human-authorised transitions do, and both are recorded
in the tables below like any other pair so that `verify_multi_phase.py` can replay them:

    state  completed -> pending   trigger `superseded`           (a rollback re-enters the phase)
    gate   failed    -> pending   trigger `rollback_authorised`  (the rejecting gate is re-armed)

`superseded` is additionally engine-checked: `transition()` refuses it unless the caller
supplies a supersession record (`fields={"supersession": {"authorisation_id": ...}}`), and
it moves the item's prior `completion` block, unmodified, into the append-only
`supersessions` list together with that record. Attempt 1's artifact is therefore never
rewritten by a re-entry; the next attempt writes elsewhere and `completion` follows it. Only
`framework_runtime.py`'s `rollback` command constructs the record, after the owner-role and
budget checks it shares with `gate`.

`retrying` carries `available_at`: the instant the work item becomes dispatchable again.
`config/task-queue.md` distinguishes `Retrying` from `Waiting` by history rather than by
eligibility -- a retrying task has already executed -- so the two cannot be folded together
without losing that distinction. This module stores the field and the deadline; deciding the
delay is the Recovery Controller's job, in `recovery_policy.py`, and promoting the item once
the deadline passes is the scheduler's, in `framework_runtime.py`.
"""

from __future__ import annotations

import hashlib
import json
import os
from datetime import datetime, timezone
from pathlib import Path

SCHEMA = "framework.runtime/state-engine.v1"

PENDING, LEASED, RUNNING, RETRYING, COMPLETED, FAILED, BLOCKED = (
    "pending", "leased", "running", "retrying", "completed", "failed", "blocked")

STATUSES = (PENDING, LEASED, RUNNING, RETRYING, COMPLETED, FAILED, BLOCKED)
TERMINAL_STATUSES = frozenset({COMPLETED, FAILED})
ACTIVE_STATUSES = frozenset({LEASED, RUNNING})

WORK_TYPES = ("state", "gate")

# Projection onto the canonical task states of config/task-queue.md. `pending` splits by
# guard verdict, so it resolves through `queue_status()` rather than this table.
QUEUE_PROJECTION = {
    LEASED: "Executing",
    RUNNING: "Executing",
    RETRYING: "Retrying",
    COMPLETED: "Completed",
    FAILED: "Failed",
    BLOCKED: "Blocked",
}

# Canonical reason codes, config/progress-model.md. Closed set.
REASON_CODES = frozenset({
    "enqueued", "leased", "context_hydration_started", "context_hydration_completed",
    "analysis_started", "execution_started", "dependency_wait", "approval_wait",
    "retry_wait", "output_accepted", "validation_failed", "timeout", "tool_failure",
    "policy_block", "terminal_failure",
})

# Legal transitions, per work type. Every key is (from_status, to_status); the value is the
# trigger recorded in the transition log. Any pair absent from the table for that work type
# is forbidden, which is what makes terminal statuses terminal: the only keys leaving
# `completed` or `failed` are the two authorised re-entry pairs named in `AUTHORISED_EXITS`,
# and one of them is refused by `transition()` unless a supersession record accompanies it.
TRANSITIONS = {
    # A state work item is executed by an agent through the invocation gateway, so it must
    # pass through a lease and an invocation before it can complete.
    "state": {
        (PENDING, LEASED): "lease_acquired",
        (PENDING, BLOCKED): "blocker_detected",
        (LEASED, RUNNING): "invocation_started",
        (LEASED, PENDING): "lease_released",
        (LEASED, BLOCKED): "blocker_detected",
        (RUNNING, COMPLETED): "result_accepted",
        (RUNNING, FAILED): "result_rejected",
        (RUNNING, BLOCKED): "blocker_detected",
        (RUNNING, PENDING): "lease_expired",
        (RUNNING, RETRYING): "retryable_failure_classified",
        (LEASED, RETRYING): "retryable_failure_classified",
        (RETRYING, PENDING): "backoff_elapsed",
        (RETRYING, BLOCKED): "retry_paused",
        (RETRYING, FAILED): "retry_budget_exhausted",
        (BLOCKED, PENDING): "blocker_cleared",
        (BLOCKED, FAILED): "blocker_terminal",
        # An authorised rollback re-enters a committed phase as a new attempt. The pair is
        # legal only with a supersession record; see `transition()`.
        (COMPLETED, PENDING): "superseded",
    },
    # A gate is an explicit control state, not an invocation: it is committed by a recorded
    # decision, so it never leases and never runs. `config/execution-engine.md` requires
    # gates to be control states rather than implicit post-processing, and this table is
    # where that requirement is enforced.
    "gate": {
        (PENDING, BLOCKED): "awaiting_decision",
        (PENDING, COMPLETED): "decision_approved",
        (PENDING, FAILED): "decision_rejected",
        (BLOCKED, COMPLETED): "decision_approved",
        (BLOCKED, FAILED): "decision_rejected",
        (BLOCKED, PENDING): "blocker_cleared",
        # A rejected gate is re-armed on the same work item once a rollback is authorised.
        # Its decision returns to null and the rejection moves into `decision_history`.
        (FAILED, PENDING): "rollback_authorised",
    },
}

# The only transitions that leave a terminal status, per work type. `verify_multi_phase.py`
# M4 and M13 read this rather than restating it, so the two cannot disagree.
AUTHORISED_EXITS = {
    "state": {(COMPLETED, PENDING): "superseded"},
    "gate": {(FAILED, PENDING): "rollback_authorised"},
}


class TransitionError(Exception):
    """A transition the state machine forbids."""


def now() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds").replace("+00:00", "Z")


def digest(*parts: str) -> str:
    return "sha256:" + hashlib.sha256("|".join(parts).encode("utf-8")).hexdigest()[:32]


def work_item_id(run_id: str, state_id: str, work_type: str = "state") -> str:
    """Stable work-item identity.

    The `state` form matches the identifier the runtime has always written into its progress
    events, so event streams produced before this slice stay comparable with those produced
    after it.
    """
    return f"{run_id}::{state_id}" if work_type == "state" \
        else f"{run_id}::{work_type}::{state_id}"


def idempotency_key(run_id: str, state_id: str, owner_agent_id: str,
                    agent_version: str, payload_digest: str) -> str:
    """Idempotency key for one work item.

    `attempt` is deliberately excluded: `config/task-queue.md` requires a retry to preserve
    the same `task_id` and `idempotency_key` across attempts, so the key identifies the unit
    of work, not one execution of it. The payload digest is included, so a run replayed
    against changed inputs or a changed context slice is a different unit of work and cannot
    silently reuse a prior completion.
    """
    return digest("work-item", run_id, state_id, owner_agent_id or "-",
                  agent_version or "-", payload_digest or "-")


def attempts_charged(item: dict) -> int:
    """Attempts that count against the retry budget.

    Every dispatch increments `attempt`. Only those that produced a result the Validation
    Engine could classify are charged, so a run is not driven to `failed` by transport
    failures that never reached the agent.
    """
    return item.get("attempt", 0) - item.get("attempts_lost", 0)


def retry_due(item: dict, at: str | None = None) -> bool:
    """True when a retrying work item has waited out its backoff.

    Comparing the ISO-8601 timestamps as strings is exact here, because every timestamp this
    module writes is UTC, second-precision, and `Z`-suffixed, which makes lexical order and
    chronological order the same order.
    """
    if item["status"] != RETRYING:
        return False
    due = item.get("available_at")
    return not due or (at or now()) >= due


def queue_status(item: dict) -> str:
    """Canonical task-queue.md status for a work item."""
    if item["status"] == PENDING:
        return "Ready" if item.get("eligible") else "Waiting"
    return QUEUE_PROJECTION[item["status"]]


def new_work_item(*, run_id: str, workflow_id: str, state_id: str, work_type: str,
                  owner_agent_id: str | None, phase_index: int, gate: str | None,
                  artifact: str | None, depends_on: list, max_attempts: int = 3) -> dict:
    """A work item in its initial status.

    Carries every field required by the Work Item contract in `config/execution-engine.md`
    plus the optional fields `config/task-queue.md` requires for blocking and failure
    bookkeeping.
    """
    if work_type not in WORK_TYPES:
        raise TransitionError(f"unknown work type {work_type!r}; known: {WORK_TYPES}")
    return {
        "work_item_id": work_item_id(run_id, state_id, work_type),
        "run_id": run_id,
        "workflow_id": workflow_id,
        "state_id": state_id,
        "work_type": work_type,
        "owner_agent_id": owner_agent_id,
        "status": PENDING,
        "queue_status": "Waiting",
        "eligible": False,
        "phase_index": phase_index,
        "gate": gate,
        "artifact": artifact,
        "depends_on": depends_on,
        "attempt": 0,
        # Attempts consumed by worker loss rather than by a classified result. An adapter
        # that never reported produced nothing to judge, so it does not spend the retry
        # budget: `config/task-queue.md` makes lease expiry a recovery classification and
        # explicitly says it does not itself mean failure. Both counters are persisted, so
        # the distinction stays auditable rather than being folded away.
        "attempts_lost": 0,
        "max_attempts": max_attempts,
        "lease_expires_at": None,
        # The instant a retrying work item becomes dispatchable again. Null on every status
        # other than `retrying`, and cleared by the transition that leaves it.
        "available_at": None,
        "last_worker_id": None,
        "idempotency_key": None,
        "payload_digest": None,
        "blocked_reason": None,
        "blocked_detail": None,
        "failure_class": None,
        "failure_detail": None,
        "guards": [],
        "completion": None,
        # Append-only. Each entry is a prior attempt's `completion` block, moved here
        # unmodified by an authorised `superseded` transition together with the
        # authorisation that superseded it. Never rewritten, never pruned.
        "supersessions": [],
        "replays": [],
        "created_at": now(),
        "updated_at": now(),
    }


class StateStore:
    """Durable, atomically written run state.

    The execution context store is the source of truth for run state, so every mutation
    lands on disk before the caller regains control. Writes go to a sibling temp file and
    are moved into place, so a crash mid-write cannot leave a half-parsed ledger.
    """

    FILENAME = "state.json"

    def __init__(self, path: Path, data: dict):
        self.path = path
        self.data = data

    # ------------------------------------------------------------------ construction

    @classmethod
    def create(cls, run_dir: Path, *, run_id: str, command_id: str, workflow_id: str,
               workflow_version: str, runtime_version: str, input_digest: str,
               inputs: list) -> "StateStore":
        run_dir.mkdir(parents=True, exist_ok=True)
        data = {
            "schema": SCHEMA,
            "run_id": run_id,
            "runtime_version": runtime_version,
            "command_id": command_id,
            "workflow_id": workflow_id,
            "workflow_version": workflow_version,
            "run_status": "Initializing",
            "created_at": now(),
            "updated_at": now(),
            "input_digest": input_digest,
            "inputs": inputs,
            "work_items": {},
            "gates": {},
            "transitions": [],
            "replays": [],
        }
        store = cls(run_dir / cls.FILENAME, data)
        store.save()
        return store

    @classmethod
    def load(cls, run_dir: Path) -> "StateStore | None":
        p = run_dir / cls.FILENAME
        if not p.exists():
            return None
        data = json.loads(p.read_text(encoding="utf-8"))
        if data.get("schema") != SCHEMA:
            raise TransitionError(
                f"{p} declares schema {data.get('schema')!r}, expected {SCHEMA!r}")
        return cls(p, data)

    @classmethod
    def load_or_create(cls, run_dir: Path, **kw) -> tuple["StateStore", bool]:
        existing = cls.load(run_dir)
        if existing is not None:
            return existing, False
        return cls.create(run_dir, **kw), True

    def save(self):
        self.data["updated_at"] = now()
        tmp = self.path.with_suffix(".json.tmp")
        tmp.write_text(json.dumps(self.data, indent=2), encoding="utf-8")
        os.replace(tmp, self.path)

    # ------------------------------------------------------------------ accessors

    @property
    def items(self) -> dict:
        return self.data["work_items"]

    @staticmethod
    def _key(state_id: str, work_type: str) -> str:
        return state_id if work_type == "state" else f"{work_type}:{state_id}"

    def item(self, state_id: str, work_type: str = "state") -> dict:
        key = self._key(state_id, work_type)
        if key not in self.items:
            raise TransitionError(f"no {work_type} work item for {state_id!r} in {self.path}")
        return self.items[key]

    def has_item(self, state_id: str, work_type: str = "state") -> bool:
        return self._key(state_id, work_type) in self.items

    def add_item(self, item: dict) -> dict:
        key = self._key(item["state_id"], item["work_type"])
        if key in self.items:
            return self.items[key]
        self.items[key] = item
        self.save()
        return item

    def ordered_items(self, work_type: str | None = None) -> list:
        """Work items in execution order: each phase, then the gates that close it."""
        vals = [i for i in self.items.values()
                if work_type is None or i["work_type"] == work_type]
        return sorted(vals, key=lambda i: (i["phase_index"],
                                           0 if i["work_type"] == "state" else 1,
                                           i["state_id"]))

    def state_items(self) -> list:
        return self.ordered_items("state")

    # ------------------------------------------------------------------ transitions

    def transition(self, item: dict, to_status: str, *, reason_code: str,
                   actor_type: str, actor_id: str, detail: str,
                   trigger: str | None = None, fields: dict | None = None) -> dict:
        """Apply one guarded status transition and persist it.

        Raises `TransitionError` when the pair is absent from the table for this work type,
        which is how terminal statuses are enforced: nothing leaves `completed` or `failed`
        except the two authorised re-entry pairs in `AUTHORISED_EXITS`, and the `superseded`
        pair is refused unless `fields` carries a supersession record with a non-empty
        `authorisation_id`. When it does, the item's prior `completion` block moves unmodified
        into `supersessions` beside that record, so the committed evidence of the superseded
        attempt stays on the item and attempt 1's artifact is never rewritten.
        """
        frm = item["status"]
        table = TRANSITIONS[item["work_type"]]
        if to_status not in STATUSES:
            raise TransitionError(f"unknown status {to_status!r}; known: {STATUSES}")
        if reason_code not in REASON_CODES:
            raise TransitionError(
                f"non-canonical reason code {reason_code!r}; config/progress-model.md "
                f"declares a closed set")
        if (frm, to_status) not in table:
            legal = sorted(t for (f, t) in table if f == frm)
            raise TransitionError(
                f"forbidden transition {frm} -> {to_status} for {item['work_type']} work item "
                f"{item['work_item_id']}; legal from {frm}: {legal or 'none (terminal)'}")

        fields = dict(fields or {})
        supersession = None
        if table[(frm, to_status)] == "superseded":
            supersession = fields.pop("supersession", None)
            if not isinstance(supersession, dict) or not supersession.get("authorisation_id"):
                raise TransitionError(
                    f"transition {frm} -> {to_status} (superseded) for work item "
                    f"{item['work_item_id']} requires a supersession record carrying an "
                    f"authorisation_id; a committed phase is re-entered only under a "
                    f"recorded rollback authorisation")
        elif "supersession" in fields:
            raise TransitionError(
                f"a supersession record accompanies transition {frm} -> {to_status}, which "
                f"is not the superseded pair")

        if supersession is not None:
            item.setdefault("supersessions", []).append({
                **supersession,
                "attempt": item.get("attempt", 0),
                "superseded_at": now(),
                "completion": item.get("completion"),
            })
            fields.setdefault("completion", None)
            fields.setdefault("eligible", False)
            fields.setdefault("guards", [])
            fields.setdefault("lease_expires_at", None)
            fields.setdefault("last_worker_id", None)

        item.update(fields)
        item["status"] = to_status
        item["updated_at"] = now()
        item["queue_status"] = queue_status(item)

        record = {
            "seq": len(self.data["transitions"]) + 1,
            "at": now(),
            "work_item_id": item["work_item_id"],
            "state_id": item["state_id"],
            "work_type": item["work_type"],
            "phase_index": item["phase_index"],
            "from": frm,
            "to": to_status,
            "trigger": trigger or table[(frm, to_status)],
            "reason_code": reason_code,
            "queue_status": item["queue_status"],
            "actor_type": actor_type,
            "actor_id": actor_id,
            "attempt": item["attempt"],
            "idempotency_key": item.get("idempotency_key"),
            "detail": detail,
        }
        self.data["transitions"].append(record)
        self.data["run_status"] = self.project_run_status()
        self.save()
        return record

    def set_eligibility(self, item: dict, eligible: bool, guards: list):
        """Record the latest guard evaluation without moving the item.

        Eligibility is a projection of the guard verdicts, not a status, so it never enters
        the transition log; only the `pending` split between `Ready` and `Waiting` observes
        it.
        """
        item["eligible"] = bool(eligible)
        item["guards"] = guards
        item["queue_status"] = queue_status(item)
        item["updated_at"] = now()
        self.data["run_status"] = self.project_run_status()
        self.save()

    # ------------------------------------------------------------------ idempotency

    def bind_payload(self, item: dict, *, owner_agent_id: str, agent_version: str,
                     payload_digest: str) -> str:
        """Compute and durably bind this work item's idempotency key.

        Binding is stable for a given payload: a second bind with the same payload returns
        the same key, which is what lets a re-dispatch be recognised as a replay rather than
        a new unit of work. A second bind with a *different* payload against a work item
        that has already reached a terminal status is refused, because a committed result
        may not be silently re-attributed to different inputs.
        """
        key = idempotency_key(item["run_id"], item["state_id"], owner_agent_id,
                              agent_version, payload_digest)
        if item["idempotency_key"] and item["idempotency_key"] != key \
                and item["status"] in TERMINAL_STATUSES:
            raise TransitionError(
                f"work item {item['work_item_id']} is {item['status']} under idempotency key "
                f"{item['idempotency_key']}; the supplied payload derives {key}. A committed "
                f"result cannot be rebound to a different payload; changed inputs are a new "
                f"run.")
        item["idempotency_key"] = key
        item["payload_digest"] = payload_digest
        item["owner_agent_id"] = owner_agent_id
        item["updated_at"] = now()
        self.save()
        return key

    def record_replay(self, item: dict, *, action: str, detail: str) -> dict:
        """Record that a caller repeated an action already committed for this work item.

        A replay writes one bookkeeping entry and nothing else. It emits no progress event
        and appends no transition, so a repeated `dispatch` or `complete` cannot duplicate
        the side effects of the run. The entry itself is the evidence that the repetition
        was seen and suppressed.
        """
        entry = {
            "at": now(),
            "work_item_id": item["work_item_id"],
            "state_id": item["state_id"],
            "action": action,
            "status_at_replay": item["status"],
            "idempotency_key": item.get("idempotency_key"),
            "detail": detail,
        }
        item["replays"].append(entry)
        self.data["replays"].append(entry)
        self.save()
        return entry

    def completion_matches(self, item: dict, artifact_digest: str | None) -> bool:
        """True when a completed item's committed result matches what is on disk now."""
        c = item.get("completion") or {}
        return bool(c) and c.get("artifact_digest") == artifact_digest

    # ------------------------------------------------------------------ gates

    def add_gate(self, gate: dict) -> dict:
        return self.data["gates"].setdefault(gate["gate"], gate)

    def gate(self, name: str) -> dict | None:
        return self.data["gates"].get(name)

    def gates_for(self, state_id: str) -> list:
        return [g for g in self.data["gates"].values() if g["closes_state"] == state_id]

    # ------------------------------------------------------------------ projections

    def project_run_status(self) -> str:
        """Project the run lifecycle status defined by `config/execution-engine.md`.

        The run status is derived, never assigned: it is a pure function of the work-item
        statuses, so it cannot drift from them.
        """
        items = self.state_items()
        if not items:
            return "Initializing"
        statuses = [i["status"] for i in items]
        if any(s in ACTIVE_STATUSES for s in statuses):
            return "ExecutingState"
        # A scheduled retry outranks the availability of unrelated work: the most informative
        # fact about a run holding one is that it is recovering from a classified failure, and
        # `config/execution-engine.md` gives `Retrying` its own run-lifecycle state for exactly
        # that reason.
        if any(s == RETRYING for s in statuses):
            return "Retrying"
        if all(s == COMPLETED for s in statuses):
            return "Completed"
        if any(s == FAILED for s in statuses):
            return "Recovering"
        if any(i["status"] == PENDING and i["eligible"] for i in items):
            return "Dispatching"
        if any(s == BLOCKED for s in statuses):
            # Every blocker this slice can raise needs a human action to clear: register a
            # capability, decide a gate, or supply a missing input.
            return "WaitingForHuman"
        return "Ready"

    def summary(self) -> dict:
        items = self.state_items()
        return {
            "run_id": self.data["run_id"],
            "run_status": self.data["run_status"],
            "phases": len(items),
            "completed": sum(1 for i in items if i["status"] == COMPLETED),
            "blocked": sum(1 for i in items if i["status"] == BLOCKED),
            "failed": sum(1 for i in items if i["status"] == FAILED),
            "pending": sum(1 for i in items if i["status"] == PENDING),
            "retrying": sum(1 for i in items if i["status"] == RETRYING),
            "active": sum(1 for i in items if i["status"] in ACTIVE_STATUSES),
            "transitions": len(self.data["transitions"]),
            "replays_suppressed": len(self.data["replays"]),
        }
