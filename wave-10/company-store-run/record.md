# Wave 10 — record of the company-store run of item 001 with the in-house voice

**Run by the orchestrator on 2026-10-10, on the owner's explicit go given in the session after the
Verification Gate**, at the feature head `1ed872a` (version 1.7.0), against the company store
`mediacompany`. Logs and the settings used are in `wave-10/company-store-run/`. This record is the
evidence for the scope's criteria of the post-phase run (scope criteria 42, 50, 51 and 52).

## Steps, in order

| # | Step | Result |
|---|---|---|
| 1 | Backup of the company store (`pg_dump -Fc`) | `C:/Users/vuhoangcao/MediaCompanyRun/backup/mediacompany-20261010T125616Z-before-own-voice-run.dump`, 211,362 bytes |
| 2 | Readings before | 69 tables; rows in 27 tables (the statistics view's live-row counts), among them 1 operation (`agent_costs`, USD 0.007695 estimate), 1 production version (version 2, Metered, failed at Audio), 22 production artifacts, 34 audit entries |
| 3 | Run settings written outside the repository from the sample | `settings.company.json`; every expected SHA-256 equals the owner's decision record (`CEO-D-902`) and the Design Gate's first-hand values; output root `C:/Users/vuhoangcao/MediaCompanyRun/output-company` |
| 4 | `install --from 10`, twice | exit 0 both times, "applied the ordered resources from 10 to 10; nothing before 10 was applied"; 73 tables after the first application, 73 after the second |
| 5 | `verify-model` | exit 0; every configured file equal to its expected hash; 1,832 record entries verified, 863 listed without a hash; no process started |
| 6 | `produce --mode plan-only` (twice: once with the demonstration output root, once with the company output root) | exit 0; store designated Company; every one of 13 parts `InHouseModel`; per-part lines identical to the demonstration run's plan; "no metered spend is planned, USD 0.00"; cap counted total USD 0.007695 |
| 7 | `produce --mode own` | **exit 0 in 328 s**; item version 3 opened (mode Own, source InHouseModel) after the failed version 2 |
| 8 | Readings after | 73 tables; still exactly 1 operation in `agent_costs` (USD 0.007695 estimate); production versions 2 and 3; 13 narration provenance rows; 14 narration measurements (13 parts and the joined narration); 83 production artifacts; 108 audit entries |

## The rendered video

- `C:/Users/vuhoangcao/MediaCompanyRun/output-company/MC3-ITEM-001/v3/production/MC3-ITEM-001-v3.mp4`
- 9,098,282 bytes, SHA-256 `ce8fe1d4b0a17125b83a07816466798032ad6b0d25169adc7012815ad846e42a`
  (recomputed by the orchestrator over the file after the run: equal)
- Probed by the orchestrator: exactly one video stream (h264, 1920×1080, 30 fps) and one audio stream
  (aac); container duration 629.733 s; decoded end to end with **0** error lines.
- Runtime **10 min 29.7 s** measured from the file, beside the specified 13 min 39 s.
- 22 text-card stills, 18 labelled clip placeholders, no music; the Production stage **held**
  (clips 05, 06, 07 specified but not sourced); the Thumbnail stage held (thumbnail 03 a
  placeholder). **Nothing was published, uploaded or configured; the item stays held short of
  publish-ready.**

## The narration

From the in-house model in every part: Piper `piper-tts` 1.8.0 (GPL-3.0-or-later, read from its
package metadata), voice `en_GB-cori-high`, the voice's default settings passed explicitly (length
scale 1, noise scale 0.667, noise width 0.8, sentence silence 0 s, volume 1, the runtime's own output
normalisation on). Part 1 was generated twice with identical settings and gave two different hashes
(`b912c104…1e55`, `fcccc50d…f45b`): **not observed to repeat**, as the owner expected when choosing
the defaults.

| Beat | Measured (s, decoded) | Expected (s, 150 wpm) | Difference (s) | Loudness (LUFS) |
|---|---|---|---|---|
| 1 | 28.630 | 38.0 | -9.370 | -14.8 |
| 2 | 46.486 | 58.4 | -11.914 | -16.2 |
| 3 | 35.097 | 44.0 | -8.903 | -15.6 |
| 4 | 79.610 | 97.2 | -17.590 | -15.3 |
| 5 | 47.392 | 61.2 | -13.808 | -15.6 |
| 6 | 31.196 | 36.8 | -5.604 | -15.7 |
| 7 | 70.194 | 87.2 | -17.006 | -15.4 |
| 8 | 9.868 | 10.4 | -0.532 | -15.2 |
| 9 | 75.674 | 90.8 | -15.126 | -15.9 |
| 10 | 37.105 | 45.6 | -8.495 | -15.6 |
| 11 | 42.643 | 50.4 | -7.757 | -15.7 |
| 12 | 34.656 | 42.0 | -7.344 | -16.5 |
| 13 | 91.173 | 109.6 | -18.427 | -15.1 |
| **Whole** | **629.725** | **771.6** (the script prints 12 min 51 s) | **-141.875** | **-15.6** |

The differences are reported and were not acted on: no stretch, pad, trim or level change. The
voice reads about 184 words a minute against the script's assumed 150.

## The scope's criteria for this run

| Criterion | Result |
|---|---|
| 42: plan-only names the in-house model for every part, the hashes verified, USD 0.00 planned | **met** (step 6) |
| 50: exit 0, one rendered video decoded with zero errors, the in-house model in every part, the item held short of publish-ready | **met** (steps 7 and the video above) |
| 51: USD 0.00 metered spend, the cap's counted total unchanged at USD 0.007695 estimate, zero network requests | **met** for spend and the cap (one operation before and after). Network: **structural evidence, labelled as such** (the Planning Gate's ruling): the own production constructs no network client (architecture-tested) and the model was given explicit local file paths only (`-m`, `-c` and the text file; command line recorded with every provenance row); no observation of the process's network traffic was made |
| 52: the narration artifacts carry the provenance of Piper 1.8.0 and `en_GB-cori-high` with the installation hashes, and their measured durations and loudness | **met** (13 provenance rows, 14 measurement rows; the table above) |

## Also run first, in the demonstration store

The real model ran once in `mediacompany_demo` before this run (the Design Gate's ruling): exit 0 in
327 s, video 634.233 s, SHA-256 `cd406a0cdd6d5274d136cb4743b957b855325e0ad1d04dbb0755dd0e08dfda5b`,
0 operations, USD 0.00. Logs `demo-real-plan.log` and `demo-real-own.log` in the same folder; that
store's schema has since been dropped by the live test suite, as expected.
