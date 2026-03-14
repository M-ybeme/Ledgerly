using System.Net.Http.Headers;
using Ledgerly.Contracts.Accounts;
using Ledgerly.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Web.Services;

public sealed class AccountsApiClient
{
    private readonly HttpClient _http;

    public AccountsApiClient(HttpClient http, AuthTokenService auth)
    {
        _http = http;
        if (auth.Token?.Token is { } tok)
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tok);
    }

    public async Task<List<AccountDto>> GetAccountsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<AccountDto>>("/accounts", ct)
           ?? new List<AccountDto>();

    public async Task<AccountDto> CreateAccountAsync(CreateAccountRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/accounts", req, ct);
        return await ReadResultAsync<AccountDto>(resp, ct);
    }

    public async Task<AccountDto> UpdateAccountAsync(Guid id, UpdateAccountRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PutAsJsonAsync($"/accounts/{id}", req, ct);
        return await ReadResultAsync<AccountDto>(resp, ct);
    }

    public async Task DeleteAccountAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"/accounts/{id}", ct);
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