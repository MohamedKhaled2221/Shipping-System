using ShippingSystem.Domain.Common;
using ShippingSystem.Domain.Exceptions;

namespace ShippingSystem.Domain.Entities;

/// <summary>FR-2.1, FR-2.2. Aggregate root — owns the login identity and profile only.</summary>
public sealed class Customer : AggregateRoot<Guid>
{
    public string Name { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private Customer() { } // EF Core

    public static Customer Register(string name, string email, string phone, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required.");
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email is required.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Password hash is required.");

        return new Customer
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email.Trim().ToLowerInvariant(),
            Phone = phone,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string name, string phone)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required.");
        Name = name;
        Phone = phone;
    }

    public void ChangePasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash)) throw new DomainException("Password hash is required.");
        PasswordHash = newPasswordHash;
    }
}
