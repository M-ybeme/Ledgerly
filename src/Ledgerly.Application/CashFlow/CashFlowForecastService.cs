using Ledgerly.Application.Budget;
using Ledgerly.Application.Income;
using Ledgerly.Contracts.CashFlow;
using Ledgerly.Domain.Income;

namespace Ledgerly.Application.CashFlow;

public sealed class CashFlowForecastService
{
    private readonly IIncomeSourceRepository _incomeRepo;
    private readonly IPlannedExpenseRepository _expenseRepo;

    public CashFlowForecastService(IIncomeSourceRepository incomeRepo, IPlannedExpenseRepository expenseRepo)
    {
        _incomeRepo = incomeRepo;
        _expenseRepo = expenseRepo;
    }

    public async Task<CashFlowForecastDto> ComputeAsync(
        decimal startingBalance,
        int days,
        decimal dailyBurnRate = 0m,
        CancellationToken ct = default)
    {
        if (days is not (30 or 60 or 90))
            throw new ArgumentException("Days must be 30, 60, or 90.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var endDate = today.AddDays(days - 1);

        var incomeSources = await _incomeRepo.GetAllAsync(ct);
        var allExpenses = await _expenseRepo.GetAllAsync(ct);

        // Only unpaid expenses due within the forecast window
        var pendingExpenses = allExpenses
            .Where(e => !e.IsPaid && e.DueDate >= today && e.DueDate <= endDate)
            .ToList();

        // Build per-day event maps
        var incomeByDay = new Dictionary<DateOnly, List<(string Name, decimal Amount)>>();
        var expenseByDay = new Dictionary<DateOnly, List<(string Description, decimal Amount)>>();

        foreach (var src in incomeSources)
        {
            foreach (var date in GetIncomeDates(src, today, endDate))
            {
                if (!incomeByDay.ContainsKey(date)) incomeByDay[date] = [];
                incomeByDay[date].Add((src.Name, src.Amount));
            }
        }

        foreach (var exp in pendingExpenses)
        {
            if (!expenseByDay.ContainsKey(exp.DueDate)) expenseByDay[exp.DueDate] = [];
            expenseByDay[exp.DueDate].Add((exp.Description, exp.PlannedAmount));
        }

        // Compute daily running balance
        var snapshots = new List<CashFlowDayDto>(days);
        var balance = startingBalance;
        var lowestBalance = balance;
        var lowestDate = today;
        int? daysUntilNegative = null;
        var warnings = new List<string>();
        var warnedNegative = false;
        var warnedLow = false;

        for (int d = 0; d < days; d++)
        {
            var date = today.AddDays(d);
            var dayIncome = incomeByDay.GetValueOrDefault(date, []);
            var dayExpenses = expenseByDay.GetValueOrDefault(date, []);

            var incomeTotal = dayIncome.Sum(i => i.Amount);
            var expenseTotal = dayExpenses.Sum(e => e.Amount);
            var opening = balance;

            balance = balance + incomeTotal - expenseTotal - dailyBurnRate;

            snapshots.Add(new CashFlowDayDto(
                date,
                opening,
                incomeTotal,
                expenseTotal,
                dailyBurnRate,
                balance,
                dayIncome.Select(i => i.Name).ToList(),
                dayExpenses.Select(e => e.Description).ToList()));

            if (balance < lowestBalance)
            {
                lowestBalance = balance;
                lowestDate = date;
            }

            if (balance < 0)
            {
                daysUntilNegative ??= d + 1;
                if (!warnedNegative)
                {
                    warnings.Add($"Balance goes negative on {date:MMMM d} (${balance:F0})");
                    warnedNegative = true;
                }
            }
            else if (balance < 100 && !warnedLow && !warnedNegative)
            {
                warnings.Add($"Low balance (${balance:F0}) on {date:MMMM d}");
                warnedLow = true;
            }
        }

        return new CashFlowForecastDto(
            startingBalance,
            days,
            dailyBurnRate,
            snapshots,
            lowestBalance,
            lowestDate,
            daysUntilNegative,
            warnings);
    }

    /// <summary>Returns the dates within [today, endDate] when this income source pays out.</summary>
    private static IEnumerable<DateOnly> GetIncomeDates(IncomeSource source, DateOnly today, DateOnly endDate)
    {
        switch (source.Frequency)
        {
            case PayFrequency.Weekly:
            {
                var d = today.AddDays(7);
                while (d <= endDate) { yield return d; d = d.AddDays(7); }
                break;
            }
            case PayFrequency.Biweekly:
            {
                var d = today.AddDays(14);
                while (d <= endDate) { yield return d; d = d.AddDays(14); }
                break;
            }
            case PayFrequency.SemiMonthly:
            {
                var year = today.Year;
                var month = today.Month;
                while (true)
                {
                    var first = new DateOnly(year, month, 1);
                    if (first > endDate) break;
                    if (first > today) yield return first;
                    var fifteenth = new DateOnly(year, month, 15);
                    if (fifteenth > today && fifteenth <= endDate) yield return fifteenth;
                    month++;
                    if (month > 12) { month = 1; year++; }
                }
                break;
            }
            case PayFrequency.Monthly:
            {
                var year = today.Year;
                var month = today.Month;
                while (true)
                {
                    var first = new DateOnly(year, month, 1);
                    if (first > endDate) break;
                    if (first > today) yield return first;
                    month++;
                    if (month > 12) { month = 1; year++; }
                }
                break;
            }
        }
    }
}
