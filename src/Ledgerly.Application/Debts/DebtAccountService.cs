using Ledgerly.Contracts.Debts;
using Ledgerly.Domain.Debts;

namespace Ledgerly.Application.Debts;

public sealed class DebtAccountService
{
    private readonly IDebtAccountRepository _repo;

    public DebtAccountService(IDebtAccountRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<DebtAccountDto>> GetAllAsync(CancellationToken ct = default)
    {
        var debts = await _repo.GetAllAsync(ct);
        return [..debts.Select(ToDto)];
    }

    public async Task<DebtAccountDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var debt = await _repo.GetByIdAsync(id, ct);
        return debt is null ? null : ToDto(debt);
    }

    public async Task<DebtAccountDto> CreateAsync(CreateDebtAccountRequest req, CancellationToken ct = default)
    {
        Validate(req.Name, req.Balance, req.AnnualInterestRate, req.MinimumPayment);

        var debt = new DebtAccount
        {
            Name = req.Name.Trim(),
            Balance = req.Balance,
            AnnualInterestRate = req.AnnualInterestRate,
            MinimumPayment = req.MinimumPayment,
            CreatedUtc = DateTime.UtcNow
        };

        await _repo.AddAsync(debt, ct);
        await _repo.SaveChangesAsync(ct);

        return ToDto(debt);
    }

    public async Task<DebtAccountDto> UpdateAsync(Guid id, UpdateDebtAccountRequest req, CancellationToken ct = default)
    {
        var debt = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Debt account {id} not found.");

        Validate(req.Name, req.Balance, req.AnnualInterestRate, req.MinimumPayment);

        debt.Name = req.Name.Trim();
        debt.Balance = req.Balance;
        debt.AnnualInterestRate = req.AnnualInterestRate;
        debt.MinimumPayment = req.MinimumPayment;

        await _repo.UpdateAsync(debt, ct);
        await _repo.SaveChangesAsync(ct);

        return ToDto(debt);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var debt = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Debt account {id} not found.");

        await _repo.DeleteAsync(debt, ct);
        await _repo.SaveChangesAsync(ct);
    }

    private static void Validate(string name, decimal balance, decimal annualInterestRate, decimal minimumPayment)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.");
        if (balance < 0)
            throw new ArgumentException("Balance cannot be negative.");
        if (annualInterestRate <= 0 || annualInterestRate > 1)
            throw new ArgumentException("Annual interest rate must be between 0 and 1 (e.g. 0.1799 for 17.99%).");
        if (minimumPayment <= 0)
            throw new ArgumentException("Minimum payment must be greater than zero.");
    }

    private static DebtAccountDto ToDto(DebtAccount d) =>
        new(d.Id, d.Name, d.Balance, d.AnnualInterestRate, d.MinimumPayment, d.CreatedUtc);
}
