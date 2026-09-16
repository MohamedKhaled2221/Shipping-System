using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Configurations;

/// <summary>
/// Child entity of Order — no DbSet exposed on AppDbContext, reachable only via
/// Order.Items. Still needs its own IEntityTypeConfiguration because it's a full
/// Entity&lt;Guid&gt;, not an EF-owned value object (it has its own Id/identity per the
/// SRS data model).
/// </summary>
public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Quantity);
        builder.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");

        builder.Ignore(i => i.LineTotal); // computed, not persisted
        builder.Ignore(i => i.DomainEvents);

        builder.HasIndex(i => i.ProductId);
    }
}
