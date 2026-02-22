using Ledgerly.Contracts.Budget;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Web.Services;

public sealed class BudgetCategoriesApiClient
{
    private readonly HttpClient _http;

    public BudgetCategoriesApiClient(HttpClient http) => _http = http;

    public async Task<List<BudgetCategoryDto>> GetAllAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<BudgetCategoryDto>>("/budget-categories", ct) ?? [];

    public async Task<BudgetCategoryDto> CreateAsync(CreateBudgetCategoryRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/budget-categories", req, ct);
        return await ReadResultAsync<BudgetCategoryDto>(resp, ct);
    }

    public async Task<BudgetCategoryDto> UpdateAsync(Guid id, UpdateBudgetCategoryRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync($"/budget-categories/{id}", req, ct);
        return await ReadResultAsync<BudgetCategoryDto>(resp, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"/budget-categories/{id}", ct);
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
