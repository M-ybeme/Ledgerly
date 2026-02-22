namespace Ledgerly.Domain.Scenarios;

public sealed class ActualPayment
{
    public Guid Id { get; set; }
    public Guid ScenarioId { get; set; }
    public Guid DebtAccountId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedUtc { get; set; }
}
