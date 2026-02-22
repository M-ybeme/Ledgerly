using Ledgerly.Domain.Credit;

namespace Ledgerly.Application.Credit;

public interface ICreditProfileRepository
{
    Task<CreditProfile?> GetByScenarioAsync(Guid scenarioId, CancellationToken ct = default);
    Task AddAsync(CreditProfile profile, CancellationToken ct = default);
    Task DeleteAsync(CreditProfile profile, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
