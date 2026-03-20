using Ledgerly.Application.Auth;
using Ledgerly.Application.Budget;
using Ledgerly.Domain.Budget;
using Ledgerly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Budget;

public sealed class EfSavingsGoalRepository : ISavingsGoalRepository
{
    private readonly LedgerlyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EfSavingsGoalRepository(LedgerlyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<List<SavingsGoal>> GetAllAsync(CancellationToken ct = default)
        => _db.SavingsGoals.AsNoTracking().OrderBy(g => g.Name).ToListAsync(ct);

    public Task<SavingsGoal?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.SavingsGoals.FirstOrDefaultAsync(g => g.Id == id, ct);

    public Task AddAsync(SavingsGoal goal, CancellationToken ct = default)
    {
        goal.UserId = _currentUser.UserId;
        return _db.SavingsGoals.AddAsync(goal, ct).AsTask();
    }

    public Task DeleteAsync(SavingsGoal goal, CancellationToken ct = default)
    {
        _db.SavingsGoals.Remove(goal);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
