using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ShippingSystem.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RequestType).IsRequired().HasMaxLength(200);
        builder.Property(r => r.IdempotencyKey).IsRequired().HasMaxLength(200);

        builder.HasIndex(r => new { r.RequestType, r.IdempotencyKey }).IsUnique();
    }
}
