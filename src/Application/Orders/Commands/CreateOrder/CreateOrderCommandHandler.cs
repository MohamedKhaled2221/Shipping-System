using MediatR;
using ShippingSystem.Application.Common.Exceptions;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Domain.Entities;
using DomainException = ShippingSystem.Domain.Exceptions.DomainException;

namespace ShippingSystem.Application.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private const string RequestTypeKey = nameof(CreateOrderCommand);

    private readonly ICustomerRepository _customers;
    private readonly IShippingAddressRepository _addresses;
    private readonly IProductRepository _products;
    private readonly IOrderRepository _orders;
    private readonly IInventoryReservationRepository _reservations;
    private readonly IIdempotencyService _idempotency;
    private readonly IInventoryReservationPolicy _reservationPolicy;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderCommandHandler(
        ICustomerRepository customers,
        IShippingAddressRepository addresses,
        IProductRepository products,
        IOrderRepository orders,
        IInventoryReservationRepository reservations,
        IIdempotencyService idempotency,
        IInventoryReservationPolicy reservationPolicy,
        IUnitOfWork unitOfWork)
    {
        _customers = customers;
        _addresses = addresses;
        _products = products;
        _orders = orders;
        _reservations = reservations;
        _idempotency = idempotency;
        _reservationPolicy = reservationPolicy;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // NFR Idempotency — a retried checkout with the same key returns the original order
        // instead of creating (or double-reserving stock for) a second one.
        var existingOrderId = await _idempotency.TryGetExistingResultAsync(RequestTypeKey, request.IdempotencyKey, cancellationToken);
        if (existingOrderId is not null)
        {
            var existingOrder = await _orders.GetByIdAsync(existingOrderId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Order), existingOrderId.Value);
            var existingProducts = await _products.GetByIdsAsync(existingOrder.Items.Select(i => i.ProductId), cancellationToken);
            return MapToDto(existingOrder, existingProducts.ToDictionary(p => p.Id));
        }

        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        var address = await _addresses.GetByIdAsync(request.ShippingAddressId, cancellationToken)
            ?? throw new NotFoundException(nameof(ShippingAddress), request.ShippingAddressId);

        if (address.CustomerId != customer.Id)
            throw new DomainException("The shipping address does not belong to this customer.");

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _products.GetByIdsAsync(productIds, cancellationToken);
        var productsById = products.ToDictionary(p => p.Id);

        foreach (var item in request.Items)
        {
            if (!productsById.ContainsKey(item.ProductId))
                throw new NotFoundException(nameof(Product), item.ProductId);
        }

        // FR-5.1, FR-5.2, FR-5.5 — reserve stock for every line atomically before the order exists.
        // Product.ReserveStock throws InsufficientInventoryException (a DomainException) if any
        // line exceeds available stock; nothing has been added to the UnitOfWork yet at that
        // point, so no partial state is persisted.
        foreach (var item in request.Items)
            productsById[item.ProductId].ReserveStock(item.Quantity);

        var orderLines = request.Items
            .Select(i => (i.ProductId, i.Quantity, UnitPrice: productsById[i.ProductId].Price))
            .ToList();

        var order = Order.Create(request.CustomerId, request.ShippingAddressId, request.PaymentMethod, orderLines);
        _orders.Add(order);

        foreach (var item in request.Items)
        {
            var reservation = InventoryReservation.Create(
                item.ProductId, order.Id, item.Quantity, _reservationPolicy.ExpiryWindow);
            _reservations.Add(reservation);
        }

        _idempotency.StoreResult(RequestTypeKey, request.IdempotencyKey, order.Id);

        // FR-5.3 — Product.RowVersion guards this SaveChanges; a concurrent reservation against
        // the same product surfaces as Domain.Exceptions.ConcurrencyConflictException here,
        // which the API's global exception middleware maps to HTTP 409 so the client can retry
        // the whole (still-idempotent, same key) request.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(order, productsById);
    }

    private static OrderDto MapToDto(Order order, IReadOnlyDictionary<Guid, Product> productsById) => new(
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
