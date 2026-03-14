using Ledgerly.Domain.Budget;

namespace Ledgerly.Application.Budget;

public interface IMonthlyBudgetRepository
{
    Task<List<MonthlyBudget>> GetForMonthAsync(DateOnly month, CancellationToken ct = default);
    Task<MonthlyBudget?> GetAsync(DateOnly month, Guid categoryId, CancellationToken ct = default);
    Task AddAsync(MonthlyBudget budget, CancellationToken ct = default);
    Task DeleteAsync(MonthlyBudget budget, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
