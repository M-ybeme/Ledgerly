namespace Ledgerly.Contracts.Budget;

public sealed record UpdateSavingsGoalRequest(
    string Name,
    decimal TargetAmount,
    decimal CurrentAmount,
    DateOnly? TargetDate);
