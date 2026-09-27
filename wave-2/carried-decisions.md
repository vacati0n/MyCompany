# Decisions carried into Wave 2 from other sessions

**Why this file exists.** `research/ceo-decision-record.md` has **diverged into two incompatible
numbering spaces**, and Wave 2 depends on decisions recorded in both. Until that is resolved
(see §1), every decision below is cited by **branch, commit and title** rather than by its
`D-` number, because the numbers are currently ambiguous and the titles are not.

---

## 1. The decision record forked — RESOLVED, and how

Two sessions have written five different decisions each under the identifiers `D-011` to
`D-015`. Both branches descend from the same merge-base, `849835b`, and both files present
themselves as the authoritative record that governs where it conflicts with an upstream
artifact.

| Id | On `claude/eloquent-taussig-724cc2` (Wave 1 session) | On `claude/execute-prompt-txt-4a3ee4` (CEO/research session) |
|---|---|---|
| `D-011` | Operate as an individual creator; the CEO carries the legal position personally | Tax posture: individual or household business first, company only if it works |
| `D-012` | Two-phase payee structure, and the launch channel already exists | Niche direction: animals, form not yet settled |
| `D-013` | The test channel is an experiment; the company's process is unchanged | Niche: science and the natural world, adult-framed edutainment |
| `D-014` | The two Vietnamese disclosure gaps stay carried; affiliate disclosure becomes a standing rule | Wave 2 begins, in a separate session |
| `D-015` | The first item carries no affiliate link | Science and the natural world stands, reframed as a deliberate experiment |

`D-016` (budget holds at USD 77.41) and `C-001` to `C-003` exist only on the CEO/research
branch. `D-017` (this wave's subject) was filed by the Wave 2 session on top of that branch.

### Identifier allocation — settled on the third attempt, block `D-200`–`D-299`

Three schemes were proposed in one day. **The one that stands:** a single flat `D-nnn` space, no
prefixes, with a **reserved block per session** so allocation needs no coordination.

| Session | Block |
|---|---|
| CEO / research (`execute-prompt-txt`) | `D-023`–`D-099` |
| Wave 1 (`eloquent-taussig`) | `D-100`–`D-199` |
| **Wave 2 (this session)** | **`D-200`–`D-299`** |

**This wave's next identifier is `D-200`, allocated without asking anyone.** The `W0-`/`W1-`/`W2-`
prefix scheme recorded earlier is **withdrawn and no prefixed identifier exists anywhere**;
`W0-D-018` was renumbered to `D-023`.

**What actually resolved the collision:** the Wave 1 session's `D-011`–`D-015` were renumbered to
**`D-018`–`D-022`** (commit `21e9882`, on `main`, with withdrawal markers carrying the mapping).
The CEO/research space keeps `D-011`–`D-015` unchanged and is now unambiguous. **`D-017` is
unchanged** — this wave's video subject keeps its identifier.

**Why the block reservation still earns its place after renumbering.** Renumbering fixed **the
collision that existed**; it does nothing about **the next one**. Three sessions appending to one
record with no allocation mechanism was the underlying defect, and a reserved block is the part
that survives sessions not talking — which is precisely what the first two collisions depended
on. The `branch : identifier : title` citation habit remains correct when citing across branches;
it is simply no longer *required* to resolve these five.

### RESOLVED 2026-09-26 — this branch's space stands, the Wave 1 space renumbered

The CEO decided the Wave 1 session's space moves, on the reasoning this session gave: that space
was closed, while this one has `D-016`, `D-017` and `C-001` to `C-004` stacked on top and is
still being written. The Wave 1 session renumbered **`D-011`–`D-015` → `D-018`–`D-022`**, left a
withdrawal-marker block at the old heading position rather than renaming in place, and recorded
that a withdrawn identifier is never reissued. Committed at `21e9882`, which is what `main`
points at.

| Was | Now | Decision |
|---|---|---|
| `D-011` | **`D-018`** | Operate as an individual creator; the CEO carries the legal position personally |
| `D-012` | **`D-019`** | Two-phase payee structure, and the launch channel already exists |
| `D-013` | **`D-020`** | The test channel is an experiment; the company's process is unchanged |
| `D-014` | **`D-021`** | Vietnamese disclosure gaps carried; affiliate disclosure a standing rule |
| `D-015` | **`D-022`** | The first item carries no affiliate link |

**`D-011` to `D-017` now mean unambiguously what this branch says they mean.** The no-affiliate
decision that discharges one compliance determination is cited hereafter as **`D-022`**.

### ⚠ CORRECTION — this session's "nothing contradicts" reading was wrong on one pair

This file previously asserted that the two sets were mutually compatible and that only citation
was broken. **That was true of nine of the ten decisions and false of the `D-011` pair**, and the
Wave 1 session was right to challenge it.

- **This space's `D-011`** (tax posture) states: *"`RK-003` is not discharged by this decision —
  it is narrowed. Qualified Vietnamese counsel is still required."*
- **`D-018`** records a **later** CEO decision on a fact the earlier one did not have: **there is
  no budget to retain counsel, and the CEO accepts `RK-003` personally instead.**

The mitigation "qualified local counsel before the payment account is created" is therefore
**superseded, not narrowed**. Same CEO, same subject, later decision — **`D-018` governs on that
point.** The two postures otherwise agree (individual now, company later), so nothing else moves.

**Two consequences from this space's `D-011` are carried forward and are NOT withdrawn by
`D-018`:** the **W-8BEN** point — failing to submit it as an individual triggers **24% backup
withholding on total worldwide earnings**, worse than the 30% on the US-sourced share, making it
a first-class precondition of the payment account — and the **payee-migration cost**, since
individual-to-company is a payee change rather than a settings change and its cost should be
known before the first account is created.

**Why this session got it wrong, recorded rather than quietly fixed:** the compatibility claim
was made by comparing decision *titles* and *subjects*, which is exactly the check that a
same-subject-later-decision defeats. Two decisions can share a subject, not contradict in
posture, and still have one supersede the other's mitigation. **A title-level scan is not a
conflict check**, and the resolution convention in `C-004` — cite by branch, identifier and title
— makes citation unambiguous without making supersession visible. That is a residual gap in the
convention, not a failure of it.

**Assessment (as originally written, and still true of the other nine).** These are not competing drafts of the same decisions — they are ten distinct
decisions that happen to collide on five identifiers. Nothing is lost and nothing contradicts;
the *content* of both sets is compatible and Wave 2 can act on all of it. What is broken is the
**citation**: `D-013` currently names two different decisions depending on which branch a reader
holds. That is a worse failure than a contradiction, because a contradiction announces itself
and this does not.

**The underlying defect is the missing allocation mechanism, not the collision.** This is the
third identifier collision in this one file in a single day, and the first two were caught only
because sessions happened to be in contact. Three sessions appending to one authoritative record
with no way to reserve an identifier will collide again. **This is a process finding, not a
framework one.**

### How the proposal evolved — recorded because the churn is itself the finding

`C-004` was recorded by the CEO/research session (`claude/execute-prompt-txt-4a3ee4`, commit
`029cba1`). **Cite `C-004`; this section is context, not a second record.** Its original
recommendation was *additive only — never renumber, namespace future identifiers by wave, keep a
concordance*. **Two of those three did not survive.** The CEO's clarified instruction was that
renumbering was what he meant, and the outcome is the block scheme above.

**What was withdrawn, and what it cost.** The prefix scheme (`W0-`/`W1-`/`W2-`) was adopted, this
session adopted `W2-`, and then the whole scheme was withdrawn — three numbering schemes in one
day. No prefixed identifier now exists anywhere.

**The argument against renumbering was not wrong, it was outweighed.** It held that existing
citations live in artifacts that have **already passed their gates**, so renumbering invalidates
them silently — a reader holding a gated artifact citing `D-013` gets no signal the target moved.
That is the same reasoning that left `S-006` unrewritten under `C-001`. It was answered in
practice rather than in principle: the Wave 1 session renumbered **with withdrawal markers
carrying the mapping**, so the silent-invalidation failure the argument predicted does not occur
— the marker is the signal. **A renumber that leaves a forwarding address is not the thing the
argument objected to.**

**What survives from the original proposal, and it is the part that matters.** Renumbering fixed
the collision that existed. **Block allocation fixes the next one.** Three sessions appending to
one authoritative record with no way to reserve an identifier was the underlying defect, and only
the reserved block addresses it. The `branch : identifier : title` citation habit also survives
as good practice across branches.

**Wave 2's position.** `D-017` keeps its identifier and is listed as unambiguous in the
concordance. Wave 2's run artifacts are unaffected in substance: no scope item, task or
acceptance criterion cites a colliding identifier, because the run's registers (`S-`, `A-`, `T-`,
`R-`, `Q-`) are internal to the run and do not overlap this space. Further identifiers this wave
originates come from `D-200`–`D-299`.

---

## 2. The launch channel — established facts

Source: `claude/eloquent-taussig-724cc2`, commit `0ab3d32`, "Two-phase payee structure, and the
launch channel already exists". Confirmed by the CEO.

| Fact | Value |
|---|---|
| Channel exists | **Yes** — an existing YouTube channel the CEO already holds |
| In YPP / monetization enabled | **No** |
| Existing content | **None** |
| Subscribers, qualified watch hours | **None published** |
| Active copyright or community-guidelines strikes | **None** |
| Made-for-kids at channel level | **No** |
| Two-step verification | **UNCONFIRMED** — the open half of MASTER-PLAN §21.3 precondition 3 |

**Two consequences that move Wave 2's risk position, both favourably:**

1. **`RK-006` is confirmed, not merely cautious.** The channel is *not* in YPP, so the
   grandfathering sentence — which covers YPP **membership**, not channel age — does not reach
   it. It is a new entrant for threshold purposes and the 2027-02-01 doubling applies:
   1,000 subscribers plus **8,000** qualified watch hours in 365 days. The plan already treats
   capture as lost and budgets 8,000 hours. **That position is now confirmed correct. No
   recalculation follows.**
2. **Channel-level reused-content exposure does not arise for this item.** There are no prior
   uploads inside the assessment. The originality regime's channel-level penalty is the risk the
   whole plan exists to respect, and on these facts it is **removed on the facts, not deferred**
   — for the first item. It returns the moment there is a second item, so the discipline does
   not relax.

**Bearing on the two scope items that are channel-scoped rather than item-scoped:**

- **`S-011`** — library registration is per-channel and **has not been done** for this channel.
  It remains a genuine coded precondition of publish-ready. The channel existing does not
  discharge it.
- **`S-002`** — the audience-drift monitoring obligation and the per-item audience designation
  are assessed against a channel. This one starts from a **clean designation**: no made-for-kids
  designation to contend with, and no existing audience to have drifted.

---

## 3. Disclosure — one of the five compliance determinations is discharged

Source: `claude/eloquent-taussig-724cc2`, commits `ad18503` and `7386322`.

**The first item carries no affiliate link and no sponsored placement.**

**Direct effect on `S-008`.** Of the five compliance determinations, **affiliate and
paid-promotion disclosure now resolves by recording that neither is present.** That is
**recorded evidence, not a finding that it cannot be evidenced** — the distinction matters,
because `S-008` accepts either but they are different answers and only one of them is good news.
The in-video disclosure obligation **does not arise** for this item, so there is nothing to
produce for it.

**The standing rule, which does not bind this item.** Any future item carrying an affiliate link
or sponsorship declares it in Studio **and** discloses in-video, **always, without first
assessing whether a rule requires it**. Adopted because the declaration costs nothing — the
dossier confirms disclosure does not affect reach or earnings — while omitting it on a channel
where enforcement reaches the account does. **Its condition is absent for this item; that is not
the same as it being relaxed.**

**Two Vietnamese gaps are carried deliberately and must NOT be escalated or blocked on:**
Vietnamese advertising-law disclosure obligations for sponsored or affiliate content, and any
Vietnamese rule on AI-media disclosure. Both remain unestablished. The language analysis records
that for an English-language channel aimed at Tier-1 audiences this is a **background item**,
becoming immediately load-bearing only for a Vietnamese-language channel aimed at Vietnamese
viewers. Wave 2 is the former.

---

## 4. Accountability for escalated legal determinations — answered

Source: `claude/eloquent-taussig-724cc2`, "Operate as an individual creator; the CEO carries the
legal position personally".

The company operates as an **individual creator**. The CEO is the Vietnam-resident payee in his
own name and **personally accepts accountability for tax declaration and for legal
determinations escalated out of the workforce**. `RK-003` is **accepted, not discharged**.

**This answers who is accountable for a legal determination escalated out of the workforce** —
specifically the audience designation and the disclosure judgements. The answer is the CEO.
**The workforce escalates; it does not decide.** That is the correct shape for the audience
designation in particular, where the niche analysis established that mis-declaring "not made for
kids" carries statutory exposure and that the FTC pursues the uploader rather than the platform.

---

## 5. Process boundary — what did NOT change

Source: `claude/eloquent-taussig-724cc2`, "The test channel is an experiment; the company's
process is unchanged".

The channel is an initial test channel and the company's operating process does not change. The
CEO approves channel creation; departments coordinate the remaining work among themselves.

**Boundary, recorded because it is easy to misread:** this governs **inter-departmental
coordination only**. It does **not** touch the rule that the CEO approves every publication
individually, nor the property that the approval is a state in the gate transition table and
absent from configuration. **Both stand.** "Departments work with each other" is not a
relaxation of the publication gate, and Wave 2 does not treat it as one.

---

## 6. Wave 1 is delivered — and one finding Wave 2 should heed

Run `run-dd80173faaad` is **Completed, 6/6 phases and 6/6 gates**, with 220 tests passing and 0
skipped against a real PostgreSQL 17.

**Five issues carried, not accepted:** no provider adapter has met a live provider; the secret
store is an environment-backed adapter pending the store decision; the role-permission mirror
has no runtime drift check; the accepted change account `IR-2026-0001` predates the review
corrections; and the backup/restore position must be established before the first append-only
entry is written in any real deployment.

**The finding worth carrying.** The datastore integration suite found a defect no unit test
could reach: **`MAX()` has no `uuid` form in PostgreSQL**, which broke the entire cost-recording
path and **passed 205 unit tests, a review and two gates** before a real database caught it.

**[INFERENCE — and it bears directly on this wave's credibility.]** The broken path was the
**cost-recording path** — the same mechanism Wave 2's ledger depends on, and the same one
`C-003` already records as designed-but-never-executed. That it could pass two gates while
broken is the strongest available argument that **this wave's hand-metered ledger is not a
temporary inconvenience but the only cost evidence that currently exists.** If Wave 2 adds SQL,
it runs against a real instance; `db/README.md` on the Wave 1 branch carries the container
command.
