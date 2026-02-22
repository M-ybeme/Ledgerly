using Microsoft.AspNetCore.Identity;

namespace Ledgerly.Infrastructure.Auth;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
