using MediatR;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Application.Shipments.Common;

namespace ShippingSystem.Application.Shipments.Queries.GetShipmentsByAgent;

public sealed class GetShipmentsByAgentQueryHandler : IRequestHandler<GetShipmentsByAgentQuery, IReadOnlyList<ShipmentDto>>
{
    private readonly IShipmentRepository _shipments;

    public GetShipmentsByAgentQueryHandler(IShipmentRepository shipments) => _shipments = shipments;

    public async Task<IReadOnlyList<ShipmentDto>> Handle(GetShipmentsByAgentQuery request, CancellationToken cancellationToken)
    {
        var shipments = await _shipments.GetByDeliveryAgentIdAsync(request.DeliveryAgentId, cancellationToken);
        return shipments.Select(ShipmentMapping.ToDto).ToList();
    }
}
