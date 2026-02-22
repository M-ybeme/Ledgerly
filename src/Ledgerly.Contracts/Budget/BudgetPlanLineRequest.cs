namespace Ledgerly.Contracts.Budget;

public sealed record BudgetPlanLineRequest(Guid CategoryId, decimal PlannedAmount);
