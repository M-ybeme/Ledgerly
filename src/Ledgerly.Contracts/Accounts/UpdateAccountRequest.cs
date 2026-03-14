using Ledgerly.Domain.Accounts;

namespace Ledgerly.Contracts.Accounts;

public sealed record UpdateAccountRequest(string Name, AccountType Type);
