using Ledgerly.Domain.Budget;

namespace Ledgerly.Application.Budget;

public interface IBudgetPlanRepository
{
    Task<List<BudgetPlan>> GetAllAsync(CancellationToken ct = default);
    Task<BudgetPlan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(BudgetPlan plan, CancellationToken ct = default);
    Task DeleteAsync(BudgetPlan plan, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
