namespace Ledgerly.Domain.Credit;

public sealed class CreditAccountProfile
{
    public Guid Id { get; set; }
    public Guid CreditProfileId { get; set; }
    public Guid? DebtAccountId { get; set; }    // optional link to an existing DebtAccount
    public string Name { get; set; } = "";      // display name; for standalone accounts this is required
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; } // used for standalone accounts; linked accounts derive balance from projections
    public int AgeMonths { get; set; }
    public CreditAccountType AccountType { get; set; }
}
