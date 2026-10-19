# Configuration: Gate Decision Policy

## Purpose

Define how gate decisions are made: always by a human (the default), or — when a team
opts in — by the runtime itself for gates whose evidence is unambiguously clean, keeping
human review for the decisions that warrant one and for the final pull-request review.

The machine-readable companion the runtime reads is `config/gate-policy.json`. This
document specifies its semantics. A missing `gate-policy.json` means `human-required`
for everything: no existing installation changes behavior until a team writes the file
or passes `--gate-policy` explicitly.

## Modes

| Mode | Behavior |
|---|---|
| `human-required` | Every gate blocks until a human records a decision via `gate --decision`. Today's behavior, and the default. |
| `auto-on-clean-evidence` | The runtime may approve a decision-eligible gate itself, when — and only when — every auto-approval condition below holds. Any condition failing keeps that gate on the human path, byte-for-byte identical to `human-required`. |

The mode is set in `config/gate-policy.json`, and overridden per invocation with
`--gate-policy auto` / `--gate-policy human` on the runtime's `plan`/`dispatch`/
`complete`/`next`/`gate` subcommands.

## `config/gate-policy.json`

```json
{
  "schemaVersion": 1,
  "mode": "auto-on-clean-evidence",
  "severityThreshold": "high",
  "pinned": {
    "fix-bug": { "Closure Gate": "human-required" }
  }
}
```

- `mode` — `human-required` (default) or `auto-on-clean-evidence`.
- `severityThreshold` — a gate auto-approves only when the upstream artifact's declared
  severity ranks *below* this value. Default `high`, so `critical` and `high` always stay
  human. Rank order: `low < medium < high < critical`. An artifact type whose schema
  carries no severity field is unaffected by this condition.
- `pinned` — `{workflow id: {gate name: "human-required"}}`. A pinned gate is never
  auto-decided regardless of mode, letting a team keep, say, the Closure Gate
  always-human while automating Triage/Fix/Verification.

## Auto-approval conditions

Under `auto-on-clean-evidence`, the runtime approves a decision-eligible gate only when
ALL of the following hold. The evaluation is the runtime's own — never the agent whose
evidence the gate assesses. The Producer Exclusion Rule applies to the automated decider
role exactly as it applies to a human one: the recorded `owner_role` is always a listed
gate owner that did not produce the evidence, and a gate with no such role stays human.

1. **The gate is not pinned `human-required`** for this workflow in `pinned`.
2. **The upstream phase's validation passed clean**: the phase is `completed`, its
   Validation Engine result is `pass`, and no undeclared side effect was recorded.
   Retries are permitted provided the final attempt passed clean — a phase only reaches
   `completed` through an accepted artifact.
3. **The declared severity is below `severityThreshold`** where the artifact schema
   carries one (e.g. `bug-analysis.md`'s `severity`). No declared severity ⇒ this
   condition does not apply.
4. **No open question in the phase's artifact is marked blocking** (the `Blocking`
   column of its Open Questions table).
5. **No deviation was escalated** (the `Escalation` column of its Deviations and
   Tradeoffs table names only `not-required`).
6. **No defect stands unresolved** — for QA-owned gates specifically (`qa` among the
   gate's owners), no row of the validation report's Defects table has `Status: open`.
7. **An approved precedent exists**: this exact (workflow, workflow version, gate,
   producing-agent version) combination has been decided `approved` in a prior run. The
   first pass of a new workflow or agent version through a gate is never auto-approved —
   an unproven contract earns automation by first being decided cleanly under a human eye.

## Audit trail

An auto-decision is recorded exactly like a human one — same gate record, same work-item
transition, same `escalation_resolved` ledger event — attributed to
`decided_by: "runtime:auto-policy"` with `actor_type: "runtime"`, and carrying an
`auto_policy` block naming every condition evaluated and the value found, plus the
thresholds in force. A decision is never silently skipped or silently taken: a held gate
prints why it was held, and an auto-approved gate prints who decided it.

## Worked example

A `fix-bug` run of a low-severity defect under `mode: auto-on-clean-evidence`,
`severityThreshold: high`, nothing pinned. Four gates: Triage Gate, Fix Gate,
Verification Gate, Closure Gate. The same workflow and agent versions have prior
approved runs, so precedent exists at every gate.

| Step | Event | Decided by |
|---|---|---|
| 1 | `triage-and-impact` completes; validation `pass` (17/17); `bug-analysis.md` declares `severity: low`, no blocking open question | — |
| 2 | Triage Gate becomes decision-eligible and **auto-approves** — `severity=low (below high); blocking-open-questions=0; precedent=found` | `runtime:auto-policy` |
| 3 | `root-cause-analysis` and `fix-implementation` complete clean; no deviation escalated | — |
| 4 | Fix Gate **auto-approves** on the same grounds | `runtime:auto-policy` |
| 5 | `regression-validation` completes; validation report verdict `pass`, Defects table empty | — |
| 6 | Verification Gate (QA-owned) **auto-approves** — `open-defects=0` among its conditions | `runtime:auto-policy` |
| 7 | `closure-and-communication` completes clean, but its `orchestration-result.md` carries `Q-001` with `Blocking: yes` (an unresolved stakeholder-notification question) | — |
| 8 | Closure Gate is **held for a human** — `held-for-human: open question(s) marked blocking: ['Q-001']` — and blocks with `awaiting_human_decision`, exactly as under `human-required` | — |
| 9 | A human reviews the evidence and records the decision via `gate --gate "Closure Gate" --decision approve ...` | the human |

Three of four stop-and-wait round trips are removed; the one gate whose evidence
actually warranted a human look reached one. The run ledger records all four decisions
with identical structure and full attribution.

## Change Discipline

- The default stays `human-required`; automation is a per-team opt-in.
- Widening automation (lowering the threshold, unpinning a gate) is a policy change and
  belongs in review like any other governance change.
- `runtime/framework_runtime.py` (`load_gate_policy`, `evaluate_auto_approval`,
  `maybe_auto_decide_gates`, `record_gate_decision`) is the implementation this
  specification governs.
