# Workflow Gate Matrix

## Purpose

Map each workflow to mandatory gate ownership for consistent quality decisions.

| Workflow | Gate | Required Owners |
|---|---|---|
| implement-feature | Scope Gate | product-owner, business-analyst |
| implement-feature | Planning Gate | tech-lead, orchestrator |
| implement-feature | Design Gate | architect, tech-lead |
| implement-feature | Review Gate | dev-2-reviewer, qa |
| implement-feature | Verification Gate | qa |
| implement-feature | Closure Gate | orchestrator, documentation |
| fix-bug | Triage Gate | dev-1-bug-analyst, tech-lead |
| fix-bug | Fix Gate | dev-2-reviewer |
| fix-bug | Verification Gate | qa, dev-2-reviewer |
| fix-bug | Closure Gate | orchestrator, documentation |
| investigate | Framing Gate | business-analyst, product-owner |
| investigate | Technical Gate | context-agent, architect |
| investigate | Recommendation Gate | tech-lead, orchestrator |
| research | Framing Gate | business-analyst, product-owner |
| research | Technical Validity Gate | context-agent, architect |
| research | Recommendation Gate | tech-lead, orchestrator |
| refactor | Invariant Gate | architect, tech-lead |
| refactor | Implementation Gate | dev-2-reviewer |
| refactor | Regression Gate | qa, dev-2-reviewer |
| refactor | Closure Gate | orchestrator, documentation |
| code-quality-scan | Quality Handoff Gate | dev-2-reviewer, tech-lead |
| review-pull-request | Code Quality Gate | dev-2-reviewer, tech-lead |
| review-pull-request | Architecture Gate | architect, tech-lead |
| review-pull-request | Verification Gate | qa, dev-2-reviewer |
| review-pull-request | Merge Gate | tech-lead, orchestrator |
| release | Readiness Gate | tech-lead, qa |
| release | Artifact Gate | dev-2-reviewer, tech-lead |
| release | Deployment Gate | orchestrator, tech-lead |
| release | Communication Gate | documentation, product-owner |

Every gate named by an active workflow Phase Model appears here, and every row names at least
one owner that does not produce the evidence the gate assesses. A gate whose only owner is the
producing role cannot be decided at all, because the Producer Exclusion Rule below forbids
self-approval, so single-owner rows were completed rather than left as latent deadlocks.

### Rows Added or Completed for Producer Exclusion

| Workflow | Gate | Change | Reason |
|---|---|---|---|
| implement-feature | Review Gate | added qa as second owner | dev-2-reviewer owns `quality-review` and produces the findings log the gate assesses |
| fix-bug | Verification Gate | added dev-2-reviewer as second owner | qa owns `regression-validation` and produces the verification evidence |
| fix-bug | Closure Gate | row added | `closure-and-communication` closes with a gate the matrix did not carry |
| investigate | Recommendation Gate | row added | `recommendation` closes with the gate the workflow specification already named |
| research | Framing Gate, Technical Validity Gate | rows added | both gates were named by the workflow specification but absent here |
| refactor | Implementation Gate, Closure Gate | rows added | both gates were named by the workflow specification but absent here |
| refactor | Regression Gate | unchanged, already carried two owners | qa owns `behavioral-validation`, so the decision rests with the dev-2-reviewer entry this row already named |
| review-pull-request | Code Quality Gate, Architecture Gate, Verification Gate, Merge Gate | rows added | the single `Readiness Gate` row this matrix carried named no phase in the workflow's Phase Model; these four are the gates the state machine and the specification declare |
| review-pull-request | Code Quality Gate, Architecture Gate, Verification Gate | second owner added | the first-listed owner of each produces the evidence its gate assesses |
| release | Artifact Gate, Communication Gate | rows added | both gates were named by the workflow specification but absent here |
| release | Artifact Gate, Deployment Gate | second owner tech-lead | dev-2-reviewer owns `artifact-packaging` and orchestrator owns `deployment-execution` |
| code-quality-scan | Quality Handoff Gate | row added with the workflow | dev-2-reviewer owns `repository-quality-scan` and produces the scan package this gate assesses; the decision rests with tech-lead, the human handoff the workflow terminates at |

The retired row is `review-pull-request | Readiness Gate | dev-2-reviewer`. Review
readiness is an entry condition of that workflow, not a gate between two of its phases, and no
Phase Model row referenced it.

## Usage

Use this matrix when validating whether an output can move to the next phase.

The Planner Agent produces the evidence assessed at the implement-feature Planning Gate.
It does not approve that gate, or any other. Gate approval remains with the owners listed
above.

The Architect Agent produces the evidence assessed at the Design, Invariant, Technical,
Technical Validity, and Architecture Gates, including architecture decision records emitted at
status `Proposed`. Accepting those records is a gate decision owned by the listed gate owners,
never by the producing agent. Gate owner entries naming `architect` name the architecture
role now implemented by `architect`; the reference migration is deferred.

A gate is resolvable when this matrix names its workflow and gate. The runtime reads ownership
from here and never invents a decision, so a Phase Model that names a gate absent from this
matrix would hold its successor blocked with no owner able to release it.

## Producer Exclusion Rule

An agent may not approve a gate for an artifact it produced, even when its role appears in
that gate's owner list. Where the producing role is also a listed owner, approval requires a
different listed owner.

This applies today to the Design and Invariant Gates: `architect` produces the technical
design package and the decision records assessed there, so acceptance rests with
`tech-lead`. It also decides the second owner on every row listed in Rows Added or
Completed for Producer Exclusion above. The same rule applies to any future agent whose role
owns a gate its own output must pass.

The rule reads through role aliases. A gate owned by `architect` cannot be approved by
`architect`, because both identifiers name one role.
