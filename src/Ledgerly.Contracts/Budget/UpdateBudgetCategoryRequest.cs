using Ledgerly.Domain.Budget;

namespace Ledgerly.Contracts.Budget;

public sealed record UpdateBudgetCategoryRequest(string Name, CategoryType Type);
