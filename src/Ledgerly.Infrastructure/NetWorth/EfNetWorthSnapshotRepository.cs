using Ledgerly.Application.NetWorth;
using Ledgerly.Domain.NetWorth;
using Ledgerly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.NetWorth;

public sealed class EfNetWorthSnapshotRepository : INetWorthSnapshotRepository
{
    private readonly LedgerlyDbContext _db;

    public EfNetWorthSnapshotRepository(LedgerlyDbContext db) => _db = db;

    public async Task<List<NetWorthSnapshot>> GetRecentAsync(int months, CancellationToken ct = default)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.Today).AddMonths(-months);
        return await _db.NetWorthSnapshots
            .Where(s => s.Month >= cutoff)
            .OrderBy(s => s.Month)
            .Take(months)
            .ToListAsync(ct);
    }
}
