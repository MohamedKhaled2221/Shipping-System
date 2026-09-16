using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Configurations;

public sealed class ShippingProviderConfigConfiguration : IEntityTypeConfiguration<ShippingProviderConfig>
{
    public void Configure(EntityTypeBuilder<ShippingProviderConfig> builder)
    {
        builder.ToTable("ShippingProviderConfigs");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ProviderName).IsRequired().HasMaxLength(50);
        builder.Property(c => c.ConfigJson).HasColumnType("nvarchar(max)");

        // FR-11.2/11.3 — IShippingProviderResolver looks providers up by name.
        builder.HasIndex(c => c.ProviderName).IsUnique();

        builder.Ignore(c => c.DomainEvents);
    }
}
