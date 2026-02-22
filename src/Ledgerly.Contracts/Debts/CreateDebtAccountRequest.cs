namespace Ledgerly.Contracts.Debts;

public sealed record CreateDebtAccountRequest(
    string Name,
    decimal Balance,
    decimal AnnualInterestRate,
    decimal MinimumPayment);
