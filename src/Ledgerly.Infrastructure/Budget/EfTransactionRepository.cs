using Ledgerly.Application.Auth;
using Ledgerly.Application.Budget;
using Ledgerly.Domain.Budget;
using Ledgerly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Budget;

public sealed class EfTransactionRepository : ITransactionRepository
{
    private readonly LedgerlyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EfTransactionRepository(LedgerlyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<List<Transaction>> GetAllAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default)
    {
        var query = _db.Transactions.AsNoTracking().Include(t => t.Category).AsQueryable();
        if (from.HasValue) query = query.Where(t => t.Date >= from.Value);
        if (to.HasValue) query = query.Where(t => t.Date <= to.Value);
        return query.OrderByDescending(t => t.Date).ToListAsync(ct);
    }

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Transactions.Include(t => t.Category).FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<List<Transaction>> GetByDateRangeAsync(DateOnly start, DateOnly end, CancellationToken ct = default)
        => _db.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Where(t => t.Date >= start && t.Date <= end)
            .OrderByDescending(t => t.Date)
            .ToListAsync(ct);

    public Task AddAsync(Transaction transaction, CancellationToken ct = default)
    {
        transaction.UserId = _currentUser.UserId;
        return _db.Transactions.AddAsync(transaction, ct).AsTask();
    }

    public Task UpdateAsync(Transaction transaction, CancellationToken ct = default)
    {
        _db.Transactions.Update(transaction);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Transaction transaction, CancellationToken ct = default)
    {
        _db.Transactions.Remove(transaction);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
