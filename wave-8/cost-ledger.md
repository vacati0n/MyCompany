# Wave 8 — cost ledger

**Run `run-79ce8c121936` · ticket MC-9 · 2026-10-09 · 6/6 phases, 6/6 gates.**

Host `subagent_tokens` figures. A resumed agent's figure is cumulative for that agent; the
first-pass figure is taken from its first hand-back, the rework is the delta.

## 1. Per phase

| Phase | Agent | First pass | Final | Rework | Resumes | Active time | Validation |
|---|---|---|---|---|---|---|---|
| 1 scope | omn-product-owner | 204,326 | 204,326 | 0 | 0 | 9m 08s | 33/33 |
| 2 planning | planner | 295,981 | 295,981 | 0 | 0 | 12m 00s | 45/45 |
| 3 design | architect | 370,569 | 370,569 | 0 | 0 | 17m 05s | 78/78 + 10 decision records |
| 4 implementation | omn-dev-1-implement | 810,615 | 938,778 | 128,163 | 2 | 77m 37s + 21m 04s + 10m 53s | 32/32 (all three passes) |
| 5 quality review | omn-dev-2-reviewer | 345,748 | 447,409 | 101,661 | 2 | 13m 09s + 9m 04s + 9m 52s | 31/31 (all three passes) |
| 6 documentation | omn-documentation | 249,422 | 260,287 | 10,865 | 1 | 12m 48s + 1m 37s | 32/32 (both invocations) |
| **Total** | | **2,276,661** | **2,517,350** | **240,689** | **5** | **3h 14m** | **1 runtime rejection** |

**Rework share: 9.6 percent** (Wave 7: 17.0; Wave 6: 4.4; Wave 5: 13.8).

**One runtime rejection, the first in five waves, and it was the orchestrator's.** The documentation
artifact passed its validator 32/32, but the runtime refused the hand-back because the
orchestrator's briefing had told the agent to write and commit `db/README.md`, a path outside the
documentation role's write scope. The agent objected in its own report. Cleared by a recorded policy
exception and a second invocation (10,865 tokens). The exception could not let the evidence through
on its own: recorded as framework finding 12.

The rework is two correction cycles plus that re-issue:

1. **First cycle (eight findings: four medium, four low; no high, the first wave without one).** The
   CTO label and the router counting comparable runs over different route sets; a re-verification
   count reading observed zero when nothing was ever re-checked; the brief growing without bound,
   against the 15-minute reading; a missing round-trip check; four low.
2. **Second cycle (two low).** The first cycle's selection change could skip a hold-and-escalate
   route for a metered provider once evidence exists — reversed; the new console runner missing from
   the build's boundary scan.

**Against Wave 7: 2,517,350 tokens against 2,912,242, 13.6 percent less**, for a change of similar
size (41 files, 6,840 lines inserted before the closing records; 46 new tests, 736 against 690). No
infrastructure waste: Docker and the database container were up; no spend limit was met.

## 2. Operating spend — USD 0.00, eighth wave running

No model call of any kind, nothing published, nothing bought, no account and no channel created, by
the owner's decision `CEO-D-700`. The authorised single-metered-operation exception is **unspent and
still available**. The benchmark corpus is unpopulated.

## 3. Orchestration errors, counted honestly

Five of the 53 objections recorded this wave were the orchestrator's errors:

1. `CEO-D-701` and the ticket cited master plan section 18 for the ten comparable runs; it is
   section 16. Caught by the scope agent; corrected as `CEO-C-700`.
2. The Scope Gate tied the report read to a 60-second admission hold. Holds can last longer (the
   120-second recovery timeout, a lost client). Caught by the planner; the binding rule became
   "never wait on any hold of any length". **The timeout check the Wave 7 lesson demanded was made,
   and was still incomplete: it weighed one clock, not all of them.**
3. The Design Gate required every source to be filtered at the report instant; sources with no
   datastore-stamped instant cannot be, and filtering on a caller instant would let a skewed process
   clock drop rows. Caught by the implementer.
4. The documentation briefing asked the note to say every weekly reading in the company store reads
   unmeasured or observed zero; the company store has no tables, so no reading exists there.
5. The documentation briefing ordered a repository write outside the role's scope — the runtime
   rejection above.

## 4. Agent objections — fifty-three, all checked, all correct

Scope 8, planning 10, design 7, implementation 7 + 4 + 2, review 4 + 3 + 1, documentation 7.
Eight waves, and the record stands unbroken.
