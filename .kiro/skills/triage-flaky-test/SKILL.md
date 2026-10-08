---
name: triage-flaky-test
description: Use when the user asks to diagnose an intermittent, cross-layer or unexplained test failure — a test that passes alone and fails in parallel, fails only in CI, or fails near a period boundary — and needs a root-cause classification and the matching fix rather than a retry.
---

# Triage a flaky test

A diagnostic playbook for the failure class this role exists to handle: intermittent, cross-layer, nobody-knows-why failures spanning generated code, test data, tenancy and infrastructure. The output is a **named root cause and the matching fix**, or a quarantine entry with a ticket. Never a retry.

Quarantine policy, banned mechanisms and the pipeline side: #[[file:ci-azure-devops.md]]. Waiting and evidence rules: #[[file:ui-automation-selenium.md]]. Isolation, cleanup and correlation: #[[file:api-automation.md]]. Hooks and async discipline: #[[file:step-definitions.md]]. Tenancy failure modes: #[[file:domain-keyinfinity.md]]. Readability: #[[file:code-style.md]].

## The ban

Stated first because it is the one rule that gets broken under delivery pressure.

| Banned | Why |
| --- | --- |
| `retryCountOnTaskFailure` on a test task | hides the failure and multiplies runtime |
| NUnit `[Retry]` on the scenario | same problem, moved into the code |
| `continueOnError: true` on a test stage | converts red to green with no record |
| Re-running the pipeline until it passes, then merging | the defect ships; the evidence is gone |
| Adding `Thread.Sleep` or `Task.Delay` to "stabilise" it | banned outright |
| Inflating a global timeout to make it pass | masks a race and slows every other test |
| Deleting or commenting out the scenario | loses the coverage and the evidence |

A re-run is a **diagnostic tool**, used deliberately to gather evidence, and its result is recorded. It is never a remediation. "It passed on retry" is the start of this playbook, not the end of it.

An intermittent failure in a multi-tenant financial product is a **product risk hypothesis** until disproven. Tests that fail under concurrency sometimes do so because the product is not safe under concurrency, and VAT submissions and payments are exactly where that matters.

## Step 1 — Capture the evidence before it expires

Artefact retention is short. Do this first, before any re-run that overwrites it.

| Collect | From |
| --- | --- |
| Scenario name, tags, owning project | the `.trx` and the feature file |
| Failure message and stack | `.trx` / test result. The Selenium exception type is the first clue: `WebDriverTimeoutException`, `StaleElementReferenceException`, `NoSuchElementException`, `ElementClickInterceptedException` |
| **Screenshot, page source, browser console log** | the scenario's folder under `test-results/`, or the published failure artefact |
| Driver log | the driver service log file, if enabled in driver options |
| Browser and driver version | the run log. Selenium Manager resolves the driver automatically, so a browser auto-update on the agent can change behaviour with no commit |
| Correlation id for the scenario | test log; then pull server-side logs for it |
| Run metadata | build number, stage attempt, agent, commit, environment, start time |
| Concurrency | worker count, which other scenarios ran alongside |
| Frequency | failures per runs, over how many days, and whether it is one scenario or several |
| Deployed SUT version | the test stage records it; a regeneration of generated code is a prime suspect |

**Selenium has no trace viewer.** There is no timeline, no DOM snapshot history and no network tab to open after the fact, so the three artefacts the suite captures itself — screenshot, page source, console log — are the entire evidence base. If they are missing, the first fix is the capture hook and the artefact publishing, not the test: nothing else can be diagnosed without them. See #[[file:ui-automation-selenium.md]] for what gets captured and #[[file:ci-azure-devops.md]] for publishing it on failure with a stage-attempt-suffixed artefact name.

## Step 2 — Reproduce deliberately

Run the experiments in this order and record pass/fail counts for each. The pattern across them is what identifies the class.

| # | Experiment | A failure here points at |
| --- | --- | --- |
| 1 | Run the single scenario alone, locally, 20 times | genuine timing bug or a real product race — not an isolation problem |
| 2 | Run the whole feature file alone | state leaking between scenarios in the same file, or a `Background` that varies |
| 3 | Run the project at CI's worker count | parallelism, shared tenant or shared data collision |
| 4 | Run at double CI's worker count | confirms a concurrency ceiling; tighten the lane |
| 5 | Run the scenario twice back to back in the same run | missing cleanup, or a non-idempotent operation |
| 6 | Run against the CI environment from a local machine | environment-specific: data, config, latency, tenant state |
| 7 | Run on the CI agent with artefact capture on and no other stages | agent resources, cold start, readiness |
| 8 | Run with the system clock near a period or day boundary | real-clock test data |
| 9 | **Re-run under forced serialisation** — worker count 1, or the feature in a serial lane | if it goes green, the cause is concurrency (class D) or a product race (class E), not timing |
| 10 | Re-run headed, locally, watching the screen | what the user would have seen at the moment it failed — often the fastest route to classes A and G |

Two rules: change one variable per experiment, and write the results into the ticket as a table. "Could not reproduce" is a result — it narrows the cause to CI-only factors and is worth recording.

## Step 3 — Read the Selenium artefacts properly

There is no trace to open. Read the four artefacts together — each one answers a different question, and it is the combination that identifies the class.

### 3a. The exception type

| Exception | What it actually means | Look next at |
| --- | --- | --- |
| `WebDriverTimeoutException` | the wait condition never became true inside the timeout | which locator and condition; then the screenshot and page source |
| `NoSuchElementException` | a **bare `FindElement`** was used on something not yet rendered — a missing wait | the page object: that call needs `ElementWaits` |
| `StaleElementReferenceException` | an element was held across a re-render | the page object: an `IWebElement` is being cached, or reused across a Vue update |
| `ElementClickInterceptedException` | something was on top — overlay, spinner, sticky header, toast | the screenshot: the covering element is usually visible in it |
| `ElementNotInteractableException` | present but disabled or zero-size | `WaitUntilClickable` is missing, or the app genuinely disabled the control |
| `InvalidSelectorException` | malformed selector, usually a hand-built XPath | the locator itself; it is almost certainly priority 3 |

### 3b. The screenshot

| Look for | Points at |
| --- | --- |
| A spinner still showing | waiting on the wrong thing, or the back end was slow (A or F) |
| An error banner or validation message | the product rejected the action; this may not be a flake at all |
| The wrong screen, or the login page | session or token expiry mid-scenario |
| A modal, cookie banner or toast over the target | `ElementClickInterceptedException` territory — dismiss it in the page object, do not click through it |
| An empty grid where rows were expected | data leakage (B), cross-tenant bleed (C), or an unfinished load |
| A visually different layout | generated-code drift (G) after a platform regeneration |

### 3c. The page source

| Look for | Points at |
| --- | --- |
| The `data-testid` is absent entirely | contract drift (G) — the front end dropped or renamed it |
| The id is present but the element is hidden | the wait condition was wrong, not the locator |
| The element is present with a **different** test id | a rename that did not come through review |
| Structural shape changed around a priority-3 selector | regeneration invalidated it (G); replace it with an agreed test id |
| Rows present but in a different order | an index-based locator; match on business content instead |

### 3d. The browser console log

| Look for | Points at |
| --- | --- |
| An unhandled front-end exception at the failure timestamp | a genuine product defect, not a test problem |
| A failed XHR — 401, 403, 5xx | session expiry, permissions, or a back-end fault (F or a real bug) |
| A Vue hydration or reactivity warning | the re-render that caused the stale element |
| Nothing at all | the front end was fine; look at timing and concurrency instead |

Three high-value readings for this product:

1. **`StaleElementReferenceException` on a grid row.** The row was re-rendered between resolving it and acting on it. Fix: hold the `By`, resolve at the point of use, wait for the container to settle first, and match rows by business content instead of position. Do **not** wrap it in a `catch`.
2. **`WebDriverTimeoutException` with a spinner in the screenshot and a pending XHR in the console log.** The UI was waiting on the back end. Fix: wait on the app's own completion signal, or settle via the API from a step — not a longer UI timeout.
3. **The login page in the screenshot mid-scenario.** Session or token expiry. Fix: re-establish the session in a hook; never retry inside a page object.

When comparing several failures: if the same locator and the same condition time out every time, it is a deterministic race or a contract problem. If the failing step moves around, it is resource contention or concurrency.

## Step 4 — Classify the root cause

Work down the table. The first row whose signature matches is the class; do not stop at the first plausible one if a later signature matches better.

| Class | Signature | Confirm by |
| --- | --- | --- |
| **A. Timing / wait** | fails alone too; `NoSuchElementException`, `StaleElementReferenceException`, `ElementNotInteractableException`, or a `WebDriverTimeoutException` on the wrong condition | experiment 1 fails; grep the page object for a bare `FindElement`, a `Thread.Sleep`, a `catch (StaleElementReferenceException)`, or a `Displayed` check used as an assertion |
| **B. Test-data leakage** | passes first, fails on the second run in the same environment; fails when the feature runs whole | experiment 5 or 2 fails; the scenario asserts on data it did not create, or cleanup is missing |
| **C. Cross-tenant bleed** | counts, totals, list contents or search results differ by run; a record from another tenant appears | assert on tenant-scoped identifiers, re-run with both tenants seeded; **treat as a product defect until proven otherwise** |
| **D. Shared-state collision** | passes alone, fails in parallel; worse at higher worker counts | experiments 3 and 4; two scenarios share a tenant, a reference sequence, a Respawn target or a WireMock mapping |
| **E. Genuine product race** | fails under concurrency with a server-side error, duplicate record, lost update or inconsistent total; correlation id shows two overlapping writes | server logs for the correlation id; try to reproduce the race by API alone — if you can, it is a bug report, not a test fix |
| **F. Environment / infrastructure** | fails on the first run of the day, after a deploy, or in bursts across unrelated scenarios; 5xx, DNS, timeout, pod restart | experiment 7; correlate failures against deploys and pod events; check whether unrelated suites failed in the same window |
| **G. Generated-code contract drift** | started after a platform regeneration, a SUT version bump **or a browser auto-update on the agent**; structural selector or response field no longer resolves; the `data-testid` is missing from the captured page source | diff the deployed version and the recorded browser version; check whether the locator is priority 3 or the response shape changed |
| **H. Real-clock data** | clusters near month end, quarter end, midnight, or a weekend | experiment 8; grep the builders for `DateTime.Now` and hardcoded dates |

Two or more classes can be live at once — a shared-state collision often hides a product race behind it. Name every class you find, and fix the deepest one.

## Step 5 — Apply the fix for the class

| Class | Fix | Explicitly not the fix |
| --- | --- | --- |
| A. Timing | route the interaction through `ElementWaits` (`WaitUntilVisible`, `WaitUntilClickable`, `WaitUntilGone`, `WaitUntilTextIs`); hold the `By` and resolve at the point of use; wait for the container to settle before reaching into it; delete every sleep and every stale-element `catch` | a longer timeout, a retry, a sleep, an implicit wait |
| B. Data leakage | the scenario creates its own data with a run-scoped unique id; cleanup in `[AfterScenario]`; `Respawn` reset **before** the scenario | asserting on "whatever is in the environment" |
| C. Cross-tenant bleed | raise a **defect** with the correlation id and evidence; add the `@multitenant` regression scenario that proves the isolation | relaxing the assertion, filtering the other tenant's rows out of the expectation |
| D. Shared-state collision | one tenant fixture per scenario; or move the feature to a serial lane and tag it; one database per concurrent lane; reset WireMock per scenario | lowering global parallelism to hide one bad scenario |
| E. Product race | raise a **defect** with the reproduction; keep the test failing (quarantined with the ticket) until the product is fixed | serialising the test to make the symptom disappear |
| F. Environment | readiness probe before the test stages with a distinct failure message; bounded `Polly` on health check; record the SUT version | treating an environment outage as a test failure |
| G. Contract drift | replace the structural locator with an agreed `data-testid`; update the contract record and assert the shape so the next drift fails loudly | pinning the test to the old structure |
| H. Real-clock data | relative dates resolved in a `*Builder`; named boundary descriptors in `Examples` tables | hardcoded quarter dates, or skipping the test at month end |

```csharp
// Class A — before: a bare FindElement races the render, and Displayed samples once.
IWebElement status = this.driver.FindElement(By.CssSelector("[data-testid='vat-return-status']"));
status.Displayed.Should().BeTrue();

// Class A — after: the page object waits on the real condition through the shared helper,
// then returns the value, and the step asserts on it.
string actualStatus = this.vatReturnPage.ReadStatus();
actualStatus.Should().Be(ExpectedStatus,
    because: "a finalised return must show as finalised before it can be submitted");
```

```csharp
// Class A — the stale-element variant. Before: the element is held across a Vue re-render.
private IWebElement statusBadge;   // never do this

// After: hold the locator, resolve at the point of use, wait first.
private static readonly By StatusBadge = TestIdLocator.ByTestId(StatusTestId);

public string ReadStatus()
{
    IWebElement badge = this.waits.WaitUntilVisible(StatusBadge);
    return badge.Text;
}
```

```csharp
// Class B — before: depends on data someone else created.
string returnId = "VR-00017";

// Class B — after: the scenario owns its data, uniquely, per run.
VatReturnDraft draft = this.vatReturnBuilder
    .WithRunScopedReference(this.runContext.RunId)
    .WithStatus(DraftStatus)
    .Build();
string returnId = await this.vatReturnClient.CreateAsync(
    this.tenantContext.TenantId, draft, this.tenantContext.CorrelationId, CancellationToken.None);
```

```csharp
// Class E / F — legitimate async settle: poll a terminal state with a bounded policy.
// Polly is for API polling only. It never retries an assertion and never appears in UI code.
// Note: Polly is a RESERVED package in the current template — activate the 8.8.0 pin before using it.
ResiliencePipeline<ObligationStatus> pipeline = new ResiliencePipelineBuilder<ObligationStatus>()
    .AddRetry(new RetryStrategyOptions<ObligationStatus>
    {
        ShouldHandle = new PredicateBuilder<ObligationStatus>()
            .HandleResult(status => status == ObligationStatus.Pending),
        MaxRetryAttempts = 10,
        Delay = TimeSpan.FromSeconds(2),
        BackoffType = DelayBackoffType.Constant
    })
    .Build();
```

## Step 6 — Prove the fix

A fix with no evidence is a guess.

1. Re-run experiment 1 at least 20 times green.
2. Re-run the experiment that originally failed, at the same settings, 20 times green.
3. Re-run at double CI's worker count for classes D and E.
4. Confirm no new sleep, retry, `[Retry]`, `continueOnError`, raised timeout, implicit wait or `catch (StaleElementReferenceException)` appears in the diff.
5. Watch the scenario across three consecutive CI runs before closing the ticket.
6. Record the class, the evidence and the fix in the ticket, so the next person recognises the pattern instead of rediscovering it.

If the fix changes what the product does, or relaxes an assertion about what the user sees, stop — that is a product decision for the Product Manager, not a triage outcome.

## Step 7 — Fallback: quarantine with a ticket and an expiry

Only when the cause is not yet known and the gate is blocking the team. Quarantine buys diagnosis time; it does not close the problem.

1. Raise the ticket the same day, with the screenshot, page source, console log, run links, experiment table and the suspected class.
2. Tag the scenario with all three of `@quarantined`, the ticket link and an expiry date, plus a one-line comment:

   ```gherkin
   @quarantined @issue:KEYINF-1234 @expires:2026-11-30
   # Suspected class D: fails only at 4 workers; two scenarios share the primary tenant.
   Scenario: Accountant submits a finalised VAT return
   ```

3. Every gate stage filters `TestCategory!=quarantined`; a separate non-blocking nightly stage runs `TestCategory=quarantined` so the failure keeps producing evidence.
4. Report the quarantine list and each item's age weekly. A growing list is a team problem, not an individual's backlog.
5. Expiry is enforced: fixed and returned to the gate, or formally deleted with the coverage gap recorded in the feature file header. No silent extension.
6. At the agreed ceiling (suggested five scenarios, **to confirm with the team**), fixing flakes takes priority over new automation.
7. A suspected class C or E **is not quarantine-and-forget**. The product defect ticket stays open on the product backlog even if the test is parked.

## Step 8 — Close the loop

| Action | Why |
| --- | --- |
| Record the class and fix in the ticket, and in a short team-visible note | the same class recurs across capabilities |
| If the cause was a missing `data-testid`, raise the front-end change | the contract gap will bite the next screen too |
| If the cause was concurrency, update the recorded concurrency ceiling in the pipeline YAML with a comment | the next person needs to know it was a decision |
| If the cause was a product race or tenant bleed, make sure the defect is on the product backlog, not the test backlog | the suite was right |
| If the cause was environment, raise the readiness-probe or platform gap | "tests failed" and "environment was down" must never look the same |

## Deliverable shape

Return, in this order:

1. The evidence summary from Step 1, including frequency and the deployed SUT version.
2. The reproduction experiment table with pass/fail counts.
3. The artefact reading — the Selenium exception type, what the screenshot showed, what the page source showed about the locator, and what the console log showed at the failure timestamp.
4. The root-cause classification, named, with the signature that confirmed it, plus any secondary class.
5. The fix, as a diff-shaped snippet, with the explicit statement of what was *not* done (no retry, no sleep, no timeout inflation).
6. The proof: experiments re-run and their counts.
7. If unresolved: the quarantine entry with ticket, expiry and suspected class — and the product defect ticket if class C or E is suspected.
8. Assumptions and open questions, flagged where the diagnosis rests on unconfirmed product behaviour.
