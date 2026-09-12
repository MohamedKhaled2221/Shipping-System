using MediatR;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Application.DeliveryAgents.Common;

namespace ShippingSystem.Application.DeliveryAgents.Queries.GetAvailableAgents;

public sealed class GetAvailableAgentsQueryHandler : IRequestHandler<GetAvailableAgentsQuery, IReadOnlyList<DeliveryAgentDto>>
{
    private readonly IDeliveryAgentRepository _agents;

    public GetAvailableAgentsQueryHandler(IDeliveryAgentRepository agents) => _agents = agents;

    public async Task<IReadOnlyList<DeliveryAgentDto>> Handle(GetAvailableAgentsQuery request, CancellationToken cancellationToken)
    {
        var agents = await _agents.GetAvailableAsync(cancellationToken);
        return agents.Select(DeliveryAgentMapping.ToDto).ToList();
    }
}
