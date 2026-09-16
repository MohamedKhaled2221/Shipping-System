using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.PaymentStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.PaymentMethod).HasConversion<string>().HasMaxLength(20);

        // FR-5.3, FR-6.3 companion — Order itself doesn't get reserved/assigned concurrently
        // the way Product/Shipment do, but its RowVersion still guards concurrent OrderStatus/
        // PaymentStatus transitions (e.g. a webhook and an admin cancellation racing).
        builder.Property(o => o.RowVersion).IsRowVersion();

        builder.HasIndex(o => o.CustomerId);

        // Order.Items is a read-only computed property (`_items.AsReadOnly()`), so EF must
        // read/write the `_items` backing field directly rather than going through the
        // property — UsePropertyAccessMode(Field) tells it to do exactly that.
        builder.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Domain events are runtime-only bookkeeping (Entity.DomainEvents), never persisted.
        builder.Ignore(o => o.DomainEvents);
    }
}
