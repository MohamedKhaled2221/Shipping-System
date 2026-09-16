using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.PaymentStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Method).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.TransactionRef).IsRequired().HasMaxLength(200);
        builder.Property(p => p.RefundReason).HasMaxLength(500);

        // FR-4.7 — the idempotency key ConfirmOrderPaymentCommandHandler looks up before
        // ever creating a duplicate Payment row for the same gateway charge.
        builder.HasIndex(p => p.TransactionRef).IsUnique();
        builder.HasIndex(p => p.OrderId);

        builder.Ignore(p => p.DomainEvents);
    }
}
