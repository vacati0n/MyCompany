# MC-4: Wave 3: the publishing capability, built and exercised short of upload

- Source: local://MASTER-PLAN.md#35-development-waves
- Issue type: Story   Priority: High
- Labels: implement, wave-3, publishing, gate
- Routed as: /implement (feature-request)

## Description

WAVE 3 — PUBLISHING CAPABILITY, BUILT AND EXERCISED, WITHOUT PUBLISHING

Wave 0 (`run-258e0a3415d2`), Wave 1 (`run-dd80173faaad`) and Wave 2 (`run-3a58551ee912`) are all Completed. Wave 3 builds on the Wave 1 foundation and the Wave 2 production path and must not re-create either. The Wave 2 item `MC3-ITEM-001` exists, is held short of publish-ready, and is the item this wave carries.

### GOAL

Build the complete publishing capability named in MASTER-PLAN section 35 for Wave 3 — platform integration, scheduling, metadata, thumbnails, captions, the publishing gate state machine carrying the mandatory owner-approval step, its approval-minutes instrumentation, and idempotent upload — and exercise all of it end to end against a boundary that stops immediately before the upload call.

### WAVE 3 DOES NOT PUBLISH. THIS IS A CEO DECISION, NOT A DEFAULT.

Decided by the CEO on 2026-09-27 when Wave 3 was authorised: the wave builds the machinery and stops short of upload.

The reasoning, recorded so a later reader does not mistake it for timidity: the three coded preconditions of first publication at MASTER-PLAN section 21.3 are outside the workforce's reach by construction — each needs the owner to sign in, verify identity, or open an account. Building the machine first and publishing later is one command, not another wave. The reverse order would have the wave block on work no agent can do.

Therefore: no upload, no channel creation, no account creation, no spend, no purchase, no subscription session. The upload path must be built, tested, and structurally prevented from firing.

### THE THREE CODED PRECONDITIONS — none discharged, all owner-held

MASTER-PLAN section 21.3 requires these before anything goes out. Wave 3 must encode and verify them; it cannot discharge them.

1. The channel is registered on every music and stock library. NOT DONE. Revenue lost before registration is unrecoverable, with no reimbursement (`RK-008`).
2. The payee position is settled and the payment account exists. The payee position is settled — the CEO operates as an individual creator and is the Vietnam-resident payee (`D-018`), accepting `RK-003` personally. The payment account does not yet exist. One account per payee name; duplicates are disapproved and monetization is turned off for the associated channel. This precedes channel creation, not merely publication.
3. Two-step verification is enabled and there is no active community-guidelines strike. No strikes — confirmed. Two-step verification is UNCONFIRMED.

Each must be a coded precondition that refuses, evaluated from recorded state, never assumed and never defaulted to satisfied. An unverifiable precondition resolves as NOT satisfied.

### THE CHANNEL, established

An existing YouTube channel the CEO already holds: not in YPP, no published content, no subscribers or watch hours, no active strikes, not made-for-kids at channel level. Because it carries no prior uploads, channel-level reused-content exposure does not arise for the first item — it returns at the second, so the discipline does not relax.

`RK-006` is confirmed on the facts rather than as caution: the channel is not in YPP, so the grandfathering sentence — which covers YPP membership, not channel age — does not reach it. It is a new entrant. Plan for 8,000 qualified watch hours, not 4,000.

### BINDING CONSTRAINTS CARRIED FORWARD

1. Owner approval is a state in the gate transition table and absent from configuration (`D-002`, `D-007`). It is not a setting and implementation must not make it one. It relaxes only by an explicit, recorded, later CEO decision.
2. Approval-minutes instrumentation is first-class. `RK-005` has exactly one measurement — 1 min 58 s, recorded in `wave-2/approval-instrument.md` — and that measurement is a lower bound, taken on a held item by an owner who already knew its contents, with zero queue time and zero rework. The clean-record threshold that would relax per-publication approval must not be derived from it. Wave 3 must measure every approval and separate review time, queue time and rework time.
3. Idempotent upload. An upload that is retried must not produce a second video. Exactly-once is a property of the state change and the queue entry committing together.
4. Publishing is auditable (`R-027`): what was published, where, when, with which metadata and settings, and on whose approval.
5. Adult framing remains a hard production rule. The nine treatment conditions are gate conditions, not guidance.
6. Budget USD 77.41 per month, unchanged. The second stock library at plus USD 16.50 is not authorised (`D-016`). Re-fetch every unit price immediately before it is applied (`RK-002`); next policy re-verification due 2026-10-26.
7. Code first, AI when necessary. Deterministic tasks must be structurally incapable of a model call, verified by the build-time boundary test.
8. The publishing capability must be niche-agnostic and re-pointable. The niche is a recorded experiment, not the company's final business bet. Subject, pillars, treatment conditions and library set are values the company holds and can change.

### WHAT THIS WAVE MUST PROVE

- That the upload path is structurally prevented from firing, not merely unused — demonstrated, not asserted.
- That every one of the three section 21.3 preconditions refuses from recorded state, including when the state is unknown rather than false.
- That owner approval cannot be bypassed: not by configuration, not by a second approval bound to a different version of the item, not by retry.
- That upload is idempotent under retry, crash and duplicate dispatch.
- The real approval-minutes series, with review, queue and rework time separated.
- Whether the two compliance determinations Wave 2 could not evidence become evidenceable at the upload surface — exact and near-duplicate detection, and advertiser-suitability self-rating. Both were recorded as not evidenceable because Wave 2 does not publish. Wave 3 builds that surface without using it, so establish how far each can be evidenced short of upload, and record honestly what still cannot be.

### CARRIED IN FROM WAVE 2, NOT RESOLVED

- Supply is unestablished for both the species and the behaviour (`D-200`). Zero clip counts were obtained across 14 audited subjects; the committed library needs an authenticated subscription. Three waggle-run clips are unsourced and no claim depends on them — the exposure is structurally bounded, not absorbed. No clip count may be invented under any circumstances.
- `R-007` — the supply control compares the package against itself, so a subject the script should call for but does not remains invisible to it.
- Five blocking open questions from Wave 2's implementation, and 24 known issues in its release note.
- `T-023` is recorded not-run, not a predicted pass.
- `C-003` — no tier routing exists anywhere; the resolution boundary that reads a reasoning tier is built and has never executed. Every figure derived from the 290,000/87,000 split, including USD 2.647440 per item and the USD 77.41 envelope, is an assumption rather than a measurement until it runs. Making the boundary record the tier actually served per operation should be an explicit acceptance criterion of this wave.

### PRICING BASIS

Meter against option O-002 — 377,000 input and 33,000 output tokens, USD 2.647440 per item variable, USD 42.99 per month standing, USD 5.95 per item all-in at 13 items per month. The USD 1.58 figure is superseded and comparing against it manufactures a false overrun (`C-001`). Development cost and per-item cost are separate quantities (`C-002`).

## Acceptance criteria

_Not provided in the ticket. To be defined and approved at the scope gate before implementation starts._
