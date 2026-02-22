using Ledgerly.Domain.Budget;

namespace Ledgerly.Contracts.Budget;

public sealed record CreateBudgetCategoryRequest(string Name, CategoryType Type);
