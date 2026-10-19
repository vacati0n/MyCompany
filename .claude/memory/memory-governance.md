# Memory Governance

## Purpose

Define durable memory quality and update discipline.

## Memory Quality Rules

- Record only stable, reusable project knowledge.
- Avoid transient task chatter and unresolved assumptions.
- Include owner and last validation date for critical memory.

## Required Categories

- Architecture
- Business Rules
- Coding Standard
- Decision Log
- Known Issues
- Technology Stack
- Glossary

## Lesson Category

Lessons from earlier runs are governed memory written by the runtime, not by hand.

- Source: recorded run evidence only (validation failures and gate rejections), harvested by
  `runtime/knowledge.py` into the lesson bundle `memory/lessons/` (one OKF v0.2 concept
  document per lesson, plus a derived `index.md` and `log.md`); evidence free text is never
  copied.
- Lifecycle: `candidate` on first harvest, `active` when approved, `retired` when retired. Every
  approve, retire, and annotate action needs an actor and a rationale and appends one history
  entry. Nothing is deleted.
- Ownership: each lesson carries an owner (default `tech-lead`) and a rationale.
- Eligibility: active, or a candidate recurring in at least two distinct runs (the threshold in
  `config/knowledge-policy.json`); retired lessons are never shown.
- Hydration is recorded per run at plan time: on by default, off with `--no-lesson-hydration`;
  a run planned before the record existed is never shown lessons.
- Staleness: an operator retires a lesson that no longer helps; `knowledge.py report` shows how
  often its failure recurred after it was shown.

## Write Triggers

- Approved architecture or policy decision.
- Production incident with reusable lessons.
- Business rule changes affecting acceptance logic.
- Run aggregation of a run carrying a lesson-hydration record (automatic lesson harvest).

## Validation

- Memory entries must be concise and testable when possible.
- Contradictory entries should be corrected immediately.
