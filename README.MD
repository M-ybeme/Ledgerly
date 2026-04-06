# Ledgerly

*Last Updated: April 2026 — Phase 14 Complete | 94 tests passing*

A privacy-first financial simulation and planning tool built with .NET 8, Blazor Server, and PostgreSQL. No bank integrations. No credential storage. Manual data entry with deterministic modeling.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET 8, ASP.NET Core, EF Core 8 |
| Database | PostgreSQL (Npgsql 8) |
| Frontend | Blazor Server, Radzen.Blazor v9 |
| Auth | ASP.NET Core Identity + JWT + TOTP 2FA + Google OAuth |
| Email | SendGrid |
| Logging | Serilog (console + rolling file) |
| Tests | xUnit — 94 tests (unit + user-isolation + auth flow integration) |

---

## Architecture

Clean architecture with strict layer boundaries:

```
src/
  Ledgerly.Domain          # Entities, enums, core rules. No EF or infra references.
  Ledgerly.Contracts       # Sealed record DTOs and request types. References Domain only.
  Ledgerly.Application     # Use case services, repository interfaces, validation.
  Ledgerly.Infrastructure  # EF Core DbContext, repository impls, SendGrid, Serilog.
  Ledgerly.Api             # Controllers, JWT auth, DI wiring. Depends on Application only.
  Ledgerly.Web             # Blazor Server UI, API clients, view models.

tests/
  Ledgerly.Tests           # xUnit test project
```

Domain → Application → Infrastructure. Controllers never reference DbContext directly.

---

## Features

### Debt Payoff Engine
- Snowball and Avalanche strategies
- Month-by-month amortization with per-debt snapshots
- Total interest and payoff timeline
- Scenario duplication for "what-if" branching
- Side-by-side comparison: months saved, interest saved, winner label

### Reality Tracking
- Log actual payments per debt per scenario
- `DriftService` computes ahead/behind per debt and overall
- Updated payoff timeline recalculated from actual payment history

### Budget System
- Monthly budget plans with category-based lines
- Transaction CRUD linked by date range
- Planned vs actual summary with variance

### Cash Flow Forecast
- 30 / 60 / 90-day daily balance simulation
- Income and bill event markers
- Negative balance warnings, low-cash alerts, burn rate overlay

### Income & Planned Expenses
- Multiple income sources with frequency normalization to monthly values
- Recurring and one-off planned expenses with due dates and priority levels
- Paid / Overdue / Upcoming status tracking
- Recurring expense auto-creates next month's copy on mark-as-paid

### Credit Score Projection
- Range-based estimation (not FICO)
- 3-factor model: utilization, account age, payment history
- Month-by-month projections tied to debt payoff scenarios

### Net Worth Tracking
- Assets vs liabilities calculation
- Monthly snapshot system
- 12-month trend chart (area + line)

### Savings Goals
- Named goals with target amount, current amount, and target date
- Progress bars with color-coded status (On Track / At Risk / Complete)

### Insight Engine
- Rule-based financial insights (no AI)
- Budget overrun warnings, debt prioritization signals, cash shortfall alerts
- Severity levels: Info / Warning / Danger

### Goal Planner
- Debt-free-by-date and savings target constraints
- Outputs: required monthly effort, feasibility status, shortfall, confidence level

### Export / Import
- `GET /export/json` — full data snapshot
- `GET /export/csv` — ZIP archive (accounts, debts, income, expenses, transactions, scenarios)
- CSV transaction import with preview and category mapping

### Two-Factor Authentication
- TOTP via authenticator apps (Google Authenticator, Authy)
- QR code + manual key setup in Settings
- Login challenge step when 2FA is enabled

---

## Security

- Global EF query filters — all queries scoped to the authenticated user's ID
- JWT (15-min expiry) + rotating refresh tokens (30-day)
- Rate limiting on auth endpoints: sliding window, 5 req / 60s per IP
- TOTP 2FA support
- Google OAuth sign-in
- No bank credentials stored

---

## Running Locally

**Prerequisites:** .NET 8 SDK, Docker

```bash
# Start PostgreSQL
docker run -d --name ledgerly-postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres

# Apply migrations
dotnet ef database update --project src/Ledgerly.Infrastructure --startup-project src/Ledgerly.Api

# Run API
dotnet run --project src/Ledgerly.Api

# Run Web
dotnet run --project src/Ledgerly.Web

# Run tests
dotnet test
```

Configure `appsettings.Development.json` in `Ledgerly.Api` with connection string, JWT secret, and SendGrid key.

---

## Migrations

```bash
dotnet ef migrations add <Name> \
  --project src/Ledgerly.Infrastructure \
  --startup-project src/Ledgerly.Api

dotnet ef database update \
  --project src/Ledgerly.Infrastructure \
  --startup-project src/Ledgerly.Api
```

---

## Completed Phases

| Phase | Description |
|---|---|
| 0 | Foundation — EF Core, PostgreSQL, migrations |
| 0.5 | Clean architecture hardening |
| 1 | Debt projection engine (Snowball/Avalanche, unit tests) |
| 2 | Budget system (BudgetCategory, BudgetPlan, Transactions) |
| 3 | Scenario comparison and duplication |
| 4 | Reality tracking (ActualPayment, DriftService) |
| 5 | Credit score estimation (3-factor model) |
| 6 | UI/UX — Radzen.Blazor v9, dialogs, notifications |
| 7 | Auth — Identity + JWT + SendGrid + user isolation |
| 8 | Dashboard, income sources, planned expenses, dark/light theme |
| 9 | TOTP 2FA |
| 10 | Export (JSON + CSV), net worth chart, overdue email notifications |
| 11 | Rate limiting, refresh tokens, Google OAuth, Serilog, savings goals |
| 12 | Cash flow forecast, account model, net worth tracking |
| 13 | Insight engine, goal planner |
| 14 | Onboarding wizard, empty states, what-if slider, inline recommendations |
