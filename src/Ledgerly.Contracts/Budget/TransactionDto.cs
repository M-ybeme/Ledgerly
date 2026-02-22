using Ledgerly.Domain.Budget;

namespace Ledgerly.Contracts.Budget;

public sealed record TransactionDto(
    Guid Id,
    string Description,
    decimal Amount,
    DateOnly Date,
    Guid CategoryId,
    string CategoryName,
    CategoryType CategoryType,
    DateTime CreatedUtc);
