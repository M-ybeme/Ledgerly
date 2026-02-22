using Ledgerly.Application.Auth;
using Ledgerly.Application.Scenarios;
using Ledgerly.Domain.Scenarios;
using Ledgerly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Scenarios;

public sealed class EfScenarioRepository : IScenarioRepository
{
    private readonly LedgerlyDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EfScenarioRepository(LedgerlyDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<List<Scenario>> GetAllAsync(CancellationToken ct = default)
        => _db.Scenarios
            .AsNoTracking()
            .Include(s => s.DebtAccounts)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public Task<Scenario?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Scenarios
            .Include(s => s.DebtAccounts)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task AddAsync(Scenario scenario, CancellationToken ct = default)
    {
        scenario.UserId = _currentUser.UserId;
        return _db.Scenarios.AddAsync(scenario, ct).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
