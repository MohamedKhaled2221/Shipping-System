namespace ShippingSystem.Application.Common.Models;

/// <summary>Returned by Register/Login/RefreshToken — the API layer hands AccessToken back
/// as a Bearer token and RefreshToken as an httpOnly cookie or response body field, per the
/// client's needs.</summary>
public sealed record AuthTokensDto(
    Guid UserId,
    string Role,
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
