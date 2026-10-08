---
name: design-test-cases
description: Use when the user asks to design test cases for a rule or feature area, to derive coverage using equivalence partitioning, boundary value analysis or state transition testing, to build a Scenario Outline Examples table, or to decide what a suite should and should not cover.
---

# Design test cases from a rule

Takes one business rule or feature area and produces a `Scenario Outline` with technique-named `Examples` tables, plus an explicit statement of what is deliberately **not** covered and why. The uncovered list is half the deliverable — undeclared gaps are how a suite gets trusted for coverage it does not have.

Technique detail and worked domain examples: #[[file:test-design-techniques.md]]. Table and wording rules: #[[file:bdd-gherkin-standards.md]]. Domain vocabulary: #[[file:domain-keyinfinity.md]]. Binding rules and `*Builder` conventions: #[[file:step-definitions.md]] and #[[file:project-structure.md]].

Two standing rules:

1. **Every rate, threshold, band, state name and period boundary is an assumption until sourced from the specification.** Write `<confirm>` and list it. Do not ship a plausible number.
2. **No secrets, no real tenant identifiers, no customer-shaped data.** Placeholders are `YOUR_API_KEY_HERE`; data comes from `Bogus` builders.

## Step 1 — State the rule under test in one sentence

One rule, one sentence, with the inputs and the outcome named. If the sentence needs an "and" joining two outcomes, you have two rules — split them and design each separately.

Record: the rule, its inputs, its outcome, and the lowest layer that can prove it (`@api` before `@ui`; `@e2e` only for journeys that must work end to end).

## Step 2 — Choose the techniques

| The rule has | Technique | Produces |
| --- | --- | --- |
| Inputs with ranges or categories behaving alike | Equivalence partitioning | one row per class |
| Limits where behaviour changes | Boundary value analysis | rows at limit−1, limit, limit+1 |
| Dependence on what happened before | State transition testing | from-state / event / to-state rows |
| Several conditions combining into one decision | Decision table | one row per surviving rule |
| More combinations than you can enumerate | Pairwise | reduced set covering all pairs |

Rules:

1. Partition first, always. BVA needs partitions to know where the limits are, and pairwise needs partitioned values to choose from.
2. Name the techniques you chose **and** the ones you rejected, with one line of reasoning each. The rejection is a coverage decision.
3. One technique per `Examples` table. Mixed tables cannot be reasoned about.

## Step 3 — Partition the inputs

For each input: the valid classes, the invalid classes, one representative per class, and the expected outcome. One row per class — a second row from the same class costs runtime and adds nothing.

| Input | Class | Representative | Valid? | Expected outcome |
| --- | --- | --- | --- | --- |
| reclaim amount | positive within limit | 1000.00 | valid | accepted |
| reclaim amount | zero | 0.00 | valid | accepted, no effect on total |
| reclaim amount | negative | −0.01 | invalid | rejected |
| reclaim amount | above maximum | maximum + 0.01 | invalid | rejected, `<confirm>` maximum |

Rule: valid and invalid classes go in separate `Examples` tables. Their `Then` differs — one asserts a value, the other asserts a refusal.

## Step 4 — Derive the boundaries

For every limit found in Step 3, take three values: just below, exactly at, just above. Use the smallest meaningful increment for the type (0.01 for currency, one day for dates, one character for text).

| Limit | below | at | above |
| --- | --- | --- | --- |
| period start date | day before start | period start date | day after start |
| period end date | day before end date | period end date | day after end date |
| reference field length | max − 1 characters | max characters | max + 1 characters |
| monetary rounding | rounds down | exact half | rounds up |

Rules:

1. Use **named boundary descriptors** in the table, not literal dates. A table of hardcoded quarter dates rots every three months.
2. Resolve descriptors to real values in a `*Builder`, so the specification stays readable and the date logic lives in one place:

```csharp
public static class VatPeriodDateResolver
{
    private const string DayBeforeStart = "day before start";
    private const string PeriodStart = "period start date";
    private const string DayAfterStart = "day after start";
    private const string DayBeforeEnd = "day before end date";
    private const string PeriodEnd = "period end date";
    private const string DayAfterEnd = "day after end date";

    public static DateOnly Resolve(string descriptor, VatPeriod period)
    {
        if (descriptor == DayBeforeStart)
        {
            return period.StartDate.AddDays(-1);
        }

        if (descriptor == PeriodStart)
        {
            return period.StartDate;
        }

        if (descriptor == DayAfterStart)
        {
            return period.StartDate.AddDays(1);
        }

        if (descriptor == DayBeforeEnd)
        {
            return period.EndDate.AddDays(-1);
        }

        if (descriptor == PeriodEnd)
        {
            return period.EndDate;
        }

        if (descriptor == DayAfterEnd)
        {
            return period.EndDate.AddDays(1);
        }

        throw new ArgumentException($"Unknown boundary descriptor: {descriptor}", nameof(descriptor));
    }
}
```

Explicit types, no LINQ, a guard for the unknown case, and a breakpoint available on every line — see #[[file:code-style.md]].

## Step 5 — Map the state transitions

If behaviour depends on prior state, enumerate every state and every event, then mark each cell legal or refused. The refused cells are where the defects are.

| From | Event | To | Legal? |
| --- | --- | --- | --- |
| draft | finalise | finalised | yes |
| finalised | submit | submitting | yes |
| submitting | authority accepts | accepted | yes |
| submitting | authority rejects | rejected | yes |
| accepted | submit again | accepted | no, refused |
| accepted | edit | accepted | no, refused |

Rules:

1. Cover every legal transition once, and the high-risk illegal ones explicitly — double submission and editing a terminal state above all.
2. Illegal transitions go in their own `Examples` table; the `Then` is a refusal plus "state unchanged".
3. Third-party responses (accept, reject, unavailable) are stubbed with `WireMock.Net`. Never call a real tax authority — see #[[file:tech-stack.md]].
4. State names and the lifecycle shape are an assumption until confirmed.

## Step 6 — Emit the Scenario Outline

One `Scenario Outline`, technique-named `Examples` tables, an expected-outcome column on every table, under roughly ten rows per table. Tags per #[[file:bdd-gherkin-standards.md]]: one layer tag, at least one suite tag, at least one feature-area tag.

```gherkin
@api @regression @vat
Feature: VAT period allocation
  # Coverage: date boundaries (BVA) and amount partitions for period allocation.
  # Not covered: see the exclusions table in the design note.

  Scenario Outline: A transaction is allocated to a VAT period by its transaction date
    Given a VAT period for the current quarter
    And a transaction dated "<transactionDate>" for <netAmount>
    When the period's transactions are gathered
    Then the transaction is "<inclusion>" in the period

    Examples: period date boundaries (BVA)
      | transactionDate     | netAmount | inclusion |
      | day before start    | 1000.00   | excluded  |
      | period start date   | 1000.00   | included  |
      | day after start     | 1000.00   | included  |
      | day before end date | 1000.00   | included  |
      | period end date     | 1000.00   | included  |
      | day after end date  | 1000.00   | excluded  |

    Examples: amount classes (equivalence partitions)
      | transactionDate   | netAmount | inclusion |
      | period start date | 0.00      | included  |
      | period start date | -0.01     | rejected  |
```

The matching binding keeps the table readable and pushes the resolution into the resolver from Step 4:

```csharp
[Given("a transaction dated {string} for {decimal}")]
public void GivenATransactionDatedFor(string transactionDateDescriptor, decimal netAmount)
{
    VatPeriod period = this.scenarioContext.Get<VatPeriod>(CurrentPeriodKey);
    DateOnly transactionDate = VatPeriodDateResolver.Resolve(transactionDateDescriptor, period);

    Transaction transaction = this.transactionBuilder
        .WithDate(transactionDate)
        .WithNetAmount(netAmount)
        .Build();

    this.scenarioContext.Set(transaction, PendingTransactionKey);
}
```

Rules:

1. No assertion in a `Given`. Assertions live in the `Then`, in the step, never in a builder or client.
2. Named `const` keys for `ScenarioContext`, never inline strings.
3. Keep the outcome column in business language (`included`, `excluded`, `rejected`), not status codes.

## Step 7 — State what is NOT covered, and why

Mandatory. Produce the table every time; "nothing excluded" is almost never true and claiming it is the failure mode this step exists to prevent.

| Not covered | Why | Risk accepted | Covered elsewhere? |
| --- | --- | --- | --- |
| Non-UK VAT schemes | out of scope for this release, per PM | low | no |
| Flat rate and cash accounting schemes | support unconfirmed — **(ASSUMPTION)** | medium | no, pending confirmation |
| Concurrent allocation by two users | needs a serial lane; deferred | medium | no, ticket raised |
| Multi-currency transactions | separate feature area | low | yes, `@invoicing` pairwise set |
| Performance at high transaction volume | not a functional concern | low | no, separate performance work |
| Visual layout of the period selector | judgement call, kept manual | low | manual exploratory |

Rules:

1. Each exclusion names a reason and an accepted risk level. "Not needed" is not a reason.
2. Exclusions that should eventually be covered get a ticket, and the feature file header carries the one-line coverage note.
3. Anything intentionally left manual is tagged `@manual` with a linked reason — see the automate-vs-manual heuristic in #[[file:test-design-techniques.md]].
4. Every `<confirm>` and every **(ASSUMPTION)** appears in this table or in an assumptions table beside it. An unconfirmed value is a coverage risk, not a detail.

## Step 8 — Review gate

1. Each `Examples` table names its technique and holds one technique only.
2. Every limit has its three rows; every class has exactly one representative.
3. Valid and invalid cases are in separate tables.
4. Every table has an expected-outcome column and is under roughly ten rows.
5. Boundary descriptors are named, not hardcoded dates.
6. State transition tables cover all legal transitions plus the high-risk illegal ones.
7. The not-covered table exists and is non-empty.
8. No invented domain facts, no secrets, no real identifiers.
9. Snippets use explicit types and no LINQ, per #[[file:code-style.md]].

## Deliverable shape

Return, in this order:

1. The rule under test, and the layer chosen to prove it.
2. The technique selection, including what was rejected and why.
3. The partition and boundary tables.
4. The state transition table, if the rule is stateful.
5. The `Scenario Outline` with technique-named `Examples` tables.
6. The not-covered table, with risk levels.
7. The assumptions to confirm, with who owns each answer.
