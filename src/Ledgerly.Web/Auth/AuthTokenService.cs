using Ledgerly.Contracts.Auth;

namespace Ledgerly.Web.Auth;

public sealed class AuthTokenService
{
    public AuthTokenDto? Token { get; private set; }
    public string? RefreshToken { get; private set; }
    public bool IsAuthenticated => Token is not null;
    public bool IsInitialized { get; private set; }
    public event Action? OnChange;

    public void SetToken(AuthTokenDto dto, string? refreshToken = null)
    {
        Token = dto;
        if (refreshToken is not null)
            RefreshToken = refreshToken;
        IsInitialized = true;
        OnChange?.Invoke();
    }

    public void ClearToken()
    {
        Token = null;
        RefreshToken = null;
        IsInitialized = true;
        OnChange?.Invoke();
    }

    /// <summary>Called when localStorage has been checked but no token was found.</summary>
    public void MarkInitialized()
    {
        IsInitialized = true;
        OnChange?.Invoke();
    }
}
