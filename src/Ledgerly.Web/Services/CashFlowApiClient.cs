using System.Net.Http.Headers;
using Ledgerly.Contracts.CashFlow;
using Ledgerly.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Web.Services;

public sealed class CashFlowApiClient
{
    private readonly HttpClient _http;
    private readonly AuthTokenService _auth;

    public CashFlowApiClient(HttpClient http, AuthTokenService auth)
    {
        _http = http;
        _auth = auth;
    }

    public async Task<CashFlowForecastDto> GetForecastAsync(
        decimal startingBalance,
        int days,
        decimal? dailyBurnRate = null,
        CancellationToken ct = default)
    {
        var url = $"/cash-flow/forecast?startingBalance={startingBalance}&days={days}";
        if (dailyBurnRate is > 0)
            url += $"&dailyBurnRate={dailyBurnRate}";

        var req = new HttpRequestMessage(HttpMethod.Get, url);
        if (_auth.Token?.Token is { } tok)
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tok);

        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            throw new InvalidOperationException(problem?.Detail ?? $"Request failed ({(int)resp.StatusCode}).");
        }

        return await resp.Content.ReadFromJsonAsync<CashFlowForecastDto>(cancellationToken: ct)
               ?? throw new InvalidOperationException("API returned empty response.");
    }
}
