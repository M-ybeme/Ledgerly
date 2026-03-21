using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ledgerly.Contracts.Insights;
using Ledgerly.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Web.Services;

public sealed class InsightsApiClient
{
    private readonly HttpClient _http;
    private readonly AuthTokenService _auth;

    public InsightsApiClient(HttpClient http, AuthTokenService auth)
    {
        _http = http;
        _auth = auth;
    }

    public async Task<InsightsDto?> GetInsightsAsync(CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/insights");
        if (_auth.Token?.Token is { } tok)
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tok);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            throw new InvalidOperationException(problem?.Detail ?? $"Request failed ({(int)resp.StatusCode}).");
        }
        return await resp.Content.ReadFromJsonAsync<InsightsDto>(ct);
    }
}
