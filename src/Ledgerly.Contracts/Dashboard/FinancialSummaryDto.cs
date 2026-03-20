namespace Ledgerly.Contracts.Dashboard;

public sealed record MonthlyFinancialSnapshotDto(
    string Month,
    decimal Income,
    decimal Expenses,
    decimal Net,
    decimal CumulativeSavings);

public sealed record FinancialSummaryDto(
    decimal TotalDebt,
    decimal CumulativeSavings,
    IReadOnlyList<MonthlyFinancialSnapshotDto> Months);
