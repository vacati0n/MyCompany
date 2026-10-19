# Validator fixtures

Conforming reference artifacts, one per artifact type the Validation Engine covers. They exist
so that `verify_validators.py` can prove two things about every registered validator:

1. a conforming artifact is **accepted** -- the validator is not rejecting everything, and
2. a deliberately mutated artifact is **rejected by the expected check** -- the validator is
   not accepting everything either.

A validator that has only ever seen a conforming artifact has proved nothing, so every fixture
is paired with a mutation in `verify_validators.py` that must trip a named check.

These are fixtures, not run artifacts. They carry no run provenance, so the digest cross-check
(`C7.1`, `V10.1`, `A14.x`) reports `not-machine-checkable` when they are validated without an
invocation envelope, which is the correct outcome: there is no envelope to check against.

`framework-change-proposal.md` has no fixture either, for a different reason: it is a governance
artifact written *about* a run rather than inside one, so its accepted instances live under
`proposals/`. `verify_validators.py` declares that directory as its instance source and reads the
newest proposal, on the same principle as below.

`execution-plan.md` and `technical-design.md` have no fixture here. Real, accepted instances of
both exist under `runs/`, produced by the registered agents and committed by the runtime, and
`verify_validators.py` reads those instead. Evidence produced by an actual run outranks a
fixture.
