namespace Ledgerly.Domain.Budget;

public sealed class BudgetCategory
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public CategoryType Type { get; set; }
    public DateTime CreatedUtc { get; set; }
}
