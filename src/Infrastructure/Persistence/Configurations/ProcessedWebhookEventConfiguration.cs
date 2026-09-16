using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence.Configurations;

public sealed class ProcessedWebhookEventConfiguration : IEntityTypeConfiguration<ProcessedWebhookEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedWebhookEvent> builder)
    {
        builder.ToTable("ProcessedWebhookEvents");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ProviderName).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ExternalEventId).IsRequired().HasMaxLength(200);

        // FR-11.4, FR-4.7 — THE idempotency guarantee. This unique index, not the
        // ExistsAsync check that runs before it, is what actually closes the race between
        // two near-simultaneous deliveries of the same webhook: the second insert throws a
        // unique-constraint DbUpdateException, which the repository/UnitOfWork translates
        // into a no-op rather than a 500.
        builder.HasIndex(e => new { e.ProviderName, e.ExternalEventId }).IsUnique();

        builder.Ignore(e => e.DomainEvents);
    }
}
