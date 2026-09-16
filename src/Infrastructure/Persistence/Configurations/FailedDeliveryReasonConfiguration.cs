using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Configurations;

public sealed class FailedDeliveryReasonConfiguration : IEntityTypeConfiguration<FailedDeliveryReason>
{
    public void Configure(EntityTypeBuilder<FailedDeliveryReason> builder)
    {
        builder.ToTable("FailedDeliveryReasons");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Reason).IsRequired().HasMaxLength(1000);

        builder.Ignore(f => f.DomainEvents);
    }
}
