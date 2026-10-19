# Skill Resolver Specification

## Purpose

Define the runtime resolver that maps agents to reusable engineering skills and ensures each execution loads only the skills required for the active workflow state.

## Scope

The resolver is responsible for:

- resolving mandatory and optional skills for each agent execution
- enforcing workflow-phase skill requirements
- minimizing unnecessary skill loading
- validating compatibility, version, and governance constraints
- producing auditable skill-loading decisions

## Design Principles

- Deterministic: same inputs produce the same resolved skill set.
- Minimal: load mandatory skills first and load optional skills only when justified.
- Governed: all decisions respect skill governance and policy constraints.
- Traceable: every included or excluded skill has recorded rationale.

## Inputs

- workflow id and version
- workflow state id
- task intent
- primary agent id
- participating secondary agents (if any)
- agent-skill matrix
- workflow phase required skills
- runtime risk profile and policy flags

## Outputs

- resolved primary skill set
- resolved secondary skill set
- excluded skills with exclusion reasons
- effective skill versions and compatibility status
- skill resolution digest for audit and caching

## Resolver Architecture

### Components

- Skill Request Classifier: classifies execution into a resolver profile.
- Matrix Resolver: reads agent coverage from agent-skill matrix.
- Phase Requirement Resolver: reads mandatory skills for the active workflow phase.
- Policy Filter: applies governance, risk, and compliance constraints.
- Minimality Optimizer: removes low-signal, non-required skills.
- Compatibility Validator: verifies skill version and dependency compatibility.
- Skill Cache: reuses safe resolution results.
- Audit Emitter: records resolution decisions and evidence.

### Runtime Placement

The resolver executes during runtime hydration before agent execution begins.
The resolved skill bundle is attached to the state execution contract.

## Resolution Model

### Step 1: Build Candidate Set

Construct candidate skills from:

- workflow phase required skills (highest priority)
- primary agent Primary and Secondary coverage
- risk-triggered mandatory skills (for example security, testing)

### Step 2: Apply Priority

Priority tiers:

1. Mandatory by workflow phase or policy
2. Primary coverage for active agent
3. Secondary coverage for active agent
4. Advisory coverage and optional enrichments

### Step 3: Enforce Minimality

Remove any candidate not required by one of:

- phase requirement
- policy requirement
- dependency requirement for an included skill
- explicit escalation requirement

### Step 4: Validate Compatibility

For remaining skills, validate:

- skill file availability
- version compatibility with workflow/runtime policy
- dependency closure

If validation fails, trigger fallback resolution or escalation.

### Step 5: Produce Effective Bundle

Emit ordered skill set with:

- skill id
- category
- source rule (phase, primary, secondary, policy, dependency)
- version
- inclusion rationale

## Agent Mapping Rules

- One execution has one primary agent owner.
- Primary agent receives full mandatory bundle for the state.
- Secondary agents receive only the subset needed for their gate/review responsibility.
- Advisory-only skills are not loaded unless elevated by policy or risk triggers.

## Minimal Skill Loading Policy

### Mandatory vs Optional

- Mandatory: required by workflow phase, governance policy, or dependency graph.
- Optional: advisory enhancements not required for the current state outcome.

### Loading Strategy

- Start with mandatory skills only.
- Permit one controlled expansion pass if validation indicates missing capability.
- Keep per-state skill budget caps to avoid context bloat.

### Expansion Triggers

Load additional skills only when:

- output schema validation fails due to missing methodological support
- gate criteria require a skill not initially loaded
- orchestrator escalation explicitly requests broader analysis

### Domain-conditional skill reading

Resolution and reading are two decisions. Every phase-mandatory code and every code an agent
manifest declares is *resolved* on every dispatch, exactly as above, and an unresolvable code
still blocks the phase. Whether the resolved skill file is *read* depends on whether its domain
is affected by the task, which the runtime derives into the task context's `affected_areas`
and records per code in the envelope's `skill_dispatch`.

| Code | Skill | Area | Read when |
|---|---|---|---|
| S01, S02, S03, S07, S10, S11, S12 | architecture, business, .NET, testing, git, logging, error handling | domain-general | always, when the phase or manifest names it |
| S04 | Avalonia UI Engineering | `avalonia-ui` | the task names Avalonia, XAML, a view-model boundary, or desktop UI |
| S05 | React Engineering | `react-frontend` | the task names React, JSX or TSX, hooks, or a frontend surface |
| S06 | Database Engineering | `database` | the task names a database, schema or migration, an ORM, a persistence layer, or a transaction boundary |
| S08 | Performance Engineering | `performance` | the task names latency, throughput, a benchmark, a percentile, a load test, or profiling |
| S09 | Security Engineering | `security` | the task names authentication, authorization, secrets, credentials, permissions, encryption, injection, or another security term; **always** in review and validation phases |

Rules:

- An area is affected when its trigger matches a supplied input or an accepted upstream
  artifact, or when the operator declares it with `--affected-area`. Declared areas are
  additive and persist on the run.
- When no text is available to derive affectedness, every conditional code is read. Unknown
  never narrows.
- A `not-triggered` skill stays resolved. An agent whose work reveals the domain reads it then
  and records the domain as affected in its artifact.
- The trigger table is data (`DOMAIN_SKILLS` in `runtime/task_context.py`); widening a
  trigger is a runtime change, narrowing one is a governance change and needs a change
  proposal.

## Conflict Resolution

### Conflict Types

- two rules select incompatible versions of the same skill
- policy forbids a skill selected by agent preference
- duplicate skill intent from different categories

### Resolution Order

1. governance policy and compliance constraints
2. workflow phase mandatory requirements
3. runtime stability policy (pinned compatible versions)
4. agent preference defaults

### Unresolved Conflict Handling

- mark resolution as blocked
- escalate to architect or tech lead role
- prevent execution start for affected state

## Caching Strategy

### Cache Levels

- L1: in-run resolution cache by state and agent
- L2: workflow-version cache for repeated phase executions
- L3: shared cross-run cache keyed by matrix/policy/version digest

### Cache Key

- workflow version
- state id
- agent id
- risk profile
- matrix hash
- policy version

### Invalidation

Invalidate cache on:

- changes in agent-skill matrix
- changes in skill governance or runtime policy
- changes in workflow version or phase requirements
- missing skill artifact or version deprecation

## Failure Handling

### Fail-Fast Conditions

- mandatory skill artifact missing
- incompatible mandatory skill version
- policy-blocked mandatory skill with no approved alternative

### Recovery Actions

- fallback to compatible pinned version when allowed
- retry resolution after metadata refresh
- escalate for manual override decision

## Observability

### Logs

- candidate skill set and source rules
- filtered/excluded skills and reasons
- compatibility checks and failures
- cache hit/miss and invalidation reason
- final effective skill bundle

### Metrics

- average skills loaded per execution
- mandatory-to-optional ratio
- resolver latency
- cache hit ratio
- skill-related execution failure rate
- expansion-pass frequency

## Future Optimization

- adaptive skill ranking using historical execution success
- dependency graph pruning by state outcome type
- predictive skill prefetch for next likely workflow states
- confidence-aware optional skill activation
- policy-aware semantic deduplication across overlapping skills
