using Ledgerly.Domain.Scenarios;

namespace Ledgerly.Application.Scenarios;

public interface IActualPaymentRepository
{
    Task<List<ActualPayment>> GetByScenarioAsync(Guid scenarioId, CancellationToken ct = default);
    Task<ActualPayment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(ActualPayment payment, CancellationToken ct = default);
    Task DeleteAsync(ActualPayment payment, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
