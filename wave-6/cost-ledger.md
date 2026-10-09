# Wave 6 — cost ledger

**Run `run-cdce6ebe03ac` · ticket MC-7 · 2026-10-08 to 2026-10-09 · 6/6 phases, 6/6 gates.**

Host `subagent_tokens` figures. A resumed agent's figure is cumulative for that agent; the first-pass
figure is taken from its first hand-back, the rework is the delta.

## 1. Per phase

| Phase | Agent | First pass | Final (cumulative) | Rework | Resumes | Active time | Validation |
|---|---|---|---|---|---|---|---|
| 1 scope | omn-product-owner | 171,064 | 171,064 | 0 | 0 | 6m 46s | 33/33 |
| 2 planning | planner | 286,154 | 286,154 | 0 | 0 | 13m 15s | 45/45 |
| 3 design | architect | 467,264 | 467,264 | 0 | 0 | 29m 35s | 78/78 + 7 decision records |
| 4 implementation | omn-dev-1-implement | 757,795 | 816,418 | 58,623 | 1 | 70m 11s + 13m 13s | 32/32 (both passes) |
| 5 quality review | omn-dev-2-reviewer | 312,875 | 357,283 | 44,408 | 1 | 14m 30s + 5m 22s | 31/31 (both passes) |
| 6 documentation | omn-documentation | 242,093 | 242,093 | 0 | 0 | 9m 26s | 32/32 |
| **Total** | | **2,237,245** | **2,340,276** | **103,031** | **2** | **2h 42m** | **251/251, 0 rejections** |

**Rework share: 4.4 percent** (Wave 5: 13.8 percent), with **zero validator rejections** for the
third wave running. The whole of the rework is one correction cycle: two high findings, three
medium and one low from the review, fixed by the implementer and re-verified by the reviewer.
Every rework token bought a defect fixed; none was spent on a framework rejection or on
infrastructure.

**Against Wave 5: 2,340,276 tokens against 2,835,446, 17 percent less**, for a change of similar
size (67 files, 8,820 lines inserted; 46 new tests, 619 against 573). Wave 5 carried roughly two
percent infrastructure waste (an organisation spend limit and a hung container runtime); Wave 6
carried none.

## 2. Operating spend — USD 0.00, sixth wave running

Nothing was published, nothing was bought, no account and no channel was created, no metered
operation ran. The authorised single-metered-operation exception is **unspent and still available**.

## 3. Orchestration errors, counted honestly

Three of the 48 objections recorded this wave were the orchestrator's errors:

1. The decision record called the owner's per-channel list "exactly" master plan section 17.5's
   list; it differs (corrected as `CEO-C-500`).
2. A shared staging folder let the planner pick up the scope phase's result envelope; the planner
   caught and corrected it before hand-back. Every later agent had its own staging folder.
3. The implementation briefing asked for a reintroduce-and-watch-fail on a "channel-partitioned
   measurement row shape" that the approved design does not persist.

A fourth, the Planning Gate answer on the gate service's Draft transition, was partly wrong and
corrected by the architect.

## 4. Agent objections — forty-eight, all checked, all correct

Scope 8, planning 10, design 7, implementation 7 + 5, review 3 + 3, documentation 5 — some
overlapping (the test-file count was re-confirmed by four agents). The record across six waves now
stands unbroken.
