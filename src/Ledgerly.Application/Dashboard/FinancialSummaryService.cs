using Ledgerly.Application.Budget;
using Ledgerly.Contracts.Dashboard;
using Ledgerly.Domain.Budget;

namespace Ledgerly.Application.Dashboard;

public sealed class FinancialSummaryService
{
    private readonly ITransactionRepository _transactions;

    public FinancialSummaryService(ITransactionRepository transactions)
    {
        _transactions = transactions;
    }

    public async Task<(IReadOnlyList<MonthlyFinancialSnapshotDto> Months, decimal CumulativeSavings)> GetMonthlySavingsAsync(
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var start = new DateOnly(today.Year, today.Month, 1).AddMonths(-11);

        var transactions = await _transactions.GetByDateRangeAsync(start, today, ct);

        var snapshots = new List<MonthlyFinancialSnapshotDto>(12);
        decimal cumulative = 0;

        for (int i = 11; i >= 0; i--)
        {
            var m = DateTime.Today.AddMonths(-i);
            var monthStart = new DateOnly(m.Year, m.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var monthTx = transactions.Where(t => t.Date >= monthStart && t.Date <= monthEnd).ToList();
            var income = monthTx.Where(t => t.Category.Type == CategoryType.Income).Sum(t => t.Amount);
            var expenses = monthTx.Where(t => t.Category.Type == CategoryType.Expense).Sum(t => t.Amount);
            var net = income - expenses;
            cumulative += net;

            snapshots.Add(new MonthlyFinancialSnapshotDto(m.ToString("MMM yy"), income, expenses, net, cumulative));
        }

        return (snapshots, cumulative);
    }
}
