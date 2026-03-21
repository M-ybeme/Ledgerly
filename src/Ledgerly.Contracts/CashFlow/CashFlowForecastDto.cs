namespace Ledgerly.Contracts.CashFlow;

public sealed record CashFlowForecastDto(
    decimal StartingBalance,
    int Days,
    decimal DailyBurnRate,
    List<CashFlowDayDto> DailySnapshots,
    decimal LowestBalance,
    DateOnly LowestBalanceDate,
    int? DaysUntilNegative,
    List<string> Warnings);
