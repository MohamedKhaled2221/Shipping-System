using MediatR;
using ShippingSystem.Application.Common.Exceptions;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Application.Orders.Queries.GetOrderById;

public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;

    public GetOrderByIdQueryHandler(IOrderRepository orders, IProductRepository products)
    {
        _orders = orders;
        _products = products;
    }

    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        // Product names aren't stored on OrderItem (only ProductId/Quantity/UnitPrice, per the
        // SRS data model) — fetched here for display purposes only, never re-priced from this lookup.
        var productsById = (await _products.GetByIdsAsync(order.Items.Select(i => i.ProductId), cancellationToken))
            .ToDictionary(p => p.Id);

        return new OrderDto(
            order.Id,
            order.CustomerId,
            order.Status,
            order.PaymentStatus,
            order.PaymentMethod,
            order.TotalAmount,
            order.CreatedAt,
            order.Items
                .Select(i => new OrderItemDto(
                    i.ProductId,
                    productsById.TryGetValue(i.ProductId, out var p) ? p.Name : "(unknown)",
                    i.Quantity,
                    i.UnitPrice,
                    i.LineTotal))
                .ToList());
    }
}
