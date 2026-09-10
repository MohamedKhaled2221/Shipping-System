using MediatR;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Application.Orders.Commands.CreateOrder;

public sealed record CreateOrderItemRequest(Guid ProductId, int Quantity);

/// <summary>
/// FR-3.2, FR-3.4. IdempotencyKey should be a client-generated GUID/UUID (e.g. from an
/// `Idempotency-Key` request header) so retried checkouts never create duplicate orders
/// (NFR: Idempotency — "critical order/shipment creation operations must be idempotent").
/// </summary>
public sealed record CreateOrderCommand(
    Guid CustomerId,
    Guid ShippingAddressId,
    PaymentMethod PaymentMethod,
    IReadOnlyList<CreateOrderItemRequest> Items,
    string IdempotencyKey) : IRequest<OrderDto>;
