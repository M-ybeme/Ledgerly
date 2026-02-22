namespace Ledgerly.Contracts.Scenarios;

public sealed record LogPaymentRequest(
    Guid DebtAccountId,
    DateOnly PaymentDate,
    decimal Amount);
