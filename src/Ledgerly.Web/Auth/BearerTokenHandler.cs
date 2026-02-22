using System.Net.Http.Headers;

namespace Ledgerly.Web.Auth;

public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly AuthTokenService _auth;

    public BearerTokenHandler(AuthTokenService auth)
    {
        _auth = auth;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_auth.Token is { } dto)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", dto.Token);

        return base.SendAsync(request, cancellationToken);
    }
}
