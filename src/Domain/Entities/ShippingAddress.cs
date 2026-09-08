using ShippingSystem.Domain.Common;
using ShippingSystem.Domain.Exceptions;

namespace ShippingSystem.Domain.Entities;

/// <summary>Section 3.8. Owned by a Customer; referenced by Id from Shipment.</summary>
public sealed class ShippingAddress : Entity<Guid>
{
    public Guid CustomerId { get; private set; }
    public string Country { get; private set; } = default!;
    public string City { get; private set; } = default!;
    public string Area { get; private set; } = default!;
    public string Street { get; private set; } = default!;
    public string BuildingNumber { get; private set; } = default!;
    public string? ApartmentNumber { get; private set; }
    public string? Instructions { get; private set; }
    public string Phone { get; private set; } = default!;

    private ShippingAddress() { } // EF Core

    public static ShippingAddress Create(
        Guid customerId, string country, string city, string area, string street,
        string buildingNumber, string? apartmentNumber, string? instructions, string phone)
    {
        if (customerId == Guid.Empty) throw new DomainException("Address must belong to a customer.");
        if (string.IsNullOrWhiteSpace(country)) throw new DomainException("Country is required.");
        if (string.IsNullOrWhiteSpace(city)) throw new DomainException("City is required.");
        if (string.IsNullOrWhiteSpace(street)) throw new DomainException("Street is required.");
        if (string.IsNullOrWhiteSpace(phone)) throw new DomainException("Phone number is required.");

        return new ShippingAddress
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Country = country,
            City = city,
            Area = area,
            Street = street,
            BuildingNumber = buildingNumber,
            ApartmentNumber = apartmentNumber,
            Instructions = instructions,
            Phone = phone
        };
    }

    public void Update(string country, string city, string area, string street,
        string buildingNumber, string? apartmentNumber, string? instructions, string phone)
    {
        Country = country;
        City = city;
        Area = area;
        Street = street;
        BuildingNumber = buildingNumber;
        ApartmentNumber = apartmentNumber;
        Instructions = instructions;
        Phone = phone;
    }
}
