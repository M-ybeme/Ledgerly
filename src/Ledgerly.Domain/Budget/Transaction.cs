namespace Ledgerly.Domain.Budget;

public sealed class Transaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public Guid CategoryId { get; set; }
    public BudgetCategory Category { get; set; } = null!;
    public DateTime CreatedUtc { get; set; }
}
