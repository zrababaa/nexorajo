# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build SMPP.slnx

# Run the web app (dev profile, http://localhost:5083)
dotnet run --project src/SMPP.Web/SMPP.Web.csproj

# Run all tests
dotnet test tests/SMPP.Tests/SMPP.Tests.csproj

# Run a single test
dotnet test tests/SMPP.Tests/SMPP.Tests.csproj --filter "FullyQualifiedName~SegmentCounterTests"

# Add an EF Core migration (from the Infrastructure project)
dotnet ef migrations add <Name> --project src/SMPP.Infrastructure --startup-project src/SMPP.Web
```

Local dev needs a MySQL server matching `appsettings.Development.json`'s connection string
(`smpp_bulk_db_new`, root/root). To run against no MySQL at all, set
`Database__UseInMemory=true` as an environment variable instead of editing checked-in config.

## Architecture

Four-project Clean Architecture, referenced top-down (`Web` -> `Infrastructure` -> `Application` -> `Domain`):

- **SMPP.Domain** — entities (`Entities/`), enums, and the `Pricing` namespace (`MessagePricing`). No dependencies.
- **SMPP.Application** — one folder per feature (`Accounts`, `Campaigns`, `Sending`, `Reports`, `Payments`, `AdminBudget`, `SpamKeywords`, `History`, `PublicApi`, `Dashboard`), each holding a service interface (`IXService`) plus its DTOs. Cross-cutting interfaces live in `Abstractions/`. No implementations here — this project is contracts only.
- **SMPP.Infrastructure** — implements every `Application` interface (`Services/`), owns EF Core (`Persistence/SmppDbContext.cs`, `Persistence/Configurations/`, `Persistence/Migrations/`), and registers everything in `DependencyInjection.AddInfrastructure`. New service? Add the interface in `Application`, the implementation in `Infrastructure/Services`, and register it in `DependencyInjection.cs`.
- **SMPP.Web** — ASP.NET Core MVC + a parallel REST API surface. `Controllers/` are MVC (server-rendered `Views/`); `Controllers/Api/` are the `/api/v1` REST controllers, secured by `SMPP.Web.Auth.JwtTokenService` bearer tokens (cookie auth also works — see `Program.cs`'s `WriteApiStatusOrRedirect`, which returns JSON status codes instead of login redirects under `/api`). `Api/ApiServiceExtensions.cs` wires up JWT auth, CORS, and Swagger.

### The database is shared with a legacy system — read before touching Persistence

`smpp_bulk_db_new` is shared with a legacy Laravel app and an external SMPP daemon written in
another stack. The daemon owns two tables outright:

- **`historys`** (mapped by `History`/`HistoryConfiguration`) — the daemon inserts rows and updates
  them via DLR callback keyed on `get_message_id`. Status/message_type are legacy text codes, not
  enum ordinals (see `LegacyMessageCodes`). This table is `ExcludeFromMigrations()` — no EF
  migration may ever emit DDL against it.
- **`under_process`** (mapped by `UnderProcess`) — this app enqueues outbound sends here by
  inserting a single row per batch; the daemon polls the table, submits to SMPP/SOAP, and writes
  the per-recipient result to `historys`. **Sending is fire-and-forget**: `SendCore.ExecuteAsync`
  (`Infrastructure/Services/SendCore.cs`) is the shared debit+spam-filter+enqueue path used by
  `QuickSendService`, `BulkSendService`, and the public API send endpoint — it writes the
  `UnderProcess` row and returns; this app never speaks SMPP itself.

Because of this, `Persistence/DatabaseBootstrapper.cs` (registered instead of a plain
`MigrateAsync` call) branches at startup: on a fresh/EF-owned database it runs normal migrations;
on the shared legacy database (daemon tables already present, baseline migration unapplied) it
creates the app's own tables from an embedded SQL script and stamps the baseline as applied,
specifically to avoid EF renaming/recreating the daemon's live tables. It also refuses to start if
`lower_case_table_names` would make the app's `Campaigns`/`SpamKeywords` tables silently resolve
to the legacy lowercase `campaigns`/`spam_keywords` tables. Read the class comment before changing
migration or bootstrap behavior.

### Auth and localization

- Every MVC/API route requires authentication by default (`Program.cs` adds a global
  `AuthorizeFilter`); anonymous actions opt out explicitly (e.g. `AccountController` login).
- English/Arabic localization via `IViewLocalizer`/`SharedResource`, culture cookie or
  `Accept-Language` header (`Program.cs` `RequestLocalizationOptions`).
- `CheckBlacklistFilter` and `ApiExceptionFilter` are applied globally to all MVC actions.
