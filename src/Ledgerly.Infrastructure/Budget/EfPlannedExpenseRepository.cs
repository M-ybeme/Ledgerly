using Ledgerly.Application.Auth;
using Ledgerly.Application.Budget;
using Ledgerly.Domain.Budget;
using Ledgerly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Budget;

public sealed class EfPlannedExpenseRepository : IPlannedExpenseRepository
{
    private readonly LedgerlyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EfPlannedExpenseRepository(LedgerlyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<List<PlannedExpense>> GetAllAsync(CancellationToken ct = default)
        => _db.PlannedExpenses.AsNoTracking().OrderBy(e => e.DueDate).ToListAsync(ct);

    public Task<PlannedExpense?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.PlannedExpenses.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task AddAsync(PlannedExpense expense, CancellationToken ct = default)
    {
        expense.UserId = _currentUser.UserId;
        return _db.PlannedExpenses.AddAsync(expense, ct).AsTask();
    }

    public Task DeleteAsync(PlannedExpense expense, CancellationToken ct = default)
    {
        _db.PlannedExpenses.Remove(expense);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
