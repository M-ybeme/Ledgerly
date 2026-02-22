namespace Ledgerly.Contracts.Debts;

public sealed record UpdateDebtAccountRequest(
    string Name,
    decimal Balance,
    decimal AnnualInterestRate,
    decimal MinimumPayment);
