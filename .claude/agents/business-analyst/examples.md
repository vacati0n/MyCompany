# Business Analyst: Reference Examples

## Status

Non-binding references for `business-analyst`, version 1.0.0. Where an example appears to
disagree with `output.md` or `quality.md`, those modules govern and the example is wrong.

These are fragments, not whole artifacts. They exist to fix the distinction the role turns on:
between a condition that must be true and a mechanism that would make it true, and between a
gap that was recorded and a gap that was closed by inference.

## Example 1 — A complete framing

### Supplied input

```
business-intent: "Customers abandon checkout when their saved address fails validation.
Support gets around 40 calls a week about it. We want fewer abandoned checkouts."
product-context: context/product-context.md
```

### Conforming fragments

```yaml
requirementFraming:
  framingId: FRAME-2026-0001
  subject: Checkout abandonment on saved-address validation failure
  sourceInputs:
    - type: business-intent
      reference: inline
    - type: product-context
      reference: context/product-context.md
  producedBy: business-analyst
  agentVersion: 1.0.0
  schemaVersion: 1.0.0
  status: complete
  framingVerdict: framed
  requirementCount: 4
```

**Target Outcomes**

| ID | Outcome | Measure | Business Driver |
|---|---|---|---|
| `O-001` | Fewer checkouts are abandoned at the address step | Abandonment rate at the address step, weekly | Revenue recovery |
| `O-002` | Fewer support calls about address validation | Address-validation call volume, weekly | Support cost |

**Requirements**

| ID | Requirement | Type | Outcome Ref | Priority |
|---|---|---|---|---|
| `R-001` | A customer whose saved address fails validation can correct it without leaving checkout | functional | `O-001` | High |
| `R-002` | A validation failure states which part of the address was rejected | functional | `O-002` | High |
| `R-003` | A transient validation failure does not end the checkout session | non-functional | `O-001` | High |
| `R-004` | A customer can complete checkout with an address the validator cannot confirm, once they confirm it themselves | functional | `O-001` | Medium |

**Acceptance Intent**

| ID | Acceptance Intent | Requirement Ref | Demonstrated By | Priority |
|---|---|---|---|---|
| `AI-001` | A customer with a failing saved address reaches payment without restarting checkout | `R-001` | A completed checkout following a validation failure | High |
| `AI-002` | The failure message identifies the rejected address component | `R-002` | The message shown on a rejected postcode | High |
| `AI-003` | A checkout in progress survives a validator outage | `R-003` | A checkout completed while validation is unavailable | High |
| `AI-004` | An unconfirmable address can be accepted on customer confirmation | `R-004` | A completed checkout using a customer-confirmed address | Medium |

**Assumptions**

| ID | Assumption | Basis | Confidence | Impact If False |
|---|---|---|---|---|
| `AS-001` | The 40 weekly calls concern saved addresses rather than newly entered ones | Stated in the supplied intent as "saved address" | Medium | `O-002` targets the wrong call population and its measure misreports |

### Why it conforms

- Every requirement names an outcome, and both outcomes are declared.
- Every requirement carries a type, and `R-003` is correctly typed non-functional: it is a
  property of how the system behaves under failure, not a behaviour a customer performs.
- Every requirement is bounded by an acceptance intent, so `RF6` passes.
- `R-003` states the condition — the session survives — and not the mechanism. Whether that is
  achieved by retry, queue, or cache is the architect's sentence to write.
- Each acceptance intent states what would be shown, not the test that shows it and not a
  numeric threshold. The threshold on `O-001` belongs to `product-owner`.
- `AS-001` carries a basis drawn from the supplied text, so it is an assumption rather than an
  invention.

## Example 2 — A partial framing

### Supplied input

```
problem-statement: "Reporting is too slow. Make it faster."
```

### Conforming fragments

```yaml
  status: provisional
  framingVerdict: partially-framed
  requirementCount: 1
```

**Requirements**

| ID | Requirement | Type | Outcome Ref | Priority |
|---|---|---|---|---|
| `R-001` | A report returns within a bound the business has agreed | non-functional | `O-001` | High |

**Open Questions**

| ID | Question | Blocking | Owner | Needed By |
|---|---|---|---|---|
| `Q-001` | Which reports are in question, and how slow are they today? | yes | `context-agent` | Technical Gate |
| `Q-002` | What response time would the business accept? | yes | `product-owner` | Framing Gate |

### Why it conforms

The intent supports exactly one requirement and no threshold. Recording `R-001` as "a report
returns within two seconds" would have invented the number the business never gave, so the
bound is named as agreed-but-unset and `Q-002` carries the gap to the role that owns it. Two
blocking questions stand, so the verdict is `partially-framed` and `RF7` is satisfied by the
recorded rows.

## Example 3 — Non-conforming behaviour

Each fragment below fails a named check.

### Recording a mechanism as a requirement

```
| `R-005` | The address service caches validation results for 24 hours | functional | `O-001` |
```

This states how, not what. The condition the cache exists to satisfy — repeated validation of
an unchanged address does not delay checkout — is the requirement. The cache, if the inputs
supplied it, is a constraint with its source named. Fails the `what, never how` invariant and
rejection rule 4.

### Recording a requirement that serves no outcome

```
| `R-006` | The admin console lists recent validation failures | functional | `O-004` |
```

`O-004` is not defined in Target Outcomes. Fails `C6.2` and `RF3`. Either the outcome was
never declared, or this requirement belongs to another change.

### Recording an untestable requirement

```
| `R-007` | The checkout experience feels responsive | non-functional | `O-001` |
```

Nothing would demonstrate it, so no acceptance intent can bound it and `RF6` fails. The
conforming record is an open question asking what responsiveness the business would accept.

### Writing the threshold

```
| `AI-005` | The report returns in under 2.0 seconds at the 95th percentile | `R-001` |
```

The threshold and the percentile are acceptance criteria, owned by `product-owner`, and
choosing the percentile is verification design owned by `qa`. The conforming intent states
that the report returns within the agreed bound, and leaves the number to the role that sets
it.

### Recording an assumption with no basis

```
| `AS-002` | Customers would accept a slower checkout for better validation | — | Low |
```

No basis, so it is an invented requirement in disguise. Fails `RF9`. The conforming record is
an open question to `product-owner`.

### Claiming the verdict

```
  framingVerdict: framed
```

with two blocking open questions recorded. Fails `RF7` and rejection rule 8. A framing with
blocking questions outstanding is `partially-framed`, whatever the pressure to call it done.

### Deciding the mechanism, the scope, or the gate

```
## Handoff
- Gate: Framing Gate — approved
```

Recording the gate decision on this artifact fails rejection rule 7 and the Producer Exclusion
Rule. The Framing Gate on this artifact is decided by `product-owner`.

### Issuing downstream identifiers

```
| `T-001` | Update the address validation client | ... |
| `ADR-004` | Adopt asynchronous validation | ... |
```

Task breakdown belongs to `planner` and decision records belong to `architect`. Fails `RF11`.
