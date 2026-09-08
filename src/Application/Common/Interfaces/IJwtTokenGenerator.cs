using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Application.Common.Interfaces;

public sealed record AccessTokenResult(string AccessToken, DateTime ExpiresAtUtc);

/// <summary>FR-1.4 — JWT access tokens; refresh tokens are handled separately by IRefreshTokenStore.</summary>
public interface IJwtTokenGenerator
{
    AccessTokenResult GenerateAccessToken(Guid userId, UserRole role, string email);
}
