using MediatR;
using ShippingSystem.Application.Common.Exceptions;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Entities;
using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Application.Orders.Commands.CancelOrder;

/// <summary>
/// SRS §2.2 step 11: "If the order is cancelled before shipment/fulfillment, reserved
/// inventory is released back to stock." FR-5.4.
/// </summary>
public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand>
{
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly IInventoryReservationRepository _reservations;
    private readonly IShipmentRepository _shipments;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOrderCommandHandler(
        IOrderRepository orders,
        IProductRepository products,
        IInventoryReservationRepository reservations,
        IShipmentRepository shipments,
        IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _products = products;
        _reservations = reservations;
        _shipments = shipments;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        // A shipment already past Pending means fulfillment has started; cancelling the
        // shipment itself is a separate operation (Shipment module) with its own state
        // machine — this handler only owns the Order side of §2.2 step 11.
        var shipment = await _shipments.GetByOrderIdAsync(order.Id, cancellationToken);
        if (shipment is not null && shipment.Status != ShipmentStatus.Pending)
        {
            throw new Domain.Exceptions.DomainException(
                $"Order '{order.Id}' already has an active shipment in status '{shipment.Status}'; " +
                "cancel the shipment first.");
        }

        // Throws Domain.Exceptions.InvalidStateTransitionException if the order is already
        // Completed/Cancelled — the state machine is the single source of truth, not a
        // manual check here.
        order.TransitionTo(OrderStatus.Cancelled);

        var activeReservations = (await _reservations.GetByOrderIdAsync(order.Id, cancellationToken))
            .Where(r => r.Status == ReservationStatus.Reserved)
            .ToList();

        if (activeReservations.Count > 0)
        {
            var productIds = activeReservations.Select(r => r.ProductId).Distinct();
            var productsById = (await _products.GetByIdsAsync(productIds, cancellationToken))
                .ToDictionary(p => p.Id);

            foreach (var reservation in activeReservations)
            {
                reservation.Release();
                productsById[reservation.ProductId].ReleaseStock(reservation.QuantityReserved);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
