using Ledgerly.Contracts.Scenarios;
using Ledgerly.Domain.Scenarios;

namespace Ledgerly.Application.Scenarios;

public sealed class DebtProjectionService
{
    private const int MaxMonths = 360;

    public ProjectionResultDto Project(Scenario scenario)
    {
        if (scenario.DebtAccounts.Count == 0)
            return new ProjectionResultDto(0, 0m, 0m, []);

        // Work on mutable copies so we don't modify the domain entities
        var workingDebts = scenario.DebtAccounts
            .Select(d => new WorkingDebt(d.Id, d.Name, d.Balance, d.AnnualInterestRate, d.MinimumPayment))
            .ToList();

        var months = new List<MonthSnapshotDto>();
        decimal totalInterestPaid = 0m;
        decimal totalPrincipalPaid = 0m;

        for (int month = 1; month <= MaxMonths; month++)
        {
            var activeDebts = workingDebts.Where(d => d.Balance > 0).ToList();
            if (activeDebts.Count == 0) break;

            // Track per-debt payment details for this month
            var debtPayments = workingDebts.ToDictionary(d => d.Id, _ => (InterestPaid: 0m, PrincipalPaid: 0m));

            // Step 1: Accrue interest on all active debts
            foreach (var debt in activeDebts)
            {
                decimal monthlyRate = debt.AnnualInterestRate / 12m;
                decimal interest = Math.Round(debt.Balance * monthlyRate, 2, MidpointRounding.AwayFromZero);
                debt.Balance += interest;
                debtPayments[debt.Id] = debtPayments[debt.Id] with { InterestPaid = interest };
                totalInterestPaid += interest;
            }

            // Step 2: Apply minimum payments to all active debts
            foreach (var debt in activeDebts)
            {
                decimal payment = Math.Min(debt.MinimumPayment, debt.Balance);
                debt.Balance = Math.Max(0m, debt.Balance - payment);
                debt.Balance = Math.Round(debt.Balance, 2, MidpointRounding.AwayFromZero);
                debtPayments[debt.Id] = debtPayments[debt.Id] with { PrincipalPaid = payment };
                totalPrincipalPaid += payment;
            }

            // Step 3: Apply extra payment to priority debts (cascade if one gets paid off)
            if (scenario.ExtraMonthlyPayment > 0)
            {
                var prioritized = scenario.Strategy == PayoffStrategy.Snowball
                    ? activeDebts.Where(d => d.Balance > 0).OrderBy(d => d.Balance).ToList()
                    : activeDebts.Where(d => d.Balance > 0).OrderByDescending(d => d.AnnualInterestRate).ToList();

                decimal extraRemaining = scenario.ExtraMonthlyPayment;
                foreach (var debt in prioritized)
                {
                    if (extraRemaining <= 0) break;
                    decimal payment = Math.Min(extraRemaining, debt.Balance);
                    debt.Balance = Math.Round(debt.Balance - payment, 2, MidpointRounding.AwayFromZero);
                    extraRemaining -= payment;
                    var existing = debtPayments[debt.Id];
                    debtPayments[debt.Id] = existing with { PrincipalPaid = existing.PrincipalPaid + payment };
                    totalPrincipalPaid += payment;
                }
            }

            // Build snapshot for this month
            var debtSnapshots = workingDebts
                .Select(d => new DebtSnapshotDto(
                    d.Id,
                    d.Name,
                    d.Balance,
                    debtPayments[d.Id].InterestPaid,
                    debtPayments[d.Id].PrincipalPaid))
                .ToList();

            months.Add(new MonthSnapshotDto(month, debtSnapshots));
        }

        return new ProjectionResultDto(
            months.Count,
            totalInterestPaid,
            totalInterestPaid + totalPrincipalPaid,
            months);
    }

    private sealed class WorkingDebt
    {
        public Guid Id { get; }
        public string Name { get; }
        public decimal Balance { get; set; }
        public decimal AnnualInterestRate { get; }
        public decimal MinimumPayment { get; }

        public WorkingDebt(Guid id, string name, decimal balance, decimal annualInterestRate, decimal minimumPayment)
        {
            Id = id;
            Name = name;
            Balance = balance;
            AnnualInterestRate = annualInterestRate;
            MinimumPayment = minimumPayment;
        }
    }
}
