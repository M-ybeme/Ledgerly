using Ledgerly.Domain.Scenarios;

namespace Ledgerly.Contracts.Scenarios;

public sealed record ScenarioSummaryDto(
    Guid ScenarioId,
    string ScenarioName,
    PayoffStrategy Strategy,
    decimal ExtraMonthlyPayment,
    int TotalMonths,
    decimal TotalInterestPaid,
    decimal TotalPaid);
