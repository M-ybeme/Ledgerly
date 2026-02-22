using Ledgerly.Contracts.Budget;
using Ledgerly.Domain.Budget;

namespace Ledgerly.Application.Budget;

public sealed class TransactionService
{
    private readonly ITransactionRepository _repo;
    private readonly IBudgetCategoryRepository _categoryRepo;

    public TransactionService(ITransactionRepository repo, IBudgetCategoryRepository categoryRepo)
    {
        _repo = repo;
        _categoryRepo = categoryRepo;
    }

    public async Task<List<TransactionDto>> GetAllAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default)
    {
        var transactions = await _repo.GetAllAsync(from, to, ct);
        return [..transactions.Select(ToDto)];
    }

    public async Task<TransactionDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var transaction = await _repo.GetByIdAsync(id, ct);
        return transaction is null ? null : ToDto(transaction);
    }

    public async Task<List<TransactionDto>> GetByDateRangeAsync(DateOnly start, DateOnly end, CancellationToken ct = default)
    {
        var transactions = await _repo.GetByDateRangeAsync(start, end, ct);
        return [..transactions.Select(ToDto)];
    }

    public async Task<List<Transaction>> GetEntitiesByDateRangeAsync(DateOnly start, DateOnly end, CancellationToken ct = default)
        => await _repo.GetByDateRangeAsync(start, end, ct);

    public async Task<TransactionDto> CreateAsync(CreateTransactionRequest req, CancellationToken ct = default)
    {
        await ValidateAsync(req.Description, req.Amount, req.CategoryId, ct);

        var transaction = new Transaction
        {
            Description = req.Description.Trim(),
            Amount = req.Amount,
            Date = req.Date,
            CategoryId = req.CategoryId,
            Category = (await _categoryRepo.GetByIdAsync(req.CategoryId, ct))!,
            CreatedUtc = DateTime.UtcNow
        };

        await _repo.AddAsync(transaction, ct);
        await _repo.SaveChangesAsync(ct);

        return ToDto(transaction);
    }

    public async Task<TransactionDto> UpdateAsync(Guid id, UpdateTransactionRequest req, CancellationToken ct = default)
    {
        var transaction = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Transaction {id} not found.");

        await ValidateAsync(req.Description, req.Amount, req.CategoryId, ct);

        var category = await _categoryRepo.GetByIdAsync(req.CategoryId, ct)
            ?? throw new ArgumentException($"Category {req.CategoryId} not found.");

        transaction.Description = req.Description.Trim();
        transaction.Amount = req.Amount;
        transaction.Date = req.Date;
        transaction.CategoryId = req.CategoryId;
        transaction.Category = category;

        await _repo.UpdateAsync(transaction, ct);
        await _repo.SaveChangesAsync(ct);

        return ToDto(transaction);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var transaction = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Transaction {id} not found.");

        await _repo.DeleteAsync(transaction, ct);
        await _repo.SaveChangesAsync(ct);
    }

    private async Task ValidateAsync(string description, decimal amount, Guid categoryId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.");
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");
        var category = await _categoryRepo.GetByIdAsync(categoryId, ct);
        if (category is null)
            throw new ArgumentException($"Category {categoryId} not found.");
    }

    private static TransactionDto ToDto(Transaction t) =>
        new(t.Id, t.Description, t.Amount, t.Date,
            t.CategoryId, t.Category.Name, t.Category.Type, t.CreatedUtc);
}
