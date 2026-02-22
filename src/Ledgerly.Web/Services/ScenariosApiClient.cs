using Ledgerly.Contracts.Scenarios;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Web.Services;

public sealed class ScenariosApiClient
{
    private readonly HttpClient _http;

    public ScenariosApiClient(HttpClient http) => _http = http;

    public async Task<List<ScenarioDto>> GetAllAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<ScenarioDto>>("/scenarios", ct)
           ?? [];

    public async Task<ScenarioDto> CreateAsync(CreateScenarioRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/scenarios", req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            throw new InvalidOperationException(problem?.Detail ?? $"Request failed ({(int)resp.StatusCode}).");
        }
        var created = await resp.Content.ReadFromJsonAsync<ScenarioDto>(cancellationToken: ct);
        return created ?? throw new InvalidOperationException("API returned empty response.");
    }

    public async Task<ScenarioDto> DuplicateAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"/scenarios/{id}/duplicate", null, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            throw new InvalidOperationException(problem?.Detail ?? $"Request failed ({(int)resp.StatusCode}).");
        }
        var created = await resp.Content.ReadFromJsonAsync<ScenarioDto>(cancellationToken: ct);
        return created ?? throw new InvalidOperationException("API returned empty response.");
    }

    public async Task<ProjectionResultDto> GetProjectionAsync(Guid scenarioId, CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<ProjectionResultDto>($"/scenarios/{scenarioId}/projection", ct);
        return result ?? throw new InvalidOperationException("API returned empty response.");
    }

    public async Task<ScenarioComparisonDto> CompareAsync(Guid idA, Guid idB, CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<ScenarioComparisonDto>(
            $"/scenarios/compare?a={idA}&b={idB}", ct);
        return result ?? throw new InvalidOperationException("API returned empty response.");
    }

    public async Task<List<ActualPaymentDto>> GetPaymentsAsync(Guid scenarioId, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<ActualPaymentDto>>($"/scenarios/{scenarioId}/payments", ct)
           ?? [];

    public async Task<ActualPaymentDto> LogPaymentAsync(Guid scenarioId, LogPaymentRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"/scenarios/{scenarioId}/payments", req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            throw new InvalidOperationException(problem?.Detail ?? $"Request failed ({(int)resp.StatusCode}).");
        }
        var created = await resp.Content.ReadFromJsonAsync<ActualPaymentDto>(cancellationToken: ct);
        return created ?? throw new InvalidOperationException("API returned empty response.");
    }

    public async Task DeletePaymentAsync(Guid scenarioId, Guid paymentId, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"/scenarios/{scenarioId}/payments/{paymentId}", ct);
        if (!resp.IsSuccessStatusCode)
        {
            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            throw new InvalidOperationException(problem?.Detail ?? $"Request failed ({(int)resp.StatusCode}).");
        }
    }

    public async Task<DriftSummaryDto> GetDriftAsync(Guid scenarioId, CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<DriftSummaryDto>($"/scenarios/{scenarioId}/drift", ct);
        return result ?? throw new InvalidOperationException("API returned empty response.");
    }
}
