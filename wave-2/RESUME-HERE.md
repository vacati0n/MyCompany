# Wave 2 — resume point

**Paused 2026-09-26 evening at the CEO's request. Read this first.**

| Field | Value |
|---|---|
| Run | `run-3a58551ee912` (`implement-feature` v1.0.0) |
| Branch | `claude/trusting-shirley-90ea75` |
| Feature branch | `feature/mc-3-wave-2-one-complete-video-end`, worktree `D:/Project/MyCompany/.worktrees/mc-3-feature/` |
| Progress | **4 of 6 phases complete, 3 of 3 gates passed.** Phase 5 was interrupted mid-flight. |
| Always pass | `--target "D:/Project/MyCompany"` |

## Exact command to resume

Phase 5 (`quality-review`) was **dispatched and then stopped mid-run**. It left a **partial
`review-package.md` (15.8 KB) and no result envelope**, so the phase is still `running` with a
lease held and attempt 1 charged.

```
omn-agent run MC-3 --target "D:/Project/MyCompany" --show
omn-agent run MC-3 --target "D:/Project/MyCompany" --dispatch --phase quality-review --approve
```

Re-dispatching overwrites the partial artifact — that is fine and expected. If the runtime
refuses because the lease is still held, `release` it first; the failure message prints the exact
command. **Do not treat the partial artifact as work in progress — discard and redo.**

Then dispatch the `omn-dev-2-reviewer` subagent with the phase's generated `dispatch-prompt.md`,
plus the warnings in the next section.

## Phases done

| # | Phase | Agent | Tokens | Attempts | Gate |
|---|---|---|---|---|---|
| 1 | `scope-and-acceptance` | `omn-product-owner` | 257,876 | 1 | Scope Gate ✓ `omn-business-analyst` |
| 2 | `execution-planning` | `planner` | 313,533 | 1 | Planning Gate ✓ `omn-tech-lead` |
| 3 | `solution-design-and-risk-assessment` | `architect` | 645,617 | **2** | Design Gate ✓ `omn-tech-lead` |
| 4 | `implementation` | `omn-dev-1-implement` | 767,697 | **2** | *(no gate at phase 4)* |
| 5 | `quality-review` | `omn-dev-2-reviewer` | — | **interrupted** | Review Gate + Verification Gate pending |
| 6 | `documentation-and-release-handoff` | `omn-documentation` | — | — | Closure Gate pending |

**Governance total so far: 1,984,723 tokens.** Development capex, not per-video — see `C-002`.

## MUST TELL EVERY SUBAGENT

1. **`declared_side_effects` takes bare written file paths ONLY.** Prose there is misclassified as
   an undeclared write and blocks the run permanently. All notes go in `structured_output`.
2. **For the REVIEWER specifically — the defect-3 variant.** `declared_side_effects` must contain
   only files *the reviewer itself wrote*, **never the paths of the change under review**, however
   honest that feels. A prior reviewer honestly listed 18 paths the correction cycle had touched
   and was blocked for it, right after passing 31/31. The framework has nowhere to record that the
   change moved while under review. Put it in `structured_output`.
3. **Never cite an identifier bare across artifacts.** Local registers (`C-`, `T-`, `V-`, `R-`,
   `Q-`) resolve locally; upstream identifiers cited bare resolve **silently to the wrong
   definition** — this happened three times in phase 4 and no "undefined identifier" check can
   catch it, because they *are* defined, as something else.
4. **Do not put the string `claude` in any framework artifact** — including file paths and branch
   names. Check `C3.2` scans for vendor names and the branch name trips it.
5. **Write by shell redirection**, not the editor tool — it refuses the base-repository framework
   path from this worktree.
6. **Never invent a clip count.** See below.

## Phase 5 instructions, condensed

Verify rather than accept. The item's value is in whether its **honest negatives are honest**, not
whether it looks complete. Hardest looks:

- **Static check `P8`** asserts no numeric figure appears in the supply-audit count column.
  **Verify it exists, runs, and would actually fail on a fabricated count.** Most important single
  verification in the review.
- **The 343 executed checks** (315 .NET tests, 220 baseline + 95 new, against live PostgreSQL 17,
  plus 28 static). Reproduce, do not accept. `MediaCompany.slnx`; `db/README.md` has the container
  command and the two environment variables.
- **The two not-evidenceable determinations** — duplicate detection and advertiser suitability.
  Confirm neither is a disguised failure.
- **The item must NOT be publish-ready.** Three stages `Held`; `PublishReady` must have an empty
  outgoing transition set and no assembly declaring upload/publish/channel/account.
- **Runtime 13:39 is SPECIFIED, not measured** — no rendered file exists. Confirm it is never
  presented as a measurement.
- **Originality claim:** all 23 claims carried by narration and graphics; with all 18 clips
  deleted it must still read as a complete essay. Test this directly — it is the entire basis of
  the reused-content position.
- **`tools/verify_item_package.py`** recomputes the ledger quantities. Run it.

## Gates still to decide — and who may decide them

**Producer Exclusion Rule:** a gate cannot be decided by the role that produced its evidence.

- **Review Gate** — owners `omn-dev-2-reviewer`, `omn-qa`. `omn-dev-2-reviewer` produces
  `review-package.md`, so **decide as `omn-qa`**.
- **Verification Gate** — owner `omn-qa` only. **This is decidable — defect 4 is withdrawn.**
  `omn-qa` does *not* produce the phase-5 evidence (`omn-dev-2-reviewer` does), so there is no
  exclusion. The Wave 1 session proved it empirically by deciding this gate and having the runtime
  accept. **Do not fake a decision, do not invent an owner, do not treat it as blocked.**
- **Closure Gate** — owners `omn-orchestrator`, `omn-documentation`. `omn-documentation` produces
  phase 6's evidence, so **decide as `omn-orchestrator`**.

## Still owed before the wave closes

1. **RK-005, the approval-minutes measurement.** `wave-2/approval-instrument.md` is set up but
   `A-1` is unfilled. The item has **not reached the CEO approval gate**. Capture `t_presented`
   and `t_decided` separately from queue time and rework time, and carry the `n = 1` caveat — a
   single sample cannot set the clean-record threshold that would later relax per-publish approval.
2. **The CEO report, in Vietnamese** (code, identifiers, paths and framework artifacts stay
   English). Cite `C-001`–`C-004` and `D-200` by reference rather than re-deriving them.
3. **Merge the feature branch back** into `claude/trusting-shirley-90ea75` when the run closes.
4. **Report the phase-5 runtime result on the Verification Gate** to the research session — it was
   left open pending an empirical result, though the Wave 1 session has since supplied one.

## Open questions from phase 4 — five blocking

`Q-001` who obtains the clip counts · `Q-003` does a specified runtime satisfy `A-003` ·
`Q-004` stack selection before dossier persistence (`omn-tech-lead`) · `Q-005` does
not-evidenceable satisfy a determination · `Q-006` is a held item the intended terminal outcome
of the wave. (`Q-002`, the `honey bee` correction, is **closed by `D-200`**.)

Deliberately not built, safe because no caller binds to them: dossier persistence (no SQL
written, so no repeat of the `MAX()`/`uuid` class of defect), the twelve concrete stage handlers,
and the `T-022` approval surface — all pending the stack selection.

## State of the four things the wave must prove

| | Answer so far |
|---|---|
| What an item costs | **Partly.** Media exact at **$0.870880** (32.9% of the approved $2.647440). Token half unmeasurable — **no variance may be stated.** |
| Owner-approval minutes | **Not yet.** Item has not reached the gate. |
| Can the determinations be evidenced | **Yes, specifically:** 3 of 5 can before upload; 2 cannot and **both are upload-surface**. |
| What the libraries hold | **No.** Zero counts across 14 subjects; every row carries its reason and its remedy. **None invented.** |

## Identifier allocation

This session's block is **`D-200`–`D-299`**, allocated without coordinating with anyone. No `W0-`/
`W1-`/`W2-` prefixes — that scheme was withdrawn. `D-200` is taken. Next free: **`D-201`**.
