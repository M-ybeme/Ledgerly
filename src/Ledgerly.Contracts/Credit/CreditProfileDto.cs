namespace Ledgerly.Contracts.Credit;

public sealed record CreditProfileDto(
    Guid Id,
    Guid ScenarioId,
    int CurrentScoreRangeLow,
    int CurrentScoreRangeHigh,
    bool PaymentHistoryIsClean,
    List<CreditAccountProfileDto> Accounts,
    DateTime CreatedUtc);
