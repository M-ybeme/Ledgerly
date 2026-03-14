using Ledgerly.Contracts.Budget;
using Ledgerly.Domain.Budget;

namespace Ledgerly.Application.Budget;

public sealed class PlannedExpenseService
{
    private readonly IPlannedExpenseRepository _repo;
    private readonly IBudgetCategoryRepository _categories;

    public PlannedExpenseService(IPlannedExpenseRepository repo, IBudgetCategoryRepository categories)
    {
        _repo = repo;
        _categories = categories;
    }

    public async Task<List<PlannedExpenseDto>> GetAllAsync(CancellationToken ct = default)
    {
        var expenses = await _repo.GetAllAsync(ct);
        var catNames = await BuildCategoryNameMapAsync(expenses, ct);
        return expenses.Select(e => ToDto(e, catNames)).ToList();
    }

    public async Task<PlannedExpenseDto> CreateAsync(CreatePlannedExpenseRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Description))
            throw new ArgumentException("Description is required.");
        if (req.PlannedAmount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        var expense = new PlannedExpense
        {
            Description = req.Description.Trim(),
            PlannedAmount = req.PlannedAmount,
            DueDate = req.DueDate,
            CategoryId = req.CategoryId,
            IsRecurring = req.IsRecurring,
            Priority = req.Priority
        };

        await _repo.AddAsync(expense, ct);
        await _repo.SaveChangesAsync(ct);

        var catNames = await BuildCategoryNameMapAsync([expense], ct);
        return ToDto(expense, catNames);
    }

    public async Task<PlannedExpenseDto> UpdateAsync(Guid id, UpdatePlannedExpenseRequest req, CancellationToken ct = default)
    {
        var expense = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Planned expense {id} not found.");

        if (string.IsNullOrWhiteSpace(req.Description))
            throw new ArgumentException("Description is required.");
        if (req.PlannedAmount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        expense.Description = req.Description.Trim();
        expense.PlannedAmount = req.PlannedAmount;
        expense.DueDate = req.DueDate;
        expense.CategoryId = req.CategoryId;
        expense.IsRecurring = req.IsRecurring;
        expense.Priority = req.Priority;

        await _repo.SaveChangesAsync(ct);

        var catNames = await BuildCategoryNameMapAsync([expense], ct);
        return ToDto(expense, catNames);
    }

    public async Task<PlannedExpenseDto> MarkPaidAsync(Guid id, MarkPaidRequest req, CancellationToken ct = default)
    {
        var expense = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Planned expense {id} not found.");

        expense.ActualAmount = req.ActualAmount;
        expense.PaidDate = req.PaidDate;

        await _repo.SaveChangesAsync(ct);

        var catNames = await BuildCategoryNameMapAsync([expense], ct);
        return ToDto(expense, catNames);
    }

    public async Task MarkUnpaidAsync(Guid id, CancellationToken ct = default)
    {
        var expense = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Planned expense {id} not found.");

        expense.ActualAmount = null;
        expense.PaidDate = null;

        await _repo.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var expense = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Planned expense {id} not found.");
        await _repo.DeleteAsync(expense, ct);
        await _repo.SaveChangesAsync(ct);
    }

    private async Task<Dictionary<Guid, string>> BuildCategoryNameMapAsync(
        IEnumerable<PlannedExpense> expenses, CancellationToken ct)
    {
        var ids = expenses.Where(e => e.CategoryId.HasValue).Select(e => e.CategoryId!.Value).Distinct().ToList();
        if (ids.Count == 0) return [];
        var all = await _categories.GetAllAsync(ct);
        return all.Where(c => ids.Contains(c.Id)).ToDictionary(c => c.Id, c => c.Name);
    }

    private static PlannedExpenseDto ToDto(PlannedExpense e, Dictionary<Guid, string> catNames) =>
        new(e.Id, e.Description, e.PlannedAmount, e.DueDate,
            e.CategoryId,
            e.CategoryId.HasValue && catNames.TryGetValue(e.CategoryId.Value, out var n) ? n : null,
            e.IsRecurring, e.IsPaid, e.ActualAmount, e.PaidDate, e.Priority);
}
