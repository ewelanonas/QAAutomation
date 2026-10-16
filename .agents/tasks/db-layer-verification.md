# DB-layer implementation — verification note

First iteration (no `db-layer-review.json` existed). Implemented per `db-layer-plan.md`.
All changes are **uncommitted** in the working tree, as required.

## Exact answers the task asked to record

- **Microsoft.Data.SqlClient final pinned version: `7.1.0`.** It restored and built cleanly on
  `net10.0`, so no step-down was needed.
- **Testcontainers: SKIPPED (not pinned, no helper added).** `Testcontainers.PostgreSql` and
  `Testcontainers.MsSql` (4.15.0) remain *reserved* in `tech-stack.md`. Reason: there is no
  container-based integration test to run yet, and pinning unused packages only adds transitive
  surface and warning risk for no gain. The core deliverable (Npgsql + Microsoft.Data.SqlClient +
  Respawn) is complete without them.
- **Transitive security pin added:** `Microsoft.Data.SqlClient` 7.1.0 pulls in
  `System.Security.Cryptography.Pkcs` **9.0.18**, which NuGet flags **NU1902** (moderate) and
  **NU1903** (high) — CVE-2026-84008 / GHSA-846f-f764-76mx. Per the advisory the `net10.0` line is
  vulnerable through 10.0.12 and first patched at **10.0.13**, so
  `System.Security.Cryptography.Pkcs` is pinned to **10.0.13** in `Directory.Packages.props`
  (central transitive pinning is already enabled). Both warnings cleared.
- **Full build result:** `dotnet build QAAutomation.sln` → **Build succeeded, 0 Warning(s),
  0 Error(s).** `dotnet restore QAAutomation.sln` → no NU1008, no NU1902/NU1903, no warnings.
  No `using System.Linq` warning from the `WarnOnLinqUsage` target.
- **Test suites run:**
  - `dotnet test tests\QAAutomation.Tests.Api` → **Passed: 9, Failed: 0, Skipped: 1, Total: 10.**
    The one skipped test is `AStoredVATReturnCanBeReadBackForItsTenant` — the new
    `VatReturnPersistence` scenario, skipped by `@ignore` exactly as designed (it did not fail,
    and the `@database` reset hook did not fire for the existing scenarios).
  - **UI and E2E suites: NOT run.** They need Chrome + internet; the build compiles both cleanly
    (they are in the solution build above). Build-green is the primary gate per the task.
  - Environment note: the API suite reaches the live public website and all 9 existing scenarios
    passed, so outbound network was available here. If a sandbox without network shows those as
    failing, that is an environment limitation, not a regression from this change — the
    `@database @ignore` scenario is skipped regardless.

## What was built (all uncommitted)

New project `src/QAAutomation.Data/` (references Core only; the only project taking a DB driver):
- `QAAutomation.Data.csproj` — added to `QAAutomation.sln` under the `src` solution folder.
- `IDatabaseConnectionFactory.cs` — `Task<DbConnection> OpenAsync(CancellationToken)`, returns the
  `System.Data.Common` base type so everything above it is provider-neutral.
- `NpgsqlConnectionFactory.cs` / `SqlServerConnectionFactory.cs` — the only provider-aware code;
  each guard-throws naming `QAAUTOMATION_Database__ConnectionString` when the string is blank.
- `VatReturnRecord.cs` — plain data carrier.
- `VatReturnRepository.cs` — "the DB class". Parameterised ANSI `SELECT` (named params via
  `command.CreateParameter()`, never concatenated); `tenantId` is a required explicit parameter
  (cross-tenant guard); returns a mapped record or `null`; asserts nothing.
- `DatabaseResetter.cs` — wraps Respawn; adapter chosen from `DatabaseOptions.Provider`; Respawner
  built once and cached (registered singleton).
- `DataRegistration.cs` — `AddDatabaseAccess(this IServiceCollection, DatabaseOptions)`: options
  singleton, connection factory + repository scoped, resetter singleton.

Core configuration (no DB package leaks into Core):
- `DatabaseOptions.cs` (new) — `DatabaseProvider { Postgres, SqlServer }` enum + POCO
  (`Provider`, `ConnectionString`, `CommandTimeoutSeconds`).
- `TestConfiguration.cs`, `TestConfigurationLoader.cs`, `appsettings.json` — wired the `Database`
  section exactly like `ApiOptions`; `ConnectionString` default is empty `""` (secret arrives via
  env var at run time, keeps the secret scanner clean).

Test wiring:
- `QAAutomation.Tests.Api` and `QAAutomation.Tests.E2E` reference `QAAutomation.Data` and call
  `services.AddDatabaseAccess(testConfiguration.Database)`. The UI suite does **not** reference it.
- `DatabaseResetHooks.cs` (API suite) — `[AfterScenario("database")]`, tag-scoped so it only runs
  for `@database` scenarios and never for the existing ones.
- `VatReturnPersistenceSteps.cs` + `Features/VatReturnPersistence.feature` tagged
  `@api @regression @database @ignore` — the assertion lives in the step
  (`stored.Should().NotBeNull(...)`, `stored.Status.Should().Be(...)`).

Packages pinned in `Directory.Packages.props`: `Npgsql` 10.0.3, `Microsoft.Data.SqlClient` 7.1.0,
`Respawn` 7.0.0, `System.Security.Cryptography.Pkcs` 10.0.13 (transitive security pin).

Steering: `tech-stack.md` updated — Npgsql / Microsoft.Data.SqlClient / Respawn moved to Active, a
"Decision — provider-agnostic data access" note added, Testcontainers rows kept reserved
(+ `Testcontainers.MsSql`), and the transitive pin recorded. PostgreSQL noted as KEYinfinity's
documented primary engine; SQL Server added on request for portability.

## Two things flagged for the user (not changed here)

1. **`@database` is a new feature-area tag** not yet in `bdd-gherkin-standards.md`'s taxonomy
   table. Adding it there is a recommended consistency follow-up; this task's steering edit scope
   was limited to `tech-stack.md`, so it was left alone.
2. **DI lifetime note (by design, per the task):** `DatabaseResetter` is a singleton that depends
   on the scoped `IDatabaseConnectionFactory`. This never triggers in the default run — the reset
   hook is tag-scoped to `@database`, which is `@ignore`, so Reqnroll never constructs the hook or
   resolves the resetter. If a live `@database` run is ever enabled, revisit this lifetime pairing
   (e.g. make the factory a singleton, since it is stateless). Not changed here because the task
   specified these exact lifetimes.
