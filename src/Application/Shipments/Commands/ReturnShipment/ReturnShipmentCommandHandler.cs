using MediatR;
using ShippingSystem.Application.Common.Exceptions;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Application.Shipments.Commands.ReturnShipment;

public sealed class ReturnShipmentCommandHandler : IRequestHandler<ReturnShipmentCommand>
{
    private readonly IShipmentRepository _shipments;
    private readonly IUnitOfWork _unitOfWork;

    public ReturnShipmentCommandHandler(IShipmentRepository shipments, IUnitOfWork unitOfWork)
    {
        _shipments = shipments;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ReturnShipmentCommand request, CancellationToken cancellationToken)
    {
        var shipment = await _shipments.GetByIdAsync(request.ShipmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Shipment), request.ShipmentId);

        // Domain.Entities.Shipment.ReturnShipment -> TransitionTo(Returned, ...), which
        // ShipmentStateMachine only allows from Failed (FR-6.7) — throws
        // InvalidStateTransitionException otherwise.
        shipment.ReturnShipment(request.Reason, request.ReturnedBy);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
