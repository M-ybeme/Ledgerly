using Ledgerly.Contracts.Budget;
using Ledgerly.Domain.Budget;

namespace Ledgerly.Application.Budget;

public sealed class SavingsGoalService
{
    private readonly ISavingsGoalRepository _repo;

    public SavingsGoalService(ISavingsGoalRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<SavingsGoalDto>> GetAllAsync(CancellationToken ct = default)
    {
        var goals = await _repo.GetAllAsync(ct);
        return goals.Select(ToDto).ToList();
    }

    public async Task<SavingsGoalDto> CreateAsync(CreateSavingsGoalRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Name is required.");
        if (req.TargetAmount <= 0)
            throw new ArgumentException("Target amount must be greater than zero.");
        if (req.CurrentAmount < 0)
            throw new ArgumentException("Current amount cannot be negative.");

        var goal = new SavingsGoal
        {
            Name = req.Name.Trim(),
            TargetAmount = req.TargetAmount,
            CurrentAmount = req.CurrentAmount,
            TargetDate = req.TargetDate,
            CreatedUtc = DateTime.UtcNow
        };

        await _repo.AddAsync(goal, ct);
        await _repo.SaveChangesAsync(ct);

        return ToDto(goal);
    }

    public async Task<SavingsGoalDto> UpdateAsync(Guid id, UpdateSavingsGoalRequest req, CancellationToken ct = default)
    {
        var goal = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Savings goal {id} not found.");

        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Name is required.");
        if (req.TargetAmount <= 0)
            throw new ArgumentException("Target amount must be greater than zero.");
        if (req.CurrentAmount < 0)
            throw new ArgumentException("Current amount cannot be negative.");

        goal.Name = req.Name.Trim();
        goal.TargetAmount = req.TargetAmount;
        goal.CurrentAmount = req.CurrentAmount;
        goal.TargetDate = req.TargetDate;

        await _repo.SaveChangesAsync(ct);

        return ToDto(goal);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var goal = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Savings goal {id} not found.");
        await _repo.DeleteAsync(goal, ct);
        await _repo.SaveChangesAsync(ct);
    }

    private static SavingsGoalDto ToDto(SavingsGoal g) =>
        new(g.Id, g.Name, g.TargetAmount, g.CurrentAmount, g.Progress, g.Remaining, g.TargetDate, g.CreatedUtc);
}
