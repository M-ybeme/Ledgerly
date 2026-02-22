using Ledgerly.Contracts.Budget;
using Ledgerly.Domain.Budget;

namespace Ledgerly.Application.Budget;

public sealed class BudgetPlanService
{
    private readonly IBudgetPlanRepository _repo;
    private readonly IBudgetCategoryRepository _categoryRepo;

    public BudgetPlanService(IBudgetPlanRepository repo, IBudgetCategoryRepository categoryRepo)
    {
        _repo = repo;
        _categoryRepo = categoryRepo;
    }

    public async Task<List<BudgetPlanDto>> GetAllAsync(CancellationToken ct = default)
    {
        var plans = await _repo.GetAllAsync(ct);
        return [..plans.Select(ToDto)];
    }

    public async Task<BudgetPlanDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var plan = await _repo.GetByIdAsync(id, ct);
        return plan is null ? null : ToDto(plan);
    }

    public Task<BudgetPlan?> GetPlanEntityAsync(Guid id, CancellationToken ct = default)
        => _repo.GetByIdAsync(id, ct);

    public async Task<BudgetPlanDto> CreateAsync(CreateBudgetPlanRequest req, CancellationToken ct = default)
    {
        await ValidateRequest(req.Name, req.StartDate, req.EndDate, req.Lines, ct);

        var plan = new BudgetPlan
        {
            Name = req.Name.Trim(),
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            CreatedUtc = DateTime.UtcNow
        };

        foreach (var lineReq in req.Lines)
        {
            var category = await _categoryRepo.GetByIdAsync(lineReq.CategoryId, ct)
                ?? throw new ArgumentException($"Category {lineReq.CategoryId} not found.");

            plan.Lines.Add(new BudgetPlanLine
            {
                CategoryId = lineReq.CategoryId,
                Category = category,
                PlannedAmount = lineReq.PlannedAmount
            });
        }

        await _repo.AddAsync(plan, ct);
        await _repo.SaveChangesAsync(ct);

        return ToDto(plan);
    }

    public async Task<BudgetPlanDto> UpdateAsync(Guid id, UpdateBudgetPlanRequest req, CancellationToken ct = default)
    {
        var plan = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Budget plan {id} not found.");

        await ValidateRequest(req.Name, req.StartDate, req.EndDate, req.Lines, ct);

        plan.Name = req.Name.Trim();
        plan.StartDate = req.StartDate;
        plan.EndDate = req.EndDate;
        plan.Lines.Clear();

        foreach (var lineReq in req.Lines)
        {
            var category = await _categoryRepo.GetByIdAsync(lineReq.CategoryId, ct)
                ?? throw new ArgumentException($"Category {lineReq.CategoryId} not found.");

            plan.Lines.Add(new BudgetPlanLine
            {
                CategoryId = lineReq.CategoryId,
                Category = category,
                PlannedAmount = lineReq.PlannedAmount
            });
        }

        await _repo.SaveChangesAsync(ct);

        return ToDto(plan);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var plan = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Budget plan {id} not found.");

        await _repo.DeleteAsync(plan, ct);
        await _repo.SaveChangesAsync(ct);
    }

    private static async Task ValidateRequest(
        string name, DateOnly startDate, DateOnly endDate,
        List<BudgetPlanLineRequest> lines, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.");
        if (startDate > endDate)
            throw new ArgumentException("Start date must be on or before end date.");
        if (lines.Count == 0)
            throw new ArgumentException("At least one budget line is required.");
        foreach (var line in lines)
        {
            if (line.PlannedAmount <= 0)
                throw new ArgumentException("Planned amount must be greater than zero.");
        }
        await Task.CompletedTask;
    }

    private static BudgetPlanDto ToDto(BudgetPlan p) =>
        new(p.Id, p.Name, p.StartDate, p.EndDate,
            p.Lines.Select(l => new BudgetPlanLineDto(
                l.Id, l.CategoryId, l.Category.Name, l.Category.Type, l.PlannedAmount)).ToList(),
            p.CreatedUtc);
}
