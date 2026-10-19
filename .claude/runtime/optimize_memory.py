#!/usr/bin/env python3
"""Token optimizer for the framework's durable knowledge surfaces.

Memory, context, and instruction files are read into every run's context slice, so their token
cost is paid on every dispatch rather than once. This module rewrites them denser without
rewriting what they say.

The honest claim, stated up front, is the one this module is built around: a language model
cannot *prove* it preserved meaning, and no prompt makes it able to. So nothing here trusts the
model's own assurance. Compression is proposed by the model and then judged mechanically against
invariants extracted from the original — headings, fenced code, frontmatter, link targets, table
shape, inline identifiers, declared rule identifiers. A candidate that drops one is rejected and
the original file is left exactly as it was. "Lossless" in this module means *these invariants
held*, which is a checkable claim, and the report says which ones were checked.

Three consequences follow, and they are the module's operating rules:

- **The default is a dry run.** `run` proposes and reports; `--apply` is what overwrites.
- **Nothing is overwritten un-backed-up.** Every applied pass writes the pre-image, digests, and
  a manifest under `runs/optimize-memory/<stamp>/`, and `restore` puts them back.
- **Generated surfaces are never touched.** Run evidence, reports, proposals, changelogs, and
  anything the framework writes for itself are denied by path, not by convention -- a rewritten
  record of what happened would corrupt the evidence the governance layer resolves against.

    python .claude/runtime/optimize_memory.py scan
    python .claude/runtime/optimize_memory.py run
    python .claude/runtime/optimize_memory.py run --apply
    python .claude/runtime/optimize_memory.py restore --manifest runs/optimize-memory/<stamp>/manifest.json

Requires the `anthropic` package and resolvable API credentials for `run`; `scan` and `restore`
are offline and need neither.
"""

from __future__ import annotations

import argparse
import difflib
import hashlib
import json
import os
import re
import sys
from dataclasses import dataclass, field, asdict
from datetime import datetime, timezone
from pathlib import Path

CLAUDE = Path(__file__).resolve().parent.parent
REPO = CLAUDE.parent
RUNS = CLAUDE / "runs" / "optimize-memory"

MODEL = os.environ.get("OPTIMIZE_MEMORY_MODEL", "claude-opus-5")
MAX_TOKENS = 64000

# --------------------------------------------------------------------------- targets

# Framework-relative roots scanned when no --root is supplied. Each is a durable knowledge
# surface that a run loads rather than an artifact a run writes.
DEFAULT_ROOTS = ("memory", "context")

# Instruction files: single documents rather than trees. Repository-root governance documents
# are included because agent contracts cite them, so their token cost is paid the same way.
DEFAULT_FILES = (
    CLAUDE / "CLAUDE.md",
    REPO / "decision-matrix.md",
    REPO / "quality-gates.md",
    REPO / "rule-engine.md",
    REPO / "working-memory.md",
)

# Path segments that are never rewritten, whatever the caller asks for. These hold what the
# framework wrote about its own execution; compressing evidence would falsify it.
DENIED_SEGMENTS = frozenset({
    "runs", "reports", "proposals", "__pycache__", ".git", "node_modules", "build", "dist",
})

# Filenames that are generated or machine-parsed with a fixed line shape. A denser rewrite of
# either is a defect, not a saving.
DENIED_NAMES = frozenset({
    "changelog.md", "change-log.md", "history.md", "memory.md",
})

# Frontmatter or body markers that declare a file machine-generated.
GENERATED_MARKERS = (
    re.compile(r"^\s*generated\s*:\s*true\s*$", re.M | re.I),
    re.compile(r"<!--\s*(?:auto)?generated[^>]*-->", re.I),
    re.compile(r"^\s*DO NOT EDIT\b", re.M | re.I),
)

MIN_BYTES = 400          # below this there is nothing worth a request
FLOOR_RATIO = 0.25       # a candidate under this share of the original reads as deletion
MIN_GAIN_RATIO = 0.03    # under a 3% saving, keep the original and spend nothing


# --------------------------------------------------------------------------- system prompt

SYSTEM_PROMPT = """\
You compress technical Markdown to its minimum token cost without changing what it means.

The document you receive is a durable instruction, memory, or context file read by automated \
agents on every task. Every token in it is paid repeatedly. Your single job is to make it \
cheaper to read while leaving every instruction, constraint, identifier, and decision it \
carries exactly as binding as before.

REMOVE:
- Filler, throat-clearing, and restatements of what an adjacent sentence already said.
- Motivational or self-congratulatory prose that carries no rule.
- Redundant transitions, hedges, and meta-commentary about the document itself.
- Repeated qualifiers where one statement of the qualifier governs the section.

REWRITE:
- Long sentences into punchy declarative fragments.
- Passive constructions into active ones.
- Multi-sentence explanations into one sentence, or a list row, when nothing is lost.
- Prose enumerations into terse lists.

NEVER CHANGE, DROP, OR REORDER:
- YAML frontmatter. Reproduce the block between the leading `---` fences byte for byte.
- Any fenced code block. Reproduce every fence and its contents byte for byte.
- Any heading. Same text, same level, same order. Add none, remove none, merge none.
- Any table. Same header cells, same number of body rows, same row order.
- Any identifier: rule and requirement codes (SR-1, C-3, E-7, FR-05, S10, Q1.1, T-004, \
GD-001), file paths, command lines, flags, URLs, link targets, inline `code` spans, \
acronyms, proper nouns, version numbers, dates, and numeric thresholds.
- Any negation, obligation, permission, or exception. "must", "must not", "never", "only", \
"unless", "except", and their scope survive verbatim in force even when reworded.
- Any conditional. If a rule fires under a condition, the condition survives.
- Any list item. Compress an item's wording; do not merge two items into one or drop one.

If a passage is already minimal, return it unchanged. Losing a rule to save tokens is a \
total failure of the task; returning a document that is merely 5% smaller but complete is a \
success.

Output the rewritten Markdown document and nothing else. No preamble, no explanation, no \
summary of changes, and no code fence wrapped around the whole document.\
"""

USER_TEMPLATE = """\
File: {path}
Purpose: {purpose}

Compress the document below under the rules in your instructions. Return only the rewritten \
Markdown.

<document>
{content}
</document>\
"""


# --------------------------------------------------------------------------- invariants


@dataclass
class Invariants:
    """What must survive compression, extracted from the original document."""

    frontmatter: str = ""
    headings: list = field(default_factory=list)      # (level, text)
    fences: list = field(default_factory=list)        # verbatim block bodies
    tables: list = field(default_factory=list)        # (header cells, body row count)
    links: list = field(default_factory=list)         # link targets
    code_spans: list = field(default_factory=list)    # inline `code`
    identifiers: list = field(default_factory=list)   # rule/requirement codes
    list_items: int = 0


FRONTMATTER_RE = re.compile(r"\A---\r?\n.*?\r?\n---\r?\n", re.S)
FENCE_RE = re.compile(r"^([ \t]*)(```+|~~~+)([^\n]*)\n(.*?)^[ \t]*\2[ \t]*$", re.S | re.M)
HEADING_RE = re.compile(r"^(#{1,6})[ \t]+(.+?)[ \t]*#*\s*$", re.M)
LINK_RE = re.compile(r"\[[^\]]*\]\(([^)\s]+)")
CODE_SPAN_RE = re.compile(r"`([^`\n]+)`")
LIST_ITEM_RE = re.compile(r"^[ \t]*(?:[-*+]|\d+\.)[ \t]+\S", re.M)
# Rule and requirement codes the framework uses to bind one statement to another.
IDENTIFIER_RE = re.compile(r"\b([A-Z]{1,4}\d{1,2}(?:\.\d{1,2})?|[A-Z]{1,4}-\d{1,3})\b")


def _mask_fences(text: str) -> str:
    """The document with fenced blocks blanked, so prose scans do not read code."""
    return FENCE_RE.sub(lambda m: "\n" * m.group(0).count("\n"), text)


def _tables(text: str) -> list:
    """Every pipe table as (header cells, body row count), in document order."""
    out, header, rows = [], None, 0
    for line in _mask_fences(text).split("\n"):
        s = line.strip()
        if s.startswith("|") and s.endswith("|") and len(s) > 1:
            cells = [c.strip() for c in s.strip("|").split("|")]
            if header is None:
                header, rows = cells, 0
            elif set("".join(cells)) <= set("-: "):
                continue
            else:
                rows += 1
            continue
        if header is not None:
            out.append((header, rows))
            header, rows = None, 0
    if header is not None:
        out.append((header, rows))
    return out


def extract(text: str) -> Invariants:
    fm = FRONTMATTER_RE.match(text)
    masked = _mask_fences(text)
    return Invariants(
        frontmatter=fm.group(0) if fm else "",
        headings=[(len(m.group(1)), m.group(2).strip()) for m in HEADING_RE.finditer(masked)],
        fences=[m.group(0).strip() for m in FENCE_RE.finditer(text)],
        tables=_tables(text),
        links=sorted({m.group(1) for m in LINK_RE.finditer(masked)}),
        code_spans=sorted({m.group(1).strip() for m in CODE_SPAN_RE.finditer(masked)}),
        identifiers=sorted({m.group(1) for m in IDENTIFIER_RE.finditer(masked)}),
        list_items=len(LIST_ITEM_RE.findall(masked)),
    )


def check(before: str, after: str) -> list:
    """Every invariant the candidate breaks, as human-readable violations.

    An empty list is the only result that permits an overwrite. Each rule is stated so its
    failure names the thing that went missing rather than reporting a similarity score.
    """
    v, src, dst = [], extract(before), extract(after)

    if not after.strip():
        return ["I0 empty candidate"]
    if "�" in after:
        v.append("I0 candidate contains replacement characters; encoding was not preserved")

    if src.frontmatter and not after.startswith(src.frontmatter):
        v.append("I1 frontmatter changed or moved; it must be reproduced byte for byte")

    if src.headings != dst.headings:
        lost = [h for h in src.headings if h not in dst.headings]
        added = [h for h in dst.headings if h not in src.headings]
        if lost:
            v.append(f"I2 heading(s) dropped or altered: {[t for _, t in lost][:5]}")
        if added:
            v.append(f"I2 heading(s) introduced: {[t for _, t in added][:5]}")
        if not lost and not added:
            v.append("I2 heading order or level changed")

    missing_fences = [f for f in src.fences if f not in [d for d in dst.fences]]
    if missing_fences:
        v.append(f"I3 {len(missing_fences)} fenced code block(s) altered or dropped")
    if len(dst.fences) != len(src.fences):
        v.append(f"I3 fenced block count changed: {len(src.fences)} -> {len(dst.fences)}")

    if len(src.tables) != len(dst.tables):
        v.append(f"I4 table count changed: {len(src.tables)} -> {len(dst.tables)}")
    else:
        for i, ((sh, sr), (dh, dr)) in enumerate(zip(src.tables, dst.tables)):
            if sh != dh:
                v.append(f"I4 table {i + 1} header changed: {sh} -> {dh}")
            elif sr != dr:
                v.append(f"I4 table {i + 1} row count changed: {sr} -> {dr}")

    lost_links = [x for x in src.links if x not in dst.links]
    if lost_links:
        v.append(f"I5 link target(s) dropped: {lost_links[:5]}")

    lost_spans = [x for x in src.code_spans if x not in dst.code_spans]
    if lost_spans:
        v.append(f"I6 inline code span(s) dropped: {lost_spans[:5]}")

    lost_ids = [x for x in src.identifiers if x not in dst.identifiers]
    if lost_ids:
        v.append(f"I7 declared identifier(s) dropped: {lost_ids[:8]}")

    if dst.list_items < src.list_items:
        v.append(f"I8 list items dropped: {src.list_items} -> {dst.list_items}")

    ratio = len(after) / max(len(before), 1)
    if ratio < FLOOR_RATIO:
        v.append(f"I9 candidate is {ratio:.0%} of the original, below the {FLOOR_RATIO:.0%} "
                 f"floor; that is deletion, not compression")

    return v


# --------------------------------------------------------------------------- discovery


@dataclass
class Target:
    path: Path
    rel: str
    bytes: int
    reason: str = ""          # why it was skipped, empty when eligible

    @property
    def eligible(self) -> bool:
        return not self.reason


def _rel(p: Path) -> str:
    """Repository-relative posix form where possible, else the absolute path."""
    try:
        return p.resolve().relative_to(REPO).as_posix()
    except ValueError:
        return p.resolve().as_posix()


def _framework_parts(p: Path) -> tuple:
    """The path's segments relative to the framework root, else the repository root.

    A path under neither keeps its absolute segments, so a denial over it can only widen.
    """
    resolved = p.resolve()
    for base in (CLAUDE, REPO):
        try:
            return resolved.relative_to(base).parts
        except ValueError:
            continue
    return resolved.parts


def _denied(p: Path) -> str:
    parts = {seg.lower() for seg in p.parts}
    hit = parts & DENIED_SEGMENTS
    if hit:
        return f"denied path segment: {sorted(hit)[0]}/"
    segments = [seg.lower() for seg in _framework_parts(p)]
    if any(a == "memory" and b == "lessons" for a, b in zip(segments, segments[1:])):
        return "denied path: memory/lessons/ is the lesson bundle, written only by the knowledge loop"
    if p.name.lower() in DENIED_NAMES:
        return f"denied filename: {p.name} is generated or machine-parsed"
    return ""


def classify(path: Path) -> Target:
    rel = _rel(path)
    denied = _denied(path)
    if denied:
        return Target(path, rel, 0, denied)
    if path.suffix.lower() != ".md":
        return Target(path, rel, 0, "not a markdown document")
    try:
        text = path.read_text(encoding="utf-8")
    except (OSError, UnicodeDecodeError) as exc:
        return Target(path, rel, 0, f"unreadable: {exc}")
    size = len(text.encode("utf-8"))
    for marker in GENERATED_MARKERS:
        if marker.search(text):
            return Target(path, rel, size, "declares itself generated")
    if size < MIN_BYTES:
        return Target(path, rel, size, f"below the {MIN_BYTES}-byte floor")
    return Target(path, rel, size)


def discover(roots: list, files: list) -> list:
    """Every candidate under the supplied roots and files, eligible or not, in path order."""
    seen, out = set(), []
    for root in roots:
        root = Path(root)
        if not root.is_absolute():
            root = (CLAUDE / root) if (CLAUDE / root).exists() else (REPO / root)
        if not root.exists():
            continue
        if root.is_file():
            candidates = [root]
        elif _denied(root):
            out.append(Target(root, _rel(root), 0, _denied(root)))
            continue
        else:
            candidates = sorted(root.rglob("*.md"))
        for p in candidates:
            if p.resolve() in seen:
                continue
            seen.add(p.resolve())
            out.append(classify(p))
    for f in files:
        f = Path(f)
        if f.exists() and f.resolve() not in seen:
            seen.add(f.resolve())
            out.append(classify(f))
    return sorted(out, key=lambda t: t.rel)


def purpose_of(rel: str) -> str:
    if "/memory/" in rel or rel.endswith("/memory"):
        return ("durable engineering memory: decisions, standards, and known issues that agents "
                "resolve against")
    if "/context/" in rel:
        return "authoritative product, technical, or release context loaded into every run"
    return "a standing instruction or governance rule file that binds agent behaviour"


# --------------------------------------------------------------------------- model access


def client_or_die():
    try:
        import anthropic
    except ImportError:
        print("FAILURE the 'anthropic' package is not installed: pip install anthropic",
              file=sys.stderr)
        raise SystemExit(2)
    try:
        return anthropic, anthropic.Anthropic()
    except Exception as exc:  # noqa: BLE001 - surfaced to the operator, not swallowed
        print(f"FAILURE could not construct an API client: {exc}", file=sys.stderr)
        raise SystemExit(2)


def count_tokens(anthropic_mod, client, text: str) -> tuple:
    """Token cost of one document, and whether it was measured or estimated.

    The report says a saving is measured rather than estimated, so a count that fell back to
    a byte heuristic must not be presented as the same kind of number. The caller records
    which it got.
    """
    try:
        return client.messages.count_tokens(
            model=MODEL, messages=[{"role": "user", "content": text}]).input_tokens, "measured"
    except anthropic_mod.APIError as exc:
        print(f"  token count unavailable ({exc.__class__.__name__}); "
              f"falling back to a byte estimate")
        return len(text) // 4, "estimated"


def strip_wrapper(text: str) -> str:
    """Remove a code fence wrapped around the whole answer, which some responses add anyway."""
    s = text.strip()
    m = re.match(r"\A(```+|~~~+)[^\n]*\n(.*)\n\1\s*\Z", s, re.S)
    return m.group(2).strip() + "\n" if m else s + "\n"


def compress(anthropic_mod, client, target: Target, text: str, effort: str) -> str:
    """One compression request. Streamed, because these documents are long."""
    prompt = USER_TEMPLATE.format(path=target.rel, purpose=purpose_of(target.rel), content=text)
    with client.messages.stream(
        model=MODEL,
        max_tokens=MAX_TOKENS,
        system=[{"type": "text", "text": SYSTEM_PROMPT, "cache_control": {"type": "ephemeral"}}],
        thinking={"type": "adaptive"},
        output_config={"effort": effort},
        messages=[{"role": "user", "content": prompt}],
    ) as stream:
        message = stream.get_final_message()

    if message.stop_reason == "refusal":
        detail = getattr(getattr(message, "stop_details", None), "explanation", "")
        raise RuntimeError(f"the request was declined: {detail or 'no explanation given'}")
    if message.stop_reason == "max_tokens":
        raise RuntimeError(f"the candidate hit the {MAX_TOKENS}-token output cap and is truncated")

    body = "".join(b.text for b in message.content if b.type == "text")
    return strip_wrapper(body)


# --------------------------------------------------------------------------- backup


def sha256(text: str) -> str:
    return "sha256:" + hashlib.sha256(text.encode("utf-8")).hexdigest()


def stamp() -> str:
    return datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")


def session_rel(rel: str) -> str:
    """A path safe to join under the session directory.

    `Target.rel` is repository-relative for a file inside the repository and absolute for one
    outside it. Joining an absolute path resolves to that path itself, so a naive join would
    write the pre-image over the original -- destroying, in the name of backing up, exactly
    what it was meant to preserve. Drive letters, leading separators, and parent traversals
    are stripped so a session write can only land inside the session.
    """
    p = rel.replace("\\", "/")
    p = re.sub(r"^[A-Za-z]:", lambda m: m.group(0)[0] + "_", p)
    parts = [seg for seg in p.split("/") if seg not in ("", ".", "..")]
    return "/".join(parts) or "unnamed.md"


def back_up(session: Path, target: Target, text: str) -> str:
    """Write the pre-image under the session directory and return its repository-relative path."""
    dest = session / "backup" / session_rel(target.rel)
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_text(text, encoding="utf-8")
    return _rel(dest)


# --------------------------------------------------------------------------- reporting


def render_report(session: Path, results: list, applied: bool) -> str:
    saved = sum(r["tokens_before"] - r["tokens_after"] for r in results if r["verdict"] == "accepted")
    before = sum(r["tokens_before"] for r in results)
    lines = [
        "# Memory Token Optimization Report",
        "",
        f"- Session: `{_rel(session)}`",
        f"- Mode: {'applied' if applied else 'dry run -- no file was modified'}",
        "- Model: the configured optimizer model; effort is recorded per file",
        f"- Files considered: {len(results)}",
        f"- Accepted: {sum(1 for r in results if r['verdict'] == 'accepted')}",
        f"- Rejected: {sum(1 for r in results if r['verdict'] == 'rejected')}",
        f"- Unchanged: {sum(1 for r in results if r['verdict'] == 'unchanged')}",
        f"- Failed: {sum(1 for r in results if r['verdict'] == 'failed')}",
        f"- Tokens before: {before}",
        f"- Tokens saved: {saved} ({(saved / before * 100) if before else 0:.1f}%)",
        "",
        "## Per-File Result",
        "",
        "| File | Before | After | Saved | Verdict | Note |",
        "|---|---|---|---|---|---|",
    ]
    for r in results:
        saved_f = r["tokens_before"] - r["tokens_after"]
        pct = f"{saved_f / r['tokens_before'] * 100:.0f}%" if r["tokens_before"] else "-"
        note = r["note"].replace("|", "\\|")[:160]
        if r.get("count_basis") == "estimated":
            note = ("token counts estimated from bytes, not measured; " + note)[:200]
        lines.append(f"| `{r['file']}` | {r['tokens_before']} | {r['tokens_after']} | "
                     f"{saved_f} ({pct}) | {r['verdict']} | {note} |")
    rejected = [r for r in results if r["verdict"] == "rejected"]
    lines += ["", "## Rejected Candidates", ""]
    if not rejected:
        lines.append("None identified.")
    else:
        lines.append("Each was discarded and its original left untouched.")
        lines.append("")
        for r in rejected:
            lines.append(f"- `{r['file']}`")
            for viol in r["violations"]:
                lines.append(f"  - {viol}")
    lines += [
        "",
        "## Invariants Checked",
        "",
        "| ID | Invariant |",
        "|---|---|",
        "| `I0` | Candidate is non-empty and decodes without replacement characters |",
        "| `I1` | YAML frontmatter is reproduced byte for byte |",
        "| `I2` | Every heading survives with the same text, level, and order; none added |",
        "| `I3` | Every fenced code block survives byte for byte |",
        "| `I4` | Every table keeps its header cells and body row count |",
        "| `I5` | Every link target survives |",
        "| `I6` | Every inline code span survives |",
        "| `I7` | Every declared rule or requirement identifier survives |",
        "| `I8` | No list item is dropped |",
        "| `I9` | The candidate is not below the size floor |",
        "",
        "These invariants are structural and identifier-level. They are what this pass verifies "
        "and therefore what it claims. Semantic equivalence of the surrounding prose is asserted "
        "by the model and is not machine-proven; a reviewer reading the recorded diff is the "
        "control for it.",
        "",
    ]
    return "\n".join(lines)


# --------------------------------------------------------------------------- commands


def cmd_scan(a) -> int:
    targets = discover(a.root or list(DEFAULT_ROOTS), [] if a.root else list(DEFAULT_FILES))
    eligible = [t for t in targets if t.eligible]
    print(f"{'file':<62} {'bytes':>8}  disposition")
    print("-" * 100)
    for t in targets:
        print(f"{t.rel[:60]:<62} {t.bytes:>8}  "
              f"{'eligible' if t.eligible else 'skipped: ' + t.reason}")
    print("-" * 100)
    print(f"{len(eligible)} eligible of {len(targets)} considered; "
          f"{sum(t.bytes for t in eligible)} bytes in scope")
    if a.json_out:
        Path(a.json_out).write_text(json.dumps(
            {"schema": "framework.runtime/optimize-memory-scan.v1",
             "targets": [asdict(t) | {"path": t.rel} for t in targets]},
            indent=2, default=str), encoding="utf-8")
        print(f"machine record: {a.json_out}")
    return 0


def cmd_run(a) -> int:  # noqa: C901 - a linear pass over files, kept in one place
    targets = [t for t in discover(a.root or list(DEFAULT_ROOTS),
                                   [] if a.root else list(DEFAULT_FILES)) if t.eligible]
    if a.only:
        wanted = {w.replace("\\", "/") for w in a.only}
        targets = [t for t in targets if any(t.rel.endswith(w) for w in wanted)]
    if not targets:
        print("no eligible file in scope")
        return 1

    anthropic_mod, client = client_or_die()
    session = RUNS / stamp()
    session.mkdir(parents=True, exist_ok=True)

    print(f"{'applying to' if a.apply else 'dry run over'} {len(targets)} file(s); "
          f"session {_rel(session)}")
    print("-" * 100)

    results = []
    for t in targets:
        text = t.path.read_text(encoding="utf-8")
        before, before_basis = count_tokens(anthropic_mod, client, text)
        row = {"file": t.rel, "tokens_before": before, "tokens_after": before,
               "verdict": "failed", "note": "", "violations": [],
               "digest_before": sha256(text), "digest_after": sha256(text),
               "backup": None, "effort": a.effort, "count_basis": before_basis}
        print(f"{t.rel}")
        try:
            candidate = compress(anthropic_mod, client, t, text, a.effort)
        except (anthropic_mod.APIError, RuntimeError) as exc:
            row["note"] = f"{exc.__class__.__name__}: {exc}"
            print(f"  FAILED   {row['note']}")
            results.append(row)
            continue

        violations = check(text, candidate)
        after, after_basis = count_tokens(anthropic_mod, client, candidate)
        row["tokens_after"], row["violations"] = after, violations
        if "estimated" in (before_basis, after_basis):
            row["count_basis"] = "estimated"

        if violations:
            row["verdict"] = "rejected"
            row["note"] = f"{len(violations)} invariant violation(s); original kept"
            print(f"  REJECTED {row['note']}")
            for v in violations[:4]:
                print(f"           {v}")
        elif before - after < max(1, int(before * MIN_GAIN_RATIO)):
            row["verdict"] = "unchanged"
            row["tokens_after"] = before
            row["note"] = (f"gain of {before - after} token(s) is under the "
                           f"{MIN_GAIN_RATIO:.0%} threshold; original kept")
            print(f"  UNCHANGED {row['note']}")
        else:
            row["verdict"] = "accepted"
            row["digest_after"] = sha256(candidate)
            row["note"] = f"-{before - after} tokens ({(before - after) / before:.0%})"
            if a.apply:
                row["backup"] = back_up(session, t, text)
                t.path.write_text(candidate, encoding="utf-8")
                print(f"  APPLIED  {row['note']}; pre-image at {row['backup']}")
            else:
                proposal = session / "proposed" / session_rel(t.rel)
                proposal.parent.mkdir(parents=True, exist_ok=True)
                proposal.write_text(candidate, encoding="utf-8")
                print(f"  PROPOSED {row['note']}; candidate at {_rel(proposal)}")
            diff = session / "diffs" / (session_rel(t.rel).replace("/", "__") + ".diff")
            diff.parent.mkdir(parents=True, exist_ok=True)
            diff.write_text("\n".join(difflib.unified_diff(
                text.splitlines(), candidate.splitlines(),
                fromfile=t.rel + " (before)", tofile=t.rel + " (after)", lineterm="")),
                encoding="utf-8")
        results.append(row)

    manifest = {
        "schema": "framework.runtime/optimize-memory.v1",
        "session": _rel(session),
        "created_at": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        "mode": "apply" if a.apply else "dry-run",
        "effort": a.effort,
        "floor_ratio": FLOOR_RATIO,
        "min_gain_ratio": MIN_GAIN_RATIO,
        "results": results,
    }
    (session / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    report = render_report(session, results, a.apply)
    (session / "report.md").write_text(report, encoding="utf-8")

    saved = sum(r["tokens_before"] - r["tokens_after"] for r in results
                if r["verdict"] == "accepted")
    total = sum(r["tokens_before"] for r in results)
    print("-" * 100)
    print(f"{sum(1 for r in results if r['verdict'] == 'accepted')} accepted, "
          f"{sum(1 for r in results if r['verdict'] == 'rejected')} rejected, "
          f"{sum(1 for r in results if r['verdict'] == 'unchanged')} unchanged, "
          f"{sum(1 for r in results if r['verdict'] == 'failed')} failed")
    print(f"{saved} of {total} tokens saved ({(saved / total * 100) if total else 0:.1f}%)")
    print(f"report   : {_rel(session / 'report.md')}")
    print(f"manifest : {_rel(session / 'manifest.json')}")
    if not a.apply:
        print("dry run: no file was modified. Re-run with --apply to overwrite.")
    return 0 if not any(r["verdict"] == "failed" for r in results) else 1


def cmd_restore(a) -> int:
    path = Path(a.manifest)
    if not path.is_absolute():
        path = (REPO / a.manifest) if (REPO / a.manifest).exists() else (CLAUDE / a.manifest)
    if not path.exists():
        print(f"FAILURE no manifest at {a.manifest}", file=sys.stderr)
        return 2
    manifest = json.loads(path.read_text(encoding="utf-8"))
    if manifest.get("mode") != "apply":
        print(f"nothing to restore: session mode is {manifest.get('mode')!r}")
        return 0

    restored, skipped = 0, 0
    for r in manifest.get("results", []):
        if r["verdict"] != "accepted" or not r.get("backup"):
            continue
        backup = REPO / r["backup"]
        target = REPO / r["file"]
        if not backup.exists():
            print(f"  MISSING  {r['file']}: pre-image {r['backup']} is gone")
            skipped += 1
            continue
        current = target.read_text(encoding="utf-8") if target.exists() else ""
        if sha256(current) != r["digest_after"] and not a.force:
            print(f"  CHANGED  {r['file']} was edited since the pass; "
                  f"pass --force to overwrite it anyway")
            skipped += 1
            continue
        target.write_text(backup.read_text(encoding="utf-8"), encoding="utf-8")
        print(f"  RESTORED {r['file']}")
        restored += 1
    print(f"{restored} file(s) restored, {skipped} skipped")
    return 0 if not skipped else 1


def main() -> int:
    ap = argparse.ArgumentParser(
        description="Compress framework memory, context, and instruction files to their "
                    "minimum token cost without losing what they say")
    sub = ap.add_subparsers(dest="cmd", required=True)

    s = sub.add_parser("scan", help="report what is in scope and what is skipped, offline")
    s.add_argument("--root", action="append",
                   help="a directory or file to scan; repeat to add more. Replaces the "
                        "default roots and instruction files entirely.")
    s.add_argument("--json-out")
    s.set_defaults(fn=cmd_scan)

    r = sub.add_parser("run", help="propose compressions; --apply to overwrite")
    r.add_argument("--root", action="append", help="see 'scan --root'")
    r.add_argument("--only", action="append",
                   help="restrict the pass to paths ending with this suffix; repeat to add more")
    r.add_argument("--apply", action="store_true",
                   help="overwrite the originals. Without it nothing on disk changes and "
                        "candidates are written under the session directory for review.")
    r.add_argument("--effort", choices=["low", "medium", "high", "xhigh", "max"], default="high")
    r.set_defaults(fn=cmd_run)

    b = sub.add_parser("restore", help="put back the pre-images of an applied session")
    b.add_argument("--manifest", required=True)
    b.add_argument("--force", action="store_true",
                   help="restore even where the file changed after the pass")
    b.set_defaults(fn=cmd_restore)

    parsed = ap.parse_args()
    return parsed.fn(parsed)


if __name__ == "__main__":
    sys.exit(main())
