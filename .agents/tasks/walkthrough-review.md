# Factual-accuracy review — `docs/template-walkthrough.md` (pass 2)

A 3,275-line study guide that documents the repository folder by folder, for a reader who will
learn both C# and this framework from it. This pass independently re-derived every checkable
claim from the repository itself: all 128 non-build files were enumerated, every class, method,
constant and configuration key the guide names was opened and read, all 21 pinned package
versions were compared against `Directory.Packages.props`, the six generated Reqnroll fixture
names were read out of the `.feature.cs` files, and the three claimed absences (`pipelines\`,
`src\QAAutomation.Core\TestData\`, `test-results\`) were confirmed absent. The four findings
from pass 1 are all closed, and no new HIGH or MEDIUM defect was found.

Watch for: nothing blocking. Two LOW cosmetic items only — a file list in §2/§5.6 that predates
two untracked artefacts this review loop itself wrote, and one abbreviated group label in §4.4.
Both are optional.

**Verdict**: APPROVED

## High-level view

Every path, class, method, property and configuration key the guide names exists and behaves as
described. `WebDriverFactory.Create`, `ScenarioDriver`'s four members, `TestConfigurationLoader`'s
three readers and five validators, `ElementWaits`' five public methods and five private helpers,
`FailureArtefacts.Capture`'s four separately-guarded writes, `ArtefactPaths.FindRepositoryRoot`,
all seven `Utilities` helpers, the Refit interface's three `[Get]` methods, `PagesClient`'s three
constants and three methods, all five contract records, the five UI page objects and components,
and every test-project wiring file were read and matched line for line. The long code snippets
are verbatim quotes, not paraphrases — including the ones in §6.2, §6.4, §6.5, §7, §9, §10 and
§11.

Package versions match `Directory.Packages.props` exactly: Reqnroll 3.3.4, NUnit 4.6.1,
NUnit3TestAdapter 6.3.0, Microsoft.NET.Test.Sdk 18.10.1, Selenium 4.50.0, AwesomeAssertions 9.6.0,
Refit 16.3.0, and the five `Microsoft.Extensions.*` packages at 10.0.12. The guide's two claimed
absences hold: the nine reserved packages (WireMock.Net, Testcontainers.PostgreSql, Npgsql,
Respawn, Bogus, Polly, Allure.Reqnroll, Reqnroll.Verify, RestSharp) appear in `tech-stack.md` and
in the props file's comment but carry no `PackageVersion` entry, and the four banned packages
appear nowhere.

The documented-absence claims are correct. `pipelines\` does not exist, `ci-azure-devops.md`'s own
header says `DEFERRED` and says `pipelines/` has not been created, `src\QAAutomation.Core\TestData\`
does not exist, and `test-results\` does not exist. Folder coverage is complete in both
directions: every directory in the repository is documented and no documented directory is
missing.

Every PowerShell block is valid Windows PowerShell. No `&&` appears as a separator anywhere — the
only two occurrences are prose telling the reader not to use it. Project paths
(`tests\QAAutomation.Tests.UI` and siblings) are correct, the headed-run variable is spelled
`QAAUTOMATION_WebDriver__Headless`, which matches the `QAAUTOMATION_` prefix constant plus the
double-underscore nesting separator, and `Remove-Item Env:\...` undoes it. The six fixture names
in §16's table, including `PublishedPageRendersInTheBrowserFeature` with its "InThe", and the
three generated method names cited in §13, §14 and §16, were read out of the generated code and
all match.

No C# snippet violates the house style: no LINQ, no leading `var`, no `_`-prefixed field, no
`Thread.Sleep`. The guide's precision claim about lambdas now checks out exactly — a repository-wide
grep finds lambdas in three files only: five `Until` lambdas in `ElementWaits`, two `provider =>`
container factories in `CoreRegistration`, and one `ConfigureHttpClient` in `ApiRegistration`,
which is what §6.5 and §15 say. No real credential appears in the document; `TenantId` and
`ApiKey` are `YOUR_TENANT_ID_HERE` and `YOUR_API_KEY_HERE`, and the only URLs are the public
marketing site and its public `wp-json` endpoint.

Both execution-flow walkthroughs name real classes and methods in a reachable order, and the
UI-sync / API-async distinction is stated correctly and matches the code: all three UI step
classes are plain `void`, every API `When` is `async Task` and `await`ed with `void` `Then`s, and
the E2E class is async in its `Given` and synchronous in its `When`.

All four pass-1 findings are closed. §5.6 now attributes the three code decisions to the **second**
review document, naming items 2, 3 and 4 of its `Issues (4)` list — which is exactly right — and
says the third records them as closed, which the third document's opening line confirms. §5.6's
table row now carries the third pass's `Issues (3 notes, 0 blocking)`. §6.5 and §15 now scope the
lambda claim to `Until` and name the container and Refit lambdas as the same exception.

<details>
<summary>Issues (2)</summary>

1. **LOW — §2 map and §5.6 table predate two artefacts in `.agents\tasks\`** (guide lines 86–104
   and 672–682). `walkthrough-review.md` and `walkthrough-review.json` now sit in
   `d:\git\QAAutomation\.agents\tasks\` and appear in neither list. `git ls-files` shows both are
   untracked, so the guide is accurate about the *committed* set, and the two files are output of
   this review loop rather than part of the template. Optional fix: add a line saying the folder
   also collects review artefacts from later passes, or leave as is and let the lists describe
   the committed set only.
2. **LOW — §4.4 group label abbreviated while claiming exactness** (guide line 379 and the table
   row at line 390). The sentence promises the set is "grouped exactly as the file groups it",
   then labels the last group "Configuration and DI" where
   `d:\git\QAAutomation\Directory.Packages.props` labels its `ItemGroup` "Configuration and
   dependency injection". The grouping and every version are correct; only the label is shortened.
   Optional fix: use the file's own label, or soften "exactly" to "in the same groups".

</details>

<details>
<summary>Details</summary>

### What was opened, and what matched

§6.4 is the section the original question asked for — where headless mode is decided — so it was
read against source line by line.
`d:\git\QAAutomation\src\QAAutomation.Core\Support\WebDriverFactory.cs` is a static
class with one public `Create(WebDriverOptions)`, the browser guard throws
`TestConfigurationException.ForKey("WebDriver:Browser", ...)`, `BuildChromeOptions` adds
`--headless=new` only inside `if (options.Headless)`, builds `--window-size=` from
`WindowWidth`/`WindowHeight`, adds `--disable-gpu`, and calls
`SetLoggingPreference(LogType.Browser, LogLevel.All)`; `ApplyTimeouts` pins `ImplicitWait` to
`TimeSpan.Zero` and sets `PageLoad` from `PageLoadTimeoutSeconds`. The three quoted snippets are
verbatim. `"Headless": true` is on line 4 of
`src\QAAutomation.Core\Configuration\appsettings.json`, as §6.3 says, and the whole JSON file is
quoted correctly character for character.

`d:\git\QAAutomation\src\QAAutomation.Core\Configuration\TestConfigurationLoader.cs` confirms the
whole §6.3 account: `AddJsonFile` then `AddEnvironmentVariables("QAAUTOMATION_")` in that order,
no `Get<T>()` anywhere, three private readers (`ReadRequiredText`, `ReadWholeNumber`,
`ReadBoolean`) and five private validators. The validator table is accurate down to the numbers —
`RequireSensibleTimeout` is 1 to 300 seconds, `RequireAtLeast` floors `WindowWidth` at 320,
`WindowHeight` at 480 and `Site:MissingPageId` at 1, and `RequirePageSizeRange` caps the maximum
at 100.

`TestConfigurationException.cs` carries `SecretKeyEndings` as `Key`, `Secret`, `Token`, `Password`,
a `HoldsASecret` helper that walks them with a `foreach` and a case-insensitive `EndsWith`, and
the replacement text `(hidden, because this key holds a secret)` — all as described, and the
example message shape in §6.3 matches `ForKey`'s actual string composition.

### The lambda claim, which pass 1 flagged

This was the one place the guide previously over-reached, so it was re-tested mechanically. A grep
for `=>` across every hand-written `.cs` file in `src\` and `tests\`, excluding `bin`, `obj` and
`*.feature.cs`, returns eight hits in three files:

```
ApiRegistration.cs:47      .ConfigureHttpClient(client =>
CoreRegistration.cs:53     services.AddScoped<IWebDriver>(provider => ...)
CoreRegistration.cs:55     services.AddScoped<ElementWaits>(provider => CreateElementWaits(provider));
ElementWaits.cs:36,49,65,75,84   wait.Until(currentDriver => ...)
```

So the boundary a learner takes away is the right one: a
`services.AddScoped<X>(provider => ...)` in a dependencies factory is in style,
`elements.Where(element => element.Displayed)` in a page object is not — which is the contrast
§6.5 draws at line 1244.

### Counts, names and the generated code

Scenario arithmetic holds throughout. The UI suite's three feature files give 2 + 1 + 5 = 8
scenarios once `EnquiryForm.feature`'s four-row outline expands; the API suite's five scenario
definitions expand to 7; E2E has 1. That is the 18 total, split 9 API / 8 UI / 1 E2E, that §1,
§16 and `phase-b-report.md`'s recorded run all state.

Both fixture-name traps §16 calls out are real: `Homepage.feature` generates `HomePageFeature`
because its `Feature:` line reads "Home page", so the `~Homepage` filter the guide warns about
matches nothing, and `PublishedPageRendersInBrowser.feature` generates
`PublishedPageRendersInTheBrowserFeature`. `CategoryAttribute` entries for `ui`, `regression`,
`marketing` and `smoke` are present in the generated code, so the `TestCategory=smoke` filter
advice works, and the six-tag inventory in §16 matches the complete tag set across all six
feature files.

### Governance, tooling and the machine-specific claims

`git config --get core.hooksPath` returns nothing in this clone and exits 1, so §5.4's "nothing is
scanning your commits" is correct. `dotnet --list-sdks` reports exactly the three versions §4.2
and §16 name: 2.1.526, 8.0.425 and 10.0.301. `git ls-files` confirms the tracking claims in §4.6
and §2 one by one — `.agents/` tracked, `.vscode/settings.json` tracked, and no `*.user`, no
`.idea/`, no `*.feature.cs` tracked anywhere.

`scripts\Find-StagedSecrets.ps1` has the three scopes described, reads staged blobs with
`git show :<path>`, carries exactly fifteen rules matching the fifteen categories §5.5 lists,
masks through `Get-MaskedExcerpt` in the first-four-plus-last-two shape the `ghp_********r8`
example implies, tests the allow-list against the matched value rather than the line, and uses
exit codes 0/1/2 with the fail-closed path §5.5 describes. `.githooks\pre-commit` does the three
things in the order given and prints the quoted
`failing closed - a security check that cannot run must block` message.
`.kiro\hooks\scan-agents-artefacts.json` triggers on `PostFileCreate` with matcher
`\.agents[\\/]`, runs the scanner with `-Scope Path`, and times out at 60 seconds.

All ten steering files exist with the inclusion modes and `fileMatchPattern` globs §5.1
tabulates, and all eight skills exist with the `name` and `description` front matter §5.3
summarises. The five `.idea` files are described correctly, down to `indexLayout.xml` having no
attached folders and `projectSettingsUpdater.xml` holding three Rider preferences, and
`.vscode\settings.json` really is `{}`.

### The review-history pointers, which were the pass-1 MEDIUM

The second document's
`Issues (4)` list reads: item 1 the BoDi deviation, item 2 "Unguarded error-body deserialisation"
(`PagesClient.ReadError`), item 3 "Partial failure capture is lost" (`FailureArtefacts.Capture`),
item 4 "A wait disguised as a query" (`PrimaryNavigationComponent.WaitUntilVisible`). The guide
cites items 2, 3 and 4 for exactly those three code decisions. The third document opens "All four
findings from the second pass are closed" and names the same three fixes, so the reading order the
guide recommends — second for why, third for confirmation — is the right one. The second
document's opening likewise confirms §5.6's "the nine closed, four new", and `review.json` is
`{"verdict": "APPROVED", "findings": []}` as §2 and §5.6 state.

### One thing not re-verified, by instruction

§16's "you want `Build succeeded` with no warnings" could not be re-tested, because this is a
documentation-only task and no build was run. It is corroborated by three repository artefacts
rather than assumed: `phase-b-report.md` records `Build succeeded, 0 Warning(s), 0 Error(s)` and
18 passed, and both the second and third review documents repeat the same numbers. Treated as
accurate on that evidence, not as independently confirmed.

</details>

<details>
<summary>Files touched by this review</summary>

| File | Change |
| --- | --- |
| `.agents\tasks\walkthrough-review.md` | This document. |
| `.agents\tasks\walkthrough-review.json` | Gate verdict derived from this document. |

No file under `docs\`, `src\`, `tests\` or `.kiro\` was modified. Review target:
`d:\git\QAAutomation\docs\template-walkthrough.md`.

</details>
