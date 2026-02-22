using Ledgerly.Domain.Accounts;

namespace Ledgerly.Contracts.Accounts;

public sealed record AccountDto(Guid Id, string Name, AccountType Type, DateTime CreatedUtc);