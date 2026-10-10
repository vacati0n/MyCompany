# Record store — local datastore for development and the integration demonstrations

The system holds relational state, the append-only record and the durable job queue in one
PostgreSQL instance. Two properties of the design live in this datastore rather than in
application code, so neither can be exercised without one running:

- cost is a stored generated column over the unit counts and the unit prices snapshotted onto
  each row, which is what keeps it exact and re-derivable after a price is superseded;
- the state change, the queue claim, the operation record and the audit entry commit in one
  transaction, which is what makes accounting exactly-once without an outbox.

## Start it

```bash
docker run -d --name mediacompany-pg -e POSTGRES_PASSWORD=mediacompany-dev -e POSTGRES_USER=postgres -e POSTGRES_DB=mediacompany -p 55432:5432 postgres:17-alpine
```

Port 55432 is deliberate: it leaves a default local PostgreSQL on 5432 alone. The password is a
development value and belongs to this container only; it is not a credential the system holds.
Production secrets reach the credential broker from a process-external source and never from a
connection string in a repository.

## Create the schema

```bash
MEDIACOMPANY_CONNECTION_STRING="Host=localhost;Port=55432;Database=mediacompany;Username=postgres;Password=mediacompany-dev" dotnet run --project src/MediaCompany.Host -- install
```

**Apply the schema before any process that writes gate transitions starts.** The sixth resource
(`006-multi-channel.sql`) numbers the existing gate transitions once, by their recorded instants;
re-applying it while gate writers are running could move the order sequence backwards. Install
is a stopped-system step.

**All seven resources are applied in order, before any process starts.** The seventh resource
(`007-ai-economics.sql`) adds the benchmark record, the admission decision and booking tables,
the cost-stated column and its reason, and the separate same-instant gate trigger. Rows written
before it is applied read their cost as not stated, so applying it mid-month to a store that
already holds operations refuses metered admission until that month ends. Apply it to an empty
store, or at a month boundary.

**All eight resources are applied in order, before any process starts.** The eighth resource
(`008-management.sql`) adds the open-decisions register, seeded with a transcription of the
owner's recorded decisions and open questions; the platform-policy statement register and the
re-verification result record, both created empty; and the held-outcome record, created empty,
that the admission path writes for every held or deferred request from then on. It also widens
the admission decision readings' amount, spend and utilisation columns to an unbounded decimal,
keeping every stored value. A held operation booked before it is applied reads its reason,
escalation and hold timeout as not recorded. The eighth resource can be re-applied on its own and
changes nothing the second time; the first four resources cannot, so a full install is not
repeatable on a store that already holds them. Its reversal is dropping what it creates in reverse
order and restoring the column bound while no stored value exceeds it; after that, restore-to-point.
The register has no writer: a decision the owner records later reaches it only through a later
resource.

Establish the backup and restore position **before** the first append-only entry is written. The
recorded history refuses update and delete, so from that point the store has no in-place
correction and restore-to-point is the only reversal.

## Run the integration demonstrations

**The demonstrations destroy the schema of whatever database the test connection string names.**
Each one drops that database's schema when it starts and drops it again when it completes,
whether it passed, failed or aborted, so no row a demonstration wrote outlives it — including the
first-publication conditions some of them record as satisfied, the owner approvals, the gate-state
fixtures and the dispatch records, which exist only as fixtures of a demonstration. A development
schema must therefore live in a **different database** from the one the demonstrations are pointed
at: the development schema above is created in `mediacompany`, and the demonstrations run against
a separate `mediacompany_demo` database in the same container.

```bash
docker exec mediacompany-pg createdb -U postgres mediacompany_demo
MEDIACOMPANY_TEST_CONNECTION_STRING="Host=localhost;Port=55432;Database=mediacompany_demo;Username=postgres;Password=mediacompany-dev" dotnet test MediaCompany.slnx
```

Never point the test variable at a database holding anything you intend to keep, the development
schema included. Without the variable the datastore demonstrations skip with a recorded reason
rather than passing, and the suite reports them as not-run. With it they execute.

## Other commands

```bash
dotnet run --project src/MediaCompany.Host -- check
```

Resolves every registration without opening a connection: the cheapest confirmation that a
deployment is wired before it touches the datastore. `registers` prints what the company is
configured to operate, and `report` prints the measurable-now set with every deferred measure
naming the parameter it waits on.

```bash
dotnet run --project src/MediaCompany.Host -- weekly 2026-W41
dotnet run --project src/MediaCompany.Host -- dashboard 2026-W41
```

`weekly` prints the COO, CTO and CFO reports, channel performance, the risk summary and the CEO
brief; `dashboard` prints the read-only dashboard, one tile per brief line. Both need
`MEDIACOMPANY_CONNECTION_STRING` and compose everything from one read-only read of the store at
one datastore instant: they write nothing, advance nothing, open no listener and call no model.
The week is an ISO week, the UTC week from Monday 00:00 to the next Monday 00:00 (Monday 07:00
Vietnam time); without the argument it is the week containing the datastore's current instant.
A week reads NOT FINAL until `report` has run after the week ended; that run is the closure that
makes it final.

Exit codes: `0` printed; `2` a malformed week, or a well-formed week that does not exist such as
`2027-W53`, refused by name with nothing read; `3` the read ended in a named outcome instead of
printing: the store being changed (the 2-second lock bound), a read too slow (the 5-second
statement bound), a store without the eighth resource, or another datastore failure named by its
class. The read never waits on a hold kept elsewhere; it ends instead.

Every figure reads as exactly one of observed, observed zero, unmeasured (naming what was looked
for), recorded (naming where it is recorded) or not recorded. Recommendations come only from a
fixed catalogue of seven rules, and a rule abstains over an unmeasured reading.

## The ninth resource and the production commands

**All nine resources are applied in order, before any process starts.** The ninth resource
(`009-production.sql`) adds, each created empty and write-once: the store designation (demonstration
or company, with the database it names), the item cap register, the admission reservations (the
worst case of every capped provider attempt, committed before the call), the production versions
and the production artifacts. It widens the price kinds to character and image units and makes the
operation record's stored cost cover all five kinds, recomputing every existing row to the value it
already held; it records the owner's five answers of 2026-10-09 as decided register entries
superseding the questions they answer. It records no designation, cap, channel, budget amount,
configuration value, price or route, and it re-applies changing nothing.

```bash
dotnet run --project src/MediaCompany.Host -- prepare --settings <settings.json> --designation demonstration
dotnet run --project src/MediaCompany.Host -- produce --mode plan-only --settings <settings.json>
dotnet run --project src/MediaCompany.Host -- produce --mode fake --settings <settings.json>
```

`prepare` loads `config/preparation-item-001.json` and item 001's package into the connected store,
inserting where absent and refusing, by name, any row that differs. `produce` runs one production:
`plan-only` prints the plan and calls nothing; `fake` produces the video at USD 0.00 with fake
providers, in a **demonstration** store only; `metered` books into the company store only. The
settings file (see `config/production-settings.sample.json`) names the output root, which must lie
outside the repository, the media tool's two executables, the font, the render profile and the
bounds. The owner's guide to the metered run is `wave-9/owner-guide-metered-run.md`.

The live suite's production demonstrations need the media tool and the font on the machine; without
them those demonstrations skip as not-run, saying why, and never pass.

## The tenth resource and the company's own voice

**The tenth resource is applied ALONE to a store that holds the nine**, after a backup:

```bash
dotnet run --project src/MediaCompany.Host -- install --from 10
```

`install --from <ordinal>` applies the ordered resources from that one on and nothing before it. A
plain `install` over a store that already holds the store designation table is **refused**, naming
this option, because the first four resources cannot be re-applied over real rows. The tenth
resource (`010-own-voice.sql`) admits the own production mode, adds a nullable narration source to
the production version, checked against the mode (a row recorded before it reads null, its mode's
source), and creates, each empty and write-once: the recording registrations (the performer and the
release fields, each null when not given), the registered beat files, the narration measurements
and the narration provenance. Every duration there is a generated column, decoded samples over the
sample rate; no column holds a document, a recording or a model file. It changes no delivered row
and re-applies changing nothing. Its reversal is dropping what it creates while only demonstration
rows exist; a company store holding a real registration, measurement or provenance is restored from
the backup.

```bash
dotnet run --project src/MediaCompany.Host -- verify-model --settings <settings.json>
dotnet run --project src/MediaCompany.Host -- register-recording --settings <settings.json> --recordings <folder>
dotnet run --project src/MediaCompany.Host -- produce --mode own --settings <settings.json>
```

`verify-model` reads the in-house model's files only and starts no process; `register-recording`
registers one file per beat with the release fields given; `produce --mode own` narrates from a
registered recording with a complete release, else from the verified in-house model, in a
demonstration store or in the company store matching the configured identity, books no operation and
needs no credential; `produce --mode metered` is refused by the narration source rule (the owner's
decision of 2026-10-10). The owner's guide is `wave-10/owner-guide-own-voice.md`.

## Stop and remove

```bash
docker rm -f mediacompany-pg
```
