using System.Net.Http.Headers;
using Ledgerly.Contracts.Income;
using Ledgerly.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Web.Services;

public sealed class IncomeSourcesApiClient
{
    private readonly HttpClient _http;

    public IncomeSourcesApiClient(HttpClient http, AuthTokenService auth)
    {
        _http = http;
        if (auth.Token?.Token is { } tok)
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tok);
    }

    public async Task<List<IncomeSourceDto>> GetAllAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<IncomeSourceDto>>("/income-sources", ct)
           ?? new List<IncomeSourceDto>();

    public async Task<IncomeSourceDto> CreateAsync(CreateIncomeSourceRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/income-sources", req, ct);
        return await ReadResultAsync<IncomeSourceDto>(resp, ct);
    }

    public async Task<IncomeSourceDto> UpdateAsync(Guid id, UpdateIncomeSourceRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync($"/income-sources/{id}", req, ct);
        return await ReadResultAsync<IncomeSourceDto>(resp, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"/income-sources/{id}", ct);
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
