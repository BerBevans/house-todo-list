# Architecture

## Overview

One ASP.NET Core app (Razor Pages, rendered on the server) stores the house's jobs in a SQLite file. It runs on a small cloud VPS in Docker Compose, behind Caddy for HTTPS, with Litestream streaming the database to Cloudflare R2 for backups. People sign in with Google, and only the house's members get in.

## Deployables

- `HouseTodoList.Web`: the app, one container on the VPS. One instance only.
- Caddy and Litestream: off-the-shelf containers in the same Compose stack, configured, not coded.

## Projects and dependency rules

| Project | Contains | May reference |
| --- | --- | --- |
| `HouseTodoList.Core` | Domain (`House`, `Member`, `Job`), use cases, and the ports they need (repositories) | nothing in the solution; no EF Core, SQLite, ASP.NET Core or `HttpClient` |
| `HouseTodoList.Adapters` | EF Core `DbContext`, repositories, migrations, and the extension methods that register and migrate storage | Core; no ASP.NET Core |
| `HouseTodoList.Web` | Razor Pages, sign-in, and the composition root `HouseTodoList.Web.Program` | Core; Adapters only from `Program`; never EF Core or SQLite |
| `HouseTodoList.UnitTests` | Sociable tests of Core and Web, with the repositories and clock faked | Core, Web |
| `HouseTodoList.IntegrationTests` | Adapters against a real SQLite file in a temporary folder | Adapters, Core |
| `HouseTodoList.ArchitectureTests` | The rules in this table | all product projects |

The architecture tests in `HouseTodoList.ArchitectureTests` enforce these rules.

- Web counts as core code: it's unit-tested and mutation-tested. Keep page handlers thin and put the logic in Core's use cases.
- `Program.Main` only calls `BuildApp(args).RunAsync()`. All registration happens in `Program.BuildApp`, which unit tests call directly or through `WebApplicationFactory` with the adapters swapped for fakes.
- Each project has a placeholder type and test, there only to satisfy the harness. Delete it in the first ticket that adds real code to that project.

## Boundaries

- Storage (SQLite through EF Core): owned by `HouseTodoList.Adapters`, tested by `HouseTodoList.IntegrationTests`. SQLite runs in-process, so these tests use a real database file rather than Testcontainers; a later server dependency uses Testcontainers.
- Identity (Google): ASP.NET Core's Google handler, configured in `HouseTodoList.Web`. Whether a signed-in email is a member is decided in Core. Unit tests use a fake authentication scheme.
- Clock: `TimeProvider`, injected, faked in unit tests.
- Backups (Litestream to R2): outside the app; configured in the Compose stack.

## Data

One SQLite file on a Docker volume, with the ASP.NET Data Protection keys beside it.

- Tables: `Houses` (one row, seeded), `Members` (house, email, role), `Jobs` (GUID id, house, title, description, done flag, created, completed and updated times).
- Every row carries a `HouseId`, and every query is scoped to the signed-in member's house (ADR 0005).
- Schema changes are EF Core migrations in `HouseTodoList.Adapters`, applied at startup.
- Later features (areas, sub-tasks, statuses, priorities, links, assignment) arrive as additive migrations. Nothing is added for them in advance.

## Cross-cutting

- **Configuration:** `appsettings.json` and environment variables. Secrets (Google client secret, R2 keys) live only in the host's environment file, never in the repo.
- **Logging:** the built-in console logger, read with `docker logs`. No telemetry.
- **Errors:** validation failures (such as an empty title) come back from Core as results and are shown on the form. Anything unexpected is logged and shows a generic error page.
- **Authentication:** a cookie after Google sign-in; every page requires it, and non-members get a 403 (ADR 0004). Razor Pages' anti-forgery protects every form.
- **Phone use:** a mobile-first stylesheet, text sized in `rem`, tap targets at least 44px, and a viewport meta tag on every page. There are no UI tests yet; a `HouseTodoList.UiTests` project with Playwright comes after the first real page.

## Decisions

- [ADR 0001: One Razor Pages app on a VPS with Docker Compose](docs/adr/0001-single-razor-pages-app-on-a-vps.md)
- [ADR 0002: SQLite with EF Core migrations](docs/adr/0002-sqlite-with-ef-core-migrations.md)
- [ADR 0003: Litestream backups to Cloudflare R2](docs/adr/0003-litestream-backups-to-cloudflare-r2.md)
- [ADR 0004: Google sign-in with a member allowlist](docs/adr/0004-google-sign-in-with-a-member-allowlist.md)
- [ADR 0005: Every row belongs to a house](docs/adr/0005-house-scoped-data-model.md)

## When to escalate

A ticket needs an architecture decision from the owner, flagged as a blocking assumption, if it adds a new dependency direction, new persistence, a new external integration or a new deployable.
