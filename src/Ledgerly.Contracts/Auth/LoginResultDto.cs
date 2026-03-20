namespace Ledgerly.Contracts.Auth;

public sealed record LoginResultDto(
    bool RequiresTwoFactor,
    string? Token,
    string? Email,
    DateTime ExpiresUtc,
    string? RefreshToken = null);
