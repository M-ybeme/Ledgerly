using Ledgerly.Domain.Income;

namespace Ledgerly.Contracts.Income;

public sealed record IncomeSourceDto(
    Guid Id,
    string Name,
    decimal Amount,
    PayFrequency Frequency,
    decimal MonthlyAmount,
    Guid? AccountId,
    string? AccountName,
    DateTime CreatedUtc);
