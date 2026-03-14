using Ledgerly.Contracts.Budget;
using Ledgerly.Domain.Budget;

namespace Ledgerly.Application.Budget;

public sealed class MonthlyBudgetService
{
    private readonly IMonthlyBudgetRepository _repo;
    private readonly IBudgetCategoryRepository _categories;
    private readonly ITransactionRepository _transactions;

    public MonthlyBudgetService(
        IMonthlyBudgetRepository repo,
        IBudgetCategoryRepository categories,
        ITransactionRepository transactions)
    {
        _repo = repo;
        _categories = categories;
        _transactions = transactions;
    }

    public async Task<List<MonthlyBudgetDto>> GetForMonthAsync(DateOnly month, CancellationToken ct = default)
    {
        var firstOfMonth = new DateOnly(month.Year, month.Month, 1);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);

        var budgets = await _repo.GetForMonthAsync(firstOfMonth, ct);
        var allCategories = await _categories.GetAllAsync(ct);
        var allTransactions = await _transactions.GetAllAsync(from: firstOfMonth, to: lastOfMonth, ct: ct);

        var catMap = allCategories.ToDictionary(c => c.Id, c => c.Name);

        // Actual spending per category this month
        var actuals = allTransactions
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        return budgets.Select(b =>
        {
            actuals.TryGetValue(b.CategoryId, out var actual);
            return new MonthlyBudgetDto(
                b.Id,
                b.Month,
                b.CategoryId,
                catMap.TryGetValue(b.CategoryId, out var name) ? name : "Unknown",
                b.Amount,
                actual,
                b.Amount - actual);
        }).OrderBy(b => b.CategoryName).ToList();
    }

    /// <summary>Upsert: create if missing for that month+category, update if it exists.</summary>
    public async Task<MonthlyBudgetDto> SetAsync(DateOnly month, SetMonthlyBudgetRequest req, CancellationToken ct = default)
    {
        if (req.Amount < 0)
            throw new ArgumentException("Amount cannot be negative.");

        var firstOfMonth = new DateOnly(month.Year, month.Month, 1);
        var existing = await _repo.GetAsync(firstOfMonth, req.CategoryId, ct);

        if (existing is null)
        {
            existing = new MonthlyBudget
            {
                Month = firstOfMonth,
                CategoryId = req.CategoryId,
                Amount = req.Amount
            };
            await _repo.AddAsync(existing, ct);
        }
        else
        {
            existing.Amount = req.Amount;
        }

        await _repo.SaveChangesAsync(ct);

        var cat = await _categories.GetByIdAsync(req.CategoryId, ct);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
        var transactions = await _transactions.GetAllAsync(from: firstOfMonth, to: lastOfMonth, ct: ct);
        var actual = transactions.Where(t => t.CategoryId == req.CategoryId).Sum(t => t.Amount);

        return new MonthlyBudgetDto(
            existing.Id, existing.Month, existing.CategoryId,
            cat?.Name ?? "Unknown",
            existing.Amount, actual, existing.Amount - actual);
    }

    public async Task DeleteAsync(DateOnly month, Guid categoryId, CancellationToken ct = default)
    {
        var firstOfMonth = new DateOnly(month.Year, month.Month, 1);
        var existing = await _repo.GetAsync(firstOfMonth, categoryId, ct)
            ?? throw new KeyNotFoundException("Monthly budget entry not found.");
        await _repo.DeleteAsync(existing, ct);
        await _repo.SaveChangesAsync(ct);
    }
}
