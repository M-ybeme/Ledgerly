namespace Ledgerly.Contracts.Budget;

public sealed record UpdateBudgetPlanRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    List<BudgetPlanLineRequest> Lines);
