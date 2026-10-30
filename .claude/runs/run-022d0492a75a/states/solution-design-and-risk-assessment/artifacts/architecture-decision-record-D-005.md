# Architecture Decision Record

## Metadata

- ADR ID: D-005
- Title: What the approval record says when the CEO or an employee presses approve
- Date: 2026-10-27
- Status: Proposed
- Owners: tech-lead (accepting Design Gate owner under the producer exclusion rule); the CEO decides the wording at the Design Gate
- Related Work Items: MC-17, run run-022d0492a75a, technical design TD-MC17-PILOT-WEB, planner tasks T-014, T-015

## Context

- Problem statement: an employee may press approve on the CEO's written instruction, and the record must name the CEO; when an employee presses, it must also name the employee and the instruction, all inside the existing approver text, with the record form unchanged (C-021).
- Business and technical constraints: C-009, C-015, C-021.
- Current architecture baseline: the approval record holds video code, video SHA-256, approver text and date; the toolchain stores the trimmed approver text unchecked and shows it in its verify line (F-014).
- Choice for the CEO, in plain words:
  - Option A (proposed, O-015): when you press approve, the record reads "[your name] (CEO)". When an employee presses approve, it reads "[your name] (CEO; nhân viên [employee's name] bấm duyệt theo chỉ thị bằng văn bản của CEO)". Your name is entered once at setup. The approval screen asks who is pressing.
  - The other ways considered break your Scope Gate answer: a separate note beside the package (the answer said inside the existing field), or your name only (the employee would not be named).

## Decision

- Selected option: O-015.
- Decision statement: M-001 composes the approver text from the CEO name in the app record and, when the employee choice is made, the employee's typed name, both after NFC and trimming, in the two forms above; a name holding a control character, "(", ")" or ";" is refused in Vietnamese; the approve verb receives the text unchanged; the approval screen states the rule in Vietnamese.
- Scope of impact: M-001, M-002, M-010 (form unchanged), M-017.

## Alternatives Considered

1. O-016: separate record of employee and instruction beside the package
- Benefits: the approver text stays a bare name.
- Risks: a second record the toolchain does not read.
- Why not selected: eliminated by C-021.

2. O-017: CEO's name only
- Benefits: simplest text.
- Risks: the employee who pressed is not recorded.
- Why not selected: eliminated by C-021.

## Consequences

- Positive outcomes expected: every approval names the CEO (T-015); the verify line shows who pressed; a record written from the command line stays valid.
- Tradeoffs accepted: three characters are reserved in names.
- Risks introduced: R-005, R-011.

## Validation Plan

- Metrics to monitor: approval records whose text does not start with the stored CEO name (expected zero).
- Verification checkpoints: P-012; SC-18 and SC-19 browser runs for both choices.
- Rollback or reversal conditions: rewording only, if the CEO chooses other words; the record form never changes.

## Approval

- Architect: unsigned (producer; excluded from accepting)
- Tech Lead: unsigned
- Product Owner (if scope-impacting): unsigned; the CEO confirms the wording
