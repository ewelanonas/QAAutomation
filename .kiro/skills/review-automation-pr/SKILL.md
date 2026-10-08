---
name: review-automation-pr
description: Use when the user asks to review a pull request containing automated tests — a developer's test PR, a new feature area, or a change to page objects, API clients, steps or pipelines — and wants findings triaged by severity with coaching comments rather than a pass/fail verdict.
---
# Review an automation PR as a quality enabler

Reviews a whole pull request that adds or changes automated tests. The output is a **triaged list of findings, each with the replacement the author should write**, plus one thing the PR did well.

This skill is for reviewing *someone else's* work across layers. The per-artefact `Review gate` tables in the `scaffold-reqnroll-feature`, `build-selenium-page-object`, `build-api-contract-test` and `wire-suite-into-azure-pipeline` skills are authoring self-checks — run those for the layers the PR touches and do not restate them here. Rule detail lives in the steering files: #[[file:code-style.md]], #[[file:project-structure.md]], #[[file:step-definitions.md]], #[[file:bdd-gherkin-standards.md]], #[[file:ui-automation-selenium.md]], #[[file:api-automation.md]], #[[file:ci-azure-devops.md]], #[[file:tech-stack.md]].

## The stance

Coach, do not gatekeep. The role exists to raise the team's testing capability, not to be the only person allowed to approve a test.

| Do | Do not |
| --- | --- |
| Name the replacement: "this wants a `foreach` — here it is" | Flag a rule number and leave the author to guess |
| Link the steering file that owns the rule, once | Paste the rule's full text into the comment |
| Separate *must fix* from *worth knowing* | Mark every preference as blocking |
| Approve with non-blocking notes when nothing is wrong, only unidiomatic | Hold a PR open over naming taste |
| Ask what the test is trying to prove when it is unclear | Assume the author misunderstood |
| Say what the PR got right, every time | Comment only on defects |

If the author disagrees on whether something is a genuine exception, the restrictive reading wins — see the enforcement note in #[[file:code-style.md]].

## Step 1 — Establish what the PR claims to prove

Before reading an assertion, answer from the PR description and the feature files:

1. Which capability, and which layer — `@ui`, `@api`, `@e2e`?
2. Which business rule is now covered that was not covered before?
3. Is the depth tag (`@smoke` / `@regression`) consistent with where CI will run it?

If you cannot answer 2 from the feature file alone, that is the first finding. A scenario whose purpose is only legible from the step code is a specification failure, not a style issue.

## Step 2 — Check the layering before the detail

Cheapest high-value pass. Dependency direction is one-way: `tests/*` → `src/*` → `Core`.

| Look for | Finding if present |
| --- | --- |
| A selector, URL or CSS string in a `*Steps.cs` file | Move it to the page object |
| Raw `HttpClient`, JSON string or SQL in a step | Move it behind a `*Client` |
| `Should()` inside a page object or client | Move the assertion up to the step |
| Gherkin wording leaking into a page object method name | Rename to intent, not sentence |
| A test project referencing another test project | Hard block — breaks suite isolation |
| New package with a `Version` attribute in a `.csproj` | Pin it in `Directory.Packages.props` instead |

## Step 3 — Triage every finding by severity

Assign exactly one severity. This is the part that makes a review useful rather than discouraging.

| Severity | Meaning | Examples |
| --- | --- | --- |
| **Block** | Merging makes the suite less trustworthy or leaks something | Committed secret; `Thread.Sleep`; retry-until-pass; weakened authorisation assertion; a scenario asserting nothing; cross-test data dependency; `FluentAssertions` added |
| **Fix before merge** | Correct behaviour, wrong placement or shape | Layering violation; LINQ outside the three exceptions; generated-markup locator; missing `fileMatchPattern`-covered convention; unpinned package |
| **Coach** | Works and is safe, but teaches a habit we do not want | `var` where the type is not obvious; two concerns in one scenario; imperative Gherkin; abbreviated names |
| **Note** | Observation, author's call | Duplication that has not hit three occurrences; a helper that could be named better |

Only **Block** and **Fix before merge** hold the PR. Say so explicitly, so the author knows what approval is waiting on.

## Step 4 — Read the assertions hardest

A test that cannot fail is worse than no test, because it reports coverage that does not exist.

1. Does each scenario assert one concern, and would it actually fail if the rule were broken?
2. Are status codes exact? No `BeSuccessful()`, no "any 4xx".
3. Are negative and authorisation-denied cases present, and does the refused action verifiably change nothing?
4. Is any assertion weakened — a tolerance widened, a field dropped, an `@ignore` added — to get the build green? Treat that as **Block** and ask what the failure was. A weakened assertion is usually a real defect wearing a test change.

## Step 5 — Look for the flake the author has not hit yet

Most flakes are introduced by a PR that passed. Cross-reference the root-cause classes in the `triage-flaky-test` skill.

| Pattern | Why it will fail later |
| --- | --- |
| Any hard wait | Passes on a fast agent, fails under CI load |
| Locator bound to generated markup or ordinal position | Dies on the next platform regeneration |
| Scenario asserting on pre-existing environment data | Breaks when another suite runs in parallel |
| Shared tenant or fixed identifier instead of a run-scoped one | Collides under parallelism |
| A date, period or "today" with no fixed clock | Fails at the period boundary |
| Cleanup that assumes creation succeeded | Leaves debris that fails the *next* run |

## Step 6 — Write the comments

One comment per finding, on the line. Shape: severity, what, why, the replacement.

> **Fix before merge** — this chain needs to be a loop; #[[file:code-style.md]] bans LINQ so a non-C# maintainer can set a breakpoint on any line. Replacement:
> ```csharp
> Transaction unmatched = null;
> foreach (Transaction transaction in transactions)
> {
>     if (transaction.Status == UnmatchedStatus)
>     {
>         unmatched = transaction;
>         break;
>     }
> }
> ```

> **Block** — `Thread.Sleep(2000)` here will pass locally and fail under CI load. Selenium has no auto-waiting, so the fix is the shared wait helper on the real condition, not a longer sleep: `string status = this.waits.WaitUntilTextIs(SubmissionStatus, "Submitted");`

> **Coach** — non-blocking. `txn` → `transaction`; full words throughout, loop variables included.

## Step 7 — Close the review

State plainly:

1. **Verdict** — approve, approve with notes, or changes requested, and the exact blocking finding count.
2. **What the PR got right** — one specific thing, not a pleasantry.
3. **One transferable lesson** — the single habit that would prevent the most findings next time. One, not a list.
4. **Anything the author uncovered** — a weakened assertion or a surprising failure may be a product defect. Say so, and ask for a ticket rather than a test change.

## Review gate for the reviewer

| # | Check |
| --- | --- |
| 1 | Every finding carries a severity, and blocking findings are counted explicitly. |
| 2 | Every finding names the replacement, not just the rule. |
| 3 | Each rule is linked once, not quoted at length. |
| 4 | No finding is blocking on taste alone. |
| 5 | Assertions were checked for being able to fail, not just for being present. |
| 6 | No secret, real tenant identifier or customer-shaped data reaches the merge; placeholders are `YOUR_API_KEY_HERE`. |
| 7 | Any new package is pinned in `Directory.Packages.props` with no floating range. |
| 8 | One thing done well is named. |
| 9 | Suspected product defects are raised as tickets, not absorbed into the test. |

## Deliberate limits

1. A review cannot establish that a domain fact is correct. If a rate, threshold, role or endpoint shape is unverified, the finding is "mark this as an assumption and confirm with the Product Manager" — not a correction of the number.
2. Reviewing cannot substitute for running. If the PR's suite has not run green in CI, say that approval is contingent on it rather than inferring from the diff.
