# Phase B report — C# BDD Selenium automation template

The template is finished and verified on this machine. `dotnet build QAAutomation.sln` reports **Build succeeded, 0 Warning(s), 0 Error(s)**. `dotnet test QAAutomation.sln` reports **18 passed, 0 failed, 0 skipped** — 9 API, 8 UI, 1 E2E. `Get-Process chromedriver` afterwards returns nothing, and `d:\git\QAAutomation\test-results` does not exist, which is the expected state after a green run. No scratch or temporary file was left behind. The workspace is not a git repository, so nothing was committed; the changes are on disk.

## File tree

```text
d:\git\QAAutomation\
  .editorconfig
  .gitignore
  Directory.Build.props              # net10.0, ImplicitUsings=disable, Nullable=disable, EnforceCodeStyleInBuild
  Directory.Build.targets            # WarnOnLinqUsage - findstr for "using System.Linq", warning only, Windows-gated
  Directory.Packages.props           # the single pin list (16 packages)
  QAAutomation.sln
  README.md                          # 12 sections, UK English, written for a QA who is new to C#
  src\
    QAAutomation.Core\               # no ProjectReference, no test-runner reference
      Configuration\                 # appsettings.json, WebDriverOptions, ApiOptions, SiteOptions,
                                     # ArtefactOptions, TestConfiguration, TestConfigurationLoader,
                                     # TestConfigurationException
      Support\                       # WebDriverFactory, ScenarioDriver, FailureArtefacts, ScenarioCorrelation
      Utilities\                     # ElementWaits, TestIdLocator, HeaderValueReader, PositiveIntegerParser,
                                     # FileNameSanitiser, UrlHostChecker, ArtefactPaths
      CoreRegistration.cs
      QAAutomation.Core.csproj
    QAAutomation.UI\                 # references Core
      Pages\                         # HomePage, LandedPage, ContactUsPage
      Components\                    # PrimaryNavigationComponent, SubscribeModalComponent
      UiRegistration.cs
      QAAutomation.UI.csproj
    QAAutomation.Api\                # references Core
      Clients\                       # IWordPressApi, PagesClient, RequestContextHandler
      Contracts\                     # RenderedText, WordPressPageResponse, WordPressErrorResponse,
                                     # PageListResponse, PageLookupResponse
      ApiRegistration.cs
      QAAutomation.Api.csproj
  tests\
    QAAutomation.Tests.UI\
      Features\                      # Homepage.feature, PrimaryNavigation.feature, EnquiryForm.feature
      Steps\                         # HomepageSteps, PrimaryNavigationSteps, EnquiryFormSteps
      UiScenarioDependencies.cs      # the [ScenarioDependencies] factory
      WebDriverHooks.cs
      SiteNavigationContext.cs, EnquiryFormContext.cs
      ContainerStartup.cs, AssemblyInfo.cs, reqnroll.json
      QAAutomation.Tests.UI.csproj
    QAAutomation.Tests.Api\
      Features\                      # PageListing.feature, PageLookup.feature
      Steps\                         # PageListingSteps, PageLookupSteps
      ApiScenarioDependencies.cs
      PageApiContext.cs
      ContainerStartup.cs, AssemblyInfo.cs, reqnroll.json
      QAAutomation.Tests.Api.csproj  # no hooks class, no driver registration
    QAAutomation.Tests.E2E\
      Features\                      # PublishedPageRendersInBrowser.feature
      Steps\                         # PublishedPageRendersInBrowserSteps
      E2EScenarioDependencies.cs
      WebDriverHooks.cs
      CrossLayerContext.cs
      ContainerStartup.cs, AssemblyInfo.cs, reqnroll.json
      QAAutomation.Tests.E2E.csproj  # the only project referencing both UI and Api
```

There is deliberately **no `pipelines\` folder** (CI is parked) and **no `Core\TestData\`** (nothing creates data yet, so `Bogus` stays reserved). `test-results\` is runtime output only and is gitignored. Reqnroll writes a `*.feature.cs` beside each `.feature` at build time; those are generated, gitignored, excluded from the no-LINQ grep, and marked `generated_code = true` in `.editorconfig`.

Dependency direction holds one way: `tests\*` → `src\*` → `Core`. `Core` carries no `ProjectReference`, and no test project references another test project.

## Pinned package set

All 16 live in one `Directory.Packages.props` with `ManagePackageVersionsCentrally` and `CentralPackageTransitivePinningEnabled` both true. Every `PackageReference` across the six `.csproj` files is written with no `Version` attribute.

| Package | Version |
| --- | --- |
| `Reqnroll`, `Reqnroll.NUnit`, `Reqnroll.Microsoft.Extensions.DependencyInjection` | 3.3.4 |
| `NUnit` | 4.6.1 |
| `NUnit3TestAdapter` | 6.3.0 |
| `Microsoft.NET.Test.Sdk` | 18.10.1 |
| `Selenium.WebDriver`, `Selenium.Support` | 4.50.0 |
| `AwesomeAssertions` | 9.6.0 |
| `Refit`, `Refit.HttpClientFactory` | 16.3.0 |
| `Microsoft.Extensions.Configuration`, `.Json`, `.EnvironmentVariables` | 10.0.12 |
| `Microsoft.Extensions.Http` | 10.0.12 |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.12 |

Not present, by decision recorded in `tech-stack.md`: `FluentAssertions` (v8+ is a commercial-licence liability), `Selenium.WebDriver.ChromeDriver` and `WebDriverManager` (Selenium Manager resolves the driver), `DotNetSeleniumExtras.WaitHelpers` (abandoned — `ElementWaits` replaces it). NUnit 5.0.0 is deferred until Reqnroll confirms support.

## DI, hooks and driver lifecycle

**DI.** Each test project has its own static `[ScenarioDependencies]` factory at its root, because Reqnroll only discovers that attribute inside a binding assembly and each suite needs a different service graph. The factory calls `TestConfigurationLoader.Load()`, then composes registrations exposed by the `src` projects: `AddTestConfiguration`, `AddBrowserSupport`, `AddPageObjects`, `AddApiClients`. Options classes are registered as concrete singletons (`AddSingleton(webDriverOptions)`), never `IOptions<T>`, so a step's constructor reads as plain English. Scenario contexts are scoped and also live at the owning project's root, because they hold that suite's own response and page types.

Suite separation is structural rather than conditional: `Tests.Api` registers no browser and has no hooks class, so an API scenario cannot start Chrome even by accident; `Tests.UI` registers no API client, so a UI scenario cannot make an HTTP call; `Tests.E2E` is the only project that registers both.

**Driver lifecycle.** One `IWebDriver` per scenario. `WebDriverHooks.StartBrowser` runs at `[BeforeScenario(Order = 10)]` and calls `ScenarioDriver.Start()`. `CaptureEvidenceThenQuitBrowser` runs at `[AfterScenario(Order = 100)]`: inside a `try`, if `ScenarioContext.TestError != null` it calls `FailureArtefacts.Capture`, and `ScenarioDriver.Quit()` sits in the `finally`, so a capture that throws can never leak a Chrome or chromedriver process. `Quit()` is `Quit()` then `Dispose()`, never `Close()`.

Page objects receive the live driver through `services.AddScoped<IWebDriver>(provider => provider.GetRequiredService<ScenarioDriver>().Driver)`, which resolves lazily at the first step — after the hook has started the browser. Asking for `Driver` before `Start()` throws a message that names the wiring to fix rather than a null reference.

**Waits.** Implicit wait is pinned at `TimeSpan.Zero`. `ElementWaits` in `Core\Utilities` is the only file in the repository holding a `WebDriverWait` or an `Until` lambda, which is how `code-style.md` exception 2 stays bounded to one place. There is no `Thread.Sleep`, no `Task.Delay` and no hand-rolled polling loop anywhere.

**Failure artefacts.** On failure, `FailureArtefacts.Capture` writes `screenshot.png`, `page-source.html`, `console.log` and `location.txt` to `test-results\<suite>\<scenario>-<timestamp>\`, each write guarded on its own so one failure does not suppress the rest, with any write problem recorded in `capture-problems.txt`. Nothing is echoed to the test output beyond the folder path, because page source and console logs can carry tokens and personal data.

**API wiring.** Refit is registered with `services.AddRefitGeneratedClient<T>()` — `AddRefitClient<T>()` compiles but fails at run time demanding the unpinned `Refit.Reflection` package. Methods return `IApiResponse<T>` rather than bare `T`, so 400 and 404 are inspected instead of thrown. `RequestContextHandler` is a `DelegatingHandler` adding a per-scenario correlation id.

## Gherkin scenarios

18 scenarios across six feature files. Every feature carries its suite tag plus a depth tag, so `--filter "TestCategory=smoke"` works at any level.

**`Tests.Api\Features\PageListing.feature`** — `@api @regression @marketing`

| Scenario | Extra tags |
| --- | --- |
| Requesting the page list returns published pages | `@smoke` |
| The page list never returns more pages than the requested page size | — |
| The page list reports how many pages exist in total | — |
| A page size inside the documented range is accepted (Outline: 1, 100) | — |
| A page size outside the documented range is rejected (Outline: 0, 101) | — |

**`Tests.Api\Features\PageLookup.feature`** — `@api @regression @marketing`

| Scenario | Extra tags |
| --- | --- |
| Looking up a published page by its slug returns one fully formed page | `@smoke` |
| Looking up a page identifier that does not exist is refused | — |

**`Tests.UI\Features\Homepage.feature`** — `@ui @regression @marketing`

| Scenario | Extra tags |
| --- | --- |
| The home page identifies the product in its page title | `@smoke` |
| The home page shows the primary navigation | — |

**`Tests.UI\Features\PrimaryNavigation.feature`** — `@ui @regression @marketing`

| Scenario | Extra tags |
| --- | --- |
| The About Us navigation link opens the About Us page | — |

**`Tests.UI\Features\EnquiryForm.feature`** — `@ui @regression @marketing`

| Scenario | Extra tags |
| --- | --- |
| The enquiry form declares which of its fields are mandatory (Outline: First Name, Email address, Company Name, Your Enquiry) | — |
| The enquiry form holds the details the visitor types without submitting them | — |

**`Tests.E2E\Features\PublishedPageRendersInBrowser.feature`** — `@e2e @smoke @marketing`

| Scenario | Extra tags |
| --- | --- |
| A page published through the content API renders with a matching title in the browser | — |

The two outlines use boundary value analysis (`per_page` valid 1–100 inclusive, invalid at 0 and 101) and equivalence partitioning (mandatory versus optional fields), which is the coverage derivation `test-design-techniques.md` asks for.

## The three deliberate documented exceptions

**1. No `data-testid` on a third-party site.** `ui-automation-selenium.md` §1 makes `data-testid` the first-choice locator. Landmark Systems' site is a third-party WordPress install: it has no test ids and we cannot add any, because we do not own the markup. So the page objects use the most stable selectors that actually exist — the header's own class name `nav.site-header__navigation` scoped to the first *visible* match (two copies exist, desktop and mobile, and a bare `FindElement` can click a hidden link), Gravity Forms' deterministic `input_2_*` field ids, and href-based navigation links. The exception and its reasoning are written at the top of every page object and component (`HomePage`, `LandedPage`, `ContactUsPage`, `PrimaryNavigationComponent`, `SubscribeModalComponent`) and in the README. `TestIdLocator` ships unused in `Core\Utilities` with a comment saying why not to delete it: on KEYinfinity, where we own the front end, `data-testid` becomes primary again on day one.

**2. The contact form is never submitted.** This is a live, customer-facing site, and a submitted enquiry reaches a real inbox and a real sales team. `ContactUsPage` therefore has no submit method and contains no `Click` call at all — the prohibition is enforced by the absence of the capability, not by a convention someone can forget. The reason is written at the top of both `ContactUsPage` and `EnquiryForm.feature` in capitals, so nobody "finishes" the page object later. The form's validation is only testable server-side anyway: the form carries `novalidate`, so there is no client-side validation to assert on. What the suite does instead is read field metadata (`aria-required`) and confirm typed values are held in the inputs.

**3. The browser title is a *contains*, never an *equals*.** The E2E scenario hands the API's `title.rendered` to the browser and asserts containment, because the site's titles are Yoast SEO titles with no uniform suffix. `/technical/` returns `title.rendered` of `Technical` from the API while the DOM title is `Technical - Landmark minimum recommended hardware requirements | Landmark Systems`. The API title is a case-insensitive substring of the browser title and will never equal it. The step uses `WaitUntilTitleContains` (compares `OrdinalIgnoreCase`) paired with `ContainEquivalentOf`, so the wait and the assertion cannot disagree, and the step carries a comment explaining that containment *is* the contract rather than a weakened version of one.

A fourth rule is worth naming beside those three because it is the most transferable lesson in the template: **no live content count is ever hard-asserted.** `X-WP-Total` was 18 at the time of writing and changes the moment Landmark publishes a page. The API steps assert that the header exists, parses to a positive integer, and that the returned item count is less than or equal to the requested `per_page`. Nothing asserts on the rotating hero carousel, the animated odometer counters, or any `h1` (there are four, one per carousel slide) — and `HomePage` exposes no heading reader, so a future author cannot start.

## How to run the suites

```powershell
dotnet build d:\git\QAAutomation\QAAutomation.sln

# everything (18 scenarios)
dotnet test d:\git\QAAutomation\QAAutomation.sln

# one suite at a time
dotnet test d:\git\QAAutomation\tests\QAAutomation.Tests.Api
dotnet test d:\git\QAAutomation\tests\QAAutomation.Tests.UI
dotnet test d:\git\QAAutomation\tests\QAAutomation.Tests.E2E

# by depth - Reqnroll emits an NUnit [Category] per Gherkin tag
dotnet test d:\git\QAAutomation\tests\QAAutomation.Tests.Api --filter "TestCategory=smoke"

# after any UI run, this must print nothing
Get-Process chromedriver -ErrorAction SilentlyContinue
```

Prerequisites: .NET SDK 10 and Google Chrome. There is no driver install step — Selenium Manager, built into Selenium 4.6+, resolves and downloads the matching chromedriver on first run, which needs network access once. Chrome runs headless by default; override through `QAAUTOMATION_WebDriver__Headless=false` to watch a scenario. All configuration overrides use the `QAAUTOMATION_` prefix with `__` for nesting, and committed `appsettings.json` holds `YOUR_API_KEY_HERE`-style placeholders only.

Running a single scenario by name needs the generated fixture name, not the Gherkin text — the README lists all six fixture names for `--filter FullyQualifiedName~`, because this is the first thing that trips a newcomer.

## Gaps and assumptions a reader should know

**The system under test is third-party production.** Every request is a read-only GET, request volume is deliberately low, and the suite must stay that way. If Landmark restructures the site — renames a nav link, changes a Gravity Forms field id, retires `/technical/` — scenarios go red for a reason that is not a product defect. That is inherent to using someone else's site as a placeholder and disappears when the suite is repointed at KEYinfinity.

**Chrome only.** Any other value for `WebDriver:Browser` fails with a configuration exception that names the setting. Adding Firefox or Edge is a `WebDriverFactory` change plus a pin decision; it was left out to keep the template readable.

**`Site:KnownPageSlug` has an undocumented constraint.** The E2E title rule compares `title.rendered` against the DOM title, and the two differ on HTML entities — the home page's API title carries `&amp;` where the DOM title carries a literal `&`. Repointing the slug at a page whose title contains an entity surfaces as a twenty-second `WaitUntilTitleContains` timeout with nothing to say why. The configured slug (`technical`) is safe. Recorded in the third review pass as non-blocking; the fix is one sentence in the step's slug comment.

**Two more non-blocking notes carried forward from the third review pass.** `PrimaryNavigationSteps` waits case-insensitively and then asserts case-sensitively with `Should().Contain`, so a casing change on the site ("About Us" → "About us") would fail with a message about casing rather than about navigation; `ContainEquivalentOf`, as the E2E step uses, closes it. And `FailureArtefacts.Capture` calls `ArtefactPaths.ForScenario` outside the per-write guards, so a bad `QAAUTOMATION_Artefacts__Directory` override would report as an `AfterScenario` error beside the scenario's real failure. The driver still quits in either case, so nothing leaks. All three were left as-is because they change observable behaviour or wording and the reviewer marked them non-required; they are listed here so the decision is visible rather than lost.

**The no-LINQ guard is Windows-only.** `Directory.Build.targets` uses `findstr` and is gated on `Windows_NT`, so on a Linux or macOS agent the rule falls back to code review with no warning to say so. The target's own comment names the real fix: take `Microsoft.CodeAnalysis.BannedApiAnalyzers`, pinned in `Directory.Packages.props` first. Do not solve it by dropping the OS condition — that turns a missing guard into a silent pass that looks like enforcement.

**Not covered, because the placeholder target has none of it:** authentication, multi-tenant isolation, third-party stubbing, database setup and reset, generated test data, and the artefact-masking pass. Each has a reserved package and a documented trigger in `tech-stack.md`, and each comes back the day the thing it exists for exists.

**CI is parked at the user's request.** No `pipelines\` folder and no YAML. The conventions are already written in `.kiro\steering\ci-azure-devops.md` and the `wire-suite-into-azure-pipeline` skill, both marked deferred, so they cost nothing while parked.

**Not a git repository.** `d:\git\QAAutomation` has no `.git`, so nothing was committed. Initialise and commit before anyone else relies on this.
