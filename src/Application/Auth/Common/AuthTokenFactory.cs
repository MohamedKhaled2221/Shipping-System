using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Application.Auth.Common;

/// <summary>
/// Small shared helper so RegisterCustomerCommandHandler and LoginCommandHandler don't each
/// duplicate "generate access token + issue refresh token + shape the DTO." Kept as a static
/// helper rather than its own MediatR request since it's not a use case on its own — nothing
/// ever calls it directly from a controller.
/// </summary>
internal static class AuthTokenFactory
{
    public static async Task<AuthTokensDto> IssueAsync(
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenStore refreshTokenStore,
        Guid userId,
        UserRole role,
        string email,
        CancellationToken cancellationToken)
    {
        var accessToken = jwtTokenGenerator.GenerateAccessToken(userId, role, email);
        var refreshToken = await refreshTokenStore.IssueAsync(userId, role, email, cancellationToken);

        return new AuthTokensDto(
            userId,
            role.ToString(),
            accessToken.AccessToken,
            accessToken.ExpiresAtUtc,
            refreshToken.Token,
            refreshToken.ExpiresAtUtc);
    }
}
