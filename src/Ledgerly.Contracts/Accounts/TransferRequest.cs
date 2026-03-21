namespace Ledgerly.Contracts.Accounts;

public sealed record TransferRequest(Guid FromAccountId, Guid ToAccountId, decimal Amount);
