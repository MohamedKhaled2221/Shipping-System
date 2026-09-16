using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Configurations;

public sealed class ShippingAddressConfiguration : IEntityTypeConfiguration<ShippingAddress>
{
    public void Configure(EntityTypeBuilder<ShippingAddress> builder)
    {
        builder.ToTable("ShippingAddresses");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Country).IsRequired().HasMaxLength(100);
        builder.Property(a => a.City).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Area).HasMaxLength(100);
        builder.Property(a => a.Street).IsRequired().HasMaxLength(200);
        builder.Property(a => a.BuildingNumber).HasMaxLength(30);
        builder.Property(a => a.ApartmentNumber).HasMaxLength(30);
        builder.Property(a => a.Instructions).HasMaxLength(500);
        builder.Property(a => a.Phone).IsRequired().HasMaxLength(30);

        // No navigation back to Customer on purpose — ShippingAddress is looked up by Id
        // (from Order/Shipment) via IShippingAddressRepository, never loaded as part of the
        // Customer aggregate graph.
        builder.HasIndex(a => a.CustomerId);
        builder.Ignore(a => a.DomainEvents);
    }
}
