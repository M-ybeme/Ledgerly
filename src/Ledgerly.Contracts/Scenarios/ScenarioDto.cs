using Ledgerly.Contracts.Debts;
using Ledgerly.Domain.Scenarios;

namespace Ledgerly.Contracts.Scenarios;

public sealed record ScenarioDto(
    Guid Id,
    string Name,
    decimal ExtraMonthlyPayment,
    PayoffStrategy Strategy,
    List<DebtAccountDto> DebtAccounts,
    DateTime CreatedUtc);
