# Command Specification: /document

## Purpose
Create or update technical and release documentation aligned with delivered behavior.

## Inputs
- Change summary.
- Target audience.
- Documentation scope and release context.

## Workflow Triggered
Implement Feature (`workflows/implement-feature.md`), whose
`documentation-and-release-handoff` phase this command owns.

`domain-model/command-specification.md` permits exactly one primary workflow per command, so
the primary mapping is the delivery lifecycle where documentation is a routed phase with an
owner, an artifact, and a closing gate. Documentation work inside other lifecycles is reached
through their own commands and is not a second mapping for this one:

| Lifecycle | Phase that carries documentation | Entry command |
|---|---|---|
| Implement Feature | `documentation-and-release-handoff` | `/document`, `/implement` |
| Fix Bug | `closure-and-communication` | `/bugfix` |
| Release | `communication-and-post-release` | `/release` |
| Review Pull Request | `documentation-impact` | `/review` |
| Investigate, Research | `publication`, `findings-publication` | `/investigate`, `/research` |

## Expected Outputs
- Updated technical documentation.
- Release note updates.
- Operational guidance and known issue notes.

## Success Criteria
- Documentation matches implemented behavior.
- Critical usage and troubleshooting guidance is present.
- Release communication artifacts are complete.

## Failure Handling
- Block publication of unverified statements.
- Return gaps to engineering owners for clarification.
- Escalate conflicting source information to orchestrator.
