using Ledgerly.Application.Accounts;
using Ledgerly.Application.Auth;
using Ledgerly.Domain.Accounts;
using Ledgerly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Accounts;

public sealed class EfAccountRepository : IAccountRepository
{
    private readonly LedgerlyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EfAccountRepository(LedgerlyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<List<Account>> GetAllAsync(CancellationToken ct = default)
        => _db.Accounts.AsNoTracking().OrderBy(a => a.Name).ToListAsync(ct);

    public Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task AddAsync(Account account, CancellationToken ct = default)
    {
        account.UserId = _currentUser.UserId;
        return _db.Accounts.AddAsync(account, ct).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
