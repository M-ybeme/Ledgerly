using Ledgerly.Application.Credit;
using Ledgerly.Domain.Credit;
using Ledgerly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Credit;

public sealed class EfCreditProfileRepository : ICreditProfileRepository
{
    private readonly LedgerlyDbContext _db;

    public EfCreditProfileRepository(LedgerlyDbContext db) => _db = db;

    public Task<CreditProfile?> GetByScenarioAsync(Guid scenarioId, CancellationToken ct = default)
        => _db.CreditProfiles
            .Include(p => p.Accounts)
            .FirstOrDefaultAsync(p => p.ScenarioId == scenarioId, ct);

    public async Task AddAsync(CreditProfile profile, CancellationToken ct = default)
        => await _db.CreditProfiles.AddAsync(profile, ct);

    public Task DeleteAsync(CreditProfile profile, CancellationToken ct = default)
    {
        _db.CreditProfiles.Remove(profile);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
