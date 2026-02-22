namespace Ledgerly.Contracts.Credit;

public sealed record CreditScoreProjectionDto(
    Guid ScenarioId,
    int BaseScoreRangeLow,
    int BaseScoreRangeHigh,
    List<CreditScoreMonthDto> Months);
