#!/usr/bin/env python3
"""Declarative artifact contract engine.

`plan_validator.py` and `design_validator.py` are hand-written, because the Planner and
Architect contracts declare their own numbered check sets (`Q1.1`, `A1.1`, ...) with their
own severities, and a validator for those must reproduce that numbering exactly. The four
artifact types added by this slice have no such check set: their owning agents ship prose
contracts (`agents/<agent-id>.md`) rather than a `quality.md` module, so the authority for
what conformance means is

  1. the canonical template, which fixes section titles, order, and field labels,
  2. the owning agent's declared Outputs, Decision Rules, and Constraints,
  3. the framework registries and vocabularies, for anything the artifact names.

Reproducing four near-identical validators over that authority would duplicate the same
structural, boundary, field, and identifier logic four times, and every future artifact type
would duplicate it again. So the rules are declared as data here and executed by one engine.

The engine knows nothing about bugs, investigations, releases, or reviews. It knows about
metadata blocks, sections, field bullets, tables, vocabularies, identifier schemes, and
determinism. Each per-artifact module declares an `ArtifactContract` and may append its own
semantic checks, exactly as the two hand-written validators do.

Check identifiers are stable and grouped by concern, so a failure names the same check
across every artifact type governed by this engine:

    C1  metadata block          C5  vocabularies
    C2  section structure       C6  identifier schemes
    C3  boundary constraints    C7  determinism
    C4  declared fields         N   declared not-machine-checkable obligations

Severities are the three the framework's agent quality contracts use: `Blocking`,
`Correctable`, `Advisory`.
"""

from __future__ import annotations

import re
from dataclasses import dataclass, field as dc_field
from pathlib import Path

import yaml

from artifact_lib import (
    VENDOR_TOKENS, Check, Report, canonical_agent_id, parse_all_tables, split_sections,
    strip_comments, strip_md,
)

# --------------------------------------------------------------------------- declarations


@dataclass(frozen=True)
class Field:
    """One declared field bullet inside a section, rendered `- Label: value`."""

    label: str
    required: bool = True
    severity: str = "Blocking"
    enum: tuple = ()            # permitted values, matched case-insensitively
    pattern: str | None = None  # regex the value must match in full
    min_words: int = 0          # substance floor, for fields a template invites one word into


@dataclass(frozen=True)
class Table:
    """A table a section must carry."""

    columns: tuple                 # headers that must all be present, matched case-insensitively
    min_rows: int = 1
    optional_columns: tuple = ()   # columns permitted to be empty
    id_column: str | None = None   # column carrying the row identifier


@dataclass(frozen=True)
class Section:
    """One mandatory level-2 section."""

    title: str
    fields: tuple = ()
    table: Table | None = None
    min_items: int = 0          # minimum top-level list items, when the section is a list
    numbered: bool = False      # min_items counts numbered rather than bulleted items
    allow_none: bool = True     # "None identified." satisfies the section content floor
    checkable: bool = True      # False records the section as present-only, no content rule


@dataclass
class ArtifactContract:
    artifact: str                       # canonical file name, the VALIDATORS key
    producers: tuple                    # agent identifiers permitted in producedBy
    metadata_key: str                   # root key of the leading yaml block
    metadata_fields: tuple              # keys that must be present and populated
    statuses: tuple                     # permitted status values
    sections: tuple                     # Section, in contract order
    appendices: tuple = ()              # permitted optional level-2 sections
    id_prefixes: dict = dc_field(default_factory=dict)  # prefix -> defining section title
    template_ref: str = ""
    contract_refs: tuple = ()
    schema_version: str = "1.0.0"
    obligations: tuple = ()             # (id, ref, description) recorded not-machine-checkable
    max_fenced_blocks: int = 1          # the metadata block only, unless the artifact needs more
    allow_diff_markers: bool = False    # review packages quote diffs; plans and designs do not


NONE_MARKERS = ("none identified", "none", "not applicable", "n/a")

# Vocabularies the framework already declares elsewhere. Named here once so a per-artifact
# contract references the source rather than restating a list.
SEVERITIES = ("critical", "high", "medium", "low")           # rule-engine.md, `severity`
CONFIDENCE = ("high", "medium", "low")                       # agents/*/quality.md confidence scale
STATUSES_COMPLETE = ("complete", "provisional", "blocked")    # templates/technical-design.md


# --------------------------------------------------------------------------- parsing helpers


def parse_field_bullets(body: str) -> dict:
    """Read `- Label: value` bullets, including values carried on nested bullets.

    A field whose value is empty on its own line but continues as nested bullets is
    populated; a field with neither is not. The templates invite both shapes, so the engine
    accepts both rather than forcing authors into one.
    """
    text = strip_comments(body)
    lines = text.split("\n")
    out = {}
    for idx, ln in enumerate(lines):
        m = re.match(r"^(\s*)[-*]\s+([^:]{1,80}?)\s*:\s*(.*)$", ln)
        if not m:
            continue
        indent, label, inline = len(m.group(1)), strip_md(m.group(2)), m.group(3).strip()
        if indent > 0:
            continue  # nested under a previous field; it is part of that field's value
        nested = []
        for nxt in lines[idx + 1:]:
            if not nxt.strip():
                continue
            n_ind = len(nxt) - len(nxt.lstrip())
            if n_ind > indent and re.match(r"^\s*[-*0-9]", nxt):
                nested.append(nxt.strip().lstrip("-*").strip())
                continue
            break
        out[label.lower()] = (inline + " " + " ".join(nested)).strip()
    return out


def defined_ids(body: str, prefix: str) -> list:
    """Identifiers declared in definition position: a heading, a list item, or column one."""
    text = strip_comments(body)
    found = []
    for ln in text.split("\n"):
        s = ln.strip()
        m = re.match(r"^(?:#{2,4}\s+|[-*]\s+|\|\s*)`?(" + prefix + r"-\d{3})`?\b", s)
        if m:
            found.append(m.group(1))
    return found


def referenced_ids(text: str, prefix: str) -> list:
    return re.findall(r"\b" + prefix + r"-\d{3}\b", strip_comments(text))


def find_table(body: str, columns: tuple):
    """The first table in `body` whose headers cover every declared column."""
    want = [c.lower() for c in columns]
    for headers, rows in parse_all_tables(body, min(2, len(columns))):
        low = [strip_md(h).lower() for h in headers]
        if all(any(w == h or w in h for h in low) for w in want):
            return low, rows
    return None, []


def column_index(headers: list, name: str) -> int:
    name = name.lower()
    for i, h in enumerate(headers):
        if h == name or name in h:
            return i
    return -1


def is_none_marker(body: str) -> bool:
    stripped = strip_comments(body).strip().lower().rstrip(".")
    return stripped in NONE_MARKERS


# --------------------------------------------------------------------------- the engine


def run_contract(contract: ArtifactContract, artifact_path: Path,
                 envelope: dict | None = None) -> Report:
    """Execute one declared contract against one rendered artifact."""
    text = artifact_path.read_text(encoding="utf-8")
    rep = Report(artifact=str(artifact_path))
    rep.schema_version = contract.schema_version

    def add(cid, ref, severity, description, result, detail=""):
        rep.checks.append(Check(cid, ref, severity, description, result, detail))

    # ------------------------------------------------------------------ C1 metadata
    meta = {}
    m = re.search(r"```yaml\s*\n(.*?)\n```", text, re.S)
    if not m:
        add("C1.1", contract.template_ref, "Blocking",
            "Leading fenced yaml metadata block present and parseable", "fail",
            "no fenced yaml block found; " + contract.template_ref + " declares one under "
            "key " + repr(contract.metadata_key))
    else:
        try:
            meta = (yaml.safe_load(m.group(1)) or {}).get(contract.metadata_key, {}) or {}
            missing = [f for f in contract.metadata_fields
                       if f not in meta or meta[f] in (None, "", [], {})]
            add("C1.1", contract.template_ref, "Blocking",
                "Metadata block present with every declared field populated",
                "fail" if missing else "pass",
                f"missing or empty: {missing}" if missing
                else f"{len(contract.metadata_fields)} of "
                     f"{len(contract.metadata_fields)} fields populated")
        except Exception as exc:  # noqa: BLE001
            add("C1.1", contract.template_ref, "Blocking",
                "Leading fenced yaml metadata block present and parseable", "fail", str(exc))

    produced = str(meta.get("producedBy") or "")
    add("C1.2", contract.template_ref, "Blocking",
        f"producedBy is an agent contracted to emit {contract.artifact}",
        "pass" if canonical_agent_id(produced) in contract.producers else "fail",
        f"producedBy={produced!r}; contracted producers: {list(contract.producers)}")

    add("C1.3", contract.template_ref, "Blocking",
        f"schemaVersion is {contract.schema_version} and agentVersion is semantic",
        "pass" if str(meta.get("schemaVersion")) == contract.schema_version
        and str(meta.get("agentVersion") or "").count(".") == 2 else "fail",
        f"schemaVersion={meta.get('schemaVersion')} agentVersion={meta.get('agentVersion')}")

    add("C1.4", contract.template_ref, "Blocking",
        f"status is one of {', '.join(contract.statuses)}",
        "pass" if str(meta.get("status") or "").strip().lower() in contract.statuses else "fail",
        f"status={meta.get('status')!r}")

    # ------------------------------------------------------------------ C2 structure
    sections = split_sections(text)
    titles = [t for t, _ in sections]
    body_of = {}
    for t, b in sections:
        body_of.setdefault(t, b)

    mandatory = [s.title for s in contract.sections]
    present = [t for t in titles if t in mandatory]
    dupes = sorted({t for t in present if present.count(t) > 1})
    missing_sec = [t for t in mandatory if t not in titles]
    add("C2.1", contract.template_ref, "Blocking",
        f"All {len(mandatory)} mandatory sections present exactly once",
        "fail" if (missing_sec or dupes) else "pass",
        f"missing={missing_sec} duplicated={dupes}" if (missing_sec or dupes)
        else f"{len(mandatory)} of {len(mandatory)} present, once each")

    add("C2.2", contract.template_ref, "Blocking",
        "Mandatory sections appear in contract order",
        "pass" if present == [t for t in mandatory if t in titles] else "fail",
        f"observed order: {present}")

    extra = [t for t in titles if t not in mandatory and t not in contract.appendices]
    add("C2.3", contract.template_ref, "Correctable",
        "No unpermitted level-2 section introduced",
        "pass" if not extra else "fail",
        f"unpermitted: {extra}; permitted appendices: {list(contract.appendices)}"
        if extra else "none")

    empty = [s.title for s in contract.sections
             if s.title in body_of and not strip_comments(body_of[s.title]).strip()]
    add("C2.4", contract.template_ref, "Blocking", "No mandatory section is empty",
        "pass" if not empty else "fail",
        f"empty: {empty}" if empty else "all non-empty")

    leftover = [s.title for s in contract.sections
                if s.title in body_of and re.search(r"<!--.*?-->", body_of[s.title], re.S)]
    add("C2.5", contract.template_ref, "Advisory",
        "No template authoring guidance remains in the emitted artifact",
        "pass" if not leftover else "fail",
        f"guidance comments remain in: {leftover}" if leftover else "none")

    # ------------------------------------------------------------------ C3 boundary
    fences = re.findall(r"^\s*(?:```|~~~~)", text, re.M)
    permitted_fences = contract.max_fenced_blocks * 2
    add("C3.1", "config/execution-engine.md#agent-adapter", "Blocking",
        f"At most {contract.max_fenced_blocks} fenced block(s), as the contract permits",
        "pass" if len(fences) <= permitted_fences else "fail",
        f"{len(fences) // 2} fenced block(s) found; {contract.max_fenced_blocks} permitted")

    # `.claude/` is the framework's own root, so a repository path that contains it is
    # context rather than a vendor selection, and the prefix is removed before the scan.
    scannable = re.sub(r"\.claude(?=[/\\])", "", text, flags=re.I).lower()
    hits = [v for v in VENDOR_TOKENS if v in scannable]
    add("C3.2", "domain-model/agent-specification.md", "Blocking",
        "No model, vendor, or provider named", "pass" if not hits else "fail",
        f"found: {hits}" if hits else "none (repository path prefixes excluded)")

    if not contract.allow_diff_markers:
        diffish = re.findall(r"^\s*(?:diff --git|\+\+\+ |--- [ab]/|@@ )", text, re.M)
        add("C3.3", "domain-model/agent-specification.md", "Blocking",
            "No patch instruction, diff, or file-modification directive",
            "pass" if not diffish else "fail", f"{len(diffish)} diff marker(s)")

    # ------------------------------------------------------------------ C4 declared fields
    missing_fields, empty_fields, thin_fields = {}, {}, {}
    bad_enum, bad_pattern = {}, {}
    checked = 0
    for sec in contract.sections:
        if not sec.fields or sec.title not in body_of:
            continue
        body = body_of[sec.title]
        if sec.allow_none and is_none_marker(body):
            continue
        found = parse_field_bullets(body)
        for f in sec.fields:
            key = f.label.lower()
            checked += 1
            if key not in found:
                if f.required:
                    missing_fields.setdefault(sec.title, []).append(f.label)
                continue
            val = found[key]
            if not val:
                if f.required:
                    empty_fields.setdefault(sec.title, []).append(f.label)
                continue
            if f.min_words and len(val.split()) < f.min_words:
                thin_fields.setdefault(sec.title, []).append(
                    f"{f.label} ({len(val.split())}<{f.min_words} words)")
            if f.enum and strip_md(val).lower().rstrip(".") not in f.enum:
                bad_enum.setdefault(sec.title, []).append(f"{f.label}={val!r}")
            if f.pattern and not re.fullmatch(f.pattern, strip_md(val), re.I):
                bad_pattern.setdefault(sec.title, []).append(f"{f.label}={val!r}")

    add("C4.1", contract.template_ref, "Blocking",
        "Every declared field label is present in its section",
        "pass" if not missing_fields else "fail",
        f"absent: {missing_fields}" if missing_fields
        else f"{checked} declared field(s) present")
    add("C4.2", contract.template_ref, "Blocking",
        "No declared field is left unanswered",
        "pass" if not empty_fields else "fail",
        f"empty: {empty_fields}" if empty_fields else "every declared field carries a value")
    add("C4.3", contract.template_ref, "Correctable",
        "Fields with a declared substance floor carry more than a token answer",
        "pass" if not thin_fields else "fail",
        f"below floor: {thin_fields}" if thin_fields else "all fields meet their floor")
    add("C5.1", "rule-engine.md", "Blocking",
        "Fields bound to a vocabulary use a permitted value",
        "pass" if not bad_enum else "fail",
        f"outside vocabulary: {bad_enum}" if bad_enum else "all bounded fields valid")
    add("C5.2", contract.template_ref, "Correctable", "Fields bound to a format match it",
        "pass" if not bad_pattern else "fail",
        f"malformed: {bad_pattern}" if bad_pattern else "all formatted fields valid")

    # ------------------------------------------------------------------ content floors
    thin_lists, bad_tables, incomplete_rows = {}, {}, {}
    row_counts = {}
    for sec in contract.sections:
        if sec.title not in body_of or not sec.checkable:
            continue
        body = body_of[sec.title]
        if sec.allow_none and is_none_marker(body):
            continue
        if sec.min_items:
            pattern = r"^\s*\d+\.\s+\S" if sec.numbered else r"^\s*[-*]\s+\S"
            n = len(re.findall(pattern, strip_comments(body), re.M))
            if n < sec.min_items:
                thin_lists[sec.title] = f"{n} item(s), {sec.min_items} required"
        if sec.table:
            headers, rows = find_table(body, sec.table.columns)
            if headers is None:
                bad_tables[sec.title] = f"no table carrying columns {list(sec.table.columns)}"
                continue
            row_counts[sec.title] = len(rows)
            if len(rows) < sec.table.min_rows:
                bad_tables[sec.title] = f"{len(rows)} row(s), {sec.table.min_rows} required"
            optional = {c.lower() for c in sec.table.optional_columns}
            required_idx = [(c, column_index(headers, c)) for c in sec.table.columns
                            if c.lower() not in optional]
            for r in rows:
                gaps = [c for c, i in required_idx if i < 0 or i >= len(r) or not r[i].strip()]
                if gaps:
                    label = strip_md(r[0]) if r else "?"
                    incomplete_rows.setdefault(sec.title, []).append(f"{label}: {gaps}")

    add("C4.4", contract.template_ref, "Blocking",
        "Sections with a declared minimum carry at least that many items",
        "pass" if not thin_lists else "fail",
        f"below minimum: {thin_lists}" if thin_lists
        else "all list sections meet their minimum")
    add("C4.5", contract.template_ref, "Blocking",
        "Sections with a declared table carry it, with the declared columns",
        "pass" if not bad_tables else "fail",
        f"malformed: {bad_tables}" if bad_tables else f"row counts: {row_counts}")
    add("C4.6", contract.template_ref, "Blocking",
        "No table row leaves a required cell empty",
        "pass" if not incomplete_rows else "fail",
        f"incomplete: {incomplete_rows}" if incomplete_rows else "all rows complete")

    # ------------------------------------------------------------------ C6 identifiers
    malformed = []
    if contract.id_prefixes:
        alternation = "|".join(contract.id_prefixes)
        malformed = sorted({f"{p}-{n}" for p, n in
                            re.findall(r"\b(" + alternation + r")-(\d+)\b", text)
                            if len(n) != 3})
    add("C6.1", contract.template_ref, "Correctable",
        "Identifiers use the zero-padded three-digit scheme",
        "pass" if not malformed else "fail",
        f"malformed: {malformed}" if malformed else "all well-formed")

    undefined, noncontig, duplicated, counts = {}, {}, {}, {}
    for prefix, sec_title in contract.id_prefixes.items():
        body = body_of.get(sec_title, "")
        defined = defined_ids(body, prefix)
        counts[prefix] = len(set(defined))
        dupes_id = sorted({x for x in defined if defined.count(x) > 1})
        if dupes_id:
            duplicated[prefix] = dupes_id
        refs = set(referenced_ids(text, prefix))
        gap = sorted(refs - set(defined))
        if gap:
            undefined[prefix] = gap
        want = [f"{prefix}-{i:03d}" for i in range(1, len(set(defined)) + 1)]
        if sorted(set(defined)) != want:
            noncontig[prefix] = sorted(set(defined))

    add("C6.2", contract.template_ref, "Blocking",
        "Every referenced identifier is defined in its declaring section",
        "pass" if not undefined else "fail",
        f"undefined: {undefined}" if undefined
        else (f"defined: {counts}" if counts else "no identifier scheme declared"))
    add("C6.3", contract.template_ref, "Blocking", "No identifier is defined twice",
        "pass" if not duplicated else "fail",
        f"duplicated: {duplicated}" if duplicated else "identifiers unique")
    add("C6.4", contract.template_ref, "Correctable",
        "Identifiers ascend from 001 without gaps",
        "pass" if not noncontig else "fail",
        f"non-contiguous: {noncontig}" if noncontig else f"contiguous: {counts}")

    # ------------------------------------------------------------------ C7 determinism
    if envelope:
        exp_in = envelope.get("context_slice", {}).get("input_digest")
        exp_ctx = envelope.get("context_slice", {}).get("context_digest")
        ok = meta.get("inputDigest") == exp_in and meta.get("contextDigest") == exp_ctx
        add("C7.1", "config/execution-engine.md#context-loader", "Blocking",
            "Metadata digests match the frozen snapshot recorded in the invocation envelope",
            "pass" if ok else "fail",
            "digests match the envelope" if ok
            else f"artifact=({meta.get('inputDigest')}, {meta.get('contextDigest')}) "
                 f"envelope=({exp_in}, {exp_ctx})")
    else:
        add("C7.1", "config/execution-engine.md#context-loader", "Advisory",
            "Digest cross-check against the invocation envelope", "not-machine-checkable",
            "no envelope supplied")

    for oid, ref, desc in contract.obligations:
        add(oid, ref, "Advisory", desc, "not-machine-checkable",
            "agent self-verification obligation; not decidable by artifact inspection")

    rep.counts = {
        "sections": len(titles),
        "status": meta.get("status"),
        "identifiers": counts,
        "tableRows": row_counts,
    }
    # Carried for the per-artifact semantic checks, which read the same parse rather than
    # re-parsing the file.
    rep.meta = meta
    rep.body_of = body_of
    return rep


# --------------------------------------------------------------------------- module runner


def cli(contract: ArtifactContract, extra=None, argv=None) -> int:
    """Shared command-line entry point for every contract-driven validator."""
    import argparse
    import json

    ap = argparse.ArgumentParser(
        description=f"Validate {contract.artifact} against its framework contract "
                    f"({', '.join(contract.contract_refs) or contract.template_ref})")
    ap.add_argument("artifact")
    ap.add_argument("--envelope",
                    help="invocation envelope JSON, enables the digest cross-check")
    ap.add_argument("--json-out")
    a = ap.parse_args(argv)

    env = json.loads(Path(a.envelope).read_text(encoding="utf-8")) if a.envelope else None
    rep = run_contract(contract, Path(a.artifact), env)
    if extra:
        extra(rep, contract)
    d = rep.to_dict()
    if a.json_out:
        Path(a.json_out).write_text(json.dumps(d, indent=2), encoding="utf-8")

    print(f"Validation Engine: {a.artifact}  ({contract.artifact})")
    print(f"  result      : {d['result'].upper()}")
    print(f"  checks      : {d['checksPassed']}/{d['checksRun']} passed "
          f"({d['notMachineCheckable']} declared not-machine-checkable)")
    print(f"  blocking    : {d['blockingFailures']}    correctable: {d['correctableFailures']}")
    print(f"  counts      : {d['counts']}")
    for c in rep.checks:
        if c.result == "fail":
            print(f"  FAIL [{c.severity}] {c.id} ({c.quality_ref}): "
                  f"{c.description} -- {c.detail}")
    return 0 if rep.passed else 1
