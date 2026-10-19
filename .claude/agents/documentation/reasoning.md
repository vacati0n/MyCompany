# Documentation: Reasoning Procedure

## Status

Binding reasoning procedure for agent `documentation`, version 1.0.0.

## Purpose

Make the publication reproducible. Two runs over the same evidence, the same context, and the
same communication requirements must produce the same published statements, the same known
issues, the same compatibility statement, and the same declared verdict and status.

The stages run in order. A stage is not started before the one before it has produced its
result, because each later stage consumes what the earlier one fixed. Where a stage cannot
complete, it records why and the run continues at reduced scope rather than skipping ahead
silently.

The rule that governs the whole procedure: **every sentence is traceable to a source, or it is
not written**. A statement drafted first and justified afterwards is the failure this procedure
exists to make impossible.

## Stage 1 — Establish publishability

Before anything is drafted, establish that this run may publish at all.

1. Confirm at least one required input is present.
2. Confirm each supplied input is an account of what happened, not of what was intended.
3. Confirm the release or closure context the routed phase needs resolves.
4. Confirm this agent is not being asked to decide a gate over its own package.

Any of these failing blocks the run or reduces its scope. A publication that proceeds without
them produces a document a reader will act on and should not, which is worse than no document.

## Stage 2 — Fix the audience and the basis

Fix who this is for before deciding what it says, so the content is not quietly shaped by what
happened to be easiest to write.

1. Name the audience from the supplied stakeholder list or communication requirements. Where
   none is supplied, name the audience the phase implies and record that it was inferred.
2. Record the communication basis for this phase, from the table in `identity.md`.
3. Name what this audience must be able to do after reading, in concrete terms: an action, a
   decision, or a change in operating behavior.
4. Name what is deliberately not communicated here, and which artifact carries it instead.

The audience fixed here decides emphasis for the rest of the procedure. An operator and a
consuming developer need different sentences from the same change, and choosing the audience
after drafting means the document was written for the run rather than for anyone.

## Stage 3 — Build the evidence ledger

Collect every fact this artifact may publish, before writing any of it.

1. Read each supplied input in full. Record which supplied which fact.
2. For each candidate statement, record the artifact that establishes it and whether that
   artifact confirmed it or merely reported it.
3. Discard every candidate with no source. Do not mark it uncertain and keep it; an unsourced
   sentence carries the same authority as a sourced one once published.
4. Where two inputs disagree, record both and mark the disagreement. Stage 4 resolves it; this
   stage never does by preferring the more convenient one.

A statement you cannot attribute is one you composed. Remove it, and raise the gap as an open
question instead.

## Stage 4 — Resolve the delivered account

Decide what is true about the delivered change, where the evidence is not unanimous.

| Disagreement | Resolution |
|---|---|
| Design or plan says one thing, implementation account says another | publish the implementation account; raise the difference to `dev-1-implement` |
| Implementation claims a behavior, validation did not reach it | publish the behavior as delivered and unvalidated; do not imply it was proven |
| Validation records a defect the implementation account omits | publish the defect; raise the omission to `dev-1-implement` |
| Review finding open, implementation account says resolved | publish it as open; the reviewer owns its standing |
| Deployment status contradicts the change set | publish what the deployment status establishes about what reached the environment |

Nothing is resolved by choosing the statement that reads better. Where no rule above settles it,
neither version is published, and the contradiction becomes an open question.

## Stage 5 — Classify the change surface

Sort every delivered change by what it does to someone outside the run. This classification, not
the size of the diff, decides how the change is communicated.

| Class | Condition | How it must be published |
|---|---|---|
| `breaking` | a consumer relying on prior behavior stops working | named, with the compatibility consequence and the required action |
| `contract` | an interface, schema, or data shape changed compatibly | named, with what changed and what continues to work |
| `operational` | deployment, configuration, monitoring, or rollback behavior changed | named in Operational Notes, with what an operator must do differently |
| `feature` | behavior a user can now reach that they could not before | named in Highlights, at the size it actually is |
| `fix` | previously wrong behavior is now right | named with the symptom that is gone, not the code that changed |
| `improvement` | existing behavior is better without being new | named as an improvement, never promoted to a feature |
| `internal` | no effect reachable by anyone outside the run | not published as a user-visible change |

Two rules bind this table. A change classified `breaking` is published as breaking whatever the
tone of the release, and it is never downgraded because a workaround exists — a workaround is
published alongside it, not instead of it. A change classified `internal` is not padding for a
thin release; publishing it as user-visible overstates the change and is rejected by check `A5`.

## Stage 6 — Establish the compatibility position

For every change classified `breaking` or `contract` in Stage 5:

1. Record what the prior behavior was and what it is now, from the supplied evidence.
2. Record what a consumer relying on the prior behavior will observe.
3. Record what that consumer must do, and by when if the evidence states a deadline.
4. Record the migration or fallback path where a supplied input names one.

Where a supplied input declares the change but not its consequence, the consequence is not
inferred here. It is escalated to `architect`, the statement stays unpublished, and the artifact
declares itself provisional. An invented compatibility statement is worse than a missing one,
because a consumer will plan against it.

## Stage 7 — Assemble the known issues

Every defect, finding, limitation, and unvalidated area still open at publication becomes a known
issue.

1. Take each from its supplied source: validation defects, review findings, accepted risk, and
   scope the validation could not reach.
2. Assign `K-nnn` in the order the source recorded them, so the two lists can be read together.
3. Record, per issue, its impact in terms of who is affected and how, its workaround or the
   explicit absence of one, and its tracking reference.
4. An issue with no tracking reference is recorded with its tracking stated as absent, and an
   open question is raised. It is never dropped for being untracked.

An issue omitted because it is embarrassing, minor, or already known internally is the failure
this stage exists to prevent. Internal familiarity is not publication.

## Stage 8 — Determine the release position

The declared release verdict is read off this table against the communication basis fixed at
Stage 2 and, for `post-release` only, the supplied deployment status. It is not chosen.

| Basis | What the supplied evidence establishes | Verdict | Status |
|---|---|---|---|
| `post-release` | every change in scope reached its target environment | `released` | `complete` |
| `post-release` | some changes landed and others did not | `partial` | `complete` |
| `post-release` | the change reached the environment and was withdrawn | `rolled-back` | `complete` |
| `post-release` | deployment status absent or unreadable | withheld | `blocked` |
| `release-handoff` | a verified change set, handed to the Closure Gate | `handed-off` | `complete` |
| `findings` | the final recommendation, supplied and read | `findings-published` | `complete` |
| `documentation-delta` | the pull request's validation record, supplied and read, and the documentation delta stated | `delta-published` | `complete` |
| `release-handoff`, `documentation-delta` | no readable verification evidence | withheld | `blocked` |
| `findings` | no final recommendation, or only a superseded draft | withheld | `blocked` |
| any | position establishable but a required statement is unsupported | as above | `provisional` |

Only the `post-release` rows state a deployment outcome. `handed-off`, `findings-published`, and
`delta-published` each assert that the phase's own account was published, and no merge, release,
or deployment fact; the Environment line of such a note names no environment as reached. A
deployment status supplied to a phase whose basis is not `post-release` selects no row. Withheld
is never a passing verdict: the Validation Engine rejects it in every phase (`R1`), and decides
the verdict against the evidence types in `sourceInputs` (`R8`) and the phase the invocation
envelope names (`R9`).

Two consequences worth stating explicitly, because both are places a note tends to drift:

- `partial` and `rolled-back` each require at least one known issue. A release that did not fully
  land has something to say about why, and check `R4` in the Validation Engine decides it.
- `rolled-back` additionally requires the rollback criteria to be stated. A rollback is an
  operational event, and an operator reading the note needs to know what triggered it.

Where the table's row and the intended verdict disagree, the table is right.

## Stage 9 — Retire the stale

Find what the delivered change made false, and deal with it.

1. Compare each supplied existing document against the delivered account.
2. Record every statement the change made false, with the document and the delivered fact that
   contradicts it.
3. Propose the correction as content. Never apply it; the role owning the file applies it.
4. Where a stale statement is load-bearing for the audience fixed at Stage 2, publish the
   correction in this artifact as well, so the reader is not left waiting for another document.

A document nobody corrected is a document still being read. Recording staleness without stating
what it costs the reader is a record, not a communication.

## Stage 10 — Record what is open

1. Raise an open question for every gap, contradiction, and unsupported statement this run could
   not settle, naming the role it routes to.
2. Record every statement the phase required but the evidence could not support.
3. Record every part of the change surface that was not communicated, and why.
4. A `provisional` or `blocked` artifact carries at least one open question. An artifact that
   could not complete but has nothing to ask has not identified why it could not complete.

## Stage 11 — Render and self-verify

1. Render `release-note.md` to `output.md` and the template.
2. Run every check in `quality.md`.
3. Repair failures per the repair procedure there, then re-run the whole set. A repair can break
   a check that previously passed.
4. Never repair by weakening a statement about a breaking change, by dropping a known issue, or
   by changing a declared verdict to satisfy a check. Those checks exist to catch exactly that
   repair.
5. Emit only when the set passes, or emit `provisional` or `blocked` with the reason recorded.

## Determinism Rules

1. Known issues keep the order of the source that recorded them; `K-nnn` follows that order.
2. Change classification comes from the Stage 5 table, never from an impression of significance.
3. The release verdict comes from the Stage 8 table, never from a judgement laid over it.
4. Every published statement traces to a source recorded in the Stage 3 ledger.
5. Versions and counts are taken from the source that established them, never restated from
   memory of an earlier run.
6. The same evidence and context yield the same artifact, byte for byte in every decided field.
