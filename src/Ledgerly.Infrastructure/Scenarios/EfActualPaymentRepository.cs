using Ledgerly.Application.Scenarios;
using Ledgerly.Domain.Scenarios;
using Ledgerly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Scenarios;

public sealed class EfActualPaymentRepository : IActualPaymentRepository
{
    private readonly LedgerlyDbContext _db;

    public EfActualPaymentRepository(LedgerlyDbContext db) => _db = db;

    public Task<List<ActualPayment>> GetByScenarioAsync(Guid scenarioId, CancellationToken ct = default) =>
        _db.ActualPayments
            .AsNoTracking()
            .Where(p => p.ScenarioId == scenarioId)
            .OrderBy(p => p.PaymentDate)
            .ToListAsync(ct);

    public Task<ActualPayment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.ActualPayments.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task AddAsync(ActualPayment payment, CancellationToken ct = default) =>
        _db.ActualPayments.AddAsync(payment, ct).AsTask();

    public Task DeleteAsync(ActualPayment payment, CancellationToken ct = default)
    {
        _db.ActualPayments.Remove(payment);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
