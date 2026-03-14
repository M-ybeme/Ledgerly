using Ledgerly.Application.Auth;
using Ledgerly.Application.Income;
using Ledgerly.Domain.Income;
using Ledgerly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Income;

public sealed class EfIncomeSourceRepository : IIncomeSourceRepository
{
    private readonly LedgerlyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EfIncomeSourceRepository(LedgerlyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<List<IncomeSource>> GetAllAsync(CancellationToken ct = default)
        => _db.IncomeSources.AsNoTracking().OrderBy(s => s.Name).ToListAsync(ct);

    public Task<IncomeSource?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.IncomeSources.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task AddAsync(IncomeSource source, CancellationToken ct = default)
    {
        source.UserId = _currentUser.UserId;
        return _db.IncomeSources.AddAsync(source, ct).AsTask();
    }

    public Task DeleteAsync(IncomeSource source, CancellationToken ct = default)
    {
        _db.IncomeSources.Remove(source);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
