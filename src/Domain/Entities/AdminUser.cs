using ShippingSystem.Domain.Common;
using ShippingSystem.Domain.Exceptions;

namespace ShippingSystem.Domain.Entities;

/// <summary>
/// GAP-FILL, flagged per SRS §8 ("Any missing requirements ... identified during this
/// analysis phase should be raised and resolved before implementation begins"): FR-1.2
/// requires "Admins and Delivery Agents have separate login flows," and §2.1 lists
/// Admin/Shipping Employee as a formal actor — but §6 Data Requirements never lists an Admin
/// entity at all. A login flow needs *something* to check credentials against, so this
/// minimal aggregate was added to close that gap. Deliberately kept small (no permissions/
/// role-hierarchy modeling) since the SRS gives no further detail on Admin account
/// structure — extend it if/when the business defines multiple admin permission levels.
/// </summary>
public sealed class AdminUser : AggregateRoot<Guid>
{
    public string Name { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private AdminUser() { } // EF Core

    /// <summary>
    /// No public self-registration is exposed for AdminUser in the API layer (unlike
    /// Customer) — deliberately: letting anyone POST their way into an Admin account would
    /// be a real security hole. This factory is used only by a one-time startup seed
    /// (see the API module's Program.cs) until a proper "admin creates admin" flow exists.
    /// </summary>
    public static AdminUser Create(string name, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required.");
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email is required.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Password hash is required.");

        return new AdminUser
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void ChangePasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash)) throw new DomainException("Password hash is required.");
        PasswordHash = newPasswordHash;
    }
}
