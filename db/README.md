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

## Stop and remove

```bash
docker rm -f mediacompany-pg
```
