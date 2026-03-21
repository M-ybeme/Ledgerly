using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ledgerly.Contracts.Dashboard;
using Ledgerly.Contracts.NetWorth;
using Ledgerly.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Web.Services;

public sealed class DashboardApiClient
{
    private readonly HttpClient _http;
    private readonly AuthTokenService _auth;

    public DashboardApiClient(HttpClient http, AuthTokenService auth)
    {
        _http = http;
        _auth = auth;
    }

    public async Task<FinancialSummaryDto?> GetFinancialSummaryAsync(CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/dashboard/financial-summary");
        if (_auth.Token?.Token is { } tok)
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tok);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            throw new InvalidOperationException(problem?.Detail ?? $"Request failed ({(int)resp.StatusCode}).");
        }
        return await resp.Content.ReadFromJsonAsync<FinancialSummaryDto>(ct);
    }

    public async Task<NetWorthSummaryDto?> GetNetWorthSummaryAsync(CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/dashboard/net-worth");
        if (_auth.Token?.Token is { } tok)
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tok);
        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            throw new InvalidOperationException(problem?.Detail ?? $"Request failed ({(int)resp.StatusCode}).");
        }
        return await resp.Content.ReadFromJsonAsync<NetWorthSummaryDto>(ct);
    }
}
