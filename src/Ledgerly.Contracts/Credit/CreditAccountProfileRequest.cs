using Ledgerly.Domain.Credit;

namespace Ledgerly.Contracts.Credit;

public sealed record CreditAccountProfileRequest(
    Guid? DebtAccountId,
    string Name,
    decimal CreditLimit,
    decimal CurrentBalance,
    int AgeMonths,
    CreditAccountType AccountType);
