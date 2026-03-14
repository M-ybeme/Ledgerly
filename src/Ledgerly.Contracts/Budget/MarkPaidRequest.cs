namespace Ledgerly.Contracts.Budget;

public sealed record MarkPaidRequest(decimal ActualAmount, DateOnly PaidDate);
