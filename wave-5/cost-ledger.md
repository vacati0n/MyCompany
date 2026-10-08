# Wave 5 — cost ledger

**Run `run-423e990b743d` · MC-6 · started 2026-09-27, closed 2026-10-08 · Completed, 6/6 phases, 6/6 gates.**

---

## 1. Wave 5, per phase

Host `subagent_tokens` figures. A resumed agent's figure is cumulative for that agent (confirmed in
Wave 4); the rework is the delta between the first-pass figure and the final one.

| Phase | Agent | First pass | Final (cumulative) | Rework | Resumes | Active time | Validation |
|---|---|---|---|---|---|---|---|
| 1 `scope-and-acceptance` | `omn-product-owner` | 224,881 | **224,881** | 0 | 0 | 12m 20s | 33/33 PASS |
| 2 `execution-planning` | `planner` | 270,015 | **270,015** | 0 | 0 | 24m 03s | 45/45 PASS |
| 3 `solution-design-and-risk-assessment` | `architect` | 547,651 | **615,580** | 67,929 | 2 | 40m 07s + 2m 50s | 78/78 PASS, re-run 78/78 |
| 4 `implementation` | `omn-dev-1-implement` | 694,731 | **903,930** | 209,199 | 4 | 76m 28s + 60m 29s + 70m 52s | 32/32 PASS, re-run 32/32 |
| 5 `quality-review` | `omn-dev-2-reviewer` | 437,829 | **551,084** | 113,255 | 1 | 41m 29s + 15m 23s | 31/31 PASS, re-run 31/31 |
| 6 `documentation-and-release-handoff` | `omn-documentation` | 269,956 | **269,956** | 0 | 0 | 11m 38s | 32/32 PASS |
| | **TOTAL** | 2,445,063 | **2,835,446** | **390,383** | 7 | **5h 56m** | **251/251, 0 rejections** |

**Every phase passed validation on its first framework attempt — six of six, for the second
wave running.** The runtime records `1/3` attempts everywhere because every correction was a
host-level resume of the same invocation. Three of the seven resumes were not rework at all:
two were forced by an organisation spend limit that killed the architect and the implementer
mid-correction on 2026-09-28 (HTTP 429), and one was the implementer blocked for an hour
because Docker Desktop had hung with its WSL virtual machine stopped. The tokens those three
resumes consumed are inside the implementation and design deltas and cannot be separated from
the useful rework; the implementer's interim hand-back at 885,012 bounds the Docker wait at
under 19,000 tokens, and the two 429 deaths each cost one context reload.

**Wall clock is not a cost figure this wave.** The runtime reports 266 hours elapsed because
the run was paused twice — once by the owner on 2026-09-27 because the phases run long, once by
the spend limit. Agent-active time is the honest figure: **5h 56m**, against Wave 4's 2h 44m.

---

## 2. Rework — the share went up again, and again it bought something

| | Wave 2 | Wave 3 | Wave 4 | **Wave 5** |
|---|---|---|---|---|
| Total tokens | 1,836,117 | 1,930,677 | 1,859,405 | **2,835,446** |
| Rework | 127,908 | 137,333 | 183,885 | **390,383** |
| **Rework share** | 7.0% | 7.1% | 9.9% | **13.8%** |
| Phases passing validation first time | 2 / 6 | 4 / 6 | 6 / 6 | **6 / 6** |
| Rework caused by validator rejection | most | most | none | **none** |
| Real defects the rework fixed | – | – | 2 | **1 high, 1 medium, 2 low** |

Composition of the 390,383: the architect's design amendment (67,929), the implementer's
correction cycle (209,199) and the reviewer's re-verification (113,255).

**Read the share against the rejection count, never alone.** Zero validator rejections; every
token of rework bought a verified fix. The high finding was the central measurement property
failing in the round trip — a period could read as *observed zero* and then gain entries — in a
direction the scope, the plan, the design, the implementation and the orchestrator's briefing
had all missed. That is the same shape as Wave 4's high finding (an observed zero stored as
NULL), one layer deeper: Wave 4 made the collapsed row unwritable; Wave 5 made the late entry
unwritable. The rework is larger because the fix was larger: a new schema resource, a datastore
check binding every writer, a non-waiting reader that cannot deadlock, and four new
concurrency demonstrations — then verified under six concurrent writers over 1,162 units.

**The programme is 32% more expensive than its previous most expensive wave** and that is the
first time a wave's cost has moved materially. Two causes, both visible: the implementation was
the largest of any wave (38 files, 84 new tests, 573 against 489), and the correction cycle
touched the schema. Neither is waste. What *was* waste — the two 429 deaths and the Docker
hour — is bounded above at roughly 2% of the total and is an infrastructure cost, not a
framework or an agent cost.

---

## 3. Operating spend — USD 0.00, fifth wave running

| Item | Authorised | Spent | Position |
|---|---|---|---|
| Monthly envelope | USD 77.41 | USD 0.00 | untouched |
| Single-metered-operation exception | one operation | none | **unspent, still available** |
| Second stock library | not authorised | – | – |
| Narration, clips, channel, accounts | owner-reserved | none | undischarged |

The served reasoning tier reached a recorded operation on the **fully composed production
path** this wave, over a non-provider route that states a tier, at zero units and zero cost. The
exception was neither drawn on nor depended on by any in-scope item — the scope agent
established that before planning began. **The assumed reasoning-tier split is now testable by a
production series and still untested**: the fourth consecutive wave closing with that true, and
the first in which the reason is the absence of a series rather than the absence of a column.

---

## 4. What the figures do not say

- **No per-item cost moved.** The USD 2.647440 variable and USD 5.95 all-in figures from option
  O-002 are unchanged and still rest on the assumed split, labelled as such.
- **The approval-minutes series still does not exist.** One measurement, 1 min 58 s, on a held
  item. No threshold is derivable and none was derived.
- **The clean-record threshold is still the owner's** and is not closer.
- **No clip count exists.** Zero were obtained across 14 subjects; none was invented.

---

## 5. Agent objections — twenty-six, all correct

| Phase | Objections | Of which orchestrator errors caught |
|---|---|---|
| scope | 4 | 1 (the split's figures appear in no supplied input) |
| planning | 3 | 2 (research paths did not exist at the base checkout; defect numbering) |
| design | 9 | 2 (file counts stale; "list two files" contradicted the output contract) |
| implementation | 5 + 4 | 2 (editor-tool claim false; "writes no record" imprecise) |
| review | 2 | 1 (approval components are computed, not persisted) |
| documentation | 5 | 1 (Verification Gate wording on the thirteenth criterion) |
| **total** | **32 raised, 26 distinct** | **9** |

Every one was checked rather than defended, and every one was right. The record across five
waves is unbroken.
