namespace ShippingSystem.Domain.Exceptions;

/// <summary>
/// Base type for every business-rule violation raised from within the Domain layer.
/// The API's global exception middleware maps this to HTTP 400/409 as appropriate —
/// the Domain itself knows nothing about HTTP.
///
/// Instantiable directly (not abstract): most invariant checks inside entity factory
/// methods and validation guards are one-off messages that don't need their own named
/// type (e.g. "Order must contain at least one item."). Give a violation its own
/// subclass — like InvalidStateTransitionException or InsufficientInventoryException
/// below — only when calling code needs to catch it specifically or read structured
/// data off it beyond the message string.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}
