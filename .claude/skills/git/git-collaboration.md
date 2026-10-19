# Skill: Git Collaboration

## Purpose
Provide reusable source-control practices for safe collaboration, traceability, and change isolation.

## Principles
- Commits are small, coherent, and reversible.
- Branches represent a clear unit of work.
- History should explain why changes exist.
- Integration should happen frequently.

## Best Practices
- Use descriptive commit messages with intent and impact.
- Rebase or merge frequently to reduce conflict complexity.
- Keep pull requests focused and reviewable.
- Tag releases consistently and annotate key milestones.

## Anti-patterns
- Large mixed-purpose commits.
- Force pushes to shared protected branches.
- Long-lived branches that drift from mainline.

## Examples
- Commit message format: `feat(order): validate shipping transition rule`
- Branch name format: `feature/order-shipping-validation`

## Decision Rules
- Split changes when review scope exceeds one concern.
- Prefer revert over risky manual rollback edits.
- Require review before merging production-impacting changes.

## Common Mistakes
- Resolving conflicts without rerunning tests.
- Squashing unrelated changes into a single commit.
- Missing links between commits and tracked work items.
