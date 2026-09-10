using MediatR;
using ShippingSystem.Application.Common.Exceptions;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Application.Shipments.Common;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Application.Shipments.Queries.GetShipmentById;

public sealed class GetShipmentByIdQueryHandler : IRequestHandler<GetShipmentByIdQuery, ShipmentDto>
{
    private readonly IShipmentRepository _shipments;

    public GetShipmentByIdQueryHandler(IShipmentRepository shipments) => _shipments = shipments;

    public async Task<ShipmentDto> Handle(GetShipmentByIdQuery request, CancellationToken cancellationToken)
    {
        var shipment = await _shipments.GetByIdAsync(request.ShipmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Shipment), request.ShipmentId);

        return ShipmentMapping.ToDto(shipment);
    }
}
