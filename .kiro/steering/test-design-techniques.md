---
inclusion: auto
name: Test design techniques
description: Use when designing test cases, deriving coverage, choosing what to automate, or turning business rules into Scenario Outline examples for UI, API or E2E features.
---

# Test design techniques, applied through automation

Techniques only earn their place if they change what ends up in an `Examples` table. Each section below ends in Gherkin. Wording rules: #[[file:bdd-gherkin-standards.md]]. Binding rules: #[[file:step-definitions.md]].

> **Domain specifics are assumptions.** Every rate, band, threshold, state name, role name and period boundary below is placeholder shape, not verified KEYinfinity or tax-authority fact. Confirm each against the real specification before a feature file is committed. No real endpoint paths, keys or tenant identifiers appear here — configuration uses `YOUR_API_KEY_HERE` placeholders.

## Technique selection

| Situation | Technique | Lands as |
| --- | --- | --- |
| An input has ranges or categories that behave alike | Equivalence partitioning | One `Examples` row per partition |
| An input has limits, and the limits are where bugs live | Boundary value analysis | Rows at limit−1, limit, limit+1 |
| Behaviour depends on what happened before | State transition testing | Rows of from-state / event / to-state |
| Several conditions combine into a decision | Decision table | Rows of condition combination → outcome |
| Too many combinations to enumerate | Pairwise | A reduced table covering all pairs |

---

## 1. Equivalence partitioning — VAT rate bands

Identify the classes where behaviour is identical, then test one representative per class plus the invalid classes. Assumption: the product classifies supplies into named rate bands; confirm the real band list and percentages.

| Partition | Representative | Class |
| --- | --- | --- |
| Standard-rated supply | standard band | valid |
| Reduced-rated supply | reduced band | valid |
| Zero-rated supply | zero band | valid |
| Exempt supply | exempt | valid, but excluded from the reclaim total |
| Outside scope | out of scope | valid, excluded entirely |
| Unrecognised band code | unknown | invalid, rejected |

```gherkin
@api @regression @vat
Scenario Outline: VAT is calculated according to the rate band of the supply
  Given an invoice line of <netAmount> classified as "<rateBand>"
  When the VAT return totals are calculated
  Then the line contributes <expectedVat> to the VAT due total

  Examples: rate bands (equivalence partitions)
    | rateBand      | netAmount | expectedVat |
    | standard      | 1000.00   | <confirm>   |
    | reduced       | 1000.00   | <confirm>   |
    | zero          | 1000.00   | 0.00        |
    | exempt        | 1000.00   | 0.00        |
    | out of scope  | 1000.00   | 0.00        |

  Examples: rejected classifications (invalid partition)
    | rateBand   | netAmount | expectedVat |
    | unknown    | 1000.00   | rejected    |
```

Rules:
1. One row per partition. A second row from the same partition is runtime without coverage.
2. Valid and invalid partitions go in separate `Examples` tables — they usually have different `Then` outcomes.
3. Replace every `<confirm>` with a specification-sourced value before committing.

---

## 2. Boundary value analysis — VAT period boundaries

Partitions tell you *which* values; BVA tells you *where* it breaks. Test each limit and the values immediately either side. Assumption: returns are period-scoped with inclusive start and end dates; confirm the real period calendar and cut-off rules.

```gherkin
@api @regression @vat
Scenario Outline: Transactions fall into a VAT period by their transaction date
  Given a VAT period running from the period start date to the period end date
  And a transaction dated "<transactionDate>"
  When the period's transactions are gathered
  Then the transaction is "<inclusion>" in the period

  Examples: period date boundaries (BVA)
    | transactionDate     | inclusion |
    | day before start    | excluded  |
    | period start date   | included  |
    | day after start     | included  |
    | day before end date | included  |
    | period end date     | included  |
    | day after end date  | excluded  |
```

Apply the same limit−1 / limit / limit+1 shape to:

| Boundary | Values to cover |
| --- | --- |
| Monetary rounding | amount rounding down, exact half, rounding up |
| Field length (reference, description) | max−1, max, max+1 characters |
| Zero and negative amounts | −0.01, 0.00, 0.01 |
| Submission deadline | before deadline, on deadline, after deadline |
| Pagination | first page, last full page, page past the end |

Rule: use named boundary descriptors in the table (as above) and resolve them to real dates in a `*Builder`, so the specification stays readable and does not rot each quarter.

---

## 3. State transition testing — reconciliation and MTD submission

Two state machines, two different jobs: the reconciliation machine is data-driven, the submission machine is externally driven.

### 3a. Bank reconciliation states

Assumption: a statement line is `unmatched`, `partially matched` or `matched`; confirm the real state names and whether partial matching exists.

| From | Event | To |
| --- | --- | --- |
| unmatched | full-value match applied | matched |
| unmatched | part-value match applied | partially matched |
| partially matched | remaining value matched | matched |
| partially matched | match removed | unmatched |
| matched | match removed | unmatched |
| matched | further match applied | rejected, stays matched |

```gherkin
@ui @regression @bankrec
Scenario Outline: Applying and removing matches moves a statement line between states
  Given a bank statement line in the "<fromState>" state
  When the user applies the "<event>"
  Then the statement line is in the "<toState>" state

  Examples: valid transitions (state transition coverage)
    | fromState         | event                   | toState           |
    | unmatched         | full value match        | matched           |
    | unmatched         | part value match        | partially matched |
    | partially matched | remaining value match   | matched           |
    | partially matched | match removal           | unmatched         |
    | matched           | match removal           | unmatched         |

  Examples: invalid transitions
    | fromState | event              | toState |
    | matched   | further full match | matched |
```

### 3b. MTD submission state machine

Assumption: the submission lifecycle is `draft → finalised → submitting → accepted | rejected`, with a terminal `accepted`. Confirm the real lifecycle, retry semantics and whether a rejected return can be amended or must be superseded. **No endpoint paths are stated here by design** — the tax-authority interface is stubbed with WireMock in test (see #[[file:tech-stack.md]]).

```gherkin
@api @regression @mtd
Scenario Outline: The submission lifecycle only allows forward transitions
  Given a VAT return in the "<fromState>" state
  When the "<event>" occurs
  Then the return is in the "<toState>" state
  And the return is editable: <editable>

  Examples: submission lifecycle (state transition coverage)
    | fromState  | event                      | toState   | editable |
    | draft      | finalise                   | finalised | false    |
    | finalised  | submit                     | submitting| false    |
    | submitting | authority accepts          | accepted  | false    |
    | submitting | authority rejects          | rejected  | true     |
    | submitting | authority unavailable      | finalised | false    |
    | accepted   | submit again               | accepted  | false    |
    | rejected   | correct and resubmit       | submitting| false    |
```

Rules:
1. Cover every valid transition at least once, and the highest-risk invalid transitions (double submission, editing a terminal state) explicitly.
2. Invalid transitions are a separate `Examples` table — their `Then` is a refusal, not a state change.
3. Stub the authority's accept/reject/unavailable responses; never call a real tax authority from CI.

---

## 4. Decision tables — permission role matrix

When two or more conditions combine, enumerate the conditions, collapse the impossible combinations, and let each surviving rule become one row. Assumption: role names and capability set below are placeholders; confirm the real role model and whether permissions are additive.

| Rule | Role | Return state | Action allowed |
| --- | --- | --- | --- |
| 1 | preparer | draft | edit: yes, submit: no |
| 2 | preparer | finalised | edit: no, submit: no |
| 3 | approver | draft | edit: no, submit: no |
| 4 | approver | finalised | edit: no, submit: yes |
| 5 | read-only | any | edit: no, submit: no |

```gherkin
@api @regression @permissions
Scenario Outline: Edit and submit rights depend on role and return state
  Given a user with the "<role>" role
  And a VAT return in the "<returnState>" state
  When the user's permitted actions are evaluated
  Then editing is permitted: <canEdit>
  And submitting is permitted: <canSubmit>

  Examples: role and state decision table
    | role      | returnState | canEdit | canSubmit |
    | preparer  | draft       | true    | false     |
    | preparer  | finalised   | false   | false     |
    | approver  | draft       | false   | false     |
    | approver  | finalised   | false   | true      |
    | read only | draft       | false   | false     |
    | read only | finalised   | false   | false     |
```

Rule: a decision table with no `false`/refusal rows is incomplete. Negative authorisation cases are the point — a permission bug that grants too much is worse than one that grants too little.

---

## 5. Pairwise — combinatorial explosion

Use pairwise only once a full table would exceed roughly ten rows and the conditions are believed independent. Most defects in combinatorial space are caused by a pair of factors, not by a specific triple.

Worked shape — invoicing combinations. Four factors would be 3 × 3 × 2 × 2 = 36 full-factorial rows; the pairwise set below covers every pair in 9.

| Factor | Values |
| --- | --- |
| rate band | standard, reduced, zero |
| currency | base, foreign, mixed |
| payment terms | immediate, deferred |
| tenancy | single tenant, multi tenant |

```gherkin
@e2e @regression @invoicing @multitenant
Scenario Outline: An invoice posts correctly across supported option combinations
  Given an invoice with the "<rateBand>" band, "<currency>" currency and "<paymentTerms>" terms
  And the tenant context is "<tenancy>"
  When the invoice is posted
  Then the invoice is accepted and appears on the tenant's ledger only

  Examples: pairwise reduced set
    | rateBand | currency | paymentTerms | tenancy       |
    | standard | base     | immediate    | single tenant |
    | standard | foreign  | deferred     | multi tenant  |
    | standard | mixed    | immediate    | multi tenant  |
    | reduced  | base     | deferred     | multi tenant  |
    | reduced  | foreign  | immediate    | single tenant |
    | reduced  | mixed    | deferred     | single tenant |
    | zero     | base     | immediate    | multi tenant  |
    | zero     | foreign  | immediate    | single tenant |
    | zero     | mixed    | deferred     | single tenant |
```

Rules:
1. Generate the set with a tool, record which tool and which factors, and regenerate when a factor changes. Do not hand-maintain a pairwise table.
2. Known interacting triples (e.g. foreign currency + deferred terms + period boundary) get their own explicit scenario — pairwise does not cover them.
3. Pairwise is a reduction technique, not a substitute for partitioning. Derive the factor values by partitioning first.

---

## Risk-based prioritisation

Grow coverage one feature area at a time. A deep, trustworthy `@vat` suite beats a shallow suite spanning six areas.

1. Score each feature area on **business impact × likelihood of change × current defect history**.
2. Take the highest-scoring area and finish it: `@smoke` happy path first, then partitions and boundaries, then state transitions, then negative and permission cases.
3. Only move to the next area once the current one is green and stable in CI for a full week. Flaky coverage is negative coverage.
4. Suggested starting order, to confirm with the team: `@vat` and `@mtd` (regulatory, hard to fix after release) → `@bankrec` (high volume, data-heavy) → `@payments` and `@invoicing` → `@permissions` (continuous, as a cross-cutting matrix).
5. Every area keeps a one-line coverage note in its feature file header comment, so the gaps are visible rather than assumed.

## What to automate vs leave manual

| Automate | Leave manual |
| --- | --- |
| Regulatory calculations and totals (VAT bands, period aggregation) | Exploratory testing of a newly built screen |
| State machines with defined transitions | Visual design, layout and brand review |
| Permission and tenancy isolation matrices | One-off data migration verification |
| Anything you would otherwise re-run every release | Usability and accessibility judgement calls |
| Deterministic API contracts and error responses | Scenarios depending on an unstubbed third party |
| Boundary and rounding arithmetic | Behaviour still changing weekly |

Decision heuristic, in order:
1. **Will it be re-run?** Run once, keep it manual.
2. **Is the outcome deterministic?** Non-deterministic outcomes produce flaky tests, which cost more than they catch.
3. **Can the dependency be controlled?** If the third party cannot be stubbed (WireMock) and the data cannot be reset (Respawn), automation will be unreliable.
4. **Is the cost of the bug high?** Regulatory and financial correctness justify automation even when it is expensive.
5. **Is the lowest level that can prove it available?** Prefer API over UI, and UI only where the proof is genuinely visual or interactive. E2E covers the thin set of journeys that must work end to end.
6. If a case is not automated, mark it `@manual` with a linked reason — see the tag taxonomy in #[[file:bdd-gherkin-standards.md]].

## Assumptions to confirm

| Assumption | Confirm with |
| --- | --- |
| VAT rate band names and percentages | Product/regulatory specification |
| VAT period calendar, start/end inclusivity, deadline rules | Product specification |
| Reconciliation state names and existence of partial matching | KEYinfinity bank reconciliation module |
| MTD submission lifecycle states, retry and amendment semantics | Integration specification; interface is stubbed in test |
| Role names and whether permissions are additive | Security/permissions model |
| Rounding rules and currency precision | Finance/product specification |
