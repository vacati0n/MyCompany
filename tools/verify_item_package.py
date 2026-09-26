"""Static checks over item 001's production package.

Each check recomputes a quantity the ledger or an acceptance criterion depends on, from the
package file itself, so a figure quoted anywhere in the wave can be re-derived rather than
trusted. Exit code is non-zero if any check fails.
"""
import io
import os
import re
import sys

ROOT = r"D:/Project/MyCompany/.worktrees/mc-3-feature"
PKG = os.path.join(ROOT, "wave-2", "item-001")

failures = []
results = []


def check(name, actual, expected):
    ok = actual == expected
    results.append((name, actual, expected, "PASS" if ok else "FAIL"))
    if not ok:
        failures.append(name)


def read(name):
    with io.open(os.path.join(PKG, name), encoding="utf-8") as f:
        return f.read().replace("\r\n", "\n")


# --- P1: narration character and word count, the metered quantity -------------
narration = read("narration.txt")
check("P1 narration characters", len(narration), 11096)
check("P1 narration words", len(narration.split()), 1929)

# --- P2: the narration file carries narration only, no shot cues --------------
cue_markers = ("GFX-", "CLIP-", "THUMB-", "[", "]", "|")
check("P2 narration free of shot cues",
      [m for m in cue_markers if m in narration], [])

# --- P3: shot list counts, recomputed from the tables -------------------------
shots = read("shot-list.md")
check("P3 original motion graphics", len(set(re.findall(r"`(GFX-\d\d)`", shots))), 22)
check("P3 licensed stock clips", len(set(re.findall(r"`(CLIP-\d\d)`", shots))), 18)
check("P3 thumbnail candidates", len(set(re.findall(r"`(THUMB-\d\d)`", shots))), 6)

# --- P4: zero generated video seconds, asserted in the package ----------------
check("P4 AI-generated video seconds stated zero",
      "**AI-generated video seconds: 0.**" in shots, True)

# --- P5: claim set, and every claim attributed --------------------------------
claims = read("claim-to-source.md")
claim_rows = re.findall(r"^\| `(CL-\d\d)` \| (.+?) \| (.+?) \| (.+?) \|$", claims, re.M)
check("P5 claim count", len(claim_rows), 23)
check("P5 unattributed claims",
      [c[0] for c in claim_rows if not c[2].strip() or c[2].strip() == "-"], [])
check("P5 claims carried without footage",
      [c[0] for c in claim_rows if not c[3].strip().startswith("Yes")], [])

# --- P6: every claim is carried by a graphic that the shot list defines -------
gfx_defined = set(re.findall(r"`(GFX-\d\d)`", shots))
gfx_cited = set(re.findall(r"`(GFX-\d\d)`", claims))
check("P6 claim graphics all defined in the shot list", sorted(gfx_cited - gfx_defined), [])

# --- P7: the supply audit obtained no count, and every row has a remedy -------
supply = read("supply-audit.md")
audit_rows = re.findall(r"^\| (\d+) \| `(.+?)` \| (.+?) \| (.+?) \| (.+?) \| (.+?) \|$", supply, re.M)
check("P7 subjects audited", len(audit_rows), 14)
check("P7 counts obtained", [r[1] for r in audit_rows if r[3].strip() != "—"], [])
check("P7 rows missing a reason", [r[1] for r in audit_rows if not r[4].strip()], [])
check("P7 rows missing a remedy", [r[1] for r in audit_rows if not r[5].strip()], [])

# --- P8: no fabricated numeric count anywhere in the supply audit table -------
# A digit in the Count column would be an invented figure; the column must hold only the
# not-obtained marker.
check("P8 no numeric count in the count column",
      [r[1] for r in audit_rows if re.search(r"\d", r[3])], [])

# --- P9: the five determinations, each resolved -------------------------------
comp = read("compliance-determinations.md")
check("P9 evidenced determinations", comp.count("**Evidenced**"), 3)
check("P9 not-evidenceable determinations", comp.count("**Recorded as not evidenceable**"), 2)
check("P9 unresolved stated zero", "Unresolved: 0" in comp, True)
check("P9 fourteen suitability categories",
      len(re.findall(r"^\| \d+ \| .+? \| No \| .+? \|$", comp, re.M)), 14)

# --- P10: metadata carries no barred term -------------------------------------
meta = read("metadata-and-thumbnails.md")
barred = ["for kids", "for children", "cute", "funny", "baby animals",
          "learn animals", "kids", "nursery", "toddler", "preschool"]
title = re.search(r"^> \*\*(.+?)\*\*$", meta, re.M).group(1)
tags_line = re.search(r"^## 3\. Tags.*?\n\n(.+?)\n", meta, re.M | re.S).group(1)
hits = [b for b in barred
        if re.search(r"\b" + re.escape(b) + r"\b", title, re.I)
        or re.search(r"\b" + re.escape(b) + r"\b", tags_line, re.I)]
check("P10 barred terms in title and tags", hits, [])

# --- P11: the twelve stages each carry a recorded outcome ---------------------
dossier = read("item-dossier.md")
stage_rows = re.findall(r"^\| (\d+) \| (.+?) \| (.+?) \| (.+?) \|$", dossier, re.M)
stage_rows = [r for r in stage_rows if r[0].isdigit() and int(r[0]) <= 12]
check("P11 stages recorded", len(stage_rows), 12)
check("P11 stages without an outcome", [r[1] for r in stage_rows if not r[2].strip()], [])

# --- P12: the ledger arithmetic, recomputed independently ---------------------
narration_cost = round(len(narration) / 1000 * 0.05, 6)
thumb_cost = round(6 * 0.05268, 6)
check("P12 narration cost", narration_cost, 0.5548)
check("P12 thumbnail cost", thumb_cost, 0.31608)
check("P12 media subtotal", round(narration_cost + thumb_cost, 6), 0.87088)

ledger = io.open(os.path.join(ROOT, "wave-2", "cost-ledger.md"), encoding="utf-8").read()
check("P12 ledger states the narration count", "**11,096**" in ledger, True)
check("P12 ledger states the subtotal", "$0.870880" in ledger, True)

# --- report -------------------------------------------------------------------
width = max(len(r[0]) for r in results)
for name, actual, expected, verdict in results:
    print(f"{verdict:4}  {name:<{width}}  actual={actual!r} expected={expected!r}")

print()
print(f"{len(results)} checks, {len(results) - len(failures)} passed, {len(failures)} failed")
sys.exit(1 if failures else 0)
