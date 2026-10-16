# Implementation Plan — Provider-agnostic DB validation layer (PostgreSQL + SQL Server)

Scaffolding to demonstrate where database validation lives in this Reqnroll/Selenium BDD
solution. Provider-agnostic via ADO.NET (`System.Data.Common`), selectable by configuration,
supporting PostgreSQL (Npgsql) and SQL Server (Microsoft.Data.SqlClient). Must build with no
new warnings and must not break the existing passing tests.

Work directly in `d:\git\QAAutomation` (NO git worktree). DO NOT COMMIT — leave everything
uncommitted for the user to review.

## Scope decision (read before touching the workflow)

This task does **NOT** decompose into separable features. It is one cohesive change: a new
project, its config, its classes, test wiring, one hook, one example, the package pins and the
steering entry are mutually dependent and only make sense delivered together. The existing
implement-and-review loop should implement it as written below, in order. Do not restructure
the workflow tail.

## Design decisions (grounded in the repo)

- **Separate `QAAutomation.Data` project.** DB packages stay out of `Core` and `UI`. Mirrors
  the one-way direction `tests/*` → `src/*` → `Core` (`project-structure.md`). `QAAutomation.Data`
  references `QAAutomation.Core` only, exactly as `QAAutomation.Api` does.
- **`DatabaseOptions` POCO lives in `Core`** (no DB package), wired through
  `TestConfiguration`/`TestConfigurationLoader` exactly like `ApiOptions`. This keeps config in
  one place and lets the `Data` project depend on `Core` for its options type.
- **Program against `System.Data.Common` abstractions** (`DbConnection`, `DbCommand`,
  `DbParameter`, `DbDataReader`). The repository and resetter never name a concrete engine type;
  the connection factory is the only provider-aware seam.
- **`DatabaseOptions` registration is owned by `AddDatabaseAccess`** (in the `Data` project), not
  by `Core`'s `AddTestConfiguration`. That keeps `Core` free of any DB concept and means only the
  two suites that opt in (API, E2E) ever see `DatabaseOptions`. `TestConfiguration` still carries
  a `Database` property that each `[ScenarioDependencies]` factory passes to `AddDatabaseAccess`.
- **`CommandTimeoutSeconds` is added to the `Database` section of `appsettings.json`** (default
  `30`) so `TestConfigurationLoader` can read it with the same explicit `ReadWholeNumber` helper
  the rest of the loader uses, keeping the loader uniform.
- **`ConnectionString` is read as an *optional* value** (empty allowed). The committed
  `appsettings.json` carries `""`; the real value is a secret supplied at run time via
  `QAAUTOMATION_Database__ConnectionString`. The existing `ReadRequiredText` rejects empty, so the
  loader needs one new helper that permits an empty/absent string. Presence is validated later, by
  the connection factory, with a clear message naming the env var.
- **The example scenario is `@ignore`** so the default `dotnet test` never runs it and it cannot
  fail without a live DB. `VatReturnPersistenceSteps` shares state across its own steps through a
  private field (one Reqnroll binding instance per scenario), so no extra typed context class is
  needed for a single-class demo.

House rules enforced throughout (`code-style.md`, `Directory.Build.props/targets`): no LINQ (no
`using System.Linq`), explicit `using` directives at the top of every file (ImplicitUsings OFF —
include `System`, `System.Threading`, `System.Threading.Tasks`, `System.Data`,
`System.Data.Common` as used), explicit types (`var` only for a `new` of that exact type),
private fields plain `camelCase` read via `this.`, constants `PascalCase`, guard clauses,
parameterised SQL only (never concatenate values), small methods.

## Package pins (add to `Directory.Packages.props`, reference with NO Version in csproj)

- `Npgsql` = `10.0.3`
- `Microsoft.Data.SqlClient` = `7.1.0` (latest GA, confirmed Sept 2026 — Microsoft docs:
  [What's New in Microsoft.Data.SqlClient](https://learn.microsoft.com/en-us/sql/connect/ado-net/microsoft-data-sql-client-release-notes?view=sql-server-ver17)).
  If `7.1.0` does not restore/build cleanly on `net10.0`, step down to the latest version that
  does and pin that; record the version actually pinned.
- `Respawn` = `7.0.0`
- `Testcontainers.PostgreSql` = `4.15.0` and `Testcontainers.MsSql` = `4.15.0` — **OPTIONAL**.
  Pin and reference these ONLY if a small compile-only ephemeral-container helper is added and it
  builds with no new warnings. If they add any build risk or warnings, SKIP them entirely (do not
  pin unused packages) and note they remain reserved. Core deliverable is Npgsql +
  Microsoft.Data.SqlClient + Respawn.
- If `Microsoft.Data.SqlClient` drags in a transitive package flagged with a NuGet
  security/vulnerability warning (NU1902/NU1903), resolve it by pinning the flagged transitive
  package in `Directory.Packages.props` (central transitive pinning is already enabled) and record
  the pin. `TreatWarningsAsErrors` is false, but the repo bar is "no warnings", so a NU190x
  warning must be cleared, not tolerated.

(Content was rephrased for compliance with licensing restrictions.)

---

# Steps

- [ ] 1. Pin the new packages in `Directory.Packages.props`.
      Add `<PackageVersion>` entries for `Npgsql` 10.0.3, `Microsoft.Data.SqlClient` 7.1.0 and
      `Respawn` 7.0.0 (new `<ItemGroup Label="Data access">`). Do NOT add Testcontainers yet
      (decided in step 11). Update the file's top comment that lists "reserved" packages so Npgsql
      and Respawn are no longer described as reserved.
      Files: `d:\git\QAAutomation\Directory.Packages.props`
      Verify: `dotnet restore d:\git\QAAutomation\QAAutomation.sln` succeeds (packages download).
      Full build happens in step 12 once consumers exist.

- [ ] 2. Create the `DatabaseOptions` POCO and its provider enum in Core.
      `DatabaseProvider { Postgres, SqlServer }` enum and a `DatabaseOptions` class with
      `DatabaseProvider Provider { get; set; }`, `string ConnectionString { get; set; }`,
      `int CommandTimeoutSeconds { get; set; }`. Properties only, no behaviour, XML summaries in
      the style of `ApiOptions.cs`. The `ConnectionString` summary states it is a secret supplied
      at run time via `QAAUTOMATION_Database__ConnectionString` and must never be committed.
      Files: `d:\git\QAAutomation\src\QAAutomation.Core\Configuration\DatabaseOptions.cs`
      Verify: builds in step 12 (no consumers yet).

- [ ] 3. Add `Database` to `TestConfiguration`.
      Add `public DatabaseOptions Database { get; set; }` alongside the existing four options
      properties.
      Files: `d:\git\QAAutomation\src\QAAutomation.Core\Configuration\TestConfiguration.cs`
      Verify: builds in step 12.

- [ ] 4. Read the `Database` section in `TestConfigurationLoader`.
      Add `testConfiguration.Database = ReadDatabaseOptions(configuration);` in `Load()`. Add a
      private `ReadDatabaseOptions` that reads `Database:Provider` (parsed to the enum via a new
      private helper), `Database:ConnectionString` (via a new optional-text helper that allows an
      empty/absent value and returns `""`), and `Database:CommandTimeoutSeconds` (via the existing
      `ReadWholeNumber`, then `RequireSensibleTimeout`). The provider parser uses
      `System.Enum.TryParse<DatabaseProvider>(value, ignoreCase: true, out ...)` and throws
      `TestConfigurationException.ForKey("Database:Provider", "expected Postgres or SqlServer", value)`
      on failure. No LINQ; explicit `if`/guard style matching the existing file.
      Files: `d:\git\QAAutomation\src\QAAutomation.Core\Configuration\TestConfigurationLoader.cs`
      Verify: covered by the Api test run in step 13 (loader runs at scenario start).

- [ ] 5. Add the `Database` section to the committed `appsettings.json`.
      Add `"Database": { "Provider": "Postgres", "ConnectionString": "", "CommandTimeoutSeconds": 30 }`.
      Keep `ConnectionString` EMPTY (placeholder) so the pre-commit secret scanner stays clean, and
      add a JSON-adjacent note (there is no existing comment convention in this file, so keep it a
      sibling `"_comment"`-style note only if it does not break strict JSON — otherwise record the
      run-time env-var guidance in `DatabaseOptions.cs` XML only and leave the JSON comment-free).
      The real connection string arrives at run time via `QAAUTOMATION_Database__ConnectionString`.
      Files: `d:\git\QAAutomation\src\QAAutomation.Core\Configuration\appsettings.json`
      Verify: step 13 — loader parses the section without throwing; existing scenarios still pass.

- [ ] 6. Create the `QAAutomation.Data` project and add it to the solution.
      New csproj mirroring `QAAutomation.Api.csproj` shape: `RootNamespace` `QAAutomation.Data`,
      `ProjectReference` to `..\QAAutomation.Core\QAAutomation.Core.csproj`, and
      `PackageReference` (NO Version) to `Npgsql`, `Microsoft.Data.SqlClient`, `Respawn`, and
      `Microsoft.Extensions.DependencyInjection.Abstractions` (needed by `DataRegistration`). Add
      the project to `QAAutomation.sln` under the `src` solution folder with a fresh GUID and the
      six Debug/Release config lines, plus a `NestedProjects` entry mapping it to the `src` folder
      GUID `{827E0CD3-B72D-47B6-A68D-7590B98EB39B}`. Prefer `dotnet sln d:\git\QAAutomation\QAAutomation.sln add`
      to generate the entries correctly, then confirm it landed under the `src` folder.
      Files: create `d:\git\QAAutomation\src\QAAutomation.Data\QAAutomation.Data.csproj`;
      edit `d:\git\QAAutomation\QAAutomation.sln`
      Verify: `dotnet restore d:\git\QAAutomation\QAAutomation.sln` restores the new project with
      no NU1008 (version-in-csproj) and no NU190x vulnerability warnings. If NU190x appears, pin
      the flagged transitive package per step 1's rule and re-restore.

- [ ] 7. Add the connection-factory abstraction and the two provider implementations.
      `IDatabaseConnectionFactory` with `Task<DbConnection> OpenAsync(CancellationToken cancellationToken)`
      (return `System.Data.Common.DbConnection`, never a concrete type). `NpgsqlConnectionFactory`
      (uses `Npgsql.NpgsqlConnection`) and `SqlServerConnectionFactory` (uses
      `Microsoft.Data.SqlClient.SqlConnection`). Each takes `DatabaseOptions` via constructor,
      guard-throws `InvalidOperationException` naming `QAAUTOMATION_Database__ConnectionString` when
      `ConnectionString` is blank, news up its concrete connection, `await connection.OpenAsync(token)`,
      and returns it as `DbConnection`. Explicit usings incl. `System`, `System.Data.Common`,
      `System.Threading`, `System.Threading.Tasks`. No LINQ.
      Files: `d:\git\QAAutomation\src\QAAutomation.Data\IDatabaseConnectionFactory.cs`,
      `d:\git\QAAutomation\src\QAAutomation.Data\NpgsqlConnectionFactory.cs`,
      `d:\git\QAAutomation\src\QAAutomation.Data\SqlServerConnectionFactory.cs`
      Verify: builds in step 12.

- [ ] 8. Add the `VatReturnRecord` POCO and the `VatReturnRepository` ("the DB class").
      `VatReturnRecord` — plain data carrier with `string ReturnId`, `string TenantId`,
      `string Status`, `decimal TotalVatDue` (record or class, properties only, no behaviour).
      `VatReturnRepository` — constructor takes `IDatabaseConnectionFactory`; method
      `Task<VatReturnRecord> FindByIdAsync(string tenantId, string returnId, CancellationToken cancellationToken)`
      that: guard-throws `ArgumentException` when `tenantId` is blank (comment: ambient tenancy is
      the cross-tenant bug class this guard exists to catch); opens a connection via the factory
      inside `await using`; creates a `DbCommand` via `connection.CreateCommand()`; sets
      `CommandText` to a portable ANSI `SELECT` using named parameters only —
      `SELECT tenant_id, return_id, status, total_vat_due FROM vat_return WHERE tenant_id = @tenantId AND return_id = @returnId`
      (store the SQL in a `private const string`); creates each parameter via
      `command.CreateParameter()`, sets `ParameterName`/`Value`, and adds to `command.Parameters`
      (NEVER concatenate values into the SQL); reads with `await command.ExecuteReaderAsync(token)`
      inside `await using`; if `await reader.ReadAsync(token)` is false returns `null`, otherwise
      maps columns by `reader.GetOrdinal(...)` into a `VatReturnRecord` and returns it. The
      repository ASSERTS NOTHING. Explicit usings incl. `System`, `System.Data`, `System.Data.Common`,
      `System.Threading`, `System.Threading.Tasks`. No LINQ.
      Files: `d:\git\QAAutomation\src\QAAutomation.Data\VatReturnRecord.cs`,
      `d:\git\QAAutomation\src\QAAutomation.Data\VatReturnRepository.cs`
      Verify: builds in step 12.

- [ ] 9. Add the `DatabaseResetter` wrapping Respawn.
      Constructor takes `IDatabaseConnectionFactory` and `DatabaseOptions`. A private
      `Respawn.Respawner` field, built lazily and cached (built once per run — comment that this is
      why it is registered SINGLETON). `BuildRespawnerAsync` opens a connection via the factory and
      calls `Respawner.CreateAsync(connection, new RespawnerOptions { DbAdapter = ... })` choosing
      the adapter from `DatabaseOptions.Provider` via an explicit `if`/`else` (not a ternary):
      `DatabaseProvider.Postgres` → `DbAdapter.Postgres`, `DatabaseProvider.SqlServer` →
      `DbAdapter.SqlServer`, else guard-throw. `ResetAsync(CancellationToken cancellationToken)`
      ensures the respawner is built (guard the cached field), opens a connection via the factory
      and calls `await respawner.ResetAsync(connection)`. Note the Respawn 7 API shape may be
      `Respawner.CreateAsync` + `ResetAsync(DbConnection)`; confirm the exact method/overload at
      implement time and adjust. Explicit usings; no LINQ.
      Files: `d:\git\QAAutomation\src\QAAutomation.Data\DatabaseResetter.cs`
      Verify: builds in step 12.

- [ ] 10. Add the `DataRegistration` DI extension.
      Static class `DataRegistration` with
      `IServiceCollection AddDatabaseAccess(this IServiceCollection services, DatabaseOptions options)`,
      mirroring `ApiRegistration.cs` style (null guard on `options`, blank-provider-independent).
      Registers: `options` as singleton; selects the `IDatabaseConnectionFactory` implementation by
      `options.Provider` via explicit `if`/`else` and registers it SCOPED (register the concrete
      type as the interface, scoped); `VatReturnRepository` SCOPED; `DatabaseResetter` SINGLETON
      (so the Respawner is built once per run). Explicit usings incl. `System`,
      `Microsoft.Extensions.DependencyInjection`, `QAAutomation.Core.Configuration`. No LINQ.
      Files: `d:\git\QAAutomation\src\QAAutomation.Data\DataRegistration.cs`
      Verify: builds in step 12.

- [ ] 11. (OPTIONAL) Testcontainers helper — decide and either add cleanly or skip.
      Only if it builds with zero new warnings: pin `Testcontainers.PostgreSql` 4.15.0 and
      `Testcontainers.MsSql` 4.15.0 in `Directory.Packages.props`, reference them (NO Version) in
      `QAAutomation.Data.csproj`, and add a small compile-only `EphemeralDatabase` helper under
      `d:\git\QAAutomation\src\QAAutomation.Data\` that constructs a container builder (not started
      in the default run). If it introduces ANY warning or build risk, SKIP: do not pin the
      packages, do not add the file, and record in the plan notes + steering that Testcontainers
      stays reserved. Default expectation: SKIP unless the build stays perfectly clean.
      Files (if added): `d:\git\QAAutomation\Directory.Packages.props`,
      `d:\git\QAAutomation\src\QAAutomation.Data\QAAutomation.Data.csproj`,
      `d:\git\QAAutomation\src\QAAutomation.Data\EphemeralDatabase.cs`
      Verify: `dotnet build` with no new warnings; if any warning, revert this step entirely.

- [ ] 12. Build the whole solution and clear every warning.
      Files: none (verification gate for steps 1–11).
      Verify: from cwd `d:\git\QAAutomation` run `dotnet build QAAutomation.sln`. Expected:
      "Build succeeded" with 0 warnings and 0 errors — specifically no `using System.Linq` warning
      from the `WarnOnLinqUsage` target, no analyzer/IDE0005 warnings, no NU190x. Fix any warning
      before proceeding (pin flagged transitive packages; step down `Microsoft.Data.SqlClient` if
      `7.1.0` does not build on `net10.0`, and record the version pinned).

- [ ] 13. Wire the data layer into the API and E2E suites.
      Add a `ProjectReference` to `..\..\src\QAAutomation.Data\QAAutomation.Data.csproj` in BOTH
      `QAAutomation.Tests.Api.csproj` and `QAAutomation.Tests.E2E.csproj`. In BOTH
      `ApiScenarioDependencies.cs` and `E2EScenarioDependencies.cs` add one line
      `services.AddDatabaseAccess(testConfiguration.Database);` plus the matching
      `using QAAutomation.Data;`. The UI suite (`QAAutomation.Tests.UI`) must NOT reference
      `QAAutomation.Data` — suite separation.
      Files: `d:\git\QAAutomation\tests\QAAutomation.Tests.Api\QAAutomation.Tests.Api.csproj`,
      `d:\git\QAAutomation\tests\QAAutomation.Tests.E2E\QAAutomation.Tests.E2E.csproj`,
      `d:\git\QAAutomation\tests\QAAutomation.Tests.Api\ApiScenarioDependencies.cs`,
      `d:\git\QAAutomation\tests\QAAutomation.Tests.E2E\E2EScenarioDependencies.cs`
      Verify: `dotnet build QAAutomation.sln` still succeeds with no new warnings.

- [ ] 14. Add the tag-scoped database reset hook to the API suite.
      `[Binding]` class `DatabaseResetHooks` injecting `DatabaseResetter` via constructor (field
      `this.databaseResetter`), with `[AfterScenario("database")] public async Task ResetDatabaseAsync()`
      calling `await this.databaseResetter.ResetAsync(CancellationToken.None)`. CRITICAL: the
      `"database"` tag argument scopes it so it runs ONLY for `@database` scenarios and never for
      the existing API scenarios (which have no DB and would fail trying to connect). Explicit
      usings incl. `System.Threading`, `System.Threading.Tasks`, `Reqnroll`, `QAAutomation.Data`.
      Files: `d:\git\QAAutomation\tests\QAAutomation.Tests.Api\DatabaseResetHooks.cs`
      Verify: step 16 confirms existing scenarios still pass (hook does not fire for them).

- [ ] 15. Add the compile-and-demonstrate example (steps + feature), skipped by default.
      Step class `VatReturnPersistenceSteps` (`[Binding]`) injecting `VatReturnRepository` via
      constructor. A `When` step calls `await this.vatReturnRepository.FindByIdAsync(tenantId, returnId, CancellationToken.None)`
      and stores the result in a private field; a `Then` step asserts the ASSERTION LIVES IN THE
      STEP using AwesomeAssertions: `stored.Should().NotBeNull("...")` and
      `stored.Status.Should().Be(expectedStatus, "...")`. tenantId/returnId/expected come from the
      Gherkin step parameters. `async Task` steps (DB layer is async). Explicit usings incl.
      `System.Threading`, `System.Threading.Tasks`, `AwesomeAssertions`, `QAAutomation.Data`,
      `Reqnroll`. No LINQ, no selectors, no SQL in the step.
      Feature file `VatReturnPersistence.feature` tagged at feature level
      `@api @regression @database @ignore` with one scenario exercising those steps, written
      declaratively (no SQL/connection detail). Add a one-line `#` comment immediately above the
      scenario stating WHY it is ignored (no live database in the default run; it exists to show
      where DB validation lives), to satisfy `bdd-gherkin-standards.md`'s "exclusion tags need a
      linked reason" rule. Note: `@database` is a new feature-area tag not yet in the
      `bdd-gherkin-standards.md` taxonomy table — flag this to the user; adding it to that table is
      a recommended (not required) consistency follow-up, outside this task's steering-edit scope
      (which is limited to `tech-stack.md`).
      Files: `d:\git\QAAutomation\tests\QAAutomation.Tests.Api\Steps\VatReturnPersistenceSteps.cs`,
      `d:\git\QAAutomation\tests\QAAutomation.Tests.Api\Features\VatReturnPersistence.feature`
      Verify: step 16.

- [ ] 16. Record the decision in steering (`tech-stack.md`).
      Append a short, honest entry (matching the file's existing table/prose style) stating that a
      provider-agnostic data-access project `QAAutomation.Data` was activated supporting PostgreSQL
      (`Npgsql` 10.0.3) and SQL Server (`Microsoft.Data.SqlClient` 7.1.0 — or the version actually
      pinned), with `Respawn` 7.0.0 for reset. Note PostgreSQL remains KEYinfinity's documented
      primary engine and SQL Server was added on request for portability. Move
      `Npgsql`/`Respawn` (and `Testcontainers.PostgreSql` only if step 11 activated it) from the
      "Reserved" framing to "Active" as appropriate, and add `Microsoft.Data.SqlClient` and
      `Testcontainers.MsSql` rows with their pinned versions (mark Testcontainers rows reserved if
      step 11 was skipped). Keep the central-package-management pinning rule intact.
      Files: `d:\git\QAAutomation\.kiro\steering\tech-stack.md`
      Verify: re-read the edited section; versions match `Directory.Packages.props` exactly.

- [ ] 17. Final verification gate.
      Files: none.
      Verify (from cwd `d:\git\QAAutomation`):
      1. `dotnet restore QAAutomation.sln` — succeeds, no NU1008, no NU190x.
      2. `dotnet build QAAutomation.sln` — "Build succeeded", 0 warnings, 0 errors (no
         `using System.Linq` warning, no analyzer warnings).
      3. `dotnet test tests\QAAutomation.Tests.Api` — existing API scenarios still pass and the new
         `VatReturnPersistence` scenario is reported SKIPPED (not failed), because of `@ignore`.
         (The API tests hit a live public website; a sandbox with no outbound network may show
         those as failing/erroring on connectivity — that is an environment limitation, not a
         regression from this change. The `@database @ignore` scenario must be skipped regardless.)
      Confirm no files were committed; all changes remain in the working tree.

## Notes / assumptions

- Respawn 7 API: `Respawner.CreateAsync(DbConnection, RespawnerOptions)` and
  `respawner.ResetAsync(DbConnection)` are the expected shapes; confirm exact signatures/overloads
  at implement time and adjust `DatabaseResetter`.
- `Microsoft.Data.SqlClient` 7.1.0 is the current GA (Sept 2026). If it does not restore/build on
  `net10.0`, step down to the latest version that does and record it in both
  `Directory.Packages.props` and the `tech-stack.md` entry.
- Only `tech-stack.md` is edited among steering files (as the task requires). The `@database`
  feature-area tag addition to `bdd-gherkin-standards.md` is flagged as a recommended follow-up,
  not done here.
