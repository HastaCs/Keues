# AGENTS.md — Keues

Queue/ticket management system for in-person service. This repo contains the admin API and web dashboard; the Counter, Monitors, and TicketMachine apps live in separate repos.

**Frontend work:** read `Keues.Dashboard/AGENTS.md` first. It has the dashboard commands, architecture, and quirks (pnpm, TanStack Query, i18n, Oxlint/Oxfmt, etc.).

## Layout

- `Keues.API/` — ASP.NET Core entrypoint (`Program.cs`), controllers (`/api/*`), OpenAPI, SignalR `DeviceHub` at `/devices`.
- `Keues.Application/` — use cases. Each operation has a folder with `*Command` + `*Handler`; features expose a facade (e.g. `QueuesUseCases`) and register handlers in `DependencyInjection.cs`. Handlers depend on `IApplicationDbContext`, not `AppDbContext`.
- `Keues.Domain/` — entities, enums, domain events. No EF dependency.
- `Keues.Infrastructure/` — EF Core/SQLite, migrations, JWT, email, event channel, webhooks, background services.
- `Keues.Tests/` — xUnit; use-case tests plus full-HTTP integration tests (`ApiTestBase`, `TestWebApplicationFactory`).
- `Keues.Dashboard/` — React 19 SPA; built into the API's `wwwroot` in Docker.

Dependency direction: `API -> Infrastructure -> Application -> Domain`. Keep it that way.

## Commands

```bash
dotnet run --project Keues.API        # http://localhost:5125 (launchSettings)
dotnet test Keues.Tests               # full backend suite
dotnet test Keues.Tests --filter "FullyQualifiedName~QueueUseCasesTests"
./scripts/export-openapi.sh           # regenerate docs/openapi.json after API changes
```

- The API dev port is **5125**, not 8080. Port 8080 is the Docker container; the Vite proxy also targets 5125.
- There is no CI test/lint workflow (only release-please). Run verification locally.
- For fast frontend iteration use `pnpm typecheck`, `pnpm lint`, and `pnpm vitest` separately; `pnpm test` also runs a production build.

## Adding an API feature

1. Add `Command` + `Handler` under `Keues.Application/Features/<Feature>/<Operation>/`.
2. Register the handler and expose it on the feature facade in that feature's `DependencyInjection.cs` / `*UseCases.cs`.
3. Add or extend the controller in `Keues.API/Controllers/`.
4. Add a use-case test and/or an API test, then regenerate `docs/openapi.json`.

Controllers catch every exception and return **HTTP 400** with `ErrorResponse`; business failures are not 404. Keep that convention unless the whole API is migrated.

## Runtime / data

- SQLite lives at `Keues.API/data/keues.db`; `Keues.API/data/config.json` is auto-generated on first start and contains the JWT key. Both are gitignored — never commit them.
- `Program.cs` runs `Database.Migrate()` on startup. Migrations live in `Keues.Infrastructure/Migrations/`; add them with `dotnet ef migrations add <Name> --project Keues.Infrastructure --startup-project Keues.API` (install `dotnet-ef` if missing).
- `config.json` values are overridden by `KEUES_*` env vars (`KEUES_JWT_KEY`, `KEUES_DASHBOARD_URL`, SMTP/Mailjet settings). `Jwt:Issuer`, `Jwt:Audience`, and expiration still come from `appsettings.json`/standard configuration.
- Deletes are soft deletes: `RemovedAt` plus EF global query filters hide removed Locations, Queues, Counters, Flows, Users, and UserGroups. Historical tickets are also filtered through their Queue/Flow.
- Dates are serialized as UTC with a trailing `Z` (`UtcDateTimeConverter`); SQLite stores unspecified values, so use `DateTime.UtcNow` and do not reintroduce local-time conversions.
- Real-time events flow through an in-process channel (`IKeuesEventPublisher`) to handlers/webhooks. Publish after saving, not inside the transaction.

## Tests

- xUnit parallelization is disabled assembly-wide (`Keues.Tests/AssemblyInfo.cs`); integration tests share process-wide env vars, so do not re-enable parallel execution.
- `TestWebApplicationFactory` uses a temp content root, in-memory SQLite, and fakes for email/events. No external services are required.
- Use-case tests use `TestDatabaseFactory` (`EnsureCreated`) and the `Seed` helpers; integration tests start from an empty admin via `CreateAuthenticatedClientAsync()`.
- Auth is a JWT in the HttpOnly `access_token` cookie (Bearer is also accepted). Protected admin endpoints require the `Admin` role.

## Git / releases

- Never run `git commit`, `git push`, or amend — the user commits manually.
- Releases are driven by release-please on `main` using **Conventional Commits** (`feat:`, `fix:`, `chore:`, ...). It updates `CHANGELOG.md` and the version in `Keues.Dashboard/package.json` and `Keues.API/Keues.API.csproj`.
