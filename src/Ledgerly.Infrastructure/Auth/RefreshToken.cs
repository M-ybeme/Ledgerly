namespace Ledgerly.Infrastructure.Auth;

public sealed class RefreshToken
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public string TokenHash { get; init; } = default!;
    public DateTime ExpiresUtc { get; init; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

    public ApplicationUser User { get; init; } = default!;
}
