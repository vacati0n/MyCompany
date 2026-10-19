#!/usr/bin/env python3
"""Validation Engine: technical-design.md conformance.

Mechanically checks a produced technical design package, and any architecture decision
record emitted alongside it, against the Architect Agent's own contract:

  - `.claude/agents/architect/output.md`     structural and semantic contract
  - `.claude/agents/architect/quality.md`    check set A1 to A16 and severities
  - `.claude/templates/technical-design.md`  rendered form
  - `.claude/templates/architecture-decision-record.md`

Scope boundary, matching `plan_validator.py`: this covers the checks in `quality.md` that
are decidable by inspecting the artifact, its sibling decision records, the supplied
inputs named by the invocation envelope, and the framework registries. Checks that need
semantic judgement -- A6.2 "a layer name is not a module" is the clearest -- are reported
as `not-machine-checkable` and remain the agent's own obligation. Nothing is skipped
silently.

Two checks are cross-artifact rather than structural, and they are the reason this
validator exists rather than a generic section checker:

  D3.3 / D16.5  planner task identifiers referenced by the design must exist in the
                execution plan the envelope supplied, and may never be invented here
  D8.4          the recorded selection must be the option the evaluation table marks as
                selected, so a design cannot state one approach and evaluate another
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

import yaml

from artifact_lib import (
    CLAUDE, VENDOR_TOKENS, Check, Report, framework_facts, ids_of,
    parse_all_tables, parse_table, split_sections, strip_comments, strip_md,
)

MANDATORY_SECTIONS = [
    "Metadata",
    "Objective",
    "Requirements Summary",
    "Current-State Assumptions and Constraints",
    "Architecture and Component Design",
    "API and Data Model Impact",
    "Reusable Components and Reuse Rationale",
    "Operational Considerations",
    "Delivery Plan",
    "Risks and Mitigations",
    "Estimate and Confidence",
    "Open Decisions and Escalations",
    "Sign-off",
]

METADATA_FIELDS = [
    "designId", "changeReference", "sourceInputs", "producedBy", "agentVersion",
    "schemaVersion", "status", "decisionRecords", "consumesPlan",
    "inputDigest", "contextDigest",
]
# `decisionRecords` is legitimately empty when no decision is architecture-significant.
METADATA_MAY_BE_EMPTY = {"decisionRecords"}

DESIGN_STATUSES = {"complete", "provisional", "blocked"}
IMPACT_TYPES = {
    "contract-change", "behavior-change", "extension",
    "dependency-change", "operational-impact", "no-change-verified",
}
CONFIDENCE_VALUES = {"confirmed", "speculative"}
REUSE_OUTCOMES = {"reuse-as-is", "reuse-extended", "rejected", "none-found"}
CONSTRAINT_CLASSES = {
    "functional", "quality-attribute", "security", "compliance",
    "operability", "structural", "migration",
}
CONSTRAINT_FORCE = {"hard", "negotiable"}
RISK_CLASSES = {
    "structural", "contract", "migration", "security",
    "performance", "operability", "delivery",
}
LIKELIHOODS = {"high", "medium", "low"}
EFFORT_LEVELS = ["XS", "S", "M", "L", "XL"]

# Effort is a complexity level, never a duration (A12.1). Scheduling and resourcing belong
# to the tech lead (A10.7).
DURATION_TOKENS = r"\b(hour|hours|day|days|week|weeks|month|months|sprint|sprints|quarter|quarters)\b"
SCHEDULING_TOKENS = r"\b(milestone|milestones|deadline|deadlines|schedule|scheduled|headcount|staffing|resourcing|fte)\b"

ADR_SECTIONS = [
    "Metadata", "Context", "Decision", "Alternatives Considered",
    "Consequences", "Validation Plan", "Approval",
]

# A sign-off line is signed when it carries a completed signature: an explicit "signed
# by", an approval, an initials marker, or a signature date. "to be signed at the Design
# Gate" is an unsigned line and must not trip this.
SIGNED_MARK = re.compile(r"\b(signed by|approved|countersigned)\b|/s/|\d{4}-\d{2}-\d{2}", re.I)

# `output.md` fixes the section set, the identifier schemes, and what each section must
# contain. It does not fix bullet syntax, and it governs over the template where the two
# differ. So a labelled item is recognised whether it is rendered as a `- Label:` bullet or
# as a standalone `Label:` line introducing a list or a paragraph.
LABEL_LINE = re.compile(r"^\s*(?:[-*]\s*)?\**[A-Za-z][A-Za-z -]{0,45}\**\s*:")


# --------------------------------------------------------------------------- helpers


def labelled(body: str, label: str) -> str | None:
    """Return the text of a `- <label>:` bullet, including its nested lines.

    Returns None when the label is absent, which the caller reports as a structural
    failure rather than silently treating as empty.
    """
    lines = strip_comments(body).split("\n")
    # A label may carry a qualifying clause before its colon: "Out of structural scope,
    # which bounds the impact surface:" is the same contract item as "Out of scope:".
    pat = re.compile(
        rf"^\s*(?:[-*]\s*)?\**{re.escape(label)}\b[^:\n]{{0,80}}\**\s*:\s*(.*)$", re.I)
    for i, ln in enumerate(lines):
        m = pat.match(ln)
        if not m:
            continue
        out = [m.group(1)]
        for nxt in lines[i + 1:]:
            if LABEL_LINE.match(nxt) and not re.match(r"^\s{2,}", nxt):
                break
            if nxt.startswith("#") or nxt.strip().startswith("|"):
                break
            out.append(nxt)
        return "\n".join(out).strip()
    return None


def labelled_any(body: str, *labels: str) -> str | None:
    """First matching spelling of a label. The contract names the content, not the wording.

    "Out of scope" and "Out of structural scope" are the same contract item; a validator
    that accepts only one of them is checking the template, not the contract.
    """
    for lab in labels:
        found = labelled(body, lab)
        if found is not None:
            return found
    return None


def items(block: str | None) -> list:
    """Content items of a labelled block: its inline value plus its bullets.

    A bullet that wraps across several physical lines is one item. Splitting on newlines
    would report the trace identifier carried on a bullet's last line as its own untraced
    item, which is a rendering artefact rather than a contract failure.
    """
    if not block:
        return []
    out = []
    for ln in block.split("\n"):
        s = ln.strip()
        if not s:
            continue
        if s.startswith("-") or s.startswith("*"):
            out.append(s.lstrip("-*").strip())
        elif out:
            out[-1] = f"{out[-1]} {s}"
        else:
            out.append(s)
    return [x for x in out if x]


def rows_with_prefix(body: str, prefix: str, min_cols: int = 2) -> list:
    """Every table row in `body` whose first cell is a `<prefix>-nnn` identifier."""
    out = []
    for _headers, rows in parse_all_tables(body, min_cols):
        for r in rows:
            if re.match(rf"^{prefix}-\d{{3}}$", strip_md(r[0])):
                out.append(r)
    return out


def table_by_headers(body: str, *needles: str):
    for headers, rows in parse_all_tables(body, 2):
        low = [h.lower() for h in headers]
        if all(any(n in h for h in low) for n in needles):
            return headers, rows
    return [], []


def cell(row: list, i: int) -> str:
    return strip_md(row[i]) if i < len(row) else ""


def nonempty(v: str) -> bool:
    return bool(v) and v.lower() not in {"n/a", "-", "tbd", "todo"}


def topo(nodes: dict) -> tuple:
    """(waves, cyclic_nodes) over {node: {prerequisites}}."""
    waves, remaining, guard = [], {k: set(v) for k, v in nodes.items()}, 0
    while remaining and guard < 500:
        guard += 1
        ready = sorted([n for n, ps in remaining.items() if not (ps & set(remaining))])
        if not ready:
            break
        waves.append(ready)
        for n in ready:
            remaining.pop(n)
    return waves, sorted(remaining)


def supplied_inputs(envelope: dict | None) -> list:
    if not envelope:
        return []
    return (envelope.get("input_contract") or {}).get("supplied") or []


# --------------------------------------------------------------------------- validation


def validate(artifact_path: Path, envelope: dict | None = None) -> Report:
    text = artifact_path.read_text(encoding="utf-8")
    rep = Report(artifact=str(artifact_path))
    facts = framework_facts()

    def add(cid, qref, severity, description, result, detail=""):
        rep.checks.append(Check(cid, qref, severity, description, result, detail))

    # ------------------------------------------------- D1 metadata block
    m = re.search(r"```yaml\s*\n(.*?)\n```", text, re.S)
    meta = {}
    if not m:
        add("D1.1", "A1.5", "Blocking", "Metadata block present and parseable", "fail",
            "no leading fenced yaml metadata block found")
    else:
        try:
            meta = (yaml.safe_load(m.group(1)) or {}).get("design", {}) or {}
            missing = [f for f in METADATA_FIELDS
                       if f not in meta
                       or (meta[f] in (None, "", []) and f not in METADATA_MAY_BE_EMPTY)]
            add("D1.1", "A1.5", "Blocking",
                "Metadata block present with all fields populated",
                "fail" if missing else "pass",
                f"missing or empty: {missing}" if missing
                else f"{len(METADATA_FIELDS)} of {len(METADATA_FIELDS)} fields populated")
        except Exception as exc:  # noqa: BLE001
            add("D1.1", "A1.5", "Blocking", "Metadata block present and parseable", "fail", str(exc))

    add("D1.2", "A1.5", "Blocking", "producedBy is architect",
        "pass" if meta.get("producedBy") == "architect" else "fail",
        f"producedBy={meta.get('producedBy')!r}")
    add("D1.3", "A1.5", "Blocking", "agentVersion is 1.0.0 and schemaVersion is 1.0.0",
        "pass" if str(meta.get("agentVersion")) == "1.0.0"
        and str(meta.get("schemaVersion")) == "1.0.0" else "fail",
        f"agentVersion={meta.get('agentVersion')} schemaVersion={meta.get('schemaVersion')}")
    add("D1.4", "A1.7", "Blocking", "status is one of complete, provisional, blocked",
        "pass" if meta.get("status") in DESIGN_STATUSES else "fail",
        f"status={meta.get('status')!r}")

    # ------------------------------------------------- D2 structural conformance
    sections = split_sections(text)
    titles = [t for t, _ in sections]
    body_of = {}
    for t, b in sections:
        body_of.setdefault(t, b)

    present = [t for t in titles if t in MANDATORY_SECTIONS]
    dupes = sorted({t for t in present if present.count(t) > 1})
    missing_sec = [t for t in MANDATORY_SECTIONS if t not in titles]
    add("D2.1", "A1.1", "Blocking", "All thirteen mandatory sections present exactly once",
        "fail" if (missing_sec or dupes) else "pass",
        f"missing={missing_sec} duplicated={dupes}" if (missing_sec or dupes)
        else "13 of 13 present, once each")
    add("D2.2", "A1.1", "Blocking", "Mandatory sections appear in contract order",
        "pass" if present == [t for t in MANDATORY_SECTIONS if t in titles] else "fail",
        f"observed order: {present}")
    add("D2.3", "A1.2", "Blocking", "Section titles match output.md exactly",
        "pass" if not missing_sec else "fail",
        "exact-title match" if not missing_sec else f"unmatched: {missing_sec}")

    extra = [t for t in titles
             if t not in MANDATORY_SECTIONS and not t.lower().startswith("appendix")]
    add("D2.4", "A1.4", "Correctable",
        "No unpermitted level-2 sections introduced; supplementary material is an appendix",
        "pass" if not extra else "fail", f"unpermitted: {extra}" if extra else "none")

    empty = [t for t in MANDATORY_SECTIONS
             if t in body_of and not strip_comments(body_of[t]).strip()]
    add("D2.5", "A1.3", "Blocking",
        "No mandatory section is empty; an empty section reads None identified.",
        "pass" if not empty else "fail", f"empty: {empty}" if empty else "all non-empty")

    malformed = sorted({f"{p}-{n}" for p, n in re.findall(r"\b([SFACMODPRQ])-(\d+)\b", text)
                        if len(n) != 3})
    add("D2.6", "A1.6", "Correctable",
        "Identifiers use the zero-padded three-digit scheme", "pass" if not malformed else "fail",
        f"malformed: {malformed}" if malformed else "all well-formed")

    # ------------------------------------------------- D3 boundary compliance
    fences = re.findall(r"^\s*(?:```|~~~~)", text, re.M)
    add("D3.1", "A2.1", "Blocking",
        "No code, pseudocode, or schema block other than the metadata block",
        "pass" if len(fences) <= 2 else "fail",
        f"{len(fences) // 2} fenced block(s); exactly 1 (metadata) is permitted")

    diffish = re.findall(r"^\s*(?:diff --git|\+\+\+ |--- [ab]/|@@ )", text, re.M)
    add("D3.2", "A2.2", "Blocking",
        "No patch instruction, diff, or file-modification directive",
        "pass" if not diffish else "fail", f"{len(diffish)} diff marker(s)")

    plan_text = next((s["text"] for s in supplied_inputs(envelope)
                      if s.get("type") == "execution-plan"), None)
    plan_tasks = set(re.findall(r"^###\s+(T-\d{3})", plan_text or "", re.M))
    referenced_tasks = set(ids_of("T", text))
    consumes = str(meta.get("consumesPlan", "")).strip().lower()
    if plan_text is None and consumes in {"none", ""}:
        add("D3.3", "A2.4", "Blocking",
            "No planner task identifier is created; none may appear without a supplied plan",
            "pass" if not referenced_tasks else "fail",
            f"invented task ids: {sorted(referenced_tasks)}" if referenced_tasks
            else "no task identifier present")
    else:
        invented = sorted(referenced_tasks - plan_tasks)
        add("D3.3", "A2.4", "Blocking",
            "Every referenced planner task exists in the supplied execution plan",
            "pass" if not invented else "fail",
            f"not defined by the supplied plan: {invented}" if invented
            else f"{len(referenced_tasks)} of {len(plan_tasks)} supplied task(s) referenced")

    bad_status = re.findall(r"^\s*[-*]?\s*\**Status\**\s*:\s*\**(\w+)", text, re.M | re.I)
    non_proposed = sorted({s for s in bad_status if s.lower() in
                           {"accepted", "superseded", "rejected", "approved"}})
    add("D3.4", "A2.5", "Blocking",
        "No decision record is referenced at a status other than Proposed",
        "pass" if not non_proposed else "fail",
        f"found status value(s): {non_proposed}" if non_proposed else "none")

    signoff = strip_comments(body_of.get("Sign-off", ""))
    signed = [ln.strip() for ln in signoff.split("\n")
              if re.match(r"^\s*[-*]\s*[A-Za-z].*:", ln) and SIGNED_MARK.search(ln.split(":", 1)[1])]
    add("D3.5", "A2.6", "Blocking", "No sign-off line is signed by the producing agent",
        "pass" if not signed else "fail", f"signed: {signed}" if signed else "all lines unsigned")

    scannable = re.sub(r"\.claude(?=[/\\])", "", text, flags=re.I).lower()
    hits = [v for v in VENDOR_TOKENS if v in scannable]
    add("D3.6", "A4.1", "Blocking", "No model, vendor, provider, or agent runtime named",
        "pass" if not hits else "fail",
        f"found: {hits}" if hits else "none (repository path prefixes excluded)")

    # ------------------------------------------------- D4 objective and requirements
    obj = body_of.get("Objective", "")
    obj_block = labelled_any(obj, "Architectural objectives", "Architectural objective")
    obj_items = items(obj_block)
    untraced_obj = [o for o in obj_items if not ids_of("S", o)]
    add("D4.1", "A5.1", "Blocking",
        "Every architectural objective traces to a statement identifier",
        "pass" if (obj_items and not untraced_obj) else "fail",
        "no Architectural objectives block found" if obj_block is None
        else (f"untraced: {untraced_obj}" if untraced_obj
              else f"{len(obj_items)} objective(s) traced"))

    oos = labelled_any(obj, "Out of scope", "Out of structural scope",
                       "Explicitly out of scope")
    add("D4.2", "A5.4", "Correctable",
        "Structural out-of-scope is stated and non-empty",
        "pass" if items(oos) else "fail",
        f"{len(items(oos))} exclusion(s)" if items(oos) else "out-of-scope is absent or empty")

    req = body_of.get("Requirements Summary", "")
    req_untraced, req_count = [], 0
    for label in ("Functional requirements", "Non-functional requirements", "Acceptance criteria"):
        block = labelled_any(req, label, label.replace(" requirements", " requirement"),
                             "Acceptance intent" if label == "Acceptance criteria" else label)
        if block is None:
            req_untraced.append(f"{label}: block absent")
            continue
        for it in items(block):
            if it.lower().startswith("none identified"):
                continue
            req_count += 1
            if not ids_of("S", it):
                req_untraced.append(f"{label}: {it[:60]}")
    add("D4.3", "A5.3", "Blocking", "Every requirement traces to a statement identifier",
        "pass" if not req_untraced else "fail",
        f"untraced: {req_untraced}" if req_untraced else f"{req_count} requirement(s) traced")

    # ------------------------------------------------- D5 register integrity
    reg = body_of.get("Current-State Assumptions and Constraints", "")
    f_rows = rows_with_prefix(reg, "F", 3)
    a_rows = rows_with_prefix(reg, "A", 5)
    c_rows = rows_with_prefix(reg, "C", 5)

    f_bad = [cell(r, 0) for r in f_rows if not (nonempty(cell(r, 1)) and nonempty(cell(r, 2)))]
    add("D5.1", "A3.2", "Blocking",
        "Every fact states the property and the supplied context that establishes it",
        "pass" if (f_rows and not f_bad) else "fail",
        f"incomplete: {f_bad}" if f_bad else
        (f"{len(f_rows)} fact(s) complete" if f_rows else "no fact recorded"))

    a_bad = [cell(r, 0) for r in a_rows
             if not all(nonempty(cell(r, i)) for i in (1, 2, 3, 4))]
    add("D5.2", "A3.3", "Blocking",
        "Every assumption states need, impact-if-false, and a confirming role",
        "pass" if not a_bad else "fail",
        f"incomplete: {a_bad}" if a_bad else f"{len(a_rows)} assumption(s) complete")

    def norm(s):
        return re.sub(r"[^a-z0-9 ]", "", s.lower()).strip()

    f_texts = {norm(cell(r, 1)): cell(r, 0) for r in f_rows}
    collisions = [(cell(r, 0), f_texts[norm(cell(r, 1))]) for r in a_rows
                  if norm(cell(r, 1)) in f_texts]
    add("D5.3", "A3.1", "Blocking", "Facts and assumptions are disjoint",
        "pass" if not collisions else "fail",
        f"same claim registered twice: {collisions}" if collisions
        else f"{len(f_rows)} fact(s) and {len(a_rows)} assumption(s) are distinct")

    c_bad = [cell(r, 0) for r in c_rows
             if not all(nonempty(cell(r, i)) for i in (1, 2, 3, 4))]
    add("D5.4", "A3.4", "Blocking", "Every constraint states class, text, force, and source",
        "pass" if not c_bad else "fail",
        f"incomplete: {c_bad}" if c_bad else f"{len(c_rows)} constraint(s) complete")

    c_badclass = [cell(r, 0) for r in c_rows if cell(r, 1).lower() not in CONSTRAINT_CLASSES]
    c_badforce = [cell(r, 0) for r in c_rows if cell(r, 3).lower() not in CONSTRAINT_FORCE]
    add("D5.5", "A3.4", "Correctable",
        "Constraint class and force use the declared vocabularies",
        "pass" if not (c_badclass or c_badforce) else "fail",
        f"invalid class: {c_badclass}; invalid force: {c_badforce}")

    # ------------------------------------------------- D6 impact analysis
    arch = body_of.get("Architecture and Component Design", "")
    m_rows = rows_with_prefix(arch, "M", 5)
    known_fa = {cell(r, 0) for r in f_rows} | {cell(r, 0) for r in a_rows}

    m_bad = [cell(r, 0) for r in m_rows
             if not all(nonempty(cell(r, i)) for i in (1, 2, 3, 5))]
    add("D6.1", "A6.1", "Blocking",
        "Every impacted module states name, impact type, basis, and confidence",
        "pass" if (m_rows and not m_bad) else "fail",
        f"incomplete: {m_bad}" if m_bad else
        (f"{len(m_rows)} module(s) complete" if m_rows else "no impacted module recorded"))

    m_badtype = [cell(r, 0) for r in m_rows if cell(r, 2).lower() not in IMPACT_TYPES]
    add("D6.2", "A6.1", "Blocking", "Every impact type uses the declared vocabulary",
        "pass" if not m_badtype else "fail",
        f"invalid: {m_badtype}" if m_badtype else f"{len(m_rows)} type(s) valid")

    m_badbasis = []
    for r in m_rows:
        refs = ids_of("F", cell(r, 3)) + ids_of("A", cell(r, 3))
        if not refs or any(x not in known_fa for x in refs):
            m_badbasis.append(cell(r, 0))
    add("D6.3", "A6.3", "Blocking",
        "Every module basis resolves to an existing fact or assumption",
        "pass" if not m_badbasis else "fail",
        f"unresolved basis: {m_badbasis}" if m_badbasis else f"{len(m_rows)} basis reference(s) resolve")

    m_badconf = [cell(r, 0) for r in m_rows if cell(r, 5).lower() not in CONFIDENCE_VALUES]
    add("D6.4", "A6.1", "Blocking", "Every module confidence is confirmed or speculative",
        "pass" if not m_badconf else "fail",
        f"invalid: {m_badconf}" if m_badconf else f"{len(m_rows)} confidence value(s) valid")

    risk_body = body_of.get("Risks and Mitigations", "")
    r_rows = rows_with_prefix(risk_body, "R", 8)
    risk_refs = " ".join(cell(r, 5) for r in r_rows)
    speculative = [cell(r, 0) for r in m_rows if cell(r, 5).lower() == "speculative"]
    spec_unrisked = [x for x in speculative if x not in ids_of("M", risk_refs)]
    add("D6.5", "A6.6", "Blocking", "Every speculative impact has a matching risk",
        "pass" if not spec_unrisked else "fail",
        f"speculative without risk: {spec_unrisked}" if spec_unrisked
        else f"{len(speculative)} speculative module(s), all risked")

    # ------------------------------------------------- D7 reuse discipline
    reuse = body_of.get("Reusable Components and Reuse Rationale", "")
    _rh, reuse_rows = table_by_headers(reuse, "capability", "outcome")
    reuse_bad = [cell(r, 0) or "(unnamed)" for r in reuse_rows
                 if not all(nonempty(cell(r, i)) for i in (0, 1, 2, 3))]
    add("D7.1", "A7.2", "Blocking",
        "Every reuse survey row states capability, candidate, outcome, and rationale",
        "pass" if (reuse_rows and not reuse_bad) else "fail",
        f"incomplete: {reuse_bad}" if reuse_bad else
        (f"{len(reuse_rows)} survey row(s) complete" if reuse_rows else "no reuse survey recorded"))

    reuse_badout = [cell(r, 0) for r in reuse_rows if cell(r, 2).lower() not in REUSE_OUTCOMES]
    add("D7.2", "A7.2", "Blocking", "Every reuse outcome uses the declared vocabulary",
        "pass" if not reuse_badout else "fail",
        f"invalid: {reuse_badout}" if reuse_badout else f"{len(reuse_rows)} outcome(s) valid")

    nf_thin = [cell(r, 0) for r in reuse_rows
               if cell(r, 2).lower() == "none-found" and len(cell(r, 3)) < 20]
    add("D7.3", "A7.3", "Blocking", "Every none-found outcome states where the search looked",
        "pass" if not nf_thin else "fail",
        f"search basis missing: {nf_thin}" if nf_thin else "search basis stated for every none-found")

    rej_thin = [cell(r, 0) for r in reuse_rows
                if cell(r, 2).lower() == "rejected" and len(cell(r, 3)) < 20]
    add("D7.4", "A7.5", "Blocking", "Every rejected candidate states why it is unsuitable",
        "pass" if not rej_thin else "fail",
        f"rationale missing: {rej_thin}" if rej_thin else "rationale stated for every rejection")

    # ------------------------------------------------- D8 options and selection
    o_rows = rows_with_prefix(arch, "O", 3)
    o_headers = []
    for headers, rows in parse_all_tables(arch, 3):
        if any(re.match(r"^O-\d{3}$", strip_md(r[0])) for r in rows):
            o_headers = headers
            break

    selected_txt = labelled(arch, "Selected") or ""
    forced = [cell(r, 0) for r in c_rows
              if cell(r, 3).lower() == "hard" and cell(r, 0) in ids_of("C", selected_txt)]
    add("D8.1", "A8.1", "Blocking",
        "At least two materially different options, or a hard constraint named as forcing",
        "pass" if (len(o_rows) >= 2 or (o_rows and forced)) else "fail",
        f"{len(o_rows)} option(s) recorded; forcing hard constraint(s): {forced or 'none'}")

    o_gaps = {cell(r, 0): [o_headers[i] if i < len(o_headers) else f"col{i}"
                           for i in range(1, max(len(o_headers), len(r)))
                           if not nonempty(cell(r, i))]
              for r in o_rows}
    o_gaps = {k: v for k, v in o_gaps.items() if v}
    add("D8.2", "A8.3", "Blocking", "Every option is evaluated against every recorded criterion",
        "pass" if not o_gaps else "fail",
        f"unevaluated cells: {o_gaps}" if o_gaps
        else f"{len(o_rows)} option(s) x {max(len(o_headers) - 1, 0)} criteria complete")

    eliminated = [r for r in o_rows if re.search(r"eliminat|rejected", " ".join(r), re.I)]
    elim_bad = [cell(r, 0) for r in eliminated if not ids_of("C", " ".join(r))]
    add("D8.3", "A8.4", "Blocking",
        "Every eliminated option names the hard constraint it violates",
        "pass" if not elim_bad else "fail",
        f"no constraint cited: {elim_bad}" if elim_bad
        else f"{len(eliminated)} eliminated option(s) cite a constraint")

    sel_ids = ids_of("O", selected_txt)
    sel_row = next((r for r in o_rows if cell(r, 0) in sel_ids), None)
    sel_marked = bool(sel_row) and bool(re.search(r"select", " ".join(sel_row), re.I))
    add("D8.4", "A8.5", "Blocking",
        "The recorded selection is the option the evaluation table marks selected",
        "pass" if (len(sel_ids) == 1 and sel_marked) else "fail",
        f"Selected names {sel_ids}; evaluation table marks it selected: {sel_marked}")

    rejected_alt = labelled(arch, "Highest-scoring rejected alternative and why it lost")
    add("D8.5", "A8.6", "Blocking",
        "At least one rejected alternative is recorded with its rationale",
        "pass" if (rejected_alt and ids_of("O", rejected_alt) and len(rejected_alt) > 30)
        else "fail",
        f"value={(rejected_alt or '')[:80]!r}")

    tradeoffs = labelled(arch, "Tradeoffs accepted")
    add("D8.6", "A8.7", "Blocking", "Tradeoffs accepted by the selection are stated",
        "pass" if items(tradeoffs) else "fail",
        f"{len(items(tradeoffs))} tradeoff(s)" if items(tradeoffs) else "absent or empty")

    # ------------------------------------------------- D9 decisions
    d_rows = rows_with_prefix(arch, "D", 4)
    d_bad = [cell(r, 0) for r in d_rows if not all(nonempty(cell(r, i)) for i in (1, 2, 3))]
    add("D9.1", "A1.8", "Blocking",
        "Every decision states the decision, its significance, and its record reference",
        "pass" if (d_rows and not d_bad) else "fail",
        f"incomplete: {d_bad}" if d_bad else
        (f"{len(d_rows)} decision(s) complete" if d_rows else "no decision recorded"))

    significant = [cell(r, 0) for r in d_rows if re.match(r"^(yes|true)$", cell(r, 2), re.I)]
    declared_records = [str(x).strip() for x in (meta.get("decisionRecords") or [])]
    add("D9.2", "A1.8", "Blocking",
        "decisionRecords lists exactly the architecture-significant decisions",
        "pass" if sorted(declared_records) == sorted(significant) else "fail",
        f"metadata={sorted(declared_records)} significant in 5.4={sorted(significant)}")

    d_nooption = [cell(r, 0) for r in d_rows if not ids_of("O", " ".join(r) + " " + selected_txt)]
    add("D9.3", "A15.3", "Blocking", "Every decision traces to an option",
        "pass" if not d_nooption else "fail",
        f"no option trace: {d_nooption}" if d_nooption else f"{len(d_rows)} decision(s) traced")

    # ------------------------------------------------- D10 contract and migration
    api = strip_comments(body_of.get("API and Data Model Impact", ""))
    contract_modules = [cell(r, 0) for r in m_rows if cell(r, 2).lower() == "contract-change"]
    absent = [x for x in contract_modules if x not in ids_of("M", api)]
    add("D10.1", "A9.1", "Blocking",
        "Every contract-change module appears in the API and Data Model Impact section",
        "pass" if not absent else "fail",
        f"absent: {absent}" if absent
        else f"{len(contract_modules)} contract-change module(s) present")

    if contract_modules:
        has_transition = bool(re.search(r"\b(transition|compatibility|coexist)", api, re.I))
        has_rollback = bool(re.search(r"\brollback|revers", api, re.I))
        add("D10.2", "A9.2", "Blocking",
            "Every contract change carries a transition strategy and a rollback position",
            "pass" if (has_transition and has_rollback) else "fail",
            f"transition stated={has_transition}, rollback stated={has_rollback}")
    else:
        add("D10.2", "A9.2", "Blocking",
            "Every contract change carries a transition strategy and a rollback position",
            "pass", "no contract-change module declared")

    mig = labelled_any(api, "Schema or migration changes", "Schema and migration changes",
                       "Schema or data model changes", "Schema and data model changes")
    mig_items = [i for i in items(mig) if not re.match(r"^none\b", i.strip(), re.I)]
    if mig_items:
        joined = " ".join(mig_items).lower()
        ok = all(k in joined for k in ("direction", "revers"))
        add("D10.3", "A9.4", "Blocking",
            "Every data migration states direction, reversibility, and transition behavior",
            "pass" if ok else "fail",
            f"direction stated={'direction' in joined}, reversibility stated={'revers' in joined}")
    else:
        add("D10.3", "A9.4", "Blocking",
            "Every data migration states direction, reversibility, and transition behavior",
            "pass", "no data migration declared")

    # ------------------------------------------------- D11 sequencing
    plan = body_of.get("Delivery Plan", "")
    p_rows = rows_with_prefix(plan, "P", 5)
    p_ids = {cell(r, 0) for r in p_rows}
    m_ids = {cell(r, 0) for r in m_rows}

    p_bad = [cell(r, 0) for r in p_rows
             if not all(nonempty(cell(r, i)) for i in (1, 2, 3, 4))]
    add("D11.1", "A10.1", "Blocking",
        "Every sequencing constraint states modules, prerequisites, and a structural reason",
        "pass" if (p_rows and not p_bad) else "fail",
        f"incomplete: {p_bad}" if p_bad else
        (f"{len(p_rows)} constraint(s) complete" if p_rows else "no sequencing constraint recorded"))

    prereq = {}
    dangling_p = []
    for r in p_rows:
        refs = ids_of("P", cell(r, 3))
        prereq[cell(r, 0)] = set(refs)
        dangling_p += [x for x in refs if x not in p_ids]
    add("D11.2", "A10.2", "Blocking", "Every prerequisite references an existing plan step",
        "pass" if not dangling_p else "fail",
        f"dangling: {sorted(set(dangling_p))}" if dangling_p else "all prerequisites resolve")

    waves, cyclic = topo(prereq)
    add("D11.3", "A10.3", "Blocking", "The prerequisite graph is acyclic",
        "pass" if not cyclic else "fail",
        f"cycle involves: {cyclic}" if cyclic else f"acyclic; {len(waves)} wave(s)")

    p_badmod = [cell(r, 0) for r in p_rows
                if not ids_of("M", cell(r, 2)) or any(x not in m_ids for x in ids_of("M", cell(r, 2)))]
    add("D11.4", "A15.4", "Blocking",
        "Every sequencing constraint resolves to existing impacted modules",
        "pass" if not p_badmod else "fail",
        f"unresolved modules: {p_badmod}" if p_badmod else f"{len(p_rows)} module reference set(s) resolve")

    sched = sorted(set(re.findall(SCHEDULING_TOKENS, strip_comments(plan), re.I)))
    add("D11.5", "A10.7", "Correctable",
        "No milestone, schedule, or resourcing commitment appears in the delivery plan",
        "pass" if not sched else "fail",
        f"scheduling language: {sched}" if sched else "none")

    test_focus = re.search(r"^###\s+Test Strategy Focus Areas\s*$(.*?)(?=^###|\Z)",
                           plan, re.S | re.M)
    rollout = re.search(r"^###\s+Rollout and Rollback\s*$(.*?)(?=^###|\Z)", plan, re.S | re.M)
    tf_ok = bool(test_focus) and bool(items(strip_comments(test_focus.group(1))))
    ro_ok = bool(rollout) and bool(items(strip_comments(rollout.group(1))))
    add("D11.6", "A1.3", "Blocking",
        "Test strategy focus areas and the rollout and rollback strategy are stated",
        "pass" if (tf_ok and ro_ok) else "fail",
        f"test focus present={tf_ok}, rollout and rollback present={ro_ok}")

    # ------------------------------------------------- D12 risk integrity
    r_bad = [cell(r, 0) for r in r_rows
             if not all(nonempty(cell(r, i)) for i in (1, 2, 3, 4, 5, 6, 7))]
    add("D12.1", "A11.1", "Blocking",
        "Every risk states class, trigger, impact, likelihood, attachment, mitigation, and owner",
        "pass" if (r_rows and not r_bad) else "fail",
        f"incomplete: {r_bad}" if r_bad else
        (f"{len(r_rows)} risk(s) complete" if r_rows else "no risk recorded"))

    attach_pool = m_ids | {cell(r, 0) for r in d_rows} | p_ids
    r_unattached = []
    for r in r_rows:
        refs = ids_of("M", cell(r, 5)) + ids_of("D", cell(r, 5)) + ids_of("P", cell(r, 5))
        if not refs or any(x not in attach_pool for x in refs):
            r_unattached.append(cell(r, 0))
    add("D12.2", "A11.2", "Blocking",
        "Every risk attaches to an existing module, decision, or plan step",
        "pass" if not r_unattached else "fail",
        f"unattached: {r_unattached}" if r_unattached else f"{len(r_rows)} risk(s) attached")

    r_badclass = [cell(r, 0) for r in r_rows if cell(r, 1).lower() not in RISK_CLASSES]
    r_badlik = [cell(r, 0) for r in r_rows if cell(r, 4).lower() not in LIKELIHOODS]
    add("D12.3", "A11.1", "Correctable",
        "Risk class and likelihood use the declared vocabularies",
        "pass" if not (r_badclass or r_badlik) else "fail",
        f"invalid class: {r_badclass}; invalid likelihood: {r_badlik}")

    def unresolved_owners(cells):
        bad = []
        for owner in cells:
            parts = [x.strip() for x in re.split(r"[,/]| and ", owner) if x.strip()]
            if not parts or any(x not in facts["resolvable_agents"] for x in parts):
                bad.append(owner)
        return bad

    r_badowner = unresolved_owners([cell(r, 7) for r in r_rows])
    add("D12.4", "A14.2", "Correctable", "Every risk owner resolves to a known agent",
        "pass" if not r_badowner else "fail",
        f"unresolvable: {r_badowner}" if r_badowner else f"{len(r_rows)} owner(s) resolve")

    ops = strip_comments(body_of.get("Operational Considerations", ""))
    sec = labelled_any(ops, "Security considerations", "Security and compliance considerations",
                       "Security")
    sec_ok = bool(sec) and (not sec.lower().startswith("none identified") or len(sec) > 30)
    add("D12.5", "A11.5", "Blocking",
        "Security and compliance impact is assessed, with a reason when none is identified",
        "pass" if sec_ok else "fail",
        f"value={(sec or '')[:80]!r}")

    # ------------------------------------------------- D13 estimation
    est = strip_comments(body_of.get("Estimate and Confidence", ""))
    overall = labelled_any(est, "Overall", "Overall estimate") or ""
    # The level may be rendered as code, emphasised, or followed by prose justification.
    # What the check decides is that a level and an explicit confidence are both stated.
    overall_head = strip_md(overall.strip().split("\n")[0])
    lvl = re.match(r"^(XS|S|M|L|XL)\b.*?\(confidence:\s*(high|medium|low)\)",
                   overall_head, re.I)
    add("D13.1", "A12.2", "Blocking",
        "Overall effort is a complexity level carrying an explicit confidence qualifier",
        "pass" if lvl else "fail", f"value={overall.strip()[:80]!r}")

    durations = sorted(set(re.findall(DURATION_TOKENS, est, re.I)))
    add("D13.2", "A12.1", "Blocking", "Effort is never expressed as a duration",
        "pass" if not durations else "fail",
        f"duration language: {durations}" if durations else "no duration language")

    scope_asm = labelled_any(est, "Scope assumptions", "Scope assumption")
    add("D13.3", "A12.5", "Blocking", "The scope assumptions behind the estimate are stated",
        "pass" if items(scope_asm) else "fail",
        f"{len(items(scope_asm))} assumption(s)" if items(scope_asm) else "absent or empty")

    q_body = body_of.get("Open Decisions and Escalations", "")
    q_rows = rows_with_prefix(q_body, "Q", 6)
    is_xl = bool(lvl) and lvl.group(1).upper() == "XL"
    add("D13.4", "A12.4", "Blocking",
        "An XL estimate carries a corresponding open decision",
        "pass" if (not is_xl or q_rows) else "fail",
        f"overall={lvl.group(1) if lvl else None}, open decisions={len(q_rows)}")

    # ------------------------------------------------- D14 open decisions
    q_bad = [cell(r, 0) for r in q_rows if not all(nonempty(cell(r, i)) for i in (1, 2, 3, 4, 5))]
    add("D14.1", "A1.3", "Blocking",
        "Every open decision states the question, blocking status, owner, attachment, and consequence",
        "pass" if not q_bad else "fail",
        f"incomplete: {q_bad}" if q_bad else f"{len(q_rows)} open decision(s) complete")

    blocking_q = [cell(r, 0) for r in q_rows if re.match(r"^(yes|true|blocking)$", cell(r, 2), re.I)]
    consistent = (meta.get("status") == "blocked") == bool(blocking_q)
    add("D14.2", "A1.7", "Blocking",
        "A blocking open decision sets package status to blocked, and only then",
        "pass" if consistent else "fail",
        f"status={meta.get('status')!r}, blocking entries={blocking_q}")

    q_badowner = unresolved_owners([cell(r, 3) for r in q_rows])
    add("D14.3", "A14.2", "Correctable", "Every open decision owner resolves to a known agent",
        "pass" if not q_badowner else "fail",
        f"unresolvable: {q_badowner}" if q_badowner else f"{len(q_rows)} owner(s) resolve")

    # ------------------------------------------------- D15 framework alignment
    named_gates = sorted(set(re.findall(r"\b([A-Z][A-Za-z]+ Gate)\b", text)))
    unknown_gates = [g for g in named_gates if g not in facts["gates"]]
    add("D15.1", "A14.1", "Blocking",
        "Every named gate exists in workflows/workflow-gate-matrix.md",
        "pass" if not unknown_gates else "fail",
        f"unknown: {unknown_gates}" if unknown_gates
        else f"{len(named_gates)} gate name(s) resolve")

    # Agent identifiers carry no common prefix, so a reference is any resolvable identifier
    # the text names, plus any identifier in the retired `omn-` naming scheme, which only
    # resolves while the registry still lists it.
    named_agents = sorted(
        {a for a in facts["resolvable_agents"]
         if re.search(rf"(?<![\w-]){re.escape(a)}(?![\w-])", text)}
        | set(re.findall(r"(?<![\w-])(omn-[a-z0-9-]+)\b", text)))
    unknown_agents = [a for a in named_agents if a not in facts["resolvable_agents"]]
    add("D15.2", "A14.2", "Blocking",
        "Every referenced agent resolves in registry/agents.yaml or under agents/",
        "pass" if not unknown_agents else "fail",
        f"unresolvable: {unknown_agents}" if unknown_agents
        else f"{len(named_agents)} agent reference(s) resolve")

    named_skills = sorted(set(re.findall(r"\bS\d{2}\b", text)))
    unknown_skills = [s for s in named_skills if s not in facts["skills"]]
    add("D15.3", "A14.4", "Blocking",
        "Every referenced skill identifier exists in skills/agent-skill-matrix.md",
        "pass" if not unknown_skills else "fail",
        f"unknown: {unknown_skills}" if unknown_skills
        else f"{len(named_skills)} skill identifier(s) resolve")

    named_templates = sorted(set(re.findall(r"templates/[a-z0-9-]+\.md", text)))
    # A template this design proposes to create is named in the impacted-module table, so
    # it is declared scope rather than an invented reference. A14.5 governs the templates
    # the design relies on as current state; those must already be registered.
    proposed = {x for x in named_templates
                if any(x in " ".join(r) for r in m_rows)}
    unknown_templates = [x for x in named_templates
                         if x not in facts["templates"] and x not in proposed]
    add("D15.4", "A14.5", "Blocking",
        "Every referenced template is registered, or is declared as an impacted module "
        "this design proposes",
        "pass" if not unknown_templates else "fail",
        f"unregistered and undeclared: {unknown_templates}" if unknown_templates
        else f"{len(named_templates)} template reference(s) resolve "
             f"({len(proposed)} proposed as impacted module(s))")

    # ------------------------------------------------- D16 traceability and determinism
    defined = {
        "F": {cell(r, 0) for r in f_rows},
        "A": {cell(r, 0) for r in a_rows},
        "C": {cell(r, 0) for r in c_rows},
        "M": m_ids,
        "O": {cell(r, 0) for r in o_rows},
        "D": {cell(r, 0) for r in d_rows},
        "P": p_ids,
        "R": {cell(r, 0) for r in r_rows},
        "Q": {cell(r, 0) for r in q_rows},
    }
    dangling = {}
    for prefix, known in defined.items():
        refs = set(ids_of(prefix, text))
        missing_refs = sorted(refs - known)
        if missing_refs:
            dangling[prefix] = missing_refs
    add("D16.1", "A15.4", "Blocking",
        "Lateral closure: every referenced identifier is defined in its own register",
        "pass" if not dangling else "fail",
        f"undefined: {dangling}" if dangling
        else "; ".join(f"{k}={len(v)}" for k, v in defined.items()))

    if envelope:
        exp_in = envelope.get("context_slice", {}).get("input_digest")
        exp_ctx = envelope.get("context_slice", {}).get("context_digest")
        ok = meta.get("inputDigest") == exp_in and meta.get("contextDigest") == exp_ctx
        add("D16.2", "A16.3", "Blocking",
            "Metadata digests match the frozen snapshot recorded in the invocation envelope",
            "pass" if ok else "fail",
            "digests match the envelope" if ok
            else f"design=({meta.get('inputDigest')}, {meta.get('contextDigest')}) "
                 f"envelope=({exp_in}, {exp_ctx})")
    else:
        add("D16.2", "A16.3", "Advisory", "Digest cross-check against the invocation envelope",
            "not-machine-checkable", "no envelope supplied")

    gaps = {}
    for prefix, known in defined.items():
        xs = sorted(known)
        if not xs:
            continue
        want = [f"{prefix}-{i:03d}" for i in range(1, len(xs) + 1)]
        if xs != want:
            gaps[prefix] = xs
    add("D16.3", "A16.1", "Correctable",
        "Identifiers are unique, zero-padded, and ascending without gaps",
        "pass" if not gaps else "fail",
        f"non-contiguous: {gaps}" if gaps
        else "; ".join(f"{k}={len(v)}" for k, v in defined.items()))

    binds = " ".join(cell(r, 5) for r in p_rows)
    unbound = sorted(plan_tasks - set(ids_of("T", binds))) if plan_tasks else []
    add("D16.4", "A15.5", "Correctable",
        "No supplied planner task is left unaddressed by the sequencing constraints",
        "pass" if not unbound else "fail",
        f"unaddressed: {unbound}" if unbound
        else (f"{len(plan_tasks)} supplied task(s) bound" if plan_tasks
              else "no execution plan supplied"))

    # ------------------------------------------------- D17 decision records
    adr_dir = artifact_path.parent
    adr_files = sorted(adr_dir.glob("architecture-decision-record-*.md"))
    adr_ids = {}
    for f in adr_files:
        mm = re.search(r"(D-\d{3})", f.name)
        if mm:
            adr_ids[mm.group(1)] = f
    add("D17.1", "A13.1", "Blocking",
        "Every architecture-significant decision has exactly one decision record",
        "pass" if sorted(adr_ids) == sorted(significant) else "fail",
        f"records={sorted(adr_ids)} significant={sorted(significant)}")

    adr_texts = {d: f.read_text(encoding="utf-8") for d, f in adr_ids.items()}
    bad_struct = [d for d, tx in adr_texts.items()
                  if [s for s, _ in split_sections(tx) if s in ADR_SECTIONS] != ADR_SECTIONS]
    add("D17.2", "A13.4", "Blocking",
        "Every decision record carries the seven template sections in order",
        "pass" if not bad_struct else "fail",
        f"malformed: {bad_struct}" if bad_struct else f"{len(adr_texts)} record(s) conform")

    bad_state = [d for d, tx in adr_texts.items()
                 if not re.search(r"^\s*[-*]\s*Status\s*:\s*Proposed\s*$", tx, re.M | re.I)]
    add("D17.3", "A13.2", "Blocking", "Every decision record is at status Proposed",
        "pass" if not bad_state else "fail",
        f"not Proposed: {bad_state}" if bad_state else f"{len(adr_texts)} record(s) at Proposed")

    bad_cite = [d for d, tx in adr_texts.items()
                if not (ids_of("C", tx) and ids_of("F", tx) and ids_of("M", tx))]
    add("D17.4", "A13.4", "Blocking",
        "Every decision record cites its constraints, baseline facts, and impacted modules",
        "pass" if not bad_cite else "fail",
        f"incomplete citations: {bad_cite}" if bad_cite else f"{len(adr_texts)} record(s) cite all three")

    bad_valid = []
    for d, tx in adr_texts.items():
        vp = next((b for s, b in split_sections(tx) if s == "Validation Plan"), "")
        if not (items(labelled_any(vp, "Metrics to monitor", "Metrics"))
                and items(labelled_any(vp, "Verification checkpoints", "Verification"))
                and items(labelled_any(vp, "Rollback or reversal conditions",
                                       "Rollback or reversal condition",
                                       "Reversal conditions", "Rollback conditions"))):
            bad_valid.append(d)
    add("D17.5", "A13.5", "Blocking",
        "Every decision record states a validation plan and a reversal condition",
        "pass" if not bad_valid else "fail",
        f"incomplete: {bad_valid}" if bad_valid else f"{len(adr_texts)} validation plan(s) complete")

    bad_signed = []
    for d, tx in adr_texts.items():
        ap = next((b for s, b in split_sections(tx) if s == "Approval"), "")
        for ln in ap.split("\n"):
            if re.match(r"^\s*[-*]\s*[A-Za-z].*:", ln) and SIGNED_MARK.search(ln.split(":", 1)[1]):
                bad_signed.append(d)
                break
    add("D17.6", "A13.6", "Blocking", "No decision record approval block is signed",
        "pass" if not bad_signed else "fail",
        f"signed: {bad_signed}" if bad_signed else f"{len(adr_texts)} approval block(s) unsigned")

    bad_alts = []
    option_ids = defined["O"]
    for d, tx in adr_texts.items():
        alt = next((b for s, b in split_sections(tx) if s == "Alternatives Considered"), "")
        cited = set(ids_of("O", alt))
        if not cited or not cited.issubset(option_ids):
            bad_alts.append(d)
    add("D17.7", "A13.3", "Blocking",
        "Every decision record's alternatives are carried from the package evaluation table",
        "pass" if not bad_alts else "fail",
        f"divergent: {bad_alts}" if bad_alts else f"{len(adr_texts)} record(s) match the option set")

    # ------------------------------------------------- declared not-machine-checkable
    for cid, qref, desc in [
        ("N1", "A2.7", "No external system, repository, or ticketing access occurred"),
        ("N2", "A2.9", "No product-scope decision was taken without product-owner authority"),
        ("N3", "A2.10", "No claim is made that designed work was performed"),
        ("N4", "A3.5", "No assumption was promoted to a fact without a new citation"),
        ("N5", "A6.2", "Every module name is a named module, not a layer or generic grouping"),
        ("N6", "A6.4", "Modules a reader would expect to be impacted appear, including no-change-verified"),
        ("N7", "A8.2", "Options differ structurally, not only in naming or sequencing"),
        ("N8", "A10.6", "No step is ordered by preference rather than structural necessity"),
        ("N9", "A15.1", "Forward closure: every supplied statement reaches an objective, module, constraint, or question"),
        ("N10", "A4.2", "Every technology named traces to the supplied context or an input statement"),
    ]:
        add(cid, qref, "Advisory", desc, "not-machine-checkable",
            "agent self-verification obligation; not decidable by artifact inspection")

    rep.counts = {
        "sections": len(titles),
        "facts": len(f_rows),
        "assumptions": len(a_rows),
        "constraints": len(c_rows),
        "modules": len(m_rows),
        "options": len(o_rows),
        "decisions": len(d_rows),
        "reuseRows": len(reuse_rows),
        "planSteps": len(p_rows),
        "risks": len(r_rows),
        "openDecisions": len(q_rows),
        "decisionRecords": len(adr_files),
        "designStatus": meta.get("status"),
    }
    return rep


def main():
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    ap = argparse.ArgumentParser(
        description="Validate technical-design.md against the Architect Agent contract")
    ap.add_argument("artifact")
    ap.add_argument("--envelope", help="invocation envelope JSON, enables the digest and plan cross-checks")
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
