using Ledgerly.Application.Auth;
using Ledgerly.Application.Budget;
using Ledgerly.Domain.Budget;
using Ledgerly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Budget;

public sealed class EfMonthlyBudgetRepository : IMonthlyBudgetRepository
{
    private readonly LedgerlyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EfMonthlyBudgetRepository(LedgerlyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<List<MonthlyBudget>> GetForMonthAsync(DateOnly month, CancellationToken ct = default)
        => _db.MonthlyBudgets.AsNoTracking().Where(b => b.Month == month).ToListAsync(ct);

    public Task<MonthlyBudget?> GetAsync(DateOnly month, Guid categoryId, CancellationToken ct = default)
        => _db.MonthlyBudgets.FirstOrDefaultAsync(b => b.Month == month && b.CategoryId == categoryId, ct);

    public Task AddAsync(MonthlyBudget budget, CancellationToken ct = default)
    {
        budget.UserId = _currentUser.UserId;
        return _db.MonthlyBudgets.AddAsync(budget, ct).AsTask();
    }

    public Task DeleteAsync(MonthlyBudget budget, CancellationToken ct = default)
    {
        _db.MonthlyBudgets.Remove(budget);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
