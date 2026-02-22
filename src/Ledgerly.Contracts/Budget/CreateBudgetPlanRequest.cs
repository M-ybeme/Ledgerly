namespace Ledgerly.Contracts.Budget;

public sealed record CreateBudgetPlanRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    List<BudgetPlanLineRequest> Lines);
