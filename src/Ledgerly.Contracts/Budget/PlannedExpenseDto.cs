using Ledgerly.Domain.Budget;

namespace Ledgerly.Contracts.Budget;

public sealed record PlannedExpenseDto(
    Guid Id,
    string Description,
    decimal PlannedAmount,
    DateOnly DueDate,
    Guid? CategoryId,
    string? CategoryName,
    bool IsRecurring,
    bool IsPaid,
    decimal? ActualAmount,
    DateOnly? PaidDate,
    ExpensePriority Priority);
