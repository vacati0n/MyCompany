# Item 001 — supply audit

**Item:** `MC3-ITEM-001` · **Version:** 1 · **Audited:** 2026-09-26 · **Library:** Storyblocks
(committed; Unlimited All Access at USD 30.00/month).

Satisfies `A-024`, `A-025` and `A-026`, and is recorded **before** the script commit, which is the
ordering obligation `C-015` makes a write-time precondition rather than a check.

---

## 0. The headline, stated first because everything else depends on it

**No clip count was obtained first-hand for any subject in this list, and none has been invented,
estimated, extrapolated or inferred.**

Every row below resolves either to a recorded reason the count was not obtained together with
what would obtain it, or — where a prior observation exists — to that prior carried with its date
and its own admissibility, explicitly not re-presented as a fresh measurement.

That is a successful outcome of this audit, not a failure of it. The alternative — a plausible
number with nothing behind it — would silently defeat `A-024` to `A-026`, and it is the documented
route by which this format ends up publishing generated animals as documentary evidence.

---

## 1. Why no count was obtained

Two independent reasons, either of which alone is sufficient.

**The library's search surface requires an authenticated subscription.** The count is visible only
from inside the subscription session. A prior audit of this same library was rate-limited at
HTTP 403 partway through a battery of roughly forty queries, so automated access is additionally
unreliable even where a session exists.

**This role may not reach the library directly.** Under the delivered architecture every library
search is a capability-class request through the resolution boundary, so that no consumer holds a
library dependency and every audit query is metered like any other operation. The resolution
boundary has not executed. Reaching the library outside it would be both an authority breach and
the precise coupling the architecture removes.

**What would obtain the counts:** an authenticated Storyblocks session driven through the
resolution boundary once that boundary is executing, issuing one literal single-token query per
subject and recording the result line the library itself returns.

---

## 2. The term-fidelity rule this audit applies

A total reported for a term the library substituted, spelling-corrected or matched loosely is
**no count**, not a count. Only a single, distinctive, unambiguous token the library echoes back
unchanged yields an admissible figure.

The rule is not conservatism. It is the recorded finding that the library's search is fuzzy and
spelling-corrected rather than literal: the query `saola` returns a page whose own result line
reads *"181 results found for saona"* — a Caribbean island — while the true count is zero; and the
nonsense query `xyzzy monkey` returns 26 results, which establishes that a multi-word query is a
relevance blend rather than an intersection.

---

## 3. The audit

`Fidelity class` is the term's class as the rule above decides it. `Count` is the count actually
obtained. It is empty in every row, and that is the finding.

| # | Requested term | Fidelity class | Count obtained | Reason not obtained | What would obtain it |
|---|---|---|---|---|---|
| 1 | `bee` | single token | — | Library surface unreachable: authenticated subscription required, and this role may not reach it directly | Literal query inside an authenticated session via the resolution boundary |
| 2 | `honeybee` | single token | — | As above | As above |
| 3 | `honey bee` | **multi-word — inadmissible** | — | The reported total for a multi-word phrase is an upper bound of unknown looseness and is not a count | A literal single-token query (`bee` or `honeybee`), plus per-clip confirmation that results show *Apis mellifera* |
| 4 | `waggle dance` | **multi-word — inadmissible** | — | As row 3. This is the item's critical subject and the reported total is explicitly inadmissible | **Per-clip confirmation by eye** inside the subscription that a candidate shows the waggle run itself and not generic hive activity |
| 5 | `beehive` | single token | — | Library surface unreachable | Literal query via the resolution boundary |
| 6 | `honeycomb` | single token | — | Library surface unreachable | As above |
| 7 | `apiary` | single token | — | Library surface unreachable | As above |
| 8 | `pollination` | single token | — | Library surface unreachable | As above |
| 9 | `blossom` | single token | — | Library surface unreachable | As above |
| 10 | `linden` | single token | — | Library surface unreachable | As above |
| 11 | `meadow` | single token | — | Library surface unreachable | As above |
| 12 | `farmland` | single token | — | Library surface unreachable | As above |
| 13 | `sunset` | single token | — | Library surface unreachable | As above |
| 14 | `beekeeper` | single token | — | Library surface unreachable | As above |

**Subjects: 14. Counts obtained: 0. Subjects carrying a recorded reason and a remedy: 14.**

Every row resolves under `A-024`, because the criterion is satisfied by a count **or** by a
recorded statement of non-evidenceability naming its reason and its remedy. None resolves by a
count.

---

## 4. The one prior observation, and the correction it carries

| Term | Reported total | Observed | Source | Admissible then? |
|---|---|---|---|---|
| `honey bee` | 1,213 | 2026-09-26 | `research/animal-niche-analysis.md` §3.3, Storyblocks video search | **No** |

**This figure is cited as a prior recorded observation with its date. It is not a measurement made
by this audit, and it is not admissible as a count.**

**A correction is owed here, and it is material.** Two upstream statements describe this figure as
a trustworthy single-token read:

- the dispatch for this phase, which states that `honey bee` = 1,213 "is a trustworthy single-token
  prior read";
- the subject decision itself, `claude/trusting-shirley-90ea75 : D-017 : "First video: the honey
  bee waggle dance, as a coordinate-encoding mechanism"`, whose supporting reasoning records that
  `honey bee` is *"a single, distinctive, unambiguous token, so it is one of the counts §3.3
  classifies as trustworthy"*.

**§3.3 does not classify it that way.** `honey bee` is two words. The section's own list of
trustworthy figures names nine single-token species — pangolin, narwhal, bowerbird, okapi,
platypus, binturong, cassowary, axolotl, tapir — and excludes `honey bee` along with the seven
other multi-word rows in the same table (`grey wolf`, `humpback whale`, `bengal tiger`, `mantis
shrimp`, `snow leopard`, `naked mole rat`, `emperor penguin`). The table's header calls the battery
"single-token species names", and for those eight rows the header is inaccurate.

**What this does and does not change.**

It does **not** change the subject. The other four reasons `D-017` gives for choosing the waggle
dance stand entirely: the species is common and well-filmed and therefore far from the contaminated
rare-species tail; the originality burden lands on graphics the channel can produce; the
made-for-kids exposure is at its lowest here; and the science is attributable, which
`claim-to-source.md` demonstrates with 23 attributed claims.

It **does** change the supply position. `D-017` treated supply for this subject as *established*
on the strength of 1,213. On the source's own rule it is **not established** — not for the
species, and still less for the behaviour. The honest position is that supply for this item is
unestablished in both terms, and that is what rows 3 and 4 record.

This is escalated rather than absorbed. A phase that quietly re-read the evidence to agree with
the instruction would defeat the purpose of the audit.

---

## 5. The binding consequence for production

**`A-025` — a subject the libraries cannot illustrate changes the script.** No subject here has been
recorded unavailable, because unavailability is a finding from a completed audit and this audit
completed no count. The script therefore commits with its subject list **conditionally**, and the
condition is stated in the shot list: `CLIP-05`, `CLIP-06` and `CLIP-07` — the three waggle-run
clips — are specified but not sourced.

**If per-clip confirmation finds no clip that shows the waggle run itself**, the item does not
substitute generic hive activity presented as the dance. `GFX-02`, `GFX-03` and `GFX-04` already
carry that part of the argument, and the narration is written so that the dance is *described and
diagrammed* rather than *shown*. The three clips are illustration the item can lose.

That property is deliberate and it is the mitigation: the item was written so that the one subject
whose supply cannot be established is the one subject no claim depends on.

**`A-028` — no generated or composited asset appears presented as recorded footage.** Zero generated
cutaways are commissioned, and every licensed clip is subject to origin assessment before use. The
count of generated assets presented as recorded footage is zero by construction.
