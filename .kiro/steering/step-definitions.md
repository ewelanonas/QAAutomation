---
inclusion: fileMatch
fileMatchPattern: "**/*Steps.cs"
---

# Step definition standards

Rules for every `*Steps.cs` binding class. Packages and pinned versions: #[[file:tech-stack.md]]. Project layout, layer rules and naming: #[[file:project-structure.md]]. Gherkin wording: #[[file:bdd-gherkin-standards.md]].

## The thin step rule

A step definition translates a Gherkin sentence into one or two calls on a page object or an API client, then asserts. Nothing else.

| A step may contain | A step must never contain |
| --- | --- |
| Calls to page objects (`src/QAAutomation.UI/Pages`) | `By` locators, CSS, XPath, `FindElement`, `IWebDriver`, `WebDriverWait` |
| Calls to API clients (`src/QAAutomation.Api/Clients`) | `HttpClient`, raw URLs, headers, status-code handling, JSON parsing |
| Assertions using AwesomeAssertions | SQL, connection strings, credentials |
| Reads/writes of a typed scenario context | Control flow driving business logic (`if`/`switch` on domain rules) |
| Test data builder calls (`*Builder`) | `Thread.Sleep`, retry loops, polling |

If a step body exceeds roughly ten lines, the missing abstraction belongs in a page object, a client or a builder.

## Dependency injection

Use the Microsoft DI container via `Reqnroll.Microsoft.Extensions.DependencyInjection`. Never `BoDi`, never service location, never static state.

**One sanctioned exception, and only one.** That ban governs **registration and resolution**: nothing in this repository registers a service with BoDi, resolves one from it, or keeps scenario state in a static field. It does not govern the runner's own start-up settings. Raising `ObjectContainer.DefaultConcurrentObjectResolutionTimeout` from a `[ModuleInitializer]` — which is what `tests/*/ContainerStartup.cs` does, and the only reason those files reference `Reqnroll.BoDi` at all — is permitted, because the runner exposes that limit as a static property and offers no other way to change it: a `[BeforeTestRun]` hook runs too late, since the hook class is itself built by the container. Without it the second feature file of a run gives up after one second waiting on the first `[ScenarioDependencies]` call and reports a cold start as a potential circular dependency, which fails only on the first run after a build. Do not delete those files, and do not extend this exception to anything else.

1. Registration lives in **one** `[ScenarioDependencies]` factory method per test project, in a `*ScenarioDependencies.cs` file at that test project's root (`tests/QAAutomation.Tests.Api/ApiScenarioDependencies.cs`), not scattered across step classes. It is not in `src/QAAutomation.Core/Support`, for two reasons: Reqnroll only discovers `[ScenarioDependencies]` inside a binding assembly, so a factory in `Core` is never found; and each suite needs a different graph — the API suite must not register a browser. The shared registrations themselves still live in `src/*`, exposed as `IServiceCollection` extension methods (`CoreRegistration`, `ApiRegistration`, `UiRegistration`) that each factory calls, so there is still one place per layer.
2. Step classes take their collaborators through **constructor injection only**. No property injection, no `ScenarioContext.Get<T>()` to fetch a service.
3. Page objects, clients and typed contexts are registered **scoped** so each scenario gets a fresh graph.
4. Expensive, immutable, thread-safe things (bound configuration options, the token cache) are registered so they are created once and shared; per-scenario state never is. **`IWebDriver` is not one of them** — it is scoped per scenario, because it is neither immutable nor thread-safe. See #[[file:ui-automation-selenium.md]].
5. A step class never constructs its own dependencies with `new`.

```csharp
[Binding]
public sealed class VatReturnSubmissionSteps
{
    private readonly VatReturnPage vatReturnPage;
    private readonly VatReturnContext vatReturnContext;

    public VatReturnSubmissionSteps(VatReturnPage vatReturnPage, VatReturnContext vatReturnContext)
    {
        this.vatReturnPage = vatReturnPage;
        this.vatReturnContext = vatReturnContext;
    }

    // Selenium is synchronous, so a UI-only step is a plain void method.
    [When("the accountant finalises the return")]
    public void WhenTheAccountantFinalisesTheReturn()
    {
        this.vatReturnContext.Status = this.vatReturnPage.Finalise(this.vatReturnContext.ReturnId);
    }
}
```

## Scenario state: typed contexts, not string bags

`ScenarioContext` is a last resort. Share state between step classes through small, purpose-built context objects registered in DI.

1. One context class per capability: `VatReturnContext`, `ReconciliationContext`. Mutable properties, no behaviour, no assertions.
2. A context lives at the root of the test project whose steps share it (`tests/QAAutomation.Tests.UI/SiteNavigationContext.cs`, `tests/QAAutomation.Tests.Api/PageApiContext.cs`), and is registered scoped by that project's `[ScenarioDependencies]` factory. It does **not** go in `src/QAAutomation.Core/Support`: a context holds the response and page types of one suite, so putting it in `Core` would make `Core` depend on `QAAutomation.Api` or `QAAutomation.UI` and invert the one-way direction `tests/*` → `src/*` → `Core` set in #[[file:project-structure.md]]. A context is shared between step classes, never between suites — two suites needing the same state means the state belongs in a layer class, not in a context.
3. Ban `ScenarioContext["someKey"]` and stringly-typed `Get<T>("key")`. A typo there fails at runtime; a typed property fails at compile time.
4. Inject raw `ScenarioContext`/`FeatureContext` only for genuine runner metadata — scenario title, tags, test error state in a hook.
5. A context holds **test** state, never a cached driver, connection or client.

## Reuse and ambiguity

1. One binding per distinct business phrase, in the capability-named class that owns it (`VatReturnSubmissionSteps`), grouped by capability — never by Given/When/Then.
2. Before adding a step, search for the phrase. Two bindings matching the same sentence is an ambiguous-step failure, and Reqnroll is right to fail it.
3. Prefer a parameter over a near-duplicate step. `the accountant has a {returnStatus} VAT return` beats three separate bindings.
4. Use `[Binding(Scope = ...)]` or `[Scope(Tag = "...")]` to disambiguate genuinely layer-specific wording (a `@ui` versus `@api` meaning of the same sentence). Scope by tag, not by feature title.
5. Keep regex/cucumber-expression parameters business-shaped (`{string}`, enums, typed converters), and register custom `[StepArgumentTransformation]` conversions centrally rather than parsing strings inline.
6. Shared helper logic goes to Core, never to a base step class with protected helpers — inheritance between binding classes is not supported and hides ambiguity.

## Assertions

1. Assertions live in steps only. Page objects return state; clients return responses; neither asserts. See the layer table in #[[file:project-structure.md]].
2. Use AwesomeAssertions (`Should()...`). Do not add FluentAssertions — see the licence warning in #[[file:tech-stack.md]].
3. One assertion concern per scenario. Group related checks with `AssertionScope` instead of splitting one concern across several `Then` steps.
4. Always supply a `because` reason on non-obvious assertions so the failure message explains the business rule.
5. Never assert inside a `Given`. A failing precondition is a setup error, not a test failure — let it throw.

## Sync and async discipline

Which one you get depends on the layer. Selenium's C# API is **synchronous**; Refit is **async-only**.

1. A step that only drives the UI through a page object is a plain **`void`** method. There is nothing to await, and wrapping Selenium in `Task.Run` to make it look async adds a thread and hides the stack trace.
2. A step that touches HTTP (or, later, the database) is `async Task`. Reqnroll awaits async bindings natively.
3. Async all the way down **on the API side only**: client methods are `Task`-returning and `*Async`-suffixed. UI page object methods are synchronous with no `*Async` suffix.
4. `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` and `async void` are banned everywhere — they deadlock and swallow failures.
5. No `Thread.Sleep` and no ad-hoc retry loops. Selenium has no auto-waiting, so UI waiting goes through the one shared `WebDriverWait` helper in `Core/Utilities` (see #[[file:ui-automation-selenium.md]]) — never a sleep, never a `catch` around `FindElement`. `Polly` covers API polling and async settle, and is reserved in the current template.
6. Pass the scenario cancellation token through where the API accepts one.
7. A mixed step — UI action then an API check — is `async Task`, with the synchronous UI call made plainly on its own line. Do not await it; it does not return a `Task`.

## Hooks

Hooks live in a `*Hooks.cs` class, never in a step class. Give every hook an explicit `Order` so sequencing is readable rather than incidental.

A hook class lives at the root of the test project it serves (`tests/QAAutomation.Tests.UI/WebDriverHooks.cs`), not in `src/QAAutomation.Core/Support`, for the same two reasons as the DI factory: Reqnroll only discovers `[Binding]` hooks inside a binding assembly, and a hook shared across suites would start a browser for the API suite, which needs none. The *work* a hook does still belongs in `Core/Support` — `ScenarioDriver`, `WebDriverFactory` and `FailureArtefacts` are there, and the hook is a thin shell that calls them. That is why the UI and E2E suites each have a `WebDriverHooks.cs`: two short files holding a suite name and an ordering, over one shared file holding an `if` about which suite is running.

| Hook | Belongs there | Does not belong there |
| --- | --- | --- |
| `[BeforeTestRun]` | Configuration binding, driver options assembled once, token acquisition per role | Anything scenario-specific; launching a driver |
| `[BeforeScenario]` | **Fresh `IWebDriver`** for the scenario, its `ElementWaits`, tag-driven skips (plus DB/stub reset once those are activated) | Navigation that belongs in a `Given`, test data that belongs in a builder |
| `[AfterScenario]` | Failure artefact capture (screenshot, page source, console logs) **then** `driver.Quit()` in a `finally` | Assertions, business cleanup that a scenario should have done |
| `[AfterTestRun]` | Dispose shared resources, flush reporting, kill orphaned driver processes | Per-scenario teardown |

1. Low `Order` values for infrastructure (driver creation, and later DB/stub reset), higher values for test-visible setup. On teardown, artefact capture runs **before** the driver quits.
2. `[AfterScenario]` teardown must run even when the scenario failed — no early returns before dispose. `driver.Quit()` goes in a `finally` so a capture failure cannot leak a browser process. Use `Quit()`, not `Close()`: `Close()` closes one window and leaves the driver process running.
3. Reset state **before** a scenario, not after, so a crashed run leaves evidence to inspect.
4. Hooks read scenario tags for conditional setup; they never rewrite them.
5. Never log secrets, tokens or unmasked PII in hook diagnostics. Configuration placeholders stay as `YOUR_API_KEY_HERE`.

## Assumptions to confirm

- Capability names used in examples (`VatReturn`, `Reconciliation`) follow the brief's description of KEYinfinity; confirm the real module names before creating binding classes.
- Whether a per-scenario browser session is sufficient isolation, or whether a per-scenario tenant is also needed, depends on the SUT's tenancy model — confirm before building hooks.
