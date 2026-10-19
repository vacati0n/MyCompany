# Command Specification: /investigate

## Purpose
Run decision-support discovery when a question must be answered against the current system
before any change is planned.

## Inputs
- Investigation question and decision owner.
- Scope, timeline, and constraints.
- Accessible context sources.

## Workflow Triggered
Investigate (`workflows/investigate.md`).

## Expected Outputs
- Framed objective with success criteria.
- Investigation report with evidence and confidence levels.
- Option analysis and recommended option with implementation implications.
- Published findings package and decision dependencies.

## Success Criteria
- Recommendation is evidence-backed and decision-ready.
- Assumptions and risks are explicit.
- Stakeholders can proceed with implementation or defer with recorded rationale.

## Failure Handling
- Reframe scope when evidence is insufficient.
- Extend data collection for low-confidence conclusions.
- Escalate conflicting findings for architectural or business arbitration.

## Relationship to /research
`/research` and this command cover the same intent through two workflows that the framework
retains deliberately. `workflows/research.md` is the canonical research lifecycle.
`workflows/investigate.md` is the compatibility lifecycle that `agents/planner/manifest.yaml`
and `agents/architect/manifest.yaml` declare in `supportedWorkflows`, which is why it stays
reachable through a command of its own rather than being routed away. Use `/investigate` when
the question is about the current system and the planner or architect contracts are expected
to participate; use `/research` for open product or technology decisions.
