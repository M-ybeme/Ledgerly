namespace Ledgerly.Contracts.NetWorth;

public sealed record NetWorthSnapshotDto(
    DateOnly Month,
    decimal AssetsTotal,
    decimal LiabilitiesTotal,
    decimal NetWorth);
