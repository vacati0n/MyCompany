# Wave 9 — cost ledger

**Run `run-7ae81c0de400` · ticket MC-10 · 2026-10-09 to 2026-10-10 · 6/6 phases, 6/6 gates.**

Host `subagent_tokens` figures. A resumed agent's figure is normally cumulative for that agent, and
the rework is the delta. **Exception:** the implementer's second correction cycle reported a fresh
count (its figures restarted below the earlier total), so that cycle is added in full.

## 1. Per phase

| Phase | Agent | First pass | Final | Rework | Resumes | Active time | Validation |
|---|---|---|---|---|---|---|---|
| 0 survey | Explore (read-only) | 105,079 | 105,079 | 0 | 0 | 2m 26s | — |
| 1 scope | omn-product-owner | 251,370 | 267,456 | 16,086 | 1 | 12m 49s + 1m 58s | 33/33 (both passes) |
| 2 planning | planner | 328,761 | 328,761 | 0 | 0 | 16m 52s | 45/45 |
| 3 design | architect | 403,653 | 403,653 | 0 | 0 | 21m 24s | 78/78 + 15 decision records |
| 4 implementation | omn-dev-1-implement | 873,995 | 1,375,706 | 501,711 | 2 | 95m 23s + 34m 46s + 81m 07s | 32/32 (all three passes) |
| 5 quality review | omn-dev-2-reviewer | 347,165 | 422,259 | 75,094 | 1 | 21m 52s + 21m 53s | 31/31 (both passes) |
| 6 documentation | omn-documentation | 228,868 | 228,868 | 0 | 0 | 9m 44s | 32/32 |
| **Total** | | **2,538,891** | **3,131,782** | **592,891** | **4** | **about 5h 20m** | **0 runtime rejections** |

**Rework share: 18.9 percent of the total** (Wave 8: 9.6; Wave 7: 17.0; Wave 6: 4.4).

The rework is three cycles, and only one of them is a defect cycle in the usual sense:

1. **Scope revision (16,086):** not a defect. The scope phase raised one blocking question; the owner
   answered four questions in one click; the agent folded the answers in.
2. **Implementation cycle 1 (62,668):** ordered by the orchestrator **before** review, on the
   implementer's own objection 2: fake-provider figures rendered as observed zero on the weekly
   lines and dashboard.
3. **Implementation cycle 2 (439,043) and the re-review (75,094):** the review's ten corrections,
   two high: the `report` command still rendered demonstration figures as observed zero (cycle 1
   fixed only some readers — its claim was wider than its fix), and the owner's unit-kind pricing
   rule was applied on admission but not on booking. Cycle 2 is the most expensive correction of the
   programme: ten corrections, a real stalled-call test and a re-run of the headline demonstration.

**Zero runtime rejections.** Wave 8's lesson held: the documentation agent wrote no repository file;
the orchestrator copied the phase artifacts into `wave-9/` in the closing commit.

## 2. Spend

| Item | Amount |
|---|---|
| Metered spend in the wave | **USD 0.00** |
| The first metered video (authorised, cap USD 5.95) | **not run** — estimate USD 0.17 for narration, labelled ESTIMATE, at a price not yet verified first hand |
| The earlier single-operation exception | unspent |
| Development cost (tokens above) | separate quantity, never netted against per-item cost |

## 3. Objections

**66 agent objections, all correct** — nine waves unbroken. Scope 10, planning 14, design 12,
implementation 15 (9 + 6), review 8 (5 + 3), documentation 7. **At least five were orchestrator
errors:** the "store named for the run" left undefined in the ticket; "rendered by code from the shot
list" against "nothing specific to the subject"; the plan premise that the controller decides on
booked spend only (the orchestrator's survey framing, corrected by the architect); the briefing and
the dispatch prompt asking different reply formats; and the documentation briefing asking for the
exact credential variable names in a framework artifact, where the vendor-token check and framework
defect 6 forbid them.

## 4. What it bought

- A production pipeline that turns item 001's recorded package into a verified video file — 1920 by
  1080, 30 fps, h264 and aac, 665.77 s measured from the file, decoded end to end with zero errors,
  byte-identical across three independent runs — at USD 0.00 against fake providers.
- A real speech-vendor adapter proven against recorded fixtures and a stub, a durable worst-case
  reservation under a per-item cap that fails closed, metered mode refusing every unmet precondition
  before any call, and every fake figure labelled demonstration in every host reader.
- 857 tests live (Wave 8: 736), architecture suite 80 (Wave 8: 63), version 1.6.0, nine schema
  resources.
