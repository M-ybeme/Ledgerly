using Ledgerly.Application.Accounts;
using Ledgerly.Application.Debts;
using Ledgerly.Contracts.NetWorth;
using Ledgerly.Domain.Accounts;

namespace Ledgerly.Application.NetWorth;

public sealed class NetWorthService
{
    private readonly IAccountRepository _accounts;
    private readonly IDebtAccountRepository _debts;
    private readonly INetWorthSnapshotRepository _snapshots;

    public NetWorthService(
        IAccountRepository accounts,
        IDebtAccountRepository debts,
        INetWorthSnapshotRepository snapshots)
    {
        _accounts = accounts;
        _debts = debts;
        _snapshots = snapshots;
    }

    public async Task<NetWorthSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var accounts = await _accounts.GetAllAsync(ct);
        var debtAccounts = await _debts.GetAllAsync(ct);
        var history = await _snapshots.GetRecentAsync(12, ct);

        var assets = accounts
            .Where(a => a.Type != AccountType.CreditCard)
            .Sum(a => a.Balance);

        var liabilities = debtAccounts.Sum(d => d.Balance);

        var historyDtos = history
            .OrderBy(s => s.Month)
            .Select(s => new NetWorthSnapshotDto(s.Month, s.AssetsTotal, s.LiabilitiesTotal, s.NetWorth))
            .ToList();

        return new NetWorthSummaryDto(assets, liabilities, assets - liabilities, historyDtos);
    }
}
