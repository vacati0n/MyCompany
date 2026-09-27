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
| A-1 | The item approval (`D-002`), on `MC3-ITEM-001` | 2026-09-27T02:13:59Z | 2026-09-27T02:15:57Z | **1 min 58 s** | **0** — the owner was present; the artifact did not wait | **0** — approved first pass, no change requested | **1 min 58 s total.** See the reading below before using this number. |

---

## The first measurement, and what it is worth

**RK-005 = 1 minute 58 seconds.** Review 1:58, queue 0, rework 0. Approved first pass, verdict
"approve — hold at the gate".

### What this number legitimately establishes

**The instrument exists and produces a figure.** That was the real question. `D-007` specifies
owner approval as a state in the gate transition table with elapsed minutes derived from two
recorded timestamps; until now nothing had ever derived one. Wave 2 did it by hand and the
measurement is clean: two timestamps, three components separated, no reconstruction after the
fact.

### What it does NOT establish, and must not be used for

**It is a lower bound, not a steady-state figure, and it is not a distribution.** Five reasons,
all of which push the number down and none of which will hold at volume:

1. **It approved a HELD item, not a publish-ready one.** Three stages are `Held` and the terminal
   predicate refuses. **The approval `D-002` really describes — the one that releases a finished
   video to a channel — has still never been exercised.** A reviewer approving something that
   cannot ship is not carrying the same risk as one authorising a publication.
2. **The owner already knew the contents.** He chose the subject (`D-017`), was told the
   `honey bee` supply error and confirmed the corrected premise (`D-200`), and saw the findings
   as they arose. A steady-state approval is of an item the owner is meeting for the first time.
3. **The decision surface was pre-structured.** The item was presented as a summary with the
   quantities, the evidence totals, the held reasons and the three largest carried risks already
   extracted. `D-002` anticipates exactly this — "the CEO review surface must make per-video
   approval cheap" — so the low figure partly measures **the quality of the review surface**, which
   is a real and reusable gain, and partly measures that the summary did the reading.
4. **No queue time, because the owner was at the desk.** At 13 items a month the artifact will
   wait, and waiting is real workload even though it is not review effort.
5. **n = 1.** One sample, on the first item the company has produced.

### What follows for the threshold

`D-002` records that the "clean record" bar which would relax per-publication approval **must be
proposed to the CEO, not assumed**. **This measurement does not support proposing one.** It
establishes the instrument and gives the series its first point. A threshold needs a distribution
over items the owner has not pre-read, at a cadence where queueing occurs, and including at least
one approval that returned a change request — because rework time is the component that decides
whether per-item approval scales, and it is the one component still at zero here.

**Recommended:** carry the instrument into Wave 3 unchanged, measure every approval, and revisit
the threshold question no earlier than the tenth item.
