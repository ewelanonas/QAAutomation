---
name: wire-suite-into-azure-pipeline
description: Use when the user asks to add a test suite or stage to the Azure DevOps pipeline — tag filtering with dotnet test, browser prerequisites on the agent, published test results and failure artefacts, concurrency limits, or Key Vault-backed secrets for a test stage.
---

# Wire a test suite into the Azure DevOps pipeline

Adds one suite (API smoke, UI regression, E2E, quarantine-watch) to the pipeline so it selects the right scenarios, installs what it needs, publishes a trustworthy pass/fail signal, and gets its secrets from Key Vault rather than YAML.

Output is **markdown YAML snippets with their intended file paths** under `pipelines/`. Do not write `.yml` files from this skill unless the user explicitly asks for files.

Stage layering, filters, artefacts, concurrency and quarantine policy: #[[file:ci-azure-devops.md]]. Suite separation and pipeline file names: #[[file:project-structure.md]]. Tag spelling: #[[file:bdd-gherkin-standards.md]]. Pinned package versions: #[[file:tech-stack.md]].

Two standing rules:

1. **No secret in YAML.** Not a key, not a password, not a connection string, not a tenant credential, not "just for the sandbox". Secrets live in Azure Key Vault, surfaced through a linked variable group, and are mapped explicitly into a task's `env:` block.
2. **CI never calls a real tax authority.** Third-party boundaries are stubbed with `WireMock.Net`. An environment that cannot be pointed away from a real third party is not an automation target.

## Step 1 — Place the suite in the stage layering

| Decide | Rule |
| --- | --- |
| Which project | `tests/QAAutomation.Tests.Api`, `.UI` or `.E2E`. Project first is the hard guarantee a suite cannot pull in another suite's fixtures. |
| Which tags | one suite tag (`smoke` / `regression`), optionally narrowed by a feature-area tag. |
| Trigger | PR gate, nightly schedule, manual, or pre-release. |
| Blocking | blocking for PRs only if it is a smoke gate. |
| Target duration | from the stage table in #[[file:ci-azure-devops.md]]; set `timeoutInMinutes` to match. |
| Browsers needed | UI and E2E yes, API no. |

Rules: a PR never runs full regression — if the smoke gate is too slow, shrink the smoke set rather than weakening the gate. Every suite is its own **stage**, not a step, so it retries, reports and quarantines independently.

## Step 2 — Declare variables and variable groups

```yaml
# pipelines/azure-pipelines.yml (orchestrator)
parameters:
  - name: environment
    displayName: Target environment
    type: string
    default: qa
    values: [dev, qa, staging]

variables:
  - group: keyinfinity-qa-nonsecret          # base URLs, worker counts, feature switches
  - group: keyinfinity-qa-kv                 # linked to Azure Key Vault — secrets only
  - name: buildConfiguration
    value: Release
  - name: targetFramework
    value: net10.0                           # must match the TFM pinned in tech-stack.md
  - name: testResultsDirectory
    value: $(Agent.TempDirectory)/TestResults
  # Concurrency ceiling: the shared multi-tenant test environment, not agent count,
  # is the limit. Raise only after a full week of green runs.
  - name: uiWorkerCount
    value: 2
```

| Value | Where it lives |
| --- | --- |
| Base URLs, timeouts, browser choice, worker count | pipeline variables or committed `appsettings.{env}.json` |
| API keys, test user passwords, connection strings, tenant credentials | Azure Key Vault, via the linked variable group |
| Committed config placeholders | `YOUR_API_KEY_HERE` and nothing else |

Environment selection is one parameter that picks the variable groups and the `appsettings.{environment}.json`. No conditional URL logic scattered through tasks.

## Step 3 — Build once, test with `--no-build`

Rebuilding per stage hides compile differences between stages.

```yaml
stages:
  - stage: Build
    displayName: Build and publish test binaries
    jobs:
      - job: BuildJob
        timeoutInMinutes: 10
        steps:
          - task: UseDotNet@2
            displayName: Install .NET SDK
            inputs:
              packageType: sdk
              version: 10.0.x          # keep aligned with the TFM in tech-stack.md

          - task: DotNetCoreCLI@2
            displayName: Restore
            inputs:
              command: restore
              projects: 'QAAutomation.sln'

          - task: DotNetCoreCLI@2
            displayName: Build
            inputs:
              command: build
              projects: 'QAAutomation.sln'
              arguments: '--configuration $(buildConfiguration) --no-restore'

          - task: PublishPipelineArtifact@1
            displayName: Publish build output
            inputs:
              targetPath: '$(Build.SourcesDirectory)'
              artifact: 'build-output'
```

## Step 4 — Select scenarios by tag

Reqnroll maps Gherkin tags to NUnit categories, so `--filter TestCategory=<tag>` is the selection mechanism. Write the tag **without** the `@`.

| Need | Filter |
| --- | --- |
| One suite | `TestCategory=smoke` |
| One feature area | `TestCategory=vat` |
| Area within a suite | `TestCategory=smoke&TestCategory=vat` |
| Exclude quarantined | `TestCategory!=quarantined` |
| Exclude manual and blocked | `TestCategory!=manual&TestCategory!=ignore` |

Put the whole filter in a variable and quote it. Do not concatenate pipeline variables into a filter expression inline — a stray value can silently alter the expression and change what ran.

```yaml
  - stage: SmokeApi
    displayName: API smoke
    dependsOn: Build
    jobs:
      - job: ApiSmoke
        timeoutInMinutes: 10
        variables:
          - name: apiSmokeFilter
            value: 'TestCategory=smoke&TestCategory!=quarantined&TestCategory!=manual&TestCategory!=ignore'
        steps:
          - download: current
            artifact: build-output

          - task: DotNetCoreCLI@2
            displayName: Run API smoke
            inputs:
              command: test
              projects: 'tests/QAAutomation.Tests.Api/QAAutomation.Tests.Api.csproj'
              arguments: >-
                --configuration $(buildConfiguration)
                --no-build
                --filter "$(apiSmokeFilter)"
                --logger "trx;LogFileName=api-smoke.trx"
                --results-directory $(testResultsDirectory)
            env:
              # Secrets are mapped explicitly. They are not auto-injected, and that is a feature.
              QAAUTOMATION__Api__ClientSecret: $(apiClientSecret)      # from Key Vault group
              QAAUTOMATION__Auth__TestUserPassword: $(testUserPassword) # from Key Vault group
              QAAUTOMATION__Api__BaseAddress: $(apiBaseAddress)         # non-secret group
```

Every gate stage excludes `quarantined` explicitly. A quarantined test that still runs in the gate defeats the point.

## Step 5 — Browsers on the agent (no install step needed)

There is no browser-install step to write. **Selenium Manager is built into Selenium 4.6+** and resolves and downloads the matching browser driver on first use, so there is no driver package to pin and no cache key to keep in step with it. Do not add `Selenium.WebDriver.ChromeDriver` or `WebDriverManager` — see #[[file:tech-stack.md]].

What the UI/E2E stages do need:

```yaml
          - task: PowerShell@2
            displayName: Record browser version
            inputs:
              targetType: inline
              script: |
                $chrome = "C:\Program Files\Google\Chrome\Application\chrome.exe"
                if (-not (Test-Path $chrome)) {
                  Write-Error "No browser on the agent. Selenium Manager resolves the driver, not the browser."
                  exit 1
                }
                (Get-Item $chrome).VersionInfo.ProductVersion
```

```yaml
          # After the UI/E2E tests, always. One scenario that crashed before teardown
          # leaves a driver process behind and poisons every later run on this agent.
          - task: PowerShell@2
            displayName: Kill orphaned driver processes
            condition: always()
            inputs:
              targetType: inline
              script: |
                Get-Process -Name chromedriver, msedgedriver, geckodriver -ErrorAction SilentlyContinue |
                  Stop-Process -Force -ErrorAction SilentlyContinue
```

Rules: a real browser must be installed on the agent and the agent image pinned, because a browser auto-update changes behaviour with no commit; Selenium Manager needs outbound network access on first use, so pre-seed its cache on a locked-down agent rather than reintroducing a pinned driver package; record the browser version every run so a post-update failure is diagnosable; headless always in CI; API-only stages need no browser at all.

## Step 6 — Add a readiness probe

"Tests failed" and "the environment was down" must never look the same.

```yaml
          - task: PowerShell@2
            displayName: Wait for environment readiness
            inputs:
              targetType: inline
              script: |
                $healthUrl = "$(apiBaseAddress)/health"
                $attempts = 0
                while ($attempts -lt 10) {
                  try {
                    $response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 10
                    if ($response.StatusCode -eq 200) { exit 0 }
                  } catch { }
                  $attempts++
                  Start-Sleep -Seconds 6
                }
                Write-Error "Environment not ready at $healthUrl after $attempts attempts. Failing fast: this is an environment problem, not a test failure."
                exit 1
```

A test stage never deploys the system under test and never runs `kubectl apply`. It verifies the version already deployed and records which version that was.

## Step 7 — Publish results and artefacts

```yaml
          - task: PublishTestResults@2
            displayName: Publish test results
            condition: succeededOrFailed()
            inputs:
              testResultsFormat: VSTest
              testResultsFiles: '$(testResultsDirectory)/*.trx'
              testRunTitle: 'UI regression - $(Build.BuildNumber)'
              mergeTestResults: true
              failTaskOnFailedTests: true

          # Selenium has no trace viewer, so these three files ARE the diagnostics.
          # The suite writes screenshot, page source and console log per failing scenario.
          - task: PublishPipelineArtifact@1
            displayName: Publish UI failure artefacts
            condition: failed()
            inputs:
              targetPath: 'test-results'
              artifact: 'ui-failure-artefacts-$(System.StageAttempt)'

          # Allure.Reqnroll is a RESERVED package — include this only once it is activated.
          - task: PublishPipelineArtifact@1
            displayName: Publish Allure results
            condition: succeededOrFailed()
            inputs:
              targetPath: 'tests/QAAutomation.Tests.UI/bin/$(buildConfiguration)/$(targetFramework)/allure-results'
              artifact: 'allure-results-ui-$(System.StageAttempt)'
```

| Rule | Reason |
| --- | --- |
| `condition: succeededOrFailed()` on every publish | the failing run is the one you need to read |
| `failTaskOnFailedTests: true` | a green pipeline with red tests inside is the worst outcome |
| `$(System.StageAttempt)` in every artifact name | two attempts publishing the same name is an error, and it happens on exactly the retry you care about |
| Failure artefacts on failure only | capturing green runs costs storage and buries the one capture that matters |
| Mask before publishing | page source and console logs can carry tokens, cookies and PII, and artifacts are read by more people than anyone assumes |
| One `testRunTitle` per stage, with the build number | a run stays identifiable months later |
| No `continueOnError: true` on a test task | model an allowed failure with stage dependencies, not by muting the task |
| Scrub artefacts | a trace is a full HTTP recording; no tokens, cookies, auth headers, config dumps or customer-shaped data |

Azure DevOps native test results are the pass/fail signal of record. Allure is the stakeholder report, not the gate.

## Step 8 — Set concurrency and the quarantine watch stage

| Dimension | Position |
| --- | --- |
| Across stages (API / UI / E2E) | parallel — separate projects, separate state |
| Within the API project | parallel, bounded, when each scenario owns its tenant or Respawn resets it |
| Within the UI project | parallel, low worker count — contexts are isolated, the back end is not |
| E2E | serial by default |
| Anything touching tenant-wide configuration | serial, and tagged so it can be isolated |

Record the chosen concurrency as a commented variable (Step 2) so the next person knows it was a decision. Scenarios sharing a tenant must not run concurrently: give each its own tenant fixture, or put the feature in a serial lane.

The quarantine stage keeps parked failures producing evidence without blocking anyone:

```yaml
  - stage: QuarantineWatch
    displayName: Quarantined scenarios (non-blocking)
    dependsOn: Build
    condition: succeededOrFailed()
    jobs:
      - job: Quarantined
        timeoutInMinutes: 30
        variables:
          - name: quarantineFilter
            value: 'TestCategory=quarantined'
        steps:
          - download: current
            artifact: build-output
          - task: DotNetCoreCLI@2
            displayName: Run quarantined scenarios
            continueOnError: true        # the ONE place this is allowed: a non-gating watch stage
            inputs:
              command: test
              projects: 'tests/QAAutomation.Tests.UI/QAAutomation.Tests.UI.csproj'
              arguments: >-
                --configuration $(buildConfiguration)
                --no-build
                --filter "$(quarantineFilter)"
                --logger "trx;LogFileName=quarantined.trx"
                --results-directory $(testResultsDirectory)
```

Nightly cadence is declared in YAML, not the Azure DevOps UI, so it is reviewable in a pull request:

```yaml
schedules:
  - cron: "0 2 * * *"
    displayName: Nightly regression
    branches:
      include:
        - main
    always: true
```

## Step 9 — Secrets: Key Vault, mapped explicitly

Test automation is one of the most common ways credentials leak. A test repository accumulates logins, keys and tenant identifiers, and is read by more people than production code.

| # | Rule |
| --- | --- |
| 1 | No secret value in YAML, in a committed `appsettings*.json`, in a `.runsettings`, or in a test data file. Ever. |
| 2 | Secrets reach the test process as environment variables mapped explicitly in the task's `env:` block. Secret variables are not auto-injected, and explicit mapping makes the surface reviewable. |
| 3 | Never echo or log a secret. No `Write-Host $(apiKey)`, no header logging, no dumping bound configuration objects. Scrub `Authorization` and `Cookie` before any log or trace is published. |
| 4 | Key Vault access uses a service connection with workload identity federation; the pipeline identity gets `get`/`list` on secrets only. |
| 5 | Credentials are scoped per environment and short-lived. One long-lived key shared across environments is a single point of compromise. |
| 6 | Never point a pipeline at production. A production smoke check, if ever needed, is a separate, explicitly named, read-only pipeline with its own approval. |
| 7 | If a secret is ever committed, treat it as compromised: rotate first, scrub history second. |

Committed configuration looks like this and nothing more:

```json
{
  "Api": {
    "BaseAddress": "https://qa.example.invalid",
    "ClientSecret": "YOUR_API_KEY_HERE"
  }
}
```

## Step 10 — Review gate

| # | Check |
| --- | --- |
| 1 | The suite is its own stage, with `dependsOn: Build` and a `timeoutInMinutes` matching its target duration. |
| 2 | Project selected first, tag filter second; the filter is in a quoted variable. |
| 3 | `TestCategory!=quarantined` on every gating stage. |
| 4 | `--no-build`, with the build output downloaded as an artifact. |
| 5 | For UI/E2E stages only: browser version recorded, agent image pinned, orphaned-driver cleanup step present. No browser-install script and no pinned driver package — Selenium Manager handles the driver. |
| 6 | Readiness probe present, failing with an environment-specific message. |
| 7 | `PublishTestResults@2` with `succeededOrFailed()` and `failTaskOnFailedTests: true`. |
| 8 | Artifact names include `$(System.StageAttempt)`; traces on failure only. |
| 9 | No `retryCountOnTaskFailure`, no `[Retry]`, no `continueOnError` outside the non-gating quarantine stage. |
| 10 | Every secret comes from the Key Vault variable group and is mapped in `env:`; no secret literal anywhere. |
| 11 | No `kubectl apply`, no deployment, no production target in a test stage. |
| 12 | Third-party boundaries stubbed; no pipeline path reaches a real tax authority. |
| 13 | Concurrency recorded as a commented variable. |

## Deliverable shape

Return, in this order:

1. The suite's placement: project, tags, trigger, blocking status, target duration.
2. The variable and variable-group delta, with the concurrency comment.
3. The stage YAML snippet, with its intended file path under `pipelines/`.
4. The browser-version record and orphaned-driver cleanup steps, if the suite needs a browser.
5. The publish tasks for results, traces and Allure results.
6. The secret mapping table: which variable comes from which group, and which Key Vault secret backs it — names only, never values.
7. Anything assumed: agent pool and OS, Key Vault and variable-group names, health endpoint path, service connection name, artefact paths. These are **(ASSUMPTION)** until confirmed against the Azure DevOps project.
