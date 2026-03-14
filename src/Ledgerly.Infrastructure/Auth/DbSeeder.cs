using Ledgerly.Domain.Accounts;
using Ledgerly.Domain.Budget;
using Ledgerly.Domain.Credit;
using Ledgerly.Domain.Debts;
using Ledgerly.Domain.Income;
using Ledgerly.Domain.Scenarios;
using Ledgerly.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Ledgerly.Infrastructure.Auth;

public static class DbSeeder
{
    private const string DemoEmail    = "demo@ledgerly.dev";
    private const string DemoPassword = "Demo1234!";

    public static async Task SeedDemoUserAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // If user already exists the data was already seeded — nothing to do.
        if (await userManager.FindByEmailAsync(DemoEmail) is not null)
            return;

        var user = new ApplicationUser
        {
            UserName  = DemoEmail,
            Email     = DemoEmail,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Demo user seed failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        var db  = scope.ServiceProvider.GetRequiredService<LedgerlyDbContext>();
        var uid = user.Id;

        // All dates are relative to today so the demo stays "fresh" indefinitely.
        var today      = DateOnly.FromDateTime(DateTime.Today);
        var thisMonth  = new DateOnly(today.Year, today.Month, 1);

        // ── 1. ACCOUNTS ──────────────────────────────────────────────────────
        var checkingAcct = new Account { Id = Guid.NewGuid(), UserId = uid, Name = "Chase Checking",   Type = AccountType.Checking };
        var savingsAcct  = new Account { Id = Guid.NewGuid(), UserId = uid, Name = "Marcus Savings",   Type = AccountType.Savings  };
        db.Accounts.AddRange(checkingAcct, savingsAcct);

        // ── 2. INCOME SOURCES ─────────────────────────────────────────────────
        // Biweekly paycheck: $2,450/check → ~$5,308/month
        // Monthly side income: $600
        var incSalary = new IncomeSource
        {
            Id = Guid.NewGuid(), UserId = uid,
            Name = "Apex Software — Salary", Amount = 2_450m,
            Frequency = PayFrequency.Biweekly,
            AccountId = checkingAcct.Id,
        };
        var incSide = new IncomeSource
        {
            Id = Guid.NewGuid(), UserId = uid,
            Name = "Freelance Dev Work", Amount = 600m,
            Frequency = PayFrequency.Monthly,
            AccountId = checkingAcct.Id,
        };
        db.IncomeSources.AddRange(incSalary, incSide);

        // ── 3. DEBT ACCOUNTS ──────────────────────────────────────────────────
        var debtStudent = new DebtAccount
        {
            Id = Guid.NewGuid(), UserId = uid,
            Name = "Federal Student Loan",
            Balance = 22_400m, AnnualInterestRate = 0.065m, MinimumPayment = 275m,
        };
        var debtCar = new DebtAccount
        {
            Id = Guid.NewGuid(), UserId = uid,
            Name = "Honda Civic Auto Loan",
            Balance = 14_800m, AnnualInterestRate = 0.059m, MinimumPayment = 320m,
        };
        var debtVisa = new DebtAccount
        {
            Id = Guid.NewGuid(), UserId = uid,
            Name = "Capital One Visa",
            Balance = 3_650m, AnnualInterestRate = 0.2199m, MinimumPayment = 73m,
        };
        db.DebtAccounts.AddRange(debtStudent, debtCar, debtVisa);

        // ── 4. BUDGET CATEGORIES ──────────────────────────────────────────────
        var catSalary   = Cat(uid, "Salary",         CategoryType.Income);
        var catSideInc  = Cat(uid, "Side Income",    CategoryType.Income);
        var catRent     = Cat(uid, "Housing",        CategoryType.Expense);
        var catGrocery  = Cat(uid, "Groceries",      CategoryType.Expense);
        var catTransport= Cat(uid, "Transportation", CategoryType.Expense);
        var catUtilities= Cat(uid, "Utilities",      CategoryType.Expense);
        var catDining   = Cat(uid, "Dining Out",     CategoryType.Expense);
        var catSubs     = Cat(uid, "Subscriptions",  CategoryType.Expense);
        var catHealth   = Cat(uid, "Healthcare",     CategoryType.Expense);
        var catDebt     = Cat(uid, "Debt Payments",  CategoryType.Expense);
        var catShopping = Cat(uid, "Shopping",       CategoryType.Expense);
        db.BudgetCategories.AddRange(
            catSalary, catSideInc, catRent, catGrocery, catTransport,
            catUtilities, catDining, catSubs, catHealth, catDebt, catShopping);

        // ── 5. SCENARIOS ──────────────────────────────────────────────────────
        var scenMin = new Scenario
        {
            Id = Guid.NewGuid(), UserId = uid,
            Name = "Minimum Payments Only", ExtraMonthlyPayment = 0m,
            Strategy = PayoffStrategy.Avalanche,
        };
        var scenAvalanche = new Scenario
        {
            Id = Guid.NewGuid(), UserId = uid,
            Name = "Debt Avalanche (+$800/mo)", ExtraMonthlyPayment = 800m,
            Strategy = PayoffStrategy.Avalanche,
        };
        var scenSnowball = new Scenario
        {
            Id = Guid.NewGuid(), UserId = uid,
            Name = "Debt Snowball (+$400/mo)", ExtraMonthlyPayment = 400m,
            Strategy = PayoffStrategy.Snowball,
        };
        // Assign all three debts to each scenario via the navigation collection
        scenMin.DebtAccounts      = [debtStudent, debtCar, debtVisa];
        scenAvalanche.DebtAccounts = [debtStudent, debtCar, debtVisa];
        scenSnowball.DebtAccounts  = [debtStudent, debtCar, debtVisa];
        db.Scenarios.AddRange(scenMin, scenAvalanche, scenSnowball);

        // ── 6. CREDIT PROFILE (for Avalanche scenario) ───────────────────────
        var creditProfile = new CreditProfile
        {
            Id = Guid.NewGuid(), ScenarioId = scenAvalanche.Id,
            CurrentScoreRangeLow = 682, CurrentScoreRangeHigh = 710,
            PaymentHistoryIsClean = true,
        };
        creditProfile.Accounts =
        [
            new CreditAccountProfile
            {
                Id = Guid.NewGuid(), CreditProfileId = creditProfile.Id,
                DebtAccountId = debtVisa.Id, Name = "Capital One Visa",
                AccountType = CreditAccountType.Revolving,
                CreditLimit = 6_000m, CurrentBalance = 3_650m, AgeMonths = 36,
            },
            new CreditAccountProfile
            {
                Id = Guid.NewGuid(), CreditProfileId = creditProfile.Id,
                DebtAccountId = debtCar.Id, Name = "Honda Auto Loan",
                AccountType = CreditAccountType.Installment,
                CreditLimit = 20_000m, CurrentBalance = 14_800m, AgeMonths = 18,
            },
            new CreditAccountProfile
            {
                Id = Guid.NewGuid(), CreditProfileId = creditProfile.Id,
                DebtAccountId = debtStudent.Id, Name = "Federal Student Loan",
                AccountType = CreditAccountType.Installment,
                CreditLimit = 30_000m, CurrentBalance = 22_400m, AgeMonths = 72,
            },
        ];
        db.CreditProfiles.Add(creditProfile);

        // ── 7. PLANNED EXPENSES (recurring monthly bills) ────────────────────
        // These are always due in the current month with relative dates.
        // Bills whose due day has passed are marked paid; future ones are upcoming.
        var plannedExpenses = new List<PlannedExpense>
        {
            PE(uid, "Rent",                1_350m,  1, catRent.Id,      isRecurring: true,  priority: ExpensePriority.MustPay,   today),
            PE(uid, "Car Payment",           320m,  5, catDebt.Id,      isRecurring: true,  priority: ExpensePriority.MustPay,   today),
            PE(uid, "Netflix",                18m,  8, catSubs.Id,      isRecurring: true,  priority: ExpensePriority.WantToPay, today),
            PE(uid, "Spotify",                12m,  8, catSubs.Id,      isRecurring: true,  priority: ExpensePriority.WantToPay, today),
            PE(uid, "Student Loan",          275m, 10, catDebt.Id,      isRecurring: true,  priority: ExpensePriority.MustPay,   today),
            PE(uid, "Electricity",            88m, 14, catUtilities.Id, isRecurring: true,  priority: ExpensePriority.MustPay,   today),
            PE(uid, "Internet",               72m, 18, catUtilities.Id, isRecurring: true,  priority: ExpensePriority.MustPay,   today),
            PE(uid, "Phone Bill",             90m, 20, catUtilities.Id, isRecurring: true,  priority: ExpensePriority.MustPay,   today),
            PE(uid, "Gym Membership",         40m, 22, catHealth.Id,    isRecurring: true,  priority: ExpensePriority.WantToPay, today),
            PE(uid, "Groceries (monthly)",   380m, 27, catGrocery.Id,   isRecurring: true,  priority: ExpensePriority.MustPay,   today),
            // Non-recurring one-time expense this month
            PE(uid, "Car Registration",      195m, 20, catTransport.Id, isRecurring: false, priority: ExpensePriority.MustPay,   today),
        };
        db.PlannedExpenses.AddRange(plannedExpenses);

        // ── 8. BUDGET PLANS + TRANSACTIONS (6 months) ────────────────────────
        for (int i = 5; i >= 0; i--)
        {
            var mStart = thisMonth.AddMonths(-i);
            var mEnd   = mStart.AddMonths(1).AddDays(-1);
            bool isPast = i > 0;   // past months have full data; current month is partial

            var plan = new BudgetPlan
            {
                Id = Guid.NewGuid(), UserId = uid,
                Name      = mStart.ToString("MMMM yyyy"),
                StartDate = mStart,
                EndDate   = mEnd,
            };

            plan.Lines =
            [
                Line(plan.Id, catSalary.Id,    5_308m),
                Line(plan.Id, catSideInc.Id,     600m),
                Line(plan.Id, catRent.Id,      1_350m),
                Line(plan.Id, catGrocery.Id,     380m),
                Line(plan.Id, catTransport.Id,   140m),
                Line(plan.Id, catUtilities.Id,   250m),
                Line(plan.Id, catDining.Id,      180m),
                Line(plan.Id, catSubs.Id,         90m),
                Line(plan.Id, catHealth.Id,       60m),
                Line(plan.Id, catDebt.Id,        668m),  // 275 + 320 + 73
                Line(plan.Id, catShopping.Id,    120m),
            ];
            db.BudgetPlans.Add(plan);

            // Transactions for this month
            // --- Income ---
            Tx(db, uid, "Apex Software — Paycheck", 2_450m, mStart.AddDays(0),  catSalary.Id);
            Tx(db, uid, "Apex Software — Paycheck", 2_450m, mStart.AddDays(13), catSalary.Id);
            if (isPast || today.Day >= 20)
                Tx(db, uid, "Apex Software — Paycheck", 2_450m, mStart.AddDays(27), catSalary.Id);
            Tx(db, uid, "Freelance — Client Invoice", 600m, mStart.AddDays(4), catSideInc.Id);

            // --- Fixed expenses (always) ---
            Tx(db, uid, "Rent Payment",         1_350m, mStart.AddDays(0),  catRent.Id);
            Tx(db, uid, "Honda Auto Loan",         320m, mStart.AddDays(4),  catDebt.Id);
            Tx(db, uid, "Student Loan Payment",    275m, mStart.AddDays(9),  catDebt.Id);
            Tx(db, uid, "Capital One — Min Pay",    73m, mStart.AddDays(14), catDebt.Id);
            Tx(db, uid, "Netflix",                  18m, mStart.AddDays(7),  catSubs.Id);
            Tx(db, uid, "Spotify",                  12m, mStart.AddDays(7),  catSubs.Id);
            Tx(db, uid, "Gym Membership",           40m, mStart.AddDays(21), catHealth.Id);
            Tx(db, uid, "Phone Bill",               90m, mStart.AddDays(19), catUtilities.Id);

            // --- Variable utilities ---
            decimal elec = isPast ? 82m + (mStart.Month % 3) * 8m : 88m;
            Tx(db, uid, "Electricity Bill",  elec, mStart.AddDays(13), catUtilities.Id);
            Tx(db, uid, "Internet Bill",      72m, mStart.AddDays(17), catUtilities.Id);

            // --- Groceries (weekly) ---
            Tx(db, uid, "Whole Foods",         92m, mStart.AddDays(2),  catGrocery.Id);
            Tx(db, uid, "Trader Joe's",        88m, mStart.AddDays(9),  catGrocery.Id);
            if (isPast || today.Day >= 16)
            {
                Tx(db, uid, "Whole Foods",     95m, mStart.AddDays(16), catGrocery.Id);
                Tx(db, uid, "Costco Run",     118m, mStart.AddDays(23), catGrocery.Id);
            }

            // --- Dining out ---
            Tx(db, uid, "Chipotle",             14m, mStart.AddDays(3),  catDining.Id);
            Tx(db, uid, "Local Brewery",        42m, mStart.AddDays(8),  catDining.Id);
            Tx(db, uid, "Sushi Place",          68m, mStart.AddDays(15), catDining.Id);
            if (isPast)
                Tx(db, uid, "Pizza Night",      32m, mStart.AddDays(22), catDining.Id);

            // --- Transportation (gas) ---
            Tx(db, uid, "Shell Gas Station",    55m, mStart.AddDays(5),  catTransport.Id);
            if (isPast || today.Day >= 20)
                Tx(db, uid, "Shell Gas Station",58m, mStart.AddDays(20), catTransport.Id);

            // --- Extra debt payment (Avalanche scenario — visa first) ---
            if (isPast)
            {
                decimal extra = i > 3 ? 800m : 600m; // reduced extra recently
                db.ActualPayments.Add(new ActualPayment
                {
                    Id = Guid.NewGuid(), ScenarioId = scenAvalanche.Id,
                    DebtAccountId = debtVisa.Id,
                    PaymentDate = mStart.AddDays(14), Amount = extra,
                });
            }

            // --- Occasional shopping ---
            if (isPast && i % 2 == 0)
                Tx(db, uid, "Amazon Purchase", 64m + i * 7m, mStart.AddDays(11), catShopping.Id);
        }

        await db.SaveChangesAsync();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static BudgetCategory Cat(Guid uid, string name, CategoryType type) =>
        new() { Id = Guid.NewGuid(), UserId = uid, Name = name, Type = type };

    private static BudgetPlanLine Line(Guid planId, Guid catId, decimal amount) =>
        new() { Id = Guid.NewGuid(), BudgetPlanId = planId, CategoryId = catId, PlannedAmount = amount };

    private static void Tx(LedgerlyDbContext db, Guid uid, string desc, decimal amount, DateOnly date, Guid catId) =>
        db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(), UserId = uid,
            Description = desc, Amount = amount, Date = date, CategoryId = catId,
        });

    /// <summary>
    /// Creates a planned expense for the current month.
    /// Bills whose due day is strictly before today are marked as paid automatically.
    /// </summary>
    private static PlannedExpense PE(
        Guid uid, string desc, decimal amount, int dueDay,
        Guid? catId, bool isRecurring, ExpensePriority priority, DateOnly today)
    {
        var dueDate = new DateOnly(today.Year, today.Month, dueDay);
        bool isPaid = dueDate < today;

        return new PlannedExpense
        {
            Id = Guid.NewGuid(), UserId = uid,
            Description = desc, PlannedAmount = amount,
            DueDate = dueDate, CategoryId = catId,
            IsRecurring = isRecurring, Priority = priority,
            ActualAmount = isPaid ? amount : null,
            PaidDate     = isPaid ? dueDate : null,
        };
    }
}
