#!/usr/bin/env python3
"""Open Knowledge Format (OKF) v0.2 bundles: a frontmatter subset, documents, and the §11 check.

An OKF bundle is a directory of Markdown concept documents, each opening with a YAML frontmatter
block, plus the reserved files `index.md` and `log.md` at any level. This module knows nothing
about what a bundle holds. It offers four things, standard library only:

- `dump` writes a mapping in a canonical frontmatter subset; `parse` reads that subset back.
- `split_document` and `join_document` separate and assemble frontmatter and body.
- `parse_document` is the two together.
- `conformance` checks a bundle directory against OKF v0.2 §11.

Frontmatter subset
------------------
Writing is canonical: two-space indentation; bare keys matching `[A-Za-z_][A-Za-z0-9_]*`; every
string double-quoted, with backslash and double quote escaped and every character outside
printable ASCII written as a `\\uXXXX` or `\\UXXXXXXXX` escape, so frontmatter bytes are ASCII and
no value can span a line or open a delimiter; `null`, `true`, `false`, and base-10 integers bare;
floats and every other type refused; lists and mappings in block form, a list of mappings with
its first key on the dash line, and empty collections as `[]` and `{}`. Top-level key order is the
caller's; a nested mapping keeps the caller's order only when its top-level key is named in
`keep_order`, and every other nested mapping is written with sorted keys. Equal input yields
equal bytes.

Reading accepts that form, plus plain scalars (`null`, `~`, or empty as null; `true` and
`false`; integers; anything else a string), single-quoted strings, one-line flow lists and
mappings of scalars, and full-line comments. Anything else is unparseable and raises `OkfError`.

Conformance (§11)
-----------------
Every `.md` file under the bundle, recursively, is checked:

- `OKF-11.1`  a concept document has no parseable frontmatter;
- `OKF-11.2`  a concept document has a missing or empty `type`;
- `OKF-11.3-index`  the root `index.md` frontmatter holds a key other than `okf_version`, a
  non-root `index.md` carries frontmatter, its first non-blank body line is not a `#` heading, or
  a body line is neither a heading, a list entry with a bracketed link, nor blank;
- `OKF-11.3-log`  the first non-blank line of `log.md` is not a `#` title, a `##` heading is not
  a valid ISO date, the dates are not strictly descending, or a list entry precedes the first
  date heading.

The result is `pass`, `fail`, or `absent` (no directory, or no `.md` file in it), with findings
sorted by path, then rule, then detail. It carries no timestamp, so two checks of one bundle give
identical output.
"""

from __future__ import annotations

import re
from datetime import date
from pathlib import Path

OKF_VERSION = "0.2"
DELIMITER = "---"
INDEX_NAME = "index.md"
LOG_NAME = "log.md"
RESERVED = (INDEX_NAME, LOG_NAME)

KEY = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*$")
_KEY_LINE = re.compile(r"^([A-Za-z_][A-Za-z0-9_]*):(?:[ ]+(.*))?$")
_INTEGER = re.compile(r"^[-+]?[0-9]+$")
_HEADING = re.compile(r"^#{1,6}[ ]+\S")
_LINK_ENTRY = re.compile(r"^\s*[-*+][ ]+\[(?:\\.|[^\]\\])*\]\([^)\s]+\)")
_LIST_ENTRY = re.compile(r"^\s*[-*+][ ]+")
_TITLE = re.compile(r"^#[ ]+\S")
_DATE_HEADING = re.compile(r"^##[ ]+(.*?)\s*$")
_PLAIN_REFUSED = tuple("|>&*!%@`")


class OkfError(ValueError):
    """Frontmatter that cannot be written or read within the subset."""


# ------------------------------------------------------------------ writing


def _quote(text: str) -> str:
    out = ['"']
    for ch in text:
        code = ord(ch)
        if ch == "\\":
            out.append("\\\\")
        elif ch == '"':
            out.append('\\"')
        elif 0x20 <= code <= 0x7E:
            out.append(ch)
        elif code <= 0xFFFF:
            out.append(f"\\u{code:04X}")
        else:
            out.append(f"\\U{code:08X}")
    out.append('"')
    return "".join(out)


def _scalar(value) -> str:
    if value is None:
        return "null"
    if isinstance(value, bool):
        return "true" if value else "false"
    if isinstance(value, int):
        return str(value)
    if isinstance(value, str):
        return _quote(value)
    raise OkfError(f"unsupported value type {type(value).__name__}")


def _is_collection(value) -> bool:
    return isinstance(value, (list, dict))


def _check_key(key) -> str:
    if not isinstance(key, str) or not KEY.match(key):
        raise OkfError(f"key {key!r} is not a bare key")
    return key


def _mapping_lines(mapping: dict, indent: int, ordered: bool) -> list:
    lines = []
    pad = " " * indent
    keys = list(mapping) if ordered else sorted(mapping, key=str)
    for key in keys:
        _check_key(key)
        lines.extend(_entry_lines(pad + key + ":", mapping[key], indent))
    return lines


def _entry_lines(head: str, value, indent: int) -> list:
    """`head` is `key:` or `- key:`; the value follows on the line or as a block below it."""
    if _is_collection(value) and value:
        if isinstance(value, dict):
            return [head] + _mapping_lines(value, indent + 2, False)
        return [head] + _list_lines(value, indent + 2)
    if isinstance(value, list):
        return [head + " []"]
    if isinstance(value, dict):
        return [head + " {}"]
    return [head + " " + _scalar(value)]


def _list_lines(items: list, indent: int) -> list:
    lines = []
    pad = " " * indent
    for item in items:
        if isinstance(item, dict) and item:
            keys = sorted(item, key=str)
            first = _check_key(keys[0])
            lines.extend(_entry_lines(pad + "- " + first + ":", item[first], indent + 2))
            for key in keys[1:]:
                _check_key(key)
                lines.extend(_entry_lines(pad + "  " + key + ":", item[key], indent + 2))
        elif isinstance(item, list) and item:
            raise OkfError("a non-empty list inside a list is outside the subset")
        elif isinstance(item, list):
            lines.append(pad + "- []")
        elif isinstance(item, dict):
            lines.append(pad + "- {}")
        else:
            lines.append(pad + "- " + _scalar(item))
    return lines


def dump(mapping: dict, keep_order: tuple = ()) -> str:
    """The canonical frontmatter text for `mapping`, without delimiters, ending in a newline."""
    if not isinstance(mapping, dict):
        raise OkfError("frontmatter must be a mapping")
    lines = []
    for key in mapping:
        _check_key(key)
        value = mapping[key]
        if isinstance(value, dict) and value and key in keep_order:
            lines.append(key + ":")
            lines.extend(_mapping_lines(value, 2, True))
        else:
            lines.extend(_entry_lines(key + ":", value, 0))
    return "".join(line + "\n" for line in lines)


def join_document(mapping: dict, body: str, keep_order: tuple = ()) -> str:
    """A document: the frontmatter between two delimiter lines, then the body."""
    return f"{DELIMITER}\n{dump(mapping, keep_order)}{DELIMITER}\n{body}"


# ------------------------------------------------------------------ reading


def _unquote_double(text: str) -> tuple:
    """(value, rest) for a double-quoted scalar at the start of `text`."""
    simple = {"\\": "\\", '"': '"', "/": "/", "n": "\n", "t": "\t", "r": "\r", "0": "\0",
              "b": "\b", "f": "\f", "e": "\x1b", "a": "\a", "v": "\v", " ": " ",
              "N": "\u0085", "_": " ", "L": " ", "P": " "}
    out, i = [], 1
    while i < len(text):
        ch = text[i]
        if ch == '"':
            return "".join(out), text[i + 1:]
        if ch == "\\":
            if i + 1 >= len(text):
                break
            esc = text[i + 1]
            width = {"x": 2, "u": 4, "U": 8}.get(esc)
            if width:
                digits = text[i + 2:i + 2 + width]
                if len(digits) != width or not re.fullmatch(r"[0-9A-Fa-f]+", digits):
                    raise OkfError(f"bad \\{esc} escape")
                out.append(chr(int(digits, 16)))
                i += 2 + width
                continue
            if esc not in simple:
                raise OkfError(f"unknown escape \\{esc}")
            out.append(simple[esc])
            i += 2
            continue
        out.append(ch)
        i += 1
    raise OkfError("unterminated double-quoted string")


def _unquote_single(text: str) -> tuple:
    out, i = [], 1
    while i < len(text):
        ch = text[i]
        if ch == "'":
            if text[i + 1:i + 2] == "'":
                out.append("'")
                i += 2
                continue
            return "".join(out), text[i + 1:]
        out.append(ch)
        i += 1
    raise OkfError("unterminated single-quoted string")


def _trailing_ok(rest: str) -> None:
    rest = rest.strip()
    if rest and not rest.startswith("#"):
        raise OkfError(f"unexpected text after a quoted scalar: {rest!r}")


def _plain(text: str):
    text = re.sub(r"\s+#.*$", "", text).strip()
    if text in ("", "~", "null", "Null", "NULL"):
        return None
    if text in ("true", "True", "TRUE"):
        return True
    if text in ("false", "False", "FALSE"):
        return False
    if _INTEGER.match(text):
        return int(text, 10)
    if text.startswith(_PLAIN_REFUSED) or text.startswith(("- ", "[", "{", "]", "}")):
        raise OkfError(f"plain scalar {text!r} is outside the subset")
    if ": " in text or text.endswith(":"):
        raise OkfError(f"plain scalar {text!r} looks like a nested mapping")
    return text


def _split_flow(inner: str) -> list:
    parts, buf, quote, i = [], [], None, 0
    while i < len(inner):
        ch = inner[i]
        if quote:
            buf.append(ch)
            if ch == "\\" and quote == '"' and i + 1 < len(inner):
                buf.append(inner[i + 1])
                i += 2
                continue
            if ch == quote:
                quote = None
        elif ch in "\"'":
            quote = ch
            buf.append(ch)
        elif ch in "[]{}":
            raise OkfError("nested flow collections are outside the subset")
        elif ch == ",":
            parts.append("".join(buf))
            buf = []
        else:
            buf.append(ch)
        i += 1
    if quote:
        raise OkfError("unterminated string in a flow collection")
    last = "".join(buf)
    if last.strip() or parts:
        parts.append(last)
    return [p.strip() for p in parts]


def _flow_scalar(text: str):
    if text.startswith('"'):
        value, rest = _unquote_double(text)
        _trailing_ok(rest)
        return value
    if text.startswith("'"):
        value, rest = _unquote_single(text)
        _trailing_ok(rest)
        return value
    if not text:
        raise OkfError("empty item in a flow collection")
    return _plain(text)


def _value(text: str):
    """A scalar or one-line flow collection, from the text after `key: ` or `- `."""
    text = text.strip()
    if text.startswith('"'):
        value, rest = _unquote_double(text)
        _trailing_ok(rest)
        return value
    if text.startswith("'"):
        value, rest = _unquote_single(text)
        _trailing_ok(rest)
        return value
    if text.startswith("["):
        body, rest = _flow_body(text, "[", "]")
        return [_flow_scalar(p) for p in _split_flow(body)]
    if text.startswith("{"):
        body, rest = _flow_body(text, "{", "}")
        out = {}
        for part in _split_flow(body):
            m = re.match(r"^([A-Za-z_][A-Za-z0-9_]*)\s*:(?:\s+(.*))?$", part)
            if not m:
                raise OkfError(f"flow mapping entry {part!r} is outside the subset")
            if m.group(1) in out:
                raise OkfError(f"duplicate key {m.group(1)!r}")
            out[m.group(1)] = _flow_scalar((m.group(2) or "").strip()) \
                if (m.group(2) or "").strip() else None
        return out
    return _plain(text)


def _flow_body(text: str, opener: str, closer: str) -> tuple:
    quote, i = None, 1
    while i < len(text):
        ch = text[i]
        if quote:
            if ch == "\\" and quote == '"':
                i += 2
                continue
            if ch == quote:
                quote = None
        elif ch in "\"'":
            quote = ch
        elif ch == closer:
            _trailing_ok(text[i + 1:])
            return text[1:i], text[i + 1:]
        i += 1
    raise OkfError(f"unterminated flow collection {opener}...{closer}")


def _lines(text: str) -> list:
    out = []
    for raw in text.split("\n"):
        line = raw[:-1] if raw.endswith("\r") else raw
        stripped = line.lstrip(" ")
        if not stripped or stripped.startswith("#"):
            continue
        if stripped.startswith("\t") or "\t" in line[: len(line) - len(stripped)]:
            raise OkfError("tab indentation is outside the subset")
        out.append([len(line) - len(stripped), stripped.rstrip()])
    return out


def _block(lines: list, pos: int, indent: int) -> tuple:
    if lines[pos][1] == "-" or lines[pos][1].startswith("- "):
        return _sequence(lines, pos, indent)
    return _mapping(lines, pos, indent)


def _nested(lines: list, pos: int, indent: int, allow_same_indent_list: bool) -> tuple:
    """The block value after `key:` or `-` with nothing on the line, or null."""
    if pos < len(lines):
        n, text = lines[pos]
        if n > indent:
            return _block(lines, pos, n)
        if allow_same_indent_list and n == indent and (text == "-" or text.startswith("- ")):
            return _sequence(lines, pos, n)
    return None, pos


def _mapping(lines: list, pos: int, indent: int) -> tuple:
    out = {}
    while pos < len(lines):
        n, text = lines[pos]
        if n < indent:
            break
        if n > indent:
            raise OkfError(f"unexpected indentation at {text!r}")
        m = _KEY_LINE.match(text)
        if not m:
            raise OkfError(f"line {text!r} is not a bare key entry")
        key = m.group(1)
        if key in out:
            raise OkfError(f"duplicate key {key!r}")
        rest = m.group(2)
        pos += 1
        if rest is None or not rest.strip() or rest.strip().startswith("#"):
            out[key], pos = _nested(lines, pos, indent, True)
        else:
            out[key] = _value(rest)
    return out, pos


def _sequence(lines: list, pos: int, indent: int) -> tuple:
    out = []
    while pos < len(lines):
        n, text = lines[pos]
        if n < indent:
            break
        if n > indent:
            raise OkfError(f"unexpected indentation at {text!r}")
        if not (text == "-" or text.startswith("- ")):
            break
        rest = text[1:]
        body = rest.lstrip(" ")
        if not body:
            value, pos = _nested(lines, pos + 1, indent, False)
            out.append(value)
            continue
        column = indent + 1 + (len(rest) - len(body))
        if _KEY_LINE.match(body):
            lines[pos] = [column, body]
            value, pos = _mapping(lines, pos, column)
            out.append(value)
            continue
        if body == "-" or body.startswith("- "):
            raise OkfError("a list inside a list is outside the subset")
        out.append(_value(body))
        pos += 1
    return out, pos


def parse(text: str) -> dict:
    """The mapping a frontmatter text (without delimiters) holds; raises OkfError."""
    lines = _lines(text)
    if not lines:
        return {}
    if lines[0][0] != 0:
        raise OkfError("the frontmatter does not start at column zero")
    value, pos = _mapping(lines, 0, 0)
    if pos != len(lines):
        raise OkfError(f"unexpected line {lines[pos][1]!r}")
    return value


def split_document(text: str) -> tuple:
    """(frontmatter text or None, body). None when the document opens with no delimiter line."""
    if text.startswith("﻿"):
        text = text[1:]
    lines = text.split("\n")
    if lines[0].rstrip("\r") != DELIMITER:
        return None, text
    for i in range(1, len(lines)):
        if lines[i].rstrip("\r") == DELIMITER:
            return "\n".join(lines[1:i]), "\n".join(lines[i + 1:])
    raise OkfError("the frontmatter is not closed by a delimiter line")


def parse_document(text: str) -> tuple:
    """(frontmatter mapping or None, body); raises OkfError on unparseable frontmatter."""
    front, body = split_document(text)
    if front is None:
        return None, body
    return parse(front), body


# ------------------------------------------------------------------ conformance


def _finding(path: str, rule: str, detail: str) -> dict:
    return {"path": path, "rule": rule, "detail": detail}


def _check_concept(rel: str, text: str, findings: list) -> None:
    try:
        front, _ = parse_document(text)
    except OkfError as exc:
        findings.append(_finding(rel, "OKF-11.1", f"frontmatter is not parseable: {exc}"))
        return
    if front is None:
        findings.append(_finding(rel, "OKF-11.1", "the document has no frontmatter"))
        return
    kind = front.get("type")
    if not isinstance(kind, str) or not kind.strip():
        findings.append(_finding(rel, "OKF-11.2", "type is missing or empty"))


def _check_index(rel: str, text: str, is_root: bool, findings: list) -> None:
    rule = "OKF-11.3-index"
    try:
        front, body = parse_document(text)
    except OkfError as exc:
        findings.append(_finding(rel, rule, f"frontmatter is not parseable: {exc}"))
        return
    if front is not None:
        if not is_root:
            findings.append(_finding(rel, rule, "a non-root index carries frontmatter"))
        else:
            extra = sorted(k for k in front if k != "okf_version")
            if extra:
                findings.append(_finding(rel, rule, "the root index frontmatter holds keys "
                                                    f"other than okf_version: {extra}"))
    seen_content = False
    for n, line in enumerate(body.split("\n"), 1):
        line = line.rstrip("\r")
        if not line.strip():
            continue
        if not seen_content:
            seen_content = True
            if not _HEADING.match(line):
                findings.append(_finding(rel, rule, f"body line {n} is the first non-blank line "
                                                    "and is not a # heading"))
                continue
        if _HEADING.match(line) or _LINK_ENTRY.match(line):
            continue
        findings.append(_finding(rel, rule, f"body line {n} is neither a heading, a list "
                                            "entry with a bracketed link, nor blank"))


def _iso_date(text: str):
    if not re.fullmatch(r"\d{4}-\d{2}-\d{2}", text):
        return None
    try:
        return date.fromisoformat(text)
    except ValueError:
        return None


def _check_log(rel: str, text: str, findings: list) -> None:
    rule = "OKF-11.3-log"
    if text.startswith("﻿"):
        text = text[1:]
    seen_title, previous = False, None
    for n, line in enumerate(text.split("\n"), 1):
        line = line.rstrip("\r")
        if not line.strip():
            continue
        if not seen_title:
            seen_title = True
            if not _TITLE.match(line):
                findings.append(_finding(rel, rule, f"line {n} is the first non-blank line and "
                                                    "is not a # title"))
            continue
        m = _DATE_HEADING.match(line)
        if m:
            day = _iso_date(m.group(1))
            if day is None:
                findings.append(_finding(rel, rule, f"line {n}: heading {m.group(1)!r} is not "
                                                    "an ISO date"))
                continue
            if previous is not None and day >= previous:
                findings.append(_finding(rel, rule, f"line {n}: date {m.group(1)} is not "
                                                    "older than the date above it"))
            previous = day
            continue
        if _LIST_ENTRY.match(line) and previous is None:
            findings.append(_finding(rel, rule, f"line {n}: a list entry precedes the first "
                                                "date heading"))


def conformance(bundle, label: str | None = None) -> dict:
    """The §11 verdict for a bundle directory: result, counts, and sorted findings."""
    bundle = Path(bundle)
    out = {"bundle": label if label is not None else bundle.as_posix(), "result": "absent",
           "documents": 0, "reserved": 0, "findings": []}
    if not bundle.is_dir():
        return out
    paths = sorted(p for p in bundle.rglob("*.md") if p.is_file())
    if not paths:
        return out
    findings = []
    for path in paths:
        rel = path.relative_to(bundle).as_posix()
        try:
            text = path.read_bytes().decode("utf-8")
        except (OSError, UnicodeDecodeError) as exc:
            findings.append(_finding(rel, "OKF-11.1", f"unreadable: {type(exc).__name__}"))
            if path.name not in RESERVED:
                out["documents"] += 1
            else:
                out["reserved"] += 1
            continue
        if path.name == INDEX_NAME:
            out["reserved"] += 1
            _check_index(rel, text, path.parent == bundle, findings)
        elif path.name == LOG_NAME:
            out["reserved"] += 1
            _check_log(rel, text, findings)
        else:
            out["documents"] += 1
            _check_concept(rel, text, findings)
    out["findings"] = sorted(findings, key=lambda f: (f["path"], f["rule"], f["detail"]))
    out["result"] = "fail" if findings else "pass"
    return out
