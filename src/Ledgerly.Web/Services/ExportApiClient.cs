using System.Net.Http.Headers;
using Ledgerly.Web.Auth;

namespace Ledgerly.Web.Services;

public sealed class ExportApiClient
{
    private readonly HttpClient _http;

    public ExportApiClient(HttpClient http, AuthTokenService auth)
    {
        _http = http;
        if (auth.Token?.Token is { } tok)
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tok);
    }

    public async Task<byte[]> ExportJsonAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetAsync("/export/json", ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<byte[]> ExportCsvAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetAsync("/export/csv", ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsByteArrayAsync(ct);
    }
}
