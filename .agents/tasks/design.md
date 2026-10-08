# Technical design — QAAutomation C# BDD template (UI, API, E2E)

## 1. Overview

This design specifies a greenfield C# BDD test automation **template** in `d:\git\QAAutomation`. It is a blueprint, not a product suite: a small number of real, passing scenarios that demonstrate every layer, every convention and every piece of plumbing a QA will later copy. The target framework is `net10.0` (SDK 10.0.301 is installed; 8.0.425 is also present). The runner is Reqnroll 3.3.4 on NUnit 4.6.1, the browser driver is Microsoft.Playwright 1.63.0, the REST client is Refit 16.3.0, assertions are AwesomeAssertions 9.6.0, and all versions are pinned centrally in `Directory.Packages.props`.

Two audiences shape every decision. The first is the maintainer who is **not** an experienced C# developer: no LINQ, explicit types, guard clauses, named constants, one intent per method, and a README that explains the directory map before it explains anything else. The second is CI: three separate test projects so UI, API and E2E fail and retry independently, tag-filtered smoke gates, and evidence capture on failure only.

The system under test is a **documented placeholder**. KEYinfinity is not publicly reachable, so the template runs against Landmark Systems' public marketing website (`https://www.landmarksystems.co.uk/`, WordPress) and its public WordPress REST API. That choice drives three hard constraints baked into this design: the site is **read-only** (nothing is ever submitted), response content is **live** (no count or copy is hard-asserted in code), and locators cannot use `data-testid` because a third-party WordPress site does not have any.

The deliverable of the implementation phase is: a solution that restores, builds and passes `dotnet test` on day one on a clean Windows machine after one Playwright browser install, plus a basic English README that a manual tester can follow.

---

## 2. Verified facts (do not re-guess)

Probed 2026-10-07. Rows marked **(probe)** were verified during this design step and **supersede the assumptions in the task brief** — see §3 for the three design changes they forced.

| # | Fact | Value |
| --- | --- | --- |
| F1 | `robots.txt` | `User-agent: *` / `Disallow:` (empty — crawling permitted) |
| F2 | Homepage `<title>` | `Farm & Rural Estate Management Software \| Landmark Systems` (raw HTML carries `&amp;`; the DOM title Playwright compares has a literal `&`) |
| F3 | Real pages | `/`, `/contact-us/`, `/about-us/`, `/our-team/`, `/support/`, `/technical/`, `/landmark-hub/`, `/landmark-hub/careers/`, `/sitemap/`, `/terms-of-use/`, `/terms-and-conditions/`, `/privacy-policy-2/` |
| F4 | Never assert on | rotating hero carousel, animated odometer stat counters |
| F5 | REST API base | `https://www.landmarksystems.co.uk/wp-json/wp/v2` (live, public, unauthenticated) |
| F6 | `GET /pages?per_page=1&_fields=id` | `200`, 1 item, `X-WP-Total: 18`, `X-WP-TotalPages: 18` |
| F7 | `GET /pages?per_page=100&_fields=id` | `200`, `X-WP-Total: 18`, `X-WP-TotalPages: 1` |
| F8 | `GET /pages?per_page=0` | `400`, `rest_invalid_param` |
| F9 | `GET /pages?per_page=101` | `400`, `rest_invalid_param`, message `per_page must be between 1 (inclusive) and 100 (inclusive)` |
| F10 | `GET /pages/1785?_fields=id,slug` | `200`, slug `technical` |
| F11 | `GET /pages/999999` | `404` |
| F12 | **per_page boundary** | valid `1`–`100` inclusive; invalid at `0` and `101`. The four boundary values are exactly **0, 1, 100, 101** |
| F13 **(probe)** | `/contact-us/` form | Gravity Forms, `<form id='gform_2' ... novalidate>`, AJAX iframe submission (`gform_submission_method=iframe`, `target='gform_ajax_frame_2'`) |
| F14 **(probe)** | Contact form fields | placeholders `First Name*`, `Surname*`, `Postcode*`, `Email address*`, `Telephone*` all with `aria-required="true"`; `Company Name`, `Mobile`, `Your Enquiry` optional (no `aria-required`); all fields carry `aria-invalid="false"` before submission |
| F15 **(probe)** | Page titles are **not** uniform | `/contact-us/` → `Contact Us - Landmark`; `/about-us/` → `About Us - Helping rural business owners make smarter decisions - Landmark`; `/technical/` → `Technical - Landmark minimum recommended hardware requirements \| Landmark Systems`. There is **no** site-wide title suffix |
| F16 **(probe)** | API title ⊂ browser title | `pages?slug=contact-us` → id `43`, `title.rendered` `Contact Us`; `pages/1785` → slug `technical`, `title.rendered` `Technical`; `pages?slug=about-us` → id `12`, `title.rendered` `About Us`. Each `title.rendered` is a case-insensitive substring of the corresponding browser `<title>` |
| F17 **(probe)** | Header nav | **two** `<nav class="site-header__navigation">` elements in the DOM (desktop + mobile copies), so `GetByRole(AriaRole.Navigation)` matches 2 and would trip Playwright strict mode |
| F18 **(probe)** | Nav link names | `About Us` → `/about-us/`, `Support` → `/support/`, `Landmark HUB` → `/landmark-hub/`, `Our Products` → submenu (`href="#empty"`), and the contact link is labelled **`Contact Sales`** → `/contact-us/` (not "Contact Us") |
| F19 | Environment | not a git repository (no `git init`, no commits); `pwsh` (PowerShell 7) is **not installed** — only Windows PowerShell 5.1 |
| F20 | Playwright browsers | not installed; post-build install of **chromium only** is required |

---

## 3. Decisions that deviate from the brief or from steering

Each row states the conflict, the choice and the reason. These are closed decisions, not options.

| # | Topic | Brief / steering says | This design does | Reason |
| --- | --- | --- | --- | --- |
| D1 | Contact form validation | Brief: "fills fields and asserts CLIENT-SIDE validation on an invalid/empty submit, then stops" | **Never clicks submit.** The scenario fills the optional fields with Bogus data and asserts the form's declared mandatory-field contract (`aria-required="true"` on the five starred fields) and that the typed value is accepted | F13: the form carries `novalidate`, so **there is no client-side validation to assert** — the only validation is server-side, reached by an AJAX POST to `/contact-us/`. The brief's own higher-order constraint is READ-ONLY / "do NOT submit the contact form". A submit click is a real write to a production site, so read-only wins. Enforced structurally: `ContactUsPage` has **no submit method at all** (§8.3) |
| D2 | E2E title assertion | Brief: "assert in the browser the corresponding page renders the matching title" | Asserts the browser `<title>` **contains** the API's `title.rendered`, case-insensitively | F15/F16: titles are SEO titles and differ per page; there is no uniform suffix and no equality to assert. "Contains" is verified true for `technical`, `contact-us` and `about-us` |
| D3 | Navigation locator | `GetByRole(AriaRole.Navigation)` scoping | Scopes to `GetByRole(AriaRole.Navigation).Filter(new() { Visible = true }).First`, with a comment naming F17 | F17: two header nav copies exist; unfiltered it is a strict-mode violation, and a bare `.First` can select the hidden mobile copy and then hang on click |
| D4 | `data-testid` locators | `ui-automation-playwright.md` §1 makes `data-testid` priority 1 | Priority 2 (accessible locators: `GetByRole`, `GetByLabel`, `GetByPlaceholder`, `GetByText`), CSS only with a written justification | A third-party WordPress site has no test ids and we cannot add them. **Deliberate, documented exception**, restated at the top of every page object and in the README. On KEYinfinity the `data-testid` contract becomes primary again |
| D5 | `[ScenarioDependencies]` location | `step-definitions.md`: one factory "in `src/QAAutomation.Core/Support`" | One factory **per test project**, at the test project root (`UiScenarioDependencies.cs`, `ApiScenarioDependencies.cs`, `E2EScenarioDependencies.cs`), each composed from shared registration extensions in `src/*` | Reqnroll's DI plugin discovers the attribute in **binding assemblies** (the test projects). A single shared factory would also register the UI browser for the API suite, breaking the suite-separation rule in `project-structure.md`. The *intent* of the steering rule — one factory, never scattered across step classes — is kept |
| D6 | Hook location | `*Hooks.cs` under `Core/Support` | Hook *implementations* (`PlaywrightSession`, `EvidenceRecorder`, `WireMockSession`) are plain classes in `Core/Support`; the thin `[Binding]` hook classes (`UiHooks.cs`, `ApiHooks.cs`, `E2EHooks.cs`) sit at each test project root | Same reason as D5: a `[Binding]` hook in a shared assembly would launch a browser in the API suite. Keeps `Core` free of any runner coupling |
| D7 | Reporting | `tech-stack.md`: Allure.Reqnroll 2.15.0 for reporting | **Deferred.** Version pinned in `Directory.Packages.props` but not referenced. The pass/fail signal is Azure DevOps native results from `.trx`; `README` documents how to switch Allure on | `tech-stack.md` itself names LivingDoc/ADO results as the lighter option and the signal of record. Allure needs `allureConfig.json` plus a report-generation step and is a day-one build risk for a template whose first requirement is "builds and passes" |
| D8 | Nullable reference types | `Directory.Build.props` holds "nullable" | `<Nullable>disable</Nullable>` | `code-style.md`'s own reference snippets assign `null` to non-nullable locals (`VatReturnLine firstVatLine = null;`). A template that warns on its own house style teaches noise to a non-C# maintainer. Upgrade path noted in the README |
| D9 | Implicit usings | not stated | `<ImplicitUsings>disable</ImplicitUsings>` | The SDK's implicit using set **includes `System.Linq`**, which would silently re-enable the single most important banned construct. Explicit usings also make every file self-explanatory for a learner |
| D10 | Scheduled regression | `ci-azure-devops.md` §1: nightly regression schedule | Schedule is declared in YAML but the regression and E2E stages are gated behind a `runRegression` parameter defaulting to **`false`** | While the target is a third-party **production** marketing site, a nightly full-regression run is unacceptable request volume. The gate is one parameter flip once the suite points at a KEYinfinity test environment |
| D11 | Unit tests | no unit test project in the prescribed tree | None added. `Core/Utilities` is proven through the API and UI suites | The tree is to be followed exactly. Adding `tests/QAAutomation.Tests.Unit` requires updating `project-structure.md` first |
| D12 | Ambient tenancy | `api-automation.md` §3: tenant is a **required explicit parameter**, no ambient default | Template ships `TenantContextHandler` (ambient, config-driven) **plus** an explicit `correlationId` parameter on the client wrapper, and a header comment stating the handler is template-only scaffolding to be replaced by an explicit `tenantId` parameter on KEYinfinity | The placeholder SUT has no tenancy, so there is nothing to pass explicitly or assert on. The handler exists so the wiring is real and exercised; the comment prevents it being mistaken for the KEYinfinity pattern |
| D13 | `pwsh` | steering and brief use `pwsh playwright.ps1 install` | README and pipelines give `pwsh` first and `powershell -ExecutionPolicy Bypass -File` as the documented fallback | F19: PowerShell 7 is not installed on this machine |

---

## 4. Technology stack (locked)

Every version below is pinned in a single `Directory.Packages.props` at the repository root. `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` and `<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>` are set there; **no `.csproj` carries a `Version` attribute**, and no floating ranges are permitted.

### 4.1 Pinned versions

| Package | Version | Pinned and referenced? |
| --- | --- | --- |
| `Reqnroll` | 3.3.4 | pinned; transitive via `Reqnroll.NUnit` |
| `Reqnroll.NUnit` | 3.3.4 | referenced by all three test projects |
| `Reqnroll.Microsoft.Extensions.DependencyInjection` | 3.3.4 | referenced by all three test projects |
| `NUnit` | 4.6.1 | referenced by all three test projects (**not** 5.0.0) |
| `NUnit3TestAdapter` | 6.3.0 | referenced by all three test projects |
| `Microsoft.NET.Test.Sdk` | 18.10.1 | referenced by all three test projects |
| `Microsoft.Playwright` | 1.63.0 | referenced by `Core`, `UI`, `Tests.UI`, `Tests.E2E` |
| `AwesomeAssertions` | 9.6.0 | referenced by all three test projects (**never** FluentAssertions 8+) |
| `Refit` | 16.3.0 | referenced by `Api` |
| `Refit.HttpClientFactory` | 16.3.0 | referenced by `Api` (DI registration) |
| `WireMock.Net` | 2.18.0 | referenced by `Core` (stub session) and `Tests.Api` |
| `Bogus` | 35.6.5 | referenced by `Core` |
| `Polly` | 8.8.0 | referenced by `Core` (API settle only; never in UI code) |
| `Microsoft.Extensions.Configuration` | 10.0.3 | referenced by `Core` |
| `Microsoft.Extensions.Configuration.Json` | 10.0.3 | referenced by `Core` |
| `Microsoft.Extensions.Configuration.EnvironmentVariables` | 10.0.3 | referenced by `Core` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.3 | referenced by `Core` |
| `Microsoft.Extensions.Http` | 10.0.3 | referenced by `Api` |
| `Microsoft.CodeAnalysis.BannedApiAnalyzers` | 5.6.0 | referenced by **every** project (`PrivateAssets="all"`) — enforces the no-LINQ rule |
| `Allure.Reqnroll` | 2.15.0 | **pinned only**, not referenced (D7) |
| `Reqnroll.Verify` | 3.3.4 | **pinned only**, not referenced (no snapshot targets in the template) |
| `RestSharp` | 114.0.0 | **pinned only**, not referenced (Refit is the default) |
| `Testcontainers.PostgreSql` | 4.15.0 | **pinned only**, not referenced (no database in the placeholder SUT) |
| `Npgsql` | 10.0.3 | **pinned only**, not referenced |
| `Respawn` | 7.0.0 | **pinned only**, not referenced |

Rule for the implementer: the *pinned only* rows exist so the recorded version is the single source of truth when a future suite needs them. Adding a `PackageReference` for one of them is allowed; adding a package that has **no** `PackageVersion` row is not.

### 4.2 Runner configuration decisions

| Decision | Value | Reason |
| --- | --- | --- |
| Test platform | **VSTest** (the default). No `dotnet.config` `[dotnet.test.runner]` section, no `TestingPlatformDotnetTestSupport` property | `ci-azure-devops.md` selects tests with `--filter "TestCategory=..."` and `--logger trx`. Those are VSTest semantics; opting into Microsoft.Testing.Platform would change the filter and logger contract |
| Tag → category | Reqnroll's NUnit generator emits `[Category("<tag>")]`, so `@smoke` is selected as `TestCategory=smoke` | Already assumed by the pipeline steering |
| UI parallelism | `[assembly: Parallelizable(ParallelScope.Fixtures)]` + `[assembly: LevelOfParallelism(2)]` in `tests/QAAutomation.Tests.UI/AssemblyInfo.cs` | Brief: 1–2 workers against a production site. Feature-level (fixture) parallelism only |
| API parallelism | `ParallelScope.Fixtures`, `LevelOfParallelism(4)` | Read-only GETs; still bounded to keep request volume low |
| E2E parallelism | **none** — no `Parallelizable` attribute | `ci-azure-devops.md` §6: E2E is serial by default |
| Reqnroll config | one `reqnroll.json` per test project: `{ "language": { "feature": "en-GB" } }` | UK English Gherkin; no extra binding assemblies are needed because every `[Binding]` lives in the test project (D5/D6) |

---

## 5. Exact file tree

Create exactly this. Folder names, file names and casing are normative. Files at a project root (rather than in a subfolder) are deliberate — the folder set in `project-structure.md` is unchanged.

```text
QAAutomation/
  .editorconfig
  .gitignore
  BannedSymbols.txt
  Directory.Build.props
  Directory.Packages.props
  QAAutomation.sln
  README.md
  src/
    QAAutomation.Core/
      QAAutomation.Core.csproj
      CoreRegistration.cs
      Configuration/
        appsettings.json
        ApiOptions.cs
        ArtifactOptions.cs
        PlaywrightOptions.cs
        SiteContentOptions.cs
        TestConfigurationLoader.cs
        TestConfigurationException.cs
      Support/
        EvidenceRecorder.cs
        PlaywrightSession.cs
        ScenarioBrowser.cs
        ScenarioCorrelation.cs
        SubmissionStubs.cs
        WireMockSession.cs
      TestData/
        EnquiryDetails.cs
        EnquiryDetailsBuilder.cs
      Utilities/
        ArtifactPaths.cs
        FileNameSanitiser.cs
        HeaderValueReader.cs
        PositiveIntegerParser.cs
        UrlHostChecker.cs
    QAAutomation.UI/
      QAAutomation.UI.csproj
      UiRegistration.cs
      Pages/
        AboutUsPage.cs
        ContactUsPage.cs
        HomePage.cs
      Components/
        PrimaryNavigationComponent.cs
    QAAutomation.Api/
      QAAutomation.Api.csproj
      ApiRegistration.cs
      Clients/
        CorrelationIdHandler.cs
        IWordPressApi.cs
        ISubmissionApi.cs
        PagesClient.cs
        SubmissionClient.cs
        TenantContextHandler.cs
      Contracts/
        RenderedText.cs
        SubmissionRequest.cs
        SubmissionResponse.cs
        WordPressErrorResponse.cs
        WordPressPageResponse.cs
  tests/
    QAAutomation.Tests.UI/
      QAAutomation.Tests.UI.csproj
      AssemblyInfo.cs
      UiHooks.cs
      UiScenarioDependencies.cs
      reqnroll.json
      Features/
        EnquiryForm.feature
        Homepage.feature
        PrimaryNavigation.feature
      Steps/
        EnquiryFormSteps.cs
        HomepageSteps.cs
        PrimaryNavigationSteps.cs
    QAAutomation.Tests.Api/
      QAAutomation.Tests.Api.csproj
      AssemblyInfo.cs
      ApiHooks.cs
      ApiScenarioDependencies.cs
      reqnroll.json
      Features/
        PageLookup.feature
        PageListing.feature
        StubbedSubmission.feature
      Steps/
        PageListingSteps.cs
        PageLookupSteps.cs
        StubbedSubmissionSteps.cs
    QAAutomation.Tests.E2E/
      QAAutomation.Tests.E2E.csproj
      AssemblyInfo.cs
      E2EHooks.cs
      E2EScenarioDependencies.cs
      reqnroll.json
      Features/
        PublishedPageRendersInBrowser.feature
      Steps/
        PublishedPageRendersInBrowserSteps.cs
  pipelines/
    azure-pipelines.yml
    azure-pipelines-api.yml
    azure-pipelines-e2e.yml
    azure-pipelines-ui.yml
```

### 5.1 Project references (one-way only)

| Project | References |
| --- | --- |
| `src/QAAutomation.Core` | packages only — **no** project references, **no** test runner, **no** NUnit |
| `src/QAAutomation.UI` | `Core` |
| `src/QAAutomation.Api` | `Core` |
| `tests/QAAutomation.Tests.UI` | `Core`, `UI` |
| `tests/QAAutomation.Tests.Api` | `Core`, `Api` |
| `tests/QAAutomation.Tests.E2E` | `Core`, `UI`, `Api` |

No test project references another test project. Direction is `tests/* → src/* → Core`, enforced by review (and visible in the `.sln`).

`appsettings.json` lives once, at `src/QAAutomation.Core/Configuration/appsettings.json`, and each test project links it into its own output rather than keeping a copy:

```xml
<ItemGroup>
  <Content Include="..\..\src\QAAutomation.Core\Configuration\appsettings.json"
           Link="appsettings.json"
           CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

---

## 6. Build configuration

### 6.1 `Directory.Build.props`

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>   <!-- D9: the implicit set includes System.Linq -->
    <Nullable>disable</Nullable>               <!-- D8 -->
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <IsPackable>false</IsPackable>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.BannedApiAnalyzers" PrivateAssets="all" />
    <AdditionalFiles Include="$(MSBuildThisFileDirectory)BannedSymbols.txt" />
  </ItemGroup>
</Project>
```

`TreatWarningsAsErrors` is explicitly `false`: the no-LINQ rule must **warn, never break the build** (brief requirement). Test projects set `<IsTestProject>true</IsTestProject>` and `<IsPackable>false</IsPackable>` in their own file.

### 6.2 No-LINQ enforcement — `BannedSymbols.txt` + `.editorconfig`

`.editorconfig` alone cannot warn on a namespace, so enforcement is `Microsoft.CodeAnalysis.BannedApiAnalyzers` (rule `RS0030`) at **warning** severity.

`BannedSymbols.txt` (repository root, supplied to every project as an `AdditionalFiles` item):

```text
T:System.Linq.Enumerable;Do not use LINQ - write a foreach loop. See .kiro/steering/code-style.md
T:System.Linq.Queryable;Do not use LINQ - write a foreach loop. See .kiro/steering/code-style.md
T:System.Threading.Thread;Do not use Thread.Sleep - Playwright auto-waits; Polly handles API polling
```

`.editorconfig` (root, `root = true`) carries:

| Setting | Value | Why |
| --- | --- | --- |
| `dotnet_diagnostic.RS0030.severity` | `warning` | the no-LINQ gate; a warning, so the build still succeeds |
| `dotnet_diagnostic.IDE0005.severity` | `warning` | flags an unnecessary `using System.Linq;` left behind |
| `csharp_style_var_for_built_in_types` / `var_when_type_is_apparent` / `var_elsewhere` | `false:suggestion` | `code-style.md` wants explicit types |
| `dotnet_diagnostic.CA2007.severity` | `none` | `ConfigureAwait` noise is not helpful in test code |
| indentation / newline settings | 4 spaces, CRLF, final newline | house style; `*.feature` files use 2 spaces |

The template itself contains **zero** LINQ, so a clean build produces zero `RS0030` warnings. If a future boundary genuinely needs `.ToList()` (exception 3 in `code-style.md`), the recipe — documented in the README — is one line of `#pragma warning disable RS0030` with a justification comment and an immediate `restore`, never a change to the severity.

### 6.3 `.gitignore`

Not a git repository today (F19), but the file is part of the template: `bin/`, `obj/`, `test-results/`, `TestResults/`, `*.trx`, `playwright-report/`, `*.user`, `.vs/`, `allure-results/`, and `storage-state*.json` (session state is a build artefact and must never be committed).

---
## 7. Core plumbing (`src/QAAutomation.Core`)

### 7.1 Configuration

`appsettings.json` is committed with **placeholders only** for anything secret-shaped. Base URLs for a public site are not secrets and are committed as real values.

```json
{
  "Playwright": {
    "BaseUrl": "https://www.landmarksystems.co.uk",
    "Headless": true,
    "ViewportWidth": 1920,
    "ViewportHeight": 1080,
    "DefaultTimeoutMilliseconds": 15000,
    "NavigationTimeoutMilliseconds": 30000
  },
  "Api": {
    "BaseUrl": "https://www.landmarksystems.co.uk/wp-json/wp/v2",
    "TimeoutSeconds": 30,
    "TenantId": "YOUR_TENANT_ID_HERE",
    "ApiKey": "YOUR_API_KEY_HERE"
  },
  "SiteContent": {
    "HomePageTitle": "Farm & Rural Estate Management Software | Landmark Systems",
    "PrimaryNavigationLinkName": "About Us",
    "PrimaryNavigationTargetPath": "/about-us/",
    "PrimaryNavigationExpectedTitleFragment": "About Us",
    "CrossLayerPageId": 1785,
    "MaximumPageSize": 100,
    "MinimumPageSize": 1
  },
  "Artifacts": {
    "Directory": "test-results"
  }
}
```

Why `SiteContent` exists: every value in it is **live third-party content**. Keeping them in configuration means a copy change on the marketing site is a one-line config edit, not a code change — and it makes the anti-flake rule visible rather than tribal. `ApiKey` and `TenantId` are unused against WordPress; they are present to demonstrate the placeholder-plus-environment-override pattern.

Binding, in `Configuration/TestConfigurationLoader.cs`:

1. `AddJsonFile("appsettings.json", optional: false)`
2. `AddJsonFile($"appsettings.{environment}.json", optional: true)` where `environment` comes from `QAAUTOMATION_ENVIRONMENT` (default `local`)
3. `AddEnvironmentVariables(prefix: "QAAUTOMATION_")` — so a CI override is `QAAUTOMATION_Api__BaseUrl` (double underscore for nesting). PowerShell: `$env:QAAUTOMATION_Api__BaseUrl = "https://test.example/wp-json/wp/v2"`

Options classes are plain classes with settable properties and are registered **as concrete singletons**, not as `IOptions<T>`:

```csharp
services.AddSingleton(playwrightOptions);   // ctor reads: PlaywrightOptions options
```

A constructor parameter of `PlaywrightOptions` is readable to a newcomer; `IOptions<PlaywrightOptions>.Value` is one more concept to learn for no benefit in a test process.

### 7.2 Configuration validation

Validation is explicit guard code in `TestConfigurationLoader`, not DataAnnotations (no `IHost`, so `ValidateOnStart` is unavailable). Every failure throws `TestConfigurationException` naming the **exact configuration key** and the offending value.

| Key | Rule | On failure |
| --- | --- | --- |
| `Playwright:BaseUrl` | required; absolute URI; scheme must be `https` | `TestConfigurationException`: `"Playwright:BaseUrl must be an absolute https URL. Found: '<value>'."` |
| `Playwright:ViewportWidth` / `ViewportHeight` | required; `>= 320` / `>= 480` | as above, naming the key and the minimum |
| `Playwright:DefaultTimeoutMilliseconds` | required; `1000`–`120000` | as above |
| `Playwright:NavigationTimeoutMilliseconds` | required; `1000`–`120000` | as above |
| `Playwright:Headless` | optional; defaults to `true` | n/a |
| `Api:BaseUrl` | required; absolute URI; scheme must be `https` | as above |
| `Api:TimeoutSeconds` | required; `1`–`300` | as above |
| `Api:TenantId` | optional; may remain the placeholder | n/a — it is only used as a header value |
| `SiteContent:HomePageTitle` | required; non-empty after trim | as above |
| `SiteContent:PrimaryNavigationLinkName` | required; non-empty | as above |
| `SiteContent:PrimaryNavigationTargetPath` | required; must start with `/` | as above |
| `SiteContent:CrossLayerPageId` | required; `> 0` | as above |
| `SiteContent:MinimumPageSize` / `MaximumPageSize` | required; `1 <= min <= max <= 100` | as above |
| `Artifacts:Directory` | optional; defaults to `test-results`; must not be rooted outside the test output unless overridden by env var | as above |

Failure class: **fatal, not recoverable**. It is thrown from `[BeforeTestRun]`, so the run stops before any scenario executes and the message identifies the key. Nothing is logged — the exception message *is* the diagnostic, and configuration objects are never dumped (they can carry an injected key).

### 7.3 Playwright lifecycle

| Scope | Type | Lifetime | Owner |
| --- | --- | --- | --- |
| `IPlaywright`, `IBrowser` | `PlaywrightSession` | **once per run**, chromium only | `[BeforeTestRun]` / `[AfterTestRun]` in `UiHooks` / `E2EHooks` |
| `IBrowserContext`, `IPage` | `ScenarioBrowser` | **fresh per scenario** | `[BeforeScenario]` / `[AfterScenario]` |
| Page objects, clients, contexts | — | scoped per scenario | `[ScenarioDependencies]` |

`PlaywrightSession` is created in `[BeforeTestRun]` and held in a single `internal static` property, `PlaywrightSession.Current`. This is the one permitted piece of static state in the repository, for a structural reason: Reqnroll's `[BeforeTestRun]` is static by contract, so run-scoped resources have nowhere else to live. It is documented in the file header, set exactly once, and cleared in `[AfterTestRun]`. `IBrowser` is thread-safe for `NewContextAsync`, so feature-level parallelism is safe.

`ScenarioBrowser` (in `Core/Support`) wraps the per-scenario context and page:

```csharp
public sealed class ScenarioBrowser
{
    private readonly PlaywrightOptions _options;
    private IBrowserContext _context;
    private IPage _page;

    public ScenarioBrowser(PlaywrightOptions options)
    {
        _options = options;
    }

    public IPage Page
    {
        get
        {
            if (_page == null)
            {
                throw new InvalidOperationException(
                    "The browser page has not been started. A UI scenario must run with UiHooks registered.");
            }

            return _page;
        }
    }

    public async Task StartAsync(IBrowser browser)
    {
        _context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize
            {
                Width = _options.ViewportWidth,
                Height = _options.ViewportHeight
            }
        });

        _context.SetDefaultTimeout(_options.DefaultTimeoutMilliseconds);
        _context.SetDefaultNavigationTimeout(_options.NavigationTimeoutMilliseconds);
        _page = await _context.NewPageAsync();
    }
}
```

`IPage` reaches page objects by constructor injection, exactly as `ui-automation-playwright.md` requires, through a factory registration that runs **after** `[BeforeScenario]` because step classes (and therefore page objects) are constructed on first step execution:

```csharp
services.AddScoped<ScenarioBrowser>();
services.AddScoped<IPage>(provider => provider.GetRequiredService<ScenarioBrowser>().Page);
```

The desktop viewport (1920×1080) is a deliberate anti-flake choice: F17's mobile nav copy stays hidden, so the visible-nav filter in D3 is deterministic.

Disposal order in `[AfterScenario]`, with no early return on failure: capture evidence → stop tracing → `_page` is disposed with the context → `await context.DisposeAsync()`. Tracing is started in `[BeforeScenario]` immediately after `StartAsync`.

### 7.4 Evidence capture — failure only

`EvidenceRecorder` (in `Core/Support`) owns the decision and the paths. `ArtifactPaths.ForScenario(...)` builds `<artifacts root>/<suite>/<sanitised scenario title>-<yyyyMMdd-HHmmss>/`, where the artifacts root is `AppContext.BaseDirectory` + `Artifacts:Directory` (so, by default, `tests/<project>/bin/Debug/net10.0/test-results/`) and can be redirected in CI with `QAAUTOMATION_Artifacts__Directory`.

| Scenario outcome | Trace | Screenshot | Written where |
| --- | --- | --- | --- |
| passed | `StopAsync()` with no path — discarded | none | — |
| failed (`ScenarioContext.TestError != null`) | `StopAsync(new() { Path = ".../trace.zip" })` | full-page PNG, `.../screenshot.png` | the per-scenario folder above |

Video is **not** enabled: it needs context-level opt-in before the scenario runs, which means recording every scenario and discarding most. Trace already carries screenshots, snapshots and sources, which is the better signal per megabyte. The README says how to turn video on if a specific defect needs it.

`FileNameSanitiser` replaces anything outside `[A-Za-z0-9-_]` with `-` using an explicit `foreach` over `char`s (no regex, no LINQ). Artefacts carry no request bodies, no headers and no configuration dumps.

### 7.5 Test data — Bogus, no PII

`EnquiryDetails` is a record with `FirstName`, `Surname`, `CompanyName`, `EmailAddress`, `TelephoneNumber`, `Postcode`, `Enquiry`. `EnquiryDetailsBuilder` uses a `Faker` with the `en_GB` locale and a **fixed seed by default** (`new Randomizer(20261007)`) so a failure is reproducible, with `WithSeed(int)` for variation.

Two non-negotiables, stated as comments in the builder:

1. Email addresses are generated on `example.com` (`faker.Internet.Email(provider: "example.com")`). An RFC-2606 reserved domain cannot reach a real mailbox even if a future change ever did submit something.
2. The enquiry text is a fixed, obviously-synthetic sentence identifying it as automated test data, never Lorem Ipsum that reads like a real enquiry.

The builder is a fluent `WithX` chain returning `this`, with a final `Build()`. No LINQ, no reflection, no randomness outside the seeded `Faker`.

### 7.6 Utilities

| Utility | Signature | Notes |
| --- | --- | --- |
| `HeaderValueReader` | `static string ReadFirstValue(HttpResponseHeaders headers, string name)` | `TryGetValues` then a `foreach` with `break`; returns `null` when absent. No LINQ |
| `PositiveIntegerParser` | `static bool TryParsePositive(string value, out int result)` | `int.TryParse` with `NumberStyles.Integer` + `CultureInfo.InvariantCulture`, then `> 0` |
| `FileNameSanitiser` | `static string ToSafeFileName(string value)` | `foreach` over chars; caps length at 80 |
| `UrlHostChecker` | `static bool HasSameHost(string expectedBaseUrl, string candidateUrl)` | guards the E2E hand-off (§10.3); case-insensitive host compare |
| `ArtifactPaths` | `static string ForScenario(string root, string suite, string scenarioTitle)` | creates the directory; see §11 for the failure path |
| `ScenarioCorrelation` | scoped class exposing `string CorrelationId` | one GUID per scenario, sent on every API request and included in artefact folder names so UI and API evidence line up |

---
<!-- SECTION-MARKER -->
