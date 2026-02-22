using Ledgerly.Contracts.Scenarios;
using Ledgerly.Domain.Debts;
using Ledgerly.Domain.Scenarios;

namespace Ledgerly.Application.Scenarios;

public sealed class DriftService
{
    private readonly DebtProjectionService _projectionService;

    public DriftService(DebtProjectionService projectionService)
    {
        _projectionService = projectionService;
    }

    public DriftSummaryDto Compute(Scenario scenario, List<ActualPayment> actualPayments, DateOnly today)
    {
        var scenarioStart = DateOnly.FromDateTime(scenario.CreatedUtc.ToUniversalTime().Date);
        int currentMonth = (today.Year - scenarioStart.Year) * 12 + (today.Month - scenarioStart.Month);

        var originalProjection = _projectionService.Project(scenario);

        if (currentMonth <= 0 || scenario.DebtAccounts.Count == 0)
        {
            var emptyDebts = scenario.DebtAccounts
                .Select(d => new DebtDriftDto(d.Id, d.Name, d.Balance, d.Balance, 0m, true))
                .ToList();
            return new DriftSummaryDto(
                scenario.Id, 0,
                originalProjection.TotalMonths, originalProjection.TotalMonths,
                0, 0m, emptyDebts);
        }

        // Group actual payments by (DebtAccountId, month slot)
        // Month slot: 1-indexed month relative to scenario start
        var paymentsByKey = actualPayments
            .GroupBy(p => (
                p.DebtAccountId,
                Month: (p.PaymentDate.Year - scenarioStart.Year) * 12 + (p.PaymentDate.Month - scenarioStart.Month) + 1
            ))
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        // Simulate month-by-month on actual balances
        var workingBalances = scenario.DebtAccounts
            .ToDictionary(d => d.Id, d => d.Balance);

        decimal simulatedInterestPaid = 0m;

        for (int month = 1; month <= currentMonth; month++)
        {
            foreach (var debt in scenario.DebtAccounts)
            {
                var balance = workingBalances[debt.Id];
                if (balance <= 0) continue;

                // Accrue interest (same formula as DebtProjectionService)
                decimal monthlyRate = debt.AnnualInterestRate / 12m;
                decimal interest = Math.Round(balance * monthlyRate, 2, MidpointRounding.AwayFromZero);
                balance += interest;
                simulatedInterestPaid += interest;

                // Apply actual payment if logged, otherwise assume minimum was paid
                var key = (debt.Id, month);
                decimal payment = paymentsByKey.TryGetValue(key, out var actual)
                    ? actual
                    : debt.MinimumPayment;

                payment = Math.Min(payment, balance);
                balance = Math.Max(0m, balance - payment);
                balance = Math.Round(balance, 2, MidpointRounding.AwayFromZero);
                workingBalances[debt.Id] = balance;
            }
        }

        // Get projected balances at currentMonth from original projection
        var projectedBalances = GetProjectedBalancesAt(originalProjection, currentMonth, scenario);

        // Build per-debt drift
        var debtDrifts = scenario.DebtAccounts.Select(debt =>
        {
            decimal projected = projectedBalances.GetValueOrDefault(debt.Id, 0m);
            decimal actual = workingBalances[debt.Id];
            decimal drift = projected - actual; // positive = actual lower = ahead
            return new DebtDriftDto(debt.Id, debt.Name, projected, actual, drift, drift >= 0);
        }).ToList();

        // Re-project from actual balances
        var recalcScenario = BuildRecalcScenario(scenario, workingBalances);
        var updatedProjection = _projectionService.Project(recalcScenario);

        int updatedTotalMonths = currentMonth + updatedProjection.TotalMonths;
        int monthsDrift = originalProjection.TotalMonths - updatedTotalMonths; // positive = ahead
        decimal totalInterestSaved = originalProjection.TotalInterestPaid - (simulatedInterestPaid + updatedProjection.TotalInterestPaid);

        return new DriftSummaryDto(
            scenario.Id,
            currentMonth,
            originalProjection.TotalMonths,
            updatedTotalMonths,
            monthsDrift,
            Math.Round(totalInterestSaved, 2, MidpointRounding.AwayFromZero),
            debtDrifts);
    }

    private static Dictionary<Guid, decimal> GetProjectedBalancesAt(
        ProjectionResultDto projection, int month, Scenario scenario)
    {
        // Clamp to last month if currentMonth exceeds projection length
        int idx = Math.Min(month, projection.Months.Count) - 1;
        if (idx < 0)
            return scenario.DebtAccounts.ToDictionary(d => d.Id, d => 0m);

        var snapshot = projection.Months[idx];
        return snapshot.Debts.ToDictionary(d => d.DebtAccountId, d => d.RemainingBalance);
    }

    private static Scenario BuildRecalcScenario(Scenario original, Dictionary<Guid, decimal> actualBalances)
    {
        var recalcDebts = original.DebtAccounts
            .Select(d => new DebtAccount
            {
                Id = d.Id,
                Name = d.Name,
                Balance = actualBalances.GetValueOrDefault(d.Id, 0m),
                AnnualInterestRate = d.AnnualInterestRate,
                MinimumPayment = d.MinimumPayment,
                CreatedUtc = d.CreatedUtc
            })
            .ToList();

        return new Scenario
        {
            Id = original.Id,
            Name = original.Name,
            ExtraMonthlyPayment = original.ExtraMonthlyPayment,
            Strategy = original.Strategy,
            CreatedUtc = original.CreatedUtc,
            DebtAccounts = recalcDebts
        };
    }
}
