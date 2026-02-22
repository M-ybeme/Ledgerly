using Ledgerly.Application.Scenarios;
using Ledgerly.Contracts.Credit;
using Ledgerly.Domain.Credit;

namespace Ledgerly.Application.Credit;

public sealed class CreditProfileService
{
    private readonly ICreditProfileRepository _repo;
    private readonly IScenarioRepository _scenarioRepo;

    public CreditProfileService(ICreditProfileRepository repo, IScenarioRepository scenarioRepo)
    {
        _repo = repo;
        _scenarioRepo = scenarioRepo;
    }

    public async Task<CreditProfileDto?> GetByScenarioAsync(Guid scenarioId, CancellationToken ct = default)
    {
        var profile = await _repo.GetByScenarioAsync(scenarioId, ct);
        return profile is null ? null : ToDto(profile);
    }

    public Task<CreditProfile?> GetEntityByScenarioAsync(Guid scenarioId, CancellationToken ct = default)
        => _repo.GetByScenarioAsync(scenarioId, ct);

    public async Task<CreditProfileDto> UpsertAsync(Guid scenarioId, CreateCreditProfileRequest req, CancellationToken ct = default)
    {
        var scenario = await _scenarioRepo.GetByIdAsync(scenarioId, ct)
            ?? throw new KeyNotFoundException($"Scenario {scenarioId} not found.");

        if (req.CurrentScoreRangeLow < 300 || req.CurrentScoreRangeLow > 850)
            throw new ArgumentException("Score range low must be between 300 and 850.");
        if (req.CurrentScoreRangeHigh < 300 || req.CurrentScoreRangeHigh > 850)
            throw new ArgumentException("Score range high must be between 300 and 850.");
        if (req.CurrentScoreRangeLow >= req.CurrentScoreRangeHigh)
            throw new ArgumentException("Score range low must be less than score range high.");

        foreach (var a in req.Accounts)
        {
            if (a.CreditLimit <= 0)
                throw new ArgumentException($"Credit limit must be greater than zero for account '{a.Name}'.");
            if (a.AgeMonths < 0)
                throw new ArgumentException($"Age months cannot be negative for account '{a.Name}'.");
            if (a.DebtAccountId.HasValue)
            {
                var debtInScenario = scenario.DebtAccounts.Any(d => d.Id == a.DebtAccountId.Value);
                if (!debtInScenario)
                    throw new ArgumentException($"Debt account {a.DebtAccountId} is not part of scenario {scenarioId}.");
            }
            else if (string.IsNullOrWhiteSpace(a.Name))
            {
                throw new ArgumentException("Name is required for standalone credit accounts.");
            }
        }

        // Delete existing profile if present (cascade removes its CreditAccountProfiles)
        var existing = await _repo.GetByScenarioAsync(scenarioId, ct);
        if (existing is not null)
        {
            await _repo.DeleteAsync(existing, ct);
            await _repo.SaveChangesAsync(ct);
        }

        var profile = new CreditProfile
        {
            Id = Guid.NewGuid(),
            ScenarioId = scenarioId,
            CurrentScoreRangeLow = req.CurrentScoreRangeLow,
            CurrentScoreRangeHigh = req.CurrentScoreRangeHigh,
            PaymentHistoryIsClean = req.PaymentHistoryIsClean,
            CreatedUtc = DateTime.UtcNow,
            Accounts = req.Accounts.Select(a => new CreditAccountProfile
            {
                Id = Guid.NewGuid(),
                DebtAccountId = a.DebtAccountId,
                Name = a.DebtAccountId.HasValue
                    ? (scenario.DebtAccounts.FirstOrDefault(d => d.Id == a.DebtAccountId.Value)?.Name ?? a.Name)
                    : a.Name.Trim(),
                CreditLimit = a.CreditLimit,
                CurrentBalance = a.CurrentBalance,
                AgeMonths = a.AgeMonths,
                AccountType = a.AccountType
            }).ToList()
        };

        await _repo.AddAsync(profile, ct);
        await _repo.SaveChangesAsync(ct);
        return ToDto(profile);
    }

    public async Task DeleteAsync(Guid scenarioId, CancellationToken ct = default)
    {
        var profile = await _repo.GetByScenarioAsync(scenarioId, ct)
            ?? throw new KeyNotFoundException($"No credit profile found for scenario {scenarioId}.");
        await _repo.DeleteAsync(profile, ct);
        await _repo.SaveChangesAsync(ct);
    }

    private static CreditProfileDto ToDto(CreditProfile p) =>
        new(p.Id, p.ScenarioId,
            p.CurrentScoreRangeLow, p.CurrentScoreRangeHigh,
            p.PaymentHistoryIsClean,
            p.Accounts.Select(a => new CreditAccountProfileDto(
                a.Id, a.DebtAccountId, a.Name,
                a.CreditLimit, a.CurrentBalance, a.AgeMonths, a.AccountType)).ToList(),
            p.CreatedUtc);
}
