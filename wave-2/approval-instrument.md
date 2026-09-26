# RK-005 — the approval-minutes instrument

**Purpose.** D-002 makes the CEO approve every publication individually. The instrument that
would later *relax* that rule — a "clean record" threshold — is derived from how long an
approval actually takes. `research/ceo-decision-record.md` D-002 states plainly that the
threshold "must be proposed to the CEO, not assumed". This file is the measurement it must be
derived from, and it is set up **before** the approval rather than reconstructed after it.

## Method

Wave 1 decision record D-007 already defines the shape: owner approval is a state in the gate
transition table, and **elapsed minutes are derived from two recorded timestamps**. Wave 2
measures by hand what D-007 will later measure automatically.

| Mark | Definition |
|---|---|
| `t_presented` | The moment the complete review surface is put in front of the CEO — video artifact, thumbnail, metadata, rights record, gate verdicts — with nothing further owed. |
| `t_decided` | The moment the CEO's decision returns. |
| **Elapsed** | `t_decided − t_presented`, in minutes. |

**What the measurement must separate**, because conflating them is what would make a later
threshold wrong:

1. **Review time** — the CEO reading the artifact and forming a judgment. This is the quantity
   that scales with volume and the one the threshold is really about.
2. **Queue time** — the artifact waiting because the CEO was not at the desk. Real workload but
   not a property of the review surface, and it does not scale the same way.
3. **Rework time** — a decision that returns a change request, and the round trip that follows.

A single elapsed figure that silently includes queue time will overstate the cost of D-002 and
argue for relaxing it too early. **Report the three separately or the number is not usable.**

## Caveat that must travel with any figure produced here

**n = 1.** One approval, on the first video the company has ever made, reviewed by a CEO who
already knows its contents from having chosen its subject. It is a **lower bound** on steady-state
review time and it is not a distribution. `research/ceo-decision-record.md` D-002 names the real
cost as recurring and linear in channel count — a single sample cannot characterise that, and
the threshold must not be set from this number alone. What this measurement legitimately
establishes is that the instrument exists and produces a figure; the figure earns weight only
after the record is several videos long.

## Measurements

| # | Approval | `t_presented` | `t_decided` | Review | Queue | Rework | Notes |
|---|---|---|---|---|---|---|---|
| A-0 | D-015, the video's subject (not a publication approval; recorded because it is the first CEO decision this run) | 2026-09-26, during phase 1 | same session, single exchange | not separable | not separable | none — decided first time | Four options, each with its footage-supply basis stated. Decided without a follow-up question, which is itself weak evidence that a pre-structured choice is cheap to decide. **Not an RK-005 data point:** it approves a subject, not a publication. |
| A-1 | The publication approval (D-002) | _pending_ | _pending_ | | | | The RK-005 measurement proper. |
