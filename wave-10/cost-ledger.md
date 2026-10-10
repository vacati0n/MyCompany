# Wave 10 — cost ledger

**Run `run-dfcae1756c32` · ticket MC-11 · 2026-10-10 · 6/6 phases, 6/6 gates.**

Host `subagent_tokens` figures. A resumed agent's figure is cumulative for that agent, and the rework
is the delta. The orchestrator's own session tokens are not measured here.

## 1. Per phase

| Phase | Agent | First pass | Final | Rework | Resumes | Active time | Validation |
|---|---|---|---|---|---|---|---|
| 1 scope | omn-product-owner | 231,369 | 244,061 | 12,692 | 1 | 9m 51s + 1m 39s | 33/33 (both passes) |
| 2 planning | planner | 325,538 | 325,538 | 0 | 0 | 15m 52s | 45/45 |
| 3 design | architect | 500,859 | 500,859 | 0 | 0 | 23m 44s | 78/78 + 11 decision records |
| 4 implementation | omn-dev-1-implement | 785,894 | 821,916 | 36,022 | 1 | 126m 01s + 18m 11s | 32/32 (both passes) |
| 5 quality review | omn-dev-2-reviewer | 399,135 | 427,836 | 28,701 | 1 | 27m 40s + 43m 06s | 31/31 (both passes) |
| 6 documentation | omn-documentation | 259,064 | 259,064 | 0 | 0 | 11m 27s | 32/32 |
| **Total** | | **2,501,859** | **2,579,274** | **77,415** | **3** | **about 4h 37m** | **0 runtime rejections** |

**Rework share: 3.0 percent of the total** (Wave 9: 18.9; Wave 8: 9.6; Wave 7: 17.0) — the lowest
of the programme.

1. **Scope revision (12,692):** not a defect. The scope phase raised six owner questions; the owner
   answered four in one click; the agent folded the answers in.
2. **Implementation cycle 1 (36,022) and the re-review (28,701):** the review's five findings, none
   critical or high: one medium (an installed-files record hashed from one read and parsed from a
   second, so a record swapped between the two would be verified as matching) and four low (the
   sample's fake-mode voice, an unreachable command, two missing steps in the guide, and the sample
   carrying the expected hashes, ruled no change).

**Wave 9's lesson held:** when the implementer claimed the one-read fix covered every file, the
orchestrator required the list; the implementer listed eleven files and the reviewer checked each
read site of the verifier against it. Nothing was missed, and the correction cost about 65k tokens
against Wave 9's 514k.

## 2. Spend

| Item | Amount |
|---|---|
| Metered spend in the wave | **USD 0.00** |
| The company-store production of item 001 (own voice) | **USD 0.00**, no operation booked |
| The item cap of USD 5.95 | counted total unchanged at **USD 0.007695** (estimate, the attempt of 2026-10-10 morning) |
| The one owner-approved download | the voice runtime, its six dependencies and the voice files, about 180 MB, no charge |
| Local compute | **unmeasured** (about 5.5 minutes of wall-clock time per production of item 001 on this machine) |
| Development cost (tokens above) | separate quantity, never netted against per-item cost |

## 3. Objections

**51 agent objections, all correct** — ten waves unbroken. Scope 10, planning 10, design 13,
implementation 6, review 4, documentation 8. The review raised five findings besides. **At least
five were orchestrator errors:** the implementation briefing asked the settings sample to carry the
expected hashes while the orchestrator's own Planning Gate ruling placed them outside the repository;
the Planning Gate's hashed set omitted the base interpreter that the environment's launcher actually
starts; the planning briefing asked for an install-and-verify step that the owner's mid-phase
decision had already made unnecessary; the briefing and the dispatch prompt asked for different reply
formats (again, as in Wave 9); and the download size reached the release note only from a briefing.
The orchestrator also first told the owner the narration was "about 1,500 words"; its own count
(1,929) corrected that before any agent was briefed.

## 4. What it bought

- **Item 001 exists as a real video in the company's records** — narrated by a voice model running
  on the company's own machine, at USD 0.00, with no vendor and nothing leaving the machine:
  10 min 29.7 s, 1920×1080, decoded end to end with zero errors, every narration part traced to the
  model, its version, its licences, the voice, its settings and the hash of every file loaded.
- The path for the CEO's own recording: register 13 files with the release record; the next
  production uses them in place of the model voice with no code change.
- The vendor route barred by the owner's recorded decision, not by a setting; a vendor's refusal now
  records its status and screened reason.
- 934 tests live (Wave 9: 857), architecture suite 88 (Wave 9: 80), version 1.7.0, ten schema
  resources.
