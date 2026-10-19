# Configuration: Runtime Policies

## Security and Compliance

- Do not expose secrets in logs or artifacts.
- Respect principle of least privilege in automation.
- Record sensitive changes with approval trace.

## Reliability

- Prefer idempotent operations for workflow steps.
- Require explicit retries and timeout strategy.
- Fail fast on invalid inputs.

## Auditability

- Every workflow must produce a traceable output artifact.
- Decision records must include rationale and owner.
- Release actions require timestamped status entries.

## Context Budget

- A session in the framework checkout auto-compacts once its context passes 250,000 tokens.
  `.claude/settings.json` sets `CLAUDE_CODE_AUTO_COMPACT_WINDOW` to `250000`, an absolute token
  count, so the threshold is the same on every model, including 1M-context models that would
  otherwise compact only near their full window.
- Do not express the budget with `CLAUDE_AUTOCOMPACT_PCT_OVERRIDE`: it is a percentage of the
  window, so it means a different token count on each model.
- The setting is read by Claude Code only after the folder is trusted.

## Change Management

- Policy updates require tech lead approval.
- Breaking policy changes must include migration guidance.
