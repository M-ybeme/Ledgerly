namespace Ledgerly.Contracts.Budget;

public sealed record CreateSavingsGoalRequest(
    string Name,
    decimal TargetAmount,
    decimal CurrentAmount,
    DateOnly? TargetDate);
