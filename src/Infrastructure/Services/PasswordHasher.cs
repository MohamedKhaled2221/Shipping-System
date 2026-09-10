using Microsoft.AspNetCore.Identity;

namespace ShippingSystem.Infrastructure.Services;

/// <summary>
/// Wraps Microsoft.AspNetCore.Identity's PasswordHasher&lt;TUser&gt; — battle-tested
/// PBKDF2-based hashing — without pulling in the full ASP.NET Core Identity membership
/// system (no IdentityUser, no Identity EF stores). The generic parameter is never actually
/// used by PasswordHasher's algorithm, so a placeholder `object` is fine.
/// </summary>
public sealed class PasswordHasher : ShippingSystem.Application.Common.Interfaces.IPasswordHasher
{
    private readonly PasswordHasher<object> _identityHasher = new();
    private static readonly object DummyUser = new();

    public string Hash(string plainTextPassword) =>
        _identityHasher.HashPassword(DummyUser, plainTextPassword);

    public bool Verify(string plainTextPassword, string passwordHash)
    {
        var result = _identityHasher.VerifyHashedPassword(DummyUser, passwordHash, plainTextPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
