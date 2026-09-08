using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Application.Common.Interfaces;

// One interface per AGGREGATE ROOT only (Entity/Order/Product/Shipment/etc.) —
// child entities (OrderItem, ShipmentStatusHistory, FailedDeliveryReason) are
// loaded/saved through their parent aggregate, never via their own repository.
//
// Query methods return tracked aggregates ready for mutation + SaveChangesAsync.
// Pure read-only projections (e.g. list screens, DTOs) should go through a
// separate read-model/query service instead of these, to avoid over-fetching
// full aggregates just to render a grid.

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    void Add(Customer customer);
}

public interface IShippingAddressRepository
{
    Task<ShippingAddress?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShippingAddress>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    void Add(ShippingAddress address);
    void Remove(ShippingAddress address);
}

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Loads several products in one round trip for order-creation validation (FR-5.1).</summary>
    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>FR-3.1 catalog listing — ordered by Name for a stable page-to-page sequence. pageNumber is 1-based.</summary>
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    void Add(Product product);
}

public interface IInventoryReservationRepository
{
    Task<InventoryReservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryReservation>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>FR-5.6 — used by the background expiry job to auto-release abandoned holds.</summary>
    Task<IReadOnlyList<InventoryReservation>> GetExpiredReservedAsync(DateTime asOfUtc, CancellationToken cancellationToken = default);
    void Add(InventoryReservation reservation);
}

public interface IOrderRepository
{
    /// <summary>Loads the Order aggregate including its OrderItems.</summary>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    void Add(Order order);
}

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Payment?> GetByTransactionRefAsync(string transactionRef, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    void Add(Payment payment);
}

public interface IShipmentRepository
{
    /// <summary>Loads the Shipment aggregate including StatusHistory and FailedDeliveryReasons.</summary>
    Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<Shipment?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Shipment>> GetByDeliveryAgentIdAsync(Guid deliveryAgentId, CancellationToken cancellationToken = default);
    void Add(Shipment shipment);
}

public interface IDeliveryAgentRepository
{
    Task<DeliveryAgent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>DeliveryAgent has no Email field in the Domain model (see its entity) — agents log in by phone.</summary>
    Task<DeliveryAgent?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeliveryAgent>> GetAvailableAsync(CancellationToken cancellationToken = default);
    void Add(DeliveryAgent agent);
}

/// <summary>Backs AdminUser (see Domain.Entities.AdminUser for why this aggregate exists at all).</summary>
public interface IAdminUserRepository
{
    Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AdminUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    void Add(AdminUser admin);
}

public interface INotificationRepository
{
    void Add(Notification notification);
}

public interface IShippingProviderConfigRepository
{
    Task<ShippingProviderConfig?> GetByProviderNameAsync(string providerName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShippingProviderConfig>> GetActiveAsync(CancellationToken cancellationToken = default);
}

public interface IProcessedWebhookEventRepository
{
    /// <summary>FR-11.4, FR-4.7 — the idempotency check every webhook handler must call first.</summary>
    Task<bool> ExistsAsync(string providerName, string externalEventId, CancellationToken cancellationToken = default);
    void Add(ProcessedWebhookEvent processedEvent);
}
