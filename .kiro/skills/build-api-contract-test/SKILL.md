---
name: build-api-contract-test
description: Use when the user asks to write or review an API test for an endpoint — the Refit typed interface and contracts, token acquisition and caching, explicit tenant context, happy path, boundary cases and authorisation-denied negatives.
---

# Build a Refit-based API contract test

Takes one endpoint and produces the complete API-layer test set for it: the typed contract, the client wrapper, the auth plumbing, the feature file and the step definitions — covering the happy path, the boundaries, the authorisation matrix and cross-tenant refusal.

Output is **markdown snippets with intended file paths**. Do not write `.cs`, `.feature` or project files from this skill unless the user explicitly asks for files.

Client, contract, auth, tenancy, assertion and stubbing rules: #[[file:api-automation.md]]. Binding rules and DI: #[[file:step-definitions.md]]. Gherkin and tags: #[[file:bdd-gherkin-standards.md]]. Coverage techniques: #[[file:test-design-techniques.md]]. Layout: #[[file:project-structure.md]]. Pins: #[[file:tech-stack.md]] — note that only `Refit` and `AwesomeAssertions` are **active** in the current template; `WireMock.Net`, `Polly` and `Bogus` are reserved and must be activated at their pinned versions before use. Readability: #[[file:code-style.md]]. Domain: #[[file:domain-keyinfinity.md]].

Four standing rules:

1. **Never a real secret.** Every snippet uses `YOUR_API_KEY_HERE`. Tokens, passwords and connection strings come from environment variables or the CI secret store at run time.
2. **Tenant is explicit.** No ambient tenant, no "current tenant" static, no config fallback. And it is **asserted on**, not just sent.
3. **CI never calls a real tax authority.** HMRC/MTD, payment providers and bank feeds are stubbed with `WireMock.Net`.
4. **Routes, headers, payload shapes and status codes are assumptions** until read from the API specification. Mark them **(ASSUMPTION)** and list them.

## Step 1 — Capture the endpoint facts

| Record | Notes |
| --- | --- |
| Capability and feature-area tag | `VatReturnSubmission`, `@vat @mtd` |
| Method and route | **(ASSUMPTION)** unless quoted from the spec |
| Request and response shape | field names, types, required vs optional |
| Expected success status | the **exact** code: `200`, `201` with `Location`, `202`, `204` |
| Tenancy carrier | header, token claim or path segment — **unconfirmed**, and it changes the handler design |
| Roles permitted | from the role model, not guessed |
| Agreed refusal codes | 401 unauthenticated, 403 unauthorised, cross-tenant 403 **or** 404 — confirm which |
| Third-party calls behind it | each one needs a WireMock mapping |
| Idempotency | is there a key, and what does a duplicate return |

Anything you cannot source goes into the assumptions table and is written as `<confirm>` in the test, not filled in with a plausible value.

## Step 2 — Declare the typed contract

Records in `Contracts/`, named `*Request` / `*Response`. No anonymous types, no `dynamic`, no JSON-tree walking.

```csharp
// src/QAAutomation.Api/Contracts/SubmitVatReturnRequest.cs
namespace QAAutomation.Api.Contracts;

public sealed record SubmitVatReturnRequest(
    string ReturnId,
    string ObligationPeriodKey,
    string DeclaredByUserId);

// src/QAAutomation.Api/Contracts/SubmitVatReturnResponse.cs
public sealed record SubmitVatReturnResponse(
    string ReturnId,
    string TenantId,
    string Status,
    string ReceiptReference);

// src/QAAutomation.Api/Contracts/ApiErrorResponse.cs
public sealed record ApiErrorResponse(string Code, string Message);
```

The Refit interface returns `IApiResponse<T>`, never a bare `T` — negative testing needs the status code and headers, and Refit only throws if you ask it to.

```csharp
// src/QAAutomation.Api/Clients/IVatReturnApi.cs
// Routes and header names below are (ASSUMPTION) — confirm against the API specification.
public interface IVatReturnApi
{
    [Get("/vat-returns/{returnId}")]
    Task<IApiResponse<VatReturnResponse>> GetAsync(
        [Header(TenantHeader)] string tenantId,
        string returnId,
        [Header(CorrelationHeader)] string correlationId,
        CancellationToken cancellationToken);

    [Post("/vat-returns/{returnId}/submit")]
    Task<IApiResponse<SubmitVatReturnResponse>> SubmitAsync(
        [Header(TenantHeader)] string tenantId,
        string returnId,
        [Body] SubmitVatReturnRequest request,
        [Header(IdempotencyHeader)] string idempotencyKey,
        [Header(CorrelationHeader)] string correlationId,
        CancellationToken cancellationToken);

    private const string TenantHeader = "X-Tenant-Id";
    private const string CorrelationHeader = "X-Correlation-Id";
    private const string IdempotencyHeader = "Idempotency-Key";
}
```

If the compiler version in use does not allow `const` members on an interface, put the header names in a `static class ApiHeaderNames` in `Core/Utilities` and reference them from the attributes.

## Step 3 — Wrap it in a client that demands a tenant

```csharp
// src/QAAutomation.Api/Clients/VatReturnClient.cs
public sealed class VatReturnClient
{
    private readonly IVatReturnApi vatReturnApi;

    public VatReturnClient(IVatReturnApi vatReturnApi)
    {
        this.vatReturnApi = vatReturnApi;
    }

    // tenantId is required by design: there is no ambient tenant anywhere in this suite.
    public async Task<IApiResponse<SubmitVatReturnResponse>> SubmitAsync(
        string tenantId,
        string returnId,
        SubmitVatReturnRequest request,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("A tenant must be supplied explicitly.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("An idempotency key must be supplied.", nameof(idempotencyKey));
        }

        return await this.vatReturnApi.SubmitAsync(
            tenantId, returnId, request, idempotencyKey, correlationId, cancellationToken);
    }
}
```

The wrapper holds request context and cleanup bookkeeping. It never asserts, never generates test data, never builds a base URL, and never news up an `HttpClient`.

## Step 4 — Authentication: acquire once, cache, never leak

A token provider acquires a token per **role and tenant**, caches it, and refreshes on a clock check — never on a 401 retry loop, because a 401 is frequently the thing under test.

```csharp
// src/QAAutomation.Core/Support/TokenProvider.cs
public sealed class TokenProvider
{
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(2);

    private readonly Dictionary<string, CachedToken> tokensByRoleAndTenant = new();
    private readonly IAuthApi authApi;
    private readonly AuthOptions authOptions;

    public TokenProvider(IAuthApi authApi, AuthOptions authOptions)
    {
        this.authApi = authApi;
        this.authOptions = authOptions;
    }

    public async Task<string> GetTokenAsync(string role, string tenantId, CancellationToken cancellationToken)
    {
        string cacheKey = $"{role}|{tenantId}";

        CachedToken? cached = null;
        if (this.tokensByRoleAndTenant.ContainsKey(cacheKey))
        {
            cached = this.tokensByRoleAndTenant[cacheKey];
        }

        if (cached is not null && cached.ExpiresAtUtc - RefreshMargin > DateTime.UtcNow)
        {
            return cached.AccessToken;
        }

        // Credentials are supplied by the environment / CI secret store.
        // Committed configuration carries YOUR_API_KEY_HERE and nothing else.
        TokenRequest request = new TokenRequest(
            this.authOptions.ClientId,
            this.authOptions.ClientSecret,
            role,
            tenantId);

        IApiResponse<TokenResponse> response = await this.authApi.GetTokenAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode || response.Content is null)
        {
            // Fail closed, and say nothing about the credential itself.
            throw new InvalidOperationException(
                $"Token acquisition failed for role '{role}' with status {(int)response.StatusCode}.");
        }

        TokenResponse body = response.Content;
        CachedToken fresh = new CachedToken(
            body.AccessToken,
            DateTime.UtcNow.AddSeconds(body.ExpiresInSeconds));

        this.tokensByRoleAndTenant[cacheKey] = fresh;
        return fresh.AccessToken;
    }
}

public sealed record CachedToken(string AccessToken, DateTime ExpiresAtUtc);
```

The token is attached by a `DelegatingHandler`, so no client method handles headers and no step can forget one.

```csharp
// src/QAAutomation.Core/Support/BearerTokenHandler.cs
public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly TokenProvider tokenProvider;
    private readonly RequestIdentity requestIdentity;   // scoped: role + tenant for this scenario

    public BearerTokenHandler(TokenProvider tokenProvider, RequestIdentity requestIdentity)
    {
        this.tokenProvider = tokenProvider;
        this.requestIdentity = requestIdentity;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (this.requestIdentity.IsAnonymous)
        {
            // Deliberately unauthenticated: the 401 case is a test, not a mistake.
            return await base.SendAsync(request, cancellationToken);
        }

        string token = await this.tokenProvider.GetTokenAsync(
            this.requestIdentity.Role, this.requestIdentity.TenantId, cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
```

| Rule | Why |
| --- | --- |
| Acquire once per role and tenant per run, in `[BeforeTestRun]` | login is not the thing under test here |
| Refresh on expiry, never on a 401 | a 401 must be allowed to surface |
| Never log the `Authorization` header, token, cookie or unmasked PII | build artefacts are read more widely than people assume |
| Never disable certificate validation | a stub is the answer to a certificate problem |
| Test accounts are scoped and short-lived | no shared admin account |
| `YOUR_API_KEY_HERE` in every committed file and snippet | a committed secret is a compromised secret |

Use the explicit-anonymous and explicit-expired paths (`RequestIdentity`) rather than deleting the handler for negative tests, so the unauthenticated case is a first-class scenario.

## Step 5 — Make tenancy an asserted outcome

A 200 proves the request worked. It does not prove the response belonged to the caller. That distinction is the highest-severity defect class in a multi-tenant product.

```gherkin
# tests/QAAutomation.Tests.Api/Features/VatReturnSubmission.feature
@api @regression @vat @mtd
Feature: VAT return submission
  # Coverage: happy path, amount boundaries, authorisation matrix, cross-tenant refusal.
  # Not covered: rejection payload handling, fraud prevention header contents (ASSUMPTION — unspecified).

  @smoke
  Scenario: Submitting a finalised return returns a receipt scoped to the caller's tenant
    Given the accountant is authenticated for the primary tenant
    And the primary tenant has a finalised VAT return for an open obligation
    When the accountant submits the return
    Then the submission is accepted
    And the response is scoped to the primary tenant

  @multitenant
  Scenario: A return belonging to another tenant cannot be read
    Given the accountant is authenticated for the primary tenant
    And a finalised VAT return exists in the secondary tenant
    When the accountant requests that return
    Then the request is refused as not found
    And the error body contains no data from the secondary tenant
```

```csharp
[Then("the response is scoped to the primary tenant")]
public void ThenTheResponseIsScopedToThePrimaryTenant()
{
    IApiResponse<SubmitVatReturnResponse> response = this.vatReturnContext.SubmitResponse;
    SubmitVatReturnResponse body = response.Content;

    using (new AssertionScope())
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "an accepted submission returns 200 (ASSUMPTION: confirm 200 vs 202)");
        body.TenantId.Should().Be(this.tenantContext.PrimaryTenantId,
            because: "a response must never carry another tenant's identifier");
        body.ReceiptReference.Should().NotBeNullOrWhiteSpace(
            because: "an accepted submission must be auditable");
    }
}

[Then("the error body contains no data from the secondary tenant")]
public async Task ThenTheErrorBodyContainsNoDataFromTheSecondaryTenant()
{
    IApiResponse<VatReturnResponse> response = this.vatReturnContext.GetResponse;

    response.StatusCode.Should().Be(HttpStatusCode.NotFound,
        because: "the agreed cross-tenant contract hides existence (<confirm> 403 vs 404)");

    string errorPayload = await response.Error!.GetContentAsAsync<string>() ?? string.Empty;
    errorPayload.Should().NotContain(this.tenantContext.SecondaryTenantId,
        because: "a refusal must not leak the other tenant's identifier");
    errorPayload.Should().NotContain(this.vatReturnContext.SecondaryTenantReturnReference,
        because: "a refusal must not confirm the resource exists");
}
```

Mandatory tenancy coverage for any tenancy-sensitive endpoint: cross-tenant read refused, cross-tenant write refused with a follow-up read proving no change, list/search scoped including counts and pagination totals, and reports scoped.

## Step 6 — Cover the happy path properly

| Layer | Assert |
| --- | --- |
| Transport | the exact status code; `Location` on create; `ETag`/concurrency headers where used |
| Body | each business field, with a `because` naming the rule |
| Shape | required fields present, types correct, no unexpected nulls — this is how model-driven contract drift is caught |
| Side effect | a follow-up read, or a verified outbound call to the stub |

1. Assert the exact code. `Should().BeSuccessful()` hides a 204 that should have been a 201.
2. For wide report-shaped payloads (VAT boxes, reconciliation summaries, aged debt) use `Reqnroll.Verify` snapshots instead of thirty field assertions. Approved snapshots are reviewed like code.
3. Where the endpoint calls a third party, **verify the outbound request** against the WireMock stub, so "submission succeeded" proves something actually left the building.

```csharp
// Stub mapping — HMRC/MTD stands behind WireMock.Net; CI never calls a real tax authority.
// Matcher lambdas are an allowed exception under code-style.md.
this.wireMockServer
    .Given(Request.Create()
        .WithPath("/organisations/vat/*/returns")   // (ASSUMPTION) confirm against HMRC documentation
        .UsingPost())
    .RespondWith(Response.Create()
        .WithStatusCode(201)
        .WithBodyAsJson(new { processingDate = "2026-01-31T00:00:00.000Z", formBundleNumber = "<confirm>" }));
```

Model the failure modes deliberately — timeout, 429, 503, duplicate submission, rejection payload. For MTD that is where the value is; the happy path is the easy part.

## Step 7 — Derive the boundary cases

Partition the inputs, then take the three values either side of every limit. Use a `Scenario Outline` with technique-named `Examples` tables, valid and invalid rows in separate tables. Technique detail: #[[file:test-design-techniques.md]].

```gherkin
  Scenario Outline: A submitted figure is accepted or refused according to its value
    Given the accountant is authenticated for the primary tenant
    And the primary tenant has a finalised VAT return for an open obligation
    When the accountant submits the return with a net figure of <netFigure>
    Then the submission outcome is "<outcome>"

    Examples: accepted figure classes (equivalence partitions)
      | netFigure | outcome  |
      | 0.00      | accepted |
      | 1000.00   | accepted |

    Examples: figure limits (BVA)
      | netFigure            | outcome  |
      | maximum minus 0.01   | accepted |
      | maximum              | accepted |
      | maximum plus 0.01    | refused  |
      | -0.01                | refused  |
```

Resolve descriptors such as `maximum` in a `*Builder` or a named resolver in `Core/Utilities`, so the table stays readable and the limit lives in one place. The maximum itself is `<confirm>` until sourced.

Also cover as a matter of course: missing required fields, wrong types, oversized payload, malformed body, unknown enum value, and the duplicate request where an idempotency key exists.

## Step 8 — Cover the authorisation matrix

Not an afterthought. Authorisation is server-side behaviour, and only an API test can prove it independently of what the UI chose to render.

| Case | Identity | Expected | Note |
| --- | --- | --- | --- |
| Permitted role | role with the permission | success code | the happy path |
| Under-privileged role | authenticated, lacks the permission | `403` | confirm the contract |
| No token | anonymous | `401` | use the explicit anonymous identity |
| Expired token | authenticated, token past expiry | `401` | force expiry; do not let the provider refresh |
| Token for another tenant | valid token, other tenant's resource | `403` or `404` — **confirm which** | assert the agreed one, never "any 4xx" |
| Refused write changes nothing | any refusal above on a write | follow-up read proves state untouched | mandatory |

```gherkin
  Scenario Outline: Only a permitted role may submit a VAT return
    Given a user authenticated as "<role>" for the primary tenant
    And the primary tenant has a finalised VAT return for an open obligation
    When that user submits the return
    Then the request is refused with "<refusal>"
    And the return remains finalised

    Examples: authorisation matrix (decision table)
      | role            | refusal   |
      | read-only user  | forbidden |
      | property manager| forbidden |
```

Role names are **(ASSUMPTION)** — the real role model is an open question in #[[file:domain-keyinfinity.md]]. Never weaken a security assertion to make a test pass; a failing authorisation check is a finding.

## Step 9 — Isolation, cleanup and correlation

1. Each scenario creates its own data, in its own tenant, with a run-scoped identifier from `Bogus` plus a run id. Never assert on pre-existing environment data.
2. Clean up in `[AfterScenario]` through the API that created the data, tolerant of a partially created state. Database-backed suites reset with `Respawn`.
3. Use `Polly` for legitimate async settle — polling an obligation or job until a terminal state, with a bounded timeout. Never to retry a failed assertion.
4. One correlation id per scenario, sent on every request, logged with the scenario name and attached to the report.

## Step 10 — Review gate

| # | Check |
| --- | --- |
| 1 | No secret anywhere; every placeholder is `YOUR_API_KEY_HERE`. |
| 2 | Contracts are records in `Contracts/`; interface is `I*Api`; wrapper is `*Client`. |
| 3 | `IApiResponse<T>` returned wherever a status code or header is asserted. |
| 4 | Tenant is an explicit guarded parameter, and the response's tenant scoping is asserted. |
| 5 | At least one `@multitenant` cross-tenant negative, asserting the body leaks nothing. |
| 6 | Exact status codes asserted; no `BeSuccessful()`, no "any 4xx". |
| 7 | Full authorisation matrix present, including the refused-write-changes-nothing check. |
| 8 | Boundary rows derived from named partitions, valid and invalid in separate tables. |
| 9 | Every third party stubbed with `WireMock.Net`; outbound request verified. |
| 10 | No `Thread.Sleep`, no retry-until-pass, no certificate validation disabled. |
| 11 | No LINQ outside the three exceptions; explicit types; no JSON-tree walking. |
| 12 | Every route, header, field, status code and role is marked **(ASSUMPTION)** or sourced. |

## Deliberate limits

1. A stub only proves we handle the response we *think* the third party sends. Keep stub payloads traceable to published documentation and review them when that documentation changes.
2. Any genuine contract check against a real sandbox is a separate, manually triggered, clearly tagged job with its own credentials — never part of the CI regression run.

## Deliverable shape

Return, in this order:

1. The endpoint facts table from Step 1, with unsourced rows marked.
2. The contract records and the `I*Api` interface.
3. The `*Client` wrapper.
4. The auth plumbing delta — token provider and handler changes only.
5. The feature file: happy path, boundary outline, authorisation outline, tenancy negatives.
6. The step definitions, with assertions showing both status and body.
7. The WireMock mappings for every third party behind the endpoint.
8. The DI registration delta.
9. The assumptions table: routes, headers, field names, status codes, role names, limits — with who owns each answer.
