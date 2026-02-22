using Ledgerly.Domain.Scenarios;

namespace Ledgerly.Application.Scenarios;

public interface IScenarioRepository
{
    Task<List<Scenario>> GetAllAsync(CancellationToken ct = default);
    Task<Scenario?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Scenario scenario, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
