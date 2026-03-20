namespace Ledgerly.Contracts.Auth;

public sealed record AuthTokenDto(string Token, string Email, DateTime ExpiresUtc, string? RefreshToken = null);
