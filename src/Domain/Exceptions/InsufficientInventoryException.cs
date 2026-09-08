namespace ShippingSystem.Domain.Exceptions;

/// <summary>FR-5.5 — inventory can never go negative; reservation exceeding stock is rejected atomically.</summary>
public sealed class InsufficientInventoryException : DomainException
{
    public Guid ProductId { get; }
    public int Requested { get; }
    public int Available { get; }

    public InsufficientInventoryException(Guid productId, int requested, int available)
        : base($"Cannot reserve {requested} unit(s) of product '{productId}'; only {available} available.")
    {
        ProductId = productId;
        Requested = requested;
        Available = available;
    }
}
