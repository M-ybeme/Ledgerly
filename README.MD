# Ledgerly — Privacy-First Financial Intelligence Platform

*Last Updated: March 2026 — Phase 14 Complete*

---

# Overview

Ledgerly is a **privacy-first financial simulation and planning platform** designed to give users clarity, control, and confidence over their financial decisions — without requiring bank integrations or exposing sensitive credentials.

It is built around a simple principle:

> You should be able to understand and plan your financial future without giving anyone access to your accounts.

---

# Core Philosophy

Ledgerly deliberately avoids:

* Bank account integrations (Plaid, MX, etc.)
* Credential storage
* Third-party financial data dependencies

Instead, it focuses on:

* Manual but intentional data entry
* Deterministic financial modeling
* Transparent calculations
* Exportable, user-owned data

This results in:

* Lower security risk
* No compliance burden
* Full user control

---

# What Ledgerly Is

Ledgerly is a:

* Financial **simulation engine**
* Budgeting and **reality tracking tool**
* Decision-support system
* Scenario comparison platform

---

# What Ledgerly Is NOT

Ledgerly is not:

* A bank aggregator
* A passive tracking app
* A robo-advisor
* A replacement for financial institutions

---

# Key Features

## 1. Debt Payoff Engine

* Snowball and Avalanche strategies
* Month-by-month amortization
* Total interest and payoff timeline
* Scenario duplication and comparison

---

## 2. Budgeting System

* Monthly budget plans
* Category-based tracking
* Planned vs actual comparison
* Transaction logging

---

## 3. Planned Expenses (Bills)

* Recurring and one-off expenses
* Due dates and priority levels
* Paid / Overdue / Upcoming tracking
* Automatic recurrence generation

---

## 4. Income Modeling

* Multiple income sources
* Frequency normalization to monthly values
* Expected vs actual tracking

---

## 5. Credit Score Projection

* Range-based estimation (not FICO)
* Utilization, age, and payment history modeling
* Month-by-month projections tied to scenarios

---

## 6. Two-Factor Authentication

* TOTP via authenticator apps (Google Authenticator, Authy, etc.)
* QR code + manual key setup in Settings
* Login challenge step when 2FA is enabled

---

## 7. Export

* Full data snapshot as JSON
* ZIP archive of CSVs (accounts, debts, income, expenses, transactions, scenarios)
* CSV transaction import — upload a bank statement, preview and categorize rows before importing

---

## 8. Savings Goals

* Named goals with target amount, current amount, and target date
* Progress bars with color-coded status (on track / at risk / complete)

---

# Financial Intelligence Layer (Phases 12–14)

Ledgerly evolves from a tracking tool into a **decision-making system**.

---

## 9. Cash Flow Forecast (Survival Layer)

**Purpose:** Prevent short-term financial failure

* 30 / 60 / 90 day projections
* Daily balance simulation
* Income and bill event markers
* Negative balance warnings and low cash alerts
* Optional daily burn rate overlay

**Outcome:**
Users know *when* they will run out of money — before it happens.

---

## 10. Account Model (Reality Layer)

**Purpose:** Anchor all data to real-world balances

* Manual account tracking (checking, savings, cash)
* Current balance per account
* Transfers between accounts
* Dashboard cash and net worth visibility

**Outcome:**
Ledgerly reflects reality, not abstractions.

---

## 11. Net Worth Tracking (Progress Layer)

**Purpose:** Measure true financial growth

* Assets vs liabilities calculation
* Monthly snapshot system
* 12-month trend visualization (area + line chart)

**Outcome:**
Users see long-term progress, not just debt reduction.

---

## 12. Insight Engine (Decision Layer)

**Purpose:** Turn data into guidance

* Rule-based financial insights
* Budget overrun warnings
* Debt prioritization signals
* Cash shortfall alerts

**Design Principles:**

* Deterministic (no AI black box)
* Short and actionable
* Severity-based (Info / Warning / Danger)

**Outcome:**
Users don’t just see data — they know what to do.

---

## 13. Goal Planner (Strategy Layer)

**Purpose:** Enable constraint-based planning

* Debt-free by date
* Savings targets
* Spending caps

**Outputs:**

* Required monthly effort
* Feasibility status (On Track / At Risk / Not Feasible)
* Shortfall and recommendations
* Confidence level (High / Medium / Low)

**Outcome:**
Ledgerly becomes proactive, not reactive.

---

## 14. Onboarding & UX (Phase 14)

**Purpose:** Reduce time-to-first-value for new users

* Guided onboarding wizard (5 steps: goal → income → debts → expenses → review)
* Designed empty states on every data page
* First-win headline on dashboard (debt-free estimate, cash runway, or surplus)
* Scenario What-If slider — instant recalculation without saving
* Inline contextual recommendations on Scenarios, Budget, Cash Flow, and Goals pages

---

# User Loop

1. Complete the onboarding wizard (income, debts, expenses in under 90 seconds)
2. Create and compare debt payoff scenarios
3. Set budget and track transactions
4. Forecast cash flow (30/60/90-day)
5. Monitor net worth and savings goals
6. Review insights and inline recommendations
7. Adjust strategy via the goal planner
8. Export data; repeat monthly

---

# Architecture

Ledgerly follows a strict **Clean Architecture** approach:

```
Domain        → Core business models and rules
Application   → Use cases and services
Infrastructure→ Persistence and external concerns
API           → Controllers and auth
Web           → Blazor UI
```

---

# Security Model

Ledgerly prioritizes **risk reduction over convenience**:

* No bank credentials stored
* JWT-based authentication with 15-minute expiry
* Rotating refresh tokens (30-day)
* Rate-limited auth endpoints (sliding window, 5 req/60s per IP)
* TOTP 2FA support (authenticator app)
* Google OAuth sign-in
* Global EF query filters — users can only access their own data
* 94 tests including user-isolation integration tests and auth flow tests

---

# Data Ownership

Users retain full control of their data:

* Export as JSON (full snapshot) or CSV (ZIP of entity files)
* CSV transaction import from manual bank exports
* No vendor lock-in
* No hidden data processing

---

# Product Positioning

Ledgerly is best described as:

> A privacy-first financial simulator with decision intelligence

It sits between:

* Spreadsheets (flexible but manual)
* Budget apps (automated but shallow)

Ledgerly provides:

* Structure without lock-in
* Insight without surveillance

---

# Target Users

* Developers and technical users
* Privacy-conscious individuals
* People actively working on debt payoff
* Users who want to model scenarios, not just track history

---

# Roadmap Direction

Future expansion focuses on:

* Smarter insights (not AI fluff — real signals)
* Multi-goal optimization
* Advanced scenario modeling
* Public read-only share links for scenarios
* Multi-currency support

Ledgerly will not pursue:

* Bank integrations
* Ads or data monetization
* Black-box AI recommendations

---

# Final Statement

Ledgerly is built on a clear stance:

> Financial clarity should not require financial exposure.

This is not a compromise.
It is the product’s defining advantage.
