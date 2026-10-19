# Skills Specification

## Skill Definition

A skill is a reusable engineering knowledge unit that defines stable guidance
for decisions, implementation quality, and risk control.

## Reusable Knowledge

- Skills capture practices that are reused across workflows and agents.
- Skills are domain-scoped, not task-scoped.
- Skills must be explicit, testable, and version-aware when applicable.

## Skill Categories

- Architecture
- Business
- Platform engineering (.NET, frontend, database)
- Quality engineering (testing, performance, security)
- Delivery engineering (git, CI/CD, observability, error handling)

## Dependency Rules

- Agents may depend on multiple skills, but workflows define mandatory skills per phase.
- Skills may reference configuration policies, but not command behavior directly.
- Workflow definitions are the integration layer between agents and skills.
- Skill conflicts are resolved by Architect or Tech Lead ownership.
