```yaml
requirementFraming:
  framingId: FRAME-2026-0007
  subject: Bulk refund handling for partially fulfilled orders
  sourceInputs:
    - type: business-intent
      reference: inline
    - type: problem-statement
      reference: inline
    - type: product-context
      reference: context/product-context.md
  producedBy: business-analyst
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  framingVerdict: framed
  requirementCount: 5
  inputDigest: sha256:fixture-input-not-run-produced
  contextDigest: sha256:fixture-context-not-run-produced
```

## Metadata

- Subject: Bulk refund handling for partially fulfilled orders
- Requested by: commerce operations lead
- Decision owner: product-owner
- Workflow phase: problem-framing
- Framing date: 2026-08-19

## Business Context

- Business intent: give operations a way to refund the unfulfilled part of an order without cancelling the whole order.
- Problem statement: when only part of an order ships, operations must cancel and re-create the order to refund the rest, which loses the fulfilment history and delays the customer refund.
- Affected stakeholders: commerce operations agents, finance reconciliation, customers awaiting a partial refund.
- Current-state pain: roughly 120 orders a month are cancelled and rebuilt by hand, and each rebuild breaks the reconciliation link finance depends on.

## Target Outcomes

| ID | Outcome | Measure | Business Driver |
|---|---|---|---|
| `O-001` | Operations refunds an unfulfilled order line without cancelling the order | Share of partial refunds handled without an order rebuild, monthly | Operational cost |
| `O-002` | Finance reconciles a partial refund to its original order | Share of refunds reconciled without manual matching, monthly | Financial control |

## Requirements

| ID | Requirement | Type | Outcome Ref | Priority |
|---|---|---|---|---|
| `R-001` | An operations agent can refund one or more unfulfilled lines of an order while the order stays open | functional | `O-001` | High |
| `R-002` | A refunded line records the amount refunded and the agent who authorised it | functional | `O-002` | High |
| `R-003` | A partial refund keeps the order's fulfilment history intact and attributable | functional | `O-002` | High |
| `R-004` | A refund that fails part way leaves the order in the state it held before the attempt | non-functional | `O-001` | High |
| `R-005` | A refund total can never exceed the amount the customer paid for the affected lines | non-functional | `O-002` | High |

## Acceptance Intent

| ID | Acceptance Intent | Requirement Ref | Demonstrated By | Priority |
|---|---|---|---|---|
| `AI-001` | An order with shipped and unshipped lines is partially refunded and remains open | `R-001` | The order state after refunding an unshipped line | High |
| `AI-002` | A refunded line shows its refund amount and the authorising agent | `R-002` | The refund record attached to the line | High |
| `AI-003` | The fulfilment history of a partially refunded order is unchanged and still attributable | `R-003` | The order history before and after the refund | High |
| `AI-004` | An interrupted refund leaves no partial change on the order | `R-004` | The order state after a refund interrupted mid-way | High |
| `AI-005` | A refund attempt above the paid amount for the affected lines is refused | `R-005` | The outcome of an over-refund attempt | High |

## Framing Boundaries

| ID | Excluded Concern | Reason | Revisit Trigger |
|---|---|---|---|
| `B-001` | Refunds for orders that shipped in full | The supplied intent covers partially fulfilled orders only | Operations reports full-order refunds sharing the same rebuild cost |
| `B-002` | Customer-initiated self-service refunds | The intent names operations agents as the actor throughout | A self-service refund request enters the product context |

## Assumptions

| ID | Assumption | Basis | Confidence | Impact If False |
|---|---|---|---|---|
| `AS-001` | The 120 monthly rebuilds are all partial-fulfilment cases | Stated in the supplied intent as partially fulfilled orders | Medium | The measure on `O-001` counts a population the framing does not cover |
| `AS-002` | Finance reconciles against the original order reference rather than the refund reference | Supplied product context describes reconciliation keyed to the order | Medium | `R-003` and `O-002` target the wrong reconciliation key |

## Open Questions

| ID | Question | Blocking | Owner | Needed By |
|---|---|---|---|---|
| `Q-001` | Should a partial refund be permitted while a line is in transit but not yet delivered? | no | product-owner | Scope Gate |

## Handoff

- Downstream owner: context-agent, for technical discovery against the current order and refund model
- Gate: Framing Gate
- Evidence for the gate: five requirements, each traced to a declared outcome and bounded by an acceptance intent, with two assumptions carrying their basis
- Deferred to downstream: refund thresholds and approval limits to product-owner, the refund and reconciliation mechanism to architect
