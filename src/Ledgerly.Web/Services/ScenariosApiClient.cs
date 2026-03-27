using System.Net.Http.Headers;
using Ledgerly.Contracts.Scenarios;
using Ledgerly.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Web.Services;

public sealed class ScenariosApiClient
{
    private readonly HttpClient _http;

    public ScenariosApiClient(HttpClient http, AuthTokenService auth)
    {
        _http = http;
        if (auth.Token?.Token is { } tok)
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tok);
    }

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

    public async Task<ProjectionResultDto> GetProjectionAsync(Guid scenarioId, decimal? extraPaymentOverride = null, CancellationToken ct = default)
    {
        var url = extraPaymentOverride.HasValue
            ? $"/scenarios/{scenarioId}/projection?extraPaymentOverride={extraPaymentOverride.Value}"
            : $"/scenarios/{scenarioId}/projection";
        var result = await _http.GetFromJsonAsync<ProjectionResultDto>(url, ct);
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
