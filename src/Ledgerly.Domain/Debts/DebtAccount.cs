using Ledgerly.Domain.Scenarios;

namespace Ledgerly.Domain.Debts;

public sealed class DebtAccount
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public decimal Balance { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public decimal MinimumPayment { get; set; }
    public DateTime CreatedUtc { get; set; }
    public ICollection<Scenario> Scenarios { get; set; } = [];
}
