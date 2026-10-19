#!/usr/bin/env python3
"""Task Context -- the compact, runtime-owned shared state every phase reads first.

`runs/<run-id>/task-context.yaml` is the lightweight execution state that
`config/runtime.md#task-context` specifies. It is derived, never authored: the runtime
rebuilds it from the persisted run (state store, gate records, committed artifacts) at every
dispatch, completion, and gate decision, so it can never disagree with the evidence it
summarises. Nothing in it is an agent's narrative. Every entry is a one-line fact carrying the
identifier the source artifact already gave it, so a downstream agent can open the source
section by identifier when it needs the full statement.

Why this exists. Before it, each phase's agent re-derived the objective, scope, decisions,
constraints, and changed files by reading every upstream artifact in full: 22-72 KB per
artifact, per phase, per attempt. The task context carries those facts once, at roughly 5-10
KB, and the dispatch prompt names the upstream sections an agent still reads in full.

Three rules bound what enters:

1. facts are extracted by deterministic table and bullet parsing (`artifact_lib`), never by
   summarisation, so two rebuilds over the same run produce byte-identical output;
2. every cell is capped (`CELL_MAX`) and every list is capped (`ROWS_MAX`); an artifact that
   exceeds a cap is cited by identifier and the reader opens the source;
3. nothing is copied from an artifact that the Validation Engine has not accepted. A rejected
   or superseded attempt contributes nothing.

Affected areas are derived here too: the domain triggers in `DOMAIN_SKILLS` are scanned over
the supplied inputs and the accepted upstream artifacts, and the result decides which
domain-conditional skills a phase reads (`skills/skill-resolver.md`, section
"Domain-conditional skill reading"). When no text is available the derivation reports
`determinable: false` and every conditional skill stays required, because "unknown" must never
narrow what an agent reads.
"""

from __future__ import annotations

import re
from pathlib import Path

import yaml

import artifact_lib as al

SCHEMA = "framework.runtime/task-context.v1"
FILENAME = "task-context.yaml"

CELL_MAX = 160          # characters kept from any one table cell or bullet
ROWS_MAX = 40           # rows kept from any one register
OBJECTIVE_MAX = 700     # characters of the request kept as the objective

# --------------------------------------------------------------------- domain policy

# Domain-conditional skills, per `skills/skill-resolver.md` ("Domain-conditional skill
# reading"). A code absent from this table is domain-general and is read whenever a Phase
# Model or an agent manifest requires it. A code present here is *resolved* exactly as before
# (registry resolution, guard G1, and the phase-mandatory rule are unchanged) but is *read*
# only when its domain is affected by the task -- or when affectedness cannot be determined.
DOMAIN_SKILLS = {
    "S04": {"area": "avalonia-ui",
            "pattern": r"\b(avalonia|axaml|xaml|view-?model|mvvm|desktop ui)\b"},
    "S05": {"area": "react-frontend",
            "pattern": r"\b(react|jsx|tsx|use(state|effect|memo)|redux|next\.js|frontend|"
                       r"front-end|web ui)\b"},
    "S06": {"area": "database",
            "pattern": r"\b(database|sql|schema migration|migration|entity ?framework|ef core|"
                       r"dbcontext|persistence layer|stored procedure|orm|nosql|mongodb|"
                       r"postgres(ql)?|sqlite|sql server|transaction boundary|data model)\b"},
    "S08": {"area": "performance",
            "pattern": r"\b(performance|latency|throughput|benchmark|p9[059]|load test|"
                       r"profil(e|ing)|memory footprint|hot path|scalab(le|ility)|"
                       r"slow(ness)?)\b"},
    "S09": {"area": "security",
            "pattern": r"\b(security|authenticat(e|ion)|authoriz(e|ation)|token|secret|"
                       r"password|credential|permission|encrypt(ion)?|crypto(graphy)?|"
                       r"injection|sanitiz(e|ation)|xss|csrf|cors|least privilege|"
                       r"vulnerab(le|ility)|tls|certificate|pii|gdpr)\b"},
}

# Review and validation phases keep the security skill required whatever the derivation says:
# a reviewer is the last line of defence, and "the task did not mention security" is not
# evidence that nothing security-relevant changed. Recorded as a policy trade-off in
# `config/runtime.md` ("Conditional Skill Dispatch").
SECURITY_ALWAYS_PHASES = {
    "quality-review", "code-quality-review", "structural-compliance", "regression-validation",
    "behavioral-validation", "test-risk-validation", "candidate-validation",
    "repository-quality-scan", "readiness-assessment", "merge-decision",
}

# Which sections of an accepted artifact a downstream reader opens first, and which it opens
# only when a fact it needs is absent from the task context. Titles are matched
# case-insensitively after stripping numbering, so `4.3 Constraints` matches `Constraints`.
# An artifact type absent here is read in full, which is the safe default.
UPSTREAM_SECTIONS = {
    "scope-definition.md": {
        "read_first": ["In Scope", "Out of Scope", "Acceptance Criteria",
                       "Constraints and Dependencies", "Scope Decisions", "Open Questions"],
        "on_demand": ["Business Context", "Handoff"],
    },
    "execution-plan.md": {
        "read_first": ["Technical Objectives", "Scope", "Task Breakdown", "Dependencies",
                       "Acceptance Criteria", "Definition of Done", "Open Questions"],
        "on_demand": ["Executive Summary", "Business Objectives", "Assumptions", "Risks",
                      "Suggested Workflow", "Required Capabilities", "Traceability Matrix"],
    },
    "technical-design.md": {
        "read_first": ["Objective", "Constraints", "Impacted Modules", "Selected Approach",
                       "Decisions", "API and Data Model Impact", "Delivery Plan",
                       "Open Decisions and Escalations"],
        "on_demand": ["Requirements Summary", "Facts", "Assumptions", "Options Considered",
                      "Reusable Components and Reuse Rationale", "Operational Considerations",
                      "Risks and Mitigations", "Estimate and Confidence", "Sign-off"],
    },
    "implementation-report.md": {
        "read_first": ["Implementation Summary", "Change Set", "Test Evidence",
                       "Verification Results", "Deviations and Tradeoffs", "Open Questions"],
        "on_demand": ["Boundary Compliance", "Residual Risk", "Handoff Notes"],
    },
    "review-package.md": {
        "read_first": ["Findings", "Correction Requests", "Verdict", "Open Questions"],
        "on_demand": ["Review Scope", "Severity Summary",
                      "Standards and Architecture Conformance", "Test Adequacy Assessment",
                      "Residual Risk"],
    },
    "validation-report.md": {
        "read_first": ["Acceptance Criteria Results", "Defects", "Verdict", "Open Questions"],
        "on_demand": ["Validation Scope", "Test Strategy", "Execution Summary",
                      "Regression Assessment", "Residual Risk"],
    },
    "bug-analysis.md": {
        "read_first": ["Symptom Summary", "Impact Assessment", "Root Cause Analysis",
                       "Fix Strategy", "Validation Plan", "Open Questions"],
        "on_demand": ["Reproduction", "Closure"],
    },
}

# Section title (lower-cased, numbering stripped) -> task-context register. Matching is by
# prefix or suffix, so `5.4 Decisions` and `Scope Decisions` both reach a register; the first
# matching key in this order wins.
SECTION_REGISTERS = [
    ("in scope", "scope.in_scope"),
    ("out of scope", "scope.out_of_scope"),
    ("acceptance criteria", "validation_requirements.acceptance_criteria"),
    ("test strategy focus", "validation_requirements.test_focus"),
    ("test evidence", "validation_requirements.evidence"),
    ("validation plan", "validation_requirements.test_focus"),
    ("verification results", "validation_requirements.verification"),
    ("sequencing constraints", "constraints"),
    ("facts", "facts"),
    ("assumptions", "assumptions"),
    ("constraints", "constraints"),
    ("open decisions", "open_questions"),
    ("open questions", "open_questions"),
    ("scope decisions", "decisions"),
    ("decisions", "decisions"),
    ("selected approach", "decisions"),
    ("deviations", "decisions"),
    ("impacted modules", "repository_context.relevant_modules"),
    ("change set", "changed_files"),
    ("risks", "risks"),
    ("residual risk", "risks"),
    ("findings", "findings"),
    ("correction requests", "findings"),
    ("defects", "findings"),
    ("verdict", "verdicts"),
    ("fix strategy", "decisions"),
    ("root cause", "decisions"),
    ("impact assessment", "risks"),
    ("technical objectives", "objectives"),
    ("objective", "objectives"),
    ("implementation order", "constraints"),
]


# --------------------------------------------------------------------- helpers


def _cap(text: str, n: int = CELL_MAX) -> str:
    t = re.sub(r"\s+", " ", al.strip_md(text or "")).strip()
    return t if len(t) <= n else t[: n - 3].rstrip() + "..."


def _norm_title(title: str) -> str:
    t = re.sub(r"^[0-9]+(\.[0-9]+)*\s*", "", title or "").strip().lower()
    t = re.sub(r"\s+\{#.*\}$", "", t)
    return t


def _register_for(title: str) -> str | None:
    t = _norm_title(title)
    for key, reg in SECTION_REGISTERS:
        if t.startswith(key) or t.endswith(key):
            return reg
    return None


def _column(headers: list, name: str):
    for i, h in enumerate(headers or []):
        if al.strip_md(h).strip().lower() == name:
            return i
    return None


def _rows_from_table(headers: list, rows: list) -> list:
    """Render table rows as `ID: primary | secondary | tertiary` one-liners."""
    out = []
    if not headers:
        return out
    for cells in rows[:ROWS_MAX]:
        cells = [al.strip_md(c) for c in cells]
        if not any(c.strip() for c in cells):
            continue
        ident = cells[0].strip()
        body = [c for c in cells[1:] if c.strip()]
        if not body and not ident:
            continue
        # The identifier plus the three most informative cells: by template convention the
        # statement and its qualifiers come first.
        line = (f"{ident}: " if ident else "") + " | ".join(_cap(c, 110) for c in body[:3])
        out.append(_cap(line, CELL_MAX * 2))
    return out


def _bullets(body: str) -> list:
    out = []
    for ln in al.strip_comments(body).splitlines():
        s = ln.strip()
        if s.startswith(("- ", "* ")) and len(s) > 2:
            val = s[2:].strip()
            if val and not set(val) <= set("-: "):
                out.append(_cap(val))
        if len(out) >= ROWS_MAX:
            break
    return out


def _task_headings(body: str) -> list:
    """`### T-001 <title>` subsections of a plan's Task Breakdown, with their dependencies."""
    out = []
    cur = None
    for ln in body.splitlines():
        m = re.match(r"^###\s+(T-[0-9]+)\s+(.*)$", ln.strip())
        if m:
            cur = {"id": m.group(1), "title": _cap(m.group(2), 100), "depends_on": ""}
            out.append(cur)
            continue
        if cur is not None:
            d = re.match(r"^-\s*Depends on:\s*(.*)$", ln.strip())
            if d:
                cur["depends_on"] = _cap(d.group(1), 60)
    return [f"{t['id']}: {t['title']}"
            + (f" | depends on {t['depends_on']}" if t["depends_on"] else "")
            for t in out[:ROWS_MAX]]


def _set(target: dict, dotted: str, values: list):
    if not values:
        return
    parts = dotted.split(".")
    d = target
    for p in parts[:-1]:
        d = d.setdefault(p, {})
    existing = d.setdefault(parts[-1], [])
    for v in values:
        if v not in existing and len(existing) < ROWS_MAX * 2:
            existing.append(v)


# --------------------------------------------------------------------- extraction


def _sections_with_subsections(text: str) -> list:
    """Level-2 sections split further at their level-3 headings.

    A design's registers live under `### 5.1 Impacted Modules` and `### 5.4 Decisions`, inside
    one level-2 section that names no register of its own. Each subsection is offered under its
    own title; a subsection whose title names no register inherits its parent's title, so
    `### 4.1 Facts` under `## Current-State Assumptions and Constraints` still reaches the
    constraints register. The `Task Breakdown` section is kept whole, because its `### T-nnn`
    headings are the facts.
    """
    out = []
    for title, body in al.split_sections(text):
        if _norm_title(title).startswith("task breakdown"):
            out.append((title, body))
            continue
        parts, cur, buf, fence = [], None, [], False
        for ln in body.split("\n"):
            s = ln.lstrip()
            if s.startswith("```") or s.startswith("~~~~"):
                fence = not fence
            if not fence and ln.startswith("### "):
                parts.append((cur, "\n".join(buf)))
                cur, buf = ln[4:].strip(), []
            else:
                buf.append(ln)
        parts.append((cur, "\n".join(buf)))
        for sub, sub_body in parts:
            if sub is None:
                out.append((title, sub_body))
            else:
                out.append((sub if _register_for(sub) else title, sub_body))
    return out


def extract_facts(artifact_name: str, text: str) -> dict:
    """Deterministic one-line facts from an accepted artifact, keyed by task-context register.

    Every fact keeps the identifier the artifact gave it. Tables win over bullets in a section
    that has both; a `Task Breakdown` section yields its `T-nnn` headings. `artifact_name`
    is accepted for symmetry with the section maps and reserved for type-specific rules.
    """
    facts: dict = {}
    for title, body in _sections_with_subsections(text):
        nt = _norm_title(title)
        if nt.startswith("task breakdown"):
            _set(facts, "plan_tasks", _task_headings(body))
            continue
        reg = _register_for(title)
        if reg is None:
            continue
        clean = al.strip_comments(body)
        rows = []
        for headers, trows in al.parse_all_tables(clean):
            rows += _rows_from_table(headers, trows)
        if not rows:
            rows = _bullets(clean)
        if reg == "changed_files":
            # Change-set rows are the repository's changed files: keep the path cell first.
            paths = []
            for headers, trows in al.parse_all_tables(clean):
                pi = _column(headers, "path")
                ti = _column(headers, "change type")
                for cells in trows[:ROWS_MAX]:
                    if pi is not None and pi < len(cells) and cells[pi].strip():
                        p = al.strip_md(cells[pi]).strip()
                        ct = (al.strip_md(cells[ti]).strip()
                              if ti is not None and ti < len(cells) else "")
                        paths.append(_cap(f"{p} ({ct})" if ct else p, 140))
            rows = paths or rows
        _set(facts, reg, rows)
    return facts


def objective_from_input(text: str) -> str:
    """The request, reduced to its opening statement: headings and blank lines dropped."""
    lines = []
    for ln in (text or "").splitlines():
        s = ln.strip()
        if not s or s.startswith("#") or s.startswith("```") or s.startswith("---"):
            continue
        if s.startswith(("- ", "* ", "| ")):
            s = s[2:].strip()
        lines.append(s)
        if sum(len(x) for x in lines) > OBJECTIVE_MAX:
            break
    return _cap(" ".join(lines), OBJECTIVE_MAX)


# --------------------------------------------------------------------- affected areas


def derive_affected_areas(sources: list, declared: list | None = None) -> dict:
    """Scan `[(source_label, text), ...]` for the domain triggers in DOMAIN_SKILLS.

    Returns the areas that fired with the evidence that fired them, the areas that did not,
    the operator-declared areas (always treated as affected), and `determinable`, which is
    False when no text was available -- the case in which every conditional skill stays
    required.
    """
    declared = sorted({d.strip().lower() for d in (declared or []) if d and d.strip()})
    triggered: dict = {}
    for code, spec in DOMAIN_SKILLS.items():
        rx = re.compile(spec["pattern"], re.I)
        hits = []
        for label, text in sources:
            if not text:
                continue
            m = rx.search(text)
            if m:
                hits.append({"source": label, "term": m.group(0).lower()})
        if hits:
            triggered[spec["area"]] = hits[:3]
    areas = sorted({s["area"] for s in DOMAIN_SKILLS.values()})
    for d in declared:
        triggered.setdefault(d, [{"source": "operator", "term": "declared"}])
    return {
        "determinable": any(t for _, t in sources) or bool(declared),
        "declared": declared,
        "triggered": triggered,
        "not_triggered": [a for a in areas if a not in triggered],
        "sources_scanned": [label for label, t in sources if t],
    }


def skill_dispatch(phase_id: str, codes: list, areas: dict, registry_records: list) -> list:
    """Per skill code: `required` or `not-triggered`, with the basis for each verdict.

    Domain-general codes are always `required`. Domain-conditional codes are `required`
    when their area is triggered or declared, when affectedness is not determinable, or
    when the phase is a review or validation phase and the code is the security skill.
    Otherwise they are `not-triggered`: resolved and available, not read.
    """
    by_code = {r.get("skillCode"): r for r in registry_records or []}
    out = []
    for code in codes:
        rec = by_code.get(code) or {}
        entry = {"skillCode": code, "displayName": rec.get("displayName"),
                 "specificationPath": rec.get("specificationPath")}
        spec = DOMAIN_SKILLS.get(code)
        if spec is None:
            entry.update(status="required", basis="domain-general skill")
        elif not areas.get("determinable", False):
            entry.update(status="required", area=spec["area"],
                         basis="affected areas not determinable; conditional skill kept required")
        elif code == "S09" and phase_id in SECURITY_ALWAYS_PHASES:
            entry.update(status="required", area=spec["area"],
                         basis="security is always read in review and validation phases")
        elif spec["area"] in (areas.get("triggered") or {}):
            ev = (areas["triggered"][spec["area"]] or [{}])[0]
            entry.update(status="required", area=spec["area"],
                         basis=f"area {spec['area']} triggered by {ev.get('term')!r} in "
                               f"{ev.get('source')}")
        else:
            entry.update(status="not-triggered", area=spec["area"],
                         basis=f"area {spec['area']} is not affected by this task; the skill "
                               f"stays resolved and is read only if the work reveals the domain")
        out.append(entry)
    return out


# --------------------------------------------------------------------- build / io


def parallel_groups(deps: dict, order: list) -> list:
    """Topological levels over hard edges: phases in one level share no dependency path."""
    level = {}
    for phase in order:
        hard = [d["state_id"] for d in deps.get(phase, []) if d.get("kind") == "hard"]
        level[phase] = 1 + max((level.get(h, 0) for h in hard), default=0)
    groups: dict = {}
    for phase in order:
        groups.setdefault(level[phase], []).append(phase)
    return [groups[k] for k in sorted(groups)]


def build(*, store, run_dir: Path, rows: list, deps: dict, supplied: list, claude_root: Path,
          declared_areas: list | None = None, skill_records: list | None = None,
          now: str | None = None) -> dict:
    """Rebuild the task context from persisted run state. Pure over its inputs."""
    order = [r["phase"] for r in rows]
    data: dict = {
        "schema": SCHEMA,
        "task_id": store.data["run_id"],
        "command": store.data["command_id"],
        "workflow": f"{store.data['workflow_id']} v{store.data['workflow_version']}",
        "run_status": store.data["run_status"],
        "updated_at": now,
        "objective": objective_from_input(supplied[0]["text"]) if supplied else None,
        "inputs": [{"type": s["type"], "reference": s["reference"], "digest": s.get("digest")}
                   for s in supplied],
        "relevant_agents": [],
        "completed_phases": [],
        "gates": [],
        "artifacts": {},
        "parallel_groups": parallel_groups(deps, order),
    }
    sources = [(f"input:{s['type']}", s.get("text", "")) for s in supplied]
    for row in rows:
        phase = row["phase"]
        item = store.item(phase) if store.has_item(phase) else None
        data["relevant_agents"].append({
            "phase": phase, "agent": row.get("owner agent"),
            "status": item["status"] if item else "unplanned",
            "required_skills": [s.strip() for s in (row.get("required skills") or "").split(",")
                                if s.strip()],
        })
        if not item or item["status"] != "completed":
            continue
        completion = item.get("completion") or {}
        art_rel = completion.get("artifact_path") or item.get("artifact_path")
        val = completion.get("validation") or {}
        data["completed_phases"].append({
            "phase": phase, "agent": item.get("owner_agent_id"),
            "artifact": art_rel, "digest": completion.get("artifact_digest"),
            "validation": (f"{val.get('result')} {val.get('checksPassed')}/"
                           f"{val.get('checksRun')}") if val else None,
            "attempts": item.get("attempt"),
        })
        if art_rel:
            data["artifacts"][phase] = art_rel
            p = claude_root / art_rel
            if p.exists():
                text = p.read_text(encoding="utf-8")
                sources.append((f"artifact:{phase}", text))
                for reg, values in extract_facts(Path(art_rel).name, text).items():
                    if isinstance(values, dict):
                        for sub, vals in values.items():
                            _set(data, f"{reg}.{sub}", vals)
                    else:
                        _set(data, reg, values)
    for g in (store.data.get("gates") or {}).values():
        data["gates"].append({"gate": g["gate"], "closes": g["closes_state"],
                              "decision": g.get("decision"), "owner_role": g.get("owner_role"),
                              "decided_by": g.get("decided_by")})
    areas = derive_affected_areas(sources, declared_areas)
    data["affected_areas"] = areas
    data["relevant_skills"] = {
        row["phase"]: {e["skillCode"]: e["status"] for e in skill_dispatch(
            row["phase"],
            [s.strip() for s in (row.get("required skills") or "").split(",") if s.strip()],
            areas, skill_records or [])}
        for row in rows
    }
    rc = data.setdefault("repository_context", {})
    rc.setdefault("relevant_modules", [])
    rc["relevant_files"] = list(data.get("changed_files") or [])
    rc["rescan_required_when"] = [
        "the scope or the accepted design changes",
        "a dependency this context does not name is discovered",
        "a named module or file no longer exists at the recorded path",
    ]
    return data


def write(run_dir: Path, data: dict) -> Path:
    path = run_dir / FILENAME
    path.write_text(yaml.safe_dump(data, sort_keys=False, allow_unicode=True, width=120),
                    encoding="utf-8")
    return path


def load(run_dir: Path) -> dict | None:
    path = run_dir / FILENAME
    if not path.exists():
        return None
    return yaml.safe_load(path.read_text(encoding="utf-8")) or {}


def sections_of_interest(artifact_name: str) -> dict | None:
    return UPSTREAM_SECTIONS.get(artifact_name)
