---
inclusion: fileMatch
fileMatchPattern:
  - "**/Clients/**/*.cs"
  - "**/Api/**/*Client.cs"
  - "**/*Client.cs"
  - "**/Contracts/**/*.cs"
---

# API automation conventions

Rules for every API client and contract in `src/QAAutomation.Api`. Pinned packages: #[[file:tech-stack.md]]. Layout, naming and layer rules: #[[file:project-structure.md]]. What a step may call: #[[file:step-definitions.md]]. C# readability rules (no LINQ, explicit types): #[[file:code-style.md]].

**Scope note for the current template:** only `Refit` 16.3.0 and `AwesomeAssertions` 9.6.0 are active. `RestSharp`, `WireMock.Net`, `Polly`, `Respawn`, `Bogus` and `Reqnroll.Verify` are **reserved** — pinned and documented in #[[file:tech-stack.md]], but not wired in, because the current target has no database, no tenancy and no third-party boundary. Sections below that depend on them (3, 6, 7) describe the KEYinfinity target and are the plan, not the present state. Do not add a reserved package without activating it in `Directory.Packages.props` at its pinned version.

A client's job is to make a request and hand back a typed response. It does not assert, does not generate test data, and does not know what a scenario is trying to prove.

## 1. Refit is the default contract surface

| Approach | Use when | Notes |
| --- | --- | --- |
| `Refit` typed interface `I*Api` — **default** | Every stable endpoint the suite calls | Compile-time contract; a breaking API change becomes a build error, not a run-time 400. |
| `RestSharp` — alternative | A request must be composed dynamically (fuzzing headers, malformed payloads, content-type abuse) | Acceptable, but the compile-time contract is lost — say so in a comment. |
| Plain `HttpClient` / `IHttpClientFactory` | One-off probes, health checks | Never for a journey covered by a feature file. |

1. Declare the interface as `I*Api` in `Clients/`; wrap it in a `*Client` class that steps actually inject. The wrapper is where context (tenant, correlation id) and cleanup bookkeeping live.
2. Request and response types are records in `Contracts/`, named `*Request` / `*Response`. No anonymous types, no `dynamic`, no `JObject` traversal.
3. Return `IApiResponse<T>` (not bare `T`) wherever the test needs the status code or headers — which, for negative testing, is nearly always. Refit only throws when you ask it to; prefer inspecting the response over catching exceptions.
4. `HttpClient` instances come from `IHttpClientFactory` registered once in `Support`. A client never news up an `HttpClient` and never holds a static one.
5. Base URLs, timeouts and retry limits bind from configuration (`*Options` in `Configuration/`). No literal URLs in a client body.

```csharp
public interface IVatReturnApi
{
    [Get("/vat-returns/{returnId}")]
    Task<IApiResponse<VatReturnResponse>> GetAsync(
        [Header("X-Tenant-Id")] string tenantId,
        string returnId,
        [Header("X-Correlation-Id")] string correlationId,
        CancellationToken cancellationToken);

    [Post("/vat-returns/{returnId}/submit")]
    Task<IApiResponse<SubmitVatReturnResponse>> SubmitAsync(
        string returnId,
        [Body] SubmitVatReturnRequest request,
        [Header("X-Correlation-Id")] string correlationId,
        CancellationToken cancellationToken);
}
```

The route above is **illustrative**. Do not treat any path, header name or payload shape in this file as the real KEYinfinity contract until it is confirmed against the API specification.

## 2. Authentication: acquire once, cache, never leak

1. Acquire a token through the real auth flow once per role (and per tenant) per test run, in `[BeforeTestRun]`, and cache it in a scoped token provider registered in DI.
2. The token provider owns expiry: it refreshes on a clock check, not on a 401 retry loop. A 401 must be allowed to surface — it is frequently the thing under test.
3. Attach the token with a `DelegatingHandler` on the named `HttpClient`, so no client method handles headers and no step can forget one.
4. One cached token per role. Permission scenarios select a role explicitly; they never reuse "whatever token the last scenario had".
5. Credentials come from environment variables or the CI secret store — see the security section below.

## 3. Multi-tenant request context — explicit and asserted

KEYinfinity is multi-tenant SaaS. The most expensive bug class here is not a 500, it is tenant A seeing tenant B's data. A suite that resolves tenancy implicitly cannot detect that.

1. The tenant is a **required, explicit parameter** on the client wrapper. There is no ambient default, no "current tenant" static, no fallback to a config value when the caller omits it.
2. Whatever carries tenancy — a header, a claim in the token, or a path segment — it is set deliberately per request and named as a constant, not spelled inline.
3. **Assert on it.** Responses are checked for tenant-scoped identifiers, not just for a 200. A test that only checks the status code cannot tell a correct response from another tenant's.
4. Every tenancy-sensitive capability carries a `@multitenant` cross-tenant negative scenario: authenticate as tenant A, request a resource belonging to tenant B, and assert the API refuses (`404` or `403` — confirm which, and assert the agreed one, not "either").
5. A cross-tenant scenario asserts on the body too: the refusal must not echo the other tenant's data, names or identifiers in the error payload.
6. Test data is created inside its own tenant and never reused across tenants.

```csharp
public sealed class VatReturnClient
{
    private const string TenantHeaderName = "X-Tenant-Id";

    private readonly IVatReturnApi vatReturnApi;

    public VatReturnClient(IVatReturnApi vatReturnApi)
    {
        this.vatReturnApi = vatReturnApi;
    }

    // tenantId is required by design: there is no ambient tenant.
    public async Task<IApiResponse<VatReturnResponse>> GetAsync(
        string tenantId,
        string returnId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("A tenant must be supplied explicitly.", nameof(tenantId));
        }

        return await this.vatReturnApi.GetAsync(tenantId, returnId, correlationId, cancellationToken);
    }
}
```

## 4. Assertions: status code **and** body

A status-code-only assertion is the API equivalent of checking the page loaded. Both layers are mandatory, and both live in step definitions, never in the client.

| Layer | Assert | Example concern |
| --- | --- | --- |
| Transport | Exact status code; `Location` on create; `ETag`/concurrency headers where used | `201 Created`, not "2xx" |
| Body | Business fields, with values traced to the scenario's expected outcome | VAT box totals, obligation period, reconciliation status |
| Shape | Required fields present, types correct, no unexpected nulls | Contract drift from a model-driven regeneration |
| Errors | Error code and message shape for negative cases | Stable machine-readable code, no stack trace leaked |

1. Assert the **exact** expected status code. `Should().BeSuccessful()` hides a 204 that should have been a 201.
2. Assert business fields individually with a `because` reason naming the rule. Group related checks in one `AssertionScope` rather than splitting a single concern over several `Then` steps.
3. For wide report-shaped payloads (VAT return boxes, reconciliation summaries) use snapshot assertions via `Reqnroll.Verify` instead of thirty field assertions. Approved snapshots are reviewed like code.
4. Deserialise into the `Contracts/` record and assert on properties. Do not assert against a raw JSON string, and do not walk a JSON tree with LINQ — see #[[file:code-style.md]].
5. When iterating a response collection to find the line under test, write an explicit `foreach` with a `break` into a named variable. The single-comparison `Should().Contain(line => line.BoxNumber == 1)` form is the only predicate allowed.
6. Error responses must be asserted on as deliberately as success responses: no stack traces, no internal paths, no SQL fragments in a user-facing message.

## 5. Negative and authorisation testing are first-class

The `@permissions` journey is not an afterthought appended once the happy paths pass. Authorisation is server-side behaviour, and only an API test can prove it independently of what the UI chose to render.

1. For every capability, cover the authorisation matrix: permitted role succeeds, under-privileged role is refused, no token is refused, expired token is refused, and token-for-another-tenant is refused.
2. Assert on the **agreed** refusal code per case — 401 for unauthenticated, 403 for unauthorised, and whatever the API contract specifies for cross-tenant (often 404 to avoid leaking existence). Confirm the contract; do not accept "any 4xx".
3. A refusal must change nothing. Follow a refused write with a read that proves the state is untouched.
4. Cover input-validation negatives as a matter of course: missing required fields, wrong types, out-of-range values, oversized payloads, and the boundary rows named by #[[file:test-design-techniques.md]].
5. UI permission tests check that a control is hidden; API permission tests check the operation is actually refused. Both are required — hiding a button is not an access control.
6. Never weaken a check to make a test pass. If a security assertion fails, that is a finding, not a flaky test.

## 6. Third-party boundaries and WireMock.Net

**CI must never call a real tax authority.** No HMRC production endpoint, no live MTD submission, no real sandbox credentials baked into a pipeline. A submission is a legally meaningful act; an automated suite retrying one is an incident.

1. Stub every third-party boundary — HMRC/MTD, payment providers, bank feeds, email — with `WireMock.Net` started once in `[BeforeTestRun]` and reset per scenario.
2. The system under test points at the stub through configuration. If an environment cannot be pointed away from a real third party, that environment is not an automation target.
3. Stub mappings live in `Core/Support` or alongside the capability's test data, named for the behaviour they model: accepted submission, duplicate submission, rate-limited, upstream unavailable, malformed response.
4. Model failure modes deliberately. The interesting MTD coverage is timeout, 429, 503 and a rejection payload — not the happy path.
5. Verify the outbound request: assert the stub was called with the expected body and headers, so a "submission succeeded" scenario proves something actually left the building. WireMock.Net matcher lambdas are an allowed exception under #[[file:code-style.md]].
6. Any genuine contract check against a real sandbox is a separate, manually triggered, clearly tagged job with its own credentials — never part of the CI regression run.
7. Record the fidelity risk: a stub only proves we handle the response we *think* the third party sends. Keep stub payloads traceable to published documentation and review them when that documentation changes.

## 7. Idempotency, cleanup and correlation

1. Every scenario creates its own data with a unique, run-scoped identifier (`Bogus` plus a run id) so parallel runs and reruns cannot collide.
2. Never assert on pre-existing environment data. If a scenario needs a thing to exist, the scenario creates it.
3. Clean up via the API that created the data, in `[AfterScenario]`, and make teardown tolerant of a partially created state. For database-backed suites, `Respawn` handles the reset — see #[[file:tech-stack.md]].
4. Where the API supports an idempotency key, send one and cover the duplicate-request case explicitly — especially for payments and MTD submissions, where a double post is a real-world defect.
5. Use `Polly` for legitimate async settle: polling an obligation status or a reconciliation job until it reaches a terminal state, with a bounded timeout. Never use it to retry a failed assertion, and never to paper over a flaky endpoint.
6. Generate one correlation id per scenario, send it on every request from every layer, log it with the scenario name, and attach it to the test report. When a UI scenario fails, that id is what makes the server-side logs findable.

## 8. Security — the obvious failure mode

Test automation is a notorious source of leaked credentials: tokens pasted into a client for a quick debug, a connection string committed in an `appsettings` file, a full request log attached to a public build artefact. Treat this section as non-negotiable.

1. **Never hardcode** a token, API key, password, client secret or connection string — not in a client, not in a feature file, not in a comment, not "temporarily".
2. Committed configuration and every snippet in this repository use obvious placeholders: `YOUR_API_KEY_HERE`. Real values come from environment variables or the Azure DevOps secret store, injected at run time.
3. Never log an `Authorization` header, bearer token, cookie, or unmasked PII. If a client logs requests, it masks secrets first and masks by allow-list, not by blocklist.
4. Never attach a raw request/response dump to a report without masking. Build artefacts are frequently more widely readable than people assume.
5. Use TLS for everything. Do not disable certificate validation, even against a test environment — a stub is the answer to a certificate problem, not `ServerCertificateCustomValidationCallback`.
6. Credentials are scoped and short-lived: a test account with only the permissions its scenarios need, rotated on a schedule, never a shared admin account.
7. Use realistic-but-generated data (`Bogus`). Never copy production data, taxpayer references or customer records into a test.
8. If a secret is ever committed, treat it as compromised: rotate it, then clean history. Removing the line is not a fix.

## Assumptions to confirm

- Every route, header name (`X-Tenant-Id`, `X-Correlation-Id`) and payload shape in this file is **illustrative**, inferred from the brief rather than read from a specification. Confirm the real contract before writing clients.
- Whether tenancy travels in a header, a token claim or a path segment is **unconfirmed** and changes the handler design.
- The expected refusal code for a cross-tenant read (403 versus 404) is a product decision; confirm it before asserting on one.
- Whether an MTD sandbox is available at all, and under what credential policy, is **unconfirmed** — see the open questions in #[[file:domain-keyinfinity.md]].
