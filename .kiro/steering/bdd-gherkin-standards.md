---
inclusion: fileMatch
fileMatchPattern: "**/*.feature"
---

# Gherkin authoring standards

Rules for every `.feature` file in `tests/*/Features`. Layout and naming: #[[file:project-structure.md]]. Runner and package pins: #[[file:tech-stack.md]].

A feature file is a specification a business analyst can read and a tester can trust. If a step only makes sense to someone who has seen the UI, it is written wrong.

## Core rules

| # | Rule | Reason |
| --- | --- | --- |
| 1 | Declarative, not imperative. Describe the business intent, never the mechanics. | A UI redesign must not force a rewrite of the specification. |
| 2 | One behaviour per scenario. One `When` where possible, one assertion concern. | A failing scenario should name exactly one broken thing. |
| 3 | No UI mechanics in Gherkin — no selectors, element IDs, CSS classes, URLs, HTTP verbs, status codes, SQL or JSON payloads. | Those belong in page objects and API clients. |
| 4 | Third-person, consistent phrasing throughout the repository. | `Given the accountant has a draft VAT return`, never `Given I have…` mixed with `Given the user has…`. |
| 5 | No conjunction-stuffed steps. A step containing `and`, `then` or a comma-separated list of actions must be split. | Stuffed steps hide which half failed. |
| 6 | Present tense for `Given` state, active voice for `When`, observable outcome for `Then`. | Keeps intent unambiguous. |
| 7 | One capability per file, `PascalCase.feature`. Scenario titles describe the outcome, not the steps. | Discoverability. |
| 8 | Step wording is reused verbatim across features where the meaning is identical. | Prevents near-duplicate bindings — see #[[file:step-definitions.md]]. |

## Background discipline

1. `Background` holds only shared, non-negotiable preconditions that every scenario in the file needs.
2. Maximum three steps. If it grows past that, the feature is doing too much — split the file.
3. Never put a `When` or a `Then` in a `Background`.
4. Never put data that varies per scenario in a `Background`; that data belongs in the scenario or in an `Examples` table.
5. If a scenario would have to undo something the `Background` did, the `Background` is wrong.

## Scenario Outline and Examples

Use `Scenario Outline` only when the *same behaviour* is being proven across different data. Different behaviour means different scenarios.

1. Every `Examples` table must trace back to a named test design technique — equivalence partitioning, boundary value analysis, state transition or decision table. Technique detail: #[[file:test-design-techniques.md]].
2. Name the table after the technique and partition it represents, e.g. `Examples: VAT rate bands (equivalence partitions)` and `Examples: period end boundaries (BVA)`.
3. One row per partition representative, plus the boundary rows either side of each limit. Do not pad the table with extra rows from the same partition — they add runtime, not coverage.
4. Add an expected-outcome column. A table of inputs with no expected result is not a specification.
   **One exception — the split table.** When the expected outcome is the same for every row, say it in the `Then` and split the partitions into two outlines instead: one `Scenario Outline` whose `Then` is the accepted outcome, one whose `Then` is the refusal, each with its own inputs-only `Examples` table. An outcome column that reads `accepted, accepted, accepted` is noise, and a single table mixing accepted and rejected rows needs a `Then` that branches on the data — which is the `if`-in-a-step that rule 2 exists to prevent. `tests/QAAutomation.Tests.Api/Features/PageListing.feature` is the worked example. This exception is for a binary accepted/refused rule only; the moment rows differ in *how much* they produce, the outcome column comes back.
5. Keep tables under roughly ten rows. Beyond that, reach for pairwise reduction rather than a longer table.
6. Placeholders use `<lowerCamelCase>` names that read as business terms, not as field IDs.

## Tag taxonomy

Tags are the CI selection mechanism, so they are mandatory and spelled exactly as below. Every feature carries **one layer tag, at least one suite tag and at least one feature-area tag**.

| Dimension | Tags | Rules |
| --- | --- | --- |
| Layer | `@ui`, `@api`, `@e2e` | Exactly one, at feature level. Must match the owning test project. |
| Suite | `@smoke`, `@regression` | At least one. `@smoke` is a small, fast, business-critical subset — keep it ruthlessly short. `@smoke` implies `@regression` coverage exists. |
| Feature area | `@vat`, `@bankrec`, `@mtd`, `@invoicing`, `@payments`, `@permissions` | At least one. Add a new area tag only by updating this table first. |
| Feature area — template | `@marketing` | For the public marketing website used as the template's placeholder system under test. None of the KEYinfinity area tags above fit a marketing site, and inventing one per feature would defeat the taxonomy. |
| Tenancy | `@multitenant` | Scenario proves per-tenant isolation or tenant-scoped data separation. Scenarios without it must not depend on cross-tenant state. |
| Exclusion | `@ignore`, `@manual` | Requires a linked reason — see below. |

Scenario-level tags are additive and only used to narrow, never to contradict, the feature-level layer tag.

### Exclusion tags require a linked reason

`@ignore` and `@manual` are never allowed on their own. Each must be paired with a work-item link tag and a one-line comment:

```gherkin
@ignore @issue:KEYINF-1234
# Blocked: sandbox submission endpoint returns 503 in the shared test tenant.
Scenario: Accountant submits a finalised VAT return to the tax authority
```

- `@ignore` — automation exists or is planned but cannot run. Temporary by definition; the linked item is the exit route.
- `@manual` — deliberately not automated. The linked item records the decision, not a defect. See the automate-vs-manual heuristic in #[[file:test-design-techniques.md]].
- An exclusion tag with no linked item is treated as a review failure.

## Good vs bad

Bad — imperative, UI-coupled, conjunction-stuffed, first person:

```gherkin
Scenario: VAT test
  Given I log in as an "admin" user at "/login"
  When I click the button with id "btn-period" and select "Q1" and press #submit
  Then the div ".badge-success" shows "Submitted" and the row turns green
```

Good — declarative, business language, one behaviour, outcome-named:

```gherkin
@ui @regression @vat
Scenario: Finalising a draft VAT return makes it ready for submission
  Given the accountant has a draft VAT return for the current period
  When the accountant finalises the return
  Then the return is marked as ready for submission
```

## Assumptions to confirm

- Feature-area tags reflect the KEYinfinity capability names given in the brief. Confirm the real module names and extend the taxonomy table before authoring features.
- The work-item prefix `KEYINF-` is illustrative; replace it with the project's actual tracker key.
