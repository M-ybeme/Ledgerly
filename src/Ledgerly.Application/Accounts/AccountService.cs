using Ledgerly.Domain.Accounts;
using Ledgerly.Contracts.Accounts;

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

    private static AccountDto ToDto(Account a) =>
        new(a.Id, a.Name, a.Type, a.CreatedUtc);
}