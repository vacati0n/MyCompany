#!/usr/bin/env python3
"""Executable proof of Validation Engine coverage.

Two questions, asked of every artifact type the framework emits.

**Is it covered?** Every workflow Phase Model names an Output Artifact per phase. Wherever that
column names a file, the Validation Engine must have a registered validator for it, or the
runtime has no way to decide whether what came back conforms -- and `G1-CAPABILITY` blocks the
phase. `V1` reads the Phase Models and reports coverage per artifact, with the phases each one
serves.

`V1` asks that question of every artifact the column names, which leaves a row naming none
outside the question entirely: it contributes nothing to collect, so `V1` passes without ever
examining it, and the phase blocks at run time instead. `V5` asks the same question of every
row, so an Output Artifact the Validation Engine cannot decide is caught at verification time.
`V6` covers the other half of the same gap: the output-contract reader in
`framework_runtime.declared_output_artifact` and the collection here read one cell under two
rules that agree on only one of the three shapes a cell can take, and that agreement was
asserted in a source comment rather than tested. `V6` tests it, in all three directions.

**Does the validator actually decide?** A validator that accepts everything and a validator that
rejects everything both report a result, and neither is a decision. So each registered validator
is run twice: once over a conforming artifact, which must be accepted, and once over the same
artifact with one declared mutation applied, which must be rejected *by the named check*. A
mutation that trips some other check would mean the validator noticed by accident.

Conforming instances come from committed runs wherever one exists, because an artifact the
runtime has already accepted in a real run outranks a fixture. `scope-definition.md`,
`execution-plan.md`, `technical-design.md`, and `implementation-report.md` are read from
`runs/`; the remainder read `runtime/fixtures/`, which `fixtures/README.md` explains. Within
that preference, the instance used is the first candidate on which the declared mutation can be
applied, and every candidate passed over is reported; `conforming_instance` states the order.

A committed instance produced under a contract its validator now supersedes is not a candidate
at all. `SUPERSEDED_CONTRACTS` names, per artifact type, the producing agent and the contract
version that changed what a conforming instance may say; an instance whose envelope records an
earlier version of that agent is reported by count as superseded and neither selected nor
re-validated. The validator itself exempts nothing: an instance produced at or after that
version is validated as before, and a rejected one still fails `V3`.

    python .claude/runtime/verify_validators.py
    python .claude/runtime/verify_validators.py --json-out <path>
"""

from __future__ import annotations

import argparse
import json
import re
import sys
import tempfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import artifact_contract as ac  # noqa: E402
import framework_runtime as fr  # noqa: E402
import release_note_validator as rnv  # noqa: E402
from artifact_lib import canonical_agent_id  # noqa: E402

CLAUDE = fr.CLAUDE
FIXTURES = Path(__file__).resolve().parent / "fixtures"

def understate_a_severity_count(text: str):
    """Decrement the first non-zero severity count in a review package's summary.

    A literal anchor cannot serve this artifact type. `conforming_instance` prefers a
    committed run artifact over a fixture, and a real review carries whatever severity
    counts it actually found, so an anchor pinned to one count stops matching the moment a
    genuine review replaces the fixture. The mutation is the same either way -- state a
    smaller number than the findings table carries -- so it is expressed as the edit rather
    than as one instance's spelling of it.
    """
    for level in ("Critical", "High", "Medium", "Low"):
        for n in range(9, 0, -1):
            anchor = f"- {level}: {n}"
            if anchor in text:
                return anchor, f"- {level}: {n - 1}"
    return None


def disagree_about_the_severity(text: str):
    """Restate the Metadata section's severity, leaving the metadata block saying the other.

    The same reasoning as `understate_a_severity_count` applies, and this artifact type proved
    it the expensive way. `conforming_instance` prefers a committed run artifact over a
    fixture, and a real defect carries whatever severity it was actually classified at, so an
    anchor pinned to one spelling stops matching the moment a genuine analysis replaces the
    fixture: an anchor written for `high` went dead as soon as a run committed a `critical`
    analysis, and `V4` began reporting that the mutation could not be applied at all. The
    mutation itself never depended on which severity was written -- it is "make the section
    contradict the block about one fact" -- so it is expressed as the edit rather than as one
    instance's spelling of it. The replacement is drawn from the declared vocabulary, so the
    only rule broken is agreement between the two places and not the vocabulary itself, which
    is what keeps `B2` the sole check that catches it.
    """
    for level in ac.SEVERITIES:
        anchor = f"- Severity: {level}"
        if anchor in text:
            other = next(s for s in ac.SEVERITIES if s != level)
            return anchor, f"- Severity: {other}"
    return None


def disagree_about_the_version(text: str):
    """Restate the metadata block's version, leaving the Metadata section saying the other.

    The same reasoning as `disagree_about_the_severity` applies, and this artifact type
    reached it the same way. `conforming_instance` prefers a committed run artifact over a
    fixture, and a real release note carries whatever version it was actually cut at, so an
    anchor pinned to one spelling stops matching the moment a genuine note replaces the
    fixture: an anchor written for `0.4.0` went dead as soon as a run committed a note
    versioned `0+OMT-01-unreleased`, and `V4` began reporting that the mutation could not be
    applied at all. The mutation never depended on which version was written -- it is "make
    the block contradict the section about one fact" -- so it is expressed as the edit rather
    than as one instance's spelling of it. The replacement increments the leading numeric
    component and keeps every other character, so it stays as recognisable a version string
    as the original was, and the only rule broken is agreement between the two places, which
    is what keeps `R2` the sole check that catches it.
    """
    m = re.search(r"^  version:\s*(\S+)\s*$", text, re.M)
    if not m:
        return None
    value = m.group(1)
    lead = re.match(r"(v?)(\d+)(.*)$", value, re.S)
    other = f"{lead.group(1)}{int(lead.group(2)) + 1}{lead.group(3)}" if lead else value + "1"
    return m.group(0), f"  version: {other}"


def overstate_an_option_count(text: str):
    """Increment the `Options evaluated` figure in a recommendation's assessment summary.

    The same reasoning as `understate_a_severity_count` applies. `conforming_instance` prefers a
    committed run artifact over a fixture, and a real decision carries however many options were
    genuinely open, so an anchor pinned to one count stops matching the moment a genuine
    recommendation replaces the fixture. The mutation is the same either way -- claim a wider
    comparison than the options table carries -- so it is expressed as the edit rather than as
    one instance's spelling of it.
    """
    for n in range(2, 40):
        anchor = f"- Options evaluated: {n}"
        if anchor in text:
            return anchor, f"- Options evaluated: {n + 1}"
    return None


def overstate_a_met_count(text: str):
    """Increment the `Met` figure in a validation report's execution summary.

    The same reasoning as `understate_a_severity_count` applies. `conforming_instance`
    prefers a committed run artifact over a fixture, and a real validation carries whatever
    criteria counts it actually recorded, so an anchor pinned to one count stops matching the
    moment a genuine report replaces the fixture. The mutation is the same either way --
    claim more criteria met than the results table carries -- so it is expressed as the edit
    rather than as one instance's spelling of it.
    """
    for n in range(0, 40):
        anchor = f"- Met: {n}"
        if anchor in text:
            return anchor, f"- Met: {n + 1}"
    return None


# A Phase Progression identifier cell, backticked or plain, and nothing else in the cell.
PHASE_ID_CELL = re.compile(r"^`?PH-\d{3}`?$")


def award_itself_its_own_gate(text: str):
    """Name `orchestrator` as the decider of a gate its own output is the evidence for.

    The characteristic coordinator failure, and the one the Producer Exclusion Rule exists to
    stop. Expressed as a derivation rather than a literal for the same reason as the count
    mutations above: `conforming_instance` prefers a committed run artifact over a fixture, and a
    real coordination record names whichever authority actually decided its gate, so an anchor
    pinned to one agent identifier stops matching the moment a genuine record replaces the
    fixture. What stays constant is the shape -- a Phase Progression row this role owns, at a real
    gate, whose recorded decider is somebody else -- so the row is located and its decider column
    is overwritten in place.

    The row is located by its identifier cell, under the same optional-backtick rule the shared
    contract engine applies in `artifact_contract.defined_ids`: a cell reading `PH-005` with or
    without backticks. The contract leaves that rendering free, and the validator accepts both,
    so a locator that required the backticked form found no row in a conforming record that
    rendered its identifiers plain, and `V4` reported a missing anchor for a mutation the
    validator does reject.
    """
    for line in text.splitlines():
        if not line.startswith("|"):
            continue
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        # ID, Phase, Owner, Declared Output, Gate, Gate Decision, Decided By, Evidence, Progression
        if len(cells) < 9 or not PHASE_ID_CELL.match(cells[0]):
            continue
        owner, gate, decider = cells[2], cells[4], cells[6]
        if canonical_agent_id(owner.strip("`")) != "orchestrator":
            continue
        if gate.lower() in ("none", "not-applicable") or canonical_agent_id(decider.strip("`")) == "orchestrator":
            continue
        cells[6] = "`orchestrator`"
        return line, "| " + " | ".join(cells) + " |"
    return None


# Matches the template's `- Recommended option:` spelling only; any other spelling of the label
# finds no field and fails `V4` loudly as a missing mutation anchor.
def recommend_an_unevaluated_option(text: str):
    """Point the `Recommended option` field at an option identifier the report never uses.

    The same reasoning as `understate_a_severity_count` applies, and this artifact type reached
    it the same way as `disagree_about_the_severity`. The row used to be a literal pair copied
    from the fixture: recommended option `O-002` as the anchor and `O-009` as the substitute.
    `conforming_instance` prefers a committed run artifact over a fixture, and a real
    investigation recommends whichever option it actually chose, so the anchor went dead as soon
    as a run committed a report recommending `O-003`, and `V4` began reporting that the mutation
    could not be applied at all. The substitute was fixture-bound too: a conforming report
    evaluating nine options and recommending `O-002` would have carried the anchor and turned
    `O-009` into another evaluated option, so the mutation would have been accepted.

    The contract fixes no recommended value; it fixes only that the recommendation names an
    evaluated option. So the edit is "name an option that was not evaluated": the first
    three-digit option identifier on the field's line is replaced by the lowest `O-nnn` that
    appears nowhere in the text. Every evaluated option is defined in the text, so the substitute
    can never be one of them, whatever the instance recommends or how many options it carries.

    Returns None when the field, or an option identifier on it, is missing, or when every
    identifier from `O-001` to `O-999` is already used, so `V4` reports the absent anchor
    rather than mutating something else.
    """
    field = re.search(r"^- Recommended option:[^\n]*$", text, re.M)
    if not field:
        return None
    line = field.group(0)
    named = re.search(r"\bO-\d{3}\b", line)
    if not named:
        return None
    used = set(re.findall(r"\bO-\d{3}\b", text))
    free = next((f"O-{n:03d}" for n in range(1, 1000) if f"O-{n:03d}" not in used), None)
    if free is None:
        return None
    # The pair is applied with `text.replace(old, new, 1)`, which edits the first occurrence of
    # `old` anywhere in the text. The bare line could also occur earlier inside another line, an
    # indented `  - Recommended option: ...` for instance, and that line would be edited instead
    # of the field. Anchoring on the preceding newline pins the edit to the matched field line:
    # an earlier line starting with the same text would have been the regex's first match.
    lead = "\n" if field.start() > 0 else ""
    return lead + line, lead + line[:named.start()] + free + line[named.end():]


# One declared mutation per artifact type: what to change, and the check that must catch it.
# Each is a single, surgical edit, so a rejection is attributable to it and to nothing else.
# The `old` element is a literal to find, or a callable that derives the (old, new) pair from
# the instance under test when no literal can be fixed in advance.
MUTATIONS = {
    "scope-definition.md": (
        "  acceptanceCriteriaCount: ", "  acceptanceCriteriaCount: 99  # was ", "SD2",
        "disagreeing with its own table about how many criteria it carries"),
    "execution-plan.md": (
        "\n## Risks\n", "\n## Risk Register\n", "V2.1",
        "renaming a mandatory section"),
    "technical-design.md": (
        "\n## Sign-off\n", "\n## Signoff\n", "D2.1",
        "renaming a mandatory section"),
    "bug-analysis.md": (
        disagree_about_the_severity, None, "B2",
        "disagreeing with the metadata block about severity"),
    "investigation-report.md": (
        recommend_an_unevaluated_option, None, "I1",
        "recommending an option the report never evaluated"),
    "release-note.md": (
        disagree_about_the_version, None, "R2",
        "disagreeing with itself about the version"),
    "review-package.md": (
        understate_a_severity_count, None, "P3",
        "understating a severity count the findings table contradicts"),
    "validation-report.md": (
        overstate_a_met_count, None, "Q3",
        "claiming more criteria met than the results table carries"),
    "implementation-report.md": (
        "- Review status: pending-review", "- Review status: approved", "M6",
        "awarding its own change the verdict the reviewer owns"),
    "requirement-framing.md": (
        "  requirementCount: ", "  requirementCount: 99  # was ", "RF2",
        "disagreeing with its own table about how many requirements it carries"),
    "technical-recommendation.md": (
        overstate_an_option_count, None, "T3",
        "claiming a wider option comparison than the options table carries"),
    "framework-change-proposal.md": (
        "/run-ledger.json", "/run-ledger-absent.json", "F3",
        "linking a run artifact that does not exist"),
    "orchestration-result.md": (
        award_itself_its_own_gate, None, "O4",
        "deciding the gate its own coordination record is the evidence for"),
}

# Artifact types whose conforming instance is neither a run artifact nor a fixture. A change
# proposal is a governance record: it is written about a run rather than inside one, so it lives
# under `proposals/`. The same principle as `committed_instances` applies -- a real accepted
# instance outranks a fixture -- so this source is consulted before `fixtures/`.
ALTERNATE_SOURCES = {
    "framework-change-proposal.md": "proposals/framework-change-proposal-*.md",
}


# Artifact type -> (producing agent, first contract version a conforming instance is judged
# under, why an earlier one is set aside). `release-note.md`: documentation 1.2.0 keyed the
# release position on the phase's communication basis, so a note an earlier version produced
# carries a position the corrected contract would not yield (fix-bug run run-f94564cb61ce). Those
# notes stay as recorded; they are run evidence, and are counted here rather than validated.
SUPERSEDED_CONTRACTS = {
    "release-note.md": ("documentation", rnv.CURRENT_CONTRACT_VERSION,
                        "produced under the superseded release-position contract"),
}


def version_tuple(text) -> tuple | None:
    parts = str(text or "").strip().split(".")
    return tuple(int(p) for p in parts) if len(parts) == 3 and all(
        p.isdigit() for p in parts) else None


def superseded_reason(artifact: str, envelope_path) -> str | None:
    """Why a committed instance is set aside as produced under a superseded contract, or None.

    Decided from the instance's own invocation envelope: the agent it names and the version it
    records. An instance with no envelope, another producing agent, or no readable version is
    not superseded, so it is validated as any other candidate is.
    """
    rule = SUPERSEDED_CONTRACTS.get(artifact)
    if rule is None or envelope_path is None or not Path(envelope_path).exists():
        return None
    agent_id, minimum, why = rule
    try:
        env = json.loads(Path(envelope_path).read_text(encoding="utf-8"))
    except (OSError, UnicodeDecodeError, ValueError):
        return None   # unreadable: validated like any candidate, so a failure is reported
    if not isinstance(env, dict):
        return None
    version = version_tuple(env.get("agent_version"))
    if canonical_agent_id(env.get("agent_id")) != agent_id or version is None or version >= minimum:
        return None
    return (f"{why}: {agent_id} {env.get('agent_version')} is below "
            f"{'.'.join(map(str, minimum))}")


def committed_with_envelopes(artifact: str) -> list:
    return [(instance, instance.parent.parent / "invocation-envelope.json")
            for instance in reversed(committed_instances(artifact))]


def superseded_candidates(artifact: str) -> list:
    """Committed instances `candidate_instances` leaves out, as `{"instance", "source", "reason"}`."""
    out = []
    for instance, envelope in committed_with_envelopes(artifact):
        reason = superseded_reason(artifact, envelope)
        if reason:
            out.append({"instance": instance_label(instance),
                        "source": "committed run artifact", "reason": reason})
    return out


class Report:
    def __init__(self):
        self.checks = []

    def add(self, cid, description, ok, detail=""):
        self.checks.append({"id": cid, "description": description,
                            "result": "pass" if ok else "fail", "detail": detail})

    @property
    def passed(self):
        return all(c["result"] == "pass" for c in self.checks)


def committed_instances(artifact: str) -> list:
    """Artifacts of this type that a run committed and the runtime accepted."""
    out = []
    for state in sorted(fr.RUNS.glob("run-*/state.json")):
        data = json.loads(state.read_text(encoding="utf-8"))
        for item in (data.get("work_items") or {}).values():
            c = item.get("completion") or {}
            path = c.get("artifact_path") or ""
            if path.endswith("/" + artifact) and (CLAUDE / path).exists():
                out.append(CLAUDE / path)
    return out


def candidate_instances(artifact: str) -> list:
    """Every instance of this artifact type the selector may use, in the order it tries them.

    Committed run artifacts come first, last to first in the order `committed_instances`
    returns them. That order sorts run directories by identifier, which is lexical, not
    chronological: the last-sorted run is not necessarily the most recent one. Governance
    records follow, last to first in path order, and the fixture comes last. Each entry is
    `(path, source, envelope_path)`; only a committed run artifact carries an envelope. A
    committed instance `superseded_reason` sets aside is not listed; `superseded_candidates`
    reports it instead.
    """
    out = []
    for instance, envelope in committed_with_envelopes(artifact):
        if superseded_reason(artifact, envelope):
            continue
        out.append((instance, "committed run artifact",
                    envelope if envelope.exists() else None))
    alternate = ALTERNATE_SOURCES.get(artifact)
    if alternate:
        for found in reversed(sorted(CLAUDE.glob(alternate))):
            out.append((found, "governance artifact", None))
    fixture = FIXTURES / artifact
    if fixture.exists():
        out.append((fixture, "fixture", None))
    return out


def instance_label(path: Path) -> str:
    """A candidate's path as reports print it: framework-relative where it can be."""
    return path.relative_to(CLAUDE).as_posix() if CLAUDE in path.parents else str(path)


def conforming_instance(artifact: str):
    """The instance `V3` validates and `V4` mutates for this artifact type, with its envelope.

    Returns `(instance, source, envelope_path, skipped)`. Candidates are tried in the order
    `candidate_instances` gives, and the first one on which the declared mutation resolves an
    anchor is taken. A candidate is skipped only for that reason; `skipped` lists every skipped
    candidate as `{"instance", "source", "reason"}`, so a fallback is always reported and never
    silent. When no candidate applies, `instance` is None and `skipped` names every candidate.

    Selection deliberately never asks whether the validator accepts a candidate. Doing so would
    turn a rejection `V3` must report into a quiet fallback to some other instance, so `V3`
    validates the instance `V4` mutates and, in `main`, every candidate skipped on the way to
    it; a rejected one still fails `V3`. Nor does
    selection consult the mutation verdict: a validator that accepts the mutation, or rejects
    it by the wrong check, still fails `V4` on the selected instance.

    The envelope matters. Some checks are cross-artifact rather than structural -- the design
    validator resolves referenced planner task identifiers against the execution plan the
    envelope supplied, for instance -- so validating a run artifact without its envelope would
    fail checks the runtime passed when it accepted the artifact. A fixture has no envelope,
    which is why the digest cross-check reports `not-machine-checkable` for one.

    A committed instance produced under a superseded contract (`SUPERSEDED_CONTRACTS`) is never
    a candidate, so it is neither selected nor re-validated; `main` reports it by count. That
    is a contract-version rule, decided from the instance's envelope, not a conformance one.
    """
    skipped = []
    candidates = candidate_instances(artifact)
    for instance, source, envelope in candidates:
        text = instance.read_text(encoding="utf-8")
        old, _new, _expect, what = resolve_mutation(artifact, text)
        if old is not None:
            return instance, source, envelope, skipped
        skipped.append({"instance": instance_label(instance), "source": source,
                        "reason": f"no mutation anchor for {what!r}"})
    return None, ("no applicable candidate" if candidates else "none found"), None, skipped


def resolve_mutation(artifact: str, text: str):
    """The mutation `MUTATIONS` declares for this artifact type, resolved against one instance.

    Returns `(old, new, expect_id, what)`. A callable row is asked to derive the pair from the
    text; a literal row is used as written. `old` and `new` are both None when the mutation
    cannot be applied to this instance, which `V4` reports as a missing anchor.
    """
    old, new, expect_id, what = MUTATIONS[artifact]
    if callable(old):
        derived = old(text)
        old, new = derived if derived else (None, None)
    if old is None or old not in text:
        return None, None, expect_id, what
    return old, new, expect_id, what


def mutation_verdict(artifact: str, validator, text: str, envelope, workdir: Path):
    """The `V4` verdict for one artifact type over one instance.

    Applies the resolved mutation once, writes the mutated text under `workdir`, and runs
    `validator.validate` over it. Returns `(finding, checks_failed)`: `finding` is None when the
    validator rejected the mutated artifact by the named check, and otherwise states why `V4`
    fails for this type; `checks_failed` lists the checks the mutated artifact failed, or None
    when no mutation could be applied.
    """
    old, new, expect_id, what = resolve_mutation(artifact, text)
    if old is None:
        return f"{artifact}: no mutation anchor for {what!r} in this instance", None
    path = Path(workdir) / f"mutated-{artifact}"
    path.write_text(text.replace(old, new, 1), encoding="utf-8")
    mrep = validator.validate(path, envelope)
    failed = [c["id"] for c in mrep.to_dict()["checks"] if c["result"] == "fail"]
    if mrep.passed:
        return f"{artifact}: mutation accepted ({what})", failed
    if expect_id not in failed:
        return f"{artifact}: rejected, but not by {expect_id} (by {failed})", failed
    return None, failed


def mutation_outcome(expect_id: str, finding, checks_failed) -> str:
    """The console's account of one type's mutation: the outcome observed, never the expected one.

    Only a type with no `V4` finding was caught by its named check, so only that type is
    reported as caught. Any other type is reported with the finding that failed it.
    """
    if finding is None:
        return f"caught by {expect_id} (all: {checks_failed})"
    return f"NOT CAUGHT by {expect_id}: {finding} (all: {checks_failed})"


def collected_artifacts(cell: str | None) -> list:
    """Every `.md` name an Output Artifact cell carries, in the order the cell states them.

    This is the coverage proof's reading of the column, expressed once so that
    `phase_model_artifacts`, `V5`, and `V6` cannot drift apart. It is deliberately not the
    same function as `fr.declared_output_artifact`: the two readings are what `V6` compares,
    and a shared implementation would make that comparison vacuous.
    """
    out = []
    for term in (cell or "").replace("`", "").split():
        token = term.strip(".,").strip()
        if token.endswith(".md"):
            out.append(token)
    return out


def phase_model_rows() -> list:
    """Every row of every active workflow's Phase Model, with the workflow that declares it."""
    rows = []
    for wf in fr.load_yaml("registry/workflows.yaml").get("records") or []:
        if wf.get("status") != "active":
            continue
        for row in fr.parse_phase_model(wf["specificationPath"]):
            rows.append((wf["identifier"], row))
    return rows


def phase_model_artifacts() -> dict:
    """Every Output Artifact a Phase Model names as a file, mapped to the phases naming it.

    A row whose cell names no file contributes nothing here. That is the reading this function
    has always applied, and on its own it is why `V1` could pass while five rows carried an
    Output Artifact no validator could ever be registered against: an uncollected row is not a
    covered row, it is an unexamined one. `V5` examines the rows this function does not.
    """
    out = {}
    for workflow_id, row in phase_model_rows():
        for token in collected_artifacts(row.get("output artifact")):
            out.setdefault(token, []).append(f"{workflow_id}/{row['phase']}")
    return out


# Rows whose Output Artifact cell names no artifact the Validation Engine can decide, and whose
# outstanding status is already recorded rather than newly discovered.
#
# The set is declared rather than derived. Deriving it would make `V5` restate whatever the
# workflows currently say, which is the failure `V1` already had; declaring it makes `V5` fail
# the moment a row is authored that nobody has accounted for, and fail again if one of these
# rows is corrected without being struck from this set. Shrinking the set is the measure of
# progress on `O-001`, and it is an edit somebody makes on purpose.
#
# The set is now empty. The five rows it held are the remainder of `O-001` in
# `proposals/framework-change-proposal-FC-004.md`, and none of them needed the new artifact type
# that item anticipated. Four were `documentation` phases, and the deliverable table in
# `agents/documentation/output.md` already declared one artifact type for every publication
# that agent emits, discriminated by communication basis rather than by file. The fifth was
# `review-pull-request/structural-compliance`, and `review_package_validator` already contracted
# `architect` as a permitted producer of `review-package.md` for that exact phase. Each cell was
# corrected to name the file its owner already declared; the workflow specifications carry the
# correction. `FC-004` is a signed record and is left as written, as `FC-005` left it when it
# struck `fix-bug/triage-and-impact` from the same item.
#
# An empty set does not weaken `V5`: an unaccounted-for row still fails it, and
# `V5_DECISIVENESS_PROBE` still proves the rule rejects both ways of being wrong on sets the
# repository no longer exhibits.
OUTSTANDING_UNDECIDABLE_ROWS = set()


def undecidable_verdict(found: set, outstanding: set) -> tuple:
    """The `V5` rule, as a function of two sets, so it can be tested on sets that are not real.

    Returns the rows that are undecidable and unaccounted for, and the recorded outstanding
    rows that are no longer undecidable. Either being non-empty fails `V5`: the first is a row
    nobody has decided about, the second is a record that has outlived what it records.
    """
    return sorted(found - outstanding), sorted(outstanding - found)


# The Output Artifact cell `fix-bug/triage-and-impact` carried when it blocked a run at
# `G1-CAPABILITY`, before an operator replaced it with a filename. `V5` must classify it
# undecidable, which is what ties the check to the defect it exists to catch: `V1` was and
# remains blind to this cell, because a cell naming no file gives it nothing to collect.
HISTORICAL_UNDECIDABLE_CELL = "severity classification and reproducibility decision"


# One synthetic pair per way `V5` must fail, so the check is shown to reject something. Real
# sets cannot demonstrate this: today every undecidable row is recorded outstanding, which is
# the passing case and the only one the repository can exhibit.
V5_DECISIVENESS_PROBE = [
    ("a newly authored row nobody has accounted for",
     {"some-workflow/some-phase"}, set(), (["some-workflow/some-phase"], [])),
    ("a recorded outstanding row that has since been corrected",
     set(), {"some-workflow/some-phase"}, ([], ["some-workflow/some-phase"])),
    ("every undecidable row recorded outstanding",
     {"some-workflow/some-phase"}, {"some-workflow/some-phase"}, ([], [])),
]


def undecidable_rows() -> list:
    """Active Phase Model rows the runtime cannot turn into a validatable artifact.

    A row qualifies when the artifact `fr.declared_output_artifact` derives from its Output
    Artifact cell has no registered validator. That is the condition `G1-CAPABILITY` blocks on
    at run time, evaluated here at verification time instead.
    """
    out = []
    for workflow_id, row in phase_model_rows():
        cell = row.get("output artifact") or ""
        derived = fr.declared_output_artifact(cell)
        if derived in fr.VALIDATORS:
            continue
        out.append({
            "row": f"{workflow_id}/{row['phase']}",
            "cell": cell,
            "derived": derived,
            "collected": collected_artifacts(cell),
        })
    return out


def reader_divergence(cell: str | None) -> str:
    """How the two readers of this cell relate: one verdict per cell.

      `agree`               the cell names one artifact and both readers name that artifact
      `no-artifact`         the cell names none; the output-contract reader returns it whole,
                            so the phase blocks, while the coverage proof collects nothing and
                            never examines the row
      `multiple-artifacts`  the cell names two or more; the output-contract reader returns it
                            whole, so the phase blocks, while the coverage proof requires a
                            validator for each name
      `tokeniser-drift`     the two readers disagree about how many artifacts the cell even
                            names, which is neither documented nor intended

    The last verdict is the one worth having. The first three are the divergence the column's
    shape produces, and correcting a cell removes them. Drift is the two tokenisers themselves
    parting company -- they strip different punctuation and treat backticks differently -- and
    it would put a phase's artifact and its coverage out of step for a reason no cell shape
    explains. Nothing detected it before.
    """
    collected = collected_artifacts(cell)
    derived = fr.declared_output_artifact(cell)
    whole = (cell or "").strip("`")
    if len(collected) == 1:
        return "agree" if derived == collected[0] else "tokeniser-drift"
    if derived != whole:
        return "tokeniser-drift"
    return "no-artifact" if not collected else "multiple-artifacts"


# Cells exercising every verdict `reader_divergence` can return, used by `V6` to test the
# relationship in all directions rather than only in the case where the readers agree. The
# first three are shapes real rows carry or have carried. The fourth is the drift case: the
# output-contract reader strips a trailing colon and finds one artifact, the coverage proof
# does not strip it and finds none. It is here so that `V6` is shown to fail on something,
# since a check nothing can trip decides nothing.
READER_PROBE_CELLS = [
    ("no artifact named", "release note draft, closure package", "no-artifact"),
    ("exactly one artifact named",
     "`release-note.md` and post-release action plan", "agree"),
    ("two artifacts named", "`release-note.md` and `orchestration-result.md`",
     "multiple-artifacts"),
    ("one artifact named, punctuated so only one reader sees it",
     "`release-note.md`: the release communication", "tokeniser-drift"),
]


def main():
    ap = argparse.ArgumentParser(description="Prove Validation Engine coverage and decisiveness")
    ap.add_argument("--json-out")
    a = ap.parse_args()

    r = Report()
    print("Validator Coverage Verification")
    print("-" * 100)

    # ---------------------------------------------------------------- V1 coverage
    all_rows = phase_model_rows()
    declared = phase_model_artifacts()
    uncovered = {k: v for k, v in declared.items() if k not in fr.VALIDATORS}
    r.add("V1", "every artifact a Phase Model names as a file has a registered validator",
          not uncovered,
          f"{len(declared)} artifact type(s) named across the active Phase Models, "
          f"{len(fr.VALIDATORS)} registered: "
          + ", ".join(f"{k} ({len(v)} phase(s))" for k, v in sorted(declared.items()))
          if not uncovered else f"uncovered: {uncovered}")

    # ---------------------------------------------------------------- V2 registration integrity
    broken = []
    for artifact, module in sorted(fr.VALIDATORS.items()):
        path = Path(__file__).resolve().parent / f"{module}.py"
        if not path.exists():
            broken.append(f"{artifact}: {module}.py absent")
            continue
        try:
            mod = __import__(module)
        except Exception as exc:  # noqa: BLE001
            broken.append(f"{artifact}: {module} does not import ({exc})")
            continue
        if not callable(getattr(mod, "validate", None)):
            broken.append(f"{artifact}: {module} exposes no validate()")
    r.add("V2", "every registered validator module exists, imports, and exposes validate()",
          not broken,
          f"{len(fr.VALIDATORS)} registration(s): "
          + ", ".join(f"{k} -> {v}" for k, v in sorted(fr.VALIDATORS.items()))
          if not broken else f"broken: {broken}")

    # ---------------------------------------------------------------- V3/V4 per artifact type
    tmp = Path(tempfile.mkdtemp(prefix="validator-check-"))
    summaries = []
    accepted_all, rejected_all = [], []
    skipped_all = []
    superseded_all = {}
    for artifact, module in sorted(fr.VALIDATORS.items()):
        mod = __import__(module)
        _old, _new, expect_id, what = MUTATIONS[artifact]
        superseded = superseded_candidates(artifact)
        if superseded:
            superseded_all[artifact] = superseded
        instance, source, envelope_path, skipped = conforming_instance(artifact)
        for s in skipped:
            skipped_all.append(f"{artifact}: skipped {s['instance']} ({s['source']}): "
                               f"{s['reason']}")
        if instance is None and not skipped:
            accepted_all.append(f"{artifact}: no conforming instance available")
            continue
        no_candidate = instance is None
        if no_candidate:
            # No candidate carries the mutation's shape, so `V4` fails, naming every one. `V3`
            # still validates the first candidate tried, so a rejection is reported as well.
            rejected_all.append(
                f"{artifact}: no candidate carries a mutation anchor for {what!r}; tried "
                + "; ".join(f"{s['instance']} ({s['source']}): {s['reason']}"
                            for s in skipped))
            instance, source, envelope_path = candidate_instances(artifact)[0]
        envelope = json.loads(envelope_path.read_text(encoding="utf-8")) \
            if envelope_path else None
        rep = mod.validate(instance, envelope)
        d = rep.to_dict()
        if not rep.passed:
            accepted_all.append(
                f"{artifact}: conforming instance rejected "
                f"{[c['id'] for c in d['checks'] if c['result'] == 'fail']}")
        # Selection passes over a candidate only because the mutation cannot be applied to it,
        # never because of conformance. `V3` still validates every candidate passed over, so a
        # rejected instance fails `V3` whichever instance `V4` ends up mutating.
        skipped_labels = {s["instance"] for s in skipped}
        for cand, _src, cand_env_path in candidate_instances(artifact):
            if cand == instance or instance_label(cand) not in skipped_labels:
                continue
            cand_env = json.loads(cand_env_path.read_text(encoding="utf-8")) \
                if cand_env_path else None
            crep = mod.validate(cand, cand_env)
            if not crep.passed:
                accepted_all.append(
                    f"{artifact}: skipped candidate {instance_label(cand)} rejected "
                    f"{[c['id'] for c in crep.to_dict()['checks'] if c['result'] == 'fail']}")

        if no_candidate:
            finding, mutated_result = rejected_all[-1], None
        else:
            text = instance.read_text(encoding="utf-8")
            finding, mutated_result = mutation_verdict(artifact, mod, text, envelope, tmp)
            if finding:
                rejected_all.append(finding)

        summaries.append({
            "artifact": artifact,
            "validator": module,
            "conforming_instance": instance.relative_to(CLAUDE).as_posix()
            if CLAUDE in instance.parents else str(instance),
            "instance_source": source,
            "envelope": instance_label(envelope_path) if envelope_path else None,
            "phases_served": declared.get(artifact, []),
            "result": d["result"],
            "checksRun": d["checksRun"],
            "checksPassed": d["checksPassed"],
            "notMachineCheckable": d["notMachineCheckable"],
            "mutation": {"description": what, "expected_check": expect_id,
                         "checks_failed": mutated_result, "finding": finding},
            "skipped_candidates": skipped,
            "superseded_candidates": superseded,
            "counts": d["counts"],
        })

    # Instances produced under a superseded contract are counted, never validated, so setting
    # them aside is reported whether `V3` passes or fails.
    superseded_detail = "".join(
        f"; {len(v)} {k} candidate(s) produced under a superseded contract, not validated"
        for k, v in sorted(superseded_all.items()))
    r.add("V3", "every registered validator accepts a conforming artifact of its type",
          not accepted_all,
          (f"{len(summaries)} artifact type(s) validated" if not accepted_all
           else f"{accepted_all}") + superseded_detail)
    # Skipped candidates are reported whether `V4` passes or fails, so a fallback past an
    # instance the mutation could not be applied to is never silent.
    skipped_detail = (f"; skipped candidates: {skipped_all}" if skipped_all
                      else "; no candidate skipped")
    r.add("V4", "every registered validator rejects a mutated artifact, by the named check",
          not rejected_all,
          ("; ".join(f"{s['artifact']} -> {s['mutation']['expected_check']}"
                     for s in summaries) if not rejected_all else f"{rejected_all}")
          + skipped_detail)

    # ------------------------------------------------------------ V5 row decidability
    # `V1` asks whether every artifact a Phase Model names has a validator, and a row that names
    # no artifact answers it by saying nothing. `V5` asks the question the other way round, of
    # every row rather than of every named artifact, so a row the Validation Engine cannot
    # decide is caught here instead of blocking a run at `G1-CAPABILITY`.
    undecidable = undecidable_rows()
    found = {u["row"] for u in undecidable}
    unaccounted, stale = undecidable_verdict(found, OUTSTANDING_UNDECIDABLE_ROWS)
    v5_probe_wrong = [f"{what}: got {undecidable_verdict(f, o)}, expected {expect}"
                      for what, f, o, expect in V5_DECISIVENESS_PROBE
                      if undecidable_verdict(f, o) != expect]
    if fr.declared_output_artifact(HISTORICAL_UNDECIDABLE_CELL) in fr.VALIDATORS:
        v5_probe_wrong.append(
            "the cell that blocked fix-bug/triage-and-impact is no longer classified "
            "undecidable, so this check no longer detects the defect it was written for")
    if found:
        v5_detail = (f"{len(all_rows)} active row(s), {len(found)} undecidable, all "
                     f"{len(OUTSTANDING_UNDECIDABLE_ROWS)} of them recorded outstanding under "
                     f"O-001 of proposals/framework-change-proposal-FC-004.md: "
                     + ", ".join(sorted(found)))
    else:
        v5_detail = (f"{len(all_rows)} active row(s), every one naming an artifact the "
                     f"Validation Engine can decide; no row is recorded outstanding, so the "
                     f"O-001 remainder in proposals/framework-change-proposal-FC-004.md "
                     f"carries no row this check can still find")
    v5_detail += (f"; the rule rejects {len(V5_DECISIVENESS_PROBE) - 1} synthetic "
                  f"non-conforming set(s)")
    if unaccounted or stale or v5_probe_wrong:
        v5_detail = (f"unaccounted (undecidable and not recorded outstanding): {unaccounted}; "
                     f"stale (recorded outstanding but now decidable or absent): {stale}; "
                     f"rule probe: {v5_probe_wrong}")
    r.add("V5", "every active Phase Model row names an artifact the Validation Engine can "
                "decide, or is a recorded outstanding row",
          not unaccounted and not stale and not v5_probe_wrong, v5_detail)

    # ------------------------------------------------------------ V6 reader agreement
    # The output-contract reader and the coverage proof read the same cell under different
    # rules. `framework_runtime.declared_output_artifact` once documented them as agreeing.
    # They agree on one cell shape of three, so the relationship is exercised here in all
    # three directions, and every active row is required to diverge for no reason other than
    # the number of artifacts its cell names.
    probe = [(what, cell, reader_divergence(cell), expect)
             for what, cell, expect in READER_PROBE_CELLS]
    probe_wrong = [f"{what}: got {got!r}, expected {expect!r}"
                   for what, _cell, got, expect in probe if got != expect]
    verdicts = {}
    drifting = []
    for workflow_id, row in all_rows:
        verdict = reader_divergence(row.get("output artifact"))
        verdicts[verdict] = verdicts.get(verdict, 0) + 1
        if verdict == "tokeniser-drift":
            drifting.append(f"{workflow_id}/{row['phase']}")
    v6_detail = (f"{len(all_rows)} active row(s): "
                 + ", ".join(f"{k}={v}" for k, v in sorted(verdicts.items()))
                 + "; probe: "
                 + "; ".join(f"{what} -> {got}" for what, _c, got, _e in probe))
    if probe_wrong or drifting:
        v6_detail = (f"probe disagreements: {probe_wrong}; "
                     f"rows whose two readers disagree about how many artifacts the cell "
                     f"names: {drifting}")
    r.add("V6", "the two readers of the Output Artifact column are tested against each other, "
                "over cells naming zero, one, and two artifacts, and no active row drifts",
          not probe_wrong and not drifting, v6_detail)

    for c in r.checks:
        print(f"[{'PASS' if c['result'] == 'pass' else 'FAIL'}] {c['id']:<4} "
              f"{c['description']}")
        print(f"       {c['detail']}")

    print()
    print("Validation summaries")
    print(f"  {'artifact':<26} {'validator':<32} {'result':<7} {'checks':<10} "
          f"{'nmc':<4} instance")
    print("  " + "-" * 116)
    for s in summaries:
        print(f"  {s['artifact']:<26} {s['validator']:<32} {s['result'].upper():<7} "
              f"{str(s['checksPassed']) + '/' + str(s['checksRun']):<10} "
              f"{s['notMachineCheckable']:<4} {s['instance_source']}")
    print()
    print("Mutation results (a validator that accepts everything decides nothing)")
    for s in summaries:
        m = s["mutation"]
        print(f"  {s['artifact']:<26} {m['description']:<52} -> "
              f"{mutation_outcome(m['expected_check'], m['finding'], m['checks_failed'])}")
        for sk in s["skipped_candidates"]:
            print(f"  {'':<26} skipped {sk['instance']} ({sk['source']}): {sk['reason']}")
        for sk in s["superseded_candidates"]:
            print(f"  {'':<26} superseded {sk['instance']}: {sk['reason']}")

    total = len(r.checks)
    passed = sum(1 for c in r.checks if c["result"] == "pass")
    print()
    print("-" * 100)
    print(f"{passed}/{total} checks passed -- "
          f"{'COVERED' if r.passed else 'NOT COVERED'}")

    if a.json_out:
        Path(a.json_out).write_text(json.dumps({
            "schema": "framework.runtime/validator-coverage.v1",
            "runtime_version": fr.RUNTIME_VERSION,
            "result": "pass" if r.passed else "fail",
            "registered_validators": fr.VALIDATORS,
            "phase_model_artifacts": declared,
            "phase_model_row_count": len(all_rows),
            "undecidable_rows": undecidable,
            "outstanding_undecidable_rows": sorted(OUTSTANDING_UNDECIDABLE_ROWS),
            "reader_divergence_probe": [
                {"cell_shape": what, "cell": cell, "verdict": got, "expected": expect}
                for what, cell, got, expect in probe
            ],
            "reader_divergence_by_row": verdicts,
            "checks": r.checks,
            "summaries": summaries,
        }, indent=2), encoding="utf-8")
    return 0 if r.passed else 1


if __name__ == "__main__":
    sys.exit(main())
