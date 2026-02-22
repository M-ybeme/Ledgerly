using Ledgerly.Contracts.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Web.Services;

public sealed class AccountsApiClient
{
    private readonly HttpClient _http;

    public AccountsApiClient(HttpClient http) => _http = http;

    public async Task<List<AccountDto>> GetAccountsAsync(CancellationToken ct = default)
        => await _http.GetFromJsonAsync<List<AccountDto>>("/accounts", ct)
           ?? new List<AccountDto>();

    public async Task<AccountDto> CreateAccountAsync(CreateAccountRequest req, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/accounts", req, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            throw new InvalidOperationException(problem?.Detail ?? $"Request failed ({(int)resp.StatusCode}).");
        }

        var created = await resp.Content.ReadFromJsonAsync<AccountDto>(cancellationToken: ct);
        return created ?? throw new InvalidOperationException("API returned empty response.");
    }
}