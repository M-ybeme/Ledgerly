using Ledgerly.Contracts.Scenarios;
using Ledgerly.Domain.Scenarios;

namespace Ledgerly.Application.Scenarios;

public sealed class ActualPaymentService
{
    private readonly IActualPaymentRepository _repo;
    private readonly IScenarioRepository _scenarioRepo;

    public ActualPaymentService(IActualPaymentRepository repo, IScenarioRepository scenarioRepo)
    {
        _repo = repo;
        _scenarioRepo = scenarioRepo;
    }

    public async Task<List<ActualPaymentDto>> GetByScenarioAsync(Guid scenarioId, CancellationToken ct = default)
    {
        var payments = await _repo.GetByScenarioAsync(scenarioId, ct);
        return [..payments.Select(ToDto)];
    }

    public Task<List<ActualPayment>> GetEntitiesByScenarioAsync(Guid scenarioId, CancellationToken ct = default)
        => _repo.GetByScenarioAsync(scenarioId, ct);

    public async Task<ActualPaymentDto> LogPaymentAsync(Guid scenarioId, LogPaymentRequest req, CancellationToken ct = default)
    {
        var scenario = await _scenarioRepo.GetByIdAsync(scenarioId, ct)
            ?? throw new KeyNotFoundException($"Scenario {scenarioId} not found.");

        if (req.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        var debtInScenario = scenario.DebtAccounts.Any(d => d.Id == req.DebtAccountId);
        if (!debtInScenario)
            throw new ArgumentException($"Debt account {req.DebtAccountId} is not part of this scenario.");

        var payment = new ActualPayment
        {
            Id = Guid.NewGuid(),
            ScenarioId = scenarioId,
            DebtAccountId = req.DebtAccountId,
            PaymentDate = req.PaymentDate,
            Amount = req.Amount,
            CreatedUtc = DateTime.UtcNow
        };

        await _repo.AddAsync(payment, ct);
        await _repo.SaveChangesAsync(ct);
        return ToDto(payment);
    }

    public async Task DeleteAsync(Guid scenarioId, Guid paymentId, CancellationToken ct = default)
    {
        var payment = await _repo.GetByIdAsync(paymentId, ct)
            ?? throw new KeyNotFoundException($"Payment {paymentId} not found.");

        if (payment.ScenarioId != scenarioId)
            throw new KeyNotFoundException($"Payment {paymentId} not found in scenario {scenarioId}.");

        await _repo.DeleteAsync(payment, ct);
        await _repo.SaveChangesAsync(ct);
    }

    private static ActualPaymentDto ToDto(ActualPayment p) =>
        new(p.Id, p.ScenarioId, p.DebtAccountId, p.PaymentDate, p.Amount, p.CreatedUtc);
}
