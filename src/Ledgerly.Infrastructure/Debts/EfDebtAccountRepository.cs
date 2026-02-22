using Ledgerly.Application.Auth;
using Ledgerly.Application.Debts;
using Ledgerly.Domain.Debts;
using Ledgerly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Debts;

public sealed class EfDebtAccountRepository : IDebtAccountRepository
{
    private readonly LedgerlyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EfDebtAccountRepository(LedgerlyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<List<DebtAccount>> GetAllAsync(CancellationToken ct = default)
        => _db.DebtAccounts.AsNoTracking().OrderBy(d => d.Name).ToListAsync(ct);

    public Task<DebtAccount?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.DebtAccounts.FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task AddAsync(DebtAccount debtAccount, CancellationToken ct = default)
    {
        debtAccount.UserId = _currentUser.UserId;
        return _db.DebtAccounts.AddAsync(debtAccount, ct).AsTask();
    }

    public Task UpdateAsync(DebtAccount debtAccount, CancellationToken ct = default)
    {
        _db.DebtAccounts.Update(debtAccount);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(DebtAccount debtAccount, CancellationToken ct = default)
    {
        _db.DebtAccounts.Remove(debtAccount);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
