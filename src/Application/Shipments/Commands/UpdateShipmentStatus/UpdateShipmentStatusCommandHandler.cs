using MediatR;
using ShippingSystem.Application.Common.Exceptions;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Application.Shipments.Common;
using ShippingSystem.Domain.Entities;
using ShippingSystem.Domain.Enums;
using ShippingSystem.Domain.Services;

namespace ShippingSystem.Application.Shipments.Commands.UpdateShipmentStatus;

public sealed class UpdateShipmentStatusCommandHandler : IRequestHandler<UpdateShipmentStatusCommand, ShipmentDto>
{
    // FR-4.3 — the same gated-status set ShipmentPaymentGuard enforces; Failed/Cancelled/Returned
    // are deliberately excluded (a prepaid-but-unpaid order can still be marked Failed/Cancelled).
    private static readonly ShipmentStatus[] GatedStatuses =
    {
        ShipmentStatus.Preparing,
        ShipmentStatus.PickedUp,
        ShipmentStatus.OutForDelivery,
        ShipmentStatus.Delivered
    };

    private readonly IShipmentRepository _shipments;
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateShipmentStatusCommandHandler(
        IShipmentRepository shipments, IOrderRepository orders, IUnitOfWork unitOfWork)
    {
        _shipments = shipments;
        _orders = orders;
        _unitOfWork = unitOfWork;
    }

    public async Task<ShipmentDto> Handle(UpdateShipmentStatusCommand request, CancellationToken cancellationToken)
    {
        var shipment = await _shipments.GetByIdAsync(request.ShipmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Shipment), request.ShipmentId);

        if (GatedStatuses.Contains(request.NewStatus))
        {
            var order = await _orders.GetByIdAsync(shipment.OrderId, cancellationToken)
                ?? throw new NotFoundException(nameof(Order), shipment.OrderId);

            ShipmentPaymentGuard.EnsureCanProgress(order, request.NewStatus);
        }

        if (request.NewStatus == ShipmentStatus.Failed)
        {
            // FR-7.5 — Notes is guaranteed non-empty here by the validator; RecordFailure
            // both transitions to Failed and adds the FailedDeliveryReason row atomically.
            shipment.RecordFailure(request.Notes!, request.ChangedBy);
        }
        else
        {
            // Throws InvalidStateTransitionException (FR-6.7) if not a legal transition
            // from the shipment's current status.
            shipment.TransitionTo(request.NewStatus, request.ChangedBy, request.Notes);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ShipmentMapping.ToDto(shipment);
    }
}
