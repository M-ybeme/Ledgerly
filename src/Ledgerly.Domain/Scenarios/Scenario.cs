using Ledgerly.Domain.Debts;

namespace Ledgerly.Domain.Scenarios;

public sealed class Scenario
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public decimal ExtraMonthlyPayment { get; set; }
    public PayoffStrategy Strategy { get; set; }
    public DateTime CreatedUtc { get; set; }
    public ICollection<DebtAccount> DebtAccounts { get; set; } = [];
}
