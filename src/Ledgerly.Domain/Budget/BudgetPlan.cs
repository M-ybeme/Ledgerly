namespace Ledgerly.Domain.Budget;

public sealed class BudgetPlan
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTime CreatedUtc { get; set; }
    public ICollection<BudgetPlanLine> Lines { get; set; } = [];
}
