# Template: Release Note

## Usage

Canonical artifact template for `release-note.md`, produced by agent `documentation` in
its five publication phases: `documentation-and-release-handoff` of `workflows/implement-feature.md`,
`publication` of `workflows/investigate.md`, `findings-publication` of `workflows/research.md`,
`documentation-impact` of `workflows/review-pull-request.md`, and `communication-and-post-release`
of `workflows/release.md`.

Template version 1.2.0. Version 1.1.0 is the machine-checkable revision: it adds the leading
provenance metadata block and the Known Issues table, so the Validation Engine
(`runtime/release_note_validator.py`) can decide conformance rather than trusting it. Version
1.2.0 keys the release verdict on the phase's communication basis: only the release phase states
a deployment outcome (`released`, `partial`, `rolled-back`), and the other four publish
`handed-off`, `findings-published`, or `delta-published`, none of which claims a merge, release,
or deployment. Section titles and section order are unchanged from 1.0.0.

Section titles, section order, and identifier schemes are fixed. Sections are never omitted.
A section with nothing to report reads `None identified.`

Identifier scheme: known issues `K-nnn`, zero-padded to three digits and ascending.

The binding behavioural contract is `agents/documentation.md`. Its rule that
documentation matches delivered behaviour is enforced mechanically where it is decidable: a
declared contract change requires a compatibility statement, and a rolled-back release
requires a recorded issue.

---

```yaml
releaseNote:
  releaseId:
  version:
  sourceInputs:
    - type:          # deployment-status | monitoring-health-record | final-change-summary | stakeholder-list | verification-report | validation-report | technical-recommendation | architecture-context
      reference:     # supplied reference, or "inline"
  producedBy: documentation
  agentVersion:
  schemaVersion: 1.0.0
  status:            # complete | provisional | blocked
  releaseVerdict:    # released | partial | rolled-back | handed-off | findings-published | delta-published
  inputDigest:
  contextDigest:
```

## Metadata

- Version:                       <!-- equals the metadata block -->
- Release date and time:
- Environment:
- Release owner:

## Highlights

- Feature additions:
- Bug fixes:
- Improvements:

## Technical Changes and Compatibility

- API or contract changes:
- Database or migration impact:
- Configuration changes:
- Backward compatibility notes:   <!-- required whenever a contract change is declared -->

## Operational Notes

- Deployment considerations:
- Monitoring and alerts:
- Rollback criteria:

## Validation Summary

- Test status:
- Known risk acceptance:
- Post-release checks:

## Known Issues

<!-- One row per issue shipped with the release. Impact states who is affected and how.
`None identified.` is valid only when the release verdict is `released`, `handed-off`,
`findings-published`, or `delta-published`. -->

| ID | Issue | Impact | Workaround | Tracking |
|---|---|---|---|---|
| `K-001` | | | | |

## Communication

- Stakeholders notified:
- Support handoff notes:
