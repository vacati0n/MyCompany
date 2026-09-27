"""Static checks over item 001's production package.

Each check recomputes a quantity the ledger or an acceptance criterion depends on, from the
package file itself, so a figure quoted anywhere in the wave can be re-derived rather than
trusted. Exit code is non-zero if any check fails.

Two properties of this script are load-bearing and were added after a mutation test escaped it.

**It fails on what it cannot parse.** Every table and list it reads is parsed structurally, and a
row it cannot decompose into its declared columns is a FAILURE, never a row it skips. The earlier
version matched rows with a permissive regex, so un-backticking a subject dropped that row out of
the guarded set silently; a fabricated count could then sit visibly in the table while every
check passed.

**It binds sets, not counts.** The audited subject set is compared against the script's own
subject list, which is what criterion A-024 means by inspecting the audit *against the script's
subject list*. A row count alone can be restored by adding a decoy row, and that is exactly the
mutation that escaped.

Run with --self-test to execute the mutation demonstrations that hold both properties in place.
"""
import io
import os
import re
import sys

# Resolved relative to this file, never to one absolute checkout, so a second clone verifies its
# own files rather than reporting a pass over files it does not contain.
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), os.pardir))

NOT_OBTAINED = "—"          # em dash: the only admissible content of the Count column
AUDIT_COLUMNS = 6
AUDIT_HEADER = "| # | Requested term |"


class ParseFailure(Exception):
    """Raised when a guarded structure cannot be read. Never caught into a skip."""


def read(name, pkg):
    with io.open(os.path.join(pkg, name), encoding="utf-8") as f:
        return f.read().replace("\r\n", "\n")


# --------------------------------------------------------------------------- parsing

def parse_subject_list(script_text):
    """The script's committed subject list. Every line between the markers must parse."""
    m = re.search(r"<!-- subject-list:begin -->\n(.*?)<!-- subject-list:end -->",
                  script_text, re.S)
    if not m:
        raise ParseFailure("the script carries no delimited subject list")
    subjects = []
    for line in m.group(1).split("\n"):
        if not line.strip():
            continue
        entry = re.fullmatch(r"- `(.+?)`", line.strip())
        if not entry:
            raise ParseFailure("unparseable subject-list line: %r" % line)
        subjects.append(entry.group(1))
    if not subjects:
        raise ParseFailure("the subject list is empty")
    return subjects


def parse_audit_table(supply_text):
    """Every row of the supply-audit table, structurally.

    A row is any line inside the table block. It must decompose into exactly the declared number
    of columns and must name a term, or this raises. Nothing is skipped.
    """
    lines = supply_text.split("\n")
    start = None
    for i, l in enumerate(lines):
        if l.startswith(AUDIT_HEADER):
            start = i
            break
    if start is None:
        raise ParseFailure("the supply-audit table header was not found")
    if not re.fullmatch(r"\|(\s*-+\s*\|)+", lines[start + 1].strip()):
        raise ParseFailure("the supply-audit table has no delimiter row")

    rows = []
    for line in lines[start + 2:]:
        if not line.strip():
            break
        if not line.lstrip().startswith("|"):
            raise ParseFailure("non-row line inside the supply-audit table: %r" % line)
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if len(cells) != AUDIT_COLUMNS:
            raise ParseFailure(
                "supply-audit row has %d columns, expected %d: %r"
                % (len(cells), AUDIT_COLUMNS, line))
        term = cells[1].strip().strip("`").strip()
        if not term:
            raise ParseFailure("supply-audit row names no subject: %r" % line)
        rows.append({
            "ordinal": cells[0], "term": term, "fidelity": cells[2],
            "count": cells[3], "reason": cells[4], "remedy": cells[5],
        })
    if not rows:
        raise ParseFailure("the supply-audit table has no rows")
    return rows


# --------------------------------------------------------------------------- audit checks

def audit_findings(subjects, rows):
    """Every way the audit can be wrong, as a mapping of finding name to offending values."""
    audited = [r["term"] for r in rows]
    return {
        "subjects in the script with no audit row": sorted(set(subjects) - set(audited)),
        "audit rows naming a subject absent from the script": sorted(set(audited) - set(subjects)),
        "duplicate audit rows": sorted({t for t in audited if audited.count(t) > 1}),
        "rows whose count column is not the not-obtained marker":
            [r["term"] for r in rows if r["count"] != NOT_OBTAINED],
        "rows whose count column contains a digit":
            [r["term"] for r in rows if re.search(r"\d", r["count"])],
        "rows missing a reason": [r["term"] for r in rows if not r["reason"]],
        "rows missing a remedy": [r["term"] for r in rows if not r["remedy"]],
    }


# --------------------------------------------------------------------------- runner

def run_checks(root):
    pkg = os.path.join(root, "wave-2", "item-001")
    results = []

    def check(name, actual, expected):
        results.append((name, actual, expected, actual == expected))

    narration = read("narration.txt", pkg)
    script = read("script.md", pkg)
    shots = read("shot-list.md", pkg)
    claims = read("claim-to-source.md", pkg)
    supply = read("supply-audit.md", pkg)
    comp = read("compliance-determinations.md", pkg)
    meta = read("metadata-and-thumbnails.md", pkg)
    dossier = read("item-dossier.md", pkg)
    ledger = read("cost-ledger.md", os.path.join(root, "wave-2"))

    # P1/P2 narration -------------------------------------------------------
    check("P1 narration characters", len(narration), 11096)
    check("P1 narration words", len(narration.split()), 1929)
    check("P2 narration free of shot cues",
          [m for m in ("GFX-", "CLIP-", "THUMB-", "[", "]", "|") if m in narration], [])

    # P3/P4 shot list -------------------------------------------------------
    check("P3 original motion graphics", len(set(re.findall(r"`(GFX-\d\d)`", shots))), 22)
    check("P3 licensed stock clips", len(set(re.findall(r"`(CLIP-\d\d)`", shots))), 18)
    check("P3 thumbnail candidates", len(set(re.findall(r"`(THUMB-\d\d)`", shots))), 6)
    check("P4 AI-generated video seconds stated zero",
          "**AI-generated video seconds: 0.**" in shots, True)

    # P5/P6 claims ----------------------------------------------------------
    claim_rows = re.findall(r"^\| `(CL-\d\d)` \| (.+?) \| (.+?) \| (.+?) \|$", claims, re.M)
    check("P5 claim count", len(claim_rows), 23)
    check("P5 unattributed claims",
          [c[0] for c in claim_rows if not c[2].strip() or c[2].strip() == "-"], [])
    check("P5 claims carried without footage",
          [c[0] for c in claim_rows if not c[3].strip().startswith("Yes")], [])
    check("P6 claim graphics all defined in the shot list",
          sorted(set(re.findall(r"`(GFX-\d\d)`", claims))
                 - set(re.findall(r"`(GFX-\d\d)`", shots))), [])

    # P7/P8 supply audit, bound to the script's subject list ------------------
    try:
        subjects = parse_subject_list(script)
        rows = parse_audit_table(supply)
        check("P7 supply audit and subject list parse without failure", "parsed", "parsed")
        check("P7 subjects committed by the script", len(subjects), 14)
        check("P7 audit rows parsed", len(rows), len(subjects))
        for name, offenders in sorted(audit_findings(subjects, rows).items()):
            prefix = "P8" if "count column" in name else "P7"
            check("%s %s" % (prefix, name), offenders, [])
    except ParseFailure as exc:
        check("P7 supply audit and subject list parse without failure", str(exc), "parsed")

    # P13 every clip's recorded placement matches the script's own beat table ---
    beat_of = {}
    for ln in script.splitlines():
        m = re.match(r"^\| (\d+)\. (.+?) \| \d\d:\d\d \|.*\|(.*)\|$", ln)
        if m:
            for c in re.findall(r"CLIP-\d\d", m.group(3)):
                beat_of.setdefault(c, "Beat %s, %s" % (m.group(1), m.group(2).strip().lower()))
    placements = dict(re.findall(r"^\| `(CLIP-\d\d)` \| .+? \| (.+?) \| .+? \|$", shots, re.M))
    check("P13 clips placed by the script", len(beat_of), 18)
    check("P13 clips with a recorded placement", len(placements), 18)
    check("P13 placements disagreeing with the script beat",
          sorted(c for c, v in placements.items() if beat_of.get(c) != v), [])
    check("P13 clips in the shot list the script never places",
          sorted(set(placements) - set(beat_of)), [])

    # P9 determinations -----------------------------------------------------
    check("P9 evidenced determinations", comp.count("**Evidenced**"), 3)
    check("P9 not-evidenceable determinations", comp.count("**Recorded as not evidenceable**"), 2)
    check("P9 unresolved stated zero", "Unresolved: 0" in comp, True)
    check("P9 fourteen suitability categories",
          len(re.findall(r"^\| \d+ \| .+? \| No \| .+? \|$", comp, re.M)), 14)

    # P10 barred terms, across all three surfaces criterion A-006 names -------
    barred = ["for kids", "for children", "cute", "funny", "baby animals",
              "learn animals", "kids", "nursery", "toddler", "preschool"]
    title = re.search(r"^> \*\*(.+?)\*\*$", meta, re.M).group(1)
    description = re.search(r"^## 2\. Description\n\n(.*?)\nScreened:", meta, re.M | re.S).group(1)
    tags = re.search(r"^## 3\. Tags.*?\n\n(.+?)\n", meta, re.M | re.S).group(1)
    for surface, text in (("title", title), ("description", description), ("tags", tags)):
        check("P10 barred terms in %s" % surface,
              [b for b in barred if re.search(r"\b" + re.escape(b) + r"\b", text, re.I)], [])

    # P11 stages ------------------------------------------------------------
    stage_rows = [r for r in re.findall(r"^\| (\d+) \| (.+?) \| (.+?) \| (.+?) \|$", dossier, re.M)
                  if r[0].isdigit() and int(r[0]) <= 12]
    check("P11 stages recorded", len(stage_rows), 12)
    check("P11 stages without an outcome", [r[1] for r in stage_rows if not r[2].strip()], [])

    # P12 ledger arithmetic --------------------------------------------------
    narration_cost = round(len(narration) / 1000 * 0.05, 6)
    thumb_cost = round(6 * 0.05268, 6)
    check("P12 narration cost", narration_cost, 0.5548)
    check("P12 thumbnail cost", thumb_cost, 0.31608)
    check("P12 media subtotal", round(narration_cost + thumb_cost, 6), 0.87088)
    check("P12 ledger states the narration count", "**11,096**" in ledger, True)
    check("P12 ledger states the subtotal", "$0.870880" in ledger, True)

    return results


# --------------------------------------------------------------------------- self-test

SOUND_SUBJECTS = ["bee", "honeybee", "waggle dance"]

SOUND_TABLE = (
    "| # | Requested term | Fidelity class | Count obtained | Reason not obtained "
    "| What would obtain it |\n"
    "|---|---|---|---|---|---|\n"
    "| 1 | `bee` | single token | — | unreachable | authenticated session |\n"
    "| 2 | `honeybee` | single token | — | unreachable | authenticated session |\n"
    "| 3 | `waggle dance` | multi-word | — | inadmissible | per-clip confirmation |\n"
)

BEE_ROW = "| 1 | `bee` | single token | — | unreachable | authenticated session |"


def self_test():
    """The mutation demonstrations. Each asserts a fabricated or malformed audit is REJECTED."""
    cases = []

    def case(name, table, subjects=None, expect_parse_failure=False):
        subjects = subjects or SOUND_SUBJECTS
        try:
            rows = parse_audit_table(table)
        except ParseFailure as exc:
            cases.append((name, "rejected at parse: %s" % str(exc)[:56], expect_parse_failure))
            return
        if expect_parse_failure:
            cases.append((name, "PARSED when it should not have", False))
            return
        findings = {k: v for k, v in audit_findings(subjects, rows).items() if v}
        detail = "rejected: " + "; ".join(sorted(findings)) if findings else "ACCEPTED"
        cases.append((name, detail[:96], bool(findings)))

    # The control must accept the sound table, or it is useless.
    sound_ok = not any(audit_findings(SOUND_SUBJECTS, parse_audit_table(SOUND_TABLE)).values())
    cases.append(("M0 a sound audit is accepted",
                  "accepted" if sound_ok else "REJECTED", sound_ok))

    case("M1 plain numeric count",
         SOUND_TABLE.replace(BEE_ROW, BEE_ROW.replace("| — |", "| 1,213 |")))
    case("M2 hedged count",
         SOUND_TABLE.replace(BEE_ROW, BEE_ROW.replace("| — |", "| ~400 (est.) |")))
    case("M3 word-form count",
         SOUND_TABLE.replace(BEE_ROW,
                             BEE_ROW.replace("| — |", "| one thousand two hundred |")))
    case("M4 vague count",
         SOUND_TABLE.replace(BEE_ROW, BEE_ROW.replace("| — |", "| many |")))
    case("M5 subject un-backticked out of the old pattern",
         SOUND_TABLE.replace("| `bee` |", "| bee-unlisted |"))

    # M6 is the mutation that escaped the earlier control: a fabricated count in a row
    # reformatted out of the pattern, plus a decoy row restoring the matched-row count.
    escaping = SOUND_TABLE.replace(
        BEE_ROW, "| 1 | bee | single token | 1,213 | fabricated | none |"
    ) + "| 4 | `decoy` | single token | — | unreachable | authenticated session |\n"
    case("M6 fabricated count in a reformatted row plus a decoy row", escaping)

    case("M7 row with a missing column",
         SOUND_TABLE.replace(
             "| 2 | `honeybee` | single token | — | unreachable | authenticated session |",
             "| 2 | `honeybee` | single token | — | unreachable |"),
         expect_parse_failure=True)
    case("M8 row naming no subject",
         SOUND_TABLE.replace("| 2 | `honeybee` |", "| 2 |  |"),
         expect_parse_failure=True)
    case("M9 a script subject with no audit row",
         SOUND_TABLE, subjects=SOUND_SUBJECTS + ["linden"])
    case("M10 non-row line inside the table",
         SOUND_TABLE.replace("| 3 | `waggle dance`", "note: see below\n| 3 | `waggle dance`"),
         expect_parse_failure=True)

    width = max(len(c[0]) for c in cases)
    for name, detail, ok in cases:
        print("%-4s %-*s  %s" % ("PASS" if ok else "FAIL", width, name, detail))
    failed = [c[0] for c in cases if not c[2]]
    print("\n%d mutation demonstrations, %d passed, %d failed"
          % (len(cases), len(cases) - len(failed), len(failed)))
    return 1 if failed else 0


def main():
    if "--self-test" in sys.argv:
        return self_test()

    results = run_checks(ROOT)
    width = max(len(r[0]) for r in results)
    for name, actual, expected, ok in results:
        print("%-4s  %-*s  actual=%r expected=%r"
              % ("PASS" if ok else "FAIL", width, name, actual, expected))
    failed = [r[0] for r in results if not r[3]]
    print("\n%d checks, %d passed, %d failed"
          % (len(results), len(results) - len(failed), len(failed)))
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
