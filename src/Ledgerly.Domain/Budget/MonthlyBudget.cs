namespace Ledgerly.Domain.Budget;

/// <summary>A spending target for one category in one calendar month.</summary>
public sealed class MonthlyBudget
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    /// <summary>Always the first day of the target month.</summary>
    public DateOnly Month { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
}
