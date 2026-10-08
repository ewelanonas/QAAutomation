# QA Automation template — C# BDD for UI, API and end-to-end

A starter kit for automated testing in C#, written so that someone coming from manual testing
can read it, run it and add to it on their first day.

Everything below uses UK English and explains each C# term the first time it appears.

---

## 1. What this is

This is a **template**, not a test suite for a real product.

Its job is to show the shape of a working automation framework: how a plain-English test is
written, where the code behind it lives, how the browser is started and stopped, how failures
are recorded, and what the house rules are. The tests in it are real and they pass, but they
are examples.

The website being tested is <https://www.landmarksystems.co.uk> — a public marketing site. It
stands in as a **placeholder** for KEYinfinity, the product this framework is really intended
for. When KEYinfinity is available, the website address changes in one settings file and the
tests themselves barely move.

Two things to hold on to:

- The placeholder site is **live production** and belongs to someone else. Every test here only
  reads. Nothing is ever created, changed or submitted.
- Because the content is live, **no test ever asserts a content count**. More on why in
  section 10.

### The words you will meet

| Word | What it means here |
| --- | --- |
| **Solution** | The whole codebase, described by `QAAutomation.sln`. Think of it as the folder the IDE opens. |
| **Project** | One buildable part of the solution, described by a `.csproj` file. This solution has six. |
| **Class** | A named block of C# code holding data and methods. Every `.cs` file here holds one. |
| **Method** | A named action a class can perform. The C# word for a function. |
| **NuGet package** | A reusable library downloaded from the internet. The equivalent of an npm or pip package. |

---

## 2. When to write a UI, an API or an end-to-end test

Three kinds of test live here, in three separate projects. Pick deliberately.

| Kind | What it drives | Use it when | Cost |
| --- | --- | --- | --- |
| **API** | An HTTP request straight to the server | The rule lives on the server: validation, status codes, data shape, permissions, boundaries | Fast, seconds, very stable |
| **UI** | A real Chrome browser | The rule is about what a person sees or can do: a page renders, a link goes somewhere, a field is mandatory | Slow, tens of seconds, needs waiting |
| **End-to-end** | Both, in one scenario | The rule is that two layers **agree** with each other, and neither layer on its own can show it | Slowest and hardest to diagnose |

The rule: **prefer the lowest layer that can prove it.**

If a server refuses a bad page size, that is an API test. Driving a browser to show the same
thing adds a minute of runtime and a new way to fail, and proves nothing extra.

The one end-to-end test in this template is a fair example of when the top layer earns its
keep: the content API says page `technical` exists and is called "Technical", and the browser
then proves that page really renders with that name. An API test cannot see the rendered page.
A UI test would have to hard-code the page's name, which is the thing being checked.

---

## 3. Directory map

```text
QAAutomation\
  Directory.Packages.props        every package version, pinned in one place
  Directory.Build.props           settings shared by all six projects
  Directory.Build.targets         the build-time warning that flags banned code
  .editorconfig                   formatting and code-style rules the IDE applies
  QAAutomation.sln                the solution file
  README.md                       this file

  src\                            production-style code. No tests live here.
    QAAutomation.Core\            shared plumbing every suite needs
      Configuration\              appsettings.json and the typed settings classes
      Support\                    browser start-up, shut-down and failure capture
      Utilities\                  small shared helpers, including the ONE wait helper
    QAAutomation.UI\
      Pages\                      page objects - one per screen
      Components\                 reusable page fragments, such as the header navigation
    QAAutomation.Api\
      Clients\                    the typed HTTP contract and the wrapper tests call
      Contracts\                  the classes a request and a response are read into

  tests\                          the three test suites
    QAAutomation.Tests.Api\
      Features\                   plain-English .feature files
      Steps\                      the C# behind each .feature line
    QAAutomation.Tests.UI\        same two folders
    QAAutomation.Tests.E2E\       same two folders

  test-results\                   evidence from failures. Created at run time, never committed.
```

### Where a new test goes

| You want to test | Write the `.feature` in | Write the code behind it in | And add the real work to |
| --- | --- | --- | --- |
| A server rule | `tests\QAAutomation.Tests.Api\Features\` | `tests\QAAutomation.Tests.Api\Steps\` | `src\QAAutomation.Api\Clients\` |
| Something on screen | `tests\QAAutomation.Tests.UI\Features\` | `tests\QAAutomation.Tests.UI\Steps\` | `src\QAAutomation.UI\Pages\` |
| Two layers agreeing | `tests\QAAutomation.Tests.E2E\Features\` | `tests\QAAutomation.Tests.E2E\Steps\` | whichever of the two above applies |

Two rules about this layout, both worth knowing before you move a file:

- **Dependencies only run one way**: `tests\` may use `src\`, and `src\` may use `Core`. Never
  the other way round, and no test project may ever use another test project.
- Each test project also has a few files at its root — the hooks that start the browser, the
  wiring that builds each scenario's objects, and the small classes a scenario uses to remember
  things between steps. They sit at the root because the test runner only finds them there.

You will also see `*.feature.cs` files appear next to the `.feature` files after a build. Those
are **generated** by the runner. Never edit one, and never commit one.

---

## 4. Prerequisites

Two things, and nothing else.

1. **The .NET 10 SDK.** The SDK is the compiler and command-line tooling for C#. Check it with
   `dotnet --list-sdks`; you want a `10.x` entry.
2. **Google Chrome**, installed normally. The UI and end-to-end tests drive the real browser.

### There is no browser-driver install step

This matters, because most Selenium guides on the internet tell you otherwise.

A browser driver is a small program that sits between the test and the browser, and it has to
match the browser's version. Historically that was a constant maintenance job: Chrome updated
itself, the pinned driver did not, and the whole suite went red for no product reason.

Selenium 4.6 and later include **Selenium Manager**, which works out which driver your Chrome
needs and downloads it for you the first time you run the tests. So:

- there is no driver to install;
- there is no driver version to keep in step with Chrome;
- there is deliberately **no** ChromeDriver package in this repository.

The one consequence: the machine needs **outbound internet access the first time** it runs a
UI test, so Selenium Manager can fetch the driver. After that it is cached. On a locked-down
build agent, pre-seed that cache rather than adding a driver package back.

Checking the two prerequisites:

```powershell
dotnet --list-sdks
Test-Path "C:\Program Files\Google\Chrome\Application\chrome.exe"
```

---

## 5. First run

Open PowerShell and paste these in order.

```powershell
cd d:\git\QAAutomation
dotnet restore
dotnet build
dotnet test
```

What each one does:

- `dotnet restore` downloads the NuGet packages the projects need.
- `dotnet build` compiles all six projects. You want `Build succeeded` with no warnings.
- `dotnet test` runs all three suites: 18 scenarios in total, 9 API, 8 UI and 1 end-to-end.

The first run is the slowest. The UI tests start a real Chrome each time and they talk to a
real website, so expect a couple of minutes overall. The API suite alone takes seconds.

If the UI tests fail on the very first run and the message mentions a driver, that is almost
always the outbound-access point in section 4.

---

## 6. Running a subset

You will rarely want all 18.

**One suite:**

```powershell
cd d:\git\QAAutomation
dotnet test tests\QAAutomation.Tests.Api
```

**One feature file:**

```powershell
cd d:\git\QAAutomation
dotnet test tests\QAAutomation.Tests.Api --filter "FullyQualifiedName~PageListingFeature"
```

**One tag:**

```powershell
cd d:\git\QAAutomation
dotnet test tests\QAAutomation.Tests.UI --filter "TestCategory=smoke"
```

### How tags become filters

The first line of a `.feature` file carries its tags:

```gherkin
@api @regression @marketing
Feature: Page listing
```

When the project is built, the runner turns each tag into an NUnit **category** — NUnit is the
test framework underneath, and a category is simply a label it can filter on. So `@smoke`
becomes `TestCategory=smoke`. No extra configuration, and it is how a build pipeline will one
day select a short smoke run.

Tags are not free-form. Every feature carries exactly one layer tag (`@ui`, `@api` or `@e2e`),
at least one suite tag (`@smoke` or `@regression`) and at least one feature-area tag — here
that is `@marketing`.

### One trap with `FullyQualifiedName`

The filter matches the **generated class name**, which comes from the `Feature:` line, not from
the file name — and it is case-sensitive.

`Homepage.feature` starts with `Feature: Home page`, so the class is `HomePageFeature` and
`--filter "FullyQualifiedName~Homepage"` matches nothing at all. Use `HomePageFeature`.

The six names available today: `PageListingFeature`, `PageLookupFeature`, `HomePageFeature`,
`PrimaryNavigationFeature`, `EnquiryFormFeature` and `PublishedPageRendersInTheBrowserFeature`.

---

## 7. From Gherkin to code

**Gherkin** is the plain-English format a `.feature` file is written in. Each line is a
**step**, and each step is matched to one C# method called a **step definition**.

Here is how one real line becomes a real HTTP request, file by file.

### Trace one — an API line

The line, in `tests\QAAutomation.Tests.Api\Features\PageListing.feature`:

```gherkin
Then the pagination headers report positive whole numbers
```

1. **The feature file** holds the sentence. That is all it holds — no address, no header name,
   no code.

2. **`tests\QAAutomation.Tests.Api\Steps\PageListingSteps.cs`** owns the matching method. Above
   it sits `[Then("the pagination headers report positive whole numbers")]`. The square brackets
   are an **attribute** — a label on a method that the runner reads. This one says "run this
   method for that sentence".

3. **The step reads what the earlier `When` step fetched.** The `When` called
   `PagesClient.ListPagesAsync(...)` and put the answer in a small class called
   `PageApiContext`, which exists only so one step can hand something to the next.

4. **`src\QAAutomation.Api\Clients\PagesClient.cs`** made the actual call. This is the class the
   step injects. It makes one request, reads the two headers it needs, and hands back a plain
   object. It never asserts anything.

5. **`src\QAAutomation.Api\Clients\IWordPressApi.cs`** describes the request. An **interface**
   is a list of method signatures with no bodies. This one is annotated with `[Get("/pages")]`,
   and the Refit library writes the HTTP code from those annotations at build time. Nobody here
   builds a URL by joining strings together.

6. **`src\QAAutomation.Core\Utilities\HeaderValueReader.cs`** pulls a single value out of the
   response headers, returning nothing at all when the header is absent.

7. **`src\QAAutomation.Core\Utilities\PositiveIntegerParser.cs`** decides whether that value is
   a positive whole number. The step asserts on the `true` or `false` it returns.

Read step 2 and step 4 together and the division is clear: the step says *what should be true*,
the client knows *how to ask*.

### Trace two — a UI line

The line, in `tests\QAAutomation.Tests.UI\Features\Homepage.feature`:

```gherkin
Then the browser page title is the expected home page title
```

1. **The feature file** holds the sentence.

2. **`tests\QAAutomation.Tests.UI\Steps\HomepageSteps.cs`** owns the method. It asks the page
   object for the title, then compares it with the expected title from configuration. There is
   no element lookup and no browser command in this file.

3. **`src\QAAutomation.UI\Pages\HomePage.cs`** is the **page object** — the only class that
   knows how that screen is built. Its `ReadTitle()` method returns the browser's tab title.
   Its `Open()` method navigates to the site and waits until the header navigation is really
   visible, so the next step is not racing the page load.

4. **`src\QAAutomation.UI\Components\PrimaryNavigationComponent.cs`** handles the header,
   because the header appears on every page and does not belong to the home page alone.

5. **`src\QAAutomation.Core\Utilities\ElementWaits.cs`** does the waiting. It is the only place
   in the whole repository that waits for anything, and the only place that contains a lambda
   for Selenium's `Until`. Section 9 explains why that concentration is deliberate.

Notice that the expected title is not written in the C#. It lives under `Site:HomePageTitle` in
`src\QAAutomation.Core\Configuration\appsettings.json`, because it is live marketing copy. When
the company rewords it, the fix is one line of settings rather than a change to a test.

---

## 8. How do I add a new test?

A recipe. Follow it in order.

1. **Pick the layer.** Section 2. Prefer the lowest layer that can prove it.

2. **Pick or create the `.feature` file.** One capability per file, named in `PascalCase` —
   `PageListing.feature`. Add to an existing file if the capability already has one.

3. **Tag it**, on the `Feature:` line: one layer tag, at least one suite tag, at least one
   feature-area tag. For example `@api @regression @marketing`.

4. **Write the Gherkin declaratively.** Describe what the business expects, not what the
   browser does. `Then the page lookup reports that the page was not found`, never
   `Then I see the div with class error`. One behaviour per scenario, and one `When` where you
   can manage it.

5. **Add the step definition** to that capability's `*Steps.cs` file in the same test project.
   Before you write it, search the project for the sentence: two methods matching the same
   sentence is an error, and the runner is right to refuse it.

6. **Add the real work** to the page object (UI) or the client (API). If a step body is growing
   past about ten lines, the missing piece belongs in one of those two, not in the step.

7. **Register anything new.** Each test project has one file named `*ScenarioDependencies.cs`
   at its root. If you added a new page object, client or context class, add one line there.
   This is **dependency injection**: you list what exists in one place, and the runner then
   passes each class whatever its constructor asks for. Nothing in this repository builds its
   own collaborators.

8. **Run just your test**, using the `--filter` forms in section 6. Do not run all 18 to check
   one new line.

9. **If it fails, read the evidence** before changing anything. Section 11.

---

## 9. House rules

Five rules, each with the reason it exists. They are enforced in review, and the first one is
also flagged by the build.

### No LINQ

LINQ is the C# query syntax — things like `.Where(...)`, `.Select(...)`, `.FirstOrDefault(...)`
and `from x in y`. It is idiomatic C#, and it is not used here.

**Why:** a `foreach` loop is readable by anyone who has written any language at all, and you
can put a breakpoint on any line of it and watch it run. A chain of four extension methods
reads as one line you cannot step into. Test code is read under pressure, usually when a build
has just gone red.

So instead of a chain, write the loop with a named variable. It is longer and there is nothing
to learn before reading it.

**How it is enforced:** `Directory.Build.targets` searches the hand-written source files for
`using System.Linq` and raises a build **warning** naming the offending file. It is a warning
on purpose — a red build with no product reason costs a team more than a style slip does. If a
harder gate is ever wanted, the upgrade path is the `Microsoft.CodeAnalysis.BannedApiAnalyzers`
package, which can fail the build on a named method rather than a namespace. That would be a
deliberate decision to record in the steering notes first.

**That check only runs on Windows.** It uses `findstr`, a Windows command, so on a Linux or
macOS build agent the check is skipped silently and the rule falls back to review alone. Every
machine this suite builds on today is Windows, so nothing is missing — but it is worth knowing
before the suite is moved to a Linux agent, and the `BannedApiAnalyzers` route above is the
cross-platform fix rather than a second grep.

Three narrow exceptions exist, because some libraries accept nothing else: a single-comparison
assertion predicate, a lambda a library demands (Selenium's `Until`, Refit's client
configuration), and one `.ToList()` at a boundary. They are listed in
`.kiro\steering\code-style.md` and nothing else qualifies.

### No `Thread.Sleep`

`Thread.Sleep` pauses the test for a fixed time.

**Why not:** Selenium does not wait for anything by itself — it acts the instant it is told to
and throws if the page is not ready. A sleep does not fix that; it only moves the race. Too
short and it still fails, too long and every run pays for it, and the day the site is slow it
fails anyway.

Every wait in this repository goes through one class: `ElementWaits` in
`src\QAAutomation.Core\Utilities`. It waits for a condition and gives up after a configured
timeout, so a slow page costs a second rather than a fixed ten. One file to read, one file to
fix. There is no sleep and no hand-written polling loop anywhere in the repository.

### No secrets in the repository

Committed settings carry obvious placeholders — `YOUR_API_KEY_HERE`, `YOUR_TENANT_ID_HERE` —
and nothing else. Real values arrive at run time as environment variables.

**Why:** a committed credential is a leaked credential, and removing the line later is not a
fix. Test automation is a well-known source of exactly this.

Any setting can be overridden with an environment variable named `QAAUTOMATION_`, the section,
two underscores, then the key:

```powershell
# Set it for this shell only...
$env:QAAUTOMATION_Api__BaseUrl = "https://www.landmarksystems.co.uk/wp-json/wp/v2"

# ...run whatever you need, then put the shell back as it was.
Remove-Item Env:\QAAUTOMATION_Api__BaseUrl
```

The same pattern covers `QAAUTOMATION_Site__BaseUrl`, `QAAUTOMATION_WebDriver__Headless` and
every other setting in `appsettings.json`. Note that the public website and API addresses in
that file are **not** secrets; `ApiKey` and `TenantId` are placeholders that exist to
demonstrate the override.

Nothing in this repository ever logs an authorisation header, a cookie value or personal data.

#### This rule is enforced by a hook, not by memory

`.agents/` is committed on purpose, and those review reports quote source code and
configuration. That is a realistic route for a real key to reach the repository by accident,
so the rule is mechanical rather than a matter of remembering.

`scripts/Find-StagedSecrets.ps1` scans **staged** content for credential-shaped values and
`.githooks/pre-commit` runs it on every commit. A finding blocks the commit.

```powershell
# Scan what you are about to commit (this is what the hook runs).
powershell -NoProfile -File .\scripts\Find-StagedSecrets.ps1

# Audit everything already committed.
powershell -NoProfile -File .\scripts\Find-StagedSecrets.ps1 -Scope Tracked

# Scan a folder in the working copy.
powershell -NoProfile -File .\scripts\Find-StagedSecrets.ps1 -Scope Path -Path .agents
```

**After cloning, activate the hook once.** Git does not install committed hooks for you, so
until you run one of these, nothing is checking your commits:

```powershell
git config core.hooksPath .githooks
git config --get core.hooksPath          # should print .githooks
```

Three things worth knowing about how it behaves:

| Behaviour | Why |
| --- | --- |
| Matched values are **masked** in the output (`ghp_********r8`) | Printing a secret copies it into your terminal history and, in CI, into the build log — the very thing the scanner exists to prevent. |
| A placeholder is a **pass**, not a finding | `YOUR_API_KEY_HERE` is the correct thing to commit. The check tests the matched value itself, not the whole line, so a real token is not excused by the word "example" appearing elsewhere on the line. |
| If the scanner cannot run, the commit is **blocked** (exit 2) | A security check that errors must fail closed. An unexplained pass is worse than a stopped commit. |

It scans for GitHub and cloud provider keys, JWTs, bearer and basic auth values, private key
blocks, passwords inside connection strings and URLs, and secret-shaped assignments. It is
calibrated against this repository: 112 tracked files, zero findings, while catching all eight
shapes in its test fixture.

`git commit --no-verify` skips it. That exists for a genuine emergency, not for a Tuesday. If
a finding is a false positive, widen the allow-list in the script so the next person benefits.

**If a real credential was ever pushed, rotate it.** Deleting the line in a later commit does
not remove it from the history, and the history is what an attacker reads.

### Assertions live in steps only

An **assertion** is the line that decides whether a test passes — here written with
AwesomeAssertions, as `something.Should().Be(...)`.

Assertions belong in step definitions. A page object returns what it sees; a client returns
what the server said; neither judges it.

**Why:** a page object that asserts can only ever be used by a scenario expecting that one
answer. The moment a second scenario needs a different expectation, the page object has to be
copied or given a flag. Keep the judgement in the step and the page object serves every
scenario.

One assertion concern per scenario. Related checks that form a single concern are grouped so
that a failure reports all of them, instead of stopping at the first.

### `data-testid` becomes the primary locator on KEYinfinity

A **locator** is how a test finds an element on a page. The strongest kind is a dedicated
attribute added by the developers purely for testing — `data-testid="vat-return-finalise"` —
because it has no other purpose and so nothing else can accidentally change it.

This template cannot use one. The placeholder site is third-party; it has no `data-testid`
attributes and we cannot add any. So the page objects here use the most stable thing the site
actually offers: the header navigation's own class name, the contact form's fixed field ids,
and links identified by where they point.

This is a **deliberate, documented exception**, written at the top of every page object so
nobody copies it onto a system where we do own the markup. On KEYinfinity, `data-testid` is the
default again. The helper that builds such a locator —
`src\QAAutomation.Core\Utilities\TestIdLocator.cs` — already ships here, unused, for that day.

---

## 10. Honest limitations

Read this section before judging the coverage.

- **The system under test is a public marketing website.** It stands in for KEYinfinity. It has
  no login, no tenants, no database and no third-party integrations, so nothing here
  demonstrates authentication, multi-tenant isolation or stubbing. Those are real KEYinfinity
  concerns and they are not faked.

- **It is production, and it is read-only.** Every request is a GET and every page visit is a
  read. The suite runs with a low number of parallel workers for the same reason: a test suite
  has no business putting load on someone else's live site.

- **The contact form is never submitted.** The page object for it,
  `src\QAAutomation.UI\Pages\ContactUsPage.cs`, has **no submit method at all** — not a
  disabled one, not a commented-out one. That is structural on purpose, so nobody "finishes" it
  later and posts a real enquiry to a real sales team. The form's validation is server-side
  only, so the scenarios check what the form declares as mandatory and that typed values are
  held, which is everything that can honestly be checked without a POST.

- **No test asserts a live content count.** The API reports a total number of published pages
  in a response header. That number changes whenever someone at the company publishes a page,
  and nobody tells the test suite. A test asserting today's number would go red for no product
  reason, somebody would "fix" it by editing the number, and after that happened twice nobody
  would trust a red build again. So the suite asserts the rule the API actually promises: the
  total is reported, and it is a positive whole number.

- **Nothing asserts on animated or rotating content.** The home page leads with a rotating hero
  carousel and a row of counters that animate upwards. What either shows depends on timing, so
  an assertion on them would pass or fail on timing rather than on behaviour. The carousel is
  also why no scenario asserts on a heading: the page carries four `h1` elements, one per slide.
  Page titles, stable headings, navigation and form field metadata only.

- **The site opens a newsletter popup on its own, ten seconds after every page load.** While it
  is up it covers the whole window, so any click made later than about ten seconds after the
  page loaded is refused by the browser. That is not a product fault and not a fault in the
  scenario — it is a marketing popup racing the test, and it is intermittent by nature, because
  a fast page gets clicked before the timer fires and a slow one does not. Every navigation
  click therefore closes the popup first if it happens to be open, through
  `src\QAAutomation.UI\Components\SubscribeModalComponent.cs`. That class is worth reading: it
  explains why it *looks* for the popup rather than waiting for it, which is the difference
  between a check that costs nothing and one that burns the full timeout on every click.

- **There is no CI pipeline.** Build-server wiring is parked at the user's request, so there is
  no `pipelines\` folder. The conventions for it are already written down in
  `.kiro\steering\ci-azure-devops.md` and cost nothing while parked.

- **Only Chrome is supported.** Any other browser setting fails with a clear configuration
  error. Adding Edge or Firefox is a later, deliberate change.

- **Live third-party content is a standing fragility.** The expected home page title, the known
  page slug, the navigation link names and the form field ids are all *content*, not contract.
  They are concentrated in `appsettings.json` and in one block of named constants per page
  object, precisely so a copy change on the site is a one-line edit here.

### Moving to KEYinfinity

Three things, in order:

1. Change `Site:BaseUrl` and `Api:BaseUrl` in `appsettings.json`.
2. Restore `data-testid` as the primary locator, and agree it with the front-end developers as
   a contract rather than a favour. Section 9 explains why that agreement is load-bearing.
3. Replace the example features with real ones. The structure stays; the content does not.

---

## 11. Where failures are recorded

Selenium has no trace viewer. There is no timeline to open after the fact, so whatever the
suite does not capture itself is gone. An uncaptured intermittent failure on a build agent is
unexplainable.

So when a UI or end-to-end scenario fails, four files are written to:

```text
d:\git\QAAutomation\test-results\<suite>\<scenario title>-<timestamp>\
```

| File | What it answers |
| --- | --- |
| `screenshot.png` | What did the user actually see — the wrong page, an error banner, a cookie dialog in the way |
| `page-source.html` | Was the element absent, or present but hidden, or present under a different name |
| `console.log` | Did the page's own JavaScript throw an error |
| `location.txt` | The address and title the browser was really on, plus the scenario's correlation id |

`<suite>` is `ui` or `e2e`, so the two suites' evidence never mixes.

**Treat that folder as sensitive.** `page-source.html` and `console.log` are whatever the
application actually sent, written out word for word with nothing hidden. Against this public
marketing site that is harmless — the whole page is published to the internet anyway — which is
why no masking step exists yet. On a system with a login it would not be harmless, so writing
that masking step is part of pointing this suite at KEYinfinity, not a job for later. Either
way the habit is the same: read an artefact folder before you attach it to a ticket or publish
it from a build. The reasoning is recorded in
`.kiro\steering\ui-automation-selenium.md`, section 6.

**Nothing is written when a scenario passes.** Artefacts from green runs are storage cost and
they bury the one capture that matters. So an empty or missing `test-results` folder is the
normal, healthy state:

```powershell
cd d:\git\QAAutomation
if (Test-Path test-results) { Get-ChildItem test-results -Recurse -File | Select-Object FullName } else { "No failures recorded." }
```

`test-results\` is build output. It is never committed.

To write the evidence somewhere else — a build agent's artefact staging folder, for instance —
override the directory:

```powershell
$env:QAAUTOMATION_Artefacts__Directory = "C:\temp\qa-artefacts"
Remove-Item Env:\QAAUTOMATION_Artefacts__Directory
```

One last check worth knowing. A scenario that crashed without shutting its browser down leaves
a driver process behind, and a few hundred of those will bring a machine to its knees. The
shut-down here runs in a `finally` block, so it happens even when the evidence capture itself
fails. After any UI run, this should print nothing at all:

```powershell
Get-Process chromedriver -ErrorAction SilentlyContinue
```

---

## 12. Glossary

**BDD (Behaviour-Driven Development)** — writing down the expected behaviour in plain English
first, in a form the whole team can read, and then automating that exact wording. The point is
the shared understanding; the automation is a by-product.

**Gherkin** — the plain-English format used for that writing-down. Keywords (`Feature`,
`Scenario`, `Given`, `When`, `Then`) and ordinary sentences, saved in a `.feature` file.

**Given / When / Then** — the three parts of a scenario. `Given` is the starting state, `When`
is the one action, `Then` is the observable outcome. A `Given` never asserts; a failing
precondition is a set-up problem, not a test failure.

**Step definition** — the C# method that runs for one Gherkin sentence. It lives in a
`*Steps.cs` file and it is deliberately thin: one or two calls, then an assertion.

**Page object** — a class that holds everything about how one screen is built: how to find its
elements and what you can do on it. Tests talk to the page object, so when the screen changes,
one file changes. Page objects never assert.

**Tag** — a label on a feature or scenario, written `@smoke`. Tags are how a run is narrowed to
a subset, and here they become NUnit categories you can filter on.

**Flaky test** — a test that passes and fails on the same code. Almost always a timing problem:
the test looked before the page was ready, or it asserted on something that moves. A flaky test
is worse than no test, because it teaches the team to re-run the build instead of reading it.

**Explicit wait** — waiting for a *condition* ("until this element is visible") with a maximum
time, rather than waiting a fixed number of seconds. It returns the moment the condition is
met, so it is both faster and more reliable than a sleep. Every wait here is explicit, and they
all live in `ElementWaits`.

**Boundary value analysis** — a way of choosing test data. Most defects sit at the edges of an
allowed range, so you test the edges and one value either side rather than lots of values from
the middle. The API suite does exactly this with page size: valid at 1 and 100, invalid at 0 and
101. Four rows, all the useful coverage.
