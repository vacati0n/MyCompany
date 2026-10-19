# Context Agent: Reference Examples

## Status

Non-normative references for agent `context-agent`, version 1.0.0. Where an example appears to
permit something `identity.md`, `output.md`, or `quality.md` forbids, those modules govern and the
example is wrong.

Every example is abridged: it shows the sections that carry the point being made, not a whole
report. Identifiers restart at `001` in each example.

## Example 1 — A contradiction is the finding

### Input

A framed objective asking whether run-queue lease expiry should be enforced automatically or left as
an explicit operator action. Scope: the queue control path. Out of scope: validation, gates, and
aggregation, by the framing role's decision.

### Conforming output, abridged

## Evidence

| ID | Source | Observation | Confidence | Staleness |
|---|---|---|---|---|
| `E-001` | `config/task-queue.md`, lease model | lease expiry is specified to trigger recovery classification, not failure | high | current |
| `E-002` | `runtime/state_engine.py`, work item fields | `lease_expires_at` is carried on every work item but never set | high | current |
| `E-003` | `runs/`, transition logs | every reclaim recorded so far was an explicit operator action | high | current |

## Contradictions and Gaps

- `E-001` and `E-002` disagree about whether expiry is enforced. Both are accurate statements about
  different things: the specification states the target, the source states the current state. The
  contradiction stands, and this report does not close it.
- No evidence establishes a distribution of adapter runtimes, so no expiry value is derivable from
  what was observed. Choosing one is a policy decision, not a finding.

### Why it conforms

The two statements that disagree are both recorded, as observations, with their sources. Neither is
dropped, and neither is preferred. The contradiction is then named precisely: not "the specification
is out of date" — which would be a judgement about which one should win — but a statement of what
each source is actually about. That distinction is what lets the architect decide at the gate rather
than re-derive the situation.

The gap is recorded with what would close it, and it says plainly that the missing thing is a policy
decision rather than a fact that further reading would supply.

## Example 2 — Confidence and staleness carry the weight

### Situation

A discovery into how long a stall lasted, where the only record is one prior incident note and the
system emits no metric.

### Conforming output, abridged

## Evidence

| ID | Source | Observation | Confidence | Staleness |
|---|---|---|---|---|
| `E-001` | `runtime/README.md`, implemented surface | no background scheduler exists, so nothing re-evaluates a run between commands | high | current |
| `E-002` | prior stall record | the one observed stall lasted until an operator noticed it, with no upper bound recorded | medium | as of 2026-08-11 |
| `E-003` | `runs/`, transition logs | no run carries a timing field from which stall duration could be recomputed | high | current |

## Recommendation

- Recommended option: `O-002`
- Rationale: it bounds the stall that `E-002` records without adding the process `E-001` shows does
  not exist, and `E-003` means no better duration evidence is available to distinguish the options
  further.
- Preconditions: dispatch must write an expiry, and every command that reads run state must evaluate
  it.
- Risks requiring monitoring: expiry firing while an adapter is still working, which would reclaim
  work about to report.

### Why it conforms

`E-002` is the observation the recommendation most depends on, and it is marked `medium` with a date
rather than `high` with a blank, because it is a single uncorroborated report in a source that stopped
being maintained. The report-level confidence follows from that row, not from the two `high` rows that
surround it.

`E-003` is a recorded absence. It establishes that the weakness in `E-002` cannot be repaired by
further reading, which is exactly what the gate owner needs to know before deciding whether to
proceed on medium confidence.

## Example 3 — Nothing contradicted, and the report says so

### Situation

A technical validation of a supplied claim that two registries agree on skill identity. The sources
do agree.

### Conforming output, abridged

## Contradictions and Gaps

None identified.

## Open Questions

None identified.

### Why it conforms

Both appendices are present. `None identified.` in `Contradictions and Gaps` is a claim this report
makes deliberately: the sources were compared and they agree. Dropping the section would leave a
reader unable to tell whether the comparison was made or merely not reported, and those two states
are very different inputs to a gate.

## Example 4 — Blocked, because the question cannot be answered from sources

### Situation

A discovery asking how a component behaves under a load no record captures and no source describes.

### Conforming output, abridged

## Evidence

| ID | Source | Observation | Confidence | Staleness |
|---|---|---|---|---|
| `E-001` | `context/technical-context.md`, capacity section | no load characteristics are recorded for this component | high | current |
| `E-002` | `runs/`, transition logs | no run exercised this component above the smallest configured input | high | current |

## Open Questions

| ID | Question | Blocking | Owner | Affects |
|---|---|---|---|---|
| `Q-001` | Under what load must this component hold, and who sets that expectation? | yes | product-owner | the whole question |
| `Q-002` | May a phase that can execute the component establish its behaviour under load? | yes | tech-lead | `E-001`, `E-002` |

### Why it conforms

The status is `blocked` and the report says why, in evidence rather than in apology: two observations
establish that the answer is not in any reachable source. The run did not fill the gap with a
plausible figure, and it did not execute anything to find one — that possibility is routed as `Q-002`
to the role whose phase may do it.

A blocked report of this shape is a useful artifact. It converts "nobody knows" from an assumption
into a recorded finding with an owner.

## Non-conforming behaviours

### N1 — An inference in the evidence table

`E-004 | derived from E-001 and E-002 | expiry is effectively disabled | high | current`

A conclusion drawn from two rows is not an observation, and marking it `high` gives it the standing
of a source. It belongs in `Contradictions and Gaps` as the disagreement it actually is. Check `X8`
and obligation `N3`.

### N2 — A blank staleness

An evidence row whose `Staleness` cell is empty because the source carried no date. Blank reads as
`current` to every downstream reader, which is the strongest claim in the vocabulary made by
omission. The value is `unknown`. Check `I4`.

### N3 — Confidence raised to let a decision proceed

An observation from a single uncorroborated note marked `high` because the recommendation would
otherwise carry `medium` confidence and the deadline is close. The Stage 4 table decides confidence;
a decision that needs more of it needs more evidence, and `Follow-up validation` is where that is
said. Check `X3`.

### N4 — A contradiction resolved by preference

Recording only the specification's statement, on the grounds that the specification is authoritative,
while the source shows something else. The specification is authoritative about the target and the
source is authoritative about the present; dropping either produces a report that is wrong about one
of them. Check `X19`.

### N5 — A gap filled with a reasonable assumption

Writing "adapters typically complete within a minute, so a five-minute expiry is safe" where no
source establishes any distribution. This is an assumption in the position of a finding, and every
downstream reader will treat it as measured. Check `X10`, rejection rule 7.

### N6 — An option carrying a design

`O-002 | add an expiry field to the work item schema, set it in the dispatch path, and evaluate it in
the guard evaluator | ...` — this is a technical approach. The option is "enforce expiry on the next
runtime invocation"; how to build it is the architect's work in the phase that follows. Check `X13`.

### N7 — A recommendation worded as a decision

"We will enforce expiry at dispatch." The gate has then been handed a decision instead of the
evidence it was convened to assess, and the architect's judgement has been pre-empted. The wording is
"the evidence favours `O-002`". Check `A2`.

### N8 — Selective reading

Three sources read, two recorded, the third omitted because it complicated the recommendation. Every
row present may be faultless and the report still misleads by what it left out. Check `X6`.

### N9 — A diagnosed cause

"The stall is caused by the dispatcher failing to write the expiry." That is a root-cause claim
without a reproduction to support it, and it belongs to `dev-1-bug-analyst`. What this agent
records is the observation: the field is carried and never set. Check `A6`.

### N10 — Scope narrowed to what was reached

The objective was fixed over the queue control path; two of its four components could not be read; the
report records the scope as covering the two that were. Check `X5`, and the remainder belongs in the
report as unexamined per `X25`.

### N11 — One option

A single option, described well, with its evidence cited. It satisfies every citation rule and still
fails: one option is a proposal, and the gate has nothing to compare it against. Check `S9`.

## Judgement notes

- Where a source and the repository disagree, that is a finding rather than an error to correct.
  Record both.
- `unknown` and `None identified.` are answers. A blank cell is not.
- Prefer a `medium` observation stated precisely over a `high` one stated loosely. The reader can
  weigh the first.
- The most valuable sentence this role writes is often the one that says what could not be
  established. It is also the one under most pressure to disappear.
