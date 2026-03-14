using System.Net.Http.Headers;
using Ledgerly.Contracts.Budget;
using Ledgerly.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Web.Services;

public sealed class PlannedExpensesApiClient
{
    private readonly HttpClient _http;

    public PlannedExpensesApiClient(HttpClient http, AuthTokenService auth)
    {
        _http = http;
        if (auth.Token?.Token is { } tok)
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tok);
    }

    public async Task<List<PlannedExpenseDto>> GetAllAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<PlannedExpenseDto>>("/planned-expenses", ct)
           ?? new List<PlannedExpenseDto>();

    public async Task<PlannedExpenseDto> CreateAsync(CreatePlannedExpenseRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/planned-expenses", req, ct);
        return await ReadResultAsync<PlannedExpenseDto>(resp, ct);
    }

    public async Task<PlannedExpenseDto> UpdateAsync(Guid id, UpdatePlannedExpenseRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync($"/planned-expenses/{id}", req, ct);
        return await ReadResultAsync<PlannedExpenseDto>(resp, ct);
    }

    public async Task<PlannedExpenseDto> MarkPaidAsync(Guid id, MarkPaidRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"/planned-expenses/{id}/mark-paid", req, ct);
        return await ReadResultAsync<PlannedExpenseDto>(resp, ct);
    }

    public async Task MarkUnpaidAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"/planned-expenses/{id}/mark-unpaid", (object?)null, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            throw new InvalidOperationException(problem?.Detail ?? $"Request failed ({(int)resp.StatusCode}).");
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"/planned-expenses/{id}", ct);
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
