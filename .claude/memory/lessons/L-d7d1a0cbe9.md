---
type: "Lesson"
title: "validation-check V5.1 on execution-plan.md"
description: "Every dependency edge references existing tasks or a named external party"
tags:
  - "validation-check"
  - "execution-plan.md"
  - "execution-planning"
sources:
  - resource: "../../runs/run-022d0492a75a/events.jsonl"
  - resource: "../../runs/run-535f3f938f32/events.jsonl"
generated:
  at: "2026-10-26T21:10:33Z"
  by: "knowledge-loop/1.0.0"
status: "draft"
lesson_record:
  lesson_id: "L-d7d1a0cbe9"
  fingerprint: "check:execution-plan.md:V5.1"
  kind: "validation-check"
  check_id: "V5.1"
  check_description: "Every dependency edge references existing tasks or a named external party"
  check_severity: "Blocking"
  gate: null
  artifact_type: "execution-plan.md"
  phases:
    - "execution-planning"
  occurrences: 2
  distinct_runs: 2
  first_seen: "2026-10-26T15:26:06Z"
  last_seen: "2026-10-26T21:10:33Z"
  evidence:
    - "runs/run-022d0492a75a/events.jsonl#E-0025"
    - "runs/run-535f3f938f32/events.jsonl#E-0037"
  evidence_state: "current"
  status: "candidate"
  owner: "tech-lead"
  rationale: "harvested from recorded run evidence: the Validation Engine rejected this artifact type on this check"
  remedy: null
  history: []
---
# validation-check V5.1 on execution-plan.md

Summary: Every dependency edge references existing tasks or a named external party
