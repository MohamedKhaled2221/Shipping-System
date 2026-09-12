using MediatR;
using ShippingSystem.Application.Common.Exceptions;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Application.DeliveryAgents.Common;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Application.DeliveryAgents.Queries.GetAgentById;

public sealed class GetAgentByIdQueryHandler : IRequestHandler<GetAgentByIdQuery, DeliveryAgentDto>
{
    private readonly IDeliveryAgentRepository _agents;

    public GetAgentByIdQueryHandler(IDeliveryAgentRepository agents) => _agents = agents;

    public async Task<DeliveryAgentDto> Handle(GetAgentByIdQuery request, CancellationToken cancellationToken)
    {
        var agent = await _agents.GetByIdAsync(request.AgentId, cancellationToken)
            ?? throw new NotFoundException(nameof(DeliveryAgent), request.AgentId);

        return DeliveryAgentMapping.ToDto(agent);
    }
}
