namespace Ledgerly.Contracts.Credit;

public sealed record CreditScoreMonthDto(
    int Month,
    int ScoreRangeLow,
    int ScoreRangeHigh,
    double Utilization,
    int ScoreDelta);
