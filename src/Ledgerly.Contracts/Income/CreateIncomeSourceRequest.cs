using Ledgerly.Domain.Income;

namespace Ledgerly.Contracts.Income;

public sealed record CreateIncomeSourceRequest(
    string Name,
    decimal Amount,
    PayFrequency Frequency,
    Guid? AccountId);
