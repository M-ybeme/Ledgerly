using Ledgerly.Application.Goals;
using Ledgerly.Contracts.Accounts;
using Ledgerly.Contracts.Budget;
using Ledgerly.Contracts.Debts;
using Ledgerly.Contracts.Goals;
using Ledgerly.Contracts.Income;
using Ledgerly.Domain.Accounts;
using Ledgerly.Domain.Budget;
using Ledgerly.Domain.Income;

namespace Ledgerly.Tests.Goals;

public class GoalPlannerServiceTests
{
    private static readonly GoalPlannerService Svc = new();
    private static readonly DateOnly Today = new(2026, 3, 22);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static DebtAccountDto Debt(decimal balance, decimal aprDecimal, decimal min) =>
        new(Guid.NewGuid(), "Card", balance, aprDecimal, min, DateTime.UtcNow);

    private static IncomeSourceDto Income(decimal monthly) =>
        new(Guid.NewGuid(), "Salary", monthly, PayFrequency.Monthly, monthly, null, null, DateTime.UtcNow);

    private static PlannedExpenseDto Expense(decimal amount) =>
        new(Guid.NewGuid(), "Bill", amount, Today, null, null, false, false, null, null, ExpensePriority.MustPay);

    private static GoalPlanResultDto ComputeDebtFree(
        DateOnly targetDate,
        List<DebtAccountDto>? debts = null,
        List<IncomeSourceDto>? income = null,
        List<PlannedExpenseDto>? expenses = null) =>
        Svc.Compute(
            new GoalPlanRequest(GoalType.DebtFree, targetDate, null, null, null),
            debts ?? [],
            income ?? [],
            expenses ?? [],
            Today);

    private static GoalPlanResultDto ComputeSave(decimal target, int months,
        List<IncomeSourceDto>? income = null, List<PlannedExpenseDto>? expenses = null,
        List<DebtAccountDto>? debts = null) =>
        Svc.Compute(
            new GoalPlanRequest(GoalType.SaveAmount, null, target, months, null),
            debts ?? [],
            income ?? [],
            expenses ?? [],
            Today);

    private static GoalPlanResultDto ComputeCap(decimal cap,
        List<PlannedExpenseDto>? expenses = null) =>
        Svc.Compute(
            new GoalPlanRequest(GoalType.SpendingCap, null, null, null, cap),
            [],
            [],
            expenses ?? [],
            Today);

    // ── Debt-Free ────────────────────────────────────────────────────────────

    [Fact]
    public void DebtFree_NoDebt_OnTrackImmediately()
    {
        var result = ComputeDebtFree(Today.AddYears(3));

        Assert.Equal(GoalFeasibility.OnTrack, result.Feasibility);
        Assert.Equal(0m, result.RequiredMonthly);
    }

    [Fact]
    public void DebtFree_TargetDateInPast_NotFeasible()
    {
        var result = ComputeDebtFree(
            Today.AddMonths(-1),
            debts: [Debt(5000m, 0.18m, 100m)]);

        Assert.Equal(GoalFeasibility.NotFeasible, result.Feasibility);
    }

    [Fact]
    public void DebtFree_MinimumsAloneSufficient_OnTrack()
    {
        // $1000 debt at 0% interest, 36-month target → PMT = $27.78
        // Minimum = $200, which exceeds PMT → OnTrack
        var result = ComputeDebtFree(
            Today.AddMonths(36),
            debts: [Debt(1000m, 0m, 200m)]);

        Assert.Equal(GoalFeasibility.OnTrack, result.Feasibility);
        // Required PMT (27.78) is less than minimum (200), so no extra needed
        Assert.True(result.RequiredMonthly <= 200m);
    }

    [Fact]
    public void DebtFree_AffordableExtra_OnTrack()
    {
        // $5000 debt, 0% interest, 24 months → needs ~$209/month
        // Income $4000, expenses $1000 → $3000 surplus → capacity covers extra easily
        var result = ComputeDebtFree(
            Today.AddMonths(24),
            debts: [Debt(5000m, 0m, 100m)],
            income: [Income(4000m)],
            expenses: [Expense(1000m)]);

        Assert.Equal(GoalFeasibility.OnTrack, result.Feasibility);
        Assert.Equal(0m, result.Shortfall);
    }

    [Fact]
    public void DebtFree_SmallShortfall_AtRisk()
    {
        // $15,000 at 24% APR (2%/mo), 12-month target
        // PMT ≈ $1,418  |  minimums = $300  |  required extra ≈ $1,118
        // income $2,500 - expenses $1,200 - minimums $300 = $1,000 available for extra
        // shortfall ≈ $118  →  $118/$1418 ≈ 8%  <  25% threshold  →  AtRisk
        var result = ComputeDebtFree(
            Today.AddMonths(12),
            debts: [Debt(15000m, 0.24m, 300m)],
            income: [Income(2500m)],
            expenses: [Expense(1200m)]);

        Assert.Equal(GoalFeasibility.AtRisk, result.Feasibility);
        Assert.True(result.Shortfall > 0);
    }

    [Fact]
    public void DebtFree_LargeShortfall_NotFeasible()
    {
        // $50,000 debt at 20% APR, 6-month target → enormous PMT, low income
        var result = ComputeDebtFree(
            Today.AddMonths(6),
            debts: [Debt(50000m, 0.20m, 500m)],
            income: [Income(3000m)],
            expenses: [Expense(2000m)]);

        Assert.Equal(GoalFeasibility.NotFeasible, result.Feasibility);
        Assert.True(result.Shortfall > 0);
    }

    [Fact]
    public void DebtFree_ZeroInterestDebt_PmtEqualsBalanceDividedByMonths()
    {
        // $1200 at 0% over 12 months → PMT exactly $100
        var result = ComputeDebtFree(
            Today.AddMonths(12),
            debts: [Debt(1200m, 0m, 10m)],
            income: [Income(5000m)]);

        Assert.NotNull(result.RequiredMonthly);
        Assert.Equal(100m, result.RequiredMonthly!.Value, precision: 0);
    }

    // ── Save Amount ──────────────────────────────────────────────────────────

    [Fact]
    public void SaveAmount_SurplusCoversRequired_OnTrack()
    {
        // Need $500/month, surplus = $3000 income - $1000 expenses - $0 minimums = $2000
        var result = ComputeSave(6000m, 12, income: [Income(3000m)], expenses: [Expense(1000m)]);

        Assert.Equal(GoalFeasibility.OnTrack, result.Feasibility);
        Assert.Equal(0m, result.Shortfall);
    }

    [Fact]
    public void SaveAmount_SmallShortfall_AtRisk()
    {
        // Need $1000/month (12000 in 12 months), surplus = $1150
        // shortfall = 0 → actually OnTrack... let me use $1200/month required
        // $14400 in 12 months, surplus = $1150 → shortfall = $50 → 50/1200 ≈ 4% < 30% → AtRisk
        var result = ComputeSave(14_400m, 12, income: [Income(2500m)], expenses: [Expense(1350m)]);

        // shortfall should be non-zero but < 30% of required
        Assert.True(result.Shortfall > 0);
        Assert.NotEqual(GoalFeasibility.NotFeasible, result.Feasibility);
    }

    [Fact]
    public void SaveAmount_LargeShortfall_NotFeasible()
    {
        // Need $2000/month, surplus = $500 → shortfall $1500 > 30% of $2000
        var result = ComputeSave(24_000m, 12, income: [Income(2000m)], expenses: [Expense(1500m)]);

        Assert.Equal(GoalFeasibility.NotFeasible, result.Feasibility);
        Assert.True(result.Shortfall >= 1500m);
    }

    // ── Spending Cap ─────────────────────────────────────────────────────────

    [Fact]
    public void SpendingCap_UnderCap_OnTrack()
    {
        // Expenses $1200, cap $1500 → $300 surplus
        var result = ComputeCap(1500m, expenses: [Expense(800m), Expense(400m)]);

        Assert.Equal(GoalFeasibility.OnTrack, result.Feasibility);
        Assert.Equal(0m, result.Shortfall);
        Assert.Contains("surplus", result.Summary);
    }

    [Fact]
    public void SpendingCap_OverageWithinTenPercent_AtRisk()
    {
        // Cap $1000, spending $1080 → overage $80 = 8% of cap → AtRisk
        var result = ComputeCap(1000m, expenses: [Expense(1080m)]);

        Assert.Equal(GoalFeasibility.AtRisk, result.Feasibility);
        Assert.Equal(80m, result.Shortfall);
    }

    [Fact]
    public void SpendingCap_OverageAboveTenPercent_NotFeasible()
    {
        // Cap $1000, spending $1200 → overage $200 = 20% → NotFeasible
        var result = ComputeCap(1000m, expenses: [Expense(1200m)]);

        Assert.Equal(GoalFeasibility.NotFeasible, result.Feasibility);
        Assert.Equal(200m, result.Shortfall);
    }
}
