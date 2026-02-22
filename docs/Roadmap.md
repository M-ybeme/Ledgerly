# Ledgerly — Architecture-First Roadmap (v3)

*Last Updated: Feb 21, 2026 — Phase 6 (UI Polish) complete; Phase 7 (Production Readiness) pending user UI review*

---

# Product Vision

Ledgerly is a privacy-first financial simulation platform designed to model:

* Debt payoff strategies (Snowball, Avalanche)
* Side-by-side scenario comparison — the core insight engine
* Monthly budgeting (planned vs actual)
* Reality tracking — log what you actually paid, see drift, get updated projections
* Credit score estimation (range-based, assumption-driven)

Primary Goals:

1. Serve as a real personal financial planning tool.
2. Demonstrate mid-level .NET backend architecture competence.
3. Be deployable as a production-ready SaaS-style application.

---

# What "Complete" Feels Like to a User

A user opens Ledgerly and:

1. **Models their financial reality** — enters debts (name, balance, interest rate, minimum payment), sets extra monthly payment capacity.
2. **Runs a payoff plan** — picks Snowball or Avalanche, sees a month-by-month table: which debt is attacked each month, total interest paid, payoff date.
3. **Compares scenarios** — duplicates a scenario, changes one variable (e.g. +$100 extra/month, or Snowball vs Avalanche), sees side-by-side: months saved, interest saved. *This is the emotional core — it stops feeling like a calculator and starts feeling powerful.*
4. **Tracks reality** — each month, logs what they actually paid. Ledgerly shows: are they ahead or behind? Updated payoff timeline based on actual payments. The plan feels alive.
5. **Sees credit impact** — range-based score projection tied to their debt payoff strategy. "If you pay this off by month 18, your estimated score range is 680–720."
6. **Trusts the tool with their data** — logged in, data is theirs, can export to CSV or JSON backup. Feels like a product, not a demo.

---

# v1.0 Hard Cut

The minimum feature set to call this a complete v1.0:

* [X] Model debts with balance, rate, minimum payment
* [X] Run Snowball and Avalanche strategies with extra payment
* [X] **Compare scenarios side-by-side** (months saved, interest saved)
* [X] Log actual payments; see updated payoff timeline
* [ ] Export (CSV or JSON)
* [ ] JWT Authentication + user isolation
* [ ] Deployed to production

Credit estimation is a Phase 2 enrichment, not a v1.0 blocker.

---

# Current Status (As of Feb 21, 2026)

## Completed

* .NET 8 solution scaffolded
* PostgreSQL running in Docker (ledgerly-postgres container)
* EF Core 8 + Npgsql configured
* Migrations operational (InitialCreate → RemovePing → AddAccounts → AddDebtsAndScenarios → AddBudgetSystem)
* Accounts vertical slice working end-to-end (Web → API → DB)
* Swagger operational
* Clean baseline schema established
* **Full clean architecture implemented (Phase 0.5 complete)**
  * Ledgerly.Domain — Account entity, AccountType enum, no EF/infra references
  * Ledgerly.Contracts — AccountDto, CreateAccountRequest; references Domain
  * Ledgerly.Application — IAccountRepository interface, AccountService with consolidated ToDto mapping
  * Ledgerly.Infrastructure — EfAccountRepository, LedgerlyDbContext, AddLedgerlyInfrastructure DI extension
  * Ledgerly.Api — Controller depends only on AccountService, never DbContext; ProblemDetails error shape standardized
  * Ledgerly.Web — Blazor Server; AccountsApiClient extracts ProblemDetails detail on errors; Accounts page fully interactive
* Reference directions enforced: Domain ← Contracts ← Application, Infrastructure → Domain + Application, Api → Application + Infrastructure + Contracts, Web → Contracts
* **Debt Projection Engine implemented (Phase 1 complete)**
  * DebtAccount entity + full CRUD (API + service + EF repository)
  * Scenario entity with user-selected many-to-many debt associations
  * PayoffStrategy enum (Snowball / Avalanche)
  * DebtProjectionService — pure monthly amortization engine (no DB dependency)
  * Snowball and Avalanche strategies with extra payment cascading
  * GET /scenarios/{id}/projection endpoint — returns full month-by-month ProjectionResultDto
  * Debts.razor — full CRUD UI (create, inline edit, delete)
  * Scenarios.razor — scenario creation with debt multi-select, inline projection results table
  * xUnit test project (Ledgerly.Tests) — 7 passing tests validating projection correctness
* **Budget System implemented (Phase 2 complete)**
  * BudgetCategory entity (user-defined, typed Income/Expense) + full CRUD
  * BudgetPlan entity (freeform date range) with BudgetPlanLine children + full CRUD
  * Transaction entity (date-linked, no explicit plan FK) + full CRUD
  * BudgetSummaryService — pure planned vs actual computation with variance, income/expense totals, net
  * GET /budget-plans/{id}/summary endpoint — returns BudgetSummaryDto
  * Restrict FK: categories cannot be deleted if transactions exist (409 Conflict)
  * BudgetCategories.razor — category CRUD
  * Budget.razor — plan selector, create plan form with per-category amounts, planned vs actual summary table, transaction management
  * 5 additional unit tests for BudgetSummaryService (13 total passing)
* **Scenario Comparison implemented (Phase 3 complete)**
  * ScenarioComparisonService — pure service, projects both scenarios, computes MonthsSaved + InterestSaved + WinnerLabel
  * ScenarioSummaryDto + ScenarioComparisonDto contracts
  * GET /scenarios/compare?a={id}&b={id} endpoint
  * POST /scenarios/{id}/duplicate endpoint — creates named copy with same debts + strategy
  * Scenarios.razor — Duplicate button per scenario; Compare section with two dropdowns, side-by-side results table with winner banner
  * 13 unit tests still passing
* **Reality Tracking implemented (Phase 4 complete)**
  * ActualPayment entity — links Scenario + DebtAccount + date + amount
  * ActualPaymentService — CRUD with validation (debt must be in scenario, amount > 0)
  * DriftService — pure service; simulates actual vs projected month-by-month; recomputes payoff timeline from actual balances
  * GET /scenarios/{id}/payments, POST /scenarios/{id}/payments, DELETE /scenarios/{id}/payments/{paymentId}
  * GET /scenarios/{id}/drift — returns DriftSummaryDto with MonthsDrift, TotalInterestSaved, per-debt drift
  * Scenarios.razor — "This Month" plan panel with Log Payment modal; "View Drift" button with drift results table
  * Migration: AddActualPayments (cascade from Scenario, restrict from DebtAccount)
  * 19 total unit tests passing (6 new DriftService tests)
* **Credit Score Estimation implemented (Phase 5 complete)**
  * CreditProfile + CreditAccountProfile entities — per-scenario, hybrid linked/standalone accounts
  * CreditScoreService — pure 3-factor model: utilization, average account age, payment history recovery
  * Score always returned as a range [low, high], clamped to [300, 850]
  * GET/PUT/DELETE /scenarios/{id}/credit endpoints + GET /scenarios/{id}/credit/projection
  * Credit.razor — scenario selector, profile form with per-account rows, projection table, assumptions panel
  * Migration: AddCreditProfiles (cascade from Scenario, restrict from DebtAccount)
  * 25 total unit tests passing (6 new CreditScoreService tests)

## Not Yet Implemented

* ~~Reality tracking (actual payments + drift + updated projections)~~ ✓ Phase 4 complete
* ~~Credit score estimation~~ ✓ Phase 5 complete
* JWT Authentication + user isolation
* Production deployment
* Export (CSV / JSON)

---

# Target Architecture (Clean Layering)

```
src/
  Ledgerly.Web
  Ledgerly.Api
  Ledgerly.Application
  Ledgerly.Domain
  Ledgerly.Infrastructure

tests/
  Ledgerly.Tests
```

## Layer Responsibilities

### Ledgerly.Domain

* Core entities (Account, DebtAccount, Scenario, etc.)
* Value objects
* Enums
* Core business rules
* No EF Core references
* No infrastructure dependencies

This layer contains pure business concepts.

---

### Ledgerly.Application

* Use case services (DebtProjectionService, BudgetService, etc.)
* DTOs
* Validation
* Interfaces for repositories
* Business workflows

This layer orchestrates domain logic.

---

### Ledgerly.Infrastructure

* EF Core DbContext
* Repository implementations
* Persistence configuration
* External service integrations

Implements interfaces defined in Application.

---

### Ledgerly.Api

* Controllers
* Authentication
* DI wiring
* API configuration

Depends only on Application (never directly on EF).

---

### Ledgerly.Web

* Blazor UI
* HTTP client calls to API
* View models
* UI logic

---

# Phase Roadmap (Architecture-First)

---

## Phase 0 — Foundation (Complete)

* [X] EF Core configured
* [X] PostgreSQL connected
* [X] Initial migration pipeline validated
* [X] Basic Accounts slice functional

Outcome:
[X] Infrastructure proven stable.

---

## Phase 0.5 — Architectural Hardening (Complete)

Goal: Refactor to clean layering before complexity increases.

### Deliverables:

* [X] Create Ledgerly.Domain project — Account entity, AccountType enum, no infrastructure dependencies
* [X] Create Ledgerly.Contracts project — AccountDto, CreateAccountRequest; Contracts → Domain reference
* [X] Move Account entity and AccountType enum to Domain (removed from Contracts and Infrastructure)
* [X] Remove EF-specific concerns from entities
* [X] Create Ledgerly.Application project — IAccountRepository interface, AccountService
* [X] Consolidate Domain → DTO mapping into a single private ToDto() method in AccountService
* [X] Refactor AccountsController to depend on AccountService only (never DbContext directly)
* [X] Infrastructure implements IAccountRepository via EfAccountRepository
* [X] AddLedgerlyInfrastructure() DI extension method — DbContext + repository registration in one call
* [X] Standardize API error shape — AddProblemDetails() + UseExceptionHandler() + Problem() in controllers
* [X] Web error display — AccountsApiClient reads ProblemDetails.Detail and surfaces it to the UI
* [X] Accounts.razor marked @rendermode InteractiveServer — Create button and bindings functional

Outcome:
[X] Clear separation between business logic and persistence. Full vertical slice proven end-to-end.

---

## Phase 1 — Debt Projection Engine (Complete)

Backend:

* [X] DebtAccount entity
* [X] Scenario entity
* [X] Projection engine service
* [X] Snowball strategy
* [X] Avalanche strategy
* [X] Monthly amortization logic
* [X] Unit tests validating payoff correctness

Frontend:

* [X] Debts page
* [X] Scenario creation page
* [X] Projection run trigger
* [X] Results table (month-by-month)

Outcome:
[X] User can model debt payoff timeline and total interest.

---

## Phase 2 — Budget System (Complete)

Backend:

* [X] BudgetCategory entity
* [X] Transaction entity
* [X] BudgetPlan entity
* [X] Planned vs actual summary endpoint

Frontend:

* [X] Monthly budget dashboard
* [X] Transaction management (add, edit, delete)
* [X] Category summaries with variance

Outcome:
[X] User tracks spending against plan.

---

## Phase 3 — Scenario Comparison (Complete)

*This is the emotional core of the debt payoff tool. Without comparison, it's a calculator. With comparison, it's a decision-making tool.*

Backend:

* [X] ScenarioComparisonService — pure service; projects both scenarios, returns side-by-side summary
* [X] `GET /scenarios/compare?a={id}&b={id}` endpoint — returns `ScenarioComparisonDto`
* [X] `ScenarioComparisonDto` — ScenarioA summary, ScenarioB summary, MonthsSaved, InterestSaved, WinnerLabel
* [X] Scenario duplication: `POST /scenarios/{id}/duplicate` — creates a copy (same debts, same strategy, new name) for easy "what if" branching

Frontend:

* [X] Scenario list shows "Duplicate" button next to each scenario
* [X] Comparison selector — choose two scenarios from dropdowns, click Compare
* [X] Comparison results panel — side-by-side: Strategy | Extra Payment | Months | Interest | Total Paid | Difference highlighted

Outcome:
[X] User can clone a scenario, change one variable (e.g. +$100/month, Snowball vs Avalanche), and immediately see months saved and interest saved. The tool feels powerful.

---

## Phase 4 — Reality Tracking + Drift Recalculation (Complete)

*The plan feels alive when it reacts to what the user actually did.*

Backend:

* [X] ActualPayment entity — links to a Scenario + DebtAccount, records date and amount paid
* [X] `POST /scenarios/{id}/payments` — log an actual payment for a debt in this scenario
* [X] `GET /scenarios/{id}/payments` — list all payments for a scenario
* [X] `DELETE /scenarios/{id}/payments/{paymentId}` — remove a logged payment
* [X] DriftService — pure service; simulates actual vs projected month-by-month; computes ahead/behind per debt and overall
* [X] `GET /scenarios/{id}/drift` — returns DriftSummaryDto: ahead/behind in months, which debts are off-track, updated payoff date
* [X] Projection recalculation — rebuild from actual balances; return updated payoff timeline

Frontend:

* [X] "This Month" panel on Scenarios page — shows what the plan says to pay this month for each debt, with logged amounts highlighted
* [X] "Log Payment" button per debt in scenario view — opens a modal to record actual amount paid
* [X] Drift indicator — shows "X months ahead / behind" with color coding (green/red)
* [X] Updated payoff timeline — recalculated from actual payment history, shows new vs original months

Outcome:
[X] User sees whether they are ahead or behind their plan. The payoff date updates as they make real payments.

---

## Phase 5 — Credit Score Estimation (Range-Based) (Complete)

Backend:

* [X] CreditProfile entity — per-scenario, stores score range + payment history flag
* [X] CreditAccountProfile entity — hybrid (optional FK to DebtAccount or standalone), stores limit/balance/age/type
* [X] CreditScoreService — pure 3-factor model: utilization, average account age, payment history recovery
* [X] Utilization score table (0–9% → +50, 10–29% → +20, 30–49% → 0, 50–74% → -30, ≥75% → -60)
* [X] Age score table (<12mo → -30, 12–23 → -15, 24–59 → -5, 60–119 → 0, ≥120 → +15)
* [X] History recovery: dirty history recovers +50 points linearly over 84 months
* [X] `PUT /scenarios/{id}/credit` — upsert credit profile (delete + recreate pattern)
* [X] `GET /scenarios/{id}/credit` — retrieve current profile
* [X] `DELETE /scenarios/{id}/credit` — remove profile
* [X] `GET /scenarios/{id}/credit/projection` — month-by-month score range delta table
* [X] Migration: AddCreditProfiles (cascade from Scenario, restrict from DebtAccount)

Rules:

* [X] Always display a score range (e.g. 640–680), never a single number
* [X] Clearly disclose assumptions on the UI
* [X] Never claim FICO accuracy

Frontend:

* [X] Credit.razor page — scenario selector, credit profile form with per-account rows (linked debt or standalone), score range inputs
* [X] "Run Score Projection" button — month-by-month table: Month | Low | High | Utilization | Delta
* [X] Assumptions panel — always visible; lists factors modeled and factors not modeled
* [X] NavMenu — Credit nav item added

* 25 total unit tests passing (6 new CreditScoreService tests)

Outcome:
[X] User sees projected credit score impact of their payoff strategy. "If you finish by month 18, your estimated range is 680–720."

---

## Phase 6 — UI/UX Polish with Radzen Blazor (Complete)

* [X] Installed Radzen.Blazor v9.0.5
* [X] Configured Radzen CSS/JS in App.razor + `<RadzenComponents />` in MainLayout.razor
* [X] Added `AddRadzenComponents()` to Program.cs
* [X] Removed template pages: Counter.razor, Weather.razor
* [X] Replaced Home.razor with a proper welcome page with nav cards
* [X] Accounts.razor — RadzenDataGrid, RadzenTextBox, RadzenDropDown, RadzenButton, toast notifications
* [X] Debts.razor — RadzenDataGrid, RadzenNumeric, RadzenButton, toast notifications
* [X] BudgetCategories.razor — RadzenDataGrid, RadzenDropDown, RadzenButton, toast notifications
* [X] Budget.razor — RadzenDatePicker (with DateOnly proxies), RadzenNumeric, RadzenDataGrid, RadzenDropDown
* [X] Scenarios.razor — RadzenCard per scenario, RadzenDropDown multi-select, RadzenDataGrid (drift, this-month panel)
* [X] LogPaymentDialog extracted into Components/Dialogs/LogPaymentDialog.razor (Radzen dialog via DialogService)
* [X] Credit.razor — RadzenDropDown, RadzenCheckBox, RadzenNumeric, RadzenDataGrid for projection table
* [X] All 25 unit tests still passing

Outcome:
[X] All pages use Radzen components consistently. Log Payment modal uses DialogService. Toast notifications replace inline error text. No leftover template code.

---

## Phase 7 — Production Readiness

* JWT Authentication — user registration, login, token issuance
* User isolation — all entities scoped to userId; no cross-user data access
* Railway deployment — API + Web + PostgreSQL hosted
* Environment variable configuration — no secrets in code
* JSON backup export — full user data snapshot

Outcome:
Ledgerly behaves like a lightweight SaaS product. Users own their data. The application is live.

---

# Definition of "v1.0 Complete"

The minimum bar to call this a shipped v1.0 product:

* Clean layered architecture demonstrated end-to-end
* EF Core migrations stable and documented
* Domain logic separated from persistence
* Projection engine unit tested
* Budget system functional
* **Scenario comparison working** (the insight differentiator)
* Reality tracking: actual payments logged, drift shown, projection updated
* Export working (CSV or JSON)
* Authentication + user isolation working
* Deployed and publicly accessible

---

# Definition of "Portfolio Complete"

What a technical reviewer evaluating the project would assess:

* Domain modeling maturity (entities, enums, no EF in domain)
* Financial calculation correctness (unit tested projection engine, budget summary)
* Clean API boundaries (controllers depend only on services, never DbContext)
* Service layer patterns (pure computation services, repository interfaces, DI)
* EF Core competence (migrations, cascade rules, many-to-many, AsNoTracking discipline)
* Production deployment competence (Railway, environment config, JWT)

All of the above are demonstrated by completing through Phase 6.

---

# Future Expansion (Optional)

These are genuinely optional features that extend the product after v1.0:

* CSV bank import — upload a bank statement, auto-categorize transactions
* Multi-currency support
* Public read-only share links — share a scenario view without requiring login
* Mobile-optimized layout
* Strategy comparison dashboard (enhanced visualization of Phase 3 comparison)

---

End of Roadmap v3
