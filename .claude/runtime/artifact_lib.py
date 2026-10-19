#!/usr/bin/env python3
"""Shared Validation Engine primitives.

Everything here is artifact-agnostic: the check and report records that every validator
emits, the markdown parsing used to read a rendered artifact back into structure, and the
framework fact set that `A14` / `Q9` style alignment checks resolve against.

Artifact-specific rules live in the per-artifact validator (`plan_validator.py`,
`design_validator.py`). Nothing in this module knows what a plan or a design is.
"""

from __future__ import annotations

import re
from dataclasses import dataclass, field, asdict
from pathlib import Path

import yaml

CLAUDE = Path(__file__).resolve().parent.parent

# Model and vendor tokens. Every registered agent contract forbids naming one, so the scan
# is shared. `.claude` path prefixes are stripped by the caller before scanning, because a
# repository path is context, not a vendor selection.
VENDOR_TOKENS = [
    "claude", "anthropic", "openai", "gpt-", "chatgpt", "gemini",
    "llama", "mistral", "copilot", "bedrock", "vertex ai", "azure openai",
]


@dataclass
class Check:
    id: str
    quality_ref: str
    severity: str          # Blocking | Correctable | Advisory
    description: str
    result: str            # pass | fail | not-machine-checkable
    detail: str = ""


@dataclass
class Report:
    artifact: str
    schema_version: str = "1.0.0"
    checks: list = field(default_factory=list)
    counts: dict = field(default_factory=dict)

    @property
    def blocking_failures(self):
        return [c for c in self.checks if c.result == "fail" and c.severity == "Blocking"]

    @property
    def correctable_failures(self):
        return [c for c in self.checks if c.result == "fail" and c.severity == "Correctable"]

    @property
    def advisory_failures(self):
        return [c for c in self.checks if c.result == "fail" and c.severity == "Advisory"]

    @property
    def passed(self):
        return not self.blocking_failures and not self.correctable_failures

    def to_dict(self):
        return {
            "artifact": self.artifact,
            "validatorSchemaVersion": self.schema_version,
            "result": "pass" if self.passed else "fail",
            "blockingFailures": len(self.blocking_failures),
            "correctableFailures": len(self.correctable_failures),
            "advisoryFailures": len(self.advisory_failures),
            "checksRun": len([c for c in self.checks if c.result != "not-machine-checkable"]),
            "checksPassed": len([c for c in self.checks if c.result == "pass"]),
            "notMachineCheckable": len([c for c in self.checks if c.result == "not-machine-checkable"]),
            "counts": self.counts,
            "checks": [asdict(c) for c in self.checks],
        }


# --------------------------------------------------------------------------- parsing


def split_sections(text: str):
    """Return ordered [(title, body)] for level-2 headings, ignoring fenced blocks."""
    out, fence, cur, buf = [], False, None, []
    for ln in text.split("\n"):
        stripped = ln.lstrip()
        if stripped.startswith("```") or stripped.startswith("~~~~"):
            fence = not fence
        if not fence and ln.startswith("## "):
            if cur is not None:
                out.append((cur, "\n".join(buf)))
            cur, buf = ln[3:].strip(), []
        else:
            buf.append(ln)
    if cur is not None:
        out.append((cur, "\n".join(buf)))
    return out


def parse_table(body: str, min_cols: int = 2):
    """Parse the first markdown table in `body` into (headers, rows)."""
    rows, headers, seen_sep = [], None, False
    for ln in body.split("\n"):
        s = ln.strip()
        if not s.startswith("|"):
            if headers is not None and seen_sep:
                break
            continue
        cells = [c.strip() for c in s.strip("|").split("|")]
        if len(cells) < min_cols:
            continue
        if headers is None:
            headers = cells
            continue
        if set("".join(cells)) <= set("-: "):
            seen_sep = True
            continue
        rows.append(cells)
    return headers or [], rows


def parse_all_tables(body: str, min_cols: int = 2):
    """Parse every markdown table in `body`."""
    tables, block = [], []
    for ln in body.split("\n"):
        if ln.strip().startswith("|"):
            block.append(ln)
        else:
            if block:
                tables.append(parse_table("\n".join(block), min_cols))
                block = []
    if block:
        tables.append(parse_table("\n".join(block), min_cols))
    return [t for t in tables if t[1]]


def strip_md(v: str) -> str:
    return v.replace("`", "").replace("*", "").strip()


def ids_of(prefix: str, text: str):
    return re.findall(rf"\b{prefix}-\d{{3}}\b", text or "")


def strip_comments(body: str) -> str:
    """Remove HTML comments, which templates use for authoring guidance."""
    return re.sub(r"<!--.*?-->", "", body or "", flags=re.S)


# --------------------------------------------------------------------------- agent identity

# Agent identifiers once carried an `omn-` prefix. Run evidence, proposals, and dated reports
# written before the prefix was dropped are never edited in place, so they still name agents
# such as `omn-qa`; this prefix maps such a name to the agent it denotes today.
LEGACY_AGENT_PREFIX = "omn-"


def canonical_agent_id(agent_id: str) -> str:
    """The current identifier for an agent name, mapping a legacy prefixed one onto it."""
    agent_id = str(agent_id or "")
    if agent_id.startswith(LEGACY_AGENT_PREFIX):
        return agent_id[len(LEGACY_AGENT_PREFIX):]
    return agent_id


# --------------------------------------------------------------------------- framework facts


def framework_facts():
    def y(p):
        return yaml.safe_load((CLAUDE / p).read_text(encoding="utf-8"))

    agents = y("registry/agents.yaml")
    workflows = y("registry/workflows.yaml")
    templates = y("registry/templates.yaml")

    registered_agents = {r["identifier"] for r in (agents.get("records") or [])}

    contract_agents = set()
    skip = {
        "README.md", "agent-catalog.md", "capability-matrix.md",
        # Registers, not agent contracts: `retired-roles.md` resolves retired role
        # identifiers, `superseded-contracts.md` resolves the shared contract modules the
        # runtime module-set architecture superseded (`agents/superseded-contracts.md`).
        "retired-roles.md", "superseded-contracts.md",
    }
    for p in (CLAUDE / "agents").iterdir():
        if p.is_file() and p.suffix == ".md" and p.name not in skip:
            contract_agents.add(p.stem.replace(".agent", ""))
        elif p.is_dir() and (p / "manifest.yaml").exists():
            contract_agents.add(p.name)

    gate_text = (CLAUDE / "workflows/workflow-gate-matrix.md").read_text(encoding="utf-8")
    gates = set()
    for row in parse_table(gate_text, 3)[1]:
        if len(row) >= 2:
            gates.add(strip_md(row[1]))

    # Capability identifiers come from the "Capability Identifiers" table only, which the
    # matrix declares authoritative. Scanning the whole file for backticked tokens would
    # admit agent names and check ids; requiring a hyphen would drop single-word
    # identifiers such as `documentation`.
    cap_text = (CLAUDE / "agents/capability-matrix.md").read_text(encoding="utf-8")
    cap_section = re.search(r"^## Capability Identifiers\s*$(.*?)^## ", cap_text, re.S | re.M)
    capabilities = set()
    if cap_section:
        for row in parse_table(cap_section.group(1), 2)[1]:
            if len(row) >= 2:
                capabilities |= set(re.findall(r"`([a-z0-9][a-z0-9-]*)`", row[1]))

    skill_text = (CLAUDE / "skills/agent-skill-matrix.md").read_text(encoding="utf-8")
    skills = set(re.findall(r"\bS\d{2}\b", skill_text))

    return {
        "registered_agents": registered_agents,
        # Legacy prefixed names resolve to the active agent they denote, so committed evidence
        # that names one is not read as naming an unknown agent.
        "resolvable_agents": (registered_agents | contract_agents
                              | {LEGACY_AGENT_PREFIX + a for a in contract_agents}),
        "workflows": {r["identifier"] for r in (workflows.get("records") or [])},
        "gates": gates,
        "capabilities": capabilities,
        "skills": skills,
        "templates": {r["specificationPath"] for r in (templates.get("records") or [])},
    }
