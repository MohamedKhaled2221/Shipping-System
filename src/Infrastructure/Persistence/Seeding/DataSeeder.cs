using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Entities;
using ShippingSystem.Domain.Enums;
using ShippingSystem.Domain.Services;

namespace ShippingSystem.Infrastructure.Persistence.Seeding;

/// <summary>
/// Development/demo convenience only — never runs in Production (see Program.cs's
/// environment guard). Builds every entity through its real Domain factory method
/// (Product.Create, Order.Create, Shipment.Create, ...) and saves via the normal
/// AppDbContext, rather than EF Core's `HasData` seeding: `HasData` requires static,
/// migration-baked values and bypasses every constructor/factory invariant this solution's
/// rich domain model depends on (validation, computed TotalAmount, raised domain events),
/// which would either fail outright (private constructors, required computed fields) or
/// silently produce entities that never went through their own business rules.
///
/// Because this saves through the normal AppDbContext, the same DomainEventDispatchInterceptor
/// used by every real request also fires here: seeding a Shipment genuinely raises
/// ShipmentCreatedEvent (booking with LocalShippingProvider), and each seeded status
/// transition genuinely raises ShipmentStatusChangedEvent (cache invalidation, SignalR
/// broadcast, notification dispatch) — exactly like a real request would. This is
/// deliberate: it's the same code path, so seed data can never drift out of sync with what
/// "really" happens, and it doubles as a smoke test of that whole pipeline on every startup.
/// </summary>
public class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DataSeeder>>();

        // Idempotency guard: re-running the app (a restart, `dotnet watch`, etc.) must never
        // duplicate seed data or throw a unique-constraint exception on the second pass.
        if (await dbContext.Products.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Seed data already present; skipping.");
            return;
        }

        logger.LogInformation("Seeding development data...");

        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var trackingNumbers = scope.ServiceProvider.GetRequiredService<ITrackingNumberGenerator>();

        var admin = SeedAdmin(dbContext, passwordHasher);
        var agents = SeedDeliveryAgents(dbContext, passwordHasher);
        var (customer, address) = SeedCustomerAndAddress(dbContext, passwordHasher);
        var products = SeedProducts(dbContext);
        SeedShippingProviderConfigs(dbContext);

        await dbContext.SaveChangesAsync(cancellationToken);

        await SeedSampleOrderAsync(dbContext, trackingNumbers, customer, address, products, agents[0], cancellationToken);

        logger.LogInformation(
            "Seed data created: 1 admin, {AgentCount} delivery agents, 1 customer, {ProductCount} products, 1 sample order/shipment.",
            agents.Length, products.Length);
        logger.LogInformation(
            "Seeded login credentials — Admin: admin@shippingsystem.com / Admin@12345 | " +
            "Agent: 01000000001 / Agent@12345 | Customer: customer@shippingsystem.com / Customer@12345");
    }

    private static AdminUser SeedAdmin(AppDbContext dbContext, IPasswordHasher passwordHasher)
    {
        var admin = AdminUser.Create("System Admin", "admin@shippingsystem.com", passwordHasher.Hash("Admin@12345"));
        dbContext.AdminUsers.Add(admin);
        return admin;
    }

    private static DeliveryAgent[] SeedDeliveryAgents(AppDbContext dbContext, IPasswordHasher passwordHasher)
    {
        var agents = new[]
        {
            DeliveryAgent.Register("Mohamed Ali", "01000000001", passwordHasher.Hash("Agent@12345")),
            DeliveryAgent.Register("Sara Youssef", "01000000002", passwordHasher.Hash("Agent@12345"))
        };
        dbContext.DeliveryAgents.AddRange(agents);
        return agents;
    }

    private static (Customer Customer, ShippingAddress Address) SeedCustomerAndAddress(AppDbContext dbContext, IPasswordHasher passwordHasher)
    {
        var customer = Customer.Register("Ahmed Hassan", "customer@shippingsystem.com", "01100000000", passwordHasher.Hash("Customer@12345"));
        dbContext.Customers.Add(customer);

        var address = ShippingAddress.Create(
            customer.Id, "Egypt", "Cairo", "Nasr City", "Makram Ebeid St.",
            "12", "4B", "Leave with doorman if not home", "01100000000");
        dbContext.ShippingAddresses.Add(address);

        return (customer, address);
    }

    private static Product[] SeedProducts(AppDbContext dbContext)
    {
        var products = new[]
        {
            Product.Create("Wireless Mouse", 15.99m, 200),
            Product.Create("Mechanical Keyboard", 49.99m, 120),
            Product.Create("27\" 4K Monitor", 329.00m, 40),
            Product.Create("USB-C Hub", 24.50m, 150),
            Product.Create("Noise-Cancelling Headphones", 89.99m, 75),
            Product.Create("Laptop Stand", 34.00m, 100),
            Product.Create("Webcam 1080p", 29.99m, 90),
            Product.Create("Portable SSD 1TB", 79.00m, 60)
        };
        dbContext.Products.AddRange(products);
        return products;
    }

    private static void SeedShippingProviderConfigs(AppDbContext dbContext)
    {
        // FR-11.2 — Local active, the three real-carrier stubs present but inactive so an
        // admin (once a management endpoint exists) has rows to toggle rather than create
        // from scratch.
        dbContext.ShippingProviderConfigs.AddRange(
            ShippingProviderConfig.Create("Local", isActive: true, configJson: "{}"),
            ShippingProviderConfig.Create("DHL", isActive: false, configJson: "{}"),
            ShippingProviderConfig.Create("Aramex", isActive: false, configJson: "{}"),
            ShippingProviderConfig.Create("FedEx", isActive: false, configJson: "{}"));
    }

    /// <summary>
    /// One fully-paid order already progressed to OutForDelivery, so a freshly-cloned
    /// frontend has an order-history entry and a live tracking page to render immediately —
    /// without whoever's building the UI needing to first implement checkout just to see
    /// what a shipment detail view looks like.
    /// </summary>
    private static async Task SeedSampleOrderAsync(
        AppDbContext dbContext,
        ITrackingNumberGenerator trackingNumbers,
        Customer customer,
        ShippingAddress address,
        Product[] products,
        DeliveryAgent agent,
        CancellationToken cancellationToken)
    {
        var mouse = products[0];
        var monitor = products[2];

        // Mirrors CreateOrderCommandHandler: reserve stock, record the reservation, then
        // build the Order aggregate from the same (ProductId, Quantity, UnitPrice) shape.
        mouse.ReserveStock(2);
        monitor.ReserveStock(1);

        var orderLines = new[]
        {
            (ProductId: mouse.Id, Quantity: 2, UnitPrice: mouse.Price),
            (ProductId: monitor.Id, Quantity: 1, UnitPrice: monitor.Price)
        };

        var order = Order.Create(customer.Id, address.Id, PaymentMethod.Card, orderLines);
        dbContext.Orders.Add(order);

        var reservations = new[]
        {
            InventoryReservation.Create(mouse.Id, order.Id, 2, TimeSpan.FromMinutes(20)),
            InventoryReservation.Create(monitor.Id, order.Id, 1, TimeSpan.FromMinutes(20))
        };
        dbContext.InventoryReservations.AddRange(reservations);

        // Mirrors ConfirmOrderPaymentCommandHandler's success path: mark paid, confirm the
        // order, consume the reservations.
        var payment = Payment.CreatePending(order.Id, order.TotalAmount, PaymentMethod.Card, "SEED-TXN-0001");
        payment.MarkPaid();
        dbContext.Payments.Add(payment);

        order.UpdatePaymentStatus(PaymentStatus.Paid);
        order.TransitionTo(OrderStatus.Confirmed);
        foreach (var reservation in reservations)
            reservation.Consume();

        // Order/Payment/Reservations committed first so Shipment's OrderId foreign key is
        // valid, and so ShipmentCreatedEvent (raised by Shipment.Create below) fires against
        // an Order that's already fully persisted and Paid.
        await dbContext.SaveChangesAsync(cancellationToken);

        var trackingNumber = trackingNumbers.Generate(DateTime.UtcNow.Year);
        var shipment = Shipment.Create(order.Id, trackingNumber, address.Id, shippingFee: 5.00m, DateTime.UtcNow.AddDays(3));
        dbContext.Shipments.Add(shipment);
        await dbContext.SaveChangesAsync(cancellationToken); // Pending — fires ShipmentCreatedEvent (provider booking)

        // Order is already Paid, so ShipmentPaymentGuard would trivially allow every one of
        // these transitions — not invoked explicitly here since this is seed data, not a
        // command handler, but the sequence below is exactly what a real fulfillment flow
        // would produce for this order.
        shipment.TransitionTo(ShipmentStatus.Confirmed, "Seed");
        shipment.TransitionTo(ShipmentStatus.Preparing, "Seed");
        shipment.AssignToAgent(agent.Id);
        shipment.TransitionTo(ShipmentStatus.AssignedToCourier, "Seed");
        shipment.TransitionTo(ShipmentStatus.PickedUp, "Seed");
        shipment.TransitionTo(ShipmentStatus.OutForDelivery, "Seed", "On the way to Nasr City.");

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}