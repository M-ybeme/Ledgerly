using Ledgerly.Application.Auth;
using Ledgerly.Application.Budget;
using Ledgerly.Domain.Budget;
using Ledgerly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Budget;

public sealed class EfBudgetCategoryRepository : IBudgetCategoryRepository
{
    private readonly LedgerlyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EfBudgetCategoryRepository(LedgerlyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<List<BudgetCategory>> GetAllAsync(CancellationToken ct = default)
        => _db.BudgetCategories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public Task<BudgetCategory?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.BudgetCategories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task AddAsync(BudgetCategory category, CancellationToken ct = default)
    {
        category.UserId = _currentUser.UserId;
        return _db.BudgetCategories.AddAsync(category, ct).AsTask();
    }

    public Task UpdateAsync(BudgetCategory category, CancellationToken ct = default)
    {
        _db.BudgetCategories.Update(category);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(BudgetCategory category, CancellationToken ct = default)
    {
        _db.BudgetCategories.Remove(category);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
