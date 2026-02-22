using Ledgerly.Domain.Accounts;

namespace Ledgerly.Contracts.Accounts;

public sealed record CreateAccountRequest(string Name, AccountType Type);