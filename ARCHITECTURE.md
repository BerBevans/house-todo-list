# Architecture

## Overview

One ASP.NET Core app (Razor Pages, rendered on the server) stores the house's jobs in a SQLite file. It runs on a small cloud VPS in Docker Compose, reached only through Cloudflare Tunnel, with Litestream streaming the database to Cloudflare R2 for backups. Cloudflare Access signs people in with Google; the app validates Access's signed token and lets in only the house's members.

## Deployables

- `HouseTodoList.Web`: the app, one container on the VPS. One instance only.
- `cloudflared` and Litestream: off-the-shelf containers in the same Compose stack, configured, not coded. `cloudflared` connects outbound to Cloudflare; the app's port is never published on the host, and the VPS accepts no inbound connections (ADR 0006).

## Projects and dependency rules

| Project | Contains | May reference |
| --- | --- | --- |
| `HouseTodoList.Core` | Domain (`House`, `Member`, `Job`), use cases, and the ports they need (repositories) | nothing in the solution; no EF Core, SQLite, ASP.NET Core, token libraries or `HttpClient` |
| `HouseTodoList.Adapters` | EF Core `DbContext`, repositories, migrations, and the extension methods that register and migrate storage | Core; no ASP.NET Core |
| `HouseTodoList.Web` | Razor Pages, Access token validation, and the composition root `HouseTodoList.Web.Program` | Core; Adapters only from `Program`; never EF Core or SQLite |
| `HouseTodoList.UnitTests` | Sociable tests of Core and Web, with the repositories and clock faked | Core, Web |
| `HouseTodoList.IntegrationTests` | Adapters against a real SQLite file in a temporary folder | Adapters, Core |
| `HouseTodoList.ArchitectureTests` | The rules in this table | all product projects |

The architecture tests in `HouseTodoList.ArchitectureTests` enforce these rules.

- Web counts as core code: it's unit-tested and mutation-tested. Keep page handlers thin and put the logic in Core's use cases.
- `Program.Main` only calls `BuildApp(args).RunAsync()`. All registration happens in `Program.BuildApp`, which unit tests call directly or through `WebApplicationFactory` with the adapters swapped for fakes.
- Each project has a placeholder type and test, there only to satisfy the harness. Delete it in the first ticket that adds real code to that project.

## Boundaries

- Storage (SQLite through EF Core): owned by `HouseTodoList.Adapters`, tested by `HouseTodoList.IntegrationTests`. SQLite runs in-process, so these tests use a real database file rather than Testcontainers; a later server dependency uses Testcontainers.
- Identity (Cloudflare Access): ASP.NET Core's JwtBearer handler, configured in `HouseTodoList.Web`, validates the `Cf-Access-Jwt-Assertion` header: signature against the team's published keys, issuer, audience (the Access application's AUD tag), expiry, and an `email` claim. The plain email header and the `CF_Authorization` cookie are never read. Whether the verified email is a member is decided in Core (ADR 0007). Unit tests, local development and the black-box suite point the configuration at a stub issuer that signs tokens with a test key. The stub is test code, never in the production image, and no setting skips authentication.
- Clock: `TimeProvider`, injected, faked in unit tests.
- Backups (Litestream to R2): outside the app; configured in the Compose stack.

## Data

One SQLite file on a Docker volume, with the ASP.NET Data Protection keys (for anti-forgery tokens) beside it.

- Tables: `Houses` (one row, seeded), `Members` (house, email, role), `Jobs` (GUID id, house, title, description, done flag, created, completed and updated times).
- Every row carries a `HouseId`, and every query is scoped to the signed-in member's house (ADR 0005).
- Schema changes are EF Core migrations in `HouseTodoList.Adapters`, applied at startup.
- Later features (areas, sub-tasks, statuses, priorities, links, assignment) arrive as additive migrations. Nothing is added for them in advance.

## Cross-cutting

- **Configuration:** `appsettings.json` and environment variables. Secrets (the tunnel token, R2 keys) live only in the host's environment file, never in the repo.
- **Logging:** the built-in console logger, read with `docker logs`. No telemetry.
- **Errors:** validation failures (such as an empty title) come back from Core as results and are shown on the form. Anything unexpected is logged and shows a generic error page.
- **Authentication:** every request must carry a valid Access token, and non-members get a 403 (ADR 0007). The app issues no cookie of its own; Access's session decides how long people stay signed in, and sign-out goes to `/cdn-cgi/access/logout`. Razor Pages' anti-forgery protects every form.
- **Phone use:** a mobile-first stylesheet, text sized in `rem`, tap targets at least 44px, and a viewport meta tag on every page. There are no UI tests yet; a `HouseTodoList.UiTests` project with Playwright comes after the first real page.

## Decisions

- [ADR 0002: SQLite with EF Core migrations](docs/adr/0002-sqlite-with-ef-core-migrations.md)
- [ADR 0003: Litestream backups to Cloudflare R2](docs/adr/0003-litestream-backups-to-cloudflare-r2.md)
- [ADR 0005: Every row belongs to a house](docs/adr/0005-house-scoped-data-model.md)
- [ADR 0006: One Razor Pages app on a VPS behind Cloudflare Tunnel](docs/adr/0006-razor-pages-app-on-a-vps-behind-cloudflare-tunnel.md) (supersedes 0001)
- [ADR 0007: Cloudflare Access identity, validated in the app, with a member allowlist](docs/adr/0007-cloudflare-access-identity-with-a-member-allowlist.md) (supersedes 0004)

## When to escalate

A ticket needs an architecture decision from the owner, flagged as a blocking assumption, if it adds a new dependency direction, new persistence, a new external integration or a new deployable.
