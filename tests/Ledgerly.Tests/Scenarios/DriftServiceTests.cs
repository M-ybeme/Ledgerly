using Ledgerly.Application.Scenarios;
using Ledgerly.Domain.Debts;
using Ledgerly.Domain.Scenarios;

namespace Ledgerly.Tests.Scenarios;

public class DriftServiceTests
{
    private static readonly DebtProjectionService ProjectionSvc = new();
    private static readonly DriftService Svc = new(ProjectionSvc);

    private static readonly DateTime FixedCreated = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly ScenarioStart = new(2026, 1, 1);

    private static Scenario MakeScenario(
        PayoffStrategy strategy,
        decimal extraPayment,
        params (string name, decimal balance, decimal rate, decimal minPayment)[] debts)
    {
        var scenario = new Scenario
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Strategy = strategy,
            ExtraMonthlyPayment = extraPayment,
            CreatedUtc = FixedCreated
        };

        foreach (var (name, balance, rate, minPayment) in debts)
        {
            scenario.DebtAccounts.Add(new DebtAccount
            {
                Id = Guid.NewGuid(),
                Name = name,
                Balance = balance,
                AnnualInterestRate = rate,
                MinimumPayment = minPayment,
                CreatedUtc = FixedCreated
            });
        }

        return scenario;
    }

    private static ActualPayment MakePayment(Guid scenarioId, Guid debtId, DateOnly date, decimal amount) =>
        new()
        {
            Id = Guid.NewGuid(),
            ScenarioId = scenarioId,
            DebtAccountId = debtId,
            PaymentDate = date,
            Amount = amount,
            CreatedUtc = DateTime.UtcNow
        };

    [Fact]
    public void NoMonthsElapsed_ReturnsNoDrift()
    {
        // Scenario created today — no months have elapsed
        var scenario = MakeScenario(PayoffStrategy.Avalanche, 0,
            ("Test Debt", 1000m, 0m, 100m));

        var today = ScenarioStart; // same month as creation
        var result = Svc.Compute(scenario, [], today);

        Assert.Equal(0, result.CurrentMonth);
        Assert.Equal(0, result.MonthsDrift);
        Assert.Equal(result.OriginalTotalMonths, result.UpdatedTotalMonths);
    }

    [Fact]
    public void ExactlyOnPlan_ZeroDrift()
    {
        // $1,000 at 0% interest, $100/mo minimum — 10 months to pay off
        var scenario = MakeScenario(PayoffStrategy.Avalanche, 0,
            ("Simple Debt", 1000m, 0m, 100m));

        var debt = scenario.DebtAccounts.First();

        // 1 month elapsed; logged exactly the minimum ($100)
        // Month 1 simulation: 0 interest, pay $100 → balance = $900
        // Projection month 1 balance = $900 → drift = 0
        var today = ScenarioStart.AddMonths(1);
        var payments = new List<ActualPayment>
        {
            MakePayment(scenario.Id, debt.Id, new DateOnly(2026, 1, 15), 100m)
        };

        var result = Svc.Compute(scenario, payments, today);

        Assert.Equal(1, result.CurrentMonth);
        Assert.Equal(0, result.MonthsDrift);
        Assert.Single(result.Debts);
        Assert.True(result.Debts[0].IsOnTrack);
        Assert.Equal(0m, result.Debts[0].BalanceDrift);
    }

    [Fact]
    public void PaidMore_AheadOfSchedule()
    {
        // $1,000 at 0% interest, $100/mo minimum
        // After 1 month, paid $300 (3× the minimum)
        // Actual balance: $700 vs projected $900 → BalanceDrift = +$200 (ahead)
        var scenario = MakeScenario(PayoffStrategy.Avalanche, 0,
            ("Simple Debt", 1000m, 0m, 100m));

        var debt = scenario.DebtAccounts.First();
        var today = ScenarioStart.AddMonths(1);
        var payments = new List<ActualPayment>
        {
            MakePayment(scenario.Id, debt.Id, new DateOnly(2026, 1, 15), 300m)
        };

        var result = Svc.Compute(scenario, payments, today);

        Assert.Equal(1, result.CurrentMonth);
        Assert.True(result.MonthsDrift > 0, "Should be ahead of schedule.");
        Assert.Single(result.Debts);
        Assert.True(result.Debts[0].IsOnTrack);
        Assert.True(result.Debts[0].BalanceDrift > 0, "Actual balance should be lower than projected.");
        Assert.True(result.UpdatedTotalMonths < result.OriginalTotalMonths);
    }

    [Fact]
    public void PaidLess_BehindSchedule()
    {
        // $1,000 at 0% interest, $100/mo minimum
        // After 1 month, only paid $50 (less than minimum)
        // Actual balance: $950 vs projected $900 → BalanceDrift = -$50 (behind)
        var scenario = MakeScenario(PayoffStrategy.Avalanche, 0,
            ("Simple Debt", 1000m, 0m, 100m));

        var debt = scenario.DebtAccounts.First();
        var today = ScenarioStart.AddMonths(1);
        var payments = new List<ActualPayment>
        {
            MakePayment(scenario.Id, debt.Id, new DateOnly(2026, 1, 15), 50m)
        };

        var result = Svc.Compute(scenario, payments, today);

        Assert.Equal(1, result.CurrentMonth);
        Assert.True(result.MonthsDrift < 0, "Should be behind schedule.");
        Assert.Single(result.Debts);
        Assert.False(result.Debts[0].IsOnTrack);
        Assert.True(result.Debts[0].BalanceDrift < 0, "Actual balance should be higher than projected.");
        Assert.True(result.UpdatedTotalMonths > result.OriginalTotalMonths);
    }

    [Fact]
    public void LargeOverpayment_FullyPaysDebtEarly()
    {
        // $1000 at 0%, $100/mo min — 10 months original plan
        // After 3 months, paid $1000 all in month 1
        // Actual balance after 3 months = $0
        // Updated projection: 3 + 0 = 3 total months vs original 10 → MonthsDrift = 7
        var scenario = MakeScenario(PayoffStrategy.Avalanche, 0,
            ("Medium Debt", 1000m, 0m, 100m));

        var debt = scenario.DebtAccounts.First();
        var today = ScenarioStart.AddMonths(3);
        var payments = new List<ActualPayment>
        {
            MakePayment(scenario.Id, debt.Id, new DateOnly(2026, 1, 10), 1000m) // pay it all off in month 1
        };

        var result = Svc.Compute(scenario, payments, today);

        Assert.Single(result.Debts);
        Assert.Equal(0m, result.Debts[0].ActualBalance);
        Assert.Equal(10, result.OriginalTotalMonths);
        Assert.Equal(3, result.UpdatedTotalMonths);
        Assert.Equal(7, result.MonthsDrift);
    }

    [Fact]
    public void MultipleDebts_MixedDrift()
    {
        // Debt A: $1000 at 0%, $100 min
        // Debt B: $500 at 0%, $100 min
        // 1 month elapsed
        // Debt A: paid $200 (ahead — actual $800 vs projected $900)
        // Debt B: paid $50 (behind — actual $450 vs projected $400)
        var scenario = MakeScenario(PayoffStrategy.Avalanche, 0,
            ("Debt A", 1000m, 0m, 100m),
            ("Debt B", 500m, 0m, 100m));

        var debtA = scenario.DebtAccounts.First(d => d.Name == "Debt A");
        var debtB = scenario.DebtAccounts.First(d => d.Name == "Debt B");
        var today = ScenarioStart.AddMonths(1);

        var payments = new List<ActualPayment>
        {
            MakePayment(scenario.Id, debtA.Id, new DateOnly(2026, 1, 15), 200m),
            MakePayment(scenario.Id, debtB.Id, new DateOnly(2026, 1, 15), 50m)
        };

        var result = Svc.Compute(scenario, payments, today);

        Assert.Equal(2, result.Debts.Count);

        var driftA = result.Debts.First(d => d.Name == "Debt A");
        var driftB = result.Debts.First(d => d.Name == "Debt B");

        Assert.True(driftA.IsOnTrack, "Debt A should be on track (paid more).");
        Assert.True(driftA.BalanceDrift > 0, "Debt A actual balance should be lower than projected.");

        Assert.False(driftB.IsOnTrack, "Debt B should be behind (paid less).");
        Assert.True(driftB.BalanceDrift < 0, "Debt B actual balance should be higher than projected.");
    }
}
