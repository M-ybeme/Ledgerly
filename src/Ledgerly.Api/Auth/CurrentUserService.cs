using System.Security.Claims;
using Ledgerly.Application.Auth;

namespace Ledgerly.Api.Auth;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserService(IHttpContextAccessor http)
    {
        _http = http;
    }

    public Guid UserId
    {
        get
        {
            var value = _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? _http.HttpContext?.User.FindFirstValue("sub");
            return value is not null ? Guid.Parse(value) : Guid.Empty;
        }
    }
}
