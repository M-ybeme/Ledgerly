# Ledgerly — Architecture-First Roadmap (v4)

*Last Updated: Mar 14, 2026 — Phase 8 (Dashboard & UX Enhancements) complete; Export remaining for v1.0*

---

# Product Vision

Ledgerly is a privacy-first financial simulation platform designed to model:

* Debt payoff strategies (Snowball, Avalanche)
* Side-by-side scenario comparison — the core insight engine
* Monthly budgeting (planned vs actual)
* Reality tracking — log what you actually paid, see drift, get updated projections
* Credit score estimation (range-based, assumption-driven)
* Income tracking and planned expense management

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
6. **Manages income and bills** — tracks income sources, planned monthly expenses, marks bills paid, sees this month's cash flow at a glance.
7. **Trusts the tool with their data** — logged in, data is theirs, can export to CSV or JSON backup. Feels like a product, not a demo.

---

# v1.0 Hard Cut

The minimum feature set to call this a complete v1.0:

* [X] Model debts with balance, rate, minimum payment
* [X] Run Snowball and Avalanche strategies with extra payment
* [X] **Compare scenarios side-by-side** (months saved, interest saved)
* [X] Log actual payments; see updated payoff timeline
* [X] JWT Authentication + user isolation
* [X] Deployed to production (Railway)
* [ ] Export (CSV or JSON)

---

# Current Status (As of Mar 14, 2026)

## Completed

* .NET 8 solution scaffolded
* PostgreSQL running in Docker (ledgerly-postgres container)
* EF Core 8 + Npgsql configured
* Migrations operational through AddIncomeSourcesPlannedExpensesMonthlyBudgets + AddPlannedExpensePriority
* **Full clean architecture implemented (Phase 0.5 complete)**
* **Debt Projection Engine (Phase 1 complete)**
* **Budget System (Phase 2 complete)**
* **Scenario Comparison (Phase 3 complete)**
* **Reality Tracking (Phase 4 complete)**
* **Credit Score Estimation (Phase 5 complete)**
* **UI/UX Polish with Radzen Blazor (Phase 6 complete)**
* **Production Readiness — Auth + Deployment (Phase 7 complete)**
* **Dashboard & UX Enhancements (Phase 8 complete)**
  * Income Sources, Planned Expenses, Monthly Budgets
  * Full dashboard rebuild with charts
  * Dark/light theme
  * Mobile-responsive navbar
  * Demo data seeder

## Not Yet Implemented

* Export (CSV / JSON) — last remaining v1.0 item

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

* Blazor Server
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

## Phase 7 — Production Readiness (Complete)

### Authentication & User Isolation

* [X] ASP.NET Core Identity installed (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`)
* [X] `ApplicationUser : IdentityUser<Guid>` in Infrastructure/Auth
* [X] `LedgerlyDbContext` extended to `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`
* [X] `ICurrentUserService` interface in Application layer; `CurrentUserService` in Api reads JWT `sub` claim
* [X] Global EF query filters on all 7 root entities (Account, DebtAccount, Scenario, BudgetCategory, BudgetPlan, Transaction, IncomeSource, PlannedExpense, MonthlyBudget) — all scoped to `UserId`
* [X] `UserId` FK added to all root entities; migration sets existing rows to sentinel `00000000-...`
* [X] All 7 existing controllers decorated with `[Authorize]`
* [X] All repository Create methods set `entity.UserId = _currentUser.UserId`

### JWT

* [X] `Microsoft.AspNetCore.Authentication.JwtBearer` installed
* [X] `JwtTokenService` — reads `Jwt:Secret/Issuer/Audience/ExpiryHours` from config; issues tokens with `sub` + `email` + `jti` claims
* [X] `PostConfigure` used to guarantee JWT as the default scheme (overrides Identity's cookie challenge)

### AuthController

* [X] `POST /auth/register` — creates Identity user, sends confirmation email; returns 201 (no token until confirmed)
* [X] `POST /auth/login` — validates password, checks email confirmed, returns `AuthTokenDto`
* [X] `GET /auth/confirm-email?email=&token=` — confirms Identity email token
* [X] `POST /auth/forgot-password` — generates reset token, sends email; always returns 200
* [X] `POST /auth/reset-password` — validates reset token, updates password
* [X] `POST /auth/change-password` — `[Authorize]`; requires current password
* [X] `GET /auth/me` — `[Authorize]`; returns email + id

### Email

* [X] `IEmailService` interface in Application layer
* [X] `SendGridEmailService` in Infrastructure — reads `SendGrid:ApiKey` and `SendGrid:FromEmail`

### Web Auth Layer

* [X] `AuthTokenService` — scoped per circuit; holds token + email; raises `OnChange` event
* [X] `BearerTokenHandler` — `DelegatingHandler` that injects `Authorization: Bearer` header on all API calls
* [X] `LedgerlyAuthStateProvider` — extends `AuthenticationStateProvider`; parses JWT claims from token
* [X] JWT stored in `localStorage` ("authToken" / "authEmail"); restored in `MainLayout.OnAfterRenderAsync(firstRender)`
* [X] `Routes.razor` uses `<AuthorizeRouteView>` with `<NotAuthorized><RedirectToLogin /></NotAuthorized>`
* [X] Login.razor, Register.razor, ConfirmEmail.razor, ForgotPassword.razor, ResetPassword.razor, ChangePassword.razor

### Deployment

* [X] `src/Ledgerly.Api/Dockerfile`
* [X] `src/Ledgerly.Web/Dockerfile`
* [X] `appsettings.Production.json` for both Api and Web
* [X] `.env.example` at solution root documenting all required environment variables
* [X] Railway deployment — API + Web + managed PostgreSQL
* [X] `db.Database.MigrateAsync()` at API startup — migrations auto-apply on Railway deploy
* [X] Demo user (`demo@ledgerly.dev` / `Demo1234!`) seeded at startup in all environments

Outcome:
[X] Ledgerly behaves like a lightweight SaaS product. Users own their data. The application is live on Railway.

---

## Phase 8 — Income, Planned Expenses & Dashboard Rebuild (Complete)

### New Domain Entities

* [X] `IncomeSource` entity — Name, Amount, PayFrequency enum (Weekly/Biweekly/SemiMonthly/Monthly), optional AccountId, computed `MonthlyAmount`
* [X] `PlannedExpense` entity — Description, PlannedAmount, DueDate, CategoryId (optional), IsRecurring, Priority (`ExpensePriority` enum: MustPay/WantToPay), ActualAmount, PaidDate; computed `IsPaid`
* [X] `MonthlyBudget` entity — Month (DateOnly, first of month), CategoryId, Amount; per-user budget allocation

### New API Endpoints

* [X] `GET/POST/PUT/DELETE /income-sources` — full CRUD for income sources
* [X] `GET/POST/PUT/DELETE /planned-expenses` — full CRUD for planned expenses
* [X] `POST /planned-expenses/{id}/mark-paid` — records actual amount and paid date
* [X] `POST /planned-expenses/{id}/mark-unpaid` — clears payment
* [X] `GET/PUT /monthly-budgets` — read and set monthly budget allocations

### New Migrations

* [X] `AddIncomeSourcesPlannedExpensesMonthlyBudgets` — adds all three tables
* [X] `AddPlannedExpensePriority` — adds `Priority` integer column to PlannedExpenses

### New Pages

* [X] `IncomeSources.razor` — CRUD table for income sources with frequency and monthly equivalent display
* [X] `PlannedExpenses.razor` — CRUD table with priority badges, status badges (Paid/Overdue/Upcoming), mark-paid flow
* [X] NavMenu updated — Planned Expenses link added

### Dashboard Rebuild (Home.razor)

* [X] Summary cards — Total Debt, Expected Monthly Income, Actual Income This Month, Planned Expenses (with overdue count)
* [X] Cash Flow chart with **Last 6 Months / This Month toggle** (`RadzenSelectBar`)
  * Last 6 Months: grouped bar chart — Expected Income, Actual Income, Planned Expenses, Actual Expenses per month
  * This Month: grouped bar chart — Expected vs Actual for Income and Expenses
* [X] Priority breakdown pie chart — MustPay vs WantToPay proportion with progress bars and paid amounts
* [X] Planned Expenses table — inline edit (description, due date, amount, priority), inline mark-paid with actual amount input
* [X] Mark-paid updates Actual Expenses bar on chart immediately (uses ActualAmount from PlannedExpenses, not Transactions)
* [X] Debt Balances horizontal bar chart
* [X] Quick-action cards — Accounts, Categories, Income Sources, Planned Expenses

### Chart Improvements Across App

* [X] **Credit.razor** — Area chart showing Score Low/High trajectory over projection period (above the data table)
* [X] **Scenarios.razor** — Line chart showing total remaining debt declining over time (per projection result)
* [X] **Budget.razor** — Horizontal grouped bar chart (Planned vs Actual per category) shown when summary is loaded
* [X] All chart heights tuned to fit on a standard screen without scrolling

### Dark / Light Theme

* [X] CSS variable-based theming (`--ld-bg-page`, `--ld-bg-card`, `--ld-text-primary`, etc.)
* [X] `[data-theme="dark"]` selector overrides all Radzen CSS variables for dark mode
* [X] Anti-FOUC inline script in `App.razor` — applies saved theme before Blazor renders (no flash)
* [X] Toggle button (sun/moon icon) in nav footer — persists preference to `localStorage`
* [X] Smooth `transition` on background/color changes

### Mobile-Responsive Navbar

* [X] Hamburger button in `MainLayout.razor` (outside sidebar) — toggles `navOpen` state
* [X] Sidebar uses `transform: translateX(-100%)` on mobile, slides in with `.page.nav-open` class
* [X] Full-screen backdrop overlay closes sidebar on tap
* [X] Sidebar auto-closes on any navigation (`Nav.LocationChanged`)
* [X] Mobile topbar fixed at top (56px) with hamburger + Ledgerly logo + name
* [X] Desktop sidebar unchanged — sticky flex item, always visible

### Brand & Visual Identity

* [X] Ledgerly logo + "Ledgerly" wordmark in sidebar nav-brand and mobile topbar
* [X] Brand CSS variable system (`--ld-primary`, `--ld-secondary`, `--ld-tertiary`, etc.)
* [X] Sidebar gradient updated from generic navy-purple to teal-forward (`#054849` → `#021e1e`) matching logo
* [X] `RadzenSelectBar` active item styled with brand lime-green
* [X] Radzen CSS variables overridden globally to match brand colors

### Demo Data Seeder

* [X] Comprehensive `DbSeeder.SeedDemoUserAsync` — creates demo user + full financial dataset on first startup
* [X] **All dates are relative to `DateTime.Today`** — data always looks current regardless of when the app starts
* [X] Seeded data: 2 accounts, 2 income sources, 3 debts, 3 scenarios, 11 planned expenses (past-due ones auto-marked paid), 6 months of budget plans + ~20 transactions/month, credit profile
* [X] Scenario actual payments seeded for past months (Avalanche scenario drift tracking works out of the box)

Outcome:
[X] Ledgerly has a polished, brand-consistent UI that works on mobile. The dashboard gives a real financial snapshot at a glance. New users can explore with demo data that always looks live.

---

# Definition of "v1.0 Complete"

The minimum bar to call this a shipped v1.0 product:

* [X] Clean layered architecture demonstrated end-to-end
* [X] EF Core migrations stable and documented
* [X] Domain logic separated from persistence
* [X] Projection engine unit tested
* [X] Budget system functional
* [X] **Scenario comparison working** (the insight differentiator)
* [X] Reality tracking: actual payments logged, drift shown, projection updated
* [X] Authentication + user isolation working
* [X] Deployed and publicly accessible
* [ ] Export working (CSV or JSON) ← last remaining item

---

# Definition of "Portfolio Complete"

What a technical reviewer evaluating the project would assess:

* [X] Domain modeling maturity (entities, enums, no EF in domain)
* [X] Financial calculation correctness (unit tested projection engine, budget summary)
* [X] Clean API boundaries (controllers depend only on services, never DbContext)
* [X] Service layer patterns (pure computation services, repository interfaces, DI)
* [X] EF Core competence (migrations, cascade rules, many-to-many, AsNoTracking discipline)
* [X] Production deployment competence (Railway, environment config, JWT)
* [X] UI/UX quality (Radzen components, dark/light theme, mobile-responsive, brand consistency)
* [X] Real-world data modeling (income sources, planned expenses, cash flow, credit scoring)

---

# Future Expansion (Optional)

These are genuinely optional features that extend the product after v1.0:

* [ ] Export (CSV / JSON) — full user data snapshot; last v1.0 blocker
* [ ] CSV bank import — upload a bank statement, auto-categorize transactions
* [ ] Multi-currency support
* [ ] Public read-only share links — share a scenario view without requiring login
* [ ] Strategy comparison dashboard (enhanced visualization of Phase 3 comparison)
* [ ] Push/email notifications for overdue planned expenses
* [ ] Account balance tracking over time (net worth chart)

---

End of Roadmap v4
