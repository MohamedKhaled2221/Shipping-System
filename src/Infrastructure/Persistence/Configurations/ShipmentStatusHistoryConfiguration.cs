using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Configurations;

public sealed class ShipmentStatusHistoryConfiguration : IEntityTypeConfiguration<ShipmentStatusHistory>
{
    public void Configure(EntityTypeBuilder<ShipmentStatusHistory> builder)
    {
        builder.ToTable("ShipmentStatusHistory");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.ChangedBy).IsRequired().HasMaxLength(200);
        builder.Property(h => h.Notes).HasMaxLength(1000);

        // FR-9.2 — tracking lookups always order this by ChangedAt for a shipment.
        builder.HasIndex(h => new { h.ShipmentId, h.ChangedAt });

        builder.Ignore(h => h.DomainEvents);
    }
}
