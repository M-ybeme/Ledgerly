namespace Ledgerly.Contracts.Budget;

public sealed record UpdateTransactionRequest(
    string Description,
    decimal Amount,
    DateOnly Date,
    Guid CategoryId);
