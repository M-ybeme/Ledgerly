namespace Ledgerly.Domain.NetWorth;

public sealed class NetWorthSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DateOnly Month { get; set; }        // first day of the month
    public decimal AssetsTotal { get; set; }
    public decimal LiabilitiesTotal { get; set; }
    public decimal NetWorth { get; set; }
}
