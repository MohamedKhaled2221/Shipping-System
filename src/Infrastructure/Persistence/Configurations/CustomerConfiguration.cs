using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(256);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.PasswordHash).IsRequired();

        // FR-1.1 — a customer registers with a unique email; enforced at the DB level,
        // not just in Application-layer validation.
        builder.HasIndex(c => c.Email).IsUnique();

        builder.Ignore(c => c.DomainEvents);
    }
}
