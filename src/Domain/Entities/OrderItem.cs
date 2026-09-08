using ShippingSystem.Domain.Common;
using ShippingSystem.Domain.Exceptions;

namespace ShippingSystem.Domain.Entities;

/// <summary>FR-3.2. Child entity of the Order aggregate — never loaded/saved independently.</summary>
public sealed class OrderItem : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;

    private OrderItem() { } // EF Core

    internal static OrderItem Create(Guid productId, int quantity, decimal unitPrice)
    {
        if (quantity <= 0) throw new DomainException("Order item quantity must be positive.");
        if (unitPrice < 0) throw new DomainException("Order item unit price cannot be negative.");

        return new OrderItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }

    internal void AssignToOrder(Guid orderId) => OrderId = orderId;
}
