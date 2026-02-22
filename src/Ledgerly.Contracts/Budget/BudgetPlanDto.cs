namespace Ledgerly.Contracts.Budget;

public sealed record BudgetPlanDto(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    List<BudgetPlanLineDto> Lines,
    DateTime CreatedUtc);
