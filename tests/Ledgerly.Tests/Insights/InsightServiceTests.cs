using Ledgerly.Application.Insights;
using Ledgerly.Contracts.Accounts;
using Ledgerly.Contracts.Budget;
using Ledgerly.Contracts.Debts;
using Ledgerly.Contracts.Income;
using Ledgerly.Contracts.Insights;
using Ledgerly.Domain.Accounts;
using Ledgerly.Domain.Budget;
using Ledgerly.Domain.Income;

namespace Ledgerly.Tests.Insights;

public class InsightServiceTests
{
    private static readonly InsightService Svc = new();
    private static readonly DateOnly Today = new(2026, 3, 22);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static DebtAccountDto Debt(string name, decimal balance, decimal aprDecimal, decimal min) =>
        new(Guid.NewGuid(), name, balance, aprDecimal, min, DateTime.UtcNow);

    private static IncomeSourceDto Income(decimal monthly) =>
        new(Guid.NewGuid(), "Salary", monthly, PayFrequency.Monthly, monthly, null, null, DateTime.UtcNow);

    private static PlannedExpenseDto Bill(decimal amount, DateOnly due, bool paid = false,
        ExpensePriority priority = ExpensePriority.MustPay) =>
        new(Guid.NewGuid(), "Bill", amount, due, null, null, false, paid, null, null, priority);

    private static AccountDto Account(decimal balance,
        AccountType type = AccountType.Checking) =>
        new(Guid.NewGuid(), "Bank", type, balance, DateTime.UtcNow);

    private InsightsDto Compute(
        List<DebtAccountDto>? debts = null,
        List<IncomeSourceDto>? income = null,
        List<PlannedExpenseDto>? expenses = null,
        List<AccountDto>? accounts = null) =>
        Svc.ComputeAll(debts ?? [], income ?? [], expenses ?? [], accounts ?? [], Today);

    // ── Debt Insights ─────────────────────────────────────────────────────────

    [Fact]
    public void NoDebts_ReturnsAddYourDebtsInfo()
    {
        var result = Compute();

        var debtInsight = Assert.Single(result.Debt);
        Assert.Equal(InsightSeverity.Info, debtInsight.Severity);
        Assert.Contains("Add your debts", debtInsight.Message);
    }

    [Fact]
    public void HighAprDebt_AboveTwentyFourPercent_DangerSeverity()
    {
        var result = Compute(debts: [Debt("Capital One", 5000m, 0.26m, 100m)]);

        var aprInsight = result.Debt.First(i => i.Message.Contains("APR"));
        Assert.Equal(InsightSeverity.Danger, aprInsight.Severity);
        Assert.Contains("Capital One", aprInsight.Message);
    }

    [Fact]
    public void MidAprDebt_EighteenToTwentyFourPercent_WarningSeverity()
    {
        var result = Compute(debts: [Debt("Chase", 3000m, 0.20m, 60m)]);

        var aprInsight = result.Debt.First(i => i.Message.Contains("APR"));
        Assert.Equal(InsightSeverity.Warning, aprInsight.Severity);
    }

    [Fact]
    public void LowAprDebt_BelowEighteenPercent_InfoSeverity()
    {
        var result = Compute(debts: [Debt("Auto Loan", 8000m, 0.06m, 200m)]);

        var aprInsight = result.Debt.First(i => i.Message.Contains("APR"));
        Assert.Equal(InsightSeverity.Info, aprInsight.Severity);
    }

    [Fact]
    public void DebtMinimumsAboveTwentyFivePercentOfIncome_DangerSeverity()
    {
        // Minimums = $800, income = $2000 → 40% → Danger
        var result = Compute(
            debts: [Debt("Card A", 5000m, 0.20m, 400m), Debt("Card B", 3000m, 0.18m, 400m)],
            income: [Income(2000m)]);

        var ratioInsight = result.Debt.First(i => i.Message.Contains("minimums"));
        Assert.Equal(InsightSeverity.Danger, ratioInsight.Severity);
    }

    [Fact]
    public void DebtMinimumsFilfteenToTwentyFivePercentOfIncome_WarningSeverity()
    {
        // Minimums = $400, income = $2000 → 20% → Warning
        var result = Compute(
            debts: [Debt("Card", 5000m, 0.20m, 400m)],
            income: [Income(2000m)]);

        var ratioInsight = result.Debt.First(i => i.Message.Contains("minimums"));
        Assert.Equal(InsightSeverity.Warning, ratioInsight.Severity);
    }

    // ── Budget Insights ────────────────────────────────────────────────────────

    [Fact]
    public void OverdueBills_DangerInsightWithCorrectCountAndTotal()
    {
        var result = Compute(expenses:
        [
            Bill(200m, Today.AddDays(-3)),  // overdue
            Bill(150m, Today.AddDays(-1)),  // overdue
            Bill(300m, Today.AddDays(5)),   // upcoming
        ]);

        var overdueInsight = result.Budget.First(i => i.Message.Contains("overdue"));
        Assert.Equal(InsightSeverity.Danger, overdueInsight.Severity);
        Assert.Contains("2 bill", overdueInsight.Message);
        Assert.Contains("350", overdueInsight.Message); // $200 + $150
    }

    [Fact]
    public void UnpaidMustPayBills_WarningInsight()
    {
        var result = Compute(expenses:
        [
            Bill(500m, Today.AddDays(5), priority: ExpensePriority.MustPay),
            Bill(200m, Today.AddDays(10), priority: ExpensePriority.MustPay),
        ]);

        var mustPayInsight = result.Budget.FirstOrDefault(i => i.Message.Contains("must-pay"));
        Assert.NotNull(mustPayInsight);
        Assert.Equal(InsightSeverity.Warning, mustPayInsight.Severity);
        Assert.Contains("700", mustPayInsight.Message);
    }

    [Fact]
    public void AllBillsPaid_InfoInsight()
    {
        var result = Compute(expenses:
        [
            Bill(300m, Today.AddDays(-5), paid: true),
            Bill(150m, Today.AddDays(-2), paid: true),
        ]);

        var paidInsight = result.Budget.First(i => i.Message.Contains("paid"));
        Assert.Equal(InsightSeverity.Info, paidInsight.Severity);
    }

    // ── Cash Flow Insights ────────────────────────────────────────────────────

    [Fact]
    public void NoAccounts_ReturnsAddAccountsInfo()
    {
        var result = Compute();

        var cashInsight = Assert.Single(result.CashFlow);
        Assert.Equal(InsightSeverity.Info, cashInsight.Severity);
        Assert.Contains("Add your bank accounts", cashInsight.Message);
    }

    [Fact]
    public void CashLessThanUnpaidBills_DangerInsightWithShortfall()
    {
        // Cash = $300, unpaid bills = $800 → shortfall $500
        var result = Compute(
            expenses: [Bill(800m, Today.AddDays(5))],
            accounts: [Account(300m)]);

        var cashInsight = result.CashFlow.First(i => i.Severity == InsightSeverity.Danger);
        Assert.Contains("short", cashInsight.Message);
        Assert.Contains("500", cashInsight.Message);
    }

    [Fact]
    public void CashBelowFiveHundred_WarningInsight()
    {
        // Cash = $350, no unpaid bills → low cash warning
        var result = Compute(accounts: [Account(350m)]);

        var cashInsight = result.CashFlow.First();
        Assert.Equal(InsightSeverity.Warning, cashInsight.Severity);
        Assert.Contains("low", cashInsight.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HealthyCashBuffer_InfoInsightWithMonthsCovered()
    {
        // Cash = $6000, total monthly bills = $2000 → 3 months covered
        var result = Compute(
            expenses: [Bill(2000m, Today.AddDays(5))],
            accounts: [Account(6000m)]);

        var cashInsight = Assert.Single(result.CashFlow);
        Assert.Equal(InsightSeverity.Info, cashInsight.Severity);
        Assert.Contains("3.0", cashInsight.Message);
    }

    [Fact]
    public void CreditCardAccountsExcludedFromCash()
    {
        // Checking $200 (counts), CreditCard $5000 (excluded from cash)
        // Unpaid bill $300 → cash ($200) < bills ($300) → Danger
        var result = Compute(
            expenses: [Bill(300m, Today.AddDays(3))],
            accounts:
            [
                Account(200m, AccountType.Checking),
                Account(5000m, AccountType.CreditCard),
            ]);

        var cashInsight = result.CashFlow.First(i => i.Severity == InsightSeverity.Danger);
        Assert.Contains("short", cashInsight.Message);
    }
}
