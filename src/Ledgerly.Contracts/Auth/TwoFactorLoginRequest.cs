namespace Ledgerly.Contracts.Auth;

public sealed record TwoFactorLoginRequest(string Email, string Password, string Code);
