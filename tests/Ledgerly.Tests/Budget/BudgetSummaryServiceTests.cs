using Ledgerly.Application.Budget;
using Ledgerly.Domain.Budget;

namespace Ledgerly.Tests.Budget;

public class BudgetSummaryServiceTests
{
    private static readonly BudgetSummaryService Svc = new();

    private static BudgetCategory MakeCategory(string name, CategoryType type) =>
        new() { Id = Guid.NewGuid(), Name = name, Type = type, CreatedUtc = DateTime.UtcNow };

    private static BudgetPlan MakePlan(DateOnly start, DateOnly end, params (BudgetCategory cat, decimal amount)[] lines)
    {
        var plan = new BudgetPlan
        {
            Id = Guid.NewGuid(),
            Name = "Test Plan",
            StartDate = start,
            EndDate = end,
            CreatedUtc = DateTime.UtcNow
        };
        foreach (var (cat, amount) in lines)
        {
            plan.Lines.Add(new BudgetPlanLine
            {
                Id = Guid.NewGuid(),
                CategoryId = cat.Id,
                Category = cat,
                PlannedAmount = amount
            });
        }
        return plan;
    }

    private static Transaction MakeTx(BudgetCategory cat, decimal amount, DateOnly date) =>
        new()
        {
            Id = Guid.NewGuid(),
            Description = "Test",
            Amount = amount,
            Date = date,
            CategoryId = cat.Id,
            Category = cat,
            CreatedUtc = DateTime.UtcNow
        };

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public void NoTransactions_AllCategoriesShowZeroActual()
    {
        var groceries = MakeCategory("Groceries", CategoryType.Expense);
        var salary = MakeCategory("Salary", CategoryType.Income);
        var plan = MakePlan(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31),
            (groceries, 500m), (salary, 5000m));

        var result = Svc.Compute(plan, []);

        Assert.Equal(2, result.Categories.Count);
        Assert.All(result.Categories, c => Assert.Equal(0m, c.Actual));
        Assert.Equal(0m, result.TotalActualIncome);
        Assert.Equal(0m, result.TotalActualExpenses);
    }

    [Fact]
    public void TransactionsInRange_SummedPerCategory()
    {
        var groceries = MakeCategory("Groceries", CategoryType.Expense);
        var plan = MakePlan(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), (groceries, 500m));

        var txs = new List<Transaction>
        {
            MakeTx(groceries, 120m, new DateOnly(2026, 3, 5)),
            MakeTx(groceries, 95m,  new DateOnly(2026, 3, 15)),
            MakeTx(groceries, 80m,  new DateOnly(2026, 3, 28))
        };

        var result = Svc.Compute(plan, txs);

        var cat = result.Categories.Single();
        Assert.Equal(295m, cat.Actual);
        Assert.Equal(500m, cat.Planned);
        Assert.Equal(205m, cat.Variance); // under budget
    }

    [Fact]
    public void OverBudget_NegativeVariance()
    {
        var dining = MakeCategory("Dining", CategoryType.Expense);
        var plan = MakePlan(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), (dining, 200m));

        var txs = new List<Transaction>
        {
            MakeTx(dining, 150m, new DateOnly(2026, 3, 10)),
            MakeTx(dining, 100m, new DateOnly(2026, 3, 20))
        };

        var result = Svc.Compute(plan, txs);

        var cat = result.Categories.Single();
        Assert.Equal(250m, cat.Actual);
        Assert.Equal(-50m, cat.Variance); // over budget
    }

    [Fact]
    public void IncomeAndExpense_TotalsCorrect()
    {
        var salary = MakeCategory("Salary", CategoryType.Income);
        var rent = MakeCategory("Rent", CategoryType.Expense);
        var plan = MakePlan(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31),
            (salary, 5000m), (rent, 1500m));

        var txs = new List<Transaction>
        {
            MakeTx(salary, 5200m, new DateOnly(2026, 3, 1)),
            MakeTx(rent,   1500m, new DateOnly(2026, 3, 3))
        };

        var result = Svc.Compute(plan, txs);

        Assert.Equal(5000m, result.TotalPlannedIncome);
        Assert.Equal(5200m, result.TotalActualIncome);
        Assert.Equal(1500m, result.TotalPlannedExpenses);
        Assert.Equal(1500m, result.TotalActualExpenses);
        Assert.Equal(3500m, result.NetPlanned);   // 5000 - 1500
        Assert.Equal(3700m, result.NetActual);    // 5200 - 1500
    }

    [Fact]
    public void UnplannedCategory_AppearsWithZeroPlanned()
    {
        var rent = MakeCategory("Rent", CategoryType.Expense);
        var unplanned = MakeCategory("Surprise", CategoryType.Expense);
        var plan = MakePlan(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), (rent, 1500m));

        var txs = new List<Transaction>
        {
            MakeTx(rent,      1500m, new DateOnly(2026, 3, 3)),
            MakeTx(unplanned,  300m, new DateOnly(2026, 3, 20))
        };

        var result = Svc.Compute(plan, txs);

        Assert.Equal(2, result.Categories.Count);
        var surpriseCat = result.Categories.Single(c => c.CategoryName == "Surprise");
        Assert.Equal(0m, surpriseCat.Planned);
        Assert.Equal(300m, surpriseCat.Actual);
        Assert.Equal(-300m, surpriseCat.Variance);
    }

    [Fact]
    public void EmptyPlan_EmptyTransactions_ReturnsZeroTotals()
    {
        var plan = MakePlan(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        var result = Svc.Compute(plan, []);

        Assert.Empty(result.Categories);
        Assert.Equal(0m, result.NetPlanned);
        Assert.Equal(0m, result.NetActual);
    }
}
