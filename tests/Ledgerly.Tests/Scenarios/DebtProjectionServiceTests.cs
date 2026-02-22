using Ledgerly.Application.Scenarios;
using Ledgerly.Domain.Debts;
using Ledgerly.Domain.Scenarios;

namespace Ledgerly.Tests.Scenarios;

public class DebtProjectionServiceTests
{
    private static readonly DebtProjectionService Svc = new();

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
            CreatedUtc = DateTime.UtcNow
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
                CreatedUtc = DateTime.UtcNow
            });
        }

        return scenario;
    }

    [Fact]
    public void NoDebts_ReturnsZeroMonths()
    {
        var scenario = MakeScenario(PayoffStrategy.Avalanche, 0);
        var result = Svc.Project(scenario);

        Assert.Equal(0, result.TotalMonths);
        Assert.Equal(0m, result.TotalInterestPaid);
        Assert.Equal(0m, result.TotalPaid);
        Assert.Empty(result.Months);
    }

    [Fact]
    public void SingleDebt_PaysOffCorrectly()
    {
        // $1,000 at 12% annually (1%/mo), $100/mo minimum
        // Known: ~11 months to pay off
        var scenario = MakeScenario(PayoffStrategy.Avalanche, 0,
            ("Test Debt", 1000m, 0.12m, 100m));

        var result = Svc.Project(scenario);

        Assert.True(result.TotalMonths > 0 && result.TotalMonths <= 12);
        Assert.True(result.TotalInterestPaid > 0m);

        // Final month should have zero balance for all debts
        var lastMonth = result.Months.Last();
        Assert.All(lastMonth.Debts, d => Assert.Equal(0m, d.RemainingBalance));
    }

    [Fact]
    public void Snowball_PaysLowestBalanceFirst()
    {
        // Debt A: $500, 5% — Debt B: $2000, 20%
        // Snowball should pay off A first despite A having lower rate
        var scenario = MakeScenario(PayoffStrategy.Snowball, 200m,
            ("Low Balance", 500m, 0.05m, 50m),
            ("High Balance", 2000m, 0.20m, 100m));

        var result = Svc.Project(scenario);

        // Find the month when each debt reaches zero
        int lowBalancePaidOffMonth = FindPayoffMonth(result, "Low Balance");
        int highBalancePaidOffMonth = FindPayoffMonth(result, "High Balance");

        Assert.True(lowBalancePaidOffMonth < highBalancePaidOffMonth,
            "Snowball should pay off the lower-balance debt first.");
    }

    [Fact]
    public void Avalanche_PaysHighestRateFirst()
    {
        // Debt A: $500, 5% — Debt B: $500, 20% (same balance, different rates)
        // Avalanche should pay off B first (highest rate)
        var scenario = MakeScenario(PayoffStrategy.Avalanche, 200m,
            ("Low Rate", 500m, 0.05m, 50m),
            ("High Rate", 500m, 0.20m, 50m));

        var result = Svc.Project(scenario);

        int lowRatePaidOffMonth = FindPayoffMonth(result, "Low Rate");
        int highRatePaidOffMonth = FindPayoffMonth(result, "High Rate");

        Assert.True(highRatePaidOffMonth < lowRatePaidOffMonth,
            "Avalanche should pay off the higher-rate debt first.");
    }

    [Fact]
    public void ExtraPayment_AcceleratesPayoff()
    {
        var debtArgs = new[] { ("Debt", 5000m, 0.18m, 100m) };

        var withoutExtra = MakeScenario(PayoffStrategy.Avalanche, 0, debtArgs[0]);
        var withExtra = MakeScenario(PayoffStrategy.Avalanche, 200m, debtArgs[0]);

        var resultWithout = Svc.Project(withoutExtra);
        var resultWith = Svc.Project(withExtra);

        Assert.True(resultWith.TotalMonths < resultWithout.TotalMonths,
            "Extra payment should reduce the number of months to pay off.");
        Assert.True(resultWith.TotalInterestPaid < resultWithout.TotalInterestPaid,
            "Extra payment should reduce total interest paid.");
    }

    [Fact]
    public void AllDebts_ReachZeroBalance()
    {
        var scenario = MakeScenario(PayoffStrategy.Avalanche, 300m,
            ("Credit Card A", 3000m, 0.22m, 80m),
            ("Car Loan", 8000m, 0.07m, 200m),
            ("Personal Loan", 2000m, 0.15m, 75m));

        var result = Svc.Project(scenario);

        Assert.True(result.TotalMonths > 0);

        var lastMonth = result.Months.Last();
        Assert.All(lastMonth.Debts, d =>
            Assert.Equal(0m, d.RemainingBalance));
    }

    [Fact]
    public void Snowball_VsAvalanche_SameDebts_AvalancheLessInterest()
    {
        // Avalanche always pays less total interest than snowball (when rates differ)
        var debts = new[]
        {
            ("Card A", 3000m, 0.22m, 75m),
            ("Card B", 1000m, 0.10m, 30m)
        };

        var snowball = MakeScenario(PayoffStrategy.Snowball, 150m, debts[0], debts[1]);
        var avalanche = MakeScenario(PayoffStrategy.Avalanche, 150m, debts[0], debts[1]);

        var snowballResult = Svc.Project(snowball);
        var avalancheResult = Svc.Project(avalanche);

        Assert.True(avalancheResult.TotalInterestPaid <= snowballResult.TotalInterestPaid,
            "Avalanche strategy should pay equal or less interest than Snowball.");
    }

    private static int FindPayoffMonth(Ledgerly.Contracts.Scenarios.ProjectionResultDto result, string debtName)
    {
        foreach (var month in result.Months)
        {
            var debt = month.Debts.FirstOrDefault(d => d.Name == debtName);
            if (debt?.RemainingBalance == 0m)
                return month.Month;
        }
        return int.MaxValue;
    }
}
