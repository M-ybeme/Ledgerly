using Ledgerly.Domain.Budget;

namespace Ledgerly.Contracts.Budget;

public sealed record BudgetCategoryDto(
    Guid Id,
    string Name,
    CategoryType Type,
    DateTime CreatedUtc);
