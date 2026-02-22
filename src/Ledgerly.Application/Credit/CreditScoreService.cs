using Ledgerly.Contracts.Credit;
using Ledgerly.Contracts.Scenarios;
using Ledgerly.Domain.Credit;
using Ledgerly.Domain.Debts;

namespace Ledgerly.Application.Credit;

public sealed class CreditScoreService
{
    public CreditScoreProjectionDto Project(
        CreditProfile profile,
        ICollection<DebtAccount> scenarioDebts,
        ProjectionResultDto projection)
    {
        var revolving = profile.Accounts
            .Where(a => a.AccountType == CreditAccountType.Revolving)
            .ToList();

        double totalLimit = (double)revolving.Sum(a => a.CreditLimit);

        double utilization0 = totalLimit > 0
            ? (double)GetRevolvingBalance(revolving, scenarioDebts, null) / totalLimit
            : 0.0;

        double avgAge0 = profile.Accounts.Count > 0
            ? profile.Accounts.Average(a => (double)a.AgeMonths)
            : 0.0;

        int utilizationScore0 = UtilizationScore(utilization0);
        int ageScore0 = AgeScore(avgAge0);

        var months = new List<CreditScoreMonthDto>(projection.TotalMonths + 1);

        for (int n = 0; n <= projection.TotalMonths; n++)
        {
            MonthSnapshotDto? snapshot = n == 0
                ? null
                : projection.Months.Count >= n ? projection.Months[n - 1] : null;

            double utilizationN = totalLimit > 0
                ? (double)GetRevolvingBalance(revolving, scenarioDebts, snapshot) / totalLimit
                : 0.0;

            int utilizationDelta = UtilizationScore(utilizationN) - utilizationScore0;
            int ageDelta = AgeScore(avgAge0 + n) - ageScore0;
            int historyDelta = profile.PaymentHistoryIsClean
                ? 0
                : (int)Math.Round(50.0 * n / 84.0) > 50
                    ? 50
                    : (int)Math.Round(50.0 * n / 84.0);

            int totalDelta = utilizationDelta + ageDelta + historyDelta;

            int lowN = Math.Clamp(profile.CurrentScoreRangeLow + totalDelta, 300, 850);
            int highN = Math.Clamp(profile.CurrentScoreRangeHigh + totalDelta, 300, 850);

            months.Add(new CreditScoreMonthDto(n, lowN, highN, utilizationN, totalDelta));
        }

        return new CreditScoreProjectionDto(
            profile.ScenarioId,
            profile.CurrentScoreRangeLow,
            profile.CurrentScoreRangeHigh,
            months);
    }

    private static decimal GetRevolvingBalance(
        List<CreditAccountProfile> revolving,
        ICollection<DebtAccount> scenarioDebts,
        MonthSnapshotDto? snapshot)
    {
        decimal total = 0m;
        foreach (var account in revolving)
        {
            if (account.DebtAccountId.HasValue)
            {
                if (snapshot is not null)
                {
                    var debtSnap = snapshot.Debts.FirstOrDefault(d => d.DebtAccountId == account.DebtAccountId.Value);
                    total += debtSnap?.RemainingBalance ?? 0m;
                }
                else
                {
                    var debt = scenarioDebts.FirstOrDefault(d => d.Id == account.DebtAccountId.Value);
                    total += debt?.Balance ?? 0m;
                }
            }
            else
            {
                total += snapshot is not null ? 0m : account.CurrentBalance;
            }
        }
        return total;
    }

    private static int UtilizationScore(double utilization) => utilization switch
    {
        < 0.10 => 50,
        < 0.30 => 20,
        < 0.50 => 0,
        < 0.75 => -30,
        _ => -60
    };

    private static int AgeScore(double ageMonths) => ageMonths switch
    {
        < 12 => -30,
        < 24 => -15,
        < 60 => -5,
        < 120 => 0,
        _ => 15
    };
}
