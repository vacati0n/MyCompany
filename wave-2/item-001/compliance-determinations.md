# Item 001 — the five compliance determinations

**Item:** `MC3-ITEM-001` · **Version:** 1 · **Resolved:** 2026-09-26.
**Governing policy text verified:** 2026-09-18 (`research/platform-policy-dossier.md`). Next
scheduled re-verification: 2026-10-26.

Satisfies `A-019` to `A-023`. Each determination resolves to recorded evidence **or** to a
recorded statement that it cannot be evidenced naming the reason and what would make it
evidenceable. Determinations resolving to neither: **zero**.

**The three resolutions, and why the third is not a failure.** A determination is `evidenced`,
`recorded as not evidenceable`, or `unresolved`. Only the third refuses the publish-ready state.
The second exists because some determinations have an evidence surface that opens only inside the
upload workflow, which this change does not reach — and an honest negative answer recorded with
its reason is the outcome the model is built for. It carries a mandatory reason and a mandatory
remedy so that it can never become a quiet way of passing something nobody checked.

---

## Summary

| # | Determination | Resolution |
|---|---|---|
| 1 | Originality self-assessment | **Evidenced** |
| 2 | Exact and near-duplicate detection | **Recorded as not evidenceable** |
| 3 | Synthetic-media disclosure | **Evidenced** |
| 4 | Affiliate and paid-promotion disclosure | **Evidenced** — positive evidence that neither is present |
| 5 | Advertiser-suitability self-rating | **Recorded as not evidenceable** |

**Evidenced: 3. Recorded as not evidenceable: 2. Unresolved: 0. Failing: 0.**

---

## 1. Originality self-assessment — EVIDENCED

**Evidence.** The item's argument holds with its licensed footage set aside. All 23 claims in
`claim-to-source.md` are made by the narration and shown by one of the 22 original motion
graphics; none rests on a licensed clip. With all 18 clips deleted the item remains a complete
essay on a mechanism with its diagrams intact.

**Why this is the right test.** The governing policy names collections assembled from other
people's work as not allowed *"even if you have their permission"*, so licensing and originality
are two independent gates and a stock licence passes only the first. The originality burden rests
entirely on the script, the narration and the original graphics.

**Policy basis.** Inauthentic-content policy, reused-content sub-category; verified 2026-09-18.

**Standard of comparison actually applied.** The nearest first-party formulation of the same test
is Instagram's: *"if someone could remove your contribution to your post or reel, and the content
would virtually be the same, it probably needs more of you in it."* Here the contribution is the
argument, and removing the third-party material leaves the item substantially intact.

---

## 2. Exact and near-duplicate detection — RECORDED AS NOT EVIDENCEABLE

**Reason it cannot be evidenced.** Two halves, and both fail for this item.

Against the company's own catalogue: the catalogue is empty. This is the first item the company
has produced, and the channel carries no prior uploads. A duplicate result against a set of size
zero is trivially negative and evidences nothing, because the comparison that would have force has
no other member to make.

Against sources outside the company: a near-duplicate result requires a perceptual hash computed
over rendered keyframes and compared against an external corpus. No render exists — see
determination 5 and the runtime note in `item-dossier.md` — and no external corpus is reachable
from this role.

**What would make it evidenceable.** A rendered cut, a perceptual hash computed over its
keyframes, and a comparison against both the company catalogue (once it holds a second item) and a
reachable external corpus, with the distance at which the item was judged distinct recorded
alongside the result.

**What is recorded instead.** The script-level distinctness position: the item is an original
script on a mechanism, written for this item, with no passage derived from another work. That is a
statement about the script, not a duplicate result, and it is not offered as one.

**`A-021` note.** The criterion requires the recorded *distance* alongside the result. Because no
result was computed, no distance is recorded. Recording a distance here would be inventing the
quantity the criterion exists to check.

---

## 3. Synthetic-media disclosure — EVIDENCED

**The element set, enumerated.** The determination states, for every generated or altered element,
whether disclosure is required and on what basis. Unassessed elements: zero.

| Element | Disclosure required | Basis |
|---|---|---|
| Narration audio (synthesised voice, original script) | **No** | The trigger is realistic content that is meaningfully altered or synthetically generated in a way that makes a real person appear to say or do something they did not, or depicts an event that did not occur. A synthesised voice reading an original script does neither: no real person is portrayed, no identifiable voice is cloned, and no depicted event is asserted. |
| 22 original motion graphics | **No** | Data-visualisation artefacts produced for this item. They depict no real person, place or event, and are not realistic content in the sense the trigger names. |
| 18 licensed stock clips | **No** | Recorded footage licensed from the committed library, subject to per-clip origin assessment before use. Nothing is altered beyond colour, framing and speed, which are named as production edits that need not be disclosed. |
| Generated video footage | **Not applicable — zero commissioned** | No generated footage of a real place or event exists in this item, so the single element class that would most clearly trigger disclosure is absent by decision rather than by omission. |
| 6 thumbnail candidates | **No** | Composed from the item's own graphics and licensed frames. No candidate depicts a real person, and none is a realistic depiction of an event. |

**The decision that keeps this clean.** Zero AI-generated video seconds are commissioned. The
disclosure trigger expressly reaches *"AI generated extra footage of a real place"*, so
commissioning none removes the only element of this production that would have required disclosure
on that ground.

**Two limits recorded rather than glossed.**

The platform may apply a label irrespective of what the uploader declares, where a generating tool
carries provenance metadata (Content Credentials). The narration synthesiser's provenance-metadata
behaviour has not been verified first-hand. If it signs its output, the label may be applied
regardless of this determination. **What would settle it:** inspecting the returned audio for
Content Credentials at the moment narration is commissioned.

The assessment above is a determination about what the policy requires. It is not the act of
setting the altered-or-synthetic attribute, which happens in the upload workflow this change does
not reach.

**Policy basis.** Synthetic content and disclosure requirements; verified 2026-09-18.

---

## 4. Affiliate and paid-promotion disclosure — EVIDENCED

**This is a positive determination, and the distinction matters.**

**Evidence.** The first item carries **no affiliate link and no sponsored placement**. Neither is
present in the narration, the description, the on-screen graphics, the end card or the metadata
set. The in-video disclosure obligation therefore does not arise for this item, and the
paid-promotion declaration has nothing to declare.

**Source.** Owner decision `D-022`, "The first item carries no affiliate link".
(That decision was originally filed as `D-015` on its branch and renumbered to `D-022`; withdrawn
identifiers are not reissued.)

**Why this resolves as evidenced rather than as not evidenceable.** The determination is satisfied
by a recorded observation about the item's own content — that neither element is present — which
is checkable against the item today. It does not depend on any upload surface. Both resolutions
would satisfy `A-019`, but they are different answers and only one of them is good news.

**What this sidesteps.** Whether an affiliate link *alone* triggers the paid-promotion declaration
is **not established against first-party text** — the policy dossier records it as an explicit
negative finding, noting that neither the paid-promotion page nor the branded-content policy
addresses affiliate links specifically, and that the US endorsement rules which do require
disclosure are law rather than platform policy. Because this item carries no affiliate link, that
unresolved question does not have to be answered for it to proceed.

**The standing rule, which is not relaxed by this.** Any future item carrying an affiliate link or
a sponsorship declares it in the upload workflow **and** discloses in-video, always, without first
assessing whether a rule requires it. Its condition is absent for this item; that is not the same
as the rule being relaxed.

---

## 5. Advertiser-suitability self-rating — RECORDED AS NOT EVIDENCEABLE

**Reason it cannot be evidenced.** The self-rating is an answer given to the self-certification
questionnaire, and that questionnaire exists only inside the upload workflow. This change publishes
nothing and reaches no upload surface, so the rating cannot be given, and no rating is returned to
be recorded.

**What would make it evidenceable.** Completing the self-certification questionnaire at upload,
from the classification recorded below, and recording the ad state returned (full, limited or
none) together with any review outcome.

**The classification itself IS recorded, and it is what `A-022` protects.** The criterion's force
is that a rating produced from a fixed answer with no classification behind it resolves as
*absent* rather than as a rating. The classification is therefore recorded now, against the item's
own content, so that when the questionnaire is answered it is answered from evidence rather than
from a default.

| # | Category | Present | Basis |
|---|---|---|---|
| 1 | Inappropriate language | No | Full narration reviewed; documentary register throughout |
| 2 | Violence | No | No depiction of violence; no predation sequence in this item |
| 3 | Adult content | No | No sexual content; the reproductive biology of the colony is not a subject here |
| 4 | Shocking content | No | No content designed to shock; no gore |
| 5 | Harmful acts and unreliable content | No | Every claim attributed to a named paper or institution; contested questions presented as contested (`CL-22`) |
| 6 | Hateful and derogatory content | No | No group is referenced |
| 7 | Recreational drugs | No | Not referenced |
| 8 | Firearms-related content | No | Not referenced |
| 9 | Controversial issues | No | The one contested matter is a scientific question about dance error, which is not a controversial issue in the guideline sense |
| 10 | Sensitive events | No | No real-world event is referenced |
| 11 | Enabling dishonest behaviour | No | Not applicable |
| 12 | Inappropriate content for kids and families | No | Adult register, but nothing unsuitable; audience designation is not-made-for-kids on treatment grounds, which is a different question |
| 13 | Incendiary and demeaning | No | Not applicable |
| 14 | Tobacco-related content | No | Not referenced |

**Categories classified: 14 of 14. Categories with a recorded basis: 14. Expected rating on this
classification: full ad revenue — stated as an expectation, not as a rating.**

**Why the distinction is kept.** Repeated inaccurate self-rating puts programme eligibility under
review, and a pipeline that answers "none of the above" by default is the documented route to
exactly that. Recording the classification without recording a rating is the honest position: the
work the criterion asks for is done, and the answer it asks for cannot be given yet.

---

## 6. What this set demonstrates, stated for the gate

Two of the five determinations cannot be evidenced, and both say so with a reason and a remedy.
That is one of the four things this wave exists to find out — *whether the compliance
determinations can be evidenced at all* — and the answer is now on the record: **three can be
evidenced before upload; two cannot, and both of those have their evidence surface inside the
upload workflow.**

Neither of the two is blocked by anything the company can fix by working harder. Both are blocked
by the fact that this change publishes nothing, which is the constraint the change was given.
