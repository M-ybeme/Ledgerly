namespace Ledgerly.Contracts.Auth;

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
