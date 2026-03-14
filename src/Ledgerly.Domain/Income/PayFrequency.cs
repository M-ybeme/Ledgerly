namespace Ledgerly.Domain.Income;

public enum PayFrequency
{
    Weekly = 0,      // 52x / year
    Biweekly = 1,    // 26x / year
    SemiMonthly = 2, // 24x / year (twice a month)
    Monthly = 3      // 12x / year
}
