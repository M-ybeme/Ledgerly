using Ledgerly.Application.Credit;
using Ledgerly.Application.Scenarios;
using Ledgerly.Contracts.Scenarios;
using Ledgerly.Domain.Credit;
using Ledgerly.Domain.Debts;
using Ledgerly.Domain.Scenarios;

namespace Ledgerly.Tests.Credit;

public class CreditScoreServiceTests
{
    private static readonly CreditScoreService Svc = new();
    private static readonly DebtProjectionService ProjSvc = new();

    private static CreditProfile MakeProfile(
        int scoreLow,
        int scoreHigh,
        bool historyIsClean,
        params CreditAccountProfile[] accounts)
    {
        var profile = new CreditProfile
        {
            Id = Guid.NewGuid(),
            ScenarioId = Guid.NewGuid(),
            CurrentScoreRangeLow = scoreLow,
            CurrentScoreRangeHigh = scoreHigh,
            PaymentHistoryIsClean = historyIsClean,
            CreatedUtc = DateTime.UtcNow
        };
        foreach (var a in accounts)
            profile.Accounts.Add(a);
        return profile;
    }

    private static Scenario MakeScenario(params (string name, decimal balance, decimal rate, decimal minPayment)[] debts)
    {
        var scenario = new Scenario
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Strategy = PayoffStrategy.Avalanche,
            ExtraMonthlyPayment = 0,
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

    private static ProjectionResultDto EmptyProjection() =>
        new(0, 0m, 0m, []);

    [Fact]
    public void NoAccounts_ZeroDeltaAllMonths()
    {
        var profile = MakeProfile(650, 700, true);
        var projection = EmptyProjection();

        var result = Svc.Project(profile, [], projection);

        // Month 0 only (no projection months)
        Assert.Single(result.Months);
        Assert.Equal(0, result.Months[0].ScoreDelta);
        Assert.Equal(650, result.Months[0].ScoreRangeLow);
        Assert.Equal(700, result.Months[0].ScoreRangeHigh);
    }

    [Fact]
    public void HighUtilizationDropsToZero_LargeDelta()
    {
        // Single revolving account linked to a debt that pays off
        var scenario = MakeScenario(("Card", 1000m, 0.20m, 100m));
        var debt = scenario.DebtAccounts.First();
        var projection = ProjSvc.Project(scenario);

        var account = new CreditAccountProfile
        {
            Id = Guid.NewGuid(),
            DebtAccountId = debt.Id,
            Name = "Card",
            CreditLimit = 1000m,
            CurrentBalance = 0m, // ignored when linked
            AgeMonths = 60,
            AccountType = CreditAccountType.Revolving
        };
        var profile = MakeProfile(650, 700, true, account);

        var result = Svc.Project(profile, scenario.DebtAccounts, projection);

        // Month 0: balance=1000, limit=1000 → 100% utilization → -60 score
        // Final month: balance=0 → 0% utilization → +50 score
        // Delta should improve over the projection
        var month0 = result.Months.First();
        var lastMonth = result.Months.Last();

        Assert.True(lastMonth.ScoreDelta > month0.ScoreDelta,
            "Score delta should improve as debt is paid off.");
    }

    [Fact]
    public void CleanHistory_NoDelta()
    {
        var scenario = MakeScenario(("Debt", 500m, 0.10m, 50m));
        var projection = ProjSvc.Project(scenario);

        // No accounts so utilization is always 0 / untouched — only check that history never adds
        var profile = MakeProfile(650, 700, historyIsClean: true);

        var result = Svc.Project(profile, scenario.DebtAccounts, projection);

        // With no accounts, utilization delta = 0 (no revolving), age delta = 0 (no accounts),
        // history delta = 0 (clean). All deltas must be 0.
        Assert.All(result.Months, m => Assert.Equal(0, m.ScoreDelta));
    }

    [Fact]
    public void DirtyHistory_RecoversByMonth84()
    {
        // Build a projection with at least 84 months: large balance, low payment
        var scenario = MakeScenario(("Debt", 20000m, 0.05m, 200m));
        var projection = ProjSvc.Project(scenario);

        Assert.True(projection.TotalMonths >= 84, "Projection needs at least 84 months for this test.");

        // Add a single installment account at 120+ months so the age bucket stays ≥120 throughout,
        // making ageDelta = 0. No revolving accounts so utilizationDelta = 0.
        // This isolates historyDelta as the only contributor.
        var stableAgeAccount = new CreditAccountProfile
        {
            Id = Guid.NewGuid(),
            Name = "Old Loan",
            AccountType = CreditAccountType.Installment,
            CreditLimit = 10000m,
            CurrentBalance = 0m,
            AgeMonths = 120  // ≥120 → +15, stays +15 at 120+84=204 → ageDelta = 0
        };
        var profile = MakeProfile(550, 600, historyIsClean: false, stableAgeAccount);
        var result = Svc.Project(profile, scenario.DebtAccounts, projection);

        // At month 84 the history delta should be 50 (fully recovered)
        var month84 = result.Months.FirstOrDefault(m => m.Month == 84);
        Assert.NotNull(month84);

        // historyDelta = min(50, round(50*84/84)) = 50; ageDelta = 0; utilizationDelta = 0
        Assert.Equal(50, month84.ScoreDelta);
    }

    [Fact]
    public void AccountAge_ImprovesAtThresholds()
    {
        // Start age at 10 months — below the 12-month threshold
        // After 2+ months the average age crosses 12mo (AgeScore goes from -30 to -15)
        var scenario = MakeScenario(("Debt", 5000m, 0.05m, 100m));
        var projection = ProjSvc.Project(scenario);

        var account = new CreditAccountProfile
        {
            Id = Guid.NewGuid(),
            Name = "Installment",
            AccountType = CreditAccountType.Installment, // no revolving → utilization always 0
            CreditLimit = 5000m,
            CurrentBalance = 0m,
            AgeMonths = 10
        };
        var profile = MakeProfile(650, 700, historyIsClean: true, account);

        var result = Svc.Project(profile, scenario.DebtAccounts, projection);

        // Month 0: age=10 → AgeScore=-30; Month 2: age=12 → AgeScore=-15 → delta improves by 15
        var month0 = result.Months.First(m => m.Month == 0);
        var month2 = result.Months.First(m => m.Month == 2);

        Assert.True(month2.ScoreDelta > month0.ScoreDelta,
            "Age delta should improve once average age crosses 12 months.");
    }

    [Fact]
    public void LinkedDebt_BalanceTracksProjection()
    {
        // Revolving account linked to a debt — utilization should track projection balance
        var scenario = MakeScenario(("Card", 500m, 0.20m, 100m));
        var debt = scenario.DebtAccounts.First();
        var projection = ProjSvc.Project(scenario);

        var account = new CreditAccountProfile
        {
            Id = Guid.NewGuid(),
            DebtAccountId = debt.Id,
            Name = "Card",
            CreditLimit = 1000m,
            CurrentBalance = 0m,
            AgeMonths = 24,
            AccountType = CreditAccountType.Revolving
        };
        var profile = MakeProfile(650, 700, historyIsClean: true, account);

        var result = Svc.Project(profile, scenario.DebtAccounts, projection);

        // For each month N > 0, utilization should equal projection balance / limit
        for (int n = 1; n < result.Months.Count; n++)
        {
            var snap = projection.Months[n - 1];
            var debtSnap = snap.Debts.First(d => d.DebtAccountId == debt.Id);
            double expectedUtil = (double)debtSnap.RemainingBalance / 1000.0;

            Assert.Equal(expectedUtil, result.Months[n].Utilization, precision: 10);
        }
    }
}
