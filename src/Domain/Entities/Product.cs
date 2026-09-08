using ShippingSystem.Domain.Common;
using ShippingSystem.Domain.Exceptions;

namespace ShippingSystem.Domain.Entities;

/// <summary>
/// FR-3.1, FR-5.x. QuantityAvailable represents *unreserved* stock — reserving decrements it
/// immediately (FR-5.2), releasing increments it back. RowVersion drives optimistic
/// concurrency (FR-5.3) so 1000 concurrent requests against 100 units can never oversell (NFR).
/// </summary>
public sealed class Product : AggregateRoot<Guid>, IHasConcurrencyToken
{
    public string Name { get; private set; } = default!;
    public decimal Price { get; private set; }
    public int QuantityAvailable { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private Product() { } // EF Core

    public static Product Create(string name, decimal price, int initialQuantity)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Product name is required.");
        if (price < 0) throw new DomainException("Price cannot be negative.");
        if (initialQuantity < 0) throw new DomainException("Initial quantity cannot be negative.");

        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price,
            QuantityAvailable = initialQuantity
        };
    }

    public void UpdateDetails(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Product name is required.");
        if (price < 0) throw new DomainException("Price cannot be negative.");
        Name = name;
        Price = price;
    }

    /// <summary>
    /// FR-5.2, FR-5.5 — atomically decrements available stock. The caller (Application layer)
    /// must persist this change guarded by RowVersion; a stale RowVersion causes EF Core to
    /// throw a concurrency exception, which is translated to ConcurrencyConflictException.
    /// </summary>
    public void ReserveStock(int quantity)
    {
        if (quantity <= 0) throw new DomainException("Reservation quantity must be positive.");
        if (quantity > QuantityAvailable)
            throw new InsufficientInventoryException(Id, quantity, QuantityAvailable);

        QuantityAvailable -= quantity;
    }

    /// <summary>FR-5.4 — releases previously reserved stock back to available pool.</summary>
    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0) throw new DomainException("Release quantity must be positive.");
        QuantityAvailable += quantity;
    }

    public void RestockManually(int quantity)
    {
        if (quantity <= 0) throw new DomainException("Restock quantity must be positive.");
        QuantityAvailable += quantity;
    }
}
