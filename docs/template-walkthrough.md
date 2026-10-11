# Template walkthrough — every folder, every file, and why it sits there

A study guide for the owner of this repository. The README gets you to a green run; this
document explains the **shape** of the thing, folder by folder and file by file, so that you
can point it at a different system under test later without guessing.

Everything below was read from the repository before it was described. Where a C# or .NET idea
appears that a manual tester would not have met, it is explained in a sentence or two at the
point it first matters.

Windows paths and PowerShell throughout. In PowerShell, commands are separated with `;`, not
`&&`.

---

## Contents

- [1. What this template is](#1-what-this-template-is)
- [2. The repository at a glance](#2-the-repository-at-a-glance)
- [3. The one-way dependency rule](#3-the-one-way-dependency-rule)
- [4. Root-level files](#4-root-level-files)
  - [4.1 QAAutomation.sln](#41-qaautomationsln)
  - [4.2 Directory.Build.props](#42-directorybuildprops)
  - [4.3 Directory.Build.targets](#43-directorybuildtargets)
  - [4.4 Directory.Packages.props](#44-directorypackagesprops)
  - [4.5 .editorconfig](#45-editorconfig)
  - [4.6 .gitignore and .gitattributes](#46-gitignore-and-gitattributes)
  - [4.7 README.md](#47-readmemd)
  - [4.8 QAAutomation.sln.DotSettings.user](#48-qaautomationslndotsettingsuser)
- [5. The tooling and governance folders](#5-the-tooling-and-governance-folders)
  - [5.1 .kiro\steering\](#51-kirosteering)
  - [5.2 .kiro\hooks\](#52-kirohooks)
  - [5.3 .kiro\skills\](#53-kiroskills)
  - [5.4 .githooks\](#54-githooks)
  - [5.5 scripts\](#55-scripts)
  - [5.6 .agents\tasks\](#56-agentstasks)
  - [5.7 .vscode\ and .idea\](#57-vscode-and-idea)
- [6. src\QAAutomation.Core\ — the heart of the template](#6-srcqaautomationcore--the-heart-of-the-template)
  - [6.1 QAAutomation.Core.csproj](#61-qaautomationcorecsproj)
  - [6.2 CoreRegistration.cs](#62-coreregistrationcs)
  - [6.3 Configuration\](#63-configuration)
  - [6.4 Support\](#64-support)
  - [6.5 Utilities\](#65-utilities)
  - [6.6 TestData\ — deliberately absent](#66-testdata--deliberately-absent)
- [7. src\QAAutomation.Api\](#7-srcqaautomationapi)
- [8. src\QAAutomation.UI\](#8-srcqaautomationui)
- [Why the wiring files sit at each test project's root](#why-the-wiring-files-sit-at-each-test-projects-root)
- [9. tests\QAAutomation.Tests.UI\](#9-testsqaautomationtestsui)
- [10. tests\QAAutomation.Tests.Api\](#10-testsqaautomationtestsapi)
- [11. tests\QAAutomation.Tests.E2E\](#11-testsqaautomationtestse2e)
- [12. test-results\](#12-test-results)
- [13. Execution flow: what happens when a UI scenario runs](#13-execution-flow-what-happens-when-a-ui-scenario-runs)
- [14. Execution flow: what happens when an API scenario runs](#14-execution-flow-what-happens-when-an-api-scenario-runs)
- [15. Key design decisions and the reasoning](#15-key-design-decisions-and-the-reasoning)
- [16. How do I actually run this](#16-how-do-i-actually-run-this)
- [17. Reusing this template on a new project](#17-reusing-this-template-on-a-new-project)

---

## 1. What this template is

A C# behaviour-driven test automation template covering three layers — UI, API and end-to-end
— built on Reqnroll 3.3.4 as the BDD runner, NUnit 4.6.1 as the test framework underneath it,
Selenium WebDriver 4.50.0 for the browser, Refit 16.3.0 for typed HTTP calls and
AwesomeAssertions 9.6.0 for assertions, all targeting `net10.0`. It currently drives the
public Landmark Systems marketing website (`https://www.landmarksystems.co.uk`) and that
site's read-only WordPress content API, which stand in as a placeholder for KEYinfinity, the
product the framework is really intended for. Six projects, three of them test suites,
18 scenarios in total: 9 API, 8 UI, 1 end-to-end.

Two words you will meet constantly. A **solution** is the whole codebase, described by
`QAAutomation.sln`. A **project** is one buildable part of it, described by a `.csproj` file.
This solution has six projects.

---

## 2. The repository at a glance

```text
QAAutomation\
  QAAutomation.sln                    the solution: names the six projects and their folders
  QAAutomation.sln.DotSettings.user   Rider's local test-session state. UNTRACKED (*.user is gitignored)
  Directory.Build.props               build settings every project inherits (net10.0, no implicit usings)
  Directory.Build.targets             the build-time WARNING that flags "using System.Linq"
  Directory.Packages.props            every NuGet version, pinned, in one place
  .editorconfig                       formatting plus the private-field naming rule the IDE enforces
  .gitattributes                      line-ending normalisation and binary/generated file marking
  .gitignore                          what never gets committed
  README.md                           the day-one guide: prerequisites, first run, house rules

  .agents\                            agent working artefacts. COMMITTED on purpose, not framework code
    tasks\
      design.md                       the technical design this template was built from
      plan.md                         the implementation plan, step by step
      phase-a-report.md               report: the Playwright-to-Selenium swap and scope reduction
      phase-b-report.md               report: the template build-out
      2026-10-08-111025-review.md     first review pass and its findings
      2026-10-08-132239-review.md     second review pass
      2026-10-08-153000-review.md     third review pass
      review.json                     the final verdict: APPROVED, no open findings
      task-selenium-bdd-template\     the task and feature records the build was driven from
        task.json                     the overall task description and status
        context.json                  the verified stack, house rules and SUT facts the build used
        features\FEAT-001..004.json   the four work packages, with their steps

  .githooks\
    pre-commit                        blocks a commit that would publish a credential

  .kiro\
    hooks\scan-agents-artefacts.json  runs the secret scanner when a new .agents file is written
    skills\                           eight task playbooks, one folder each, SKILL.md inside
    steering\                         ten standing rule files that govern how code here is written

  .vscode\settings.json               VS Code workspace settings. Present but EMPTY ({})
  .idea\                              Rider/IntelliJ project settings. UNTRACKED (.idea/ is gitignored)

  docs\
    template-walkthrough.md           this document: the folder-by-folder study guide

  scripts\
    Find-StagedSecrets.ps1            the credential scanner the pre-commit hook and the Kiro hook run

  src\                                library code. No test runner, no assertions, no scenarios.
    QAAutomation.Core\                shared plumbing every suite needs
      QAAutomation.Core.csproj        references NO other project: the bottom of the stack
      CoreRegistration.cs            AddTestConfiguration() and AddBrowserSupport()
      Configuration\
        appsettings.json              the settings file; public URLs plus placeholders
        TestConfigurationLoader.cs    reads JSON then environment variables, and validates
        TestConfiguration.cs          the holder for the four options objects
        TestConfigurationException.cs the error type; hides values behind secret-shaped keys
        WebDriverOptions.cs           Browser, Headless, window size, two timeouts
        ApiOptions.cs                 BaseUrl, TimeoutSeconds, TenantId, ApiKey
        SiteOptions.cs                facts about the site a scenario must not hard-code
        ArtefactOptions.cs            where failure artefacts are written
      Support\
        WebDriverFactory.cs           builds Chrome from configuration. THE HEADLESS SWITCH LIVES HERE
        ScenarioDriver.cs             owns one browser for one scenario; Start() and Quit()
        FailureArtefacts.cs           writes the four evidence files when a scenario fails
        ScenarioCorrelation.cs        one id per scenario, sent as a header and written to disk
      Utilities\
        ElementWaits.cs               THE ONLY wait helper, and the only place with an Until lambda
        ArtefactPaths.cs              works out the artefact folder and creates it
        FileNameSanitiser.cs          turns a scenario title into a safe folder name
        TestIdLocator.cs              builds a data-testid locator. Ships UNUSED, on purpose
        UrlHostChecker.cs             guards the end-to-end hand-off: same host or refuse
        HeaderValueReader.cs          reads one response header value, null when absent
        PositiveIntegerParser.cs      parses a whole number that must be above zero
    QAAutomation.UI\
      QAAutomation.UI.csproj          references Core only
      UiRegistration.cs               AddPageObjects()
      Pages\
        HomePage.cs                   the home page: Open(), ReadTitle(), FollowNavigationLinkTo()
        LandedPage.cs                 "wherever navigation took us": Open(url), ReadTitle(), ReadCurrentUrl()
        ContactUsPage.cs              the enquiry form. NO submit method, and none may be added
      Components\
        PrimaryNavigationComponent.cs the header nav bar, shared by every page
        SubscribeModalComponent.cs    the newsletter popup that fires ten seconds after load
    QAAutomation.Api\
      QAAutomation.Api.csproj         references Core only
      ApiRegistration.cs              AddApiClients(): the Refit client, its pipeline, the wrapper
      Clients\
        IWordPressApi.cs              the typed HTTP contract; three GET methods
        PagesClient.cs                the class steps inject; reads transport details once
        RequestContextHandler.cs      puts the correlation id on every outgoing request
      Contracts\
        WordPressPageResponse.cs      one page: Id, Slug, Link, Title
        RenderedText.cs               WordPress's { "rendered": "..." } wrapper
        PageListResponse.cs           what a step needs to know about a list request
        PageLookupResponse.cs         what a step needs to know about a single lookup
        WordPressErrorResponse.cs     the refusal body: Code and Message

  tests\                              the three suites. These are the binding assemblies.
    QAAutomation.Tests.UI\
      QAAutomation.Tests.UI.csproj    references Core and UI. Never Api, never another test project
      reqnroll.json                   runner settings: feature language en-GB
      AssemblyInfo.cs                 parallelism: fixtures, two at a time
      ContainerStartup.cs             raises the container's first-resolution timeout to 60s
      UiScenarioDependencies.cs       the [ScenarioDependencies] factory: this suite's service graph
      WebDriverHooks.cs               [BeforeScenario] start browser, [AfterScenario] capture then quit
      SiteNavigationContext.cs        typed state shared between navigation steps
      EnquiryFormContext.cs           typed state shared between enquiry-form steps
      Features\
        Homepage.feature              2 scenarios: page title, navigation visible
        PrimaryNavigation.feature     1 scenario: the About Us link lands on About Us
        EnquiryForm.feature           5 scenarios: a 4-row outline on mandatory fields, plus one typing check
        *.feature.cs                  GENERATED by Reqnroll at build time. Gitignored. Never edit
      Steps\
        HomepageSteps.cs              bindings for Homepage.feature
        PrimaryNavigationSteps.cs     bindings for PrimaryNavigation.feature
        EnquiryFormSteps.cs           bindings for EnquiryForm.feature
    QAAutomation.Tests.Api\
      QAAutomation.Tests.Api.csproj   references Core and Api. No UI reference, so no browser exists
      reqnroll.json                   feature language en-GB
      AssemblyInfo.cs                 parallelism: fixtures, two at a time
      ContainerStartup.cs             same first-resolution fix, duplicated on purpose
      ApiScenarioDependencies.cs      this suite's service graph. No driver, no page objects
      PageApiContext.cs               typed state shared between API steps
      Features\
        PageListing.feature           7 scenarios: 3 plain, plus two 2-row boundary outlines
        PageLookup.feature            2 scenarios: lookup by slug, and a missing identifier
      Steps\
        PageListingSteps.cs           bindings for PageListing.feature
        PageLookupSteps.cs            bindings for PageLookup.feature
    QAAutomation.Tests.E2E\
      QAAutomation.Tests.E2E.csproj   the ONLY project referencing both UI and Api
      reqnroll.json                   feature language en-GB
      AssemblyInfo.cs                 comments only, NO attribute: this suite runs serially
      ContainerStartup.cs             same first-resolution fix again
      E2EScenarioDependencies.cs      registers both layers, the only place that does
      WebDriverHooks.cs               a deliberate copy of the UI suite's hooks; SuiteName is "e2e"
      CrossLayerContext.cs            the API-to-browser hand-off state
      Features\
        PublishedPageRendersInBrowser.feature   1 scenario, on purpose
      Steps\
        PublishedPageRendersInBrowserSteps.cs   bindings for it

  test-results\                       failure evidence. Created at RUN TIME. Gitignored. Absent right now
  bin\  obj\                          BUILD OUTPUT, one pair per project. Gitignored. Ignore them entirely
```

### What to ignore when you are reading

| Path | Why you can skip it |
| --- | --- |
| `**\bin\`, `**\obj\` | Compiler output. Every project has a pair. Gitignored. |
| `**\*.feature.cs` | Reqnroll generates one next to every `.feature` at build time. Gitignored, marked `generated_code = true` in `.editorconfig`, and marked `linguist-generated` in `.gitattributes`. Never edit, never commit. |
| `test-results\` | Written only when a UI or end-to-end scenario fails. Gitignored. It does not exist in a clean clone, and an absent folder is the healthy state. |
| `.idea\`, `*.user` | Local IDE state. Both gitignored, so they exist on your disk but git does not track them. |

`.agents\` is the exception that proves the rule: it is **committed deliberately**. The
`.gitignore` says so in a comment, because those documents explain why the framework is shaped
this way and are reusable on the next project.

---

## 3. The one-way dependency rule

Dependencies run in exactly one direction:

```text
tests\*   ->   src\*   ->   QAAutomation.Core
```

Read right to left: `Core` references no other project in this repository. `QAAutomation.UI`
and `QAAutomation.Api` each reference `Core` and nothing else. The three test projects
reference `Core` plus whichever layer they drive. **No test project references another test
project**, and nothing in `src\` ever references anything in `tests\`.

What each project is allowed to reference, read from the actual `.csproj` files:

| Project | References |
| --- | --- |
| `QAAutomation.Core` | nothing |
| `QAAutomation.UI` | Core |
| `QAAutomation.Api` | Core |
| `QAAutomation.Tests.UI` | Core, UI |
| `QAAutomation.Tests.Api` | Core, Api |
| `QAAutomation.Tests.E2E` | Core, UI, Api |

Two consequences worth internalising, because they are the whole reason the rule exists:

- **The API suite cannot start a browser.** Not "is told not to" — it has no reference to
  `QAAutomation.UI` and its service graph registers no driver, so there is no browser for it to
  start. Suite separation is structural, not a convention somebody has to remember.
- **A scenario context cannot live in `Core`.** `PageApiContext` holds `PageListResponse`, a
  type from `QAAutomation.Api`. Moving that context into `Core` would force `Core` to reference
  `QAAutomation.Api` and reverse the arrow. That is why the contexts sit at each test project's
  root instead.

---

## 4. Root-level files

### 4.1 QAAutomation.sln

The solution file. It lists the six projects, gives each a GUID, and groups them under two
solution folders named `src` and `tests` so the IDE tree mirrors the folder tree. It also
declares the build configurations — `Debug` and `Release`, each for `Any CPU`, `x64` and `x86`
— and maps every platform to `Any CPU`, which is the normal arrangement for a .NET library
solution.

You will rarely edit this by hand. `dotnet sln add` and the IDE maintain it. The one thing
worth knowing is that `dotnet build` and `dotnet test` with no argument act on the solution, so
a scenario in any of the three suites can be reached from the repository root.

The solution file also serves a second, less obvious purpose: `ArtefactPaths` locates the
repository root by walking up from the test assembly until it finds `QAAutomation.sln`. See
[6.5](#65-utilities).

### 4.2 Directory.Build.props

**The MSBuild concept first.** MSBuild is the build engine behind `dotnet build`. When it
builds a project, it looks up the folder tree for a file called `Directory.Build.props` and
imports the first one it finds **before** reading the project file. So a single file at the
repository root sets properties for every project beneath it, and no `.csproj` has to opt in.
That is why the six `.csproj` files in this repository are so short — there is no
`<TargetFramework>` in any of them.

What this one sets, with the repository's own reasoning:

| Property | Value | Why |
| --- | --- | --- |
| `TargetFramework` | `net10.0` | .NET 10, matching the installed SDK (`dotnet --list-sdks` here lists 2.1.526, 8.0.425 and 10.0.301 — only the last one matters). |
| `LangVersion` | `latest` | Use the newest C# the SDK offers. |
| `ImplicitUsings` | `disable` | **This one is load-bearing.** The SDK's implicit `using` set includes `System.Linq`, which would silently re-enable the one construct the house style bans outright. Disabling it also means every file names its own `using` directives, so a file is readable on its own. |
| `Nullable` | `disable` | The house style assigns `null` to plain locals on purpose, and a template that warns about its own documented style teaches noise rather than the rule. |
| `TreatWarningsAsErrors` | `false` | A warning should inform, not stop a build that has no product problem. |
| `EnforceCodeStyleInBuild` | `true` | Makes the `.editorconfig` rules show up during a build, not only in the IDE. |
| `IsPackable` | `false` | None of these projects is a NuGet package. |
| `GenerateDocumentationFile` | `true` | Roslyn only runs IDE0005 (unused `using` directive) during a build when a documentation file is generated, and `.editorconfig` asks for IDE0005 as a warning. |
| `NoWarn` | adds `CS1591` | Suppresses "missing XML comment on a public member". Requiring a comment on every public member of a test framework would be busywork; the comments that matter are already there. |

The `GenerateDocumentationFile` / `CS1591` pairing is the sort of thing that looks arbitrary
until you read why: the first switch exists purely to make an unused-`using` warning work,
and the second stops that switch from producing hundreds of unrelated warnings. A leftover
`using` is the first sign that a LINQ chain was only half removed.

### 4.3 Directory.Build.targets

Same MSBuild mechanism as `.props`, with one difference: a `Directory.Build.targets` file is
imported **after** the project file rather than before. That makes it the right place for
build *steps*, as opposed to build *settings*.

This file contains exactly one target, `WarnOnLinqUsage`. It runs `AfterTargets="Build"` and
is conditioned on `'$(OS)' == 'Windows_NT'`. It collects the project's hand-written source
files, excluding generated code:

```xml
<HandWrittenSource Include="@(Compile)"
                   Exclude="$(BaseIntermediateOutputPath)**\*.cs;obj\**\*.cs;bin\**\*.cs;**\*.feature.cs" />
```

then runs `findstr` over them looking for `using System.Linq`, and emits an MSBuild `Warning`
naming any file that matched.

Three things to understand about it:

1. **It is a warning, never an error.** The file says so explicitly: a red build with no
   product reason costs a team more than a style slip does. Turning it into an error would be a
   deliberate decision to record in the steering notes first.
2. **It is Windows-only.** `findstr` is a Windows command, so on a Linux or macOS agent the
   target simply does not run and the rule falls back to review alone. The file names the
   proper fix — take `Microsoft.CodeAnalysis.BannedApiAnalyzers`, which is cross-platform and
   can name a method rather than a namespace — and warns against the tempting wrong fix of
   removing the Windows condition, because `ContinueOnError` would turn a failing command into
   a silent pass that looks like enforcement.
3. **Generated code is excluded on purpose.** Refit generates into `obj\`, and Reqnroll writes
   a `.feature.cs` beside every `.feature`. Without the exclusions, a future generator emitting
   `using System.Linq` would raise a warning naming a file nobody is allowed to edit.

### 4.4 Directory.Packages.props

**Central Package Management**, which is worth a paragraph because it changes how you add a
package.

Normally a `.csproj` says both what it needs and which version:
`<PackageReference Include="NUnit" Version="4.6.1" />`. That spreads versions across six files
and lets two projects drift onto different versions of the same library. Central Package
Management splits the two halves. This file turns the feature on:

```xml
<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
```

and then declares every version exactly once as a `<PackageVersion>` entry. A `.csproj` says
only *what* it needs, with **no `Version` attribute at all**:

```xml
<PackageReference Include="Reqnroll.NUnit" />
```

If you add a `Version` attribute to a `.csproj` in this repository, the restore fails with
NU1008. That is the feature working, not a bug. `CentralPackageTransitivePinningEnabled` extends
the same control to transitive dependencies — the packages your packages pull in — so a
library cannot quietly drag in a different version of something already pinned here.

The pinned set, grouped exactly as the file groups it:

| Group | Packages | Version |
| --- | --- | --- |
| BDD runner and unit test framework | `Reqnroll`, `Reqnroll.NUnit`, `Reqnroll.Microsoft.Extensions.DependencyInjection` | 3.3.4 |
| | `NUnit` | 4.6.1 |
| | `NUnit3TestAdapter` | 6.3.0 |
| | `Microsoft.NET.Test.Sdk` | 18.10.1 |
| Browser automation | `Selenium.WebDriver`, `Selenium.Support` | 4.50.0 |
| Assertions | `AwesomeAssertions` | 9.6.0 |
| REST client | `Refit`, `Refit.HttpClientFactory` | 16.3.0 |
| Configuration and dependency injection | `Microsoft.Extensions.Configuration`, `.Configuration.Json`, `.Configuration.EnvironmentVariables`, `.Http`, `.DependencyInjection.Abstractions` | 10.0.12 |

**Why pinning matters here specifically.** A test suite's job is to tell you whether the
product changed. A floating version range (`*`, `[9.0,)`) means the suite can also change
underneath you, so a build that went red overnight has two possible causes instead of one.
Pinning removes one of them. Exact versions also make a failure reproducible months later,
which is the difference between diagnosing a flaky test and guessing about it.

**Two absences to understand, because they will otherwise send you hunting.**

- **Reserved packages are listed in steering but not pinned here.**
  `.kiro\steering\tech-stack.md` has a whole "Reserved" table — WireMock.Net,
  Testcontainers.PostgreSql, Npgsql, Respawn, Bogus, Polly, Allure.Reqnroll, Reqnroll.Verify,
  RestSharp — each with a version and a note saying when to bring it back. Those are
  **decisions recorded in advance**, not dependencies. None of them appears in this file, and
  `dotnet restore` does not download any of them. The current system under test is a read-only
  public website: no database to reset, no tenancy, no third party to stub, no scenario that
  creates data. The comment at the top of this file names them and says they stay unpinned
  until they are actually activated.
- **Four packages are banned outright,** also named in the comment:
  `Selenium.WebDriver.ChromeDriver` and `WebDriverManager` (Selenium Manager already resolves
  the driver — see [15](#15-key-design-decisions-and-the-reasoning)),
  `DotNetSeleniumExtras.WaitHelpers` (abandoned, exactly one release ever) and
  `FluentAssertions` (licence — see the same section).

The rule to carry forward: **no package is added without a pin here and a decision recorded in
`tech-stack.md`.**

### 4.5 .editorconfig

A standard, IDE-agnostic file for formatting and code-style rules. `root = true` at the top
stops editors looking further up the disk for another one. Because
`Directory.Build.props` sets `EnforceCodeStyleInBuild`, the rules here also surface during a
build, so the IDE and the build say the same thing.

What it enforces:

| Scope | Rule |
| --- | --- |
| `[*]` | UTF-8, CRLF line endings, final newline, trimmed trailing whitespace, spaces, indent 4 |
| `[*.{feature,json,yml,yaml,md}]` | indent 2 |
| `[*.cs]` | all three `csharp_style_var_*` preferences set to `false:suggestion` |
| `[*.cs]` | `dotnet_diagnostic.IDE0005.severity = warning` |
| `[*.cs]` | three `dotnet_naming_rule` entries for private field naming |
| `[*.feature.cs]` | `generated_code = true` |

Two of those deserve explanation.

**The `var` severity is `suggestion`, not `warning`, on purpose.** The house style wants
explicit types (`List<Transaction> rows = ...`), but it does allow `var` when the right-hand
side is a `new` of that exact type. A `warning` severity would flag that one permitted form on
every build.

**The private-field naming rule is the repository settling an argument.** Private instance
fields are plain `camelCase` and are read through `this.` — `this.driver`, `this.waits` — never
`_underscore`. Constants and `static` fields keep `PascalCase`, which is what they already use
(`SuiteName`, `CloseButton`). The order matters: the constant and static rules are declared
*before* the instance rule, because the first matching rule wins. Both forms are defensible in
C# generally; this repository picks one so that no file has to be read twice.

**What this file does *not* do: it cannot ban LINQ.** An `.editorconfig` has no rule that
forbids a namespace. What it does instead is raise IDE0005 — unused `using` directive — to a
warning, because a leftover `using System.Linq` is the first sign that a LINQ chain was only
half removed. The actual mechanical check on `using System.Linq` lives in
[`Directory.Build.targets`](#43-directorybuildtargets).

### 4.6 .gitignore and .gitattributes

**`.gitignore`** — what never gets committed, grouped by reason:

- `bin/`, `obj/` — build output.
- `test-results/`, `TestResults/`, `*.trx` — test output and failure artefacts, runtime only.
- `allure-results/` — reporting output; Allure is reserved, not active.
- `*.feature.cs` — Reqnroll writes one of these next to every `.feature` at build time. They
  are generated code: never edit one, never commit one.
- `*.user`, `.vs/`, `.idea/` — local editor and user files. This is why
  `QAAutomation.sln.DotSettings.user` and the whole `.idea\` tree exist on disk but are not
  tracked by git. `.vscode\settings.json` is **not** ignored, so it is tracked.

The file ends with a comment rather than a pattern, and it is the interesting part:
`.agents/` **is** committed on purpose, because the design document, plan and review reports
explain why the framework is shaped this way and are reusable as templates next time. The same
comment adds the warning that follows from that decision: those reports quote source and
configuration, so check anything new under `.agents/` for credentials before committing it.
Placeholders such as `YOUR_API_KEY_HERE` only.

**`.gitattributes`** — three jobs:

1. `* text=auto eol=lf` normalises line endings so the repository stays clean across Windows
   dev machines and Linux CI agents. Without it, a Linux agent produces a whole-file diff on
   every text file it touches. `*.ps1`, `*.cmd` and `*.bat` are exempted back to CRLF so they
   run correctly when checked out on Windows.
2. Binary types (`*.png`, `*.dll`, `*.exe`, `*.snk` and others) are marked `binary` so git never
   tries to normalise them.
3. `*.feature.cs linguist-generated=true` keeps generated Reqnroll code out of diffs if one is
   ever committed by accident, and `*.md linguist-documentation=true` helps the host report the
   language mix correctly.

### 4.7 README.md

The README is the **day-one operational guide**; this document is the **study guide**. Reach for
the README when you want to run something, and this file when you want to know why something
is where it is.

The README's twelve sections cover: what the template is and the placeholder site; when to
write a UI, API or end-to-end test; a directory map; the two prerequisites (.NET 10 SDK and
Chrome) and why there is no driver install step; the first run; running a subset with
`--filter`, including the `FullyQualifiedName` trap where the generated class name comes from
the `Feature:` line rather than the file name; two end-to-end traces from a Gherkin line down
to the code; a recipe for adding a new test; the five house rules with the reasoning and the
secret-scanner commands; an honest-limitations section; where failure artefacts are written and
how to treat them; and a glossary of BDD terms.

This walkthrough does not repeat those. Where the two overlap — the run commands in
[16](#16-how-do-i-actually-run-this), the design decisions in
[15](#15-key-design-decisions-and-the-reasoning) — the content here was re-verified against the
repository rather than copied.

### 4.8 QAAutomation.sln.DotSettings.user

JetBrains Rider's per-user settings for this solution. It currently holds exactly one thing: a
saved unit-test session named "All tests in category 'smoke'" that filters on the `smoke`
category. It is matched by the `*.user` line in `.gitignore`, so it is **not tracked** — it
exists only on this machine and nothing depends on it. If you work in Visual Studio or VS Code
you will never see it.

Worth noticing for one reason: that saved session is the IDE equivalent of
`--filter "TestCategory=smoke"`, which is the same tag filtering described in
[16](#16-how-do-i-actually-run-this).

---

## 5. The tooling and governance folders

None of these folders ships with the test suite. They govern how the suite is written and they
guard what reaches the repository.

### 5.1 .kiro\steering\

Ten Markdown files. Each one carries a small YAML header declaring **when it loads**:

- `inclusion: always` — always in context.
- `inclusion: fileMatch` with a `fileMatchPattern` — loads only when a matching file is open or
  being edited.
- `inclusion: manual` — loaded only when deliberately pulled in.
- `inclusion: auto` with a `name` and `description` — offered automatically when the described
  situation arises.

Keeping most of them conditional is the point: the always-on budget stays small, and the three
always-on files are deliberately short.

| File | Loads | What it governs |
| --- | --- | --- |
| `project-structure.md` | **always** | The solution layout, the layer table, naming conventions, suite separation, and the glob targets the other files key off. |
| `tech-stack.md` | **always** | Every package choice and pin, the Active-versus-Reserved split, the Selenium-over-Playwright decision, and the FluentAssertions licence warning. |
| `code-style.md` | **always** | Readability over cleverness: the no-LINQ rule and its three narrow exceptions, explicit types, `this.`-prefixed camelCase fields, guard clauses, and the sync/async table by layer. |
| `api-automation.md` | `fileMatch`: `**/Clients/**/*.cs`, `**/Api/**/*Client.cs`, `**/*Client.cs`, `**/Contracts/**/*.cs` | Refit as the default contract surface, `IApiResponse<T>` over bare `T`, auth token caching, multi-tenant request context, status-code **and** body assertions, and the security rules for credentials. |
| `bdd-gherkin-standards.md` | `fileMatch`: `**/*.feature` | Declarative Gherkin, one behaviour per scenario, `Background` discipline, `Scenario Outline` rules tied to a named test-design technique, and the tag taxonomy. |
| `step-definitions.md` | `fileMatch`: `**/*Steps.cs` | The thin-step rule, constructor injection only, typed contexts instead of `ScenarioContext` string bags, ambiguity rules, assertions, sync/async discipline and the hook responsibility table. |
| `ui-automation-selenium.md` | `fileMatch`: `**/Pages/**/*.cs`, `**/UI/**/*Page.cs`, `**/*Page.cs` | Locator strategy and the `data-testid` contract, page object shape, the mandatory shared wait helper, driver lifecycle, designing out stale elements, and failure artefacts. |
| `test-design-techniques.md` | `auto` (named "Test design techniques") | Equivalence partitioning, boundary value analysis, state transition and decision tables, each ending in a Gherkin `Examples` table. |
| `ci-azure-devops.md` | `fileMatch`: `**/azure-pipelines*.yml`, `**/pipelines/**/*.yml`, `**/*.yml` | Azure DevOps conventions. Its own header says **DEFERRED**: no pipeline file exists in this repository and `pipelines\` has not been created. |
| `domain-keyinfinity.md` | `manual` | The KEYinfinity business glossary, so Gherkin reads in the business's language. Its header marks which parts are inferred from the role brief rather than verified. |

Two habits these files bake in and that this walkthrough follows: a decision is recorded
**before** it is used, and anything inferred rather than verified is marked as such.

### 5.2 .kiro\hooks\

One file, `scan-agents-artefacts.json`. A hook is automation the repository owner configured to
run on an event. This one:

- triggers on `PostFileCreate`,
- matches paths containing `.agents\` (the `matcher` is the regex `\.agents[\\/]`),
- runs `scripts\Find-StagedSecrets.ps1` with `-Scope Path -Path "d:\git\QAAutomation\.agents"`,
- with a 60-second timeout.

Its description states the reasoning: `.agents\` is committed on purpose, so a leak is caught
at **write** time rather than waiting for commit time. It is the belt to the pre-commit hook's
braces.

### 5.3 .kiro\skills\

Eight folders, each containing one `SKILL.md` with a `name` and a `description` in its header.
A skill is a playbook for a recurring task — not rules, but a procedure with a defined output.

| Skill | What it produces |
| --- | --- |
| `author-gherkin-acceptance-criteria` | A **pre-development** skill: turns a story or vague requirement into reviewable Given/When/Then criteria plus an explicit list of open questions for the Product Manager. |
| `design-test-cases` | A `Scenario Outline` with technique-named `Examples` tables, plus a statement of what is deliberately **not** covered. The uncovered list is half the deliverable. |
| `scaffold-reqnroll-feature` | The full artefact set for one named feature area — `.feature`, steps, page object or client, DI registration — obeying the one-way dependency rule. |
| `build-selenium-page-object` | One page object or component for a Vue 3 screen, built so a markup regeneration does not turn the suite red. |
| `build-api-contract-test` | The complete API-layer test set for one endpoint: typed contract, client wrapper, auth plumbing, feature file and steps, covering happy path, boundaries, the authorisation matrix and cross-tenant refusal. |
| `triage-flaky-test` | A named root cause and the matching fix for an intermittent or cross-layer failure, or a quarantine entry with a ticket. Never a retry. |
| `review-automation-pr` | A triaged list of findings, each with the replacement the author should write, plus one thing the pull request did well. |
| `wire-suite-into-azure-pipeline` | One pipeline stage for one suite: tag filtering, agent prerequisites, published results and artefacts, concurrency limits, Key Vault secrets. |

The last one is written and waiting: CI is parked, so there is nothing for it to edit yet.

### 5.4 .githooks\

One file: `pre-commit`. A POSIX shell script (git runs hooks through its bundled shell on
Windows too) that blocks a commit which would publish a credential.

What it does, in order:

1. Checks `scripts/Find-StagedSecrets.ps1` exists. If not, it prints
   `failing closed - a security check that cannot run must block` and exits 1.
2. Looks for `pwsh` (PowerShell 7+) and falls back to `powershell` (Windows PowerShell 5.1). If
   neither is on `PATH`, it fails closed again, telling you to install PowerShell or bypass
   deliberately with `--no-verify` after checking the diff yourself.
3. Runs the scanner with `-Scope Staged` and propagates a non-zero exit as a blocked commit.

**It is not active until you activate it, and right now it is not active in this clone.** Git
does not install committed hooks for you; it only runs what is in `.git\hooks\`, and this file
lives in `.githooks\` so that it survives a clone and gets reviewed like any other code. The
one-off activation, which the script's own header documents:

```powershell
cd d:\git\QAAutomation
git config core.hooksPath .githooks
git config --get core.hooksPath          # should print .githooks
```

I checked: `git config --get core.hooksPath` returns nothing here, so until you run that line
nothing is scanning your commits.

To undo it: `git config --unset core.hooksPath`.

### 5.5 scripts\

One file: `Find-StagedSecrets.ps1`, the scanner both hooks run.

**Three scopes**, chosen with `-Scope`:

| Scope | Scans | Used by |
| --- | --- | --- |
| `Staged` (default) | Staged content only, read with `git show :<path>` | the pre-commit hook |
| `Tracked` | Every tracked file, via `git ls-files` | an audit of what is already committed |
| `Path` | The files or folders given in `-Path` | the Kiro hook, pointed at `.agents` |

`Staged` reads the **staged blob**, not the working copy, because a file can be staged clean and
then dirtied, and only the staged half ships.

**What it looks for** — fifteen rules, each matching a *value* rather than prose: GitHub tokens
and fine-grained PATs, AWS access key ids and secret access keys, Azure storage account keys,
Google API keys, Slack tokens, NuGet API keys, private key blocks, JSON web tokens, bearer and
basic auth values, URLs with an inline password, a password inside a connection string, and
secret-shaped assignments (`api_key = "..."` and friends).

The narrowness is deliberate and the script says why: a broad rule firing on the word
"password" would flag every steering file that says *never commit a password*, the scanner
would be switched off within a week, and that is worse than having no scanner.

**Three behaviours worth knowing:**

| Behaviour | Why |
| --- | --- |
| Matched values are **masked** (`ghp_********r8`, via `Get-MaskedExcerpt`) | Printing a secret copies it into terminal history and, in CI, into the build log — the very thing the scanner exists to prevent. |
| A placeholder is a **pass** | `YOUR_API_KEY_HERE` is the correct thing to commit. The allow-list is tested against the **matched value**, not the whole line, so a real token is not excused by the word "example" appearing elsewhere on the line. |
| A scanner that cannot run **exits 2** | Both hooks treat that as a block. A security check that errors must fail closed; an unexplained pass is worse than a stopped commit. |

Exit codes: `0` clean, `1` at least one finding, `2` the scanner itself failed. It also excludes
paths not worth scanning — build output, binaries, `test-results`, and `Find-StagedSecrets.ps1`
and `pre-commit` themselves, since the scanner contains every pattern above and would otherwise
flag its own source.

Running it by hand:

```powershell
cd d:\git\QAAutomation
powershell -NoProfile -File .\scripts\Find-StagedSecrets.ps1
powershell -NoProfile -File .\scripts\Find-StagedSecrets.ps1 -Scope Tracked
powershell -NoProfile -File .\scripts\Find-StagedSecrets.ps1 -Scope Path -Path .agents
```

### 5.6 .agents\tasks\

Agent working artefacts, committed on purpose. **Useful as history, not part of the
framework** — delete the whole folder and the suite still builds and passes.

The table below lists the committed set. The folder also collects review artefacts from any
later agent pass, so expect untracked files alongside these.

| File | What it is |
| --- | --- |
| `design.md` | The technical design the template was built from. |
| `plan.md` | The implementation plan: the ordered, specific steps. |
| `phase-a-report.md` | Report on the Playwright-to-Selenium swap and the scope reduction, made by correcting the existing steering files in place. |
| `phase-b-report.md` | Report on the template build-out, including the verification numbers. |
| `2026-10-08-111025-review.md` | First review pass: nine findings. |
| `2026-10-08-132239-review.md` | Second pass: the nine closed, four new. |
| `2026-10-08-153000-review.md` | Third pass: the four closed, plus three non-blocking notes — `Issues (3 notes, 0 blocking)`, on the title comparison, the cross-layer entity gap and unguarded artefact-folder creation. Verdict APPROVED. |
| `review.json` | The final verdict — `"verdict": "APPROVED"`, empty `findings`. |
| `task-selenium-bdd-template\task.json` | The overall task description and its `completed` status. |
| `task-selenium-bdd-template\context.json` | The brief the build was carried out against: the stack, the pinned versions, the binding house rules, and a `verified_target_system_facts` block recording what was actually probed on the Landmark site — the nav link labels, the Gravity Forms field ids, the `per_page` boundaries, and `X-WP-Total: 18` noted explicitly as a live value that must never be asserted. |
| `task-selenium-bdd-template\features\FEAT-001..004.json` | The four work packages and their steps. |

Why keep them: the reviews are the audit trail for the non-obvious decisions you will find in
the code. `FailureArtefacts.Capture` guarding each of its four writes separately,
`PagesClient.ReadError` catching `JsonException`, and `PrimaryNavigationComponent` having both
`WaitUntilVisible()` and `IsVisible()` were all asked for in the **second** review document
(`2026-10-08-132239-review.md`, items 2, 3 and 4 of its `Issues (4)` list) — and the third
document is where each is recorded as closed. That is the order to read them in: the second says
why the code has the shape it has, the third confirms it landed.

Treat them as read-only history. If you reuse this template, the reports are a model for how to
document the next build-out, but they describe *this* build.

### 5.7 .vscode\ and .idea\

Both are IDE state, and neither affects the build.

- **`.vscode\settings.json`** is tracked by git and currently contains `{}` — an empty object.
  Nothing is configured. It exists so that a workspace setting has an obvious home if one is
  ever needed.
- **`.idea\.idea.QAAutomation\.idea\`** is JetBrains Rider project state, and the whole tree is
  gitignored (`.idea/` in `.gitignore`), so it is local to this machine. It holds: its own
  `.gitignore` listing the Rider files that should never be shared; `vcs.xml`, which maps the
  project directory to Git; `indexLayout.xml`, which has no attached folders or explicit
  includes/excludes; `projectSettingsUpdater.xml`, three Rider UI preferences; and
  `workspace.xml`, local window and session state.

Nothing in either folder needs to travel to a new project.

---

## 6. src\QAAutomation.Core\ — the heart of the template

This is the project to understand properly. Everything else is either a thin wrapper over it or
a consumer of it. It holds configuration, the browser lifecycle, failure capture and the shared
helpers — and it references **no other project**, which is what keeps the dependency arrow
pointing one way.

### 6.1 QAAutomation.Core.csproj

Short, because `Directory.Build.props` already set the target framework and the rest. It sets
`RootNamespace` to `QAAutomation.Core`, takes six packages — `Selenium.WebDriver`,
`Selenium.Support`, the three `Microsoft.Extensions.Configuration*` packages and
`Microsoft.Extensions.DependencyInjection.Abstractions` — all with **no `Version` attribute**,
and does one more thing:

```xml
<None Include="Configuration\appsettings.json" CopyToOutputDirectory="PreserveNewest" />
```

That makes the settings file travel with the build output. Each test project *also* links it
explicitly rather than relying on it flowing transitively, so the file lands in every test
project's output every time. See [9](#9-testsqaautomationtestsui).

Note which package is **not** here: nothing from NUnit or Reqnroll. Core has no test-runner
reference at all, which is why a `[Binding]` class or a `[ScenarioDependencies]` factory cannot
live in it.

### 6.2 CoreRegistration.cs

**Dependency injection in two sentences.** Instead of each class building the things it needs
with `new`, you list once what exists and what lifetime each thing has; the container then
hands every class whatever its constructor asks for. Nothing in this repository builds its own
collaborators, and nothing reaches into a static.

This file holds two **extension methods** on `IServiceCollection`. An extension method is a
static method written so it can be called as if it were a method on the type — which is why the
call sites read `services.AddBrowserSupport()`.

`AddTestConfiguration(TestConfiguration)` registers the configuration holder and all four
options objects as **singletons** — created once and shared for the whole test run:

```csharp
services.AddSingleton(testConfiguration);
services.AddSingleton(testConfiguration.WebDriver);
services.AddSingleton(testConfiguration.Api);
services.AddSingleton(testConfiguration.Site);
services.AddSingleton(testConfiguration.Artefacts);
```

They are registered as plain singletons, **not** as `IOptions<T>`. That is a readability choice
the file states outright: a page object takes a constructor parameter of type `SiteOptions` and
uses it directly — no `.Value`, no extra concept to learn. Singleton is correct because
configuration is read once per run and never changes while the run is in progress.

`AddBrowserSupport()` registers everything a browser-driving suite needs, all **scoped**.

**Scoped lifetime is the single most important idea in this file.** Scoped means one instance
per scope, and Reqnroll opens one scope per scenario. So one scenario gets one browser, one wait
helper and one correlation id, shared by the hook and every step and page object in that
scenario — and the next scenario gets fresh ones.

```csharp
services.AddScoped<ScenarioDriver>();
services.AddScoped<ScenarioCorrelation>();
services.AddScoped<FailureArtefacts>();

// Resolved lazily, at the first step that needs it. By then the BeforeScenario hook
// has already called Start(), so page objects can simply ask for an IWebDriver.
services.AddScoped<IWebDriver>(provider => provider.GetRequiredService<ScenarioDriver>().Driver);

services.AddScoped<ElementWaits>(provider => CreateElementWaits(provider));
```

The `IWebDriver` registration is the clever bit, and it is worth slowing down on. A page object
wants a plain `IWebDriver`, not a `ScenarioDriver`. But the browser does not exist until the
`[BeforeScenario]` hook has started it. Registering `IWebDriver` as a **factory** — a small
function the container calls when someone first asks — defers the question to the moment of
first use, by which time the hook has run. If a page object somehow got there first, the
`ScenarioDriver.Driver` property throws a message telling you exactly what to fix.

`CreateElementWaits` is the private helper behind the last registration. It pulls the driver and
`WebDriverOptions` out of the container, turns `ElementTimeoutSeconds` into a `TimeSpan`, and
constructs the one wait helper for that scenario. That is how the element timeout reaches the
wait helper from configuration without any call site passing it.

And note what `AddBrowserSupport` means by omission: the API suite never calls it, which is why
the API suite has no browser at all.

### 6.3 Configuration\

Eight files. One JSON settings file, one loader, one holder, one exception type and four plain
options classes.

#### appsettings.json

The whole settings file, verbatim:

```json
{
  "WebDriver": {
    "Browser": "chrome",
    "Headless": true,
    "WindowWidth": 1920,
    "WindowHeight": 1080,
    "ElementTimeoutSeconds": 20,
    "PageLoadTimeoutSeconds": 60
  },
  "Api": {
    "BaseUrl": "https://www.landmarksystems.co.uk/wp-json/wp/v2",
    "TimeoutSeconds": 30,
    "TenantId": "YOUR_TENANT_ID_HERE",
    "ApiKey": "YOUR_API_KEY_HERE"
  },
  "Site": {
    "BaseUrl": "https://www.landmarksystems.co.uk",
    "HomePageTitle": "Farm & Rural Estate Management Software | Landmark Systems",
    "ContactPagePath": "/contact-us/",
    "KnownPageSlug": "technical",
    "MissingPageId": 999999,
    "MinimumPageSize": 1,
    "MaximumPageSize": 100
  },
  "Artefacts": {
    "Directory": "test-results"
  }
}
```

**`"Headless": true` on line 4 is the answer to "why is the browser invisible?"** Nothing is
hidden or hard-coded — the browser runs with no visible window because this setting says so,
and `WebDriverFactory` turns it into a Chrome argument. How to flip it for one run is in
[16](#16-how-do-i-actually-run-this); why the environment variable is the better switch is
immediately below.

**On secrets:** the two URLs in this file are the public addresses of a public marketing
website, so they are not secrets and they belong in source control. `TenantId` and `ApiKey` are
**placeholders** — `YOUR_TENANT_ID_HERE` and `YOUR_API_KEY_HERE`. The WordPress API this
template calls has no tenancy and needs no key, so nothing reads either value yet. They exist
to show where a real credential would come from: an environment variable at run time, never a
committed file.

**Where this file actually gets read from, and why editing it needs a rebuild.** Each test
`.csproj` links the file with `CopyToOutputDirectory="PreserveNewest"`, so it is **copied next
to the test assembly** at build time, and `TestConfigurationLoader` reads it from
`AppContext.BaseDirectory` — that is, from `bin\Debug\net10.0\appsettings.json`, not from
`src\`. Edit the source file and the change does not reach a test run until the next build.
That is exactly why the environment variable is the better local switch: it needs no rebuild.

#### TestConfigurationLoader.cs

A `static` class with one public method, `Load()`, returning a `TestConfiguration`. It is the
only place in the repository that reads configuration.

**The chain, in order:**

```csharp
string settingsFilePath = Path.Combine(AppContext.BaseDirectory, SettingsFileName);

ConfigurationBuilder builder = new ConfigurationBuilder();
builder.AddJsonFile(settingsFilePath, optional: false);
builder.AddEnvironmentVariables(EnvironmentVariablePrefix);
IConfigurationRoot configuration = builder.Build();
```

Two sources, and **order decides the winner**: the JSON file is added first, environment
variables second, and a later source overrides an earlier one. So the environment always wins.
That is how a secret or a per-environment override reaches the test process without ever being
committed.

**The environment variable naming convention** — `EnvironmentVariablePrefix` is
`"QAAUTOMATION_"`:

| Configuration key | Environment variable |
| --- | --- |
| `WebDriver:Headless` | `QAAUTOMATION_WebDriver__Headless` |
| `Site:BaseUrl` | `QAAUTOMATION_Site__BaseUrl` |
| `Api:BaseUrl` | `QAAUTOMATION_Api__BaseUrl` |
| `Artefacts:Directory` | `QAAUTOMATION_Artefacts__Directory` |

The prefix is stripped, and **a double underscore stands in for the `:` nesting separator**,
because `:` is not legal in an environment variable name on every platform.

**Reading is explicit, not bound.** There is no `Get<T>()` call anywhere. Every value is read
one at a time through `GetSection(key).Value` and assigned to a property:

```csharp
WebDriverOptions options = new WebDriverOptions();
options.Browser = ReadRequiredText(configuration, "WebDriver:Browser");
options.Headless = ReadBoolean(configuration, "WebDriver:Headless");
options.WindowWidth = ReadWholeNumber(configuration, "WebDriver:WindowWidth");
```

Two reasons, both stated in the file: `Microsoft.Extensions.Configuration.Binder` is
deliberately not pinned, and explicit assignment is readable without knowing how binding works.

**It validates, and it names the key when it refuses.** Three private readers
(`ReadRequiredText`, `ReadWholeNumber`, `ReadBoolean`) and five private validators:

| Validator | Checks |
| --- | --- |
| `RequireSecureAbsoluteUrl` | An absolute URL whose scheme is **https**. Applied to `Api:BaseUrl` and `Site:BaseUrl`. |
| `RequireSensibleTimeout` | Between 1 and 300 seconds. Applied to both WebDriver timeouts and `Api:TimeoutSeconds`. |
| `RequireAtLeast` | A floor. `WindowWidth` at least 320, `WindowHeight` at least 480, `Site:MissingPageId` at least 1. |
| `RequireLeadingSlash` | `Site:ContactPagePath` starts with `/`. |
| `RequirePageSizeRange` | `MinimumPageSize` at least 1, `MaximumPageSize` at least the minimum and no more than 100. |

Every failure throws a `TestConfigurationException` naming the exact key. A misconfigured suite
tells you which line of settings is wrong, instead of failing somewhere inside Selenium twenty
seconds later.

#### TestConfiguration.cs

A plain holder with four properties — `WebDriver`, `Api`, `Site`, `Artefacts` — and no
behaviour. `TestConfigurationLoader` fills it; `AddTestConfiguration` hands the four options
objects out individually so a consumer takes only the section it needs.

#### TestConfigurationException.cs

The error type, and more careful than it first looks.

`ForKey(configurationKey, problem, offendingValue)` builds a message of the shape:

```text
Configuration key 'Site:BaseUrl' is invalid: expected an https URL. Value was: 'ftp://example.com'.
```

Naming both the key and the value is what makes a configuration failure self-diagnosing. But a
value can itself be a secret, and an exception message ends up in logs and build output. So the
class holds a list of key endings that mark a value as secret:

```csharp
private static readonly string[] SecretKeyEndings = new string[]
{
    "Key",
    "Secret",
    "Token",
    "Password"
};
```

`HoldsASecret` walks that list with a plain `foreach` and an `EndsWith` comparison that ignores
case, so `Api:apikey` is treated the same as `Api:ApiKey`. When it matches, the value is
replaced with `(hidden, because this key holds a secret)` and the message still names the key
and the problem — which is what a reader actually needs.

The comment on that field is worth reading as an example of the mindset: today no real secret
can reach a message here, because the only checks that run on `Api:ApiKey` fire when the value
is missing or blank. **That is luck, not a safeguard** — the day someone adds a length or format
check on the key, the message would carry the key into the build output. The guard closes that
door up front.

#### The four options classes

All four are plain settings holders: properties only, no attributes, no behaviour, readable
without knowing any framework.

| File | Properties |
| --- | --- |
| `WebDriverOptions.cs` | `Browser`, `Headless`, `WindowWidth`, `WindowHeight`, `ElementTimeoutSeconds`, `PageLoadTimeoutSeconds` |
| `ApiOptions.cs` | `BaseUrl`, `TimeoutSeconds`, `TenantId`, `ApiKey` |
| `SiteOptions.cs` | `BaseUrl`, `HomePageTitle`, `ContactPagePath`, `KnownPageSlug`, `MissingPageId`, `MinimumPageSize`, `MaximumPageSize` |
| `ArtefactOptions.cs` | `Directory` |

`SiteOptions` is the one to study when you reuse the template. Every property on it is a **fact
about the system under test that a scenario needs but must not hard-code**. The expected home
page title is live marketing copy; the known slug is live content; the missing page id is an
environment fact. Keeping them here means a copy change on the site is a one-line settings edit
rather than a change to a test. `ApiOptions.TenantId` and `ApiOptions.ApiKey` carry their own
comments saying they are placeholders and where the real values come from.
`ArtefactOptions.Directory` notes that a relative value is resolved against the repository root
while an absolute value is used as-is.

### 6.4 Support\

Four files: `WebDriverFactory.cs`, `ScenarioCorrelation.cs`, `ScenarioDriver.cs` and
`FailureArtefacts.cs`.

#### WebDriverFactory.cs — the driver setup you went looking for

A `static` class with one public method, `Create(WebDriverOptions)`. **This is where the browser
is built, and where headless mode is decided.**

First, a guard:

```csharp
if (string.Equals(options.Browser, ChromeBrowserName, StringComparison.OrdinalIgnoreCase) == false)
{
    throw TestConfigurationException.ForKey(
        "WebDriver:Browser",
        "only 'chrome' is supported by this template",
        options.Browser);
}
```

Anything other than `chrome` is refused with a message naming the key `WebDriver:Browser`.
Adding Edge or Firefox is a later, deliberate change — not something that half-works by
accident.

Then `BuildChromeOptions` assembles the Chrome arguments:

```csharp
ChromeOptions chromeOptions = new ChromeOptions();

if (options.Headless)
{
    chromeOptions.AddArgument("--headless=new");
}

string windowSize = "--window-size="
    + options.WindowWidth.ToString(CultureInfo.InvariantCulture)
    + ","
    + options.WindowHeight.ToString(CultureInfo.InvariantCulture);
chromeOptions.AddArgument(windowSize);
chromeOptions.AddArgument("--disable-gpu");

// Without this, driver.Manage().Logs.GetLog(LogType.Browser) comes back empty and the
// console.log artefact is useless on a failure.
chromeOptions.SetLoggingPreference(LogType.Browser, LogLevel.All);
```

Four things happen there:

- `--headless=new` is added **only when `options.Headless` is true**. That flag traces straight
  back to `WebDriver:Headless` in `appsettings.json`, so flipping that one value — or its
  environment variable — is the whole of running headed.
- `--window-size` comes from `WindowWidth` and `WindowHeight` (1920x1080 by default). It matters
  more than it looks: `PrimaryNavigationComponent` depends on which of the site's two navigation
  bars, desktop or mobile, is the displayed one at that width.
- `--disable-gpu` is a standard headless companion flag.
- `SetLoggingPreference(LogType.Browser, LogLevel.All)` is **required for the `console.log`
  failure artefact to contain anything**. Without it, `FailureArtefacts` writes an empty file.

Finally `ApplyTimeouts`:

```csharp
// Implicit wait stays at zero, deliberately. Mixing an implicit wait with the explicit
// waits in ElementWaits produces unpredictable total timeouts, and "is this element
// gone?" starts taking the implicit wait every single time.
driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(options.PageLoadTimeoutSeconds);
```

An **implicit wait** is a global instruction to Selenium to keep retrying every element lookup
for up to N seconds. It sounds helpful and is not: combined with the explicit waits in
`ElementWaits` it makes total timeouts unpredictable, and it makes every "has this gone away?"
check pay the full implicit wait before it can answer. Pinned at zero, on purpose.

**There is no driver download step anywhere in this file or this repository.** Selenium Manager
is built into Selenium 4.6 and above: it works out which driver the installed Chrome needs and
fetches it on first run. That is why no ChromeDriver package is pinned — see
[15](#15-key-design-decisions-and-the-reasoning).

#### ScenarioDriver.cs

**`IWebDriver` lifetime in one sentence:** an `IWebDriver` is a live, stateful connection to a
running browser process — it is not thread-safe and it must be explicitly shut down, so
something has to own exactly one per scenario and be responsible for ending it.

That something is this class. It is registered **scoped**, so the hook and every page object in
one scenario share the one instance, and the next scenario gets a fresh one.

Four members:

- `IsStarted` — a `bool` property, true once the browser exists. `FailureArtefacts` checks it
  before trying to capture anything.
- `Driver` — the live browser. Asking for it before `Start()` is a **wiring mistake, not a test
  failure**, so instead of a null reference it throws a message that tells you what to fix:
  that a `[BeforeScenario]` hook must call `Start()` first, and to check that the project has a
  `WebDriverHooks` class and registered `AddBrowserSupport()`.
- `Start()` — returns immediately if already started, otherwise calls
  `WebDriverFactory.Create(this.webDriverOptions)`.
- `Quit()` — the shutdown:

```csharp
this.startedDriver.Quit();
this.startedDriver.Dispose();
this.startedDriver = null;
```

**`Quit()` then `Dispose()`, and never `Close()`.** The distinction is operationally important:
`Close()` shuts the browser *window* but leaves the `chromedriver` process alive. A few hundred
orphaned driver processes will bring a build agent to its knees. `Quit()` ends every window and
the driver process with it.

#### ScenarioCorrelation.cs

One identifier per scenario, so evidence from different layers can be lined up.

```csharp
public const string HeaderName = "X-Correlation-Id";

public ScenarioCorrelation()
{
    this.CorrelationId = Guid.NewGuid();
}
```

A fresh `Guid` per instance, and the class is registered scoped, so every class in one scenario
sees the same id and the next scenario gets a new one. `CorrelationIdText` exposes it as a
string in the standard `"D"` format.

The same value goes two places: onto every outgoing HTTP request as the `X-Correlation-Id`
header, added by `RequestContextHandler`; and into `location.txt` in the failure-artefact
folder. That means a server-side log line and a screenshot can be matched to the same scenario
run after the fact. It is deliberately **not** part of the folder name — that is the scenario
title plus a timestamp, so the folders stay readable.

#### FailureArtefacts.cs

Writes everything you will wish you had when a UI scenario fails. Selenium has no trace viewer,
so a failed run leaves nothing behind unless the suite captures it.

Constructor takes `ScenarioDriver`, `ArtefactOptions` and `ScenarioCorrelation`. One public
method, `Capture(suiteName, scenarioTitle)`, returning the folder it wrote to — or `null` when
there was no browser to capture from:

```csharp
if (this.scenarioDriver.IsStarted == false)
{
    return null;
}
```

Then it asks `ArtefactPaths.ForScenario` for the folder and writes four files, each through its
own private method:

| File | Written by | Answers |
| --- | --- | --- |
| `screenshot.png` | `SaveScreenshot` — casts the driver to `ITakesScreenshot` with `as`, returns early if the cast fails | What did the user actually see |
| `page-source.html` | `SavePageSource` — `driver.PageSource` | Was the element absent, or present but hidden |
| `console.log` | `SaveConsoleLog` — `driver.Manage().Logs.GetLog(LogType.Browser)`, each entry as timestamp, level and message | Did the page's own JavaScript throw |
| `location.txt` | `SaveLocation` — URL, title, the correlation id, and a UTC capture timestamp | Where the browser really was |

**Each of the four writes is guarded separately**, and the reasoning is recorded in the file: a
partial capture is still a capture. The one most likely to throw is the screenshot — a browser
that has already crashed, or a disk with no space — and the one most likely to explain that
crash is `location.txt`, which is written last. Guarding the whole sequence once would mean
losing the other three artefacts on the very run that most needed evidence.

A write that fails is recorded in a fifth file, `capture-problems.txt`, by `NoteProblem`. If even
that cannot be written, the exception is swallowed — because this code runs while a scenario is
already failing, and anything thrown out of it would replace the real failure with a teardown
error and lose the actual story. `SaveConsoleLog` has its own inner guard too, catching
`WebDriverException` and writing the reason into the log file, because not every browser or grid
exposes the console log and a missing log must never break teardown.

**The security note at the top of this file is not boilerplate.** Page source and console logs
are raw application output: they can contain tokens, session identifiers, email addresses and
other personal data. Both are written **verbatim, with no masking pass**, and that is a recorded
decision rather than an oversight: against an anonymous public website there is nothing to mask,
and a masking pass that guessed would either throw away the text a reader needs or protect only
the shapes somebody thought of. The condition that ends the deferral is named — the first
authenticated or multi-tenant target — and it is a blocker for that change, not a follow-up.
Until then the control is the handling rule: never attach the folder to a public ticket, never
publish it unmasked from a build. See [12](#12-test-results).

### 6.5 Utilities\

Seven small files. The rule for this folder: shared helpers with **no domain knowledge and no
page knowledge**.

#### ElementWaits.cs — the one wait helper

The most important file in the repository after `WebDriverFactory`. Selenium does not wait for
anything by itself: it acts immediately and throws if the element is not ready. So **every wait
in the suite goes through this class.** There is no `Thread.Sleep` anywhere and no hand-rolled
polling loop anywhere.

It is constructed with an `IWebDriver` and a `TimeSpan` — the timeout comes from
`WebDriver:ElementTimeoutSeconds`, assembled by `CoreRegistration.CreateElementWaits`, so no
call site passes a timeout.

Five public methods:

| Method | Waits until |
| --- | --- |
| `WaitUntilVisible(By)` | One matching element is present and visible, then returns it |
| `WaitUntilClickable(By)` | One matching element is visible **and** enabled, then returns it. Use before a click: a visible but disabled button still throws |
| `WaitUntilFirstVisible(By)` | At least one match is visible, and returns the **first visible one** |
| `WaitUntilGone(By)` | Nothing matching is visible on the page any more |
| `WaitUntilTitleContains(string)` | The browser title contains the text, compared case-insensitively |

`WaitUntilFirstVisible` is the one to reach for whenever a page renders the same block twice —
such as a desktop and a mobile copy of the same navigation bar. A plain `FindElement` would
return the hidden copy and the click would fail with `ElementNotInteractableException`. That is
not hypothetical here: it is exactly why `PrimaryNavigationComponent` uses it for every lookup.

`CreateWait` builds the `WebDriverWait` and does one extra thing:

```csharp
// A single-page app can re-render between finding an element and using it. Ignoring
// these two means the wait retries inside its own bounded timeout instead of failing
// on the first blink. It does NOT make the wait longer.
wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
```

That is how stale elements are handled **once, here**, instead of with a `try`/`catch` scattered
through page objects.

**Why every `Until` lambda in the repository lives in this one file.** The house style bans LINQ
and lambdas with three narrow exceptions, and one of them is "a library API that accepts nothing
else". `WebDriverWait.Until` is exactly that. So every `Until` lambda is written once, here, and
each is one line delegating to a named private method:

```csharp
public IWebElement WaitUntilVisible(By locator)
{
    WebDriverWait wait = CreateWait();

    // code-style.md exception 2: WebDriverWait.Until takes a lambda and nothing else.
    IWebElement element = wait.Until(currentDriver => FindVisible(currentDriver, locator));
    return element;
}
```

The real work sits in five plain static helpers — `FindVisible`, `FindClickable`,
`FindFirstVisible`, `IsGone`, `TitleContains` — so the lambda stays one line and the logic is
something you can put a breakpoint on. Returning `null` from one of those tells `WebDriverWait`
to try again.

The lesson to carry forward: **add a method to this class rather than writing a new `Until`
lambda in a page object.** One place to read, one place to fix.

To be precise about the rule, because this is the point where it is easy to over-read: *`Until`*
lambdas live only here, but this is not the only lambda in the repository, and the other two
kinds you will meet are the same `code-style.md` exception 2.
`CoreRegistration.AddBrowserSupport` passes two container factory lambdas — the
`provider => ...` pair printed in §6.2 — because the `IServiceCollection` overload that defers
construction accepts nothing else, and `ApiRegistration` passes one to Refit's
`ConfigureHttpClient` for the same reason. So `services.AddScoped<X>(provider => ...)` in a
`[ScenarioDependencies]` factory is in-style; `elements.Where(element => element.Displayed)` in
a page object is not.

#### ArtefactPaths.cs

Works out where a scenario's failure artefacts go, and creates the folder. The shape:

```text
<repository root>\test-results\<suite>\<scenario title>-<yyyyMMdd-HHmmss>\
```

An absolute `Artefacts:Directory` is used as-is. A relative one — `test-results` by default — is
resolved against the repository root, which `FindRepositoryRoot` locates by walking up from
`AppContext.BaseDirectory` until it finds `QAAutomation.sln`:

```csharp
DirectoryInfo candidate = new DirectoryInfo(AppContext.BaseDirectory);

while (candidate != null)
{
    string solutionPath = Path.Combine(candidate.FullName, SolutionFileName);
    if (File.Exists(solutionPath))
    {
        return candidate.FullName;
    }

    candidate = candidate.Parent;
}

// No solution file above us. Rather than fail a teardown, write next to the assembly.
return AppContext.BaseDirectory;
```

The walk-up exists because a test assembly runs from `bin\Debug\net10.0`, so a plain relative
path would drop the evidence inside the build output where nobody thinks to look. If no solution
file is found it falls back to writing next to the assembly rather than failing a teardown.

#### FileNameSanitiser.cs

Turns a scenario title into something safe to use as a folder name. Scenario titles are
sentences — spaces, quotes, colons, slashes — none of which belong in a Windows path. Anything
outside `A-Z`, `a-z`, `0-9`, hyphen and underscore becomes a hyphen, and the result is capped at
80 characters so a long title cannot push the full path past the operating system limit. A `null`
or empty result becomes `"unnamed"`.

A plain `foreach` over the characters with a named `IsAllowed` helper, **no regex**, which is the
house style in miniature: nothing to learn before reading it, and a breakpoint can go on any
line.

#### TestIdLocator.cs

Builds a locator from a `data-testid` attribute:

```csharp
public static By ByTestId(string testId)
{
    string cssSelector = "[" + TestIdAttributeName + "='" + testId + "']";
    return By.CssSelector(cssSelector);
}
```

**It is deliberately unused in this template, and that is worth understanding rather than
deleting.** The steering rules make `data-testid` the first-choice locator strategy, because a
test id is put there for testing and will not move when a designer renames a class. But the
system under test is a third-party WordPress site: it has no test ids and we cannot add any. So
the page objects fall back to the most stable selectors that actually exist there, and every one
of them carries a note saying so.

This class ships anyway because on a product we own, `data-testid` becomes the primary strategy
on day one, and this is the helper to use. When you reuse the template, this file goes from
unused to used on the first page object you write.

#### UrlHostChecker.cs

```csharp
public static bool HasSameHost(string expectedBaseUrl, string candidateUrl)
```

Parses both URLs with `Uri.TryCreate`, returns `false` if either fails to parse, and otherwise
compares hosts case-insensitively.

It guards the end-to-end hand-off. An end-to-end scenario reads a link out of an API response
and then opens it in a browser. **That link is data returned by a server, so it is untrusted
input** — exactly like a value typed into a form. If it ever pointed somewhere else, the suite
would drive a real browser to a site nobody intended to visit, on a machine that may hold
credentials. The step checks the host first and fails with a clear message instead of
navigating.

#### HeaderValueReader.cs

```csharp
public static string ReadFirstValue(HttpResponseHeaders headers, string headerName)
```

An HTTP header can legally appear more than once, so the framework hands back a collection.
Nearly every header the suite cares about has one value, so this takes the first via
`TryGetValues` and a `foreach` with an immediate `return`, and returns `null` when the header is
absent. That keeps "header missing" a plain null check in a step rather than an exception.

Its comment adds the rule that matters: never pass an `Authorization` header or a cookie through
here into a log or an assertion message.

#### PositiveIntegerParser.cs

```csharp
public static bool TryParsePositive(string value, out int result)
```

Returns `false` for `null`, for anything that does not parse as an integer, and for zero or
below. `InvariantCulture` on purpose, so a build agent in another locale reads `"18"` the same
way.

Used on pagination headers, and the reason is the most transferable lesson in the template: a
count header is text on the wire, and the suite asserts that it **parses to a positive whole
number** rather than asserting a specific count, because the content of a live site changes
without notice. See [10](#10-testsqaautomationtestsapi).

### 6.6 TestData\ — deliberately absent

`src\QAAutomation.Core\TestData\` does **not** exist, and neither is `Bogus` pinned in
`Directory.Packages.props`. That is deliberate, not an omission: the current system under test
is a read-only public website, every request is a GET, and no scenario creates data, so there is
nothing for a test data builder to build. An empty folder would be noise.

The conventions for it are already written down — the folder arrives with the first
`*Builder.cs`, which is also when `Bogus` gets pinned — and the steering globs already include
`**/TestData/**/*.cs` and `**/*Builder.cs` so the rules apply the day it appears.

---

## 7. src\QAAutomation.Api\

The HTTP layer. References `Core` only. No test runner, no assertions, no test data generation.
The `.csproj` takes four packages — `Refit`, `Refit.HttpClientFactory`,
`Microsoft.Extensions.Http` and `Microsoft.Extensions.DependencyInjection.Abstractions` — all
unversioned, and one project reference to Core.

### What Refit is, and how a typed interface becomes a working HTTP client

**Refit turns an interface into HTTP code at build time.** You declare an interface whose
methods carry attributes describing the request — `[Get("/pages")]` — and Refit's **source
generator** writes the class that actually builds the URL, sends the request and deserialises
the response. A source generator is a compiler plug-in: it produces real C# during the build (in
`obj\`), so the generated client is normal compiled code you can step into, not run-time
reflection.

What you get from that: the contract is **checked at compile time**. Rename a method or change a
parameter type and the build breaks. Nobody in this repository joins strings together to build a
URL.

### ApiRegistration.cs

One extension method, `AddApiClients(this IServiceCollection services, ApiOptions apiOptions)`.
It guards its input — `ArgumentNullException` on a null options object, `ArgumentException` on a
blank base URL — then does four things:

```csharp
services.AddTransient<RequestContextHandler>();

// The ConfigureHttpClient lambda is one of the three allowed lambdas in
// code-style.md: the API accepts nothing else.
services.AddRefitGeneratedClient<IWordPressApi>()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = baseAddress;
        client.Timeout = timeout;
    })
    .AddHttpMessageHandler<RequestContextHandler>();

services.AddScoped<PagesClient>();
```

**`AddRefitGeneratedClient<T>()`, not `AddRefitClient<T>()`.** This is the single most likely
trap in the whole template. `AddRefitClient<T>()` compiles fine and then fails at **run time**
asking for the separate `Refit.Reflection` package, which this repository deliberately does not
pin. The generated variant uses the client Refit source-generated at build time, which is what
we want anyway.

**The `HttpClient` comes from `IHttpClientFactory`.** That is what `AddRefitGeneratedClient`
registers against, supplied by `Microsoft.Extensions.Http`. No client creates its own
`HttpClient` and none holds a static one, so socket handling and the handler pipeline stay in
one place. `Transient` on the handler means a new one per request pipeline construction, which
is what `IHttpClientFactory` expects.

The base address and the timeout both come from `ApiOptions`, so pointing the suite at another
environment is a settings change rather than a code change. `BuildBaseAddress` normalises the
configured URL by trimming whitespace and a trailing slash, and the comment explains why that
matters more than it looks: the configured base URL has a path of its own (`.../wp-json/wp/v2`)
and every request path on the Refit interface starts with a slash, so the two are joined as
`base` + `/pages`. A trailing slash on the base would make that `.../v2//pages`, and a doubled
slash is a 404 that looks like a product fault.

### Clients\

#### IWordPressApi.cs — the typed contract

Three methods, all `[Get]`, all returning `Task<IApiResponse<T>>`:

```csharp
[Get("/pages")]
Task<IApiResponse<List<WordPressPageResponse>>> GetPagesAsync(
    [AliasAs("per_page")] int perPage,
    [AliasAs("_fields")] string fields,
    CancellationToken cancellationToken);

[Get("/pages")]
Task<IApiResponse<List<WordPressPageResponse>>> GetPagesBySlugAsync(
    [AliasAs("slug")] string slug,
    [AliasAs("_fields")] string fields,
    CancellationToken cancellationToken);

[Get("/pages/{pageId}")]
Task<IApiResponse<WordPressPageResponse>> GetPageAsync(
    int pageId,
    [AliasAs("_fields")] string fields,
    CancellationToken cancellationToken);
```

Two house rules are visible in that:

1. **Every method returns `IApiResponse<T>`, never a bare `T`.** A bare return type makes Refit
   throw on a 400 or a 404 — and this suite needs to inspect those, because a refusal is
   frequently the thing under test. With `IApiResponse` the status code, the headers and the
   error body all come back as **data**.
2. **Query-string names are spelled with `[AliasAs]`.** The API uses snake_case (`per_page`,
   `_fields`), the C# parameters use normal casing, and `AliasAs` is the bridge. No string
   concatenation, no manually built query strings. In the third method, `{pageId}` in the path
   is filled from the matching parameter name.

The base address is deliberately **not** here — it arrives from configuration through
`ApiRegistration`.

#### PagesClient.cs — the class steps inject

A wrapper around the Refit interface. Why have one at all, when the interface is already typed?
Because a step definition is not allowed to touch `HttpResponseHeaders`, a Refit type or a JSON
string. Reading a header and parsing an error body are transport concerns, so they happen here,
once, instead of in every step that needs them.

Three named constants at the top — `FieldSelection` is `"id,slug,title,link"`, and the two
pagination header names are `X-WP-Total` and `X-WP-TotalPages`. Asking for only four fields is
deliberate: a narrow request is cheaper for a production site and a narrow contract has less
surface to drift.

Three public methods, each `async Task` and `*Async`-suffixed:

| Method | Returns | Notes |
| --- | --- | --- |
| `ListPagesAsync(int perPage, CancellationToken)` | `PageListResponse` | An out-of-range page size is passed **straight through**, because the refusal it produces is something the suite asserts on. |
| `FindPagesBySlugAsync(string slug, CancellationToken)` | `PageLookupResponse` | Guards a blank slug with `ArgumentException`. Reports `MatchCount` and the first match. |
| `GetPageByIdAsync(int pageId, CancellationToken)` | `PageLookupResponse` | Guards an id of zero or below. A 404 comes back as **data**, not as an exception. |

Each one wraps the response in a `using` block, because `IApiResponse<T>` is disposable, then
fills a plain record. Two details show the house style doing real work:

```csharp
// Empty rather than null on a refusal, so a step can count the pages without
// first checking for null.
if (response.Content == null)
{
    listResponse.Pages = new List<WordPressPageResponse>();
}
else
{
    listResponse.Pages = response.Content;
}
```

and in `FindPagesBySlugAsync`:

```csharp
// An explicit loop with a break, not FirstOrDefault: take the first match and
// stop. See .kiro/steering/code-style.md.
foreach (WordPressPageResponse matchedPage in matchedPages)
{
    lookupResponse.Page = matchedPage;
    break;
}
```

Two private static helpers finish the class:

- **`ReadStatusCode`** — Refit reports the status code as a value that can be *missing*, because
  a request can fail before any server answers it: a wrong host name, a timeout, no network.
  Rather than invent a code and let a step report a confusing mismatch, it throws a plain
  message naming `Api:BaseUrl` and reachability as the things to check.
- **`ReadError`** — turns a refusal into a typed `WordPressErrorResponse`. `response.Error` is
  declared as the base exception type, and the body only exists on `ApiException`, so the cast
  is **checked** (`as`, then a null test) because `Error` is null on success and can be a
  transport failure carrying no body at all. The deserialise is wrapped in
  `catch (JsonException)` returning `null`, and the reason is worth remembering: the site sits
  behind a cache and an edge layer, so a 502 or 503 can arrive as an **HTML** error page.
  Parsing that would throw inside the client, the step would never reach its status-code
  assertion, and the output would show a JSON parsing error instead of "the site answered 503".

#### RequestContextHandler.cs — the DelegatingHandler pipeline

**What a `DelegatingHandler` is.** `HttpClient` sends a request through a chain of handlers, each
wrapping the next, with the one that actually talks to the network at the end. A
`DelegatingHandler` is one link in that chain: it can inspect or modify the request on the way
out, call the next link, and inspect the response on the way back. `.AddHttpMessageHandler<T>()`
in `ApiRegistration` is what inserts this one into the chain for the `IWordPressApi` client.

This handler does one thing — put the scenario's correlation id on every outgoing request:

```csharp
// Remove first, then add: a retried request already carries the header, and adding
// a second copy would send the value twice.
request.Headers.Remove(ScenarioCorrelation.HeaderName);
request.Headers.Add(ScenarioCorrelation.HeaderName, this.scenarioCorrelation.CorrelationIdText);

return base.SendAsync(request, cancellationToken);
```

A handler does this rather than each client method, so **no client can forget it and no step
ever has to think about a header**. The constructor takes `ScenarioCorrelation` and guards
against null. `base.SendAsync` is the call that passes the request down the chain.

Nothing here is logged. A request context can carry a token or a tenant identifier, and build
artefacts are read more widely than people expect.

The long comment block in this file is a **placeholder for KEYinfinity**, and it is the most
useful thing in the file for your purposes. It records that KEYinfinity is multi-tenant SaaS and
that tenancy must **not** be added here as an ambient header. Instead the tenant becomes a
required, explicit parameter on the client wrapper's methods, passed per request by the caller,
and it is **asserted on** — because the expensive bug class is tenant A seeing tenant B's data,
and a suite that resolves tenancy implicitly cannot detect that: every request would quietly be
correct. Whether the tenant travels as a header, a token claim or a path segment is still
unconfirmed, and that answer changes the design. The WordPress site has no tenancy at all, so
there is nothing to pass and nothing to assert, which is why this is a comment and not code.

### Contracts\

Five `record` types. A **record** is a C# class with concise syntax and value-like equality; for
reading purposes, treat them as plain data holders. All five have properties only and no
behaviour.

| File | Shape | Notes |
| --- | --- | --- |
| `WordPressPageResponse.cs` | `Id` (int), `Slug`, `Link`, `Title` (a `RenderedText`) | Only the four fields the suite asks for are modelled, because every request sends `_fields=id,slug,title,link`. The `[JsonPropertyName]` attributes are spelled out rather than relying on a naming policy, so the wire-name-to-property mapping is visible in one place. |
| `RenderedText.cs` | `Rendered` | WordPress does not return plain text for a title — it returns `{ "rendered": "Technical" }`. "Rendered" means *after the content filters have run*, which is why a shortcode or an HTML entity can appear in it. Modelling it as its own type keeps the mapping honest: a step asks for `page.Title.Rendered`, which reads the same way the JSON does. |
| `PageListResponse.cs` | `StatusCode`, `Pages`, `TotalCountHeader`, `TotalPagesHeader`, `Error` | The headers are kept **as text**, on purpose: whether a header is present and whether it parses are two separate things a test may want to say. |
| `PageLookupResponse.cs` | `StatusCode`, `Page`, `MatchCount`, `Error` | `MatchCount` is carried separately from `Page` because "exactly one page matched that slug" is a rule worth asserting, and it cannot be seen from `Page` alone. |
| `WordPressErrorResponse.cs` | `Code`, `Message` | `Code` is the part worth asserting on — it is the machine-readable contract and it does not change when someone rewords the message. `Message` is carried for the failure output only. |

The first two model the **wire format**. The last three exist so a step never touches
`HttpResponseHeaders`, an `HttpResponseMessage` or a Refit type: the client reads the transport
details once and hands back one of these.

---

## 8. src\QAAutomation.UI\

Page objects and page components. References `Core` only — no packages of its own at all, since
Selenium arrives transitively through Core. No test runner, no assertions.

### The page object pattern, and the rule about assertions

**A page object is a class that holds everything about how one screen is built:** how to find its
elements, and what you can do on it. Tests talk to the page object, so when the screen changes,
one file changes.

**A page object never asserts.** It returns what it sees; the step definition decides whether
that is right. The reason is practical: a page object that asserts can only serve a scenario
expecting that one answer. The moment a second scenario needs a different expectation, the page
object has to be copied or given a flag. Keep the judgement in the step and one page object
serves every scenario.

**Every method here is synchronous** — no `await`, no `Task`, no `*Async` suffix — because
Selenium's C# API is synchronous. There is nothing to await, and wrapping it in `Task.Run` to
look modern would add a thread and hide the stack trace.

One more pattern to notice: **a method that moves the user to another screen returns that
screen's page object**, so a step definition reads as a journey.

### UiRegistration.cs

One extension method, `AddPageObjects()`, registering all five classes as **scoped**:

```csharp
services.AddScoped<SubscribeModalComponent>();
services.AddScoped<PrimaryNavigationComponent>();
services.AddScoped<HomePage>();
services.AddScoped<LandedPage>();
services.AddScoped<ContactUsPage>();
```

Scoped means one instance per scenario, which matches the browser: a page object is always paired
with that scenario's own driver and never shared with the scenario running next to it.

### Pages\

#### HomePage.cs

Takes `IWebDriver`, `SiteOptions`, `PrimaryNavigationComponent` and `LandedPage` by constructor
injection. Four members:

- `Navigation` — exposes the shared header component.
- `Open()` — navigates to `SiteOptions.BaseUrl`, then calls `this.navigation.WaitUntilVisible()`
  so the page is genuinely usable before any step does anything else.
- `ReadTitle()` — returns `this.driver.Title`.
- `FollowNavigationLinkTo(string linkText)` — clicks the link and **returns the `LandedPage`**,
  so the step reads as a journey. `LandedPage` is injected rather than constructed here, because
  it holds no state of its own, so the scenario's single scoped instance is the right one to
  return — and a step can inject that same instance to read from.

Two comment blocks at the top are the ones to read. The **locator note** records that the
steering rules make `data-testid` the default, that this third-party WordPress site has none and
we cannot add any, and that the page objects therefore use the most stable selectors the site
actually offers. The **"what this page object deliberately does not expose"** note is the more
interesting one: the home page has a rotating hero carousel and animated statistic counters, and
neither is offered here, because what a carousel shows depends on how long the page has been open
and what a counter reads depends on how far its animation has run. An assertion on either would
pass or fail on **timing rather than behaviour** — a flaky test by construction. The carousel is
also why there is no "read the heading" method: the page carries four `h1` elements, one per
slide, so "the heading" is not a single thing on this page. The page title is server-rendered and
stable, so that is what identifies the page.

#### LandedPage.cs

"Wherever the navigation took us." Takes only `IWebDriver`. Three methods: `Open(string url)`,
`ReadTitle()` and `ReadCurrentUrl()`.

**Why it is so thin** is explained in the file: the site has a dozen marketing pages, and writing
a page object for each one just to read a title would be a lot of files proving nothing. This one
stands in for all of them so a navigation journey still reads as a journey — home page, follow a
link, check where we landed — without inventing a class per page. The moment a scenario needs to
do something specific on a specific page, that page gets its own page object and this one stays
as it is.

`Open(string url)` exists for the end-to-end suite, where the address comes from an API response
rather than a link. Two things about it:

- It guards a blank address with `ArgumentException`.
- **The caller is responsible for checking the address belongs to the site under test before
  calling it** — through `UrlHostChecker`. A page object navigates where it is told; deciding
  whether an address is trustworthy is not its job.
- It does **no wait**, because what counts as "loaded" depends on what the scenario is about to
  check, so the waiting belongs with that check.

#### ContactUsPage.cs

The enquiry page, and the most heavily commented file in the repository for a reason.

**It has no submit method, and none may be added.** The site is Landmark Systems' live,
customer-facing site and the form is their real sales enquiry form, so a successful submit does
not land in a test database — it creates a real sales enquiry that a real person has to read and
answer. There is also nothing to gain from it: the live form carries the HTML `novalidate`
attribute, which switches browser validation off completely, so there is **no client-side
validation to assert on**, and the only validation that exists runs on the server behind exactly
the submit we are not allowed to make.

**The absence is the control.** If the method does not exist, nobody can "just finish" the
scenario later by calling it, and a reviewer can see the rule has been kept by reading the file.

Structurally the class is a good model to copy. Three blocks of named constants: the eight
Gravity Forms field ids (`input_2_5` and friends — the plugin generates them from the form
definition in the shape `input_<formId>_<fieldId>`, which is what makes them the most stable
thing on the page), the eight human-readable field names a feature file uses (`"Email address"`),
and the attribute and property names it reads (`aria-required`, `value`). A `static readonly
FormField[]` table pairs each name with its locator, and a private `LocatorFor` walks it with a
`foreach` and throws a message **listing every field name it knows about** when a name does not
match. That error message is the difference between a five-minute fix and a confusing afternoon.

Four public methods:

| Method | Does |
| --- | --- |
| `Open()` | Navigates to `BaseUrl + ContactPagePath + "#form"` and waits until the first field is visible |
| `IsFieldMandatory(string fieldName)` | Reads the field's `aria-required` attribute via `GetDomAttribute`; an optional field simply does not carry it |
| `TypeInto(string fieldName, string value)` | Waits until clickable, then `Clear()` and `SendKeys(value)` |
| `ReadValueOf(string fieldName)` | Reads the DOM **property** `value` via `GetDomProperty` |

Three details in there are each worth a sentence:

- **The `#form` anchor on `Open()` is load-bearing.** The site reveals the form with a scroll
  animation, and until it scrolls into view its container sits at zero opacity — which Selenium
  correctly treats as not displayed, because a visitor cannot see it either. So a wait for the
  first field on the plain `/contact-us/` address times out after the full twenty seconds even
  though the fields are right there in the page source. Navigating to the page's own `#form`
  anchor, the same place its "COMPLETE FORM" button points, lands on the form section, the reveal
  runs, and the fields become visible. There is a second reason not to click that button instead:
  ten seconds after load a newsletter panel covers the window and a click underneath it would be
  refused, so jumping to the anchor means this page object never races that timer.
- **`aria-required` is the form's own published declaration** — it is also what a screen reader
  announces — so reading it is a read of the contract, not a guess, and it needs no submit.
- **`GetDomProperty("value")`, not the attribute.** The `value` *attribute* only shows the
  starting value from the HTML; the *property* is what the visitor actually typed.

### Components\

A component is a reusable page fragment built around its own root element, for markup that
appears on more than one page.

#### PrimaryNavigationComponent.cs

The header navigation bar that appears on every page. Two locators, built from named constants:
`nav.site-header__navigation` for the bar and `a` for links inside it. Takes `ElementWaits` and
`SubscribeModalComponent`.

**Why every lookup goes through `WaitUntilFirstVisible`** — this is the concrete payoff for that
method existing. The page contains **two** `nav.site-header__navigation` elements, a desktop copy
and a mobile copy, and at the 1920x1080 window the suite runs at only one of them is displayed. A
bare `FindElement` returns whichever comes first in the DOM, which may be the hidden one, and
clicking a link inside a hidden element fails with `ElementNotInteractableException`.
`WaitUntilFirstVisible` returns the first **displayed** match, which makes it deterministic.

Three public methods:

- `WaitUntilVisible()` — a `void` wait, nothing more.
- `IsVisible()` — returns state for a step to assert on.
- `FollowLink(string linkText)` — guards a blank name, finds the visible bar, dismisses the
  newsletter popup if it is up, then loops the links comparing trimmed text case-insensitively
  and clicks the first visible match.

**Why both `WaitUntilVisible()` and `IsVisible()` exist** is spelled out in the file, and it is a
nice piece of reasoning: `IsVisible()` returns state for a step to assert on, while
`WaitUntilVisible()` is only a wait. A page object that wants the wait should say so, rather than
calling `IsVisible()` and throwing the answer away — C# gives no warning for a discarded return
value, so that line would read as a no-op and is the kind of thing a reader deletes.

The popup dismissal is placed deliberately: **after** the navigation bar has been found and
**just before** the click, because waiting for the bar is the slow part and the popup can open
during that wait. The search is scoped to the visible navigation bar rather than the whole page,
so a link with the same wording in the footer cannot be clicked by mistake. And when no link
matches, the `NoSuchElementException` message says that link names are live website content and
gives the concrete gotcha: the contact link is labelled "Contact Sales", not "Contact Us".

#### SubscribeModalComponent.cs

The newsletter popup the site shows on its own, ten seconds after every page load. Three locators:
the open overlay (`div.modal-overlay.is-visible`), the overlay shell (`div.modal-overlay`) and the
close button, scoped to the open overlay.

**Why the class exists:** when the popup opens it covers the whole window, so any click made
later than about ten seconds after the page loaded is refused with
`ElementClickInterceptedException`. That is not a product fault and not a fault in the scenario —
it is a marketing popup racing the test. It is also **intermittent by nature**: a fast page gets
clicked before the timer fires and a slow one does not, so the same scenario passes on a quiet
machine and fails on a busy one.

Two public methods:

- `IsShowing()` — a **look, not a wait**. It answers about this instant and never blocks.
- `DismissIfShowing()` — returns immediately if nothing is showing; otherwise waits until the
  close button is clickable, clicks it, then waits until the overlay is gone.

Three decisions in this one small file that generalise to any similar problem:

- **Look before you wait.** Waiting for something that will usually never appear burns the full
  element timeout on every single click. `FindElements` returns an empty list rather than
  throwing, which is exactly what "is it there?" needs. Only once it knows there is something to
  wait for does it use the shared wait helper.
- **Then use the wait helper.** The overlay fades in, so the close button can be on the page a
  moment before it can actually be clicked.
- **Wait until it is gone before returning,** or the very next click would be intercepted by the
  popup on its way out.

Closing it once is enough: the site's own close handler writes a timestamp into local storage and
will not show the popup again for an hour, and each scenario gets a fresh browser, so the popup
appears at most once per scenario. And `DismissIfShowing` is deliberately **not** called from a
hook — a hook runs before the first page is even open, which is the one moment the popup can
never be up.

---

## Why the wiring files sit at each test project's root

Before the three test project sections, the question they all raise. Each test project root holds
a `*ScenarioDependencies.cs`, usually a `*Hooks.cs`, one or more `*Context.cs` classes, an
`AssemblyInfo.cs`, a `ContainerStartup.cs` and a `reqnroll.json`. Why not share them from `Core`?

Three reasons, and each is sufficient on its own:

1. **Reqnroll only discovers `[ScenarioDependencies]` and `[Binding]` inside a binding
   assembly.** A test project is a binding assembly; `Core` is not — it has no test runner
   reference at all. A factory or a hook class placed in `Core` would simply never be found.
2. **Each suite needs a different service graph.** The API suite must register no browser. A
   single shared factory would mean an `if` about which suite is running, and a shared hook class
   would start a browser for the API suite — destroying the separation that makes each suite
   honest about what it proves.
3. **A context holds that suite's own types.** `PageApiContext` holds `PageListResponse` from
   `QAAutomation.Api`; `CrossLayerContext` holds API-shaped values. Moving either into `Core`
   would force `Core` to reference `QAAutomation.Api` and invert the one-way dependency
   direction.

The **work** those files do still lives in `src\*`. `ScenarioDriver`, `WebDriverFactory` and
`FailureArtefacts` are in `Core\Support`; the registration extension methods are
`CoreRegistration`, `ApiRegistration` and `UiRegistration`. The files at each test project root
are thin shells that call them. That is why the UI and E2E suites have two near-identical
`WebDriverHooks.cs` files: two short files holding a suite name and an ordering, rather than one
shared file holding an `if` about which suite is running.

---

## 9. tests\QAAutomation.Tests.UI\

Eight scenarios across three feature files. Root contents: `QAAutomation.Tests.UI.csproj`,
`reqnroll.json`, `AssemblyInfo.cs`, `ContainerStartup.cs`, `UiScenarioDependencies.cs`,
`WebDriverHooks.cs`, `SiteNavigationContext.cs`, `EnquiryFormContext.cs`, plus `Features\` and
`Steps\`.

### QAAutomation.Tests.UI.csproj

Sets `RootNamespace` and `IsTestProject`. References `Core` and `UI` — **and must never
reference another test project**. Six packages, all unversioned:
`Microsoft.NET.Test.Sdk` (required for `dotnet test` discovery), `NUnit`, `NUnit3TestAdapter`
(despite the name, it supports NUnit 4.x), `Reqnroll.NUnit`,
`Reqnroll.Microsoft.Extensions.DependencyInjection` and `AwesomeAssertions`.

Then the settings file:

```xml
<None Include="..\..\src\QAAutomation.Core\Configuration\appsettings.json"
      Link="appsettings.json"
      CopyToOutputDirectory="PreserveNewest" />
```

Linked explicitly rather than relying on content flowing transitively from Core, so the settings
file lands in this project's output every time. All three test projects do this identically. It
is also the reason editing `appsettings.json` requires a rebuild — the copy next to the assembly
is what gets read.

### reqnroll.json

The runner's own configuration file. Currently one setting:

```json
{
  "language": {
    "feature": "en-GB"
  }
}
```

That tells Reqnroll the feature files are written in UK English. All three suites carry an
identical copy.

### AssemblyInfo.cs — what the parallelism attributes do

**Assembly-level attributes in one sentence:** an attribute written as
`[assembly: Something]` applies to the whole compiled test project rather than to one class, and
NUnit reads these two to decide how much runs at once.

```csharp
[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelism(2)]
```

- `Parallelizable(ParallelScope.Fixtures)` — fixtures may run in parallel with each other. A
  fixture here is one generated feature class, so **scenarios in different feature files** may
  run at the same time, while scenarios within one file stay in order.
- `LevelOfParallelism(2)` — at most two at once.

**Two, and not more, for two stated reasons.** Every scenario starts its own browser, so the
worker count is the number of Chrome processes running at once — that is memory, and on a build
agent it is the thing that falls over first. And the system under test is a third-party
production website: a test suite has no business putting load on someone else's live site.

One browser per worker, never shared: `IWebDriver` is not thread-safe, and the driver is
registered scoped so each scenario gets its own.

### ContainerStartup.cs

The most mysterious-looking file in the repository, which is why it carries the longest comment.

```csharp
private static readonly TimeSpan FirstResolutionTimeout = TimeSpan.FromSeconds(60);

[ModuleInitializer]
internal static void AllowTimeForTheFirstResolution()
{
    ObjectContainer.DefaultConcurrentObjectResolutionTimeout = FirstResolutionTimeout;
}
```

**The problem it solves.** Scenarios in different feature files run two at a time, so the files
start within milliseconds of each other. The first one to start runs the
`[ScenarioDependencies]` factory while the .NET runtime is still loading and compiling everything
that factory touches — configuration, the driver stack, the page objects. On a cold machine, or
on an agent that has just compiled the solution, that first call can take a little over a second.
Reqnroll's container lets a second thread wait only **one** second for that, then gives up with
`Concurrent object resolution timeout (potential circular dependency)`. There is no circular
dependency — the first call was simply slow — but the second feature fails in its set-up, and
only on the first run after a build. That is the worst kind of failure: it looks random, it never
reproduces on a second attempt, and it wastes an afternoon.

Raising the wait to a minute means nothing actually waits that long; a slow first start is simply
allowed to finish instead of being called a deadlock.

**Why `[ModuleInitializer]` and not a hook.** A `[ModuleInitializer]` method runs once when the
assembly is loaded, before any scenario. A `[BeforeTestRun]` hook would be the obvious home, but
a hook class is itself built by the container, so it runs too late to change how the container
waits.

This is also the one sanctioned exception to the "never BoDi" rule: the ban governs registration
and resolution, and nothing here registers or resolves anything with BoDi — it only changes a
start-up limit the runner exposes as a static property and offers no other way to set. The file
is duplicated in all three test projects rather than shared, because no test project may
reference another and the house style prefers plain duplication over a premature abstraction.
**Do not delete these files.**

### UiScenarioDependencies.cs

The one place this suite is wired up:

```csharp
[ScenarioDependencies]
public static IServiceCollection CreateServices()
{
    TestConfiguration testConfiguration = TestConfigurationLoader.Load();

    ServiceCollection services = new ServiceCollection();

    services.AddTestConfiguration(testConfiguration);

    // The browser, the one shared wait helper, failure-artefact capture and the
    // correlation id, all scoped so each scenario gets its own.
    services.AddBrowserSupport();

    services.AddPageObjects();

    services.AddScoped<SiteNavigationContext>();
    services.AddScoped<EnquiryFormContext>();

    return services;
}
```

Reqnroll calls this to build the services for a scenario, then hands step classes and hooks
whatever their constructors ask for. Four calls and two registrations — and that is the whole
wiring of the suite.

**Notice what is not here: no API client.** The UI suite cannot make an API call because nothing
in it knows how to. Cross-layer work belongs in the end-to-end suite, which registers both.

When you add a page object, a client or a context, this is the file that gets one new line.

### WebDriverHooks.cs

A `[Binding]` class — the attribute is what makes Reqnroll look inside it — holding the browser
lifecycle. `SuiteName` is the constant `"ui"`, which is what keeps this suite's evidence out of
the end-to-end suite's artefact folder. Constructor takes `ScenarioDriver`, `FailureArtefacts`
and `ScenarioContext`. The first two are scoped, so these are the **same instances** the steps
and page objects get; `ScenarioContext` is injected only to read runner metadata — the scenario
title and whether it failed.

```csharp
[BeforeScenario(Order = 10)]
public void StartBrowser()
{
    this.scenarioDriver.Start();
}

[AfterScenario(Order = 100)]
public void CaptureEvidenceThenQuitBrowser()
{
    try
    {
        if (this.scenarioContext.TestError != null)
        {
            this.failureArtefacts.Capture(SuiteName, this.scenarioContext.ScenarioInfo.Title);
        }
    }
    finally
    {
        this.scenarioDriver.Quit();
    }
}
```

Three things to take from that:

- **Explicit `Order` numbers** so sequencing is readable rather than incidental. A low number for
  infrastructure — it has to be in place before anything a scenario can see — and a high one for
  teardown.
- **Capture runs before the quit,** because the screenshot has to be taken while the browser is
  still open.
- **The quit sits in a `finally`,** so a capture that throws — a browser that has already crashed,
  a full disk — can never leave a `chromedriver` or Chrome process running. There is no early
  return anywhere in the method, for the same reason.

`this.scenarioContext.TestError != null` is the test for "did this scenario fail". Nothing is
captured on a pass.

### SiteNavigationContext.cs and EnquiryFormContext.cs

Typed scenario state. A `When` step puts what it saw in one of these and a `Then` step reads it.

| Context | Properties |
| --- | --- |
| `SiteNavigationContext` | `PageTitle`, `LandedUrl`, `NavigationVisible` |
| `EnquiryFormContext` | `FieldIsMandatory`, `ValueHeldByField` |

**Typed properties, not a `ScenarioContext` string bag.** The reason is stated identically in
every context in the repository: a typo in a property name fails the **build**, while a typo in a
dictionary key fails at run time with a confusing message. Both are registered scoped, so every
scenario starts with an empty one.

### Features\

| File | Tags | Scenarios |
| --- | --- | --- |
| `Homepage.feature` | `@ui @regression @marketing` | **The home page identifies the product in its page title** (`@smoke`); **The home page shows the primary navigation** |
| `PrimaryNavigation.feature` | `@ui @regression @marketing` | **The About Us navigation link opens the About Us page** |
| `EnquiryForm.feature` | `@ui @regression @marketing` | **The enquiry form declares which of its fields are mandatory** — a `Scenario Outline` with four rows; **The enquiry form holds the details the visitor types without submitting them** |

Eight scenarios in total, since the outline expands to four.

Three things to study in these files:

- **The tag line.** Every feature carries one layer tag (`@ui`), at least one suite tag
  (`@regression`, plus `@smoke` on the one scenario) and at least one feature-area tag
  (`@marketing`). Reqnroll turns each tag into an NUnit **category**, which is what makes
  `--filter "TestCategory=smoke"` work — you can see the `CategoryAttribute` entries in the
  generated `.feature.cs`.
- **The comment blocks.** `Homepage.feature` explains what it deliberately never asserts on and
  why; `EnquiryForm.feature` carries the read-only warning about the production form. A feature
  file is where coverage decisions belong, because that is what a business reader reads.
- **The `Examples` table is named after its technique.** `Examples: mandatory and optional fields
  (equivalence partitions)`, with four rows: two representatives of each partition — fields the
  form declares as required (`First Name`, `Email address`) and fields it declares as optional
  (`Company Name`, `Your Enquiry`) — rather than all eight fields, because a fifth required field
  would add runtime and no coverage. The `mandatory` column is the expected outcome, which is
  what makes the table a specification rather than a list of inputs.

### Steps\

Three classes, grouped by capability rather than by Given/When/Then.

| File | Injects | Bindings |
| --- | --- | --- |
| `HomepageSteps.cs` | `HomePage`, `SiteNavigationContext`, `SiteOptions` | `Given the visitor opens the Landmark home page`; `Then the browser page title is the expected home page title`; `Then the primary navigation is visible` |
| `PrimaryNavigationSteps.cs` | `HomePage`, `LandedPage`, `SiteNavigationContext`, `ElementWaits` | `When the visitor follows the {string} link in the primary navigation`; `Then the browser page title mentions {string}` |
| `EnquiryFormSteps.cs` | `ContactUsPage`, `EnquiryFormContext` | `Given the visitor opens the Landmark contact page`; `Then the "(.*)" field is mandatory: (true\|false)`; `When the visitor types {string} into the {string} field`; `Then the {string} field holds {string}` |

**All of these are plain `void` methods**, because Selenium's C# API is synchronous. There is no
locator, no driver and no `WebDriverWait` anywhere in these three files.

Four patterns worth copying:

- **The expected value comes from configuration, not from a literal.** In `HomepageSteps`:

  ```csharp
  this.siteNavigationContext.PageTitle = this.homePage.ReadTitle();

  this.siteNavigationContext.PageTitle.Should().Be(
      this.siteOptions.HomePageTitle,
      "a visitor arriving from a search result reads the page title first, so it has to name the product");
  ```

  The `because` reason names the business rule, so the failure message explains itself.
- **`Given` never asserts.** `GivenTheVisitorOpensTheLandmarkHomePage` just calls `Open()`. A
  failing precondition is a set-up error, not a test failure, so it is left to throw.
- **The step wording is reused verbatim across features.** `PrimaryNavigation.feature` reuses
  `Given the visitor opens the Landmark home page`, and the binding stays in `HomepageSteps`.
  Two methods matching the same sentence is an ambiguous-step failure, and Reqnroll is right to
  refuse it.
- **`ElementWaits` is injected into a step when a step genuinely needs to wait.**
  `PrimaryNavigationSteps` calls `this.waits.WaitUntilTitleContains(expectedTitlePart)` before
  reading the new page's title. That is still the one shared wait helper — the same one the page
  objects use — so there is no `WebDriverWait` and no locator in the file. Without the wait the
  assertion would race the page load and fail roughly one run in twenty, which is the kind of
  failure nobody ever gets to the bottom of.

One more detail: `EnquiryFormSteps` uses a regular expression rather than the usual `{string}`
shorthand for the mandatory check, because the second value is a `true`/`false` outcome read from
the `Examples` table — the `(true|false)` group reads clearly in the feature file and refuses
anything else.

---

## 10. tests\QAAutomation.Tests.Api\

Nine scenarios across two feature files, and the fastest suite by a wide margin — seconds, not
minutes. Root contents: `QAAutomation.Tests.Api.csproj`, `reqnroll.json`, `AssemblyInfo.cs`,
`ContainerStartup.cs`, `ApiScenarioDependencies.cs`, `PageApiContext.cs`, plus `Features\` and
`Steps\`.

**There is no `*Hooks.cs` in this project, and that is correct.** Configuration is loaded in the
dependencies factory, and there is nothing to start or tear down — no browser, no database, no
stub.

### QAAutomation.Tests.Api.csproj

Same shape as the UI project's, with the same six packages and the same linked
`appsettings.json`. The difference is in the project references and the comment on them:
references `Core` and `Api`, and **must never reference the UI project**, because the API suite
starts no browser at all.

### AssemblyInfo.cs

Identical attributes to the UI suite:

```csharp
[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelism(2)]
```

The reason is different, though, and the comment says so: these are read-only GET requests, so
there is no browser memory argument here. Two workers because the system under test is a
third-party production website, and a test suite has no business putting load on someone else's
live site. **Raise this only against an environment we own.** That is the line to remember when
you point the suite at KEYinfinity — a fast API suite is one of the main wins available, and this
is the number to change.

### ContainerStartup.cs

A duplicate of the UI suite's file, for the reasons in [9](#9-testsqaautomationtestsui).

### ApiScenarioDependencies.cs

```csharp
[ScenarioDependencies]
public static IServiceCollection CreateServices()
{
    TestConfiguration testConfiguration = TestConfigurationLoader.Load();

    ServiceCollection services = new ServiceCollection();

    services.AddTestConfiguration(testConfiguration);
    services.AddApiClients(testConfiguration.Api);

    // One correlation id per scenario, put on every outgoing request by
    // RequestContextHandler so server-side logs can be matched to a scenario run.
    services.AddScoped<ScenarioCorrelation>();

    services.AddScoped<PageApiContext>();

    return services;
}
```

**Notice what is not here: no driver, no browser support, no page objects.** Suite separation
made structural instead of conditional.

**And notice what has to be added back.** `ScenarioCorrelation` is normally registered by
`AddBrowserSupport()`, which this suite does not call — but `RequestContextHandler` needs it, so
this factory registers it directly. That is a small, concrete illustration of why one shared
factory would not work: the two suites need genuinely different graphs, not the same graph with a
flag.

### PageApiContext.cs

Three properties: `RequestedPageSize` (kept so a `Then` step can compare against what was asked
for), `ListResponse` and `LookupResponse`. Registered scoped.

This is the class that makes the dependency rule concrete: it holds `PageListResponse` and
`PageLookupResponse`, both from `QAAutomation.Api`. Putting it in `Core` would force `Core` to
reference the Api project and reverse the arrow.

### Features\

#### PageListing.feature — `@api @regression @marketing`, seven scenarios

| Scenario | Tag | Shape |
| --- | --- | --- |
| Requesting the page list returns published pages | `@smoke` | plain |
| The page list never returns more pages than the requested page size | | plain |
| The page list reports how many pages exist in total | | plain |
| A page size inside the documented range is accepted | | `Scenario Outline`, 2 rows: `1`, `100` |
| A page size outside the documented range is rejected | | `Scenario Outline`, 2 rows: `0`, `101` |

Five scenario definitions, seven executed scenarios, because each outline expands to one per row.

**The coverage note at the top of this file is the single most important lesson in the template**,
and it is worth reading in full in the repository. In summary: the system under test is a live
production website whose content changes whenever someone at the company publishes or unpublishes
a page, and nobody tells the suite. So no scenario here asserts an exact number of pages. The
rules asserted are the ones the API promises and content cannot break — the shape of a returned
page, never returning more pages than were asked for, reporting a total that is a positive whole
number, and which page sizes are accepted or refused. A scenario asserting "18 pages exist" would
pass today and fail next week for no product reason.

**The two outlines are a worked example of boundary value analysis**, and of a deliberate
exception to the usual rule that an `Examples` table carries an expected-outcome column. The
accepted and rejected partitions are split into two outlines because their `Then` differs, so the
expected outcome lives in the `Then` rather than in a column that would read
`accepted, accepted`. A single table mixing the two would need a `Then` that branches on the data
— which is the `if`-in-a-step that one-behaviour-per-scenario exists to prevent. Four rows at
`0`, `1`, `100` and `101`: the two valid boundaries and one value either side.

#### PageLookup.feature — `@api @regression @marketing`, two scenarios

| Scenario | Tag |
| --- | --- |
| Looking up a published page by its slug returns one fully formed page | `@smoke` |
| Looking up a page identifier that does not exist is refused | |

The comment explains a split that is easy to get wrong. The slug `"technical"` **is** written in
the feature file, because it is the subject of the behaviour — and the same value is configured as
`Site:KnownPageSlug`, which is how the end-to-end suite reaches the matching web page, so if the
company renames that page both change together. The missing identifier is **not** written here:
it comes from `Site:MissingPageId`, because "an identifier that does not exist" is an environment
fact rather than part of the behaviour being specified.

### Steps\

Two classes. **Every `When` here is `async Task` and every call is `await`ed**, because Refit and
`HttpClient` are async-only. The `Then` steps are plain `void` methods, because by then the
response is already sitting in the context and there is nothing left to await.

#### PageListingSteps.cs

Injects `PagesClient` and `PageApiContext`. One named constant, `InvalidParameterErrorCode =
"rest_invalid_param"`, declared once rather than spelled inline in two assertions.

```csharp
[When("a page list is requested with a page size of {int}")]
public async Task WhenAPageListIsRequestedWithAPageSizeOf(int pageSize)
{
    this.pageApiContext.RequestedPageSize = pageSize;
    this.pageApiContext.ListResponse = await this.pagesClient.ListPagesAsync(pageSize, CancellationToken.None);
}
```

`{int}` is a Cucumber expression — Reqnroll converts the matched text to an `int` for you, so the
step never parses a string.

Five `Then` steps, and four of them teach something:

- **`ThenEveryReturnedPageCarriesAnIdentifierASlugATitleAndALink`** iterates with a plain
  `foreach`, not a LINQ chain, and asserts five things about each page, plus one on the
  collection itself (`returnedPages.Should().NotBeEmpty(...)`, which sits outside the loop),
  with a `because` on every one. The comment names the reason: the loop can be stepped through
  and a breakpoint can go on any line of it.
- **`ThenNoMorePagesAreReturnedThanWereRequested`** asserts `BeLessThanOrEqualTo`, never
  equality. The rule the API promises is an **upper bound**. Asserting equality would turn the
  last page of results, or a site with fewer pages than the page size, into a failure that has
  nothing to do with the behaviour under test.
- **`ThenThePaginationHeadersReportPositiveWholeNumbers`** uses
  `PositiveIntegerParser.TryParsePositive` on both header values and then groups both checks in
  one `AssertionScope`:

  ```csharp
  // One concern - "the totals are reported sensibly" - so both checks run and the
  // failure message names both, instead of stopping at the first.
  using (new AssertionScope())
  {
      totalCountIsPositive.Should().BeTrue(
          "the API must report how many pages exist as a positive whole number, whatever that number is today");

      totalPagesIsPositive.Should().BeTrue(
          "the API must report how many result pages exist as a positive whole number, whatever that number is today");
  }
  ```

  An `AssertionScope` collects every failure inside it and reports them together, instead of
  stopping at the first. Use it when related checks form a **single concern** — which is the
  alternative to splitting one concern across several `Then` steps.
- **`ThenThePageListRequestIsRejectedAsAnInvalidParameter`** asserts three things: the exact
  status code (`HttpStatusCode.BadRequest`, not "any 4xx"), that an error body came back at all,
  and that `Error.Code` is `rest_invalid_param` — the machine-readable part of the contract, which
  does not change when somebody rewords the message.

#### PageLookupSteps.cs

Injects `PagesClient`, `PageApiContext` and **`SiteOptions`** — the last one so that the
identifier known not to exist comes from configuration instead of being typed into a step. The
file states the general principle: an environment fact in a step body is the thing that makes a
suite unusable somewhere else.

`ThenExactlyOnePageIsReturned` asserts `MatchCount` is exactly 1, and the comment explains why
that is not a content count and therefore safe: a slug is unique within the site, so a search by
slug either finds one page or none. If it ever returns two, the site has a genuine problem.

`ThenThePageLookupReportsThatThePageWasNotFound` asserts both the `NotFound` status **and** that
`Page` is null — because a refused lookup must not hand back a page.

Note what is absent from both files: no URL, no header name, no JSON, no status-code literal.
`HttpStatusCode` gives the codes names, and the client owns the transport.

---

## 11. tests\QAAutomation.Tests.E2E\

One scenario, on purpose. Root contents: `QAAutomation.Tests.E2E.csproj`, `reqnroll.json`,
`AssemblyInfo.cs`, `ContainerStartup.cs`, `E2EScenarioDependencies.cs`, `WebDriverHooks.cs`,
`CrossLayerContext.cs`, plus `Features\` and `Steps\`.

### QAAutomation.Tests.E2E.csproj

The only project in the solution referencing **all three** of `Core`, `UI` and `Api`, because an
end-to-end scenario deliberately crosses the two layers. Same six packages, same linked
`appsettings.json`. Still must never reference another test project.

### AssemblyInfo.cs — the file with no attributes

This file contains **comments only**. There is deliberately no `[assembly: Parallelizable(...)]`,
and without one NUnit runs everything here serially.

Three reasons, from the file:

1. An end-to-end scenario is the most expensive thing in the repository — a real HTTP call **and**
   a real browser. Running several at once multiplies both against a third-party production
   website.
2. An end-to-end failure is the hardest kind to read. Serial means the test output, the browser
   console log and the captured artefacts all belong to one scenario, in order, with nothing
   interleaved.
3. The suite is small by design. End-to-end coverage is the top of the pyramid — a handful of
   journeys proving the layers join up — so there is nothing for parallelism to speed up.

If it ever grows enough to need parallelism, the instruction is to copy the attributes from the
UI suite and keep the worker count low for the same reasons.

Worth noting as a documentation habit: an absence is only a decision if it is written down.
Without this file, "no parallelism attribute" would read as an oversight.

### E2EScenarioDependencies.cs

```csharp
services.AddTestConfiguration(testConfiguration);

// This call also registers ScenarioCorrelation, which the API layer's outgoing
// request handler needs. That is why it is not registered again below: one
// registration, from the call that owns it. The API test project, which has no
// browser support, has to register it itself.
services.AddBrowserSupport();

services.AddPageObjects();

services.AddApiClients(testConfiguration.Api);

services.AddScoped<CrossLayerContext>();
```

**This is the only project that registers both layers.** The API suite has no browser and the UI
suite has no API client, which is how those two stay honest about what they are proving. An
end-to-end scenario is the one case where crossing the boundary is the point.

The comment about `ScenarioCorrelation` is the other half of the note in the API suite's factory:
here it arrives via `AddBrowserSupport()` and so is not registered again.

### WebDriverHooks.cs

A deliberate copy of the UI suite's hooks. The **only** difference is
`private const string SuiteName = "e2e"`, which keeps this suite's evidence in its own artefact
folder.

The file explains why it is a copy rather than a shared base class, and it is the clearest
statement of the layering rules in the repository: the house rules forbid one test project
referencing another, so it cannot be borrowed from the UI project; Reqnroll only discovers a
`[Binding]` class inside a test assembly, so moving it into `Core` would not work either — and if
it somehow did, it would start a browser for the API suite, which is exactly the separation being
protected. Test code is DAMP rather than DRY, and forty readable lines in two places beat an
abstraction invented for a second occurrence.

One behavioural note specific to this suite: the browser is started unconditionally even though
the scenario's first step talks to the API, not the browser. Starting it unconditionally keeps the
hook free of an `if`, and the whole point of an end-to-end scenario is that the browser is coming.

### CrossLayerContext.cs

The hand-off. Four properties: `ApiSlug`, `ApiTitle`, `ApiLink` and `BrowserTitle`.

The flow it supports: the `Given` asks the content API which page to look at and what its title
should say, and writes the answers here. The `When` reads the address out of here and opens it.
The `Then` compares what the browser showed against what the API reported.

### Features\PublishedPageRendersInBrowser.feature

`@e2e @smoke @marketing`, one scenario:

```gherkin
Scenario: A page published through the content API renders with a matching title in the browser
  Given the content API supplies the page with the configured known slug
  When the visitor opens that page in the browser
  Then the browser page title mentions the title the content API reported
```

The comment block names the pattern and tells you to copy it: this is **API-seeded UI
verification**. The API supplies the facts (which page, what title, what address), the browser is
the thing under test, and the assertion compares the two. Nothing is hard-coded between the two
halves, so the scenario follows the content rather than a snapshot of it.

It also states why an end-to-end test is justified here at all, which is the question to ask every
time: the API says page `technical` exists and is called "Technical", and the browser then proves
that page really renders with that name. An API test cannot see the rendered page. A UI test would
have to hard-code the page's name, which is the thing being checked. Everything about the API's own
behaviour is covered in the API suite, and everything about a page's own behaviour in the UI suite.
The rule is: **prefer the lowest layer that can prove it.**

### Steps\PublishedPageRendersInBrowserSteps.cs

Injects `PagesClient`, `LandedPage`, `CrossLayerContext`, `SiteOptions` and `ElementWaits`.

**This is the file that shows the sync/async split most clearly.** The `Given` makes an HTTP call,
so it is `async Task` and the call is `await`ed. The `When` drives the browser, and Selenium is
synchronous, so that call sits plainly on its own line with no `await` — there is no `Task` to
await. A step that mixes the two is `async` because of the HTTP half.

Three steps:

- **`GivenTheContentApiSuppliesThePageWithTheConfiguredKnownSlug`** — `async Task`. Calls
  `FindPagesBySlugAsync(this.siteOptions.KnownPageSlug, ...)`, then **guards rather than
  asserts**: a null page throws an `InvalidOperationException` whose message says the scenario
  cannot start and to check `Site:KnownPageSlug` because the page may have been renamed or
  unpublished. Nothing is asserted in a `Given` — a precondition that cannot be met is a set-up
  problem, not a test failure. Then it writes slug, title and link into the context.
- **`WhenTheVisitorOpensThatPageInTheBrowser`** — plain `void`. It checks the host before
  navigating:

  ```csharp
  bool linkIsOnTheSiteUnderTest = UrlHostChecker.HasSameHost(
      this.siteOptions.BaseUrl,
      this.crossLayerContext.ApiLink);

  if (linkIsOnTheSiteUnderTest == false)
  {
      throw new InvalidOperationException(
          "The content API returned an address on a different host, so the browser was not sent there. Expected the host of '" +
          this.siteOptions.BaseUrl + "' but the API returned '" + this.crossLayerContext.ApiLink + "'.");
  }

  // Selenium is synchronous, so this is a plain call on its own line with no await.
  this.landedPage.Open(this.crossLayerContext.ApiLink);
  ```

  The reasoning is worth internalising: that address came back in a server response, which makes
  it **untrusted input** — exactly like a value typed into a form. If it ever pointed at another
  host, the suite would drive a real browser to a site nobody intended to visit, on a machine that
  may hold credentials. A guard clause, not an assertion: a refusal to navigate is a safety stop,
  not a statement about the behaviour under test.
- **`ThenTheBrowserPageTitleMentionsTheTitleTheContentApiReported`** — waits via
  `this.waits.WaitUntilTitleContains(...)`, reads the title, then asserts with
  `ContainEquivalentOf`, which is a case-insensitive "contains".

**Why "mentions" and not "equals"** is the most instructive comment in the file, and it is the
contract rather than a shortcut. The two titles are different things: the API reports the page's
own name, which for the configured page is "Technical", while the browser shows the page's
search-engine title, which is "Technical - Landmark minimum recommended hardware requirements |
Landmark Systems". There is no uniform suffix to strip and no rule that turns one into the other,
because each page's search-engine title is written by hand. So equality is not assertable. What
the site really promises is that the page the API named is the page the browser rendered, and the
page's name appearing in the browser title is how that shows. Case is ignored for the same reason:
the casing of marketing copy is an editorial choice, not behaviour.

---

## 12. test-results\

The failure-artefact folder. It does **not** exist in this repository right now, and that is the
healthy state: artefacts are written only when a UI or end-to-end scenario **fails**.

### What gets written, and where

```text
d:\git\QAAutomation\test-results\<suite>\<scenario title>-<yyyyMMdd-HHmmss>\
```

`<suite>` is `ui` or `e2e`, taken from the `SuiteName` constant in each suite's
`WebDriverHooks`, so the two suites' evidence never mixes. The scenario title is passed through
`FileNameSanitiser`, so spaces and punctuation become hyphens and the name is capped at 80
characters. The timestamp is local time in `yyyyMMdd-HHmmss`. The whole path is built and created
by `ArtefactPaths.ForScenario`.

| File | What it answers |
| --- | --- |
| `screenshot.png` | What did the user actually see — the wrong page, an error banner, a dialog in the way |
| `page-source.html` | Was the element absent, or present but hidden, or present under a different name |
| `console.log` | Did the page's own JavaScript throw an error |
| `location.txt` | The URL and title the browser was really on, plus the scenario's correlation id and a UTC capture timestamp |
| `capture-problems.txt` | Only when one of the four above could not be written, with the reason |

`console.log` is only populated because `WebDriverFactory` calls
`SetLoggingPreference(LogType.Browser, LogLevel.All)` when it builds the browser. Without that
line the file would be written empty.

The driver's own log file is **deliberately not** captured. Enabling it means handing
`ChromeDriverService` a log path when the driver starts — before anyone knows whether the scenario
will fail — so the file is always written and then either copied or deleted. That is a temporary
file, its cleanup and a service object to dispose, all in the one place that must never throw, in
exchange for evidence about a failure mode that has not happened once against this site. The
condition for bringing it back is recorded: when a driver-level failure actually needs explaining,
or when the suite runs on a shared build agent where the driver is a plausible suspect.

### It is gitignored

`test-results/` and `TestResults/` are both in `.gitignore`, along with `*.trx`. It is build
output. Nothing in it is ever committed.

Nothing is written when a scenario passes, either — artefacts from green runs are storage cost and
they bury the one capture that matters. So an absent or empty folder after a run means everything
passed.

To send the evidence somewhere else, such as a build agent's artefact staging folder, override the
directory:

```powershell
$env:QAAUTOMATION_Artefacts__Directory = "C:\temp\qa-artefacts"
```

An absolute path is used as-is; a relative one is resolved against the repository root.

### The security warning

**Treat this folder as sensitive.** `page-source.html` and `console.log` are whatever the
application actually sent, written out word for word with **no masking pass**. Against this public
marketing site that is harmless — the whole page is published to the internet anyway — which is
why no masking step exists yet.

On a system with a login it would not be harmless. Page source and console logs routinely carry
session identifiers, tokens, email addresses and other personal data. So:

- Never attach an artefact folder to a public ticket without reading it first.
- Never publish it unmasked from a build, where artefacts are frequently more widely readable than
  people assume.
- **Writing the masking pass is part of pointing this suite at an authenticated target**, not a
  follow-up task. The deferral is recorded, and so is the condition that ends it: the moment the
  suite logs in, holds a token or touches a tenant, those two files start carrying credentials.

Nothing in the template logs an `Authorization` header or a cookie value, and the hooks log nothing
beyond the artefact folder path. That rule stands whatever is captured.

One operational check worth knowing after any UI run. A scenario that crashed without shutting its
browser down leaves a driver process behind, and a few hundred of those will bring a machine to its
knees. The shutdown runs in a `finally`, so this should print nothing:

```powershell
Get-Process chromedriver -ErrorAction SilentlyContinue
```

---

## 13. Execution flow: what happens when a UI scenario runs

Traced with the actual class and method at each hop. The example is the `@smoke` scenario in
`Homepage.feature`: **"The home page identifies the product in its page title"**.

**1. `dotnet test tests\QAAutomation.Tests.UI`**

The .NET SDK builds the project. Reqnroll's generator writes
`Features\Homepage.feature.cs`, which contains an NUnit fixture class `HomePageFeature` in
namespace `QAAutomation.Tests.UI.Features`, with one test method per scenario —
`TheHomePageIdentifiesTheProductInItsPageTitle` — carrying NUnit `CategoryAttribute` entries for
each Gherkin tag. `Microsoft.NET.Test.Sdk` plus `NUnit3TestAdapter` then discover and run it.

**2. `ContainerStartup.AllowTimeForTheFirstResolution()`**

Runs once as the assembly loads, via `[ModuleInitializer]` — before any scenario. Sets
`ObjectContainer.DefaultConcurrentObjectResolutionTimeout` to 60 seconds so a cold first
resolution is not mistaken for a circular dependency.

**3. `UiScenarioDependencies.CreateServices()`**

Reqnroll calls the `[ScenarioDependencies]` factory to build this suite's service graph.

**4. `TestConfigurationLoader.Load()`**

Reads `appsettings.json` from `AppContext.BaseDirectory`, then layers environment variables
prefixed `QAAUTOMATION_` on top — the environment wins. Validates every value and throws
`TestConfigurationException` naming the key if anything is wrong. Returns a `TestConfiguration`
holding `WebDriverOptions`, `ApiOptions`, `SiteOptions` and `ArtefactOptions`.

**5. The container is populated**

`CoreRegistration.AddTestConfiguration(...)` registers the configuration and the four options
objects as singletons. `CoreRegistration.AddBrowserSupport()` registers `ScenarioDriver`,
`ScenarioCorrelation`, `FailureArtefacts`, the `IWebDriver` factory and `ElementWaits` — all
scoped, so one set per scenario. `UiRegistration.AddPageObjects()` registers the five page objects
and components, scoped. Then `SiteNavigationContext` and `EnquiryFormContext`, scoped.

**6. Reqnroll opens a scope for the scenario**

This is what makes "scoped" mean "per scenario". `ScenarioCorrelation` is constructed on first use
with a fresh `Guid`.

**7. `WebDriverHooks.StartBrowser()` — `[BeforeScenario(Order = 10)]`**

Calls `ScenarioDriver.Start()`, which calls `WebDriverFactory.Create(webDriverOptions)`.
`Create` rejects any browser other than `chrome` with a `TestConfigurationException` naming
`WebDriver:Browser`. `BuildChromeOptions` adds `--headless=new` when `Headless` is true, plus
`--window-size=1920,1080`, `--disable-gpu` and
`SetLoggingPreference(LogType.Browser, LogLevel.All)`. `new ChromeDriver(chromeOptions)` starts
Chrome — Selenium Manager resolves and, on first run, downloads the matching driver.
`ApplyTimeouts` sets `ImplicitWait` to `TimeSpan.Zero` and `PageLoad` from
`PageLoadTimeoutSeconds`.

**8. `Given the visitor opens the Landmark home page`**

Reqnroll constructs `HomepageSteps`, injecting `HomePage`, `SiteNavigationContext` and
`SiteOptions`. Constructing `HomePage` resolves `IWebDriver` through the factory registration,
which calls `ScenarioDriver.Driver` — the browser from step 7. `ElementWaits` is built by
`CoreRegistration.CreateElementWaits`, wrapping that same driver with a timeout of
`ElementTimeoutSeconds`.

`HomepageSteps.GivenTheVisitorOpensTheLandmarkHomePage()` calls `HomePage.Open()`, which does two
things: `driver.Navigate().GoToUrl(this.siteOptions.BaseUrl)`, then
`PrimaryNavigationComponent.WaitUntilVisible()`.

**9. Through the shared wait helper**

`PrimaryNavigationComponent.WaitUntilVisible()` calls
`ElementWaits.WaitUntilFirstVisible(HeaderNavigation)`. `ElementWaits.CreateWait()` builds a
`WebDriverWait` with the configured timeout and calls `IgnoreExceptionTypes` for
`NoSuchElementException` and `StaleElementReferenceException`. The one permitted `Until` lambda —
`wait.Until(currentDriver => FindFirstVisible(currentDriver, locator))` — polls the private
`FindFirstVisible`, which calls `FindElements`, loops the matches with a `foreach` and returns the
first one whose `Displayed` is true, or `null` to make the wait retry.

Nothing on this path sleeps, and no page object contains a `WebDriverWait`.

**10. `Then the browser page title is the expected home page title`**

`HomepageSteps.ThenTheBrowserPageTitleIsTheExpectedHomePageTitle()` calls `HomePage.ReadTitle()`,
which returns `driver.Title` and asserts nothing. The step stores it on
`SiteNavigationContext.PageTitle` and asserts:

```csharp
this.siteNavigationContext.PageTitle.Should().Be(
    this.siteOptions.HomePageTitle,
    "a visitor arriving from a search result reads the page title first, so it has to name the product");
```

AwesomeAssertions does the comparing; the expected value comes from `Site:HomePageTitle`.
**The assertion is in the step, never in the page object.**

**11. `WebDriverHooks.CaptureEvidenceThenQuitBrowser()` — `[AfterScenario(Order = 100)]`**

If `scenarioContext.TestError` is not null, it calls
`FailureArtefacts.Capture("ui", scenarioContext.ScenarioInfo.Title)`. `Capture` returns `null`
immediately if `ScenarioDriver.IsStarted` is false; otherwise it asks
`ArtefactPaths.ForScenario` for the folder (walking up to `QAAutomation.sln`, sanitising the title
through `FileNameSanitiser`, creating the directory) and writes the four files through
`SaveScreenshot`, `SavePageSource`, `SaveConsoleLog` and `SaveLocation`, each guarded separately,
with `NoteProblem` recording any that failed.

Then, in the `finally`, `ScenarioDriver.Quit()` calls `Quit()` and `Dispose()` on the driver —
never `Close()`. The `finally` is what guarantees a capture failure cannot leak a browser process.

**12. Reqnroll disposes the scope**

The scoped instances go away. The next scenario gets a new browser, a new wait helper and a new
correlation id.

---

## 14. Execution flow: what happens when an API scenario runs

The example is the `@smoke` scenario in `PageListing.feature`: **"Requesting the page list
returns published pages"**, which runs the step `When a page list is requested with a page size
of 2`.

**1. `dotnet test tests\QAAutomation.Tests.Api`**

Same build and discovery path. The generated fixture is `PageListingFeature` in namespace
`QAAutomation.Tests.Api.Features`, and the method for this scenario is
`RequestingThePageListReturnsPublishedPages`.

**2. `ContainerStartup.AllowTimeForTheFirstResolution()`**

Identical to the UI suite's, from this project's own copy of the file.

**3. `ApiScenarioDependencies.CreateServices()` and `TestConfigurationLoader.Load()`**

Same configuration chain: `appsettings.json` from `AppContext.BaseDirectory`, then
`QAAUTOMATION_`-prefixed environment variables, which win. `Api:BaseUrl` is validated as an
absolute https URL and `Api:TimeoutSeconds` as a sensible timeout.

**4. `ApiRegistration.AddApiClients(testConfiguration.Api)`**

Guards the options object and the base URL, then:

- `BuildBaseAddress` trims whitespace and any trailing slash off `Api:BaseUrl`, so the base and
  the interface's leading-slash paths join cleanly.
- `services.AddTransient<RequestContextHandler>()` makes the handler available to the pipeline.
- `services.AddRefitGeneratedClient<IWordPressApi>()` registers the **source-generated** Refit
  client against `IHttpClientFactory` — not `AddRefitClient<T>()`, which would fail at run time
  demanding `Refit.Reflection`.
- `.ConfigureHttpClient(...)` sets `BaseAddress` and `Timeout` from `ApiOptions`.
- `.AddHttpMessageHandler<RequestContextHandler>()` inserts the handler into the
  `DelegatingHandler` chain for that client.
- `services.AddScoped<PagesClient>()` registers the wrapper steps inject.

Then `ScenarioCorrelation` and `PageApiContext`, both scoped. **No driver, no page objects.**

**5. Reqnroll opens a scope; `ScenarioCorrelation` gets a fresh `Guid`**

There is no `[BeforeScenario]` hook in this suite, because there is nothing to start.

**6. `When a page list is requested with a page size of 2`**

Reqnroll constructs `PageListingSteps`, injecting `PagesClient` and `PageApiContext`. Constructing
`PagesClient` resolves `IWordPressApi` — the generated client, with its configured `HttpClient`
from the factory.

```csharp
this.pageApiContext.RequestedPageSize = pageSize;
this.pageApiContext.ListResponse = await this.pagesClient.ListPagesAsync(pageSize, CancellationToken.None);
```

`async Task`, `await`ed.

**7. `PagesClient.ListPagesAsync(2, cancellationToken)`**

Calls `this.wordPressApi.GetPagesAsync(perPage, FieldSelection, cancellationToken)` inside a
`using`, where `FieldSelection` is `"id,slug,title,link"`.

**8. The typed interface call becomes an HTTP request**

Refit's generated implementation of `[Get("/pages")]` builds `GET /pages?per_page=2&_fields=...`
against the configured base address, mapping the parameters through their `[AliasAs]` names.

**9. `RequestContextHandler.SendAsync(...)` adds its header**

On the way out, the handler removes any existing `X-Correlation-Id` (so a retried request does not
carry two copies) and adds `ScenarioCorrelation.CorrelationIdText`, then calls `base.SendAsync` to
pass the request down the chain to the network. Nothing is logged.

**10. The response comes back as `IApiResponse<List<WordPressPageResponse>>`**

Status code, headers and any error body all arrive as **data**, because the interface returns
`IApiResponse<T>` rather than a bare `T`. A 400 or 404 does not throw.

**11. `PagesClient` reads the transport details once**

```csharp
PageListResponse listResponse = new PageListResponse();
listResponse.StatusCode = ReadStatusCode(response);
listResponse.TotalCountHeader = HeaderValueReader.ReadFirstValue(response.Headers, TotalCountHeaderName);
listResponse.TotalPagesHeader = HeaderValueReader.ReadFirstValue(response.Headers, TotalPagesHeaderName);
listResponse.Error = ReadError(response);
```

`ReadStatusCode` throws a plain, actionable message if the API never answered at all.
`HeaderValueReader.ReadFirstValue` pulls `X-WP-Total` and `X-WP-TotalPages`, returning `null` when
absent. `ReadError` checks `response.Error as ApiException`, and deserialises the body inside
`catch (JsonException)` so an HTML error page from the edge layer does not mask the status code.
`Pages` is set to an empty list rather than `null` on a refusal.

**12. The response lands in the context object**

The `When` step assigns the returned `PageListResponse` to `PageApiContext.ListResponse`. That is
the hand-off from `When` to `Then` — a typed property, not a `ScenarioContext` string key.

**13. The assertion in the step**

```csharp
this.pageApiContext.ListResponse.StatusCode.Should().Be(
    HttpStatusCode.OK,
    "a page list request made with a page size the API documents as valid is accepted");
```

then `ThenEveryReturnedPageCarriesAnIdentifierASlugATitleAndALink` reads
`ListResponse.Pages` and loops it with a plain `foreach`, asserting on `Id`, `Slug`, `Link`,
`Title` and `Title.Rendered`. The exact status code is asserted, never "any 2xx", and the client
asserted nothing at all.

**14. The scope is disposed**

No teardown hook, nothing to quit.

### Where the two flows differ

| | UI | API |
| --- | --- | --- |
| Sync or async | **Synchronous.** Page object methods return values directly; UI-only steps are plain `void`. Selenium's C# API is synchronous, so there is nothing to await. | **Async all the way down.** Client methods are `Task`-returning and `*Async`-suffixed; the `When` step is `async Task` and `await`s. Refit and `HttpClient` are async-only. |
| Hooks | `[BeforeScenario]` starts the browser, `[AfterScenario]` captures then quits | **No hooks at all.** Nothing to start or tear down |
| Waiting | Mandatory, through `ElementWaits`, because Selenium does not wait | None. A request either answers within `Api:TimeoutSeconds` or it does not |
| Failure evidence | Four files in `test-results\ui\...` | None. The response and the assertion message are the evidence |
| Cost | Tens of seconds per scenario; a Chrome process each | Seconds for the whole suite |

Three rules follow from that table, and they are the ones to carry to a new project:
`.Result`, `.Wait()`, `.GetAwaiter().GetResult()` and `async void` are banned everywhere — they
deadlock and swallow failures. A mixed step is `async Task` because of its HTTP half, with the
synchronous UI call plainly on its own line. And a UI page object never grows an `*Async` suffix
just to look modern.

---

## 15. Key design decisions and the reasoning

Each of these is a decision recorded in the repository's own steering files, with the reason that
was actually written down. None of it is invented here.

### Reqnroll over SpecFlow

SpecFlow is **unmaintained**. Tricentis removed the SpecFlow repository from GitHub in December
2024, and the last SpecFlow NuGet package is 3.9.74. Reqnroll is the maintained fork — forked in
January 2024, Cucumber-compatible, with a SpecFlow-compatible migration path. New work targets
Reqnroll; SpecFlow is recorded as "not an option", with "revisit when" set to **never**.

### Selenium over Playwright

Playwright is the stronger tool on paper. Selenium is the right tool here for one reason that
outranks the feature comparison: **the person who has to own and maintain this framework is not
yet familiar with Playwright.** A framework nobody on the team can confidently debug at 9am on a
release day is not an asset. The role's job description also lists Selenium as acceptable, so it
is a supported choice rather than a workaround.

The decision is recorded as deferred-by-choice rather than ruled out, revisitable when the team is
comfortable owning Playwright — and then as a deliberate migration, not a drive-by.

**The two costs, named honestly:**

| Cost | Consequence for this suite |
| --- | --- |
| **No auto-waiting.** Selenium acts immediately and throws if the element is not ready. | Explicit waits are **mandatory**, not a style preference. Every wait goes through `ElementWaits`. A bare `FindElement` on anything the app renders asynchronously is a defect waiting to happen. |
| **No trace viewer.** There is no Playwright-style timeline, DOM snapshot or network tab to open after a failure. | Diagnostics rely on what the suite captures itself: **screenshot + page source + browser console logs**, written to `test-results\` on failure. Those three had to be wired up on day one, because without them an intermittent CI failure is unexplainable. |

Everything in `ElementWaits` and `FailureArtefacts` exists because of those two rows. They are not
incidental plumbing — they are the price of the Selenium decision, paid in one place each.

### AwesomeAssertions over FluentAssertions

FluentAssertions v8 and above moved to an Xceed **commercial licence**: free for open-source and
non-commercial use only, paid per-seat for commercial use. This product is commercial, so taking
FluentAssertions 8+ would create a licence liability.

`AwesomeAssertions` 9.6.0 is a community Apache-2.0 fork with a near-identical API — which is why
the assertion code reads exactly like FluentAssertions code you may have seen elsewhere. The rule
is that `FluentAssertions` is not added to any project, directly or transitively, without a licence
decision. The fallback if a true drop-in is ever required is FluentAssertions **7.x**, which
remains open source.

### Selenium Manager, and the three packages deliberately not taken

Selenium Manager is built into Selenium 4.6 and above. It works out which driver the installed
browser needs and downloads it on first use. That removes a whole problem class — and three
packages people reflexively add:

| Not added | Why not |
| --- | --- |
| `Selenium.WebDriver.ChromeDriver` | Not needed. Pinning a ChromeDriver version reintroduces exactly the version-drift problem Selenium Manager solved: the browser updates, the pin does not, the suite goes red for no product reason. |
| `WebDriverManager` | Not needed, same reason. It predates Selenium Manager and now duplicates it. |
| `DotNetSeleniumExtras.WaitHelpers` | **Abandoned** — exactly one release, ever. It is the old `ExpectedConditions` helper removed from Selenium in v4. `ElementWaits` is written by hand instead. |

The one operational consequence: a machine needs **outbound network access the first time** it
runs a UI test, so Selenium Manager can fetch the driver. After that it is cached. On a
locked-down build agent, pre-seed that cache rather than adding a driver package back.

### Three separate test projects instead of one

Three projects — UI, API, E2E — so each runs, fails and is retried independently, and so that
suite separation is **structural rather than conditional**. The API project has no reference to
`QAAutomation.UI` and its service graph registers no browser, so it cannot start one by accident.
The end-to-end project is the only one registering both layers, and that is visible from its
`.csproj`.

Within a project, every feature is tagged with its layer, so CI can select by project first and
tag filter second. If the three were ever collapsed into one, tag-driven filters would become
mandatory rather than optional — a suite must never be able to pull in another suite's fixtures.

### The no-LINQ rule, and its three narrow exceptions

LINQ — `.Where(...)`, `.Select(...)`, `.FirstOrDefault(...)`, `from x in y` — is idiomatic C# and
is not used here. The reason is the audience: a `foreach` loop is readable by anyone who has
written any language at all, and you can put a breakpoint on any line of it and watch it run. A
chain of four extension methods reads as one line you cannot step into. Test code is read under
pressure, usually when a build has just gone red.

Three mechanical supports: `ImplicitUsings` is disabled so the SDK does not add `System.Linq`
silently; `.editorconfig` raises IDE0005 to a warning so a leftover `using` shows up; and
`Directory.Build.targets` greps for `using System.Linq` and warns, naming the file.

**The three exceptions, and nothing else qualifies:**

1. **An assertion predicate AwesomeAssertions requires** — for example
   `Should().Contain(line => line.BoxNumber == 1)`. One comparison. No chaining, no nesting.
2. **A library API that accepts only a lambda.** Three such APIs are used here, so a lambda in
   this repository is one of exactly three kinds: Selenium's `WebDriverWait.Until`, in
   `ElementWaits` and nowhere else; Refit's `ConfigureHttpClient`, in `ApiRegistration`; and the
   deferred-construction `IServiceCollection` overloads, in `CoreRegistration.AddBrowserSupport`
   (the two `provider => ...` factories). Anything else accepting a lambda needs a reason.
3. **One `.ToList()` or `.ToArray()` at a boundary**, when an `IEnumerable<T>` must be
   materialised before it can be counted or indexed.

Anything beyond those goes into a named method in `Core\Utilities`. The loop still exists, but it
exists once, behind an intent-revealing name. And the rule that settles arguments: if a reviewer
and an author disagree about whether a case is a genuine exception, **the loop wins** — the bar is
"the library will not compile without it", not "the chain is nicer".

### Implicit wait pinned at zero

`WebDriverFactory.ApplyTimeouts` sets `ImplicitWait` to `TimeSpan.Zero` on purpose. Mixing an
implicit wait with explicit waits produces unpredictable total timeouts, and "is this element
gone?" starts taking the full implicit wait every single time. All waiting is explicit, bounded,
and in one file.

### Thread.Sleep banned outright

A sleep pauses the test for a fixed time. Selenium does not wait for anything by itself, and a
sleep does not fix that — it only moves the race. Too short and it still fails; too long and
every run pays for it; and the day the site is slow it fails anyway.

Also banned alongside it: `Task.Delay`, "just to be safe" pauses, hand-rolled `while` loops
polling `Displayed`, a `try`/`catch` around a `FindElement` to retry it, and inflating the global
timeout to mask a race. There is no sleep and no hand-written polling loop anywhere in the
repository — every wait goes through `ElementWaits`, which returns the moment its condition is
met, so a slow page costs a second rather than a fixed ten.

### Two smaller decisions worth knowing

- **`data-testid` is the default locator strategy everywhere except here.** The page objects in
  this template use the site's own class names, fixed form field ids and link text, because the
  system under test is third-party markup we cannot change. That is a documented exception written
  at the top of **every** page object so nobody copies it onto a system where we do own the
  markup, and `TestIdLocator` ships unused for that day.
- **No scenario asserts a live content count.** The API reports a total number of published pages
  in a header. That number changes whenever someone publishes a page, and nobody tells the test
  suite. A test asserting today's number would go red for no product reason, somebody would "fix"
  it by editing the number, and after that happened twice nobody would trust a red build again. So
  the suite asserts the rule the API actually promises: the total is reported, and it is a positive
  whole number.

---

## 16. How do I actually run this

Windows PowerShell. Commands are separated with `;`, never `&&`.

### Prerequisites

Two, and nothing else:

```powershell
dotnet --list-sdks
Test-Path "C:\Program Files\Google\Chrome\Application\chrome.exe"
```

You want a `10.x` entry from the first and `True` from the second. The first command lists every
SDK installed, not just the one in use — this machine reports three (2.1.526, 8.0.425 and
10.0.301), and extra older entries are harmless. **There is no browser-driver install step** —
Selenium Manager handles it, which is the one thing most Selenium guides on the internet will tell
you otherwise.

### Restore, build, run everything

```powershell
cd d:\git\QAAutomation
dotnet restore
dotnet build
dotnet test
```

`dotnet restore` downloads the pinned packages. `dotnet build` compiles all six projects — you
want `Build succeeded` with no warnings. `dotnet test` runs all three suites: 18 scenarios, 9 API,
8 UI, 1 end-to-end.

The first run is the slowest: the UI tests start a real Chrome each time and talk to a real
website, so expect a couple of minutes overall. The API suite alone takes seconds. If the UI tests
fail on the very first run with a message about a driver, that is almost always the outbound-access
point above.

### Run one suite

```powershell
cd d:\git\QAAutomation
dotnet test tests\QAAutomation.Tests.Api
dotnet test tests\QAAutomation.Tests.UI
dotnet test tests\QAAutomation.Tests.E2E
```

### Filter by tag

Reqnroll turns each Gherkin tag into an NUnit **category**, so `@smoke` becomes
`TestCategory=smoke`:

```powershell
cd d:\git\QAAutomation
dotnet test tests\QAAutomation.Tests.UI --filter "TestCategory=smoke"
dotnet test --filter "TestCategory=smoke"
```

The second form runs the smoke subset across all three suites. The tags available today are
`@ui`, `@api`, `@e2e`, `@smoke`, `@regression` and `@marketing`.

### Run one feature file

The filter matches the **generated class name**, which comes from the `Feature:` line — not from
the file name — and it is case-sensitive. `Homepage.feature` starts with `Feature: Home page`, so
the class is `HomePageFeature` and `~Homepage` matches nothing at all.

```powershell
cd d:\git\QAAutomation
dotnet test tests\QAAutomation.Tests.Api --filter "FullyQualifiedName~PageListingFeature"
dotnet test tests\QAAutomation.Tests.UI --filter "FullyQualifiedName~HomePageFeature"
```

The six fixture names, verified against the generated `.feature.cs` files:

| Feature file | Generated fixture |
| --- | --- |
| `tests\QAAutomation.Tests.Api\Features\PageListing.feature` | `PageListingFeature` |
| `tests\QAAutomation.Tests.Api\Features\PageLookup.feature` | `PageLookupFeature` |
| `tests\QAAutomation.Tests.UI\Features\Homepage.feature` | `HomePageFeature` |
| `tests\QAAutomation.Tests.UI\Features\PrimaryNavigation.feature` | `PrimaryNavigationFeature` |
| `tests\QAAutomation.Tests.UI\Features\EnquiryForm.feature` | `EnquiryFormFeature` |
| `tests\QAAutomation.Tests.E2E\Features\PublishedPageRendersInBrowser.feature` | `PublishedPageRendersInTheBrowserFeature` |

Note the last one: the `Feature:` line reads "Published page renders in the browser", so the
fixture name includes **"InThe"** where the file name does not.

### Run a single scenario

The generated method name is the scenario title with the spaces and punctuation removed:

```powershell
cd d:\git\QAAutomation
dotnet test tests\QAAutomation.Tests.UI --filter "FullyQualifiedName~TheHomePageIdentifiesTheProductInItsPageTitle"
dotnet test tests\QAAutomation.Tests.Api --filter "FullyQualifiedName~RequestingThePageListReturnsPublishedPages"
```

Or spell the whole thing out, with the namespace and fixture:

```powershell
cd d:\git\QAAutomation
dotnet test tests\QAAutomation.Tests.UI --filter "FullyQualifiedName=QAAutomation.Tests.UI.Features.HomePageFeature.TheHomePageIdentifiesTheProductInItsPageTitle"
```

The namespace is the test project's root namespace plus `.Features`.

If you are unsure of a name, list what the runner found without running any of it:

```powershell
cd d:\git\QAAutomation
dotnet test tests\QAAutomation.Tests.UI --list-tests
```

### Run headed instead of headless — the answer to "why can't I see the browser?"

The browser is invisible because `WebDriver:Headless` is `true` in `appsettings.json`, and
`WebDriverFactory.BuildChromeOptions` turns that into the Chrome argument `--headless=new`.

**The environment variable is the switch to use**, because it needs no rebuild:

```powershell
cd d:\git\QAAutomation
$env:QAAUTOMATION_WebDriver__Headless = "false"
dotnet test tests\QAAutomation.Tests.UI --filter "TestCategory=smoke"
Remove-Item Env:\QAAUTOMATION_WebDriver__Headless
```

Three things about that:

- `QAAUTOMATION_` is the prefix, and the **double underscore** stands in for the `:` in
  `WebDriver:Headless`. The environment beats the JSON file, so nothing needs editing.
- The variable lasts for that PowerShell session only. `Remove-Item Env:\...` puts the shell back
  as it was, which matters because a forgotten override is a confusing afternoon later.
- Editing `appsettings.json` in `src\` also works, but only after a rebuild — the file is copied
  next to the test assembly and read from `AppContext.BaseDirectory`. That is why the variable is
  the better local switch.

Watch one scenario headed, which is what you usually want:

```powershell
cd d:\git\QAAutomation
$env:QAAUTOMATION_WebDriver__Headless = "false"
dotnet test tests\QAAutomation.Tests.UI --filter "FullyQualifiedName~TheHomePageShowsThePrimaryNavigation"
Remove-Item Env:\QAAUTOMATION_WebDriver__Headless
```

### Other overrides, same pattern

```powershell
$env:QAAUTOMATION_Site__BaseUrl = "https://www.landmarksystems.co.uk"
$env:QAAUTOMATION_Api__BaseUrl = "https://www.landmarksystems.co.uk/wp-json/wp/v2"
$env:QAAUTOMATION_Artefacts__Directory = "C:\temp\qa-artefacts"
$env:QAAUTOMATION_WebDriver__ElementTimeoutSeconds = "30"
```

Every key in `appsettings.json` works this way. A real credential would arrive the same way — for
example `QAAUTOMATION_Api__ApiKey` — and never as a committed value.

### After a run

```powershell
cd d:\git\QAAutomation
if (Test-Path test-results) { Get-ChildItem test-results -Recurse -File | Select-Object FullName } else { "No failures recorded." }
Get-Process chromedriver -ErrorAction SilentlyContinue
```

The first should say `No failures recorded.` after a green run. The second should print nothing.

### Checking the secret scanner

```powershell
cd d:\git\QAAutomation
git config core.hooksPath .githooks
git config --get core.hooksPath
powershell -NoProfile -File .\scripts\Find-StagedSecrets.ps1 -Scope Tracked
```

The first line is the one-off activation — it is **not** set in this clone right now, so until you
run it the pre-commit hook does nothing.

### There is no CI pipeline to run

CI wiring is **parked** at the user's request. There is no `pipelines\` folder and no YAML file
anywhere in this repository. The conventions for it are written down in
`.kiro\steering\ci-azure-devops.md` and cost nothing while parked, and the
`wire-suite-into-azure-pipeline` skill is the playbook for when it comes back.

---

## 17. Reusing this template on a new project

The whole point of the layout is that the things that change when you point at a new system under
test are few, obvious and concentrated. Work through these in order.

### What to change, in order

**1. `src\QAAutomation.Core\Configuration\appsettings.json`**

Start here, because the first three steps may be the only ones that touch code at all.

| Key | Change to |
| --- | --- |
| `Site:BaseUrl` | The new site's address. It is validated as an absolute **https** URL. |
| `Api:BaseUrl` | The new API's base address. Also validated as absolute https. Mind the trailing slash — `ApiRegistration.BuildBaseAddress` trims one, so either spelling works. |
| `Site:HomePageTitle`, `Site:ContactPagePath`, `Site:KnownPageSlug`, `Site:MissingPageId`, `Site:MinimumPageSize`, `Site:MaximumPageSize` | Facts about the new system. Replace the ones that still apply, delete the ones that do not. |
| `Api:TenantId`, `Api:ApiKey` | Keep them as **placeholders**. If the new API needs real values, they arrive as `QAAUTOMATION_Api__TenantId` and `QAAUTOMATION_Api__ApiKey` at run time. Never commit a real one. |
| `WebDriver:*` | Usually unchanged. Review `WindowWidth`/`WindowHeight` if the new app has a responsive breakpoint that matters, and `ElementTimeoutSeconds` only with evidence that an operation is legitimately slow. |

**2. `SiteOptions.cs`, and `TestConfigurationLoader` with it**

`SiteOptions` is the typed shape of the `Site` section, so it changes alongside the JSON: add a
property for each new fact, remove the ones that no longer apply. Then update
`TestConfigurationLoader.ReadSiteOptions` to read each one and validate it. Follow the existing
pattern — read through `ReadRequiredText` or `ReadWholeNumber`, then apply a validator that names
the key. The payoff is that a misconfigured suite tells you which setting is wrong instead of
failing twenty seconds later inside Selenium.

The test for whether something belongs in `SiteOptions`: **would a scenario have to hard-code it
otherwise?** Expected titles, known identifiers, documented limits and paths all belong here.

**3. The Refit interface and the contracts — `src\QAAutomation.Api\`**

- Replace `IWordPressApi.cs` with an interface for the new API, named `I*Api`. Keep the two house
  rules: every method returns `IApiResponse<T>` and never a bare `T`, and query-string and path
  names are spelled with `[AliasAs]` rather than built by concatenation.
- Replace the `Contracts\` records. Keep the split that makes them work: records modelling the
  **wire format** (like `WordPressPageResponse`), and records shaped for what a **step** needs
  (like `PageListResponse`), so a step never touches `HttpResponseHeaders` or a Refit type.
- Replace `PagesClient.cs` with a `*Client` wrapper. Its job stays identical: make one request,
  read the transport details once, hand back a plain record, assert nothing.
- Update `ApiRegistration.AddApiClients` to register the new interface and wrapper. Keep
  `AddRefitGeneratedClient<T>()` — not `AddRefitClient<T>()`.
- `RequestContextHandler` stays, but read its KEYinfinity comment block before you extend it. If
  the new API is multi-tenant, the tenant does **not** become an ambient header here; it becomes a
  required, explicit parameter on the client wrapper's methods, and it gets asserted on.

**4. The page objects and components — `src\QAAutomation.UI\`**

- Replace `HomePage.cs`, `ContactUsPage.cs` and the two components with page objects for the new
  screens. Keep `LandedPage.cs` if a navigation journey still makes sense; it is deliberately
  generic.
- **Switch the locator strategy back to `data-testid`.** This is the big one. Every page object in
  this template carries a note explaining that the third-party site has no test ids and we cannot
  add any. On a product you own, that exception ends: use `TestIdLocator.ByTestId(...)` — the
  helper already ships in `Core\Utilities`, unused, for exactly this day.
- Agree the `data-testid` contract with the front-end developers as a contract, not a favour.
  Selenium has no role or label locator API, so there is no accessible-name fallback: if the ids
  are not there, the only option left is structural selectors, which is the failure mode the whole
  strategy exists to avoid. A missing id is a blocker to escalate, not a reason to write XPath.
- Keep the shape: locators as `private static readonly By` fields built from named constants,
  intent-level methods, `ElementWaits` by constructor injection, no stored `IWebElement` fields,
  no assertions, synchronous throughout.
- Update `UiRegistration.AddPageObjects()` with one line per new class.

**5. The feature files and steps — `tests\*\Features\` and `tests\*\Steps\`**

- Replace the `.feature` files. The structure stays; the content does not. Keep the tag
  discipline: one layer tag, at least one suite tag, at least one feature-area tag — and update
  the taxonomy table in `bdd-gherkin-standards.md` **before** inventing a new area tag, which is
  what `@marketing` was added for.
- Replace the `*Steps.cs` classes, grouped by capability rather than by Given/When/Then. Keep the
  thin-step rule: one or two calls then an assertion, with a `because` reason naming the business
  rule.
- Replace the `*Context.cs` classes with typed contexts for the new capabilities, and register
  them in that suite's `*ScenarioDependencies.cs`.
- Delete the scenarios you are not replacing. A template's example scenarios are worth more as a
  model than as coverage.

**6. Revisit the parallelism numbers**

Both `AssemblyInfo.cs` files cap at two workers because the system under test is a third-party
production website. Against an environment you own, that cap is the obvious win — and the comment
in the API suite's file says so explicitly. Raise it deliberately, and keep an eye on memory for
the UI suite, where each worker is a Chrome process.

**7. Write the masking pass before the first authenticated run**

If the new system has a login, `FailureArtefacts` starts capturing page source and console logs
that can carry tokens and personal data. That masking pass is **a blocker for the migration, not a
follow-up** — it is the condition the deferral was recorded against. See [12](#12-test-results).

**8. Activate the reserved packages you actually need**

A database means `Testcontainers.PostgreSql`, `Npgsql` and `Respawn`. Scenarios that create data
mean `Bogus` and the arrival of `Core\TestData\`. A third-party boundary means `WireMock.Net`. An
asynchronous back-end state to settle on means `Polly` — for API polling only, never to paper over
a UI race. Each one is already chosen with a version and a reason in `tech-stack.md`; activating
it means **pinning it in `Directory.Packages.props` at that version**, not at whatever is latest
that day.

### What to leave alone

| Keep as-is | Why |
| --- | --- |
| `src\QAAutomation.Core\Utilities\` | `ElementWaits`, `ArtefactPaths`, `FileNameSanitiser`, `TestIdLocator`, `UrlHostChecker`, `HeaderValueReader`, `PositiveIntegerParser` — all seven have no domain or page knowledge. They work unchanged against any system. `TestIdLocator` goes from unused to used. |
| `ElementWaits` specifically | Add a method when you need a new condition; do **not** write a new `Until` lambda in a page object. One place to read, one place to fix. |
| `Core\Support\` — `WebDriverFactory`, `ScenarioDriver`, `FailureArtefacts`, `ScenarioCorrelation` | The browser lifecycle, the artefact capture and the correlation id are system-independent. `WebDriverFactory` only changes if you add a second browser, and that is a deliberate decision in its own right. |
| `Core\Configuration\` plumbing — `TestConfigurationLoader`'s readers and validators, `TestConfiguration`, `TestConfigurationException`, `WebDriverOptions`, `ArtefactOptions` | The chain and its error handling carry over. Only `SiteOptions` and `ApiOptions` are system-specific. |
| `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, `.editorconfig` | The build settings, the no-LINQ warning, the pinning model and the style rules are the framework's spine. Change a pin deliberately, with a decision recorded in `tech-stack.md`; do not rewrite the files. |
| `ContainerStartup.cs` in all three test projects | The first-resolution fix is not system-specific, and deleting it brings back an intermittent failure that only shows up on the first run after a build. All three copies stay. |
| The `*ScenarioDependencies.cs` structure | Replace the registrations, keep the one-factory-per-suite shape and the separation it enforces: no API client in the UI suite, no browser in the API suite, both only in E2E. |
| `WebDriverHooks.cs` in the UI and E2E suites | Only `SuiteName` is suite-specific. The order, the capture-then-quit sequence and the `finally` are correct as they stand. |
| `.githooks\pre-commit` and `scripts\Find-StagedSecrets.ps1` | Credential scanning is not project-specific. Copy both, and remember the one-off `git config core.hooksPath .githooks` in the new clone. |
| `.gitignore`, `.gitattributes` | The ignore list and the line-ending normalisation apply to any .NET repository with Reqnroll in it. |
| `.kiro\steering\` | The rules are the reason the code reads the way it does. Update the system-specific parts — the tag taxonomy, the domain glossary, the locator exception — and leave the rest. |

### The three-line summary

If you remember nothing else: **configuration and the contract change; the plumbing does not.**
Point `appsettings.json` at the new system, rewrite the Refit interface and the page objects
against the new contract and the new markup, replace the feature files — and leave `Core`, the
props files and the house rules exactly where they are.

And the rule that keeps a suite trusted, which is the real transferable lesson here: **assert the
rule the system promises, not the state it happens to be in today.** Every awkward-looking
decision in this template — "positive whole number" instead of eighteen, "mentions" instead of
"equals", `BeLessThanOrEqualTo` instead of `Be`, no assertion on a carousel — is that one rule
applied.








