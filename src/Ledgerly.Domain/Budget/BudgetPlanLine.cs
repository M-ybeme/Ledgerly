namespace Ledgerly.Domain.Budget;

public sealed class BudgetPlanLine
{
    public Guid Id { get; set; }
    public Guid BudgetPlanId { get; set; }
    public BudgetPlan BudgetPlan { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public BudgetCategory Category { get; set; } = null!;
    public decimal PlannedAmount { get; set; }
}
