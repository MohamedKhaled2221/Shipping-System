using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ShippingSystem.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenRecordConfiguration : IEntityTypeConfiguration<RefreshTokenRecord>
{
    public void Configure(EntityTypeBuilder<RefreshTokenRecord> builder)
    {
        builder.ToTable("RefreshTokenRecords");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Role).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Email).IsRequired().HasMaxLength(256);
        builder.Property(r => r.TokenHash).IsRequired().HasMaxLength(200);

        // ValidateAndRotateAsync looks a presented token up by its hash — must be unique and indexed.
        builder.HasIndex(r => r.TokenHash).IsUnique();
        builder.HasIndex(r => r.UserId);
    }
}
