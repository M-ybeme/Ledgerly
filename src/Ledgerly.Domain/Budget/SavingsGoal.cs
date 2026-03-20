namespace Ledgerly.Domain.Budget;

public sealed class SavingsGoal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateOnly? TargetDate { get; set; }
    public DateTime CreatedUtc { get; set; }

    public decimal Progress => TargetAmount > 0
        ? Math.Min(CurrentAmount / TargetAmount * 100m, 100m)
        : 0m;

    public decimal Remaining => Math.Max(TargetAmount - CurrentAmount, 0m);
}
