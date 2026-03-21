using Ledgerly.Domain.NetWorth;

namespace Ledgerly.Application.NetWorth;

public interface INetWorthSnapshotRepository
{
    Task<List<NetWorthSnapshot>> GetRecentAsync(int months, CancellationToken ct = default);
}
