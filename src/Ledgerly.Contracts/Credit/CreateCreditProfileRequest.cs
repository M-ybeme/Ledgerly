namespace Ledgerly.Contracts.Credit;

public sealed record CreateCreditProfileRequest(
    int CurrentScoreRangeLow,
    int CurrentScoreRangeHigh,
    bool PaymentHistoryIsClean,
    List<CreditAccountProfileRequest> Accounts);
