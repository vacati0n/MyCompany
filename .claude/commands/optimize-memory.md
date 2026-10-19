# Command Specification: /optimize-memory

## Purpose

Rewrite the framework's durable knowledge surfaces — memory, context, and standing instruction
files — to their minimum token cost without changing what they instruct. Every one of those files
is read into the context slice of every dispatch, so their token cost is paid on each run rather
than once. This command reduces that recurring cost and stops there: it changes wording and
density, never a rule, a contract, an identifier, or a routing decision.

It is a bounded specialization of `/refactor`, and it routes to the same workflow for the reason
that specialization exists. The claim this command makes — that nothing observable changed — is
exactly the invariant claim the Refactor workflow is built to hold to account, and
`behavioral-validation` is where that claim is checked rather than asserted.

## Inputs

One supplied input of type `change-request`, plus the `business-intent` and
`architecture-context` the Refactor entry phase requires, stating:

- Optimization scope: which roots and instruction files are in scope (defaults to `memory/`,
  `context/`, `CLAUDE.md`, and the repository-root governance documents `SR-2` names).
- Excluded paths beyond the standing deny list.
- Invariants that must survive, beyond the mechanical set below.
- Token-reduction target, where the requester has one, and whether it is a goal or a threshold.
- Whether the pass may overwrite in place, or must stop at proposed candidates for review.

The command materializes these documents from the operator's arguments when they are not supplied
as files.

## Workflow Triggered

Refactor (`workflows/refactor.md`), entered at `scope-invariants-and-risk-profile`. The five
phases carry this command's lifecycle without alteration:

| Phase | What it carries here |
|---|---|
| `scope-invariants-and-risk-profile` | Which files are in scope, and which invariants the pass may not break |
| `safety-net-establishment` | The baseline: pre-image digests and the recorded token cost of every file in scope |
| `refactor-implementation` | The compression pass, performed by `runtime/optimize_memory.py` |
| `behavioral-validation` | The invariant check per file, and the accept/reject disposition of every candidate |
| `closure-and-debt-record` | What was left uncompressed, and why |

## Execution

From the repository root:

1. **Scan.** `python .claude/runtime/optimize_memory.py scan` reports what is in scope and what
   is skipped, with the reason for each skip. It makes no API request and modifies nothing.
2. **Dry run.** `python .claude/runtime/optimize_memory.py run` proposes a compression for every
   eligible file, checks each candidate against the invariants below, and writes the candidates,
   unified diffs, a manifest, and a report under `runs/optimize-memory/<stamp>/`. Nothing on disk
   under `memory/`, `context/`, or the instruction set is touched.
3. **Review.** Read `runs/optimize-memory/<stamp>/report.md`. It states, per file, the token cost
   before and after, the verdict, and — for every rejected candidate — which invariant it broke.
4. **Apply.** `python .claude/runtime/optimize_memory.py run --apply` performs the pass again and
   overwrites the originals, writing every pre-image to the session's `backup/` tree first.
5. **Restore, if needed.** `python .claude/runtime/optimize_memory.py restore --manifest
   runs/optimize-memory/<stamp>/manifest.json` puts every pre-image back.

Steps 2–4 are what the host performs when an operator types `/optimize-memory`; the review in
step 3 is the operator's, and the command does not proceed past it on its own.

## Invariants

A candidate is accepted only when it breaks none of these. The check is mechanical, runs against
the original, and is what the phrase "lossless" means in this command — no more and no less.

| ID | Invariant |
|---|---|
| `I0` | The candidate is non-empty and decodes without replacement characters |
| `I1` | YAML frontmatter is reproduced byte for byte |
| `I2` | Every heading survives with the same text, level, and order; none is introduced |
| `I3` | Every fenced code block survives byte for byte |
| `I4` | Every table keeps its header cells and its body row count |
| `I5` | Every link target survives |
| `I6` | Every inline code span survives |
| `I7` | Every declared rule or requirement identifier survives |
| `I8` | No list item is dropped |
| `I9` | The candidate is not below the size floor that separates compression from deletion |

A candidate that breaks one is rejected, the original is kept unmodified, and the violation is
named in the report. A candidate that saves less than the minimum gain threshold is recorded as
unchanged and the original is kept, so a pass never spends a write for a negligible saving.

`I0`–`I9` are structural and identifier-level. They are what the pass verifies and therefore what
it claims. Semantic equivalence of the surrounding prose is asserted by the model that produced
the candidate and is **not** machine-proven; the recorded unified diff, read by the operator at
step 3, is the control for that. A requester who needs a stronger guarantee than a reviewed diff
should not use this command.

## Framework Safety

- Generated and evidence surfaces are denied by path, not by convention: `runs/`, `reports/`,
  `proposals/`, and any changelog or history file. Rewriting the record of what the framework did
  would falsify the evidence the governance layer resolves against.
- Any file declaring itself generated — a `generated: true` frontmatter key, a generated-file
  HTML comment, or a leading `DO NOT EDIT` — is skipped with that reason recorded.
- Files below the byte floor are skipped: there is nothing there to recover.
- Non-markdown files are never rewritten. The pass has no opinion about source code.
- The deny list binds regardless of what `--root` asks for. An operator cannot widen the pass into
  run evidence by pointing it there.

## Expected Outputs

- `runs/optimize-memory/<stamp>/report.md`: the per-file token delta, verdict, and the invariant
  every rejected candidate broke.
- `runs/optimize-memory/<stamp>/manifest.json`: the machine record — mode, thresholds, per-file
  digests before and after, and the backup path of every applied file.
- `runs/optimize-memory/<stamp>/diffs/`: a unified diff per accepted candidate, which is the
  artifact the operator reviews.
- `runs/optimize-memory/<stamp>/proposed/` on a dry run, or the rewritten originals plus
  `backup/` on an applied pass.
- The workflow artifacts the Refactor phases contract for, unchanged by this command.

## Success Criteria

- Every accepted candidate passed `I0`–`I9`.
- Every rejected candidate left its original byte-identical.
- Every applied file has a recoverable pre-image and a recorded digest pair.
- No file under a denied path was read for rewriting.
- The reported token saving is measured, not estimated.

## Failure Handling

- A missing client library or unresolvable credentials fails before any file is read, with the
  remediation named.
- A declined or truncated response is recorded as a failed file; the pass continues, and the
  original is kept.
- A candidate breaking an invariant is rejected without a retry. Re-running is cheap; a second
  attempt at a broken candidate is not more likely to be correct.
- An applied pass that a reviewer rejects after the fact is undone with `restore`. Where a file
  changed after the pass, `restore` refuses it and says so rather than discarding the later edit.

## Boundaries

This command compresses wording. It does not decide what belongs in memory or context — that is
`memory/memory-governance.md` and the `/refactor` and `/implement` lifecycles that add and retire
knowledge. It does not touch source code, agent contracts, registries, workflow specifications,
or command specifications, none of which are knowledge surfaces loaded per run. It never edits
run evidence, and it never edits itself.
