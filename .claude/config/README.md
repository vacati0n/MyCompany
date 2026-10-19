# Configuration Specification

## Configuration System

The configuration system defines enforceable governance policies for framework
execution. It provides the control plane for:

- Agent routing and escalation.
- Quality gates and approval criteria.
- Runtime safety, reliability, and auditability policies.

## Key Specifications

- `runtime.md`: top-level runtime architecture and lifecycle.
- `execution-engine.md`: detailed execution engine design for orchestration, retries, escalation, observability, and extensibility.
- `runtime-policies.md`: cross-cutting governance and reliability constraints.
- `output-aggregator.md`: deterministic final-package assembly and provenance rules.
- `self-hosting-profile.md`: the one command profile for framework-internal change routing. It
  decides whether a change is framework-internal, which command carries it, what evidence its
  change proposal must link, and when the change counts as completed in self-hosting mode.
  `runtime/self_hosting.py` is its executable form.

Configuration artifacts are normative. Workflow and command behavior must align
with active configuration policies.
