---
inclusion: always
---

# Tech stack (pinned)

C# BDD automation for UI, API and E2E. Every version below was verified against the NuGet v3 API on 2026-10-07; the Selenium and `Microsoft.Extensions.Configuration` rows were re-verified on the driver swap. Treat this table as the single source of truth for package choices.

Two sections follow. **Active** packages are the ones the current template uses. **Reserved** packages are the right long-term answers for KEYinfinity but are deliberately not wired in yet — see the reasoning under that heading.

## Active — the current template

| Concern | Choice | Pinned version | Why |
| --- | --- | --- | --- |
| Target framework | `net10.0` | SDK 10.0.301 installed locally (8.0.425 also present) | .NET 10 is the current LTS. The TFM should track the LTS of the system under test — if the SUT stays on net8.0, retarget rather than forcing net10.0. |
| BDD runner | Reqnroll (`Reqnroll`, `Reqnroll.NUnit`) | 3.3.4 | SpecFlow is dead: Tricentis removed the SpecFlow repo from GitHub in Dec 2024 and the last SpecFlow NuGet is 3.9.74. Reqnroll is the maintained fork (forked Jan 2024), Cucumber-compatible, with a SpecFlow-compatible migration path. |
| DI for steps | `Reqnroll.Microsoft.Extensions.DependencyInjection` | 3.3.4 | Use the standard Microsoft DI container in step definitions instead of BoDi. |
| Unit test framework | `NUnit` | 4.6.1 — **not** 5.0.0 | NUnit 5.0.0 exists, but Reqnroll docs state NUnit 3.13.1+ support and NUnit 5 is brand new / unverified against Reqnroll 3.3.4. Pin 4.6.1. |
| Test adapter | `NUnit3TestAdapter` | 6.3.0 | Despite the name, NUnit3TestAdapter supports NUnit 4.x. |
| Test SDK | `Microsoft.NET.Test.Sdk` | 18.10.1 | Required for `dotnet test` / VSTest discovery. |
| UI / E2E driver | `Selenium.WebDriver` | 4.50.0 | The driver. Chosen over Playwright — see the decision note below. Synchronous C# API, so UI page objects are synchronous. |
| UI waits | `Selenium.Support` | 4.50.0 | Supplies `WebDriverWait` from `OpenQA.Selenium.Support.UI`. **Required, not optional** — Selenium has no auto-waiting, so every wait in the suite goes through this package via one shared helper. |
| Assertions | `AwesomeAssertions` | 9.6.0 | **Licensing-critical — see warning below.** Community Apache-2.0 fork of FluentAssertions with a near-identical API. |
| REST client | `Refit` | 16.3.0 | Refit typed interfaces so API contracts are declarative and reusable from step definitions. Plain `HttpClient`/`IHttpClientFactory` only for one-off probes. |
| Configuration | `Microsoft.Extensions.Configuration` | 10.0.12 | Binds `*Options` classes under `Configuration/`. Matches the `net10.0` TFM. |
| Configuration — JSON | `Microsoft.Extensions.Configuration.Json` | 10.0.12 | Reads committed `appsettings*.json` (placeholders only, never secrets). |
| Configuration — environment | `Microsoft.Extensions.Configuration.EnvironmentVariables` | 10.0.12 | How secrets and per-environment overrides actually reach the test process. |
| REST client — DI registration | `Refit.HttpClientFactory` | 16.3.0 | `api-automation.md` §1.4 mandates `IHttpClientFactory` registration rather than `RestService.For<T>()`, and this package supplies the `IServiceCollection` extension that does it. |
| HTTP client factory | `Microsoft.Extensions.Http` | 10.0.12 | The `IHttpClientFactory` implementation itself, plus the `DelegatingHandler` pipeline the correlation-id handler plugs into. Matches the `net10.0` TFM. |
| DI abstractions | `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.12 | `QAAutomation.Core` exposes `IServiceCollection` registration extensions, and `Core` takes no runner reference, so it needs the abstractions package directly. |

Refit 16 registers through `services.AddRefitGeneratedClient<T>()`. Do **not** use `AddRefitClient<T>()` — it compiles but fails at run time demanding the separate `Refit.Reflection` package, which is not pinned here.

That is the whole active set. Nothing else goes into `Directory.Packages.props` without a decision recorded here.

If typed binding via `ConfigurationBinder.Get<T>()` is wanted later, the package is `Microsoft.Extensions.Configuration.Binder` **10.0.12** — pin that version when it is added. Until then, read settings through `GetSection(...)` and assign to properties explicitly, which is also the plainer thing to read.

## Decision — Selenium over Playwright

Playwright is the stronger tool on paper. Selenium is the right tool here, for one reason that outranks the feature comparison: **the person who has to own and maintain this framework is not yet familiar with Playwright.** A framework nobody on the team can confidently debug at 9am on a release day is not an asset. The job description also lists Selenium as acceptable, so this is a supported choice rather than a workaround.

Be honest about what that costs:

| Cost | Consequence for this suite |
| --- | --- |
| **No auto-waiting.** Selenium acts immediately and throws if the element is not ready. | Explicit waits are **mandatory**, not a style preference. `Thread.Sleep` stays banned outright — the answer is the shared `WebDriverWait` helper, never a sleep. A bare `FindElement` on anything the app renders asynchronously is a defect waiting to happen. |
| **No trace viewer.** There is no Playwright-style timeline, DOM snapshot or network tab to open after a failure. | Failure diagnostics rely on what we capture ourselves: **screenshot + page source + browser console logs**, written to `test-results/` on failure. Those three must be wired up on day one, because without them an intermittent CI failure is unexplainable. |

Revisit only if the team's Playwright familiarity changes, and then as a deliberate migration, not a drive-by.

### Selenium Manager — and the three packages we deliberately do not take

Selenium Manager is built into Selenium 4.6+ and resolves and downloads the matching browser driver automatically. That removes the whole driver-version-drift problem class, and it removes three packages people reflexively add:

| Do **not** add | Latest version | Why not |
| --- | --- | --- |
| `Selenium.WebDriver.ChromeDriver` | 155.0.8059.3900 | Not needed. Selenium Manager already resolves the driver. Pinning a ChromeDriver version reintroduces exactly the version-drift problem Selenium Manager solved: the browser updates, the pin does not, the suite goes red for no product reason. |
| `WebDriverManager` | 2.17.7 | Not needed, same reason. It predates Selenium Manager and now duplicates it. |
| `DotNetSeleniumExtras.WaitHelpers` | 3.11.0 | **Abandoned** — exactly one release, ever. It is the old `ExpectedConditions` helper that was removed from Selenium in v4. Do not take a dependency on it. Write one small wait helper in `src/QAAutomation.Core/Utilities` built on `WebDriverWait` with a `Until(driver => ...)` lambda instead. That lambda is permitted under exception 2 in #[[file:code-style.md]] — `Until` accepts nothing else. |

## Reserved — not used in the current template

These keep their pins and their rationale, because they are still the right choices for KEYinfinity. They are **not** in the current template.

Why: the current target is a **public marketing website**. No database, no tenancy, no third-party dependency to stub. Wiring these in now would add weight and setup cost while teaching nothing, and a template nobody can read end to end in one sitting does not get used. Each one comes back the moment the thing it exists for exists.

| Concern | Choice | Pinned version | Bring back when |
| --- | --- | --- | --- |
| Third-party stubbing | `WireMock.Net` | 2.18.0 | There is a third-party boundary to stub. Essential for MTD / HMRC submission paths — a real tax authority cannot be called from CI. |
| Ephemeral database | `Testcontainers.PostgreSql`, `Npgsql` | 4.15.0 / 10.0.3 | The SUT has a database. Matches the PostgreSQL + Kubernetes reality of KEYinfinity. |
| DB reset between scenarios | `Respawn` | 7.0.0 | There is a database to reset. Deterministic truncate-and-reseed between scenarios instead of per-test transactions. |
| Test data generation | `Bogus` | 35.6.5 | Scenarios create their own data. Generated, non-PII test data. |
| Resilience / polling | `Polly` | 8.8.0 | There is an async back-end state to settle on. **API polling only** — never to paper over a UI race; UI waits go through the Selenium wait helper. |
| Reporting | `Allure.Reqnroll` | 2.15.0 | Stakeholders need a rich report. Reqnroll's built-in LivingDoc is the lighter option; CI-native test results remain the pass/fail signal of record. |
| Snapshot assertions | `Reqnroll.Verify` | 3.3.4 | There are wide, report-shaped outputs (VAT return boxes) where field-by-field assertions are noise. |
| REST client — alternative | `RestSharp` | 114.0.0 | A request must be composed dynamically (fuzzing headers, malformed payloads). Costs the compile-time contract, so Refit stays the default. |

A reserved package is still subject to the pinning rule: when it is activated, it is added to `Directory.Packages.props` at the version above, not at whatever is latest that day.

## Central Package Management

Versions live in **one** `Directory.Packages.props` at the repository root (convention described here; the file is created during framework build-out, not by this steering file).

1. The props file sets `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`.
2. Every package gets one `<PackageVersion Include="..." Version="..." />` entry there.
3. Project files reference packages with **no** `Version` attribute: `<PackageReference Include="Reqnroll.NUnit" />`.

```xml
<!-- Directory.Packages.props (illustrative shape, root of repo) -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Reqnroll.NUnit" Version="3.3.4" />
    <PackageVersion Include="AwesomeAssertions" Version="9.6.0" />
  </ItemGroup>
</Project>
```

**Rule: do not add a package without pinning it in `Directory.Packages.props`.** No floating ranges (`*`, `[9.0,)`), no `Version` attributes in `.csproj`, no transitive version drift.

## Licence warning — FluentAssertions

FluentAssertions v8+ (current 8.11.0) moved to an Xceed commercial licence: free for OSS / non-commercial use only, paid per-seat for commercial use. This product is commercial, so taking FluentAssertions 8+ creates a licence liability.

- **Use `AwesomeAssertions` 9.6.0** (Apache-2.0, near-identical API).
- Do not add `FluentAssertions` to any project, directly or transitively, without a licence decision.
- Fallback if a true drop-in is ever required: FluentAssertions **7.x**, which remains open-source.

## Deliberate deferrals

| Item | Status | Revisit when |
| --- | --- | --- |
| NUnit 5.0.0 | Deferred. Pinned to 4.6.1. | Reqnroll publishes confirmed NUnit 5 support. Then bump `NUnit` and `NUnit3TestAdapter` together and run the full suite before merging. |
| SpecFlow | Not an option. Unmaintained. | Never. New work targets Reqnroll. |
| Playwright | Deferred by choice, not ruled out. | The team is comfortable owning it. See the decision note above. |
| Azure DevOps CI wiring | **Parked by the user.** No pipeline files exist yet. | The suite runs reliably locally. The conventions are already written down in #[[file:ci-azure-devops.md]] and cost nothing while parked. |

## Related

- Layout, naming and glob conventions: #[[file:project-structure.md]]
- Driver lifecycle, waits and locators: #[[file:ui-automation-selenium.md]]

Secrets note: no snippet in this repository may contain a real key, token or connection string. Use obvious placeholders such as `YOUR_API_KEY_HERE`, and never log secrets or unmasked PII.
