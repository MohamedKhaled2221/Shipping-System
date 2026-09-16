using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Configurations;

public sealed class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.ToTable("InventoryReservations");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

        // FR-5.6 — the background expiry sweep queries (Status = Reserved AND ExpiresAt <= now),
        // so this composite index keeps that scan cheap even with a large reservation history.
        builder.HasIndex(r => new { r.Status, r.ExpiresAt });
        builder.HasIndex(r => r.OrderId);
        builder.HasIndex(r => r.ProductId);
        builder.Ignore(r => r.DomainEvents);
    }
}
