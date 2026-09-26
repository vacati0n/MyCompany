# Item 001 — item dossier

**Item:** `MC3-ITEM-001` · **Version:** 1 · **Channel:** the existing, blank, non-YPP channel.
**State:** held short of publish-ready. **Recorded:** 2026-09-26.

This is the item's evidence record — the structure `M-020` models in code, instantiated for this
item. Every blocking condition is read from here.

---

## 1. The twelve stages (`A-001`)

| # | Stage | Outcome | Evidence |
|---|---|---|---|
| 1 | Idea | Succeeded | Subject settled by `claude/trusting-shirley-90ea75 : D-017 : "First video: the honey bee waggle dance, as a coordinate-encoding mechanism"` |
| 2 | Idea scoring | Succeeded | Scored against the five pillar criteria; pillar 1, format F5. Supply criterion scored **conditional**, not satisfied — see §4 |
| 3 | Research | Succeeded | 23 claims, each attributed — `claim-to-source.md` |
| 4 | Script | Succeeded | `script.md`, `narration.txt`; committed after the supply audit |
| 5 | Originality check | Succeeded | Footage-removal assessment: 23 of 23 claims survive — `claim-to-source.md` §2 |
| 6 | Design | Succeeded | 22 original motion graphics specified — `shot-list.md` §1 |
| 7 | Production | **Held** | Cut specified, not assembled: three waggle-run clips unsourced, no subscription session |
| 8 | Audio | **Held** | Narration text final at 11,096 characters; audio not commissioned, because commissioning it is spend |
| 9 | Thumbnail | Succeeded | 6 candidates specified — `shot-list.md` §4 |
| 10 | Quality check | Succeeded | Treatment conditions assessed; metadata screened clean |
| 11 | Copyright check | **Held** | Library registration not performed for this channel; per-clip licence records cannot exist before the clips do |
| 12 | Policy check | Succeeded | Five determinations resolved — `compliance-determinations.md` |

**Stages with a recorded outcome: 12 of 12. Stages recorded `Held`: 3.**

A `Held` outcome is a recorded outcome, so `A-001` is satisfied — the stage set is complete and
none is absent, skipped or outcome-less. But `Held` is not `Succeeded`, and the terminal predicate
reads the outcome and not merely its presence. **The item is therefore held short of publish-ready,
by three stages, for reasons that are all the same reason.**

---

## 2. Treatment assessment (`A-004`, `A-005`)

| Condition | Verdict | Evidence |
|---|---|---|
| K-1 no anthropomorphism | Pass | No named individual, no invented personality, no dialogue, no motive attributed. The colony is never framed as a community or a team |
| K-2 scientific register | Pass | *Apis mellifera* on first mention; 23 attributed claims; contested matters presented as contested |
| K-3 no cartoon styling | Pass | All 22 graphics specified in a data-visualisation idiom; no illustrated character exists in the specification |
| K-4 documentary score | Pass | Score brief admits documentary register only; no song, no nursery register, no sound-effect comedy |
| K-5 metadata discipline | Pass | Title, description and 12 tags screened against the barred-term list; zero hits |
| K-6 no play-acting | Pass | No quiz, no guess prompt, no game framing, no second-person challenge |
| K-7 adult-signalling element | Pass | Beats 10–12: measured error, an unresolved literature dispute, and a negative result on the dance's value in dense habitat |
| K-8 audience-share monitoring | **Standing obligation** | Observable only after publication. Awaited parameter: the Studio age-demographic report, from month one |
| K-9 deliberate designation | Pass | Set per item with reasoning recorded below |

**Nine of nine resolved. Eight pass; one is a standing obligation naming the parameter it waits
on**, which is the resolution the model admits for K-8 alone and which does not block.

**Audience designation.** Not made for kids. **Reasoning:** the item is an adult general-science
explainer in documentary register with no characters, no play-acting and no simplified vocabulary;
its adult-signalling content is quantitative and includes an unresolved scientific dispute and a
negative result; the subject requires no named protagonist to hold attention. Set for this item
and not taken from a channel-level default. The channel carries no made-for-kids designation to
contend with and no existing audience to have drifted.

---

## 3. Sourcing, provenance and origin (`A-007` to `A-009`, `A-027`, `A-028`)

| Measure | Value |
|---|---|
| Visuals resolving to an admissible source | 22 original graphics + 18 licensed clips = **40 specified** |
| Visuals resolving to neither | **0** |
| Third-party clips, on-location footage, presenter-on-camera footage | **0** |
| Generated cutaways | **0** — none commissioned, so no recorded reason is owed |
| Synthetic personas | **0** |
| Depicted human experts | **0** — `CLIP-16` is hands only, no face |
| AI-generated video seconds | **0** |
| Clips with a completed origin assessment | **0 of 18** |
| Generated or composited assets presented as recorded footage | **0** |

**The origin-assessment row is the honest one.** No clip has been origin-assessed, because no clip
has been obtained. The assessment is a precondition of a clip's *use*, and no clip has been used —
so the ordering obligation is not violated, it is simply not yet reached. `A-027` is satisfiable
only once clips exist, and it is recorded here as outstanding rather than as passed.

---

## 4. Runtime, and why it is specified rather than measured (`A-003`)

| Measure | Value | Basis |
|---|---|---|
| Narration characters | 11,096 | **Measurement** — exact count of `narration.txt` |
| Narration words | 1,929 | **Measurement** |
| Delivery rate | 150 wpm | Assumption, documentary register |
| Narration duration | 12 min 51 s | Derived |
| Non-narration holds | 48 s | Specification |
| **Runtime of the cut** | **13 min 39 s** | **SPECIFIED, not measured** |

**Above the 10-minute floor and inside the 12–14 minute target.** Across the plausible delivery
band of 145–160 wpm the cut runs 12 min 51 s to 14 min 06 s, so it stays inside the target at every
rate in that band and never approaches the floor.

**Why it is not a measurement.** `A-003`'s verification method is measurement of the finished
item's duration, and no finished file exists. Rendering requires commissioning narration audio —
which is spend — and downloading licensed clips — which requires a subscription session this change
does not hold. **`A-003` therefore resolves as recorded-not-evidenceable at the measurement level,
with the specified runtime recorded.** What would evidence it: a rendered cut, and the duration
read from it.

Stating 13 min 39 s as a measurement would be the same class of error as inventing a clip count.

---

## 5. The three blocking reasons, which are one reason

The item is held short of publish-ready. The terminal predicate would refuse it, and the refusals
name these conditions:

| Refusal | Condition | Why |
|---|---|---|
| `StageOutcomeMissing` | Production, Audio, Copyright check recorded `Held` | Each requires an action this change is forbidden or unable to take |
| `ClipUnassessed` | 18 clips specified, 0 obtained, 0 assessed | No subscription session |
| `LibraryUnregistered` | Storyblocks registration not performed for this channel | A channel-level action not yet taken |
| `RuntimeBelowFloor` | Runtime specified, not measured | No rendered file to measure |

**All four trace to the same boundary:** this change commits no spend, creates no account, holds no
subscription session and publishes nothing. Those are the constraints the change was given, not
failures inside it.

**What this demonstrates, and it is the point of the wave.** The controls work. The item is held by
named conditions rather than passing on an assumption, and every refusal says what it refused. An
item that had reached publish-ready on this evidence would mean the predicate was not reading the
record.

---

## 6. Approval position (`A-012`, `A-013`)

**No owner approval has been taken, and none should be.** The item is not at publish-ready, so
there is nothing to approve. `A-013` requires the first approval to record the item, the exact
version, the verdict, the reason and the elapsed minutes; those five will be recorded when the
item reaches the gate.

The elapsed-minutes figure — one of the four quantities this wave exists to measure — is therefore
**not yet measured**, and this is recorded as an outstanding measurement rather than estimated.

---

## 7. The four things the wave set out to prove

| # | What it was to prove | Answer |
|---|---|---|
| 1 | What an item actually costs | **Partly.** Media quantities exact; the token half is not measurable at billed level, and phases 4–5 mix capex with opex by judgement |
| 2 | How long an owner approval takes | **Not yet.** The item has not reached the gate |
| 3 | Whether the five compliance determinations can be evidenced at all | **Yes, and the answer is specific:** 3 can be evidenced before upload, 2 cannot, and both of those have their evidence surface inside the upload workflow |
| 4 | What the committed libraries actually hold | **No.** No count was obtained. Both reasons recorded, both remedies named, and a material correction to the subject decision's supply premise recorded in `supply-audit.md` §4 |
