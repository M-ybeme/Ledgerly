namespace Ledgerly.Contracts.Debts;

public sealed record DebtAccountDto(
    Guid Id,
    string Name,
    decimal Balance,
    decimal AnnualInterestRate,
    decimal MinimumPayment,
    DateTime CreatedUtc);
