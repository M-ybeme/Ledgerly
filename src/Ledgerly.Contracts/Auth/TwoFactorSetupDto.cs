namespace Ledgerly.Contracts.Auth;

public sealed record TwoFactorSetupDto(string AuthenticatorUri, string SecretKey, string QrCodeBase64);
