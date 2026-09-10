using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Infrastructure.Persistence;

/// <summary>
/// Backs Application's IRefreshTokenStore (FR-1.4). Not a Domain entity for the same reason
/// as IdempotencyRecord/TrackingNumberCounter — it's session/auth-session plumbing, not a
/// business concept. TokenHash stores a SHA-256 hash of the actual refresh token, never the
/// raw value, so a leaked database dump can't be replayed as valid refresh tokens.
/// </summary>
public sealed class RefreshTokenRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public UserRole Role { get; set; }
    public string Email { get; set; } = default!;
    public string TokenHash { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }

    public bool IsActive(DateTime nowUtc) => RevokedAtUtc is null && ExpiresAtUtc > nowUtc;
}
