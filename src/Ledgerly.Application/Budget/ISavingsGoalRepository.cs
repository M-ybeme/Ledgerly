using Ledgerly.Domain.Budget;

namespace Ledgerly.Application.Budget;

public interface ISavingsGoalRepository
{
    Task<List<SavingsGoal>> GetAllAsync(CancellationToken ct = default);
    Task<SavingsGoal?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(SavingsGoal goal, CancellationToken ct = default);
    Task DeleteAsync(SavingsGoal goal, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
