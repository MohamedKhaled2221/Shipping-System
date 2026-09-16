using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(300);
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
        builder.Property(p => p.QuantityAvailable);

        // FR-5.3, NFR Concurrency — SQL Server ROWVERSION column. EF Core translates
        // IsRowVersion() into an auto-updating rowversion/timestamp column and includes it
        // in the WHERE clause of every UPDATE, so a stale in-memory Product loses the race
        // instead of silently overwriting a concurrent reservation.
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.Ignore(p => p.DomainEvents);
    }
}
