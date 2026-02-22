using Ledgerly.Domain.Budget;

namespace Ledgerly.Application.Budget;

public interface IBudgetCategoryRepository
{
    Task<List<BudgetCategory>> GetAllAsync(CancellationToken ct = default);
    Task<BudgetCategory?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(BudgetCategory category, CancellationToken ct = default);
    Task UpdateAsync(BudgetCategory category, CancellationToken ct = default);
    Task DeleteAsync(BudgetCategory category, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
