using Ledgerly.Domain.Budget;

namespace Ledgerly.Contracts.Budget;

public sealed record UpdatePlannedExpenseRequest(
    string Description,
    decimal PlannedAmount,
    DateOnly DueDate,
    Guid? CategoryId,
    bool IsRecurring,
    ExpensePriority Priority = ExpensePriority.MustPay);
