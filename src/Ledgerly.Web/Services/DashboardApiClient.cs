using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ledgerly.Contracts.Dashboard;
using Ledgerly.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Web.Services;

public sealed class DashboardApiClient
{
    private readonly HttpClient _http;

    public DashboardApiClient(HttpClient http, AuthTokenService auth)
    {
        _http = http;
        if (auth.Token?.Token is { } tok)
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tok);
    }

    public async Task<FinancialSummaryDto?> GetFinancialSummaryAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetAsync("/dashboard/financial-summary", ct);
        if (!resp.IsSuccessStatusCode)
        {
            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            throw new InvalidOperationException(problem?.Detail ?? $"Request failed ({(int)resp.StatusCode}).");
        }
        return await resp.Content.ReadFromJsonAsync<FinancialSummaryDto>(ct);
    }
}
