using Ledgerly.Contracts.Debts;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Web.Services;

public sealed class DebtAccountsApiClient
{
    private readonly HttpClient _http;

    public DebtAccountsApiClient(HttpClient http) => _http = http;

    public async Task<List<DebtAccountDto>> GetAllAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<DebtAccountDto>>("/debt-accounts", ct)
           ?? [];

    public async Task<DebtAccountDto> CreateAsync(CreateDebtAccountRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/debt-accounts", req, ct);
        return await ReadResultAsync<DebtAccountDto>(resp, ct);
    }

    public async Task<DebtAccountDto> UpdateAsync(Guid id, UpdateDebtAccountRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync($"/debt-accounts/{id}", req, ct);
        return await ReadResultAsync<DebtAccountDto>(resp, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"/debt-accounts/{id}", ct);
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
