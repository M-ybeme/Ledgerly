using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace Ledgerly.Web.Auth;

public sealed class LedgerlyAuthStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly AuthTokenService _auth;

    public LedgerlyAuthStateProvider(AuthTokenService auth)
    {
        _auth = auth;
        _auth.OnChange += OnAuthChanged;
    }

    private void OnAuthChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_auth.Token is null)
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

        var claims = ParseTokenClaims(_auth.Token.Token);
        var identity = new ClaimsIdentity(claims, "jwt");
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }

    private static List<Claim> ParseTokenClaims(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2) return [];

        var payload = parts[1];
        var padded = payload.Replace('-', '+').Replace('_', '/');
        var mod = padded.Length % 4;
        if (mod == 2) padded += "==";
        else if (mod == 3) padded += "=";

        var bytes = Convert.FromBase64String(padded);
        var json = Encoding.UTF8.GetString(bytes);

        var claims = new List<Claim>();
        using var doc = JsonDocument.Parse(json);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var value = prop.Value.ValueKind == JsonValueKind.String
                ? prop.Value.GetString()!
                : prop.Value.ToString();

            if (prop.Name == "sub")
                claims.Add(new Claim(ClaimTypes.NameIdentifier, value));
            else if (prop.Name == "email")
                claims.Add(new Claim(ClaimTypes.Email, value));
            else
                claims.Add(new Claim(prop.Name, value));
        }
        return claims;
    }

    public void Dispose() => _auth.OnChange -= OnAuthChanged;
}
