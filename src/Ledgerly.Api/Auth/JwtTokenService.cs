using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
    private readonly int _expiryHours;

    public JwtTokenService(IConfiguration config)
    {
        _secret = config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        _issuer = config["Jwt:Issuer"] ?? "Ledgerly";
        _audience = config["Jwt:Audience"] ?? "LedgerlyUsers";
        _expiryHours = int.TryParse(config["Jwt:ExpiryHours"], out var h) ? h : 24;
    }

    public AuthTokenDto GenerateToken(ApplicationUser user)
    {
        var expiresUtc = DateTime.UtcNow.AddHours(_expiryHours);
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
}
