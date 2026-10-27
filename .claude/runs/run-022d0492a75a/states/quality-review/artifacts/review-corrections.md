# MC-17 review corrections — run-022d0492a75a

Recorded by the orchestrator after the quality review (verdict approve-with-corrections).

| CR | Finding | Resolution | Evidence |
|---|---|---|---|
| CR-001 (blocking) | F-001 in-process recording admission | CEO decided CEO-D-1312 "Giữ báo sớm": keep the read-only early admission, amend D-001 / CEO-D-1311 item 1. Admission is serialised with produce (wait, then `ledger-busy` / `recording-check-busy`). | ef327e9; T-031, T-020 |
| CR-002 | F-002 live check covers the check verb's fields (V-007) | Not changed. Known issue for the release note: the live warning covers the fields the check verb scans; the check verb at step 1 save and produce stays authoritative. | — |
| CR-003 | F-003 screenshot names | Uploads matched against declared names. | ef327e9; T-014 |
| CR-004 | F-004 produce job stuck running | Any exception ends the job as failed. | ef327e9; T-032, T-033 |
| CR-005 | F-005 verify path | Taken from the package manifest. | ef327e9; T-024 |
| CR-006 | F-006 missing tests | T-033 (produce fault), T-034 (shortcut-failed). Exit-4 start-up check left untested: reaching it needs a seam to bind off 127.0.0.1, which would weaken the guarantee; the bind address is fixed in code. Known issue. | ef327e9 |

Also fixed: the step-4 status line and the checklist `**` markup found in the Browser-pane demo (T-030), and a socket-harness false positive from process-id reuse (T-035).

Open questions closed by the orchestrator:
- Q-002 (60 s): browser takes are cut to 60.000 s at conversion per approved D-002; uploaded files over 60 s are still refused by the toolchain. Accepted.
- Q-003 (who presses approve): covered by CEO-D-1310 (employee may press on the CEO's written instruction) and CEO-D-1311 item 4 (approver texts); setup is on the CEO's own machine. Accepted.

Verification after corrections: build 0 warnings / 0 errors; MediaCompany.Pilot.Tests 251/251; MediaCompany.Pilot.Web.Tests 36/36 twice (180 s, 185 s). `src/MediaCompany.Pilot` unchanged since main...bb5adb4~1.
