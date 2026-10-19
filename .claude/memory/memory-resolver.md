# Memory Resolver Specification

## Purpose

Define the runtime resolver that determines which memory categories must be loaded for each workflow and workflow state, while minimizing unnecessary memory hydration.

## Scope

The resolver governs:

- workflow-to-memory category requirements
- state-level memory narrowing
- read and write eligibility by role and workflow
- deterministic memory bundle construction
- auditability of loaded and excluded memory

## Memory Categories

The resolver operates on governed categories:

- architecture
- business-rules
- coding-standard
- decision-log
- known-issues
- technology-stack
- glossary
- lessons (machine-maintained; resolved by the rule below, not by the workflow tables)

## Resolver Inputs

- workflow id and version
- workflow state id
- primary agent id
- task intent
- risk profile (standard, high-risk, incident)
- memory governance policy version
- category metadata (owner, validation date, status)

## Resolver Outputs

- required memory categories for current execution
- optional categories excluded by minimality policy
- effective memory snapshot versions and digest
- writeback permissions by category

## Resolution Model

### Step 1: Determine Workflow Baseline

Load baseline categories required for the workflow type.

### Step 2: Apply State Narrowing

Restrict baseline to categories needed for the active state and expected output.

### Step 3: Apply Risk Expansion

Add conditional categories only when triggered by risk profile or gate policy.

### Step 4: Validate Governance

Reject categories that are archived, unapproved, or policy-blocked unless escalation authorizes fallback.

### Step 5: Emit Effective Bundle

Produce an ordered bundle with source rule, category version, and rationale.

## Workflow Memory Requirements

### Requirement Legend

- Required: always loaded for this workflow.
- Conditional: loaded only when specific triggers apply.

### 1) Implement Feature

Required:

- business-rules
- architecture
- coding-standard
- technology-stack
- decision-log

Conditional:

- glossary (domain vocabulary complexity)
- known-issues (touches historically unstable modules)

### 2) Fix Bug

Required:

- known-issues
- coding-standard
- technology-stack
- decision-log

Conditional:

- architecture (cross-module or structural defect)
- business-rules (acceptance behavior defect)
- glossary (ambiguous domain terms in defect report)

### 3) Investigate

Required:

- architecture
- business-rules
- decision-log
- technology-stack
- glossary

Conditional:

- known-issues (investigation includes recurrent incidents)
- coding-standard (investigation includes implementation feasibility constraints)

### 4) Research

Required:

- business-rules
- architecture
- decision-log
- glossary

Conditional:

- technology-stack (research has platform feasibility implications)
- known-issues (research informed by production incidents)
- coding-standard (research compares implementation patterns)

### 5) Refactor

Required:

- architecture
- coding-standard
- technology-stack
- decision-log
- known-issues

Conditional:

- business-rules (refactor touches acceptance-sensitive behavior)
- glossary (domain invariants require exact terminology)

### 6) Review Pull Request

Required:

- coding-standard
- architecture
- known-issues
- decision-log

Conditional:

- business-rules (PR changes business behavior)
- technology-stack (PR introduces platform-specific risks)
- glossary (PR changes domain terms or public semantics)

### 7) Release

Required:

- known-issues
- decision-log
- technology-stack
- business-rules

Conditional:

- architecture (release includes structural/runtime changes)
- coding-standard (release requires remediation verification)
- glossary (external communication has domain-critical wording)

## State-Level Narrowing Rules

- Scope and framing states prioritize business-rules, glossary, and decision-log.
- Design and architecture review states prioritize architecture, technology-stack, and decision-log.
- Implementation and code review states prioritize coding-standard, architecture, technology-stack, and known-issues.
- Verification and QA states prioritize known-issues, coding-standard, and decision-log.
- Closure and release communication states prioritize decision-log, business-rules, and glossary.

## Minimality Policy

- Start from required categories only.
- Load conditional categories only on explicit trigger.
- Use section-level memory slicing where available.
- Enforce per-state memory budget caps.
- Allow one controlled expansion pass when validation indicates missing memory context.

## Writeback Policy by Workflow

- Implement Feature: decision-log, architecture, coding-standard, business-rules.
- Fix Bug: known-issues, decision-log, coding-standard.
- Investigate: decision-log, architecture, business-rules.
- Research: decision-log, business-rules, architecture.
- Refactor: decision-log, architecture, coding-standard, known-issues.
- Review Pull Request: decision-log, known-issues.
- Release: decision-log, known-issues, business-rules, technology-stack.

Writeback constraints:

- write only durable and validated knowledge
- include owner and validation date for critical updates
- preserve superseded references for traceability

## Lesson Resolution

The lesson category is resolved per dispatch, not per workflow, by `runtime/knowledge.py`:

- Store: the lesson bundle `memory/lessons/`, one OKF v0.2 concept document per lesson; every
  document is read, and `index.md` and `log.md` are never read back.

- A dispatch receives lessons only when its run's plan-time record says hydration is on. The
  default is on for runs planned after runtime 0.10.0; the plan flag `--no-lesson-hydration`
  records off; a run without a record never receives lessons.
- Candidates are the eligible lessons: `active`, or `candidate` with at least two distinct runs
  (configurable threshold); `retired` lessons never resolve.
- Narrowing: a lesson resolves for a state only when its phases include that state and its
  artifact type is the state's output artifact.
- Minimality: ranked by distinct runs, then occurrences, then lesson ID, capped at five; the
  prompt section is at most one heading and two lines per lesson.
- Delivery: `memory_slice` and the prompt section only, never the context slice or the payload
  digest.
- Writeback: harvest at run aggregation is an idempotent upsert keyed by semantic fingerprint;
  governance actions change only status or remedy and append history.

## Conflict Handling

- If categories provide contradictory guidance, priority order is:
1. decision-log (latest approved decision)
2. business-rules for acceptance semantics
3. architecture for structural constraints
4. coding-standard for implementation consistency
5. technology-stack and known-issues for operational constraints
6. glossary for terminology normalization

- Unresolved conflicts block execution start for affected state and escalate to tech lead or architect.

## Caching Strategy

- L1 in-run cache keyed by workflow/state/agent/category versions.
- L2 workflow-version cache for repeated states.
- Invalidate on memory file hash changes, governance version changes, or approval status changes.
- Sensitive categories can be marked no-cache.

## Observability

Required logs:

- categories selected and reason codes
- conditional trigger activations
- exclusions and budget-based drops
- writeback decisions and denied writes

Core metrics:

- average categories loaded per state
- memory resolver latency
- conditional expansion rate
- memory-related validation failure rate
- cache hit ratio

## Failure and Recovery

Fail-fast conditions:

- missing required memory category
- required category in invalid lifecycle state
- unresolved category conflict

Recovery actions:

- refresh metadata and retry once for stale reads
- fallback to previous approved memory snapshot when policy allows
- escalate and pause workflow when conflict or compliance violation persists
