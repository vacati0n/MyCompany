#!/usr/bin/env python3
"""OKF v0.2 §11 conformance check for a knowledge bundle.

Checks every `.md` file under a bundle directory against the conformance rules `okf.conformance`
implements (OKF-11.1, OKF-11.2, OKF-11.3-index, OKF-11.3-log) and prints the verdict with its
findings. With no path it checks the lesson bundle, `memory/lessons` under the framework root.

The check is advisory (`validation/framework-release-checklist.md`, item FR-13): it reads the
bundle and never writes. Its output carries no timestamp, so two checks of one bundle print
identical text, and it closes with the `N/N checks passed -- <verdict>` summary line every
verifier prints: one check per file, with the verdict CONFORMANT, NON-CONFORMANT or ABSENT.

Usage:
    python .claude/runtime/verify_okf_bundle.py
    python .claude/runtime/verify_okf_bundle.py path/to/bundle
    python .claude/runtime/verify_okf_bundle.py --json

Exit code 0 when the bundle conforms or is absent (no directory, or no `.md` file in it), 1 when
any finding is recorded, 2 when an explicitly named path does not exist.

Dependencies: the standard library and `okf`.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import okf  # noqa: E402

CLAUDE = Path(__file__).resolve().parent.parent
DEFAULT_BUNDLE_REL = "memory/lessons"


def render(verdict: dict) -> str:
    lines = [f"OKF v{okf.OKF_VERSION} conformance: {verdict['bundle']}",
             f"result: {verdict['result']}",
             f"documents: {verdict['documents']}",
             f"reserved files: {verdict['reserved']}",
             f"findings: {len(verdict['findings'])}"]
    for f in verdict["findings"]:
        lines.append(f"  {f['rule']}  {f['path']}  {f['detail']}")
    # The closing summary every verifier prints: one check per file, passed when the file has
    # no finding.
    total = verdict["documents"] + verdict["reserved"]
    failed = len({f["path"] for f in verdict["findings"]})
    label = {"pass": "CONFORMANT", "fail": "NON-CONFORMANT"}.get(verdict["result"], "ABSENT")
    lines.append("")
    lines.append(f"{total - failed}/{total} checks passed -- {label}")
    return "\n".join(lines) + "\n"


def main(argv: list | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("path", nargs="?", help="bundle directory (default: memory/lessons)")
    parser.add_argument("--json", action="store_true", help="print the verdict as JSON")
    args = parser.parse_args(argv)
    if args.path is None:
        bundle, label = CLAUDE / DEFAULT_BUNDLE_REL, DEFAULT_BUNDLE_REL
    else:
        bundle, label = Path(args.path), Path(args.path).as_posix()
        if not bundle.exists():
            print(f"error: no such path: {label}", file=sys.stderr)
            return 2
    verdict = okf.conformance(bundle, label)
    if args.json:
        sys.stdout.write(json.dumps(verdict, indent=2, sort_keys=True) + "\n")
    else:
        sys.stdout.write(render(verdict))
    return 1 if verdict["result"] == "fail" else 0


if __name__ == "__main__":
    sys.exit(main())
