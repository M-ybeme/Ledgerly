namespace Ledgerly.Domain.Income;

public sealed class IncomeSource
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public decimal Amount { get; set; }
    public PayFrequency Frequency { get; set; }
    public Guid? AccountId { get; set; }
    public DateTime CreatedUtc { get; set; }

    /// <summary>Normalized monthly equivalent of this income source.</summary>
    public decimal MonthlyAmount => Frequency switch
    {
        PayFrequency.Weekly     => Amount * 52m / 12m,
        PayFrequency.Biweekly   => Amount * 26m / 12m,
        PayFrequency.SemiMonthly => Amount * 2m,
        _                        => Amount
    };
}
