# Provider-agnostic database validation layer (`QAAutomation.Data`)

A new `QAAutomation.Data` project introduces a scaffolded database-validation layer: a connection-factory seam over `System.Data.Common`, a `VatReturnRepository` ("the DB class"), a Respawn-based resetter, DI registration, config wiring in Core, a tag-scoped `@database` reset hook, and one `@ignore` example feature + steps. It supports PostgreSQL (Npgsql 10.0.3) and SQL Server (Microsoft.Data.SqlClient 7.1.0), selected at runtime by `DatabaseOptions.Provider`. The intent is to demonstrate *where* database validation lives without connecting to anything in the default run — the system under test is still a public marketing website with no database. Per the coder's verification note the full solution builds with 0 warnings / 0 errors, restore is clean (no NU1008, no NU190x), and the API suite reports 9 passed / 0 failed / 1 skipped (the `@ignore` scenario correctly skipped).

Watch for: (1) a by-design captive dependency — the singleton `DatabaseResetter` depends on the scoped `IDatabaseConnectionFactory`; it never resolves in the default run because the only consumer is tag-scoped to `@database`/`@ignore` (confirmed); (2) the `@database` feature-area tag is absent from the `bdd-gherkin-standards.md` taxonomy and `@ignore` carries no work-item link tag, both deliberately out of this task's scope and flagged by the coder (confirmed); (3) the build/test numbers come from the coder's note and were not independently re-run, per the task's instruction.

**Verdict**: APPROVED

## High-level view

The design puts a single provider-aware seam — `IDatabaseConnectionFactory` — between the drivers and everything above them. The factory returns `System.Data.Common.DbConnection`, so the repository and resetter never name Npgsql or SqlClient. The two concrete factories are the only files that mention a driver, and the drivers are referenced by exactly one project. That is what keeps the database packages out of `Core` and `UI`: `DatabaseOptions` is a driverless POCO in `Core`, and the UI test project references neither `QAAutomation.Data` nor any driver, so a browser-only suite structurally cannot open a connection.

The repository is parameterised correctly — a `private const` ANSI `SELECT` with `@tenantId`/`@returnId` markers bound through `command.CreateParameter()`, never concatenated — and it asserts nothing. `tenantId` is a required explicit argument with a guard, which is the cross-tenant-leak guard made structural. Assertions live in the step class via AwesomeAssertions with `because` reasons.

Configuration follows the existing loader pattern exactly: a new `ReadDatabaseOptions` reuses `ReadWholeNumber`/`RequireSensibleTimeout` and adds two small helpers (`ReadOptionalText`, `ReadDatabaseProvider`) in the file's own explicit, guard-first style. The committed `appsettings.json` carries `Provider=Postgres` and an empty `ConnectionString`; the real secret arrives at runtime via `QAAUTOMATION_Database__ConnectionString`, and the factory fails closed with a message naming that variable when it is blank.

DI lifetimes match the task contract: options and resetter singleton, factory and repository scoped, provider chosen by `DatabaseOptions.Provider` through explicit `if`/`else`. The singleton-depends-on-scoped pairing is a known captive dependency; it is inert in the default run and the coder documented the revisit path, so it is noted rather than blocking.

The reset hook is `[AfterScenario("database")]`, so it fires only for `@database` scenarios; the example feature is `@api @regression @database @ignore`, so it is skipped by default and cannot fail without a live database. The `@database` tag is not yet in the Gherkin taxonomy and `@ignore` has no linked work item — both are real gaps against `bdd-gherkin-standards.md`, but both were explicitly scoped out of this task (steering edits limited to `tech-stack.md`) and flagged for follow-up.

The `Microsoft.Data.SqlClient` 7.1.0 dependency drags in a `System.Security.Cryptography.Pkcs` 9.0.18 that NuGet flags NU1902/NU1903; the coder pinned it to 10.0.13 as a central transitive pin. Central transitive pinning is enabled in the props file, so the pin takes effect and clears both warnings. The `tech-stack.md` decision entry is honest, matches the file's table/prose style, and records the exact pinned versions.

<details>
<summary>Issues (4)</summary>

1. **Captive dependency (non-blocking)** — singleton `DatabaseResetter` consumes scoped `IDatabaseConnectionFactory`. Inert today (tag-scoped to `@database`/`@ignore`, never resolved). When a live `@database` run is enabled, make the stateless factory a singleton or have the resetter open its own scope, or MS DI scope validation will throw at resolve time.
2. **`@database` not in Gherkin taxonomy (non-blocking, out of scope)** — the feature-area tag table in `bdd-gherkin-standards.md` has no `@database`. Add it to that table when the DB layer goes live. Flagged by the coder; this task's steering scope was limited to `tech-stack.md`.
3. **`@ignore` without a work-item link tag (non-blocking, out of scope)** — `bdd-gherkin-standards.md` requires `@ignore` to pair with a tracker link; the example has an explanatory comment but no `@issue:` tag. The template has no real tracker key and the task specified this exact tag set. Add the link + taxonomy entry when the scenario is activated.
4. **Build/test evidence not independently verified** — the green build and 9/0/1 test counts are from the coder's verification note, read per the task's explicit instruction not to re-run. Static review corroborates them (no LINQ, no banned patterns, transitive pin mechanically sound), but the numbers themselves were not reproduced here.

</details>

<details>
<summary>Details</summary>

### Provider-agnostic seam over `System.Data.Common`

`IDatabaseConnectionFactory.OpenAsync` returns `Task<DbConnection>` — the `System.Data.Common` base type, never a concrete connection (confirmed). The two implementations are the only files that name a driver: `NpgsqlConnectionFactory` news up `NpgsqlConnection`, `SqlServerConnectionFactory` news up `SqlConnection`, each opens it with `await connection.OpenAsync(token)` and returns it upcast to `DbConnection`. Everything above the seam — `VatReturnRepository`, `DatabaseResetter` — works purely against `DbConnection`/`DbCommand`/`DbParameter`/`DbDataReader`, so no engine name leaks upward. The provider is chosen once, at registration time, from `DatabaseOptions.Provider`.

```
DatabaseOptions.Provider
        │  (if/else at registration)
        ▼
IDatabaseConnectionFactory ── Npgsql ──► PostgreSQL
   (returns DbConnection)   └ SqlClient ─► SQL Server
        ▲
        │ DbConnection only
   VatReturnRepository · DatabaseResetter   ← no driver names here
```

### Driver confinement and suite isolation

The three drivers (`Npgsql`, `Microsoft.Data.SqlClient`, `Respawn`) are referenced only by `QAAutomation.Data.csproj`, which references `QAAutomation.Core` and nothing else (confirmed). `DatabaseOptions` lives in `Core/Configuration` as a driverless POCO, so `Core` stays engine-free. The API and E2E test projects reference `QAAutomation.Data` and call `services.AddDatabaseAccess(testConfiguration.Database)`; the UI test project references only `Core` and `UI` (confirmed by reading `QAAutomation.Tests.UI.csproj`). A browser-only suite therefore cannot open a connection because the type that knows how is not on its reference graph.

### Parameterised query and the tenant guard

`FindByIdSql` is a `private const string` of portable ANSI SQL — `SELECT tenant_id, return_id, status, total_vat_due FROM vat_return WHERE tenant_id = @tenantId AND return_id = @returnId` — with values bound through a small `AddParameter` helper that calls `command.CreateParameter()`, sets `ParameterName`/`Value`, and adds to `command.Parameters` (confirmed). Nothing is concatenated into the SQL. The `@`-prefixed named-parameter style is accepted by both Npgsql and Microsoft.Data.SqlClient, and `MapRow` reads columns by `GetOrdinal(name)` rather than positional index, so result mapping is order-independent across engines.

`FindByIdAsync` takes `tenantId` as a required explicit argument and guard-throws `ArgumentException` when it is blank; the step passes the tenant from the Gherkin sentence, never from ambient state. The repository returns a mapped `VatReturnRecord` or `null` and asserts nothing.

### Configuration and the fail-closed secret posture

`ReadDatabaseOptions` slots into `TestConfigurationLoader.Load()` alongside the existing four readers and reuses `ReadWholeNumber` + `RequireSensibleTimeout` for the timeout. Two new helpers match the file's style: `ReadOptionalText` (returns `string.Empty` for an absent value — the one reader that tolerates emptiness, on purpose) and `ReadDatabaseProvider` (uses `Enum.TryParse<DatabaseProvider>(value, ignoreCase: true, out ...)` and throws `TestConfigurationException.ForKey(...)` on an unrecognised value). The committed `appsettings.json` carries `Provider=Postgres`, `ConnectionString=""`, `CommandTimeoutSeconds=30` — no secret in source. Presence of the connection string is enforced later, at `OpenAsync`, which throws `InvalidOperationException` naming `QAAUTOMATION_Database__ConnectionString` when blank. A missing secret fails closed with an actionable message rather than a vague connection error. Because the shared `appsettings.json` now carries the `Database` section and every suite links it, the loader runs cleanly for all three suites; the passing API run empirically confirms the section parses.

### DI lifetimes and the captive dependency

`AddDatabaseAccess` registers `DatabaseOptions` singleton, the chosen `IDatabaseConnectionFactory` scoped, `VatReturnRepository` scoped, and `DatabaseResetter` singleton — exactly the lifetimes the task mandated (confirmed). The singleton resetter caches the Respawner (built once per run, which is the stated reason it is a singleton), but it constructor-injects the scoped factory. That is a classic captive dependency: a singleton holding a scoped service. It is harmless in practice here — the factory is stateless (it only holds the singleton `DatabaseOptions`) and, more importantly, nothing resolves `DatabaseResetter` in the default run, because its only consumer is the tag-scoped hook on `@database`/`@ignore` scenarios. The green build and the 9/0/1 API result confirm the container is not eagerly scope-validated at startup. The coder flagged this and named the revisit (make the factory a singleton, or open a scope inside `ResetAsync`) for when a live `@database` run is enabled. Because the task fixed these exact lifetimes and the pairing never materialises today, this is noted, not blocking.

### Tag-scoped reset hook and the skipped example

`DatabaseResetHooks.ResetDatabaseAsync` is annotated `[AfterScenario("database")]`, so Reqnroll only instantiates the class and resolves `DatabaseResetter` when a `@database` scenario actually runs (confirmed). The existing API scenarios have no `@database` tag, so the hook never fires for them — which is what stops the reset path from trying to connect during the ordinary suite. The example `VatReturnPersistence.feature` is tagged `@api @regression @database @ignore` at feature level, and the coder's run reports its single scenario as skipped, not failed. The step class shares state between `When` and `Then` through a private field (one binding instance per scenario), which is sufficient for a single-class demo and avoids an unneeded context type.

### Gherkin taxonomy gap

Two real deviations from `bdd-gherkin-standards.md` ride along, both known. First, `@database` is not in the feature-area tag table, and the file requires every feature to carry a recognised feature-area tag and to extend the table before using a new one. Second, the standard requires `@ignore` to be paired with a work-item link tag (e.g. `@issue:KEYINF-1234`); the example carries an explanatory comment but no link tag. Both are deliberate: the task limited steering edits to `tech-stack.md`, the template has no real tracker key to link to, and the task contract itself specified the exact tag set `@api @regression @database @ignore`. The coder flagged the taxonomy gap for follow-up. These should be closed when the DB layer is activated for real, but they do not block scaffolding whose scenario is skipped by design.

### Transitive security pin

`Microsoft.Data.SqlClient` 7.1.0 pulls in `System.Security.Cryptography.Pkcs` 9.0.18, flagged NU1902 (moderate) and NU1903 (high) under CVE-2026-84008. The fix is a central transitive pin of `System.Security.Cryptography.Pkcs` to 10.0.13 — the first patched build on the `net10.0` line, one ahead of the repo's other 10.0.12 pins. `CentralPackageTransitivePinningEnabled` is `true` in `Directory.Packages.props` (confirmed by reading the file), so the pin propagates across the graph and, per the coder's note, clears both warnings. The pin is well commented with the advisory IDs and a revisit condition.

### Build and test evidence; coverage

The verification note records `dotnet build QAAutomation.sln` as 0 warnings / 0 errors (including no `using System.Linq` warning from the `WarnOnLinqUsage` target), restore clean, and `dotnet test tests\QAAutomation.Tests.Api` as 9 passed / 0 failed / 1 skipped, the skip being the new `@ignore` scenario. Per the task instruction these were read, not re-run. Static review corroborates the build claim: a grep across the new project finds no `using System.Linq`, no `.Result`/`.Wait()`/`.GetAwaiter()`, no `async void`, no `Thread.Sleep`; every file has explicit `using` directives; types are explicit (no `var` outside `new`); fields are camelCase read via `this.`; constants are PascalCase. One sound deviation from the plan: `VatReturnRepository` omits the plan's suggested `using System.Data;` because it uses nothing from that namespace — including it would have raised an IDE0005 unused-using warning, so omitting it is correct against the zero-warning bar.

Not tested: the UI and E2E suites were not run (they need Chrome + network); the coder relies on the solution build to prove they compile. No live-database path was exercised — by design, since there is no database. There is no unit test asserting the provider→adapter or provider→factory mapping, so a future wrong-enum regression in `ChooseDbAdapter`/`RegisterConnectionFactory` would only surface at runtime; acceptable for `@ignore` scaffolding.

</details>

<details>
<summary>File map</summary>

New — `src/QAAutomation.Data/`:
- `QAAutomation.Data.csproj` — references Core + the three drivers + DI abstractions; added to the solution under `src`.
- `IDatabaseConnectionFactory.cs` — the seam; `OpenAsync` returns `DbConnection`.
- `NpgsqlConnectionFactory.cs` / `SqlServerConnectionFactory.cs` — the only driver-aware files; fail-closed on a blank connection string.
- `VatReturnRecord.cs` — plain data carrier.
- `VatReturnRepository.cs` — parameterised ANSI `SELECT`, required `tenantId` guard, asserts nothing.
- `DatabaseResetter.cs` — wraps Respawn; adapter chosen from provider; Respawner cached (singleton).
- `DataRegistration.cs` — `AddDatabaseAccess`: options + resetter singleton, factory + repository scoped.

New — tests:
- `tests/QAAutomation.Tests.Api/DatabaseResetHooks.cs` — `[AfterScenario("database")]`, tag-scoped.
- `tests/QAAutomation.Tests.Api/Steps/VatReturnPersistenceSteps.cs` — assertion in the step (AwesomeAssertions).
- `tests/QAAutomation.Tests.Api/Features/VatReturnPersistence.feature` — `@api @regression @database @ignore`.

Modified:
- `src/QAAutomation.Core/Configuration/DatabaseOptions.cs` (new) — `DatabaseProvider` enum + driverless POCO.
- `src/QAAutomation.Core/Configuration/TestConfiguration.cs` — adds `Database` property.
- `src/QAAutomation.Core/Configuration/TestConfigurationLoader.cs` — `ReadDatabaseOptions` + two helpers.
- `src/QAAutomation.Core/Configuration/appsettings.json` — `Database` section, empty connection string.
- `tests/QAAutomation.Tests.Api/*.csproj` + `ApiScenarioDependencies.cs` — reference Data, call `AddDatabaseAccess`.
- `tests/QAAutomation.Tests.E2E/*.csproj` + `E2EScenarioDependencies.cs` — same wiring.
- `Directory.Packages.props` — pins Npgsql 10.0.3, Microsoft.Data.SqlClient 7.1.0, Respawn 7.0.0, System.Security.Cryptography.Pkcs 10.0.13 (transitive).
- `QAAutomation.sln` — adds the Data project under `src`.
- `.kiro/steering/tech-stack.md` — "Decision — provider-agnostic data access" entry; Active/Reserved tables updated.

Full diff: `git diff` plus the untracked files under `src/QAAutomation.Data/` and `tests/QAAutomation.Tests.Api/` listed above.

</details>
