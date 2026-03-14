namespace Ledgerly.Contracts.Budget;

public sealed record MonthlyBudgetDto(
    Guid Id,
    DateOnly Month,
    Guid CategoryId,
    string CategoryName,
    decimal BudgetedAmount,
    decimal ActualAmount,
    decimal Remaining);
