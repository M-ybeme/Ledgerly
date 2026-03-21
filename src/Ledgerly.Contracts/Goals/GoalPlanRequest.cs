namespace Ledgerly.Contracts.Goals;

public enum GoalType { DebtFree, SaveAmount, SpendingCap }

public sealed record GoalPlanRequest(
    GoalType GoalType,
    // DebtFree
    DateOnly? TargetDate,
    // SaveAmount
    decimal? TargetSavingsAmount,
    int? MonthsToSave,
    // SpendingCap
    decimal? MonthlySpendingCap);
