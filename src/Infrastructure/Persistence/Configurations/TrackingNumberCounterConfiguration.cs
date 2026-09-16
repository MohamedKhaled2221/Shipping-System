using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ShippingSystem.Infrastructure.Persistence.Configurations;

public sealed class TrackingNumberCounterConfiguration : IEntityTypeConfiguration<TrackingNumberCounter>
{
    public void Configure(EntityTypeBuilder<TrackingNumberCounter> builder)
    {
        builder.ToTable("TrackingNumberCounters");
        builder.HasKey(c => c.Year);
        builder.Property(c => c.Year).ValueGeneratedNever(); // the year IS the key, not an identity
        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}
