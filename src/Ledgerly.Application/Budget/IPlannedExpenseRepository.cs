using Ledgerly.Domain.Budget;

namespace Ledgerly.Application.Budget;

public interface IPlannedExpenseRepository
{
    Task<List<PlannedExpense>> GetAllAsync(CancellationToken ct = default);
    Task<PlannedExpense?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(PlannedExpense expense, CancellationToken ct = default);
    Task DeleteAsync(PlannedExpense expense, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
