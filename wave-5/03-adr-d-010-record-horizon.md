# Architecture Decision Record D-010

## Metadata

- ADR ID: D-010
- Title: A period quantity is final by construction — store-stamped audit entries, a record horizon a datastore check enforces, and availability decided on the datastore's clock
- Date: 2026-09-28
- Status: Proposed
- Owners: omn-tech-lead as the accepting owner of the Design Gate under the producer exclusion rule; architect as the producing agent
- Related Work Items: MC-6; sequencing constraint P-013 of the technical design package DESIGN-2026-0006, which it amends; planner tasks T-009, T-014, T-018 and T-020; the blocking correctness finding of the peer review in this run's quality-review phase

## Context

- Problem statement: The coverage clause of D-003 decided that a period had elapsed against the datastore's read instant, while every entry it counts is stamped by the process clock when written and before its transaction commits. A period could therefore read observed, observed zero included, and afterwards gain an entry stamped inside it; the peer review demonstrated exactly that, and the delivered store suite asserts observed zero over a period ahead of its own stamping clock. The window is commit latency plus any lag of the process clock behind the datastore's. An observed zero that can later become one is the collapse this programme exists to prevent. The named-unit claim has the same cause: availability is set from the process clock and compared against the datastore's, so where the process leads, a unit made available at once is not yet claimable at the next position.
- Business and technical constraints: C-017 requires a period to read observed only when no entry stamped inside it can still commit, enforced rather than made likely; C-004 keeps every quantity in the closed union; C-005 requires the case to be encoded where a check in the datastore admits it; C-003 forbids any server or system setting change; C-014 requires every existing caller and row to keep its meaning; C-001 keeps the five absences unchanged.
- Current architecture baseline: F-038 records the stamp and closure clocks and the demonstrated defect; F-039 records that the entry's hash covers its stamp and is computed before the insert, and that the head is read without a hold; F-040 records that availability is set on the process clock and claimed on the datastore's; F-041 records the datastore's shared, exclusive and non-waiting row holds that the delivered queue claim already relies on; F-016 and F-019 record that every lifecycle transition writes its entry in its own transaction into a record that refuses amendment.

## Decision

- Selected option: O-002, amended by this record.
- Decision statement: One row, the record horizon, holds the instant below which the append-only record is closed. Every audit entry is stamped by the datastore, not the process, as the later of the datastore's clock and the horizon, read while the appender holds the horizon shared; that shared hold lasts until the appender's transaction ends, and the stamp is taken before the hash is computed, so the chain rule is unchanged. A check in the datastore on the append-only record takes the same shared hold and refuses any entry stamped below the horizon, so the property binds every writer, a writer bypassing the appender included. A throughput read obtains the horizon exclusively by a non-waiting attempt, which is granted only when no audited transaction is in flight, raises it to the later of the datastore's clock and its prior value, and takes every count in statements issued after the grant. A period reads observed, observed zero included, only if it starts no earlier than the record's earliest entry and ends no later than the horizon that read established; every other period reads unmeasured because no observation exists. If the attempt fails on each of a bounded number of tries, every period quantity reads unmeasured because the source cannot state one, naming that audited transitions were in flight. For the queue, every writer that makes a unit claimable — enqueue, release for retry, advance — states a delay until claimability, zero or the backoff, and the datastore sets the availability instant from its own clock plus that delay; no port member accepts an availability instant, and the claim and the lease already use the datastore's clock. The item dossier's counts are current-state readings of an accumulating record and claim no permanence; their required statement says so.
- Scope of impact: M-001 holds the horizon and the check in the fifth resource, amended in place because no store the company keeps has applied it; M-037 stamps from the datastore under the shared hold; M-008 and M-004 carry the non-waiting read and the horizon it established; M-010 decides the cases against that horizon; M-003, M-006, M-014 and M-015 move the availability members to the delay form; M-021, M-022 and M-023 move with them.

## Alternatives Considered

1. Stamping each entry at commit time from the datastore's commit timestamp facility, considered within O-002.
- Benefits: the stamp would be the commit instant, so a reader comparing against the datastore's clock would never see a later commit stamped earlier.
- Risks: the facility exists only behind a server setting, which C-003 forbids changing, and the commit instant is known only after commit, so it cannot enter the entry's hash computed before the insert.
- Why not selected: it is not available without a system setting change, and it would break the delivered chain rule.

2. Closing periods on the datastore-assigned sequence number of the append-only record, considered within O-002.
- Benefits: a single monotonic number per entry, assigned by the datastore.
- Risks: the number is assigned at insert, and transactions commit out of sequence order, so a reader can see a later number committed while an earlier one is still in flight.
- Why not selected: sequence order is not commit order, so it reproduces the gap it was meant to close.

3. A settling margin bounded by a stated maximum transaction duration and clock offset, considered within O-002.
- Benefits: no lock and no write on the read path; the smallest code change.
- Risks: it makes a late entry unlikely rather than impossible; the duration bound needs a server setting and the offset bound cannot be enforced; every observed reading is delayed by the margin.
- Why not selected: it keeps a zero honest only probabilistically, where C-017 requires construction.

4. The option-level alternatives O-001, O-003, O-004, O-005 and O-006 of the package evaluation.
- Benefits: each would carry this record unchanged.
- Risks: O-001 violates C-016, O-003 violates C-005 and O-006 violates C-007 on decisions recorded separately; O-004 loses on operability and O-005 on reuse leverage; O-005's counters would need this same finality rule.
- Why not selected: none is rejected on this record, and each is carried so the option set stays whole.

## Consequences

- Positive outcomes expected: an observed period, observed zero included, is permanent whatever the offset between the clocks and whatever step the datastore's clock takes later, because no entry can commit stamped below a horizon a reader has set; the property is enforced in the datastore for every writer; a unit made available at once is claimable at once on any deployment, so a drive no longer depends on clock discipline between the process and the datastore.
- Tradeoffs accepted: every audited transaction takes a shared hold on one row until it ends, which does not serialize appenders against one another; a throughput read writes that row and holds it exclusively for its count statements; a read that never finds a quiet instant returns unmeasured rather than waiting; audit stamps come from the datastore's clock rather than the process clock; the analytics surface's read of throughput is no longer free of writes, though it writes nothing into the append-only record.
- Risks introduced: R-021, reads unmeasured under sustained load; R-022, a consumer ordering a datastore stamp against a process instant; R-023, a test double diverging from the datastore or a hold order forming a cycle.

## Validation Plan

- Metrics to monitor:
  - The count of entries in the append-only record stamped below the record horizon in force when they committed; the expected value is zero, and the datastore check refuses any such entry.
  - The count of throughput periods read observed whose count differs on any later read; the expected value is zero.
  - The count of drives resting unclaimed at a position whose unit was released as immediately claimable; the expected value is zero.
- Verification checkpoints:
  - A period read as observed zero claims against a live throwaway store, followed by a claim committed through the delivered services, which is stamped at or after the horizon, and a direct insert stamped inside the period, which the datastore refuses; a second read of the period still reads observed zero.
  - An audited transaction held open while a throughput read is attempted, showing the read returning unmeasured with the in-flight reason, and, after the transaction ends, a read counting its entry.
  - The entry point driven with the injected process clock set well ahead of and well behind the datastore's clock, reaching the terminal claim state over the fixture store in both cases.
- Rollback or reversal conditions:
  - Reverse if the shared hold is shown to form a deadlock cycle with another hold the delivered transactions take, by moving appenders to an exclusive hold on the horizon under Q-010, which serializes them and removes every cycle through this row.
  - Reverse to unmeasured-only throughput, removing the observed case entirely, if any entry is found committed below a horizon a reader set, since the property would then not hold by construction and no weaker form is acceptable.

## Approval

- Architect: left for the Design Gate; the producing agent does not sign its own record
- Tech Lead: acceptance rests here under the producer exclusion rule
- Product Owner (if scope-impacting): not required; this record decides how an existing obligation is kept, not product scope
