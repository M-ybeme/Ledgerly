namespace Ledgerly.Contracts.Scenarios;

public sealed record DebtSnapshotDto(
    Guid DebtAccountId,
    string Name,
    decimal RemainingBalance,
    decimal InterestPaid,
    decimal PrincipalPaid);
