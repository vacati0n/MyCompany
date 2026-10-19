# Template: Technical Recommendation

## Usage

Canonical artifact template for `technical-recommendation.md`: the single artifact type that
carries a delivery decision's evaluation criteria, its options, its tradeoffs, its risks and
blockers, and the recommendation it closes with.

Six phases across four workflows ask for a technical leadership output. All six now name the
file in their Output Artifact column:

| Workflow | Phase | Producer | Decision Basis |
|---|---|---|---|
| `investigate` | `option-analysis` | `tech-lead` | `option-analysis` |
| `investigate` | `recommendation` | `tech-lead` | `recommendation` |
| `research` | `option-synthesis` | `tech-lead` | `option-analysis` |
| `research` | `recommendation-draft` | `tech-lead` | `recommendation` |
| `review-pull-request` | `merge-decision` | `tech-lead` | `merge-decision` |
| `release` | `readiness-assessment` | `tech-lead` | `release-readiness` |

They are one artifact type. Each is a criteria-based comparison of named options over a stated
decision context, carrying the risks and blockers that bear on delivery and closing with a
recommendation the gate owner decides on, and the Validation Engine
(`runtime/technical_recommendation_validator.py`) judges all of them against this one
contract. The Decision Basis field is what distinguishes an option comparison from a merge
recommendation or a release readiness assessment; the structure does not change with it.

A technical recommendation is not a technical design. A design states *how the system is to be
built* — the structure, the boundaries, the decision records. A recommendation states *which
way delivery should go and at what cost* — the option to take, the sequence to take it in, the
risk carried, and what must be true first. The two produce different artifacts because they
answer different questions and are owned by different roles: structural design belongs to
`architect`, delivery tradeoffs belong to `tech-lead`.

A technical recommendation is also not a gate decision. Every gate this artifact is evidence
for — the Recommendation Gate, the Merge Gate, the Readiness Gate — is decided by another role
under the Producer Exclusion Rule, because `tech-lead` produced the evidence assessed
there. The Readiness section therefore names the deciding authority and recommends to it; it
never records the decision as taken.

Section titles, section order, and identifier schemes are fixed. Sections are never omitted.
A section with nothing to report reads `None identified.`

Identifier schemes: evaluation criteria `EC-nnn`, options `O-nnn`, risks and blockers `RK-nnn`,
open questions `Q-nnn`. All zero-padded to three digits and ascending.

The binding behavioural contract is the module set under `agents/tech-lead/` — `output.md`
for structure and `quality.md` for the checks. Six tech-lead rules are enforced mechanically: a
recommended option must be one the artifact actually evaluated, the assessment summary cannot
drift from the tables it summarises, every evaluated option must be assessed against the
criteria the artifact declared, an unqualified `proceed` cannot sit on top of an open critical
or high blocker, the outstanding blockers named at the close must be exactly the open ones, and
the deciding authority may not be the producer.

---

```yaml
technicalRecommendation:
  recommendationId:
  decisionReference:
  decisionBasis:       # option-analysis | recommendation | merge-decision | release-readiness
  sourceInputs:
    - type:            # investigation-report | technical-design | review-package | validation-report | implementation-report | scope-definition | execution-plan | release-checklist | risk-profile | evaluation-criteria | workflow-constraints | readiness-criteria
      reference:       # supplied reference, or "inline"
  producedBy:          # tech-lead
  agentVersion:
  schemaVersion: 1.0.0
  status:              # complete | provisional | blocked
  recommendedOption:   # an option identifier from the Options table, or "deferred"
  readinessDecision:   # proceed | proceed-with-conditions | do-not-proceed | deferred
  inputDigest:
  contextDigest:
```

## Metadata

- Recommendation ID:
- Decision owner:
- Requested by:
- Decision date:

## Decision Context

- Decision to make:     <!-- the one question this artifact answers, stated as a decision -->
- Delivery constraints: <!-- time, capacity, sequencing, and dependency limits in force -->
- Assumptions in force: <!-- what is taken as true and would change the answer if it were not -->

## Evaluation Criteria

<!-- The criteria every option is judged against, fixed before the options are scored.
     Priority is must-have | high | medium | low. Source names where the criterion came from:
     a scope definition, a design constraint, a workflow rule, or a stated risk appetite. -->

| ID | Criterion | Why it matters | Priority | Source |
|---|---|---|---|---|
| `EC-001` | | | | |

## Options

<!-- At least two options: a single option is a proposal, not an evaluation. `do nothing` and
     `do not proceed` are legitimate options and are recorded as such when they are live.
     Effort is trivial | small | medium | large | unknown. Delivery risk and reversibility use
     the declared vocabularies. Evidence names what the assessment rests on. -->

| ID | Option | Summary | Effort | Delivery risk | Reversibility | Evidence |
|---|---|---|---|---|---|---|
| `O-001` | | | | | | |
| `O-002` | | | | | | |

## Tradeoff Analysis

<!-- One row per option in the table above. Criteria met and criteria missed name `EC-nnn`
     identifiers, so a claim about an option is traceable to the criterion it is a claim
     about. Sequencing implication states what the option forces to happen before or after. -->

| Option | Criteria met | Criteria missed | Strengths | Weaknesses | Sequencing implication |
|---|---|---|---|---|---|
| `O-001` | | | | | |

## Risk and Blocker Register

<!-- What stands between the recommended direction and delivery. A blocker is a risk that has
     already occurred. Severity and likelihood use the declared vocabularies; status is
     open | mitigated | accepted | resolved. Owner names the role that carries it. -->

| ID | Risk or blocker | Severity | Likelihood | Delivery impact | Mitigation | Owner | Status |
|---|---|---|---|---|---|---|---|
| `RK-001` | | | | | | | |

## Assessment Summary

<!-- These figures must recompute exactly from the tables above. -->

- Criteria applied:
- Options evaluated:
- Risks and blockers recorded:
- Blocking items open:      <!-- critical and high entries whose status is open -->

## Recommendation

- Recommended option:   <!-- an option identifier from the Options table, or `deferred` -->
- Rationale:            <!-- why this option beats the others against the stated criteria -->
- Preconditions:        <!-- what must be true before the option is taken, or `None identified.` -->
- Options rejected:     <!-- the option identifiers not recommended, each with the reason -->

## Delivery Impact

- Effort and capacity:      <!-- what taking this option costs, in the declared effort scale -->
- Sequencing constraints:   <!-- what must precede, follow, or run alongside -->
- Dependencies:             <!-- work, teams, or systems this direction depends on -->
- Reversal plan:            <!-- how the direction is unwound if it proves wrong -->

## Readiness

- Recommended decision:         <!-- proceed | proceed-with-conditions | do-not-proceed | deferred -->
- Conditions to satisfy:        <!-- what a conditional recommendation is conditional on -->
- Blocking items outstanding:   <!-- the open critical and high entries, by `RK-nnn`, or `None identified.` -->
- Deciding authority:           <!-- the role that decides the gate; never the producer of this artifact -->

## Open Questions

<!-- `Q-nnn` items, or `None identified.` A provisional or blocked artifact carries at least
     one. A question is recorded here rather than answered by assumption. -->
