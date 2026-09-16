using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Configurations;

public sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("Shipments");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.TrackingNumber).IsRequired().HasMaxLength(20);
        builder.Property(s => s.ShippingFee).HasColumnType("decimal(18,2)");
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.ProviderName).HasMaxLength(50);
        builder.Property(s => s.ProviderReference).HasMaxLength(200);

        // FR-6.3, FR-6.5, NFR Concurrency — guards assignment and every status transition.
        builder.Property(s => s.RowVersion).IsRowVersion();

        // FR-9.1 — tracking numbers must be globally unique; this is the real DB-level
        // guarantee, TrackingNumberGenerator's own retry loop is just the happy path.
        builder.HasIndex(s => s.TrackingNumber).IsUnique();
        builder.HasIndex(s => s.OrderId).IsUnique(); // one Shipment per Order (FR-3.4/FR-6.1)
        builder.HasIndex(s => s.DeliveryAgentId);

        builder.Navigation(s => s.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(s => s.FailedDeliveryReasons).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(s => s.StatusHistory)
            .WithOne()
            .HasForeignKey(h => h.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.FailedDeliveryReasons)
            .WithOne()
            .HasForeignKey(f => f.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(s => s.DomainEvents);
    }
}
