using Ledgerly.Application.Accounts;
using Ledgerly.Contracts.Income;
using Ledgerly.Domain.Income;

namespace Ledgerly.Application.Income;

public sealed class IncomeSourceService
{
    private readonly IIncomeSourceRepository _repo;
    private readonly IAccountRepository _accounts;

    public IncomeSourceService(IIncomeSourceRepository repo, IAccountRepository accounts)
    {
        _repo = repo;
        _accounts = accounts;
    }

    public async Task<List<IncomeSourceDto>> GetAllAsync(CancellationToken ct = default)
    {
        var sources = await _repo.GetAllAsync(ct);
        var accountNames = await BuildAccountNameMapAsync(sources, ct);
        return sources.Select(s => ToDto(s, accountNames)).ToList();
    }

    public async Task<IncomeSourceDto> CreateAsync(CreateIncomeSourceRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Name is required.");
        if (req.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        var source = new IncomeSource
        {
            Name = req.Name.Trim(),
            Amount = req.Amount,
            Frequency = req.Frequency,
            AccountId = req.AccountId,
            CreatedUtc = DateTime.UtcNow
        };

        await _repo.AddAsync(source, ct);
        await _repo.SaveChangesAsync(ct);

        var accountNames = await BuildAccountNameMapAsync([source], ct);
        return ToDto(source, accountNames);
    }

    public async Task<IncomeSourceDto> UpdateAsync(Guid id, UpdateIncomeSourceRequest req, CancellationToken ct = default)
    {
        var source = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Income source {id} not found.");

        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Name is required.");
        if (req.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        source.Name = req.Name.Trim();
        source.Amount = req.Amount;
        source.Frequency = req.Frequency;
        source.AccountId = req.AccountId;

        await _repo.SaveChangesAsync(ct);

        var accountNames = await BuildAccountNameMapAsync([source], ct);
        return ToDto(source, accountNames);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var source = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Income source {id} not found.");
        await _repo.DeleteAsync(source, ct);
        await _repo.SaveChangesAsync(ct);
    }

    private async Task<Dictionary<Guid, string>> BuildAccountNameMapAsync(
        IEnumerable<IncomeSource> sources, CancellationToken ct)
    {
        var ids = sources.Where(s => s.AccountId.HasValue).Select(s => s.AccountId!.Value).Distinct().ToList();
        if (ids.Count == 0) return [];
        var all = await _accounts.GetAllAsync(ct);
        return all.Where(a => ids.Contains(a.Id)).ToDictionary(a => a.Id, a => a.Name);
    }

    private static IncomeSourceDto ToDto(IncomeSource s, Dictionary<Guid, string> accountNames) =>
        new(s.Id, s.Name, s.Amount, s.Frequency, s.MonthlyAmount,
            s.AccountId,
            s.AccountId.HasValue && accountNames.TryGetValue(s.AccountId.Value, out var n) ? n : null,
            s.CreatedUtc);
}
