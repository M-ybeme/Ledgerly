using Ledgerly.Application.Auth;
using Ledgerly.Application.Budget;
using Ledgerly.Domain.Budget;
using Ledgerly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Budget;

public sealed class EfBudgetPlanRepository : IBudgetPlanRepository
{
    private readonly LedgerlyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EfBudgetPlanRepository(LedgerlyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<List<BudgetPlan>> GetAllAsync(CancellationToken ct = default)
        => _db.BudgetPlans
            .AsNoTracking()
            .Include(p => p.Lines).ThenInclude(l => l.Category)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync(ct);

    public Task<BudgetPlan?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.BudgetPlans
            .Include(p => p.Lines).ThenInclude(l => l.Category)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task AddAsync(BudgetPlan plan, CancellationToken ct = default)
    {
        plan.UserId = _currentUser.UserId;
        return _db.BudgetPlans.AddAsync(plan, ct).AsTask();
    }

    public Task DeleteAsync(BudgetPlan plan, CancellationToken ct = default)
    {
        _db.BudgetPlans.Remove(plan);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
