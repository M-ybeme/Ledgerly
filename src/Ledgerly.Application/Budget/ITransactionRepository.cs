using Ledgerly.Domain.Budget;

namespace Ledgerly.Application.Budget;

public interface ITransactionRepository
{
    Task<List<Transaction>> GetAllAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default);
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Transaction>> GetByDateRangeAsync(DateOnly start, DateOnly end, CancellationToken ct = default);
    Task AddAsync(Transaction transaction, CancellationToken ct = default);
    Task UpdateAsync(Transaction transaction, CancellationToken ct = default);
    Task DeleteAsync(Transaction transaction, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
