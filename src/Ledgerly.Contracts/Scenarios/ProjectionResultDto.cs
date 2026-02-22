namespace Ledgerly.Contracts.Scenarios;

public sealed record ProjectionResultDto(
    int TotalMonths,
    decimal TotalInterestPaid,
    decimal TotalPaid,
    List<MonthSnapshotDto> Months);
