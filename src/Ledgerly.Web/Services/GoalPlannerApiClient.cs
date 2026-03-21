using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ledgerly.Contracts.Goals;
using Ledgerly.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Web.Services;

public sealed class GoalPlannerApiClient
{
    private readonly HttpClient _http;
    private readonly AuthTokenService _auth;

    public GoalPlannerApiClient(HttpClient http, AuthTokenService auth)
    {
        _http = http;
        _auth = auth;
    }

    public async Task<GoalPlanResultDto?> ComputeAsync(GoalPlanRequest request, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/goal/plan")
        {
            Content = JsonContent.Create(request)
        };
        if (_auth.Token?.Token is { } tok)
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tok);

        var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            throw new InvalidOperationException(problem?.Detail ?? $"Request failed ({(int)resp.StatusCode}).");
        }
        return await resp.Content.ReadFromJsonAsync<GoalPlanResultDto>(ct);
    }
}
