using Ledgerly.Domain.Scenarios;

namespace Ledgerly.Contracts.Scenarios;

public sealed record CreateScenarioRequest(
    string Name,
    decimal ExtraMonthlyPayment,
    PayoffStrategy Strategy,
    List<Guid> DebtAccountIds);
