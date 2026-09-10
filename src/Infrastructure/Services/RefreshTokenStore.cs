using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Infrastructure.Options;
using ShippingSystem.Infrastructure.Persistence;

namespace ShippingSystem.Infrastructure.Services;

/// <summary>
/// FR-1.4. Deliberately takes the AMBIENT scoped AppDbContext (not a factory-created one) —
/// unlike TrackingNumberGenerator/NotificationDispatcher, this class's writes genuinely
/// belong in the same unit of work as the command handler that calls it (e.g.
/// RegisterCustomerCommandHandler stages both the new Customer and the new
/// RefreshTokenRecord, then commits both in one SaveChangesAsync call).
/// </summary>
public sealed class RefreshTokenStore : IRefreshTokenStore
{
    private readonly AppDbContext _dbContext;
    private readonly JwtOptions _options;

    public RefreshTokenStore(AppDbContext dbContext, IOptions<JwtOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;
    }

    public Task<IssuedRefreshToken> IssueAsync(Guid userId, Domain.Enums.UserRole role, string email, CancellationToken cancellationToken = default)
    {
        var rawToken = GenerateRawToken();
        var expiresAtUtc = DateTime.UtcNow.AddDays(_options.RefreshTokenDays);

        _dbContext.RefreshTokenRecords.Add(new RefreshTokenRecord
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Role = role,
            Email = email,
            TokenHash = Hash(rawToken),
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        });

        return Task.FromResult(new IssuedRefreshToken(rawToken, expiresAtUtc));
    }

    public async Task<RotatedSession?> ValidateAndRotateAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = Hash(refreshToken);
        var record = await _dbContext.RefreshTokenRecords
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);

        if (record is null)
            return null;

        if (record.RevokedAtUtc is not null)
        {
            // Reuse of a token that was already rotated away is a strong signal the refresh
            // token was stolen — rotation is precisely what makes this detectable in the
            // first place. As a precaution the entire session is revoked, not just this one
            // token, and committed immediately: this security action must not be lost even
            // if the caller's handler goes on to throw and never reaches its own
            // IUnitOfWork.SaveChangesAsync call.
            var activeTokens = await _dbContext.RefreshTokenRecords
                .Where(r => r.UserId == record.UserId && r.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var activeToken in activeTokens)
                activeToken.RevokedAtUtc = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }

        if (record.ExpiresAtUtc <= DateTime.UtcNow)
            return null;

        record.RevokedAtUtc = DateTime.UtcNow; // rotation: this exact token can never be used again

        var newToken = await IssueAsync(record.UserId, record.Role, record.Email, cancellationToken);
        return new RotatedSession(record.UserId, record.Role, record.Email, newToken);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = Hash(refreshToken);
        var record = await _dbContext.RefreshTokenRecords
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);

        if (record is not null)
            record.RevokedAtUtc = DateTime.UtcNow;
    }

    private static string GenerateRawToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
}
