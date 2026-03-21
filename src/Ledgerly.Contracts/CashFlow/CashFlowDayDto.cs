namespace Ledgerly.Contracts.CashFlow;

public sealed record CashFlowDayDto(
    DateOnly Date,
    decimal OpeningBalance,
    decimal IncomeTotal,
    decimal ExpenseTotal,
    decimal BurnRate,
    decimal ClosingBalance,
    List<string> IncomeEvents,
    List<string> ExpenseEvents);
