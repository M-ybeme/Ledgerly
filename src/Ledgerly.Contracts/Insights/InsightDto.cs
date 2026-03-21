namespace Ledgerly.Contracts.Insights;

public enum InsightSeverity { Info, Warning, Danger }

public sealed record InsightDto(string Message, InsightSeverity Severity);

public sealed record InsightsDto(
    List<InsightDto> Debt,
    List<InsightDto> Budget,
    List<InsightDto> CashFlow);
