using Microsoft.EntityFrameworkCore;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Infrastructure.Persistence;

/// <summary>
/// The only place EF Core touches the Domain model directly. Every mapping decision
/// (RowVersion columns, backing-field access for private collections, cascade rules) lives
/// in Configurations/*, applied via ApplyConfigurationsFromAssembly below — the DbContext
/// itself stays a thin DbSet registry.
///
/// Only aggregate roots get a DbSet (per Application's "one repository per aggregate root"
/// rule). Child entities (OrderItem, ShipmentStatusHistory, FailedDeliveryReason) are
/// reachable only through their parent's navigation and are never queried directly.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ShippingAddress> ShippingAddresses => Set<ShippingAddress>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<DeliveryAgent> DeliveryAgents => Set<DeliveryAgent>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ShippingProviderConfig> ShippingProviderConfigs => Set<ShippingProviderConfig>();
    public DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents => Set<ProcessedWebhookEvent>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    // Infra-only plumbing tables — not part of the Domain model, see their own files for why.
    public DbSet<TrackingNumberCounter> TrackingNumberCounters => Set<TrackingNumberCounter>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<RefreshTokenRecord> RefreshTokenRecords => Set<RefreshTokenRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
