# Implementation Plan — C# BDD Selenium automation template

Target: a **working, compiling, passing** Reqnroll + NUnit + Selenium + Refit template in `d:\git\QAAutomation`, built to the corrected steering in `.kiro`. All paths below are absolute.

Not a git repository. Do **not** run `git init`, do not create a worktree, do not commit.

---

## Verification spike — already done, do not repeat

The following were proved on this machine before the plan was written. Treat them as facts.

| Checked | Result |
| --- | --- |
| `Reqnroll.NUnit` 3.3.4 + `NUnit` 4.6.1 + `NUnit3TestAdapter` 6.3.0 + `Microsoft.NET.Test.Sdk` 18.10.1 on `net10.0` | restores, builds, `dotnet test` discovers and passes |
| `Selenium.WebDriver` / `Selenium.Support` 4.50.0, headless Chrome, Selenium Manager | driver resolved automatically, no driver package, no install step |
| Central package management with `CentralPackageTransitivePinningEnabled=true` across all 16 active packages | restores clean, no version conflict |
| Instance `[Binding]` hook with constructor-injected **scoped** service, shared with step classes | works — the driver started in `[BeforeScenario]` is the same instance the page object receives |
| `services.AddScoped<IWebDriver>(provider => provider.GetRequiredService<ScenarioDriver>().Driver)` | resolves lazily at first step, i.e. after the hook started the browser |
| `driver.Manage().Logs.GetLog(LogType.Browser)` after `ChromeOptions.SetLoggingPreference(LogType.Browser, LogLevel.All)` | returns entries (3 on the Landmark homepage) |
| `dotnet test --filter "TestCategory=smoke"` | Reqnroll's NUnit generator emits `[Category]` per tag, so tag filtering works |
| **Refit 16.3.0 registration** | `AddRefitClient<T>()` **fails at run time** — "needs the reflection request builder… add `Refit.Reflection`". Use **`AddRefitGeneratedClient<T>()`**. `RestService.For<T>()` also works but is not the DI path. |
| `IApiResponse<T>` against the WordPress API | 200/400/404 all returned without throwing; `response.Headers` exposes `X-WP-Total`; `response.Error` is `ApiExceptionBase` — cast to `ApiException` to read `.Content` |
| 400 body for `per_page=0` | `{"code":"rest_invalid_param","message":"Invalid parameter(s): per_page",...}` |
| `GET /pages?per_page=2&_fields=id,slug,title,link` | `X-WP-Total: 18`, `X-WP-TotalPages: 9`; `title` is `{ "rendered": "..." }` |
| `GET /pages/1785` | `slug: technical`, `title.rendered: Technical`, `link: https://www.landmarksystems.co.uk/technical/` |
| `https://www.landmarksystems.co.uk/technical/` browser title | `Technical - Landmark minimum recommended hardware requirements \| Landmark Systems` — so `title.rendered` is a case-insensitive **substring**, never an equal |
| Homepage DOM title | `Farm & Rural Estate Management Software \| Landmark Systems` (raw HTML has `&amp;`; the DOM title has a literal `&`) |
| Homepage nav | **two** `<nav class="site-header__navigation">` elements (desktop + mobile). Only one is `Displayed` at 1920×1080 |
| Homepage `<h1>` | **four** of them (carousel slides). Never assert on an `h1` here |
| `/contact-us/` form | Gravity Forms, `<form id='gform_2' … novalidate>`, `gform_submission_method=iframe`. **`novalidate` means there is no client-side validation at all** |
| `/contact-us/` fields | `input_2_4` Company Name (optional), `input_2_5` First Name\*, `input_2_19` Surname\*, `input_2_6` Postcode\*, `input_2_7` Email address\*, `input_2_8` Telephone\*, `input_2_9` Mobile (optional), `input_2_10` Your Enquiry (optional). The five starred fields carry `aria-required="true"`; the optional ones do not |
| SDKs | `10.0.301` and `8.0.425` — target `net10.0` |

---

## Design decisions made here (not re-litigated downstream)

| # | Decision | Reason |
| --- | --- | --- |
| D1 | **The enquiry-form scenario never clicks submit.** `ContactUsPage` has no submit method at all. It asserts the form's declared mandatory-field contract (`aria-required="true"`) and that typed values are held. | The brief asked for an assertion on *client-side* validation after an invalid submit. The live form carries `novalidate`, so no client-side validation exists — the only validation is server-side behind an AJAX POST to a **production** site. The brief's own hard rule ("READ-ONLY… do NOT submit") outranks the softer wording. Structural enforcement (no method) stops anyone "finishing" it later. |
| D2 | E2E asserts the browser title **contains** the API's `title.rendered`, case-insensitively. | Titles are SEO titles with no uniform suffix. Verified true for `technical`. Equality is not assertable. |
| D3 | Page objects use the most stable selector actually available: `nav.site-header__navigation` scoped to the first **visible** match, Gravity Forms' deterministic `input_<formId>_<fieldId>` ids, and `href`-based nav links. No `data-testid`. | A third-party WordPress site has none and we cannot add any. Documented exception to `ui-automation-selenium.md` §1, restated as a comment at the top of every page object and in the README. `TestIdLocator` still ships, unused and commented, because it is the KEYinfinity primary strategy. |
| D4 | `[ScenarioDependencies]` factory and the `[Binding]` hook classes live at **each test project root**, not in `Core/Support`. Only the plain implementation classes live in `Core/Support`. | Reqnroll discovers both attributes in binding (test) assemblies. A shared `[Binding]` hook would launch a browser for the API suite, breaking the suite-separation rule in `project-structure.md`. `Core` also keeps zero runner coupling. The steering's *intent* — one factory, never scattered across step classes — is preserved. |
| D5 | Scenario context classes live at the owning test project root, not `Core/Support`. | A context holding Refit contract records would force `Core` to reference `Api` and invert the one-way dependency direction. Dependency direction outranks folder placement. |
| D6 | No tag-driven conditional driver start. `Tests.Api` registers **no** driver and has **no** hooks; `Tests.UI` and `Tests.E2E` always start one. | Removes an `if` and a concept for a non-C# reader, and makes suite separation structural rather than conditional. |
| D7 | `<Nullable>disable</Nullable>` | `code-style.md`'s own reference snippets assign `null` to non-nullable locals. A template that warns on its own house style teaches noise. |
| D8 | `<ImplicitUsings>disable</ImplicitUsings>` | The SDK implicit using set **includes `System.Linq`**, which would silently re-enable the single most important banned construct. Explicit usings also make each file self-explanatory to a learner. |
| D9 | The no-LINQ mechanical signal is a 15-line MSBuild target in `Directory.Build.targets` that `findstr`s hand-written sources for `using System.Linq` and emits an MSBuild **warning**; `.editorconfig` carries style only. | `.editorconfig` cannot ban a namespace, and `BannedApiAnalyzers` would add an unpinned package `tech-stack.md` has not sanctioned. **Verified**: warns on an offending file, `Build succeeded`, `0 Error(s)`. Upgrade path to `BannedApiAnalyzers` noted in the README. |
| D10 | Options are registered as **concrete singletons** (`services.AddSingleton(webDriverOptions)`), never `IOptions<T>`. Configuration is loaded inside the `[ScenarioDependencies]` factory, not a `[BeforeTestRun]` hook. | A constructor parameter of `WebDriverOptions` is readable to a newcomer; `IOptions<T>.Value` is one more concept for no benefit. Loading in the factory removes a whole hook class from the API suite; the file is ~1 KB. |
| D11 | `tech-stack.md` gains three Active rows: `Refit.HttpClientFactory` 16.3.0, `Microsoft.Extensions.Http` 10.0.12, `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.12. | `api-automation.md` §1.4 mandates `IHttpClientFactory`, which needs the first two; `Core`'s DI registration extension needs `IServiceCollection` from the third. `tech-stack.md` requires the decision be recorded there before the pin exists. |
| D12 | `bdd-gherkin-standards.md` tag taxonomy gains `@marketing`. | The taxonomy table says a new feature-area tag may only be added by updating the table first. The template's SUT is a marketing website; none of `@vat`/`@bankrec`/… fit. |
| D13 | Accepted and rejected `per_page` boundaries go in **two** `Scenario Outline`s with separate `Examples` tables and no expected-outcome column. | `bdd-gherkin-standards.md` requires valid and invalid partitions in separate tables because their `Then` differs; with the tables split, a constant outcome column would be padding. A comment in the feature says so. |
| D14 | Failure artefacts resolve to the repo-root `d:\git\QAAutomation\test-results\` by walking up from `AppContext.BaseDirectory` until `QAAutomation.sln` is found. | Writing into `bin/Debug/net10.0/test-results` is undiscoverable for a QA. The walk-up is eight plain lines and teachable. Overridable with `QAAUTOMATION_Artefacts__Directory`. |

---

## Plan

- [ ] 1. Record the three new package pins and the `@marketing` tag in steering, so the rest of the build is compliant rather than retro-justified.
      Add an Active table row each for `Refit.HttpClientFactory` 16.3.0, `Microsoft.Extensions.Http` 10.0.12 and `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.12 (reason per D11). In the Selenium/Refit area add a one-line note that Refit 16 registers through `AddRefitGeneratedClient<T>()`, because `AddRefitClient<T>()` needs the extra `Refit.Reflection` package. Add a `@marketing` row to the feature-area dimension of the tag taxonomy table.
      Files: `d:\git\QAAutomation\.kiro\steering\tech-stack.md`, `d:\git\QAAutomation\.kiro\steering\bdd-gherkin-standards.md`
      Verify: re-read both files; the three pins and the `@marketing` row are present and no other row changed.

- [ ] 2. Create the repository-root build configuration.
      `Directory.Build.props`: `net10.0`, `ImplicitUsings disable`, `Nullable disable`, `TreatWarningsAsErrors false`, `EnforceCodeStyleInBuild true`, `IsPackable false`, `LangVersion latest`.
      `Directory.Packages.props`: `ManagePackageVersionsCentrally true`, `CentralPackageTransitivePinningEnabled true`, and exactly these `PackageVersion` rows — `Reqnroll` 3.3.4, `Reqnroll.NUnit` 3.3.4, `Reqnroll.Microsoft.Extensions.DependencyInjection` 3.3.4, `NUnit` 4.6.1, `NUnit3TestAdapter` 6.3.0, `Microsoft.NET.Test.Sdk` 18.10.1, `Selenium.WebDriver` 4.50.0, `Selenium.Support` 4.50.0, `AwesomeAssertions` 9.6.0, `Refit` 16.3.0, `Refit.HttpClientFactory` 16.3.0, `Microsoft.Extensions.Configuration` 10.0.12, `Microsoft.Extensions.Configuration.Json` 10.0.12, `Microsoft.Extensions.Configuration.EnvironmentVariables` 10.0.12, `Microsoft.Extensions.Http` 10.0.12, `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.12. No reserved package, and **none** of `Selenium.WebDriver.ChromeDriver`, `WebDriverManager`, `DotNetSeleniumExtras.WaitHelpers`. No `FluentAssertions`.
      `Directory.Build.targets`: the `WarnOnLinqUsage` target from D9 — an `ItemGroup` of `@(Compile)` excluding `$(BaseIntermediateOutputPath)**\*.cs;obj\**\*.cs;bin\**\*.cs`, an `Exec` running `findstr /M /C:"using System.Linq"` over those paths with `ContinueOnError`, `IgnoreExitCode`, `ConsoleToMSBuild`, and a `<Warning>` naming the offending files and pointing at `.kiro/steering/code-style.md`.
      `.editorconfig`: `root = true`; UTF-8, CRLF, final newline, 4-space indent, 2-space for `*.{feature,json,yml,yaml,md}`; `csharp_style_var_for_built_in_types`/`_when_type_is_apparent`/`_elsewhere` all `false:suggestion`; `dotnet_diagnostic.IDE0005.severity = warning`; `generated_code = true` for `*.feature.cs`.
      `.gitignore`: `bin/`, `obj/`, `test-results/`, `TestResults/`, `*.trx`, `*.user`, `.vs/`, `allure-results/`.
      Files: `d:\git\QAAutomation\Directory.Build.props`, `d:\git\QAAutomation\Directory.Packages.props`, `d:\git\QAAutomation\Directory.Build.targets`, `d:\git\QAAutomation\.editorconfig`, `d:\git\QAAutomation\.gitignore`
      Verify: nothing to build yet — `Get-Content` each file and confirm it parses as XML / valid editorconfig. Real verification lands in item 5.

- [ ] 3. Create the solution and all six empty projects with their package and project references, then confirm the dependency direction.
      `dotnet new sln -n QAAutomation` at the root, then one `dotnet new classlib` for each of `src/QAAutomation.Core`, `src/QAAutomation.UI`, `src/QAAutomation.Api` and one `dotnet new classlib` for each of `tests/QAAutomation.Tests.UI`, `tests/QAAutomation.Tests.Api`, `tests/QAAutomation.Tests.E2E` (classlib, then add the test packages by hand — do **not** use `dotnet new nunit`, which writes its own versions). Delete every generated `Class1.cs`. Add all six to the solution. Every `PackageReference` is written **without a `Version` attribute**.
      References: `Core` → packages only (`Selenium.WebDriver`, `Selenium.Support`, `Microsoft.Extensions.Configuration`, `.Json`, `.EnvironmentVariables`, `Microsoft.Extensions.DependencyInjection.Abstractions`), no project reference, no runner. `UI` → `Core`. `Api` → `Core`, plus `Refit`, `Refit.HttpClientFactory`, `Microsoft.Extensions.Http`, `Microsoft.Extensions.DependencyInjection.Abstractions`. Each test project → `Microsoft.NET.Test.Sdk`, `NUnit`, `NUnit3TestAdapter`, `Reqnroll.NUnit`, `Reqnroll.Microsoft.Extensions.DependencyInjection`, `AwesomeAssertions`, plus `<IsTestProject>true</IsTestProject>`. `Tests.UI` → `Core`, `UI`. `Tests.Api` → `Core`, `Api`. `Tests.E2E` → `Core`, `UI`, `Api`. **No test project references another test project.**
      Each test project also links the shared settings file so it lands in the test output deterministically:
      `<None Include="..\..\src\QAAutomation.Core\Configuration\appsettings.json" Link="appsettings.json" CopyToOutputDirectory="PreserveNewest" />`
      Files: `d:\git\QAAutomation\QAAutomation.sln`, `d:\git\QAAutomation\src\QAAutomation.Core\QAAutomation.Core.csproj`, `d:\git\QAAutomation\src\QAAutomation.UI\QAAutomation.UI.csproj`, `d:\git\QAAutomation\src\QAAutomation.Api\QAAutomation.Api.csproj`, `d:\git\QAAutomation\tests\QAAutomation.Tests.UI\QAAutomation.Tests.UI.csproj`, `d:\git\QAAutomation\tests\QAAutomation.Tests.Api\QAAutomation.Tests.Api.csproj`, `d:\git\QAAutomation\tests\QAAutomation.Tests.E2E\QAAutomation.Tests.E2E.csproj`
      Verify: `dotnet restore d:\git\QAAutomation\QAAutomation.sln` succeeds with no NU1008 (version attribute under central management), no NU1605 downgrade and no version-conflict error.

- [ ] 4. Create the typed configuration layer in `Core/Configuration`.
      `appsettings.json` with four sections — `WebDriver` (`Browser: "chrome"`, `Headless: true`, `WindowWidth: 1920`, `WindowHeight: 1080`, `ElementTimeoutSeconds: 20`, `PageLoadTimeoutSeconds: 60`), `Api` (`BaseUrl: "https://www.landmarksystems.co.uk/wp-json/wp/v2"`, `TimeoutSeconds: 30`, `TenantId: "YOUR_TENANT_ID_HERE"`, `ApiKey: "YOUR_API_KEY_HERE"`), `Site` (`BaseUrl: "https://www.landmarksystems.co.uk"`, `HomePageTitle: "Farm & Rural Estate Management Software | Landmark Systems"`, `ContactPagePath: "/contact-us/"`, `KnownPageSlug: "technical"`, `MissingPageId: 999999`, `MinimumPageSize: 1`, `MaximumPageSize: 100`), `Artefacts` (`Directory: "test-results"`). **Only placeholders for anything secret-shaped**; a comment in the README explains that public base URLs are not secrets but `ApiKey`/`TenantId` are placeholders demonstrating the override pattern.
      `WebDriverOptions.cs`, `ApiOptions.cs`, `SiteOptions.cs`, `ArtefactOptions.cs`: plain classes, settable properties, no attributes, no behaviour.
      `TestConfiguration.cs`: a plain holder exposing the four options objects.
      `TestConfigurationLoader.cs`: `public static TestConfiguration Load()` — builds an `IConfigurationRoot` from `AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false)` then `AddEnvironmentVariables("QAAUTOMATION_")`, reads every value through `GetSection(...)` with explicit assignment (no `Get<T>()`, no binder package), and validates with guard clauses: both base URLs absolute and `https`, timeouts 1–300 s, window width ≥ 320 and height ≥ 480, `HomePageTitle` non-empty, `ContactPagePath` starts with `/`, `KnownPageSlug` non-empty, `MissingPageId > 0`, `1 <= MinimumPageSize <= MaximumPageSize <= 100`.
      `TestConfigurationException.cs`: one exception type whose message names the **exact configuration key** and the offending value, and never dumps the whole configuration object (it can carry an injected key).
      Files: `d:\git\QAAutomation\src\QAAutomation.Core\Configuration\appsettings.json`, `...\WebDriverOptions.cs`, `...\ApiOptions.cs`, `...\SiteOptions.cs`, `...\ArtefactOptions.cs`, `...\TestConfiguration.cs`, `...\TestConfigurationLoader.cs`, `...\TestConfigurationException.cs`
      Verify: `dotnet build d:\git\QAAutomation\src\QAAutomation.Core` succeeds with 0 errors and 0 warnings.

- [ ] 5. Create `Core/Utilities` and `Core/Support`, then prove the LINQ warning fires.
      `Utilities/ElementWaits.cs` — the **one** wait helper for the whole suite, built on `WebDriverWait` from `Selenium.Support`, constructed with `(IWebDriver driver, TimeSpan timeout)`. Methods: `WaitUntilVisible(By)`, `WaitUntilClickable(By)`, `WaitUntilFirstVisible(By)` (returns the first `Displayed` match — this is what makes the duplicated `site-header__navigation` deterministic), `WaitUntilGone(By)`, `WaitUntilTitleContains(string)`. One private `CreateWait()` calling `IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException))`. Every `Until` lambda is written **here and only here**, with the code-style exception-2 comment above it.
      `Utilities/TestIdLocator.cs` — `ByTestId(string)` over a `data-testid` constant. Header comment: deliberately unused against the WordPress placeholder; it is the primary strategy on KEYinfinity.
      `Utilities/HeaderValueReader.cs` — `static string ReadFirstValue(HttpResponseHeaders headers, string name)` using `TryGetValues` then a `foreach` with `break`; returns `null` when absent.
      `Utilities/PositiveIntegerParser.cs` — `static bool TryParsePositive(string value, out int result)` using `int.TryParse` with `NumberStyles.Integer` and `CultureInfo.InvariantCulture`, then `> 0`.
      `Utilities/FileNameSanitiser.cs` — `static string ToSafeFileName(string value)`, `foreach` over chars replacing anything outside `[A-Za-z0-9-_]` with `-`, capped at 80 characters. No regex.
      `Utilities/UrlHostChecker.cs` — `static bool HasSameHost(string expectedBaseUrl, string candidateUrl)`, case-insensitive host compare. Guards the E2E hand-off so a URL from the API is never navigated to blindly.
      `Utilities/ArtefactPaths.cs` — `static string ForScenario(string configuredDirectory, string suiteName, string scenarioTitle)`. If `configuredDirectory` is rooted, use it; otherwise walk up from `AppContext.BaseDirectory` until a folder containing `QAAutomation.sln` is found (D14), falling back to `AppContext.BaseDirectory`. Creates `<root>\<configuredDirectory>\<suite>\<sanitised title>-<yyyyMMdd-HHmmss>\`.
      `Support/WebDriverFactory.cs` — `static IWebDriver Create(WebDriverOptions options)`. Guard clause: anything other than `"chrome"` throws `TestConfigurationException` naming `WebDriver:Browser`. Builds `ChromeOptions` with `--headless=new` when `Headless`, `--window-size=<w>,<h>`, `--disable-gpu`, and `SetLoggingPreference(LogType.Browser, LogLevel.All)` so console logs are capturable. Sets `Timeouts().ImplicitWait = TimeSpan.Zero` (steering rule 4) and `PageLoad` from config. Comment stating Selenium Manager resolves the driver, so there is no driver package and no install step.
      `Support/ScenarioDriver.cs` — holds the per-scenario `IWebDriver`. `Start()`, `Quit()` (calls `Quit()` then `Dispose()`, never `Close()`), `IsStarted`, and a `Driver` property whose guard throws a plain-English `InvalidOperationException` if the browser was not started.
      `Support/FailureArtefacts.cs` — `Capture(string suiteName, string scenarioTitle)` writing `screenshot.png`, `page-source.html`, `console.log` and `location.txt` (URL + title) into the `ArtefactPaths` folder. The console-log read is wrapped in `try`/`catch (WebDriverException)` so a missing log cannot break teardown. Comment warning that page source and console logs can carry tokens and PII and must not be published unmasked.
      `Support/ScenarioCorrelation.cs` — scoped class exposing one `CorrelationId` GUID per scenario, used in artefact folder names and sent as a request header, so UI and API evidence line up.
      `CoreRegistration.cs` — two `IServiceCollection` extension methods: `AddTestConfiguration(TestConfiguration)` registering the four options objects as concrete singletons, and `AddBrowserSupport()` registering `ScenarioDriver` (scoped), `IWebDriver` (scoped factory reading `ScenarioDriver.Driver`), `ElementWaits` (scoped, timeout from `WebDriverOptions`), `FailureArtefacts` (scoped) and `ScenarioCorrelation` (scoped).
      Files: `d:\git\QAAutomation\src\QAAutomation.Core\Utilities\ElementWaits.cs`, `...\TestIdLocator.cs`, `...\HeaderValueReader.cs`, `...\PositiveIntegerParser.cs`, `...\FileNameSanitiser.cs`, `...\UrlHostChecker.cs`, `...\ArtefactPaths.cs`, `d:\git\QAAutomation\src\QAAutomation.Core\Support\WebDriverFactory.cs`, `...\ScenarioDriver.cs`, `...\FailureArtefacts.cs`, `...\ScenarioCorrelation.cs`, `d:\git\QAAutomation\src\QAAutomation.Core\CoreRegistration.cs`
      Verify: `dotnet build d:\git\QAAutomation\QAAutomation.sln` → `Build succeeded`, `0 Warning(s)`, `0 Error(s)`. Then temporarily add `using System.Linq;` to any Core file, rebuild, and confirm **exactly one** `LINQ is banned` warning with `Build succeeded` and `0 Error(s)`; delete the line and rebuild back to 0 warnings.

- [ ] 6. Create the API layer in `src/QAAutomation.Api`.
      `Contracts/RenderedText.cs` — record with `Rendered`. `Contracts/WordPressPageResponse.cs` — record with `Id`, `Slug`, `Link`, `Title` (a `RenderedText`). `Contracts/WordPressErrorResponse.cs` — record with `Code`, `Message`. `Contracts/PageListResponse.cs` — record with `HttpStatusCode StatusCode`, `List<WordPressPageResponse> Pages`, `string TotalCountHeader`, `string TotalPagesHeader`, `WordPressErrorResponse Error`. `Contracts/PageLookupResponse.cs` — record with `HttpStatusCode StatusCode`, `WordPressPageResponse Page`, `int MatchCount`, `WordPressErrorResponse Error`. These wrapper records exist so steps never touch `HttpResponseHeaders` or a status-code enum from Refit directly, per the step-definition layer rules.
      `Clients/IWordPressApi.cs` — the Refit interface. `[Get("/pages")] Task<IApiResponse<List<WordPressPageResponse>>> GetPagesAsync([AliasAs("per_page")] int perPage, [AliasAs("_fields")] string fields, CancellationToken cancellationToken)`; an overload taking `[AliasAs("slug")] string slug`; and `[Get("/pages/{pageId}")] Task<IApiResponse<WordPressPageResponse>> GetPageAsync(int pageId, [AliasAs("_fields")] string fields, CancellationToken cancellationToken)`. Always `IApiResponse<T>`, never bare `T`, so negative cases do not throw.
      `Clients/PagesClient.cs` — the wrapper steps inject. `ListPagesAsync(int perPage, CancellationToken)`, `FindPagesBySlugAsync(string slug, CancellationToken)`, `GetPageByIdAsync(int pageId, CancellationToken)`. Reads headers through `HeaderValueReader`, reads the error body by casting `response.Error` to `ApiException` and deserialising `.Content` into `WordPressErrorResponse` with `System.Text.Json`, and returns the Contracts wrapper records. No assertions, no test data, no literal URLs — the base URL comes from `ApiOptions`. A named `const string FieldSelection = "id,slug,title,link"`.
      `Clients/RequestContextHandler.cs` — a `DelegatingHandler` that adds the correlation-id header, with a **clearly commented placeholder block** for the KEYinfinity multi-tenant request-context pattern from `api-automation.md` §3: the comment states that on KEYinfinity the tenant is a required explicit parameter with no ambient default, that this handler is template-only scaffolding, and that WordPress has no tenancy so there is nothing to pass or assert here yet.
      `ApiRegistration.cs` — `AddApiClients(IServiceCollection, ApiOptions)` using `services.AddTransient<RequestContextHandler>()` and `services.AddRefitGeneratedClient<IWordPressApi>().ConfigureHttpClient(...).AddHttpMessageHandler<RequestContextHandler>()`, plus `services.AddScoped<PagesClient>()`. **`AddRefitGeneratedClient`, not `AddRefitClient`** — see the spike table.
      Files: `d:\git\QAAutomation\src\QAAutomation.Api\Contracts\RenderedText.cs`, `...\WordPressPageResponse.cs`, `...\WordPressErrorResponse.cs`, `...\PageListResponse.cs`, `...\PageLookupResponse.cs`, `d:\git\QAAutomation\src\QAAutomation.Api\Clients\IWordPressApi.cs`, `...\PagesClient.cs`, `...\RequestContextHandler.cs`, `d:\git\QAAutomation\src\QAAutomation.Api\ApiRegistration.cs`
      Verify: `dotnet build d:\git\QAAutomation\QAAutomation.sln` → 0 errors, 0 warnings (in particular no Refit `RF006`, which would mean a method cannot generate inline and the generated-client registration would fail at run time).

- [ ] 7. Create the API test project's wiring, features and steps, then run the suite.
      `reqnroll.json`: `{ "language": { "feature": "en-GB" } }`. `AssemblyInfo.cs`: `[assembly: Parallelizable(ParallelScope.Fixtures)]`, `[assembly: LevelOfParallelism(2)]` — read-only GETs, but kept low because the target is a production site. `ApiScenarioDependencies.cs`: a static `[ScenarioDependencies]` factory calling `TestConfigurationLoader.Load()`, `AddTestConfiguration`, `AddApiClients`, and registering `PageApiContext` scoped. **No driver registration and no hooks class** (D6). `PageApiContext.cs`: `int RequestedPageSize`, `PageListResponse ListResponse`, `PageLookupResponse LookupResponse`.
      `Features/PageListing.feature`, feature tags `@api @regression @marketing`, with a coverage-note comment stating the site is live so no exact count is ever asserted:
      ```gherkin
        @smoke
        Scenario: Requesting the page list returns published pages
          When a page list is requested with a page size of 2
          Then the page list request succeeds
          And every returned page carries an identifier, a slug, a title and a link

        Scenario: The page list never returns more pages than the requested page size
          When a page list is requested with a page size of 2
          Then the page list request succeeds
          And no more pages are returned than were requested

        Scenario: The page list reports how many pages exist in total
          When a page list is requested with a page size of 2
          Then the page list request succeeds
          And the pagination headers report positive whole numbers

        # The accepted and rejected partitions are separate tables because their Then
        # differs, so the expected outcome lives in the Then rather than in a column
        # that would be constant for every row.
        Scenario Outline: A page size inside the documented range is accepted
          When a page list is requested with a page size of <pageSize>
          Then the page list request succeeds

          Examples: accepted page size boundaries (boundary value analysis)
            | pageSize |
            | 1        |
            | 100      |

        Scenario Outline: A page size outside the documented range is rejected
          When a page list is requested with a page size of <pageSize>
          Then the page list request is rejected as an invalid parameter

          Examples: rejected page size boundaries (boundary value analysis)
            | pageSize |
            | 0        |
            | 101      |
      ```
      `Features/PageLookup.feature`, feature tags `@api @regression @marketing`:
      ```gherkin
        @smoke
        Scenario: Looking up a published page by its slug returns one fully formed page
          When the page with the slug "technical" is requested
          Then the page lookup succeeds
          And exactly one page is returned
          And the returned page carries an identifier, a slug, a title and a link

        Scenario: Looking up a page identifier that does not exist is refused
          When the page with an identifier that does not exist is requested
          Then the page lookup reports that the page was not found
      ```
      `Steps/PageListingSteps.cs` and `Steps/PageLookupSteps.cs`: `async Task` bindings, constructor-injected `PagesClient` + `PageApiContext`, AwesomeAssertions only, `AssertionScope` to group the pagination-header checks into one concern, a `because` reason on every non-obvious assertion. The pagination-header step reads both headers through `PositiveIntegerParser` and asserts **present and positive**, never `18`, with a comment stating exactly why: `X-WP-Total` changes the moment Landmark publishes a page, and this is the most important lesson in the template. The page-size step asserts `pages.Count <= requestedPageSize`, never an equality. The rejected step asserts status `400` and `Error.Code == "rest_invalid_param"` against a named constant. The missing-id step reads `MissingPageId` from `SiteOptions` and asserts `404`. Loops over returned pages are explicit `foreach` with named locals — no LINQ.
      Files: `d:\git\QAAutomation\tests\QAAutomation.Tests.Api\reqnroll.json`, `...\AssemblyInfo.cs`, `...\ApiScenarioDependencies.cs`, `...\PageApiContext.cs`, `...\Features\PageListing.feature`, `...\Features\PageLookup.feature`, `...\Steps\PageListingSteps.cs`, `...\Steps\PageLookupSteps.cs`
      Verify: `dotnet test d:\git\QAAutomation\tests\QAAutomation.Tests.Api` → all 8 scenarios pass, 0 failed. Then `dotnet test d:\git\QAAutomation\tests\QAAutomation.Tests.Api --filter "TestCategory=smoke"` → exactly 2 pass, proving tag filtering.

- [ ] 8. Create the UI layer in `src/QAAutomation.UI`.
      Every page object opens with the same four-line header comment: this is a third-party WordPress site with no `data-testid`, so locators are the most stable selectors actually available; this is a deliberate documented exception to `ui-automation-selenium.md` §1; on KEYinfinity `data-testid` becomes primary again; any XPath must carry its own justification. Methods are **synchronous**, hold `private static readonly By` fields built from named constants, never cache an `IWebElement`, never assert, and receive `IWebDriver` + `ElementWaits` by constructor injection.
      `Components/PrimaryNavigationComponent.cs` — `private static readonly By HeaderNavigation = By.CssSelector("nav.site-header__navigation")`. `IsVisible()` and `FollowLink(string linkText)`: resolve the first **visible** nav via `ElementWaits.WaitUntilFirstVisible`, then `FindElements(By.CssSelector("a"))` scoped to it and a `foreach` matching trimmed link text, clicking the first `Displayed` match and throwing `NoSuchElementException` with a plain message if none matched. Comment explaining there are two nav copies in the DOM (desktop and mobile) and only one is displayed, so a bare `FindElement` could click a hidden link and throw `ElementNotInteractableException`.
      `Pages/HomePage.cs` — `Open()` navigating to `SiteOptions.BaseUrl` and waiting for the navigation to be visible; `ReadTitle()`; `Navigation` exposing the component; `FollowNavigationLinkTo(string linkText)` returning `LandedPage`.
      `Pages/LandedPage.cs` — a deliberately thin page object for "wherever the nav took us": `ReadTitle()`, `ReadCurrentUrl()`. Keeps the journey readable without a page object per marketing page.
      `Pages/ContactUsPage.cs` — `Open()` navigating to `BaseUrl + ContactPagePath`. A `private static readonly` array of `(string FieldName, By Locator)` pairs for the eight fields, built from named `const` ids (`input_2_5` First Name, `input_2_19` Surname, `input_2_6` Postcode, `input_2_7` Email address, `input_2_8` Telephone, `input_2_4` Company Name, `input_2_9` Mobile, `input_2_10` Your Enquiry) with a comment that Gravity Forms renders deterministic `input_<formId>_<fieldId>` ids and form 2 is the enquiry form. `IsFieldMandatory(string fieldName)` resolves the field with `ElementWaits` and reads `aria-required`. `TypeInto(string fieldName, string value)` and `ReadValueOf(string fieldName)`. **A prominent header comment and NO submit method**: this is a production site, a successful submit creates a real sales enquiry, the form carries `novalidate` so there is no client-side validation to assert, and the absence of a submit method is deliberate so nobody can finish this later (D1).
      `UiRegistration.cs` — `AddPageObjects(IServiceCollection)` registering `PrimaryNavigationComponent`, `HomePage`, `LandedPage`, `ContactUsPage` scoped.
      Files: `d:\git\QAAutomation\src\QAAutomation.UI\Components\PrimaryNavigationComponent.cs`, `d:\git\QAAutomation\src\QAAutomation.UI\Pages\HomePage.cs`, `...\LandedPage.cs`, `...\ContactUsPage.cs`, `d:\git\QAAutomation\src\QAAutomation.UI\UiRegistration.cs`
      Verify: `dotnet build d:\git\QAAutomation\QAAutomation.sln` → 0 errors, 0 warnings.

- [ ] 9. Create the UI test project's wiring, features and steps, then run the suite headless.
      `reqnroll.json` as in item 7. `AssemblyInfo.cs`: `[assembly: Parallelizable(ParallelScope.Fixtures)]`, `[assembly: LevelOfParallelism(2)]` — one browser per worker, two workers, per the brief. `UiScenarioDependencies.cs`: `[ScenarioDependencies]` factory calling `TestConfigurationLoader.Load()`, `AddTestConfiguration`, `AddBrowserSupport`, `AddPageObjects`, plus the two scenario contexts. **No API client registration** — suite separation. `WebDriverHooks.cs`: a `[Binding]` class with `ScenarioDriver`, `FailureArtefacts` and `ScenarioContext` injected; `[BeforeScenario(Order = 10)]` calling `ScenarioDriver.Start()`; `[AfterScenario(Order = 100)]` capturing artefacts inside a `try` when `ScenarioContext.TestError != null` and calling `ScenarioDriver.Quit()` in the `finally`, so a capture failure can never leak a browser process. `SiteNavigationContext.cs` (`PageTitle`, `LandedUrl`, `NavigationVisible`) and `EnquiryFormContext.cs` (`FieldIsMandatory`, `ValueHeldByField`).
      `Features/Homepage.feature`, feature tags `@ui @regression @marketing`, with a comment stating the rotating hero carousel and the animated statistic counters on this page are deliberately never asserted on, and that there are four `<h1>` elements for the same reason:
      ```gherkin
        @smoke
        Scenario: The home page identifies the product in its page title
          Given the visitor opens the Landmark home page
          Then the browser page title is the expected home page title

        Scenario: The home page shows the primary navigation
          Given the visitor opens the Landmark home page
          Then the primary navigation is visible
      ```
      `Features/PrimaryNavigation.feature`, feature tags `@ui @regression @marketing`:
      ```gherkin
        Scenario: The About Us navigation link opens the About Us page
          Given the visitor opens the Landmark home page
          When the visitor follows the "About Us" link in the primary navigation
          Then the browser page title mentions "About Us"
      ```
      `Features/EnquiryForm.feature`, feature tags `@ui @regression @marketing`, opening with the read-only warning block from D1 verbatim in comments:
      ```gherkin
        Scenario Outline: The enquiry form declares which of its fields are mandatory
          Given the visitor opens the Landmark contact page
          Then the "<fieldName>" field is mandatory: <mandatory>

          Examples: mandatory and optional fields (equivalence partitions)
            | fieldName     | mandatory |
            | First Name    | true      |
            | Email address | true      |
            | Company Name  | false     |
            | Your Enquiry  | false     |

        Scenario: The enquiry form holds the details the visitor types without submitting them
          Given the visitor opens the Landmark contact page
          When the visitor types "Automated test enquiry - please ignore" into the "Your Enquiry" field
          Then the "Your Enquiry" field holds "Automated test enquiry - please ignore"
      ```
      `Steps/HomepageSteps.cs`, `Steps/PrimaryNavigationSteps.cs`, `Steps/EnquiryFormSteps.cs`: plain `void` bindings (Selenium is synchronous), constructor injection only, one or two page-object calls then an assertion. The expected home page title comes from `SiteOptions.HomePageTitle`, not a literal in the step. The navigation step uses `ElementWaits.WaitUntilTitleContains` before reading, so the assertion is not racing the page load. The `Given` steps contain no assertions.
      Files: `d:\git\QAAutomation\tests\QAAutomation.Tests.UI\reqnroll.json`, `...\AssemblyInfo.cs`, `...\UiScenarioDependencies.cs`, `...\WebDriverHooks.cs`, `...\SiteNavigationContext.cs`, `...\EnquiryFormContext.cs`, `...\Features\Homepage.feature`, `...\Features\PrimaryNavigation.feature`, `...\Features\EnquiryForm.feature`, `...\Steps\HomepageSteps.cs`, `...\Steps\PrimaryNavigationSteps.cs`, `...\Steps\EnquiryFormSteps.cs`
      Verify: `dotnet test d:\git\QAAutomation\tests\QAAutomation.Tests.UI` → all 8 scenarios pass headless, 0 failed, and no `chromedriver.exe` is left behind (`Get-Process chromedriver -ErrorAction SilentlyContinue` returns nothing). Then temporarily break one expected title, re-run, and confirm `d:\git\QAAutomation\test-results\ui\<scenario>-<timestamp>\` contains `screenshot.png`, `page-source.html`, `console.log` and `location.txt`; restore the title and re-run green.

- [ ] 10. Create the E2E suite — one scenario proving API-seeded UI verification.
      `reqnroll.json` as above. `AssemblyInfo.cs`: **no** `Parallelizable` attribute — E2E runs serially. `E2EScenarioDependencies.cs`: configuration, `AddBrowserSupport`, `AddPageObjects`, `AddApiClients` and `CrossLayerContext` scoped — the only project that registers both layers. `WebDriverHooks.cs`: the same shape as item 9's. This file is duplicated rather than shared because `project-structure.md` forbids a test project referencing another test project, and `code-style.md` prefers DAMP duplication over a premature abstraction — state that in a comment. `CrossLayerContext.cs`: `ApiSlug`, `ApiTitle`, `ApiLink`, `BrowserTitle`.
      `Features/PublishedPageRendersInBrowser.feature`, feature tags `@e2e @smoke @marketing`:
      ```gherkin
        Scenario: A page published through the content API renders with a matching title in the browser
          Given the content API supplies the page with the configured known slug
          When the visitor opens that page in the browser
          Then the browser page title mentions the title the content API reported
      ```
      `Steps/PublishedPageRendersInBrowserSteps.cs`: `async Task` because the `Given` touches HTTP; the `When` makes the synchronous Selenium call plainly on its own line without awaiting it. The `Given` calls `PagesClient.FindPagesBySlugAsync(SiteOptions.KnownPageSlug, ...)` and stores slug, `title.rendered` and `link`. Before navigating, the `When` guards with `UrlHostChecker.HasSameHost(SiteOptions.BaseUrl, context.ApiLink)` and throws a plain-English exception if the API handed back a different host — a URL from a response is untrusted input. The `Then` asserts the browser title **contains** the API title, case-insensitively, with a comment explaining D2: titles are SEO titles, there is no uniform suffix, equality is not assertable.
      Files: `d:\git\QAAutomation\tests\QAAutomation.Tests.E2E\reqnroll.json`, `...\AssemblyInfo.cs`, `...\E2EScenarioDependencies.cs`, `...\WebDriverHooks.cs`, `...\CrossLayerContext.cs`, `...\Features\PublishedPageRendersInBrowser.feature`, `...\Steps\PublishedPageRendersInBrowserSteps.cs`
      Verify: `dotnet test d:\git\QAAutomation\tests\QAAutomation.Tests.E2E` → 1 scenario passes. Then `dotnet test d:\git\QAAutomation\QAAutomation.sln` → all 17 scenarios pass across the three projects, 0 failed, 0 skipped.

- [ ] 11. Write the repository README in basic UK English for a QA engineer new to C# and BDD.
      Required sections, in this order. **What this is** — a template, not a product suite; the SUT is a public placeholder for KEYinfinity. **When to write a UI, an API or an E2E test** — a short decision table, with the rule "prefer the lowest layer that can prove it". **Directory map** — every folder in the tree with one line on what belongs there and one line on where a new test of each kind goes. **Prerequisites** — .NET 10 SDK and Google Chrome installed; state plainly that there is **no browser-driver install step** because Selenium Manager (built into Selenium 4.6+) downloads the matching driver on first run, and that the agent therefore needs outbound network access the first time. **First run** — copy-pasteable PowerShell: `cd d:\git\QAAutomation`, `dotnet restore`, `dotnet build`, `dotnet test`. **Running a subset** — one suite (`dotnet test tests\QAAutomation.Tests.Api`), one feature (`--filter "FullyQualifiedName~PageListing"`), one tag (`--filter "TestCategory=smoke"`), and how the tags map to NUnit categories. **From Gherkin to code** — a numbered trace of one real line (`Then the pagination headers report positive whole numbers`) through the feature file → `PageListingSteps.cs` → `PagesClient` → `IWordPressApi` → `HeaderValueReader`, and a second trace for a UI line through `HomepageSteps.cs` → `HomePage` → `ElementWaits`. **How do I add a new test?** — a numbered recipe: pick the layer, pick or create the `.feature` file, tag it (one layer tag, one suite tag, one feature-area tag), write the Gherkin declaratively, add the binding to the capability's `*Steps.cs`, add the method to the page object or client, register anything new in that project's `*ScenarioDependencies.cs`, run the single test, check the artefacts if it fails. **House rules, each with a one-line reason** — no LINQ (a `foreach` is readable by anyone and breakpointable on any line; the build warns); no `Thread.Sleep` (Selenium does not auto-wait, so every wait goes through the one `ElementWaits` helper and a sleep only moves the race); no secrets in the repository (committed config carries `YOUR_API_KEY_HERE` and real values arrive as `QAAUTOMATION_*` environment variables); assertions live in steps, never in page objects or clients (a page object that asserts cannot be reused by a scenario with a different expectation); `data-testid` becomes the primary locator on KEYinfinity (this template cannot use it because the placeholder site is third-party). **Honest limitations** — the SUT is a public marketing website standing in for KEYinfinity; it is **production and read-only**; the contact form is never submitted and `ContactUsPage` deliberately has no submit method; `X-WP-Total` is live so no count is ever hard-asserted; the hero carousel and animated counters are never asserted on; CI wiring is deferred by the user and there is no `pipelines/` folder; swapping to KEYinfinity means changing `Site:BaseUrl` and `Api:BaseUrl` and restoring `data-testid` as the primary locator. **Where failures are recorded** — `d:\git\QAAutomation\test-results\<suite>\<scenario>-<timestamp>\` with the four artefacts, written on failure only, and the `QAAUTOMATION_Artefacts__Directory` override. **Glossary** — BDD, Gherkin, Given/When/Then, step definition, page object, tag, flaky test, explicit wait, boundary value analysis. Keep every paragraph short; no C# jargon left unexplained on first use.
      Files: `d:\git\QAAutomation\README.md`
      Verify: `dotnet test d:\git\QAAutomation\QAAutomation.sln` still passes, and every PowerShell command quoted in the README runs successfully when pasted into a fresh shell at the repo root.

- [ ] 12. Final integration pass.
      Delete any leftover `Class1.cs`, confirm `d:\git\QAAutomation` contains **no** `pipelines/` folder, confirm no `.csproj` carries a `Version` attribute on a `PackageReference`, confirm `grep` for `FluentAssertions`, `Selenium.WebDriver.ChromeDriver`, `WebDriverManager`, `DotNetSeleniumExtras`, `Thread.Sleep` and `using System.Linq` across `src` and `tests` returns nothing, and confirm no committed file contains a real key, token or connection string.
      Files: none created — cleanup and audit only.
      Verify: `dotnet build d:\git\QAAutomation\QAAutomation.sln` → `0 Warning(s)`, `0 Error(s)`; `dotnet test d:\git\QAAutomation\QAAutomation.sln` → 17 passed, 0 failed, 0 skipped; `Get-Process chromedriver -ErrorAction SilentlyContinue` returns nothing afterwards.

---

## Known gaps and assumptions

1. **The enquiry-form scenario does not assert client-side validation.** The live form has `novalidate`, so there is none. See D1. If Landmark ever adds client-side validation, the scenario can be extended — but only if it can be proved without a POST.
2. **Live third-party content is a standing fragility.** `Site:HomePageTitle`, `Site:KnownPageSlug`, the `About Us` nav link name and the Gravity Forms field ids are all content, not contract. They are concentrated in `appsettings.json` and in one `const` block per page object precisely so a copy change is a one-line edit, and the README says so.
3. **Scenario count is 17** (8 API, 8 UI, 1 E2E) on the design above. If the implementer splits or merges a scenario, update the expected counts in the verification commands rather than leaving them wrong.
4. **Only Chrome is supported.** `WebDriverFactory` throws a configuration exception for any other `WebDriver:Browser` value. Adding Edge or Firefox is a later, deliberate change.
5. **No unit test project.** `project-structure.md`'s tree does not have one, and `Core/Utilities` is exercised through the API and UI suites. Adding `tests/QAAutomation.Tests.Unit` would require updating that steering file first.
