using System.Net.Http.Headers;
using Ledgerly.Contracts.Budget;
using Ledgerly.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Web.Services;

public sealed class SavingsGoalsApiClient
{
    private readonly HttpClient _http;

    public SavingsGoalsApiClient(HttpClient http, AuthTokenService auth)
    {
        _http = http;
        if (auth.Token?.Token is { } tok)
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tok);
    }

    public async Task<List<SavingsGoalDto>> GetAllAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<SavingsGoalDto>>("/savings-goals", ct)
           ?? new List<SavingsGoalDto>();

    public async Task<SavingsGoalDto> CreateAsync(CreateSavingsGoalRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/savings-goals", req, ct);
        return await ReadResultAsync<SavingsGoalDto>(resp, ct);
    }

    public async Task<SavingsGoalDto> UpdateAsync(Guid id, UpdateSavingsGoalRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync($"/savings-goals/{id}", req, ct);
        return await ReadResultAsync<SavingsGoalDto>(resp, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"/savings-goals/{id}", ct);
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
