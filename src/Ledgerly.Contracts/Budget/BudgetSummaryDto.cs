namespace Ledgerly.Contracts.Budget;

public sealed record BudgetSummaryDto(
    Guid PlanId,
    string PlanName,
    DateOnly StartDate,
    DateOnly EndDate,
    List<CategorySummaryDto> Categories,
    decimal TotalPlannedIncome,
    decimal TotalActualIncome,
    decimal TotalPlannedExpenses,
    decimal TotalActualExpenses,
    decimal NetPlanned,
    decimal NetActual);
