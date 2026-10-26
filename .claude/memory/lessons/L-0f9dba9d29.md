---
type: "Lesson"
title: "validation-check C7.1 on investigation-report.md"
description: "Metadata digests match the frozen snapshot recorded in the invocation envelope"
tags:
  - "validation-check"
  - "investigation-report.md"
  - "technical-validation"
sources:
  - resource: "../../runs/run-8d7e366341bf/events.jsonl"
generated:
  at: "2026-10-26T11:52:25Z"
  by: "knowledge-loop/1.0.0"
status: "draft"
lesson_record:
  lesson_id: "L-0f9dba9d29"
  fingerprint: "check:investigation-report.md:C7.1"
  kind: "validation-check"
  check_id: "C7.1"
  check_description: "Metadata digests match the frozen snapshot recorded in the invocation envelope"
  check_severity: "Blocking"
  gate: null
  artifact_type: "investigation-report.md"
  phases:
    - "technical-validation"
  occurrences: 1
  distinct_runs: 1
  first_seen: "2026-10-26T11:52:25Z"
  last_seen: "2026-10-26T11:52:25Z"
  evidence:
    - "runs/run-8d7e366341bf/events.jsonl#E-0024"
  evidence_state: "current"
  status: "candidate"
  owner: "tech-lead"
  rationale: "harvested from recorded run evidence: the Validation Engine rejected this artifact type on this check"
  remedy: null
  history: []
---
# validation-check C7.1 on investigation-report.md

Summary: Metadata digests match the frozen snapshot recorded in the invocation envelope
