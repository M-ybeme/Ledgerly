namespace Ledgerly.Contracts.Budget;

public sealed record SetMonthlyBudgetRequest(Guid CategoryId, decimal Amount);
