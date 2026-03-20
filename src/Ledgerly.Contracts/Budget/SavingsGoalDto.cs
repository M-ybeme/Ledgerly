namespace Ledgerly.Contracts.Budget;

public sealed record SavingsGoalDto(
    Guid Id,
    string Name,
    decimal TargetAmount,
    decimal CurrentAmount,
    decimal Progress,
    decimal Remaining,
    DateOnly? TargetDate,
    DateTime CreatedUtc);
