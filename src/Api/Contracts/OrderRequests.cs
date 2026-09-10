using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Api.Contracts;

public sealed record OrderItemLineRequest(Guid ProductId, int Quantity);

/// <summary>
/// CustomerId is deliberately NOT part of this request body — it's taken from the
/// authenticated user's JWT claims in the controller, so a customer can never place an
/// order "as" someone else by tampering with the payload.
/// </summary>
public sealed record CreateOrderRequest(
    Guid ShippingAddressId,
    PaymentMethod PaymentMethod,
    IReadOnlyList<OrderItemLineRequest> Items);
