---
inclusion: fileMatch
fileMatchPattern:
  - "**/Pages/**/*.cs"
  - "**/UI/**/*Page.cs"
  - "**/*Page.cs"
---

# UI automation with Selenium WebDriver for .NET

Rules for every page object and UI driver file in `src/QAAutomation.UI`. Pinned `Selenium.WebDriver` / `Selenium.Support` versions, and why Selenium rather than Playwright: #[[file:tech-stack.md]]. Layout, naming and layer rules: #[[file:project-structure.md]]. What a step is allowed to call: #[[file:step-definitions.md]]. C# readability rules (no LINQ, explicit types): #[[file:code-style.md]].

Two facts drive this entire file.

**One: KEYinfinity's front end is Vue 3 + TypeScript, and part of the application is produced by a model-driven platform.** Markup we did not hand-write can be regenerated at any time, so **anything the generator owns is not a locator**.

**Two: Selenium does not wait for you.** It acts the instant it is told to, and throws if the element is not there yet. Playwright's auto-waiting does not exist here. That is not a reason to sprinkle sleeps — it is the reason the shared wait helper in section 3 is mandatory on every interaction with a dynamic element.

Selenium's C# API is **synchronous**. Page objects in this repository return values directly, not `Task<T>`. There is no `await` in a page object and no `*Async` suffix on its methods.

## 1. Locator strategy — the `data-testid` contract

Generated DOM means auto-generated `id`, `class` and structural attributes are **unstable by design**. A suite built on them is a suite that goes red on a platform upgrade that changed no behaviour.

| Priority | Strategy | Example | When |
| --- | --- | --- | --- |
| 1 — default | `data-testid` via a CSS attribute selector | `By.CssSelector("[data-testid='vat-return-finalise']")` | Every interactive element the suite touches. |
| 2 — stable / semantic | A stable meaningful attribute, or an accessible/semantic selector | `By.Name("periodStart")`, `By.CssSelector("button[aria-label='Finalise']")` | No test id yet, and the element carries something genuinely stable and meaningful. |
| 3 — last resort | `By.XPath`, or a structural CSS selector | `By.XPath("//table//tbody/tr[2]/td[3]")` | Only with a written justification comment and a linked work item to replace it. |

**Selenium has no role or label locator API.** There is no `GetByRole`, no `GetByLabel`, no `GetByText`. Playwright's accessibility-first locators simply have no equivalent here, and `By.XPath` with a `text()` predicate is not a substitute — it is brittle and it breaks on translation. So the discipline that keeps this suite stable does not come from the tool. It comes from the **`data-testid` contract with the Vue 3 developers**. If that contract is not honoured, Selenium gives us nothing to fall back on but structural selectors, which is exactly the failure mode we are trying to avoid.

Keep the attribute name in one named constant and build the selector through one small helper, so the whole suite changes in one place if the agreed attribute name changes:

```csharp
// src/QAAutomation.Core/Utilities/TestIdLocator.cs
public static class TestIdLocator
{
    private const string TestIdAttributeName = "data-testid";

    public static By ByTestId(string testId)
    {
        return By.CssSelector("[" + TestIdAttributeName + "='" + testId + "']");
    }
}
```

### The contract, not a request

`data-testid` is a **contract with the Vue 3 developers and the platform owners**, agreed up front and treated as production API surface:

1. Every element the suite interacts with carries a `data-testid`. Adding one is a normal front-end change, not a favour to QA.
2. Ids are **stable, semantic and business-shaped**: `vat-return-finalise`, `bankrec-statement-import`, `invoice-line-row`. Never generator output, never index-based, never a translated label.
3. For generated screens the id must be emitted by the **model/template**, so every generated instance of a control gets one for free rather than being patched per screen.
4. Renaming or removing a `data-testid` is a breaking change: it goes through review and the suite is updated in the same pull request.
5. Collections expose a repeated id on the row plus a nested id per cell, so rows are selected by matching visible business content, not by position.
6. Because Selenium cannot fall back on accessible names, item 1 is load-bearing. Escalate a missing id as a blocker, do not quietly write XPath.

Finding a row by business content, without LINQ and without an index:

```csharp
// Scope the search to the rows, then match on the business value the scenario cares about.
private IWebElement FindInvoiceLineRow(string invoiceNumber)
{
    IReadOnlyCollection<IWebElement> rows = this.driver.FindElements(TestIdLocator.ByTestId(InvoiceLineRowTestId));

    foreach (IWebElement row in rows)
    {
        if (row.Text.Contains(invoiceNumber))
        {
            return row;
        }
    }

    throw new NoSuchElementException("No invoice line row contains invoice number " + invoiceNumber + ".");
}
```

## 2. Page object shape

A page object is the only place that knows how the screen is built. It exposes **intent**, not mechanics.

| A page object does | A page object never does |
| --- | --- |
| Expose intent-level methods (`Finalise`, `ImportStatement`) | Assert anything — assertions live in steps |
| Hold locators as `private static readonly By` fields built from named test id constants | Contain Gherkin wording or scenario vocabulary |
| Return the next page object when navigation occurs | Contain `Thread.Sleep`, `Task.Delay`, or a hand-rolled retry loop |
| Return typed state (`string`, `decimal`, record, DTO) for steps to assert on | Read test data files or generate test data |
| Receive its `IWebDriver` and the wait helper through constructor injection | Create its own `IWebDriver` |

1. Methods are **synchronous**. Selenium's API is synchronous, so there is nothing to await. No `async void`, no `Task.Run`, no `*Async` suffix on UI methods.
2. A method that moves the user to another screen returns that screen's page object, so step definitions read as a journey.
3. A method that stays on the screen returns `void` or the state the step needs.
4. Do not cache `IWebElement` in a field. Elements go stale on re-render; a `By` does not. Store the `By`, resolve it when you need it. See section 5.
5. Scope a search to a parent element rather than reaching across the page. Reusable fragments go to `Components/` as `*Component.cs`, built around their own root element.
6. One level of inheritance maximum (a thin `BasePage` for navigation plumbing, nothing more). No framework inside the framework.

```csharp
public sealed class VatReturnPage
{
    private const string FinaliseTestId = "vat-return-finalise";
    private const string StatusTestId = "vat-return-status";

    private readonly IWebDriver driver;
    private readonly ElementWaits waits;

    public VatReturnPage(IWebDriver driver, ElementWaits waits)
    {
        this.driver = driver;
        this.waits = waits;
    }

    public VatSubmissionPage Finalise()
    {
        IWebElement finaliseButton = this.waits.WaitUntilClickable(TestIdLocator.ByTestId(FinaliseTestId));
        finaliseButton.Click();
        return new VatSubmissionPage(this.driver, this.waits);
    }

    public string ReadStatus()
    {
        IWebElement status = this.waits.WaitUntilVisible(TestIdLocator.ByTestId(StatusTestId));
        return status.Text;
    }
}
```

## 3. Waiting — one shared helper, explicit every time

Selenium has no auto-waiting. Every interaction with an element the app renders asynchronously goes through the wait helper. This is the single highest-value rule in the file: it is the difference between a suite that is trusted and a suite that is re-run.

**Banned outright:** `Thread.Sleep`, `Task.Delay`, "just to be safe" pauses, hand-rolled `while` loops polling `Displayed`, `try`/`catch` around a `FindElement` to retry it, and global timeout inflation to mask a race.

**Also banned:** a bare `driver.FindElement(...)` on anything dynamic. `FindElement` resolves once, immediately, and throws if the app has not rendered yet. It is acceptable only for an element that is present in the initial server-rendered document and never re-rendered — and if you have to think about whether that is true, use the wait.

There is **one** wait helper for the whole suite, in `src/QAAutomation.Core/Utilities`, built on `WebDriverWait` from `Selenium.Support`. We write it ourselves rather than taking `DotNetSeleniumExtras.WaitHelpers`, which is abandoned — see #[[file:tech-stack.md]].

```csharp
// src/QAAutomation.Core/Utilities/ElementWaits.cs
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace QAAutomation.Core.Utilities;

public sealed class ElementWaits
{
    private readonly IWebDriver driver;
    private readonly TimeSpan timeout;

    public ElementWaits(IWebDriver driver, TimeSpan timeout)
    {
        this.driver = driver;
        this.timeout = timeout;
    }

    public IWebElement WaitUntilVisible(By locator)
    {
        WebDriverWait wait = CreateWait();

        // The Until lambda is required by WebDriverWait — it accepts nothing else.
        // Allowed under exception 2 in code-style.md.
        return wait.Until(driver =>
        {
            IWebElement element = driver.FindElement(locator);
            if (element.Displayed)
            {
                return element;
            }

            return null;
        });
    }

    public IWebElement WaitUntilClickable(By locator)
    {
        WebDriverWait wait = CreateWait();

        return wait.Until(driver =>
        {
            IWebElement element = driver.FindElement(locator);
            if (element.Displayed && element.Enabled)
            {
                return element;
            }

            return null;
        });
    }

    public void WaitUntilGone(By locator)
    {
        WebDriverWait wait = CreateWait();

        wait.Until(driver =>
        {
            IReadOnlyCollection<IWebElement> found = driver.FindElements(locator);
            if (found.Count == 0)
            {
                return true;
            }

            bool anyStillShowing = false;
            foreach (IWebElement element in found)
            {
                if (element.Displayed)
                {
                    anyStillShowing = true;
                    break;
                }
            }

            return !anyStillShowing;
        });
    }

    public string WaitUntilTextIs(By locator, string expectedText)
    {
        WebDriverWait wait = CreateWait();

        return wait.Until(driver =>
        {
            IWebElement element = driver.FindElement(locator);
            if (element.Text == expectedText)
            {
                return element.Text;
            }

            return null;
        });
    }

    private WebDriverWait CreateWait()
    {
        WebDriverWait wait = new WebDriverWait(this.driver, this.timeout);

        // A re-render between resolving the element and reading it is normal in Vue.
        // Ignoring these two makes the wait retry instead of failing on the first blink.
        wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
        return wait;
    }
}
```

Rules for using it:

1. Every page object takes `ElementWaits` by constructor injection. It never news up a `WebDriverWait` inline.
2. Add a wait method to the helper rather than writing a new `Until` lambda in a page object. One lambda per condition, in one file, so there is one place to read and one place to fix.
3. The timeout comes from a `*Options` class under `Configuration/`, set once, never per call site. Raise it only with evidence that the operation is legitimately slow.
4. **Do not set a long implicit wait.** Implicit and explicit waits interact badly and make timeouts unpredictable. Leave `ImplicitWait` at zero and use the helper.
5. Spinner and skeleton handling belongs in a page object method (`this.waits.WaitUntilGone(ImportSpinnerLocator)`), never in a step.
6. `Polly` is for **API** polling and async settle only, and it is reserved anyway in the current template. It must never appear in UI code to retry a click — see #[[file:tech-stack.md]].

Assertions on page state still live in step definitions. A page object waits, then returns the value; the step asserts on it with AwesomeAssertions.

## 4. Driver lifecycle — one driver per scenario, always disposed

Isolation is per scenario. A leaked cookie or a reused session is a cross-scenario dependency, and in a multi-tenant SaaS it is also a false pass. A leaked *process* is worse: orphaned `chromedriver.exe` and browser processes accumulate until the agent runs out of memory.

| Scope | Lifetime | Where |
| --- | --- | --- |
| Driver options / bound configuration | Once per test run, shared | `[BeforeTestRun]` in `*Hooks.cs` |
| `IWebDriver` | **Fresh per scenario** | `[BeforeScenario]`, quit in `[AfterScenario]` |
| `ElementWaits` | One per scenario, wrapping that scenario's driver | Registered scoped in DI |
| Page objects | Scoped per scenario | DI registration in `Support` |

1. **One driver per scenario.** Selenium has no equivalent of a browser context, so a fresh browser session is the only reliable isolation boundary. Reusing one driver across scenarios and clearing cookies between them is not isolation — it leaves storage, in-memory app state and open dialogs behind.
2. Selenium Manager resolves the browser driver automatically (Selenium 4.6+). Do **not** add `Selenium.WebDriver.ChromeDriver` or `WebDriverManager` — see #[[file:tech-stack.md]].
3. **Quit, do not just dispose a reference.** `driver.Quit()` closes every window and ends the driver process. `driver.Close()` closes one window and leaves the process alive. Call `Quit()`.
4. Teardown runs **even when the scenario failed**, and it runs **after** evidence capture. Wrap it so a capture failure cannot skip the quit:

   ```csharp
   // In *Hooks.cs — illustrative; the hook itself belongs under Support/.
   [AfterScenario(Order = 100)]
   public void QuitDriver()
   {
       try
       {
           if (this.scenarioContext.TestError != null)
           {
               this.failureArtefacts.Capture(this.scenarioContext.ScenarioInfo.Title);
           }
       }
       finally
       {
           this.driver.Quit();
           this.driver.Dispose();
       }
   }
   ```

5. Never hold a `static IWebDriver` and never share one across threads. `IWebDriver` is not thread-safe. Parallelism is one driver per worker, per scenario.
6. In CI, run headless and add a step that kills orphaned driver processes after the stage — belt and braces, because one crashed scenario that skipped teardown poisons the agent for every run after it.
7. Hook ordering, DI registration and the hook responsibility table live in #[[file:step-definitions.md]]; do not restate them in page objects.

## 5. Stale elements — design them out, do not catch them

`StaleElementReferenceException` means the element was found, then the DOM replaced it. With Vue's reactivity and a model-driven platform re-rendering regions, this is routine, not exceptional.

The fix is a design rule, not a `catch`:

1. **Never store an `IWebElement` in a field.** Store the `By`. Resolve the element at the moment of use, inside the method that uses it.
2. Resolve, then act, in adjacent lines. The longer the gap between `FindElement` and `Click`, the wider the window for a re-render.
3. Wait for the container to settle *before* reaching into it. If a grid is still loading, every row reference you take is already stale.
4. Re-find rather than retry. When a method needs the element twice, call the locator twice — that is cheap and it is correct.
5. `ElementWaits` already ignores `StaleElementReferenceException` inside its `Until`, so the retry lives in one place, bounded by the timeout.
6. A `try`/`catch (StaleElementReferenceException)` in a page object is a review failure. It hides the design problem and swallows real failures with it.

```csharp
// Avoid: the element is resolved once and held while the grid re-renders.
private IWebElement firstRow;

// Prefer: hold the locator, resolve at the point of use.
private static readonly By FirstRowLocator = TestIdLocator.ByTestId("statement-line-row");
```

## 6. Failure artefacts — screenshot, page source, console logs

Selenium has **no trace viewer**. There is no timeline, no DOM snapshot history and no network tab to open after the fact. Whatever we do not capture ourselves is gone, and an uncaptured intermittent CI failure is unexplainable. So capture is not a nice-to-have — it is the diagnostic story.

Capture on failure only, into `test-results/`, in a folder named for the scenario and a timestamp:

| Artefact | How | Answers |
| --- | --- | --- |
| Screenshot (PNG) | `((ITakesScreenshot)driver).GetScreenshot().SaveAsFile(path)` | What did the user see — right dialog, error banner, wrong screen |
| Page source (HTML) | `driver.PageSource` | Was the element absent, present-but-hidden, or present with a different test id |
| Browser console logs | `driver.Manage().Logs.GetLog(LogType.Browser)` | Unhandled front-end error, hydration warning, failed request |
| Current URL and window title | `driver.Url`, `driver.Title` | Did navigation go somewhere unexpected, was the session bounced to login |
| Driver log | enable the driver's own log file via driver service options | Driver-level crashes and timeouts |

**Decision — the driver log is deliberately not captured in the current template.** The first four artefacts are written by `src/QAAutomation.Core/Support/FailureArtefacts.cs`; the driver log is not. Enabling it means handing `ChromeDriverService` a log path **when the driver starts**, which is before anyone knows whether the scenario will fail, so the file is always written and then either copied into the scenario folder or deleted. That is a temporary file, its cleanup and a service object to dispose — moving parts in the one place that must never throw, in exchange for evidence about a failure mode (chromedriver itself crashing) that has not happened once against this site. Bring it back when a driver-level failure actually needs explaining, or when the suite runs on a shared build agent where the driver is a plausible suspect. Until then the gap is this decision, not an oversight.

```csharp
// src/QAAutomation.Core/Support/FailureArtefacts.cs — illustrative shape.
public sealed class FailureArtefacts
{
    private const string ArtefactRootFolder = "test-results";

    private readonly IWebDriver driver;

    public FailureArtefacts(IWebDriver driver)
    {
        this.driver = driver;
    }

    public void Capture(string scenarioTitle)
    {
        string folder = CreateScenarioFolder(scenarioTitle);

        ITakesScreenshot screenshotTaker = (ITakesScreenshot)this.driver;
        Screenshot screenshot = screenshotTaker.GetScreenshot();
        screenshot.SaveAsFile(Path.Combine(folder, "screenshot.png"));

        File.WriteAllText(Path.Combine(folder, "page-source.html"), this.driver.PageSource);
        File.WriteAllText(Path.Combine(folder, "console.log"), ReadConsoleLog());
        File.WriteAllText(Path.Combine(folder, "location.txt"), this.driver.Url + Environment.NewLine + this.driver.Title);
    }

    private string ReadConsoleLog()
    {
        StringBuilder builder = new StringBuilder();
        ILogs logs = this.driver.Manage().Logs;

        // Not every driver exposes the browser log; a missing log must not break teardown.
        try
        {
            IReadOnlyCollection<LogEntry> entries = logs.GetLog(LogType.Browser);
            foreach (LogEntry entry in entries)
            {
                builder.AppendLine(entry.Timestamp.ToString("O") + " [" + entry.Level + "] " + entry.Message);
            }
        }
        catch (WebDriverException exception)
        {
            builder.AppendLine("Browser log unavailable: " + exception.Message);
        }

        return builder.ToString();
    }

    private static string CreateScenarioFolder(string scenarioTitle)
    {
        string safeTitle = scenarioTitle.Replace(' ', '-');
        string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        string folder = Path.Combine(ArtefactRootFolder, safeTitle + "-" + stamp);
        Directory.CreateDirectory(folder);
        return folder;
    }
}
```

Rules:

1. Capture happens in `[AfterScenario]`, **before** the driver is quit, driven by `ScenarioContext.TestError`.
2. On pass, capture nothing. Artefacts for green runs are storage cost and they bury the one capture that matters.
3. `test-results/` is gitignored. It is build output, never committed.
4. A capture failure must never prevent the driver from quitting — hence the `try`/`finally` in section 4.
5. **Page source and console logs can contain tokens, cookies and PII.** Mask before writing, mask by allow-list, and never attach an unmasked capture to a widely readable artefact store. Never log an `Authorization` header or a session cookie value.

   **Decision — masking is deferred in the current template, and this is the condition that ends the deferral.** `FailureArtefacts` writes the page source and the console log verbatim today. Two reasons. First, there is nothing to mask: the system under test is an anonymous public marketing website, the suite holds no session, sends no `Authorization` header and submits no personal data, so every byte captured is already published to the internet. Second, the honest implementations both cost more than they are worth here — a true allow-list would throw away the unrecognised text that is exactly what a reader needs after an unexplained failure, and a pattern-matching blocklist is the approach #[[file:api-automation.md]] §8.3 rules out, because it protects only against the shapes someone thought of.

   So the masking pass is written **at the same time as the first authenticated target**, not before, and it is a blocker for that change rather than a follow-up: the moment the suite logs in, holds a token or touches a tenant — step 1 of the KEYinfinity migration — these two files start carrying credentials. Until then the control is the handling rule, not the code: `test-results/` is gitignored, nothing is echoed into the test output, and an artefact folder is never attached to a ticket or published from a build without being read first. Nothing in the template logs an `Authorization` header or a cookie value, and that rule stands whatever is captured.
6. When CI is wired up, these artefacts are what gets published on failure — see #[[file:ci-azure-devops.md]], currently parked.

## 7. Authenticated sessions

Logging in through the UI in every scenario tests the login form thousands of times and tests nothing else. Selenium has no storage-state file to reuse, which makes this harder than it is in Playwright and means the options are narrower.

1. Prefer **authenticating out of band**: obtain a session or token through the API, then inject it into the browser before navigating — set the cookie via `driver.Manage().Cookies.AddCookie(...)`, or seed `localStorage` with a script, whichever the app actually reads.
2. A cookie can only be set for the current domain, so navigate to the origin first, add the cookie, then navigate to the target page.
3. Keep the credential-to-session exchange in one place under `Support`, keyed per role and per tenant, so a permission scenario cannot silently borrow another identity.
4. Keep a handful of explicit `@ui @smoke @permissions` scenarios that **do** log in through the form, so the real login path still has coverage.
5. Credentials come from environment variables or the CI secret store. Committed config carries placeholders such as `YOUR_API_KEY_HERE` only. Never log a token, a cookie value or unmasked PII.
6. If the session expires mid-run, re-establish it in a hook. Never add a retry loop inside a page object.
7. For the current public-marketing-website template there is no login at all — this section is here for KEYinfinity, and is **unconfirmed** until the real auth mechanism is known.

## 8. Prerequisites

1. `dotnet restore` is enough for the packages. **Selenium Manager handles the browser driver** — no install script, no pinned ChromeDriver, no cache key to maintain. This is the main operational simplification over Playwright.
2. A real browser must be installed on the machine and on every CI agent. Selenium Manager resolves the *driver*, not the browser itself.
3. Selenium Manager downloads drivers on first use, so an agent needs outbound network access to do it. On a locked-down agent, pre-seed the Selenium Manager cache rather than reintroducing a pinned driver package.
4. Headless by default; headed only behind a local configuration switch, never in CI.
5. Document the browser prerequisite in the repository README so a new QA can run the suite on day one.

## Assumptions to confirm

- `data-testid` as the attribute name, and whether the model-driven platform can emit it from the template layer, are **assumptions**. Confirm with the front-end and platform owners before writing page objects. Because Selenium has no accessible-name fallback, a "no" here is a blocker, not an inconvenience.
- Role and tenant names used in the session examples (`accountant`, `tenant-a`) are illustrative. Confirm the real role model — see #[[file:domain-keyinfinity.md]].
- Whether the application stores its session in a cookie or in `localStorage` determines which out-of-band auth approach in section 7 works. Unconfirmed.
- Whether a fresh browser session is sufficient isolation, or a fresh tenant is also required per scenario, depends on the tenancy model and is unconfirmed.
