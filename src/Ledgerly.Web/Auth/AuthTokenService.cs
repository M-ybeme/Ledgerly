using Ledgerly.Contracts.Auth;

namespace Ledgerly.Web.Auth;

public sealed class AuthTokenService
{
    public AuthTokenDto? Token { get; private set; }
    public bool IsAuthenticated => Token is not null;
    public bool IsInitialized { get; private set; }
    public event Action? OnChange;

    public void SetToken(AuthTokenDto dto)
    {
        Token = dto;
        IsInitialized = true;
        OnChange?.Invoke();
    }

    public void ClearToken()
    {
        Token = null;
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
