using Ledgerly.Contracts.Budget;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Web.Services;

public sealed class BudgetPlansApiClient
{
    private readonly HttpClient _http;

    public BudgetPlansApiClient(HttpClient http) => _http = http;

    public async Task<List<BudgetPlanDto>> GetAllAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<BudgetPlanDto>>("/budget-plans", ct) ?? [];

    public async Task<BudgetPlanDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<BudgetPlanDto>($"/budget-plans/{id}", ct);

    public async Task<BudgetPlanDto> CreateAsync(CreateBudgetPlanRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/budget-plans", req, ct);
        return await ReadResultAsync<BudgetPlanDto>(resp, ct);
    }

    public async Task<BudgetPlanDto> UpdateAsync(Guid id, UpdateBudgetPlanRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync($"/budget-plans/{id}", req, ct);
        return await ReadResultAsync<BudgetPlanDto>(resp, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"/budget-plans/{id}", ct);
        if (!resp.IsSuccessStatusCode)
        {
            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            throw new InvalidOperationException(problem?.Detail ?? $"Request failed ({(int)resp.StatusCode}).");
        }
    }

    public async Task<BudgetSummaryDto> GetSummaryAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<BudgetSummaryDto>($"/budget-plans/{id}/summary", ct);
        return result ?? throw new InvalidOperationException("API returned empty response.");
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
