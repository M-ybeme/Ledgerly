namespace Ledgerly.Contracts.Scenarios;

public sealed record DriftSummaryDto(
    Guid ScenarioId,
    int CurrentMonth,
    int OriginalTotalMonths,
    int UpdatedTotalMonths,
    int MonthsDrift,
    decimal TotalInterestSaved,
    List<DebtDriftDto> Debts);
