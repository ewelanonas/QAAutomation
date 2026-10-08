---
inclusion: manual
---

# Domain glossary — KEYinfinity

Pull this file in when writing feature files, naming capabilities, or deciding what a scenario should prove. Its purpose is that Gherkin reads in the business's language rather than the automation's. Wording rules: #[[file:bdd-gherkin-standards.md]]. Coverage techniques: #[[file:test-design-techniques.md]].

## How to read this file

Much of what follows is **generalised from the role brief and public UK accounting/tax practice, not verified against Landmark Systems' internals**. Anything inferred is marked:

| Marker | Meaning |
| --- | --- |
| **(ASSUMPTION)** | Inferred. Confirm with the Product Manager or a domain SME before building scenarios on it. |
| *(general UK practice)* | True of UK accounting/tax generally; how KEYinfinity implements it is unconfirmed. |

Deliberately absent: VAT rates, HMRC endpoint paths, API field names, box-number mappings and submission payload shapes. Those are factual details that must come from the specification or HMRC's published documentation, not from this file. Do not invent them in a scenario.

## 1. Products

| Term | Meaning |
| --- | --- |
| KEYPrime | The legacy desktop product. Established accounting and property/land management system with an existing customer base and years of accumulated behaviour. |
| KEYinfinity | The cloud successor: multi-tenant SaaS, Vue 3 + TypeScript front end, .NET/C# services, PostgreSQL, Azure + Kubernetes, Azure DevOps CI/CD. The system under test. |
| Model-driven platform | An internal platform that **generates parts of the application** from a model. Drives the locator strategy in #[[file:ui-automation-selenium.md]] and means contract drift can appear from a regeneration rather than a hand-written change. |
| Migration **(ASSUMPTION)** | Customers are expected to move from KEYPrime to KEYinfinity, so parity of calculated outputs (VAT figures, balances, reconciliation states) is likely a test concern in its own right. Confirm whether parity testing is in scope for this suite. |

Testing implication: where KEYinfinity replaces KEYPrime behaviour, the oracle may be "what KEYPrime did", not a written spec. Confirm who owns that oracle before asserting on it.

## 2. UK VAT and Making Tax Digital (MTD)

### Core concepts *(general UK practice)*

| Term | Meaning |
| --- | --- |
| VAT | Value Added Tax. A business charges it on sales (output tax) and reclaims it on purchases (input tax). |
| Output tax / input tax | VAT charged to customers / VAT paid to suppliers and reclaimable. |
| VAT return | A periodic declaration summarising output and input tax for a period, producing an amount payable to or reclaimable from HMRC. Composed of numbered boxes — **do not hardcode box meanings here; take them from HMRC guidance**. |
| VAT period | The accounting period a return covers. Commonly quarterly; monthly and annual arrangements exist. |
| VAT scheme | Variations such as standard accounting, cash accounting, flat rate and annual accounting, which change how and when VAT is recognised. Which schemes KEYinfinity supports is **(ASSUMPTION)** — confirm. |
| VAT registration number (VRN) | The business identifier used for VAT. Treat as sensitive — never commit a real one; generate test values. |
| Reverse charge / partial exemption / domestic VAT edge cases | Known sources of calculation complexity in UK VAT. Whether they apply to KEYinfinity's customers is **(ASSUMPTION)**. |

### MTD obligations and submission lifecycle

Making Tax Digital requires VAT records to be kept digitally and returns to be submitted to HMRC through software rather than typed into a web form.

| Term | Meaning |
| --- | --- |
| Obligation | A period HMRC says the business must report for, with a start date, end date and due date, in an open or fulfilled state. Retrieved from HMRC rather than invented locally. |
| Fraud prevention headers | HMRC requires a set of headers identifying the originating device and software on MTD calls. Exact header set is published by HMRC — read it from the specification, never from memory. |
| Submission | Sending the finalised figures for an obligation to HMRC. Legally meaningful and effectively irreversible. |
| Declaration / finalisation | The user's explicit confirmation that the figures are correct, required before submission *(general UK practice)*. |
| Liability / payment | What is owed (or reclaimable) after a return is accepted, and its settlement. |

A lifecycle **(ASSUMPTION)** — confirm the real state model before writing state-transition coverage:

1. Draft — figures accumulating from transactions, freely editable.
2. Ready for submission — period closed, figures finalised, declaration made.
3. Submitting — in flight to HMRC.
4. Accepted — HMRC acknowledged; a receipt/reference is stored.
5. Rejected — HMRC refused; the error must be surfaced in business language and the return must remain correctable.

Testing implications:

1. The submission boundary is stubbed. `WireMock.Net` stands in for HMRC in every CI run — **CI must never call a real tax authority**. See #[[file:api-automation.md]].
2. The high-value coverage is the unhappy path: timeout mid-submission, duplicate submission, rate limiting, HMRC unavailable, authorisation expired, and a rejection payload.
3. Duplicate submission deserves its own scenario. Submitting twice for one obligation is a real-world incident, so idempotency must be proven, not assumed.
4. An accepted submission must leave an auditable record — who declared, when, what figures, what HMRC returned. **(ASSUMPTION)** that KEYinfinity stores this; confirm, then assert on it.
5. State transitions (draft → finalised → submitted) are a state-transition design problem, including the invalid transitions — see #[[file:test-design-techniques.md]].

## 3. Bank reconciliation

Proving that the accounting records agree with what the bank actually did.

| Term | Meaning |
| --- | --- |
| Statement import | Loading bank transactions, by file upload (CSV/OFX/CAMT) or a bank feed. Format support is **(ASSUMPTION)** — confirm. |
| Matching | Pairing a statement line with the ledger entry it represents, automatically by rule or manually by the user. |
| Part-matching | One statement line settling several ledger entries, or one ledger entry settled by several statement lines. The messiest area and a rich source of defects. |
| Unmatched | A statement line with no ledger counterpart, or a ledger entry the bank has not shown. Both directions matter. |
| Adjustment | A correction posted during reconciliation — bank charges, interest, rounding, an error write-off. |
| Reconciled balance | The agreed closing position once every line is matched or explained. |
| Re-import / duplicate detection | Importing an overlapping statement must not create duplicate transactions. |

Testing implications:

1. Boundary-rich by nature: zero-value lines, exact-amount ties, same-amount-same-day candidates, dates on a period boundary, negative amounts, foreign currency **(ASSUMPTION — confirm multi-currency support)**.
2. Part-matching needs decision-table coverage (one-to-many, many-to-one, over-settled, under-settled, partially reversed).
3. An import must be idempotent. Re-importing the same statement is an explicit scenario, not an edge case.
4. Reconciliation state is a prerequisite for believable VAT figures, so cross-capability E2E coverage is justified: import, reconcile, then produce the return.
5. A reconciliation must never silently lose a transaction. Assert on counts and totals, not only on the UI's success banner.

## 4. Invoicing and payments

| Term | Meaning |
| --- | --- |
| Sales invoice | A charge raised to a customer or tenant, carrying lines, VAT treatment, dates and a total. |
| Purchase invoice | A supplier's charge to the business. |
| Credit note | A reversal or partial reversal of an invoice. |
| Invoice lifecycle | Draft → approved/issued → part-paid → paid → (credited / written off) **(ASSUMPTION)** — confirm the real states and who may move between them. |
| Payment / receipt | Money out to a supplier / money in from a customer or tenant. |
| Allocation | Applying a payment against one or more invoices. Unallocated cash on account is a legitimate state. |
| Recurring charge **(ASSUMPTION)** | Property and estate work usually involves periodic rent and service charges raised on a schedule. Confirm whether KEYinfinity generates these. |
| Aged debt / arrears | Outstanding amounts grouped by age. A reporting concern and a likely snapshot-assertion target. |

Testing implications:

1. Rounding and VAT-at-line-versus-at-total are classic defect sites. Use boundary values and name the expected result in the `Examples` table.
2. Overpayment, underpayment, part-allocation and payment reversal each need their own scenario.
3. Payments must be idempotent — a double-submitted payment is a real-world incident. Cover the duplicate explicitly.
4. Invoicing output feeds VAT figures, so an invoicing defect surfaces as a VAT defect. Keep that traceability in scenario naming.

## 5. Permissions and roles

| Term | Meaning |
| --- | --- |
| Role | A named bundle of permissions. Likely examples **(ASSUMPTION)**: accountant/finance user, property or estate manager, read-only viewer, tenant administrator, support/internal user. Confirm the real role model. |
| Permission | A specific allowed operation (approve an invoice, submit a VAT return, import a statement, view another entity's data). |
| Approval / segregation of duties **(ASSUMPTION)** | Financial systems commonly require that the person who raises a transaction is not the person who approves it. Confirm whether KEYinfinity enforces this — if it does, it is high-value coverage. |
| Entity-level scope **(ASSUMPTION)** | A user may be restricted to particular farms, estates or properties rather than the whole tenant. Confirm; it materially changes the authorisation matrix. |

Testing implications:

1. Authorisation is server-side. A hidden button is not an access control — prove refusal at the API, per #[[file:api-automation.md]].
2. Every `@permissions` capability needs the full matrix: permitted role succeeds, under-privileged role refused, unauthenticated refused, expired credential refused, cross-tenant refused.
3. A refusal must change no state. Follow a refused write with a read that proves it.
4. Role-based UI differences deserve their own `@ui @permissions` scenarios, with a session established per role so identities cannot leak between scenarios — see the authenticated-session rules in #[[file:ui-automation-selenium.md]].

## 6. Farm and estate structures — what makes this domain unusual

This is the part that does not look like a generic accounting package, and the part most likely to be mis-modelled by someone new to it.

Rural and property businesses are **hierarchical and multi-entity by nature**. A single client relationship can span several legal entities, several trading enterprises and many physical land parcels, with costs and income that must be attributed across all three dimensions at once.

An illustrative hierarchy **(ASSUMPTION — the real model must be confirmed before any test data builder is written)**:

| Level | Example | Why it matters to testing |
| --- | --- | --- |
| Client / group | A family farming business | May own several legal entities. |
| Legal entity | A partnership, a limited company, a trust, an individual | VAT registration and reporting attach here, not at group level. |
| Estate / holding | A named estate | Groups properties and land under common management. |
| Enterprise / cost centre | Arable, livestock, let property, diversification | Costs and income are attributed per enterprise for management reporting. |
| Property / unit | A farmhouse, a cottage, a commercial let, a building | Tenancies, rent and service charges attach here. |
| Land parcel / field | A field with an area and a code | Land-based activity and subsidy/scheme references attach here. |

Testing implications:

1. **Hierarchy depth is the test risk.** Totals must aggregate correctly up the tree and must not double-count where an item belongs to several dimensions.
2. A VAT return is produced for a **VAT-registered entity**, which may be only part of a group. Scenarios must state which entity they mean, in business language.
3. Permission scope probably follows this hierarchy **(ASSUMPTION)**, so authorisation tests need entity-level cases, not just role-level ones.
4. Test data builders (`*Builder` in `Core/TestData`) must be able to construct a realistic multi-level structure. Flat test data will pass tests the real data would fail.
5. Terminology must come from the business. If the business says "holding", the Gherkin says holding — not "organisational unit".
6. Reorganisations (a property moving between estates, an entity being added) are plausible state transitions worth confirming and covering.

## 7. Multi-tenancy — an explicit test concern

KEYinfinity is multi-tenant SaaS. Multiple customers share infrastructure, so **isolation is a functional requirement that must be tested, not an infrastructure detail assumed to work**.

| Term | Meaning |
| --- | --- |
| Tenant | One customer organisation's isolated slice of data and configuration. |
| Tenant context | How a request is attributed to a tenant — header, token claim or path segment. **Unconfirmed**; see #[[file:api-automation.md]]. |
| Tenant isolation | The guarantee that no tenant can read, write or infer another tenant's data. |
| Cross-tenant leak | The failure mode: data, identifiers, counts, error messages or search results from another tenant becoming visible. The highest-severity defect class in this product. |
| Tenant-scoped configuration **(ASSUMPTION)** | Tenants may differ in enabled features, VAT schemes or role definitions. Confirm — it affects whether one test tenant is representative. |
| Noisy-neighbour | One tenant's load degrading another's experience. Usually a performance concern, out of scope for functional BDD. |

Mandatory coverage, tagged `@multitenant`:

1. **Cross-tenant read refused.** Authenticated as tenant A, request a resource known to belong to tenant B; assert the agreed refusal code and assert the error body leaks nothing — no name, no identifier, no existence hint.
2. **Cross-tenant write refused**, with a follow-up read proving tenant B's state is unchanged.
3. **Listing and search are tenant-scoped.** Seed both tenants with similar records, then assert a list returns only the caller's — including counts and pagination totals, which are a common leak route.
4. **Reports and exports are tenant-scoped.** A VAT return or aged debt report must never aggregate across tenants.
5. **Tenant-scoped identifiers do not collide.** The same invoice number or reference in two tenants must stay separate.
6. Every non-tenancy scenario creates its own data in its own tenant and must not depend on cross-tenant state, per #[[file:bdd-gherkin-standards.md]].

Practical notes:

1. Keep at least two seeded test tenants. Isolation cannot be tested with one.
2. Keep a cached session and token **per role and per tenant**, so no scenario can borrow another tenant's identity by accident.
3. Never log a tenant identifier alongside customer-identifying data, and never commit real tenant names.

## Open questions for the Product Manager

1. **Role model** — what are the actual roles and permissions, and is there entity-level (farm/estate/property) scope on top of role?
2. **Entity hierarchy** — what is the real structure and its terminology, and which level owns VAT registration?
3. **VAT scope** — which VAT schemes does KEYinfinity support, and are partial exemption, reverse charge or multi-currency in scope?
4. **MTD lifecycle** — what are the real return states, who may finalise and submit, and what is stored as the audit record of a submission?
5. **MTD sandbox** — is an HMRC sandbox available to QA, under what credential policy, and is it ever permitted in a pipeline (default answer: no)?
6. **Tenant context** — is tenancy carried by header, token claim or path, and what is the agreed refusal code for a cross-tenant request (403 or 404)?
7. **Test tenants** — how many seeded tenants will QA have, and can tenants be created and torn down on demand?
8. **Bank reconciliation** — which statement formats and bank feeds are supported, and what are the automatic matching rules?
9. **Part-matching rules** — what combinations are permitted, and what are the agreed limits?
10. **Recurring charges** — does KEYinfinity generate periodic rent and service charges, and on what schedule?
11. **Segregation of duties** — is raise-versus-approve separation enforced, and where?
12. **KEYPrime parity** — is parity with legacy calculated output in scope for this suite, and who owns the expected values?
13. **Model-driven platform** — can the generator emit `data-testid` from the template layer, and who owns that change? See #[[file:ui-automation-selenium.md]].
14. **Test data** — is there a sanctioned, non-production dataset for farm/estate structures, and what is the policy on data retention in test environments?
15. **Environments** — which environments can automation run against, and can every third-party integration be pointed at a stub in each of them?
