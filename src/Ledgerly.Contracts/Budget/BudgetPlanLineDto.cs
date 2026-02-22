using Ledgerly.Domain.Budget;

namespace Ledgerly.Contracts.Budget;

public sealed record BudgetPlanLineDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    CategoryType CategoryType,
    decimal PlannedAmount);
