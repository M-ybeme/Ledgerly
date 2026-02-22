namespace Ledgerly.Domain.Credit;

public enum CreditAccountType
{
    Revolving = 0,   // Credit cards, lines of credit — affects utilization ratio
    Installment = 1  // Personal loans, auto loans — excluded from utilization ratio
}
