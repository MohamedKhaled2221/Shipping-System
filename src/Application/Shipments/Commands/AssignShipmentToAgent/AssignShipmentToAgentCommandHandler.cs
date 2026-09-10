using MediatR;
using ShippingSystem.Application.Common.Exceptions;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Application.Shipments.Common;
using ShippingSystem.Domain.Entities;
using ShippingSystem.Domain.Enums;
using ShippingSystem.Domain.Exceptions;
using ShippingSystem.Domain.Services;

namespace ShippingSystem.Application.Shipments.Commands.AssignShipmentToAgent;

/// <summary>
/// FR-6.3 — double-assignment under concurrent requests is prevented by Shipment.RowVersion:
/// two concurrent calls for the same shipment both load the current RowVersion, only the
/// first SaveChangesAsync wins, the second surfaces UnitOfWork's ConcurrencyConflictException
/// (mapped to HTTP 409) instead of silently overwriting the first assignment.
/// </summary>
public sealed class AssignShipmentToAgentCommandHandler : IRequestHandler<AssignShipmentToAgentCommand, ShipmentDto>
{
    private readonly IShipmentRepository _shipments;
    private readonly IDeliveryAgentRepository _agents;
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;

    public AssignShipmentToAgentCommandHandler(
        IShipmentRepository shipments,
        IDeliveryAgentRepository agents,
        IOrderRepository orders,
        IUnitOfWork unitOfWork)
    {
        _shipments = shipments;
        _agents = agents;
        _orders = orders;
        _unitOfWork = unitOfWork;
    }

    public async Task<ShipmentDto> Handle(AssignShipmentToAgentCommand request, CancellationToken cancellationToken)
    {
        var shipment = await _shipments.GetByIdAsync(request.ShipmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Shipment), request.ShipmentId);

        var agent = await _agents.GetByIdAsync(request.DeliveryAgentId, cancellationToken)
            ?? throw new NotFoundException(nameof(DeliveryAgent), request.DeliveryAgentId);

        if (!agent.IsAvailable)
            throw new DomainException($"Delivery agent '{agent.Id}' is not currently available for assignment.");

        var order = await _orders.GetByIdAsync(shipment.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), shipment.OrderId);

        // FR-4.3 — AssignedToCourier is a gated status for prepaid orders.
        ShipmentPaymentGuard.EnsureCanProgress(order, ShipmentStatus.AssignedToCourier);

        // Sets DeliveryAgentId (throws DomainException if the shipment is already terminal).
        shipment.AssignToAgent(request.DeliveryAgentId);

        // Throws InvalidStateTransitionException unless the shipment is currently Preparing —
        // the state machine (FR-6.7) is the single source of truth for that precondition.
        shipment.TransitionTo(ShipmentStatus.AssignedToCourier, request.AssignedBy);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ShipmentMapping.ToDto(shipment);
    }
}
