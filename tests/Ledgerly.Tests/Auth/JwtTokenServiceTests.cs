using System.IdentityModel.Tokens.Jwt;
using Ledgerly.Api.Auth;
using Ledgerly.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;

namespace Ledgerly.Tests.Auth;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService(int expiryHours = 24)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-key-must-be-at-least-32-chars!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:ExpiryHours"] = expiryHours.ToString(),
            })
            .Build();

        return new JwtTokenService(config);
    }

    private static ApplicationUser CreateUser(Guid? id = null, string email = "user@example.com")
    {
        var user = new ApplicationUser
        {
            Id = id ?? Guid.NewGuid(),
            UserName = email,
            Email = email,
        };
        return user;
    }

    [Fact]
    public void GenerateToken_ValidUser_ReturnsNonEmptyToken()
    {
        var svc = CreateService();
        var user = CreateUser();

        var dto = svc.GenerateToken(user);

        Assert.NotNull(dto.Token);
        Assert.NotEmpty(dto.Token);
        Assert.Equal(user.Email, dto.Email);
    }

    [Fact]
    public void GenerateToken_ClaimsContainUserIdAndEmail()
    {
        var svc = CreateService();
        var userId = Guid.NewGuid();
        var email = "test@ledgerly.dev";
        var user = CreateUser(userId, email);

        var dto = svc.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(dto.Token);

        var sub = parsed.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
        var emailClaim = parsed.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value;

        Assert.Equal(userId.ToString(), sub);
        Assert.Equal(email, emailClaim);
    }

    [Fact]
    public void GenerateToken_TokenExpiresAtConfiguredTime()
    {
        var expiryHours = 12;
        var svc = CreateService(expiryHours);
        var user = CreateUser();
        var before = DateTime.UtcNow;

        var dto = svc.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(dto.Token);

        var expectedExpiry = before.AddHours(expiryHours);
        // Allow 5-second tolerance
        Assert.True(Math.Abs((parsed.ValidTo - expectedExpiry).TotalSeconds) < 5);
        Assert.True(Math.Abs((dto.ExpiresUtc - expectedExpiry).TotalSeconds) < 5);
    }

    [Fact]
    public void GenerateToken_IssuerAndAudienceAreCorrect()
    {
        var svc = CreateService();
        var user = CreateUser();

        var dto = svc.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(dto.Token);

        Assert.Equal("TestIssuer", parsed.Issuer);
        Assert.Contains("TestAudience", parsed.Audiences);
    }

    [Fact]
    public void GenerateToken_EachTokenHasUniqueJti()
    {
        var svc = CreateService();
        var user = CreateUser();

        var dto1 = svc.GenerateToken(user);
        var dto2 = svc.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jti1 = handler.ReadJwtToken(dto1.Token).Claims
            .First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = handler.ReadJwtToken(dto2.Token).Claims
            .First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        Assert.NotEqual(jti1, jti2);
    }
}
