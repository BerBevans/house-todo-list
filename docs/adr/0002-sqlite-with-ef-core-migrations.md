# 0002. SQLite with EF Core migrations

- Status: accepted
- Date: 2026-10-28

## Context

The data is a few hundred jobs for one household, written by a handful of people, with one app instance (ADR 0001). Every saved change must survive a crash and a reboot, and must be backed up off the machine (ADR 0003). The harness expects adapters to be tested against real dependencies in Testcontainers.

## Decision

Store everything in one SQLite file on a Docker volume, accessed through EF Core in `HouseTodoList.Adapters`. Schema changes are EF Core migrations in that project, applied when the app starts; that is safe because there is only ever one instance. `HouseTodoList.Adapters` exposes extension methods to register and migrate storage, so `HouseTodoList.Web` never uses EF Core itself.

SQLite runs inside the app's process, so there is no container to start. Integration tests use a real SQLite database file in a temporary folder, which is the real engine and file format. Any later dependency that runs as a server uses Testcontainers as usual.

## Alternatives considered

- PostgreSQL in its own container: integration tests would follow the harness's Testcontainers rule as written, but it adds a stateful server to run, patch and back up, for no benefit at this size.
- Dapper and hand-written SQL: less magic, but we'd own the schema-change tooling.

## Consequences

No database server, backups that are a file (ADR 0003), and fast integration tests. SQLite allows one writer at a time, which is plenty here. More than one instance, or heavy concurrent writing, would mean moving to a server database; EF Core keeps that move mostly inside `HouseTodoList.Adapters`.
