using Ledgerly.Contracts.Accounts;
using Ledgerly.Contracts.Budget;
using Ledgerly.Contracts.Debts;
using Ledgerly.Contracts.Income;
using Ledgerly.Contracts.Scenarios;

namespace Ledgerly.Contracts.Export;

public sealed record ExportDto(
    DateTime ExportedAt,
    string Email,
    IReadOnlyList<AccountDto> Accounts,
    IReadOnlyList<DebtAccountDto> Debts,
    IReadOnlyList<IncomeSourceDto> IncomeSources,
    IReadOnlyList<PlannedExpenseDto> PlannedExpenses,
    IReadOnlyList<BudgetCategoryDto> BudgetCategories,
    IReadOnlyList<TransactionDto> Transactions,
    IReadOnlyList<ScenarioDto> Scenarios);
