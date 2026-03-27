# Ledgerly — Architecture-First Roadmap (v9)

*Last Updated: Mar 22, 2026 — Phase 13 complete: 94 tests passing (unit + user isolation + auth flow integration)*

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
* [X] Export (CSV or JSON)

---

# Current Status (As of Mar 19, 2026)

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
* **Two-Factor Authentication (Phase 9 complete)**
  * TOTP via authenticator apps (Google Authenticator, Authy, etc.)
  * QR code + manual key setup flow in Settings
  * Login challenge step when 2FA is enabled
  * Demo account permanently exempt from 2FA
* **Export, Net Worth Chart & Overdue Notifications (Phase 10 complete)**
  * Export: `GET /export/json` (full snapshot) + `GET /export/csv` (ZIP of CSVs) with browser download
  * Net Savings Trend: 12-month cumulative savings chart on dashboard (line + bar)
  * Overdue notifications: daily background service emails users about unpaid expenses due within past 14 days
* **Security Hardening, Auth Improvements & New Features (Phase 11 complete)**
  * Rate limiting: sliding window (5 req/60s per IP) on all auth endpoints
  * Health check: `GET /health` with Npgsql connectivity probe
  * Refresh tokens: 15-min JWTs + 30-day rotating refresh tokens; silent auto-refresh in Web every 12 min
  * Google OAuth: "Sign in with Google" on login page; ExternalCookie + `/auth/google/complete` redirect flow
  * Serilog: structured logging to console + rolling file; EF/ASP noise filtered to Warning
  * Recurring transactions: marking a recurring `PlannedExpense` as paid auto-creates next month's copy
  * Savings goals: full CRUD (`SavingsGoal` entity, `/savings-goals` API, Savings Goals page with progress bars)
  * Dashboard chart fix: replaced `RadzenBarSeries` with `RadzenColumnSeries` for all vertical bar charts

## All v1.0 Items Complete ✓

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
* [X] Export working (CSV + JSON)

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

* [X] Export (CSV / JSON) — full user data snapshot
* [ ] CSV bank import — upload a bank statement, auto-categorize transactions
* [ ] Multi-currency support
* [ ] Public read-only share links — share a scenario view without requiring login
* [ ] Strategy comparison dashboard (enhanced visualization of Phase 3 comparison)
* [X] Push/email notifications for overdue planned expenses
* [X] Account balance tracking over time (net worth chart)

---

## Phase 9 — Two-Factor Authentication (Complete)

### TOTP via Authenticator App

* [X] `QRCoder` NuGet added to Api project — generates QR code PNG as Base64
* [X] `LoginResultDto` — new login response that signals `RequiresTwoFactor` when 2FA is enrolled
* [X] `POST /auth/login` — returns `RequiresTwoFactor: true` instead of JWT when user has 2FA enabled
* [X] `POST /auth/2fa/verify-login` — validates email + password + TOTP code, returns JWT
* [X] `GET /auth/2fa/setup` — `[Authorize]` — generates authenticator key + QR code for setup page
* [X] `GET /auth/2fa/status` — `[Authorize]` — returns `{isEnabled}` for Settings page
* [X] `POST /auth/2fa/enable` — `[Authorize]` — verifies first code and calls `SetTwoFactorEnabledAsync`
* [X] `POST /auth/2fa/disable` — `[Authorize]` — disables 2FA and resets authenticator key
* [X] Login.razor — two-step UI: password step then TOTP code step (no page navigation)
* [X] Settings.razor — new page at `/settings`; shows 2FA status, QR code setup flow, enable/disable
* [X] NavMenu "Settings" link updated to `/settings`; Change Password accessible from Settings page
* [X] Demo user (`demo@ledgerly.dev`) — seeder explicitly disables 2FA on every startup; shared demo account always accessible

### Local Testing Bug Fixes (Mar 19, 2026)

* [X] `Web:BaseUrl` in `appsettings.Development.json` corrected to `localhost:5136` (email confirmation links were pointing to wrong port)
* [X] `AuthApiClient` updated to inject `AuthTokenService` and use per-request `Authorization` headers for all `[Authorize]` endpoints (2FA setup/status/enable/disable, change-password)
* [X] Settings.razor updated to defer `GetTwoFactorStatusAsync` call until after `AuthTokenService` is initialized (avoids 401 on page load before localStorage token is restored)
* [X] Setup error now displayed in the pre-setup state so failures surface visibly

Outcome:
[X] Users can secure their account with any TOTP authenticator app. Zero additional cost (no SMS, no third-party service). Demo account remains freely accessible.

---

## Phase 10 — Export, Net Worth Chart & Overdue Notifications (Complete)

### Export (CSV + JSON)

* [X] `GET /export/json` — `[Authorize]` — full user data snapshot as a downloadable JSON file
* [X] `GET /export/csv` — `[Authorize]` — ZIP archive containing separate CSVs: accounts, debts, income sources, planned expenses, budget categories, transactions, scenarios
* [X] `ExportController` uses all existing services (auto-scoped to current user via query filters)
* [X] `ExportApiClient` in Web downloads raw bytes; `downloadBase64File` JS helper triggers browser save dialog
* [X] Export buttons (JSON + CSV) added to Settings page

### Net Savings Trend Chart

* [X] `FinancialSummaryService` in Application — queries 12 months of transactions, groups by CategoryType, computes monthly net + cumulative running total
* [X] `GET /dashboard/financial-summary` — returns `FinancialSummaryDto` with `TotalDebt`, `CumulativeSavings`, and 12 monthly snapshots
* [X] `DashboardApiClient` in Web
* [X] Dashboard (Home.razor) — "Net Savings Trend (12 Months)" card with cumulative line/area + monthly bar chart; loads in parallel with other dashboard data

### Overdue Expense Notifications

* [X] `OverdueExpenseNotifier : BackgroundService` in Infrastructure — runs every 24 hours
* [X] Queries all users' unpaid planned expenses due within the past 14 days using `IgnoreQueryFilters()`
* [X] Groups by user, fetches email from Identity, sends HTML reminder email via `IEmailService` (SendGrid)
* [X] Email includes due description, date, amount, and a link back to `/planned-expenses`
* [X] Registered as `AddHostedService<OverdueExpenseNotifier>()` in API Program.cs

### Local Testing Bug Fixes (Mar 19, 2026)

* [X] `SendGrid:ApiKey` populated in `appsettings.Development.json` with real key; `ledgerly.noreply@gmail.com` verified as Single Sender in SendGrid dashboard
* [X] Email confirmation flow end-to-end tested with Mailinator

Outcome:
[X] v1.0 is complete. Users can export their data, see savings progress over time, and receive timely reminders about overdue bills.

---

## Phase 11 — Security Hardening, Auth Improvements & New Features (Complete)

### Security Hardening

* [X] **Rate limiting on auth endpoints** — `AddRateLimiter` (built-in .NET 8) applied to `/auth/register`, `/auth/login`, `/auth/forgot-password`, `/auth/resend-confirmation`; sliding window: 5 requests / 60 seconds per IP; returns 429 on violation
* [X] **Health check endpoint** — `GET /health` via `AddHealthChecks().AddNpgSql(...)` with Npgsql database connectivity probe; suitable for Railway uptime monitoring

### Auth Improvements

* [X] **Refresh tokens** — `RefreshToken` entity with SHA256-hashed token stored in DB; 15-min JWTs + 30-day refresh tokens; `POST /auth/refresh` rotates (revokes old, issues new); Web auto-refreshes every 12 min via `Routes.razor` timer; token stored in `localStorage` as `refreshToken`; `POST /auth/logout` revokes server-side; `AddRefreshTokens` migration
* [X] **Google OAuth** — `Microsoft.AspNetCore.Authentication.Google` v8.0.0; "Sign in with Google" button on Login page; `GET /auth/google/login` initiates challenge; middleware handles `/signin-google` callback; `GET /auth/google/complete` issues JWT + refresh token; redirects to Web `/oauth-callback` page; new users auto-created with `EmailConfirmed = true`; `OAuthCallback.razor` page stores tokens and navigates home

### New Features

* [X] **Recurring transactions** — `PlannedExpenseService.MarkPaidAsync` automatically creates next month's copy when `IsRecurring` is true; same description, amount, category, and priority; new copy has `DueDate` advanced by one month, no paid state
* [X] **Savings goals** — `SavingsGoal` entity (Name, TargetAmount, CurrentAmount, TargetDate, computed Progress/Remaining); full CRUD at `GET/POST/PUT/DELETE /savings-goals`; `SavingsGoals.razor` page with inline add/edit form and color-coded progress bars (green ≥100%, amber ≥60%, blue otherwise); `AddSavingsGoals` migration; "Savings Goals" nav link added
* [X] **Dashboard chart fix** — replaced all `RadzenBarSeries` with `RadzenColumnSeries` for vertical bar charts; removed duplicate `RadzenAreaSeries` from Net Savings Trend that was causing a blank chart

### Observability

* [X] **Structured logging with Serilog** — `Serilog.AspNetCore` v8.0.3 + Console + File sinks; EF Database Command and ASP.NET noise filtered to Warning; rolling daily log files at `logs/ledgerly-.log` (7-day retention); `builder.Host.UseSerilog()` replaces default logger

---

## Phase 12 — Planning Intelligence & Account Reality (Planned)

*Goal: Transform Ledgerly from a data tracker into a decision-making tool. Users stop asking "am I okay?" and start knowing.*

---

### Feature 1 — Cash Flow Forecast (Short-Term Survival) ✓ Complete

**User story:** "Will I run out of money before my next paycheck?"

* [X] New **Cash Flow** page at `/cash-flow` with 30 / 60 / 90 day range selector
* [X] Daily balance line chart — starting balance ± income deposits ± planned expense deductions per day; labels thinned on 60/90-day views
* [X] Upcoming Events table — shows only days with activity (income or expense); color-coded balance column
* [X] Warning indicators: "Balance goes negative on April 23" / "Low balance (<$100) on April 19" (first occurrence only, deduplicated)
* [X] Key output panel: Starting balance · Lowest balance (with date) · Days until negative · Ending balance
* [X] "No risk detected" state when balance stays positive for the full window
* [X] Optional daily burn rate — flat daily deduction applied on top of scheduled bills (simulates average daily spend)
* [X] Data sources: manual starting balance + existing IncomeSources + PlannedExpenses (no bank sync)
* [X] Income date logic: Weekly = every 7 days; Biweekly = every 14 days; SemiMonthly = 1st & 15th; Monthly = 1st of month
* [X] Cash Flow nav link added to sidebar
* [X] Demo seeder updated: next 2 months of recurring bills seeded unpaid so forecast has data immediately on demo login

---

### Feature 2 — Account Model (Manual, No Sync) ✓ Complete

**User story:** "Where is my money actually sitting?"

* [X] `Balance` field added to `Account` entity; `AddAccountBalance` migration
* [X] Create / Edit forms include balance field; existing CRUD updated across all layers
* [X] Accounts page shows balance column (red if negative); edit pre-fills current balance
* [X] Transfer between accounts — `POST /accounts/transfer`; deducts from source, adds to destination; Accounts page shows transfer form when 2+ accounts exist
* [X] Dashboard: **Total Cash** summary card (sum of non-credit account balances); **Account Balances** panel shows each account as a card with balance
* [X] Cash Flow Forecast: account selector dropdown auto-fills starting balance from selected account's current balance; falls back to manual entry
* [X] Demo seeder: Chase Checking seeded with $3,240; Marcus Savings with $8,750

---

### Feature 3 — Net Worth Tracking ✅ COMPLETE

**User story:** "Am I actually getting wealthier?"

* [X] Net worth calculation: Assets (account balances) − Liabilities (debt balances)
* [X] Net Worth card on Dashboard: current net worth, total assets, total liabilities
* [X] 12-month trend chart — area chart (Net Worth) + lines (Assets, Liabilities) on dashboard
* [X] `NetWorthSnapshot` entity — UserId, Month (DateOnly), AssetsTotal, LiabilitiesTotal, NetWorth
* [X] `INetWorthSnapshotRepository` / `EfNetWorthSnapshotRepository` — fetches last 12 months
* [X] `NetWorthService.GetSummaryAsync()` — computes live current values + retrieves history
* [X] `GET /dashboard/net-worth` endpoint returning `NetWorthSummaryDto`
* [X] `DashboardApiClient.GetNetWorthSummaryAsync()` in Web layer
* [X] Demo seeder: 12 months of historical snapshots showing steady improvement
* [X] Migration: `AddNetWorthSnapshots`

---

### Feature 4 — Decision Recommendations (Rule-Based Insights) ✅ COMPLETE

**User story:** "Tell me what I should do."

* [X] Insight engine — deterministic, threshold-based rules; no LLM required
* [X] **Debt insights**: highest APR warning, monthly minimums as % of income (Info/Warning/Danger thresholds)
* [X] **Budget insights**: overdue bills (Danger), unpaid must-pay bills remaining (Warning), all-paid confirmation (Info)
* [X] **Cash flow insights**: shortfall alert if unpaid bills exceed cash (Danger), low cash warning, months-covered summary
* [X] Output style: short, blunt, actionable — emoji prefix (⚠/⚡/💡), color-coded by severity
* [X] `InsightService` in Application layer — pure, takes DTOs, returns `InsightsDto` with three lists
* [X] `GET /insights` endpoint — loads data in parallel, filters to current month, returns grouped insights
* [X] `InsightsApiClient` in Web layer
* [X] Insights panel on Dashboard — shows total count badge, color-coded cards (red/orange/blue)

---

### Feature 5 — Constraint-Based Scenario Planning (Goal Mode) ✅ COMPLETE

**User story:** "Can I hit this goal — and what will it take?"

* [X] Dedicated `/goals` page — "Goal Planner" with three goal types selectable via styled button toggle
* [X] **Debt-Free by Date** — PMT formula from weighted-average debt APR, back-calculates required monthly payment, compares to available capacity
* [X] **Save a Target Amount** — divides target by months, compares required savings rate to monthly surplus (income − expenses − debt minimums)
* [X] **Spending Cap** — compares monthly cap against current planned expenses, calculates overage
* [X] Feasibility indicator: On Track (green) / At Risk (orange) / Not Feasible (red) — with icon + color-coded border
* [X] Summary sentence + status detail + recommendation on shortfall
* [X] Three key number cards: Required / Current Capacity / Shortfall
* [X] `GoalPlannerService` — pure calculation, no DB; PMT formula handles zero-interest edge case
* [X] `POST /goal/plan` endpoint — loads debts, income, expenses; passes to service
* [X] `GoalPlannerApiClient` in Web layer
* [X] `ld-*` utility CSS classes added to `app.css` (also fixes CashFlow page styling)
* [X] Goal Planner nav link added to sidebar

---

### Phase 12 Positioning

With these five features Ledgerly becomes a **manual-input, privacy-first financial simulator with decision intelligence** — no bank integrations, no compliance overhead, but real planning, forecasting, and insight.

| Feature | What It Adds |
|---|---|
| Cash Flow Forecast | Survival — know before it's a crisis |
| Account Model | Reality — data reflects actual money |
| Net Worth Tracking | Momentum — proof of long-term progress |
| Decision Recommendations | Confidence — interpretation, not just data |
| Constraint-Based Planning | Strategy — goal-driven, not reactive |

---

## Phase 13 — Test Coverage & Validation

**Goal:** Close the test gap so the project demonstrates professional-grade quality assurance to any technical reviewer. Every pure service gets unit tests. User isolation gets an integration test. The existing 30 tests stay green.

**Current test baseline:** 30 tests in `tests/Ledgerly.Tests` — `CreditScoreService` (25), `JwtTokenService` (3), `DebtProjectionService` (7). Note: some overlap likely accounts for the 30 total.

---

### Feature 1 — Unit Tests: Pure Application Services ✅ COMPLETE

**Goal:** Every stateless service that takes inputs and returns outputs should have full branch coverage.

* [X] **`GoalPlannerService` tests** (`GoalPlannerServiceTests.cs`):
  * DebtFree — no debt → OnTrack immediately
  * DebtFree — target date in the past → NotFeasible
  * DebtFree — sufficient minimums → OnTrack (no extra required)
  * DebtFree — affordable extra payment → OnTrack
  * DebtFree — shortfall < 25% of PMT → AtRisk
  * DebtFree — shortfall ≥ 25% of PMT → NotFeasible
  * DebtFree — zero-interest debt (PMT = principal / months)
  * SaveAmount — required ≤ capacity → OnTrack
  * SaveAmount — shortfall < 30% of required → AtRisk
  * SaveAmount — shortfall ≥ 30% of required → NotFeasible
  * SpendingCap — spending under cap → OnTrack with surplus
  * SpendingCap — overage ≤ 10% of cap → AtRisk
  * SpendingCap — overage > 10% of cap → NotFeasible

* [X] **`InsightService` tests** (`InsightServiceTests.cs`):
  * No debts → "Add your debts" info insight returned
  * High-APR debt (≥ 24%) → Danger severity
  * Mid-APR debt (18–24%) → Warning severity
  * Low-APR debt → Info severity
  * Debt minimums ≥ 25% of income → Danger
  * Debt minimums 15–25% of income → Warning
  * Overdue bills → Danger insight with correct count and total
  * Unpaid must-pay bills → Warning insight
  * All bills paid → "All bills paid" info insight
  * No accounts → "Add your bank accounts" info
  * Cash < unpaid bills → Danger with correct shortfall
  * Cash < $500 → Warning low-cash insight
  * Healthy cash buffer → Info with months-covered calculation
  * Credit card accounts excluded from available cash calculation

* [X] **`CashFlowForecastService` tests** (`CashFlowForecastServiceTests.cs`):
  * 30/60/90-day forecast returns exact day count (Theory)
  * Starting balance propagates correctly to day 1 opening
  * No events, no burn — balance unchanged throughout
  * Expense on a known date deducts from closing balance
  * Daily burn rate compounds daily across the period
  * `DaysUntilNegative` is null when balance never goes negative
  * `DaysUntilNegative` is correct when balance crosses zero
  * `LowestBalance` and `LowestBalanceDate` identify the correct day
  * Warning generated when balance first goes negative
  * Low-balance warning generated without going negative

* [X] **`BudgetSummaryService` tests** (`BudgetSummaryServiceTests.cs`):
  * Actual spend below planned → under budget
  * Actual spend above planned → over budget, correct overage
  * Empty transactions → all categories show $0 actual
  * Summary totals match sum of category lines
  * Unplanned category appears with zero planned, negative variance
  * Empty plan + empty transactions → zero totals

---

### Feature 2 — Integration Tests: User Isolation ✅ COMPLETE

**Goal:** Prove that the EF Core global query filter actually prevents cross-user data access. This is the most important security property in the system.

* [X] **Setup**: EF Core InMemory database; `TestCurrentUser` mutable stub swaps active user between queries without rebuilding the context
* [X] User A `DebtAccounts` query → only User A's debt returned
* [X] User B `DebtAccounts` query → only User B's debt returned
* [X] User A queries User B's debt by ID → `null` (filter makes it invisible, not 403)
* [X] Isolation verified for `Accounts`, `IncomeSources`, `PlannedExpenses`
* [X] `IgnoreQueryFilters()` confirms both rows ARE in the store — filter is the reason they're hidden
* [X] Switching users on the same DbContext instance correctly re-scopes all four entity types

---

### Feature 3 — Integration Tests: Auth Flows ✅ COMPLETE

**Goal:** Prove that token issuance, protection, and invalidation actually work end-to-end.

* [X] Valid credentials → 200 with access token + refresh token
* [X] Invalid password → 401
* [X] Unconfirmed email → 403
* [X] Expired access token → 401 on protected endpoint
* [X] Valid refresh token → new access token issued
* [X] Used (rotated) refresh token → 401 on second use
* [X] `POST /auth/change-password` with wrong current password → 400
* [X] `POST /auth/change-password` with correct password → 200

---

### Feature 4 — Regression: Existing Tests Stay Green ✅ COMPLETE

* [X] All 94 tests pass after Phase 13 additions (85 pre-existing + 9 new auth flow tests)
* [X] `dotnet test` from solution root runs all test projects cleanly
* [X] No test depends on database state left by another test (proper isolation/teardown)

---

### Phase 13 Positioning

Tests are the difference between a portfolio project and a professional project. A hiring manager who clones this repo will run `dotnet test` within the first five minutes. Phase 13 makes that moment count.

| Test Category | What It Proves |
|---|---|
| GoalPlannerService unit tests | PMT math, feasibility logic, edge cases handled |
| InsightService unit tests | Threshold rules, severity logic, all branches covered |
| CashFlowForecastService unit tests | Day-by-day balance loop, warning triggers |
| BudgetSummaryService unit tests | Aggregation correctness |
| User isolation integration tests | Global query filter actually works in production conditions |
| Auth flow integration tests | JWT issuance, rotation, and rejection all verified |

**Target on completion:** 80+ tests, all green, covering every pure service and the two most critical security properties of the system.

---

### Phase 13 Implementation Notes

**Test infrastructure decisions:**

- `WebApplicationFactory<Program>` with the minimal-API hosting model (`WebApplication.CreateBuilder`) reads configuration eagerly during service registration — before `ConfigureAppConfiguration` callbacks run. Environment variables set in the factory constructor are the only reliable way to inject config values (e.g. `Jwt__Secret`) before `Program.cs` reads them. `ConfigureAppConfiguration` is still useful for non-critical overrides but cannot substitute for env vars when config is read at startup.

- `app.UseRateLimiter()` and the NpgSQL health check registration were both guarded for the `"Testing"` environment. Middleware guards (`app.Environment.IsEnvironment(...)`) work correctly because they're evaluated after `builder.Build()`, at which point `WebApplicationFactory`'s `UseEnvironment("Testing")` has taken effect. Service-registration guards do not work for the same reason config overrides don't.

- The EF Core global query filter user-isolation tests use a nested `private sealed class TestCurrentUser : ICurrentUserService` (not `file sealed class`) because `file`-local types cannot appear in method signatures of non-file-local members (CS9051).

- `PlannedExpense.IsPaid` is a computed property (`PaidDate.HasValue`), not directly settable. Test helpers leave `PaidDate = null` to represent unpaid expenses.

- Auth flow tests build expired JWTs manually using the test secret. The expiry must be at least 6 minutes in the past to exceed the JWT validator's default 5-minute clock skew allowance (`ClockSkew = TimeSpan.FromMinutes(5)`).

**Final test count:** 94 tests — 0 failed, 0 skipped.

---

## Phase 14 — UX Polish: First-Value Fast Lane

**Goal:** Reduce time-to-value for new users and improve the experience when pages have no data. A user who signs up and sees nothing will leave. Phase 14 fixes that.

---

### Feature 1 — Empty State Design ✅ COMPLETE

**Goal:** Every page with a data list gets a designed empty state — a helpful message and a direct action — instead of a blank content area.

* [X] Shared `<EmptyState>` Blazor component (`Components/Shared/EmptyState.razor`) — accepts `Title`, `Message`, `Icon`, and `ChildContent` action slot.
* [X] `Components/_Imports.razor` — added `@using Ledgerly.Web.Components.Shared`
* [X] **Debts page** — "No debt accounts yet." with icon and message
* [X] **Scenarios page** — "No scenarios yet." with icon and message
* [X] **Budget page** — "No budget plans yet." with icon and message
* [X] **Accounts page** — "No accounts yet." with icon and message
* [X] **Dashboard** — "Welcome to Ledgerly" hero card with "Build My Plan" CTA when user has no data; computed via `HasAnyData` property

---

### Feature 2 — Onboarding Wizard ✅ COMPLETE

**Goal:** Guide new users through minimum viable data entry (5 steps max) so they see a meaningful result within 90 seconds of signing up.

* [X] `/onboarding` page (`Components/Pages/Onboarding.razor`)
* [X] Step 1: Goal selection ("Get out of debt / Stay afloat / Build savings") — stored in wizard state only
* [X] Step 2: Income — name, amount, PayFrequency dropdown. Saved via `IncomeSourcesApiClient.CreateAsync` on Next
* [X] Step 3: Debts — dynamic list of (Name, Balance, APR, MinPayment) entries. Each saved via `DebtAccountsApiClient.CreateAsync` on Next
* [X] Step 4: Expenses — Rent, Food, Utilities, Everything else. Each saved via `PlannedExpensesApiClient.CreateAsync` (recurring, due next month 1st)
* [X] Step 5: Review summary → "Finish Setup" navigates to dashboard
* [X] Every step has "Skip" / "I'll add later" that advances without saving
* [X] "Skip setup" link on step 1 goes directly to dashboard
* [X] Nav link: "Setup Wizard" in sidebar

---

### Feature 3 — First-Win Dashboard Panel ✅ COMPLETE

**Goal:** Immediately surface one high-impact number so users feel value before exploring any other page.

* [X] `HasAnyData` computed property — false when debts, income, and accounts are all empty
* [X] When `!HasAnyData`: full-page welcome hero card with "Build My Plan" CTA → `/onboarding`
* [X] When data exists: `ld-hero-card` with computed `FirstWinHeadline` (priority: rough debt-free estimate → cash runway → surplus) shown above summary cards
* [X] `FirstWinSub` provides one-sentence action hint ("Run a Scenario to model strategies")

---

### Feature 4 — Scenario What-If Slider ✅ COMPLETE

**Goal:** Let users experiment with extra payments on the Scenarios page without committing changes — instant recalculation, no save required.

* [X] `GET /scenarios/{id}/projection?extraPaymentOverride=N` — controller sets `scenario.ExtraMonthlyPayment` on the in-memory copy before projecting
* [X] `ScenariosApiClient.GetProjectionAsync(scenarioId, extraPaymentOverride?)` — optional override param builds query string
* [X] Collapsible "What If?" toggle per scenario, below the projection result
* [X] `RadzenSlider<decimal>` from $0–$1,000 in $25 increments; `Change` event triggers `RunWhatIf`
* [X] Result shows: new payoff months, months saved, interest saved, total interest
* [X] "Apply to Scenario" button replaces the main projection result with the overridden one
* [X] Disclaimer: "Based on consistent monthly payments and no new debt"
* [X] Snowball inline tip: suggests Avalanche if using Snowball with multiple debts

---

### Feature 5 — Inline Contextual Recommendations ✅ COMPLETE

**Goal:** Surface relevant insights on the pages where action can be taken, not only on the dashboard.

* [X] **Scenarios page** — Snowball tip shown after projection when strategy is Snowball and 2+ debts
* [X] **Budget page** — Warning banner when selected plan's total lines exceed monthly income (`IncomeSourcesApiClient` added to Budget page)
* [X] **Cash Flow page** — Red danger card above the chart when `DaysUntilNegative < 30`
* [X] **Goals page** — Recommendation card already rendered by `GoalPlannerService` for AtRisk/NotFeasible results

---

### Feature 6 — Data Confidence Caveats ✅ COMPLETE

**Goal:** Prevent distrust of projections by being transparent about what is estimated vs. known.

* [X] `string? ConfidenceLevel = null` added to `GoalPlanResultDto` (optional positional parameter)
* [X] `GoalPlannerService.ComputeConfidence(GoalType, debts, income, expenses)` — returns "High" / "Medium" / "Low"
* [X] `Compute()` sets `ConfidenceLevel` via `with` expression on the returned record
* [X] Goals page shows `.ld-confidence` badge (green/amber/red) + "Actual results will vary" disclaimer
* [X] Scenarios page projection result shows disclaimer line: "Based on consistent monthly payments and no new debt"
* [X] Cash Flow forecast chart shows disclaimer line below the heading

---

### Feature 7 — CSV Transaction Import ✅ COMPLETE

**Goal:** Let users import transactions from a manual bank export without requiring bank integration.

* [X] `/import` page (`Components/Pages/Import.razor`) — "Import Transactions"
* [X] `InputFile` reads CSV up to 5 MB; custom `ParseCsv` handles quoted fields, flexible column order
* [X] Columns: `Date`, `Description`, `Amount` (case-insensitive; accepts `Memo`, `Name`, `Value` aliases)
* [X] Duplicate detection: same Date + Amount + Description → row pre-checked as skip with "duplicate" label
* [X] Preview table: checkbox per row, all-select, skip indicator
* [X] Category dropdown (required before import); amount stored as `Math.Abs` (CSV sign is for user reference)
* [X] Import loops `TransactionsApiClient.CreateAsync` per selected row; success notification
* [X] Nav link: "Import" in sidebar

---

### Phase 14 Implementation Notes

**Architecture decisions:**

- `EmptyState` component is in `Components/Shared/` — added namespace to `_Imports.razor` so no per-page import needed.
- What-If slider mutates the `Scenario` entity's `ExtraMonthlyPayment` in-memory before projecting. The entity is loaded with `GetScenarioEntityAsync` which is not change-tracked for write, so no accidental persistence.
- `GoalPlanResultDto.ConfidenceLevel` uses a default parameter (`= null`) on the positional record so all 7-argument constructors in the private service methods continue to compile.
- Budget page loaded `IncomeSourcesApiClient` alongside existing tasks in `WhenAll` — no additional render cycles.
- CSV import stores `Math.Abs(amount)` — the sign in the imported file indicates income/expense direction for the user's reference only; the assigned category determines the transaction type in Ledgerly's model.

---

### Phase 14 Positioning

Phase 13 proved the system is correct. Phase 14 makes it usable by real people.

| Feature | What It Fixes |
|---|---|
| Empty states | Blank pages that make users feel lost |
| Onboarding wizard | No path to first value for new signups |
| First-win dashboard | No immediate "aha" moment after login |
| What-if slider | Projections feel passive — users can't explore |
| Inline recommendations | Insights are dashboard-only, not where action happens |
| Data confidence caveats | Users distrust projections they can't verify |
| CSV import | Manual data entry is the biggest friction point |

**Target on completion:** A new user can sign up, enter minimal data, and see a meaningful financial projection in under 2 minutes.

---

End of Roadmap v11
