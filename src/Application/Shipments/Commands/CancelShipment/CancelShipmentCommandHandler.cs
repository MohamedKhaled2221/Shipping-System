using MediatR;
using ShippingSystem.Application.Common.Exceptions;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Entities;
using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Application.Shipments.Commands.CancelShipment;

public sealed class CancelShipmentCommandHandler : IRequestHandler<CancelShipmentCommand>
{
    private readonly IShipmentRepository _shipments;
    private readonly IUnitOfWork _unitOfWork;

    public CancelShipmentCommandHandler(IShipmentRepository shipments, IUnitOfWork unitOfWork)
    {
        _shipments = shipments;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CancelShipmentCommand request, CancellationToken cancellationToken)
    {
        var shipment = await _shipments.GetByIdAsync(request.ShipmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Shipment), request.ShipmentId);

        // Throws InvalidStateTransitionException (FR-6.7) if the shipment is already
        // Delivered/Cancelled/Returned — a terminal state has no valid path to Cancelled.
        shipment.TransitionTo(ShipmentStatus.Cancelled, request.CancelledBy, request.Reason);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
