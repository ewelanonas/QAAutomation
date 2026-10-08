---
inclusion: fileMatch
fileMatchPattern:
  - "**/azure-pipelines*.yml"
  - "**/pipelines/**/*.yml"
  - "**/*.yml"
---

# Azure DevOps pipeline conventions

> **DEFERRED.** CI wiring is parked by the user. **No pipeline files exist in this repository yet** and `pipelines/` has not been created. This file is the agreed convention waiting to be used — it is `fileMatch`-scoped, so it costs nothing while parked. Read it when CI work starts; do not treat anything below as describing the current state of the repo.

Rules for every pipeline YAML in `pipelines/`. Package pins and tool versions: #[[file:tech-stack.md]]. Project layout, suite separation and tag taxonomy targets: #[[file:project-structure.md]]. Tag spelling is owned by #[[file:bdd-gherkin-standards.md]] — CI consumes those tags, it does not invent new ones.

A pipeline has one job: give a developer a trustworthy pass/fail within a few minutes, and give the team a full regression signal on a predictable cadence. Everything below serves that.

## 1. Suite layering — fast gate, slow net

| Stage | Trigger | Scope | Target duration | Gate? |
| --- | --- | --- | --- | --- |
| `Build` | every PR and every CI run | restore, build, analysers | under 3 min | yes, blocking |
| `SmokeApi` | every PR and every CI run | `tests/QAAutomation.Tests.Api`, `@smoke` | under 5 min | yes, blocking |
| `SmokeUi` | every PR and every CI run | `tests/QAAutomation.Tests.UI`, `@smoke` | under 8 min | yes, blocking |
| `RegressionApi` | scheduled nightly + manual | API project, `@regression` | under 30 min | non-blocking for PRs, blocking for release |
| `RegressionUi` | scheduled nightly + manual | UI project, `@regression` | under 60 min | as above |
| `E2E` | scheduled nightly + manual, and pre-release | `tests/QAAutomation.Tests.E2E` | under 45 min | blocking for release |

Rules:

1. A PR never runs full regression. If the smoke gate is too slow, shrink the smoke set — do not weaken the gate.
2. Select by **project first, tag filter second**. The project boundary is the hard guarantee that a suite cannot pull in another suite's fixtures.
3. Every stage is a separate stage (not a step) so it retries, reports and quarantines independently.
4. Schedules are declared in YAML, not in the Azure DevOps UI, so the cadence is reviewable in a PR.

```yaml
schedules:
  - cron: "0 2 * * *"           # 02:00 UTC nightly regression
    displayName: Nightly regression
    branches:
      include:
        - main
    always: true
```

## 2. Tag filtering with `dotnet test`

Reqnroll maps Gherkin tags to NUnit categories, so `--filter TestCategory=<tag>` is the selection mechanism. Write the tag without the `@`.

```yaml
- task: DotNetCoreCLI@2
  displayName: API smoke
  inputs:
    command: test
    projects: 'tests/QAAutomation.Tests.Api/QAAutomation.Tests.Api.csproj'
    arguments: >-
      --configuration $(buildConfiguration)
      --no-build
      --filter "TestCategory=smoke"
      --logger "trx;LogFileName=api-smoke.trx"
      --results-directory $(Agent.TempDirectory)/TestResults
```

| Need | Filter |
| --- | --- |
| One suite | `TestCategory=smoke` |
| One feature area | `TestCategory=vat` |
| Area within a suite | `TestCategory=smoke&TestCategory=vat` |
| Exclude quarantined | `TestCategory!=quarantined` |
| Exclude manual and blocked | `TestCategory!=manual&TestCategory!=ignore` |

Rules:

1. Every test stage excludes `quarantined` explicitly. A quarantined test that still runs in the gate defeats the point.
2. Do not build filter strings by string-concatenating pipeline variables inline; put the full filter in a variable and quote it, so a stray value cannot alter the expression.
3. `--no-build` on test stages, with the build stage publishing its output as a pipeline artifact. Rebuilding per stage hides compile differences between stages.

## 3. Clear pass/fail signals

The pass/fail signal of record is Azure DevOps native test results. Allure, when it is activated, is a stakeholder report, not the gate.

```yaml
- task: PublishTestResults@2
  displayName: Publish test results
  condition: succeededOrFailed()
  inputs:
    testResultsFormat: VSTest
    testResultsFiles: '$(Agent.TempDirectory)/TestResults/*.trx'
    testRunTitle: 'API smoke - $(Build.BuildNumber)'
    mergeTestResults: true
    failTaskOnFailedTests: true

- task: PublishCodeCoverageResults@2
  displayName: Publish coverage
  condition: succeededOrFailed()
  inputs:
    summaryFileLocation: '$(Agent.TempDirectory)/TestResults/**/coverage.cobertura.xml'
```

Rules:

1. `condition: succeededOrFailed()` on every publish task. Results that only publish on success are useless — the failing run is the one you need to read.
2. `failTaskOnFailedTests: true`. A green pipeline with red tests inside it is the worst possible outcome.
3. One `testRunTitle` per stage, including the build number, so a run is identifiable months later.
4. Coverage is collected (`--collect:"XPlat Code Coverage"`) for signal and trend, not as a quality gate. Do not set a coverage threshold that blocks a test-automation repository — coverage of a test project measures nothing useful.
5. No `continueOnError: true` on a test task. If a stage is allowed to fail, model that with stage dependencies, not by muting the task.

## 4. Browsers on the agent — Selenium Manager does the driver, you supply the browser

There is no browser-install script to run. **Selenium Manager is built into Selenium 4.6+** and resolves and downloads the matching browser driver on first use, so there is no `Selenium.WebDriver.ChromeDriver` pin to keep in step and no cache key to maintain. That is the main CI simplification over Playwright — see #[[file:tech-stack.md]].

What CI still has to get right:

1. **A real browser must be installed on the agent.** Selenium Manager resolves the *driver*, not the browser. Microsoft-hosted agents ship with Chrome, Edge and Firefox; a self-hosted agent must have one installed and kept patched, and that is an agent-provisioning task, not a pipeline step.
2. **Selenium Manager needs outbound network access** on first use to download the driver. On a locked-down agent, pre-seed the Selenium Manager cache during agent provisioning rather than reintroducing a pinned driver package.
3. Add a named step that **records the browser version** before the UI stages. When the suite goes red after a browser auto-update, that line in the log is the answer:

   ```yaml
   - task: PowerShell@2
     displayName: Record browser version
     inputs:
       targetType: inline
       script: |
         (Get-Item "C:\Program Files\Google\Chrome\Application\chrome.exe").VersionInfo.ProductVersion
   ```

4. Browser auto-update on the agent is a **real flakiness source** and it is not visible in the diff. Pin the agent image, and treat a same-commit failure after a browser bump as class G contract drift, not a test bug.
5. Add a cleanup step after every UI/E2E stage that kills orphaned driver and browser processes. One scenario that crashed before teardown poisons the agent for every run after it.
6. API-only stages need no browser at all.
7. Headless in CI, always. Headed is a local-only configuration switch.

## 5. Artifacts — screenshot, page source, console logs

**Selenium has no trace viewer.** There is no timeline, no DOM snapshot history and no network tab to open after the fact. Whatever the suite does not capture itself is gone, so publishing the artefacts is not housekeeping — it is the entire diagnostic story. A failed UI test with no artefacts is a re-run request, not a bug report.

| Artifact | Produced by | Publish when | Retention |
| --- | --- | --- | --- |
| Screenshot (`.png`) | `ITakesScreenshot` in `[AfterScenario]` | failure only | short, days |
| Page source (`.html`) | `driver.PageSource` in `[AfterScenario]` | failure only | short, days |
| Browser console log (`.log`) | `driver.Manage().Logs.GetLog(LogType.Browser)` | failure only | short, days |
| Driver log | driver service log file, enabled in driver options | failure only | short, days |
| `.trx` results | `dotnet test --logger trx` | always | pipeline default |
| Allure results / HTML report *(reserved — `Allure.Reqnroll` is not active yet)* | report generation step | always | longer, releases |

The suite writes all four failure artefacts into `test-results/`, one folder per scenario, so there is one path to publish:

```yaml
- task: PublishPipelineArtifact@1
  displayName: Publish UI failure artefacts
  condition: failed()
  inputs:
    targetPath: 'test-results'
    artifact: 'ui-failure-artefacts-$(System.StageAttempt)'
```

Rules:

1. Include `$(System.StageAttempt)` (or the job attempt) in the artifact name. Two attempts publishing the same artifact name is a pipeline error, and it happens on exactly the retry you care about.
2. Capture on failure only. Capturing green runs is storage and runtime cost that buries the one capture that matters.
3. **Page source and console logs can contain tokens, cookies and PII.** Mask before writing, mask by allow-list, and never publish an unmasked capture. Artifacts are routinely readable by more people than anyone assumes.
4. No configuration dumps, no request/response bodies without masking, no customer-shaped data.

## 6. Parallelisation and the multi-tenant ceiling

KEYinfinity is multi-tenant SaaS on shared Azure/Kubernetes test environments, so shared state is the real constraint on concurrency, not agent count.

| Dimension | Position |
| --- | --- |
| Across stages (API / UI / E2E) | Parallel. Separate projects, separate state. |
| Within the API project | Parallel, bounded, when each scenario owns its tenant or is reset by Respawn. |
| Within the UI project | Parallel, low worker count. Each scenario gets its own browser session, but every session is a real browser process — the agent's memory, not isolation, is usually the first ceiling. The back end is not isolated either. |
| E2E | Serial by default. These journeys mutate the most shared state. |
| Anything touching tenant-wide configuration | Serial, and tagged so it can be isolated. |

Rules:

1. Start at a low degree of parallelism and raise it only after a full week of green runs. Concurrency-induced flakiness is indistinguishable from product flakiness at a glance, and far more expensive to diagnose.
2. Scenarios sharing a tenant must not run concurrently. Either give each scenario its own tenant fixture, or constrain that feature to a serial lane.
3. Respawn resets between scenarios within a lane; it cannot protect you from two lanes resetting the same database. One database per concurrent lane, or one lane.
4. Set a timeout on every job (`timeoutInMinutes`) matching the stage targets in section 1. A hung UI job consuming a parallel slot blocks everyone.
5. Record the chosen concurrency in the pipeline YAML as a variable with a comment explaining the ceiling, so the next person knows it was a decision rather than a default.

## 7. Configuration and secrets — Key Vault, never YAML

**State this plainly: test automation is one of the most common ways credentials leak.** A test repository accumulates logins, API keys and tenant identifiers, and it is read by more people than production code.

| Value | Where it lives |
| --- | --- |
| Non-secret settings (base URLs, timeouts, browser choice, worker count) | pipeline variables, or committed `appsettings.{env}.json` |
| Secrets (API keys, test user passwords, connection strings, tenant credentials) | Azure Key Vault, surfaced through a variable group linked to the vault |
| Committed config placeholders | `YOUR_API_KEY_HERE` and nothing else |

```yaml
variables:
  - group: keyinfinity-qa-nonsecret        # plain pipeline variables
  - group: keyinfinity-qa-kv               # linked to Azure Key Vault
  - name: buildConfiguration
    value: Release
```

Rules:

1. No secret value in YAML, in a committed `appsettings*.json`, in a `.runsettings`, or in a test data file. Ever. Not even for a sandbox.
2. Secrets reach the test process as environment variables mapped explicitly in the task's `env:` block. Secret variables are not auto-injected into the process environment, and that is a feature — mapping them explicitly makes the surface reviewable.
3. Never echo or log a secret. No `Write-Host $(apiKey)`, no logging of request headers, no dumping bound configuration objects. Scrub `Authorization` and `Cookie` headers before any log or trace is published.
4. Never log unmasked PII or customer-shaped data. Test data comes from `*Builder` classes (see #[[file:project-structure.md]]), not from a production extract.
5. Credentials are scoped per environment and short-lived where the platform allows it. One shared long-lived key across all environments is a single point of compromise.
6. Key Vault access uses a service connection with workload identity federation; the pipeline's identity gets `get`/`list` on secrets only.
7. If a secret is ever committed, treat it as compromised: rotate first, scrub history second. Removing the commit is not remediation.

## 8. Targeting the Azure / Kubernetes test environment

1. Environment selection is one pipeline parameter (`environment`: `dev` | `qa` | `staging`), which picks the variable groups and the `appsettings.{environment}.json`. No conditional URL logic scattered through tasks.
2. Use Azure DevOps **Environments** for deployment-gated stages so approvals, history and the Kubernetes namespace view are attached to a named environment rather than to a job.
3. A test stage never deploys the system under test and never runs `kubectl apply`. It verifies the version already deployed and records which version that was.
4. Add a short readiness probe before the test stages: poll the SUT health endpoint with a bounded `Polly` policy or a retry task, and fail fast with a clear message if the environment is not up. "Tests failed" and "environment was down" must not look the same.
5. Never point a pipeline at production. If a production smoke check is ever required, it is a separate, explicitly named, read-only pipeline with its own approval.
6. Third-party tax-authority interfaces are stubbed with `WireMock.Net` in CI. No pipeline calls a real tax authority — see #[[file:tech-stack.md]].

## 9. Flaky tests — zero tolerance, quarantine with an expiry

A test that fails intermittently is worse than no test: it trains the team to ignore red. The policy is quarantine with an exit route, never a silent retry.

### Banned

| Banned | Why |
| --- | --- |
| `retryCountOnTaskFailure` on a test task to mask flakiness | Hides the failure and multiplies runtime. |
| NUnit `[Retry]` on a flaky scenario | Same problem, moved into the code. |
| `continueOnError: true` on a test stage | Converts red to green with no record. |
| `Thread.Sleep` added to "stabilise" a UI test | Banned outright. Selenium has no auto-waiting, so the answer is the shared `WebDriverWait` helper — see #[[file:ui-automation-selenium.md]] and #[[file:code-style.md]]. |
| Deleting or commenting out the scenario | Loses the coverage and the evidence. |

### The quarantine procedure

1. **Detect.** Two unexplained failures of the same scenario inside 10 runs, or any failure that passes on re-run with no code change.
2. **Raise a ticket** the same day, with the screenshot, page source, console log and the run links attached.
3. **Tag the scenario** with `@quarantined`, the ticket link and an expiry date — all three, or the review rejects it:

   ```gherkin
   @quarantined @issue:KEYINF-1234 @expires:2026-11-30
   # Flaky: period selector intermittently renders before tenant config loads.
   Scenario: Accountant selects a VAT period for a newly provisioned tenant
   ```

4. **Exclude from gates**, not from visibility: every stage filters `TestCategory!=quarantined`, and a separate non-blocking nightly stage runs `TestCategory=quarantined` so the failure keeps producing evidence.
5. **Report weekly.** The quarantine list and each item's age is a standing agenda item. A growing list is a team problem, not an individual's backlog.
6. **Expiry is enforced.** At the expiry date the scenario is either fixed and returned to the gate, or formally deleted with the coverage gap recorded in the feature file header. It does not get a silent extension.
7. **Cap the list.** Agree a ceiling (suggested: 5 scenarios, to confirm with the team). At the ceiling, fixing flakes takes priority over new automation.

### Fix the cause, not the symptom

| Symptom | Usual cause | Fix |
| --- | --- | --- |
| UI step times out intermittently | waiting on the wrong thing, or not waiting at all | wait on the user-visible condition through the shared `WebDriverWait` helper — see #[[file:ui-automation-selenium.md]] |
| `StaleElementReferenceException` | element held across a Vue re-render | store the `By`, resolve at the point of use; wait for the container to settle first |
| UI fails after a browser auto-update on the agent | agent image not pinned | pin the agent image; record the browser version per run |
| Passes alone, fails in parallel | shared tenant or shared data | own fixture per scenario, or a serial lane |
| Fails on the first run of the day | cold environment | readiness probe before the stage |
| Fails near a period boundary | real clock in test data | relative dates resolved in a `*Builder` |
| Intermittent 5xx from a third party | unstubbed dependency | `WireMock.Net` stub |

## Related

- Pinned packages and versions: #[[file:tech-stack.md]]
- Solution layout, suite separation, pipeline file names: #[[file:project-structure.md]]
- Tag taxonomy CI selects on: #[[file:bdd-gherkin-standards.md]]
- C# readability rules: #[[file:code-style.md]]
- Selenium waits, driver lifecycle and failure artefacts: #[[file:ui-automation-selenium.md]]
