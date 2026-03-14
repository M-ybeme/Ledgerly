using Ledgerly.Domain.Income;

namespace Ledgerly.Application.Income;

public interface IIncomeSourceRepository
{
    Task<List<IncomeSource>> GetAllAsync(CancellationToken ct = default);
    Task<IncomeSource?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(IncomeSource source, CancellationToken ct = default);
    Task DeleteAsync(IncomeSource source, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
