# Memory: Business Rules

## Purpose

Persist validated domain and policy rules that must remain consistent across features and releases.

## Structure

- Rule ID:
- Statement:
- Source of truth:
- Affected bounded contexts:
- Preconditions:
- Exceptions:
- Last validation date:

## Ownership

- Primary Owner: Product Owner
- Approver: Business Analyst
- Contributors: Architect, QA, Documentation

## Update Rules

- Update only after rule validation with business owners.
- Use deterministic language and measurable conditions.
- Preserve superseded rules with replacement references.
- Revalidate high-impact rules at release milestones.

## Consumers

- Business Analyst for requirement definition.
- Product Owner for scope and acceptance decisions.
- Developers and QA for implementation and verification.

## Lifecycle

- Draft during requirement discovery.
- Active after business approval.
- Deprecated when replaced by newer policy.
- Archived when policy is retired and no longer applicable.

