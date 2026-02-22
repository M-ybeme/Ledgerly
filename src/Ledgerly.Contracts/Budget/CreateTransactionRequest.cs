namespace Ledgerly.Contracts.Budget;

public sealed record CreateTransactionRequest(
    string Description,
    decimal Amount,
    DateOnly Date,
    Guid CategoryId);
