#!/usr/bin/env python3
"""Validation Engine: release-note.md conformance.

Mechanically checks a produced `release-note.md` against the contract set that governs it:

  - `.claude/templates/release-note.md`      rendered form, section and field labels
  - `.claude/agents/documentation.md`    behavioural contract: outputs, decision rules,
                                             constraints
  - `.claude/context/release-context.md`     the release facts a note must not contradict

Scope boundary: `R1` to `R9` are the rules in the agent contract that are decidable by
inspecting the artifact and the invocation envelope. Whether the note describes what actually
shipped is not decidable from the note alone; what is decidable is internal consistency -- a
declared contract change carries a compatibility statement, a rolled-back release carries a
recorded issue, and the version is one fact rather than two -- and whether the release position
is one the evidence and the phase can yield.

The release position is keyed on the phase's communication basis (Stage 8 of
`agents/documentation/reasoning.md`). Only the `post-release` basis reads a deployment
outcome, off a supplied deployment status; the other three publish a position that asserts no
merge, release, or deployment fact. `R8` decides the position against the evidence types the
note lists in `sourceInputs`; `R9` decides it against the phase the invocation envelope names,
and is declared not-machine-checkable only when no envelope is supplied. Withheld is never a
passing verdict.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import artifact_contract as ac  # noqa: E402
from artifact_lib import Check, strip_md  # noqa: E402

TEMPLATE = "templates/release-note.md"
CONTRACT_REF = "agents/documentation.md"

STAGE8_REF = "agents/documentation/reasoning.md#stage-8--determine-the-release-position"

# The contract version that introduced the basis-keyed release position. A note an earlier
# version produced was read off the superseded deployment-only table; `verify_validators`
# reads this to tell such a note apart, and this validator never exempts one.
CURRENT_CONTRACT_VERSION = (1, 2, 0)

# Phase -> communication basis, as the output contract's Deliverable table and the agent
# contract's Workflow Participation table state it. Phase identifiers are unique across the
# five workflows, so the envelope's `state_id` names the basis.
PHASE_BASIS = {
    "documentation-and-release-handoff": "release-handoff",
    "publication": "findings",
    "findings-publication": "findings",
    "documentation-impact": "documentation-delta",
    "communication-and-post-release": "post-release",
}

# Basis -> the release positions Stage 8 can yield for it when its evidence is present.
VERDICT_OF_BASIS = {
    "release-handoff": ("handed-off",),
    "findings": ("findings-published",),
    "documentation-delta": ("delta-published",),
    "post-release": ("released", "partial", "rolled-back"),
}
VERDICTS = ("released", "partial", "rolled-back", "handed-off", "findings-published",
            "delta-published")

# Verdict -> the `sourceInputs` types, any one of which is the evidence that position needs.
# `validation-report` sits beside `verification-report` because it is the identifier the
# agent's manifest accepts for the same account.
VERDICT_EVIDENCE = {
    "released": ("deployment-status",),
    "partial": ("deployment-status",),
    "rolled-back": ("deployment-status",),
    "handed-off": ("verification-report", "validation-report"),
    "findings-published": ("technical-recommendation",),
    "delta-published": ("verification-report", "validation-report"),
}
ISSUE_TABLE = ("ID", "Issue", "Impact", "Workaround", "Tracking")

CONTRACT = ac.ArtifactContract(
    artifact="release-note.md",
    producers=("documentation",),
    metadata_key="releaseNote",
    metadata_fields=("releaseId", "version", "sourceInputs", "producedBy", "agentVersion",
                     "schemaVersion", "status", "releaseVerdict", "inputDigest",
                     "contextDigest"),
    statuses=ac.STATUSES_COMPLETE,
    template_ref=TEMPLATE,
    contract_refs=(TEMPLATE, CONTRACT_REF),
    id_prefixes={"K": "Known Issues"},
    sections=(
        ac.Section("Metadata", fields=(
            ac.Field("Version"),
            ac.Field("Release date and time"),
            ac.Field("Environment"),
            ac.Field("Release owner"),
        )),
        ac.Section("Highlights", fields=(
            ac.Field("Feature additions"),
            ac.Field("Bug fixes"),
            ac.Field("Improvements"),
        )),
        ac.Section("Technical Changes and Compatibility", fields=(
            ac.Field("API or contract changes"),
            ac.Field("Database or migration impact"),
            ac.Field("Configuration changes"),
            ac.Field("Backward compatibility notes"),
        )),
        ac.Section("Operational Notes", fields=(
            ac.Field("Deployment considerations"),
            ac.Field("Monitoring and alerts"),
            ac.Field("Rollback criteria", min_words=3),
        )),
        ac.Section("Validation Summary", fields=(
            ac.Field("Test status", min_words=3),
            ac.Field("Known risk acceptance"),
            ac.Field("Post-release checks"),
        )),
        ac.Section("Known Issues", table=ac.Table(ISSUE_TABLE, min_rows=1)),
        ac.Section("Communication", fields=(
            ac.Field("Stakeholders notified"),
            ac.Field("Support handoff notes"),
        )),
    ),
    obligations=(
        ("N1", CONTRACT_REF + "#responsibilities",
         "The note describes the behaviour that was actually delivered"),
        ("N2", CONTRACT_REF + "#responsibilities",
         "Stale content from the prior release has been removed rather than carried forward"),
        ("N3", CONTRACT_REF + "#constraints",
         "Every user-visible change is represented, and none is overstated"),
    ),
)


def source_input_types(meta: dict):
    """The `type` of every `sourceInputs` entry, or None when the list is absent or malformed."""
    entries = meta.get("sourceInputs")
    if not isinstance(entries, list) or not entries:
        return None
    types = []
    for entry in entries:
        if not isinstance(entry, dict) or not str(entry.get("type") or "").strip():
            return None
        types.append(str(entry["type"]).strip().lower())
    return types


def semantic_checks(rep, contract, envelope: dict | None = None):
    """The release-specific rules of the documentation contract.

    `envelope` is the invocation envelope, when one is supplied. Only `R9` reads it.
    """
    meta = getattr(rep, "meta", {}) or {}
    body_of = getattr(rep, "body_of", {}) or {}

    def add(cid, ref, severity, description, result, detail=""):
        rep.checks.append(Check(cid, ref, severity, description, result, detail))

    verdict = str(meta.get("releaseVerdict") or "").strip().lower()
    add("R1", TEMPLATE, "Blocking",
        f"releaseVerdict is one of {', '.join(VERDICTS)}",
        "pass" if verdict in VERDICTS else "fail", f"releaseVerdict={verdict!r}")

    # R2 -- the version is one fact. A note whose header and provenance disagree cannot be
    # cited by the closure package that consumes it.
    section_version = strip_md(
        ac.parse_field_bullets(body_of.get("Metadata", "")).get("version", ""))
    meta_version = str(meta.get("version") or "").strip()
    add("R2", TEMPLATE, "Blocking",
        "The version in the metadata block and in the Metadata section are the same",
        "pass" if meta_version and meta_version == section_version else "fail",
        f"metadata={meta_version!r} section={section_version!r}")

    tech = ac.parse_field_bullets(body_of.get("Technical Changes and Compatibility", ""))
    api_change = tech.get("api or contract changes", "")
    compat = tech.get("backward compatibility notes", "")
    declares_change = bool(api_change) and not ac.is_none_marker(api_change)
    states_compat = bool(compat) and not ac.is_none_marker(compat)
    add("R3", CONTRACT_REF + "#responsibilities", "Blocking",
        "A declared contract change carries a backward compatibility statement",
        "pass" if not declares_change or states_compat else "fail",
        f"API or contract changes declared as {api_change!r} with compatibility notes "
        f"{compat!r}" if declares_change and not states_compat
        else ("contract change declared and compatibility stated" if declares_change
              else "no contract change declared"))

    # R4 -- a release that did not fully land has something to say about why.
    issues_body = body_of.get("Known Issues", "")
    _h, rows = ac.find_table(issues_body, ISSUE_TABLE)
    has_issue = bool(rows)
    needs_issue = verdict in ("partial", "rolled-back")
    add("R4", CONTRACT_REF + "#responsibilities", "Blocking",
        "A partial or rolled-back release records at least one known issue",
        "pass" if not needs_issue or has_issue else "fail",
        f"releaseVerdict={verdict!r} with an empty Known Issues section" if needs_issue
        and not has_issue else f"releaseVerdict={verdict!r}, {len(rows)} known issue(s)")

    # R5 -- a rollback is an operational event, so the rollback criteria cannot be silent.
    rollback = ac.parse_field_bullets(body_of.get("Operational Notes", "")).get(
        "rollback criteria", "")
    ok = verdict != "rolled-back" or (bool(rollback) and not ac.is_none_marker(rollback))
    add("R5", CONTRACT_REF + "#responsibilities", "Blocking",
        "A rolled-back release states the criteria the rollback was taken under",
        "pass" if ok else "fail",
        f"rollback criteria={rollback!r} for a rolled-back release" if not ok
        else "rollback criteria stated")

    # R8 -- the position is read off evidence, not chosen. A deployment outcome needs the
    # deployment status; each non-deploying position needs the account its basis publishes.
    types = source_input_types(meta)
    needed = VERDICT_EVIDENCE.get(verdict)
    if types is None:
        ok, detail = False, "sourceInputs is absent or an entry carries no type"
    elif needed is None:
        ok, detail = False, f"no evidence rule for releaseVerdict={verdict!r}"
    else:
        ok = any(t in needed for t in types)
        detail = (f"releaseVerdict={verdict!r} needs a sourceInputs entry of type "
                  f"{' or '.join(needed)}; listed: {sorted(set(types))}")
    add("R8", STAGE8_REF, "Blocking",
        "The release position is supported by the evidence type it is read off",
        "pass" if ok else "fail", detail)

    # R9 -- the position belongs to the phase. The envelope names the phase; without one the
    # rule is declared rather than guessed, as the digest cross-check is.
    basis = None
    if envelope is None:
        add("R9", STAGE8_REF, "Advisory",
            "The release position is one the phase's communication basis yields",
            "not-machine-checkable", "no envelope supplied")
    else:
        state_id = envelope.get("state_id")
        basis = PHASE_BASIS.get(state_id)
        if basis is None:
            ok, detail = False, (f"envelope state_id={state_id!r} is not a phase that "
                                 f"publishes release-note.md")
        else:
            allowed = VERDICT_OF_BASIS[basis]
            ok = verdict in allowed
            detail = (f"phase {state_id!r} has basis {basis!r}, which yields "
                      f"{', '.join(allowed)}; releaseVerdict={verdict!r}")
        add("R9", STAGE8_REF, "Blocking",
            "The release position is one the phase's communication basis yields",
            "pass" if ok else "fail", detail)

    # R6 -- a note is stakeholder communication, so the audience is named.
    comms = ac.parse_field_bullets(body_of.get("Communication", ""))
    notified = comms.get("stakeholders notified", "")
    add("R6", CONTRACT_REF + "#purpose", "Correctable",
        "The note names the stakeholders it was communicated to",
        "pass" if notified and not ac.is_none_marker(notified) else "fail",
        f"stakeholders notified={notified!r}")

    rep.counts["releaseVerdict"] = verdict
    rep.counts["communicationBasis"] = basis
    rep.counts["knownIssues"] = len(rows)
    rep.counts["version"] = meta_version or None
    # A version string is provenance, so a note that carries one that is not a version at all
    # is recorded rather than accepted silently.
    if meta_version and not re.match(r"^v?\d+(\.\d+)*([-+][0-9A-Za-z.\-]+)?$", meta_version):
        add("R7", TEMPLATE, "Correctable", "The version is a recognisable version string",
            "fail", f"version={meta_version!r}")
    else:
        add("R7", TEMPLATE, "Correctable", "The version is a recognisable version string",
            "pass", f"version={meta_version!r}")
    return rep


def validate(artifact_path, envelope: dict | None = None):
    rep = ac.run_contract(CONTRACT, Path(artifact_path), envelope)
    return semantic_checks(rep, CONTRACT, envelope)


def main(argv=None):
    # The shared command line hands its `extra` hook the report and the contract only, so the
    # envelope `R9` reads is taken from the same `--envelope` argument here, leaving the hook
    # every other validator uses unchanged.
    import argparse
    import json

    ap = argparse.ArgumentParser(add_help=False)
    ap.add_argument("--envelope")
    known, _rest = ap.parse_known_args(argv)
    env = (json.loads(Path(known.envelope).read_text(encoding="utf-8"))
           if known.envelope else None)
    return ac.cli(CONTRACT, extra=lambda rep, contract: semantic_checks(rep, contract, env),
                  argv=argv)


if __name__ == "__main__":
    sys.exit(main())
