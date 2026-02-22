using Ledgerly.Contracts.Auth;

namespace Ledgerly.Web.Auth;

public sealed class AuthTokenService
{
    public AuthTokenDto? Token { get; private set; }
    public bool IsAuthenticated => Token is not null;
    public event Action? OnChange;

    public void SetToken(AuthTokenDto dto)
    {
        Token = dto;
        OnChange?.Invoke();
    }

    public void ClearToken()
    {
        Token = null;
        OnChange?.Invoke();
    }
}
