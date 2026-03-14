using Ledgerly.Contracts.Accounts;
using Ledgerly.Domain.Accounts;

namespace Ledgerly.Application.Accounts;

public sealed class AccountService
{
    private readonly IAccountRepository _repo;

    public AccountService(IAccountRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<AccountDto>> GetAllAsync(CancellationToken ct = default)
    {
        var accounts = await _repo.GetAllAsync(ct);
        return [..accounts.Select(ToDto)];
    }

    public async Task<AccountDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var a = await _repo.GetByIdAsync(id, ct);
        return a is null ? null : ToDto(a);
    }

    public async Task<AccountDto> CreateAsync(CreateAccountRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Name is required.");

        var account = new Account
        {
            Name = req.Name.Trim(),
            Type = req.Type,
            CreatedUtc = DateTime.UtcNow
        };

        await _repo.AddAsync(account, ct);
        await _repo.SaveChangesAsync(ct);

        return ToDto(account);
    }

    public async Task<AccountDto> UpdateAsync(Guid id, UpdateAccountRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Name is required.");

        var account = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Account {id} not found.");

        account.Name = req.Name.Trim();
        account.Type = req.Type;

        await _repo.SaveChangesAsync(ct);
        return ToDto(account);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await _repo.DeleteAsync(id, ct);
        await _repo.SaveChangesAsync(ct);
    }

    private static AccountDto ToDto(Account a) =>
        new(a.Id, a.Name, a.Type, a.CreatedUtc);
}