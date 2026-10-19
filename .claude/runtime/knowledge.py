#!/usr/bin/env python3
"""Knowledge loop -- harvest recorded run evidence into governed lessons, and recall them.

What one run learns the hard way is already on disk: every rejected artifact is a
`validation_failed` event naming its failing check IDs, and every gate rejection is a
`gate-rejection` failure envelope in the run's recovery ledger. This module distils that
evidence into *lessons*, keeps them in one governed store, and selects the ones a dispatch of a
given phase should be shown. It never calls a model, and every lesson is derived mechanically.

Store
-----
`memory/lessons/`, an Open Knowledge Format (OKF) v0.2 bundle written through `okf.py`. One lesson
per fingerprint:

- `check:<artifact-type>:<check-id>` -- a check of the Validation Engine that rejected that
  artifact type;
- `gate:<gate>:<phase>` -- a gate that rejected the evidence of the phase it closes.

Each lesson is one concept document, `L-<10 hex>.md`, whose frontmatter carries `type: Lesson`,
a title, description, tags, sources (the evidence paths, relative to the document), generated,
verified (one entry per approval), an OKF status (`candidate` draft, `active` stable, `retired`
deprecated), `stale_after` for a retained lesson only, and the full lesson record under
`lesson_record`, a closed field list (see `LESSON_FIELDS`). `index.md` (frontmatter
`okf_version` only, then one link entry per lesson) and `log.md` (dated headings, newest first:
a Creation entry per lesson and an Update or Deprecation entry per governance action, with no
free text) are derived from the record set and never read back.

Free text from the evidence -- event summaries, report details, ledger details and resolutions,
gate rationales, actor names -- is never read into a record: the only text copied is the check's
own validator-authored description and severity. Evidence fields are recomputed from scratch on
every harvest; the governance fields (`status`, `owner`, `rationale`, `remedy`, `history`) carry
over by fingerprint, so a re-harvest is an idempotent upsert and an operator's decision survives
it. Serialisation is canonical (every string quoted with ASCII escapes, LF, trailing newline, no
harvest timestamp), so unchanged evidence yields byte-identical files, `log.md` included.

A write holds an exclusive lock file (`lessons.lock`) and, after rendering every file, replaces
in order each changed document, removes documents with no record and leftover temporary files,
then rewrites `log.md` and `index.md` last; each file goes through a temporary file and
`os.replace`, and is skipped when its bytes are equal. The store digest reported by recall and
harvest is the digest over each root `.md` file name and its sha256. A checkout still holding
the pre-bundle JSON store (`lessons.store.json`) runs `migrate` once: it writes the bundle, reads
it back, compares it record for record, and removes the JSON store only on equality. Until then
every reader and writer refuses with `store-migration-pending`. The bundle belongs to this
repository alone; one repository's lessons are never shipped to another.

Lifecycle
---------
`candidate` -> `active` (approve) -> `retired` (retire); `retired` -> `active` (approve). A
lesson is *eligible* for hydration when it is `active`, or when it is a `candidate` recurring in
at least `auto_eligible_distinct_runs` distinct runs (`config/knowledge-policy.json`, default 2).
A `retired` lesson is never eligible. Nothing is ever deleted.

Recall
------
A dispatch of phase P whose output artifact is A receives the eligible lessons whose `phases`
include P and whose `artifact_type` is A, ranked by distinct runs, then occurrences (both
descending), then lesson ID, capped at `RECALL_CAP`. The runtime decides *whether* a run
hydrates (its `knowledge_loop` record, written once at plan time); this module only decides
*what* is shown.

Dependencies: the standard library, `state_engine.digest`, and `okf`. Nothing here imports
`framework_runtime.py`; the runtime calls this module, one-way.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

import okf
import state_engine as se

CLAUDE = Path(__file__).resolve().parent.parent

BUNDLE_REL = "memory/lessons"
LOCK_REL = "memory/lessons/lessons.lock"
# The JSON store every checkout held before the bundle. Only `migrate` reads it; while it
# exists with no bundle beside it, every reader and writer refuses with
# `store-migration-pending`.
LEGACY_STORE_REL = "memory/lessons/lessons.store.json"
POLICY_REL = "config/knowledge-policy.json"
HARVEST_FILENAME = "knowledge-harvest.json"

# The in-memory interface identifier `load_store` returns; no file carries it any more.
STORE_SCHEMA = "framework.memory/lessons.v1"
POLICY_SCHEMA = "framework.config/knowledge-policy.v1"
HARVEST_SCHEMA = "framework.runtime/knowledge-harvest.v1"

STATUSES = ("candidate", "active", "retired")
KINDS = ("validation-check", "gate-rejection")
HYDRATION_VALUES = ("on", "off")
DEFAULT_THRESHOLD = 2
DEFAULT_OWNER = "tech-lead"

RECALL_CAP = 5             # fixed by design; deliberately not a policy key
DESCRIPTION_CAP = 160
REMEDY_CAP = 200
TEXT_CAP = 300             # governance actor and rationale, collapsed to one line

# The lesson bundle: one OKF concept document per lesson, named by its lesson_id.
DOCUMENT_TYPE = "Lesson"
RECORD_KEY = "lesson_record"
PRODUCER = "knowledge-loop/1.0.0"   # producer and lesson document format version
SOURCE_PREFIX = "../../"            # a source resolves against the document's own location
LESSON_ID = re.compile(r"^L-[0-9a-f]{10}$")
DOCUMENT_NAME = re.compile(r"^L-[0-9a-f]{10}\.md$")
TMP_SUFFIX = ".tmp"
EPOCH_DAY = "1970-01-01"
OKF_STATUS = {"candidate": "draft", "active": "stable", "retired": "deprecated"}
LOG_ACTIONS = {"approve": "Update", "annotate": "Update", "retire": "Deprecation"}

CHECK_ID = re.compile(r"^[A-Z]{1,3}\d+(?:\.\d+)*$")
# A remedy is operator text that later dispatches are shown. Anything that looks like a
# credential is refused outright rather than stored and masked.
CREDENTIAL_LIKE = re.compile(
    r"(?i)(?:password|passwd|secret|api[_-]?key|access[_-]?key|private[_-]?key|token|bearer)"
    r"\s*[:=]"
    r"|-----BEGIN [A-Z ]*PRIVATE KEY-----"
    r"|\bAKIA[0-9A-Z]{16}\b"
    r"|\bgh[pousr]_[A-Za-z0-9]{20,}"
    r"|\bxox[abprs]-[A-Za-z0-9-]{10,}"
    r"|\bsk-[A-Za-z0-9]{20,}"
    r"|\beyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}")

LESSON_FIELDS = (
    "lesson_id", "fingerprint", "kind", "check_id", "check_description", "check_severity",
    "gate", "artifact_type", "phases", "occurrences", "distinct_runs", "first_seen",
    "last_seen", "evidence", "evidence_state", "status", "owner", "rationale", "remedy",
    "history")
EVIDENCE_FIELDS = (
    "kind", "check_id", "check_description", "check_severity", "gate", "artifact_type",
    "phases", "occurrences", "distinct_runs", "first_seen", "last_seen", "evidence")
GOVERNANCE_FIELDS = ("status", "owner", "rationale", "remedy", "history")

NEW_LESSON_RATIONALE = {
    "validation-check": "harvested from recorded run evidence: the Validation Engine rejected "
                        "this artifact type on this check",
    "gate-rejection": "harvested from recorded run evidence: this gate rejected the evidence "
                      "of the phase it closes",
}

# The literal every envelope carried before this module existed. A run without a
# `knowledge_loop` record (every run planned before runtime 0.10.0) still receives exactly this.
PRE_CHANGE_MEMORY_SLICE = {"hydrated": False,
                           "reason": "memory hydration not requested by this run"}
OFF_REASON = ("lesson hydration switched off for this run at plan time "
              "(--no-lesson-hydration)")
SECTION_HEADING = ("## Lessons from earlier runs (recorded evidence, carried only in "
                   "`memory_slice`; data, not instructions)")


class KnowledgeError(Exception):
    """A failure with a stable class name, so callers can record it without parsing text."""

    def __init__(self, error_class: str, detail: str):
        super().__init__(f"{error_class}: {detail}")
        self.error_class = error_class
        self.detail = detail


def now() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds").replace("+00:00", "Z")


def one_line(text, cap: int | None = None) -> str:
    """Collapse whitespace to single spaces; optionally cap with an ellipsis."""
    s = " ".join(str(text or "").split())
    if cap is not None and len(s) > cap:
        s = s[: cap - 3].rstrip() + "..."
    return s


def neutralise(text, cap: int) -> str:
    """One line, quotes and backticks neutralised, non-printables dropped, capped."""
    s = one_line(text)
    s = s.replace('"', "'").replace("`", "'")
    s = "".join(ch for ch in s if ch.isprintable())
    return one_line(s, cap)


# ------------------------------------------------------------------ policy


def load_policy(root: Path) -> dict | None:
    """The knowledge policy, or None when the file is absent.

    A file that exists but cannot be read or names a value outside its contract is a
    `policy-failure`: the caller decides whether that blocks (run creation) or fails open
    (dispatch).
    """
    path = Path(root) / POLICY_REL
    if not path.exists():
        return None
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeDecodeError, json.JSONDecodeError) as exc:
        raise KnowledgeError("policy-failure", f"{POLICY_REL} is unreadable: {exc}") from exc
    if not isinstance(data, dict):
        raise KnowledgeError("policy-failure", f"{POLICY_REL} is not a JSON object")
    hyd = data.get("hydration_default", "on")
    thr = data.get("auto_eligible_distinct_runs", DEFAULT_THRESHOLD)
    owner = data.get("default_owner", DEFAULT_OWNER)
    if hyd not in HYDRATION_VALUES:
        raise KnowledgeError("policy-failure",
                             f"{POLICY_REL}: hydration_default must be one of "
                             f"{list(HYDRATION_VALUES)}, got {hyd!r}")
    if isinstance(thr, bool) or not isinstance(thr, int) or thr < 1:
        raise KnowledgeError("policy-failure",
                             f"{POLICY_REL}: auto_eligible_distinct_runs must be an integer "
                             f">= 1, got {thr!r}")
    if not isinstance(owner, str) or not owner.strip():
        raise KnowledgeError("policy-failure", f"{POLICY_REL}: default_owner must be a "
                                               f"non-empty string")
    return {"schema": data.get("schema"), "hydration_default": hyd,
            "auto_eligible_distinct_runs": thr, "default_owner": owner.strip()}


def threshold_of(policy: dict | None) -> int:
    return (policy or {}).get("auto_eligible_distinct_runs", DEFAULT_THRESHOLD)


# ------------------------------------------------------------------ run-level record


def plan_record(root: Path, no_hydration: bool) -> dict:
    """The `knowledge_loop` record a run receives once, at creation."""
    if no_hydration:
        return {"hydration": "off", "basis": "--no-lesson-hydration given at plan time"}
    policy = load_policy(root)
    if policy is None:
        return {"hydration": "off",
                "basis": f"{POLICY_REL} absent at plan time, so the run is recorded off"}
    return {"hydration": policy["hydration_default"],
            "basis": f"hydration_default {policy['hydration_default']!r} in {POLICY_REL} "
                     f"at plan time"}


def run_record(run_dir: Path) -> dict | None:
    """The run's `knowledge_loop` record, or None for a run planned before the record existed.

    Absence is the only signal of a pre-change run, and such a run never hydrates. An
    unreadable request file is treated the same way, because hydrating on a guess is exactly
    what the record exists to prevent.
    """
    req = Path(run_dir) / "execution-request.json"
    if not req.exists():
        return None
    try:
        rec = json.loads(req.read_text(encoding="utf-8")).get("knowledge_loop")
    except (OSError, UnicodeDecodeError, json.JSONDecodeError, AttributeError):
        return None
    if isinstance(rec, dict) and rec.get("hydration") in HYDRATION_VALUES:
        return rec
    return None


# ------------------------------------------------------------------ store


def bundle_path(root: Path) -> Path:
    return Path(root) / BUNDLE_REL


def legacy_store_path(root: Path) -> Path:
    return Path(root) / LEGACY_STORE_REL


def bytes_digest(data: bytes) -> str:
    return se.digest(data.decode("utf-8"))


def lesson_id_for(fingerprint: str) -> str:
    return "L-" + se.digest(fingerprint)[len("sha256:"):][:10]


def _documents(bundle: Path) -> list:
    """The lesson documents at the bundle root, sorted by name."""
    if not bundle.is_dir():
        return []
    return sorted((p for p in bundle.iterdir() if p.is_file() and DOCUMENT_NAME.match(p.name)),
                  key=lambda p: p.name)


def bundle_exists(root: Path) -> bool:
    """True when the bundle directory holds index.md or any lesson document."""
    bundle = bundle_path(root)
    return (bundle / okf.INDEX_NAME).is_file() or bool(_documents(bundle))


def _refuse_pending(root: Path) -> None:
    if legacy_store_path(root).exists() and not bundle_exists(root):
        raise KnowledgeError("store-migration-pending",
                             f"{LEGACY_STORE_REL} holds the lessons and {BUNDLE_REL} does not; "
                             f"run `knowledge.py migrate` once to move them into the bundle")


def bundle_digest(root: Path) -> str | None:
    """The state_engine digest over `name sha256` lines, one per root .md file, by name."""
    bundle = bundle_path(root)
    if not bundle.is_dir():
        return None
    lines = [f"{p.name} {hashlib.sha256(p.read_bytes()).hexdigest()}"
             for p in sorted(bundle.iterdir(), key=lambda p: p.name)
             if p.is_file() and p.suffix == ".md"]
    return se.digest("\n".join(lines))


def _read_legacy(root: Path) -> dict:
    """The JSON store a pre-bundle checkout holds; read only by `migrate`."""
    try:
        data = json.loads(legacy_store_path(root).read_bytes().decode("utf-8"))
    except (OSError, UnicodeDecodeError, json.JSONDecodeError) as exc:
        raise KnowledgeError("store-unreadable", f"{LEGACY_STORE_REL}: {exc}") from exc
    if not isinstance(data, dict) or data.get("schema") != STORE_SCHEMA \
            or not isinstance(data.get("lessons"), list):
        raise KnowledgeError("store-unreadable",
                             f"{LEGACY_STORE_REL} is not a {STORE_SCHEMA} document")
    return data


def load_store(root: Path) -> dict | None:
    """The lessons as one in-memory store document, or None when no bundle exists yet.

    Every lesson document at the bundle root is parsed; each must be a `Lesson` whose
    `lesson_record` holds exactly the `LESSON_FIELDS` keys and whose `lesson_id` is the file
    stem. The returned shape is the one every caller has always used: `schema` and the lessons
    ordered by fingerprint.
    """
    root = Path(root)
    _refuse_pending(root)
    if not bundle_exists(root):
        return None
    lessons = []
    for path in _documents(bundle_path(root)):
        rel = f"{BUNDLE_REL}/{path.name}"
        try:
            front, _ = okf.parse_document(path.read_bytes().decode("utf-8"))
        except (OSError, UnicodeDecodeError, okf.OkfError) as exc:
            raise KnowledgeError("store-unreadable", f"{rel}: {exc}") from exc
        if not isinstance(front, dict) or front.get("type") != DOCUMENT_TYPE:
            raise KnowledgeError("store-unreadable", f"{rel} is not a {DOCUMENT_TYPE} document")
        record = front.get(RECORD_KEY)
        if not isinstance(record, dict) or set(record) != set(LESSON_FIELDS):
            raise KnowledgeError("store-unreadable",
                                 f"{rel}: {RECORD_KEY} does not hold exactly the lesson fields")
        if record.get("lesson_id") != path.stem:
            raise KnowledgeError("store-unreadable",
                                 f"{rel}: lesson_id {record.get('lesson_id')!r} is not the "
                                 f"file name")
        lessons.append({k: record[k] for k in LESSON_FIELDS})
    lessons.sort(key=lambda l: str(l.get("fingerprint")))
    return {"schema": STORE_SCHEMA, "lessons": lessons}


# --- lesson document, index, and change log: pure functions of the record set


def okf_status(status) -> str:
    return OKF_STATUS.get(status, "draft")


def okf_actor(actor) -> str:
    """`human:` and a sanitised identifier; the prefix is never doubled."""
    text = str(actor or "").strip()
    if text.lower().startswith("human:"):
        text = text[len("human:"):]
    text = re.sub(r"[^A-Za-z0-9._@-]", "-", text).strip("-")[:64].strip("-")
    return "human:" + (text or "unknown")


def lesson_title(lesson: dict) -> str:
    key = lesson.get("check_id") or lesson.get("gate") or "?"
    return (f"{neutralise(lesson.get('kind'), 40)} {neutralise(key, 60)} on "
            f"{neutralise(lesson.get('artifact_type'), 60)}")


def lesson_description(lesson: dict) -> str:
    """The description exactly as `render_section` shows it."""
    return neutralise(lesson.get("check_description"), DESCRIPTION_CAP) \
        or "no description recorded"


def lesson_frontmatter(lesson: dict) -> dict:
    """The OKF frontmatter for one lesson; the whole record rides under `lesson_record`."""
    tags = []
    for raw in [lesson.get("kind"), lesson.get("artifact_type"), *(lesson.get("phases") or [])]:
        tag = neutralise(raw, 60)
        if tag and tag not in tags:
            tags.append(tag)
    front = {"type": DOCUMENT_TYPE, "title": lesson_title(lesson),
             "description": lesson_description(lesson), "tags": tags or ["lesson"]}
    paths = sorted({str(ref).split("#", 1)[0] for ref in (lesson.get("evidence") or [])}
                   - {""})
    if paths:
        front["sources"] = [{"resource": SOURCE_PREFIX + p} for p in paths]
    seen = lesson.get("last_seen") or lesson.get("first_seen")
    if seen:
        front["generated"] = {"at": seen, "by": PRODUCER}
    verified = [{"at": h.get("time"), "by": okf_actor(h.get("actor"))}
                for h in (lesson.get("history") or [])
                if isinstance(h, dict) and h.get("action") == "approve"]
    if verified:
        front["verified"] = verified
    front["status"] = okf_status(lesson.get("status"))
    if lesson.get("evidence_state") == "retained" and seen:
        front["stale_after"] = seen
    front[RECORD_KEY] = {k: lesson.get(k) for k in LESSON_FIELDS}
    return front


def render_document(lesson: dict) -> bytes:
    """One lesson document; refuses an unsafe lesson_id or an unsupported value type."""
    lesson_id = lesson.get("lesson_id")
    if not isinstance(lesson_id, str) or not LESSON_ID.match(lesson_id):
        raise KnowledgeError("store-unwritable",
                             f"lesson_id {lesson_id!r} is not L- and ten lowercase hex "
                             f"characters; nothing was written")
    front = lesson_frontmatter(lesson)
    body = f"# {front['title']}\n\nSummary: {front['description']}\n"
    try:
        text = okf.join_document(front, body, keep_order=(RECORD_KEY,))
    except okf.OkfError as exc:
        raise KnowledgeError("store-unwritable",
                             f"{lesson_id}: {exc}; nothing was written") from exc
    return text.encode("utf-8")


def _md_escape(text: str) -> str:
    return text.replace("\\", "\\\\").replace("[", "\\[").replace("]", "\\]")


def render_index(lessons: list) -> bytes:
    """index.md: the okf_version frontmatter, a heading, one link entry per lesson."""
    lines = [okf.DELIMITER, f'okf_version: "{okf.OKF_VERSION}"', okf.DELIMITER, "# Lessons", ""]
    for l in lessons:
        lines.append(f"- [{_md_escape(lesson_title(l))}]({l['lesson_id']}.md) - "
                     f"{_md_escape(lesson_description(l))}")
    return ("\n".join(lines) + "\n").encode("utf-8")


def _log_entries(lessons: list) -> list:
    """(timestamp, fingerprint, history position, line); creation has position -1."""
    entries = []
    for l in lessons:
        lid, fp = l["lesson_id"], str(l.get("fingerprint"))
        history = [h for h in (l.get("history") or []) if isinstance(h, dict)]
        stamp = str(l.get("first_seen") or l.get("last_seen") or EPOCH_DAY)
        initial = history[0].get("from") if history else l.get("status")
        entries.append((stamp, fp, -1, f"- **Creation** [{lid}]({lid}.md): status "
                                       f"{okf_status(initial)}, by {PRODUCER}"))
        for pos, h in enumerate(history):
            word = LOG_ACTIONS.get(h.get("action"), "Update")
            entries.append((str(h.get("time") or EPOCH_DAY), fp, pos,
                            f"- **{word}** [{lid}]({lid}.md): status "
                            f"{okf_status(h.get('to'))}, by {okf_actor(h.get('actor'))}"))
    return entries


def _day(stamp: str) -> str:
    try:
        return datetime.fromisoformat(stamp[:10]).date().isoformat()
    except ValueError:
        return EPOCH_DAY


def render_log(lessons: list) -> bytes:
    """log.md: dated headings newest first; within a date, newest entry first."""
    by_day: dict = {}
    for entry in _log_entries(lessons):
        by_day.setdefault(_day(entry[0]), []).append(entry)
    lines = ["# Lesson Change Log"]
    for day in sorted(by_day, reverse=True):
        rows = sorted(by_day[day], key=lambda e: (e[1], -e[2]))
        rows.sort(key=lambda e: e[0], reverse=True)
        lines += ["", f"## {day}", ""] + [r[3] for r in rows]
    return ("\n".join(lines) + "\n").encode("utf-8")


def _replace(path: Path, data: bytes) -> bool:
    """Whole-file replace through a temporary file; skipped when the bytes are equal."""
    if path.is_file() and path.read_bytes() == data:
        return False
    tmp = path.with_name(path.name + TMP_SUFFIX)
    tmp.write_bytes(data)
    os.replace(tmp, path)
    return True


def _write_bundle(root: Path, data: dict) -> bool:
    lessons = sorted((l for l in data.get("lessons") or [] if isinstance(l, dict)),
                     key=lambda l: str(l.get("fingerprint")))
    docs = {f"{l.get('lesson_id')}.md": render_document(l) for l in lessons}
    if len(docs) != len(lessons):
        raise KnowledgeError("store-unwritable",
                             "two lessons share one lesson_id; nothing was written")
    log, index = render_log(lessons), render_index(lessons)
    bundle = bundle_path(root)
    bundle.mkdir(parents=True, exist_ok=True)
    changed = False
    for name in sorted(docs):                                   # step 1: documents
        changed = _replace(bundle / name, docs[name]) or changed
    for path in sorted(bundle.iterdir(), key=lambda p: p.name):  # step 2: orphans, temps
        orphan = bool(DOCUMENT_NAME.match(path.name)) and path.name not in docs
        if path.is_file() and (orphan or path.name.endswith(TMP_SUFFIX)):
            path.unlink()
            changed = True
    changed = _replace(bundle / okf.LOG_NAME, log) or changed      # step 3: log.md
    changed = _replace(bundle / okf.INDEX_NAME, index) or changed  # step 4: index.md last
    return changed


def write_store(root: Path, data: dict) -> bool:
    """Write the bundle for `data`; True when any file was written or removed.

    Every file is rendered before the first is touched, so a guard or type failure writes
    nothing. Then, in order: each changed lesson document; the removal of documents with no
    record and of leftover temporary files; log.md; index.md last. The reserved files are pure
    functions of the record set, so an interrupted write is repaired by the next one. The
    caller holds the lock.
    """
    root = Path(root)
    _refuse_pending(root)
    return _write_bundle(root, data)


def migrate(root: Path = CLAUDE, *, lock_timeout: float = 10.0) -> dict:
    """Move the JSON store into the bundle; the JSON store is removed only once verified.

    Under the lock: read the JSON store, write the full bundle, read the bundle back, and
    compare it record for record (count, lesson_id set, equal records). Only on equality is
    the JSON store removed. On any mismatch both forms stay in place and the call fails with
    `migration-mismatch`. Re-running after an interruption repeats the whole procedure.

    With both forms present the bundle is authoritative, so the JSON store may only complete
    it, never overwrite it: the procedure runs only when every lesson the bundle already holds
    is in the JSON store with an equal record, as after an interrupted or mismatched earlier
    migration. When the bundle holds a lesson the JSON store lacks, or a record unequal to
    its JSON record (a later harvest or governance action), the call fails with
    `migration-mismatch` before anything is written: no bundle file changes and the JSON
    store is kept.
    """
    root = Path(root)
    if not legacy_store_path(root).exists():
        return {"status": "no-legacy-store", "lessons": None, "documents": None,
                "store": BUNDLE_REL, "legacy_removed": False, "store_digest": bundle_digest(root)}
    with StoreLock(root, lock_timeout):
        source = _read_legacy(root)
        records = source["lessons"]
        if not all(isinstance(l, dict) and set(l) == set(LESSON_FIELDS) for l in records):
            raise KnowledgeError("migration-mismatch",
                                 f"{LEGACY_STORE_REL} holds a record without exactly the lesson "
                                 f"fields; nothing was written and the JSON store was kept")
        want = sorted(({k: l[k] for k in LESSON_FIELDS} for l in records),
                      key=lambda l: str(l.get("fingerprint")))
        if bundle_exists(root):
            by_id = {l["lesson_id"]: l for l in want}
            present = (load_store(root) or {"lessons": []})["lessons"]
            only = sorted(l["lesson_id"] for l in present if l["lesson_id"] not in by_id)
            differ = sorted(l["lesson_id"] for l in present
                            if l["lesson_id"] in by_id and l != by_id[l["lesson_id"]])
            if only or differ:
                found = []
                if only:
                    found.append(f"{len(only)} lesson(s) only in the bundle "
                                 f"({', '.join(only)})")
                if differ:
                    found.append(f"{len(differ)} lesson(s) whose bundle record differs from "
                                 f"the JSON record ({', '.join(differ)})")
                raise KnowledgeError("migration-mismatch",
                                     f"{BUNDLE_REL} already exists and is authoritative; it holds "
                                     + " and ".join(found)
                                     + f"; nothing was written and {LEGACY_STORE_REL} was kept. "
                                     f"Reconcile the two before removing the JSON store")
        written = _write_bundle(root, {"schema": STORE_SCHEMA, "lessons": want})
        got = (load_store(root) or {"lessons": []})["lessons"]
        problems = []
        if len(got) != len(want):
            problems.append(f"{len(got)} documents read back for {len(want)} records")
        if {l["lesson_id"] for l in got} != {l["lesson_id"] for l in want}:
            problems.append("the lesson_id sets differ")
        if not problems and got != want:
            problems.append("a record read back differs from its JSON record")
        if problems:
            raise KnowledgeError("migration-mismatch",
                                 "; ".join(problems) + f"; {LEGACY_STORE_REL} was kept")
        legacy_store_path(root).unlink()
        return {"status": "migrated", "lessons": len(want),
                "documents": len(_documents(bundle_path(root))), "store": BUNDLE_REL,
                "bundle_written": written, "legacy_removed": True,
                "store_digest": bundle_digest(root)}


class StoreLock:
    """Exclusive lock file beside the store. `timeout` 0 means try once and never wait."""

    def __init__(self, root: Path, timeout: float = 10.0):
        self.path = Path(root) / LOCK_REL
        self.timeout = timeout
        self.held = False

    def __enter__(self):
        self.path.parent.mkdir(parents=True, exist_ok=True)
        deadline = time.monotonic() + max(0.0, self.timeout)
        while True:
            try:
                fd = os.open(self.path, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
            except FileExistsError:
                if time.monotonic() >= deadline:
                    raise KnowledgeError(
                        "lock-contention",
                        f"{LOCK_REL} is held by another writer; nothing was written. If no "
                        f"writer is running, the lock is stale and may be removed") from None
                time.sleep(0.05)
                continue
            with os.fdopen(fd, "w", encoding="utf-8") as fh:
                fh.write(f"pid={os.getpid()} at={now()}\n")
            self.held = True
            return self

    def __exit__(self, *exc):
        if self.held:
            try:
                self.path.unlink()
            except FileNotFoundError:
                pass
            self.held = False
        return False


def find_lesson(data: dict, ref: str) -> dict | None:
    for lesson in data.get("lessons") or []:
        if lesson.get("lesson_id") == ref or lesson.get("fingerprint") == ref:
            return lesson
    return None


# ------------------------------------------------------------------ evidence reading


def _read_json(path: Path, counters: dict):
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError:
        return None
    except (OSError, UnicodeDecodeError, json.JSONDecodeError):
        counters["unreadable_files"] += 1
        return None


def _read_events(path: Path, counters: dict) -> list:
    if not path.exists():
        return []
    try:
        lines = path.read_text(encoding="utf-8").splitlines()
    except (OSError, UnicodeDecodeError):
        counters["unreadable_files"] += 1
        return []
    out = []
    for n, line in enumerate(lines, 1):
        if not line.strip():
            continue
        try:
            ev = json.loads(line)
        except json.JSONDecodeError:
            counters["unreadable_lines"] += 1
            continue
        if isinstance(ev, dict):
            ev["_line"] = n
            out.append(ev)
    return out


def _run_maps(state: dict | None) -> tuple[dict, dict]:
    """state_id -> output artifact type, and gate -> closed phase, from a run's state.json."""
    artifacts, closes = {}, {}
    if not isinstance(state, dict):
        return artifacts, closes
    for item in (state.get("work_items") or {}).values():
        if isinstance(item, dict) and item.get("work_type", "state") == "state" \
                and item.get("artifact"):
            artifacts[item.get("state_id")] = item["artifact"]
    for name, gate in (state.get("gates") or {}).items():
        if isinstance(gate, dict) and gate.get("closes_state"):
            closes[gate.get("gate") or name] = gate["closes_state"]
    return artifacts, closes


def _event_ref(run_id: str, ev: dict) -> str:
    return f"runs/{run_id}/events.jsonl#{ev.get('event_id') or 'line-' + str(ev.get('_line'))}"


def _artifact_name(path_text) -> str:
    return re.split(r"[\\/]", str(path_text or ""))[-1]


def scan(root: Path) -> tuple[list, dict]:
    """Every lesson occurrence in the recorded evidence, plus the scan counters."""
    counters = {"runs_scanned": 0, "occurrences": 0, "unresolved": 0,
                "empty_failure_lists": 0, "skipped_ids": 0, "unreadable_files": 0,
                "unreadable_lines": 0}
    occurrences: list = []
    runs = Path(root) / "runs"
    if not runs.is_dir():
        return occurrences, counters
    for run_dir in sorted(p for p in runs.iterdir() if p.is_dir() and p.name.startswith("run-")):
        run_id = run_dir.name
        counters["runs_scanned"] += 1
        artifacts, closes = _run_maps(_read_json(run_dir / "state.json", counters))
        events = _read_events(run_dir / "events.jsonl", counters)
        rollbacks: dict = {}
        for idx, ev in enumerate(events):
            etype = ev.get("event_type")
            details = ev.get("details_ref") if isinstance(ev.get("details_ref"), dict) else {}
            if etype == "validation_failed":
                failures = details.get("failures")
                if not isinstance(failures, list) or not failures:
                    counters["empty_failure_lists"] += 1
                    continue
                state_id = ev.get("state_id")
                artifact = artifacts.get(state_id)
                for cid in failures:
                    if not isinstance(cid, str) or not CHECK_ID.match(cid):
                        counters["skipped_ids"] += 1
                        continue
                    if not artifact:
                        counters["unresolved"] += 1
                        continue
                    occurrences.append({
                        "kind": "validation-check",
                        "fingerprint": f"check:{artifact}:{cid}",
                        "check_id": cid, "gate": None, "artifact_type": artifact,
                        "phase": state_id, "run_id": run_id,
                        "timestamp": ev.get("timestamp") or "", "order": idx,
                        "ref": _event_ref(run_id, ev),
                        "report": (run_dir / details["report"]) if isinstance(
                            details.get("report"), str) else None})
            elif etype == "rollback_scheduled" and isinstance(details.get("gate"), str):
                rollbacks.setdefault(details["gate"], []).append(_event_ref(run_id, ev))
        ledger = _read_json(run_dir / "recovery-ledger.json", counters)
        entries = (ledger or {}).get("entries") if isinstance(ledger, dict) else None
        for n, entry in enumerate(entries or []):
            if not isinstance(entry, dict) or entry.get("failure_class") != "gate-rejection" \
                    or entry.get("work_type") != "gate":
                # Approval waits are normal flow, output-schema failures duplicate the event,
                # and the successor's own gate-rejection envelope is a consequence of the
                # gate's, so only the gate work item's envelope is an occurrence.
                continue
            gate = entry.get("state_id")
            bundle = entry.get("evidence_bundle_ref") if isinstance(
                entry.get("evidence_bundle_ref"), dict) else {}
            phase = bundle.get("closes_state") or closes.get(gate)
            artifact = artifacts.get(phase)
            if not isinstance(gate, str) or not gate or not phase or not artifact:
                counters["unresolved"] += 1
                continue
            occurrences.append({
                "kind": "gate-rejection", "fingerprint": f"gate:{gate}:{phase}",
                "check_id": None, "gate": gate, "artifact_type": artifact, "phase": phase,
                "run_id": run_id, "timestamp": entry.get("detected_at") or "",
                "order": n,
                "ref": f"runs/{run_id}/recovery-ledger.json#"
                       f"{entry.get('envelope_id') or 'entry-' + str(n + 1)}",
                "extra_refs": rollbacks.get(gate, []), "report": None})
    counters["occurrences"] = len(occurrences)
    return occurrences, counters


class _Descriptions:
    """Validator-authored check description and severity, resolved per artifact type."""

    def __init__(self, root: Path, counters: dict):
        self.root = Path(root)
        self.counters = counters
        self.cache: dict = {}
        self.by_artifact: dict | None = None

    def _checks(self, path: Path) -> dict:
        key = str(path)
        if key not in self.cache:
            rep = _read_json(path, self.counters)
            checks = {}
            if isinstance(rep, dict):
                for c in rep.get("checks") or []:
                    if isinstance(c, dict) and isinstance(c.get("id"), str):
                        checks[c["id"]] = (c.get("description"), c.get("severity"))
            self.cache[key] = (checks, _artifact_name(rep.get("artifact"))
                               if isinstance(rep, dict) else "")
        return self.cache[key][0]

    def _index(self) -> dict:
        if self.by_artifact is None:
            self.by_artifact = {}
            runs = self.root / "runs"
            if runs.is_dir():
                for path in sorted(runs.glob("run-*/states/*/validation-report.json")):
                    self._checks(path)
                    name = self.cache[str(path)][1]
                    self.by_artifact.setdefault(name, []).append(path)
        return self.by_artifact

    def resolve(self, artifact: str, check_id: str, occurrences: list) -> tuple:
        latest_first = sorted((o for o in occurrences if o.get("report")),
                              key=lambda o: (o["timestamp"], o["run_id"], o["order"]),
                              reverse=True)
        for o in latest_first:
            found = self._checks(o["report"]).get(check_id)
            if found and found[0]:
                return found
        for path in self._index().get(artifact, []):
            found = self._checks(path).get(check_id)
            if found and found[0]:
                return found
        return (None, None)


def _evidence_fields(fingerprint: str, occ: list, descriptions: _Descriptions) -> dict:
    first = occ[0]
    times = sorted(o["timestamp"] for o in occ if o["timestamp"])
    refs = set(o["ref"] for o in occ)
    for o in occ:
        refs.update(o.get("extra_refs") or [])
    if first["kind"] == "validation-check":
        desc, sev = descriptions.resolve(first["artifact_type"], first["check_id"], occ)
        description = one_line(desc) if desc else None
        severity = one_line(sev) if sev else None
    else:
        description = (f"Gate '{first['gate']}' rejected the evidence of phase "
                       f"'{first['phase']}' and the phase was sent back for rework")
        severity = None
    return {
        "kind": first["kind"], "check_id": first["check_id"],
        "check_description": description, "check_severity": severity,
        "gate": first["gate"], "artifact_type": first["artifact_type"],
        "phases": sorted({o["phase"] for o in occ}),
        "occurrences": len(occ), "distinct_runs": len({o["run_id"] for o in occ}),
        "first_seen": times[0] if times else None, "last_seen": times[-1] if times else None,
        "evidence": sorted(refs),
    }


# ------------------------------------------------------------------ harvest


def harvest(root: Path = CLAUDE, *, lock_timeout: float = 10.0) -> dict:
    """Rebuild every lesson's evidence from the recorded runs and upsert the store.

    Returns the outcome counters. Never deletes a lesson: one whose evidence has gone is kept
    with `evidence_state: retained`. With no runs and no store, nothing is written at all.
    """
    started = time.monotonic()
    root = Path(root)
    policy = load_policy(root)
    occurrences, counters = scan(root)
    grouped: dict = {}
    for o in occurrences:
        grouped.setdefault(o["fingerprint"], []).append(o)
    descriptions = _Descriptions(root, counters)
    computed = {fp: _evidence_fields(fp, occ, descriptions)
                for fp, occ in sorted(grouped.items())}

    outcome = {"status": "succeeded", "lessons": 0, "created": 0, "updated": 0,
               "retained": 0, "store": BUNDLE_REL, "store_written": False,
               "store_digest": None, **counters}
    _refuse_pending(root)
    if not computed and not bundle_exists(root):
        outcome["elapsed_seconds"] = round(time.monotonic() - started, 3)
        return outcome

    with StoreLock(root, lock_timeout):
        existing = load_store(root) or {"schema": STORE_SCHEMA, "lessons": []}
        by_fp = {l.get("fingerprint"): l for l in existing["lessons"] if isinstance(l, dict)}
        merged = []
        for fp in sorted(set(by_fp) | set(computed)):
            old = by_fp.get(fp)
            if fp in computed:
                ev = computed[fp]
                if old is None:
                    gov = {"status": "candidate",
                           "owner": (policy or {}).get("default_owner", DEFAULT_OWNER),
                           "rationale": NEW_LESSON_RATIONALE[ev["kind"]], "remedy": None,
                           "history": []}
                    outcome["created"] += 1
                else:
                    gov = {k: old.get(k) for k in GOVERNANCE_FIELDS}
                    gov["history"] = list(old.get("history") or [])
                    if any(old.get(k) != ev[k] for k in EVIDENCE_FIELDS) \
                            or old.get("evidence_state") != "current":
                        outcome["updated"] += 1
                rec = {"lesson_id": lesson_id_for(fp), "fingerprint": fp, **ev,
                       "evidence_state": "current", **gov}
            else:
                rec = {k: old.get(k) for k in LESSON_FIELDS}
                rec["evidence_state"] = "retained"
                outcome["retained"] += 1
            merged.append(rec)
        data = {"schema": STORE_SCHEMA, "lessons": merged}
        outcome["store_written"] = write_store(root, data)
        outcome["lessons"] = len(merged)
        outcome["store_digest"] = bundle_digest(root)
    outcome["elapsed_seconds"] = round(time.monotonic() - started, 3)
    return outcome


def harvest_for_run(root: Path, run_dir: Path) -> dict | None:
    """The isolated post-aggregation step: harvest, record the outcome, never raise.

    Runs only for a run carrying a `knowledge_loop` record. The lock is tried once and never
    waited on, so an aggregation is never held up by a concurrent writer; contention is
    recorded as `skipped`, any other failure as `failed`, each with its error class, in
    `runs/<run>/knowledge-harvest.json`. No ledger event is emitted.
    """
    try:
        if run_record(run_dir) is None:
            return None
    except Exception:  # noqa: BLE001 -- the step must never fail an aggregation
        return None
    record = {"schema": HARVEST_SCHEMA, "run_id": Path(run_dir).name, "at": now()}
    try:
        out = harvest(root, lock_timeout=0.0)
        record.update({k: out.get(k) for k in (
            "status", "lessons", "created", "updated", "retained", "runs_scanned",
            "occurrences", "unresolved", "store_written", "store_digest", "elapsed_seconds")})
        record["error_class"] = None
    except KnowledgeError as exc:
        record.update({"status": "skipped" if exc.error_class == "lock-contention"
                       else "failed", "error_class": exc.error_class})
    except Exception as exc:  # noqa: BLE001 -- the step must never fail an aggregation
        record.update({"status": "failed", "error_class": type(exc).__name__})
    try:
        (Path(run_dir) / HARVEST_FILENAME).write_text(json.dumps(record, indent=2) + "\n",
                                                      encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    return record


# ------------------------------------------------------------------ governance


def _screen_remedy(remedy) -> str:
    text = one_line(remedy)
    if not text:
        raise KnowledgeError("request-validation-failure", "a remedy must not be empty")
    if len(text) > REMEDY_CAP:
        raise KnowledgeError("request-validation-failure",
                             f"a remedy is at most {REMEDY_CAP} characters on one line; "
                             f"got {len(text)}")
    if not text.isprintable():
        raise KnowledgeError("request-validation-failure",
                             "a remedy may hold printable characters only")
    if CREDENTIAL_LIKE.search(text):
        raise KnowledgeError("request-validation-failure",
                             "the remedy looks like it carries a credential; it was refused "
                             "and nothing was written")
    return text


TRANSITIONS = {
    "approve": ({"candidate", "retired"}, "active"),
    "retire": ({"candidate", "active"}, "retired"),
}


def govern(root: Path, action: str, lesson_ref: str, *, actor: str, rationale: str,
           remedy: str | None = None, lock_timeout: float = 10.0) -> dict:
    """Approve, retire, or annotate one lesson, appending exactly one history entry."""
    actor_s, rationale_s = one_line(actor, TEXT_CAP), one_line(rationale, TEXT_CAP)
    if not actor_s or not rationale_s:
        raise KnowledgeError("request-validation-failure",
                             "every governance action needs --actor and --rationale")
    if action == "annotate":
        remedy_s = _screen_remedy(remedy)
    elif action not in TRANSITIONS:
        raise KnowledgeError("request-validation-failure", f"unknown action {action!r}")
    root = Path(root)
    _refuse_pending(root)
    if not bundle_exists(root):
        raise KnowledgeError("unknown-lesson", f"no lesson bundle at {BUNDLE_REL}; run harvest "
                                               f"first")
    with StoreLock(root, lock_timeout):
        data = load_store(root)
        lesson = find_lesson(data, lesson_ref)
        if lesson is None:
            raise KnowledgeError("unknown-lesson", f"no lesson {lesson_ref!r} in {BUNDLE_REL}")
        before = lesson.get("status")
        if action == "annotate":
            after = before
            lesson["remedy"] = remedy_s
        else:
            allowed, after = TRANSITIONS[action]
            if before not in allowed:
                raise KnowledgeError("invalid-transition",
                                     f"{action} is not permitted from status {before!r}")
            lesson["status"] = after
        lesson.setdefault("history", []).append({
            "action": action, "from": before, "to": after, "actor": actor_s,
            "rationale": rationale_s, "time": now()})
        write_store(root, data)
        return lesson


# ------------------------------------------------------------------ eligibility and recall


def eligible(lesson: dict, threshold: int) -> bool:
    status = lesson.get("status")
    if status == "active":
        return True
    if status == "candidate":
        return int(lesson.get("distinct_runs") or 0) >= threshold
    return False


def rank_key(lesson: dict):
    return (-int(lesson.get("distinct_runs") or 0), -int(lesson.get("occurrences") or 0),
            str(lesson.get("lesson_id")))


SLICE_FIELDS = ("lesson_id", "kind", "check_id", "gate", "artifact_type", "check_description",
                "check_severity", "distinct_runs", "occurrences", "last_seen", "status",
                "remedy")


def recall(root: Path, phase: str, artifact_type: str) -> dict:
    """The lessons a dispatch of `phase` (emitting `artifact_type`) would be shown."""
    root = Path(root)
    policy = load_policy(root)
    threshold = threshold_of(policy)
    data = load_store(root)
    lessons = (data or {}).get("lessons") or []
    matches = [l for l in lessons if isinstance(l, dict) and eligible(l, threshold)
               and phase in (l.get("phases") or []) and l.get("artifact_type") == artifact_type]
    ranked = sorted(matches, key=rank_key)[:RECALL_CAP]
    return {
        "store": BUNDLE_REL,
        "store_digest": bundle_digest(root) if data is not None else None,
        "threshold": threshold,
        "matched": len(matches),
        "lessons": [{k: l.get(k) for k in SLICE_FIELDS} for l in ranked],
    }


def dispatch_memory_slice(root: Path, record: dict | None, phase: str,
                          artifact_type: str) -> tuple[dict, list | None]:
    """`memory_slice` for one dispatch, and the lesson IDs shown (None for a pre-change run).

    A run with no record receives the pre-change literal and nothing else. A run recorded off
    receives `hydrated: false` with a reason naming the plan flag. A run recorded on receives
    the recalled lessons; an unreadable store or policy fails open, withholding lessons for
    this dispatch and naming the fault, because a lesson is advice and never a precondition.
    """
    if record is None:
        return dict(PRE_CHANGE_MEMORY_SLICE), None
    if record.get("hydration") != "on":
        return {"hydrated": False, "reason": OFF_REASON}, []
    try:
        got = recall(root, phase, artifact_type)
    except Exception as exc:  # noqa: BLE001 -- fail open: lessons are advice, not a gate
        cls = exc.error_class if isinstance(exc, KnowledgeError) else type(exc).__name__
        return {"hydrated": False,
                "reason": f"lessons withheld for this dispatch: the lesson store or policy "
                          f"could not be read ({cls}); recall fails open"}, []
    return {"hydrated": True,
            "reason": "lessons from earlier runs hydrated per this run's knowledge_loop "
                      "record (recorded at plan time)",
            **got}, [l["lesson_id"] for l in got["lessons"]]


def render_section(lessons: list) -> list:
    """The bounded prompt section: one heading line, then one or two lines per lesson."""
    if not lessons:
        return []
    lines = [SECTION_HEADING]
    for l in [l for l in lessons if isinstance(l, dict)][:RECALL_CAP]:
        # Every field comes from the store, which load_store does not validate, so each one is
        # neutralised: a malformed record can never add a line or an unquoted heading (F-005).
        key = l.get("check_id") or l.get("gate") or "?"
        desc = neutralise(l.get("check_description"), DESCRIPTION_CAP) \
            or "no description recorded"
        runs = l.get("distinct_runs")
        runs = runs if isinstance(runs, int) and not isinstance(runs, bool) else 0
        lines.append(f"- `{neutralise(l.get('lesson_id'), 40)}` "
                     f"{neutralise(l.get('kind'), 40)} `{neutralise(key, 60)}` on "
                     f"`{neutralise(l.get('artifact_type'), 60)}`, seen in "
                     f"{runs} run(s): \"{desc}\"")
        if l.get("remedy"):
            lines.append(f"  operator remedy (data): \"{neutralise(l['remedy'], REMEDY_CAP)}\"")
    return lines


# ------------------------------------------------------------------ effectiveness


def effectiveness(root: Path) -> dict:
    """Per lesson: dispatches shown it, recurrences after being shown, unwarned occurrences.

    A shown dispatch is an `invocation_started` event whose `lessons_shown` names the lesson;
    each retry and each rollback re-entry is its own dispatch. A failure is attributed to the
    one dispatch of the same run and phase it follows: a recurrence when that dispatch showed
    the lesson, unwarned otherwise. No threshold is applied.
    """
    root = Path(root)
    policy = load_policy(root)
    threshold = threshold_of(policy)
    data = load_store(root) or {"lessons": []}
    by_fp = {l["fingerprint"]: l for l in data["lessons"] if isinstance(l, dict)}
    rows = {l["lesson_id"]: {"shown": 0, "recurrences": 0, "unwarned": 0}
            for l in by_fp.values()}
    counters = {"unreadable_files": 0, "unreadable_lines": 0}
    runs = root / "runs"
    run_dirs = sorted(p for p in runs.iterdir() if p.is_dir() and p.name.startswith("run-")) \
        if runs.is_dir() else []
    for run_dir in run_dirs:
        artifacts, closes = _run_maps(_read_json(run_dir / "state.json", counters))
        events = _read_events(run_dir / "events.jsonl", counters)
        latest: dict = {}
        starts: dict = {}
        for ev in events:
            etype, state_id = ev.get("event_type"), ev.get("state_id")
            details = ev.get("details_ref") if isinstance(ev.get("details_ref"), dict) else {}
            if etype == "invocation_started":
                shown = details.get("lessons_shown")
                shown = set(shown) if isinstance(shown, list) else set()
                latest[state_id] = shown
                starts.setdefault(state_id, []).append((ev.get("timestamp") or "", shown))
                for lid in shown:
                    if lid in rows:
                        rows[lid]["shown"] += 1
            elif etype == "validation_failed":
                artifact = artifacts.get(state_id)
                for cid in details.get("failures") or []:
                    lesson = by_fp.get(f"check:{artifact}:{cid}")
                    if lesson is None:
                        continue
                    hit = lesson["lesson_id"] in latest.get(state_id, set())
                    rows[lesson["lesson_id"]]["recurrences" if hit else "unwarned"] += 1
        ledger = _read_json(run_dir / "recovery-ledger.json", counters)
        for entry in ((ledger or {}).get("entries") or []) if isinstance(ledger, dict) else []:
            if not isinstance(entry, dict) or entry.get("failure_class") != "gate-rejection" \
                    or entry.get("work_type") != "gate":
                continue
            gate = entry.get("state_id")
            bundle = entry.get("evidence_bundle_ref") if isinstance(
                entry.get("evidence_bundle_ref"), dict) else {}
            phase = bundle.get("closes_state") or closes.get(gate)
            lesson = by_fp.get(f"gate:{gate}:{phase}")
            if lesson is None:
                continue
            at = entry.get("detected_at") or ""
            before = [s for t, s in starts.get(phase, []) if t <= at]
            hit = bool(before) and lesson["lesson_id"] in before[-1]
            rows[lesson["lesson_id"]]["recurrences" if hit else "unwarned"] += 1
    out = []
    for fp in sorted(by_fp):
        l = by_fp[fp]
        r = rows[l["lesson_id"]]
        out.append({"lesson_id": l["lesson_id"], "fingerprint": fp, "status": l.get("status"),
                    "eligible": eligible(l, threshold), "shown_dispatches": r["shown"],
                    "recurrences_after_shown": r["recurrences"],
                    "unwarned_occurrences": r["unwarned"],
                    "recurrence_ratio": round(r["recurrences"] / r["shown"], 3)
                    if r["shown"] else None})
    return {"threshold": threshold, "lessons": out,
            "totals": {"shown_dispatches": sum(r["shown_dispatches"] for r in out),
                       "recurrences_after_shown": sum(r["recurrences_after_shown"]
                                                      for r in out),
                       "unwarned_occurrences": sum(r["unwarned_occurrences"] for r in out)}}


# ------------------------------------------------------------------ command line


def _lesson_line(l: dict, threshold: int) -> str:
    key = l.get("check_id") or l.get("gate")
    return (f"{l['lesson_id']}  {l.get('status'):9}  "
            f"{'eligible' if eligible(l, threshold) else 'not-eligible':12}  "
            f"{l.get('kind')}  {key}  on {l.get('artifact_type')}  "
            f"phases={','.join(l.get('phases') or [])}  occurrences={l.get('occurrences')}  "
            f"distinct_runs={l.get('distinct_runs')}  first_seen={l.get('first_seen')}  "
            f"last_seen={l.get('last_seen')}  evidence={len(l.get('evidence') or [])}  "
            f"description={one_line(l.get('check_description'), 100)!r}"
            + (f"  [{l.get('evidence_state')}]" if l.get("evidence_state") != "current"
               else ""))


def _print_lessons(root: Path, only_eligible: bool = False):
    data = load_store(root) or {"lessons": []}
    threshold = threshold_of(load_policy(root))
    shown = [l for l in data["lessons"] if not only_eligible or eligible(l, threshold)]
    print(f"lessons        : {len(shown)}  (eligibility threshold: {threshold} distinct runs)")
    for l in shown:
        print("  " + _lesson_line(l, threshold))


def main(argv=None) -> int:
    try:
        sys.stdout.reconfigure(errors="backslashreplace")
    except (AttributeError, ValueError):
        pass
    ap = argparse.ArgumentParser(
        description="Knowledge loop: harvest recorded run evidence into governed lessons")
    ap.add_argument("--root", default=str(CLAUDE),
                    help="framework directory holding runs/, memory/, and config/ "
                         "(default: the one this script lives in)")
    sub = ap.add_subparsers(dest="cmd", required=True)
    h = sub.add_parser("harvest", help="rebuild lessons from recorded run evidence")
    h.add_argument("--json", action="store_true")
    ls = sub.add_parser("list", help="list the lessons in the store")
    ls.add_argument("--eligible", action="store_true", help="only lessons eligible for recall")
    ls.add_argument("--json", action="store_true")
    for name, text in (("approve", "make a lesson active (eligible at any count)"),
                       ("retire", "retire a lesson; it is kept, never deleted"),
                       ("annotate", "attach an operator remedy to a lesson")):
        g = sub.add_parser(name, help=text)
        g.add_argument("--lesson", required=True, help="lesson ID (L-...) or fingerprint")
        g.add_argument("--actor", required=True, help="who is deciding; never a role alone")
        g.add_argument("--rationale", required=True)
        if name == "annotate":
            g.add_argument("--remedy", required=True,
                           help=f"one line, at most {REMEDY_CAP} printable characters")
    rc = sub.add_parser("recall", help="preview what a dispatch of a phase would be shown")
    rc.add_argument("--phase", required=True)
    rc.add_argument("--artifact", required=True, help="the phase's output artifact type")
    rc.add_argument("--json", action="store_true")
    rp = sub.add_parser("report", help="per-lesson effectiveness: shown, recurred, unwarned")
    rp.add_argument("--json", action="store_true")
    mg = sub.add_parser("migrate", help="move the JSON lesson store into the lesson bundle, "
                                        "removing it only once verified")
    mg.add_argument("--json", action="store_true")
    args = ap.parse_args(argv)
    root = Path(args.root)
    try:
        if args.cmd == "harvest":
            out = harvest(root)
            if args.json:
                print(json.dumps(out, indent=2, sort_keys=True))
                return 0
            print(f"harvest        : {out['status']}  runs scanned {out['runs_scanned']}, "
                  f"occurrences {out['occurrences']}, lessons {out['lessons']} "
                  f"(created {out['created']}, updated {out['updated']}, retained "
                  f"{out['retained']})")
            print(f"skipped        : unresolved {out['unresolved']}, empty failure lists "
                  f"{out['empty_failure_lists']}, non-check IDs {out['skipped_ids']}, "
                  f"unreadable files {out['unreadable_files']}")
            print(f"store          : {BUNDLE_REL}  "
                  f"({'written' if out['store_written'] else 'unchanged'}"
                  + (f", {out['store_digest']}" if out["store_digest"] else "") + ")")
            if bundle_exists(root):
                _print_lessons(root)
            return 0
        if args.cmd == "list":
            if args.json:
                print(json.dumps(load_store(root) or {"schema": STORE_SCHEMA, "lessons": []},
                                 indent=2, sort_keys=True))
                return 0
            _print_lessons(root, args.eligible)
            return 0
        if args.cmd in ("approve", "retire", "annotate"):
            lesson = govern(root, args.cmd, args.lesson, actor=args.actor,
                            rationale=args.rationale, remedy=getattr(args, "remedy", None))
            print(f"{args.cmd:9}: {lesson['lesson_id']} status={lesson['status']} "
                  f"history={len(lesson.get('history') or [])}")
            return 0
        if args.cmd == "recall":
            got = recall(root, args.phase, args.artifact)
            if args.json:
                print(json.dumps(got, indent=2, sort_keys=True))
                return 0
            print(f"recall         : {len(got['lessons'])} shown of {got['matched']} eligible "
                  f"match(es) for {args.phase} -> {args.artifact}")
            for line in render_section(got["lessons"]):
                print(line)
            return 0
        if args.cmd == "migrate":
            out = migrate(root)
            if args.json:
                print(json.dumps(out, indent=2, sort_keys=True))
                return 0
            if out["status"] == "no-legacy-store":
                print(f"migrate        : nothing to do, no {LEGACY_STORE_REL} "
                      f"(bundle {BUNDLE_REL}"
                      + (f", {out['store_digest']})" if out["store_digest"] else " absent)"))
                return 0
            print(f"migrate        : {out['lessons']} lesson(s) verified record for record; "
                  f"{out['documents']} document(s) in {BUNDLE_REL}, {out['store_digest']}")
            print(f"removed        : {LEGACY_STORE_REL}")
            return 0
        if args.cmd == "report":
            rep = effectiveness(root)
            if args.json:
                print(json.dumps(rep, indent=2, sort_keys=True))
                return 0
            t = rep["totals"]
            print(f"effectiveness  : {len(rep['lessons'])} lesson(s); shown dispatches "
                  f"{t['shown_dispatches']}, recurrences after shown "
                  f"{t['recurrences_after_shown']}, unwarned occurrences "
                  f"{t['unwarned_occurrences']} (no pass threshold is applied)")
            for r in rep["lessons"]:
                print(f"  {r['lesson_id']}  {r['status']:9}  shown={r['shown_dispatches']}  "
                      f"recurred={r['recurrences_after_shown']}  "
                      f"unwarned={r['unwarned_occurrences']}  ratio={r['recurrence_ratio']}  "
                      f"{r['fingerprint']}")
            return 0
    except KnowledgeError as exc:
        print(f"KNOWLEDGE FAILURE {exc.error_class}: {exc.detail}", file=sys.stderr)
        return 2
    return 2


if __name__ == "__main__":
    sys.exit(main())
