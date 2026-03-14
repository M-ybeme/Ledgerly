namespace Ledgerly.Domain.Budget;

public sealed class PlannedExpense
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Description { get; set; } = "";
    public decimal PlannedAmount { get; set; }
    public DateOnly DueDate { get; set; }
    public Guid? CategoryId { get; set; }
    public bool IsRecurring { get; set; }
    public ExpensePriority Priority { get; set; } = ExpensePriority.MustPay;

    // Filled in when the expense is paid
    public decimal? ActualAmount { get; set; }
    public DateOnly? PaidDate { get; set; }

    public bool IsPaid => PaidDate.HasValue;
}
