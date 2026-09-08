using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Application.Common.Interfaces;

public sealed record IssuedRefreshToken(string Token, DateTime ExpiresAtUtc);

public sealed record RotatedSession(Guid UserId, UserRole Role, string Email, IssuedRefreshToken NewRefreshToken);

/// <summary>
/// FR-1.4 — "JWT access tokens with refresh token support." Rotation-based: every successful
/// refresh issues a brand-new token and immediately invalidates the one just used, so a
/// stolen-and-reused old refresh token is detectable (it simply won't validate a second time).
/// </summary>
public interface IRefreshTokenStore
{
    Task<IssuedRefreshToken> IssueAsync(Guid userId, UserRole role, string email, CancellationToken cancellationToken = default);

    /// <summary>Returns null if the token is unknown, already used/revoked, or expired.</summary>
    Task<RotatedSession?> ValidateAndRotateAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
}
