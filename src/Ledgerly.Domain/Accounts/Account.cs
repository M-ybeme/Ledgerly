namespace Ledgerly.Domain.Accounts;

public sealed class Account
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public AccountType Type { get; set; }
    public DateTime CreatedUtc { get; set; }
}