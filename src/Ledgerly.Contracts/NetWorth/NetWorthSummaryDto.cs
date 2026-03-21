namespace Ledgerly.Contracts.NetWorth;

public sealed record NetWorthSummaryDto(
    decimal CurrentAssets,
    decimal CurrentLiabilities,
    decimal CurrentNetWorth,
    List<NetWorthSnapshotDto> History);
