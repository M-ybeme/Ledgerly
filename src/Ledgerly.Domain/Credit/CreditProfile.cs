namespace Ledgerly.Domain.Credit;

public sealed class CreditProfile
{
    public Guid Id { get; set; }
    public Guid ScenarioId { get; set; }
    public int CurrentScoreRangeLow { get; set; }
    public int CurrentScoreRangeHigh { get; set; }
    public bool PaymentHistoryIsClean { get; set; }
    public ICollection<CreditAccountProfile> Accounts { get; set; } = [];
    public DateTime CreatedUtc { get; set; }
}
