using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Configurations;

public sealed class DeliveryAgentConfiguration : IEntityTypeConfiguration<DeliveryAgent>
{
    public void Configure(EntityTypeBuilder<DeliveryAgent> builder)
    {
        builder.ToTable("DeliveryAgents");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Phone).IsRequired().HasMaxLength(30);
        builder.Property(a => a.PasswordHash).IsRequired();

        // FR-7.2 — "view assigned shipments" and admin's "find an available agent" queries
        // both filter on IsAvailable.
        builder.HasIndex(a => a.IsAvailable);

        // FR-1.2 — agents log in by phone (no Email field on this entity); must be unique
        // for GetByPhoneAsync to be a safe credential lookup.
        builder.HasIndex(a => a.Phone).IsUnique();

        builder.Ignore(a => a.DomainEvents);
    }
}
