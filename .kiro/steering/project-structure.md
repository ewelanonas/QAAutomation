---
inclusion: always
---

# Project structure and naming

Three steering files are always-on: this one, #[[file:tech-stack.md]] and #[[file:code-style.md]]. Everything else (UI, API, E2E, DI, hooks, CI, reporting guidance) is conditional — loaded by `fileMatchPattern` or pulled in manually — to keep the always-injected context budget small. All three always-on files are deliberately short.

The layout below is a proposal for a greenfield framework; it is inferred from the stated UI + API + E2E scope, not quoted from an existing repository.

## Solution layout

```text
QAAutomation/
  Directory.Packages.props           # central pinned versions (see tech-stack.md)
  Directory.Build.props              # shared TFM, nullable, analysers
  QAAutomation.sln
  src/
    QAAutomation.Core/               # no test runner reference
      Configuration/                 # typed options, env binding
      Support/                       # driver lifecycle, failure artefacts, shared DI registration
      Utilities/                     # shared helpers (no domain logic) — incl. the Selenium wait helper
    QAAutomation.UI/
      Pages/                         # page objects
      Components/                    # reusable page fragments
    QAAutomation.Api/
      Clients/                       # Refit interfaces + wrappers
      Contracts/                     # request/response records
  tests/
    QAAutomation.Tests.UI/
      Features/
      Steps/
      *ScenarioDependencies.cs       # the [ScenarioDependencies] factory for this suite
      *Hooks.cs                      # [Binding] hooks for this suite
      *Context.cs                    # typed scenario contexts shared by this suite's steps
    QAAutomation.Tests.Api/
      Features/
      Steps/
    QAAutomation.Tests.E2E/
      Features/
      Steps/
  test-results/                      # failure artefacts (gitignored, never committed)
```

**Why the DI factory, the hooks and the contexts sit at each test project's root rather than in `Core/Support`:** Reqnroll only discovers `[ScenarioDependencies]` and `[Binding]` inside a binding assembly, each suite needs a different service graph (the API suite must register no browser), and a context holds that suite's own response and page types, so moving it to `Core` would invert the one-way dependency direction. The work those files do still lives in `src/*`; the files themselves are thin shells. Full reasoning: #[[file:step-definitions.md]].

**`pipelines/` is deferred.** CI wiring is parked by the user, so no pipeline files exist yet. When it comes back, the folder is `pipelines/` with one orchestrator plus one file per suite (`azure-pipelines.yml`, `azure-pipelines-ui.yml`, `azure-pipelines-api.yml`, `azure-pipelines-e2e.yml`) — the conventions are already written in #[[file:ci-azure-devops.md]].

**`src/QAAutomation.Core/TestData/` is deferred.** No test data builder exists yet: the current system under test is a read-only public website, every request is a GET, and no scenario creates data, so `Bogus` is still reserved in #[[file:tech-stack.md]]. An empty folder would be noise, so the folder arrives with the first `*Builder.cs` — which is also when `Bogus` is pinned. The `**/TestData/**/*.cs` glob below is kept, so the conventions apply the day it appears.

## Layer rules

| Layer | Project | May reference | Must not contain |
| --- | --- | --- | --- |
| Feature files | `tests/*/Features` | — | implementation detail, selectors, URLs |
| Step definitions | `tests/*/Steps` | Core, UI, Api | selectors, raw HTTP, SQL |
| Page objects | `src/QAAutomation.UI/Pages` | Core | assertions, Gherkin wording |
| API clients | `src/QAAutomation.Api/Clients` | Core, Contracts | assertions, test data generation |
| Domain / support | `src/QAAutomation.Core` | — | references to test projects |
| Shared utilities | `src/QAAutomation.Core/Utilities` | — | domain or page knowledge |

Dependency direction is one-way: `tests/*` → `src/*` → `Core`. No test project references another test project.

## Suite separation

1. Three test projects — UI, API, E2E — so each runs, fails and is retried independently in CI.
2. Within a project, tag every feature with its suite: `@ui`, `@api`, `@e2e`, plus `@smoke` / `@regression` for depth.
3. CI selects by project first, tag filter second: `dotnet test tests/QAAutomation.Tests.Api --filter "TestCategory=smoke"`.
4. If the three projects are ever collapsed into one, tag-driven filters become mandatory, not optional — a suite must never be able to pull in another suite's fixtures.

## Naming conventions

| Artefact | Convention | Example |
| --- | --- | --- |
| Feature file | `PascalCase.feature`, one capability per file | `VatReturnSubmission.feature` |
| Step definitions | `*Steps.cs`, `[Binding]`, grouped by capability not by Given/When/Then | `VatReturnSubmissionSteps.cs` |
| Page object | `*Page.cs` | `VatReturnPage.cs` |
| Page component | `*Component.cs` | `PeriodSelectorComponent.cs` |
| API client | `*Client.cs`; Refit interface `I*Api` | `VatReturnClient.cs`, `IVatReturnApi` |
| Contract record | `*Request` / `*Response` | `SubmitVatReturnRequest` |
| Hooks class | `*Hooks.cs` | `WebDriverHooks.cs` |
| Test data builder | `*Builder.cs` | `TaxpayerBuilder.cs` |
| Config class | `*Options.cs` | `BrowserOptions.cs` |
| Namespace | mirrors folder path under the project root | `QAAutomation.UI.Pages` |

## Glob targets for conditional steering

Other steering files target these globs. Keep the layout stable or update both sides together.

| Concern | `fileMatchPattern` |
| --- | --- |
| Feature files / Gherkin style | `**/*.feature` |
| Step definitions | `**/*Steps.cs` |
| Page objects | `**/Pages/**/*.cs`, `**/UI/**/*Page.cs` |
| Page components | `**/Components/**/*.cs` |
| API clients | `**/Clients/**/*.cs`, `**/Api/**/*Client.cs` |
| API contracts | `**/Contracts/**/*.cs` |
| Hooks / DI / lifecycle | `**/Support/**/*.cs`, `**/*Hooks.cs` |
| Test data | `**/TestData/**/*.cs`, `**/*Builder.cs` |
| Configuration | `**/Configuration/**/*.cs`, `**/appsettings*.json` |
| Pipelines *(deferred — no YAML in the repo yet)* | `**/*.yml`, `azure-pipelines*.yml` |
| Package pinning | `**/Directory.Packages.props`, `**/*.csproj` |

## Non-negotiables

1. No secrets in the repository. Configuration binds from environment variables or the CI secret store; committed `appsettings*.json` carries placeholders such as `YOUR_API_KEY_HERE` only.
2. No package added without a pin in `Directory.Packages.props` — see #[[file:tech-stack.md]].
3. No `Thread.Sleep` in UI code. Selenium has no auto-waiting, so UI waits go through the one shared `WebDriverWait` helper in `Core/Utilities` — see #[[file:ui-automation-selenium.md]]. Never a sleep, never a hand-rolled polling loop.
4. One assertion concern per scenario; assertions live in steps, never in page objects or clients.
