using Ledgerly.Domain.Debts;

namespace Ledgerly.Application.Debts;

public interface IDebtAccountRepository
{
    Task<List<DebtAccount>> GetAllAsync(CancellationToken ct = default);
    Task<DebtAccount?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(DebtAccount debtAccount, CancellationToken ct = default);
    Task UpdateAsync(DebtAccount debtAccount, CancellationToken ct = default);
    Task DeleteAsync(DebtAccount debtAccount, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
