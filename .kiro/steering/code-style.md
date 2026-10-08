---
inclusion: always
---

# Code style — readability over cleverness

Optimised for a maintainer who is **not** an experienced C# developer. A manual tester learning to code, or a developer new to this framework, must be able to read a step definition, page object or API client top to bottom and understand it without looking anything up.

Where a shorter, cleverer form exists, choose the longer, plainer one. Test code is read far more often than it is written, and it is read under pressure — usually when a build has just gone red.

## No LINQ

Do not use LINQ in step definitions, page objects, API clients, hooks, builders or utilities.

| Banned | Use instead |
| --- | --- |
| Query syntax (`from x in y select x`) | `foreach` loop |
| Chained extension methods (`.Where().Select().First()`) | `foreach` with an `if`, assigning to a named variable |
| `.Any()`, `.All()`, `.Count(predicate)` | `foreach` plus a named `bool` flag |
| `.FirstOrDefault()`, `.Single()`, `.Last()` | `foreach` with an early `break`, or index after a `Count` check |
| `.OrderBy()`, `.GroupBy()`, `.Sum()`, `.Aggregate()`, `.Distinct()` | an explicit loop, or a named helper in `QAAutomation.Core/Utilities` |
| `.Select()` to project a list | `foreach` that adds to a new list |
| `using System.Linq;` in a new file | omit it — if you need it, you are probably breaking this rule |

Avoid:

```csharp
var unmatched = transactions.Where(t => t.Status == "Unmatched").ToList();
var firstVatLine = response.Lines.FirstOrDefault(l => l.BoxNumber == 1);
bool allReconciled = transactions.All(t => t.Status == "Matched");
```

Prefer:

```csharp
List<Transaction> unmatched = new List<Transaction>();
foreach (Transaction transaction in transactions)
{
    if (transaction.Status == "Unmatched")
    {
        unmatched.Add(transaction);
    }
}

VatReturnLine firstVatLine = null;
foreach (VatReturnLine line in response.Lines)
{
    if (line.BoxNumber == 1)
    {
        firstVatLine = line;
        break;
    }
}

bool allReconciled = true;
foreach (Transaction transaction in transactions)
{
    if (transaction.Status != "Matched")
    {
        allReconciled = false;
        break;
    }
}
```

Longer, yes. But there is nothing to learn before reading it, and a breakpoint can be set on any line.

### The three narrow exceptions

A total ban is unworkable where a library's own API takes a predicate. These three are allowed. Nothing else is.

1. **Assertion predicates** that AwesomeAssertions requires — for example `Should().Contain(line => line.BoxNumber == 1)`. The predicate must be one comparison. No chaining, no nesting.
2. **Library APIs that accept only a lambda** — Selenium's `WebDriverWait.Until(driver => ...)`, Refit configuration, and (when they are activated) WireMock.Net request matchers and Polly predicates. Use the lambda the API demands and add nothing around it. For `Until`, write the lambda **once** in the shared wait helper in `QAAutomation.Core/Utilities`, not at every call site — see #[[file:ui-automation-selenium.md]].
3. **`.ToList()` / `.ToArray()` on a library return value** when an `IEnumerable<T>` must be materialised before it can be counted or indexed. One call, at the boundary, assigned straight to a named variable.

Anything beyond these three goes into a named method in `QAAutomation.Core/Utilities`. The loop still exists, but it exists once, behind an intent-revealing name, so every call site reads as plain English:

```csharp
Transaction unmatched = TransactionFinder.FindFirstUnmatched(transactions);
int outstandingCount = TransactionCounter.CountOutstanding(transactions);
```

A helper that wraps a loop is good. A helper that wraps a LINQ chain is the same problem moved one file away — write the loop.

## Plain C# rules

| Rule | Do this |
| --- | --- |
| Explicit types | Declare the type: `List<Transaction> rows = ...`. Use `var` only when the right-hand side is a `new` of that exact type. |
| Field naming | Private fields are plain `camelCase` and are read through `this.` — `this.driver`, `this.waits`. No `_underscore` prefix. Constants and `static readonly` fields stay `PascalCase`. `.editorconfig` carries the rule, so the IDE and the build say the same thing. Either form is defensible in C# generally; this repository picks one so that no file has to be read twice. |
| One statement, one action | No more than two chained calls in a single statement. Split into named locals. |
| No ternaries in step definitions | Use `if` / `else`. Never nest a ternary anywhere. |
| No null-conditional chains | `a?.b?.c` hides which link was null. Check, name the problem, return or fail. |
| Guard clauses | Return early. Maximum nesting depth of two. |
| No magic values | Named `const` or `static readonly` with a meaningful name — not `"Unmatched"` scattered across six files. |
| No reflection, no `dynamic`, no expression trees | If a design needs them, the design is wrong for this codebase. |
| Regex only as a last resort | If unavoidable: a named `static readonly Regex` with a comment giving one example that matches. |
| Shallow inheritance | One level of base class, maximum. No generic base-class hierarchies, no framework inside the framework. |
| Small methods | Around 20 lines. More than three parameters means pass a record or an options object. |
| Full words | `transaction` not `txn`, `vatReturn` not `vr`, `reconciliation` not `rec`. Loop variables included. |
| Duplication beats premature abstraction | Test code should be DAMP (Descriptive And Meaningful Phrases), not strictly DRY. Extract on the third occurrence, and only when the name is obvious. |
| Comments say *why* | Naming carries the *what*. Delete commented-out code rather than committing it. |

## Scope of this rule

Applies to every `.cs` file in this repository, and to every C# snippet in documentation, steering files and skills — an example that uses LINQ teaches the wrong habit.

Does **not** apply to: `async` / `await`, Reqnroll table helpers such as `table.CreateSet<T>()`, or records and collection initialisers. None of those are LINQ and all of them are plain to read.

On `async` / `await` specifically, know which layer you are in:

| Layer | Sync or async | Why |
| --- | --- | --- |
| UI page objects and components | **Synchronous.** No `await`, no `Task`, no `*Async` suffix. | Selenium's C# API is synchronous. There is nothing to await, and wrapping it in `Task.Run` to look modern just adds a thread and hides the stack trace. |
| API clients (Refit) and the steps that call them | **Async.** `async Task` all the way down, `*Async` suffix. | Refit and `HttpClient` are async-only. |
| Step definitions | Async only if they touch the API layer. A UI-only step is a plain `void` method. | Match the layer being called. Reqnroll handles both. |

`.Result`, `.Wait()`, `.GetAwaiter().GetResult()` and `async void` stay banned everywhere.

## Enforcement

1. Code review rejects LINQ outside the three exceptions. Reviewers name the replacement loop rather than just flagging it — see the quality-enabler stance in the JD: coach, do not gatekeep.
2. If a reviewer and an author disagree on whether a case is a genuine exception, the loop wins. The bar for an exception is "the library will not compile without it", not "the chain is nicer".
3. When the framework is scaffolded, add an `.editorconfig` entry that raises a warning on `System.Linq` usage, so the rule is mechanical rather than a matter of reviewer memory.

## Related

- Layout and naming: #[[file:project-structure.md]]
- Pinned packages: #[[file:tech-stack.md]]
- Selenium waits, locators and the one allowed `Until` lambda: #[[file:ui-automation-selenium.md]]
