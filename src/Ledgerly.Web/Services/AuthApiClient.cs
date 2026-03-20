using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ledgerly.Contracts.Auth;
using Ledgerly.Web.Auth;

namespace Ledgerly.Web.Services;

public sealed class AuthApiClient
{
    private readonly HttpClient _http;
    private readonly AuthTokenService _auth;

    public AuthApiClient(HttpClient http, AuthTokenService auth)
    {
        _http = http;
        _auth = auth;
    }

    private HttpRequestMessage AuthorizedGet(string url)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        if (_auth.Token?.Token is { } tok)
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tok);
        return req;
    }

    private HttpRequestMessage AuthorizedPost<T>(string url, T body)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        if (_auth.Token?.Token is { } tok)
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tok);
        return req;
    }

    public async Task RegisterAsync(RegisterRequest req)
    {
        var response = await _http.PostAsJsonAsync("auth/register", req);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadProblemDetailAsync(response);
            throw new InvalidOperationException(detail);
        }
    }

    public async Task<LoginResultDto> LoginAsync(LoginRequest req)
    {
        var response = await _http.PostAsJsonAsync("auth/login", req);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadProblemDetailAsync(response);
            throw new InvalidOperationException(detail);
        }
        return (await response.Content.ReadFromJsonAsync<LoginResultDto>())!;
    }

    public async Task<AuthTokenDto> VerifyTwoFactorLoginAsync(TwoFactorLoginRequest req)
    {
        var response = await _http.PostAsJsonAsync("auth/2fa/verify-login", req);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadProblemDetailAsync(response);
            throw new InvalidOperationException(detail);
        }
        return (await response.Content.ReadFromJsonAsync<AuthTokenDto>())!;
    }

    public async Task<TwoFactorSetupDto> GetTwoFactorSetupAsync()
    {
        var response = await _http.SendAsync(AuthorizedGet("auth/2fa/setup"));
        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadProblemDetailAsync(response);
            throw new InvalidOperationException(detail);
        }
        return (await response.Content.ReadFromJsonAsync<TwoFactorSetupDto>())!;
    }

    public async Task<bool> GetTwoFactorStatusAsync()
    {
        var response = await _http.SendAsync(AuthorizedGet("auth/2fa/status"));
        if (!response.IsSuccessStatusCode) return false;
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return doc.RootElement.TryGetProperty("isEnabled", out var val) && val.GetBoolean();
    }

    public async Task EnableTwoFactorAsync(VerifyTwoFactorRequest req)
    {
        var response = await _http.SendAsync(AuthorizedPost("auth/2fa/enable", req));
        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadProblemDetailAsync(response);
            throw new InvalidOperationException(detail);
        }
    }

    public async Task DisableTwoFactorAsync()
    {
        var response = await _http.SendAsync(AuthorizedPost("auth/2fa/disable", new { }));
        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadProblemDetailAsync(response);
            throw new InvalidOperationException(detail);
        }
    }

    public async Task ConfirmEmailAsync(string email, string token)
    {
        var url = $"auth/confirm-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadProblemDetailAsync(response);
            throw new InvalidOperationException(detail);
        }
    }

    public async Task ResendConfirmationAsync(string email)
    {
        var response = await _http.PostAsJsonAsync("auth/resend-confirmation", new ForgotPasswordRequest(email));
        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadProblemDetailAsync(response);
            throw new InvalidOperationException(detail);
        }
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest req)
    {
        var response = await _http.PostAsJsonAsync("auth/forgot-password", req);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadProblemDetailAsync(response);
            throw new InvalidOperationException(detail);
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest req)
    {
        var response = await _http.PostAsJsonAsync("auth/reset-password", req);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadProblemDetailAsync(response);
            throw new InvalidOperationException(detail);
        }
    }

    public async Task<AuthTokenDto> RefreshAsync(string refreshToken)
    {
        var response = await _http.PostAsJsonAsync("auth/refresh", new { refreshToken });
        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadProblemDetailAsync(response);
            throw new InvalidOperationException(detail);
        }
        return (await response.Content.ReadFromJsonAsync<AuthTokenDto>())!;
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var response = await _http.SendAsync(AuthorizedPost("auth/logout", new { refreshToken }));
        // Best-effort — ignore errors
        _ = response;
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest req)
    {
        var response = await _http.SendAsync(AuthorizedPost("auth/change-password", req));
        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadProblemDetailAsync(response);
            throw new InvalidOperationException(detail);
        }
    }

    private static async Task<string> ReadProblemDetailAsync(HttpResponseMessage response)
    {
        try
        {
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            if (doc.RootElement.TryGetProperty("detail", out var detail))
                return detail.GetString() ?? "An error occurred.";
        }
        catch { }
        return $"Request failed: {response.StatusCode}";
    }
}
