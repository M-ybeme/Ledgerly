using System.Net.Http.Headers;
using Ledgerly.Contracts.Budget;
using Ledgerly.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Web.Services;

public sealed class MonthlyBudgetsApiClient
{
    private readonly HttpClient _http;

    public MonthlyBudgetsApiClient(HttpClient http, AuthTokenService auth)
    {
        _http = http;
        if (auth.Token?.Token is { } tok)
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tok);
    }

    public async Task<List<MonthlyBudgetDto>> GetForMonthAsync(int year, int month, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<MonthlyBudgetDto>>($"/monthly-budgets?year={year}&month={month}", ct)
           ?? new List<MonthlyBudgetDto>();

    public async Task<MonthlyBudgetDto> SetAsync(int year, int month, SetMonthlyBudgetRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync($"/monthly-budgets?year={year}&month={month}", req, ct);
        return await ReadResultAsync<MonthlyBudgetDto>(resp, ct);
    }

    public async Task DeleteAsync(int year, int month, Guid categoryId, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"/monthly-budgets?year={year}&month={month}&categoryId={categoryId}", ct);
        if (!resp.IsSuccessStatusCode)
        {
            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            throw new InvalidOperationException(problem?.Detail ?? $"Request failed ({(int)resp.StatusCode}).");
        }
    }

    private static async Task<T> ReadResultAsync<T>(HttpResponseMessage resp, CancellationToken ct)
    {
        if (!resp.IsSuccessStatusCode)
        {
            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            throw new InvalidOperationException(problem?.Detail ?? $"Request failed ({(int)resp.StatusCode}).");
        }
        var result = await resp.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("API returned empty response.");
    }
}
