# Phase A report — Playwright → Selenium swap and scope reduction

Targeted correction of the existing steering files and skills in `.kiro`. No rewrite: every file's structure, frontmatter and inclusion mode was preserved.

## Files renamed

| From | To |
| --- | --- |
| `.kiro/steering/ui-automation-playwright.md` | `.kiro/steering/ui-automation-selenium.md` |
| `.kiro/skills/build-playwright-page-object/` | `.kiro/skills/build-selenium-page-object/` |

Both renames were followed through: no reference to either old name survives anywhere in `.kiro`, and all ten `#[[file:...]]` targets were verified to resolve to a file that exists on disk.

## Files changed

### Steering

| File | Change |
| --- | --- |
| `tech-stack.md` | Split into **Active** and **Reserved** sections. Playwright rows replaced by `Selenium.WebDriver` 4.50.0 and `Selenium.Support` 4.50.0. Added the "Selenium over Playwright" decision note (unfamiliarity with Playwright as the deciding reason, JD lists Selenium as acceptable, and the two honest costs: no auto-waiting, no trace viewer). Added the Selenium Manager note and the three banned packages with versions and reasons. Added the three config-provider pins. Added Playwright and Azure DevOps rows to the deferrals table. |
| `ui-automation-selenium.md` | Body rewritten for Selenium; `inclusion: fileMatch` and all three glob patterns unchanged. Covers locator strategy (`data-testid` primary via `By.CssSelector`), page object shape (synchronous, intent-level, returns next page object, no assertions), the single shared `ElementWaits` helper, driver lifecycle (one driver per scenario, `Quit()` in a `finally`), stale-element design rules, and failure artefacts to `test-results/`. |
| `project-structure.md` | `pipelines/` dropped from the tree and explicitly noted as deferred; `test-results/` added. `PlaywrightHooks.cs` → `WebDriverHooks.cs`, `PlaywrightOptions.cs` → `BrowserOptions.cs`. Non-negotiable 3 rewritten for the Selenium wait helper. Pipelines glob row marked deferred. `Bogus` reference in the tree genericised. |
| `code-style.md` | **Not weakened.** Lambda exception 2 re-pointed to `WebDriverWait.Until` with the "write it once in the helper" constraint. The async note corrected: a new table states UI page objects are **synchronous** (Selenium's API is synchronous), API clients remain async via Refit, and a step matches the layer it calls. `.Result` / `.Wait()` / `async void` stay banned everywhere. |
| `step-definitions.md` | DI note 4 corrected — `IWebDriver` is scoped, not shared. Example step converted to synchronous. "Async discipline" became "Sync and async discipline" with the per-layer rule. Hooks table rewritten for driver creation and artefact-then-quit teardown. Step-may-not-contain row updated to Selenium vocabulary. |
| `api-automation.md` | Added a scope note naming which packages are active versus reserved, and flagging sections 3, 6 and 7 as the KEYinfinity plan rather than the present state. |
| `ci-azure-devops.md` | **Left in place, not deleted.** A `> **DEFERRED.**` line added at the top stating CI wiring is parked by the user and no pipeline files exist. Section 4 rewritten: Selenium Manager handles the driver, so there is no install step — what remains is browser presence on the agent, network access for first-use driver download, a browser-version record step, and orphaned-process cleanup. Section 5 rewritten for screenshot / page source / console log / driver log. Parallelism, flaky-cause and quarantine tables updated. |
| `domain-keyinfinity.md` | Two `#[[file:ui-automation-playwright.md]]` references repointed. Two Playwright-only "storage-state file" references replaced with per-role session wording. |
| `bdd-gherkin-standards.md`, `test-design-techniques.md` | No change needed — no tool-specific content. |

### Skills

| Skill | Change |
| --- | --- |
| `build-selenium-page-object` | Renamed folder; `name:` and `description:` updated to match; body rewritten for Selenium — `By` locators via a `TestIdLocator` helper, `private static readonly By` instead of cached elements, synchronous methods, `ElementWaits` on every dynamic interaction, `foreach` row matching (no LINQ), stale-element design rules, and a 14-row review gate. |
| `triage-flaky-test` | "Read the Playwright trace" replaced by "Read the Selenium artefacts": exception-type table (`WebDriverTimeoutException`, `NoSuchElementException`, `StaleElementReferenceException`, `ElementClickInterceptedException`, …), then screenshot, page source and console-log reading tables. Evidence list updated to the four captured artefacts plus browser/driver version. Two experiments added: forced serialisation (worker count 1) and a headed local re-run. Class A and G signatures, the class A fix and the code snippets rewritten. |
| `wire-suite-into-azure-pipeline` | **Left in place, not deleted.** `description:` no longer mentions the Playwright install step. Step 5 replaced with browser-prerequisite, version-record and orphaned-driver-cleanup guidance. Artefact publishing switched to `test-results/`, with Allure marked reserved and a masking rule added. Review gate row 5 and the deliverable list updated. |
| `review-automation-pr` | Cross-references repointed to `build-selenium-page-object` and `ui-automation-selenium.md`. The example `Thread.Sleep` block comment rewritten to the Selenium wait helper. |
| `scaffold-reqnroll-feature` | UI page object example rewritten as synchronous Selenium. Steps checklist item 4 corrected for sync UI steps. DI lifetime table updated (`IWebDriver` scoped, never a singleton). `Bogus` in the builder example flagged as reserved. Locator reference repointed. |
| `build-api-contract-test` | Pin reference updated to name which packages are active versus reserved. |
| `author-gherkin-acceptance-criteria`, `design-test-cases` | No change needed. |

## Config-provider versions pinned

Looked up against the NuGet v3 flat-container index on this run:

| Package | Pinned |
| --- | --- |
| `Microsoft.Extensions.Configuration` | **10.0.12** |
| `Microsoft.Extensions.Configuration.Json` | **10.0.12** |
| `Microsoft.Extensions.Configuration.EnvironmentVariables` | **10.0.12** |

Recorded in the Active table in `tech-stack.md`. `Microsoft.Extensions.Configuration.Binder` **10.0.12** is noted there as the pin to use *if* typed `Get<T>()` binding is wanted later; it is deliberately not in the active set.

Also re-verified on this run: `Selenium.WebDriver` 4.50.0, `Selenium.Support` 4.50.0, `Selenium.WebDriver.ChromeDriver` 155.0.8059.3900, `WebDriverManager` 2.17.7, and `DotNetSeleniumExtras.WaitHelpers` 3.11.0 — confirmed as having exactly one stable release ever, which is the evidence behind calling it abandoned.

## Playwright grep result

`grep -r '[Pp]laywright' .kiro` returns matches in exactly three files, and every one is a deliberate comparison:

| File | Mentions | Nature |
| --- | --- | --- |
| `steering/tech-stack.md` | 6 | The "Selenium over Playwright" decision note, the two cost rows, the "deferred by choice" row, and the driver row pointing at the note. |
| `steering/ui-automation-selenium.md` | 4 | Why Selenium rather than Playwright (header), "Playwright's auto-waiting does not exist here", "no equivalent to Playwright's accessibility-first locators", "no storage-state file", and the Selenium Manager simplification. |
| `steering/ci-azure-devops.md` | 1 | "the main CI simplification over Playwright". |

No Playwright API call, package reference, install step, trace artefact or file/skill name remains.

## Invariants confirmed

- **Frontmatter parses.** All 10 steering files and all 8 `SKILL.md` files open with `---` and have a closing `---`. Checked programmatically.
- **Every `fileMatch` file still has its `fileMatchPattern`.** `api-automation.md` (4 globs), `bdd-gherkin-standards.md` (1), `ci-azure-devops.md` (3), `step-definitions.md` (1), `ui-automation-selenium.md` (3 — unchanged from the Playwright version). `code-style.md`, `project-structure.md`, `tech-stack.md` remain `inclusion: always`; `domain-keyinfinity.md` remains `manual`; `test-design-techniques.md` remains `auto`.
- **Every skill folder name matches its `name:` field.** All 8 verified equal, including the renamed `build-selenium-page-object`.
- **Every `#[[file:...]]` reference resolves** to a file that exists in `.kiro/steering/`. Ten distinct targets, all present.

## Note

`d:\git\QAAutomation` is **not a git repository** (`git status` returns "not a git repository"), so no commit was made. The changes are on disk only. Initialise the repo before Phase B if the work should be versioned.
