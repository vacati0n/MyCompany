#!/usr/bin/env python3
"""Manifest and agent-contract shape verification.

Answers two questions no other verify_*.py script covers, using the same resolvers the
runtime itself uses so this report cannot agree with a runtime that would disagree:

  M1  Is every declared `authorityScope.repositoryWrites.scope`/`excluded`, on a manifest
      that declares `allowed: true`, a YAML list of glob patterns rather than a prose string?
      A string there is silently exploded character-by-character by Python's `list(...)` at
      dispatch time (see `framework_runtime._glob_list`), corrupting every dispatch envelope's
      `permitted_writes` for that agent without raising anywhere.

  M2  Does every contract clause a real run traced a first-attempt validation failure to
      still carry its pointer to a concrete worked example? See `REQUIRED_EXAMPLE_POINTERS`
      below -- this is deliberately scoped to the specific clauses ON-165 exercised, not a
      blanket rule over every agent's docs, since which clauses need a worked example is a
      judgment call this script cannot make on its own.

Usage:
    python .claude/runtime/verify_manifests.py
    python .claude/runtime/verify_manifests.py --json-out manifests.json

Exit code 0 when every check passes, 1 otherwise.
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

import framework_runtime as fr  # noqa: E402

CLAUDE = fr.CLAUDE


class Result:
    def __init__(self):
        self.checks = []

    def add(self, cid: str, check: str, passed: bool, counts: dict, detail: str,
            rows: list | None = None):
        self.checks.append({
            "id": cid, "check": check, "result": "PASS" if passed else "FAIL",
            "counts": counts, "detail": detail, "rows": rows or [],
        })

    @property
    def passed(self) -> bool:
        return all(c["result"] == "PASS" for c in self.checks)


def agent_manifest_paths() -> list:
    """Active agents whose registry record resolves to an actual manifest file.

    A retired role's record still resolves (to `agents/retired-roles.md`, a shared prose
    record rather than a manifest), so this is filtered the same way
    `verify_registry_coverage.active()` filters every other registry read: by `status ==
    "active"`, and additionally requiring the resolved path to actually be a manifest file
    rather than incidentally existing.
    """
    reg = fr.load_yaml("registry/agents.yaml")
    out = []
    for rec in reg.get("records") or []:
        if rec.get("status") != "active":
            continue
        rel = rec.get("specificationPath")
        if rel and rel.endswith("manifest.yaml") and (CLAUDE / rel).exists():
            out.append((rec["identifier"], rel))
    return out


def repository_writes_shape_ok(repo_writes: dict) -> bool:
    """Whether an `allowed: true` repositoryWrites block is machine-actionable.

    `scope` must be a non-empty YAML list of glob patterns -- a prose string
    would be exploded character-by-character by `list(...)` at dispatch time,
    and an absent scope on an allowed grant declares a permission with no
    patterns behind it, which is a bug shape of its own. `excluded` may be
    absent, but present it must be a list for the same reason.
    """
    scope = repo_writes.get("scope")
    excluded = repo_writes.get("excluded")
    scope_ok = isinstance(scope, list) and len(scope) > 0
    excluded_ok = excluded is None or isinstance(excluded, list)
    return scope_ok and excluded_ok


def m1_repository_writes_are_lists(res: Result):
    rows = []
    bad = 0
    for agent_id, manifest_rel in agent_manifest_paths():
        manifest = fr.load_yaml(manifest_rel)
        repo_writes = ((manifest.get("authorityScope") or {}).get("repositoryWrites")) or {}
        if not repo_writes.get("allowed"):
            continue
        ok = repository_writes_shape_ok(repo_writes)
        if not ok:
            bad += 1
        rows.append({
            "agent": agent_id,
            "manifest": manifest_rel,
            "scope_type": type(repo_writes.get("scope")).__name__,
            "excluded_type": type(repo_writes.get("excluded")).__name__,
            "result": "PASS" if ok else "FAIL",
        })
    res.add("M1",
            "repositoryWrites.scope/excluded are YAML lists on every allowed:true manifest",
            bad == 0,
            {"checked": len(rows), "bad": bad},
            f"{len(rows)} agent(s) declare repositoryWrites.allowed: true; {bad} declare a "
            f"non-list scope or excluded value" if rows else
            "no agent declares repositoryWrites.allowed: true",
            rows)


# Contract clauses a real run showed were foreseeable from the agent's own contract text, but
# carried no pointer into a concrete example -- one entry per (agent, substring the clause's
# surrounding prose must contain, example label the clause must name). Scoped to exactly the
# clauses ON-165 traced a first-attempt validation failure to, not a blanket rule over every
# agent's docs: an agent added here is a clause this framework already knows an agent gets
# wrong on a first attempt, and a docs regression on it is worth catching by name.
REQUIRED_EXAMPLE_POINTERS = [
    ("dev-1-implement",
     "An identifier referenced anywhere in the artifact is defined in its declaring section.",
     "N7"),
    ("dev-1-implement",
     "a `partially-verified` or `unverified` claim names at least one.",
     "N8"),
    ("orchestrator",
     "must carry a `Gate Decision` of `approved` or `rejected`",
     "completion without a gate decision"),
]


def m2_examples_cross_referenced(res: Result):
    """Every clause this run traced a first-attempt failure to still names its example.

    Each entry in `REQUIRED_EXAMPLE_POINTERS` names a clause by a substring of its
    surrounding prose in `output.md`, and the example label that clause's fix added a pointer
    to. This fails if the clause's prose has drifted away from that substring (the pointer
    would then be orphaned, sitting near text that no longer says what it did) or if the
    example label it names is missing from the same file, in either `examples.md` or the
    pointer sentence in `output.md` itself.
    """
    rows = []
    bad = 0
    for agent_id, anchor, example_label in REQUIRED_EXAMPLE_POINTERS:
        base = CLAUDE / "agents" / agent_id
        output_md = base / "output.md"
        examples_md = base / "examples.md"
        output_text = output_md.read_text(encoding="utf-8") if output_md.exists() else ""
        examples_text = examples_md.read_text(encoding="utf-8") if examples_md.exists() else ""
        # Markdown prose is hard-wrapped, so anchors are matched over whitespace-normalized
        # text: an anchor spanning a line break is still the same sentence.
        flat = " ".join(output_text.split())
        anchor_present = anchor in flat
        pointer_present = "examples.md" in flat and example_label in flat
        example_present = example_label in " ".join(examples_text.split())
        ok = anchor_present and pointer_present and example_present
        if not ok:
            bad += 1
        rows.append({
            "agent": agent_id,
            "example_label": example_label,
            "anchor_present": anchor_present,
            "pointer_present_in_output_md": pointer_present,
            "example_present_in_examples_md": example_present,
            "result": "PASS" if ok else "FAIL",
        })
    res.add("M2",
            "a clause traced to a first-attempt failure still points at its worked example",
            bad == 0,
            {"checked": len(rows), "bad": bad},
            f"{len(rows)} tracked clause(s); {bad} missing their anchor, their pointer, or "
            f"their example",
            rows)


def run_checks() -> Result:
    res = Result()
    m1_repository_writes_are_lists(res)
    m2_examples_cross_referenced(res)
    return res


def print_report(res: Result):
    print("Manifest & Contract Shape Verification")
    print("-" * 100)
    for c in res.checks:
        print(f"[{c['result']}] {c['id']:<4} {c['check']}")
        print(f"        {c['detail']}")
        print(f"        counts: {json.dumps(c['counts'])}")
    print("-" * 100)
    n_pass = sum(1 for c in res.checks if c["result"] == "PASS")
    print(f"{n_pass}/{len(res.checks)} checks passed -- "
          f"{'CONFORMS' if res.passed else 'NON-CONFORMANT'}")


def main() -> int:
    ap = argparse.ArgumentParser(description="Verify manifest and agent-contract shape")
    ap.add_argument("--json-out")
    a = ap.parse_args()

    res = run_checks()
    print_report(res)

    if a.json_out:
        Path(a.json_out).write_text(json.dumps({
            "verdict": "CONFORMS" if res.passed else "NON-CONFORMANT",
            "checks": res.checks,
        }, indent=2), encoding="utf-8")
        print(f"\nwrote {a.json_out}")
    return 0 if res.passed else 1


if __name__ == "__main__":
    sys.exit(main())
