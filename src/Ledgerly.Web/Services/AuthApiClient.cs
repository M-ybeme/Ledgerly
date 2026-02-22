using System.Net.Http.Json;
using System.Text.Json;
using Ledgerly.Contracts.Auth;

namespace Ledgerly.Web.Services;

public sealed class AuthApiClient
{
    private readonly HttpClient _http;

    public AuthApiClient(HttpClient http)
    {
        _http = http;
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

    public async Task<AuthTokenDto> LoginAsync(LoginRequest req)
    {
        var response = await _http.PostAsJsonAsync("auth/login", req);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await ReadProblemDetailAsync(response);
            throw new InvalidOperationException(detail);
        }
        return (await response.Content.ReadFromJsonAsync<AuthTokenDto>())!;
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

    public async Task ChangePasswordAsync(ChangePasswordRequest req)
    {
        var response = await _http.PostAsJsonAsync("auth/change-password", req);
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
