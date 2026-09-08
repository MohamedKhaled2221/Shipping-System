using ShippingSystem.Domain.Common;
using ShippingSystem.Domain.Exceptions;

namespace ShippingSystem.Domain.Entities;

/// <summary>FR-7.1 to FR-7.4.</summary>
public sealed class DeliveryAgent : AggregateRoot<Guid>
{
    public string Name { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public bool IsAvailable { get; private set; }
    public string PasswordHash { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private DeliveryAgent() { } // EF Core

    public static DeliveryAgent Register(string name, string phone, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required.");
        if (string.IsNullOrWhiteSpace(phone)) throw new DomainException("Phone is required.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Password hash is required.");

        return new DeliveryAgent
        {
            Id = Guid.NewGuid(),
            Name = name,
            Phone = phone,
            PasswordHash = passwordHash,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetAvailability(bool isAvailable) => IsAvailable = isAvailable;

    public void UpdateProfile(string name, string phone)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required.");
        Name = name;
        Phone = phone;
    }
}
