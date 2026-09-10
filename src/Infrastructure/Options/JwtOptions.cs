namespace ShippingSystem.Infrastructure.Options;

/// <summary>Bind from appsettings/user-secrets under "Jwt". Secret must be at least 32 characters (256 bits) for HS256.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = default!;
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;
}
