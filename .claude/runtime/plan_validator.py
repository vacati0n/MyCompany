#!/usr/bin/env python3
"""Validation Engine: execution-plan.md conformance.

Mechanically checks a produced `execution-plan.md` against the Planner Agent's own
contract:

  - `.claude/agents/planner/output.md`    structural and semantic contract
  - `.claude/agents/planner/quality.md`   check set and severities
  - `.claude/templates/execution-plan.md` rendered form

This is the Validation Engine named in `config/runtime.md` and step 6 of the invocation
pipeline in `config/execution-engine.md`, implemented for one artifact type only.

Scope boundary: this validator covers the checks in `quality.md` that are decidable by
inspection of the artifact plus the framework's registries. Checks that require semantic
judgement (for example Q4.4, "objectives express outcomes, not activities") are reported
as `not-machine-checkable` and remain the agent's own self-verification obligation.
Nothing is silently skipped.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

import yaml

from artifact_lib import (  # noqa: F401  -- re-exported for callers of this module
    CLAUDE, VENDOR_TOKENS, Check, Report, framework_facts, ids_of,
    parse_all_tables, parse_table, split_sections, strip_md,
)

MANDATORY_SECTIONS = [
    "Executive Summary",
    "Business Objectives",
    "Technical Objectives",
    "Scope",
    "Assumptions",
    "Risks",
    "Task Breakdown",
    "Dependencies",
    "Suggested Workflow",
    "Required Capabilities",
    "Acceptance Criteria",
    "Definition of Done",
]
PERMITTED_APPENDICES = ["Open Questions", "Traceability Matrix"]

METADATA_FIELDS = [
    "planId", "sourceInputs", "producedBy", "agentVersion",
    "schemaVersion", "status", "inputDigest", "contextDigest",
]
PLAN_STATUSES = {"complete", "partial", "blocked"}
TASK_STATUSES = {"ready", "blocked", "assumption-dependent"}
EDGE_TYPES = {
    "produces-consumes", "decision-gate", "contract",
    "verification", "external", "policy-gate",
}
RISK_CLASSES = {
    "requirement", "technical", "dependency",
    "security", "operational", "delivery",
}
LIKELIHOODS = {"high", "medium", "low"}

TASK_FIELDS = [
    "Owner", "Complexity", "Depends on", "Traces to",
    "Status", "Description", "Acceptance Criteria", "Gate",
]


# --------------------------------------------------------------------------- validation


def validate(artifact_path: Path, envelope: dict | None = None) -> Report:
    text = artifact_path.read_text(encoding="utf-8")
    rep = Report(artifact=str(artifact_path))
    facts = framework_facts()

    def add(cid, qref, severity, description, result, detail=""):
        rep.checks.append(Check(cid, qref, severity, description, result, detail))

    # ---------------- Q1 structural conformance
    m = re.search(r"```yaml\s*\n(.*?)\n```", text, re.S)
    meta = {}
    if not m:
        add("V1.1", "Q1.5", "Blocking", "Metadata block present and parseable", "fail",
            "no leading fenced yaml metadata block found")
    else:
        try:
            meta = (yaml.safe_load(m.group(1)) or {}).get("plan", {}) or {}
            missing = [f for f in METADATA_FIELDS if f not in meta or meta[f] in (None, "", [])]
            add("V1.1", "Q1.5", "Blocking", "Metadata block present with all fields populated",
                "fail" if missing else "pass",
                f"missing or empty: {missing}" if missing else "8 of 8 fields populated")
        except Exception as exc:  # noqa: BLE001
            add("V1.1", "Q1.5", "Blocking", "Metadata block present and parseable", "fail", str(exc))

    add("V1.2", "Q1.5", "Blocking", "producedBy is planner",
        "pass" if meta.get("producedBy") == "planner" else "fail",
        f"producedBy={meta.get('producedBy')!r}")
    add("V1.3", "Q1.5", "Blocking", "agentVersion is 1.0.0 and schemaVersion is 1.0.0",
        "pass" if str(meta.get("agentVersion")) == "1.0.0"
        and str(meta.get("schemaVersion")) == "1.0.0" else "fail",
        f"agentVersion={meta.get('agentVersion')} schemaVersion={meta.get('schemaVersion')}")
    add("V1.4", "Q1.7", "Blocking", "status is one of complete, partial, blocked",
        "pass" if meta.get("status") in PLAN_STATUSES else "fail",
        f"status={meta.get('status')!r}")

    sections = split_sections(text)
    titles = [t for t, _ in sections]
    body_of = {}
    for t, b in sections:
        body_of.setdefault(t, b)

    present_mandatory = [t for t in titles if t in MANDATORY_SECTIONS]
    dupes = sorted({t for t in present_mandatory if present_mandatory.count(t) > 1})
    missing_sec = [t for t in MANDATORY_SECTIONS if t not in titles]
    add("V2.1", "Q1.1", "Blocking", "All twelve mandatory sections present exactly once",
        "fail" if (missing_sec or dupes) else "pass",
        f"missing={missing_sec} duplicated={dupes}" if (missing_sec or dupes)
        else "12 of 12 present, once each")
    add("V2.2", "Q1.1", "Blocking", "Mandatory sections appear in contract order",
        "pass" if present_mandatory == [t for t in MANDATORY_SECTIONS if t in titles] else "fail",
        f"observed order: {present_mandatory}")
    add("V2.3", "Q1.2", "Blocking", "Section titles match output.md exactly",
        "pass" if not missing_sec else "fail",
        "exact-title match" if not missing_sec else f"unmatched: {missing_sec}")

    extra = [t for t in titles if t not in MANDATORY_SECTIONS and t not in PERMITTED_APPENDICES]
    add("V2.4", "Q1.4", "Correctable", "No unpermitted level-2 sections introduced",
        "pass" if not extra else "fail", f"unpermitted: {extra}" if extra else "none")

    empty = [t for t in MANDATORY_SECTIONS
             if t in body_of and not re.sub(r"<!--.*?-->", "", body_of[t], flags=re.S).strip()]
    add("V2.5", "Q1.3", "Blocking", "No mandatory section is empty",
        "pass" if not empty else "fail", f"empty: {empty}" if empty else "all non-empty")

    malformed = sorted({f"{p}-{n}" for p, n in re.findall(r"\b([SARTQ])-(\d+)\b", text)
                        if len(n) != 3})
    add("V2.6", "Q1.6", "Correctable", "Identifiers use the zero-padded three-digit scheme",
        "pass" if not malformed else "fail",
        f"malformed: {malformed}" if malformed else "all well-formed")

    # ---------------- Q2 / Q3 boundary and independence
    fences = re.findall(r"^\s*(?:```|~~~~)", text, re.M)
    add("V3.1", "Q2.1", "Blocking", "No fenced code block other than the metadata block",
        "pass" if len(fences) <= 2 else "fail",
        f"{len(fences) // 2} fenced block(s) found; exactly 1 (metadata) is permitted")
    # The framework's own root directory is `.claude/`. A repository path that contains it
    # is context, not a vendor selection, so path prefixes are removed before the scan.
    scannable = re.sub(r"\.claude(?=[/\\])", "", text, flags=re.I).lower()
    hits = [v for v in VENDOR_TOKENS if v in scannable]
    add("V3.2", "Q3.1", "Blocking", "No model, vendor, or provider named",
        "pass" if not hits else "fail",
        f"found: {hits}" if hits else "none (repository path prefixes excluded)")
    diffish = re.findall(r"^\s*(?:diff --git|\+\+\+ |--- [ab]/|@@ )", text, re.M)
    add("V3.3", "Q2.2", "Blocking", "No patch instruction, diff, or file-modification directive",
        "pass" if not diffish else "fail", f"{len(diffish)} diff marker(s)")

    # ---------------- Q5 task quality
    tb = body_of.get("Task Breakdown", "")
    task_blocks = re.split(r"^### (?=T-\d{3})", tb, flags=re.M)[1:]
    tasks = {}
    for blk in task_blocks:
        tid = re.match(r"(T-\d{3})", blk).group(1)
        fields = {}
        for f in TASK_FIELDS:
            mm = re.search(rf"^\s*[-*]\s*{re.escape(f)}\s*:\s*(.*)$", blk, re.M)
            if not mm:
                continue
            if f == "Acceptance Criteria":
                crits = []
                for ln in blk[mm.end():].split("\n"):
                    if re.match(r"^\s{2,}[-*]\s+\S", ln):
                        crits.append(ln.strip()[1:].strip())
                    elif ln.strip().startswith("###") or re.match(r"^\s*[-*]\s*\w+\s*:", ln):
                        break
                fields[f] = crits
            else:
                fields[f] = mm.group(1).strip()
        fields["_title"] = blk.split("\n", 1)[0].strip()
        tasks[tid] = fields

    add("V4.0", "Q5.1", "Blocking", "Task Breakdown contains at least one task",
        "pass" if tasks else "fail", f"{len(tasks)} task(s)")

    incomplete = {t: [f for f in TASK_FIELDS if not v.get(f)] for t, v in tasks.items()}
    incomplete = {t: f for t, f in incomplete.items() if f}
    add("V4.1", "Q5.1", "Blocking", "Every task carries all eight declared fields",
        "pass" if not incomplete else "fail",
        f"incomplete: {incomplete}" if incomplete else f"{len(tasks)} task(s) complete")

    bad_owner, multi_owner = {}, []
    for t, v in tasks.items():
        o = strip_md(v.get("Owner", ""))
        if "," in o or " and " in o:
            multi_owner.append(t)
        elif o and o not in facts["resolvable_agents"]:
            bad_owner[t] = o
    add("V4.2", "Q5.2", "Blocking",
        "Every task Owner resolves to a registered agent or an agent contract",
        "pass" if not bad_owner else "fail",
        f"unresolvable: {bad_owner}" if bad_owner else f"{len(tasks)} owner(s) resolved")
    add("V4.3", "Q5.3", "Blocking", "Exactly one owner per task",
        "pass" if not multi_owner else "fail",
        f"multi-owner: {multi_owner}" if multi_owner else "one owner each")

    bad_cx = {}
    for t, v in tasks.items():
        if not re.match(r"^(XS|S|M|L|XL)\s*\(confidence:\s*(high|medium|low)\)$",
                        strip_md(v.get("Complexity", ""))):
            bad_cx[t] = v.get("Complexity")
    add("V4.4", "Q5.1", "Blocking", "Complexity states a valid level and an explicit confidence",
        "pass" if not bad_cx else "fail", f"malformed: {bad_cx}" if bad_cx else "all valid")

    bad_status = {}
    for t, v in tasks.items():
        s = strip_md(v.get("Status", ""))
        if not s or s.split()[0] not in TASK_STATUSES:
            bad_status[t] = v.get("Status")
    add("V4.5", "Q5.1", "Blocking", "Task Status is ready, blocked, or assumption-dependent",
        "pass" if not bad_status else "fail", f"invalid: {bad_status}" if bad_status else "all valid")

    oq_body = body_of.get("Open Questions", "")
    q_defined = set()
    if oq_body.strip():
        for row in parse_table(oq_body, 2)[1]:
            if re.match(r"^Q-\d{3}$", strip_md(row[0])):
                q_defined.add(strip_md(row[0]))

    xl_bad = [t for t, v in tasks.items()
              if strip_md(v.get("Complexity", "")).startswith("XL")
              and not ids_of("Q", v.get("Description", ""))]
    add("V4.6", "Q5.7", "Blocking",
        "Every XL task carries an open question explaining non-decomposition",
        "pass" if not xl_bad else "fail",
        f"XL without question: {xl_bad}" if xl_bad else "no unexplained XL task")

    blocked_bad = [t for t, v in tasks.items()
                   if strip_md(v.get("Status", "")).startswith("blocked")
                   and not (ids_of("Q", v.get("Status", "")) or ids_of("Q", v.get("Description", "")))]
    add("V4.7", "Q5.8", "Blocking", "Every blocked task names its blocking open question",
        "pass" if not blocked_bad else "fail",
        f"blocked without question: {blocked_bad}" if blocked_bad else "none")

    no_crit = [t for t, v in tasks.items() if len(v.get("Acceptance Criteria") or []) < 1]
    add("V4.8", "Q5.6", "Blocking", "Every task states at least one acceptance criterion",
        "pass" if not no_crit else "fail",
        f"missing: {no_crit}" if no_crit else "all tasks carry criteria")

    bad_gate = {}
    for t, v in tasks.items():
        g = strip_md(v.get("Gate", ""))
        if g.lower() in {"none", "n/a", ""}:
            continue
        for part in [x.strip() for x in g.split(",")]:
            if part and part not in facts["gates"]:
                bad_gate.setdefault(t, []).append(part)
    add("V4.9", "Q9.3", "Blocking", "Named task gates exist in workflows/workflow-gate-matrix.md",
        "pass" if not bad_gate else "fail",
        f"unknown gates: {bad_gate}" if bad_gate else "all named gates resolve")

    # ---------------- Q6 dependency and order integrity
    dep = body_of.get("Dependencies", "")
    edges, ext_rows = [], []
    for headers, rows in parse_all_tables(dep, 3):
        h = [x.lower() for x in headers]
        if "from" in h and "to" in h and "type" in h:
            for r in rows:
                if len(r) >= 4:
                    edges.append((strip_md(r[0]), strip_md(r[1]), strip_md(r[2]), r[3].strip()))
        elif any("responsible" in x for x in h) or "blocks" in h:
            ext_rows.extend(rows)

    ext_parties = {strip_md(r[0]) for r in ext_rows if r and strip_md(r[0])}
    dangling = [e for e in edges
                if e[1] not in tasks
                or (e[0] not in tasks and e[0] not in ext_parties
                    and not e[0].lower().startswith("external"))]
    add("V5.1", "Q6.2", "Blocking",
        "Every dependency edge references existing tasks or a named external party",
        "pass" if not dangling else "fail",
        f"dangling: {dangling}" if dangling else f"{len(edges)} edge(s) resolve")

    bad_type = [e for e in edges if e[2] not in EDGE_TYPES or not e[3]]
    add("V5.2", "Q6.3", "Blocking", "Every edge carries a valid type and a justification",
        "pass" if not bad_type else "fail",
        f"invalid: {bad_type}" if bad_type else "all edges typed and justified")

    preds = {t: set() for t in tasks}
    for f, to, _ty, _j in edges:
        if to in preds and f in tasks:
            preds[to].add(f)

    waves_computed, remaining, guard = [], {t: set(p) for t, p in preds.items()}, 0
    while remaining and guard < 200:
        guard += 1
        ready = sorted([t for t, ps in remaining.items() if not (ps & set(remaining))])
        if not ready:
            break
        waves_computed.append(ready)
        for t in ready:
            remaining.pop(t)
    cyclic = sorted(remaining)
    add("V5.3", "Q6.1", "Blocking", "Dependency graph is acyclic",
        "pass" if not cyclic else "fail",
        f"cycle involves: {cyclic}" if cyclic else f"acyclic; {len(waves_computed)} wave(s)")

    stated = []
    for ln in dep.split("\n"):
        mm = re.match(r"^\s*[-*]?\s*\**Wave\s+(\d+)\**\s*:?\s*(.*)$", ln.strip(), re.I)
        if mm:
            stated.append((int(mm.group(1)), sorted(set(ids_of("T", mm.group(2))))))
    stated.sort()
    stated_waves = [w for _, w in stated]
    order_match = stated_waves == waves_computed
    add("V5.4", "Q6.4", "Blocking",
        "Stated implementation order recomputes exactly from the edge table",
        "pass" if order_match else "fail",
        "recomputed topological order equals stated order" if order_match
        else f"stated={stated_waves} recomputed={waves_computed}")
    add("V5.5", "Q6.5", "Blocking",
        "Wave 1 contains exactly the tasks with no unmet dependency",
        "pass" if (stated_waves and waves_computed and stated_waves[0] == waves_computed[0])
        else "fail",
        f"stated={stated_waves[0] if stated_waves else None} "
        f"computed={waves_computed[0] if waves_computed else None}")

    covered = {t for w in waves_computed for t in w}
    add("V5.6", "Q6.4", "Blocking", "Every task appears in the implementation order",
        "pass" if covered == set(tasks) else "fail",
        f"omitted: {sorted(set(tasks) - covered)}" if covered != set(tasks)
        else f"{len(covered)} task(s) ordered")

    # ---------------- Q7 assumption and risk integrity
    a_body = body_of.get("Assumptions", "")
    _ah, a_rows = parse_table(a_body, 5)
    a_ids = [strip_md(r[0]) for r in a_rows if re.match(r"^A-\d{3}$", strip_md(r[0]))]
    a_bad = [strip_md(r[0]) for r in a_rows
             if re.match(r"^A-\d{3}$", strip_md(r[0])) and any(not c.strip() for c in r[1:5])]
    none_assumptions = "none identified" in a_body.lower()
    add("V6.1", "Q7.1", "Blocking",
        "Every assumption states basis, impact-if-false, and a confirming role",
        "pass" if (not a_bad and (a_ids or none_assumptions)) else "fail",
        f"incomplete: {a_bad}" if a_bad else f"{len(a_ids)} assumption(s) complete")

    _rh, r_rows = parse_table(body_of.get("Risks", ""), 8)
    risks = [r for r in r_rows if re.match(r"^R-\d{3}$", strip_md(r[0]))]
    r_incomplete = [strip_md(r[0]) for r in risks if any(not c.strip() for c in r[1:8])]
    add("V6.2", "Q7.3", "Blocking",
        "Every risk carries class, trigger, impact, likelihood, attachment, mitigation, and owner",
        "pass" if not r_incomplete else "fail",
        f"incomplete: {r_incomplete}" if r_incomplete else f"{len(risks)} risk(s) complete")

    r_badclass = [strip_md(r[0]) for r in risks if strip_md(r[1]).lower() not in RISK_CLASSES]
    r_badlik = [strip_md(r[0]) for r in risks if strip_md(r[4]).lower() not in LIKELIHOODS]
    add("V6.3", "Q7.3", "Correctable", "Risk class and likelihood use the declared vocabularies",
        "pass" if not (r_badclass or r_badlik) else "fail",
        f"invalid class: {r_badclass}; invalid likelihood: {r_badlik}")

    r_unattached = []
    for r in risks:
        aff = strip_md(r[5])
        if "plan-wide" in aff.lower():
            continue
        refs = ids_of("T", aff)
        if not refs or any(x not in tasks for x in refs):
            r_unattached.append(strip_md(r[0]))
    add("V6.4", "Q7.4", "Blocking",
        "Every risk attaches to existing task identifiers or is marked plan-wide",
        "pass" if not r_unattached else "fail",
        f"unattached: {r_unattached}" if r_unattached else "all risks attached")

    r_owner_bad = [strip_md(r[0]) for r in risks
                   if strip_md(r[7]) and strip_md(r[7]) not in facts["resolvable_agents"]]
    add("V6.5", "Q5.2", "Correctable", "Every risk owner resolves to a known agent",
        "pass" if not r_owner_bad else "fail",
        f"unresolvable owners: {r_owner_bad}" if r_owner_bad else "all risk owners resolve")

    # ---------------- Q8 traceability closure
    trace_bad = {}
    for t, v in tasks.items():
        raw = v.get("Traces to", "")
        refs = ids_of("S", raw) + ids_of("A", raw)
        if not refs:
            trace_bad[t] = raw
        else:
            unknown = [x for x in refs if x.startswith("A-") and x not in a_ids]
            if unknown:
                trace_bad[t] = f"unknown assumption refs {unknown}"
    add("V7.1", "Q8.2", "Blocking",
        "Backward closure: every task traces to a statement or an assumption",
        "pass" if not trace_bad else "fail",
        f"untraced: {trace_bad}" if trace_bad else f"{len(tasks)} task(s) traced")

    if "Traceability Matrix" in body_of:
        _th, trows = parse_table(body_of["Traceability Matrix"], 2)
        uncovered = [strip_md(r[0]) for r in trows
                     if re.match(r"^S-\d{3}$", strip_md(r[0])) and not r[1].strip()]
        add("V7.2", "Q8.1", "Blocking",
            "Forward closure: every statement in the traceability matrix is covered",
            "pass" if not uncovered else "fail",
            f"uncovered: {uncovered}" if uncovered else f"{len(trows)} statement(s) covered")
    else:
        add("V7.2", "Q8.1", "Advisory",
            "Forward closure evidence present (matrix required when status is partial or blocked)",
            "pass" if meta.get("status") == "complete" else "fail",
            f"status={meta.get('status')} and no Traceability Matrix section")

    dangling_q = sorted(set(ids_of("Q", text)) - q_defined)
    add("V7.3", "Q8.3", "Blocking",
        "Lateral closure: every referenced open question is defined in Open Questions",
        "pass" if not dangling_q else "fail",
        f"undefined: {dangling_q}" if dangling_q else "all question references resolve")

    # ---------------- Q9 framework alignment
    sw = body_of.get("Suggested Workflow", "")
    named_wf = sorted([w for w in facts["workflows"] if re.search(rf"\b{re.escape(w)}\b", sw)])
    add("V8.1", "Q9.1", "Blocking", "Suggested workflow is a registered workflow identifier",
        "pass" if named_wf else "fail",
        f"selected: {named_wf}" if named_wf
        else f"no registered workflow named; registered={sorted(facts['workflows'])}")

    rc = body_of.get("Required Capabilities", "")
    declared_caps, declared_skills = [], []
    for headers, rows in parse_all_tables(rc, 3):
        h = [x.lower() for x in headers]
        if h and "capability" in h[0]:
            declared_caps += [strip_md(r[0]) for r in rows if strip_md(r[0])]
        elif h and "skill" in h[0]:
            declared_skills += [strip_md(r[0]) for r in rows if strip_md(r[0])]
    unknown_caps = [c for c in declared_caps if c not in facts["capabilities"]]
    add("V8.2", "Q9.4", "Blocking", "Declared capabilities exist in agents/capability-matrix.md",
        "pass" if not unknown_caps else "fail",
        f"unknown: {unknown_caps}" if unknown_caps
        else f"{len(declared_caps)} capability name(s) resolve")
    unknown_skills = [s for s in declared_skills
                      if not re.match(r"^S\d{2}$", s) or s not in facts["skills"]]
    add("V8.3", "Q9.5", "Blocking",
        "Declared skill identifiers exist in skills/agent-skill-matrix.md",
        "pass" if not unknown_skills else "fail",
        f"unknown: {unknown_skills}" if unknown_skills
        else f"{len(declared_skills)} skill identifier(s) resolve")

    # ---------------- Q10 acceptance and closure
    ac = body_of.get("Acceptance Criteria", "")
    n_ac = len(re.findall(r"^\s*\d+\.\s+\S", ac, re.M))
    add("V9.1", "Q10.1", "Blocking", "Acceptance Criteria is a non-empty numbered list",
        "pass" if n_ac >= 1 else "fail", f"{n_ac} criterion/criteria")

    dod = body_of.get("Definition of Done", "")
    n_dod = len(re.findall(r"^\s*[-*]\s*\[[ xX]\]", dod, re.M))
    add("V9.2", "Q10.3", "Blocking", "Definition of Done is an objectively checkable checklist",
        "pass" if n_dod >= 1 else "fail", f"{n_dod} checklist item(s)")

    if oq_body.strip():
        _qh, qrows = parse_table(oq_body, 5)
        q_bad = [strip_md(r[0]) for r in qrows
                 if re.match(r"^Q-\d{3}$", strip_md(r[0])) and any(not c.strip() for c in r[1:5])]
        add("V9.3", "Q10.5", "Blocking",
            "Every open question carries blocking status, owner, and affected tasks",
            "pass" if not q_bad else "fail",
            f"incomplete: {q_bad}" if q_bad else f"{len(q_defined)} question(s) complete")
    else:
        add("V9.3", "Q10.5", "Advisory", "Open Questions present when questions exist",
            "pass", "no open questions recorded")

    # ---------------- Q11 determinism
    if envelope:
        exp_in = envelope.get("context_slice", {}).get("input_digest")
        exp_ctx = envelope.get("context_slice", {}).get("context_digest")
        ok = meta.get("inputDigest") == exp_in and meta.get("contextDigest") == exp_ctx
        add("V10.1", "Q11.3", "Blocking",
            "Metadata digests match the frozen snapshot recorded in the invocation envelope",
            "pass" if ok else "fail",
            "digests match the envelope" if ok
            else f"plan=({meta.get('inputDigest')}, {meta.get('contextDigest')}) "
                 f"envelope=({exp_in}, {exp_ctx})")
    else:
        add("V10.1", "Q11.3", "Advisory", "Digest cross-check against the invocation envelope",
            "not-machine-checkable", "no envelope supplied")

    seqs = {
        "A": sorted(set(a_ids)),
        "R": sorted({strip_md(r[0]) for r in risks}),
        "T": sorted(tasks),
        "Q": sorted(q_defined),
    }
    gaps = {}
    for p, xs in seqs.items():
        if not xs:
            continue
        want = [f"{p}-{i:03d}" for i in range(1, len(xs) + 1)]
        if xs != want:
            gaps[p] = xs
    add("V10.2", "Q11.1", "Correctable",
        "Identifiers are unique, zero-padded, and ascending without gaps",
        "pass" if not gaps else "fail",
        f"non-contiguous: {gaps}" if gaps
        else f"A={len(seqs['A'])} R={len(seqs['R'])} T={len(seqs['T'])} Q={len(seqs['Q'])}")

    # ---------------- declared not-machine-checkable obligations
    for cid, qref, desc in [
        ("N1", "Q4.4", "Business objectives express outcomes, not activities"),
        ("N2", "Q5.4", "Every task satisfies the five R6 validity conditions"),
        ("N3", "Q6.8", "No edge encodes preference rather than necessity"),
        ("N4", "Q2.8", "Task descriptions state completion conditions, not implementation approach"),
        ("N5", "Q7.6", "No inference appears in the artifact without a registered assumption"),
    ]:
        add(cid, qref, "Advisory", desc, "not-machine-checkable",
            "agent self-verification obligation; not decidable by artifact inspection")

    rep.counts = {
        "sections": len(titles),
        "tasks": len(tasks),
        "assumptions": len(a_ids),
        "risks": len(risks),
        "edges": len(edges),
        "waves": len(waves_computed),
        "openQuestions": len(q_defined),
        "planStatus": meta.get("status"),
    }
    return rep


def main():
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    ap = argparse.ArgumentParser(
        description="Validate execution-plan.md against the Planner Agent contract")
    ap.add_argument("artifact")
    ap.add_argument("--envelope", help="invocation envelope JSON, enables the digest cross-check")
    ap.add_argument("--json-out")
    a = ap.parse_args()

    env = json.loads(Path(a.envelope).read_text(encoding="utf-8")) if a.envelope else None
    rep = validate(Path(a.artifact), env)
    d = rep.to_dict()

    if a.json_out:
        Path(a.json_out).write_text(json.dumps(d, indent=2), encoding="utf-8")

    print(f"Validation Engine: {a.artifact}")
    print(f"  result      : {d['result'].upper()}")
    print(f"  checks      : {d['checksPassed']}/{d['checksRun']} passed "
          f"({d['notMachineCheckable']} declared not-machine-checkable)")
    print(f"  blocking    : {d['blockingFailures']}    correctable: {d['correctableFailures']}")
    print(f"  counts      : {d['counts']}")
    for c in rep.checks:
        if c.result == "fail":
            print(f"  FAIL [{c.severity}] {c.id} ({c.quality_ref}): {c.description} -- {c.detail}")
    return 0 if rep.passed else 1


if __name__ == "__main__":
    sys.exit(main())
