namespace Ledgerly.Contracts.Scenarios;

public sealed record MonthSnapshotDto(
    int Month,
    List<DebtSnapshotDto> Debts);
