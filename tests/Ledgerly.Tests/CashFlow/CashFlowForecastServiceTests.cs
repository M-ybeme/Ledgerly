using Ledgerly.Application.Budget;
using Ledgerly.Application.CashFlow;
using Ledgerly.Application.Income;
using Ledgerly.Domain.Budget;
using Ledgerly.Domain.Income;

namespace Ledgerly.Tests.CashFlow;

// ── Lightweight in-memory stubs (no mocking framework needed) ─────────────

file sealed class StubIncomeRepo(List<IncomeSource> sources) : IIncomeSourceRepository
{
    public Task<List<IncomeSource>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult(sources);
    public Task<IncomeSource?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(sources.FirstOrDefault(s => s.Id == id));
    public Task AddAsync(IncomeSource source, CancellationToken ct = default) => Task.CompletedTask;
    public Task DeleteAsync(IncomeSource source, CancellationToken ct = default) => Task.CompletedTask;
    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

file sealed class StubExpenseRepo(List<PlannedExpense> expenses) : IPlannedExpenseRepository
{
    public Task<List<PlannedExpense>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult(expenses);
    public Task<PlannedExpense?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(expenses.FirstOrDefault(e => e.Id == id));
    public Task AddAsync(PlannedExpense expense, CancellationToken ct = default) => Task.CompletedTask;
    public Task DeleteAsync(PlannedExpense expense, CancellationToken ct = default) => Task.CompletedTask;
    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

// ── Tests ─────────────────────────────────────────────────────────────────

public class CashFlowForecastServiceTests
{
    // Today as the service sees it (DateOnly.FromDateTime(DateTime.Today))
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    private static CashFlowForecastService Build(
        List<IncomeSource>? income = null,
        List<PlannedExpense>? expenses = null) =>
        new(new StubIncomeRepo(income ?? []), new StubExpenseRepo(expenses ?? []));

    private static PlannedExpense Expense(string desc, decimal amount, DateOnly due) =>
        new()
        {
            Id = Guid.NewGuid(), UserId = Guid.NewGuid(),
            Description = desc, PlannedAmount = amount,
            DueDate = due, IsRecurring = false,
            Priority = Domain.Budget.ExpensePriority.MustPay,
            // PaidDate = null → IsPaid = false (computed property)
        };

    // ── Snapshot count ────────────────────────────────────────────────────

    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(90)]
    public async Task Forecast_ReturnsExactDayCount(int days)
    {
        var svc = Build();
        var result = await svc.ComputeAsync(1000m, days);

        Assert.Equal(days, result.DailySnapshots.Count);
    }

    // ── Balance propagation ───────────────────────────────────────────────

    [Fact]
    public async Task StartingBalance_PropagatesAsDay0Opening()
    {
        var svc = Build();
        var result = await svc.ComputeAsync(2500m, 30);

        Assert.Equal(2500m, result.DailySnapshots[0].OpeningBalance);
    }

    [Fact]
    public async Task NoEventsNoBurn_BalanceUnchangedThroughout()
    {
        var svc = Build();
        var result = await svc.ComputeAsync(1000m, 30);

        Assert.All(result.DailySnapshots, d =>
        {
            Assert.Equal(1000m, d.OpeningBalance);
            Assert.Equal(1000m, d.ClosingBalance);
        });
    }

    // ── Expense deduction ─────────────────────────────────────────────────

    [Fact]
    public async Task ExpenseOnKnownDate_DeductsFromClosingBalance()
    {
        var expenseDate = Today.AddDays(5);
        var svc = Build(expenses: [Expense("Rent", 400m, expenseDate)]);

        var result = await svc.ComputeAsync(1000m, 30);

        var day5 = result.DailySnapshots[5];
        Assert.Equal(400m, day5.ExpenseTotal);
        Assert.Equal(600m, day5.ClosingBalance);
    }

    // ── Daily burn rate ───────────────────────────────────────────────────

    [Fact]
    public async Task DailyBurnRate_CompoundsEachDay()
    {
        var svc = Build();
        var result = await svc.ComputeAsync(1000m, 30, dailyBurnRate: 10m);

        // Each day reduces by $10; after 30 days = $700
        Assert.Equal(990m, result.DailySnapshots[0].ClosingBalance);
        Assert.Equal(980m, result.DailySnapshots[1].ClosingBalance);
        Assert.Equal(700m, result.DailySnapshots[29].ClosingBalance);
    }

    // ── DaysUntilNegative ─────────────────────────────────────────────────

    [Fact]
    public async Task DaysUntilNegative_NullWhenNeverNegative()
    {
        var svc = Build();
        var result = await svc.ComputeAsync(100_000m, 30);

        Assert.Null(result.DaysUntilNegative);
    }

    [Fact]
    public async Task DaysUntilNegative_CorrectWhenBalanceCrossesZero()
    {
        // $50 starting, $10/day burn → goes negative on day 6 (after day 5 closes at $0, day 6 closes at -$10)
        var svc = Build();
        var result = await svc.ComputeAsync(50m, 30, dailyBurnRate: 10m);

        // d=0→$40, d=1→$30, d=2→$20, d=3→$10, d=4→$0, d=5→-$10 → DaysUntilNegative = 6
        Assert.Equal(6, result.DaysUntilNegative);
    }

    // ── LowestBalance ─────────────────────────────────────────────────────

    [Fact]
    public async Task LowestBalance_IdentifiesCorrectDayAndAmount()
    {
        // Two expenses: $200 on day 3 and $100 on day 7
        // Starting $500, no burn → balances: ...$300 on day 3 ...$200 on day 7 (lowest)
        var svc = Build(expenses:
        [
            Expense("A", 200m, Today.AddDays(3)),
            Expense("B", 100m, Today.AddDays(7)),
        ]);

        var result = await svc.ComputeAsync(500m, 30);

        Assert.Equal(200m, result.LowestBalance);
        Assert.Equal(Today.AddDays(7), result.LowestBalanceDate);
    }

    // ── Warnings ─────────────────────────────────────────────────────────

    [Fact]
    public async Task NegativeBalance_WarningGenerated()
    {
        var svc = Build();
        var result = await svc.ComputeAsync(50m, 30, dailyBurnRate: 10m);

        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("negative", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task LowBalanceWithoutGoingNegative_LowBalanceWarningGenerated()
    {
        // $150 starting, $5/day burn → hits $95 at day 11 (< $100 threshold), but $150 - $150 = $0 at day 30 (never < 0)
        var svc = Build();
        var result = await svc.ComputeAsync(150m, 30, dailyBurnRate: 5m);

        Assert.Null(result.DaysUntilNegative);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("Low balance", StringComparison.OrdinalIgnoreCase));
    }
}
