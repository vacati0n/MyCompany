# Memory: Technology Stack

## Purpose
Persist authoritative technology stack definitions, approved versions, and compatibility constraints.

## Structure

- Stack Area (runtime, frontend, database, tooling, infrastructure):
- Technology name:
- Approved version range:
- Usage scope:
- Compatibility constraints:
- Support status:
- Upgrade cadence:
- Last reviewed date:

## Ownership

- Primary Owner: Tech Lead
- Approver: Architect
- Contributors: Backend Developer, Frontend Developer, QA, Security

## Update Rules

- Update after approved technology adoption, deprecation, or version policy change.
- Record compatibility and migration implications for version changes.
- Mark unsupported technologies with retirement deadlines.
- Revalidate stack baseline before release planning cycles.

## Consumers

- Implementers for build and dependency choices.
- Reviewer and Security for compliance and vulnerability posture.
- QA for environment parity and test strategy.

## Lifecycle

- Candidate when evaluation is in progress.
- Approved when selected for production use.
- Deprecated when replacement is mandated.
- Retired when removed from all maintained components.
