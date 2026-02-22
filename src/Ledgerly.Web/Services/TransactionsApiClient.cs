using Ledgerly.Contracts.Budget;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Web.Services;

public sealed class TransactionsApiClient
{
    private readonly HttpClient _http;

    public TransactionsApiClient(HttpClient http) => _http = http;

    public async Task<List<TransactionDto>> GetAllAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default)
    {
        var url = "/transactions";
        var qs = new List<string>();
        if (from.HasValue) qs.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue) qs.Add($"to={to.Value:yyyy-MM-dd}");
        if (qs.Count > 0) url += "?" + string.Join("&", qs);
        return await _http.GetFromJsonAsync<List<TransactionDto>>(url, ct) ?? [];
    }

    public async Task<TransactionDto> CreateAsync(CreateTransactionRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/transactions", req, ct);
        return await ReadResultAsync<TransactionDto>(resp, ct);
    }

    public async Task<TransactionDto> UpdateAsync(Guid id, UpdateTransactionRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync($"/transactions/{id}", req, ct);
        return await ReadResultAsync<TransactionDto>(resp, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"/transactions/{id}", ct);
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
