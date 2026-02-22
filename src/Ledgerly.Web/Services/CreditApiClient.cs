using Ledgerly.Contracts.Credit;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Web.Services;

public sealed class CreditApiClient
{
    private readonly HttpClient _http;

    public CreditApiClient(HttpClient http) => _http = http;

    public async Task<CreditProfileDto?> GetProfileAsync(Guid scenarioId, CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"/scenarios/{scenarioId}/credit", ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<CreditProfileDto>(cancellationToken: ct);
    }

    public async Task<CreditProfileDto> UpsertProfileAsync(Guid scenarioId, CreateCreditProfileRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync($"/scenarios/{scenarioId}/credit", req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            throw new InvalidOperationException(problem?.Detail ?? $"Request failed ({(int)resp.StatusCode}).");
        }
        var dto = await resp.Content.ReadFromJsonAsync<CreditProfileDto>(cancellationToken: ct);
        return dto ?? throw new InvalidOperationException("API returned empty response.");
    }

    public async Task DeleteProfileAsync(Guid scenarioId, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"/scenarios/{scenarioId}/credit", ct);
        if (!resp.IsSuccessStatusCode)
        {
            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            throw new InvalidOperationException(problem?.Detail ?? $"Request failed ({(int)resp.StatusCode}).");
        }
    }

    public async Task<CreditScoreProjectionDto> GetProjectionAsync(Guid scenarioId, CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<CreditScoreProjectionDto>(
            $"/scenarios/{scenarioId}/credit/projection", ct);
        return result ?? throw new InvalidOperationException("API returned empty response.");
    }
}
