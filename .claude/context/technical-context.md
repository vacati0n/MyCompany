# Technical Context

## Technology Stack

- Runtime: .NET ecosystem.
- Architecture target: Clean Architecture.
- Delivery pattern: test-first where practical, automation-first for repeatable checks.

## Engineering Constraints

- Strong separation between domain and infrastructure.
- Backward compatibility for externally consumed contracts.
- Observable operations with meaningful telemetry.

## Integration Expectations

- Explicit contract versioning.
- Stable failure-handling semantics.
- Documented timeout and retry behavior.
