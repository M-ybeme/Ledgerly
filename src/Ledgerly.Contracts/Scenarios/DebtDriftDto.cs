namespace Ledgerly.Contracts.Scenarios;

public sealed record DebtDriftDto(
    Guid DebtAccountId,
    string Name,
    decimal ProjectedBalance,
    decimal ActualBalance,
    decimal BalanceDrift,
    bool IsOnTrack);
