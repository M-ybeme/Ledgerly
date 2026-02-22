namespace Ledgerly.Application.Auth;

public interface ICurrentUserService
{
    Guid UserId { get; }
}
