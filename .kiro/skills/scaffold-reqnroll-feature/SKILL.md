---
name: scaffold-reqnroll-feature
description: Use when the user asks to scaffold, generate or set up a new Reqnroll feature area — the .feature file, its step definition class, the page object or API client it delegates to, and the DI registration — or to check that an existing feature area follows the layering rules.
---

# Scaffold a Reqnroll feature area

Takes one named feature area (for example "VAT return submission" or "bank statement import") and produces the full set of artefacts for it, consistent across layers and obeying the one-way dependency rule `tests/*` → `src/*` → `Core`.

Output is **markdown snippets with their intended file paths**, ready for the user to place. Do not write `.feature`, `.cs`, `.csproj` or pipeline files to disk from this skill unless the user explicitly asks for files.

Layout, naming and layer rules: #[[file:project-structure.md]]. Gherkin wording and tags: #[[file:bdd-gherkin-standards.md]]. Binding rules, DI and hooks: #[[file:step-definitions.md]]. Locators, waits and driver lifecycle: #[[file:ui-automation-selenium.md]]. Clients and contracts: #[[file:api-automation.md]]. Packages and pins: #[[file:tech-stack.md]]. C# readability: #[[file:code-style.md]]. Domain vocabulary: #[[file:domain-keyinfinity.md]].

Two standing rules:

1. **Every route, header, field name, status value and role name is an assumption until sourced from a specification.** Mark it **(ASSUMPTION)** in the output and list it at the end. Never present an invented API shape as the contract.
2. **No secrets.** Placeholders are `YOUR_API_KEY_HERE`. No tokens, tenant names or connection strings in any snippet.

## Step 1 — Fix the capability name and the layer

Ask for, or infer and state, three things before writing anything.

| Decision | Rule |
| --- | --- |
| Capability name | One capability, `PascalCase`, in the business's words — `VatReturnSubmission`, `BankStatementImport`. Not a screen name, not a sprint name. |
| Layer | Exactly one: `@api` if the rule can be proven at the API, `@ui` if it is genuinely about what the user sees, `@e2e` only for a journey that must work across layers. |
| Owning project | `tests/QAAutomation.Tests.Api`, `.UI` or `.E2E`. The layer tag must match the project. |

Default to `@api`. A business rule proven at the API runs in seconds and fails for one reason; the same rule proven through the browser runs in minutes and fails for six.

## Step 2 — Decide the artefact set

The layer determines what gets generated. Never generate both a page object and an API client for the same step class unless the layer is `@e2e`.

| Layer | Feature file | Steps class | Delegate | Contracts | DI additions |
| --- | --- | --- | --- | --- | --- |
| `@api` | `tests/QAAutomation.Tests.Api/Features/<Capability>.feature` | `.../Steps/<Capability>Steps.cs` | `src/QAAutomation.Api/Clients/<Capability>Client.cs` + `I<Capability>Api` | `src/QAAutomation.Api/Contracts/*Request`, `*Response` | client, Refit interface, typed context |
| `@ui` | `tests/QAAutomation.Tests.UI/Features/<Capability>.feature` | `.../Steps/<Capability>Steps.cs` | `src/QAAutomation.UI/Pages/<Screen>Page.cs` | — | page objects, typed context |
| `@e2e` | `tests/QAAutomation.Tests.E2E/Features/<Capability>.feature` | `.../Steps/<Capability>Steps.cs` | page objects **and** clients (clients for setup, pages for the journey) | as above | both, typed context |

Also decide whether the capability needs a `*Builder` in `src/QAAutomation.Core/TestData` (it does, if the scenario needs data that does not already exist — the folder is deferred until the first builder, see #[[file:project-structure.md]]) and a typed context at the **root of the test project** that owns the capability, such as `tests/QAAutomation.Tests.Api/PageApiContext.cs`. A context does not go in `Core`: it holds that suite's own response and page types, which `Core` is not allowed to know about.

## Step 3 — Write the feature file

One capability per file. Feature-level tags carry one layer tag, at least one suite tag and at least one feature-area tag. A coverage comment in the header states what the file does **not** cover.

```gherkin
# tests/QAAutomation.Tests.Api/Features/VatReturnSubmission.feature
@api @regression @vat @mtd
Feature: VAT return submission
  # Coverage: finalise, submit, duplicate submission, cross-tenant refusal.
  # Not covered: rejection payload handling (separate feature), UI declaration wording.

  Background:
    Given a VAT-registered entity exists in the current tenant
    And the entity has a draft VAT return for an open obligation

  @smoke
  Scenario: Submitting a finalised return records the authority's acceptance
    Given the accountant has finalised the return
    When the accountant submits the return to the tax authority
    Then the return is recorded as accepted
    And the submission receipt is stored against the return

  @multitenant
  Scenario: A return belonging to another tenant cannot be submitted
    Given a finalised VAT return exists in another tenant
    When the accountant submits that return
    Then the request is refused
    And no figures from the other tenant appear in the response
```

Rules applied above, all owned by #[[file:bdd-gherkin-standards.md]]:

1. Third person, declarative, present tense. No `I`, no URLs, no status codes, no JSON.
2. `Background` is three steps or fewer and contains no `When` or `Then`.
3. `@smoke` goes on the one scenario that must never break, not on the whole feature.
4. Reuse step wording verbatim where the meaning is identical — check for an existing binding before inventing a phrase.
5. Status names (`accepted`, `finalised`) are **(ASSUMPTION)** until the real lifecycle is confirmed.

## Step 4 — Write the step definition class

Thin. Each step is one or two calls on an injected collaborator, then an assertion. No locators, no HTTP, no SQL, no business `if`.

```csharp
// tests/QAAutomation.Tests.Api/Steps/VatReturnSubmissionSteps.cs
using AwesomeAssertions;
using QAAutomation.Api.Clients;
using QAAutomation.Api.Contracts;
using QAAutomation.Core.Support;
using Refit;
using Reqnroll;

namespace QAAutomation.Tests.Api.Steps;

[Binding]
public sealed class VatReturnSubmissionSteps
{
    private const string AcceptedStatus = "Accepted";   // (ASSUMPTION) confirm lifecycle names

    private readonly VatReturnClient vatReturnClient;
    private readonly VatReturnContext vatReturnContext;
    private readonly TenantContext tenantContext;

    public VatReturnSubmissionSteps(
        VatReturnClient vatReturnClient,
        VatReturnContext vatReturnContext,
        TenantContext tenantContext)
    {
        this.vatReturnClient = vatReturnClient;
        this.vatReturnContext = vatReturnContext;
        this.tenantContext = tenantContext;
    }

    [When("the accountant submits the return to the tax authority")]
    public async Task WhenTheAccountantSubmitsTheReturnToTheTaxAuthority()
    {
        SubmitVatReturnRequest request = new SubmitVatReturnRequest(
            this.vatReturnContext.ReturnId,
            this.vatReturnContext.DeclaredByUserId);

        this.vatReturnContext.SubmitResponse = await this.vatReturnClient.SubmitAsync(
            this.tenantContext.TenantId,
            this.vatReturnContext.ReturnId,
            request,
            this.tenantContext.CorrelationId,
            CancellationToken.None);
    }

    [Then("the return is recorded as accepted")]
    public async Task ThenTheReturnIsRecordedAsAccepted()
    {
        IApiResponse<SubmitVatReturnResponse> response = this.vatReturnContext.SubmitResponse;

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "an accepted submission returns 200 with a receipt (ASSUMPTION: confirm 200 vs 202)");

        SubmitVatReturnResponse body = response.Content;
        body.Should().NotBeNull();
        body.Status.Should().Be(AcceptedStatus,
            because: "the authority acknowledged the submission");
    }
}
```

Checklist for the class:

1. `sealed`, `[Binding]`, named `<Capability>Steps`, grouped by capability and never by Given/When/Then.
2. Constructor injection only. No `new` of a collaborator, no `ScenarioContext.Get<T>("key")`, no static state.
3. Shared scenario state goes in a typed context, not a string bag.
4. `async Task` for anything touching HTTP or the database. A **UI-only step is a plain `void` method** — Selenium is synchronous, so there is nothing to await. No `.Result`, no `.Wait()`, no `async void`.
5. Assertions only in `Then` steps, with a `because` reason naming the business rule. Never assert in a `Given`.
6. Named `const` for every literal status or key. No magic strings.
7. No LINQ — see the three narrow exceptions in #[[file:code-style.md]].

## Step 5 — Write the delegate: API client or page object

### API layer

A Refit interface declares the contract; a wrapper class is what steps inject. The wrapper is where the explicit tenant parameter and its guard live.

```csharp
// src/QAAutomation.Api/Clients/IVatReturnApi.cs  — routes are (ASSUMPTION)
public interface IVatReturnApi
{
    [Post("/vat-returns/{returnId}/submit")]
    Task<IApiResponse<SubmitVatReturnResponse>> SubmitAsync(
        [Header("X-Tenant-Id")] string tenantId,
        string returnId,
        [Body] SubmitVatReturnRequest request,
        [Header("X-Correlation-Id")] string correlationId,
        CancellationToken cancellationToken);
}
```

```csharp
// src/QAAutomation.Api/Clients/VatReturnClient.cs
public sealed class VatReturnClient
{
    private readonly IVatReturnApi vatReturnApi;

    public VatReturnClient(IVatReturnApi vatReturnApi)
    {
        this.vatReturnApi = vatReturnApi;
    }

    // tenantId is required by design: there is no ambient tenant.
    public async Task<IApiResponse<SubmitVatReturnResponse>> SubmitAsync(
        string tenantId,
        string returnId,
        SubmitVatReturnRequest request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("A tenant must be supplied explicitly.", nameof(tenantId));
        }

        return await this.vatReturnApi.SubmitAsync(
            tenantId, returnId, request, correlationId, cancellationToken);
    }
}
```

Contracts are records in `Contracts/`:

```csharp
// src/QAAutomation.Api/Contracts/SubmitVatReturnRequest.cs
public sealed record SubmitVatReturnRequest(string ReturnId, string DeclaredByUserId);

// src/QAAutomation.Api/Contracts/SubmitVatReturnResponse.cs
public sealed record SubmitVatReturnResponse(string ReturnId, string Status, string ReceiptReference);
```

Rules: `IApiResponse<T>` rather than bare `T` so negative cases can read the status; no literal base URL in the client; no assertions and no test data generation inside the client. Full detail in #[[file:api-automation.md]].

### UI layer

A page object exposes intent, holds the locators, waits before every interaction, asserts nothing, and returns the next page object on navigation. Selenium's C# API is **synchronous**, so page objects are synchronous too — no `async`, no `Task`, no `*Async` suffix.

```csharp
// src/QAAutomation.UI/Pages/VatReturnPage.cs
using OpenQA.Selenium;
using QAAutomation.Core.Utilities;

public sealed class VatReturnPage
{
    private const string FinaliseTestId = "vat-return-finalise";
    private const string SubmitTestId = "vat-return-submit";
    private const string StatusTestId = "vat-return-status";

    private static readonly By FinaliseButton = TestIdLocator.ByTestId(FinaliseTestId);
    private static readonly By SubmitButton = TestIdLocator.ByTestId(SubmitTestId);
    private static readonly By StatusBadgeValue = TestIdLocator.ByTestId(StatusTestId);

    private readonly IWebDriver driver;
    private readonly ElementWaits waits;

    public VatReturnPage(IWebDriver driver, ElementWaits waits)
    {
        this.driver = driver;
        this.waits = waits;
    }

    public void Finalise()
    {
        IWebElement finaliseButton = this.waits.WaitUntilClickable(FinaliseButton);
        finaliseButton.Click();
    }

    public VatSubmissionPage Submit()
    {
        IWebElement submitButton = this.waits.WaitUntilClickable(SubmitButton);
        submitButton.Click();
        return new VatSubmissionPage(this.driver, this.waits);
    }

    public string ReadStatus()
    {
        IWebElement statusBadge = this.waits.WaitUntilVisible(StatusBadgeValue);
        return statusBadge.Text;
    }
}
```

The page object waits internally and returns the value; the step asserts on it with AwesomeAssertions. Locator strategy, the shared `ElementWaits` helper, the generated-markup caveat and the no-hard-waits rule live in #[[file:ui-automation-selenium.md]]; for a new screen, use the `build-selenium-page-object` skill instead of improvising here.

## Step 6 — Add the typed context and the test data builder

```csharp
// tests/QAAutomation.Tests.Api/VatReturnContext.cs
public sealed class VatReturnContext
{
    public string ReturnId { get; set; } = string.Empty;
    public string DeclaredByUserId { get; set; } = string.Empty;
    public IApiResponse<SubmitVatReturnResponse>? SubmitResponse { get; set; }
}
```

```csharp
// src/QAAutomation.Core/TestData/VatReturnBuilder.cs  — field names are (ASSUMPTION)
public sealed class VatReturnBuilder
{
    private readonly Faker faker = new Faker("en_GB");

    private string status = "Draft";
    private decimal netAmount = 1000.00m;

    public VatReturnBuilder WithStatus(string status)
    {
        this.status = status;
        return this;
    }

    public VatReturnBuilder WithNetAmount(decimal netAmount)
    {
        this.netAmount = netAmount;
        return this;
    }

    public VatReturnDraft Build()
    {
        string reference = $"VR-{this.faker.Random.AlphaNumeric(8).ToUpperInvariant()}";
        return new VatReturnDraft(reference, this.status, this.netAmount);
    }
}
```

Rules: contexts hold test state only — never a driver, client or connection. Builders never assert. Data is created inside its own tenant with a run-scoped identifier so parallel runs cannot collide.

The `Faker` above comes from `Bogus`, which is a **reserved** package in the current template — activate the 35.6.5 pin in `Directory.Packages.props` before using it, or hand-write the values in the builder until then. See #[[file:tech-stack.md]].

## Step 7 — Register everything in DI

One `[ScenarioDependencies]` factory per test project, in a `*ScenarioDependencies.cs` file at that test project's root — Reqnroll only finds the attribute inside a binding assembly, so a factory in `Core` is never discovered. Shared registrations stay in `src/*` as `IServiceCollection` extension methods the factory calls. Do not scatter registrations across step classes.

```csharp
// tests/QAAutomation.Tests.Api/ApiScenarioDependencies.cs
public static class ApiScenarioDependencies
{
    [ScenarioDependencies]
    public static IServiceCollection CreateServices()
    {
        IServiceCollection services = new ServiceCollection();

        // Scoped: one fresh instance per scenario.
        services.AddScoped<VatReturnContext>();
        services.AddScoped<TenantContext>();
        services.AddScoped<VatReturnClient>();
        services.AddScoped<VatReturnBuilder>();

        // Refit interface over a named HttpClient; the handler attaches the token.
        services
            .AddRefitClient<IVatReturnApi>()
            .ConfigureHttpClient(ConfigureApiBaseAddress)
            .AddHttpMessageHandler<BearerTokenHandler>();

        return services;
    }

    private static void ConfigureApiBaseAddress(HttpClient httpClient)
    {
        // Base address binds from configuration; never a literal URL here.
        string baseAddress = ApiOptions.Current.BaseAddress;
        httpClient.BaseAddress = new Uri(baseAddress);
    }
}
```

| Lifetime | What belongs there |
| --- | --- |
| Scoped (per scenario) | typed contexts, page objects, client wrappers, builders, **`IWebDriver` and its `ElementWaits`** |
| Shared for the run | bound configuration, driver options assembled once, token cache (and the WireMock server once that is activated) |
| Never registered | per-scenario mutable state as a singleton; `IWebDriver` as a singleton — it is not thread-safe; static anything |

Credentials for the token handler come from environment variables or the CI secret store. Committed configuration carries `YOUR_API_KEY_HERE` and nothing else.

## Step 8 — Review gate

Refuse to hand over the scaffold until all of these hold.

| # | Check |
| --- | --- |
| 1 | Folder and file names match #[[file:project-structure.md]]; namespaces mirror folders. |
| 2 | Exactly one layer tag, matching the owning project; at least one suite tag and one feature-area tag. |
| 3 | No locator, URL, HTTP verb, status code, SQL or JSON in the feature file. |
| 4 | No locator, raw HTTP, SQL or `Thread.Sleep` in the steps class; every step body under roughly ten lines. |
| 5 | Page object asserts nothing; client asserts nothing and generates no data. |
| 6 | Tenant is an explicit parameter with a guard, and at least one `@multitenant` negative scenario exists for a tenancy-sensitive capability. |
| 7 | All collaborators resolved by constructor injection and registered once in `[ScenarioDependencies]`. |
| 8 | No LINQ outside the three exceptions; explicit types; named `const` for literals. |
| 9 | No secret, real tenant name or customer-shaped value anywhere. |
| 10 | Every invented route, header, field and status is marked **(ASSUMPTION)** and listed in the deliverable. |

## Deliverable shape

Return, in this order:

1. The capability name, chosen layer and the reason that layer can prove the rule.
2. The artefact list with intended file paths.
3. The `.feature` snippet.
4. The `*Steps.cs` snippet.
5. The page object or client (plus contracts) snippet.
6. The typed context and builder snippets.
7. The DI registration delta — only the lines being added.
8. The assumptions table: every route, header, field, status and role name to confirm, with who owns the answer.
