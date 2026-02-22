namespace Ledgerly.Contracts.Scenarios;

public sealed record ActualPaymentDto(
    Guid Id,
    Guid ScenarioId,
    Guid DebtAccountId,
    DateOnly PaymentDate,
    decimal Amount,
    DateTime CreatedUtc);
