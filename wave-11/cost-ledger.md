# Wave 11 — cost ledger

**Run `run-9d83536ecd16` · ticket MC-12 · 2026-10-11 to 2026-10-12 · 6/6 phases, 6/6 gates.**

Host `subagent_tokens` figures. A resumed agent's figure is cumulative for that agent, and rework is
the delta. **Caveat this wave:** the implementer's first pass ran 5 h 42 min and its context was
compacted, so its reported first-pass figure (71,142) understates what it used; the implementer's
second cycle was stopped mid-way when the owner paused overnight and its tokens before the stop are
not reported. The implementation row is therefore a lower bound. The orchestrator's own session
tokens are not measured here.

## 1. Per phase

| Phase | Agent | First pass | Final | Rework | Resumes | Active time | Validation |
|---|---|---|---|---|---|---|---|
| 1 scope | omn-product-owner | 283,464 | 283,464 | 0 | 0 | 14m 14s | 33/33 |
| 2 planning | planner | 295,566 | 295,566 | 0 | 0 | 12m 56s | 45/45 |
| 3 design | architect | 511,656 | 511,656 | 0 | 0 | 27m 08s | 78/78 + 8 decision records |
| 4 implementation | omn-dev-1-implement | 71,142 (understated) | 454,923 (lower bound) | 383,781 | 4 | 5h 42m + 1h 16m + 1h 14m (+ stopped part) + 1h 30m | 31/31 (every pass) |
| 5 quality review | omn-dev-2-reviewer | 401,677 | 514,972 | 113,295 | 2 | 1h 53m + 1h 16m + 57m | 31/31 (every pass) |
| 6 documentation | omn-documentation | 315,043 | 315,043 | 0 | 0 | 13m 28s | 32/32 |
| **Total** | | | **2,375,624 (lower bound)** | **497,076** | **6** | **about 14h 55m of agent time, plus the stopped part** | **0 runtime rejections** |

**Rework, split by cause:**

1. **The owner's change at the contact-sheet review (121,203):** not a defect. The owner approved all
   22 graphics with one change (drop a fourth recorded source with no author from the end card); the
   implementer re-rendered and re-ran the suite.
2. **Review defects (375,873 = 262,578 implementer + 113,295 reviewer, 15.8 percent of the total):**
   one high finding needed three cycles. The number rule first admitted substrings and
   self-named sources (cycle 2 closed both), then still admitted a figure cut at a punctuation edge
   ("–15 Hz" from "13–15 Hz") and missed some number words (cycle 3 closed it with one tokenizer for
   source and quotation). One medium (module resolution outside the verified installation) and one
   low (the guide's approval-file members) closed in cycle 2.

Wave 10's rework share was 3.0 percent; Wave 9's 18.9. **Lesson:** a text-matching rule claimed to
"admit only quotations" was a completeness claim; asking for the adversarial probe list up front (the
reviewer's cuts at every token edge) would likely have saved cycle 3.

## 2. Spend

| Item | Amount |
|---|---|
| Metered spend in the wave | **USD 0.00** |
| Model calls of any kind | **none** |
| The company-store production | **none this wave** (the owner's decision of 2026-10-12; deferred to Wave 12) |
| The item cap of USD 5.95 | counted total unchanged at **USD 0.007695** (estimate) |
| Remotion licence | free tier (the owner confirmed the company has at most three people); **USD 0.00**, no licence key, no usage event sent |
| The owner-approved download | Remotion 4.0.534 packages (about 236 MB installed, 252 packages), Chrome Headless Shell 157.0.8080.0 (126 MB archive, 221 MB executable), Inter 4.1 (34 MB archive); no charge |
| Local compute | **unmeasured** (one produce of item 001 with all 22 graphics rendered twice: about 15–16 minutes on this machine) |
| Development cost (tokens above) | separate quantity, never netted against per-item cost |

## 3. Objections

**64 agent objections, all correct or accepted** — eleven waves unbroken. Scope 11, planning 11,
design 8, implementation 14 (across four passes), review 9 (plus 3 findings), documentation 11.
**At least seven were orchestrator errors:**

1. the scope briefing named a Design stage handler in the capability project that does not exist;
2. the first installation record hashed only four files, while a render loads about fifteen thousand
   (the planner's objection; the orchestrator then wrote full manifests);
3. the orchestrator's own trial render left a 170 MB bundler cache inside the installed folder
   (removed before the manifests were written);
4. the Node manifest was first written with an absolute POSIX path (the architect's objection);
5. the briefings and dispatch prompts again asked for different reply formats (third wave running);
6. the Scope Gate rationale was passed with non-ASCII characters (° and –) that the command line
   mangled in the stored record;
7. the review briefing's heredoc let a back-quoted path run as a command (caught by the
   orchestrator before dispatch, the path restored).

## 4. What it bought

- 22 graphics of item 001 drawn by code, each a JSON specification checked against the item's four
  recorded sources, rendered deterministically (every graphic twice, byte-identical, in separate
  processes) and approved by the owner with its final-frame hash.
- The renderer installation verified file by file from four manifests (15,117 files), held for the
  render, never written to; module resolution kept inside it; render network traffic loopback only.
- The owner's review command (contact sheet and preview) and the approval-file comparison the
  company-store run will use.
- Version 1.8.0, eleven schema resources, 1076 tests passing live (from 934).
