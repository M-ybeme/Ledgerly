using Ledgerly.Contracts.Budget;
using Ledgerly.Domain.Budget;

namespace Ledgerly.Application.Budget;

public sealed class BudgetCategoryService
{
    private readonly IBudgetCategoryRepository _repo;
    private readonly ITransactionRepository _transactionRepo;

    public BudgetCategoryService(IBudgetCategoryRepository repo, ITransactionRepository transactionRepo)
    {
        _repo = repo;
        _transactionRepo = transactionRepo;
    }

    public async Task<List<BudgetCategoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var categories = await _repo.GetAllAsync(ct);
        return [..categories.Select(ToDto)];
    }

    public async Task<BudgetCategoryDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var category = await _repo.GetByIdAsync(id, ct);
        return category is null ? null : ToDto(category);
    }

    public async Task<BudgetCategoryDto> CreateAsync(CreateBudgetCategoryRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Name is required.");

        var category = new BudgetCategory
        {
            Name = req.Name.Trim(),
            Type = req.Type,
            CreatedUtc = DateTime.UtcNow
        };

        await _repo.AddAsync(category, ct);
        await _repo.SaveChangesAsync(ct);

        return ToDto(category);
    }

    public async Task<BudgetCategoryDto> UpdateAsync(Guid id, UpdateBudgetCategoryRequest req, CancellationToken ct = default)
    {
        var category = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Budget category {id} not found.");

        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Name is required.");

        category.Name = req.Name.Trim();
        category.Type = req.Type;

        await _repo.UpdateAsync(category, ct);
        await _repo.SaveChangesAsync(ct);

        return ToDto(category);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Budget category {id} not found.");

        var transactions = await _transactionRepo.GetAllAsync(ct: ct);
        if (transactions.Any(t => t.CategoryId == id))
            throw new InvalidOperationException("Cannot delete a category that has existing transactions.");

        await _repo.DeleteAsync(category, ct);
        await _repo.SaveChangesAsync(ct);
    }

    private static BudgetCategoryDto ToDto(BudgetCategory c) =>
        new(c.Id, c.Name, c.Type, c.CreatedUtc);
}
