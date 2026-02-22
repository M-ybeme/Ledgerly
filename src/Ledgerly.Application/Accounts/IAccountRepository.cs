using Ledgerly.Domain.Accounts;

namespace Ledgerly.Application.Accounts;

public interface IAccountRepository
{
    Task<List<Account>> GetAllAsync(CancellationToken ct = default);
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Account account, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}