# Wave 7 — cost ledger

**Run `run-56bcc037b633` · ticket MC-8 · 2026-10-09 · 6/6 phases, 6/6 gates.**

Host `subagent_tokens` figures. A resumed agent's figure is normally cumulative for that agent; the
first-pass figure is taken from its first hand-back, the rework is the delta. **One exception:** the
implementer's second resume reported 238,993 tokens, which it described as that cycle's own use
("about 235k this cycle") rather than cumulative; it is added in full, not as a delta.

## 1. Per phase

| Phase | Agent | First pass | Final | Rework | Resumes | Active time | Validation |
|---|---|---|---|---|---|---|---|
| 1 scope | omn-product-owner | 237,983 | 237,983 | 0 | 0 | 11m 22s | 33/33 |
| 2 planning | planner | 290,318 | 290,318 | 0 | 0 | 13m 57s | 45/45 |
| 3 design | architect | 502,220 | 502,220 | 0 | 0 | 30m 10s | 78/78 + 8 decision records |
| 4 implementation | omn-dev-1-implement | 823,259 | 1,197,930 | 374,671 | 2 | 85m 56s + 27m 52s + 31m 52s | 32/32 (all three passes) |
| 5 quality review | omn-dev-2-reviewer | 316,468 | 437,508 | 121,040 | 2 | 15m 18s + 6m 19s + 7m 34s | 31/31 (all three passes) |
| 6 documentation | omn-documentation | 246,283 | 246,283 | 0 | 0 | 10m 04s | 32/32 |
| **Total** | | **2,416,531** | **2,912,242** | **495,711** | **4** | **4h 00m** | **all passed, 0 rejections** |

**Rework share: 17.0 percent** (Wave 6: 4.4; Wave 5: 13.8), with **zero validator rejections** for
the fourth wave running (the architect's own first validator run failed one wording check and was
corrected before hand-back; nothing reached the runtime rejected).

The rework is two correction cycles, and every token of it bought a defect fixed:

1. **First cycle (seven findings: three high, one medium, three low).** Double booking on a lost
   commit reply and a cost lost to a late cancellation; concurrent admissions together passing a
   budget; delivered readings summing unstated costs as observed; one clock but not one snapshot;
   three low.
2. **Second cycle (three findings, one high).** The first cycle's serialisation ruling — the
   orchestrator's — made admissions wait on a lock under a 30-second command timeout while a holder
   could keep it across a 60-second provider call. Fixed by never waiting; two low findings rode
   along.

**About half the rework (the second cycle, roughly 290,000 tokens with re-verification) traces to
an orchestrator ruling** that did not weigh the command timeout. That is the most expensive
orchestrator error in the programme so far.

**Against Wave 6: 2,912,242 tokens against 2,340,276, 24 percent more**, for a change of similar
size (56 files, 8,244 lines inserted before the closing records; 71 new tests, 690 against 619).
The difference is the second correction cycle. No infrastructure waste: the database container had
stopped and was restarted before the first dispatch; no spend limit was met.

## 2. Operating spend — USD 0.00, seventh wave running

No metered call, nothing published, nothing bought, no account and no channel created, by the
owner's decision `CEO-D-600`. The authorised single-metered-operation exception is **unspent and
still available**. The benchmark corpus is unpopulated.

## 3. Orchestration errors, counted honestly

Three of the 67 objections recorded this wave were the orchestrator's errors:

1. The Scope Gate ruled that "the configured ordering always applies" until the owner names an
   observation count; read literally it made the scope's own evidence-ranking criterion
   impossible. The planner caught it; corrected at the Planning Gate.
2. The Design Gate required the admission transaction to be "bounded by the request's timeout";
   the request carries only a multi-hour hold timeout. The implementer caught it; the bound in
   force is the 60-second client timeout.
3. The first review ruling serialised admissions by waiting on a lock, without weighing the
   30-second command timeout. The reviewer caught it as a new high finding; it cost the second
   correction cycle.

## 4. Agent objections — sixty-seven, all checked, all correct

Scope 11, planning 10, design 10, implementation 6 + 6 + 6, review 5 + 4 + 3, documentation 6.
Seven waves, and the record stands unbroken.
