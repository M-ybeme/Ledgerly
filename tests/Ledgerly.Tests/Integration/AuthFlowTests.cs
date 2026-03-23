using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Ledgerly.Contracts.Auth;
using Ledgerly.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Ledgerly.Tests.Integration;

/// <summary>
/// End-to-end auth flow tests. Each test method gets a fresh HttpClient from the
/// shared factory instance; the factory uses a unique InMemory DB per factory instance
/// so tests within this class do not collide as long as they use distinct emails.
/// </summary>
public sealed class AuthFlowTests : IClassFixture<LedgerlyApiFactory>
{
    private readonly LedgerlyApiFactory _factory;
    private readonly HttpClient _client;

    // Shared test JWT config (must match LedgerlyApiFactory values)
    private const string TestSecret   = "ledgerly-test-secret-key-32chars!!";
    private const string TestIssuer   = "LedgerlyTest";
    private const string TestAudience = "LedgerlyTestUsers";

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public AuthFlowTests(LedgerlyApiFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Registers a user via the API and returns (email, password).</summary>
    private async Task<(string email, string password)> RegisterAsync(string? email = null, string? password = null)
    {
        email    ??= $"test-{Guid.NewGuid():N}@example.com";
        password ??= "P@ssword1!";

        var resp = await _client.PostAsJsonAsync("/auth/register", new RegisterRequest(email, password));
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (email, password);
    }

    /// <summary>Manually confirms email via Identity UserManager (bypasses email delivery).</summary>
    private async Task ConfirmEmailAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await um.FindByEmailAsync(email);
        Assert.NotNull(user);
        var token = await um.GenerateEmailConfirmationTokenAsync(user!);
        var result = await um.ConfirmEmailAsync(user!, token);
        Assert.True(result.Succeeded);
    }

    /// <summary>Register + confirm + login; returns the LoginResultDto.</summary>
    private async Task<LoginResultDto> RegisterConfirmLoginAsync(string? email = null, string? password = null)
    {
        var (e, p) = await RegisterAsync(email, password);
        await ConfirmEmailAsync(e);
        var resp = await _client.PostAsJsonAsync("/auth/login", new LoginRequest(e, p));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await resp.Content.ReadFromJsonAsync<LoginResultDto>(JsonOpts);
        Assert.NotNull(result);
        return result!;
    }

    /// <summary>
    /// Builds a JWT signed with the test secret but with an expiry in the past.
    /// This simulates an expired access token without waiting.
    /// </summary>
    private static string BuildExpiredToken(string userId, string email)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };

        // notBefore and expires both in the past — token is expired well beyond the default
        // 5-minute clock skew allowance that the JWT validator uses
        var token = new JwtSecurityToken(
            issuer:             TestIssuer,
            audience:           TestAudience,
            claims:             claims,
            notBefore:          DateTime.UtcNow.AddMinutes(-30),
            expires:            DateTime.UtcNow.AddMinutes(-10),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ── Register ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidCredentials_Returns201()
    {
        var email = $"reg-{Guid.NewGuid():N}@example.com";
        var resp = await _client.PostAsJsonAsync("/auth/register", new RegisterRequest(email, "P@ssword1!"));

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/auth/register", new RegisterRequest(email, "P@ssword1!"));

        // Second registration with same email
        var resp = await _client.PostAsJsonAsync("/auth/register", new RegisterRequest(email, "P@ssword1!"));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidConfirmedUser_Returns200WithToken()
    {
        var dto = await RegisterConfirmLoginAsync();

        Assert.False(dto.RequiresTwoFactor);
        Assert.NotNull(dto.Token);
        Assert.NotEmpty(dto.Token!);
        Assert.NotNull(dto.RefreshToken);
        Assert.NotEmpty(dto.RefreshToken!);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var (email, _) = await RegisterAsync();
        await ConfirmEmailAsync(email);

        var resp = await _client.PostAsJsonAsync("/auth/login", new LoginRequest(email, "WrongPass99!"));

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Login_UnconfirmedEmail_Returns403()
    {
        var (email, password) = await RegisterAsync();
        // Do NOT confirm email

        var resp = await _client.PostAsJsonAsync("/auth/login", new LoginRequest(email, password));

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Login_NonExistentUser_Returns401()
    {
        var resp = await _client.PostAsJsonAsync("/auth/login",
            new LoginRequest("nobody@nowhere.com", "P@ssword1!"));

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Protected endpoint (/auth/me) ─────────────────────────────────────────

    [Fact]
    public async Task Me_WithValidToken_Returns200AndCorrectEmail()
    {
        var email = $"me-{Guid.NewGuid():N}@example.com";
        var dto   = await RegisterConfirmLoginAsync(email);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", dto.Token);

        var resp = await _client.GetAsync("/auth/me");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains(email, body, StringComparison.OrdinalIgnoreCase);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var resp = await _client.GetAsync("/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Me_WithExpiredToken_Returns401()
    {
        // Register and confirm so we have a real userId
        var email = $"exp-{Guid.NewGuid():N}@example.com";
        await RegisterAsync(email);
        await ConfirmEmailAsync(email);

        using var scope = _factory.Services.CreateScope();
        var um   = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await um.FindByEmailAsync(email);
        Assert.NotNull(user);

        var expiredToken = BuildExpiredToken(user!.Id.ToString(), email);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expiredToken);

        var resp = await _client.GetAsync("/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    // ── Refresh token rotation ────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ValidToken_Returns200WithNewTokens()
    {
        var dto = await RegisterConfirmLoginAsync();
        Assert.NotNull(dto.RefreshToken);

        var refreshResp = await _client.PostAsJsonAsync("/auth/refresh",
            new RefreshTokenRequest(dto.RefreshToken!));

        Assert.Equal(HttpStatusCode.OK, refreshResp.StatusCode);
        var newDto = await refreshResp.Content.ReadFromJsonAsync<AuthTokenDto>(JsonOpts);
        Assert.NotNull(newDto?.Token);
        Assert.NotNull(newDto?.RefreshToken);
        // New refresh token must differ from the original
        Assert.NotEqual(dto.RefreshToken, newDto!.RefreshToken);
    }

    [Fact]
    public async Task Refresh_UsedToken_Returns401()
    {
        var dto = await RegisterConfirmLoginAsync();
        Assert.NotNull(dto.RefreshToken);

        // First use — should succeed and rotate the token
        await _client.PostAsJsonAsync("/auth/refresh", new RefreshTokenRequest(dto.RefreshToken!));

        // Second use of the same (now revoked) token — must fail
        var secondResp = await _client.PostAsJsonAsync("/auth/refresh",
            new RefreshTokenRequest(dto.RefreshToken!));

        Assert.Equal(HttpStatusCode.Unauthorized, secondResp.StatusCode);
    }

    [Fact]
    public async Task Refresh_InvalidToken_Returns401()
    {
        var resp = await _client.PostAsJsonAsync("/auth/refresh",
            new RefreshTokenRequest("not-a-valid-refresh-token"));

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    // ── Change password ───────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Returns400()
    {
        var dto = await RegisterConfirmLoginAsync();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", dto.Token);

        var resp = await _client.PostAsJsonAsync("/auth/change-password",
            new ChangePasswordRequest("WrongCurrent!", "NewP@ss1!"));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task ChangePassword_CorrectCurrentPassword_Returns200()
    {
        const string originalPassword = "P@ssword1!";
        var dto = await RegisterConfirmLoginAsync(password: originalPassword);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", dto.Token);

        var resp = await _client.PostAsJsonAsync("/auth/change-password",
            new ChangePasswordRequest(originalPassword, "NewP@ss1!"));

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }
}
