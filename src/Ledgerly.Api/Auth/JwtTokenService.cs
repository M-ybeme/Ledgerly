using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ledgerly.Contracts.Auth;
using Ledgerly.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Ledgerly.Api.Auth;

public sealed class JwtTokenService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;

    public JwtTokenService(IConfiguration config)
    {
        _secret = config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        _issuer = config["Jwt:Issuer"] ?? "Ledgerly";
        _audience = config["Jwt:Audience"] ?? "LedgerlyUsers";
        // ExpiryMinutes preferred; fall back to ExpiryHours for backwards-compat
        if (int.TryParse(config["Jwt:ExpiryMinutes"], out var m))
            _expiryMinutes = m;
        else if (int.TryParse(config["Jwt:ExpiryHours"], out var h))
            _expiryMinutes = h * 60;
        else
            _expiryMinutes = 15;
    }

    public AuthTokenDto GenerateToken(ApplicationUser user)
    {
        var expiresUtc = DateTime.UtcNow.AddMinutes(_expiryMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expiresUtc,
            signingCredentials: creds);

        return new AuthTokenDto(
            new JwtSecurityTokenHandler().WriteToken(token),
            user.Email!,
            expiresUtc);
    }

    /// <summary>Generates a cryptographically random refresh token string (not stored — caller stores the hash).</summary>
    public static (string Raw, string Hash) GenerateRefreshToken()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        return (raw, hash);
    }
}
