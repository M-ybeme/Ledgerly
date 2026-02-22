using Ledgerly.Application.Debts;
using Ledgerly.Contracts.Debts;
using Ledgerly.Contracts.Scenarios;
using Ledgerly.Domain.Scenarios;

namespace Ledgerly.Application.Scenarios;

public sealed class ScenarioService
{
    private readonly IScenarioRepository _repo;
    private readonly IDebtAccountRepository _debtRepo;

    public ScenarioService(IScenarioRepository repo, IDebtAccountRepository debtRepo)
    {
        _repo = repo;
        _debtRepo = debtRepo;
    }

    public async Task<List<ScenarioDto>> GetAllAsync(CancellationToken ct = default)
    {
        var scenarios = await _repo.GetAllAsync(ct);
        return [..scenarios.Select(ToDto)];
    }

    public async Task<ScenarioDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var scenario = await _repo.GetByIdAsync(id, ct);
        return scenario is null ? null : ToDto(scenario);
    }

    public Task<Scenario?> GetScenarioEntityAsync(Guid id, CancellationToken ct = default)
        => _repo.GetByIdAsync(id, ct);

    public async Task<ScenarioDto> CreateAsync(CreateScenarioRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Name is required.");
        if (req.ExtraMonthlyPayment < 0)
            throw new ArgumentException("Extra monthly payment cannot be negative.");
        if (req.DebtAccountIds.Count == 0)
            throw new ArgumentException("At least one debt account must be selected.");

        var debtAccounts = new List<Domain.Debts.DebtAccount>();
        foreach (var debtId in req.DebtAccountIds)
        {
            var debt = await _debtRepo.GetByIdAsync(debtId, ct)
                ?? throw new ArgumentException($"Debt account {debtId} not found.");
            debtAccounts.Add(debt);
        }

        var scenario = new Scenario
        {
            Name = req.Name.Trim(),
            ExtraMonthlyPayment = req.ExtraMonthlyPayment,
            Strategy = req.Strategy,
            CreatedUtc = DateTime.UtcNow,
            DebtAccounts = debtAccounts
        };

        await _repo.AddAsync(scenario, ct);
        await _repo.SaveChangesAsync(ct);

        return ToDto(scenario);
    }

    public async Task<ScenarioDto> DuplicateAsync(Guid id, CancellationToken ct = default)
    {
        var original = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Scenario {id} not found.");

        var copy = new Scenario
        {
            Name = original.Name + " (copy)",
            ExtraMonthlyPayment = original.ExtraMonthlyPayment,
            Strategy = original.Strategy,
            CreatedUtc = DateTime.UtcNow,
            DebtAccounts = [..original.DebtAccounts]
        };

        await _repo.AddAsync(copy, ct);
        await _repo.SaveChangesAsync(ct);

        return ToDto(copy);
    }

    private static ScenarioDto ToDto(Scenario s) =>
        new(s.Id, s.Name, s.ExtraMonthlyPayment, s.Strategy,
            s.DebtAccounts.Select(d => new DebtAccountDto(
                d.Id, d.Name, d.Balance, d.AnnualInterestRate, d.MinimumPayment, d.CreatedUtc)).ToList(),
            s.CreatedUtc);
}
